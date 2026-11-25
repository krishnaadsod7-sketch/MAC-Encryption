namespace MacE.Models;

/// <summary>
/// Parameters for a file encryption operation.
/// Created by the EncryptViewModel and passed to CryptoService.
/// </summary>
public class EncryptionRequest
{
    /// <summary>
    /// Full path to the file to encrypt.
    /// </summary>
    public string SourceFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Full path where the resulting .mace file should be saved.
    /// </summary>
    public string OutputFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Devices authorized to decrypt the file.
    /// Each device's public key will be used to wrap the AES key.
    /// </summary>
    public List<DeviceIdentity> AuthorizedDevices { get; set; } = new();
}
