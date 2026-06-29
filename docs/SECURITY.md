# Security Model

Unity AI MCP Game Maker is designed as a local editor automation bridge. It should never be treated as a public network service.

## Defaults

- Bind host: `127.0.0.1`
- Auth: required
- Token: generated per Unity project
- Cloud mode: not implemented
- Telemetry: not implemented
- Auto-update: not implemented
- Dynamic C# execution: disabled by design
- Private reflection method calls: disabled by design

## Threat Model

The main risk is not Unity AI MCP Game Maker itself doing something malicious. The main risk is another local process sending commands to Unity through the local server.

That is why RPC calls require a bearer token and the server binds only to loopback.

## Dangerous Tools

These capabilities must remain opt-in:

- deleting assets
- removing packages
- editing project settings
- executing Unity menu items
- compiling or running arbitrary C# snippets
- invoking arbitrary methods by reflection
- binding to anything other than loopback

The first public versions should favor safe, structured editor operations over open-ended execution.

## Token Handling

The token is stored in the Unity project under:

```text
UserSettings/UnityAiGameMaker.json
```

Do not commit `UserSettings/` to source control.

## Public Network Warning

Do not expose this local server with:

```text
0.0.0.0
```

Do not put it behind a tunnel unless a separate authentication and authorization layer has been reviewed.

## Supply Chain

This repository keeps external research projects as Git submodules under `references/`. They are references, not runtime dependencies.

Runtime dependency policy:

- keep the CLI dependency-free where possible
- prefer Unity built-in APIs
- avoid installing packages that add broad runtime behavior
- pin versions when dependencies become necessary
