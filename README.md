# Unity AI MCP Connector

Secure local AI control for the Unity Editor.

Unity AI MCP Connector is a lightweight Unity Editor bridge designed for AI coding agents that need to inspect and edit Unity projects without turning the editor into an unsafe remote shell.

The goal is simple: keep the workflow fast, expand toward full asset and scene coverage, and keep dangerous capabilities locked behind explicit project policy.

## Why This Exists

AI agents are useful inside Unity when they can do real editor work:

- find and edit scene objects
- inspect UI Canvas hierarchies
- update components safely
- manage prefabs and assets
- capture screenshots and logs
- prepare builds and run tests

Most existing connectors prove the idea, but they often choose breadth before safety. This project starts from the opposite direction: a small trusted local core, then broader tools in layers.

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

This repository is an early foundation. It already includes:

- installable Unity Editor package
- local token-protected HTTP RPC server
- editor control tools for scenes, GameObjects, components, assets, prefabs, cameras, and Unity UI
- game creation tools for scripts, physics, build settings, playmode, console logs, screenshots, lights, audio, and mobile controls
- dependency-free Node CLI
- architecture, roadmap, and security docs

## Repository Layout

```text
.
|-- cli/                                  # Dependency-free local CLI prototype
|-- docs/                                 # Security, architecture, roadmap
|-- examples/                             # Example policy and requests
|-- packages/
|   `-- com.alday.unity-ai-connector/     # Unity Editor package
`-- server/                               # MCP adapter notes and future server layer
```

## Quick Start

Install the package into a Unity project:

```bash
node cli/unity-ai.js /path/to/UnityProject install
```

Open Unity, then use:

```text
Tools > Unity AI Connector > Start Local Server
Tools > Unity AI Connector > Print Token
```

Call the connector with the prototype CLI:

```bash
node cli/unity-ai.js /path/to/UnityProject doctor
node cli/unity-ai.js /path/to/UnityProject health
node cli/unity-ai.js /path/to/UnityProject tools
node cli/unity-ai.js /path/to/UnityProject call scene.listOpen '{}'
```

The CLI reads the generated token from:

```text
UserSettings/UnityAiConnector.json
```

## Sample Game

Create a complete 3D runner sample in a fresh Unity 6000.5 project:

```bash
node cli/unity-ai.js /path/to/UnityProject sample-runner3d --unity /Applications/Unity/Hub/Editor/6000.5.1f1/Unity.app/Contents/MacOS/Unity
```

The sample is generated through connector batch tools and includes:

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
3. Capture a screenshot with `screenshot.capture`.
4. Adjust layout if validation or screenshot review shows overlap, weak contrast, or poor proportions.

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

Power is useful only when it is accountable. This connector should make Unity easier for AI agents to operate, while making every high-risk action visible, explicit, and reversible.
