[한국어](README.md) | [English](README.en.md) | [日本語](README.ja.md)

# Gakumas VR

Gakumas VR is an unofficial Meta Quest/OpenXR VR mod for the DMM version of Gakuen Idolmaster. Supported 3D scenes are presented in stereo VR, while other content appears on aspect-correct flat panels that can be controlled with VR controllers.

The current public build is the **v0.175.6 stable release**. The core 6DoF controls were hardware-validated through v0.173 on Meta Quest 2 with Virtual Desktop OpenXR; v0.175.6 retains that behavior while updating the spatial/scale profiles, default GUI flow, and package-validation baseline for the current public release. SteamVR OpenXR and Meta Quest Link are preliminary targets and have not yet been hardware-tested by this project.

## Documentation

- [Installation](docs/en/INSTALLATION.md)
- [Usage and controls](docs/en/USAGE.md)
- [Architecture](docs/en/ARCHITECTURE.md)
- [Reusable VR interaction and pose-composition specification](docs/en/VR_INTERACTION_SPEC.md)
- [Developer guide](vrmod/README.md) (Korean)
- [Current status](docs/GAKUMAS_VR_STATUS.md) · [Design record](docs/GAKUMAS_VR_DESIGN.md) · [Milestones](docs/VR_MILESTONES.md) · [Changelog](vrmod/CHANGELOG.md) (Korean)

## Highlights

- OpenXR stereo output in supported 3D environments, including Live, Home, and Communications
- Automatic front panel for 2D screens and an optional left-hand panel in 3D
- Right-hand 3D movement, left-hand world-axis turning, and 30° default snap turn
- Right-hand ray pointer, A/trigger click, and B back
- Korean, English, and Japanese installer and configurator
- Render scale, stereo depth, panel placement, hand roles, buttons, and VFX controls
- Install and uninstall behavior designed to preserve existing Localify files and settings

## Important limitations

- This stable release targets Windows 11 x64, the DMM edition, and Unity 6000.0.77f1.
- The release ZIP includes the required Dobby binary. Before touching the game directory, the v0.175.6 installer preflights every payload hash, required clean-install component, and preservation policy.
- If something fails, close the game and use the installer to uninstall or roll back. Before attaching logs to an issue, check them for account identifiers or authentication data.

This repository does not contain game files, Localify assets, user settings, logs, rollback data, or build outputs. Project source is released under the [MIT License](LICENSE); third-party components retain their own licenses. See [Credits and third-party licenses](CREDITS.md).

> Gakumas VR is an unofficial fan project and is not affiliated with the game's developer or publisher. The game, trademarks, and related works belong to their respective owners. A legitimate game installation is required.
