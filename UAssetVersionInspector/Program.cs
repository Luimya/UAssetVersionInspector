using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace UAssetVersionInspector;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length >= 3 && args[0].Equals("--report-file", StringComparison.OrdinalIgnoreCase))
        {
            var reportPath = args[1];
            var reports = args.Skip(2)
                .Where(File.Exists)
                .Select(path => UAssetAnalyzer.Analyze(path).ToText(UiText.For(AppLanguage.English)));
            File.WriteAllText(reportPath, string.Join("\r\n\r\n" + new string('=', 78) + "\r\n\r\n", reports), Encoding.UTF8);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(args));
    }
}

internal sealed class MainForm : Form
{
    private readonly TextBox _output = new();
    private readonly Button _openButton = new();
    private readonly Button _copyButton = new();
    private readonly Button _saveButton = new();
    private readonly ComboBox _languageBox = new();
    private readonly Label _header = new();
    private readonly Label _hint = new();
    private AppLanguage _language = AppLanguage.English;
    private string[] _lastFiles = [];

    public MainForm(string[] args)
    {
        Text = "UAsset Version Inspector";
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        if (File.Exists(iconPath))
            Icon = new Icon(iconPath);
        Width = 1040;
        Height = 760;
        MinimumSize = new Size(760, 520);
        AllowDrop = true;
        StartPosition = FormStartPosition.CenterScreen;

        _header.Dock = DockStyle.Top;
        _header.Height = 42;
        _header.Font = new Font("Microsoft YaHei UI", 15, FontStyle.Bold);
        _header.TextAlign = ContentAlignment.MiddleLeft;
        _header.Padding = new Padding(14, 0, 0, 0);

        _hint.Dock = DockStyle.Top;
        _hint.Height = 30;
        _hint.Font = new Font("Microsoft YaHei UI", 9);
        _hint.TextAlign = ContentAlignment.MiddleLeft;
        _hint.Padding = new Padding(15, 0, 0, 0);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 46,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(12, 7, 12, 6)
        };

        _languageBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _languageBox.Width = 130;
        _languageBox.Height = 30;
        _languageBox.Items.AddRange(
        [
            new LanguageItem(AppLanguage.English, "English"),
            new LanguageItem(AppLanguage.ChineseSimplified, "中文"),
            new LanguageItem(AppLanguage.Japanese, "日本語")
        ]);
        _languageBox.SelectedIndexChanged += (_, _) =>
        {
            if (_languageBox.SelectedItem is LanguageItem item)
            {
                _language = item.Language;
                ApplyLanguage();
                if (_lastFiles.Length > 0)
                    AnalyzeFiles(_lastFiles);
            }
        };

        _openButton.Width = 130;
        _openButton.Height = 30;
        _openButton.Click += (_, _) => OpenFiles();

