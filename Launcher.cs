using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

internal static class Launcher
{
    [STAThread]
    private static void Main(string[] args)
    {
        var root = AppDomain.CurrentDomain.BaseDirectory;
        var target = Path.Combine(root, "Data", "UAssetVersionInspector.exe");
        if (!File.Exists(target))
        {
            MessageBox.Show(
                "Missing Data\\UAssetVersionInspector.exe.",
                "UAsset Version Inspector",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        Directory.CreateDirectory(Path.Combine(root, "Save"));

        var startInfo = new ProcessStartInfo
        {
            FileName = target,
            WorkingDirectory = Path.GetDirectoryName(target),
            UseShellExecute = true,
            Arguments = string.Join(" ", args.Select(Quote))
        };
        Process.Start(startInfo);
    }

    private static string Quote(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "\"\"";
        return value.Contains(" ") || value.Contains("\"")
            ? "\"" + value.Replace("\"", "\\\"") + "\""
            : value;
    }
}
