using System.Text;
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
                .Select(path => UAssetAnalyzer.Analyze(path).ToText());
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

    public MainForm(string[] args)
    {
        Text = "UAsset Version Inspector";
        Width = 1040;
        Height = 760;
        MinimumSize = new Size(760, 520);
        AllowDrop = true;
        StartPosition = FormStartPosition.CenterScreen;

        var header = new Label
        {
            Text = "UE .uasset Version Inspector",
            Dock = DockStyle.Top,
            Height = 42,
            Font = new Font("Microsoft YaHei UI", 15, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 0, 0)
        };

        var hint = new Label
        {
            Text = "Drop .uasset files into this window, or click Open. The tool is read-only.",
            Dock = DockStyle.Top,
            Height = 30,
            Font = new Font("Microsoft YaHei UI", 9),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(15, 0, 0, 0)
        };

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 46,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(12, 7, 12, 6)
        };

        _openButton.Text = "Open .uasset";
        _openButton.Width = 130;
        _openButton.Height = 30;
        _openButton.Click += (_, _) => OpenFiles();

        _copyButton.Text = "Copy Report";
        _copyButton.Width = 100;
        _copyButton.Height = 30;
        _copyButton.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_output.Text))
                Clipboard.SetText(_output.Text);
        };

        _saveButton.Text = "Save Report";
        _saveButton.Width = 100;
        _saveButton.Height = 30;
        _saveButton.Click += (_, _) => SaveReport();

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
        _output.Text = "Waiting for files...\r\n\r\nSupported: .uasset\r\nTip: keep same-name .uexp / .ubulk files in the same folder when available.";

        Controls.Add(_output);
        Controls.Add(toolbar);
        Controls.Add(hint);
        Controls.Add(header);

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

        if (args.Length > 0)
            AnalyzeFiles(args);
    }

    private void OpenFiles()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Unreal Asset (*.uasset)|*.uasset|All files (*.*)|*.*",
            Multiselect = true,
            Title = "Open .uasset files"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            AnalyzeFiles(dialog.FileNames);
    }

    private void SaveReport()
    {
        if (string.IsNullOrWhiteSpace(_output.Text))
            return;

        using var dialog = new SaveFileDialog
        {
            Filter = "Text report (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = $"uasset-report-{DateTime.Now:yyyyMMdd-HHmmss}.txt",
            Title = "Save diagnostic report"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            File.WriteAllText(dialog.FileName, _output.Text, Encoding.UTF8);
    }

    private void AnalyzeFiles(IEnumerable<string> paths)
    {
        var reports = new List<string>();
        foreach (var path in paths.Where(File.Exists))
        {
            try
            {
                reports.Add(UAssetAnalyzer.Analyze(path).ToText());
            }
            catch (Exception ex)
            {
                reports.Add($"File: {path}\r\nAnalysis failed: {ex.Message}");
            }
        }

        _output.Text = reports.Count == 0
            ? "No readable files were found."
            : string.Join("\r\n\r\n" + new string('=', 78) + "\r\n\r\n", reports);
    }
}

internal static class UAssetAnalyzer
{
    private const uint PackageTag = 0x9E2A83C1;
    private static readonly Regex PathRegex = new(@"/(?:Game|Engine|Script)/[A-Za-z0-9_./-]+", RegexOptions.Compiled);
    private static readonly Regex EngineBranchRegex = new(@"\+\+UE[45]\+Release-[0-9.]+", RegexOptions.Compiled);

