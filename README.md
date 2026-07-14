# Horror Project

> A first-person Indonesian folk-horror game about an expedition, a disputed colonial archive, and a fire in Leuweng Sancang that may remember more than history recorded.

**Horror Project** is a work-in-progress narrative horror game built with Unity 6. It combines slow exploration, environmental investigation, survival pressure, localized dialogue, and a hostile supernatural presence inspired by stories of **Banaspati**.

The project currently contains a playable foundation spanning a city departure, a remote roadside stop, a village investigation, and an early forest gameplay prototype. It is under active development and is not yet a complete release.

## Synopsis

A field researcher and two expedition crew members discover an old colonial document describing the destruction of a military expedition near Sancang. The surviving witnesses never agreed on what attacked them. Their report gave it a foreign name: **Ashes That Remember**.

Local stories offer another name: **Banaspati**.

The archive should have been easy to dismiss as fear, folklore, or a misunderstood weapon. However, later research found traces of a major fire event dating back more than six hundred years, long before the colonial expedition entered the region. The evidence proves that something burned in Sancang, but it does not prove what caused it.

The team travels south from the city toward a village near **Leuweng Sancang**. Along the way, conflicting accounts raise more questions: Was the archive accurate? Did frightened soldiers turn separate events into a monster? Have local residents ever explored the deepest parts of the forest? And why do stories separated by generations describe fire as if it could choose what to burn?

Before entering the forest, the player must gather supplies, speak with residents, compare oral history with written evidence, and decide which details deserve to be trusted.

This synopsis intentionally avoids major discoveries, puzzle solutions, and endings.

## Experience

The game is designed around vulnerability rather than combat power.

- **Slow-burn tension** through darkness, distance, silence, and uncertain information.
- **Investigation through conversation** with residents who have different occupations, knowledge, and versions of local events.
- **Conflicting evidence** presented through archives, research notes, folklore, and environmental clues.
- **Limited visibility** built around a directional flashlight with adjustable focus.
- **Physical pressure** through stamina, exhaustion, passive heat, burn damage, and pursuit.
- **Contextual horror** where the enemy behaves like a sentient floating fire or plasma presence rather than a humanoid creature.
- **Localized storytelling** with English and Indonesian text available throughout menus, subtitles, dialogue, and scene conversations.

## Current Narrative Flow

The implemented prototype currently follows this sequence:

1. **City departure** - The main menu is presented over the opening vehicle journey. A difficulty can be selected before the expedition conversation begins.
2. **Roadside stop** - The team stops at a remote fuel station and Kopdes to check supplies and ask for information.
3. **Return to the road** - The player re-enters the vehicle, reviews the archive with the crew, and continues toward the village.
4. **Village arrival** - A localized loading transition leads into a longer vehicle journey through the countryside.
5. **Village investigation** - The player leaves the vehicle, approaches residents, and begins gathering local testimony.
6. **Forest prototype** - The main gameplay scene contains the current movement, flashlight, stamina, damage, ghost, chase, and death-system experiments.

The order and pacing remain subject to change as level production continues.

## Implemented Features

### Player And Camera

- First-person movement built exclusively on Unity's **new Input System**.
- Separate movement and camera controller responsibilities.
- Custom FPS camera pivot and parenting without requiring Cinemachine.
- Independent yaw and pitch sensitivity.
- Optional reversed mouse pitch.
- Keyboard, mouse, and gamepad input support.
- Configurable hold-to-run or toggle-to-run behavior.
- Smooth crouching and adjustable camera height.
- Head bob with configurable intensity.
- Double-tap backward **180-degree quick turn**.
- Camera pitch restoration when entering a vehicle, preventing the view from remaining locked downward.

### Flashlight

- Toggleable spot flashlight.
- Mouse-wheel beam focusing.
- Narrow beams become brighter while wide beams become dimmer.
- Flashlight follows the camera's vertical pitch.
- Separate on/off HUD icons.
- HUD feedback appears briefly and fades smoothly.

### Stamina And Movement Pressure

