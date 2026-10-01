# MOMENTUM

**MOMENTUM** is a fast first-person movement shooter built in Unity: slide, wallrun, grapple, rocket jump and shotgun-boost your way through ten handcrafted levels and four boss fights while racing the clock for an S rank. It ships with a full in-game level editor, JSON custom maps, local best times and splits, and a one-click setup tool that generates every prefab, material, scene and level from code.

The project is an original game inspired by the *style* of momentum shooters such as DaniDev's KARLSON. It does **not** contain any code, levels, models, textures or audio from KARLSON or any other game. All visuals are procedural (primitives, generated meshes, a grid shader) and every sound is synthesized at runtime, so there are no third-party assets and no downloads.

> "MOMENTUM" is a working title. Change it in `Assets/_Game/Resources/GameConfig.asset` (`Game Title`) after setup. The title is used by the menus, window title and credits.

---

## Table of contents

1. [Unity version](#unity-version)
2. [Installation](#installation)
3. [Opening the project](#opening-the-project)
4. [Initial setup](#initial-setup)
5. [What the setup tool does](#what-the-setup-tool-does)
6. [Controls](#controls)
7. [Gameplay](#gameplay)
8. [Movement](#movement)
9. [Weapons](#weapons)
10. [Enemies](#enemies)
11. [Bosses](#bosses)
12. [Campaign levels](#campaign-levels)
13. [Level editor](#level-editor)
14. [Creating custom levels](#creating-custom-levels)
15. [Save file location](#save-file-location)
16. [Project structure](#project-structure)
17. [Adding new weapons](#adding-new-weapons)
18. [Adding new enemies](#adding-new-enemies)
19. [Adding new bosses](#adding-new-bosses)
20. [Creating campaign levels](#creating-campaign-levels)
21. [Build instructions](#build-instructions)
22. [Troubleshooting](#troubleshooting)

---

## Unity version

- **Unity 2022.3 LTS** (the project was authored against `2022.3.45f1`; any 2022.3.x release works).
- **Built-in Render Pipeline**, 3D Core template settings.
- Legacy **Input Manager** (no Input System package needed).
- Packages: only `com.unity.ugui` and Unity's built-in modules (see `Packages/manifest.json`). No paid or Asset Store assets.

Newer versions (Unity 6) will usually upgrade the project fine, but 2022.3 LTS is the supported version.

## Installation

1. Install **Unity Hub** and **Unity 2022.3 LTS** (add the build support modules for the platforms you want to build for).
2. Clone the repository:
   ```bash
   git clone https://github.com/Corbino50211/karlson.git
   ```
   (or download the ZIP from GitHub and extract it).

The repository only contains `Assets/`, `Packages/`, `ProjectSettings/` and this README. Unity regenerates `Library/`, `Temp/`, `Logs/` and `UserSettings/` locally; they are excluded by `.gitignore`.

## Opening the project

1. Open **Unity Hub** > **Projects** > **Add** > **Add project from disk** and select the cloned folder (the one that contains `Assets/`).
2. If Hub asks for an editor version, pick your installed **2022.3** editor.
3. Open the project. The first import takes a minute or two while Unity compiles the scripts and builds the `Library/` cache.
4. Wait until the progress bar finishes and the Console shows no compile errors.

When the project opens for the first time it has no scenes yet - that is expected. The setup tool creates them.

## Initial setup

Run **one** menu item:

**`Tools > Parkour FPS > Setup Complete Game`**

That's it. When the dialog says "Setup complete", the `MainMenu` scene is open: press **Play**.

The setup takes roughly 15-60 seconds depending on your machine. It is **idempotent**: you can run it again at any time (after pulling changes, or if something got deleted). Existing assets are overwritten *in place* so their GUIDs - and therefore all references - stay valid, tuned ScriptableObject values (weapon stats, enemy stats, movement settings) are kept, and scenes are regenerated from scratch so you never get duplicates like `Player (1)`.

Other menu items:

| Menu item | What it does |
|---|---|
| `Tools > Parkour FPS > Setup Complete Game` | Builds everything (see below). |
| `Tools > Parkour FPS > Rebuild Demo Levels` | Regenerates only the main menu scene and the 10 campaign scenes (+ their JSON exports) from `CampaignLevels.cs`. |
| `Tools > Parkour FPS > Validate Project` | Checks layers, tags, input axes, GameConfig and registries, all prefabs (missing scripts, missing references, required components), weapon/enemy definitions, scenes and Build Settings. Prints one readable report to the Console. |
| `Tools > Parkour FPS > Unlock All Levels` | Writes "unlock all" into the local save file. |
| `Tools > Parkour FPS > Clear Local Save` | Deletes best times/unlocks (optionally settings too). Custom levels are kept. |
| `Tools > Parkour FPS > Open Main Menu Scene` | Opens `MainMenu.unity`. |
| `Tools > Parkour FPS > Open Save Folder` | Reveals `Application.persistentDataPath`. |

## What the setup tool does

`Setup Complete Game` runs these generators in order (all in `Assets/_Game/Editor/`):

1. **ProjectConfigurator** - creates the folder structure; adds tags (`Enemy`, `Boss`, `Pickup`, `NoWallRun`, `Checkpoint`, `Hazard`) and layers (`Player` 8, `Enemy` 9, `Environment` 10, `Projectile` 11, `Pickup` 12, `Grapple` 13, `Prop` 14, `ViewModel` 15, `EditorGizmo` 16, `Trigger` 17, `Debris` 18); configures physics (gravity, layer collision matrix, 100 Hz fixed timestep); adds missing Input Manager axes; sets player settings (product name, linear color space, 1920x1080).
2. **ScriptableObjectGenerator** - creates `GameConfig` (in `Resources/` so the runtime finds it), `PrefabRegistry`, `MaterialLibrary`, `AudioLibrary`, `MovementSettings`, `CameraFeelSettings`, `LevelRegistry` and the 10 `LevelDefinition` assets, and links them together.
3. **MaterialGenerator** - generates textures (soft particle, line, editor grid), all world materials (grid-shaded architecture palette, enemy/boss/weapon materials, emissive FX, transparent helpers, particles, gizmo overlays) and four procedural skyboxes.
4. **MeshAssetGenerator** - saves the procedural meshes (ramp wedge, torus, cone, band, chevron) as assets.
5. **EffectPrefabGenerator** - muzzle flash, impacts, explosions, tracers, rail trail, debris, glass shards, telegraphs, shockwave, laser beam, pickup/checkpoint bursts and every projectile (enemy bullet/pellet, energy orb, rocket, missile, grenade, rock).
6. **WeaponPrefabGenerator** - the 8 `WeaponDefinition` assets plus first-person view models and world models.
7. **EnvironmentPrefabGenerator** - every level-editor object: geometry, moving platform, door, glass, breakable wall, launch pad, speed pad, grapple point, spawn, start gate, checkpoint, finish, kill zone, hazard panel, secret, sign, weapon/ammo/health pickups, crate, barrels, light, boss arena trigger and boss sockets.
8. **EnemyPrefabGenerator** - the 6 `EnemyDefinition` assets and enemy prefabs.
9. **BossPrefabGenerator** - the 4 boss prefabs and the Core's weak point.
10. **PlayerPrefabGenerator** - the Rigidbody player with cameras, weapon holder, grapple rope and indicator, all references wired.
11. **UIGenerator** - HUD, pause menu, results screen, main menu and level editor UI saved as prefabs (restyle them in the Inspector).
12. **SceneGenerator** - `MainMenu` (animated diorama), `LevelEditor`, `CustomLevel` (plays JSON maps) with realtime lighting.
13. **DemoLevelGenerator** - the 10 campaign scenes from `CampaignLevels.cs` (objects are prefab instances) plus a JSON export of each layout in `Assets/_Game/Levels/Campaign/`.
14. Writes **Build Settings** (MainMenu first) and runs the validator.

Nothing has to be wired by hand. If you delete a generated asset, just run the setup again.

## Controls

| Action | Key |
|---|---|
| Move | **W A S D** |
| Look | **Mouse** |
| Jump / wall jump | **Space** |
| Slide / crouch | **Left Ctrl** (or **C**) |
| Sprint | **Left Shift** |
| Fire | **Left Mouse** |
| Alt fire / aim | **Right Mouse** |
| Reload | **R** |
| Interact (doors, pickups) | **E** |
| Grapple (hold) | **Q** |
| Select weapon slot | **1 - 9** |
| Switch weapon | **Mouse wheel** |
| Pause menu | **Esc** |
| Restart from last checkpoint | **T** |
| Debug overlay | **F3** |

Developer shortcuts (only when `GameConfig > Developer Mode` is enabled): **F5** restart level, **F6** finish level instantly.

Bindings live in `KeyBindings.cs` (serialized on the player's `PlayerInputHandler`), so they can be changed per prefab.

## Gameplay

- Each level is a **time trial**. The timer starts when you cross the **start gate** and stops at the **finish**. Your time earns a rank: **S / A / B / C / D** (thresholds per level in its `LevelDefinition`, or `rankTimes` in a JSON map).
- **Checkpoints** save your respawn point and record a split. Dying respawns you at the last checkpoint instantly (no scene reload); the clock keeps running. Press **T** to respawn on purpose.
- **Personal bests**, best splits, deaths, kills and found secrets are saved locally. The HUD shows the current time, your PB and split deltas.
- **Arena doors** open when every enemy of their arena is dead. **Boss doors** close when the fight starts. Boss levels only let you finish once the boss is defeated.
- **Secrets** are hidden in most levels - look for odd ledges, grapple points off the main path and suspicious walls.
- **Unlocks:** Training Facility is unlocked from the start; finishing a level unlocks the next. Use `Tools > Parkour FPS > Unlock All Levels` (or set `GameConfig > Unlock All Levels`) to open everything.
- **Results screen** after each run: time, rank, PB, deaths, kills, secrets, with Retry / Next Level / Level Select / Main Menu.
- **Settings** (saved locally): mouse sensitivity, invert Y, field of view, master/music/SFX volume, fullscreen, resolution, VSync, FPS limit, camera shake, head bob, weapon bob, motion effects (FOV kick, tilt, landing dip).
- **F3 debug overlay:** FPS, velocity, horizontal speed, grounded / sliding / wallrunning / grappling state, current weapon, checkpoint.

## Movement

The player is a **Rigidbody** (no CharacterController) with Quake-style acceleration, so speed is earned and kept:

- **Ground movement** with acceleration, friction and counter-movement for snappy stops; sprint optional.
- **Air strafing** and air acceleration: strafe while turning the mouse to curve and build speed.
- **Bunny hopping:** jumping within a short window of landing skips ground friction and keeps your momentum (hold Space to auto-hop).
- **Coyote time** and **jump buffering** so jumps feel fair.
- **Slopes**: run up and slide down ramps; sliding downhill accelerates.
- **Slide**: press Ctrl while running for a speed boost; **slide jump** carries it forward. Slide under low gaps.
- **Crouch** under obstacles (capsule resizes).
- **Wallrun** along walls (hold W, jump at a wall), **wall jump** off them, **wall kicks** to climb corners.
- **Vault** over low obstacles and **mantle** onto ledges up to ~2.3 m.
- **Grapple**: hold **Q** while aiming at any surface or a glowing grapple point (magnetic aim assist). Swing with momentum, reel in, release for a boost, jump to release with extra height. A ring shows your target.
- **Launch pads**, **speed pads** and **moving platforms** (you inherit the platform's velocity).
- **Knockback**: shotgun blasts, rocket explosions and enemy hits push you around - use them.
- **Fall respawn** below each level's kill height.

Every value (speeds, jump, gravity, slide, wallrun, grapple, vault/mantle, ground snapping...) is exposed in `ScriptableObjects/Settings/MovementSettings.asset`. Camera feel (FOV, speed FOV, landing dip, head bob, tilt, recoil recovery, weapon FOV) is in `CameraFeelSettings.asset`.

## Weapons

Weapons are physical pickups in the levels (walk into them or press E). Picking up a weapon you already own refills its ammo. Ammo boxes refill reserves of everything.

| Slot | Weapon | Notes | Alt fire (RMB) |
|---|---|---|---|
| 1 | **Pistol** | Accurate semi-auto, infinite reserve. | Zoom |
| 2 | **SMG** | Fast full-auto, spread grows with your speed. | Zoom |
| 3 | **Shotgun** | 10 pellets, huge knockback. **Shotgun jumping:** shoot the floor mid-jump; blasts near walls/floors give an extra surface boost. | Double shot |
| 4 | **Assault Rifle** | Accurate full-auto. | Zoom (scope) |
| 5 | **Revolver** | Heavy hitter with big knockback. | Fan the hammer (3-shot burst) |
| 6 | **Rocket Launcher** | Explosions hurt enemies, push props and launch you. **Rocket jump:** aim at your feet and jump - your current momentum is preserved and added to. | Detonate rockets mid-air |
| 7 | **Grenade Launcher** | Bouncing grenades on a fuse. | Detonate grenades |
| 8 | **Railgun** | Instant beam that pierces several enemies, strong recoil and self-knockback. | Zoom |

Headshots (and drone/turret sensors) deal critical damage. Explosions break crates, glass and walls and set off explosive barrels.

## Enemies

All enemies share `EnemyBase` with an AI state machine: **Idle, Patrol, Alert, Chase, Attack, Search, Stunned, Dead**. They use sight cones, hearing, a reaction delay, NavMesh pathfinding (baked at runtime for every level, including custom maps), auto-targeting with velocity lead, and break into physics debris when killed (sometimes dropping health or ammo).

| Enemy | Behaviour |
|---|---|
| **Gunner** | Keeps medium range, strafes and fires 3-round bursts. |
| **Brute** (shotgunner) | Rushes you and fires a wide pellet blast up close. |
| **Marksman** (sniper) | Stays far away; a laser sight tracks you (yellow), locks (red) and fires a deadly shot. Break line of sight! |
| **Charger** | Winds up and charges in a straight line; knocks you flying on hit and gets stunned if it hits a wall. |
| **Drone** | Flies, orbits you and shoots; explodes when destroyed. |
| **Turret** | Stationary, sweeps and fires bursts. Can be shielded by The Core. |

Stats live in `ScriptableObjects/Enemies/Enemy_*.asset`.

## Bosses

Bosses use `BossBase`: an arena trigger starts the fight (doors close), a big health bar appears, phases change at health thresholds, attacks are chosen from a weighted, cooldown-based table with telegraphs, and the fight resets cleanly if you die. Defeating a boss plays an explosion sequence and unlocks the exit.

| Boss | Level | Attacks |
|---|---|---|
| **The Warden** | 08 | Leaping **ground slam**, **shotgun** blast, **charge** (stunned if it hits a wall), **shockwave** ring (jump it), homing **missiles**; faster and more aggressive each phase. |
| **The Sentinel** | 09 | Floating eye: **laser sweep**, **energy orb volleys**, **summons drones**, **rotating beams** (jump over), electrifies **floor panels**. |
| **The Juggernaut** | 06 | **Charges** that smash through breakable walls, **slam**, **throws rocks**; enters a **rage** phase at low health. |
| **The Core** | 10 | Four stages: shielded while **turrets** are up, sweeping **lasers** (jump the low beam, slide under the high one), **enemy waves**, then it rises and you must destroy the **weak points** around the arena (use the towers, launch pads and grapple points). |

## Campaign levels

| # | Level | Highlights |
|---|---|---|
| 01 | **Training Facility** | Tutorial: jumps, mantle, vault, slide tunnel, wallrun pit, first arena, launch pad, grapple gap. |
| 02 | **Warehouse** | Shelving aisles vs. a fast catwalk route, glass and breakable partitions, conveyor speed pad, loading-dock arena. |
| 03 | **Construction Site** | Climb an unfinished building, ride the crane load across the excavation, roof finish. |
| 04 | **Tower** | Night climb up a hollow tower above a coolant pool: ledges, launch pads, elevator, grapple. |
| 05 | **Research Complex** | Glass labs, electrified floor corridor, balcony arena, vent slide, sniper gallery. |
| 06 | **Reactor** | Coolant channels, moving platform or wallrun route, rocket launcher - then **The Juggernaut**. |
| 07 | **Sky Facility** | Floating platforms: speed-pad gap jump, grapple chain, wall-jump gauntlet, launch pad hops, railgun. |
| 08 | **The Warden** | Boss arena with pillars, corner platforms, launch pads and grapple points. |
| 09 | **The Sentinel** | Arena with an electrified floor grid and raised safe platforms. |
| 10 | **The Core** | Final arena with corner towers, wall bridges, turret mounts and weak points. |

Every level has alternate routes or shortcuts and most have a secret. Boss levels are shown in red in the level select.

## Level editor

Open it from **Main Menu > LEVEL EDITOR** (or play the `LevelEditor` scene).

**Layout:** object browser on the left (categories: Geometry, Movement, Gameplay, Enemies, Pickups, Props, Lights, Boss), toolbar at the top (New, Save, Load, Test, Undo, Redo, Move/Rotate/Scale, Snap, Grid size, Rotation step, Help, Menu, level name), properties on the right (transform, material and every object-specific property such as moving platform offset/speed, door mode, enemy type, weapon type, pad strength, light color, boss type...).

| Control | Action |
|---|---|
| Hold **Right Mouse** + **WASD / Q E** | Fly the camera (Shift = fast, wheel while flying = speed) |
| **Middle Mouse** drag / **wheel** | Pan / zoom |
| **Left Click** | Select (**Shift/Ctrl + Click** = multi-select) |
| Click an object in the browser, then click in the world | Place (R / Q rotate the placement, Esc cancels) |
| **W / E / R** | Move / Rotate / Scale gizmo |
| **G** | Toggle grid snap (GRID and ROT buttons change the step) |
| **Arrow keys / PgUp / PgDn** | Nudge selection |
| **Ctrl+Z / Ctrl+Y** | Undo / Redo |
| **Ctrl+C / Ctrl+V / Ctrl+D** | Copy / Paste (at the mouse) / Duplicate |
| **Ctrl+A** | Select all |
| **Delete** | Delete selection |
| **F** | Focus the selection |
| **Ctrl+S** | Save |
| **F5** | **Test** the level (plays it with the real player); F5 again returns to the editor |
| **H** | Help panel |

While testing, the pause menu has "Back to Editor". Test runs never record best times.

## Creating custom levels

1. Open the editor, give the level a name in the toolbar.
2. Place a **Spawn Point** (required, one per level), a **Start Gate** (optional - without one the timer starts when you move) and a **Finish**.
3. Build with geometry, add checkpoints (set their **Order**), enemies (set an **Arena Id** and give a **Door** the same id in *ArenaClear* mode for arenas), pickups, pads, grapple points, hazards...
4. Press **F5** to test, **Ctrl+S** to save.
5. Play it from **Main Menu > LEVEL SELECT > CUSTOM MAPS**, where you can also edit or delete levels and open the levels folder.

Custom levels are plain JSON files, so they can be shared by copying the file into someone else's levels folder.

### JSON format

```json
{
  "formatVersion": 1,
  "levelId": "my-level",
  "levelName": "My Level",
  "author": "you",
  "killHeight": -40,
  "rankTimes": { "s": 30, "a": 45, "b": 60, "c": 90 },
  "music": "Level",
  "startingWeapons": ["pistol"],
  "environment": { "preset": "Day", "skybox": "Day", "fog": true, "...": "..." },
  "objects": [
    {
      "id": 1,
      "objectType": "Floor",
      "position": { "x": 0, "y": -0.25, "z": 0 },
      "rotation": { "x": 0, "y": 0, "z": 0 },
      "scale": { "x": 10, "y": 0.5, "z": 10 },
      "properties": [ { "key": "material", "value": "LightGray" } ]
    },
    {
      "id": 2,
      "objectType": "MovingPlatform",
      "position": { "x": 0, "y": 2, "z": 12 },
      "rotation": { "x": 0, "y": 0, "z": 0 },
      "scale": { "x": 4, "y": 0.4, "z": 4 },
      "properties": [
        { "key": "offset", "value": "0,0,10" },
        { "key": "speed", "value": "4" }
      ]
    }
  ]
}
```

`objectType` is one of the types in `LevelObjectCatalog.cs` (Cube, Floor, Wall, Ramp, Stairs, Platform, Pillar, MovingPlatform, Door, Glass, BreakableWall, LaunchPad, SpeedPad, GrapplePoint, SpawnPoint, StartTrigger, Checkpoint, FinishTrigger, KillZone, HazardPanel, Secret, Sign, EnemySpawn, WeaponPickup, AmmoPickup, HealthPickup, Crate, Barrel, ExplosiveBarrel, Light, BossTrigger, BossSocket). The catalog also defines each type's default size and editable properties. The campaign layouts are exported in the same format to `Assets/_Game/Levels/Campaign/` - copy one into your levels folder to remix it.

## Save file location

Everything is stored in Unity's `Application.persistentDataPath`:

| File | Content |
|---|---|
| `save.json` | Unlocks, best times, best splits, ranks, attempts, deaths, secrets |
| `settings.json` | Settings menu values |
| `Levels/*.json` | Custom levels from the editor |

Typical locations (company/product names come from Player Settings):

- **Windows:** `%USERPROFILE%\AppData\LocalLow\<Company>\MOMENTUM\`
- **macOS:** `~/Library/Application Support/<Company>/MOMENTUM/`
- **Linux:** `~/.config/unity3d/<Company>/MOMENTUM/`

Use `Tools > Parkour FPS > Open Save Folder` to jump there, or **CUSTOM MAPS > OPEN FOLDER** in game.

## Project structure

```
Assets/_Game/
  Art/Shaders/           WorldGrid (architecture grid), GizmoOverlay (editor gizmos)
  Editor/                Setup tool, generators, validator, CampaignLevels.cs
  Scripts/
    Audio/               AudioManager, procedural sound synthesis, AudioLibrary
    Bosses/              BossBase + Warden, Sentinel, Juggernaut, Core, arena/telegraph/laser/shockwave
    Combat/              DamageInfo, Health, Hitbox, Projectile, Grenade, Explosions, Tracer
    Core/                GameManager, GameConfig, PrefabRegistry, MaterialLibrary, GameEvents, Layers, bootstrap, scene loading
    Enemies/             EnemyBase + six enemy types, navigation, spawner
    LevelEditor/         LevelEditorManager, camera, gizmo, history (undo/redo), UI, level browser
    LevelSystem/         LevelManager, LevelLoader, LevelData (JSON), catalog, factory, builder, checkpoints, timer, ranks
      Environment/       Pads, platforms, doors, breakables, hazards, triggers, signs, lights
    Movement/            PlayerMovement, WallRunAbility, LedgeAbility, GrappleHook, GrappleRope, MovementSettings
    Player/              PlayerController, camera, input, health, combat, interaction, audio, camera shake
    Save/                SaveManager, SettingsManager, JSON helpers
    UI/                  HUD, menus, level select, settings, results, debug overlay (uGUI built in code)
    Utilities/           Pooling, procedural meshes, helpers
    Weapons/             WeaponBase + hitscan/shotgun/railgun/projectile weapons, WeaponManager, sway, pickups
  (generated by setup)   Materials/, Prefabs/, Resources/, ScriptableObjects/, Scenes/, Levels/Campaign/
```

Systems communicate through `GameEvents` and look prefabs up in the `PrefabRegistry`, so scenes and custom levels need no manual references. Scene names come from `GameConfig` and `LevelDefinition` assets, never from hard-coded strings in gameplay code.

## Adding new weapons

1. Create a `WeaponDefinition` (*Create > Momentum > Weapon Definition*) or duplicate one in `ScriptableObjects/Weapons/`. Give it a unique `id`, slot, stats, fire mode and alt fire.
2. Make a view model prefab: a root with a `WeaponBase` subclass (`HitscanWeapon`, `ShotgunWeapon`, `RailgunWeapon`, `ProjectileWeapon` - or your own subclass overriding `Fire`), a `Muzzle` transform, optional muzzle flash particle and light. Assign the definition on the component and the prefab in `viewModelPrefab`.
3. Make a world model (any mesh) and assign it to `worldModelPrefab` (used by pickups). Projectile weapons also need `projectilePrefab` (see `Prefabs/Projectiles/Rocket.prefab`).
4. Add the definition to `PrefabRegistry > Weapons`.
5. To make it placeable in the level editor, add its id to `LevelObjectCatalog.WeaponIds`.

Tip: the easiest route is to add a new `Spec` entry in `WeaponPrefabGenerator.cs` and rerun the setup - it builds the definition, view model and world model for you.

## Adding new enemies

1. Add a value to `EnemyType` (append at the end so saved data stays valid).
2. Create a subclass of `EnemyBase` and override the state methods you need (`UpdateChase`, `UpdateAttack`, ...). Helpers: `MoveTo`, `StopMoving`, `FacePoint`, `CanSee`, `FireProjectile`, `SpawnMuzzleFlash`, `SetState`.
3. Create an `EnemyDefinition` asset with its stats.
4. Build a prefab (Rigidbody, collider, `EnemyHealth`, `HitFlash`, your component with definition/eye/aim pivot/muzzle/visual root assigned) - or add a builder in `EnemyPrefabGenerator.cs` and rerun the setup.
5. Register it in `PrefabRegistry > Enemies` and add the type name to `LevelObjectCatalog.EnemyTypes` so the editor can place it.

## Adding new bosses

1. Add a value to `BossType`.
2. Subclass `BossBase`: implement `PopulateDefaultAttacks()` (attack ids, cooldowns, phases, weights, ranges) and `PerformAttack(string id)` (a coroutine per attack). Use the helpers: `TelegraphCircle`, `TelegraphLane`, `FireProjectile`, `SpawnShockwave`, `SpawnLaser`, `AreaBlast`, `MoveBody`, `RotateBodyTowards`, `Track` (auto-cleanup on reset). Override `UpdateBoss`, `Intro`, `OnFightStarted`, `OnPhaseChanged`, `OnReset` as needed.
3. Build the prefab (kinematic Rigidbody, colliders, `BossHealth`, `HitFlash`, your boss component) or add it to `BossPrefabGenerator.cs`.
4. Register it in `PrefabRegistry > Bosses` and add the name to `LevelObjectCatalog.BossTypes`.
5. Place a **Boss Arena** object (BossTrigger) in a level and pick your boss.

## Creating campaign levels

Campaign levels are authored in code in `Assets/_Game/Editor/CampaignLevels.cs` with the fluent `LevelBuilder`:

```csharp
static LevelData MyLevel()
{
    var b = New(Get("level11"), "Dusk", -20f, "Level", "pistol", "shotgun");
    Rect(b, -10f, 10f, 0f, 40f, 0f);                 // floor slab, top at y = 0
    b.Spawn(V(0f, 0f, 2f), 0f);
    b.StartGate(V(0f, 3f, 6f), 0f, V(20f, 6f, 1f));
    b.LaunchPad(V(0f, 0f, 20f), 0f, 22f, 6f);
    b.Enemy(EnemyType.Gunner, V(4f, 0f, 30f), 180f);
    b.Checkpoint(V(0f, 0f, 34f), 0f, 1);
    b.Finish(V(0f, 0f, 38f), 0f, false);
    return b.Data;
}
```

1. Add an `Entry` to `CampaignLevels.All` (id, number, name, scene name, ranks, boss flag, builder method).
2. Run `Tools > Parkour FPS > Rebuild Demo Levels` (or the full setup). It creates the scene, the `LevelDefinition`, the JSON export and adds the scene to Build Settings.

Alternatively build a level in the in-game editor, then recreate it as a scene: any scene works as a campaign level as long as it contains a `LevelManager` (with `LevelTimer`, `CheckpointManager`, `NavMeshBaker`), a `UIManager`, a `SpawnPoint` and a `FinishTrigger`, and its `LevelDefinition` is in the `LevelRegistry` with the scene in Build Settings. Rank times are tuned per level in the `LevelDefinition` asset.

Level design reference (default movement settings): run 11 m/s, sprint 14 m/s, jump ~1.6 m high and ~7 m long, mantle up to 2.3 m, vault up to 1.15 m, wallrun ~20 m, grapple range 42 m, launch pad (22 up) ~8.6 m.

## Build instructions

1. Run `Tools > Parkour FPS > Setup Complete Game` (Build Settings are written automatically: `MainMenu` first, then `LevelEditor`, `CustomLevel` and the 10 levels).
2. Optionally run `Tools > Parkour FPS > Validate Project` - it should report 0 errors.
3. **File > Build Settings**, choose the platform (Windows, macOS or Linux), then **Build** (or **Build And Run**) into a folder outside `Assets/` (for example `Builds/`, which is git-ignored).
4. Custom levels and saves are written to the player's persistent data folder, so the build folder can be read-only.

## Troubleshooting

| Problem | Fix |
|---|---|
| Main menu is empty / "No player prefab registered" / pink materials | Run `Tools > Parkour FPS > Setup Complete Game`. |
| `Tools > Parkour FPS` menu is missing | The scripts have not compiled. Check the Console for errors (usually from a wrong Unity version or a modified script) and fix them; the menu appears after a successful compile. |
| Compile errors right after opening | Make sure you opened the project with **Unity 2022.3**. Let the import finish, then use *Assets > Reimport All* if Unity was interrupted. |
| Layer warnings ("Layer 8 was ... renaming") | The setup renames layers 8-18. If you already used those layers for something else, move your objects to other layers first. |
| Enemies stand still | They need a NavMesh, which is baked at runtime from the level geometry. Check the Console for NavMesh warnings and make sure enemies stand on geometry (floors, platforms). Run the validator. |
| Input does nothing in menus | Run the setup again - it adds the Input Manager axes uGUI needs (Horizontal, Vertical, Submit, Cancel). |
| Mouse cursor stuck / not visible | Press **Esc** to open the pause menu, which unlocks the cursor. |
| No sound | Check the volume sliders in Settings. All sounds are synthesized, so no audio files are needed. |
| Low frame rate in the editor | Disable *Gizmos* in the Game view, close the Scene view while playing, or lower *FPS Limit / Resolution* in Settings. |
| Levels locked | `Tools > Parkour FPS > Unlock All Levels`, or tick *Unlock All Levels* on `GameConfig`. |
| Reset everything | `Tools > Parkour FPS > Clear Local Save`. To regenerate assets from scratch, delete `Assets/_Game/Prefabs`, `Materials`, `ScriptableObjects`, `Resources` and `Scenes`, then run the setup again. |
| Something else looks broken | `Tools > Parkour FPS > Validate Project` lists missing references, missing scripts, missing layers/tags/axes and scene problems with a description of each. |

---

### Credits

Design and code: the MOMENTUM project. Inspired by the momentum-shooter genre (KARLSON, Quake movement, Titanfall wallrunning). All content in this repository is original; no third-party code, models, textures or audio are included.
