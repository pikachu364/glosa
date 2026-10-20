# 本家 upstream の更新手順

このリポジトリでは、開発・調査の履歴を保持するため、**本家の更新を自分の `main` に直接取り込まず、作業ブランチと Pull Request を経由する**。

- 自分のリポジトリ: `pikachu364/glosa`（remote 名 `origin`）
- 本家: `hsshss/glosa`（remote 名 `upstream`）
- 本家へ push しない。変更先は自分のリポジトリだけにする。

## 1. 更新があるか調べる

作業ツリーに未コミット変更がないことを確認してから実行する。

```powershell
git fetch origin
git fetch upstream
git rev-list --left-right --count origin/main...upstream/main
git log --oneline origin/main..upstream/main
git diff --stat origin/main...upstream/main
```

最初の数値は `origin/main` のみのコミット数、2番目は `upstream/main` のみのコミット数。2番目が `0` で、`git log origin/main..upstream/main` も空なら、本家から取り込むべき新しいコミットはない。何もマージしない。

差分がある場合は、コミット一覧とファイル差分を先に読み、Glosa の独自変更と衝突しそうな箇所を確認する。

## 2. 更新用ブランチで取り込む

`main` では作業しない。日付を付けた新しいブランチを、自分の GitHub の最新 `origin/main` から作る。

```powershell
git switch -c maintenance/upstream-YYYY-MM-DD origin/main
git merge upstream/main
```

`YYYY-MM-DD` は実行日で置き換える。コンフリクトが出たら、機械的にどちらかを採用せず、変更の意図を確認して解決する。解決後は少なくとも以下を実行する。

```powershell
dotnet build
dotnet test
git diff --check
git status
```

テストが通らない、または変更の意味を判断できない場合はマージ結果を push せず、原因を調べる。

## 3. 自分の GitHub でレビューする

```powershell
git push -u origin maintenance/upstream-YYYY-MM-DD
```

そのブランチから `pikachu364/glosa` の `main` 向け Pull Request を作る。PR で取り込まれるファイル、CI、コンフリクト解決を確認し、問題がなければマージする。

## 安全ルール

- `git push upstream` は実行しない。
- `main` に直接マージしない。
- `git reset --hard`、force push、作業ファイルの一括削除で差分を解消しない。
- 未追跡ファイルや未コミット変更がある場合は、先に中身を確認・保全する。
- 本家に新しいコミットがないときは、同期のためだけに空の PR や不要なマージを作らない。

## 現在の確認記録

2026-10-20 時点で、本家 `upstream/main` の SHA は `75ea8928d081cdae21c67e658d52940174476d68`。自分の GitHub の `main` は `4cd1c58d75f592035525ac4b0551b8880f9c971f` で、本家の履歴を含み、本家より 327 コミット先行・0 コミット遅れだった。この状態では本家から取り込む新規コミットはなかった。

この記録はその時点のスナップショット。次回は必ず fetch 後に差分を再確認する。
