using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Runtime.EventBus;
using RDCS.EmployeeAgent.Runtime.Policy;
using RDCS.EmployeeAgent.Runtime.Upload.DTOs;
using RDCS.EmployeeAgent.Runtime.Upload.Events;
using RDCS.EmployeeAgent.Runtime.Upload.Interfaces;
using RDCS.EmployeeAgent.Runtime.Upload.Models;
using RDCS.EmployeeAgent.Runtime.Upload.Policy;
using RDCS.EmployeeAgent.Runtime.Upload.Services;
using RDCS.EmployeeAgent.Runtime.Workers;
using RDCS.EmployeeAgent.Runtime.Screenshot.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace RDCS.EmployeeAgent.Runtime.Upload.Workers;

public class UploadWorker : BackgroundWorkerBase, IUploadWorker
{
    private readonly IUploadQueueService _queueService;
    private readonly IUploadRetryService _retryService;
    private readonly IChecksumService _checksumService;
    private readonly IConnectivityService _connectivityService;
    private readonly INetworkMonitorService _networkMonitor;
    private readonly IUploadStatisticsService _statisticsService;
    private readonly IPolicyEngine _policyEngine;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITokenStorage _tokenStorage;
    private readonly IAuthenticationService _authenticationService;
    private SemaphoreSlim _parallelLock = new(3, 3);

    public override string Name => "UploadWorker";

    public UploadWorker(
        IUploadQueueService queueService,
        IUploadRetryService retryService,
        IChecksumService checksumService,
        IConnectivityService connectivityService,
        INetworkMonitorService networkMonitor,
        IUploadStatisticsService statisticsService,
        IPolicyEngine policyEngine,
        IEventBus eventBus,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ITokenStorage tokenStorage,
        IAuthenticationService authenticationService,
        IAgentLogger logger) : base(logger, eventBus)
    {
        _queueService = queueService;
        _retryService = retryService;
        _checksumService = checksumService;
        _connectivityService = connectivityService;
        _networkMonitor = networkMonitor;
        _statisticsService = statisticsService;
        _policyEngine = policyEngine;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _tokenStorage = tokenStorage;
        _authenticationService = authenticationService;
    }

    protected override async Task OnStartedAsync(CancellationToken cancellationToken)
    {
        try
        {
            var policy = await _policyEngine.GetPolicyAsync<UploadPolicy>(cancellationToken);

#if DEBUG
            policy.Enabled = true;
            policy.MaxParallelUploads = 2;
            await _policyEngine.UpdatePolicyAsync(policy, cancellationToken);
#endif

            Configuration.ExecutionInterval = TimeSpan.FromSeconds(policy.IntervalSeconds);
            _parallelLock = new SemaphoreSlim(policy.MaxParallelUploads, policy.MaxParallelUploads);

            Logger.LogInformation(LogCategory.Application,
                "UploadWorker configured: Enabled={Enabled}, Interval={Interval}s, MaxParallel={Max}",
                policy.Enabled, policy.IntervalSeconds, policy.MaxParallelUploads);
        }
        catch (Exception ex)
        {
            Logger.LogError(LogCategory.Exception, "UploadWorker failed to load policy, using defaults", ex);
            Configuration.ExecutionInterval = TimeSpan.FromSeconds(30);
        }

        _networkMonitor.NetworkLost += (_, _) =>
        {
            Logger.LogWarning(LogCategory.Application, "UploadWorker: Network lost — pausing uploads");
            _ = PauseAsync(CancellationToken.None);
        };

        _networkMonitor.NetworkRestored += (_, _) =>
        {
            Logger.LogInformation(LogCategory.Application, "UploadWorker: Network restored — resuming uploads");
            _ = ResumeAsync(CancellationToken.None);
        };

        await _networkMonitor.StartMonitoringAsync(cancellationToken);

        // Recover jobs left in Uploading/Preparing state from a previous crash
        await _queueService.ResetStuckJobsAsync(cancellationToken);
        Logger.LogInformation(LogCategory.Application, "UploadWorker: Stuck job recovery completed");
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var policy = await _policyEngine.GetPolicyAsync<UploadPolicy>(cancellationToken);
        ScreenshotWorkerTracer.Trace($"UPLOAD_EXEC: Policy Enabled={policy.Enabled}");

        if (!policy.Enabled)
        {
            ScreenshotWorkerTracer.Trace("UPLOAD_EXEC: Disabled by policy, skipping");
            Logger.LogInformation(LogCategory.Application, "UploadWorker: Disabled by policy, skipping");
            return;
        }

        var isOnline = await _connectivityService.CheckConnectivityAsync(cancellationToken);
        ScreenshotWorkerTracer.Trace($"UPLOAD_EXEC: ConnectivityCheck isOnline={isOnline}");
        if (!isOnline)
        {
            ScreenshotWorkerTracer.Trace("UPLOAD_EXEC: Offline, skipping upload cycle");
            Logger.LogInformation(LogCategory.Application, "UploadWorker: Offline, skipping upload cycle");
            return;
        }

        if (policy.PauseOnMeteredConnection && _connectivityService.IsMeteredConnection)
        {
            ScreenshotWorkerTracer.Trace("UPLOAD_EXEC: Metered connection, skipping");
            Logger.LogInformation(LogCategory.Application, "UploadWorker: Metered connection detected, skipping");
            return;
        }

        await _retryService.ProcessDueRetriesAsync(cancellationToken);

        var batch = await _queueService.DequeueBatchAsync(policy.MaxParallelUploads, cancellationToken);
        ScreenshotWorkerTracer.Trace($"UPLOAD_EXEC: Dequeued {batch.Count} jobs");

        if (batch.Count == 0)
            return;

        Logger.LogInformation(LogCategory.Application, "UploadWorker: Processing {Count} upload jobs", batch.Count);

        var uploadTasks = batch.Select(job => ProcessJobAsync(job, policy, cancellationToken));
        await Task.WhenAll(uploadTasks);
    }

