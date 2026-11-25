using MacE.Models;
using MacE.Services.Interfaces;
using System.IO;
using System.Security.Cryptography;

namespace MacE.Services;

/// <summary>
/// Core cryptographic engine for MAC-E.
/// Implements authenticated AES-256-GCM file encryption and RSA-3072-OAEP key wrapping.
/// Resilient against key rotation: checks all authorized devices against locally available private keys.
/// </summary>
public class CryptoService : ICryptoService
{
    private readonly IMaceFileService _maceFileService;
    private readonly IDeviceService _deviceService;
    private readonly IKeyStorageProvider _keyStorageProvider;
    private readonly IWorkspaceService _workspaceService;

    public CryptoService(
        IMaceFileService maceFileService,
        IDeviceService deviceService,
        IKeyStorageProvider keyStorageProvider,
        IWorkspaceService workspaceService)
    {
        _maceFileService = maceFileService;
        _deviceService = deviceService;
        _keyStorageProvider = keyStorageProvider;
        _workspaceService = workspaceService;
    }

    public async Task<bool> EncryptFileAsync(EncryptionRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (!File.Exists(request.SourceFilePath))
            throw new FileNotFoundException($"Source file not found: {request.SourceFilePath}");

        if (request.AuthorizedDevices == null || request.AuthorizedDevices.Count == 0)
            throw new InvalidOperationException("At least one authorized device must be selected.");

        var plaintext = await File.ReadAllBytesAsync(request.SourceFilePath);

        // 1. Generate unique 256-bit AES file key and 96-bit GCM nonce
        var aesKey = new byte[32];
        var nonce = new byte[12];
        RandomNumberGenerator.Fill(aesKey);
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];

