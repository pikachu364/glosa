# 逆解析の進捗サマリー（2026-10-21）

## 現在地

- **進行中の最優先課題:** v1 tempo meta の tick 8 / tick 192 の出力時刻を SNG2S の処理経路と対応付ける。
- **参照 MIDI で確認済み:** tick 0 = 500,000、tick 8 = 869,000、tick 192 = 869,565 µs/quarter。time signature は tick 0 の 4/4。
- **静的解析で説明できたこと:** raw tempo 37 → 69 BPM。tempo-record 経路は (60000 / 69) * 1000 = 869000、192-tick grid 経路は 60000000 / 69 = 869565。数値は参照値と一致するが、tick 8 の割当は実行時に未確認。
- **直近の重要な制約:** 初回 FUN_1000_3406 は 0xF96 != 0 の例外経路で、通常経路の pending-delta +1 は通らない。参照 MIDI の track 1 SysEx は tick 0 / 4 / 8 / 8。これを使うと 0x6EE = 4 が具体的な候補になるが、queue payload と accumulator の対応は未確認。
- **オプションフラグ対応の再評価:** 下流の用途から `0x626 ↔ /F`（track-order table 選択）、`0x622 ↔ /XD`（SysEx 抑制）が最有力。以前の `0x622 ↔ /F` 仮説は撤回する。parser の比較 operand `0xF05` が `/XD` 文字列に対応すること、token bytes、参照生成時の実際の引数は未確認。FACTS F-069 を参照。
- **実装への影響:** v1 の汎用 ReadTempi ガードを維持。converter / tempo regression tests は変更しない。v2–v4 の既存挙動も変更なし。
- **PR:** #5 は reverse/v1-tempo-investigation で open / unmerged。直近確認時の CI status は空であり、CI 成功とは扱わない。
- **作業の種類:** ここまでの tempo 調査は静的解析。実行時トレース・実再生確認は未実施。

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
- [ ] SysEx 4件の SNG 側生成元を追い、SNG2S の `/X1`、`/X2`、`/XD` オプションとの関係を確認する。生成元を特定するまでは payload を converter にハードコードしない。 元 SNG で payload と共通先頭列 `41 10 16 12` の literal byte sequence は見つからなかったが、これは生成元を確定する証拠ではない（H-013）。
- [x] v1 専用レイアウト分岐を v2〜v4 と分離し、fixture の参照 Note-on/off 比較テストを追加。作業ブランチ `docs/ballade-version-testdata` の CI run [39253494378](https://github.com/pikachu364/glosa/actions/runs/39253494378) は Ubuntu・macOS・Windows の全 job が成功。

**完了条件:** 候補がソース解析または参照 MIDI と一致する。整合性だけを根拠に実装へ反映しない。

## T-003 — 参照 MIDI の生成条件を確認する
**優先度:** 中

- [x] リポジトリ内の `testdata/ballade/sng2gs/SNG2S.DOC` を確認し、SNG2S Ver. 3.3 の対応バージョン、timebase 48、/F・/N などの出力オプションを FACTS F-001 に整理した。
- [ ] 既存の参照 MIDI を生成した実行ファイル名・版・指定オプションを確認する。
- [ ] 参照 MIDI の作成条件が不明なら、未確認として明記する。
- [ ] +128 tick の根拠をコンダクタ情報と出力イベントから追跡する。

- [x] 静的解析で `FUN_1000_3406` が内部バッファ `0x21DC` から `FF 58` time-signature meta、`0x21E0` から `FF 51` tempo meta を出力することを確認（FACTS F-055）。`0x21E4` は別のレコード列として `0x7DE` と `FUN_1000_3B78` に接続するが、意味は未確定。
- [ ] `0x21DC` / `0x21E0` / `0x21E4` は SNG 内絶対オフセットではなく内部 far-pointer。各バッファの充填元を SNG 読込側へ逆追跡する。
- [ ] v1 `runner.MID` の tempo meta（tick 0 / 8 / 192）と、v1 conductor に現行 `ReadTempi` を適用した値列の不一致を解消する。`0x7BA` の設定元と内部 tempo buffer の各レコードを対応付ける（FACTS F-053 / F-055 / HYPOTHESES H-012）。
- [x] 作業ブランチで v1 に後続版向け `ReadTempi` を適用しないガードと回帰テストを追加。これにより誤った tempo 候補は出さないが、tick 8 / 192 の参照 tempo change は未実装。CI run [39400722489](https://github.com/pikachu364/glosa/actions/runs/39400722489) で Ubuntu/macOS/Windows の全 job が成功（FACTS F-056）。

**完了条件:** 生成条件とオフセット差の説明が、根拠付きで記録される。

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
- [x] 参照 MIDI の track chunk 順・Note-on・channel を抽出し、SNG パートへ対応付けた。ノートイベントを持つパートの channel は part-1 と一致（FACTS F-030）。
- [ ] `/F` の昇順オプションを実行し、出力 track chunk 順を検証する。SNG2S/SNG2GS 実行ファイルはリポジトリの tree に見当たらず、既存 MIDI だけでは `/F` 出力を直接確認できない。実行可能な参照環境を入手するまで未検証とする。

**完了条件:** トラックチャンク順と MIDI チャンネル／SNG パートの対応が説明でき、変更時の互換性が判定できる。

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



## T-011 — v1 tempo queue と 192-tick grid の対応付け
**優先度:** 高

- [ ] FUN_1000_33D4 の初回 record 消費から FUN_1000_08EC の queue drain まで、pending delta-time (0xF9A:0xF9C) と absolute tick (0x6F2:0x6F4) の更新をイベント出力点ごとに追う。
- [ ] grid delta 計算（uVar11=192）後の FUN_1000_618A 呼び出しについて、キュー投入時刻と実際の MIDI tick を分けて整理する。
- [x] tick 0 と tick 192 の経路を対応付ける。tick 0 は既定 500000 µs/quarter、tick 192 は grid-aligned exact tempo meta。v1/v3/v4 の cross-version values confirm the same integer path (FACTS F-071/F-072).
- [ ] tick 8 の 869,000 µs/quarter がどの queue record/consumer で出力されるかを追う。tempo slider raw value 37 → 69 BPM は確定したが、coarse tempo meta の直接の producer と queue-drain tick 8 はまだ静的に完全確定していない。
- [ ] 実行時トレースができない場合は、静的推論と参照 MIDI の観測事実を明確に分離して記録する。

**完了条件:** 3つの参照 tempo event の値・tick と SNG2S の各出力経路を根拠付きで説明できること。そこまでは converter / regression test を変更しない。


## T-012 — tempo と device-parameter queue を分離

- [x] `0x21E4` / `0x7DE` / `FUN_1000_3BA7` を FF 51 tempo path (`0x21E0`) から分離して確認。
- [x] tempo-record 側の整数計算 `(60000 / 69) * 1000 = 869000` と、192-tick grid 側の `60000000 / 69 = 869565` を静的解析で照合。参照 tick 8 / 192 の tempo 値に一致する。
- [ ] queue drain 時点の absolute tick が tick 8 であることを runtime trace で確認する。pending delta `0xF9A:0xF9C` と absolute tick `0x6F2:0x6F4` の値を、最初の queue drain 前後で追う。
- [ ] `FUN_1000_3406` の1回の scheduler call で、`0x21E0` と `0x21E4` の両方が処理される順序を整理し、`FUN_1000_08EC` の queue drain 順序を確認する。

**注意:** 数値の一致は強い静的根拠だが、tick 8 の割当は runtime 未確認。実行時トレースが済むまでは converter / regression test を変更しない。

## T-013 — preamble delta と初回 tempo queue drain の状態を確定

- [x] v1 の `FUN_1000_1C70` → `FUN_1000_1CE4` → `FUN_1000_33D4` → 初回 `FUN_1000_3406` → `FUN_1000_08EC` の呼び出し順を静的に追跡。
- [x] `FUN_1000_3DE2` が `0xF96 = 0xFFFF` にし、`FUN_1000_33D4` が初回 `FUN_1000_3406` の後で `0xF96 = 0` に戻すため、初回 `FUN_1000_3406` は通常の pending delta `+1` 経路を通らないことを確認。
- [x] 解析テキスト全体の命令行を検索し、`[0x6EE]` の29参照がすべて直接読み出しで、直接 store が見つからないことを確認。複数の event-output path で `0x1ED8:0x1EDA` への加算と `0xF9A:0xF9C` への設定に使われる（FACTS F-067）。
- [ ] `0x6EE` の実値と初期化元を特定する。data segment、別名アドレス、間接書き込み、runtime memory trace は未確認。
- [x] `FUN_1000_184A` → `FUN_1000_5EC8(input, 0xF05, 2)` の比較経路を確認。2-byte token の一致で `0x622=1`。下流用途との整合から `/XD` が最有力で、以前の `/F` 対応推定は撤回。ただし token bytes と文字列の直接対応は未確認（FACTS F-069）。
- [ ] 参照 MIDI 生成時に `/XD` が指定されたか確認し、`0x622 != 0` の pending-delta reset が実際に適用されたかを絞る。SysEx 4件の存在は `/XD` 無効を強く示唆するが、生成条件の直接証拠ではない。
- [ ] `FUN_1000_1CE4` 内の条件付き queue drain 後の `0x6F2:0x6F4` と `0xF9A:0xF9C`、続く初回 tempo drain の前後を対応表にする。可能なら runtime trace で照合する。

**注意:** 初回 `FUN_1000_3406` に通常経路の +1 を当てはめない。tick 8 が説明できるまで `SngConverter.cs` と tempo regression tests は変更しない。


## T-014 — tempo slider curve と 3/4 の MIDI 出力丸めを分離
**優先度:** 中（v1 の読み込み失敗を直すブロッカーではない）

- [x] jmplayer の仕様メモと SNG2S `FUN_1000_3406` を照合し、`rawValue * baseTempo / 64` の整数除算、1..480 の clamp、baseTempo=120 の場合の `floor(15*v/8) = 2v - ceil(v/8)` を確認（FACTS F-071）。
- [x] `FUN_1000_3406` の tempo stream reader を確認。レコードは interval/count と raw tempo value の組で、`37` は raw tempo slider value、`48/144/192` は interval fields。baseTempo 120 なら `floor(37*120/64)=69 BPM`。以前の逆の解釈を撤回（FACTS F-071）。
- [ ] SNG2S の 3/4 固有 +1 BPM 説明について、time-signature input と MIDI tempo-meta generation の間に条件分岐があるか、decompilation の全 call/data flow を調べる。
- [ ] 3/4 の SNG と SNG2S 参照 MIDI が得られた場合、slider 値ごとに実際の tempo meta と time-signature meta を比較する。現在の private fixture tree には 3/4 の参照 fixture が見つかっていないため、未確認のまま数値補正を実装しない。
- [x] v1/v3/v4 reference MIDIs show the same two integer paths: v1 869000 at tick 8 and 869565 at tick 192 (69 BPM); v3 361000 at tick 92 and 361445 at tick 192 (166 BPM); v4 260000 at tick 116 and 260869 at tick 192 (230 BPM). This confirms the values are coarse millisecond and exact microsecond representations of the same normalized tempo, not two actual BPM changes (FACTS F-071).
- [ ] Keep T-011/T-012 open for the exact queue-drain/tick placement explanation, especially why the first event lands at tick 8 and the exact-tempo event at tick 192. Keep the 3/4 +1 BPM quirk separate.

**完了条件:** slider-to-timer curve と SNG2S MIDI-export rounding を別の処理として説明し、3/4 の補正は参照 fixture で確認できるまで実装しない。
