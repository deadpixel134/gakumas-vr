using System.Diagnostics;
using GakumasVR.Installer;
using GakumasVR.Management;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        InstallerText.Initialize(InstallerLanguage.English);
        (string Name, Action Run)[] tests =
        {
            ("Installer automatically fills a unique detected game folder", AutoFill),
            ("Installer leaves multiple candidates for the user to select", MultipleCandidates),
            ("Background discovery preserves a manually entered path", () => PreserveManualPath(false)),
            ("Startup discovery preserves a path entered before the window is shown", () => PreserveManualPath(true))
        };
        int failures = 0;
        foreach (var test in tests)
        {
            try { test.Run(); Console.WriteLine($"PASS {test.Name}"); }
            catch (Exception exception) { failures++; Console.WriteLine($"FAIL {test.Name}: {exception}"); }
        }
        if (failures == 0 && args.Length > 0) Render(args[0]);
        if (failures == 0 && args.Contains("--probe-real")) ProbeRealInstallation();
        Console.WriteLine($"Executed {tests.Length} installer tests; failures: {failures}");
        return failures == 0 ? 0 : 1;
    }

    private static InstallerForm Open(Func<GameInstallationDiscovery> discover)
    {
        InstallerForm form = new(discover) { Opacity = 0 };
        form.Show();
        Application.DoEvents();
        return form;
    }

    private static void CompleteDiscovery(InstallerForm form) => WaitUntil(() => form.Controls.Find("DetectGameFolder", true).Single().Enabled);

    private static ComboBox Folder(InstallerForm form) => (ComboBox)form.Controls.Find("GameRoot", true).Single();

    private static void AutoFill()
    {
        using GameFixture game = new();
        using InstallerForm form = Open(() => new(new[] { game.Path }, game.Path));
        CompleteDiscovery(form);
        Require(Folder(form).Text == game.Path, "Unique game path was not filled");
        Require(Folder(form).Items.Count == 1, "Detected candidate is missing");
    }

    private static void MultipleCandidates()
    {
        using GameFixture first = new();
        using GameFixture second = new();
        using InstallerForm form = Open(() => new(new[] { first.Path, second.Path }, null));
        CompleteDiscovery(form);
        Require(Folder(form).Text == string.Empty, "An ambiguous path was selected automatically");
        Require(Folder(form).Items.Count == 2, "Candidate list is incomplete");
    }

    private static void PreserveManualPath(bool beforeShown)
    {
        using GameFixture detected = new();
        using GameFixture manual = new();
        TaskCompletionSource<GameInstallationDiscovery> gate = new();
        using InstallerForm form = new(() => gate.Task.GetAwaiter().GetResult()) { Opacity = 0 };
        if (beforeShown) Folder(form).Text = manual.Path;
        form.Show();
        Application.DoEvents();
        if (!beforeShown) Folder(form).Text = manual.Path;
        gate.SetResult(new(new[] { detected.Path }, detected.Path));
        CompleteDiscovery(form);
        Require(Folder(form).Text == manual.Path, "Discovery overwrote manual input");
    }

    private static void Render(string directory)
    {
        Directory.CreateDirectory(directory);
        using GameFixture game = new();
        foreach (InstallerLanguage language in Enum.GetValues<InstallerLanguage>())
        {
            InstallerText.Initialize(language);
            using InstallerForm form = Open(() => new(new[] { game.Path }, game.Path));
            CompleteDiscovery(form);
            using Bitmap image = new(form.Width, form.Height);
            form.DrawToBitmap(image, new Rectangle(Point.Empty, form.Size));
            image.Save(System.IO.Path.Combine(directory, $"installer-path-{language}.png"));
        }
    }

    private static void ProbeRealInstallation()
    {
        // Use unrelated application/working directories so only DMM registration can locate the game.
        GameInstallationDiscovery found = GameInstallationLocator.Discover(
            System.IO.Path.GetTempPath(), System.IO.Path.GetTempPath(),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        Require(found.SelectedPath is not null, "DMM did not provide a unique valid installation");
        using InstallerForm form = Open(() => found);
        CompleteDiscovery(form);
        Require(Folder(form).Text == found.SelectedPath, "Installer did not load the real DMM path");
        Console.WriteLine($"PASS real DMM registration: {found.SelectedPath}");
    }

    private static void WaitUntil(Func<bool> condition)
    {
        Stopwatch timeout = Stopwatch.StartNew();
        while (!condition())
        {
            if (timeout.Elapsed > TimeSpan.FromSeconds(5)) throw new TimeoutException("Discovery did not complete");
            Application.DoEvents();
            Thread.Sleep(5);
        }
        Application.DoEvents();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class GameFixture : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GakumasVR.Installer.Tests", Guid.NewGuid().ToString("N"));
        public GameFixture()
        {
            Directory.CreateDirectory(Path);
            foreach (string name in new[] { "gakumas.exe", "GameAssembly.dll", "UnityPlayer.dll" })
                File.WriteAllText(System.IO.Path.Combine(Path, name), "fixture");
        }
        public void Dispose()
        {
            string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GakumasVR.Installer.Tests")) + System.IO.Path.DirectorySeparatorChar;
            string target = System.IO.Path.GetFullPath(Path);
            if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unsafe fixture path");
            Directory.Delete(target, recursive: true);
        }
    }
}
