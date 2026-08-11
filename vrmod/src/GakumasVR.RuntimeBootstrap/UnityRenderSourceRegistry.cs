using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Doorstop;

internal sealed class D3D11TextureLease : IDisposable
{
    private IntPtr _texture;

    internal D3D11TextureLease(IntPtr texture, string sourceName)
    {
        _texture = texture;
        SourceName = sourceName;
    }

    public IntPtr Texture => Volatile.Read(ref _texture);

    public string SourceName { get; }

    public void Dispose()
    {
        IntPtr texture = Interlocked.Exchange(ref _texture, IntPtr.Zero);
        if (texture != IntPtr.Zero)
        {
            _ = Marshal.Release(texture);
        }
    }
}

internal sealed class D3D11StereoTextureLease : IDisposable
{
    private IntPtr _leftTexture;
    private IntPtr _rightTexture;

    internal D3D11StereoTextureLease(
        IntPtr leftTexture,
        IntPtr rightTexture,
        long publishedTimestamp,
        long sequence,
        bool requiresDynamicUi)
    {
        _leftTexture = leftTexture;
        _rightTexture = rightTexture;
        PublishedTimestamp = publishedTimestamp;
        Sequence = sequence;
        RequiresDynamicUi = requiresDynamicUi;
    }

    public IntPtr LeftTexture => Volatile.Read(ref _leftTexture);

    public IntPtr RightTexture => Volatile.Read(ref _rightTexture);

    public long PublishedTimestamp { get; }

    public long Sequence { get; }

    public bool RequiresDynamicUi { get; }

    public void Dispose()
    {
        IntPtr left = Interlocked.Exchange(ref _leftTexture, IntPtr.Zero);
        IntPtr right = Interlocked.Exchange(ref _rightTexture, IntPtr.Zero);
        UnityRenderSourceRegistry.ReleaseStereoTextureLease(left, right);
    }
}

internal static class UnityRenderSourceRegistry
{
    private static readonly object Sync = new();
    private static readonly Guid Id3D11Texture2D =
        new("6F15AAF2-D208-4E89-9AB4-489535D34F9C");
    private static IntPtr _liveWorldTexture;
    private static IntPtr _liveUiTexture;
    private static string _uiSourceName = string.Empty;
    private static long _uiUpdatedMilliseconds;
    private static string _sourceName = string.Empty;
    private static long _updatedMilliseconds;
    private static IntPtr _stereoLeftTexture;
    private static IntPtr _stereoRightTexture;
    private static long _stereoUpdatedMilliseconds;
    private static long _stereoPublishedTimestamp;
    private static long _stereoSequence;
    private static bool _stereoRequiresDynamicUi;
    private static readonly Dictionary<IntPtr, int> StereoLeaseCounts = new();

    public static void UpdateLiveWorldTexture(IntPtr texture, string sourceName)
    {
        if (texture == IntPtr.Zero)
        {
            ClearLiveWorldTexture();
            return;
        }

        Guid interfaceId = Id3D11Texture2D;
        int queryResult = Marshal.QueryInterface(texture, ref interfaceId, out IntPtr texture2D);
        if (queryResult < 0 || texture2D == IntPtr.Zero)
        {
            ClearLiveWorldTexture();
            return;
        }

        lock (Sync)
        {
            if (_liveWorldTexture != texture2D)
            {
                IntPtr previous = _liveWorldTexture;
                _liveWorldTexture = texture2D;
                if (previous != IntPtr.Zero)
                {
                    _ = Marshal.Release(previous);
                }
            }
            else
            {
                _ = Marshal.Release(texture2D);
            }

            _sourceName = sourceName;
            _updatedMilliseconds = Environment.TickCount64;
        }
    }

    public static void ClearLiveWorldTexture()
    {
        lock (Sync)
        {
            IntPtr previous = _liveWorldTexture;
            _liveWorldTexture = IntPtr.Zero;
            _sourceName = string.Empty;
            _updatedMilliseconds = 0;
            if (previous != IntPtr.Zero)
            {
                _ = Marshal.Release(previous);
            }
        }
    }

