using BepInEx;
using BepInEx.Unity.IL2CPP;

namespace GakumasVR.Diagnostic;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("gakumas.exe")]
public sealed class DiagnosticPlugin : BasePlugin
{
    public const string PluginGuid = "io.github.gakumasvr.diagnostic";
    public const string PluginName = "GakumasVR Diagnostic";
    public const string PluginVersion = "0.1.0";

    public override void Load()
    {
        string outputDirectory = Path.Combine(Paths.ConfigPath, "GakumasVR");
        Directory.CreateDirectory(outputDirectory);

        DiagnosticBehaviour behaviour = AddComponent<DiagnosticBehaviour>();
        behaviour.Initialize(Log, Path.Combine(outputDirectory, "diagnostics.jsonl"));
        Log.LogInfo($"{PluginName} {PluginVersion} loaded. Diagnostics: {outputDirectory}");
    }
}

