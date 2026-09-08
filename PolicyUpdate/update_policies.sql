-- RDCS Employee Agent - Policy Update Script
-- Applies faster upload and optimized screenshot settings.
-- Run via: sqlite3 "C:\RDCS Agent\Database\agent.db" ".read update_policies.sql"

-- UploadPolicy: faster cycles and more parallelism
INSERT OR REPLACE INTO Policies (
  PolicyType, PolicyJson, Version, DownloadedAtUtc, AppliedAtUtc, IsActive
) VALUES (
  'UploadPolicy',
  '{
    "Enabled": true,
    "IntervalSeconds": 5,
    "MaxParallelUploads": 4,
    "MaxRetryCount": 5,
    "RetryBaseDelayMinutes": 1,
    "DeleteLocalAfterUpload": true,
    "PauseOnMeteredConnection": false,
    "PauseOnLowBattery": false,
    "UploadDuringOfficeHours": false,
    "CompressionBeforeUpload": false,
    "MaxUploadBandwidthBytesPerSec": 0,
    "MaxFileSizeBytes": 52428800
  }',
  '1.0', datetime('now'), datetime('now'), 1
);

-- ScreenshotPolicy: smaller images upload faster
INSERT OR REPLACE INTO Policies (
  PolicyType, PolicyJson, Version, DownloadedAtUtc, AppliedAtUtc, IsActive
) VALUES (
  'ScreenshotPolicy',
  '{
    "Enabled": true,
    "IntervalSeconds": 10,
    "Quality": 75,
    "Format": "JPEG",
    "CaptureActiveWindowOnly": false,
    "CaptureOnIdle": false,
    "IdleThresholdSeconds": 300,
    "CaptureDuringOfficeHours": false,
    "OfficeHoursStart": "09:00:00",
    "OfficeHoursEnd": "17:00:00",
    "DisableWeekends": false,
    "CaptureMultiMonitor": true,
    "CompressionEnabled": true,
    "MaxWidth": 1600,
    "MaxHeight": 900,
    "LocalStorageEnabled": true,
    "MaximumLocalStorageSizeBytes": 10737418240,
    "AutoCleanupDays": 30
  }',
  '1.0', datetime('now'), datetime('now'), 1
);
