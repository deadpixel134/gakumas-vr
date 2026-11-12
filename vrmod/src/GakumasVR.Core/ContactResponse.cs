using System.Numerics;

namespace GakumasVR.Core;

// All distances are in the rendered Unity world, after hand/eye alignment.
public sealed class ContactResponse
{
    public Vector3 Offset { get; private set; }
    public Vector3 Velocity { get; private set; }
    private long _pair = long.MinValue;
    private Vector3 _output;

    public void Reset()
    {
        Offset = Velocity = _output = Vector3.Zero;
        _pair = long.MinValue;
    }

    public static bool Sphere(Vector3 center, float targetRadius, Vector3? hand, float handRadius,
        Vector3 fallbackNormal, out Vector3 displacement)
    {
        displacement = Vector3.Zero;
        if (hand is not Vector3 position || !Finite(center) || !Finite(position) ||
            !float.IsFinite(targetRadius) || !float.IsFinite(handRadius) || targetRadius <= 0 || handRadius <= 0)
            return false;
        Vector3 delta = center - position;
        float distance = delta.Length(), radius = targetRadius + handRadius;
        if (!float.IsFinite(distance) || distance >= radius) return false;
        Vector3 normal = distance > 0.000001f ? delta / distance :
            Finite(fallbackNormal) && fallbackNormal.LengthSquared() > 0.000001f
                ? Vector3.Normalize(fallbackNormal) : Vector3.UnitZ;
        displacement = normal * ((radius - distance) * 0.65f);
        return true;
    }

    public Vector3 Step(long pair, float seconds, Vector3 desired, float limit, bool continuous,
        bool jiggle = false, Vector3 releaseImpulse = default, ContactSpringTuning? tuning = null)
    {
        if (_pair == pair) return _output; // Both eyes and repeated camera callbacks share one result.
        _pair = pair;
        if (!continuous || !float.IsFinite(seconds) || seconds <= 0 || seconds > 0.1f ||
            !Finite(desired) || !float.IsFinite(limit) || limit <= 0)
        {
            Offset = Velocity = _output = Vector3.Zero;
            return Offset;
        }
        desired = Clamp(desired, limit);
        if (jiggle && Finite(releaseImpulse)) Velocity += Clamp(releaseImpulse, limit * 12f);
        float omega = tuning is { } material ? 2f * MathF.PI * material.FrequencyHz : 25f;
        float damping = tuning is { } profile ? 2f * profile.DampingRatio * omega : jiggle ? 12f : 35f;
        float stiffening = tuning?.Stiffening ?? 0f;
        // Substeps keep a damped spring bounded even at low game rates.
        int steps = Math.Max(1, (int)MathF.Ceiling(seconds / 0.004f));
        float dt = seconds / steps;
        for (int i = 0; i < steps; i++)
        {
            float relative = Offset.LengthSquared() / (limit * limit);
            Velocity += ((desired - Offset * (1f + stiffening * relative)) * (omega * omega) - Velocity * damping) * dt;
            Offset += Velocity * dt;
            float guard = tuning.HasValue ? limit * 3f : limit;
            if (Offset.LengthSquared() > guard * guard)
            {
                Offset = Clamp(Offset, guard);
                float outward = Vector3.Dot(Velocity, Vector3.Normalize(Offset));
                if (outward > 0) Velocity -= Vector3.Normalize(Offset) * outward;
            }
        }
        if (desired == Vector3.Zero && Offset.LengthSquared() < 1e-12f && Velocity.LengthSquared() < 1e-10f)
            Offset = Velocity = Vector3.Zero;
        float magnitude = Offset.Length();
        _output = tuning.HasValue && magnitude > 0 ? Offset * (limit * MathF.Tanh(magnitude / limit) / magnitude) : Offset;
        return _output;
    }

    public static Vector3 Clamp(Vector3 value, float limit) =>
        value.LengthSquared() > limit * limit ? Vector3.Normalize(value) * limit : value;
    public static bool Finite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

    public static Vector3 ExitImpulse(int previousMask, int currentMask, int velocityValidMask,
        Vector3 leftVelocity, Vector3 rightVelocity, out int exitMask)
    {
        exitMask = previousMask & ~currentMask & velocityValidMask & 3;
        Vector3 impulse = default;
        if ((exitMask & 1) != 0 && Finite(leftVelocity)) impulse += leftVelocity * .08f;
        if ((exitMask & 2) != 0 && Finite(rightVelocity)) impulse += rightVelocity * .08f;
        return impulse;
    }
}

// A lost/blocked hand must first be seen outside every target before another pulse.
public sealed class ContactEnterLatch
{
    private bool _armed;
    public void Reset() => _armed = false;
    public bool Update(bool valid, bool touching)
    {
        if (!valid) { _armed = false; return false; }
        if (!touching) { _armed = true; return false; }
        bool pulse = _armed;
        _armed = false;
        return pulse;
    }
}
