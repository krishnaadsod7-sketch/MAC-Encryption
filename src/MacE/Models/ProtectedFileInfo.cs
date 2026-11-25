namespace MacE.Models;

/// <summary>
/// Information about a .mace file managed by the application.
/// Used in the Protected Files page to display a list of known encrypted documents.
/// </summary>
public class ProtectedFileInfo
{
    /// <summary>
    /// Full path to the .mace file on disk.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Original file name before encryption (from .mace metadata).
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// Original file extension (e.g., ".docx").
    /// </summary>
    public string OriginalExtension { get; set; } = string.Empty;

    /// <summary>
    /// When the .mace file was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Number of devices authorized to access this file.
    /// </summary>
    public int AuthorizedDeviceCount { get; set; }

    /// <summary>
    /// Whether the current device is authorized to decrypt this file.
    /// </summary>
    public bool IsAccessibleByCurrentDevice { get; set; }
}
