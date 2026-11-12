using System.Numerics;
using GakumasVR.Core;

namespace Doorstop;

internal sealed partial class MainThreadSampler
{
    private const float CalfContactReleaseMarginMeters = .004f;
    private bool _contactCollisionEnabled;
    private long _contactCollisionPair, _contactCollisionTick;
    private float _contactCollisionScale;
    private Vector3? _contactPreviousLeft, _contactPreviousRight;
    private TrackingVector3? _contactPreviousRawLeft, _contactPreviousRawRight;
    private long _contactPreviousRawTick;
    private readonly ContactEnterLatch _contactLeftLatch = new(), _contactRightLatch = new();
    private IntPtr _contactInverseVector;
    private DateTimeOffset _nextContactCollisionLog;
    private int _contactCollisionMask, _contactCollisionValid, _contactCollisionPulse;
    private long _contactCollisionSample;
    private Vector3? _contactPairLeft, _contactPairRight;

    private void ResetContactCollision()
    {
        _contactCollisionTick = _contactPreviousRawTick = 0;
        _contactPreviousRawLeft = _contactPreviousRawRight = null;
        _contactPreviousLeft = _contactPreviousRight = null;
        _contactPairLeft = _contactPairRight = null;
        _contactLeftLatch.Reset(); _contactRightLatch.Reset();
        _contactCollisionMask = _contactCollisionValid = _contactCollisionPulse = 0;
        OpenXrContactHaptics.Clear();
        foreach (var target in _contactBoneTrialTargets)
        {
            target.ResetContactMotion(); target.PreviousCenter = null; target.PairOffset = null;
            target.TouchMask = target.PreviousTouchMask = target.ReleaseMask = 0; target.PairCenter = target.PairBone = target.PairSurface = null;
        }
    }