- Sprint drain and delayed recovery.
- Centered stamina bar that contracts from both sides.
- Stamina HUD fades when the player is safely recovered.
- Low-stamina alpha blinking below the configured threshold.
- Exhaustion prevents sprinting and temporarily blocks regeneration.
- Exhausted movement speed penalty.
- Normal movement returns only after the recovery threshold is reached.

### Health, Heat, And Damage

- Hidden 100-point player health foundation.
- Slow passive health regeneration.
- Passive heat damage based on distance from the hostile entity.
- Stronger burning attack with increased damage and effective range.
- Fire-based damage-over-time feedback.
- Camera-edge hit feedback.
- Particle intensity driven by accumulated burn state.
- Player and enemy audio hooks for footsteps, breathing, damage, burning, pursuit, and death.

### Enemy Behaviour

- A floating, non-humanoid fire/plasma enemy concept.
- Three-dimensional random patrol movement, including vertical travel.
- Minimum ground clearance to reduce terrain penetration.
- Spherical target detection combined with line-of-sight obstruction checks.
- Target memory after detection, allowing persistent pursuit.
- Distance and stuck conditions for abandoning a chase.
- Obstacle probes and alternate movement directions.
- Passive heat remains dangerous through nearby barriers, while initial target awareness uses obstruction-aware visibility checks.
- Enemy returns to patrol after the player dies.

### Death And Feedback

- Context-sensitive death handling based on the final damage situation.
- Separate sequences for passive burning, direct burning attacks, and prolonged pursuit.
- Camera detachment and look-at staging where required.
- Fire color, scale, and simulation-speed transitions.
- Localized scream subtitles with dynamic subtitle sizing.
- Fading death panel and delayed blinking input prompt.
- Keyboard, mouse, and gamepad input support on the death screen.

### Dialogue And Localization

- English and Indonesian localization table.
- Localized TextMeshPro labels and dropdown options.
- Automatic scene conversations with speaker names and configurable timing.
- `Esc` support for skipping cinematic or travel conversations.
- Manual NPC dialogue progression using any button.
- Separate first-meeting and repeat-dialogue sequences.
- Optional profanity filtering for multiple languages.
- Situation-based player subtitle barks for first sighting, pursuit, damage, low stamina, and death.
- Subtitle accessibility settings including enable/disable, five size levels, background visibility, and speaker-name visibility.

### NPC And World Interaction

- New Input System interaction action.
- Separate prompts for NPC interaction and world-object interaction.
- Proximity-based prompt visibility with alpha animation.
- Player movement locks during conversation preparation and dialogue.
- Player smoothly approaches and faces the NPC before dialogue begins.
- Camera look aligns with the active conversation target.
- ScriptableObject villager data containing localized name, occupation, description, audio, and dialogue sequences.
- Configurable wandering residents and stationary merchant behaviour.
- Branching cashier conversation with localized questions, answers, purchase confirmation, and vehicle-unlock handoff.

### Vehicles And Scene Transitions

- Procedural road-following vehicles for city and village travel.
- Lane offsets, look-ahead steering, smooth rotation, turn slowdown, acceleration, and stopping behaviour.
- Animated player entry and exit instead of instant teleportation.
- Connected city, gas-station, dirt-road, and main-road route handoffs.
- Automatic travel conversations synchronized with journey length.
- Route-progress triggers for arrival dialogue.
- Camera-rise transitions, configurable hold timing, black fade, loading text, and scene handoff.

### Menu And Settings

- Main-menu selection feedback for mouse, keyboard, and controller.
- Animated main-menu/settings transitions.
- Direction-aware settings-tab swipe animations.
- Shared UI/UX animation utility for reusable fades, slides, swaps, and transitions.
- Apply buttons activate only when settings have changed.
- Per-tab restoration to the values present when the menu opened.
- Settings are persisted through `PlayerPrefs` where implemented.

## Controls

The active gameplay controls use `Assets/InputSystem_Actions.inputactions` and the new Input System only.

