using System.Diagnostics;
using System.Runtime.InteropServices;
using GakumasVR.Core;

namespace Doorstop;

internal sealed class MainThreadSampler
{
    private const bool EnableUiObjectEnumeration = true;
    private const bool EnableUiCaptureSubmission = false;
    private const bool EnableUiElementReplay = false;
    private const bool EnableNaturalUiCapture = false;
    private const bool EnableStereoCloneSetup = true;
    private const bool EnableStereoOneShotRender = true;
    private const int StereoContinuousIntervalMilliseconds = 33;
    private const int StereoStartupStableFrames = 2;
    private const int StereoBlackFrameRetryMilliseconds = 100;
    private const int StereoFrozenDiagnosticDurationMilliseconds = 30_000;
    private const int NaturalUiVisibilitySettleMilliseconds = 500;
    private const int M5TopologyPollMilliseconds = 1_000;
    private const bool RequireStereoDepthTexture = true;
    private static readonly bool SuppressLiveWorldDuringUiCapture = false;
    private const string FrameCountIcall = "UnityEngine.Time::get_frameCount()";
    private readonly Il2CppApi _api;
    private readonly IntPtr _domain;
    private readonly string _logPath;
    private readonly string _stereoVisualEffectMode;
    private readonly VrManualVisualEffectSettings _manualVisualEffects;
    private readonly float _stereoRenderResolutionScale;
    private readonly float _stereoWorldEyeOffsetScale;
    private readonly FrameCountDelegate _replacement;
    private readonly DrawFlareDelegate _drawFlareReplacement;
    private readonly VlPostProcessRenderDelegate _vlPostProcessRenderReplacement;
    private readonly SetupVlBloomDelegate _setupVlBloomReplacement;
    private readonly DrawStarStreakDelegate _drawStarStreakReplacement;
    private readonly DoVlDofDelegate _doVlDofReplacement;
    private readonly DoVlTextureBlurDelegate _doVlTextureBlurReplacement;
    private readonly OrientationStabilizer _orientationStabilizer = new(
        requiredStableFrames: 5,
        timeoutMilliseconds: 2_000);
    private readonly SceneClassifier _sceneClassifier = new(requiredStableFrames: 5);
    private readonly StereoStartupGate _stereoStartupGate = new(
        requiredStableFrames: StereoStartupStableFrames);
    private readonly StereoBlackFrameRetryPolicy _stereoBlackFrameRetry = new(
        maximumAttempts: 20,
        timeoutMilliseconds: 2_000);
    private FrameCountDelegate? _original;
    private IntPtr _dobbyLibrary;
    private long _lastSampleMilliseconds;
    private string _lastSignature = string.Empty;
    private string _lastTopologySignature = string.Empty;
    private string _lastDecisionSignature = string.Empty;
    private DateTimeOffset _nextHeartbeatUtc = DateTimeOffset.MinValue;
    private DateTimeOffset _nextM5TopologyCaptureUtc = DateTimeOffset.MinValue;
    private DateTimeOffset _nextCameraFailureUtc = DateTimeOffset.MinValue;
    private DateTimeOffset _nextCameraCaptureUtc = DateTimeOffset.MinValue;
    private IReadOnlyList<CameraProbeRecord> _lastCameras = Array.Empty<CameraProbeRecord>();
    private IntPtr _lastLiveTargetTexture;
    private int _lastLiveTargetWidth;
    private int _lastLiveTargetHeight;
    private IntPtr _m6WorldCandidateCamera;
    private IntPtr _m6WorldCandidateTargetTexture;
    private string _m6WorldCandidateTargetName = string.Empty;
    private int _m6WorldCandidateTargetWidth;
    private int _m6WorldCandidateTargetHeight;
    private IntPtr _m6WorldSurfaceRawImage;
    private string _m6WorldSurfacePath = string.Empty;
    private bool _m6NonLiveWorldSurfaceEligible;
    private long _lastNativeTextureRefreshMilliseconds;
    private DateTimeOffset _nextUiFailureUtc = DateTimeOffset.MinValue;
    private DateTimeOffset _nextVideoFailureUtc = DateTimeOffset.MinValue;
    private DateTimeOffset _nextMediaSurfaceFailureUtc = DateTimeOffset.MinValue;
    private IReadOnlyList<VideoPlayerProbeRecord> _lastVideoPlayers =
        Array.Empty<VideoPlayerProbeRecord>();
    private IReadOnlyList<MediaSurfaceProbeRecord> _lastMediaSurfaces =
        Array.Empty<MediaSurfaceProbeRecord>();
    private IReadOnlyList<M5MediaClass> _m5MediaClasses =
        Array.Empty<M5MediaClass>();
    private bool _m5MediaClassesDiscovered;
    private IntPtr _m5AdditionalCameraDataClass;
    private IntPtr _m5AdditionalCameraDataType;
    private IntPtr _m5GetComponent;
    private IntPtr _m5RendererIndexField;
    private bool _urpCapabilitiesCaptured;
    private bool _uiReplayCapabilitiesCaptured;
    private bool _uiCaptureInProgress;
    private IntPtr _uiCaptureRequest;
    private IntPtr _uiCaptureRenderTexture;
    private uint _uiCaptureRequestHandle;
    private uint _uiCaptureRenderTextureHandle;
    private bool _uiCaptureDestinationVerified;
    private IntPtr _cachedThreeDTextureRawImage;
    private IntPtr _cachedThreeDTextureCanvasRenderer;
    private IntPtr _cachedUiCamera;
    private long _nextUiCaptureMilliseconds;
    private int _uiCaptureFrameCount;
    private DateTimeOffset _nextUiCaptureLogUtc = DateTimeOffset.MinValue;
    private bool _uiNaturalCaptureArmed;
    private long _uiNaturalCaptureStartPresentSerial;
    private IntPtr _uiNaturalCamera;
    private IntPtr _uiNaturalOriginalTargetTexture;
    private IntPtr _uiNaturalCanvasImage;
    private readonly List<(IntPtr Renderer, bool WasCulled)>
        _uiNaturalSuppressedRenderers = new();
    private IntPtr _liveUiCanvasGroup;
    private IntPtr _liveUiVisibilityRenderer;
    private int _liveUiVisibilityState = -1;
    private long _nextNaturalUiCaptureRetryMilliseconds;
    private bool _uiReplayInProgress;
    private IntPtr _uiReplayRenderTexture;
    private IntPtr _uiReplayRenderTargetIdentifier;
    private IntPtr _uiReplayCommandBuffer;
    private IntPtr _uiReplayPropertyBlock;
    private uint _uiReplayRenderTextureHandle;
    private uint _uiReplayRenderTargetIdentifierHandle;
    private uint _uiReplayCommandBufferHandle;
    private uint _uiReplayPropertyBlockHandle;
    private int _uiReplayMainTexturePropertyId;
    private long _nextUiReplayMilliseconds;
    private int _uiReplayFrameCount;
    private DateTimeOffset _nextUiReplayFailureUtc = DateTimeOffset.MinValue;
    private DateTimeOffset _nextUiReplayLogUtc = DateTimeOffset.MinValue;
    private IntPtr _lastLiveCamera;
    private bool _stereoCloneSetupAttempted;
    private IntPtr _stereoLeftCamera;
    private IntPtr _stereoRightCamera;
    private IntPtr _stereoLeftAdditionalCameraData;
    private IntPtr _stereoRightAdditionalCameraData;
    private IntPtr _stereoLeftRenderTexture;
    private IntPtr _stereoRightRenderTexture;
    private IntPtr _stereoAlternateLeftRenderTexture;
    private IntPtr _stereoAlternateRightRenderTexture;
    private IntPtr _stereoThirdLeftRenderTexture;
    private IntPtr _stereoThirdRightRenderTexture;
    private uint _stereoLeftGameObjectHandle;
    private uint _stereoRightGameObjectHandle;
    private uint _stereoLeftCameraHandle;
    private uint _stereoRightCameraHandle;
    private uint _stereoLeftRenderTextureHandle;
    private uint _stereoRightRenderTextureHandle;
    private uint _stereoAlternateLeftRenderTextureHandle;
    private uint _stereoAlternateRightRenderTextureHandle;
    private uint _stereoThirdLeftRenderTextureHandle;
    private uint _stereoThirdRightRenderTextureHandle;
    private IntPtr _stereoLeftRenderRequest;
    private IntPtr _stereoRightRenderRequest;
    private IntPtr _stereoAlternateLeftRenderRequest;
    private IntPtr _stereoAlternateRightRenderRequest;
    private uint _stereoLeftRenderRequestHandle;
    private uint _stereoRightRenderRequestHandle;
    private uint _stereoAlternateLeftRenderRequestHandle;
    private uint _stereoAlternateRightRenderRequestHandle;
    private IntPtr _stereoGpuCompletionQuery;
    private IntPtr _stereoPublishedLeftRenderTexture;
    private IntPtr _stereoPublishedRightRenderTexture;
    private bool _stereoRenderSubmitted;
    private bool _stereoDiagnosticSaved;
    private long _stereoRenderSubmittedMilliseconds;
    private bool _stereoCloneSetupReady;
    private bool? _stereoSourceRenderShadows;
    private bool? _stereoSourcePostProcessing;
    private bool _stereoClonePostProcessing;
    private bool _stereoVisualEffectOverrideConfigured;
    private bool _stereoVisualEffectOverrideApplied;
    private bool _stereoVisualEffectFallback;
    private bool _stereoVisualEffectOverrideFailed;
    private IntPtr _stereoSetVolumeFrameworkUpdateMode;
    private IntPtr _stereoUpdateVolumeStack;
    private IntPtr _stereoGetVolumeStack;
    private IntPtr _stereoVolumeStackGetComponent;
    private IntPtr _stereoVolumeComponentActiveField;
    private IntPtr[] _stereoVisualEffectManagedTypes = Array.Empty<IntPtr>();
    private bool _vlRenderPathDiagnosticsLogged;
    private DrawFlareDelegate? _drawFlareOriginal;
    private VlPostProcessRenderDelegate? _vlPostProcessRenderOriginal;
    private VlPostProcessRenderDelegate? _basePostProcessRender;
    private SetupVlBloomDelegate? _setupVlBloomOriginal;
    private DrawStarStreakDelegate? _drawStarStreakOriginal;
    private DoVlDofDelegate? _doVlDofOriginal;
    private DoVlTextureBlurDelegate? _doVlTextureBlurOriginal;
    private bool _drawFlareHookInstalled;
    private int _renderingDataCameraDataOffset;
    private int _cameraDataCameraOffset;
    private int _insideCloneVlPostProcess;
    private IntPtr _vlBloomIntensityField;
    private IntPtr _vlBloomThresholdField;
    private IntPtr _vlBloomDiffusionField;
    private IntPtr _volumeFloatValueField;
    private IntPtr _volumeIntValueField;
    private long _drawFlareCallCount;
    private long _drawFlareCloneSkipCount;
    private long _drawFlareSourceCount;
    private long _drawFlareNullCameraCount;
    private long _drawFlareOtherCameraCount;
    private long _vlPostProcessRenderCount;
    private long _vlPostProcessCloneRenderCount;
    private long _vlPostProcessSourceRenderCount;
    private long _vlPostProcessUnmatchedRenderCount;
    private long _setupVlBloomCallCount;
    private long _setupVlBloomCloneSkipCount;
    private long _drawStarStreakCallCount;
    private long _drawStarStreakCloneSkipCount;
    private long _vlBloomIntensityScaleCount;
    private long _vlBloomIntensityScaleFailureCount;
    private long _vlBloomThresholdRaiseCount;
    private long _vlBloomThresholdRaiseFailureCount;
    private float _lastVlBloomOriginalThreshold;
    private float _lastVlBloomAdjustedThreshold;
    private long _vlBloomDiffusionScaleCount;
    private long _vlBloomDiffusionScaleFailureCount;
    private float _lastVlBloomOriginalDiffusion;
    private float _lastVlBloomAdjustedDiffusion;
    private long _vlBloomCombinedScaleCount;
    private long _vlBloomCombinedScaleFailureCount;
    private float _lastVlBloomOriginalIntensity;
    private float _lastVlBloomAdjustedIntensity;
    private long _doVlDofCallCount;
    private long _doVlDofCloneSkipCount;
    private long _doVlTextureBlurCallCount;
    private long _doVlTextureBlurCloneSkipCount;
    private long _nextDrawFlareStatsMilliseconds;
    private bool? _stereoSourceRequiresDepthTexture;
    private bool? _stereoCloneRequiresDepthTexture;
    private int? _stereoSourceRequiresDepthOption;
    private int? _stereoCloneRequiresDepthOption;
    private int? _stereoRendererIndex;
    private int? _stereoSourceRenderType;
    private int? _stereoCloneRenderType;
    private int? _stereoSourceAntialiasing;
    private long _stereoContinuousStartMilliseconds;
    private long _nextStereoRenderMilliseconds;
    private int _stereoContinuousFrameCount;
    private bool _stereoContinuousFailed;
    private bool _stereoOutputValidated;
    private long _nextStereoValidationMilliseconds;
    private long _stereoFrozenUntilMilliseconds;
    private bool _stereoNaturalRenderArmed;
    private long _stereoNaturalRenderStartPresentSerial;
    private long _stereoNaturalRenderArmTimestamp;
    private int _stereoNaturalRenderCompletionMask;
    private long _lastStereoSourceRenderPresentSerial = -1;
    private IntPtr _stereoNaturalLeftRenderTexture;
    private IntPtr _stereoNaturalRightRenderTexture;
    private IntPtr _stereoPumpCoreImage;
    private bool _stereoPumpEligible;
    private IntPtr _stereoPumpSourceCamera;
    private IntPtr _stereoPumpSourceTexture;
    private int _lastStereoPumpFrameCount = -1;
    private long _stereoLastPublishMilliseconds;
    private long _stereoLastPublishPresentSerial;
    private long _stereoRateWindowStartMilliseconds;
    private int _stereoRateWindowStartFrameCount;
    private DateTimeOffset _nextStereoStateUnavailableLogUtc = DateTimeOffset.MinValue;
    private int _stereoGenerationRetireCount;
    private int _liveStereoGenerationRetireCount;
    private bool _stereoGenerationRequiresDynamicUi;
    private int _lastPortraitResolutionNudgeRetireCount;
    private int _canonicalPortraitWidth;
    private int _canonicalPortraitHeight;
    private bool _portraitResolutionRestorePending;
    private long _portraitResolutionRestoreAfterMilliseconds;

    public MainThreadSampler(Il2CppApi api, IntPtr domain, string logPath)
    {
        _api = api;
        _domain = domain;
        _logPath = logPath;
        VrSettings settings = VrSettingsRuntime.Current;
        _stereoVisualEffectMode = settings.Render.VisualEffectMode;
        _manualVisualEffects = settings.Render.ManualVisualEffects;
        _stereoRenderResolutionScale = settings.Render.EyeRenderScale;
        _stereoWorldEyeOffsetScale = settings.Render.WorldEyeOffsetScale;
        _replacement = OnFrameCount;
        _drawFlareReplacement = OnDrawFlare;
        _vlPostProcessRenderReplacement = OnVlPostProcessRender;
        _setupVlBloomReplacement = OnSetupVlBloom;
        _drawStarStreakReplacement = OnDrawStarStreak;
        _doVlDofReplacement = OnDoVlDof;
        _doVlTextureBlurReplacement = OnDoVlTextureBlur;
    }

    public void Run()
    {
        Install();
        WaitForever();
    }

