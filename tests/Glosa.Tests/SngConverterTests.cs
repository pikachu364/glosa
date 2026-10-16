using System.Buffers.Binary;
using System.Text;
using Glosa.Core.Smf;

namespace Glosa.Tests;

public class SngConverterTests
{
    [Fact]
    public void RecognizesBalladeSignature()
    {
        byte[] data = new byte[32];
        Encoding.ASCII.GetBytes("BALLADE SONG").CopyTo(data, 0);
        Assert.True(SngConverter.IsSng(data));
        Assert.False(SngConverter.IsSng("MThd"u8));
    }

    [Fact]
    public void RejectsUnknownBalladeVersion()
    {
        byte[] data = new byte[256];
        Encoding.ASCII.GetBytes("BALLADE SONG").CopyTo(data, 0);
        Encoding.ASCII.GetBytes("9.99").CopyTo(data, 18);
        Assert.Throws<InvalidDataException>(() => SngConverter.ToSmf(data));
    }

    [Fact]
    public void ConvertsMinimalVersionThreeSongAndReaderRecognizesIt()
    {
        byte[] sng = CreateMinimalSong("3.00", 10);
        byte[] smf = SngConverter.ToSmf(sng);
        Assert.True(smf.AsSpan().StartsWith("MThd"u8));

        MidiSequence sequence = SmfReader.Read(sng);
        Assert.Equal(1, sequence.Format);
        Assert.Equal(11, sequence.TrackCount);
        Assert.Contains(sequence.Events, e => e.Kind == MidiEventKind.Channel);
    }

    [Fact]
    public void ConvertsMinimalVersionFourSongWithSixteenParts()
    {
        byte[] sng = CreateMinimalSong("4.00", 16);
        byte[] smf = SngConverter.ToSmf(sng);
        Assert.True(smf.AsSpan().StartsWith("MThd"u8));

        MidiSequence sequence = SmfReader.Read(sng);
        Assert.Equal(1, sequence.Format);
        Assert.Equal(17, sequence.TrackCount);
        Assert.Contains(sequence.Events, e => e.Kind == MidiEventKind.Channel);
    }

    private static byte[] CreateMinimalSong(string version, int parts)
    {
        const int table = 0x46;
        int tableEnd = table + 4 * (parts + 1);
        int firstBlock = tableEnd + 94;
        const int conductorSize = 100;
        const int partEntrySize = 100;
        int partBlockSize = partEntrySize + 81;
        byte[] data = new byte[firstBlock + conductorSize + parts * partBlockSize];

        Encoding.ASCII.GetBytes("BALLADE SONG").CopyTo(data, 0);
        Encoding.ASCII.GetBytes(version).CopyTo(data, 18);
        for (int i = 0; i <= parts; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(table + 4 * i),
                (uint)(i == 0 ? conductorSize : partEntrySize));

        int conductor = firstBlock;
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(conductor + 77), 192);
        data[conductor + 79] = 4;
        data[conductor + 80] = 4;

        for (int part = 1; part <= parts; part++)
        {
            int start = firstBlock + conductorSize + (part - 1) * partBlockSize;
            Encoding.ASCII.GetBytes(part == 1 ? "MELO" : $"PART {part}").CopyTo(data, start + 81);
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(start + 112), 16);
            data[start + 158] = 60;
            data[start + 159] = 24;
            data[start + 160] = 24;
            data[start + 162] = 100;
            data[start + 166] = 0xFC;
        }
        return data;
    }
}
