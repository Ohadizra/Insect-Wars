# Terrain Feature Art Guide — Insect Wars

## Overview

Insect Wars is a real-time strategy game at **insect scale**. The battlefield is a patch of earth, and objects that seem mundane at human scale become enormous landmarks. Terrain features should feel like **natural micro-landscapes** — a dewdrop is a lake, a pebble is a boulder, a blade of grass is a towering tree.

All features use Unity URP (Universal Render Pipeline) and are assigned via `UnitVisualLibrary` prefab slots. The runtime system falls back to procedural placeholder geometry when no prefab is provided.

---

## General Style Rules

| Rule | Details |
|------|---------|
| **Scale reference** | An insect unit is roughly **0.5–1.1 m** tall in Unity units. A terrain feature with `radius = 5` covers a ~10 m diameter circle. |
| **Art style** | Semi-stylized / toon. Soft gradients, muted color-key silhouettes, readable from a top-down RTS camera at 30–60 m altitude. |
| **Poly budget** | Each feature prefab should be under **1500 triangles**. The entire map may have 8–12 features, so total budget is ~12–18k tris for terrain art. |
| **Materials** | 1–2 materials per prefab. Use URP/Lit or URP/Simple Lit. Avoid transparency where possible (use alpha-clip if needed for grass). |
| **Pivot** | Prefab pivot must be at **ground level, center XZ**. The system places the pivot on the terrain surface. |
| **Rotation** | The system sets Y rotation from `TerrainFeaturePlaced.rotation`. Don't hard-bake a rotation into the prefab. |
| **Scale** | Prefabs are **not scaled by the system** — model at 1:1 for a "radius = 1" zone. The radius field controls gameplay zone size, not visual size, but visuals should match roughly. Use a reference sphere of radius 1 when modeling. The art should feel natural if the zone has radius 3–6. |
| **Colliders** | Do **NOT** add colliders to the prefab. The system adds `NavMeshObstacle` (for RockyRidge) or `NavMeshModifier` programmatically. |
| **Layer / Tag** | Leave default — the system does not change layer on terrain features. |
| **LODs** | Optional but appreciated for rocky ridges (most complex geometry). |

---

## Feature Type Specifications

### 1. Water Puddle (`waterPuddlePrefab`)

**Gameplay:** Slows movement to 50%, reduces vision radius by 25%.

**Thematic idea:** A shallow dewdrop pool or rain puddle on bare soil. At insect scale this is a small lake with a visible water surface.

| Aspect | Guideline |
|--------|-----------|
| **Shape** | Roughly circular, irregular edges. Flatten bottom, slight rim. |
| **Color palette** | Translucent blue-grey (`#3878B8`) with brownish soil visible beneath. |
| **Material** | High smoothness (0.8–0.9) to suggest reflective water. Slight metallic (0.05). Optional scrolling normal map for gentle ripples. |
| **Height** | Surface should be ~0.02–0.05 units above ground (paper thin). Optional rim geometry at 0.1 units. |
| **Detail props** | 2–3 tiny floating debris (leaf fragments, pollen grains) on the surface. Optional small cattail-like reeds at edges (2–3 stalks). |
| **Readability** | Must be clearly blue/reflective from RTS camera height. The silhouette should read as "water" even without zoom. |
| **Animation (optional)** | UV-scroll a tiling normal map at 0.02 speed for subtle water movement. |

**Reference at insect scale:** Think of a single water drop sitting in a dip in the soil, maybe 3 cm across in real life but appearing as a pond to the bugs.

---

### 2. Tall Grass (`tallGrassPrefab`)

**Gameplay:** Provides concealment — units inside are hidden from enemies unless they come within 5 world units.

**Thematic idea:** A small cluster of grass blades towering over the insects. Each blade is a translucent green column.

