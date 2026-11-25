using MacE.Helpers;
using MacE.Services.Interfaces;
using System.ComponentModel;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace MacE.Platforms.Windows;

/// <summary>
/// Windows-specific provider for secure RSA private key storage using Windows DPAPI (Crypt32.dll).
/// Keys are stored in AppPaths.KeysDirectory (%LOCALAPPDATA%\MacE\Keys\).
/// </summary>
public class WindowsDpapiKeyStorage : IKeyStorageProvider
{
    public WindowsDpapiKeyStorage()
    {
        Directory.CreateDirectory(AppPaths.KeysDirectory);
    }

    private string GetKeyFilePath(string deviceId)
    {
        var safeId = string.Join("_", deviceId.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(AppPaths.KeysDirectory, $"{safeId}.key");
    }

    public Task<byte[]> GenerateAndStoreKeyPairAsync(string deviceId)
    {
        return Task.Run(() =>
        {
            using var rsa = RSA.Create(3072);

            // Export private key in PKCS#8 format
            var privateKeyBytes = rsa.ExportPkcs8PrivateKey();

            try
            {
                // Encrypt with native Windows DPAPI (bound to current user)
                var encryptedKey = ProtectData(privateKeyBytes);

                var keyFilePath = GetKeyFilePath(deviceId);
                File.WriteAllBytes(keyFilePath, encryptedKey);

                // Export public key in SubjectPublicKeyInfo (DER) format
                var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
                return publicKeyBytes;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(privateKeyBytes);
            }
        });
    }

    public Task<byte[]> UnwrapAsync(string deviceId, byte[] wrappedData)
    {
        return Task.Run(() =>
        {
            var keyFilePath = GetKeyFilePath(deviceId);
            if (!File.Exists(keyFilePath))
            {
                throw new FileNotFoundException($"Private key for device '{deviceId}' was not found in key storage.");
            }

            var encryptedKey = File.ReadAllBytes(keyFilePath);
            var privateKeyBytes = UnprotectData(encryptedKey);

            try
            {
                using var rsa = RSA.Create();
                rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);

                var unwrappedKey = rsa.Decrypt(wrappedData, RSAEncryptionPadding.OaepSHA256);
                return unwrappedKey;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(privateKeyBytes);
            }
        });
    }

    public Task<bool> HasPrivateKeyAsync(string deviceId)
    {
        var keyFilePath = GetKeyFilePath(deviceId);
        return Task.FromResult(File.Exists(keyFilePath));
    }

    public Task DeleteKeyAsync(string deviceId)
    {
        var keyFilePath = GetKeyFilePath(deviceId);
        if (File.Exists(keyFilePath))
        {
            try
            {
                var length = new FileInfo(keyFilePath).Length;
                File.WriteAllBytes(keyFilePath, new byte[length]);
                File.Delete(keyFilePath);
            }
            catch
            {
                try { File.Delete(keyFilePath); } catch { }
            }
        }
        return Task.CompletedTask;
    }

    #region Native Win32 DPAPI Interop

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DATA_BLOB
    {
        public int cbData;
        public IntPtr pbData;
    }

    private const int CRYPTPROTECT_UI_FORBIDDEN = 0x1;

    [DllImport("Crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool CryptProtectData(
        ref DATA_BLOB pDataIn,
        string? szDataDescr,
        ref DATA_BLOB pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        int dwFlags,
        ref DATA_BLOB pDataOut);

    [DllImport("Crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool CryptUnprotectData(
        ref DATA_BLOB pDataIn,
        IntPtr ppszDataDescr,
        ref DATA_BLOB pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        int dwFlags,
        ref DATA_BLOB pDataOut);

    [DllImport("Kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);

    private static byte[] ProtectData(byte[] plainBytes)
    {
        var inBlob = new DATA_BLOB();
        var outBlob = new DATA_BLOB();
        var entropyBlob = new DATA_BLOB();

        var inPtr = Marshal.AllocHGlobal(plainBytes.Length);
        try
        {
            Marshal.Copy(plainBytes, 0, inPtr, plainBytes.Length);
            inBlob.cbData = plainBytes.Length;
            inBlob.pbData = inPtr;

            var success = CryptProtectData(
                ref inBlob,
                "MacE_DeviceKey",
                ref entropyBlob,
                IntPtr.Zero,
                IntPtr.Zero,
                CRYPTPROTECT_UI_FORBIDDEN,
                ref outBlob);

            if (!success)
            {
                var err = Marshal.GetLastWin32Error();
                throw new Win32Exception(err, $"CryptProtectData failed with error code {err}.");
            }

            var encryptedBytes = new byte[outBlob.cbData];
            Marshal.Copy(outBlob.pbData, encryptedBytes, 0, outBlob.cbData);
            return encryptedBytes;
        }
        finally
        {
            Marshal.FreeHGlobal(inPtr);
            if (outBlob.pbData != IntPtr.Zero)
            {
                LocalFree(outBlob.pbData);
            }
        }
    }

    private static byte[] UnprotectData(byte[] cipherBytes)
    {
        var inBlob = new DATA_BLOB();
        var outBlob = new DATA_BLOB();
        var entropyBlob = new DATA_BLOB();

        var inPtr = Marshal.AllocHGlobal(cipherBytes.Length);
        try
        {
            Marshal.Copy(cipherBytes, 0, inPtr, cipherBytes.Length);
            inBlob.cbData = cipherBytes.Length;
            inBlob.pbData = inPtr;

            var success = CryptUnprotectData(
                ref inBlob,
                IntPtr.Zero,
                ref entropyBlob,
                IntPtr.Zero,
                IntPtr.Zero,
                CRYPTPROTECT_UI_FORBIDDEN,
                ref outBlob);

            if (!success)
            {
                var err = Marshal.GetLastWin32Error();
                throw new Win32Exception(err, $"CryptUnprotectData failed with error code {err}.");
            }

            var plainBytes = new byte[outBlob.cbData];
            Marshal.Copy(outBlob.pbData, plainBytes, 0, outBlob.cbData);
            return plainBytes;
        }
        finally
        {
            Marshal.FreeHGlobal(inPtr);
            if (outBlob.pbData != IntPtr.Zero)
            {
                LocalFree(outBlob.pbData);
            }
        }
    }

    #endregion
}

/// <summary>
/// Windows-specific device information provider.
/// </summary>
public class WindowsDeviceInfoProvider : IDeviceInfoProvider
{
    public string GetMachineName()
    {
        return Environment.MachineName;
    }

    public string GetPlatform()
    {
        return "Windows";
    }

    public string? GetMacAddress()
    {
        try
        {
            var nic = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n =>
                    n.OperationalStatus == OperationalStatus.Up &&
                    n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            return nic?.GetPhysicalAddress().ToString();
        }
        catch
        {
            return null;
        }
    }
}
