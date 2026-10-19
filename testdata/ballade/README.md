# Ballade SNG Version Test Data

This directory contains Ballade SNG inputs and MIDI outputs from SNG2GS and Glosa for version-specific converter compatibility testing.

## SNG fixtures

| File | Reported Ballade version | Size | SHA-256 |
|---|---:|---:|---|
| `v1/runner.SNG` | 1.00 | 85,897 bytes | `e61b8b0315a3752dfa3d3546d5a436df0d4b0421201183c21ce4bf1780556e15` |
| `v2/SHIZUME0.SNG` | 2.00 | 75,597 bytes | `eb6e1eb5bd1aeb36acb7b53a89fb8409b09e7d3988a45847309da105622272ee` |
| `v3/START__0.SNG` | 3.00 | 82,391 bytes | `b4b719cec15139e411c94c34b05ca698e9432b9ccad308a808f08a648256cbe4` |
| `v4/BOURREE0.SNG` | 4.00 | 49,517 bytes | `a1eb69b4735b809e35c9c368776e9dae621e30405dc5a9c717133654c5f7b715` |

The version labels are the reported versions of these samples; they do not yet imply that Glosa can automatically identify every version from its header.

## MIDI outputs currently present

| Version | SNG2GS output | Glosa output | Current event-level observation |
|---|---|---|---|
| v1 | `v1/sng2gs/runner.MID` (40,398 bytes) | Not present | SNG2GS only; Glosa v1 layout validation remains pending |
| v2 | `v2/sng2gs/SHIZUME0.MID` (30,262 bytes) | `v2/glosa-test-convert/SHIZUME0.mid` (45,380 bytes) | 2,713 reference Note-ons vs 3,657 Glosa Note-ons; v2 tick offset differs by 160 ticks |
| v3 | `v3/sng2gs/START__0.MID` (37,171 bytes) | `v3/glosa-test-convert/START__0.mid` (48,923 bytes) | 5,362 reference Note-ons vs 5,489 Glosa Note-ons |
| v4 | `v4/sng2gs/BOURREE0.MID` (19,623 bytes) | `v4/glosa-test-convert/BOURREE0.mid` (20,726 bytes) | Note-on/off event counts and aggregate events match |

SNG2GS version/options and the exact Glosa build used to produce these saved outputs have not yet been recorded. See `manifest.json` and the reverse-engineering notes for details.

## Validation status

- **Tie handling:** A first correction is implemented in Glosa and synthetic regression tests have been added. CI and post-change fixture comparisons still need to be confirmed.
- **Ballade 1.00:** Reference MIDI exists, but Glosa output is not present; the v1 layout remains unresolved.
- **Ballade 2.00:** Reference and Glosa MIDI are present; the event comparison exposes extra Tie-derived notes in the previous Glosa output.
- **Ballade 3.00:** Reference and Glosa MIDI are present; the event comparison exposes extra Tie-derived notes in the previous Glosa output.
- **Ballade 4.00:** Reference and Glosa MIDI are present; aggregate note events match.

## CI strategy

The existing synthetic SNG converter tests should run in GitHub Actions. Fixture-based tests should additionally validate parsing and compare generated note events against the checked-in MIDI outputs while accounting for known track/channel ordering and tick-offset differences.

Future work:
- Verify the modified converter against the checked-in v2-v4 fixtures.
- Add v1 layout/event validation when its record layout is independently established.
- Record converter versions/options and explain residual event differences.
- Implement automatic version identification only when the file format provides a reliable discriminator.
