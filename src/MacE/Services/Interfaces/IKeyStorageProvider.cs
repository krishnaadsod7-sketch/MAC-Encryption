namespace MacE.Services.Interfaces;

/// <summary>
/// Platform-specific provider for secure private key storage.
/// 
/// Windows implementation: encrypts the RSA private key using DPAPI
/// (ProtectedData.Protect with CurrentUser scope) and stores the
/// encrypted blob in the app's local data directory.
/// 
/// Android implementation: generates the RSA key pair inside the
/// Android Keystore, which provides hardware-backed key protection
/// on supported devices. The private key never leaves the Keystore.
/// </summary>
public interface IKeyStorageProvider
{
    /// <summary>
    /// Generate a new RSA-3072 key pair and store the private key securely.
    /// Returns the public key in DER-encoded SubjectPublicKeyInfo format.
    /// </summary>
    /// <param name="deviceId">Unique device identifier used as the storage key.</param>
    Task<byte[]> GenerateAndStoreKeyPairAsync(string deviceId);

    /// <summary>
    /// Decrypt (unwrap) data using the stored private key.
    /// Uses RSA-OAEP with SHA-256 padding.
    /// </summary>
    /// <param name="deviceId">Which device's private key to use.</param>
    /// <param name="wrappedData">Data encrypted with the corresponding public key.</param>
    /// <returns>The decrypted (unwrapped) data.</returns>
    Task<byte[]> UnwrapAsync(string deviceId, byte[] wrappedData);

    /// <summary>
    /// Check whether a private key exists for the given device identifier.
    /// </summary>
    Task<bool> HasPrivateKeyAsync(string deviceId);

    /// <summary>
    /// Permanently delete the stored private key for a device.
    /// </summary>
    Task DeleteKeyAsync(string deviceId);
}
