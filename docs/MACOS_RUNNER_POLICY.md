# macOS GitHub Actions runner の運用方針

## 目的

macOS runner の変更による予期しない CI・リリースビルドの失敗を避けつつ、必要以上に広い変更を行わず、動作確認できた環境を維持する。

## 現在の方針

当面、対象 workflow では `macos-15` を使用する。

- `.github/workflows/ci.yml`：CI の macOS matrix を `macos-15` に固定する。
- `.github/workflows/release.yml`：リリースビルドの macOS job を `macos-15` に固定する。

`macos-latest` が誤った指定という意味ではない。これは GitHub が提供する最新の macOS runner イメージを指す指定であり、将来のイメージ更新の影響を受ける可能性がある。`macos-15` は macOS 15 の runner を明示する指定で、環境を固定しやすい。

今回の変更は、以前の CI で macOS runner を固定したところ成功したという確認を踏まえた、安定性優先の暫定措置である。これだけで、元の失敗原因が runner の更新だったと断定するものではない。

## 変更範囲の原則

- 問題に関係する runner 指定だけを変更する。
- Windows・Linux の設定、ビルド手順、依存関係などは、別の根拠がない限り変更しない。
- 変更後は CI の実行結果を確認する。
- リリース workflow は、CI が通っただけで成功と判断せず、実際のリリースビルド結果を別に確認する。
- テスト結果を確認するまでは「設定を変更した」と「ビルドが成功した」を区別して記録する。

## 見直し時期

以下はプロジェクト内の目安であり、GitHub が指定する期限ではない。

1. **2026年11月中旬**：CI の実行履歴を確認し、macOS job が安定して成功しているか、失敗が再発していないかを確認する。
2. **2027年1月ごろ**：GitHub が提供する macOS runner の状況、プロジェクトの SDK・依存関係の対応状況、CI とリリースビルドの実績を確認する。
3. その後も、macOS runner の大きな更新や依存関係の更新があるときに見直す。

日付が来たことだけを理由に `macos-latest` へ戻さない。状況に変化がなければ `macos-15` を維持してよい。

## `macos-latest` に戻す判断基準

次の条件を確認できた場合に、専用の変更として試験する。

- GitHub の runner イメージ情報を確認し、`macos-latest` が指す環境を把握している。
- 使用中の SDK・ツールチェーン・依存関係が、その環境に対応している。
- `macos-latest` を指定した CI が成功する。
- リリース workflow も実行し、リリースビルドが成功することを確認できる。

可能であれば、runner 指定の変更だけを含む小さな PR で検証する。失敗した場合はログを確認し、原因が分かるまでは安定している `macos-15` に戻す。

## 確認チェックリスト

- [ ] CI の macOS job が成功している
- [ ] 失敗が起きた場合、ログと失敗箇所を確認した
- [ ] リリースビルドを別途確認した
- [ ] runner の移行情報と依存関係の対応状況を確認した
- [ ] `macos-latest` へ戻す場合、変更を小さくし、結果を記録した

## 参考情報

- [GitHub Actions: Choosing GitHub-hosted runners](https://docs.github.com/en/actions/how-tos/write-workflows/choose-where-workflows-run/choose-the-runner-for-a-job)
- [GitHub-hosted runners: runner images](https://github.com/actions/runner-images)

---

この文書は運用方針と見直し目安を記録するもの。runner の実際の成功状況は、対象ブランチの GitHub Actions の実行結果を確認して更新する。
