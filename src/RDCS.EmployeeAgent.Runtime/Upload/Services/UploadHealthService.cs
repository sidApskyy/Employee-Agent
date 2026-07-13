using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Runtime.Upload.Interfaces;

namespace RDCS.EmployeeAgent.Runtime.Upload.Services;

public class UploadHealthService
{
    private readonly UploadStatisticsService _statisticsService;
    private readonly IConnectivityService _connectivityService;
    private readonly IAgentLogger _logger;

    public UploadHealthService(
        UploadStatisticsService statisticsService,
        IConnectivityService connectivityService,
        IAgentLogger logger)
    {
        _statisticsService = statisticsService;
        _connectivityService = connectivityService;
        _logger = logger;
    }

    public async Task<UploadHealthReport> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        var stats = await _statisticsService.GetAsync(cancellationToken);

        var report = new UploadHealthReport
        {
            IsOnline = _connectivityService.IsOnline,
            PendingUploads = stats.PendingCount,
            DeadLetterCount = stats.DeadLetterCount,
            SuccessRate = stats.SuccessRate,
            FailureRate = stats.FailureRate,
            AverageUploadMs = stats.AverageUploadMs,
            TotalBytesUploaded = stats.TotalBytesUploaded,
            ReportedAtUtc = DateTime.UtcNow
        };

        report.IsHealthy = _connectivityService.IsOnline
            && stats.DeadLetterCount == 0
            && stats.SuccessRate >= 90;

        if (stats.PendingCount > 100)
            _logger.LogWarning(LogCategory.Application, "UploadHealth: High pending queue ({Count})", stats.PendingCount);

        if (stats.DeadLetterCount > 0)
            _logger.LogWarning(LogCategory.Application, "UploadHealth: {Count} items in DeadLetter queue", stats.DeadLetterCount);

        return report;
    }
}

public class UploadHealthReport
{
    public bool IsHealthy { get; set; }
    public bool IsOnline { get; set; }
    public int PendingUploads { get; set; }
    public int DeadLetterCount { get; set; }
    public double SuccessRate { get; set; }
    public double FailureRate { get; set; }
    public double AverageUploadMs { get; set; }
    public long TotalBytesUploaded { get; set; }
    public DateTime ReportedAtUtc { get; set; }
}
