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
- **Ballade 1.00:** A v1-specific layout path handles the old 80-byte part-info layout. The converter's Note-on/off signatures match `v1/sng2gs/runner.MID` for all 3,295 Note-on and 3,295 Note-off events. A focused regression test checks the same-tick sequence at tick 1728, channel 9, pitch 69. A controller/program/pitch-bend comparison test and v1-specific state/timing normalization were added; GitHub Actions run [39333267230](https://github.com/pikachu364/glosa/actions/runs/39333267230) passed on Ubuntu, macOS, and Windows.
- **Ballade 2.00 / 3.00 / 4.00:** Reference MIDI note-event regression tests compare the actual C# converter output, including tick, channel, pitch, velocity and event type.
- **CI:** Run [39252039302](https://github.com/pikachu364/glosa/actions/runs/39252039302) passed build and tests on Ubuntu, macOS, and Windows.
- **Scope:** Note-on/off events are CI-verified. Controller/program/pitch-bend events have an offline data comparison showing 1,595 reference events matched after repeated-state suppression and tick alignment; that regression test passed CI on all three platforms. The v1 reference tempo events are now asserted in a regression test. The converter deliberately avoids the known-wrong generic tempo formula for v1, so only the default 500,000 µs/quarter event is currently emitted; reference changes at ticks 8 and 192 remain unimplemented. The tempo and reference track-layout regression tests passed in GitHub Actions run [39400722489](https://github.com/pikachu364/glosa/actions/runs/39400722489) on Ubuntu, macOS, and Windows. Regression tests now record the reference's 12-track physical layout and the four SysEx events in its `Exclusive` track. The order and SysEx output remain unverified against current Glosa output; the SNG source mapping for these SysEx messages is unresolved. The v1 timer and controller models are offline models, not runtime traces of SNG2S.

## Future work

- Identify the individual meanings of the 13 v1 relative-seek values; their cumulative positions already match each next part-info buffer and EOF.
- Record the reference MIDI generator version/options if the original generation environment can be established.
- [x] Extend the v1 comparison to controller/program/pitch-bend events; the regression test passed CI on Ubuntu/macOS/Windows (run 39333267230).
- Extend comparisons to tempo/meta events and physical track order where reference evidence is available.
- Implement automatic version identification only when the file format provides a reliable discriminator.
## Manual playback check (pending)

This check complements CI; it has **not yet been performed**. Do not mark it passed until a person has listened to the generated files.

1. Generate a fresh MIDI from each available SNG fixture using the current Glosa build. Do not use the saved `glosa-test-convert` files as current output; they are pre-fix fixtures.
2. Play the fresh Glosa MIDI and the corresponding `sng2gs/*.MID` reference with the same player and the same sound bank/synthesizer settings.
3. Check for audible silence, abrupt early termination, obvious missing passages, stuck notes, or gross timing differences. Compare instrument timbre separately because it depends on the selected sound source.
4. Record the player/version, sound bank or synthesizer, operating system, Glosa build/commit, files played, and observations for each version.
5. If a difference is heard, inspect event-level differences (including controllers, tempo/meta events and track order) before concluding that the converter is wrong.

| Sample | Glosa input | Reference MIDI | Result |
|---|---|---|---|
| v1 | `v1/runner.SNG` | `v1/sng2gs/runner.MID` | Pending: fresh Glosa output and listening check |
| v2 | `v2/SHIZUME0.SNG` | `v2/sng2gs/SHIZUME0.MID` | Pending: fresh Glosa output and listening check |
| v3 | `v3/START__0.SNG` | `v3/sng2gs/START__0.MID` | Pending: fresh Glosa output and listening check |
| v4 | `v4/BOURREE0.SNG` | `v4/sng2gs/BOURREE0.MID` | Pending: fresh Glosa output and listening check |

A listening check is a manual sanity check, not a substitute for automated event comparisons. If the original SNG2GS runtime is available, listening to its direct playback as well is useful, but the comparison must still document the sound source and settings.
