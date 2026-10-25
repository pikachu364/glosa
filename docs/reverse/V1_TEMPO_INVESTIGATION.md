# v1 Tempo Event Investigation

> **Current status (updated 2026-10-25):** Static startup tracing and byte-level comparison with the saved reference MIDI now explain the inspected v1 path. The original EXE initialized image confirms `0x6EE = 4`; setup SysEx at ticks 0/4/8/8 has been mapped to producer paths, and both tick-8 payloads match byte-for-byte (FACTS F-094/F-095/F-097/F-101). The coarse 869,000 µs/qn event at tick 8 and exact 869,565 µs/qn event at tick 192 arise from distinct paths. Runtime trace/playback, indirect writes, and generalization to every v1 file remain unverified. Do not change converter timing on the basis of static analysis alone. See [NEXT_STEPS.md](NEXT_STEPS.md), T-017.

## Scope and current reference

This note tracks the unresolved v1 tempo conversion in `runner.SNG` compared with `testdata/ballade/v1/sng2gs/runner.MID`. The reference MIDI contains tempo meta events at ticks 0, 8, and 192:

- tick 0: 500,000 µs per quarter note
- tick 8: 869,000 µs per quarter note
- tick 192: 869,565 µs per quarter note

The current converter has a dedicated v1 tempo path, and the public regression test asserts byte sequences for all three reference tempo values. The unresolved item is the original SNG2S tick-8 scheduling explanation, not absence of all v1 tempo implementation.

## Confirmed static-analysis findings

- The explicit 500,000 µs/quarter tempo is emitted during startup, before `FUN_1000_33D4` initializes tempo-record processing.
- `FUN_1000_272a` is documented as reading 590 bytes into the internal buffer referenced by `0x21DC`; the grid/time-signature bytes are `C0 00 04 04`. Notes disagree on their absolute source offset: `0x00B5` (=181), 231, and `0x74 + 125` (=241) are all recorded. Treat the 192-tick grid as supported, but the absolute offset as unresolved until raw file coordinates are reconciled.
- `FUN_1000_0010` reads the first byte of `0x21DC` as `uVar11`; in this sample it is 192. The following `04 04` values contribute to the 4/4 time-signature meta event.
- After initial event handling, the entry path computes a remainder using `FUN_1000_61EC(currentTick, uVar11, 0)`. If nonzero, the caller calculates `uVar11 - remainder` as the delta to the next boundary. This is consistent with a 192-tick grid.
- `FUN_1000_3406` processes 4-byte records through internal pointer `0x21E0`. The second word is normalized using the tempo base at memory address `0x2962`; raw value 37 with base 120 gives 69 BPM after integer arithmetic, corresponding to 869,565 µs/quarter.
- The normal scheduler path increments pending delta accumulator `0xF9A:0xF9C`; `FUN_1000_08EC` flushes pending delta when emitting events and updates the absolute tick accumulator `0x6F2:0x6F4`.

## Remaining limitations

- The static startup path explains the coarse tempo's tick-8 placement for the inspected reference path: the first two setup drains leave pending delta 4; the first `FUN_1000_3406` call skips the normal scheduler increment while `0xF96` is nonzero; when `0x622 == 0`, the pending delta survives to the subsequent queue drain (FACTS F-095). The user confirmed that the reference MIDI was generated without command-line switches, but no runtime invocation trace was performed.
- The exact-tempo event at tick 192 is associated with the separate 192-tick grid path. Keep it distinct from the coarse tick-8 event.
- Runtime memory state, possible indirect/aliased writes, and generalization beyond `runner.SNG` remain unverified. The analysis does not establish actual playback behavior in MIMPIV4.

## Next steps

1. Keep converter behavior and v1 tempo regression tests unchanged until independent validation is available.
2. If a runtime trace or executable test environment becomes available, record `0x622`, `0xF9A:0xF9C`, and `0x6F2:0x6F4` around startup drains and compare against ticks 0/8/192.
3. Keep the SNG2S MIDI-export tempo path separate from the unresolved MIMPIV4 playback-tempo consumer.
4. Preserve the distinction between static executable/data-image evidence and runtime-trace evidence.

## Evidence limitations

This is static analysis of the SNG2S disassembly and source bytes. No runtime trace has confirmed the exact output tick mapping. No converter or test code has been changed as part of this investigation.


