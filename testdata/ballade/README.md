# Ballade SNG Version Test Data

This directory contains Ballade SNG inputs and saved MIDI outputs from SNG2GS and Glosa for converter compatibility testing.

## SNG fixtures

| File | Reported Ballade version | Size | SHA-256 |
|---|---:|---:|---|
| `v1/runner.SNG` | 1.00 | 85,897 bytes | `e61b8b0315a3752dfa3d3546d5a436df0d4b0421201183c21ce4bf1780556e15` |
| `v2/SHIZUME0.SNG` | 2.00 | 75,597 bytes | `eb6e1eb5bd1aeb36acb7b53a89fb8409b09e7d3988a45847309da105622272ee` |
| `v3/START__0.SNG` | 3.00 | 82,391 bytes | `b4b719cec15139e411c94c34b05ca698e9432b9ccad308a808f08a648256cbe4` |
| `v4/BOURREE0.SNG` | 4.00 | 49,517 bytes | `a1eb69b4735b809e35c9c368776e9dae621e30405dc5a9c717133654c5f7b715` |

The version labels are the reported versions of these samples; they do not yet imply that Glosa can automatically identify every version from its header.

## MIDI outputs present

| Version | SNG2GS output | Saved Glosa output | Saved-output observation |
|---|---|---|---|
| v1 | `v1/sng2gs/runner.MID` (40,398 bytes) | Not present | Current C# converter output is validated in an integration test; a generated MIDI file is not checked in |
| v2 | `v2/sng2gs/SHIZUME0.MID` (30,262 bytes) | `v2/glosa-test-convert/SHIZUME0.mid` (45,380 bytes) | Pre-fix Glosa output has 944 additional note pairs; reference timing differs by 160 ticks |
| v3 | `v3/sng2gs/START__0.MID` (37,171 bytes) | `v3/glosa-test-convert/START__0.mid` (48,923 bytes) | Pre-fix Glosa output has 127 additional note pairs |
| v4 | `v4/sng2gs/BOURREE0.MID` (19,623 bytes) | `v4/glosa-test-convert/BOURREE0.mid` (20,726 bytes) | Saved aggregate Note-on/off events match |

The saved Glosa outputs are pre-fix fixtures. The actual converter output is compared against SNG2GS reference MIDI by automated tests. v2 reference ticks are shifted by +160 for comparison; v1, v3, and v4 use no tick shift.

The exact SNG2GS version/options and the Glosa build used for the saved outputs have not been recorded. See `manifest.json` and `docs/reverse/FACTS.md`.

## Validation status

- **Tie handling:** The converter suppresses Tie Note-ons and extends the latest pending same-key Note-off whose scheduled tick is at or before the Tie start. Synthetic regression tests cover contiguous ties, a small gate gap, interleaved pitches, a Tie with no prior same-pitch note, and same-tick overlapping notes.
- **Ballade 1.00:** A v1-specific layout path now handles the old 80-byte part-info layout. The converter's Note-on/off signatures match `v1/sng2gs/runner.MID` for all 3,295 Note-on and 3,295 Note-off events. A focused regression test also checks the same-tick sequence at tick 1728, channel 9, pitch 69.
- **Ballade 2.00 / 3.00 / 4.00:** Reference MIDI note-event regression tests compare the actual C# converter output, including tick, channel, pitch, velocity and event type.
- **CI:** Run [39252039302](https://github.com/pikachu364/glosa/actions/runs/39252039302) passed build and tests on Ubuntu, macOS, and Windows.
- **Scope:** These compatibility tests focus on Note-on/off events. They do not establish full equivalence of controller events, tempo/meta events, or physical MIDI track-chunk ordering. The v1 timer behavior is an offline model derived from static disassembly, not a runtime trace of SNG2S.

## Future work

- Identify the individual meanings of the 13 v1 relative-seek values; their cumulative positions already match each next part-info buffer and EOF.
- Record the reference MIDI generator version/options if the original generation environment can be established.
- Extend comparisons to controller events, tempo/meta events, and physical track order where reference evidence is available.
- Implement automatic version identification only when the file format provides a reliable discriminator.
