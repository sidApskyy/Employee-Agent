# Dependency Injection and Service Lifetime Audit

**Audit Date:** July 7, 2026  
**Scope:** All service registrations in `App.xaml.cs`  
**Application Type:** Desktop WPF using Microsoft.Extensions.Hosting

---

## Service Registration Analysis

| Service Name | Interface | Implementation | Lifetime | Dependencies | Reason for Chosen Lifetime | Potential Risks |
|-------------|-----------|----------------|----------|--------------|---------------------------|----------------|
| IConfiguration | IConfiguration | - | Singleton | None | Configuration is immutable and shared across application | None - Correct |
| IDateTimeProvider | IDateTimeProvider | DateTimeProvider | Singleton | None | Stateless utility for time operations | None - Correct |
| StorageSettings | IOptions\<StorageSettings\> | - | Singleton | IConfiguration | Configuration wrapper, immutable | None - Correct |
| IAgentLogger | IAgentLogger | SerilogAgentLogger | Singleton | None | Logging infrastructure, thread-safe | None - Correct |
| ITokenStorage | ITokenStorage | WindowsCredentialStorage | Singleton | None | Windows Credential Manager is system-wide resource | None - Correct |
| IDeviceInfoProvider | IDeviceInfoProvider | WindowsDeviceInfoProvider | Singleton | None | Device info doesn't change during runtime | None - Correct |
| IExceptionHandler | IExceptionHandler | GlobalExceptionHandler | Singleton | IAgentLogger | Global exception handler, must be single instance | None - Correct |
| IApiClient | IApiClient | ApiClient | Singleton | HttpClient, IAgentLogger | HttpClient should be reused for connection pooling | None - Correct |
| StoragePathProvider | StoragePathProvider | - | Singleton | IOptions\<StorageSettings\> | Path configuration, stateless | None - Correct |
| StorageDirectoryManager | StorageDirectoryManager | - | Singleton | IOptions\<StorageSettings\>, IAgentLogger | Utility class for directory operations | None - Correct |
| IStorageInitializer | IStorageInitializer | StorageInitializer | Singleton | StorageDirectoryManager, IAgentLogger | Runs once at startup to initialize storage | None - Correct |
| StorageHealthService | StorageHealthService | - | Singleton | StorageDirectoryManager, IAgentLogger | Health monitoring service | None - Correct |
| StorageStatisticsService | StorageStatisticsService | - | Singleton | StorageDirectoryManager, IAgentLogger | Statistics tracking service | None - Correct |
| StorageCleanupService | StorageCleanupService | - | Singleton | StorageDirectoryManager, IAgentLogger | Cleanup service | None - Correct |
| StorageProviderFactory | StorageProviderFactory | - | Singleton | IOptions\<StorageSettings\> | Factory pattern for storage providers | None - Correct |
| IStorageProvider | IStorageProvider | LocalStorageProvider | Singleton | StoragePathHelper, IAgentLogger | Local storage is stateless | None - Correct |
| SQLiteConnectionFactory | SQLiteConnectionFactory | - | Singleton | IOptions\<StorageSettings\> | Factory for database connections | **HIGH RISK** - Should be Singleton but connection pooling not implemented |
| DatabaseInitializer | DatabaseInitializer | - | Singleton | SQLiteConnectionFactory, IAgentLogger | Runs once at startup to initialize database | None - Correct |
| IScreenshotRepository | IScreenshotRepository | ScreenshotRepository | Scoped | SQLiteConnectionFactory, IAgentLogger | Repository should be scoped for proper connection management | **CRITICAL RISK** - Scoped repository in hosted service context |
| IScreenshotJobRepository | IScreenshotJobRepository | ScreenshotJobRepository | Scoped | SQLiteConnectionFactory, IAgentLogger | Repository should be scoped for proper connection management | **CRITICAL RISK** - Scoped repository in hosted service context |
| IJobQueueRepository | IJobQueueRepository | JobQueueRepository | Scoped | SQLiteConnectionFactory, IAgentLogger | Repository should be scoped for proper connection management | **CRITICAL RISK** - Scoped repository in hosted service context |
| IPolicyRepository | IPolicyRepository | PolicyRepository | Scoped | SQLiteConnectionFactory, IAgentLogger | Repository should be scoped for proper connection management | **CRITICAL RISK** - Scoped repository in hosted service context |
| IFeatureFlagRepository | IFeatureFlagRepository | FeatureFlagRepository | Scoped | SQLiteConnectionFactory, IAgentLogger | Repository should be scoped for proper connection management | **CRITICAL RISK** - Scoped repository in hosted service context |
| IAgentStateRepository | IAgentStateRepository | AgentStateRepository | Scoped | SQLiteConnectionFactory, IAgentLogger | Repository should be scoped for proper connection management | **CRITICAL RISK** - Scoped repository in hosted service context |
| IEventBus | IEventBus | EventBus | Scoped | IAgentLogger | **INCORRECT** - Stateful service with ConcurrentDictionary and SemaphoreSlim | **CRITICAL RISK** - Scoped in hosted service context, state not shared |
| IJobQueue | IJobQueue | JobQueue | Scoped | IJobQueueRepository, IEventBus, IAgentLogger | **INCORRECT** - Stateful service maintaining queue state | **CRITICAL RISK** - Scoped in hosted service context, state not shared |
| IJobProcessor | IJobProcessor | JobProcessor | Singleton | None | Placeholder implementation, stateless | None - Correct (but placeholder) |
| IQueueWorker | IQueueWorker | QueueWorker | Scoped | IJobQueue, IJobProcessor, IEventBus, IAgentLogger | **INCORRECT** - Stateful worker with task tracking | **CRITICAL RISK** - Scoped in hosted service context |
| IPolicyEngine | IPolicyEngine | PolicyEngine | Scoped | IPolicyRepository, IEventBus, IAgentLogger | **INCORRECT** - Has cache that should be shared | **CRITICAL RISK** - Scoped cache defeats purpose |
| IScheduler | IScheduler | Scheduler | Singleton | IAgentLogger | Scheduler maintains job dictionary | **MEDIUM RISK** - Singleton but job dictionary may grow unbounded |
| IHealthMonitor | IHealthMonitor | HealthMonitor | Singleton | IAgentLogger | Health monitoring service | None - Correct |
| IFeatureFlagManager | IFeatureFlagManager | FeatureFlagManager | Singleton | IFeatureFlagRepository, IAgentLogger | Feature flag management | None - Correct |
| IScreenshotService | IScreenshotService | ScreenshotService | Singleton | IAgentLogger | Screenshot capture service, stateless | None - Correct |
| IImageProcessingService | IImageProcessingService | ImageProcessingService | Singleton | JpegCompressionProvider, PngCompressionProvider, WebpCompressionProvider, IAgentLogger | Image processing service, stateless | None - Correct |
| JpegCompressionProvider | JpegCompressionProvider | - | Singleton | None | Compression provider, stateless | None - Correct |
| PngCompressionProvider | PngCompressionProvider | - | Singleton | None | Compression provider, stateless | None - Correct |
| WebpCompressionProvider | WebpCompressionProvider | - | Singleton | None | Compression provider, stateless | None - Correct |
| StoragePathHelper | StoragePathHelper | - | Singleton | IOptions\<StorageSettings\> | Path helper utility, stateless | None - Correct |
| IAuthenticationService | IAuthenticationService | AuthenticationService | Singleton | IApiClient, ITokenStorage, IAgentLogger | Authentication service | None - Correct |
| IDeviceRegistrationService | IDeviceRegistrationService | DeviceRegistrationService | Singleton | IApiClient, IDeviceInfoProvider, IAgentLogger | Device registration service | None - Correct |
| IConfigurationService | IConfigurationService | ConfigurationService | Singleton | IApiClient, IAgentLogger | Configuration service | None - Correct |
| IHeartbeatService | IHeartbeatService | HeartbeatService | Singleton | IApiClient, IAgentLogger | Heartbeat service | None - Correct |
| ModuleRegistry | ModuleRegistry | - | Singleton | None | Module registry, stateless | None - Correct |
| IModuleHost | IModuleHost | ModuleHost | Singleton | ModuleRegistry, IAgentLogger | Module host | None - Correct |
| ApplicationOrchestrator | ApplicationOrchestrator | - | Singleton | IModuleHost, IAgentLogger | Application orchestrator | None - Correct |
| SchedulerBackgroundService | BackgroundService | SchedulerBackgroundService | Singleton | IScheduler, IAgentLogger | Hosted service wrapper | **CRITICAL RISK** - Singleton consuming scoped IScheduler |
| QueueWorkerBackgroundService | BackgroundService | QueueWorkerBackgroundService | Singleton | IQueueWorker, IAgentLogger | Hosted service wrapper | **CRITICAL RISK** - Singleton consuming scoped IQueueWorker |
| EventQueueBackgroundService | BackgroundService | EventQueueBackgroundService | Singleton | IEventBus, IAgentLogger | Hosted service wrapper | **CRITICAL RISK** - Singleton consuming scoped IEventBus |
| HealthMonitorBackgroundService | BackgroundService | HealthMonitorBackgroundService | Singleton | IHealthMonitor, IAgentLogger | Hosted service wrapper | None - Correct (IHealthMonitor is Singleton) |
| LoginViewModel | LoginViewModel | - | Transient | IAuthenticationService, IDeviceRegistrationService, IConfigurationService, IAgentLogger | View model for login window | None - Correct (view models should be transient) |
| ShellViewModel | ShellViewModel | - | Transient | IAgentLogger | View model for shell window | None - Correct (view models should be transient) |

