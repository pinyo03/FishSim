# Graph Report - C:/Asztal/BME/4_felev/Net_jatekfejlesztes/FishSim  (2026-05-12)

## Corpus Check
- Corpus is ~6,212 words - fits in a single context window. You may not need a graph.

## Summary
- 116 nodes · 122 edges · 16 communities (14 shown, 2 thin omitted)
- Extraction: 79% EXTRACTED · 21% INFERRED · 0% AMBIGUOUS · INFERRED: 26 edges (avg confidence: 0.88)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- [[_COMMUNITY_Verlet Body Physics Core|Verlet Body Physics Core]]
- [[_COMMUNITY_Architecture and High-Level Design|Architecture and High-Level Design]]
- [[_COMMUNITY_Procedural Geometry|Procedural Geometry]]
- [[_COMMUNITY_Camera and Sky Rendering|Camera and Sky Rendering]]
- [[_COMMUNITY_Game Loop and Entry|Game Loop and Entry]]
- [[_COMMUNITY_Physics Simulation Pipeline|Physics Simulation Pipeline]]
- [[_COMMUNITY_Content Loading and Assets|Content Loading and Assets]]
- [[_COMMUNITY_Draw Call Hierarchy|Draw Call Hierarchy]]
- [[_COMMUNITY_Clownfish Visual Identity|Clownfish Visual Identity]]
- [[_COMMUNITY_Sky and Environment Art|Sky and Environment Art]]
- [[_COMMUNITY_Verlet Particle Dynamics|Verlet Particle Dynamics]]
- [[_COMMUNITY_Sprite Animation Assets|Sprite Animation Assets]]
- [[_COMMUNITY_Cube Geometry Builders|Cube Geometry Builders]]
- [[_COMMUNITY_Game Initialization|Game Initialization]]

## God Nodes (most connected - your core abstractions)
1. `Game1` - 12 edges
2. `BasicGeometry` - 10 edges
3. `Fish` - 9 edges
4. `Body` - 8 edges
5. `Camera` - 6 edges
6. `Fish (Playable Entity)` - 5 edges
7. `FishSim Project Overview (CLAUDE.md)` - 5 edges
8. `Sky` - 4 edges
9. `Sky.Draw` - 4 edges
10. `Clownfish Sprite` - 4 edges

## Surprising Connections (you probably didn't know these)
- `6-Particle Fish Body Layout` --conceptually_related_to--> `Fish (Playable Entity)`  [INFERRED]
  CLAUDE.md → FishSim/Content/Fish.cs
- `Fish-Follow vs Free-Cam Mode` --conceptually_related_to--> `Game1.Update`  [INFERRED]
  CLAUDE.md → FishSim/Game1.cs
- `Iterative Distance Constraint Solver` --conceptually_related_to--> `Body.ApplyConstraints`  [INFERRED]
  CLAUDE.md → FishSim/Content/Body.cs
- `Verlet Integration Physics` --conceptually_related_to--> `Verlet.Step`  [INFERRED]
  CLAUDE.md → FishSim/Content/Verlet.cs
- `Clownfish Reference Image` --semantically_similar_to--> `Clownfish Content Texture`  [INFERRED] [semantically similar]
  images/clownfish.png → FishSim/Content/clownfish.png

## Hyperedges (group relationships)
- **Verlet Physics Pipeline (Forces -> Integration -> Constraints)** — fish_ApplyForces, verlet_Step, body_ApplyConstraints [EXTRACTED 0.95]
- **Scene Render Pipeline (Camera, Sky, Fish)** — camera_Main, sky_Draw, fish_Draw [EXTRACTED 0.95]
- **Fish Particle-to-Mesh World Transform Binding** — verlet_Verlet, fish_WorldTransform, fish_Draw [INFERRED 0.85]

## Communities (16 total, 2 thin omitted)

### Community 0 - "Verlet Body Physics Core"
Cohesion: 0.13
Nodes (10): Body, bool, Body, FishSim, Fish, FishSim, int, Matrix (+2 more)

