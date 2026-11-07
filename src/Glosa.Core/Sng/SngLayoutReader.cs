using System.Buffers.Binary;
using System.Text;

namespace Glosa.Core.Sng;

/// <summary>Validated byte ranges for the conductor and each Ballade part.</summary>
/// <remarks>
/// The offsets are relative to the original SNG file. V1 has a distinct part header;
/// V2–V4 share the later part header, with V4 carrying sixteen parts instead of ten.
/// </remarks>
internal sealed record SngLayout(
    SngVersion Version,
    int PartCount,
    int[] Starts,
    int[] Sizes,
    int ConductorStart,
    int ConductorSize)
{
    public bool IsVersion1 => Version == SngVersion.V1;
}

/// <summary>Finds and validates the version-specific Ballade SNG block layout.</summary>
internal static class SngLayoutReader
{
    private const int PartTableOffset = 0x46;

    public static SngLayout Read(ReadOnlySpan<byte> data)
    {
        SngHeader header = SngHeader.Read(data);
        if (!TryRead(data, header.Version, out SngLayout? layout))
            throw new InvalidDataException("Invalid or unsupported Ballade SNG block layout.");
        return layout;
    }

    private static bool TryRead(ReadOnlySpan<byte> data, SngVersion version,
                                out SngLayout? layout)
    {
        layout = null;
        int parts = version == SngVersion.V4 ? 16 : 10;
        int tableEnd = PartTableOffset + 4 * (parts + 1);
        if (data.Length < tableEnd + 8) return false;

        var entries = new int[parts + 1];
        for (int i = 0; i <= parts; i++)
        {
            uint size = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(PartTableOffset + 4 * i, 4));
            if (size > int.MaxValue) return false;
            entries[i] = (int)size;
        }

        if (version == SngVersion.V1)
        {
            int headerEnd = tableEnd + 2;
            var starts = new int[parts + 1];
            var sizes = new int[parts + 1];
            starts[0] = headerEnd;
            sizes[0] = entries[0];
            long offset = (long)headerEnd + entries[0];

            for (int part = 1; part <= parts; part++)
            {
                long size = (long)entries[part] + 80;
                if (offset < 0 || size < 145 || size > int.MaxValue ||
                    offset + size + 65 > data.Length)
                    return false;

                int start = (int)offset;
                int used = U16(data, start + 115);
                int controllers = U16(data, start + 117);
                if (used % 8 != 0 || controllers % 4 != 0 ||
                    used > size - 145 || controllers > size - 145 - used)
                    return false;

                int seekDistance = 0;
                for (int i = 0; i < 13; i++)
                    seekDistance += U16(data, start + 119 + 2 * i);
                if ((long)used + controllers + seekDistance != entries[part])
                    return false;

                starts[part] = start;
                sizes[part] = (int)size;
                offset += size;
            }

            layout = new SngLayout(version, parts, starts, sizes, starts[0], sizes[0]);
            return true;
        }

        // Historical later-version files occur with each of these conductor-header
        // lengths. Accept a candidate only when every part block validates completely.
        foreach (int conductorStart in new[] { tableEnd + 94, tableEnd + 2, tableEnd })
        {
            if (conductorStart <= 0 || conductorStart >= data.Length) continue;
            var starts = new int[parts + 1];
            var sizes = new int[parts + 1];
            long offset = conductorStart;
            starts[0] = conductorStart;
            sizes[0] = entries[0];
            offset += entries[0];

            bool valid = true;
            for (int part = 1; part <= parts; part++)
            {
                long size = (long)entries[part] + 81;
                if (offset < 0 || offset + 158 > data.Length ||
                    size < 158 || size > int.MaxValue)
                {
                    valid = false;
                    break;
                }

                int start = (int)offset;
                int used = U16(data, start + 112);
                int controllers = U16(data, start + 114);
                int indexBytes = U16(data, start + 116);
                long capacity = size - 158;
                if (used % 8 != 0 || controllers % 4 != 0 ||
                    (long)used + controllers + indexBytes > capacity)
                {
                    valid = false;
                    break;
                }

                starts[part] = start;
                sizes[part] = (int)size;
                offset += size;
            }

            if (valid)
            {
                layout = new SngLayout(version, parts, starts, sizes,
                                       conductorStart, entries[0]);
                return true;
            }
        }

        return false;
    }

    private static int U16(ReadOnlySpan<byte> data, int offset)
        => BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
}
