namespace MacE.Models;

/// <summary>
/// Represents one authorized device inside a .mace file.
/// Contains the device identifier and the AES key wrapped with that device's public key.
/// </summary>
public class AuthorizedDeviceEntry
{
    /// <summary>
    /// The device's unique identifier (matches DeviceIdentity.DeviceId).
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the device at the time of encryption. Stored for convenience
    /// so the file can show which devices have access without looking them up.
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// The AES-256 file-encryption key, encrypted (wrapped) with this device's
    /// RSA-3072 public key using OAEP-SHA256 padding.
    /// Base64-encoded.
    /// </summary>
    public string WrappedKeyBase64 { get; set; } = string.Empty;
}