        _copyButton.Width = 100;
        _copyButton.Height = 30;
        _copyButton.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_output.Text))
                Clipboard.SetText(_output.Text);
        };

        _saveButton.Width = 100;
        _saveButton.Height = 30;
        _saveButton.Click += (_, _) => SaveReport();

        toolbar.Controls.Add(_languageBox);
        toolbar.Controls.Add(_openButton);
        toolbar.Controls.Add(_copyButton);
        toolbar.Controls.Add(_saveButton);

        _output.Dock = DockStyle.Fill;
        _output.Multiline = true;
        _output.ScrollBars = ScrollBars.Both;
        _output.WordWrap = false;
        _output.ReadOnly = true;
        _output.BorderStyle = BorderStyle.FixedSingle;
        _output.Font = new Font("Consolas", 10);

        Controls.Add(_output);
        Controls.Add(toolbar);
        Controls.Add(_hint);
        Controls.Add(_header);

        DragEnter += (_, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
        };

        DragDrop += (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files)
                AnalyzeFiles(files);
        };

        _languageBox.SelectedIndex = 0;
        ApplyLanguage();

        if (args.Length > 0)
            AnalyzeFiles(args);
    }

    private UiText T => UiText.For(_language);

    private void ApplyLanguage()
    {
        var t = T;
        Text = t.WindowTitle;
        _header.Text = t.Header;
        _hint.Text = t.Hint;
        _openButton.Text = t.OpenButton;
        _copyButton.Text = t.CopyButton;
        _saveButton.Text = t.SaveButton;

        if (_lastFiles.Length == 0)
            _output.Text = t.WaitingText;
    }

    private void OpenFiles()
    {
        var t = T;
        using var dialog = new OpenFileDialog
        {
            Filter = "Unreal files (*.uasset;*.umap;*.uproject;*.uplugin;*.uexp;*.ubulk)|*.uasset;*.umap;*.uproject;*.uplugin;*.uexp;*.ubulk|Packages (*.uasset;*.umap)|*.uasset;*.umap|Project/plugin descriptors (*.uproject;*.uplugin)|*.uproject;*.uplugin|Sidecar data (*.uexp;*.ubulk)|*.uexp;*.ubulk|All files (*.*)|*.*",
            Multiselect = true,
            Title = t.OpenDialogTitle
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            AnalyzeFiles(dialog.FileNames);
    }

    private void SaveReport()
    {
        if (string.IsNullOrWhiteSpace(_output.Text))
            return;

        var t = T;
        using var dialog = new SaveFileDialog
        {
            Filter = "Text report (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = $"uasset-report-{DateTime.Now:yyyyMMdd-HHmmss}.txt",
            Title = t.SaveDialogTitle
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            File.WriteAllText(dialog.FileName, _output.Text, Encoding.UTF8);
    }

    private void AnalyzeFiles(IEnumerable<string> paths)
    {
        var inputFiles = paths.ToArray();
        _lastFiles = inputFiles;
        var t = T;
        var reports = new List<string>();
        foreach (var path in inputFiles.Where(File.Exists))
        {
            try
            {
                reports.Add(UAssetAnalyzer.Analyze(path).ToText(t));
            }
            catch (Exception ex)
            {
                reports.Add($"{t.File}: {path}\r\n{t.AnalysisFailed}: {ex.Message}");
            }
        }

        _output.Text = reports.Count == 0
            ? t.NoReadableFiles
            : string.Join("\r\n\r\n" + new string('=', 78) + "\r\n\r\n", reports);
    }
}

internal static class UAssetAnalyzer
{
    private const uint PackageTag = 0x9E2A83C1;
    private static readonly Regex PathRegex = new(@"/(?:Game|Engine|Script)/[A-Za-z0-9_./-]+", RegexOptions.Compiled);
    private static readonly Regex EngineBranchRegex = new(@"\+\+UE[45]\+Release-[0-9.]+", RegexOptions.Compiled);

    public static IAnalysisReport Analyze(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".uasset" or ".umap" => AnalyzePackage(path),
            ".uproject" => AnalyzeDescriptor(path, "Unreal project descriptor"),
            ".uplugin" => AnalyzeDescriptor(path, "Unreal plugin descriptor"),
            ".uexp" or ".ubulk" => AnalyzeSidecar(path),
            _ => AnalyzeUnknown(path)
        };
    }

    private static UAssetReport AnalyzePackage(string path)
    {
        var info = new FileInfo(path);
        var bytes = File.ReadAllBytes(path);
        var report = new UAssetReport
        {
            FilePath = path,
            FileKind = Path.GetExtension(path).Equals(".umap", StringComparison.OrdinalIgnoreCase) ? "Unreal map package" : "Unreal asset package",
            FileSize = info.Length,
            LastWriteTime = info.LastWriteTime
        };

        if (bytes.Length < 32)
        {
            report.Errors.Add("The file is too small to look like a valid Unreal asset.");
            return report;
        }

        report.PackageTag = ReadUInt32(bytes, 0);
        report.IsValidPackage = report.PackageTag == PackageTag;
        if (!report.IsValidPackage)
        {
            report.Errors.Add("The standard Unreal Package tag 0x9E2A83C1 was not found.");
            return report;
        }

        report.LegacyFileVersion = ReadInt32(bytes, 4);
        report.FileVersionUE = ReadInt32(bytes, 8);
        report.FileVersionUE4 = ReadInt32(bytes, 12);
        report.FileVersionUE5 = ReadInt32(bytes, 16);
        report.LicenseeUE = ReadInt32(bytes, 20);
        report.CustomVersionCount = ReadInt32(bytes, 24);

        var text = Encoding.ASCII.GetString(bytes);
        report.EngineVersions.AddRange(FindEngineVersions(bytes, text));
        report.Paths = PathRegex.Matches(text)
            .Select(m => TrimPath(m.Value))
            .Where(p => p.Length > 7)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        report.PackagePath = report.Paths.FirstOrDefault(p => p.StartsWith("/Game/", StringComparison.OrdinalIgnoreCase)
            && Path.GetFileNameWithoutExtension(path).Equals(PathSegmentName(p), StringComparison.OrdinalIgnoreCase));

        report.SiblingFiles = FindSiblingFiles(path);
        report.MissingProjectDependencies = FindMissingProjectDependencies(path, report.Paths);
        return report;
    }

    private static DescriptorReport AnalyzeDescriptor(string path, string kind)
    {
        var info = new FileInfo(path);
        var report = new DescriptorReport
        {
            FilePath = path,
            FileKind = kind,
            FileSize = info.Length,
            LastWriteTime = info.LastWriteTime
        };

        var json = File.ReadAllText(path, Encoding.UTF8);
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });

        var root = document.RootElement;
        report.Fields.AddRange(ReadDescriptorFields(root));
        report.Modules.AddRange(ReadNamedObjects(root, "Modules", ["Name", "Type", "LoadingPhase", "WhitelistPlatforms", "PlatformAllowList"]));
        report.Plugins.AddRange(ReadNamedObjects(root, "Plugins", ["Name", "Enabled", "MarketplaceURL", "SupportedTargetPlatforms"]));
        report.TargetPlatforms.AddRange(ReadStringArray(root, "TargetPlatforms"));

        if (Path.GetExtension(path).Equals(".uproject", StringComparison.OrdinalIgnoreCase))
            report.ProjectFolders.AddRange(CheckProjectFolders(path));

        return report;
    }

    private static SidecarReport AnalyzeSidecar(string path)
    {
        var info = new FileInfo(path);
        var dir = Path.GetDirectoryName(path) ?? "";
        var name = Path.GetFileNameWithoutExtension(path);
        var related = new[] { ".uasset", ".umap", ".uexp", ".ubulk" }
            .Select(ext => Path.Combine(dir, name + ext))
            .Where(File.Exists)
            .Select(p => $"{Path.GetFileName(p)} ({new FileInfo(p).Length:N0} bytes)")
            .ToList();

        return new SidecarReport
        {
            FilePath = path,
            FileKind = "Unreal package sidecar data",
            FileSize = info.Length,
            LastWriteTime = info.LastWriteTime,
            RelatedFiles = related
        };
    }

    private static UnknownReport AnalyzeUnknown(string path)
    {
        var info = new FileInfo(path);
        return new UnknownReport
        {
            FilePath = path,
            FileKind = "Unsupported file",
            FileSize = info.Length,
            LastWriteTime = info.LastWriteTime
        };
    }

    private static List<EngineVersionInfo> FindEngineVersions(byte[] bytes, string text)
    {
        var versions = new List<EngineVersionInfo>();
        foreach (Match match in EngineBranchRegex.Matches(text))
        {
            var offset = match.Index - 14;
            if (offset < 0 || offset + 14 > bytes.Length)
                continue;

            var major = ReadUInt16(bytes, offset);
            var minor = ReadUInt16(bytes, offset + 2);
            var patch = ReadUInt16(bytes, offset + 4);
            var changelist = ReadUInt32(bytes, offset + 6);
            if (major is < 4 or > 5 || minor > 99 || patch > 99)
                continue;

            versions.Add(new EngineVersionInfo
            {
                LabelIndex = versions.Count,
                Offset = offset,
                Major = major,
                Minor = minor,
                Patch = patch,
                Changelist = changelist,
                Branch = match.Value
            });
        }

        return versions
            .GroupBy(v => $"{v.Version}|{v.Changelist}|{v.Branch}")
            .Select(g => g.First())
            .ToList();
    }

    private static List<string> FindSiblingFiles(string path)
    {
        var dir = Path.GetDirectoryName(path);
        var name = Path.GetFileNameWithoutExtension(path);
        if (dir is null)
            return new List<string>();

        var packageExt = Path.GetExtension(path).Equals(".umap", StringComparison.OrdinalIgnoreCase) ? ".umap" : ".uasset";
        return new[] { packageExt, ".uexp", ".ubulk" }
            .Select(ext => Path.Combine(dir, name + ext))
            .Where(File.Exists)
            .Select(p => $"{Path.GetFileName(p)} ({new FileInfo(p).Length:N0} bytes)")
            .ToList();
    }

    private static IEnumerable<KeyValuePair<string, string>> ReadDescriptorFields(JsonElement root)
    {
        foreach (var name in new[] { "FileVersion", "EngineAssociation", "EngineVersion", "Version", "VersionName", "FriendlyName", "Category", "Description", "CreatedBy", "CreatedByURL" })
        {
            if (root.TryGetProperty(name, out var value))
                yield return new KeyValuePair<string, string>(name, JsonValueToText(value));
        }
    }

    private static IEnumerable<string> ReadNamedObjects(JsonElement root, string arrayName, string[] fields)
    {
        if (!root.TryGetProperty(arrayName, out var array) || array.ValueKind != JsonValueKind.Array)
            yield break;

        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            var parts = new List<string>();
            foreach (var field in fields)
            {
                if (item.TryGetProperty(field, out var value))
                    parts.Add($"{field}={JsonValueToText(value)}");
            }

            if (parts.Count > 0)
                yield return string.Join(", ", parts);
        }
    }

    private static IEnumerable<string> ReadStringArray(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array)
            yield break;

        foreach (var item in array.EnumerateArray())
            yield return JsonValueToText(item);
    }

    private static IEnumerable<string> CheckProjectFolders(string path)
    {
        var root = Path.GetDirectoryName(path);
        if (root is null)
            yield break;

        foreach (var folder in new[] { "Content", "Config", "Plugins", "Source", "Intermediate", "Saved" })
        {
            var fullPath = Path.Combine(root, folder);
            yield return $"{folder}: {(Directory.Exists(fullPath) ? "found" : "not found")}";
        }
    }

    private static string JsonValueToText(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? "",
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.Array => string.Join(", ", value.EnumerateArray().Select(JsonValueToText)),
            JsonValueKind.Object => value.GetRawText(),
            _ => ""
        };
    }

    private static List<string> FindMissingProjectDependencies(string assetPath, IEnumerable<string> paths)
    {
        var contentRoot = FindContentRoot(assetPath);
        if (contentRoot is null)
            return new List<string>();

        var self = Normalize(assetPath);
        return paths
            .Where(p => p.StartsWith("/Game/", StringComparison.OrdinalIgnoreCase))
            .Select(p => new { UnrealPath = p, DiskPath = Path.Combine(contentRoot, p[6..].Replace('/', Path.DirectorySeparatorChar) + ".uasset") })
            .Where(x => !Normalize(x.DiskPath).Equals(self, StringComparison.OrdinalIgnoreCase))
            .Where(x => !File.Exists(x.DiskPath))
            .Select(x => x.UnrealPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? FindContentRoot(string path)
    {
        var full = Path.GetFullPath(path);
        var marker = $"{Path.DirectorySeparatorChar}Content{Path.DirectorySeparatorChar}";
        var index = full.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return null;

        return full[..(index + marker.Length - 1)];
    }

    private static string TrimPath(string path)
    {
        return path.TrimEnd('\0', '.', ',', ';', ':', '"', '\'', ')', ']', '}');
    }

    private static string PathSegmentName(string unrealPath)
    {
        var slash = unrealPath.LastIndexOf('/');
        return slash >= 0 ? unrealPath[(slash + 1)..] : unrealPath;
    }

    private static string Normalize(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static uint ReadUInt32(byte[] bytes, int offset)
    {
        return offset + 4 <= bytes.Length ? BitConverter.ToUInt32(bytes, offset) : 0;
    }

    private static int ReadInt32(byte[] bytes, int offset)
    {
        return offset + 4 <= bytes.Length ? BitConverter.ToInt32(bytes, offset) : 0;
    }

    private static ushort ReadUInt16(byte[] bytes, int offset)
    {
        return offset + 2 <= bytes.Length ? BitConverter.ToUInt16(bytes, offset) : (ushort)0;
    }
}

internal interface IAnalysisReport
{
    string ToText(UiText t);
}

internal sealed class UAssetReport : IAnalysisReport
{
    public string FilePath { get; set; } = "";
    public string FileKind { get; set; } = "";
    public long FileSize { get; set; }
    public DateTime LastWriteTime { get; set; }
    public uint PackageTag { get; set; }
    public bool IsValidPackage { get; set; }
    public int LegacyFileVersion { get; set; }
    public int FileVersionUE { get; set; }
    public int FileVersionUE4 { get; set; }
    public int FileVersionUE5 { get; set; }
    public int LicenseeUE { get; set; }
    public int CustomVersionCount { get; set; }
    public string? PackagePath { get; set; }
    public List<EngineVersionInfo> EngineVersions { get; } = new();
    public List<string> Paths { get; set; } = new();
    public List<string> SiblingFiles { get; set; } = new();
    public List<string> MissingProjectDependencies { get; set; } = new();
    public List<string> Errors { get; } = new();

    public string ToText(UiText t)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{t.File}: {FilePath}");
        if (!string.IsNullOrWhiteSpace(FileKind))
            sb.AppendLine($"Type: {FileKind}");
        sb.AppendLine($"{t.Size}: {FileSize:N0} bytes");
        sb.AppendLine($"{t.Modified}: {LastWriteTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Package Tag: 0x{PackageTag:X8}" + (IsValidPackage ? $" ({t.Valid})" : $" ({t.Invalid})"));

        if (Errors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"{t.Errors}:");
            foreach (var error in Errors)
                sb.AppendLine($"- {error}");
            return sb.ToString();
        }

        sb.AppendLine();
        sb.AppendLine($"{t.VersionInfo}:");
        foreach (var version in EngineVersions)
            sb.AppendLine($"- {t.EngineLabel(version)}: UE {version.Version}, CL {version.Changelist}, {version.Branch}");

        if (EngineVersions.Count == 0)
            sb.AppendLine($"- {t.NoPlaintextEngineVersion}");

        sb.AppendLine($"- LegacyFileVersion: {LegacyFileVersion}");
        sb.AppendLine($"- FileVersionUE: {FileVersionUE}");
        sb.AppendLine($"- FileVersionUE4: {FileVersionUE4}");
        sb.AppendLine($"- FileVersionUE5: {FileVersionUE5}");
        sb.AppendLine($"- LicenseeUE: {LicenseeUE}");
        sb.AppendLine($"- {t.CustomVersionCount}: {CustomVersionCount}");

        if (!string.IsNullOrWhiteSpace(PackagePath))
        {
            sb.AppendLine();
            sb.AppendLine($"{t.AssetPath}: {PackagePath}");
            sb.AppendLine($"{t.SuggestedDiskPath}: Content{PackagePath[5..].Replace('/', Path.DirectorySeparatorChar)}{Path.GetExtension(FilePath)}");
        }

        sb.AppendLine();
        sb.AppendLine($"{t.SameNameFiles}:");
        if (SiblingFiles.Count == 0)
            sb.AppendLine($"- {t.NoneFound}");
        else
            foreach (var sibling in SiblingFiles)
                sb.AppendLine($"- {sibling}");

        sb.AppendLine();
        sb.AppendLine($"{t.Findings}:");
        foreach (var finding in BuildFindings(t))
            sb.AppendLine($"- {finding}");

        if (MissingProjectDependencies.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"{t.MissingDependencies}:");
            foreach (var dep in MissingProjectDependencies.Take(80))
                sb.AppendLine($"- {dep}");
            if (MissingProjectDependencies.Count > 80)
                sb.AppendLine($"- {t.AndMore(MissingProjectDependencies.Count - 80)}");
        }

        if (Paths.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"{t.ResourceReferences}:");
            foreach (var path in Paths.Take(120))
                sb.AppendLine($"- {path}");
            if (Paths.Count > 120)
                sb.AppendLine($"- {t.AndMore(Paths.Count - 120)}");
        }

        return sb.ToString();
    }

    private IEnumerable<string> BuildFindings(UiText t)
    {
        if (EngineVersions.Count > 0)
        {
            var saved = EngineVersions[0];
            yield return t.OpenWithEngine(saved);
        }
        else if (FileVersionUE5 > 0)
        {
            yield return t.NoPlaintextButUE5;
        }

        if (MissingProjectDependencies.Count > 0)
            yield return t.MissingDependenciesFinding;

        if (!SiblingFiles.Any(s => s.EndsWith(".uexp", StringComparison.OrdinalIgnoreCase)) &&
            !SiblingFiles.Any(s => s.EndsWith(".ubulk", StringComparison.OrdinalIgnoreCase)))
            yield return t.NoSidecarFinding;

        if (LicenseeUE != 0)
            yield return t.CustomEngineFinding(LicenseeUE);
    }
}

