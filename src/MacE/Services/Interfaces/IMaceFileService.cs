using MacE.Models;

namespace MacE.Services.Interfaces;

/// <summary>
/// Reads and writes the .mace binary file format.
/// 
/// File layout:
///   [8 bytes]  Magic: "MACE_ENC"
///   [2 bytes]  Format version (uint16, little-endian)
///   [4 bytes]  Header length in bytes (uint32, little-endian)
///   [N bytes]  JSON metadata (UTF-8, length = header length)
///   [rest]     Encrypted payload (raw AES-256-GCM ciphertext)
/// </summary>
public interface IMaceFileService
{
    /// <summary>
    /// Write a .mace file to disk.
    /// </summary>
    /// <param name="outputPath">Where to write the file.</param>
    /// <param name="metadata">The file metadata (will be serialized to JSON).</param>
    /// <param name="ciphertext">The encrypted file contents.</param>
    Task WriteAsync(string outputPath, MaceFileMetadata metadata, byte[] ciphertext);

    /// <summary>
    /// Read the metadata header from a .mace file without reading the entire ciphertext.
    /// </summary>
    Task<MaceFileMetadata> ReadMetadataAsync(string maceFilePath);

    /// <summary>
    /// Read the ciphertext payload from a .mace file.
    /// Call after ReadMetadataAsync to get the encrypted data for decryption.
    /// </summary>
    Task<byte[]> ReadCiphertextAsync(string maceFilePath);

    /// <summary>
    /// Validate that a file is a valid .mace file (correct magic bytes and version).
    /// Does not validate the cryptographic contents.
    /// </summary>
    Task<bool> IsValidMaceFileAsync(string filePath);
}
