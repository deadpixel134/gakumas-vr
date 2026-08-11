[한국어](../ko/INSTALLATION.md) | [English](../en/INSTALLATION.md) | [日本語](INSTALLATION.md)

# インストール

[プロジェクトホーム](../../README.ja.md) · [使い方](USAGE.md) · [構成](ARCHITECTURE.md)

## 必要環境

- Windows 11 x64
- 正常にインストールされた学園アイドルマスターDMM版
- OpenXRランタイムを利用できるPC VR環境
- v0.163.0配布ZIPに含まれるDobbyランタイム依存ファイル

> Localifyがある場合、インストーラーは翻訳・フォント・テクスチャ・設定と既存の`BepInEx/core/dobby.dll`を保持します。Localifyがない場合は必要なDobbyだけをインストールし、Localifyファイルは作成しません。このクリーンインストール経路は自動インストール／削除検証を通過していますが、VR実機確認はまだです。

## インストール手順

1. GitHubの[Releasesページ](https://github.com/deadpixel134/gakumas-vr/releases)から最新プレリリースZIPをダウンロードします。
2. ゲームを終了し、`gakumas.exe`が実行中でないことを確認します。
3. ZIPをゲームフォルダー外の一時フォルダーへ展開します。
4. `GakumasVR.Installer.exe`を実行します。
5. `gakumas.exe`、`GameAssembly.dll`、`UnityPlayer.dll`があるゲームフォルダーを選択します。
6. **インストール**を押して完了を待ちます。既存ファイルは`ゲームフォルダー\vrmod\rollback\`にバックアップされます。
7. **設定を開く**を押すか、`ゲームフォルダー\vrmod\tools\GakumasVR.Configurator.exe`を実行します。
8. 使用するPC VRソフトをアクティブなOpenXRランタイムに設定し、ゲームを起動します。

設定はゲームを完全に終了した状態で保存してください。次回起動時から反映されます。

## OpenXRランタイム

### Virtual Desktop — 実機確認済み

1. QuestからVirtual DesktopでPCへ接続します。
2. Virtual Desktop StreamerでVDXR／Virtual Desktop OpenXRをアクティブにします。
3. HMD内のデスクトップからDMMランチャーを開き、ゲームを起動します。

Virtual Desktopの**Games**タブではなく、デスクトップ上のDMMランチャーを使用してください。この構成ではSteamVRを起動する必要はありません。

### SteamVR — 予備対応

SteamVRをアクティブなOpenXRランタイムに設定し、DMMランチャーからゲームを起動します。Windows D3D11 OpenXRとして互換性が見込まれますが、本プロジェクトではまだ実機確認していません。

### Meta Quest Link／Air Link — 予備対応

Quest LinkまたはAir Linkで接続し、Meta Quest LinkアプリでMetaランタイムをアクティブなOpenXRに設定してゲームを起動します。まだ本プロジェクトで実機確認していません。

## 更新

ゲームを終了し、新しいReleaseのインストーラーで同じゲームフォルダーを選択します。`vrmod/config/settings.json`は保持され、置き換えるファイルはロールバック用にバックアップされます。

## アンインストール／ロールバック

1. ゲームを完全に終了します。
2. 使用した配布フォルダーの`GakumasVR.Installer.exe`を実行します。
3. ゲームフォルダーを選択し、**アンインストール**または表示される**ロールバック**を実行します。

インストーラーが操作するのはmanifestに記録されたファイルだけです。インストール後に変更されたファイルはハッシュが異なるため削除せず、警告を表示します。元からあったファイルはバックアップから復元します。`vrmod/config/settings.json`とLocalifyの`version.dll`、`gakumas-local/`は保持されます。

## トラブルシューティング

- VRが開始しない場合は、アクティブなOpenXRランタイムと`BepInEx/core/dobby.dll`を確認してください。
- 設定が反映されない場合は、ゲーム終了中に保存したか確認してください。
- 表示されるのに操作できない場合は、ゲームウィンドウを最前面にしてください。
- 障害時もPC側のゲームは動作を続け、VRは平面パネルへフォールバックする設計です。
- Issueへ`vrmod/logs/`のログを添付する前に、アカウントID、viewer ID、token、起動認証情報が含まれていないか確認してください。
