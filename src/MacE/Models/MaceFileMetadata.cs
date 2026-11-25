namespace MacE.Models;

/// <summary>
/// The metadata stored in the JSON header of a .mace file.
/// This is everything needed to identify, validate, and decrypt the file —
/// except the actual ciphertext, which follows the header as raw bytes.
/// </summary>
public class MaceFileMetadata
{
    /// <summary>
    /// File format version. Increment when the format changes in a breaking way.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// When the .mace file was created (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// DeviceId of the device that created this .mace file.
    /// </summary>
    public string CreatorDeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Original file name before encryption (e.g., "report.docx").
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// Original file extension including the dot (e.g., ".docx").
    /// Stored separately for quick lookup when opening in an external application.
    /// </summary>
    public string OriginalExtension { get; set; } = string.Empty;

    /// <summary>
    /// Encryption algorithm identifier. Always "AES-256-GCM" in format version 1.
    /// </summary>
    public string EncryptionAlgorithm { get; set; } = "AES-256-GCM";

    /// <summary>
    /// Key wrapping algorithm identifier. Always "RSA-OAEP-SHA256" in format version 1.
    /// </summary>
    public string KeyWrapAlgorithm { get; set; } = "RSA-OAEP-SHA256";

    /// <summary>
    /// The 12-byte AES-GCM nonce (IV), Base64-encoded.
    /// Must be unique per encryption operation. Generated using a secure random source.
    /// </summary>
    public string NonceBase64 { get; set; } = string.Empty;

    /// <summary>
    /// The 16-byte AES-GCM authentication tag, Base64-encoded.
    /// Used to verify that neither the ciphertext nor the authenticated data
    /// has been tampered with.
    /// </summary>
    public string AuthTagBase64 { get; set; } = string.Empty;

    /// <summary>
    /// HMAC-SHA256 of this metadata section (excluding this field itself),
    /// keyed with the AES file-encryption key.
    /// Protects against metadata tampering (e.g., changing the original filename).
    /// Base64-encoded.
    /// </summary>
    public string MetadataHmacBase64 { get; set; } = string.Empty;

    /// <summary>
    /// List of devices authorized to decrypt this file.
    /// Each entry contains the device identifier and the wrapped AES key.
    /// </summary>
    public List<AuthorizedDeviceEntry> AuthorizedDevices { get; set; } = new();
}
