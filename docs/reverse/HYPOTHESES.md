# 仮説・未解決事項

仮説は確認済み事実と混ぜず、検証条件を明記します。

## H-001 — v1 は v2 以降と異なるパートブロック構造を持つ可能性
**状態:** v1 の主要レイアウト候補と Note-on/off は参照 MIDI と照合済み。13 seek 値の個別の意味は未確定。SNG2S の実行時トレースではない。

v1 `runner.SNG` のサイズ表を直接走査した結果、次の組み合わせが全10パートで容量・長さ条件を満たす。

- パート領域の開始候補: `tableEnd + 2 + entries[0]`
- パートサイズ候補: `entries[p] + 80`
- SNG2S 情報バッファ開始: part start + 65（buffer+4 に名前らしい文字列）
- ノート領域長 / コントローラ領域長: +115 / +117
- v1 の13個の16-bit値群: +119〜+143（DOS 相対 seek 距離。全10パートで累積後の位置が次パート開始候補+65に一致し、最終パートはEOFに一致）
- ノートレコード列の開始候補: +145

これは、現行 converter の v2〜v4 向け +81 / +112 / +114 / +116 / +158 前提とは異なる v1 レイアウトを支持する。read/seek の累積位置、Note-on 3,295件、静的逆アセンブルから再構成したタイマー規則による Note-off 3,295件が参照 MIDI と一致した。イベント全体（controller、tempo/meta、トラックチャンク順）の一致は未検証。

**残る確認:**
- controller、tempo/meta event と SMF track chunk 順を参照 MIDI と照合する。
- 13個の seek 距離を `0x21DC` / `0x21E0` / `0x21E4` 等の後段参照と対応付け、個々の用途を確定する。
- 可能なら SNG2S の v1 分岐を追加の静的証拠または実行時観測で検証する。

## H-002 — Tie の Note-on 抑制と Note-off 延長規則
**状態:** v2〜v4 の実 C# 変換出力と参照 MIDI の Note-on/off 集計がパート単位で一致することを確認済み。トラック／チャンネル別の対応と同音高重複時の Note-off の厳密な対応付けは未完了。

v2 `SHIZUME0` では非 Tie ノートレコード 2,713 件、v3 `START__0` では 5,362 件がそれぞれ SNG2GS Note-on 数と一致する。Tie レコードを新規 Note-on/off にせず、同じ音高の pending Note-off のうち予定 tick が Tie 開始 tick 以下で最も後のものを Tie の終了 tick まで延長する規則で、参照 MIDI の全 Note-on/off がパート単位で一致することをオフライン再生成で確認した。v2 は参照側に +160 ticks、v3 は補正なしで一致する。v4 は Tie レコードがなく、Note-on/off が一致する。

**残る確認:**
- この照合は SNG2GS/SNG2S の実行ファイルやオプションを特定したものではない。
- トラック／チャンネルごとの比較と同音高重複時の Note-off 対応を検証する。
- v1 は別の +80 / +115 / +117 / +145 レイアウトとタイマー競合規則を持つため、v2〜v4 の Tie 仮説とは別経路で扱う。v1 の Note-on/off 比較は F-044/F-045 で確認済みだが、controller/tempo/meta と track chunk 順は未検証。



補足観測（2026-10-19）: FUN_1000_3406 の静的解析記録では、展開済み内部レコードのフラグ +7 に対する 0xC0 マスクが 0x80 の場合、通常 Note-on 出力を迂回する分岐がある。これは Note-on 抑制と Note-off の期限管理が別経路である可能性を示すが、Tie の所有ノートや Note-off の対応規則は未確定。+7 は展開済み内部バッファのオフセットであり、生 SNG の byte 7 と同一視しない。

## H-003 — 参照 MIDI の開始 tick オフセット
**状態:** オフセット値は観測済み、理由は未解決

SHIZUME0 の参照 MIDI は source tick +128、現行 Glosa は source tick +288 と記録されている。参照 MIDI が +128 を採用する理由、生成元プログラムとオプションは未確認。

