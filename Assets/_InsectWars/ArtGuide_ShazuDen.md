# Shazu Den — Art & Technical Specification

## Design Vision

**Shazu Den** is a 2-player skirmish map inspired by StarCraft 2's *King Sejong Station*, reimagined at insect scale in a **winter garden** setting. The map takes place on a patch of frozen garden soil in late winter — the ground is hard and pale, frost crystals cling to pebbles, vegetation is dead and brown, and frozen dewdrops form impassable ice formations. Despite the cold, this is still the real world of ants: organic, earthy, and grounded in nature.

**Core aesthetic:** Frozen earth, not fantasy ice. Think of what a garden looks like in February at 5 AM — frost on every surface, pale grey-brown soil, skeletal twigs, and frozen moisture. No magical glowing ice; just cold, muted, natural winter.

---

## Map Layout (King Sejong Station Adaptation)

The map uses diagonal symmetry (NW ↔ SE) on a 240×240 unit play area (`mapHalfExtent = 120`).

### Position Reference (Clock Positions)

| Location | Clock Position | World Coordinates (approx) | Elevation |
|----------|---------------|---------------------------|-----------|
| Player Main Base | 11 o'clock (NW) | (-80, 80) | High ground (plateau) |
| Enemy Main Base | 5 o'clock (SE) | (80, -80) | High ground (plateau) |
| Player Natural Expansion | 9 o'clock (W) | (-55, 30) | Mid ground (smaller plateau) |
| Enemy Natural Expansion | 3 o'clock (E) | (55, -30) | Mid ground (smaller plateau) |
| Player Pocket Third | Behind natural (NW inner) | (-70, 50) | High ground (connected to main) |
| Enemy Pocket Third | Behind natural (SE inner) | (70, -50) | High ground (connected to main) |
| Gold Base Alpha | 3 o'clock perimeter | (95, 40) | Low ground (basin) |
| Gold Base Beta | 9 o'clock perimeter | (-95, -40) | Low ground (basin) |
| Map Center | — | (0, 0) | Low ground |

### Elevation Design

The map has **three elevation tiers**, emulating King Sejong Station's high-ground advantage system:

1. **Tier 3 — Main Base Plateaus (highest):** Large circular plateaus at NW and SE corners. Gentle ramps face inward toward the map center. These are the safest positions.
2. **Tier 2 — Natural & Third Plateaus (mid):** Smaller plateaus adjacent to the main. The natural sits slightly lower than the main, connected by a wide ramp. The pocket third is at main-base elevation but separated by a chitin barrier (destructible).
3. **Tier 1 — Valley Floor (lowest):** The central corridor and gold base basins. Open, dangerous, contestable.

**Ramp Art Note:** Ramps should show exposed frozen soil layers — imagine a cross-section of frozen earth with visible root fragments and compacted dirt. Lighter frost color at the top transitioning to darker exposed soil at the bottom.

---

## Terrain — Ground Surfaces

### Primary Terrain Layer: Frozen Soil Base

The dominant ground texture. Hard-packed winter earth with a thin frost glaze.

| Property | Value |
|----------|-------|
| **Terrain Layer Asset** | `DenFrozen_Layer.terrainlayer` (exists) or `FrozenInsect_Layer.terrainlayer` |
| **Base Color** | Pale grey-brown `(0.55, 0.50, 0.45)` — `#8C8073` |
| **Frost Highlights** | Cool white-blue tinge in crevices `(0.72, 0.75, 0.80)` — `#B8BFCC` |
| **Smoothness** | 0.25–0.35 (slightly glossy from frost, but not wet-looking) |
| **Tiling** | 15–20 m per repeat (must tile seamlessly across 240 m) |
| **Normal Map** | Subtle cracked-earth pattern with fine frost veining |
| **Feel** | Like stepping on frozen garden soil at dawn — firm, pale, slightly crystalline |

### Secondary Terrain Layer: Exposed Dark Soil

Used on plateau tops, ramp bases, and areas under barriers. Warmer, darker — soil that was sheltered from frost.

| Property | Value |
|----------|-------|
| **Terrain Layer Asset** | `DenMud_Layer.terrainlayer` (exists) or `FrozenMud_Texture.mat` as reference |
| **Base Color** | Dark brown-grey `(0.35, 0.30, 0.25)` — `#594D40` |
| **Smoothness** | 0.15–0.20 (dry, matte) |
| **Tiling** | Match primary layer tiling |
| **Blend** | Perlin-noise transition. Plateau tops get more dark soil; valley floors get more frost. |

