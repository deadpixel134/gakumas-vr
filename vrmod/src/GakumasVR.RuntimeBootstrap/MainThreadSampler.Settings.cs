using System.Runtime.InteropServices;
using GakumasVR.Core;

namespace Doorstop;

internal sealed partial class MainThreadSampler
{
    private bool _worldGripEnabled;
    private TrackingVector3 _worldGripOffset;
    private DateTimeOffset _nextWorldGripLogUtc;

    private void ApplyWorldGripMotion(DateTimeOffset now, TrackingVector3 handDelta,
        float headTranslationScale, TrackingQuaternion navigationWorldRotation)
    {
        if (headTranslationScale <= 0f || (handDelta.X == 0f && handDelta.Y == 0f && handDelta.Z == 0f) ||
            !_sixDofPoseMapper.TryMapWorldGripDelta(handDelta, headTranslationScale, out TrackingVector3 localDelta))
            return;

        TrackingVector3 delta = RotateTrackingVector(navigationWorldRotation, localDelta);
        TrackingVector3 offset = AddTrackingPosition(_worldGripOffset, delta);
        if (!float.IsFinite(offset.X) || !float.IsFinite(offset.Y) || !float.IsFinite(offset.Z)) return;

        _worldGripOffset = offset;
        if (now >= _nextWorldGripLogUtc)
        {
            _nextWorldGripLogUtc = now.AddSeconds(1);
            RuntimeProbe.Append(_logPath, new ProbeEvent
            {
                TimestampUtc = now,
                Event = "controller-world-grip-applied",
                ProcessId = Environment.ProcessId,
                Reason = $"headScale={headTranslationScale:R};delta={delta.X:R},{delta.Y:R},{delta.Z:R};offset={offset.X:R},{offset.Y:R},{offset.Z:R}"
            });
        }
    }

    private void ApplyLiveSettings(DateTimeOffset now, IntPtr coreImage)
    {
        (VrSettings settings, long revision) = VrSettingsRuntime.SnapshotSettings();
        if (_settingsRevision == revision)
        {
            VrSettingsRuntime.MarkApplied(revision);
            return;
        }
        bool rebuild = _stereoRenderResolutionScale != settings.Render.EyeRenderScale;
        string previousMode = _stereoVisualEffectMode;
        _stereoVisualEffectMode = settings.Render.VisualEffectMode;
        _manualVisualEffects = settings.Render.ManualVisualEffects;
        _stereoRenderResolutionScale = settings.Render.EyeRenderScale;
        _stereoWorldEyeOffsetScale = settings.Render.WorldEyeOffsetScale;
        _liveSpatialProfile = settings.Spatial.Live;
        _nonLiveSpatialProfile = settings.Spatial.NonLive;
        _stereoSpatialMultipliers = SpatialScaleResolver.Resolve(
            _stereoSpatialProfileKind == StereoSpatialProfileKind.NonLive ? _nonLiveSpatialProfile : _liveSpatialProfile);
        _liveSixDofEnabled = settings.Tracking.LiveSixDofEnabled;
        _locomotionEnabled = settings.Tracking.LocomotionEnabled;
        _worldGripEnabled = settings.Tracking.WorldGripEnabled;
        _showContactHands = settings.Tracking.ShowContactHands;
#if !GAKUMAS_PUBLIC
        bool contactChanged = _contactCollisionEnabled != settings.Tracking.ContactCollisionEnabled;
        _contactCollisionEnabled = settings.Tracking.ContactCollisionEnabled;
        SetContactBoneTrialEnabled(settings.Tracking.ContactBoneTrialEnabled);
        if (contactChanged) RetireContactTrial();
#endif
        _locomotionHand = settings.Tracking.LocomotionHand;
        _locomotionSpeed = settings.Tracking.LocomotionSpeed;
        _viewTurnMode = settings.Tracking.ViewTurnMode;
        _viewTurnSpeed = settings.Tracking.ViewTurnSpeed;
        _viewSnapAngleDegrees = settings.Tracking.ViewSnapAngleDegrees;
        if (rebuild && _stereoCloneSetupReady)
        {
            RetireStereoCameraGeneration(now, coreImage, _navigationScene);
            _lastSignature = string.Empty;
        }
        else if (_stereoCloneSetupReady)
        {
            _stereoVisualEffectOverrideFailed = false;
            _stereoVisualEffectFallback = false;
            SetStereoPostProcessing(_stereoLeftAdditionalCameraData, true);
            SetStereoPostProcessing(_stereoRightAdditionalCameraData, true);
            TryConfigureStereoVisualEffectOverride(now);
        }
        _settingsRevision = revision;
        VrSettingsRuntime.MarkApplied(revision);
        RuntimeProbe.Append(_logPath, new ProbeEvent
        {
            TimestampUtc = now, Event = "vr-settings-live-applied",
            StereoVisualEffectMode = _stereoVisualEffectMode,
            Reason = $"revision={revision};previous={previousMode};rebuild={rebuild};runtime.enabled and panel.startEnabled apply next session."
        });
    }

    private float ReadProjectionY(IntPtr camera)
    {
        try
        {
            if (_projectionGetter == IntPtr.Zero)
            {
                IntPtr cameraClass = FindClass(FindImage("UnityEngine.CoreModule.dll"), "UnityEngine", "Camera");
                _projectionGetter = FindMethod(cameraClass, "get_projectionMatrix", 0);
            }
            IntPtr value = Invoke(_projectionGetter, camera);
            IntPtr matrix = value == IntPtr.Zero ? IntPtr.Zero : _api.ObjectUnbox(value);
            if (matrix == IntPtr.Zero) return float.NaN;
            float m11 = Marshal.PtrToStructure<float>(IntPtr.Add(matrix, 5 * sizeof(float)));
            return float.IsFinite(m11) && m11 > 0f ? m11 : float.NaN;
        }
        catch { return float.NaN; }
    }
}