## Integer tempo formulas and event-time confidence (2026-10-21)

- `FUN_1000_3406` normalizes raw tempo 37 to 69 BPM. In the tempo-record path, the assembly divides 60,000 by 69 first (integer truncation to 869), then multiplies by 1000, producing 869,000 µs/quarter. This matches the reference tempo value at tick 8.
- Separately, after the initial queue drain the entry path computes the distance to the next 192-tick grid boundary with `FUN_1000_61EC`. If `0x7BA != 120`, it calls `FUN_1000_618A`, which computes 60,000,000 / 69 = 869,565 µs/quarter and emits FF 51. This matches the reference tempo value at tick 192.
- The different values are explained by two distinct integer arithmetic paths; they are not evidence that raw record first words are MIDI ticks.
- Confidence boundary: the formulas and grid-aligned path are supported by static control-flow analysis. The queue drain’s absolute tick being exactly 8 still needs a runtime trace, including the provenance of pending delta `0xF9A:0xF9C` before the first drain. Do not treat the tick-8 mapping as runtime-verified, and do not change converter logic yet.

## 初回 tempo queue drain の再追跡（2026-10-21）

`FUN_1000_1C70` から初回 tempo meta の queue drain までを静的に追った。

- v1 の `FUN_1000_1C70` は `FUN_1000_1CE4` を呼ぶ。`FUN_1000_1CE4` は条件 `(0x6F8 == 0) || (0x6FA == 2)` が成立する経路で queue event を出力し `FUN_1000_08EC` を呼んだ後、`0xF9A:0xF9C = 0x6EE:0` を設定する処理を2回行う。2回目の drain は pending delta を flush し、条件に応じて `0x6F2:0x6F4` を進める。その後も pending delta は `0x6EE` に設定された状態で戻る。
- `FUN_1000_33D4` は `FUN_1000_3DE2` の後に `FUN_1000_3406` を呼ぶ。`FUN_1000_3DE2` が `0xF96 = 0xFFFF` に設定し、`FUN_1000_33D4` は `FUN_1000_3406` の後で `0xF96 = 0` に戻す。このため、**初回** `FUN_1000_3406` は通常経路の pending delta `+1` を実行しない。
- `FUN_1000_33D4` の戻り後、entry path は `0x622 != 0` の場合に pending delta と `0x1ED8:0x1EDA` をゼロ化し、それから `FUN_1000_08EC` で queue を drain する。`FF` meta status はこの consumer で pending delta flush と absolute tick 更新を起こす。
- よって初回 tempo meta の absolute tick は、preamble drain 後の absolute tick、`0x6EE` の実値、`0x622` 分岐、および queue の先行 event に依存する。解析テキストでは `0x6EE` の直接代入を確認できず、値の由来と reference MIDI 生成時の `0x622` は未解決。

**結論:** これで初回呼び出しが通常の `+1` 経路ではないことは静的に分かったが、tick 8 の最終対応はまだ証明されていない。runtime trace は未実施。converter / tests は変更しない。


## 現在の進捗スナップショット（2026-10-21）

### 説明できていること

- 参照 MIDI の tempo meta は tick 0 / 8 / 192 で 500,000 / 869,000 / 869,565 µs/quarter。
- raw tempo 37 を 69 BPM に正規化した後、tempo-record path の整数除算は 869,000、192-tick grid path は 869,565 を生成する。
- FUN_1000_33D4 内の初回 FUN_1000_3406 は 0xF96 != 0 の例外経路であり、通常 scheduler path の pending-delta +1 を実行しない。
- 以前は `0x622 ↔ /F` と推定していたが、この対応は撤回する。`0x622` は SysEx を抑制する経路、`0x626` は track-order table を選ぶ経路に使われるため、現時点の最有力対応は `0x622 ↔ /XD`、`0x626 ↔ /F`。token bytes の直接確認は未実施。

### まだ証明できていないこと

- tempo-record path の 869000 が参照 MIDI の tick 8 で出力されること（数値の一致だけでは時刻の証明にならない）。
- 0x6EE の実値と初期化元、preamble queue drain 前後の pending delta / absolute tick。
- 参照 MIDI 生成時に `/XD` が指定されたか、したがって `0x622` の分岐が実際に有効だったか。
- grid path が参照 MIDI の tick 192 event を直接生成したという因果関係の runtime 確認。

### 次の調査順

