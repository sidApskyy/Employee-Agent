# Phase 4: Enterprise Local Storage Infrastructure

## Overview

This document defines the Enterprise Local Storage Infrastructure for RDCS Employee Agent. This centralized storage system serves as the foundation for all modules: Screenshots, Upload Engine, Browser Monitoring, Application Monitoring, AI Analytics, Logs, Queue, Database, and Diagnostics.

### Design Principles

- **Configuration-Driven**: All paths come from configuration; no hardcoded paths
- **Centralized Path Management**: Single source of truth for all path generation
- **Automatic Folder Creation**: Missing folders are created automatically
- **Asynchronous Operations**: All I/O operations are async
- **Policy-Driven Cleanup**: Retention and cleanup based on configurable policies
- **Health Monitoring**: Continuous monitoring of storage health and statistics
- **Security**: Path validation to prevent traversal attacks
- **Performance**: Optimized for millions of files with safe directory operations

---

## 1. Storage Architecture

### 1.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     RDCS Employee Agent                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │              Configuration Layer                           │  │
│  │  • StorageSettings (settings.json)                       │  │
│  └──────────────────────────────────────────────────────────┘  │
│                            │                                    │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │              Storage Path Provider                        │  │
│  │  • Central path generation for all modules               │  │
│  │  • Path validation and security                          │  │
│  └──────────────────────────────────────────────────────────┘  │
│                            │                                    │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │              Storage Services                            │  │
│  │  • StorageInitializer                                     │  │
│  │  • StorageDirectoryManager                               │  │
│  │  • StorageHealthService                                  │  │
│  │  • StorageStatisticsService                              │  │
│  │  • StorageCleanupService                                 │  │
│  └──────────────────────────────────────────────────────────┘  │
│                            │                                    │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │              Module Consumers                            │  │
│  │  • Screenshot Module                                     │  │
│  │  • Queue System                                          │  │
│  │  • Database (SQLite)                                     │  │
│  │  • Logging (Serilog)                                     │  │
│  │  • Temp/Cache/Diagnostics                                │  │
│  └──────────────────────────────────────────────────────────┘  │
│                            │                                    │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │              Background Workers                           │  │
│  │  • StorageCleanupWorker                                  │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 Component Responsibilities

| Component | Responsibility |
|------------|----------------|
| `StorageSettings` | Configuration schema for all storage paths and policies |
| `IStorageInitializer` | Interface for initializing storage infrastructure |
| `StorageInitializer` | Creates root folder and all subfolders on startup |
| `StorageDirectoryManager` | Manages directory creation, validation, and maintenance |
| `StoragePathProvider` | Central class for generating all paths throughout the application |
| `StorageHealthService` | Monitors storage health, disk space, accessibility |
| `StorageStatisticsService` | Provides storage usage statistics per folder |
| `StorageCleanupService` | Executes cleanup operations based on policies |
| `StorageCleanupWorker` | Background worker for scheduled cleanup tasks |

---

## 2. Folder Structure

### 2.1 Root Storage Path

**Development**: `C:\RDCS Agent`

**Production**: `%ProgramData%\RDCS Agent`

The root path is configurable via `StorageSettings.RootPath`.

### 2.2 Complete Folder Hierarchy

```
C:\RDCS Agent\
│
├── Screenshots\
│   └── {Year}\
│       └── {Month}\
│           └── {Day}\
│               └── {EmployeeId}\
│                   ├── {Timestamp}.webp
│                   ├── {Timestamp}.jpg
│                   └── {Timestamp}.png
│
├── Queue\
│   ├── Pending\
│   ├── Processing\
│   ├── Failed\
│   └── Archive\
│
├── Database\
│   ├── rdcs_agent.db
│   └── rdcs_agent.db-shm
│
├── Logs\
│   ├── Application\
│   │   ├── 20260810.log
│   │   └── 20260811.log
│   ├── Error\
│   │   └── error_20260810.log
│   └── Performance\
│       └── perf_20260810.log
│
├── Temp\
│   ├── Processing\
│   │   └── {Guid}.tmp
│   └── Uploads\
│       └── {Guid}.tmp
│
├── Cache\
│   ├── Policies\
│   ├── FeatureFlags\
│   ├── Downloads\
│   └── Metadata\
│
├── Config\
│   ├── policies.json
│   └── feature_flags.json
│
├── Diagnostics\
│   ├── CrashDumps\
│   ├── PerformanceReports\
│   └── HealthReports\
│
└── Backups\
    ├── Database\
    │   └── rdcs_agent_20260810.db
    └── Config\
        └── policies_20260810.json
```