---

## Critical Issues

### Issue 1: Scoped Services in Hosted Services (CRITICAL)

**Affected Services:**
- IEventBus (Scoped) consumed by EventQueueBackgroundService (Singleton)
- IJobQueue (Scoped) consumed by QueueWorkerBackgroundService (Singleton)
- IQueueWorker (Scoped) consumed by QueueWorkerBackgroundService (Singleton)
- IScheduler (Singleton) consumed by SchedulerBackgroundService (Singleton) - OK
- IHealthMonitor (Singleton) consumed by HealthMonitorBackgroundService (Singleton) - OK

**Problem:**
Hosted services are registered as Singleton and run for the application lifetime. When they consume Scoped services, this creates a captive dependency problem. The Scoped service will be resolved once and treated as a Singleton, defeating the purpose of the Scoped lifetime.

**Impact:**
- EventBus state (ConcurrentDictionary, SemaphoreSlim) will not be shared correctly
- JobQueue state will not be shared correctly
- QueueWorker state will not be shared correctly
- Potential memory leaks from accumulated state
- Thread safety issues

**Recommended Fix:**
Change stateful services that need to be shared across the application to Singleton:
- IEventBus → Singleton
- IJobQueue → Singleton
- IQueueWorker → Singleton (but this creates another issue - see below)