**確認に必要なこと:**
- 参照 MIDI の生成元実行ファイル・バージョン・オプションを特定する。
- SNG のコンダクタ／小節長と MIDI 先頭イベントを再照合する。

## H-004 — スケジューラ slot と SNG part の対応
**状態:** 未確定

FUN_1000_3DE2 が選択するテーブル値はチェックイン済み解析テキストから得られていない。FUN_1000_272A は 0x1E54 などのポインタ表を参照するが、静的テーブル内容なしでは内部 slot と SNG part の対応を確定できない。

**確認に必要なこと:** テーブルの静的データを回収し、slot・SNG part・MIDI track を別々に対応付ける。

## H-005 — 0xFA0 項目のイベント意味
**状態:** 構造は確認済み、意味は未確定

登録・照合・期限切れの処理は追跡できているが、項目のキーと3-byte出力を特定の MIDI イベント種別に結び付けるには不十分。

**確認に必要なこと:** FUN_1000_3A10 の呼び出し元から AL/AH/DL を追い、FUN_1000_39C9 の出力先まで追跡する。

## H-006 — SNG2GS と SNG2S の実行ファイル同一性
**状態:** 未確認

逆解析資料には SNG2S.EXE / SNG2S Ver 3.3 とあるが、対象として言及されている SNG2GS と同じ実行ファイルかどうかは確認できていない。名称の類似だけで同一視しない。

## H-007 — JMPlayer 実装と参照 MIDI の Tie 挙動の不一致

**状態:** ソース比較で不一致を確認、参照 MIDI の生成元は未確定

JMPlayer の `sngmidi.cpp` 267–292行と Glosa の現行実装は、Tie bit が立っている場合でも「直前のノートと音高が同じ」「前ノートの Note-off tick と現在の開始 tick が一致」の両条件を満たす場合にだけ Note-off を延長する。それ以外は新しい Note-on/Note-off を出す。一方、SHIZUME0 の参照 MIDI の Note-on は Tie bit が立っていない SNG レコードと一致する。

**含意:** 参照 MIDI がこの JMPlayer ロジックで生成されたと仮定するのは不適切。参照 MIDI の生成元実行ファイル・バージョン・オプションを確認するか、SNG2S/SNG2GS 側の Tie 処理を解析する必要がある。

## H-008 — v1 の +80 レイアウト候補と JMPlayer 実装の不一致

**状態:** SNG2S の静的逆解析、実データ境界、参照 MIDI の Note-on/off 全件照合は主要レイアウトを支持。+119〜+143 は13個の16-bit相対 seek 値で、個別の用途は未確定。末尾65 byteは次の情報バッファ位置として説明済み。

JMPlayer の `buildLayout`（`sngmidi.cpp` 96–147行）は全バージョンに同じ3つの header 候補、パートサイズ `entry+81`、長さフィールド `+112/+114/+116` を適用し、v1 専用分岐を持たない。runner.SNG では +80 パートサイズ候補と、SNG2S が支持する +115/+117 の長さフィールド、+145 record start が観測される。

**残る確認:** FACTS F-046〜F-050 の後段バッファ解析を踏まえ、13個の seek 値と個々の読み込み先の対応を調べる。末尾65 byteと Note-off は F-040 / F-044 で説明済みだが、静的解析であり実行時トレースではない。
## H-009 — v1 layout は現行 converter の候補探索では認識されない
**状態:** 当初の失敗は作業ブランチ上の v1 専用変換経路と参照 MIDI 比較テストで解消。残るのは Note-on/off 以外のイベントとレイアウト細部。

v1 `runner.SNG` は、作業ブランチ `docs/ballade-version-testdata` の専用経路で変換され、参照 `runner.MID` に対する Note-on 3,295件・Note-off 3,295件の比較が一致する。同 tick の重複ノート順もテストで固定し、GitHub Actions run 39253494378 は Ubuntu/macOS/Windows で成功した。