### Splat Map Blending Rules

- **Plateau interiors (Tier 3):** 70% dark soil / 30% frost base
- **Ramp zones:** Gradient from dark soil (top) to frost base (bottom)
- **Valley floor (Tier 1):** 85% frost base / 15% dark soil patches
- **Under barriers/walls:** 100% dark soil (sheltered from frost)
- **Blend noise:** Perlin scale 0.08–0.12, same system as existing maps

---

## Barrier System — Frozen Chitin Walls

In King Sejong Station, cliffs and destructible rocks define the strategic geography. In Shazu Den, these are replaced by **frozen chitin formations** — the remains of dead insects and exoskeletons from past seasons, now frozen solid into the earth. They serve as impassable terrain (NavMeshObstacle with carving).

### Existing Assets to Use

| Prefab | Role | Notes |
|--------|------|-------|
| `FrozenInsectPillar.prefab` (+ variants 1–9) | **Primary barrier element** | Tall chitin columns. Use clusters of 2–4 at varying scales to form walls. |
| `FrozenInsectHorn.prefab` | **Accent barrier** | Curved horn-like protrusions. Place at wall ends and corners for organic silhouette. |
| `FrozenChitinClaw.prefab` | **Destructible rock replacement** | Distinctive claw shape. Use specifically at the pocket-third entrance to signal "breakable barrier." |
| `FrozenHiveSpire.prefab` | **Landmark barrier** | Tall, dramatic. Place 1–2 at key choke points as visual landmarks. |
| `FrozenHiveSpire_Organic.prefab` | **Alt landmark** | More organic variant. Use for visual variety. |
| `FrozenOrganicPillar.prefab` | **Ridge filler** | Shorter, rounder. Fill gaps between taller pillars. |
| `StationPillar_Icy.prefab` | **Map-edge sentinel** | Tall icy pillar. Place at map boundary corners for dramatic framing. |

### Material Assignments

| Material | Use |
|----------|-----|
| `ChitinIce_Texture.mat` | Primary barrier surface — dark chitin with frost overlay |
| `FrozenOrganicBase_Texture.mat` / `_v2.mat` | Base/foundation of barrier clusters |
| `GlacialIce_Texture.mat` | Accent patches on barrier tops — subtle ice buildup |
| `FrozenHiveMacro_Texture.mat` | Large barrier landmark surfaces |

### New Art Needed: Barrier Cluster Prefabs

Assemble the individual pillar/horn/claw prefabs into **pre-composed cluster prefabs** for the map builder:

#### `ShazuDen_WallSegment.prefab` — Standard Barrier

Replaces generic clay walls. A cluster of 2–3 `FrozenInsectPillar` variants + 1 `FrozenInsectHorn` at one end.

| Property | Value |
|----------|-------|
| **Footprint** | ~12×4 m (length × depth) |
| **Height** | 3.5–5.0 m (must block vision from RTS camera) |
| **Pivot** | Ground center |
| **Composition** | 2–3 pillar meshes (varying Y scale 0.8–1.2×), 1 horn mesh at end, rubble scatter at base |
| **Color** | Dark brown-black chitin `(0.18, 0.14, 0.10)` with frost-white edges `(0.75, 0.78, 0.82)` |
| **Material** | `ChitinIce_Texture.mat` on pillars, `FrozenOrganicBase_Texture.mat` on base rubble |
| **Smoothness** | Chitin body: 0.4–0.5 (glossy exoskeleton). Frost patches: 0.6–0.7. Base: 0.1 |
| **Tri budget** | ≤ 2500 |

#### `ShazuDen_ChokeWall.prefab` — Choke Point Barrier

Larger formation for the championship chokes between natural and third base areas.

| Property | Value |
|----------|-------|
| **Footprint** | ~20×6 m |
| **Height** | 4.0–6.0 m |
| **Composition** | 1 `FrozenHiveSpire` (center, tallest), 2–3 `FrozenInsectPillar` flanking, 2 `FrozenInsectHorn` at ends curving inward to narrow the passage |
| **Gap** | Leave a 6–8 m gap in the middle — this is the choke point units must pass through |
| **Color** | Same as WallSegment but spire gets `FrozenHiveMacro_Texture.mat` for visual prominence |
| **Tri budget** | ≤ 4000 |

