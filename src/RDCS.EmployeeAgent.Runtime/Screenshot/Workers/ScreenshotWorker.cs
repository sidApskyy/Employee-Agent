using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Core.Models;
using RDCS.EmployeeAgent.Persistence.Repositories;
using RDCS.EmployeeAgent.Runtime.Queue;
using RDCS.EmployeeAgent.Runtime.EventBus;
using RDCS.EmployeeAgent.Runtime.Policy;
using RDCS.EmployeeAgent.Runtime.Policy.Policies;
using RDCS.EmployeeAgent.Runtime.Screenshot.Diagnostics;
using RDCS.EmployeeAgent.Runtime.Screenshot.Events;
using RDCS.EmployeeAgent.Runtime.Screenshot.Models;
using RDCS.EmployeeAgent.Runtime.Screenshot.Services;
using RDCS.EmployeeAgent.Runtime.Screenshot.Storage;
using RDCS.EmployeeAgent.Runtime.Storage;
using RDCS.EmployeeAgent.Runtime.Upload.Interfaces;
using RDCS.EmployeeAgent.Runtime.Upload.Models;
using RDCS.EmployeeAgent.Runtime.Upload.Services;
using RDCS.EmployeeAgent.Runtime.Workers;
using Microsoft.Extensions.Configuration;
using ScreenshotJobModel = RDCS.EmployeeAgent.Runtime.Screenshot.Models.ScreenshotJob;
using CoreScreenshot = RDCS.EmployeeAgent.Core.Models.Screenshot;

namespace RDCS.EmployeeAgent.Runtime.Screenshot.Workers;

public class ScreenshotWorker : BackgroundWorkerBase, IScreenshotWorker
{
    private readonly IScreenshotService _screenshotService;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly IPolicyEngine _policyEngine;
    private readonly IJobQueue _jobQueue;
    private readonly IEventBus _eventBus;
    private readonly IScreenshotRepository _screenshotRepository;
    private readonly IStorageProvider _storageProvider;
    private readonly StoragePathHelper _storagePathHelper;
    private readonly IConfiguration _configuration;
    private readonly IUploadWorker _uploadWorker;
    private readonly IChecksumService _checksumService;
    private readonly ITokenStorage _tokenStorage;

    public override string Name => "ScreenshotWorker";

    public ScreenshotWorker(
        IScreenshotService screenshotService,
        IImageProcessingService imageProcessingService,
        IPolicyEngine policyEngine,
        IJobQueue jobQueue,
        IEventBus eventBus,
        IScreenshotRepository screenshotRepository,
        IStorageProvider storageProvider,
        StoragePathHelper storagePathHelper,
        IConfiguration configuration,
        IUploadWorker uploadWorker,
        IChecksumService checksumService,
        ITokenStorage tokenStorage,
        IAgentLogger logger) : base(logger, eventBus)
    {
        _screenshotService = screenshotService;
        _imageProcessingService = imageProcessingService;
        _policyEngine = policyEngine;
        _jobQueue = jobQueue;
        _eventBus = eventBus;
        _screenshotRepository = screenshotRepository;
        _storageProvider = storageProvider;
        _storagePathHelper = storagePathHelper;
        _configuration = configuration;
        _uploadWorker = uploadWorker;
        _checksumService = checksumService;
        _tokenStorage = tokenStorage;
        ScreenshotWorkerTracer.Trace($"CONSTRUCTOR: ScreenshotWorker created, config EmployeeId={configuration["Agent:EmployeeId"]}, DeviceId={configuration["Agent:DeviceId"]}");
    }

    protected override async Task OnStartedAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation(LogCategory.Application, "ScreenshotWorker OnStartedAsync called");
        ScreenshotWorkerTracer.Trace("ONSTARTED: ScreenshotWorker OnStartedAsync called");
        
