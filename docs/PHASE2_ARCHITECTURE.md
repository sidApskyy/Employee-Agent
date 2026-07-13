# RDCS Employee Agent — Phase 2 Architecture

> **Version:** 2.0  
> **Scope:** Agent Runtime Infrastructure. This phase builds the enterprise-grade infrastructure that all future modules (Screenshot, Browser Monitoring, Application Monitoring, etc.) will use.  
> **Scale Target:** 10,000 employees, millions of screenshots, offline operation, cloud storage ready.

---

## 1. Executive Summary

Phase 2 transforms the Phase 1 foundation into an enterprise-grade runtime infrastructure. We introduce **15 core infrastructure components** that will be used by all future modules:

1. **Event Bus** - Decoupled, strongly-typed event communication
2. **Worker Framework** - Generic base class for all background tasks
3. **Scheduler** - Enterprise scheduling (cron, interval, dynamic)
4. **SQLite Database** - Local persistence with repository pattern
5. **Queue System** - Enterprise job queue with retry, DLQ, priority
6. **Policy Engine** - Centralized policy management
7. **Feature Flags** - Remote feature flag management
8. **Health Monitor** - System health monitoring
9. **Module Manager** - Enhanced module lifecycle management
10. **State Machine** - Agent state machine with validation
11. **Storage Provider** - Cloud storage abstraction
12. **Background Services** - Microsoft.Extensions.Hosting conversion
13. **Backend Extensions** - New tables and APIs
14. **Testing Strategy** - Unit and integration tests
15. **Documentation** - Architecture, communication flow, threading, lifecycles

### Core Architectural Principles (Extended)

- **Event-Driven Architecture:** All inter-module communication flows through the Event Bus. No direct module-to-module calls.
- **Worker-Based Execution:** All background tasks inherit from `BackgroundWorkerBase`. The framework handles lifecycle, retry, health, and cancellation.
- **Repository Pattern:** All data access goes through repository interfaces. SQLite is the default; future databases can be swapped.
- **Queue-Based Processing:** All async operations (screenshots, uploads, sync) go through the Queue System. Supports retry, DLQ, and priority.
- **Policy-Driven Behavior:** Modules never read settings.json directly. They query the Policy Engine.
- **Feature Flag Control:** All optional features are controlled by Feature Flags downloaded from the backend.
- **State Machine Validation:** Agent state transitions are validated. Invalid transitions throw exceptions.
- **Storage Abstraction:** Cloud storage is abstracted behind `IStorageProvider`. Modules don't know if it's S3, R2, or Azure.
- **Offline-First Design:** All operations queue locally when offline. Sync when connection restored.
- **Scalability:** SQLite with WAL mode for concurrent access. Queue system supports 10,000+ concurrent jobs.

---

## 2. Phase 2 Project Structure

### New Projects

```
D:\RDCS Employee Agent\src\
├── RDCS.EmployeeAgent.Runtime/          # NEW: Runtime infrastructure
│   ├── EventBus/
│   ├── Workers/
│   ├── Scheduler/
│   ├── Queue/
│   ├── Policy/
│   ├── FeatureFlags/
│   ├── Health/
│   ├── StateMachine/
│   └── Storage/
├── RDCS.EmployeeAgent.Persistence/      # NEW: Data persistence
│   ├── SQLite/
│   ├── Repositories/
│   ├── Migrations/
│   └── SeedData/
└── RDCS.EmployeeAgent.Tests/           # EXTENDED: Phase 2 tests
    ├── Runtime/
    ├── Persistence/
    └── Integration/
```

### Updated Folder Structure

```
RDCS.EmployeeAgent.Runtime/
├── EventBus/
│   ├── IEventBus.cs
│   ├── EventBus.cs
│   ├── EventSubscription.cs
│   ├── EventPriority.cs
│   └── Events/
│       ├── AuthenticationSucceeded.cs
│       ├── ConfigurationChanged.cs
│       ├── HeartbeatCompleted.cs
│       ├── PolicyUpdated.cs
│       ├── AgentStarted.cs
│       ├── AgentStopped.cs
│       ├── InternetConnected.cs
│       ├── InternetDisconnected.cs
│       ├── JobCompleted.cs
│       └── JobFailed.cs
├── Workers/
│   ├── IBackgroundWorker.cs
│   ├── BackgroundWorkerBase.cs
│   ├── WorkerState.cs
│   ├── WorkerHealth.cs
│   └── WorkerConfiguration.cs
├── Scheduler/
│   ├── IScheduler.cs
│   ├── Scheduler.cs
│   ├── ScheduleType.cs
│   ├── ScheduledJob.cs
│   └── CronExpression.cs
├── Queue/
│   ├── IJobQueue.cs
│   ├── IJobProcessor.cs
│   ├── JobQueue.cs
│   ├── JobProcessor.cs
│   ├── QueueWorker.cs
│   ├── JobState.cs
│   ├── JobPriority.cs
│   └── Models/
│       ├── IJob.cs
│       ├── JobContext.cs
│       └── JobResult.cs
├── Policy/
│   ├── IPolicyEngine.cs
│   ├── PolicyEngine.cs
│   ├── PolicyType.cs
│   └── Policies/
│       ├── ScreenshotPolicy.cs
│       ├── BrowserPolicy.cs
│       ├── ApplicationPolicy.cs
│       ├── IdlePolicy.cs
│       └── UsbPolicy.cs
├── FeatureFlags/
│   ├── IFeatureFlagManager.cs
│   ├── FeatureFlagManager.cs
│   ├── FeatureFlag.cs
│   └── FlagName.cs
├── Health/
│   ├── IHealthMonitor.cs
│   ├── HealthMonitor.cs
│   ├── HealthStatus.cs
│   ├── HealthMetric.cs
│   └── Metrics/
│       ├── CpuMetric.cs
│       ├── RamMetric.cs
│       ├── DiskMetric.cs
│       ├── InternetMetric.cs
│       ├── QueueMetric.cs
│       ├── DatabaseMetric.cs
│       └── ServiceMetric.cs
├── StateMachine/
│   ├── IAgentStateMachine.cs
│   ├── AgentStateMachine.cs
│   ├── AgentState.cs
│   ├── StateTransition.cs
│   └── StateTransitionValidator.cs
└── Storage/
    ├── IStorageProvider.cs
    ├── StorageProviderFactory.cs
    ├── Providers/
    │   ├── AmazonS3StorageProvider.cs
    │   ├── CloudflareR2StorageProvider.cs (future)
    │   └── AzureStorageProvider.cs (future)
    └── Models/
        ├── StorageRequest.cs
        ├── StorageResponse.cs
        └── StorageException.cs

RDCS.EmployeeAgent.Persistence/
├── SQLite/
│   ├── SQLiteContext.cs
│   ├── SQLiteConnectionFactory.cs
│   └── DatabaseInitializer.cs
├── Repositories/
│   ├── IAgentStateRepository.cs
│   ├── AgentStateRepository.cs
│   ├── IJobQueueRepository.cs
│   ├── JobQueueRepository.cs
│   ├── IPolicyRepository.cs
│   ├── PolicyRepository.cs
│   ├── IFeatureFlagRepository.cs
│   ├── FeatureFlagRepository.cs
│   ├── ILogRepository.cs
│   ├── LogRepository.cs
│   ├── IOfflineEventRepository.cs
│   ├── OfflineEventRepository.cs
│   ├── IQueueHistoryRepository.cs
│   └── QueueHistoryRepository.cs
├── Migrations/
│   └── [SQLite migration files]
└── SeedData/
    └── SeedData.cs
```

---

## 3. Component Architecture

### 3.1 Event Bus

**Purpose:** Decoupled, strongly-typed event communication between modules.

**Interface:**
```csharp
public interface IEventBus
{
    // Publish an event asynchronously
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class;
    
    // Publish with priority
    Task PublishAsync<TEvent>(TEvent @event, EventPriority priority, CancellationToken cancellationToken = default) where TEvent : class;
    
    // Subscribe to an event
    IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;
    
    // Subscribe with priority
    IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler, EventPriority priority) where TEvent : class;
    
    // Unsubscribe
    void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;
}

public enum EventPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}
```

