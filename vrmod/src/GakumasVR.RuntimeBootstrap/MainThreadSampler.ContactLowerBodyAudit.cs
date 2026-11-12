using System.Numerics;
using System.Runtime.InteropServices;
using GakumasVR.Core;

namespace Doorstop;

internal sealed partial class MainThreadSampler
{
    private readonly HashSet<int> _contactLowerBodyAuditedActors = new();

    // Uses the existing scan's rooted renderer array after normal chest binding has finished.
    private bool AdvanceContactLowerBodyAudit(DateTimeOffset now, IntPtr coreImage, ContactRigScan scan,
        IntPtr componentClass, IntPtr inParent, IntPtr animatorType, IntPtr getTransform, IntPtr getRoot,
        IntPtr getBones, IntPtr getMesh, IntPtr getName, IntPtr getParent, IntPtr isChild, int eyeLayers)
    {
        if (!_contactCollisionEnabled || !_contactBoneTrialTargets.Any(target =>
                !target.Invalid && !_contactLowerBodyAuditedActors.Contains(target.ActorId)))
            return true;

        int end = Math.Min(scan.LowerBodyCursor + 8, scan.Skins.Count);
        while (scan.LowerBodyCursor < end)
        {
            IntPtr skin = scan.Skins[scan.LowerBodyCursor++];
            if (!ContactTrialAlive(skin) || !ReadContactBool(Invoke(_contactTrialSkinEnabled, skin))) continue;
            IntPtr animator = InvokeWithObjectArgument(inParent, skin, animatorType);
            ContactTrialTarget? target = _contactBoneTrialTargets.FirstOrDefault(t => !t.Invalid && t.Animator == animator);
            if (target is null || _contactLowerBodyAuditedActors.Contains(target.ActorId) || !ContactTrialAlive(animator)) continue;
            IntPtr gameObject = Invoke(FindMethod(componentClass, "get_gameObject"), skin);
            if (gameObject == IntPtr.Zero || !InvokeInstanceBool(coreImage, "UnityEngine", "GameObject", "get_activeInHierarchy", gameObject)) continue;
            int layer = InvokeInstanceInt(coreImage, "UnityEngine", "GameObject", "get_layer", gameObject);
            if (layer is < 0 or > 31 || (eyeLayers & (1 << layer)) == 0) continue;
            IntPtr mesh = Invoke(getMesh, skin);
            IntPtr root = Invoke(getRoot, skin);
            IntPtr skinTransform = Invoke(getTransform, skin);
            if (!ContactTrialAlive(mesh) || !ContactTrialAlive(root) || !ContactTrialAlive(skinTransform) ||
                !ContactChildOf(isChild, root, target.Actor) || !ContactChildOf(isChild, skinTransform, target.Actor)) continue;

            IntPtr bones = Invoke(getBones, skin);
            if (bones == IntPtr.Zero) continue;
            long slotCount = Marshal.ReadInt64(bones, 3 * IntPtr.Size);
            if (slotCount is < 1 or > 512) continue;
            int slots = (int)slotCount;
            // Copy the slots before further Unity calls and retain their original indices.
            var boneSlots = new IntPtr[slots];
            Marshal.Copy(IntPtr.Add(bones, 4 * IntPtr.Size), boneSlots, 0, slots);
            scan.LowerBodyInspectedBones += slots;
            scan.LowerBodySkins++;
            var candidates = new Dictionary<int, string>();
            var labels = new Dictionary<IntPtr, (string Name, IntPtr Parent)>();
            for (int slot = 0; slot < slots; slot++)
            {
                IntPtr bone = boneSlots[slot];
                if (!ContactTrialAlive(bone)) continue;
                var (name, parent) = ReadTopology(bone);
                if (name is not ("LeftUpLegSkin2_S" or "RightUpLegSkin2_S" or "LeftLegSkin2_S" or "RightLegSkin2_S"))
                    continue;
                var ancestry = new List<string>(5);
                for (int depth = 0; depth < 5 && parent != IntPtr.Zero && parent != target.Actor; depth++)
                {
                    var (parentName, next) = ReadTopology(parent);
                    if (!IsSafeContactBoneLabel(parentName)) break;
                    ancestry.Add(parentName);
                    if (IsLowerBodyRoot(parentName)) break;
                    parent = next;
                }
                ancestry.Reverse();
                ancestry.Add(name);
                string path = string.Join("/", ancestry);
                if (ContactRegionForPath(path) is (ContactRegion.Thigh or ContactRegion.Calf) && ContactChildOf(isChild, bone, root))
                    candidates.Add(slot, path);
            }

            int[]? influences = null;
            string weights = candidates.Count == 0 ? "no-candidates" :
                ObserveContactSkinWeights(coreImage, mesh, slots, candidates, out influences);
            LogContactPilot(now, "contact-lower-body-audit-skin",
                $"actor={target.ActorId};skin={scan.LowerBodySkins};slots={slots};candidates={candidates.Count};weights={weights};auditWrites=0");
            if (influences is not null)
            {
                scan.LowerBodyWeightedActors.Add(target.ActorId);
                BindWeightedLowerBodyPatches(now, coreImage, target, skin, mesh, root, skinTransform, boneSlots,
                    candidates, influences, labels);
            }
            break; // Yield after one skin, including the weight pass.

            (string Name, IntPtr Parent) ReadTopology(IntPtr transform)
            {
                if (!labels.TryGetValue(transform, out var value))
                {
                    value = (ReadContactBoneLabel(getName, transform), Invoke(getParent, transform));
                    labels.Add(transform, value);
                }
                return value;
            }
        }

        if (scan.LowerBodyCursor < scan.Skins.Count) return false;
        _contactLowerBodyAuditedActors.UnionWith(scan.LowerBodyWeightedActors);
        LogContactPilot(now, "contact-lower-body-audit-complete",
            $"weightedActors={string.Join(",", scan.LowerBodyWeightedActors.OrderBy(id => id))};totalWeightedActors={_contactLowerBodyAuditedActors.Count};skins={scan.LowerBodySkins};scanned={scan.LowerBodyCursor}/{scan.Skins.Count};bones={scan.LowerBodyInspectedBones};auditWrites=0");
        return true;
    }

