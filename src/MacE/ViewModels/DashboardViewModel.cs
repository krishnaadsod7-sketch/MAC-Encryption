using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacE.Services.Interfaces;

namespace MacE.ViewModels;

/// <summary>
/// ViewModel for the Dashboard page.
/// Shows current device enrollment status, registered peer count, and quick actions.
/// </summary>
public partial class DashboardViewModel : BaseViewModel
{
    private readonly IDeviceService _deviceService;

    public DashboardViewModel(IDeviceService deviceService)
    {
        _deviceService = deviceService;
        Title = "Dashboard";
    }

    [ObservableProperty]
    private bool _isDeviceEnrolled;

    [ObservableProperty]
    private string _currentDeviceName = "Not enrolled";

    [ObservableProperty]
    private string _currentDeviceId = string.Empty;

    [ObservableProperty]
    private int _registeredDeviceCount;

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        try
        {
            var currentDevice = await _deviceService.GetCurrentDeviceAsync();
            var allDevices = await _deviceService.GetAllDevicesAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsDeviceEnrolled = currentDevice != null;
                CurrentDeviceName = currentDevice?.DisplayName ?? "Not enrolled";
                CurrentDeviceId = currentDevice?.DeviceId ?? string.Empty;
                RegisteredDeviceCount = allDevices.Count;

                OnPropertyChanged(nameof(IsDeviceEnrolled));
                OnPropertyChanged(nameof(CurrentDeviceName));
                OnPropertyChanged(nameof(RegisteredDeviceCount));
            });
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                SetStatus($"Error loading dashboard: {ex.Message}", isError: true);
            });
        }
    }

    [RelayCommand]
    public async Task GoToEncryptAsync()
    {
        await Shell.Current.GoToAsync("//EncryptPage");
    }

    [RelayCommand]
    public async Task GoToDecryptAsync()
    {
        await Shell.Current.GoToAsync("//DecryptPage");
    }

    [RelayCommand]
    public async Task GoToDevicesAsync()
    {
        await Shell.Current.GoToAsync("//DevicesPage");
    }
}