---

### Issue 2: EventBus Should Be Singleton (CRITICAL)

**Current:** Scoped  
**Should Be:** Singleton

**Reason:**
EventBus is designed to be a shared in-process event bus with:
- ConcurrentDictionary for subscriptions (should be shared)
- SemaphoreSlim for thread safety (should be shared)
- Event subscriptions should persist across the application

**Impact of Current Scoped Lifetime:**
- Each resolution creates a new EventBus instance
- Subscriptions are not shared between components
- Events published in one component won't reach handlers in another
- Defeats the purpose of an event bus

**Recommended Fix:**
Change IEventBus to Singleton. The EventBus is designed to be a shared service.

---

### Issue 3: JobQueue Should Be Singleton (CRITICAL)

**Current:** Scoped  
**Should Be:** Singleton

**Reason:**
JobQueue maintains:
- Queue state
- Job tracking
- Integration with JobQueueRepository

If multiple instances exist, they will have inconsistent state.

**Impact of Current Scoped Lifetime:**
- Multiple queue instances with different state
- Jobs may be lost or duplicated
- Race conditions between queue instances

**Recommended Fix:**
Change IJobQueue to Singleton. The queue should be a shared resource.

---

### Issue 4: QueueWorker Should Be Singleton with Proper Lifecycle (CRITICAL)

**Current:** Scoped  
**Should Be:** Singleton (but managed by hosted service)

**Reason:**
QueueWorker is a background worker that:
- Maintains worker state
- Tracks running tasks
- Should have a single instance per application

The current design has QueueWorkerBackgroundService (Singleton) consuming IQueueWorker (Scoped), which is incorrect.

**Impact of Current Scoped Lifetime:**
- Hosted service will resolve QueueWorker once and treat it as Singleton
- If the hosted service tries to resolve it multiple times, it will get different instances
- Worker state management is inconsistent

**Recommended Fix:**
Change IQueueWorker to Singleton and remove the hosted service wrapper, OR keep the hosted service wrapper and register QueueWorker as Singleton.