    private async Task ProcessJobAsync(UploadJob job, UploadPolicy policy, CancellationToken cancellationToken)
    {
        await _parallelLock.WaitAsync(cancellationToken);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            ScreenshotWorkerTracer.Trace($"UPLOAD_JOB: Starting job {job.JobId} file={job.LocalFilePath}");
            Logger.LogInformation(LogCategory.Application, "UploadWorker: Starting job {JobId}", job.JobId);

            if (!File.Exists(job.LocalFilePath))
            {
                Logger.LogError(LogCategory.Application, $"UploadWorker: File not found {job.LocalFilePath}, skipping job {job.JobId}", null);
                await _queueService.MarkFailedAsync(job.JobId, $"Local file not found: {job.LocalFilePath}", cancellationToken);
                return;
            }

            await _queueService.MarkUploadingAsync(job.JobId, cancellationToken);
            await EventBus.PublishAsync(new UploadStarted(job.JobId, job.EmployeeId, job.DeviceId, job.FileSize, DateTime.UtcNow), cancellationToken);

            var checksum = await _checksumService.ComputeSha256Async(job.LocalFilePath, cancellationToken);
            if (!string.IsNullOrEmpty(job.Checksum) && !string.Equals(checksum, job.Checksum, StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogError(LogCategory.Application, $"UploadWorker: Checksum mismatch job {job.JobId}, skipping", null);
                await _queueService.MarkFailedAsync(job.JobId, "Checksum mismatch — file may be corrupted", cancellationToken);
                return;
            }

            await EventBus.PublishAsync(new UploadVerified(job.JobId, checksum, true, DateTime.UtcNow), cancellationToken);

            var result = await UploadToBackendAsync(job, checksum, cancellationToken);

            if (result == null)
            {
                ScreenshotWorkerTracer.Trace($"UPLOAD_JOB: {job.JobId} backend returned null — scheduling retry");
                await _retryService.ScheduleRetryAsync(job, "Backend upload failed — no response", cancellationToken);
                return;
            }
            ScreenshotWorkerTracer.Trace($"UPLOAD_JOB: {job.JobId} uploaded OK, UploadId={result.UploadId}, S3Key={result.S3ObjectKey}");

            await _queueService.MarkUploadedAsync(job.JobId, result.UploadId, result.S3ObjectKey, cancellationToken);

            await ConfirmCompleteAsync(job, result, cancellationToken);

            await _queueService.MarkCompletedAsync(job.JobId, cancellationToken);
            stopwatch.Stop();

            await _statisticsService.RecordUploadAsync(true, job.FileSize, stopwatch.ElapsedMilliseconds, cancellationToken);

            await EventBus.PublishAsync(new UploadCompleted(
                job.JobId, job.EmployeeId, result.S3ObjectKey, job.FileSize, stopwatch.ElapsedMilliseconds, DateTime.UtcNow), cancellationToken);

            Logger.LogInformation(LogCategory.Application,
                "UploadWorker: Job {JobId} completed in {Ms}ms → {S3Key}", job.JobId, stopwatch.ElapsedMilliseconds, result.S3ObjectKey);

            if (policy.DeleteLocalAfterUpload)
            {
                TryDeleteLocalFile(job.LocalFilePath, job.JobId, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            ScreenshotWorkerTracer.Trace($"UPLOAD_JOB: {job.JobId} EXCEPTION {ex.GetType().Name}: {ex.Message}");
            Logger.LogError(LogCategory.Exception, $"UploadWorker: Job {job.JobId} failed", ex);
            await _statisticsService.RecordUploadAsync(false, 0, stopwatch.ElapsedMilliseconds, cancellationToken);
            await _retryService.ScheduleRetryAsync(job, ex.Message, cancellationToken);
        }
        finally
        {
            _parallelLock.Release();
        }
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var identity = await _tokenStorage.RetrieveTokensAsync(cancellationToken);
        if (identity == null || string.IsNullOrEmpty(identity.AccessToken))
        {
            Logger.LogWarning(LogCategory.Application, "UploadWorker: No stored access token — agent may need to re-login");
            return string.Empty;
        }

        if (identity.ExpiresAt <= DateTime.UtcNow.AddMinutes(2))
        {
            try
            {
                Logger.LogInformation(LogCategory.Application, "UploadWorker: Access token expiring, refreshing");
                var refreshed = await _authenticationService.RefreshTokenAsync(cancellationToken);
                return refreshed.AccessToken;
            }
            catch (Exception ex)
            {
                Logger.LogError(LogCategory.Exception, "UploadWorker: Token refresh failed, using existing token", ex);
                return identity.AccessToken;
            }
        }

        return identity.AccessToken;
    }

    private async Task<UploadResultDto?> UploadToBackendAsync(UploadJob job, string checksum, CancellationToken cancellationToken)
    {
        var apiUrl = _configuration["ApiUrl"] ?? "https://api.rdcs.example.com";
        ScreenshotWorkerTracer.Trace($"UPLOAD_HTTP: ApiUrl={apiUrl}");
        var token = await GetAccessTokenAsync(cancellationToken);

        if (string.IsNullOrEmpty(token))
        {
            ScreenshotWorkerTracer.Trace("UPLOAD_HTTP: No access token available — returning null");
            return null;
        }
        ScreenshotWorkerTracer.Trace($"UPLOAD_HTTP: Token acquired (len={token.Length})");

        using var client = _httpClientFactory.CreateClient("UploadClient");
        client.Timeout = TimeSpan.FromSeconds(120);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var form = new MultipartFormDataContent();
        await using var fileStream = new FileStream(job.LocalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

        form.Add(fileContent, "file", Path.GetFileName(job.LocalFilePath));
        form.Add(new StringContent(job.JobId), "jobId");
        form.Add(new StringContent(job.CorrelationId), "correlationId");
        form.Add(new StringContent(job.EmployeeId), "employeeId");
        form.Add(new StringContent(job.DeviceId), "deviceId");
        form.Add(new StringContent(checksum), "checksum");
        form.Add(new StringContent(job.FileSize.ToString()), "fileSize");
        form.Add(new StringContent(DateTime.UtcNow.ToString("o")), "capturedAt");

        var response = await client.PostAsync($"{apiUrl}/api/storage/upload", form, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            ScreenshotWorkerTracer.Trace($"UPLOAD_HTTP: FAILED {response.StatusCode}: {body}");
            Logger.LogError(LogCategory.Application, $"UploadWorker: Backend returned {response.StatusCode}: {body}", null);
            return null;
        }
        ScreenshotWorkerTracer.Trace($"UPLOAD_HTTP: SUCCESS {response.StatusCode}");

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var envelope = JsonDocument.Parse(json);
        var data = envelope.RootElement.GetProperty("data");
        return JsonSerializer.Deserialize<UploadResultDto>(data.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private async Task ConfirmCompleteAsync(UploadJob job, UploadResultDto result, CancellationToken cancellationToken)
    {
        var apiUrl = _configuration["ApiUrl"] ?? "https://api.rdcs.example.com";
        var token = await GetAccessTokenAsync(cancellationToken);

        if (string.IsNullOrEmpty(token))
            return;

        using var client = _httpClientFactory.CreateClient("UploadClient");
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new UploadCompleteDto
        {
            JobId = job.JobId,
            UploadId = result.UploadId,
            S3ObjectKey = result.S3ObjectKey
        };

        var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
        await client.PostAsync($"{apiUrl}/api/storage/upload/complete", content, cancellationToken);
    }

    private void TryDeleteLocalFile(string filePath, string jobId, CancellationToken cancellationToken)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Logger.LogInformation(LogCategory.Application, "UploadWorker: Deleted local file {Path} after upload", filePath);
                _ = EventBus.PublishAsync(new CleanupCompleted(jobId, filePath, true, DateTime.UtcNow), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(LogCategory.Exception, $"UploadWorker: Failed to delete local file {filePath}", ex);
            _ = EventBus.PublishAsync(new CleanupCompleted(jobId, filePath, false, DateTime.UtcNow), cancellationToken);
        }
    }

    public async Task EnqueueUploadAsync(UploadJob job, CancellationToken cancellationToken = default)
        => await _queueService.EnqueueAsync(job, cancellationToken);

    public async Task<UploadStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        => await _statisticsService.GetAsync(cancellationToken);

    protected override Task OnErrorAsync(Exception exception, CancellationToken cancellationToken)
    {
        Logger.LogError(LogCategory.Exception, "UploadWorker unhandled error", exception);  // correct signature
        return Task.CompletedTask;
    }
}
