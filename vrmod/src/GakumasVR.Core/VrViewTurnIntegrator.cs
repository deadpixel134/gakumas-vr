namespace GakumasVR.Core;

public sealed class VrViewTurnIntegrator
{
    private float _yawRadians;
    private float _pitchRadians;

    public TrackingQuaternion Rotation => FromYawPitch(
        _yawRadians,
        _pitchRadians);

    public bool Update(
        float axisX,
        float axisY,
        float deltaSeconds,
        float degreesPerSecond,
        float deadzone = 0.20f)
    {
        if (!float.IsFinite(axisX) || !float.IsFinite(axisY) ||
            !float.IsFinite(deltaSeconds) || deltaSeconds < 0f ||
            !float.IsFinite(degreesPerSecond) || degreesPerSecond < 0f ||
            !float.IsFinite(deadzone) || deadzone < 0f || deadzone >= 1f)
        {
            return false;
        }

        float adjustedX = ApplyDeadzone(axisX, deadzone);
        float adjustedY = ApplyDeadzone(axisY, deadzone);
        float radiansPerSecond = degreesPerSecond * MathF.PI / 180f;
        float step = radiansPerSecond * MathF.Min(deltaSeconds, 0.10f);
        _yawRadians += adjustedX * step;
        _pitchRadians -= adjustedY * step;
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
