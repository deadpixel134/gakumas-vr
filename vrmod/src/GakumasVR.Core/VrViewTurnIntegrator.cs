namespace GakumasVR.Core;

public sealed class VrViewTurnIntegrator
{
    private float _yawRadians;
    private float _pitchRadians;
    private bool _snapArmed = true;

    public TrackingQuaternion Rotation => FromYawPitch(
        _yawRadians,
        _pitchRadians);

    public bool Update(
        float axisX,
        float axisY,
        float deltaSeconds,
        VrViewTurnMode mode,
        float degreesPerSecond,
        int snapAngleDegrees,
        float deadzone = 0.20f)
    {
        if (!float.IsFinite(axisX) || !float.IsFinite(axisY) ||
            !float.IsFinite(deltaSeconds) || deltaSeconds < 0f ||
            !float.IsFinite(degreesPerSecond) || degreesPerSecond < 0f ||
            !Enum.IsDefined(typeof(VrViewTurnMode), mode) ||
            snapAngleDegrees is not (15 or 30 or 45 or 60) ||
            !float.IsFinite(deadzone) || deadzone < 0f || deadzone >= 1f)
        {
            return false;
        }

        float absoluteX = MathF.Abs(axisX);
        float absoluteY = MathF.Abs(axisY);
        float maximumAxis = MathF.Max(absoluteX, absoluteY);
        if (maximumAxis <= deadzone)
        {
            _snapArmed = true;
            return true;
        }

        // A view stick is a cardinal world-space control. Selecting only the
        // dominant axis prevents small physical stick skew from becoming an
        // unintended diagonal yaw/pitch rotation.
        float cardinalX = absoluteX >= absoluteY ? axisX : 0f;
        float cardinalY = absoluteY > absoluteX ? axisY : 0f;
        if (mode == VrViewTurnMode.Snap)
        {
            const float SnapActivationThreshold = 0.65f;
            if (!_snapArmed || maximumAxis < MathF.Max(deadzone, SnapActivationThreshold))
            {
                return true;
            }

            float snapRadians = snapAngleDegrees * MathF.PI / 180f;
            _yawRadians += MathF.Sign(cardinalX) * snapRadians;
            _pitchRadians -= MathF.Sign(cardinalY) * snapRadians;
            _snapArmed = false;
        }
        else
        {
            float adjustedX = ApplyDeadzone(cardinalX, deadzone);
            float adjustedY = ApplyDeadzone(cardinalY, deadzone);
            float radiansPerSecond = degreesPerSecond * MathF.PI / 180f;
            float step = radiansPerSecond * MathF.Min(deltaSeconds, 0.10f);
            _yawRadians += adjustedX * step;
            _pitchRadians -= adjustedY * step;
        }

        _pitchRadians = Math.Clamp(
            _pitchRadians,
            -MathF.PI * 0.495f,
            MathF.PI * 0.495f);
        if (MathF.Abs(_yawRadians) > MathF.PI * 2f)
        {
            _yawRadians %= MathF.PI * 2f;
        }
        return true;
    }

    public void Reset()
    {
        _yawRadians = 0f;
        _pitchRadians = 0f;
        _snapArmed = true;
    }

    private static float ApplyDeadzone(float value, float deadzone)
    {
        float magnitude = MathF.Abs(value);
        if (magnitude <= deadzone)
        {
            return 0f;
        }
        float adjusted =
            (MathF.Min(magnitude, 1f) - deadzone) / (1f - deadzone);
        return value < 0f ? -adjusted : adjusted;
    }

    private static TrackingQuaternion FromYawPitch(float yaw, float pitch)
    {
        float halfYaw = yaw * 0.5f;
        float halfPitch = pitch * 0.5f;
        TrackingQuaternion yawRotation = new(
            0f,
            MathF.Sin(halfYaw),
            0f,
            MathF.Cos(halfYaw));
        TrackingQuaternion pitchRotation = new(
            MathF.Sin(halfPitch),
            0f,
            0f,
            MathF.Cos(halfPitch));
        return Multiply(yawRotation, pitchRotation);
    }

    private static TrackingQuaternion Multiply(
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
}
