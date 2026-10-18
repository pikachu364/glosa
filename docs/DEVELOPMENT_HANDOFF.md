# Glosa 作業引き継ぎ・進捗台帳（Ballade SNG → MIDI）

最終更新: 2026-10-19  
対象リポジトリ: https://github.com/pikachu364/glosa  
この文書は、チャットを新しくしても作業を再開できるように、完了事項・確認済みの事実・未解決事項・次の手順を区別して記録する。

## 0. 最重要サマリー

**現在の優先課題は、v1 のレイアウト判定エラーの原因究明と、tie の Note-off 延長・160 ticks の開始時刻差の検証。** ノートオン数の差（3,657対2,713）は tie フラグ処理の不一致で説明できた。v1 は3候補すべてが最初のパート長検証で失敗し、v2〜v4 はそれぞれ少なくとも1候補が通る。詳細は §10。

実行したコマンド:

    dotnet run --project src/Glosa.Cli -- convert-sng testdata/ballade/v1/runner.SNG testdata/ballade/v1/runner.mid

結果:

    error: Invalid or unsupported Ballade SNG layout.

- 原因は未特定。
- 修正はまだ行っていない／修正成功は確認していない。
- テストスイートが通ることと、実際の SNG 変換が成功することは別。変換成功を主張しないこと。
- 次は元実装と Glosa のレイアウト判定を比較し、候補ごとの失敗条件を確認する。
- ユーザーは Windows CMD を使用している。PowerShell 専用コマンドは `powershell -NoProfile -Command "..."` の形で提示する。
- 一度に大量の手順を提示せず、1ステップずつ案内する。

## 1. リポジトリ・作業環境

- Glosa: https://github.com/pikachu364/glosa
- ローカル作業ディレクトリ: `R:\glosa`
- 元実装 JMPlayer: https://github.com/jihodaya/jmplayer
- 元実装のローカルクローン: `R:\jmplayer`
- 元実装コード: `R:\jmplayer\sngmidi.cpp`
- 元実装の形式資料: `R:\jmplayer\docs\formats\sng-ballade.md`
- 直近に確認されたローカルブランチ: `main`
- 直近に確認された main コミット: `8325a0d`
- 引き継ぎ文書の前回コミット: `62e93ef4fb67d279e8387d1d184d894b749cdac4`

ローカル状態は時間とともに変わるので、新しいチャットで必要になったら `git status` と `git log -1 --oneline` で再確認する。引き継ぎメモだけを根拠に、現在も同じブランチ・コミットだと決めつけない。

## 2. 完了したこと

### GitHub / PR
- PR #1「Add Ballade SNG to SMF conversion」はマージ済み。  
  https://github.com/pikachu364/glosa/pull/1
- PR #3「Document Ballade SNG version test data」は main にマージ済み。  
  https://github.com/pikachu364/glosa/pull/3
- PR #3 は base main、head `docs/ballade-version-testdata`。4 commits、6 files changed、66 additions、0 deletions と確認した。
- PR #3 の CI は全て green とユーザーが確認した。
- PR #2 は次のリンク。内容・現在の状態をこのメモでは詳細に検証していない。  
  https://github.com/pikachu364/glosa/pull/2
- main に SNG converter ソースと fixture ファイルが含まれる状態まで pull したことを確認した。

### ローカルテスト
次のコマンドでテスト成功を確認した:

    dotnet test -p:Platform="Any CPU"

結果:
- 964 passed
- 0 failed
- 0 skipped
- build success

注意: `dotnet test` 単体では、環境設定によって次のエラーになった:

    error MSB4126: specified solution configuration "Debug|x64" is invalid

この環境では `-p:Platform="Any CPU"` を付けることでテストが通った。

### CLI の確認
次のコマンドで CLI のヘルプを確認した:

    dotnet run --project src/Glosa.Cli -- sng --help

表示された汎用ヘルプには次が含まれていた:

    glosa convert-sng <file.sng> [output.mid]

