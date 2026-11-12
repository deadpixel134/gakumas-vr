using GakumasVR.Core;

namespace Doorstop;

internal sealed partial class MainThreadSampler
{
    internal const string ContactLeftBonePath = "Spine2/LeftBust1_S";
    internal const string ContactRightBonePath = "Spine2/RightBust1_S";
    internal const string ContactLeftThighPath = "Pelvis/LeftUpLeg/LeftUpLeg_Roll_H/LeftUpLegSkin1_S/LeftUpLegSkin2_S";
    internal const string ContactRightThighPath = "Pelvis/RightUpLeg/RightUpLeg_Roll_H/RightUpLegSkin1_S/RightUpLegSkin2_S";
    internal const string ContactLeftCalfPath = "Pelvis/LeftUpLeg/LeftLeg/LeftLegSkin1_S/LeftLegSkin2_S";
    internal const string ContactRightCalfPath = "Pelvis/RightUpLeg/RightLeg/RightLegSkin1_S/RightLegSkin2_S";
    private static readonly string[] ContactBonePaths = { ContactLeftBonePath, ContactRightBonePath };

    internal readonly record struct ContactRigKey(IntPtr Animator, IntPtr Bone, string Path);

    internal static bool CanBindContactRig(IReadOnlyCollection<ContactRigKey> existing,
        IntPtr animator, IntPtr bone, string path, bool verifiedSurface = false)
    {
        ContactRegion region = ContactRegionForPath(path);
        bool supported = region == ContactRegion.Chest || (verifiedSurface && region != ContactRegion.Unknown);
        if (animator == IntPtr.Zero || bone == IntPtr.Zero || !supported ||
            existing.Any(key => key.Bone == bone || (key.Animator == animator && key.Path == path))) return false;
        return existing.Any(key => key.Animator == animator) ||
            existing.Select(key => key.Animator).Distinct().Count() < MaxContactTrialActors;
    }

    internal static ContactRegion ContactRegionForPath(string path) => path switch
    {
        ContactLeftBonePath or ContactRightBonePath => ContactRegion.Chest,
        ContactLeftThighPath or ContactRightThighPath => ContactRegion.Thigh,
        ContactLeftCalfPath or ContactRightCalfPath => ContactRegion.Calf,
        _ => ContactRegion.Unknown
    };

    internal static (int Slot, string Path)[] SelectContactRigSlots(IReadOnlyDictionary<int, string> candidates,
        IReadOnlyList<int> influences, IReadOnlyCollection<string> alreadyBound)
    {
        var selected = new List<(int Slot, string Path)>(2);
        foreach (string path in ContactBonePaths)
        {
            if (alreadyBound.Contains(path)) continue;
            int[] slots = candidates.Where(pair => pair.Value == path).Select(pair => pair.Key).ToArray();
            if (slots.Length != 1 || slots[0] < 0 || slots[0] >= influences.Count || influences[slots[0]] <= 0) continue;
            selected.Add((slots[0], path));
        }
        return selected.ToArray();
    }

    private bool ContactActorHasBothSides(IntPtr animator) => ContactBonePaths.All(path =>
        _contactBoneTrialTargets.Any(target => !target.Invalid && target.Animator == animator && target.BonePath == path));
}