    public static bool TouchLiveWorldTexture(string sourceName)
    {
        lock (Sync)
        {
            if (_liveWorldTexture == IntPtr.Zero)
            {
                return false;
            }

            _sourceName = sourceName;
            _updatedMilliseconds = Environment.TickCount64;
            return true;
        }
    }

    public static D3D11TextureLease? AcquireLiveWorldTexture(int maximumAgeMilliseconds)
    {
        lock (Sync)
        {
            if (_liveWorldTexture == IntPtr.Zero ||
                Environment.TickCount64 - _updatedMilliseconds > maximumAgeMilliseconds)
            {
                return null;
            }

            _ = Marshal.AddRef(_liveWorldTexture);
            return new D3D11TextureLease(_liveWorldTexture, _sourceName);
        }
    }

    public static void UpdateLiveUiTexture(IntPtr texture, string sourceName)
    {
        if (texture == IntPtr.Zero)
        {
            return;
        }

        Guid interfaceId = Id3D11Texture2D;
        int queryResult = Marshal.QueryInterface(texture, ref interfaceId, out IntPtr texture2D);
        if (queryResult < 0 || texture2D == IntPtr.Zero)
        {
            return;
        }

        lock (Sync)
        {
            IntPtr previous = _liveUiTexture;
            _liveUiTexture = texture2D;
            _uiSourceName = sourceName;
            _uiUpdatedMilliseconds = Environment.TickCount64;
            if (previous != IntPtr.Zero)
            {
                _ = Marshal.Release(previous);
            }
        }
    }

    public static D3D11TextureLease? AcquireLiveUiTexture(int maximumAgeMilliseconds)
    {
        lock (Sync)
        {
            if (_liveUiTexture == IntPtr.Zero ||
                Environment.TickCount64 - _uiUpdatedMilliseconds > maximumAgeMilliseconds)
            {
                return null;
            }

            _ = Marshal.AddRef(_liveUiTexture);
            return new D3D11TextureLease(_liveUiTexture, _uiSourceName);
        }
    }

    public static bool TouchLiveUiTexture(string sourceName)
    {
        lock (Sync)
        {
            if (_liveUiTexture == IntPtr.Zero)
            {
                return false;
            }

            _uiSourceName = sourceName;
            _uiUpdatedMilliseconds = Environment.TickCount64;
            return true;
        }
    }

    public static void ClearLiveUiTexture()
    {
        lock (Sync)
        {
            IntPtr previous = _liveUiTexture;
            _liveUiTexture = IntPtr.Zero;
            _uiSourceName = string.Empty;
            _uiUpdatedMilliseconds = 0;
            if (previous != IntPtr.Zero)
            {
                _ = Marshal.Release(previous);
            }
        }
    }

    public static void UpdateStereoTextures(
        IntPtr leftTexture,
        IntPtr rightTexture,
        bool requiresDynamicUi = false)
    {
        if (leftTexture == IntPtr.Zero || rightTexture == IntPtr.Zero)
        {
            return;
        }

        Guid interfaceId = Id3D11Texture2D;
        int leftResult = Marshal.QueryInterface(
            leftTexture,
            ref interfaceId,
            out IntPtr leftTexture2D);
        interfaceId = Id3D11Texture2D;
        int rightResult = Marshal.QueryInterface(
            rightTexture,
            ref interfaceId,
            out IntPtr rightTexture2D);
        if (leftResult < 0 || rightResult < 0 ||
            leftTexture2D == IntPtr.Zero || rightTexture2D == IntPtr.Zero)
        {
            if (leftTexture2D != IntPtr.Zero)
            {
                _ = Marshal.Release(leftTexture2D);
            }
            if (rightTexture2D != IntPtr.Zero)
            {
                _ = Marshal.Release(rightTexture2D);
            }
            return;
        }

        lock (Sync)
        {
            IntPtr previousLeft = _stereoLeftTexture;
            IntPtr previousRight = _stereoRightTexture;
            _stereoLeftTexture = leftTexture2D;
            _stereoRightTexture = rightTexture2D;
            _stereoUpdatedMilliseconds = Environment.TickCount64;
            _stereoPublishedTimestamp = Stopwatch.GetTimestamp();
            _stereoSequence++;
            _stereoRequiresDynamicUi = requiresDynamicUi;
            if (previousLeft != IntPtr.Zero)
            {
                _ = Marshal.Release(previousLeft);
            }
            if (previousRight != IntPtr.Zero)
            {
                _ = Marshal.Release(previousRight);
            }
        }
    }

