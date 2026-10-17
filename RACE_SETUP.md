# Fun Race & Parkour — Race Setup

## First-time setup

The first scene to play is `Assets/Scenes/MainMenu.unity`. It is produced by the level builder; the repository's existing `GetStarted_Scene` is the scene to open before running the builder if no race scenes have been generated yet.

In Unity 6, run these menu commands in order:

1. **Tools → FunRace → Build All Levels** — creates the Main Menu, the three race scenes, and the `AIRacer.prefab` variant. It also refreshes Build Settings.
2. **Tools → FunRace → Update Build Settings** — explicitly confirms the order: MainMenu, Level 1, Level 2, Level 3.
3. **Tools → FunRace → Validate Levels** — checks jumpable gaps, lane coverage, checkpoints, finish triggers, and the single RaceManager in each race scene.
4. **Tools → FunRace → Run All Tests** — runs the EditMode-style math, ranking, progression, hazard prediction, and track-layout checks.

Then open `Assets/Scenes/MainMenu.unity` and press Play. To test one race directly, open its scene and press Play; Level 1 is selected by default when opened directly.

## Controls and race flow

- **W / S:** move forward or backward along the fixed X-axis course.
- **Space:** jump.
- **Esc:** pause or resume.
- A three-second countdown releases all racers together. One race timer starts at GO.
- The player must finish to unlock the next level; winning is not required. Best times and unlocks persist in PlayerPrefs under `FRP_` keys.

## Track and obstacle constraints

- Racers use parallel Z lanes at `-1.5`, `0`, and `+1.5` metres; the human uses the middle lane.
- Track obstacles and platforms are generated 6 metres wide across Z, covering all lanes with margin.
- Keep gaps below the safe reach for the shared controller setup: 2 m/s, 1.2 m jump height, and -15 m/s² gravity. The editor validator applies a 15% reach margin (about 1.36 m maximum gap).
- Checkpoints should precede long or combined hazard sections. Their respawn positions preserve each racer's lane and place the racer just before the trigger.
- Moving hazards use deterministic time functions so the AI can query the same timing window that governs the obstacle.

## Tuning

- Obstacle placements and level themes are in `Assets/Editor/LevelBuilder.cs` (`Level1Layout`, `Level2Layout`, `Level3Layout`).
- Shared obstacle dimensions and lane span are in `Assets/Editor/TrackObstacleFactory.cs`.
- Human and AI movement values are aligned in `RacerSetup`; do not tune AI speed, jump height, gravity, or CharacterController dimensions separately.
- AI behavior profiles are ScriptableObjects in `Assets/ScriptableObjects/AI/`. Tune reaction time, jump timing error, safety margin, risk tolerance, and recovery delay only.
- Main menu settings persist master/music/effects volume, look sensitivity, quality, VSync, resolution, and fullscreen state.

## Verification notes and remaining Editor work

Unity 6.6.4f1 is installed, but batch mode refused to open this checkout because Unity reports another Editor instance already has the project open. No successful Unity compile or PlayMode run is claimed. After the current Editor session releases the project, allow Unity to import the new scripts, check the Console, run the builder and validator commands above, and test each generated level in Play Mode. The custom test runner is editor-menu based; the project does not currently include Unity's Test Framework package or an automated PlayMode test assembly.

The project contains no AudioMixer asset. The generated scenes assign the existing clips to AudioManager AudioSources, with volume controls applied through those sources.