1. 0x6EE の初期値・書き込み元をデータ領域と間接書き込みも含めて探す。
2. `0xF05` の option token 実体を含むバイナリ／データダンプを探し、`0x622 ↔ /XD` を直接確認する。`0x626 ↔ /F` も token bytes か runtime trace で検証する。
3. preamble の queue drain ごとに 0xF9A:0xF9C と 0x6F2:0x6F4 を表にして、参照 tick と照合する。
4. runtime trace ができなければ、その制約を維持し、converter の tempo 規則を推測で変更しない。

**実装ステータス:** v1 ReadTempi guard 維持。converter / regression tests に変更なし。静的解析のみで、runtime trace と実再生は未実施。


## 0x6EE の命令参照スキャンと option parser の追加確認（2026-10-21）

### 0x6EE

チェックイン済み逆解析テキストの命令行を全走査したところ、`[0x6EE]` の直接参照は29箇所で、いずれも `MOV AX,[0x6EE]` の読み出しだった。直接 store は見つからない。`FUN_1000_1CE4` の queue drain 後は、読み出した値を `0x1ED8:0x1EDA` に加算し、`0xF9A:0xF9C` に設定する。ほかの event-output path にも同型の処理がある。このことから delta/間隔に関係する共通値という見方は強まるが、値・単位・初期化元は未確定。data segment、別名アドレス、間接 store は今回の資料だけでは排除できない。

### 0xF05 / 0x622

`FUN_1000_184A` は `FUN_1000_5EC8(input, 0xF05, 2)` を呼び、一致時に `0x622 = 1` とする。下流で `0x622` が SysEx 出力を抑制する一方、`0x626` が track-order table を選ぶことから、最有力対応は `0x622 ↔ /XD`、`0x626 ↔ /F`。これは静的な用途の整合に基づく推定で、token の実 bytes や参照 MIDI 生成時のオプションは未確認。

### これで進んだ点と限界

- `0x6EE` の単純な直接書き込み探索は、解析テキスト全体に対して実施済み。
- 次に必要なのは逆アセンブル本文以外の初期化データ／文字列領域、または実行時メモリ状態。これらがなければ値・token の実体は確定できない。
- tick 8 の absolute tick への割当はまだ未証明。converter / tests は変更しない。


## 参照 MIDI の SysEx tick と 0x6EE 候補（2026-10-21）

`runner.MID` の SMF bytes を直接解析し、tempo meta と track 1 SysEx の絶対 tick を再確認した。

- SMF header は format 1、12 tracks、division 48。
- conductor track の tempo meta は tick 0 / 8 / 192（500,000 / 869,000 / 869,565 µs/quarter）。
- track 1 の SysEx は4件で tick 0 / 4 / 8 / 8。payload length は 12 / 12 / 10 / 10 bytes。
- `FUN_1000_1CE4` は条件成立時に2回 queue event を drain し、各 drain 後に `0x6EE` を `0x1ED8:0x1EDA` に加算、`0xF9A:0xF9C` に設定する。この2回の drain が最初の2件の SysEx（tick 0 / 4）に対応するなら `0x6EE = 4` が候補となり、2回目の後に残る delta 4 は次の event を tick 8 に進め得る。
- ただし、queue payload と参照 SysEx payload の対応は未確認。track 1 event の時刻観測だけでは、conductor track の tempo event が同じ経路・累積時刻で出力される因果関係までは証明できない。`0x622` の実行時値も不明。

**現在の評価:** `0x6EE = 4` は具体的で検証可能な仮説になったが、確認済み事実ではない。次は `0xF34` / `0xF3F` の queue payload を参照 SysEx に対応付け、queue drain 前後の accumulator を追う。runtime trace ができなければ、仮説と事実を分けたまま維持する。


## オプションフラグ対応の再評価（2026-10-21）

既存の下流制御フローを横断して見直した結果、以前の `0x622 ↔ /F` 仮説は撤回する。`FUN_1000_08EC` では `0x622 != 0` が SysEx payload の出力抑制に使われ、`FUN_1000_0010` では `0x626` が track-order table の選択に使われる。SNG2S の help text は `/XD` を Exclusive disable、`/F` を part-to-track order change と説明するため、現在の最有力対応は `0x622 ↔ /XD`、`0x626 ↔ /F` である（FACTS F-069）。

