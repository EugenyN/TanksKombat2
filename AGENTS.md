# AGENTS.md

Guidance for AI assistants (and humans) working in this repository.

## 1. Project Overview

- **Goal**: R&D project exploring **NetCode for Entities** in a **competitive multiplayer game with fast-paced gameplay**.
- **Genre / style**: 2D **retro pixel-art** (NES/SNES era) tank battles.
- **Language**: **English — everywhere**. Code, identifiers, comments, `Debug.Log`
  /log/error messages, UI strings, and **documentation** (`.md` files, diagrams,
  READMEs) must all be written in English.

## 2. Environment & Platforms

- **Engine**: Unity **6000.6**, URP 17.6.
- **Core packages**: Entities 6.6, NetCode 6.6, Unity Networking Transport, Unity Services Multiplayer (Relay and Direct Connection).
- **Primary platform**: **Windows Self-Hosted** (client build profile `Windows Self-Hosted.asset`).
- **Secondary platforms**: **Android** (mobile client + host via `Android Self-Hosted.asset`), **Dedicated Server** (`Windows Server.asset`).

## 3. Directory Layout

```
Assets/
  Audio/                  game SFX/music (ogg/mp3)
  Configs/                MapSizesConfig.asset, TileSetConfig.asset (shared data assets)
  Font/                   PressStart2P (retro font)
  Maps/                   CustomMapConfig assets (custom maps)
  Materials/              .mat assets + Shaders/
    Shaders/TiledAtlasUnlit.shader   custom URP Unlit sprite/tile shader
  Prefabs/                gameplay + UI prefabs (Tank, Bullet, Bonus, Wall, ...)
  Scenes/                 Frontend.unity, GameScene.unity (+ SubScene/)
  Settings/               Build Profiles/
  TextMesh Pro/           TMP Essential Resources (Settings, default font, shaders) — committed, required.
  Textures/               source textures (gameplay sprites)
    UI/                   UI textures + status_icons SpriteAtlas (+ OnScreenControls/)
  Default.globalconfig    NetCode source-generator global config (write_files_to_disk, logging, timing)
  Scripts/
    Bootstrap/            GameBootstrap, CLI utils, ClientServerBootstrap override
    Shared/               code built into BOTH client and server worlds
    Client/               client-only code (input, presentation, UI, audio, camera)
    Server/               server-only code (spawning, bots, match state, level)
    Authoring/            editor-time authoring/baking (+ Client/, Server/ subfolders)
    MapEditor/            custom map editor (EditorWindow, Editor-only assembly)
    NetCodeGen/           NetCode source-generator templates + user-defined types
```

## 4. Assembly & World Architecture

Each major folder is its own assembly via `.asmdef`. NetCode creates separate
**Client world** and **Server world** (plus a thin Local world in Frontend); code is
filtered into them by `defineConstraints`:

| Assembly                      | defineConstraints            | Purpose                                                  |
|-------------------------------|------------------------------|----------------------------------------------------------|
| `TanksKombat2.Shared`         | —                            | Shared simulation: components, systems, RPCs, helpers.   |
| `TanksKombat2.Client`         | `!UNITY_SERVER \|\| UNITY_EDITOR` | Client-only: input, presentation, UI, camera, audio. |
| `TanksKombat2.Server`         | `!UNITY_CLIENT`              | Server-only: spawn, match flow, bots, level loading.     |
| `TanksKombat2.Authoring`      | `UNITY_EDITOR`               | Editor authoring/baking components.                      |
| `TanksKombat2.Client.Authoring` | (client)                   | Client-side authoring.                                   |
| `TanksKombat2.Server.Authoring` | (server)                   | Server-side authoring.                                   |
| `TanksKombat2.MapEditor`      | (Editor platform)            | Custom map editor: `EditorWindow`, read-only inspector.  |
| `TanksKombat2.Bootstrap`      | —                            | `ClientServerBootstrap` override + CLI parsing.          |

## 5. Rendering

- All tiles & sprites use the custom URP Unlit shader **`TanksKombat2/TiledAtlasUnlit`**
  (`Assets/Materials/Shaders/TiledAtlasUnlit.shader`) with atlas-frame selection
  (`_Columns`/`_Rows`/`_Frame`) and DOTS-instancing support.
- Sprite animation is driven per-entity via the `_Frame` material property
  (`SpriteAnimationSystem`, `MaterialPropertyFrame`, `PlayAnimation`).
- Orthographic camera with fixed logical resolution **864×480 (16:9)** and
  **32 units per tile**, enforced by `FixedAspectCamera`.

## 6. ECS & NetCode Conventions

**Two system types coexist — pick the right one:**

- **`SystemBase`** (`partial class`) — use **only** when the system deals with
  **managed code**: it holds a managed field/reference, or it is meant to be consumed
  as a service via `World.GetExistingSystemManaged<>` (e.g. UI/view systems, audio, camera).
- **`ISystem`** (`partial struct`, `[BurstCompile]`, `IJobEntity`) — use in **all other
  cases**: unmanaged high-frequency simulation & presentation effects (movement, bullets,
  hits, animation, spawn).

If in doubt, default to `ISystem`; reach for `SystemBase` only when managed data or the
service pattern forces it.

**Managed references in components — use `UnityObjectRef<T>`, not managed components:**
- Do **not** create class-based (`IComponentData`) components, do **not** use
  `SystemAPI.ManagedAPI` / `AddComponentObject` to pass Unity objects into systems.
