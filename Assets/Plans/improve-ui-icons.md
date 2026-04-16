# Project Overview
- Game Title: Insect Wars
- High-Level Concept: An RTS game featuring insects.
- Players: Single player (implied by skirmish demo).
- Target Platform: PC (StandaloneOSX).
- Render Pipeline: PC_RPAsset (likely URP or Custom).
- UI System: uGUI (Unity Canvas).

# Game Mechanics
## Core Gameplay Loop
Players select insect units and give them commands: Move, Stop, Hold Position, Patrol, Attack, Gather resources, and Build structures.
## Controls and Input Methods
- Left-click: Select units / confirm pending command.
- Right-click: Contextual command (Move/Attack/Gather).
- Keyboard Hotkeys: M (Move), S (Stop), H (Hold), P (Patrol), A (Attack), G (Gather), B (Build), Esc (Cancel).

# UI
The bottom bar contains a command panel where these actions are displayed as icons. Currently, the icons are theme-heavy and hard to understand. The goal is to replace them with clearer versions.

# Key Asset & Context
- **Scripts**:
    - `Assets/_InsectWars/Scripts/RTS/BottomBar.cs`: Manages the UI and assigns sprites to command buttons.
    - `Assets/_InsectWars/Scripts/RTS/GameHUD.cs`: Helper for loading sprites.
- **Current Sprites**:
    - `Assets/_InsectWars/Sprites/UI/NewIcons/icon_move.png`
    - `Assets/_InsectWars/Sprites/UI/NewIcons/icon_stop.png`
    - `Assets/_InsectWars/Sprites/UI/NewIcons/icon_hold.png`
    - `Assets/_InsectWars/Sprites/UI/NewIcons/icon_patrol.png`
    - `Assets/_InsectWars/Sprites/UI/NewIcons/icon_attack.png`
    - `Assets/_InsectWars/Sprites/UI/NewIcons/icon_gather.png`
    - `Assets/_InsectWars/Sprites/UI/NewIcons/icon_build.png`
    - `Assets/_InsectWars/Sprites/UI/NewIcons/icon_cancel.png`

# Implementation Steps
1. **Generate Improved Icons**:
    - Create a set of 8 icons with high clarity.
    - Style: Semi-minimalist, bold, high-contrast, universally understood RTS symbols.
    - Resolution: 512x512 or 1024x1024 (matching existing).
    - Icons to generate:
        - `move_icon`: Bold arrow cursor.
        - `stop_icon`: Red octagon or square.
        - `hold_icon`: Shield or "Stop" hand.
        - `patrol_icon`: Two arrows in a loop.
        - `attack_icon`: Crosshair or sword.
        - `gather_icon`: Basket or resource bag.
        - `build_icon`: Hammer or Hammer/Wrench.
        - `cancel_icon`: Red 'X'.
2. **Import & Configure**:
    - Save new icons to `Assets/_InsectWars/Sprites/UI/ImprovedIcons/`.
    - Configure them as Sprite (2D and UI).
3. **Update UI System**:
    - Modify `BottomBar.cs` to reference the new sprites.
    - Since `BottomBar.cs` uses `GameHUD.LoadSpriteFromResources`, and currently the assets are NOT in a Resources folder (as per my search, although the code tries to load them), I will move the new icons to `Assets/_InsectWars/Resources/UI/ImprovedIcons/` to ensure the existing loading logic works, OR update the inspector references if the prefab is available.
    - Note: `BottomBar.cs` has `[SerializeField]` fields for these sprites. The most reliable way is to update the prefab if it exists, or update the `Awake` fallback logic.

# Verification & Testing
1. **Manual Check**: Run the game, select a unit, and verify the command panel displays the new, clear icons.
2. **UI Scaling**: Ensure icons look good at different resolutions.
3. **Hotkey Check**: Ensure hotkeys still match the labels on the new icons.