| Aspect | Guideline |
|--------|-----------|
| **Shape** | 8–15 tall vertical blades arranged in a natural cluster. Blades should be slightly tapered (wider at base, narrow at tip). Gentle lean outward, max ~15 degrees. |
| **Color palette** | Saturated natural green (`#4D9635`), with lighter tips (`#7DC462`) and darker bases (`#2D6B1A`). |
| **Material** | URP/Lit with alpha-clip for blade edges if using planes. Alternatively, use capsule/cylinder geometry with solid color. Moderate smoothness (0.3). |
| **Height** | Blades should be 1.0–2.0 units tall (towering over most units). |
| **Width** | Each blade 0.08–0.15 units wide. |
| **Ground** | Include a small circular dark-green base disc or scattered soil at the foot of the blades to ground them visually. |
| **Readability** | The cluster must look distinctly different from decorative scatter grass. Use denser, taller, more vibrant green. Consider adding a very subtle green-tinted rim on the base disc to signal "this is a zone." |
| **Animation (optional)** | Vertex-driven gentle sway using wind noise (sine offset on Y based on vertex height). Keep subtle — large sway is distracting. |

**Reference at insect scale:** Imagine standing at the base of 3–4 grass blades in your garden. They rise like a bamboo forest.

---

### 3. Mud Patch (`mudPatchPrefab`)

**Gameplay:** Slows movement to 60%. No vision or damage effects.

**Thematic idea:** A patch of wet, sticky soil. Dark and saturated.

| Aspect | Guideline |
|--------|-----------|
| **Shape** | Roughly circular disc, slightly lumpy/uneven surface. Irregular edge blending into surrounding terrain. |
| **Color palette** | Dark brown (`#614023`) with slightly wet-looking highlights (`#7A5533`). |
| **Material** | High smoothness in wet areas (0.5–0.7) to suggest moisture. Low metallic. |
| **Height** | Essentially ground-level (0.01–0.03 units raised). Surface should have small bumps/ridges. |
| **Detail props** | Optional: 1–2 tiny root segments poking out, small soil clumps at the edges, or a half-buried twig. |
| **Readability** | Must be clearly a different ground texture from the surrounding terrain. Darker, wetter, distinct. |
| **Edge treatment** | Feathered/soft edges if using alpha-clip ground plane, or use geometry that gradually merges with ground height. |

**Reference at insect scale:** A 2 cm wet spot in the garden where water collected and made the soil soft and sticky.

---

### 4. Thorn Patch (`thornPatchPrefab`)

**Gameplay:** Slows movement to 70%, deals 2 damage per second to any unit inside.

**Thematic idea:** Fallen rose thorns, thistle fragments, or sharp dried plant material scattered on the ground. Danger zone.

| Aspect | Guideline |
|--------|-----------|
| **Shape** | Circular patch with sharp, angular thorn spikes pointing upward and outward at various angles. Mix vertical and diagonal thorns. |
| **Color palette** | Dark olive green base (`#3A6619`), brown-red thorns (`#8B3A1A`), dried yellow-brown tips (`#A67C52`). |
| **Material** | Low smoothness (0.1–0.2) for the dry, rough look. |
| **Height** | Base disc at ground level. Thorns rise 0.3–0.8 units. Some lean outward. |
| **Thorn shapes** | Elongated cones or thin capsules. 6–12 thorns of varying size. Tips should be visibly sharp (thin). |
| **Ground** | Include a thin disc base with a sickly, dried-vegetation color to signal danger zone. |
| **Readability** | Must read as "dangerous" from camera height. The spiky silhouette is key — avoid making it look like harmless twigs. Consider a slightly reddish tint to the base to suggest danger. |
| **VFX (optional)** | Subtle particle effect — tiny red floating motes or occasional dust puffs to signal "this hurts." |

**Reference at insect scale:** A fallen rose thorn or blackberry bramble segment, each thorn the size of a tree trunk to an insect.

---

### 5. Rocky Ridge (`rockyRidgePrefab`)

**Gameplay:** Completely impassable (blocks pathing and vision line-of-sight). Acts as a natural wall.

**Thematic idea:** A cluster of pebbles and small stones forming an impassable ridge. At insect scale, these are mountains and cliffs.

