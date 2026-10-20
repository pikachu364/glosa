#!/usr/bin/env python3
"""Compare two MIDI files locally; reports omit file paths and text metadata."""
from __future__ import annotations

import argparse
import hashlib
import json
import sys
from collections import Counter, defaultdict, deque
from fractions import Fraction
from pathlib import Path
from typing import Any

try:
    import mido
except ImportError:
    print("Missing dependency: install requirements.txt with pip.", file=sys.stderr)
    raise SystemExit(2)

IGNORED_META = {
    "track_name", "text", "copyright", "instrument_name", "lyrics",
    "marker", "cue_marker", "sequencer_specific", "end_of_track",
}


def position(ticks: int, ppq: int) -> str:
    value = Fraction(ticks, ppq)
    return str(value.numerator) if value.denominator == 1 else f"{value.numerator}/{value.denominator}"


def event_fields(msg: Any) -> tuple:
    """Keep event values in memory for exact comparison; never print payloads."""
    fields = [msg.type]
    for name in (
        "channel", "control", "value", "program", "pitch", "pressure",
        "note", "velocity", "numerator", "denominator", "clocks_per_click",
        "notated_32nd_notes_per_beat", "tempo",
    ):
        if hasattr(msg, name):
            fields.append((name, getattr(msg, name)))
    if msg.type == "sysex":
        # Compare payloads exactly without putting their contents in the report.
        fields.append(("payload_sha256", hashlib.sha256(bytes(msg.data)).hexdigest()))
    return tuple(fields)


def read_midi(path: Path) -> tuple[list[tuple], dict[str, int]]:
    midi = mido.MidiFile(str(path))
    if midi.ticks_per_beat <= 0:
        raise ValueError("Invalid MIDI ticks-per-beat value")
    if midi.type == 2:
        raise ValueError("MIDI type 2 contains independent sequences and is not supported yet")

    notes: list[tuple] = []
    other: list[tuple] = []
    orphan_offs = unmatched_ons = 0

    # Track numbers and track names are ignored so equivalent exports can differ in layout.
    for track in midi.tracks:
        tick = 0
        pending: dict[tuple[int, int], deque] = defaultdict(deque)
        for msg in track:
            tick += msg.time
            pos = position(tick, midi.ticks_per_beat)
            if msg.type == "note_on" and msg.velocity > 0:
                key = (getattr(msg, "channel", 0), msg.note)
                pending[key].append((tick, msg.velocity, pos))
            elif msg.type == "note_off" or (msg.type == "note_on" and msg.velocity == 0):
                key = (getattr(msg, "channel", 0), msg.note)
                if pending[key]:
                    start, velocity, start_pos = pending[key].popleft()
                    notes.append(("note", key[0], key[1], velocity, start_pos,
                                  position(tick - start, midi.ticks_per_beat)))
                else:
                    orphan_offs += 1
                    notes.append(("orphan_note_off", key[0], key[1], pos))
            elif msg.type not in IGNORED_META:
                other.append((pos, event_fields(msg)))
        for queue in pending.values():
            unmatched_ons += len(queue)
            for _, velocity, start_pos in queue:
                notes.append(("unmatched_note_on", start_pos, velocity))

    events = [("note", item) for item in notes]
    events.extend(("event", item) for item in other)
    return events, {
        "midi_type": midi.type,
        "track_count": len(midi.tracks),
        "note_count": sum(1 for item in notes if item[0] == "note"),
        "other_event_count": len(other),
        "orphan_note_offs": orphan_offs,
        "unmatched_note_ons": unmatched_ons,
    }


def compare(path_a: Path, path_b: Path, max_diffs: int) -> dict[str, Any]:
    events_a, summary_a = read_midi(path_a)
    events_b, summary_b = read_midi(path_b)
    counts_a, counts_b = Counter(events_a), Counter(events_b)
    only_a, only_b = counts_a - counts_b, counts_b - counts_a
    differences = []
    for event, count in sorted(only_a.items(), key=lambda pair: repr(pair[0])):
        differences.append({"side": "A_only", "count": count, "category": event[0]})
    for event, count in sorted(only_b.items(), key=lambda pair: repr(pair[0])):
        differences.append({"side": "B_only", "count": count, "category": event[0]})

    return {
        "result": "PASS" if not only_a and not only_b else "FAIL",
        "input_A": summary_a,
        "input_B": summary_b,
        "counts": {
            "events_A": sum(counts_a.values()),
            "events_B": sum(counts_b.values()),
            "A_only": sum(only_a.values()),
            "B_only": sum(only_b.values()),
        },
        "differences": differences[:max_diffs],
        "differences_truncated": len(differences) > max_diffs,
        "notes": [
            "PASS means the compared note and MIDI-event signatures match; it does not prove either converter matches the original SNG specification.",
            "Track names, text metadata, track order and track count are not pass/fail criteria.",
            "Note-on/off pairing uses FIFO within each track/channel/pitch.",
            "SysEx payloads are compared internally by SHA-256; payloads are never written to the report.",
        ],
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Compare two MIDI files locally. The report excludes paths and song text."
    )
    parser.add_argument("midi_a", type=Path, help="MIDI from Sng2gs")
    parser.add_argument("midi_b", type=Path, help="MIDI from Glosa")
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
    report = json.dumps(result, ensure_ascii=False, indent=2)
    print(report)
    if args.json_path:
        try:
            args.json_path.write_text(report + "\n", encoding="utf-8")
        except OSError:
            print("Could not write report file.", file=sys.stderr)
            return 2
    return 0 if result["result"] == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
