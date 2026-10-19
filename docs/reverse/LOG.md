# 調査ログ

日付順の短い記録です。詳細な経緯・長い引用・古い仮説は [DEVELOPMENT_HANDOFF.md](../DEVELOPMENT_HANDOFF.md) に残します。

## 2026-10-19 — 知識ベースの初期整理

- 旧引き継ぎ文書から、マニュアル由来の事実、静的解析結果、データ比較結果、未確定仮説、次のタスクを分類した。
- SNG2S Ver. 3.3 の手動資料と逆解析の観測を区別した。
- SHIZUME0 の Note-on 2,713件の一致と tick offset +128 は静的比較結果として記録。Tie Note-off の対応規則は未確定のまま保持した。
- v1 の +80 レイアウトは有力候補として記録したが、確定仕様にはしていない。
- SngConverter.cs は変更していない。今回の整理は既存記録の再分類であり、新たな実行テストや MIDI 再生は行っていない。

## 2026-10-19 — JMPlayer の実装と v1/Tie 仮説の照合

- JMPlayer の `sngmidi.cpp` 96–147行を再確認。レイアウト判定は v1〜v4 共通で、header候補 `tableEnd+94`, `tableEnd+2`, `tableEnd`、パートサイズ `entry+81)、長さフィールド `+112/+114/+116) を使う。v1専用の +80 レイアウト分岐は実装に存在しない。
- したがって v1 の +80 候補は、現時点ではデータ上の整合性がある一方、JMPlayer の実装根拠とは一致しない。v1 のファイル構造差、fixture と想定の差、または候補計算の前提をさらに調べる必要がある。
- 同ファイル 267–292行では Tie bit (flags bit 7) を、直前に記録したノートの音高が同じで、前ノートの Note-off tick と今回の開始 tick が一致する場合に限って延長する。それ以外は新しい Note-on/Note-off を出す。Glosa の現行コードも同じ条件を実装している。
- これにより、SHIZUME0 参照 MIDI の Note-on が Tie bit なしレコードだけと一致する観測は、JMPlayer の tie 条件だけでは説明できない。参照 MIDI の生成元・オプションを同定するか、SNG2S/SNG2GS の実際の Tie 処理を別途解析する必要がある。
- 同ファイル 198–208行では小節長の既定値は192 ticksで、コンダクタ部の有効な値があれば上書きされる。314–316行では lead-in にその barLen を使う。従って参照 MIDI の +128 tick が JMPlayer の既定 lead-in から生じたとは言えない。
- 静的ソース確認のみ。変換実行、再生、SngConverter.cs の変更は行っていない。


## 2026-10-19 — MIDI トラック順の互換性差を発見

- SNG2S Ver. 3.3 マニュアルの既定出力順と現行 `SngConverter.ToSmf` のトラック追加順を比較した。
- マニュアルの既定順は v1〜v3 がパート 10→1、v4 が 10,16→11,9→1。一方、現行実装はパート 1→N の昇順でトラックを追加しているため、トラックチャンク順に差がある。
- これを F-009 と T-006 に記録した。チャンネル割当・トラック名・参照 MIDI の実チャンク順を確認するまでは、既定順の互換性が欠けている可能性として扱い、コードは変更しない。
- ソースとマニュアルの静的比較のみ。テスト実行や MIDI 再生は行っていない。


## 2026-10-19 — 0xFA0 登録の呼び出し条件を追加確認

- GitHub 上の `testdata/ballade/sng2gs/sng2gs解析データ.txt` を直接読み、`FUN_1000_3406` の `1000:37F3–381F` と `FUN_1000_3A10` の逆アセンブルを照合した。
- `FUN_1000_3A10` は、直前の `FUN_1000_3A3F` がゼロを返し、かつ `DH != 0x40` の場合に呼ばれる。`DH == 0x40` では別の条件分岐に進み、登録関数は呼ばれない。
- これは 0xFA0 タイマー表の呼び出し条件を狭める観測だが、DH の意味や登録項目と MIDI イベントの意味対応は未確定。FACTS F-010 に記録し、T-005 を部分更新した。
- 静的解析のみ。変換テスト・再生・変換ソースの変更は行っていない。


## 2026-10-19 — 0xFA0 検索と出力バッファ経路を追加確認

- `FUN_1000_3A3F` は最大64項目を4-byte刻みで検索し、項目先頭 word と項目 `+2` word、入力 AX の一致を条件とする。該当時は項目先頭 byte と DL の大小比較を行い、条件によって項目を空き状態に戻す。
- 入力 DH が `0xC0` でない場合、AL・AH・ゼロの3 byte を `FUN_1000_39C9` 経由で出力する。`FUN_1000_39C9` は AL を出力位置 `0x1152` に書き、`0x1556` で `0x1156` に折り返す単純な byte writer。
- ここまででタイマー表の照合条件と出力先は具体化したが、AX/DX の意味および3 byte のイベント上の意味は未確定。FACTS F-011 を追加し、T-005 を更新。ソース改変や変換テストは実施していない。


## 2026-10-19 — writer の参照範囲を拡張確認

- `FUN_1000_39C9` の逆解析資料にある参照一覧を確認。0xFA0 経路のほか、期限切れ処理や `FUN_1000_3BA7` 系を含む多数の呼び出し元から使われている。
- したがって `FUN_1000_39C9` は汎用的な1-byte出力処理として扱い、Tie 専用の処理とは分類しない。`FUN_1000_3A3F` が DH=`0xC0` のとき3-byte出力をスキップする事実も記録したが、レジスタ値の意味は未確定。
- FACTS F-012 を追加し、T-005 の次の確認項目を呼び出し側の値設定・出力先の追跡へ更新した。変換コードの変更やテスト実行は行っていない。