## 3. 未完了・やり残し

### 最優先: v1 のレイアウト判定エラーを解決
- [ ] `TryLayout` のどの候補・条件で失敗しているか特定する。
- [ ] Glosa と JMPlayer の `buildLayout`、および little-endian 読み取りヘルパーを比較する。
- [ ] 必要なら診断ログを一時的に加え、失敗した条件を明示する。
- [ ] 最小限の修正を行う。
- [ ] v1 の変換コマンドを再実行し、成功を確認する。
- [ ] 生成した MIDI のサイズ・形式・読み戻しを確認する。
- [ ] 可能なら MIDI プレイヤーで実際に再生し、イベントや音が期待どおりか確認する。

### 4バージョンすべての実データ検証
- [ ] v1 `runner.SNG` を変換・検証する。
- [ ] v2 `SHIZUME0.SNG` を変換・検証する。
- [ ] v3 `START__0.SNG` を変換・検証する。
- [ ] v4 `BOURREE0.SNG` を変換・検証する。
- [ ] 各バージョンの検証結果を既存の fixture 文書に正確に反映する。
- [ ] エラー・警告・未検証事項を文書に残す。

### 品質・GitHub
- [ ] 修正後に `dotnet test -p:Platform="Any CPU"` を再実行する。
- [ ] 関連する SNG テストが存在するか確認し、不足していれば回帰テストを追加する。
- [ ] 修正とテストをブランチにコミットし、必要なら PR を作成する。
- [ ] CI が成功したことを確認する。
- [ ] 完了した作業・未完了事項・検証結果をこの文書と関連文書に更新する。

## 4. 判明した事実

### v1 fixture
- パス: `testdata/ballade/v1/runner.SNG`
- サイズ: 85,897 bytes
- SHA-256: `e61b8b0315a3752dfa3d3546d5a436df0d4b0421201183c21ce4bf1780556e15`
- 先頭シグネチャ: `BALLADE SONG`
- ヘッダー文字列: `BALLADE SONG Ver. 1.00`
- バージョン文字列はオフセット 18 にあることを確認した。
- バージョンの4バイト文字列は `1.00`。
- サイズ表の開始位置は `0x46`。
- v1/v2/v3 は10パート、v4 は16パートとして扱う実装・資料を確認した。

v1 サイズ表の先頭値（32-bit little-endian）:
- `0x46`: 1394
- `0x4A`: 1230
- `0x4E`: 28398
- `0x52`: 10914
- `0x56`: 5984
- `0x5A`: 4482
- `0x5E`: 4764
- `0x62`: 8766
- `0x66`: 2766
- `0x6A`: 6856
- `0x6E`: 9362

### v1 のヘッダー候補調査
- `tableEnd = 0x46 + 4 * (10 + 1) = 114`。
- JMPlayer の資料・実装で候補として確認した値は `tableEnd+94`、`tableEnd+2`、`tableEnd`。
- CMD から実行した調査で `headerEnd=116` とした場合、最初のパート開始位置は1510となった。
- その位置から `used=0`、`ctl=53248`、`idx=1` を読んだ。
- `ctl=53248` は期待される4バイト単位の長さとしては不自然に見える。ただし、これだけで候補が間違いだと断定してはいけない。開始位置やオフセットの前提を再確認する必要がある。
- オフセット `0x6E`～`0x81` の一部バイトを表示したが、配置を確定するには至っていない。
- 一度診断 PowerShell コマンドで配列演算エラーが出た。これは調査コマンド側のエラーで、SNG ファイルが壊れたことを示すものではない。