    private void PrepareContactCollisionPair(OpenXrStereoStateSnapshot stereo,
        TrackingVector3 leftEye, TrackingVector3 rightEye, TrackingQuaternion leftRotation, TrackingQuaternion rightRotation)
    {
        if (!_contactCollisionEnabled) return;
        if (!_contactTrialPairPrepared || _contactTrialReadyTick == 0)
        {
            ResetContactCollision();
            return;
        }
        long now = Environment.TickCount64;
        VrTrackingSettings tracking = VrSettingsRuntime.Current.Tracking;
        bool fresh = now >= stereo.CapturedMilliseconds &&
            now - stereo.CapturedMilliseconds <= tracking.ContactHandHoldSeconds * 1000f;
        if (!fresh || !ContactHandAlignment.TryCreate(ToTrackingStereoPose(stereo), AsVector(leftEye), AsVector(rightEye),
            AsQuaternion(leftRotation), AsQuaternion(rightRotation), out var alignment))
        {
            ResetContactCollision();
            return;
        }
        try
        {
            if (_contactInverseVector == IntPtr.Zero)
                _contactInverseVector = FindMethodBySignature(FindClass(FindImage("UnityEngine.CoreModule.dll"),
                    "UnityEngine", "Transform"), "InverseTransformVector", "UnityEngine.Vector3");
            int allowed = stereo.ContactHands.AllowedMask;
            Vector3? left = (allowed & 1) != 0 && stereo.ContactHands.Left is TrackingVector3 l && alignment.TryMap(l, out var lp) ? lp : null;
            Vector3? right = (allowed & 2) != 0 && stereo.ContactHands.Right is TrackingVector3 r && alignment.TryMap(r, out var rp) ? rp : null;
            float seconds = (now - _contactCollisionTick) / 1000f;
            bool continuous = _contactCollisionTick != 0 && seconds is > 0 and <= 0.1f &&
                MathF.Abs(alignment.Scale - _contactCollisionScale) <= alignment.Scale * 0.02f;
            bool leftContinuous = continuous && ContinuousHand(left, _contactPreviousLeft, alignment.Scale);
            bool rightContinuous = continuous && ContinuousHand(right, _contactPreviousRight, alignment.Scale);
            int velocityMask = 0;
            Vector3 leftVelocity = default, rightVelocity = default;
            float rawSeconds = (stereo.CapturedMilliseconds - _contactPreviousRawTick) / 1000f;
            if (continuous && rawSeconds > 0 && rawSeconds <= tracking.ContactHandHoldSeconds)
            {
                if (leftContinuous && ContactHandVelocity(alignment, stereo.ContactHands.Left, _contactPreviousRawLeft, rawSeconds, out leftVelocity)) velocityMask |= 1;
                if (rightContinuous && ContactHandVelocity(alignment, stereo.ContactHands.Right, _contactPreviousRawRight, rawSeconds, out rightVelocity)) velocityMask |= 2;
            }
            if (stereo.CapturedMilliseconds != _contactPreviousRawTick)
            {
                _contactPreviousRawLeft = left.HasValue ? stereo.ContactHands.Left : null;
                _contactPreviousRawRight = right.HasValue ? stereo.ContactHands.Right : null;
                _contactPreviousRawTick = stereo.CapturedMilliseconds;
            }
            _contactPreviousLeft = left; _contactPreviousRight = right;
            _contactPairLeft = left; _contactPairRight = right;
            _contactCollisionTick = now; _contactCollisionScale = alignment.Scale;
            _contactCollisionPair++;
            int mask = 0;
            bool targetsContinuous = true;
            bool captureBounds = DateTimeOffset.UtcNow >= _nextContactCollisionLog;
            bool jiggle = tracking.ContactJiggleEnabled;
            ContactMaterialProfile thighMaterial = ContactMaterialProfile.Thigh(tracking);
            ContactMaterialProfile calfMaterial = ContactMaterialProfile.Calf();
            foreach (var target in _contactTrialPairTargets)
            {
                target.PairOffset = Vector3.Zero;
                if (target.Invalid || (target.RequiresMeasuredSurface && !target.Surface.HasValue)) continue;
                if (!ContactTrialBindingMatches(target)) { target.Invalid = true; continue; }
                Vector3 center = ReadContactTrialVector(Invoke(_contactTrialWorldPosition, target.Bone));
                target.PairBone = center; target.ReleaseMask = 0;
                Vector3 baseline = ReadContactTrialVector(Invoke(_contactTrialLocalPosition, target.Bone));
                Vector3 radial = target.RequiresMeasuredSurface ? Vector3.Zero :
                    ReadContactTrialVector(InvokeWithObjectArgument(_contactTrialTransformVector,
                        target.Parent, BoxContactTrialVector(baseline)));
                float length = baseline.Length(), worldLength = radial.Length();
                if (!ContactResponse.Finite(center) || !float.IsFinite(length) || !float.IsFinite(worldLength) ||
                    (!target.RequiresMeasuredSurface && (length <= 0.0001f || worldLength <= 0.0001f)))
                { target.ResetContactMotion(); target.PreviousCenter = null; targetsContinuous &= target.IsCalf; continue; }
                float radius = tracking.ContactBodyRadiusMeters;
                Vector3 outward = target.RequiresMeasuredSurface ? Vector3.Zero : Vector3.Normalize(radial);
                Matrix4x4 boneToWorld = default;
                if (target.Surface.HasValue || (jiggle && _contactReadRotation != IntPtr.Zero))
                    boneToWorld = ReadContactMatrix(Invoke(_contactBoneToWorld, target.Bone));
                if (target.Surface is ContactSurfaceAnchor surface)
                {
                    if (!surface.TryMap(boneToWorld, out center, out outward, out radius))
                    {
                        target.ResetContactMotion(); target.PreviousCenter = null; target.TouchMask = target.PreviousTouchMask = 0;
                        targetsContinuous &= target.IsCalf; continue;
                    }
                }
                target.PairSurface = target.Surface.HasValue ? center + outward * radius : center;
                bool targetContinuous = target.PreviousCenter is Vector3 previous && Vector3.Distance(previous, center) <= 0.15f;
                target.PreviousCenter = center;
                targetsContinuous &= target.IsCalf || targetContinuous;
                target.PairCenter = center; target.PairRadius = radius;
                if (captureBounds)
                {
                    IntPtr boxedBounds = Invoke(_contactTrialBounds, target.Skin);
                    IntPtr bounds = boxedBounds == IntPtr.Zero ? IntPtr.Zero : _api.ObjectUnbox(boxedBounds);
                    target.SkinCenter = bounds == IntPtr.Zero ? null : ReadContactTrialVector(Invoke(_contactTrialBoundsCenter, bounds));
                    target.SkinExtents = bounds == IntPtr.Zero ? null : ReadContactTrialVector(Invoke(_contactTrialBoundsExtents, bounds));
                }
                float handRadius = alignment.Scale * tracking.ContactHandRadiusMeters;
                bool hitLeft = ContactResponse.Sphere(center, radius, left, handRadius, outward, out var pushLeft);
                bool hitRight = ContactResponse.Sphere(center, radius, right, handRadius, outward, out var pushRight);
                int rawTouchMask = (hitLeft ? 1 : 0) | (hitRight ? 2 : 0);
                target.RawTouchMask = rawTouchMask;
                target.TouchMask = target.IsCalf
                    ? ResolveCalfContactMask(rawTouchMask, target.PreviousTouchMask,
                        targetContinuous && leftContinuous, targetContinuous && rightContinuous,
                        center, radius + handRadius, left, right)
                    : rawTouchMask;
                mask |= target.TouchMask;
                if (target.IsCalf && target.TouchMask == 0 && target.PreviousTouchMask == 0 &&
                    target.Response.Offset == Vector3.Zero && target.Response.Velocity == Vector3.Zero &&
                    target.TiltResponse.Offset == Vector3.Zero && target.TiltResponse.Velocity == Vector3.Zero &&
                    target.PressLog == Vector3.Zero)
                    continue;
                Vector3 pressure = (leftContinuous ? pushLeft : Vector3.Zero) + (rightContinuous ? pushRight : Vector3.Zero);
                float penetration = ((leftContinuous ? pushLeft.Length() : 0) + (rightContinuous ? pushRight.Length() : 0)) / .65f;
                ContactMaterialProfile material = target.Region switch
                {
                    ContactRegion.Chest => ContactMaterialProfile.Chest(tracking, target.BonePath == ContactRightBonePath),
                    ContactRegion.Thigh => thighMaterial,
                    ContactRegion.Calf => calfMaterial,
                    _ => throw new InvalidOperationException("Unsupported contact region.")
                };
                Vector3 desired = ContactMaterialProfile.Compliant(pressure / .65f, outward, Vector3.UnitY);
                // A lost or reserved contacting hand cancels inertia; only a tracked exit springs back.
                bool validMotion = continuous && targetContinuous &&
                    ContactTargetContinuous(target.PreviousTouchMask, leftContinuous, rightContinuous);
                Vector3 release = ContactResponse.ExitImpulse(target.PreviousTouchMask, target.TouchMask,
                    validMotion ? velocityMask : 0, leftVelocity, rightVelocity, out int exitMask);
                target.ReleaseMask = jiggle ? exitMask : 0;
                target.ReleaseCount += BitOperations.PopCount((uint)target.ReleaseMask);
                target.PreviousTouchMask = target.TouchMask;
                release = ContactMaterialProfile.Compliant(release * (material.ReleaseGain / .08f), outward, Vector3.UnitY);
                float worldLimit = MathF.Min(.05f, material.MaxDisplacement);
                Vector3 worldOffset = target.Response.Step(_contactCollisionPair, seconds, desired, worldLimit, validMotion,
                    jiggle, release, jiggle ? material.Position : null);
                bool elastic = target.CompensatedChildren.Length == 0 &&
                    (target.Region == ContactRegion.Thigh ? tracking.ContactThighElasticEnabled : jiggle);
                // Measured patches use the patch size as the lever because their helper pivots may coincide.
                float shapeLever = target.RequiresMeasuredSurface ? radius * 2f : worldLength;
                PrepareContactShape(target, boneToWorld, outward, shapeLever, worldOffset, pressure, penetration, seconds,
                    material, validMotion && elastic);
                Vector3 localOffset = ReadContactTrialVector(InvokeWithObjectArgument(_contactInverseVector,
                    target.Parent, BoxContactTrialVector(worldOffset)));
                if (!ContactResponse.Finite(localOffset)) throw new InvalidOperationException("Nonfinite contact offset.");
                float localLimit = target.RequiresMeasuredSurface ? .02f : MathF.Min(.04f, length * .5f);
                target.PairOffset = ContactResponse.Clamp(localOffset, localLimit);
            }
            _contactCollisionValid = (leftContinuous && targetsContinuous ? 1 : 0) | (rightContinuous && targetsContinuous ? 2 : 0);
            int pulse = (_contactLeftLatch.Update((_contactCollisionValid & 1) != 0, (mask & 1) != 0) ? 1 : 0) |
                (_contactRightLatch.Update((_contactCollisionValid & 2) != 0, (mask & 2) != 0) ? 2 : 0);
            _contactCollisionMask = mask;
            _contactTrialPulse = _contactTrialPairTargets.Any(t => t.HasContactWrite);
            _contactCollisionPulse = pulse;
            _contactCollisionSample = stereo.CapturedMilliseconds;
        }
        catch (Exception error) { FailContactBoneTrial("collision-prepare", error); }
    }