### Community 1 - "Architecture and High-Level Design"
Cohesion: 0.16
Nodes (14): BasicGeometry.CreateHalfSphere, BasicGeometry.CreateSphere, Body (Verlet Rigid Body Base), Body.GenerateFullyConnectedBody, Camera.Main (singleton), FishSim Project Overview (CLAUDE.md), Fish-Follow vs Free-Cam Mode, 6-Particle Fish Body Layout (+6 more)

### Community 2 - "Procedural Geometry"
Cohesion: 0.2
Nodes (5): BasicEffect, BasicGeometry, Game1, IndexBuffer, VertexBuffer

### Community 3 - "Camera and Sky Rendering"
Cohesion: 0.2
Nodes (7): BasicGeometry, Camera, FishSim, FishSim, Sky, float, Vector3

### Community 4 - "Game Loop and Entry"
Cohesion: 0.18
Nodes (7): Fish, Game1, Game, GraphicsDeviceManager, List, Sky, SpriteBatch

### Community 5 - "Physics Simulation Pipeline"
Cohesion: 0.2
Nodes (10): Body.ApplyConstraints, Body.ApplyLengthConstraint, Iterative Distance Constraint Solver, Verlet Integration Physics, Fish.ApplyForces, Fish.Step, Fish.WorldTransform (particle-derived matrix), Verlet.AddSqFriction (+2 more)

### Community 6 - "Content Loading and Assets"
Cohesion: 0.36
Nodes (7): Clownfish Content Texture, Clownfish Reference Image, Clownfish Images Variant (clownfish_0), Fish Class (Fish.cs), FishClown 3D Model (FBX), FishSim, Fish Sky Background Texture

### Community 7 - "Draw Call Hierarchy"
Cohesion: 0.33
Nodes (7): BasicGeometry.Draw, BasicGeometry.DrawWithoutEffect, Camera, Camera.GetReflection, Fish.Draw, Game1.Draw, Sky.Draw

### Community 8 - "Clownfish Visual Identity"
Cohesion: 0.4
Nodes (6): 2D Stylized Art Style, Clownfish (Game Entity), Clownfish Sprite, Fish Simulation Game, FishSim Content Assets, Orange and Black Coloring (Clownfish Pattern)

### Community 9 - "Sky and Environment Art"
Cohesion: 0.5
Nodes (5): FishSim Game Project, Panoramic HDRI Sky Capture, Skybox / Sky Dome Environment, Sunset / Dusk Sky Scene, Fish Sky 1 Sky Texture

### Community 11 - "Sprite Animation Assets"
Cohesion: 0.67
Nodes (4): Clownfish, FishSim Game, Clownfish Sprite Frame 0, Clownfish Sprite Sheet / Animation

### Community 12 - "Cube Geometry Builders"
Cohesion: 0.67
Nodes (3): BasicGeometry, BasicGeometry.CreateCube, BasicGeometry.CreateRoundedCube

## Knowledge Gaps
- **37 isolated node(s):** `Game1`, `VertexBuffer`, `IndexBuffer`, `BasicEffect`, `FishSim` (+32 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **2 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Game1` connect `Game Loop and Entry` to `Verlet Body Physics Core`, `Content Loading and Assets`?**
  _High betweenness centrality (0.099) - this node is a cross-community bridge._
- **Why does `bool` connect `Verlet Body Physics Core` to `Game Loop and Entry`?**
  _High betweenness centrality (0.092) - this node is a cross-community bridge._
- **Why does `Fish` connect `Verlet Body Physics Core` to `Camera and Sky Rendering`?**
  _High betweenness centrality (0.078) - this node is a cross-community bridge._
- **What connects `Game1`, `VertexBuffer`, `IndexBuffer` to the rest of the system?**
  _37 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Verlet Body Physics Core` be split into smaller, more focused modules?**
  _Cohesion score 0.13 - nodes in this community are weakly interconnected._