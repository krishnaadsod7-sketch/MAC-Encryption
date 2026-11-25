using Android.Security.Keystore;
using Java.Security;
using Java.Security.Spec;
using Javax.Crypto;
using Javax.Crypto.Spec;
using MacE.Services.Interfaces;
using System.IO;
using System.Security.Cryptography;

namespace MacE.Platforms.Android;

/// <summary>
/// Android-specific provider for secure RSA private key storage using the Android Keystore system.
/// 
/// Security properties:
/// 1. RSA-3072 key pair is generated inside the Android Keystore hardware-backed provider.
/// 2. The private key never leaves the Keystore (non-exportable by design).
/// 3. All key unwrapping/decryption operations are performed within the Keystore boundary.
/// 4. Protected against extraction by app sandboxing, SELinux, and TEE/StrongBox where available.
/// </summary>
public class AndroidKeystoreKeyStorage : IKeyStorageProvider
{
    private const string AndroidKeyStore = "AndroidKeyStore";
    private const string KeyAliasPrefix = "mace_key_";

    private static string GetAlias(string deviceId)
    {
        return $"{KeyAliasPrefix}{deviceId.Replace("-", "_")}";
    }

    private static KeyStore GetKeyStore()
    {
        var keyStore = KeyStore.GetInstance(AndroidKeyStore);
        if (keyStore == null)
            throw new InvalidOperationException("Failed to obtain AndroidKeyStore instance.");

        keyStore.Load(null);
        return keyStore;
    }

    public Task<byte[]> GenerateAndStoreKeyPairAsync(string deviceId)
    {
        return Task.Run(() =>
        {
            var alias = GetAlias(deviceId);
            var keyStore = GetKeyStore();

            if (keyStore.ContainsAlias(alias))
            {
                keyStore.DeleteEntry(alias);
            }

            var kpg = KeyPairGenerator.GetInstance(KeyProperties.KeyAlgorithmRsa, AndroidKeyStore);
            if (kpg == null)
                throw new InvalidOperationException("Failed to initialize AndroidKeyStore KeyPairGenerator.");

            var builder = new KeyGenParameterSpec.Builder(
                alias,
                KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt)
                .SetKeySize(3072)
                .SetDigests(KeyProperties.DigestSha256, KeyProperties.DigestSha1)
                .SetEncryptionPaddings(KeyProperties.EncryptionPaddingRsaOaep);

            kpg.Initialize(builder.Build());
            var keyPair = kpg.GenerateKeyPair();

            var pubBytes = keyPair?.Public?.GetEncoded();
            if (pubBytes == null)
                throw new InvalidOperationException("Failed to export public key from Android Keystore.");

            return pubBytes;
        });
    }

    public Task<byte[]> UnwrapAsync(string deviceId, byte[] wrappedData)
    {
        return Task.Run(() =>
        {
            var alias = GetAlias(deviceId);
            var keyStore = GetKeyStore();

            if (!keyStore.ContainsAlias(alias))
            {
                throw new FileNotFoundException($"Device key '{deviceId}' was not found in Android Keystore.");
            }

            var key = keyStore.GetKey(alias, null);
            if (key is not IPrivateKey privateKey)
            {
                throw new InvalidOperationException($"Unable to access private key entry for device '{deviceId}'.");
            }

            var cipher = Cipher.GetInstance("RSA/ECB/OAEPWithSHA-256AndMGF1Padding");
            if (cipher == null)
                throw new InvalidOperationException("Failed to initialize RSA OAEP cipher.");

            var oaepParams = new OAEPParameterSpec(
                "SHA-256",
                "MGF1",
                MGF1ParameterSpec.Sha1,
                PSource.PSpecified.Default);

            cipher.Init(Javax.Crypto.CipherMode.DecryptMode, privateKey, oaepParams);
            var unwrapped = cipher.DoFinal(wrappedData);

            if (unwrapped == null)
                throw new CryptographicException("Android Keystore unwrapping failed.");

            return unwrapped;
        });
    }

    public Task<bool> HasPrivateKeyAsync(string deviceId)
    {
        try
        {
            var alias = GetAlias(deviceId);
            var keyStore = GetKeyStore();
            return Task.FromResult(keyStore.ContainsAlias(alias));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task DeleteKeyAsync(string deviceId)
    {
        try
        {
            var alias = GetAlias(deviceId);
            var keyStore = GetKeyStore();
            if (keyStore.ContainsAlias(alias))
            {
                keyStore.DeleteEntry(alias);
            }
        }
        catch
        {
            // Ignore deletion errors if key is already absent
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// Android-specific device information provider.
/// </summary>
public class AndroidDeviceInfoProvider : IDeviceInfoProvider
{
    public string GetMachineName()
    {
        var model = DeviceInfo.Current.Model;
        var manufacturer = DeviceInfo.Current.Manufacturer;

        if (string.IsNullOrWhiteSpace(manufacturer))
            return model;

        if (model.StartsWith(manufacturer, StringComparison.OrdinalIgnoreCase))
            return model;

        return $"{manufacturer} {model}".Trim();
    }

    public string GetPlatform()
    {
        return $"Android {DeviceInfo.Current.VersionString}";
    }

    public string? GetMacAddress()
    {
        return null;
    }
}
