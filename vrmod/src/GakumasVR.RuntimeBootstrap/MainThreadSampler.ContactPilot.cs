using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using GakumasVR.Core;

namespace Doorstop;

internal sealed partial class MainThreadSampler
{
    private const float ContactMarkerDisplayScale = 0.8f;
    private bool _showContactHands;
    private bool _contactHandsFailed;
    private bool _contactHandsLogged;
    private DateTimeOffset _nextContactShaderProbeUtc;
    private int _contactShaderProbeAttempts;
    private string _contactShaderName = string.Empty;
    private readonly record struct ContactMarkerAsset(IntPtr Object, IntPtr Root);
    private ContactMarkerAsset _contactMeshAsset, _contactMaterialAsset;
    private IntPtr _contactMesh => _contactMeshAsset.Object;
    private IntPtr _contactMaterial => _contactMaterialAsset.Object;
    private IntPtr _contactTrs, _contactMarkerObjectClass, _contactMarkerAlive, _contactMarkerSetHideFlags, _contactMarkerKeepFlag;
    private ContactFreeHandle? _contactMarkerFreeHandle;
    private bool _contactCheckMarkerAssets;
    private IntPtr _contactVectorClass, _contactOrientation;
    private IntPtr _contactSceneDraw, _contactSceneLayer, _contactSceneSubmesh, _contactSceneShadows, _contactSceneReceiveShadows, _contactSceneLightProbes;
    private int _contactMarkerGeneration = -1;
    private IntPtr _contactResetView, _contactSetNearClip, _contactViewportPoint;
    private bool _contactEyeProjectionFailed;
    private bool _contactProjectionProbeFailed;
    private DateTimeOffset _nextContactProjectionProbeUtc;


    private float PrepareContactPilotEyeProjection(DateTimeOffset now, IntPtr coreImage, IntPtr cameraClass,
        float originalNear, float eyeScale)
    {
        if (_contactEyeProjectionFailed) return originalNear;
        float near = ContactHandAlignment.PilotNearClip(originalNear, eyeScale);
        try
        {
            if (_contactResetView == IntPtr.Zero)
            {
                _contactSetNearClip = FindMethodBySignature(cameraClass, "set_nearClipPlane", "System.Single");
                _contactResetView = FindMethod(cameraClass, "ResetWorldToCameraMatrix");
            }
            // CopyFrom may leave a source-camera view override on an eye clone.
            _ = Invoke(_contactResetView, _stereoLeftCamera);
            _ = Invoke(_contactResetView, _stereoRightCamera);
            if (near < originalNear)
            {
                IntPtr value = BoxSingle(FindImage("mscorlib.dll"), near);
                _ = InvokeWithObjectArgument(_contactSetNearClip, _stereoLeftCamera, value);
                _ = InvokeWithObjectArgument(_contactSetNearClip, _stereoRightCamera, value);
            }
            return near;
        }
        catch (Exception error)
        {
            _contactEyeProjectionFailed = true;
            // Restore only the near-plane property. CopyFrom would replace the eye render targets.
            if (_contactSetNearClip != IntPtr.Zero && near < originalNear)
            {
                try
                {
                    IntPtr value = BoxSingle(FindImage("mscorlib.dll"), originalNear);
                    _ = InvokeWithObjectArgument(_contactSetNearClip, _stereoLeftCamera, value);
                    _ = InvokeWithObjectArgument(_contactSetNearClip, _stereoRightCamera, value);
                }
                catch { }
            }
            LogContactPilot(now, "contact-pilot-eye-clip-unavailable", error.GetType().Name);
            return originalNear;
        }
    }

    private bool TryReadContactEyePoses(IntPtr transformClass, IntPtr left, IntPtr right,
        out TrackingVector3 leftPosition, out TrackingVector3 rightPosition,
        out TrackingQuaternion leftRotation, out TrackingQuaternion rightRotation)
    {
        rightPosition = default;
        rightRotation = default;
        try
        {
            return TryReadWorldPose(transformClass, left, out leftPosition, out leftRotation) &&
                TryReadWorldPose(transformClass, right, out rightPosition, out rightRotation);
        }
        catch (Exception)
        {
            leftPosition = default;
            leftRotation = default;
            return false;
        }
    }