        try
        {
            // 2. Encrypt plaintext using AES-256-GCM
            using (var aesGcm = new AesGcm(aesKey, 16))
            {
                aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
            }

            // 3. Wrap AES key with public key for each authorized device
            var authorizedEntries = new List<AuthorizedDeviceEntry>();
            foreach (var device in request.AuthorizedDevices)
            {
                if (string.IsNullOrWhiteSpace(device.PublicKeyBase64))
                    continue;

                var publicKeyBytes = Convert.FromBase64String(device.PublicKeyBase64);
                using var rsa = RSA.Create();
                rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

                var wrappedKeyBytes = rsa.Encrypt(aesKey, RSAEncryptionPadding.OaepSHA256);

                authorizedEntries.Add(new AuthorizedDeviceEntry
                {
                    DeviceId = device.DeviceId,
                    DeviceName = device.DisplayName,
                    WrappedKeyBase64 = Convert.ToBase64String(wrappedKeyBytes)
                });
            }

            if (authorizedEntries.Count == 0)
                throw new InvalidOperationException("Failed to wrap encryption key for any selected device.");

            var currentDevice = await _deviceService.GetCurrentDeviceAsync();

            // 4. Construct .mace metadata
            var metadata = new MaceFileMetadata
            {
                Version = 1,
                CreatedAtUtc = DateTime.UtcNow,
                CreatorDeviceId = currentDevice?.DeviceId ?? "Unknown",
                OriginalFileName = Path.GetFileName(request.SourceFilePath),
                OriginalExtension = Path.GetExtension(request.SourceFilePath),
                EncryptionAlgorithm = "AES-256-GCM",
                KeyWrapAlgorithm = "RSA-OAEP-SHA256",
                NonceBase64 = Convert.ToBase64String(nonce),
                AuthTagBase64 = Convert.ToBase64String(tag),
                AuthorizedDevices = authorizedEntries
            };

            // 5. Write to .mace binary format
            await _maceFileService.WriteAsync(request.OutputFilePath, metadata, ciphertext);
            return true;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(aesKey);
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public async Task<DecryptionResult> DecryptFileAsync(string maceFilePath)
    {
        if (!File.Exists(maceFilePath))
            return DecryptionResult.Failed($"The file '{maceFilePath}' does not exist.");

        if (!await _maceFileService.IsValidMaceFileAsync(maceFilePath))
            return DecryptionResult.Failed("The selected file is not a valid MAC-E (.mace) protected file.");

        MaceFileMetadata metadata;
        try
        {
            metadata = await _maceFileService.ReadMetadataAsync(maceFilePath);
        }
        catch (Exception ex)
        {
            return DecryptionResult.Failed($"Corrupted .mace file header: {ex.Message}");
        }

        // 1. Identify which authorized device in the document corresponds to a private key on this machine
        var currentDevice = await _deviceService.GetCurrentDeviceAsync();
        AuthorizedDeviceEntry? matchedEntry = null;

        // Check if current active device matches
        if (currentDevice != null)
        {
            matchedEntry = metadata.AuthorizedDevices.FirstOrDefault(d => d.DeviceId == currentDevice.DeviceId);
        }

        // Fallback: check if this computer holds the private key for ANY of the authorized devices listed in the file
        if (matchedEntry == null)
        {
            foreach (var authDev in metadata.AuthorizedDevices)
            {
                if (await _keyStorageProvider.HasPrivateKeyAsync(authDev.DeviceId))
                {
                    matchedEntry = authDev;
                    break;
                }
            }
        }

        if (matchedEntry == null)
        {
            var deviceName = currentDevice?.DisplayName ?? "This device";
            var authorizedNames = string.Join(", ", metadata.AuthorizedDevices.Select(d => $"'{d.DeviceName}'"));
            return DecryptionResult.Failed($"Access Denied: Your device is not authorized to decrypt this document.\nAuthorized devices for this file: {authorizedNames}.");
        }

        byte[]? aesKey = null;
        byte[]? plaintext = null;

        try
        {
            // 2. Unwrap AES key using the matching device's hardware/DPAPI-protected private key
            var wrappedKeyBytes = Convert.FromBase64String(matchedEntry.WrappedKeyBase64);
            aesKey = await _keyStorageProvider.UnwrapAsync(matchedEntry.DeviceId, wrappedKeyBytes);

            // 3. Read ciphertext and decode nonce/tag
            var ciphertext = await _maceFileService.ReadCiphertextAsync(maceFilePath);
            var nonce = Convert.FromBase64String(metadata.NonceBase64);
            var tag = Convert.FromBase64String(metadata.AuthTagBase64);

            plaintext = new byte[ciphertext.Length];

            // 4. Decrypt and verify authentication tag
            using (var aesGcm = new AesGcm(aesKey, 16))
            {
                aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
            }

            // 5. Write plaintext to secure temporary workspace
            var tempFilePath = await _workspaceService.CreateTempFileAsync(plaintext, metadata.OriginalExtension);

            return DecryptionResult.Succeeded(tempFilePath, metadata.OriginalFileName, maceFilePath, metadata.AuthorizedDevices);
        }
        catch (AuthenticationTagMismatchException)
        {
            return DecryptionResult.Failed("Decryption failed: Integrity verification failed. The file has been modified or corrupted.");
        }
        catch (CryptographicException ex)
        {
            return DecryptionResult.Failed($"Cryptographic failure: {ex.Message}");
        }
        catch (Exception ex)
        {
            return DecryptionResult.Failed($"Failed to decrypt file: {ex.Message}");
        }
        finally
        {
            if (aesKey != null) CryptographicOperations.ZeroMemory(aesKey);
            if (plaintext != null) CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public async Task<bool> ReEncryptFromTempAsync(string tempFilePath, string originalFileName, List<AuthorizedDeviceEntry> originalAuthorizedDevices, string outputMaceFilePath)
    {
        if (!File.Exists(tempFilePath))
            throw new FileNotFoundException($"Temporary file not found: {tempFilePath}");

        if (originalAuthorizedDevices == null || originalAuthorizedDevices.Count == 0)
            throw new InvalidOperationException("No authorized devices to re-encrypt for.");

        var plaintext = await File.ReadAllBytesAsync(tempFilePath);

        var aesKey = new byte[32];
        var nonce = new byte[12];
        RandomNumberGenerator.Fill(aesKey);
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];

        try
        {
            using (var aesGcm = new AesGcm(aesKey, 16))
            {
                aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
            }

            // Re-wrap the AES key for each originally authorized device using their stored public keys
            var allDevices = await _deviceService.GetAllDevicesAsync();
            var reAuthorizedEntries = new List<AuthorizedDeviceEntry>();

            foreach (var original in originalAuthorizedDevices)
            {
                var registryDevice = allDevices.FirstOrDefault(d => d.DeviceId == original.DeviceId);
                var publicKeyBase64 = registryDevice?.PublicKeyBase64;

                if (string.IsNullOrWhiteSpace(publicKeyBase64))
                    continue; // Device no longer in registry — skip

                var publicKeyBytes = Convert.FromBase64String(publicKeyBase64);
                using var rsa = RSA.Create();
                rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

                var wrappedKeyBytes = rsa.Encrypt(aesKey, RSAEncryptionPadding.OaepSHA256);
                reAuthorizedEntries.Add(new AuthorizedDeviceEntry
                {
                    DeviceId = original.DeviceId,
                    DeviceName = original.DeviceName,
                    WrappedKeyBase64 = Convert.ToBase64String(wrappedKeyBytes)
                });
            }

            if (reAuthorizedEntries.Count == 0)
                throw new InvalidOperationException("None of the originally authorized devices are in your registry. Cannot re-encrypt safely.");

            var currentDevice = await _deviceService.GetCurrentDeviceAsync();

            var metadata = new MaceFileMetadata
            {
                Version = 1,
                CreatedAtUtc = DateTime.UtcNow,
                CreatorDeviceId = currentDevice?.DeviceId ?? "Unknown",
                OriginalFileName = string.IsNullOrWhiteSpace(originalFileName) ? Path.GetFileName(tempFilePath) : originalFileName,
                OriginalExtension = string.IsNullOrWhiteSpace(originalFileName) ? Path.GetExtension(tempFilePath) : Path.GetExtension(originalFileName),
                EncryptionAlgorithm = "AES-256-GCM",
                KeyWrapAlgorithm = "RSA-OAEP-SHA256",
                NonceBase64 = Convert.ToBase64String(nonce),
                AuthTagBase64 = Convert.ToBase64String(tag),
                AuthorizedDevices = reAuthorizedEntries
            };

            await _maceFileService.WriteAsync(outputMaceFilePath, metadata, ciphertext);
            return true;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(aesKey);
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public async Task<MaceFileMetadata?> ReadMetadataAsync(string maceFilePath)
    {
        try
        {
            return await _maceFileService.ReadMetadataAsync(maceFilePath);
        }
        catch
        {
            return null;
        }
    }
}
