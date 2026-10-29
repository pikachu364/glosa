# 調査ログ

## 2026-10-25 — tick-8 SysEx format and checksum expressions recovered

- Followed `FUN_1000_407F` to establish that template output stops at byte `0xFF`; traced `FUN_1000_3BA7` instructions for both tick-8 message bodies.
- Recorded message forms `template + 10 00 16 t checksum F7` and `template + 52 00 10 t checksum F7`, with checksum expressions `(-(t+0x26)) & 0x7F` and `(-(t+0x62)) & 0x7F`, where `t = low byte of 0x7DE`.
- The output lengths match the two 10-byte reference events if the template has four bytes. Template contents and runtime `0x7DE` remain unverified; no byte-for-byte mapping or runtime execution is claimed.


## 2026-10-25 — v1 tick-8 SysEx producer path narrowed

- Static call-chain review found startup calls `FUN_1000_3B68` after `FUN_1000_3406`; `FUN_1000_3B68` can call `FUN_1000_3BA7` when `0x7DE` changes.
- `FUN_1000_3BA7` contains template-plus-six-byte output paths consistent with 10-byte messages if the template is four bytes. This is a supported candidate for the two 10-byte SysEx events at tick 8, not yet a byte-for-byte mapping.
- Recorded the bounded result as FACTS F-101 and retained template bytes / runtime branch conditions as open work. No converter code, tests, or MIDI fixtures were changed.


## 2026-10-25 — SNG2GS /DEF の位置づけを確定

- SNG2S Ver. 3.3 説明書の記述に基づき F-051 を整理し直した。`/DEF` は MIMPI V4 のチャンネル音源名・トラック名表示用ファイルの自動生成であり、MIDI 変換・演奏の必須要件ではない。
- ユーザー指示により、この点の追加調査は行わない。Glosa の変換実装は変更していない。


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


## 2026-10-20 — 調査記録の現状整合性を修正

