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
        var saveDir = Path.Combine(root, "Save");
        var dataDir = Path.Combine(root, "Data");
        var target = Path.Combine(dataDir, "UAssetVersionInspector.exe");
        if (!File.Exists(target))
        {
            MessageBox.Show(
                "Missing Data\\UAssetVersionInspector.exe.",
                "UAsset Version Inspector",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        Directory.CreateDirectory(saveDir);

        var startInfo = new ProcessStartInfo
        {
            FileName = target,
            WorkingDirectory = dataDir,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = false,
            Arguments = string.Join(" ", args.Select(Quote))
        };
        try
        {
            var process = Process.Start(startInfo);
            if (process == null)
                throw new InvalidOperationException("Failed to start Data\\UAssetVersionInspector.exe.");

            if (process.WaitForExit(1500) && process.ExitCode != 0)
            {
                var message = process.StandardError.ReadToEnd();
                if (string.IsNullOrWhiteSpace(message))
                    message = process.StandardOutput.ReadToEnd();
                if (string.IsNullOrWhiteSpace(message))
                    message = "The application exited immediately.";

                string fallbackError;
                if (!TryStartWithDotnet(dataDir, args, out fallbackError))
                {
                    var logPath = Path.Combine(saveDir, "launcher-error.txt");
                    File.WriteAllText(logPath, message + Environment.NewLine + Environment.NewLine + fallbackError);
                    MessageBox.Show(
                        message + Environment.NewLine + Environment.NewLine + fallbackError + Environment.NewLine + Environment.NewLine + "Details were saved to Save\\launcher-error.txt.",
                        "UAsset Version Inspector",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(saveDir, "launcher-error.txt");
            File.WriteAllText(logPath, ex.ToString());
            MessageBox.Show(
                ex.Message + Environment.NewLine + Environment.NewLine + "Details were saved to Save\\launcher-error.txt.",
                "UAsset Version Inspector",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static bool TryStartWithDotnet(string dataDir, string[] args, out string error)
    {
        error = "";
        var dll = Path.Combine(dataDir, "UAssetVersionInspector.dll");
        if (!File.Exists(dll))
        {
            error = "Missing Data\\UAssetVersionInspector.dll.";
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = dataDir,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = false,
                Arguments = Quote(dll) + " " + string.Join(" ", args.Select(Quote))
            };

            var process = Process.Start(startInfo);
            if (process == null)
            {
                error = "Failed to start dotnet fallback.";
                return false;
            }

            if (process.WaitForExit(1500) && process.ExitCode != 0)
            {
                error = process.StandardError.ReadToEnd();
                if (string.IsNullOrWhiteSpace(error))
                    error = process.StandardOutput.ReadToEnd();
                if (string.IsNullOrWhiteSpace(error))
                    error = "dotnet fallback exited immediately.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.ToString();
            return false;
        }
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
