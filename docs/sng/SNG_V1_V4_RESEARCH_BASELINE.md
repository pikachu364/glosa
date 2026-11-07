# Ballade SNG v1-v4: research baseline

Status: research handoff; no SNG implementation has been ported into this repository yet.

## Scope and implementation direction

Glosa is intended to support Ballade SNG versions 1.00 through 4.00 through two separate paths:

- **SNG to SMF conversion:** reproduce the observable conversion behavior of SNG2GS / SNG2S Ver. 3.3.
- **Direct SNG playback:** independently reproduce the relevant direct-input and playback behavior of MIMPI V4.27. This path must not rely on converting the SNG to SMF first.

The two paths must not be conflated. JMPlayer's SNG implementation is incomplete and does not support v1; it is historical reference material, not a format specification.

## Primary references and data handling

The original research material (SNG2S executable/manual, MIMPI executable/manual, SNG_DATA, paired reference MID files, and binary-analysis dumps) is held in the private research repository. Do not copy those binaries, private samples, or raw dumps into this public repository.

The public development repository may contain independently written implementation code, tests based on public fixtures, and concise findings that do not disclose private source data.

## Confirmed manual-level facts

### SNG2S Ver. 3.3

- Converts Music-kun / Music Rou / Ballade SNG files to Standard MIDI Files.
- Supports Ballade identifiers 1.00, 2.00, 3.00, and 4.00.
- MIDI division is 48 ticks per quarter note.
- Default output is SMF Format 1.
- Default SysEx placement is a dedicated track immediately after the conductor track (/X2); /X1 places SysEx in the conductor track and /XD disables SysEx conversion.
- Default part ordering is descending. Ballade3 additionally places parts 16 down through 11 between parts 10 and 9. /F selects ascending order; /N omits parts without note data.

### MIMPI V4.27

Its manual lists SNG playback for Music-kun, Music Rou, Music Rou 2, Ballade, Ballade2, and Ballade3, separately from Standard MIDI File playback. This establishes SNG as a direct input format, but does not disclose the parser's internal layout or prove every edge case.

## Static-analysis findings: scheduler streams

The following stream pointers were identified in the inspected executable bytes and cross-checked between SNG2S and MIMPI. These are static-analysis findings, not runtime traces.

| Purpose | SNG2S | MIMPI V4.27 |
|---|---:|---:|
| Conductor / time-signature stream | ES:[21DC] | ES:[6E88] |
| Tempo stream | ES:[21E0] | ES:[6E8C] |
| Volume automation stream | ES:[21E4] | ES:[6E90] |
| SysEx stream | ES:[1E50] | ES:[6A5C] |

Tempo and volume are separate streams. The tempo and volume streams use four-byte records: a countdown word followed by a parameter word. A separate 64-entry countdown queue emits note-off-shaped messages; it must not be mistaken for the tempo stream.

The tempo conversion observed in both programs is:

`timerCount = clamp(floor(baseTempo * parameter / 64), 1, 480)`

SNG2S converts the timer count to an SMF tempo value using:

`microsecondsPerQuarter = floor(60000 / timerCount) * 1000`

MIMPI applies the timing internally rather than emitting an SMF tempo meta event.

Volume automation values at or above 0x65 are clamped to 0x4B. MIMPI's downstream handling corresponds to MIDI Controller 7.

## Base-tempo fields observed in paired samples

The following file offsets were observed in the inspected SNG_DATA samples:

- v1: offset 0x8F (conductor start + 27 in the inspected layout)
- v2: offset 0x79
- v3: offset 0x79
- v4: offset 0x91

Values vary by song and version. Treat these as sample-supported layout findings; validate the correct header/layout selection for each file before using them in production parsing.

## Known defects in the historical Glosa converter

The converter in the former development repository contains version-specific code, but its layout and tempo handling were substantially based on JMPlayer and are not proven SNG2S-compatible.

In particular:

- Later-version tempo conversion assumes a base tempo of 120 rather than reading the song's actual base-tempo field.
- A v2 special case handles only one initial tempo record and does not process the full stream.
- Some tempo tick positions are derived from fixed shortcuts.
- The first non-default tempo event occurs at different ticks in inspected v2 references (including 108, 128, 152, and 192); a universal fixed tick is not valid.
- Startup tick placement depends on shared scheduler state and interactions among conductor/time-signature and tempo streams. This is not yet fully reconstructed.
- The historical converter's part ordering and SysEx-track behavior also differ from SNG2S defaults.

Do not patch these issues by substituting another fixed offset. Reconstruct the scheduler startup state and stream service order first.

## Evidence policy

Label findings as **Confirmed**, **Hypothesis**, or **Unknown**. Distinguish manual facts, byte-level static analysis, sample observations, and inferred behavior. Never describe static analysis as runtime tracing or successful playback.

## Next steps

1. Continue reconstructing SNG2S scheduler initialization and stream service order, including the code paths around CS:39C9 and CS:3B78.
2. Map the corresponding MIMPI scheduler and independently identify its v1-v4 SNG parsing paths.
3. Expand paired SNG/MID comparisons across all available v1-v4 samples, covering tracks, absolute event ticks, tempo, time signatures, SysEx, controllers, program changes, pitch bend, and end-of-track.
4. Design the converter and direct player as separate components that fit Glosa's current architecture.
5. Add regression tests from public fixtures and synthetic cases. Do not add private reference MID files or private SNG_DATA samples to this repository.
6. Only implement behavior once its binary layout and timing rules have sufficient evidence.
