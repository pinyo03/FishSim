# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

FishSim is a 3D fish swimming simulation written in **C# / MonoGame 3.8 (WindowsDX)** targeting **.NET 8.0 on Windows**. It is a BME academic project demonstrating verlet-integration physics with constraint-based rigid bodies.

## Build & Run

```powershell
# From repo root
dotnet build                     # Debug build
dotnet build -c Release          # Release build
dotnet run --project FishSim     # Run (debug)
```

The MonoGame Content Pipeline (MGCB) compiles assets automatically on build. Compiled `.xnb` files land in `FishSim/Content/bin/`.

There are no automated tests in this project.

## Architecture

### System layout

```
Game1 (MonoGame entry point)
├── Camera          – singleton; View/Projection matrices; reflection math
├── Sky             – procedural half-sphere skybox, follows camera
└── Fish : Body     – playable entity
```

### Physics stack (`Body` → `Verlet`)

`Body.cs` is the base for all verlet rigid bodies. It holds an array of `Verlet` particles and a fully-connected distance constraint network. Each frame:
1. External forces are accumulated on each `Verlet` (gravity, buoyancy spring at y=0, quadratic water drag).
2. Verlet position integration runs (`pos += pos - prevPos + force * dt²`).
3. Constraint solver iterates to restore distances between all particle pairs.

`Fish` uses 6 particles: indices 0–3 define the body quad corners, index 4 is the dorsal fin, index 5 is the tail (receives WASD thrust forces). The FBX mesh is transformed each frame using a `WorldTransform` matrix derived from the particle positions.

### Camera modes

`Tab` toggles between fish-follow mode (camera tracks Fish position/orientation) and free-cam mode (WASD + QE + Shift for speed).

### Content pipeline

Assets declared in `FishSim/Content/Content.mgcb`. Key assets:
- `fishClown.fbx` – playable clownfish model
- `fishsky1.jpg` – skybox texture

## Knowledge Graph (RAG)

A graphify knowledge graph of this codebase lives in `graphify-out/`. Use it to answer structural questions before reading source files.

**Query the graph:**
```powershell
# BFS — broad context ("what is X connected to?")
/graphify query "your question here"

# DFS — trace a specific path ("how does X reach Y?")
/graphify query "your question here" --dfs

# Shortest path between two concepts
/graphify path "Fish" "Camera"

# Full explanation of one node
/graphify explain "Body"
```

**Keep it fresh** — after adding or modifying files, run:
```powershell
/graphify . --update
```

Key graph facts (116 nodes, 122 edges, 16 communities):
- **God nodes** (highest connectivity): `Game1` (12 edges), `BasicGeometry` (10), `Fish` (9), `Body` (8), `Camera` (6)
- **Physics community** (`Body`, `Verlet`, `Fish.Step`, `ApplyConstraints`, `ApplyForces`) is the core simulation cluster
- **Draw hierarchy** (`Game1.Draw` → `Sky.Draw` + `Fish.Draw` + `BasicGeometry.Draw`) forms its own cluster
- `Fish` is the single bridge between the physics pipeline and the rendering/camera systems

## Key Files

| File | Role |
|---|---|
| `FishSim/Game1.cs` | Main loop; input dispatch; scene orchestration |
| `FishSim/Fish.cs` | Fish entity: 6-particle setup, control forces, mesh rendering |
| `FishSim/Body.cs` | Verlet body base: constraint generation & iterative solving |
| `FishSim/Verlet.cs` | Physics particle: position integration, friction accumulation |
| `FishSim/Camera.cs` | Singleton camera; View/Projection; follow & free-cam logic |
| `FishSim/Sky.cs` | Skybox: procedural half-sphere, textured with BasicEffect |
| `FishSim/BasicGeometry.cs` | Procedural mesh builders (cube, sphere, half-sphere) |
| `FishSim/Content/Content.mgcb` | MGCB asset declarations |