    private void PrepareContactHands(DateTimeOffset now, IntPtr coreImage, OpenXrStereoStateSnapshot renderStereo,
        OpenXrStereoStateSnapshot contactStereo,
        TrackingVector3 leftEye, TrackingVector3 rightEye, TrackingQuaternion leftRotation, TrackingQuaternion rightRotation, float nearClip)
    {
        float holdSeconds = .1f;
        float radiusMeters = ContactHandAlignment.HandRadiusMeters;
        bool showTrialMarkers = false;
        long age = Environment.TickCount64 - contactStereo.CapturedMilliseconds;
        OpenXrContactHands hands = _showContactHands && age >= 0 && age <= holdSeconds * 1000f
            ? contactStereo.ContactHands : default;
        if ((hands.Left is null && hands.Right is null && !showTrialMarkers) ||
            !ContactHandAlignment.TryCreate(ToTrackingStereoPose(contactStereo), AsVector(leftEye), AsVector(rightEye),
                AsQuaternion(leftRotation), AsQuaternion(rightRotation), out ContactHandAlignment alignment))
            return;

        try
        {
            if (_contactMarkerGeneration != _stereoGenerationRetireCount)
            {
                _contactMarkerGeneration = _stereoGenerationRetireCount;
                _contactHandsLogged = false;
                _contactHandsFailed = false;
                _contactCompositionFailed = false;
                _contactFinalOverlayFailed = false;
                _contactCheckMarkerAssets = true;
                _contactSceneLayer = IntPtr.Zero;
                _contactShaderProbeAttempts = 0;
                _nextContactShaderProbeUtc = now;
            }
            if (_contactCheckMarkerAssets)
            {
                _contactCheckMarkerAssets = false;
                bool expired = DropExpiredContactMarkerAsset(ref _contactMeshAsset) |
                    DropExpiredContactMarkerAsset(ref _contactMaterialAsset);
                if (expired)
                {
                    _contactHandsFailed = _contactCompositionFailed = _contactFinalOverlayFailed = false;
                    _contactHandsLogged = false;
                    _contactShaderProbeAttempts = 0;
                    _nextContactShaderProbeUtc = now;
                    LogContactPilot(now, "contact-marker-assets-expired", "Rebuilding released Unity marker resources.");
                }
            }
            if (_contactHandsFailed) return;
            if (!EnsureContactMarker(now, coreImage)) return;
            float radius = alignment.Scale * radiusMeters * ContactMarkerDisplayScale;
            Vector3? left = hands.Left is TrackingVector3 l && alignment.TryMap(l, out Vector3 lp) ? lp : null;
            Vector3? right = hands.Right is TrackingVector3 r && alignment.TryMap(r, out Vector3 rp) ? rp : null;
            if (left is null && right is null) return;
            var frame = new ContactHandFrame(renderStereo, left, right, radius, alignment.Scale, nearClip);
            if (!_contactFinalOverlayFailed)
            {
                try { EnsureContactOverlayCommand(coreImage); }
                catch (Exception error)
                {
                    _contactFinalOverlayFailed = true;
                    DisableContactComposition("initialize-command: " + error.Message.Split('\n')[0]);
                }
            }
            if (!_contactCompositionFailed)
            {
                try { EnsureContactComposition(coreImage); }
                catch (Exception error) { DisableContactComposition("initialize-renderer: " + error.Message.Split('\n')[0]); }
            }
            if (_contactCompositionFailed && _contactFinalOverlayFailed)
                DrawContactHandsInScene(now, coreImage, frame);
            else
                _contactHandFrame = frame;
            if (left is not null || right is not null)
                ProbeContactProjection(now, coreImage, frame, alignment, contactStereo);
        }
        catch (Exception error)
        {
            _contactHandsFailed = true;
            LogContactPilot(now, "contact-pilot-hands-unavailable", error.GetType().Name + ": " + error.Message.Split('\n')[0]);
        }
    }

