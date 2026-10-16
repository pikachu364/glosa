# Ballade SNG version test data

This directory tracks the locally available real-world SNG samples used during development of Ballade SNG conversion.

## Why the binary files are not stored here

The sample files are currently kept outside the repository (for example, under `R:\\testdata`). Permission to redistribute them publicly has not been confirmed, so this repository records metadata only. Do not commit the SNG binaries unless redistribution permission is confirmed.

## Current sample matrix

| File | Reported Ballade version | Size | Status |
|---|---:|---:|---|
| `SHIZUME0.SNG` | 2.00 | 75,597 bytes | Conversion and read-back validation pending |
| `START__0.SNG` | 3.00 | 82,391 bytes | Conversion succeeded locally; generated MIDI is 48,923 bytes; read-back and playback validation pending |
| `BOURREE0.SNG` | 4.00 | 49,517 bytes | Conversion and read-back validation pending |

The version labels above are the versions reported for the local samples. They are not yet a claim that Glosa automatically detects the version from every file header.

See [manifest.json](manifest.json) for machine-readable metadata. SHA-256 values are intentionally left null until they are calculated directly from the local copies and checked in.

## Local metadata verification on Windows

Run these commands from Command Prompt to record each file's SHA-256 value:

```bat
certutil -hashfile R:\\testdata\\SHIZUME0.SNG SHA256
certutil -hashfile R:\\testdata\\START__0.SNG SHA256
certutil -hashfile R:\\testdata\\BOURREE0.SNG SHA256
```

Compare the reported file sizes with `manifest.json` before recording hashes.

## CI strategy

GitHub Actions should run the synthetic SNG converter tests that are already part of `Glosa.Tests`. Those tests validate parsing and MIDI generation without requiring proprietary or user-provided binary samples. Real-file checks should remain a separate local validation step until redistribution rights and a fixture strategy are clear.

Future work:
- verify converter output for each local sample;
- read generated MIDI back through `SmfReader`;
- record per-version results and any unsupported structures;
- add automatic version identification only when the file format provides a reliable discriminator.
