# Ballade SNG Version Test Data

This directory contains original Roland/Boss Ballade SNG test fixtures for version-specific converter compatibility testing.

## Test fixtures

| File | Reported Ballade version | Size | SHA-256 |
|---|---:|---:|---|
| `v1/runner.SNG`   | 1.00 | 85,897 bytes | `e61b8b0315a3752dfa3d3546d5a436df0d4b0421201183c21ce4bf1780556e15` |
| `v2/SHIZUME0.SNG` | 2.00 | 75,597 bytes | `eb6e1eb5bd1aeb36acb7b53a89fb8409b09e7d3988a45847309da105622272ee` |
| `v3/START__0.SNG` | 3.00 | 82,391 bytes | `b4b719cec15139e411c94c34b05ca698e9432b9ccad308a808f08a648256cbe4` |
| `v4/BOURREE0.SNG` | 4.00 | 49,517 bytes | `a1eb69b4735b809e35c9c368776e9dae621e30405dc5a9c717133654c5f7b715` |

The version labels are the reported versions of these samples. They do not yet imply that Glosa can automatically identify every version from its file header.

See [`manifest.json`](manifest.json) for machine-readable metadata.

## Validation status

- **Ballade 1.00:** File copied into the repository test fixtures; converter validation pending.
- **Ballade 2.00:** File copied into the repository test fixtures; converter validation pending.
- **Ballade 3.00:** Local conversion succeeded and generated a 48,923-byte MIDI file; MIDI read-back and playback validation pending.
- **Ballade 4.00:** File copied into the repository test fixtures; converter validation pending.

## CI strategy

GitHub Actions should run the existing synthetic SNG converter tests in `Glosa.Tests`. Fixture-based tests should also validate parsing and MIDI generation for each real SNG sample, so version-specific differences can be detected automatically.

Future work:

- Convert each fixture and verify the generated MIDI can be read back through `SmfReader`.
- Record per-version results and unsupported structures.
- Implement automatic version identification only when the file format provides a reliable discriminator.