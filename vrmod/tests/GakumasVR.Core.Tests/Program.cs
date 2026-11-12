using GakumasVR.Core;
using System.Text.Json;

var tests = new (string Name, Action Run)[]
{
    ("Reference chest profile applies defaults and side detuning", ReferenceContactProfile),
    ("Contact compression preserves volume and skips uncertain axes", ContactVolume),
    ("Thigh elasticity adds limited tilt and compression while keeping the position limit", ThighElasticity),
    ("Contact surface fits the visible weighted patch", SurfacePatch),
    ("Contact surface follows bone rotation and scale", SurfaceAnchor),
    ("Contact surface rejects ambiguous geometry", SurfaceRejectsInvalid),
    ("Contact jiggle overshoots and decays after release", ContactJiggle),
    ("Release impulses require a tracked exit and integrate once", ContactRelease),
    ("Sphere contact uses rendered distances and handles overlap", SphereContact),
    ("Contact spring shares a pair and settles after tracked release", ContactSpring),
    ("Tracking loss and time discontinuity cancel contact inertia", ContactDiscontinuity),
    ("Contact entry rearms only after valid separation", ContactEntry),
    ("Contact consent requires a saved current notice and fails closed on unreadable records", ContactConsentStorage),
    ("Contact consent blocks every reaction and VR setting while preserving hand display", ContactConsentGate),
    ("Shared translations cover every VR setting and enum", SharedTranslations),
    ("GUI language preference preserves legacy and culture fallbacks", LanguagePreferences),
    ("Comparison bloom preserves authored diffusion and follows FOV", ComparisonBloom),
    ("Grip distinguishes short long and tracking loss", GripGestures),
    ("VR editor exposes all settings without mutating source", SettingsEditor),
    ("VR editor keeps role pairs and ranges valid", SettingsEditorValidation),
    ("VR menu layout rejects gaps and edges", MenuLayout),
    ("Transition takes priority", TransitionTakesPriority),
    ("Video takes priority over 3D", VideoTakesPriority),
    ("Unapproved live uses safe panel", UnapprovedLiveUsesPanel),
    ("Approved live enables immersive", ApprovedLiveEnablesImmersive),
    ("UI-only scene uses panel", UiOnlyUsesPanel),
    ("Orientation waits and rebinds", OrientationWaitsAndRebinds),
    ("Render dimensions override stale Unity orientation", DimensionsOverrideStaleUnityOrientation),
    ("Orientation timeout falls back", OrientationTimeoutFallsBack),
    ("Failed rebind falls back", FailedRebindFallsBack),
    ("Grip toggle requires release edge", GripToggleRequiresReleaseEdge),
    ("Grip toggle rejects shallow and debounced presses", GripToggleRejectsShallowAndDebouncedPresses),
    ("Grip toggle supports configured initial state", GripToggleSupportsInitialState),
    ("Analog click latches before the press threshold", AnalogClickLatchesBeforePress),
    ("Analog click cancels shallow presses", AnalogClickCancelsShallowPress),
    ("Stereo startup waits for frames and Present", StereoStartupWaitsForFramesAndPresent),
    ("Stereo startup reset requires a fresh generation", StereoStartupResetRequiresFreshGeneration),
    ("Black stereo frames retry before timeout", BlackStereoFramesRetryBeforeTimeout),
    ("Black stereo frame policy resets per generation", BlackStereoFramePolicyResetsPerGeneration),
    ("6DoF origin preserves configured eye separation", SixDofOriginPreservesEyeSeparation),
    ("6DoF maps physical translation into Unity space", SixDofMapsPhysicalTranslation),
    ("6DoF maps relative head rotation into Unity space", SixDofMapsRelativeRotation),
    ("6DoF separates physical roll from yaw at a tilted origin", SixDofSeparatesPhysicalRoll),
    ("6DoF reset captures a fresh scene origin", SixDofResetCapturesFreshOrigin),
    ("6DoF recenter clears yaw pitch and roll while preserving valid position tracking", SixDofRecentersCurrentHeadPose),
    ("Locomotion moves in the full 3D view direction", LocomotionMovesInViewDirection),
    ("Locomotion follows upward and downward view pitch", LocomotionFollowsViewPitch),
    ("Locomotion applies radial deadzone and frame clamp", LocomotionAppliesDeadzoneAndFrameClamp),
    ("Locomotion reset clears the scene offset", LocomotionResetClearsSceneOffset),
    ("View turn changes movement direction", ViewTurnChangesMovementDirection),
    ("View turn keeps world yaw and pitch cardinal", ViewTurnKeepsWorldAxesCardinal),
    ("World view composition removes scene camera roll", WorldViewCompositionRemovesSceneRoll),
    ("Snap turn advances once per stick deflection", SnapTurnAdvancesOncePerDeflection),
    ("View turn reset clears artificial rotation", ViewTurnResetClearsRotation),
    ("VR settings preserve product defaults", VrSettingsPreserveProductDefaults),
    ("VR settings allow 2x eye render scale", VrSettingsAllowTwoTimesEyeRenderScale),
    ("VR settings preserve manual VFX controls", VrSettingsPreserveManualVfxControls),
    ("VR settings load legacy JSON without manual VFX", VrSettingsLoadLegacyJsonWithoutManualVfx),
    ("VR settings load swapped movement hand", VrSettingsLoadSwappedMovementHand),
    ("VR settings repair invalid roles and ranges", VrSettingsRepairInvalidValues),
    ("VR settings reject unsupported schema", VrSettingsRejectUnsupportedSchema),
    ("Spatial defaults preserve baseline", SpatialDefaultsPreserveBaseline),
    ("Spatial auto inverts character size", SpatialAutoInvertsCharacterSize),
    ("Spatial manual values override independently", SpatialManualOverridesIndependently),
    ("Spatial validation repairs invalid values", SpatialValidationRepairsInvalidValues)
};

var failed = 0;
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception exception)
    {
        failed++;
        Console.Error.WriteLine($"FAIL {test.Name}: {exception.Message}");
    }
}

Console.WriteLine($"Executed {tests.Length} tests; failures: {failed}");
return failed == 0 ? 0 : 1;

