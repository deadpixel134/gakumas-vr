using System.Runtime.InteropServices;

namespace Doorstop;

internal sealed partial class OpenXrControllerActions
{
    private IntPtr _contactHapticAction;
    private ApplyHapticFeedbackDelegate? _contactApplyHaptic;
    private readonly uint _contactHapticThread = NativeMethods.GetCurrentThreadId();

    private void InitializeContactHaptics(IntPtr loader, IntPtr action)
    {
        _contactHapticAction = action;
        if (action == IntPtr.Zero) return;
        try { _contactApplyHaptic = LoadExport<ApplyHapticFeedbackDelegate>(loader, "xrApplyHapticFeedback"); }
        catch (Exception) { Log("contact-haptics-unavailable", "Vibration export unavailable; contact continues."); }
    }

    public void SubmitContactHaptics(int allowedMask)
    {
        var tracking = VrSettingsRuntime.Current.Tracking;
        bool enabled = tracking.ContactCollisionEnabled && tracking.ContactHapticsEnabled &&
            tracking.ContactHapticStrengthPercent > 0 && tracking.ContactHapticDurationMs > 0;
        int mask = OpenXrContactHaptics.Consume(enabled ? allowedMask : 0,
            Environment.TickCount64);
        if (_contactApplyHaptic is null || _disposed || NativeMethods.GetCurrentThreadId() != _contactHapticThread) return;
        for (int bit = 1; bit <= 2; bit <<= 1)
        {
            if ((mask & bit) == 0) continue;
            XrHapticActionInfo info = new() { Type = 59, Action = _contactHapticAction,
                SubactionPath = bit == 1 ? _leftHandPath : _rightHandPath };
            XrHapticVibration vibration = new() { Type = 13, Duration = (long)(tracking.ContactHapticDurationMs * 1_000_000), Frequency = 0,
                Amplitude = tracking.ContactHapticStrengthPercent / 100f };
            int result = _contactApplyHaptic(_session, ref info, ref vibration);
            if (result != XrSuccess)
            {
                Log("contact-haptics-unavailable", $"Vibration failed ({result}); contact and existing inputs continue.");
                _contactApplyHaptic = null;
                return;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XrHapticActionInfo
    {
        public int Type;
        public IntPtr Next;
        public IntPtr Action;
        public ulong SubactionPath;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XrHapticVibration
    {
        public int Type;
        public IntPtr Next;
        public long Duration;
        public float Frequency;
        public float Amplitude;
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate int ApplyHapticFeedbackDelegate(IntPtr session, ref XrHapticActionInfo info, ref XrHapticVibration vibration);
}