## 2026-10-19 — byte queue の読み取り側を確認

- `FUN_1000_39E9` の命令列を確認。読み取りポインタ `0x1154` から1 byte を取り出し、共有カウンタ `0x1150` を減らす。空の場合は `0xFFFF` を返し、ポインタは writer と同じ `0x1556 → 0x1156` の折り返しを使う。
- `FUN_1000_3BA7` は `F0`/`F7` を含む byte 列を writer 経由で積んでおり、writer を汎用 byte queue producer とする見方を補強する。
- FACTS F-013 を追加。次は `FUN_1000_39E9` の複数呼び出し元と `FUN_1000_3BA7` の consumer 側の扱いを追う。変換コードは変更していない。

## 2026-10-19 — キュー consumer の MIDI ステータス分岐を確認

- `FUN_1000_183A` は queue reader のラッパー。呼び出し元 `FUN_1000_0Bxx` 付近で Note-off、Note-on、poly pressure、control change、program change、channel pressure、pitch bend、SysEx に対応するステータス範囲へ分岐する命令列を確認した。
- SysEx 経路では `F0` から `F7` まで byte を収集し、条件によって `FUN_1000_16E6` へ渡す。これにより queue が MIDI 解析器への byte-stream 中継点であることが強く裏付けられた。
- FACTS F-014 を追加。次は各ステータス経路と最終 SMF 出力先の対応を追う。変換ソースは未変更。

## 2026-10-19 — consumer から出力バッファへの経路を確認

- `FUN_1000_17C2` は `FUN_1000_5648` に byte を渡し、出力位置 `0x1C2A:0x1C2C` を加算する。`FUN_1000_5648` は `0x1AE0` の出力バッファ管理構造体に byte を書き、残量を減らす。残量不足時は `FUN_1000_48A2` を呼ぶ。
- `FUN_1000_16E6` と `FUN_1000_1754` は 7-bit 継続形式の値を組み立て直し、それぞれ `FUN_1000_17C2` / `FUN_1000_17FE` を通して出力する。VLQ と整合するが、各 call-site の意味は未確定。
- FACTS F-015 を追加。次は call-site から delta-time 等の役割を確定し、バッファ flush 経路を追う。変換コードは未変更。


## 2026-10-19 — VLQ 呼び出し箇所と出力ブロック境界を確認

- FUN_1000_16E6 の呼び出し箇所を調査。1000:0937 / 1000:096D は蓄積時間値を出力後に内部時刻へ加算するため delta-time 経路と整合する。1000:0AD6 は SysEx を F7 まで収集した byte 数を符号化する経路。1000:04C4 はテンポメタイベント FF 51 03 の前にあり、時間値出力の可能性が高いが、値の由来は未確定。
- FUN_1000:5648 は残量が尽きると FUN_1000:48A2 を呼ぶ。FUN_1000:48A2 は管理フラグ、ストリーム別状態 (0x186A/0x186C)、ブロック長 0x200、FUN_1000:59AC による移送を扱う。単純な flush と断定せず、状態遷移の調査を次タスクに残した。
- FACTS F-016 / F-017 を追加し、NEXT_STEPS を更新。SngConverter.cs は未変更。


## 2026-10-19 — ストリーム別バッファ状態表を追跡

- `FUN_1000_4A26` から、管理構造体 `0x17CA+n*8` と状態表 `0x186A+n*6` の対応を確認。通常は `FUN_1000_5AF8(0x200)` で 512-byte バッファを取得し、状態表のブロック長を `0x200` に設定する。取得失敗時は内部の 1-byte 代替領域を使う。
- `FUN_1000_48A2` がバッファ境界で `FUN_1000_59AC(handle, buffer, count)` を呼び、下位出力処理へ蓄積データを渡すことを確認。`FUN_1000_59AC` は handle ごとのフラグを参照し、特定モードで改行を扱うため、単純なメモリ flush と同一視できない。
- FACTS F-018 を追加。次は管理構造体と状態表の各フラグを分岐別に整理する。変換コードは未変更。


## 2026-10-19 — ストリーム管理フラグの確認済み経路を整理

- `FUN_1000_4A92` の open mode 解析で、`r` / `w` / `a` の初期フラグと `+`、`b`、`t` の分岐を確認した。
- `FUN_1000_4A26` のバッファ確保成功時 `0x08`、失敗時 `0x04` のフラグ更新を確認。`FUN_1000_48A2` では `0x01` / `0x40` の条件で通常経路を外れ、失敗時に `0x20` を設定するが、未解釈ビットは断定せず残した。
- `FUN_1000_59AC` の `0x80` 経路は LF→CR/LF のテキスト変換、`0x20` 経路は DOS `AH=42h` seek と確認。FACTS F-019 を追加し、NEXT_STEPS を未解釈ビットと seek 呼び出し元の調査へ絞った。
- `src/Glosa.Core/Smf/SngConverter.cs` は変更していない。


## 2026-10-19 — ストリームの read/write/cleanup ライフサイクルを追跡