**Example Events:**
```csharp
public record AuthenticationSucceeded(string EmployeeId, string DeviceId, DateTime Timestamp);
public record ConfigurationChanged(string ConfigVersion, DateTime Timestamp);
public record HeartbeatCompleted(string DeviceId, DateTime Timestamp, TimeSpan Duration);
public record PolicyUpdated(string PolicyType, DateTime Timestamp);
public record AgentStarted(string AgentVersion, DateTime Timestamp);
public record AgentStopped(DateTime Timestamp);
public record InternetConnected(DateTime Timestamp);
public record InternetDisconnected(DateTime Timestamp);
public record JobCompleted(string JobId, JobResult Result, DateTime Timestamp);
public record JobFailed(string JobId, Exception Exception, DateTime Timestamp);
```

**Implementation Details:**
- Uses `ConcurrentDictionary` for thread-safe subscription management
- Priority queue for event processing
- Cancellation token support for graceful shutdown
- Async/await throughout to prevent blocking
- Weak reference subscriptions to prevent memory leaks

**Communication Flow:**
```
[Module A] → EventBus.PublishAsync(Event)
    ↓
[EventBus] → Priority Queue → Subscribers
    ↓
[Module B].Subscribe(Event) → Handler invoked
    ↓
[Module C].Subscribe(Event) → Handler invoked
```

---

### 3.2 Worker Framework

**Purpose:** Generic base class for all background tasks. Provides lifecycle, retry, health, and cancellation.

**Interface:**
```csharp
public interface IBackgroundWorker
{
    string Name { get; }
    WorkerState State { get; }
    WorkerHealth Health { get; }
    WorkerConfiguration Configuration { get; }
    
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task PauseAsync(CancellationToken cancellationToken = default);
    Task ResumeAsync(CancellationToken cancellationToken = default);
    Task<WorkerHealth> GetHealthAsync(CancellationToken cancellationToken = default);
}

public abstract class BackgroundWorkerBase : IBackgroundWorker, IHostedService
{
    protected readonly IAgentLogger Logger;
    protected readonly IEventBus EventBus;
    protected readonly CancellationTokenSource WorkerCts = new();
    
    public abstract string Name { get; }
    public WorkerState State { get; protected set; }
    public WorkerHealth Health { get; protected set; }
    public WorkerConfiguration Configuration { get; set; }
    
    // Abstract methods to implement
    protected abstract Task ExecuteAsync(CancellationToken cancellationToken);
    protected abstract Task OnErrorAsync(Exception exception, CancellationToken cancellationToken);
    
    // Built-in retry logic
    protected async Task ExecuteWithRetryAsync(Func<Task> action, CancellationToken cancellationToken);
    
    // Health monitoring
    protected void UpdateHealth(HealthStatus status, string message);
}
```

**Worker States:**
```csharp
public enum WorkerState
{
    Stopped,
    Starting,
    Running,
    Paused,
    Stopping,
    Error
}
```

**Worker Health:**
```csharp
public class WorkerHealth
{
    public HealthStatus Status { get; set; }
    public string Message { get; set; }
    public DateTime LastCheckTime { get; set; }
    public TimeSpan Uptime { get; set; }
    public int ErrorCount { get; set; }
    public int SuccessCount { get; set; }
}

public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}
```

**Worker Configuration:**
```csharp
public class WorkerConfiguration
{
    public TimeSpan ExecutionInterval { get; set; }
    public int MaxRetryCount { get; set; }
    public TimeSpan RetryDelay { get; set; }
    public TimeSpan HealthCheckInterval { get; set; }
    public bool EnableHealthChecks { get; set; }
}
```

**Example Usage:**
```csharp
public class HeartbeatWorker : BackgroundWorkerBase
{
    public HeartbeatWorker(IAgentLogger logger, IEventBus eventBus) 
        : base(logger, eventBus)
    {
        Name = "HeartbeatWorker";
        Configuration = new WorkerConfiguration
        {
            ExecutionInterval = TimeSpan.FromSeconds(60),
            MaxRetryCount = 3,
            RetryDelay = TimeSpan.FromSeconds(5)
        };
    }
    
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Heartbeat logic here
        await _heartbeatService.SendHeartbeatAsync(cancellationToken);
        UpdateHealth(HealthStatus.Healthy, "Heartbeat sent successfully");
    }
    
    protected override async Task OnErrorAsync(Exception exception, CancellationToken cancellationToken)
    {
        Logger.LogError(LogCategory.Heartbeat, "Heartbeat failed", exception);
        await EventBus.PublishAsync(new HeartbeatFailed(exception.Message, DateTime.UtcNow));
    }
}
```

---

### 3.3 Scheduler

**Purpose:** Enterprise scheduling for cron, interval, one-time, dynamic, and configuration-driven schedules.

**Interface:**
```csharp
public interface IScheduler
{
    // Schedule a job with cron expression
    string ScheduleCron(string name, string cronExpression, Func<CancellationToken, Task> job);
    
    // Schedule a job with interval
    string ScheduleInterval(string name, TimeSpan interval, Func<CancellationToken, Task> job);
    
    // Schedule a one-time job
    string ScheduleOneTime(string name, DateTime runAt, Func<CancellationToken, Task> job);
    
    // Schedule from configuration
    string ScheduleFromConfig(string name, ScheduleConfig config, Func<CancellationToken, Task> job);
    
    // Dynamic schedule (update existing)
    void UpdateSchedule(string jobId, ScheduleConfig newConfig);
    
    // Cancel a job
    void CancelSchedule(string jobId);
    
    // Get all scheduled jobs
    IReadOnlyList<ScheduledJob> GetScheduledJobs();
    
    // Start the scheduler
    Task StartAsync(CancellationToken cancellationToken = default);
    
    // Stop the scheduler
    Task StopAsync(CancellationToken cancellationToken = default);
}

public class ScheduleConfig
{
    public ScheduleType Type { get; set; }
    public string? CronExpression { get; set; }
    public TimeSpan? Interval { get; set; }
    public DateTime? RunAt { get; set; }
    public bool Enabled { get; set; }
    public Dictionary<string, string> Parameters { get; set; }
}

public enum ScheduleType
{
    Cron,
    Interval,
    OneTime,
    Dynamic
}
```

**Example Usage:**
```csharp
// Schedule heartbeat every 60 seconds
scheduler.ScheduleInterval("Heartbeat", TimeSpan.FromSeconds(60), async (ct) =>
{
    await heartbeatWorker.ExecuteAsync(ct);
});

// Schedule screenshot capture every 5 minutes (from config)
var config = await configurationService.GetScheduleConfigAsync("ScreenshotCapture");
scheduler.ScheduleFromConfig("ScreenshotCapture", config, async (ct) =>
{
    await screenshotWorker.ExecuteAsync(ct);
});

// Schedule one-time cleanup job
scheduler.ScheduleOneTime("Cleanup", DateTime.UtcNow.AddHours(1), async (ct) =>
{
    await cleanupWorker.ExecuteAsync(ct);
});
```

---

### 3.4 SQLite Database

**Purpose:** Local persistence for agent state, job queue, policies, feature flags, logs, offline events, and queue history.

