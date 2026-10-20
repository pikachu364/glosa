# v1 tempo investigation

更新日: 2026-10-20

## 目的

testdata/ballade/v1/sng2gs/runner.MID の tempo meta を、SNG2S の入力読込経路から根拠付きで説明する。ここでは converter の推測実装を追加せず、静的解析の事実・仮説・未解決点を分ける。

## 現時点で確定している観測

- 参照 MIDI の tempo meta は tick 0 = 500,000、tick 8 = 869,000、tick 192 = 869,565 microseconds per quarter note。time signature は tick 0 の 4/4。
- 現行 SngConverter.ToSmf は ReadTempi の結果を conductor track に出力する。v1 の ReadTempi 候補は SNG 内 offset 761 の sentinel の後から step/value = 37/144, 37/192 と読むが、後続版向け変換式では参照値と一致しない。
- v1 に対する汎用 ReadTempi の適用はガードされており、現在は誤った候補を出さず、既定 tempo 500,000 のみを出す。参照 MIDI の tick 8 / 192 の変更を再現できているわけではない。
- 静的逆解析では FUN_1000_3406 が内部 far-pointer 0x21DC のレコード列から FF 58 time-signature meta、0x21E0 の列から FF 51 tempo meta を出力する。0x21E4 は別の4-byte列として処理され、0x7DE を更新して FUN_1000_3B78 を呼ぶ。
- 0x21DC / 0x21E0 / 0x21E4 は SNG 内の絶対オフセットではなく、SNG2S 内部バッファの far-pointer。FUN_1000_272a は各ポインタを設定するが、SNG から各バッファへデータを供給するフィールド対応は未確定。
- FUN_1000_618A 周辺には 0x7BA を参照し、60,000,000 / tempo から tempo meta を出す経路が記録されている。ただし、参照 MIDI の tick 8 / 192 のどちらに対応するかは未確定。

## 次に追うデータフロー

1. **書込み先から呼出元へ戻る:** 0x21E0 を読む FUN_1000_3406 のレコード判定・tick 更新を確認し、どの条件で FF 51 が出力されるかを記録する。特に event delta / tick accumulator の更新順を区別する。
2. **バッファの充填元:** FUN_1000_272a 内で 0x21E0 の領域が確保・設定される箇所を特定し、直前の file read / seek がどの長さフィールドと相対 seek 値を使うか確認する。ポインタの代入だけで SNG フィールドと結び付けない。
3. **グローバル tempo の設定元:** 0x7BA への全書込み箇所と、それぞれの呼出元・版分岐・入力値を列挙する。FUN_1000_618A への到達条件と FUN_1000_3406 の 0x21E0 レコード処理を別経路として扱う。
4. **値から参照 MIDI への対応:** 候補レコードの raw value、出力時の tick、計算後の microseconds-per-quarter を表にし、tick 0 / 8 / 192 の3イベントと全て整合する場合だけ対応を採用する。
5. **実装ゲート:** SNG 内データと出力 tick の関係を説明できるまでは SngConverter.cs を変更しない。静的解析結果は実行時トレースや実再生の証明と混同しない。

## 現時点の結論

v1 の tempo meta は未解決。確定しているのは、SNG2S に少なくとも内部 tempo-record buffer と 0x7BA を使う別の tempo 出力経路があること、および現行の汎用 parser が参照 MIDI と一致しないことまで。runner.MID の tick 8 / 192 のイベントを特定の SNG フィールドや特定の内部経路へ割り当てる証拠はまだ不足している。

## 既存記録との関係

- 確認済み事実: FACTS.md F-053, F-055, F-056
- 未解決仮説: HYPOTHESES.md H-012
- タスクリスト: NEXT_STEPS.md T-003

この文書は調査順序を整理するもので、新しい変換仕様を確定するものではない。
