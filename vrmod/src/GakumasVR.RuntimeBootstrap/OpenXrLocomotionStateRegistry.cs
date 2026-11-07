using GakumasVR.Core;

namespace Doorstop;

internal readonly record struct OpenXrLocomotionStateSnapshot(
    float AxisX,
    float AxisY,
    float ViewTurnAxisX,
    float ViewTurnAxisY,
    bool ResetView,
    bool ToggleCameraFollow,
    TrackingVector3 WorldGripDelta);

internal static class OpenXrLocomotionStateRegistry
{
    private static readonly object Sync = new();
    private static readonly OpenXrWorldGripMotion WorldGrip = new();
    private const int MaximumAgeMilliseconds = 250;
    private static float _axisX;
    private static float _axisY;
    private static float _viewTurnAxisX;
    private static float _viewTurnAxisY;
    private static long _updatedMilliseconds;
    private static bool _leftClickArmed;
    private static bool _rightClickArmed;
    private static long? _resetViewMilliseconds;
    private static long? _toggleFollowMilliseconds;

    public static void Update(
        bool locomotionActive,
        float axisX,
        float axisY,
        bool viewTurnActive,
        float viewTurnAxisX,
        float viewTurnAxisY,
        bool? leftClick,
        bool? rightClick)
    {
        lock (Sync)
        {
            _axisX = locomotionActive && float.IsFinite(axisX) ? axisX : 0f;
            _axisY = locomotionActive && float.IsFinite(axisY) ? axisY : 0f;
            _viewTurnAxisX = viewTurnActive && float.IsFinite(viewTurnAxisX)
                ? viewTurnAxisX
                : 0f;
            _viewTurnAxisY = viewTurnActive && float.IsFinite(viewTurnAxisY)
                ? viewTurnAxisY
                : 0f;
            long now = Environment.TickCount64;
            TrackClick(leftClick, now, ref _leftClickArmed, ref _resetViewMilliseconds, toggle: false);
            TrackClick(rightClick, now, ref _rightClickArmed, ref _toggleFollowMilliseconds, toggle: true);
            _updatedMilliseconds = now;
        }
    }

    public static OpenXrLocomotionStateSnapshot? Consume()
    {
        lock (Sync)
        {
            long now = Environment.TickCount64;
            if (_updatedMilliseconds == 0 ||
                now - _updatedMilliseconds > MaximumAgeMilliseconds)
            {
                return null;
            }

            var snapshot = new OpenXrLocomotionStateSnapshot(
                _axisX,
                _axisY,
                _viewTurnAxisX,
                _viewTurnAxisY,
                IsFresh(_resetViewMilliseconds, now),
                IsFresh(_toggleFollowMilliseconds, now),
                WorldGrip.Consume(now));
            _resetViewMilliseconds = null;
            _toggleFollowMilliseconds = null;
            return snapshot;
        }
    }

    public static void UpdateWorldGrip(float? grip, TrackingVector3? position, long sampleTime)
    {
        lock (Sync) WorldGrip.Update(grip, position, sampleTime, Environment.TickCount64);
    }

    public static void CancelWorldGrip()
    {
        lock (Sync) WorldGrip.Cancel();
    }

    private static void TrackClick(bool? pressed, long now, ref bool armed, ref long? pending, bool toggle)
    {
        if (pressed is null)
        {
            armed = false;
            pending = null;
        }
        else if (!pressed.Value)
        {
            armed = true;
        }
        else if (armed)
        {
            armed = false;
            // Two unconsumed toggle presses cancel each other. Reset presses coalesce.
            pending = toggle && IsFresh(pending, now) ? null : now;
        }
    }

    private static bool IsFresh(long? timestamp, long now) =>
        timestamp is long value && now - value <= MaximumAgeMilliseconds;

    public static void Clear()
    {
        lock (Sync)
        {
            _axisX = 0f;
            _axisY = 0f;
            _viewTurnAxisX = 0f;
            _viewTurnAxisY = 0f;
            _updatedMilliseconds = 0;
            _leftClickArmed = false;
            _rightClickArmed = false;
            _resetViewMilliseconds = null;
            _toggleFollowMilliseconds = null;
            WorldGrip.Cancel();
        }
    }
}
