using System.Diagnostics;
using System.Runtime.InteropServices;
using GakumasVR.Core;

namespace Doorstop;

internal static partial class OpenXrProbe
{
    private sealed class SettingsOverlay : IDisposable
    {
        private readonly IntPtr _session, _space;
        private readonly D3D11Texture2DDescription _sourceDescription;
        private readonly EnumerateSwapchainFormatsDelegate _formats;
        private readonly CreateSwapchainDelegate _create;
        private readonly EnumerateSwapchainImagesDelegate _images;
        private readonly DestroySwapchainDelegate _destroy;
        private readonly Action<string, string>? _diagnostic;
        private readonly Func<VrUiLanguage> _readLanguage;
        private readonly Func<VrUiLanguage, bool> _writeLanguage;
        private VrUiLanguage _language;
        private long _nextLanguageRead;
        private bool _languageSaveFailed;
        private object[] _statusArguments = Array.Empty<object>();
        private PanelSwapchainResources? _panel;
        private IntPtr _layerPointer;
        private VrSettingsEditor? _editor;
        private int _page;
        private bool _dirty, _clickHeld, _backHeld, _suppressGame, _pointerHit;
        private float _u, _v;
        private long _lastAppliedRevision;
        private string _status = "MenuEdit";
        private Task<string>? _save;
        private long _nextFailureLog;
        public bool Visible { get; private set; }
        public bool BlocksGameInput => Visible || _suppressGame;

        public SettingsOverlay(IntPtr session, IntPtr space, D3D11Texture2DDescription description,
            EnumerateSwapchainFormatsDelegate formats, CreateSwapchainDelegate create,
            EnumerateSwapchainImagesDelegate images, DestroySwapchainDelegate destroy,
            Func<VrUiLanguage>? readLanguage = null, Func<VrUiLanguage, bool>? writeLanguage = null,
            Action<string, string>? diagnostic = null)
        { _readLanguage = readLanguage ?? (() => VrUiPreferences.Load());
          _writeLanguage = writeLanguage ?? (language => VrUiPreferences.TrySave(language));
          _language = _readLanguage(); _diagnostic = diagnostic; _session = session; _space = space; _sourceDescription = description; _formats = formats; _create = create; _images = images; _destroy = destroy; }

        public void Update(OpenXrControllerFrame frame)
        {
            if (_save is { IsCompleted: true })
            {
                _status = _save.GetAwaiter().GetResult();
                _statusArguments = Array.Empty<object>();
                _save = null;
                _dirty = true;
            }
            if (_lastAppliedRevision != VrSettingsRuntime.AppliedRevision)
            { _lastAppliedRevision = VrSettingsRuntime.AppliedRevision; _dirty = true; }
            bool pressed = frame.PointerPrimaryPressed || frame.PointerTriggerValue >= (_clickHeld ? .25f : .72f);
            bool allReleased = !frame.PointerPrimaryPressed && !frame.PointerBackPressed && frame.PointerTriggerValue <= .10f;
            if (_suppressGame && allReleased) _suppressGame = false;
            if (!frame.ActionsFocused)
            {
                if (Visible) Close();
                _clickHeld = true;
                _backHeld = true;
                return;
            }
            if (frame.SettingsToggleRequested)
            {
                if (Visible) Close();
                else
                {
                    _editor = new VrSettingsEditor(VrSettingsRuntime.Current, VrSettingsRuntime.ContactConsentGranted);
                    _page = 0; Visible = true; VrSettingsRuntime.MenuOpen = true;
                    _language = _readLanguage(); _languageSaveFailed = false;
                    _nextLanguageRead = Environment.TickCount64 + 1000;
                    _status = "MenuEdit"; _statusArguments = Array.Empty<object>(); _dirty = true;
                    _clickHeld = pressed; _backHeld = frame.PointerBackPressed;
                    Log("vr-settings-menu-open", "hold=800ms");
                }
            }
            if (!Visible) { _clickHeld = pressed; _backHeld = frame.PointerBackPressed; return; }
            if (!_languageSaveFailed && Environment.TickCount64 >= _nextLanguageRead)
            {
                _nextLanguageRead = Environment.TickCount64 + 1000;
                VrUiLanguage nextLanguage = _readLanguage();
                if (nextLanguage != _language) { _language = nextLanguage; _dirty = true; }
            }
            _pointerHit = frame.PointerAimTracked && TryIntersectPanel(frame.PointerAimPose,
                MenuPose(), new XrExtent2Df { Width = 1.2f, Height = .9f }, out _u, out _v);
            bool back = frame.PointerBackPressed && !_backHeld;
            if (back) Close();
            else if (_pointerHit && pressed && !_clickHeld) Click(_u, _v);
            _clickHeld = pressed; _backHeld = frame.PointerBackPressed;
        }