internal sealed class DescriptorReport : IAnalysisReport
{
    public string FilePath { get; set; } = "";
    public string FileKind { get; set; } = "";
    public long FileSize { get; set; }
    public DateTime LastWriteTime { get; set; }
    public List<KeyValuePair<string, string>> Fields { get; } = new();
    public List<string> Modules { get; } = new();
    public List<string> Plugins { get; } = new();
    public List<string> TargetPlatforms { get; } = new();
    public List<string> ProjectFolders { get; } = new();

    public string ToText(UiText t)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{t.File}: {FilePath}");
        sb.AppendLine($"Type: {FileKind}");
        sb.AppendLine($"{t.Size}: {FileSize:N0} bytes");
        sb.AppendLine($"{t.Modified}: {LastWriteTime:yyyy-MM-dd HH:mm:ss}");

        sb.AppendLine();
        sb.AppendLine("Descriptor Info:");
        if (Fields.Count == 0)
            sb.AppendLine($"- {t.NoneFound}");
        else
            foreach (var field in Fields)
                sb.AppendLine($"- {field.Key}: {field.Value}");

        AppendList(sb, "Modules", Modules, t);
        AppendList(sb, "Plugins", Plugins, t);
        AppendList(sb, "Target Platforms", TargetPlatforms, t);
        AppendList(sb, "Project Folders", ProjectFolders, t);

