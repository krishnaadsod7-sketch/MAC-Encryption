using MacE.Models;
using MacE.Services.Interfaces;
using System.IO;
using System.Text;
using System.Text.Json;

namespace MacE.Services;

/// <summary>
/// Reads and writes the .mace binary file format.
/// 
/// Format specification:
///   [8 bytes] Magic ASCII: "MACE_ENC"
///   [2 bytes] uint16 little-endian: format version (1)
///   [4 bytes] uint32 little-endian: header length in bytes
///   [N bytes] JSON metadata (UTF-8)
///   [rest]    Encrypted payload (AES-256-GCM ciphertext)
/// </summary>
public class MaceFileService : IMaceFileService
{
    private static readonly byte[] MagicBytes = Encoding.ASCII.GetBytes("MACE_ENC");
    private const ushort CurrentVersion = 1;

    public async Task WriteAsync(string outputPath, MaceFileMetadata metadata, byte[] ciphertext)
    {
        var jsonOptions = new JsonSerializerOptions { WriteIndented = false };
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(metadata, jsonOptions);

        using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        using var writer = new BinaryWriter(fileStream, Encoding.UTF8, leaveOpen: true);

        // 1. Magic bytes (8 bytes)
        writer.Write(MagicBytes);

        // 2. Format version (2 bytes)
        writer.Write(CurrentVersion);

        // 3. Header length (4 bytes)
        writer.Write((uint)jsonBytes.Length);

        // 4. JSON metadata
        writer.Write(jsonBytes);
        writer.Flush();

        // 5. Ciphertext payload
        await fileStream.WriteAsync(ciphertext.AsMemory());
        await fileStream.FlushAsync();
    }

    public async Task<MaceFileMetadata> ReadMetadataAsync(string maceFilePath)
    {
        if (!File.Exists(maceFilePath))
            throw new FileNotFoundException($"The file '{maceFilePath}' does not exist.");

        using var stream = new FileStream(maceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        if (stream.Length < 14) // 8 + 2 + 4 minimum header prefix
            throw new InvalidDataException("The file is too small to be a valid .mace file.");

        var magic = reader.ReadBytes(8);
        if (!magic.SequenceEqual(MagicBytes))
            throw new InvalidDataException("Invalid .mace file: unrecognized magic header.");

        var version = reader.ReadUInt16();
        if (version > CurrentVersion)
            throw new NotSupportedException($"Unsupported .mace file version {version}. Highest supported version is {CurrentVersion}.");

        var headerLength = reader.ReadUInt32();
        if (stream.Length < 14 + headerLength)
            throw new InvalidDataException("Corrupted .mace file: truncated metadata header.");

        var jsonBytes = reader.ReadBytes((int)headerLength);
        var metadata = JsonSerializer.Deserialize<MaceFileMetadata>(jsonBytes);

        if (metadata == null)
            throw new InvalidDataException("Failed to parse .mace metadata.");

        return metadata;
    }

    public async Task<byte[]> ReadCiphertextAsync(string maceFilePath)
    {
        if (!File.Exists(maceFilePath))
            throw new FileNotFoundException($"The file '{maceFilePath}' does not exist.");

        using var stream = new FileStream(maceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        if (stream.Length < 14)
            throw new InvalidDataException("Invalid .mace file.");

        var magic = reader.ReadBytes(8);
        if (!magic.SequenceEqual(MagicBytes))
            throw new InvalidDataException("Invalid .mace header.");

        _ = reader.ReadUInt16(); // version
        var headerLength = reader.ReadUInt32();

        var payloadOffset = 14 + headerLength;
        if (stream.Length < payloadOffset)
            throw new InvalidDataException("Corrupted .mace file.");

        stream.Seek(payloadOffset, SeekOrigin.Begin);
        var ciphertextLength = stream.Length - payloadOffset;
        var ciphertext = new byte[ciphertextLength];

        int read = 0;
        while (read < ciphertextLength)
        {
            int r = await stream.ReadAsync(ciphertext.AsMemory(read, (int)(ciphertextLength - read)));
            if (r == 0) break;
            read += r;
        }

        return ciphertext;
    }

    public Task<bool> IsValidMaceFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return Task.FromResult(false);

        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length < 14)
                return Task.FromResult(false);

            var magic = new byte[8];
            int read = stream.Read(magic, 0, 8);
            return Task.FromResult(read == 8 && magic.SequenceEqual(MagicBytes));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}