        private void Close()
        {
            Visible = false; VrSettingsRuntime.MenuOpen = false; _suppressGame = true;
            _editor = null; _pointerHit = false;
            Log("vr-settings-menu-close", "Unapplied edits discarded; game input waits for release.");
        }

        private void Click(float u, float v)
        {
            if (_editor is null) return;
            var hit = SettingsMenuLayout.Hit(u, v);
            int pages = (_editor.Entries.Count + SettingsMenuLayout.RowsPerPage - 1) / SettingsMenuLayout.RowsPerPage;
            switch (hit.Action)
            {
                case SettingsMenuAction.Language:
                    _language = (VrUiLanguage)(((int)_language + 1) % 3);
                    _languageSaveFailed = !_writeLanguage(_language);
                    _status = _languageSaveFailed ? "MenuLanguageSaveFailed" : "StatusLanguageChanged";
                    _statusArguments = Array.Empty<object>();
                    _nextLanguageRead = Environment.TickCount64 + 1000;
                    Log("vr-settings-menu-language", $"language={VrUiPreferences.Code(_language)};saved={!_languageSaveFailed}");
                    break;
                case SettingsMenuAction.Previous: _page = (_page + pages - 1) % pages; break;
                case SettingsMenuAction.Next: _page = (_page + 1) % pages; break;
                case SettingsMenuAction.Close: Close(); return;
                case SettingsMenuAction.ProfileA: _editor.Draft.Render.VisualEffectMode = VrVisualEffectModes.Approved; break;
                case SettingsMenuAction.ProfileC: _editor.Draft.Render.VisualEffectMode = VrVisualEffectModes.VrifyFollow; break;
                case SettingsMenuAction.Decrease:
                case SettingsMenuAction.Increase:
                    int index = _page * SettingsMenuLayout.RowsPerPage + hit.Row;
                    if (index < _editor.Entries.Count) _editor.Change(index, hit.Action == SettingsMenuAction.Decrease ? -1 : 1);
                    break;
                case SettingsMenuAction.Apply:
                case SettingsMenuAction.Save:
                    if (_save is not null) { _status = "MenuSavingInProgress"; break; }
                    try
                    {
                        long revision = VrSettingsRuntime.Publish(_editor.Draft);
                        _status = "MenuApplied"; _statusArguments = new object[] { revision };
                        Log("vr-settings-menu-apply", $"revision={revision};mode={_editor.Draft.Render.VisualEffectMode}");
                        if (hit.Action == SettingsMenuAction.Save)
                        {
                            VrSettings snapshot = VrSettingsValidator.Validate(_editor.Draft).Settings;
                            _status = "MenuSaving";
                            _save = Task.Run(() =>
                            {
                                try { VrSettingsRuntime.Save(snapshot); Log("vr-settings-menu-saved", $"revision={revision}"); return "MenuSaved"; }
                                catch (Exception error) { Log("vr-settings-menu-save-failure", error.GetType().Name); return "MenuSaveFailed"; }
                            });
                        }
                    }
                    catch (Exception error) { _status = "MenuApplyFailed"; Log("vr-settings-menu-apply-failure", error.GetType().Name); }
                    break;
            }
            _dirty = true;
        }

