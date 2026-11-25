using System.IO;

namespace MacE.Helpers;

/// <summary>
/// Centralized path resolver for MAC-E.
/// Ensures that DeviceService, KeyStorage, and Workspace always share the exact same
/// persistent storage locations across application builds and updates.
/// </summary>
public static class AppPaths
{
    private static string? _baseDirectory;

    public static string BaseDirectory
    {
        get
        {
            if (_baseDirectory != null) return _baseDirectory;

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                _baseDirectory = Path.Combine(FileSystem.AppDataDirectory, "MacE");
            }
            else
            {
                // Consistent Windows path: %LOCALAPPDATA%\MacE
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                _baseDirectory = Path.Combine(localAppData, "MacE");
            }

            Directory.CreateDirectory(_baseDirectory);
            return _baseDirectory;
        }
    }

    public static string KeysDirectory
    {
        get
        {
            var dir = Path.Combine(BaseDirectory, "Keys");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string WorkspaceDirectory
    {
        get
        {
            var dir = Path.Combine(BaseDirectory, "Workspace");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string RegistryFilePath => Path.Combine(BaseDirectory, "devices.json");
}
