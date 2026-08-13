namespace GakumasVR.Core;

public readonly struct VrThumbstickRouting : IEquatable<VrThumbstickRouting>
{
    public VrThumbstickRouting(
        bool panelHandScroll,
        bool offHandScroll,
        bool offHandLocomotion) =>
        (PanelHandScroll, OffHandScroll, OffHandLocomotion) =
        (panelHandScroll, offHandScroll, offHandLocomotion);

    public bool PanelHandScroll { get; }

    public bool OffHandScroll { get; }

    public bool OffHandLocomotion { get; }

    public bool Equals(VrThumbstickRouting other) =>
        PanelHandScroll == other.PanelHandScroll &&
        OffHandScroll == other.OffHandScroll &&
        OffHandLocomotion == other.OffHandLocomotion;

    public override bool Equals(object? obj) =>
        obj is VrThumbstickRouting other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(PanelHandScroll, OffHandScroll, OffHandLocomotion);
}

public static class VrThumbstickRouter
{
    public static VrThumbstickRouting Route(
        LocomotionInputMode mode,
        bool panelEnabled) => mode switch
    {
        LocomotionInputMode.SplitHands => new VrThumbstickRouting(
            panelHandScroll: true,
            offHandScroll: false,
            offHandLocomotion: true),
        LocomotionInputMode.ContextualOffHand => new VrThumbstickRouting(
            panelHandScroll: false,
            offHandScroll: panelEnabled,
            offHandLocomotion: !panelEnabled),
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };
}