        sb.AppendLine();
        sb.AppendLine($"{t.Findings}:");
        foreach (var finding in BuildFindings())
            sb.AppendLine($"- {finding}");

        return sb.ToString();
    }

    private IEnumerable<string> BuildFindings()
    {
        var extension = Path.GetExtension(FilePath);
        var hasEngine = Fields.Any(f => f.Key.Equals("EngineAssociation", StringComparison.OrdinalIgnoreCase) || f.Key.Equals("EngineVersion", StringComparison.OrdinalIgnoreCase));
        if (extension.Equals(".uproject", StringComparison.OrdinalIgnoreCase) && !hasEngine)
            yield return "No EngineAssociation field was found. Unreal may ask you to select an engine version when opening the project.";

        if (Modules.Count > 0)
            yield return "This descriptor declares code modules. Opening it may require a matching C++ toolchain or generated project files.";

        if (Plugins.Count > 0)
            yield return "This descriptor references plugins. Missing plugins can stop the project or plugin from loading correctly.";

        if (ProjectFolders.Count > 0 && ProjectFolders.Any(f => f.StartsWith("Content: not found", StringComparison.OrdinalIgnoreCase)))
            yield return "The project Content folder was not found next to the .uproject file.";
    }

    private static void AppendList(StringBuilder sb, string title, List<string> items, UiText t)
    {
        sb.AppendLine();
        sb.AppendLine($"{title}:");
        if (items.Count == 0)
            sb.AppendLine($"- {t.NoneFound}");
        else
            foreach (var item in items)
                sb.AppendLine($"- {item}");
    }
}

