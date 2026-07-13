using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using System.Windows;

namespace RDCS.EmployeeAgent.UI.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IDeviceRegistrationService _deviceRegistrationService;
    private readonly IConfigurationService _configurationService;
    private readonly IAgentLogger _logger;
    private readonly Action _onLoginSuccess;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _rememberMe;

    [ObservableProperty]
    private bool _showPassword;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isLoginEnabled = true;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private Visibility _errorMessageVisibility = Visibility.Collapsed;

    public LoginViewModel(
        IAuthenticationService authenticationService,
        IDeviceRegistrationService deviceRegistrationService,
        IConfigurationService configurationService,
        IAgentLogger logger,
        Action onLoginSuccess)
    {
        _authenticationService = authenticationService;
        _deviceRegistrationService = deviceRegistrationService;
        _configurationService = configurationService;
        _logger = logger;
        _onLoginSuccess = onLoginSuccess;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Email and password are required";
            HasError = true;
            ErrorMessageVisibility = Visibility.Visible;
            return;
        }

        try
        {
            IsLoading = true;
            IsLoginEnabled = false;
            HasError = false;
            ErrorMessageVisibility = Visibility.Collapsed;
            ErrorMessage = string.Empty;

            _logger.LogInformation(LogCategory.Authentication, "Login attempt from UI");

            var isAuthenticated = await _authenticationService.IsAuthenticatedAsync();
            
            if (isAuthenticated)
            {
                var identity = await _authenticationService.GetStoredIdentityAsync();
                if (identity != null && !identity.RequiresDeviceRegistration)
                {
                    _logger.LogInformation(LogCategory.Authentication, "Already authenticated, skipping login");
                    _onLoginSuccess();
                    return;
                }
            }

            var success = await _authenticationService.LoginAsync(Email, Password);

            if (success.RequiresDeviceRegistration || string.IsNullOrEmpty(success.DeviceId))
            {
                _logger.LogInformation(LogCategory.Authentication, "Device registration required");
                
                var deviceInfo = await _deviceRegistrationService.CollectDeviceInfoAsync();
                deviceInfo.EmployeeId = success.EmployeeId;
                deviceInfo.CompanyId = success.CompanyId;

                var deviceId = await _deviceRegistrationService.RegisterDeviceAsync(deviceInfo);
                success.DeviceId = deviceId;
                success.RequiresDeviceRegistration = false;

                await _authenticationService.LogoutAsync();
                await _authenticationService.LoginAsync(Email, Password);
            }

            await _configurationService.DownloadConfigurationAsync(success.ConfigVersion);

            _logger.LogInformation(LogCategory.Authentication, "Login successful");
            _onLoginSuccess();
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Authentication, "Login failed", ex);
            _logger.LogError(LogCategory.Authentication, $"Exception Type: {ex.GetType().Name}");
            _logger.LogError(LogCategory.Authentication, $"Inner Exception: {ex.InnerException?.Message}");
            _logger.LogError(LogCategory.Authentication, $"Stack Trace: {ex.StackTrace}");
            _logger.LogError(LogCategory.Authentication, $"Target Site: {ex.TargetSite?.Name}");
            
            ErrorMessage = $"Login failed: {ex.GetType().Name} - {ex.Message}";
            if (ex.InnerException != null)
            {
                ErrorMessage += $" | Inner: {ex.InnerException.Message}";
            }
            HasError = true;
            ErrorMessageVisibility = Visibility.Visible;
        }
        finally
        {
            IsLoading = false;
            IsLoginEnabled = true;
        }
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        ShowPassword = !ShowPassword;
    }

    [RelayCommand]
    private void ClearError()
    {
        HasError = false;
        ErrorMessage = string.Empty;
        ErrorMessageVisibility = Visibility.Collapsed;
    }
}
