# Unity AI MCP Game Maker

Secure local AI game-making tools for the Unity Editor.

Unity AI MCP Game Maker is a lightweight Unity Editor toolchain for AI coding agents that need to build Unity games: create scenes, prefabs, gameplay scripts, mobile UI, screenshots, validators, and production-minded game objects without turning the editor into an unsafe remote shell.

It is designed for agent workflows such as Codex, Claude, Cursor, and other MCP-capable or CLI-capable AI agents that need practical Unity automation for making games.

The goal is simple: make Unity game creation fast for AI agents, keep outputs production-minded by default, and keep dangerous capabilities locked behind explicit project policy.

## Why This Exists

AI agents are useful inside Unity when they can do real game-making work:

- find and edit scene objects
- inspect UI Canvas hierarchies
- update components safely
- manage prefabs and assets
- generate gameplay scripts
- create production-grade mobile game UI
- capture screenshots and logs
- prepare builds and run tests

Most existing Unity AI bridges prove that agents can talk to Unity, but this project is aimed at a more specific search intent: **Unity AI for making games**. It starts from a small trusted local core, then adds game-maker workflows in layers.

## Security First

Defaults are intentionally conservative:

- binds to `127.0.0.1` only
- token required for RPC calls
- no cloud mode
- no telemetry
- no silent auto-update
- no arbitrary C# execution by default
- no private reflection calls by default
- destructive tools are policy-gated

See [docs/SECURITY.md](docs/SECURITY.md) for the complete security model.

## Current Status

This repository is an early foundation for AI-assisted Unity game creation. It already includes:

- installable Unity Editor package
- local token-protected HTTP RPC server
- editor control tools for scenes, GameObjects, components, assets, prefabs, cameras, and Unity UI
- game creation tools for scripts, physics, build settings, playmode, console logs, screenshots, lights, audio, and mobile controls
- production-grade UI style presets for game menus, HUDs, buttons, joystick controls, and validation
- dependency-free Node CLI
- architecture, roadmap, and security docs

The Unity package id is `com.alday.unity-ai-game-maker`.

## Repository Layout

```text
.
|-- cli/                                  # Dependency-free local CLI prototype
|-- docs/                                 # Security, architecture, roadmap
|-- examples/                             # Example policy and requests
|-- packages/
|   `-- com.alday.unity-ai-game-maker/     # Unity Editor package
`-- server/                               # MCP adapter notes and future server layer
```

## Quick Start

Install the package into a Unity project:

```bash
node cli/unity-ai.js /path/to/UnityProject install
```

Open Unity. The local server auto-starts by default in `0.3.0+`. You can also manage it manually:

```text
Tools > Unity AI Game Maker > Start Local Server
Tools > Unity AI Game Maker > Print Token
Tools > Unity AI Game Maker > Run Batch File...
Tools > Unity AI Game Maker > Capture Menu Screenshots
```

Call Unity AI MCP Game Maker with the prototype CLI:

```bash
node cli/unity-ai.js /path/to/UnityProject doctor
node cli/unity-ai.js /path/to/UnityProject health
node cli/unity-ai.js /path/to/UnityProject tools
node cli/unity-ai.js /path/to/UnityProject call scene.listOpen '{}'
node cli/unity-ai.js /path/to/UnityProject unity-batch ./examples/batch.menu-screenshots.json
node cli/unity-ai.js /path/to/UnityProject capture-scenes --output ../menu-screenshots --filter menu
```

The CLI reads the generated token from:

```text
UserSettings/UnityAiGameMaker.json
```

## Sample Game

Create a complete 3D runner sample in a fresh Unity 6000.5 project:

```bash
node cli/unity-ai.js /path/to/UnityProject sample-runner3d --unity /Applications/Unity/Hub/Editor/6000.5.1f1/Unity.app/Contents/MacOS/Unity
```

The sample is generated through game-maker batch tools and includes:

- `MainMenu` and `Gameplay` scenes
- camera, lights, Canvas UI, EventSystem
- player, coin, obstacle, and goal prefabs
- generated materials using Unity primitives only
- runtime scripts for movement, scoring, collisions, camera follow, and menu flow