    private void PrepareContactShape(ContactTrialTarget target, Matrix4x4 boneToWorld, Vector3 outward,
        float worldLever, Vector3 worldOffset, Vector3 pressure, float penetration, float seconds,
        ContactMaterialProfile material, bool continuous)
    {
        if (!continuous || _contactReadRotation == IntPtr.Zero || !Matrix4x4.Invert(boneToWorld, out Matrix4x4 worldToBone))
        {
            target.TiltResponse.Reset();
            target.PressLog = Vector3.Zero;
            return;
        }
        Vector3 localOutward = Vector3.TransformNormal(outward, worldToBone);
        float length = localOutward.Length();
        if (!float.IsFinite(length) || length <= .000001f)
        {
            target.TiltResponse.Reset();
            target.PressLog = Vector3.Zero;
            return;
        }
        Vector3 depth = localOutward / length;
        float lever = worldLever * length;
        Vector3 localOffset = Vector3.TransformNormal(worldOffset, worldToBone);
        Vector3 tiltGoal = material.TiltGoal(localOffset, depth, lever);
        target.PairTilt = target.TiltResponse.Step(_contactCollisionPair, seconds, tiltGoal, material.MaxTiltRadians,
            true, tuning: material.Tilt);
        if (target.Surface.HasValue && _contactReadScale != IntPtr.Zero)
        {
            Vector3 localPressure = Vector3.TransformNormal(pressure, worldToBone);
            target.PairScale = material.ScaleGoal(localOffset, localPressure, penetration * length, depth, lever,
                target.PressLog, seconds, out Vector3 press);
            target.PressLog = press;
        }
    }

