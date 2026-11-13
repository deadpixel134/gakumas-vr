using System.Text.RegularExpressions;

namespace GakumasVR.Core;

// Installation-local preference, intentionally separate from imported/exported VR presets.
public static class LogStorage
{
    private static readonly object Sync = new();
    private const string PolicyFile = "logging-policy-v1.txt";
    private const string EnabledFile = "logging-enabled.txt";
    private static readonly Regex Capture = new(@"^v[0-9]+(?:\.[0-9]+)*-[A-Za-z0-9_-]+\.(bmp|png)$", RegexOptions.IgnoreCase);

    public static string LogDirectory(string gameRoot) => SafePath(gameRoot, "logs");
    private static string Config(string root) => SafePath(root, "config");

    private static string SafePath(string root, string child)
    {
        string path = Path.GetFullPath(Path.Combine(root, "vrmod", child));
        for (DirectoryInfo? d = new(path); d is not null; d = d.Parent)
            if (d.Exists && (d.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Log management does not follow directory links.");
        return path;
    }

    private static void CheckFile(string path)
    {
        if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Log management does not follow file links.");
    }

    public static bool IsEnabled(string root)
    {
        try
        {
            string config = Config(root);
            string marker = Path.Combine(config, PolicyFile);
            string enabled = Path.Combine(config, EnabledFile);
            CheckFile(marker); CheckFile(enabled);
            return File.Exists(marker) && File.ReadAllText(marker).Trim() == "complete" &&
                File.Exists(enabled) && File.ReadAllText(enabled).Trim() == "on";
        }
        catch { return false; }
    }

    // Missing marker means an older installation, whose logging was unconditional.
    // Pending cleanup retries without resetting a subsequent user preference.
    public static LogCleanupResult Initialize(string root)
    {
        lock (Sync)
        {
            string config = Config(root);
            Directory.CreateDirectory(config);
            string marker = Path.Combine(config, PolicyFile);
            CheckFile(marker);
            if (File.Exists(marker) && File.ReadAllText(marker).Trim() == "complete") return new();
            if (!File.Exists(marker))
            {
                WriteAtomic(Path.Combine(config, EnabledFile), "off");
                WriteAtomic(marker, "pending");
            }
            LogCleanupResult result = Clear(root);
            if (result.Failed == 0) WriteAtomic(marker, "complete");
            return result;
        }
    }

    public static void SetEnabled(string root, bool enabled)
    {
        lock (Sync)
        {
            LogCleanupResult result = Initialize(root);
            if (result.Failed != 0) throw new IOException("Close the game and retry log cleanup.");
            WriteAtomic(Path.Combine(Config(root), EnabledFile), enabled ? "on" : "off");
        }
    }

    private static void WriteAtomic(string path, string value)
    {
        CheckFile(path);
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, value);
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    public static IReadOnlyList<string> Files(string root)
    {
        string directory = LogDirectory(root);
        if (!Directory.Exists(directory)) return Array.Empty<string>();
        return Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly)
            .Where(path => Path.GetFileName(path).Equals("runtime-bootstrap.jsonl", StringComparison.OrdinalIgnoreCase) ||
                Capture.IsMatch(Path.GetFileName(path)))
            .Where(path => (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0).ToArray();
    }

    public static LogCleanupResult Clear(string root)
    {
        lock (Sync)
        {
            LogCleanupResult result = new();
            foreach (string path in Files(root))
            {
                try
                {
                    CheckFile(path);
                    long length = new FileInfo(path).Length;
                    File.Delete(path);
                    result.Deleted++; result.Bytes += length;
                }
                catch (IOException) { result.Failed++; }
                catch (UnauthorizedAccessException) { result.Failed++; }
            }
            return result;
        }
    }
}

public sealed class LogCleanupResult
{
    public int Deleted { get; internal set; }
    public int Failed { get; internal set; }
    public long Bytes { get; internal set; }
}