- HYPOTHESES.md の H-001 / H-008 / H-009 / H-010 / H-002 を最新の FACTS F-040〜F-045 と照合し、Note-off 残差と末尾65 byteを未解決とする古い記述、v1 converter 未対応とする古い記述を更新した。
- NEXT_STEPS.md の T-002 / T-007 に残っていた「v1テスト未対応」「末尾65 byte未解決」の記述を履歴と現在の状態に分け、13 seek 値の累積位置は解決済み、各値の個別用途と controller/tempo/meta/track chunk 順は未解決と整理した。
- 実装の確認済み範囲は v1 Note-on 3,295件・Note-off 3,295件の参照 MIDI 一致と、CI run [39253494378](https://github.com/pikachu364/glosa/actions/runs/39253494378) の Ubuntu/macOS/Windows 成功。これは静的解析に基づくオフライン再現と Note イベントの照合であり、SNG2S 実行時トレースや MIDI 全イベントの完全一致を意味しない。
- 次の技術調査は、v1 の controller/tempo/meta event を参照 MIDI と比較するテスト設計、13 seek 値と後段バッファ参照の対応付け、そして実再生確認。実再生はユーザー環境が必要なため、実施済みとは扱わない。


## 2026-10-20 — v1 controller/program/pitch-bend eventsを参照 MIDI と照合

- `runner.SNG` の各パートで note stream 直後の4-byte streamを走査し、B0 controller / C0 program change / E0 pitch bend 候補1,966件を抽出した。status 種別と controller number ごとに連続する同一状態を抑制すると1,595件となり、参照 `runner.MID` の同種イベント1,595件と tick/status/data1/data2 の全件一致を確認した。
- tick の対応は、source tick 0 の初期状態イベントが MIDI tick 8、それ以降が source tick +191。v1 専用 converter にこの時刻変換と状態重複抑制を実装し、v1 では参照に存在しない RPN 初期化イベントを出さないようにした。v2〜v4 の controller 経路は維持する。
- `BalladeV1MatchesSng2GsReferenceControllerAndProgramEvents` テストを追加した。CI run [39333267230](https://github.com/pikachu364/glosa/actions/runs/39333267230) は Ubuntu/macOS/Windows で成功し、新しい v1 controller/program/pitch-bend 回帰テストも CI を通過した。
- この一致は fixture のオフライン比較に基づき、SNG2S 実行時トレースではない。tempo/meta event、SMF track chunk 順、実再生・聴感確認は継続課題。


## 2026-10-20 — v1 tempo meta の汎用パーサー不一致を検出

- 参照 `runner.MID` の conductor track を解析し、tempo meta が tick 0 = 500,000、tick 8 = 869,000、tick 192 = 869,565 µs/quarter、time signature が tick 0 に 4/4 であることを確認した。
- v1 conductor の sentinel（file offset 761）後を現行 `ReadTempi` と同じ方法で読むと step 37 / value 144、step 37 / value 192 ... となり、現行の tempo 変換式では参照 tempo と一致しない。
- SNG2S の静的命令列には初期 500,000 tempo meta と、`0x7BA` が120以外のとき `FUN_1000_618A` で `60,000,000 / tempo` を計算して tempo meta を出す経路がある。ただし tick 8 / 192 の参照イベントと SNG のどのフィールドが対応するかは未確定。
- FACTS F-053、HYPOTHESES H-012、NEXT_STEPS T-003 を更新。根拠が不足するため tempo conversion のコードは推測で変更せず、tempo/meta は未検証のままとする。


## 2026-10-20 — v1 controller 比較テストの CI 完了

- GitHub Actions run [39333267230](https://github.com/pikachu364/glosa/actions/runs/39333267230) の完了を確認。Ubuntu/macOS/Windows の CI はすべて成功。
- FACTS F-052 の CI 状態を「確認待ち」から成功に更新。tempo/meta event と track chunk 順、13 seek 値の個別の意味、実再生は引き続き未完了。


## 2026-10-20 — v1 参照 MIDI の物理 track chunk 順を確認

- `v1/sng2gs/runner.MID` は12個の MTrk chunk を持つ。タイトル文字列を含む track、名前なし track、part 10→2 の Note-on track 9本、空の Keyboard/part 1 track の構成を確認した。
- Note-on track は channel 9→1 の降順で、part は channel+1 に一致。現在の Glosa writer の conductor + part 追加順とは物理 track 順が一致しない可能性があるため、直ちに並べ替えはせず F-054 と T-002/T-006 に残課題を記録した。
- v1 controller/program/pitch-bend 比較テストは CI run 39333267230 で Ubuntu/macOS/Windows 全て成功。tempo/meta 変換と track chunk order はまだ完了扱いにしない。


## 2026-10-20 — SNG2S conductor metadata buffer consumersを追跡

- FUN_1000_272a の初期化処理で、3つの内部 far-pointer 0x21DC / 0x21E0 / 0x21E4 に別々の確保済みバッファを設定することを確認。
- FUN_1000_3406 の命令列で 0x21DC が FF 58 04 time-signature meta、0x21E0 が FF 51 03 tempo meta の出力経路に接続することを確認。tempo stream の値は value * (word stored at memory address 0x2962) / 0x40 で1〜480に制限され、60,000,000 / value から microseconds-per-quarter を作る。
- 0x21E4 は別レコード列から 0x7DE を更新して FUN_1000_3B78 を呼ぶ。出力イベントの意味は未確定として断定しない。
- これらは内部ポインタであり、SNG ファイル内の直接オフセットではない。SNG reader が各 buffer をどこから埋めるか、v1 参照 tempo の tick 8 / 192 とどのレコードが対応するかは未解決。converter の tempo 式は変更していない。
- 作業ブランチの現行 head 650c5fa44982b9f82ce58ea005d4f7e831c4a4d9 に紐づく CI run 39334626186 は成功を返した。これは当該 head の CI 状態であり、今回の追加テストコミットの CI 結果ではない。


## 2026-10-20 — v1 汎用 tempo parser の誤適用をガード

- 現在の `docs/ballade-version-testdata` ブランチを確認し、v1 conductor に `ReadTempi` を使うと参照 `runner.MID` の tempo meta と一致しない問題（F-053）を再確認した。
- v1 では汎用パーサーを呼ばず、根拠のない tempo change を出力しない暫定ガードを実装。テストで参照 MIDI の tempo meta（tick 0/8/192）を固定し、現 converter が既定 tempo のみを出力することを検証する。
- **当時の状態:** このログ時点では参照 MIDI の tick 8 / 192 tempo change は未実装だった。後続の実装状況は現在の `SngConverter.cs` と `SngConverterTests.cs` を参照する。実行時トレース・実再生を行った記録ではない。
- コードコミット `ca99066649e5a5341e820486fd526c4d5ed4ac31`、テストコミット `cf77ba8736e36aa68bc9caa14d122136fc0a7b48`。branch HEAD `28a668fe56485f03fb571c692717bdbb5162797c` の CI run [39400722489](https://github.com/pikachu364/glosa/actions/runs/39400722489) で build/test が Ubuntu/macOS/Windows の全 job で成功した。


## 2026-10-20 — v1 reference MIDI の物理 track layout テストを追加

- runner.MID の SMF reader 出力を使い、12 track、track-name meta を持つ chunk index、Note-on を持つ track の物理順・channel・件数、空の part-1/Keyboard track を回帰テストに記録した。
- このテストは reference fixture 自体の構造を固定するもので、Glosa の出力が同じ物理 chunk 順になったことを意味しない。現 converter は conductor + part 昇順で書き出し、参照は title/conductor を含む先頭 track、別の Exclusive/SysEx track、part 10→2、part 1 の順である。実装前に track 1 の SysEx 内容と SNG 側の出所を確認する。
- テストコミット b03cebb06a27c9efe509447d8bb369c8fa89c499。最新 branch HEAD の CI run 39400722489 で3 OSの build/test 成功を確認。


## 2026-10-20 — v1 reference Exclusive track の SysEx を記録

- runner.MID の track index 1 は Text meta `Exclusive` と4件の SysEx を持つことを raw SMF parse で確認。イベント tick は 0 / 4 / 8 / 8、payload は FACTS F-057 に記録した。
- SNG2S のマニュアルに `/X1` / `/X2` / `/XD` があるため、reference SysEx と option の関係、元 SNG の生成元を追う。既存資料だけでは mapping を確定できないため converter には payload を追加しない。
- reference track 1 の4件を固定する回帰テストを作業ブランチへ追加（commit `cefcac720b993871534ce655a12494462d9f99e5`）。初回 CI は `SmfReader.GetData()` が SysEx の `F0` を含む表現を返す点で期待値がずれたため失敗。期待値を reader の実際の表現に合わせて修正（commit `335b23e0aef2763f1eaadd949a7a9804267a4f28`）し、CI run [39401367632](https://github.com/pikachu364/glosa/actions/runs/39401367632) で Ubuntu/macOS/Windows の build/test が成功。


## 2026-10-20 — v1 SysEx payload の SNG 内 literal 検索

- 元 `runner.SNG` 全体（85,897 bytes）を走査し、参照 MIDI track 1 の4つの SysEx payload および共通先頭列 `41 10 16 12` の literal byte sequence は見つからないことを確認。
- この不在だけで SNG2S が SysEx を合成すると断定できない。SNG 内の別フィールドから組み立てる可能性も残るため、H-013 として仮説を登録し、/X1 /X2 /XD の分岐および SysEx 出力関数の静的解析を次の調査項目とした。


## 2026-10-21 — v1 tempo 初期化と 192-tick grid の切り分け

- runner.SNG offset 0x00B5 の実バイト列が `C0 00 04 04` であることを確認。FUN_1000_272a が読み込む内部 buffer 0x21DC の先頭 byte 192 は FUN_1000_0010 で uVar11 として利用される。
- FUN_1000_61EC(currentTick, uVar11, 0) の剰余と uVar11 - remainder によって次の 192-tick 境界への delta を計算する経路を確認。
- 初期 500,000 µs/quarter tempo は FUN_1000_33D4 の tempo record 処理より前に出力される。record countdown と grid-based tempo output は別の段階として扱う。
- tick 8 の 869,000 µs/quarter の生成元と、tick 192 の 869,565 µs/quarter の出力経路はまだ確定していない。静的解析のみであり、converter/test code は変更していない。


## 2026-10-21 — v1 tempo integer formulas separated

- Raw tempo 37 was traced through `FUN_1000_3406` to 69 BPM.
- The tempo-record arithmetic `(60000 / 69) * 1000` produces 869000 µs/quarter, matching the reference tick-8 value.
- The grid-aligned arithmetic `60000000 / 69` produces 869565 µs/quarter; the entry path calculates the distance to the next 192-tick boundary before emitting it, matching the reference tick-192 value.
- Recorded the calculations in FACTS F-065 and added the confidence boundary to V1_TEMPO_INVESTIGATION.md and HYPOTHESES H-012. The tick-8 absolute-tick assignment is not runtime verified; no converter or test code changed.

## 2026-10-21 — 初回 tempo queue drain の例外経路を追跡

- `FUN_1000_1C70` の v1 分岐から `FUN_1000_1CE4`、続いて entry path の `FUN_1000_33D4`、その中の初回 `FUN_1000_3406`、呼び出し元の `FUN_1000_08EC` までを静的に追った。
- `FUN_1000_1CE4` は条件 `(0x6F8 == 0) || (0x6FA == 2)` が成立する経路で、queue drain の前後に `0xF9A:0xF9C = 0x6EE:0` を設定する。2回目の drain は pending delta を flush して absolute tick `0x6F2:0x6F4` を更新する。
- `FUN_1000_3DE2` が `0xF96 = 0xFFFF` に設定し、`FUN_1000_33D4` は初回 `FUN_1000_3406` の後で `0xF96 = 0` にする。よって初回 `FUN_1000_3406` は通常経路の `0xF9A:0xF9C += 1` を通らない。
- `FUN_1000_33D4` の後、`0x622 != 0` なら呼び出し元が pending delta をゼロ化してから初回 queue drain を行う。`FF` meta の consumer はその時点の pending delta を flush する。
- 未解決: `0x6EE` の実値・由来、参照 MIDI 生成時の `0x622` 状態、preamble 後の absolute tick。したがって tick 8 の割当は未証明。静的解析のみで、converter / test code は変更していない。FACTS F-066、H-014、T-013、V1_TEMPO_INVESTIGATION.md を更新。


## 2026-10-21 — 進捗スナップショットと残課題の整理

- reverse docs を見たときに実装状況と未解決点を一目で把握できるよう、NEXT_STEPS.md の先頭に現状・証拠レベル・次の作業順を追加。
- 0x622 は FUN_1000_184A が option token を検出したときに設定される。SNG2S.DOC の /F（track generation order change）説明と下流の track-order 分岐は整合するが、文字列テーブル実体は未確認のため H-015 の推定として記録。reference invocation に /F があったかも未確認。
- 0x6EE は複数の event-output path で pending delta として使われるが、解析テキストでは直接書き込みを確認できない。初期値・由来を未解決として維持。
- tempo の整数計算と初回 scheduler 例外経路は静的に追跡済み。一方、tick 8 への実際の時刻割当、runtime trace、実再生確認は未実施。
- converter / regression test code は変更していない。PR #5 は open / unmerged のまま。


## 2026-10-21 — 0x6EE の全命令参照と option token 比較を追加調査

- `sng2gs解析データ.txt` 全体を命令行で走査し、`[0x6EE]` の29参照がすべて直接読み出しであることを確認。直接 store は見つからなかったが、data segment・間接書き込み・別名アドレスを含めた値の由来は未解決。
- `FUN_1000_1CE4` の preamble queue drain 後、`0x6EE` は `0x1ED8:0x1EDA` に加算され、`0xF9A:0xF9C` にセットされる。同じパターンが別の event-output path にもあり、共通の delta/間隔値という仮説を H-016 として記録した。単位や MIDI tick との関係は未確定。
- `FUN_1000_184A` が `FUN_1000_5EC8(input, 0xF05, 2)` を呼ぶこと、比較関数が一致時に0を返すことを確認。token の長さは2 bytesだが、実文字列は解析資料にないため /F 対応を確定できない。
- FACTS F-067、HYPOTHESES H-015 follow-up / H-016、NEXT_STEPS T-013 を更新。converter / regression tests は変更なし。runtime trace・実再生も未実施。


## 2026-10-21 — 参照 MIDI の SysEx tick から 0x6EE 候補を絞る

- 参照 `runner.MID` を SMF bytes から直接解析し、format 1 / 12 tracks / division 48、conductor tempo tick 0 / 8 / 192、track 1 SysEx tick 0 / 4 / 8 / 8 を確認した。
- `FUN_1000_1CE4` は条件成立時に2回 queue drain し、それぞれの後で `0x6EE` を pending delta にセットする。最初の2 drain が SysEx tick 0 / 4 に対応するなら `0x6EE = 4` と説明でき、続く tempo tick 8 の候補経路にもつながる。
- ただし queue payload と SysEx payload の対応、`0x6F2:0x6F4` / `0xF9A:0xF9C` の実値、`0x622` の実行時値は未確認。4 ticks は仮説であり、converter は変更しない。
- FACTS F-068、HYPOTHESES H-016 follow-up、V1_TEMPO_INVESTIGATION.md、NEXT_STEPS.md を更新。runtime trace・実再生は未実施。


- 追加照合: `FUN_1000_1CE4` の最初の2イベント生成経路は共通 SysEx prefix、異なるデータ文字列 (`0xF34` / `0xF3F`)、checksum、`F7` terminator を使う。参照 track 1 の最初の2 SysEx も共通 prefix・payload 12 bytes で tick 0 / 4 にあるため、2つの drain がこの2件に対応し `0x6EE = 4` となる推定は強まった。文字列 data segment がなく、payload の byte-for-byte 同一性はまだ確認できない。


## 2026-10-21 — option flag mapping の再評価

- `0x622` と `/F` の対応を再検討。`FUN_1000_08EC` では `0x622 != 0` が SysEx payload の抑制に使われ、`FUN_1000_0010` では `0x626` が track-order table の選択に使われる。
- help text の説明（`/XD`: Exclusive disable、`/F`: part-to-track order change）と照合し、最有力対応を `0x622 ↔ /XD`、`0x626 ↔ /F` に修正。旧 `0x622 ↔ /F` 仮説は撤回する（FACTS F-069）。token bytes の直接対応・参照実行時の引数とフラグ値は未確認。
- 参照 MIDI に SysEx が4件あることは `/XD` 無効を示唆するが、生成時の引数を証明するものではない。tick 8 の pending-delta 説明は静的推定のまま維持し、converter / tests は変更しない。
- `NEXT_STEPS.md` と `V1_TEMPO_INVESTIGATION.md` を更新。HYPOTHESES.md の H-015 には旧対応の記述が残っており、文書間の同期が未完了である点に注意。


## 2026-10-22 — reverse 文書全体の整合性監査

- `docs/reverse/` 以下の README、FACTS、HYPOTHESES、LOG、NEXT_STEPS、V1_TEMPO_INVESTIGATION、BALLADE_LAYOUT_COMPARISON、引継ぎ文書を読み直した。
- 現在の結論は、SNG2S の tempo-record 数値計算と 192-tick grid 経路は静的解析で説明できる一方、tick 8 の delta / absolute-tick 状態、`0x6EE` の初期値、SysEx payload の対応、参照生成時の実オプションは未解決というもの。
- README に残っていた「H-015 の同期未完了」という記述は更新。HYPOTHESES の `0x6EE = 4` は旧仮説として明示し、後続の否定的な制約を優先するよう注記した。
- FACTS の重複 ID を F-089〜F-092 に改番し、参照を更新。
- 比較文書の節番号が「10」で重複していたため、後続の tempo/SysEx 追加節を 14 に整理する。過去の調査ログは削除せず、履歴として残す。
- 文書だけの監査であり、converter / regression tests は変更していない。private fixture は公開せず、PR #9 はマージしていない。

## 2026-10-22 — MIMPIV4 driver setup と tempo consumer を分離

- 静的解析で `FUN_1FF5_1344` の driver dispatch、選択された初期化関数から `0x0438` → `0x0440` のコピー、`FUN_1FF5_0D61` の INT 8 hook、`FUN_1FF5_0DFA` の divisor 計算と I/O port 書き込みを整理した。
- この経路は timer の初期化を示すが、SNG tempo record の第2 word が playback 中に timer globals を更新する呼び出し経路はまだ確認できていない。
- FACTS F-092 と NEXT_STEPS に反映。静的解析のみで、runtime trace・実再生・converter 変更はなし。


## 2026-10-23 — private v1 fixture mismatch の原因を確定

- private repo の Actions run 一覧と job logs を取得し、`Private Ballade fixture validation` run #6（ID `39699711256`）が成功、build 成功・977 tests passed であることを確認した。workflow は Glosa commit `440000b971e72debcef72bb12407bcf4e944527a` を checkout していた。
- 先行 run #4/#5 の `PrivateV1RunnerMatchesReferenceNoteEventsPerTrack` は、同 tick / channel の Note-off pitch 77 と 82 の順序だけが比較配列上で逆転して失敗していた。tick/channel/key/event type/velocity を安定化した canonical sort 後の run #6 では成功した。
- したがって、この差分は converter の Note-event mismatch ではなく、テスト比較順序の問題と確認できた。v1 TEST.SNG tempo test も含めて pass。converter の追加変更は不要。
- 現在の PR #9 head `3463baf2f59c2e31fc76c00846e88ac5e713f2b4` と private run の checkout を比較すると、後続の変更は docs のみで converter / tests に差分なし。PR #9 は引き続き open / unmerged。
- 制約: tempo-event equality と Note-event signature equality の範囲を確認しただけで、SysEx payload、physical track/chunk order、全 controller/program/pitch-bend、runtime playback、byte-for-byte SMF equivalence は証明していない。


### MIMPIV4 tempo investigation update (2026-10-24)

The latest static pass inspected `FUN_1000_341E`, `FUN_1000_3543`, `FUN_1000_2D27`, and `FUN_1000_8888`. The first three are special-record/event pre-scan or Program Change processing paths and do not show writes to `0x0438`, `0x0440`, or `0x7BF0`. The text-command path does schedule a parsed relative value plus `0x0440`, but its connection to binary SNG tempo records is unproven.

Keep T-016 open: the consumer is still unidentified, and static analysis is not runtime proof. Do not transfer SNG2S MIDI-export tempo formulas to MIMPIV4 playback.


### Documentation structure correction (2026-10-24)

- MIMPIV4 の追加調査結果を末尾の独立節から既存の項目へ移動した。`NEXT_STEPS.md` は既存の `T-016 — MIMPIV4 legacy tempo records to playback timer` に調査済み経路・未解決点・次の検証手順を統合。
- `FACTS.md` の既存 F-084、`HYPOTHESES.md` の H-017、`README.md`、tempo investigation、layout comparison、引継ぎ文書の既存セクションへ内容を統合し、重複した追記節は削除した。
- 今回の静的解析で除外できた候補は `FUN_1000_341E`、`FUN_1000_3543`、`FUN_1000_2D27`。`FUN_1000_8888` は text-command delay + `0x0440` の経路だが、binary SNG tempo record との接続は未確認。
- T-016 は未完了。tempo record を無視するとの結論にはせず、実際の playback event dispatcher の追跡、または既知の tempo 変化 fixture による runtime trace を次の根拠とする。
- `docs/reverse/` の9文書を再確認。private fixture / 解析 dump は公開 repository に追加していない。実装変更、テスト、実再生は未実施。

## 2026-10-24 — v2 legacy setup parameter path traced; override flags remain unresolved

- Traced `FUN_1000_272A`'s version-class branch and the v2 `SHIZUME0.SNG` version-block mapping: `0x4E` bytes are read to `0x1AE2` from file offset `0x73`; the inspected sample selects the fallback pair at `0x1AF4` / `0x1AF6` with values 6 and 6 and sets `0x7BE = 5`.
- Confirmed that `0x7BC` bits 0–2 gate assignments to `0x7C0`, `0x7C2`, and `0x7C4`; set bits preserve the prior values. A search of the available static export found no direct store to `0x7BC`, which is not proof that no writer exists elsewhere.
- Corrected the formatter dataflow: `%c` consumes one byte argument; `FUN_1000_1CE4` supplies the three byte values in order to the `0xF34` and `0xF3F` templates. The `0xFE` marker becomes `0x00` before checksum generation. The routine can also emit a separate `0xF4A` event on another branch.
- The all-bits-clear candidate does not match the previously extracted v2 `SHIZUME0.MID` payload values. No final `0x7BC` state or exact producer-to-payload mapping is established.
- Added F-093 / H-019 / T-018 and a focused comparison section. No converter or test code was changed; no build, test, runtime trace, MIDI regeneration, or playback was performed. Private fixture bytes and reverse exports remain private.

## 2026-10-24 — full-export cross-reference audit for v2 setup override flags

- Re-scanned the complete private SNG2S analysis export (18,795 lines). The only `0x7BC` references are three decompiler bit tests and the corresponding three assembly `TEST` instructions; no direct store appears in the available text export.
- Cross-checked the related globals: `0x7C2` and `0x7C4` have no other visible direct store sites beyond the loader's conditional assignments. `0x7C0` has additional writes in the formatter path. The export's `STRINGS / DEFINED DATA` section does not provide initialized bytes for `0x7BC`, so this pass cannot establish the flag's initial value or rule out indirect/aliased writes.
- Updated the existing F-093, H-019, and T-018 sections rather than adding duplicate investigation items. The payload mismatch remains unresolved; no converter/test change, build, runtime trace, MIDI regeneration, or playback was performed. Private SNG/MIDI fixtures and analysis dumps remain private.

## 2026-10-24 — v2 setup SysEx producer paths reconciled

- Original `SNG2S.EXE` initialized data confirms the on-disk initial byte at `DS:0x07BC` is `0x00`; this does not substitute for runtime state, and indirect/aliased writes remain possible.
- Matched the distinct reference-MIDI event pairs: `FUN_1000_23C0(4)` explains ticks 4/8, while `FUN_1000_1CE4` templates explain ticks 100/104. The comparisons include calculated data, checksums, and terminators.
- Corrected the interpretation of the earlier mismatch: it compared two different producer paths. Updated the existing H-019, README conclusion, and comparison section 15; T-018 remains complete for static producer mapping.
- No converter/test code changed; no runtime trace or playback was performed. Private fixtures and full raw event sequences remain private.

## 2026-10-24 — SNG2S `0x6EE` reference audit

- Re-scanned the complete private SNG2S analysis export (18,795 lines). The address appears 58 times across decompiler and assembly views; the assembly references are reads, and no direct store to `0x6EE` appears.
- This does not establish the initialized value: the text export omits initialized data-segment bytes, and indirect/aliased writes are not excluded. The proposed value `4` remains unproven.
- Updated H-016 and the existing T-017 checklist to record the completed full-export scan and keep the remaining work focused on recovering initialized data/runtime state, the v1 setup-SysEx payload mapping, and reference command-line options.
- No converter/test code changed; no build, test, runtime trace, or playback was performed. Private executable, fixtures, and analysis export remain private.


## 2026-10-24 — T-017 tick-8 queue-order trace

- Confirmed main startup order in SNG2S: `1000:0448` calls `FUN_1000_1C70` (which dispatches to the v1 `FUN_1000_1CE4` setup producer for the legacy path); `1000:044B` then calls `FUN_1000_33D4`; and `1000:0463` calls `FUN_1000_08EC` to drain the queued tempo event.
- In the v1 setup producer, each of the first two SysEx-style events is drained by `FUN_1000_08EC`, then `0x6EE` is copied into pending delta `0xF9A:0xF9C`. With the confirmed on-disk initial value 4, this advances the setup-event sequence by four ticks per drain.
- `FUN_1000_33D4` calls `FUN_1000_3DE2`, then `FUN_1000_3406` while `0xF96` still has its initial nonzero value; the first scheduler call therefore skips the normal pending-delta +1 path. The main routine then preserves pending delta when `0x622 == 0), drains the queued tempo event at `1000:0463`, and computes the next delta from the absolute tick/grid calculation.
- This static call-order chain explains the reference tick-8 coarse tempo placement under the observed legacy path and the `0x622 == 0` condition. It is not a runtime trace; indirect writes and fixture-specific state still bound the conclusion.
- No converter/test code changed. Runtime execution, MIDI regeneration, and playback were not performed.


## 2026-10-24 — SNG2S option token cross-reference

- Decoded the original executable's initialized data image and confirmed `DS:0x0F05` contains the two-character token `XD\0` (file offset `0x7375`). The parser compares the two-character option token against it and sets `0x622 = 1` on a match; this confirms `/XD → 0x622` (FACTS F-096).
- The help text documents `/XD` as disabling Exclusive/SysEx conversion and documents `/X1` / `/X2` placement choices. This does not establish which options were used to create the saved reference MIDI files. The `/F` token-to-`0x626` mapping remains unconfirmed.
- Updated H-015, marked the parser-token check complete in T-017, and explicitly retained the reference invocation and v1 producer-to-payload mapping as open work. Reframed the older H-016 note as superseded history so it no longer contradicts F-094/F-095.
- No converter/test code changed; no runtime invocation, MIDI regeneration, build, test, or playback was performed. Private executable, fixtures, and analysis export remain private.


## 2026-10-24 — v1 setup SysEx mapping, first pair

- Compared the original executable's `FUN_1000_1CE4` format templates `0xF34` / `0xF3F` with the saved v1 reference MIDI event signatures.
- The two 12-byte setup SysEx events at ticks 0 and 4 match the formatted template outputs, checksum, and terminator in both fixtures: `runner.MID` uses arguments `0,5,6`; `TEST.MID` uses `0,5,5` (FACTS F-097). Private SysEx payload bytes are not included in the public docs.
- This closes the first producer pair only. The separate 10-byte events at ticks 8 and 8 remain to be mapped. No converter/test code changed; no runtime execution, MIDI regeneration, build, test, or playback was performed.


## 2026-10-24 — SNG2S.DOC option semantics reviewed

- Read the private SNG2S Ver 3.3 manual at `testdata/ballade/sng2gs/SNG2S.DOC`. It documents `/1` as the Format 1 default, `/X2` as the default dedicated SysEx track after the conductor track, `/X1` as conductor-track placement, `/XD` as no SysEx generation, and `/F` as ascending MIDI track order for SNG tracks (FACTS F-098).
- Because both saved reference MIDIs contain SysEx events, the effective conversion path did not suppress SysEx via `/XD`. The manual does not contain the actual historical invocation; explicit versus omitted default switches cannot be distinguished from that documentation alone.
- Updated H-015 and the comparison/next-step documents to separate manual-defined behavior from historical invocation evidence. Next, compare reference MIDI track ordering against the manual's `/F` and non-`/F` order tables. No converter/test code changed; no runtime conversion or playback was performed.


## 2026-10-24 — SNG2S README.DOC の版履歴を反映

- private `testdata/ballade/sng2gs/README.DOC` を確認し、SNG2S Ver 3.3 の版履歴を FACTS F-099 と比較メモに追加。
- README に明記された Ver 2.0 からの変更（Ballade 3 対応、タイトル/トラック名の meta-event `FF 03` 化、`/DEF` と `/MT` の追加）、Ver 3.2 → 3.3 の `/N` 使用時に Ballade 3 の 11 トラック目以降が欠落する不具合修正、Ver 3.0〜3.1 の使用警告を記録した。
- `/DEF` と `/MT` の具体的な仕様、保存済み reference MIDI の実際の起動オプション、個別 fixture の runtime behavior は README からは確定できないと明記。詳細オプションは `SNG2S.DOC` を参照するという README の案内も保持。
- private README の本文や private fixture / payload bytes は public repository に複製していない。converter/test code の変更、build、test、runtime conversion、playback は行っていない。


## 2026-10-24 — Reference MIDI の生成コマンドを確認

- 利用者から、`runner.MID` / `TEST.MID` とその他の `sng2gs` 生成物は `sng2gs.exe runner.sng`、`sng2gs.exe TEST.sng` のようにオプションなしで生成したと確認。
- このため、保存済み MIDI はデフォルト動作の reference として扱う。`/F`、`/N`、`/X1` などの明示指定を仮定しない旨を FACTS F-100、比較文書 section 18、NEXT_STEPS に反映。
- MIDI の構造確認は既存 reference の静的解析。converter/test code は変更しておらず、再変換・再生・build・test は実施していない。


## 2026-10-25 — v1 tick-8 SysEx payload を静的照合

- 元の SNG2S.EXE の CS:0x3F82 initialized bytes が `F0 41 10 16 12 FF` であることを確認。終端 FF を除いた template と body の組み合わせが、SMF SysEx event data の形式に合う。
- 0x7DE の initialized word 0x004B と、runner.MID / TEST.MID の tick 8 SysEx 2件を比較。A `41 10 16 12 10 00 16 4B 0F F7`、B `41 10 16 12 52 00 10 4B 53 F7` が静的生成式と byte-for-byte 一致した。
- FACTS F-101、比較文書 section 19、NEXT_STEPS に結果を記録。runtime trace、再生成、実再生、converter/test code の変更は行っていない。

## 2026-10-25 — v1 tick-8 SysEx payloads completed and T-017 status reconciled

- Updated the current T-017 status to reflect the original EXE initialized bytes, static producer paths, and byte-for-byte comparison of both 10-byte SysEx payloads at tick 8 in `runner.MID` and `TEST.MID` (FACTS F-101).
- The earlier tick-8 payload uncertainty is closed for these saved references. The remaining boundary is runtime state / playback, indirect or aliased writes, and generalization to every v1 file; no converter or test changes were made.
- Reconciled the top-level status in `NEXT_STEPS.md`, the current status and next steps in `V1_TEMPO_INVESTIGATION.md`, and the SysEx subsection in `BALLADE_LAYOUT_COMPARISON_2026-10-21.md`. Older dated notes remain as historical records and are superseded by the current findings.

## 2026-10-25 — v1 default track order compared with reference MIDI

- Compared the confirmed no-switch generation procedure with the saved `runner.MID` physical chunk order and the SNG2S v3.3 manual. The reference's part track order is 10→1, matching the documented v1–v3 default; the empty part-1/Keyboard track is retained.
- Documented the separate current-Glosa layout gap: its writer emits one conductor track followed by parts in ascending order and does not emit the reference's dedicated `Exclusive` track. Channel-to-part mapping (`channel = part - 1`) is a separate concern.
- `/F` runtime behavior remains untested because the executable is unavailable. No converter or test code was changed; track-layout implementation remains gated on resolving track roles and cross-version regression coverage.

## 2026-10-25 — reconcile Exclusive-track and reference-generation records

- Updated FACTS F-057 to remove stale wording that the four SysEx producer paths were unresolved: tick 0/4 are covered by F-097 and tick 8/8 by F-101. Kept converter-side generation as an implementation gap.
- Updated NEXT_STEPS T-002 / T-003 / T-017 to reflect the user-confirmed no-switch reference command, documented `/X2` default, completed static payload mapping, and the remaining distinction between static parser mapping and runtime flag state.
- No converter or test code changed.


## 2026-10-25 — SngConverter の先頭 track writer を再確認

- 作業ブランチの `src/Glosa.Core/Smf/SngConverter.cs` を再取得し、`ToSmf` の SMF writer を確認。最初の track body が time-signature meta (`FF 58`) と tempo meta (`FF 51`) を出力し、その後に part 1→N の track を出力することを確認した。
- 確認した writer 経路には、reference track 0 のタイトル文字列 meta-event、または reference track 1 の独立 `Exclusive` track を生成する処理がない。これは現行ソースの出力経路に関する確認であり、他のコード経路がないことまで一般化しない。
- FACTS F-054 と NEXT_STEPS T-006 の既存記述を更新。物理 track layout の互換実装は引き続き保留し、v2/v3/v4 の reference layout と回帰範囲の確認を先行する。converter / test code は変更していない。


## 2026-10-25 — reverse README / hypothesis status synchronization

- Audited current summary statements against FACTS F-057, F-097, and F-101. Updated `README.md` so the v1 tick-8 / tick-8 setup SysEx pair is no longer described as unmapped; it now records the static producer path and byte-level match, while retaining the runtime-trace/playback and Glosa implementation limits.
- Added a dated follow-up under H-016 to mark the tick-8 setup SysEx producer mapping complete for the inspected reference files. Earlier text remains as historical context; the follow-up explicitly supersedes that item.
- No converter or test code changed. No claim of current-HEAD CI success is made from this documentation-only update.


## 2026-10-25 — PR #9 mergeability and cross-version track-order audit

- Rechecked PR #9 at head `a95d600ae3f2bafb378906f488db64fa5b9a3b7e`: it remains open and unmerged. GitHub currently reports `mergeable=true`; the earlier `false` observation is not current.
- The base branch is two commits ahead of the PR's merge base because of commit `d7ee60f` and merge commit `6e99e68` (PR #10, stop-silence timing test tolerance). These changes affect `tests/Glosa.Tests/SequencerTests.cs`, not the SNG converter/docs paths listed in PR #9. No merge or rebase was performed.
- The PR head's associated CI workflow run #1076 completed successfully. Combined commit status returned no individual status entries; this should not be reported as separate required checks passing.
- Re-read the SNG2S v3.3 manual: default output is Format 1; `/X2` is the default and creates a dedicated Exclusive track immediately after the conductor track. Without `/F`, non-Ballade-3 files use part order 10→1; Ballade 3 uses 10, 16→11, 9→1. The user-confirmed reference generation command has no switches, so these documented defaults are the appropriate comparison baseline.
- This documents expected ordering from the manual, not a new binary inspection of every v2/v3/v4 reference MIDI. The existing v4 saved-MIDI observation (18 reference tracks vs 17 saved Glosa tracks) is consistent with one missing dedicated Exclusive track, but the cause of tempo-event count/value differences remains unresolved.
- No converter or test code changed.


## 2026-10-25 — v2/v3/v4 saved MIDI の物理 track / tempo audit

- private testdata にある v2 SHIZUME0、v3 START__0、v4 BOURREE0 の reference MIDI と保存済み Glosa MIDI を SMF chunk/event 単位で解析し、比較文書 §20 に track 数、tempo event 数、初期 tempo event、独立 SysEx track、part order を記録。
- 3版とも reference は Glosa 保存出力より物理 track が1本多く、reference の track index 1 に SysEx を持つ独立 track がある。reference の part order は SNG2S v3.3 manual のオプションなし default と整合し、保存済み Glosa は part 昇順。
- tempo event の差も記録したが、比較対象の Glosa MIDI は保存済み旧生成物であり、現行 converter の挙動とは断定していない。
- 現行 PR head `ab381d4b82865312d973377c860616d1bc4a7e6f` の CI run #1080 は成功完了を確認。
- private fixture workflow の YAML を確認。workflow_dispatch の `glosa_ref` で branch/tag/commit を指定して再変換・テストできる設計だが、この環境から workflow dispatch を起動する GitHub action は利用できなかった。現行コードからの再生成比較は未実施。
- converter/test code は変更していない。次は private fixture workflow を現行 PR head で実行し、再生成 MIDI の track layout / tempo event を比較する。


## 2026-10-25 — v2〜v4 tempo record と reference MIDI の照合

- private SNG の tempo sentinel / raw records / cumulative positions を読み、対応する SNG2S reference MIDI の conductor tempo events と比較した。
- v3 value 89 → 166 BPM の値 361000 / 361445、v4 value 123 → 230 BPM の値 260000 / 260869 が、coarse whole-ms conversion と exact integer µs/qn conversion に一致することを確認。
- 後続 coarse events の複数例は cumulative record boundary の1 tick前に出るが、初回 coarse event（v3 tick 92 / v4 tick 116）はこの則では説明できない。v2 の reference tick 128 = 1000000 µs/qn も後続版の slider formula からは説明できない。
- FACTS F-105、NEXT_STEPS T-015、README の結論を更新。raw value mapping と scheduler tick rule の未解決部分を明確に分けた。
- Converter / tests は変更していない。Private workflow / local tests は実行していない。静的な保存済みファイル比較であり、runtime trace / playback は未実施。


## 2026-10-25 — SNG2S の coarse / exact tempo writer を静的追跡

- private sng2gs解析データ.txt の FUN_1000_3406 を追跡し、tempo record の step-1 countdown、value * baseTempo / 64 による BPM 算出、1〜480 BPM の clamp、BPM 変更時の FF 51 03 出力を確認。
- 同関数の coarse writer は 60000 / BPM の整数除算後に 1000 を掛けるため、v3 166 BPM → 361000、v4 230 BPM → 260000 µs/qn を説明する。
- 別の FUN_1000_0010 path は FUN_1000_618a(0x8700, 0x393, BPM, 0) を呼び、60,000,000 / BPM の整数除算を行うため exact 値 361445 / 260869 を説明する。F-106 に根拠と制約を記録し、T-015 は tick accumulator / call order の調査に絞った。
- まだ初回 coarse event tick 92 / 116 と後続 boundary-minus-one を完全には説明できていない。converter / tests は変更せず、private workflow も実行していない。静的逆解析であり runtime trace / playback は未実施。


## 2026-10-25 — PR #9 merged; main CI #1123 passed

- Confirmed PR #9「fix: add Ballade SNG v1 legacy layout parsing」is merged.
- Merge commit: `5571b4d059620ba300971d7fd1444013532232a9`.
- PR checks: CI run #1122 passed; the user verified the three successful matrix checks for macOS, Ubuntu, and Windows.
- Post-merge main CI run #1123 completed successfully on the merge commit.
- Updated FACTS F-107, the reverse README, and NEXT_STEPS to remove stale claims that PR #9 remained open and to distinguish CI success from runtime playback or v2–v4 tempo equivalence.
- No converter or test code was changed in this documentation pass.


## 2026-10-25 — SNG2S tempo-event delta writer path separated

- Re-read the private SNG2S export around `FUN_1000_3406`, `FUN_1000_08EC`, `FUN_1000_16E6`, and `FUN_1000_17C2`.
- Confirmed that the tempo stream consumer queues tempo-event bytes, while `FUN_1000_08EC` flushes pending delta before writing an event and accumulates the absolute tick; `FUN_1000_16E6` encodes the delta as SMF VLQ and `FUN_1000_17C2` only appends a byte / increments buffer byte count.
- Recorded the bounded static result as FACTS F-108 and narrowed T-015. The initial coarse tempo ticks 92 / 116 and later boundary-minus-one events are still not fully explained; v2 remains separate.
- No converter or test code changed. No runtime trace, MIDI regeneration, or playback was performed.


## 2026-10-26 — tempo scheduler initialization path follow-up

- Continued the static call-order review around `FUN_1000_39C9`, `FUN_1000_33D4`, and `FUN_1000_0010` in the private SNG2S Ver. 3.3 analysis export.
- `FUN_1000_39C9` appends one byte to the internal circular queue at `0x1152`, advances the write pointer, increments the queue count at `0x1150`, and wraps at `0x1556` to `0x1156`; it does not itself update MIDI tick or pending delta.
- `FUN_1000_33D4` initializes scheduler state, calls `FUN_1000_3406` once before entering the repeated processing path, and then calls `FUN_1000_3B68`. In the inspected setup path, `FUN_1000_0010` later drains the internal event stream and computes the initial exact tempo value through `FUN_1000_618A` when the base tempo differs from 120 BPM.
- The initial call and repeated calls do not follow identical pending-delta increment conditions: the initial nonzero `0xF96` state skips the normal increment path before the state is changed for subsequent processing.
- These observations narrow the call order but do not yet derive the reference coarse tempo event ticks 92 (v3) and 116 (v4), the later boundary-minus-one cases, or v2 tick 128. Keep T-015 open; do not apply a fixed tick correction based on this static trace alone.
- Static analysis only: no runtime trace, MIDI regeneration, playback, converter change, or test change was performed.


## 2026-10-26 — T-015 scheduler ordering narrowed

- Re-read the exact decompiler and instruction sequence for `FUN_1000_3406`, `FUN_1000_3DE2`, and `FUN_1000_33D4` in the private SNG2S Ver. 3.3 export.
- Confirmed normal-call ordering: pending delta `0xF9A:0xF9C` increments before tempo countdown `0x10AA` is processed; when the countdown is already zero, the next 4-byte tempo record is consumed and any changed-tempo bytes are queued during that call.
- Confirmed startup differs: `FUN_1000_3DE2` sets `0xF96=0xFFFF`; `FUN_1000_33D4` performs one scheduler call before setting `0xF96=0`. Added FACTS F-111 and updated the existing T-015 checklist.
- This narrows the state transition but does not yet explain the absolute SMF positions tick 92 / 116 or all later boundary-minus-one cases, because queue production and SMF draining are separate stages. No converter/test code changed; no runtime trace, MIDI regeneration, build, test, or playback was performed.

## 2026-10-26 — SNG2S stream separation and CI concurrency fix verified

- Retrieved the current `docs/reverse/FACTS.md` from `main` and read F-112 in full. Its blob SHA `1cf14aa30971775092b625bb0b4ea22f88fae8e4` matches the reported post-write SHA. F-112 distinguishes the grid/scheduler stream at `0x21DC` from the 4-byte tempo-record stream at `0x21E0`, records the 10/12-byte grid path and relevant state offsets, and explicitly leaves reference coarse-tempo ticks 92 (v3) / 116 (v4) unexplained. No F-112 correction was needed based on this document-level review; the private static-analysis source itself was not independently re-audited.
- Updated the existing T-015 in `NEXT_STEPS.md` to mark stream separation as confirmed and to retain the initial absolute-tick rule, later boundary-minus-one placements, and v2 tempo behavior as unresolved. T-016 was left unchanged because this SNG2S finding does not directly change its MIMPIV4 investigation.
- Verified commit `59210fbbd1fb6acb08be79bef0dd431fc8144949` removes the `concurrency` / `cancel-in-progress` block from `.github/workflows/ci.yml`. GitHub Actions run [#1140](https://github.com/pikachu364/glosa/actions/runs/40238175442) is completed with conclusion `success` for that commit.
- F-112 was introduced by commit `0e2fc982593d13aeb40a6ad8adf4ada634f6fd65`. This entry records the content review and CI evidence; it does not claim that run #1140 tested the later documentation-only changes.
- No converter or test code changed. No runtime trace, MIDI regeneration, playback, or new test run was performed.

## 2026-10-26 — T-017 option flags and remaining static checklist reconciled

- Read the private `SNG2S.EXE` as base64 through the repository connector and inspected the initialized image directly. Its Git blob SHA is `0e7a9d20f0d45eaf3f4c11958a0c74b76ead9ef0`. At file offset `0x7353`, the bytes are `46 00` (`F\\0`); parser function `FUN_1000_184A` compares the one-character token at DS:`0x0EE3` and sets `0x626=1`. This confirms `/F → 0x626=1`, previously listed as unverified.
- Reconfirmed `XD\\0` at DS:`0x0F05` and its parser write `0x622=1` (F-096). Initialized words at DS:`0x0622` / `0x0626` are both zero in the EXE image (file offsets `0x6A92` / `0x6A96`). The user-confirmed reference commands have no options (F-100), so the static path is consistent with both flags remaining at defaults; this is not a runtime trace and indirect runtime writes are not fully excluded.
- Added FACTS F-113 with the binary bytes, parser control flow, initial flag values, and limitations. Updated the current T-017 checklist in `NEXT_STEPS.md`: the `0x6EE` initialized value, option-token mappings, no-option invocation, all four setup SysEx payload mappings, and the static tick-8 pending-delta path are now marked complete with existing evidence references. Runtime trace remains open; converter and regression tests remain unchanged.
- No converter or test code changed. No original-executable runtime trace, MIDI regeneration, playback, or new test run was performed.

## 2026-10-26 — T-017 option downstream effects reviewed

- Checked all visible static uses of `0x626` and `0x622` across the private SNG2S analysis export. `0x626` selects the track-order table / part-count setup; no direct write to tempo stream `0x21E0`, countdown `0x10AA`, or tempo record offset `0x10AC` was identified in the inspected paths. `0x622` affects SysEx and preceding-delta output in `FUN_1000_08EC`, and also resets pending-delta / absolute-tick-related state during initialization.
- Added FACTS F-114 to distinguish these actual downstream effects from a causal explanation for reference ticks 92 / 116. Since the confirmed reference invocation has no options and both initialized flags are zero, the option parser alone does not explain the discrepancy. This does not prove the complete runtime call sequence.
- Updated NEXT_STEPS to focus the next pass on `FUN_1000_0010` startup tempo setup through scheduler calls, queue insertion/dequeue, pending-delta flush, and absolute-tick update.
- CI run #1148 for commit `2f02960db1d9eef03aea2e118fd2269f7bb56ac1` completed successfully: https://github.com/pikachu364/glosa/actions/runs/40241526565
- No converter or test code changed. No runtime trace, MIDI regeneration, playback, or new test run was performed.

## 2026-10-26 — T-017 startup setup / main-loop boundary traced statically

- Continued into `FUN_1000_0010` and `FUN_1000_33D4`. Confirmed the initial scheduler pass occurs before the main `FUN_1000_3406` → `FUN_1000_08EC` loop; the initial exact-tempo setup uses a remainder-based delta calculation, then decrements pending delta before entering the loop.
- Added FACTS F-115 and refined NEXT_STEPS to focus on the accumulator and queue state at that remainder calculation. This is a verified static call order, not a derivation of coarse tempo ticks 92 / 116; the startup values and queue drain state at runtime remain unverified.
- No converter or test code changed. No runtime trace, MIDI regeneration, playback, or new test run was performed.

## 2026-10-26 — T-017 initial scheduler sentinel and tempo-record start

- Confirmed statically that `FUN_1000_3DE2` zeroes tempo countdown/offset and sets `0xF96=0xFFFF`; the first `FUN_1000_3406` call consumes the tempo stream without the normal pending-delta increment, then `FUN_1000_33D4` resets `0xF96` to zero.
- The initial scheduler call can read nonzero tempo records and set `0x10AA=step-1`; `FUN_1000_0010` drains the generated queue only after `FUN_1000_33D4` returns. Added FACTS F-116 to capture this call boundary.
- This narrows the call order but does not derive first coarse tempo absolute ticks 92 / 116 because the fixture-specific raw tempo-record values and runtime state have not yet been established. NEXT_STEPS now prioritizes direct raw SNG/reference MIDI comparison.
- CI #1154 for the preceding documentation commits completed successfully: https://github.com/pikachu364/glosa/actions/runs/40242244244
- No converter/test code changes, runtime trace, MIDI regeneration, or playback.


## 2026-10-26 — v2〜v4 reference tempo meta の直接再読

- GitHub Actions CI #1157 completed successfully: https://github.com/pikachu364/glosa/actions/runs/40243996854
- Private reference MIDI を base64 fetch 後、SMF header、track chunks、VLQ delta、tempo meta `FF 51 03` を直接パースした。v2 SHIZUME0: tick 0=500000 → tick 128=1000000; v3 START__0: tick 0=500000 → tick 92=361000 → tick 192=361445; v4 BOURREE0: tick 0=500000 → tick 116=260000 → tick 192=260869 µs/qn。全て Format 1 / PPQN 48。
- FACTS F-117 と NEXT_STEPS の T-015 に結果を記録。MIDI側の絶対 tick は再確認できたが、SNG raw tempo-record step/countdown と scheduler / delta-drain 状態から初回 tick を導出できたわけではない。特に v2 は値・時刻とも別途未解決。
- Converter、test code、private fixture は変更していない。runtime trace、MIDI 再生成、実再生は行っていない。


## 2026-10-26 — CI #1160 成功後の parser buffer 経路追跡

- GitHub Actions #1158、#1159、#1160 は completed / success を確認。
- `FUN_1000_272A` の static export を再読し、tempo buffer は `local_6c` を使って確保し、grid/scheduler buffer は `local_3a` から別に確保されることを記録した。各 pointer はそれぞれ `0x21E0` と `0x21DC` に設定される。
- FACTS F-118 と NEXT_STEPS の T-015 に追記。重要な境界は、`0x21E0` は raw SNG の絶対 file offset ではなく、parser が確保した buffer pointer という点。
- Raw file bytes を単純な `step,value` パターン検索するだけでは stream の実内容を確定できない。version 別 metadata table と `FUN_1000_40C8` の I/O 引数をさらに解析する必要がある。tick 92 / 116 / 128 の生成則は未解決。
- Converter、test code、private fixture は変更していない。runtime trace、MIDI 再生成、実再生は行っていない。


## 2026-10-26 — SNG2S parser の file-I/O と block 順序を追跡

- CI run #1161、#1162、#1163 は completed / success。
- private static export の parser `FUN_1000_272A` と file-I/O wrapper `FUN_1000_40C8` を確認し、grid/scheduler block の読み込み後に tempo block を読み、相対 seek と別 block の読み込みを挟んで optional block を処理する順序を記録した。
- この調査で、tempo buffer が raw SNG の絶対 offset そのものではなく、ファイル読み込みで埋められる buffer であることを確認。FACTS F-119 と NEXT_STEPS T-015 に反映。
- Version metadata の fixture-specific values は未解決。tempo block の raw absolute offset と初回 coarse tempo tick 92 / 116、v2 tick 128 の生成規則は未確定。
- converter、test code、private fixture は変更していない。runtime trace、MIDI 再生成、実再生は行っていない。


## 2026-10-26 — v2-v4 SNG tempo block raw offsets confirmed

- Combined the static parser read order in FUN_1000_272A with version-specific metadata lengths and direct byte inspection of the private v2/v3/v4 SNG fixtures.
- Confirmed raw tempo-block starts: v2 SHIZUME0.SNG offset 1285, v3 START__0.SNG offset 1893, and v4 BOURREE0.SNG offset 1137. The first records are (step=0, value=64), (step=192, value=89), and (step=24, value=123), respectively.
- Recorded the fixture-specific offsets and leading records in FACTS F-120 and marked the corresponding T-015 metadata/offset checklist item complete. This is static parser analysis plus direct file-byte comparison only.
- Reference MIDI first coarse-tempo ticks (v2 128, v3 92, v4 116) remain unexplained. Raw step values are not treated as MIDI absolute ticks; no converter, test, or fixture files were changed.


## 2026-10-26 — v2〜v4 grid/scheduler 先頭レコードと tempo countdown を直接照合

- CI #1168 成功後、private v2/v3/v4 SNG fixture を base64 で取得し、F-120 の raw tempo-block offsets に加えて grid/scheduler block の先頭 bytes を直接確認した。
- Grid block starts: v2 offset 193, v3 offset 285, v4 offset 309。先頭 word は v2 = 288、v3/v4 = 192。対応する tempo stream の先頭非ゼロ step は v2 = 192（先頭の step 0 record を読み飛ばした次 record）、v3 = 192、v4 = 24。
- `FUN_1000_3406` の静的解析と照合し、grid countdown `0x10A4` と tempo countdown `0x10AA` は別 stream から独立して設定されること、いずれの raw step も MIDI absolute tick と直接一致しないことを記録（FACTS F-121）。
- この比較では v3/v4 の grid interval 192 と exact-tempo tick 192 は整合するが、coarse tempo event tick 92/116 は説明できない。v2 grid interval 288 と coarse event tick 128 も不一致。次は initial scheduler が queue に入れた bytes と最初の `FUN_1000_08EC` drain 時の pending delta / absolute tick に調査を絞る。
- Converter、tests、private fixture は変更していない。runtime trace、MIDI 再生成、実再生は未実施。

## 2026-10-26 — text preamble check

- Checked the early text-entry loop as a possible explanation for the first coarse tempo positions.
- The parser's count byte is zero at file offset 59 in all three v2/v3/v4 fixtures, so that loop is skipped for these files.
- Recorded this exclusion in FACTS F-122. The startup setup-call path remains the next target; tempo ticks 92 / 116 and v2 tick 128 are still unresolved.
- Documentation only; no converter, tests, or fixture files changed.


## 2026-10-26 — setup delta reset paths identified

- Static call-order tracing found versioned setup routines before `FUN_1000_33D4` that drain queued bytes and reset pending delta to `0x6EE = 4`.
- The v2 path `FUN_1000_1CE4` has three such paths; the v3/v4 path `FUN_1000_1E6C` has twelve. `FUN_1000_26A6`, `FUN_1000_265C`, and per-part helpers also use this pattern (FACTS F-123).
- T-015 now focuses on counting fixture-specific setup queue events/drains and checking the 4-tick advances against v3 tick 92 / v4 tick 116. Exact counts and v2 tick 128 remain unresolved.
- Documentation only; no converter, test, or fixture changes. No runtime trace or playback.


## 2026-10-26 — startup setup-drain arithmetic candidate bounded

- Compared the statically identified setup drains with the version-specific per-part loop bounds: 10 iterations for internal v3 state 2 and 16 for internal v4 state 3.
- Under a deliberately simplified assumption of one relevant drain per iteration, the counts are 24 (v3) and 30 (v4) after adding the fixed paths in FUN_1000_26A6, FUN_1000_265C, and FUN_1000_1E6C. These are numerically one 4-tick advance above reference coarse-tempo ticks 92 and 116.
- Recorded this as FACTS F-124 explicitly as a candidate only. Fixture-specific status/option values, branch-dependent actual FUN_1000_08EC drain counts, and the final pending-delta flush boundary still need proof. v2 tick 128 remains separate.
- NEXT_STEPS T-015 now calls for checking the exact branch values and rejecting the simplified model if it fails. No converter, tests, or private fixtures were changed; no runtime trace, MIDI regeneration, or playback was performed.


## 2026-10-26 — startup branch-state trace narrowed

- After PR #14 was merged and main CI #1178 passed, refined the existing T-015 task rather than adding another task.
- The next required evidence is the dataflow that populates per-part status bytes around 0x923 and the branch flags 0x6F8, 0x6FC, 0x620, and 0x62E, followed by a fixture-by-fixture branch table for every FUN_1000_2144 iteration and a count of actual FUN_1000_08EC drains.
- Updated the reverse-analysis README to describe F-124's 24/30 counts strictly as a numerical candidate. The first coarse tempo ticks 92/116 and v2 tick 128 remain unresolved; no converter, tests, or private fixtures were changed.


## 2026-10-26 — v3/v4 startup part-status branch evaluation

- Located the private static-analysis export at `testdata/ballade/sng2gs/sng2gs解析データ.txt` and traced `FUN_1000_272A`'s per-part status-table population plus `FUN_1000_184A`'s command-line option flags.
- The original EXE initializes the status table with a zero sentinel followed by 0xFF entries. For v3/v4, per-part state comes from byte 0x11 of the 0x50-byte part-information buffer; values 0/1/2 are remapped to 2/0/1, while other values are retained. The relevant flags correspond to /MT, /GS, /LA, /SC2, and /SC; the confirmed reference commands use no switches.
- Evaluated every v3/v4 `FUN_1000_2144` loop branch against the inspected fixture data and default flags. The per-part helper drain counts are 17 for v3 and 31 for v4, followed by the separate final drain in `FUN_1000_265C`.
- This rejects F-124's simplified one-drain-per-iteration model as an exact count. It does not explain reference tempo ticks 92/116: the conditional `FUN_1000_1E6C` paths and final pending-delta / absolute-tick boundary remain open. v2 tick 128 remains separate.
- Documentation only. No converter, tests, or private fixtures changed; no runtime trace, MIDI regeneration, or playback was performed.


## 2026-10-26 — fixed v3/v4 setup drains and tick-boundary discrepancy

- Confirmed that `FUN_1000_1E6C` contains twelve sequential drain/reset sequences, not twelve conditional alternatives. `FUN_1000_1C70`'s additional eight-call `FUN_1000_2094` loop is guarded by `0x554 == 1`; the original EXE initializes `0x554` to zero and the available export contains no direct store, so the loop is skipped on the inspected default path, subject to the indirect-write caveat.
- Combining `FUN_1000_26A6` (one drain), the fixture-specific `FUN_1000_2144` loop (17 v3 / 31 v4 drains), `FUN_1000_265C` (one drain), and `FUN_1000_1E6C` (12 drains) yields static setup totals of 31 for v3 and 45 for v4 on the no-switch path.
- Those counts multiplied by four give 124 / 180, whereas the reference first coarse-tempo ticks are 92 / 116. The 32 / 64 tick difference is not yet explained and must not be interpreted as proof that particular drains are ignored. T-015 now targets the pending-delta / absolute-tick / grid-boundary flush accounting.
- Documentation only; no converter, tests, or private fixtures changed. No runtime trace or playback was performed.

## 2026-10-26 — distinguish queue-drain calls from effective tick advances

- Re-read `FUN_1000_0010` and `FUN_1000_08EC` around the first scheduler flush. The first queued scheduler/tempo bytes are flushed after `FUN_1000_33D4`; only afterward does `FUN_1000_61EC` compute the next grid-boundary delta. The latter cannot directly place the already-flushed first coarse-tempo event.
- `FUN_1000_08EC` can process multiple events in one invocation and consumes the pending delta only when emitting an eligible event. Therefore the 31/45 setup call counts are not equivalent to 31/45 tick advances.
- If the first eligible event in the post-scheduler flush consumes pending delta 4, reference ticks 92/116 constrain the pre-flush absolute tick to 88/112. This is an inference from call order and reference event positions, not a runtime trace. T-015 now asks for a per-call queue-output / delta-consumption table.
- No converter, tests, or private fixtures changed; no runtime trace or playback performed.


## 2026-10-26 — setup queue producer helpers classified

- Traced the helper layer beneath the v3/v4 setup routines: byte-append wrappers, Control Change construction, two template-based SysEx construction paths, checksum/terminator helpers, and the template-copy helper.
- Recorded that producer/helper calls do not imply a queue flush or a tick advance. `FUN_1000_08EC` can emit multiple queued events in one call and consumes pending delta only for eligible emitted events (FACTS F-127).
- Added H-020 to keep the 31/45 setup-call counts separate from the observed tempo ticks. The 88/112 pre-flush values remain conditional constraints only, assuming the first eligible event consumes pending delta 4.
- T-015 remains open: the next meaningful result is a fixture-specific queue/event/delta ledger, not another call-count estimate. No converter, tests, or private fixtures were changed; no runtime trace or playback was performed.


## 2026-10-26 — per-part active-state scan traced

- Inspected `FUN_1000_1668` and its visible call site. It scans each part's records in 8-byte increments, treats byte 0 = `0xFC` as that part's terminator, and sets the active-part state at `0x766` when byte +4 is nonzero (FACTS F-128).
- The call site is guarded by `0x628 != 0`; the exact default/reference-run state of this flag and the fixture-specific record values still need confirmation. No claim is made that this alone explains first tempo tick 92 / 116.
- Updated T-015's next action to include active-part eligibility before the per-call queue/delta ledger. No converter code, tests, or private fixtures changed; static analysis only.


## 2026-10-26 — /N flag tied to active-state scan guard

- Traced the `0x628` write in `FUN_1000_184A` to the third one-byte switch comparison in the option parser. The option-help sequence (`/0`, `/1`, then `/N`) identifies it as the no-note-part skip switch.
- The caller invokes `FUN_1000_1668` only when `0x628 != 0`, so the per-part active-state scan from F-128 is guarded by `/N` rather than being unconditional. No-switch reference commands predict this branch is not taken.
- Remaining caveat: verify default initialization and all writes to `0x628`, then trace `0x766` on the default path. The first v3/v4 tempo ticks remain unexplained; static analysis only, no runtime trace/playback.

## 2026-10-27 — default-path writes to 0x766 narrowed

- Searched every textual occurrence of 0x766 in the available private SNG2S Ghidra export. The only direct stores are at 1000:1687 and 1000:16D4, both inside FUN_1000_1668; reads remain in FUN_1000_08EC and the main setup/reporting path.
- Combined this with F-129: the scan is called only when 0x628 != 0 (/N), so the no-switch reference path is expected to skip the only visible direct initializer for the table.
- This narrows the problem but does not reveal the table's initial value. The available export does not by itself prove the original EXE's data-segment load/zero-initialization behavior, and indirect/aliased writes are not excluded. Added F-130 and H-021; updated T-015 to inspect the EXE's MZ/data-segment layout before attempting the event ledger.
- No converter, tests, or private fixtures changed. Static analysis only; no runtime trace or playback.

## 2026-10-27 — executable-byte access limitation recorded

- Confirmed the private repository metadata for SNG2S.EXE (32,383 bytes; SHA-1 0e7a9d20f0d45eaf3f4c11958a0c74b76ead9ef0) is accessible, but the GitHub retrieval connector rejects the binary payload as non-UTF-8.
- Consequently, the DOS MZ header and initialized data image were not inspected in this pass. No initial value for DS:0x766 is inferred.
- Added FACTS F-131 and HYPOTHESES H-022; changed T-015 to request a byte-preserving source or verified local dump before resuming the default-path state analysis.
- No converter, tests, or private fixtures changed. No runtime trace or playback was performed.


## 2026-10-27 — SNG2S default part-state table read from the original EXE image

- Retrieved the private `SNG2S.EXE` through the GitHub connector's Base64 encoding and decoded the bytes without treating the executable as UTF-8 text.
- Confirmed the file size (32,383 bytes), raw-file SHA-1 (`bf0fbeed636762310356a9e571adf758d5f38bf0`), Git blob SHA (`0e7a9d20f0d45eaf3f4c11958a0c74b76ead9ef0`), DOS `MZ` signature, and data-image mapping anchored by the previously verified `DS:0x07BC → file offset 0x6C2C` location.
- The 32 bytes at file offset `0x6BD6`, corresponding to `DS:0x0766..0x0785`, decode as sixteen little-endian words all equal to `0x0001`. Recorded as FACTS F-132; updated H-021 and marked H-022 superseded.
- This is an initialized-image observation, not a runtime trace. The first coarse tempo ticks 92/116 and the separate v2 tick 128 remain unresolved. No converter, test, or private fixture was changed; next work is the fixture-specific queue/event/delta ledger.


## 2026-10-27 — absolute-tick accumulator write sites narrowed

- Searched the complete available SNG2S analysis export for `0x6F2` / `0x6F4`. Visible writes are the startup zero-initialization and the delta-add/carry operations inside `FUN_1000_08EC`; no later direct reset/store appears in the export.
- `FUN_1000_08EC` consumes pending delta only on eligible channel/system-event paths and clears it after adding to the absolute accumulator. This confirms that static helper-call totals are not themselves tick totals.
- Added FACTS F-133 and HYPOTHESES H-023. The per-call queue/event/delta ledger remains necessary to explain v3 tick 92 and v4 tick 116; v2 tick 128 remains separate. No converter, test, or private fixture changed.


## 2026-10-27 — fixed setup SysEx sequence matched to v3/v4 reference ticks

- Parsed the private reference MIDIs directly. v3 track 1 has setup SysEx ticks 4–92 in four-tick steps; v4 has ticks 4–116. The first coarse tempo meta events are at tick 92 / 116 in track 0.
- Matched all twelve sequential `FUN_1000_1E6C` producer/drain pairs to the reference SysEx payloads byte-for-byte: ticks 44–88 for v3 and 68–112 for v4. Matched `FUN_1000_265C`'s `52 00 10 4B` message to tick 40 / 64.
- Added FACTS F-134 and refined H-023/T-015. The remaining task is to identify which per-part-loop drain calls do not consume a delta; static counts are 17 / 31, while the observed tick progression requires nine / fifteen per-part advances. No converter, test, or private fixture changed; no runtime trace or playback was performed.


## 2026-10-27 — first structurally empty per-part drain candidate identified

- Audited `FUN_1000_2144`, `FUN_1000_25B0`, `FUN_1000_2554`, and `FUN_1000_23C0` control flow. `FUN_1000_25B0(1)` reaches `FUN_1000_08EC` without executing either of its own SysEx producer branches; it is therefore a no-tick candidate if the queue is empty on entry.
- Recorded F-135 and H-024 and narrowed T-015 to verify fixture reachability/queue state and enumerate the remaining branch-gated drains. This does not yet reconcile all 17 / 31 loop calls with nine / fifteen per-part tick advances.
- The exported text has reads of state bytes at `0x923 + part`, but no direct immediate-address store to `0x923` was found in the searched export. Do not interpret that as proof that no indirect/aliased write exists. No converter, tests, or private fixtures changed; no runtime trace or playback was performed.


## 2026-10-27 — reference part-loop messages mapped to FUN_1000_25B0

- Compared the first setup SysEx events against the static producer templates in `FUN_1000_25B0`. v3 ticks 4–36 match part arguments 2–10 using template `0x10`; v4 ticks 4–36 match parts 2–10 and ticks 40–60 match parts 11–16 using template `0x52`.
- Both sequences imply a leading part-1 helper call that emits no setup record, consistent with a no-tick drain if the queue is empty. This is a static/template match, not runtime confirmation.
- Added F-136/H-025 and refined T-015 to separate static call-site counts from actual tick advances. Remaining conditional paths and queue-entry state are still unresolved. No converter, tests, or private fixtures changed.


## 2026-10-27 — part-1 no-output drain confirmed from fixture state byte

- Re-read `FUN_1000_272A`, `FUN_1000_2144`, `FUN_1000_25B0`, `FUN_1000_26A6`, and `FUN_1000_08EC` in the private SNG2S analysis export.
- Derived the first part-information buffer offsets from the documented block lengths and parser seek/read sequence: v3 offset 3861 and v4 offset 2585. Both buffer byte `+0x11` values are `0x04` (raw offsets 3878 / 2602), so the loader retains part-1 state 4.
- With no-switch flags, that state routes the part-1 iteration through `FUN_1000_25B0(1)`. The helper skips both SysEx producer conditions but still calls the queue drain and resets pending delta to 4. The immediately preceding `FUN_1000_26A6` drains the queue, and no producer intervenes before the part loop; the empty-queue interpretation is therefore statically supported.
- Added FACTS F-137 and HYPOTHESES H-025; refined H-024 and T-015. The remaining v3/v4 per-call queue/event/delta ledger is still open. No converter, tests, or private fixtures changed; no runtime trace or playback performed.


## 2026-10-27 — corrected v3/v4 per-part drain counts and completed static tick ledger

- Parsed every v3/v4 per-part information record by following the loader's 0x50-byte reads, variable-length blocks, and nineteen relative seeks per part. v3 states are 4 for parts 1–9 and 5 for part 10; v4 states are 4 for parts 1–9, 5 for part 10, and 4 for parts 11–16.
- Re-evaluated `FUN_1000_2144` using those actual state bytes and no-switch flags. Each iteration reaches one `FUN_1000_25B0(part)` call, so the per-part drain counts are 10 / 16, not the earlier 17 / 31 recorded in F-125. The `FUN_1000_2554` and `FUN_1000_22A4` alternatives are not reached for these fixture states/default flags.
- The corrected ledger accounts for reference timing: part 1 calls the helper but emits no SysEx and does not advance tick; parts 2–10 / 2–16 advance to 36 / 60; `FUN_1000_265C` advances to 40 / 64; twelve `FUN_1000_1E6C` messages advance to 88 / 112; the first eligible event in the initial scheduler/tempo flush consumes pending delta 4 and reaches 92 / 116.
- Updated F-124/F-125, added F-138, revised H-023/H-024 and T-015. This supersedes the prior 17 / 31 and 31 / 45 call-count estimate. No converter, tests, or private fixtures changed; no runtime trace or playback performed.


## 2026-10-27 — v2 first changed-tempo tick explained by scheduler boundary

- Rechecked the v2 grid/scheduler bytes at raw offset 193: leading bytes are `20 01 06 04...`. Although the first word is little-endian `0x0120` (288), `FUN_1000_0010` passes the first byte `0x20` as `uVar11` to `FUN_1000_61EC`; the interval for this boundary calculation is therefore 32 ticks.
- The caller computes the next-boundary delta as interval minus current tick modulo interval (or one full interval when already aligned). With the inspected v2 setup tick at 104, `104 mod 32 = 8`; delta 24 reaches tick 128, matching the saved reference MIDI's first changed-tempo event.
- Confirmed the versioned setup call chain: v2 skips `FUN_1000_26A6`, executes `FUN_1000_2144`, then `FUN_1000_1C70` → `FUN_1000_1CE4`; v3/v4 use the distinct `FUN_1000_1E6C` path. Added F-139, refined H-023 and T-015. Static evidence only; no runtime trace/playback, converter changes, tests, or private fixture changes.


## 2026-10-27 — v2 base-tempo byte confirmed; designated SNG2GS source aligned

- After PR #30 merged, rechecked the v2 tempo finding against the preferred private analysis source `testdata/ballade/sng2gs/sng2gs_mimpiv4解析java使用.txt`. The file contains the static-export functions `FUN_1000_272a` and `FUN_1000_3406` used for the base-tempo source and coarse-tempo calculation.
- Raw `SHIZUME0.SNG` inspection confirms byte `0x79 = 0x3C` (60); `0x78` and `0x7A` are zero. For tempo record value 64, the static formula gives 60 BPM and then 1,000,000 µs/qn, matching the saved reference MIDI tempo at tick 128.
- Updated F-140's source citation and marked the corresponding T-015 byte-verification task complete. The tick-128 scheduler-boundary explanation remains separate (F-139).
- Evidence boundary: byte inspection and static control-flow/arithmetic only; no runtime trace/playback and no generalization to all v2 files. No converter, test, or private fixture changes.


## 2026-10-28 — reconcile v2 tempo wording after F-140 verification

- After PR #31 merged, removed stale statements in F-139/T-015 that still said the v2 base-tempo byte or fixture-specific tempo calculation was unverified. F-140 records `SHIZUME0.SNG[0x79] = 0x3C` and the static calculation to 1,000,000 µs/qn.
- Preserved the evidence boundary: tick 128 placement is a static scheduler derivation; the value calculation is raw-byte inspection plus static analysis for this fixture only. Playback behavior is not established.
- No converter code, tests, or private fixtures changed.


## 2026-10-28 — later v3/v4 coarse-tempo record/tick comparison

- Read the v3/v4 raw SNG tempo blocks from the offsets established in F-120 and compared cumulative nonzero record steps/values against the saved reference MIDI tempo events.
- Added F-141 with the fixture-specific mapping: v3 cumulative positions 384/432/480/516/576 correspond to ticks 383/623/671/707/767; v4 positions 252/288/324/360/384 correspond to ticks 407/443/479/515/551.
- The mappings show changing offsets rather than one global step - 1 rule. Updated T-015 to trace the grid countdown, tempo countdown, pending delta, and queue drain together before any converter implementation.
- Static raw-byte/reference-MIDI comparison only. No runtime trace, playback, converter code, tests, or private fixtures changed.


## 2026-10-28 — correct v3 later-tempo event ledger

- Re-read the v3/v4 private raw SNG tempo blocks and parsed the saved reference MIDI tempo events directly after reviewing F-141.
- Found that F-141 had omitted the v3 reference event at tick 575 (983000 µs/qn), corresponding to raw value 33 at cumulative step 432. That omission shifted the later value-to-event matching. Corrected F-141 to include the observed v3 sequence: tick 92/value 89, 383/value 36, 575/value 33, 623/value 27, 671/value 25, 707/value 22, and 767/value 44.
- Recomputed the fixture-specific raw cumulative position → reference tick offsets. v3 later offsets are −1, +143, +143, +155, +131, −1 for cumulative positions 384, 432, 480, 516, 576, 768. The v4 mapping remains 252/288/324/360/384 → 407/443/479/515/551.
- Kept exact-tempo events (v3 tick 192 = 361445; v4 tick 192 = 260869 µs/qn) separate from coarse-tempo records. The underlying countdown/grid/queue interaction is still unresolved; no converter or test changes were made.
- Evidence is raw-byte and saved-MIDI parsing plus static analysis only. No runtime trace, MIDI regeneration, or playback.

## 2026-10-29 — trace grid early-return order before tempo countdown

- Re-read the complete SNG2S static export for `FUN_1000_3406` after PR #34 merged.
- Confirmed the grid/scheduler branch precedes the tempo countdown branch. The grid path can return on its `-1` record sentinel or record-count limit before the `0x10AA` tempo countdown is handled on that invocation.
- Added FACTS F-142 and refined T-015 to record this as a control-flow constraint, not as a proven cause of the later v3/v4 tempo-event offsets.
- The exact per-call state and queue-drain sequence remains unresolved. No converter, tests, raw fixtures, or reference MIDI files changed; no runtime trace, MIDI regeneration, or playback was performed.


## 2026-10-30 — both scheduler early exits converge on caller termination

- Followed the grid `-1` sentinel and record-count-limit returns from `FUN_1000_3406` through the caller loop in `FUN_1000_0010`.
- Although the two branches leave the grid cursor/countdown differently, both reach the same scheduler termination state. After the scheduler call returns, the caller invokes `FUN_1000_08EC` once and then checks that state.
- This is a final drain of bytes already queued, not a resumed scheduler iteration. The caller does not re-enter `FUN_1000_3406`, so the drain cannot process the skipped tempo countdown or read another tempo record. Recorded the corrected caller-level result in F-143 and the distinct final-drain fact in F-144.
- This rules out either terminal branch as an explanation for a one-call pause followed by later tempo processing. It does not explain the later v3/v4 offsets.
- Next: trace normal-call transitions of grid countdown `0x10A4`, tempo countdown `0x10AA`, pending delta `0xF9A:0xF9C`, and queue drain `FUN_1000_08EC` against the fixture event ledger.
- Documentation only. No converter, tests, raw fixtures, or reference MIDIs changed; no runtime trace, MIDI regeneration, or playback was performed.
