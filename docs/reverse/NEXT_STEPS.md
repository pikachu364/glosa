# 逆解析の進捗サマリー（2026-10-25 更新）

## 現在地

> **作業境界:** T-017 の v1 SNG2S coarse tick-8 tempo と setup SysEx の静的対応付けは、保存済み reference MIDI に対する byte-level 比較まで進んだ。残るのは runtime state の確認と全 v1 ファイルへの一般化。別件の T-014（3/4 rounding）、T-015（v2 tempo audit）、T-016（MIMPIV4 playback tempo）は混同せず継続する。

- **T-017 の進展:** 元 EXE initialized image から `DS:0x06EE = 4`、`DS:0x07DE = 0x004B`、`CS:0x3F82 = F0 41 10 16 12 FF` を確認。静的 call order と reference MIDI を照合し、tick 0/4 の 12-byte SysEx 2件、tick 8 の 10-byte SysEx 2件すべてを producer path / payload と対応付けた（FACTS F-094/F-095/F-097/F-101）。
- **tempo event:** `runner.MID` の tempo は tick 0 = 500,000、tick 8 = 869,000、tick 192 = 869,565 µs/quarter。`TEST.MID` は tick 0 の 500,000 のみ。coarse tempo の tick 8 placement は、setup drains の後に pending delta 4 が残る startup path と整合する。tick 192 の exact tempo は別の 192-tick grid path（FACTS F-072/F-080/F-095）。
- **v2〜v4 saved MIDI audit (2026-10-25):** reference と保存済み Glosa MIDI をバイナリ解析し、各版で Glosa 側が物理 track 1本少ないこと、reference track index 1 に独立 SysEx track があること、パート順が逆方向であること、tempo event に差があることを記録（比較文書 §20）。これは保存済み旧出力の比較であり、現行コードからの再生成結果ではない。次は private fixture workflow で現行 PR head を指定して再検証し、現行挙動と旧生成物の差を切り分ける。
- **オプションフラグ:** parser の文字列と分岐から `/XD → 0x622 = 1` は直接確認済み（F-096）。reference MIDI の生成時に `/XD` が指定されたかは不明だが、ユーザー確認では生成コマンドにオプションなし。`/F` token-to-flag の直接対応は未確認。
- **残る制約:** 解析は original EXE initialized image、静的命令列、保存済み MIDI の照合であり、runtime trace / 実再生は未実施。indirect writes と inspected fixture 以外への一般化は未証明。converter と tempo regression tests は変更しない。
## reverse 文書全体の監査（2026-10-22）

- 対象: README、FACTS、HYPOTHESES、LOG、BALLADE_LAYOUT_COMPARISON、V1_TEMPO_INVESTIGATION、引継ぎ文書。
- 古い調査メモは履歴として残し、現在の結論・TODO と混同しない。過去ログにある option flag の旧対応や `0x6EE = 4` は、後続の訂正・制約を優先する。
- FACTS の重複 ID は F-089〜F-092 に整理し、参照先も更新した。
- 節番号の重複を比較文書で修正。コード、テスト、private fixture はこの文書監査では変更していない。

---

## 調査項目の整理（2026-10-22）

このファイルには、完了した解析履歴と現在の未解決事項が混在している。重複した TODO を別々に再調査しないため、以後は次の整理に従う。

- **現在の最優先 TODO の正本:** T-017。残件は (1) 初回 coarse tempo が tick 8 に出る直前の pending delta / absolute tick の根拠、(2) `0x6EE` の初期値・設定元、(3) setup SysEx 4件と各 producer の payload 対応、(4) 参照 MIDI を生成した実際の SNG2S オプション。
- **重複する観点:** T-011 は grid と queue drain の全体像、T-012 は tempo/device queue の分離、T-013 は preamble と初回 drain の状態を記録している。これらの完了済みチェックをやり直さず、残件は T-017 の上記4点に集約して進める。個々の過去の根拠・解析ログは削除せず保持する。
- **別件として維持:** T-014 の 3/4 固有丸め、T-015 の v2 tempo-stream audit、T-016 の MIMPIV4 再生タイマー経路は、tick-8 queue gap と同一視しない。
- **実装/検証の境界:** PR #9 の説明にある tempo 値の出力は現実装と参照 MIDI の一致を指す。一方、初回イベントが tick 8 になる内部 delta の完全な説明や、任意の v1 ファイルへの一般化は未証明。これらを「実装が未対応」と「内部根拠が未確定」で混同しない。

---

## 次に行うこと（順序）

1. [x] 解析テキスト内の `0x6EE` 命令参照を全件確認。29参照はすべて直接読み出しで、直接 store は見つからず、複数出力経路で delta/間隔に使われることを記録（FACTS F-067 / H-016）。
2. [ ] `0x6EE` の実値・初期化元を、実行ファイルの data segment／別名アドレス／間接書き込みまで含めて調べる。
3. [ ] parser token `0xF05` の文字列実体とデータ segment 対応を確認し、`0x622 ↔ /XD` を直接検証する。`0x626 ↔ /F` も token bytes または runtime trace で確認する。
4. [ ] reference MIDI を生成した SNG2S の実行条件が分かる資料を探し、`/XD` の有無（従って `0x622` の実際の値）を絞る。
5. [ ] `FUN_1000_1CE4` の `0xF34` / `0xF3F` queue payload を参照 track 1 の SysEx 4件に対応付け、`0x6EE = 4` 仮説を検証する（FACTS F-068 / H-016）。
6. [ ] preamble の各 queue drain 前後について `0xF9A:0xF9C` と `0x6F2:0x6F4` を対応表にし、tick 0 / 8 / 192 と照合する。
7. [ ] 実行時トレースが可能なら静的推論と照合する。可能でなければ未確認と明記する。
8. [ ] tempo 問題が解決するまで converter / regression tests を変更しない。
---

# 次の調査タスク

優先順位順。完了条件を満たすまでは、実装変更を先行させません。

## T-001 — Tie Note-off 対応表を作る
**優先度:** 高  
**目的:** SHIZUME0 の同音高重複を含む Note-off の対応を明確化する。

- [x] v2/v3 の Note-on 数が非 Tie レコード数と一致することを確認した。
- [x] トラック/チャンネルを無視したイベント比較で、v2 の160 ticks補正後と v3 の補正なしでは参照 MIDI の Note-on/off がすべて Glosa 側に含まれることを確認した。
- [x] v2/v3 の SNG 通常ノート数/Tie 数と参照 MIDI の Note-on/off 件数を照合（v2: 通常 2,713 / Tie 1,070、v3: 通常 5,362 / Tie 357）。参照 Note-on/off 数もそれぞれ一致し、同音高の最新 Tie 候補は両方で一意だった。詳細は FACTS F-032。
- [x] Tie 開始 tick と選択候補の予定 Note-off tick をイベント単位で照合。v2 は 1,070 Tie 中 1,021 件が tick 一致、49 件が 4〜33 ticks の gap。v3 は 357 Tie 中 353 件が一致、4 件が 1〜3 ticks の gap。オフラインモデルの Note-on/off 署名は参照 MIDI と v2 5,426 件 / v3 10,724 件で差分 0 件（FACTS F-032）。
- [x] v2/v3 参照 MIDI のトラック内走査で同音高 Note-on の重なり 0 件、同 tick の最新 Tie 候補の競合 0 件を確認。実データは合成 fixture の同 tick 候補 tie-break を直接検証できない。
- [ ] FIFO/LIFO を正解と仮定せず、Note-off の所有ノートと例外を特定する。

**完了条件:** イベント規則のオフライン照合は完了。CI と修正後の実出力比較を確認してクローズする。