    private void DrawContactHandsInScene(DateTimeOffset now, IntPtr coreImage, ContactHandFrame frame)
    {
        if (_contactSceneDraw == IntPtr.Zero)
        {
            IntPtr mscorlib = FindImage("mscorlib.dll");
            _contactSceneSubmesh = RootContactValue(BoxInt32(mscorlib, 0));
            _contactSceneShadows = RootContactValue(BoxContactAction(coreImage, "ShadowCastingMode", 0));
            _contactSceneReceiveShadows = RootContactValue(BoxBoolean(mscorlib, false));
            _contactSceneLightProbes = RootContactValue(BoxContactAction(coreImage, "LightProbeUsage", 0));
            _contactSceneDraw = FindMethodBySignature(FindClass(coreImage, "UnityEngine", "Graphics"), "DrawMesh",
                "UnityEngine.Mesh", "UnityEngine.Matrix4x4", "UnityEngine.Material", "System.Int32", "UnityEngine.Camera",
                "System.Int32", "UnityEngine.MaterialPropertyBlock", "UnityEngine.Rendering.ShadowCastingMode",
                "System.Boolean", "UnityEngine.Transform", "UnityEngine.Rendering.LightProbeUsage", "UnityEngine.LightProbeProxyVolume");
        }
        if (_contactSceneLayer == IntPtr.Zero)
        {
            uint layers = unchecked((uint)(InvokeInstanceInt(coreImage, "UnityEngine", "Camera", "get_cullingMask", _stereoLeftCamera) &
                InvokeInstanceInt(coreImage, "UnityEngine", "Camera", "get_cullingMask", _stereoRightCamera)));
            if (layers == 0) return;
            _contactSceneLayer = RootContactValue(BoxInt32(FindImage("mscorlib.dll"), BitOperations.TrailingZeroCount(layers)));
        }
        IntPtr size = BoxContactVector(new Vector3(frame.Radius));
        DrawMarker(frame.Left, size, _contactMaterial);
        DrawMarker(frame.Right, size, _contactMaterial);
        if (!_contactHandsLogged)
        {
            _contactHandsLogged = true;
            LogContactPilot(now, "contact-pilot-hands-submitted",
                $"left={frame.Left.HasValue};right={frame.Right.HasValue};displayRadius={frame.Radius:R};shader={_contactShaderName};cameraTargets=stereo-eyes;phase=scene-fallback");
        }

        void DrawMarker(Vector3? center, IntPtr markerSize, IntPtr material)
        {
            if (center is not Vector3 position) return;
            IntPtr matrix = InvokeWithObjectArguments(_contactTrs, IntPtr.Zero, BoxContactVector(position), _contactOrientation, markerSize);
            _ = InvokeWithObjectArguments(_contactSceneDraw, IntPtr.Zero, _contactMesh, matrix, material,
                _contactSceneLayer, _stereoLeftCamera, _contactSceneSubmesh, IntPtr.Zero, _contactSceneShadows,
                _contactSceneReceiveShadows, IntPtr.Zero, _contactSceneLightProbes, IntPtr.Zero);
            _ = InvokeWithObjectArguments(_contactSceneDraw, IntPtr.Zero, _contactMesh, matrix, material,
                _contactSceneLayer, _stereoRightCamera, _contactSceneSubmesh, IntPtr.Zero, _contactSceneShadows,
                _contactSceneReceiveShadows, IntPtr.Zero, _contactSceneLightProbes, IntPtr.Zero);
        }
    }

