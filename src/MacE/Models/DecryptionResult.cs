namespace MacE.Models;

/// <summary>
/// Result of a decryption attempt.
/// Communicates success/failure and the path to the temporary decrypted file.
/// </summary>
public class DecryptionResult
{
    /// <summary>
    /// Whether decryption succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// If decryption succeeded, the path to the temporary decrypted file.
    /// This file should be opened in its associated application and cleaned up afterward.
    /// </summary>
    public string? TempFilePath { get; set; }

    /// <summary>
    /// Original file name from the .mace metadata (e.g., "report.docx").
    /// </summary>
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// If decryption failed, a user-friendly explanation of why.
    /// Examples: "This device is not authorized", "File is corrupted or tampered with".
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The original .mace file path — used to overwrite it after re-encryption.
    /// </summary>
    public string? SourceMaceFilePath { get; set; }

    /// <summary>
    /// The list of devices that were authorized in the original .mace file.
    /// Used to re-encrypt with the same access list when saving back.
    /// </summary>
    public List<AuthorizedDeviceEntry> AuthorizedDevices { get; set; } = new();

    /// <summary>
    /// Create a successful result.
    /// </summary>
    public static DecryptionResult Succeeded(string tempFilePath, string originalFileName, string sourceMaceFilePath, List<AuthorizedDeviceEntry> authorizedDevices)
        => new() { Success = true, TempFilePath = tempFilePath, OriginalFileName = originalFileName, SourceMaceFilePath = sourceMaceFilePath, AuthorizedDevices = authorizedDevices };

    /// <summary>
    /// Create a failed result.
    /// </summary>
    public static DecryptionResult Failed(string errorMessage)
        => new() { Success = false, ErrorMessage = errorMessage };
}
