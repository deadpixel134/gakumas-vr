[한국어](../ko/USAGE.md) | [English](USAGE.md) | [日本語](../ja/USAGE.md)

# Usage and controls

[Project home](../../README.en.md) · [Installation](INSTALLATION.md) · [Architecture](ARCHITECTURE.md)

## Display modes

- **3D environments:** the game world is shown in stereo VR. The left-hand auxiliary panel starts off and can be toggled with the left Grip.
- **Fully flat screens:** the game appears automatically on a front-facing panel in a black VR environment. Grip cannot hide this primary panel.
- **Transitions:** leaving 3D restores the front panel automatically; entering 3D returns to the stereo world.
- **Hand-panel visibility:** an enabled panel is visible only while the panel hand is inside the HMD view. Moving it out of view also stops the panel copy and submission work.

## Default controls

| Action | Default input |
|---|---|
| Aim cursor | Right controller aim ray |
| Click or hold-drag | A button or right Trigger |
| Back | B button (`Esc`) |
| Scroll | Right Thumbstick up/down |
| Toggle 3D auxiliary panel | Left Grip |

Use A when pulling the trigger would disturb fine aim. Trigger input latches the early press position to reduce drift and becomes a drag only after being held and moved far enough. Direct touch is not supported.

By default, input is injected only while the game window is in the Windows foreground. Mouse and normal game input remain available.

## Button names

- **Primary Face:** A on the right hand, X on the left
- **Secondary Face:** B on the right hand, Y on the left

If you swap the panel and pointer hands in settings, these terms follow the selected hand.

## Configurator

After installation, run `game directory\vrmod\tools\GakumasVR.Configurator.exe`. Korean, English, and Japanese can be selected at the top.

- **Rendering:** per-eye render scale from 0.50 to 2.00 and world eye-offset scale
- **VFX:** approved default, all on, all off, or manual effects
- **Panel:** panel/pointer hands, initial state, placement, size, rotation, viewer-facing behavior, and toggle binding
- **Input:** click/back buttons, trigger click, scrolling and sensitivity, and game-focus requirement

Save only while the game is closed. Invalid values are replaced with safe defaults and the reason is logged.

## Render scale and frame rate

Per-eye render scale affects immersive stereo only, not the source resolution of the front or hand panel. Values above 1.00 are supersampling and produce a warning in the configurator. Because both dimensions scale, 2.00 renders about four times as many pixels as 1.00.

One measured session produced about 59.14 unique stereo pairs per second while OpenXR submission averaged 114.79fps with a 117.60fps median. The latter includes resubmitting game frames on the HMD schedule; it does not mean the game renders 120 distinct scenes per second.

## Known limitations

- SteamVR and Meta Quest Link have not yet been hardware-tested.
- Direct touch is unsupported.
- Render scales above 1.00 are available but still lack hardware performance and stability validation.
- A game update that changes Unity or rendering internals may make some 3D contexts fall back to the flat panel.
