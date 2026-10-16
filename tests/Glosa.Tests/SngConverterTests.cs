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

    [Fact]
    public void ConvertsTempoSliderValuesAndTempoChanges()
    {
        byte[] sng = CreateMinimalSong("3.00", 10);
        int conductor = 0x46 + 4 * 11 + 94;

        // The conductor heap marker is followed by 6 allocator bytes, then
        // [step, tempo slider value] records. SNG2S's measured curve is
        // BPM = 2v - ceil(v / 8): 61 -> 114 BPM, 72 -> 135 BPM.
        new byte[] { 0xFF, 0x7F, 0xFF, 0xFF, 0xFF, 0xFF }
            .CopyTo(sng, conductor + 84);
        BinaryPrimitives.WriteUInt16LittleEndian(sng.AsSpan(conductor + 96), 48);
        BinaryPrimitives.WriteUInt16LittleEndian(sng.AsSpan(conductor + 98), 61);
        BinaryPrimitives.WriteUInt16LittleEndian(sng.AsSpan(conductor + 100), 0);
        BinaryPrimitives.WriteUInt16LittleEndian(sng.AsSpan(conductor + 102), 72);

        byte[] smf = SngConverter.ToSmf(sng);
        int firstTempoUs = 60_000_000 / 114;
        int secondTempoUs = 60_000_000 / 135;

        Assert.True(ContainsSequence(smf, TempoEvent(firstTempoUs)));
        Assert.True(ContainsSequence(smf, TempoEvent(secondTempoUs)));
        // Each record's step advances the timeline after that record, so the
        // first tempo lasts 48 ticks before the second tempo change (delta 0x30).
        Assert.True(ContainsSequence(smf, [0x30, 0xFF, 0x51, 0x03]));
    }

    private static bool ContainsSequence(byte[] data, byte[] expected)
    {
        for (int i = 0; i <= data.Length - expected.Length; i++)
            if (data.AsSpan(i, expected.Length).SequenceEqual(expected))
                return true;
        return false;
    }

    private static byte[] TempoEvent(int microsecondsPerQuarter)
        => [0xFF, 0x51, 0x03,
            (byte)(microsecondsPerQuarter >> 16),
            (byte)(microsecondsPerQuarter >> 8),
            (byte)microsecondsPerQuarter];

    private static byte[] CreateMinimalSong(string version, int parts)
    {
        const int table = 0x46;
        int tableEnd = table + 4 * (parts + 1);
        int firstBlock = tableEnd + 94;
        const int conductorSize = 120;
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