#### `ShazuDen_DestructibleBarrier.prefab` — Pocket Third Entrance

The "destructible rocks" equivalent. Visually distinct from permanent walls — must read as "this can be broken."

| Property | Value |
|----------|-------|
| **Footprint** | ~8×4 m (blocks a passage) |
| **Height** | 2.5–3.5 m (shorter than permanent walls) |
| **Composition** | 2 `FrozenChitinClaw` prefabs crossed over each other, with `FrozenOrganicPillar` fragments at the base |
| **Visual Distinction** | Add visible **crack lines** (thin dark seams) across the surface. Use lighter frost color `(0.65, 0.68, 0.72)` compared to permanent walls. Optional: subtle ice crystal geometry (small angular protrusions) suggesting brittleness. |
| **Color** | Lighter than permanent barriers — more ice, less chitin. Grey-white `(0.58, 0.55, 0.52)` body with blue-white frost `(0.78, 0.82, 0.88)` |
| **Material** | `GlacialIce_Texture.mat` primary, `ChitinIce_Texture.mat` accents |
| **Tri budget** | ≤ 2000 |

**Gameplay note:** Currently the game has no destructible barrier mechanic. This prefab should be designed so the system can be added later (health bar, crumble animation). For now it functions as a permanent NavMesh obstacle like any other clay wall, but the visual language signals future destructibility.

---

## Resource Nodes — Winter Fruit

Resources in Insect Wars are rotting fruit. In the winter setting, these become **frozen/frost-covered fruit** — apples and berries that fell in autumn and froze into the soil.

### Main Base Apple (Big Apple — Primary Resource)

The player's starting resource, placed near the hive.

| Property | Value |
|----------|-------|
| **Shape** | Sphere scaled (4, 3, 4), bottom third buried (same geometry as default) |
| **Color** | Frost-dulled golden `(0.65, 0.52, 0.18)` — less saturated than default `BigRootedApple.mat` |
| **Frost overlay** | White-blue frost patches on the upper hemisphere `(0.80, 0.83, 0.88)`, 30–40% coverage |
| **Material** | New: `BigRootedApple_Frozen.mat` — based on `BigRootedApple.mat` but desaturated, with added frost detail via secondary UV or vertex color |
| **Smoothness** | Fruit skin: 0.3 (matte, cold). Frost patches: 0.55 (slightly glossy) |
| **Ground contact** | Dark soil ring around the base where the apple sheltered the ground from frost |

### Natural Expansion Fruit (Standard Fruit Nodes)

| Property | Value |
|----------|-------|
| **Shape** | Standard `RottingFruitNode` sphere, scale ~1.8 |
| **Color** | Frozen berry purple-blue `(0.45, 0.20, 0.50)` — colder shift of default purple-pink |
| **Frost** | Thin white frost veining across the surface, like frozen condensation |
| **Placement** | Semi-circle "mineral line" arrangement: 3–4 fruit nodes in an arc, 2–3 m apart, with the hive deposit point 8–10 m from the arc center |

### Gold Base Fruit (High-Yield — Center Map)

The equivalent of SC2's gold mineral patches. Visually distinct to signal higher value.

| Property | Value |
|----------|-------|
| **Shape** | Standard fruit sphere, scale ~2.2 (slightly larger than normal) |
| **Color** | Amber-gold `(0.90, 0.70, 0.12)` — warm and saturated, stands out against cold terrain |
| **Frost** | Minimal — these fruits are in low-ground basins sheltered from wind, so less frost. Light condensation only. |
| **Visual distinction** | Add a subtle warm-toned particle effect (tiny amber motes drifting upward, 2–3 particles visible at any time) to signal "this is valuable." Or add a faint golden ground stain (decal/disc mesh) beneath. |
| **Material** | New: `GoldFruit_Frozen.mat` — high saturation amber, smoothness 0.45 |
| **Calories** | 2× standard gather rate (game mechanic, not art — but the visual must communicate "rich") |

**New art needed:** `GoldFruit_Frozen.mat` material. Optionally a variant prefab `RottingFruit_Gold.prefab` with the amber mote particle system.

