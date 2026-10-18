# Glosa 開発メモ

[English](DEVELOPMENT.md) | 日本語

Glosa を作る側のための文書。構成、ビルド、動作確認の道具、配布物の作り方を置く。設計の判断とその理由は [IMPLEMENTATION.md](IMPLEMENTATION.md)、使い方は [README.ja.md](README.ja.md) にある。

---

## 構成

| プロジェクト | 内容 |
|---|---|
| `src/Glosa.Core` | SMF 解析、シーケンサ、曲送り、DEF 解析、エミュレーション（解決・実行時変換・パネル状態）、CP932 |
| `src/Glosa.Midi` | MIDI 出力・入力の抽象（`IMidiOutput`・`IMidiInput` とそのファクトリ）、装置を名前で指す `DeviceName`、UMP を送るバックエンドが共用する `UmpEncoder` |
| `src/Glosa.Midi.Windows` | Windows MIDI Services バックエンド（vtable を直接呼ぶ）、WinMM バックエンド（P/Invoke）、高分解能タイマ |
| `src/Glosa.Midi.MacOS` | CoreMIDI バックエンド（P/Invoke） |
| `src/Glosa.Midi.Linux` | ALSA シーケンサーのバックエンド（P/Invoke） |
| `src/Glosa.Midi.Brack` | Brack がホストするオーディオプラグインを出力にするバックエンド。OS を問わない。Brack が無いビルドでは中身が無い |
| `src/Glosa.App` | Avalonia の GUI |
| `src/Glosa.Cli` | 動作確認用のコンソールハーネス |
| `tests/Glosa.Tests` | xUnit |

依存パッケージは **Avalonia**（GUI）、**CommunityToolkit.Mvvm**（ビューモデル）、**YamlDotNet**（設定とプレイリストの保存形式）と、テスト用のものだけ。

## ビルドと実行

```bash
dotnet build
dotnet test
```

push と pull request のたびに、`.github/workflows/ci.yml` が Windows・macOS・Linux でソリューション全体をビルドし、テストを回す。MIDI のバックエンドは OS ごとにそのビルドにしか入らないので、ほかの OS のビルドを壊す変更もここで分かる。

ビルドは、どの OS でも、オーディオプラグインのホスト Brack を次の順に探して参照し、見つかれば定数 `BRACK` を定義する（`Directory.Build.props`）。参照するのは `src/Glosa.Midi.Brack` で、App はそれを Brack があるときだけ参照する。
このリポジトリの隣に Brack のリポジトリ（`../brack`）があれば、その .NET バインディングをソースからビルドし、Brack がビルド済みのネイティブライブラリ（Windows では `build\bin` と `build-x86\bin` の `brack.dll`）を出力にコピーする。
無ければ、`BrackVersion` に書いたバージョンの NuGet パッケージを nuget.org から取る。隣にリポジトリの無い CI とリリースは、これを使う。
`BrackVersion` が空ならどちらも使わず、Brack 無しでビルドする。
隣にリポジトリがあってもパッケージで確かめたいときは、`-p:UseBrackPackage=true` を付ける。
ソースから参照しているときは、テストが Brack のテスト用の音源とエフェクト（`build/bin/brack-test-synth.clap`、`build/bin/brack-test-gain.clap`）でプラグインを鳴らすので、Brack をテスト込みでビルドしておく。macOS では Brack がメインスレッドで動くので、テストは自前のエントリポイント（`tests/Glosa.Tests/Program.cs`）で別のスレッドに移し、メインスレッドではランループを回す。

GUI:

```bash
dotnet run --project src/Glosa.App
```

```bash
dotnet run --project src/Glosa.App -- --def path/to/default.DEF --use SC-88PRO --target MU2000 --device "loopMIDI Port 1" song.mid
```

