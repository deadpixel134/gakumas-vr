using System.Numerics;
using System.Runtime.InteropServices;
using GakumasVR.Core;

namespace Doorstop;

internal sealed partial class MainThreadSampler
{
    private const string RenderTargetIdentifierType = "UnityEngine.Rendering.RenderTargetIdentifier";
    private const string RenderBufferLoadActionType = "UnityEngine.Rendering.RenderBufferLoadAction";
    private const string RenderBufferStoreActionType = "UnityEngine.Rendering.RenderBufferStoreAction";
    private const string ScriptableRenderContextType = "UnityEngine.Rendering.ScriptableRenderContext";
    private const string RenderingDataType = "UnityEngine.Rendering.Universal.RenderingData";
    private const int MaxContactRenderPasses = 64;
    private const int NamedRenderTarget = -4;
    private const int RenderTextureTarget = -2;
    private const int CurrentCameraTarget = 2;

#if !GAKUMAS_PUBLIC
    private readonly record struct ContactTrialMarker(Vector3 Center, float Radius, IntPtr Material, IntPtr Mesh,
        ContactRegion RangeRegion = ContactRegion.Unknown)
    {
        public bool IsRange => RangeRegion != ContactRegion.Unknown;
    }
    private int _contactRangeOverlayRegions;
    private sealed record ContactHandFrame(OpenXrStereoStateSnapshot Stereo, Vector3? Left, Vector3? Right,
        float Radius, float Scale, float NearClip, ContactTrialMarker[] TargetMarkers);
#else
    private sealed record ContactHandFrame(OpenXrStereoStateSnapshot Stereo, Vector3? Left, Vector3? Right,
        float Radius, float Scale, float NearClip);
#endif

    private readonly record struct ContactDepthStore(IntPtr Pass, int Action, byte Overridden);
    private ContactHandFrame? _contactHandFrame;
    private ContactRendererExecute? _contactRendererReplacement, _contactRendererOriginal;
    private ContactRendererFinish? _contactFinishReplacement, _contactFinishOriginal;
    private uint _contactUnityThread;
    private bool _contactContextPassedByAddress;
    private bool _contactCompositionFailed;
    private bool _contactFinalOverlayFailed;
    private int _contactComposedEyes;
    private IntPtr _contactUniversalRenderer, _contactVlRenderer;
    private IntPtr _contactPassQueue, _contactPassCount, _contactPassItem, _contactStoreActionField, _contactStoreOverrideField;
    private IntPtr _contactNativeRenderPassField, _contactDepthTexture, _contactDepthNameId, _contactCurrentDepthHandle;
    private IntPtr _contactDepthScopeCamera, _contactDepthScopeRenderer, _contactDepthScopeColor;
    private int _contactDescriptorFromCamera, _contactDescriptorHeight, _contactDescriptorSamples, _contactDescriptorSlices;
    private IntPtr _contactTextureWidth, _contactTextureHeight, _contactTextureSamples, _contactTextureDepth, _contactTextureMemoryless, _contactTextureCreated;
    private IntPtr _contactCameraTarget, _contactCameraViewport, _contactCameraView, _contactCameraProjection;
    private IntPtr _contactCommand, _contactCommandClear, _contactCommandTarget, _contactCommandViewport, _contactCommandDraw;
    private IntPtr _contactContextExecute, _contactGpuProjection, _contactSetMatrices;
    private IntPtr _contactGraphicsExecute, _contactNoDepth;
    private IntPtr _contactIdentifierClass, _contactIdentifierCtor, _contactIdentifierType, _contactIdentifierName, _contactLoad, _contactStore, _contactZero, _contactTrue;
    private IntPtr _contactIdentifierInstance, _contactFindTexture, _contactRenderTextureClass, _contactMscorlib;