    private void ProbeContactProjection(DateTimeOffset now, IntPtr coreImage, ContactHandFrame frame,
        ContactHandAlignment alignment, OpenXrStereoStateSnapshot stereo)
    {
        if (_contactProjectionProbeFailed || now < _nextContactProjectionProbeUtc) return;
        _nextContactProjectionProbeUtc = now.AddSeconds(1);
        try
        {
            float nearClip = frame.NearClip;
            if (_contactViewportPoint == IntPtr.Zero)
                _contactViewportPoint = FindMethodBySignature(FindClass(coreImage, "UnityEngine", "Camera"),
                    "WorldToViewportPoint", "UnityEngine.Vector3");

            string leftHandLeftEye = ContactProjectionSample(stereo.ContactHands.Left, stereo.Left, _stereoLeftCamera, alignment, nearClip);
            string leftHandRightEye = ContactProjectionSample(stereo.ContactHands.Left, stereo.Right, _stereoRightCamera, alignment, nearClip);
            string rightHandLeftEye = ContactProjectionSample(stereo.ContactHands.Right, stereo.Left, _stereoLeftCamera, alignment, nearClip);
            string rightHandRightEye = ContactProjectionSample(stereo.ContactHands.Right, stereo.Right, _stereoRightCamera, alignment, nearClip);
            string reach = string.Empty;
            LogContactPilot(now, "contact-pilot-hand-projection",
                $"scale={alignment.Scale:F4};near={nearClip:F4};leftHandLeftEye={leftHandLeftEye};leftHandRightEye={leftHandRightEye};rightHandLeftEye={rightHandLeftEye};rightHandRightEye={rightHandRightEye}{reach}");
        }
        catch (Exception error)
        {
            _contactProjectionProbeFailed = true;
            LogContactPilot(now, "contact-pilot-hand-projection-unavailable", error.GetType().Name);
        }
    }

    private string ContactProjectionSample(TrackingVector3? hand, OpenXrEyeState eye, IntPtr camera,
        ContactHandAlignment alignment, float nearClip)
    {
        if (hand is not TrackingVector3 tracked || !alignment.TryMap(tracked, out Vector3 world)) return "untracked";
        if (!ContactHandAlignment.TryProjectRawHand(tracked, ToTrackingEyePose(eye), eye.FovLeft,
                eye.FovRight, eye.FovDown, eye.FovUp, out Vector3 expected)) return "behind-eye";
        IntPtr boxed = InvokeWithObjectArgument(_contactViewportPoint, camera, BoxContactVector(world));
        IntPtr data = boxed == IntPtr.Zero ? IntPtr.Zero : _api.ObjectUnbox(boxed);
        if (data == IntPtr.Zero) return "viewport-unavailable";
        UiReplayVector3 actual = Marshal.PtrToStructure<UiReplayVector3>(data);
        return $"rawDepth={expected.Z:F3},eyeDepth={actual.Z:F3},du={actual.X - expected.X:F3},dv={actual.Y - expected.Y:F3},nearClipped={actual.Z < nearClip}";
    }

