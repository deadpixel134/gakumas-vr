using GakumasVR.Core;
using System.Text.Json;

var tests = new (string Name, Action Run)[]
{
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
    ("VR settings preserve approved defaults", VrSettingsPreserveApprovedDefaults),
    ("VR settings allow 2x eye render scale", VrSettingsAllowTwoTimesEyeRenderScale),
    ("VR settings preserve manual VFX controls", VrSettingsPreserveManualVfxControls),
    ("VR settings load legacy JSON without manual VFX", VrSettingsLoadLegacyJsonWithoutManualVfx),
    ("VR settings repair invalid roles and ranges", VrSettingsRepairInvalidValues),
    ("VR settings reject unsupported schema", VrSettingsRejectUnsupportedSchema)
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

static void VrSettingsPreserveApprovedDefaults()
{
    VrSettingsValidationResult result = VrSettingsValidator.Validate(
        VrSettings.CreateApprovedDefaults());
    Equal(false, result.UsedFallback);
    Equal(VrHand.Left, result.Settings.Panel.PanelHand);
    Equal(VrHand.Right, result.Settings.Panel.PointerHand);
    Equal(true, result.Settings.Panel.ViewerFacing);
    Equal(0.10f, result.Settings.Panel.OffsetY);
    Equal(0.65f, result.Settings.Render.EyeRenderScale);
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
    Equal(true, result.Settings.Render.ManualVisualEffects.PostProcessingEnabled);
    Equal(1.40f, result.Settings.Render.ManualVisualEffects.VlBloomIntensityScale);
    Equal(1, result.Settings.Render.ManualVisualEffects.VlBloomDiffusion);
}

static void VrSettingsRepairInvalidValues()
{
    VrSettings settings = VrSettings.CreateApprovedDefaults();
    settings.Panel.PointerHand = VrHand.Left;
    settings.Panel.MaximumWidth = 4f;
    settings.Render.EyeRenderScale = 2.01f;
    settings.Render.ManualVisualEffects.VlBloomIntensityScale = 4f;
    settings.Render.ManualVisualEffects.VlBloomDiffusion = 0;
    settings.Input.BackButton = FaceButtonBinding.Primary;
    VrSettingsValidationResult result = VrSettingsValidator.Validate(settings);
    Equal(true, result.UsedFallback);
    Equal(VrHand.Left, result.Settings.Panel.PanelHand);
    Equal(VrHand.Right, result.Settings.Panel.PointerHand);
    Equal(0.42f, result.Settings.Panel.MaximumWidth);
    Equal(0.75f, result.Settings.Render.EyeRenderScale);
    Equal(1.40f, result.Settings.Render.ManualVisualEffects.VlBloomIntensityScale);
    Equal(1, result.Settings.Render.ManualVisualEffects.VlBloomDiffusion);
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