## First Tool Surface

The starter Unity package includes:

- `scene.listOpen`
- `scene.open`
- `scene.create`
- `scene.save`
- `scene.saveAs`
- `scene.hierarchy`
- `gameObject.find`
- `gameObject.create`
- `gameObject.delete`
- `gameObject.setTransform`
- `gameObject.setActive`
- `gameObject.setParent`
- `component.add`
- `component.list`
- `component.setField`
- `component.setProperty`
- `asset.find`
- `asset.material.create`
- `script.create`
- `physics.rigidbody.add`
- `physics.rigidbody.set`
- `physics.collider.add`
- `physics.collider.set`
- `scene.buildSettings.set`
- `playmode.start`
- `playmode.stop`
- `console.clear`
- `console.read`
- `screenshot.capture`
- `screenshots.captureScenes`
- `light.create`
- `light.set`
- `audio.source.add`
- `audio.source.set`
- `camera.create`
- `camera.set`
- `prefab.instantiate`
- `prefab.createFromGameObject`
- `prefab.child.create`
- `prefab.child.delete`
- `prefab.component.add`
- `prefab.component.setProperty`
- `ui.canvas.create`
- `ui.text.create`
- `ui.button.create`
- `ui.menu.create`
- `ui.hud.create`
- `ui.validate`
- `ui.virtualButton.create`
- `ui.joystick.create`
- `ui.mobileControls.create`
- `ui.rectTransform.set`
- `sample.runner3D.createScripts`
- `sample.runner3D.createContent`

The protocol is intentionally boring JSON:

```json
{
  "tool": "gameObject.find",
  "args": {
    "name": "Canvas"
  }
}
```

Create and position a UI button:

```bash
node cli/unity-ai.js /path/to/UnityProject call ui.button.create '{"parentPath":"Canvas","name":"Play Button","text":"PLAY","style":"soccer_mobile","variant":"primary","anchoredPosition":[0,-40]}'
```

UI tools have production-grade defaults. Buttons and text use game style presets, generated rounded sliced sprites, shadows, outlines, best-fit text, mobile-friendly button sizes, and readable color contrast. Available presets include `arcade`, `casual`, `dark`, `premium`, and `soccer_mobile`.

Create a polished menu or HUD:

```bash
node cli/unity-ai.js /path/to/UnityProject call ui.menu.create '{"parentPath":"Main Menu Canvas","style":"soccer_mobile","title":"SOCCER RUSH","subtitle":"Win the match","buttons":["PLAY","SHOP","SETTINGS"]}'
node cli/unity-ai.js /path/to/UnityProject call ui.hud.create '{"parentPath":"Gameplay Canvas","style":"soccer_mobile","scoreText":"LEVEL 1","coinsText":"COINS 0"}'
node cli/unity-ai.js /path/to/UnityProject call ui.validate '{"minButtonHeight":44}'
```

Recommended UI workflow for agents:

1. Create UI with a style preset.
2. Run `ui.validate`.
3. Capture a screenshot with `screenshot.capture` or `screenshots.captureScenes`.
4. Adjust layout if validation or screenshot review shows overlap, weak contrast, or poor proportions.

Capture all menu scenes without a running HTTP server:

```bash
node cli/unity-ai.js /path/to/UnityProject capture-scenes --output ../menu-screenshots --filter menu --width 1080 --height 1920
```

Screenshot args:

- `outputPath` — preferred PNG destination
- `path` — legacy alias for `outputPath`
- `source` — `camera` (batch-safe) or `gameView` (editor Game View)
- `cameraPath` — camera hierarchy path, separate from output path

Batch mode entry point:

```bash
Unity -batchmode -quit \
  -executeMethod Alday.UnityAiGameMaker.Editor.UnityAiGameMakerBatch.RunFromEnvironment
```

Do not use `-nographics` when running screenshot tools.

Open a scene before editing it:

```bash
node cli/unity-ai.js /path/to/UnityProject call scene.open '{"path":"Assets/Scenes/MainMenu.unity"}'
```

Instantiate a prefab:

