using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Infrastructure.Logging;
using RDCS.EmployeeAgent.Infrastructure.Security;
using RDCS.EmployeeAgent.Infrastructure.Platform;
using RDCS.EmployeeAgent.Infrastructure.ExceptionHandling;
using RDCS.EmployeeAgent.Infrastructure.Api;
using RDCS.EmployeeAgent.Infrastructure.Http;
using RDCS.EmployeeAgent.Runtime.Storage;
using RDCS.EmployeeAgent.Runtime.Storage.Providers;
using RDCS.EmployeeAgent.Runtime.Upload.Interfaces;
using RDCS.EmployeeAgent.Runtime.Upload.Repositories;
using RDCS.EmployeeAgent.Runtime.Upload.Services;
using RDCS.EmployeeAgent.Runtime.Upload.Workers;
using RDCS.EmployeeAgent.Runtime.EventBus;
using RDCS.EmployeeAgent.Runtime.Queue;
using RDCS.EmployeeAgent.Runtime.Policy;
using RDCS.EmployeeAgent.Runtime.Scheduler;
using RDCS.EmployeeAgent.Runtime.Health;
using RDCS.EmployeeAgent.Runtime.FeatureFlags;
using RDCS.EmployeeAgent.Runtime.Screenshot.Services;
using RDCS.EmployeeAgent.Runtime.Screenshot.Storage;
using RDCS.EmployeeAgent.Runtime.Screenshot.Workers;
using RDCS.EmployeeAgent.Runtime.HostedServices;
using RDCS.EmployeeAgent.Runtime.Workers;
using RDCS.EmployeeAgent.Services.Authentication;
using RDCS.EmployeeAgent.Services.DeviceRegistration;
using RDCS.EmployeeAgent.Services.Configuration;
using RDCS.EmployeeAgent.Services.Heartbeat;
using RDCS.EmployeeAgent.Services.Modules;
using RDCS.EmployeeAgent.Services.Orchestration;
using RDCS.EmployeeAgent.Persistence.Repositories;
using RDCS.EmployeeAgent.Persistence.SQLite;
using RDCS.EmployeeAgent.UI.ViewModels;
using RDCS.EmployeeAgent.UI.Views;
using RDCS.EmployeeAgent.Shared.Utilities;
using System.Windows;
using System.IO;
using Microsoft.Win32;
using WinForms = System.Windows.Forms;

