[한국어](../ko/INSTALLATION.md) | [English](INSTALLATION.md) | [日本語](../ja/INSTALLATION.md)

# Installation

[Project home](../../README.en.md) · [Usage](USAGE.md) · [Architecture](ARCHITECTURE.md)

## Requirements

- Windows 11 x64
- A working installation of the DMM edition of Gakuen Idolmaster
- A PC VR setup with an OpenXR runtime
- The Dobby runtime dependency included in the v0.163.0 release ZIP

> When Localify is present, the installer preserves its translations, fonts, textures, settings, and any existing `BepInEx/core/dobby.dll`. Without Localify, it installs only the required Dobby dependency and does not create Localify files. This clean installation path passes automated install/uninstall validation but has not yet been VR hardware-tested.

## Install

1. Download the newest prerelease ZIP from the [GitHub Releases page](https://github.com/deadpixel134/gakumas-vr/releases).
2. Fully close the game and make sure `gakumas.exe` is no longer running.
3. Extract the ZIP to a temporary directory outside the game folder.
4. Run `GakumasVR.Installer.exe`.
5. Select the game directory containing `gakumas.exe`, `GameAssembly.dll`, and `UnityPlayer.dll`.
6. Select **Install** and wait for completion. Existing target files are backed up under `game directory\vrmod\rollback\`.
7. Select **Open settings**, or run `game directory\vrmod\tools\GakumasVR.Configurator.exe`.
8. Select your PC VR software as the active OpenXR runtime, then start the game.

Save settings only while the game is fully closed. Changes take effect on the next launch.

## OpenXR runtimes

### Virtual Desktop — tested

1. Connect the Quest to the PC with Virtual Desktop.
2. Select VDXR/Virtual Desktop OpenXR as the active runtime in Virtual Desktop Streamer.
3. From the desktop visible inside the headset, open the DMM launcher and start the game.

Use the DMM launcher on the desktop, not Virtual Desktop's **Games** tab. SteamVR does not need to be running for this setup.

### SteamVR — preliminary

Set SteamVR as the active OpenXR runtime, then start the game from the DMM launcher. The Windows D3D11 OpenXR path is expected to be compatible, but this project has not hardware-tested it yet.

### Meta Quest Link/Air Link — preliminary

Connect with Quest Link or Air Link, set the Meta runtime as active OpenXR in the Meta Quest Link app, and start the game. This path has not yet been hardware-tested by the project.

## Update

Close the game and run the installer from the new Release against the same game directory. `vrmod/config/settings.json` is preserved and replaced files are backed up for rollback.

## Uninstall or roll back

1. Fully close the game.
2. Run `GakumasVR.Installer.exe` from the release directory you used.
3. Select the game directory, then choose **Uninstall** or the available **Rollback** action.

The installer only manages files recorded in its manifest. A file modified after installation has a different hash, so it is preserved with a warning instead of being deleted. Pre-existing files are restored from backup. `vrmod/config/settings.json` and Localify's `version.dll` and `gakumas-local/` are preserved.

## Troubleshooting

- If VR does not start, check the active OpenXR runtime and the presence of `BepInEx/core/dobby.dll`.
- If settings do not apply, confirm that the game was closed when you saved them.
- If the display works but controls do not, bring the game window to the foreground.
- On failure, the game window should continue running and VR should fall back to a flat panel.
- Before attaching files from `vrmod/logs/` to an issue, inspect them for account IDs, viewer IDs, tokens, or launch authentication data.
