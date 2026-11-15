using System.Text.Json;

namespace GakumasVR.Management;

public sealed record GameInstallationDiscovery(IReadOnlyList<string> Candidates, string? SelectedPath);

public static class GameInstallationLocator
{
    private const int MaximumConfigCharacters = 1024 * 1024;

    public static GameInstallationDiscovery Discover() => Discover(
        AppContext.BaseDirectory, Environment.CurrentDirectory,
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

    public static GameInstallationDiscovery Discover(string applicationDirectory, string workingDirectory, string roamingAppData)
    {
        List<string> candidates = new();
        string? preferred = FindNearby(applicationDirectory);
        AddCandidate(candidates, preferred);
        string? working = ValidatedPath(workingDirectory);
        AddCandidate(candidates, working);
        preferred ??= working;

        if (!string.IsNullOrWhiteSpace(roamingAppData))
        {
            try
            {
                string config = Path.Combine(roamingAppData, "dmmgameplayer5", "dmmgame.cnf");
                foreach (string path in ReadDmmPaths(config)) AddCandidate(candidates, path);
            }
            catch (Exception exception) when (IsPathReadFailure(exception))
            {
                // Discovery is best effort. Never read authentication files or write DMM configuration.
            }
        }

        return new(candidates.ToArray(), preferred ?? (candidates.Count == 1 ? candidates[0] : null));
    }

    private static string? FindNearby(string directory)
    {
        try
        {
            DirectoryInfo? current = new(directory);
            for (int depth = 0; depth < 7 && current is not null; depth++, current = current.Parent)
                if (ValidatedPath(current.FullName) is string path) return path;
        }
        catch (Exception exception) when (IsPathReadFailure(exception)) { }
        return null;
    }

    private static void AddCandidate(List<string> candidates, string? value)
    {
        string? path = ValidatedPath(value);
        if (path is not null && !candidates.Contains(path, StringComparer.OrdinalIgnoreCase)) candidates.Add(path);
    }

    private static string? ValidatedPath(string? path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path)) return null;
            string full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
            return InstallationEngine.IsGameRoot(full) ? full : null;
        }
        catch (Exception exception) when (IsPathReadFailure(exception)) { return null; }
    }

    private static IReadOnlyList<string> ReadDmmPaths(string config)
    {
        List<string> paths = new();
        using FileStream stream = new(config, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        if (stream.Length > MaximumConfigCharacters) return paths;
        using StreamReader reader = new(stream, detectEncodingFromByteOrderMarks: true);
        char[] buffer = new char[MaximumConfigCharacters + 1];
        int length = reader.ReadBlock(buffer, 0, buffer.Length);
        if (length > MaximumConfigCharacters) return paths;
        using JsonDocument document = JsonDocument.Parse(buffer.AsMemory(0, length), new JsonDocumentOptions
        {
            AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip, MaxDepth = 16
        });
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty("contents", out JsonElement contents) ||
            contents.ValueKind != JsonValueKind.Array) return paths;
        foreach (JsonElement entry in contents.EnumerateArray().Take(2048))
        {
            if (entry.ValueKind != JsonValueKind.Object ||
                !entry.TryGetProperty("detail", out JsonElement detail) || detail.ValueKind != JsonValueKind.Object ||
                !detail.TryGetProperty("installed", out JsonElement installed) || installed.ValueKind != JsonValueKind.True ||
                !detail.TryGetProperty("path", out JsonElement path) || path.ValueKind != JsonValueKind.String) continue;
            if (path.GetString() is string value) paths.Add(value);
        }
        return paths;
    }

    private static bool IsPathReadFailure(Exception exception) => exception is
        IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or
        System.Security.SecurityException or JsonException;
}
