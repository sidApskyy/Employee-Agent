# Phase 3 Architecture: Enterprise Screenshot Capture Module (Local Storage)

## Overview

The Screenshot Capture Module is a critical component of the RDCS Employee Agent that enables enterprise-wide desktop monitoring through automated screenshot capture. This module integrates seamlessly with the Phase 2 infrastructure (Worker Framework, Scheduler, Policy Engine, Queue System, Event Bus, SQLite, Health Monitor, Storage Provider) to provide a scalable, policy-driven, and performant screenshot capture system using **LOCAL STORAGE ONLY**.

### Key Design Principles

- **Policy-Driven**: All configuration comes from the Policy Engine - no hardcoded values
- **Asynchronous**: All operations are async with cancellation token support
- **Scalable**: Designed for 10,000+ employees with millions of screenshots
- **Performance-Optimized**: Low CPU/RAM usage, no UI freezes
- **Multi-Monitor Support**: Primary, secondary, virtual desktops, high DPI (125%, 150%, 175%, 200%), different resolutions, portrait/landscape
- **Pipeline Architecture**: Capture → Validate → Resize → Compress → Generate Metadata → Store Locally → Create Queue Job → Publish Event
- **Event-Driven**: Publishes events for all state changes
- **Health-Monitored**: Comprehensive health metrics and reporting
- **Storage-Agnostic**: LocalStorageProvider implements IStorageProvider interface for future Amazon S3 migration
- **Auto-Cleanup**: Background worker for retention policy enforcement

---

## Folder Structure

```
src/
├── RDCS.EmployeeAgent.Runtime/
│   ├── Screenshot/
│   │   ├── Models/
│   │   │   ├── Screenshot.cs
│   │   │   ├── ScreenshotJob.cs
│   │   │   ├── CompressionStats.cs
│   │   │   ├── CaptureHistory.cs
│   │   │   ├── StorageStatistics.cs
│   │   │   └── ScreenshotMetadata.cs
│   │   ├── Services/
│   │   │   ├── IScreenshotService.cs
│   │   │   ├── ScreenshotService.cs
│   │   │   ├── IImageProcessingService.cs
│   │   │   ├── ImageProcessingService.cs
│   │   │   ├── ICompressionProvider.cs
│   │   │   ├── JpegCompressionProvider.cs
│   │   │   ├── PngCompressionProvider.cs
│   │   │   └── WebpCompressionProvider.cs
│   │   ├── Storage/
│   │   │   ├── LocalStorageProvider.cs
│   │   │   └── StoragePathHelper.cs
│   │   ├── Workers/
│   │   │   ├── IScreenshotWorker.cs
│   │   │   ├── ScreenshotWorker.cs
│   │   │   └── AutoCleanupWorker.cs
│   │   ├── Events/
│   │   │   ├── ScreenshotCaptured.cs
│   │   │   ├── ScreenshotSaved.cs
│   │   │   ├── ScreenshotFailed.cs
│   │   │   ├── CompressionCompleted.cs
│   │   │   ├── StorageCompleted.cs
│   │   │   ├── QueueJobCreated.cs
│   │   │   ├── CaptureSkipped.cs
│   │   │   └── CleanupCompleted.cs
│   │   ├── Health/
│   │   │   ├── IScreenshotHealthMonitor.cs
│   │   │   └── ScreenshotHealthMonitor.cs
│   │   └── Policies/
│   │       └── ScreenshotPolicy.cs
│   └── EventBus/Events/
│       └── [Existing events...]
│
├── RDCS.EmployeeAgent.Persistence/
│   ├── Repositories/
│   │   ├── IScreenshotRepository.cs
│   │   ├── ScreenshotRepository.cs
│   │   ├── IScreenshotJobRepository.cs
│   │   └── ScreenshotJobRepository.cs
│   └── SQLite/
│       └── DatabaseInitializer.cs
│           └── [Add screenshot tables]
│
└── backend/
    ├── prisma/
    │   └── schema.prisma
    │       └── [Add Screenshot model]
    ├── src/
    │   ├── controllers/
    │   │   └── screenshot.controller.ts
    │   ├── routes/
    │   │   └── screenshot.routes.ts
    │   ├── services/
    │   │   └── screenshot.service.ts
    │   ├── validators/
    │   │   └── screenshot.validator.ts
    │   └── dto/
    │       └── screenshot.dto.ts
    └── crm/
        └── src/
            ├── components/
            │   ├── ScreenshotTimeline.tsx
            │   ├── ImageViewer.tsx
            │   └── ScreenshotFilters.tsx
            └── pages/
                └── EmployeeScreenshots.tsx
```

---

## Class Responsibilities

### Core Models

#### `Screenshot`
- **Responsibility**: Represents a captured screenshot with metadata
- **Properties**: Id, EmployeeId, DeviceId, MonitorId, CaptureTime, FilePath, Width, Height, Format, Quality, Compressed, Encrypted, FileSizeBytes, CorrelationId
- **Lifetime**: Created after capture, stored in SQLite, queued for upload

#### `ScreenshotJob`
- **Responsibility**: Queue job for screenshot upload to backend
- **Properties**: JobId, CorrelationId, EmployeeId, DeviceId, MonitorId, CaptureTime, FilePath, Encrypted, Compressed, RetryCount, Priority, Status, CreatedAt, StartedAt, CompletedAt, Error
- **Lifetime**: Created after image processing, processed by Queue Worker

#### `CompressionStats`
- **Responsibility**: Tracks compression performance metrics
- **Properties**: Id, ScreenshotId, OriginalSizeBytes, CompressedSizeBytes, CompressionRatio, CompressionDurationMs, Algorithm, Timestamp
- **Lifetime**: Created after compression, stored for analytics

