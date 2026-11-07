using GakumasVR.Core;

namespace Doorstop;

internal sealed class OpenXrWorldGripMotion
{
    private const long MaximumAgeMilliseconds = 250;
    private bool _armed;
    private TrackingVector3? _previousPosition;
    private TrackingVector3 _pending;
    private long? _pendingSince;
    private long _sampleTime;
    private long _sampleMilliseconds;

    public void Update(float? grip, TrackingVector3? position, long sampleTime, long now)
    {
        if (sampleTime <= 0 || grip is not float value || !float.IsFinite(value))
        {
            Cancel();
            return;
        }
        if (_sampleTime != 0 && (sampleTime <= _sampleTime || now - _sampleMilliseconds > MaximumAgeMilliseconds))
        {
            Cancel();
        }
        _sampleTime = sampleTime;
        _sampleMilliseconds = now;

        if (value <= .25f)
        {
            _previousPosition = null;
            _armed = true;
            return;
        }
        if (_previousPosition is null && (!_armed || value < .72f)) return;
        if (position is not TrackingVector3 tracked || !IsFinite(tracked))
        {
            Cancel();
            return;
        }

        if (_previousPosition is TrackingVector3 previous)
        {
            if (_pendingSince is long since && now - since > MaximumAgeMilliseconds)
            {
                _pending = default;
                _pendingSince = null;
            }
            var accumulated = new TrackingVector3(
                _pending.X + (tracked.X - previous.X),
                _pending.Y + (tracked.Y - previous.Y),
                _pending.Z + (tracked.Z - previous.Z));
            if (!IsFinite(accumulated))
            {
                Cancel();
                return;
            }
            _pending = accumulated;
            _pendingSince ??= now;
        }
        _previousPosition = tracked;
        _armed = false;
    }

    public TrackingVector3 Consume(long now)
    {
        TrackingVector3 delta = _pendingSince is long since && now - since <= MaximumAgeMilliseconds
            ? _pending : default;
        _pending = default;
        _pendingSince = null;
        return delta;
    }

    public void Cancel()
    {
        _armed = false;
        _previousPosition = null;
        _pending = default;
        _pendingSince = null;
        _sampleTime = 0;
        _sampleMilliseconds = 0;
    }

    private static bool IsFinite(TrackingVector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