**残る確認:** controller、tempo/meta event、SMF track chunk 順の比較。変換ソースの変更は作業ブランチ上であり、PR #2 の main への取り込み状態とは区別する。


## H-010 — v1 Note-off は単純な onset + duration だけでは説明できない

**状態:** F-044 により旧仮説として superseded。単純な onset + duration では一致しない点は正しいが、タイマー競合モデルで参照 MIDI 全件との一致を確認済み。

v1 part 10 には Tie flag のない pitch 69 / velocity 21 / duration 1 のレコードが2件あり、Note-on は同 tick 1728 に発生する。参照 MIDI の対応する Note-off は tick 1728 と 1729 に分かれるため、両方を `onset + duration` に置く単純モデルでは一致しない。同様のパターンは後続の12-tick間隔でも観測される。

**解決記録:** `FUN_1000_3406` / `FUN_1000_3A3F` / `FUN_1000_3A10` の静的命令列からタイマー競合モデルを組み立て、参照 MIDI の Note-off 3,295件と差分0を確認した。v1 専用 converter 経路は work branch に実装済みで、CI run 39253494378 は Ubuntu/macOS/Windows で成功済み。


## H-011 — SNG2S は同じ識別値の Note-off タイマー登録を統合／競合処理する可能性

**状態:** オフラインのイベント照合で強く支持。SNG2S 実行時トレースではない。

`FUN_1000_3A3F` はタイマー表 `0xFA0` から入力 `AX` と一致する識別値を検索し、既存カウンタと入力 `DL` を比較する。`FUN_1000_3406` のノート処理は戻り値に応じて `FUN_1000_3A10` による新規登録を省略する場合がある。これにより同じチャンネル／音高の重複ノートの Note-off は、各レコードの `onset + duration` だけでは決まらない可能性がある。

v1 part 10 の同 tick・同音高ノートで Note-off が tick 1728 と 1729 に分かれる例を含め、全10パートの Note-off 3,295件がこのモデルで参照 MIDI と一致した。実行時トレースではないため、逆アセンブルの意味付けは引き続き静的根拠として扱う。


**追加の静的根拠:** `FUN_1000_3B2F` が `0x90` の上位 nibble にチャンネル表を OR し、その値と pitch が `FUN_1000_3A10` に保存され、`FUN_1000_3A3F` のキー比較に使われる。したがって仮説の対象は同じチャンネル・同じ音高のタイマー競合であり、velocity はキーに含まれない。


## H-011 更新 — v1 タイマー競合規則のオフライン検証結果

`FUN_1000_3406` の呼び出し元分岐と `FUN_1000_3A3F` / `FUN_1000_3A10` をモデル化したところ、v1 参照 MIDI の Note-off 3,295件がすべて `(channel, pitch, tick)` で一致した。キー衝突時の即時 Note-off、duration 比較、`0x40` の新規登録抑止、`0xC0` の即時 Note-off 抑止、期限切れイベントを含めたモデルで差分は0。

これは静的逆アセンブルからのオフライン再現による強い裏付けだが、実行時トレースではない。converter への移植時には v2/v3 の Tie 処理と既存テストを維持する必要がある。


## H-012 — v1 の tempo meta は後続版と異なる変換経路を持つ可能性

**状態:** v1 参照 MIDI と汎用 `ReadTempi` の出力候補が不一致。v1 専用 tempo 規則は未確定。

v1 `runner.MID` は tick 0 / 8 / 192 にそれぞれ 500,000 / 869,000 / 869,565 µs/quarter の tempo meta を持つ。一方、v1 conductor 領域に対する現行 sentinel parser は step/value = 37/144, 37/192 ... と読むため、現行の後続版向け式では参照と一致しない。SNG2S の起動時 tempo 出力と `0x7BA` を参照する経路は逆アセンブル上確認できるが、参照 MIDI の各 tempo event と SNG 内フィールドの対応は未確定。