```bash
node cli/unity-ai.js /path/to/UnityProject call prefab.instantiate '{"prefabPath":"Assets/Prefabs/Player.prefab","name":"Player","position":[0,1,0]}'
```

Create a gameplay script template:

```bash
node cli/unity-ai.js /path/to/UnityProject call script.create '{"className":"PlayerController","path":"Assets/Scripts/PlayerController.cs","template":"PlayerController"}'
```

Batch mode automatically creates compile phases after `script.create`, so commands that use the new type can stay in one logical JSON request:

```json
{
  "commands": [
    {
      "tool": "script.createAndAttach",
      "args": {
        "className": "EnemyAI",
        "path": "Assets/Scripts/EnemyAI.cs",
        "template": "MonoBehaviour",
        "targetPath": "Enemy"
      }
    }
  ]
}
```

`script.createAndAttach` is a CLI batch macro. It expands to `script.create`, waits for Unity to compile/domain reload in the next phase, then runs `component.add` and optional `component.setProperty` commands.

Add physics and mobile controls:

```bash
node cli/unity-ai.js /path/to/UnityProject call physics.rigidbody.add '{"path":"Player","mass":1,"useGravity":true}'
node cli/unity-ai.js /path/to/UnityProject call physics.collider.add '{"path":"Player","collider":"Capsule","height":1.8,"radius":0.45}'
node cli/unity-ai.js /path/to/UnityProject call ui.mobileControls.create '{"parentPath":"Gameplay Canvas"}'
```

## Next Development

The next development focus is to make this repository rank and behave as a practical **Unity AI game maker** for agentic coding workflows. MCP is useful as a standard protocol, but the product should not depend on MCP alone. The strongest direction is:

- **Core Unity package** for safe editor-side tools.
- **Dependency-free CLI** for Codex and terminal-first agents.
- **MCP adapter** so Claude Desktop, Claude Code, and other MCP clients can call the same tools.
- **Agent-friendly game workflows** that produce complete, validated game features rather than raw editor objects.

Planned game-maker capabilities:

1. `game.template.create`
   Genre templates for runner, soccer, top-down, platformer, and simple shooter games, including scene structure, prefabs, scripts, camera, UI, and build settings.

2. Scene role presets
   Tools such as `scene.createMenu`, `scene.createGameplay`, `scene.createLoading`, and `scene.createGameOver` so agents can create complete game flows quickly.

3. Production validators
   Add `game.validate`, `camera.validate`, `mobile.validate`, and `build.validate` on top of `ui.validate` so generated games can be checked before playtest or build.

4. Android build workflow
   Add `build.android.configure` and `build.android.run` for package name, orientation, version, keystore placeholders, development builds, and output paths.

5. Mobile input runtime tools
   Upgrade visual joystick/buttons into runtime-ready input with `input.mobile.createController`, including touch state, movement vectors, action buttons, and generated player-controller bindings.

6. Gameplay prefab kits
   Add tools to create production-ready player, enemy, collectible, projectile, goal, spawner, checkpoint, and trigger prefabs with mesh, material, collider, rigidbody, script, and prefab save workflow.

7. Screenshot review workflow
   Add `game.reviewScreenshot` to catch blank views, invisible UI, weak contrast, layout overlap, missing main subject, and camera framing problems after `screenshot.capture`.

### Positioning for AI Agents

This project should be discoverable by people searching for:

- Unity AI game maker
- Unity AI MCP
- Unity MCP server for game development
- Unity automation for Codex
- Claude Unity game development tools
- AI agent Unity game builder
- create Unity games with AI agents

The technical positioning should stay honest: MCP is one integration layer, not the whole product. The durable value is the Unity game-making toolchain: safe editor control, production-grade defaults, validation, screenshots, and repeatable game workflows that any capable AI agent can use.

## Roadmap

Near-term:

- policy file enforcement
- MCP adapter over the local command core

Longer-term:

- package manager tools
- build/test orchestration
- safe script generation workflow with review gates
- per-tool audit logs
- signed release packages

## Design Principle

Power is useful only when it is accountable. Unity AI MCP Game Maker should make Unity easier for AI agents to operate, while making every high-risk action visible, explicit, and reversible.