### 2.3 Folder Creation Rules

- **Automatic Creation**: All folders are created automatically on startup
- **Validation**: Folder existence is validated before use
- **Safe Creation**: Uses `Directory.CreateDirectory` which is idempotent
- **No Duplicate Checks**: Avoids redundant directory existence checks

---

## 3. Class Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                     StorageSettings                             │
├─────────────────────────────────────────────────────────────────┤
│ + RootPath: string                                               │
│ + ScreenshotsPath: string                                       │
│ + QueuePath: string                                             │
│ + LogsPath: string                                              │
│ + DatabasePath: string                                          │
│ + TempPath: string                                               │
│ + CachePath: string                                             │
│ + ConfigPath: string                                            │
│ + DiagnosticsPath: string                                      │
│ + BackupPath: string                                            │
│ + MaxLogFileSizeMB: int                                         │
│ + LogRetentionDays: int                                          │
│ + TempCleanupIntervalHours: int                                 │
│ + CacheRetentionDays: int                                       │
│ + BackupRetentionDays: int                                      │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │
┌─────────────────────────────────────────────────────────────────┐
│                IStorageInitializer                               │
├─────────────────────────────────────────────────────────────────┤
│ + InitializeAsync(): Task<bool>                                  │
│ + ValidateStorageAsync(): Task<bool>                             │
└─────────────────────────────────────────────────────────────────┘
                              ▲
                              │