### JMPlayer のレイアウト判定（これまでにソースで確認した内容）
`R:\jmplayer\sngmidi.cpp` の `buildLayout` を読み、次を確認した:
- ファイルサイズとシグネチャを確認する。
- バージョン文字列を読み、v1/v2/v3 は10パート、v4 は16パートにする。
- サイズ表は `0x46` から32-bit little-endian で読む。
- 各パートサイズの計算に81を足す。
- ヘッダー候補は `tableEnd+94`、`tableEnd+2`、`tableEnd`。
- パート先頭から `+112`、`+114`、`+116` の位置で `used`、`ctl`、`idx` を読む。
- `used % 8 == 0`、`ctl % 4 == 0`、`used + ctl + idx <= capacity` を検証する。
- 元実装のコメントでは、一部の破損ファイルでブロックの終端がファイル末尾を超えることを許容するが、ブロック開始位置はファイル内である必要があると説明している。

Glosa の `src/Glosa.Core/Smf/SngConverter.cs` にある `TryLayout` も上記に近い実装に見える。現段階では、両実装の差分が原因だと確定していない。

## 5. SNG fixture 一覧

| Version | File | Size | SHA-256 | 実変換・再生の検証 |
|---|---|---:|---|---|
| v1 | `testdata/ballade/v1/runner.SNG` | 85,897 | `e61b8b0315a3752dfa3d3546d5a436df0d4b0421201183c21ce4bf1780556e15` | 変換失敗。原因未特定 |
| v2 | `testdata/ballade/v2/SHIZUME0.SNG` | 75,597 | `eb6e1eb5bd1aeb36acb7b53a89fb8409b09e7d3988a45847309da105622272ee` | 未検証 |
| v3 | `testdata/ballade/v3/START__0.SNG` | 82,391 | `b4b719cec15139e411c94c34b05ca698e9432b9ccad308a808f08a648256cbe4` | 以前 48,923-byte の MIDI ができた記録あり。読み戻し・再生検証は未完了 |
| v4 | `testdata/ballade/v4/BOURREE0.SNG` | 49,517 | `a1eb69b4735b809e35c9c368776e9dae621e30405dc5a9c717133654c5f7b715` | 未検証 |

「MIDI ファイルが生成された」ことと「MIDI の内容が正しく、再生できる」ことを分けて記録する。

## 6. 関係するファイル・コード

Glosa:
- `src/Glosa.Core/Smf/SngConverter.cs`: SNG レイアウト判定・変換の主要な調査対象。
- `src/Glosa.Cli`: CLI の `convert-sng` コマンド。
- `testdata/ballade/v1/runner.SNG`
- `testdata/ballade/v2/SHIZUME0.SNG`
- `testdata/ballade/v3/START__0.SNG`
- `testdata/ballade/v4/BOURREE0.SNG`
- バージョン別 fixture 文書は PR #3 の変更内容を確認すること。
- `docs/DEVELOPMENT_HANDOFF.md`: この引き継ぎ文書。

JMPlayer:
- `R:\jmplayer\sngmidi.cpp`: 元実装。
- `R:\jmplayer\docs\formats\sng-ballade.md`: SNG 形式の説明。CMD の `type` で表示すると文字化けして見えることがあったが、内容の一部は読めた。

## 7. 次に行う手順（未実施）

**次の一手から始める。まだ修正を急がない。**

1. 元実装の関数位置を再確認する:

       findstr /n /c:"buildLayout" /c:"tableEnd" /c:"headerEnd" R:\jmplayer\sngmidi.cpp

2. 該当する `buildLayout` 全体を表示し、必要なら `rd16` / `rd32` 相当の読み取り関数も表示する。
3. Glosa の `TryLayout` と元実装を条件単位で比較する。
4. 3候補それぞれについて、最初に失敗する条件を表示する短い診断を作る。PowerShell の配列式でエラーが起きた前回のコマンドをそのまま再利用せず、スカラー値を明示して一つずつ検証する。
5. 最初に失敗する条件が分かってから、サイズ表の解釈・オフセット・読み取り幅・候補順・境界チェックのどこが違うか判断する。
6. 原因が特定できたら修正し、同じ v1 コマンドを再実行する。
7. 変換後に MIDI の存在・サイズを確認し、テストを実行する。
8. v1 の成功後、v2/v3/v4 の順に実データで検証する。各段階で結果を記録する。