        public uint Render(IntPtr context, IntPtr query, IntPtr layerPointers, uint layerCount,
            PanelSwapchainResources? cursor, IntPtr cursorLayerPointer,
            AcquireSwapchainImageDelegate acquire, WaitSwapchainImageDelegate wait, ReleaseSwapchainImageDelegate release)
        {
            if (!Visible || _editor is null) return layerCount;
            try
            {
                if (_panel is null)
                {
                    D3D11Texture2DDescription description = _sourceDescription;
                    description.Width = SettingsMenuLayout.Width; description.Height = SettingsMenuLayout.Height;
                    description.MipLevels = 1; description.ArraySize = 1; description.SampleCount = 1;
                    _panel = CreatePanelSwapchainResources(_session, _space, description, _formats, _create, _images, _destroy);
                    XrCompositionLayerQuad layer = _panel.Layer;
                    layer.Pose = MenuPose(); layer.Size = new XrExtent2Df { Width = 1.2f, Height = .9f };
                    _panel.Layer = layer;
                    _layerPointer = Marshal.AllocHGlobal(Marshal.SizeOf<XrCompositionLayerQuad>());
                    _dirty = true;
                }
                if (_dirty)
                {
                    byte[] pixels = Draw(_panel.Format is 87 or 91);
                    XrSwapchainImageAcquireInfo acquireInfo = new() { Type = 55 };
                    Check(acquire(_panel.Swapchain, ref acquireInfo, out uint index), "acquire settings image");
                    XrSwapchainImageWaitInfo waitInfo = new() { Type = 56, Timeout = long.MaxValue };
                    Check(wait(_panel.Swapchain, ref waitInfo), "wait settings image");
                    try
                    {
                        if (index >= _panel.Images.Count) throw new InvalidOperationException("Settings swapchain index.");
                        D3D11Interop.UpdateTexture(context, _panel.Images[(int)index], 0, pixels,
                            SettingsMenuLayout.Width, SettingsMenuLayout.Height);
                        D3D11Interop.WaitForGpuLowLatency(context, query, 2000);
                    }
                    finally
                    {
                        XrSwapchainImageReleaseInfo releaseInfo = new() { Type = 57 };
                        Check(release(_panel.Swapchain, ref releaseInfo), "release settings image");
                    }
                    _dirty = false;
                }
                Marshal.StructureToPtr(_panel.Layer, _layerPointer, false);
                Marshal.WriteIntPtr(layerPointers, checked((int)layerCount * IntPtr.Size), _layerPointer); layerCount++;
                if (_pointerHit && cursor is not null && cursorLayerPointer != IntPtr.Zero)
                {
                    ConfigureCursorLayer(cursor, _panel, _u, _v);
                    UpdateCursorSwapchain(context, query, cursor, acquire, wait, release);
                    Marshal.StructureToPtr(cursor.Layer, cursorLayerPointer, false);
                    Marshal.WriteIntPtr(layerPointers, checked((int)layerCount * IntPtr.Size), cursorLayerPointer); layerCount++;
                }
                return layerCount;
            }
            catch (Exception error)
            {
                // A failed menu upload never tears down the active OpenXR session or game.
                Close();
                if (Environment.TickCount64 >= _nextFailureLog)
                { _nextFailureLog = Environment.TickCount64 + 5000; Log("vr-settings-menu-render-failure", error.GetType().Name); }
                return layerCount;
            }
        }

