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
**状態:** v2〜v4 の実 C# 変換出力と参照 MIDI の集計比較テストが CI で成功。トラック／チャンネル単位の検証は未完了。

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


## H-015 — 0x622 は SNG2S の /F オプションに対応する可能性

**状態:** 有力な推定だが未確定。文字列テーブルの実バイト照合と参照実行条件の確認が必要。

- FUN_1000_184A は文字列テーブル offset 0xF05 の option token を検出すると 0x622 を設定する。ただし、利用可能な解析テキストには token の実文字列が含まれていない。
- SNG2S.DOC は /F を MIDI track generation order の変更オプションとして説明する。逆アセンブルで観測した 0x622 の下流利用（alternate track-order selection と queue drain 前の pending-delta reset）はこの説明と整合する。
- したがって 0x622 ↔ /F は有力な推定だが、実文字列の照合なしに確定扱いしない。また reference runner.MID 生成時に /F が指定されたかは不明。
- 0x6EE は複数の event-output path で pending delta として読み出されるが、利用可能な解析テキスト内では直接書き込みを見つけられていない。値・由来を未解決として維持し、tick 8 の根拠に推測で使わない。

**次の確認:** 文字列テーブルを含むバイナリ／データダンプ、実際の SNG2S 呼び出し条件、0x6EE の初期値または間接書き込みを確認する。0x622 の対応が確定するまでは H-015 の推定状態を維持する。


### H-015 follow-up — 0xF05 token の比較長を確認（2026-10-21）

`FUN_1000_184A` から `FUN_1000_5EC8(local_56, 0xF05, 2)` が呼ばれ、比較結果が一致（戻り値 0）の場合に `0x622 = 1` となる。`FUN_1000_5EC8` の命令列は指定長の文字列比較と整合するため、0xF05 は2-byte token の候補位置である。これは H-015 の parser 経路を具体化するが、token の文字そのものは文字列データ未収録のため確認できず、/F 対応は依然として推定。

### H-016 — 0x6EE は複数の出力経路で使う delta/間隔値の可能性

**状態:** 用途については静的根拠あり、意味・値・単位は未確定。

- 解析テキストの命令行にある `[0x6EE]` 参照29箇所はすべて読み出しで、直接書き込みは見つかっていない。
- `FUN_1000_1CE4` の queue drain 後には、読み出した値を `0x1ED8:0x1EDA` に加算し、同値を `0xF9A:0xF9C` にセットする。別の event-output path にも同じ形があるため、単なる tempo 値というより、出力間隔・delta に関係する共通値である可能性がある。
- ただし初期化データ、別名参照、間接 store の可能性が残る。値が分からないため、MIDI tick との換算や tick 8 との対応はまだ導けない。

**次の確認:** 実行ファイルの data segment / relocation / 文字列領域を含むダンプ、または runtime memory trace を得る。解析テキストの xref scan だけでは値の由来は確定しない。


### H-016 follow-up — 0x6EE の数値候補は4 ticks（暫定）

参照 `runner.MID` を SMF として直接解析すると、track 1 の SysEx event は tick 0 / 4 / 8 / 8 に出力されている（FACTS F-068）。一方、v1 の `FUN_1000_1CE4` は条件成立時に2回 SysEx-style event を生成して drain し、各 drain 後に `0x6EE` を pending delta `0xF9A:0xF9C` に設定する。`FUN_1000_3F79` の共通 byte 列、`FUN_1000_5FAA` のデータ生成、`FUN_1000_3F54` の checksum 用和計算、`FUN_1000_3EED` の checksum + `F7` 追加から、これらが SysEx 形式であることは静的に支持される。

この2回の drain が track 1 の tick 0 と tick 4 の最初の2件に対応するなら、`0x6EE = 4` と推定でき、2回目の drain 後に残る delta 4 は次の出力を tick 8 に進め得る。これは tick 8 tempo の説明につながる具体的候補だが、まだ以下が未確認である。

- `0xF34` / `0xF3F` から作られる queue payload が、参照 SysEx のどの payload と一致するか。
- queue drain ごとの `0x6F2:0x6F4` と `0x1ED8:0x1EDA` の実際の値。
- 参照実行時の `0x622` が pending delta reset を有効にしたか。

したがって `0x6EE = 4` は有望な仮説であり、確認済み事実ではない。runtime trace または payload / tick のイベント単位対応が得られるまでは converter を変更しない。


追加の対応根拠（2026-10-21）: 参照 MIDI の最初の2件の SysEx は、どちらも payload 12 bytes で共通 prefix を持つ。一方、`FUN_1000_1CE4` の最初の2経路は同じ SysEx-style builder を使い、異なる文字列 offset `0xF34` / `0xF3F` のデータを出力して checksum と `F7` を追加する。event 数・形式・順序・tick 間隔が整合するため、これらが tick 0 / 4 の2件に対応し `0x6EE = 4` となる推定は以前より強い。ただし文字列データ本体が解析資料にないため byte-for-byte の照合は未完了で、確定事実には格上げしない。