    private void EnsureContactComposition(IntPtr coreImage)
    {
        if (_contactRendererOriginal is not null) return;
        IntPtr urp = FindImage("Unity.RenderPipelines.Universal.Runtime.dll");
        IntPtr renderer = FindClass(urp, "UnityEngine.Rendering.Universal", "ScriptableRenderer");
        IntPtr execute = FindMethodBySignature(renderer, "Execute", ScriptableRenderContextType,
            RenderingDataType + "&");
        IntPtr finish;
        try
        {
            finish = FindMethodBySignature(renderer, "InternalFinishRendering", ScriptableRenderContextType,
                "System.Boolean", RenderingDataType);
        }
        catch (MissingMethodException)
        {
            finish = FindMethodBySignature(renderer, "InternalFinishRendering", ScriptableRenderContextType,
                "System.Boolean", RenderingDataType + "&");
        }
        foreach (IntPtr method in new[] { execute, finish })
        {
            if ((_api.MethodGetFlags(method, out _) & 0x10) != 0 ||
                GetTypeName(_api.MethodGetReturnType(method)) != "System.Void")
                throw new MissingMethodException("Unexpected ScriptableRenderer callback signature.");
        }
        ReadRenderingDataCameraOffsets(urp);

        IntPtr context = FindClass(coreImage, "UnityEngine.Rendering", "ScriptableRenderContext");
        var valueSize = Marshal.GetDelegateForFunctionPointer<ContactClassValueSize>(
            NativeLibrary.GetExport(NativeMethods.GetModuleHandle("GameAssembly.dll"), "il2cpp_class_value_size"));
        int contextSize = valueSize(context, out _);
        // Win64 passes the pointer-only context directly and the context with a safety handle by address.
        if (IntPtr.Size != 8 || contextSize is not (8 or 24))
            throw new InvalidOperationException("Unexpected ScriptableRenderContext value size.");
        if (valueSize(FindClass(urp, "UnityEngine.Rendering.Universal", "RenderingData"), out _) <= 8)
            throw new InvalidOperationException("Expected RenderingData to be passed indirectly on Win64.");
        _contactContextPassedByAddress = contextSize == 24;
        _contactContextExecute = FindMethodBySignature(context, "ExecuteCommandBuffer", "UnityEngine.Rendering.CommandBuffer");
        ReadContactDescriptorLayout(coreImage, urp, valueSize);

        _contactUniversalRenderer = FindClass(urp, "UnityEngine.Rendering.Universal", "UniversalRenderer");
        _contactVlRenderer = FindClassAcrossImages("VL.Rendering", "VLSRPRenderer");
        if (_api.ClassGetParent(_contactVlRenderer) != _contactUniversalRenderer)
            throw new InvalidOperationException("Unexpected VLSRPRenderer base class.");
        _contactCurrentDepthHandle = FindMethod(renderer, "get_cameraDepthTargetHandle");
        _contactPassQueue = FindMethod(renderer, "get_activeRenderPassQueue");
        IntPtr pass = FindClass(urp, "UnityEngine.Rendering.Universal", "ScriptableRenderPass");
        _contactStoreActionField = FindField(pass, "m_DepthStoreAction");
        _contactStoreOverrideField = FindField(pass, "m_OverriddenDepthStoreAction");
        _contactNativeRenderPassField = FindField(renderer, "useRenderPassEnabled");

        IntPtr rtHandle = FindClass(FindImage("Unity.RenderPipelines.Core.Runtime.dll"), "UnityEngine.Rendering", "RTHandle");
        _contactDepthTexture = FindMethod(rtHandle, "get_rt");
        _contactDepthNameId = FindMethod(rtHandle, "get_nameID");
        IntPtr texture = FindClass(coreImage, "UnityEngine", "RenderTexture");
        _contactRenderTextureClass = texture;
        _contactTextureWidth = FindMethod(texture, "get_width");
        _contactTextureHeight = FindMethod(texture, "get_height");
        _contactTextureDepth = FindMethod(texture, "get_depth");
        _contactTextureSamples = FindMethod(texture, "get_antiAliasing");
        _contactTextureMemoryless = FindMethod(texture, "get_memorylessMode");
        _contactTextureCreated = FindMethod(texture, "IsCreated");
        _contactIdentifierType = FindField(_contactIdentifierClass, "m_Type");
        _contactIdentifierName = FindField(_contactIdentifierClass, "m_NameID");
        _contactIdentifierInstance = FindField(_contactIdentifierClass, "m_InstanceID");
        _contactFindTexture = FindContactTextureLookup(coreImage);
        _contactUnityThread = NativeMethods.GetCurrentThreadId();
        _contactRendererReplacement = OnContactRendererExecute;
        IntPtr target = Marshal.ReadIntPtr(execute);
        IntPtr finishTarget = Marshal.ReadIntPtr(finish);
        if (target == IntPtr.Zero || finishTarget == IntPtr.Zero)
            throw new MissingMethodException("A ScriptableRenderer callback has no native pointer.");
        var hook = Marshal.GetDelegateForFunctionPointer<DobbyHookDelegate>(NativeLibrary.GetExport(_dobbyLibrary, "DobbyHook"));
        if (_contactFinishOriginal is null)
        {
            _contactFinishReplacement = OnContactRendererFinish;
            int finishStatus = hook(finishTarget, Marshal.GetFunctionPointerForDelegate(_contactFinishReplacement), out IntPtr finishOriginal);
            if (finishStatus != 0 || finishOriginal == IntPtr.Zero)
                throw new InvalidOperationException($"ScriptableRenderer.InternalFinishRendering hook failed ({finishStatus}).");
            _contactFinishOriginal = Marshal.GetDelegateForFunctionPointer<ContactRendererFinish>(finishOriginal);
        }
        int status = hook(target, Marshal.GetFunctionPointerForDelegate(_contactRendererReplacement), out IntPtr original);
        if (status != 0 || original == IntPtr.Zero)
            throw new InvalidOperationException($"ScriptableRenderer.Execute hook failed ({status}).");
        _contactRendererOriginal = Marshal.GetDelegateForFunctionPointer<ContactRendererExecute>(original);
    }