- `FUN_1000_47E2` の read 経路で `0x01`、終端／失敗側で `0x10` または `0x20` が設定されることを確認。
- `FUN_1000_48A2` の write 経路は `0x02` を設定し `0x10` をクリア、失敗側では `0x20` を設定する。各ビットの正式な名称はまだ付けず、実際の遷移として記録。
- `FUN_1000_49F8` は動的バッファを解放し `0x08` とポインタ類をクリア。 `FUN_1000_4B8A` は特別な 2 管理構造体に固定領域と 0x200 byte 状態を設定する。
- FACTS F-020 を追加。次は open/read/write/close の呼び出し元を横断して状態表を完成させる。変換コードは未変更。


## 2026-10-19 — ストリーム API のラッパーと参照元を照合

- FUN_1000_4740 は FUN_1000_560E で対象を得てから FUN_1000_4A92 に渡す open 系ラッパーで、参照元 9 箇所を列挙。
- FUN_1000_4768 は特殊管理領域の初期化 FUN_1000_4B8A、処理 FUN_1000_4D12、後始末 FUN_1000_4C0E を順に呼ぶ。
- FUN_1000_49F8 の参照元 1000:4697 では cleanup 後に FUN_1000_5690 が続くため、close と即断せず、ハンドル後処理を継続調査する。
- FACTS F-021 を追加。converter は未変更。次は FUN_1000_5687 の関数境界と、管理構造体の実インデックスを caller から追う。


## 2026-10-19 — flush / buffer cleanup / OS close を分離

- `FUN_1000_5670` は buffered reader で、残量が尽きた場合のみ `FUN_1000_47E2` に読み取りを委譲することを確認。
- `FUN_1000_4CA4` は write-mode 管理構造体の未出力 byte を `FUN_1000_59AC` に渡す flush 経路。`FUN_1000_470E` は有効な管理構造体を走査して flush を試みる。
- `FUN_1000_5690` は DOS `AH=3Eh` による OS handle close。`FUN_1000_49F8` の動的バッファ cleanup とは別で、`FUN_1000_4654` では cleanup の後に close が続く。
- FACTS F-022 を追加。converter は未変更。次は close 前 flush の caller 条件と handle 登録フラグの設定源を確認する。


## 2026-10-19 — close 経路と OS handle flag table を接続

- `FUN_1000_4654` 内部の命令順を追い、`FUN_1000_4CA4` の flush 試行が `FUN_1000_49F8` cleanup と `FUN_1000_5690` DOS close より前に実行されることを確認。
- `FUN_1000_572A` が成功した OS handle ごとに `0x1782+h` flag byte を初期化し、`FUN_1000_4CA4` が管理構造体 `+7` の handle を `FUN_1000_59AC` に渡すことから、両 table の接続は管理構造体 index ではなく handle 値経由と確定。
- `FUN_1000_59AC` の `0x20` (seek-to-end) と `0x80` (LF→CR/LF) の用途を命令列で再確認。FACTS F-023 を追加。converter は未変更。


## 2026-10-19 — close 関数名の訂正と戻り値 caller 調査

- 直前の記録で `FUN_1000_4697` としたのは誤記。`1000:4697` は `FUN_1000_4654` 内の命令アドレスで、関数名は `FUN_1000_4654`。
- close routine の参照元 10 箇所を確認。列挙された呼び出し位置では close の戻り値直後に条件分岐せず、次の処理へ進む。全 caller のエラー無視と一般化はせず、観測範囲に限定して記録。
- FACTS F-024 を追加。converter は未変更。


## 2026-10-19 — open mode と handle flag byte の対応表

- `FUN_1000_4A92` の `r/w/a`、`+`、`b/t` parser と `FUN_1000_572A` の flag byte 組み立てを突合。DOS 属性 bit 0 由来の `0x10`、open mode bit `0x08` 由来の `0x20`、text/device 判定由来の `0x80`/`0x40` を記録。
- `FUN_1000_58CE` の read-side も確認し、`0x80` が read/write 双方のテキスト処理、`0x02` が read-side EOF 状態に関係することを追記。すべての bit の完全な意味は未確定。
- FACTS F-025 を追加。converter は未変更。


## 2026-10-19 — read-side text flag と EOF-like state

- `FUN_1000_58CE` の命令列を再確認。text flag `0x80` が立つ場合にのみ CR/LF processing loop に入り、loop 冒頭で `0x04` をクリア、読み込みバッファ先頭が LF の場合に `0x04` をセットする。
- text loop 内で SUB (`0x1A`) を検出すると `0x02` をセットして終了し、次の read 呼び出しは `0x02` を見て DOS read をスキップする。`FUN_1000_47E2` の zero/error-like result に対応する `0x10/0x20` とは別経路として記録。
- FACTS F-026 と NEXT_STEPS を更新。`0x02` の reset/clear caller と `0x04` の reader/caller を全検索するのが次の作業。変換コードは未変更。


## 2026-10-19 — EOF-like flag clear path と flag byte 全体検索

- `FUN_1000_56B0` (`1000:5722`) で DOS seek (`AH=42h`) 成功後に handle flag `0x02` を clear する命令を確認。`0x02` は text SUB (`0x1A`) 後の read 停止状態だが、成功した seek で解除可能。
- `0x04` は `FUN_1000_58CE` の text loop でセットされ、同関数の次の text loop 開始時に clear。checked-in disassembly 全文の直接検索では外部参照を確認できず、これ以上の意味付けは保留。
- close (`FUN_1000_5690`) 成功時は `0x1782+h` flag byte 全体をゼロクリア。FACTS F-026 / NEXT_STEPS を更新。converter 未変更。


## 2026-10-19 — Tie 検証の fixture 状況を確認

