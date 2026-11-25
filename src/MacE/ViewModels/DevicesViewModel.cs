using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacE.Models;
using MacE.Services.Interfaces;
using System.Collections.ObjectModel;
using System.IO;

namespace MacE.ViewModels;

/// <summary>
/// ViewModel for the Registered Devices page.
/// Manages device enrollment, live UI refresh, identity export, and peer device import.
/// Cross-platform support for Windows and Android.
/// </summary>
public partial class DevicesViewModel : BaseViewModel
{
    private readonly IDeviceService _deviceService;

    public DevicesViewModel(IDeviceService deviceService)
    {
        _deviceService = deviceService;
        Title = "Devices";
    }

    public ObservableCollection<DeviceIdentity> Devices { get; } = new();

    [ObservableProperty]
    private bool _isCurrentDeviceEnrolled;

    [ObservableProperty]
    private string _currentDeviceSummary = string.Empty;

    [ObservableProperty]
    private string _newDeviceName = string.Empty;

    [RelayCommand]
    public async Task LoadDevicesAsync()
    {
        await RefreshDevicesListAsync();
    }

    public async Task RefreshDevicesListAsync()
    {
        try
        {
            var currentDevice = await _deviceService.GetCurrentDeviceAsync();
            var devices = await _deviceService.GetAllDevicesAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsCurrentDeviceEnrolled = currentDevice != null;
                CurrentDeviceSummary = currentDevice != null
                    ? $"Registered: {currentDevice.DisplayName}"
                    : "Not enrolled";

                Devices.Clear();
                foreach (var device in devices)
                {
                    Devices.Add(device);
                }

                OnPropertyChanged(nameof(IsCurrentDeviceEnrolled));
                OnPropertyChanged(nameof(CurrentDeviceSummary));
            });
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                SetStatus($"Error loading devices: {ex.Message}", isError: true);
            });
        }
    }

    [RelayCommand]
    public async Task EnrollDeviceAsync()
    {
        if (string.IsNullOrWhiteSpace(NewDeviceName))
        {
            SetStatus("Please enter a device display name (e.g. My Phone or Laptop).", isError: true);
            return;
        }

        if (IsBusy) return;
        IsBusy = true;
        ClearStatus();

        try
        {
            var deviceName = NewDeviceName.Trim();
            var enrolled = await _deviceService.EnrollCurrentDeviceAsync(deviceName);

            NewDeviceName = string.Empty;
            OnPropertyChanged(nameof(NewDeviceName));

            await RefreshDevicesListAsync();

            SetStatus($"Device '{enrolled.DisplayName}' is now registered.", isError: false);
        }
        catch (Exception ex)
        {
            SetStatus($"Enrollment failed: {ex.Message}", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ExportCurrentDeviceAsync()
    {
        try
        {
            var json = await _deviceService.ExportCurrentDeviceIdentityAsync();
            var current = await _deviceService.GetCurrentDeviceAsync();

            var safeName = current?.DisplayName ?? "device";
            var fileName = $"{safeName}_identity.mace-device";

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                var tempPath = Path.Combine(FileSystem.CacheDirectory, fileName);
                await File.WriteAllTextAsync(tempPath, json);

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Export Device Key",
                    File = new ShareFile(tempPath)
                });

                SetStatus("Exported device key file.", isError: false);
            }
            else
            {
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var fullPath = Path.Combine(desktopPath, fileName);
                await File.WriteAllTextAsync(fullPath, json);
                SetStatus($"Device key saved to Desktop: {fileName}", isError: false);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Failed to export device identity: {ex.Message}", isError: true);
        }
    }

    [RelayCommand]
    public async Task ImportPeerDeviceAsync()
    {
        try
        {
            var customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".mace-device", ".json" } },
                    { DevicePlatform.Android, new[] { "*/*" } }
                });

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select a .mace-device file to import",
                FileTypes = customFileType
            });

            if (result == null) return;

            var json = await File.ReadAllTextAsync(result.FullPath);
            var imported = await _deviceService.ImportDeviceIdentityAsync(json);

            await RefreshDevicesListAsync();

            SetStatus($"Added peer device: '{imported.DisplayName}'", isError: false);
        }
        catch (Exception ex)
        {
            SetStatus($"Import failed: {ex.Message}", isError: true);
        }
    }

    [RelayCommand]
    public async Task RemoveDeviceAsync(DeviceIdentity device)
    {
        if (device == null) return;

        try
        {
            await _deviceService.RemoveDeviceAsync(device.DeviceId);
            await RefreshDevicesListAsync();
            SetStatus($"Removed device '{device.DisplayName}'.", isError: false);
        }
        catch (Exception ex)
        {
            SetStatus($"Failed to remove device: {ex.Message}", isError: true);
        }
    }
}