## T-002 — v1 レイアウト候補を独立検証する
**優先度:** 高

- [x] `runner.SNG` の header end = tableEnd + 2 と part size = entries[p] + 80 の候補を全10パートで検査。SNG2S の旧版 read 順から、情報バッファ +65、note length +115、controller length +117、record 列 +145 が対応することを確認（FACTS F-038）。以前の +161 record-start と +119 を追加列長とする解釈は撤回。
- [x] 初期実装では `Invalid or unsupported Ballade SNG layout.` となった。この制約は後続の v1 専用経路で解消済み（F-045）。ここは履歴として残し、現在の対応状況とは区別する。
- [x] 初期調査時に未対応テストを一時除外した。後続で v1 専用変換・参照イベント比較テストを追加し、3 OS CI 成功を確認済み（F-045）。
- [x] SNG2S の `FUN_1000_272a` で旧版と後続版のヘッダ項目参照が分岐し、版3でパート数が16になることを確認（FACTS F-034）。これは版別処理の存在を示すが、サイズ表や v1 の具体的オフセット確定には至っていない。
- [x] SNG2S が各パートの 0x50-byte 情報バッファを扱い、旧版クラスと後続版でフィールド参照位置（`0x32` / `0x23`）を切り替えることを確認（FACTS F-035）。
- [x] `FUN_1000_272a` の open/read 経路を追跡し、このファイルは `0x8000` 指定で開かれ、テキスト用 CR/LF・Ctrl-Z 変換が有効でないことを確認。さらに旧版は 80-byte 読込後に追加 byte を読まず、後続版は 1 byte 追加で読むため、固定読込幅が 80 / 81 byte となる（FACTS F-036）。
- [x] 初期静的検査で `+161` を列開始とする容量候補を確認した（FACTS F-037）が、これは F-038 の read順／Note-on照合で撤回済み。`+119` は列長ではなく13個の16-bit値群の先頭。算出終端後の65 byteも未解明。
- [x] `+145` record start で参照 MIDI の全 Note-on 3,295件を pitch/velocity/tick（source +192）まで照合し、差分0を確認（F-038）。後続のタイマー競合モデルで Note-off 3,295件も差分0（F-044）。
- [x] 13個の値が DOS の現在位置基準相対 seek 距離であり、累積位置が全パートで次情報バッファ候補 `+65`、最終パートで EOF に一致することを確認（F-039/F-040）。
- [ ] 13個の seek 値それぞれを、後段で参照されるデータ／バッファへ個別に対応付ける。正式なフィールド仕様は未確定。
- [x] 末尾65 byteは未説明の余剰データではなく、最終パート後の次情報バッファ開始候補が EOF に一致する位置関係として説明済み（F-040）。
- [x] SNG2GS 参照 MIDI とイベント単位で照合。v1 専用経路で Note-on 3,295件と Note-off 3,295件を channel/part・pitch・velocity・tick で照合し、差分0を確認（F-038/F-044）。
- [x] controller/program/pitch-bend の比較で、1,966候補レコードから連続する同一状態を抑制すると参照 MIDI の1,595件と一致することを確認。v1 converter と回帰テストを追加し、CI run [39333267230](https://github.com/pikachu364/glosa/actions/runs/39333267230) が Ubuntu/macOS/Windows で成功（F-052）。
- [ ] tempo/meta event の v1 変換規則は未解決（F-053）。
- [x] 参照 MIDI の物理 track layout を記録するテストを追加。12 track、track-name chunk index、Note-on track の順序/channel/件数、Note-on のない part-1/Keyboard track を検証する（F-054/F-056）。CI run [39400722489](https://github.com/pikachu364/glosa/actions/runs/39400722489) で Ubuntu/macOS/Windows の全 job が成功。
- [ ] 現行 Glosa 出力と参照の物理 chunk 順を比較する。タイトル/conductor track と Exclusive/SysEx track の役割・生成元が未確定のため、part track の並べ替えだけを先行させない。
- [x] 参照 track 1 に Text meta `Exclusive` と tick 0/4/8/8 の SysEx 4件があることを確認し、payload を回帰テストへ記録。CI run [39401367632](https://github.com/pikachu364/glosa/actions/runs/39401367632) で3 OS成功（FACTS F-057）。
- [x] 参照 SysEx 4件を静的 producer path / initialized template / checksum / terminator と照合した（tick 0/4: F-097、tick 8/8: F-101）。reference はオプションなし生成で、`/X2` の documented default と track 1 `Exclusive` は整合する。`/X1`・`/XD` の alternate output は未実行。payload を converter にハードコードする実装判断は、生成条件と version scope の回帰方針が決まるまで保留する。
- [x] v1 専用レイアウト分岐を v2〜v4 と分離し、fixture の参照 Note-on/off 比較テストを追加。作業ブランチ `docs/ballade-version-testdata` の CI run [39253494378](https://github.com/pikachu364/glosa/actions/runs/39253494378) は Ubuntu・macOS・Windows の全 job が成功。

**完了条件:** 候補がソース解析または参照 MIDI と一致する。整合性だけを根拠に実装へ反映しない。

## T-003 — 参照 MIDI の生成条件を確認する（残件は T-017 に集約）

- [x] リポジトリ内の `SNG2S.DOC` を確認し、SNG2S Ver. 3.3 の対応バージョン、timebase 48、出力オプションの説明を FACTS F-001 に記録した。
- [x] tempo-record の整数計算と v1/v3/v4 の参照値を静的解析で対応付けた（FACTS F-071/F-073/F-085）。
- [x] 参照生成コマンドは利用者確認済み：`sng2gs.exe runner.sng` / `sng2gs.exe TEST.sng`、どちらもオプションなし（FACTS F-100）。実行ファイルの hash / 実行時バージョンは未特定。内部 option flag の runtime trace も未実施のため、静的 parser mapping と実際の flag 値は区別する。
- [x] 以前の汎用 `ReadTempi` 適用が v1 に不適切な候補を出す問題をガードし、後続の `ReadLegacyV1Tempi()` と tempo regression tests を追加した。過去の CI 記録は当時の commit に対する結果であり、現在の branch head の CI 成功を意味しない。

**完了条件:** マニュアルに書かれたオプション説明と、参照 MIDI を実際に生成したオプションを区別する。実行条件が特定できなければ未確認のまま記録し、推測で埋めない。

## T-004 — スケジューラ表と slot 対応を回収する
**優先度:** 中

- [ ] 0x1566/0x1576/0x1586/0x1596 の実データを確認する。
- [ ] 0x1E54 などのポインタ表を確認する。
- [ ] 内部 scheduler slot、SNG part、MIDI track の対応表を別々に作る。

**完了条件:** 各対応関係に直接根拠がある。根拠のない並べ替え推定を残さない。

## T-005 — 0xFA0 登録から MIDI 出力まで追跡する
**優先度:** 中（検索条件と出力先を一部確認）

- [x] `FUN_1000_3A10` の呼び出し元 `1000:37FD` と、その直前の条件分岐を特定した（`FUN_1000_3A3F` がゼロを返し、`DH != 0x40` の場合に登録）。
- [x] `FUN_1000_3A3F` の表検索条件、項目先頭 byte と DL の比較、DH が `0xC0` でない場合の3-byte出力経路を確認。
- [x] `FUN_1000_39C9` が AL を出力バッファへ書き込むこと、`0x1556` から `0x1156` への折り返しを確認。
- [x] `FUN_1000_39C9` の全参照一覧から、writer が `FUN_1000_3A3F` 以外の経路（期限切れ処理、`FUN_1000_3BA7` 系など）でも使用されることを確認。
- [ ] `FUN_1000_39C9` の呼び出し側で、レジスタ設定と出力後の読み取り／書き出し経路を追い、バイト列がどのイベント形式へ変換されるか確認する。
- [ ] 登録関数の他の参照・呼び出し元があるか、解析テキスト全体で確認する。
- [ ] 呼び出し直前の AL/AH/DL/AX を記録する。
- [ ] FUN_1000_39C9 が書き込むバイト列を追跡する。

**完了条件:** 0xFA0 項目の各フィールドと出力イベントの関係が根拠付きで説明できる。

## T-006 — MIDI トラック順とパート対応を検証する
**優先度:** 中

- [x] マニュアル記載の既定順（v1〜v3: 10→1、v4: 10,16→11,9→1）と現行 Glosa のパート昇順出力の差を整理した。
- [x] 参照 MIDI の track chunk 順・Note-on・channel を抽出し、SNG パートへ対応付けた。ノートイベントを持つパートの channel は part-1 と一致（FACTS F-030/F-054）。
- [x] ユーザー確認の「オプションなし」実行と参照 MIDI の実際の track chunk 順を照合。v1 `runner.MID` は track 0（title/conductor）、track 1（Exclusive）、part track の part 10→1 順で、part 1 / Keyboard の空 track も保持する。これはマニュアルの v1〜v3 既定順と整合する（FACTS F-054/F-057/F-100）。
- [x] 現行 Glosa の物理出力との違いをソースで再確認。`SngConverter.ToSmf` の先頭 track body は time-signature (`FF 58`) と tempo (`FF 51`) を出力するが、確認した writer 経路には参照 track 0 のタイトル文字列 meta-event と独立 `Exclusive` track の生成がない。続く part track は part 1→N 順であり、参照の part 10→1 と異なる。channel の `part - 1` 対応とは別問題。
- [ ] `/F` の昇順オプション自体は実行時に未検証。既存 reference がオプションなしの default を示すため、default track order の判定には `/F` 実行は不要。実行可能な SNG2S 環境が得られた場合に限り、`/F` の動作を別途確認する。

**完了条件:** default reference の chunk order、track role、channel／SNG part の対応、および現行 Glosa との差は説明済み。互換実装の変更は、SysEx/conductor/title track の生成元と回帰テスト方針を確定してから行う。

## 実装変更のゲート
src/Glosa.Core/Smf/SngConverter.cs の Tie またはレイアウト処理を変更する前に、該当タスクの完了条件を満たし、参照データとのイベント単位の照合と回帰テスト計画を用意する。

- [x] `FUN_1000_39E9` の読み取り処理を確認。writer と共有する `0x1150` カウンタ、読み取りポインタ `0x1154`、空キュー時の `0xFFFF` 返却を確認した。
- [ ] `FUN_1000_39E9` の呼び出し元（`1000:1840` および参照一覧の `0Bxx` 付近）を追い、取り出した byte の用途と最終出力先を特定する。
- [ ] `FUN_1000_3BA7` が生成する版別 byte 列と、読み取り側が期待する形式を照合する。
- [x] 読み取り側 `FUN_1000_0Bxx` が `0x80`〜`0xF0` の MIDI ステータス経路に分岐し、`F0` では `F7` まで読むことを確認。
- [ ] MIDI ステータスごとの処理先（`FUN_1000_17C2`、`FUN_1000_16E6` 等）と、最終 SMF 書き込みへの接続を追う。
- [x] `FUN_1000_17C2` → `FUN_1000_5648` の出力経路を確認。出力バッファポインタ／残量の更新と位置カウンタ `0x1C2A:0x1C2C` の増加を確認。
- [x] `FUN_1000_16E6` / `FUN_1000_1754` が 7-bit 継続形式の値を組み立て、異なる出力経路へ byte を書くことを確認。
- [ ] VLQ 再構成関数の呼び出し箇所を追い、delta-time とその他の可変長フィールドを区別する。
- [ ] `FUN_1000_48A2` の出力バッファ切り替え／flush の意味を調べる。


- [x] FUN_1000_16E6 の呼び出し箇所を確認。1000:0937 / 1000:096D は時間差出力と整合し、1000:0AD6 は SysEx byte 数の符号化に使われる。1000:04C4 はテンポメタイベント経路上だが、引数の由来は追加確認が必要。
- [x] FUN_1000_48A2 の基本制御フローを確認。出力管理構造体のフラグ、ストリーム別状態、0x200 byte ブロックを扱うため、単純な flush と断定しない。
- [ ] FUN_1000_48A2 の FUN_1000_59AC 呼び出しと 0x186A/0x186C 状態表を追い、各出力ストリームのブロック遷移条件を説明する。
- [ ] 1000:04C4 の 0xF9A/0xF9C がテンポメタイベント前に設定される経路を追い、delta-time 値の由来を確定する。

- [x] `FUN_1000_4A26` を追跡。ストリーム状態表は管理構造体 8-byte stride に対して 6-byte stride で対応し、通常は `0x200` byte のバッファを取得、取得失敗時は 1-byte の代替領域を使う。
- [x] `FUN_1000_48A2` / `FUN_1000_4A26` / `FUN_1000_4A92` / `FUN_1000_59AC` のフラグ更新と使用を追跡し、open mode・buffer fallback・テキスト変換の確認済み経路を整理。`0x02` / `0x10` / `0x40` など未解釈ビットは継続調査。
- [x] `FUN_1000_59AC` の handle フラグを追跡。`0x80` は LF→CR/LF のテキスト変換、`0x20` は DOS seek (`AH=42h`) 経路に使われることを確認。binary 経路と各 mode の設定源は追加調査。


- [ ] 管理フラグ `0x02` / `0x10` / `0x40` の全読み書き箇所を検索し、未確定の例外状態と正式な意味を整理する。
- [ ] `FUN_1000_59AC` の `0x20` seek がどの呼び出し元／handle 初期化と対応するかを追い、単なる seek と出力の順序を確認する。


- [x] `FUN_1000_47E2` の read 側、`FUN_1000_48A2` の write 側、`FUN_1000_49F8` の確保バッファ cleanup、`FUN_1000_4B8A` の特別固定領域設定を確認。
- [ ] `0x17CA+n*8` の各管理構造体を初期化・open・read・write・close する呼び出し元を一覧化し、`0x01/0x02/0x08/0x10/0x20/0x40` の横断状態表を完成させる。


- [x] open wrapper FUN_1000_4740 → FUN_1000_4A92 の接続と参照元を列挙。
- [x] special wrapper FUN_1000_4768 の FUN_1000_4B8A → FUN_1000_4D12 → FUN_1000_4C0E 順序を確認。
- [ ] FUN_1000_5687 を含む関数境界と引数を確定し、FUN_1000_47E2 / FUN_1000_48A2 が呼び分けられる条件を追う。
- [ ] FUN_1000_4697 の FUN_1000_49F8 呼び出し前後を追い、cleanup が handle/stream close のどの段階に相当するかを確認。
- [ ] 管理構造体の実インデックスを呼び出し元から逆算し、各フラグの遷移表をストリーム単位で完成する。


- [x] `FUN_1000_5670` を特定し、残量カウンタが尽きた時に `FUN_1000_47E2` を呼ぶ buffered reader と確認。
- [x] `FUN_1000_4CA4` の write-buffer flush 条件、`FUN_1000_470E` の全管理構造体走査、`FUN_1000_5690` の DOS close (`AH=3Eh`) を区別。
- [ ] `FUN_1000_4654` の flush 戻り値を caller が確認する条件を検証。
- [ ] `FUN_1000_59AC` の各 handle フラグと `FUN_1000_572A` の handle 登録・フラグ初期化をつなぎ、handle と管理構造体の対応を確定。


- [x] `FUN_1000_4654` の命令列で、`FUN_1000_4CA4` → `FUN_1000_49F8` → `FUN_1000_5690` の close 順序を確認。flush 失敗時の戻り値処理は未確定。
- [x] `FUN_1000_572A` の OS handle flag table 初期化と、`FUN_1000_4CA4` が管理構造体 `+7` の handle を `FUN_1000_59AC` に渡す接続を確認。
- [ ] open mode ごとの `0x1782+h` flag byte 遷移表を完成し、`FUN_1000_4654` の全 caller が flush エラーを処理するか確認。

- [x] close routine の正しい関数名が `FUN_1000_4654` で、`1000:4697` は内部 call site であると訂正。複数 caller で戻り値直後の検査がないことを確認。

- [x] open mode `r/w/a`, `+`, `b/t` と DOS handle flag byte の生成条件を整理（F-025）。
- [x] `FUN_1000_58CE` の read-side で `0x04` を read-buffer 先頭 LF 条件に応じてセットすること、`0x1A` 検出で `0x02` を latch して以後の read を止めることを確認（F-026）。
- [x] `0x02` の clear は `FUN_1000_56B0` の seek 成功後 (`1000:5722`)、`0x04` は `FUN_1000_58CE` 内でセット・次回 text pass 冒頭でクリアされることを確認。解析テキスト全体検索では `0x04` の外部参照は未検出。
- [x] 参照 MIDI の所在を `docs/ballade-version-testdata` ブランチで確認し、v2/v3/v4 の Note-on/off を比較した。
- [x] Tie bit の Note-on 抑制と pending Note-off 延長規則を、v2/v3 の全パートの参照イベントでオフライン照合した。
- [x] v2〜v4 の C# 変換出力と参照 MIDI の集計 Note-on/off 比較テストが CI で成功した（F-028）。
- [x] トラックごとのイベント構成を比較する CI テストを実行。fixture branch commit `65ace59` のテストは Linux/macOS/Windows で成功（CI runs `39220897447`, `39220904556`）。
- [x] チャンネル割当を参照 MIDI と比較し、パート番号 − 1 と一致することを確認。チャンネル番号もイベント署名に含めた CI run [39223684171](https://github.com/pikachu364/glosa/actions/runs/39223684171) が Linux・Windows・macOS で成功（FACTS F-030）。
- [ ] 同音高重複時の Note-off 所有関係を検証する。イベント集合の一致だけでは、重複する同音高ノートの Note-off がどの Note-on に対応するかは一意に決まらない。

## T-007 — Tie 修正の CI と実 MIDI 回帰を確認する
**優先度:** 高

- [x] Tie 修正を含む v2〜v4 の実変換出力比較テストが CI で成功した。
- [x] 合成テストとトラック単位比較テストの CI 結果を記録。runs `39220897447` / `39220904556` は全 OS job 成功。
- [x] チャンネル番号をイベント署名に追加し、run `39223684171` で Linux・Windows・macOS の全 job 成功を確認。
- [x] v2/v3 の参照 MIDI と修正後 C# 出力を統合テストで再比較（v2 は参照側に +160 tick 補正）。v2〜v4 は集計・トラック単位の比較が CI で成功。
- [x] トラックごとのイベント構成とチャンネル割当を参照 MIDI と比較し、CI 成功を確認。
- [ ] v2/v3 Tie のイベント署名一致とは別に、参照 MIDI の重複同音高 Note-off がどの Note-on に対応するかを意味論として特定する。v1 タイマー競合モデルのオフライン一致は F-044 に記録済み。
- [x] 初期段階では v1 の参照 MIDI 比較テストを一時除外した。後続コミットで v1 専用経路と参照イベント比較テストを追加し、この制約は解消済み（F-045）。
- [x] v1 の参照 MIDI を使ったレイアウト/Note-on/off イベント検証は作業ブランチで実装・CI 確認済み。13個の seek 値それぞれの意味、controller/tempo/meta、track chunk 順は引き続き未解決。

**完了条件:** CI 結果と参照 MIDI 比較結果を記録し、残差があれば具体的なイベント例を残す。


## T-008 — 同音高重複 Tie テストの fixture/CI 失敗を解消する
**状態:** 完了（fixture 修正と3 OS CI成功を確認）

- [x] run [39224478408](https://github.com/pikachu364/glosa/actions/runs/39224478408) で、旧 fixture が Windows・Ubuntu・macOS の全 job で失敗することを確認。
- [x] テスト fixture と Tie の候補 Note-off 選択コードを確認。
- [x] commit [07e8b57](https://github.com/pikachu364/glosa/commit/07e8b57c73628b7e263781fdff84b20748172b36) で最初のレコードの duration/step 引数を修正し、意図した同一 Note-off tick 条件を表現する fixture にした。
- [x] commit に紐づく CI run [39224987508](https://github.com/pikachu364/glosa/actions/runs/39224987508) で Windows・Ubuntu・macOS の全 job が成功することを確認。
- [x] FACTS.md と LOG.md に結果を記録。
- [ ] 実 MIDI における同音高重複時の Note-off 所有関係を確定する。これは fixture/CI 修正とは別の意味論上の調査として T-007 で継続する。

**完了条件:** fixture 修正コミットの CI が3 OS で成功すること。達成済み。

- [x] `FUN_1000_40c8` の引数配置と DOS `INT 21h` 呼び出しを確認。`0x4201` と `CX=0, DX=value` の組み合わせから、v1 の13個の16-bit値が現在位置基準の相対 seek 距離であることを確認した。
- [ ] seek 距離を runner.SNG の全10パートに適用し、各パートの終端候補と算出位置を照合する。相対 seek の役割を確定するまでは converter を変更しない。

- [x] 全10パートの seek 後位置を検算し、最終65 byteが最終パートの読み取り位置とファイル末尾の関係で説明できることを確認（FACTS F-040）。
- [x] Note-off tick 残差（part 2/3/10）をタイマー競合モデルで説明し、参照 MIDI 全3,295件との一致を確認（FACTS F-044）。

- [x] v1 part 10 の同 tick・同音高ノート（pitch 69、tick 1728、duration 1）を含む Note-off の差をタイマー競合規則で再現（FACTS F-044）。
- [x] part 2 / 3 の Note-off 残差を同じタイマー規則で再現し、全パートの差分0を確認（FACTS F-044）。

- [x] `FUN_1000_3A10` / `FUN_1000_3A3F` / `FUN_1000_3406` の静的命令列を確認し、同じ識別値を持つ既存タイマーと新しい長さを照合する経路、および期限切れ時の処理を記録。
- [x] `FUN_1000_3A3F` の入力キー・残り値比較・登録抑止をモデル化し、tick 1728/1729 の例を含む参照 Note-off 全件との一致を確認。
- [x] part 2 / 3 を含む全10パートで同じタイマー競合処理を照合。

- [x] タイマー表の照合キーを caller から追跡し、velocity ではなく Note-on status/channel byte と pitch の組であることを確認。
- [x] 残り値比較と同 pitch 重複 Note-off の対応を record 単位でモデル化し、差分0を確認。

- [x] v1 の Note-off タイマー処理を `FUN_1000_3406` / `FUN_1000_3A3F` / `FUN_1000_3A10` の静的命令列に沿ってオフライン再現し、参照 MIDI の全 Note-off 3,295件と差分0を確認。
- [ ] この規則を converter の v1 経路に反映する前に、既存の converter の note/tie/event abstraction とテスト設計を照合し、v2/v3 の既存一致を退行させない実装計画を作る。
- [ ] v1 の13個の16-bit seek 距離それぞれの個別の意味を調査する。累積値が各パートの次情報バッファ位置／EOFに一致することは確認済み（FACTS F-039/F-040）。この意味論の調査は継続。


## T-009 — SNG2GS `/DEF` オプションの必要性を確認する
**優先度:** 低（保留中・実装方針の変更なし）

- [x] `/DEF` は MIMPI 用 `.DEF` 音源定義ファイルを生成し、チャンネルごとの音源種別やトラック名の表示に関係する機能として調査メモに記録（FACTS F-051）。
- [x] JMPlayer `sngmidi.cpp` に音源種別に関係する情報取得はあるが、同ファイル内に `.DEF` 出力処理が見当たらないことを記録。
- [ ] 音色名表示のための定義ファイル不足と、MIDI の音色指定・再生品質の問題を混同せず、実際の利用要件が確認できた場合にのみ再検討する。
- [ ] SNG 解析で得られる音源種別情報の利用方法は、必要性が具体化するまで検討を保留する。

**方針:** 現行の SNG 対応実装・優先順位・コードを変更しない。`.DEF` 生成は必須要件に含めず、必要性が確認できるまで着手しない。

## T-010 — 最終出力の実再生・聴感確認
**優先度:** 中（自動テストとは別の最終確認）

- [ ] Glosa で生成した MIDI を実際に再生し、無音・途中停止・明らかなノート欠落がないか確認する。
- [ ] 可能なら元の SNG を SNG2GS で生成・再生した参照音と、Glosa の変換 MIDI を同じプレーヤー・同じ音源設定で比較する。
- [ ] 試聴に使った SNG/MIDI、プレーヤー、音源・設定、確認結果と気づいた問題を記録する。
- [ ] 聴感上の違いがあれば、イベント比較（Note-on/off、controller、tempo/meta、track chunk 順）と切り分け、耳だけで変換正当性を判定しない。

**完了条件:** 自動テスト・参照イベント比較とは別に、実再生した環境と結果が記録されていること。環境が用意できない場合は未実施として残し、実施済みと扱わない。



## T-011〜T-013 — v1 tempo queue / grid / preamble（静的解析は完了）

**状態:** 静的な呼び出し順、tempo-record の整数計算、192-tick grid の exact-tempo 経路、初回 `FUN_1000_3406` の例外経路と queue-drain semantics は記録済み（FACTS F-065〜F-068、F-073、F-076、F-080、F-082、F-085）。

これらの節は調査履歴として残す。残件を重複して管理しないため、次の作業は T-017 に集約する。

- tick 8 の pending-delta / absolute-tick の実際の値と因果関係
- `0x6EE` の初期値・設定元
- 4件の setup SysEx payload と producer の対応
- 参照 `runner.MID` を生成した実際のオプション

**検証境界:** `SngConverterTests.cs` の公開テストは 500000 / 869000 / 869565 の期待 byte sequence を検査する。これは変換出力の回帰確認であり、元実行ファイルの実行時挙動全体、SysEx payload、SMF 全体のバイト一致、実再生を証明するものではない。

---

## T-014 — tempo slider curve と 3/4 の MIDI 出力丸めを分離
**優先度:** 中（v1 の読み込み失敗を直すブロッカーではない）

- [x] jmplayer の仕様メモと SNG2S `FUN_1000_3406` を照合し、`rawValue * baseTempo / 64` の整数除算、1..480 の clamp、baseTempo=120 の場合の `floor(15*v/8) = 2v - ceil(v/8)` を確認（FACTS F-071）。
- [x] `runner.SNG` の v1 tempo stream を実バイトで再確認。レコードは `(step, value)` の little-endian word pair で、`37` は第2 word、`48/144/192` は第1 word の interval fields。baseTempo 120 で SNG2S の式に入れると 69 BPM になるが、37 をユーザー向け slider 値と同一視できる根拠は未確認（FACTS F-071/F-089）。
- [ ] SNG2S の 3/4 固有 +1 BPM 説明について、time-signature input と MIDI tempo-meta generation の間に条件分岐があるか、decompilation の全 call/data flow を調べる。
- [ ] 3/4 の SNG と SNG2S 参照 MIDI が得られた場合、slider 値ごとに実際の tempo meta と time-signature meta を比較する。現在の private fixture tree には 3/4 の参照 fixture が見つかっていないため、未確認のまま数値補正を実装しない。
- [x] v1/v3/v4 reference MIDIs show the same two integer paths: v1 869000 at tick 8 and 869565 at tick 192 (69 BPM); v3 361000 at tick 92 and 361445 at tick 192 (166 BPM); v4 260000 at tick 116 and 260869 at tick 192 (230 BPM). This confirms the values are coarse millisecond and exact microsecond representations of the same normalized tempo, not two actual BPM changes (FACTS F-071).
- [ ] The remaining tick-8 queue-drain question is tracked in T-017. Keep the separate 3/4 +1 BPM investigation here; do not duplicate the same queue task.

**完了条件:** slider-to-timer curve と SNG2S MIDI-export rounding を別の処理として説明し、3/4 の補正は参照 fixture で確認できるまで実装しない。


## T-015 — versioned tempo-stream start offsets
**Priority:** completed for v1; keep later-version base-tempo semantics as a separate audit.

- [x] Verify v1 tempo stream starts at sentinel +10: `runner.SNG` yields (step=48,value=37), (144,37), (192,37); SNG2S consumes the second word in its base-tempo scaling calculation, so 37 at baseTempo120 yields 69 BPM. Whether this word is semantically the same as the UI slider remains unproven (FACTS F-089).
- [x] Verify v3/v4 use sentinel +12: value 89 gives 166 BPM in `START__0.SNG`, and value 123 gives 230 BPM in `BOURREE0.SNG`, matching their reference MIDIs.
- [x] Confirm Glosa already has separate `ReadLegacyV1Tempi(last+10)` and later-version `ReadTempi(last+12)` implementations.
- [ ] Separate audit: v2 `SHIZUME0.SNG` has a different base-tempo setting and a 6/4 grid. Verify its full tempo-stream-to-MIDI tick scaling before changing shared later-version code; do not infer it from the v1 rule.

**Result:** jmplayer's shared `last+12` tempo parser would misread the v1 stream even after its part-block layout is fixed. This is a separate v1 compatibility fix, documented in FACTS F-074.


## T-016 — MIMPIV4 legacy tempo records to playback timer

**Priority:** high; required for the SNG2S/MIMPIV4 comparison.

- [x] Retrieve the complete private MIMPIV4 Ghidra export via its blob; the file is larger than the ordinary file-content response and must remain private.
- [x] Confirm `FUN_1000_22FC` legacy branch reads the byte at `+0x1C` of the loaded conductor structure and stores it in `0x7BF0` and `0x0440`; the timer parameter is not calculated from the tempo record's second word at this point (FACTS F-090).
- [x] Trace timer setup: `FUN_1FF5_0D61` installs the interrupt-8 handler and initializes timer state; `FUN_1FF5_0DFA` calculates a hardware timer divisor using the current CX parameter, a factor of 120, and `0x3392` timebase; `FUN_1FF5_0DE1` updates the scheduled count.
- [x] Scan the full available MIMPIV4 Ghidra export for direct `0x0440` references: only loader initialization and timer-driver initialization write it; playback later passes it to `FUN_1FF5_0DE1` (FACTS F-090).
- [x] Re-open the full private export and verify the call sites: legacy `FUN_1000_22FC` copies conductor byte `header+0x1C` to both `0x7BF0` and `0x0440`; `FUN_1FF5_0D61` loads `0x0440` into `CX` before calling the timer-divisor calculation, and `FUN_1FF5_1409` restores `0x0440` from `0x7BF0`. Other playback scheduling paths call `FUN_1FF5_0DE1` with an event-relative count plus `0x0440`. This confirms the base timer input and additive scheduling path, but does not identify a consumer for the SNG tempo-record second word; do not infer that MIMPIV4 applies SNG2S's `rawValue * baseTempo / 64` formula.
- [x] Inspect `FUN_1000_341E`, the early special-record scan: it accepts record types `0x01`, `0x02`, and `0x03`, copies bounded payloads into fixed buffers at `0x3BA` / `0x400`, and does not itself read the tempo-record `(step,value)` pairs or reference `0x0440` / `0x7BF0`. This narrows the search but does not identify a dynamic-tempo consumer; do not treat this pre-scan as proof that tempo records are ignored.
- [x] Follow the `0x3BA` / `0x400` buffers to `FUN_1000_9D75`: it iterates their NUL-terminated bytes and passes text-like values to `FUN_1000_BF17` with fixed screen coordinates/attributes. These buffers are therefore consistent with UI/display labels, not the missing playback-tempo record consumer. This is a static interpretation of call arguments, not a verified function name.
- [x] Check further candidate paths in the complete export: `FUN_1000_3543` pre-scans event streams and computes maximum absolute tick; `FUN_1000_2D27` rewrites Program Change records; neither shows a timer-base update. `FUN_1000_8888` schedules a parsed text-command delay plus `0x0440`, but no link to the binary SNG tempo table is established.
- [ ] Identify which MIMPIV4 routine consumes the v1 conductor tempo records and determine whether it changes another timer parameter or per-event delta scaling.
- [ ] Reconcile that routine against the actual v1 `runner.SNG` records without assuming the SNG2S MIDI-export curve applies to MIMPIV4 playback.
- [ ] If the path cannot be proven statically, record the exact remaining dataflow gap; do not claim runtime behavior without a trace.
- [ ] If static tracing cannot identify the consumer, run a runtime trace on a known changing-tempo SNG and compare timer/scheduler state immediately before and after the tempo record.

**Current result:** the loader and hardware-timer path are mapped, and several likely parser/pre-scan candidates have been excluded. The actual consumer of the SNG tempo-record second word remains unidentified. This does not prove that MIMPIV4 ignores tempo records (FACTS F-077/F-084).


## T-017 — distinguish coarse tick-8 tempo from exact grid-aligned tempo

**Priority:** high; remaining SNG2S scheduler gap.

- [x] Re-read the complete SNG2S `FUN_1000_0010` decompilation rather than relying on the summary wording. The code reads the first byte of the grid structure through `0x21DC` into `uVar11`; the grid buffer yields `uVar11 = 192`, not 8. The absolute source offset is unresolved (FACTS F-062/F-072/F-080).
- [x] Confirm `FUN_1000_61EC(absTick, uVar11, 0, 0)` computes the remainder modulo the grid operand and that the following delta aligns the **exact** tempo meta to the next 192-tick boundary. The 869565 event at tick 192 is this path (FACTS F-072/F-080).
- [x] Correct FACTS F-080 to explicitly state that grid alignment does not explain the earlier coarse 869000 event at tick 8.
- [x] Trace the startup call order across `1000:0448` → `FUN_1000_1C70` / v1 `FUN_1000_1CE4`, `1000:044B` → `FUN_1000_33D4`, and `1000:0463` → `FUN_1000_08EC`. The two setup drains each leave pending delta 4; the first `FUN_1000_3406` call skips the normal +1 path while `0xF96` is nonzero. When `0x622 == 0`, the main routine preserves pending delta and the queued coarse tempo meta drains at tick 8 (FACTS F-095).
- [x] Cross-check the setup-output schedule against both private reference files: `runner.MID` and `TEST.MID` each contain SysEx events at ticks 0, 4, 8, and 8 (payload lengths 12, 12, 10, 10). `TEST.MID` has no changed-tempo event because its tempo stream normalizes to the default 120 BPM (FACTS F-082).
- [x] Re-scan the complete 18,795-line SNG2S export: `0x6EE` appears 58 times across decompiler and assembly views; the assembly references are reads, with no direct store found. The export alone omits initialized data-segment bytes; the original EXE's on-disk initial value is recorded in the next item.
- [x] Recover the on-disk initialized value of `0x6EE`: `DS:0x06EE` maps to file offset `0x6B5E`, whose bytes `04 00` encode little-endian word value 4 (FACTS F-094).
- [x] Confirm the parser mapping `/XD → 0x622 = 1` from the original executable's initialized bytes and comparison branch (FACTS F-096). The user confirmed that reference generation used no switches, so `/XD` was not explicitly requested; runtime tracing of the resulting internal flag was not performed. `/F` token-to-flag mapping is also not directly confirmed.
- [x] Map the first two v1 setup SysEx events at ticks 0 and 4 to `FUN_1000_1CE4` templates `0xF34` / `0xF3F`; formatted arguments, checksums, terminators, and lengths match both `runner.MID` and `TEST.MID` (FACTS F-097). Private payload bytes are not copied into public docs.
- [x] Trace the startup call `FUN_1000_33D4` → `FUN_1000_3B68` → `FUN_1000_3B78` → `FUN_1000_3BA7`; identify the two message bodies and checksum formulas (FACTS F-101).
- [x] Read the initialized bytes at `CS:0x3F82`: `F0 41 10 16 12 FF`. The four payload-template bytes after the SMF SysEx status are `41 10 16 12`; `0xFF` is the template terminator.
- [x] Read the initialized word at `DS:0x07DE` as `0x004B` and compare the formula results with the saved tick-8 events in both `runner.MID` and `TEST.MID`. Both 10-byte event payloads match byte-for-byte (FACTS F-101).
- [ ] Keep converter behavior and tempo tests unchanged until independently validated. The static tick-8 mechanism explains the inspected reference path, but runtime state and generalization to every v1 file are not proven.

**Current result:** setup SysEx payloads at ticks 0, 4, and both tick-8 events are matched to static producer paths and reference MIDI. The initialized `0x6EE = 4` value and tick-8 setup-template bytes are recovered. The coarse tempo's tick-8 placement has a static explanation (FACTS F-072/F-080/F-094/F-095/F-097/F-101), but runtime state, indirect writes, and generalization to all v1 files remain unproven.

### Follow-up trace — scheduler calls are not yet a tempo-record consumer (2026-10-22)

- Re-opened the full private MIMPIV4 export and inspected `FUN_1000_8888`, `FUN_1000_A3BB`, `FUN_1FF5_0DE1`, and `FUN_1FF5_0DFA`.
- `FUN_1000_8888` is called from `FUN_1000_7E55`, a text-command parser that dispatches commands beginning with `@`. The handler parses a value from a text token, stores it at `0x43E`, then schedules `parsedValue + [0x440]` through `FUN_1FF5_0DE1`. This establishes a base-plus-relative scheduling mechanism for that command language; it does **not** establish that the binary SNG conductor tempo-record second word reaches this handler.
- `FUN_1000_A3BB` is a UI/event-loop routine. When counter `0x43A` reaches threshold `0x3516`, it clears `0x43E` and `0x43A` and reschedules the base value at `0x440`. This looks like expiration/reset handling, not a tempo-record decode path.
- `FUN_1FF5_0DE1` simply stores the requested count in `0x3390` (normalizing zero to one). The timer-divisor routine `FUN_1FF5_0DFA` uses its CX input to program the timer hardware; no conductor-record pointer or tempo-record field is present in these routines.
- These traces narrow the known scheduling calls but still do not identify the binary conductor tempo-record consumer. The `FUN_1000_7E55` text-command path should not be conflated with the SNG binary parser without a proven caller/dataflow link.
- CI recheck for head `1ff04bcb42700727f6963c464e8dc4d4deab730e`: both push run `39736654504` and PR run `39736659682` completed successfully. PR #9 remains open and unmerged.

### Setup-template trace follow-up (2026-10-22)

- The SNG2S text exports show that helper routines at 1000:3F79 and 1000:3F97 load BX with 0x3F82 and 0x3FA0 respectively, then scan bytes until 0xFF. One path sends each byte to the output-byte routine (FUN_1000_4098); the other writes bytes into the queued output buffer (FUN_1000_39C9).
- FUN_1000_3BA7 uses the 0x3F82/0x3FA0 template paths and also emits explicit bytes including 0x10, 0x16, the current low byte of 0x7DE, a derived 7-bit value, and 0xF7. The branch conditions depend on 0x6FA, 0x6F8, and format byte 0x25DA.
- This narrows the setup-message construction logic, but the exported disassembly does not include the initialized data bytes at 0x3F82 and 0x3FA0. Consequently the actual template payloads cannot be recovered from these text exports alone, and a one-to-one mapping to runner.MID SysEx payloads is not yet supported. Do not infer full payload bytes from the helper names or the F7 terminator.
- CI for 67f2dcd completed successfully for both push and PR workflows. PR #9 is still open and unmerged.

### SNG2S option documentation cross-check (2026-10-22)

- Read the archived SNG2S v3.3 manual (`testdata/ballade/sng2gs/SNG2S.DOC` in the private reference repository). It documents `/1` as the default (Format 1), `/X2` as the default SysEx placement (a dedicated track immediately after the conductor track), and `/XD` as disabling SysEx conversion. `/X1` instead puts SysEx into the conductor track; `/F` changes SNG-track ordering, and `/N` omits parts without notes.
- This narrows the documented defaults for a normal SNG2S invocation, but the exact command line that produced `runner.MID` / `TEST.MID` is still not recorded. The manual does not include the binary template bytes at 0x3F82/0x3FA0, so it cannot close the payload mapping by itself.
- The SNG2S manual also states output timebase is fixed at 48 ticks per quarter note. That is consistent with the reference MIDI's PPQN but does not explain the coarse tempo meta at tick 8 or the pending-delta value 0x6EE.
- CI for commit 608e731 completed successfully on both push and PR workflows (runs 39738048630 and 39738058337). PR #9 remains open and unmerged.


### Latest continuation note (2026-10-22)

- `FUN_1000_3406` directly reads 4-byte tempo records from the table at `0x21E0`; the second word is normalized using the base tempo at `0x2962`, divided by `0x40`, clamped to 1–480, and compared with `0x7BA` before emitting changed-tempo output.
- `FUN_1000_3DE2` sets `0xF96 = 0xFFFF`; `FUN_1000_33D4` calls `FUN_1000_3406` before resetting `0xF96` to zero. This confirms the first-call exception path, but does not resolve the tick-8 queue/pending-delta state.
- The unresolved work is now narrower: trace the state before the first coarse tempo queue drain and recover the initialized value of `0x6EE`; do not re-open the already-supported tempo arithmetic or 192-tick grid calculation.


### Private Ballade reference-directory audit (2026-10-22)

The full `glosa-private-data/testdata/ballade` tree has been inventoried and the text documentation / reverse-analysis exports reviewed. It contains versioned SNG fixtures for Ballade v1–v4, SNG2GS reference MIDIs for each version, saved pre-fix Glosa MIDI outputs for v2–v4, the SNG2S v3.3 manual and two SNG2S analysis exports, plus MIMPIV4 v4.27 documentation and its analysis export. The private repository also contains private fixture integration tests; keep all SNG/MID fixtures and private analysis dumps out of the public `glosa` repository.

#### New findings
- SNG2S manual: default SMF Format 1 and SysEx track placement `/X2`; `/X1` puts SysEx in the conductor track; `/XD` disables SysEx conversion; `/F` changes track ordering; `/N` omits parts with no notes; timebase is fixed at 48 PPQN. The actual options used to create the saved reference MIDIs remain unknown.
- MIMPIV4 manual: playback tempo can be changed with Up/Down and fine-adjusted with `/Tn`; this is documentation of controls, not the SNG tempo-record conversion formula.
- MIMPIV4 disassembly: `FUN_1FF5_0D61` hooks INT 8 and initializes timer state; `FUN_1FF5_0DFA` derives a hardware divisor from its `CX` input and writes timer-related I/O ports. `0x0440` and `0x7BF0` receive a byte from a format-dependent loaded record. This is a separate timer path, and no proven link from the binary SNG tempo-record table to that timer input has been established.

#### Revised next actions
1. For SNG2S, continue only with evidence that can recover the initialized data segment / aliases for `0x6EE`, or obtain a runtime trace. The text export's direct references are reads; do not assume `0x6EE = 4`.
2. For MIMPIV4, identify the binary SNG parser path that loads `0x7BF0` / `0x0440`, then trace whether any SNG tempo-record data changes the timer input or a separate playback interval. Keep timer hardware initialization distinct from MIDI-export tempo meta handling.
3. Record the reference MIDI generator version/options if recoverable; otherwise keep this as a comparison limitation.
4. Keep tempo/meta and physical SMF track-order claims unverified until event-level comparisons or runtime evidence exist. Do not change converter/test behavior based only on numeric coincidence.

**No converter or test code was changed by this audit.**

### MIMPIV4 header-field trace update (2026-10-22)

Follow-up inspection of `FUN_1000_22FC` narrowed one previously open item: the initial timer base is copied from a format-dependent byte in the loaded header/record at `0x6568` into `0x7BF0` and `0x0440`. The old-format branch (`0x7800 == 0`) reads offset `+0x1C`; the nonzero format branch reads offset `+0x06`. `FUN_1000_314F` also contains a separate default initialization of `0x7BF0 = 120`.

**Updated priority:** do not spend the next pass rediscovering the initial timer-base byte. Trace whether later SNG playback parsing or event processing changes `0x0440` / `0x7BF0`, and whether the loaded binary tempo-record stream is connected to that update. This connection is not established yet.

### Private fixture metadata consistency check (2026-10-22)

The private repository's `.github/private-tests/PrivateBalladeFixtureTests.cs` contains tests for v1 `TEST.SNG` tempo-event equality and v1 `runner.SNG` Note-event equality. Its `private-data-ci.yml` stages the private fixtures/tests into the requested Glosa ref and runs the full test project without publishing the fixture files. However, `testdata/ballade/README.md`, `manifest.json`, and `v1/sng2gs/README.md` still describe v1 as unsupported / excluded, which no longer matches the current v1 implementation and private tests. Treat those status strings as stale metadata; the actual CI result for the specific Glosa ref remains the authority. The private metadata files were not changed during this audit.

### MIMPIV4 driver-dispatch cross-reference (2026-10-22)

The six additional writers of `0x0440` are reached from the driver-dispatch routine `FUN_1FF5_1344` through its selected branches (`FUN_1FF5_2769`, `FUN_1FF5_3DD2`, `FUN_1FF5_44E0`, `FUN_1FF5_4CF8`, `FUN_1FF5_5114`, `FUN_1FF5_579C`). Their code initializes driver/scheduler state and copies `0x0438` to `0x0440`; this is evidence for a driver setup path, not a proven SNG tempo-record consumer. The unresolved work is to connect the SNG playback entry path to this dispatch, or document the missing static edge and retain runtime tracing as the required next step.

### MIMPIV4 tempo consumer boundary (2026-10-22)

Static tracing now establishes the driver-dispatch and timer-initialization path: `FUN_1FF5_1344` selects driver setup; selected branches copy `0x0438` to `0x0440`; `FUN_1FF5_0D61` installs the INT 8 handler and passes `0x0440` to `FUN_1FF5_0DFA`, which calculates the timer divisor and writes I/O ports. This is initialization evidence only. No static dataflow yet connects the binary SNG tempo-record second word to updates of `0x0438`, `0x0440`, or `0x7BF0` during playback.

**Next action:** trace the playback record parser/caller chain and all writes to those timer globals after initialization. If the static export omits the relevant edge or initialized data, record that gap and require runtime tracing; do not infer the playback formula from SNG2S MIDI-export arithmetic. No converter changes are justified by this finding.


### Private v1 fixture CI result (2026-10-23)

Private workflow run [#6](https://github.com/pikachu364/glosa-private-data/actions/runs/39699711256) passed on Glosa checkout `440000b971e72debcef72bb12407bcf4e944527a`: build succeeded and 977/977 tests passed, including v1 `TEST.SNG` tempo comparison and `runner.SNG` Note-event comparison. The preceding failures were a comparison-order issue for simultaneous Note-off events at tick 4012 (pitches 77 and 82), not evidence of a converter timing/channel mismatch. The canonical test ordering was stabilized by pitch; production converter code was not changed. Current PR head adds docs only after the validated checkout. Remaining v1 limits: this does not establish SysEx byte equality, physical SMF track/chunk order, complete controller/program/pitch-bend equivalence, or runtime playback correctness. Private metadata files still contain stale v1-unsupported descriptions and should be corrected in the private repository separately.


## 2026-10-23 — private v1 validation outcome and next priorities

### What is now verified

- Private workflow [run #6](https://github.com/pikachu364/glosa-private-data/actions/runs/39699711256) checked out Glosa commit `440000b971e72debcef72bb12407bcf4e944527a`, built successfully, and passed 977/977 tests, including `PrivateV1TestSngKeepsReferenceTempoEvents` and `PrivateV1RunnerMatchesReferenceNoteEvents`.
- The prior runner Note-event mismatch was only the ordering of two simultaneous Note-off signatures at tick 4012, channel 1, pitches 77 and 82. Canonical sorting by pitch fixed the test comparison; production converter code did not need a behavior change.
- The current PR #9 head is documentation-only after that validated converter/test state. Public CI run #824 for the current docs head was cancelled; prior public run #815 succeeded on an older head. Keep these statuses separate.

### Next actions (ordered)

1. **Do not modify note conversion to address the resolved ordering mismatch.** The canonical-order test fix is sufficient for that issue.
2. Obtain a non-cancelled public CI run for the current PR head; a cancelled run is neither pass nor failure evidence.
3. Correct the stale v1-unsupported status in the private metadata (`testdata/ballade/README.md`, `manifest.json`, `v1/sng2gs/README.md`) in the private repository, without copying fixture bytes or private analysis exports into public `glosa`.
4. Continue only on genuine equivalence gaps: setup SysEx payload byte equality, physical SMF track/chunk order, controller/program/pitch-bend coverage, and later changing-tempo reference fixtures.
5. Keep the SNG2S tick-8 pending-delta / `0x6EE` initialization question and MIMPIV4 runtime tempo-consumer question separate. `0x6EE = 4` remains unproven; no runtime trace or actual playback test has been performed.
6. PR #9 remains open. Do not merge without explicit user authorization; before merging, review the two commits behind `main` and resolve any conflict deliberately.

## T-018 — v2 setup SysEx producer mapping

**Status:** static disassembly, original executable initialized-data inspection, and reference-MIDI byte comparison complete for the three identified event pairs. No runtime trace was performed.

- [x] Confirm the v2 loader reads `0x4E` bytes into `0x1AE2` from file offset `0x73` for `SHIZUME0.SNG`.
- [x] Trace the class-1 fallback: `0x1B04=0`, so `0x1AF4` / `0x1AF6` provide `6,6`; `0x7BE=5`.
- [x] Inspect the original `SNG2S.EXE` initialized image: `DS:0x07BC` maps to file offset `0x6C2C` and is initialized to `0x00`. Adjacent words initialize `0x7BE=5`, `0x7C0=0`, `0x7C2=5`, `0x7C4=3`.
- [x] Trace the override gates: bits 0–2 of `0x7BC` control assignments to `0x7C0`, `0x7C2`, and `0x7C4`. The available full analysis export contains no direct store to `0x7BC`; indirect/aliased runtime writes are not fully ruled out.
- [x] Match `FUN_1000_1CE4`'s `0xF34` / `0xF3F` output against reference MIDI ticks 100 and 104. The events at ticks 100 and 104 match the calculated arguments `0,6,6`, checksums, and terminators.
- [x] Identify the earlier tick-4 / tick-8 pair as `FUN_1000_23C0(4)`: the calculated data values match the reference events at ticks 4 and 8, including checksums and terminators.
- [x] Correct the earlier mismatch framing: the tick-4/tick-8 pair and tick-100/tick-104 pair are distinct producer paths, not competing expected values.
- [x] Keep this v2 investigation separate from T-017's v1 tick-8 pending-delta problem and T-016's MIMPIV4 playback-tempo consumer.

**Result:** the `0x7BC` on-disk initial byte and both identified SysEx producer paths are resolved by static analysis and reference-MIDI byte comparison. No converter/test code was changed; no runtime trace, MIDI regeneration, build, or test was performed.


### 付属 README による版情報（2026-10-24）

- [x] `README.DOC` を確認し、対象資料が SNG2S Ver 3.3 であること、Ver 3.2 → 3.3 で Ballade 3 + `/N` 時の 11 トラック目以降の変換欠落が修正されたことを記録した（FACTS F-099）。
- [x] Ver 3.0〜3.1 は正しい変換を行わないという README の警告を記録した。
- [x] `/DEF` と `/MT` は Ver 2.0 からの追加オプションと分かったが、README には詳細仕様がないため、推測で意味を補わない。


### Reference generation command confirmed (2026-10-24)
- [x] Record the user's confirmation that `runner.MID` / `TEST.MID` and the other `sng2gs` outputs were generated with commands such as `sng2gs.exe runner.sng` and `sng2gs.exe TEST.sng`, with no switches.
- [x] Treat these files as default-behavior references; do not explain them by assuming explicit `/F`, `/N`, or `/X1` arguments.
- [ ] Compare observed empty-track retention and track order against the documented default behavior, keeping observed output separate from unproven internal implementation details.


### v1 tick-8 SysEx の byte 照合（2026-10-25）
- [x] 元の SNG2S.EXE の CS:0x3F82 を読み、template が F0 41 10 16 12、終端が FF であることを確認。
- [x] 0x7DE の initialized word 0x004B と、runner.MID / TEST.MID の tick 8 SysEx 2 payload を照合。A は 41 10 16 12 10 00 16 4B 0F F7、B は 41 10 16 12 52 00 10 4B 53 F7 で、静的生成式と byte-for-byte 一致。
- [ ] runtime trace や再生での確認は未実施。今回の一致は EXE initialized image・静的命令列・保存済み MIDI の比較による。
