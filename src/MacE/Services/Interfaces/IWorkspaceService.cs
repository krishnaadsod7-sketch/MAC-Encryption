namespace MacE.Services.Interfaces;

/// <summary>
/// Manages the temporary workspace where decrypted files are placed.
/// 
/// SECURITY LIMITATIONS (documented honestly):
/// - The temporary directory uses standard OS file permissions.
/// - A privileged user or admin can still access the temp files.
/// - File deletion does not guarantee forensic erasure (SSD wear-leveling,
///   filesystem journaling, OS page files, etc.).
/// - External applications opened by the user may create their own copies
///   (autosave, recent files, clipboard, etc.).
/// - This service minimizes plaintext exposure; it does not eliminate it.
/// </summary>
public interface IWorkspaceService
{
    /// <summary>
    /// Create a temporary file with the given contents and extension.
    /// Returns the full path to the temp file.
    /// </summary>
    Task<string> CreateTempFileAsync(byte[] contents, string extension);

    /// <summary>
    /// Open a file in its associated application (e.g., .docx → Word).
    /// </summary>
    Task LaunchFileAsync(string filePath);

    /// <summary>
    /// Delete a specific temporary file. Overwrites with zeros before deleting
    /// (best-effort; not a forensic guarantee).
    /// </summary>
    Task CleanupFileAsync(string filePath);

    /// <summary>
    /// Delete all files in the workspace directory.
    /// Called at app startup to remove orphaned temp files from previous sessions.
    /// </summary>
    Task CleanupAllAsync();

    /// <summary>
    /// Check if a temporary file has been modified since it was created.
    /// Used to detect edits that should be re-encrypted.
    /// </summary>
    Task<bool> HasFileBeenModifiedAsync(string filePath, DateTime createdAtUtc);
}
