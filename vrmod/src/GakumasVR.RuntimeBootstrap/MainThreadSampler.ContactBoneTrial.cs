using System.Numerics;
using GakumasVR.Core;
using System.Runtime.InteropServices;

namespace Doorstop;

internal sealed partial class MainThreadSampler
{
    private const int ContactTrialPhaseMilliseconds = 2_000;
    private const int MaxContactTrialActors = 12;
    private const int MaxContactTrialTargets = MaxContactTrialActors * 6;
    private const int MaxContactCompensatedChildren = 8;
    private string _contactTrialScene = string.Empty;
    private int? _contactTrialSceneHandle, _contactActiveSceneHandle;
    private IntPtr _contactSceneHandleGetter;

    private sealed record ContactTrialTarget(IntPtr Animator, IntPtr Actor, IntPtr Skin, IntPtr Mesh,
        IntPtr Root, IntPtr Bone, IntPtr Parent, int Slot, int Slots, IntPtr[] Handles)
    {
        public int Id { get; init; }
        public int ActorId { get; init; }
        public string BonePath { get; init; } = ContactLeftBonePath;
        public ContactRegion Region { get; init; } = ContactRegion.Chest;
        public bool RequiresMeasuredSurface => Region != ContactRegion.Chest;
        public bool IsCalf => Region == ContactRegion.Calf;
        public IntPtr[] CompensatedChildren { get; init; } = Array.Empty<IntPtr>();
        public int TouchMask { get; set; }
        public int RawTouchMask { get; set; }
        public int PreviousTouchMask { get; set; }
        public Vector3? PairCenter { get; set; }
        public Vector3? PairBone { get; set; }
        public Vector3? PairSurface { get; set; }
        public ContactSurfaceAnchor? Surface { get; set; }
        public string SurfaceSource { get; set; } = "bone-fallback";
        public int ReleaseMask { get; set; }
        public int ReleaseCount { get; set; }
        public float PairRadius { get; set; }
        public Vector3? SkinCenter { get; set; }
        public Vector3? SkinExtents { get; set; }
        public float MaxEyeCenterGap { get; set; }
        public Vector3? PairOffset { get; set; }
        public ContactResponse Response { get; } = new();
        public ContactResponse TiltResponse { get; } = new();
        public Vector3 PressLog { get; set; }
        public Vector3 PairTilt { get; set; }
        public Vector3 PairScale { get; set; } = Vector3.One;
        public bool HasContactWrite => PairOffset.GetValueOrDefault() != Vector3.Zero || PairTilt != Vector3.Zero || PairScale != Vector3.One;

        public void ResetContactMotion()
        {
            Response.Reset();
            TiltResponse.Reset();
            PressLog = PairTilt = Vector3.Zero;
            PairScale = Vector3.One;
        }
        public Vector3? PreviousCenter { get; set; }
        public int EyeWrites { get; set; }
        public bool Invalid { get; set; }
        public long NextSlotCheck { get; set; }
    }
    private readonly record struct ContactTrialLease(IntPtr Bone, IntPtr Parent, ContactTrialPosition Position);
    internal readonly record struct ContactTrialPosition(Vector3 Baseline, Vector3 Written)
    {
        internal bool Owns(Vector3 current) => current == Written;
    }

    private bool _contactBoneTrialEnabled, _contactTrialFaulted, _contactTrialInsideStack, _contactTrialPulse;
    private bool _contactTrialPreviousPulse, _contactTrialPairPrepared;
    private bool? _contactTrialLoggedPhase;
    private int _contactTrialGeneration = -1, _contactTrialSeenRoles;
    private int _contactNextActorId, _contactNextTargetId;
    private readonly HashSet<IntPtr> _contactTrialScopedSkins = new(MaxContactTrialTargets);
    private IntPtr _contactTrialGetTransform;
    private int _contactTrialWrites, _contactTrialRestores, _contactTrialExternalChanges, _contactTrialSourceCalls;
    private int _contactTrialMatrixScopes;
    private uint _contactTrialOwnerThread;
    private long _contactTrialReadyTick;
    private string? _contactTrialPendingFailure;
    private readonly List<ContactTrialTarget> _contactBoneTrialTargets = new(MaxContactTrialTargets);
    private ContactTrialTarget[] _contactTrialPairTargets = Array.Empty<ContactTrialTarget>();
    private readonly List<ContactTrialLease> _contactTrialLeases = new(MaxContactTrialTargets);
    private readonly List<IntPtr> _contactTrialMatrixLeases = new(MaxContactTrialTargets);
    private ContactCameraStack? _contactTrialReplacement, _contactTrialOriginal;
    private ContactFreeHandle? _contactTrialFreeHandle;
    private IntPtr _contactTrialAlive, _contactTrialParent, _contactTrialChildOf, _contactTrialMesh, _contactTrialRoot;
    private IntPtr _contactTrialChildCount, _contactTrialGetChild;
    private IntPtr _contactTrialBones, _contactTrialLocalPosition, _contactTrialWorldPosition, _contactTrialSetPosition, _contactTrialVectorClass;
    private IntPtr _contactTrialTransformVector, _contactTrialSkinEnabled, _contactTrialCameraTarget;
    private IntPtr _contactTrialSkinGameObject, _contactTrialObjectActive;
    private IntPtr _contactTrialCameraStack;
    private IntPtr _contactTrialGetForceMatrices, _contactTrialSetForceMatrices, _contactTrialTrue, _contactTrialFalse;
    private IntPtr _contactTrialBounds, _contactTrialBoundsCenter, _contactTrialBoundsExtents;
    private ContactMarkerAsset _contactTrialMarkerBaseline, _contactTrialMarkerOffset, _contactRangeMeshAsset;
    private bool _contactTrialMarkerFailed, _contactTrialMarkerLogged, _contactRangeMarkerFailed;

