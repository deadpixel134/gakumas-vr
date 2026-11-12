using GakumasVR.Core;

namespace Doorstop;

internal sealed partial class OpenXrControllerActions
{
    internal static int ContactInputMask(VrSettings settings, bool panelEnabled, float? panelGrip,
        bool toggleHeld, float? trigger, bool? primary, bool? secondary, float? worldGrip)
    {
        int allowed = 3;
        if (panelEnabled || panelGrip is null || panelGrip > GripReleaseThreshold || toggleHeld)
            allowed &= settings.Panel.PanelHand == VrHand.Left ? ~1 : ~2;
        if (panelEnabled || trigger is null || primary is null || secondary is null ||
            trigger > 0.15f || primary == true || secondary == true)
            allowed &= settings.Panel.PointerHand == VrHand.Left ? ~1 : ~2;
        if (settings.Tracking.WorldGripEnabled && settings.Panel.PanelHand != VrHand.Right &&
            (worldGrip is null || worldGrip > GripReleaseThreshold)) allowed &= ~2;
        return allowed;
    }
}
