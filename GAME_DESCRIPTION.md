# Insect Wars

**Genre:** Real-Time Strategy (RTS)
**Engine:** Unity 6 (URP)
**Players:** Single-player vs AI
**Platform:** WebGL / macOS / Windows

---

## Overview

Insect Wars is a real-time strategy game set at insect scale in a patch of garden soil. Pebbles are mountains, grass blades are trees, and rotting fruit is a precious resource. You command an insect colony — workers, fighters, and specialist units — to gather calories, construct buildings, and destroy the enemy hive before they destroy yours.

The game features two themed battlefields, six distinct unit types with unique abilities, four constructible buildings, terrain hazards, fog of war, and a full AI opponent with three difficulty levels.

---

## How to Play

### Starting a Game

1. Launch the game to reach the **Home Screen**.
2. Select **START MISSION** to begin a skirmish match.
3. Choose a **difficulty** (Easy, Normal, or Hard).
4. Pick a **map** — either **Frozen Expanse** (large, strategic) or **Lava Pass** (compact, aggressive).
5. The match loads and you begin with a Hive, a handful of Workers, and some starting combat units.

### Controls

| Action | Input |
|--------|-------|
| **Select unit(s)** | Left-click or box-drag |
| **Select all of same type** | Double-click a unit |
| **Move / Command** | Right-click on ground |
| **Attack-move** | Right-click on enemy unit or building |
| **Gather resources** | Right-click a rotting fruit node with Workers selected |
| **Build** | Select a Worker, use Build hotkeys (Q/W/E/R) |
| **Set rally point** | Right-click with a building selected |
| **Camera pan** | Move mouse to screen edges or use arrow keys |
| **Camera zoom** | Mouse scroll wheel |
| **Minimap navigation** | Click on the minimap |
| **Control groups** | Ctrl+1-9 to assign, 1-9 to recall |
| **Pause** | Escape |

### Economy

There is a single resource: **Calories**. Workers gather Calories from **rotting fruit nodes** scattered across the map and deposit them at the Hive or Ant Nests. Calories are spent on constructing buildings and training units.

### Buildings

Select a Worker and press the corresponding hotkey to place a building:

| Building | Hotkey | Cost | Produces |
|----------|--------|------|----------|
| **Underground** | Q | 200 | Mantis Fighter, Bombardier Beetle, Giant Stag Beetle |
| **Sky Tower** | W | 300 | Black Widow, Stick Spy |
| **Ant's Nest** | E | 400 | Workers (additional colony capacity) |
| **Root Cellar** | R | 150 | None (supply depot — increases unit cap) |

Buildings must be placed within valid build zones (green rings near the Hive and fruit nodes).

### Units

| Unit | Role | Key Traits |
|------|------|------------|
| **Worker (Ant)** | Economy & Construction | Gathers calories, builds structures |
| **Mantis Fighter** | Melee DPS | Fast, aggressive melee combatant |
| **Bombardier Beetle** | Ranged AoE | Sprays a caustic chemical cone hitting multiple enemies |
| **Black Widow** | Melee + Crowd Control | Periodic web ability that slows enemies in a cone (50% slow for 8s) |
| **Stick Spy** | Scout / Stealth | Cannot attack; massive vision range (18); cloaks after standing still 5s; can see over high ground |
| **Giant Stag Beetle** | Tank | 120 HP heavy melee; ground stomp AoE every 8s that slows nearby enemies |

### Terrain Features

The battlefield includes hazards and tactical terrain:

- **Water** — slows movement and reduces vision
- **Mud** — slows movement
- **Thorns** — slows movement and deals 2 damage per second
- **Tall Grass** — provides concealment (enemies cannot see your units unless very close)
- **Rocky Ridge** — blocks pathing and vision

### Fog of War

Unexplored areas are hidden. Your units reveal terrain as they move. Previously explored areas remain visible on the minimap but enemy movements are only visible within your units' current line of sight.

### Victory & Defeat

- **Win** by destroying the enemy Hive.
- **Lose** if your Hive is destroyed.

### Difficulty Levels

| Setting | Easy | Normal | Hard |
|---------|------|--------|------|
| Enemy HP | 75% | 100% | 135% |
| Enemy starting units | 85% | 100% | 125% |
| AI reaction speed | Slower | Normal | Faster |
| Player starting calories | 115% | 100% | 90% |

### Maps

**Frozen Expanse** — A large, frozen tundra battlefield with elevated plateaus, icy terrain, clay barriers, and multiple resource nodes. Rewards strategic positioning and long-game macro play.

**Lava Pass** — A compact volcanic map with lava pools, basalt pillars, and tight chokepoints. Forces early aggression and fast-paced skirmishes.

### Tips

- Always keep Workers gathering. Economy wins games.
- Use the Stick Spy's stealth and vision to scout the enemy base before attacking.
- Bombardier Beetles are devastating in groups — their cone spray stacks.
- The Black Widow's web slow is powerful for kiting and controlling enemy pushes.
- Position your army on high ground when possible for vision advantage.
- Set rally points on your buildings to direct freshly trained units toward the front line.

---

## Additional Modes

- **Tutorial** — A guided introduction on a small map to learn the basics.
- **Play-Ground** — A sandbox mode for experimenting with units and mechanics without enemy pressure. Includes the Spotlight codex for browsing unit and building information.

---

## Technical Details

- Built with **Unity 6** and **Universal Render Pipeline (URP)**
- Procedural map generation from data-driven `MapDefinition` presets
- Runtime NavMesh baking for pathfinding
- Full AI opponent (`EnemyCommander`) with macro strategy and per-unit micro AI
- Semi-stylized organic art style at insect scale
- Custom soundtrack per map
