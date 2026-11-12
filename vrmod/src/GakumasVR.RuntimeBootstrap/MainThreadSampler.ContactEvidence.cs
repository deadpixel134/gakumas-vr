using System.Runtime.InteropServices;

namespace Doorstop;

internal sealed partial class MainThreadSampler
{
#if !GAKUMAS_PUBLIC
    private const int MaxContactWeightVertices = 50_000;
    private const int MaxContactWeightInfluences = 400_000;
#endif
    private int _contactRenderEvidenceGeneration = -1;
    private int _contactRenderEvidencePairs;
    private int _contactRenderSequence;
    private (int Enter, int Exit) _contactSourceScope, _contactLeftScope, _contactRightScope;

#if !GAKUMAS_PUBLIC
    private string ObserveContactSkinWeights(IntPtr coreImage, IntPtr mesh, int boneSlots, Dictionary<int, string> candidates,
        out int[]? verifiedInfluences)
    {
        verifiedInfluences = null;
        try
        {
            IntPtr meshClass = FindClass(coreImage, "UnityEngine", "Mesh");
            int vertices = ReadContactInt(FindMethod(meshClass, "get_vertexCount"), mesh);
            int bindposes = ReadContactInt(FindMethod(meshClass, "get_bindposeCount"), mesh);
            string summary = $"vertices={vertices}/bindposes={bindposes}/slots={boneSlots}";
            if (vertices is < 1 or > MaxContactWeightVertices || bindposes != boneSlots)
                return summary + "/skipped=incompatible-counts";
            IntPtr weightClass = FindClass(coreImage, "UnityEngine", "BoneWeight1");
            var valueSize = Marshal.GetDelegateForFunctionPointer<ContactClassValueSize>(
                NativeLibrary.GetExport(NativeMethods.GetModuleHandle("GameAssembly.dll"), "il2cpp_class_value_size"));
            int stride = valueSize(weightClass, out _);
            int firstField = _api.FieldGetOffset(FindField(weightClass, "m_Weight"));
            if (stride != 8 || (firstField != 0 && firstField != 2 * IntPtr.Size) ||
                _api.FieldGetOffset(FindField(weightClass, "m_BoneIndex")) != firstField + sizeof(float))
                return summary + "/skipped=unknown-weight-layout";

            IntPtr array = Invoke(FindMethod(meshClass, "GetAllBoneWeights"), mesh);
            if (array == IntPtr.Zero) return summary + "/skipped=missing-weights";
            IntPtr arrayClass = Marshal.ReadIntPtr(array);
            if (GetNativeName(arrayClass, _api.ClassGetNamespace) != "Unity.Collections" ||
                GetNativeName(arrayClass, _api.ClassGetName) != "NativeArray`1")
                return summary + "/skipped=unknown-native-array";
            int length = 0;
            unsafe
            {
                _api.FieldGetValue(array, FindField(arrayClass, "m_Length"), (IntPtr)(&length));
                if (length < vertices || length > MaxContactWeightInfluences)
                    return summary + $"/skipped=influence-count-{length}";
                IntPtr buffer = ReadContactFieldPointer(array, FindField(arrayClass, "m_Buffer"));
                if (buffer == IntPtr.Zero || (buffer.ToInt64() & 3) != 0)
                    return summary + "/skipped=missing-weight-buffer";
                // Unity owns this NativeArray view. Read it synchronously on the owner thread.
                var values = new ReadOnlySpan<ContactBoneWeight>((void*)buffer, length);
                int[]? counts = CountContactBoneInfluences(values, boneSlots);
                if (counts is null) return summary + "/skipped=invalid-weight-data";
                verifiedInfluences = counts;
                return summary + $"/influences={length}/matches=" +
                    string.Join(",", candidates.Select(pair => $"{pair.Value}@{pair.Key}:{counts[pair.Key]}"));
            }
        }
        catch (Exception error)
        {
            return "unavailable=" + error.GetType().Name;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly record struct ContactBoneWeight(float Weight, int Index);

    internal static int[]? CountContactBoneInfluences(ReadOnlySpan<ContactBoneWeight> weights, int boneSlots)
    {
        var counts = new int[boneSlots];
        foreach (ContactBoneWeight influence in weights)
        {
            if (!(influence.Weight > 0f && influence.Weight <= 1f) || (uint)influence.Index >= (uint)counts.Length)
                return null;
            counts[influence.Index]++;
        }
        return counts;
    }
#endif

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
