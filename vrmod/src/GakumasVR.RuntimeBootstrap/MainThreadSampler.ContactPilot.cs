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
#if !GAKUMAS_PUBLIC
    private int _contactRigProbeAttempts;
    private ContactRigScan? _contactRigScan;
    private DateTimeOffset _nextContactRigProbeUtc;
#endif
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

#if !GAKUMAS_PUBLIC
    private static bool IsContactTrialScene(string scene) =>
        string.Equals(scene, "env_3d_home_classroom-01-00-noon", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(scene, "env_3d_home_classroom-01-00-evening", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(scene, "env_3d_home_classroom-01-00-night", StringComparison.OrdinalIgnoreCase);
#endif

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
#if !GAKUMAS_PUBLIC
        VrTrackingSettings tracking = VrSettingsRuntime.Current.Tracking;
        holdSeconds = tracking.ContactHandHoldSeconds;
        radiusMeters = tracking.ContactHandRadiusMeters;
        showTrialMarkers = _contactTrialPairPrepared && (!_contactCollisionEnabled || tracking.ShowContactLocations || tracking.ContactShowRanges);
#endif
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
#if !GAKUMAS_PUBLIC
                expired |= DropExpiredContactMarkerAsset(ref _contactTrialMarkerBaseline) |
                    DropExpiredContactMarkerAsset(ref _contactTrialMarkerOffset) |
                    DropExpiredContactMarkerAsset(ref _contactRangeMeshAsset);
                if (expired) _contactTrialMarkerFailed = false;
#endif
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
#if !GAKUMAS_PUBLIC
            ContactTrialMarker[] markers = GetContactTrialMarkers(now, coreImage);
            if (left is null && right is null && markers.Length == 0) return;
            var frame = new ContactHandFrame(renderStereo, left, right, radius, alignment.Scale, nearClip, markers);
#else
            if (left is null && right is null) return;
            var frame = new ContactHandFrame(renderStereo, left, right, radius, alignment.Scale, nearClip);
#endif
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
        DrawMarker(frame.Left, size, _contactMaterial, _contactMesh);
        DrawMarker(frame.Right, size, _contactMaterial, _contactMesh);
#if !GAKUMAS_PUBLIC
        foreach (ContactTrialMarker marker in frame.TargetMarkers)
            DrawMarker(marker.Center, BoxContactVector(new Vector3(marker.Radius)), marker.Material, marker.Mesh);
#endif
        if (!_contactHandsLogged)
        {
            _contactHandsLogged = true;
            LogContactPilot(now, "contact-pilot-hands-submitted",
                $"left={frame.Left.HasValue};right={frame.Right.HasValue};displayRadius={frame.Radius:R};shader={_contactShaderName};cameraTargets=stereo-eyes;phase=scene-fallback");
        }

        void DrawMarker(Vector3? center, IntPtr markerSize, IntPtr material, IntPtr mesh)
        {
            if (center is not Vector3 position) return;
            IntPtr matrix = InvokeWithObjectArguments(_contactTrs, IntPtr.Zero, BoxContactVector(position), _contactOrientation, markerSize);
            _ = InvokeWithObjectArguments(_contactSceneDraw, IntPtr.Zero, mesh, matrix, material,
                _contactSceneLayer, _stereoLeftCamera, _contactSceneSubmesh, IntPtr.Zero, _contactSceneShadows,
                _contactSceneReceiveShadows, IntPtr.Zero, _contactSceneLightProbes, IntPtr.Zero);
            _ = InvokeWithObjectArguments(_contactSceneDraw, IntPtr.Zero, mesh, matrix, material,
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
#if !GAKUMAS_PUBLIC
            reach = DescribeContactTrialReach(frame);
#endif
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
                mesh => InitializeContactMarkerMesh(coreImage, meshClass, mesh, ContactMarkerVertices()));
        }
        return true;
    }

    private static List<Vector3> ContactMarkerVertices()
    {
        List<Vector3> vertices = new();
        Vector3[] ring = { Vector3.UnitX, -Vector3.UnitZ, -Vector3.UnitX, Vector3.UnitZ };
        for (int i = 0; i < ring.Length; i++)
        {
            Vector3 next = ring[(i + 1) % ring.Length];
            AddContactSphereFace(vertices, Vector3.UnitY, ring[i], next, 2);
            AddContactSphereFace(vertices, -Vector3.UnitY, next, ring[i], 2);
        }
        return vertices;
    }

    private void InitializeContactMarkerMesh(IntPtr coreImage, IntPtr meshClass, IntPtr mesh, IReadOnlyList<Vector3> vertices)
    {
        _ = Invoke(FindMethod(meshClass, ".ctor"), mesh);
        var arrayNew = Marshal.GetDelegateForFunctionPointer<ContactArrayNew>(
            NativeLibrary.GetExport(NativeMethods.GetModuleHandle("GameAssembly.dll"), "il2cpp_array_new"));
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

#if !GAKUMAS_PUBLIC
    private void ProbeContactPilot(DateTimeOffset now, IntPtr coreImage)
    {
        if (now < _nextContactRigProbeUtc) return;
        try
        {
            if (!ProbeContactPilotRig(now, coreImage)) return;
            _contactRigProbeAttempts = Math.Min(3, _contactRigProbeAttempts + 1);
            // Keep finding actors that load later while the same camera stays active.
            _nextContactRigProbeUtc = now.AddSeconds(_contactRigProbeAttempts < 3 || _contactBoneTrialTargets.Count == 0 ? 2 : 10);
        }
        catch (Exception error)
        {
            ReleaseContactRigScan();
            _contactRigProbeAttempts = 3;
            _nextContactRigProbeUtc = now.AddSeconds(10);
            LogContactPilot(now, "contact-pilot-rig-unavailable", error.GetType().Name + ": " + error.Message.Split('\n')[0]);
        }
    }

    private sealed class ContactRigScan(IReadOnlyList<IntPtr> skins, IntPtr root)
    {
        public IReadOnlyList<IntPtr> Skins { get; } = skins;
        public IntPtr Root { get; } = root;
        public int Cursor, InspectedBones, ActorSkins;
        public int InactiveSkins, LayerRejectedSkins, MissingAnimators, InvalidRigRoots;
        public int LowerBodyCursor, LowerBodyInspectedBones, LowerBodySkins;
        public HashSet<int> LowerBodyWeightedActors { get; } = new();
        public double ScanMilliseconds;
        public HashSet<IntPtr> ChestActors { get; } = new();
        public SortedSet<string> Candidates { get; } = new(StringComparer.Ordinal);
        public SortedSet<string> LowerBody { get; } = new(StringComparer.Ordinal);
        public List<string> Weighted { get; } = new();
    }

    private void ReleaseContactRigScan()
    {
        if (_contactRigScan is { } scan && scan.Root != IntPtr.Zero) _contactTrialFreeHandle!(scan.Root);
        _contactRigScan = null;
    }

    private void ProbeContactRenderContract(DateTimeOffset now, string scene)
    {
        try
        {
            IntPtr urp = FindImage("Unity.RenderPipelines.Universal.Runtime.dll");
            IntPtr pipeline = FindClass(urp, "UnityEngine.Rendering.Universal", "UniversalRenderPipeline");
            IntPtr renderer = FindClass(urp, "UnityEngine.Rendering.Universal", "ScriptableRenderer");
            LogContactPilot(now, "contact-pilot-render-contract",
                $"scene={scene};stack={DescribeNamedMethods(pipeline, "RenderCameraStack")};renderer={DescribeNamedMethods(renderer, "Execute")};cleanup={DescribeNamedMethods(renderer, "InternalFinishRendering")};writes=0");
        }
        catch (Exception error)
        {
            LogContactPilot(now, "contact-pilot-render-contract-unavailable", $"scene={scene};error={error.GetType().Name};writes=0");
        }
    }

    private bool ProbeContactPilotRig(DateTimeOffset now, IntPtr coreImage)
    {
        long started = Stopwatch.GetTimestamp();
        IntPtr rendererClass = FindClass(coreImage, "UnityEngine", "SkinnedMeshRenderer");
        IntPtr animatorClass = FindClass(FindImage("UnityEngine.AnimationModule.dll"), "UnityEngine", "Animator");
        IntPtr animatorType = GetManagedType(animatorClass);
        if (animatorType == IntPtr.Zero) throw new MissingMemberException("Animator runtime type is unavailable.");
        IntPtr componentClass = FindClass(coreImage, "UnityEngine", "Component");
        IntPtr inParent = FindMethodBySignature(componentClass, "GetComponentInParent", "System.Type");
        IntPtr transformClass = FindClass(coreImage, "UnityEngine", "Transform");
        IntPtr isChild = FindMethodBySignature(transformClass, "IsChildOf", "UnityEngine.Transform");
        IntPtr getTransform = FindMethod(componentClass, "get_transform");
        IntPtr getParent = FindMethod(transformClass, "get_parent");
        IntPtr getChildCount = FindMethod(transformClass, "get_childCount");
        IntPtr getChild = FindMethodBySignature(transformClass, "GetChild", "System.Int32");
        IntPtr mscorlib = FindImage("mscorlib.dll");
        IntPtr getName = FindMethod(coreImage, "UnityEngine", "Object", "get_name");
        IntPtr getBones = FindMethod(rendererClass, "get_bones");
        IntPtr getMesh = FindMethod(rendererClass, "get_sharedMesh");
        IntPtr getRoot = FindMethod(rendererClass, "get_rootBone");
        int eyeLayers = InvokeInstanceInt(coreImage, "UnityEngine", "Camera", "get_cullingMask", _stereoLeftCamera) &
            InvokeInstanceInt(coreImage, "UnityEngine", "Camera", "get_cullingMask", _stereoRightCamera);
        if (_contactRigScan is null)
        {
            EnsureContactTrialHook(coreImage);
            IntPtr array = FindActiveContactSkins(coreImage, rendererClass);
            IReadOnlyList<IntPtr> skins = ReadObjectArray(array, 512);
            IntPtr handle = array == IntPtr.Zero ? IntPtr.Zero : RootObject(array, "contact rig scan");
            _contactRigScan = new ContactRigScan(skins, handle);
        }
        ContactRigScan scan = _contactRigScan;
        int end = Math.Min(scan.Cursor + 8, scan.Skins.Count);
        while (scan.Cursor < end)
        {
            IntPtr skin = scan.Skins[scan.Cursor++];
            if (!ContactTrialAlive(skin)) continue;
            IntPtr gameObject = Invoke(_contactTrialSkinGameObject, skin);
            if (gameObject == IntPtr.Zero || !ReadContactBool(Invoke(_contactTrialObjectActive, gameObject)) ||
                !ReadContactBool(Invoke(_contactTrialSkinEnabled, skin)))
            {
                scan.InactiveSkins++;
                continue;
            }
            IntPtr mesh = Invoke(getMesh, skin);
            if (mesh == IntPtr.Zero) continue;
            int layer = InvokeInstanceInt(coreImage, "UnityEngine", "GameObject", "get_layer", gameObject);
            if (layer is < 0 or > 31 || (eyeLayers & (1 << layer)) == 0)
            {
                scan.LayerRejectedSkins++;
                continue;
            }

            IntPtr animator = InvokeWithObjectArgument(inParent, skin, animatorType);
            // The Animator identifies ownership even when a timeline drives the active skin.
            if (!ContactTrialAlive(animator))
            {
                scan.MissingAnimators++;
                continue;
            }
            if (ContactActorHasBothSides(animator))
            {
                scan.ChestActors.Add(animator);
                continue;
            }
            IntPtr actorTransform = Invoke(getTransform, animator);
            IntPtr root = Invoke(getRoot, skin);
            if (actorTransform == IntPtr.Zero || root == IntPtr.Zero || !ContactChildOf(isChild, root, actorTransform))
            {
                scan.InvalidRigRoots++;
                continue;
            }
            IntPtr bones = Invoke(getBones, skin);
            if (bones == IntPtr.Zero) continue;
            ulong slots = unchecked((ulong)Marshal.ReadInt64(bones, 3 * IntPtr.Size));
            if (slots is 0 or > 512) continue;
            scan.InspectedBones += (int)slots;
            var chestSlots = new Dictionary<int, string>();
            var skinLowerBody = new List<string>();
            for (int slot = 0; slot < (int)slots; slot++)
            {
                // Bone weights refer to the original skin.bones slot, including empty slots.
                IntPtr bone = Marshal.ReadIntPtr(bones, (4 + slot) * IntPtr.Size);
                if (bone == IntPtr.Zero) continue;
                string name = ReadContactBoneLabel(getName, bone);
                bool chestHint = name is "LeftBust1_S" or "RightBust1_S";
                bool lowerHint = IsContactLowerBodyHint(name);
                if (!chestHint && !lowerHint) continue;
                IntPtr parent = Invoke(getParent, bone);
                if (parent == IntPtr.Zero) continue;
                string parentName = ReadContactBoneLabel(getName, parent);
                if (!IsSafeContactBoneLabel(name) || !IsSafeContactBoneLabel(parentName)) continue;
                if (IsObservedContactChestBone(name, parentName) && ContactChildOf(isChild, bone, root))
                    chestSlots[slot] = $"{parentName}/{name}";
                if (lowerHint)
                    skinLowerBody.Add($"{parentName}/{name}");
                if (name == "Hips")
                {
                    int children = Math.Min(16, ReadContactInt(getChildCount, bone));
                    for (int i = 0; i < children; i++)
                    {
                        string child = ReadContactBoneLabel(getName,
                            InvokeWithObjectArgument(getChild, bone, BoxInt32(mscorlib, i)));
                        if (IsSafeContactBoneLabel(child)) skinLowerBody.Add($"Hips/{child}");
                    }
                }
            }
            if (chestSlots.Count != 0)
            {
                scan.ActorSkins++;
                scan.Candidates.UnionWith(chestSlots.Values);
                scan.LowerBody.UnionWith(skinLowerBody);
                scan.ChestActors.Add(animator);
                scan.Weighted.Add(ObserveContactSkinWeights(coreImage, mesh, (int)slots, chestSlots, out int[]? weights));
                if (weights is not null)
                {
                    string[] boundPaths = _contactBoneTrialTargets.Where(target => !target.Invalid && target.Animator == animator)
                        .Select(target => target.BonePath).ToArray();
                    foreach (var candidate in SelectContactRigSlots(chestSlots, weights, boundPaths))
                    {
                        int slot = candidate.Slot;
                        IntPtr bone = Marshal.ReadIntPtr(bones, (4 + slot) * IntPtr.Size);
                        BindContactBoneTrial(now, coreImage, animator, skin, mesh, root, bone, Invoke(getParent, bone),
                            slot, (int)slots, weights[slot], candidate.Path);
                    }
                }
            }
            break;
        }
        scan.ScanMilliseconds += (Stopwatch.GetTimestamp() - started) * 1000d / Stopwatch.Frequency;
        if (scan.Cursor < scan.Skins.Count)
            return false;
        try
        {
            if (!AdvanceContactLowerBodyAudit(now, coreImage, scan, componentClass, inParent, animatorType,
                getTransform, getRoot, getBones, getMesh, getName, getParent, isChild, eyeLayers)) return false;
        }
        catch (Exception error)
        {
            LogContactPilot(now, "contact-lower-body-audit-unavailable", $"error={error.GetType().Name};auditWrites=0");
        }
        LogContactPilot(now, "contact-pilot-rig-observed",
            $"skins={scan.Skins.Count};scanned={scan.Cursor};inactive={scan.InactiveSkins};layerRejected={scan.LayerRejectedSkins};eyeLayers=0x{eyeLayers:X8};missingAnimator={scan.MissingAnimators};invalidRigRoot={scan.InvalidRigRoots};actorsWithChest={scan.ChestActors.Count};trialTargets={_contactBoneTrialTargets.Count};selectedActorSkins={scan.ActorSkins};inspectedBones={scan.InspectedBones};scanMs={scan.ScanMilliseconds:F2};chestCandidates={string.Join(",", scan.Candidates)};lowerBodyTopology={string.Join(",", scan.LowerBody.Take(24))};skinWeights={string.Join("|", scan.Weighted)};writes=0");
        ReleaseContactRigScan();
        return true;
    }

    private unsafe IntPtr FindActiveContactSkins(IntPtr coreImage, IntPtr rendererClass)
    {
        IntPtr find = FindMethodBySignature(FindClass(coreImage, "UnityEngine", "Object"), "FindObjectsByType",
            "System.Type", "UnityEngine.FindObjectsSortMode");
        int unsorted = 0; // FindObjectsSortMode.None
        IntPtr mode = _api.ValueBox(FindClass(coreImage, "UnityEngine", "FindObjectsSortMode"), (IntPtr)(&unsorted));
        return InvokeWithObjectArguments(find, IntPtr.Zero, GetManagedType(rendererClass), mode);
    }

    private string ReadContactBoneLabel(IntPtr getName, IntPtr bone)
    {
        if (bone == IntPtr.Zero) return string.Empty;
        IntPtr value = Invoke(getName, bone);
        if (value == IntPtr.Zero) return string.Empty;
        int length = _api.StringLength(value);
        if (length is < 1 or > 64) return string.Empty;
        IntPtr characters = _api.StringChars(value);
        return characters == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUni(characters, length) ?? string.Empty;
    }

    private bool ContactChildOf(IntPtr method, IntPtr transform, IntPtr parent)
    {
        if (transform == parent) return true;
        return ReadContactBool(InvokeWithObjectArgument(method, transform, parent));
    }

    internal static bool IsObservedContactChestBone(string name, string parent) =>
        parent == "Spine2" && name is "LeftBust1_S" or "RightBust1_S";

    private static bool IsContactLowerBodyHint(string name) =>
        name.Contains("butt", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("glut", StringComparison.OrdinalIgnoreCase) || name.Contains("hip", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("thigh", StringComparison.OrdinalIgnoreCase) || name.Contains("upperleg", StringComparison.OrdinalIgnoreCase);

    private static bool IsSafeContactBoneLabel(string name) =>
        name.Length is > 0 and <= 64 && name.All(c => char.IsLetterOrDigit(c) || c is '_' or '.' or '-');
#endif

    private void LogContactPilot(DateTimeOffset now, string name, string reason) => RuntimeProbe.Append(_logPath, new ProbeEvent
    {
        TimestampUtc = now, Event = name, Scene = _navigationScene, ProcessId = Environment.ProcessId, Reason = reason
    });

    private static Vector3 AsVector(TrackingVector3 value) => new(value.X, value.Y, value.Z);
    private static Quaternion AsQuaternion(TrackingQuaternion value) => new(value.X, value.Y, value.Z, value.W);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr ContactArrayNew(IntPtr elementClass, nuint length);
}
