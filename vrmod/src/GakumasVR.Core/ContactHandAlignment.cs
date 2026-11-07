using System.Numerics;

namespace GakumasVR.Core;

public readonly struct ContactHandAlignment
{
    public const float HandRadiusMeters = 0.035f;
    private readonly Vector3 _rawCenter;
    private readonly Vector3 _renderedCenter;
    private readonly Quaternion _rotation;
    public float Scale { get; }

    private ContactHandAlignment(Vector3 rawCenter, Vector3 renderedCenter, Quaternion rotation, float scale)
    {
        _rawCenter = rawCenter;
        _renderedCenter = renderedCenter;
        _rotation = rotation;
        Scale = scale;
    }

    public static bool TryCreate(TrackingStereoPose raw, Vector3 left, Vector3 right,
        Quaternion leftRotation, Quaternion rightRotation, out ContactHandAlignment alignment)
    {
        alignment = default;
        Vector3 rawLeft = ToUnity(raw.Left.Position);
        Vector3 rawRight = ToUnity(raw.Right.Position);
        float rawIpd = Vector3.Distance(rawLeft, rawRight);
        float renderedIpd = Vector3.Distance(left, right);
        float scale = renderedIpd / rawIpd;
        if (rawIpd <= 0.0001f || !float.IsFinite(scale) || scale <= 0f ||
            !TryCenter(ToUnity(raw.Left.Orientation), ToUnity(raw.Right.Orientation), out Quaternion rawRotation) ||
            !TryCenter(leftRotation, rightRotation, out Quaternion renderedRotation))
            return false;

        Vector3 rawCenter = rawLeft + (rawRight - rawLeft) * 0.5f;
        Vector3 renderedCenter = left + (right - left) * 0.5f;
        if (!IsFinite(rawCenter) || !IsFinite(renderedCenter)) return false;
        alignment = new(rawCenter, renderedCenter, renderedRotation * Quaternion.Conjugate(rawRotation), scale);
        return true;
    }

    public bool TryMap(TrackingVector3 hand, out Vector3 position)
    {
        position = _renderedCenter + Vector3.Transform(ToUnity(hand) - _rawCenter, _rotation) * Scale;
        return Scale > 0f && IsFinite(position);
    }

    public static float PilotNearClip(float originalNear, float unitsPerMeter)
    {
        if (!float.IsFinite(originalNear) || originalNear <= 0f ||
            !float.IsFinite(unitsPerMeter) || unitsPerMeter <= 0f)
            return originalNear;

        return MathF.Min(originalNear, MathF.Max(0.005f, 0.06f * unitsPerMeter));
    }

    public static bool TryProjectRawHand(TrackingVector3 hand, TrackingEyePose eye,
        float fovLeft, float fovRight, float fovDown, float fovUp, out Vector3 viewportAndDepth)
    {
        viewportAndDepth = default;
        Quaternion rotation = ToUnity(eye.Orientation);
        float norm = rotation.LengthSquared();
        if (!float.IsFinite(norm) || norm < 0.000001f) return false;
        Vector3 local = Vector3.Transform(ToUnity(hand) - ToUnity(eye.Position), Quaternion.Conjugate(Quaternion.Normalize(rotation)));
        float left = MathF.Tan(fovLeft), right = MathF.Tan(fovRight);
        float down = MathF.Tan(fovDown), up = MathF.Tan(fovUp);
        if (!IsFinite(local) || local.Z <= 0f || !(left < right) || !(down < up)) return false;
        viewportAndDepth = new((local.X / local.Z - left) / (right - left),
            (local.Y / local.Z - down) / (up - down), local.Z);
        return IsFinite(viewportAndDepth);
    }

    private static bool TryCenter(Quaternion left, Quaternion right, out Quaternion center)
    {
        center = default;
        float leftNorm = left.LengthSquared();
        float rightNorm = right.LengthSquared();
        if (!float.IsFinite(leftNorm) || !float.IsFinite(rightNorm) || leftNorm < 0.000001f || rightNorm < 0.000001f)
            return false;
        center = Quaternion.Normalize(Quaternion.Slerp(Quaternion.Normalize(left), Quaternion.Normalize(right), 0.5f));
        return true;
    }

    private static Vector3 ToUnity(TrackingVector3 value) => new(value.X, value.Y, -value.Z);
    private static Quaternion ToUnity(TrackingQuaternion value) => new(-value.X, -value.Y, value.Z, value.W);
    private static bool IsFinite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
