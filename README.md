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

## 中文说明

UAsset Version Inspector 是一个 Windows 桌面工具，用于检查常见 Unreal Engine 项目文件和资源包文件。

它主要用于判断从网络下载的 UE 文件最初是由哪个 Unreal Engine 版本制作或保存的。

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

## 日本語説明

UAsset Version Inspector は、Unreal Engine の一般的なプロジェクトファイルやパッケージファイルを調べるための Windows デスクトップツールです。

インターネットから入手した UE ファイルが、元々どの Unreal Engine バージョンで作成または保存されたものかを確認する用途を想定しています。

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
