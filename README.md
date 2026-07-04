# Horror Project

**Horror Project** is an early-stage first-person horror game prototype built in Unity. The project focuses on slow tension, limited visibility, player vulnerability, and atmospheric exploration rather than constant action.

The current foundation includes a first-person controller, camera separation, flashlight interaction, stamina behavior, localization groundwork, and initial rendering experiments for a dark, foggy horror mood.

## Project Status

This project is still in active development. Core systems are being built and tested before final gameplay, level design, story structure, and art direction are locked.

Current focus areas:

- First-person horror movement and camera feel
- Flashlight-based visibility and tension
- Stamina pressure during exploration and pursuit
- Localization-ready UI and text systems
- Dark environment setup using Unity rendering tools
- Early fog, lighting, and post-processing experiments

## Game Direction

The intended experience is a horror FPS where the player feels exposed, under-equipped, and uncertain. The flashlight is an important survival tool, but it should also create tension by limiting what the player can clearly see.

Planned design pillars:

- **Atmosphere first**: darkness, fog, silence, and distant sounds should carry much of the fear.
- **Limited control**: stamina, visibility, and movement restrictions should make decisions matter.
- **Exploration tension**: the player should be encouraged to move forward while feeling unsafe.
- **Readable systems**: controls and feedback should be clear without breaking immersion.
- **Expandable structure**: systems should be modular enough for future story, AI, inventory, and puzzle features.

## Implemented Systems

The project currently contains these early systems:

- First-person player controller using Unity's new Input System
- Separate player movement and camera control scripts
- Camera pivot parenting for FPS view handling
- Double-tap backward quick turn support
- CharacterController setup with centered controller values
- Head bobbing support
- Flashlight toggle and scroll-based beam adjustment
- Flashlight HUD status feedback
- Stamina system with sprint drain, exhaustion, delayed recovery, and low-stamina feedback
- Centered stamina bar UI behavior
- Basic localization scripts and localization table asset
- Initial URP migration work
- Experimental local fog shader/material

## Controls

Default controls are expected to follow the current Input System setup:

| Action | Input |
| --- | --- |
| Move | WASD / Left Stick |
| Look | Mouse / Right Stick |
| Sprint | Sprint action binding |
| Jump | Jump action binding |
| Crouch | Crouch action binding |
| Flashlight | F |
| Adjust flashlight beam | Mouse Scroll |
| Quick turn | Double-tap S or Down Arrow |

Input bindings may change as the project evolves.

## Unity Version and Pipeline

This project is being developed with Unity 6 and the Universal Render Pipeline package has been added.

Rendering notes:

- URP is the intended render pipeline going forward.
- Some rendering work is still experimental.
- The local fog shader is a custom shader for a separate fog mesh/object, not a Terrain material.
- Depth texture support is required for depth-aware fog behavior.

If materials appear pink or effects do not render, check:

- `Project Settings > Graphics`
- `Project Settings > Quality`
- The assigned URP Asset
- URP Renderer settings
- Console shader/package errors

## Folder Overview

Important project folders:

| Path | Purpose |
| --- | --- |
| `Assets/Scripts/Player` | First-person player, camera, stamina, head bob, and flashlight logic |
| `Assets/Scripts/UI` | HUD and UI behavior scripts |
| `Assets/Scripts/Localization` | Localization system scripts |
| `Assets/Scripts/Rendering` | Rendering helper scripts |
| `Assets/Shaders` | Custom shader experiments |
| `Assets/Materials` | Project materials |
| `Assets/Resources` | Runtime-loaded resources and imported assets |
| `Assets/Scenes` | Unity scenes |

## Development Notes

This repository is a game prototype, so some values are intentionally left adjustable in the Unity Inspector. Movement speed, camera height, flashlight intensity, stamina behavior, and fog density should be tuned through playtesting instead of being treated as final constants.

When adding new gameplay systems, prefer keeping responsibilities separated:

- Player movement should stay separate from camera look.
- Camera behavior should stay separate from stamina and flashlight logic.
- UI feedback should be driven by gameplay state, not hardcoded into controller scripts.
- Visual tuning should be adjustable from assets or Inspector fields where practical.

## Planned Features

Potential future systems:

- Enemy AI and stealth/pursuit behavior
- Interaction system for doors, notes, keys, and objects
- Inventory or limited-use item system
- Audio-driven horror events
- Puzzle and objective flow
- Save/checkpoint system
- More complete localization content
- Polished URP lighting, fog, and post-processing stack

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

## Author

Created by **Ikbal Gumilar**.
