# UAsset Version Inspector

Windows desktop tool for inspecting common Unreal Engine project and package files.

The tool reads package headers, project/plugin descriptors, and common embedded references to help identify:

- Saved Unreal Engine version
- Compatible Unreal Engine version
- UE4 / UE5 serialization version fields
- Licensee version field
- `.uproject` engine association, modules, plugins, target platforms, and project folders
- `.uplugin` metadata, modules, and plugin dependencies
- `.uexp` / `.ubulk` sidecar file relationships
- `/Game`, `/Engine`, and `/Script` references
- Same-name `.uexp` / `.ubulk` sidecar files
- Missing `/Game` dependencies when the asset is inside a `Content` folder
- English, Chinese, and Japanese UI/report languages
- Custom Windows application icon

## Download

Download the latest Windows build:

[UAssetVersionInspector-windows-x64.zip](https://github.com/Luimya/UAssetVersionInspector/releases/latest/download/UAssetVersionInspector-windows-x64.zip)

Extract the zip and run:

```text
UAssetVersionInspector.exe
```

Release builds are distributed through GitHub Releases instead of being stored in the repository tree.

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

This build targets Windows with .NET 9 Desktop Runtime.

If the app does not start on another PC, install the .NET 9 Desktop Runtime from Microsoft, or rebuild a self-contained package.

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
