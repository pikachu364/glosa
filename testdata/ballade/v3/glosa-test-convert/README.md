# Glosa test conversion — Ballade v3

- Input: [`../START__0.SNG`](../START__0.SNG)
- Output: `START__0.mid` (48,923 bytes; present)
- Converter: Development version of Glosa; exact commit/build not recorded
- Purpose: SNG-to-MIDI comparison fixture

This is the pre-fix output: it contains 5,489 Note-ons and 5,489 Note-offs, including 127 additional note pairs compared with SNG2GS. The refined offline Tie model matches the reference events; the actual post-fix C# output remains to be verified.
