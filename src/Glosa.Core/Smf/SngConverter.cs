using System.Buffers.Binary;
using System.Text;

namespace Glosa.Core.Smf;

/// <summary>Converts Roland/Boss Ballade SNG song data to Standard MIDI Files.</summary>
/// <remarks>
/// The SNG layout and tempo conversion are based on sngmidi.cpp from jmplayer
/// (MIT License), https://github.com/jihodaya/jmplayer. This implementation is a C# port.
/// </remarks>
public static class SngConverter
{
    private const int TicksPerQuarter = 48;
    private static ReadOnlySpan<byte> Signature => "BALLADE SONG"u8;

    public static bool IsSng(ReadOnlySpan<byte> data)
        => data.Length >= 32 && data.StartsWith(Signature);

    public static byte[] ToSmf(ReadOnlySpan<byte> data)
    {
        if (!IsSng(data)) throw new InvalidDataException("Not a Ballade SNG file.");
        if (!TryLayout(data, out Layout layout)) throw new InvalidDataException("Invalid or unsupported Ballade SNG layout.");

        int barLength = 192, numerator = 4, denominator = 4;
        int conductorStart = layout.Starts[0];
        int conductorEnd = Math.Min(data.Length, conductorStart + layout.Sizes[0]);
        int meterAt = conductorStart + 77;
        if (meterAt + 12 <= conductorEnd)
        {
            int len = U16(data, meterAt);
            int num = data[meterAt + 2], den = data[meterAt + 3];
            if (len is > 0 and <= 4000 && num is > 0 and <= 32 &&
                den is 1 or 2 or 4 or 8 or 16 or 32)
            { barLength = len; numerator = num; denominator = den; }
        }

        var tempi = ReadTempi(data, conductorStart, conductorEnd);
        int settings = 0x46 + 4 * (layout.Parts + 1);
        int transpose = settings + 11 <= data.Length ? (short)U16(data, settings + 9) : 0;
        var tracks = new List<Track>();
        for (int part = 1; part <= layout.Parts; part++)
        {
            int start = layout.Starts[part], end = Math.Min(data.Length, start + layout.Sizes[part]);
            int streamOffset = layout.IsLegacyV1 ? 145 : 158;
            if (start + streamOffset > data.Length) break;
            int used = U16(data, start + (layout.IsLegacyV1 ? 115 : 112));
            int controllerBytes = U16(data, start + (layout.IsLegacyV1 ? 117 : 114));
            int stream = start + streamOffset, controllers = stream + used;
            int channel = part - 1, module = data[start + 94];
            int shift = layout.IsLegacyV1 || module is 2 or 5 ? 0 : transpose;
            int nameOffset = layout.IsLegacyV1 ? start + 69 : start + 81;
            int nameLength = 0;
            while (nameLength < 12 && nameOffset + nameLength < data.Length && data[nameOffset + nameLength] != 0) nameLength++;
            var track = new Track(data.Slice(nameOffset, nameLength).ToArray(), channel);
            int tick = 0;
            int safeUsed = Math.Min(used, Math.Max(0, end - stream));
            if (layout.IsLegacyV1)
            {
                AddLegacyV1Notes(data, stream, safeUsed, channel, shift, track);
            }
            else
            {
                var pendingNoteOffs = new Dictionary<int, List<(int Tick, int EventIndex)>>();
                for (int pos = 0; pos + 8 <= safeUsed; pos += 8)
                {
                    var record = data.Slice(stream + pos, 8);
                    if (record[0] == 0xFC) break;
                    int step = record[2], nudge = (sbyte)record[3];
                    if (record[1] != 0)
                    {
                        int at = Math.Max(0, tick + nudge);
                        int key = Math.Clamp(record[0] + shift, 0, 127);
                        if ((record[7] & 0x80) != 0)
                        {
                            // A Tie record never starts a new note. It extends the latest
                            // same-key Note-off that was scheduled no later than this start.
                            // The SNG scheduler can place the Tie start slightly after the
                            // previous gate end, and other pitches may be interleaved.
                            if (pendingNoteOffs.TryGetValue(key, out var candidates))
                            {
                                int candidate = -1, candidateTick = int.MinValue;
                                for (int i = 0; i < candidates.Count; i++)
                                {
                                    var item = candidates[i];
                                    if (item.Tick <= at &&
                                        (item.Tick > candidateTick ||
                                         (item.Tick == candidateTick &&
                                          (candidate < 0 || item.EventIndex > candidates[candidate].EventIndex))))
                                    {
                                        candidate = i;
                                        candidateTick = item.Tick;
                                    }
                                }

                                if (candidate >= 0)
                                {
                                    int eventIndex = candidates[candidate].EventIndex;
                                    candidates.RemoveAt(candidate);
                                    if (candidates.Count == 0) pendingNoteOffs.Remove(key);
                                    int extendedOffTick = at + record[1];
                                    track.Events[eventIndex] = track.Events[eventIndex] with { Tick = extendedOffTick };
                                    if (!pendingNoteOffs.TryGetValue(key, out var updated))
                                        pendingNoteOffs[key] = updated = [];
                                    updated.Add((extendedOffTick, eventIndex));
                                }
                            }
                        }
                        else
                        {
                            track.Events.Add(new MidiOutEvent(at, 2, [(byte)(0x90 | channel), (byte)key, record[4]]));
                            track.Events.Add(new MidiOutEvent(at + record[1], 1, [(byte)(0x90 | channel), (byte)key, 0]));
                            int eventIndex = track.Events.Count - 1;
                            if (!pendingNoteOffs.TryGetValue(key, out var candidates))
                                pendingNoteOffs[key] = candidates = [];
                            candidates.Add((at + record[1], eventIndex));
                        }
                    }
                    tick += step;
                }
            }

            tick = 0;
            var controllerStates = new Dictionary<int, int>();
            int safeControllers = Math.Min(controllerBytes, Math.Max(0, end - controllers));
            for (int pos = 0; pos + 4 <= safeControllers; pos += 4)
            {
                var record = data.Slice(controllers + pos, 4);
                if (record[0] == 0xFC) break;
                int status = record[0] & 0xF0;
                int data1 = record[2] & 0x7F;
                int data2 = record[3] & 0x7F;
                if (status is 0xB0 or 0xC0 or 0xE0)
                {
                    bool emit = true;
                    int eventTick = tick;
                    if (layout.IsLegacyV1)
                    {
                        // The v1 stream repeats unchanged controller states. SNG2GS's
                        // reference MIDI emits only state changes, so suppress repeats.
                        int stateKey = status == 0xB0 ? (status << 8) | data1 : status;
                        int stateValue = status == 0xC0 ? data1 :
                            status == 0xE0 ? data1 | (data2 << 7) : data2;
                        if (controllerStates.TryGetValue(stateKey, out int previousValue) &&
                            previousValue == stateValue)
                            emit = false;
                        else
                            controllerStates[stateKey] = stateValue;

                        // v1 initial state messages are emitted at tick 8; later events
                        // use accumulated stream time with a 191-tick lead-in. Stored ticks
                        // are adjusted because the SMF writer adds barLength.
                        eventTick = tick == 0 ? 8 - barLength : tick - 1;
                    }

                    if (emit)
                    {
                        if (status is 0xB0 or 0xE0)
                            track.Events.Add(new MidiOutEvent(eventTick, 0,
                                [(byte)(status | channel), (byte)data1, (byte)data2]));
                        else if (status == 0xC0)
                            track.Events.Add(new MidiOutEvent(eventTick, 0,
                                [(byte)(0xC0 | channel), (byte)data1]));
                    }
                }
                tick += record[1];
            }
            tracks.Add(track);
        }
        if (tracks.Count == 0) throw new InvalidDataException("Ballade SNG contains no readable parts.");

        using var output = new MemoryStream();
        output.Write("MThd"u8); WriteBE32(output, 6);
        output.WriteByte(0); output.WriteByte(1);
        WriteBE16(output, checked((ushort)(tracks.Count + 1)));
        WriteBE16(output, TicksPerQuarter);

        using (var body = new MemoryStream())
        {
            int denPow = 0;
            for (int value = denominator; value > 1; value >>= 1) denPow++;
            WriteVar(body, 0); body.Write([0xFF, 0x58, 0x04, (byte)numerator, (byte)denPow, 24, 8]);
            int firstTempo = tempi.Count == 0 ? 500000 : tempi[0].Microseconds;
            WriteVar(body, 0); WriteTempo(body, firstTempo);
            int previous = 0;
            foreach (var tempo in tempi)
            {
                int at = tempo.Tick + barLength;
                if (at <= previous && tempo.Tick == 0) continue;
                WriteVar(body, (uint)Math.Max(0, at - previous));
                WriteTempo(body, tempo.Microseconds);
                previous = at;
            }
            WriteChunk(output, body.ToArray());
        }

        foreach (Track track in tracks)
        {
            // OrderBy/ThenBy are stable, preserving source order for otherwise equal events.
            using var body = new MemoryStream();
            WriteVar(body, 0); body.WriteByte(0xFF); body.WriteByte(0x03);
            WriteVar(body, (uint)track.Name.Length); body.Write(track.Name);
            if (!layout.IsLegacyV1)
            {
                WriteVar(body, 0); body.Write([(byte)(0xB0 | track.Channel), 101, 0]);
                WriteVar(body, 0); body.Write([(byte)(0xB0 | track.Channel), 100, 0]);
                WriteVar(body, 0); body.Write([(byte)(0xB0 | track.Channel), 6, 2]);
            }
            int previous = 0;
            foreach (var e in track.Events.OrderBy(e => e.Tick).ThenBy(e => e.Order))
            {
                int at = Math.Max(0, e.Tick + barLength);
                WriteVar(body, (uint)Math.Max(0, at - previous));
                body.Write(e.Bytes);
                previous = at;
            }
            WriteChunk(output, body.ToArray());
        }
        return output.ToArray();
    }