**Database Schema:**
```sql
-- Agent State
CREATE TABLE AgentState (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    AgentVersion TEXT NOT NULL,
    CurrentState TEXT NOT NULL,
    EmployeeId TEXT,
    DeviceId TEXT,
    LastHeartbeatUtc TEXT,
    LastConfigSyncUtc TEXT,
    IsOnline INTEGER DEFAULT 0,
    CreatedAtUtc TEXT NOT NULL,
    UpdatedAtUtc TEXT NOT NULL
);

-- Job Queue
CREATE TABLE JobQueue (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    JobType TEXT NOT NULL,
    JobPriority INTEGER DEFAULT 0,
    JobState TEXT NOT NULL,
    Payload TEXT NOT NULL,
    RetryCount INTEGER DEFAULT 0,
    MaxRetryCount INTEGER DEFAULT 3,
    Error TEXT,
    CreatedAtUtc TEXT NOT NULL,
    ScheduledAtUtc TEXT,
    StartedAtUtc TEXT,
    CompletedAtUtc TEXT,
    NextRetryAtUtc TEXT
);

-- Policies
CREATE TABLE Policies (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PolicyType TEXT NOT NULL UNIQUE,
    PolicyJson TEXT NOT NULL,
    Version TEXT NOT NULL,
    DownloadedAtUtc TEXT NOT NULL,
    AppliedAtUtc TEXT,
    IsActive INTEGER DEFAULT 1
);

-- Feature Flags
CREATE TABLE FeatureFlags (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FlagName TEXT NOT NULL UNIQUE,
    IsEnabled INTEGER DEFAULT 0,
    Description TEXT,
    DownloadedAtUtc TEXT NOT NULL,
    UpdatedAtUtc TEXT
);

-- Logs
CREATE TABLE Logs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Category TEXT NOT NULL,
    Level TEXT NOT NULL,
    Message TEXT NOT NULL,
    Exception TEXT,
    Properties TEXT,
    LoggedAtUtc TEXT NOT NULL
);

-- Offline Events
CREATE TABLE OfflineEvents (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EventType TEXT NOT NULL,
    EventData TEXT NOT NULL,
    CreatedAtUtc TEXT NOT NULL,
    SyncedAtUtc TEXT,
    SyncStatus TEXT DEFAULT 'Pending'
);

-- Queue History
CREATE TABLE QueueHistory (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    JobId INTEGER NOT NULL,
    JobType TEXT NOT NULL,
    OldState TEXT NOT NULL,
    NewState TEXT NOT NULL,
    ChangedAtUtc TEXT NOT NULL,
    Reason TEXT
);
```

**Repository Pattern:**
```csharp
public interface IAgentStateRepository
{
    Task<AgentState> GetAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AgentState state, CancellationToken cancellationToken = default);
    Task UpdateStateAsync(AgentState newState, CancellationToken cancellationToken = default);
}

public interface IJobQueueRepository
{
    Task EnqueueAsync<T>(T job, JobPriority priority, CancellationToken cancellationToken = default);
    Task<IJob?> DequeueAsync(CancellationToken cancellationToken = default);
    Task UpdateJobStateAsync(string jobId, JobState newState, CancellationToken cancellationToken = default);
    Task<IJob?> GetJobAsync(string jobId, CancellationToken cancellationToken = default);
    Task<List<IJob>> GetPendingJobsAsync(int limit, CancellationToken cancellationToken = default);
    Task<List<IJob>> GetFailedJobsAsync(int limit, CancellationToken cancellationToken = default);
}

public interface IPolicyRepository
{
    Task<TPolicy?> GetPolicyAsync<TPolicy>(string policyType, CancellationToken cancellationToken = default);
    Task SavePolicyAsync<TPolicy>(string policyType, TPolicy policy, CancellationToken cancellationToken = default);
    Task<bool> IsPolicyActiveAsync(string policyType, CancellationToken cancellationToken = default);
}

public interface IFeatureFlagRepository
{
    Task<bool> IsEnabledAsync(string flagName, CancellationToken cancellationToken = default);
    Task SetFlagAsync(string flagName, bool isEnabled, CancellationToken cancellationToken = default);
    Task<FeatureFlag?> GetFlagAsync(string flagName, CancellationToken cancellationToken = default);
    Task<List<FeatureFlag>> GetAllFlagsAsync(CancellationToken cancellationToken = default);
}
```

**SQLite Configuration:**
- WAL mode for concurrent read/write
- Connection pooling
- Automatic migrations on startup
- Backup before migrations
- Periodic VACUUM for maintenance

---

### 3.5 Queue System

**Purpose:** Enterprise job queue with retry, dead letter queue, and priority support.

**Interfaces:**
```csharp
public interface IJob
{
    string JobId { get; }
    string JobType { get; }
    JobPriority Priority { get; }
    JobState State { get; }
    DateTime CreatedAtUtc { get; }
    DateTime? ScheduledAtUtc { get; }
    int RetryCount { get; }
    string? Error { get; }
}

public interface IJobQueue
{
    Task<string> EnqueueAsync<T>(T job, JobPriority priority = JobPriority.Normal, CancellationToken cancellationToken = default);
    Task<IJob?> DequeueAsync(CancellationToken cancellationToken = default);
    Task UpdateJobStateAsync(string jobId, JobState newState, string? error = null, CancellationToken cancellationToken = default);
    Task<List<IJob>> GetPendingJobsAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task<List<IJob>> GetFailedJobsAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task<List<IJob>> GetDeadLetterJobsAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task RetryJobAsync(string jobId, CancellationToken cancellationToken = default);
    Task MoveToDeadLetterAsync(string jobId, CancellationToken cancellationToken = default);
}

public interface IJobProcessor
{
    Task ProcessJobAsync(IJob job, CancellationToken cancellationToken = default);
    Task<bool> CanProcessAsync(IJob job, CancellationToken cancellationToken = default);
}

public interface IQueueWorker : IHostedService
{
    Task StartProcessingAsync(CancellationToken cancellationToken = default);
    Task StopProcessingAsync(CancellationToken cancellationToken = default);
}
```

**Job States:**
```csharp
public enum JobState
{
    Pending,
    Scheduled,
    Running,
    Completed,
    Failed,
    Retrying,
    DeadLetter
}

public enum JobPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}
```

**Queue Processing Flow:**
```
[Module] → IJobQueue.EnqueueAsync(Job)
    ↓
[JobQueue] → SQLite (Pending state)
    ↓
[QueueWorker] → DequeueAsync()
    ↓
[QueueWorker] → IJobProcessor.ProcessJobAsync()
    ↓
[Success] → UpdateJobStateAsync(Completed)
[Failure] → UpdateJobStateAsync(Failed) → Retry if retryCount < maxRetryCount
[MaxRetries] → MoveToDeadLetterAsync()
```

**Example Usage:**
```csharp
// Enqueue a screenshot job
var screenshotJob = new ScreenshotJob
{
    EmployeeId = employeeId,
    DeviceId = deviceId,
    Timestamp = DateTime.UtcNow
};

await jobQueue.EnqueueAsync(screenshotJob, JobPriority.Normal);

// Queue worker processes it
public class ScreenshotJobProcessor : IJobProcessor
{
    public async Task ProcessJobAsync(IJob job, CancellationToken cancellationToken)
    {
        var screenshotJob = (ScreenshotJob)job;
        await screenshotService.CaptureAsync(screenshotJob, cancellationToken);
    }
    
    public Task<bool> CanProcessAsync(IJob job, CancellationToken cancellationToken)
    {
        return Task.FromResult(job is ScreenshotJob);
    }
}
```

---

### 3.6 Policy Engine

**Purpose:** Centralized policy management. Modules query policies instead of reading settings.json directly.

**Interface:**
```csharp
public interface IPolicyEngine
{
    Task<TPolicy> GetPolicyAsync<TPolicy>(CancellationToken cancellationToken = default) where TPolicy : class;
    Task<bool> IsPolicyEnabledAsync(string policyType, CancellationToken cancellationToken = default);
    Task UpdatePolicyAsync<TPolicy>(TPolicy policy, CancellationToken cancellationToken = default) where TPolicy : class;
    Task ReloadPoliciesAsync(CancellationToken cancellationToken = default);
    Task<List<PolicyInfo>> GetAllPoliciesAsync(CancellationToken cancellationToken = default);
}

public class PolicyEngine : IPolicyEngine
{
    private readonly IPolicyRepository _policyRepository;
    private readonly IApiClient _apiClient;
    private readonly IAgentLogger _logger;
    private readonly ConcurrentDictionary<string, object> _policyCache = new();
    
    public async Task<TPolicy> GetPolicyAsync<TPolicy>(CancellationToken cancellationToken = default)
    {
        var policyType = typeof(TPolicy).Name;
        
        // Check cache first
        if (_policyCache.TryGetValue(policyType, out var cachedPolicy))
        {
            return (TPolicy)cachedPolicy;
        }
        
        // Load from SQLite
        var policy = await _policyRepository.GetPolicyAsync<TPolicy>(policyType, cancellationToken);
        
        if (policy == null)
        {
            // Download from backend
            policy = await DownloadPolicyAsync<TPolicy>(policyType, cancellationToken);
            await _policyRepository.SavePolicyAsync(policyType, policy, cancellationToken);
        }
        
        _policyCache[policyType] = policy;
        return policy;
    }
}
```