---

## Terrain Features — Winter Variants

Each terrain feature type needs a winter-themed visual treatment for Shazu Den. The existing `UnitVisualLibrary` slots can be overridden per-map via a dedicated `VisualLibrary_ShazuDen.asset`.

### 1. Frozen Puddle (replaces Water Puddle)

Shallow dewdrops that froze solid. Impassable-looking but gameplay-wise just a slow zone.

| Property | Value |
|----------|-------|
| **Prefab** | New: `TF_FrozenPuddle.prefab` |
| **Shape** | Irregular flat disc (same as water puddle) but with angular surface cracks |
| **Color** | Pale blue-white ice `(0.78, 0.84, 0.92)` — `#C7D6EB` |
| **Crack pattern** | 4–6 dark hairline fracture lines radiating from center (geometry or normal map) |
| **Frost rim** | Raised crystalline edge, 0.08–0.12 m tall, white `(0.88, 0.90, 0.92)` |
| **Smoothness** | 0.75–0.85 (ice is reflective) |
| **Detail** | 1–2 small frozen air bubbles (tiny white spheres trapped in the surface). Optional: a dead leaf or twig fragment frozen into the ice at the edge. |
| **Readability** | Must read as "ice" from camera height — high contrast against the brown-grey frozen soil |
| **Tri budget** | ≤ 800 |

### 2. Dead Grass (replaces Tall Grass — Concealment Zone)

Winter-killed grass blades — brown, brittle, skeletal. Still provides concealment because the dense dead stalks block line of sight.

| Property | Value |
|----------|-------|
| **Prefab** | New: `TF_DeadGrass.prefab` |
| **Shape** | 8–12 tall blades, same structure as `TF_TallGrass` but with broken/bent tips |
| **Color** | Straw yellow-brown `(0.58, 0.48, 0.28)` base, pale frost-white tips `(0.75, 0.72, 0.68)` |
| **Height** | 1.2–2.0 m (same as living grass) |
| **Width per blade** | 0.06–0.12 m (slightly thinner — dried out) |
| **Bent tips** | 30–50% of blades should have tips bent or broken at ~60% height, hanging downward |
| **Ground base** | Pale brown disc with scattered frost-white specks |
| **Material** | Matte — smoothness 0.05–0.10 (bone dry) |
| **Readability** | Dense cluster of pale brown verticals. Distinct from scatter grass (which is shorter and sparser). |
| **Tri budget** | ≤ 1200 |

### 3. Frost Patch (replaces Mud Patch — Slow Zone)

Instead of sticky mud, units slip on a smooth frost-covered ground area.

| Property | Value |
|----------|-------|
| **Prefab** | New: `TF_FrostPatch.prefab` |
| **Shape** | Flat irregular disc, ground-level, with crystalline frost texture |
| **Color** | White-blue frost `(0.82, 0.85, 0.90)` with pale soil showing through `(0.60, 0.55, 0.50)` |
| **Surface** | Slightly raised frost crystals (small angular bumps, 0.01–0.03 m) across the surface |
| **Smoothness** | 0.60–0.75 (slippery ice feel) |
| **Edge** | Feathered/gradient edge — frost density decreases toward the perimeter |
| **Detail** | Optional: 2–3 tiny frost crystal clusters (small angular white geometry, 0.05–0.10 m tall) scattered on the surface |
| **Readability** | Bright white-blue patch on the grey-brown ground. Clear "slippery" signal. |
| **Tri budget** | ≤ 600 |

### 4. Frozen Thorn Patch (replaces Thorn Patch — Damage Zone)

Ice crystals and frozen thorny plant fragments that damage units. Sharp, dangerous, crystalline.

| Property | Value |
|----------|-------|
| **Prefab** | New: `TF_FrozenThorns.prefab` |
| **Shape** | Angular ice crystal spikes + frozen thorn branches radiating from a central point |
| **Color** | Ice crystal spikes: pale blue-white `(0.72, 0.78, 0.88)`. Frozen thorns: dark brown-black `(0.25, 0.18, 0.12)` with frost edges. |
| **Height** | Spikes: 0.4–1.0 m. Thorns: 0.3–0.6 m. |
| **Composition** | 4–6 angular ice spikes (elongated pyramids/prisms) pointing upward at various angles, interleaved with 3–4 dark frozen thorn branches |
| **Base** | Thin disc with cracked ice texture, pale blue |
| **Smoothness** | Ice spikes: 0.7–0.8. Thorns: 0.1 (matte dead wood) |
| **Danger signal** | The sharp angular silhouette + contrast between dark thorns and bright ice = "do not enter." More dangerous-looking than standard thorns. |
| **Tri budget** | ≤ 1500 |