internal sealed class SidecarReport : IAnalysisReport
{
    public string FilePath { get; set; } = "";
    public string FileKind { get; set; } = "";
    public long FileSize { get; set; }
    public DateTime LastWriteTime { get; set; }
    public List<string> RelatedFiles { get; set; } = new();

    public string ToText(UiText t)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{t.File}: {FilePath}");
        sb.AppendLine($"Type: {FileKind}");
        sb.AppendLine($"{t.Size}: {FileSize:N0} bytes");
        sb.AppendLine($"{t.Modified}: {LastWriteTime:yyyy-MM-dd HH:mm:ss}");

        sb.AppendLine();
        sb.AppendLine("Related package files:");
        if (RelatedFiles.Count == 0)
            sb.AppendLine($"- {t.NoneFound}");
        else
            foreach (var file in RelatedFiles)
                sb.AppendLine($"- {file}");

        sb.AppendLine();
        sb.AppendLine($"{t.Findings}:");
        sb.AppendLine("- .uexp and .ubulk files are package sidecar data. They usually need the same-name .uasset or .umap file to be useful.");

        return sb.ToString();
    }
}

internal sealed class UnknownReport : IAnalysisReport
{
    public string FilePath { get; set; } = "";
    public string FileKind { get; set; } = "";
    public long FileSize { get; set; }
    public DateTime LastWriteTime { get; set; }