    private static void AddLegacyV1Notes(
        ReadOnlySpan<byte> data, int stream, int used, int channel, int shift, Track track)
    {
        var records = new List<LegacyNoteRecord>();
        int tick = 0;
        for (int pos = 0; pos + 8 <= used; pos += 8)
        {
            var record = data.Slice(stream + pos, 8);
            if (record[0] == 0xFC) break;
            byte flags = (byte)(record[7] & 0xC0);
            if (record[1] != 0 && flags != 0x80)
                records.Add(new LegacyNoteRecord(
                    Math.Max(0, tick + (sbyte)record[3]), pos,
                    record[0], record[1], record[4], flags));
            tick += record[2];
        }

        records.Sort((a, b) =>
        {
            int byTick = a.Tick.CompareTo(b.Tick);
            return byTick != 0 ? byTick : a.Offset.CompareTo(b.Offset);
        });

        // SNG2S keeps one active timer per channel/pitch. A collision can emit an
        // immediate Note-off; duration comparison decides whether to replace the timer.
        // Flags 0x40 and 0xC0 have distinct registration/output behavior in the old path.
        var timers = new Dictionary<int, int>();
        int eventOrder = 10;
        int index = 0;
        while (index < records.Count || timers.Count != 0)
        {
            int nextRecord = index < records.Count ? records[index].Tick : int.MaxValue;
            int nextExpiry = timers.Count != 0 ? timers.Values.Min() : int.MaxValue;
            int at = Math.Min(nextRecord, nextExpiry);
            if (at == int.MaxValue) break;

            foreach (var timer in timers.Where(item => item.Value <= at).ToArray())
            {
                int outputKey = Math.Clamp(timer.Key + shift, 0, 127);
                track.Events.Add(new MidiOutEvent(timer.Value, eventOrder++,
                    [(byte)(0x90 | channel), (byte)outputKey, 0]));
                timers.Remove(timer.Key);
            }

            while (index < records.Count && records[index].Tick == at)
            {
                var record = records[index++];
                int rawKey = record.Key;
                int outputKey = Math.Clamp(rawKey + shift, 0, 127);
                bool hasTimer = timers.TryGetValue(rawKey, out int expiry);
                bool registerTimer = true;
                if (hasTimer)
                {
                    if (record.Flags != 0xC0)
                        track.Events.Add(new MidiOutEvent(at, eventOrder++,
                            [(byte)(0x90 | channel), (byte)outputKey, 0]));

                    if (expiry - at < record.Duration)
                        timers.Remove(rawKey);
                    else
                        registerTimer = false;
                }

                if (registerTimer && record.Flags != 0x40)
                    timers[rawKey] = at + record.Duration;

                if (record.Flags != 0xC0)
                    track.Events.Add(new MidiOutEvent(at, eventOrder++,
                        [(byte)(0x90 | channel), (byte)outputKey, record.Velocity]));
            }
        }
    }