    public static UAssetReport Analyze(string path)
    {
        var info = new FileInfo(path);
        var bytes = File.ReadAllBytes(path);
        var report = new UAssetReport
        {
            FilePath = path,
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
        report.Findings.AddRange(BuildFindings(report));
        return report;
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
                Label = versions.Count == 0 ? "Saved version" : versions.Count == 1 ? "Compatible version" : $"Version record {versions.Count + 1}",
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

        return new[] { ".uasset", ".uexp", ".ubulk" }
            .Select(ext => Path.Combine(dir, name + ext))
            .Where(File.Exists)
            .Select(p => $"{Path.GetFileName(p)} ({new FileInfo(p).Length:N0} bytes)")
            .ToList();
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

    private static IEnumerable<string> BuildFindings(UAssetReport report)
    {
        if (report.EngineVersions.Count > 0)
        {
            var saved = report.EngineVersions[0];
            yield return $"Open with UE {saved.Major}.{saved.Minor}.x when possible. Best match: UE {saved.Version}.";
        }
        else if (report.FileVersionUE5 > 0)
        {
            yield return "No plaintext engine branch was found, but this appears to be a UE5 serialized asset.";
        }

        if (report.MissingProjectDependencies.Count > 0)
            yield return "Some /Game dependencies are missing under the current Content folder. The editor may show errors or fail to load the asset.";

        if (!report.SiblingFiles.Any(s => s.EndsWith(".uexp", StringComparison.OrdinalIgnoreCase)) &&
            !report.SiblingFiles.Any(s => s.EndsWith(".ubulk", StringComparison.OrdinalIgnoreCase)))
            yield return "No same-name .uexp / .ubulk files were found. This is normal for some assets, but large assets often need sidecar data files.";

        if (report.LicenseeUE != 0)
            yield return $"LicenseeUE is {report.LicenseeUE}; this may come from a customized engine build.";
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

internal sealed class UAssetReport
{
    public string FilePath { get; set; } = "";
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
    public List<string> Findings { get; } = new();
    public List<string> Errors { get; } = new();

    public string ToText()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"File: {FilePath}");
        sb.AppendLine($"Size: {FileSize:N0} bytes");
        sb.AppendLine($"Modified: {LastWriteTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Package Tag: 0x{PackageTag:X8}" + (IsValidPackage ? " (valid)" : " (invalid)"));

        if (Errors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Errors:");
            foreach (var error in Errors)
                sb.AppendLine($"- {error}");
            return sb.ToString();
        }

        sb.AppendLine();
        sb.AppendLine("Version Info:");
        foreach (var version in EngineVersions)
            sb.AppendLine($"- {version.Label}: UE {version.Version}, CL {version.Changelist}, {version.Branch}");

        if (EngineVersions.Count == 0)
            sb.AppendLine("- No plaintext SavedByEngineVersion / CompatibleWithEngineVersion was found.");

        sb.AppendLine($"- LegacyFileVersion: {LegacyFileVersion}");
        sb.AppendLine($"- FileVersionUE: {FileVersionUE}");
        sb.AppendLine($"- FileVersionUE4: {FileVersionUE4}");
        sb.AppendLine($"- FileVersionUE5: {FileVersionUE5}");
        sb.AppendLine($"- LicenseeUE: {LicenseeUE}");
        sb.AppendLine($"- CustomVersion count: {CustomVersionCount}");

        if (!string.IsNullOrWhiteSpace(PackagePath))
        {
            sb.AppendLine();
            sb.AppendLine($"Asset path: {PackagePath}");
            sb.AppendLine($"Suggested disk path: Content{PackagePath[5..].Replace('/', Path.DirectorySeparatorChar)}.uasset");
        }

        sb.AppendLine();
        sb.AppendLine("Same-name files:");
        if (SiblingFiles.Count == 0)
            sb.AppendLine("- None found");
        else
            foreach (var sibling in SiblingFiles)
                sb.AppendLine($"- {sibling}");

        sb.AppendLine();
        sb.AppendLine("Findings:");
        foreach (var finding in Findings)
            sb.AppendLine($"- {finding}");

        if (MissingProjectDependencies.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Likely missing dependencies under the current Content folder:");
            foreach (var dep in MissingProjectDependencies.Take(80))
                sb.AppendLine($"- {dep}");
            if (MissingProjectDependencies.Count > 80)
                sb.AppendLine($"- ... and {MissingProjectDependencies.Count - 80} more");
        }

        if (Paths.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Resource references found in the file:");
            foreach (var path in Paths.Take(120))
                sb.AppendLine($"- {path}");
            if (Paths.Count > 120)
                sb.AppendLine($"- ... and {Paths.Count - 120} more");
        }

        return sb.ToString();
    }
}

internal sealed class EngineVersionInfo
{
    public string Label { get; set; } = "";
    public int Offset { get; set; }
    public int Major { get; set; }
    public int Minor { get; set; }
    public int Patch { get; set; }
    public uint Changelist { get; set; }
    public string Branch { get; set; } = "";
    public string Version => $"{Major}.{Minor}.{Patch}";
}
