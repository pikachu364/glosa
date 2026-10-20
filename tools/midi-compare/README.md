# MIDI Compare

Windows 10 / 11 で、Sng2gs が出力した MIDI と開発中の Glosa が出力した MIDI をローカル比較するツールです。

Sng2gs による変換は自分で行います。このツールは SNG を読み込まず、外部送信もしません。

## 必要なもの
- Python 3.10 以降
- `mido` パッケージ

## セットアップ（PowerShell）

リポジトリのルートで実行：

~~~powershell
py -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r tools\midi-compare\requirements.txt
~~~

## 比較

~~~powershell
.\.venv\Scripts\python.exe tools\midi-compare\midi_compare.py "D:\local\reference.mid" "D:\local\glosa.mid"
~~~

JSON レポートも保存する場合：

~~~powershell
.\.venv\Scripts\python.exe tools\midi-compare\midi_compare.py "D:\local\reference.mid" "D:\local\glosa.mid" --json ".\comparison-report.json"
~~~

## 比較する項目
- 音高、チャンネル、ベロシティ、開始位置、音符の長さ
- テンポ、プログラムチェンジ、コントロールチェンジ、ピッチベンドなどの MIDI イベント
- SysEx の内容（内部でハッシュ比較。内容そのものは表示・保存しない）
- MIDI の PPQ が異なる場合の位置正規化

トラック名・曲名などのテキストメタデータ、トラック順、トラック数は合否判定から除外します。変換器ごとにトラック構成が異なる場合があるためです。

## 結果
- `PASS`: 比較対象のイベントが一致
- `FAIL`: 差分あり
- 終了コード `0`: PASS、`1`: FAIL、`2`: 入力・解析・レポートエラー

## 注意
比較結果の PASS は、2つの MIDI が一致したことを示すだけで、両方が SNG 仕様に対して正しいことまでは保証しません。実際の SNG / MIDI は GitHub に追加せず、PC 内で検証してください。