┌─────────────────────────────────────────────────────────────────┐
│                  StorageInitializer                              │
├─────────────────────────────────────────────────────────────────┤
│ - _settings: StorageSettings                                    │
│ - _directoryManager: StorageDirectoryManager                    │
│ - _logger: IAgentLogger                                          │
│ + InitializeAsync(): Task<bool>                                  │
│ + ValidateStorageAsync(): Task<bool>                             │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │
┌─────────────────────────────────────────────────────────────────┐
│              StorageDirectoryManager                            │
├─────────────────────────────────────────────────────────────────┤
│ - _settings: StorageSettings                                    │
│ - _logger: IAgentLogger                                          │
│ + EnsureDirectoryExistsAsync(path): Task                        │
│ + CreateDirectoryAsync(path): Task                              │
│ + ValidatePathAsync(path): Task<bool>                           │
│ + GetDirectorySizeAsync(path): Task<long>                       │
│ + GetFileCountAsync(path): Task<int>                            │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │
┌─────────────────────────────────────────────────────────────────┐
│              StoragePathProvider                                │
├─────────────────────────────────────────────────────────────────┤
│ - _settings: StorageSettings                                    │
│ + GetRootPath(): string                                          │
│ + GetScreenshotFolder(): string                                 │
│ + GetEmployeeScreenshotFolder(employeeId, date): string          │
│ + GetQueueFolder(): string                                      │
│ + GetQueuePendingFolder(): string                               │
│ + GetQueueProcessingFolder(): string                            │
│ + GetQueueFailedFolder(): string                                │
│ + GetQueueArchiveFolder(): string                               │
│ + GetDatabaseFolder(): string                                   │
│ + GetDatabasePath(): string                                     │
│ + GetLogFolder(): string                                        │
│ + GetTempFolder(): string                                       │
│ + GetCacheFolder(): string                                      │
│ + GetConfigFolder(): string                                     │
│ + GetDiagnosticsFolder(): string                                │
│ + GetBackupFolder(): string                                    │
│ + CombinePath(params): string                                   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │
┌─────────────────────────────────────────────────────────────────┐
│              StorageHealthService                               │
├─────────────────────────────────────────────────────────────────┤
│ - _settings: StorageSettings                                    │
│ - _directoryManager: StorageDirectoryManager                    │
│ - _logger: IAgentLogger                                          │
│ + GetHealthStatusAsync(): Task<StorageHealthStatus>              │
│ + CheckRootExistsAsync(): Task<bool>                             │
│ + GetFolderCountAsync(): Task<int>                               │
│ + GetTotalStorageUsedAsync(): Task<long>                         │
│ + GetFreeDiskSpaceAsync(): Task<long>                            │
│ + GetAvailableSpaceAsync(): Task<long>                           │
│ + IsStorageAccessibleAsync(): Task<bool>                         │
│ + GetLastCleanupTimeAsync(): Task<DateTime?>                    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │
┌─────────────────────────────────────────────────────────────────┐
│            StorageStatisticsService                             │
├─────────────────────────────────────────────────────────────────┤
│ - _directoryManager: StorageDirectoryManager                    │
│ - _logger: IAgentLogger                                          │
│ + GetScreenshotStatisticsAsync(): Task<FolderStatistics>         │
│ + GetQueueStatisticsAsync(): Task<FolderStatistics>             │
│ + GetDatabaseStatisticsAsync(): Task<FolderStatistics>          │
│ + GetLogStatisticsAsync(): Task<FolderStatistics>               │
│ + GetTempStatisticsAsync(): Task<FolderStatistics>              │
│ + GetCacheStatisticsAsync(): Task<FolderStatistics>             │
│ + GetOverallStatisticsAsync(): Task<OverallStatistics>          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │
┌─────────────────────────────────────────────────────────────────┐
│              StorageCleanupService                              │
├─────────────────────────────────────────────────────────────────┤
│ - _settings: StorageSettings                                    │
│ - _directoryManager: StorageDirectoryManager                    │
│ - _logger: IAgentLogger                                          │
│ + CleanupTempFilesAsync(): Task<CleanupResult>                   │
│ + CleanupCacheAsync(): Task<CleanupResult>                      │
│ + CleanupExpiredBackupsAsync(): Task<CleanupResult>             │
│ + CleanupOldLogsAsync(): Task<CleanupResult>                     │
│ + CleanupAbandonedTempFilesAsync(): Task<CleanupResult>         │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │
┌─────────────────────────────────────────────────────────────────┐
│              StorageCleanupWorker (BackgroundWorkerBase)         │
├─────────────────────────────────────────────────────────────────┤
│ - _cleanupService: StorageCleanupService                        │
│ - _healthService: StorageHealthService                         │
│ + ExecuteAsync(cancellationToken): Task                          │
└─────────────────────────────────────────────────────────────────┘
```

---

## 4. Configuration Schema

### 4.1 StorageSettings (settings.json)

```json
{
  "StorageSettings": {
    "RootPath": "C:\\RDCS Agent",
    "ScreenshotsPath": "Screenshots",
    "QueuePath": "Queue",
    "LogsPath": "Logs",
    "DatabasePath": "Database",
    "TempPath": "Temp",
    "CachePath": "Cache",
    "ConfigPath": "Config",
    "DiagnosticsPath": "Diagnostics",
    "BackupPath": "Backups",
    "MaxLogFileSizeMB": 10,
    "LogRetentionDays": 30,
    "TempCleanupIntervalHours": 1,
    "CacheRetentionDays": 7,
    "BackupRetentionDays": 30,
    "TempFileMaxAgeHours": 24,
    "QueueArchiveRetentionDays": 90
  }
}
```

### 4.2 Configuration Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `RootPath` | string | `C:\RDCS Agent` | Root storage path (dev) or `%ProgramData%\RDCS Agent` (prod) |
| `ScreenshotsPath` | string | `Screenshots` | Relative path to screenshots folder |
| `QueuePath` | string | `Queue` | Relative path to queue folder |
| `LogsPath` | string | `Logs` | Relative path to logs folder |
| `DatabasePath` | string | `Database` | Relative path to database folder |
| `TempPath` | string | `Temp` | Relative path to temporary files folder |
| `CachePath` | string | `Cache` | Relative path to cache folder |
| `ConfigPath` | string | `Config` | Relative path to configuration folder |
| `DiagnosticsPath` | string | `Diagnostics` | Relative path to diagnostics folder |
| `BackupPath` | string | `Backups` | Relative path to backups folder |
| `MaxLogFileSizeMB` | int | 10 | Maximum size of a single log file in MB |
| `LogRetentionDays` | int | 30 | Number of days to retain log files |
| `TempCleanupIntervalHours` | int | 1 | Interval for temp file cleanup in hours |
| `CacheRetentionDays` | int | 7 | Number of days to retain cache files |
| `BackupRetentionDays` | int | 30 | Number of days to retain backups |
| `TempFileMaxAgeHours` | int | 24 | Maximum age of temp files before cleanup |
| `QueueArchiveRetentionDays` | int | 90 | Number of days to retain archived queue items |

---

## 5. Dependency Injection Changes

### 5.1 Service Registration

```csharp
// In Program.cs or ServiceCollectionExtensions.cs

