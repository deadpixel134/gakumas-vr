namespace GakumasVR.Configurator;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        UiText.Initialize();
        if (args.Contains("--verify-localization", StringComparer.OrdinalIgnoreCase))
        {
            return;
        }
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
