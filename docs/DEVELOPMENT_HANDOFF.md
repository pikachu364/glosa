# Glosa 作業引き継ぎ・進捗台帳（Ballade SNG → MIDI）

最終更新: 2026-10-19  
対象リポジトリ: https://github.com/pikachu364/glosa  
この文書は、チャットを新しくしても作業を再開できるように、完了事項・確認済みの事実・未解決事項・次の手順を区別して記録する。

## 0. 最重要サマリー

**現在の優先課題は、v1 の SNG ファイルを Glosa の CLI で変換するとレイアウト判定エラーになる問題の原因究明。**

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

この比較から、既存の2つの MIDI に差があることは確認できる。ただし、イベント単位の対応付け・トラック対応・差の原因はまだ確定していない。これらの数値だけで、どちらが正しいか、または Glosa の不具合原因が何かを断定しない。

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

## 9. 新しいチャットに貼る開始メッセージ

次の文章をコピーして、新しいチャットの最初のメッセージに使う:

> Glosa リポジトリで Ballade SNG→MIDI 変換を調査しています。まず GitHub の docs/DEVELOPMENT_HANDOFF.md とリンク先の作業記録を読んでください。
>
> PR #1 と PR #3 はマージ済み。過去に dotnet test -p:Platform="Any CPU" は964件成功した記録がありますが、現在このチャットから再実行したわけではありません。v1 fixture の convert-sng は「Invalid or unsupported Ballade SNG layout.」で失敗した過去の記録があり、原因は未特定です。
>
> 現在は PC を使わず、GitHub 上のソース・資料・既存の MIDI を ChatGPT から調査しています。主要コードは src/Glosa.Core/Smf/SngConverter.cs、元実装は https://github.com/jihodaya/jmplayer/blob/main/sngmidi.cpp です。実行していないテストや再生を実施済みと扱わず、確認済み事実と仮説を分け、1ステップずつ進めてください。

次の文章をコピーして、新しいチャットの最初のメッセージに使う:

> Glosa リポジトリで Ballade SNG→MIDI 変換を調査しています。詳細は GitHub の docs/DEVELOPMENT_HANDOFF.md を読んでください。
>
> PR #1 と PR #3 はマージ済み。dotnet test -p:Platform="Any CPU" は964件成功しました。ただし v1 fixture の convert-sng は「Invalid or unsupported Ballade SNG layout.」で失敗し、原因は未特定です。SNG converter の修正成功はまだありません。
>
> Windows CMD を使っています。元実装は R:\jmplayer\sngmidi.cpp、Glosa の主要調査対象は src/Glosa.Core/Smf/SngConverter.cs です。まず元実装の buildLayout を照合するところから、一度に一手順ずつ案内してください。確認前に原因を決めつけたり、成功したと扱ったりしないでください。