    public string ToText(UiText t)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{t.File}: {FilePath}");
        sb.AppendLine($"Type: {FileKind}");
        sb.AppendLine($"{t.Size}: {FileSize:N0} bytes");
        sb.AppendLine($"{t.Modified}: {LastWriteTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine($"{t.Findings}:");
        sb.AppendLine("- This extension is not supported yet. Supported: .uasset, .umap, .uproject, .uplugin, .uexp, .ubulk.");
        return sb.ToString();
    }
}

internal sealed class EngineVersionInfo
{
    public int LabelIndex { get; set; }
    public int Offset { get; set; }
    public int Major { get; set; }
    public int Minor { get; set; }
    public int Patch { get; set; }
    public uint Changelist { get; set; }
    public string Branch { get; set; } = "";
    public string Version => $"{Major}.{Minor}.{Patch}";
}

internal enum AppLanguage
{
    English,
    ChineseSimplified,
    Japanese
}

internal sealed record LanguageItem(AppLanguage Language, string Name)
{
    public override string ToString() => Name;
}

internal sealed class UiText
{
    public required string WindowTitle { get; init; }
    public required string Header { get; init; }
    public required string Hint { get; init; }
    public required string WaitingText { get; init; }
    public required string OpenButton { get; init; }
    public required string CopyButton { get; init; }
    public required string SaveButton { get; init; }
    public required string OpenDialogTitle { get; init; }
    public required string SaveDialogTitle { get; init; }
    public required string NoReadableFiles { get; init; }
    public required string File { get; init; }
    public required string AnalysisFailed { get; init; }
    public required string Size { get; init; }
    public required string Modified { get; init; }
    public required string Valid { get; init; }
    public required string Invalid { get; init; }
    public required string Errors { get; init; }
    public required string VersionInfo { get; init; }
    public required string SavedVersion { get; init; }
    public required string CompatibleVersion { get; init; }
    public required string VersionRecord { get; init; }
    public required string NoPlaintextEngineVersion { get; init; }
    public required string CustomVersionCount { get; init; }
    public required string AssetPath { get; init; }
    public required string SuggestedDiskPath { get; init; }
    public required string SameNameFiles { get; init; }
    public required string NoneFound { get; init; }
    public required string Findings { get; init; }
    public required string MissingDependencies { get; init; }
    public required string ResourceReferences { get; init; }
    public required Func<EngineVersionInfo, string> OpenWithEngine { get; init; }
    public required string NoPlaintextButUE5 { get; init; }
    public required string MissingDependenciesFinding { get; init; }
    public required string NoSidecarFinding { get; init; }
    public required Func<int, string> CustomEngineFinding { get; init; }
    public required Func<int, string> AndMore { get; init; }

