using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using GakumasVR.Core;

namespace Doorstop;

internal sealed partial class MainThreadSampler
{
    private IntPtr _contactBoneToWorld;

    private void CaptureContactSurface(DateTimeOffset now, IntPtr coreImage, ContactTrialTarget target)
    {
        long started = Stopwatch.GetTimestamp();
        string stage = "abi", bakeFailure = "none";
        try
        {
            var valueSize = Marshal.GetDelegateForFunctionPointer<ContactClassValueSize>(
                NativeLibrary.GetExport(NativeMethods.GetModuleHandle("GameAssembly.dll"), "il2cpp_class_value_size"));
            if (valueSize(FindClass(coreImage, "UnityEngine", "Vector3"), out _) != 12 ||
                valueSize(FindClass(coreImage, "UnityEngine", "Matrix4x4"), out _) != 64 ||
                valueSize(FindClass(coreImage, "UnityEngine", "BoneWeight1"), out _) != 8)
                throw new InvalidOperationException("Surface ABI layout.");
            IntPtr transformClass = FindClass(coreImage, "UnityEngine", "Transform");
            _contactBoneToWorld = FindMethod(transformClass, "get_localToWorldMatrix");
            Matrix4x4 boneToWorld = ReadContactMatrix(Invoke(_contactBoneToWorld, target.Bone));
            IntPtr meshClass = FindClass(coreImage, "UnityEngine", "Mesh");
            stage = "weights";
            int count = ReadContactInt(FindMethod(meshClass, "get_vertexCount"), target.Mesh);
            if (count is < 12 or > MaxContactWeightVertices) throw new InvalidOperationException("Surface vertex budget.");
            byte[] perVertex = ReadContactNativeArray<byte>(Invoke(FindMethod(meshClass, "GetBonesPerVertex"), target.Mesh), count, count);
            ContactBoneWeight[] weights = ReadContactNativeArray<ContactBoneWeight>(
                Invoke(FindMethod(meshClass, "GetAllBoneWeights"), target.Mesh), count, MaxContactWeightInfluences);
            int[] selected = SelectContactSurfaceVertices(perVertex, weights, target.Slot, target.Slots)
                ?? throw new InvalidOperationException("Surface weight mapping.");
            Vector3[] vertices, normals;
            Matrix4x4 meshToWorld;
            string source;
            try
            {
                stage = "bake";
                (vertices, normals) = BakeContactSurface(coreImage, meshClass, target.Skin, count);
                meshToWorld = ReadContactMatrix(Invoke(_contactBoneToWorld, Invoke(_contactTrialGetTransform, target.Skin)));
                source = "baked-weighted-patch";
            }
            catch (Exception error)
            {
                bakeFailure = error.GetType().Name; stage = "readable-fallback";
                // Read-only fallback for readable meshes when the BakeMesh wrapper is stripped.
                if (!ReadContactBool(Invoke(FindMethod(meshClass, "get_isReadable"), target.Mesh))) throw;
                vertices = ReadContactManagedArray<Vector3>(Invoke(FindMethod(meshClass, "get_vertices"), target.Mesh), count);
                normals = ReadContactManagedArray<Vector3>(Invoke(FindMethod(meshClass, "get_normals"), target.Mesh), count);
                Matrix4x4[] bindposes = ReadContactManagedArray<Matrix4x4>(Invoke(FindMethod(meshClass, "get_bindposes"), target.Mesh), target.Slots);
                meshToWorld = bindposes[target.Slot] * boneToWorld;
                source = "readable-bindpose-patch";
            }
            stage = "fit";
            if (!Matrix4x4.Invert(meshToWorld, out var inverse)) throw new InvalidOperationException("Surface matrix inverse.");
            Matrix4x4 normalMatrix = Matrix4x4.Transpose(inverse);
            var points = new Vector3[selected.Length];
            var directions = new Vector3[selected.Length];
            Vector3 boneOrigin = Vector3.Transform(Vector3.Zero, boneToWorld);
            for (int i = 0; i < selected.Length; i++)
            {
                points[i] = Vector3.Transform(vertices[selected[i]], meshToWorld);
                directions[i] = Vector3.TransformNormal(normals[selected[i]], normalMatrix);
                if (Vector3.Distance(points[i], boneOrigin) > .3f) throw new InvalidOperationException("Surface outside bone neighborhood.");
            }
            if (!ContactSurfaceFit.TryFit(points, directions, out var center, out var outward, out float radius) ||
                !ContactSurfaceAnchor.TryBind(center, outward, radius, boneToWorld, out var anchor))
                throw new InvalidOperationException("Surface fit is ambiguous.");
            target.Surface = anchor;
            target.SurfaceSource = source;
            LogContactPilot(now, "contact-surface-bound",
                $"actor={target.ActorId};target={target.Id};bonePath={target.BonePath};source={source};bakeFailure={bakeFailure};samples={selected.Length};center={Position(center)};outward={Position(outward)};radius={radius:F4};boneOffset={Vector3.Distance(center, boneOrigin):F4};scanMs={(Stopwatch.GetTimestamp() - started) * 1000d / Stopwatch.Frequency:F2}");
        }
        catch (Exception error)
        {
            target.SurfaceSource = "bone-fallback";
            LogContactPilot(now, "contact-surface-unavailable", $"actor={target.ActorId};target={target.Id};bonePath={target.BonePath};stage={stage};error={error.GetType().Name};bakeFailure={bakeFailure};fallback=existing-bone-sphere");
        }
    }