---

### Issue 5: PolicyEngine Should Be Singleton (HIGH)

**Current:** Scoped  
**Should Be:** Singleton

**Reason:**
PolicyEngine has:
- In-memory cache of policies
- Event publishing for policy updates

If scoped, each instance has its own cache, defeating the purpose of caching.

**Impact of Current Scoped Lifetime:**
- Cache not shared between components
- Multiple database calls for same policy
- Increased memory usage from duplicate caches

**Recommended Fix:**
Change IPolicyEngine to Singleton. The cache should be shared.

---

### Issue 6: Repositories in Non-Scoped Context (HIGH)

**Affected Services:**
- All repositories (Scoped) consumed by:
  - JobQueue (currently Scoped, should be Singleton)
  - PolicyEngine (currently Scoped, should be Singleton)
  - ScreenshotWorker (Scoped)
  - Other workers

**Problem:**
Repositories are correctly registered as Scoped, but they are consumed by services that are Scoped or should be Singleton. This creates captive dependency issues.

**Impact:**
- If consumed by Singleton services, repositories will be treated as Singleton
- Connection management may be incorrect
- Potential connection leaks

**Recommended Fix:**
Two options:
1. Keep repositories Scoped and ensure all consumers are Scoped (requires architectural change)
2. Change repositories to Singleton with proper connection pooling (easier but less ideal)

For a desktop WPF application, option 2 is more practical given the single-user nature.

---

### Issue 7: Scheduler Job Dictionary Growth (MEDIUM)

**Current:** Singleton  
**Issue:** Job dictionary may grow unbounded

**Reason:**
Scheduler maintains a dictionary of scheduled jobs. If jobs are not cleaned up after completion, the dictionary will grow indefinitely.

**Impact:**
- Memory leak
- Performance degradation over time

**Recommended Fix:**
Add job cleanup logic to remove completed jobs from the dictionary.

---

## Summary of Recommended Changes

### Critical Changes Required

1. **IEventBus:** Scoped → Singleton
2. **IJobQueue:** Scoped → Singleton
3. **IQueueWorker:** Scoped → Singleton
4. **IPolicyEngine:** Scoped → Singleton
5. **All Repositories:** Scoped → Singleton (with connection pooling) OR ensure all consumers are Scoped

### Recommended Service Lifetime Strategy for WPF Desktop Application

For a desktop WPF application using Microsoft.Extensions.Hosting:

**Singleton Services:**
- Configuration (IConfiguration, IOptions)
- Logging (IAgentLogger)
- Infrastructure (ITokenStorage, IDeviceInfoProvider, IExceptionHandler)
- API Client (IApiClient)
- Storage Infrastructure (all storage services)
- Database Infrastructure (SQLiteConnectionFactory, DatabaseInitializer)
- Runtime Core Services (IEventBus, IJobQueue, IQueueWorker, IPolicyEngine, IScheduler, IHealthMonitor, IFeatureFlagManager)
- Screenshot Services (all screenshot services)
- Business Services (IAuthenticationService, IDeviceRegistrationService, IConfigurationService, IHeartbeatService)
- Modules (ModuleRegistry, IModuleHost, ApplicationOrchestrator)
- Background Workers (all hosted services)

**Scoped Services:**
- Repositories (if using proper connection pooling and scoped context)
- ViewModels (if using scoped lifetime for views)

**Transient Services:**
- ViewModels (current approach is correct)

**Rationale:**
- Desktop WPF applications are single-user, single-instance
- No need for request-based scoping like web applications
- Singleton is appropriate for most services
- Transient for ViewModels ensures fresh instances per view

---

## Conclusion

The current DI configuration has **5 critical issues** related to service lifetimes:

1. EventBus is Scoped but should be Singleton (shared event bus)
2. JobQueue is Scoped but should be Singleton (shared queue)
3. QueueWorker is Scoped but should be Singleton (single worker instance)
4. PolicyEngine is Scoped but should be Singleton (shared cache)
5. Repositories are Scoped but consumed by Singleton services (captive dependency)

These issues will cause:
- State not being shared correctly
- Memory leaks
- Thread safety issues
- Incorrect behavior of core infrastructure components

**Recommendation:** Change the 4 stateful services (EventBus, JobQueue, QueueWorker, PolicyEngine) to Singleton, and either change repositories to Singleton or ensure all consumers are Scoped.