- repository の recursive tree と `testdata/ballade/README.md` / `manifest.json` を確認。SNG fixture は v1 `runner.SNG`、v2 `SHIZUME0.SNG`、v3 `START__0.SNG`、v4 `BOURREE0.SNG` の4件。参照 `.mid` は現行 tree に存在しない。
- `tests/Glosa.Tests/SngConverterTests.cs` は最小 SNG、tempo、壊れた layout を検証するが、Tie bit による Note-off 延長や同音重複の比較テストはない。したがって現状の repo 内容だけでは reference MIDI への event-by-event 検証を再現できない。
- NEXT_STEPS に参照 MIDI の出典/版/オプションを伴う test fixture の準備と、fixture 入手後の SNG raw record ↔ MIDI event 突合を追記。変換コードは変更していない。


## 2026-10-19 — 参照 MIDI 比較と Tie 修正
- ユーザー提示の `docs/ballade-version-testdata` ブランチに、v1 SNG2GS MIDI、v2〜v4 の SNG2GS MIDI と Glosa MIDI が実在することを確認。各ディレクトリの README は MIDI が未追加と書かれており、実ファイルと説明が不整合。
- MIDI の Note-on/off を tick/key/velocity/type で比較。v2 は Glosa の tick を160 ticks補正し、v3/v4 は補正なしで比較。v2 は参照2,713件に対し Glosa 3,657件、v3 は参照5,362件に対し Glosa 5,489件、v4 は両方1,721件。v2/v3 では参照イベントがすべて Glosa 側にも存在し、Glosa 側にそれぞれ追加ノート944件/127件と対応する Note-off が存在した（トラック/チャンネルを無視した比較）。
- SNG 生レコード集計で v2 は 3,783 ノートレコード中 Tie bit 1,070件、非 Tie 2,713件。v3 は 5,719件中 Tie bit 357件、非 Tie 5,362件。v4 は Tie bit 付きノートレコードなし。非 Tie 件数が参照 Note-on 数と一致した。
- この根拠に基づき、Tie レコードが延長条件を満たさない場合に通常ノートとして出力される現行挙動を修正。Tie bit 付きレコードは新規 Note-on/off を出さず、同じ音高かつ前 Note-off と開始 tick が連続する場合だけ前 Note-off を延長する。合成回帰テスト2件を追加。CI 実行と修正後の実 MIDI 再比較は未完了。
- 注意: イベント比較はトラック/チャンネルを無視した集計で、同音高重複の Note-off 所有関係は未解決。v2 の160 ticks差と SNG2GS の版/オプションも未解決。


## 2026-10-19 — Tie Note-off 対応規則の絞り込み
- SNG2GS MIDI の各ノートトラックを、SNG パートの非 Tie レコードの `(tick,key,velocity)` で対応付けた。v2 の参照イベントは source tick +128、v3 は source tick +192 で整合する。Glosa 出力との比較では v2 に160 ticks差がある。
- Tie レコードは Note-on を出さないだけでなく、直前のレコードだけを見ていると chord/interleaved note を取りこぼす。さらに、Tie start が元 Note-off より少し後に来るケースがある。
- オフライン再生成で、同じ音高の pending Note-off から予定 tick が Tie start 以下で最も後のものを選び、Tie duration を加えて延長する規則を適用すると、v2/v3 の全 Note-on/off が各パート単位で参照 MIDI と一致した。v4 は Tie なしで既に一致。
- これを C# 実装に反映し、合成テストを4件（連続 Tie、gate gap、他音高の interleave、同音高の候補なし）に拡張。実際の .NET 出力と CI はまだ確認待ち。

## 2026-10-19 — 実 C# 変換出力の比較 CI と v1 layout failure