static void ContactConsentStorage()
{
    string directory = Path.Combine(Path.GetTempPath(), "GakumasVR-consent-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    string path = Path.Combine(directory, "character-contact-consent.txt");
    try
    {
        True(!ContactConsent.IsAccepted(directory));
        File.WriteAllText(path, "invalid");
        True(!ContactConsent.IsAccepted(directory));
        File.WriteAllText(path, "character-contact-v1");
        True(!ContactConsent.IsAccepted(directory));
        True(ContactConsent.TryAccept(directory));
        True(ContactConsent.IsAccepted(directory));
        True(ContactConsent.TryAccept(directory));
        File.Delete(path);
        Directory.CreateDirectory(path);
        True(!ContactConsent.IsAccepted(directory));
        True(!ContactConsent.TryAccept(directory));
    }
    finally { Directory.Delete(directory, recursive: true); }
}

static void ContactConsentGate()
{
    var settings = VrSettings.CreateApprovedDefaults();
    settings.Tracking.ContactBoneTrialEnabled = true;
    settings.Tracking.ShowContactLocations = true;
    settings.Tracking.ContactShowRanges = true;
    ContactConsent.Enforce(settings.Tracking, accepted: true);
    True(settings.Tracking.ContactBoneTrialEnabled && settings.Tracking.ContactCollisionEnabled);
    var locked = new VrSettingsEditor(settings, includeContactSettings: false);
    True(locked.Entries.All(entry => !ContactConsent.IsContactSetting(entry.Path)));
    True(locked.Entries.Any(entry => entry.Path == "Tracking.ShowContactHands"));
    var tracking = locked.Draft.Tracking;
    True(tracking.ShowContactHands && tracking.WorldGripEnabled && tracking.LocomotionEnabled);
    True(!tracking.ContactBoneTrialEnabled && !tracking.ContactCollisionEnabled && !tracking.ContactHapticsEnabled &&
        !tracking.ContactJiggleEnabled && !tracking.ContactThighElasticEnabled &&
        !tracking.ShowContactLocations && !tracking.ContactShowRanges);
    var unlocked = new VrSettingsEditor(settings, includeContactSettings: true);
    True(unlocked.Entries.Any(entry => entry.Path == "Tracking.ContactCollisionEnabled"));
    True(unlocked.Draft.Tracking.ContactCollisionEnabled);
}

static void SpatialDefaultsPreserveBaseline()
{
    SpatialScaleMultipliers result = SpatialScaleResolver.Resolve(VrSettings.CreateApprovedDefaults().Spatial.Live);
    Equal(1f, result.EyeOffsetMultiplier);
    Equal(1f, result.HeadTranslationMultiplier);
    Equal(1f, result.LocomotionMultiplier);
}

static void SpatialAutoInvertsCharacterSize()
{
    SpatialScaleMultipliers result = SpatialScaleResolver.Resolve(new VrSpatialScaleProfile { PerceivedCharacterScale = 1.5f });
    Near(2f / 3f, result.EyeOffsetMultiplier);
    Near(2f / 3f, result.HeadTranslationMultiplier);
    Near(2f / 3f, result.LocomotionMultiplier);
}

static void SpatialManualOverridesIndependently()
{
    SpatialScaleMultipliers result = SpatialScaleResolver.Resolve(new VrSpatialScaleProfile
    {
        PerceivedCharacterScale = 2f,
        EyeOffsetMode = SpatialScaleMode.Manual,
        EyeOffsetMultiplier = 1.25f,
        LocomotionMode = SpatialScaleMode.Manual,
        LocomotionMultiplier = 0.5f
    });
    Equal(1.25f, result.EyeOffsetMultiplier);
    Equal(0.5f, result.HeadTranslationMultiplier);
    Equal(0.5f, result.LocomotionMultiplier);
}

static void SpatialValidationRepairsInvalidValues()
{
    VrSettings settings = VrSettings.CreateApprovedDefaults();
    settings.Spatial.Live.PerceivedCharacterScale = 5f;
    settings.Spatial.NonLive.EyeOffsetMultiplier = float.NaN;
    VrSettingsValidationResult result = VrSettingsValidator.Validate(settings);
    True(result.UsedFallback);
    Equal(1f, result.Settings.Spatial.Live.PerceivedCharacterScale);
    Equal(1f, result.Settings.Spatial.NonLive.EyeOffsetMultiplier);
}

static RenderObservation StableObservation()
{
    return new RenderObservation
    {
        AreRenderTargetsStable = true,
        StableFrameCount = 5,
        HasValidWorldCamera = true,
        IsUrpCameraStackValid = true,
        WorldCameraCount = 1
    };
}

static void GripToggleRequiresReleaseEdge()
{
    var toggle = new GripToggleLatch(0.72f, 0.25f, 10);
    Equal(false, toggle.Enabled);
    Equal(true, toggle.Update(true, 0.80f, 100));
    Equal(true, toggle.Enabled);
    Equal(false, toggle.Update(true, 0.95f, 120));
    Equal(false, toggle.Update(true, 0.50f, 130));
    Equal(false, toggle.Update(true, 0.20f, 140));
    Equal(true, toggle.Update(true, 0.80f, 150));
    Equal(false, toggle.Enabled);
}

static void GripToggleRejectsShallowAndDebouncedPresses()
{
    var toggle = new GripToggleLatch(0.72f, 0.25f, 100);
    Equal(false, toggle.Update(true, 0.60f, 100));
    Equal(false, toggle.Enabled);
    Equal(true, toggle.Update(true, 0.80f, 110));
    Equal(false, toggle.Update(true, 0.10f, 120));
    Equal(false, toggle.Update(true, 0.80f, 130));
    Equal(false, toggle.Update(true, 0.10f, 140));
    Equal(true, toggle.Update(true, 0.80f, 220));
    Equal(false, toggle.Enabled);
}

static void GripToggleSupportsInitialState()
{
    var toggle = new GripToggleLatch(0.72f, 0.25f, 10, initialEnabled: true);
    True(toggle.Enabled);
    True(toggle.Update(active: true, value: 0.80f, timestamp: 0));
    False(toggle.Enabled);
}

static void AnalogClickLatchesBeforePress()
{
    var latch = new AnalogPressLatch(0.15f, 0.72f, 0.10f, 0.25f);
    Equal(AnalogPressTransition.None, latch.Update(true, 0.10f));
    Equal(AnalogPressTransition.Armed, latch.Update(true, 0.20f));
    True(latch.IsArmed);
    Equal(AnalogPressTransition.None, latch.Update(true, 0.60f));
    Equal(AnalogPressTransition.Pressed, latch.Update(true, 0.80f));
    True(latch.IsPressed);
    Equal(AnalogPressTransition.None, latch.Update(true, 0.50f));
    Equal(AnalogPressTransition.Released, latch.Update(true, 0.20f));
    False(latch.IsPressed);
}

static void AnalogClickCancelsShallowPress()
{
    var latch = new AnalogPressLatch(0.15f, 0.72f, 0.10f, 0.25f);
    Equal(AnalogPressTransition.Armed, latch.Update(true, 0.30f));
    Equal(AnalogPressTransition.None, latch.Update(true, 0.05f));
    False(latch.IsArmed);
    Equal(AnalogPressTransition.Armed, latch.Update(true, 0.20f));
    False(latch.Cancel());
    False(latch.IsArmed);
}

static void StereoStartupWaitsForFramesAndPresent()
{
    var gate = new StereoStartupGate(requiredStableFrames: 2);
    gate.Arm(frameCount: 100, presentSerial: 50);

    False(gate.IsReady(frameCount: 101, presentSerial: 51));
    False(gate.IsReady(frameCount: 102, presentSerial: 50));
    True(gate.IsReady(frameCount: 102, presentSerial: 51));
}

static void StereoStartupResetRequiresFreshGeneration()
{
    var gate = new StereoStartupGate(requiredStableFrames: 2);
    gate.Arm(frameCount: 100, presentSerial: 50);
    True(gate.IsReady(frameCount: 102, presentSerial: 51));

    gate.Reset();
    False(gate.IsReady(frameCount: 200, presentSerial: 100));

    gate.Arm(frameCount: 200, presentSerial: 100);
    False(gate.IsReady(frameCount: 201, presentSerial: 101));
    True(gate.IsReady(frameCount: 202, presentSerial: 101));
}

static void BlackStereoFramesRetryBeforeTimeout()
{
    var policy = new StereoBlackFrameRetryPolicy(
        maximumAttempts: 3,
        timeoutMilliseconds: 1_000);
    Equal(StereoBlackFrameDecision.Retry, policy.ObserveBlack(100));
    Equal(StereoBlackFrameDecision.Retry, policy.ObserveBlack(200));
    Equal(StereoBlackFrameDecision.TimedOut, policy.ObserveBlack(300));
    Equal(3, policy.AttemptCount);
}

static void BlackStereoFramePolicyResetsPerGeneration()
{
    var policy = new StereoBlackFrameRetryPolicy(
        maximumAttempts: 3,
        timeoutMilliseconds: 100);
    Equal(StereoBlackFrameDecision.Retry, policy.ObserveBlack(1_000));
    Equal(StereoBlackFrameDecision.TimedOut, policy.ObserveBlack(1_101));
    policy.Reset();
    Equal(0, policy.AttemptCount);
    Equal(StereoBlackFrameDecision.Retry, policy.ObserveBlack(2_000));
}

static void SixDofOriginPreservesEyeSeparation()
{
    var mapper = new SixDofPoseMapper();
    True(mapper.TryMap(
        StereoPose(-0.03f, 0f, 0f, 0.03f, 0f, 0f),
        headTranslationScale: 1f,
        eyeOffsetScale: 0.5f,
        out UnityStereoPose mapped));

    Near(-0.015f, mapped.Left.LocalPosition.X);
    Near(0.015f, mapped.Right.LocalPosition.X);
    Near(0f, mapped.Left.LocalPosition.Y);
    Near(0f, mapped.Left.LocalPosition.Z);
    Near(1f, mapped.Left.LocalRotation.W);
}

static void SixDofMapsPhysicalTranslation()
{
    var mapper = new SixDofPoseMapper();
    True(mapper.TryMap(
        StereoPose(-0.03f, 0f, 0f, 0.03f, 0f, 0f),
        1f,
        1f,
        out _));
    True(mapper.TryMap(
        StereoPose(0.17f, 0.10f, -0.50f, 0.23f, 0.10f, -0.50f),
        1f,
        1f,
        out UnityStereoPose mapped));

    Near(0.17f, mapped.Left.LocalPosition.X);
    Near(0.23f, mapped.Right.LocalPosition.X);
    Near(0.10f, mapped.Left.LocalPosition.Y);
    Near(0.50f, mapped.Left.LocalPosition.Z);
}

static void SixDofMapsRelativeRotation()
{
    var mapper = new SixDofPoseMapper();
    True(mapper.TryMap(
        StereoPose(-0.03f, 0f, 0f, 0.03f, 0f, 0f),
        1f,
        1f,
        out _));
    float halfAngle = MathF.PI * 0.25f;
    TrackingQuaternion yaw = new(0f, MathF.Sin(halfAngle), 0f, MathF.Cos(halfAngle));
    True(mapper.TryMap(
        StereoPose(-0.03f, 0f, 0f, 0.03f, 0f, 0f, yaw),
        1f,
        1f,
        out UnityStereoPose mapped));

    Near(-MathF.Sin(halfAngle), mapped.Left.LocalRotation.Y);
    Near(MathF.Cos(halfAngle), mapped.Left.LocalRotation.W);
}

static void SixDofResetCapturesFreshOrigin()
{
    var mapper = new SixDofPoseMapper();
    True(mapper.TryMap(
        StereoPose(-0.03f, 0f, 0f, 0.03f, 0f, 0f),
        1f,
        1f,
        out _));
    mapper.Reset();
    True(mapper.TryMap(
        StereoPose(1.97f, 1f, -3f, 2.03f, 1f, -3f),
        1f,
        1f,
        out UnityStereoPose mapped));

    Near(-0.03f, mapped.Left.LocalPosition.X);
    Near(0f, mapped.Left.LocalPosition.Y);
    Near(0f, mapped.Left.LocalPosition.Z);
}

static void SixDofRecentersCurrentHeadPose()
{
    var mapper = new SixDofPoseMapper();
    True(mapper.TryMap(StereoPose(-0.03f, 1f, 0f, 0.03f, 1f, 0f), 1f, 1f, out _));
    TrackingQuaternion turned = MultiplyTestQuaternion(AxisAngle(0f, 1f, 0f, 45f),
        MultiplyTestQuaternion(AxisAngle(1f, 0f, 0f, 20f), AxisAngle(0f, 0f, 1f, 15f)));
    TrackingStereoPose current = StereoPose(1.97f, 2f, -3f, 2.03f, 2f, -3f, UnityToTracking(turned));
    True(mapper.TryRecenter(current, 1f, 1f, positionTracked: true, out UnityStereoPose recentered));
    Near(0f, recentered.Left.YawDeltaRadians);
    Near(0f, recentered.Left.PitchDeltaRadians);
    Near(0f, recentered.Left.RollDeltaRadians);
    Near(0f, recentered.Left.LocalPosition.X + recentered.Right.LocalPosition.X);
    Near(0f, recentered.Left.LocalPosition.Y + recentered.Right.LocalPosition.Y);
    Near(0f, recentered.Left.LocalPosition.Z + recentered.Right.LocalPosition.Z);

    TrackingQuaternion later = MultiplyTestQuaternion(AxisAngle(0f, 1f, 0f, 15f), turned);
    TrackingStereoPose laterPose = StereoPose(1.97f, 2f, -3f, 2.03f, 2f, -3f, UnityToTracking(later));
    True(mapper.TryMap(laterPose, 1f, 1f, out UnityStereoPose moved));
    Near(MathF.PI / 12f, moved.Left.YawDeltaRadians);

    TrackingStereoPose untracked = StereoPose(0f, 0f, 0f, 0f, 0f, 0f, UnityToTracking(later));
    True(mapper.TryRecenter(untracked, 0f, 1f, positionTracked: false, out UnityStereoPose rotationOnly));
    Near(0f, rotationOnly.Left.YawDeltaRadians);
    True(mapper.TryMap(laterPose, 1f, 1f, out UnityStereoPose recovered));
    Near(0f, recovered.Left.LocalPosition.X + recovered.Right.LocalPosition.X);
    Near(0f, recovered.Left.LocalPosition.Y + recovered.Right.LocalPosition.Y);
    Near(0f, recovered.Left.LocalPosition.Z + recovered.Right.LocalPosition.Z);
}

static void SixDofSeparatesPhysicalRoll()
{
    var mapper = new SixDofPoseMapper();
    TrackingQuaternion originUnity = AxisAngle(0f, 0f, 1f, 20f);
    True(mapper.TryMap(
        StereoPose(
            -0.03f,
            0f,
            0f,
            0.03f,
            0f,
            0f,
            UnityToTracking(originUnity)),
        1f,
        1f,
        out _));

    TrackingQuaternion yawWithSameRoll = MultiplyTestQuaternion(
        AxisAngle(0f, 1f, 0f, 45f),
        originUnity);
    True(mapper.TryMap(
        StereoPose(
            -0.03f,
            0f,
            0f,
            0.03f,
            0f,
            0f,
            UnityToTracking(yawWithSameRoll)),
        1f,
        1f,
        out UnityStereoPose yawMapped));
    Near(MathF.PI / 4f, yawMapped.Left.YawDeltaRadians);
    Near(0f, yawMapped.Left.RollDeltaRadians);

    TrackingQuaternion yawWithAddedRoll = MultiplyTestQuaternion(
        AxisAngle(0f, 1f, 0f, 45f),
        AxisAngle(0f, 0f, 1f, 35f));
    True(mapper.TryMap(
        StereoPose(
            -0.03f,
            0f,
            0f,
            0.03f,
            0f,
            0f,
            UnityToTracking(yawWithAddedRoll)),
        1f,
        1f,
        out UnityStereoPose rollMapped));
    Near(MathF.PI / 12f, rollMapped.Left.RollDeltaRadians);
}

static void LocomotionMovesInViewDirection()
{
    var locomotion = new VrLocomotionIntegrator();
    TrackingQuaternion identity = new(0f, 0f, 0f, 1f);
    True(locomotion.Update(0f, 1f, identity, 0.10f, 2f));
    Near(0f, locomotion.Offset.X);
    Near(0f, locomotion.Offset.Y);
    Near(0.20f, locomotion.Offset.Z);

    float halfAngle = MathF.PI * 0.25f;
    TrackingQuaternion rightFacing = new(
        0f,
        MathF.Sin(halfAngle),
        0f,
        MathF.Cos(halfAngle));
    True(locomotion.Update(0f, 1f, rightFacing, 0.10f, 2f));
    Near(0.20f, locomotion.Offset.X);
    Near(0.20f, locomotion.Offset.Z);
}

static void LocomotionFollowsViewPitch()
{
    float halfAngle = MathF.PI * 0.25f;
    TrackingQuaternion lookingUp = new(
        -MathF.Sin(halfAngle),
        0f,
        0f,
        MathF.Cos(halfAngle));
    var upward = new VrLocomotionIntegrator();
    True(upward.Update(0f, 1f, lookingUp, 0.10f, 2f));
    Near(0.20f, upward.Offset.Y);
    Near(0f, upward.Offset.Z);

    TrackingQuaternion lookingDown = new(
        MathF.Sin(halfAngle),
        0f,
        0f,
        MathF.Cos(halfAngle));
    var downward = new VrLocomotionIntegrator();
    True(downward.Update(0f, 1f, lookingDown, 0.10f, 2f));
    Near(-0.20f, downward.Offset.Y);
    Near(0f, downward.Offset.Z);
}

static void LocomotionAppliesDeadzoneAndFrameClamp()
{
    var locomotion = new VrLocomotionIntegrator();
    TrackingQuaternion identity = new(0f, 0f, 0f, 1f);
    True(locomotion.Update(0.10f, 0.10f, identity, 1f, 5f));
    Near(0f, locomotion.Offset.X);
    Near(0f, locomotion.Offset.Z);

    True(locomotion.Update(1f, 0f, identity, 1f, 5f));
    Near(0.50f, locomotion.Offset.X);
    Near(0f, locomotion.Offset.Z);
}

static void LocomotionResetClearsSceneOffset()
{
    var locomotion = new VrLocomotionIntegrator();
    True(locomotion.Update(
        1f,
        0f,
        new TrackingQuaternion(0f, 0f, 0f, 1f),
        0.10f,
        1f));
    locomotion.Reset();
    Near(0f, locomotion.Offset.X);
    Near(0f, locomotion.Offset.Y);
    Near(0f, locomotion.Offset.Z);
}

static void ViewTurnChangesMovementDirection()
{
    var turn = new VrViewTurnIntegrator();
    for (int index = 0; index < 5; index++)
    {
        True(turn.Update(
            1f,
            0f,
            0.10f,
            VrViewTurnMode.Smooth,
            180f,
            15,
            0f));
    }
    var locomotion = new VrLocomotionIntegrator();
    True(locomotion.Update(0f, 1f, turn.Rotation, 0.10f, 2f));
    Near(0.20f, locomotion.Offset.X);
    Near(0f, locomotion.Offset.Z);
}

static void ViewTurnKeepsWorldAxesCardinal()
{
    var turn = new VrViewTurnIntegrator();
    True(turn.Update(
        0f,
        1f,
        0.10f,
        VrViewTurnMode.Smooth,
        300f,
        15,
        0f));
    // The small off-axis Y component must not add pitch during a yaw input.
    True(turn.Update(
        1f,
        0.30f,
        0.10f,
        VrViewTurnMode.Smooth,
        900f,
        15,
        0f));

    var locomotion = new VrLocomotionIntegrator();
    True(locomotion.Update(0f, 1f, turn.Rotation, 0.10f, 2f));
    Near(0.20f * MathF.Cos(MathF.PI / 6f), locomotion.Offset.X);
    Near(0.10f, locomotion.Offset.Y);
    Near(0f, locomotion.Offset.Z);
}

static void SnapTurnAdvancesOncePerDeflection()
{
    var turn = new VrViewTurnIntegrator();
    True(turn.Update(1f, 0f, 0.10f, VrViewTurnMode.Snap, 90f, 15));
    True(turn.Update(1f, 0f, 0.10f, VrViewTurnMode.Snap, 90f, 15));

    var first = new VrLocomotionIntegrator();
    True(first.Update(0f, 1f, turn.Rotation, 0.10f, 2f));
    Near(0.20f * MathF.Sin(MathF.PI / 12f), first.Offset.X);
    Near(0.20f * MathF.Cos(MathF.PI / 12f), first.Offset.Z);

    True(turn.Update(0f, 0f, 0.10f, VrViewTurnMode.Snap, 90f, 15));
    True(turn.Update(1f, 0f, 0.10f, VrViewTurnMode.Snap, 90f, 15));
    var second = new VrLocomotionIntegrator();
    True(second.Update(0f, 1f, turn.Rotation, 0.10f, 2f));
    Near(0.10f, second.Offset.X);
    Near(0.20f * MathF.Cos(MathF.PI / 6f), second.Offset.Z);
}

static void WorldViewCompositionRemovesSceneRoll()
{
    TrackingQuaternion baseRotation = MultiplyTestQuaternion(
        AxisAngle(0f, 1f, 0f, 40f),
        MultiplyTestQuaternion(
            AxisAngle(1f, 0f, 0f, -20f),
            AxisAngle(0f, 0f, 1f, 25f)));
    var turn = new VrViewTurnIntegrator();
    True(turn.TryCreateWorldNavigationRotation(baseRotation, out TrackingQuaternion leveled));

    TrackingVector3 originalForward = RotateTestVector(
        baseRotation,
        new TrackingVector3(0f, 0f, 1f));
    TrackingVector3 leveledForward = RotateTestVector(
        leveled,
        new TrackingVector3(0f, 0f, 1f));
    TrackingVector3 leveledRight = RotateTestVector(
        leveled,
        new TrackingVector3(1f, 0f, 0f));
    Near(originalForward.X, leveledForward.X);
    Near(originalForward.Y, leveledForward.Y);
    Near(originalForward.Z, leveledForward.Z);
    Near(0f, leveledRight.Y);

    True(turn.Update(1f, 0.1f, 0.10f, VrViewTurnMode.Snap, 90f, 30));
    True(turn.TryCreateWorldNavigationRotation(baseRotation, out TrackingQuaternion turned));
    TrackingVector3 turnedRight = RotateTestVector(
        turned,
        new TrackingVector3(1f, 0f, 0f));
    Near(0f, turnedRight.Y);

    True(turn.TryCreateWorldViewRotation(
        baseRotation,
        physicalYawDeltaRadians: MathF.PI / 6f,
        physicalPitchDeltaRadians: 0f,
        physicalRollDeltaRadians: MathF.PI / 12f,
        out TrackingQuaternion physicalRollView));
    TrackingVector3 physicalRight = RotateTestVector(
        physicalRollView,
        new TrackingVector3(1f, 0f, 0f));
    TrackingVector3 physicalUp = RotateTestVector(
        physicalRollView,
        new TrackingVector3(0f, 1f, 0f));
    Near(
        MathF.PI / 12f,
        MathF.Atan2(physicalRight.Y, physicalUp.Y));
}

static void ViewTurnResetClearsRotation()
{
    var turn = new VrViewTurnIntegrator();
    True(turn.Update(
        1f,
        1f,
        0.10f,
        VrViewTurnMode.Smooth,
        90f,
        15,
        0f));
    turn.Reset();
    Near(0f, turn.Rotation.X);
    Near(0f, turn.Rotation.Y);
    Near(0f, turn.Rotation.Z);
    Near(1f, turn.Rotation.W);
}

static TrackingStereoPose StereoPose(
    float leftX,
    float leftY,
    float leftZ,
    float rightX,
    float rightY,
    float rightZ,
    TrackingQuaternion? orientation = null)
{
    TrackingQuaternion rotation = orientation ?? new TrackingQuaternion(0f, 0f, 0f, 1f);
    return new TrackingStereoPose(
        new TrackingEyePose(new TrackingVector3(leftX, leftY, leftZ), rotation),
        new TrackingEyePose(new TrackingVector3(rightX, rightY, rightZ), rotation));
}

static TrackingQuaternion AxisAngle(float x, float y, float z, float degrees)
{
    float halfRadians = degrees * MathF.PI / 360f;
    float sine = MathF.Sin(halfRadians);
    return new TrackingQuaternion(
        x * sine,
        y * sine,
        z * sine,
        MathF.Cos(halfRadians));
}

static TrackingQuaternion UnityToTracking(TrackingQuaternion value) =>
    new(-value.X, -value.Y, value.Z, value.W);

static TrackingQuaternion MultiplyTestQuaternion(
    TrackingQuaternion left,
    TrackingQuaternion right) => new(
        (left.W * right.X) + (left.X * right.W) +
            (left.Y * right.Z) - (left.Z * right.Y),
        (left.W * right.Y) - (left.X * right.Z) +
            (left.Y * right.W) + (left.Z * right.X),
        (left.W * right.Z) + (left.X * right.Y) -
            (left.Y * right.X) + (left.Z * right.W),
        (left.W * right.W) - (left.X * right.X) -
            (left.Y * right.Y) - (left.Z * right.Z));

static TrackingVector3 RotateTestVector(
    TrackingQuaternion rotation,
    TrackingVector3 value)
{
    TrackingVector3 q = new(rotation.X, rotation.Y, rotation.Z);
    TrackingVector3 t = ScaleTestVector(CrossTestVector(q, value), 2f);
    return AddTestVector(
        value,
        AddTestVector(
            ScaleTestVector(t, rotation.W),
            CrossTestVector(q, t)));
}

static TrackingVector3 CrossTestVector(
    TrackingVector3 left,
    TrackingVector3 right) => new(
        (left.Y * right.Z) - (left.Z * right.Y),
        (left.Z * right.X) - (left.X * right.Z),
        (left.X * right.Y) - (left.Y * right.X));

static TrackingVector3 ScaleTestVector(TrackingVector3 value, float scale) =>
    new(value.X * scale, value.Y * scale, value.Z * scale);

static TrackingVector3 AddTestVector(
    TrackingVector3 left,
    TrackingVector3 right) =>
    new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

static void VrSettingsPreserveProductDefaults()
{
    VrSettingsValidationResult result = VrSettingsValidator.Validate(
        VrSettings.CreateApprovedDefaults());
    Equal(false, result.UsedFallback);
    Equal(VrHand.Left, result.Settings.Panel.PanelHand);
    Equal(VrHand.Right, result.Settings.Panel.PointerHand);
    Equal(true, result.Settings.Panel.ViewerFacing);
    Equal(0.10f, result.Settings.Panel.OffsetY);
    Equal(0.65f, result.Settings.Render.EyeRenderScale);
    Equal(true, result.Settings.Tracking.LiveSixDofEnabled);
    Equal(true, result.Settings.Tracking.LocomotionEnabled);
    Equal(VrHand.Right, result.Settings.Tracking.LocomotionHand);
    Equal(1.95f, result.Settings.Tracking.LocomotionSpeed);
    Equal(VrViewTurnMode.Snap, result.Settings.Tracking.ViewTurnMode);
    Equal(90f, result.Settings.Tracking.ViewTurnSpeed);
    Equal(30, result.Settings.Tracking.ViewSnapAngleDegrees);
    Equal(VrVisualEffectModes.Approved, result.Settings.Render.VisualEffectMode);
    Equal(true, result.Settings.Render.ManualVisualEffects.PostProcessingEnabled);
    Equal(1.40f, result.Settings.Render.ManualVisualEffects.VlBloomIntensityScale);
    Equal(1, result.Settings.Render.ManualVisualEffects.VlBloomDiffusion);
    Equal(false, result.Settings.Render.ManualVisualEffects.VlDepthOfFieldEnabled);
    Equal(false, result.Settings.Render.ManualVisualEffects.VlTextureBlurEnabled);
}

static void VrSettingsPreserveManualVfxControls()
{
    VrSettings settings = VrSettings.CreateApprovedDefaults();
    settings.Tracking.LiveSixDofEnabled = true;
    settings.Render.VisualEffectMode = VrVisualEffectModes.Manual;
    settings.Render.ManualVisualEffects.PostProcessingEnabled = true;
    settings.Render.ManualVisualEffects.VlBloomEnabled = false;
    settings.Render.ManualVisualEffects.VlBloomIntensityScale = 0.85f;
    settings.Render.ManualVisualEffects.VlBloomDiffusion = 4;
    settings.Render.ManualVisualEffects.VlDepthOfFieldEnabled = true;
    settings.Render.ManualVisualEffects.VlTextureBlurEnabled = true;
    settings.Render.ManualVisualEffects.VlStarStreakEnabled = false;
    settings.Render.ManualVisualEffects.VlFlareEnabled = false;

    VrSettingsValidationResult result = VrSettingsValidator.Validate(settings);

    Equal(false, result.UsedFallback);
    Equal(VrVisualEffectModes.Manual, result.Settings.Render.VisualEffectMode);
    Equal(true, result.Settings.Tracking.LiveSixDofEnabled);
    Equal(true, result.Settings.Render.ManualVisualEffects.PostProcessingEnabled);
    Equal(false, result.Settings.Render.ManualVisualEffects.VlBloomEnabled);
    Equal(0.85f, result.Settings.Render.ManualVisualEffects.VlBloomIntensityScale);
    Equal(4, result.Settings.Render.ManualVisualEffects.VlBloomDiffusion);
    Equal(true, result.Settings.Render.ManualVisualEffects.VlDepthOfFieldEnabled);
    Equal(true, result.Settings.Render.ManualVisualEffects.VlTextureBlurEnabled);
    Equal(false, result.Settings.Render.ManualVisualEffects.VlStarStreakEnabled);
    Equal(false, result.Settings.Render.ManualVisualEffects.VlFlareEnabled);
}

static void VrSettingsAllowTwoTimesEyeRenderScale()
{
    VrSettings settings = VrSettings.CreateApprovedDefaults();
    settings.Render.EyeRenderScale = 2.00f;

    VrSettingsValidationResult result = VrSettingsValidator.Validate(settings);

    Equal(false, result.UsedFallback);
    Equal(2.00f, result.Settings.Render.EyeRenderScale);
}

static void VrSettingsLoadLegacyJsonWithoutManualVfx()
{
    const string json = """
        {
          "schemaVersion": 1,
          "render": {
            "visualEffectMode": "manual"
          }
        }
        """;
    VrSettings? parsed = JsonSerializer.Deserialize<VrSettings>(
        json,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    VrSettingsValidationResult result = VrSettingsValidator.Validate(parsed);

    Equal(false, result.UsedFallback);
    Equal(VrVisualEffectModes.Manual, result.Settings.Render.VisualEffectMode);
    Equal(true, result.Settings.Tracking.LiveSixDofEnabled);
    Equal(true, result.Settings.Tracking.LocomotionEnabled);
    Equal(VrHand.Right, result.Settings.Tracking.LocomotionHand);
    Equal(1.95f, result.Settings.Tracking.LocomotionSpeed);
    Equal(VrViewTurnMode.Snap, result.Settings.Tracking.ViewTurnMode);
    Equal(90f, result.Settings.Tracking.ViewTurnSpeed);
    Equal(30, result.Settings.Tracking.ViewSnapAngleDegrees);
    Equal(true, result.Settings.Render.ManualVisualEffects.PostProcessingEnabled);
    Equal(1.40f, result.Settings.Render.ManualVisualEffects.VlBloomIntensityScale);
    Equal(1, result.Settings.Render.ManualVisualEffects.VlBloomDiffusion);
}

static void VrSettingsLoadSwappedMovementHand()
{
    const string json = """
        {
          "schemaVersion": 1,
          "tracking": {
            "locomotionEnabled": true,
            "locomotionHand": "left",
            "locomotionSpeed": 2.25,
            "viewTurnMode": "smooth",
            "viewTurnSpeed": 120.0,
            "viewSnapAngleDegrees": 45
          }
        }
        """;
    VrSettings? parsed = JsonSerializer.Deserialize<VrSettings>(
        json,
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        });

    VrSettingsValidationResult result = VrSettingsValidator.Validate(parsed);

    False(result.UsedFallback);
    Equal(VrHand.Left, result.Settings.Tracking.LocomotionHand);
    Equal(2.25f, result.Settings.Tracking.LocomotionSpeed);
    Equal(VrViewTurnMode.Smooth, result.Settings.Tracking.ViewTurnMode);
    Equal(120f, result.Settings.Tracking.ViewTurnSpeed);
    Equal(45, result.Settings.Tracking.ViewSnapAngleDegrees);
}

static void VrSettingsRepairInvalidValues()
{
    VrSettings settings = VrSettings.CreateApprovedDefaults();
    settings.Panel.PointerHand = VrHand.Left;
    settings.Panel.MaximumWidth = 4f;
    settings.Render.EyeRenderScale = 2.01f;
    settings.Render.ManualVisualEffects.VlBloomIntensityScale = 4f;
    settings.Render.ManualVisualEffects.VlBloomDiffusion = 0;
    settings.Tracking.LocomotionHand = (VrHand)99;
    settings.Tracking.LocomotionSpeed = 9f;
    settings.Tracking.ViewTurnMode = (VrViewTurnMode)99;
    settings.Tracking.ViewTurnSpeed = 500f;
    settings.Tracking.ViewSnapAngleDegrees = 20;
    settings.Input.BackButton = FaceButtonBinding.Primary;
    VrSettingsValidationResult result = VrSettingsValidator.Validate(settings);
    Equal(true, result.UsedFallback);
    Equal(VrHand.Left, result.Settings.Panel.PanelHand);
    Equal(VrHand.Right, result.Settings.Panel.PointerHand);
    Equal(0.42f, result.Settings.Panel.MaximumWidth);
    Equal(0.75f, result.Settings.Render.EyeRenderScale);
    Equal(1.40f, result.Settings.Render.ManualVisualEffects.VlBloomIntensityScale);
    Equal(1, result.Settings.Render.ManualVisualEffects.VlBloomDiffusion);
    Equal(VrHand.Right, result.Settings.Tracking.LocomotionHand);
    Equal(1.95f, result.Settings.Tracking.LocomotionSpeed);
    Equal(VrViewTurnMode.Snap, result.Settings.Tracking.ViewTurnMode);
    Equal(90f, result.Settings.Tracking.ViewTurnSpeed);
    Equal(30, result.Settings.Tracking.ViewSnapAngleDegrees);
    Equal(FaceButtonBinding.Primary, result.Settings.Input.PrimaryClickButton);
    Equal(FaceButtonBinding.Secondary, result.Settings.Input.BackButton);
}

static void VrSettingsRejectUnsupportedSchema()
{
    VrSettings settings = VrSettings.CreateApprovedDefaults();
    settings.SchemaVersion = 99;
    settings.Panel.PanelHand = VrHand.Right;
    VrSettingsValidationResult result = VrSettingsValidator.Validate(settings);
    Equal(true, result.UsedFallback);
    Equal(VrSettings.CurrentSchemaVersion, result.Settings.SchemaVersion);
    Equal(VrHand.Left, result.Settings.Panel.PanelHand);
}

static void TransitionTakesPriority()
{
    var result = new SceneClassifier().Classify(new RenderObservation
    {
        IsOrientationChanging = true,
        AreRenderTargetsStable = true,
        StableFrameCount = 5,
        HasLiveCameraMarker = true,
        HasValidWorldCamera = true,
        IsUrpCameraStackValid = true,
        IsProfileApproved = true,
        WorldCameraCount = 1
    });

    Equal(PresentationContext.Transition, result.Context);
    Equal(RecommendedPresentationMode.FrozenPanel, result.Mode);
}

static void VideoTakesPriority()
{
    var observation = StableObservation();
    observation = new RenderObservation
    {
        AreRenderTargetsStable = observation.AreRenderTargetsStable,
        StableFrameCount = observation.StableFrameCount,
        HasValidWorldCamera = observation.HasValidWorldCamera,
        IsUrpCameraStackValid = observation.IsUrpCameraStackValid,
        WorldCameraCount = observation.WorldCameraCount,
        HasFullScreenVideo = true,
        HasLiveCameraMarker = true,
        IsProfileApproved = true
    };

    var result = new SceneClassifier().Classify(observation);
    Equal(PresentationContext.Video2D, result.Context);
    Equal(RecommendedPresentationMode.SafePanel, result.Mode);
}

static void UnapprovedLiveUsesPanel()
{
    var result = new SceneClassifier().Classify(new RenderObservation
    {
        AreRenderTargetsStable = true,
        StableFrameCount = 5,
        HasLiveCameraMarker = true,
        HasValidWorldCamera = true,
        IsUrpCameraStackValid = true,
        WorldCameraCount = 2
    });

    Equal(PresentationContext.LiveCandidate, result.Context);
    Equal(RecommendedPresentationMode.SafePanel, result.Mode);
}

static void ApprovedLiveEnablesImmersive()
{
    var result = new SceneClassifier().Classify(new RenderObservation
    {
        AreRenderTargetsStable = true,
        StableFrameCount = 5,
        HasLiveCameraMarker = true,
        HasValidWorldCamera = true,
        IsUrpCameraStackValid = true,
        IsProfileApproved = true,
        WorldCameraCount = 2
    });

    Equal(PresentationContext.LiveCandidate, result.Context);
    Equal(RecommendedPresentationMode.Immersive, result.Mode);
}

static void UiOnlyUsesPanel()
{
    var result = new SceneClassifier().Classify(new RenderObservation
    {
        AreRenderTargetsStable = true,
        StableFrameCount = 5,
        UiCanvasDominates = true,
        WorldCameraCount = 0
    });

    Equal(PresentationContext.Menu2D, result.Context);
    Equal(RecommendedPresentationMode.SafePanel, result.Mode);
}

static void OrientationWaitsAndRebinds()
{
    var stabilizer = new OrientationStabilizer(requiredStableFrames: 3, timeoutMilliseconds: 2000);
    var initial = stabilizer.Observe(Sample(1080, 1920, "portrait", 0));
    Equal(OrientationTransitionState.StablePortrait, initial.State);

    var first = stabilizer.Observe(Sample(1920, 1080, "landscape", 10, explicitChange: true));
    Equal(OrientationTransitionState.WaitingForStableTargets, first.State);
    True(first.FreezeFrame);
    True(first.BlockPointerInput);

    var second = stabilizer.Observe(Sample(1920, 1080, "landscape", 20));
    False(second.RequestRebind);

    var third = stabilizer.Observe(Sample(1920, 1080, "landscape", 30));
    Equal(OrientationTransitionState.Rebinding, third.State);
    True(third.RequestRebind);

    var completed = stabilizer.CompleteRebind(success: true);
    Equal(OrientationTransitionState.StableLandscape, completed.State);
    Equal(OrientationKind.Landscape, completed.Orientation);
    False(completed.FreezeFrame);
}

static void OrientationTimeoutFallsBack()
{
    var stabilizer = new OrientationStabilizer(requiredStableFrames: 3, timeoutMilliseconds: 100);
    stabilizer.Observe(Sample(1080, 1920, "portrait", 0));
    stabilizer.Observe(Sample(0, 0, "none", 10, explicitChange: true, valid: false));
    var timedOut = stabilizer.Observe(Sample(0, 0, "none", 110, valid: false));

    Equal(OrientationTransitionState.SafePanel, timedOut.State);
    True(timedOut.TimedOut);
    False(timedOut.BlockPointerInput);
}

static void DimensionsOverrideStaleUnityOrientation()
{
    var stabilizer = new OrientationStabilizer(requiredStableFrames: 2, timeoutMilliseconds: 2000);
    var portrait = Sample(1080, 1920, "portrait", 0);
    portrait.ReportedScreenOrientation = 1;
    Equal(OrientationTransitionState.StablePortrait, stabilizer.Observe(portrait).State);

    var firstLandscape = Sample(1920, 1080, "landscape", 10);
    firstLandscape.ReportedScreenOrientation = 1;
    Equal(
        OrientationTransitionState.WaitingForStableTargets,
        stabilizer.Observe(firstLandscape).State);

    var stableLandscape = Sample(1920, 1080, "landscape", 20);
    stableLandscape.ReportedScreenOrientation = 1;
    var pending = stabilizer.Observe(stableLandscape);
    True(pending.RequestRebind);
    Equal(
        OrientationTransitionState.StableLandscape,
        stabilizer.CompleteRebind(success: true).State);
}

static void FailedRebindFallsBack()
{
    var stabilizer = new OrientationStabilizer(requiredStableFrames: 1, timeoutMilliseconds: 1000);
    stabilizer.Observe(Sample(1080, 1920, "portrait", 0));
    var pending = stabilizer.Observe(Sample(1920, 1080, "landscape", 10, explicitChange: true));
    if (!pending.RequestRebind)
    {
        pending = stabilizer.Observe(Sample(1920, 1080, "landscape", 20));
    }

    Equal(OrientationTransitionState.Rebinding, pending.State);
    var failed = stabilizer.CompleteRebind(success: false);
    Equal(OrientationTransitionState.SafePanel, failed.State);
    True(failed.FreezeFrame);
}

static OrientationSample Sample(
    int width,
    int height,
    string signature,
    long now,
    bool explicitChange = false,
    bool valid = true)
{
    return new OrientationSample
    {
        Width = width,
        Height = height,
        TargetSignature = signature,
        NowMilliseconds = now,
        ExplicitChangeSignal = explicitChange,
        RenderTargetsValid = valid
    };
}

static void Equal<T>(T expected, T actual)
    where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}