    private void SetContactBoneTrialEnabled(bool enabled)
    {
        if (_contactBoneTrialEnabled == enabled) return;
        _contactBoneTrialEnabled = enabled;
        _contactTrialFaulted = false;
        RetireContactTrial();
    }

    private void RetireContactTrial()
    {
        if (_contactTrialInsideStack || !RestoreContactTrialState()) return;
        ReleaseContactTrialTargets();
        ReleaseContactRigScan();
        ResetContactTrialGeneration(-1);
    }

    internal static bool IsContactRigRequested(string scene, bool collision, bool trial) =>
        collision || (trial && IsContactTrialScene(scene));

    private void ReadContactSceneHandle(IntPtr coreImage, IntPtr sceneValue)
    {
        _contactActiveSceneHandle = null;
        try
        {
            if (_contactSceneHandleGetter == IntPtr.Zero)
                _contactSceneHandleGetter = FindMethod(coreImage, "UnityEngine.SceneManagement", "Scene", "get_handle");
            IntPtr boxed = Invoke(_contactSceneHandleGetter, sceneValue);
            IntPtr value = boxed == IntPtr.Zero ? IntPtr.Zero : _api.ObjectUnbox(boxed);
            if (value != IntPtr.Zero) _contactActiveSceneHandle = Marshal.ReadInt32(value);
        }
        catch (Exception) { } // The scene name and camera generation also delimit bindings.
    }

    private void MaintainContactBoneTrial(DateTimeOffset now, IntPtr coreImage, string scene)
    {
        if (_contactTrialInsideStack) return;
        if (_contactTrialPendingFailure is { } failure)
        {
            _contactTrialPendingFailure = null;
            LogContactPilot(now, "contact-bone-trial-stopped", failure);
        }
        if (!RestoreContactTrialState()) return;
        if (!_stereoPumpEligible || !_stereoOutputValidated || _stereoContinuousFailed ||
            !IsContactRigRequested(scene, _contactCollisionEnabled, _contactBoneTrialEnabled))
        {
            if (_contactTrialGeneration != -1) RetireContactTrial();
            return;
        }
        if (_contactTrialGeneration != _stereoGenerationRetireCount || _contactTrialScene != scene ||
            _contactTrialSceneHandle != _contactActiveSceneHandle)
        {
            ReleaseContactTrialTargets();
            ReleaseContactRigScan();
            ResetContactTrialGeneration(_stereoGenerationRetireCount);
            _contactTrialFaulted = false;
            _contactTrialScene = scene;
            _contactTrialSceneHandle = _contactActiveSceneHandle;
            _contactRigProbeAttempts = 0;
            _nextContactRigProbeUtc = now.AddSeconds(1);
            ProbeContactRenderContract(now, scene);
        }
        else if (!_stereoNaturalRenderArmed)
            PruneContactTrialTargets(now);
        if (_contactTrialFaulted) ReleaseContactRigScan();
        else ProbeContactPilot(now, coreImage);
    }

    private void PruneContactTrialTargets(DateTimeOffset now)
    {
        if (ContactActiveLeases != 0) return;
        for (int i = _contactBoneTrialTargets.Count - 1; i >= 0; i--)
        {
            ContactTrialTarget target = _contactBoneTrialTargets[i];
            if (!target.Invalid) continue;
            _contactLowerBodyAuditedActors.Remove(target.ActorId);
            FreeContactTrialHandles(target.Handles);
            _contactBoneTrialTargets.RemoveAt(i);
            if (_contactRigProbeAttempts >= 3)
            {
                _contactRigProbeAttempts = 0;
                _nextContactRigProbeUtc = now.AddSeconds(1);
            }
        }
    }

    private void ResetContactTrialGeneration(int generation)
    {
        ResetContactCollision();
        _contactTrialGeneration = generation;
        _contactTrialScene = string.Empty;
        _contactTrialSceneHandle = null;
        _contactNextActorId = _contactNextTargetId = 0;
        _contactLowerBodyAuditedActors.Clear();
        _contactTrialSeenRoles = 0;
        _contactTrialReadyTick = 0;
        _contactTrialPreviousPulse = false;
        _contactTrialLoggedPhase = null;
        _contactTrialMarkerLogged = false;
        _contactRangeOverlayRegions = 0;
        _contactRangeMarkerFailed = false;
        _contactTrialPairPrepared = false;
        _contactTrialPairTargets = Array.Empty<ContactTrialTarget>();
        _contactHandFrame = null;
        _contactTrialWrites = _contactTrialRestores = _contactTrialExternalChanges = _contactTrialSourceCalls = 0;
        _contactTrialMatrixScopes = 0;
    }