| Aspect | Guideline |
|--------|-----------|
| **Shape** | Dense cluster of overlapping rocks. Mix rounded (sphere-like) and angular (cube-like) shapes. Should fill the zone fully — no large gaps. Tall enough to block vision (1.5–3.0 units). |
| **Color palette** | Neutral grey-brown (`#857B72`) with variation: lighter quartz-white patches (`#B8AFA5`), darker crevice shadows (`#5A524A`). |
| **Material** | Very low smoothness (0.05–0.15), zero metallic. Rocky, matte surface. Consider a subtle normal map for stone grain texture. |
| **Height** | 1.5–3.0 units tall (must visually block the view behind it). |
| **Composition** | 3–6 rock meshes of varying sizes. Largest at center, smaller ones filling gaps. Avoid perfect geometric shapes — use irregular, natural-looking forms. |
| **Ground** | Small rubble/gravel scatter at the base to ground it. |
| **Readability** | Should instantly read as "can't walk through this." Solid, massive, clearly a wall. From the minimap/top-down, should cast visible shadows or have a distinctly darker appearance. |
| **Orientation** | Some rocks tilted at slight angles (5–20 degrees) for a natural, tumbled look. |

**Reference at insect scale:** 3–4 small gravel stones next to each other on a garden path. To the insects, this is a mountain pass.

---

## Prefab Structure

Each terrain feature prefab should follow this hierarchy:

```
TF_WaterPuddle (root — empty Transform, pivot at ground center)
 +-- WaterSurface (mesh)
 +-- Rim (optional mesh)
 +-- FloatingDebris (optional mesh)
```

```
TF_TallGrass (root)
 +-- BasePatch (disc mesh)
 +-- Blade_01 (mesh)
 +-- Blade_02 (mesh)
 +-- ...
```

```
TF_RockyRidge (root)
 +-- Rock_Large (mesh)
 +-- Rock_Medium_01 (mesh)
 +-- Rock_Medium_02 (mesh)
 +-- Rock_Small_01 (mesh)
 +-- Gravel (optional mesh)
```

**Do NOT include:**
- Colliders (added by code)
- NavMeshObstacle / NavMeshModifier (added by code)
- Any MonoBehaviour scripts
- Lights or cameras

---

## Minimap Considerations

Terrain features should be distinguishable on the minimap. The fog-of-war system reveals them like other terrain. Consider making the top-down footprint visually distinct:

| Feature | Minimap hint |
|---------|-------------|
| Water | Blue-tinted ground area |
| Tall Grass | Dense green cluster |
| Mud | Dark brown patch |
| Thorns | Red-brown speckled area |
| Rocky Ridge | Grey solid mass |

The current minimap uses an orthographic camera, so top-down appearance matters. Keep strong color-coding.

---

## Color Coding Summary

| Feature | Primary Color | Secondary | Danger Signal |
|---------|--------------|-----------|---------------|
| Water Puddle | Blue `#3878B8` | Brown soil beneath | None |
| Tall Grass | Green `#4D9635` | Light green tips | None |
| Mud Patch | Dark brown `#614023` | Wet highlights | None |
| Thorn Patch | Olive `#3A6619` | Red-brown thorns `#8B3A1A` | Reddish tinge |
| Rocky Ridge | Grey `#857B72` | White quartz, dark crevices | None |

---

## Performance Notes

- Aim for **under 1500 triangles** per prefab
- Use **1–2 draw calls** (shared materials, atlas textures)
- Avoid real-time lights, reflections, or heavy transparency
- Grass blades: solid capsule geometry preferred over transparent billboarded quads (avoids sorting issues)
- Water: a single plane with a tiling normal map is ideal (no SSR needed)
- Rocky Ridge: use LODs if over 1000 tris — simplify to 2–3 merged hulls for LOD1

---

## Thematic Consistency

The world of Insect Wars is set at ground level in nature. Keep these principles:

1. **Everything is organic** — no metal, plastic, or man-made materials
2. **Muted earth tones** dominate, with pops of saturated color for game-critical features
3. **Readable from above** — the RTS camera sits 30–60 m up. Terrain features need strong silhouettes and color-coding
4. **Insect perspective** — a 3 cm pebble is a 3-story boulder. A grass blade is a 20 m tree. Water droplets are lakes. Scale details accordingly
5. **No glowing effects** — this is a natural world. Use material properties (smoothness, color) rather than emissive/bloom for visual distinction