    private void EnsureContactOverlayCommand(IntPtr coreImage)
    {
        if (_contactCommand != IntPtr.Zero) return;
        IntPtr camera = FindClass(coreImage, "UnityEngine", "Camera");
        _contactCameraTarget = FindMethod(camera, "get_targetTexture");
        _contactCameraViewport = FindMethod(camera, "get_pixelRect");
        _contactCameraView = FindMethod(camera, "get_worldToCameraMatrix");
        _contactCameraProjection = FindMethod(camera, "get_projectionMatrix");
        _contactGpuProjection = FindMethodBySignature(FindClass(coreImage, "UnityEngine", "GL"),
            "GetGPUProjectionMatrix", "UnityEngine.Matrix4x4", "System.Boolean");
        IntPtr urp = FindImage("Unity.RenderPipelines.Universal.Runtime.dll");
        _contactSetMatrices = FindMethodBySignature(FindClass(urp, "UnityEngine.Rendering.Universal", "RenderingUtils"),
            "SetViewAndProjectionMatrices", "UnityEngine.Rendering.CommandBuffer", "UnityEngine.Matrix4x4",
            "UnityEngine.Matrix4x4", "System.Boolean");
        _contactIdentifierClass = FindClass(coreImage, "UnityEngine.Rendering", "RenderTargetIdentifier");
        _contactIdentifierCtor = FindMethodBySignature(_contactIdentifierClass, ".ctor", "UnityEngine.Texture");
        IntPtr cmd = FindClass(coreImage, "UnityEngine.Rendering", "CommandBuffer");
        _contactCommandClear = FindMethod(cmd, "Clear");
        _contactCommandTarget = FindMethodBySignature(cmd, "SetRenderTarget", RenderTargetIdentifierType,
            RenderBufferLoadActionType, RenderBufferStoreActionType, RenderTargetIdentifierType,
            RenderBufferLoadActionType, RenderBufferStoreActionType);
        _contactCommandViewport = FindMethodBySignature(cmd, "SetViewport", "UnityEngine.Rect");
        _contactCommandDraw = FindMethodBySignature(cmd, "DrawMesh", "UnityEngine.Mesh", "UnityEngine.Matrix4x4",
            "UnityEngine.Material", "System.Int32", "System.Int32", "UnityEngine.MaterialPropertyBlock");
        _contactMscorlib = FindImage("mscorlib.dll");
        _contactZero = RootContactValue(BoxInt32(_contactMscorlib, 0));
        _contactTrue = RootContactValue(BoxBoolean(_contactMscorlib, true));
        _contactLoad = RootContactValue(BoxContactAction(coreImage, "RenderBufferLoadAction", 0));
        _contactStore = RootContactValue(BoxContactAction(coreImage, "RenderBufferStoreAction", 0));
        IntPtr command = _api.ObjectNew(cmd);
        _ = RootObject(command, "contact composition CommandBuffer");
        _ = Invoke(FindMethod(cmd, ".ctor"), command);
        _contactCommand = command;
    }