    private bool EnsureContactMarker(DateTimeOffset now, IntPtr coreImage)
    {
        if (_contactMesh != IntPtr.Zero && _contactMaterial != IntPtr.Zero) return true;
        if (_contactShaderProbeAttempts >= 3 || now < _nextContactShaderProbeUtc) return false;
        _contactShaderProbeAttempts++;
        _nextContactShaderProbeUtc = now.AddSeconds(2);
        if (_contactMaterial == IntPtr.Zero)
        {
            IntPtr shader = FindContactMarkerShader(coreImage);
            if (shader == IntPtr.Zero)
            {
                LogContactPilot(now, "contact-pilot-shader-unavailable", $"attempt={_contactShaderProbeAttempts};supportedCandidates=0");
                return false;
            }
            IntPtr materialClass = FindClass(coreImage, "UnityEngine", "Material");
            _contactMaterialAsset = CreateContactMarkerAsset(coreImage, materialClass, "contact marker Material", material =>
            {
                _ = InvokeWithObjectArgument(FindMethodBySignature(materialClass, ".ctor", "UnityEngine.Shader"), material, shader);
                ConfigureContactMarkerMaterial(coreImage, materialClass, material);
            });
        }
        if (_contactTrs == IntPtr.Zero)
            _contactTrs = FindMethodBySignature(FindClass(coreImage, "UnityEngine", "Matrix4x4"), "TRS",
                "UnityEngine.Vector3", "UnityEngine.Quaternion", "UnityEngine.Vector3");
        if (_contactOrientation == IntPtr.Zero)
            _contactOrientation = RootContactValue(BoxQuaternion(coreImage, new TrackingQuaternion(0f, 0f, 0f, 1f)));
        if (_contactMesh == IntPtr.Zero)
        {
            IntPtr meshClass = FindClass(coreImage, "UnityEngine", "Mesh");
            _contactMeshAsset = CreateContactMarkerAsset(coreImage, meshClass, "contact marker Mesh",
                mesh => InitializeContactMarkerMesh(coreImage, meshClass, mesh));
        }
        return true;
    }

    private void InitializeContactMarkerMesh(IntPtr coreImage, IntPtr meshClass, IntPtr mesh)
    {
        _ = Invoke(FindMethod(meshClass, ".ctor"), mesh);
        var arrayNew = Marshal.GetDelegateForFunctionPointer<ContactArrayNew>(
            NativeLibrary.GetExport(NativeMethods.GetModuleHandle("GameAssembly.dll"), "il2cpp_array_new"));
        List<Vector3> vertices = new();
        Vector3[] ring = { Vector3.UnitX, -Vector3.UnitZ, -Vector3.UnitX, Vector3.UnitZ };
        for (int i = 0; i < ring.Length; i++)
        {
            Vector3 next = ring[(i + 1) % ring.Length];
            AddContactSphereFace(vertices, Vector3.UnitY, ring[i], next, 2);
            AddContactSphereFace(vertices, -Vector3.UnitY, next, ring[i], 2);
        }
        _contactVectorClass = FindClass(coreImage, "UnityEngine", "Vector3");
        IntPtr vertexArray = arrayNew(_contactVectorClass, (nuint)vertices.Count);
        if (vertexArray == IntPtr.Zero) throw new InvalidOperationException("Could not allocate contact marker vertices.");
        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 vertex = vertices[i];
            Marshal.StructureToPtr(new UiReplayVector3 { X = vertex.X, Y = vertex.Y, Z = vertex.Z },
                IntPtr.Add(vertexArray, 4 * IntPtr.Size + i * 3 * sizeof(float)), false);
        }
        _ = InvokeWithObjectArgument(FindMethod(meshClass, "set_vertices", 1), mesh, vertexArray);
        _ = InvokeWithObjectArgument(FindMethod(meshClass, "set_normals", 1), mesh, vertexArray);
        IntPtr colorArray = arrayNew(FindClass(coreImage, "UnityEngine", "Color"), (nuint)vertices.Count);
        if (colorArray == IntPtr.Zero) throw new InvalidOperationException("Could not allocate contact marker colors.");
        var white = new UiReplayColor { Red = 1f, Green = 1f, Blue = 1f, Alpha = 1f };
        for (int i = 0; i < vertices.Count; i++)
            Marshal.StructureToPtr(white, IntPtr.Add(colorArray, 4 * IntPtr.Size + i * 4 * sizeof(float)), false);
        _ = InvokeWithObjectArgument(FindMethod(meshClass, "set_colors", 1), mesh, colorArray);