    private void BindWeightedLowerBodyPatches(DateTimeOffset now, IntPtr coreImage, ContactTrialTarget owner, IntPtr skin, IntPtr mesh,
        IntPtr root, IntPtr skinTransform, IntPtr[] boneSlots, Dictionary<int, string> candidates, int[] influences,
        Dictionary<IntPtr, (string Name, IntPtr Parent)> topology)
    {
        var boundPaths = _contactBoneTrialTargets.Where(target => !target.Invalid && target.Animator == owner.Animator)
            .Select(target => target.BonePath).ToHashSet(StringComparer.Ordinal);
        var patches = candidates
            .GroupBy(pair => pair.Value).Where(group => group.Count() == 1)
            .Select(group => group.Single())
            .Where(pair => influences[pair.Key] > 0 && !boundPaths.Contains(pair.Value))
            .OrderBy(pair => ContactRegionForPath(pair.Value)).ToArray();
        if (patches.Length == 0) return;
        try
        {
            IntPtr meshClass = FindClass(coreImage, "UnityEngine", "Mesh");
            int count = ReadContactInt(FindMethod(meshClass, "get_vertexCount"), mesh);
            if (count is < 12 or > MaxContactWeightVertices) return;
            byte[] counts = ReadContactNativeArray<byte>(Invoke(FindMethod(meshClass, "GetBonesPerVertex"), mesh), count, count);
            ContactBoneWeight[] weights = ReadContactNativeArray<ContactBoneWeight>(
                Invoke(FindMethod(meshClass, "GetAllBoneWeights"), mesh), count, MaxContactWeightInfluences);
            var (vertices, normals) = BakeContactSurface(coreImage, meshClass, skin, count);
            IntPtr transform = FindClass(coreImage, "UnityEngine", "Transform");
            IntPtr localToWorld = FindMethod(transform, "get_localToWorldMatrix");
            _contactBoneToWorld = localToWorld;
            Matrix4x4 skinToWorld = ReadContactMatrix(Invoke(localToWorld, skinTransform));
            foreach (var pair in patches)
            {
                string path = pair.Value;
                ContactRegion region = ContactRegionForPath(path);
                IntPtr bone = boneSlots[pair.Key];
                int[]? selected = SelectContactSurfaceVertices(counts, weights, pair.Key, boneSlots.Length);
                if (selected is null) { LogRejected("weights"); continue; }
                Matrix4x4 boneToWorld = ReadContactMatrix(Invoke(localToWorld, bone));
                if (!TryGetContactLimbAxis(region, path, bone, topology, out Vector3 origin, out Vector3 axis) ||
                    !TryMeasureThighCoverage(vertices, selected, skinToWorld, origin, axis, out Vector3 range))
                {
                    LogRejected("limb-axis");
                    continue;
                }
                if (region == ContactRegion.Thigh)
                    LogContactPilot(now, "contact-lower-body-audit-thigh",
                        FormattableString.Invariant($"actor={owner.ActorId};side={(path == ContactLeftThighPath ? "Left" : "Right")};bone={path};slot={pair.Key};influences={influences[pair.Key]};samples={selected.Length};hipToKneeMin={range.X:F3};hipToKneeMean={range.Y:F3};hipToKneeMax={range.Z:F3};auditWrites=0"));
                if (!TryFitLowerBodySurface(region, vertices, normals, selected, skinToWorld, boneToWorld, range, out var surface))
                {
                    LogRejected("coverage-or-surface-fit");
                    continue;
                }
                if (BindContactBoneTrial(now, coreImage, owner.Animator, skin, mesh, root, bone, topology[bone].Parent,
                    pair.Key, boneSlots.Length, influences[pair.Key], path, surface) && region == ContactRegion.Calf)
                    LogContactPilot(now, "contact-lower-body-patch-measured",
                        FormattableString.Invariant($"actor={owner.ActorId};region={region};bone={path};samples={selected.Length};axisMin={range.X:F3};axisMean={range.Y:F3};axisMax={range.Z:F3}"));

                void LogRejected(string reason) => LogContactPilot(now, "contact-lower-body-patch-unavailable",
                    $"actor={owner.ActorId};region={region};bone={path};reason={reason}");
            }
        }
        catch (Exception error)
        {
            LogContactPilot(now, "contact-lower-body-patch-unavailable", $"actor={owner.ActorId};error={error.GetType().Name};auditWrites=0");
        }
    }