    private void OnContactRendererExecute(IntPtr renderer, IntPtr context, IntPtr renderingData, IntPtr method)
    {
        IntPtr observedCamera = BeginContactRenderEvidence(renderingData);
        int enter = observedCamera == IntPtr.Zero ? 0 : ++_contactRenderSequence;
        try
        {
            RenderContactCamera(renderer, context, renderingData, method);
        }
        finally
        {
            EndContactRenderEvidence(observedCamera, enter);
        }
    }

    private void RenderContactCamera(IntPtr renderer, IntPtr context, IntPtr renderingData, IntPtr method)
    {
        ContactRendererExecute original = _contactRendererOriginal!;
        ContactHandFrame? frame = _contactHandFrame;
        if (_contactCompositionFailed || frame is null || !_stereoNaturalRenderArmed ||
            !ReferenceEquals(frame.Stereo, _stereoNaturalRenderState) ||
            NativeMethods.GetCurrentThreadId() != _contactUnityThread)
        {
            original(renderer, context, renderingData, method);
            return;
        }

        bool ownsDepthScope = false;
        Span<ContactDepthStore> changed = stackalloc ContactDepthStore[MaxContactRenderPasses];
        int changedCount = 0;
        try
        {
            try
            {
                IntPtr camera = TryReadRenderingDataCamera(renderingData);
                if (_contactDepthScopeCamera == IntPtr.Zero &&
                    (camera == _stereoLeftCamera || camera == _stereoRightCamera))
                {
                    IntPtr color = camera == _stereoLeftCamera ? _stereoNaturalLeftRenderTexture : _stereoNaturalRightRenderTexture;
                    ValidateContactEye(renderer, camera, color);
                    RetainContactPassDepth(renderer, changed, ref changedCount);
                    ownsDepthScope = true;
                    _contactDepthScopeCamera = camera;
                    _contactDepthScopeRenderer = renderer;
                    _contactDepthScopeColor = color;
                }
            }
            catch (Exception error)
            {
                DisableContactComposition("prepare-depth: " + error.Message.Split('\n')[0]);
            }
            original(renderer, context, renderingData, method);
        }
        finally
        {
            if (ownsDepthScope)
                _contactDepthScopeCamera = _contactDepthScopeRenderer = _contactDepthScopeColor = IntPtr.Zero;
            for (int i = changedCount - 1; i >= 0; i--)
            {
                ContactDepthStore saved = changed[i];
                WriteContactDepthStore(saved.Pass, saved.Action, saved.Overridden);
            }
        }
    }

    private void OnContactRendererFinish(IntPtr renderer, IntPtr context, byte resolveFinalTarget,
        IntPtr renderingData, IntPtr method)
    {
        // The camera's final color is ready, and cleanup has not released its named attachments yet.
        if (resolveFinalTarget != 0 && !_contactCompositionFailed && _contactHandFrame is { } frame &&
            _contactDepthScopeCamera != IntPtr.Zero && renderer == _contactDepthScopeRenderer &&
            NativeMethods.GetCurrentThreadId() == _contactUnityThread)
        {
            string stage = "resolve-depth";
            try
            {
                IntPtr camera = TryReadRenderingDataCamera(renderingData, out IntPtr cameraField);
                if (camera == _contactDepthScopeCamera)
                {
                    IntPtr depth = ResolveContactDepth(renderer, _contactDepthScopeColor, cameraField, out string source);
                    stage = "draw-with-depth";
                    ComposeContactHands(frame, context, camera, _contactDepthScopeColor, depth, source);
                }
            }
            catch (Exception error) { DisableContactComposition(stage + ": " + error.Message.Split('\n')[0]); }
        }
        _contactFinishOriginal!(renderer, context, resolveFinalTarget, renderingData, method);
    }

    private void ValidateContactEye(IntPtr renderer, IntPtr camera, IntPtr color)
    {
        if (color == IntPtr.Zero || Invoke(_contactCameraTarget, camera) != color)
            throw new InvalidOperationException("The active camera target is not the armed eye texture.");
        IntPtr rendererClass = Marshal.ReadIntPtr(renderer);
        if (_stereoCloneRenderType != 0 || (rendererClass != _contactUniversalRenderer && rendererClass != _contactVlRenderer))
            throw new InvalidOperationException("The eye camera is not using a supported base renderer.");
        if (ReadContactFieldBoolean(renderer, _contactNativeRenderPassField))
            throw new InvalidOperationException("The eye renderer enables native render passes.");
    }

