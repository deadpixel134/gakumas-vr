namespace Doorstop;

internal static class OpenXrContactHaptics
{
    private static readonly object Sync = new();
    private static int _pending;
    private static long _leftSample, _rightSample;

    internal static void Request(int mask, long sample)
    {
        lock (Sync)
        {
            if ((mask & 1) != 0) _leftSample = sample;
            if ((mask & 2) != 0) _rightSample = sample;
            _pending |= mask & 3;
        }
    }

    internal static int Consume(int allowedMask, long now)
    {
        lock (Sync)
        {
            int fresh = (now >= _leftSample && now - _leftSample <= 100 ? 1 : 0) |
                (now >= _rightSample && now - _rightSample <= 100 ? 2 : 0);
            int result = _pending & allowedMask & fresh;
            _pending = 0;
            return result;
        }
    }

    internal static void Clear() { lock (Sync) _pending = 0; }
}