#### `CaptureHistory`
- **Responsibility**: Historical record of capture attempts
- **Properties**: Id, EmployeeId, DeviceId, CaptureTime, Success, FailureReason, DurationMs, MonitorCount, TotalSizeBytes
- **Lifetime**: Created after each capture attempt, stored for analytics

#### `ScreenshotMetadata`
- **Responsibility**: Lightweight metadata for UI display
- **Properties**: Id, EmployeeId, DeviceId, CaptureTime, ThumbnailPath, Status, UploadStatus
- **Lifetime**: Created after capture, used for CRM display

#### `StorageStatistics`
- **Responsibility**: Tracks local storage usage metrics
- **Properties**: Id, TotalScreenshots, TotalSizeBytes, OldestScreenshotDate, NewestScreenshotDate, AverageSizeBytes, LastCleanupTimeUtc
- **Lifetime**: Updated on each capture and cleanup

### Services

#### `IScreenshotService` / `ScreenshotService`
- **Responsibility**: Captures screenshots from desktop
- **Methods**:
  - `CaptureFullDesktopAsync(cancellationToken)` - Captures all monitors
  - `CaptureMonitorAsync(monitorId, cancellationToken)` - Captures specific monitor
  - `GetMonitorInfoAsync()` - Returns monitor configuration
  - `GetDesktopBoundsAsync()` - Returns desktop bounds
- **Dependencies**: IAgentLogger, IPolicyEngine
- **Integration**: Uses Windows API for capture, respects policy settings

#### `IImageProcessingService` / `ImageProcessingService`
- **Responsibility**: Processes captured images (validate, resize, compress, generate metadata)
- **Methods**:
  - `ValidateImageAsync(imageStream, cancellationToken)` - Validates image integrity
  - `ResizeImageAsync(imageStream, targetWidth, targetHeight, cancellationToken)` - Resizes image
  - `CompressImageAsync(imageStream, format, quality, cancellationToken)` - Compresses image
  - `GenerateMetadataAsync(imageStream, filePath, cancellationToken)` - Generates screenshot metadata
  - `ProcessImagePipelineAsync(imageStream, cancellationToken)` - Full pipeline
- **Dependencies**: ICompressionProvider, IAgentLogger
- **Integration**: Uses compression provider, publishes events

#### `ICompressionProvider` (Interface)
- **Responsibility**: Abstract compression interface
- **Methods**:
  - `CompressAsync(stream, quality, cancellationToken)` - Compresses image
  - `GetSupportedFormats()` - Returns supported formats
- **Implementations**: JpegCompressionProvider, PngCompressionProvider, WebpCompressionProvider

### Storage

#### `LocalStorageProvider` (Implements IStorageProvider)
- **Responsibility**: Stores screenshots locally using IStorageProvider interface
- **Storage Path**: `%ProgramData%\RDCS Agent\Screenshots\{Year}\{Month}\{Day}\{EmployeeId}\`
- **Methods**:
  - `UploadAsync(request, cancellationToken)` - Saves file to local storage
  - `DownloadAsync(key, cancellationToken)` - Reads file from local storage
  - `DeleteAsync(key, cancellationToken)` - Deletes file from local storage
  - `ExistsAsync(key, cancellationToken)` - Checks if file exists
  - `ListAsync(prefix, cancellationToken)` - Lists files in directory
  - `GetMetadataAsync(key, cancellationToken)` - Gets file metadata
- **Dependencies**: IAgentLogger, StoragePathHelper
- **Integration**: Implements IStorageProvider for future Amazon S3 migration

#### `StoragePathHelper`
- **Responsibility**: Generates storage paths based on date and employee
- **Methods**:
  - `GetStoragePath(employeeId, captureTimeUtc)` - Returns full storage path
  - `EnsureDirectoryExists(path)` - Creates directory if not exists
  - `GenerateFileName(captureTimeUtc, format)` - Generates unique filename
- **Dependencies**: None

### Workers

#### `IScreenshotWorker` / `ScreenshotWorker`
- **Responsibility**: Orchestrates screenshot capture and processing
- **Inherits**: BackgroundWorkerBase
- **Methods**:
  - `ExecuteAsync(cancellationToken)` - Main capture loop
  - `CaptureAndProcessAsync(cancellationToken)` - Single capture cycle
  - `ShouldCaptureAsync()` - Checks policy conditions
- **Dependencies**: IScreenshotService, IImageProcessingService, IPolicyEngine, IJobQueue, IEventBus, IScreenshotRepository, IStorageProvider, IAgentLogger
- **Integration**: 
  - Registered with Scheduler
  - Respects Policy Engine settings
  - Publishes events to Event Bus
  - Queues jobs to Queue System
  - Stores files using IStorageProvider (LocalStorageProvider)
  - Reports health to Health Monitor

#### `AutoCleanupWorker`
- **Responsibility**: Automatically deletes old screenshots based on retention policy
- **Inherits**: BackgroundWorkerBase
- **Methods**:
  - `ExecuteAsync(cancellationToken)` - Main cleanup loop
  - `CleanupOldScreenshotsAsync(cancellationToken)` - Deletes screenshots older than retention period
  - `UpdateStorageStatisticsAsync(cancellationToken)` - Updates storage statistics
- **Dependencies**: IScreenshotRepository, IStorageProvider, IPolicyEngine, IAgentLogger
- **Integration**:
  - Registered with Scheduler (daily)
  - Respects AutoCleanupDays policy
  - Publishes CleanupCompleted event
  - Updates StorageStatistics

### Repositories

#### `IScreenshotRepository` / `ScreenshotRepository`
- **Responsibility**: Persists screenshot metadata to SQLite
- **Methods**:
  - `SaveAsync(screenshot, cancellationToken)` - Saves screenshot record
  - `GetByIdAsync(id, cancellationToken)` - Retrieves by ID
  - `GetByEmployeeIdAsync(employeeId, cancellationToken)` - Retrieves by employee
  - `GetByDeviceIdAsync(deviceId, cancellationToken)` - Retrieves by device
  - `GetByDateRangeAsync(startDate, endDate, cancellationToken)` - Retrieves by date range
  - `DeleteAsync(id, cancellationToken)` - Deletes record
  - `DeleteOlderThanAsync(date, cancellationToken)` - Deletes records older than date
- **Dependencies**: SQLiteConnectionFactory, IAgentLogger

#### `IScreenshotJobRepository` / `ScreenshotJobRepository`
- **Responsibility**: Persists screenshot queue jobs to SQLite
- **Methods**:
  - `SaveAsync(job, cancellationToken)` - Saves job record
  - `GetByIdAsync(id, cancellationToken)` - Retrieves by ID
  - `GetPendingJobsAsync(limit, cancellationToken)` - Retrieves pending jobs
  - `UpdateStatusAsync(id, status, error, cancellationToken)` - Updates job status
  - `IncrementRetryAsync(id, cancellationToken)` - Increments retry count
- **Dependencies**: SQLiteConnectionFactory, IAgentLogger

### Health Monitoring

#### `IScreenshotHealthMonitor` / `ScreenshotHealthMonitor`
- **Responsibility**: Tracks screenshot-specific health metrics
- **Methods**:
  - `GetLastCaptureTimeAsync()` - Returns last successful capture time
  - `GetAverageCaptureDurationAsync()` - Returns average capture duration
  - `GetAverageCompressionTimeAsync()` - Returns average compression duration
  - `GetQueueSizeAsync()` - Returns current queue size
  - `GetStorageUsageAsync()` - Returns local storage usage
  - `GetFailureCountAsync()` - Returns failure count
  - `GetSuccessRateAsync()` - Returns capture success rate
  - `GetCompressionStatsAsync()` - Returns compression statistics
- **Dependencies**: IScreenshotRepository, IScreenshotJobRepository, IStorageProvider, IAgentLogger

### Events

#### `ScreenshotCaptured`
- **Trigger**: After successful capture
- **Data**: ScreenshotId, EmployeeId, DeviceId, MonitorId, CaptureTime, FilePath, FileSize

#### `ScreenshotSaved`
- **Trigger**: After successful local storage
- **Data**: ScreenshotId, FilePath, StoragePath, FileSize, Timestamp

#### `ScreenshotFailed`
- **Trigger**: After capture failure
- **Data**: ScreenshotId, EmployeeId, DeviceId, Error, Timestamp

#### `CompressionCompleted`
- **Trigger**: After compression
- **Data**: ScreenshotId, OriginalSize, CompressedSize, CompressionRatio, Duration

#### `StorageCompleted`
- **Trigger**: After file storage
- **Data**: ScreenshotId, StoragePath, FileSize, Timestamp

#### `QueueJobCreated`
- **Trigger**: After queue job creation
- **Data**: JobId, ScreenshotId, Priority, Status

#### `CaptureSkipped`
- **Trigger**: When capture is skipped due to policy
- **Data**: EmployeeId, DeviceId, Reason, Timestamp

#### `CleanupCompleted`
- **Trigger**: After auto-cleanup completes
- **Data**: DeletedCount, FreedSpaceBytes, Timestamp

---

## Communication Flow

### Capture Pipeline Flow

```
Scheduler (Interval-based)
    ↓
