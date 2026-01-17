# 🏰 Level Generation V4 - Multi-Room PCG System

<p align="center">
  <img src="https://img.shields.io/badge/Unity-2022.3+-blue?logo=unity" alt="Unity Version">
  <img src="https://img.shields.io/badge/License-MIT-green" alt="License">
  <img src="https://img.shields.io/badge/Status-Completed-brightgreen" alt="Status">
</p>

A **rule-based procedural content generation (PCG) system** for Unity that creates multi-room dungeon layouts with natural cave terrain. Built with a modular architecture supporting async generation and hot-swappable rules.

---

## ✨ Features

| Feature | Description |
|---------|-------------|
| 🧩 **Rule Pipeline** | Modular `IGeneratorRule` interface for pluggable generation rules |
| 📋 **Blackboard Pattern** | `DungeonContext` enables data sharing between rules |
| ⚡ **Async Generation** | UniTask-powered async execution with cancellation support |
| 🗺️ **Macro-Micro Architecture** | Separate room layout (macro) and terrain detail (micro) layers |
| 🎨 **Multi-Theme Support** | Configurable tile themes (Blue, Red, Yellow) |
| 🔧 **Editor Integration** | Odin Inspector for visual configuration |

---

## 🎮 Demo

### Generated Dungeon Example
```
┌──────────┐     ┌──────────┐
│  START   │─────│  ROOM 2  │
│ (Entry)  │     │          │
└────┬─────┘     └────┬─────┘
     │                │
┌────┴─────┐     ┌────┴─────┐
│  ROOM 3  │─────│  ROOM 4  │
│          │     │          │
└────┬─────┘     └──────────┘
     │
┌────┴─────┐
│   END    │
│  (Exit)  │
└──────────┘
```

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    DungeonGenerator                         │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              DungeonPipelineData (SO)                │   │
│  │  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐   │   │
│  │  │ Rule 1  │→│ Rule 2  │→│ Rule 3  │→│ Rule N  │   │   │
│  │  └─────────┘ └─────────┘ └─────────┘ └─────────┘   │   │
│  └─────────────────────────────────────────────────────┘   │
│                           ↓                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                 DungeonContext                       │   │
│  │  ┌──────────────┐  ┌──────────────┐                 │   │
│  │  │  Macro Data  │  │  Micro Data  │                 │   │
│  │  │  RoomNodes   │  │  TileData    │                 │   │
│  │  └──────────────┘  └──────────────┘                 │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

---

## 📁 Project Structure

```
LevelGenerationV4/
├── Core/                          # Core framework
│   ├── DungeonGenerator.cs        # Main generator executor
│   ├── DungeonContext.cs          # Data blackboard
│   └── DungeonPipelineData.cs     # Pipeline configuration SO
├── Rules/
│   ├── Abstractions/              # Interfaces & base classes
│   │   ├── IGeneratorRule.cs
│   │   └── GeneratorRuleBase.cs
│   ├── Macro/                     # Room layout rules
│   │   ├── ConstrainedLayoutRule.cs   # Drunkard walk algorithm
│   │   └── BFSValidationRule.cs       # Connectivity validation
│   ├── Micro/                     # Terrain generation rules
│   │   ├── CellularAutomataRule.cs    # Cave terrain (CA)
│   │   ├── EntranceExitRule.cs        # Entry/exit carving
│   │   ├── PathValidationRule.cs      # 2x2 player pathfinding
│   │   └── PlatformRule.cs            # Platform generation
│   └── Rendering/                 # Tilemap rendering rules
│       ├── RoomRenderRule.cs
│       ├── WallRenderRule.cs
│       ├── GroundRenderRule.cs
│       └── PlatformRenderRule.cs
├── Data/                          # Data structures
│   ├── RoomNode.cs
│   ├── TileConfig.cs
│   └── TilemapLayer.cs
└── Editor/                        # Editor extensions
```

---

## 🔧 Rule Execution Order

| Order | Rule | Type | Description |
|-------|------|------|-------------|
| 10 | `ConstrainedLayoutRule` | Macro | Drunkard walk room layout |
| 20 | `BFSValidationRule` | Macro | Connectivity & critical path |
| 30 | `CellularAutomataRule` | Micro | Cave terrain generation |
| 35 | `EntranceExitRule` | Micro | Carve entry/exit areas |
| 36 | `PathValidationRule` | Micro | 2x2 player path validation |
| 40 | `PlatformRule` | Micro | Air column platform sampling |
| 100 | `RoomRenderRule` | Render | Background layer |
| 105 | `WallRenderRule` | Render | Wall borders |
| 110 | `GroundRenderRule` | Render | Ground tiles |
| 120 | `PlatformRenderRule` | Render | Platform tiles |

---

## 🧮 Core Algorithms

### Drunkard Walk (Room Layout)
```csharp
// Weighted random walk with downward bias
Direction = Random.value < DownwardBias ? Down : Random.Side;
```

### Cellular Automata (Terrain)
```csharp
// Conway's Game of Life variant
if (neighbors >= BirthLimit) → Solid
if (neighbors < DeathLimit) → Empty
```

### Air Column Sampling (Platforms)
```csharp
// Vertical scan for continuous air gaps
if (airCount >= SafeHeight && airCount % Interval == 0)
    → Place platform
```

---

## 📦 Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Unity | 2022.3+ | Game engine |
| [UniTask](https://github.com/Cysharp/UniTask) | 2.5.10+ | Async/await support |
| [Odin Inspector](https://odininspector.com/) | 3.0+ | Editor UI |

---

## 🚀 Quick Start

### 1. Create Pipeline Asset
```
Right-click → Create → Dungeon → Pipeline Data
```

### 2. Configure Rules
Add rules in the Inspector and adjust parameters.

### 3. Setup Scene
```csharp
// Add DungeonGenerator component to a GameObject
// Assign PipelineData and Tilemaps
```

### 4. Generate
```csharp
var generator = GetComponent<DungeonGenerator>();
bool success = await generator.GenerateDungeonAsync(seed);
```

---

## 📖 API Reference

### DungeonGenerator
```csharp
// Generate dungeon with optional seed
public async UniTask<bool> GenerateDungeonAsync(int seed = -1)

// Cancel current generation
public void CancelGeneration()
```

### DungeonContext
```csharp
// Tile access
public int GetTile(TilemapLayer layer, int x, int y)
public void SetTile(TilemapLayer layer, int x, int y, int value)

// Room data
public List<RoomNode> RoomNodes { get; }
public Vector2Int StartRoom { get; }
public Vector2Int EndRoom { get; }
```

### Custom Rule
```csharp
[Serializable]
public class MyRule : GeneratorRuleBase
{
    public MyRule()
    {
        _ruleName = "MyRule";
        _executionOrder = 50;
    }

    public override async UniTask<bool> ExecuteAsync(
        DungeonContext context, 
        CancellationToken token)
    {
        // Your generation logic here
        return true;
    }
}
```

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

---

<p align="center">
  Made with ❤️ for procedural generation enthusiasts
</p>
