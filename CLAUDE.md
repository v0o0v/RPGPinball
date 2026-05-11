# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity 6 (`6000.4.0f1`) 2D project — a pinball game with RPG mechanics layered on top (player stats, enemies, damage from ball velocity). Uses Universal Render Pipeline (2D Renderer) and the new Input System package. Single scene entry point: [Main.unity](Assets/01.Scenes/Main.unity).

## Build / run

There is no CLI build path here. All build, play, and test workflows go through the Unity Editor (Editor version pinned in [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt)). Open the project root in Unity Hub with the matching editor version; the scene auto-opens via build profiles in [Assets/Settings/Build Profiles](Assets/Settings/Build%20Profiles).

Tests: `com.unity.test-framework` is installed but no test assemblies / `Tests~` folders exist yet — adding tests means creating an asmdef under a new `Tests` folder.

## Folder convention

Assets follow a **numbered top-level** convention; preserve it when adding new content:

- `01.Scenes/` — scenes
- `02.Scripts/{Core,Pinball,RPG}/` — gameplay code, one subfolder per namespace
- `03.Sprites/`, `03.Textures/` — 2D art
- `05.Data/` — ScriptableObject assets (e.g. flipper tuning) and `Physics/` 2D physics materials
- `05.Prefabs/Pinball/` — gameplay prefabs (ball, bumpers)
- `50. Extenal Assets/` — third-party (ProCamera2D, Kenney brick pack); do **not** modify

`GeneratedAssets/` at repo root is gitignored — assume it's a scratch output dir.

## Code architecture

Three namespaces matching the script subfolders:

- **`RPGPinball.Core`** — cross-cutting singletons and UI glue.
  - [GameManager.cs](Assets/02.Scripts/Core/GameManager.cs) is a `DontDestroyOnLoad` singleton exposing `Score` and an `OnScoreChanged` `Action<int>`. Anything that needs to award points calls `GameManager.Instance.AddScore(...)`; UI subscribes to the event.
  - [ScoreDisplay.cs](Assets/02.Scripts/Core/ScoreDisplay.cs) is the canonical consumer pattern — subscribe in `Start`, unsubscribe in `OnDestroy`.
  - [CameraController.cs](Assets/02.Scripts/Core/CameraController.cs) integrates with **ProCamera2D** (`Com.LuisPedroFonseca.ProCamera2D`). It guards with `FindObjectOfType<ProCamera2D>()` before touching `ProCamera2D.Instance` — keep that guard if you edit, because the plugin's Instance property throws when missing.

- **`RPGPinball.Pinball`** — physics-driven gameplay components.
  - [FlipperController.cs](Assets/02.Scripts/Pinball/FlipperController.cs) drives a `HingeJoint2D` + `JointMotor2D` and listens to an `InputAction` configured per-instance in the inspector. Tunable values (mass, motor torque, angle limits, kick force) come from a [`FlipperData`](Assets/02.Scripts/Pinball/FlipperData.cs) ScriptableObject — instances live at [LFlipper.asset](Assets/05.Data/LFlipper.asset) / [RFlipper.asset](Assets/05.Data/RFlipper.asset). When tuning flipper feel, edit the asset, not the script.
  - [BallController.cs](Assets/02.Scripts/Pinball/BallController.cs) clamps `Rigidbody2D.linearVelocity` in `FixedUpdate`.
  - [Bumper.cs](Assets/02.Scripts/Pinball/Bumper.cs) is the shared bumper logic; the three bumper prefabs in `05.Prefabs/Pinball/` all use it with different inspector values.
  - Ball detection across all pinball scripts uses `CompareTag("Ball")` — the ball prefab must keep that tag.

- **`RPGPinball.RPG`** — overlay layer for stats/damage.
  - [PlayerStats.cs](Assets/02.Scripts/RPG/PlayerStats.cs), [Enemy.cs](Assets/02.Scripts/RPG/Enemy.cs), and the static [DamageSystem.cs](Assets/02.Scripts/RPG/DamageSystem.cs) form an emerging system where ball velocity → damage. Currently `Enemy` uses a hard-coded `damageToTake = 10` (marked TODO) — the intended wiring is `DamageSystem.CalculateDamage(playerStats, ballRb)`.

### Cross-cutting patterns to preserve

- **ScriptableObject for tuning** — `FlipperData` is the template. New tunable systems should follow the same `[CreateAssetMenu(menuName = "RPG Pinball/...")]` pattern rather than hard-coding values or using serialized fields on MonoBehaviours.
- **Event-based score/UI** — never poll `GameManager.Instance.Score` from Update; subscribe to `OnScoreChanged`.
- **Pinball → Core dependency direction** — `Pinball` and `RPG` may reference `Core`; `Core` does not reference them. Don't introduce reverse dependencies.

## Review style

PR review style is defined in [.gemini/styleguide.md](.gemini/styleguide.md) — **all review comments must be written in Korean**, prioritized `correctness > performance > structure > style`, and limited to genuine issues (skip nits/naming preferences). Comment budget is 6 (see [.gemini/config.yaml](.gemini/config.yaml)). When using the `/review` skill on this repo, follow that guide.