    private bool BindContactBoneTrial(DateTimeOffset now, IntPtr coreImage, IntPtr animator, IntPtr skin,
        IntPtr mesh, IntPtr root, IntPtr bone, IntPtr parent, int slot, int slots, int influences, string path,
        ContactSurfaceAnchor? verifiedSurface = null)
    {
        ContactRegion region = ContactRegionForPath(path);
        if (_contactTrialFaulted || _contactTrialGeneration != _stereoGenerationRetireCount ||
            parent == IntPtr.Zero || influences <= 0 || _contactBoneTrialTargets.Count >= MaxContactTrialTargets ||
            !CanBindContactRig(_contactBoneTrialTargets.Where(target => !target.Invalid)
                .Select(target => new ContactRigKey(target.Animator, target.Bone, target.BonePath)).ToArray(),
                animator, bone, path, _contactCollisionEnabled && verifiedSurface.HasValue))
            return false;
        IntPtr[] handles = Array.Empty<IntPtr>();
        bool retained = false;
        try
        {
            EnsureContactTrialHook(coreImage);
            int childCount = region == ContactRegion.Calf ? ReadContactInt(_contactTrialChildCount, bone) : 0;
            if (childCount is < 0 or > MaxContactCompensatedChildren)
            {
                LogContactPilot(now, "contact-patch-binding-unavailable", $"bone={path};reason=child-count;children={childCount}");
                return false;
            }
            IntPtr actor = Invoke(FindMethod(coreImage, "UnityEngine", "Component", "get_transform"), animator);
            IntPtr[] objects = { animator, actor, skin, mesh, root, bone, parent };
            handles = new IntPtr[objects.Length + childCount];
            for (int i = 0; i < objects.Length; i++)
                handles[i] = RootObject(objects[i], "contact trial binding");
            var children = new IntPtr[childCount];
            if (childCount != 0)
            {
                IntPtr mscorlib = FindImage("mscorlib.dll");
                for (int i = 0; i < children.Length; i++)
                {
                    children[i] = InvokeWithObjectArgument(_contactTrialGetChild, bone, BoxInt32(mscorlib, i));
                    handles[objects.Length + i] = RootObject(children[i], "contact compensated child");
                }
            }
            int actorId = _contactBoneTrialTargets.FirstOrDefault(target => !target.Invalid && target.Animator == animator)?.ActorId
                ?? ++_contactNextActorId;
            LogContactPilot(now, "contact-bone-trial-bound",
                $"actor={actorId};target={_contactNextTargetId + 1};region={region.ToString().ToLowerInvariant()};bone={path};slot={slot};influences={influences};compensatedChildren={children.Length}");
            var target = new ContactTrialTarget(animator, actor, skin, mesh, root, bone, parent,
                slot, slots, handles)
            {
                Id = ++_contactNextTargetId, ActorId = actorId, BonePath = path, Region = region,
                CompensatedChildren = children
            };
            _contactBoneTrialTargets.Add(target);
            retained = true;
            if (target.RequiresMeasuredSurface)
            {
                target.Surface = verifiedSurface;
                target.SurfaceSource = $"baked-{region.ToString().ToLowerInvariant()}-patch";
            }
            else if (_contactCollisionEnabled) CaptureContactSurface(now, coreImage, target);
            return true;
        }
        catch (Exception error)
        {
            if (!retained) FreeContactTrialHandles(handles);
            if (region != ContactRegion.Chest)
                LogContactPilot(now, "contact-patch-binding-unavailable", $"bone={path};error={error.GetType().Name}");
            else FailContactBoneTrial("binding", error);
            return false;
        }
    }

    private void EnsureContactTrialHook(IntPtr coreImage)
    {
        if (_contactTrialOriginal is not null) return;
        IntPtr urp = FindImage("Unity.RenderPipelines.Universal.Runtime.dll");
        IntPtr pipeline = FindClass(urp, "UnityEngine.Rendering.Universal", "UniversalRenderPipeline");
        IntPtr method = FindMethodBySignature(pipeline, "RenderCameraStack", ScriptableRenderContextType, "UnityEngine.Camera");
        if ((_api.MethodGetFlags(method, out _) & 0x10) == 0 ||
            GetTypeName(_api.MethodGetReturnType(method)) != "System.Void")
            throw new MissingMethodException("Unexpected RenderCameraStack contract.");
        IntPtr assembly = NativeMethods.GetModuleHandle("GameAssembly.dll");
        var sizeOf = Marshal.GetDelegateForFunctionPointer<ContactClassValueSize>(
            NativeLibrary.GetExport(assembly, "il2cpp_class_value_size"));
        int contextSize = sizeOf(FindClass(coreImage, "UnityEngine.Rendering", "ScriptableRenderContext"), out _);
        if (IntPtr.Size != 8 || contextSize is not (8 or 24))
            throw new InvalidOperationException("Unsupported RenderCameraStack ABI.");
        _contactTrialFreeHandle = Marshal.GetDelegateForFunctionPointer<ContactFreeHandle>(
            NativeLibrary.GetExport(assembly, "il2cpp_gchandle_free"));

        _contactTrialAlive = FindMethodBySignature(FindClass(coreImage, "UnityEngine", "Object"),
            "op_Implicit", "UnityEngine.Object");
        _contactTrialGetTransform = FindMethod(coreImage, "UnityEngine", "Component", "get_transform");
        IntPtr transform = FindClass(coreImage, "UnityEngine", "Transform");
        _contactTrialParent = FindMethod(transform, "get_parent");
        _contactTrialChildOf = FindMethodBySignature(transform, "IsChildOf", "UnityEngine.Transform");
        _contactTrialChildCount = FindMethod(transform, "get_childCount");
        _contactTrialGetChild = FindMethodBySignature(transform, "GetChild", "System.Int32");
        _contactTrialLocalPosition = FindMethod(transform, "get_localPosition");
        _contactTrialWorldPosition = FindMethod(transform, "get_position");
        _contactTrialSetPosition = FindMethodBySignature(transform, "set_localPosition", "UnityEngine.Vector3");
        _contactTrialTransformVector = FindMethodBySignature(transform, "TransformVector", "UnityEngine.Vector3");
        _contactTrialVectorClass = FindClass(coreImage, "UnityEngine", "Vector3");
        ResolveContactDeformation(coreImage, transform);
        IntPtr skin = FindClass(coreImage, "UnityEngine", "SkinnedMeshRenderer");
        _contactTrialMesh = FindMethod(skin, "get_sharedMesh");
        _contactTrialRoot = FindMethod(skin, "get_rootBone");
        _contactTrialBones = FindMethod(skin, "get_bones");
        _contactTrialGetForceMatrices = FindMethod(skin, "get_forceMatrixRecalculationPerRender");
        _contactTrialSetForceMatrices = FindMethodBySignature(skin, "set_forceMatrixRecalculationPerRender", "System.Boolean");
        IntPtr mscorlib = FindImage("mscorlib.dll");
        _contactTrialTrue = RootContactValue(BoxBoolean(mscorlib, true));
        _contactTrialFalse = RootContactValue(BoxBoolean(mscorlib, false));
        _contactTrialSkinEnabled = FindMethod(coreImage, "UnityEngine", "Renderer", "get_enabled");
        _contactTrialBounds = FindMethod(coreImage, "UnityEngine", "Renderer", "get_bounds");
        IntPtr boundsClass = FindClass(coreImage, "UnityEngine", "Bounds");
        _contactTrialBoundsCenter = FindMethod(boundsClass, "get_center");
        _contactTrialBoundsExtents = FindMethod(boundsClass, "get_extents");
        _contactTrialSkinGameObject = FindMethod(coreImage, "UnityEngine", "Component", "get_gameObject");
        _contactTrialObjectActive = FindMethod(coreImage, "UnityEngine", "GameObject", "get_activeInHierarchy");
        _contactTrialCameraTarget = FindMethod(coreImage, "UnityEngine", "Camera", "get_targetTexture");
        _contactTrialCameraStack = FindMethod(urp, "UnityEngine.Rendering.Universal", "UniversalAdditionalCameraData", "get_cameraStack");

        _contactTrialOwnerThread = NativeMethods.GetCurrentThreadId();
        _contactTrialReplacement = OnContactTrialCameraStack;
        IntPtr target = Marshal.ReadIntPtr(method);
        if (target == IntPtr.Zero) throw new MissingMethodException("RenderCameraStack has no native pointer.");
        var hook = Marshal.GetDelegateForFunctionPointer<DobbyHookDelegate>(NativeLibrary.GetExport(_dobbyLibrary, "DobbyHook"));
        int status = hook(target, Marshal.GetFunctionPointerForDelegate(_contactTrialReplacement), out IntPtr original);
        if (status != 0 || original == IntPtr.Zero)
            throw new InvalidOperationException($"RenderCameraStack hook failed ({status}).");
        _contactTrialOriginal = Marshal.GetDelegateForFunctionPointer<ContactCameraStack>(original);
    }