この再評価により、`0x622 != 0` を `/F` の効果として説明する旧記述は採用しない。参照 MIDI に SysEx が4件あるため `/XD` が指定されていなかった可能性は高いが、実際の起動引数・実行時フラグ値は未確認。したがって tick 8 の pending-delta 論は依然として静的推定であり、converter / tests は変更しない。

## Update: current findings and remaining proof gaps (2026-10-22)

### Findings now supported by static control flow

- The two non-default tempo values are generated by **different integer paths** from the same normalized tempo of 69 BPM: the tempo-record path uses `floor(60,000 / 69) × 1,000 = 869,000`; the bar/grid path uses `floor(60,000,000 / 69) = 869,565`. These are not evidence of two different musical BPM settings.
- The source bytes at SNG offset `0x00B5` begin `C0 00 04 04`; the grid operand is 192 ticks. The grid-alignment code therefore explains the exact-tempo value associated with the next 192-tick boundary. It does **not** explain the earlier coarse event.
- `FUN_1000_3406` queues the coarse 869,000 value. The reference MIDI places it at tick 8, but the pending-delta / absolute-tick state that causes that placement is not yet proven.
- The first `FUN_1000_3406` call is an exception path: initialization sets `0xF96 = 0xFFFF`, and the caller resets it only after that call. Do not apply the normal scheduler's pending-delta `+1` rule to this first call.
- `FUN_1000_1CE4` has conditional queue drains and assignments from `0x6EE`. The possible `0x6EE = 4` explanation is consistent with the first two reference SysEx events at ticks 0 and 4, but payloads and accumulator states are not mapped one-to-one; this remains a hypothesis.

### Separate issue: MIMPIV4 playback

The MIMPIV4 loader initializes its timer-period parameter from the legacy conductor/header structure, and the timer driver uses that parameter. The complete available export was also checked at `FUN_1000_341E` (special-record payload scan), `FUN_1000_3543` (event/duration pre-scan), and `FUN_1000_2D27` (Program Change rewrite); these bodies do not show the binary SNG tempo-record value being passed to the timer globals. `FUN_1000_8888` schedules a parsed text-command delay plus `0x0440`, but its connection to the binary SNG tempo table is unproven. The actual playback consumer remains unidentified. This narrows the static search but does not prove tempo records are ignored; next evidence is the playback event-dispatch path or a runtime trace of timer/scheduler state around a known tempo record. Do not infer that MIMPIV4 playback uses SNG2S's MIDI-export conversion curve.

### Still open

1. Establish the initialized value and source of `0x6EE`, including data-segment and indirect-write possibilities.
2. Map the two setup-template paths to the exact SysEx payloads, or explicitly retain the missing initialized bytes as the evidence limit.
3. Recover the option token/string mapping and actual SNG2S command line for the reference MIDI; the manual's defaults do not prove they were used.
4. Prove the pending delta and absolute tick at the drain that emits the coarse tempo event at tick 8. No runtime trace has been performed.
5. Keep the v1 converter's tempo guard and tests unchanged until those gaps are closed; do not claim full MIDI equivalence based only on the 3,295 Note-on/off matches.

## Follow-up audit (2026-10-22): narrow the remaining tick-8 question

The static trace has progressed beyond identifying candidate tempo producers:

- The coarse value `869000` is produced by the tempo-record path; its arithmetic is no longer the open question.
- The exact value `869565` and its 192-tick grid alignment are supported by the static path and match the reference event at tick 192.
- The remaining tempo question is specifically **why the coarse event is serialized at absolute tick 8**. The needed evidence is the queue/pending-delta state immediately before and after the first tempo queue drain, including `0xF9A:0xF9C`, `0x6F2:0x6F4`, and preceding setup events.
- The first `FUN_1000_3406` call takes an initialization exception path because `0xF96` starts at `0xFFFF`; it should not be reasoned about using the normal scheduler's later-call delta adjustment.
- `0x6EE` is read and added into pending timing in setup-output paths, but the exported text disassembly omits initialized data bytes. Its value is therefore not established by the current static export; interpreting it as 4 ticks remains speculative.

This narrows the next useful experiment to a runtime trace or a recovered initialized data segment. Re-reading the 192-tick grid calculation will not resolve the tick-8 placement. No converter behavior is changed by this finding.


## Follow-up audit 2 (2026-10-22): what the 0x6EE references actually establish