**Policy Types:**
```csharp
public class ScreenshotPolicy
{
    public bool Enabled { get; set; }
    public int IntervalSeconds { get; set; }
    public int Quality { get; set; }
    public bool CaptureActiveWindowOnly { get; set; }
    public bool CaptureOnIdle { get; set; }
    public int IdleThresholdSeconds { get; set; }
}

public class BrowserPolicy
{
    public bool Enabled { get; set; }
    public List<string> AllowedDomains { get; set; }
    public List<string> BlockedDomains { get; set; }
    public bool TrackIncognito { get; set; }
}

public class ApplicationPolicy
{
    public bool Enabled { get; set; }
    public List<string> MonitoredApplications { get; set; }
    public List<string> BlockedApplications { get; set; }
    public bool TrackIdleTime { get; set; }
}

public class IdlePolicy
{
    public bool Enabled { get; set; }
    public int IdleThresholdSeconds { get; set; }
    public bool PauseMonitoringOnIdle { get; set; }
    public bool NotifyOnIdle { get; set; }
}

public class UsbPolicy
{
    public bool Enabled { get; set; }
    public bool BlockUsbStorage { get; set; }
    public bool LogUsbActivity { get; set; }
    public List<string> AllowedUsbDevices { get; set; }
}
```

**Example Usage:**
```csharp
// Module queries policy
var screenshotPolicy = await policyEngine.GetPolicyAsync<ScreenshotPolicy>();

if (screenshotPolicy.Enabled)
{
    await captureScreenshotAsync(screenshotPolicy);
}
```

---

### 3.7 Feature Flags

**Purpose:** Remote feature flag management. Flags are downloaded from the backend.

**Interface:**
```csharp
public interface IFeatureFlagManager
{
    Task<bool> IsEnabledAsync(string flagName, CancellationToken cancellationToken = default);
    Task SetFlagAsync(string flagName, bool isEnabled, CancellationToken cancellationToken = default);
    Task<FeatureFlag?> GetFlagAsync(string flagName, CancellationToken cancellationToken = default);
    Task<List<FeatureFlag>> GetAllFlagsAsync(CancellationToken cancellationToken = default);
    Task DownloadFlagsAsync(CancellationToken cancellationToken = default);
    Task ReloadFlagsAsync(CancellationToken cancellationToken = default);
}

public class FeatureFlag
{
    public string FlagName { get; set; }
    public bool IsEnabled { get; set; }
    public string? Description { get; set; }
    public DateTime DownloadedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
```

**Flag Names:**
```csharp
public static class FlagName
{
    public const string EnableScreenshot = "EnableScreenshot";
    public const string EnableBrowserMonitoring = "EnableBrowserMonitoring";
    public const string EnableApplicationMonitoring = "EnableApplicationMonitoring";
    public const string EnableUSBMonitoring = "EnableUSBMonitoring";
    public const string EnableAutoUpdate = "EnableAutoUpdate";
    public const string EnableIdleDetection = "EnableIdleDetection";
    public const string EnableCloudStorage = "EnableCloudStorage";
}
```

**Example Usage:**
```csharp
// Check if screenshot is enabled
if (await featureFlagManager.IsEnabledAsync(FlagName.EnableScreenshot))
{
    await scheduler.ScheduleFromConfig("ScreenshotCapture", config, executeScreenshot);
}
```

---

### 3.8 Health Monitor

**Purpose:** System health monitoring for CPU, RAM, Disk, Internet, Queue, Database, and Services.

**Interface:**
```csharp
public interface IHealthMonitor
{
    Task<HealthStatus> GetOverallHealthAsync(CancellationToken cancellationToken = default);
    Task<CpuMetric> GetCpuMetricAsync(CancellationToken cancellationToken = default);
    Task<RamMetric> GetRamMetricAsync(CancellationToken cancellationToken = default);
    Task<DiskMetric> GetDiskMetricAsync(CancellationToken cancellationToken = default);
    Task<InternetMetric> GetInternetMetricAsync(CancellationToken cancellationToken = default);
    Task<QueueMetric> GetQueueMetricAsync(CancellationToken cancellationToken = default);
    Task<DatabaseMetric> GetDatabaseMetricAsync(CancellationToken cancellationToken = default);
    Task<ServiceMetric> GetServiceMetricAsync(string serviceName, CancellationToken cancellationToken = default);
    Task<List<HealthMetric>> GetAllMetricsAsync(CancellationToken cancellationToken = default);
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);
    Task StopMonitoringAsync(CancellationToken cancellationToken = default);
}

public class HealthMetric
{
    public string Name { get; set; }
    public HealthStatus Status { get; set; }
    public string Message { get; set; }
    public DateTime MeasuredAtUtc { get; set; }
    public Dictionary<string, object> Data { get; set; }
}

public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}
```

**Metric Types:**
```csharp
public class CpuMetric : HealthMetric
{
    public double CpuPercent { get; set; }
    public int ProcessCount { get; set; }
}

public class RamMetric : HealthMetric
{
    public long TotalBytes { get; set; }
    public long AvailableBytes { get; set; }
    public long UsedBytes { get; set; }
    public double UsedPercent { get; set; }
}

public class DiskMetric : HealthMetric
{
    public string DriveLetter { get; set; }
    public long TotalBytes { get; set; }
    public long AvailableBytes { get; set; }
    public long UsedBytes { get; set; }
    public double UsedPercent { get; set; }
}

public class InternetMetric : HealthMetric
{
    public bool IsConnected { get; set; }
    public string? IpAddress { get; set; }
    public double? LatencyMs { get; set; }
    public DateTime? LastConnectedUtc { get; set; }
}

public class QueueMetric : HealthMetric
{
    public int PendingCount { get; set; }
    public int RunningCount { get; set; }
    public int FailedCount { get; set; }
    public int DeadLetterCount { get; set; }
}

public class DatabaseMetric : HealthMetric
{
    public bool IsConnected { get; set; }
    public long DatabaseSizeBytes { get; set; }
    public TimeSpan? LastBackupAge { get; set; }
}

public class ServiceMetric : HealthMetric
{
    public string ServiceName { get; set; }
    public bool IsRunning { get; set; }
    public TimeSpan Uptime { get; set; }
    public int ErrorCount { get; set; }
}
```

**Example Usage:**
```csharp
// Get overall health
var health = await healthMonitor.GetOverallHealthAsync();

if (health.Status == HealthStatus.Unhealthy)
{
    await eventBus.PublishAsync(new SystemUnhealthy(health.Message, DateTime.UtcNow));
}

// Get specific metric
var cpuMetric = await healthMonitor.GetCpuMetricAsync();
if (cpuMetric.CpuPercent > 90)
{
    await eventBus.PublishAsync(new HighCpuUsage(cpuMetric.CpuPercent, DateTime.UtcNow));
}
```

---

### 3.9 Module Manager (Enhanced)

**Purpose:** Enhanced module lifecycle management with load, unload, enable, disable, restart, dependencies, health, version, and permissions.

**Interface:**
```csharp
public interface IModuleManager
{
    // Lifecycle
    Task LoadModuleAsync(string moduleName, CancellationToken cancellationToken = default);
    Task UnloadModuleAsync(string moduleName, CancellationToken cancellationToken = default);
    Task EnableModuleAsync(string moduleName, CancellationToken cancellationToken = default);
    Task DisableModuleAsync(string moduleName, CancellationToken cancellationToken = default);
    Task RestartModuleAsync(string moduleName, CancellationToken cancellationToken = default);
    
    // State
    Task<ModuleState> GetModuleStateAsync(string moduleName, CancellationToken cancellationToken = default);
    Task<WorkerHealth> GetModuleHealthAsync(string moduleName, CancellationToken cancellationToken = default);
    Task<string> GetModuleVersionAsync(string moduleName, CancellationToken cancellationToken = default);
    
    // Dependencies
    Task<List<string>> GetModuleDependenciesAsync(string moduleName, CancellationToken cancellationToken = default);
    Task<bool> CheckDependenciesAsync(string moduleName, CancellationToken cancellationToken = default);
    
    // Permissions
    Task<List<string>> GetModulePermissionsAsync(string moduleName, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(string moduleName, string permission, CancellationToken cancellationToken = default);
    
    // Discovery
    Task<List<ModuleInfo>> GetAllModulesAsync(CancellationToken cancellationToken = default);
    Task<ModuleInfo?> GetModuleInfoAsync(string moduleName, CancellationToken cancellationToken = default);
}

public class ModuleInfo
{
    public string Name { get; set; }
    public string Version { get; set; }
    public ModuleState State { get; set; }
    public WorkerHealth Health { get; set; }
    public List<string> Dependencies { get; set; }
    public List<string> Permissions { get; set; }
    public DateTime? LoadedAtUtc { get; set; }
    public DateTime? LastStartedUtc { get; set; }
}
```

