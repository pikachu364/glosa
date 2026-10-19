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

| Version | SNG2GS output | Glosa output | Saved-output observation |
|---|---|---|---|
| v1 | `v1/sng2gs/runner.MID` (40,398 bytes) | Not present | Current converter rejects this SNG layout; v1 layout investigation is pending |
| v2 | `v2/sng2gs/SHIZUME0.MID` (30,262 bytes) | `v2/glosa-test-convert/SHIZUME0.mid` (45,380 bytes) | Old Glosa output has 944 additional note pairs; reference timing differs by 160 ticks |
| v3 | `v3/sng2gs/START__0.MID` (37,171 bytes) | `v3/glosa-test-convert/START__0.mid` (48,923 bytes) | Old Glosa output has 127 additional note pairs |
| v4 | `v4/sng2gs/BOURREE0.MID` (19,623 bytes) | `v4/glosa-test-convert/BOURREE0.mid` (20,726 bytes) | Aggregate Note-on/off events match |

The saved Glosa outputs are pre-fix fixtures. An offline event-generation model using the refined Tie rule matches all v2/v3 reference Note-on/off events per track (v2 reference ticks are shifted by +160 for comparison; v3 needs no shift). The actual post-fix C# converter output is checked by CI.

The exact SNG2GS version/options and the Glosa build used for the saved outputs have not been recorded. See `manifest.json` and `docs/reverse/FACTS.md`.

## Validation status

- **Tie handling:** The converter now suppresses Tie Note-ons and extends the latest pending same-key Note-off whose scheduled tick is at or before the Tie start. Four synthetic regression tests cover contiguous ties, a small gate gap, interleaved pitches, and a Tie with no prior same-pitch note.
- **Ballade 1.00:** The reference MIDI exists, but the current converter rejects `runner.SNG` as an unsupported layout. The attempted integration test confirmed this limitation and was removed from the passing compatibility suite until the layout is independently established.
- **Ballade 2.00 / 3.00:** The offline event-generation model matches the reference MIDI per track; CI and compiled output comparison are pending.
- **Ballade 4.00:** Saved aggregate Note-on/off events match.

## Future work

- Keep CI comparison for v2-v4 and investigate any failures.
- Reverse engineer the v1 block layout from `runner.SNG` and its SNG2GS reference before adding a v1 conversion test.
- Record converter versions/options and explain any residual differences.
- Implement automatic version identification only when the file format provides a reliable discriminator.