ScreenshotWorker.ExecuteAsync()
    ↓
ShouldCaptureAsync() → Policy Engine Check
    ↓ (if true)
ScreenshotService.CaptureFullDesktopAsync()
    ↓
ImageProcessingService.ProcessImagePipelineAsync()
    ├─→ Validate
    ├─→ Resize (optional)
    ├─→ Compress (ICompressionProvider)
    ├─→ Generate Metadata
    └─→ Return Processed Stream
    ↓
LocalStorageProvider.UploadAsync() → Save to %ProgramData%\RDCS Agent\Screenshots\{Year}\{Month}\{Day}\{EmployeeId}\
    ↓
ScreenshotRepository.SaveAsync()
    ↓
JobQueue.EnqueueAsync(ScreenshotJob)
    ↓
EventBus.PublishAsync(ScreenshotCaptured)
    ↓
EventBus.PublishAsync(ScreenshotSaved)
    ↓
EventBus.PublishAsync(StorageCompleted)
    ↓
EventBus.PublishAsync(QueueJobCreated)
    ↓
QueueWorker.ProcessJobAsync() → Upload metadata to Backend (NO IMAGE UPLOAD)
```

### Policy Check Flow

```
ScreenshotWorker.ShouldCaptureAsync()
    ↓
PolicyEngine.GetPolicyAsync<ScreenshotPolicy>()
    ↓
Check:
    ├─ CaptureEnabled
    ├─ CaptureDuringIdle (if true, check idle state)
    ├─ CaptureDuringOfficeHours (if true, check time)
    ├─ CaptureMultiMonitor (if false, capture primary only)
    ├─ LocalStorageEnabled
    ├─ MaximumLocalStorageSize (check current usage)
    └─ Any other policy conditions
    ↓
Return true/false
```

### Event Flow

```
ScreenshotCaptured
    ↓
[Subscribers]
    ├─→ HealthMonitor (update metrics)
    ├─→ ScreenshotRepository (save metadata)
    ├─→ QueueWorker (process job)
    └─→ Backend (upload metadata)

ScreenshotSaved
    ↓
[Subscribers]
    ├─→ HealthMonitor (update storage metrics)
    └─→ StorageStatistics (update)

StorageCompleted
    ↓
[Subscribers]
    ├─→ HealthMonitor (update storage metrics)
    └─→ AutoCleanupWorker (check if cleanup needed)

CleanupCompleted
    ↓
[Subscribers]
    ├─→ HealthMonitor (update storage metrics)
    └─→ StorageStatistics (update)