    private bool TryGetContactLimbAxis(ContactRegion region, string path, IntPtr bone,
        Dictionary<IntPtr, (string Name, IntPtr Parent)> topology, out Vector3 origin, out Vector3 axis)
    {
        origin = axis = default;
        string side = path.StartsWith("Pelvis/Left", StringComparison.Ordinal) ? "Left" : "Right";
        bool thigh = region == ContactRegion.Thigh;
        IntPtr joint = bone;
        for (int depth = 0; depth < (thigh ? 3 : 2) && joint != IntPtr.Zero; depth++)
            joint = topology.TryGetValue(joint, out var node) ? node.Parent : IntPtr.Zero;
        if (!topology.TryGetValue(joint, out var jointNode) || jointNode.Name != side + (thigh ? "UpLeg" : "Leg"))
            return false;
        string endName = side + (thigh ? "Leg" : "Foot");
        IntPtr end = topology.FirstOrDefault(entry => entry.Value.Parent == joint && entry.Value.Name == endName).Key;
        if (end == IntPtr.Zero && !thigh)
            end = topology.FirstOrDefault(entry => entry.Value.Parent == joint && entry.Value.Name == endName + "_H").Key;
        if (end == IntPtr.Zero) return false;
        origin = ReadContactTrialVector(Invoke(_contactTrialWorldPosition, joint));
        axis = ReadContactTrialVector(Invoke(_contactTrialWorldPosition, end)) - origin;
        return true;
    }

    internal static bool TryFitThighSurface(Vector3[] vertices, Vector3[] normals, int[] selected,
        Matrix4x4 skinToWorld, Matrix4x4 boneToWorld, Vector3 coverage, out ContactSurfaceAnchor anchor) =>
        TryFitLowerBodySurface(ContactRegion.Thigh, vertices, normals, selected, skinToWorld, boneToWorld, coverage, out anchor);