    internal static bool ContactHandVelocity(ContactHandAlignment alignment, TrackingVector3? current,
        TrackingVector3? previous, float seconds, out Vector3 velocity)
    {
        velocity = default;
        if (!float.IsFinite(seconds) || seconds <= 0 || seconds > .25f ||
            current is not TrackingVector3 c || previous is not TrackingVector3 p ||
            !alignment.TryMap(c, out var currentWorld) || !alignment.TryMap(p, out var previousWorld)) return false;
        // Map both raw poses with this pair's alignment: artificial camera movement adds no strike velocity.
        velocity = (currentWorld - previousWorld) / seconds;
        return ContactResponse.Finite(velocity) && velocity.Length() <= 4f * alignment.Scale;
    }

    internal static bool ContactTargetContinuous(int previousMask, bool left, bool right) =>
        (left || right) && ((previousMask & 1) == 0 || left) && ((previousMask & 2) == 0 || right);

    // Keep the original entry radius and pressure. Only a continuously tracked touch gets a wider exit boundary.
    internal static int ResolveCalfContactMask(int overlapMask, int previousMask, bool leftContinuous,
        bool rightContinuous, Vector3 center, float entryRadius, Vector3? left, Vector3? right)
    {
        float exitRadius = entryRadius + CalfContactReleaseMarginMeters;
        float exitRadiusSquared = exitRadius * exitRadius;
        int retained = 0;
        if (leftContinuous && (previousMask & 1) != 0 && left is Vector3 l &&
            Vector3.DistanceSquared(center, l) < exitRadiusSquared)
            retained |= 1;
        if (rightContinuous && (previousMask & 2) != 0 && right is Vector3 r &&
            Vector3.DistanceSquared(center, r) < exitRadiusSquared)
            retained |= 2;
        return overlapMask | retained;
    }

