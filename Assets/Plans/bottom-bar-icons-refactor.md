# Project Overview
- Game Title: Insect Wars
- High-Level Concept: An insect-themed RTS game where players control different types of beetles and bugs to gather resources and fight enemies.
- Players: Single player (vs AI)
- Inspiration / Reference Games: StarCraft, Warcraft III
- Tone / Art Direction: Organic, slightly stylized, natural/chitinous textures.
- Target Platform: Standalone (PC/Mac)
- Render Pipeline: URP (Universal Render Pipeline)

# Game Mechanics
## Core Gameplay Loop
Resource gathering (calories), unit production (larva/beetles), building construction, and tactical combat.
## Controls and Input Methods
Mouse-based RTS controls (box selection, right-click to move/attack) with keyboard hotkeys for abilities.

# UI
- Bottom Bar: Contains a minimap, selection info/portrait, and a command grid.
- Style: Needs to match the "Insect Wars" theme (organic/ornate frames).

# Key Asset & Context
- `BottomBar.cs` (formerly `Sc2BottomBar.cs`): Main UI script managing the bottom HUD.
- Icons: A full set of ability and unit icons (Move, Attack, Build, etc.) in a consistent organic/chitinous style.
- Prefabs/Objects: `SC2BottomBar` GameObject in the scene.

# Implementation Steps
## Phase 1: Naming & Refactoring
1. **Rename Script**: Rename `Assets/_InsectWars/Scripts/RTS/Sc2BottomBar.cs` to `BottomBar.cs`.
2. **Rename Class**: Update the class name inside the script to `BottomBar`.
3. **Update References**: Update all other scripts (`CommandController.cs`, `SkirmishMinimap.cs`, `SelectionController.cs`, etc.) to use the new `BottomBar` class name.
4. **Rename GameObject**: Change the GameObject name in the code (where it's created via `new GameObject("SC2BottomBar")`) to `BottomBar`.
5. **Rename Field**: Update any "sc2" prefixed fields or variables if requested (e.g., in `CommandController`).

## Phase 2: Icon Generation
1. **Select Model**: Use `Seedream 4` or similar for high-quality sprite generation.
2. **Define Style**: Use `StyleMatch_AssetSheet.png` as a reference to ensure consistency with the existing ornate frames. The style should be "stylized organic/insectoid".
3. **Generate Icons**:
    - `icon_move`: A beetle or insectoid leg symbol indicating movement.
    - `icon_stop`: A crossed chitinous limb or a distinct "halt" symbol.
    - `icon_hold`: A grounded insectoid symbol.
    - `icon_patrol`: A looping arrow symbol with insectoid aesthetics.
    - `icon_attack`: A mandible or horn strike symbol.
    - `icon_gather`: A beetle carrying a resource (crystal/leaf).
    - `icon_build`: Chitinous tools or a construction symbol.
    - `icon_cancel`: A clear but themed "X" or "cancel" symbol.
    - `icon_worker`: A larva or worker beetle icon.
    - `icon_fighter`: A soldier beetle/mantis icon.
    - `icon_ranged`: A spitting/ranged insect icon.
4. **Import Settings**: Set all generated textures to `Sprite (2D and UI)` with `Filter Mode: Point` or `Bilinear` depending on detail, and ensure transparency.

## Phase 3: UI Integration
1. **Update `BottomBar.cs`**:
    - Assign the new sprites to the corresponding fields in the `BottomBar` instance.
    - If the bar background is currently a solid color, consider using a generated "organic chitinous" texture instead of the solid dark brown.
2. **Verify Layout**: Ensure the new icons fit perfectly within the `frame_slot` and the command grid.

# Verification & Testing
1. **Compilation**: Ensure no errors after refactoring the class name.
2. **Visual Check**: Run the game and verify the bottom bar appears with the new icons and the "SC2" name is gone from the hierarchy.
3. **Functional Check**: Verify buttons still work and hotkeys trigger the correct commands.