        private byte[] Draw(bool bgra)
        {
            VrSettingsEditor editor = _editor!;
            string font = _language switch { VrUiLanguage.Korean => "Malgun Gothic", VrUiLanguage.Japanese => "Yu Gothic UI", _ => "Segoe UI" };
            using MenuBitmap image = new(SettingsMenuLayout.Width, SettingsMenuLayout.Height, bgra, font);
            image.Rect(0, 0, 960, 720, 0x101827);
            image.Rect(0, 0, 960, 4, 0x42b9a4);
            image.Text(T("AppTitle"), 28, 24, 3, maxCharacters: 36);
            image.Rect(700, 18, 236, 36, 0x30425d);
            string languageName = _language switch { VrUiLanguage.Korean => "한국어", VrUiLanguage.Japanese => "日本語", _ => "English" };
            image.Text(T("MenuLanguage", languageName), 712, 25, 2, maxCharacters: 18);
            int pages = (editor.Entries.Count + 5) / 6;
            image.Text(T("MenuPage", _page + 1, pages), 28, 58, 2, 0x9aadc8, 74);
            string[] modes = { VrVisualEffectModes.Approved, VrVisualEffectModes.VrifyFollow };
            for (int i = 0; i < modes.Length; i++)
            {
                int x = SettingsMenuLayout.ProfileStart + i * SettingsMenuLayout.ProfileSpacing;
                image.Rect(x, 84, SettingsMenuLayout.ProfileWidth, 48, editor.Draft.Render.VisualEffectMode == modes[i] ? 0x166d63u : 0x26364fu);
                image.Text(VrSettingsText.Value(_language, modes[i]), x + 16, 97, 2, maxCharacters: 36);
            }
            for (int row = 0; row < 6; row++)
            {
                int index = _page * 6 + row;
                if (index >= editor.Entries.Count) break;
                var entry = editor.Entries[index]; int y = 148 + row * 70;
                image.Rect(24, y, 912, 60, 0x1a273c);
                image.Text(entry.GetLabel(_language), 36, y + 8, 2, 0xe7edf7, 38);
                image.Text(entry.GetGroup(_language) + (entry.NextSession ? " · " + T("MenuNextSession") : entry.Unsupported ? " · " + T("MenuUnused") : ""), 36, y + 38, 1, 0x9aadc8, 74);
                image.Rect(510, y, 64, 60, 0x30425d); image.Text("-", 536, y + 19, 3);
                image.Text(entry.GetDisplay(_language), 590, y + 20, 2, entry.Unsupported ? 0x7e8b9fu : 0x7de2ceu, 22);
                image.Rect(866, y, 64, 60, 0x30425d); image.Text("+", 890, y + 19, 3);
            }
            image.Text(T(_status, _statusArguments), 28, 584, 2, 0x7de2ce, 75);
            image.Text(T("MenuRevision", VrSettingsRuntime.Revision, VrSettingsRuntime.AppliedRevision, VrSettingsText.Value(_language, editor.Draft.Render.VisualEffectMode)), 28, 612, 1, 0x9aadc8, 150);
            string[] footer = { T("MenuPrevious"), T("MenuNext"), T("MenuApply"), T("Save"), T("MenuClose") };
            for (int i = 0; i < footer.Length; i++)
            {
                image.Rect(24 + i * 184, 640, 176, 56, i == 2 ? 0x166d63u : 0x30425du);
                image.Text(footer[i], 42 + i * 184, 660, 2);
            }
            return image.Pixels;
        }

        private string T(string key, params object[] arguments) => VrUiText.Format(_language, key, arguments);

        private static XrPosef MenuPose()
        { XrPosef pose = IdentityPose(); pose.Position.Z = -1.2f; return pose; }
        private void Log(string name, string reason)
        {
            if (_diagnostic is not null) { _diagnostic(name, reason); return; }
            try { RuntimeProbe.Append(RuntimeProbe.GetLogPath(), new ProbeEvent { TimestampUtc = DateTimeOffset.UtcNow, Event = name, Reason = reason }); }
            catch { /* Diagnostic failure must not interrupt menu input. */ }
        }
        public void Dispose()
        {
            VrSettingsRuntime.MenuOpen = false;
            DestroyPanelSwapchainResources(_panel, _destroy);
            if (_layerPointer != IntPtr.Zero) { Marshal.FreeHGlobal(_layerPointer); _layerPointer = IntPtr.Zero; }
        }
    }
}
