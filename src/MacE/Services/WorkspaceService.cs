using MacE.Helpers;
using MacE.Services.Interfaces;
using System.Diagnostics;
using System.IO;

namespace MacE.Services;

/// <summary>
/// Manages the protected workspace where decrypted files are temporarily placed.
/// Uses centralized AppPaths.WorkspaceDirectory.
/// </summary>
public class WorkspaceService : IWorkspaceService
{
    private readonly string _workspaceDirectory;

    public WorkspaceService()
    {
        _workspaceDirectory = AppPaths.WorkspaceDirectory;
        Directory.CreateDirectory(_workspaceDirectory);
    }

    public async Task<string> CreateTempFileAsync(byte[] contents, string extension)
    {
        var safeExt = extension.StartsWith(".") ? extension : $".{extension}";
        var fileName = $"mace_view_{Guid.NewGuid():N}{safeExt}";
        var filePath = Path.Combine(_workspaceDirectory, fileName);

        await File.WriteAllBytesAsync(filePath, contents);
        return filePath;
    }

    public async Task LaunchFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Cannot launch non-existent file '{filePath}'.");

        try
        {
            var opened = await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                Title = "Open Protected Document",
                File = new ReadOnlyFile(filePath)
            });

            if (!opened)
            {
                TryProcessStart(filePath);
            }
        }
        catch
        {
            TryProcessStart(filePath);
        }
    }

    private static void TryProcessStart(string filePath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Unable to open file in external application: {ex.Message}", ex);
        }
    }

    public Task CleanupFileAsync(string filePath)
    {
        if (File.Exists(filePath))
        {
            try
            {
                var length = new FileInfo(filePath).Length;
                File.WriteAllBytes(filePath, new byte[length]);
                File.Delete(filePath);
            }
            catch
            {
                try { File.Delete(filePath); } catch { }
            }
        }
        return Task.CompletedTask;
    }

    public Task CleanupAllAsync()
    {
        if (Directory.Exists(_workspaceDirectory))
        {
            foreach (var file in Directory.GetFiles(_workspaceDirectory))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                }
            }
        }
        return Task.CompletedTask;
    }

    public Task<bool> HasFileBeenModifiedAsync(string filePath, DateTime createdAtUtc)
    {
        if (!File.Exists(filePath))
            return Task.FromResult(false);

        var lastWrite = File.GetLastWriteTimeUtc(filePath);
        return Task.FromResult(lastWrite > createdAtUtc.AddSeconds(1));
    }
}
