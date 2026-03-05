using Raylib_cs;
using System.Numerics;

namespace DungeonEscape.Core;

public sealed class GameMap : IDisposable
{
    public const int TileSize = 32;
    public const int MapWidthTiles = 60;
    public const int MapHeightTiles = 36;

    // Floor 1 is assembled from connected zones and corridors.
    private static readonly (int X, int Y, int W, int H)[] OpenZones =
    {
        (2, 2, 14, 10),   // Entry camp
        (18, 2, 14, 11),  // Barracks
        (34, 2, 22, 12),  // Catacombs
        (4, 15, 18, 12),  // Lower halls
        (24, 16, 16, 12), // Ruined shrine
        (42, 18, 16, 15)  // Sanctum
    };

    private static readonly (int X, int Y, int W, int H)[] Corridors =
    {
        (15, 6, 4, 2),
        (31, 7, 4, 2),
        (10, 11, 2, 5),
        (20, 20, 4, 2),
        (39, 21, 4, 2),
        (30, 12, 2, 5),
        (46, 13, 2, 6)
    };

    private static readonly (int X, int Y)[] Pillars =
    {
        (22, 5), (26, 5), (22, 9), (26, 9),
        (38, 5), (42, 5), (46, 5), (50, 5),
        (12, 19), (16, 19), (12, 23), (16, 23),
        (28, 20), (34, 20), (28, 24), (34, 24),
        (46, 22), (52, 22), (46, 28), (52, 28)
    };

    private static readonly Rectangle[] FloorTiles =
    {
        TileRect(16, 64),
        TileRect(32, 64),
        TileRect(48, 64),
        TileRect(16, 80),
        TileRect(32, 80),
        TileRect(48, 80),
        TileRect(16, 96),
        TileRect(32, 96)
    };

    private static readonly Rectangle WallTopLeft = TileRect(16, 0);
    private static readonly Rectangle WallTopMid = TileRect(32, 0);
    private static readonly Rectangle WallTopRight = TileRect(48, 0);
    private static readonly Rectangle WallLeft = TileRect(16, 16);
    private static readonly Rectangle WallMid = TileRect(32, 16);
    private static readonly Rectangle WallRight = TileRect(48, 16);

    private readonly Texture2D _tileSheet;
    private readonly bool _hasTileSheet;
    private bool _disposed;

    public GameMap()
    {
        var tileSheetPath = ResolveTileSheetPath();
        if (string.IsNullOrWhiteSpace(tileSheetPath))
        {
            RuntimeDiagnostics.Warn("Map tileset not found. Falling back to procedural color blocks.");
            _hasTileSheet = false;
            return;
        }

        _tileSheet = Raylib.LoadTexture(tileSheetPath);
        _hasTileSheet = _tileSheet.Id != 0;
        RuntimeDiagnostics.Info($"Map tileset loaded. Ready={_hasTileSheet} Path={tileSheetPath}");
    }

    public bool IsWall(int x, int y)
    {
        if (x < 0 || x >= MapWidthTiles || y < 0 || y >= MapHeightTiles) return true;
        if (x == 0 || y == 0 || x == MapWidthTiles - 1 || y == MapHeightTiles - 1) return true;
        if (!IsOpenTile(x, y)) return true;
        return IsPillar(x, y);
    }

    public void Draw()
    {
        for (var y = 0; y < MapHeightTiles; y++)
        {
            for (var x = 0; x < MapWidthTiles; x++)
            {
                if (IsWall(x, y))
                {
                    DrawWall(x, y);
                }
                else
                {
                    DrawFloor(x, y);
                }
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_hasTileSheet && _tileSheet.Id != 0)
        {
            Raylib.UnloadTexture(_tileSheet);
        }

        _disposed = true;
    }

    private static bool IsOpenTile(int x, int y)
    {
        foreach (var zone in OpenZones)
        {
            if (x >= zone.X && x < zone.X + zone.W && y >= zone.Y && y < zone.Y + zone.H)
            {
                return true;
            }
        }

        foreach (var corridor in Corridors)
        {
            if (x >= corridor.X && x < corridor.X + corridor.W && y >= corridor.Y && y < corridor.Y + corridor.H)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPillar(int x, int y)
    {
        for (var i = 0; i < Pillars.Length; i++)
        {
            if (Pillars[i].X == x && Pillars[i].Y == y)
            {
                return true;
            }
        }

        return false;
    }

    private void DrawFloor(int tileX, int tileY)
    {
        if (_hasTileSheet)
        {
            var n = Noise(tileX, tileY, 127.1f, 311.7f);
            var index = (int)(n * FloorTiles.Length);
            index = Math.Clamp(index, 0, FloorTiles.Length - 1);
            DrawSheetTile(FloorTiles[index], tileX, tileY);
            return;
        }

        var fallback = VisualTheme.GetFloorColor(Noise(tileX, tileY, 127.1f, 311.7f));
        Raylib.DrawRectangle(tileX * TileSize, tileY * TileSize, TileSize, TileSize, fallback);
    }

    private void DrawWall(int tileX, int tileY)
    {
        if (_hasTileSheet)
        {
            var wallTile = ResolveWallTile(tileX, tileY);
            DrawSheetTile(wallTile, tileX, tileY);
            return;
        }

        var fallback = VisualTheme.GetWallColor(Noise(tileX, tileY, 269.5f, 183.3f));
        Raylib.DrawRectangle(tileX * TileSize, tileY * TileSize, TileSize, TileSize, fallback);
    }

    private Rectangle ResolveWallTile(int x, int y)
    {
        var north = IsWall(x, y - 1);
        var west = IsWall(x - 1, y);
        var east = IsWall(x + 1, y);

        if (!north)
        {
            if (!west && east) return WallTopLeft;
            if (west && !east) return WallTopRight;
            return WallTopMid;
        }

        if (!west && east) return WallLeft;
        if (west && !east) return WallRight;
        return WallMid;
    }

    private void DrawSheetTile(Rectangle source, int tileX, int tileY)
    {
        var destination = new Rectangle(tileX * TileSize, tileY * TileSize, TileSize, TileSize);
        Raylib.DrawTexturePro(_tileSheet, source, destination, Vector2.Zero, 0f, Color.White);
    }

    private static string ResolveTileSheetPath()
    {
        var envOverride = Environment.GetEnvironmentVariable("DUNGEON_ESCAPE_TILESET_PATH");
        if (!string.IsNullOrWhiteSpace(envOverride) && File.Exists(envOverride))
        {
            return envOverride;
        }

        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "Assets", "Tiles", "0x72_DungeonTilesetII_v1.7.png"),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "Assets", "Tiles", "0x72_DungeonTilesetII_v1.7.png"))
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    private static Rectangle TileRect(int x, int y)
    {
        return new Rectangle(x, y, 16, 16);
    }

    private static float Noise(int x, int y, float k1, float k2)
    {
        var n = MathF.Sin(x * k1 + y * k2) * 43758.5453f;
        return n - MathF.Floor(n);
    }
}
