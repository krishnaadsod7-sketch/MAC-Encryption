namespace MacE.Models;

/// <summary>
/// Represents a registered device's identity in MAC-E.
/// Each device has a unique cryptographic key pair. The public key is shared;
/// the private key stays on the device protected by DPAPI or Android Keystore.
/// </summary>
public class DeviceIdentity
{
    /// <summary>
    /// Unique identifier for this device (UUID v4, generated at enrollment).
    /// This is NOT a MAC address — it is a random, cryptographically meaningless ID
    /// used only as a lookup key.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name chosen by the user (e.g., "Office Laptop").
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// RSA-3072 public key in Base64-encoded DER (SubjectPublicKeyInfo) format.
    /// Used by other devices to wrap AES file-encryption keys for this device.
    /// </summary>
    public string PublicKeyBase64 { get; set; } = string.Empty;

    /// <summary>
    /// When this device was enrolled.
    /// </summary>
    public DateTime EnrolledAt { get; set; }

    /// <summary>
    /// Operating system of the device (e.g., "Windows", "Android").
    /// Informational only — not used for security decisions.
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// Machine/host name of the device. Informational only.
    /// </summary>
    public string MachineName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the current device (the one running the app).
    /// Not persisted — set at runtime after comparing DeviceId with the local identity.
    /// </summary>
    public bool IsCurrentDevice { get; set; }
}
