using MacE.Helpers;
using MacE.Models;
using MacE.Services.Interfaces;
using System.IO;
using System.Text.Json;

namespace MacE.Services;

/// <summary>
/// Manages device enrollment, registry storage, peer device import/export, and removal.
/// Stores the device registry locally in AppPaths.RegistryFilePath.
/// Retains historical private keys so files encrypted in earlier sessions remain decryptable.
/// </summary>
public class DeviceService : IDeviceService
{
    private readonly IKeyStorageProvider _keyStorageProvider;
    private readonly IDeviceInfoProvider _deviceInfoProvider;
    private readonly string _registryFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private const string CurrentDeviceIdPrefKey = "MacE_CurrentDeviceId";

    public DeviceService(
        IKeyStorageProvider keyStorageProvider,
        IDeviceInfoProvider deviceInfoProvider)
    {
        _keyStorageProvider = keyStorageProvider;
        _deviceInfoProvider = deviceInfoProvider;
        _registryFilePath = AppPaths.RegistryFilePath;

        // Auto-migrate any legacy registry files from alternate paths
        MigrateLegacyRegistryFiles();
    }

    private void MigrateLegacyRegistryFiles()
    {
        try
        {
            // Check unpackaged MAUI AppData directory if different from AppPaths.RegistryFilePath
            var mauiDir = Path.Combine(FileSystem.AppDataDirectory, "MacE", "devices.json");
            if (File.Exists(mauiDir) && !File.Exists(_registryFilePath))
            {
                File.Copy(mauiDir, _registryFilePath, true);
            }
        }
        catch
        {
            // Ignore migration failure
        }
    }

    private async Task<List<DeviceIdentity>> LoadRegistryInternalAsync()
    {
        if (!File.Exists(_registryFilePath))
            return new List<DeviceIdentity>();

        try
        {
            var json = await File.ReadAllTextAsync(_registryFilePath);
            var list = JsonSerializer.Deserialize<List<DeviceIdentity>>(json);
            return list ?? new List<DeviceIdentity>();
        }
        catch
        {
            return new List<DeviceIdentity>();
        }
    }

