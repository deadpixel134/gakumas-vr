namespace Doorstop;

internal readonly record struct OpenXrLocomotionStateSnapshot(
    float AxisX,
    float AxisY);

internal static class OpenXrLocomotionStateRegistry
{
    private static readonly object Sync = new();
    private static float _axisX;
    private static float _axisY;
    private static long _updatedMilliseconds;

    public static void Update(bool active, float axisX, float axisY)
    {
        lock (Sync)
        {
            _axisX = active && float.IsFinite(axisX) ? axisX : 0f;
            _axisY = active && float.IsFinite(axisY) ? axisY : 0f;
            _updatedMilliseconds = Environment.TickCount64;
        }
    }

    public static OpenXrLocomotionStateSnapshot? Snapshot(
        int maximumAgeMilliseconds)
    {
        lock (Sync)
        {
            if (_updatedMilliseconds == 0 ||
                Environment.TickCount64 - _updatedMilliseconds > maximumAgeMilliseconds)
            {
                return null;
            }

            return new OpenXrLocomotionStateSnapshot(_axisX, _axisY);
        }
    }

    public static void Clear()
    {
        lock (Sync)
        {
            _axisX = 0f;
            _axisY = 0f;
            _updatedMilliseconds = 0;
        }
    }
}