namespace RDCS.EmployeeAgent.UI;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        try
        {
            // STEP 1: Load Configuration
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var projectDir = AppDomain.CurrentDomain.BaseDirectory;
            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile(Path.Combine(projectDir, "appsettings.json"), optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    // Configuration
                    services.AddSingleton<IConfiguration>(context.Configuration);
                    services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
                    
                    // Register StorageSettings as IOptions
                    services.Configure<StorageSettings>(context.Configuration.GetSection("Storage"));

                    // Logging
                    SerilogConfigurator.Configure("Information");
                    services.AddSingleton<IAgentLogger, SerilogAgentLogger>();

                    // Infrastructure
                    services.AddSingleton<ITokenStorage, WindowsCredentialStorage>();
                    services.AddSingleton<IDeviceInfoProvider, WindowsDeviceInfoProvider>();
                    services.AddSingleton<IExceptionHandler, GlobalExceptionHandler>();

                    // API Client
                    var apiUrl = context.Configuration["ApiUrl"];
                    if (string.IsNullOrWhiteSpace(apiUrl))
                    {
                        MessageBox.Show(
                            "ApiUrl is missing from appsettings.json.\n\nPlease ensure appsettings.json is in the same folder as RDCS.EmployeeAgent.UI.exe and contains a valid ApiUrl.",
                            "Configuration Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        Shutdown();
                        return;
                    }
                    services.AddAgentHttpClient(apiUrl);
                    services.AddSingleton<IApiClient, ApiClient>();

                    // Storage Infrastructure
                    services.AddSingleton<StoragePathProvider>();
                    services.AddSingleton<StorageDirectoryManager>();
                    services.AddSingleton<IStorageInitializer, StorageInitializer>();
                    services.AddSingleton<StorageHealthService>();
                    services.AddSingleton<StorageStatisticsService>();
                    services.AddSingleton<StorageCleanupService>();
                    services.AddSingleton<StorageProviderFactory>();

                    // Register storage provider based on configuration
                    var storageSettings = context.Configuration.GetSection("Storage").Get<StorageSettings>();
                    if (storageSettings?.Provider?.Equals("S3", StringComparison.OrdinalIgnoreCase) == true ||
                        storageSettings?.Provider?.Equals("AmazonS3", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        services.AddSingleton<IStorageProvider>(sp =>
                        {
                            var logger = sp.GetRequiredService<IAgentLogger>();
                            var s3Settings = storageSettings.S3;
                            return new AmazonS3StorageProvider(
                                s3Settings.BucketName,
                                s3Settings.AccessKey,
                                s3Settings.SecretKey,
                                s3Settings.Region,
                                logger);
                        });
                    }
                    else
                    {
                        services.AddSingleton<IStorageProvider, LocalStorageProvider>();
                    }

                    // SQLite Infrastructure
                    services.AddSingleton<SQLiteConnectionFactory>(sp => {
                        var settings = sp.GetRequiredService<IOptions<StorageSettings>>();
                        var databasePath = Path.Combine(settings.Value.RootPath, settings.Value.DatabasePath, "agent.db");
                        return new SQLiteConnectionFactory(databasePath);
                    });
                    services.AddSingleton<DatabaseInitializer>();

                    // Repositories (Singleton for desktop WPF application)
                    services.AddSingleton<IScreenshotRepository, ScreenshotRepository>();
                    services.AddSingleton<IScreenshotJobRepository, ScreenshotJobRepository>();
                    services.AddSingleton<IJobQueueRepository, JobQueueRepository>();
                    services.AddSingleton<IPolicyRepository, PolicyRepository>();
                    services.AddSingleton<IFeatureFlagRepository, FeatureFlagRepository>();
                    services.AddSingleton<IAgentStateRepository, AgentStateRepository>();

                    // Runtime Core Services (Singleton for shared state)
                    services.AddSingleton<IEventBus, EventBus>();
                    services.AddSingleton<IJobQueue, JobQueue>();
                    services.AddSingleton<IJobProcessor, JobProcessor>();
                    services.AddSingleton<IQueueWorker, QueueWorker>();
                    services.AddSingleton<IPolicyEngine, PolicyEngine>();
                    services.AddSingleton<IScheduler, Scheduler>();
                    services.AddSingleton<IHealthMonitor, HealthMonitor>();
                    services.AddSingleton<IFeatureFlagManager, FeatureFlagManager>();

                    // Upload Services (Phase 5 — Cloud Sync)
                    services.AddSingleton<IUploadRepository, UploadRepository>();
                    services.AddSingleton<IConnectivityService, ConnectivityService>();
                    services.AddSingleton<INetworkMonitorService, NetworkMonitorService>();
                    services.AddSingleton<IChecksumService, ChecksumService>();
                    services.AddSingleton<IMultipartPreparationService, MultipartPreparationService>();
                    services.AddSingleton<IUploadQueueService, UploadQueueService>();
                    services.AddSingleton<IUploadRetryService, UploadRetryService>();
                    services.AddSingleton<IUploadStatisticsService, UploadStatisticsService>();
                    services.AddSingleton<UploadStatisticsService>(sp => (UploadStatisticsService)sp.GetRequiredService<IUploadStatisticsService>());
                    services.AddSingleton<UploadHealthService>();
                    services.AddSingleton<IUploadWorker, UploadWorker>();
                    services.AddHttpClient("UploadClient");

                    // Screenshot Services
                    services.AddSingleton<IScreenshotService, ScreenshotService>();
                    services.AddSingleton<IImageProcessingService, ImageProcessingService>();
                    services.AddSingleton<JpegCompressionProvider>();
                    services.AddSingleton<PngCompressionProvider>();
                    services.AddSingleton<WebpCompressionProvider>();
                    services.AddSingleton<StoragePathHelper>();
                    services.AddSingleton<IScreenshotWorker, ScreenshotWorker>();

                    // Services
                    services.AddSingleton<IAuthenticationService, AuthenticationService>();
                    services.AddSingleton<IDeviceRegistrationService, DeviceRegistrationService>();
                    services.AddSingleton<IConfigurationService, ConfigurationService>();
                    services.AddSingleton<IHeartbeatService, HeartbeatService>();

                    // Modules
                    services.AddSingleton<ModuleRegistry>();
                    services.AddSingleton<IModuleHost, ModuleHost>();
                    services.AddSingleton<ApplicationOrchestrator>();

                    // Background Workers as Hosted Services
                    services.AddHostedService<SchedulerBackgroundService>();
                    services.AddHostedService<QueueWorkerBackgroundService>();
                    services.AddHostedService<EventQueueBackgroundService>();
                    services.AddHostedService<HealthMonitorBackgroundService>();
                    // Resolve the already-registered singletons so only ONE instance exists
                    services.AddHostedService(sp => (ScreenshotWorker)sp.GetRequiredService<IScreenshotWorker>());
                    services.AddHostedService(sp => (UploadWorker)sp.GetRequiredService<IUploadWorker>());

                    // ViewModels
                    services.AddTransient<LoginViewModel>();
                    services.AddTransient<ShellViewModel>();
                })
                .Build();

            stopwatch.Stop();
            var logger = _host.Services.GetRequiredService<IAgentLogger>();
            
            // STEP 3: Initialize Database
            stopwatch.Restart();
            var dbInitializer = _host.Services.GetRequiredService<DatabaseInitializer>();
            await dbInitializer.InitializeAsync();
            stopwatch.Stop();

            // STEP 4: Start Host
            stopwatch.Restart();
            await _host.StartAsync();
            stopwatch.Stop();

            // Setup global exception handlers
            GlobalExceptionHandler.SetupGlobalHandlers(logger);

            // Show login window
            var loginViewModel = new LoginViewModel(
                _host.Services.GetRequiredService<IAuthenticationService>(),
                _host.Services.GetRequiredService<IDeviceRegistrationService>(),
                _host.Services.GetRequiredService<IConfigurationService>(),
                _host.Services.GetRequiredService<IAgentLogger>(),
                OnLoginSuccess);
            var loginWindow = new LoginWindow(loginViewModel);
            loginWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Startup failed: {ex.Message}\n\n{ex.StackTrace}",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void OnLoginSuccess()
    {
        RegisterAutoStart();

        var shellViewModel = _host.Services.GetRequiredService<ShellViewModel>();
        var shellWindow = new Views.ShellWindow(shellViewModel);
        shellWindow.Show();

        if (Application.Current.Windows[0] is LoginWindow loginWindow)
        {
            loginWindow.Close();
        }
    }

    private static void RegisterAutoStart()
    {
        try
        {
            const string appName = "RDCSEmployeeAgent";
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
            if (string.IsNullOrEmpty(exePath)) return;

            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true);
            key?.SetValue(appName, $"\"{exePath}\"");
        }
        catch { /* silently ignore - non-critical */ }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }
}