    private void PrepareContactBoneTrialPair()
    {
        _contactTrialPairTargets = Array.Empty<ContactTrialTarget>();
        _contactTrialPulse = false;
        if ((_contactBoneTrialEnabled || _contactCollisionEnabled) && _contactBoneTrialTargets.Count != 0)
            PruneContactTrialTargets(DateTimeOffset.UtcNow);
        _contactTrialPairPrepared = IsContactRigRequested(_navigationScene, _contactCollisionEnabled, _contactBoneTrialEnabled) &&
            !_contactTrialFaulted && !_contactTrialInsideStack &&
            _contactBoneTrialTargets.Count != 0 && _contactTrialGeneration == _stereoGenerationRetireCount &&
            _stereoOutputValidated && _contactTrialScene == _navigationScene && _contactTrialSceneHandle == _contactActiveSceneHandle;
        if (!_contactTrialPairPrepared) return;
        _contactTrialPairTargets = _contactBoneTrialTargets.ToArray();

        _contactTrialPulse = !_contactCollisionEnabled && _contactTrialReadyTick != 0 &&
            ((Environment.TickCount64 - _contactTrialReadyTick) / ContactTrialPhaseMilliseconds & 1) != 0;
        foreach (ContactTrialTarget target in _contactTrialPairTargets)
        {
            target.PairOffset = null;
            target.PairTilt = Vector3.Zero;
            target.PairScale = Vector3.One;
            target.PairCenter = target.PairBone = target.PairSurface = null;
            target.ReleaseMask = 0;
            target.TouchMask = target.RawTouchMask = 0;
            target.MaxEyeCenterGap = 0;
            target.EyeWrites = 0;
            if (target.Invalid || (!_contactCollisionEnabled && (!_contactTrialPulse || _contactTrialPreviousPulse))) continue;
            long slotNow = Environment.TickCount64;
            if (_contactCollisionEnabled && slotNow < target.NextSlotCheck) continue;
            target.NextSlotCheck = slotNow + 1000;
            try
            {
                if (!ContactTrialSlotStillMatches(target))
                    target.Invalid = true;
            }
            catch (Exception error) { FailContactBoneTrial("bone-slot", error); }
        }
        _contactTrialPreviousPulse = _contactTrialPulse;
    }