```

---

## Database Schema

### SQLite Tables

#### `Screenshots`
```sql
CREATE TABLE Screenshots (
    Id TEXT PRIMARY KEY,
    EmployeeId TEXT NOT NULL,
    DeviceId TEXT NOT NULL,
    MonitorId TEXT NOT NULL,
    CaptureTimeUtc TEXT NOT NULL,
    FilePath TEXT NOT NULL,
    StoragePath TEXT NOT NULL,
    Width INTEGER NOT NULL,
    Height INTEGER NOT NULL,
    Format TEXT NOT NULL,
    Quality INTEGER NOT NULL,
    Compressed INTEGER NOT NULL,
    FileSizeBytes INTEGER NOT NULL,
    CorrelationId TEXT NOT NULL,
    UploadStatus TEXT NOT NULL DEFAULT 'Pending',
    UploadedAtUtc TEXT,
    CreatedAtUtc TEXT NOT NULL,
    UpdatedAtUtc TEXT NOT NULL
);

CREATE INDEX idx_screenshots_employee ON Screenshots(EmployeeId);
CREATE INDEX idx_screenshots_device ON Screenshots(DeviceId);
CREATE INDEX idx_screenshots_capture_time ON Screenshots(CaptureTimeUtc);
CREATE INDEX idx_screenshots_upload_status ON Screenshots(UploadStatus);
```

#### `ScreenshotJobs`
```sql
CREATE TABLE ScreenshotJobs (
    Id TEXT PRIMARY KEY,
    CorrelationId TEXT NOT NULL,
    EmployeeId TEXT NOT NULL,
    DeviceId TEXT NOT NULL,
    MonitorId TEXT NOT NULL,
    CaptureTimeUtc TEXT NOT NULL,
    FilePath TEXT NOT NULL,
    StoragePath TEXT NOT NULL,
    Compressed INTEGER NOT NULL,
    RetryCount INTEGER NOT NULL DEFAULT 0,
    MaxRetryCount INTEGER NOT NULL DEFAULT 3,
    Priority INTEGER NOT NULL DEFAULT 1,
    Status TEXT NOT NULL DEFAULT 'Pending',
    Error TEXT,
    CreatedAtUtc TEXT NOT NULL,
    StartedAtUtc TEXT,
    CompletedAtUtc TEXT,
    NextRetryAtUtc TEXT
);

CREATE INDEX idx_screenshot_jobs_employee ON ScreenshotJobs(EmployeeId);
CREATE INDEX idx_screenshot_jobs_status ON ScreenshotJobs(Status);
CREATE INDEX idx_screenshot_jobs_priority ON ScreenshotJobs(Priority);
CREATE INDEX idx_screenshot_jobs_created_at ON ScreenshotJobs(CreatedAtUtc);
```

#### `CompressionStats`
```sql
CREATE TABLE CompressionStats (
    Id TEXT PRIMARY KEY,
    ScreenshotId TEXT NOT NULL,
    OriginalSizeBytes INTEGER NOT NULL,
    CompressedSizeBytes INTEGER NOT NULL,
    CompressionRatio REAL NOT NULL,
    CompressionDurationMs INTEGER NOT NULL,
    Algorithm TEXT NOT NULL,
    TimestampUtc TEXT NOT NULL
);

CREATE INDEX idx_compression_stats_screenshot ON CompressionStats(ScreenshotId);
CREATE INDEX idx_compression_stats_timestamp ON CompressionStats(TimestampUtc);
```

#### `CaptureHistory`
```sql
CREATE TABLE CaptureHistory (
    Id TEXT PRIMARY KEY,
    EmployeeId TEXT NOT NULL,
    DeviceId TEXT NOT NULL,
    CaptureTimeUtc TEXT NOT NULL,
    Success INTEGER NOT NULL,
    FailureReason TEXT,
    DurationMs INTEGER NOT NULL,
    MonitorCount INTEGER NOT NULL,
    TotalSizeBytes INTEGER NOT NULL
);

