using System.Diagnostics;
using GakumasVR.Core;

namespace GakumasVR.Configurator;

internal sealed class MainForm : Form
{
    private readonly StartupResult _startup;
    private readonly UpdateService _updateService = new();
    private readonly TextBox _gameRoot = new() { Dock = DockStyle.Fill };
    private readonly CheckBox _runtimeEnabled = TaggedCheckBox("RuntimeEnabled");
    private readonly NumericUpDown _eyeScale = Number(0.50m, 2.00m, 0.01m, 3);
    private readonly Label _eyeScaleWarning = new()
    {
        AutoSize = true,
        MaximumSize = new Size(560, 0),
        Margin = new Padding(3, 2, 3, 6)
    };
    private readonly NumericUpDown _worldEyeScale = Number(0m, 0.50m, 0.005m, 3);
    private readonly CheckBox _liveSixDof = TaggedCheckBox("LiveSixDofEnabled");
    private readonly ComboBox _vfx = ChoiceCombo();
    private readonly CheckBox _manualPostProcessing = TaggedCheckBox("EffectEnabled");
    private readonly CheckBox _manualVlBloom = TaggedCheckBox("EffectEnabled");
    private readonly NumericUpDown _manualVlBloomIntensity = Number(0m, 300m, 5m, 0);
    private readonly NumericUpDown _manualVlBloomDiffusion = Number(1m, 10m, 1m, 0);
    private readonly CheckBox _manualVlDof = TaggedCheckBox("EffectEnabled");
    private readonly CheckBox _manualVlTextureBlur = TaggedCheckBox("EffectEnabled");
    private readonly CheckBox _manualVlStarStreak = TaggedCheckBox("EffectEnabled");
    private readonly CheckBox _manualVlFlare = TaggedCheckBox("EffectEnabled");
    private readonly ComboBox _panelHand = ChoiceCombo();
    private readonly ComboBox _pointerHand = ChoiceCombo();
    private readonly CheckBox _startEnabled = TaggedCheckBox("StartEnabled");
    private readonly CheckBox _viewerFacing = TaggedCheckBox("ViewerFacing");
    private readonly NumericUpDown _offsetX = Number(-0.50m, 0.50m, 0.005m, 3);
    private readonly NumericUpDown _offsetY = Number(-0.50m, 0.50m, 0.005m, 3);
    private readonly NumericUpDown _offsetZ = Number(-0.50m, 0.50m, 0.005m, 3);
    private readonly NumericUpDown _maximumWidth = Number(0.10m, 1.00m, 0.01m, 2);
    private readonly NumericUpDown _maximumHeight = Number(0.10m, 1.00m, 0.01m, 2);
    private readonly NumericUpDown _rotationPitch = Number(-180m, 180m, 1m, 1);
    private readonly NumericUpDown _rotationYaw = Number(-180m, 180m, 1m, 1);
    private readonly NumericUpDown _rotationRoll = Number(-180m, 180m, 1m, 1);
    private readonly NumericUpDown _hysteresis = Number(0m, 500m, 10m, 0);
    private readonly ComboBox _toggle = ChoiceCombo();
    private readonly ComboBox _primary = ChoiceCombo();
    private readonly ComboBox _back = ChoiceCombo();
    private readonly CheckBox _trigger = TaggedCheckBox("TriggerEnabled");
    private readonly CheckBox _locomotion = TaggedCheckBox("LocomotionEnabled");
    private readonly ComboBox _locomotionHand = ChoiceCombo();
    private readonly NumericUpDown _locomotionSpeed = Number(0.10m, 5.00m, 0.10m, 2);
    private readonly NumericUpDown _viewTurnSpeed = Number(15m, 180m, 5m, 0);
    private readonly CheckBox _requireFocus = TaggedCheckBox("RequireFocus");
    private readonly ToolStripStatusLabel _status = new();
    private readonly Button _checkUpdates;
    private readonly TabPage _renderTab = new() { Tag = "TabRender" };
    private readonly TabPage _panelTab = new() { Tag = "TabPanel" };
    private readonly TabPage _inputTab = new() { Tag = "TabInput" };