    private void OnContactTrialCameraStack(IntPtr context, IntPtr camera, IntPtr method)
    {
        ContactCameraStack original = _contactTrialOriginal!;
        if (NativeMethods.GetCurrentThreadId() != _contactTrialOwnerThread)
        {
            original(context, camera, method);
            return;
        }
        if (_contactTrialInsideStack)
        {
            RestoreContactTrialPositions();
            FailContactBoneTrial("nested-camera-stack");
            original(context, camera, method);
            return;
        }
        if (!RestoreContactTrialState() || !_contactTrialPairPrepared || !_stereoNaturalRenderArmed || _contactTrialFaulted ||
            _contactTrialPairTargets.Length == 0)
        {
            original(context, camera, method);
            return;
        }

        int role = camera == _stereoLeftCamera ? 1 : camera == _stereoRightCamera ? 2 : camera == _lastLiveCamera ? 4 : 0;
        _contactTrialInsideStack = true;
        try
        {
            try
            {
                if (role is 1 or 2)
                {
                    IntPtr eyeTarget = role == 1 ? _stereoNaturalLeftRenderTexture : _stereoNaturalRightRenderTexture;
                    if (eyeTarget == IntPtr.Zero || _stereoCloneRenderType != 0 || Invoke(_contactTrialCameraTarget, camera) != eyeTarget)
                        throw new InvalidOperationException("Camera stack does not match the armed eye target.");
                    if ((_contactTrialSeenRoles & role) == 0)
                    {
                        IntPtr additionalData = role == 1 ? _stereoLeftAdditionalCameraData : _stereoRightAdditionalCameraData;
                        if (additionalData == IntPtr.Zero)
                            throw new InvalidOperationException("The armed eye lacks additional camera data.");
                        IntPtr overlays = Invoke(_contactTrialCameraStack, additionalData);
                        if (overlays != IntPtr.Zero && ReadContactInt(FindMethod(Marshal.ReadIntPtr(overlays), "get_Count"), overlays) != 0)
                            throw new InvalidOperationException("The armed eye stack contains overlay cameras.");
                    }
                }
                else if (role == 4) _contactTrialSourceCalls++;

                _contactTrialScopedSkins.Clear();
                foreach (ContactTrialTarget target in _contactTrialPairTargets)
                {
                    if (target.Invalid) continue;
                    if (_contactCollisionEnabled && role is 1 or 2 && target.PairBone is Vector3 pairCenter && ContactTrialAlive(target.Bone))
                    {
                        Vector3 eyeCenter = ReadContactTrialVector(Invoke(_contactTrialWorldPosition, target.Bone));
                        target.MaxEyeCenterGap = MathF.Max(target.MaxEyeCenterGap, Vector3.Distance(pairCenter, eyeCenter));
                    }
                    if ((_contactCollisionEnabled ? target.HasContactWrite : _contactTrialPulse) && role is 1 or 2 && (target.EyeWrites & role) == 0)
                        ApplyContactTrialPosition(target, role);
                    // Rebuild each bound skin for every camera, including the PC view after restoring the bones.
                    if (_contactTrialReadyTick != 0 && !target.Invalid && ContactTrialAlive(target.Skin) && _contactTrialScopedSkins.Add(target.Skin))
                        ForceContactTrialMatrices(target.Skin);
                }
            }
            catch (Exception error) { FailContactBoneTrial("eye-apply", error); RestoreContactTrialPositions(); }
            original(context, camera, method);
            _contactTrialSeenRoles |= role;
        }
        finally
        {
            RestoreContactTrialState();
            _contactTrialInsideStack = false;
        }
    }

    private void ApplyContactTrialPosition(ContactTrialTarget target, int role)
    {
        if (!ContactTrialBindingMatches(target))
        {
            target.Invalid = true;
            return;
        }
        Vector3 baseline = ReadContactTrialVector(Invoke(_contactTrialLocalPosition, target.Bone));
        if (!ContactTrialFinite(baseline)) throw new InvalidOperationException("Nonfinite animation position.");
        if (target.PairOffset is null)
        {
            if (_contactCollisionEnabled) return;
            float length = baseline.Length();
            if (!float.IsFinite(length) || length <= 0.0001f) return;
            Vector3 offset = baseline * (MathF.Min(0.018f, length * 0.25f) / length);
            Vector3 worldOffset = ReadContactTrialVector(InvokeWithObjectArgument(_contactTrialTransformVector,
                target.Parent, BoxContactTrialVector(offset)));
            float worldLength = worldOffset.Length();
            if (!float.IsFinite(worldLength) || worldLength <= 0.0001f) return;
            target.PairOffset = offset * MathF.Min(1f, 0.025f / worldLength);
        }

        if (target.IsCalf && ReadContactInt(_contactTrialChildCount, target.Bone) != target.CompensatedChildren.Length)
        {
            target.Invalid = true;
            return;
        }
        Vector3 written = baseline + target.PairOffset.Value;
        bool wrotePosition = written != baseline;
        if (wrotePosition)
        {
            Span<ContactTrialPosition> children = stackalloc ContactTrialPosition[target.CompensatedChildren.Length];
            if (children.Length != 0 && !PrepareCompensatedContactChildren(target, written - baseline, children))
            {
                target.Invalid = true;
                return;
            }
            StageContactTrialPosition(target.Bone, target.Parent, new ContactTrialPosition(baseline, written));
            for (int i = 0; i < children.Length; i++)
                StageContactTrialPosition(target.CompensatedChildren[i], target.Bone, children[i]);
        }
        bool wroteShape = _contactCollisionEnabled && target.CompensatedChildren.Length == 0 && ApplyContactChannels(target);
        if (wrotePosition || wroteShape) target.EyeWrites |= role;
    }

    private bool PrepareCompensatedContactChildren(ContactTrialTarget target, Vector3 localOffset,
        Span<ContactTrialPosition> children)
    {
        Vector3 worldOffset = ReadContactTrialVector(InvokeWithObjectArgument(_contactTrialTransformVector,
            target.Parent, BoxContactTrialVector(localOffset)));
        Matrix4x4 boneToWorld = ReadContactMatrix(Invoke(_contactBoneToWorld, target.Bone));
        if (!TryCompensateContactChild(boneToWorld, worldOffset, out Vector3 correction)) return false;
        for (int i = 0; i < children.Length; i++)
        {
            IntPtr child = target.CompensatedChildren[i];
            if (!ContactTrialAlive(child) || Invoke(_contactTrialParent, child) != target.Bone) return false;
            Vector3 baseline = ReadContactTrialVector(Invoke(_contactTrialLocalPosition, child));
            Vector3 written = baseline + correction;
            if (!ContactTrialFinite(baseline) || !ContactTrialFinite(written)) return false;
            children[i] = new ContactTrialPosition(baseline, written);
        }
        return true;
    }

    internal static bool TryCompensateContactChild(Matrix4x4 boneToWorld, Vector3 worldOffset, out Vector3 correction)
    {
        correction = default;
        if (!ContactTrialFinite(worldOffset) || !Matrix4x4.Invert(boneToWorld, out Matrix4x4 worldToBone)) return false;
        correction = Vector3.TransformNormal(-worldOffset, worldToBone);
        return ContactTrialFinite(correction) && correction.LengthSquared() <= .25f * .25f;
    }

