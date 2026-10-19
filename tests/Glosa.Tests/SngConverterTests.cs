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
    public void RejectsTruncatedPartBlockTable()
    {
        byte[] data = new byte[256];
        Encoding.ASCII.GetBytes("BALLADE SONG").CopyTo(data, 0);
        Encoding.ASCII.GetBytes("3.00").CopyTo(data, 18);

        // Declare non-empty conductor and part blocks, but provide no room for
        // the complete part headers or their event streams.
        int table = 0x46;
        for (int i = 0; i <= 10; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(table + 4 * i), 100);

        Assert.Throws<InvalidDataException>(() => SngConverter.ToSmf(data));
    }

    [Fact]
    public void RejectsMalformedPartEventAndControllerLengths()
    {
        const int table = 0x46;
        const int parts = 10;
        int tableEnd = table + 4 * (parts + 1);
        int[] candidateFirstParts =
        [
            tableEnd + 94 + 120,
            tableEnd + 2 + 120,
            tableEnd + 120,
        ];

        // The parser supports several historical header layouts. Corrupt the
        // first part header under each candidate so it cannot accept a fallback
        // interpretation of the same malformed file.
        foreach ((int offset, ushort value) in new[]
        {
            (112, (ushort)15), // event length is not a multiple of 8
            (114, (ushort)3),  // controller length is not a multiple of 4
            (112, (ushort)256), // event stream exceeds the available block
        })
        {
            byte[] sng = CreateMinimalSong("3.00", parts);
            foreach (int firstPart in candidateFirstParts)
                BinaryPrimitives.WriteUInt16LittleEndian(sng.AsSpan(firstPart + offset), value);
            Assert.Throws<InvalidDataException>(() => SngConverter.ToSmf(sng));
        }
    }

    [Fact]
    public void ConvertsMinimalVersionThreeSongAndReaderRecognizesIt()
    {
        byte[] sng = CreateMinimalSong("3.00", 10);
        byte[] smf = SngConverter.ToSmf(sng);
        Assert.True(smf.AsSpan().StartsWith("MThd"u8));

        // Parse the generated SMF, not the SNG source, so this verifies the
        // converter's output is structurally readable by the project's reader.
        MidiSequence sequence = SmfReader.Read(smf);
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

        // Parse the generated SMF, not the SNG source, so this verifies the
        // converter's output is structurally readable by the project's reader.
        MidiSequence sequence = SmfReader.Read(smf);
        Assert.Equal(1, sequence.Format);
        Assert.Equal(17, sequence.TrackCount);
        Assert.Contains(sequence.Events, e => e.Kind == MidiEventKind.Channel);
    }

    [Fact]
    public void TieRecordExtendsMatchingContiguousNoteWithoutStartingANewNote()
    {
        byte[] sng = CreateMinimalSong("3.00", 10);
        int firstPart = 0x46 + 4 * 11 + 94 + 120;
        WriteNoteRecord(sng, firstPart, 0, 60, 24, 24, 100);
        WriteNoteRecord(sng, firstPart, 1, 60, 24, 24, 100, 0x80);
        sng[firstPart + 174] = 0xFC;
        BinaryPrimitives.WriteUInt16LittleEndian(sng.AsSpan(firstPart + 112), 24);

        MidiSequence sequence = SmfReader.Read(SngConverter.ToSmf(sng));
        var notes = NoteEvents(sequence, 60);

        Assert.Single(notes.Where(e => e.Data2 == 100));
        Assert.Single(notes.Where(e => e.Data2 == 0));
        Assert.Equal(192L, notes.Single(e => e.Data2 == 100).Ticks);
        Assert.Equal(240L, notes.Single(e => e.Data2 == 0).Ticks);
    }

    [Fact]
    public void TieRecordCanExtendAcrossASmallGateGap()
    {
        byte[] sng = CreateMinimalSong("3.00", 10);
        int firstPart = 0x46 + 4 * 11 + 94 + 120;
        WriteNoteRecord(sng, firstPart, 0, 60, 12, 24, 100);
        WriteNoteRecord(sng, firstPart, 1, 60, 24, 24, 100, 0x80);
        sng[firstPart + 174] = 0xFC;
        BinaryPrimitives.WriteUInt16LittleEndian(sng.AsSpan(firstPart + 112), 24);

        MidiSequence sequence = SmfReader.Read(SngConverter.ToSmf(sng));
        var notes = NoteEvents(sequence, 60);

        Assert.Single(notes.Where(e => e.Data2 == 100));
        Assert.Single(notes.Where(e => e.Data2 == 0));
        Assert.Equal(240L, notes.Single(e => e.Data2 == 0).Ticks);
    }

    [Fact]
    public void TieRecordExtendsMatchingPitchAcrossInterleavedNotes()
    {
        byte[] sng = CreateMinimalSong("3.00", 10);
        int firstPart = 0x46 + 4 * 11 + 94 + 120;
        WriteNoteRecord(sng, firstPart, 0, 60, 24, 0, 100);
        WriteNoteRecord(sng, firstPart, 1, 64, 24, 24, 90);
        WriteNoteRecord(sng, firstPart, 2, 60, 24, 24, 100, 0x80);
        sng[firstPart + 182] = 0xFC;
        BinaryPrimitives.WriteUInt16LittleEndian(sng.AsSpan(firstPart + 112), 32);

        MidiSequence sequence = SmfReader.Read(SngConverter.ToSmf(sng));
        var c = NoteEvents(sequence, 60);
        var e = NoteEvents(sequence, 64);

        Assert.Equal(240L, c.Single(x => x.Data2 == 0).Ticks);
        Assert.Equal(216L, e.Single(x => x.Data2 == 0).Ticks);
    }

    [Fact]
    public void TieRecordWithoutPriorSamePitchDoesNotCreateANewNote()
    {
        byte[] sng = CreateMinimalSong("3.00", 10);
        int firstPart = 0x46 + 4 * 11 + 94 + 120;
        WriteNoteRecord(sng, firstPart, 0, 60, 12, 24, 100);
        WriteNoteRecord(sng, firstPart, 1, 61, 24, 24, 100, 0x80);
        sng[firstPart + 174] = 0xFC;
        BinaryPrimitives.WriteUInt16LittleEndian(sng.AsSpan(firstPart + 112), 24);

        MidiSequence sequence = SmfReader.Read(SngConverter.ToSmf(sng));
        Assert.Single(NoteEvents(sequence, 60).Where(e => e.Data2 == 100));
        Assert.Single(NoteEvents(sequence, 60).Where(e => e.Data2 == 0));
        Assert.Empty(NoteEvents(sequence, 61));
    }

    [Fact]
    public void BalladeV2MatchesSng2GsReferenceNoteEvents()
        => AssertReferenceNoteEvents(
            "v2/SHIZUME0.SNG",
            "v2/sng2gs/SHIZUME0.MID",
            referenceTickShift: 160);

    [Fact]
    public void BalladeV3MatchesSng2GsReferenceNoteEvents()
        => AssertReferenceNoteEvents(
            "v3/START__0.SNG",
            "v3/sng2gs/START__0.MID",
            referenceTickShift: 0);

    [Fact]
    public void BalladeV4MatchesSng2GsReferenceNoteEvents()
        => AssertReferenceNoteEvents(
            "v4/BOURREE0.SNG",
            "v4/sng2gs/BOURREE0.MID",
            referenceTickShift: 0);

    private static void AssertReferenceNoteEvents(
        string sngRelativePath, string midiRelativePath, int referenceTickShift)
    {
        string root = FindRepositoryRoot();
        byte[] sng = File.ReadAllBytes(Path.Combine(root, "testdata", "ballade", sngRelativePath));
        byte[] reference = File.ReadAllBytes(Path.Combine(root, "testdata", "ballade", midiRelativePath));

        MidiSequence actual = SmfReader.Read(SngConverter.ToSmf(sng));
        MidiSequence expected = SmfReader.Read(reference);

        static string[] NoteEvents(MidiSequence sequence, int tickShift)
            => sequence.Events
                .Where(e => e.Kind == MidiEventKind.Channel &&
                    (((e.Status & 0xF0) == 0x90) || ((e.Status & 0xF0) == 0x80)))
                .Select(e =>
                {
                    bool noteOn = (e.Status & 0xF0) == 0x90 && e.Data2 != 0;
                    bool noteOff = (e.Status & 0xF0) == 0x80 ||
                        ((e.Status & 0xF0) == 0x90 && e.Data2 == 0);
                    if (!noteOn && !noteOff) return null;
                    int velocity = noteOn ? e.Data2 : 0;
                    return $"{e.Ticks + tickShift}:{(noteOn ? 1 : 0)}:{e.Data1}:{velocity}";
                })
                .Where(e => e is not null)
                .Select(e => e!)
                .OrderBy(e => e, StringComparer.Ordinal)
                .ToArray();

        Assert.Equal(NoteEvents(expected, referenceTickShift), NoteEvents(actual, 0));

        // Preserve each MIDI track's note-event grouping and channel assignment while
        // ignoring physical track numbers, which differ with SNG2GS track-order options.
        // This catches cross-track leakage and channel mismatches hidden by aggregation.
        static string[] TrackNoteEvents(MidiSequence sequence, int tickShift)
            => sequence.Events
                .Where(e => e.Kind == MidiEventKind.Channel &&
                    (((e.Status & 0xF0) == 0x90) || ((e.Status & 0xF0) == 0x80)))
                .Select(e =>
                {
                    bool noteOn = (e.Status & 0xF0) == 0x90 && e.Data2 != 0;
                    bool noteOff = (e.Status & 0xF0) == 0x80 ||
                        ((e.Status & 0xF0) == 0x90 && e.Data2 == 0);
                    if (!noteOn && !noteOff) return (Track: e.Track, Event: (string?)null);
                    int velocity = noteOn ? e.Data2 : 0;
                    return (Track: e.Track,
                        Event: (string?)$"{e.Ticks + tickShift}:{(noteOn ? 1 : 0)}:{e.Data1}:{velocity}:{e.Status & 0x0F}");
                })
                .Where(x => x.Event is not null)
                .GroupBy(x => x.Track)
                .Select(group => string.Join("|", group
                    .Select(x => x.Event!)
                    .OrderBy(x => x, StringComparer.Ordinal)))
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();

        Assert.Equal(TrackNoteEvents(expected, referenceTickShift), TrackNoteEvents(actual, 0));
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "testdata", "ballade")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find the repository root containing testdata/ballade.");
    }

    private static MidiEvent[] NoteEvents(MidiSequence sequence, byte key)
        => sequence.Events
            .Where(e => e.Track == 1 && e.Kind == MidiEventKind.Channel &&
                        e.Status == 0x90 && e.Data1 == key)
            .ToArray();

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

    [Fact]
    public void IgnoresMalformedTempoRecordsAndUsesDefaultTempo()
    {
        foreach ((ushort step, ushort value) in new[]
        {
            ((ushort)0, (ushort)501),  // tempo slider value is out of range
            ((ushort)4001, (ushort)61), // step is out of range
        })
        {
            byte[] sng = CreateMinimalSong("3.00", 10);
            int conductor = 0x46 + 4 * 11 + 94;
            new byte[] { 0xFF, 0x7F, 0xFF, 0xFF, 0xFF, 0xFF }
                .CopyTo(sng, conductor + 84);
            BinaryPrimitives.WriteUInt16LittleEndian(sng.AsSpan(conductor + 96), step);
            BinaryPrimitives.WriteUInt16LittleEndian(sng.AsSpan(conductor + 98), value);

            byte[] smf = SngConverter.ToSmf(sng);

            // Invalid first records terminate tempo parsing, leaving the
            // converter's documented 120 BPM default in place.
            Assert.True(ContainsSequence(smf, TempoEvent(500_000)));
            Assert.False(ContainsSequence(smf, TempoEvent(60_000_000 / 114)));
        }
    }

    private static void WriteNoteRecord(
        byte[] sng, int partStart, int recordIndex, byte key, byte duration,
        byte step, byte velocity, byte flags = 0)
    {
        int offset = partStart + 158 + recordIndex * 8;
        sng[offset] = key;
        sng[offset + 1] = duration;
        sng[offset + 2] = step;
        sng[offset + 4] = velocity;
        sng[offset + 7] = flags;
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
        const int partEntrySize = 110;
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