    internal static bool TryFitLowerBodySurface(ContactRegion region, Vector3[] vertices, Vector3[] normals, int[] selected,
        Matrix4x4 skinToWorld, Matrix4x4 boneToWorld, Vector3 coverage, out ContactSurfaceAnchor anchor)
    {
        anchor = default;
        // Limb patches stay between the two adjacent joints.
        bool withinLimb = region switch
        {
            ContactRegion.Thigh => coverage.X >= .10f && coverage.Z <= .80f && coverage.Y is >= .25f and <= .65f,
            ContactRegion.Calf => coverage.X >= .08f && coverage.Z <= .90f && coverage.Y is >= .20f and <= .78f,
            _ => false
        };
        if (!withinLimb || !ContactResponse.Finite(coverage) || !Matrix4x4.Invert(skinToWorld, out var worldToSkin))
            return false;

        Matrix4x4 normalMatrix = Matrix4x4.Transpose(worldToSkin);
        var points = new Vector3[selected.Length];
        var directions = new Vector3[selected.Length];
        Vector3 boneOrigin = Vector3.Transform(Vector3.Zero, boneToWorld);
        float neighborhood = region == ContactRegion.Calf ? .24f : float.PositiveInfinity;
        for (int i = 0; i < selected.Length; i++)
        {
            int index = selected[i];
            if ((uint)index >= (uint)vertices.Length || (uint)index >= (uint)normals.Length) return false;
            points[i] = Vector3.Transform(vertices[index], skinToWorld);
            if (Vector3.Distance(points[i], boneOrigin) > neighborhood) return false;
            directions[i] = Vector3.TransformNormal(normals[index], normalMatrix);
        }
        if (!ContactSurfaceFit.TryFit(points, directions, out Vector3 center, out Vector3 outward, out float radius))
        {
            // A limb patch may wrap around the leg. Fit a coherent sector from the measured normals.
            var sectors = new List<int>[6];
            for (int i = 0; i < sectors.Length; i++) sectors[i] = new();
            for (int i = 0; i < selected.Length; i++)
            {
                Vector3 normal = Vector3.Normalize(normals[selected[i]]);
                if (!ContactResponse.Finite(normal)) continue;
                Vector3 absolute = Vector3.Abs(normal);
                int axis = absolute.X >= absolute.Y && absolute.X >= absolute.Z ? 0 : absolute.Y >= absolute.Z ? 1 : 2;
                float component = axis == 0 ? normal.X : axis == 1 ? normal.Y : normal.Z;
                sectors[axis * 2 + (component < 0 ? 1 : 0)].Add(i);
            }
            bool fitted = false;
            foreach (var sector in sectors.OrderByDescending(entries => entries.Count))
            {
                if (sector.Count < 12) break;
                if (ContactSurfaceFit.TryFit(sector.Select(i => points[i]).ToArray(), sector.Select(i => directions[i]).ToArray(),
                        out center, out outward, out radius))
                {
                    fitted = true;
                    break;
                }
            }
            if (!fitted) return false;
        }
        float radiusLimit = region == ContactRegion.Calf ? .032f : .04f;
        float cappedRadius = MathF.Min(radius, radiusLimit);
        // Preserve the fitted outer point when narrowing the trial's collision area.
        center += outward * (radius - cappedRadius);
        return ContactSurfaceAnchor.TryBind(center, outward, cappedRadius, boneToWorld, out anchor);
    }

    internal static bool TryMeasureThighCoverage(Vector3[] vertices, int[] selected, Matrix4x4 skinToWorld,
        Vector3 hip, Vector3 hipToKnee, out Vector3 range)
    {
        range = default;
        float lengthSquared = hipToKnee.LengthSquared();
        if (!float.IsFinite(lengthSquared) || lengthSquared < .0001f || selected.Length == 0) return false;
        float min = float.PositiveInfinity, max = float.NegativeInfinity, sum = 0;
        foreach (int index in selected)
        {
            if ((uint)index >= (uint)vertices.Length) return false;
            float fraction = Vector3.Dot(Vector3.Transform(vertices[index], skinToWorld) - hip, hipToKnee) / lengthSquared;
            if (!float.IsFinite(fraction)) return false;
            min = MathF.Min(min, fraction);
            max = MathF.Max(max, fraction);
            sum += fraction;
        }
        range = new(min, sum / selected.Length, max);
        return true;
    }

    private static bool IsLowerBodyRoot(string name) =>
        name.Equals("Hips", StringComparison.OrdinalIgnoreCase) || name.Equals("Pelvis", StringComparison.OrdinalIgnoreCase);
}
