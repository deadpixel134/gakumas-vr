using System.Numerics;

namespace GakumasVR.Core;

public enum ContactRegion { Unknown, Chest, Thigh, Calf }

public readonly struct ContactSpringTuning(float frequencyHz, float dampingRatio, float stiffening)
{
    public float FrequencyHz { get; } = frequencyHz;
    public float DampingRatio { get; } = dampingRatio;
    public float Stiffening { get; } = stiffening;
}

public readonly struct ContactMaterialProfile
{
    private ContactMaterialProfile(ContactSpringTuning position, ContactSpringTuning tilt,
        float maxDisplacement, float releaseGain, float maxTiltRadians, float tiltGain,
        float stretch, float squash, float pressSquash)
    {
        Position = position;
        Tilt = tilt;
        MaxDisplacement = maxDisplacement;
        ReleaseGain = releaseGain;
        MaxTiltRadians = maxTiltRadians;
        TiltGain = tiltGain;
        Stretch = stretch;
        Squash = squash;
        PressSquash = pressSquash;
    }

    public ContactSpringTuning Position { get; }
    public ContactSpringTuning Tilt { get; }
    public float MaxDisplacement { get; }
    public float ReleaseGain { get; }
    public float MaxTiltRadians { get; }
    private float TiltGain { get; }
    public float Stretch { get; }
    public float Squash { get; }
    public float PressSquash { get; }

    public static ContactMaterialProfile Chest(VrTrackingSettings settings, bool right)
    {
        float side = right ? 1.03f : 1f;
        float damping = .12f / settings.ContactDurationMultiplier;
        float strength = settings.ContactStrengthPercent / 100f;
        float stretch = settings.ContactStretchPercent / 100f;
        return new(new(2f * side, damping, 2f), new(2.6f * side, damping * .85f, 0),
            .04f * settings.ContactRangePercent / 100f, .6f * strength,
            (12f * MathF.PI / 180f) * settings.ContactTiltPercent / 100f, strength,
            .15f * stretch, .10f * stretch, .15f * stretch);
    }

    public static ContactMaterialProfile Thigh(VrTrackingSettings settings)
    {
        float frequency = settings.ContactThighFrequencyHz;
        float damping = settings.ContactThighDampingRatio;
        float compression = settings.ContactThighCompressionPercent / 100f;
        return new(new(frequency, damping, 2f), new(frequency * 1.3f, damping, 0),
            settings.ContactThighDisplacementMm / 1000f, settings.ContactThighReleasePercent / 100f,
            settings.ContactThighTiltDegrees * MathF.PI / 180f, .35f,
            settings.ContactThighStretchPercent / 100f, .8f * compression, compression);
    }

    public static ContactMaterialProfile Calf() =>
        new(new(5f, .78f, 2f), new(6.5f, .78f, 0f),
            .004f, .01f, MathF.PI / 180f, .35f, .009f, .0144f, .018f);

    public static Vector3 Compliant(Vector3 input, Vector3 depth, Vector3 up)
    {
        Vector3 vertical = up - depth * Vector3.Dot(up, depth);
        if (vertical.LengthSquared() < .0001f) return input;
        vertical = Vector3.Normalize(vertical);
        Vector3 lateral = Vector3.Cross(vertical, depth);
        return depth * (.7f * Vector3.Dot(input, depth)) + vertical * (1.3f * Vector3.Dot(input, vertical)) +
            lateral * (.9f * Vector3.Dot(input, lateral));
    }

    public Vector3 TiltGoal(Vector3 offset, Vector3 depth, float lever)
    {
        Vector3 axis = Vector3.Cross(depth, offset);
        float length = axis.Length();
        if (length < .000001f || lever <= 0) return default;
        return axis * (MathF.Min(MaxTiltRadians, TiltGain * MathF.Atan(length / lever)) / length);
    }

    public Vector3 ScaleGoal(Vector3 offset, Vector3 pressure, float penetration, Vector3 depth, float lever, Vector3 currentPress,
        float seconds, out Vector3 press)
    {
        press = default;
        if (lever <= 0 || (Stretch <= 0 && Squash <= 0 && PressSquash <= 0)) return Vector3.One;
        int axis = AlignedAxis(depth, .7f);
        if (axis < 0) return Vector3.One;
        float extension = Vector3.Dot(offset, depth) / lever;
        float limit = extension >= 0 ? Stretch : Squash;
        float strain = limit > 0 ? limit * MathF.Tanh(extension / limit) : 0f;
        Vector3 pressureLog = default;
        if (PressSquash > 0 && pressure.LengthSquared() > 1e-12f &&
            AlignedAxis(Vector3.Normalize(pressure), .6f) == axis && Vector3.Dot(pressure, depth) < 0)
            pressureLog = AxisLog(axis, MathF.Log(1f - PressSquash * MathF.Tanh(penetration / (lever * PressSquash))));
        float alpha = 1f - MathF.Exp(-seconds * Position.DampingRatio * 2f * MathF.PI * Position.FrequencyHz);
        press = Vector3.Lerp(currentPress, pressureLog, alpha);
        if (pressureLog == default && press.LengthSquared() < 1e-8f) press = default;
        float pressureAxis = axis == 0 ? press.X : axis == 1 ? press.Y : press.Z;
        float combined = MathF.Max(MathF.Log(1f - MathF.Max(Squash, PressSquash)), MathF.Log(1f + strain) + pressureAxis);
        Vector3 log = AxisLog(axis, combined);
        return new(MathF.Exp(log.X), MathF.Exp(log.Y), MathF.Exp(log.Z));
    }

    private static int AlignedAxis(Vector3 vector, float minimum)
    {
        var v = Vector3.Abs(vector);
        float max = MathF.Max(v.X, MathF.Max(v.Y, v.Z));
        return max < minimum ? -1 : max == v.X ? 0 : max == v.Y ? 1 : 2;
    }

    private static Vector3 AxisLog(int axis, float value)
    {
        float compensation = -.5f * value;
        return axis == 0 ? new(value, compensation, compensation) : axis == 1
            ? new(compensation, value, compensation) : new(compensation, compensation, value);
    }
}
