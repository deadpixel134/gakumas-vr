[한국어](../ko/VR_INTERACTION_SPEC.md) | [English](VR_INTERACTION_SPEC.md) | [日本語](../ja/VR_INTERACTION_SPEC.md)

# Reusable VR Interaction and Pose-Composition Specification

[Project home](../../README.en.md) · [Usage](USAGE.md) · [Architecture](ARCHITECTURE.md)

This document defines the portable interaction contract validated by users in Gakumas VR v0.173.0. Game-specific camera discovery may change, but preserving these invariants should reproduce the same controls, comfort, and failure behavior in another Unity VR mod.

## User-visible contract

- Default roles: left stick turns the view; right stick moves. Selecting the movement hand in settings swaps both roles.
- Movement follows the full final 3D view direction. Looking up while moving forward ascends; looking down descends.
- Stick turning changes world-space yaw or pitch only and never creates roll.
- Default turning is a 15° snap. Available steps are 15°/30°/45°/60°, with optional smooth turning.
- Only physical HMD roll introduced after the VR origin is retained. Entry tilt, scene-camera roll, and roll derived from stick turning are removed.
- Thumbstick scrolling is disabled in VR.
- Without fresh stereo, the final game backbuffer appears on a front panel. In 3D, the same backbuffer is available on a hand panel and operated by the other hand's ray and buttons.
- A VR failure must leave the desktop game running and fall back to a panel or disabled VR.

## Coordinates and origin

OpenXR poses are converted to Unity coordinates as follows:

```text
positionUnity = ( x,  y, -z)
rotationUnity = (-x, -y,  z, w)
```

At stereo-generation entry, capture the midpoint of both eyes and their normalized average orientation. Relative positions use `inverse(origin) × (current - origin)`. Reset the pose mapper, locomotion offset, artificial turn state, and input latches together when retiring a generation.

Independent live 6DoF captures the game camera's entry world pose once; later authored camera paths do not drag the VR viewpoint. Non-live 3D uses the currently validated source camera as its base.

## Movement

Apply deadzone-remapped `(strafe, 0, forward)` through the final view quaternion. Do not flatten the vector onto XZ. Clamp integration `dt` to 0.1 seconds. Defaults are deadzone 0.20 and 1.5m/s. Physical head translation, eye offsets, and accumulated locomotion must share the same world navigation basis.

## Stick-turn state machine

Select one dominant cardinal axis per sample: `abs(x) >= abs(y)` means yaw only; otherwise pitch only. This suppresses unintended diagonal rotation from physical stick skew.

Snap mode activates at 0.65, applies exactly one configured step, and remains disarmed until the stick returns inside the 0.20 deadzone. Smooth mode integrates the selected axis at the configured degrees per second with `dt <= 0.1`. Clamp artificial pitch near ±89.1°.

Store artificial yaw and pitch as scalars and rebuild a quaternion every frame. Do not repeatedly multiply incremental quaternions into the previous final orientation; that permits axis drift and roll leakage.

## Roll isolation and final rotation

Convert each absolute HMD eye orientation into Unity space, derive axes, and decompose it as:

```text
forward = rotation × (0,0,1)
right   = rotation × (1,0,0)
up      = rotation × (0,1,0)

yaw   = atan2(forward.x, forward.z)
pitch = atan2(-forward.y, length(forward.xz))
roll  = atan2(right.y, up.y)
```

Store origin yaw/pitch/roll separately; subtract them from current values and wrap yaw/roll deltas to `[-π, π]`. Derive the scene base from its forward vector, discarding scene roll. Rebuild the final pose:

```text
finalYaw   = baseYaw   + artificialYaw   + physicalYawDelta
finalPitch = clamp(basePitch + artificialPitch + physicalPitchDelta)
finalRoll  = physicalRollDelta

finalRotation = Yaw(finalYaw) × Pitch(finalPitch) × Roll(finalRoll)
```

The stick therefore cannot modify roll. A tilted entry pose is canceled at the origin, yawing the head while maintaining the same tilt creates no new roll, and an actual post-entry head tilt remains visible.

Do not apply the raw relative quaternion as `artificial × inverse(origin) × current`. At a tilted origin, physical yaw can be represented partly as relative roll and tilt the horizon after stick turns.

## Panels and input

- No fresh stereo: a view-space front quad is the primary content.
- Fresh stereo: the projection world is primary and the hand panel is auxiliary.
- Center the hand panel at the controller tip, upright in view space and viewer-facing by default. Hide it outside tracking/FOV gates and skip copy, acquire, submit, and hit-testing work.
- Convert ray-plane intersection to UV, remove letterbox regions, then map to game client coordinates.
- Default A/Trigger performs click/drag and B goes back. Latch the early trigger aim point to reduce pull-induced pointer movement.
- Inject Windows input only while the game is foreground, and always release latched buttons on transitions.

## Rendering, lifetime, and fallback

- Left/right clone cameras render in Unity's normal render loop; publish only complete stereo pairs.
- Separate the game render rate from OpenXR submission. The latest complete pair may be resubmitted at the HMD cadence.
- Treat cameras, eye textures, render requests, and GPU queries as scene-bound generation resources. Never reuse stale Unity wrappers after scene retirement.
- Keep whole-object enumeration out of the 1–10ms transition fast path; use low-frequency, change-driven diagnostics.
- On source, clone, or OpenXR failure, stop projection submission and fall back to the final backbuffer panel. Never patch original game DLLs or assets.

## Porting boundaries

Reusable layers are OpenXR session/actions/swapchains, pose decomposition, movement and turn integrators, panels and pointer mapping, generation lifetime, safe fallback, settings validation, manifest installation, and rollback.

Game adapters must identify the real world camera, clone the active render pipeline, capture the composed backbuffer, follow orientation/scene transitions, and apply game-specific VFX overrides. Do not approve 3D from scene or camera names alone; verify the active render target and the surface actually presenting it.

## Required regression tests

- Repeated snap yaw on a tilted scene camera keeps the world-right Y component near zero.
- Physical yaw at a tilted HMD origin produces zero roll delta when tilt is unchanged.
- An actual additional 15° head tilt produces approximately 15° final roll.
- Held snap input fires once and rearms only after centering.
- Diagonal stick noise changes only the dominant axis.
- Forward movement while looking up/down ascends/descends.
- Swapping movement hand swaps both input sources.
- Leaving 3D restores the front panel without retaining an old 3D frame.
- Install, update, and uninstall preserve user settings and unrelated mod files.

Automated tests validate math and file-safety contracts; HMD runtime behavior, game-camera integration, and comfort still require separate user VR testing.