    private static List<TempoPoint> ReadTempi(ReadOnlySpan<byte> data, int start, int end)
    {
        var result = new List<TempoPoint>();
        ReadOnlySpan<byte> sentinel = [0xFF, 0x7F, 0xFF, 0xFF, 0xFF, 0xFF];
        int last = -1;
        for (int i = start; i + 6 <= end; i++)
            if (data.Slice(i, 6).SequenceEqual(sentinel)) last = i;
        if (last < 0) return result;
        int at = last + 12, tick = 0, previous = -1;
        while (at + 4 <= end)
        {
            int step = U16(data, at), value = U16(data, at + 2);
            if (value <= 0 || value > 500 || step > 4000) break;
            int bpm = Math.Clamp(2 * value - ((value + 7) / 8), 1, 600);
            int us = 60000000 / bpm;
            if (us != previous) { result.Add(new TempoPoint(tick, us)); previous = us; }
            tick += step; at += 4;
        }
        return result;
    }

    private static bool TryLayout(ReadOnlySpan<byte> data, out Layout layout)
    {
        layout = default;
        if (data.Length < 0x100) return false;
        string version = Encoding.ASCII.GetString(data.Slice(18, 4));
        bool legacyV1 = version == "1.00";
        int parts = version switch { "1.00" or "2.00" or "3.00" => 10, "4.00" => 16, _ => 0 };
        if (parts == 0) return false;
        int tableEnd = 0x46 + 4 * (parts + 1);
        if (tableEnd + 8 > data.Length) return false;
        var entries = new int[parts + 1];
        for (int i = 0; i <= parts; i++)
        {
            uint size = U32(data, 0x46 + 4 * i);
            if (size > int.MaxValue) return false;
            entries[i] = (int)size;
        }

        if (legacyV1)
        {
            int headerEnd = tableEnd + 2;
            var starts = new int[parts + 1];
            var sizes = new int[parts + 1];
            starts[0] = headerEnd;
            sizes[0] = entries[0];
            long offset = (long)headerEnd + entries[0];
            for (int p = 1; p <= parts; p++)
            {
                long size = (long)entries[p] + 80;
                if (offset < 0 || size < 145 || size > int.MaxValue ||
                    offset + size + 65 > data.Length)
                    return false;

                int partStart = (int)offset;
                int used = U16(data, partStart + 115);
                int controllers = U16(data, partStart + 117);
                if (used % 8 != 0 || controllers % 4 != 0 ||
                    used > size - 145 || controllers > size - 145 - used)
                    return false;

                int seekDistance = 0;
                for (int i = 0; i < 13; i++)
                    seekDistance += U16(data, partStart + 119 + 2 * i);
                if ((long)used + controllers + seekDistance != entries[p])
                    return false;

                starts[p] = partStart;
                sizes[p] = (int)size;
                offset += size;
            }

            layout = new Layout(parts, starts, sizes, true);
            return true;
        }

        foreach (int headerEnd in new[] { tableEnd + 94, tableEnd + 2, tableEnd })
        {
            if (headerEnd <= 0 || headerEnd >= data.Length) continue;
            var starts = new int[parts + 1]; var sizes = new int[parts + 1];
            long offset = headerEnd; starts[0] = (int)offset; sizes[0] = entries[0]; offset += entries[0];
            bool ok = true;
            for (int p = 1; p <= parts; p++)
            {
                long size = (long)entries[p] + 81;
                if (offset < 0 || offset + 158 > data.Length || size < 158 || size > int.MaxValue) { ok = false; break; }
                starts[p] = (int)offset; sizes[p] = (int)size;
                int used = U16(data, (int)offset + 112), ctl = U16(data, (int)offset + 114), idx = U16(data, (int)offset + 116);
                long capacity = size - 158;
                if (used % 8 != 0 || ctl % 4 != 0 || (long)used + ctl + idx > capacity) { ok = false; break; }
                offset += size;
            }
            if (ok) { layout = new Layout(parts, starts, sizes, false); return true; }
        }
        return false;
    }

