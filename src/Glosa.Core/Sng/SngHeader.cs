using System.Text;

namespace Glosa.Core.Sng;

/// <summary>The Ballade SNG file generation.</summary>
public enum SngVersion
{
    V1 = 1,
    V2 = 2,
    V3 = 3,
    V4 = 4,
}

/// <summary>
/// The version-bearing part of a Ballade SNG header.
/// </summary>
/// <remarks>
/// The signature starts at byte zero and the four-byte version field starts at byte 18.
/// This type deliberately does not infer part/conductor offsets: those layouts are
/// version-dependent and are parsed by the converter after their bounds are established.
/// </remarks>
public readonly record struct SngHeader(SngVersion Version)
{
    private static ReadOnlySpan<byte> Signature => "BALLADE SONG"u8;

    /// <summary>Whether the data begins with the Ballade SNG signature.</summary>
    public static bool IsSng(ReadOnlySpan<byte> data)
        => data.Length >= 32 && data.StartsWith(Signature);

    /// <summary>Reads the signature and supported version, or throws for invalid data.</summary>
    public static SngHeader Read(ReadOnlySpan<byte> data)
    {
        if (!IsSng(data))
            throw new InvalidDataException("Not a Ballade SNG file.");

        return Encoding.ASCII.GetString(data.Slice(18, 4)) switch
        {
            "1.00" => new SngHeader(SngVersion.V1),
            "2.00" => new SngHeader(SngVersion.V2),
            "3.00" => new SngHeader(SngVersion.V3),
            "4.00" => new SngHeader(SngVersion.V4),
            var version => throw new InvalidDataException(
                $"Unsupported Ballade SNG version: {version}."),
        };
    }
}
