#!/usr/bin/env python3
"""Compare two Standard MIDI Files locally without printing song-identifying metadata."""
from __future__ import annotations
import argparse, json, sys
from collections import Counter, defaultdict, deque
from fractions import Fraction
from pathlib import Path
from typing import Any

try:
    import mido
except ImportError:
    print("Missing dependency: install requirements.txt with pip.", file=sys.stderr)
    raise SystemExit(2)

TEXT_TYPES = {"track_name", "text", "copyright", "instrument_name", "lyrics",
              "marker", "cue_marker", "sequencer_specific", "end_of_track"}

def qtime(ticks: int, ppq: int) -> str:
    value = Fraction(ticks, ppq)
    return str(value.numerator) if value.denominator == 1 else f"{value.numerator}/{value.denominator}"

def fields(msg: Any) -> tuple:
    result = [msg.type]
    for name in ("channel", "control", "value", "program", "pitch", "pressure",
                 "note", "velocity", "numerator", "denominator",
                 "clocks_per_click", "notated_32nd_notes_per_beat", "tempo"):
        if hasattr(msg, name):
            result.append((name, getattr(msg, name)))
    if msg.type == "sysex":
        result.append(("data_length", len(msg.data)))
    return tuple(result)

def load_events(path: Path) -> tuple[list[tuple], dict[str, Any]]:
    midi = mido.MidiFile(str(path))
    if midi.ticks_per_beat <= 0:
        raise ValueError("Invalid MIDI ticks-per-beat value")
    if midi.type == 2:
        raise ValueError("MIDI type 2 contains independent sequences and is not supported yet")
    notes, other = [], []
    orphan_offs = unmatched_ons = tempo_events = 0
    for track in midi.tracks:
        absolute = 0
        pending = defaultdict(deque)
        for msg in track:
            absolute += msg.time
            pos = qtime(absolute, midi.ticks_per_beat)
            if msg.type == "set_tempo":
                tempo_events += 1
            on = msg.type == "note_on" and msg.velocity > 0
            off = msg.type == "note_off" or (msg.type == "note_on" and msg.velocity == 0)
            if on:
                pending[(getattr(msg, "channel", 0), msg.note)].append((absolute, msg.velocity, pos))
            elif off:
                key = (getattr(msg, "channel", 0), msg.note)
                if pending[key]:
                    start, velocity, start_pos = pending[key].popleft()
                    notes.append(("note", key[0], key[1], velocity, start_pos,
                                  qtime(absolute - start, midi.ticks_per_beat)))
                else:
                    orphan_offs += 1
                    notes.append(("orphan_note_off", key[0], key[1], pos))
            elif msg.type not in TEXT_TYPES:
                other.append((pos, fields(msg)))
        for queue in pending.values():
            unmatched_ons += len(queue)
            for _, velocity, start_pos in queue:
                notes.append(("unmatched_note_on", start_pos, velocity))
    events = [("note", x) for x in sorted(notes, key=repr)]
    events += [("other", x) for x in sorted(other, key=repr)]
    return events, {
        "midi_type": midi.type, "track_count": len(midi.tracks),
        "ticks_per_beat": midi.ticks_per_beat, "note_events": len(notes),
        "other_events": len(other), "tempo_events": tempo_events,
        "orphan_note_offs": orphan_offs, "unmatched_note_ons": unmatched_ons,
    }

def compare(a_path: Path, b_path: Path, max_diffs: int) -> dict[str, Any]:
    a_events, a_summary = load_events(a_path)
    b_events, b_summary = load_events(b_path)
    a, b = Counter(a_events), Counter(b_events)
    a_only = sum((a - b).values())
    b_only = sum((b - a).values())
    distinct = []
    for event, count in sorted((a - b).items(), key=lambda item: repr(item[0])):
        distinct.append({"side": "A_only", "count": count, "category": event[0]})
    for event, count in sorted((b - a).items(), key=lambda item: repr(item[0])):
        distinct.append({"side": "B_only", "count": count, "category": event[0]})
    return {
        "result": "PASS" if not a_only and not b_only else "FAIL",
        "comparison": "normalized MIDI event signatures; track names and text metadata excluded",
        "input_A": a_summary, "input_B": b_summary,
        "counts": {"events_A": sum(a.values()), "events_B": sum(b.values()),
                   "A_only": a_only, "B_only": b_only},
        "differences": distinct[:max_diffs],
        "differences_truncated": len(distinct) > max_diffs,
        "limitations": [
            "Structural comparison only; not an audio or sound-module listening test.",
            "Notes are paired FIFO within each track/channel/pitch; unusual overlapping-note conventions need review.",
            "Tempo and other events are compared at normalized quarter-note positions.",
            "SysEx payloads are not included; only payload length is compared.",
            "Track names and text-like metadata are excluded.",
        ],
    }

def main() -> int:
    parser = argparse.ArgumentParser(description="Compare two MIDI files locally; report omits input paths and song text.")
    parser.add_argument("midi_a", type=Path)
    parser.add_argument("midi_b", type=Path)
    parser.add_argument("--json", dest="json_path", type=Path, help="Optional JSON report path")
    parser.add_argument("--max-diffs", type=int, default=100)
    args = parser.parse_args()
    if args.max_diffs < 0:
        parser.error("--max-diffs must be zero or greater")
    if not args.midi_a.is_file() or not args.midi_b.is_file():
        print("Input file not found or not a regular file.", file=sys.stderr)
        return 2
    try:
        result = compare(args.midi_a, args.midi_b, args.max_diffs)
    except (OSError, ValueError, EOFError) as exc:
        print(f"Could not compare MIDI files: {type(exc).__name__}: {exc}", file=sys.stderr)
        return 2
    rendered = json.dumps(result, ensure_ascii=False, indent=2)
    print(rendered)
    if args.json_path:
        try:
            args.json_path.write_text(rendered + "\n", encoding="utf-8")
        except OSError:
            print("Could not write report file.", file=sys.stderr)
            return 2
    return 0 if result["result"] == "PASS" else 1

if __name__ == "__main__":
    raise SystemExit(main())