    public MainForm(StartupResult startup)
    {
        _startup = startup;
        _checkUpdates = TaggedButton("CheckUpdates", CheckUpdates);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(840, 740);
        ClientSize = new Size(980, 840);
        AutoScaleMode = AutoScaleMode.Dpi;

        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        TableLayoutPanel gameRow = new() { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3 };
        gameRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        gameRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        gameRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        gameRow.Controls.Add(TaggedLabel("GameFolder"), 0, 0);
        gameRow.Controls.Add(_gameRoot, 1, 0);
        gameRow.Controls.Add(TaggedButton("Browse", BrowseRoot), 2, 0);
        root.Controls.Add(gameRow, 0, 0);

        TabControl tabs = new() { Dock = DockStyle.Fill };
        BuildRenderTab();
        BuildPanelTab();
        BuildInputTab();
        tabs.TabPages.Add(_renderTab);
        tabs.TabPages.Add(_panelTab);
        tabs.TabPages.Add(_inputTab);
        root.Controls.Add(tabs, 0, 1);

        TableLayoutPanel footer = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(0, 8, 0, 4)
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        FlowLayoutPanel languages = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        languages.Controls.Add(LanguageButton("한국어", UiLanguage.Korean));
        languages.Controls.Add(LanguageButton("English", UiLanguage.English));
        languages.Controls.Add(LanguageButton("日本語", UiLanguage.Japanese));
        footer.Controls.Add(languages, 0, 0);

        FlowLayoutPanel actions = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        actions.Controls.Add(TaggedButton("Save", (_, _) => Run("StatusSaved", SaveSettings)));
        actions.Controls.Add(TaggedButton("Reload", (_, _) => Run("StatusReloaded", LoadSettings)));
        actions.Controls.Add(TaggedButton("Defaults", (_, _) =>
        {
            Apply(VrSettings.CreateApprovedDefaults());
            _status.Text = UiText.Get("StatusDefaults");
        }));
        actions.Controls.Add(TaggedButton("Export", Export));
        actions.Controls.Add(TaggedButton("Import", Import));
        actions.Controls.Add(_checkUpdates);
        footer.Controls.Add(actions, 1, 0);
        root.Controls.Add(footer, 0, 2);

        StatusStrip statusStrip = new();
        statusStrip.Items.Add(_status);
        root.Controls.Add(statusStrip, 0, 3);

        _gameRoot.Text = SettingsStore.FindInitialGameRoot();
        _vfx.SelectedIndexChanged += (_, _) => UpdateManualVfxControls();
        _eyeScale.ValueChanged += (_, _) => UpdateEyeScaleWarning();
        _manualPostProcessing.CheckedChanged += (_, _) => UpdateManualVfxControls();
        _manualVlBloom.CheckedChanged += (_, _) => UpdateManualVfxControls();
        _locomotion.CheckedChanged += (_, _) => UpdateLocomotionControls();
        ApplyLanguage(languageChanged: false);
        Load += (_, _) => Run("StatusLoaded", LoadSettings);
        Shown += async (_, _) => await OnShownAsync();
        FormClosed += (_, _) => _updateService.Dispose();
    }