    public void Install()
    {
        IntPtr target = ResolveIcall(FrameCountIcall);
        string gameRoot = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName)
            ?? Directory.GetCurrentDirectory();
        string dobbyPath = Path.Combine(gameRoot, "BepInEx", "core", "dobby.dll");
        _dobbyLibrary = NativeLibrary.Load(dobbyPath);
        IntPtr hookExport = NativeLibrary.GetExport(_dobbyLibrary, "DobbyHook");
        DobbyHookDelegate hook = Marshal.GetDelegateForFunctionPointer<DobbyHookDelegate>(hookExport);
        IntPtr replacement = Marshal.GetFunctionPointerForDelegate(_replacement);
        int result = hook(target, replacement, out IntPtr original);
        if (result != 0 || original == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"DobbyHook failed for {FrameCountIcall}: result={result}, original={original}.");
        }

        _original = Marshal.GetDelegateForFunctionPointer<FrameCountDelegate>(original);
        RuntimeProbe.Append(_logPath, new ProbeEvent
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Event = "main-thread-sampler-ready",
            BootstrapVersion = RuntimeProbe.BootstrapVersion,
            ProcessId = Environment.ProcessId,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString()
        });
    }

    public static void WaitForever()
    {
        while (true)
        {
            Thread.Sleep(10_000);
        }
    }

    private int OnFrameCount()
    {
        FrameCountDelegate? original = _original;
        int frameCount = original is null ? 0 : original();

        try
        {
            if (frameCount != _lastStereoPumpFrameCount)
            {
                _lastStereoPumpFrameCount = frameCount;
                TryPumpStereo();
            }

            long now = Environment.TickCount64;
            long previous = Interlocked.Read(ref _lastSampleMilliseconds);
            if (now - previous >= 100 &&
                Interlocked.CompareExchange(ref _lastSampleMilliseconds, now, previous) == previous)
            {
                TryCapture();
            }
        }
        catch (Exception exception)
        {
            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                Event = "sampler-failure",
                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                ProcessId = Environment.ProcessId,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                ErrorType = exception.GetType().FullName,
                Error = exception.Message
            });
        }

        return frameCount;
    }

    private void TryCapture()
    {
        _api.ThreadAttach(_domain);
        IntPtr coreImage = FindImage("UnityEngine.CoreModule.dll");
        int width = InvokeStaticInt(coreImage, "UnityEngine", "Screen", "get_width");
        int height = InvokeStaticInt(coreImage, "UnityEngine", "Screen", "get_height");
        int orientation = InvokeStaticInt(coreImage, "UnityEngine", "Screen", "get_orientation");
        int cameraCount = InvokeStaticInt(coreImage, "UnityEngine", "Camera", "get_allCamerasCount");
        string scene = InvokeSceneName(coreImage);
        _stereoPumpCoreImage = coreImage;
        string signature = $"{width}x{height}|{orientation}|{scene}";
        DateTimeOffset now = DateTimeOffset.UtcNow;
        bool captureM5Topology = ShouldCaptureM5Topology(scene);
        if (!captureM5Topology && _lastVideoPlayers.Count != 0)
        {
            _lastVideoPlayers = Array.Empty<VideoPlayerProbeRecord>();
        }
        if (!captureM5Topology && _lastMediaSurfaces.Count != 0)
        {
            _lastMediaSurfaces = Array.Empty<MediaSurfaceProbeRecord>();
        }

        StereoPerformanceProbeRecord? performance =
            StereoPerformanceTelemetry.SnapshotAndReset();
        if (performance is not null &&
            (IsLiveScene(scene) || _m6NonLiveWorldSurfaceEligible))
        {
            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = now,
                Event = "stereo-performance-sample",
                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                ProcessId = Environment.ProcessId,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                Scene = scene,
                StereoPerformance = performance,
                Reason = "One-second aggregate of Present, source/clone visual-effect render, stereo finalize, GPU wait, publish and OpenXR submission timing."
            });
        }

        if (_portraitResolutionRestorePending &&
            Environment.TickCount64 >= _portraitResolutionRestoreAfterMilliseconds)
        {
            _portraitResolutionRestorePending = false;
            ApplyWindowedResolutionNudgeStep(
                now,
                coreImage,
                scene,
                _canonicalPortraitWidth,
                _canonicalPortraitHeight,
                "portrait-resolution-restored");
            RefreshPortraitCanvasLayout(
                now,
                coreImage,
                scene,
                _canonicalPortraitWidth,
                _canonicalPortraitHeight);
        }

        if (!string.Equals(signature, _lastSignature, StringComparison.Ordinal) ||
            now >= _nextCameraCaptureUtc)
        {
            try
            {
                _lastCameras = CaptureCameras(coreImage, scene);
            }
            catch (Exception exception)
            {
                _lastCameras = Array.Empty<CameraProbeRecord>();
                UnityRenderSourceRegistry.ClearLiveWorldTexture();
                if (now >= _nextCameraFailureUtc)
                {
                    RuntimeProbe.Append(_logPath, new ProbeEvent
                    {
                        TimestampUtc = now,
                        Event = "camera-probe-failure",
                        BootstrapVersion = RuntimeProbe.BootstrapVersion,
                        ProcessId = Environment.ProcessId,
                        Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                        ErrorType = exception.GetType().FullName,
                        Error = exception.Message
                    });
                    _nextCameraFailureUtc = now.AddSeconds(10);
                }
            }

            _nextCameraCaptureUtc = now.AddSeconds(1);
        }

        OrientationDecision orientationDecision = _orientationStabilizer.Observe(new OrientationSample
        {
            Width = width,
            Height = height,
            RenderTargetsValid = width > 0 && height > 0 && cameraCount > 0,
            ExplicitChangeSignal = false,
            ReportedScreenOrientation = orientation,
            TargetSignature = $"{width}x{height}|{scene}",
            NowMilliseconds = Environment.TickCount64
        });
        bool stable = orientationDecision.State is
            OrientationTransitionState.StablePortrait or
            OrientationTransitionState.StableLandscape;
        ClassificationResult classification = Classify(scene, cameraCount, stable);
        string decisionSignature = DecisionSignature(
            scene,
            orientationDecision,
            classification);

        if (!string.Equals(decisionSignature, _lastDecisionSignature, StringComparison.Ordinal))
        {
            AppendPresentationDecision(
                now,
                scene,
                width,
                height,
                orientation,
                cameraCount,
                orientationDecision,
                classification);
            _lastDecisionSignature = decisionSignature;
        }

        if (orientationDecision.RequestRebind)
        {
            if (width < height)
            {
                RefreshPortraitCanvasLayout(now, coreImage, scene, width, height);
                if (_liveStereoGenerationRetireCount == 0)
                {
                    _canonicalPortraitWidth = width;
                    _canonicalPortraitHeight = height;
                }
                else if (_liveStereoGenerationRetireCount >= 2 &&
                    _lastPortraitResolutionNudgeRetireCount <
                        _liveStereoGenerationRetireCount &&
                    _canonicalPortraitWidth > 0 &&
                    _canonicalPortraitHeight > 1)
                {
                    _lastPortraitResolutionNudgeRetireCount =
                        _liveStereoGenerationRetireCount;
                    if (ApplyWindowedResolutionNudgeStep(
                        now,
                        coreImage,
                        scene,
                        _canonicalPortraitWidth,
                        _canonicalPortraitHeight - 1,
                        "portrait-resolution-nudge-applied"))
                    {
                        _portraitResolutionRestorePending = true;
                        _portraitResolutionRestoreAfterMilliseconds =
                            Environment.TickCount64 + 100;
                    }
                }
            }
            OrientationDecision completed = _orientationStabilizer.CompleteRebind(success: true);
            ClassificationResult completedClassification = Classify(scene, cameraCount, stable: true);
            AppendPresentationDecision(
                now,
                scene,
                width,
                height,
                orientation,
                cameraCount,
                completed,
                completedClassification);
            _lastDecisionSignature = DecisionSignature(
                scene,
                completed,
                completedClassification);
        }

        bool signatureChanged = !string.Equals(
            signature,
            _lastSignature,
            StringComparison.Ordinal);
        bool heartbeatDue = now >= _nextHeartbeatUtc;
        bool m5TopologyDue = captureM5Topology && now >= _nextM5TopologyCaptureUtc;
        if (signatureChanged || heartbeatDue || m5TopologyDue)
        {
            IReadOnlyList<CanvasProbeRecord> canvases = Array.Empty<CanvasProbeRecord>();
            IReadOnlyList<RawImageProbeRecord> rawImages = Array.Empty<RawImageProbeRecord>();
            IReadOnlyList<UiGraphicProbeRecord> uiGraphics = Array.Empty<UiGraphicProbeRecord>();
            IReadOnlyList<VideoPlayerProbeRecord> videoPlayers =
                Array.Empty<VideoPlayerProbeRecord>();
            IReadOnlyList<MediaSurfaceProbeRecord> mediaSurfaces =
                Array.Empty<MediaSurfaceProbeRecord>();
            if (EnableUiObjectEnumeration &&
                (IsLiveScene(scene) || captureM5Topology))
            {
                try
                {
                    (canvases, rawImages, uiGraphics) = CaptureUiHierarchy(
                        coreImage,
                        scene,
                        width,
                        height,
                        stable,
                        includeDetailedGraphics: IsLiveScene(scene));
                }
                catch (Exception exception)
                {
                    if (now >= _nextUiFailureUtc)
                    {
                        RuntimeProbe.Append(_logPath, new ProbeEvent
                        {
                            TimestampUtc = now,
                            Event = "ui-probe-failure",
                            BootstrapVersion = RuntimeProbe.BootstrapVersion,
                            ProcessId = Environment.ProcessId,
                            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                            ErrorType = exception.GetType().FullName,
                            Error = exception.Message
                        });
                        _nextUiFailureUtc = now.AddSeconds(10);
                    }
                }

                if (captureM5Topology)
                {
                    try
                    {
                        videoPlayers = CaptureVideoPlayers(coreImage);
                        _lastVideoPlayers = videoPlayers;
                    }
                    catch (Exception exception)
                    {
                        _lastVideoPlayers = Array.Empty<VideoPlayerProbeRecord>();
                        if (now >= _nextVideoFailureUtc)
                        {
                            RuntimeProbe.Append(_logPath, new ProbeEvent
                            {
                                TimestampUtc = now,
                                Event = "video-player-probe-failure",
                                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                                ProcessId = Environment.ProcessId,
                                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                                Scene = scene,
                                ErrorType = exception.GetType().FullName,
                                Error = exception.Message
                            });
                            _nextVideoFailureUtc = now.AddSeconds(10);
                        }
                    }

                    try
                    {
                        mediaSurfaces = CaptureM5MediaSurfaces(coreImage);
                        _lastMediaSurfaces = mediaSurfaces;
                    }
                    catch (Exception exception)
                    {
                        _lastMediaSurfaces = Array.Empty<MediaSurfaceProbeRecord>();
                        if (now >= _nextMediaSurfaceFailureUtc)
                        {
                            RuntimeProbe.Append(_logPath, new ProbeEvent
                            {
                                TimestampUtc = now,
                                Event = "media-surface-probe-failure",
                                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                                ProcessId = Environment.ProcessId,
                                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                                Scene = scene,
                                ErrorType = exception.GetType().FullName,
                                Error = exception.Message
                            });
                            _nextMediaSurfaceFailureUtc = now.AddSeconds(10);
                        }
                    }
                }

                if (IsLiveScene(scene) && !_urpCapabilitiesCaptured)
                {
                    TryCaptureUrpCapabilities(now);
                }

                if (IsLiveScene(scene) && !_uiReplayCapabilitiesCaptured)
                {
                    TryCaptureUiReplayCapabilities(now);
                }

            }

            string topologySignature = BuildTopologySignature(
                _lastCameras,
                canvases,
                rawImages,
                videoPlayers,
                mediaSurfaces);
            bool topologyChanged = !string.Equals(
                topologySignature,
                _lastTopologySignature,
                StringComparison.Ordinal);
            if (signatureChanged || heartbeatDue || topologyChanged)
            {
                RuntimeProbe.Append(_logPath, new ProbeEvent
                {
                    TimestampUtc = now,
                    Event = "render-snapshot",
                    BootstrapVersion = RuntimeProbe.BootstrapVersion,
                    ProcessId = Environment.ProcessId,
                    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                    Width = width,
                    Height = height,
                    Orientation = orientation,
                    CameraCount = cameraCount,
                    Scene = scene,
                    Cameras = _lastCameras,
                    UiCanvases = canvases,
                    RawImages = rawImages,
                    VideoPlayers = videoPlayers,
                    MediaSurfaces = mediaSurfaces,
                    UiGraphics = uiGraphics,
                    Reason = captureM5Topology
                        ? "M5 read-only non-live topology snapshot; URL and authentication data are not collected."
                        : null
                });
                _lastSignature = signature;
                _lastTopologySignature = topologySignature;
                _nextHeartbeatUtc = now.AddSeconds(10);
            }

            if (captureM5Topology)
            {
                _nextM5TopologyCaptureUtc = now.AddMilliseconds(
                    M5TopologyPollMilliseconds);
            }
        }

        bool liveStereoEligible =
            IsConcreteLive3DScene(scene) &&
            width > height &&
            _lastLiveCamera != IntPtr.Zero;
        bool nonLiveStereoEligible =
            IsApprovedNonLive3DScene(scene) &&
            _m6NonLiveWorldSurfaceEligible &&
            _lastLiveCamera != IntPtr.Zero;
        bool stereoPumpEligible = liveStereoEligible || nonLiveStereoEligible;
        bool stereoSourceChanged = stereoPumpEligible &&
            _stereoPumpEligible &&
            (_stereoPumpSourceCamera != _lastLiveCamera ||
                _stereoPumpSourceTexture != _lastLiveTargetTexture);
        if (stereoPumpEligible != _stereoPumpEligible || stereoSourceChanged)
        {
            if (!stereoPumpEligible || stereoSourceChanged)
            {
                RetireStereoCameraGeneration(now, coreImage, scene);
            }
            _stereoPumpEligible = stereoPumpEligible;
            _stereoPumpSourceCamera = stereoPumpEligible
                ? _lastLiveCamera
                : IntPtr.Zero;
            _stereoPumpSourceTexture = stereoPumpEligible
                ? _lastLiveTargetTexture
                : IntPtr.Zero;
            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = now,
                Event = stereoPumpEligible
                    ? nonLiveStereoEligible
                        ? "stereo-non-live-source-ready"
                        : "stereo-live-source-ready"
                    : "stereo-world-source-unavailable",
                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                ProcessId = Environment.ProcessId,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                Scene = scene,
                Width = width,
                Height = height,
                StereoCloneReady = _stereoCloneSetupReady,
                Reason = stereoPumpEligible
                    ? nonLiveStereoEligible
                        ? $"M6 approved an active world-presenting RawImage ({_m6WorldSurfacePath}) bound to the Game3DManager target; stereo requires a dynamic UI-difference layer. sourceChanged={stereoSourceChanged}."
                        : $"A valid source camera is bound in a concrete env_3d_live scene; stereo production may start or resume. sourceChanged={stereoSourceChanged}."
                    : "Stereo is paused and old eye/UI textures were cleared until an approved world-presenting surface and source camera are available."
            });
        }

        long captureNow = Environment.TickCount64;
        if (EnableStereoCloneSetup &&
            _stereoPumpEligible &&
            _lastLiveCamera != IntPtr.Zero &&
            !_stereoCloneSetupAttempted)
        {
            TryEnsureStereoCloneResources(now, coreImage);
        }
        if (_stereoRenderSubmitted &&
            !_stereoDiagnosticSaved &&
            captureNow - _stereoRenderSubmittedMilliseconds >= 2_000)
        {
            TrySaveStereoRenderDiagnostics(now, coreImage);
        }
        if (_stereoOutputValidated &&
            _stereoFrozenUntilMilliseconds > captureNow)
        {
            _ = UnityRenderSourceRegistry.TouchStereoTextures();
        }

        if (EnableUiCaptureSubmission &&
            _urpCapabilitiesCaptured &&
            IsLiveScene(scene) &&
            width > height &&
            captureNow >= _nextUiCaptureMilliseconds)
        {
            _nextUiCaptureMilliseconds = captureNow + 100;
            TrySubmitUiCapture(
                now,
                coreImage,
                width,
                height,
                rawImageObjects: null);
        }

        if (EnableNaturalUiCapture &&
            _stereoOutputValidated &&
            IsLiveScene(scene) &&
            width > height)
        {
            int visibilityState = TryGetLiveUiVisibilityState(coreImage);
            if (visibilityState >= 0 && visibilityState != _liveUiVisibilityState)
            {
                _liveUiVisibilityState = visibilityState;
                if (visibilityState == 0)
                {
                    if (_uiNaturalCaptureArmed)
                    {
                        RestoreNaturalUiCapture(coreImage);
                    }
                    _uiCaptureFrameCount = -1;
                    UnityRenderSourceRegistry.ClearLiveUiTexture();
                }
                else
                {
                    _uiCaptureFrameCount = 0;
                    _nextNaturalUiCaptureRetryMilliseconds =
                        captureNow + NaturalUiVisibilitySettleMilliseconds;
                }
                RuntimeProbe.Append(_logPath, new ProbeEvent
                {
                    TimestampUtc = now,
                    Event = visibilityState == 0
                        ? "ui-natural-visibility-hidden"
                        : "ui-natural-visibility-shown",
                    BootstrapVersion = RuntimeProbe.BootstrapVersion,
                    ProcessId = Environment.ProcessId,
                    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                    UiCaptureFrameCount = _uiCaptureFrameCount,
                    Reason = visibilityState == 0
                        ? "Live UICanvasGroup became hidden; the OpenXR UI texture was cleared."
                        : $"Live UICanvasGroup became visible; capture will wait " +
                            $"{NaturalUiVisibilitySettleMilliseconds} ms for touch effects to settle."
                });
            }

            if (_liveUiVisibilityState == 0)
            {
                UnityRenderSourceRegistry.ClearLiveUiTexture();
            }
            else if (_uiNaturalCaptureArmed ||
                (_uiCaptureFrameCount == 0 &&
                    captureNow >= _nextNaturalUiCaptureRetryMilliseconds))
            {
                TryCaptureNaturalUiLayer(now, coreImage, width, height);
            }
            else
            {
                _ = UnityRenderSourceRegistry.TouchLiveUiTexture(
                    "UICamera natural UI-only (rendered)");
            }
        }

        if (EnableUiElementReplay &&
            _stereoOutputValidated &&
            IsLiveScene(scene) &&
            width > height &&
            captureNow >= _nextUiReplayMilliseconds)
        {
            _nextUiReplayMilliseconds = captureNow + 100;
            TryRenderUiElementLayer(now, coreImage, width, height);
        }
    }

    private int TryGetLiveUiVisibilityState(IntPtr coreImage)
    {
        try
        {
            IntPtr uiModule = FindImage("UnityEngine.UIModule.dll");
            if (_liveUiVisibilityRenderer == IntPtr.Zero)
            {
                IntPtr uiImage = FindImage("UnityEngine.UI.dll");
                IReadOnlyList<IntPtr> graphics = FindObjectsOfTypeAll(
                    coreImage,
                    uiImage,
                    "UnityEngine.UI",
                    "Graphic",
                    4096);
                IntPtr fallbackGraphic = IntPtr.Zero;
                foreach (IntPtr graphic in graphics)
                {
                    string path = GetComponentPath(coreImage, graphic);
                    if (!path.Contains(
                            "/UICanvasGroup/LiveOverlayContent/",
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    fallbackGraphic = fallbackGraphic == IntPtr.Zero
                        ? graphic
                        : fallbackGraphic;
                    if (path.EndsWith(
                            "/UICanvasGroup/LiveOverlayContent/MusicTimeRoot/MusicTime",
                            StringComparison.Ordinal))
                    {
                        fallbackGraphic = graphic;
                        break;
                    }
                }

                if (fallbackGraphic != IntPtr.Zero)
                {
                    _liveUiVisibilityRenderer = InvokeInstanceObject(
                        uiImage,
                        "UnityEngine.UI",
                        "Graphic",
                        "get_canvasRenderer",
                        fallbackGraphic);
                }
            }

            if (_liveUiVisibilityRenderer != IntPtr.Zero)
            {
                try
                {
                    IntPtr rendererGameObject = InvokeInstanceObject(
                        coreImage,
                        "UnityEngine",
                        "Component",
                        "get_gameObject",
                        _liveUiVisibilityRenderer);
                    if (rendererGameObject == IntPtr.Zero || !InvokeInstanceBool(
                            coreImage,
                            "UnityEngine",
                            "GameObject",
                            "get_activeInHierarchy",
                            rendererGameObject))
                    {
                        return 0;
                    }

                    if (InvokeInstanceBool(
                            uiModule,
                            "UnityEngine",
                            "CanvasRenderer",
                            "get_cull",
                            _liveUiVisibilityRenderer))
                    {
                        return 0;
                    }

                    float inheritedAlpha = InvokeInstanceFloat(
                        uiModule,
                        "UnityEngine",
                        "CanvasRenderer",
                        "GetInheritedAlpha",
                        _liveUiVisibilityRenderer);
                    if (inheritedAlpha <= 0.01f)
                    {
                        return 0;
                    }
                    if (inheritedAlpha >= 0.99f)
                    {
                        return 1;
                    }
                }
                catch
                {
                    // Some Unity revisions do not expose GetInheritedAlpha through
                    // IL2CPP metadata. Fall through to the root CanvasGroup check.
                }
            }

            if (_liveUiCanvasGroup == IntPtr.Zero)
            {
                IReadOnlyList<IntPtr> canvasGroups = FindObjectsOfTypeAll(
                    coreImage,
                    uiModule,
                    "UnityEngine",
                    "CanvasGroup");
                foreach (IntPtr canvasGroup in canvasGroups)
                {
                    string path = GetComponentPath(coreImage, canvasGroup);
                    if (path.EndsWith(
                            "/LiveHorizontalRoot/Root/UICanvasGroup",
                            StringComparison.Ordinal))
                    {
                        _liveUiCanvasGroup = canvasGroup;
                        break;
                    }
                }
            }

            if (_liveUiCanvasGroup == IntPtr.Zero)
            {
                return -1;
            }

            IntPtr gameObject = InvokeInstanceObject(
                coreImage,
                "UnityEngine",
                "Component",
                "get_gameObject",
                _liveUiCanvasGroup);
            if (gameObject == IntPtr.Zero || !InvokeInstanceBool(
                    coreImage,
                    "UnityEngine",
                    "GameObject",
                    "get_activeInHierarchy",
                    gameObject))
            {
                return 0;
            }

            float alpha = InvokeInstanceFloat(
                uiModule,
                "UnityEngine",
                "CanvasGroup",
                "get_alpha",
                _liveUiCanvasGroup);
            if (alpha <= 0.01f)
            {
                return 0;
            }
            return alpha >= 0.99f ? 1 : -1;
        }
        catch
        {
            return -1;
        }
    }

    private void TryPumpStereo()
    {
        IntPtr coreImage = _stereoPumpCoreImage;
        if (coreImage == IntPtr.Zero || !_stereoCloneSetupReady ||
            _stereoContinuousFailed)
        {
            return;
        }

        if (!_stereoPumpEligible)
        {
            if (_stereoNaturalRenderArmed)
            {
                StopArmedStereoRender(coreImage);
            }
            return;
        }

        long pumpNow = Environment.TickCount64;
        if (!_stereoStartupGate.IsReady(
                _lastStereoPumpFrameCount,
                D3D11DeviceCapture.PresentSerial))
        {
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        TryLogDrawFlareStats(now, pumpNow);

        if (_stereoNaturalRenderArmed)
        {
            TrySubmitStereoRenderDiagnostic(now, coreImage);
            if (_stereoNaturalRenderArmed || _stereoContinuousFailed)
            {
                return;
            }
        }

        if (pumpNow < _nextStereoRenderMilliseconds)
        {
            return;
        }

        if (_stereoContinuousStartMilliseconds == 0)
        {
            _stereoContinuousStartMilliseconds = pumpNow;
        }
        _nextStereoRenderMilliseconds =
            pumpNow + StereoContinuousIntervalMilliseconds;
        TrySubmitStereoRenderDiagnostic(now, coreImage);
    }

    private void StopArmedStereoRender(IntPtr coreImage)
    {
        try
        {
            InvokeInstanceBooleanSetter(
                coreImage,
                "UnityEngine",
                "Behaviour",
                "set_enabled",
                _stereoLeftCamera,
                false);
            InvokeInstanceBooleanSetter(
                coreImage,
                "UnityEngine",
                "Behaviour",
                "set_enabled",
                _stereoRightCamera,
                false);
        }
        finally
        {
            _stereoNaturalRenderArmed = false;
            Volatile.Write(ref _stereoNaturalRenderCompletionMask, 0);
        }
    }

    private void RetireStereoCameraGeneration(
        DateTimeOffset now,
        IntPtr coreImage,
        string scene)
    {
        bool hadCloneGeneration = _stereoCloneSetupAttempted ||
            _stereoCloneSetupReady ||
            _stereoLeftCamera != IntPtr.Zero ||
            _stereoRightCamera != IntPtr.Zero;
        bool retiredLiveGeneration = hadCloneGeneration &&
            !_stereoGenerationRequiresDynamicUi;
        string stage = "stop-eye-cameras";
        Exception? resetFailure = null;
        try
        {
            if (_stereoNaturalRenderArmed)
            {
                StopArmedStereoRender(coreImage);
            }

            stage = "restore-ui-camera";
            if (_uiNaturalCaptureArmed)
            {
                RestoreNaturalUiCapture(coreImage);
            }
        }
        catch (Exception exception)
        {
            resetFailure = exception;
        }
        finally
        {
            UnityRenderSourceRegistry.ClearStereoTextures();
            UnityRenderSourceRegistry.ClearLiveUiTexture();

            // Scene transitions destroy clone Cameras and can also invalidate
            // their RenderTextures/render-request wrappers even when rooted.
            // Never invoke any Unity generation pointer after source loss.
            _stereoLeftCamera = IntPtr.Zero;
            _stereoRightCamera = IntPtr.Zero;
            _stereoLeftAdditionalCameraData = IntPtr.Zero;
            _stereoRightAdditionalCameraData = IntPtr.Zero;
            _stereoLeftGameObjectHandle = 0;
            _stereoRightGameObjectHandle = 0;
            _stereoLeftCameraHandle = 0;
            _stereoRightCameraHandle = 0;
            _stereoLeftRenderTexture = IntPtr.Zero;
            _stereoRightRenderTexture = IntPtr.Zero;
            _stereoAlternateLeftRenderTexture = IntPtr.Zero;
            _stereoAlternateRightRenderTexture = IntPtr.Zero;
            _stereoThirdLeftRenderTexture = IntPtr.Zero;
            _stereoThirdRightRenderTexture = IntPtr.Zero;
            _stereoLeftRenderTextureHandle = 0;
            _stereoRightRenderTextureHandle = 0;
            _stereoAlternateLeftRenderTextureHandle = 0;
            _stereoAlternateRightRenderTextureHandle = 0;
            _stereoThirdLeftRenderTextureHandle = 0;
            _stereoThirdRightRenderTextureHandle = 0;
            _stereoLeftRenderRequest = IntPtr.Zero;
            _stereoRightRenderRequest = IntPtr.Zero;
            _stereoAlternateLeftRenderRequest = IntPtr.Zero;
            _stereoAlternateRightRenderRequest = IntPtr.Zero;
            _stereoLeftRenderRequestHandle = 0;
            _stereoRightRenderRequestHandle = 0;
            _stereoAlternateLeftRenderRequestHandle = 0;
            _stereoAlternateRightRenderRequestHandle = 0;
            if (_stereoGpuCompletionQuery != IntPtr.Zero)
            {
                D3D11Interop.Release(_stereoGpuCompletionQuery);
                _stereoGpuCompletionQuery = IntPtr.Zero;
            }
            _stereoCloneSetupAttempted = false;
            _stereoCloneSetupReady = false;
            _stereoGenerationRequiresDynamicUi = false;
            _stereoSourceRenderShadows = null;
            _stereoSourcePostProcessing = null;
            _stereoClonePostProcessing = false;
            _stereoVisualEffectOverrideConfigured = false;
            _stereoVisualEffectOverrideApplied = false;
            _stereoVisualEffectFallback = false;
            _stereoVisualEffectOverrideFailed = false;
            _stereoVisualEffectManagedTypes = Array.Empty<IntPtr>();
            _stereoSourceRequiresDepthTexture = null;
            _stereoCloneRequiresDepthTexture = null;
            _stereoSourceRequiresDepthOption = null;
            _stereoCloneRequiresDepthOption = null;
            _stereoRendererIndex = null;
            _stereoSourceRenderType = null;
            _stereoCloneRenderType = null;
            _stereoSourceAntialiasing = null;
            _stereoStartupGate.Reset();
            _stereoBlackFrameRetry.Reset();
            _stereoContinuousStartMilliseconds = 0;
            _nextStereoRenderMilliseconds = 0;
            _stereoContinuousFrameCount = 0;
            _stereoContinuousFailed = false;
            _stereoOutputValidated = false;
            _nextStereoValidationMilliseconds = 0;
            _stereoFrozenUntilMilliseconds = 0;
            _stereoNaturalRenderArmed = false;
            _stereoNaturalRenderStartPresentSerial = 0;
            _stereoNaturalRenderArmTimestamp = 0;
            Volatile.Write(ref _stereoNaturalRenderCompletionMask, 0);
            Volatile.Write(ref _lastStereoSourceRenderPresentSerial, -1);
            _stereoNaturalLeftRenderTexture = IntPtr.Zero;
            _stereoNaturalRightRenderTexture = IntPtr.Zero;
            _stereoPublishedLeftRenderTexture = IntPtr.Zero;
            _stereoPublishedRightRenderTexture = IntPtr.Zero;
            _stereoRenderSubmitted = false;
            _stereoDiagnosticSaved = false;
            _stereoRenderSubmittedMilliseconds = 0;
            _stereoLastPublishMilliseconds = 0;
            _stereoLastPublishPresentSerial = 0;
            _stereoRateWindowStartMilliseconds = 0;
            _stereoRateWindowStartFrameCount = 0;
            _nextStereoStateUnavailableLogUtc = DateTimeOffset.MinValue;

            _cachedThreeDTextureRawImage = IntPtr.Zero;
            _cachedThreeDTextureCanvasRenderer = IntPtr.Zero;
            _cachedUiCamera = IntPtr.Zero;
            _uiCaptureRequest = IntPtr.Zero;
            _uiCaptureRenderTexture = IntPtr.Zero;
            _uiCaptureRequestHandle = 0;
            _uiCaptureRenderTextureHandle = 0;
            _uiCaptureDestinationVerified = false;
            _uiNaturalCamera = IntPtr.Zero;
            _uiNaturalOriginalTargetTexture = IntPtr.Zero;
            _uiNaturalCanvasImage = IntPtr.Zero;
            _uiNaturalSuppressedRenderers.Clear();
            _liveUiCanvasGroup = IntPtr.Zero;
            _liveUiVisibilityRenderer = IntPtr.Zero;
            _liveUiVisibilityState = -1;
            _uiCaptureFrameCount = 0;
            _nextNaturalUiCaptureRetryMilliseconds = 0;
        }

        if (hadCloneGeneration)
        {
            _stereoGenerationRetireCount++;
            if (retiredLiveGeneration)
            {
                _liveStereoGenerationRetireCount++;
            }
            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = now,
                Event = resetFailure is null
                    ? "stereo-camera-generation-retired"
                    : "stereo-camera-generation-retire-failure",
                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                ProcessId = Environment.ProcessId,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                Scene = scene,
                StereoCloneReady = false,
                UiCaptureStage = stage,
                ErrorType = resetFailure?.GetType().FullName,
                Error = resetFailure?.Message,
                Reason = resetFailure is null
                    ? "The scene-bound stereo generation was retired; cameras, eye targets and render requests will be recreated on the next approved world source."
                    : "The clone generation was invalidated despite a best-effort camera reset failure."
            });
        }
    }

    private void RefreshPortraitCanvasLayout(
        DateTimeOffset now,
        IntPtr coreImage,
        string scene,
        int width,
        int height)
    {
        try
        {
            IntPtr canvasImage = FindImage("UnityEngine.UIModule.dll");
            _ = Invoke(
                FindMethod(
                    canvasImage,
                    "UnityEngine",
                    "Canvas",
                    "ForceUpdateCanvases"),
                IntPtr.Zero);
            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = now,
                Event = "portrait-canvas-layout-refreshed",
                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                ProcessId = Environment.ProcessId,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                Scene = scene,
                Width = width,
                Height = height,
                Reason = "The portrait render size stabilized after a transition; Canvas layout was refreshed without changing the game window size."
            });
        }
        catch (Exception exception)
        {
            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = now,
                Event = "portrait-canvas-layout-refresh-failure",
                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                ProcessId = Environment.ProcessId,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                Scene = scene,
                Width = width,
                Height = height,
                ErrorType = exception.GetType().FullName,
                Error = exception.Message
            });
        }
    }

    private bool ApplyWindowedResolutionNudgeStep(
        DateTimeOffset now,
        IntPtr coreImage,
        string scene,
        int width,
        int height,
        string eventName)
    {
        try
        {
            IntPtr screenClass = FindClass(coreImage, "UnityEngine", "Screen");
            IntPtr setResolution = FindMethodBySignature(
                screenClass,
                "SetResolution",
                "System.Int32",
                "System.Int32",
                "System.Boolean");
            IntPtr mscorlib = FindImage("mscorlib.dll");
            _ = InvokeWithObjectArguments(
                setResolution,
                IntPtr.Zero,
                BoxInt32(mscorlib, width),
                BoxInt32(mscorlib, height),
                BoxBoolean(mscorlib, false));
            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = now,
                Event = eventName,
                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                ProcessId = Environment.ProcessId,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                Scene = scene,
                Width = width,
                Height = height,
                Reason = "A one-pixel windowed resolution nudge rebinds the portrait viewport after repeated live exits."
            });
            return true;
        }
        catch (Exception exception)
        {
            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = now,
                Event = "portrait-resolution-nudge-failure",
                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                ProcessId = Environment.ProcessId,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                Scene = scene,
                Width = width,
                Height = height,
                ErrorType = exception.GetType().FullName,
                Error = exception.Message
            });
            return false;
        }
    }

    private ClassificationResult Classify(string scene, int cameraCount, bool stable)
    {
        bool live = IsLiveScene(scene);
        bool commu = IsCommuScene(scene);
        return _sceneClassifier.Classify(new RenderObservation
        {
            IsOrientationChanging = !stable,
            IsSceneChanging = !stable,
            AreRenderTargetsStable = stable,
            StableFrameCount = stable ? 5 : 0,
            HasLiveCameraMarker = live,
            HasUguissMarker = commu,
            HasCinemachineOutput = commu,
            HasFullScreenVideo = _lastVideoPlayers.Any(player =>
                player.Enabled &&
                player.ActiveInHierarchy &&
                (player.IsPlaying == true || player.IsPrepared == true)) ||
                _lastMediaSurfaces.Any(surface =>
                    surface.ActiveInHierarchy &&
                    (surface.IsPlaying == true ||
                        surface.IsPrepared == true ||
                        surface.TextureName is not null)),
            HasValidWorldCamera = cameraCount > 0,
            IsUrpCameraStackValid = cameraCount > 0,
            UiCanvasDominates = IsPanelScene(scene),
            IsProfileApproved = false,
            WorldCameraCount = cameraCount
        });
    }

    private void AppendPresentationDecision(
        DateTimeOffset now,
        string scene,
        int width,
        int height,
        int rawOrientation,
        int cameraCount,
        OrientationDecision orientation,
        ClassificationResult classification)
    {
        RuntimeProbe.Append(_logPath, new ProbeEvent
        {
            TimestampUtc = now,
            Event = "presentation-decision",
            BootstrapVersion = RuntimeProbe.BootstrapVersion,
            ProcessId = Environment.ProcessId,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            Width = width,
            Height = height,
            Orientation = rawOrientation,
            CameraCount = cameraCount,
            Scene = scene,
            TransitionState = orientation.State.ToString(),
            OrientationKind = orientation.Orientation.ToString(),
            FreezeFrame = orientation.FreezeFrame,
            BlockPointerInput = orientation.BlockPointerInput,
            RequestRebind = orientation.RequestRebind,
            PresentationContext = classification.Context.ToString(),
            PresentationMode = classification.Mode.ToString(),
            Reason = classification.Reason
        });
    }

    private static string DecisionSignature(
        string scene,
        OrientationDecision orientation,
        ClassificationResult classification) =>
        string.Join(
            "|",
            scene,
            orientation.State,
            orientation.Orientation,
            orientation.FreezeFrame,
            orientation.BlockPointerInput,
            orientation.RequestRebind,
            classification.Context,
            classification.Mode);

    private static bool IsLiveScene(string scene) =>
        scene.Equals("Live", StringComparison.OrdinalIgnoreCase) ||
        scene.StartsWith("env_3d_live_", StringComparison.OrdinalIgnoreCase);

    private static bool IsConcreteLive3DScene(string scene) =>
        scene.StartsWith("env_3d_live_", StringComparison.OrdinalIgnoreCase);

    private static bool IsCommuScene(string scene) =>
        scene.StartsWith("env_3d_adv_", StringComparison.OrdinalIgnoreCase);

    private static bool IsApprovedNonLive3DScene(string scene) =>
        scene.StartsWith("env_3d_home_", StringComparison.OrdinalIgnoreCase) ||
        IsCommuScene(scene);

    private static bool IsPanelScene(string scene) =>
        scene.Equals("Splash", StringComparison.OrdinalIgnoreCase) ||
        scene.Equals("Title", StringComparison.OrdinalIgnoreCase) ||
        scene.Equals("OutGame", StringComparison.OrdinalIgnoreCase) ||
        scene.Equals("Produce", StringComparison.OrdinalIgnoreCase) ||
        (!IsLiveScene(scene) && !IsCommuScene(scene));

    private static bool ShouldCaptureM5Topology(string scene) =>
        !string.IsNullOrWhiteSpace(scene) &&
        !scene.Equals("Splash", StringComparison.OrdinalIgnoreCase) &&
        !scene.Equals("Title", StringComparison.OrdinalIgnoreCase) &&
        !IsLiveScene(scene);

    private static bool IsApprovedM6WorldSurfacePath(string path) =>
        path.EndsWith("/3dTargetImage", StringComparison.Ordinal) ||
        path.EndsWith("/Main Layer/Render Target", StringComparison.Ordinal);

    private void UpdateM6NonLiveWorldBinding(
        IntPtr coreImage,
        string scene,
        int screenWidth,
        int screenHeight,
        bool orientationStable,
        IntPtr worldSurfaceRawImage,
        string worldSurfacePath)
    {
        if (IsLiveScene(scene))
        {
            return;
        }

        bool concreteScene = IsApprovedNonLive3DScene(scene);
        bool candidateValid =
            (orientationStable || concreteScene) &&
            _m6WorldCandidateCamera != IntPtr.Zero &&
            _m6WorldCandidateTargetTexture != IntPtr.Zero &&
            _m6WorldCandidateTargetWidth > 0 &&
            _m6WorldCandidateTargetHeight > 0 &&
            M6WorldGeometryMatchesScreen(
                screenWidth,
                screenHeight,
                _m6WorldCandidateTargetWidth,
                _m6WorldCandidateTargetHeight);
        if (!candidateValid || (!concreteScene && worldSurfaceRawImage == IntPtr.Zero))
        {
            _m6NonLiveWorldSurfaceEligible = false;
            _m6WorldSurfaceRawImage = IntPtr.Zero;
            _m6WorldSurfacePath = string.Empty;
            _lastLiveCamera = IntPtr.Zero;
            UnityRenderSourceRegistry.ClearLiveWorldTexture();
            _lastLiveTargetTexture = IntPtr.Zero;
            _lastLiveTargetWidth = 0;
            _lastLiveTargetHeight = 0;
            _lastNativeTextureRefreshMilliseconds = 0;
            return;
        }

        _lastLiveCamera = _m6WorldCandidateCamera;
        _m6WorldSurfaceRawImage = worldSurfaceRawImage;
        _m6WorldSurfacePath = worldSurfaceRawImage != IntPtr.Zero
            ? worldSurfacePath
            : $"camera-only:{scene}";
        _m6NonLiveWorldSurfaceEligible = PublishWorldSourceTexture(
            coreImage,
            _m6WorldCandidateTargetTexture,
            _m6WorldCandidateTargetName,
            _m6WorldCandidateTargetWidth,
            _m6WorldCandidateTargetHeight,
            isM6NonLive: true);
        if (!_m6NonLiveWorldSurfaceEligible)
        {
            _lastLiveCamera = IntPtr.Zero;
            _m6WorldSurfaceRawImage = IntPtr.Zero;
            _m6WorldSurfacePath = string.Empty;
        }
    }

    private static bool M6WorldGeometryMatchesScreen(
        int screenWidth,
        int screenHeight,
        int targetWidth,
        int targetHeight)
    {
        if (screenWidth <= 0 || screenHeight <= 0 ||
            targetWidth <= 0 || targetHeight <= 0)
        {
            return false;
        }

        bool screenLandscape = screenWidth > screenHeight;
        bool targetLandscape = targetWidth > targetHeight;
        if (screenLandscape != targetLandscape)
        {
            return false;
        }

        double screenAspect = (double)screenWidth / screenHeight;
        double targetAspect = (double)targetWidth / targetHeight;
        double relativeDifference = Math.Abs(screenAspect - targetAspect) /
            Math.Max(screenAspect, targetAspect);
        return relativeDifference <= 0.02;
    }

    private bool PublishWorldSourceTexture(
        IntPtr coreImage,
        IntPtr targetTexture,
        string targetTextureName,
        int targetTextureWidth,
        int targetTextureHeight,
        bool isM6NonLive)
    {
        string sourceName = isM6NonLive
            ? $"M6_NONLIVE|{targetTextureName}"
            : targetTextureName;
        long nowMilliseconds = Environment.TickCount64;
        bool targetChanged = targetTexture != _lastLiveTargetTexture ||
            targetTextureWidth != _lastLiveTargetWidth ||
            targetTextureHeight != _lastLiveTargetHeight;
        bool refreshDue =
            nowMilliseconds - _lastNativeTextureRefreshMilliseconds >= 5_000;
        if (targetChanged || refreshDue ||
            !UnityRenderSourceRegistry.TouchLiveWorldTexture(sourceName))
        {
            IntPtr nativeTexture = InvokeInstanceIntPtr(
                coreImage,
                "UnityEngine",
                "Texture",
                "GetNativeTexturePtr",
                targetTexture);
            if (nativeTexture == IntPtr.Zero)
            {
                UnityRenderSourceRegistry.ClearLiveWorldTexture();
                return false;
            }

            UnityRenderSourceRegistry.UpdateLiveWorldTexture(
                nativeTexture,
                sourceName);
            _lastLiveTargetTexture = targetTexture;
            _lastLiveTargetWidth = targetTextureWidth;
            _lastLiveTargetHeight = targetTextureHeight;
            _lastNativeTextureRefreshMilliseconds = nowMilliseconds;
        }

        return UnityRenderSourceRegistry.TouchLiveWorldTexture(sourceName);
    }

    private IntPtr ResolveIcall(string name)
    {
        using Utf8String nativeName = new(name);
        IntPtr address = _api.ResolveIcall(nativeName.Pointer);
        return address == IntPtr.Zero
            ? throw new MissingMethodException($"Unity icall not found: {name}")
            : address;
    }

    private IntPtr FindImage(string wantedName)
    {
        IntPtr assemblies = _api.DomainGetAssemblies(_domain, out UIntPtr rawCount);
        ulong count = rawCount.ToUInt64();
        for (ulong index = 0; index < count; index++)
        {
            IntPtr assembly = Marshal.ReadIntPtr(assemblies, checked((int)(index * (ulong)IntPtr.Size)));
            IntPtr image = assembly == IntPtr.Zero ? IntPtr.Zero : _api.AssemblyGetImage(assembly);
            IntPtr namePointer = image == IntPtr.Zero ? IntPtr.Zero : _api.ImageGetName(image);
            string? name = namePointer == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(namePointer);
            if (string.Equals(name, wantedName, StringComparison.Ordinal))
            {
                return image;
            }
        }

        throw new InvalidOperationException($"IL2CPP image not found: {wantedName}");
    }

    private int InvokeStaticInt(IntPtr image, string namespaze, string className, string methodName)
    {
        IntPtr method = FindMethod(image, namespaze, className, methodName);
        IntPtr boxed = Invoke(method, IntPtr.Zero);
        IntPtr value = boxed == IntPtr.Zero ? IntPtr.Zero : _api.ObjectUnbox(boxed);
        if (value == IntPtr.Zero)
        {
            throw new InvalidOperationException($"{className}.{methodName} returned null.");
        }

        return Marshal.ReadInt32(value);
    }

    private string InvokeSceneName(IntPtr coreImage)
    {
        IntPtr getActiveScene = FindMethod(
            coreImage,
            "UnityEngine.SceneManagement",
            "SceneManager",
            "GetActiveScene");
        IntPtr boxedScene = Invoke(getActiveScene, IntPtr.Zero);
        IntPtr sceneValue = boxedScene == IntPtr.Zero ? IntPtr.Zero : _api.ObjectUnbox(boxedScene);
        if (sceneValue == IntPtr.Zero)
        {
            throw new InvalidOperationException("SceneManager.GetActiveScene returned null.");
        }

        IntPtr getName = FindMethod(coreImage, "UnityEngine.SceneManagement", "Scene", "get_name");
        IntPtr managedString = Invoke(getName, sceneValue);
        if (managedString == IntPtr.Zero)
        {
            return string.Empty;
        }

        int length = _api.StringLength(managedString);
        IntPtr characters = _api.StringChars(managedString);
        return length <= 0 || characters == IntPtr.Zero
            ? string.Empty
            : Marshal.PtrToStringUni(characters, length) ?? string.Empty;
    }

    private IReadOnlyList<CameraProbeRecord> CaptureCameras(IntPtr coreImage, string scene)
    {
        IntPtr getAllCameras = FindMethod(coreImage, "UnityEngine", "Camera", "get_allCameras");
        IntPtr cameraArray = Invoke(getAllCameras, IntPtr.Zero);
        if (cameraArray == IntPtr.Zero)
        {
            return Array.Empty<CameraProbeRecord>();
        }

        ulong rawLength = unchecked((ulong)Marshal.ReadInt64(cameraArray, 3 * IntPtr.Size));
        if (rawLength > 64)
        {
            throw new InvalidOperationException($"Unexpected Camera.allCameras length: {rawLength}.");
        }

        List<CameraProbeRecord> cameras = new(checked((int)rawLength));
        bool approvedWorldTextureFound = false;
        bool gameWorldCameraFound = false;
        _m6WorldCandidateCamera = IntPtr.Zero;
        _m6WorldCandidateTargetTexture = IntPtr.Zero;
        _m6WorldCandidateTargetName = string.Empty;
        _m6WorldCandidateTargetWidth = 0;
        _m6WorldCandidateTargetHeight = 0;
        IntPtr renderTextureClass = FindClass(
            coreImage,
            "UnityEngine",
            "RenderTexture");
        int vectorOffset = 4 * IntPtr.Size;
        for (ulong index = 0; index < rawLength; index++)
        {
            IntPtr camera = Marshal.ReadIntPtr(
                cameraArray,
                checked(vectorOffset + ((int)index * IntPtr.Size)));
            if (camera == IntPtr.Zero)
            {
                continue;
            }

            IntPtr cameraGameObject = InvokeInstanceObject(
                coreImage,
                "UnityEngine",
                "Component",
                "get_gameObject",
                camera);

            IntPtr targetTexture = InvokeInstanceObject(
                coreImage,
                "UnityEngine",
                "Camera",
                "get_targetTexture",
                camera);
            string cameraName = InvokeInstanceString(
                coreImage,
                "UnityEngine",
                "Object",
                "get_name",
                camera);
            string? targetTextureName = targetTexture == IntPtr.Zero
                ? null
                : InvokeInstanceString(
                    coreImage,
                    "UnityEngine",
                    "Object",
                    "get_name",
                    targetTexture);
            int? targetTextureWidth = targetTexture == IntPtr.Zero
                ? null
                : InvokeInstanceInt(
                    coreImage,
                    "UnityEngine",
                    "Texture",
                    "get_width",
                    targetTexture);
            int? targetTextureHeight = targetTexture == IntPtr.Zero
                ? null
                : InvokeInstanceInt(
                    coreImage,
                    "UnityEngine",
                    "Texture",
                    "get_height",
                    targetTexture);
            CameraUrpProbeData urp = CaptureCameraUrpData(
                coreImage,
                cameraGameObject);
            bool cameraEnabled = InvokeInstanceBool(
                coreImage,
                "UnityEngine",
                "Behaviour",
                "get_enabled",
                camera);
            bool cameraActive = cameraGameObject != IntPtr.Zero &&
                InvokeInstanceBool(
                    coreImage,
                    "UnityEngine",
                    "GameObject",
                    "get_activeInHierarchy",
                    cameraGameObject);
            if (cameraName.Equals("Game3DManager", StringComparison.Ordinal) &&
                cameraEnabled &&
                cameraActive &&
                targetTexture != IntPtr.Zero &&
                targetTextureWidth.HasValue &&
                targetTextureHeight.HasValue)
            {
                gameWorldCameraFound = true;
                _m6WorldCandidateCamera = camera;
                _m6WorldCandidateTargetTexture = targetTexture;
                _m6WorldCandidateTargetName = targetTextureName ?? cameraName;
                _m6WorldCandidateTargetWidth = targetTextureWidth.Value;
                _m6WorldCandidateTargetHeight = targetTextureHeight.Value;
                if (!IsLiveScene(scene) &&
                    _m6NonLiveWorldSurfaceEligible &&
                    (camera != _lastLiveCamera ||
                        targetTexture != _lastLiveTargetTexture))
                {
                    _m6NonLiveWorldSurfaceEligible = false;
                    _m6WorldSurfaceRawImage = IntPtr.Zero;
                    _m6WorldSurfacePath = string.Empty;
                    _lastLiveCamera = IntPtr.Zero;
                    UnityRenderSourceRegistry.ClearLiveWorldTexture();
                    UnityRenderSourceRegistry.ClearStereoTextures();
                    _lastLiveTargetTexture = IntPtr.Zero;
                    _lastLiveTargetWidth = 0;
                    _lastLiveTargetHeight = 0;
                    _lastNativeTextureRefreshMilliseconds = 0;
                }
                if (IsLiveScene(scene))
                {
                    _m6NonLiveWorldSurfaceEligible = false;
                    _m6WorldSurfaceRawImage = IntPtr.Zero;
                    _m6WorldSurfacePath = string.Empty;
                    _lastLiveCamera = camera;
                    approvedWorldTextureFound = PublishWorldSourceTexture(
                        coreImage,
                        targetTexture,
                        targetTextureName ?? cameraName,
                        targetTextureWidth.Value,
                        targetTextureHeight.Value,
                        isM6NonLive: false);
                }
            }

            cameras.Add(new CameraProbeRecord
            {
                Name = cameraName,
                Path = GetComponentPath(coreImage, camera),
                Enabled = cameraEnabled,
                ActiveInHierarchy = cameraActive,
                Depth = InvokeInstanceFloat(coreImage, "UnityEngine", "Camera", "get_depth", camera),
                CullingMask = InvokeInstanceInt(
                    coreImage,
                    "UnityEngine",
                    "Camera",
                    "get_cullingMask",
                    camera),
                ClearFlags = InvokeInstanceInt(
                    coreImage,
                    "UnityEngine",
                    "Camera",
                    "get_clearFlags",
                    camera),
                CameraType = InvokeInstanceInt(
                    coreImage,
                    "UnityEngine",
                    "Camera",
                    "get_cameraType",
                    camera),
                PixelWidth = InvokeInstanceInt(
                    coreImage,
                    "UnityEngine",
                    "Camera",
                    "get_pixelWidth",
                    camera),
                PixelHeight = InvokeInstanceInt(
                    coreImage,
                    "UnityEngine",
                    "Camera",
                    "get_pixelHeight",
                    camera),
                FieldOfView = InvokeInstanceFloat(
                    coreImage,
                    "UnityEngine",
                    "Camera",
                    "get_fieldOfView",
                    camera),
                NearClipPlane = InvokeInstanceFloat(
                    coreImage,
                    "UnityEngine",
                    "Camera",
                    "get_nearClipPlane",
                    camera),
                FarClipPlane = InvokeInstanceFloat(
                    coreImage,
                    "UnityEngine",
                    "Camera",
                    "get_farClipPlane",
                    camera),
                TargetTexturePresent = targetTexture != IntPtr.Zero,
                TargetTextureName = targetTextureName,
                TargetTextureWidth = targetTextureWidth,
                TargetTextureHeight = targetTextureHeight,
                TargetTextureGraphicsFormat = targetTexture == IntPtr.Zero
                    ? null
                    : TryInvokeInt(
                        renderTextureClass,
                        "get_graphicsFormat",
                        targetTexture),
                TargetTextureDimension = targetTexture == IntPtr.Zero
                    ? null
                    : TryInvokeInt(
                        renderTextureClass,
                        "get_dimension",
                        targetTexture),
                TargetTextureAntiAliasing = targetTexture == IntPtr.Zero
                    ? null
                    : TryInvokeInt(
                        renderTextureClass,
                        "get_antiAliasing",
                        targetTexture),
                HasUniversalAdditionalCameraData = urp.Present,
                UrpRendererIndex = urp.RendererIndex,
                UrpRenderType = urp.RenderType,
                UrpRenderPostProcessing = urp.RenderPostProcessing,
                UrpRequiresDepthTexture = urp.RequiresDepthTexture,
                UrpRequiresColorTexture = urp.RequiresColorTexture,
                UrpCameraStackCount = urp.CameraStackCount,
                UrpCameraStackNames = urp.CameraStackNames
            });
        }

        if (!gameWorldCameraFound)
        {
            _lastLiveCamera = IntPtr.Zero;
            _m6NonLiveWorldSurfaceEligible = false;
            _m6WorldSurfaceRawImage = IntPtr.Zero;
            _m6WorldSurfacePath = string.Empty;
            UnityRenderSourceRegistry.ClearLiveWorldTexture();
            _lastLiveTargetTexture = IntPtr.Zero;
            _lastLiveTargetWidth = 0;
            _lastLiveTargetHeight = 0;
            _lastNativeTextureRefreshMilliseconds = 0;
        }
        else if (IsLiveScene(scene) && !approvedWorldTextureFound)
        {
            _lastLiveCamera = IntPtr.Zero;
            UnityRenderSourceRegistry.ClearLiveWorldTexture();
        }

        return cameras;
    }

    private CameraUrpProbeData CaptureCameraUrpData(
        IntPtr coreImage,
        IntPtr cameraGameObject)
    {
        if (cameraGameObject == IntPtr.Zero)
        {
            return CameraUrpProbeData.Empty;
        }

        try
        {
            if (_m5AdditionalCameraDataClass == IntPtr.Zero)
            {
                IntPtr universalImage = FindImage(
                    "Unity.RenderPipelines.Universal.Runtime.dll");
                _m5AdditionalCameraDataClass = FindClassBySimpleName(
                    universalImage,
                    "UniversalAdditionalCameraData");
                _m5AdditionalCameraDataType = GetManagedType(
                    _m5AdditionalCameraDataClass);
                _m5GetComponent = FindMethodBySignature(
                    FindClass(coreImage, "UnityEngine", "GameObject"),
                    "GetComponent",
                    "System.Type");
                _m5RendererIndexField = TryFindField(
                    _m5AdditionalCameraDataClass,
                    "m_RendererIndex");
            }

            IntPtr additionalData = InvokeWithObjectArgument(
                _m5GetComponent,
                cameraGameObject,
                _m5AdditionalCameraDataType);
            if (additionalData == IntPtr.Zero)
            {
                return CameraUrpProbeData.Empty;
            }

            int? rendererIndex = null;
            if (_m5RendererIndexField != IntPtr.Zero)
            {
                IntPtr storage = Marshal.AllocHGlobal(sizeof(int));
                try
                {
                    _api.FieldGetValue(
                        additionalData,
                        _m5RendererIndexField,
                        storage);
                    rendererIndex = Marshal.ReadInt32(storage);
                }
                finally
                {
                    Marshal.FreeHGlobal(storage);
                }
            }

            List<string> stackNames = new();
            int? stackCount = null;
            try
            {
                IntPtr cameraStack = TryInvokeObject(
                    _m5AdditionalCameraDataClass,
                    "get_cameraStack",
                    additionalData);
                if (cameraStack != IntPtr.Zero)
                {
                    IntPtr stackClass = Marshal.ReadIntPtr(cameraStack);
                    stackCount = TryInvokeInt(
                        stackClass,
                        "get_Count",
                        cameraStack);
                    IntPtr getItem = TryFindMethod(stackClass, "get_Item", 1);
                    int boundedCount = Math.Min(stackCount.GetValueOrDefault(), 16);
                    for (int index = 0; index < boundedCount && getItem != IntPtr.Zero; index++)
                    {
                        IntPtr stackedCamera = InvokeWithObjectArgument(
                            getItem,
                            cameraStack,
                            BoxInt32(FindImage("mscorlib.dll"), index));
                        if (stackedCamera != IntPtr.Zero)
                        {
                            stackNames.Add(InvokeInstanceString(
                                coreImage,
                                "UnityEngine",
                                "Object",
                                "get_name",
                                stackedCamera));
                        }
                    }
                }
            }
            catch
            {
                // Some URP camera types reject cameraStack access. Other fields
                // still provide useful read-only topology evidence.
            }

            return new CameraUrpProbeData
            {
                Present = true,
                RendererIndex = rendererIndex,
                RenderType = TryInvokeInt(
                    _m5AdditionalCameraDataClass,
                    "get_renderType",
                    additionalData),
                RenderPostProcessing = TryInvokeBool(
                    _m5AdditionalCameraDataClass,
                    "get_renderPostProcessing",
                    additionalData),
                RequiresDepthTexture = TryInvokeBool(
                    _m5AdditionalCameraDataClass,
                    "get_requiresDepthTexture",
                    additionalData),
                RequiresColorTexture = TryInvokeBool(
                    _m5AdditionalCameraDataClass,
                    "get_requiresColorTexture",
                    additionalData),
                CameraStackCount = stackCount,
                CameraStackNames = stackNames
            };
        }
        catch
        {
            return CameraUrpProbeData.Empty;
        }
    }

    private void TryEnsureStereoCloneResources(DateTimeOffset now, IntPtr coreImage)
    {
        OpenXrStereoStateSnapshot? stereo = OpenXrStereoStateRegistry.Snapshot(1_500);
        if (stereo is null)
        {
            return;
        }

        _stereoCloneSetupAttempted = true;
        _stereoGenerationRequiresDynamicUi =
            _m6NonLiveWorldSurfaceEligible;
        string stage = "create-render-textures";
        try
        {
            int stereoRenderWidth = Math.Max(
                8,
                ((int)MathF.Round(
                    stereo.RecommendedWidth * _stereoRenderResolutionScale)) & ~7);
            int stereoRenderHeight = Math.Max(
                8,
                ((int)MathF.Round(
                    stereo.RecommendedHeight * _stereoRenderResolutionScale)) & ~7);
            _stereoLeftRenderTexture = CreateStereoRenderTexture(
                coreImage,
                stereoRenderWidth,
                stereoRenderHeight);
            _stereoLeftRenderTextureHandle = RootObject(
                _stereoLeftRenderTexture,
                "left-eye RenderTexture");
            _stereoRightRenderTexture = CreateStereoRenderTexture(
                coreImage,
                stereoRenderWidth,
                stereoRenderHeight);
            _stereoRightRenderTextureHandle = RootObject(
                _stereoRightRenderTexture,
                "right-eye RenderTexture");
            _stereoAlternateLeftRenderTexture = CreateStereoRenderTexture(
                coreImage,
                stereoRenderWidth,
                stereoRenderHeight);
            _stereoAlternateLeftRenderTextureHandle = RootObject(
                _stereoAlternateLeftRenderTexture,
                "alternate left-eye RenderTexture");
            _stereoAlternateRightRenderTexture = CreateStereoRenderTexture(
                coreImage,
                stereoRenderWidth,
                stereoRenderHeight);
            _stereoAlternateRightRenderTextureHandle = RootObject(
                _stereoAlternateRightRenderTexture,
                "alternate right-eye RenderTexture");
            _stereoThirdLeftRenderTexture = CreateStereoRenderTexture(
                coreImage,
                stereoRenderWidth,
                stereoRenderHeight);
            _stereoThirdLeftRenderTextureHandle = RootObject(
                _stereoThirdLeftRenderTexture,
                "third left-eye RenderTexture");
            _stereoThirdRightRenderTexture = CreateStereoRenderTexture(
                coreImage,
                stereoRenderWidth,
                stereoRenderHeight);
            _stereoThirdRightRenderTextureHandle = RootObject(
                _stereoThirdRightRenderTexture,
                "third right-eye RenderTexture");

            stage = "create-camera-components";
            (_stereoLeftCamera, _stereoLeftAdditionalCameraData,
                _stereoLeftGameObjectHandle, _stereoLeftCameraHandle) =
                CreateDisabledStereoCamera(
                    coreImage,
                    _lastLiveCamera,
                    _stereoLeftRenderTexture,
                    "GakumasVR_LeftEyeCamera");
            (_stereoRightCamera, _stereoRightAdditionalCameraData,
                _stereoRightGameObjectHandle, _stereoRightCameraHandle) =
                CreateDisabledStereoCamera(
                    coreImage,
                    _lastLiveCamera,
                    _stereoRightRenderTexture,
                    "GakumasVR_RightEyeCamera");

            stage = "configure-visual-effect-override";
            TryConfigureStereoVisualEffectOverride(now);

            stage = "create-render-requests";
            (_stereoLeftRenderRequest, _stereoLeftRenderRequestHandle) =
                CreateSingleCameraRenderRequest(_stereoLeftRenderTexture);
            (_stereoRightRenderRequest, _stereoRightRenderRequestHandle) =
                CreateSingleCameraRenderRequest(_stereoRightRenderTexture);
            (_stereoAlternateLeftRenderRequest, _stereoAlternateLeftRenderRequestHandle) =
                CreateSingleCameraRenderRequest(_stereoAlternateLeftRenderTexture);
            (_stereoAlternateRightRenderRequest, _stereoAlternateRightRenderRequestHandle) =
                CreateSingleCameraRenderRequest(_stereoAlternateRightRenderTexture);

            stage = "create-gpu-completion-query";
            _stereoGpuCompletionQuery = D3D11Interop.CreateEventQuery(
                D3D11DeviceCapture.Device);

            stage = "verify-native-textures";
            IntPtr leftNativeTexture = InvokeInstanceIntPtr(
                coreImage,
                "UnityEngine",
                "Texture",
                "GetNativeTexturePtr",
                _stereoLeftRenderTexture);
            IntPtr rightNativeTexture = InvokeInstanceIntPtr(
                coreImage,
                "UnityEngine",
                "Texture",
                "GetNativeTexturePtr",
                _stereoRightRenderTexture);
            IntPtr thirdLeftNativeTexture = InvokeInstanceIntPtr(
                coreImage,
                "UnityEngine",
                "Texture",
                "GetNativeTexturePtr",
                _stereoThirdLeftRenderTexture);
            IntPtr thirdRightNativeTexture = InvokeInstanceIntPtr(
                coreImage,
                "UnityEngine",
                "Texture",
                "GetNativeTexturePtr",
                _stereoThirdRightRenderTexture);
            if (leftNativeTexture == IntPtr.Zero || rightNativeTexture == IntPtr.Zero ||
                thirdLeftNativeTexture == IntPtr.Zero || thirdRightNativeTexture == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "A stereo RenderTexture returned a null native texture pointer.");
            }

            stage = "clear-new-eye-render-targets";
            ClearStereoRenderTexture(coreImage, _stereoLeftRenderTexture);
            ClearStereoRenderTexture(coreImage, _stereoRightRenderTexture);
            ClearStereoRenderTexture(coreImage, _stereoAlternateLeftRenderTexture);
            ClearStereoRenderTexture(coreImage, _stereoAlternateRightRenderTexture);
            ClearStereoRenderTexture(coreImage, _stereoThirdLeftRenderTexture);
            ClearStereoRenderTexture(coreImage, _stereoThirdRightRenderTexture);
            D3D11Interop.Flush(D3D11DeviceCapture.Context);

            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = now,
                Event = "stereo-camera-clones-ready",
                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                ProcessId = Environment.ProcessId,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                StereoCloneReady = true,
                StereoRenderWidth = stereoRenderWidth,
                StereoRenderHeight = stereoRenderHeight,
                StereoRenderResolutionScale = _stereoRenderResolutionScale,
                StereoLeftTextureDescription = D3D11Interop.DescribeTexture(leftNativeTexture),
                StereoRightTextureDescription = D3D11Interop.DescribeTexture(rightNativeTexture),
                StereoRenderSubmitted = false,
                StereoSourceRenderShadows = _stereoSourceRenderShadows,
                StereoSourcePostProcessing = _stereoSourcePostProcessing,
                StereoClonePostProcessing = _stereoClonePostProcessing,
                StereoVisualEffectMode = _stereoVisualEffectMode,
                StereoVisualEffectOverrideConfigured =
                    _stereoVisualEffectOverrideConfigured,
                StereoVisualEffectOverrideApplied = _stereoVisualEffectOverrideApplied,
                StereoVisualEffectFallback = _stereoVisualEffectFallback,
                StereoSourceRequiresDepthTexture = _stereoSourceRequiresDepthTexture,
                StereoCloneRequiresDepthTexture = _stereoCloneRequiresDepthTexture,
                StereoSourceRequiresDepthOption = _stereoSourceRequiresDepthOption,
                StereoCloneRequiresDepthOption = _stereoCloneRequiresDepthOption,
                StereoRendererIndex = _stereoRendererIndex,
                StereoSourceRenderType = _stereoSourceRenderType,
                StereoCloneRenderType = _stereoCloneRenderType,
                StereoSourceAntialiasing = _stereoSourceAntialiasing,
                StereoCloneAntialiasing = _stereoSourceAntialiasing,
                OpenXrStereoViewStateFlags = stereo.ViewStateFlags,
                Reason = $"A fresh scene-bound stereo generation is ready; clone depth is explicit and the configured clone-only visual effect mode is ready or has safely fallen back to all post-processing off. Startup waits for {StereoStartupStableFrames} Unity frames and the next Present boundary instead of a fixed warm-up delay."
            });
            _stereoCloneSetupReady = true;
            _stereoStartupGate.Arm(
                _lastStereoPumpFrameCount,
                D3D11DeviceCapture.PresentSerial);
        }
        catch (Exception exception)
        {
            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = now,
                Event = "stereo-camera-clones-failure",
                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                ProcessId = Environment.ProcessId,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                StereoCloneReady = false,
                UiCaptureStage = stage,
                ErrorType = exception.GetType().FullName,
                Error = exception.Message
            });
        }
    }

    private void ClearStereoRenderTexture(IntPtr coreImage, IntPtr renderTexture)
    {
        IntPtr nativeTexture = InvokeInstanceIntPtr(
            coreImage,
            "UnityEngine",
            "Texture",
            "GetNativeTexturePtr",
            renderTexture);
        if (nativeTexture == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "A reused stereo RenderTexture returned a null native texture pointer.");
        }

        D3D11Texture2DDescription description =
            D3D11Interop.GetTextureDescription(nativeTexture);
        int viewFormat = description.Format switch
        {
            27 => 28,
            90 => 87,
            _ => description.Format
        };
        IntPtr renderTargetView = D3D11Interop.CreateRenderTargetView(
            D3D11DeviceCapture.Device,
            nativeTexture,
            viewFormat);
        try
        {
            D3D11Interop.ClearRenderTargetView(
                D3D11DeviceCapture.Context,
                renderTargetView,
                new Color4());
        }
        finally
        {
            D3D11Interop.Release(renderTargetView);
        }
    }

    private void TrySubmitStereoRenderDiagnostic(DateTimeOffset now, IntPtr coreImage)
    {
        try
        {
            if (_stereoNaturalRenderArmed)
            {
                long presentDelta = D3D11DeviceCapture.PresentSerial -
                    _stereoNaturalRenderStartPresentSerial;
                int completionMask = Volatile.Read(
                    ref _stereoNaturalRenderCompletionMask);
                if (completionMask != 3)
                {
                    StereoPerformanceTelemetry.RecordStereoWait(
                        presentBoundaryReady: true,
                        cloneRenderCompletionMask: completionMask);
                    return;
                }

                FinalizeNaturalStereoRender(now, coreImage);
                if (_stereoContinuousFailed)
                {
                    return;
                }
            }

            OpenXrStereoStateSnapshot? stereoSnapshot =
                OpenXrStereoStateRegistry.Snapshot(1_500);
            if (stereoSnapshot is null)
            {
                if (now >= _nextStereoStateUnavailableLogUtc)
                {
                    RuntimeProbe.Append(_logPath, new ProbeEvent
                    {
                        TimestampUtc = now,
                        Event = "stereo-view-state-waiting",
                        BootstrapVersion = RuntimeProbe.BootstrapVersion,
                        ProcessId = Environment.ProcessId,
                        Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                        StereoRenderSubmitted = _stereoRenderSubmitted,
                        Reason = "Fresh OpenXR views are temporarily unavailable; stereo rendering is paused and will retry without invalidating the session."
                    });
                    _nextStereoStateUnavailableLogUtc = now.AddSeconds(10);
                }
                return;
            }
            OpenXrStereoStateSnapshot stereo = stereoSnapshot;
            if (!TrySelectWritableNaturalStereoTargets(coreImage))
            {
                StereoPerformanceTelemetry.RecordStereoBufferReuseBlocked();
                return;
            }
            IntPtr sourceTransform = InvokeInstanceObject(
                coreImage,
                "UnityEngine",
                "Component",
                "get_transform",
                _lastLiveCamera);
            IntPtr transformClass = FindClass(coreImage, "UnityEngine", "Transform");
            IntPtr cameraClass = FindClass(coreImage, "UnityEngine", "Camera");
            IntPtr leftWorldPosition = InvokeWithObjectArgument(
                FindMethodBySignature(transformClass, "TransformPoint", "UnityEngine.Vector3"),
                sourceTransform,
                BoxVector3(
                    coreImage,
                    stereo.Left.PositionX * _stereoWorldEyeOffsetScale,
                    stereo.Left.PositionY * _stereoWorldEyeOffsetScale,
                    stereo.Left.PositionZ * _stereoWorldEyeOffsetScale));
            IntPtr rightWorldPosition = InvokeWithObjectArgument(
                FindMethodBySignature(transformClass, "TransformPoint", "UnityEngine.Vector3"),
                sourceTransform,
                BoxVector3(
                    coreImage,
                    stereo.Right.PositionX * _stereoWorldEyeOffsetScale,
                    stereo.Right.PositionY * _stereoWorldEyeOffsetScale,
                    stereo.Right.PositionZ * _stereoWorldEyeOffsetScale));
            float nearClip = InvokeInstanceFloat(
                coreImage,
                "UnityEngine",
                "Camera",
                "get_nearClipPlane",
                _lastLiveCamera);
            float farClip = InvokeInstanceFloat(
                coreImage,
                "UnityEngine",
                "Camera",
                "get_farClipPlane",
                _lastLiveCamera);

            _ = InvokeWithObjectArgument(
                FindMethodBySignature(cameraClass, "CopyFrom", "UnityEngine.Camera"),
                _stereoLeftCamera,
                _lastLiveCamera);
            _ = InvokeWithObjectArgument(
                FindMethodBySignature(cameraClass, "CopyFrom", "UnityEngine.Camera"),
                _stereoRightCamera,
                _lastLiveCamera);
            _ = InvokeWithObjectArgument(
                FindMethodBySignature(
                    cameraClass,
                "set_targetTexture",
                "UnityEngine.RenderTexture"),
                _stereoLeftCamera,
                _stereoNaturalLeftRenderTexture);
            _ = InvokeWithObjectArgument(
                FindMethodBySignature(
                    cameraClass,
                "set_targetTexture",
                "UnityEngine.RenderTexture"),
                _stereoRightCamera,
                _stereoNaturalRightRenderTexture);
            ApplyNonLiveCloneRenderOrder(coreImage, cameraClass);
            CopyCameraTransform(coreImage, _lastLiveCamera, _stereoLeftCamera);
            CopyCameraTransform(coreImage, _lastLiveCamera, _stereoRightCamera);
            IntPtr leftTransform = InvokeInstanceObject(
                coreImage,
                "UnityEngine",
                "Component",
                "get_transform",
                _stereoLeftCamera);
            IntPtr rightTransform = InvokeInstanceObject(
                coreImage,
                "UnityEngine",
                "Component",
                "get_transform",
                _stereoRightCamera);
            _ = InvokeWithObjectArgument(
                FindMethodBySignature(transformClass, "set_position", "UnityEngine.Vector3"),
                leftTransform,
                leftWorldPosition);
            _ = InvokeWithObjectArgument(
                FindMethodBySignature(
                    cameraClass,
                    "set_projectionMatrix",
                    "UnityEngine.Matrix4x4"),
                _stereoLeftCamera,
                CreateStereoProjectionMatrix(coreImage, stereo.Left, nearClip, farClip));
            _ = InvokeWithObjectArgument(
                FindMethodBySignature(transformClass, "set_position", "UnityEngine.Vector3"),
                rightTransform,
                rightWorldPosition);
            _ = InvokeWithObjectArgument(
                FindMethodBySignature(
                    cameraClass,
                    "set_projectionMatrix",
                    "UnityEngine.Matrix4x4"),
                _stereoRightCamera,
                CreateStereoProjectionMatrix(coreImage, stereo.Right, nearClip, farClip));

            TryApplyStereoVisualEffectOverride(
                now,
                _stereoLeftCamera,
                _stereoLeftAdditionalCameraData);
            TryApplyStereoVisualEffectOverride(
                now,
                _stereoRightCamera,
                _stereoRightAdditionalCameraData);

            Volatile.Write(ref _stereoNaturalRenderCompletionMask, 0);
            InvokeInstanceBooleanSetter(
                coreImage,
                "UnityEngine",
                "Behaviour",
                "set_enabled",
                _stereoLeftCamera,
                true);
            InvokeInstanceBooleanSetter(
                coreImage,
                "UnityEngine",
                "Behaviour",
                "set_enabled",
                _stereoRightCamera,
                true);
            _stereoNaturalRenderArmed = true;
            long armPresentSerial = D3D11DeviceCapture.PresentSerial;
            _stereoNaturalRenderStartPresentSerial = armPresentSerial;
            _stereoNaturalRenderArmTimestamp = Stopwatch.GetTimestamp();
            StereoPerformanceTelemetry.RecordStereoArm(
                Volatile.Read(ref _lastStereoSourceRenderPresentSerial) ==
                    armPresentSerial);
            _stereoRenderSubmitted = true;
            if (_stereoContinuousFrameCount == 0)
            {
                RuntimeProbe.Append(_logPath, new ProbeEvent
                {
                    TimestampUtc = now,
                    Event = "stereo-natural-continuous-started",
                    BootstrapVersion = RuntimeProbe.BootstrapVersion,
                    ProcessId = Environment.ProcessId,
                    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                    StereoRenderSubmitted = true,
                    OpenXrStereoViewStateFlags = stereo.ViewStateFlags,
                    Reason = "Triple-buffered eye cameras publish after both clone renders complete; the first pair retains synchronous GPU validation while subsequent pairs rely on protected D3D11 command ordering, OpenXR final GPU completion, and lease-aware buffer reuse."
                });
            }
        }
        catch (Exception exception)
        {
            _stereoContinuousFailed = true;
            _stereoDiagnosticSaved = true;
            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = now,
                Event = "stereo-natural-render-failure",
                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                ProcessId = Environment.ProcessId,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                StereoRenderSubmitted = _stereoRenderSubmitted,
                ErrorType = exception.GetType().FullName,
                Error = exception.Message
            });
        }
    }

    private bool TrySelectWritableNaturalStereoTargets(IntPtr coreImage)
    {
        int startIndex = _stereoContinuousFrameCount % 3;
        for (int offset = 0; offset < 3; offset++)
        {
            int targetIndex = (startIndex + offset) % 3;
            IntPtr left = targetIndex switch
            {
                0 => _stereoLeftRenderTexture,
                1 => _stereoAlternateLeftRenderTexture,
                _ => _stereoThirdLeftRenderTexture
            };
            IntPtr right = targetIndex switch
            {
                0 => _stereoRightRenderTexture,
                1 => _stereoAlternateRightRenderTexture,
                _ => _stereoThirdRightRenderTexture
            };
            IntPtr leftNativeTexture = InvokeInstanceIntPtr(
                coreImage,
                "UnityEngine",
                "Texture",
                "GetNativeTexturePtr",
                left);
            IntPtr rightNativeTexture = InvokeInstanceIntPtr(
                coreImage,
                "UnityEngine",
                "Texture",
                "GetNativeTexturePtr",
                right);
            if (!UnityRenderSourceRegistry.CanWriteStereoTextures(
                    leftNativeTexture,
                    rightNativeTexture))
            {
                continue;
            }

            _stereoNaturalLeftRenderTexture = left;
            _stereoNaturalRightRenderTexture = right;
            return true;
        }

        return false;
    }

    private void FinalizeNaturalStereoRender(DateTimeOffset now, IntPtr coreImage)
    {
        long finalizeStartedTimestamp = Stopwatch.GetTimestamp();
        long armTimestamp = _stereoNaturalRenderArmTimestamp;
        long presentDelta = D3D11DeviceCapture.PresentSerial -
            _stereoNaturalRenderStartPresentSerial;
        InvokeInstanceBooleanSetter(
            coreImage,
            "UnityEngine",
            "Behaviour",
            "set_enabled",
            _stereoLeftCamera,
            false);
        InvokeInstanceBooleanSetter(
            coreImage,
            "UnityEngine",
            "Behaviour",
            "set_enabled",
            _stereoRightCamera,
            false);
        _stereoNaturalRenderArmed = false;
        long gpuWaitDurationTicks = 0;
        if (!_stereoOutputValidated)
        {
            long gpuWaitStartedTimestamp = Stopwatch.GetTimestamp();
            D3D11Interop.WaitForGpu(
                D3D11DeviceCapture.Context,
                _stereoGpuCompletionQuery,
                2_000);
            gpuWaitDurationTicks = Stopwatch.GetTimestamp() - gpuWaitStartedTimestamp;
        }

        IntPtr leftNativeTexture = InvokeInstanceIntPtr(
            coreImage,
            "UnityEngine",
            "Texture",
            "GetNativeTexturePtr",
            _stereoNaturalLeftRenderTexture);
        IntPtr rightNativeTexture = InvokeInstanceIntPtr(
            coreImage,
            "UnityEngine",
            "Texture",
            "GetNativeTexturePtr",
            _stereoNaturalRightRenderTexture);
        bool leftVisible = true;
        bool rightVisible = true;
        if (!_stereoOutputValidated)
        {
            leftVisible = D3D11Interop.HasVisiblePixels(
                D3D11DeviceCapture.Device,
                D3D11DeviceCapture.Context,
                leftNativeTexture);
            rightVisible = D3D11Interop.HasVisiblePixels(
                D3D11DeviceCapture.Device,
                D3D11DeviceCapture.Context,
                rightNativeTexture);
        }
        _stereoContinuousFrameCount++;
        _stereoRenderSubmittedMilliseconds = Environment.TickCount64;
        StereoPerformanceTelemetry.RecordStereoFinalize(
            armTimestamp,
            presentDelta,
            gpuWaitDurationTicks,
            Stopwatch.GetTimestamp() - finalizeStartedTimestamp);
        _stereoNaturalRenderArmTimestamp = 0;
        if (!leftVisible || !rightVisible)
        {
            long blackNow = Environment.TickCount64;
            StereoBlackFrameDecision decision =
                _stereoBlackFrameRetry.ObserveBlack(blackNow);
            bool timedOut = decision == StereoBlackFrameDecision.TimedOut;
            _stereoContinuousFailed = timedOut;
            _stereoDiagnosticSaved = timedOut;
            if (!timedOut)
            {
                _nextStereoRenderMilliseconds =
                    blackNow + StereoBlackFrameRetryMilliseconds;
            }
            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = now,
                Event = timedOut
                    ? "stereo-natural-output-black-timeout"
                    : "stereo-natural-output-black-retry",
                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                ProcessId = Environment.ProcessId,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                StereoRenderSubmitted = true,
                StereoFrameCount = _stereoContinuousFrameCount,
                Reason = timedOut
                    ? $"Natural render output remained black for {_stereoBlackFrameRetry.AttemptCount} attempts; the front panel remains active until a new source generation is detected. leftVisible={leftVisible};rightVisible={rightVisible}."
                    : $"Natural render output is still black; the front panel remains active and stereo will retry in {StereoBlackFrameRetryMilliseconds} ms. attempt={_stereoBlackFrameRetry.AttemptCount};leftVisible={leftVisible};rightVisible={rightVisible}."
            });
            return;
        }

        _stereoBlackFrameRetry.Reset();
        bool firstValidatedPair = !_stereoOutputValidated;
        _stereoOutputValidated = true;
        UnityRenderSourceRegistry.UpdateStereoTextures(
            leftNativeTexture,
            rightNativeTexture,
            requiresDynamicUi: _m6NonLiveWorldSurfaceEligible);
        StereoPerformanceTelemetry.RecordStereoPublish();
        _stereoPublishedLeftRenderTexture = _stereoNaturalLeftRenderTexture;
        _stereoPublishedRightRenderTexture = _stereoNaturalRightRenderTexture;
        RecordStereoPublishRate(now);
        if (firstValidatedPair)
        {
            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = now,
                Event = "stereo-natural-output-validated",
                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                ProcessId = Environment.ProcessId,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                StereoRenderSubmitted = true,
                StereoFrameCount = _stereoContinuousFrameCount,
                Reason = _m6NonLiveWorldSurfaceEligible
                    ? "The first M6 non-live eye pair was validated; projection submission remains gated on the dynamic UI-difference layer."
                    : "The first normal-render-loop eye pair was validated; continuous triple-buffer publishing is active."
            });
        }
    }

    private void RecordStereoPublishRate(DateTimeOffset now)
    {
        long publishMilliseconds = Environment.TickCount64;
        long presentSerial = D3D11DeviceCapture.PresentSerial;
        long publishInterval = _stereoLastPublishMilliseconds == 0
            ? 0
            : publishMilliseconds - _stereoLastPublishMilliseconds;
        long presentDelta = _stereoLastPublishPresentSerial == 0
            ? 0
            : presentSerial - _stereoLastPublishPresentSerial;
        _stereoLastPublishMilliseconds = publishMilliseconds;
        _stereoLastPublishPresentSerial = presentSerial;

        if (_stereoRateWindowStartMilliseconds == 0)
        {
            _stereoRateWindowStartMilliseconds = publishMilliseconds;
            _stereoRateWindowStartFrameCount = _stereoContinuousFrameCount;
            return;
        }

        long elapsed = publishMilliseconds - _stereoRateWindowStartMilliseconds;
        if (elapsed < 1_000)
        {
            return;
        }

        int publishedPairs =
            _stereoContinuousFrameCount - _stereoRateWindowStartFrameCount;
        float framesPerSecond = publishedPairs * 1_000f / elapsed;
        RuntimeProbe.Append(_logPath, new ProbeEvent
        {
            TimestampUtc = now,
            Event = "stereo-publish-rate",
            BootstrapVersion = RuntimeProbe.BootstrapVersion,
            ProcessId = Environment.ProcessId,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            StereoFrameCount = _stereoContinuousFrameCount,
            StereoPublishFramesPerSecond = framesPerSecond,
            StereoPublishIntervalMilliseconds = publishInterval,
            StereoPublishPresentDelta = presentDelta,
            Reason = "Measured completed eye-pair publication rate from the per-frame stereo pump."
        });
        _stereoRateWindowStartMilliseconds = publishMilliseconds;
        _stereoRateWindowStartFrameCount = _stereoContinuousFrameCount;
    }

    private void TrySubmitStereoRenderDiagnosticLegacy(DateTimeOffset now, IntPtr coreImage)
    {
        try
        {
            OpenXrStereoStateSnapshot stereo = OpenXrStereoStateRegistry.Snapshot(1_500)
                ?? throw new InvalidOperationException("A fresh OpenXR stereo state was not available.");
            bool useAlternateTargets = (_stereoContinuousFrameCount & 1) != 0;
            IntPtr leftRenderTexture = useAlternateTargets
                ? _stereoAlternateLeftRenderTexture
                : _stereoLeftRenderTexture;
            IntPtr rightRenderTexture = useAlternateTargets
                ? _stereoAlternateRightRenderTexture
                : _stereoRightRenderTexture;
            IntPtr leftRenderRequest = useAlternateTargets
                ? _stereoAlternateLeftRenderRequest
                : _stereoLeftRenderRequest;
            IntPtr rightRenderRequest = useAlternateTargets
                ? _stereoAlternateRightRenderRequest
                : _stereoRightRenderRequest;
            IntPtr sourceTransform = InvokeInstanceObject(
                coreImage,
                "UnityEngine",
                "Component",
                "get_transform",
                _lastLiveCamera);
            IntPtr transformClass = FindClass(coreImage, "UnityEngine", "Transform");
            IntPtr cameraClass = FindClass(coreImage, "UnityEngine", "Camera");
            IntPtr leftWorldPosition = InvokeWithObjectArgument(
                FindMethodBySignature(
                    transformClass,
                    "TransformPoint",
                    "UnityEngine.Vector3"),
                sourceTransform,
                BoxVector3(
                    coreImage,
                    stereo.Left.PositionX * _stereoWorldEyeOffsetScale,
                    stereo.Left.PositionY * _stereoWorldEyeOffsetScale,
                    stereo.Left.PositionZ * _stereoWorldEyeOffsetScale));
            IntPtr rightWorldPosition = InvokeWithObjectArgument(
                FindMethodBySignature(
                    transformClass,
                    "TransformPoint",
                    "UnityEngine.Vector3"),
                sourceTransform,
                BoxVector3(
                    coreImage,
                    stereo.Right.PositionX * _stereoWorldEyeOffsetScale,
                    stereo.Right.PositionY * _stereoWorldEyeOffsetScale,
                    stereo.Right.PositionZ * _stereoWorldEyeOffsetScale));
            float nearClip = InvokeInstanceFloat(
                coreImage,
                "UnityEngine",
                "Camera",
                "get_nearClipPlane",
                _lastLiveCamera);
            float farClip = InvokeInstanceFloat(
                coreImage,
                "UnityEngine",
                "Camera",
                "get_farClipPlane",
                _lastLiveCamera);
            IntPtr leftProjection = CreateStereoProjectionMatrix(
                coreImage,
                stereo.Left,
                nearClip,
                farClip);
            IntPtr rightProjection = CreateStereoProjectionMatrix(
                coreImage,
                stereo.Right,
                nearClip,
                farClip);
            IntPtr submit = FindMethod(
                coreImage,
                "UnityEngine",
                "Camera",
                "SubmitRenderRequestsInternal",
                1);

            _ = InvokeWithObjectArgument(
                FindMethodBySignature(cameraClass, "CopyFrom", "UnityEngine.Camera"),
                _stereoLeftCamera,
                _lastLiveCamera);
            _ = InvokeWithObjectArgument(
                FindMethodBySignature(cameraClass, "CopyFrom", "UnityEngine.Camera"),
                _stereoRightCamera,
                _lastLiveCamera);
            _ = InvokeWithObjectArgument(
                FindMethodBySignature(
                    cameraClass,
                "set_targetTexture",
                "UnityEngine.RenderTexture"),
                _stereoLeftCamera,
                leftRenderTexture);
            _ = InvokeWithObjectArgument(
                FindMethodBySignature(
                    cameraClass,
                "set_targetTexture",
                "UnityEngine.RenderTexture"),
                _stereoRightCamera,
                rightRenderTexture);
            ApplyNonLiveCloneRenderOrder(coreImage, cameraClass);
            CopyCameraTransform(coreImage, _lastLiveCamera, _stereoLeftCamera);
            CopyCameraTransform(coreImage, _lastLiveCamera, _stereoRightCamera);
            IntPtr leftTransform = InvokeInstanceObject(
                coreImage,
                "UnityEngine",
                "Component",
                "get_transform",
                _stereoLeftCamera);
            IntPtr rightTransform = InvokeInstanceObject(
                coreImage,
                "UnityEngine",
                "Component",
                "get_transform",
                _stereoRightCamera);
            _ = InvokeWithObjectArgument(
                FindMethodBySignature(transformClass, "set_position", "UnityEngine.Vector3"),
                leftTransform,
                leftWorldPosition);
            _ = InvokeWithObjectArgument(
                FindMethodBySignature(
                    cameraClass,
                    "set_projectionMatrix",
                    "UnityEngine.Matrix4x4"),
                _stereoLeftCamera,
                leftProjection);
            TryApplyStereoVisualEffectOverride(
                now,
                _stereoLeftCamera,
                _stereoLeftAdditionalCameraData);
            InvokeInstanceBooleanSetter(
                coreImage,
                "UnityEngine",
                "Behaviour",
                "set_enabled",
                _stereoLeftCamera,
                false);
            _ = InvokeWithObjectArgument(
                submit,
                _stereoLeftCamera,
                leftRenderRequest);

            _ = InvokeWithObjectArgument(
                FindMethodBySignature(transformClass, "set_position", "UnityEngine.Vector3"),
                rightTransform,
                rightWorldPosition);
            _ = InvokeWithObjectArgument(
                FindMethodBySignature(
                    cameraClass,
                    "set_projectionMatrix",
                    "UnityEngine.Matrix4x4"),
                _stereoRightCamera,
                rightProjection);
            TryApplyStereoVisualEffectOverride(
                now,
                _stereoRightCamera,
                _stereoRightAdditionalCameraData);
            InvokeInstanceBooleanSetter(
                coreImage,
                "UnityEngine",
                "Behaviour",
                "set_enabled",
                _stereoRightCamera,
                false);
            _ = InvokeWithObjectArgument(
                submit,
                _stereoRightCamera,
                rightRenderRequest);
            D3D11Interop.WaitForGpu(
                D3D11DeviceCapture.Context,
                _stereoGpuCompletionQuery,
                1_000);
            _stereoRenderSubmitted = true;
            _stereoRenderSubmittedMilliseconds = Environment.TickCount64;
            _stereoContinuousFrameCount++;
            IntPtr leftNativeTexture = InvokeInstanceIntPtr(
                coreImage,
                "UnityEngine",
                "Texture",
                "GetNativeTexturePtr",
                leftRenderTexture);
            IntPtr rightNativeTexture = InvokeInstanceIntPtr(
                coreImage,
                "UnityEngine",
                "Texture",
                "GetNativeTexturePtr",
                rightRenderTexture);
            long currentMilliseconds = Environment.TickCount64;
            if (!_stereoOutputValidated &&
                _stereoContinuousFrameCount >= 5 &&
                currentMilliseconds >= _nextStereoValidationMilliseconds)
            {
                _nextStereoValidationMilliseconds = currentMilliseconds + 1_000;
                bool leftVisible = D3D11Interop.HasVisiblePixels(
                    D3D11DeviceCapture.Device,
                    D3D11DeviceCapture.Context,
                    leftNativeTexture);
                bool rightVisible = D3D11Interop.HasVisiblePixels(
                    D3D11DeviceCapture.Device,
                    D3D11DeviceCapture.Context,
                    rightNativeTexture);
                if (leftVisible && rightVisible)
                {
                    _stereoOutputValidated = true;
                    _stereoContinuousFailed = true;
                    _stereoFrozenUntilMilliseconds =
                        currentMilliseconds + StereoFrozenDiagnosticDurationMilliseconds;
                    RuntimeProbe.Append(_logPath, new ProbeEvent
                    {
                        TimestampUtc = now,
                        Event = "stereo-output-validated",
                        BootstrapVersion = RuntimeProbe.BootstrapVersion,
                        ProcessId = Environment.ProcessId,
                        Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                        StereoRenderSubmitted = true,
                        StereoFrameCount = _stereoContinuousFrameCount,
                        Reason = "Both eye textures contain visible pixels. Rendering stopped and this completed pair will be held unchanged for 30 seconds."
                    });
                }
                else if (_stereoContinuousFrameCount >= 15)
                {
                    _stereoContinuousFailed = true;
                    RuntimeProbe.Append(_logPath, new ProbeEvent
                    {
                        TimestampUtc = now,
                        Event = "stereo-output-rejected-black",
                        BootstrapVersion = RuntimeProbe.BootstrapVersion,
                        ProcessId = Environment.ProcessId,
                        Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                        StereoRenderSubmitted = true,
                        StereoFrameCount = _stereoContinuousFrameCount,
                        Reason = $"Projection publishing remained disabled: leftVisible={leftVisible};rightVisible={rightVisible}."
                    });
                }
            }

            if (_stereoOutputValidated)
            {
                UnityRenderSourceRegistry.UpdateStereoTextures(
                    leftNativeTexture,
                    rightNativeTexture,
                    requiresDynamicUi: _m6NonLiveWorldSurfaceEligible);
                _stereoPublishedLeftRenderTexture = leftRenderTexture;
                _stereoPublishedRightRenderTexture = rightRenderTexture;
            }
            if (_stereoContinuousFrameCount == 1)
            {
                RuntimeProbe.Append(_logPath, new ProbeEvent
                {
                    TimestampUtc = now,
                    Event = "stereo-continuous-render-started",
                    BootstrapVersion = RuntimeProbe.BootstrapVersion,
                    ProcessId = Environment.ProcessId,
                    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                    StereoRenderSubmitted = true,
                    StereoFrameCount = _stereoContinuousFrameCount,
                    OpenXrStereoViewStateFlags = stereo.ViewStateFlags,
                    Reason = "Post-processing-off clone cameras render only until the first validated pair, which is then frozen for 30 seconds to isolate content changes from layer switching."
                });
            }
        }
        catch (Exception exception)
        {
            _stereoContinuousFailed = true;
            _stereoDiagnosticSaved = true;
            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = now,
                Event = "stereo-continuous-render-failure",
                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                ProcessId = Environment.ProcessId,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                StereoRenderSubmitted = _stereoRenderSubmitted,
                StereoFrameCount = _stereoContinuousFrameCount,
                ErrorType = exception.GetType().FullName,
                Error = exception.Message
            });
        }
    }

    private IntPtr CreateStereoProjectionMatrix(
        IntPtr coreImage,
        OpenXrEyeState eye,
        float nearClip,
        float farClip)
    {
        IntPtr mscorlib = FindImage("mscorlib.dll");
        return InvokeWithObjectArguments(
            FindMethodBySignature(
                FindClass(coreImage, "UnityEngine", "Matrix4x4"),
                "Frustum",
                "System.Single",
                "System.Single",
                "System.Single",
                "System.Single",
                "System.Single",
                "System.Single"),
            IntPtr.Zero,
            BoxSingle(mscorlib, MathF.Tan(eye.FovLeft) * nearClip),
            BoxSingle(mscorlib, MathF.Tan(eye.FovRight) * nearClip),
            BoxSingle(mscorlib, MathF.Tan(eye.FovDown) * nearClip),
            BoxSingle(mscorlib, MathF.Tan(eye.FovUp) * nearClip),
            BoxSingle(mscorlib, nearClip),
            BoxSingle(mscorlib, farClip));
    }

    private IntPtr CreateStereoRenderTexture(
        IntPtr coreImage,
        int width,
        int height)
    {
        IntPtr mscorlib = FindImage("mscorlib.dll");
        IntPtr renderTextureClass = FindClass(coreImage, "UnityEngine", "RenderTexture");
        IntPtr renderTexture = _api.ObjectNew(renderTextureClass);
        if (renderTexture == IntPtr.Zero)
        {
            throw new InvalidOperationException("Could not allocate a stereo RenderTexture.");
        }

        _ = InvokeWithObjectArguments(
            FindMethodBySignature(
                renderTextureClass,
                ".ctor",
                "System.Int32",
                "System.Int32",
                "System.Int32"),
            renderTexture,
            BoxInt32(mscorlib, width),
            BoxInt32(mscorlib, height),
            BoxInt32(mscorlib, 24));
        _ = Invoke(FindMethod(renderTextureClass, "Create"), renderTexture);
        return renderTexture;
    }

    private (IntPtr Request, uint Handle) CreateSingleCameraRenderRequest(IntPtr destination)
    {
        IntPtr universalImage = FindImage("Unity.RenderPipelines.Universal.Runtime.dll");
        IntPtr requestClass = FindClassBySimpleName(universalImage, "SingleCameraRequest");
        IntPtr request = _api.ObjectNew(requestClass);
        uint handle = RootObject(request, "SingleCameraRequest");
        _ = Invoke(FindMethod(requestClass, ".ctor"), request);

        IntPtr destinationField = FindField(requestClass, "destination");
        int destinationOffset = _api.FieldGetOffset(destinationField);
        if (destinationOffset < 2 * IntPtr.Size || destinationOffset > 4_096)
        {
            throw new InvalidOperationException(
                $"Unexpected SingleCameraRequest.destination offset: {destinationOffset}.");
        }

        _api.GcWriteBarrierSetField(
            request,
            IntPtr.Add(request, destinationOffset),
            destination);
        return (request, handle);
    }

    private void CopyCameraTransform(
        IntPtr coreImage,
        IntPtr sourceCamera,
        IntPtr destinationCamera)
    {
        IntPtr sourceTransform = InvokeInstanceObject(
            coreImage,
            "UnityEngine",
            "Component",
            "get_transform",
            sourceCamera);
        IntPtr destinationTransform = InvokeInstanceObject(
            coreImage,
            "UnityEngine",
            "Component",
            "get_transform",
            destinationCamera);
        IntPtr transformClass = FindClass(coreImage, "UnityEngine", "Transform");
        IntPtr position = Invoke(FindMethod(transformClass, "get_position"), sourceTransform);
        IntPtr rotation = Invoke(FindMethod(transformClass, "get_rotation"), sourceTransform);
        _ = InvokeWithObjectArgument(
            FindMethodBySignature(transformClass, "set_position", "UnityEngine.Vector3"),
            destinationTransform,
            position);
        _ = InvokeWithObjectArgument(
            FindMethodBySignature(transformClass, "set_rotation", "UnityEngine.Quaternion"),
            destinationTransform,
            rotation);
    }

    private void TrySaveStereoRenderDiagnostics(DateTimeOffset now, IntPtr coreImage)
    {
        try
        {
            IntPtr leftRenderTexture = _stereoPublishedLeftRenderTexture != IntPtr.Zero
                ? _stereoPublishedLeftRenderTexture
                : _stereoLeftRenderTexture;
            IntPtr rightRenderTexture = _stereoPublishedRightRenderTexture != IntPtr.Zero
                ? _stereoPublishedRightRenderTexture
                : _stereoRightRenderTexture;
            IntPtr leftNativeTexture = InvokeInstanceIntPtr(
                coreImage,
                "UnityEngine",
                "Texture",
                "GetNativeTexturePtr",
                leftRenderTexture);
            IntPtr rightNativeTexture = InvokeInstanceIntPtr(
                coreImage,
                "UnityEngine",
                "Texture",
                "GetNativeTexturePtr",
                rightRenderTexture);
            string logDirectory = Path.GetDirectoryName(_logPath) ?? Directory.GetCurrentDirectory();
            D3D11Interop.SaveTextureBmp(
                D3D11DeviceCapture.Device,
                D3D11DeviceCapture.Context,
                leftNativeTexture,
                Path.Combine(logDirectory, "v0.82-stereo-left.bmp"));
            D3D11Interop.SaveTextureBmp(
                D3D11DeviceCapture.Device,
                D3D11DeviceCapture.Context,
                rightNativeTexture,
                Path.Combine(logDirectory, "v0.82-stereo-right.bmp"));
            _stereoDiagnosticSaved = true;
            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = now,
                Event = "stereo-one-shot-render-saved",
                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                ProcessId = Environment.ProcessId,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                StereoRenderSubmitted = true,
                StereoDiagnosticSaved = true,
                StereoFrameCount = _stereoContinuousFrameCount,
                StereoLeftTextureDescription = D3D11Interop.DescribeTexture(leftNativeTexture),
                StereoRightTextureDescription = D3D11Interop.DescribeTexture(rightNativeTexture)
            });
        }
        catch (Exception exception)
        {
            _stereoDiagnosticSaved = true;
            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = now,
                Event = "stereo-one-shot-render-save-failure",
                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                ProcessId = Environment.ProcessId,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                StereoRenderSubmitted = true,
                StereoDiagnosticSaved = false,
                ErrorType = exception.GetType().FullName,
                Error = exception.Message
            });
        }
    }

    private void ApplyNonLiveCloneRenderOrder(
        IntPtr coreImage,
        IntPtr cameraClass)
    {
        if (!_stereoGenerationRequiresDynamicUi)
        {
            return;
        }

        float sourceDepth = InvokeInstanceFloat(
            coreImage,
            "UnityEngine",
            "Camera",
            "get_depth",
            _lastLiveCamera);
        IntPtr boxedDepth = BoxSingle(
            FindImage("mscorlib.dll"),
            sourceDepth - 100f);
        IntPtr setDepth = FindMethodBySignature(
            cameraClass,
            "set_depth",
            "System.Single");
        _ = InvokeWithObjectArgument(setDepth, _stereoLeftCamera, boxedDepth);
        _ = InvokeWithObjectArgument(setDepth, _stereoRightCamera, boxedDepth);
    }

    private (IntPtr Camera, IntPtr AdditionalCameraData,
        uint GameObjectHandle, uint CameraHandle)
        CreateDisabledStereoCamera(
            IntPtr coreImage,
            IntPtr sourceCamera,
            IntPtr targetTexture,
            string name)
    {
        IntPtr gameObjectClass = FindClass(coreImage, "UnityEngine", "GameObject");
        IntPtr cameraClass = FindClass(coreImage, "UnityEngine", "Camera");
        IntPtr gameObject = _api.ObjectNew(gameObjectClass);
        uint gameObjectHandle = RootObject(gameObject, name + " GameObject");
        _ = InvokeWithObjectArgument(
            FindMethodBySignature(gameObjectClass, ".ctor", "System.String"),
            gameObject,
            NewManagedString(name));

        Il2CppApi.ClassGetTypeDelegate classGetType = _api.ClassGetType ??
            throw new MissingMethodException("GameAssembly.dll does not export il2cpp_class_get_type.");
        Il2CppApi.TypeGetObjectDelegate typeGetObject = _api.TypeGetObject ??
            throw new MissingMethodException("GameAssembly.dll does not export il2cpp_type_get_object.");
        IntPtr cameraType = typeGetObject(classGetType(cameraClass));
        if (cameraType == IntPtr.Zero)
        {
            throw new InvalidOperationException("Could not create System.Type for Camera.");
        }

        IntPtr camera = InvokeWithObjectArgument(
            FindMethodBySignature(gameObjectClass, "AddComponent", "System.Type"),
            gameObject,
            cameraType);
        uint cameraHandle = RootObject(camera, name + " component");
        _ = InvokeWithObjectArgument(
            FindMethodBySignature(cameraClass, "CopyFrom", "UnityEngine.Camera"),
            camera,
            sourceCamera);
        IntPtr additionalCameraData = CopyUniversalAdditionalCameraData(
            coreImage,
            sourceCamera,
            gameObject,
            name);
        _ = InvokeWithObjectArgument(
            FindMethodBySignature(
                cameraClass,
                "set_targetTexture",
                "UnityEngine.RenderTexture"),
            camera,
            targetTexture);
        InvokeInstanceBooleanSetter(
            coreImage,
            "UnityEngine",
            "Behaviour",
            "set_enabled",
            camera,
            false);
        _ = InvokeWithObjectArgument(
            FindMethodBySignature(
                FindClass(coreImage, "UnityEngine", "Object"),
                "DontDestroyOnLoad",
                "UnityEngine.Object"),
            IntPtr.Zero,
            gameObject);
        return (camera, additionalCameraData, gameObjectHandle, cameraHandle);
    }

    private IntPtr CopyUniversalAdditionalCameraData(
        IntPtr coreImage,
        IntPtr sourceCamera,
        IntPtr destinationGameObject,
        string name)
    {
        IntPtr universalImage = FindImage("Unity.RenderPipelines.Universal.Runtime.dll");
        IntPtr additionalDataClass = FindClassBySimpleName(
            universalImage,
            "UniversalAdditionalCameraData");
        Il2CppApi.ClassGetTypeDelegate classGetType = _api.ClassGetType ??
            throw new MissingMethodException("GameAssembly.dll does not export il2cpp_class_get_type.");
        Il2CppApi.TypeGetObjectDelegate typeGetObject = _api.TypeGetObject ??
            throw new MissingMethodException("GameAssembly.dll does not export il2cpp_type_get_object.");
        IntPtr additionalDataType = typeGetObject(classGetType(additionalDataClass));
        if (additionalDataType == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Could not create System.Type for UniversalAdditionalCameraData.");
        }

        IntPtr gameObjectClass = FindClass(coreImage, "UnityEngine", "GameObject");
        IntPtr sourceGameObject = InvokeInstanceObject(
            coreImage,
            "UnityEngine",
            "Component",
            "get_gameObject",
            sourceCamera);
        IntPtr getComponent = FindMethodBySignature(
            gameObjectClass,
            "GetComponent",
            "System.Type");
        IntPtr sourceAdditionalData = InvokeWithObjectArgument(
            getComponent,
            sourceGameObject,
            additionalDataType);
        if (sourceAdditionalData == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "The live source camera has no UniversalAdditionalCameraData component.");
        }

        IntPtr destinationAdditionalData = InvokeWithObjectArgument(
            FindMethodBySignature(gameObjectClass, "AddComponent", "System.Type"),
            destinationGameObject,
            additionalDataType);
        if (destinationAdditionalData == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Could not add UniversalAdditionalCameraData to a stereo camera.");
        }

        _ = RootObject(destinationAdditionalData, name + " URP camera data");
        IntPtr rendererIndexField = TryFindField(additionalDataClass, "m_RendererIndex");
        if (rendererIndexField == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "UniversalAdditionalCameraData.m_RendererIndex was not available.");
        }

        IntPtr rendererIndexStorage = Marshal.AllocHGlobal(sizeof(int));
        try
        {
            _api.FieldGetValue(sourceAdditionalData, rendererIndexField, rendererIndexStorage);
            int rendererIndex = Marshal.ReadInt32(rendererIndexStorage);
            _stereoRendererIndex = rendererIndex;
            _ = InvokeWithObjectArgument(
                FindMethod(additionalDataClass, "SetRenderer", 1),
                destinationAdditionalData,
                BoxInt32(FindImage("mscorlib.dll"), rendererIndex));
        }
        finally
        {
            Marshal.FreeHGlobal(rendererIndexStorage);
        }

        string[] copiedProperties =
        {
            "renderShadows",
            "requiresDepthOption",
            "requiresColorOption",
            "requiresDepthTexture",
            "requiresColorTexture",
            "renderType",
            "clearDepth",
            "volumeLayerMask",
            "volumeTrigger",
            "volumeFrameworkUpdateMode",
            "renderPostProcessing",
            "antialiasing",
            "antialiasingQuality",
            "stopNaN",
            "dithering",
            "allowXRRendering",
            "useScreenCoordOverride",
            "screenSizeOverride",
            "screenCoordScaleBias"
        };
        int copiedCount = 0;
        foreach (string propertyName in copiedProperties)
        {
            IntPtr getter = TryFindMethod(additionalDataClass, "get_" + propertyName, 0);
            IntPtr setter = TryFindMethod(additionalDataClass, "set_" + propertyName, 1);
            if (getter == IntPtr.Zero || setter == IntPtr.Zero)
            {
                continue;
            }

            try
            {
                IntPtr value = Invoke(getter, sourceAdditionalData);
                _ = InvokeWithObjectArgument(setter, destinationAdditionalData, value);
                copiedCount++;
            }
            catch
            {
                // Unity/URP versions expose slightly different optional properties.
                // Renderer selection is mandatory; optional property failures are tolerated.
            }
        }

        if (copiedCount == 0)
        {
            throw new InvalidOperationException(
                "No UniversalAdditionalCameraData properties could be copied.");
        }

        _stereoSourceRequiresDepthTexture = TryInvokeBool(
            additionalDataClass,
            "get_requiresDepthTexture",
            sourceAdditionalData);
        _stereoSourceRequiresDepthOption = TryInvokeInt(
            additionalDataClass,
            "get_requiresDepthOption",
            sourceAdditionalData);
        _stereoSourceRenderType = TryInvokeInt(
            additionalDataClass,
            "get_renderType",
            sourceAdditionalData);

        IntPtr requiresDepthTextureSetter = TryFindMethod(
            additionalDataClass,
            "set_requiresDepthTexture",
            1);
        if (requiresDepthTextureSetter == IntPtr.Zero)
        {
            throw new MissingMethodException(
                "UniversalAdditionalCameraData.set_requiresDepthTexture was not available.");
        }

        _ = InvokeWithObjectArgument(
            requiresDepthTextureSetter,
            destinationAdditionalData,
            BoxBoolean(FindImage("mscorlib.dll"), RequireStereoDepthTexture));
        _stereoCloneRequiresDepthTexture = TryInvokeBool(
            additionalDataClass,
            "get_requiresDepthTexture",
            destinationAdditionalData);
        _stereoCloneRequiresDepthOption = TryInvokeInt(
            additionalDataClass,
            "get_requiresDepthOption",
            destinationAdditionalData);
        _stereoCloneRenderType = TryInvokeInt(
            additionalDataClass,
            "get_renderType",
            destinationAdditionalData);

        if (_stereoCloneRequiresDepthTexture != true)
        {
            throw new InvalidOperationException(
                "The stereo camera did not retain its explicit depth texture requirement.");
        }

        _stereoSourceRenderShadows = TryInvokeBool(
            additionalDataClass,
            "get_renderShadows",
            sourceAdditionalData);
        _stereoSourcePostProcessing = TryInvokeBool(
            additionalDataClass,
            "get_renderPostProcessing",
            sourceAdditionalData);

        IntPtr renderPostProcessingSetter = TryFindMethod(
            additionalDataClass,
            "set_renderPostProcessing",
            1);
        if (renderPostProcessingSetter == IntPtr.Zero)
        {
            throw new MissingMethodException(
                "UniversalAdditionalCameraData.set_renderPostProcessing was not available.");
        }

        bool enablePostProcessing =
            !_stereoVisualEffectMode.Equals("all-off", StringComparison.Ordinal) &&
            !(_stereoVisualEffectMode.Equals(
                    VrVisualEffectModes.Manual,
                    StringComparison.Ordinal) &&
                !_manualVisualEffects.PostProcessingEnabled);
        _ = InvokeWithObjectArgument(
            renderPostProcessingSetter,
            destinationAdditionalData,
            BoxBoolean(FindImage("mscorlib.dll"), enablePostProcessing));
        _stereoClonePostProcessing = enablePostProcessing;

        IntPtr antialiasingGetter = TryFindMethod(
            additionalDataClass,
            "get_antialiasing",
            0);
        if (antialiasingGetter == IntPtr.Zero)
        {
            throw new MissingMethodException(
                "UniversalAdditionalCameraData.get_antialiasing was not available.");
        }

        IntPtr boxedAntialiasing = Invoke(antialiasingGetter, sourceAdditionalData);
        IntPtr antialiasingValue = boxedAntialiasing == IntPtr.Zero
            ? IntPtr.Zero
            : _api.ObjectUnbox(boxedAntialiasing);
        if (antialiasingValue == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "UniversalAdditionalCameraData.get_antialiasing returned null.");
        }

        _stereoSourceAntialiasing = Marshal.ReadInt32(antialiasingValue);
        return destinationAdditionalData;
    }

    private void TryConfigureStereoVisualEffectOverride(DateTimeOffset now)
    {
        TryLogVlRenderPathDiagnostics(now);

        if (_stereoVisualEffectMode.Equals("all-off", StringComparison.Ordinal))
        {
            SetStereoPostProcessing(_stereoLeftAdditionalCameraData, false);
            SetStereoPostProcessing(_stereoRightAdditionalCameraData, false);
            _stereoClonePostProcessing = false;
            _stereoVisualEffectOverrideConfigured = true;
            return;
        }

        if (_stereoVisualEffectMode.Equals("all-on", StringComparison.Ordinal))
        {
            _stereoClonePostProcessing = true;
            _stereoVisualEffectOverrideConfigured = true;
            return;
        }

        if (_stereoVisualEffectMode.Equals(
                VrVisualEffectModes.Manual,
                StringComparison.Ordinal) &&
            !_manualVisualEffects.PostProcessingEnabled)
        {
            SetStereoPostProcessing(_stereoLeftAdditionalCameraData, false);
            SetStereoPostProcessing(_stereoRightAdditionalCameraData, false);
            _stereoClonePostProcessing = false;
            _stereoVisualEffectOverrideConfigured = true;
            return;
        }

        if (UsesVlPostProcessHooks(_stereoVisualEffectMode))
        {
            try
            {
                InstallDrawFlareHook();
                _stereoClonePostProcessing = true;
                _stereoVisualEffectOverrideConfigured = true;
                _stereoVisualEffectOverrideApplied = true;
                RuntimeProbe.Append(_logPath, new ProbeEvent
                {
                    TimestampUtc = now,
                    Event = "stereo-visual-effect-override-ready",
                    BootstrapVersion = RuntimeProbe.BootstrapVersion,
                    ProcessId = Environment.ProcessId,
                    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                    StereoClonePostProcessing = true,
                    StereoVisualEffectMode = _stereoVisualEffectMode,
                    StereoVisualEffectOverrideConfigured = true,
                    StereoVisualEffectOverrideApplied = true,
                    StereoVisualEffectFallback = false,
                    Reason = _stereoVisualEffectMode switch
                    {
                        "vl-custom-pass-off" =>
                            "Stereo clones bypass the game-specific VLPostProcessPass.Render and invoke the inherited standard URP PostProcessPass.Render; source cameras keep the original VL path.",
                        "vl-setup-bloom-off" =>
                            "VLPostProcessPass.SetupVLBloom is bypassed only inside an identified stereo clone render; diffusion and the other VL stages remain enabled.",
                        "vl-star-streak-method-off" =>
                            "VLPostProcessPass.DrawStarStreak is bypassed only inside an identified stereo clone render; VLBloom and the other VL stages remain enabled.",
                        "vl-bloom-half" =>
                            "VLBloom intensity is scaled to 50 percent only during an identified stereo clone SetupVLBloom call and immediately restored; source cameras and other VL stages remain unchanged.",
                        "vl-bloom-half-vldof-off" =>
                            "VLBloom intensity is scaled to 50 percent and VLPostProcessPass.DoVLDOF is bypassed only for identified stereo clones; source cameras remain unchanged.",
                        "vl-bloom-half-vldof-textureblur-off" =>
                            "VLBloom intensity is scaled to 50 percent and both VLPostProcessPass.DoVLDOF and DoVLTextureBlur are bypassed only for identified stereo clones; source cameras remain unchanged.",
                        "vl-bloom-threshold-vldof-textureblur-off" =>
                            "VLBloom intensity remains at the source value while threshold is raised by 0.5; VL depth of field and texture blur are bypassed only for identified stereo clones.",
                        "vl-bloom-diffusion-half-vldof-textureblur-off" =>
                            "VLBloom intensity and threshold remain at source values while diffusion is scaled to 50 percent; VL depth of field and texture blur are bypassed only for identified stereo clones.",
                        "vl-bloom-140-diffusion-min-vldof-textureblur-off" =>
                            "VLBloom intensity is scaled to 140 percent and integer diffusion is clamped to its minimum enabled step; threshold remains at the source value, and VL depth of field and texture blur are bypassed only for identified stereo clones.",
                        VrVisualEffectModes.Manual =>
                            $"Manual clone VFX: bloom={_manualVisualEffects.VlBloomEnabled};" +
                            $"bloomIntensity={_manualVisualEffects.VlBloomIntensityScale:R};" +
                            $"bloomDiffusion={_manualVisualEffects.VlBloomDiffusion};" +
                            $"dof={_manualVisualEffects.VlDepthOfFieldEnabled};" +
                            $"textureBlur={_manualVisualEffects.VlTextureBlurEnabled};" +
                            $"starStreak={_manualVisualEffects.VlStarStreakEnabled};" +
                            $"flare={_manualVisualEffects.VlFlareEnabled}.",
                        _ =>
                            "VLPostProcessPass.DrawFlare is bypassed only while RenderingData identifies a left or right stereo clone."
                    }
                });
            }
            catch (Exception exception)
            {
                FallBackToStereoPostProcessingOff(now, exception);
            }
            return;
        }

        try
        {
            IntPtr universalImage = FindImage("Unity.RenderPipelines.Universal.Runtime.dll");
            IntPtr corePipelineImage = FindImage("Unity.RenderPipelines.Core.Runtime.dll");
            IntPtr mscorlibImage = FindImage("mscorlib.dll");
            IntPtr cameraExtensionsClass = FindClass(
                universalImage,
                "UnityEngine.Rendering.Universal",
                "CameraExtensions");
            IntPtr updateModeClass = FindClass(
                universalImage,
                "UnityEngine.Rendering.Universal",
                "VolumeFrameworkUpdateMode");
            IntPtr updateModeType = (_api.TypeGetObject ??
                throw new MissingMethodException(
                    "GameAssembly.dll does not export il2cpp_type_get_object."))(
                    (_api.ClassGetType ??
                        throw new MissingMethodException(
                            "GameAssembly.dll does not export il2cpp_class_get_type."))(
                        updateModeClass));
            IntPtr viaScripting = InvokeWithObjectArguments(
                FindMethodBySignature(
                    FindClass(mscorlibImage, "System", "Enum"),
                    "Parse",
                    "System.Type",
                    "System.String"),
                IntPtr.Zero,
                updateModeType,
                NewManagedString("ViaScripting"));

            _stereoSetVolumeFrameworkUpdateMode = FindMethodBySignature(
                cameraExtensionsClass,
                "SetVolumeFrameworkUpdateMode",
                "UnityEngine.Camera",
                "UnityEngine.Rendering.Universal.VolumeFrameworkUpdateMode");
            _stereoUpdateVolumeStack = FindMethodBySignature(
                cameraExtensionsClass,
                "UpdateVolumeStack",
                "UnityEngine.Camera");

            IntPtr additionalDataClass = FindClassBySimpleName(
                universalImage,
                "UniversalAdditionalCameraData");
            _stereoGetVolumeStack = FindMethod(additionalDataClass, "get_volumeStack");
            IntPtr volumeStackClass = FindClass(
                corePipelineImage,
                "UnityEngine.Rendering",
                "VolumeStack");
            _stereoVolumeStackGetComponent = FindMethodBySignature(
                volumeStackClass,
                "GetComponent",
                "System.Type");
            IntPtr volumeComponentClass = FindClass(
                corePipelineImage,
                "UnityEngine.Rendering",
                "VolumeComponent");
            _stereoVolumeComponentActiveField = FindField(
                volumeComponentClass,
                "active");

            if (_stereoVisualEffectMode.Equals(
                    "volume-components-off",
                    StringComparison.Ordinal))
            {
                string[] universalComponents =
                {
                    "Bloom", "ChannelMixer", "ChromaticAberration",
                    "ColorAdjustments", "ColorCurves", "DepthOfField",
                    "FilmGrain", "LensDistortion", "LiftGammaGain",
                    "MotionBlur", "PaniniProjection",
                    "ShadowsMidtonesHighlights", "SplitToning",
                    "Tonemapping", "Vignette", "WhiteBalance"
                };
                string[] vlComponents =
                {
                    "VLBloom", "VLChromaticAberration", "VLDOF",
                    "VLDiffusion", "VLMotionBlur", "VLParaffin",
                    "VLRGBBlockNoise", "VLScreenSpaceAmbientOcclusion",
                    "VLScreenSpaceReflection", "VLStarStreak",
                    "VLTextureBlur", "VLTonemapping", "VLVirtualEffect",
                    "VLWaveJitterGlitch"
                };
                List<IntPtr> managedTypes = new();
                foreach (string component in universalComponents)
                {
                    managedTypes.Add(GetManagedType(FindClass(
                        universalImage,
                        "UnityEngine.Rendering.Universal",
                        component)));
                }

                foreach (string component in vlComponents)
                {
                    managedTypes.Add(GetManagedType(
                        FindClassAcrossImages("VL.Rendering", component)));
                }

                _stereoVisualEffectManagedTypes = managedTypes.ToArray();
            }
            else
            {
                IntPtr effectClass = _stereoVisualEffectMode switch
                {
                    "vlbloom-off" =>
                        FindClassAcrossImages("VL.Rendering", "VLBloom"),
                    "bloom-off" =>
                        FindClass(
                            universalImage,
                            "UnityEngine.Rendering.Universal",
                            "Bloom"),
                    "vltonemapping-off" =>
                        FindClassAcrossImages("VL.Rendering", "VLTonemapping"),
                    "vlstarstreak-off" =>
                        FindClassAcrossImages("VL.Rendering", "VLStarStreak"),
                    "vltextureblur-off" =>
                        FindClassAcrossImages("VL.Rendering", "VLTextureBlur"),
                    "vldiffusion-off" =>
                        FindClassAcrossImages("VL.Rendering", "VLDiffusion"),
                    "vlparaffin-off" =>
                        FindClassAcrossImages("VL.Rendering", "VLParaffin"),
                    "tonemapping-off" =>
                        FindClass(
                            universalImage,
                            "UnityEngine.Rendering.Universal",
                            "Tonemapping"),
                    "color-adjustments-off" =>
                        FindClass(
                            universalImage,
                            "UnityEngine.Rendering.Universal",
                            "ColorAdjustments"),
                    "depth-of-field-off" =>
                        FindClass(
                            universalImage,
                            "UnityEngine.Rendering.Universal",
                            "DepthOfField"),
                    "motion-blur-off" =>
                        FindClass(
                            universalImage,
                            "UnityEngine.Rendering.Universal",
                            "MotionBlur"),
                    _ => throw new InvalidOperationException(
                        $"Unsupported stereo visual effect mode: {_stereoVisualEffectMode}")
                };
                _stereoVisualEffectManagedTypes = new[]
                {
                    GetManagedType(effectClass)
                };
            }

            _ = InvokeWithObjectArguments(
                _stereoSetVolumeFrameworkUpdateMode,
                IntPtr.Zero,
                _stereoLeftCamera,
                viaScripting);
            _ = InvokeWithObjectArguments(
                _stereoSetVolumeFrameworkUpdateMode,
                IntPtr.Zero,
                _stereoRightCamera,
                viaScripting);
            _stereoVisualEffectOverrideConfigured = true;
            _stereoClonePostProcessing = true;
        }
        catch (Exception exception)
        {
            FallBackToStereoPostProcessingOff(now, exception);
        }
    }

    private void TryLogVlRenderPathDiagnostics(DateTimeOffset now)
    {
        if (_vlRenderPathDiagnosticsLogged)
        {
            return;
        }

        _vlRenderPathDiagnosticsLogged = true;
        try
        {
            (string Namespace, string Name)[] targets =
            {
                ("VL.Rendering", "VLBloom"),
                ("VL.Rendering", "VLDiffusion"),
                ("VL.Rendering", "VLTonemapping"),
                ("VL.Rendering", "VLPostProcessData"),
                ("VL.Rendering", "VLSRPAdditionalCameraData"),
                ("VL.Rendering", "VLSRPRendererData"),
                ("VL.Rendering", "VLSRPRenderer"),
                ("VL.Rendering.Internal", "VLPostProcessPasses"),
                ("VL.Rendering.Internal", "VLPostProcessPass")
            };
            List<string> descriptions = new();
            foreach ((string namespaze, string name) in targets)
            {
                IntPtr klass = FindClassAcrossImages(namespaze, name);
                descriptions.Add(DescribeClassHierarchy(klass));
            }

            IntPtr passClass = FindClassAcrossImages(
                "VL.Rendering.Internal",
                "VLPostProcessPass");
            descriptions.Add(DescribeNamedMethods(passClass, "DrawFlare"));
            descriptions.Add(DescribeNamedMethods(passClass, "Render"));
            string[] selectiveVlMethods =
            {
                "SetupVLDiffusion",
                "DoVLAdditive",
                "SetupVLBloom",
                "DrawStarStreak",
                "SetupVLParaffin",
                "DoVLTextureBlur",
                "DoVLVirtualEffectAdditiveTextureToAdd",
                "SetupVLVirtualEffect",
                "DoVLVirtualEffectAdditiveTextureToBloom"
            };
            foreach (string methodName in selectiveVlMethods)
            {
                descriptions.Add(DescribeNamedMethods(passClass, methodName));
            }
            descriptions.Add(DescribeNamedMethods(
                FindClassAcrossImages("VL.Rendering", "VLSRPRenderer"),
                "get_postProcessPass"));
            descriptions.Add(DescribeNamedMethods(
                FindClass(FindImage("UnityEngine.CoreModule.dll"), "UnityEngine", "Camera"),
                "get_current"));

            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = now,
                Event = "vl-render-path-diagnostics",
                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                ProcessId = Environment.ProcessId,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                StereoVisualEffectMode = _stereoVisualEffectMode,
                Reason = string.Join(" || ", descriptions)
            });
        }
        catch (Exception exception)
        {
            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = now,
                Event = "vl-render-path-diagnostics-failure",
                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                ProcessId = Environment.ProcessId,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                ErrorType = exception.GetType().FullName,
                Error = exception.Message,
                StereoVisualEffectMode = _stereoVisualEffectMode
            });
        }
    }

    private string DescribeClassHierarchy(IntPtr klass)
    {
        List<string> hierarchy = new();
        IntPtr current = klass;
        for (int depth = 0; depth < 5 && current != IntPtr.Zero; depth++)
        {
            string namespaze = GetNativeName(current, _api.ClassGetNamespace);
            string name = GetNativeName(current, _api.ClassGetName);
            List<string> fields = new();
            IntPtr fieldIterator = IntPtr.Zero;
            for (int index = 0; index < 256; index++)
            {
                IntPtr field = _api.ClassGetFields(current, ref fieldIterator);
                if (field == IntPtr.Zero)
                {
                    break;
                }

                fields.Add(GetNativeName(field, _api.FieldGetName));
            }

            List<string> methods = new();
            IntPtr methodIterator = IntPtr.Zero;
            for (int index = 0; index < 512; index++)
            {
                IntPtr method = _api.ClassGetMethods(current, ref methodIterator);
                if (method == IntPtr.Zero)
                {
                    break;
                }

                methods.Add(
                    GetNativeName(method, _api.MethodGetName) +
                    "/" +
                    _api.MethodGetParamCount(method));
            }

            hierarchy.Add(
                $"{namespaze}.{name}[fields={string.Join(',', fields)};methods={string.Join(',', methods)}]");
            current = _api.ClassGetParent(current);
        }

        return string.Join(" <- ", hierarchy);
    }

    private string DescribeNamedMethods(IntPtr klass, string wantedName)
    {
        List<string> descriptions = new();
        IntPtr current = klass;
        for (int depth = 0; depth < 5 && current != IntPtr.Zero; depth++)
        {
            IntPtr iterator = IntPtr.Zero;
            for (int index = 0; index < 512; index++)
            {
                IntPtr method = _api.ClassGetMethods(current, ref iterator);
                if (method == IntPtr.Zero)
                {
                    break;
                }

                if (!GetNativeName(method, _api.MethodGetName).Equals(
                        wantedName,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                uint parameterCount = _api.MethodGetParamCount(method);
                List<string> parameterTypes = new(checked((int)parameterCount));
                for (uint parameterIndex = 0;
                    parameterIndex < parameterCount;
                    parameterIndex++)
                {
                    parameterTypes.Add(GetTypeName(
                        _api.MethodGetParam(method, parameterIndex)));
                }

                IntPtr nativeMethodPointer = Marshal.ReadIntPtr(method, 0);
                descriptions.Add(
                    $"{GetNativeName(current, _api.ClassGetNamespace)}." +
                    $"{GetNativeName(current, _api.ClassGetName)}.{wantedName}" +
                    $"({string.Join(',', parameterTypes)})->" +
                    $"{GetTypeName(_api.MethodGetReturnType(method))}" +
                    $"@0x{nativeMethodPointer.ToInt64():x}");
            }

            current = _api.ClassGetParent(current);
        }

        return descriptions.Count == 0
            ? $"method-not-found:{wantedName}"
            : string.Join(" | ", descriptions);
    }

    private void TryApplyStereoVisualEffectOverride(
        DateTimeOffset now,
        IntPtr camera,
        IntPtr additionalCameraData)
    {
        if (!_stereoVisualEffectOverrideConfigured ||
            _stereoVisualEffectOverrideFailed ||
            _stereoVisualEffectMode is "all-off" or "all-on" ||
            UsesVlPostProcessHooks(_stereoVisualEffectMode))
        {
            return;
        }

        try
        {
            _ = InvokeWithObjectArgument(
                _stereoUpdateVolumeStack,
                IntPtr.Zero,
                camera);
            IntPtr volumeStack = Invoke(_stereoGetVolumeStack, additionalCameraData);
            if (volumeStack == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "The clone camera VolumeStack was null after a scripted update.");
            }

            int disabledComponentCount = 0;
            foreach (IntPtr managedType in _stereoVisualEffectManagedTypes)
            {
                IntPtr effect = InvokeWithObjectArgument(
                    _stereoVolumeStackGetComponent,
                    volumeStack,
                    managedType);
                if (effect == IntPtr.Zero)
                {
                    continue;
                }

                SetBooleanField(
                    effect,
                    _stereoVolumeComponentActiveField,
                    false);
                disabledComponentCount++;
            }

            if (disabledComponentCount == 0)
            {
                throw new InvalidOperationException(
                    $"The clone VolumeStack has no {_stereoVisualEffectMode} component.");
            }
            if (!_stereoVisualEffectOverrideApplied)
            {
                _stereoVisualEffectOverrideApplied = true;
                RuntimeProbe.Append(_logPath, new ProbeEvent
                {
                    TimestampUtc = now,
                    Event = "stereo-visual-effect-override-ready",
                    BootstrapVersion = RuntimeProbe.BootstrapVersion,
                    ProcessId = Environment.ProcessId,
                    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                    StereoClonePostProcessing = true,
                    StereoVisualEffectMode = _stereoVisualEffectMode,
                    StereoVisualEffectOverrideConfigured = true,
                    StereoVisualEffectOverrideApplied = true,
                    StereoVisualEffectFallback = false,
                    Reason = "The clone-owned scripted VolumeStack was updated and the selected effect was disabled without modifying the source Volume profile."
                });
            }
        }
        catch (Exception exception)
        {
            FallBackToStereoPostProcessingOff(now, exception);
        }
    }

    private void InstallDrawFlareHook()
    {
        if (_drawFlareHookInstalled)
        {
            return;
        }

        IntPtr passClass = FindClassAcrossImages(
            "VL.Rendering.Internal",
            "VLPostProcessPass");
        IntPtr vlBloomClass = FindClassAcrossImages("VL.Rendering", "VLBloom");
        _vlBloomIntensityField = FindFieldInHierarchy(vlBloomClass, "intensity");
        _vlBloomThresholdField = FindFieldInHierarchy(vlBloomClass, "threshold");
        _vlBloomDiffusionField = FindFieldInHierarchy(vlBloomClass, "diffusion");
        IntPtr minFloatParameterClass = FindClass(
            FindImage("Unity.RenderPipelines.Core.Runtime.dll"),
            "UnityEngine.Rendering",
            "MinFloatParameter");
        _volumeFloatValueField = FindFieldInHierarchy(
            minFloatParameterClass,
            "m_Value");
        IntPtr intParameterClass = FindClass(
            FindImage("Unity.RenderPipelines.Core.Runtime.dll"),
            "UnityEngine.Rendering",
            "IntParameter");
        _volumeIntValueField = FindFieldInHierarchy(
            intParameterClass,
            "m_Value");
        IntPtr universalImage = FindImage("Unity.RenderPipelines.Universal.Runtime.dll");
        IntPtr renderingDataClass = FindClass(
            universalImage,
            "UnityEngine.Rendering.Universal",
            "RenderingData");
        IntPtr cameraDataClass = FindClass(
            universalImage,
            "UnityEngine.Rendering.Universal",
            "CameraData");
        _renderingDataCameraDataOffset = _api.FieldGetOffset(
            FindField(renderingDataClass, "cameraData"));
        _cameraDataCameraOffset = _api.FieldGetOffset(
            FindField(cameraDataClass, "camera"));
        if (_renderingDataCameraDataOffset < 0 ||
            _renderingDataCameraDataOffset > 4_096 ||
            _cameraDataCameraOffset < 0 ||
            _cameraDataCameraOffset > 4_096)
        {
            throw new InvalidOperationException(
                $"Unexpected RenderingData camera offsets: " +
                $"cameraData={_renderingDataCameraDataOffset}, " +
                $"camera={_cameraDataCameraOffset}.");
        }

        IntPtr renderMethod = FindMethod(passClass, "Render", 2);
        IntPtr renderTarget = Marshal.ReadIntPtr(renderMethod, 0);
        if (renderTarget == IntPtr.Zero)
        {
            throw new MissingMethodException(
                "VLPostProcessPass.Render has no native method pointer.");
        }

        IntPtr hookExport = NativeLibrary.GetExport(_dobbyLibrary, "DobbyHook");
        DobbyHookDelegate hook = Marshal.GetDelegateForFunctionPointer<DobbyHookDelegate>(
            hookExport);
        IntPtr renderReplacement = Marshal.GetFunctionPointerForDelegate(
            _vlPostProcessRenderReplacement);
        int renderResult = hook(renderTarget, renderReplacement, out IntPtr renderOriginal);
        if (renderResult != 0 || renderOriginal == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"DobbyHook failed for VLPostProcessPass.Render: " +
                $"result={renderResult}, original={renderOriginal}.");
        }
        _vlPostProcessRenderOriginal =
            Marshal.GetDelegateForFunctionPointer<VlPostProcessRenderDelegate>(
                renderOriginal);

        IntPtr basePassClass = FindClass(
            universalImage,
            "UnityEngine.Rendering.Universal",
            "PostProcessPass");
        IntPtr baseRenderMethod = FindMethod(basePassClass, "Render", 2);
        IntPtr baseRenderTarget = Marshal.ReadIntPtr(baseRenderMethod, 0);
        if (baseRenderTarget == IntPtr.Zero)
        {
            throw new MissingMethodException(
                "Universal PostProcessPass.Render has no native method pointer.");
        }
        _basePostProcessRender =
            Marshal.GetDelegateForFunctionPointer<VlPostProcessRenderDelegate>(
                baseRenderTarget);

        IntPtr drawFlareMethod = FindMethod(passClass, "DrawFlare", 5);
        IntPtr drawFlareTarget = Marshal.ReadIntPtr(drawFlareMethod, 0);
        if (drawFlareTarget == IntPtr.Zero)
        {
            throw new MissingMethodException(
                "VLPostProcessPass.DrawFlare has no native method pointer.");
        }

        IntPtr replacement = Marshal.GetFunctionPointerForDelegate(_drawFlareReplacement);
        int result = hook(drawFlareTarget, replacement, out IntPtr original);
        if (result != 0 || original == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"DobbyHook failed for VLPostProcessPass.DrawFlare: result={result}, original={original}.");
        }

        _drawFlareOriginal = Marshal.GetDelegateForFunctionPointer<DrawFlareDelegate>(original);

        IntPtr setupVlBloomMethod = FindMethod(passClass, "SetupVLBloom", 4);
        IntPtr setupVlBloomTarget = Marshal.ReadIntPtr(setupVlBloomMethod, 0);
        if (setupVlBloomTarget == IntPtr.Zero)
        {
            throw new MissingMethodException(
                "VLPostProcessPass.SetupVLBloom has no native method pointer.");
        }
        IntPtr setupVlBloomReplacement = Marshal.GetFunctionPointerForDelegate(
            _setupVlBloomReplacement);
        int setupVlBloomResult = hook(
            setupVlBloomTarget,
            setupVlBloomReplacement,
            out IntPtr setupVlBloomOriginal);
        if (setupVlBloomResult != 0 || setupVlBloomOriginal == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"DobbyHook failed for VLPostProcessPass.SetupVLBloom: " +
                $"result={setupVlBloomResult}, original={setupVlBloomOriginal}.");
        }
        _setupVlBloomOriginal =
            Marshal.GetDelegateForFunctionPointer<SetupVlBloomDelegate>(
                setupVlBloomOriginal);

        IntPtr drawStarStreakMethod = FindMethod(passClass, "DrawStarStreak", 4);
        IntPtr drawStarStreakTarget = Marshal.ReadIntPtr(drawStarStreakMethod, 0);
        if (drawStarStreakTarget == IntPtr.Zero)
        {
            throw new MissingMethodException(
                "VLPostProcessPass.DrawStarStreak/4 has no native method pointer.");
        }
        IntPtr drawStarStreakReplacement = Marshal.GetFunctionPointerForDelegate(
            _drawStarStreakReplacement);
        int drawStarStreakResult = hook(
            drawStarStreakTarget,
            drawStarStreakReplacement,
            out IntPtr drawStarStreakOriginal);
        if (drawStarStreakResult != 0 || drawStarStreakOriginal == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"DobbyHook failed for VLPostProcessPass.DrawStarStreak/4: " +
                $"result={drawStarStreakResult}, original={drawStarStreakOriginal}.");
        }
        _drawStarStreakOriginal =
            Marshal.GetDelegateForFunctionPointer<DrawStarStreakDelegate>(
                drawStarStreakOriginal);

        IntPtr doVlDofMethod = FindMethod(passClass, "DoVLDOF", 4);
        string doVlDofReturnType = GetTypeName(_api.MethodGetReturnType(doVlDofMethod));
        if (!doVlDofReturnType.Equals("System.Boolean", StringComparison.Ordinal))
        {
            throw new MissingMethodException(
                $"VLPostProcessPass.DoVLDOF/4 returned {doVlDofReturnType}, not System.Boolean.");
        }
        IntPtr doVlDofTarget = Marshal.ReadIntPtr(doVlDofMethod, 0);
        if (doVlDofTarget == IntPtr.Zero)
        {
            throw new MissingMethodException(
                "VLPostProcessPass.DoVLDOF/4 has no native method pointer.");
        }
        IntPtr doVlDofReplacement = Marshal.GetFunctionPointerForDelegate(
            _doVlDofReplacement);
        int doVlDofResult = hook(
            doVlDofTarget,
            doVlDofReplacement,
            out IntPtr doVlDofOriginal);
        if (doVlDofResult != 0 || doVlDofOriginal == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"DobbyHook failed for VLPostProcessPass.DoVLDOF/4: " +
                $"result={doVlDofResult}, original={doVlDofOriginal}.");
        }
        _doVlDofOriginal = Marshal.GetDelegateForFunctionPointer<DoVlDofDelegate>(
            doVlDofOriginal);

        IntPtr doVlTextureBlurMethod = FindMethod(passClass, "DoVLTextureBlur", 3);
        string doVlTextureBlurReturnType = GetTypeName(
            _api.MethodGetReturnType(doVlTextureBlurMethod));
        if (!doVlTextureBlurReturnType.Equals("System.Boolean", StringComparison.Ordinal))
        {
            throw new MissingMethodException(
                $"VLPostProcessPass.DoVLTextureBlur/3 returned " +
                $"{doVlTextureBlurReturnType}, not System.Boolean.");
        }
        IntPtr doVlTextureBlurTarget = Marshal.ReadIntPtr(doVlTextureBlurMethod, 0);
        if (doVlTextureBlurTarget == IntPtr.Zero)
        {
            throw new MissingMethodException(
                "VLPostProcessPass.DoVLTextureBlur/3 has no native method pointer.");
        }
        IntPtr doVlTextureBlurReplacement = Marshal.GetFunctionPointerForDelegate(
            _doVlTextureBlurReplacement);
        int doVlTextureBlurResult = hook(
            doVlTextureBlurTarget,
            doVlTextureBlurReplacement,
            out IntPtr doVlTextureBlurOriginal);
        if (doVlTextureBlurResult != 0 || doVlTextureBlurOriginal == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"DobbyHook failed for VLPostProcessPass.DoVLTextureBlur/3: " +
                $"result={doVlTextureBlurResult}, original={doVlTextureBlurOriginal}.");
        }
        _doVlTextureBlurOriginal =
            Marshal.GetDelegateForFunctionPointer<DoVlTextureBlurDelegate>(
                doVlTextureBlurOriginal);
        _drawFlareHookInstalled = true;
    }

    private static bool UsesVlPostProcessHooks(string mode) => mode is
        "vlflare-off" or
        "vl-custom-pass-off" or
        "vl-setup-bloom-off" or
        "vl-star-streak-method-off" or
        "vl-bloom-half" or
        "vl-bloom-half-vldof-off" or
        "vl-bloom-half-vldof-textureblur-off" or
        "vl-bloom-threshold-vldof-textureblur-off" or
        "vl-bloom-diffusion-half-vldof-textureblur-off" or
        "vl-bloom-140-diffusion-min-vldof-textureblur-off" or
        VrVisualEffectModes.Manual;

    private byte OnDrawFlare(
        IntPtr instance,
        IntPtr commandBuffer,
        IntPtr material,
        IntPtr flareSetting,
        float width,
        float height,
        IntPtr methodInfo)
    {
        Interlocked.Increment(ref _drawFlareCallCount);
        if (Volatile.Read(ref _insideCloneVlPostProcess) != 0 &&
            (_stereoVisualEffectMode.Equals("vlflare-off", StringComparison.Ordinal) ||
             (_stereoVisualEffectMode.Equals(
                  VrVisualEffectModes.Manual,
                  StringComparison.Ordinal) &&
              !_manualVisualEffects.VlFlareEnabled)))
        {
            Interlocked.Increment(ref _drawFlareCloneSkipCount);
            return 0;
        }

        DrawFlareDelegate? original = _drawFlareOriginal;
        return original is null
            ? (byte)0
            : original(
                instance,
                commandBuffer,
                material,
                flareSetting,
                width,
                height,
                methodInfo);
    }

    private void OnSetupVlBloom(
        IntPtr instance,
        IntPtr commandBuffer,
        IntPtr source,
        IntPtr bloom,
        IntPtr starStreak,
        IntPtr methodInfo)
    {
        Interlocked.Increment(ref _setupVlBloomCallCount);
        if (Volatile.Read(ref _insideCloneVlPostProcess) != 0 &&
            _stereoVisualEffectMode.Equals(
                VrVisualEffectModes.Manual,
                StringComparison.Ordinal))
        {
            if (!_manualVisualEffects.VlBloomEnabled)
            {
                Interlocked.Increment(ref _setupVlBloomCloneSkipCount);
                return;
            }

            InvokeSetupVlBloomWithScaledIntensityAndDiffusion(
                instance,
                commandBuffer,
                source,
                bloom,
                starStreak,
                methodInfo,
                _manualVisualEffects.VlBloomIntensityScale,
                1f,
                _manualVisualEffects.VlBloomDiffusion);
            return;
        }

        if (Volatile.Read(ref _insideCloneVlPostProcess) != 0 &&
            _stereoVisualEffectMode.Equals(
                "vl-setup-bloom-off",
                StringComparison.Ordinal))
        {
            Interlocked.Increment(ref _setupVlBloomCloneSkipCount);
            return;
        }

        if (Volatile.Read(ref _insideCloneVlPostProcess) != 0 &&
            _stereoVisualEffectMode.Equals(
                "vl-bloom-140-diffusion-min-vldof-textureblur-off",
                StringComparison.Ordinal))
        {
            InvokeSetupVlBloomWithScaledIntensityAndDiffusion(
                instance,
                commandBuffer,
                source,
                bloom,
                starStreak,
                methodInfo,
                1.4f,
                0.05f);
            return;
        }

        if (Volatile.Read(ref _insideCloneVlPostProcess) != 0 &&
            _stereoVisualEffectMode.Equals(
                "vl-bloom-diffusion-half-vldof-textureblur-off",
                StringComparison.Ordinal))
        {
            InvokeSetupVlBloomWithScaledDiffusion(
                instance,
                commandBuffer,
                source,
                bloom,
                starStreak,
                methodInfo,
                0.5f);
            return;
        }

        if (Volatile.Read(ref _insideCloneVlPostProcess) != 0 &&
            _stereoVisualEffectMode.Equals(
                "vl-bloom-threshold-vldof-textureblur-off",
                StringComparison.Ordinal))
        {
            InvokeSetupVlBloomWithRaisedThreshold(
                instance,
                commandBuffer,
                source,
                bloom,
                starStreak,
                methodInfo,
                0.5f);
            return;
        }

        if (Volatile.Read(ref _insideCloneVlPostProcess) != 0 &&
            (_stereoVisualEffectMode.Equals("vl-bloom-half", StringComparison.Ordinal) ||
             _stereoVisualEffectMode.Equals(
                 "vl-bloom-half-vldof-off",
                 StringComparison.Ordinal) ||
             _stereoVisualEffectMode.Equals(
                 "vl-bloom-half-vldof-textureblur-off",
                 StringComparison.Ordinal)))
        {
            InvokeSetupVlBloomWithScaledIntensity(
                instance,
                commandBuffer,
                source,
                bloom,
                starStreak,
                methodInfo,
                0.5f);
            return;
        }

        _setupVlBloomOriginal?.Invoke(
            instance,
            commandBuffer,
            source,
            bloom,
            starStreak,
            methodInfo);
    }

    private void InvokeSetupVlBloomWithScaledIntensity(
        IntPtr instance,
        IntPtr commandBuffer,
        IntPtr source,
        IntPtr bloom,
        IntPtr starStreak,
        IntPtr methodInfo,
        float scale)
    {
        IntPtr parameterStorage = Marshal.AllocHGlobal(IntPtr.Size);
        IntPtr valueStorage = Marshal.AllocHGlobal(sizeof(float));
        bool changed = false;
        float originalValue = 0.0f;
        try
        {
            _api.FieldGetValue(bloom, _vlBloomIntensityField, parameterStorage);
            IntPtr parameter = Marshal.ReadIntPtr(parameterStorage);
            if (parameter == IntPtr.Zero)
            {
                throw new InvalidOperationException("VLBloom.intensity was null.");
            }

            _api.FieldGetValue(parameter, _volumeFloatValueField, valueStorage);
            originalValue = Marshal.PtrToStructure<float>(valueStorage);
            Marshal.StructureToPtr(originalValue * scale, valueStorage, false);
            _api.FieldSetValue(parameter, _volumeFloatValueField, valueStorage);
            changed = true;
            Interlocked.Increment(ref _vlBloomIntensityScaleCount);
            _setupVlBloomOriginal?.Invoke(
                instance,
                commandBuffer,
                source,
                bloom,
                starStreak,
                methodInfo);
        }
        catch
        {
            Interlocked.Increment(ref _vlBloomIntensityScaleFailureCount);
            if (!changed)
            {
                _setupVlBloomOriginal?.Invoke(
                    instance,
                    commandBuffer,
                    source,
                    bloom,
                    starStreak,
                    methodInfo);
            }
        }
        finally
        {
            if (changed)
            {
                Marshal.StructureToPtr(originalValue, valueStorage, false);
                _api.FieldSetValue(
                    Marshal.ReadIntPtr(parameterStorage),
                    _volumeFloatValueField,
                    valueStorage);
            }
            Marshal.FreeHGlobal(valueStorage);
            Marshal.FreeHGlobal(parameterStorage);
        }
    }

    private void InvokeSetupVlBloomWithRaisedThreshold(
        IntPtr instance,
        IntPtr commandBuffer,
        IntPtr source,
        IntPtr bloom,
        IntPtr starStreak,
        IntPtr methodInfo,
        float offset)
    {
        IntPtr parameterStorage = Marshal.AllocHGlobal(IntPtr.Size);
        IntPtr valueStorage = Marshal.AllocHGlobal(sizeof(float));
        IntPtr parameter = IntPtr.Zero;
        bool changed = false;
        float originalValue = 0.0f;
        try
        {
            _api.FieldGetValue(bloom, _vlBloomThresholdField, parameterStorage);
            parameter = Marshal.ReadIntPtr(parameterStorage);
            if (parameter == IntPtr.Zero)
            {
                throw new InvalidOperationException("VLBloom.threshold was null.");
            }

            _api.FieldGetValue(parameter, _volumeFloatValueField, valueStorage);
            originalValue = Marshal.PtrToStructure<float>(valueStorage);
            float adjustedValue = originalValue + offset;
            Marshal.StructureToPtr(adjustedValue, valueStorage, false);
            _api.FieldSetValue(parameter, _volumeFloatValueField, valueStorage);
            changed = true;
            _lastVlBloomOriginalThreshold = originalValue;
            _lastVlBloomAdjustedThreshold = adjustedValue;
            Interlocked.Increment(ref _vlBloomThresholdRaiseCount);
            _setupVlBloomOriginal?.Invoke(
                instance,
                commandBuffer,
                source,
                bloom,
                starStreak,
                methodInfo);
        }
        catch
        {
            Interlocked.Increment(ref _vlBloomThresholdRaiseFailureCount);
            if (!changed)
            {
                _setupVlBloomOriginal?.Invoke(
                    instance,
                    commandBuffer,
                    source,
                    bloom,
                    starStreak,
                    methodInfo);
            }
        }
        finally
        {
            if (changed)
            {
                Marshal.StructureToPtr(originalValue, valueStorage, false);
                _api.FieldSetValue(parameter, _volumeFloatValueField, valueStorage);
            }
            Marshal.FreeHGlobal(valueStorage);
            Marshal.FreeHGlobal(parameterStorage);
        }
    }

    private void InvokeSetupVlBloomWithScaledDiffusion(
        IntPtr instance,
        IntPtr commandBuffer,
        IntPtr source,
        IntPtr bloom,
        IntPtr starStreak,
        IntPtr methodInfo,
        float scale)
    {
        IntPtr parameterStorage = Marshal.AllocHGlobal(IntPtr.Size);
        IntPtr valueStorage = Marshal.AllocHGlobal(sizeof(int));
        IntPtr parameter = IntPtr.Zero;
        bool changed = false;
        int originalValue = 0;
        try
        {
            _api.FieldGetValue(bloom, _vlBloomDiffusionField, parameterStorage);
            parameter = Marshal.ReadIntPtr(parameterStorage);
            if (parameter == IntPtr.Zero)
            {
                throw new InvalidOperationException("VLBloom.diffusion was null.");
            }

            _api.FieldGetValue(parameter, _volumeIntValueField, valueStorage);
            originalValue = Marshal.ReadInt32(valueStorage);
            int adjustedValue = Math.Max(
                1,
                (int)MathF.Round(originalValue * scale));
            Marshal.StructureToPtr(adjustedValue, valueStorage, false);
            _api.FieldSetValue(parameter, _volumeIntValueField, valueStorage);
            changed = true;
            _lastVlBloomOriginalDiffusion = originalValue;
            _lastVlBloomAdjustedDiffusion = adjustedValue;
            Interlocked.Increment(ref _vlBloomDiffusionScaleCount);
            _setupVlBloomOriginal?.Invoke(
                instance,
                commandBuffer,
                source,
                bloom,
                starStreak,
                methodInfo);
        }
        catch
        {
            Interlocked.Increment(ref _vlBloomDiffusionScaleFailureCount);
            if (!changed)
            {
                _setupVlBloomOriginal?.Invoke(
                    instance,
                    commandBuffer,
                    source,
                    bloom,
                    starStreak,
                    methodInfo);
            }
        }
        finally
        {
            if (changed)
            {
                Marshal.StructureToPtr(originalValue, valueStorage, false);
                _api.FieldSetValue(parameter, _volumeIntValueField, valueStorage);
            }
            Marshal.FreeHGlobal(valueStorage);
            Marshal.FreeHGlobal(parameterStorage);
        }
    }

    private void InvokeSetupVlBloomWithScaledIntensityAndDiffusion(
        IntPtr instance,
        IntPtr commandBuffer,
        IntPtr source,
        IntPtr bloom,
        IntPtr starStreak,
        IntPtr methodInfo,
        float intensityScale,
        float diffusionScale,
        int? diffusionOverride = null)
    {
        IntPtr parameterStorage = Marshal.AllocHGlobal(IntPtr.Size);
        IntPtr valueStorage = Marshal.AllocHGlobal(sizeof(float));
        IntPtr intensityParameter = IntPtr.Zero;
        IntPtr diffusionParameter = IntPtr.Zero;
        bool intensityChanged = false;
        bool diffusionChanged = false;
        float originalIntensity = 0.0f;
        int originalDiffusion = 0;
        try
        {
            _api.FieldGetValue(bloom, _vlBloomIntensityField, parameterStorage);
            intensityParameter = Marshal.ReadIntPtr(parameterStorage);
            if (intensityParameter == IntPtr.Zero)
            {
                throw new InvalidOperationException("VLBloom.intensity was null.");
            }
            _api.FieldGetValue(intensityParameter, _volumeFloatValueField, valueStorage);
            originalIntensity = Marshal.PtrToStructure<float>(valueStorage);
            float adjustedIntensity = originalIntensity * intensityScale;
            Marshal.StructureToPtr(adjustedIntensity, valueStorage, false);
            _api.FieldSetValue(intensityParameter, _volumeFloatValueField, valueStorage);
            intensityChanged = true;

            _api.FieldGetValue(bloom, _vlBloomDiffusionField, parameterStorage);
            diffusionParameter = Marshal.ReadIntPtr(parameterStorage);
            if (diffusionParameter == IntPtr.Zero)
            {
                throw new InvalidOperationException("VLBloom.diffusion was null.");
            }
            _api.FieldGetValue(diffusionParameter, _volumeIntValueField, valueStorage);
            originalDiffusion = Marshal.ReadInt32(valueStorage);
            int adjustedDiffusion = diffusionOverride ?? Math.Max(
                1,
                (int)MathF.Round(originalDiffusion * diffusionScale));
            Marshal.StructureToPtr(adjustedDiffusion, valueStorage, false);
            _api.FieldSetValue(diffusionParameter, _volumeIntValueField, valueStorage);
            diffusionChanged = true;

            _lastVlBloomOriginalIntensity = originalIntensity;
            _lastVlBloomAdjustedIntensity = adjustedIntensity;
            _lastVlBloomOriginalDiffusion = originalDiffusion;
            _lastVlBloomAdjustedDiffusion = adjustedDiffusion;
            Interlocked.Increment(ref _vlBloomCombinedScaleCount);
            _setupVlBloomOriginal?.Invoke(
                instance,
                commandBuffer,
                source,
                bloom,
                starStreak,
                methodInfo);
        }
        catch
        {
            Interlocked.Increment(ref _vlBloomCombinedScaleFailureCount);
            if (!intensityChanged && !diffusionChanged)
            {
                _setupVlBloomOriginal?.Invoke(
                    instance,
                    commandBuffer,
                    source,
                    bloom,
                    starStreak,
                    methodInfo);
            }
        }
        finally
        {
            if (diffusionChanged)
            {
                Marshal.StructureToPtr(originalDiffusion, valueStorage, false);
                _api.FieldSetValue(
                    diffusionParameter,
                    _volumeIntValueField,
                    valueStorage);
            }
            if (intensityChanged)
            {
                Marshal.StructureToPtr(originalIntensity, valueStorage, false);
                _api.FieldSetValue(
                    intensityParameter,
                    _volumeFloatValueField,
                    valueStorage);
            }
            Marshal.FreeHGlobal(valueStorage);
            Marshal.FreeHGlobal(parameterStorage);
        }
    }

    private void OnDrawStarStreak(
        IntPtr instance,
        IntPtr commandBuffer,
        IntPtr source,
        IntPtr destination,
        IntPtr starStreak,
        IntPtr methodInfo)
    {
        Interlocked.Increment(ref _drawStarStreakCallCount);
        if (Volatile.Read(ref _insideCloneVlPostProcess) != 0 &&
            (_stereoVisualEffectMode.Equals(
                 "vl-star-streak-method-off",
                 StringComparison.Ordinal) ||
             (_stereoVisualEffectMode.Equals(
                  VrVisualEffectModes.Manual,
                  StringComparison.Ordinal) &&
              !_manualVisualEffects.VlStarStreakEnabled)))
        {
            Interlocked.Increment(ref _drawStarStreakCloneSkipCount);
            return;
        }

        _drawStarStreakOriginal?.Invoke(
            instance,
            commandBuffer,
            source,
            destination,
            starStreak,
            methodInfo);
    }

    private byte OnDoVlDof(
        IntPtr instance,
        IntPtr commandBuffer,
        IntPtr source,
        IntPtr destination,
        IntPtr camera,
        IntPtr methodInfo)
    {
        Interlocked.Increment(ref _doVlDofCallCount);
        if (Volatile.Read(ref _insideCloneVlPostProcess) != 0 &&
            (_stereoVisualEffectMode.Equals(
                 "vl-bloom-half-vldof-off",
                 StringComparison.Ordinal) ||
             _stereoVisualEffectMode.Equals(
                 "vl-bloom-half-vldof-textureblur-off",
                 StringComparison.Ordinal) ||
             _stereoVisualEffectMode.Equals(
                 "vl-bloom-threshold-vldof-textureblur-off",
                 StringComparison.Ordinal) ||
             _stereoVisualEffectMode.Equals(
                 "vl-bloom-diffusion-half-vldof-textureblur-off",
                 StringComparison.Ordinal) ||
             _stereoVisualEffectMode.Equals(
                 "vl-bloom-140-diffusion-min-vldof-textureblur-off",
                 StringComparison.Ordinal) ||
             (_stereoVisualEffectMode.Equals(
                  VrVisualEffectModes.Manual,
                  StringComparison.Ordinal) &&
              !_manualVisualEffects.VlDepthOfFieldEnabled)))
        {
            Interlocked.Increment(ref _doVlDofCloneSkipCount);
            return 0;
        }

        DoVlDofDelegate? original = _doVlDofOriginal;
        return original is null
            ? (byte)0
            : original(
                instance,
                commandBuffer,
                source,
                destination,
                camera,
                methodInfo);
    }

    private byte OnDoVlTextureBlur(
        IntPtr instance,
        IntPtr commandBuffer,
        IntPtr source,
        IntPtr destination,
        IntPtr methodInfo)
    {
        Interlocked.Increment(ref _doVlTextureBlurCallCount);
        if (Volatile.Read(ref _insideCloneVlPostProcess) != 0 &&
            (_stereoVisualEffectMode.Equals(
                 "vl-bloom-half-vldof-textureblur-off",
                 StringComparison.Ordinal) ||
             _stereoVisualEffectMode.Equals(
                 "vl-bloom-threshold-vldof-textureblur-off",
                 StringComparison.Ordinal) ||
             _stereoVisualEffectMode.Equals(
                 "vl-bloom-diffusion-half-vldof-textureblur-off",
                 StringComparison.Ordinal) ||
             _stereoVisualEffectMode.Equals(
                 "vl-bloom-140-diffusion-min-vldof-textureblur-off",
                 StringComparison.Ordinal) ||
             (_stereoVisualEffectMode.Equals(
                  VrVisualEffectModes.Manual,
                  StringComparison.Ordinal) &&
              !_manualVisualEffects.VlTextureBlurEnabled)))
        {
            Interlocked.Increment(ref _doVlTextureBlurCloneSkipCount);
            return 0;
        }

        DoVlTextureBlurDelegate? original = _doVlTextureBlurOriginal;
        return original is null
            ? (byte)0
            : original(
                instance,
                commandBuffer,
                source,
                destination,
                methodInfo);
    }

    private void OnVlPostProcessRender(
        IntPtr instance,
        IntPtr commandBuffer,
        IntPtr renderingData,
        IntPtr methodInfo)
    {
        Interlocked.Increment(ref _vlPostProcessRenderCount);
        IntPtr camera = TryReadRenderingDataCamera(renderingData);
        bool isClone = camera != IntPtr.Zero &&
            (camera == _stereoLeftCamera || camera == _stereoRightCamera);
        if (isClone)
        {
            Interlocked.Increment(ref _vlPostProcessCloneRenderCount);
            StereoPerformanceTelemetry.RecordCloneVisualEffectRender(
                D3D11DeviceCapture.PresentSerial);
            Volatile.Write(ref _insideCloneVlPostProcess, 1);
        }
        else if (camera != IntPtr.Zero && camera == _lastLiveCamera)
        {
            Interlocked.Increment(ref _vlPostProcessSourceRenderCount);
            long presentSerial = D3D11DeviceCapture.PresentSerial;
            StereoPerformanceTelemetry.RecordSourceVisualEffectRender(presentSerial);
            Volatile.Write(
                ref _lastStereoSourceRenderPresentSerial,
                presentSerial);
        }
        else
        {
            Interlocked.Increment(ref _vlPostProcessUnmatchedRenderCount);
        }

        try
        {
            if (isClone && _stereoVisualEffectMode.Equals(
                    "vl-custom-pass-off",
                    StringComparison.Ordinal))
            {
                _basePostProcessRender?.Invoke(
                    instance,
                    commandBuffer,
                    renderingData,
                    IntPtr.Zero);
            }
            else
            {
                _vlPostProcessRenderOriginal?.Invoke(
                    instance,
                    commandBuffer,
                    renderingData,
                    methodInfo);
            }
        }
        finally
        {
            if (isClone)
            {
                if (_stereoNaturalRenderArmed)
                {
                    int completionBit = camera == _stereoLeftCamera ? 1 : 2;
                    int previousMask = Interlocked.Or(
                        ref _stereoNaturalRenderCompletionMask,
                        completionBit);
                    if (previousMask != 3 &&
                        (previousMask | completionBit) == 3)
                    {
                        StereoPerformanceTelemetry.RecordStereoPairRenderCompletion(
                            D3D11DeviceCapture.PresentSerial -
                                _stereoNaturalRenderStartPresentSerial);
                    }
                }
                Volatile.Write(ref _insideCloneVlPostProcess, 0);
            }
        }
    }

    private IntPtr TryReadRenderingDataCamera(IntPtr renderingData)
    {
        if (renderingData == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        int[] renderingOffsets = _renderingDataCameraDataOffset >= 2 * IntPtr.Size
            ? new[]
            {
                _renderingDataCameraDataOffset,
                _renderingDataCameraDataOffset - 2 * IntPtr.Size
            }
            : new[] { _renderingDataCameraDataOffset };
        int[] cameraOffsets = _cameraDataCameraOffset >= 2 * IntPtr.Size
            ? new[]
            {
                _cameraDataCameraOffset,
                _cameraDataCameraOffset - 2 * IntPtr.Size
            }
            : new[] { _cameraDataCameraOffset };
        foreach (int renderingOffset in renderingOffsets)
        {
            foreach (int cameraOffset in cameraOffsets)
            {
                IntPtr camera = Marshal.ReadIntPtr(
                    renderingData,
                    checked(renderingOffset + cameraOffset));
                if (camera == _stereoLeftCamera ||
                    camera == _stereoRightCamera ||
                    camera == _lastLiveCamera)
                {
                    return camera;
                }
            }
        }

        return IntPtr.Zero;
    }

    private void TryLogDrawFlareStats(DateTimeOffset now, long nowMilliseconds)
    {
        if (!_drawFlareHookInstalled ||
            nowMilliseconds < _nextDrawFlareStatsMilliseconds)
        {
            return;
        }

        _nextDrawFlareStatsMilliseconds = nowMilliseconds + 1_000;
        RuntimeProbe.Append(_logPath, new ProbeEvent
        {
            TimestampUtc = now,
            Event = "vl-draw-flare-stats",
            BootstrapVersion = RuntimeProbe.BootstrapVersion,
            ProcessId = Environment.ProcessId,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            StereoVisualEffectMode = _stereoVisualEffectMode,
            Reason =
                $"calls={Interlocked.Read(ref _drawFlareCallCount)};" +
                $"cloneSkips={Interlocked.Read(ref _drawFlareCloneSkipCount)};" +
                $"sourceCalls={Interlocked.Read(ref _drawFlareSourceCount)};" +
                $"nullCameraCalls={Interlocked.Read(ref _drawFlareNullCameraCount)};" +
                $"otherCameraCalls={Interlocked.Read(ref _drawFlareOtherCameraCount)};" +
                $"renderCalls={Interlocked.Read(ref _vlPostProcessRenderCount)};" +
                $"cloneRenders={Interlocked.Read(ref _vlPostProcessCloneRenderCount)};" +
                $"sourceRenders={Interlocked.Read(ref _vlPostProcessSourceRenderCount)};" +
                $"unmatchedRenders={Interlocked.Read(ref _vlPostProcessUnmatchedRenderCount)};" +
                $"renderingDataCameraDataOffset={_renderingDataCameraDataOffset};" +
                $"cameraDataCameraOffset={_cameraDataCameraOffset}"
                + $";setupVlBloomCalls={Interlocked.Read(ref _setupVlBloomCallCount)}"
                + $";setupVlBloomCloneSkips={Interlocked.Read(ref _setupVlBloomCloneSkipCount)}"
                + $";drawStarStreakCalls={Interlocked.Read(ref _drawStarStreakCallCount)}"
                + $";drawStarStreakCloneSkips={Interlocked.Read(ref _drawStarStreakCloneSkipCount)}"
                + $";vlBloomIntensityScales={Interlocked.Read(ref _vlBloomIntensityScaleCount)}"
                + $";vlBloomIntensityScaleFailures={Interlocked.Read(ref _vlBloomIntensityScaleFailureCount)}"
                + $";vlBloomThresholdRaises={Interlocked.Read(ref _vlBloomThresholdRaiseCount)}"
                + $";vlBloomThresholdRaiseFailures={Interlocked.Read(ref _vlBloomThresholdRaiseFailureCount)}"
                + $";vlBloomThresholdLast={_lastVlBloomOriginalThreshold:R}-to-{_lastVlBloomAdjustedThreshold:R}"
                + $";vlBloomDiffusionScales={Interlocked.Read(ref _vlBloomDiffusionScaleCount)}"
                + $";vlBloomDiffusionScaleFailures={Interlocked.Read(ref _vlBloomDiffusionScaleFailureCount)}"
                + $";vlBloomDiffusionLast={_lastVlBloomOriginalDiffusion:R}-to-{_lastVlBloomAdjustedDiffusion:R}"
                + $";vlBloomCombinedScales={Interlocked.Read(ref _vlBloomCombinedScaleCount)}"
                + $";vlBloomCombinedScaleFailures={Interlocked.Read(ref _vlBloomCombinedScaleFailureCount)}"
                + $";vlBloomIntensityLast={_lastVlBloomOriginalIntensity:R}-to-{_lastVlBloomAdjustedIntensity:R}"
                + $";doVlDofCalls={Interlocked.Read(ref _doVlDofCallCount)}"
                + $";doVlDofCloneSkips={Interlocked.Read(ref _doVlDofCloneSkipCount)}"
                + $";doVlTextureBlurCalls={Interlocked.Read(ref _doVlTextureBlurCallCount)}"
                + $";doVlTextureBlurCloneSkips={Interlocked.Read(ref _doVlTextureBlurCloneSkipCount)}"
        });
    }

    private void FallBackToStereoPostProcessingOff(
        DateTimeOffset now,
        Exception exception)
    {
        if (_stereoVisualEffectOverrideFailed)
        {
            return;
        }

        _stereoVisualEffectOverrideFailed = true;
        _stereoVisualEffectFallback = true;
        _stereoClonePostProcessing = false;
        SetStereoPostProcessing(_stereoLeftAdditionalCameraData, false);
        SetStereoPostProcessing(_stereoRightAdditionalCameraData, false);
        RuntimeProbe.Append(_logPath, new ProbeEvent
        {
            TimestampUtc = now,
            Event = "stereo-visual-effect-override-fallback",
            BootstrapVersion = RuntimeProbe.BootstrapVersion,
            ProcessId = Environment.ProcessId,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            StereoClonePostProcessing = false,
            StereoVisualEffectMode = _stereoVisualEffectMode,
            StereoVisualEffectOverrideConfigured =
                _stereoVisualEffectOverrideConfigured,
            StereoVisualEffectOverrideApplied = _stereoVisualEffectOverrideApplied,
            StereoVisualEffectFallback = true,
            ErrorType = exception.GetType().FullName,
            Error = exception.Message,
            Reason = "The clone-only effect override was unavailable; both clone cameras safely fell back to the accepted v0.104 all-post-processing-off behavior."
        });
    }

    private void SetStereoPostProcessing(IntPtr additionalCameraData, bool enabled)
    {
        if (additionalCameraData == IntPtr.Zero)
        {
            return;
        }

        IntPtr universalImage = FindImage("Unity.RenderPipelines.Universal.Runtime.dll");
        IntPtr additionalDataClass = FindClassBySimpleName(
            universalImage,
            "UniversalAdditionalCameraData");
        _ = InvokeWithObjectArgument(
            FindMethod(additionalDataClass, "set_renderPostProcessing", 1),
            additionalCameraData,
            BoxBoolean(FindImage("mscorlib.dll"), enabled));
    }

    private IntPtr TryFindField(IntPtr klass, string wantedName)
    {
        IntPtr iterator = IntPtr.Zero;
        for (int index = 0; index < 256; index++)
        {
            IntPtr field = _api.ClassGetFields(klass, ref iterator);
            if (field == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            if (GetNativeName(field, _api.FieldGetName).Equals(
                wantedName,
                StringComparison.Ordinal))
            {
                return field;
            }
        }

        return IntPtr.Zero;
    }

    private void SetBooleanField(IntPtr instance, IntPtr field, bool value)
    {
        IntPtr storage = Marshal.AllocHGlobal(sizeof(byte));
        try
        {
            Marshal.WriteByte(storage, value ? (byte)1 : (byte)0);
            _api.FieldSetValue(instance, field, storage);
        }
        finally
        {
            Marshal.FreeHGlobal(storage);
        }
    }

    private (
        IReadOnlyList<CanvasProbeRecord> Canvases,
        IReadOnlyList<RawImageProbeRecord> RawImages,
        IReadOnlyList<UiGraphicProbeRecord> UiGraphics) CaptureUiHierarchy(
            IntPtr coreImage,
            string scene,
            int screenWidth,
            int screenHeight,
            bool orientationStable,
            bool includeDetailedGraphics)
    {
        IntPtr canvasImage = FindImage("UnityEngine.UIModule.dll");
        IntPtr uiImage = FindImage("UnityEngine.UI.dll");
        IReadOnlyList<IntPtr> canvasObjects = FindObjectsOfTypeAll(
            coreImage,
            canvasImage,
            "UnityEngine",
            "Canvas");
        IReadOnlyList<IntPtr> rawImageObjects = FindObjectsOfTypeAll(
            coreImage,
            uiImage,
            "UnityEngine.UI",
            "RawImage",
            2048);
        IReadOnlyList<IntPtr> graphicObjects = includeDetailedGraphics
            ? FindObjectsOfTypeAll(
                coreImage,
                uiImage,
                "UnityEngine.UI",
                "Graphic",
                4096)
            : Array.Empty<IntPtr>();

        List<CanvasProbeRecord> canvases = new();
        foreach (IntPtr canvas in canvasObjects.Take(128))
        {
            IntPtr gameObject = InvokeInstanceObject(
                coreImage,
                "UnityEngine",
                "Component",
                "get_gameObject",
                canvas);
            if (gameObject == IntPtr.Zero)
            {
                continue;
            }

            IntPtr worldCamera = InvokeInstanceObject(
                canvasImage,
                "UnityEngine",
                "Canvas",
                "get_worldCamera",
                canvas);
            canvases.Add(new CanvasProbeRecord
            {
                Name = InvokeInstanceString(
                    coreImage,
                    "UnityEngine",
                    "Object",
                    "get_name",
                    gameObject),
                Path = GetComponentPath(coreImage, canvas),
                Enabled = InvokeInstanceBool(
                    coreImage,
                    "UnityEngine",
                    "Behaviour",
                    "get_enabled",
                    canvas),
                ActiveInHierarchy = InvokeInstanceBool(
                    coreImage,
                    "UnityEngine",
                    "GameObject",
                    "get_activeInHierarchy",
                    gameObject),
                RenderMode = InvokeInstanceInt(
                    canvasImage,
                    "UnityEngine",
                    "Canvas",
                    "get_renderMode",
                    canvas),
                SortingOrder = InvokeInstanceInt(
                    canvasImage,
                    "UnityEngine",
                    "Canvas",
                    "get_sortingOrder",
                    canvas),
                OverrideSorting = InvokeInstanceBool(
                    canvasImage,
                    "UnityEngine",
                    "Canvas",
                    "get_overrideSorting",
                    canvas),
                WorldCameraName = worldCamera == IntPtr.Zero
                    ? null
                    : InvokeInstanceString(
                        coreImage,
                        "UnityEngine",
                        "Object",
                        "get_name",
                        worldCamera)
            });
        }

        List<RawImageProbeRecord> rawImages = new();
        IntPtr m6WorldSurface = IntPtr.Zero;
        string m6WorldSurfacePath = string.Empty;
        foreach (IntPtr rawImage in rawImageObjects)
        {
            IntPtr gameObject = InvokeInstanceObject(
                coreImage,
                "UnityEngine",
                "Component",
                "get_gameObject",
                rawImage);
            if (gameObject == IntPtr.Zero || !InvokeInstanceBool(
                coreImage,
                "UnityEngine",
                "GameObject",
                "get_activeInHierarchy",
                gameObject))
            {
                continue;
            }

            IntPtr texture = InvokeInstanceObject(
                uiImage,
                "UnityEngine.UI",
                "RawImage",
                "get_texture",
                rawImage);
            bool enabled = InvokeInstanceBool(
                coreImage,
                "UnityEngine",
                "Behaviour",
                "get_enabled",
                rawImage);
            string path = GetComponentPath(coreImage, rawImage);
            if (!IsLiveScene(scene) &&
                enabled &&
                texture != IntPtr.Zero &&
                texture == _m6WorldCandidateTargetTexture &&
                IsApprovedM6WorldSurfacePath(path))
            {
                m6WorldSurface = rawImage;
                m6WorldSurfacePath = path;
            }

            if (rawImages.Count >= 256)
            {
                continue;
            }

            rawImages.Add(new RawImageProbeRecord
            {
                Name = InvokeInstanceString(
                    coreImage,
                    "UnityEngine",
                    "Object",
                    "get_name",
                    gameObject),
                Path = path,
                Enabled = enabled,
                ActiveInHierarchy = true,
                RaycastTarget = InvokeInstanceBool(
                    uiImage,
                    "UnityEngine.UI",
                    "Graphic",
                    "get_raycastTarget",
                    rawImage),
                TextureName = texture == IntPtr.Zero
                    ? null
                    : InvokeInstanceString(
                        coreImage,
                        "UnityEngine",
                        "Object",
                        "get_name",
                        texture),
                TextureWidth = texture == IntPtr.Zero
                    ? null
                    : InvokeInstanceInt(
                        coreImage,
                        "UnityEngine",
                        "Texture",
                        "get_width",
                        texture),
                TextureHeight = texture == IntPtr.Zero
                    ? null
                    : InvokeInstanceInt(
                        coreImage,
                        "UnityEngine",
                        "Texture",
                        "get_height",
                        texture)
            });
        }

        UpdateM6NonLiveWorldBinding(
            coreImage,
            scene,
            screenWidth,
            screenHeight,
            orientationStable,
            m6WorldSurface,
            m6WorldSurfacePath);

        List<UiGraphicProbeRecord> graphics = new();
        IntPtr graphicClass = FindClass(uiImage, "UnityEngine.UI", "Graphic");
        IntPtr canvasRendererClass = FindClass(canvasImage, "UnityEngine", "CanvasRenderer");
        IntPtr getCanvasRenderer = FindMethod(graphicClass, "get_canvasRenderer");
        IntPtr getRaycastTarget = FindMethod(graphicClass, "get_raycastTarget");
        foreach (IntPtr graphic in graphicObjects)
        {
            try
            {
                IntPtr gameObject = InvokeInstanceObject(
                    coreImage,
                    "UnityEngine",
                    "Component",
                    "get_gameObject",
                    graphic);
                if (gameObject == IntPtr.Zero || !InvokeInstanceBool(
                        coreImage,
                        "UnityEngine",
                        "GameObject",
                        "get_activeInHierarchy",
                        gameObject))
                {
                    continue;
                }

                string path = GetComponentPath(coreImage, graphic);
                if (!IsLiveUiPath(path))
                {
                    continue;
                }

                IntPtr runtimeClass = Marshal.ReadIntPtr(graphic);
                IntPtr canvasRenderer = Invoke(getCanvasRenderer, graphic);
                IntPtr mainTexture = TryInvokeObject(runtimeClass, "get_mainTexture", graphic);
                int? materialCount = canvasRenderer == IntPtr.Zero
                    ? null
                    : TryInvokeInt(canvasRendererClass, "get_materialCount", canvasRenderer);
                IntPtr material = IntPtr.Zero;
                if (canvasRenderer != IntPtr.Zero && materialCount.GetValueOrDefault() > 0)
                {
                    IntPtr getMaterial = TryFindMethod(canvasRendererClass, "GetMaterial", 1);
                    if (getMaterial != IntPtr.Zero)
                    {
                        material = InvokeWithObjectArgument(
                            getMaterial,
                            canvasRenderer,
                            BoxInt32(FindImage("mscorlib.dll"), 0));
                    }
                }

                graphics.Add(new UiGraphicProbeRecord
                {
                    TypeNamespace = GetNativeName(runtimeClass, _api.ClassGetNamespace),
                    TypeName = GetNativeName(runtimeClass, _api.ClassGetName),
                    Name = InvokeInstanceString(
                        coreImage,
                        "UnityEngine",
                        "Object",
                        "get_name",
                        gameObject),
                    Path = path,
                    Enabled = InvokeInstanceBool(
                        coreImage,
                        "UnityEngine",
                        "Behaviour",
                        "get_enabled",
                        graphic),
                    ActiveInHierarchy = true,
                    RaycastTarget = Invoke(getRaycastTarget, graphic) is IntPtr boxedRaycast &&
                        boxedRaycast != IntPtr.Zero &&
                        Marshal.ReadInt32(_api.ObjectUnbox(boxedRaycast)) != 0,
                    Culled = canvasRenderer == IntPtr.Zero
                        ? null
                        : TryInvokeBool(canvasRendererClass, "get_cull", canvasRenderer),
                    AbsoluteDepth = canvasRenderer == IntPtr.Zero
                        ? null
                        : TryInvokeInt(canvasRendererClass, "get_absoluteDepth", canvasRenderer),
                    RelativeDepth = canvasRenderer == IntPtr.Zero
                        ? null
                        : TryInvokeInt(canvasRendererClass, "get_relativeDepth", canvasRenderer),
                    MaterialCount = materialCount,
                    MaterialName = material == IntPtr.Zero
                        ? null
                        : InvokeInstanceString(
                            coreImage,
                            "UnityEngine",
                            "Object",
                            "get_name",
                            material),
                    TextureName = mainTexture == IntPtr.Zero
                        ? null
                        : InvokeInstanceString(
                            coreImage,
                            "UnityEngine",
                            "Object",
                            "get_name",
                            mainTexture),
                    TextureWidth = mainTexture == IntPtr.Zero
                        ? null
                        : InvokeInstanceInt(
                            coreImage,
                            "UnityEngine",
                            "Texture",
                            "get_width",
                            mainTexture),
                    TextureHeight = mainTexture == IntPtr.Zero
                        ? null
                        : InvokeInstanceInt(
                            coreImage,
                            "UnityEngine",
                            "Texture",
                            "get_height",
                            mainTexture)
                });
                if (graphics.Count >= 512)
                {
                    break;
                }
            }
            catch
            {
                // A custom Graphic must not prevent the remaining standard UGUI
                // elements from being inventoried.
            }
        }

        graphics.Sort((left, right) =>
        {
            int depth = Nullable.Compare(left.AbsoluteDepth, right.AbsoluteDepth);
            return depth != 0 ? depth : string.CompareOrdinal(left.Path, right.Path);
        });
        return (canvases, rawImages, graphics);
    }

    private IReadOnlyList<VideoPlayerProbeRecord> CaptureVideoPlayers(
        IntPtr coreImage)
    {
        IntPtr videoImage = FindImage("UnityEngine.VideoModule.dll");
        IntPtr videoPlayerClass = FindClass(
            videoImage,
            "UnityEngine.Video",
            "VideoPlayer");
        IReadOnlyList<IntPtr> players = FindObjectsOfTypeAll(
            coreImage,
            videoImage,
            "UnityEngine.Video",
            "VideoPlayer",
            128);
        List<VideoPlayerProbeRecord> records = new(players.Count);
        foreach (IntPtr player in players)
        {
            try
            {
                IntPtr gameObject = InvokeInstanceObject(
                    coreImage,
                    "UnityEngine",
                    "Component",
                    "get_gameObject",
                    player);
                if (gameObject == IntPtr.Zero)
                {
                    continue;
                }

                IntPtr clip = TryInvokeObject(
                    videoPlayerClass,
                    "get_clip",
                    player);
                IntPtr targetCamera = TryInvokeObject(
                    videoPlayerClass,
                    "get_targetCamera",
                    player);
                IntPtr targetTexture = TryInvokeObject(
                    videoPlayerClass,
                    "get_targetTexture",
                    player);
                IntPtr outputTexture = TryInvokeObject(
                    videoPlayerClass,
                    "get_texture",
                    player);

                records.Add(new VideoPlayerProbeRecord
                {
                    Name = InvokeInstanceString(
                        coreImage,
                        "UnityEngine",
                        "Object",
                        "get_name",
                        gameObject),
                    Path = GetComponentPath(coreImage, player),
                    Enabled = InvokeInstanceBool(
                        coreImage,
                        "UnityEngine",
                        "Behaviour",
                        "get_enabled",
                        player),
                    ActiveInHierarchy = InvokeInstanceBool(
                        coreImage,
                        "UnityEngine",
                        "GameObject",
                        "get_activeInHierarchy",
                        gameObject),
                    IsPlaying = TryInvokeBool(
                        videoPlayerClass,
                        "get_isPlaying",
                        player),
                    IsPrepared = TryInvokeBool(
                        videoPlayerClass,
                        "get_isPrepared",
                        player),
                    Source = TryInvokeInt(
                        videoPlayerClass,
                        "get_source",
                        player),
                    ClipName = clip == IntPtr.Zero
                        ? null
                        : InvokeInstanceString(
                            coreImage,
                            "UnityEngine",
                            "Object",
                            "get_name",
                            clip),
                    RenderMode = TryInvokeInt(
                        videoPlayerClass,
                        "get_renderMode",
                        player),
                    AspectRatio = TryInvokeInt(
                        videoPlayerClass,
                        "get_aspectRatio",
                        player),
                    TargetCameraName = targetCamera == IntPtr.Zero
                        ? null
                        : InvokeInstanceString(
                            coreImage,
                            "UnityEngine",
                            "Object",
                            "get_name",
                            targetCamera),
                    TargetTextureName = targetTexture == IntPtr.Zero
                        ? null
                        : InvokeInstanceString(
                            coreImage,
                            "UnityEngine",
                            "Object",
                            "get_name",
                            targetTexture),
                    TargetTextureWidth = targetTexture == IntPtr.Zero
                        ? null
                        : InvokeInstanceInt(
                            coreImage,
                            "UnityEngine",
                            "Texture",
                            "get_width",
                            targetTexture),
                    TargetTextureHeight = targetTexture == IntPtr.Zero
                        ? null
                        : InvokeInstanceInt(
                            coreImage,
                            "UnityEngine",
                            "Texture",
                            "get_height",
                            targetTexture),
                    OutputTextureName = outputTexture == IntPtr.Zero
                        ? null
                        : InvokeInstanceString(
                            coreImage,
                            "UnityEngine",
                            "Object",
                            "get_name",
                            outputTexture),
                    OutputTextureWidth = outputTexture == IntPtr.Zero
                        ? null
                        : InvokeInstanceInt(
                            coreImage,
                            "UnityEngine",
                            "Texture",
                            "get_width",
                            outputTexture),
                    OutputTextureHeight = outputTexture == IntPtr.Zero
                        ? null
                        : InvokeInstanceInt(
                            coreImage,
                            "UnityEngine",
                            "Texture",
                            "get_height",
                            outputTexture)
                });
            }
            catch
            {
                // A destroyed or version-specific player must not block the
                // remaining read-only topology inventory.
            }
        }

        records.Sort((left, right) => string.CompareOrdinal(
            left.Path,
            right.Path));
        return records;
    }

    private IReadOnlyList<MediaSurfaceProbeRecord> CaptureM5MediaSurfaces(
        IntPtr coreImage)
    {
        if (!_m5MediaClassesDiscovered)
        {
            DiscoverM5MediaClasses();
        }

        List<MediaSurfaceProbeRecord> records = new();
        foreach (M5MediaClass mediaClass in _m5MediaClasses)
        {
            IReadOnlyList<IntPtr> instances = FindObjectsOfTypeAll(
                coreImage,
                mediaClass.Class,
                256);
            foreach (IntPtr instance in instances)
            {
                try
                {
                    IntPtr gameObject = InvokeInstanceObject(
                        coreImage,
                        "UnityEngine",
                        "Component",
                        "get_gameObject",
                        instance);
                    if (gameObject == IntPtr.Zero)
                    {
                        continue;
                    }

                    IntPtr runtimeClass = Marshal.ReadIntPtr(instance);
                    IntPtr texture = TryInvokeObjectAny(
                        runtimeClass,
                        instance,
                        "get_mainTexture",
                        "get_videoTexture",
                        "get_VideoTexture",
                        "get_texture",
                        "get_Texture");
                    string? textureName = null;
                    int? textureWidth = null;
                    int? textureHeight = null;
                    if (texture != IntPtr.Zero && IsClassDerivedFromSimpleName(
                            Marshal.ReadIntPtr(texture),
                            "Texture"))
                    {
                        textureName = InvokeInstanceString(
                            coreImage,
                            "UnityEngine",
                            "Object",
                            "get_name",
                            texture);
                        textureWidth = InvokeInstanceInt(
                            coreImage,
                            "UnityEngine",
                            "Texture",
                            "get_width",
                            texture);
                        textureHeight = InvokeInstanceInt(
                            coreImage,
                            "UnityEngine",
                            "Texture",
                            "get_height",
                            texture);
                    }

                    records.Add(new MediaSurfaceProbeRecord
                    {
                        AssemblyName = mediaClass.AssemblyName,
                        TypeNamespace = mediaClass.Namespace,
                        TypeName = mediaClass.Name,
                        Name = InvokeInstanceString(
                            coreImage,
                            "UnityEngine",
                            "Object",
                            "get_name",
                            gameObject),
                        Path = GetComponentPath(coreImage, instance),
                        Enabled = mediaClass.IsBehaviour
                            ? InvokeInstanceBool(
                                coreImage,
                                "UnityEngine",
                                "Behaviour",
                                "get_enabled",
                                instance)
                            : null,
                        ActiveInHierarchy = InvokeInstanceBool(
                            coreImage,
                            "UnityEngine",
                            "GameObject",
                            "get_activeInHierarchy",
                            gameObject),
                        IsPlaying = TryInvokeBoolAny(
                            runtimeClass,
                            instance,
                            "get_isPlaying",
                            "get_IsPlaying",
                            "get_isPlayingMovie"),
                        IsPrepared = TryInvokeBoolAny(
                            runtimeClass,
                            instance,
                            "get_isPrepared",
                            "get_IsPrepared"),
                        TextureName = textureName,
                        TextureWidth = textureWidth,
                        TextureHeight = textureHeight
                    });
                }
                catch
                {
                    // A scene transition can destroy one custom media component
                    // while the read-only inventory is running.
                }
            }
        }

        records.Sort((left, right) =>
        {
            int type = string.CompareOrdinal(left.TypeName, right.TypeName);
            return type != 0 ? type : string.CompareOrdinal(left.Path, right.Path);
        });
        return records;
    }

    private void DiscoverM5MediaClasses()
    {
        HashSet<string> wantedNames = new(StringComparer.Ordinal)
        {
            "ABLoopVideoPlayer",
            "BaseWebViewPrefab",
            "CampusCriVideoPlayer",
            "CampusStandaloneWebview",
            "CampusTimelineCriVideoPlayer",
            "CampusVideoPlayer",
            "CampusVideoPlayerUnityMaskable",
            "CampusWebView",
            "CanvasWebViewPrefab",
            "GashaVideoPlayerHandler"
        };
        string[] wantedAssemblies =
        {
            "Assembly-CSharp.dll",
            "campus-submodule.Runtime.dll",
            "CriMw.CriWare.Runtime.dll",
            "VideoKit.Runtime.dll",
            "Vuplex.WebView.dll"
        };
        List<M5MediaClass> discovered = new();
        foreach (string assemblyName in wantedAssemblies)
        {
            IntPtr image;
            try
            {
                image = FindImage(assemblyName);
            }
            catch
            {
                continue;
            }

            ulong classCount = _api.ImageGetClassCount(image).ToUInt64();
            if (classCount > 16_384)
            {
                continue;
            }

            for (ulong index = 0; index < classCount; index++)
            {
                IntPtr klass = _api.ImageGetClass(image, new UIntPtr(index));
                if (klass == IntPtr.Zero)
                {
                    continue;
                }

                string className = GetNativeName(klass, _api.ClassGetName);
                if (!wantedNames.Contains(className) ||
                    !IsClassDerivedFromSimpleName(klass, "Component"))
                {
                    continue;
                }

                discovered.Add(new M5MediaClass
                {
                    AssemblyName = assemblyName,
                    Namespace = GetNativeName(klass, _api.ClassGetNamespace),
                    Name = className,
                    Class = klass,
                    IsBehaviour = IsClassDerivedFromSimpleName(klass, "Behaviour")
                });
            }
        }

        _m5MediaClasses = discovered;
        _m5MediaClassesDiscovered = true;
        RuntimeProbe.Append(_logPath, new ProbeEvent
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Event = "m5-media-surface-types",
            BootstrapVersion = RuntimeProbe.BootstrapVersion,
            ProcessId = Environment.ProcessId,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            MediaSurfaceTypes = discovered.Select(value => string.Join(
                "|",
                value.AssemblyName,
                value.Namespace,
                value.Name)).ToArray(),
            Reason = "One-time metadata discovery for known game video/WebView components; no object or URL enumeration is performed here."
        });
    }

    private bool IsClassDerivedFromSimpleName(IntPtr klass, string wantedName)
    {
        IntPtr current = klass;
        for (int depth = 0; depth < 32 && current != IntPtr.Zero; depth++)
        {
            if (GetNativeName(current, _api.ClassGetName).Equals(
                wantedName,
                StringComparison.Ordinal))
            {
                return true;
            }
            current = _api.ClassGetParent(current);
        }

        return false;
    }

    private IntPtr TryInvokeObjectAny(
        IntPtr klass,
        IntPtr instance,
        params string[] methodNames)
    {
        foreach (string methodName in methodNames)
        {
            IntPtr method = TryFindMethodInHierarchy(klass, methodName, 0);
            if (method == IntPtr.Zero)
            {
                continue;
            }

            try
            {
                return Invoke(method, instance);
            }
            catch
            {
                // Try the next optional getter exposed by this custom type.
            }
        }

        return IntPtr.Zero;
    }

    private bool? TryInvokeBoolAny(
        IntPtr klass,
        IntPtr instance,
        params string[] methodNames)
    {
        foreach (string methodName in methodNames)
        {
            IntPtr method = TryFindMethodInHierarchy(klass, methodName, 0);
            if (method == IntPtr.Zero)
            {
                continue;
            }

            try
            {
                IntPtr boxed = Invoke(method, instance);
                IntPtr value = boxed == IntPtr.Zero
                    ? IntPtr.Zero
                    : _api.ObjectUnbox(boxed);
                if (value != IntPtr.Zero)
                {
                    return Marshal.ReadByte(value) != 0;
                }
            }
            catch
            {
                // Try the next optional getter exposed by this custom type.
            }
        }

        return null;
    }

    private static string BuildTopologySignature(
        IReadOnlyList<CameraProbeRecord> cameras,
        IReadOnlyList<CanvasProbeRecord> canvases,
        IReadOnlyList<RawImageProbeRecord> rawImages,
        IReadOnlyList<VideoPlayerProbeRecord> videoPlayers,
        IReadOnlyList<MediaSurfaceProbeRecord> mediaSurfaces)
    {
        List<string> parts = new(
            cameras.Count + canvases.Count + rawImages.Count +
            videoPlayers.Count + mediaSurfaces.Count);
        parts.AddRange(cameras.Select(camera => string.Join(
            ":",
            "camera",
            camera.Path,
            camera.Enabled,
            camera.ActiveInHierarchy,
            camera.TargetTextureName,
            camera.TargetTextureWidth,
            camera.TargetTextureHeight,
            camera.UrpRendererIndex,
            camera.UrpRenderType,
            camera.UrpCameraStackCount)));
        parts.AddRange(canvases.Select(canvas => string.Join(
            ":",
            "canvas",
            canvas.Path,
            canvas.Enabled,
            canvas.ActiveInHierarchy,
            canvas.RenderMode,
            canvas.SortingOrder,
            canvas.OverrideSorting,
            canvas.WorldCameraName)));
        parts.AddRange(rawImages.Select(rawImage => string.Join(
            ":",
            "raw",
            rawImage.Path,
            rawImage.Enabled,
            rawImage.ActiveInHierarchy,
            rawImage.RaycastTarget,
            rawImage.TextureName,
            rawImage.TextureWidth,
            rawImage.TextureHeight)));
        parts.AddRange(videoPlayers.Select(player => string.Join(
            ":",
            "video",
            player.Path,
            player.Enabled,
            player.ActiveInHierarchy,
            player.IsPlaying,
            player.IsPrepared,
            player.Source,
            player.ClipName,
            player.RenderMode,
            player.AspectRatio,
            player.TargetCameraName,
            player.TargetTextureName,
            player.OutputTextureName)));
        parts.AddRange(mediaSurfaces.Select(surface => string.Join(
            ":",
            "media",
            surface.AssemblyName,
            surface.TypeNamespace,
            surface.TypeName,
            surface.Path,
            surface.Enabled,
            surface.ActiveInHierarchy,
            surface.IsPlaying,
            surface.IsPrepared,
            surface.TextureName,
            surface.TextureWidth,
            surface.TextureHeight)));
        return string.Join("|", parts);
    }

    private IReadOnlyList<IntPtr> FindObjectsOfTypeAll(
        IntPtr coreImage,
        IntPtr typeImage,
        string namespaze,
        string className,
        int maximumLength = 512)
    {
        IntPtr klass = FindClass(typeImage, namespaze, className);
        return FindObjectsOfTypeAll(coreImage, klass, maximumLength);
    }

    private IReadOnlyList<IntPtr> FindObjectsOfTypeAll(
        IntPtr coreImage,
        IntPtr klass,
        int maximumLength)
    {
        Il2CppApi.ClassGetTypeDelegate classGetType = _api.ClassGetType ??
            throw new MissingMethodException("GameAssembly.dll does not export il2cpp_class_get_type.");
        Il2CppApi.TypeGetObjectDelegate typeGetObject = _api.TypeGetObject ??
            throw new MissingMethodException("GameAssembly.dll does not export il2cpp_type_get_object.");
        IntPtr nativeType = classGetType(klass);
        IntPtr managedType = nativeType == IntPtr.Zero ? IntPtr.Zero : typeGetObject(nativeType);
        if (managedType == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Could not create System.Type for the requested IL2CPP class.");
        }

        IntPtr method = FindMethod(coreImage, "UnityEngine", "Resources", "FindObjectsOfTypeAll", 1);
        IntPtr array = InvokeWithObjectArgument(method, IntPtr.Zero, managedType);
        return ReadObjectArray(array, maximumLength);
    }

    private static bool IsLiveUiPath(string path) =>
        path.Contains("LiveHorizontalRoot", StringComparison.Ordinal) ||
        path.Contains("OverlayRoot", StringComparison.Ordinal) ||
        path.Contains("EffectRoot", StringComparison.Ordinal) ||
        path.Contains("LiveLoading", StringComparison.Ordinal);

    private void TryCaptureUiReplayCapabilities(DateTimeOffset now)
    {
        try
        {
            List<Il2CppTypeProbeRecord> types = new();
            CaptureSelectedMethods(
                "UnityEngine.UIModule.dll",
                new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
                {
                    ["CanvasRenderer"] = new(StringComparer.Ordinal)
                    {
                        "get_absoluteDepth", "get_relativeDepth", "get_cull",
                        "get_materialCount", "get_popMaterialCount", "GetMaterial",
                        "GetPopMaterial", "GetMesh", "GetTexture"
                    },
                    ["Canvas"] = new(StringComparer.Ordinal)
                    {
                        "get_renderMode", "get_sortingOrder", "get_overrideSorting",
                        "get_worldCamera", "get_rootCanvas"
                    }
                },
                types);
            CaptureSelectedMethods(
                "UnityEngine.UI.dll",
                new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
                {
                    ["Graphic"] = new(StringComparer.Ordinal)
                    {
                        "get_canvasRenderer", "get_mainTexture", "get_material",
                        "get_materialForRendering", "get_raycastTarget"
                    },
                    ["MaskableGraphic"] = new(StringComparer.Ordinal)
                    {
                        "get_maskable", "get_isMaskingGraphic", "GetModifiedMaterial"
                    },
                    ["Mask"] = new(StringComparer.Ordinal)
                    {
                        "get_showMaskGraphic", "GetModifiedMaterial"
                    },
                    ["RectMask2D"] = new(StringComparer.Ordinal)
                    {
                        "get_canvasRect", "PerformClipping", "AddClippable", "RemoveClippable"
                    }
                },
                types);

            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = now,
                Event = "ui-element-replay-capabilities",
                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                ProcessId = Environment.ProcessId,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                UiReplayTypes = types,
                Reason = "Read-only UGUI replay capability inventory; no render state was changed."
            });
        }
        catch (Exception exception)
        {
            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = now,
                Event = "ui-element-replay-capabilities-failure",
                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                ProcessId = Environment.ProcessId,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                ErrorType = exception.GetType().FullName,
                Error = exception.Message
            });
        }
        finally
        {
            _uiReplayCapabilitiesCaptured = true;
        }
    }

    private void CaptureSelectedMethods(
        string assemblyName,
        IReadOnlyDictionary<string, HashSet<string>> wantedMethods,
        List<Il2CppTypeProbeRecord> destination)
    {
        IntPtr image = FindImage(assemblyName);
        ulong classCount = _api.ImageGetClassCount(image).ToUInt64();
        if (classCount > 16_384)
        {
            throw new InvalidOperationException(
                $"Unexpected IL2CPP class count in {assemblyName}: {classCount}.");
        }

        for (ulong index = 0; index < classCount; index++)
        {
            IntPtr klass = _api.ImageGetClass(image, new UIntPtr(index));
            if (klass == IntPtr.Zero)
            {
                continue;
            }

            string className = GetNativeName(klass, _api.ClassGetName);
            if (!wantedMethods.TryGetValue(className, out HashSet<string>? methodNames))
            {
                continue;
            }

            List<Il2CppMethodProbeRecord> methods = new();
            IntPtr iterator = IntPtr.Zero;
            for (int methodIndex = 0; methodIndex < 512; methodIndex++)
            {
                IntPtr method = _api.ClassGetMethods(klass, ref iterator);
                if (method == IntPtr.Zero)
                {
                    break;
                }

                if (methodNames.Contains(GetNativeName(method, _api.MethodGetName)))
                {
                    methods.Add(CreateMethodProbe(method));
                }
            }

            IntPtr parent = _api.ClassGetParent(klass);
            destination.Add(new Il2CppTypeProbeRecord
            {
                AssemblyName = assemblyName,
                Namespace = GetNativeName(klass, _api.ClassGetNamespace),
                ClassName = className,
                ParentClassName = parent == IntPtr.Zero
                    ? null
                    : GetNativeName(parent, _api.ClassGetName),
                Methods = methods
            });
        }
    }

    private void TryCaptureNaturalUiLayer(
        DateTimeOffset now,
        IntPtr coreImage,
        int width,
        int height)
    {
        string stage = _uiNaturalCaptureArmed ? "wait-normal-render" : "arm-normal-render";
        try
        {
            if (_uiNaturalCaptureArmed)
            {
                if (D3D11DeviceCapture.PresentSerial -
                    _uiNaturalCaptureStartPresentSerial < 2)
                {
                    return;
                }

                stage = "restore-live-ui";
                RestoreNaturalUiCapture(coreImage);
                D3D11Interop.WaitForGpu(
                    D3D11DeviceCapture.Context,
                    _stereoGpuCompletionQuery,
                    2_000);
                IntPtr nativeTexture = InvokeInstanceIntPtr(
                    coreImage,
                    "UnityEngine",
                    "Texture",
                    "GetNativeTexturePtr",
                    _uiCaptureRenderTexture);
                if (!D3D11Interop.HasVisiblePixels(
                        D3D11DeviceCapture.Device,
                        D3D11DeviceCapture.Context,
                        nativeTexture))
                {
                    throw new InvalidOperationException(
                        "The normal-render-loop UI texture contained no visible pixels.");
                }

                UnityRenderSourceRegistry.UpdateLiveUiTexture(
                    nativeTexture,
                    "UICamera natural UI-only (rendered)");
                _uiCaptureFrameCount = 1;
                _nextNaturalUiCaptureRetryMilliseconds = 0;
                RuntimeProbe.Append(_logPath, new ProbeEvent
                {
                    TimestampUtc = now,
                    Event = "ui-natural-capture-ready",
                    BootstrapVersion = RuntimeProbe.BootstrapVersion,
                    ProcessId = Environment.ProcessId,
                    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                    UiCaptureStage = "complete",
                    UiCaptureSubmitted = true,
                    UiCaptureRawImageSuppressed = true,
                    UiCaptureRawImageRestored = true,
                    UiCaptureWidth = width,
                    UiCaptureHeight = height,
                    UiCaptureTextureDescription = D3D11Interop.DescribeTexture(nativeTexture),
                    UiCaptureFrameCount = 1,
                    Reason = "Original UICamera rendered normally for two Present cycles into a transparent RT while 3DTexture was culled."
                });
                return;
            }

            IntPtr threeDTexture = _cachedThreeDTextureRawImage;
            if (threeDTexture == IntPtr.Zero)
            {
                IntPtr uiImage = FindImage("UnityEngine.UI.dll");
                IReadOnlyList<IntPtr> rawImages = FindObjectsOfTypeAll(
                    coreImage,
                    uiImage,
                    "UnityEngine.UI",
                    "RawImage");
                foreach (IntPtr rawImage in rawImages)
                {
                    string path = GetComponentPath(coreImage, rawImage);
                    if (path.EndsWith(
                            "/LiveFullScreenRoot/VisbleRoot/3DTexture",
                            StringComparison.Ordinal))
                    {
                        threeDTexture = rawImage;
                        _cachedThreeDTextureRawImage = rawImage;
                        break;
                    }
                }
            }
            if (threeDTexture == IntPtr.Zero)
            {
                throw new InvalidOperationException("Live 3DTexture RawImage was not found.");
            }

            _uiNaturalCamera = FindCameraByName(coreImage, "UICamera");
            if (_uiNaturalCamera == IntPtr.Zero)
            {
                throw new InvalidOperationException("UICamera was not found.");
            }

            EnsureUiCaptureObjects(coreImage, width, height);
            IntPtr nativeUiTexture = InvokeInstanceIntPtr(
                coreImage,
                "UnityEngine",
                "Texture",
                "GetNativeTexturePtr",
                _uiCaptureRenderTexture);
            D3D11Texture2DDescription description =
                D3D11Interop.GetTextureDescription(nativeUiTexture);
            int viewFormat = description.Format switch
            {
                27 => 28,
                90 => 87,
                _ => description.Format
            };
            IntPtr renderTargetView = D3D11Interop.CreateRenderTargetView(
                D3D11DeviceCapture.Device,
                nativeUiTexture,
                viewFormat);
            try
            {
                D3D11Interop.ClearRenderTargetView(
                    D3D11DeviceCapture.Context,
                    renderTargetView,
                    new Color4());
            }
            finally
            {
                D3D11Interop.Release(renderTargetView);
            }

            IntPtr uiImageAssembly = FindImage("UnityEngine.UI.dll");
            _uiNaturalCanvasImage = FindImage("UnityEngine.UIModule.dll");
            _uiNaturalSuppressedRenderers.Clear();
            SuppressNaturalUiGraphic(uiImageAssembly, threeDTexture);
            IReadOnlyList<IntPtr> graphics = FindObjectsOfTypeAll(
                coreImage,
                uiImageAssembly,
                "UnityEngine.UI",
                "Image",
                2048);
            foreach (IntPtr graphic in graphics)
            {
                string path = GetComponentPath(coreImage, graphic);
                if (path.EndsWith(
                        "/LiveHorizontalRoot/Root/LiveFullScreenRoot/Background",
                        StringComparison.Ordinal) ||
                    path.EndsWith(
                        "/OverlayRoot/FullScreen/FadeRoot/BlackTint",
                        StringComparison.Ordinal))
                {
                    SuppressNaturalUiGraphic(uiImageAssembly, graphic);
                }
            }
            _ = Invoke(
                FindMethod(
                    _uiNaturalCanvasImage,
                    "UnityEngine",
                    "Canvas",
                    "ForceUpdateCanvases"),
                IntPtr.Zero);

            _uiNaturalOriginalTargetTexture = InvokeInstanceObject(
                coreImage,
                "UnityEngine",
                "Camera",
                "get_targetTexture",
                _uiNaturalCamera);
            _ = InvokeWithObjectArgument(
                FindMethodBySignature(
                    FindClass(coreImage, "UnityEngine", "Camera"),
                    "set_targetTexture",
                    "UnityEngine.RenderTexture"),
                _uiNaturalCamera,
                _uiCaptureRenderTexture);
            _uiNaturalCaptureStartPresentSerial = D3D11DeviceCapture.PresentSerial;
            _uiNaturalCaptureArmed = true;
            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = now,
                Event = "ui-natural-capture-armed",
                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                ProcessId = Environment.ProcessId,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                UiCaptureStage = stage,
                UiCaptureSubmitted = false,
                UiCaptureRawImageSuppressed = true,
                UiCaptureSuppressedGraphicCount =
                    _uiNaturalSuppressedRenderers.Count,
                UiCaptureWidth = width,
                UiCaptureHeight = height,
                Reason = "UICamera target redirected to a transparent RT for two normal Present cycles."
            });
        }
        catch (Exception exception)
        {
            try
            {
                RestoreNaturalUiCapture(coreImage);
            }
            catch
            {
                // Preserve the original capture exception in the diagnostic log.
            }
            _uiCaptureFrameCount = 0;
            _nextNaturalUiCaptureRetryMilliseconds =
                Environment.TickCount64 + 2_000;
            UnityRenderSourceRegistry.ClearLiveUiTexture();
            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = now,
                Event = "ui-natural-capture-failure",
                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                ProcessId = Environment.ProcessId,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                UiCaptureStage = stage,
                UiCaptureSubmitted = false,
                ErrorType = exception.GetType().FullName,
                Error = exception.Message
            });
        }
    }

    private void SuppressNaturalUiGraphic(IntPtr uiImageAssembly, IntPtr graphic)
    {
        IntPtr renderer = InvokeInstanceObject(
            uiImageAssembly,
            "UnityEngine.UI",
            "Graphic",
            "get_canvasRenderer",
            graphic);
        if (renderer == IntPtr.Zero ||
            _uiNaturalSuppressedRenderers.Any(entry => entry.Renderer == renderer))
        {
            return;
        }

        bool wasCulled = InvokeInstanceBool(
            _uiNaturalCanvasImage,
            "UnityEngine",
            "CanvasRenderer",
            "get_cull",
            renderer);
        _uiNaturalSuppressedRenderers.Add((renderer, wasCulled));
        InvokeInstanceBooleanSetter(
            _uiNaturalCanvasImage,
            "UnityEngine",
            "CanvasRenderer",
            "set_cull",
            renderer,
            true);
    }

    private void RestoreNaturalUiCapture(IntPtr coreImage)
    {
        if (_uiNaturalCamera != IntPtr.Zero)
        {
            _ = InvokeWithObjectArgument(
                FindMethodBySignature(
                    FindClass(coreImage, "UnityEngine", "Camera"),
                    "set_targetTexture",
                    "UnityEngine.RenderTexture"),
                _uiNaturalCamera,
                _uiNaturalOriginalTargetTexture);
        }
        if (_uiNaturalCanvasImage != IntPtr.Zero)
        {
            foreach ((IntPtr renderer, bool wasCulled) in _uiNaturalSuppressedRenderers)
            {
                InvokeInstanceBooleanSetter(
                    _uiNaturalCanvasImage,
                    "UnityEngine",
                    "CanvasRenderer",
                    "set_cull",
                    renderer,
                    wasCulled);
            }
            _ = Invoke(
                FindMethod(
                    _uiNaturalCanvasImage,
                    "UnityEngine",
                    "Canvas",
                    "ForceUpdateCanvases"),
                IntPtr.Zero);
        }
        _uiNaturalSuppressedRenderers.Clear();
        _uiNaturalCaptureArmed = false;
    }

    private void TryRenderUiElementLayer(
        DateTimeOffset now,
        IntPtr coreImage,
        int width,
        int height)
    {
        if (_uiReplayInProgress)
        {
            return;
        }

        _uiReplayInProgress = true;
        string stage = "ensure-objects";
        try
        {
            EnsureUiReplayObjects(coreImage, width, height);
            IntPtr uiCamera = FindCameraByName(coreImage, "UICamera");
            if (uiCamera == IntPtr.Zero)
            {
                throw new InvalidOperationException("UICamera was not found for UI element replay.");
            }

            IntPtr canvasImage = FindImage("UnityEngine.UIModule.dll");
            IntPtr uiImage = FindImage("UnityEngine.UI.dll");
            IntPtr canvasClass = FindClass(canvasImage, "UnityEngine", "Canvas");
            IntPtr canvasRendererClass = FindClass(canvasImage, "UnityEngine", "CanvasRenderer");
            IntPtr graphicClass = FindClass(uiImage, "UnityEngine.UI", "Graphic");
            IReadOnlyList<IntPtr> canvasObjects = FindObjectsOfTypeAll(
                coreImage,
                canvasImage,
                "UnityEngine",
                "Canvas");
            IReadOnlyList<IntPtr> graphicObjects = FindObjectsOfTypeAll(
                coreImage,
                uiImage,
                "UnityEngine.UI",
                "Graphic",
                4096);

            stage = "collect-canvases";
            List<UiReplayCanvas> replayCanvases = new();
            foreach (IntPtr canvas in canvasObjects)
            {
                IntPtr gameObject = InvokeInstanceObject(
                    coreImage,
                    "UnityEngine",
                    "Component",
                    "get_gameObject",
                    canvas);
                if (gameObject == IntPtr.Zero || !InvokeInstanceBool(
                        coreImage,
                        "UnityEngine",
                        "GameObject",
                        "get_activeInHierarchy",
                        gameObject))
                {
                    continue;
                }

                string canvasPath = GetComponentPath(coreImage, canvas);
                if (!IsLiveUiPath(canvasPath))
                {
                    continue;
                }

                replayCanvases.Add(new UiReplayCanvas
                {
                    Path = canvasPath,
                    SortingOrder = TryInvokeInt(canvasClass, "get_sortingOrder", canvas) ?? 0,
                    RenderOrder = TryInvokeInt(canvasClass, "get_renderOrder", canvas) ?? 0
                });
            }

            stage = "collect-graphics";
            IntPtr getCanvasRenderer = FindMethod(graphicClass, "get_canvasRenderer");
            IntPtr defaultGraphicMaterial = Invoke(
                FindMethod(graphicClass, "get_defaultGraphicMaterial"),
                IntPtr.Zero);
            if (defaultGraphicMaterial == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "Graphic.defaultGraphicMaterial returned null.");
            }
            List<UiReplayDraw> draws = new();
            foreach (IntPtr graphic in graphicObjects)
            {
                try
                {
                    IntPtr gameObject = InvokeInstanceObject(
                        coreImage,
                        "UnityEngine",
                        "Component",
                        "get_gameObject",
                        graphic);
                    if (gameObject == IntPtr.Zero || !InvokeInstanceBool(
                            coreImage,
                            "UnityEngine",
                            "GameObject",
                            "get_activeInHierarchy",
                            gameObject) ||
                        !InvokeInstanceBool(
                            coreImage,
                            "UnityEngine",
                            "Behaviour",
                            "get_enabled",
                            graphic))
                    {
                        continue;
                    }

                    string path = GetComponentPath(coreImage, graphic);
                    if (!IsLiveUiPath(path) || path.EndsWith(
                            "/LiveFullScreenRoot/VisbleRoot/3DTexture",
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    IntPtr canvasRenderer = Invoke(getCanvasRenderer, graphic);
                    if (canvasRenderer == IntPtr.Zero ||
                        TryInvokeBool(canvasRendererClass, "get_cull", canvasRenderer) == true)
                    {
                        continue;
                    }

                    int materialCount = TryInvokeInt(
                        canvasRendererClass,
                        "get_materialCount",
                        canvasRenderer) ?? 0;
                    if (materialCount <= 0)
                    {
                        continue;
                    }

                    UiReplayCanvas? owner = replayCanvases
                        .Where(canvas => path.Equals(canvas.Path, StringComparison.Ordinal) ||
                            path.StartsWith(canvas.Path + "/", StringComparison.Ordinal))
                        .OrderByDescending(canvas => canvas.Path.Length)
                        .FirstOrDefault();
                    IntPtr runtimeClass = Marshal.ReadIntPtr(graphic);
                    draws.Add(new UiReplayDraw
                    {
                        Graphic = graphic,
                        CanvasRenderer = canvasRenderer,
                        RuntimeClass = runtimeClass,
                        Path = path,
                        TypeName = GetNativeName(runtimeClass, _api.ClassGetName),
                        SortingOrder = owner?.SortingOrder ?? 0,
                        RenderOrder = owner?.RenderOrder ?? 0,
                        AbsoluteDepth = TryInvokeInt(
                            canvasRendererClass,
                            "get_absoluteDepth",
                            canvasRenderer) ?? 0,
                        MaterialCount = materialCount
                    });
                }
                catch
                {
                    // Custom graphics can appear or disappear while the hierarchy is
                    // traversed. A single stale component must not abort the layer.
                }
            }

            draws.Sort((left, right) =>
            {
                int value = left.SortingOrder.CompareTo(right.SortingOrder);
                if (value == 0)
                {
                    value = left.RenderOrder.CompareTo(right.RenderOrder);
                }
                if (value == 0)
                {
                    value = left.AbsoluteDepth.CompareTo(right.AbsoluteDepth);
                }
                return value != 0 ? value : string.CompareOrdinal(left.Path, right.Path);
            });

            IntPtr commandBufferClass = FindClass(
                coreImage,
                "UnityEngine.Rendering",
                "CommandBuffer");
            IntPtr propertyBlockClass = FindClass(
                coreImage,
                "UnityEngine",
                "MaterialPropertyBlock");
            IntPtr meshClass = FindClass(coreImage, "UnityEngine", "Mesh");
            IntPtr getMesh = FindMethod(canvasRendererClass, "GetMesh");
            IntPtr getMaterial = FindMethodBySignature(
                canvasRendererClass,
                "GetMaterial",
                "System.Int32");
            IntPtr clearCommandBuffer = FindMethod(commandBufferClass, "Clear");
            IntPtr setRenderTarget = FindMethodBySignature(
                commandBufferClass,
                "SetRenderTarget",
                "UnityEngine.Rendering.RenderTargetIdentifier");
            IntPtr clearRenderTarget = FindMethodBySignature(
                commandBufferClass,
                "ClearRenderTarget",
                "System.Boolean",
                "System.Boolean",
                "UnityEngine.Color");
            IntPtr drawMesh = FindMethodBySignature(
                commandBufferClass,
                "DrawMesh",
                "UnityEngine.Mesh",
                "UnityEngine.Matrix4x4",
                "UnityEngine.Material",
                "System.Int32",
                "System.Int32",
                "UnityEngine.MaterialPropertyBlock");
            IntPtr clearPropertyBlock = FindMethod(propertyBlockClass, "Clear");
            IntPtr setTexture = FindMethodBySignature(
                propertyBlockClass,
                "SetTexture",
                "System.Int32",
                "UnityEngine.Texture");

            stage = "record-command-buffer";
            _ = Invoke(clearCommandBuffer, _uiReplayCommandBuffer);
            _ = InvokeWithObjectArgument(
                setRenderTarget,
                _uiReplayCommandBuffer,
                _uiReplayRenderTargetIdentifier);
            _ = InvokeWithObjectArguments(
                clearRenderTarget,
                _uiReplayCommandBuffer,
                BoxBoolean(FindImage("mscorlib.dll"), true),
                BoxBoolean(FindImage("mscorlib.dll"), true),
                BoxColor(coreImage, 0f, 0f, 0f, 0f));

            IntPtr viewMatrix = Invoke(
                FindMethod(coreImage, "UnityEngine", "Camera", "get_worldToCameraMatrix"),
                uiCamera);
            IntPtr projectionMatrix = Invoke(
                FindMethod(coreImage, "UnityEngine", "Camera", "get_projectionMatrix"),
                uiCamera);
            IntPtr gpuProjection = InvokeWithObjectArguments(
                FindMethodBySignature(
                    FindClass(coreImage, "UnityEngine", "GL"),
                    "GetGPUProjectionMatrix",
                    "UnityEngine.Matrix4x4",
                    "System.Boolean"),
                IntPtr.Zero,
                projectionMatrix,
                BoxBoolean(FindImage("mscorlib.dll"), true));
            IntPtr universalImage = FindImage(
                "Unity.RenderPipelines.Universal.Runtime.dll");
            IntPtr renderingUtilsClass = FindClass(
                universalImage,
                "UnityEngine.Rendering.Universal",
                "RenderingUtils");
            _ = InvokeWithObjectArguments(
                FindMethodBySignature(
                    renderingUtilsClass,
                    "SetViewAndProjectionMatrices",
                    "UnityEngine.Rendering.CommandBuffer",
                    "UnityEngine.Matrix4x4",
                    "UnityEngine.Matrix4x4",
                    "System.Boolean"),
                IntPtr.Zero,
                _uiReplayCommandBuffer,
                viewMatrix,
                gpuProjection,
                BoxBoolean(FindImage("mscorlib.dll"), true));

            int drawCallCount = 0;
            int defaultMaterialDrawCallCount = 0;
            foreach (UiReplayDraw draw in draws)
            {
                IntPtr mesh = Invoke(getMesh, draw.CanvasRenderer);
                if (mesh == IntPtr.Zero)
                {
                    continue;
                }

                int subMeshCount = InvokeInstanceInt(
                    coreImage,
                    "UnityEngine",
                    "Mesh",
                    "get_subMeshCount",
                    mesh);
                if (subMeshCount <= 0)
                {
                    continue;
                }

                IntPtr transform = InvokeInstanceObject(
                    coreImage,
                    "UnityEngine",
                    "Component",
                    "get_transform",
                    draw.Graphic);
                IntPtr matrix = Invoke(
                    FindMethod(coreImage, "UnityEngine", "Transform", "get_localToWorldMatrix"),
                    transform);
                IntPtr mainTexture = TryInvokeObject(
                    draw.RuntimeClass,
                    "get_mainTexture",
                    draw.Graphic);
                int drawMaterialCount = Math.Min(draw.MaterialCount, subMeshCount);
                for (int materialIndex = 0; materialIndex < drawMaterialCount; materialIndex++)
                {
                    bool useDefaultGraphicMaterial = draw.TypeName is "Image" or "RawImage";
                    IntPtr material = useDefaultGraphicMaterial
                        ? defaultGraphicMaterial
                        : InvokeWithObjectArgument(
                            getMaterial,
                            draw.CanvasRenderer,
                            BoxInt32(FindImage("mscorlib.dll"), materialIndex));
                    if (material == IntPtr.Zero)
                    {
                        continue;
                    }

                    _ = Invoke(clearPropertyBlock, _uiReplayPropertyBlock);
                    if (mainTexture != IntPtr.Zero && draw.TypeName is
                        "Image" or "RawImage" or "CircleRoundLine")
                    {
                        _ = InvokeWithObjectArguments(
                            setTexture,
                            _uiReplayPropertyBlock,
                            BoxInt32(
                                FindImage("mscorlib.dll"),
                                _uiReplayMainTexturePropertyId),
                            mainTexture);
                    }

                    _ = InvokeWithObjectArguments(
                        drawMesh,
                        _uiReplayCommandBuffer,
                        mesh,
                        matrix,
                        material,
                        BoxInt32(FindImage("mscorlib.dll"), materialIndex),
                        BoxInt32(FindImage("mscorlib.dll"), -1),
                        _uiReplayPropertyBlock);
                    drawCallCount++;
                    if (useDefaultGraphicMaterial)
                    {
                        defaultMaterialDrawCallCount++;
                    }
                }
            }

            stage = "execute-command-buffer";
            IntPtr graphicsClass = FindClass(coreImage, "UnityEngine", "Graphics");
            _ = InvokeWithObjectArgument(
                FindMethodBySignature(
                    graphicsClass,
                    "ExecuteCommandBuffer",
                    "UnityEngine.Rendering.CommandBuffer"),
                IntPtr.Zero,
                _uiReplayCommandBuffer);
            D3D11Interop.WaitForGpu(
                D3D11DeviceCapture.Context,
                _stereoGpuCompletionQuery,
                1_000);
            if (!UnityRenderSourceRegistry.TouchLiveUiTexture(
                    "CanvasRenderer UI-only dynamic replay (rendered)"))
            {
                throw new InvalidOperationException("The UI replay native texture lease expired.");
            }

            _uiReplayFrameCount++;
            if (_uiReplayFrameCount == 1 || now >= _nextUiReplayLogUtc)
            {
                IntPtr nativeTexture = InvokeInstanceIntPtr(
                    coreImage,
                    "UnityEngine",
                    "Texture",
                    "GetNativeTexturePtr",
                    _uiReplayRenderTexture);
                RuntimeProbe.Append(_logPath, new ProbeEvent
                {
                    TimestampUtc = now,
                    Event = "ui-element-replay-ready",
                    BootstrapVersion = RuntimeProbe.BootstrapVersion,
                    ProcessId = Environment.ProcessId,
                    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                    UiCaptureStage = stage,
                    UiCaptureSubmitted = true,
                    UiCaptureWidth = width,
                    UiCaptureHeight = height,
                    UiCaptureTextureDescription = D3D11Interop.DescribeTexture(nativeTexture),
                    UiCaptureFrameCount = _uiReplayFrameCount,
                    Reason = $"elements={draws.Count};drawCalls={drawCallCount};" +
                        $"defaultMaterialDrawCalls={defaultMaterialDrawCallCount};" +
                        "3DTexture=excluded;transparentBackground=true;dynamic=true"
                });
                _nextUiReplayLogUtc = now.AddSeconds(10);
            }
        }
        catch (Exception exception)
        {
            if (now >= _nextUiReplayFailureUtc)
            {
                RuntimeProbe.Append(_logPath, new ProbeEvent
                {
                    TimestampUtc = now,
                    Event = "ui-element-replay-failure",
                    BootstrapVersion = RuntimeProbe.BootstrapVersion,
                    ProcessId = Environment.ProcessId,
                    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                    UiCaptureStage = stage,
                    UiCaptureSubmitted = false,
                    UiCaptureFrameCount = _uiReplayFrameCount,
                    ErrorType = exception.GetType().FullName,
                    Error = exception.Message
                });
                _nextUiReplayFailureUtc = now.AddSeconds(5);
            }
        }
        finally
        {
            _uiReplayInProgress = false;
        }
    }

    private void EnsureUiReplayObjects(IntPtr coreImage, int width, int height)
    {
        if (_uiReplayRenderTexture != IntPtr.Zero)
        {
            return;
        }

        IntPtr mscorlib = FindImage("mscorlib.dll");
        IntPtr renderTextureClass = FindClass(coreImage, "UnityEngine", "RenderTexture");
        _uiReplayRenderTexture = _api.ObjectNew(renderTextureClass);
        _uiReplayRenderTextureHandle = RootObject(_uiReplayRenderTexture, "UI replay RenderTexture");
        _ = InvokeWithObjectArguments(
            FindMethodBySignature(
                renderTextureClass,
                ".ctor",
                "System.Int32",
                "System.Int32",
                "System.Int32"),
            _uiReplayRenderTexture,
            BoxInt32(mscorlib, width),
            BoxInt32(mscorlib, height),
            BoxInt32(mscorlib, 24));
        _ = Invoke(FindMethod(renderTextureClass, "Create"), _uiReplayRenderTexture);

        IntPtr renderTargetIdentifierClass = FindClass(
            coreImage,
            "UnityEngine.Rendering",
            "RenderTargetIdentifier");
        _uiReplayRenderTargetIdentifier = _api.ObjectNew(renderTargetIdentifierClass);
        _uiReplayRenderTargetIdentifierHandle = RootObject(
            _uiReplayRenderTargetIdentifier,
            "UI replay RenderTargetIdentifier");
        _ = InvokeWithObjectArgument(
            FindMethodBySignature(
                renderTargetIdentifierClass,
                ".ctor",
                "UnityEngine.Texture"),
            _api.ObjectUnbox(_uiReplayRenderTargetIdentifier),
            _uiReplayRenderTexture);

        IntPtr commandBufferClass = FindClass(
            coreImage,
            "UnityEngine.Rendering",
            "CommandBuffer");
        _uiReplayCommandBuffer = _api.ObjectNew(commandBufferClass);
        _uiReplayCommandBufferHandle = RootObject(
            _uiReplayCommandBuffer,
            "UI replay CommandBuffer");
        _ = Invoke(FindMethod(commandBufferClass, ".ctor"), _uiReplayCommandBuffer);

        IntPtr propertyBlockClass = FindClass(
            coreImage,
            "UnityEngine",
            "MaterialPropertyBlock");
        _uiReplayPropertyBlock = _api.ObjectNew(propertyBlockClass);
        _uiReplayPropertyBlockHandle = RootObject(
            _uiReplayPropertyBlock,
            "UI replay MaterialPropertyBlock");
        _ = Invoke(FindMethod(propertyBlockClass, ".ctor"), _uiReplayPropertyBlock);

        IntPtr propertyName = NewManagedString("_MainTex");
        IntPtr propertyIdBox = InvokeWithObjectArgument(
            FindMethodBySignature(
                FindClass(coreImage, "UnityEngine", "Shader"),
                "PropertyToID",
                "System.String"),
            IntPtr.Zero,
            propertyName);
        IntPtr propertyIdValue = propertyIdBox == IntPtr.Zero
            ? IntPtr.Zero
            : _api.ObjectUnbox(propertyIdBox);
        if (propertyIdValue == IntPtr.Zero)
        {
            throw new InvalidOperationException("Shader.PropertyToID(_MainTex) returned null.");
        }
        _uiReplayMainTexturePropertyId = Marshal.ReadInt32(propertyIdValue);

        IntPtr nativeTexture = InvokeInstanceIntPtr(
            coreImage,
            "UnityEngine",
            "Texture",
            "GetNativeTexturePtr",
            _uiReplayRenderTexture);
        UnityRenderSourceRegistry.UpdateLiveUiTexture(
            nativeTexture,
            "CanvasRenderer element replay (unrendered)");
    }

    private uint RootObject(IntPtr instance, string label)
    {
        if (instance == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Could not allocate {label}.");
        }

        uint handle = _api.GcHandleNew(instance, pinned: false);
        return handle == 0
            ? throw new InvalidOperationException($"Could not root {label}.")
            : handle;
    }

    private void TryCaptureUrpCapabilities(DateTimeOffset now)
    {
        try
        {
            List<Il2CppTypeProbeRecord> types = new();
            CaptureMatchingTypes(
                "UnityEngine.CoreModule.dll",
                new HashSet<string>(StringComparer.Ordinal)
                {
                    "RenderPipeline",
                    "RequestData",
                    "StandardRequest"
                },
                types);
            CaptureMatchingTypes(
                "Unity.RenderPipelines.Core.Runtime.dll",
                new HashSet<string>(StringComparer.Ordinal)
                {
                    "RenderPipeline",
                    "RequestData",
                    "StandardRequest"
                },
                types);
            CaptureMatchingTypes(
                "Unity.RenderPipelines.Universal.Runtime.dll",
                new HashSet<string>(StringComparer.Ordinal)
                {
                    "UniversalRenderPipeline",
                    "SingleCameraRequest"
                },
                types);
            CaptureRenderRequestMethodOwners("UnityEngine.CoreModule.dll", types);
            CaptureRenderRequestMethodOwners(
                "Unity.RenderPipelines.Universal.Runtime.dll",
                types);

            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = now,
                Event = "urp-ui-capture-capabilities",
                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                ProcessId = Environment.ProcessId,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                UrpRequestTypes = types,
                Reason = types.Any(type => type.ClassName.Equals(
                    "SingleCameraRequest",
                    StringComparison.Ordinal))
                    ? "URP SingleCameraRequest metadata is available; no render state was changed."
                    : "URP SingleCameraRequest metadata was not found; no render state was changed."
            });
            _urpCapabilitiesCaptured = true;
        }
        catch (Exception exception)
        {
            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = now,
                Event = "urp-ui-capture-capabilities-failure",
                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                ProcessId = Environment.ProcessId,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                ErrorType = exception.GetType().FullName,
                Error = exception.Message
            });
            _urpCapabilitiesCaptured = true;
        }
    }

    private void CaptureMatchingTypes(
        string assemblyName,
        HashSet<string> wantedClassNames,
        List<Il2CppTypeProbeRecord> destination)
    {
        IntPtr image = FindImage(assemblyName);
        ulong classCount = _api.ImageGetClassCount(image).ToUInt64();
        if (classCount > 16_384)
        {
            throw new InvalidOperationException(
                $"Unexpected IL2CPP class count in {assemblyName}: {classCount}.");
        }

        for (ulong index = 0; index < classCount; index++)
        {
            IntPtr klass = _api.ImageGetClass(image, new UIntPtr(index));
            string className = GetNativeName(klass, _api.ClassGetName);
            if (klass == IntPtr.Zero || !wantedClassNames.Contains(className))
            {
                continue;
            }

            IntPtr parent = _api.ClassGetParent(klass);
            List<Il2CppMethodProbeRecord> methods = new();
            IntPtr iterator = IntPtr.Zero;
            for (int methodIndex = 0; methodIndex < 256; methodIndex++)
            {
                IntPtr method = _api.ClassGetMethods(klass, ref iterator);
                if (method == IntPtr.Zero)
                {
                    break;
                }

                string methodName = GetNativeName(method, _api.MethodGetName);
                if (methodName is ".ctor" or "get_destination" or "set_destination" or
                    "SubmitRenderRequest" or "SupportsRenderRequest")
                {
                    methods.Add(CreateMethodProbe(method));
                }
            }

            List<Il2CppFieldProbeRecord> fields = new();
            iterator = IntPtr.Zero;
            for (int fieldIndex = 0; fieldIndex < 256; fieldIndex++)
            {
                IntPtr field = _api.ClassGetFields(klass, ref iterator);
                if (field == IntPtr.Zero)
                {
                    break;
                }

                string fieldName = GetNativeName(field, _api.FieldGetName);
                if (fieldName.Contains("destination", StringComparison.OrdinalIgnoreCase) ||
                    fieldName.Contains("camera", StringComparison.OrdinalIgnoreCase))
                {
                    fields.Add(new Il2CppFieldProbeRecord
                    {
                        Name = fieldName,
                        Offset = _api.FieldGetOffset(field)
                    });
                }
            }

            destination.Add(new Il2CppTypeProbeRecord
            {
                AssemblyName = assemblyName,
                Namespace = GetNativeName(klass, _api.ClassGetNamespace),
                ClassName = className,
                ParentClassName = parent == IntPtr.Zero
                    ? null
                    : GetNativeName(parent, _api.ClassGetName),
                Methods = methods,
                Fields = fields
            });
        }
    }

    private void CaptureRenderRequestMethodOwners(
        string assemblyName,
        List<Il2CppTypeProbeRecord> destination)
    {
        IntPtr image = FindImage(assemblyName);
        ulong classCount = _api.ImageGetClassCount(image).ToUInt64();
        if (classCount > 16_384)
        {
            throw new InvalidOperationException(
                $"Unexpected IL2CPP class count in {assemblyName}: {classCount}.");
        }

        for (ulong index = 0; index < classCount; index++)
        {
            IntPtr klass = _api.ImageGetClass(image, new UIntPtr(index));
            if (klass == IntPtr.Zero)
            {
                continue;
            }

            List<Il2CppMethodProbeRecord> renderRequestMethods = new();
            IntPtr iterator = IntPtr.Zero;
            for (int methodIndex = 0; methodIndex < 1_024; methodIndex++)
            {
                IntPtr method = _api.ClassGetMethods(klass, ref iterator);
                if (method == IntPtr.Zero)
                {
                    break;
                }

                string methodName = GetNativeName(method, _api.MethodGetName);
                if (methodName.Contains("RenderRequest", StringComparison.Ordinal))
                {
                    renderRequestMethods.Add(CreateMethodProbe(method));
                }
            }

            if (renderRequestMethods.Count == 0)
            {
                continue;
            }

            string className = GetNativeName(klass, _api.ClassGetName);
            Il2CppTypeProbeRecord? existing = destination.FirstOrDefault(record =>
                record.AssemblyName.Equals(assemblyName, StringComparison.Ordinal) &&
                record.ClassName.Equals(className, StringComparison.Ordinal));
            if (existing is not null)
            {
                existing.Methods = existing.Methods
                    .Concat(renderRequestMethods)
                    .GroupBy(method => (method.Name, method.ParameterCount))
                    .Select(group => group.First())
                    .ToArray();
                continue;
            }

            IntPtr parent = _api.ClassGetParent(klass);
            destination.Add(new Il2CppTypeProbeRecord
            {
                AssemblyName = assemblyName,
                Namespace = GetNativeName(klass, _api.ClassGetNamespace),
                ClassName = className,
                ParentClassName = parent == IntPtr.Zero
                    ? null
                    : GetNativeName(parent, _api.ClassGetName),
                Methods = renderRequestMethods
            });
        }
    }

    private void TrySubmitUiCapture(
        DateTimeOffset now,
        IntPtr coreImage,
        int width,
        int height,
        IReadOnlyList<IntPtr>? rawImageObjects)
    {
        if (_uiCaptureInProgress)
        {
            return;
        }

        _uiCaptureInProgress = true;
        string stage = "locate-live-ui";
        bool suppressed = false;
        bool restored = false;
        bool? requestSupported = null;
        IntPtr threeDTexture = _cachedThreeDTextureRawImage;
        try
        {
            if (threeDTexture == IntPtr.Zero)
            {
                IntPtr uiImage = FindImage("UnityEngine.UI.dll");
                rawImageObjects ??= FindObjectsOfTypeAll(
                    coreImage,
                    uiImage,
                    "UnityEngine.UI",
                    "RawImage");
                foreach (IntPtr rawImage in rawImageObjects)
                {
                    IntPtr gameObject = InvokeInstanceObject(
                        coreImage,
                        "UnityEngine",
                        "Component",
                        "get_gameObject",
                        rawImage);
                    if (gameObject == IntPtr.Zero)
                    {
                        continue;
                    }

                    string name = InvokeInstanceString(
                        coreImage,
                        "UnityEngine",
                        "Object",
                        "get_name",
                        gameObject);
                    if (name.Equals("3DTexture", StringComparison.Ordinal) &&
                        GetComponentPath(coreImage, rawImage).EndsWith(
                            "/LiveFullScreenRoot/VisbleRoot/3DTexture",
                            StringComparison.Ordinal))
                    {
                        threeDTexture = rawImage;
                        _cachedThreeDTextureRawImage = rawImage;
                        break;
                    }
                }
            }

            if (threeDTexture == IntPtr.Zero)
            {
                throw new InvalidOperationException("Live 3DTexture RawImage was not found.");
            }

            IntPtr uiCamera = _cachedUiCamera;
            if (uiCamera == IntPtr.Zero)
            {
                uiCamera = FindCameraByName(coreImage, "UICamera");
                _cachedUiCamera = uiCamera;
            }
            if (uiCamera == IntPtr.Zero)
            {
                throw new InvalidOperationException("UICamera was not found.");
            }

            stage = "create-render-texture";
            EnsureUiCaptureObjects(coreImage, width, height);
            IntPtr nativeTexture = InvokeInstanceIntPtr(
                coreImage,
                "UnityEngine",
                "Texture",
                "GetNativeTexturePtr",
                _uiCaptureRenderTexture);
            if (nativeTexture == IntPtr.Zero)
            {
                throw new InvalidOperationException("UI RenderTexture returned a null native pointer.");
            }

            stage = "render-request-ready";

            D3D11Texture2DDescription description = D3D11Interop.GetTextureDescription(nativeTexture);
            int viewFormat = description.Format switch
            {
                27 => 28,
                90 => 87,
                _ => description.Format
            };
            IntPtr renderTargetView = D3D11Interop.CreateRenderTargetView(
                D3D11DeviceCapture.Device,
                nativeTexture,
                viewFormat);
            try
            {
                D3D11Interop.ClearRenderTargetView(
                    D3D11DeviceCapture.Context,
                    renderTargetView,
                    new Color4());
            }
            finally
            {
                D3D11Interop.Release(renderTargetView);
            }

            IntPtr canvasRenderer = IntPtr.Zero;
            IntPtr canvasImage = IntPtr.Zero;
            if (SuppressLiveWorldDuringUiCapture)
            {
                stage = "suppress-3d-canvas-renderer";
                canvasRenderer = _cachedThreeDTextureCanvasRenderer;
                if (canvasRenderer == IntPtr.Zero)
                {
                    IntPtr uiImage = FindImage("UnityEngine.UI.dll");
                    canvasRenderer = InvokeInstanceObject(
                        uiImage,
                        "UnityEngine.UI",
                        "Graphic",
                        "get_canvasRenderer",
                        threeDTexture);
                    if (canvasRenderer == IntPtr.Zero)
                    {
                        throw new InvalidOperationException(
                            "3DTexture Graphic returned a null CanvasRenderer.");
                    }

                    _cachedThreeDTextureCanvasRenderer = canvasRenderer;
                }

                canvasImage = FindImage("UnityEngine.UIModule.dll");
                bool wasCulled = InvokeInstanceBool(
                    canvasImage,
                    "UnityEngine",
                    "CanvasRenderer",
                    "get_cull",
                    canvasRenderer);
                if (!wasCulled)
                {
                    InvokeInstanceBooleanSetter(
                        canvasImage,
                        "UnityEngine",
                        "CanvasRenderer",
                        "set_cull",
                        canvasRenderer,
                        true);
                    suppressed = true;
                }
            }

            try
            {
                stage = "submit-render-request";
                IntPtr submit = FindMethod(
                    coreImage,
                    "UnityEngine",
                    "Camera",
                    "SubmitRenderRequestsInternal",
                    1);
                _ = InvokeWithObjectArgument(submit, uiCamera, _uiCaptureRequest);
            }
            finally
            {
                if (suppressed && canvasImage != IntPtr.Zero && canvasRenderer != IntPtr.Zero)
                {
                    InvokeInstanceBooleanSetter(
                        canvasImage,
                        "UnityEngine",
                        "CanvasRenderer",
                        "set_cull",
                        canvasRenderer,
                        false);
                    _ = Invoke(
                        FindMethod(
                            canvasImage,
                            "UnityEngine",
                            "Canvas",
                            "ForceUpdateCanvases"),
                        IntPtr.Zero);
                    restored = true;
                }
            }

            stage = "complete";
            UnityRenderSourceRegistry.UpdateLiveUiTexture(nativeTexture, "UICamera/UI-only");
            _uiCaptureFrameCount++;
            if (_uiCaptureFrameCount == 1 || now >= _nextUiCaptureLogUtc)
            {
                RuntimeProbe.Append(_logPath, new ProbeEvent
                {
                    TimestampUtc = now,
                    Event = "ui-capture-submitted",
                    BootstrapVersion = RuntimeProbe.BootstrapVersion,
                    ProcessId = Environment.ProcessId,
                    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                    UiCaptureStage = stage,
                    UiCaptureSubmitted = true,
                    UiCaptureRawImageSuppressed = suppressed,
                    UiCaptureRawImageRestored = restored,
                    UiCaptureDestinationVerified = _uiCaptureDestinationVerified,
                    UiCaptureRequestSupported = requestSupported,
                    UiCaptureWidth = width,
                    UiCaptureHeight = height,
                    UiCaptureTextureDescription = D3D11Interop.DescribeTexture(nativeTexture),
                    UiCaptureFrameCount = _uiCaptureFrameCount
                });
                _nextUiCaptureLogUtc = now.AddSeconds(10);
            }
        }
        catch (Exception exception)
        {
            _cachedThreeDTextureRawImage = IntPtr.Zero;
            _cachedThreeDTextureCanvasRenderer = IntPtr.Zero;
            _cachedUiCamera = IntPtr.Zero;
            _nextUiCaptureMilliseconds = Environment.TickCount64 + 5_000;
            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = now,
                Event = "ui-capture-failure",
                BootstrapVersion = RuntimeProbe.BootstrapVersion,
                ProcessId = Environment.ProcessId,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                ErrorType = exception.GetType().FullName,
                Error = exception.Message,
                UiCaptureStage = stage,
                UiCaptureSubmitted = false,
                UiCaptureRawImageSuppressed = suppressed,
                UiCaptureRawImageRestored = restored,
                UiCaptureDestinationVerified = _uiCaptureDestinationVerified,
                UiCaptureRequestSupported = requestSupported,
                UiCaptureWidth = width,
                UiCaptureHeight = height,
                UiCaptureFrameCount = _uiCaptureFrameCount
            });
        }
        finally
        {
            _uiCaptureInProgress = false;
        }
    }

    private Il2CppMethodProbeRecord CreateMethodProbe(IntPtr method)
    {
        uint parameterCount = _api.MethodGetParamCount(method);
        List<string> parameterTypes = new(checked((int)parameterCount));
        for (uint index = 0; index < parameterCount; index++)
        {
            parameterTypes.Add(GetTypeName(_api.MethodGetParam(method, index)));
        }

        uint flags = _api.MethodGetFlags(method, out _);
        IntPtr nativeMethodPointer = Marshal.ReadIntPtr(method, 0);
        IntPtr virtualMethodPointer = Marshal.ReadIntPtr(method, IntPtr.Size);
        IntPtr invokerPointer = Marshal.ReadIntPtr(method, 2 * IntPtr.Size);
        return new Il2CppMethodProbeRecord
        {
            Name = GetNativeName(method, _api.MethodGetName),
            ParameterCount = parameterCount,
            ReturnType = GetTypeName(_api.MethodGetReturnType(method)),
            ParameterTypes = parameterTypes,
            Flags = flags,
            IsStatic = (flags & 0x0010) != 0,
            NativeMethodPointer = $"0x{nativeMethodPointer.ToInt64():x}",
            VirtualMethodPointer = $"0x{virtualMethodPointer.ToInt64():x}",
            InvokerPointer = $"0x{invokerPointer.ToInt64():x}",
            NativeMethodModule = FindModuleForAddress(nativeMethodPointer),
            VirtualMethodModule = FindModuleForAddress(virtualMethodPointer),
            InvokerModule = FindModuleForAddress(invokerPointer)
        };
    }

    private static string FindModuleForAddress(IntPtr address)
    {
        if (address == IntPtr.Zero)
        {
            return "null";
        }

        try
        {
            ulong value = unchecked((ulong)address.ToInt64());
            foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
            {
                ulong start = unchecked((ulong)module.BaseAddress.ToInt64());
                ulong end = checked(start + (uint)module.ModuleMemorySize);
                if (value >= start && value < end)
                {
                    return module.ModuleName ?? "unknown-module";
                }
            }
        }
        catch
        {
            // Address ownership is diagnostic-only; the raw pointer remains useful.
        }

        return "unmapped";
    }

    private string GetTypeName(IntPtr type)
    {
        if (type == IntPtr.Zero)
        {
            return string.Empty;
        }

        IntPtr name = _api.TypeGetName(type);
        if (name == IntPtr.Zero)
        {
            return string.Empty;
        }

        try
        {
            return Marshal.PtrToStringUTF8(name) ?? string.Empty;
        }
        finally
        {
            _api.Free(name);
        }
    }

    private void EnsureUiCaptureObjects(IntPtr coreImage, int width, int height)
    {
        if (_uiCaptureRequest != IntPtr.Zero && _uiCaptureRenderTexture != IntPtr.Zero)
        {
            return;
        }

        IntPtr renderTextureClass = FindClass(
            coreImage,
            "UnityEngine",
            "RenderTexture");
        _uiCaptureRenderTexture = _api.ObjectNew(renderTextureClass);
        if (_uiCaptureRenderTexture == IntPtr.Zero)
        {
            throw new InvalidOperationException("Could not allocate the UI RenderTexture.");
        }

        _uiCaptureRenderTextureHandle = _api.GcHandleNew(_uiCaptureRenderTexture, pinned: false);
        if (_uiCaptureRenderTextureHandle == 0)
        {
            throw new InvalidOperationException("Could not root the UI RenderTexture.");
        }

        IntPtr constructor = FindMethod(
            coreImage,
            "UnityEngine",
            "RenderTexture",
            ".ctor",
            3);
        _ = InvokeWithObjectArguments(
            constructor,
            _uiCaptureRenderTexture,
            BoxInt32(FindImage("mscorlib.dll"), width),
            BoxInt32(FindImage("mscorlib.dll"), height),
            BoxInt32(FindImage("mscorlib.dll"), 0));
        _ = Invoke(
            FindMethod(coreImage, "UnityEngine", "RenderTexture", "Create"),
            _uiCaptureRenderTexture);

        IntPtr universalImage = FindImage("Unity.RenderPipelines.Universal.Runtime.dll");
        IntPtr requestClass = FindClassBySimpleName(
            universalImage,
            "SingleCameraRequest");
        _uiCaptureRequest = _api.ObjectNew(requestClass);
        if (_uiCaptureRequest == IntPtr.Zero)
        {
            throw new InvalidOperationException("Could not allocate SingleCameraRequest.");
        }

        _uiCaptureRequestHandle = _api.GcHandleNew(_uiCaptureRequest, pinned: false);
        if (_uiCaptureRequestHandle == 0)
        {
            throw new InvalidOperationException("Could not root SingleCameraRequest.");
        }

        _ = Invoke(
            FindMethod(requestClass, ".ctor"),
            _uiCaptureRequest);
        IntPtr destination = FindField(requestClass, "destination");
        IntPtr value = Marshal.AllocHGlobal(IntPtr.Size);
        try
        {
            int destinationOffset = _api.FieldGetOffset(destination);
            if (destinationOffset < 2 * IntPtr.Size || destinationOffset > 4_096)
            {
                throw new InvalidOperationException(
                    $"Unexpected SingleCameraRequest.destination offset: {destinationOffset}.");
            }

            _api.GcWriteBarrierSetField(
                _uiCaptureRequest,
                IntPtr.Add(_uiCaptureRequest, destinationOffset),
                _uiCaptureRenderTexture);
            Marshal.WriteIntPtr(value, IntPtr.Zero);
            _api.FieldGetValue(_uiCaptureRequest, destination, value);
            _uiCaptureDestinationVerified =
                Marshal.ReadIntPtr(value) == _uiCaptureRenderTexture;
            if (!_uiCaptureDestinationVerified)
            {
                throw new InvalidOperationException(
                    "SingleCameraRequest.destination did not retain the UI RenderTexture.");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(value);
        }
    }

    private IntPtr FindCameraByName(IntPtr coreImage, string wantedName)
    {
        IntPtr cameraArray = Invoke(
            FindMethod(coreImage, "UnityEngine", "Camera", "get_allCameras"),
            IntPtr.Zero);
        foreach (IntPtr camera in ReadObjectArray(cameraArray, 64))
        {
            string name = InvokeInstanceString(
                coreImage,
                "UnityEngine",
                "Object",
                "get_name",
                camera);
            if (name.Equals(wantedName, StringComparison.Ordinal))
            {
                return camera;
            }
        }

        return IntPtr.Zero;
    }

    private IntPtr FindField(IntPtr klass, string wantedName)
    {
        IntPtr iterator = IntPtr.Zero;
        for (int index = 0; index < 256; index++)
        {
            IntPtr field = _api.ClassGetFields(klass, ref iterator);
            if (field == IntPtr.Zero)
            {
                break;
            }

            if (GetNativeName(field, _api.FieldGetName).Equals(
                wantedName,
                StringComparison.Ordinal))
            {
                return field;
            }
        }

        throw new MissingFieldException($"IL2CPP field not found: {wantedName}");
    }

    private IntPtr FindClassBySimpleName(IntPtr image, string wantedName)
    {
        ulong classCount = _api.ImageGetClassCount(image).ToUInt64();
        if (classCount > 16_384)
        {
            throw new InvalidOperationException($"Unexpected IL2CPP class count: {classCount}.");
        }

        for (ulong index = 0; index < classCount; index++)
        {
            IntPtr klass = _api.ImageGetClass(image, new UIntPtr(index));
            if (klass != IntPtr.Zero && GetNativeName(klass, _api.ClassGetName).Equals(
                wantedName,
                StringComparison.Ordinal))
            {
                return klass;
            }
        }

        throw new MissingMemberException($"IL2CPP class not found by simple name: {wantedName}");
    }

    private IntPtr FindClassAcrossImages(string namespaze, string className)
    {
        IntPtr assemblies = _api.DomainGetAssemblies(_domain, out UIntPtr rawCount);
        ulong count = rawCount.ToUInt64();
        if (count > 4_096)
        {
            throw new InvalidOperationException($"Unexpected IL2CPP assembly count: {count}.");
        }

        using Utf8String nativeNamespace = new(namespaze);
        using Utf8String nativeClass = new(className);
        for (ulong index = 0; index < count; index++)
        {
            IntPtr assembly = Marshal.ReadIntPtr(
                assemblies,
                checked((int)(index * (ulong)IntPtr.Size)));
            IntPtr image = assembly == IntPtr.Zero
                ? IntPtr.Zero
                : _api.AssemblyGetImage(assembly);
            IntPtr klass = image == IntPtr.Zero
                ? IntPtr.Zero
                : _api.ClassFromName(
                    image,
                    nativeNamespace.Pointer,
                    nativeClass.Pointer);
            if (klass != IntPtr.Zero)
            {
                return klass;
            }
        }

        throw new MissingMemberException(
            $"IL2CPP class not found across loaded images: {namespaze}.{className}");
    }

    private IntPtr FindFieldInHierarchy(IntPtr klass, string wantedName)
    {
        IntPtr current = klass;
        for (int depth = 0; depth < 16 && current != IntPtr.Zero; depth++)
        {
            IntPtr field = TryFindField(current, wantedName);
            if (field != IntPtr.Zero)
            {
                return field;
            }
            current = _api.ClassGetParent(current);
        }

        throw new MissingFieldException(
            $"IL2CPP field not found in class hierarchy: {wantedName}");
    }

    private IntPtr GetManagedType(IntPtr klass)
    {
        return (_api.TypeGetObject ??
            throw new MissingMethodException(
                "GameAssembly.dll does not export il2cpp_type_get_object."))(
                (_api.ClassGetType ??
                    throw new MissingMethodException(
                        "GameAssembly.dll does not export il2cpp_class_get_type."))(
                    klass));
    }

    private IntPtr BoxInt32(IntPtr mscorlibImage, int value)
    {
        IntPtr valueAddress = Marshal.AllocHGlobal(sizeof(int));
        try
        {
            Marshal.WriteInt32(valueAddress, value);
            return _api.ValueBox(
                FindClass(mscorlibImage, "System", "Int32"),
                valueAddress);
        }
        finally
        {
            Marshal.FreeHGlobal(valueAddress);
        }
    }

    private IntPtr BoxBoolean(IntPtr mscorlibImage, bool value)
    {
        IntPtr valueAddress = Marshal.AllocHGlobal(1);
        try
        {
            Marshal.WriteByte(valueAddress, value ? (byte)1 : (byte)0);
            return _api.ValueBox(
                FindClass(mscorlibImage, "System", "Boolean"),
                valueAddress);
        }
        finally
        {
            Marshal.FreeHGlobal(valueAddress);
        }
    }

    private IntPtr BoxSingle(IntPtr mscorlibImage, float value)
    {
        IntPtr valueAddress = Marshal.AllocHGlobal(sizeof(float));
        try
        {
            Marshal.StructureToPtr(value, valueAddress, fDeleteOld: false);
            return _api.ValueBox(
                FindClass(mscorlibImage, "System", "Single"),
                valueAddress);
        }
        finally
        {
            Marshal.FreeHGlobal(valueAddress);
        }
    }

    private IntPtr BoxVector3(IntPtr coreImage, float x, float y, float z)
    {
        UiReplayVector3 vector = new() { X = x, Y = y, Z = z };
        IntPtr valueAddress = Marshal.AllocHGlobal(Marshal.SizeOf<UiReplayVector3>());
        try
        {
            Marshal.StructureToPtr(vector, valueAddress, fDeleteOld: false);
            return _api.ValueBox(
                FindClass(coreImage, "UnityEngine", "Vector3"),
                valueAddress);
        }
        finally
        {
            Marshal.FreeHGlobal(valueAddress);
        }
    }

    private IntPtr BoxColor(
        IntPtr coreImage,
        float red,
        float green,
        float blue,
        float alpha)
    {
        UiReplayColor color = new()
        {
            Red = red,
            Green = green,
            Blue = blue,
            Alpha = alpha
        };
        IntPtr valueAddress = Marshal.AllocHGlobal(Marshal.SizeOf<UiReplayColor>());
        try
        {
            Marshal.StructureToPtr(color, valueAddress, fDeleteOld: false);
            return _api.ValueBox(
                FindClass(coreImage, "UnityEngine", "Color"),
                valueAddress);
        }
        finally
        {
            Marshal.FreeHGlobal(valueAddress);
        }
    }

    private IntPtr NewManagedString(string value)
    {
        using Utf8String native = new(value);
        IntPtr managed = _api.StringNew(native.Pointer);
        return managed == IntPtr.Zero
            ? throw new InvalidOperationException("il2cpp_string_new returned null.")
            : managed;
    }

    private void InvokeInstanceBooleanSetter(
        IntPtr image,
        string namespaze,
        string className,
        string methodName,
        IntPtr instance,
        bool value)
    {
        IntPtr boxed = BoxBoolean(FindImage("mscorlib.dll"), value);
        _ = InvokeWithObjectArgument(
            FindMethod(image, namespaze, className, methodName, 1),
            instance,
            boxed);
    }

    private static string GetNativeName<TDelegate>(IntPtr value, TDelegate getter)
        where TDelegate : Delegate
    {
        if (value == IntPtr.Zero)
        {
            return string.Empty;
        }

        IntPtr namePointer = getter switch
        {
            Il2CppApi.ClassGetNameDelegate classGetter => classGetter(value),
            Il2CppApi.ClassGetNamespaceDelegate namespaceGetter => namespaceGetter(value),
            Il2CppApi.FieldGetNameDelegate fieldGetter => fieldGetter(value),
            Il2CppApi.MethodGetNameDelegate methodGetter => methodGetter(value),
            _ => throw new ArgumentException("Unsupported IL2CPP name getter.", nameof(getter))
        };
        return namePointer == IntPtr.Zero
            ? string.Empty
            : Marshal.PtrToStringUTF8(namePointer) ?? string.Empty;
    }

    private IReadOnlyList<IntPtr> ReadObjectArray(IntPtr array, int maximumLength)
    {
        if (array == IntPtr.Zero)
        {
            return Array.Empty<IntPtr>();
        }

        ulong rawLength = unchecked((ulong)Marshal.ReadInt64(array, 3 * IntPtr.Size));
        if (rawLength > checked((ulong)maximumLength))
        {
            throw new InvalidOperationException($"Unexpected Unity object array length: {rawLength}.");
        }

        List<IntPtr> values = new(checked((int)rawLength));
        int vectorOffset = 4 * IntPtr.Size;
        for (ulong index = 0; index < rawLength; index++)
        {
            IntPtr value = Marshal.ReadIntPtr(array, checked(vectorOffset + ((int)index * IntPtr.Size)));
            if (value != IntPtr.Zero)
            {
                values.Add(value);
            }
        }

        return values;
    }

    private string GetComponentPath(IntPtr coreImage, IntPtr component)
    {
        IntPtr transform = InvokeInstanceObject(
            coreImage,
            "UnityEngine",
            "Component",
            "get_transform",
            component);
        List<string> names = new();
        for (int depth = 0; transform != IntPtr.Zero && depth < 32; depth++)
        {
            names.Add(InvokeInstanceString(
                coreImage,
                "UnityEngine",
                "Object",
                "get_name",
                transform));
            transform = InvokeInstanceObject(
                coreImage,
                "UnityEngine",
                "Transform",
                "get_parent",
                transform);
        }

        names.Reverse();
        return string.Join("/", names);
    }

    private int InvokeInstanceInt(
        IntPtr image,
        string namespaze,
        string className,
        string methodName,
        IntPtr instance)
    {
        IntPtr boxed = Invoke(FindMethod(image, namespaze, className, methodName), instance);
        IntPtr value = boxed == IntPtr.Zero ? IntPtr.Zero : _api.ObjectUnbox(boxed);
        return value == IntPtr.Zero
            ? throw new InvalidOperationException($"{className}.{methodName} returned null.")
            : Marshal.ReadInt32(value);
    }

    private bool InvokeInstanceBool(
        IntPtr image,
        string namespaze,
        string className,
        string methodName,
        IntPtr instance) =>
        InvokeInstanceInt(image, namespaze, className, methodName, instance) != 0;

    private float InvokeInstanceFloat(
        IntPtr image,
        string namespaze,
        string className,
        string methodName,
        IntPtr instance)
    {
        IntPtr boxed = Invoke(FindMethod(image, namespaze, className, methodName), instance);
        IntPtr value = boxed == IntPtr.Zero ? IntPtr.Zero : _api.ObjectUnbox(boxed);
        return value == IntPtr.Zero
            ? throw new InvalidOperationException($"{className}.{methodName} returned null.")
            : Marshal.PtrToStructure<float>(value);
    }

    private IntPtr InvokeInstanceIntPtr(
        IntPtr image,
        string namespaze,
        string className,
        string methodName,
        IntPtr instance)
    {
        IntPtr boxed = Invoke(FindMethod(image, namespaze, className, methodName), instance);
        IntPtr value = boxed == IntPtr.Zero ? IntPtr.Zero : _api.ObjectUnbox(boxed);
        return value == IntPtr.Zero
            ? throw new InvalidOperationException($"{className}.{methodName} returned null.")
            : Marshal.ReadIntPtr(value);
    }

    private IntPtr InvokeInstanceObject(
        IntPtr image,
        string namespaze,
        string className,
        string methodName,
        IntPtr instance) =>
        Invoke(FindMethod(image, namespaze, className, methodName), instance);

    private string InvokeInstanceString(
        IntPtr image,
        string namespaze,
        string className,
        string methodName,
        IntPtr instance)
    {
        IntPtr managedString = Invoke(FindMethod(image, namespaze, className, methodName), instance);
        if (managedString == IntPtr.Zero)
        {
            return string.Empty;
        }

        int length = _api.StringLength(managedString);
        IntPtr characters = _api.StringChars(managedString);
        return length <= 0 || characters == IntPtr.Zero
            ? string.Empty
            : Marshal.PtrToStringUni(characters, length) ?? string.Empty;
    }

    private IntPtr FindClass(IntPtr image, string namespaze, string className)
    {
        using Utf8String nativeNamespace = new(namespaze);
        using Utf8String nativeClass = new(className);
        IntPtr klass = _api.ClassFromName(image, nativeNamespace.Pointer, nativeClass.Pointer);
        return klass == IntPtr.Zero
            ? throw new MissingMemberException($"IL2CPP class not found: {namespaze}.{className}")
            : klass;
    }

    private IntPtr FindMethod(
        IntPtr image,
        string namespaze,
        string className,
        string methodName,
        int argumentCount = 0)
    {
        IntPtr klass = FindClass(image, namespaze, className);
        using Utf8String nativeMethod = new(methodName);
        IntPtr method = _api.ClassGetMethodFromName(klass, nativeMethod.Pointer, argumentCount);
        return method == IntPtr.Zero
            ? throw new MissingMethodException(
                $"IL2CPP method not found: {namespaze}.{className}.{methodName}/{argumentCount}")
            : method;
    }

    private IntPtr FindMethod(IntPtr klass, string methodName, int argumentCount = 0)
    {
        IntPtr method = TryFindMethod(klass, methodName, argumentCount);
        return method == IntPtr.Zero
            ? throw new MissingMethodException(
                $"IL2CPP method not found on class pointer {klass}: {methodName}/{argumentCount}")
            : method;
    }

    private IntPtr FindMethodBySignature(
        IntPtr klass,
        string methodName,
        params string[] parameterTypes)
    {
        IntPtr iterator = IntPtr.Zero;
        for (int index = 0; index < 1_024; index++)
        {
            IntPtr method = _api.ClassGetMethods(klass, ref iterator);
            if (method == IntPtr.Zero)
            {
                break;
            }

            if (!GetNativeName(method, _api.MethodGetName).Equals(
                    methodName,
                    StringComparison.Ordinal) ||
                _api.MethodGetParamCount(method) != parameterTypes.Length)
            {
                continue;
            }

            bool matches = true;
            for (int parameterIndex = 0; parameterIndex < parameterTypes.Length; parameterIndex++)
            {
                if (!GetTypeName(_api.MethodGetParam(method, checked((uint)parameterIndex))).Equals(
                        parameterTypes[parameterIndex],
                        StringComparison.Ordinal))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                return method;
            }
        }

        throw new MissingMethodException(
            $"IL2CPP method not found on class pointer {klass}: " +
            $"{methodName}({string.Join(",", parameterTypes)})");
    }

    private IntPtr TryFindMethod(IntPtr klass, string methodName, int argumentCount = 0)
    {
        if (klass == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        using Utf8String nativeMethod = new(methodName);
        return _api.ClassGetMethodFromName(klass, nativeMethod.Pointer, argumentCount);
    }

    private IntPtr TryFindMethodInHierarchy(
        IntPtr klass,
        string methodName,
        int argumentCount = 0)
    {
        IntPtr current = klass;
        for (int depth = 0; depth < 32 && current != IntPtr.Zero; depth++)
        {
            IntPtr method = TryFindMethod(current, methodName, argumentCount);
            if (method != IntPtr.Zero)
            {
                return method;
            }
            current = _api.ClassGetParent(current);
        }

        return IntPtr.Zero;
    }

    private IntPtr TryInvokeObject(IntPtr klass, string methodName, IntPtr instance)
    {
        IntPtr method = TryFindMethod(klass, methodName);
        return method == IntPtr.Zero ? IntPtr.Zero : Invoke(method, instance);
    }

    private int? TryInvokeInt(IntPtr klass, string methodName, IntPtr instance)
    {
        IntPtr method = TryFindMethod(klass, methodName);
        if (method == IntPtr.Zero)
        {
            return null;
        }

        IntPtr boxed = Invoke(method, instance);
        IntPtr value = boxed == IntPtr.Zero ? IntPtr.Zero : _api.ObjectUnbox(boxed);
        return value == IntPtr.Zero ? null : Marshal.ReadInt32(value);
    }

    private bool? TryInvokeBool(IntPtr klass, string methodName, IntPtr instance)
    {
        int? value = TryInvokeInt(klass, methodName, instance);
        return value.HasValue ? value.Value != 0 : null;
    }

    private IntPtr Invoke(IntPtr method, IntPtr instance)
    {
        IntPtr result = _api.RuntimeInvoke(method, instance, IntPtr.Zero, out IntPtr exception);
        if (exception != IntPtr.Zero)
        {
            throw new InvalidOperationException(DescribeIl2CppException(exception));
        }

        return result;
    }

    private IntPtr InvokeWithObjectArgument(IntPtr method, IntPtr instance, IntPtr argument)
        => InvokeWithObjectArguments(method, instance, argument);

    private IntPtr InvokeWithObjectArguments(
        IntPtr method,
        IntPtr instance,
        params IntPtr[] arguments)
    {
        Il2CppApi.RuntimeInvokeConvertArgsDelegate invokeConvertArgs =
            _api.RuntimeInvokeConvertArgs ?? throw new MissingMethodException(
                "GameAssembly.dll does not export il2cpp_runtime_invoke_convert_args.");
        IntPtr argumentArray = Marshal.AllocHGlobal(checked(IntPtr.Size * arguments.Length));
        try
        {
            for (int index = 0; index < arguments.Length; index++)
            {
                Marshal.WriteIntPtr(argumentArray, index * IntPtr.Size, arguments[index]);
            }

            IntPtr result = invokeConvertArgs(
                method,
                instance,
                argumentArray,
                arguments.Length,
                out IntPtr exception);
            if (exception != IntPtr.Zero)
            {
                throw new InvalidOperationException(DescribeIl2CppException(exception));
            }

            return result;
        }
        finally
        {
            Marshal.FreeHGlobal(argumentArray);
        }
    }

    private string DescribeIl2CppException(IntPtr exception)
    {
        const int bufferSize = 8_192;
        IntPtr message = Marshal.AllocHGlobal(bufferSize);
        IntPtr stack = Marshal.AllocHGlobal(bufferSize);
        try
        {
            Marshal.WriteByte(message, 0);
            Marshal.WriteByte(stack, 0);
            _api.FormatException(exception, message, bufferSize);
            _api.FormatStackTrace(exception, stack, bufferSize);
            string formattedMessage = Marshal.PtrToStringUTF8(message) ?? string.Empty;
            string formattedStack = Marshal.PtrToStringUTF8(stack) ?? string.Empty;
            return string.IsNullOrWhiteSpace(formattedStack)
                ? formattedMessage
                : $"{formattedMessage} | stack: {formattedStack}";
        }
        finally
        {
            Marshal.FreeHGlobal(stack);
            Marshal.FreeHGlobal(message);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int FrameCountDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate byte DrawFlareDelegate(
        IntPtr instance,
        IntPtr commandBuffer,
        IntPtr material,
        IntPtr flareSetting,
        float width,
        float height,
        IntPtr methodInfo);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void VlPostProcessRenderDelegate(
        IntPtr instance,
        IntPtr commandBuffer,
        IntPtr renderingData,
        IntPtr methodInfo);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SetupVlBloomDelegate(
        IntPtr instance,
        IntPtr commandBuffer,
        IntPtr source,
        IntPtr bloom,
        IntPtr starStreak,
        IntPtr methodInfo);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void DrawStarStreakDelegate(
        IntPtr instance,
        IntPtr commandBuffer,
        IntPtr source,
        IntPtr destination,
        IntPtr starStreak,
        IntPtr methodInfo);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate byte DoVlDofDelegate(
        IntPtr instance,
        IntPtr commandBuffer,
        IntPtr source,
        IntPtr destination,
        IntPtr camera,
        IntPtr methodInfo);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate byte DoVlTextureBlurDelegate(
        IntPtr instance,
        IntPtr commandBuffer,
        IntPtr source,
        IntPtr destination,
        IntPtr methodInfo);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int DobbyHookDelegate(
        IntPtr target,
        IntPtr replacement,
        out IntPtr original);

    private sealed class CameraUrpProbeData
    {
        public static CameraUrpProbeData Empty { get; } = new();

        public bool Present { get; init; }
        public int? RendererIndex { get; init; }
        public int? RenderType { get; init; }
        public bool? RenderPostProcessing { get; init; }
        public bool? RequiresDepthTexture { get; init; }
        public bool? RequiresColorTexture { get; init; }
        public int? CameraStackCount { get; init; }
        public IReadOnlyList<string>? CameraStackNames { get; init; }
    }

    private sealed class M5MediaClass
    {
        public string AssemblyName { get; init; } = string.Empty;
        public string Namespace { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public IntPtr Class { get; init; }
        public bool IsBehaviour { get; init; }
    }

    private sealed class Utf8String : IDisposable
    {
        public Utf8String(string value)
        {
            Pointer = Marshal.StringToCoTaskMemUTF8(value);
        }

        public IntPtr Pointer { get; }

        public void Dispose() => Marshal.FreeCoTaskMem(Pointer);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct UiReplayColor
    {
        public float Red;
        public float Green;
        public float Blue;
        public float Alpha;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct UiReplayVector3
    {
        public float X;
        public float Y;
        public float Z;
    }

    private sealed class UiReplayCanvas
    {
        public string Path { get; init; } = string.Empty;
        public int SortingOrder { get; init; }
        public int RenderOrder { get; init; }
    }

    private sealed class UiReplayDraw
    {
        public IntPtr Graphic { get; init; }
        public IntPtr CanvasRenderer { get; init; }
        public IntPtr RuntimeClass { get; init; }
        public string Path { get; init; } = string.Empty;
        public string TypeName { get; init; } = string.Empty;
        public int SortingOrder { get; init; }
        public int RenderOrder { get; init; }
        public int AbsoluteDepth { get; init; }
        public int MaterialCount { get; init; }
    }
}