CREATE INDEX idx_capture_history_employee ON CaptureHistory(EmployeeId);
CREATE INDEX idx_capture_history_device ON CaptureHistory(DeviceId);
CREATE INDEX idx_capture_history_capture_time ON CaptureHistory(CaptureTimeUtc);
```

#### `StorageStatistics`
```sql
CREATE TABLE StorageStatistics (
    Id TEXT PRIMARY KEY,
    TotalScreenshots INTEGER NOT NULL,
    TotalSizeBytes INTEGER NOT NULL,
    OldestScreenshotDate TEXT,
    NewestScreenshotDate TEXT,
    AverageSizeBytes REAL,
    LastCleanupTimeUtc TEXT,
    UpdatedAtUtc TEXT NOT NULL
);
```

### PostgreSQL Tables (Backend)

#### `Screenshot`
```prisma
model Screenshot {
  id              String   @id @default(uuid())
  employeeId      String   @map("employee_id")
  deviceId        String   @map("device_id")
  monitorId       String   @map("monitor_id")
  captureTimeUtc  DateTime @map("capture_time_utc")
  width           Int
  height          Int
  format          String
  quality         Int
  compressed      Boolean  @default(false)
  fileSizeBytes   BigInt   @map("file_size_bytes")
  correlationId   String   @map("correlation_id")
  storagePath     String?  @map("storage_path")
  storageProvider String?  @default("Local") @map("storage_provider")
  uploadStatus    String   @default("Pending") @map("upload_status")
  uploadedAtUtc   DateTime? @map("uploaded_at_utc")
  createdAtUtc    DateTime @default(now()) @map("created_at_utc")
  updatedAtUtc    DateTime @updatedAt @map("updated_at_utc")

  device          EmployeeDevice @relation(fields: [deviceId], references: [id], onDelete: Cascade)

  @@index([employeeId])
  @@index([deviceId])
  @@index([captureTimeUtc])
  @@index([uploadStatus])
  @@map("screenshots")
}
```

---

## API Contracts

### Backend API Endpoints

#### GET `/api/screenshots`
- **Description**: Get screenshots with filtering
- **Query Params**:
  - `employeeId` (optional) - Filter by employee
  - `deviceId` (optional) - Filter by device
  - `startDate` (optional) - Filter by start date
  - `endDate` (optional) - Filter by end date
  - `monitorId` (optional) - Filter by monitor
  - `status` (optional) - Filter by upload status
  - `page` (optional) - Page number (default: 1)
  - `limit` (optional) - Items per page (default: 50)
- **Response**: Array of ScreenshotDTO

#### GET `/api/screenshots/:id`
- **Description**: Get screenshot by ID
- **Response**: ScreenshotDTO

#### GET `/api/screenshots/:id/metadata`
- **Description**: Get screenshot metadata only (no image data)
- **Response**: ScreenshotMetadataDTO

#### GET `/api/screenshots/employee/:employeeId/timeline`
- **Description**: Get screenshot timeline for employee
- **Query Params**:
  - `startDate` (optional) - Start date
  - `endDate` (optional) - End date
  - `monitorId` (optional) - Filter by monitor
- **Response**: Array of ScreenshotTimelineDTO

#### POST `/api/screenshots/metadata`
- **Description**: Upload screenshot metadata (image uploaded separately)
- **Body**: ScreenshotMetadataDTO
- **Response**: ScreenshotDTO

#### PUT `/api/screenshots/:id/status`
- **Description**: Update screenshot upload status
- **Body**: `{ status: string, storageUrl?: string }`
- **Response**: ScreenshotDTO

#### DELETE `/api/screenshots/:id`
- **Description**: Delete screenshot
- **Response**: 204 No Content

### DTOs

#### `ScreenshotDTO`
```typescript
{
  id: string;
  employeeId: string;
  deviceId: string;
  monitorId: string;
  captureTimeUtc: string;
  width: number;
  height: number;
  format: string;
  quality: number;
  compressed: boolean;
  encrypted: boolean;
  fileSizeBytes: number;
  correlationId: string;
  storageUrl?: string;
  storageProvider?: string;
  uploadStatus: string;
  uploadedAtUtc?: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}
```

#### `ScreenshotMetadataDTO`
```typescript
{
  id: string;
  employeeId: string;
  deviceId: string;
  monitorId: string;
  captureTimeUtc: string;
  width: number;
  height: number;
  format: string;
  fileSizeBytes: number;
  correlationId: string;
  storagePath: string;
  uploadStatus: string;
}
```

#### `ScreenshotTimelineDTO`
```typescript
{
  id: string;
  captureTimeUtc: string;
  monitorId: string;
  thumbnailUrl?: string;
  status: string;
  width: number;
  height: number;
  storagePath: string;
}
```

---

## Testing Strategy

### Unit Tests

#### ScreenshotService Tests
- Test single monitor capture
- Test multi-monitor capture
- Test high DPI capture
- Test virtual desktop capture
- Test cancellation token handling
- Test error handling

#### ImageProcessingService Tests
- Test image validation
- Test image resize
- Test JPEG compression
- Test PNG compression
- Test WEBP compression
- Test DPAPI encryption
- Test pipeline execution
- Test error handling

#### CompressionProvider Tests
- Test JPEG quality levels
- Test PNG compression levels
- Test WEBP quality levels
- Test compression ratio calculation
- Test invalid input handling

#### EncryptionProvider Tests
- Test DPAPI encryption
- Test DPAPI decryption
- Test invalid data handling

#### ScreenshotWorker Tests
- Test policy check logic
- Test capture scheduling
- Test event publishing
- Test queue job creation
- Test health reporting
- Test retry logic
- Test cancellation

### Integration Tests

#### End-to-End Pipeline Test
- Setup: Initialize database, register worker
- Action: Trigger capture
- Verify: Screenshot saved, job queued, events published
- Cleanup: Delete test data

#### Policy Integration Test
- Setup: Configure screenshot policy
- Action: Trigger capture with different policy settings
- Verify: Capture respects policy (skipped when disabled, etc.)

#### Queue Integration Test
- Setup: Create screenshot job
- Action: Process queue
- Verify: Job status updated, events published

#### Database Integration Test
- Setup: Initialize SQLite database
- Action: Save screenshot metadata
- Verify: Data persisted correctly, queries work

### Performance Tests

#### Capture Performance Test
- Measure: Capture duration for different resolutions
- Target: < 500ms for 1080p, < 1s for 4K

#### Compression Performance Test
- Measure: Compression duration and ratio
- Target: < 2s for 1080p, ratio > 50%

#### Memory Usage Test
- Measure: RAM usage during capture
- Target: < 100MB per capture

#### Concurrency Test
- Measure: Multiple simultaneous captures
- Target: No deadlocks, no UI freeze

---

## Policy Configuration

### ScreenshotPolicy

```csharp
public class ScreenshotPolicy
{
    public bool Enabled { get; set; }
    public int IntervalSeconds { get; set; } = 300; // 5 minutes
    public int Quality { get; set; } = 85;
    public string Format { get; set; } = "JPEG";
    public bool CaptureActiveWindowOnly { get; set; } = false;
    public bool CaptureOnIdle { get; set; } = false;
    public int IdleThresholdSeconds { get; set; } = 300;
    public bool CaptureDuringOfficeHours { get; set; } = true;
    public TimeSpan OfficeHoursStart { get; set; } = TimeSpan.FromHours(9);
    public TimeSpan OfficeHoursEnd { get; set; } = TimeSpan.FromHours(17);
    public bool CaptureMultiMonitor { get; set; } = true;
    public bool CompressionEnabled { get; set; } = true;
    public int MaxWidth { get; set; } = 1920;
    public int MaxHeight { get; set; } = 1080;
    public bool LocalStorageEnabled { get; set; } = true;
    public long MaximumLocalStorageSizeBytes { get; set; } = 10737418240; // 10 GB
    public int AutoCleanupDays { get; set; } = 30; // Keep for 30 days
}
```

---

## Security Considerations

1. **Local Storage**: Screenshots stored locally in %ProgramData%\RDCS Agent\Screenshots
2. **Access Control**: Only authorized users can view screenshots via CRM
3. **Data Retention**: Configurable retention policy for screenshots (AutoCleanupDays)
4. **Privacy**: Screenshots not captured during idle time (if policy enabled)
5. **Compliance**: GDPR-compliant metadata storage
6. **Audit Trail**: All capture attempts logged in CaptureHistory
7. **Storage Limits**: Maximum local storage size enforced by policy

---

## Performance Optimization

1. **Async Operations**: All I/O operations are async
2. **Cancellation Tokens**: Support for immediate cancellation
3. **Memory Management**: Dispose of image streams immediately
4. **Compression**: Configurable quality to balance size vs. quality
5. **Queue**: Batch metadata uploads to reduce API calls
6. **Thumbnails**: Generate thumbnails for faster UI loading
7. **Lazy Loading**: CRM loads images on demand
8. **Storage Optimization**: Organized by date for efficient cleanup

---

## Queue Flow

### Screenshot Job Lifecycle

```
ScreenshotWorker.CaptureAndProcessAsync()
    ↓