**追加で確認できたこと:** `FUN_1000_3406` は内部 far-pointer `0x21DC` のレコード列から `FF 58` time-signature meta を、`0x21E0` の列から `FF 51` tempo meta を出力する。別の `0x21E4` 列も読み込んで `0x7DE` を更新し `FUN_1000_3B78` を呼ぶが、そのイベント上の意味はまだ未確定（FACTS F-055）。これらは内部バッファポインタであり、SNG ファイルの絶対オフセットではない。

**確認に必要なこと:** `0x21DC` / `0x21E0` / `0x21E4` の各バッファを SNG 読込・変換側の充填元まで逆引きし、特に参照 MIDI の tick 8 / 192 の tempo event と `0x7BA` の設定元を対応付ける。根拠がそろうまでは v1 tempo conversion を推測で変更しない。

**実装上の暫定対処（2026-10-20）:** 作業ブランチでは v1 に汎用 `ReadTempi` を適用しないようにした。これは誤った 270/360 BPM 候補の出力を避けるガードであり、参照 MIDI の tick 8 / 192 の tempo change を再現するものではない。F-056 を参照。


## H-013 — v1 の Exclusive SysEx は SNG にそのまま格納されていない可能性

**状態:** 仮説。元データの literal byte search と参照 MIDI の比較のみで、生成元は未確定。

- 根拠: v1 の参照 MIDI track 1 に Roland-style の SysEx 4件がある（FACTS F-057）。元の `runner.SNG` 全体（85,897 bytes）には、各 payload の完全一致も共通先頭列 `41 10 16 12` も存在しない。
- 解釈候補: SNG2S が設定値から SysEx を生成している、または元 SNG の別フィールドを変換している。現時点ではどちらとも断定しない。
- 次の確認: SNG2S マニュアルの /X1 /X2 /XD の意味を解析資料と照合し、SysEx を出力する関数・分岐と、SNG 読込時に作られる関連バッファを追跡する。4件の payload を converter に直接ハードコードしない。


## H-014 — v1 tempo event は record countdown と 192-tick grid の複数経路でスケジュールされる

**状態:** 部分的に裏付けられた仮説。正確な出力 tick は未確定。

- 根拠: 初期 500,000 µs/quarter は tempo record 初期化より前に明示出力される。別途 FUN_1000_3406 が 0x21E0 の 4-byte tempo records を countdown で処理し、entry path は 0x21DC の先頭 byte 192 を使って次の 192-tick 境界までの差分を計算する。
- したがって reference MIDI の tempo meta を一つの単純な record-to-tick 対応だけで説明できるとは限らない。queue drain / pending delta-time flush の順序を含めて検証する必要がある。
- 未解決: tick 8 の 869,000 µs/quarter の生成元、最初の tempo record の出力 tick、tick 192 の 869,565 µs/quarter と grid 経路の因果関係。
- 次の確認: FUN_1000_33D4 → FUN_1000_08EC → grid delta → FUN_1000_618A の順序と、0xF9A:0xF9C / 0x6F2:0x6F4 の値を各イベント出力点で追う。実行時トレースがない間は converter を変更しない。


### H-012 follow-up — arithmetic paths identified, tick-8 runtime point remains open

Static analysis distinguishes the two tempo values: the tempo-record path computes (60000 / 69) * 1000 = 869000, while the 192-grid path computes 60000000 / 69 = 869565. These match the reference values at ticks 8 and 192 respectively. The arithmetic is explained, but the tick-8 assignment is still a strong static mapping rather than runtime proof: the pending delta and absolute-tick state at the first queue drain have not been traced dynamically. Keep the converter guard until that timing is confirmed.

### H-014 follow-up — first scheduler call is an exception path (2026-10-21)

追加で静的追跡した結果、`FUN_1000_33D4` 内の初回 `FUN_1000_3406` は通常の pending-delta `+1` 経路ではない。`FUN_1000_3DE2` が `0xF96 = 0xFFFF` を設定し、`FUN_1000_33D4` は `FUN_1000_3406` の後で初めて `0xF96 = 0` に戻すためである。

