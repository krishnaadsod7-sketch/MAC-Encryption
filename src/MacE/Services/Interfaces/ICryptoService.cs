using MacE.Models;

namespace MacE.Services.Interfaces;

/// <summary>
/// Core cryptographic operations for MAC-E.
/// Handles AES-256-GCM file encryption/decryption and RSA-OAEP key wrapping.
/// 
/// SECURITY NOTE: This service uses .NET's built-in cryptographic libraries.
/// No algorithms are implemented from scratch.
/// </summary>
public interface ICryptoService
{
    /// <summary>
    /// Encrypt a file and produce a .mace file.
    /// 
    /// Steps performed:
    /// 1. Generate a random AES-256 key (32 bytes).
    /// 2. Generate a random 12-byte nonce.
    /// 3. Encrypt the file contents using AES-256-GCM.
    /// 4. Wrap the AES key with each authorized device's RSA public key.
    /// 5. Write the .mace file (header + ciphertext).
    /// 6. Securely clear the AES key from memory.
    /// </summary>
    Task<bool> EncryptFileAsync(EncryptionRequest request);

    /// <summary>
    /// Attempt to decrypt a .mace file using the current device's private key.
    /// 
    /// Steps performed:
    /// 1. Read and parse the .mace file header.
    /// 2. Find the current device's entry in the authorized device list.
    /// 3. Unwrap the AES key using the device's private key.
    /// 4. Decrypt the ciphertext using AES-256-GCM.
    /// 5. Verify the authentication tag (rejects tampered files).
    /// 6. Write the plaintext to a temporary file.
    /// </summary>
    Task<DecryptionResult> DecryptFileAsync(string maceFilePath);

    /// <summary>
    /// Read only the metadata header from a .mace file without decrypting the payload.
    /// Useful for displaying file information and checking authorization.
    /// </summary>
    Task<MaceFileMetadata?> ReadMetadataAsync(string maceFilePath);

    /// <summary>
    /// Re-encrypt an edited temporary file back to a .mace file,
    /// preserving the original file name and the same authorized device list as the original.
    /// </summary>
    Task<bool> ReEncryptFromTempAsync(string tempFilePath, string originalFileName, List<AuthorizedDeviceEntry> originalAuthorizedDevices, string outputMaceFilePath);
}
