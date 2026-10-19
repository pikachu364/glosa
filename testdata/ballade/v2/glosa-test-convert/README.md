# Glosa test conversion — Ballade v2

- Input: [`../SHIZUME0.SNG`](../SHIZUME0.SNG)
- Output: `SHIZUME0.mid` (45,380 bytes; present)
- Converter: Development version of Glosa; exact commit/build not recorded
- Purpose: SNG-to-MIDI comparison fixture

This is the pre-fix output: it contains 3,657 Note-ons and 3,657 Note-offs, including 944 additional note pairs compared with SNG2GS. The refined offline Tie model matches the reference events; the actual post-fix C# output remains to be verified.