A full text search of the available SNG2S disassembly export finds repeated **reads** of `0x6EE` in setup/preamble paths, including `FUN_1000_0010`, `FUN_1000_1CE4`, `FUN_1000_1E6C`, and related setup emitters. Each observed use adds that word to the internal absolute accumulator at `0x1ED8:0x1EDA` and assigns it as the next pending delta at `0xF9A:0xF9C`. The text export contains no direct store to `0x6EE`, so its initial value is not recoverable from this export alone.

`FUN_1000_08EC` drains queued MIDI-like events by converting the pending delta to a variable-length quantity, then advances the accumulated output tick (`0x6F2:0x6F4`) and clears the pending delta. This confirms the role of the *pending-delta/absolute-tick machinery*, but it still does not reveal the initialized value of `0x6EE` or prove that it equals 4. The observed reference SysEx ticks 0/4/8 are compatible with a 4-unit interval, but compatibility is not proof.

**Revised conclusion:** `0x6EE` is a shared setup-event spacing value in the SNG2S output scheduler; its numeric value and the exact mapping from each setup emission to the reference SysEx events remain unknown. Do not hard-code 4 or change converter timing from this evidence alone.


## Follow-up 3 (2026-10-22): tempo arithmetic is resolved, tick placement is not

`FUN_1000_3406` reads tempo records from `0x21E0` and computes the normalized tempo from the record value and base tempo at `0x2962`, divided by 64 and clamped to 1–480. At base tempo 120, record value 37 yields 69 BPM. The numeric conversion is therefore supported by the static trace.

`FUN_1000_3DE2` initializes `0xF96` to `0xFFFF`; `FUN_1000_33D4` calls `FUN_1000_3406` and resets `0xF96` only after that call. This confirms the first-call exception to the later pending-delta increment path.

The remaining issue is narrower: why the coarse 869000 µs/qn event is serialized at absolute tick 8. That requires the pending-delta, absolute-tick and preceding queue-drain state. The initialized numeric value of `0x6EE` is still absent from the export, so interpreting it as 4 ticks remains unproven. No converter behavior is changed.

## Cross-check against the MIMPIV4 reference directory (2026-10-22)

The private Ballade directory includes the MIMPIV4 v4.27 manual and a Ghidra export of `MIMPIV4.EXE`, so the player-side timing path can be traced separately from SNG2S's MIDI-export path.

- The manual documents playback tempo adjustment through Up/Down and `/Tn`; it does not specify the binary SNG tempo-record decoding formula.
- Static disassembly shows `FUN_1FF5_0D61` saves and replaces the INT 8 vector and calls `FUN_1FF5_0DFA` using the timing value from `0x0440`. `FUN_1FF5_0DFA` calculates a divisor and writes to I/O ports `0x71` and `0x5F`.
- The loader populates `0x0440` and `0x7BF0` from a byte in a format-dependent record. This identifies a concrete timer initialization path, but not the source of the playback tempo for Ballade SNG records.
- No proven dataflow currently connects the SNG binary tempo-record table to `0x0440` / `0x7BF0`. Do not infer that MIMPIV4 uses the same 869000 / 869565 µs-per-quarter formulas as SNG2S MIDI export.

### Next player-side trace

1. Trace the v1 SNG parser's loaded record and identify the exact field copied to `0x7BF0` / `0x0440`.
2. Search callers and writes around the loaded record for a path that updates the timer input during playback, distinguishing initialization from dynamic tempo changes.
3. Keep this separate from the unresolved SNG2S tick-8 serialization question: the latter concerns queued MIDI meta output and pending delta state, not MIMPI's timer interrupt.
4. No runtime trace has been captured; these findings are static only.

## Validation boundary update (2026-10-23)

Private workflow run [#6](https://github.com/pikachu364/glosa-private-data/actions/runs/39699711256) passed the v1 TEST.SNG tempo-event comparison and runner.SNG Note-event comparison (977 tests total) on Glosa commit `440000b971e72debcef72bb12407bcf4e944527a`. The earlier Note-event failure was a canonical list ordering issue for simultaneous Note-off events at tick 4012, channel 1, pitches 77 and 82; it was fixed in the test comparison ordering, not in tempo conversion. This does not resolve the separate SNG2S tick-8 serialization question, prove SysEx payload equality, or establish runtime playback behavior. Keep `0x6EE = 4` as unproven and do not change tempo conversion on this evidence alone.
