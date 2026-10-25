# Ballade SNG 逆解析資料

## 目的
Ballade SNG の版別レイアウトと Glosa の変換結果を、参照 MIDI・実装・テスト・静的解析の根拠に沿って整理する。調査項目を増やすことではなく、確認済み事項と未解決事項を区別する。

## 現在の結論
- **v1 レイアウト:** v1 専用のパート構造（サイズ +80、長さフィールド +115/+117、13個の相対 seek 値、ノート開始 +145）を支持する静的根拠があり、別 fixture でも容量条件が一致する。jmplayer の後続版共通検証では候補が失敗する。これは静的照合であり、jmplayer 実行時の再現とは区別する。
- **v1 tempo:** `SngConverter.cs` に `ReadLegacyV1Tempi()` がある。公開テスト `BalladeV1RunnerProducesReferenceTempoEvents()` は参照値 500000 / 869000 / 869565 の byte sequence を確認し、`BalladeV1KeepsDefaultTempoWhenTempoStreamIsUnchanged()` は変化しない tempo stream の既定値維持を確認する。
- **v1 setup SysEx:** 参照 `runner.MID` / `TEST.MID` の tick 0 / 4 の setup events は `FUN_1000_1CE4` の `0xF34` / `0xF3F` templates に対応し、arguments・checksum・terminator・length が一致する（F-097）。tick 8 / 8 の2件も `FUN_1000_33D4 → FUN_1000_3B68 → FUN_1000_3B78 → FUN_1000_3BA7` の producer path と payload 計算を byte-level で照合済み（F-101）。全件とも静的解析と保存済み MIDI の比較であり、runtime trace・実再生は未実施。Glosa 側での再現は未実装。
- **SNG2S の tempo 計算:** raw tempo 37 と base tempo 120 から 69 BPM、粗い経路の 869000 µs/qn と grid 経路の 869565 µs/qn を静的解析で説明できる。元の EXE の初期化済み data image から `0x6EE = 4` を確認し、startup call order と pending delta の静的追跡で、参照 fixture の coarse tempo event が tick 8 に出る仕組みを説明できる（F-094/F-095）。runtime trace と全 v1 ファイルへの一般化は未確認。
- **grid offset:** 192-tick grid と 4/4 は解析対象だが、絶対 offset は記録間で不一致（0x00B5=181、231、0x74+125=241）。未解決として扱い、根拠なく統一しない。
- **MIMPIV4:** timer 初期化経路は確認済みだが、SNG tempo-record の値が再生中の timer / event interval に入る経路は未特定。SNG2S の MIDI export 式を流用しない。
- **v2 setup SysEx:** 静的解析と参照 MIDI の照合で、tick 4/8 の `FUN_1000_23C0(4)` 経路と tick 100/104 の `FUN_1000_1CE4` 経路を別々に説明できる（F-093 / H-019 / T-018）。`0x7BC` の実行時状態や間接書き込みの完全排除、runtime trace は未確認。
- **v2–v4 tempo:** raw SNG tempo values were compared with SNG2S reference MIDI (F-105). v3/v4 coarse values match whole-millisecond conversion while exact values match integer µs/qn conversion, but the first coarse event and later one-tick-early events are not explained by one proven scheduler rule. v2 remains a separate unresolved path. Do not treat the hypothesis as implemented behavior.

## 次の作業
未完了作業の正本は [NEXT_STEPS.md](NEXT_STEPS.md) の T-018（v2 setup parameter override flags / SysEx payload mapping）、T-017（v1 tick-8 の runtime state / fixture generalization、setup SysEx mapping、参照 MIDI の生成オプション）、T-014（3/4 固有丸め）、T-015（v2 tempo audit）、T-016（MIMPIV4 再生 tempo）とする。T-011〜T-013 は完了した静的解析の記録として扱い、同じ作業を重複させない。

## 文書の役割
1. [FACTS.md](FACTS.md) — 根拠付きの確認事項。事実 ID の正本。
2. [HYPOTHESES.md](HYPOTHESES.md) — 未確定の仮説、反証状況、必要な確認。
3. [NEXT_STEPS.md](NEXT_STEPS.md) — 実行する未完了作業と完了条件の正本。
4. [V1_TEMPO_INVESTIGATION.md](V1_TEMPO_INVESTIGATION.md) — v1 tempo の詳細。
5. [BALLADE_LAYOUT_COMPARISON_2026-10-21.md](BALLADE_LAYOUT_COMPARISON_2026-10-21.md) — レイアウト比較と履歴。
6. [LOG.md](LOG.md) — 時系列の調査履歴。旧仮説は現状の根拠として扱わない。
7. [引継ぎ文書_20261022.txt](引継ぎ文書_20261022.txt) — 現状と次の作業の引継ぎ。

## 検証上の制約
公開テスト、private fixture CI、静的解析、実行時トレース、実再生は別の検証段階である。private workflow の成功は、その run が checkout した commit と実行したテスト範囲に限る。Note-event signature や tempo byte sequence の一致を、SMF 全体の byte-for-byte 一致、SysEx payload の一致、または再生結果の完全一致と言い換えない。

private fixture と逆解析 dump は private repository 内に保持し、この公開 repository に追加しない。PR #9 は明示的な許可なしに merge しない。
