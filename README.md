# Dungeon Escape (raylib-cs)

Dark-fantasy, top-down dungeon crawler prototype built with C# and raylib-cs.

## Overview

Dungeon Escape is a single-player RPG prototype focused on:

- Character creation and build choices
- Tactical dungeon movement with enemy AI
- Turn-based combat with skills, spells, items, and flee options
- Loot, progression, feats, and equipment
- Save/load support for longer runs

## Gameplay Features

- Main menu (`Continue`, `New Game`, `How To Play`, `Quit`)
- Character creation with:
  - Name, gender, race, class, and appearance selection
  - Starting stat allocation
  - Starting feat selection with full prerequisite checks
  - Optional condition presets
- One full floor currently playable, including:
  - Multiple map zones and reward nodes
  - Enemy packs and a floor boss (`Dread Knight`)
  - Route choice and run modifiers
- Enemy AI with patrol/chase/search/return states, plus vision range and line-of-sight behavior
- Turn-based combat:
  - Attack, skills, spells, consumables, flee
  - Enemy armor mitigation and status interactions
  - Enemy loot usage during combat
- Inventory and equipment with RPG slots (weapon, armor, accessories, ring slots, and more)
- Progression:
  - XP and level-up flow
  - Stat increases
  - Feat progression (67 feats currently in the prototype)
  - Class spell progression with spell slots
- Pause menu:
  - Inventory, Save, Load, Settings, Accessibility, Quit to title
- Persistent save system with autosave + manual slots
- Runtime crash logging

## Tech Stack

- .NET 8
- C#
- [raylib-cs](https://github.com/ChrisDill/Raylib-cs)

## Requirements

- .NET SDK 8.0 or newer
- Windows desktop environment (recommended for this build)

## Quick Start

```powershell
cd "C:\game_making\repo version"
dotnet restore
dotnet run
```

## Controls

- Move: `WASD` or Arrow Keys
- Character Sheet: `C`
- Confirm/Select: `ENTER`
- Pause / Back: `ESC`

## Assets

Assets are expected under the local `Assets` folder:

- Sprite frames: `Assets\Sprites\frames`
- Tileset: `Assets\Tiles\0x72_DungeonTilesetII_v1.7.png`

If assets are distributed as a zip in the repository, extract them back under `Assets\...` before running.

Optional environment overrides:

- `DUNGEON_ESCAPE_FRAMES_DIR`
- `DUNGEON_ESCAPE_TILESET_PATH`

## Save and Log Locations

- Saves: `%LOCALAPPDATA%\DungeonEscapeRaylib\saves`
- Logs: `%LOCALAPPDATA%\DungeonEscapeRaylib\logs`

## Project Docs

- Visual direction: `VISUAL_STYLE.md`
- Art and presentation notes: `VISUAL_BIBLE.md`
