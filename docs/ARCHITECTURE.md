# Architecture

Unity AI MCP Game Maker has three layers.

## 1. Unity Editor Package

The Unity package lives at:

```text
packages/com.alday.unity-ai-game-maker
```

It owns Unity-specific behavior:

- local HTTP listener
- token validation
- main-thread dispatch
- tool registry
- Unity Editor API calls

The package is Editor-only. It is not meant to be included in player builds.

## 2. Local Command Protocol

The first protocol is a small JSON RPC shape:

```json
{
  "tool": "scene.listOpen",
  "args": {}
}
```

Responses use:

```json
{
  "ok": true,
  "result": {}
}
```

or:

```json
{
  "ok": false,
  "error": "Message"
}
```

This stable local command layer keeps the game-maker toolchain useful even before MCP support is added.

## 3. CLI and Future MCP Adapter

The CLI starts simple:

- read project config
- call the local Unity AI MCP Game Maker server
- print JSON

The MCP adapter should sit on top of the same command protocol instead of becoming the core. This keeps the system portable across AI clients.

## Main Thread Rule

Unity Editor APIs must run on the editor main thread. The server accepts HTTP requests on a background thread, then dispatches tool execution through `EditorApplication.update`.

## Tool Categories

Initial safe tools:

- `scene.*`
- `gameObject.*`
- `component.*`
- `asset.find`

Policy-gated tools:

- `asset.delete`
- `package.remove`
- `script.execute`
- `reflection.call`
