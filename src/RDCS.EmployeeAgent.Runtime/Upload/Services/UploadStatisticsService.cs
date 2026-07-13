using RDCS.EmployeeAgent.Runtime.Upload.Interfaces;
using RDCS.EmployeeAgent.Runtime.Upload.Models;

namespace RDCS.EmployeeAgent.Runtime.Upload.Services;

public class UploadStatisticsService : IUploadStatisticsService
{
    private readonly IUploadRepository _repository;
    private readonly IConnectivityService _connectivityService;

    public UploadStatisticsService(IUploadRepository repository, IConnectivityService connectivityService)
    {
        _repository = repository;
        _connectivityService = connectivityService;
    }

    public async Task<UploadStatistics> GetAsync(CancellationToken cancellationToken = default)
    {
        var stats = await _repository.GetStatisticsAsync(cancellationToken);
        stats.IsOnline = _connectivityService.IsOnline;
        stats.LastUpdatedUtc = DateTime.UtcNow;
        return stats;
    }

    public async Task RecordUploadAsync(bool success, long bytesUploaded, long elapsedMs, CancellationToken cancellationToken = default)
        => await _repository.UpdateDailyStatisticsAsync(success, bytesUploaded, elapsedMs, cancellationToken);
}
