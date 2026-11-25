using MacE.Models;

namespace MacE.Services.Interfaces;

/// <summary>
/// Manages device enrollment, lookup, and removal.
/// Coordinates with IKeyStorageProvider for key pair generation
/// and with IDeviceInfoProvider for platform information.
/// </summary>
public interface IDeviceService
{
    /// <summary>
    /// Enroll the current device: generate a key pair, store the private key securely,
    /// and save the device identity record. Returns the new DeviceIdentity.
    /// </summary>
    Task<DeviceIdentity> EnrollCurrentDeviceAsync(string displayName);

    /// <summary>
    /// Get the identity of the current device, or null if it has not been enrolled.
    /// </summary>
    Task<DeviceIdentity?> GetCurrentDeviceAsync();

    /// <summary>
    /// Get all registered devices (this device + any imported peer devices).
    /// </summary>
    Task<List<DeviceIdentity>> GetAllDevicesAsync();

    /// <summary>
    /// Import a peer device's public identity (received via .mace-device file).
    /// The private key is NOT imported — only the public key and metadata.
    /// </summary>
    Task AddPeerDeviceAsync(DeviceIdentity peerDevice);

    /// <summary>
    /// Remove a device from the registry. If it is the current device,
    /// also delete the stored private key.
    /// </summary>
    Task RemoveDeviceAsync(string deviceId);

    /// <summary>
    /// Export the current device's public identity for sharing with other devices.
    /// Returns a JSON string that can be saved as a .mace-device file.
    /// The private key is NEVER included in the export.
    /// </summary>
    Task<string> ExportCurrentDeviceIdentityAsync();

    /// <summary>
    /// Import a device identity from a .mace-device JSON string.
    /// </summary>
    Task<DeviceIdentity> ImportDeviceIdentityAsync(string json);
}