    private static string Position(Vector3? value) => value is Vector3 v
        ? FormattableString.Invariant($"{v.X:F4},{v.Y:F4},{v.Z:F4}") : "unavailable";

    private static bool ContinuousHand(Vector3? current, Vector3? previous, float scale) =>
        current is Vector3 c && previous is Vector3 p && Vector3.Distance(c, p) <= 0.5f * scale;

    private void ReportContactCollisionPair(DateTimeOffset now)
    {
        if (_contactCollisionPulse != 0 && VrSettingsRuntime.Current.Tracking.ContactHapticsEnabled &&
            ContactActiveLeases == 0 &&
            _contactTrialPairTargets.Any(t => t.EyeWrites == 3))
        {
            int renderedTouch = _contactTrialPairTargets.Where(t => t.EyeWrites == 3).Aggregate(0, (mask, t) => mask | t.TouchMask);
            OpenXrContactHaptics.Request(_contactCollisionPulse & renderedTouch, _contactCollisionSample);
        }
        _contactCollisionPulse = 0;
        if (now < _nextContactCollisionLog) return;
        _nextContactCollisionLog = now.AddSeconds(1);
        foreach (var target in _contactTrialPairTargets)
        {
            if (target.IsCalf && target.TouchMask == 0 && target.ReleaseMask == 0 && !target.HasContactWrite) continue;
            LogContactPilot(now, "contact-collision-target",
                $"actor={target.ActorId};target={target.Id};region={target.Region.ToString().ToLowerInvariant()};bonePath={target.BonePath};invalid={target.Invalid};touchMask={target.TouchMask};eyeMask={target.EyeWrites};rawTouchMask={target.RawTouchMask};bone={Position(target.PairBone)};collider={Position(target.PairCenter)};surface={Position(target.PairSurface)};fit={target.SurfaceSource};releaseMask={target.ReleaseMask};releaseCount={target.ReleaseCount};skinCenter={Position(target.SkinCenter)};skinExtents={Position(target.SkinExtents)};left={Position(_contactPairLeft)};right={Position(_contactPairRight)};radius={target.PairRadius:F4};offset={Position(target.PairOffset)};tilt={Position(target.PairTilt)};scale={Position(target.PairScale)};eyeCenterGap={target.MaxEyeCenterGap:F5}");
        }
        LogContactPilot(now, "contact-collision-pair",
            $"actors={_contactTrialPairTargets.Where(t => !t.Invalid).Select(t => t.ActorId).Distinct().Count()};targets={_contactTrialPairTargets.Count(t => !t.Invalid)};touchMask={_contactCollisionMask};validMask={_contactCollisionValid};bothEyes={_contactTrialPairTargets.Count(t => t.EyeWrites == 3)};writes={_contactTrialWrites};restores={_contactTrialRestores};externalChanges={_contactTrialExternalChanges};activeLeases={ContactActiveLeases};visualIsolation=unverified");
    }
}
