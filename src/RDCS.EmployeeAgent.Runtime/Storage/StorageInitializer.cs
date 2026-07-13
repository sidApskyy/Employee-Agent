using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using Microsoft.Extensions.Options;

namespace RDCS.EmployeeAgent.Runtime.Storage;

public class StorageInitializer : IStorageInitializer
{
    private readonly StorageSettings _settings;
    private readonly StorageDirectoryManager _directoryManager;
    private readonly IAgentLogger _logger;

    public StorageInitializer(
        IOptions<StorageSettings> settings,
        StorageDirectoryManager directoryManager,
        IAgentLogger logger)
    {
        _settings = settings.Value;
        _directoryManager = directoryManager;
        _logger = logger;
    }

    public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(LogCategory.Application, "Starting storage initialization");

            // Validate root path
            if (!await _directoryManager.ValidatePathAsync(_settings.RootPath, cancellationToken))
            {
                _logger.LogError(LogCategory.Application, "Root path validation failed");
                return false;
            }

            // Create root folder
            await _directoryManager.EnsureDirectoryExistsAsync(_settings.RootPath, cancellationToken);

            // Create all subfolders
            var folders = new[]
            {
                Path.Combine(_settings.RootPath, _settings.ScreenshotsPath),
                Path.Combine(_settings.RootPath, _settings.QueuePath, "Pending"),
                Path.Combine(_settings.RootPath, _settings.QueuePath, "Processing"),
                Path.Combine(_settings.RootPath, _settings.QueuePath, "Failed"),
                Path.Combine(_settings.RootPath, _settings.QueuePath, "Archive"),
                Path.Combine(_settings.RootPath, _settings.DatabasePath),
                Path.Combine(_settings.RootPath, _settings.LogsPath, "Application"),
                Path.Combine(_settings.RootPath, _settings.LogsPath, "Error"),
                Path.Combine(_settings.RootPath, _settings.LogsPath, "Performance"),
                Path.Combine(_settings.RootPath, _settings.TempPath, "Processing"),
                Path.Combine(_settings.RootPath, _settings.TempPath, "Uploads"),
                Path.Combine(_settings.RootPath, _settings.CachePath, "Policies"),
                Path.Combine(_settings.RootPath, _settings.CachePath, "FeatureFlags"),
                Path.Combine(_settings.RootPath, _settings.CachePath, "Downloads"),
                Path.Combine(_settings.RootPath, _settings.CachePath, "Metadata"),
                Path.Combine(_settings.RootPath, _settings.ConfigPath),
                Path.Combine(_settings.RootPath, _settings.DiagnosticsPath, "CrashDumps"),
                Path.Combine(_settings.RootPath, _settings.DiagnosticsPath, "PerformanceReports"),
                Path.Combine(_settings.RootPath, _settings.DiagnosticsPath, "HealthReports"),
                Path.Combine(_settings.RootPath, _settings.BackupPath, "Database"),
                Path.Combine(_settings.RootPath, _settings.BackupPath, "Config")
            };

            foreach (var folder in folders)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _directoryManager.EnsureDirectoryExistsAsync(folder, cancellationToken);
            }

            // Validate all folders
            var validationSuccess = await ValidateStorageAsync(cancellationToken);

            if (validationSuccess)
            {
                _logger.LogInformation(LogCategory.Application, "Storage initialization completed successfully");
            }
            else
            {
                _logger.LogWarning(LogCategory.Application, "Storage initialization completed with validation warnings");
            }

            return validationSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "Storage initialization failed", ex);
            return false;
        }
    }

    public async Task<bool> ValidateStorageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var folders = new[]
            {
                _settings.RootPath,
                Path.Combine(_settings.RootPath, _settings.ScreenshotsPath),
                Path.Combine(_settings.RootPath, _settings.QueuePath),
                Path.Combine(_settings.RootPath, _settings.DatabasePath),
                Path.Combine(_settings.RootPath, _settings.LogsPath),
                Path.Combine(_settings.RootPath, _settings.TempPath),
                Path.Combine(_settings.RootPath, _settings.CachePath),
                Path.Combine(_settings.RootPath, _settings.ConfigPath),
                Path.Combine(_settings.RootPath, _settings.DiagnosticsPath),
                Path.Combine(_settings.RootPath, _settings.BackupPath)
            };

            var allValid = true;

            foreach (var folder in folders)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!Directory.Exists(folder))
                {
                    _logger.LogWarning(LogCategory.Application, $"Folder does not exist: {folder}");
                    allValid = false;
                }
            }

            return allValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "Storage validation failed", ex);
            return false;
        }
    }
}