    private void ReadContactDescriptorLayout(IntPtr coreImage, IntPtr urp, ContactClassValueSize valueSize)
    {
        IntPtr cameraData = FindClass(urp, "UnityEngine.Rendering.Universal", "CameraData");
        IntPtr descriptor = FindClass(coreImage, "UnityEngine", "RenderTextureDescriptor");
        int descriptorOffset = _api.FieldGetOffset(FindField(cameraData, "cameraTargetDescriptor"));
        int cameraDataSize = valueSize(cameraData, out _);
        int descriptorSize = valueSize(descriptor, out _);
        int width = _api.FieldGetOffset(FindField(descriptor, "<width>k__BackingField"));
        _contactDescriptorFromCamera = descriptorOffset - _cameraDataCameraOffset;
        _contactDescriptorHeight = Offset("<height>k__BackingField");
        _contactDescriptorSamples = Offset("<msaaSamples>k__BackingField");
        _contactDescriptorSlices = Offset("<volumeDepth>k__BackingField");
        if (descriptorOffset < 0 || descriptorSize <= 0 || descriptorSize > cameraDataSize ||
            descriptorOffset > cameraDataSize - descriptorSize + 2 * IntPtr.Size ||
            (width != 0 && width != 2 * IntPtr.Size))
            throw new InvalidOperationException("Unexpected eye target descriptor layout.");

        int Offset(string name)
        {
            int offset = _api.FieldGetOffset(FindField(descriptor, name)) - width;
            return offset >= 0 && offset <= descriptorSize - sizeof(int) ? offset :
                throw new InvalidOperationException("Unexpected eye target descriptor field layout.");
        }
    }

    private IntPtr ResolveContactDepth(IntPtr renderer, IntPtr color, IntPtr cameraField, out string source)
    {
        IntPtr handle = Invoke(_contactCurrentDepthHandle, renderer);
        if (handle == IntPtr.Zero)
            throw new InvalidOperationException("The renderer has no live eye depth attachment.");
        IntPtr identifier = Invoke(_contactDepthNameId, handle);
        if (identifier == IntPtr.Zero)
            throw new InvalidOperationException("The renderer has no eye depth target identifier.");
        IntPtr depth = Invoke(_contactDepthTexture, handle);
        if (depth == IntPtr.Zero)
        {
            int type = ReadContactFieldInt(identifier, _contactIdentifierType);
            int name = ReadContactFieldInt(identifier, _contactIdentifierName);
            if (type == NamedRenderTarget && name >= 0)
            {
                IntPtr descriptor = IntPtr.Add(cameraField, _contactDescriptorFromCamera);
                int width = Marshal.ReadInt32(descriptor);
                int height = Marshal.ReadInt32(descriptor, _contactDescriptorHeight);
                int samples = Marshal.ReadInt32(descriptor, _contactDescriptorSamples);
                int slices = Marshal.ReadInt32(descriptor, _contactDescriptorSlices);
                if (samples != 1 || slices != 1 || ReadContactInt(_contactTextureSamples, color) != 1 ||
                    width != ReadContactInt(_contactTextureWidth, color) || height != ReadContactInt(_contactTextureHeight, color))
                    throw new InvalidOperationException($"The named eye depth descriptor is incompatible ({width}x{height}x{samples}; slices={slices}).");
                source = $"renderer-named-{name}";
                return identifier;
            }
            if (type == CurrentCameraTarget)
            {
                depth = color;
                identifier = CreateContactTarget(color);
            }
            else if (type == RenderTextureTarget && _contactFindTexture != IntPtr.Zero)
            {
                int instance = ReadContactFieldInt(identifier, _contactIdentifierInstance);
                if (instance == 0)
                    throw new InvalidOperationException("The eye depth identifier has no texture instance.");
                depth = InvokeWithObjectArgument(_contactFindTexture, IntPtr.Zero, BoxInt32(_contactMscorlib, instance));
                if (depth == IntPtr.Zero || Marshal.ReadIntPtr(depth) != _contactRenderTextureClass)
                    throw new InvalidOperationException("The identified eye depth texture could not be resolved.");
            }
            else
                throw new InvalidOperationException($"Unsupported live eye depth identifier (type={type};name={name}).");
        }

        int depthWidth = ReadContactInt(_contactTextureWidth, depth);
        int depthHeight = ReadContactInt(_contactTextureHeight, depth);
        int colorWidth = ReadContactInt(_contactTextureWidth, color);
        int colorHeight = ReadContactInt(_contactTextureHeight, color);
        int depthBits = ReadContactInt(_contactTextureDepth, depth);
        int memoryless = ReadContactInt(_contactTextureMemoryless, depth);
        int depthSamples = ReadContactInt(_contactTextureSamples, depth);
        int colorSamples = ReadContactInt(_contactTextureSamples, color);
        if (depthBits <= 0 || memoryless != 0 || depthSamples != 1 || colorSamples != 1 ||
            depthWidth != colorWidth || depthHeight != colorHeight || !ReadContactBool(Invoke(_contactTextureCreated, depth)))
            throw new InvalidOperationException($"Incompatible eye depth: {depthWidth}x{depthHeight}x{depthSamples}/{depthBits}bit/{memoryless};color={colorWidth}x{colorHeight}x{colorSamples}.");
        source = depth == color ? "eye-target" : "renderer-texture";
        return identifier;
    }