Screenshot Saved Locally
    ↓
Create ScreenshotJob
    ├─ JobId: GUID
    ├─ CorrelationId: GUID
    ├─ EmployeeId: From config
    ├─ DeviceId: From config
    ├─ MonitorId: Monitor identifier
    ├─ CaptureTimeUtc: Current UTC time
    ├─ FilePath: Relative path
    ├─ StoragePath: Full storage path
    ├─ Compressed: true/false
    ├─ RetryCount: 0
    ├─ Priority: Normal (1)
    └─ Status: Pending
    ↓
JobQueue.EnqueueAsync(ScreenshotJob)
    ↓
QueueWorker.ProcessJobAsync()
    ├─ Update Status: Running
    ├─ Upload Metadata to Backend (NO IMAGE)
    ├─ Update Status: Completed
    └─ If Failed: Increment Retry, Update Status: Failed
    ↓
Backend stores metadata (image remains local)
```

### Queue Job States

- **Pending**: Job created, waiting to be processed
- **Running**: Job is being processed by QueueWorker
- **Completed**: Metadata successfully uploaded to backend
- **Failed**: Upload failed, will retry
- **DeadLetter**: Max retries exceeded, manual intervention required

---

## Screenshot Processing Flow

### Detailed Pipeline

```
1. CAPTURE
   ScreenshotService.CaptureFullDesktopAsync()
   ├─ Get monitor configuration
   ├─ Check high DPI scaling (125%, 150%, 175%, 200%)
   ├─ Capture each monitor
   ├─ Handle portrait/landscape orientations
   └─ Return Bitmap(s)

2. VALIDATE
   ImageProcessingService.ValidateImageAsync()
   ├─ Check image integrity
   ├─ Verify dimensions
   └─ Validate format

3. RESIZE (Optional)
   ImageProcessingService.ResizeImageAsync()
   ├─ Read MaxWidth/MaxHeight from policy
   ├─ Calculate aspect ratio
   ├─ Resize if exceeds limits
   └─ Return resized image

4. COMPRESS
   ImageProcessingService.CompressImageAsync()
   ├─ Read Format from policy (JPEG/PNG/WEBP)
   ├─ Read Quality from policy
   ├─ Select ICompressionProvider
   ├─ Compress image
   └─ Return compressed stream

5. GENERATE METADATA
   ImageProcessingService.GenerateMetadataAsync()
   ├─ Extract image dimensions
   ├─ Calculate file size
   ├─ Generate correlation ID
   └─ Return ScreenshotMetadata

6. STORE LOCALLY
   LocalStorageProvider.UploadAsync()
   ├─ Generate storage path: %ProgramData%\RDCS Agent\Screenshots\{Year}\{Month}\{Day}\{EmployeeId}\
   ├─ Ensure directory exists
   ├─ Generate filename: {timestamp}_{correlationId}.{ext}
   ├─ Save file
   └─ Return storage path

7. SAVE METADATA
   ScreenshotRepository.SaveAsync()
   ├─ Create Screenshot record
   ├─ Save to SQLite
   └─ Return ScreenshotId

8. CREATE QUEUE JOB
   JobQueue.EnqueueAsync()
   ├─ Create ScreenshotJob
   ├─ Enqueue to JobQueue
   └─ Return JobId

9. PUBLISH EVENTS
   EventBus.PublishAsync()
   ├─ ScreenshotCaptured
   ├─ ScreenshotSaved
   ├─ CompressionCompleted
   ├─ StorageCompleted
   └─ QueueJobCreated
```

---

## Local Storage Architecture

### Storage Path Structure

```
%ProgramData%\RDCS Agent\Screenshots\
├── 2026\
│   ├── 07\
│   │   ├── 03\
│   │   │   ├── EMP001\
│   │   │   │   ├── 1720072400_abc123.jpg
│   │   │   │   ├── 1720072700_def456.jpg
│   │   │   │   └── 1720073000_ghi789.jpg
│   │   │   ├── EMP002\
│   │   │   │   └── ...
│   │   │   └── EMP003\
│   │   │       └── ...
│   │   ├── 04\
│   │   │   └── ...
│   │   └── 05\
│   │       └── ...
│   ├── 08\
│   │   └── ...
│   └── 09\
│       └── ...
└── 2027\
    └── ...
```

### Storage Path Helper Logic

```csharp
public class StoragePathHelper
{
    private const string BasePath = @"%ProgramData%\RDCS Agent\Screenshots";