オプションは [README.ja.md](README.ja.md) にある。試しに動かすときなど、普段の設定に触れたくない起動には `--config <folder>` で設定とプレイリストを置くフォルダを変える（既定は [設定の保存](IMPLEMENTATION.md#設定の保存)を参照）。

```bash
# 出力デバイス一覧
dotnet run --project src/Glosa.Cli -- devices

# ファイル情報（トラック数・長さ・SysEx・曲名）
dotnet run --project src/Glosa.Cli -- info path/to/song.mid

# 再生
dotnet run --project src/Glosa.Cli -- play path/to/song.mid --device 0

# エミュレーションを通して再生
dotnet run --project src/Glosa.Cli -- play song.mid --device 0 --def path/to/default.DEF --use MU2000 --target SC-88PRO

# シーケンサが時計にどれだけ忠実か（内蔵のメトロノームで測る）
dotnet run --project src/Glosa.Cli -- jitter
dotnet run --project src/Glosa.Cli -- jitter --spacing 1 --seconds 5
```

コンソールハーネスのオプションは、引数なしで起動すると出る。

> `dotnet run` は `--` の前のオプションを自分で解釈するため、
> アプリ側の引数は必ず `--` の後ろに置くこと。

### 配布物

Windows は発行したフォルダを zip（`publish/glosa-<version>-<rid>.zip`）にする。展開したフォルダでそのまま動く。

```powershell
tools\windows\package.ps1                      # このマシン向け
tools\windows\package.ps1 -Rid win-arm64       # ARM 向け
```

- `glosa.exe` にアイコンを入れるため、Windows で作る。
- 展開したフォルダには `install.cmd` と `uninstall.cmd`（`tools/windows/dist/`）を添える。

macOS はアプリのバンドル（`publish/Glosa.app`）にする。発行したものを `Contents/MacOS` に入れ、`Info.plist` とアイコンを添えて、アドホック署名する。
Apple シリコンの Mac は署名の無い実行ファイルを動かさないため。

```bash
tools/macos/build-app.sh                      # この Mac 向け
tools/macos/build-app.sh -r osx-x64           # Intel Mac 向け
```

- `Info.plist` と `glosa.icns` は `tools/macos/` にある。アイコンは Windows の `.ico` と同じ SVG から `tools/icon/build-icns.sh` で作る（macOS 標準の `sips` と `iconutil` だけで動く）。

Linux は tar.gz（`publish/glosa-<version>-<rid>.tar.gz`）にする。
展開したフォルダでそのまま動く。

```bash
tools/linux/package.sh                        # このマシン向け
tools/linux/package.sh -r linux-arm64         # ARM 向け
```

- 実行権限を書庫に残すため、Linux か macOS で作る。
- 展開したフォルダには `install.sh` と `uninstall.sh`（`tools/linux/dist/`）を添える。

`THIRD_PARTY_NOTICES.md` は配布物に入るライブラリを 1 つずつ挙げ、そのライセンスを載せている。
ライブラリを足したり更新したりしたら、ここも直す。

#### リリース

GitHub でリリースを公開すると、`.github/workflows/release.yml` が配布物を作ってリリースに添付する。バージョンはリリースのタグで、先頭の `v` を落とす（`v1.2.0` なら 1.2.0。
`v1.2.0-beta.1` のような接尾辞も付けられる）。バージョンの形でないタグは失敗する。

| ファイル | 作る場所 |
|---|---|
| `glosa-<version>-win-x64.zip`・`-win-arm64.zip` | Windows のランナー（`tools/windows/package.ps1`） |
| `glosa-<version>-osx-arm64.zip`・`-osx-x64.zip` | macOS のランナー（`tools/macos/build-app.sh`） |
| `glosa-<version>-linux-x64.tar.gz`・`-linux-arm64.tar.gz` | Linux のランナー（`tools/linux/package.sh`） |

- 先にテストを回し、通らなければ何も添付しない。
- Actions の **Release → Run workflow** にタグを渡すと、既にあるリリースの配布物を作り直して差し替える。

`build-app.sh` と `package.sh` は `--version`、`package.ps1` は `-Version` でバージョンを受け取る。無ければプロジェクトの `Version`（未設定なので 1.0.0）になる。

## Windows 音声デバイスの調査メモ（2026-10）

S-MU2000 の `make` 後に音声デバイスを開けないことがあり、Windows の再起動で復旧するという報告があった。現時点では Norton のスキャンとの因果関係は未確認であり、Norton が原因だとは断定しない。音を出せる環境が整うまで、実機での音声確認は保留する。

### 確認済みのこと

- Glosa CLI の `devices` は WinMM バックエンドで動作した。
- Windows MIDI Services の初期化は `Windows.Devices.Midi2.MidiApi` の有効化に失敗し、コード `0x80040154` が表示された。これは MIDI 2.0 API の利用可否に関する別の観察事項であり、S-MU2000 の音声出力エラーの原因と結びつける根拠はまだない。
- MIDI 出力一覧には `Microsoft GS Wavetable Synth`、`Roland port A`、`Roland port B`、`YAMAHA port A`、`YAMAHA port B` が表示された。ユーザーの説明では `Roland port A` は 88emuPlayer に接続されている。
- S-MU2000 のログでは MIDI IN A/B が `YAMAHA port A/B`、C/D/E は未接続と表示された。
- 問題が発生した際、Windows の `Audiosrv` と `AudioEndpointBuilder` は `Running` だった。
- `Win32_SoundDevice` の列挙では Realtek、NVIDIA、Intel Smart Sound、ASUS Utility などが `OK` だった。
- `Get-PnpDevice -Class AudioEndpoint` では `スピーカー (Realtek(R) Audio)`、`AI Noise-cancelling Output (ASUS Utility)`、および入力エンドポイントが `OK` だった。
- S-MU2000 起動ログに、パネル設定 `panel.txt` の 59 行目でボタン画像を開けないという警告が 2 回出た。音声デバイスの問題との関連は未確認。
- `--audio "スピーカー"` を指定した際、デバイス名が文字化けしたような形で「その名前の再生デバイスが無い」と報告された。引数の文字コード、デバイス名の完全一致、アプリ側の引数処理のどれかは未確定。

### 再開時の手順

1. まず Windows を再起動し、音声を出せる環境であることを確認する。音を出せない場所ではテスト音の確認を進めない。
2. S-MU2000 の起動時に音声デバイスの指定を省略した状態（`./build/gui.exe /r/roms --audio`）で再現するか確認する。
3. 問題が再現した場合、再起動する前に Windows の「再生」タブ（`Win + R` → `mmsys.cpl`）からスピーカーのテスト音を確認する。
4. その結果と S-MU2000 の正確なエラーメッセージを記録する。必要に応じて `Get-Service Audiosrv,AudioEndpointBuilder` と `Get-PnpDevice -Class AudioEndpoint` の結果も記録する。
5. Norton のスキャンを原因と決めつけず、再現時刻・ビルド時刻・スキャン時刻との関係を比較する。保護機能を無効にすることは、最初の切り分け手段にしない。

現状の結論：Windows のオーディオサービスとエンドポイントは調査時点で認識されていたが、実際のテスト音は未確認。S-MU2000 が再生デバイスを開けない原因は未特定。