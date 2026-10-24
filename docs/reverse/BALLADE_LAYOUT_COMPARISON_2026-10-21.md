# Ballade SNG の版別レイアウトと MIDI 比較メモ

**調査日:** 2026-10-21  
**対象:** `src/Glosa.Core/Smf/SngConverter.cs`、`testdata/ballade/v1`〜`v4` の SNG / MIDI fixture、既存の reverse 文書  
**状態:** 静的ソース確認と既存 fixture の比較メモ。今回、変換コードの変更・ビルド・テスト実行・MIDI 再生は行っていない。

## 要約

1. v1 の `.SNG` は、現在の `TryLayout()` が検査する共通レイアウト候補では受理されない。v1 fixture 2件で、実データと整合する候補はテーブル直後の `headerEnd = tableEnd + 2`、パートサイズ `entry + 80`、ノート長フィールド `+115`、コントローラ長フィールド `+117`、13個の相対 seek 値 `+119..+143`、ノートレコード開始 `+145` である。
2. 同じ候補計算を v2〜v4 の fixture に適用すると、現在の `+81` サイズ・共通オフセットの検査が通る。したがって、ひとつの定数を変更するのではなく、版ごとのレイアウト定義を分けて扱うのが安全である。
3. 保存済みの v2/v3 の Glosa MIDI は、SNG2GS 参照 MIDI より Note-on/off 数が多い。ただし、リポジトリ内 README はこれらを Tie 修正前の出力と説明しているため、現行コードの不具合を示す証拠としては使えない。現行コードから再生成して比較する必要がある。
4. v4 の保存済み MIDI はノートイベントの比較で一致する一方、トラック数と tempo イベントには差がある。
5. 既存の `docs/reverse/NEXT_STEPS.md` の T-002 は v1 専用対応を作業ブランチ上で完了した記録として読めるが、その実装を含む PR #2 は closed / unmerged で、現行 `main` には取り込まれていない。詳細は「履歴調査結果」を参照する。
6. SNG2S Ver 3.3 と MIMPIV427 の読み込み関数は、`BA` 識別子・offset `0x12` の版 byte・版別ブロック長・パート数の選択が一致する。SNG2S は別系統の有力な照合対象だが、コード共有や全フィールドの同一性は未証明である。詳細は「SNG2S.EXE と MIMPIV427 の対応」を参照する。

## 1. 現行コードのレイアウト判定

対象: [SngConverter.cs](../../src/Glosa.Core/Smf/SngConverter.cs)

`TryLayout()` は、ヘッダーの offset 18 にある版文字列からパート数を選び、テーブル終端を次で計算する。

- v1.00 / v2.00 / v3.00: 10 パート
- v4.00: 16 パート
- `tableEnd = 0x46 + 4 * (parts + 1)`

その後、`headerEnd` の候補を `tableEnd + 94`、`tableEnd + 2`、`tableEnd` の順に試し、全候補に対して以下を共通適用する。

- パートサイズ: `entries[p] + 81`
- ノート長: `partStart + 112`
- コントローラ長: `partStart + 114`
- 追加値: `partStart + 116`
- ノートデータ開始: `partStart + 158`

このロジックには v1 専用の分岐がない。

## 2. v1 fixture で確認した配置

fixture は private testdata リポジトリの `testdata/ballade/v1` にあるファイルを対象とした。今回取得したファイルサイズは、以前に会話内で報告されたローカルファイルサイズとは異なるため、同一ファイルと仮定しない。

| ファイル | 今回確認したサイズ |
|---|---:|
| `TEST.SNG` | 9,089 bytes |
| `runner.SNG` | 85,897 bytes |

v1 のテーブル終端は `0x72`（114）で、テーブル直後の2 byte を越えた `headerEnd = 0x74`（116）がパート配置と整合する。v1 では次の構造が fixture の全10パートで整合した。

| 項目 | v1 の候補 |
|---|---:|
| パートサイズ | `entries[p] + 80` |
| ノートデータ長フィールド | `partStart + 115` |
| コントローラデータ長フィールド | `partStart + 117` |
| 13 個の相対 seek 値 | `partStart + 119 .. +143` |
| ノートレコード列の開始 | `partStart + 145` |

13 個の値を現在位置基準の相対 seek 距離として累積すると、各パートの次の情報バッファ候補 `+65` に到達し、最後のパートではファイル EOF と一致した。これは fixture 内の配置整合性を示すが、13 個それぞれの意味・用途を完全に解明したことを意味しない。

### TEST.SNG

テーブル値:

`[1528, 222, 4638, 222, 222, 222, 222, 222, 222, 222, 166]`

上記 v1 配置候補で計算したパート開始位置:

`[1644, 1946, 6664, 6966, 7268, 7570, 7872, 8174, 8476, 8778]`

パートサイズ:

`[302, 4718, 302, 302, 302, 302, 302, 302, 302, 246]`

最後の seek 位置は EOF（9,089）と一致した。

### runner.SNG

テーブル値:

`[1394, 1230, 28398, 10914, 5984, 4482, 4764, 8766, 2766, 6856, 9362]`

上記 v1 配置候補で計算したパート開始位置:

`[1510, 2820, 31298, 42292, 48356, 52918, 57762, 66608, 69454, 76390]`

パートサイズ:

`[1310, 28478, 10994, 6064, 4562, 4844, 8846, 2846, 6936, 9442]`

最後の seek 位置は EOF（85,897）と一致した。

## 3. 版別の候補検査結果