    public string EngineLabel(EngineVersionInfo version) => version.LabelIndex switch
    {
        0 => SavedVersion,
        1 => CompatibleVersion,
        _ => $"{VersionRecord} {version.LabelIndex + 1}"
    };

    public static UiText For(AppLanguage language) => language switch
    {
        AppLanguage.ChineseSimplified => ChineseSimplified,
        AppLanguage.Japanese => Japanese,
        _ => English
    };

    private static readonly UiText English = new()
    {
        WindowTitle = "Unreal File Inspector",
        Header = "UE File Version Inspector",
        Hint = "Drop Unreal files into this window, or click Open. The tool is read-only.",
        WaitingText = "Waiting for files...\r\n\r\nSupported: .uasset, .umap, .uproject, .uplugin, .uexp, .ubulk\r\nTip: keep same-name .uexp / .ubulk files in the same folder when available.",
        OpenButton = "Open UE file",
        CopyButton = "Copy Report",
        SaveButton = "Save Report",
        OpenDialogTitle = "Open Unreal files",
        SaveDialogTitle = "Save diagnostic report",
        NoReadableFiles = "No readable files were found.",
        File = "File",
        AnalysisFailed = "Analysis failed",
        Size = "Size",
        Modified = "Modified",
        Valid = "valid",
        Invalid = "invalid",
        Errors = "Errors",
        VersionInfo = "Version Info",
        SavedVersion = "Saved version",
        CompatibleVersion = "Compatible version",
        VersionRecord = "Version record",
        NoPlaintextEngineVersion = "No plaintext SavedByEngineVersion / CompatibleWithEngineVersion was found.",
        CustomVersionCount = "CustomVersion count",
        AssetPath = "Asset path",
        SuggestedDiskPath = "Suggested disk path",
        SameNameFiles = "Same-name files",
        NoneFound = "None found",
        Findings = "Findings",
        MissingDependencies = "Likely missing dependencies under the current Content folder",
        ResourceReferences = "Resource references found in the file",
        OpenWithEngine = saved => $"Open with UE {saved.Major}.{saved.Minor}.x when possible. Best match: UE {saved.Version}.",
        NoPlaintextButUE5 = "No plaintext engine branch was found, but this appears to be a UE5 serialized asset.",
        MissingDependenciesFinding = "Some /Game dependencies are missing under the current Content folder. The editor may show errors or fail to load the asset.",
        NoSidecarFinding = "No same-name .uexp / .ubulk files were found. This is normal for some assets, but large assets often need sidecar data files.",
        CustomEngineFinding = value => $"LicenseeUE is {value}; this may come from a customized engine build.",
        AndMore = count => $"... and {count} more"
    };

