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
- 0x622 は option token によって設定され、/F の track-order 変更というマニュアル説明と下流利用が整合する。ただし token の実文字列が未確認なので、これは有力な推定に留める。

### まだ証明できていないこと

- tempo-record path の 869000 が参照 MIDI の tick 8 で出力されること（数値の一致だけでは時刻の証明にならない）。
- 0x6EE の実値と初期化元、preamble queue drain 前後の pending delta / absolute tick。
- 参照 MIDI 生成時に /F が指定されたか、したがって 0x622 の分岐が実際に有効だったか。
- grid path が参照 MIDI の tick 192 event を直接生成したという因果関係の runtime 確認。

### 次の調査順

1. 0x6EE の初期値・書き込み元をデータ領域と間接書き込みも含めて探す。
2. 0xF05 の option token の実体を含むバイナリ／データダンプを探し、0x622 と /F の対応を確認する。
3. preamble の queue drain ごとに 0xF9A:0xF9C と 0x6F2:0x6F4 を表にして、参照 tick と照合する。
4. runtime trace ができなければ、その制約を維持し、converter の tempo 規則を推測で変更しない。

**実装ステータス:** v1 ReadTempi guard 維持。converter / regression tests に変更なし。静的解析のみで、runtime trace と実再生は未実施。


## 0x6EE の命令参照スキャンと option parser の追加確認（2026-10-21）

### 0x6EE

チェックイン済み逆解析テキストの命令行を全走査したところ、`[0x6EE]` の直接参照は29箇所で、いずれも `MOV AX,[0x6EE]` の読み出しだった。直接 store は見つからない。`FUN_1000_1CE4` の queue drain 後は、読み出した値を `0x1ED8:0x1EDA` に加算し、`0xF9A:0xF9C` に設定する。ほかの event-output path にも同型の処理がある。このことから delta/間隔に関係する共通値という見方は強まるが、値・単位・初期化元は未確定。data segment、別名アドレス、間接 store は今回の資料だけでは排除できない。

### 0xF05 / 0x622

`FUN_1000_184A` は `FUN_1000_5EC8(input, 0xF05, 2)` を呼び、一致時に `0x622 = 1` とする。`FUN_1000_5EC8` は2-byteの文字列比較であることを命令列から確認した。ただし文字列テーブルの実体は資料にないため、token が /F であること、参照 MIDI 生成時に指定されたことは依然未確認。

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