    private (Vector3[], Vector3[]) BakeContactSurface(IntPtr coreImage, IntPtr meshClass, IntPtr skin, int count)
    {
        IntPtr skinClass = FindClass(coreImage, "UnityEngine", "SkinnedMeshRenderer");
        IntPtr bake;
        bool hasScale = false;
        try { bake = FindMethodBySignature(skinClass, "BakeMesh", "UnityEngine.Mesh"); }
        catch (MissingMethodException)
        { bake = FindMethodBySignature(skinClass, "BakeMesh", "UnityEngine.Mesh", "System.Boolean"); hasScale = true; }
        // Resolve destruction before allocating a temporary native resource.
        IntPtr destroy;
        bool destroyWithDelay = false;
        try { destroy = FindMethodBySignature(FindClass(coreImage, "UnityEngine", "Object"), "Destroy", "UnityEngine.Object"); }
        catch (MissingMethodException)
        {
            destroy = FindMethodBySignature(FindClass(coreImage, "UnityEngine", "Object"), "Destroy", "UnityEngine.Object", "System.Single");
            destroyWithDelay = true;
        }
        IntPtr snapshot = IntPtr.Zero, handle = IntPtr.Zero;
        try
        {
            snapshot = _api.ObjectNew(meshClass);
            handle = RootObject(snapshot, "contact surface snapshot");
            _ = Invoke(FindMethod(meshClass, ".ctor", 0), snapshot);
            if (hasScale) _ = InvokeWithObjectArguments(bake, skin, snapshot, _contactTrialFalse);
            else _ = InvokeWithObjectArgument(bake, skin, snapshot);
            return (ReadContactManagedArray<Vector3>(Invoke(FindMethod(meshClass, "get_vertices"), snapshot), count),
                ReadContactManagedArray<Vector3>(Invoke(FindMethod(meshClass, "get_normals"), snapshot), count));
        }
        finally
        {
            try
            {
                if (snapshot != IntPtr.Zero)
                {
                    if (destroyWithDelay) _ = InvokeWithObjectArguments(destroy, IntPtr.Zero, snapshot, BoxSingle(FindImage("mscorlib.dll"), 0));
                    else _ = InvokeWithObjectArgument(destroy, IntPtr.Zero, snapshot);
                }
            }
            finally { if (handle != IntPtr.Zero) _contactTrialFreeHandle!(handle); }
        }
    }

    private Matrix4x4 ReadContactMatrix(IntPtr boxed)
    {
        IntPtr data = boxed == IntPtr.Zero ? IntPtr.Zero : _api.ObjectUnbox(boxed);
        if (data == IntPtr.Zero) throw new InvalidOperationException("Missing contact matrix.");
        // Unity's column-major field layout is Numerics' row-vector transpose in memory.
        return Marshal.PtrToStructure<Matrix4x4>(data);
    }

    private static unsafe T[] ReadContactManagedArray<T>(IntPtr array, int expected) where T : unmanaged
    {
        if (array == IntPtr.Zero || Marshal.ReadInt64(array, 3 * IntPtr.Size) != expected || expected is < 1 or > MaxContactWeightVertices)
            throw new InvalidOperationException("Unexpected surface array length.");
        return new ReadOnlySpan<T>((void*)IntPtr.Add(array, 4 * IntPtr.Size), expected).ToArray();
    }

    private unsafe T[] ReadContactNativeArray<T>(IntPtr array, int min, int max) where T : unmanaged
    {
        if (array == IntPtr.Zero) throw new InvalidOperationException("Missing surface weights.");
        IntPtr klass = Marshal.ReadIntPtr(array);
        if (GetNativeName(klass, _api.ClassGetNamespace) != "Unity.Collections" ||
            GetNativeName(klass, _api.ClassGetName) != "NativeArray`1") throw new InvalidOperationException("Unexpected surface native array.");
        int length = 0;
        _api.FieldGetValue(array, FindField(klass, "m_Length"), (IntPtr)(&length));
        IntPtr buffer = ReadContactFieldPointer(array, FindField(klass, "m_Buffer"));
        if (length < min || length > max || buffer == IntPtr.Zero) throw new InvalidOperationException("Surface native array bounds.");
        // Copy immediately; do not retain Unity-owned NativeArray buffers across calls/frames.
        return new ReadOnlySpan<T>((void*)buffer, length).ToArray();
    }

    internal static int[]? SelectContactSurfaceVertices(ReadOnlySpan<byte> counts, ReadOnlySpan<ContactBoneWeight> weights,
        int slot, int slots)
    {
        if (counts.Length is < 12 or > MaxContactWeightVertices || slot < 0 || slot >= slots) return null;
        var selectedWeight = new float[counts.Length];
        int cursor = 0;
        float maximum = 0;
        for (int vertex = 0; vertex < counts.Length; vertex++)
        {
            if (counts[vertex] == 0 || cursor + counts[vertex] > weights.Length) return null;
            float total = 0;
            for (int i = 0; i < counts[vertex]; i++)
            {
                var weight = weights[cursor++];
                if (!(weight.Weight > 0 && weight.Weight <= 1) || (uint)weight.Index >= (uint)slots) return null;
                total += weight.Weight;
                if (weight.Index == slot) selectedWeight[vertex] += weight.Weight;
            }
            if (MathF.Abs(total - 1) > .02f) return null;
            maximum = MathF.Max(maximum, selectedWeight[vertex]);
        }
        if (cursor != weights.Length || maximum < .01f) return null;
        var selected = new List<int>();
        for (int i = 0; i < selectedWeight.Length; i++)
            if (selectedWeight[i] >= maximum * .5f) selected.Add(i);
        if (selected.Count < 12) return null;
        if (selected.Count <= 4096) return selected.ToArray();
        return Enumerable.Range(0, 4096).Select(i => selected[(int)((long)i * (selected.Count - 1) / 4095)]).ToArray();
    }
}
