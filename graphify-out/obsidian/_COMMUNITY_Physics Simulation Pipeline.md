---
type: community
cohesion: 0.20
members: 10
---

# Physics Simulation Pipeline

**Cohesion:** 0.20 - loosely connected
**Members:** 10 nodes

## Members
- [[Body.ApplyConstraints]] - code - FishSim/Content/Body.cs
- [[Body.ApplyLengthConstraint]] - code - FishSim/Content/Body.cs
- [[Fish.ApplyForces]] - code - FishSim/Content/Fish.cs
- [[Fish.Step]] - code - FishSim/Content/Fish.cs
- [[Fish.WorldTransform (particle-derived matrix)]] - code - FishSim/Content/Fish.cs
- [[Iterative Distance Constraint Solver]] - rationale - CLAUDE.md
- [[Verlet (Physics Particle)]] - code - FishSim/Content/Verlet.cs
- [[Verlet Integration Physics]] - rationale - CLAUDE.md
- [[Verlet.AddSqFriction]] - code - FishSim/Content/Verlet.cs
- [[Verlet.Step]] - code - FishSim/Content/Verlet.cs

## Live Query (requires Dataview plugin)

```dataview
TABLE source_file, type FROM #community/Physics_Simulation_Pipeline
SORT file.name ASC
```

## Connections to other communities
- 2 edges to [[_COMMUNITY_Architecture and High-Level Design]]
- 1 edge to [[_COMMUNITY_Draw Call Hierarchy]]

## Top bridge nodes
- [[Iterative Distance Constraint Solver]] - degree 2, connects to 1 community
- [[Verlet Integration Physics]] - degree 2, connects to 1 community
- [[Fish.WorldTransform (particle-derived matrix)]] - degree 2, connects to 1 community