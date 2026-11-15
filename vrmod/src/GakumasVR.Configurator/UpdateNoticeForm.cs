using System.Diagnostics;
using GakumasVR.Core;
using GakumasVR.Management;

namespace GakumasVR.Configurator;

internal sealed class UpdateNoticeForm : Form
{
    public UpdateNoticeForm(AvailableUpdate update, bool gameRunning, VrUiLanguage language)
    {
        string TextFor(string key) => VrUiText.Get(language, key);
        string summary = ReleaseNotesSummary.Create(update.ReleaseNotes, language);
        Text = VrUiText.Format(language, "UpdateNoticeTitle", update.Version);
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(720, 460);
        MinimumSize = new Size(620, 420);
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 5
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);
        layout.Controls.Add(new Label
        {
            AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 12),
            Text = VrUiText.Format(language, "UpdateVersionChange", UpdateService.CurrentVersion, update.Version)
        }, 0, 0);
        layout.Controls.Add(new Label
        {
            AutoSize = true, Dock = DockStyle.Fill, Text = TextFor("UpdateNotesHeading"),
            Margin = new Padding(0, 0, 0, 8)
        }, 0, 1);
        layout.Controls.Add(new TextBox
        {
            Name = "ReleaseNotes", Dock = DockStyle.Fill, Multiline = true, ReadOnly = true,
            ScrollBars = ScrollBars.Vertical, WordWrap = true, BackColor = SystemColors.Window,
            Text = summary.Length > 0 ? summary : TextFor("UpdateNotesEmpty"), TabStop = false
        }, 0, 2);
        layout.Controls.Add(new Label
        {
            Name = "UpdateNoticeHint", AutoSize = true, Dock = DockStyle.Fill,
            MaximumSize = new Size(640, 0), Margin = new Padding(0, 12, 0, 12),
            Text = TextFor(gameRunning ? "UpdateNotesGameRunning" : "UpdateNotesInstallHint")
        }, 0, 3);

        FlowLayoutPanel actions = new()
        {
            Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = true, Margin = Padding.Empty
        };
        Button install = new()
        {
            Name = "InstallUpdate", Text = TextFor("UpdateInstallNow"), AutoSize = true,
            Padding = new Padding(12, 6, 12, 6), Enabled = !gameRunning, DialogResult = DialogResult.OK
        };
        Button later = new()
        {
            Text = TextFor("UpdateLater"), AutoSize = true,
            Padding = new Padding(12, 6, 12, 6), DialogResult = DialogResult.Cancel
        };
        Button fullNotes = new()
        {
            Text = TextFor("UpdateNotesFull"), AutoSize = true, Padding = new Padding(12, 6, 12, 6)
        };
        fullNotes.Click += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo(update.ReleaseUrl) { UseShellExecute = true }); }
            catch (Exception)
            {
                MessageBox.Show(this, TextFor("UpdateNotesOpenFailed"), Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        };
        actions.Controls.Add(install);
        actions.Controls.Add(later);
        actions.Controls.Add(fullNotes);
        layout.Controls.Add(actions, 0, 4);
        AcceptButton = gameRunning ? later : install;
        CancelButton = later;
    }
}
