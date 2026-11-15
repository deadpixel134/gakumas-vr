using System.Security.Cryptography;
using System.Text.Json;
using GakumasVR.Management;

List<(string Name, Action Test)> tests = new()
{
    ("DMM installation discovery reads validated game paths without changing configuration", DmmDiscovery),
    ("Game discovery handles missing corrupt locked and oversized DMM configuration", DmmInvalidConfiguration),
    ("Multiple DMM installations require selection while duplicate paths are merged", DmmAmbiguousPaths),
    ("Installer-local and working-directory games take priority over DMM candidates", DmmLocalPriority),
    ("Release notes select the GUI language and fall back to English", ReleaseNotesLanguages),
    ("Release notes are plain text bounded to five short items", ReleaseNotesBounds),
    ("Release notes accept missing bodies and ignore code and comments", ReleaseNotesMissing),
    ("Installer migrates logging once and preserves later choices", InstallerLogging),
    ("Install and uninstall preserve settings and prior collisions", InstallAndUninstall),
    ("Upgrade rollback restores the previous managed version", UpgradeRollback),
    ("Localify paths are rejected and existing files survive", LocalifyProtection),
    ("Modified installed files are preserved with state", ModifiedFileProtection),
    ("Payload preflight rejects corruption before writing", PayloadPreflightProtection),
    ("Product preflight requires clean-install dependencies", ProductDependencyProtection),
    ("Release update version and checksum policy", ReleaseUpdateVersionAndChecksumPolicy)
};
if (args.Length == 1)
{
    string actualPackage = Path.GetFullPath(args[0]);
    tests.Add(("Actual distribution package installs and uninstalls", () =>
        ActualDistributionPackage(actualPackage)));
}

int failures = 0;
foreach ((string name, Action test) in tests)
{
    try
    {
        test();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.WriteLine($"FAIL {name}: {exception.Message}");
    }
}
Console.WriteLine($"Executed {tests.Count} management tests; failures: {failures}");
return failures == 0 ? 0 : 1;

static (string Outside, string Roaming, string Config) DiscoveryPaths(Fixture fixture)
{
    string outside = Path.Combine(Path.GetDirectoryName(fixture.GameRoot)!, "outside");
    string roaming = Path.Combine(Path.GetDirectoryName(fixture.GameRoot)!, "roaming");
    string config = Path.Combine(roaming, "dmmgameplayer5", "dmmgame.cnf");
    Directory.CreateDirectory(outside);
    Directory.CreateDirectory(Path.GetDirectoryName(config)!);
    return (outside, roaming, config);
}

static void WriteDmmConfiguration(string config, params (string Path, bool Installed)[] games) =>
    File.WriteAllText(config, JsonSerializer.Serialize(new
    {
        contents = games.Select(game => new { detail = new { path = game.Path, installed = game.Installed } })
    }));

static void DmmDiscovery()
{
    using Fixture fixture = new();
    var paths = DiscoveryPaths(fixture);
    WriteDmmConfiguration(paths.Config, (fixture.GameRoot, true), (paths.Outside, true), ("relative/path", true));
    byte[] before = File.ReadAllBytes(paths.Config);
    GameInstallationDiscovery found = GameInstallationLocator.Discover(paths.Outside, paths.Outside, paths.Roaming);
    Equal(fixture.GameRoot, found.SelectedPath!);
    Equal(1, found.Candidates.Count);
    True(before.SequenceEqual(File.ReadAllBytes(paths.Config)));
    WriteDmmConfiguration(paths.Config, (fixture.GameRoot, false));
    True(GameInstallationLocator.Discover(paths.Outside, paths.Outside, paths.Roaming).SelectedPath is null);
}

