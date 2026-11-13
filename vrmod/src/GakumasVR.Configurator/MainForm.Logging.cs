using System.Diagnostics;
using GakumasVR.Core;

namespace GakumasVR.Configurator;

internal sealed partial class MainForm
{
    private readonly TabPage _logsTab = new() { Tag = "TabLogs", AutoScroll = true };
    private readonly CheckBox _saveLogs = TaggedCheckBox("LogEnabled");
    private readonly Label _logSummary = new() { AutoSize = true, MaximumSize = new Size(560, 0) };

    private void BuildLogsTab()
    {
        TableLayoutPanel grid = Grid();
        AddRow(grid, "LogSaving", _saveLogs);
        AddRow(grid, "LogPolicy", new Label { Tag = "LogPolicyHint", AutoSize = true, MaximumSize = new Size(560, 0) });
        AddRow(grid, "LogStored", _logSummary);
        FlowLayoutPanel buttons = new() { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
        buttons.Controls.Add(TaggedButton("LogRefresh", (_, _) => Run("StatusLoaded", RefreshLogSummary)));
        buttons.Controls.Add(TaggedButton("LogOpen", (_, _) => Run("StatusLoaded", () =>
        {
            RequireLogGameRoot();
            string directory = LogStorage.LogDirectory(_gameRoot.Text);
            Directory.CreateDirectory(directory);
            Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true });
        })));
        buttons.Controls.Add(TaggedButton("LogDelete", (_, _) => Run("LogDeleteDone", () =>
        {
            RequireLogGameRoot();
            SettingsStore.EnsureGameStopped();
            if (MessageBox.Show(this, UiText.Get("LogDeleteConfirm"), UiText.Get("TabLogs"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            LogCleanupResult result = LogStorage.Clear(_gameRoot.Text);
            RefreshLogSummary();
            if (result.Failed != 0) throw new IOException(UiText.Format("LogDeleteFailed", result.Failed));
        })));
        AddRow(grid, "LogManage", buttons);
        _logsTab.Controls.Add(grid);
    }

    private void RequireLogGameRoot()
    {
        if (!File.Exists(Path.Combine(_gameRoot.Text, "gakumas.exe")))
            throw new DirectoryNotFoundException(UiText.Get("GameExeMissing"));
    }

    private void LoadLogging()
    {
        RequireLogGameRoot();
        if (!UpdateService.IsGameRunning()) LogStorage.Initialize(_gameRoot.Text);
        _saveLogs.Checked = LogStorage.IsEnabled(_gameRoot.Text);
        RefreshLogSummary();
    }

    private void RefreshLogSummary()
    {
        RequireLogGameRoot();
        IReadOnlyList<string> files = LogStorage.Files(_gameRoot.Text);
        long bytes = files.Sum(path => new FileInfo(path).Length);
        _logSummary.Text = UiText.Format("LogSummary", files.Count, bytes / (1024d * 1024d));
    }
}
