namespace MacE.Services.Interfaces;

/// <summary>
/// Platform-specific provider for device information.
/// Returns human-readable metadata about the device.
/// 
/// IMPORTANT: None of these values are used for cryptographic security.
/// They are informational only, displayed in the UI for user convenience.
/// </summary>
public interface IDeviceInfoProvider
{
    /// <summary>
    /// Get the machine/host name (e.g., "DESKTOP-ABC123" on Windows).
    /// </summary>
    string GetMachineName();

    /// <summary>
    /// Get the OS platform identifier (e.g., "Windows", "Android").
    /// </summary>
    string GetPlatform();

    /// <summary>
    /// Get a network MAC address if available. Returns null if unavailable.
    /// This is for display purposes only — it is NOT used for security.
    /// MAC addresses can be spoofed and changed.
    /// </summary>
    string? GetMacAddress();
}