static void DmmInvalidConfiguration()
{
    using Fixture fixture = new();
    var paths = DiscoveryPaths(fixture);
    foreach (string? content in new[] { null, "invalid-json", "{\"contents\":42}", new string('x', 1024 * 1024 + 1) })
    {
        if (content is not null) File.WriteAllText(paths.Config, content);
        Equal(0, GameInstallationLocator.Discover(paths.Outside, paths.Outside, paths.Roaming).Candidates.Count);
    }
    WriteDmmConfiguration(paths.Config, (fixture.GameRoot, true));
    using FileStream locked = new(paths.Config, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    Equal(0, GameInstallationLocator.Discover(paths.Outside, paths.Outside, paths.Roaming).Candidates.Count);
}

static void DmmAmbiguousPaths()
{
    using Fixture first = new();
    using Fixture second = new();
    var paths = DiscoveryPaths(first);
    WriteDmmConfiguration(paths.Config, (first.GameRoot, true), (first.GameRoot + Path.DirectorySeparatorChar, true));
    Equal(1, GameInstallationLocator.Discover(paths.Outside, paths.Outside, paths.Roaming).Candidates.Count);
    WriteDmmConfiguration(paths.Config, (first.GameRoot, true), (second.GameRoot, true));
    GameInstallationDiscovery found = GameInstallationLocator.Discover(paths.Outside, paths.Outside, paths.Roaming);
    Equal(2, found.Candidates.Count);
    True(found.SelectedPath is null);
}

static void DmmLocalPriority()
{
    using Fixture local = new();
    using Fixture registered = new();
    var paths = DiscoveryPaths(local);
    WriteDmmConfiguration(paths.Config, (registered.GameRoot, true));
    string application = Path.Combine(local.GameRoot, "vrmod", "tools");
    Directory.CreateDirectory(application);
    GameInstallationDiscovery found = GameInstallationLocator.Discover(application, paths.Outside, paths.Roaming);
    Equal(local.GameRoot, found.SelectedPath!);
    Equal(2, found.Candidates.Count);
    Equal(local.GameRoot, GameInstallationLocator.Discover(paths.Outside, local.GameRoot, paths.Roaming).SelectedPath!);
}
static void ReleaseNotesLanguages()
{
    const string notes = "## 한국어\n- **한국어 변경**\n## English\n- English change\n## 日本語\n- 日本語の変更";
    Equal("• 한국어 변경", ReleaseNotesSummary.Create(notes, GakumasVR.Core.VrUiLanguage.Korean));
    Equal("• English change", ReleaseNotesSummary.Create(notes, GakumasVR.Core.VrUiLanguage.English));
    Equal("• 日本語の変更", ReleaseNotesSummary.Create(notes, GakumasVR.Core.VrUiLanguage.Japanese));
    Equal("• English change", ReleaseNotesSummary.Create("## English\n- English change", GakumasVR.Core.VrUiLanguage.Japanese));
    Equal("• 한국어 변경", ReleaseNotesSummary.Create("## 한국어\n- 한국어 변경", GakumasVR.Core.VrUiLanguage.English));
}

static void ReleaseNotesBounds()
{
    string notes = "# Release\n- **Fixed** [panel](https://example.com) &amp; `input`\n" +
        string.Join("\n", Enumerable.Range(0, 10).Select(index => $"- {index}: {new string('x', 500)}"));
    string summary = ReleaseNotesSummary.Create(notes, GakumasVR.Core.VrUiLanguage.English);
    string[] items = summary.Split(Environment.NewLine + Environment.NewLine);
    Equal(ReleaseNotesSummary.MaximumItems, items.Length);
    Equal("• Fixed panel & input", items[0]);
    True(items.All(item => item.Length <= ReleaseNotesSummary.MaximumItemLength + 2));
    False(summary.Contains("https://"));
}

static void ReleaseNotesMissing()
{
    Equal(string.Empty, ReleaseNotesSummary.Create(null, GakumasVR.Core.VrUiLanguage.Korean));
    Equal(string.Empty, ReleaseNotesSummary.Create("  ", GakumasVR.Core.VrUiLanguage.English));
    string notes = "<!-- internal\nnotes -->\n```\n## 日本語\nnot a change\n```\n- <b>Visible</b>\n- Visible\n![image](https://example.com/image.png)";
    Equal("• Visible", ReleaseNotesSummary.Create(notes, GakumasVR.Core.VrUiLanguage.English));
}
static void ReleaseUpdateVersionAndChecksumPolicy()
{
    True(ReleaseUpdatePolicy.IsNewer("0.165.0", "v0.166.0"));
    False(ReleaseUpdatePolicy.IsNewer("0.166.0.0", "v0.166.0"));
    False(ReleaseUpdatePolicy.IsNewer("0.167.0", "v0.166.0"));
    Equal(
        new string('A', 64),
        ReleaseUpdatePolicy.ParseSha256(
            $"{new string('a', 64)}  GakumasVR-v0.166.0.zip",
            "GakumasVR-v0.166.0.zip"));
    Equal(
        new string('B', 64),
        ReleaseUpdatePolicy.ParseSha256(
            $"sha256:{new string('b', 64)}",
            "GakumasVR-v0.166.0.zip"));
}

static void InstallAndUninstall()
{
    using Fixture fixture = new();
    string originalProxy = fixture.WriteGameFile("winhttp.dll", "original-proxy");
    string package = fixture.CreatePackage("1.0.0", "runtime-one");
    InstallationEngine engine = new();
    InstallationResult installed = engine.Install(fixture.GameRoot, package);
    Equal("1.0.0", installed.Version);
    Equal(LocalifyStatus.Absent, installed.Localify);
    Equal("runtime-one", File.ReadAllText(Path.Combine(fixture.GameRoot, "vrmod/runtime/mod.dll")));

    InstallationResult removed = engine.Uninstall(fixture.GameRoot);
    False(removed.RestoredPreviousVersion);
    Equal(originalProxy, File.ReadAllText(Path.Combine(fixture.GameRoot, "winhttp.dll")));
    True(File.Exists(Path.Combine(fixture.GameRoot, "vrmod/config/settings.json")));
    False(File.Exists(Path.Combine(fixture.GameRoot, "vrmod/install-state.json")));
}

static void UpgradeRollback()
{
    using Fixture fixture = new();
    InstallationEngine engine = new();
    string first = fixture.CreatePackage("1.0.0", "runtime-one");
    engine.Install(fixture.GameRoot, first);
    string second = fixture.CreatePackage("2.0.0", "runtime-two");
    engine.Install(fixture.GameRoot, second);

    InstallationStatus upgraded = engine.Inspect(fixture.GameRoot, second);
    Equal("2.0.0", upgraded.InstalledVersion!);
    True(upgraded.HasPreviousVersion);
    InstallationResult rollback = engine.Uninstall(fixture.GameRoot);
    True(rollback.RestoredPreviousVersion);
    Equal("1.0.0", rollback.RestoredVersion!);
    Equal("runtime-one", File.ReadAllText(Path.Combine(fixture.GameRoot, "vrmod/runtime/mod.dll")));
    Equal("1.0.0", engine.Inspect(fixture.GameRoot, first).InstalledVersion!);
}

static void LocalifyProtection()
{
    using Fixture fixture = new();
    string protectedValue = fixture.WriteGameFile("version.dll", "localify-proxy");
    Directory.CreateDirectory(Path.Combine(fixture.GameRoot, "gakumas-local"));
    fixture.WriteGameFile("gakumas-local/config.json", "{}");
    Equal(LocalifyStatus.Installed, InstallationEngine.DetectLocalify(fixture.GameRoot));
    string package = fixture.CreatePackage("1.0.0", "runtime", includeProtectedPath: true);
    InstallationEngine engine = new();
    try
    {
        engine.Install(fixture.GameRoot, package);
        throw new InvalidOperationException("Protected Localify path was accepted.");
    }
    catch (InstallationException exception) when (exception.Code == "ProtectedLocalifyPath")
    {
    }
    Equal(protectedValue, File.ReadAllText(Path.Combine(fixture.GameRoot, "version.dll")));
}

static void ModifiedFileProtection()
{
    using Fixture fixture = new();
    InstallationEngine engine = new();
    string package = fixture.CreatePackage("1.0.0", "runtime-one");
    engine.Install(fixture.GameRoot, package);
    File.WriteAllText(Path.Combine(fixture.GameRoot, "vrmod/runtime/mod.dll"), "user-change");
    InstallationResult result = engine.Uninstall(fixture.GameRoot);
    True(result.Warnings.Any(warning => warning == "Modified:vrmod/runtime/mod.dll"));
    Equal("user-change", File.ReadAllText(Path.Combine(fixture.GameRoot, "vrmod/runtime/mod.dll")));
    True(File.Exists(Path.Combine(fixture.GameRoot, "vrmod/install-state.json")));
}

static void PayloadPreflightProtection()
{
    using Fixture fixture = new();
    string package = fixture.CreatePackage("1.0.0", "runtime-one");
    File.WriteAllText(Path.Combine(package, "payload/vrmod/runtime/mod.dll"), "corrupted-after-manifest");
    InstallationEngine engine = new();
    try
    {
        engine.Install(fixture.GameRoot, package);
        throw new InvalidOperationException("Corrupted package was accepted.");
    }
    catch (InstallationException exception) when (exception.Code == "PackageHashMismatch")
    {
    }
    False(File.Exists(Path.Combine(fixture.GameRoot, "winhttp.dll")));
    False(File.Exists(Path.Combine(fixture.GameRoot, "vrmod/install-state.json")));
}

static void ProductDependencyProtection()
{
    using Fixture fixture = new();
    string package = fixture.CreatePackage(
        "1.0.0",
        "runtime-one",
        loader: "winhttp-doorstop");
    InstallationEngine engine = new();
    try
    {
        engine.Install(fixture.GameRoot, package);
        throw new InvalidOperationException("Incomplete product package was accepted.");
    }
    catch (InstallationException exception) when (exception.Code == "PackageRequiredFileMissing")
    {
    }
    False(File.Exists(Path.Combine(fixture.GameRoot, "winhttp.dll")));
    False(File.Exists(Path.Combine(fixture.GameRoot, "vrmod/install-state.json")));
}

static void ActualDistributionPackage(string package)
{
    using Fixture fixture = new();
    string localifyProxy = fixture.WriteGameFile("version.dll", "localify-proxy");
    fixture.WriteGameFile("gakumas-local/config.json", "{}");
    InstallationEngine engine = new();
    InstallationResult installed = engine.Install(fixture.GameRoot, package);
    Equal(JsonDocument.Parse(File.ReadAllText(Path.Combine(package, "package-manifest.json"))).RootElement.GetProperty("version").GetString()!, installed.Version);
    Equal(LocalifyStatus.Installed, installed.Localify);
    Equal(localifyProxy, File.ReadAllText(Path.Combine(fixture.GameRoot, "version.dll")));
    True(File.Exists(Path.Combine(
        fixture.GameRoot,
        "vrmod/tools/GakumasVR.Configurator.exe")));
    True(File.Exists(Path.Combine(
        fixture.GameRoot,
        "vrmod/runtime/openxr_loader.dll")));
    True(File.Exists(Path.Combine(
        fixture.GameRoot,
        "BepInEx/core/dobby.dll")));
    InstallationResult removed = engine.Uninstall(fixture.GameRoot);
    False(removed.RestoredPreviousVersion);
    Equal(0, removed.Warnings.Count);
    Equal(localifyProxy, File.ReadAllText(Path.Combine(fixture.GameRoot, "version.dll")));
    True(File.Exists(Path.Combine(fixture.GameRoot, "vrmod/config/settings.json")));
    False(File.Exists(Path.Combine(fixture.GameRoot, "BepInEx/core/dobby.dll")));

    using Fixture existingDobbyFixture = new();
    string existingDobby = existingDobbyFixture.WriteGameFile(
        "BepInEx/core/dobby.dll",
        "existing-localify-dobby");
    engine.Install(existingDobbyFixture.GameRoot, package);
    Equal(
        existingDobby,
        File.ReadAllText(Path.Combine(existingDobbyFixture.GameRoot, "BepInEx/core/dobby.dll")));
    InstallationResult existingDobbyRemoved = engine.Uninstall(existingDobbyFixture.GameRoot);
    Equal(0, existingDobbyRemoved.Warnings.Count);
    Equal(
        existingDobby,
        File.ReadAllText(Path.Combine(existingDobbyFixture.GameRoot, "BepInEx/core/dobby.dll")));
}

static void Equal<T>(T expected, T actual) where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}

