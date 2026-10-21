# v1 Tempo Event Investigation

## Scope and current reference

This note tracks the unresolved v1 tempo conversion in `runner.SNG` compared with `testdata/ballade/v1/sng2gs/runner.MID`. The reference MIDI contains tempo meta events at ticks 0, 8, and 192:

- tick 0: 500,000 µs per quarter note
- tick 8: 869,000 µs per quarter note
- tick 192: 869,565 µs per quarter note

The current converter does not yet reproduce the latter two events. Do not change tempo conversion until the mapping is evidence-backed.

## Confirmed static-analysis findings

- The explicit 500,000 µs/quarter tempo is emitted during startup, before `FUN_1000_33D4` initializes tempo-record processing.
- `FUN_1000_272a` reads 590 bytes from SNG offset `0x00B5` into the internal buffer referenced by `0x21DC`. The source bytes begin `C0 00 04 04`.
- `FUN_1000_0010` reads the first byte of `0x21DC` as `uVar11`; in this sample it is 192. The following `04 04` values contribute to the 4/4 time-signature meta event.
- After initial event handling, the entry path computes a remainder using `FUN_1000_61EC(currentTick, uVar11, 0)`. If nonzero, the caller calculates `uVar11 - remainder` as the delta to the next boundary. This is consistent with a 192-tick grid.
- `FUN_1000_3406` processes 4-byte records through internal pointer `0x21E0`. The second word is normalized using the tempo base at memory address `0x2962`; raw value 37 with base 120 gives 69 BPM after integer arithmetic, corresponding to 869,565 µs/quarter.
- The normal scheduler path increments pending delta accumulator `0xF9A:0xF9C`; `FUN_1000_08EC` flushes pending delta when emitting events and updates the absolute tick accumulator `0x6F2:0x6F4`.

## Still unresolved

- Which output tick receives the first tempo record.
- How the reference value 869,000 µs/quarter at tick 8 is produced.
- Whether the 192-tick grid path directly produces the reference tempo event at tick 192.
- The exact order and accumulator values across queue insertion, queue draining, countdown exceptions, and event output.

## Next steps

1. Trace `FUN_1000_33D4` through the first `FUN_1000_3406` call and the following `FUN_1000_08EC` queue drain.
2. Record pending delta `0xF9A:0xF9C` and absolute tick `0x6F2:0x6F4` at each tempo-event output point.
3. Trace the grid-delta calculation and `FUN_1000_618A` separately from tempo-record countdown processing.
4. Compare each proven output event with reference ticks 0, 8, and 192.
5. Keep static deductions clearly distinguished from runtime-trace evidence.

## Evidence limitations

This is static analysis of the SNG2S disassembly and source bytes. No runtime trace has confirmed the exact output tick mapping. No converter or test code has been changed as part of this investigation.


## Integer tempo formulas and event-time confidence (2026-10-21)

- `FUN_1000_3406` normalizes raw tempo 37 to 69 BPM. In the tempo-record path, the assembly divides 60,000 by 69 first (integer truncation to 869), then multiplies by 1000, producing 869,000 µs/quarter. This matches the reference tempo value at tick 8.
- Separately, after the initial queue drain the entry path computes the distance to the next 192-tick grid boundary with `FUN_1000_61EC`. If `0x7BA != 120`, it calls `FUN_1000_618A`, which computes 60,000,000 / 69 = 869,565 µs/quarter and emits FF 51. This matches the reference tempo value at tick 192.
- The different values are explained by two distinct integer arithmetic paths; they are not evidence that raw record first words are MIDI ticks.
- Confidence boundary: the formulas and grid-aligned path are supported by static control-flow analysis. The queue drain’s absolute tick being exactly 8 still needs a runtime trace, including the provenance of pending delta `0xF9A:0xF9C` before the first drain. Do not treat the tick-8 mapping as runtime-verified, and do not change converter logic yet.
