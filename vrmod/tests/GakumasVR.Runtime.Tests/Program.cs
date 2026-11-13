using System.Reflection;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using Doorstop;
using GakumasVR.Core;

string output = Path.GetFullPath(args.Length > 0 ? args[0] : Path.Combine("vrmod", "test-output", "runtime-settings"));
Directory.CreateDirectory(output);
int failed = 0, executed = 0;
Run("Disabled logging creates no files or captures and I/O failure is nonfatal", () =>
{
    string folder = Path.Combine(output, "logging-" + Guid.NewGuid().ToString("N"));
    string path = Path.Combine(folder, "runtime-bootstrap.jsonl");
    RuntimeProbe.LoggingEnabled = false;
    RuntimeProbe.Append(path, new ProbeEvent { Event = "disabled" });
    D3D11Interop.SaveTextureBmp(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, Path.Combine(folder, "capture.bmp"));
    Check(!Directory.Exists(folder));
    RuntimeProbe.LoggingEnabled = true;
    RuntimeProbe.Append(path, new ProbeEvent { Event = "enabled" });
    Check(File.ReadAllText(path).Contains("enabled"));
    long size = new FileInfo(path).Length;
    RuntimeProbe.LoggingEnabled = false;
    RuntimeProbe.Append(path, new ProbeEvent { Event = "disabled again" });
    Check(new FileInfo(path).Length == size);
    RuntimeProbe.LoggingEnabled = true;
    RuntimeProbe.Append(folder, new ProbeEvent()); // A directory cannot be opened as a file.
    RuntimeProbe.LoggingEnabled = false;
});

