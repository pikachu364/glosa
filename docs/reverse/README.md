# SNG → MIDI 逆解析ノート

このディレクトリは、Ballade SNG と SNG2S/SNG2GS の調査情報を、長い作業ログから切り離して整理するための入口です。

## 現在の進捗（2026-10-23）

**最優先:** Ballade v1 の tempo meta（tick 8 / 192）の出力時刻を SNG2S の処理経路と対応付ける。

- tempo の数値計算と初回 scheduler の例外経路は静的解析で説明できている。`FUN_1000_1CE4` / `FUN_1000_3BA7` は参照 SysEx を生成し得る経路だが、payload の byte-for-byte 対応は未確認。`0x6EE = 4` は後続調査で未証明と確認され、実値・初期化元・tick 8 の割当はいずれも未解決。
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


## Private fixture CI（重要）

`pikachu364/glosa-private-data/.github/private-tests/PrivateBalladeFixtureTests.cs` は、private repo の Ballade 実データを用いて Glosa の変換結果を検証する専用テストです。公開リポジトリに fixture を置かず、private repo 側の [private-data-ci.yml](https://github.com/pikachu364/glosa-private-data/blob/main/.github/workflows/private-data-ci.yml) が Glosa の指定 ref を checkout し、SNG/MID fixture とテストコードを一時的に配置して build/test します。

- `PrivateV1TestSngKeepsReferenceTempoEvents`: v1 `TEST.SNG` と `TEST.MID` の tempo events を track/tick/µs-per-quarter で比較。
- `PrivateV1RunnerMatchesReferenceNoteEvents`: v1 `runner.SNG` と `runner.MID` の Note-on/off を tick/channel/pitch/velocity で比較。同 tick のトラック間順序に依存しないよう canonical order で比較。
- private workflow の `glosa_ref` を確認し、検証したブランチ/commit と run 結果を記録する。公開 CI と private fixture CI は別の検証層として扱う。
- 現状の2テストが保証するのは v1 tempo と note events。SysEx payload、物理 track/chunk order、controller/program/pitch bend、v2-v4 全体の正しさまで保証すると解釈しない。
- private fixture と解析ダンプは公開リポジトリへコピーしない。

## MIMPIV4 playback tempo investigation

Static analysis identifies the driver-dispatch and INT 8 timer initialization path, but does **not** yet connect the binary SNG tempo-record value to a playback-time timer update. Treat the player-side tempo formula as unresolved; do not reuse SNG2S MIDI-export formulas. See [FACTS.md](FACTS.md) F-086 and [NEXT_STEPS.md](NEXT_STEPS.md).

## 2026-10-23 — PR / CI 状態の再確認

- PR #10（停止時無音時間テストの CI scheduling jitter 対策）は merge 済み。merge commit: `6e99e68aa30a91f508c816b49cb8d7d214a4c5e6`。変更は `TheSilenceOnStoppingKeepsToTheCap` の上限を 200 ms から 300 ms に広げるテストのみで、再生・MIDI pacing の本体コードは変更していない。
- PR #9（v1 legacy layout）は引き続き open / unmerged。GitHub PR metadata で head `3463baf2f59c2e31fc76c00846e88ac5e713f2b4`、公開 CI run #815（run ID `39867006220`）は `success` と確認。これは private reference 比較の失敗を解消したことを意味しない。
- PR #9 branch は `main` と diverged し、`main` より 2 commits behind / 102 commits ahead。base `main` の SHA は PR #10 merge commit `6e99e68aa30a91f508c816b49cb8d7d214a4c5e6`。branch への取り込みは private fixture discrepancy の原因特定と差分確認後に判断する。
- private fixture validation では TEST.SNG tempo 比較は成功した一方、runner.SNG note-event comparison は失敗した記録がある。原因が track mapping だけだと決めつけず、track/channel/tick/pitch/velocity の差分を分離するまで PR #9 を merge しない。
- 現在の次の実作業は、private runner 比較の失敗を診断し、公開 CI 成功と private fixture 互換性を別々に評価すること。private fixture / 解析ダンプは公開リポジトリへコピーしない。

