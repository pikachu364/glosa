# v1 tempo investigation

更新日: 2026-10-21

## 目的

testdata/ballade/v1/sng2gs/runner.MID の tempo meta を、SNG2S の入力読込経路から根拠付きで説明する。ここでは converter の推測実装を追加せず、静的解析の事実・仮説・未解決点を分ける。

## 現時点で確定している観測

- 参照 MIDI の tempo meta は tick 0 = 500,000、tick 8 = 869,000、tick 192 = 869,565 microseconds per quarter note。time signature は tick 0 の 4/4。
- 現行 SngConverter.ToSmf は ReadTempi の結果を conductor track に出力する。v1 の ReadTempi 候補は SNG 内 offset 761 の sentinel の後から step/value = 37/144, 37/192 と読むが、後続版向け変換式では参照値と一致しない。
- v1 に対する汎用 ReadTempi の適用はガードされており、現在は誤った候補を出さず、既定 tempo 500,000 のみを出す。参照 MIDI の tick 8 / 192 の変更を再現できているわけではない。
- 静的逆解析では FUN_1000_3406 が内部 far-pointer 0x21DC のレコード列から FF 58 time-signature meta、0x21E0 の列から tempo 値を処理する。0x21E4 は別の4-byte列として処理され、0x7DE を更新して FUN_1000_3B78 を呼ぶ。
- 0x21DC / 0x21E0 / 0x21E4 は SNG 内の絶対オフセットではなく、SNG2S 内部バッファの far-pointer。FUN_1000_272a は local_6c byte の buffer を確保し、seek position local_4 + local_6 から DOS read (0x3F00) で読み込んだ pointer を 0x21E0 に設定する。local_4 / local_6 / local_6c と v1 SNG 内の具体的フィールド対応は未確定。
- FUN_1000_3406 は 0x21E0 の4-byteレコード第2 word を ((raw * 0x2962) & 0xFFFF) / 0x40 で正規化して1〜480に制限し、変化時に 0x7BA を更新する。起動時には 0x7BA != 0x78 の場合、FUN_1000_618A で 60,000,000 / 0x7BA の整数除算を行い FF 51 03 tempo meta を出力する。tick 8 / 192 の参照イベントとの対応は未確定。

## runner.SNG 実データとの追加照合（2026-10-21）

`FUN_1000_272a` の read sequence と `runner.SNG` の実バイト列を照合した。

- 固定長の順次 read (25 + 35 + 10 + 44 + 1 + 66 bytes) の後、file offset は `0x00B5`。ここから `local_3a = 590 (0x24E)` bytes を読み、次に `local_6c = 352 (0x160)` bytes を読むため、`0x21DC` の source は `0x00B5`、`0x21E0` の source は `0x0303` となる。
- その後 file pointer は `0x0463`。relative seek `local_4 + local_6 = 20 + 16 = 36` bytes で `0x0487` に移動し、`0x21E4` 用の別レコード列を読む。
- `0x21E0` の先頭レコード列は `(48,37)`, `(144,37)`, `(192,37)` の反復で、終端は `(0x8702,0xFFFF)`。第2 word 37 は変換式で 69 BPM 相当、すなわち `869,565` µs/quarter になる。
- `FUN_1000_3406` は record の first word を `0x10AA = first_word - 1` としてカウントダウンに使い、record pointer `0x10AC` を4進める。よって 48 / 144 / 192 を MIDI tick そのものと扱うのは誤り。tick 8 / 192 との対応には別途、カウントダウン周期と出力 delta-time の関係を解明する必要がある。
- 数値 869,565 は参照 MIDI の tick 192 イベントと一致するが、それだけでは同じイベント由来とは証明できない。tick 8 の 869,000 の生成元も未特定。

## scheduler の countdown と delta-time の関係（2026-10-21）

解析資料の呼出し経路を追加で追跡した。

