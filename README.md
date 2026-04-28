# UAsset Version Inspector

Windows desktop tool for inspecting Unreal Engine `.uasset` files.

The tool reads package header information and common embedded references to help identify:

- Saved Unreal Engine version
- Compatible Unreal Engine version
- UE4 / UE5 serialization version fields
- Licensee version field
- `/Game`, `/Engine`, and `/Script` references
- Same-name `.uexp` / `.ubulk` sidecar files
- Missing `/Game` dependencies when the asset is inside a `Content` folder

## Download

Use the packaged build in:

```text
dist/UAssetVersionInspector-20260428-github.zip
```

Extract the zip and run:

```text
UAssetVersionInspector.exe
```

## Usage

1. Open `UAssetVersionInspector.exe`.
2. Click `Open .uasset`, or drag one or more `.uasset` files into the window.
3. Read the generated diagnostic report.

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