## 8. 2026-10-19 追加記録（PCなしでの継続調査）

### 作業方針の更新
- ユーザーは当面 PC を使わず、ChatGPT から GitHub 上のコード・資料・記録を確認して調査を続けたい。
- 以後は GitHub コネクタでファイルを直接読み、必要な変更はユーザーの明示依頼に沿って GitHub に反映する。ユーザーにローカル CMD コマンドの実行を前提とした案内をしない。
- ローカル環境にしかない情報は、GitHub 上で確認できた事実と混同しない。実行・再生していないテストを実施済みと書かない。
- 一度に大量の操作を求めず、進捗と次の一手を小さく区切る。

### v2 の既存 MIDI 比較で確認した数値
対象:
- SNG: `testdata/ballade/v2/SHIZUME0.SNG`
- SNG2GS 参照 MIDI: `testdata/ballade/v2/sng2gs/SHIZUME0.MID`
- Glosa 出力 MIDI: `testdata/ballade/v2/glosa-test-convert/SHIZUME0.mid`

| 指標 | SNG2GS 参照 MIDI | Glosa MIDI |
|---|---:|---:|
| ファイルサイズ | 30,262 bytes | 45,380 bytes |
| SMF format | 1 | 1 |
| トラック数 | 12 | 11 |
| TPQ | 48 | 48 |
| Note-on 数 | 2,713 | 3,657 |
| テンポイベント数 | 424 | 439 |
| Program Change 数 | 62 | 57 |
| 最後のノート tick | 25,088 | 25,632 |

後続の静的なイベント照合で、ノートオン数の差は tie フラグの扱いで説明できることを確認した（§10）。一方、ノート開始時刻には一律160 ticksの差があり、ノートオフ／タイ連結の完全な一致と SNG2S/SNG2GS の実行ファイル同一性は未確認。

### SNG2GS v3.3 のオプションメモ
既存の解析資料に記載されたオプション:
- `/0`: SMF Format 0
- `/1`: SMF Format 1（既定）
- `/F`: パート／トラック割り当て順を変更
- `/LA`: PCM 音色を LA 音色に変換
- `/N`: ノートデータがないパートも省略しない
- `/R`: Running Status を無効化
- `/X1`、`/X2`: Exclusive データの出力先を変更
- `/XD`: Exclusive データを無効化

比較時には、参照 MIDI がどのオプションで生成されたものか分かる場合に記録する。オプション条件が不明なら、結果を単純比較する前に不明と明記する。

