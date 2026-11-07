using System.Runtime.InteropServices;

namespace Doorstop;

internal sealed partial class MainThreadSampler
{
    private int _contactRenderEvidenceGeneration = -1;
    private int _contactRenderEvidencePairs;
    private int _contactRenderSequence;
    private (int Enter, int Exit) _contactSourceScope, _contactLeftScope, _contactRightScope;


    private IntPtr BeginContactRenderEvidence(IntPtr renderingData)
    {
        if (_contactRenderEvidenceGeneration == _stereoGenerationRetireCount ||
            !_showContactHands || !_stereoOutputValidated || !_stereoNaturalRenderArmed ||
            NativeMethods.GetCurrentThreadId() != _contactUnityThread)
            return IntPtr.Zero;
        return TryReadRenderingDataCamera(renderingData);
    }

    private void EndContactRenderEvidence(IntPtr camera, int enter)
    {
        if (camera == IntPtr.Zero) return;
        var scope = (enter, ++_contactRenderSequence);
        if (camera == _stereoLeftCamera) _contactLeftScope = scope;
        else if (camera == _stereoRightCamera) _contactRightScope = scope;
        else if (camera == _lastLiveCamera) _contactSourceScope = scope;
    }

    private void ResetContactRenderEvidencePair()
    {
        _contactRenderSequence = 0;
        _contactSourceScope = _contactLeftScope = _contactRightScope = default;
    }

    private void ReportContactRenderEvidence(DateTimeOffset now)
    {
        if (_contactRenderEvidenceGeneration == _stereoGenerationRetireCount ||
            !_showContactHands || !_stereoOutputValidated || _contactRendererOriginal is null)
            return;
        bool observedAll = _contactSourceScope.Exit != 0 && _contactLeftScope.Exit != 0 && _contactRightScope.Exit != 0;
        if (!observedAll && ++_contactRenderEvidencePairs < 3)
            return;
        _contactRenderEvidencePairs = 0;
        _contactRenderEvidenceGeneration = _stereoGenerationRetireCount;
        bool separated = observedAll &&
            !Overlaps(_contactSourceScope, _contactLeftScope) && !Overlaps(_contactSourceScope, _contactRightScope) &&
            !Overlaps(_contactLeftScope, _contactRightScope);
        LogContactPilot(now, "contact-pilot-render-order",
            $"owner=unity;source={_contactSourceScope};left={_contactLeftScope};right={_contactRightScope};separate={separated};skinning=unverified;writes=0");

        static bool Overlaps((int Enter, int Exit) a, (int Enter, int Exit) b) =>
            a.Enter < b.Exit && b.Enter < a.Exit;
    }
}