    public string GetStoragePath(string employeeId, DateTime captureTimeUtc)
    {
        var year = captureTimeUtc.Year.ToString("D4");
        var month = captureTimeUtc.Month.ToString("D2");
        var day = captureTimeUtc.Day.ToString("D2");
        
        return Path.Combine(
            Environment.ExpandEnvironmentVariables(BasePath),
            year,
            month,
            day,
            employeeId
        );
    }

    public string GenerateFileName(DateTime captureTimeUtc, string format)
    {
        var timestamp = ((DateTimeOffset)captureTimeUtc).ToUnixTimeSeconds();
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        var extension = format.ToLowerInvariant();
        
        return $"{timestamp}_{correlationId}.{extension}";
    }
}
```

### LocalStorageProvider Implementation

```csharp
public class LocalStorageProvider : IStorageProvider
{
    public string ProviderName => "Local";
    
    public async Task<StorageResponse> UploadAsync(StorageRequest request, CancellationToken cancellationToken)
    {
        var fullPath = Path.Combine(request.BucketName ?? BasePath, request.Key);
        var directory = Path.GetDirectoryName(fullPath);
        
        Directory.CreateDirectory(directory!);
        
        using var fileStream = File.Create(fullPath);
        await request.Content.CopyToAsync(fileStream, cancellationToken);
        
        var fileInfo = new FileInfo(fullPath);
        
        return new StorageResponse
        {
            Success = true,
            Key = request.Key,
            Url = fullPath,
            SizeBytes = fileInfo.Length,
            UploadedAtUtc = DateTime.UtcNow
        };
    }
    
    // Other IStorageProvider methods...
}
```

### Storage Statistics Tracking

- **TotalScreenshots**: Count of all screenshots in storage
- **TotalSizeBytes**: Sum of all screenshot file sizes
- **OldestScreenshotDate**: Date of oldest screenshot
- **NewestScreenshotDate**: Date of newest screenshot
- **AverageSizeBytes**: Average file size
- **LastCleanupTimeUtc**: Last time cleanup ran

---

## Testing Strategy

### Unit Tests

#### ScreenshotService Tests
- Test single monitor capture
- Test multi-monitor capture
- Test high DPI capture (125%, 150%, 175%, 200%)
- Test virtual desktop capture
- Test portrait monitor capture
- Test landscape monitor capture
- Test cancellation token handling
- Test error handling

#### ImageProcessingService Tests
- Test image validation
- Test image resize
- Test JPEG compression
- Test PNG compression
- Test WEBP compression
- Test metadata generation
- Test pipeline execution
- Test error handling

#### CompressionProvider Tests
- Test JPEG quality levels
- Test PNG compression levels
- Test WEBP quality levels
- Test compression ratio calculation
- Test invalid input handling

#### LocalStorageProvider Tests
- Test file upload
- Test file download
- Test file deletion
- Test file existence check
- Test directory listing
- Test path generation
- Test storage limits

#### AutoCleanupWorker Tests
- Test cleanup by date
- Test storage statistics update
- Test retention policy enforcement
- Test cleanup event publishing

#### ScreenshotWorker Tests
- Test policy check logic
- Test capture scheduling
- Test event publishing
- Test queue job creation
- Test health reporting
- Test retry logic
- Test cancellation
- Test storage limit enforcement

### Integration Tests

#### End-to-End Pipeline Test
- Setup: Initialize database, register worker
- Action: Trigger capture
- Verify: Screenshot saved locally, metadata saved, job queued, events published
- Cleanup: Delete test data

#### Policy Integration Test
- Setup: Configure screenshot policy
- Action: Trigger capture with different policy settings
- Verify: Capture respects policy (skipped when disabled, etc.)

#### Queue Integration Test
- Setup: Create screenshot job
- Action: Process queue
- Verify: Job status updated, events published

#### Database Integration Test
- Setup: Initialize SQLite database
- Action: Save screenshot metadata
- Verify: Data persisted correctly, queries work

#### Storage Integration Test
- Setup: Initialize LocalStorageProvider
- Action: Save screenshot
- Verify: File saved in correct path, metadata correct

#### AutoCleanup Integration Test
- Setup: Create old screenshots
- Action: Run AutoCleanupWorker
- Verify: Old files deleted, storage statistics updated

### Performance Tests

#### Capture Performance Test
- Measure: Capture duration for different resolutions
- Target: < 500ms for 1080p, < 1s for 4K

#### Compression Performance Test
- Measure: Compression duration and ratio
- Target: < 2s for 1080p, ratio > 50%

#### Storage Performance Test
- Measure: File save duration
- Target: < 100ms for 5MB file

#### Memory Usage Test
- Measure: RAM usage during capture
- Target: < 100MB per capture

#### Concurrency Test
- Measure: Multiple simultaneous captures
- Target: No deadlocks, no UI freeze

#### Storage Cleanup Performance Test
- Measure: Cleanup duration for 10,000 files
- Target: < 30 seconds

---

## Performance Considerations

### CPU Usage
- **Capture**: Uses Windows API, minimal CPU overhead
- **Compression**: CPU-intensive, configurable quality to balance
- **Target**: < 5% average CPU usage during idle, < 20% during capture

### RAM Usage
- **Image Buffers**: Dispose immediately after use
- **Streaming**: Process images in streams, not full memory
- **Target**: < 100MB per capture operation

### Disk I/O
- **Sequential Writes**: Organized by date for efficient writes
- **Async Operations**: All file operations async
- **Target**: < 100ms per 5MB file save

### Storage Growth
- **Compression**: Reduces file size by 50-80%
- **Auto-Cleanup**: Removes old files automatically
- **Limits**: Configurable maximum storage size

### Network
- **Metadata Only**: No image uploads in this phase
- **Batch Uploads**: Queue metadata for batch processing
- **Target**: < 1KB per screenshot metadata

---

## Future Amazon S3 Migration Strategy

### Storage Provider Interface

The `IStorageProvider` interface is designed to be storage-agnostic:

```csharp
public interface IStorageProvider
{
    string ProviderName { get; }
    Task<StorageResponse> UploadAsync(StorageRequest request, CancellationToken cancellationToken = default);
    Task<StorageResponse> DownloadAsync(string key, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task<List<string>> ListAsync(string prefix, CancellationToken cancellationToken = default);
    Task<StorageResponse> GetMetadataAsync(string key, CancellationToken cancellationToken = default);
}
```

### Migration Steps

#### Phase 1: Implement AmazonS3StorageProvider
```csharp
public class AmazonS3StorageProvider : IStorageProvider
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    
    public string ProviderName => "AmazonS3";
    
