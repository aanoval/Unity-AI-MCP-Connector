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
- starter tools for scene, GameObject, component, asset, and transform operations
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

## First Tool Surface

The starter Unity package includes:

- `scene.listOpen`
- `scene.save`
- `gameObject.find`
- `gameObject.create`
- `gameObject.setTransform`
- `component.list`
- `asset.find`

The protocol is intentionally boring JSON:

```json
{
  "tool": "gameObject.find",
  "args": {
    "name": "Canvas"
  }
}
```

## Roadmap

Near-term:

- prefab open/save/instantiate tools
- UI Canvas mapping helpers
- screenshot tools
- console log capture
- playmode control
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
