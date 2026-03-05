# Visual Bible

Project: `Dungeon Escape`
Style Direction: `Dark Fantasy - Ember Sanctum`
Primary Asset Pack: `0x72 DungeonTileset II (v1.7)`

## Core Visual Rules

1. Use one consistent pixel-art family (0x72) for all gameplay entities in v1.
2. Keep tile logic at `32x32` world units while preserving pixel art ratios.
3. Anchor entity sprites to tile ground (feet on tile), never center by full height.
4. Keep warm highlights (embers, gold, blood red) against cold stone palette.
5. Do not introduce mixed-style packs unless a hard content gap exists.

## Sprite Technical Spec

- Source frame size (0x72 entities): `16x28`
- In-game scale: `2x`
- Effective on-screen entity footprint: roughly `32x56`
- Animation cadence: `8 FPS`
- Required states per entity set:
  - `idle`
  - `run`
  - `hit` (optional)

## Current Sprite Mapping

### Player class mapping
- Warrior, Paladin -> `knight_(m|f)`
- Rogue, Ranger, Bard -> `elf_(m|f)`
- Mage -> `wizzard_(m|f)`
- Barbarian -> `dwarf_(m|f)`
- Cleric -> `doc`

### Enemy mapping
- Goblin -> `goblin`
- Warg -> `wogol`
- Skeleton -> `skelet`
- Cultist -> `masked_orc`
- Shadow Mage -> `orc_shaman`
- Ogre -> `ogre`
- Troll -> `big_zombie`
- Dread Knight -> `big_demon`

## Asset Locations

- Runtime sprite frames: `Assets/Sprites/frames`
- Tile atlases reserved for map pass: `Assets/Tiles`
- Upstream pack reference: `Assets/SOURCE_0x72_README.txt`

## Expansion Rules

1. Add new entity art only if it follows the same pixel density and palette weight.
2. Prefer adding from 0x72 ecosystem first to avoid style discontinuity.
3. If mixing packs later, run side-by-side palette checks before merging.
4. Keep fallback sprite mapping so gameplay never breaks if an asset is missing.

## Map Tileset Layer

- Floor tiles now render from `0x72_DungeonTilesetII_v1.7.png` using `floor_1..floor_8` variants.
- Wall tiles now render from the same sheet using structural wall tiles (`wall_top_*`, `wall_*`).
- Wall choice is neighbor-aware (top/left/right/mid) to keep silhouettes readable.
- If tileset texture is unavailable, runtime falls back to procedural color rendering.