**Module States:**
```csharp
public enum ModuleState
{
    Unloaded,
    Loaded,
    Enabled,
    Disabled,
    Starting,
    Running,
    Stopping,
    Error
}
```

**Example Usage:**
```csharp
// Load a module
await moduleManager.LoadModuleAsync("ScreenshotModule");

// Check dependencies
var dependencies = await moduleManager.GetModuleDependenciesAsync("ScreenshotModule");
if (await moduleManager.CheckDependenciesAsync("ScreenshotModule"))
{
    await moduleManager.EnableModuleAsync("ScreenshotModule");
}

// Get module health
var health = await moduleManager.GetModuleHealthAsync("ScreenshotModule");
if (health.Status == HealthStatus.Unhealthy)
{
    await moduleManager.RestartModuleAsync("ScreenshotModule");
}
```

---

### 3.10 State Machine

**Purpose:** Agent state machine with validation. Invalid transitions throw exceptions.

**Interface:**
```csharp
public interface IAgentStateMachine
{
    AgentState CurrentState { get; }
    Task TransitionToAsync(AgentState newState, CancellationToken cancellationToken = default);
    Task<bool> CanTransitionToAsync(AgentState newState, CancellationToken cancellationToken = default);
    Task<List<AgentState>> GetValidTransitionsAsync(CancellationToken cancellationToken = default);
    event EventHandler<StateTransitionEventArgs>? StateChanged;
}

public class StateTransitionEventArgs : EventArgs
{
    public AgentState OldState { get; set; }
    public AgentState NewState { get; set; }
    public DateTime TransitionedAtUtc { get; set; }
    public string? Reason { get; set; }
}
```

**Agent States:**
```csharp
public enum AgentState
{
    Starting,
    Authenticating,
    Ready,
    Monitoring,
    Paused,
    Offline,
    Updating,
    Stopping,
    Stopped,
    Disconnected
}
```

**Valid Transitions:**
```
Starting → Authenticating
Authenticating → Ready
Authenticating → Disconnected
Ready → Monitoring
Ready → Paused
Ready → Offline
Ready → Updating
Ready → Stopping
Monitoring → Paused
Monitoring → Offline
Monitoring → Updating
Monitoring → Stopping
Paused → Monitoring
Paused → Stopping
Offline → Ready
Offline → Stopping
Updating → Ready
Updating → Stopping
Stopping → Stopped
Stopped → Starting
Disconnected → Starting
```

**Example Usage:**
```csharp
// Transition to monitoring
if (await stateMachine.CanTransitionToAsync(AgentState.Monitoring))
{
    await stateMachine.TransitionToAsync(AgentState.Monitoring);
}
else
{
    throw new InvalidStateTransitionException(stateMachine.CurrentState, AgentState.Monitoring);
}

// Listen to state changes
stateMachine.StateChanged += (sender, args) =>
{
    logger.LogInformation(LogCategory.Application, 
        "State changed from {OldState} to {NewState}", 
        args.OldState, args.NewState);
};
```

---

### 3.11 Storage Provider

**Purpose:** Cloud storage abstraction. Modules don't know if it's S3, R2, or Azure.

**Interface:**
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

public class StorageRequest
{
    public string Key { get; set; }
    public Stream Content { get; set; }
    public string ContentType { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public string? BucketName { get; set; }
}

public class StorageResponse
{
    public bool Success { get; set; }
    public string? Key { get; set; }
    public string? Url { get; set; }
    public long? SizeBytes { get; set; }
    public string? ETag { get; set; }
    public DateTime? UploadedAtUtc { get; set; }
    public string? Error { get; set; }
}

public class StorageException : Exception
{
    public string ProviderName { get; }
    public string? Key { get; }
    
    public StorageException(string providerName, string message) 
        : base(message)
    {
        ProviderName = providerName;
    }
}
```

**Amazon S3 Implementation:**
```csharp
public class AmazonS3StorageProvider : IStorageProvider
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    
    public string ProviderName => "AmazonS3";
    
    public AmazonS3StorageProvider(IAmazonS3 s3Client, string bucketName)
    {
        _s3Client = s3Client;
        _bucketName = bucketName;
    }
    
    public async Task<StorageResponse> UploadAsync(StorageRequest request, CancellationToken cancellationToken = default)
    {
        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = request.Key,
            InputStream = request.Content,
            ContentType = request.ContentType,
            Metadata = request.Metadata
        };
        
        var response = await _s3Client.PutObjectAsync(putRequest, cancellationToken);
        
        return new StorageResponse
        {
            Success = true,
            Key = request.Key,
            Url = $"https://{_bucketName}.s3.amazonaws.com/{request.Key}",
            SizeBytes = response.ContentLength,
            ETag = response.ETag,
            UploadedAtUtc = DateTime.UtcNow
        };
    }
    
    // Other methods...
}
```

**Storage Provider Factory:**
```csharp
public class StorageProviderFactory
{
    private readonly IConfiguration _configuration;
    private readonly IAgentLogger _logger;
    
    public IStorageProvider CreateProvider(string providerType)
    {
        return providerType.ToLowerInvariant() switch
        {
            "amazons3" => new AmazonS3StorageProvider(
                new AmazonS3Client(_configuration["AWS:AccessKey"], _configuration["AWS:SecretKey"], RegionEndpoint.USEast1),
                _configuration["AWS:BucketName"]
            ),
            "cloudflarer2" => new CloudflareR2StorageProvider(/* future */),
            "azure" => new AzureStorageProvider(/* future */),
            _ => throw new ArgumentException($"Unknown storage provider: {providerType}")
        };
    }
}
```

**Example Usage:**
```csharp
// Upload screenshot
var storageProvider = storageProviderFactory.CreateProvider("AmazonS3");

var request = new StorageRequest
{
    Key = $"screenshots/{deviceId}/{timestamp}.png",
    Content = screenshotStream,
    ContentType = "image/png",
    Metadata = new Dictionary<string, string>
    {
        ["EmployeeId"] = employeeId,
        ["DeviceId"] = deviceId,
        ["Timestamp"] = timestamp.ToString("O")
    }
};

var response = await storageProvider.UploadAsync(request);
```

---

### 3.12 Background Services

**Purpose:** Convert runtime services to Microsoft.Extensions.Hosting BackgroundService with cancellation tokens.

**Implementation:**
```csharp
public class EventQueueBackgroundService : BackgroundService
{
    private readonly IEventBus _eventBus;
    private readonly IAgentLogger _logger;
    
    public EventQueueBackgroundService(IEventBus eventBus, IAgentLogger logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(LogCategory.Application, "Event Queue Service started");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Process events
                await Task.Delay(100, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogCategory.Exception, "Event Queue Service error", ex);
            }
        }
        
        _logger.LogInformation(LogCategory.Application, "Event Queue Service stopped");
    }
}

public class QueueWorkerBackgroundService : BackgroundService
{
    private readonly IQueueWorker _queueWorker;
    private readonly IAgentLogger _logger;
    
    public QueueWorkerBackgroundService(IQueueWorker queueWorker, IAgentLogger logger)
    {
        _queueWorker = queueWorker;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(LogCategory.Application, "Queue Worker Service started");
        
        await _queueWorker.StartProcessingAsync(stoppingToken);
        
        // Keep running until cancellation
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
        
        await _queueWorker.StopProcessingAsync(stoppingToken);
        
        _logger.LogInformation(LogCategory.Application, "Queue Worker Service stopped");
    }
}

public class HealthMonitorBackgroundService : BackgroundService
{
    private readonly IHealthMonitor _healthMonitor;
    private readonly IAgentLogger _logger;
    