    public async Task<StorageResponse> UploadAsync(StorageRequest request, CancellationToken cancellationToken)
    {
        // Upload to S3
        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = request.Key,
            InputStream = request.Content,
            ContentType = request.ContentType
        };
        
        var response = await _s3Client.PutObjectAsync(putRequest, cancellationToken);
        
        return new StorageResponse
        {
            Success = true,
            Key = request.Key,
            Url = $"https://{_bucketName}.s3.amazonaws.com/{request.Key}",
            SizeBytes = request.Content.Length
        };
    }
    
    // Implement other methods...
}
```

#### Phase 2: Update Policy Engine
```csharp
public class ScreenshotPolicy
{
    public bool LocalStorageEnabled { get; set; } = false; // Switch to false
    public bool CloudStorageEnabled { get; set; } = true; // New field
    public string StorageProvider { get; set; } = "AmazonS3"; // New field
    public string S3BucketName { get; set; } = "rdcs-screenshots"; // New field
    public string S3Region { get; set; } = "us-east-1"; // New field
}
```

#### Phase 3: Update DI Registration
```csharp
// Before (Local Storage)
services.AddSingleton<IStorageProvider, LocalStorageProvider>();

// After (Amazon S3)
services.AddSingleton<IStorageProvider>(sp => 
{
    var config = sp.GetRequiredService<IConfiguration>();
    var s3Config = config.GetSection("AWS:S3");
    
    return new AmazonS3StorageProvider(
        s3Config["BucketName"],
        s3Config["Region"],
        s3Config["AccessKey"],
        s3Config["SecretKey"],
        sp.GetRequiredService<IAgentLogger>()
    );
});
```

#### Phase 4: Data Migration
- Script to migrate existing local screenshots to S3
- Update SQLite records with new storage paths
- Verify all data migrated successfully

#### Phase 5: Cleanup
- Remove LocalStorageProvider if no longer needed
- Clean up local storage after migration
- Update documentation

### Key Points

1. **No Business Logic Changes**: Only DI registration changes
2. **Same Interface**: Both providers implement IStorageProvider
3. **Policy-Driven**: Storage provider selected by policy
4. **Gradual Migration**: Can run both providers during transition
5. **Fallback**: Keep local storage as backup during migration

---

## Deployment Considerations

1. **Service Registration**: Register all services in DI container
2. **Worker Registration**: Register ScreenshotWorker with Scheduler
3. **AutoCleanup Registration**: Register AutoCleanupWorker with Scheduler (daily)
4. **Database Migration**: Run SQLite schema migration on startup
5. **Policy Sync**: Download screenshot policy from backend on startup
6. **Health Monitoring**: Register ScreenshotHealthMonitor with HealthMonitor
7. **Event Subscriptions**: Subscribe to relevant events
8. **Storage Initialization**: Ensure storage directory exists on startup
9. **Storage Limits Check**: Check storage limits on startup
10. **Permissions**: Ensure agent has write access to %ProgramData%

---

## Future Enhancements

1. **AES Encryption**: Add AES encryption provider for cross-platform support
2. **OCR**: Add text extraction from screenshots
3. **Face Detection**: Add face blurring for privacy
4. **Smart Capture**: Capture on specific events (window change, etc.)
5. **Live Preview**: Add live preview in CRM
6. **Video Recording**: Add screen recording capability
7. **Cloud Storage**: Direct upload to S3/Cloudflare R2 (migration strategy defined above)
8. **Machine Learning**: Anomaly detection in screenshots
9. **Differential Compression**: Only store changed pixels
10. **Thumbnail Generation**: Auto-generate thumbnails for CRM

---

## Deployment Considerations

1. **Service Registration**: Register all services in DI container
2. **Worker Registration**: Register ScreenshotWorker with Scheduler
3. **AutoCleanup Registration**: Register AutoCleanupWorker with Scheduler (daily)
4. **Database Migration**: Run SQLite schema migration on startup
5. **Policy Sync**: Download screenshot policy from backend on startup
6. **Health Monitoring**: Register ScreenshotHealthMonitor with HealthMonitor
7. **Event Subscriptions**: Subscribe to relevant events
8. **Storage Initialization**: Ensure storage directory exists on startup
9. **Storage Limits Check**: Check storage limits on startup
10. **Permissions**: Ensure agent has write access to %ProgramData%

---

## Future Enhancements

1. **AES Encryption**: Add AES encryption provider for cross-platform support
2. **OCR**: Add text extraction from screenshots
3. **Face Detection**: Add face blurring for privacy
4. **Smart Capture**: Capture on specific events (window change, etc.)
5. **Live Preview**: Add live preview in CRM
6. **Video Recording**: Add screen recording capability
7. **Cloud Storage**: Direct upload to S3/Cloudflare R2 (migration strategy defined above)
8. **Machine Learning**: Anomaly detection in screenshots
9. **Differential Compression**: Only store changed pixels
10. **Thumbnail Generation**: Auto-generate thumbnails for CRM