### 2026-10-19 の作業記録へのリンク
この日の調査メモを GitHub に追加した:
- [2026-10-19 作業記録](https://github.com/pikachu364/glosa/blob/docs/ballade-version-testdata/testdata/ballade/%E4%BD%9C%E6%A5%AD%E8%A8%98%E9%8C%B2/2026-10-19.md)
- ブランチ: `docs/ballade-version-testdata`
- コミット: `1f8da8c27a1ac838efbde64e905bc5cd7cb5030e`

### 次に進める調査
1. GitHub 上の `SngConverter.cs`、形式資料、作業記録を再読して、現在の実装と確認済み事項を揃える。
2. v2 の MIDI をイベント単位で比較できる範囲から分析する。まずトラック名・トラック数・テンポ・Program Change・Note-on/off を区別して確認する。
3. ノート数の差を、トラック対応、同音連結／ノート延長、イベント重複、空パート扱いなどの仮説に分ける。仮説は検証前に結論として書かない。
4. v1 のレイアウト判定問題は未解決のまま維持し、ソースの条件を GitHub 上で再確認する。追加データや実行結果が得られない場合、その制約を明示して静的解析を進める。
5. 解析結果が固まったら、この文書と日付別作業記録を更新する。

## 10. 2026-10-19 追加調査：レイアウト判定と MIDI ノート数

この節は GitHub 上のファイルを静的に読み取って得た結果。Windows 上での変換実行・MIDI 再生・SNG2S/SNG2GS 実行ファイルの照合は行っていない。

### レイアウト判定の追加確認

`src/Glosa.Core/Smf/SngConverter.cs` の `TryLayout` と同じ主要条件（バージョン別パート数、サイズ表、3つの `headerEnd` 候補、各パートの `used / ctl / idx` 検証）を、GitHub にある v1〜v4 の SNG ファイルのバイト列に適用した。

- v1 `runner.SNG`：`headerEnd=208`、`116`、`114` の全候補が、最初のパートの長さフィールド検証で失敗。
  - `headerEnd=116`：part 1 start=1510、size=1311、`used=0`、`ctl=53248`、`idx=1`、capacity=1153。`used + ctl + idx > capacity` で失敗。
  - `headerEnd=114`：part 1 の `idx=53248` となり capacity を超過。
  - `headerEnd=208`：part 1 の `ctl=1036`、`idx=17920` となり capacity を超過。
- v2 `SHIZUME0.SNG`：`headerEnd=116` と `114` の候補が検証を通過。
- v3 `START__0.SNG`：`headerEnd=208` の候補が検証を通過。
- v4 `BOURREE0.SNG`：`headerEnd=232` の候補が検証を通過。

**結論：** 現行の v1 エラーは、シグネチャやバージョン認識の段階ではなく、レイアウト候補のパート長検証で起きることを静的に再現できた。v1 の形式差、サイズ表の解釈、パートヘッダーの構造差などが候補だが、原因はまだ特定できていない。条件を単純に緩めて通す修正は行わない。

### SNG2S と SNG2GS の名称・実行ファイルの注意

- 既存の逆解析ファイル `testdata/ballade/sng2gs/sng2gs解析データ.txt` のプログラム識別情報は `SNG2S.EXE`、`SNG2S Ver 3.3`。
- JMPlayer の形式資料も SNG2S 3.3 を参照 MIDI 作成に使用したと記載している。
- ただし、ユーザーの対象である SNG2GS と、逆解析資料の SNG2S が同じ実行ファイルかは未確認。名前が似ていることだけで同一視しない。

### v2 のノート数差に関する追加分析

対象ファイル:
- SNG: `testdata/ballade/v2/SHIZUME0.SNG`
- 参照 MIDI: `testdata/ballade/v2/sng2gs/SHIZUME0.MID`
- Glosa MIDI: `testdata/ballade/v2/glosa-test-convert/SHIZUME0.mid`

SNG を現行の JMPlayer/Glosa と同じレイアウト前提で静的に読み、各パートの 8-byte ノートレコードを数え、現行 tie 処理と同じ条件で連結されるノートをまとめた結果、Glosa MIDI のノートオン数とパートごとに一致した。

| SNG パート | ノートレコード数（gate > 0） | tie 連結数 | Glosa と一致するノートオン数 |
|---:|---:|---:|---:|
| 2 | 255 | 26 | 229 |
| 3 | 774 | 2 | 772 |
| 4 | 679 | 2 | 677 |
| 5 | 592 | 25 | 567 |
| 6 | 371 | 2 | 369 |
| 7 | 724 | 23 | 701 |
| 8 | 209 | 27 | 182 |
| 9 | 179 | 19 | 160 |
| **合計** | **3,783** | **126** | **3,657** |

この追加照合により、ノートオン数の差の直接原因を特定できた。

| 指標 | 件数 |
|---|---:|
| SNG の gate > 0 のノートレコード | 3,783 |
| うち bit 7（tie／継続フラグ）が立っているレコード | 1,070 |
| bit 7 が立っていないレコード | 2,713 |
| SNG2GS 参照 MIDI の Note-on | 2,713 |
| 現行 Glosa MIDI の Note-on | 3,657 |
| Glosa が参照 MIDI より多く出す Note-on | 944 |

SNG2GS 参照 MIDI の全2,713件は、SNG 内の **bit 7 が立っていないノートレコード**と、音高・ベロシティが一致する。時刻も一律160 ticksの差で一致し、source tick = reference MIDI tick + 160 となる。参照 MIDI の Note-on 数は、bit 7 が立っていないレコード数と完全一致する。

現行 Glosa の tie 処理は、直前に処理したノートだけを見て、音高と終了 tick が一致した場合に限り連結する。そのため、同じ和音内で別の音が間に入る tie など、多くの bit 7 付きレコードを新しい Note-on として出力してしまう。現行 Glosa は tie 付き1,070レコードのうち126件だけを連結し、残る944件を Note-on として出力している。これは SNG2GS 参照 MIDI とのノートオン数の差944件と一致する。

**結論：ノートオン数の差の直接原因は、Glosa の tie／継続フラグ処理が狭すぎること。** 修正方針は、bit 7 が立ったレコードを新しい Note-on として出さず、同じ音高の継続先 Note-off を正しく延長できるようにすること。実装前に、SNG2GS 参照 MIDI の Note-off tick と複数音の tie 連結を追加照合して、具体的な延長規則を確認する。

開始時刻の一律160 ticks差は別問題として残る。これは SNG2GS 参照 MIDI と Glosa の時間原点／先頭余白の違いを示すが、原因はまだ確定していない。参照 MIDI の生成オプションも未確認なので、オプション依存の差がないかを記録しておく。

### Note-off／tie 延長の追加照合

SNG の tie 付きレコードを、音高ごとに同じ音の終了 tick と次の tie 開始 tick が一致した場合に延長する簡易モデルで比較したところ、参照 MIDI の Note-off tick は 2,713 件中 2,676 件で一致した。残る37件は、同音高の重複・重なりや tie の対応規則を完全にはモデル化していないため、現時点で不一致原因を断定しない。この結果は、tie 処理の方向性を支持するが、実装規則を確定するには残件の確認が必要。

### 次に進める調査（更新）

1. v1 のバイト配置をさらに調べ、正しい境界・パートヘッダーの位置を裏付ける。推測したオフセット変更だけで実装を修正しない。
2. 逆解析資料の `SNG2S.EXE` と、実際に比較対象としている SNG2GS の実行ファイルが同一か確認する方法を整理する。バイナリが GitHub にない場合は未確認として残す。
3. Note-off の残り37件の差を確認し、同音高の重複／重なりと複数音の tie を正しく扱う規則を確定する。
4. SNG2GS と Glosa の開始時刻差160 ticksの理由を調べる。参照 MIDI の生成オプション条件も未確認として記録する。
5. 実行・再生を行っていないことを明記し、静的解析と実行検証を混同しない。
6. v1 の原因が特定された段階で、この引き継ぎ文書と日付別作業記録を更新し、必要な場合のみ実装・回帰テストに進む。

---

## 9. 新しいチャットに貼る開始メッセージ

次の文章をコピーして、新しいチャットの最初のメッセージに使う:

> Glosa リポジトリで Ballade SNG→MIDI 変換を調査しています。まず GitHub の docs/DEVELOPMENT_HANDOFF.md とリンク先の作業記録を読んでください。
>
> PR #1 と PR #3 はマージ済み。過去に dotnet test -p:Platform="Any CPU" は964件成功した記録がありますが、現在このチャットから再実行したわけではありません。v1 fixture の convert-sng は「Invalid or unsupported Ballade SNG layout.」で失敗した過去の記録があり、原因は未特定です。
>
> 現在は PC を使わず、GitHub 上のソース・資料・既存の MIDI を ChatGPT から調査しています。主要コードは src/Glosa.Core/Smf/SngConverter.cs、元実装は https://github.com/jihodaya/jmplayer/blob/main/sngmidi.cpp です。実行していないテストや再生を実施済みと扱わず、確認済み事実と仮説を分け、1ステップずつ進めてください。
