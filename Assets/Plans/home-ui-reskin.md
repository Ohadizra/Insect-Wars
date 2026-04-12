# Project Overview
- **Game Title**: Insect Wars
- **High-Level Concept**: A bug-themed RTS/RPG vertical slice where players command insect units.
- **Players**: Single player.
- **Art Direction**: Reskinning the UI to a dark fantasy/steampunk aesthetic based on "Stagbeetle Odyssey".
- **Target Platform**: Standalone (MacOS).
- **Render Pipeline**: URP.

# Game Mechanics
## Core Gameplay Loop
The Home scene serves as the gateway to the skirmish missions. Players select maps and difficulties from this menu.

# UI reskin
The objective is to replace the current placeholder "sketch" style with a high-fidelity "ornate metal" style.

## Assets & Context
- **Script**: `Assets/_InsectWars/Scripts/UI/HomeMenuBootstrap.cs`
- **Target Sprites**:
  - `mainFrameSprite`: An ornate square frame with dark metal and glowing gems.
  - `buttonSprite`: A rectangular metal button with a beveled edge.
  - `separatorSprite`: A horizontal divider with mechanical gear ends.
- **Color Palette**:
  - **Title**: Light Amber/Gold (`#f5e6c8`)
  - **Accents/Subtitles**: Copper/Bronze (`#d4af37`)
  - **Frames/Buttons**: Dark Iron/Bronze (`#3b3633`)
  - **Glows**: Bright Orange (`#ff8c00`)

# Implementation Steps
1. **Asset Generation**:
   - Use the `GenerateAsset` tool to create a high-quality sprite sheet using the user-provided image as a `referenceImageInstanceId`.
   - Prompt: "A high-quality 2D UI asset sheet on white background. Contains: 1. Ornate square dark metal frame with glowing orange gems. 2. Beveled metal button. 3. Ornate horizontal resource bar with gears. 4. Mechanical gear separator. Style: dark fantasy, steampunk, weathered metal."
2. **Asset Processing**:
   - Slice the generated sheet into individual PNGs:
     - `Assets/_InsectWars/Sprites/UI/StyleMatch/frame_ornate_v2.png`
     - `Assets/_InsectWars/Sprites/UI/StyleMatch/btn_metal_v2.png`
     - `Assets/_InsectWars/Sprites/UI/StyleMatch/separator_gear_v2.png`
   - Set import settings to Sprite (2D and UI).
3. **Code Update (`HomeMenuBootstrap.cs`)**:
   - Update `ColTitle` to `new Color(0.96f, 0.84f, 0.58f)`.
   - Update `ColSub` to `new Color(0.83f, 0.69f, 0.44f)`.
   - Update `Awake()` sprite paths to the new `v2` versions.
   - Adjust `PanelW`, `PanelH`, `BtnW`, `BtnH` if the new sprites have different aspect ratios.
4. **Visual Polish**:
   - Add a subtle shadow or outline to the text to ensure it pops against the new ornate frames.

# Verification & Testing
1. **Editor Play Mode**: Run the `Home` scene and verify all panels (Main, Play, Settings, Logs) use the new assets.
2. **Layout Integrity**: Ensure text still fits within buttons and panels.
3. **Responsive Check**: Ensure the UI Scaler handles the new assets correctly at 1920x1080.
