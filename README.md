# UAsset Version Inspector

Windows desktop tool for inspecting common Unreal Engine project and package files.

It is designed for checking the original Unreal Engine version used to create UE files downloaded from the internet.

## Download

Download the latest lightweight Windows build:

[UAssetVersionInspector-windows-x64.zip](https://github.com/Luimya/UAssetVersionInspector/releases/latest/download/UAssetVersionInspector-windows-x64.zip)

Extract the zip and run:

```text
UAssetVersionInspector.exe
```

Release builds are distributed through GitHub Releases instead of being stored in the repository tree.

## Features

- Saved Unreal Engine version
- Compatible Unreal Engine version
- UE4 / UE5 serialization version fields
- Licensee version field
- `.uproject` engine association, modules, plugins, target platforms, and project folders
- `.uplugin` metadata, modules, and plugin dependencies
- `.uexp` / `.ubulk` sidecar file relationships
- `/Game`, `/Engine`, and `/Script` references
- Missing `/Game` dependencies when the asset is inside a `Content` folder
- English, Chinese, and Japanese UI/report languages
- Custom Windows application icon

## Usage

1. Open `UAssetVersionInspector.exe`.
2. Choose a language from the menu: `English`, `中文`, or `日本語`.
3. Click `Open UE file`, or drag one or more Unreal files into the window.
4. Read the generated diagnostic report.

Supported extensions:

```text
.uasset, .umap, .uproject, .uplugin, .uexp, .ubulk
```

The tool is read-only. It does not modify Unreal assets.

## Runtime

The lightweight release targets Windows with .NET 9 Desktop Runtime.

If the app does not start on another PC, install the .NET 9 Desktop Runtime from Microsoft.

## Windows Security Notice

This project is currently not code-signed. Windows may show a warning such as:

```text
The publisher could not be verified
```

or SmartScreen may identify the app as coming from an unknown publisher.

This happens because the executable does not have a paid code-signing certificate. It does not mean the tool modifies your Unreal files. The app is read-only and only inspects selected files.

## 中文说明

UAsset Version Inspector 是一个 Windows 桌面工具，用于检查常见 Unreal Engine 项目文件和资源包文件。

它主要用于判断从网络下载的 UE 文件最初是由哪个 Unreal Engine 版本制作或保存的。

下载最新版轻量 Windows 构建：

[UAssetVersionInspector-windows-x64.zip](https://github.com/Luimya/UAssetVersionInspector/releases/latest/download/UAssetVersionInspector-windows-x64.zip)

解压后运行：

```text
UAssetVersionInspector.exe
```

可检查的信息包括：

- 资源保存时使用的 Unreal Engine 版本
- 兼容的 Unreal Engine 版本
- UE4 / UE5 序列化版本字段
- Licensee 版本字段
- `.uproject` 的引擎关联、模块、插件、目标平台和项目文件夹
- `.uplugin` 的插件信息、模块和插件依赖
- `.uexp` / `.ubulk` 外部数据文件关系
- `/Game`、`/Engine`、`/Script` 资源引用
- 当前 `Content` 目录下可能缺失的 `/Game` 依赖

使用方法：

1. 打开 `UAssetVersionInspector.exe`。
2. 在语言菜单中选择 `中文`。
3. 点击 `Open UE file`，或把 UE 文件拖入窗口。
4. 查看生成的诊断报告。

支持的文件类型：

```text
.uasset, .umap, .uproject, .uplugin, .uexp, .ubulk
```

此工具只读取文件，不会修改 Unreal 资源。

轻量版需要目标电脑安装 .NET 9 Desktop Runtime。

### Windows 安全提示

当前版本没有进行代码签名，因此 Windows 可能会提示：

```text
无法验证发布者
```

或被 SmartScreen 标记为未知发布者。

这是因为程序没有使用付费代码签名证书，并不代表工具会修改你的 Unreal 文件。本工具是只读工具，只会检查你选择的文件。

## 日本語説明

UAsset Version Inspector は、Unreal Engine の一般的なプロジェクトファイルやパッケージファイルを調べるための Windows デスクトップツールです。

インターネットから入手した UE ファイルが、元々どの Unreal Engine バージョンで作成または保存されたものかを確認する用途を想定しています。

最新版の軽量 Windows ビルドをダウンロード：

[UAssetVersionInspector-windows-x64.zip](https://github.com/Luimya/UAssetVersionInspector/releases/latest/download/UAssetVersionInspector-windows-x64.zip)

zip を展開して実行：

```text
UAssetVersionInspector.exe
```

確認できる情報：

- アセットが保存された Unreal Engine バージョン
- 互換性のある Unreal Engine バージョン
- UE4 / UE5 のシリアライズバージョンフィールド
- Licensee バージョンフィールド
- `.uproject` のエンジン関連付け、モジュール、プラグイン、対象プラットフォーム、プロジェクトフォルダー
- `.uplugin` のメタデータ、モジュール、プラグイン依存関係
- `.uexp` / `.ubulk` の外部データファイル関係
- `/Game`、`/Engine`、`/Script` の参照
- 現在の `Content` フォルダー内で不足している可能性がある `/Game` 依存関係

使い方：

1. `UAssetVersionInspector.exe` を開きます。
2. 言語メニューから `日本語` を選択します。
3. `Open UE file` をクリックするか、UE ファイルをウィンドウにドラッグします。
4. 生成された診断レポートを確認します。

対応ファイル：

```text
.uasset, .umap, .uproject, .uplugin, .uexp, .ubulk
```

このツールは読み取り専用です。Unreal のアセットを変更しません。

軽量版を使用するには、対象 PC に .NET 9 Desktop Runtime が必要です。

### Windows セキュリティ通知

現在のバージョンはコード署名されていません。そのため、Windows で次のような警告が表示される場合があります。

```text
発行元を確認できません
```

または SmartScreen によって不明な発行元として表示される場合があります。

これは、有料のコード署名証明書で exe に署名していないためです。このツールが Unreal ファイルを変更するという意味ではありません。本ツールは読み取り専用で、選択したファイルを検査するだけです。

## Batch Report Mode

```powershell
UAssetVersionInspector.exe --report-file report.txt MyAsset.uasset
```

## Build

```powershell
dotnet build .\UAssetVersionInspector\UAssetVersionInspector.csproj -c Release
```

Framework-dependent publish:

```powershell
dotnet publish .\UAssetVersionInspector\UAssetVersionInspector.csproj -c Release --self-contained false -o .\dist\UAssetVersionInspector
```