- エントリ関数は初期 tempo meta 出力後に FUN_1000_3406 と FUN_1000_08EC を反復する。
- FUN_1000_3406 の通常経路は pending delta-time accumulator 0xF9A:0xF9C を +1 する。FUN_1000_08EC はイベント出力時に FUN_1000_16E6 で accumulator の値を書き出し、accumulator をクリアする。
- 0x10AA countdown も FUN_1000_3406 内で減算され、0 になると 0x21E0 の次 record を消費する。first word は単純な MIDI tick 値ではなく、出力 scheduler の反復と関係するカウント値と考えるべき。
- ただし例外経路があり、毎回の呼出しが同じように accumulator を進めるとは断定できない。tempo record が処理された時点の pending delta-time と参照 MIDI イベント時刻を直接結ぶ追跡は未完了。

## 次に追うデータフロー

1. **レコードの意味を切り分ける:** FUN_1000_3406 が 0x21E0 の各4-byteレコードから first word / second word をどう消費し、カウンタ 0x10AA / offset 0x10AC が次レコードと tick をどう進めるか整理する。現状は第2 word の tempo 値正規化と 0x7BA 更新まで確認済み。
2. **バッファの充填元を追う:** FUN_1000_272a の local_6c (read length)、local_4 + local_6 (seek position)、0x1Bxx メタデータの版別代入を追う。0x21E0 は確保済み領域への file read で埋まるが、v1 SNG 内の具体的フィールドは未確定。
3. **0x7BA の全書込みを分類する:** 初期値設定、0x21E0 レコード消費時の更新、起動時 FF 51 出力条件を区別する。FUN_1000_618A は 60,000,000 / 0x7BA の整数除算に使われる。
4. **参照 MIDI と照合する:** レコードの raw words、レコード消費タイミング、累積 tick、正規化後の 0x7BA、出力 tempo bytes を対応表にし、tick 0 / 8 / 192 の全イベントと整合する場合だけ mapping を採用する。
5. **実装ゲート:** SNG 内データと出力 tick の関係を説明できるまでは SngConverter.cs を変更しない。静的解析結果は実行時トレースや実再生の証明と混同しない。

## 現時点の結論

v1 の tempo meta は未解決。確定しているのは、SNG2S に少なくとも内部 tempo-record buffer と 0x7BA を使う別の tempo 出力経路があること、および現行の汎用 parser が参照 MIDI と一致しないことまで。runner.MID の tick 8 / 192 のイベントを特定の SNG フィールドや特定の内部経路へ割り当てる証拠はまだ不足している。

## 既存記録との関係

- 確認済み事実: FACTS.md F-053, F-055, F-056, F-058
- 未解決仮説: HYPOTHESES.md H-012
- タスクリスト: NEXT_STEPS.md T-003

この文書は調査順序を整理するもので、新しい変換仕様を確定するものではない。


## Follow-up: separate table path

Static disassembly shows that the 0x21E4 table is not the tempo table. Its records update 0x7DE; when this value changes, FUN_1000_3B78/FUN_1000_3BA7 queue device-parameter SysEx messages. The tempo records are read from 0x21E0 and produce FF 51 meta events by a separate path. This distinction explains why queue contents around the first tempo record can include non-tempo events, but it does not yet determine the tempo events' final ticks. Source bytes at offset 0x0487 contain records with second word 75 (0x004B); the table and its effects are static-analysis findings only.


## Stronger mapping: grid-aligned tempo output

The entry path calls FUN_1000_33D4 and then drains its queue with FUN_1000_08EC. It next calls FUN_1000_61EC with the current absolute tick and grid length (192 in this sample), stores the distance to the next boundary in pending delta 0xF9A:0xF9C, and checks 0x7BA. If 0x7BA is not 120, it calls FUN_1000_618A with the 60,000,000 constant and 0x7BA, flushes the pending delta through FUN_1000_16E6, clears the pending delta, and writes FF 51 03 with the computed tempo. In this sample 0x7BA is 69, so this path emits 869,565 us/quarter at the next 192-tick boundary, matching the reference tick-192 tempo. This is strong static evidence for that event path, but tick 8 with 869,000 remains unresolved.