    public static D3D11StereoTextureLease? AcquireStereoTextures(int maximumAgeMilliseconds)
    {
        lock (Sync)
        {
            if (_stereoLeftTexture == IntPtr.Zero || _stereoRightTexture == IntPtr.Zero ||
                Environment.TickCount64 - _stereoUpdatedMilliseconds > maximumAgeMilliseconds)
            {
                return null;
            }

            _ = Marshal.AddRef(_stereoLeftTexture);
            _ = Marshal.AddRef(_stereoRightTexture);
            IncrementStereoLeaseCount(_stereoLeftTexture);
            IncrementStereoLeaseCount(_stereoRightTexture);
            return new D3D11StereoTextureLease(
                _stereoLeftTexture,
                _stereoRightTexture,
                _stereoPublishedTimestamp,
                _stereoSequence,
                _stereoRequiresDynamicUi);
        }
    }

    public static bool HasFreshStereoTextures(int maximumAgeMilliseconds)
    {
        lock (Sync)
        {
            return _stereoLeftTexture != IntPtr.Zero &&
                _stereoRightTexture != IntPtr.Zero &&
                Environment.TickCount64 - _stereoUpdatedMilliseconds <= maximumAgeMilliseconds;
        }
    }

    public static bool CanWriteStereoTextures(IntPtr leftTexture, IntPtr rightTexture)
    {
        if (leftTexture == IntPtr.Zero || rightTexture == IntPtr.Zero)
        {
            return false;
        }

        lock (Sync)
        {
            if (leftTexture == _stereoLeftTexture || rightTexture == _stereoRightTexture)
            {
                return false;
            }

            return !StereoLeaseCounts.ContainsKey(leftTexture) &&
                !StereoLeaseCounts.ContainsKey(rightTexture);
        }
    }

    internal static void ReleaseStereoTextureLease(IntPtr leftTexture, IntPtr rightTexture)
    {
        lock (Sync)
        {
            DecrementStereoLeaseCount(leftTexture);
            DecrementStereoLeaseCount(rightTexture);
        }

        if (leftTexture != IntPtr.Zero)
        {
            _ = Marshal.Release(leftTexture);
        }
        if (rightTexture != IntPtr.Zero)
        {
            _ = Marshal.Release(rightTexture);
        }
    }

    private static void IncrementStereoLeaseCount(IntPtr texture)
    {
        StereoLeaseCounts.TryGetValue(texture, out int count);
        StereoLeaseCounts[texture] = checked(count + 1);
    }

    private static void DecrementStereoLeaseCount(IntPtr texture)
    {
        if (texture == IntPtr.Zero || !StereoLeaseCounts.TryGetValue(texture, out int count))
        {
            return;
        }

        if (count <= 1)
        {
            _ = StereoLeaseCounts.Remove(texture);
        }
        else
        {
            StereoLeaseCounts[texture] = count - 1;
        }
    }

    public static bool TouchStereoTextures()
    {
        lock (Sync)
        {
            if (_stereoLeftTexture == IntPtr.Zero || _stereoRightTexture == IntPtr.Zero)
            {
                return false;
            }

            _stereoUpdatedMilliseconds = Environment.TickCount64;
            return true;
        }
    }

    public static void ClearStereoTextures()
    {
        lock (Sync)
        {
            IntPtr previousLeft = _stereoLeftTexture;
            IntPtr previousRight = _stereoRightTexture;
            _stereoLeftTexture = IntPtr.Zero;
            _stereoRightTexture = IntPtr.Zero;
            _stereoUpdatedMilliseconds = 0;
            _stereoPublishedTimestamp = 0;
            _stereoRequiresDynamicUi = false;
            if (previousLeft != IntPtr.Zero)
            {
                _ = Marshal.Release(previousLeft);
            }
            if (previousRight != IntPtr.Zero)
            {
                _ = Marshal.Release(previousRight);
            }
        }
    }
}
