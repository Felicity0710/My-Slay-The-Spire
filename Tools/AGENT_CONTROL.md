# Agent Control

This repo now exposes two layers for external control:

1. The game process listens on `127.0.0.1:47077`.
2. `Tools/SlayHs.Agent` can run as either:
   - an MCP server over stdio
   - a simple rule-based bot

## Requirements

- Run the Godot game first so the in-game bridge is available
- Install the .NET 9 SDK for `Tools/SlayHs.Agent`

The main game project itself targets .NET 8 on desktop, but the standalone agent project targets `.NET 9`.

## Start The Game

Run the Godot project first so the in-game bridge is available.

## MCP Sidecar

```powershell
dotnet run --project Tools/SlayHs.Agent -- mcp
```

Or use the wrapper:

```powershell
powershell -ExecutionPolicy Bypass -File Tools\run-agent.ps1 -Mode mcp
```

If your environment cannot read the default user-level `NuGet.Config`, build with:

```powershell
powershell -ExecutionPolicy Bypass -File Tools\build-agent.ps1
```

Exposed MCP tools:

- `get_game_snapshot`
- `execute_game_action`

## Simple Bot

```powershell
dotnet run --project Tools/SlayHs.Agent -- bot
```

Or use the wrapper:

```powershell
powershell -ExecutionPolicy Bypass -File Tools\run-agent.ps1 -Mode bot
```

The bot uses the exported legal game state and chooses actions with simple heuristics.

## Direct TCP Protocol

One request per TCP connection, one line of JSON per request.

Example:

```json
{"command":"get_snapshot"}
```

```json
{"command":"execute_action","expectedStateVersion":3,"action":{"kind":"play_card","handIndex":0,"targetEnemyIndex":0}}
```