        // Load policy and configure execution interval
        try
        {
            var policy = await _policyEngine.GetPolicyAsync<ScreenshotPolicy>(cancellationToken);
            Logger.LogInformation(LogCategory.Application, 
                "ScreenshotPolicy loaded: Enabled={Enabled}, IntervalSeconds={IntervalSeconds}, LocalStorageEnabled={LocalStorageEnabled}",
                policy.Enabled, policy.IntervalSeconds, policy.LocalStorageEnabled);
            ScreenshotWorkerTracer.Trace($"ONSTARTED: Policy loaded Enabled={policy.Enabled}, Interval={policy.IntervalSeconds}, LocalStorage={policy.LocalStorageEnabled}");
            
            // Development mode: if policy is not enabled or has default values, enable for testing
            #if DEBUG
            if (!policy.Enabled || policy.CaptureDuringOfficeHours)
            {
                Logger.LogWarning(LogCategory.Application, "Development mode: Enabling screenshot capture for testing");
                ScreenshotWorkerTracer.Trace("ONSTARTED: DEBUG - policy disabled or office hours restricted, forcing enable and 24/7 capture");
                policy.Enabled = true;
                policy.IntervalSeconds = 30; // 30 seconds for development
                policy.LocalStorageEnabled = true;
                policy.CaptureDuringOfficeHours = false; // Disable office hours restriction for development
                
                // Persist override so ShouldCaptureAsync and other code paths see the enabled policy
                await _policyEngine.UpdatePolicyAsync(policy, cancellationToken);
                ScreenshotWorkerTracer.Trace("ONSTARTED: DEBUG - policy updated via PolicyEngine (office hours disabled)");
            }
            #endif

            Configuration.ExecutionInterval = TimeSpan.FromSeconds(policy.IntervalSeconds);
            ScreenshotWorkerTracer.Trace($"ONSTARTED: ExecutionInterval set to {Configuration.ExecutionInterval.TotalSeconds}s");
            Logger.LogInformation(LogCategory.Application, 
                "ScreenshotWorker configured with interval: {Interval}s, Enabled: {Enabled}, LocalStorageEnabled: {LocalStorageEnabled}", 
                policy.IntervalSeconds, policy.Enabled, policy.LocalStorageEnabled);
        }
        catch (Exception ex)
        {
            ScreenshotWorkerTracer.Trace($"ONSTARTED: Exception loading policy: {ex.GetType().Name}: {ex.Message}");
            Logger.LogError(LogCategory.Exception, "Failed to load screenshot policy, using defaults", ex);
            Configuration.ExecutionInterval = TimeSpan.FromSeconds(30); // Fallback to 30 seconds
        }
    }

    public async Task<bool> ShouldCaptureAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var policy = await _policyEngine.GetPolicyAsync<ScreenshotPolicy>(cancellationToken);
            ScreenshotWorkerTracer.Trace($"SHOULDCAPTURE: Policy read Enabled={policy.Enabled}, LocalStorage={policy.LocalStorageEnabled}, OfficeHours={policy.CaptureDuringOfficeHours}");
            
            if (!policy.Enabled)
            {
                Logger.LogInformation(LogCategory.Application, "Screenshot capture disabled by policy");
                ScreenshotWorkerTracer.Trace("SHOULDCAPTURE: Returning False - policy.Enabled is false");
                return false;
            }

            if (!policy.LocalStorageEnabled)
            {
                Logger.LogInformation(LogCategory.Application, "Local storage disabled by policy");
                ScreenshotWorkerTracer.Trace("SHOULDCAPTURE: Returning False - policy.LocalStorageEnabled is false");
                return false;
            }

            // Check office hours if enabled
            if (policy.CaptureDuringOfficeHours)
            {
                var now = DateTime.Now.TimeOfDay;
                ScreenshotWorkerTracer.Trace($"SHOULDCAPTURE: Checking office hours Now={now}, Start={policy.OfficeHoursStart}, End={policy.OfficeHoursEnd}");
                if (now < policy.OfficeHoursStart || now > policy.OfficeHoursEnd)
                {
                    Logger.LogInformation(LogCategory.Application, "Outside office hours, skipping capture");
                    ScreenshotWorkerTracer.Trace("SHOULDCAPTURE: Returning False - outside office hours");
                    return false;
                }
            }

            ScreenshotWorkerTracer.Trace("SHOULDCAPTURE: Returning True - all checks passed");
            return true;
        }
        catch (Exception ex)
        {
            ScreenshotWorkerTracer.Trace($"SHOULDCAPTURE: EXCEPTION {ex.GetType().Name}: {ex.Message}");
            Logger.LogError(LogCategory.Exception, "Failed to check capture policy", ex);
            return false;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        ScreenshotWorkerTracer.Trace("EXECUTE: ScreenshotWorker ExecuteAsync tick");

        var shouldCapture = await ShouldCaptureAsync(cancellationToken);
        ScreenshotWorkerTracer.Trace($"EXECUTE: ShouldCaptureAsync returned {shouldCapture}");

        if (shouldCapture)
        {
            ScreenshotWorkerTracer.Trace("EXECUTE: Starting CaptureAndProcessAsync");
            await CaptureAndProcessAsync(cancellationToken);
            ScreenshotWorkerTracer.Trace("EXECUTE: CaptureAndProcessAsync completed");
        }
        else
        {
            Logger.LogInformation(LogCategory.Application, "ScreenshotWorker skipping capture (policy disabled or conditions not met)");
        }
    }

    private async Task CaptureAndProcessAsync(CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        string? employeeId = null;
        string? deviceId = null;

        try
        {
            Logger.LogInformation(LogCategory.Application, "ScreenshotWorker: Starting capture and process");
            ScreenshotWorkerTracer.Trace("CAPTURE: Starting capture and process");
            
            // Get policy
            var policy = await _policyEngine.GetPolicyAsync<ScreenshotPolicy>(cancellationToken);

            var identity = await _tokenStorage.RetrieveTokensAsync(cancellationToken);
            employeeId = !string.IsNullOrEmpty(identity?.EmployeeId) ? identity.EmployeeId : (_configuration["Agent:EmployeeId"] ?? "UNKNOWN");
            deviceId = !string.IsNullOrEmpty(identity?.DeviceId) ? identity.DeviceId : (_configuration["Agent:DeviceId"] ?? "UNKNOWN");
            
            ScreenshotWorkerTracer.Trace($"CAPTURE: EmployeeId={employeeId}, DeviceId={deviceId}");
            Logger.LogInformation(LogCategory.Application, "ScreenshotWorker: EmployeeId={EmployeeId}, DeviceId={DeviceId}", employeeId, deviceId);

            // Capture screenshot
            Logger.LogInformation(LogCategory.Application, "ScreenshotWorker: Capturing desktop");
            ScreenshotWorkerTracer.Trace("CAPTURE: Calling CaptureFullDesktopAsync");
            var captureStream = await _screenshotService.CaptureFullDesktopAsync(cancellationToken);
            var originalSize = captureStream.Length;
            captureStream.Position = 0;
            ScreenshotWorkerTracer.Trace($"CAPTURE: Captured {originalSize} bytes");
            Logger.LogInformation(LogCategory.Application, "ScreenshotWorker: Captured {OriginalSize} bytes", originalSize);

            // Process image (validate, resize, compress)
            Logger.LogInformation(LogCategory.Application, "ScreenshotWorker: Processing image pipeline");
            ScreenshotWorkerTracer.Trace($"CAPTURE: Calling ProcessImagePipelineAsync Format={policy.Format}, Quality={policy.Quality}");
            var processedStream = await _imageProcessingService.ProcessImagePipelineAsync(
                captureStream,
                policy.Format,
                policy.Quality,
                policy.MaxWidth,
                policy.MaxHeight,
                cancellationToken
            );
            ScreenshotWorkerTracer.Trace($"CAPTURE: Image processed, {processedStream.Length} bytes");
            Logger.LogInformation(LogCategory.Application, "ScreenshotWorker: Image processed, {ProcessedSize} bytes", processedStream.Length);

            // Generate storage path
            var captureTime = DateTime.UtcNow;
            var basePath = _storagePathHelper.GetBasePath();
            ScreenshotWorkerTracer.Trace($"CAPTURE: BasePath={basePath}");
            Logger.LogInformation(LogCategory.Application, "ScreenshotWorker: BasePath={BasePath}", basePath);
            
            var storagePath = _storagePathHelper.GetStoragePath(employeeId, captureTime);
            ScreenshotWorkerTracer.Trace($"CAPTURE: StoragePath={storagePath}");
            Logger.LogInformation(LogCategory.Application, "ScreenshotWorker: StoragePath={StoragePath}", storagePath);
            
            var fileName = _storagePathHelper.GenerateFileName(captureTime, policy.Format);
            ScreenshotWorkerTracer.Trace($"CAPTURE: FileName={fileName}");
            Logger.LogInformation(LogCategory.Application, "ScreenshotWorker: FileName={FileName}", fileName);
            
            var relativePath = Path.Combine(storagePath.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar), fileName);
            ScreenshotWorkerTracer.Trace($"CAPTURE: RelativePath={relativePath}");
            Logger.LogInformation(LogCategory.Application, "ScreenshotWorker: RelativePath={RelativePath}", relativePath);

            // Store locally
            var storageRequest = new StorageRequest
            {
                Key = relativePath,
                Content = processedStream,
                BucketName = basePath
            };

            Logger.LogInformation(LogCategory.Application, "ScreenshotWorker: Uploading to storage provider");
            ScreenshotWorkerTracer.Trace($"CAPTURE: Calling storage provider UploadAsync Key={relativePath}, Bucket={basePath}");
            var storageResponse = await _storageProvider.UploadAsync(storageRequest, cancellationToken);
            ScreenshotWorkerTracer.Trace($"CAPTURE: UploadAsync succeeded, URL={storageResponse.Url}, Size={storageResponse.SizeBytes}");
            Logger.LogInformation(LogCategory.Application, "ScreenshotWorker: Storage response URL={Url}", storageResponse.Url);
            
            processedStream.Position = 0;

            // Compute checksum for upload integrity verification
            processedStream.Position = 0;
            var checksum = await _checksumService.ComputeSha256Async(processedStream, cancellationToken);

            // Generate metadata
            ScreenshotWorkerTracer.Trace("CAPTURE: Generating metadata");
            var metadata = await _imageProcessingService.GenerateMetadataAsync(processedStream, storageResponse.Url, cancellationToken);
            ScreenshotWorkerTracer.Trace($"CAPTURE: Metadata generated Width={metadata.Width}, Height={metadata.Height}, FileSize={metadata.FileSizeBytes}");
            Logger.LogInformation(LogCategory.Application, "ScreenshotWorker: Metadata generated - Width={Width}, Height={Height}, FileSize={FileSize}", 
                metadata.Width, metadata.Height, metadata.FileSizeBytes);

            // Create screenshot record
            var screenshot = new CoreScreenshot
            {
                EmployeeId = employeeId,
                DeviceId = deviceId,
                MonitorId = "0", // TODO: Get from capture
                CaptureTimeUtc = captureTime,
                FilePath = fileName,
                StoragePath = storagePath,
                Width = metadata.Width,
                Height = metadata.Height,
                Format = policy.Format,
                Quality = policy.Quality,
                Compressed = policy.CompressionEnabled,
                FileSizeBytes = metadata.FileSizeBytes
            };

            Logger.LogInformation(LogCategory.Application, "ScreenshotWorker: Saving screenshot metadata to repository");
            ScreenshotWorkerTracer.Trace($"CAPTURE: Saving screenshot metadata ID={screenshot.Id}");
            await _screenshotRepository.SaveAsync(screenshot, cancellationToken);
            ScreenshotWorkerTracer.Trace($"CAPTURE: Metadata saved ID={screenshot.Id}");
            Logger.LogInformation(LogCategory.Application, "ScreenshotWorker: Screenshot metadata saved with ID={Id}", screenshot.Id);

            // Enqueue upload job for cloud synchronization
            var fullLocalPath = Path.Combine(basePath, relativePath);
            var uploadJob = new UploadJob
            {
                CorrelationId = screenshot.CorrelationId,
                EmployeeId = employeeId,
                DeviceId = deviceId,
                LocalFilePath = fullLocalPath,
                Checksum = checksum,
                FileSize = metadata.FileSizeBytes,
                MaxRetryCount = 5,
                Priority = 5
            };

            await _uploadWorker.EnqueueUploadAsync(uploadJob, cancellationToken);
            ScreenshotWorkerTracer.Trace($"CAPTURE: Upload job enqueued JobId={uploadJob.JobId}");
            Logger.LogInformation(LogCategory.Application, "ScreenshotWorker: Upload job enqueued for cloud sync JobId={JobId}", uploadJob.JobId);

            // Create queue job
            var job = new ScreenshotJobModel
            {
                CorrelationId = screenshot.CorrelationId,
                EmployeeId = employeeId,
                DeviceId = deviceId,
                MonitorId = "0",
                CaptureTimeUtc = captureTime,
                FilePath = fileName,
                StoragePath = storagePath,
                Compressed = policy.CompressionEnabled,
                Priority = 1,
                Status = "Pending"
            };

            Logger.LogInformation(LogCategory.Application, "ScreenshotWorker: Enqueuing job");
            ScreenshotWorkerTracer.Trace($"CAPTURE: Enqueuing job CorrelationId={job.CorrelationId}");
            await _jobQueue.EnqueueAsync(job, JobPriority.Normal, cancellationToken);
            ScreenshotWorkerTracer.Trace("CAPTURE: Job enqueued");
            Logger.LogInformation(LogCategory.Application, "ScreenshotWorker: Job enqueued");

            // Publish events
            await _eventBus.PublishAsync(new ScreenshotCaptured(
                screenshot.Id,
                employeeId,
                deviceId,
                "0",
                captureTime,
                fileName,
                metadata.FileSizeBytes
            ), cancellationToken);

            await _eventBus.PublishAsync(new ScreenshotSaved(
                screenshot.Id,
                fileName,
                storagePath,
                metadata.FileSizeBytes,
                DateTime.UtcNow
            ), cancellationToken);

            await _eventBus.PublishAsync(new CompressionCompleted(
                screenshot.Id,
                originalSize,
                metadata.FileSizeBytes,
                (double)metadata.FileSizeBytes / originalSize,
                stopwatch.ElapsedMilliseconds
            ), cancellationToken);

            await _eventBus.PublishAsync(new StorageCompleted(
                screenshot.Id,
                storagePath,
                metadata.FileSizeBytes,
                DateTime.UtcNow
            ), cancellationToken);

            stopwatch.Stop();
            ScreenshotWorkerTracer.Trace($"CAPTURE: Completed in {stopwatch.ElapsedMilliseconds}ms, saved to {storagePath}");
            Logger.LogInformation(LogCategory.Application, $"ScreenshotWorker: Capture and processing completed in {stopwatch.ElapsedMilliseconds}ms, saved to {storagePath}");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            ScreenshotWorkerTracer.Trace($"CAPTURE: EXCEPTION {ex.GetType().Name}: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}");
            Logger.LogError(LogCategory.Exception, "ScreenshotWorker: Capture and processing failed", ex);

            await _eventBus.PublishAsync(new ScreenshotFailed(
                Guid.NewGuid().ToString(),
                employeeId ?? "UNKNOWN",
                deviceId ?? "UNKNOWN",
                ex.Message,
                DateTime.UtcNow
            ), cancellationToken);
        }
    }

    protected override Task OnErrorAsync(Exception exception, CancellationToken cancellationToken)
    {
        Logger.LogError(LogCategory.Exception, "Screenshot Worker error", exception);
        return Task.CompletedTask;
    }
}