static void True(bool value)
{
    if (!value)
    {
        throw new InvalidOperationException("Expected true, got false.");
    }
}

static void False(bool value)
{
    if (value)
    {
        throw new InvalidOperationException("Expected false, got true.");
    }
}

static void Near(float expected, float actual, float tolerance = 0.0001f)
{
    if (MathF.Abs(expected - actual) > tolerance)
    {
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}

static void ComparisonBloom()
{
    Equal(4, BloomComparison.Diffusion(5, 100, .5f));
    Equal(6, BloomComparison.Diffusion(5, 100, 2f));
    Equal(0, BloomComparison.Diffusion(0, 100, .5f));
    Equal(1, BloomComparison.Diffusion(5, 100, float.NaN));
    Equal(5, BloomComparison.Diffusion(5, 29.9f, float.NaN));
    Equal(2, BloomComparison.Diffusion(5, float.NaN, 0));
    foreach (string mode in new[] { "vrify-default", VrVisualEffectModes.VrifyFollow })
    {
        var settings = VrSettings.CreateApprovedDefaults(); settings.Render.VisualEffectMode = mode;
        var validation = VrSettingsValidator.Validate(settings);
        Equal(0, validation.Issues.Count);
        Equal(VrVisualEffectModes.VrifyFollow, validation.Settings.Render.VisualEffectMode);
    }
}
static void GripGestures()
{
    var grip = new GripPressGesture(800);
    Equal(GripGesture.None, grip.Update(true, 1, 0)); // Already held at entry does not activate.
    grip.Update(true, 0, 1);
    grip.Update(true, .8f, 100);
    Equal(GripGesture.Short, grip.Update(true, 0, 899));
    grip.Update(true, .8f, 1000);
    Equal(GripGesture.Long, grip.Update(true, .8f, 1800));
    Equal(GripGesture.None, grip.Update(true, .8f, 2800));
    Equal(GripGesture.None, grip.Update(true, 0, 2900));
    grip.Update(true, .8f, 3000); grip.Update(false, 1, 3100);
    Equal(GripGesture.None, grip.Update(true, 1, 5000));
    Equal(GripGesture.None, grip.Update(true, 0, 5001));
}
static void SettingsEditor()
{
    var source = VrSettings.CreateApprovedDefaults(); var editor = new VrSettingsEditor(source);
    Equal("Render.VisualEffectMode", editor.Entries[0].Path);
    editor.Change(0, 1); Equal(VrVisualEffectModes.VrifyFollow, editor.Draft.Render.VisualEffectMode);
    Equal(VrVisualEffectModes.Approved, source.Render.VisualEffectMode);
    Equal(editor.Entries.Count, editor.Entries.Select(x => x.Path).Distinct().Count());
    if (editor.Entries.Count < 50) throw new Exception("Runtime settings were omitted from the VR editor.");
}
static void SettingsEditorValidation()
{
    var editor = new VrSettingsEditor(VrSettings.CreateApprovedDefaults());
    for (int index = 0; index < editor.Entries.Count; index++)
        foreach (int direction in new[] { 1, -1 })
            for (int i = 0; i < 80; i++)
            {
                editor.Change(index, direction);
                var result = VrSettingsValidator.Validate(editor.Draft);
                if (result.Issues.Count != 0) throw new Exception(editor.Entries[index].Path + ": " + string.Join(",", result.Issues));
            }
}
static void MenuLayout()
{
    Equal(SettingsMenuAction.ProfileA, SettingsMenuLayout.Hit(.1f, .15f).Action);
    Equal(SettingsMenuAction.None, SettingsMenuLayout.Hit(.5f, .15f).Action);
    Equal(SettingsMenuAction.ProfileC, SettingsMenuLayout.Hit(.9f, .15f).Action);
    Equal(SettingsMenuAction.Apply, SettingsMenuLayout.Hit(.5f, .93f).Action);
    Equal(SettingsMenuAction.None, SettingsMenuLayout.Hit(-.1f, .5f).Action);
    Equal(SettingsMenuAction.None, SettingsMenuLayout.Hit(.5f, float.NaN).Action);
    Equal(SettingsMenuAction.None, SettingsMenuLayout.Hit(.5f, .82f).Action);
}

static void SharedTranslations()
{
    VrUiText.Validate();
    var editor = new VrSettingsEditor(VrSettings.CreateApprovedDefaults());
    foreach (VrUiLanguage language in Enum.GetValues<VrUiLanguage>())
    {
        for (int index = 0; index < editor.Entries.Count; index++)
            for (int value = 0; value < 6; value++)
            {
                var entry = editor.Entries[index];
                if (string.IsNullOrWhiteSpace(entry.GetLabel(language)) || string.IsNullOrWhiteSpace(entry.GetGroup(language)) || string.IsNullOrWhiteSpace(entry.GetDisplay(language)))
                    throw new Exception("Untranslated setting: " + entry.Path);
                editor.Change(index, 1);
            }
        if (VrUiText.Get(language, "MenuPage").Contains("{0}") == false) throw new Exception("Page format is missing.");
    }
    Equal("켜짐", VrSettingsText.Value(VrUiLanguage.Korean, true));
    Equal("オフ", VrSettingsText.Value(VrUiLanguage.Japanese, false));
    Equal("140 %", VrSettingsText.Value(VrUiLanguage.English, 1.4f, "Render.ManualVisualEffects.VlBloomIntensityScale"));
    Equal(SettingsMenuAction.Language, SettingsMenuLayout.Hit(.85f, .05f).Action);
}
static void LanguagePreferences()
{
    string root = Path.Combine(Path.GetTempPath(), "GakumasVR-language-tests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);
    Equal(VrUiLanguage.Japanese, VrUiPreferences.Load(root, System.Globalization.CultureInfo.GetCultureInfo("ja-JP")));
    File.WriteAllText(Path.Combine(root, "configurator-language.txt"), "en");
    Equal(VrUiLanguage.English, VrUiPreferences.Load(root));
    foreach (VrUiLanguage language in Enum.GetValues<VrUiLanguage>())
    {
        if (!VrUiPreferences.TrySave(language, root)) throw new Exception("Preference save failed.");
        Equal(language, VrUiPreferences.Load(root));
    }
    Equal("en", File.ReadAllText(Path.Combine(root, "configurator-language.txt")));
    File.WriteAllText(Path.Combine(root, "ui-language.txt"), "invalid");
    Equal(VrUiLanguage.Korean, VrUiPreferences.Load(root));
    string blocked = Path.Combine(root, "not-a-directory"); File.WriteAllText(blocked, "keep");
    Equal(false, VrUiPreferences.TrySave(VrUiLanguage.Japanese, blocked));
    Equal("keep", File.ReadAllText(blocked));
}

static void SphereContact()
{
    var center = System.Numerics.Vector3.Zero;
    var hand = new System.Numerics.Vector3(.04f, 0, 0);
    if (!ContactResponse.Sphere(center, .04f, hand, .01f, System.Numerics.Vector3.UnitZ, out var push)) throw new Exception("Contact missed.");
    Near(-.0065f, push.X); Near(0, push.Y);
    if (ContactResponse.Sphere(center, .04f, new System.Numerics.Vector3(.051f, 0, 0), .01f, default, out _)) throw new Exception("False overlap.");
    if (!ContactResponse.Sphere(center, .04f, center, .01f, default, out push) || !ContactResponse.Finite(push)) throw new Exception("Coincident centers failed.");
    if (ContactResponse.Sphere(center, .04f, new System.Numerics.Vector3(float.NaN, 0, 0), .01f, default, out _)) throw new Exception("Nonfinite contact accepted.");
    ContactResponse.Sphere(center, .04f * .275f, hand * .275f, .01f * .275f, default, out var scaled);
    Near(-.0065f * .275f, scaled.X);
    ContactResponse.Sphere(center, .04f, -hand, .01f, default, out var opposite);
    Near(0, (opposite + new System.Numerics.Vector3(-.0065f, 0, 0)).Length());
}

static void ContactSpring()
{
    var spring = new ContactResponse();
    var force = new System.Numerics.Vector3(10, 10, 10);
    var first = spring.Step(1, .016f, force, .025f, true);
    if (first.Length() <= 0 || spring.Step(1, .016f, -force, .025f, true) != first) throw new Exception("Pair integrated twice.");
    for (int i = 2; i <= 30; i++)
        if (spring.Step(i, .033f, force, .025f, true).Length() > .02501f) throw new Exception("Combined displacement escaped limit.");
    for (int i = 31; i <= 90; i++) spring.Step(i, .016f, default, .025f, true);
    if (spring.Offset.Length() > .00001f || spring.Velocity.Length() > .0001f) throw new Exception("Tracked release did not settle.");
    var other = new ContactResponse(); other.Step(1, .016f, -force, .025f, true);
    if (spring.Offset == other.Offset) throw new Exception("Targets share spring state.");
}

static void ContactDiscontinuity()
{
    var spring = new ContactResponse();
    spring.Step(1, .016f, System.Numerics.Vector3.UnitX, .025f, true);
    if (spring.Step(2, .016f, default, .025f, false) != default || spring.Velocity != default) throw new Exception("Lost hand injected release momentum.");
    spring.Step(3, .016f, System.Numerics.Vector3.UnitX, .025f, true);
    if (spring.Step(4, .5f, default, .025f, true) != default) throw new Exception("Stale interval integrated.");
    spring.Step(5, .016f, System.Numerics.Vector3.UnitX, .025f, true);
    if (spring.Step(6, .016f, new System.Numerics.Vector3(float.NaN, 0, 0), .025f, true) != default) throw new Exception("Nonfinite force integrated.");
}

static void ContactEntry()
{
    var latch = new ContactEnterLatch();
    if (latch.Update(true, true)) throw new Exception("Acquisition inside target pulsed.");
    latch.Update(true, false);
    if (!latch.Update(true, true) || latch.Update(true, true)) throw new Exception("Contact entry duplicated or missed.");
    latch.Update(false, false);
    if (latch.Update(true, true)) throw new Exception("Blocked hand rearmed.");
    latch.Update(true, false);
    if (!latch.Update(true, true)) throw new Exception("Valid separation did not rearm.");
}

static void ReferenceContactProfile()
{
    var settings = VrSettings.CreateApprovedDefaults();
    var left = ContactMaterialProfile.Chest(settings.Tracking, false);
    var right = ContactMaterialProfile.Chest(settings.Tracking, true);
    Near(2, left.Position.FrequencyHz);
    Near(2.06f, right.Position.FrequencyHz);
    Near(.3f, left.Position.DampingRatio);
    Near(.255f, left.Tilt.DampingRatio);
    Near(.03f, left.MaxDisplacement);
    Near(.24f, left.ReleaseGain);
    Near(MathF.PI / 30f, left.MaxTiltRadians);
    Near(.0675f, left.Stretch);
    Near(.045f, left.Squash);
    Equal(15f, settings.Tracking.ContactHapticStrengthPercent);
    Equal(30f, settings.Tracking.ContactHapticDurationMs);
    settings.Tracking.ContactDurationMultiplier = 0;
    Near(.4f, VrSettingsValidator.Validate(settings).Settings.Tracking.ContactDurationMultiplier);
}

static void ContactVolume()
{
    var profile = ContactMaterialProfile.Chest(VrSettings.CreateApprovedDefaults().Tracking, false);
    var depth = System.Numerics.Vector3.UnitZ;
    var scale = profile.ScaleGoal(-depth * .005f, -depth, .02f, depth, .05f, default, .05f, out _);
    if (scale.Z >= 1 || scale.X <= 1 || scale.Y <= 1) throw new Exception("Compression did not compensate across axes.");
    Near(1, scale.X * scale.Y * scale.Z);
    var diagonal = System.Numerics.Vector3.Normalize(System.Numerics.Vector3.One);
    Equal(System.Numerics.Vector3.One, profile.ScaleGoal(-diagonal, -diagonal, .02f, diagonal, .05f, default, .05f, out _));
    var tilt = profile.TiltGoal(System.Numerics.Vector3.UnitX, depth, .05f);
    Near(profile.MaxTiltRadians, tilt.Y);
    Equal(System.Numerics.Vector3.Zero, profile.TiltGoal(depth, depth, .05f));
}

static void ThighElasticity()
{
    var tracking = VrSettings.CreateApprovedDefaults().Tracking;
    var profile = ContactMaterialProfile.Thigh(tracking);
    Near(4.5f, profile.Position.FrequencyHz);
    Near(.72f, profile.Position.DampingRatio);
    Near(.006f, profile.MaxDisplacement);
    Near(.05f, profile.ReleaseGain);
    Near(1.5f * MathF.PI / 180f, profile.MaxTiltRadians);
    Near(.015f, profile.Stretch);
    var outward = System.Numerics.Vector3.UnitZ;
    var shift = System.Numerics.Vector3.UnitX * .008f;
    var tilt = profile.TiltGoal(shift, outward, .05f);
    if (tilt.Y <= 0 || tilt.Length() > profile.MaxTiltRadians + .00001f)
        throw new Exception("Thigh tilt did not follow tangential motion within its cap.");

    var squeezed = profile.ScaleGoal(-outward * .008f, -outward, .03f, outward, .05f, default, .05f, out var pressure);
    if (squeezed.Z >= 1 || squeezed.Z < .975f - .00001f || squeezed.X <= 1)
        throw new Exception("Thigh compression did not respect its bounds.");
    Near(1, squeezed.X * squeezed.Y * squeezed.Z);
    var relaxing = profile.ScaleGoal(default, default, 0, outward, .05f, pressure, .05f, out _);
    if (relaxing.Z <= squeezed.Z || relaxing.Z > 1)
        throw new Exception("Thigh compression did not relax on release.");

    tracking.ContactThighCompressionPercent = 0;
    var stretchOnly = ContactMaterialProfile.Thigh(tracking);
    Equal(System.Numerics.Vector3.One,
        stretchOnly.ScaleGoal(-outward * .006f, -outward, .03f, outward, .05f, default, .05f, out _));
    tracking.ContactThighCompressionPercent = 2.5f;
    tracking.ContactThighStretchPercent = 0;
    var compressionOnly = ContactMaterialProfile.Thigh(tracking);
    Near(squeezed.Z,
        compressionOnly.ScaleGoal(-outward * .008f, -outward, .03f, outward, .05f, default, .05f, out _).Z);
}

static void SurfacePatch()
{
    var points = new List<System.Numerics.Vector3>();
    for (int y = -4; y <= 4; y++) for (int x = -4; x <= 4; x++)
        points.Add(new System.Numerics.Vector3(x * .01f, y * .01f, .12f));
    var normals = Enumerable.Repeat(System.Numerics.Vector3.UnitZ, points.Count).ToArray();
    if (!ContactSurfaceFit.TryFit(points.ToArray(), normals, out var center, out var normal, out float radius)) throw new Exception("Surface fit failed.");
    Near(.12f, (center + normal * radius).Z);
    if (center.Z <= 0 || radius is < .015f or > .09f) throw new Exception("Surface remained at the rig pivot.");
    if (!ContactResponse.Sphere(center, radius, new System.Numerics.Vector3(0, 0, .125f), .01f, normal, out _)) throw new Exception("Surface touch required torso penetration.");
}
static void SurfaceAnchor()
{
    var start = System.Numerics.Matrix4x4.CreateScale(.5f) * System.Numerics.Matrix4x4.CreateTranslation(2, 3, 4);
    var center = new System.Numerics.Vector3(2, 3, 4.1f);
    if (!ContactSurfaceAnchor.TryBind(center, System.Numerics.Vector3.UnitZ, .05f, start, out var anchor)) throw new Exception("Anchor rejected valid scale.");
    var next = System.Numerics.Matrix4x4.CreateScale(1.5f) * System.Numerics.Matrix4x4.CreateRotationY(MathF.PI / 2) * System.Numerics.Matrix4x4.CreateTranslation(-1, 2, 3);
    if (!anchor.TryMap(next, out var mapped, out var outward, out float radius)) throw new Exception("Anchor mapping failed.");
    Near(-.7f, mapped.X); Near(2, mapped.Y); Near(3, mapped.Z); Near(.15f, radius); Near(1, outward.X);
    if (anchor.TryMap(System.Numerics.Matrix4x4.CreateScale(1, 2, 1), out _, out _, out _)) throw new Exception("Nonuniform geometry accepted as a sphere.");
}
static void SurfaceRejectsInvalid()
{
    var points = Enumerable.Repeat(System.Numerics.Vector3.Zero, 16).ToArray();
    var normals = Enumerable.Range(0, 16).Select(i => i % 2 == 0 ? System.Numerics.Vector3.UnitZ : -System.Numerics.Vector3.UnitZ).ToArray();
    if (ContactSurfaceFit.TryFit(points, normals, out _, out _, out _)) throw new Exception("Ambiguous outward normal accepted.");
    normals = Enumerable.Repeat(System.Numerics.Vector3.UnitZ, 16).ToArray();
    if (ContactSurfaceFit.TryFit(points, normals, out _, out _, out _)) throw new Exception("Degenerate patch accepted.");
    points[0] = new System.Numerics.Vector3(float.NaN, 0, 0);
    if (ContactSurfaceFit.TryFit(points, normals, out _, out _, out _)) throw new Exception("Nonfinite patch accepted.");
}
static void ContactJiggle()
{
    var spring = new ContactResponse();
    for (int i = 1; i <= 40; i++) spring.Step(i, .016f, new System.Numerics.Vector3(.01f, 0, 0), .025f, true, true);
    bool crossed = false;
    for (int i = 41; i <= 100; i++)
    {
        var offset = spring.Step(i, .016f, default, .025f, true, true);
        crossed |= offset.X < -.0001f;
        if (offset.Length() > .02501f) throw new Exception("Jiggle escaped the deformation cap.");
    }
    if (!crossed || spring.Offset.Length() > .0001f) throw new Exception("Jiggle did not oscillate and decay.");
    spring.Step(101, .016f, System.Numerics.Vector3.UnitX, .025f, true, true);
    if (spring.Step(102, .016f, default, .025f, false, true, System.Numerics.Vector3.One) != default || spring.Velocity != default)
        throw new Exception("Tracking loss generated a jiggle impulse.");
}
static void ContactRelease()
{
    var velocity = new System.Numerics.Vector3(1, 0, 0);
    var impulse = ContactResponse.ExitImpulse(3, 2, 3, velocity, -velocity, out int mask);
    Equal(1, mask); Near(.08f, impulse.X);
    impulse = ContactResponse.ExitImpulse(1, 0, 0, velocity, default, out mask);
    Equal(0, mask); Near(0, impulse.Length());
    impulse = ContactResponse.ExitImpulse(1, 1, 1, velocity, default, out mask);
    Equal(0, mask);
    var spring = new ContactResponse();
    var first = spring.Step(1, .016f, default, .025f, true, true, velocity);
    var state = spring.Velocity;
    if (spring.Step(1, .016f, default, .025f, true, true, velocity) != first || spring.Velocity != state)
        throw new Exception("Stereo callbacks applied the release impulse twice.");
}