| 版 | パート数 | 整合した候補 | 現行 `TryLayout()` の共通検査 |
|---|---:|---|---|
| v1 | 10 | `tableEnd + 2`、サイズ `+80`、旧版フィールド配置 | 失敗 |
| v2 | 10 | `tableEnd + 2`、サイズ `+81` | 通過 |
| v3 | 10 | `tableEnd + 94`、サイズ `+81` | 通過 |
| v4 | 16 | `tableEnd + 94`、サイズ `+81` | 通過 |

この表は fixture に対する候補検査結果であり、すべてのファイルに対する完全な仕様保証ではない。

## 4. 保存済み MIDI の比較

対象の参照 MIDI は `sng2gs` ディレクトリ、Glosa 側の保存済み MIDI は `glosa-test-convert` ディレクトリにあるものを比較した。Note-on/off 数は、MIDI の Note-on と velocity 0 の Note-on を含む Note-off 相当イベントを数えた値。

| 版 | 曲 | SNG2GS 参照のノート数 | 保存済み Glosa のノート数 | 観測 |
|---|---|---:|---:|---|
| v1 | `TEST.MID` | 393 | — | 保存済み Glosa 出力との比較対象をこの表では扱わない |
| v1 | `runner.MID` | 3,295 | — | v1 レイアウト確認に使った既存参照 |
| v2 | `SHIZUME0.MID` | 2,713 | 3,657 | Glosa 側が 944 件多い |
| v3 | `START__0.MID` | 5,362 | 5,489 | Glosa 側が 127 件多い |
| v4 | `BOURREE0.MID` | 1,721 | 1,721 | ノート数は一致 |

**重要な制約:** v2/v3 の `glosa-test-convert` README は、保存済み MIDI が Tie 修正前の出力であると記載している。したがって、ここで示した件数差を現行 `main` の変換結果に帰属させてはならない。現行ソースから再変換し、参照 MIDI と比較する必要がある。

### v4 の追加観測

保存済み v4 MIDI では、ノートイベントをチャンネル・音高・開始 tick・長さ・velocity で対応付けた比較において、ノートイベントは一致した。一方で次の差がある。

- トラック数: 参照 18、Glosa 17
- tempo イベント数: 参照 24、Glosa 23
- tempo 値にも差がある

従って「ノートが一致する」ことだけでは、v4 の MIDI 全体が完全互換とは言えない。

## 5. 既存 reverse 文書との整合性 — 履歴調査結果

この不一致は、履歴を調べた範囲では次のように説明できる。

