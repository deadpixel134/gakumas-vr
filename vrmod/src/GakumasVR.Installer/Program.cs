namespace GakumasVR.Installer;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--initialize-logging")
        {
            try
            {
                if (!File.Exists(Path.Combine(args[1], "gakumas.exe"))) throw new DirectoryNotFoundException();
                if (System.Diagnostics.Process.GetProcessesByName("gakumas").Length != 0) throw new IOException();
                Environment.ExitCode = GakumasVR.Core.LogStorage.Initialize(args[1]).Failed == 0 ? 0 : 2;
            }
            catch { Environment.ExitCode = 2; }
            return;
        }
        if (AutoUpdateRunner.IsRequested(args))
        {
            Environment.ExitCode = AutoUpdateRunner.Run(args);
            return;
        }
        InstallerText.Initialize();
        if (args.Contains("--verify-localization", StringComparer.OrdinalIgnoreCase))
        {
            return;
        }
        if (args.Contains("--verify-package", StringComparer.OrdinalIgnoreCase))
        {
            if (GakumasVR.Management.InstallationEngine.FindPackageRoot() is null)
            {
                Environment.ExitCode = 2;
            }
            return;
        }
        ApplicationConfiguration.Initialize();
        Application.Run(new InstallerForm());
    }
}