    public HealthMonitorBackgroundService(IHealthMonitor healthMonitor, IAgentLogger logger)
    {
        _healthMonitor = healthMonitor;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(LogCategory.Application, "Health Monitor Service started");
        
        await _healthMonitor.StartMonitoringAsync(stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var health = await _healthMonitor.GetOverallHealthAsync(stoppingToken);
                
                if (health.Status == HealthStatus.Unhealthy)
                {
                    _logger.LogWarning(LogCategory.Application, "System unhealthy: {Message}", health.Message);
                }
                
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogCategory.Exception, "Health Monitor error", ex);
            }
        }
        
        await _healthMonitor.StopMonitoringAsync(stoppingToken);
        
        _logger.LogInformation(LogCategory.Application, "Health Monitor Service stopped");
    }
}
```

**DI Registration:**
```csharp
services.AddHostedService<EventQueueBackgroundService>();
services.AddHostedService<QueueWorkerBackgroundService>();
services.AddHostedService<HealthMonitorBackgroundService>();
services.AddHostedService<SchedulerBackgroundService>();
```

---

## 4. Backend Extensions

### 4.1 New Database Tables

```prisma
model Policy {
  id              String   @id @default(uuid())
  policyType      String   @unique @map("policy_type")
  companyId       String   @map("company_id")
  policyJson       Json     @map("policy_json")
  version         String
  isActive        Boolean  @default(true) @map("is_active")
  createdAt       DateTime @default(now()) @map("created_at")
  updatedAt       DateTime @updatedAt @map("updated_at")

  @@index([companyId])
  @@index([policyType])
  @@map("policies")
}

model FeatureFlag {
  id              String   @id @default(uuid())
  flagName        String   @unique @map("flag_name")
  companyId       String?  @map("company_id")
  isEnabled       Boolean  @default(false) @map("is_enabled")
  description     String?
  createdAt       DateTime @default(now()) @map("created_at")
  updatedAt       DateTime @updatedAt @map("updated_at")

  @@index([companyId])
  @@index([flagName])
  @@map("feature_flags")
}

model AgentEvent {
  id              String   @id @default(uuid())
  deviceId        String   @map("device_id")
  employeeId      String   @map("employee_id")
  companyId       String   @map("company_id")
  eventType       String   @map("event_type")
  eventData       Json     @map("event_data")
  occurredAt      DateTime @default(now()) @map("occurred_at")
  receivedAt      DateTime @default(now()) @map("received_at")

  device          EmployeeDevice @relation(fields: [deviceId], references: [id], onDelete: Cascade)

  @@index([deviceId])
  @@index([employeeId])
  @@index([companyId])
  @@index([eventType])
  @@index([occurredAt])
  @@map("agent_events")
}

model HealthReport {
  id              String   @id @default(uuid())
  deviceId        String   @map("device_id")
  employeeId      String   @map("employee_id")
  companyId       String   @map("company_id")
  cpuPercent      Float    @map("cpu_percent")
  ramUsedMb       Int      @map("ram_used_mb")
  diskUsedGb      Int      @map("disk_used_gb")
  isOnline        Boolean  @map("is_online")
  queueSize       Int      @map("queue_size")
  databaseStatus  String   @map("database_status")
  serviceStatus   Json     @map("service_status")
  reportedAt      DateTime @default(now()) @map("reported_at")

  device          EmployeeDevice @relation(fields: [deviceId], references: [id], onDelete: Cascade)

  @@index([deviceId])
  @@index([employeeId])
  @@index([companyId])
  @@index([reportedAt])
  @@map("health_reports")
}

model QueueJob {
  id              String   @id @default(uuid())
  deviceId        String   @map("device_id")
  employeeId      String   @map("employee_id")
  companyId       String   @map("company_id")
  jobType         String   @map("job_type")
  jobPriority     Int      @map("job_priority")
  jobState        String   @map("job_state")
  payload         Json
  retryCount      Int      @default(0) @map("retry_count")
  maxRetryCount   Int      @default(3) @map("max_retry_count")
  error           String?
  createdAt       DateTime @default(now()) @map("created_at")
  scheduledAt     DateTime? @map("scheduled_at")
  startedAt       DateTime? @map("started_at")
  completedAt     DateTime? @map("completed_at")
  nextRetryAt     DateTime? @map("next_retry_at")

  device          EmployeeDevice @relation(fields: [deviceId], references: [id], onDelete: Cascade)

  @@index([deviceId])
  @@index([employeeId])
  @@index([companyId])
  @@index([jobState])
  @@index([scheduledAt])
  @@map("queue_jobs")
}
```

### 4.2 New API Endpoints

#### Policies
```
GET    /api/agent/policies/:policyType
POST   /api/agent/policies/:policyType
PUT    /api/agent/policies/:policyType
DELETE /api/agent/policies/:policyType
GET    /api/agent/policies
```

#### Feature Flags
```
GET    /api/agent/feature-flags
GET    /api/agent/feature-flags/:flagName
POST   /api/agent/feature-flags/:flagName
PUT    /api/agent/feature-flags/:flagName
DELETE /api/agent/feature-flags/:flagName
```

#### Events
```
POST   /api/agent/events
GET    /api/agent/events/:deviceId
GET    /api/agent/events/:deviceId/:eventType
```

#### Health
```
POST   /api/agent/health
GET    /api/agent/health/:deviceId
```

#### Queue
```
POST   /api/agent/queue/jobs
GET    /api/agent/queue/jobs/:deviceId
PUT    /api/agent/queue/jobs/:jobId/retry
PUT    /api/agent/queue/jobs/:jobId/dead-letter
GET    /api/agent/queue/stats
```

---

## 5. Communication Flow

### 5.1 Module Communication via Event Bus

```
[HeartbeatWorker] → EventBus.PublishAsync(HeartbeatCompleted)
    ↓
[EventBus] → Priority Queue
    ↓
[PolicyEngine].Subscribe(HeartbeatCompleted) → Update policies if needed
[HealthMonitor].Subscribe(HeartbeatCompleted) → Update health metrics
[QueueWorker].Subscribe(HeartbeatCompleted) → Process queued jobs
```

### 5.2 Worker Lifecycle

```
[ModuleManager] → LoadModuleAsync()
    ↓
[BackgroundWorkerBase] → StartAsync()
    ↓
[Worker] → State = Starting
    ↓
[Worker] → ExecuteAsync() → Loop with ExecutionInterval
    ↓
[Worker] → State = Running
    ↓
[Worker] → Health = Healthy
    ↓
[ModuleManager] → StopAsync()
    ↓
[Worker] → State = Stopping
    ↓
[Worker] → Cancel CancellationToken
    ↓
[Worker] → State = Stopped
```

### 5.3 Queue Processing Flow

```
[ScreenshotModule] → IJobQueue.EnqueueAsync(ScreenshotJob)
    ↓
[JobQueue] → SQLite (Pending)
    ↓
[QueueWorker] → DequeueAsync()
    ↓
[QueueWorker] → IJobProcessor.ProcessJobAsync()
    ↓
[ScreenshotJobProcessor] → Capture screenshot
    ↓
[ScreenshotJobProcessor] → IStorageProvider.UploadAsync()
    ↓
[Success] → UpdateJobStateAsync(Completed)
[Failure] → UpdateJobStateAsync(Failed) → Retry
[MaxRetries] → MoveToDeadLetterAsync()
```

### 5.4 Policy Flow

```
[Module] → IPolicyEngine.GetPolicyAsync<ScreenshotPolicy>()
    ↓
[PolicyEngine] → Check cache
    ↓
[Cache Miss] → IPolicyRepository.GetPolicyAsync()
    ↓
[SQLite Miss] → IApiClient.DownloadPolicyAsync()
    ↓
[Backend] → GET /api/agent/policies/ScreenshotPolicy
    ↓
[PolicyEngine] → Cache policy
    ↓
[PolicyEngine] → Save to SQLite
    ↓
[Module] → Use policy
```

---

## 6. Dependency Graph

```
RDCS.EmployeeAgent.UI
    ↓
RDCS.EmployeeAgent.Services
    ↓
RDCS.EmployeeAgent.Runtime (NEW)
    ↓
RDCS.EmployeeAgent.Persistence (NEW)
    ↓
RDCS.EmployeeAgent.Infrastructure
    ↓
RDCS.EmployeeAgent.Core
    ↓
RDCS.EmployeeAgent.Shared
```

**Runtime Dependencies:**
```
EventBus
    → Core (IEventBus)
    → Shared (Result, Guards)