    private async Task SaveRegistryInternalAsync(List<DeviceIdentity> devices)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(devices, options);
        await File.WriteAllTextAsync(_registryFilePath, json);
    }

    public async Task<DeviceIdentity> EnrollCurrentDeviceAsync(string displayName)
    {
        // --- GUARD: If this machine already has a valid registered identity, just update the display name ---
        // This prevents generating a brand-new keypair (and thus losing the ability to decrypt old files)
        // when the user clicks Register again on a device that is already enrolled.
        var existing = await GetCurrentDeviceAsync();
        if (existing != null)
        {
            await _lock.WaitAsync();
            try
            {
                var devices = await LoadRegistryInternalAsync();
                var idx = devices.FindIndex(d => d.DeviceId == existing.DeviceId);
                if (idx >= 0)
                {
                    if (!string.IsNullOrWhiteSpace(displayName))
                        devices[idx].DisplayName = displayName.Trim();
                    devices[idx].IsCurrentDevice = true;
                    await SaveRegistryInternalAsync(devices);
                    Preferences.Default.Set(CurrentDeviceIdPrefKey, existing.DeviceId);
                    return devices[idx];
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        // --- NEW DEVICE: Key generation is slow (RSA-3072); do it BEFORE acquiring the lock ---
        var deviceId = Guid.NewGuid().ToString();
        var publicKeyBytes = await _keyStorageProvider.GenerateAndStoreKeyPairAsync(deviceId);
        var publicKeyBase64 = Convert.ToBase64String(publicKeyBytes);

        var identity = new DeviceIdentity
        {
            DeviceId = deviceId,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? _deviceInfoProvider.GetMachineName() : displayName.Trim(),
            PublicKeyBase64 = publicKeyBase64,
            EnrolledAt = DateTime.UtcNow,
            Platform = _deviceInfoProvider.GetPlatform(),
            MachineName = _deviceInfoProvider.GetMachineName(),
            IsCurrentDevice = true
        };

        await _lock.WaitAsync();
        try
        {
            var devices = await LoadRegistryInternalAsync();

            // Note: We do NOT delete old private keys from disk!
            // Keeping historical private keys ensures files encrypted in earlier sessions can still be decrypted.
            foreach (var d in devices)
                d.IsCurrentDevice = false;

            devices.Insert(0, identity);
            await SaveRegistryInternalAsync(devices);
            Preferences.Default.Set(CurrentDeviceIdPrefKey, deviceId);
            return identity;
        }
        finally
        {
            _lock.Release();
        }
    }

    // Internal version — caller must already hold _lock
    private async Task<DeviceIdentity?> GetCurrentDeviceNoLockAsync(List<DeviceIdentity> devices)
    {
        var currentId = Preferences.Default.Get(CurrentDeviceIdPrefKey, string.Empty);

        if (!string.IsNullOrEmpty(currentId))
        {
            var current = devices.FirstOrDefault(d => d.DeviceId == currentId);
            if (current != null && await _keyStorageProvider.HasPrivateKeyAsync(current.DeviceId))
            {
                current.IsCurrentDevice = true;
                return current;
            }
        }

        // Fallback: look for any device in registry that possesses a valid private key on this machine
        foreach (var d in devices)
        {
            if (await _keyStorageProvider.HasPrivateKeyAsync(d.DeviceId))
            {
                d.IsCurrentDevice = true;
                Preferences.Default.Set(CurrentDeviceIdPrefKey, d.DeviceId);
                return d;
            }
        }

        return null;
    }

    public async Task<DeviceIdentity?> GetCurrentDeviceAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var devices = await LoadRegistryInternalAsync();
            return await GetCurrentDeviceNoLockAsync(devices);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<DeviceIdentity>> GetAllDevicesAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var devices = await LoadRegistryInternalAsync();
            var current = await GetCurrentDeviceNoLockAsync(devices);

            foreach (var device in devices)
                device.IsCurrentDevice = current != null && device.DeviceId == current.DeviceId;

            return devices;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task AddPeerDeviceAsync(DeviceIdentity peerDevice)
    {
        if (peerDevice == null || string.IsNullOrWhiteSpace(peerDevice.DeviceId) || string.IsNullOrWhiteSpace(peerDevice.PublicKeyBase64))
            throw new ArgumentException("Invalid device identity: DeviceId and PublicKey are required.");

        await _lock.WaitAsync();
        try
        {
            var devices = await LoadRegistryInternalAsync();
            var existingIndex = devices.FindIndex(d => d.DeviceId == peerDevice.DeviceId);

            peerDevice.IsCurrentDevice = false;

            if (existingIndex >= 0)
            {
                devices[existingIndex] = peerDevice;
            }
            else
            {
                devices.Add(peerDevice);
            }

            await SaveRegistryInternalAsync(devices);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RemoveDeviceAsync(string deviceId)
    {
        await _lock.WaitAsync();
        try
        {
            var currentId = Preferences.Default.Get(CurrentDeviceIdPrefKey, string.Empty);
            var devices = await LoadRegistryInternalAsync();

            devices.RemoveAll(d => d.DeviceId == deviceId);
            await SaveRegistryInternalAsync(devices);

            if (deviceId == currentId)
            {
                Preferences.Default.Remove(CurrentDeviceIdPrefKey);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string> ExportCurrentDeviceIdentityAsync()
    {
        var current = await GetCurrentDeviceAsync();
        if (current == null)
            throw new InvalidOperationException("This device has not been registered yet. Register it first before exporting.");

        var exportModel = new
        {
            version = 1,
            type = "mace-device-identity",
            deviceId = current.DeviceId,
            displayName = current.DisplayName,
            publicKey = current.PublicKeyBase64,
            enrolledAt = current.EnrolledAt,
            platform = current.Platform,
            machineName = current.MachineName
        };

        return JsonSerializer.Serialize(exportModel, new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task<DeviceIdentity> ImportDeviceIdentityAsync(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var deviceId = root.GetProperty("deviceId").GetString();
        var displayName = root.GetProperty("displayName").GetString();
        var publicKey = root.GetProperty("publicKey").GetString();

        if (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(publicKey))
            throw new InvalidDataException("Invalid .mace-device format: missing deviceId or publicKey.");

        var platform = root.TryGetProperty("platform", out var p) ? p.GetString() ?? "Unknown" : "Unknown";
        var machineName = root.TryGetProperty("machineName", out var m) ? m.GetString() ?? "" : "";
        var enrolledAt = root.TryGetProperty("enrolledAt", out var e) && e.TryGetDateTime(out var dt) ? dt : DateTime.UtcNow;

        var peer = new DeviceIdentity
        {
            DeviceId = deviceId,
            DisplayName = displayName ?? "Remote Device",
            PublicKeyBase64 = publicKey,
            Platform = platform,
            MachineName = machineName,
            EnrolledAt = enrolledAt,
            IsCurrentDevice = false
        };

        await AddPeerDeviceAsync(peer);
        return peer;
    }
}