static void True(bool value)
{
    if (!value) throw new InvalidOperationException("Expected true, got false.");
}

static void False(bool value)
{
    if (value) throw new InvalidOperationException("Expected false, got true.");
}

static void InstallerLogging()
{
    using Fixture fixture = new();
    fixture.WriteGameFile("vrmod/logs/runtime-bootstrap.jsonl", "old log");
    fixture.WriteGameFile("vrmod/config/logging-enabled.txt", "on");
    string package = fixture.CreatePackage("0.178.1", "runtime-one");
    InstallationEngine engine = new();
    engine.Install(fixture.GameRoot, package);
    False(GakumasVR.Core.LogStorage.IsEnabled(fixture.GameRoot));
    False(File.Exists(Path.Combine(fixture.GameRoot, "vrmod/logs/runtime-bootstrap.jsonl")));
    GakumasVR.Core.LogStorage.SetEnabled(fixture.GameRoot, true);
    fixture.WriteGameFile("vrmod/logs/runtime-bootstrap.jsonl", "new log");
    engine.Install(fixture.GameRoot, package);
    True(GakumasVR.Core.LogStorage.IsEnabled(fixture.GameRoot));
    True(File.Exists(Path.Combine(fixture.GameRoot, "vrmod/logs/runtime-bootstrap.jsonl")));
}


