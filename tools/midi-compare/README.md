# MIDI Compare (local validation tool)

A Windows-friendly CLI for comparing MIDI produced by two independent converters, such as Sng2gs and a development build of Glosa.

The tool reads only the two MIDI files supplied by the user. It does not invoke Sng2gs, open SNG files, or upload data. JSON reports omit input paths, track names, and text-like MIDI metadata.

## Requirements
- Windows 10 / 11
- Python 3.10 or newer
- The dependency in `requirements.txt`

## Setup (PowerShell)

From the repository root:

~~~powershell
py -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r tools\midi-compare\requirements.txt
~~~

## Run

Convert the same local SNG with each converter yourself, then compare the two MIDI files:

~~~powershell
.\.venv\Scripts\python.exe tools\midi-compare\midi_compare.py "D:\private\reference.mid" "D:\private\glosa-output.mid"
~~~

Save a report (the report does not include input file paths):

~~~powershell
.\.venv\Scripts\python.exe tools\midi-compare\midi_compare.py "D:\private\reference.mid" "D:\private\glosa-output.mid" --json ".\comparison-report.json"
~~~

Exit codes:
- `0`: PASS (normalized event signatures match)
- `1`: FAIL (differences found)
- `2`: input, parsing, or report-writing error

## What is compared
- Notes paired FIFO within each track/channel/pitch: channel, pitch, velocity, onset and duration
- Other MIDI events: type, normalized musical position and supported data fields
- Tempo, common controller/program/pitch-bend events

Positions are normalized to quarter-note units so files with different ticks-per-beat values can be compared. Track order and track names are not used in the final event signature.

## Important limitations
- This is an initial comparison tool, not proof that either converter is correct.
- It does not render or listen to audio and cannot verify sound-module-specific timbre.
- Note-on/off pairing uses FIFO within each track/channel/pitch. If a format relies on another overlapping-note convention, inspect the warning counts and treat results as provisional.
- MIDI type 2 is rejected because each track is an independent sequence.
- SysEx payloads are not compared byte-for-byte in this first version; only their lengths are included.
- The report omits track names and text-like metadata. Review local output before sharing a report.
- Do not commit copyrighted or private MIDI/SNG files or reports that might reveal protected musical content. Keep real-world validation files on your PC.
