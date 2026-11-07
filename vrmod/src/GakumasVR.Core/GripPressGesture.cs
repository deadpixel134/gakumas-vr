namespace GakumasVR.Core;

public enum GripGesture { None, Short, Long }

public sealed class GripPressGesture
{
    private readonly long _holdTicks;
    private long _pressedAt;
    private bool _pressed;
    private bool _longSent;
    private bool _armed;

    public GripPressGesture(long holdTicks) => _holdTicks = holdTicks > 0 ? holdTicks : throw new ArgumentOutOfRangeException(nameof(holdTicks));

    public GripGesture Update(bool active, float value, long now)
    {
        if (!active || float.IsNaN(value))
        {
            _pressed = false;
            _armed = false;
            return GripGesture.None;
        }
        if (value <= .25f)
        {
            GripGesture result = !_pressed || _longSent ? GripGesture.None :
                now - _pressedAt >= _holdTicks ? GripGesture.Long : GripGesture.Short;
            _pressed = false;
            _armed = true;
            return result;
        }
        if (_armed && !_pressed && value >= .72f)
        {
            _pressedAt = now;
            _pressed = true;
            _longSent = false;
        }
        if (_pressed && !_longSent && now - _pressedAt >= _holdTicks)
        {
            _longSent = true;
            return GripGesture.Long;
        }
        return GripGesture.None;
    }
}