    private IntPtr FindContactTextureLookup(IntPtr coreImage)
    {
        // The public Resources wrapper can be stripped from the game build.
        foreach ((string klass, string name) in new[] { ("Resources", "InstanceIDToObject"), ("Object", "FindObjectFromInstanceID") })
        {
            IntPtr method = TryFindMethod(FindClass(coreImage, "UnityEngine", klass), name, 1);
            if (method != IntPtr.Zero &&
                GetTypeName(_api.MethodGetParam(method, 0)) == "System.Int32" &&
                GetTypeName(_api.MethodGetReturnType(method)) == "UnityEngine.Object" &&
                (_api.MethodGetFlags(method, out _) & 0x10) != 0)
                return method;
        }
        return IntPtr.Zero;
    }

    private void CompleteContactOverlay(IntPtr coreImage)
    {
        if (_contactHandFrame is not { } frame || !ReferenceEquals(frame.Stereo, _stereoNaturalRenderState) ||
            _contactFinalOverlayFailed) return;
        int rangeCount = 0;
#if !GAKUMAS_PUBLIC
        int rangeRegions = 0;
        foreach (ContactTrialMarker marker in frame.TargetMarkers)
        {
            if (!marker.IsRange) continue;
            rangeCount++;
            rangeRegions |= 1 << (int)marker.RangeRegion;
        }
#endif
        if (_contactComposedEyes == 3 && rangeCount == 0) return;
        if (_contactComposedEyes != 3)
            DisableContactComposition($"The completed pair did not compose both eyes;postVfxEyeMask={_contactComposedEyes}.");
        try
        {
            if (_contactNoDepth == IntPtr.Zero)
            {
                _contactGraphicsExecute = FindMethodBySignature(FindClass(coreImage, "UnityEngine", "Graphics"),
                    "ExecuteCommandBuffer", "UnityEngine.Rendering.CommandBuffer");
                const string builtinType = "UnityEngine.Rendering.BuiltinRenderTextureType";
                IntPtr ctor = FindMethodBySignature(_contactIdentifierClass, ".ctor", builtinType);
                IntPtr identifier = _api.ObjectNew(_contactIdentifierClass);
                IntPtr none = BoxContactAction(coreImage, "BuiltinRenderTextureType", 0);
                _ = InvokeWithObjectArgument(ctor, _api.ObjectUnbox(identifier), none);
                _contactNoDepth = RootContactValue(identifier);
            }
            DrawMissingEye(1, _stereoLeftCamera, _stereoNaturalLeftRenderTexture);
            DrawMissingEye(2, _stereoRightCamera, _stereoNaturalRightRenderTexture);
#if !GAKUMAS_PUBLIC
            if ((rangeRegions & ~_contactRangeOverlayRegions) != 0)
            {
                _contactRangeOverlayRegions |= rangeRegions;
                string regions = string.Join(",", frame.TargetMarkers.Where(marker => marker.IsRange)
                    .Select(marker => marker.RangeRegion.ToString().ToLowerInvariant()).Distinct());
                LogContactPilot(DateTimeOffset.UtcNow, "contact-range-overlay-submitted",
                    $"ranges={rangeCount};regions={regions};eyes=left,right;depth=none");
            }
#endif
        }
        catch (Exception error)
        {
            _contactCheckMarkerAssets = true;
            _contactFinalOverlayFailed = true;
            LogContactPilot(DateTimeOffset.UtcNow, "contact-pilot-final-overlay-unavailable", error.Message.Split('\n')[0]);
        }

        void DrawMissingEye(int bit, IntPtr camera, IntPtr color)
        {
            bool rangesOnly = (_contactComposedEyes & bit) != 0;
            if (rangesOnly && rangeCount == 0) return;
            if (camera == IntPtr.Zero || color == IntPtr.Zero || Invoke(_contactCameraTarget, camera) != color)
                throw new InvalidOperationException("The completed eye no longer owns its color target.");
            ComposeContactHands(frame, IntPtr.Zero, camera, color, _contactNoDepth, "none", completedEye: true,
                rangesOnly: rangesOnly);
        }
    }

