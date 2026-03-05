# Dungeon Escape (PC Prototype - raylib-cs)

This folder is the clean GitHub-ready copy of the C# PC port.
It is designed to run on its own (independent from `pc_raylib`).

## Current scope

- Main menu (`Continue` when saves exist, `New Game`, `How To Play`, `Quit`)
- Character creation (name, gender, race, class, appearance sprite selection)
- Pre-run stat allocation (6 starting points)
- Tile map movement with held-key repeat (no repeated key tapping required)
- Enemy collision to trigger combat
- Turn-based combat actions:
  - Attack
  - Skills submenu (active learned skills like Second Wind / Mana Shield)
  - Spells submenu (caster spellbook with spell slots)
  - Items submenu (combat consumables: potion/draught/oil)
  - Flee
- Level up -> stat point allocation -> skill pick
- Character sheet overlay
- Feat selection now shows full legal list with lock reasons (no random subset)
- Spell selection now shows full class spell catalog with lock reasons
- Expanded caster/half-caster spell catalogs across levels 1-6
- Pause menu in-run (`ESC`) with:
  - Root actions: Resume / Inventory / Save Game / Load Game / Settings / Quit to title
  - Inventory: use consumables and equip/unequip run items
  - Save Game: choose manual slot (1-3)
  - Load Game: choose from discovered save files (autosave + manual saves)
  - Save/load/quit confirmation prompts
- Autosave checkpoint support at key gameplay transitions
- Immediate death screen at HP 0 (no extra actions after death)
- D&D-style caster progression (first 6 levels):
  - class spell unlocks at level 1/3/5
  - spell slots through 3rd-level by level 6
- Caster class cantrips are granted automatically (free baseline spells)
- Expanded prototype spell catalog across supported classes (Mage/Cleric/Bard/Paladin/Ranger) through level 3 bands
- Duplicate skill protection
- War Cry first-strike behavior
- Delayed enemy defeat resolution (so death moment is visible)
- Built-in Phase 7 automated checks (combat math, state rules, save/load serialization)
- Crash logging with runtime context on fatal exceptions
- Dark-fantasy visual pass foundation (theme lock + lighting/fog/vignette/embers)
- Animated 0x72 sprites for player and enemies (idle/run with fallback support)
- Tileset-based world rendering for floors and walls (0x72 sheet with fallback)
- Reward/combat outcome notices now persist longer (and key loot notices require ENTER acknowledge)

## Prerequisites

1. Install `.NET SDK 8.0+`
2. On Windows, ensure Visual C++ runtime is available (usually already installed)

## Run

```powershell
cd "C:\game_making\repo version"
dotnet restore
dotnet run
```

## Run Phase 7 checks

```powershell
cd "C:\game_making\repo version"
dotnet run -c Release --no-build -- --phase7-checks
```

## Notes

- Validated in-session on 2026-03-02:
  - `dotnet restore` succeeded
  - `dotnet build -c Release` succeeded (0 errors)
  - `dotnet run -c Release --no-build` launched successfully (command timeout occurred because game loop keeps running until window close)
- The project uses `Raylib-cs` via NuGet in `DungeonEscapeRaylib.csproj`.
- Save files are stored at:
  - `%LOCALAPPDATA%\\DungeonEscapeRaylib\\saves`
- Crash logs are stored at:
  - `%LOCALAPPDATA%\\DungeonEscapeRaylib\\logs`
- Visual style lock document:
  - `VISUAL_STYLE.md`

- Sprite assets are loaded from:
  - `Assets\Sprites\frames`
  - Override path with env var `DUNGEON_ESCAPE_FRAMES_DIR`.
- Visual bible document:
  - `VISUAL_BIBLE.md`

- Map tileset is loaded from:
  - `Assets\Tiles\0x72_DungeonTilesetII_v1.7.png`
  - Override path with env var `DUNGEON_ESCAPE_TILESET_PATH`.

- Asset packaging note:
  - If you publish assets as a zip for GitHub size/structure reasons, extract them back under `Assets\...` before running.