// Configuration
services.Configure<StorageSettings>(
    configuration.GetSection("StorageSettings"));

// Storage Services
services.AddSingleton<IStorageInitializer, StorageInitializer>();
services.AddSingleton<StorageDirectoryManager>();
services.AddSingleton<StoragePathProvider>();
services.AddSingleton<StorageHealthService>();
services.AddSingleton<StorageStatisticsService>();
services.AddSingleton<StorageCleanupService>();

// Background Workers
services.AddHostedService<StorageCleanupWorker>();
```

### 5.2 Service Lifetimes

| Service | Lifetime | Reason |
|---------|----------|--------|
| `StorageSettings` | Singleton | Configuration is read once |
| `StorageInitializer` | Singleton | Runs once on startup |
| `StorageDirectoryManager` | Singleton | Stateless utility |
| `StoragePathProvider` | Singleton | Stateless utility |
| `StorageHealthService` | Singleton | Stateless utility |
| `StorageStatisticsService` | Singleton | Stateless utility |
| `StorageCleanupService` | Singleton | Stateless utility |
| `StorageCleanupWorker` | Singleton | Background worker |

---

## 6. Service Responsibilities

### 6.1 StorageSettings

**Responsibility**: Configuration schema for all storage paths and policies.

**Key Methods**: None (POCO class)

**Usage**: Injected via `IOptions<StorageSettings>` into all storage services.

### 6.2 IStorageInitializer / StorageInitializer

**Responsibility**: Initializes the storage infrastructure on application startup.

**Key Methods**:
- `InitializeAsync()`: Creates root folder and all subfolders
- `ValidateStorageAsync()`: Validates that all required folders exist and are accessible

**Usage**: Called during application startup before any module initialization.

### 6.3 StorageDirectoryManager

**Responsibility**: Manages directory creation, validation, and maintenance operations.

**Key Methods**:
- `EnsureDirectoryExistsAsync(path)`: Ensures a directory exists, creates if missing
- `CreateDirectoryAsync(path)`: Creates a directory safely
- `ValidatePathAsync(path)`: Validates a path for security (prevents traversal)
- `GetDirectorySizeAsync(path)`: Calculates total size of a directory
- `GetFileCountAsync(path)`: Counts files in a directory

**Usage**: Used by StorageInitializer, StorageHealthService, and StorageCleanupService.

### 6.4 StoragePathProvider

**Responsibility**: Central class for generating all paths throughout the application.

**Key Methods**:
- `GetRootPath()`: Returns the root storage path
- `GetScreenshotFolder()`: Returns base screenshots folder
- `GetEmployeeScreenshotFolder(employeeId, date)`: Returns employee-specific screenshot folder with date hierarchy
- `GetQueueFolder()`: Returns base queue folder
- `GetQueuePendingFolder()`: Returns queue pending folder
- `GetQueueProcessingFolder()`: Returns queue processing folder
- `GetQueueFailedFolder()`: Returns queue failed folder
- `GetQueueArchiveFolder()`: Returns queue archive folder
- `GetDatabaseFolder()`: Returns database folder
- `GetDatabasePath()`: Returns full path to SQLite database
- `GetLogFolder()`: Returns logs folder
- `GetTempFolder()`: Returns temp folder
- `GetCacheFolder()`: Returns cache folder
- `GetConfigFolder()`: Returns config folder
- `GetDiagnosticsFolder()`: Returns diagnostics folder
- `GetBackupFolder()`: Returns backup folder
- `CombinePath(params)`: Safely combines path segments

**Usage**: Injected into all modules (Screenshot, Queue, Database, Logging, etc.) to replace hardcoded paths.

### 6.5 StorageHealthService

**Responsibility**: Monitors storage health, disk space, and accessibility.

**Key Methods**:
- `GetHealthStatusAsync()`: Returns comprehensive health status
- `CheckRootExistsAsync()`: Checks if root path exists
- `GetFolderCountAsync()`: Returns total number of folders
- `GetTotalStorageUsedAsync()`: Returns total storage used by all folders
- `GetFreeDiskSpaceAsync()`: Returns free disk space on the drive
- `GetAvailableSpaceAsync()`: Returns available space considering configured limits
- `IsStorageAccessibleAsync()`: Checks if storage is read/write accessible
- `GetLastCleanupTimeAsync()`: Returns timestamp of last cleanup

**Usage**: Used by health monitoring system and dashboard.

### 6.6 StorageStatisticsService

**Responsibility**: Provides storage usage statistics per folder.

**Key Methods**:
- `GetScreenshotStatisticsAsync()`: Returns screenshot folder statistics
- `GetQueueStatisticsAsync()`: Returns queue folder statistics
- `GetDatabaseStatisticsAsync()`: Returns database folder statistics
- `GetLogStatisticsAsync()`: Returns log folder statistics
- `GetTempStatisticsAsync()`: Returns temp folder statistics
- `GetCacheStatisticsAsync()`: Returns cache folder statistics
- `GetOverallStatisticsAsync()`: Returns overall storage statistics

**Usage**: Used by analytics and reporting.

### 6.7 StorageCleanupService

**Responsibility**: Executes cleanup operations based on policies.

**Key Methods**:
- `CleanupTempFilesAsync()`: Cleans up temp files older than policy
- `CleanupCacheAsync()`: Cleans up cache files older than policy
- `CleanupExpiredBackupsAsync()`: Cleans up backups older than policy
- `CleanupOldLogsAsync()`: Cleans up log files older than policy
- `CleanupAbandonedTempFilesAsync()`: Cleans up temp files with no active references

**Usage**: Used by StorageCleanupWorker and manual cleanup operations.

### 6.8 StorageCleanupWorker

**Responsibility**: Background worker for scheduled cleanup tasks.

**Key Methods**:
- `ExecuteAsync(cancellationToken)`: Main background loop

**Usage**: Runs as a hosted service, executes cleanup based on configured intervals.

---

## 7. Storage Initialization Flow

### 7.1 Startup Sequence

```
Application Startup
        │
        ▼
