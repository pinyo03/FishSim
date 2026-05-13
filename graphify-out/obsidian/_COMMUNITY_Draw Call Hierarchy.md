---
type: community
cohesion: 0.33
members: 7
---

# Draw Call Hierarchy

**Cohesion:** 0.33 - loosely connected
**Members:** 7 nodes

## Members
- [[BasicGeometry.Draw]] - code - FishSim/BasicGeometry.cs
- [[BasicGeometry.DrawWithoutEffect]] - code - FishSim/BasicGeometry.cs
- [[Camera_1]] - code - FishSim/Camera.cs
- [[Camera.GetReflection]] - code - FishSim/Camera.cs
- [[Fish.Draw]] - code - FishSim/Content/Fish.cs
- [[Game1.Draw]] - code - FishSim/Game1.cs
- [[Sky.Draw]] - code - FishSim/Sky.cs

## Live Query (requires Dataview plugin)

```dataview
TABLE source_file, type FROM #community/Draw_Call_Hierarchy
SORT file.name ASC
```

## Connections to other communities
- 1 edge to [[_COMMUNITY_Physics Simulation Pipeline]]

## Top bridge nodes
- [[Fish.Draw]] - degree 3, connects to 1 community