Workers
    → Core (IAgentLogger)
    → Runtime (EventBus)
    → Shared (DateTimeProvider)

Scheduler
    → Core (IAgentLogger)
    → Runtime (Workers)
    → Shared (DateTimeProvider)

Queue
    → Core (IAgentLogger)
    → Persistence (IJobQueueRepository)
    → Runtime (EventBus)

Policy
    → Core (IAgentLogger)
    → Persistence (IPolicyRepository)
    → Infrastructure (IApiClient)

FeatureFlags
    → Core (IAgentLogger)
    → Persistence (IFeatureFlagRepository)
    → Infrastructure (IApiClient)

Health
    → Core (IAgentLogger)
    → Runtime (EventBus)
    → Infrastructure (IDeviceInfoProvider)

StateMachine
    → Core (IAgentLogger)
    → Runtime (EventBus)

Storage
    → Core (IAgentLogger)
    → Runtime (EventBus)
```

**Persistence Dependencies:**
```
SQLite
    → Core (IAgentLogger)
    → Shared (Result)

Repositories
    → Core (IAgentLogger)
    → Persistence (SQLite)
    → Shared (Result)
```

---

## 7. Threading Model

### 7.1 Background Services

Each BackgroundService runs on its own thread:
- **EventQueueBackgroundService** - Processes event queue
- **QueueWorkerBackgroundService** - Processes job queue
- **HealthMonitorBackgroundService** - Monitors system health
- **SchedulerBackgroundService** - Executes scheduled jobs

### 7.2 Worker Execution

Workers execute on the BackgroundService thread but can spawn additional tasks:
```csharp
protected override async Task ExecuteAsync(CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        await ExecuteWorkAsync(cancellationToken);
        await Task.Delay(Configuration.ExecutionInterval, cancellationToken);
    }
}
```

### 7.3 Event Bus

EventBus uses `ConcurrentDictionary` for thread-safe subscription management:
- Publishers don't wait for subscribers (fire-and-forget)
- Subscribers execute on ThreadPool threads
- Priority queue ensures critical events are processed first

### 7.4 Queue Processing

QueueWorker uses a single thread but can process multiple jobs concurrently:
```csharp
public async Task StartProcessingAsync(CancellationToken cancellationToken)
{
    var tasks = new List<Task>();
    
    while (!cancellationToken.IsCancellationRequested)
    {
        var job = await _jobQueue.DequeueAsync(cancellationToken);
        if (job != null)
        {
            tasks.Add(ProcessJobAsync(job, cancellationToken));
        }
        
        // Limit concurrent jobs
        if (tasks.Count >= MaxConcurrentJobs)
        {
            await Task.WhenAny(tasks);
            tasks.RemoveAll(t => t.IsCompleted);
        }
    }
    
    await Task.WhenAll(tasks);
}
```

### 7.5 SQLite Access

SQLite uses WAL mode for concurrent read/write:
- Multiple readers, single writer
- Connection pooling
- Repository methods use async/await to avoid blocking

---

## 8. Worker Lifecycle

### 8.1 States

```
Stopped → Starting → Running → Paused → Stopping → Stopped
                    ↓
                  Error → Stopping → Stopped
```

### 8.2 Lifecycle Methods

```csharp
public async Task StartAsync(CancellationToken cancellationToken)
{
    State = WorkerState.Starting;
    await OnStartingAsync(cancellationToken);
    
    _workerTask = Task.Run(() => ExecuteAsync(cancellationToken), cancellationToken);
    
    State = WorkerState.Running;
    await OnStartedAsync(cancellationToken);
}

public async Task StopAsync(CancellationToken cancellationToken)
{
    State = WorkerState.Stopping;
    await OnStoppingAsync(cancellationToken);
    
    _workerCts.Cancel();
    await _workerTask;
    
    State = WorkerState.Stopped;
    await OnStoppedAsync(cancellationToken);
}

public async Task PauseAsync(CancellationToken cancellationToken)
{
    State = WorkerState.Paused;
    await OnPausedAsync(cancellationToken);
}

public async Task ResumeAsync(CancellationToken cancellationToken)
{
    State = WorkerState.Running;
    await OnResumedAsync(cancellationToken);
}
```

### 8.3 Health Monitoring

Workers report health at regular intervals:
```csharp
protected void UpdateHealth(HealthStatus status, string message)
{
    Health = new WorkerHealth
    {
        Status = status,
        Message = message,
        LastCheckTime = DateTime.UtcNow,
        Uptime = DateTime.UtcNow - _startTime,
        ErrorCount = _errorCount,
        SuccessCount = _successCount
    };
    
    _eventBus.PublishAsync(new WorkerHealthChanged(Name, Health, DateTime.UtcNow));
}
```

---

## 9. Queue Lifecycle

### 9.1 Job States

```
Pending → Scheduled → Running → Completed
                    ↓
                  Failed → Retrying → Running
                    ↓
                  DeadLetter
```

### 9.2 Job Creation

```csharp
public async Task<string> EnqueueAsync<T>(T job, JobPriority priority, CancellationToken cancellationToken)
{
    var jobEntity = new JobQueueEntity
    {
        JobType = typeof(T).Name,
        JobPriority = (int)priority,
        JobState = JobState.Pending.ToString(),
        Payload = JsonSerializer.Serialize(job),
        CreatedAtUtc = DateTime.UtcNow
    };
    
    await _repository.AddAsync(jobEntity, cancellationToken);
    
    await _eventBus.PublishAsync(new JobEnqueued(jobEntity.Id, jobEntity.JobType, DateTime.UtcNow));
    
    return jobEntity.Id.ToString();
}
```

### 9.3 Job Processing

```csharp
public async Task ProcessJobAsync(IJob job, CancellationToken cancellationToken)
{
    await _repository.UpdateJobStateAsync(job.JobId, JobState.Running, cancellationToken);
    
    try
    {
        await _processor.ProcessJobAsync(job, cancellationToken);
        await _repository.UpdateJobStateAsync(job.JobId, JobState.Completed, cancellationToken);
        await _eventBus.PublishAsync(new JobCompleted(job.JobId, DateTime.UtcNow));
    }
    catch (Exception ex)
    {
        if (job.RetryCount < job.MaxRetryCount)
        {
            await _repository.UpdateJobStateAsync(job.JobId, JobState.Retrying, ex.Message, cancellationToken);
            await _repository.ScheduleRetryAsync(job.JobId, cancellationToken);
            await _eventBus.PublishAsync(new JobFailed(job.JobId, ex, DateTime.UtcNow));
        }
        else
        {
            await _repository.MoveToDeadLetterAsync(job.JobId, cancellationToken);
            await _eventBus.PublishAsync(new JobDeadLettered(job.JobId, ex, DateTime.UtcNow));
        }
    }
}
```

### 9.4 Retry Logic

```csharp
public async Task ScheduleRetryAsync(string jobId, CancellationToken cancellationToken)
{
    var job = await _repository.GetJobAsync(jobId, cancellationToken);
    var delay = TimeSpan.FromSeconds(Math.Pow(2, job.RetryCount));
    
    await _repository.UpdateRetryAsync(jobId, job.RetryCount + 1, DateTime.UtcNow.Add(delay), cancellationToken);
}
```

---

## 10. Policy Flow

### 10.1 Policy Loading

```
[Module] → IPolicyEngine.GetPolicyAsync<T>()
    ↓
[PolicyEngine] → Check cache
    ↓
[Cache Miss] → IPolicyRepository.GetPolicyAsync()
    ↓
[SQLite Miss] → IApiClient.DownloadPolicyAsync()
    ↓
[Backend] → GET /api/agent/policies/:policyType
    ↓
[PolicyEngine] → Cache policy
    ↓
[PolicyEngine] → Save to SQLite
    ↓
[Module] → Use policy
```

### 10.2 Policy Update

```
[Backend] → Policy changed
    ↓
[Backend] → WebSocket push / Poll
    ↓
[Agent] → IPolicyEngine.ReloadPoliciesAsync()
    ↓
[PolicyEngine] → Download from backend
    ↓
[PolicyEngine] → Update cache
    ↓
[PolicyEngine] → Save to SQLite
    ↓
[PolicyEngine] → Publish PolicyUpdated event
    ↓
