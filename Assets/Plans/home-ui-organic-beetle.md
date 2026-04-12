# Project Overview
- **Game Title**: Insect Wars
- **High-Level Concept**: A bug-themed RTS/RPG vertical slice. reskinning the home page to a "Natural Ornate Beetle" style.
- **Art Direction**: Move away from any "purple/green" or placeholder aesthetics toward a "natural and well fitted" look matching the provided concept image.
- **Inspiration**: The user-provided "Stagbeetle Odyssey" UI image.
- **Target Platform**: Standalone MacOS.
- **Render Pipeline**: URP.

# Game Mechanics
## Core Gameplay Loop
The Home scene manages the game state transition from the menu to the skirmish missions.

# UI reskin
The goal is to replace the current placeholder elements with high-quality, organic, and earthy assets.

## Assets & Context
- **Script**: `Assets/_InsectWars/Scripts/UI/HomeMenuBootstrap.cs`
- **Preferred Sprites (already in project)**:
  - `mainFrameSprite`: `Assets/_InsectWars/Sprites/UI/Extracted/frame_ornate.png`
  - `buttonSprite`: `Assets/_InsectWars/Sprites/UI/Extracted/btn_menu.png`
  - `separatorSprite`: `Assets/_InsectWars/Sprites/UI/Extracted/top_bar_frame.png`
- **Color Palette (extracted from concept image)**:
  - **Title Text**: Light Amber/Parchment (`new Color(0.96f, 0.90f, 0.78f)`)
  - **Accent Text**: Warm Copper/Gold (`new Color(0.83f, 0.69f, 0.44f)`)
  - **Shadow/Outline**: Dark Charcoal (`new Color(0.1f, 0.08f, 0.06f, 0.8f)`)
  - **Dimmer Background**: Semi-transparent Dark Grey (`new Color(0f, 0f, 0f, 0.7f)`)

# Implementation Steps
1. **Update `HomeMenuBootstrap.cs` Fields**:
   - Point the `mainFrameSprite`, `buttonSprite`, and `separatorSprite` fields in the `Awake` method (or inspector) to the `Assets/_InsectWars/Sprites/UI/Extracted/` equivalents.
2. **Update Palette Constants**:
   - Replace the `ColTitle`, `ColSub`, and `ColDim` values in `HomeMenuBootstrap.cs` with the earthy/amber tones extracted from the reference art.
3. **Update Text Effects**:
   - Adjust the `Outline` effect distance and color in the `Txt` helper method for a crisper, more "natural" look.
4. **Layout Refinement**:
   - Adjust `BtnW` and `BtnH` in `HomeMenuBootstrap.cs` to 450x80 to better fit the proportions of the `btn_menu.png` asset.
   - Adjust the `separatorSprite` width and height for the `top_bar_frame` to look more like a natural divider.

# Verification & Testing
1. **Scene Verification**: Load `Home.unity` and verify the "MainMenuCanvas" hierarchy displays the new sprites.
2. **Style Alignment**: Confirm the colors match the "Natural Ornate Beetle" theme from the concept image.
3. **Functional Test**: Click through all menu panels to ensure the styling is consistent across all sub-menus.
