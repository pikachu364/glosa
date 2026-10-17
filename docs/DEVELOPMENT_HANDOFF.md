# Glosa 引き継ぎメモ（Ballade SNG 変換）

最終更新: 2026-10-17

## 目的
チャット移行後も作業履歴、確認済み事項、未解決の不具合、次の調査手順を引き継ぐ。

## リポジトリと環境
- GitHub: https://github.com/pikachu364/glosa
- Windows 作業ディレクトリ: R:\glosa
- 参照用の元実装: R:\jmplayer（https://github.com/jihodaya/jmplayer）
- 直近確認時のローカルブランチ: main
- main の確認済みコミット: 8325a0d
- PR #1（SNG→SMF 変換）マージ済み: https://github.com/pikachu364/glosa/pull/1
- PR #3（バージョン別テストデータ文書）マージ済み: https://github.com/pikachu364/glosa/pull/3
- PR #2: https://github.com/pikachu364/glosa/pull/2

## 確認済みテスト
次のコマンドでテスト成功を確認した。

    dotnet test -p:Platform="Any CPU"

結果: 964 passed、0 failed、0 skipped。

環境によっては dotnet test 単体で、ソリューション設定 Debug|x64 が無効という MSB4126 エラーになった。Platform="Any CPU" を指定すると通った。

## 未解決の不具合
次のコマンドで v1 fixture の変換を試した。

    dotnet run --project src/Glosa.Cli -- convert-sng testdata/ballade/v1/runner.SNG testdata/ballade/v1/runner.mid

結果:

    error: Invalid or unsupported Ballade SNG layout.

原因は未特定。修正済みと扱わないこと。

## v1 fixture の調査記録
- ファイル: testdata/ballade/v1/runner.SNG
- サイズ: 85,897 bytes
- SHA-256: e61b8b0315a3752dfa3d3546d5a436df0d4b0421201183c21ce4bf1780556e15
- ヘッダー: BALLADE SONG Ver. 1.00
- バージョン文字列はオフセット18にある。
- サイズ表はオフセット 0x46 から始まり、v1 は10パート。
- 元資料には headerEnd 候補として tableEnd+94、tableEnd+2、tableEnd が記載されている。
- 各パートのサイズはサイズ表の値に81を足して計算している。
- headerEnd=116 の候補で計算した最初のパート位置は1510。そこから読み出した値は used=0、ctl=53248、idx=1。ctl が不自然に見えるが、この候補が誤りと断定はしていない。
- オフセット110～129のバイト列も確認済み。配置の結論はまだ出ていない。

## 関係するファイル
- 変換実装: src/Glosa.Core/Smf/SngConverter.cs
- CLI: src/Glosa.Cli
- v1 fixture: testdata/ballade/v1/runner.SNG
- v2 fixture: testdata/ballade/v2/SHIZUME0.SNG
- v3 fixture: testdata/ballade/v3/START__0.SNG
- v4 fixture: testdata/ballade/v4/BOURREE0.SNG
- 元実装: R:\jmplayer\sngmidi.cpp
- 元実装の形式資料: R:\jmplayer\docs\formats\sng-ballade.md

## SNG fixture 一覧

| バージョン | ファイル | サイズ | SHA-256 |
|---|---|---:|---|
| v1 | testdata/ballade/v1/runner.SNG | 85,897 | e61b8b0315a3752dfa3d3546d5a436df0d4b0421201183c21ce4bf1780556e15 |
| v2 | testdata/ballade/v2/SHIZUME0.SNG | 75,597 | eb6e1eb5bd1aeb36acb7b53a89fb8409b09e7d3988a45847309da105622272ee |
| v3 | testdata/ballade/v3/START__0.SNG | 82,391 | b4b719cec15139e411c94c34b05ca698e9432b9ccad308a808f08a648256cbe4 |
| v4 | testdata/ballade/v4/BOURREE0.SNG | 49,517 | a1eb69b4735b809e35c9c368776e9dae621e30405dc5a9c717133654c5f7b715 |

v3 は以前ローカル変換で 48,923-byte の MIDI ができた記録があるが、読戻し・再生検証は未完了。他のバージョンも変換と再生の検証が必要。

## 元実装との照合で分かっていること
R:\jmplayer\sngmidi.cpp の buildLayout では以下を確認した。
- 先頭シグネチャとバージョンを確認する。
- v1/v2/v3 は10パート、v4 は16パート。
- 32-bit little-endian のサイズ表は 0x46 から読む。
- 各パートのサイズ計算に81を足す。
- ヘッダー候補は tableEnd+94、tableEnd+2、tableEnd。
- パート先頭の +112、+114、+116 から used、ctl、idx を読む。
- used は8の倍数、ctl は4の倍数で、used+ctl+idx が capacity 以下であることを確認する。
- 元実装のコメントでは、壊れたファイルではブロック末尾がファイル末尾を越えることを許容するが、ブロック開始位置はファイル内に必要としている。

Glosa の TryLayout も概ね同じロジックに見える。原因はまだ特定できていない。条件ごとの失敗箇所を診断出力して確認する。

## 次にやること
1. 元実装の該当箇所を再確認する。

       findstr /n /c:"buildLayout" /c:"tableEnd" /c:"headerEnd" R:\jmplayer\sngmidi.cpp

2. Glosa の SngConverter.cs と元実装の buildLayout を、関数全体および rd16/rd32 相当の読み取りヘルパーまで比較する。
3. 必要なら一時的な診断出力を加え、3つの headerEnd 候補ごとに各パートの start、size、used、ctl、idx、capacity と失敗条件を表示する。
4. 原因特定後に最小限の修正を行い、dotnet test -p:Platform="Any CPU" と v1 変換を再実行する。
5. v1 が成功したら出力 MIDI のサイズ、読み戻し、再生可能性を確認し、他のバージョンも検証する。
6. テスト結果が確定してから fixture 文書の検証ステータスを更新する。

## 新しいチャットで最初に伝える文章
「Glosa リポジトリで Ballade SNG→MIDI 変換を調査中です。PR #1 と PR #3 はマージ済み、main は 8325a0d。dotnet test -p:Platform="Any CPU" は964件成功。ただし v1 fixture の convert-sng は Invalid or unsupported Ballade SNG layout. で失敗し、原因未特定です。この引き継ぎ文書の『次にやること』から、一手ずつ案内してください。Windows CMD 向けのコマンドを示し、長い手順をまとめて出さないでください。」