v1 の `FUN_1000_1CE4` は条件付きで queue drain の前後に `0xF9A:0xF9C = 0x6EE:0` を設定する。続く初回 tempo queue drain 前には、呼び出し元が `0x622 != 0` の場合に pending delta をゼロ化する分岐もある。従って tick 8 を確定するには、`0x6EE` の実値・由来、参照 MIDI を生成した条件下の `0x622`、`0x6F2:0x6F4` の preamble 後の値、queue 内の先行 event を合わせて追う必要がある。

これは tick-8 の既存仮説を強める/確定する証拠ではなく、通常 scheduler loop の `+1` を初回呼び出しに誤適用しないための制約である。runtime trace がなく、`0x6EE` の値も確定していないため、tick 8 は未確認のまま扱い、converter guard を維持する。


## H-015 — SNG2S option flags and reference invocation

**Status:** `/XD → 0x622 = 1` is confirmed from the initialized data image and parser branch (FACTS F-096). The actual command line used to create the saved reference MIDIs remains unknown; `/F → 0x626` is still a best-fit interpretation rather than a direct token-to-flag proof.

- `FUN_1000_184A` compares a two-character option token at operand `0xF05`. The original executable's initialized data image confirms `DS:0x0F05 = "XD\0"`; a match writes `1` to `0x622` (FACTS F-096).
- Downstream use agrees with the documented meaning of `/XD`: `0x622` suppresses SysEx payload output in `FUN_1000_08EC). The executable's help text documents `/XD` as disabling Exclusive/SysEx conversion.
- `0x626` selects an alternate track-order table in `FUN_1000_0010`, which fits the documented `/F` option, but this finding does not directly prove the `/F` token-to-flag mapping.
- The saved reference MIDIs contain SysEx events, so `/XD` was probably not active for those runs. This is an inference from output, not proof of the actual command line or runtime flag state.
- `0x6EE` is now confirmed as initialized value 4 on disk, and the static startup call-order explains the coarse tempo event at tick 8 for the inspected runner path (FACTS F-094/F-095). Runtime trace and generalization remain unverified.

**Next verification:** determine the actual SNG2S invocation if recoverable, directly verify the `/F` token-to-flag mapping, and finish the v1 setup-SysEx producer-to-payload comparison. Do not infer actual flags from documented defaults alone.

### H-015 correction note (2026-10-24)

The old `0x622 ↔ /F` mapping is withdrawn. The `/XD → 0x622 = 1` token mapping is directly confirmed by FACTS F-096; the actual reference invocation is unknown. The `0x626 ↔ /F` mapping remains a downstream-use hypothesis until its parser token is directly traced.

### H-016 — 0x6EE は複数の出力経路で使う delta/間隔値の可能性

**状態:** 初期化済み image 上の数値は 4 と確認。setup queue drain と tick-8 tempo 出力をつなぐ静的 call-order/dataflow が確認された。runtime trace と全 v1 ファイルへの一般化は未実施。

- 完全な 18,795 行の SNG2S 解析 export を再走査したところ、`0x6EE` はテキスト上 58 箇所に現れる。これは decompiler 表現と命令行表現の両方を含む。命令行の参照は読み出しのみで、直接書き込みは見つからない。export は初期化済み data segment の実バイトを含まないため、初期値や indirect/aliased write はこの検索だけでは確定できない。
- `FUN_1000_1CE4` の queue drain 後には、読み出した値を `0x1ED8:0x1EDA` に加算し、同値を `0xF9A:0xF9C` にセットする。別の event-output path にも同じ形があるため、単なる tempo 値というより、出力間隔・delta に関係する共通値である可能性がある。
- on-disk 初期値 4 は確認できたが、runtime 上書きや全 v1 ファイルへの一般化は未確認。startup call order による tick-8 の静的説明は FACTS F-095 を参照。

**次の確認:** `0x622` の参照実行時の値と、runtime での indirect/aliased write を確認できる手段があれば検証する。静的 startup path は F-095 に整理済み。


### H-016 follow-up — initialized value and static tick-8 path confirmed (2026-10-24)

**更新状態（2026-10-24）:** 元の EXE の初期化済み data image から `DS:0x06EE = 4` を確認した（FACTS F-094）。さらに startup call order / pending-delta の静的追跡が tick 8 の粗い tempo event を説明する（FACTS F-095）。runtime trace と全ファイルへの一般化は未実施。

参照 `runner.MID` を SMF として直接解析すると、track 1 の SysEx event は tick 0 / 4 / 8 / 8 に出力されている（FACTS F-068）。一方、v1 の `FUN_1000_1CE4` は条件成立時に2回 SysEx-style event を生成して drain し、各 drain 後に `0x6EE` を pending delta `0xF9A:0xF9C` に設定する。`FUN_1000_3F79` の共通 byte 列、`FUN_1000_5FAA` のデータ生成、`FUN_1000_3F54` の checksum 用和計算、`FUN_1000_3EED` の checksum + `F7` 追加から、これらが SysEx 形式であることは静的に支持される。

この2回の drain が track 1 の tick 0 と tick 4 の最初の2件に対応するなら、`0x6EE = 4` と推定でき、2回目の drain 後に残る delta 4 は次の出力を tick 8 に進め得る。これは tick 8 tempo の説明につながる具体的候補だが、まだ以下が未確認である。

- `0xF34` / `0xF3F` から作られる queue payload が、参照 SysEx のどの payload と一致するか。
- queue drain ごとの `0x6F2:0x6F4` と `0x1ED8:0x1EDA` の実際の値。
- 参照実行時の `0x622` が pending delta reset を有効にしたか。

したがって `0x6EE = 4` は on-disk 初期値として確認済みで、startup call order と pending-delta の静的追跡は tick 8 を説明する（FACTS F-095）。ただし runtime trace はなく、全 v1 ファイルへの一般化も未証明。converter の変更は独立した fixture 比較なしに行わない。


追加の対応根拠（2026-10-21）: 参照 MIDI の最初の2件の SysEx は、どちらも payload 12 bytes で共通 prefix を持つ。一方、`FUN_1000_1CE4` の最初の2経路は同じ SysEx-style builder を使い、異なる文字列 offset `0xF34` / `0xF3F` のデータを出力して checksum と `F7` を追加する。event 数・形式・順序・tick 間隔が整合するため、これらが tick 0 / 4 の2件に対応し `0x6EE = 4` となる推定は以前より強い。ただし文字列データ本体が解析資料にないため byte-for-byte の照合は未完了で、確定事実には格上げしない。


### H-015 follow-up — command-line token mapping refined (2026-10-24)

The original executable's initialized data image and parser control flow now directly confirm `DS:0x0F05 = "XD\0"`: the parser compares a two-character option token against this string and writes `1` to `0x622` on a match (FACTS F-096). Thus `/XD → 0x622 = 1` is confirmed; the previous statement that the token-to-string mapping was unconfirmed is superseded. The help text documents `/XD` as disabling Exclusive/SysEx conversion.

Still unknown: whether `/XD` was actually supplied when the saved `runner.MID` / `TEST.MID` files were generated. The `/F` help text is present, and downstream use is consistent with `0x626`, but the `/F` parser token-to-flag mapping and the reference invocation are not established. Do not infer the actual reference flags from documented defaults alone.


### H-016 earlier follow-up — superseded by initialized-image and startup-order evidence (2026-10-24)

The 2026-10-22 note below is retained as investigation history only. Its statements that the initialized value of `0x6EE` was unknown and that the tick-8 path had not been explained are superseded by FACTS F-094/F-095: the original executable's on-disk initialized value is 4, and static startup call-order/dataflow explains the coarse tempo event at tick 8 for the inspected runner path. Runtime trace, indirect writes, and generalization to all v1 files remain unverified. The v1 setup payload-to-event mapping and actual reference command-line invocation remain open.


### H-017 — MIMPIV4 playback tempo controls are independent of the unresolved SNG tempo-record consumer (2026-10-22)

**Status:** separation of evidence; no formula-level conclusion.

- The MIMPIV4 v4.27 manual documents Up/Down keys for changing playback tempo and `/Tn` for fine adjustment. Those are user/runtime tempo controls; their existence does not show how a Ballade SNG tempo-record value is decoded.
- Static tracing confirms a legacy header/base-tempo byte is copied to `0x7BF0` and `0x0440`, and `0x0440` is used by timer initialization and scheduling paths.
- No proven dataflow currently connects the second word of the binary SNG tempo record to a playback-tempo update routine. It remains possible that the consumer is in an untraced path, but it must not be assumed. The additional candidates `FUN_1000_341E` (special-record payload scan), `FUN_1000_3543` (event/duration pre-scan), and `FUN_1000_2D27` (Program Change rewrite) do not show the tempo-record value being passed to the timer globals. `FUN_1000_8888` schedules a parsed text-command delay plus `0x0440`, but its connection to the binary SNG tempo table is unproven.
- Do not reuse the SNG2S MIDI-export curve `floor(120*v/64)` as the MIMPIV4 playback formula without locating that consumer or obtaining runtime evidence.

**Next evidence:** trace the actual playback event dispatcher and any alternate interval/scaling state for the SNG tempo-record second word, or obtain a runtime trace comparing timer/scheduler state before and after a known tempo record. Keep SNG2S MIDI-export arithmetic separate.


### H-018 — Numeric tempo conversion resolved; tick placement remains open (2026-10-22)

Static tracing of `FUN_1000_3406` confirms that SNG2S reads a tempo value from the table at `0x21E0`, multiplies by the base tempo at `0x2962`, divides by 64, and clamps the result to 1–480. With base tempo 120, value 37 becomes 69 BPM. This is now a supported fact, not an open arithmetic hypothesis.

`FUN_1000_3DE2` initializes `0xF96` to `0xFFFF`; `FUN_1000_33D4` calls `FUN_1000_3406` before resetting it. The first call therefore skips the pending-delta increment used by later calls.

Still unresolved: why the coarse 869000 µs/qn event is serialized at absolute tick 8. `0x6EE` is read by setup paths, but its initialized value is missing from the export. The proposed value 4 remains a hypothesis only. Runtime state or initialized data is needed before changing converter behavior.

### H-019 — `0x7BC` override flags and v2 setup SysEx producer mapping (2026-10-24)

**Status:** The on-disk initial byte and the two observed reference-MIDI producer pairs are explained by static analysis; runtime state and exhaustive alias-write exclusion remain unverified.

- The original `SNG2S.EXE` initialized image maps `DS:0x07BC` to file offset `0x6C2C`, whose initial byte is `0x00`. This is the executable's on-disk initial value, not a runtime trace.
- Bits 0–2 of `0x7BC` gate assignments to `0x7C0`, `0x7C2`, and `0x7C4`. The complete available 18,795-line analysis export contains three bit-test reads and no direct store to `0x7BC`; indirect/aliased writes or paths outside the export are not fully ruled out.
- For the inspected v2 `SHIZUME0.SNG`, the class-1 fallback values are `6,6` and `0x7BE=5`. With the on-disk initial flag byte and no intervening write, the conditional assignments would replace the three values with `0,6,6`; this conditional static result should not be promoted to a runtime observation.
- Two distinct producer paths explain the inspected reference MIDI pairs: `FUN_1000_23C0(4)` matches the pair at ticks 4 and 8, while `FUN_1000_1CE4` templates `0xF34` / `0xF3F` match the pair at ticks 100 and 104. Both comparisons include calculated data, checksum, and terminator. They are separate paths, not competing expected values.
- Do not change Glosa setup values or add hard-coded SysEx based on this static mapping alone. No converter change, runtime trace, or playback was performed.

**Remaining boundary:** confirm whether runtime code can alter `0x7BC` or the related globals through indirect/aliased writes, and perform a runtime trace if available. The static producer mapping for the four inspected events is complete; runtime behavior is not.