### 5. Ice Ridge (replaces Rocky Ridge — Vision/Path Blocker)

Massive frozen formations — clusters of ice-encrusted pebbles and frozen soil mounds. The King Sejong Station equivalent of cliff edges.

| Property | Value |
|----------|-------|
| **Prefab** | New: `TF_IceRidge.prefab` |
| **Shape** | Dense cluster of frozen rock forms, taller and more angular than standard rocky ridge |
| **Height** | 2.5–4.0 m (must block vision) |
| **Color** | Grey-blue stone `(0.50, 0.52, 0.58)` with heavy frost encrustation `(0.80, 0.83, 0.88)` on upper surfaces |
| **Composition** | 3–5 rock meshes (reuse pebble scatter geometry at 3–5× scale) with ice cap geometry on top of each. Frozen soil binding them together at the base. |
| **Material** | Rocks: `FrozenOrganicGround_Texture.mat`. Ice caps: `GlacialIce_Texture.mat`. |
| **Smoothness** | Rock body: 0.15. Ice caps: 0.70. |
| **Ground** | Frost-covered rubble scatter at base, using `FrozenOrganicBase_Texture.mat` |
| **Readability** | Massive, solid, impassable. The ice-capped tops make it distinct from barrier walls (which are chitin-based). |
| **Tri budget** | ≤ 3000 (can use LODs) |

---

## Passive Scatter — Winter Decorations

The scatter system places 280–950 decorative props. For Shazu Den, all four scatter types need winter reskins.

### Distribution (same percentages as default)

| Type | % | Count (of ~648) |
|------|---|-----------------|
| Dead grass blade | 30% | ~194 |
| Frost-covered pebble | 25% | ~162 |
| Frozen fallen leaf | 25% | ~162 |
| Bare twig | 20% | ~130 |

### Dead Grass Blade (Scatter)

| Property | Value |
|----------|-------|
| **Shape** | Flat vertical rectangle (same as default grass blade), but with a broken/bent tip variant (50% chance) |
| **Height** | 1.0–2.8 m (slightly shorter range than green — winter stunting) |
| **Width** | 0.06–0.16 m |
| **Color range** | Straw brown `(0.50, 0.42, 0.25)` → pale grey-brown `(0.65, 0.58, 0.45)` |
| **Lean** | 5–25° (more droopy than green grass — weighted down by frost) |
| **Material** | URP/Lit, smoothness 0.05, no metallic |

### Frost-Covered Pebble

| Property | Value |
|----------|-------|
| **Shape** | Flattened sphere (same geometry as default pebble) |
| **Scale** | 0.3–1.1 m |
| **Color range** | Cool grey `(0.48, 0.46, 0.44)` → blue-grey `(0.55, 0.56, 0.60)` |
| **Frost effect** | Upper hemisphere slightly lighter (frost accumulation on top). 10% color lerp toward white on upper verts. |
| **Material** | Smoothness 0.20–0.30 (frost glaze) |
| **Prefab** | Use `Scatter_Pebble.glb` if available, else procedural sphere. Apply `FrozenOrganicBase_Texture.mat` or tinted URP/Lit. |

### Frozen Fallen Leaf

| Property | Value |
|----------|-------|
| **Shape** | Flat cylinder at ground level (same as default leaf) |
| **Scale** | 1.2–3.5 m across |
| **Color range** | Dark brown `(0.35, 0.25, 0.12)` → grey-brown `(0.48, 0.40, 0.30)` — desaturated autumn |
| **Frost** | Thin white frost edge along the perimeter. Optional frost veining across surface (normal map detail). |
| **Material** | Smoothness 0.10–0.20 (dry but with frost sheen at edges) |
| **Prefab** | Use `Scatter_FallenLeaf.glb` if available, else procedural cylinder. |

### Bare Twig