internal sealed class Fixture : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "GakumasVR.Management.Tests",
        Guid.NewGuid().ToString("N"));

    public Fixture()
    {
        GameRoot = Path.Combine(_root, "game");
        Directory.CreateDirectory(GameRoot);
        foreach (string required in new[] { "gakumas.exe", "GameAssembly.dll", "UnityPlayer.dll" })
        {
            File.WriteAllText(Path.Combine(GameRoot, required), required);
        }
    }

    public string GameRoot { get; }

    public string WriteGameFile(string relative, string content)
    {
        string path = Path.Combine(GameRoot, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return content;
    }

    public string CreatePackage(
        string version,
        string runtime,
        bool includeProtectedPath = false,
        string loader = "test")
    {
        string package = Path.Combine(_root, "packages", version + Guid.NewGuid().ToString("N"));
        string payload = Path.Combine(package, "payload");
        Directory.CreateDirectory(payload);
        List<PackageFile> files = new();
        AddPayload(payload, files, "winhttp.dll", "managed-proxy");
        AddPayload(payload, files, "vrmod/runtime/mod.dll", runtime);
        AddPayload(
            payload,
            files,
            "vrmod/config/settings.json",
            "{}",
            preserveExisting: true,
            preserveOnUninstall: true);
        if (includeProtectedPath)
        {
            AddPayload(payload, files, "version.dll", "must-not-install");
        }
        PackageManifest manifest = new()
        {
            SchemaVersion = 1,
            Version = version,
            Loader = loader,
            LocalifyPolicy = "preserve",
            Files = files
        };
        File.WriteAllText(
            Path.Combine(package, "package-manifest.json"),
            JsonSerializer.Serialize(manifest, JsonOptions));
        return package;
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static void AddPayload(
        string payload,
        List<PackageFile> files,
        string relative,
        string content,
        bool preserveExisting = false,
        bool preserveOnUninstall = false)
    {
        string path = Path.Combine(payload, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        files.Add(new PackageFile
        {
            Path = relative,
            Sha256 = Hash(path),
            PreserveExisting = preserveExisting,
            PreserveOnUninstall = preserveOnUninstall
        });
    }

    private static string Hash(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
