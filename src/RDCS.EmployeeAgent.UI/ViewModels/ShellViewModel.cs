using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Services.Orchestration;
using System.Windows;

namespace RDCS.EmployeeAgent.UI.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly IAgentLogger _logger;
    private readonly IAuthenticationService _authenticationService;
    private readonly ApplicationOrchestrator _orchestrator;

    [ObservableProperty]
    private string _status = "Initializing...";

    [ObservableProperty]
    private string _employeeName = "Unknown";

    [ObservableProperty]
    private string _deviceName = "Unknown";

    [ObservableProperty]
    private bool _isOnline;

    public ShellViewModel(
        IAgentLogger logger,
        IAuthenticationService authenticationService,
        ApplicationOrchestrator orchestrator)
    {
        _logger = logger;
        _authenticationService = authenticationService;
        _orchestrator = orchestrator;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        try
        {
            Status = "Initializing...";
            
            var identity = await _authenticationService.GetStoredIdentityAsync();
            if (identity != null)
            {
                EmployeeName = identity.EmployeeId;
                DeviceName = Environment.MachineName;
            }

            var agentStatus = await _orchestrator.InitializeAsync();
            Status = agentStatus.ToString();
            IsOnline = agentStatus == AgentStatus.Running;

            _logger.LogInformation(LogCategory.Application, "Shell initialized with status {Status}", agentStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Application, "Shell initialization failed", ex);
            Status = "Error: " + ex.Message;
        }
    }

    [RelayCommand]
    private void HideWindow()
    {
        try
        {
            _logger.LogInformation(LogCategory.Authentication, "Hide requested - agent keeps running in tray");
            // Hiding only closes the UI. The hosted workers (capture, upload,
            // heartbeat) live in the host process and keep running regardless.
            foreach (var window in Application.Current.Windows.OfType<Views.ShellWindow>().ToList())
            {
                window.Hide();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Authentication, "Hide failed", ex);
        }
    }

    [RelayCommand]
    private void ShowSettings()
    {
        _logger.LogInformation(LogCategory.Application, "Opening settings window");
        // TODO: Implement settings window navigation
    }
}