        IntPtr indexArray = arrayNew(FindClass(FindImage("mscorlib.dll"), "System", "Int32"), (nuint)vertices.Count);
        if (indexArray == IntPtr.Zero) throw new InvalidOperationException("Could not allocate contact marker indices.");
        Marshal.Copy(Enumerable.Range(0, vertices.Count).ToArray(), 0, IntPtr.Add(indexArray, 4 * IntPtr.Size), vertices.Count);
        _ = InvokeWithObjectArgument(FindMethod(meshClass, "set_triangles", 1), mesh, indexArray);
        _ = Invoke(FindMethod(meshClass, "RecalculateBounds"), mesh);

    }

    private unsafe ContactMarkerAsset CreateContactMarkerAsset(IntPtr coreImage, IntPtr assetClass, string label, Action<IntPtr> initialize)
    {
        if (_contactMarkerKeepFlag == IntPtr.Zero)
        {
            _contactMarkerObjectClass = FindClass(coreImage, "UnityEngine", "Object");
            _contactMarkerAlive = FindMethodBySignature(_contactMarkerObjectClass, "op_Implicit", "UnityEngine.Object");
            _contactMarkerSetHideFlags = FindMethodBySignature(_contactMarkerObjectClass, "set_hideFlags", "UnityEngine.HideFlags");
            _contactMarkerFreeHandle = Marshal.GetDelegateForFunctionPointer<ContactFreeHandle>(
                NativeLibrary.GetExport(NativeMethods.GetModuleHandle("GameAssembly.dll"), "il2cpp_gchandle_free"));
            int keep = 32; // HideFlags.DontUnloadUnusedAsset
            _contactMarkerKeepFlag = RootContactValue(_api.ValueBox(FindClass(coreImage, "UnityEngine", "HideFlags"), (IntPtr)(&keep)));
        }

        IntPtr asset = _api.ObjectNew(assetClass);
        IntPtr root = RootObject(asset, label);
        try
        {
            initialize(asset);
            // A GC handle roots the wrapper, but Unity can still unload the native mesh or material.
            _ = InvokeWithObjectArgument(_contactMarkerSetHideFlags, asset, _contactMarkerKeepFlag);
            return new ContactMarkerAsset(asset, root);
        }
        catch
        {
            try
            {
                if (ContactMarkerAssetAlive(asset))
                    _ = InvokeWithObjectArgument(FindMethodBySignature(_contactMarkerObjectClass, "Destroy", "UnityEngine.Object"),
                        IntPtr.Zero, asset);
            }
            finally
            {
                _contactMarkerFreeHandle!(root);
            }
            throw;
        }
    }

    private bool ContactMarkerAssetAlive(IntPtr asset) => asset != IntPtr.Zero &&
        ReadContactBool(InvokeWithObjectArgument(_contactMarkerAlive, IntPtr.Zero, asset));

    private bool DropExpiredContactMarkerAsset(ref ContactMarkerAsset asset)
    {
        if (asset.Object == IntPtr.Zero || ContactMarkerAssetAlive(asset.Object)) return false;
        _contactMarkerFreeHandle!(asset.Root);
        asset = default;
        return true;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void ContactFreeHandle(IntPtr handle);

    private IntPtr FindContactMarkerShader(IntPtr coreImage)
    {
        IntPtr shaderClass = FindClass(coreImage, "UnityEngine", "Shader");
        IntPtr find = FindMethodBySignature(shaderClass, "Find", "System.String");
        IntPtr isSupported = FindMethod(shaderClass, "get_isSupported");
        string[] names = { "Universal Render Pipeline/Unlit", "Universal Render Pipeline/Particles/Unlit",
            "Hidden/Internal-Colored" };
        foreach (string name in names)
        {
            IntPtr shader = InvokeWithObjectArgument(find, IntPtr.Zero, NewManagedString(name));
            if (shader == IntPtr.Zero || !ReadContactBool(Invoke(isSupported, shader))) continue;
            _contactShaderName = name;
            return shader;
        }
        return IntPtr.Zero;
    }

    private void ConfigureContactMarkerMaterial(IntPtr coreImage, IntPtr materialClass, IntPtr material)
    {
        IntPtr hasProperty = FindMethodBySignature(materialClass, "HasProperty", "System.String");
        if (!ReadContactBool(InvokeWithObjectArgument(hasProperty, material, NewManagedString("_ZWrite"))))
            throw new InvalidOperationException("The hand marker shader must support read-only scene depth.");
        SetContactMarkerColor(coreImage, materialClass, material, Vector3.One);
        IntPtr setFloat = FindMethodBySignature(materialClass, "SetFloat", "System.String", "System.Single");
        IntPtr mscorlib = FindImage("mscorlib.dll");
        // The scene fallback must reach the transparent pass after the game draws its actors.
        _ = InvokeWithObjectArgument(FindMethodBySignature(materialClass, "set_renderQueue", "System.Int32"),
            material, BoxInt32(mscorlib, 3100));
        (string Name, float Value)[] renderState = { ("_Surface", 0f), ("_SrcBlend", 1f), ("_DstBlend", 0f),
            ("_ZWrite", 0f), ("_ZTest", 4f), ("_Cull", 0f), ("_AlphaClip", 0f) };
        foreach ((string name, float value) in renderState)
        {
            IntPtr property = NewManagedString(name);
            if (ReadContactBool(InvokeWithObjectArgument(hasProperty, material, property)))
                _ = InvokeWithObjectArguments(setFloat, material, property, BoxSingle(mscorlib, value));
        }
    }

    private void SetContactMarkerColor(IntPtr coreImage, IntPtr materialClass, IntPtr material, Vector3 color)
    {
        IntPtr hasProperty = FindMethodBySignature(materialClass, "HasProperty", "System.String");
        IntPtr setColor = FindMethodBySignature(materialClass, "SetColor", "System.String", "UnityEngine.Color");
        IntPtr value = BoxColor(coreImage, color.X, color.Y, color.Z, 1f);
        foreach (string name in new[] { "_BaseColor", "_Color", "_MultiplyColor" })
        {
            IntPtr property = NewManagedString(name);
            if (ReadContactBool(InvokeWithObjectArgument(hasProperty, material, property)))
                _ = InvokeWithObjectArguments(setColor, material, property, value);
        }
    }

    private bool ReadContactBool(IntPtr result)
    {
        IntPtr value = result == IntPtr.Zero ? IntPtr.Zero : _api.ObjectUnbox(result);
        return value != IntPtr.Zero && Marshal.ReadByte(value) != 0;
    }

    private static void AddContactSphereFace(List<Vector3> vertices, Vector3 a, Vector3 b, Vector3 c, int depth)
    {
        if (depth == 0)
        {
            vertices.Add(a);
            vertices.Add(c);
            vertices.Add(b);
            return;
        }
        Vector3 ab = Vector3.Normalize(a + b);
        Vector3 bc = Vector3.Normalize(b + c);
        Vector3 ca = Vector3.Normalize(c + a);
        AddContactSphereFace(vertices, a, ab, ca, depth - 1);
        AddContactSphereFace(vertices, ab, b, bc, depth - 1);
        AddContactSphereFace(vertices, ca, bc, c, depth - 1);
        AddContactSphereFace(vertices, ab, bc, ca, depth - 1);
    }

    private unsafe IntPtr BoxContactVector(Vector3 vector) => _api.ValueBox(_contactVectorClass, (IntPtr)(&vector));

    private IntPtr RootContactValue(IntPtr value)
    {
        _ = RootObject(value, "contact marker draw argument");
        return value;
    }


    private void LogContactPilot(DateTimeOffset now, string name, string reason) => RuntimeProbe.Append(_logPath, new ProbeEvent
    {
        TimestampUtc = now, Event = name, Scene = _navigationScene, ProcessId = Environment.ProcessId, Reason = reason
    });

    private static Vector3 AsVector(TrackingVector3 value) => new(value.X, value.Y, value.Z);
    private static Quaternion AsQuaternion(TrackingQuaternion value) => new(value.X, value.Y, value.Z, value.W);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr ContactArrayNew(IntPtr elementClass, nuint length);
}