    private void StageContactTrialPosition(IntPtr bone, IntPtr parent, ContactTrialPosition position)
    {
        if (position.Baseline == position.Written) return;
        // Stage the intended value first so a setter exception can still be recovered.
        _contactTrialLeases.Add(new ContactTrialLease(bone, parent, position));
        _ = InvokeWithObjectArgument(_contactTrialSetPosition, bone, BoxContactTrialVector(position.Written));
        Vector3 actual = ReadContactTrialVector(Invoke(_contactTrialLocalPosition, bone));
        if (actual != position.Written) throw new InvalidOperationException("The contact position did not round trip exactly.");
        _contactTrialWrites++;
    }

    private bool RestoreContactTrialState()
    {
        RestoreContactTrialPositions();
        RestoreContactTrialEntries(this, _contactTrialMatrixLeases, static (owner, skin) => owner.RestoreContactTrialMatrixMode(skin));
        return ContactActiveLeases == 0;
    }

    private void RestoreContactTrialPositions()
    {
        RestoreContactTrialEntries(this, _contactChannels, static (owner, lease) => owner.RestoreContactChannel(lease));
        RestoreContactTrialEntries(this, _contactTrialLeases, static (owner, lease) => owner.RestoreContactTrialPosition(lease));
    }

    private int ContactActiveLeases => _contactTrialLeases.Count + _contactTrialMatrixLeases.Count + _contactChannels.Count;