    private unsafe void ComposeContactHands(ContactHandFrame frame, IntPtr context, IntPtr camera, IntPtr color,
        IntPtr depthIdentifier, string depthSource, bool completedEye = false, bool rangesOnly = false)
    {
        if (!completedEye && context == IntPtr.Zero)
            throw new InvalidOperationException("The renderer has no scriptable context.");
        _ = Invoke(_contactCommandClear, _contactCommand);
        string stage = "set-target";
        try
        {
            _ = InvokeWithObjectArguments(_contactCommandTarget, _contactCommand,
                CreateContactTarget(color), _contactLoad, _contactStore, depthIdentifier, _contactLoad, _contactStore);
            stage = "set-view";
            _ = InvokeWithObjectArgument(_contactCommandViewport, _contactCommand, Invoke(_contactCameraViewport, camera));
            IntPtr view = Invoke(_contactCameraView, camera);
            IntPtr projection = InvokeWithObjectArguments(_contactGpuProjection, IntPtr.Zero,
                Invoke(_contactCameraProjection, camera), _contactTrue);
            _ = InvokeWithObjectArguments(_contactSetMatrices, IntPtr.Zero, _contactCommand, view, projection, _contactTrue);
            stage = "draw-markers";
            if (!rangesOnly)
            {
                IntPtr size = BoxContactVector(new Vector3(frame.Radius));
                RecordContactMarker(frame.Left, size, _contactMaterial, _contactMesh);
                RecordContactMarker(frame.Right, size, _contactMaterial, _contactMesh);
            }
#if !GAKUMAS_PUBLIC
            foreach (ContactTrialMarker marker in frame.TargetMarkers)
                if (marker.IsRange ? completedEye : !rangesOnly)
                    RecordContactMarker(marker.Center, BoxContactVector(new Vector3(marker.Radius)), marker.Material, marker.Mesh);
#endif
            stage = "submit";
            if (completedEye)
                _ = InvokeWithObjectArgument(_contactGraphicsExecute, IntPtr.Zero, _contactCommand);
            else
            {
                IntPtr contextValue = _contactContextPassedByAddress ? context : (IntPtr)(&context);
                _ = InvokeWithObjectArgument(_contactContextExecute, contextValue, _contactCommand);
            }
        }
        catch (Exception error)
        {
            _contactCheckMarkerAssets = true;
            throw new InvalidOperationException($"{stage}: {error.Message.Split('\n')[0]}", error);
        }
        finally
        {
            _ = Invoke(_contactCommandClear, _contactCommand);
        }
        _contactComposedEyes |= camera == _stereoLeftCamera ? 1 : 2;
        if (_contactComposedEyes == 3 && !_contactHandsLogged)
        {
            _contactHandsLogged = true;
            LogContactPilot(DateTimeOffset.UtcNow, "contact-pilot-hands-submitted",
                $"left={frame.Left.HasValue};right={frame.Right.HasValue};handScale={frame.Scale:R};displayRadius={frame.Radius:R};nearClip={frame.NearClip:R};shader={_contactShaderName};cameraTargets=stereo-eyes;phase={(completedEye ? "completed-eye-overlay" : "post-vfx")};depth={depthSource}");
        }
    }

