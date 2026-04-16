# Implementation Plan - Out-of-Bounds & Map Barrier Fixes

This plan addresses the feedback on camera scroll margins, out-of-bounds visual style (frozen theme), and adding a tall boundary barrier.

## Project Overview
- Game Title: Insect Wars
- Render Pipeline: URP
- Input System: New Input System

## Key Changes

### 1. Camera Scroll Margin (Permissive)
- Modify `RTSCameraController.cs` to allow the camera to move **past** the southern boundary of the map.
- This "overshoot" allows the player to see the playable area even when partially obscured by the bottom UI bar.
- Revert the "inward" margin that was restricting view.

### 2. Out-of-Bounds Visuals (Frozen Abyss)
- Generate a new "Frozen Abyss" material: dark, deep blue cracked ice that contrasts with the white playable snow.
- Generate a "Map Barrier" material: rugged icy rock.
- Update `UnitVisualLibrary` to support these new slots.

### 3. Tall Map Barrier
- Update `SkirmishDirector.cs` to create a "slightly tall" barrier (e.g., 2.5 units high) around the map edge.
- Use the new barrier material for these edges.
- Ensure the large "skirt" plane remains to cover the background.

## Implementation Steps

### Step 1: Data Structures
- Update `Assets/_InsectWars/Scripts/Data/UnitVisualLibrary.cs` to include `public Material mapBarrierMaterial;`.

### Step 2: Asset Generation
- Generate `FrozenAbyssIce` material for the out-of-bounds floor.
- Generate `IcyBarrierStone` material for the map edges.

### Step 3: Camera Logic
- Modify `Assets/_InsectWars/Scripts/RTS/RTSCameraController.cs`:
    - Rename `southMarginExtra` to `southOvershoot`.
    - Change clamping logic: `p.z = Mathf.Clamp(p.z, -h + m - southOvershoot, h - m);`.

### Step 4: World Generation
- Modify `Assets/_InsectWars/Scripts/RTS/SkirmishDirector.cs`:
    - Update `AddMapBounds` to use a taller scale for the edge cubes (e.g., `Vector3(..., 2.5f, ...)`).
    - Assign `mapBarrierMaterial` to these cubes.
    - Keep the `skirt` for the floor using `outOfBoundsMaterial`.

### Step 5: Wiring
- Use a `RunCommand` script to assign the generated materials to the `DefaultVisualLibrary` asset.

## Verification & Testing
- Visual check: Ensure the out-of-bounds area looks like a dark frozen abyss.
- Barrier check: Ensure the map edges are clearly defined by a tall, icy barrier.
- Camera check: Ensure scrolling to the bottom of the map allows the player to see the southern playable area clearly without it being hidden by the UI.