    internal static void RestoreContactTrialEntries<TState, TEntry>(TState state, List<TEntry> entries,
        Func<TState, TEntry, bool> restore)
    {
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            if (restore(state, entries[i]))
                entries.RemoveAt(i);
        }
    }

    private bool RestoreContactTrialPosition(ContactTrialLease lease)
    {
        try
        {
            if (ContactTrialAlive(lease.Bone) && Invoke(_contactTrialParent, lease.Bone) == lease.Parent)
            {
                Vector3 current = ReadContactTrialVector(Invoke(_contactTrialLocalPosition, lease.Bone));
                if (lease.Position.Owns(current))
                {
                    _ = InvokeWithObjectArgument(_contactTrialSetPosition, lease.Bone, BoxContactTrialVector(lease.Position.Baseline));
                    if (ReadContactTrialVector(Invoke(_contactTrialLocalPosition, lease.Bone)) != lease.Position.Baseline)
                        throw new InvalidOperationException("The original local position did not round trip.");
                    _contactTrialRestores++;
                }
                else if (current != lease.Position.Baseline)
                {
                    _contactTrialExternalChanges++;
                    FailContactBoneTrial("animation-replaced-test-value");
                }
            }
            else FailContactBoneTrial("bone-or-parent-changed-during-render");
            return true;
        }
        catch (Exception error)
        {
            FailContactBoneTrial("restore", error);
            return false;
        }
    }

    private bool ContactTrialBindingMatches(ContactTrialTarget target) =>
        ContactTrialAlive(target.Animator) && ContactTrialAlive(target.Actor) && ContactTrialAlive(target.Skin) &&
        ContactTrialAlive(target.Bone) && ContactTrialAlive(target.Parent) &&
        ContactTrialSkinActive(target.Skin) &&
        ReadContactBool(Invoke(_contactTrialSkinEnabled, target.Skin)) &&
        Invoke(_contactTrialGetTransform, target.Animator) == target.Actor &&
        ContactChildOf(_contactTrialChildOf, Invoke(_contactTrialGetTransform, target.Skin), target.Actor) &&
        Invoke(_contactTrialMesh, target.Skin) == target.Mesh && Invoke(_contactTrialRoot, target.Skin) == target.Root &&
        Invoke(_contactTrialParent, target.Bone) == target.Parent &&
        ContactChildOf(_contactTrialChildOf, target.Root, target.Actor) &&
        ContactChildOf(_contactTrialChildOf, target.Bone, target.Root);

    private bool ContactTrialSkinActive(IntPtr skin)
    {
        IntPtr gameObject = Invoke(_contactTrialSkinGameObject, skin);
        return gameObject != IntPtr.Zero && ReadContactBool(Invoke(_contactTrialObjectActive, gameObject));
    }

    internal static bool ContactMarkerActive(bool collision, int touchMask, bool trialPulse) =>
        collision ? (touchMask & 3) != 0 : trialPulse;

    private bool ContactTrialSlotStillMatches(ContactTrialTarget target)
    {
        if (!ContactTrialAlive(target.Skin) || Invoke(_contactTrialMesh, target.Skin) != target.Mesh)
            return false;
        IntPtr bones = Invoke(_contactTrialBones, target.Skin);
        return bones != IntPtr.Zero && Marshal.ReadInt64(bones, 3 * IntPtr.Size) == target.Slots &&
            Marshal.ReadIntPtr(bones, (4 + target.Slot) * IntPtr.Size) == target.Bone;
    }

    private void ReleaseContactTrialTargets()
    {
        foreach (ContactTrialTarget target in _contactBoneTrialTargets)
            FreeContactTrialHandles(target.Handles);
        _contactBoneTrialTargets.Clear();
    }

    private void FreeContactTrialHandles(IntPtr[] handles)
    {
        foreach (IntPtr handle in handles)
            if (handle != IntPtr.Zero) _contactTrialFreeHandle!(handle);
    }

    private void ReportContactBoneTrialPair(DateTimeOffset now)
    {
        if (!_contactTrialPairPrepared || _contactTrialFaulted || _contactTrialPairTargets.Length == 0) return;
        if (_contactTrialReadyTick == 0 && ContactEyeScopesReady(_contactTrialSeenRoles, _contactCollisionEnabled))
        {
            _contactTrialReadyTick = Environment.TickCount64;
            LogContactPilot(now, "contact-bone-trial-scopes-observed",
                $"owner=unity;eyes=left,right;source={((_contactTrialSeenRoles & 4) != 0 ? "observed" : "not-yet-observed")};writes=0");
        }
        if (_contactCollisionEnabled) { ReportContactCollisionPair(now); return; }
        if (_contactTrialReadyTick == 0 || _contactTrialLoggedPhase == _contactTrialPulse)
            return;
        _contactTrialLoggedPhase = _contactTrialPulse;
        int bothEyes = _contactTrialPairTargets.Count(target => target.EyeWrites == 3);
        int targets = _contactTrialPairTargets.Count(target => !target.Invalid);
        LogContactPilot(now, "contact-bone-trial-phase",
            $"phase={(_contactTrialPulse ? "offset" : "baseline")};actors={_contactTrialPairTargets.Where(target => !target.Invalid).Select(target => target.ActorId).Distinct().Count()};targets={targets};bothEyes={bothEyes};writes={_contactTrialWrites};restores={_contactTrialRestores};externalChanges={_contactTrialExternalChanges};sourceCalls={_contactTrialSourceCalls};matrixScopes={_contactTrialMatrixScopes};activeLeases={ContactActiveLeases};visualIsolation=unverified");
    }

    internal static bool ContactEyeScopesReady(int seenRoles, bool collision) =>
        (seenRoles & (collision ? 3 : 7)) == (collision ? 3 : 7);

    private void ForceContactTrialMatrices(IntPtr skin)
    {
        if (!ReadContactBool(Invoke(_contactTrialGetForceMatrices, skin)))
        {
            _contactTrialMatrixLeases.Add(skin);
            _ = InvokeWithObjectArgument(_contactTrialSetForceMatrices, skin, _contactTrialTrue);
            if (!ReadContactBool(Invoke(_contactTrialGetForceMatrices, skin)))
                throw new InvalidOperationException("Skin matrix recalculation did not enable.");
        }
        _contactTrialMatrixScopes++;
    }

    private bool RestoreContactTrialMatrixMode(IntPtr skin)
    {
        try
        {
            if (ContactTrialAlive(skin) && ReadContactBool(Invoke(_contactTrialGetForceMatrices, skin)))
            {
                _ = InvokeWithObjectArgument(_contactTrialSetForceMatrices, skin, _contactTrialFalse);
                if (ReadContactBool(Invoke(_contactTrialGetForceMatrices, skin)))
                    throw new InvalidOperationException("The original skin matrix mode did not restore.");
            }
            return true;
        }
        catch (Exception error)
        {
            FailContactBoneTrial("restore-skin-matrices", error);
            return false;
        }
    }

    private void FailContactBoneTrial(string stage, Exception? error = null)
    {
        if (_contactTrialFaulted) return;
        _contactTrialFaulted = true;
        ResetContactCollision();
        _contactTrialPendingFailure =
            $"stage={stage};error={error?.GetType().Name ?? "none"};writes={_contactTrialWrites};restores={_contactTrialRestores};activeLeases={ContactActiveLeases}";
    }

    private bool ContactTrialAlive(IntPtr value) => value != IntPtr.Zero &&
        ReadContactBool(InvokeWithObjectArgument(_contactTrialAlive, IntPtr.Zero, value));

    private Vector3 ReadContactTrialVector(IntPtr boxed)
    {
        IntPtr value = boxed == IntPtr.Zero ? IntPtr.Zero : _api.ObjectUnbox(boxed);
        return value == IntPtr.Zero ? throw new InvalidOperationException("The trial transform returned no vector.") :
            Marshal.PtrToStructure<Vector3>(value);
    }

    private unsafe IntPtr BoxContactTrialVector(Vector3 value) => _api.ValueBox(_contactTrialVectorClass, (IntPtr)(&value));

    private static bool ContactTrialFinite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

    private string DescribeContactTrialReach(ContactHandFrame frame)
    {
        if (_contactTrialPairTargets.Length == 0 || _contactTrialGeneration != _stereoGenerationRetireCount || frame.Scale <= 0f)
            return string.Empty;
        float left = float.PositiveInfinity, right = float.PositiveInfinity;
        foreach (ContactTrialTarget target in _contactTrialPairTargets)
        {
            if (target.Invalid || !ContactTrialAlive(target.Bone) || Invoke(_contactTrialParent, target.Bone) != target.Parent)
                continue;
            Vector3 bone = ReadContactTrialVector(Invoke(_contactTrialWorldPosition, target.Bone));
            if (!ContactTrialFinite(bone)) continue;
            if (frame.Left is Vector3 leftHand) left = MathF.Min(left, Vector3.Distance(leftHand, bone) / frame.Scale);
            if (frame.Right is Vector3 rightHand) right = MathF.Min(right, Vector3.Distance(rightHand, bone) / frame.Scale);
        }
        return $";nearestBoneRangePhysicalM=left:{Distance(left)},right:{Distance(right)}";

        static string Distance(float value) => float.IsFinite(value)
            ? value.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) : "unavailable";
    }

    private ContactTrialMarker[] GetContactTrialMarkers(DateTimeOffset now, IntPtr coreImage)
    {
        VrTrackingSettings tracking = VrSettingsRuntime.Current.Tracking;
        bool showPoints = !_contactCollisionEnabled || tracking.ShowContactLocations;
        bool showRanges = _contactCollisionEnabled && tracking.ContactShowRanges;
        if (!_contactTrialPairPrepared || _contactTrialMarkerFailed || _contactTrialPairTargets.Length == 0 ||
            (!showPoints && !showRanges))
            return Array.Empty<ContactTrialMarker>();
        try
        {
            if (_contactTrialMarkerBaseline.Object == IntPtr.Zero || _contactTrialMarkerOffset.Object == IntPtr.Zero)
            {
                _contactTrialBounds = FindMethod(coreImage, "UnityEngine", "Renderer", "get_bounds");
                IntPtr boundsClass = FindClass(coreImage, "UnityEngine", "Bounds");
                _contactTrialBoundsCenter = FindMethod(boundsClass, "get_center");
                _contactTrialBoundsExtents = FindMethod(boundsClass, "get_extents");
                IntPtr materialClass = FindClass(coreImage, "UnityEngine", "Material");
                IntPtr copy = FindMethodBySignature(materialClass, ".ctor", "UnityEngine.Material");
                if (_contactTrialMarkerBaseline.Object == IntPtr.Zero)
                    _contactTrialMarkerBaseline = CreateMarkerMaterial(new Vector3(0.05f, 0.85f, 1f));
                if (_contactTrialMarkerOffset.Object == IntPtr.Zero)
                    _contactTrialMarkerOffset = CreateMarkerMaterial(new Vector3(1f, 0.52f, 0.04f));

                ContactMarkerAsset CreateMarkerMaterial(Vector3 color) =>
                    CreateContactMarkerAsset(coreImage, materialClass, "contact trial marker Material", material =>
                    {
                        _ = InvokeWithObjectArgument(copy, material, _contactMaterial);
                        SetContactMarkerColor(coreImage, materialClass, material, color);
                    });
            }
            if (showRanges && _contactRangeMeshAsset.Object == IntPtr.Zero && !_contactRangeMarkerFailed)
            {
                try
                {
                    IntPtr meshClass = FindClass(coreImage, "UnityEngine", "Mesh");
                    _contactRangeMeshAsset = CreateContactMarkerAsset(coreImage, meshClass, "contact range mesh",
                        mesh => InitializeContactMarkerMesh(coreImage, meshClass, mesh, ContactRangeVertices()));
                }
                catch (Exception error)
                {
                    _contactRangeMarkerFailed = true;
                    LogContactPilot(now, "contact-range-marker-unavailable", error.GetType().Name);
                }
            }
            ContactTrialTarget[] visibleTargets = _contactTrialPairTargets.Where(target => !target.Invalid && ContactTrialAlive(target.Skin)).ToArray();
            var markers = new List<ContactTrialMarker>(visibleTargets.Length * 2);
            if (showPoints)
            {
                foreach (var actorTargets in visibleTargets.GroupBy(target => target.ActorId))
                {
                    ContactTrialTarget target = actorTargets.First();
                    int actorTouchMask = actorTargets.Aggregate(0, (mask, part) => mask | part.TouchMask);
                    IntPtr boxedBounds = Invoke(_contactTrialBounds, target.Skin);
                    IntPtr bounds = boxedBounds == IntPtr.Zero ? IntPtr.Zero : _api.ObjectUnbox(boxedBounds);
                    if (bounds == IntPtr.Zero) continue;
                    Vector3 center = ReadContactTrialVector(Invoke(_contactTrialBoundsCenter, bounds));
                    Vector3 extents = ReadContactTrialVector(Invoke(_contactTrialBoundsExtents, bounds));
                    if (!ContactTrialFinite(center) || !ContactTrialFinite(extents) || extents.Y is <= 0f or > 3f)
                        continue;
                    float radius = Math.Clamp(extents.Y * 0.055f, 0.028f, 0.065f);
                    markers.Add(new ContactTrialMarker(center + Vector3.UnitY * (extents.Y + radius * 2f),
                        radius, ContactMarkerActive(_contactCollisionEnabled, actorTouchMask, _contactTrialPulse)
                            ? _contactTrialMarkerOffset.Object : _contactTrialMarkerBaseline.Object, _contactMesh));
                }
            }
            float handRadius = _contactCollisionScale * tracking.ContactHandRadiusMeters;
            foreach (ContactTrialTarget target in visibleTargets)
            {
                if (!_contactCollisionEnabled || target.PairCenter is not Vector3 contactCenter) continue;
                IntPtr material = target.TouchMask != 0 ? _contactTrialMarkerOffset.Object : _contactTrialMarkerBaseline.Object;
                if (showPoints)
                    markers.Add(new ContactTrialMarker(target.PairSurface ?? contactCenter, .006f, material, _contactMesh));
                if (showRanges && _contactRangeMeshAsset.Object != IntPtr.Zero)
                    markers.Add(new ContactTrialMarker(contactCenter, target.PairRadius + handRadius, material,
                        _contactRangeMeshAsset.Object, target.Region));
            }
            if (!_contactTrialMarkerLogged && markers.Count != 0)
            {
                _contactTrialMarkerLogged = true;
                LogContactPilot(now, "contact-bone-trial-marker", $"count={markers.Count};surfacePoints={showPoints};ranges={markers.Count(marker => marker.IsRange)};baseline=cyan;contact=amber;perTarget=true;eyes-only=true");
            }
            return markers.ToArray();
        }
        catch (Exception error)
        {
            _contactTrialMarkerFailed = true;
            LogContactPilot(now, "contact-bone-trial-marker-unavailable", error.GetType().Name);
            return Array.Empty<ContactTrialMarker>();
        }
    }

    internal static List<Vector3> ContactRangeVertices()
    {
        const int segments = 32;
        const float halfWidth = .025f;
        float ringRadius = MathF.Sqrt(1f - halfWidth * halfWidth);
        var vertices = new List<Vector3>(3 * segments * 6);
        var axes = new[] { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ };
        for (int axis = 0; axis < axes.Length; axis++)
        {
            Vector3 width = axes[axis] * halfWidth;
            Vector3 u = axes[(axis + 1) % 3], v = axes[(axis + 2) % 3];
            for (int i = 0; i < segments; i++)
            {
                float a = 2f * MathF.PI * i / segments, b = 2f * MathF.PI * (i + 1) / segments;
                Vector3 start = (u * MathF.Cos(a) + v * MathF.Sin(a)) * ringRadius;
                Vector3 end = (u * MathF.Cos(b) + v * MathF.Sin(b)) * ringRadius;
                vertices.Add(start - width); vertices.Add(start + width); vertices.Add(end + width);
                vertices.Add(start - width); vertices.Add(end + width); vertices.Add(end - width);
            }
        }
        return vertices;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ContactCameraStack(IntPtr context, IntPtr camera, IntPtr method);

}