- [PR #2](https://github.com/pikachu364/glosa/pull/2) は v1 専用の `+80` レイアウト、旧版ノートタイマー処理、controller/program/pitch-bend の状態抑制、参照 MIDI を使うテストを含む。
- PR #2 の head は `docs/ballade-version-testdata`、base は `feature/ballade-sng`。GitHub 上の PR 状態は **closed / unmerged**。
- 同ブランチの `SngConverter.cs` には `legacyV1` 分岐があり、`entry + 80`、長さフィールド `+115/+117`、ノート列 `+145` を使用する。v1 の参照イベント比較テストも同ブランチに存在する。
- PR #2 の説明には、v1 Note-on 3,295 件・Note-off 3,295 件の照合、controller 系候補 1,966 件から同一状態の繰り返しを抑えて参照 MIDI 1,595 件と一致した比較、および関連 CI run が記録されている。これらは静的解析・オフライン比較と当該ブランチの CI の記録であり、SNG2S の runtime trace や現行 `main` の検証ではない。
- 一方、現在の `main` の `SngConverter.cs` には v1 専用分岐がなく、共通 `+81` 候補のみがある。したがって、NEXT_STEPS.md の T-002 は v1 対応を作業ブランチ上で完了した記録としては読めるが、現在の `main` にそのコードが含まれていることを意味しない。
- `main` と `docs/ballade-version-testdata` の比較は diverged（head 側 68 commits ahead、350 behind）で、単純なブランチ全体比較では v1 変更以外も多数含まれる。PR #2 が未マージであることは確認できたが、閉じられた理由は取得した PR 情報からは確認できない。

**結論:** 文書と現行コードの差は、少なくとも「v1 専用実装が含まれる作業ブランチ」と「その実装を含まない現行 main」の違いで説明できる。変更が main へ取り込まれた後に戻された、という仮説を支持する必要はない。PR #2 が未マージのままである理由は未確認であり、推測しない。

## 6. SNG2S.EXE と MIMPIV427 の対応

参照した SNG2S 側の解析資料: [sng2gs解析データ.txt](https://github.com/pikachu364/glosa-private-data/blob/main/testdata/ballade/sng2gs/sng2gs%E8%A7%A3%E6%9E%90%E3%83%87%E3%83%BC%E3%82%BF.txt)。資料中の実行ファイル文字列は **SNG -> Standard Midi File Converter SNG2S Ver 3.3**、著作権表示は **M.Saito (Master-net AAD102), 1991–1993** となっている。したがって、これは Ballade `.SNG` を標準 MIDI ファイルへ変換する別プログラムの静的解析資料であり、MIMPIV427 の解析と相互照合する価値がある。

### 読み込み入口と共通点

- SNG2S 側の候補入口は `FUN_1000_272a @ 1000:272a`。入力ファイルから最初に `0x19`（25）byte を読み、先頭2 byte が `'B'`, `'A'` であることを検査する。読み込んだ先頭バッファ内の offset `0x12`（18）の byte を使って版を選び、版別の設定値を初期化している。
- MIMPIV427 側の読み込み関数 `FUN_1000_22fc @ 1000:22fc` も、最初に `0x19` byte を読み、`BA` の識別子を検査し、offset `0x12`（18）の版 byte で分岐する。
- 両方の解析で確認できる版別設定は一致する。

| SNG 版 | SNG2S の分岐結果 | MIMPIV427 との対応 |
|---|---|---|
| v1（版 byte が `'2'` 未満） | mode 0、10 パート、`0x42` byte の版別ブロック長 | mode 0、10 パート、`0x42` |
| v2（版 byte が `'2'`） | mode 1、10 パート、`0x4e` byte | mode 1、10 パート、`0x4e` |
| v3（版 byte が `'3'`） | mode 2、10 パート、`0xaa` byte | mode 2、10 パート、`0xaa` |
| v4（版 byte が `'4'`） | mode 3、16 パート、`0xaa` byte | mode 3、16 パート、`0xaa` |

ここでいう「ブロック長」は、逆コンパイル結果で版別に選択される読み込み長・設定値を指す。値の一致だけで、そのバッファ内の全フィールドの意味や後段の処理まで同一と確定したわけではない。

### この一致から言えること／言えないこと

**言えること:** SNG2S と MIMPIV427 は、少なくとも Ballade ファイルの識別、版 byte の位置、v1〜v4 の区分、10/10/10/16 パート数、版別ブロック長の選択という点で強く対応している。SNG2S は、MIMPIV427 とは別の処理経路から `.SNG` のレイアウトを照合するための有力な参照実装である。特に v1 の旧形式を追う際、MIMPIV427 単独の逆コンパイルよりも比較根拠を増やせる。

**まだ言えないこと:** 同じ作者であることと上記の一致は、実行ファイル間でコードが直接共有・複製された証明ではない。また、SNG2S 側の各データ読み込み先と MIMPIV427 側のバッファの完全な対応、13 個の v1 seek 値の個別用途、tempo/meta/SysEx の生成条件は、まだ一対一に照合できていない。両プログラムは用途も異なり、SNG2S は SMF 変換、MIMPIV427 は MIMPI の再生処理を含む。
## 7. 調査上の結論と残課題

### この調査で確認できたこと

1. v1 の2 fixture で、`headerEnd = tableEnd + 2`、`entry + 80`、`+115/+117` の長さフィールド、`+119..+143` の13個の相対 seek 値、`+145` のノート列という候補が内部整合する。
2. SNG2S の静的命令列では、旧版パスが80-byte情報バッファを読み、13個の16-bit値を DOS の現在位置基準 seek に渡す。全10パートで累積 seek 先が次の情報バッファ候補 `+65` に一致し、最後は EOF となる。
3. 旧版の Note-on/off 再構成モデルは、`runner.SNG` と参照 `runner.MID` の Note-on 3,295件・Note-off 3,295件を照合した記録があり、作業ブランチの回帰テストも CI 成功記録を持つ。ただし実行時トレースではない。
4. v1 の controller/program/pitch-bend 状態イベントも、繰り返される同一状態の抑制と tick mapping を用いたオフライン比較が参照 MIDI と一致した記録がある。現行 `main` の出力が一致することは未確認。
5. 現行 `main` に v1 専用コードがない主な履歴上の説明は、対応コードを含む PR #2 が closed / unmerged であること。

### 未解決の事項（今回の調査では解決したと扱わない）

- 13個の seek 値それぞれが、後段のどのデータ・バッファに対応するかという意味論。
- v1 の tempo/meta イベント。PR #2 の説明自体も v1 tempo/meta を未検証としている。
- v1 参照 MIDI を生成した SNG2S の正確なバージョンとオプション。
- SMF の物理トラック順、track-name / conductor / SysEx track の生成元と互換性。
- v2/v3 の保存済み Glosa MIDI は Tie 修正前と説明されているため、現行 main の出力比較には再生成が必要。
- v4 の保存済み MIDI における tempo イベント数・値、トラック数の差の原因。
- 実行時トレースおよび実再生による確認。

## 8. 推奨する次の作業

1. [ ] 残件は [NEXT_STEPS.md](NEXT_STEPS.md) の T-017 に集約済み。T-011〜T-013 の完了済み静的解析を繰り返さず、未解決の queue state / SysEx mapping のみ扱う。
2. [ ] 13 seek 値の個別の用途を、読み込み先バッファと後続の参照箇所に結びつける。用途を特定できない値には意味の名前を付けない。
3. [ ] 現行 `main` の変換結果を比較する必要が生じたら、別途明示的に実行許可を得てから再生成・テストする。今回の調査範囲では実行していない。
4. [ ] SNG2S の `FUN_1000_272a` と MIMPIV427 の `FUN_1000_22fc` の後続読み込みを、フィールド単位で対照し、同じバイト範囲を参照しているか確認する。
5. [ ] PR #2 が未マージの理由は、追加の PR timeline や repository activity から根拠が得られた場合にのみ追記する。

## 検証範囲

- 実施: GitHub 上のソースと文書の静的確認、fixture のレイアウト計算、保存済み MIDI のイベント集計・比較。
- 未実施: 現行ソースの変換実行、ビルド、単体テスト、CI、MIDI 再生、SNG2GS 実行条件の再現。
- 本文の「整合した」「一致した」は上記の fixture と比較方法の範囲に限る。未実施の検証を完了済みとは扱わない。

## 追加調査（2026-10-22）：SNG2S と MIMPIV427 の読み込み処理

### 読み込み順序

SNG2S の `FUN_1000_272a` と MIMPIV427 の `FUN_1000_22fc` は、25-byte の初期読込後に `BA` と版 byte を確認する。その後の読み込み順は、35 bytes、10 bytes、版別の44または68 bytes、1 byte、版別の66/78/170 bytesで対応している。読み込み先アドレスは異なるため、順序の一致だけで全フィールドの意味が確定するわけではない。

| 版 | mode | 版別ブロック長 | パート数 | パート情報内の2つの長さ値 | seek 系値の開始位置 | seek 系値数 |
|---|---:|---:|---:|---|---:|---:|
| v1 | 0 | 0x42 | 10 | 0x32, 0x34 | 0x36 | 13 |
| v2 | 1 | 0x4e | 10 | 0x23, 0x25 | 0x27 | 13 |
| v3 | 2 | 0xaa | 10 | 0x23, 0x25 | 0x27 | 19 |
| v4 | 3 | 0xaa | 16 | 0x23, 0x25 | 0x27 | 19 |

パート情報内のオフセットは80-byte情報バッファの先頭からの相対値である。2つの長さ値はメモリ確保・読み込み処理に渡され、後続の seek 系値は seek 処理に渡される。v1 は v2〜v4 とフィールド位置が異なり、v3/v4 では seek 系値が19個になることが逆コンパイル結果から確認できた。個々の値の意味と seek の基準位置は未確定である。

### v1 専用レイアウトとの対応

旧ブランチ `docs/ballade-version-testdata` の `SngConverter.cs` は、v1 のパート開始位置を `start` としたとき、長さ値を `start + 115` と `start + 117`、13個の seek 系値を `start + 119` から読み、ノート列を `start + 145` から読む。80-byte情報バッファの先頭が `start + 65` にあると置くと、上表の `0x32`、`0x34`、`0x36`、`0x50` と一致する。この対応は v1 の旧実装レイアウトを裏付けるが、seek 値個々の意味を特定するものではない。

### 読み込み後の Program Change 変換

SNG2S の `FUN_1000_314d` と MIMPIV427 の `FUN_1000_2d27` は、イベント列を4-byte単位で走査し、`0xFC` を終端として扱う。`(status & 0xF0) == 0xC0` の場合、イベントの3 byte目をルックアップ表の値で置換する。MIDI の `0xC0` は Program Change の status family なので、コード上の処理はチャンネル番号ではなく Program Change のデータ値の置換である。表の各値と楽器番号体系の対応は未確定。

### この追加調査の限界

- SNG2S と MIMPIV427 の読み込み順と版別分岐には強い対応があるが、これだけでソースコードの直接共有・複製までは証明できない。
- seek 系値の個別用途、全フィールドの意味、tempo/meta/SysEx の生成条件は未確定。
- この調査では実行時トレース、ビルド、テスト、変換再実行、MIDI再生は行っていない。

## 9. JMPlayer の解析メモを使った追加チェックリスト（2026-10-22）

参考資料: [JMPlayer: `.SNG` — Ballade / ミュージくん / ミュージ郎](https://github.com/jihodaya/jmplayer/blob/main/docs/formats/en/sng-ballade.md)。以下の「資料の主張」は JMPlayer 文書に記載された観測であり、Glosa 側で再現・検証済みという意味ではない。確認結果を記録するときは、資料の記述・静的解析・実測を区別する。

### Tempo 値は BPM そのものではなくスライダー位置

- [ ] SNG2S に tempo 値 1〜127 を与えたとき、資料記載の式 `BPM = 2v - ceil(v / 8)` が全値で再現するか確認する。特に `61 → 114`、`72 → 135`、`91 → 170` を境界チェックに使う。
- [ ] 8段階ごとに BPM の増分が一度止まるという曲線の特徴を、全127値の表で確認する。式の一致だけでなく、SNG2S が実際に出力する tempo meta event の値も照合する。
- [ ] 同じ tempo 値でも 3/4 拍子では資料記載上 BPM が1高くなる挙動を確認し、丸めがどの段階で発生するか追う。
- [ ] 拍子に依存しないタイマー設定の曲線と、SNG2S の 3/4 拍子向け丸めの差を区別する。SNG2S の結果をそのまま実機ドライバの一般仕様とみなさない。
- [ ] `.SNG` の設定領域にある既定 tempo と、conductor の tempo stream の各値が、変換時にどう優先・合成されるか確認する。
- [ ] Glosa の変換結果と参照 MIDI の tempo meta event を比較し、ノートの tick 一致とは別の項目として差分を記録する。

### 不一致ファイルはパーサー不具合と即断しない

JMPlayer 文書は、以下2ファイルを通常の形式解釈では説明しきれない例として挙げている。まず手元の fixture が同一ファイルか、サイズ・ハッシュ・各パート境界で確認する。

- [ ] `MAGICAL0.SNG`: 資料の主張は「126 KB で切れており、リズムパートの約半分が欠落」「最後のブロックだけは EOF を越えても許容され、それ以外の9パートは再生可能」。実 fixture の正確なサイズ、テーブル値、各パートの開始・終了位置を記録し、EOF 超過を許す条件がどのブロックに適用されるか検証する。
- [ ] `MAGICAL0.SNG`: 最後のブロック以外の境界チェックと、最終ブロックの EOF 超過許容を別々にテストする。根拠なく一般的な「全ブロックは EOF 内に収まる」規則へ変更しない。
- [ ] `3X3EYES2.SNG`: 資料の主張は、SNG2S と一致させるには途中に 10,029 ticks を挿入する必要がある一方、ファイル内にその挿入を示す根拠が見つからないというもの。conductor が示す97小節、各パートが示す96小節、SNG2S の出力148小節という3つの値を独立して再確認する。
- [ ] `3X3EYES2.SNG`: conductor と全パートの小節数・イベント step を集計し、10,029 ticks のギャップが実際のデータから導けるか調べる。導けなければ、推測で tick を挿入せず「未解決のファイル例」として残す。
- [ ] 2ファイルとも、資料に書かれた原因を Glosa の fixture について確認するまでは確定原因として扱わない。入力ファイルの欠損・特殊ケースと、Glosa のレイアウト判定の問題を切り分ける。

### 追加調査の記録ルール

- [ ] 各項目に「JMPlayer 文書の記載」「Glosa の静的解析」「fixture で確認」「SNG2S 実行結果」のどれに基づくかを明記する。
- [ ] SNG2S を実行していない項目は、実行済み・再現済みと書かない。既存 MIDI の比較結果も、生成元と生成時点が確認できる範囲に限って扱う。


## 10. 追加調査（2026-10-22）：v1 tempo/meta の生成経路

**状態:** SNG2S の静的逆解析と `runner.SNG` のバイト列を追加照合。tempo のソースフィールドと参照 MIDI の全イベントの対応は未確定。実行時トレース・SNG2S 再実行は未実施。

### Glosa 側で観測した不一致

`runner.SNG` の v1 レイアウト候補では conductor は file offset 116 から始まり、サイズ表の値は 1,394 bytes。heap marker `FF 7F FF FF FF FF` は file offset 761 にあり、後続版向けの `ReadTempi` がその12 bytes後から読むと、最初のレコードは `step=37, value=144`、次は `step=37, value=192` となる。後続版の slider 式を当てると 270 / 360 BPM になり、v1 の参照 MIDI の tempo meta と一致しない。したがって v1 に後続版の `ReadTempi` をそのまま適用しない判断は維持する。

### SNG2S の静的解析から分かった処理

- SNG2S の読み込み関数 `FUN_1000_272a` は、版別の初期化時に v1 と v2 以降で異なる内部 byte 位置を参照し、base tempo 候補を `0x2962` と `0x7BA` に設定する。v1 経路では `0x1AFE`、後続版では `0x1AE8` を読む。
- `FUN_1000_3406` は、内部 tempo buffer `0x21E0` の4-byte recordを処理する。record の value に base tempo 候補を掛けて `0x40` で割り、1〜480 に clamp する。変換後の値が現在値と異なると tempo event bytes を生成する。
- 出力処理は `60,000,000 / tempo` の計算結果を SMF tempo meta event `FF 51 03` として出力する。初期 tempo が 120 以外の場合にも、別の tempo meta event を出す経路がある。
- 参照 MIDI の tempo meta は、既存の F-053 の記録では tick 0 = 500,000 µs/quarter、tick 8 = 869,000、tick 192 = 869,565。これらの値と `0x1AFE`、tempo buffer `0x21E0` の元になった SNG byte 範囲の対応付けはまだできていない。

### 結論と次の手順

ここまでの静的根拠は、v1 の tempo が単純な「後続版と同じ slider 値の列」ではなく、SNG2S 内部の base tempo と別の tempo buffer を通って生成される可能性を示す。ただし、内部アドレスの意味だけから file offset を推測して実装してはならない。

- [ ] `0x1AFE` が読み込まれる元データの byte 範囲を、v1 の読み込み順とコピー先から特定する。
- [ ] v1 の入力 byte から内部 tempo buffer `0x21E0` が構築される経路を追う。
- [ ] 参照 MIDI の tick 8 / tick 192 の tempo event と入力データを対応付ける。
- [ ] それまでは v1 の tempo 変換を未解決として扱い、後続版の `ReadTempi` を v1 に流用しない。

参考: [JMPlayer の Ballade SNG format notes](https://github.com/jihodaya/jmplayer/blob/main/docs/formats/en/sng-ballade.md)（CC BY 4.0。v1 の設定領域・tempo stream の差異を考えるための参考資料。Glosa 側で独立に再検証した内容とは区別する）。


## 11. 追加照合（2026-10-22）：v1 tempo stream の入力位置と値

**状態:** `runner.SNG` / `TEST.SNG` のバイト列と、手元の SNG2S 参照 MIDI の tempo meta を照合。base tempo の入力位置と tempo stream の先頭は特定できた。可変 tempo の MIDI tick 変換は静的解析だけではまだ一般化できていない。

### 入力位置の対応

SNG2S `FUN_1000_272a` の v1 経路は、サイズ表を読み終えた後、file offset 114 の1 byteを読み、続いて file offset 115 から66 bytesを内部アドレス `0x1AE2` に読む。従って、v1 の base tempo byte `0x1AFE` は `0x1AE2 + 0x1C` であり、**file offset 143** に対応する。runner.SNG と TEST.SNG の値はいずれも 120。

同関数はさらに、v1 経路で590 bytesを file offset 181 から内部 `0x1F10` に読み、続いて352 bytesを file offset 771 から内部 `0x1F0C` に読む。内部 pointer `0x21E0` は後者を指す。runner.SNG の heap marker は offset 761 にあるため、SNG2S が tempo buffer として処理する領域は **marker + 10 bytes** から始まる。これは後続版の一般的な `marker + 12` 仮説を v1 にそのまま適用できないことを示す。

### 入力レコードと参照 MIDI

| ファイル | base tempo (offset 143) | SNG2S tempo buffer の先頭レコード |
|---|---:|---|
| `runner.SNG` | 120 | `step=48, value=37`; 次は `step=144, value=37` |
| `TEST.SNG` | 120 | `step=0, value=64`; 次は `step=192, value=64` |

`FUN_1000_3406` の計算は `floor(value × baseTempo / 64)` で、結果を 1〜480 に制限する。runner の `value=37` は `floor(37 × 120 / 64) = 69 BPM` になる。tempo meta writer には2つの丸め経路があることも逆アセンブルから確認できる。

- スケジューラ内の tempo 変更経路は `floor(60,000 / BPM) × 1,000` µs を出力する。69 BPM なら 869,000 µs で、参照 runner.MID の tick 8 イベントと一致。
- 初期化／曲開始側の経路は `floor(60,000,000 / BPM)` µs を出力する。69 BPM なら 869,565 µs で、参照 runner.MID の tick 192 イベントと一致。
- runner.MID の tempo meta は tick 0 = 500,000、tick 8 = 869,000、tick 192 = 869,565。TEST.MID は tick 0 = 500,000 のみ。参照 MIDI は公開リポジトリには含めず、期待値だけを記録する。

この照合により、base tempo の byte 位置・tempo buffer の入力開始位置・tempo 値のスケーリング・2種類の tempo meta 丸めは説明できる。ただし、tempo record の `step` から任意の MIDI tick への変換、特に複数の tempo 変更がある曲での時刻計算は、まだ完全に説明できていない。tick 8 と tick 192 の一致だけを根拠に一般式を仮定しない。

### 残タスク

- [ ] tempo stream の `step` が SNG2S の内部時間／MIDI delta time に変換される経路を追う。
- [ ] 2つ以上の異なる tempo 値を持つ独立 fixture でイベント時刻を照合する。
- [ ] tick 0 / tick 8 / tick 192 の値を regression test に追加し、tempo event timing の式が根拠を持つまで v1 tempo 変換は未解決のままにする。


## 12. v1 tempo parser implementation and validation status

The converter now has an isolated v1 tempo path rather than reusing the later-version slider parser.

- Reads the v1 bar/signature record at conductor offset +125.
- Reads base tempo from conductor offset +27 (file offset 143).
- Starts the v1 tempo buffer at the last heap marker +10 (file offset 771 for runner.SNG).
- Applies `floor(value × baseTempo / 64)`, clamped to 1–480 BPM.
- Starts the first v1 tempo-stream event at MIDI tick 8, then advances the next record's event time by the current record's `step`. This is the SNG2S scheduler behavior inferred from the static control flow and the runner reference output.
- Emits scheduler tempo changes using millisecond-rounded microseconds, and emits the effective non-120 tempo again at the song's first bar using integer `60,000,000 / BPM` microseconds.

Regression tests now check runner.SNG's reference tempo events at ticks 0, 8 and 192, plus a no-change tempo stream synthesized by replacing runner.SNG's tempo values with 64. They record reference MIDI facts without checking the reference binary into the public repository. The private-data TEST.SNG reference also contains only the default tempo, but it is not a public CI fixture.

**Status:** Public CI for the implementation has passed on the tested branch revision. A separate private-fixture comparison was added on 2026-10-22; its TEST.SNG tempo comparison passed, but its runner.SNG note-event comparison failed. See section 13 below. General v1 tempo compatibility is not yet proven: there is no independent v1 fixture with two or more different tempo values changing at later points in the song. Keep PR #9 open until the private comparison discrepancy is understood and the remaining limitations have been reviewed; do not claim complete event-for-event MIDI equivalence.


## 13. 非公開 fixture による追加検証（2026-10-22）

**目的:** 公開リポジトリに参照データを置かず、glosa-private-data に保存された SNG / MIDI を使って、PR #9 の変換結果を参照 MIDI と比較する。

### 実行構成

- 非公開リポジトリに専用ワークフロー .github/workflows/private-data-ci.yml を追加した。
- ワークフローは非公開リポジトリ内の fixture を取得し、対象ブランチ fix/ballade-v1-layout-2026-10-22 の Glosa ソースと一時的に組み合わせてビルド・テストする。
- private-only テストコードは .github/private-tests/PrivateBalladeFixtureTests.cs に置き、公開リポジトリには追加していない。
- 参照 fixture 自体は公開リポジトリへコピーせず、Actions artifact としても公開しない構成にしている。

ワークフロー実行: [Private Ballade fixture validation #3](https://github.com/pikachu364/glosa-private-data/actions/runs/39699313907)

### 結果

| 検証 | 結果 | 観測 |
|---|---|---|
| Build | 成功 | PR #9 のソースと private-only テストがコンパイルできた |
| TEST.SNG の tempo event 比較 | 成功 | 参照 TEST.MID と変換結果の tempo event 列が一致した |
| runner.SNG のノートイベント比較 | 失敗 | 全体のイベント配列比較で差が検出された |
| テスト全体 | 976 passed / 1 failed / 977 total | 失敗は private runner 比較テスト |

runner 比較の失敗ログでは、比較対象の最初の差として、期待側に Track=2, Tick=1728, Channel=9, Key=54 などが現れる一方、変換側は Track=2, Tick=2423, Channel=1, Key=73 から始まっている。これはテストが記録した不一致であり、ログだけでは原因を断定できない。

### 解釈と制約

- 現時点では「runner のノートイベントが参照 MIDI と一致した」とは言えない。
- 比較テストはトラック番号と tick を含むイベント列を直接比較している。参照 MIDI と変換 MIDI でトラック配置・イベント順序・チャンネルの対応が異なる場合も差分として検出されるため、まず両ファイルのトラック構成とイベントを対応付けて原因を切り分ける必要がある。
- 逆に、トラック構成の違いだけだと決めつけることもできない。tick mapping、ノートの再構成、チャンネル割当など変換側の差である可能性も未解決である。
- TEST.SNG の tempo 比較成功は、runner の可変 tempo 処理やノートイベントの完全一致を証明しない。
- 初期の private runner 比較は失敗したが、差分は tick 4012 / channel 1 の Note-off pitch 77 と 82 の配列順序だけだった。canonical sort を安定化した private workflow run #6 では 977 tests passed。これは Note-event signature 比較の成功を示すが、参照 MIDI との完全な互換性を意味しない。

### 次の作業

- [ ] runner.MID と変換 MIDI の各トラックについて、トラック名・チャンネル・ノート数・tick 範囲を集計し、トラック対応を確認する。
- [ ] トラック番号の違いを正規化した比較と、tick/音程/velocity を含むイベント内容の比較を分けて実施する。
- [ ] ノート数、ノートオン／オフの対応、tick 差分、channel 差分を個別に集計し、最初の不一致だけでなく差分の分布を確認する。
- [ ] 比較ロジックの前提が確認できるまではテストを緩めて成功扱いにせず、差分を保持する。
- [ ] 原因が Glosa の変換コードにあると確認された場合にのみ、修正と回帰テストを追加する。

**現在の判断（2026-10-23 更新）:** private workflow run #6 は Glosa commit `440000b971e72debcef72bb12407bcf4e944527a` で成功し、Note-event signature 比較の失敗原因は simultaneous Note-off の canonical order と判明した。現在の PR #9 head までの後続変更は docs のみ。PR #9 は未マージのまま。公開 CI と private event-level tests は成功しているが、SysEx / physical track-chunk order / 全メタイベント / runtime playback を含む完全互換性は未証明。

## 14. 追加調査結果（2026-10-22）：SNG2S の tempo / SysEx 経路と未確定点

この節は、版別レイアウト比較から派生した追加の静的調査をまとめる。以下は private fixture / 逆解析資料による確認であり、private データ自体はこの公開リポジトリへ追加しない。

### Tempo: 数値の生成式と出力 tick を分ける

- v1 `runner.SNG` の tempo-record value word は 37、base-tempo byte は 120。SNG2S の正規化計算では 69 BPM になる。ただし 37 がユーザー向け UI slider 値と同じ意味だとは証明されていない。
- 参照 `runner.MID` の tempo meta は tick 0 = 500,000、tick 8 = 869,000、tick 192 = 869,565 µs/quarter。869,000 は `floor(60,000 / 69) × 1,000`、869,565 は `floor(60,000,000 / 69)` に対応し、同じ 69 BPM の別々の整数丸め経路と整合する。
- `FUN_1000_3406` が coarse 869,000 event を queue すること、192-tick grid path が exact 869,565 event を生成することは静的解析で支持される。tick 192 への grid alignment は説明できるが、tick 8 の pending-delta / absolute-tick state は未確定。数値の一致だけで tick placement の因果関係を証明したことにはしない。
- `FUN_1000_33D4` 内の最初の `FUN_1000_3406` 呼び出しは、初期化フラグ `0xF96` により通常の scheduler `+1` 経路とは異なる。初回に通常ループの delta 規則を当てはめない。
- `0x6EE = 4` は setup SysEx の tick 0 / 4 と条件付き queue drain を結びつける仮説であり、確認済み事実ではない。

### Setup SysEx: producer は絞れたが payload mapping は未完了

- SNG2S の `FUN_1000_3BA7` と helper routines は template bytes を走査し、明示 byte・計算値・checksum・`F7` を含む SysEx-style output を組み立てる経路を持つ。
- 解析テキストに initialized data bytes at `0x3F82` / `0x3FA0` が含まれないため、template 全体の値と参照 MIDI の各 SysEx payload との byte-for-byte 対応はまだ取れない。
- SNG2S v3.3 manual は `/1` (Format 1) と `/X2` (conductor の直後の dedicated SysEx track) を既定値として記載し、`/X1` は conductor track、`/XD` は SysEx conversion off とする。これは文書化された defaults であって、`runner.MID` / `TEST.MID` を作った実際の引数が確認されたわけではない。manual は timebase 48 ticks/quarter も記載する。

### Option flag の修正

以前の仮説 `0x622 ↔ /F` は撤回する。静的解析上、`0x622` は SysEx payload 出力の抑制、`0x626` は track-order table 選択に使われる。manual の説明との対応から、現時点の最良仮説は `0x622 ↔ /XD`、`0x626 ↔ /F`。option token bytes と reference invocation は未確認なので、確定事実として扱わない。

### MIMPIV4 playback は別の未解決問題

MIMPIV4 の legacy loader が conductor/header 内の base-tempo byte を timer-period parameter にコピーし、timer driver がそれを使用するところまでは確認できた。一方、binary SNG tempo-record の第2 word を消費する playback routine はまだ特定できていない。SNG2S が MIDI に出力する式を MIMPIV4 playback の式と同一視しない。

### 検証境界

- 参照 v1 MIDI の Note-on 3,295 件・Note-off 3,295 件の一致は、該当作業ブランチでのオフライン比較結果であり、tempo/meta、全 SysEx、物理 track-chunk order を含む byte-for-byte SMF equivalence を意味しない。
- 現行 `main` での再変換や runtime trace はこの追加調査では実施していない。
- converter の tempo 規則・SysEx のハードコード・3/4 BPM 補正は、追加の根拠が得られるまで変更しない。

### 14.1 Additional static trace: SNG2S tempo-record decoder (2026-10-22)

`FUN_1000_3406` reads 4-byte tempo records from the table at `0x21E0`. It multiplies the record value by the base tempo at `0x2962`, divides by 64, clamps the result to 1–480, and updates `0x7BA` when the normalized tempo changes. At base tempo 120, record value 37 yields 69 BPM, explaining the numeric value behind the coarse 869000 µs/qn event.

`FUN_1000_3DE2` initializes `0xF96` to `0xFFFF`; `FUN_1000_33D4` calls `FUN_1000_3406` before resetting that flag. The first call therefore follows a different pending-delta path from later calls.

The arithmetic is now supported by static evidence. Why the coarse event is serialized at absolute tick 8 remains a separate question involving preceding setup-event drains and pending-delta/absolute-tick state. The initialized value of `0x6EE` is absent from the export, so four ticks remains unproven.

## 文書監査メモ（2026-10-22）

- 本文の v1 layout と tempo 調査は静的解析・保存済み fixture 比較の記録であり、SNG2S runtime trace や MIDI 実再生を意味しない。
- 直近の優先項目は [NEXT_STEPS.md](NEXT_STEPS.md) の T-017。既に説明できた tempo の整数計算や 192-tick grid を再調査せず、tick 8 の pending-delta / absolute-tick 状態、`0x6EE` の初期値、setup SysEx の payload 対応に絞る。
- 節番号の重複を解消した。既存の結果や未解決事項を削除・格上げしていない。

### 14.2 MIMPIV4 playback timer: evidence boundary (2026-10-22)

Static tracing identifies the driver dispatch and timer initialization chain: `FUN_1FF5_1344` selects a driver setup path, several selected branches copy `0x0438` into `0x0440`, and `FUN_1FF5_0D61` installs the INT 8 handler before passing `0x0440` to `FUN_1FF5_0DFA`. The latter calculates a timer divisor and writes to I/O ports `0x71` and `0x5F`.

The follow-up candidate review checked `FUN_1000_341E` (special-record payload scan), `FUN_1000_3543` (event/duration pre-scan), and `FUN_1000_2D27` (Program Change rewrite); none shows the tempo-record value being passed to the timer globals. `FUN_1000_8888` schedules a parsed text-command delay plus `0x0440`, but no connection to the binary SNG tempo table is established. The actual consumer of the binary tempo-record second word remains unidentified. This narrows the static search but does not prove that tempo records are ignored. Keep this question separate from SNG2S's MIDI tempo-meta conversion. No runtime trace or playback test was performed; next evidence is the actual playback event-dispatch path or a runtime trace around a known tempo record.


## Private runner comparison follow-up (2026-10-23)

The private fixture workflow's latest successful run is [run #6](https://github.com/pikachu364/glosa-private-data/actions/runs/39699711256). It checked out Glosa `440000b971e72debcef72bb12407bcf4e944527a`, built successfully, and passed all 977 tests. The previous failure's first differing signature was caused by two same-tick Note-off events (channel 1, pitches 77 and 82) appearing in opposite array order. Canonical ordering by pitch removed the test-only ordering discrepancy; no converter behavior change was needed. Current PR #9 adds only documentation changes after the tested checkout. This validation is limited to the specific tempo and Note-event signatures covered by private tests.

## 15. 追加調査（2026-10-24）：v2 setup SysEx producer mapping の静的照合

**状態:** private SNG2S 逆解析 export、original EXE initialized data、v2 fixture/reference MIDI を静的に照合。runtime trace・実再生は未実施。

`FUN_1000_272A` は v2 fixture で file offset `0x73` から `0x4E` bytes を `0x1AE2` に読み込む。`SHIZUME0.SNG` の class-1 分岐では `0x1B04=0` のため fallback pair `0x1AF4` / `0x1AF6`（値 6, 6）を選び、`0x7BE=5` に設定する。

`0x7BC` の bit 0–2 は、それぞれ `0x7C0`、`0x7C2`、`0x7C4` への代入を許可する。original `SNG2S.EXE` initialized image では `DS:0x07BC` は file offset `0x6C2C` に対応し、初期 byte は `0x00`。隣接する初期 word は `0x7BE=5`、`0x7C0=0`、`0x7C2=5`、`0x7C4=3`。解析 export には `0x7BC` への直接 store がないが、これは実行時 trace ではなく、間接/別名アドレスによる書き込みも完全には排除しない。

参照 MIDI との比較では、次の2組は別々の producer path に一致する。

- tick 4 / 8: `FUN_1000_23C0(4)` の計算値がデータ値 `10 00 0F 03` と `52 00 0F 0F` に一致し、checksum と terminator も一致。
- tick 100 / 104: `FUN_1000_1CE4` の `0xF34` / `0xF3F` template が引数 `0,6,6`、checksum、terminator を含めて一致。`%c` は1 byteを消費し、template marker `0xFE` は checksum 生成前に `0x00` へ正規化される。

従って、以前の「payload 不一致」は異なる2つの producer path を競合候補として比較したことによるものだった。ここで照合した4イベントの静的 producer mapping は説明できる。一方、実行時の `0x7BC` 状態や別経路での global 変更、全イベントへの一般化は未証明であり、converter の変更根拠にはしない。

**残る確認:** 可能なら runtime trace で `0x7BC` と関連 global の実行時値・書き込み経路を確認する。private fixture / reverse export の内容自体は public repository に追加しない。