    private void RecordContactMarker(Vector3? center, IntPtr size, IntPtr material, IntPtr mesh)
    {
        if (center is not Vector3 position) return;
        IntPtr matrix = InvokeWithObjectArguments(_contactTrs, IntPtr.Zero, BoxContactVector(position), _contactOrientation, size);
        _ = InvokeWithObjectArguments(_contactCommandDraw, _contactCommand, mesh, matrix, material,
            _contactZero, _contactZero, IntPtr.Zero);
    }

    private IntPtr CreateContactTarget(IntPtr texture)
    {
        IntPtr target = _api.ObjectNew(_contactIdentifierClass);
        _ = InvokeWithObjectArgument(_contactIdentifierCtor, _api.ObjectUnbox(target), texture);
        return target;
    }

    private unsafe IntPtr BoxContactAction(IntPtr image, string type, int value) =>
        _api.ValueBox(FindClass(image, "UnityEngine.Rendering", type), (IntPtr)(&value));

    private int ReadContactInt(IntPtr method, IntPtr instance) =>
        Marshal.ReadInt32(_api.ObjectUnbox(Invoke(method, instance)));

    private unsafe int ReadContactFieldInt(IntPtr instance, IntPtr field)
    {
        int result = 0;
        _api.FieldGetValue(instance, field, (IntPtr)(&result));
        return result;
    }

    private unsafe IntPtr ReadContactFieldPointer(IntPtr instance, IntPtr field)
    {
        IntPtr result = IntPtr.Zero;
        _api.FieldGetValue(instance, field, (IntPtr)(&result));
        return result;
    }

    private unsafe bool ReadContactFieldBoolean(IntPtr instance, IntPtr field)
    {
        byte result = 0;
        _api.FieldGetValue(instance, field, (IntPtr)(&result));
        return result != 0;
    }

    private unsafe void RetainContactPassDepth(IntPtr renderer, Span<ContactDepthStore> changed, ref int changedCount)
    {
        IntPtr queue = Invoke(_contactPassQueue, renderer);
        if (queue == IntPtr.Zero) throw new InvalidOperationException("The eye render pass queue is unavailable.");
        if (_contactPassCount == IntPtr.Zero)
        {
            IntPtr queueClass = Marshal.ReadIntPtr(queue);
            _contactPassItem = FindMethodBySignature(queueClass, "get_Item", "System.Int32");
            _contactPassCount = FindMethod(queueClass, "get_Count");
        }
        int count = ReadContactInt(_contactPassCount, queue);
        if (count < 1 || count > changed.Length)
            throw new InvalidOperationException($"Unexpected eye render pass count: {count}.");
        IntPtr mscorlib = FindImage("mscorlib.dll");
        // VL queues its own deferred and forward passes. Retain depth where any scheduled pass would discard it.
        for (int i = 0; i < count; i++)
        {
            IntPtr pass = InvokeWithObjectArgument(_contactPassItem, queue, BoxInt32(mscorlib, i));
            if (pass == IntPtr.Zero) continue;
            int action = 0;
            _api.FieldGetValue(pass, _contactStoreActionField, (IntPtr)(&action));
            if (action != 3) continue; // DontCare
            byte overridden = 0;
            _api.FieldGetValue(pass, _contactStoreOverrideField, (IntPtr)(&overridden));
            changed[changedCount++] = new ContactDepthStore(pass, action, overridden);
            WriteContactDepthStore(pass, 0, 1);
        }
    }

    private unsafe void WriteContactDepthStore(IntPtr pass, int action, byte overridden)
    {
        _api.FieldSetValue(pass, _contactStoreActionField, (IntPtr)(&action));
        _api.FieldSetValue(pass, _contactStoreOverrideField, (IntPtr)(&overridden));
    }

    private void DisableContactComposition(string reason)
    {
        if (_contactCompositionFailed) return;
        _contactCompositionFailed = true;
        LogContactPilot(DateTimeOffset.UtcNow, "contact-pilot-composition-unavailable", reason);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ContactRendererExecute(IntPtr renderer, IntPtr context, IntPtr renderingData, IntPtr method);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ContactRendererFinish(IntPtr renderer, IntPtr context, byte resolveFinalTarget,
        IntPtr renderingData, IntPtr method);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int ContactClassValueSize(IntPtr klass, out uint alignment);
}
