namespace Doorstop;

internal readonly record struct OpenXrEyeState(
    float PositionX,
    float PositionY,
    float PositionZ,
    float OrientationX,
    float OrientationY,
    float OrientationZ,
    float OrientationW,
    float FovLeft,
    float FovRight,
    float FovUp,
    float FovDown);

internal sealed class OpenXrStereoStateSnapshot
{
    public int RecommendedWidth { get; init; }
    public int RecommendedHeight { get; init; }
    public ulong ViewStateFlags { get; init; }
    public OpenXrEyeState Left { get; init; }
    public OpenXrEyeState Right { get; init; }
}

internal static class OpenXrStereoStateRegistry
{
    private static readonly object Sync = new();
    private static int _recommendedWidth;
    private static int _recommendedHeight;
    private static ulong _viewStateFlags;
    private static OpenXrEyeState _left;
    private static OpenXrEyeState _right;
    private static long _updatedMilliseconds;

    public static void UpdateConfiguration(uint recommendedWidth, uint recommendedHeight)
    {
        lock (Sync)
        {
            _recommendedWidth = checked((int)recommendedWidth);
            _recommendedHeight = checked((int)recommendedHeight);
        }
    }

    public static void UpdateViews(
        ulong viewStateFlags,
        OpenXrEyeState left,
        OpenXrEyeState right)
    {
        lock (Sync)
        {
            _viewStateFlags = viewStateFlags;
            _left = left;
            _right = right;
            _updatedMilliseconds = Environment.TickCount64;
        }
    }

    public static OpenXrStereoStateSnapshot? Snapshot(int maximumAgeMilliseconds)
    {
        lock (Sync)
        {
            if (_recommendedWidth <= 0 || _recommendedHeight <= 0 ||
                _updatedMilliseconds == 0 ||
                Environment.TickCount64 - _updatedMilliseconds > maximumAgeMilliseconds)
            {
                return null;
            }

            return new OpenXrStereoStateSnapshot
            {
                RecommendedWidth = _recommendedWidth,
                RecommendedHeight = _recommendedHeight,
                ViewStateFlags = _viewStateFlags,
                Left = _left,
                Right = _right
            };
        }
    }
}