    private void BuildRenderTab()
    {
        TableLayoutPanel grid = Grid();
        AddRow(grid, "RuntimeStatus", _runtimeEnabled);
        FlowLayoutPanel eyeScalePanel = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };
        eyeScalePanel.Controls.Add(_eyeScale);
        eyeScalePanel.Controls.Add(_eyeScaleWarning);
        AddRow(grid, "EyeScale", eyeScalePanel);
        AddRow(grid, "WorldEyeScale", _worldEyeScale);
        AddRow(grid, "LiveSixDof", _liveSixDof);
        AddRow(grid, "VfxMode", _vfx);
        AddRow(grid, "VfxPostProcessing", _manualPostProcessing);
        AddRow(grid, "VfxVlBloom", _manualVlBloom);
        AddRow(grid, "VfxVlBloomIntensity", _manualVlBloomIntensity);
        AddRow(grid, "VfxVlBloomDiffusion", _manualVlBloomDiffusion);
        AddRow(grid, "VfxVlDof", _manualVlDof);
        AddRow(grid, "VfxVlTextureBlur", _manualVlTextureBlur);
        AddRow(grid, "VfxVlStarStreak", _manualVlStarStreak);
        AddRow(grid, "VfxVlFlare", _manualVlFlare);
        _renderTab.Controls.Add(grid);
    }

    private void BuildPanelTab()
    {
        TableLayoutPanel grid = Grid();
        AddRow(grid, "PanelHand", _panelHand);
        AddRow(grid, "PointerHand", _pointerHand);
        AddRow(grid, "InitialState", _startEnabled);
        AddRow(grid, "PanelDirection", _viewerFacing);
        AddRow(grid, "OffsetX", _offsetX);
        AddRow(grid, "OffsetY", _offsetY);
        AddRow(grid, "OffsetZ", _offsetZ);
        AddRow(grid, "MaximumWidth", _maximumWidth);
        AddRow(grid, "MaximumHeight", _maximumHeight);
        AddRow(grid, "RotationPitch", _rotationPitch);
        AddRow(grid, "RotationYaw", _rotationYaw);
        AddRow(grid, "RotationRoll", _rotationRoll);
        AddRow(grid, "VisibilityHysteresis", _hysteresis);
        AddRow(grid, "ToggleButton", _toggle);
        _panelTab.Controls.Add(grid);
    }

    private void BuildInputTab()
    {
        TableLayoutPanel grid = Grid();
        AddRow(grid, "ClickButton", _primary);
        AddRow(grid, "BackButton", _back);
        AddRow(grid, "Trigger", _trigger);
        AddRow(grid, "Locomotion", _locomotion);
        AddRow(grid, "LocomotionHand", _locomotionHand);
        AddRow(grid, "LocomotionSpeed", _locomotionSpeed);
        AddRow(grid, "ViewTurnSpeed", _viewTurnSpeed);
        AddRow(grid, "InputSafety", _requireFocus);
        _inputTab.Controls.Add(grid);
    }

    private void ChangeLanguage(UiLanguage language)
    {
        UiText.SetLanguage(language);
        ApplyLanguage(languageChanged: true);
    }

    private void ApplyLanguage(bool languageChanged)
    {
        VrHand panelHand = Selected(_panelHand, VrHand.Left);
        VrHand pointerHand = Selected(_pointerHand, VrHand.Right);
        PanelToggleBinding toggle = Selected(_toggle, PanelToggleBinding.Grip);
        FaceButtonBinding primary = Selected(_primary, FaceButtonBinding.Primary);
        FaceButtonBinding back = Selected(_back, FaceButtonBinding.Secondary);
        VrHand locomotionHand = Selected(_locomotionHand, VrHand.Right);
        string vfx = Selected(_vfx, VrVisualEffectModes.Approved);

        Text = UiText.Get("AppTitle");
        UpdateTaggedText(this);
        Populate(_panelHand, Enum.GetValues<VrHand>(), UiText.Choice, panelHand);
        Populate(_pointerHand, Enum.GetValues<VrHand>(), UiText.Choice, pointerHand);
        Populate(_toggle, Enum.GetValues<PanelToggleBinding>(), UiText.Choice, toggle);
        Populate(_primary, Enum.GetValues<FaceButtonBinding>(), UiText.Choice, primary);
        Populate(_back, Enum.GetValues<FaceButtonBinding>(), UiText.Choice, back);
        Populate(
            _locomotionHand,
            Enum.GetValues<VrHand>(),
            UiText.Choice,
            locomotionHand);
        Populate(
            _vfx,
            new[]
            {
                VrVisualEffectModes.Approved,
                VrVisualEffectModes.AllOn,
                VrVisualEffectModes.AllOff,
                VrVisualEffectModes.Manual
            },
            UiText.VisualEffect,
            vfx);
        UpdateManualVfxControls();
        UpdateLocomotionControls();
        UpdateEyeScaleWarning();
        _status.Text = UiText.Get(languageChanged ? "StatusLanguageChanged" : "StatusReady");
    }

    private void BrowseRoot(object? sender, EventArgs args)
    {
        using FolderBrowserDialog dialog = new()
        {
            Description = UiText.Get("SelectGameFolder"),
            SelectedPath = _gameRoot.Text,
            UseDescriptionForTitle = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _gameRoot.Text = dialog.SelectedPath;
            Run("StatusLoaded", LoadSettings);
        }
    }

    private void LoadSettings() => Apply(SettingsStore.LoadFromGameRoot(_gameRoot.Text));

    private void SaveSettings() => SettingsStore.SaveToGameRoot(_gameRoot.Text, Read());

    private void Import(object? sender, EventArgs args)
    {
        using OpenFileDialog dialog = new() { Filter = UiText.Get("JsonOpenFilter") };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            Run("StatusImported", () => Apply(SettingsStore.LoadFile(dialog.FileName)));
        }
    }

    private void Export(object? sender, EventArgs args)
    {
        using SaveFileDialog dialog = new()
        {
            Filter = UiText.Get("JsonSaveFilter"),
            FileName = "gakumas-vr-settings.json"
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            Run("StatusExported", () => SettingsStore.Export(dialog.FileName, Read()));
        }
    }

    private async Task OnShownAsync()
    {
        if (!string.IsNullOrWhiteSpace(_startup.UpdateError))
        {
            _status.Text = UiText.Format("UpdateFailed", _startup.UpdateError);
            MessageBox.Show(
                this,
                _status.Text,
                UiText.Get("AppTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }
        if (!string.IsNullOrWhiteSpace(_startup.UpdatedVersion))
        {
            _status.Text = UiText.Format("UpdateCompleted", _startup.UpdatedVersion);
            return;
        }
        await CheckForUpdatesAsync(manual: false);
    }

    private async void CheckUpdates(object? sender, EventArgs args) =>
        await CheckForUpdatesAsync(manual: true);

    private async Task CheckForUpdatesAsync(bool manual)
    {
        if (!_checkUpdates.Enabled)
        {
            return;
        }

        _checkUpdates.Enabled = false;
        try
        {
            _status.Text = UiText.Get("UpdateChecking");
            AvailableUpdate? update = await _updateService.CheckAsync(CancellationToken.None);
            if (update is null)
            {
                _status.Text = UiText.Format("UpdateCurrent", UpdateService.CurrentVersion);
                return;
            }

            if (UpdateService.IsGameRunning())
            {
                _status.Text = UiText.Format("UpdateDeferredGameRunning", update.Version);
                if (manual)
                {
                    MessageBox.Show(
                        this,
                        _status.Text,
                        UiText.Get("AppTitle"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                return;
            }

            _status.Text = UiText.Format("UpdateAvailable", update.Version);
            Progress<string> progress = new(message => _status.Text = message);
            StagedUpdate staged = await _updateService.StageAsync(
                update,
                _gameRoot.Text,
                progress,
                CancellationToken.None);
            _status.Text = UiText.Format("UpdateInstalling", update.Version);
            LaunchAutomaticUpdater(staged);
        }
        catch (Exception exception)
        {
            _status.Text = UiText.Format("UpdateFailed", exception.Message);
            if (manual)
            {
                MessageBox.Show(
                    this,
                    _status.Text,
                    UiText.Get("AppTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        finally
        {
            if (!IsDisposed)
            {
                _checkUpdates.Enabled = true;
            }
        }
    }

    private void LaunchAutomaticUpdater(StagedUpdate update)
    {
        ProcessStartInfo start = new(update.InstallerPath)
        {
            UseShellExecute = true,
            WorkingDirectory = update.PackageRoot
        };
        start.ArgumentList.Add("--auto-update");
        start.ArgumentList.Add("--game-root");
        start.ArgumentList.Add(Path.GetFullPath(_gameRoot.Text));
        start.ArgumentList.Add("--package-root");
        start.ArgumentList.Add(update.PackageRoot);
        start.ArgumentList.Add("--cleanup-update");
        start.ArgumentList.Add(update.StagingRoot);
        start.ArgumentList.Add("--wait-pid");
        start.ArgumentList.Add(Environment.ProcessId.ToString());
        _ = Process.Start(start) ?? throw new InvalidOperationException(
            UiText.Get("UpdateLauncherFailed"));
        BeginInvoke(Close);
    }

    private VrSettings Read() => new()
    {
        Runtime = new VrRuntimeSettings { Enabled = _runtimeEnabled.Checked },
        Render = new VrRenderSettings
        {
            EyeRenderScale = (float)_eyeScale.Value,
            WorldEyeOffsetScale = (float)_worldEyeScale.Value,
            VisualEffectMode = Selected(_vfx, VrVisualEffectModes.Approved),
            ManualVisualEffects = new VrManualVisualEffectSettings
            {
                PostProcessingEnabled = _manualPostProcessing.Checked,
                VlBloomEnabled = _manualVlBloom.Checked,
                VlBloomIntensityScale = (float)_manualVlBloomIntensity.Value / 100f,
                VlBloomDiffusion = (int)_manualVlBloomDiffusion.Value,
                VlDepthOfFieldEnabled = _manualVlDof.Checked,
                VlTextureBlurEnabled = _manualVlTextureBlur.Checked,
                VlStarStreakEnabled = _manualVlStarStreak.Checked,
                VlFlareEnabled = _manualVlFlare.Checked
            }
        },
        Tracking = new VrTrackingSettings
        {
            LiveSixDofEnabled = _liveSixDof.Checked,
            LocomotionEnabled = _locomotion.Checked,
            LocomotionHand = Selected(_locomotionHand, VrHand.Right),
            LocomotionSpeed = (float)_locomotionSpeed.Value,
            ViewTurnSpeed = (float)_viewTurnSpeed.Value
        },
        Panel = new VrPanelSettings
        {
            PanelHand = Selected(_panelHand, VrHand.Left),
            PointerHand = Selected(_pointerHand, VrHand.Right),
            StartEnabled = _startEnabled.Checked,
            ViewerFacing = _viewerFacing.Checked,
            OffsetX = (float)_offsetX.Value,
            OffsetY = (float)_offsetY.Value,
            OffsetZ = (float)_offsetZ.Value,
            MaximumWidth = (float)_maximumWidth.Value,
            MaximumHeight = (float)_maximumHeight.Value,
            RotationPitch = (float)_rotationPitch.Value,
            RotationYaw = (float)_rotationYaw.Value,
            RotationRoll = (float)_rotationRoll.Value,
            VisibilityHysteresisMilliseconds = (int)_hysteresis.Value,
            ToggleBinding = Selected(_toggle, PanelToggleBinding.Grip)
        },
        Input = new VrInputSettings
        {
            PrimaryClickButton = Selected(_primary, FaceButtonBinding.Primary),
            BackButton = Selected(_back, FaceButtonBinding.Secondary),
            TriggerClickEnabled = _trigger.Checked,
            ThumbstickScrollEnabled = false,
            ScrollSensitivity = 1f,
            RequireGameFocus = _requireFocus.Checked
        }
    };

    private void Apply(VrSettings settings)
    {
        _runtimeEnabled.Checked = settings.Runtime.Enabled;
        Set(_eyeScale, settings.Render.EyeRenderScale);
        Set(_worldEyeScale, settings.Render.WorldEyeOffsetScale);
        _liveSixDof.Checked = settings.Tracking.LiveSixDofEnabled;
        _locomotion.Checked = settings.Tracking.LocomotionEnabled;
        Select(_locomotionHand, settings.Tracking.LocomotionHand);
        Set(_locomotionSpeed, settings.Tracking.LocomotionSpeed);
        Set(_viewTurnSpeed, settings.Tracking.ViewTurnSpeed);
        Select(_vfx, settings.Render.VisualEffectMode);
        VrManualVisualEffectSettings manual = settings.Render.ManualVisualEffects;
        _manualPostProcessing.Checked = manual.PostProcessingEnabled;
        _manualVlBloom.Checked = manual.VlBloomEnabled;
        Set(_manualVlBloomIntensity, manual.VlBloomIntensityScale * 100f);
        Set(_manualVlBloomDiffusion, manual.VlBloomDiffusion);
        _manualVlDof.Checked = manual.VlDepthOfFieldEnabled;
        _manualVlTextureBlur.Checked = manual.VlTextureBlurEnabled;
        _manualVlStarStreak.Checked = manual.VlStarStreakEnabled;
        _manualVlFlare.Checked = manual.VlFlareEnabled;
        UpdateManualVfxControls();
        Select(_panelHand, settings.Panel.PanelHand);
        Select(_pointerHand, settings.Panel.PointerHand);
        _startEnabled.Checked = settings.Panel.StartEnabled;
        _viewerFacing.Checked = settings.Panel.ViewerFacing;
        Set(_offsetX, settings.Panel.OffsetX);
        Set(_offsetY, settings.Panel.OffsetY);
        Set(_offsetZ, settings.Panel.OffsetZ);
        Set(_maximumWidth, settings.Panel.MaximumWidth);
        Set(_maximumHeight, settings.Panel.MaximumHeight);
        Set(_rotationPitch, settings.Panel.RotationPitch);
        Set(_rotationYaw, settings.Panel.RotationYaw);
        Set(_rotationRoll, settings.Panel.RotationRoll);
        Set(_hysteresis, settings.Panel.VisibilityHysteresisMilliseconds);
        Select(_toggle, settings.Panel.ToggleBinding);
        Select(_primary, settings.Input.PrimaryClickButton);
        Select(_back, settings.Input.BackButton);
        _trigger.Checked = settings.Input.TriggerClickEnabled;
        _requireFocus.Checked = settings.Input.RequireGameFocus;
        UpdateLocomotionControls();
        UpdateEyeScaleWarning();
    }

    private void Run(string successKey, Action action)
    {
        try
        {
            action();
            _status.Text = UiText.Get(successKey);
        }
        catch (Exception exception)
        {
            _status.Text = UiText.Format("ErrorPrefix", exception.Message);
            MessageBox.Show(
                this,
                exception.Message,
                UiText.Get("AppTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void UpdateTaggedText(Control root)
    {
        if (root.Tag is string key)
        {
            root.Text = UiText.Get(key);
        }
        foreach (Control child in root.Controls)
        {
            UpdateTaggedText(child);
        }
    }

    private void UpdateManualVfxControls()
    {
        bool manual = Selected(_vfx, VrVisualEffectModes.Approved) ==
            VrVisualEffectModes.Manual;
        _manualPostProcessing.Enabled = manual;

        bool postProcessing = manual && _manualPostProcessing.Checked;
        _manualVlBloom.Enabled = postProcessing;
        _manualVlDof.Enabled = postProcessing;
        _manualVlTextureBlur.Enabled = postProcessing;
        _manualVlStarStreak.Enabled = postProcessing;
        _manualVlFlare.Enabled = postProcessing;

        bool bloom = postProcessing && _manualVlBloom.Checked;
        _manualVlBloomIntensity.Enabled = bloom;
        _manualVlBloomDiffusion.Enabled = bloom;
    }

    private void UpdateLocomotionControls()
    {
        _locomotionHand.Enabled = _locomotion.Checked;
        _locomotionSpeed.Enabled = _locomotion.Checked;
        _viewTurnSpeed.Enabled = _locomotion.Checked;
    }

    private void UpdateEyeScaleWarning()
    {
        decimal scale = _eyeScale.Value;
        if (scale <= 1.00m)
        {
            _eyeScaleWarning.Text = string.Empty;
            _eyeScaleWarning.Visible = false;
            return;
        }

        int pixelLoadPercent = decimal.ToInt32(decimal.Round(scale * scale * 100m));
        string key;
        if (scale <= 1.25m)
        {
            key = "EyeScaleWarningElevated";
            _eyeScaleWarning.ForeColor = Color.DarkGoldenrod;
        }
        else if (scale <= 1.50m)
        {
            key = "EyeScaleWarningHigh";
            _eyeScaleWarning.ForeColor = Color.DarkOrange;
        }
        else
        {
            key = "EyeScaleWarningExtreme";
            _eyeScaleWarning.ForeColor = Color.Firebrick;
        }

        _eyeScaleWarning.Text = UiText.Format(key, pixelLoadPercent);
        _eyeScaleWarning.Visible = true;
    }

    private static TableLayoutPanel Grid()
    {
        TableLayoutPanel grid = new()
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            ColumnCount = 2,
            Padding = new Padding(16)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        return grid;
    }

    private static void AddRow(TableLayoutPanel grid, string labelKey, Control control)
    {
        int row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.Controls.Add(TaggedLabel(labelKey), 0, row);
        control.Margin = new Padding(3, 4, 3, 4);
        control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        grid.Controls.Add(control, 1, row);
    }

    private static Label TaggedLabel(string key) => new()
    {
        Tag = key,
        AutoSize = true,
        Margin = new Padding(3, 8, 12, 8),
        Anchor = AnchorStyles.Left
    };

    private static CheckBox TaggedCheckBox(string key) => new()
    {
        Tag = key,
        AutoSize = true
    };

    private static Button TaggedButton(string key, EventHandler handler)
    {
        Button button = new() { Tag = key, AutoSize = true };
        button.Click += handler;
        return button;
    }

    private Button LanguageButton(string text, UiLanguage language)
    {
        Button button = new() { Text = text, AutoSize = true };
        button.Click += (_, _) => ChangeLanguage(language);
        return button;
    }

    private static NumericUpDown Number(decimal min, decimal max, decimal increment, int decimals) => new()
    {
        Minimum = min,
        Maximum = max,
        Increment = increment,
        DecimalPlaces = decimals,
        Width = 180
    };

    private static ComboBox ChoiceCombo() => new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList
    };

    private static void Populate<T>(
        ComboBox combo,
        IEnumerable<T> values,
        Func<T, string> text,
        T selected)
        where T : notnull
    {
        combo.BeginUpdate();
        try
        {
            combo.Items.Clear();
            foreach (T value in values)
            {
                combo.Items.Add(new Choice<T>(value, text(value)));
            }
            Select(combo, selected);
        }
        finally
        {
            combo.EndUpdate();
        }
    }

    private static T Selected<T>(ComboBox combo, T fallback) where T : notnull =>
        combo.SelectedItem is Choice<T> choice ? choice.Value : fallback;

    private static void Select<T>(ComboBox combo, T value) where T : notnull
    {
        for (int index = 0; index < combo.Items.Count; index++)
        {
            if (combo.Items[index] is Choice<T> choice &&
                EqualityComparer<T>.Default.Equals(choice.Value, value))
            {
                combo.SelectedIndex = index;
                return;
            }
        }
    }

    private static void Set(NumericUpDown control, float value) =>
        control.Value = Math.Clamp((decimal)value, control.Minimum, control.Maximum);

    private static void Set(NumericUpDown control, int value) =>
        control.Value = Math.Clamp(value, (int)control.Minimum, (int)control.Maximum);

    private sealed class Choice<T>(T value, string text) where T : notnull
    {
        public T Value { get; } = value;

        public override string ToString() => text;
    }
}