- Instead, store a `UnityObjectRef<T>` field inside a normal unmanaged `struct : IComponentData`
  (see `ServerMapSizesComponent`, `AudioSettingsData`), fill it in a Baker via implicit
  conversion (`Config = authoring.MapSizesConfig`), and dereference it in systems via
  `UnityObjectRef<T>.Value` (e.g. `SystemAPI.GetSingleton<ServerMapSizesComponent>().Config.Value`,
  see `ServerLevelLoadSystem`).

**Naming:**
- Systems → `*System`, components → `*Component`, tags → `*Tag` / `*Tag`.
- RPCs / commands / events live in `Shared/Gameplay/Commands` and `Shared/Gameplay/Components`.

**Comments:**
- Every system and component **must** be preceded by a comment (`//`) describing
  what it does. Keep it to 1–2 lines; note the responsibility/owner (server authority,
  client presentation, shared prediction, etc.) where it isn't obvious.
- Comments and `Debug.Log`/error messages **must be in English**

**Update groups:**
- Shared, predicted gameplay systems declare `[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]`
  (see `TankMovementSystem`).

**Networking:**
- Networked state goes through NetCode: **ghosts**, **RPCs**, `IGhostInput`, `ICommandData`.
- Ghost/RPC scaffolding is produced by the **NetCode source generator** from templates in
  `Assets/Scripts/NetCodeGen/Templates/*.NetCodeSourceGenerator.additionalfile` —
  **do not hand-edit** those `.additionalfile`s or generated output; change the model/types instead.
- User-defined ghost sub-types/templates live in `NetCodeGen/UserDefined/`.

**Helpers** (re-use, don't reinvent): `LevelHelper`, `CollisionWorld`, `GameUtils`,
`PlayerScoreHelper` — all in `Shared/Gameplay/Helpers`.

## 7. Network Architecture

- **Model**: authoritative server with **client-side prediction**.
  - Clients send **input** to the server and continuously receive a **world snapshot**;
    on receipt they perform **world reconciliation** (rollback/replay as needed).
  - Clients and server also exchange **RPCs** for discrete events (join, ready, shoot,
    hit, bonus, disconnect — see `Shared/Gameplay/Commands`).
- **Server topologies supported**: both **self-hosted** (a player hosts client + server
  in one process) and **dedicated server** (`Windows Server` build profile).
- **Host world modes**: the self-host default is **binary worlds** (`ClientWorld` +
  `ServerWorld` joined over IPC). An experimental **single-world host** exists (opt-in:
  `NETCODE_EXPERIMENTAL_SINGLE_WORLD_HOST` scripting define + `NetCodeConfig.HostWorldModeSelection
  = SingleWorld`): one `HostWorld`, a fake `LocalConnection` for the host player, no IPC
  self-connect and no approval round trip for the host (see `Docs/connection-flow.md`).
  Disabled by default; client code already keys off the `LocalConnection` singleton, so both
  modes work.
- **Connection types** (selected at runtime via `GameSettings.ConnectionMode`):
  1. **Direct IP** — connect to a host by IP address + port (default port **7979**).
  2. **Unity Relay** — routed through **Unity Multiplayer Services** + **Unity Relay**
     (`ConnectionManager`, `CustomDriverConstructor`).
- **Host migration** is *supported by NetCode for Entities* but **not yet implemented** in
  the current game version. The integration scaffolding exists (`HostMigrationSystem`,
  `RegisterMigrationTypesSystem`) but the feature is disabled / unfinished — do not assume it
  works.

## 8. Scenes & Bootstrap

The project has **two entry scenes, both launchable from the Editor Play button:**

- **`Assets/Scenes/Frontend.unity`** — main menu flow. `GameBootstrap` detects the
  `Frontend` scene name and creates **only the Local world**; connection is started manually
  from the menu (`ConnectionManager`).
- **`Assets/Scenes/GameScene.unity`** (+ `SubScene/GameEntityScene.unity` for baking) —
  quick-start: bootstrap runs `CreateDefaultClientServerWorlds()` with **auto-connect on port 7979**.
  With the experimental single-world host enabled (see §7) this creates one `HostWorld` and
  skips the loopback self-connect; thin clients still auto-connect as usual.

Runtime behavior is dispatched in `GameBootstrap.Initialize()` based on the active scene name.
The port can be overridden on a build via the `-port <n>` command-line argument (`CommandLineUtils`).

## 9. Build & Run

This document intentionally does **not** define CLI build commands — build from the Editor:

- **Client (Windows Self-Hosted)**: `File → Build Profiles → Windows Self-Hosted → Build`.
- **Dedicated Server (Windows)**: Build Profile `Windows Server`.
- **Android**: add/select an Android Build Profile and build from there.
- **Thin client** (for testing): PlayMode Tools → enable thin client mode.
- **In-Editor play**: open `Frontend.unity` (menu flow) or `GameScene.unity` (auto-connect quick-start) and press Play.


## 10. Git Conventions

- Single working branch: **`main`**. Direct commits to `main` are fine.
- Commit messages: free-form (e.g. `Tanks WIP`, `PlayerReady logic, build configs`).
- No required AI footer or sign-off.

## 11. Do / Don't for AI

**Don't:**
- Touch generated/cache folders: `Library/`, `Temp/`, `Obj/`, `UserSettings/`, `SceneDependencyCache/`.
- Introduce `UnityEngine` singletons for networked state — use ghosts/RPCs.
- Add tests, CI, or CLI build scripts unless explicitly asked.

**Do:**
- Precede every system and component with a `//` comment (see §6).
- Register new systems in the correct `UpdateInGroup` (shared sim → `PredictedSimulationSystemGroup`).
- Use `ISystem` + Burst for new high-frequency simulation; `SystemBase` for UI/orchestration.
- Drive level/collision lookups through `LevelHelper` / `CollisionWorld`.
