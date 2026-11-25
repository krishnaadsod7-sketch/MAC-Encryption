using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacE.Models;
using MacE.Services.Interfaces;
using System.IO;

namespace MacE.ViewModels;

/// <summary>
/// ViewModel for the Decrypt page.
/// Select .mace file -> Inspect authorization -> Unwrap AES key -> Decrypt -> Launch viewer.
/// Optionally: edit in external app -> Re-encrypt and Save back to .mace.
/// </summary>
public partial class DecryptViewModel : BaseViewModel
{
    private readonly ICryptoService _cryptoService;
    private readonly IDeviceService _deviceService;
    private readonly IWorkspaceService _workspaceService;

    // Held in memory after a successful decryption so the Save command can use them
    private string? _activeTempFilePath;
    private string? _activeSourceMacePath;
    private List<AuthorizedDeviceEntry> _activeAuthorizedDevices = new();

    public DecryptViewModel(
        ICryptoService cryptoService,
        IDeviceService deviceService,
        IWorkspaceService workspaceService)
    {
        _cryptoService = cryptoService;
        _deviceService = deviceService;
        _workspaceService = workspaceService;
        Title = "Decrypt";
    }

    [ObservableProperty]
    private string _selectedFilePath = string.Empty;

    [ObservableProperty]
    private string _selectedFileName = "No file selected";

    [ObservableProperty]
    private string _fileMetadataSummary = string.Empty;

    [ObservableProperty]
    private bool _isAuthorizedOnThisDevice;

    [ObservableProperty]
    private bool _canDecrypt;

    // True once a file has been successfully decrypted — shows the save panel
    [ObservableProperty]
    private bool _isDecrypted;

    [ObservableProperty]
    private string _decryptedFileName = string.Empty;

    [RelayCommand]
    public async Task SelectFileAsync()
    {
        ClearStatus();
        ResetDecryptedState();

        try
        {
            var customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".mace" } },
                    { DevicePlatform.Android, new[] { "*/*" } }
                });

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select a .mace file to decrypt",
                FileTypes = customFileType
            });

            if (result != null)
            {
                SelectedFilePath = result.FullPath;
                SelectedFileName = result.FileName;

                var metadata = await _cryptoService.ReadMetadataAsync(result.FullPath);
                if (metadata != null)
                {
                    var currentDevice = await _deviceService.GetCurrentDeviceAsync();
                    var isAuth = currentDevice != null &&
                                 metadata.AuthorizedDevices.Any(d => d.DeviceId == currentDevice.DeviceId);

                    IsAuthorizedOnThisDevice = isAuth;
                    FileMetadataSummary = $"Original: {metadata.OriginalFileName} | Authorized Devices: {metadata.AuthorizedDevices.Count} | Created: {metadata.CreatedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm}";
                    CanDecrypt = true;
                }
                else
                {
                    FileMetadataSummary = "Unable to read .mace metadata.";
                    CanDecrypt = false;
                }
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Error selecting file: {ex.Message}", isError: true);
        }
    }

    [RelayCommand]
    public async Task DecryptAsync()
    {
        if (string.IsNullOrEmpty(SelectedFilePath))
        {
            SetStatus("Please select a .mace file first.", isError: true);
            return;
        }

        if (IsBusy) return;
        IsBusy = true;
        ClearStatus();

        try
        {
            var result = await _cryptoService.DecryptFileAsync(SelectedFilePath);
            if (result.Success && !string.IsNullOrEmpty(result.TempFilePath))
            {
                // Store state for the save-back flow
                _activeTempFilePath = result.TempFilePath;
                _activeSourceMacePath = result.SourceMaceFilePath;
                _activeAuthorizedDevices = result.AuthorizedDevices;
                DecryptedFileName = result.OriginalFileName ?? Path.GetFileName(result.TempFilePath);

                IsDecrypted = true;
                SetStatus($"Decrypted successfully! Edit the file, then click 'Save Encrypted' when done.", isError: false);
                await _workspaceService.LaunchFileAsync(result.TempFilePath);
            }
            else
            {
                SetStatus(result.ErrorMessage ?? "Decryption failed.", isError: true);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Decryption failed: {ex.Message}", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Re-encrypt the currently open temp file and save it as a .mace file.
    /// The user can choose to overwrite the original or save to a new location.
    /// </summary>
    [RelayCommand]
    public async Task SaveEncryptedAsync()
    {
        if (string.IsNullOrEmpty(_activeTempFilePath) || !File.Exists(_activeTempFilePath))
        {
            SetStatus("No decrypted file found. Please decrypt a file first.", isError: true);
            return;
        }

        if (IsBusy) return;
        IsBusy = true;
        ClearStatus();

        try
        {
            // Let user choose where to save the new .mace
            var originalBaseName = Path.GetFileNameWithoutExtension(DecryptedFileName);
            var suggestedName = $"{originalBaseName}.mace";

            // On Windows: save back directly to original .mace if available, or desktop
            string? outputPath = null;

            if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                // Use desktop as default save location if no original path
                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (!Directory.Exists(desktop))
                    desktop = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                outputPath = Path.Combine(desktop, suggestedName);

                // If the original .mace path is known, overwrite it directly
                if (!string.IsNullOrEmpty(_activeSourceMacePath) && File.Exists(_activeSourceMacePath))
                    outputPath = _activeSourceMacePath;
            }
            else
            {
                // Android: save to cache then share
                outputPath = Path.Combine(FileSystem.CacheDirectory, suggestedName);
            }

            await _cryptoService.ReEncryptFromTempAsync(
                _activeTempFilePath, 
                DecryptedFileName, 
                _activeAuthorizedDevices, 
                outputPath!);

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Save Encrypted File",
                    File = new ShareFile(outputPath!)
                });
                SetStatus("File re-encrypted and shared.", isError: false);
            }
            else
            {
                SetStatus($"Saved encrypted file to: {Path.GetFileName(outputPath)}", isError: false);
            }

            // Clean up the temp file after successful save
            await _workspaceService.CleanupFileAsync(_activeTempFilePath);
            ResetDecryptedState();
        }
        catch (Exception ex)
        {
            SetStatus($"Save failed: {ex.Message}", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Discard the decrypted temp file without saving.
    /// </summary>
    [RelayCommand]
    public async Task DiscardChangesAsync()
    {
        if (!string.IsNullOrEmpty(_activeTempFilePath))
            await _workspaceService.CleanupFileAsync(_activeTempFilePath);

        ResetDecryptedState();
        SetStatus("Changes discarded. Temporary file deleted.", isError: false);
    }

    private void ResetDecryptedState()
    {
        _activeTempFilePath = null;
        _activeSourceMacePath = null;
        _activeAuthorizedDevices = new();
        IsDecrypted = false;
        DecryptedFileName = string.Empty;
    }
}