| Action | Keyboard And Mouse | Gamepad |
| --- | --- | --- |
| Move | `WASD` or Arrow Keys | Left Stick |
| Look | Mouse | Right Stick |
| Sprint | Left Shift | Left Stick Press |
| Jump | Space | South Button |
| Crouch | `C` | East Button |
| Interact / Talk | `E` | North Button |
| Quick turn | Double-tap `S` or Down Arrow | Double-tap backward on Left Stick |
| Flashlight | `F` | Not assigned yet |
| Focus flashlight | Mouse Wheel | Not assigned yet |
| Skip scene conversation | `Esc` | Not assigned yet |
| Advance NPC dialogue | Any button | Any button |
| Navigate menus | `WASD`, Arrow Keys, Mouse | Left Stick, Right Stick, or D-pad |

Bindings and device coverage are still being refined.

## Settings Coverage

| Tab | Current Controls |
| --- | --- |
| **Video** | Fullscreen/windowed, detected aspect-aware resolutions from 360p through 4K where supported, quality level, brightness, V-Sync mode, FPS cap, Bloom, and Motion Blur |
| **Audio** | Master, music, SFX, and ambient volume with decibel readouts; mono/stereo output |
| **Control** | Yaw sensitivity, pitch sensitivity, reverse mouse, vibration, and hold/toggle run mode |
| **Gameplay** | Language, difficulty selection, crosshair, tutorial preference, autosave preference, subtitle background, camera shake preference, and head-bob intensity |
| **Accessibility** | Subtitles, subtitle size, subtitle background, speaker names, camera-shake level, blink intensity preference, and contrast |

Some settings depend on scene content or installed effects:

- Bloom, Motion Blur, brightness, and contrast require matching Post Processing Stack components in an active profile.
- Dynamic-range choices are currently UI groundwork and are intentionally disabled.
- G-Sync and FreeSync labels currently use the project's synchronized-frame path; hardware-specific adaptive-sync control is not implemented.
- Difficulty selection is stored but does not yet rebalance enemy statistics.
- Tutorial and autosave preferences exist, but the complete tutorial/save pipeline is not finished.
- Camera-shake and blink-intensity preferences are prepared for effects that are still being expanded.

## Audio Architecture

The project includes a persistent audio manager with separate master, music, SFX, and ambient categories. Scene audio profiles can provide looping music and environmental ambience, while player and ghost controllers expose slots for specific sound effects.

Many final clips are not included or assigned yet. The current implementation is the routing and playback foundation for future sound design.

## Rendering And Performance

The active project uses Unity's **Built-in Render Pipeline**. The Universal Render Pipeline was removed from active project settings to keep the prototype practical on lower-powered integrated graphics.

Current rendering and performance work includes:

- Post Processing Stack v2 integration for optional color grading, Bloom, and Motion Blur.
- Camera far-clip and occlusion-culling helpers.
- Texture streaming and mipmap budget controls.
- Terrain tree distance, billboard distance, detail distance, and density tuning.
- Road-area tree exclusion.
- Procedural road meshes and colliders.
- Built-in fog, lighting, and custom shader experiments.

Some imported vendor assets still contain optional URP support folders, but no Scriptable Render Pipeline asset is assigned in `GraphicsSettings`.

## Scenes

| Scene | Purpose |
| --- | --- |
| `Assets/Scenes/CityScene.unity` | Main menu, difficulty selection, opening expedition dialogue, road travel, gas-station stop, cashier interaction, and departure transition |
| `Assets/Scenes/VilageScene.unity` | Countryside journey, loading transition, vehicle arrival, village population, NPC dialogue, and investigation foundation |
| `Assets/Scenes/MainScene.unity` | Forest gameplay prototype containing the current player-survival and hostile-entity systems |

`VilageScene` retains its current filename spelling for compatibility with serialized scene transitions.

## Technology

