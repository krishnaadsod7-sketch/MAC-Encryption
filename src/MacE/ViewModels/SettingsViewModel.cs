using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacE.Services.Interfaces;

namespace MacE.ViewModels;

/// <summary>
/// ViewModel for the Settings page.
/// Manages application theme (System Default, Light, Dark) and displays system info.
/// </summary>
public partial class SettingsViewModel : BaseViewModel
{
    private readonly IDeviceService _deviceService;
    private readonly IDeviceInfoProvider _deviceInfoProvider;

    public const string ThemePrefKey = "MacE_UserAppTheme";

    public SettingsViewModel(IDeviceService deviceService, IDeviceInfoProvider deviceInfoProvider)
    {
        _deviceService = deviceService;
        _deviceInfoProvider = deviceInfoProvider;
        Title = "Settings";

        var savedTheme = Preferences.Default.Get(ThemePrefKey, "System");
        _isThemeSystem = savedTheme == "System";
        _isThemeLight = savedTheme == "Light";
        _isThemeDark = savedTheme == "Dark";
    }

    [ObservableProperty]
    private bool _isThemeSystem;

    [ObservableProperty]
    private bool _isThemeLight;

    [ObservableProperty]
    private bool _isThemeDark;

    [ObservableProperty]
    private string _currentDeviceStatus = "Checking device registration...";

    public string AppVersion => "1.0.0";
    public string BuildInfo => "MAC-E: Device-Bound File Protection & Access Control";
    public string SecurityLevel => "Hardware-Secured Device-Bound Access";

    [RelayCommand]
    public async Task LoadSettingsAsync()
    {
        var currentDevice = await _deviceService.GetCurrentDeviceAsync();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (currentDevice != null)
            {
                CurrentDeviceStatus = $"Registered as \"{currentDevice.DisplayName}\"";
            }
            else
            {
                CurrentDeviceStatus = "Not registered yet. Register under the Devices tab.";
            }
            OnPropertyChanged(nameof(CurrentDeviceStatus));
        });
    }

    [RelayCommand]
    public void SetTheme(string theme)
    {
        switch (theme)
        {
            case "Light":
                IsThemeLight = true;
                IsThemeDark = false;
                IsThemeSystem = false;
                Preferences.Default.Set(ThemePrefKey, "Light");
                if (Application.Current != null)
                    Application.Current.UserAppTheme = AppTheme.Light;
                break;

            case "Dark":
                IsThemeDark = true;
                IsThemeLight = false;
                IsThemeSystem = false;
                Preferences.Default.Set(ThemePrefKey, "Dark");
                if (Application.Current != null)
                    Application.Current.UserAppTheme = AppTheme.Dark;
                break;

            case "System":
            default:
                IsThemeSystem = true;
                IsThemeLight = false;
                IsThemeDark = false;
                Preferences.Default.Set(ThemePrefKey, "System");
                if (Application.Current != null)
                    Application.Current.UserAppTheme = AppTheme.Unspecified;
                break;
        }

        OnPropertyChanged(nameof(IsThemeSystem));
        OnPropertyChanged(nameof(IsThemeLight));
        OnPropertyChanged(nameof(IsThemeDark));
    }
}