    private static readonly UiText ChineseSimplified = new()
    {
        WindowTitle = "UAsset 版本诊断工具",
        Header = "UE .uasset 版本诊断工具",
        Hint = "把 .uasset 文件拖到此窗口，或点击打开。工具只读取文件，不会修改素材。",
        WaitingText = "等待文件...\r\n\r\n支持：.uasset\r\n提示：如果有同名 .uexp / .ubulk，请放在同一文件夹中一起检查。",
        OpenButton = "打开 .uasset",
        CopyButton = "复制报告",
        SaveButton = "保存报告",
        OpenDialogTitle = "打开 .uasset 文件",
        SaveDialogTitle = "保存诊断报告",
        NoReadableFiles = "没有找到可读取的文件。",
        File = "文件",
        AnalysisFailed = "分析失败",
        Size = "大小",
        Modified = "修改时间",
        Valid = "有效",
        Invalid = "无效",
        Errors = "错误",
        VersionInfo = "版本信息",
        SavedVersion = "保存版本",
        CompatibleVersion = "兼容版本",
        VersionRecord = "版本记录",
        NoPlaintextEngineVersion = "没有找到明文 SavedByEngineVersion / CompatibleWithEngineVersion。",
        CustomVersionCount = "CustomVersion 数量",
        AssetPath = "资源路径",
        SuggestedDiskPath = "建议磁盘路径",
        SameNameFiles = "同名文件",
        NoneFound = "未找到",
        Findings = "诊断",
        MissingDependencies = "当前 Content 目录下疑似缺失的依赖",
        ResourceReferences = "文件中发现的资源引用",
        OpenWithEngine = saved => $"建议尽量使用 UE {saved.Major}.{saved.Minor}.x 打开。最匹配版本：UE {saved.Version}。",
        NoPlaintextButUE5 = "没有找到明文引擎分支字符串，但这看起来是 UE5 序列化资产。",
        MissingDependenciesFinding = "检测到部分 /Game 依赖在当前 Content 目录下不存在，编辑器可能报错或加载失败。",
        NoSidecarFinding = "没有找到同名 .uexp / .ubulk。对部分资产这是正常的，但大型资源通常需要外部数据文件。",
        CustomEngineFinding = value => $"LicenseeUE 为 {value}，可能来自定制版引擎。",
        AndMore = count => $"... 还有 {count} 项"
    };

    private static readonly UiText Japanese = new()
    {
        WindowTitle = "UAsset バージョン診断ツール",
        Header = "UE .uasset バージョン診断ツール",
        Hint = ".uasset ファイルをこのウィンドウにドロップするか、開くボタンを押してください。ファイルは読み取り専用で解析します。",
        WaitingText = "ファイル待機中...\r\n\r\n対応形式: .uasset\r\nヒント: 同名の .uexp / .ubulk がある場合は、同じフォルダーに置いてください。",
        OpenButton = ".uasset を開く",
        CopyButton = "レポートをコピー",
        SaveButton = "レポートを保存",
        OpenDialogTitle = ".uasset ファイルを開く",
        SaveDialogTitle = "診断レポートを保存",
        NoReadableFiles = "読み取れるファイルが見つかりませんでした。",
        File = "ファイル",
        AnalysisFailed = "解析に失敗しました",
        Size = "サイズ",
        Modified = "更新日時",
        Valid = "有効",
        Invalid = "無効",
        Errors = "エラー",
        VersionInfo = "バージョン情報",
        SavedVersion = "保存バージョン",
        CompatibleVersion = "互換バージョン",
        VersionRecord = "バージョン記録",
        NoPlaintextEngineVersion = "SavedByEngineVersion / CompatibleWithEngineVersion の平文情報は見つかりませんでした。",
        CustomVersionCount = "CustomVersion 数",
        AssetPath = "アセットパス",
        SuggestedDiskPath = "推奨ディスクパス",
        SameNameFiles = "同名ファイル",
        NoneFound = "見つかりません",
        Findings = "診断",
        MissingDependencies = "現在の Content フォルダー内で不足している可能性がある依存関係",
        ResourceReferences = "ファイル内で検出されたリソース参照",
        OpenWithEngine = saved => $"可能であれば UE {saved.Major}.{saved.Minor}.x で開いてください。最も近いバージョン: UE {saved.Version}。",
        NoPlaintextButUE5 = "平文のエンジンブランチは見つかりませんでしたが、UE5 でシリアライズされたアセットのようです。",
        MissingDependenciesFinding = "一部の /Game 依存関係が現在の Content フォルダー内にありません。エディタでエラー表示や読み込み失敗が起きる可能性があります。",
        NoSidecarFinding = "同名の .uexp / .ubulk は見つかりませんでした。これは一部のアセットでは正常ですが、大きなアセットでは外部データファイルが必要な場合があります。",
        CustomEngineFinding = value => $"LicenseeUE は {value} です。カスタム版エンジン由来の可能性があります。",
        AndMore = count => $"... ほか {count} 件"
    };
}