- **Engine:** Unity `6000.4.0f1`
- **Language:** C#
- **Input:** Unity Input System `1.19.0`
- **Navigation package:** AI Navigation `2.0.13`
- **Rendering:** Built-in Render Pipeline
- **Post processing:** Post Processing Stack `3.5.1`
- **UI:** Unity UI and TextMeshPro
- **Localization:** Project-local English/Indonesian table and runtime manager
- **License:** MIT

## Getting Started

### Requirements

- Unity Hub
- Unity Editor `6000.4.0f1`
- Git
- A desktop platform supported by the current Unity project configuration

### Clone And Open

```bash
git clone https://github.com/IkbalGumilar/Horror-Project.git
cd Horror-Project
```

Open the folder through Unity Hub using Unity `6000.4.0f1`, then allow Unity to import packages and regenerate the project library.

For the narrative prototype, open:

```text
Assets/Scenes/CityScene.unity
```

For direct forest-system testing, open:

```text
Assets/Scenes/MainScene.unity
```

## Project Structure

| Path | Responsibility |
| --- | --- |
| `Assets/Scripts/Player` | Movement, camera, flashlight, stamina, health, damage, death, audio, and interaction foundations |
| `Assets/Scripts/AI` | Ghost patrol, detection, pursuit, obstacle avoidance, audio, and burn attacks |
| `Assets/Scripts/City` | City road generation, vehicle travel, gas-station flow, cashier conversation, and departure transition |
| `Assets/Scripts/Village` | Village road travel, loading, population, NPC data, interaction, wandering, and journey dialogue |
| `Assets/Scripts/UI` | Menu navigation, settings tabs, animation, subtitles, choice panels, and HUD behaviour |
| `Assets/Scripts/Localization` | Runtime language manager, tables, localized labels, and dropdown integration |
| `Assets/Scripts/Audio` | Persistent audio manager and per-scene audio profiles |
| `Assets/Scripts/Rendering` | Camera, terrain, culling, mipmap, and rendering helpers |
| `Assets/Localization` | Localized conversations, villager data assets, and the main language table |
| `Assets/Scenes` | City, village, and forest gameplay scenes |

The local `Note/` directory is intentionally excluded from Git because it contains private development notes and spoiler-sensitive story planning.

## Development Status

This repository is an active prototype. Systems may be functional while their art, audio, balancing, animation, or final content is incomplete.

Current priorities include:

- Replacing blockout environments and temporary vehicle geometry with final assets.
- Expanding village residents, occupations, dialogue, and investigation choices.
- Building the forest route, environmental clues, objectives, and item interactions.
- Assigning final music, ambience, footsteps, voices, ghost audio, and burn effects.
- Improving lighting and fog while maintaining integrated-GPU performance.
- Connecting difficulty, tutorial, autosave, camera shake, and accessibility preferences to their final gameplay systems.
- Testing the complete City-to-Village-to-Forest flow on keyboard, mouse, and gamepad.
- Adding focused automated and play-mode tests for shared gameplay contracts.

## Design And Implementation Principles

- Gameplay values remain Inspector-configurable so they can be tuned through playtesting.
- Player movement, camera control, UI feedback, enemy logic, and audio remain separate responsibilities.
- New controls must use the Unity Input System rather than `UnityEngine.Input`.
- Player-facing text must use localization keys instead of hardcoded single-language strings.
- The enemy is treated as a sentient floating fire/plasma presence, not a humanoid character.
- Story documentation in the public repository avoids puzzle solutions and ending spoilers.

## Cultural And Content Note

This game is a fictional work inspired by Indonesian folklore and regional storytelling. Its interpretation of Banaspati is created for this project's narrative and should not be treated as a definitive account of any single tradition.

The project contains horror themes, burning imagery, pursuit, flashing visual feedback, death sequences, and optional strong language. Accessibility and profanity-filter options are being developed alongside the content.

## License

Copyright (c) 2026 Ikbal Gumilar.

This project is licensed under the [MIT License](LICENSE). The license permits use, modification, distribution, and sublicensing under its stated conditions, while providing the software without warranty.

## Author

Created and maintained by **Ikbal Gumilar**.
