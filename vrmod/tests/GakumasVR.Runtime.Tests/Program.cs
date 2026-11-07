using System.Reflection;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using Doorstop;
using GakumasVR.Core;

string output = Path.GetFullPath(args.Length > 0 ? args[0] : Path.Combine("vrmod", "test-output", "runtime-settings"));
Directory.CreateDirectory(output);
int failed = 0, executed = 0;
Run("Published settings are isolated snapshots with pending/applied revisions", () =>
{
    var draft = VrSettings.CreateApprovedDefaults(); draft.Render.VisualEffectMode = VrVisualEffectModes.VrifyFollow;
    long revision = VrSettingsRuntime.Publish(draft);
    draft.Render.VisualEffectMode = VrVisualEffectModes.AllOff;
    Check(VrSettingsRuntime.Current.Render.VisualEffectMode == VrVisualEffectModes.VrifyFollow);
    Check(VrSettingsRuntime.AppliedRevision < revision);
    VrSettingsRuntime.MarkApplied(revision);
    Check(VrSettingsRuntime.AppliedRevision == revision);
    draft.Render.EyeRenderScale = -1;
    try { VrSettingsRuntime.Publish(draft); throw new Exception("Invalid settings were accepted."); }
    catch (InvalidOperationException) { }
    Check(VrSettingsRuntime.Revision == revision);
});
Run("Atomic save preserves unknown keys and original backup", () =>
{
    string path = Path.Combine(output, "settings-" + Guid.NewGuid().ToString("N") + ".json");
    string original = "{\"future\":{\"flag\":true},\"render\":{\"legacyValue\":123}}";
    File.WriteAllText(path, original);
    VrSettingsRuntime.Save(VrSettingsRuntime.Current, path);
    JsonObject saved = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
    Check(saved["future"]!["flag"]!.GetValue<bool>());
    Check(saved["render"]!["legacyValue"]!.GetValue<int>() == 123);
    Check(saved["render"]!["visualEffectMode"]!.GetValue<string>() == VrVisualEffectModes.VrifyFollow);
    string backup = Directory.GetFiles(output, Path.GetFileName(path) + ".vr-menu-*.bak").Single();
    Check(File.ReadAllText(backup) == original);
});
Run("Invalid existing JSON is never replaced", () =>
{
    string path = Path.Combine(output, "invalid-" + Guid.NewGuid().ToString("N") + ".json");
    File.WriteAllText(path, "invalid json");
    try { VrSettingsRuntime.Save(VrSettingsRuntime.Current, path); throw new Exception("Invalid file was overwritten."); }
    catch (System.Text.Json.JsonException) { }
    Check(File.ReadAllText(path) == "invalid json");
});
Run("Thumbstick clicks cross the frame boundary once and require release after a blocked frame", () =>
{
    static void Send(bool? left, bool? right) => OpenXrLocomotionStateRegistry.Update(
        true, .25f, .5f, true, -.25f, -.5f, left, right);
    static OpenXrLocomotionStateSnapshot Take() => OpenXrLocomotionStateRegistry.Consume()!.Value;

    OpenXrLocomotionStateRegistry.Clear();
    Send(false, false);
    Send(true, true);
    Send(true, true);
    var click = Take();
    Check(click.ResetView && click.ToggleCameraFollow && click.AxisY == .5f && click.ViewTurnAxisY == -.5f);
    Send(true, true);
    click = Take();
    Check(!click.ResetView && !click.ToggleCameraFollow);

    Send(false, false);
    Send(true, true);
    Send(null, null);
    Send(true, true);
    click = Take();
    Check(!click.ResetView && !click.ToggleCameraFollow);

    Send(false, false);
    Send(false, true);
    Send(false, false);
    Send(true, true);
    click = Take();
    Check(click.ResetView && !click.ToggleCameraFollow);
    OpenXrLocomotionStateRegistry.Clear();
    Check(OpenXrLocomotionStateRegistry.Consume() is null);
});
Run("World Grip starts at zero, accumulates samples, and rearms after release", () =>
{
    var motion = new OpenXrWorldGripMotion();
    static TrackingVector3 Position(float x, float y = 0, float z = 0) => new(x, y, z);
    motion.Update(0, null, 1, 100);
    motion.Update(.8f, Position(5), 2, 101);
    Check(motion.Consume(101).X == 0);
    motion.Update(.6f, Position(6, 1, -1), 3, 101);
    motion.Update(.6f, Position(8, 2, -2), 4, 102);
    motion.Update(0, null, 5, 103);
    TrackingVector3 delta = motion.Consume(103);
    Check(delta.X == 3 && delta.Y == 2 && delta.Z == -2);
    Check(motion.Consume(103).X == 0);

    motion.Update(1, Position(1), 6, 104);
    motion.Update(1, Position(2), 7, 105);
    motion.Update(1, null, 8, 106);
    motion.Update(1, Position(9), 9, 107);
    Check(motion.Consume(107).X == 0);
    motion.Update(0, null, 10, 108);
    motion.Update(1, Position(10), 11, 109);
    motion.Update(1, Position(11), 12, 110);
    Check(motion.Consume(110).X == 1);
    motion.Update(1, Position(12), 13, 111);
    Check(motion.Consume(362).X == 0);
    motion.Cancel();
    motion.Update(1, Position(20), 14, 363);
    motion.Update(1, Position(21), 15, 364);
    Check(motion.Consume(364).X == 0);
});
Run("World Grip reverses the same scaled displacement used by head tracking", () =>
{
    var mapper = new SixDofPoseMapper();
    var orientation = new TrackingQuaternion(0, MathF.Sin(.35f), 0, MathF.Cos(.35f));
    static TrackingStereoPose Both(TrackingVector3 position, TrackingQuaternion rotation)
    {
        var eye = new TrackingEyePose(position, rotation);
        return new TrackingStereoPose(eye, eye);
    }
    var handDelta = new TrackingVector3(.25f, .5f, -.75f);
    Check(mapper.TryMap(Both(default, orientation), 2, 0, out _));
    Check(mapper.TryMap(Both(handDelta, orientation), 2, 0, out UnityStereoPose head));
    Check(mapper.TryMapWorldGripDelta(handDelta, 2, out TrackingVector3 camera));
    Check(MathF.Abs(camera.X + head.Left.LocalPosition.X) < .0001f &&
        MathF.Abs(camera.Y + head.Left.LocalPosition.Y) < .0001f &&
        MathF.Abs(camera.Z + head.Left.LocalPosition.Z) < .0001f);

    var settings = VrSettings.CreateApprovedDefaults();
    Check(settings.Tracking.WorldGripEnabled);
    settings.Tracking.WorldGripEnabled = false;
    Check(!VrSettingsValidator.Validate(settings).Settings.Tracking.WorldGripEnabled);
});
Run("Contact hands use the rendered eye center and scale, with one XR handedness conversion", () =>
{
    var identity = new TrackingQuaternion(0, 0, 0, 1);
    var raw = new TrackingStereoPose(new TrackingEyePose(new TrackingVector3(-.03f, 0, 0), identity),
        new TrackingEyePose(new TrackingVector3(.03f, 0, 0), identity));
    var center = new Vector3(5, 2, -1);
    var yaw = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);
    Check(ContactHandAlignment.TryCreate(raw, center - new Vector3(0, 0, .06f), center + new Vector3(0, 0, .06f),
        yaw, yaw, out ContactHandAlignment alignment));
    Check(alignment.TryMap(new TrackingVector3(0, 0, -.5f), out Vector3 mapped));
    Check(Vector3.Distance(mapped, center + Vector3.UnitX) < .0001f);
    Check(MathF.Abs(alignment.Scale * ContactHandAlignment.HandRadiusMeters - .07f) < .0001f);
    Check(!alignment.TryMap(new TrackingVector3(float.NaN, 0, 0), out _));
    Check(!ContactHandAlignment.TryCreate(raw, center, center, yaw, yaw, out _));

    OpenXrStereoStateRegistry.UpdateConfiguration(100, 100);
    OpenXrStereoStateRegistry.UpdateWorldViews(15, default, default, new OpenXrContactHands(new TrackingVector3(1, 2, 3), null));
    OpenXrStereoStateSnapshot snapshot = OpenXrStereoStateRegistry.SnapshotWorld(1000)!;
    Check(snapshot.ContactHands.Left?.X == 1 && snapshot.ContactHands.Right is null);
    OpenXrStereoStateRegistry.UpdateWorldViews(15, default, default, default);
    Check(OpenXrStereoStateRegistry.SnapshotWorld(1000)!.ContactHands.Left is null);
    Check(snapshot.ContactHands.Left?.X == 1);
});
Run("IL2CPP object roots retain native pointer width through release", () =>
{
    Check(typeof(Il2CppApi.GcHandleNewDelegate).GetMethod("Invoke")!.ReturnType == typeof(IntPtr));
    Check(typeof(MainThreadSampler.ContactFreeHandle).GetMethod("Invoke")!.GetParameters()[0].ParameterType == typeof(IntPtr));
    Check(typeof(MainThreadSampler).GetMethod("RootObject", BindingFlags.Instance | BindingFlags.NonPublic)!.ReturnType == typeof(IntPtr));
});
Run("Contact pilot keeps a physical near range and projects asymmetric eye frustums", () =>
{
    Check(MathF.Abs(ContactHandAlignment.PilotNearClip(.1f, .275f) - .0165f) < .00001f);
    Check(ContactHandAlignment.PilotNearClip(.01f, .275f) == .01f);
    Check(ContactHandAlignment.PilotNearClip(.1f, 0f) == .1f);
    var eye = new TrackingEyePose(new TrackingVector3(.03f, 0f, 0f), new TrackingQuaternion(0f, 0f, 0f, 1f));
    Check(ContactHandAlignment.TryProjectRawHand(new TrackingVector3(.03f, 0f, -.25f), eye,
        MathF.Atan(-.5f), MathF.Atan(1f), MathF.Atan(-1f), MathF.Atan(1f), out Vector3 point));
    Check(MathF.Abs(point.X - 1f / 3f) < .00001f && point.Y == .5f && point.Z == .25f);
    Check(!ContactHandAlignment.TryProjectRawHand(new TrackingVector3(0f, 0f, .25f), eye,
        -.5f, .5f, -.5f, .5f, out _));
});
Run("Modal menu consumes clicks and discards drafts on close and focus loss", () =>
{
    VrSettingsRuntime.Publish(VrSettings.CreateApprovedDefaults());
    Type type = typeof(OpenXrProbe).GetNestedType("SettingsOverlay", BindingFlags.NonPublic)!;
    var ctor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
    object[] arguments = ctor.GetParameters().Select(p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType)! : null!).ToArray();
    arguments[^1] = new Action<string, string>((_, _) => { });
    object menu = ctor.Invoke(arguments);
    var update = type.GetMethod("Update")!;
    bool Visible() => (bool)type.GetProperty("Visible")!.GetValue(menu)!;
    bool Blocks() => (bool)type.GetProperty("BlocksGameInput")!.GetValue(menu)!;
    void Send(OpenXrControllerFrame frame) => update.Invoke(menu, new object[] { frame });
    OpenXrControllerFrame idle = default(OpenXrControllerFrame) with { ActionsFocused = true };
    OpenXrControllerFrame Aim(float u, float v, float trigger) => idle with
    {
        PointerAimTracked = true,
        PointerAimPose = new OpenXrControllerPose(0, 0, 0, 1, (u - .5f) * 1.2f, (.5f - v) * .9f, 0),
        PointerTriggerValue = trigger
    };
    void Click(float u, float v) { Send(Aim(u, v, 0)); Send(Aim(u, v, 1)); Send(Aim(u, v, 0)); }
    Send(idle with { SettingsToggleRequested = true });
    Check(Visible() && Blocks());
    Click(.75f, .15f); // C is only a draft until Apply.
    Check(VrSettingsRuntime.Current.Render.VisualEffectMode == VrVisualEffectModes.Approved);
    Click(.5f, .93f);
    Check(VrSettingsRuntime.Current.Render.VisualEffectMode == VrVisualEffectModes.VrifyFollow);
    Click(.25f, .15f); // Discard A on close.
    Send(idle with { PointerBackPressed = true, PointerTriggerValue = 1 });
    Check(!Visible() && Blocks());
    Check(VrSettingsRuntime.Current.Render.VisualEffectMode == VrVisualEffectModes.VrifyFollow);
    Send(idle with { PointerTriggerValue = 1 }); Check(Blocks());
    Send(idle); Check(!Blocks());
    Send(idle with { SettingsToggleRequested = true }); Check(Visible());
    Send(default); Check(!Visible() && Blocks());
    Send(idle); Check(!Blocks());
});
Run("Menu language follows GUI preference and changes without losing drafts", () =>
{
    VrUiLanguage stored = VrUiLanguage.English;
    bool saveAllowed = true;
    Type type = typeof(OpenXrProbe).GetNestedType("SettingsOverlay", BindingFlags.NonPublic)!;
    var ctor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
    object[] arguments = ctor.GetParameters().Select(p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType)! : null!).ToArray();
    arguments[^3] = new Func<VrUiLanguage>(() => stored);
    arguments[^2] = new Func<VrUiLanguage, bool>(language => { if (!saveAllowed) return false; stored = language; return true; });
    arguments[^1] = new Action<string, string>((_, _) => { });
    object menu = ctor.Invoke(arguments);
    var update = type.GetMethod("Update")!;
    var languageField = type.GetField("_language", BindingFlags.Instance | BindingFlags.NonPublic)!;
    var editorField = type.GetField("_editor", BindingFlags.Instance | BindingFlags.NonPublic)!;
    var nextRead = type.GetField("_nextLanguageRead", BindingFlags.Instance | BindingFlags.NonPublic)!;
    void Send(OpenXrControllerFrame frame) => update.Invoke(menu, new object[] { frame });
    OpenXrControllerFrame idle = default(OpenXrControllerFrame) with { ActionsFocused = true };
    OpenXrControllerFrame pointer = idle with { PointerAimTracked = true, PointerAimPose = new OpenXrControllerPose(0, 0, 0, 1, .42f, .405f, 0) };
    void ClickLanguage() { Send(pointer); Send(pointer with { PointerPrimaryPressed = true }); Send(pointer); }
    Send(idle with { SettingsToggleRequested = true });
    Check((VrUiLanguage)languageField.GetValue(menu)! == VrUiLanguage.English);
    var editor = (VrSettingsEditor)editorField.GetValue(menu)!;
    editor.Draft.Tracking.LocomotionSpeed = 3;
    ClickLanguage(); Check(stored == VrUiLanguage.Japanese);
    Check(editor.Draft.Tracking.LocomotionSpeed == 3);
    stored = VrUiLanguage.Korean; nextRead.SetValue(menu, 0L); Send(idle);
    Check((VrUiLanguage)languageField.GetValue(menu)! == VrUiLanguage.Korean);
    Check(ReferenceEquals(editor, editorField.GetValue(menu)));
    saveAllowed = false; ClickLanguage(); nextRead.SetValue(menu, 0L); Send(idle);
    Check((VrUiLanguage)languageField.GetValue(menu)! == VrUiLanguage.English);
    Check(stored == VrUiLanguage.Korean);
    Send(idle with { PointerBackPressed = true }); Send(idle);
    Send(idle with { SettingsToggleRequested = true });
    Check((VrUiLanguage)languageField.GetValue(menu)! == VrUiLanguage.Korean);
    Send(default);
});
Run("Menu rasterizes every settings page in RGBA and BGRA", () =>
{
    Type overlayType = typeof(OpenXrProbe).GetNestedType("SettingsOverlay", BindingFlags.NonPublic)!;
    var constructor = overlayType.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
    object[] values = constructor.GetParameters().Select(p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType)! : null!).ToArray();
    object overlay = constructor.Invoke(values);
    var editor = new VrSettingsEditor(VrSettings.CreateApprovedDefaults());
    overlayType.GetField("_editor", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(overlay, editor);
    var pageField = overlayType.GetField("_page", BindingFlags.NonPublic | BindingFlags.Instance)!;
    var draw = overlayType.GetMethod("Draw", BindingFlags.NonPublic | BindingFlags.Instance)!;
    int pages = (editor.Entries.Count + 5) / 6;
    foreach (VrUiLanguage language in Enum.GetValues<VrUiLanguage>())
    {
        overlayType.GetField("_language", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(overlay, language);
        for (int page = 0; page < pages; page++)
        {
            pageField.SetValue(overlay, page);
            byte[] rgba = (byte[])draw.Invoke(overlay, new object[] { false })!;
            byte[] bgra = (byte[])draw.Invoke(overlay, new object[] { true })!;
            Check(rgba.Length == SettingsMenuLayout.Width * SettingsMenuLayout.Height * 4);
            for (int i = 0; i < rgba.Length; i += 4)
                Check(rgba[i] == bgra[i + 2] && rgba[i + 1] == bgra[i + 1] && rgba[i + 2] == bgra[i] && rgba[i + 3] == 255);
            WriteBmp(Path.Combine(output, $"menu-{VrUiPreferences.Code(language)}-page-{page + 1:00}.bmp"), bgra);
        }
    }
    Console.WriteLine($"PREVIEW: 3 languages x {pages} pages / {editor.Entries.Count} settings, {output}");
});
Console.WriteLine($"Runtime checks: {executed}; failures: {failed}");
return failed == 0 ? 0 : 1;

void Run(string name, Action test)
{
    executed++;
    try { test(); Console.WriteLine("PASS " + name); }
    catch (Exception error) { failed++; Console.Error.WriteLine("FAIL " + name + ": " + error); }
}
static void Check(bool condition) { if (!condition) throw new Exception("Assertion failed."); }
static void WriteBmp(string path, byte[] pixels)
{
    using BinaryWriter writer = new(File.Create(path));
    writer.Write((ushort)0x4d42); writer.Write(54 + pixels.Length); writer.Write(0); writer.Write(54);
    writer.Write(40); writer.Write(SettingsMenuLayout.Width); writer.Write(-SettingsMenuLayout.Height);
    writer.Write((ushort)1); writer.Write((ushort)32); writer.Write(0); writer.Write(pixels.Length);
    writer.Write(2835); writer.Write(2835); writer.Write(0); writer.Write(0); writer.Write(pixels);
}
