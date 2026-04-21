# Existing Game Changes — Hackathon Summary

## Starting Point

The project began on **April 7, 2026** with an initial commit containing a basic RTS framework: core unit logic (`InsectUnit`), selection/command controllers, a simple production building system, fog of war, basic HUD, economy (Calories), scene flow, and raw 3D art assets from Meshy AI. Three unit archetypes existed (Worker, BasicFighter, BasicRanged) with placeholder geometry and minimal visuals. There was one dev-only test scene, no playable maps, no menu, no sound, and no AI opponent.

## Major Changes During the Hackathon

### New Units (3 added, total 6)

- **Black Widow** — melee unit with a **Web Net ability** that slows enemies in a cone (50% move speed for 8 seconds, 10-second cooldown)
- **Stick Spy** — non-combat stealth scout with 18 vision range, cloaks after standing still 5 seconds, can see over high ground; uses a special NavMesh agent for climbing
- **Giant Stag Beetle** — heavy tank (120 HP, 14 melee damage) with a **Ground Stomp AoE** every 8 seconds that slows nearby enemies

### New Buildings (3 added, total 5)

- **Sky Tower** — produces Black Widow and Stick Spy
- **Ant's Nest** — secondary worker production and colony expansion
- **Root Cellar** — supply-only depot to increase unit capacity

### Maps (2 playable maps, created from scratch)

- **Frozen Expanse** — large frozen tundra with elevated plateaus, clay barriers, ice terrain, multiple fruit nodes, and a unique music track
- **Lava Pass** — compact volcanic map with lava pools, basalt pillars, thorns, and tight chokepoints with its own music track

### Terrain Feature System (new)

Implemented 5 terrain feature types that affect gameplay:
- **Water** (movement slow + vision penalty)
- **Mud** (movement slow)
- **Thorns** (movement slow + 2 DPS)
- **Tall Grass** (concealment)
- **Rocky Ridge** (pathing + vision blocker)

### AI Opponent (new)

- **EnemyCommander** — macro-level AI that builds structures, produces units, manages economy, and launches attack waves
- **SimpleEnemyAi** — per-unit micro AI handling gather, aggro, kite, and flee behaviors
- **Difficulty system** — Easy/Normal/Hard scaling enemy HP, unit counts, AI speed, and starting resources

### Complete Art Overhaul

- All units received custom 3D models, materials, and team-colored shell/strap designs replacing primitive shapes
- All buildings received themed 3D art (Meshy AI-generated models with custom materials)
- Procedural animations: idle loops, walk cycles, attack animations, and ability-specific effects (stomp shockwave, spray VFX, web effect)
- Hand-painted UI icon sprites for all commands, units, and buildings
- Scatter decoration system: grass blades, pebbles, fallen leaves, twigs (280-950 per map)
- Map-specific art: ice/snow assets for Frozen Expanse, lava/basalt assets for Lava Pass

### UI System (rebuilt)

- **StarCraft-style bottom bar** — minimap, unit portrait/selection panel, command grid, and control groups
- **Chitin-themed UI frames** with organic insect aesthetic
- **Production queue display** with progress bars
- **Home menu** with video background, settings, difficulty/map selection, tutorial, sandbox mode, and Spotlight codex

### Audio (new)

- Per-map background music tracks
- Unit voice lines (e.g., Worker "Lift" sound)
- Home screen video with audio intro
- Sound effect integration through `GameAudio` singleton

### Tutorial & Sandbox Modes (new)

- **Tutorial mode** with a guided small map for learning basics
- **Play-Ground / Learning mode** — sandbox without enemy AI
- **Spotlight codex** — in-game encyclopedia for browsing unit and building information with 3D previews

### Gameplay Systems Refined

- Building placement with valid build zone rings
- Rally point system for buildings
- Control group hotkeys (Ctrl+1-9 / 1-9)
- Nest evolution upgrade
- Unit supply cap with Root Cellar expansion
- Fog of war with vision blocking, explored vs current visibility
- Win/lose conditions tied to Hive destruction
- Pause system