Load Configuration (settings.json)
        │
        ▼
Register Services (DI)
        │
        ▼
┌─────────────────────────────────────┐
│ StorageInitializer.InitializeAsync() │
├─────────────────────────────────────┤
│ 1. Validate RootPath                │
│ 2. Create Root Folder               │
│ 3. Create Screenshots Folder        │
│ 4. Create Queue Folder              │
│    - Pending                        │
│    - Processing                     │
│    - Failed                         │
│    - Archive                        │
│ 5. Create Database Folder           │
│ 6. Create Logs Folder               │
│ 7. Create Temp Folder               │
│ 8. Create Cache Folder              │
│ 9. Create Config Folder             │
│ 10. Create Diagnostics Folder       │
│ 11. Create Backups Folder           │
│ 12. Validate All Folders            │
│ 13. Log Initialization Success      │
└─────────────────────────────────────┘
        │
        ▼
Initialize Other Modules
(Screenshot, Queue, Database, etc.)
        │
        ▼
Start Background Workers
        │
        ▼
Application Ready
```

### 7.2 Error Handling

- If root path cannot be created: Application fails to start
- If subfolder cannot be created: Log warning, continue
- If validation fails: Log error, attempt repair
- All operations are async with cancellation token support

---

## 8. Cleanup Flow

### 8.1 Scheduled Cleanup

```
StorageCleanupWorker (Background)
        │
        ▼
Wait for Interval (Configurable)
        │
        ▼
┌─────────────────────────────────────┐
│ StorageCleanupService               │
├─────────────────────────────────────┤
│ 1. Cleanup Temp Files               │
│    - Files older than TempFileMaxAgeHours │
│ 2. Cleanup Cache                    │
│    - Files older than CacheRetentionDays  │
│ 3. Cleanup Expired Backups          │
│    - Files older than BackupRetentionDays │
│ 4. Cleanup Old Logs                 │
│    - Files older than LogRetentionDays    │
│ 5. Cleanup Abandoned Temp Files     │
│    - Temp files with no active references  │
│ 6. Update Last Cleanup Time         │
│ 7. Publish Cleanup Event            │
└─────────────────────────────────────┘
        │
        ▼
Log Cleanup Results
        │
        ▼