| Property | Value |
|----------|-------|
| **Shape** | Thin cylinder lying nearly flat (~85–90° tilt from vertical) |
| **Length** | 1.5–4.5 m |
| **Width** | 0.06–0.12 m |
| **Color range** | Dark bark `(0.30, 0.22, 0.14)` → grey bark `(0.45, 0.40, 0.35)` — bleached by winter |
| **Detail** | No leaves. Optional: 1–2 tiny branch stubs (0.02 m protrusions) — but can skip for tri budget |
| **Material** | Smoothness 0.05 (bone dry wood) |

### Scatter Color Palette Summary

| Element | From | To |
|---------|------|----|
| Dead grass | Straw `#806B40` | Pale grey `#A6947A` |
| Pebble | Cool grey `#7A7570` | Blue-grey `#8C8F99` |
| Fallen leaf | Dark brown `#59401F` | Grey-brown `#7A664D` |
| Twig | Dark bark `#4D3824` | Grey bark `#73665A` |

---

## Map Boundary

### Edge Treatment

| Property | Value |
|----------|-------|
| **Shape** | Thin cubes around the perimeter (same system as default) |
| **Color** | Dark frozen earth `(0.25, 0.22, 0.18)` — `#40382E` |
| **Height** | 1.5 m (same as default) |
| **Accent** | Place 4 `StationPillar_Icy.prefab` at the four corners of the map boundary as landmark sentinels |

---

## UnitVisualLibrary — Shazu Den Override

A new `UnitVisualLibrary` asset is needed: **`VisualLibrary_ShazuDen.asset`**

### Field Assignments

