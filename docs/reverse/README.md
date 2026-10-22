# SNG → MIDI 逆解析ノート

このディレクトリは、Ballade SNG と SNG2S/SNG2GS の調査情報を、長い作業ログから切り離して整理するための入口です。

## 現在の進捗（2026-10-22）

**最優先:** Ballade v1 の tempo meta（tick 8 / 192）の出力時刻を SNG2S の処理経路と対応付ける。

- 数値計算と初回 scheduler の例外経路は静的解析で説明できている。参照 track 1 の SysEx payload と checksum は `FUN_1000_1CE4` / `FUN_1000_3BA7` の生成経路に強く一致し、tick 0 / 4 の並びから `0x6EE = 4` が有力な静的推定となった。ただし実行時値と tick 8 の割当は未検証。
- 初回 scheduler の例外経路は確認済み。0x6EE の全29命令参照は直接読み出しで、複数の出力経路で delta/間隔値として使われるが、値・初期化元は未解決。
- オプションフラグの最有力対応は `0x622 ↔ /XD`（SysEx 抑制）、`0x626 ↔ /F`（track-order table 選択）。旧 `0x622 ↔ /F` 仮説は撤回。token bytes と参照生成時の引数は未確認（FACTS F-069）。
- `HYPOTHESES.md` の H-015 correction note で旧対応を明示的に撤回済み。履歴ログ中の古い記述は当時の記録として残し、現行結論は訂正ノートを優先する。
- SNG2S の tempo-record 数値計算と 192-tick grid 経路は静的解析で説明できた。tick 8 の pending-delta / absolute-tick 状態、`0x6EE` の初期値、SysEx payload 対応は未解決。
- converter / regression tests はこの調査更新では未変更。SNG2S の実行時トレースと実再生は未実施。
- まず [NEXT_STEPS.md](NEXT_STEPS.md) の進捗サマリーと優先順を確認してください。

## 読む順番
1. [FACTS.md](FACTS.md) — 根拠が確認できている事実
2. [HYPOTHESES.md](HYPOTHESES.md) — 有力候補・未確定事項
3. [NEXT_STEPS.md](NEXT_STEPS.md) — 次の調査と完了条件
4. [LOG.md](LOG.md) — 日付順の調査履歴
5. [旧・詳細引き継ぎ](../DEVELOPMENT_HANDOFF.md) — 詳細な経緯と過去のメモ

## 記録ルール
- **確認済み**: 根拠となるソース、オフセット、データ比較を記す。
- **一部確認**: 一部の条件やサンプルでのみ確認できた範囲を明示する。
- **仮説**: 事実と同じ段落に混ぜず、反証・検証方法を添える。
- **未確認**: バイナリがない、実行していない、参照出力条件が不明などの制約を明記する。
- 静的解析、テスト実行、MIDI 再生を別の検証段階として扱う。
- SngConverter.cs は、制御フローと参照 MIDI のイベント単位の検証が揃うまで変更しない。
- SNG パート番号、内部スケジューラスロット、MIDI トラック番号を同一視しない。

項目には安定した ID（例: F-001, H-001, T-001）を付け、内容を移動しても参照できるようにします。
