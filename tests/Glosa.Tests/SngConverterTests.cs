using System.Buffers.Binary;
using System.Text;
using Glosa.Core.Smf;
using Glosa.Core.Sng;

namespace Glosa.Tests;

public sealed class SngConverterTests
{
    [Theory]
    [InlineData("3.00", 10, 0x79, 92)]
    [InlineData("4.00", 16, 0x91, 116)]
    public void UsesVersionSpecificBaseTempoForLaterVersionCurves(
        string version, int parts, int baseTempoOffset, int initialTempoTick)
    {
        byte[] sng = CreateMinimalSong(version, parts);
        sng[baseTempoOffset] = 60;

        int conductor = 0x46 + 4 * (parts + 1) + 94;
        new byte[] { 0xFF, 0x7F, 0xFF, 0xFF, 0xFF, 0xFF }
            .CopyTo(sng, conductor + 84);

        // baseTempo 60 * slider 89 / 64 = timerCount 83.
        // SNG2S's coarse tempo is floor(60000 / 83) ms.
        BinaryPrimitives.WriteUInt16LittleEndian(sng.AsSpan(conductor + 96), 0);
        BinaryPrimitives.WriteUInt16LittleEndian(sng.AsSpan(conductor + 98), 89);
        // The next record changes the curve after the first bar.
        BinaryPrimitives.WriteUInt16LittleEndian(sng.AsSpan(conductor + 100), 192);
        BinaryPrimitives.WriteUInt16LittleEndian(sng.AsSpan(conductor + 102), 36);

        MidiSequence sequence = SmfReader.Read(SngConverter.ToSmf(sng));
        var tempos = ReadTempos(sequence);

        Assert.Contains((Ticks: (long)initialTempoTick, Microseconds: 722_000), tempos);
        Assert.Contains((Ticks: 192L, Microseconds: 722_891), tempos);
        Assert.Contains((Ticks: 383L, Microseconds: 1_333_000), tempos);
    }

    private static (long Ticks, int Microseconds)[] ReadTempos(MidiSequence sequence)
        => sequence.Events
            .Where(e => e.Kind == MidiEventKind.Meta && e.MetaType == MetaType.Tempo)
            .Select(e =>
            {
                ReadOnlySpan<byte> value = sequence.GetData(e);
                return (e.Ticks, (value[0] << 16) | (value[1] << 8) | value[2]);
            })
            .ToArray();

    private static byte[] CreateMinimalSong(string version, int parts)
    {
        const int table = 0x46;
        int tableEnd = table + 4 * (parts + 1);
        int firstBlock = tableEnd + 94;
        const int conductorSize = 120;
        const int partEntrySize = 110;
        const int partBlockSize = partEntrySize + 81;
        byte[] data = new byte[firstBlock + conductorSize + parts * partBlockSize];

        Encoding.ASCII.GetBytes("BALLADE SONG").CopyTo(data, 0);
        Encoding.ASCII.GetBytes(version).CopyTo(data, 18);
        for (int i = 0; i <= parts; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(
                data.AsSpan(table + 4 * i),
                (uint)(i == 0 ? conductorSize : partEntrySize));

        int conductor = firstBlock;
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(conductor + 77), 192);
        data[conductor + 79] = 4;
        data[conductor + 80] = 4;

        for (int part = 1; part <= parts; part++)
        {
            int start = firstBlock + conductorSize + (part - 1) * partBlockSize;
            data[start + 0x11] = 4;
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(start + 112), 16);
            data[start + 158] = 0xFC;
        }

        return data;
    }
}
