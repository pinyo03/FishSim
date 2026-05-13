---
type: community
cohesion: 0.16
members: 14
---

# Architecture and High-Level Design

**Cohesion:** 0.16 - loosely connected
**Members:** 14 nodes

## Members
- [[6-Particle Fish Body Layout]] - rationale - CLAUDE.md
- [[BasicGeometry.CreateHalfSphere]] - code - FishSim/BasicGeometry.cs
- [[BasicGeometry.CreateSphere]] - code - FishSim/BasicGeometry.cs
- [[Body (Verlet Rigid Body Base)]] - code - FishSim/Content/Body.cs
- [[Body.GenerateFullyConnectedBody]] - code - FishSim/Content/Body.cs
- [[Camera.Main (singleton)]] - code - FishSim/Camera.cs
- [[Fish (Playable Entity)]] - code - FishSim/Content/Fish.cs
- [[Fish-Follow vs Free-Cam Mode]] - rationale - CLAUDE.md
- [[FishSim Project Overview (CLAUDE.md)]] - document - CLAUDE.md
- [[Game1 (MonoGame Entry Point)]] - code - FishSim/Game1.cs
- [[Game1.LoadContent]] - code - FishSim/Game1.cs
- [[Game1.Update]] - code - FishSim/Game1.cs
- [[Program (Entry Point)]] - code - FishSim/Program.cs
- [[Sky (Skybox)]] - code - FishSim/Sky.cs

## Live Query (requires Dataview plugin)

```dataview
TABLE source_file, type FROM #community/Architecture_and_High-Level_Design
SORT file.name ASC
```

## Connections to other communities
- 2 edges to [[_COMMUNITY_Physics Simulation Pipeline]]

## Top bridge nodes
- [[FishSim Project Overview (CLAUDE.md)]] - degree 5, connects to 1 community