- fixture branch の CI run [39216319421](https://github.com/pikachu364/glosa/actions/runs/39216319421) で、v2〜v4 の SNG2GS 参照 MIDI と実際の C# 変換出力を比較する統合テストが成功した。比較は全トラックを集約した Note-on/off の tick・音高・velocity・種別であり、トラック／チャンネル割当の一致は未検証。
- v1 `runner.SNG` を同様に検証するテストは [CI run 39218961795](https://github.com/pikachu364/glosa/actions/runs/39218961795) で `Invalid or unsupported Ballade SNG layout.` により失敗。レイアウト判定で止まり、MIDI イベント比較は実行されなかった。
- v1 の失敗テストは [commit b75612a](https://github.com/pikachu364/glosa/commit/b75612ae59fd1d71aeb299834d427dbaf98c1c54) で一時的に削除し、fixture branch の README と manifest に未対応状態を記録した。v1 のレイアウト候補を確定したわけではない。
- `FACTS.md` に F-028、`HYPOTHESES.md` に H-009 を追加し、Tie の CI 検証状況と v1 の制約を明記した。`NEXT_STEPS.md` は v2〜v4 の集計比較を完了、トラック単位比較と v1 レイアウト解析を未完了として更新。
- converter のレイアウト処理は変更していない。次は参照 MIDI のトラック／チャンネルごとの比較を追加し、v1 はブロック境界とサイズ表を独立に検証してからテストを戻す。

## 2026-10-19 — トラック単位の MIDI 比較テスト追加

- 全トラックを集約した Note-on/off 比較ではトラック間のイベント移動を見逃すため、fixture branch のテストを拡張。各トラックのイベント構成を tick・on/off・pitch・velocity で比較し、トラック番号／チャンネル番号を正規化してトラックごとのイベント集合を照合する。
- テスト変更 commit [`65ace59`](https://github.com/pikachu364/glosa/commit/65ace594fc829e81e6824c871915448074f7f424)。CI run [39220897447](https://github.com/pikachu364/glosa/actions/runs/39220897447) と [39220904556](https://github.com/pikachu364/glosa/actions/runs/39220904556) は更新時点で実行中。
- `FACTS.md` に F-029、`NEXT_STEPS.md` に CI結果確認とチャンネル割当／同音高重複の別検証を追記。現時点ではテスト結果未確定であり、converter の実装変更は行っていない。


## 2026-10-19 — トラック単位比較 CI 成功

- fixture branch のトラック単位 Note-on/off 比較テストを実行した CI run [39220897447](https://github.com/pikachu364/glosa/actions/runs/39220897447) と [39220904556](https://github.com/pikachu364/glosa/actions/runs/39220904556) が、Linux・macOS・Windows の全 job で成功した。
- `FACTS.md` の F-029 と `NEXT_STEPS.md` を更新し、トラック単位イベント構成の比較を完了扱いにした。
- テストは各トラックの tick・on/off・pitch・velocity 構成を比較するが、トラック番号とチャンネル番号は正規化する。従って次の課題は、SNG2S マニュアル既定のトラック順と実参照 MIDI のトラック／チャンネル／SNG パート対応を個別に検証すること。同音高重複時の Note-off 所有関係も別途残る。
- 変換器のソースコードは変更していない。


## 2026-10-19 — SNG2GS の track/channel と SNG part の対応

- fixture branch の SNG と参照 MIDI を Base64 経由で解析し、MIDI track chunk 順、トラック名、channel、Note-on/off を抽出。v2/v3 のパート順は 10→1、v4 は 10,16→11,9→1。ノートイベントを持つパートの channel は part-1 と一致した。
- 参照 MIDI の channel 対応は現行 Glosa の channel 割当と一致する一方、Glosa は track を part 昇順に書くため、参照 MIDI の既定 track chunk 順とは異なる。/F オプション時の順序は未検証。
- fixture branch commit `2ba756b` でイベント比較テストに channel 番号を含める変更を追加。CI run `39223684171` は更新時点で実行中。FACTS F-030 と NEXT_STEPS T-006 を更新。

## 2026-10-19 — チャンネル比較 CI 成功

- fixture branch commit [2ba756b](https://github.com/pikachu364/glosa/commit/2ba756b10e138e1093b13507605205de66c899b0) で、トラックごとのノートイベント署名に MIDI channel を含めた。
- GitHub Actions run [39223684171](https://github.com/pikachu364/glosa/actions/runs/39223684171) は Windows・Ubuntu・macOS の全 job で成功。参照 MIDI と変換出力のノートイベント構成およびチャンネル割当を、トラック番号を正規化したうえで比較できる状態になった。
- FACTS.md F-030 と NEXT_STEPS.md T-006/T-007 を更新。チャンネル割当の照合は完了扱いにし、`/F` の実行比較と同音高重複時の Note-off 所有関係は未完了として残す。
- リポジトリの main tree には SNG2S/SNG2GS の実行ファイルが見当たらないため、既存参照 MIDI だけでは `/F` オプション出力を直接生成して比較できない。参照実行環境なしにオプション動作を確定したとは扱わない。


## 2026-10-19 — 同音高重複 Tie テストの CI 失敗を再確認

- run [39224478408](https://github.com/pikachu364/glosa/actions/runs/39224478408) の Ubuntu job log と run jobs を GitHub から取得。Windows・Ubuntu・macOS の3 job が、ビルド成功後のテスト工程で失敗。972 件中 971 件成功。
- `tests/Glosa.Tests/SngConverterTests.cs` の `TieRecordExtendsLatestEligibleNoteOffWhenSamePitchNotesOverlap` は `[240, 264]` を期待するが、実際は `[216, 288]`。テストと `SngConverter.cs` の候補選択・延長処理を再確認した。
- Tie は開始 tick 以下の同音高 pending Note-off のうち最も遅いものを選び、同 tick なら後から追加されたものを選択する。選択後は Note-off を `Tie start + duration` に移す。
- 期待値だけを変更することは保留。実際に読み取られたレコード位置・各 tick・選択された event index を追跡して不一致の根因を確定する必要がある。
- この時点でコード修正も新しい CI run も行っておらず、修正完了とは扱わない。


## 2026-10-19 — Tie 重複テスト fixture 修正と CI 成功

- fixture branch commit [07e8b57](https://github.com/pikachu364/glosa/commit/07e8b57c73628b7e263781fdff84b20748172b36) の差分を確認。TieRecordExtendsLatestEligibleNoteOffWhenSamePitchNotesOverlap の最初のレコードに渡す duration/step 値を入れ替え、同音高ノートの予定 Note-off tick が一致するように fixture を修正した。
- commit に紐づく Actions run [39224987508](https://github.com/pikachu364/glosa/actions/runs/39224987508) の job 結果を GitHub から取得し、Windows (117968976787)、Ubuntu (117968976840)、macOS (117968976870) の全 job が completed/success であることを確認。
- 修正はテスト fixture のみで、SngConverter.cs の Tie アルゴリズムは変更していない。したがって fixture/CI の失敗は解消したが、実 MIDI における Note-off 所有関係の検証は引き続き必要。
- FACTS.md F-031 と NEXT_STEPS.md T-008 を更新。


## 2026-10-19 — Tie fixture の時刻計算を明示

- WriteNoteRecord helper の引数順が duration, step であることをソースで確認。
- 修正後 fixture は、1件目 duration=48/step=24 → Note-off tick 48、2件目は開始 tick 24 + duration 24 → Note-off tick 48。Tie は累積 step により tick 48 で開始し、同 tick の候補のうち後から追加された2件目を tick 72 に延長する。
- SMF 出力の基準 tick 192 を含めた Note-off は 240 と 264。これで修正後の期待値が fixture の算術と一致することを説明できる。
- この確認は合成 fixture の期待値を裏付けるもの。実 MIDI の Note-off 所有関係は T-001 / T-007 で継続する。




## 2026-10-19 — Windows CI 再実行成功と v2/v3 Tie 候補走査

- run [39228366606](https://github.com/pikachu364/glosa/actions/runs/39228366606) の Windows job は初回に `SequencerTests.KeepsEventsRoughlyOnSchedule` だけ失敗したが、失敗 job の再実行で Windows・Ubuntu・macOS の全 job が成功。失敗したタイミングテストのコードや許容範囲は変更していない。
- T-001 を進め、v2 `SHIZUME0.SNG` と v3 `START__0.SNG`、各 SNG2GS 参照 MIDI のバイナリを直接走査。v2 は通常ノート 2,713 / Tie 1,070、v3 は通常ノート 5,362 / Tie 357。参照 MIDI の Note-on/off 件数は通常ノート数と一致した。
- Tie の同音高候補は、v2 が候補なし 0 / 候補1件 153 / 候補複数 917、v3 が候補なし 0 / 候補1件 40 / 候補複数 317。複数候補があっても、最新 tick が同値になる競合候補は両バージョンで 0 件。
- Tie 開始 tick と選択された Note-off tick は、v2 で 1,021/1,070 件が一致し、49 件に 4〜33 ticks の gap。v3 では 353/357 件が一致し、4 件に 1〜3 ticks の gap。候補なしは両方 0 件。
- Tie 延長を含むオフライン再構成から作った Note-on/off 署名を参照 MIDI と比較し、v2 5,426 件・v3 10,724 件で差分 0 件。参照 MIDI のトラック内で同音高 Note-on が前の Note-off より先に重なる例も両バージョンで 0 件。
- 実データでは最新候補の同 tick 競合がなく、合成 fixture の「同 tick に同音高 Note-off 候補が複数」の tie-break 規則は未確定のまま。FACTS F-032 と NEXT_STEPS T-001 を更新。次は v4 に同じ検査を広げるか、SNG2GS の生成条件を特定して意味論の根拠を増やす。


## 2026-10-19 — v1 レイアウト候補の実データ整合性を確認

- v1 `runner.SNG` のサイズ表と各パートを直接走査し、header end = tableEnd + 2、part size = entries[p] + 80、名前 +80、長さフィールド +115/+117/+119、レコード列 +161 の候補が全10パートで容量条件を満たすことを確認。
- この候補は現行 converter の v2〜v4 向けフィールド位置とは異なり、HYPOTHESES H-001 と NEXT_STEPS T-002 に記録。容量整合性だけでは正式レイアウトと確定しないため、変換コードは変更していない。
- v1 SNG2GS MIDI の Note-on 数を part-1 の MIDI channel と照合したところ、9つの発音パート中7つは非 Tie レコード数と一致。channel 5 と channel 6 は参照 MIDI 側が各2件多く、全 Note-on の tick/pitch/velocity 対応もまだ一致していない。
- 次は v1 の特殊フラグ・終端 marker と4件の差を調べ、レイアウト候補とイベント解釈を分けて検証する。現時点では v1 対応実装を追加しない。


## 2026-10-19 — SNG2S のレコード終端判定を逆アセンブルで確認

- `testdata/ballade/sng2gs/sng2gs解析データ.txt` の `FUN_1000_1668` を確認。各パートのレコード列は 8 byte ごとに走査され、先頭 byte が `0xFC` なら終了。byte +4 が非ゼロならそのパートを有効として扱う。
- 現行 converter の record stride / 終端 marker はこの処理と一致。byte +4 は有効パート判定にも使われるため、v1 の byte +4 を一律に velocity と解釈する根拠にはならない。
- `sng2s33.lzh` が対象ブランチに存在することを確認。今回の解析は既存の逆アセンブル資料に基づき、LZH 内の説明書本文を直接確認したものではない。v1 の4音差分は未解決で、変換コードは変更していない。
- FACTS F-033 を追加。直近の docs-only CI run [39230371014](https://github.com/pikachu364/glosa/actions/runs/39230371014) は全体成功。


## 2026-10-19 — 逆解析ノートの再読と重複記録の整理

- `docs/reverse/README.md`, `FACTS.md`, `HYPOTHESES.md`, `NEXT_STEPS.md`, `LOG.md` を再読し、既存の事実・仮説・未完了タスクを照合した。
- SNG2S.DOC の対応バージョン、timebase、出力オプションは既に FACTS F-001 に記録済みだった。直前の調査で同じ内容を F-034 として重複追加していたため、F-034 を削除し、F-001 の根拠表記にリポジトリ内の説明書を明記した。
- NEXT_STEPS T-003 の説明書確認項目も既存の完了項目だった。重複作業を避けるため、同項目を F-001 参照に整理し、改行が文字列として混入していたチェックリストを修正した。
- 既存の未解決事項は変わっていない。特に v1 の +80 レイアウトは容量整合性だけが確認済みで、イベント単位の照合・SNG2GS 側の独立根拠が不足している。Tie の同音高 Note-off 所有関係と参照 MIDI の生成条件も未確定。
- この整理では `SngConverter.cs` やテストコードを変更していない。次は T-002 の v1 パート境界とイベント差分、T-003 の参照 MIDI 生成条件の特定を優先し、既に完了した解析を繰り返さない。


## 2026-10-19 — SNG2S の版別ヘッダ処理を確認

- `sng2gs解析データ.txt` の `FUN_1000_272a` を読み、`BA` シグネチャ確認後にヘッダの版値から内部識別値 0〜3 を選ぶこと、識別値 0 と非 0 で後続のヘッダ項目参照オフセット群が分岐すること、識別値 3 でパート数が 16 になることを確認した。
- これは SNG2S 側が旧版と後続版を異なるヘッダ構造として処理する根拠になる。一方、デコンパイル上のローカル変数から SNG ファイル内の絶対オフセットへ直結する対応はまだ未確定であり、v1 の +80 レイアウト候補を証明するものではない。
- FACTS F-034 を追加。converter とテストは変更していない。


## 2026-10-19 — SNG2S のパート情報バッファを追加調査

- `FUN_1000_272a` の版分岐を続けて確認。各パートについて 0x50 byte の情報バッファを確保する経路があり、旧版クラスではフィールド参照基準 `local_64=0x32`、後続版では `0x23` に切り替わる。また後続版経路は `+0x0F` / `+0x13` の値を参照する。
- 版ごとにパート情報の解釈位置が違う点を FACTS F-035 に記録。これは v1 の別レイアウト仮説を支えるが、SNG ファイル上の絶対オフセットや候補 +80 レイアウトとの同一性はまだ示せない。
- `SngConverter.cs` とテストは変更していない。


## 2026-10-19 — SNG2S のパート情報バッファを追加調査

- `FUN_1000_272a` の版分岐を続けて確認。各パートについて 0x50 byte の情報バッファを確保する経路があり、旧版クラスではフィールド参照基準 `local_64=0x32`、後続版では `0x23` に切り替わる。また後続版経路は `+0x0F` / `+0x13` の値を参照する。
- 版ごとにパート情報の解釈位置が違う点を FACTS F-035 に記録。これは v1 の別レイアウト仮説を支えるが、SNG ファイル上の絶対オフセットや候補 +80 レイアウトとの同一性はまだ示せない。
- `SngConverter.cs` とテストは変更していない。


## 2026-10-19 — SNG2S のバイナリ読込と版別パート幅を確認

- FUN_1000_272a が FUN_1000_572a(..., 0x8000) でファイルを開く経路を追跡。open wrapper のフラグ設定と FUN_1000_58ce の条件分岐から、この経路ではテキスト用 CR/LF・Ctrl-Z 処理が有効にならないことを確認した。
- パートループは各パートの 80-byte 情報バッファを読み、旧版クラスでは追加 byte を読まず、後続版クラスでは直後に 1 byte 追加で読む。従って SNG2S のコード上、旧版／後続版の固定読込幅はそれぞれ 80 / 81 byte。
- FACTS F-036 と H-008、T-002 を更新。これにより v1 の +80 候補には容量整合性以外の SNG2S 側の裏付けが加わったが、サイズ表の基準位置・各長さフィールド・イベント列開始位置は未確定。converter とテストコードは変更していない。


## 2026-10-19 — runner.SNG v1 のパート境界・長さフィールドを再照合

- v1 runner.SNG のバイト列を再走査し、headerEnd 候補 0x74、パート開始位置と entry+80 サイズ、+80 の名前らしい文字列、+115/+117/+119 の長さ候補、+161 の列開始候補を10パートすべてで確認した。
- 10パート全てで note length が8の倍数、controller length が4の倍数となり、3列長合計は各パートの size-161 以内。これは +80 候補の構造的整合性を補強する。
- 算出終端 85832 に対してファイル長は 85897 で、末尾65 byteが残る。用途は未解明。F-037 にパートごとの開始位置・サイズ・列長を記録した。
- 参照 MIDI の差分4音、サイズ表の正式な意味、末尾65 byteの役割は未解決。converter とテストコードは変更していない。


## 2026-10-19 — v1 のレコード開始位置を SNG2S 読込順と参照 MIDI で再検証

- `FUN_1000_272a` の旧版クラスでは 80-byte 情報バッファの後に note 列を読む。runner.SNG の候補パート開始位置に対して情報バッファは +65、note length は +115、controller length は +117、note record 列は +145 と対応する。
- 以前の候補 +161 は16 byte後ろであり、SNG2S の read 順と参照 Note-on に一致しないため撤回。+119 は単一の index length ではなく、v1 経路で走査される13個の16-bit値の先頭と判断し、意味は未確定として記録。
- +145 から8-byte recordを読み、Tie bit の立ったレコードを新規 Note-on から除外するモデルでは、全10パートの Note-on 3,295件について、参照 MIDI と channel/pitch/velocity/tick（source tick +192）の署名差分が0件。
- Note-off は同じモデルで未一致が残る（part 2/3/10）。レイアウト全体の確定や converter 実装変更はまだ行わない。FACTS F-038 と H-001/T-002 を更新。


## 2026-10-19 — v1 の13個の値と DOS 相対 seek の関係を確認

- `FUN_1000_272a` の v1 経路で、ノート列長・コントローラ列長を読み取った後に `local_64` を進め、13個の16-bit値を順番に読み取るループを確認。
- `FUN_1000_40c8` の命令列が引数を `AX/BX/CX/DX` に配置して DOS `INT 21h` を呼ぶことを確認。`AX=0x4201`、`CX=0`、`DX=value` は現在位置基準の相対 seek を指定するため、13値は順次適用される seek 距離と判断できる。
- これにより「+119 は単一の列長ではない」だけでなく、値の直接用途が相対 seek であることまで明確になった。ただし、13回の移動後の位置と各パート境界候補の一致、末尾65 byteの意味、part 2/3/10 の Note-off 残差は未解決。
- FACTS F-039、NEXT_STEPS、HYPOTHESES H-001 を更新。converter とテストコードは変更していない。


## 2026-10-19 — v1 の seek 累積位置を全10パートで検証

- runner.SNG のサイズ表を正しく4-byte strideで読み、先頭値を初期位置の相対値、後続10値を各パートサイズ候補として適用。各パート開始候補は 1510, 2820, 31298, 42292, 48356, 52918, 57762, 66608, 69454, 76390。
- 各パートで情報バッファ読み取り後の `+145` から note length と controller length のデータを読み、13個の相対 seek 距離を順次加算した。全10パートで結果位置は次パート開始候補 `+65` と一致し、差分は0 byte。
- 最終パートの seek 後位置は 85,897 byteで、runner.SNG のファイル長と一致。以前「境界候補85832の後ろに未説明の65 byteがある」としていた扱いを改め、次の情報バッファ開始位置がEOFに到達する関係として説明した。
- FACTS F-040、NEXT_STEPS、HYPOTHESES H-001 を更新。Note-off tick 残差（part 2/3/10）は残るため、converter は変更していない。


## 2026-10-19 — v1 Note-off 残差の具体例を抽出

- `runner.SNG` part 10 の record file offset 76607 / 76615 を確認。pitch 69、velocity 21、duration 1、Tie flagなしの2レコードがどちらも tick 1728 に Note-on を出す。
- 参照 MIDI では同 pitch/channel の Note-off が tick 1728 と 1729 に分かれる。単純な onset + duration モデルでは両方 tick 1729 になるため、Tie がない場合にもスケジューラ側の別規則が必要。
- 同様の組み合わせは tick 1740 以降も12-tick間隔で繰り返される。原因はまだ断定せず、H-010 として検証項目を分離。converter とテストコードは変更していない。


## 2026-10-19 — Note-off タイマー競合経路を逆アセンブルで追跡

- `FUN_1000_3A10` が `0xFA0` のタイマー表へ4-byte項目を登録すること、`FUN_1000_3A3F` が同じ識別値の項目を検索し、既存カウンタと新規長さを比較することを命令列で確認。
- `FUN_1000_3406` の処理冒頭でタイマーを減算し、期限切れ時に出力キューへ3 byteを追加することを確認。
- ノートレコード経路では `FUN_1000_3A3F` の戻り値によって新規タイマー登録 `FUN_1000_3A10` がスキップされる分岐がある。従って同じ識別値を持つノートは独立したタイマーとして常に登録されるわけではない。
- この内部処理は v1 part 10 の同 tick・同音高 Note-off 残差を説明する候補だが、参照 MIDI とのイベント単位の因果対応はまだ未証明。F-042 / H-011 として記録。converter とテストコードは変更していない。


- 追加で `FUN_1000_3B2F` と呼び出し元 1000:37E4–37FD を接続して確認。`AL` は Note-on status + channel、`AH` は pitch、`DL` はレコード長として渡される。`FUN_1000_3A10` が AL/AH を保存し、`FUN_1000_3A3F` が AX と保存 word を比較するため、timer lookup key は channel/status + pitch で velocity を含まない。FACTS F-043 に記録。


## 2026-10-19 — v1 Note-off タイマー規則のオフライン再現が差分0

- `runner.SNG` の record stream と参照 `runner.MID` を比較し、タイマー処理を静的命令列に沿ってモデル化。
- 同一 status/channel + pitch の既存タイマーを検索し、残り値と新 duration を比較。期限切れ、衝突時の即時 Note-off、`0x40` の新規登録抑止、`0xC0` の即時 Note-off 抑止を含めた。
- Note-on 3,295件に加え、Note-off 3,295件も全10パートで `(channel, pitch, tick)` の差分0。これは静的逆アセンブルからのオフライン再現で、元 EXE の実行時トレースではない。
- FACTS F-044、NEXT_STEPS、HYPOTHESES H-011 を更新。コンバーターはまだ変更していない。


## 2026-10-19 — v1 converter 経路と参照回帰テストを追加

- `SngConverter` に v1 専用 layout を追加。`tableEnd+2`、part size `entry+80`、info buffer field offsets `+115/+117`、record stream `+145`、name `+69` を使用し、13個の seek 値の累積検証を行う。
- v1 Note-off を `FUN_1000_3406` / `FUN_1000_3A3F` / `FUN_1000_3A10` の静的モデルに沿って生成する専用経路を追加。v2/v3/v4 の既存経路は維持。
- `BalladeV1MatchesSng2GsReferenceNoteEvents` 回帰テストを追加し、`runner.SNG` と `runner.MID` の Note-on/off を比較する。
- コードは work branch `docs/ballade-version-testdata` にコミット済み。CI run [39251768760](https://github.com/pikachu364/glosa/actions/runs/39251768760) は確認時点で進行中で、成功／失敗はまだ確定していない。