    private static int U16(ReadOnlySpan<byte> d, int o) => BinaryPrimitives.ReadUInt16LittleEndian(d.Slice(o, 2));
    private static uint U32(ReadOnlySpan<byte> d, int o) => BinaryPrimitives.ReadUInt32LittleEndian(d.Slice(o, 4));
    private static void WriteBE16(Stream s, ushort v) { s.WriteByte((byte)(v >> 8)); s.WriteByte((byte)v); }
    private static void WriteBE32(Stream s, uint v) { s.WriteByte((byte)(v >> 24)); s.WriteByte((byte)(v >> 16)); s.WriteByte((byte)(v >> 8)); s.WriteByte((byte)v); }
    private static void WriteVar(Stream s, uint v)
    {
        Span<byte> bytes = stackalloc byte[5]; int n = 0;
        bytes[n++] = (byte)(v & 0x7F); v >>= 7;
        while (v != 0) { bytes[n++] = (byte)((v & 0x7F) | 0x80); v >>= 7; }
        while (n > 0) s.WriteByte(bytes[--n]);
    }
    private static void WriteTempo(Stream s, int us)
        => s.Write([0xFF, 0x51, 0x03, (byte)(us >> 16), (byte)(us >> 8), (byte)us]);
    private static void WriteChunk(Stream output, byte[] body)
    {
        output.Write("MTrk"u8); WriteBE32(output, checked((uint)(body.Length + 4)));
        output.Write(body); output.Write([0, 0xFF, 0x2F, 0]);
    }

    private readonly record struct Layout(int Parts, int[] Starts, int[] Sizes, bool IsLegacyV1);
    private readonly record struct TempoPoint(int Tick, int Microseconds);
    private readonly record struct LegacyNoteRecord(int Tick, int Offset, byte Key, byte Duration, byte Velocity, byte Flags);
    private readonly record struct MidiOutEvent(int Tick, int Order, byte[] Bytes);
    private sealed class Track(byte[] name, int channel)
    {
        public byte[] Name { get; } = name;
        public int Channel { get; } = channel;
        public List<MidiOutEvent> Events { get; } = [];
    }
}