Wait for Next Interval
```

### 8.2 Manual Cleanup

Modules can trigger manual cleanup by calling `StorageCleanupService` methods directly.

### 8.3 Crash Recovery

On startup, `StorageCleanupService.CleanupAbandonedTempFilesAsync()` is called to clean up temp files from previous crashes.

---

## 9. Health Monitoring Flow

### 9.1 Health Check Sequence

```
Health Check Request
        │
        ▼
┌─────────────────────────────────────┐
│ StorageHealthService               │
├─────────────────────────────────────┤
│ 1. Check Root Exists               │
│ 2. Get Folder Count                 │
│ 3. Get Total Storage Used           │
│ 4. Get Free Disk Space              │
│ 5. Get Available Space              │
│ 6. Check Storage Accessible         │
│ 7. Get Last Cleanup Time            │
│ 8. Compile Health Status           │
└─────────────────────────────────────┘
        │
        ▼
Return StorageHealthStatus
        │
        ▼
Dashboard / Alerting
```

### 9.2 Health Status Model

```csharp
public class StorageHealthStatus
{
    public bool IsHealthy { get; set; }
    public bool RootExists { get; set; }
    public int FolderCount { get; set; }
    public long TotalStorageUsedBytes { get; set; }
    public long FreeDiskSpaceBytes { get; set; }
    public long AvailableSpaceBytes { get; set; }
    public bool IsStorageAccessible { get; set; }
    public DateTime? LastCleanupTime { get; set; }
    public List<string> Warnings { get; set; }
    public List<string> Errors { get; set; }
}
```

---

## 10. Testing Strategy

### 10.1 Unit Tests

| Test Class | Test Cases |
|------------|------------|
| `StorageInitializerTests` | - Initialize creates all folders<br>- Validate returns true for valid storage<br>- Validate returns false for invalid storage |
| `StoragePathProviderTests` | - GetRootPath returns configured path<br>- GetScreenshotFolder returns correct path<br>- GetEmployeeScreenshotFolder generates date hierarchy<br>- GetQueueFolder returns correct path<br>- GetDatabasePath returns correct path<br>- CombinePath safely combines segments |
| `StorageDirectoryManagerTests` | - EnsureDirectoryExists creates missing folder<br>- ValidatePath rejects traversal attacks<br>- GetDirectorySize calculates correctly<br>- GetFileCount counts correctly |
| `StorageCleanupServiceTests` | - CleanupTempFiles removes old files<br>- CleanupCache removes old files<br>- CleanupExpiredBackups removes old backups<br>- CleanupAbandonedTempFiles removes orphaned files |
| `StorageHealthServiceTests` | - GetHealthStatus returns correct status<br>- CheckRootExists returns correct result<br>- GetFreeDiskSpace returns correct value<br>- IsStorageAccessible returns correct result |

### 10.2 Integration Tests

| Test Class | Test Cases |
|------------|------------|
| `StorageInitializationTests` | - Full initialization creates all folders<br>- Initialization handles existing folders<br>- Initialization handles permission errors |
| `StorageCleanupIntegrationTests` | - End-to-end cleanup removes old files<br>- Cleanup preserves recent files<br>- Cleanup handles locked files |
| `StoragePathIntegrationTests` | - All modules can access paths via provider<br>- Path generation is consistent |

### 10.3 Performance Tests

- Test with millions of files
- Measure directory creation time
- Measure cleanup performance
- Measure health check performance

---

## 11. Security Considerations

### 11.1 Path Validation

- All paths are validated before use
- Prevent path traversal attacks (`../`)
- Prevent invalid folder names
- Use `Path.Combine` everywhere
- Never concatenate strings for paths

### 11.2 Access Control

- Root folder should have appropriate permissions
- Database folder should be restricted
- Logs folder should be readable by support
- Temp folder should be cleaned regularly

### 11.3 Error Handling

- Never expose full paths in error messages
- Log sanitized paths
- Handle permission errors gracefully

---

## 12. Performance Considerations

### 12.1 Directory Operations

- Use `Directory.CreateDirectory` (idempotent)
- Avoid duplicate existence checks
- Batch operations where possible
- Use async I/O throughout

### 12.2 File Operations

- Use streams for large files
- Implement proper disposal
- Use async file operations

### 12.3 Cleanup Performance

- Process folders in parallel where safe
- Limit concurrent file operations
- Implement progress reporting

---

## 13. Migration Strategy

### 13.1 From Phase 3

The Screenshot Module currently uses hardcoded paths. Migration steps:

1. Update `StoragePathHelper` to use `StoragePathProvider`
2. Update `LocalStorageProvider` to use `StoragePathProvider`
3. Update `DatabaseInitializer` to use `StoragePathProvider.GetDatabasePath()`
4. Update Queue system to use `StoragePathProvider.GetQueue*Folder()`
5. Update Serilog configuration to use `StoragePathProvider.GetLogFolder()`

### 13.2 Backward Compatibility

- Existing folders will be detected and used
- No data migration required (same folder structure)
- Configuration can override paths if needed

---

## 14. Future Enhancements

### 14.1 Cloud Storage Integration

- `IStorageProvider` abstraction already exists
- Future S3/Azure Blob integration
- Hybrid local/cloud storage

### 14.2 Compression

- Automatic compression of old logs
- Compression of archived queue items
- Compression of backups

### 14.3 Encryption

- Encrypt sensitive data at rest
- Key management integration

### 14.4 Distributed Storage

- Support for network shares
- Support for cloud storage
- Sync between local and cloud

---

## 15. Implementation Checklist

- [ ] Create `StorageSettings` configuration class
- [ ] Create `IStorageInitializer` interface
- [ ] Create `StorageInitializer` implementation
- [ ] Create `StorageDirectoryManager` class
- [ ] Create `StoragePathProvider` class
- [ ] Create `StorageHealthService` class
- [ ] Create `StorageStatisticsService` class
- [ ] Create `StorageCleanupService` class
- [ ] Create `StorageCleanupWorker` class
- [ ] Configure Serilog with rotation and retention
- [ ] Update Dependency Injection registration
- [ ] Update Screenshot Module to use `StoragePathProvider`
- [ ] Update Queue System to use `StoragePathProvider`
- [ ] Update Database path to use `StoragePathProvider`
- [ ] Generate unit tests
- [ ] Generate integration tests
- [ ] Update architecture document with any changes

---

## 16. Dependencies

### 16.1 External Dependencies

- `System.IO` - File and directory operations
- `Microsoft.Extensions.Options` - Configuration
- `Microsoft.Extensions.Hosting` - Background workers
- `Serilog` - Logging (existing)

### 16.2 Internal Dependencies

- `RDCS.EmployeeAgent.Core.Interfaces.IAgentLogger` - Logging
- `RDCS.EmployeeAgent.Runtime.Workers.BackgroundWorkerBase` - Worker base class

---

## 17. Deployment Considerations

### 17.1 Development

- Root path: `C:\RDCS Agent`
- All folders created automatically
- No manual setup required

### 17.2 Production

- Root path: `%ProgramData%\RDCS Agent`
- Service account must have write permissions
- Disk space monitoring required
- Backup strategy required

### 17.3 Configuration

- Update `settings.json` with production paths
- Adjust retention policies based on requirements
- Configure Serilog for production

---

## 18. Monitoring and Alerting

### 18.1 Metrics to Monitor

- Total storage used
- Free disk space
- Folder sizes
- Cleanup frequency
- Cleanup success rate
- Storage accessibility

### 18.2 Alerts

- Disk space below threshold
- Storage not accessible
- Cleanup failures
- Folder creation failures

---

## 19. Documentation

### 19.1 Developer Documentation

- How to use `StoragePathProvider`
- How to add new folders
- How to configure cleanup policies
- How to monitor storage health

### 19.2 Operations Documentation

- Folder structure overview
- Backup procedures
- Recovery procedures
- Troubleshooting guide

---

## 20. Summary

This Enterprise Local Storage Infrastructure provides:

- **Centralized Path Management**: Single source of truth for all paths
- **Automatic Folder Creation**: No manual setup required
- **Policy-Driven Cleanup**: Configurable retention policies
- **Health Monitoring**: Continuous monitoring of storage health
- **Security**: Path validation and safe operations
- **Performance**: Optimized for millions of files
- **Extensibility**: Easy to add new folders and services
- **Testing**: Comprehensive unit and integration tests

All modules will use this infrastructure, ensuring consistency, maintainability, and reliability across the entire RDCS Employee Agent platform.