| Field | Asset |
|-------|-------|
| `baseSoilLayer` | `DenFrozen_Layer.terrainlayer` |
| `drySoilLayer` | `DenMud_Layer.terrainlayer` (or `DenSnow_Layer.terrainlayer` for more contrast) |
| `clayWallPrefab` | `ShazuDen_WallSegment.prefab` (new, see Barrier section) |
| `groundMaterial` | `FrozenInsectGround_Texture.mat` (exists) or `OrganicFrozenFloor_Texture.mat` |
| `waterPuddlePrefab` | `TF_FrozenPuddle.prefab` (new) |
| `tallGrassPrefab` | `TF_DeadGrass.prefab` (new) |
| `mudPatchPrefab` | `TF_FrostPatch.prefab` (new) |
| `thornPatchPrefab` | `TF_FrozenThorns.prefab` (new) |
| `rockyRidgePrefab` | `TF_IceRidge.prefab` (new) |
| All unit/building prefabs | Same as `DefaultVisualLibrary` (units don't change per map) |
| `bigAppleMaterial` | `BigRootedApple_Frozen.mat` (new) |
| `rottingApplePrefab` | Same as default (or a frost-tinted variant) |

---

## Strategic Terrain Feature Placement

Terrain features in Shazu Den serve the same strategic role as in King Sejong Station — controlling movement, vision, and army positioning.

### Championship Chokes (2×)

Between the natural expansion and the map center. Each side has one.

| Feature | Type | Position (Player Side) | Size |
|---------|------|----------------------|------|
| Choke Ridge | `RockyRidge` → `TF_IceRidge` | (-35, 0) | `boxHalfExtents (25, 8)`, rotation 30° |
| Choke Ridge | `RockyRidge` → `TF_IceRidge` | (35, 0) | `boxHalfExtents (25, 8)`, rotation -30° |

These ice ridges force armies through a narrow corridor between the natural and the center.

### Center Frozen Lake (1×)

A large frozen puddle in the exact map center. Slows armies that try to cross directly.

| Feature | Type | Position | Size |
|---------|------|----------|------|
| Center Lake | `WaterPuddle` → `TF_FrozenPuddle` | (0, 0) | `radius = 18` |

### Concealment Patches (4×)

Dead grass clusters near expansion approaches — ambush positions.

| Feature | Type | Positions | Size |
|---------|------|-----------|------|
| Ambush Grass | `TallGrass` → `TF_DeadGrass` | (-40, 60), (40, -60), (-60, -20), (60, 20) | `radius = 6` each |

### Frost Slow Zones (2×)

Frost patches on ramp approaches to the gold bases — defenders' advantage.

| Feature | Type | Positions | Size |
|---------|------|-----------|------|
| Gold Approach Frost | `MudPatch` → `TF_FrostPatch` | (80, 30), (-80, -30) | `radius = 10` each |

### Frozen Thorn Patches (2×)

Placed at the narrow passages between the third base pocket and the main base — area denial.

| Feature | Type | Positions | Size |
|---------|------|-----------|------|
| Pocket Thorns | `ThornPatch` → `TF_FrozenThorns` | (-55, 65), (55, -65) | `radius = 5` each |

---

## New Art Asset Checklist

### Materials (New)

| Name | Purpose | Base Reference |
|------|---------|---------------|
| `BigRootedApple_Frozen.mat` | Frost-covered main apple | Desaturate `BigRootedApple.mat`, add frost-white secondary |
| `GoldFruit_Frozen.mat` | High-yield gold resource node | Warm amber, high saturation |

### Prefabs (New)

| Name | Purpose | Tri Budget |
|------|---------|------------|
| `ShazuDen_WallSegment.prefab` | Standard barrier wall (clay replacement) | ≤ 2500 |
| `ShazuDen_ChokeWall.prefab` | Large choke-point barrier formation | ≤ 4000 |
| `ShazuDen_DestructibleBarrier.prefab` | Pocket-third entrance (future destructible) | ≤ 2000 |
| `TF_FrozenPuddle.prefab` | Frozen water terrain feature | ≤ 800 |
| `TF_DeadGrass.prefab` | Dead winter grass concealment zone | ≤ 1200 |
| `TF_FrostPatch.prefab` | Frost slow zone | ≤ 600 |
| `TF_FrozenThorns.prefab` | Ice crystal + frozen thorn damage zone | ≤ 1500 |
| `TF_IceRidge.prefab` | Ice-encrusted rock ridge (vision/path blocker) | ≤ 3000 |

### Assets (New)

| Name | Type | Purpose |
|------|------|---------|
| `VisualLibrary_ShazuDen.asset` | `UnitVisualLibrary` ScriptableObject | Per-map visual override |

### Existing Assets to Reuse

| Asset | Role in Shazu Den |
|-------|------------------|
| `FrozenInsectPillar.prefab` (+ 9 variants) | Barrier wall building blocks |
| `FrozenInsectHorn.prefab` | Barrier wall accent |
| `FrozenChitinClaw.prefab` | Destructible barrier centerpiece |
| `FrozenHiveSpire.prefab` / `_Organic` | Choke landmark |
| `FrozenOrganicPillar.prefab` | Ridge filler |
| `StationPillar_Icy.prefab` | Map corner sentinels |
| `ChitinIce_Texture.mat` | Barrier primary material |
| `GlacialIce_Texture.mat` | Ice accent material |
| `FrozenOrganicBase_Texture.mat` / `_v2` | Barrier base material |
| `FrozenHiveMacro_Texture.mat` | Landmark barrier material |
| `FrozenInsectGround_Texture.mat` | Ground material |
| `OrganicFrozenFloor_Texture.mat` | Alt ground material |
| `DenFrozen_Layer.terrainlayer` | Primary terrain layer |
| `DenMud_Layer.terrainlayer` | Secondary terrain layer |
| `DenSnow_Layer.terrainlayer` | Alt secondary terrain layer |
| `Scatter_Pebble.glb` | Pebble scatter (retinted) |
| `Scatter_FallenLeaf.glb` | Leaf scatter (retinted) |

---

## Color Palette Summary — Shazu Den

### Ground & Environment

| Element | Color | Hex |
|---------|-------|-----|
| Frozen soil (primary) | `(0.55, 0.50, 0.45)` | `#8C8073` |
| Frost highlights | `(0.72, 0.75, 0.80)` | `#B8BFCC` |
| Exposed dark soil | `(0.35, 0.30, 0.25)` | `#594D40` |
| Map boundary | `(0.25, 0.22, 0.18)` | `#40382E` |

### Barriers (Frozen Chitin)

| Element | Color | Hex |
|---------|-------|-----|
| Chitin body | `(0.18, 0.14, 0.10)` | `#2E241A` |
| Frost on chitin | `(0.75, 0.78, 0.82)` | `#BFC7D1` |
| Destructible (lighter) | `(0.58, 0.55, 0.52)` | `#948C85` |
| Ice accent | `(0.78, 0.82, 0.88)` | `#C7D1E0` |

### Resources

| Element | Color | Hex |
|---------|-------|-----|
| Main apple (frost-dulled) | `(0.65, 0.52, 0.18)` | `#A6852E` |
| Standard fruit (frozen berry) | `(0.45, 0.20, 0.50)` | `#733380` |
| Gold fruit (high-yield) | `(0.90, 0.70, 0.12)` | `#E5B31F` |
| Frost on fruit | `(0.80, 0.83, 0.88)` | `#CCD4E0` |

### Terrain Features

| Element | Color | Hex |
|---------|-------|-----|
| Frozen puddle | `(0.78, 0.84, 0.92)` | `#C7D6EB` |
| Dead grass | `(0.58, 0.48, 0.28)` | `#947A47` |
| Frost patch | `(0.82, 0.85, 0.90)` | `#D1D9E5` |
| Ice crystal spikes | `(0.72, 0.78, 0.88)` | `#B8C7E0` |
| Frozen thorns (dark) | `(0.25, 0.18, 0.12)` | `#402E1F` |
| Ice ridge stone | `(0.50, 0.52, 0.58)` | `#808594` |
| Ice ridge caps | `(0.80, 0.83, 0.88)` | `#CCD4E0` |

### Scatter Decorations

| Element | From | To |
|---------|------|----|
| Dead grass blades | `(0.50, 0.42, 0.25)` | `(0.65, 0.58, 0.45)` |
| Frost pebbles | `(0.48, 0.46, 0.44)` | `(0.55, 0.56, 0.60)` |
| Frozen leaves | `(0.35, 0.25, 0.12)` | `(0.48, 0.40, 0.30)` |
| Bare twigs | `(0.30, 0.22, 0.14)` | `(0.45, 0.40, 0.35)` |

---

## Prefab Structure Templates

### Barrier Cluster

```
ShazuDen_WallSegment (root — empty Transform at ground center)
 +-- Pillar_01 (FrozenInsectPillar variant, scale 1.0)
 +-- Pillar_02 (FrozenInsectPillar variant, scale 0.85, offset X)
 +-- Horn_End (FrozenInsectHorn, rotated to face outward)
 +-- BaseRubble (low-poly scattered rocks mesh at ground level)
```

### Terrain Feature (Frozen Puddle Example)

```
TF_FrozenPuddle (root — ground center)
 +-- IceSurface (flat disc mesh with crack normal map)
 +-- FrostRim (thin ring mesh around edge)
 +-- FrozenLeaf (optional — small leaf mesh embedded in ice edge)
 +-- AirBubble_01 (tiny white sphere, Y = 0.01)
```

### Terrain Feature (Ice Ridge Example)

```
TF_IceRidge (root — ground center)
 +-- Rock_Large (largest stone, center)
 +-- Rock_Medium_01 (offset left)
 +-- Rock_Medium_02 (offset right)
 +-- IceCap_01 (angular ice geometry on top of Rock_Large)
 +-- IceCap_02 (on Rock_Medium_01)
 +-- FrostRubble (ground-level scatter mesh)
```

---

## Performance Budget

| Category | Count | Tris Per | Total Tris |
|----------|-------|----------|------------|
| Barrier clusters | 6–10 | 2500–4000 | 15,000–40,000 |
| Terrain features | 8–12 | 600–3000 | 4,800–36,000 |
| Scatter props | ~648 | 12–50 each | 7,800–32,400 |
| Resource nodes | 8–10 | 200–500 | 1,600–5,000 |
| Map boundary + sentinels | 4 corners + edges | ~500 each | ~2,000 |
| **Total estimate** | — | — | **31,200–115,400** |

This is within budget for a URP RTS viewed from 30–60 m altitude. Barrier clusters are the heaviest — consider LODs for `ShazuDen_ChokeWall` if it exceeds 3000 tris at LOD0.

---

## Mood Board Keywords

For AI art generation or concept art reference:
- "Frozen garden soil macro photography"
- "Frost crystals on dead leaves close-up"
- "Winter ant colony entrance frozen earth"
- "Ice-encrusted pebbles morning frost"
- "Dead grass blades winter frost macro"
- "Frozen dewdrop on soil surface"
- "Chitin exoskeleton frost covered"
- "Antarctic research station StarCraft 2" (for layout reference only)