Run("Thigh binding requires an exact path and a measured mid-thigh surface", () =>
{
    var existing = Array.Empty<MainThreadSampler.ContactRigKey>();
    var actor = (IntPtr)1;
    var bone = (IntPtr)2;
    Check(!MainThreadSampler.CanBindContactRig(existing, actor, bone, MainThreadSampler.ContactLeftThighPath));
    Check(!MainThreadSampler.CanBindContactRig(existing, actor, bone, "Pelvis/LeftUpLeg", verifiedSurface: true));
    Check(MainThreadSampler.CanBindContactRig(existing, actor, bone, MainThreadSampler.ContactLeftThighPath, verifiedSurface: true));
    var points = (from y in Enumerable.Range(-4, 9)
                  from x in Enumerable.Range(-4, 9)
                  select new Vector3(x * .015f, y * .015f, .12f)).ToArray();
    var normals = Enumerable.Repeat(Vector3.UnitZ, points.Length).ToArray();
    var selected = Enumerable.Range(0, points.Length).ToArray();
    Check(!MainThreadSampler.TryFitThighSurface(points, normals, selected, Matrix4x4.Identity, Matrix4x4.Identity,
        new Vector3(.01f, .4f, .6f), out _));
    Check(MainThreadSampler.TryFitThighSurface(points, normals, selected, Matrix4x4.Identity, Matrix4x4.Identity,
        new Vector3(.21f, .43f, .65f), out var anchor));
    Check(anchor.TryMap(Matrix4x4.Identity, out var center, out var outward, out float radius) && radius <= .04f);
    Check(MathF.Abs((center + outward * radius).Z - .12f) < .0001f);
});
Run("Calf binding requires a measured patch on either exact rig path", () =>
{
    var existing = Array.Empty<MainThreadSampler.ContactRigKey>();
    var actor = (IntPtr)1;
    var bone = (IntPtr)2;
    foreach (string path in new[] { MainThreadSampler.ContactLeftCalfPath, MainThreadSampler.ContactRightCalfPath })
    {
        Check(MainThreadSampler.ContactRegionForPath(path) == ContactRegion.Calf);
        Check(!MainThreadSampler.CanBindContactRig(existing, actor, bone, path));
        Check(MainThreadSampler.CanBindContactRig(existing, actor, bone, path, verifiedSurface: true));
    }
    foreach (string path in new[]
    {
        "Pelvis/LeftUpLeg/LeftLeg",
        "Pelvis/LeftHipSkin1_S",
        "Pelvis/RightWaist_O/RightWaist_H",
        "Pelvis/Skirt_Leftside_O/LeftBackSkirt_A/LeftBackSkirt1_S/LeftBackSkirt2_S"
    })
    {
        Check(MainThreadSampler.ContactRegionForPath(path) == ContactRegion.Unknown);
        Check(!MainThreadSampler.CanBindContactRig(existing, actor, bone, path, verifiedSurface: true));
    }

    var vertices = (from y in Enumerable.Range(-2, 5) from x in Enumerable.Range(-2, 5)
                    select new Vector3(x * .015f, y * .015f, .12f)).ToArray();
    var normals = Enumerable.Repeat(Vector3.UnitZ, vertices.Length).ToArray();
    int[] selected = Enumerable.Range(0, vertices.Length).ToArray();
    Check(!MainThreadSampler.TryFitLowerBodySurface(ContactRegion.Calf, vertices, normals, selected,
        Matrix4x4.Identity, Matrix4x4.Identity, new Vector3(.02f, .4f, .6f), out _));
    Check(MainThreadSampler.TryFitLowerBodySurface(ContactRegion.Calf, vertices, normals, selected,
        Matrix4x4.Identity, Matrix4x4.Identity, new Vector3(.22f, .4f, .6f), out var calfSurface));
    Check(calfSurface.TryMap(Matrix4x4.Identity, out _, out _, out float radius) && radius <= .03201f);
    Check(!MainThreadSampler.TryFitLowerBodySurface(ContactRegion.Calf, vertices, normals, selected,
        Matrix4x4.CreateTranslation(Vector3.UnitX), Matrix4x4.Identity, new Vector3(.22f, .4f, .6f), out _));
});
Run("Contact range rings lie on the unit collision boundary", () =>
{
    var points = MainThreadSampler.ContactRangeVertices();
    Check(points.Count == 3 * 32 * 6 && points.All(p => MathF.Abs(p.LengthSquared() - 1f) < .00001f));
});
Run("Calf contact survives small boundary crossings only for an already touching tracked hand", () =>
{
    const float entryRadius = .05f;
    Vector3 near = Vector3.UnitZ * .052f;
    Vector3 outside = Vector3.UnitZ * .055f;
    Check(MainThreadSampler.ResolveCalfContactMask(1, 0, true, true, Vector3.Zero, entryRadius, null, near) == 1);
    Check(MainThreadSampler.ResolveCalfContactMask(0, 0, true, true, Vector3.Zero, entryRadius, near, near) == 0);
    Check(MainThreadSampler.ResolveCalfContactMask(0, 1, true, true, Vector3.Zero, entryRadius, near, near) == 1);
    Check(MainThreadSampler.ResolveCalfContactMask(0, 3, true, true, Vector3.Zero, entryRadius, outside, near) == 2);
    Check(MainThreadSampler.ResolveCalfContactMask(0, 3, false, true, Vector3.Zero, entryRadius, near, null) == 0);
});
Run("Contact bone translation preserves descendant world positions through a rotated and scaled parent", () =>
{
    Matrix4x4 boneToWorld = Matrix4x4.CreateScale(.6f, 1.8f, .9f) *
        Matrix4x4.CreateRotationX(.7f) * Matrix4x4.CreateTranslation(3, 4, 5);
    var worldOffset = new Vector3(.003f, -.004f, .006f);
    Check(MainThreadSampler.TryCompensateContactChild(boneToWorld, worldOffset, out var correction));
    var child = new Vector3(.1f, .2f, -.3f);
    var shifted = boneToWorld * Matrix4x4.CreateTranslation(worldOffset);
    Check(Vector3.Distance(Vector3.Transform(child, boneToWorld), Vector3.Transform(child + correction, shifted)) < .00001f);
    Check(!MainThreadSampler.TryCompensateContactChild(Matrix4x4.CreateScale(0f), worldOffset, out _));
    Check(!MainThreadSampler.TryCompensateContactChild(boneToWorld, new Vector3(float.NaN), out _));
});
Run("Thigh coverage measures the weighted patch in hip-to-knee coordinates", () =>
{
    Vector3[] vertices = { new(.2f, .04f, 0), new(.5f, -.03f, 0), new(.8f, 0, 0), new(2, 0, 0) };
    Matrix4x4 transform = Matrix4x4.CreateRotationZ(MathF.PI / 2) * Matrix4x4.CreateTranslation(3, 4, 5);
    Check(MainThreadSampler.TryMeasureThighCoverage(vertices, new[] { 0, 1, 2 }, transform,
        new Vector3(3, 4, 5), Vector3.UnitY, out Vector3 range));
    Check(Vector3.Distance(range, new Vector3(.2f, .5f, .8f)) < .0001f);
    Check(!MainThreadSampler.TryMeasureThighCoverage(vertices, new[] { 0 }, transform, Vector3.Zero, Vector3.Zero, out _));
});
Run("Bilateral rig selection uses observed slots including right slot zero", () =>
{
    var candidates = new Dictionary<int, string> { [80] = MainThreadSampler.ContactLeftBonePath, [0] = MainThreadSampler.ContactRightBonePath };
    int[] influences = new int[110]; influences[80] = 1113; influences[0] = 1068;
    var both = MainThreadSampler.SelectContactRigSlots(candidates, influences, Array.Empty<string>());
    Check(both.Length == 2 && both[0].Slot == 80 && both[1].Slot == 0);
    var right = MainThreadSampler.SelectContactRigSlots(candidates, influences, new[] { MainThreadSampler.ContactLeftBonePath });
    Check(right.Length == 1 && right[0].Slot == 0 && right[0].Path == MainThreadSampler.ContactRightBonePath);
    influences[0] = 0;
    Check(MainThreadSampler.SelectContactRigSlots(candidates, influences, new[] { MainThreadSampler.ContactLeftBonePath }).Length == 0);
    candidates[65] = MainThreadSampler.ContactLeftBonePath;
    Check(MainThreadSampler.SelectContactRigSlots(candidates, influences, Array.Empty<string>()).Length == 0);
});
Run("Bilateral rig binding keeps the actor limit and rejects shared bones and duplicate sides", () =>
{
    var existing = new List<MainThreadSampler.ContactRigKey>();
    for (int actor = 1; actor <= 12; actor++)
        existing.Add(new MainThreadSampler.ContactRigKey((IntPtr)actor, (IntPtr)(actor * 10), MainThreadSampler.ContactLeftBonePath));
    Check(MainThreadSampler.CanBindContactRig(existing, (IntPtr)1, (IntPtr)11, MainThreadSampler.ContactRightBonePath));
    Check(!MainThreadSampler.CanBindContactRig(existing, (IntPtr)13, (IntPtr)130, MainThreadSampler.ContactLeftBonePath));
    Check(!MainThreadSampler.CanBindContactRig(existing, (IntPtr)1, (IntPtr)11, MainThreadSampler.ContactLeftBonePath));
    Check(!MainThreadSampler.CanBindContactRig(existing, (IntPtr)2, (IntPtr)10, MainThreadSampler.ContactRightBonePath));
    Check(!MainThreadSampler.CanBindContactRig(existing, (IntPtr)1, (IntPtr)11, "Hips/Pelvis"));
    for (int actor = 1; actor <= 12; actor++)
    {
        Check(MainThreadSampler.CanBindContactRig(existing, (IntPtr)actor, (IntPtr)(actor * 10 + 1), MainThreadSampler.ContactRightBonePath));
        existing.Add(new MainThreadSampler.ContactRigKey((IntPtr)actor, (IntPtr)(actor * 10 + 1), MainThreadSampler.ContactRightBonePath));
    }
    Check(existing.Count == 24);
});
Run("Bilateral contact keeps displacement and release state independent", () =>
{
    var left = new ContactResponse(); var right = new ContactResponse();
    var push = new Vector3(.01f, 0, 0);
    Check(left.Step(1, .016f, push, .025f, true, true).Length() > 0 && right.Offset == default);
    var priorLeft = left.Offset;
    Check(right.Step(1, .016f, -push, .025f, true, true).X < 0 && left.Offset == priorLeft);
    left.Step(2, .016f, default, .025f, false, true);
    Check(left.Offset == default && right.Offset.X < 0);
});
Run("Surface selection preserves vertex order and the dedicated bone slot", () =>
{
    byte[] counts = Enumerable.Repeat((byte)2, 16).ToArray();
    var weights = Enumerable.Range(0, 16).SelectMany(i => new[] {
        new MainThreadSampler.ContactBoneWeight(i < 12 ? .2f : .01f, 3),
        new MainThreadSampler.ContactBoneWeight(i < 12 ? .8f : .99f, 0) }).ToArray();
    var selected = MainThreadSampler.SelectContactSurfaceVertices(counts, weights, 3, 5);
    Check(selected is not null && selected.SequenceEqual(Enumerable.Range(0, 12)));
    Check(MainThreadSampler.SelectContactSurfaceVertices(counts, weights[..^1], 3, 5) is null);
    weights[0] = new MainThreadSampler.ContactBoneWeight(.2f, 9);
    Check(MainThreadSampler.SelectContactSurfaceVertices(counts, weights, 3, 5) is null);
});
Run("Release velocity rejects tracking loss and teleport and ignores artificial camera translation", () =>
{
    var identity = new TrackingQuaternion(0, 0, 0, 1);
    var raw = new TrackingStereoPose(new TrackingEyePose(new TrackingVector3(-.03f, 0, 0), identity),
        new TrackingEyePose(new TrackingVector3(.03f, 0, 0), identity));
    var origin = new Vector3(100, 20, -30);
    Check(ContactHandAlignment.TryCreate(raw, origin - new Vector3(.00825f, 0, 0), origin + new Vector3(.00825f, 0, 0),
        Quaternion.Identity, Quaternion.Identity, out var alignment));
    var hand = new TrackingVector3(0, 0, -.5f);
    Check(MainThreadSampler.ContactHandVelocity(alignment, hand, hand, .016f, out var velocity) && velocity.Length() < .0001f);
    Check(MainThreadSampler.ContactHandVelocity(alignment, new TrackingVector3(.1f, 0, -.5f), hand, .1f, out velocity));
    Check(MathF.Abs(velocity.X - alignment.Scale) < .001f);
    Check(!MainThreadSampler.ContactHandVelocity(alignment, new TrackingVector3(1, 0, -.5f), hand, .01f, out _));
    Check(!MainThreadSampler.ContactHandVelocity(alignment, null, hand, .016f, out _));
    Check(!MainThreadSampler.ContactHandVelocity(alignment, hand, hand, 0, out _));
});
Run("Contact matrices interpret Unity column-major translation and rotation", () =>
{
    float[] unity = { 0, 0, -2, 0, 0, 2, 0, 0, 2, 0, 0, 0, 5, 6, 7, 1 };
    IntPtr data = Marshal.AllocHGlobal(64);
    try
    {
        Marshal.Copy(unity, 0, data, unity.Length);
        Matrix4x4 matrix = Marshal.PtrToStructure<Matrix4x4>(data);
        Check(Vector3.Distance(Vector3.Transform(Vector3.UnitZ, matrix), new Vector3(7, 6, 7)) < .0001f);
        Check(ContactSurfaceAnchor.TryBind(new Vector3(7, 6, 7), Vector3.UnitX, .05f, matrix, out var anchor));
        Check(Vector3.Distance(anchor.LocalCenter, Vector3.UnitZ) < .0001f);
    }
    finally { Marshal.FreeHGlobal(data); }
});
Run("Contact markers and continuity use each actor's own contact state", () =>
{
    int[] actors = { 0, 1, 0, 0 };
    bool[] markers = actors.Select(mask => MainThreadSampler.ContactMarkerActive(true, mask, true)).ToArray();
    Check(markers.SequenceEqual(new[] { false, true, false, false }));
    Check(!MainThreadSampler.ContactMarkerActive(true, 0, true)); // Another actor's pulse cannot light this actor.
    Check(MainThreadSampler.ContactMarkerActive(false, 0, true)); // Periodic diagnostic keeps its explicit behavior.
    Check(!MainThreadSampler.ContactTargetContinuous(1, false, true));
    Check(MainThreadSampler.ContactTargetContinuous(2, false, true)); // Left-hand loss on actor A does not reset actor B.
    Check(MainThreadSampler.ContactTargetContinuous(0, true, false));
});
Run("Contact reserves physical input hands and handles swapped roles and failed reads", () =>
{
    var settings = VrSettings.CreateApprovedDefaults();
    int Mask(bool panel = false, float? grip = 0, bool toggle = false, float? trigger = 0,
        bool? primary = false, bool? secondary = false, float? worldGrip = 0) =>
        OpenXrControllerActions.ContactInputMask(settings, panel, grip, toggle, trigger, primary, secondary, worldGrip);
    Check(Mask() == 3 && Mask(panel: true) == 0);
    Check(Mask(grip: .3f) == 2 && Mask(trigger: .2f) == 1);
    Check(Mask(primary: true) == 1 && Mask(secondary: true) == 1 && Mask(worldGrip: .3f) == 1);
    Check(Mask(grip: null) == 2 && Mask(trigger: null) == 1 && Mask(primary: null) == 1 && Mask(worldGrip: null) == 1);
    settings.Panel.PanelHand = VrHand.Right; settings.Panel.PointerHand = VrHand.Left;
    Check(Mask(grip: .3f) == 1 && Mask(trigger: .2f) == 2 && Mask(worldGrip: null) == 3);
});
Run("Haptic requests merge per hand and expire or cancel on reservation", () =>
{
    OpenXrContactHaptics.Clear();
    OpenXrContactHaptics.Request(1, 1000); OpenXrContactHaptics.Request(1, 1000); OpenXrContactHaptics.Request(2, 1000);
    Check(OpenXrContactHaptics.Consume(3, 1050) == 3 && OpenXrContactHaptics.Consume(3, 1051) == 0);
    OpenXrContactHaptics.Request(3, 1000); Check(OpenXrContactHaptics.Consume(1, 1050) == 1);
    OpenXrContactHaptics.Request(3, 1000); Check(OpenXrContactHaptics.Consume(3, 1101) == 0);
    OpenXrContactHaptics.Request(1, 1000); OpenXrContactHaptics.Request(2, 1200); Check(OpenXrContactHaptics.Consume(3, 1250) == 2);
    OpenXrContactHaptics.Request(3, 1000); OpenXrContactHaptics.Clear(); Check(OpenXrContactHaptics.Consume(3, 1001) == 0);
    typeof(OpenXrControllerActions).GetMethod("ValidateAbi", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null);
});
Run("Contact defaults and independent marker visibility survive normalization and VR editing", () =>
{
    var settings = VrSettings.CreateApprovedDefaults();
    Check(settings.Tracking.ContactCollisionEnabled && settings.Tracking.ShowContactHands && !settings.Tracking.ShowContactLocations && settings.Tracking.ContactHapticsEnabled && settings.Tracking.ContactJiggleEnabled);
    var editor = new VrSettingsEditor(settings);
    editor.Entries.Single(e => e.Path == "Tracking.ContactCollisionEnabled").Change(1);
    editor.Entries.Single(e => e.Path == "Tracking.ShowContactLocations").Change(1);
    editor.Entries.Single(e => e.Path == "Tracking.ContactStrengthPercent").Change(1);
    Check(editor.Draft.Tracking.ContactThighElasticEnabled);
    editor.Entries.Single(e => e.Path == "Tracking.ContactThighElasticEnabled").Change(1);
    Check(!VrSettingsValidator.Validate(editor.Draft).Settings.Tracking.ContactThighElasticEnabled);
    Check(!editor.Draft.Tracking.ContactShowRanges);
    editor.Entries.Single(e => e.Path == "Tracking.ContactShowRanges").Change(1);
    Check(VrSettingsValidator.Validate(editor.Draft).Settings.Tracking.ContactShowRanges);
    editor.Entries.Single(e => e.Path == "Tracking.ContactHapticsEnabled").Change(1);
    editor.Entries.Single(e => e.Path == "Tracking.ContactJiggleEnabled").Change(1);
    var result = VrSettingsValidator.Validate(editor.Draft).Settings;
    Check(!result.Tracking.ContactCollisionEnabled && result.Tracking.ShowContactLocations && result.Tracking.ContactStrengthPercent == 45 &&
        !result.Tracking.ContactHapticsEnabled && !result.Tracking.ContactJiggleEnabled);
    Check(settings.Tracking.ContactCollisionEnabled && settings.Tracking.ShowContactHands && !settings.Tracking.ShowContactLocations && settings.Tracking.ContactHapticsEnabled && settings.Tracking.ContactJiggleEnabled);
});
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
Run("Startup, VR apply, and VR save require separate contact consent despite legacy enabled settings", () =>
{
    var previousSettings = VrSettingsRuntime.Current;
    string directory = Path.Combine(output, "consent-" + Guid.NewGuid().ToString("N"));
    string logs = Path.Combine(directory, "logs");
    string config = Path.Combine(directory, "config");
    Directory.CreateDirectory(logs);
    Directory.CreateDirectory(config);
    File.WriteAllText(Path.Combine(config, "settings.json"),
        "{\"tracking\":{\"contactCollisionEnabled\":true,\"contactBoneTrialEnabled\":true," +
        "\"contactHapticsEnabled\":true,\"contactJiggleEnabled\":true,\"contactThighElasticEnabled\":true," +
        "\"showContactLocations\":true,\"contactShowRanges\":true,\"contactConsentAccepted\":true}}");
    bool accepted = VrSettingsRuntime.ContactConsentGranted;
    var loaded = VrSettingsRuntime.Initialize(Path.Combine(logs, "runtime-bootstrap.jsonl"));
    Check(loaded.Tracking.ContactCollisionEnabled == accepted && loaded.Tracking.ContactBoneTrialEnabled == accepted);
    Check(loaded.Tracking.ShowContactHands);
    var draft = VrSettings.CreateApprovedDefaults();
    draft.Tracking.ContactBoneTrialEnabled = true;
    draft.Tracking.ShowContactLocations = draft.Tracking.ContactShowRanges = true;
    VrSettingsRuntime.Publish(draft);
    var tracking = VrSettingsRuntime.Current.Tracking;
    Check(tracking.ContactCollisionEnabled == accepted && tracking.ContactBoneTrialEnabled == accepted &&
        tracking.ContactHapticsEnabled == accepted && tracking.ContactJiggleEnabled == accepted &&
        tracking.ContactThighElasticEnabled == accepted && tracking.ShowContactLocations == accepted &&
        tracking.ContactShowRanges == accepted);
    Check(draft.Tracking.ContactCollisionEnabled && tracking.ShowContactHands);
    string savedPath = Path.Combine(config, "saved.json");
    VrSettingsRuntime.Save(draft, savedPath);
    JsonObject saved = JsonNode.Parse(File.ReadAllText(savedPath))!.AsObject();
    Check(saved["tracking"]!["contactCollisionEnabled"]!.GetValue<bool>() == accepted);
    Check(saved["tracking"]!["contactBoneTrialEnabled"]!.GetValue<bool>() == accepted);
    VrSettingsRuntime.Publish(previousSettings);
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
Run("Collision is requested across stereo scenes while the synthetic trial stays in its classroom", () =>
{
    Check(MainThreadSampler.IsContactRigRequested("env_3d_home_classroom-01-00-noon", false, true));
    Check(MainThreadSampler.IsContactRigRequested("env_3d_home_classroom-01-00-evening", false, true));
    Check(MainThreadSampler.IsContactRigRequested("ENV_3D_HOME_CLASSROOM-01-00-NIGHT", false, true));
    foreach (string scene in new[] { "env_3d_home_classroom-02-00-evening", "env_3d_adv_consultingroom-00-00-noon", "env_3d_live" })
    {
        Check(MainThreadSampler.IsContactRigRequested(scene, true, false));
        Check(!MainThreadSampler.IsContactRigRequested(scene, false, true));
        Check(!MainThreadSampler.IsContactRigRequested(scene, false, false));
    }
    Check(MainThreadSampler.ContactEyeScopesReady(3, true));
    Check(!MainThreadSampler.ContactEyeScopesReady(5, true));
    Check(!MainThreadSampler.ContactEyeScopesReady(3, false));
    Check(MainThreadSampler.ContactEyeScopesReady(7, false));
});
Run("Contact rig evidence requires observed names and preserves sparse skin bone slots", () =>
{
    Check(MainThreadSampler.IsObservedContactChestBone("LeftBust1_S", "Spine2"));
    Check(MainThreadSampler.IsObservedContactChestBone("RightBust1_S", "Spine2"));
    Check(!MainThreadSampler.IsObservedContactChestBone("LeftBust1_S", "Hips"));
    Check(!MainThreadSampler.IsObservedContactChestBone("Hips", "Reference"));
    Check(!MainThreadSampler.IsObservedContactChestBone("Chest", "Spine2"));
    int[]? counts = MainThreadSampler.CountContactBoneInfluences(
        new MainThreadSampler.ContactBoneWeight[] { new(.4f, 0), new(.6f, 3), new(1f, 3) }, 4);
    Check(counts is not null && counts.SequenceEqual(new[] { 1, 0, 0, 2 }));
    Check(MainThreadSampler.CountContactBoneInfluences(
        new MainThreadSampler.ContactBoneWeight[] { new(1f, 4) }, 4) is null);
    Check(MainThreadSampler.CountContactBoneInfluences(
        new MainThreadSampler.ContactBoneWeight[] { new(float.NaN, 3) }, 4) is null);
});
Run("Contact trial claims its staged position and preserves animation overwrites", () =>
{
    var before = new Vector3(0.1f, 0.2f, 0.3f);
    var intended = before + new Vector3(0.01f, 0f, 0f);
    var lease = new MainThreadSampler.ContactTrialPosition(before, intended);
    Check(lease.Owns(intended));
    Check(!lease.Owns(before));
    Check(!lease.Owns(new Vector3(MathF.BitIncrement(intended.X), intended.Y, intended.Z)));
    Check(!VrSettings.CreateApprovedDefaults().Tracking.ContactBoneTrialEnabled);
});
Run("IL2CPP object roots retain native pointer width through release", () =>
{
    Check(typeof(Il2CppApi.GcHandleNewDelegate).GetMethod("Invoke")!.ReturnType == typeof(IntPtr));
    Check(typeof(MainThreadSampler.ContactFreeHandle).GetMethod("Invoke")!.GetParameters()[0].ParameterType == typeof(IntPtr));
    Check(typeof(MainThreadSampler).GetMethod("RootObject", BindingFlags.Instance | BindingFlags.NonPublic)!.ReturnType == typeof(IntPtr));
});
Run("Multi-actor trial restores the other leases when one actor must retry", () =>
{
    var pending = new List<int> { 1, 2, 3 };
    var attempted = new List<int>();
    MainThreadSampler.RestoreContactTrialEntries(attempted, pending, static (calls, actor) =>
    {
        calls.Add(actor);
        return actor != 2;
    });
    Check(attempted.SequenceEqual(new[] { 3, 2, 1 }));
    Check(pending.SequenceEqual(new[] { 2 }));
    MainThreadSampler.RestoreContactTrialEntries(attempted, pending, static (_, actor) => actor == 2);
    Check(pending.Count == 0);
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
