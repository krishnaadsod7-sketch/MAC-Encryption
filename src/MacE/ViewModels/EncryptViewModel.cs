using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacE.Models;
using MacE.Services.Interfaces;
using System.Collections.ObjectModel;
using System.IO;

namespace MacE.ViewModels;

public class SelectableDeviceViewModel : ObservableObject
{
    public DeviceIdentity Device { get; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public SelectableDeviceViewModel(DeviceIdentity device, bool isSelected = false)
    {
        Device = device;
        _isSelected = isSelected;
    }
}

/// <summary>
/// ViewModel for the Encrypt page.
/// Select file -> Choose authorized devices -> Generate AES key -> Wrap for devices -> Write .mace
/// Cross-platform support for Windows and Android.
/// </summary>
public partial class EncryptViewModel : BaseViewModel
{
    private readonly ICryptoService _cryptoService;
    private readonly IDeviceService _deviceService;

    public EncryptViewModel(ICryptoService cryptoService, IDeviceService deviceService)
    {
        _cryptoService = cryptoService;
        _deviceService = deviceService;
        Title = "Encrypt";
    }

    [ObservableProperty]
    private string _selectedFilePath = string.Empty;

    [ObservableProperty]
    private string _selectedFileName = "No file selected";

    [ObservableProperty]
    private string _selectedFileSizeText = string.Empty;

    public ObservableCollection<SelectableDeviceViewModel> AvailableDevices { get; } = new();

    public ObservableCollection<DeviceIdentity> SelectedDevices { get; } = new();

    [ObservableProperty]
    private bool _canEncrypt;

    [ObservableProperty]
    private string _selectedDeviceCountText = "0 devices selected";

    [RelayCommand]
    public async Task LoadDevicesAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var devices = await _deviceService.GetAllDevicesAsync();
            var currentDevice = devices.FirstOrDefault(d => d.IsCurrentDevice);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var previouslySelectedIds = SelectedDevices.Select(d => d.DeviceId).ToHashSet();
                var isFirstLoad = SelectedDevices.Count == 0;

                AvailableDevices.Clear();
                foreach (var device in devices)
                {
                    bool isSelected;
                    if (isFirstLoad)
                    {
                        isSelected = device.IsCurrentDevice;
                    }
                    else
                    {
                        isSelected = previouslySelectedIds.Contains(device.DeviceId);
                    }

                    AvailableDevices.Add(new SelectableDeviceViewModel(device, isSelected));
                }

                SelectedDevices.Clear();
                foreach (var item in AvailableDevices.Where(d => d.IsSelected))
                {
                    SelectedDevices.Add(item.Device);
                }

                UpdateSelectionSummary();
            });
        }
        catch (Exception ex)
        {
            SetStatus($"Error loading devices: {ex.Message}", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SelectFileAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync();
            if (result != null)
            {
                SelectedFilePath = result.FullPath;
                SelectedFileName = result.FileName;

                try
                {
                    var fileInfo = new FileInfo(result.FullPath);
                    SelectedFileSizeText = $"Size: {fileInfo.Length / 1024.0:F1} KB";
                }
                catch
                {
                    SelectedFileSizeText = string.Empty;
                }

                UpdateCanEncrypt();
                ClearStatus();
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Error selecting file: {ex.Message}", isError: true);
        }
    }

    [RelayCommand]
    public void ToggleDevice(SelectableDeviceViewModel item)
    {
        if (item == null) return;

        item.IsSelected = !item.IsSelected;

        var existing = SelectedDevices.FirstOrDefault(d => d.DeviceId == item.Device.DeviceId);
        if (item.IsSelected)
        {
            if (existing == null)
            {
                SelectedDevices.Add(item.Device);
            }
        }
        else
        {
            if (existing != null)
            {
                SelectedDevices.Remove(existing);
            }
        }

        UpdateSelectionSummary();
    }

    private void UpdateSelectionSummary()
    {
        SelectedDeviceCountText = $"{SelectedDevices.Count} of {AvailableDevices.Count} devices authorized";
        UpdateCanEncrypt();
    }

    private void UpdateCanEncrypt()
    {
        CanEncrypt = !string.IsNullOrEmpty(SelectedFilePath) && SelectedDevices.Count > 0 && !IsBusy;
    }

    [RelayCommand]
    public async Task EncryptAsync()
    {
        if (string.IsNullOrEmpty(SelectedFilePath) || SelectedDevices.Count == 0)
        {
            SetStatus("Please select a file and at least one authorized device.", isError: true);
            return;
        }

        if (IsBusy) return;
        IsBusy = true;
        ClearStatus();

        try
        {
            var isAndroid = DeviceInfo.Platform == DevicePlatform.Android;
            var dir = isAndroid
                ? FileSystem.CacheDirectory
                : Path.GetDirectoryName(SelectedFilePath) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            var baseName = Path.GetFileName(SelectedFilePath);
            var outputFilePath = Path.Combine(dir, $"{baseName}.mace");

            var request = new EncryptionRequest
            {
                SourceFilePath = SelectedFilePath,
                OutputFilePath = outputFilePath,
                AuthorizedDevices = SelectedDevices.ToList()
            };

            var success = await _cryptoService.EncryptFileAsync(request);
            if (success)
            {
                if (isAndroid)
                {
                    SetStatus($"File protected! Saved to cache. Tap Share to send.", isError: false);
                    await Share.Default.RequestAsync(new ShareFileRequest
                    {
                        Title = "Share Protected .mace Document",
                        File = new ShareFile(outputFilePath)
                    });
                }
                else
                {
                    SetStatus($"File protected successfully! Saved to: {outputFilePath}", isError: false);
                }
            }
            else
            {
                SetStatus("Protection failed.", isError: true);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Encryption failed: {ex.Message}", isError: true);
        }
        finally
        {
            IsBusy = false;
            UpdateCanEncrypt();
        }
    }
}