[Modules] → React to policy change
```

---

## 11. Module Loading

### 11.1 Module Discovery

```
[ModuleManager] → Scan assemblies for IAgentModule
    ↓
[ModuleManager] → Register in ModuleRegistry
    ↓
[ModuleManager] → Check dependencies
    ↓
[ModuleManager] → Load in dependency order
```

### 11.2 Module Loading Flow

```
[ModuleManager] → LoadModuleAsync("ScreenshotModule")
    ↓
[ModuleManager] → Check dependencies
    ↓
[Dependencies OK] → Load dependencies first
    ↓
[ModuleManager] → Create module instance
    ↓
[ModuleManager] → Call InitializeAsync()
    ↓
[Module] → State = Loaded
    ↓
[ModuleManager] → Publish ModuleLoaded event
```

### 11.3 Module Startup

```
[ModuleHost] → StartAllModulesAsync()
    ↓
[ModuleHost] → Sort by dependencies
    ↓
[ModuleHost] → StartAsync() each module
    ↓
[Module] → State = Starting
    ↓
[Module] → ExecuteAsync()
    ↓
[Module] → State = Running
    ↓
[ModuleHost] → Publish ModuleStarted event
```

---

## 12. Scalability Considerations

### 12.1 SQLite Performance

- **WAL Mode** for concurrent read/write
- **Connection Pooling** to limit connections
- **Indexes** on frequently queried columns
- **Periodic VACUUM** for maintenance
- **Query Optimization** with prepared statements

### 12.2 Queue Performance

- **Batch Processing** for multiple jobs
- **Priority Queue** for critical jobs
- **Concurrent Workers** for parallel processing
- **Backpressure** to prevent memory issues
- **Dead Letter Queue** for failed jobs

### 12.3 Event Bus Performance

- **Weak References** to prevent memory leaks
- **Priority Queue** for critical events
- **Async Subscribers** to prevent blocking
- **Event Batching** for high-frequency events
- **Circuit Breaker** for failing subscribers

### 12.4 Memory Management

- **Object Pooling** for frequent allocations
- **Stream Disposal** for large files
- **Cache Eviction** for memory pressure
- **GC Optimization** with struct usage
- **Memory Limits** for queue size

---

## 13. Offline Operation

### 13.1 Offline Detection

```
[HealthMonitor] → InternetMetric.IsConnected = false
    ↓
[HealthMonitor] → Publish InternetDisconnected event
    ↓
[StateMachine] → TransitionTo(Offline)
    ↓
[Modules] → React to offline state
```

### 13.2 Offline Queuing

```
[Module] → Need to perform operation
    ↓
[Module] → Check InternetMetric
    ↓
[Offline] → Enqueue to IJobQueue
    ↓
[JobQueue] → Save to SQLite
    ↓
[JobQueue] → Mark as Pending
```

### 13.3 Offline Sync

```
[HealthMonitor] → InternetMetric.IsConnected = true
    ↓
[HealthMonitor] → Publish InternetConnected event
    ↓
[StateMachine] → TransitionTo(Ready)
    ↓
[QueueWorker] → Process pending jobs
    ↓
[QueueWorker] → Upload to backend/cloud
```

---

## 14. Testing Strategy

### 14.1 Unit Tests

**Runtime Components:**
- EventBus: Publish, Subscribe, Unsubscribe, Priority
- Workers: Start, Stop, Pause, Resume, Health
- Scheduler: Cron, Interval, One-time schedules
- Queue: Enqueue, Dequeue, Retry, Dead Letter
- Policy Engine: Get, Update, Cache
- Feature Flags: Enable, Disable, Download
- Health Monitor: Metrics collection
- State Machine: Transitions, Validation
- Storage Provider: Upload, Download, Delete

**Persistence Components:**
- SQLite: CRUD operations
- Repositories: All repository methods
- Migrations: Up/Down migrations

### 14.2 Integration Tests

**End-to-End Flows:**
- Module loading and startup
- Job queue processing
- Policy download and application
- Feature flag synchronization
- Offline detection and sync
- Storage provider integration

**Backend Integration:**
- Policy API endpoints
- Feature flag API endpoints
- Event API endpoints
- Health API endpoints
- Queue API endpoints

### 14.3 Mock Services

- Mock IEventBus for testing subscribers
- Mock IJobQueue for testing workers
- Mock IApiClient for testing policy download
- Mock IStorageProvider for testing uploads
- Mock IHealthMonitor for testing health-dependent logic

---

## 15. Implementation Roadmap

### Phase 2 — Agent Runtime Infrastructure

| Milestone | Deliverables |
| --- | --- |
| **2.1 Architecture** | Generate Phase 2 architecture document |
| **2.2 Event Bus** | IEventBus, EventBus, EventSubscription, EventPriority, Event definitions |
| **2.3 Worker Framework** | IBackgroundWorker, BackgroundWorkerBase, WorkerState, WorkerHealth, WorkerConfiguration |
| **2.4 Scheduler** | IScheduler, Scheduler, ScheduleType, ScheduledJob, CronExpression |
| **2.5 SQLite** | SQLiteContext, Repositories, Migrations, SeedData, DatabaseInitializer |
| **2.6 Queue System** | IJobQueue, IJobProcessor, QueueWorker, JobState, JobPriority, IJob implementations |
| **2.7 Policy Engine** | IPolicyEngine, PolicyEngine, Policy definitions, PolicyRepository |
| **2.8 Feature Flags** | IFeatureFlagManager, FeatureFlagManager, FlagName, FeatureFlagRepository |
| **2.9 Health Monitor** | IHealthMonitor, HealthMonitor, HealthMetric implementations |
| **2.10 Module Manager** | Enhanced IModuleManager, ModuleInfo, ModuleState, dependencies, permissions |
| **2.11 State Machine** | IAgentStateMachine, AgentStateMachine, AgentState, StateTransitionValidator |
| **2.12 Storage Provider** | IStorageProvider, StorageProviderFactory, AmazonS3StorageProvider |
| **2.13 Background Services** | Convert to BackgroundService, DI registration, cancellation tokens |
| **2.14 Backend** | New Prisma tables, API endpoints, controllers, services, repositories |
| **2.15 Tests** | Unit tests for Runtime and Persistence, integration tests |
| **2.16 Documentation** | Architecture, communication flow, dependency graph, threading, lifecycles |

---

## 16. Design Decisions & Assumptions

- **Event-Driven Architecture:** All inter-module communication flows through EventBus. This decouples modules and enables future extensibility.
- **Worker-Based Execution:** All background tasks inherit from BackgroundWorkerBase. This provides consistent lifecycle, retry, and health monitoring.
- **Repository Pattern:** All data access goes through repository interfaces. This enables testing and future database swaps.
- **Queue-Based Processing:** All async operations go through Queue System. This enables retry, DLQ, and priority handling.
- **Policy-Driven Behavior:** Modules never read settings.json directly. They query Policy Engine. This enables centralized control.
- **Feature Flag Control:** All optional features are controlled by Feature Flags. This enables remote enable/disable.
- **State Machine Validation:** Agent state transitions are validated. This prevents invalid states.
- **Storage Abstraction:** Cloud storage is abstracted behind IStorageProvider. This enables multi-cloud support.
- **Offline-First Design:** All operations queue locally when offline. This ensures no data loss.
- **SQLite with WAL:** WAL mode enables concurrent read/write for better performance.
- **Priority Queue:** Critical events and jobs are processed first.
- **Weak References:** EventBus uses weak references to prevent memory leaks.
- **Cancellation Tokens:** All async operations respect cancellation for graceful shutdown.
- **Health Monitoring:** System health is continuously monitored and reported.
- **Module Dependencies:** Modules declare dependencies for proper load order.
- **Configuration-Driven Scheduling:** Schedules can be updated from backend without code changes.
- **Dead Letter Queue:** Failed jobs after max retries go to DLQ for manual inspection.
- **Retry with Exponential Backoff:** Failed jobs retry with increasing delays.
- **Cache Policies:** Policies and feature flags are cached to reduce backend calls.
- **Batch Processing:** Queue worker can process multiple jobs concurrently.
- **Memory Limits:** Queue size is limited to prevent memory issues.
- **Backpressure:** Queue worker slows down when system is under load.
