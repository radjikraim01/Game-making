using Raylib_cs;

namespace DungeonEscape.Core;

public enum SpriteMotion
{
    Idle,
    Run,
    Hit
}

public sealed class SpriteLibrary : IDisposable
{
    private const float AnimationFps = 8f;
    private const float SpriteScale = 2f;
    private const string FallbackSpriteId = "knight_m";

    private readonly Dictionary<string, Texture2D[]> _idleFrames = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Texture2D[]> _runFrames = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Texture2D[]> _hitFrames = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _framesDirectory;
    private bool _disposed;

    public bool IsReady { get; }

    public SpriteLibrary()
    {
        _framesDirectory = ResolveFramesDirectory();
        if (string.IsNullOrWhiteSpace(_framesDirectory))
        {
            RuntimeDiagnostics.Warn("Sprite library disabled: no frames directory found.");
            IsReady = false;
            return;
        }

        var spriteIds = new[]
        {
            "knight_m", "knight_f", "elf_m", "elf_f", "wizzard_m", "wizzard_f", "dwarf_m", "dwarf_f", "doc",
            "goblin", "wogol", "skelet", "masked_orc", "orc_shaman", "ogre", "big_zombie", "big_demon"
        };

        foreach (var spriteId in spriteIds)
        {
            LoadSpriteSet(spriteId);
        }

        IsReady = _idleFrames.Count > 0;
        RuntimeDiagnostics.Info($"Sprite library initialized. Ready={IsReady} Root={_framesDirectory} Sets={_idleFrames.Count}");
    }

    public bool TryDraw(string spriteId, int tileX, int tileY, SpriteMotion motion)
    {
        if (_disposed || !IsReady || string.IsNullOrWhiteSpace(spriteId))
        {
            return false;
        }

        var frames = ResolveFrames(spriteId, motion);
        if (frames.Length == 0)
        {
            return false;
        }

        var frameIndex = (int)(Raylib.GetTime() * AnimationFps) % frames.Length;
        var texture = frames[frameIndex];
        DrawTextureAtTile(texture, tileX, tileY);
        return true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var set in _idleFrames.Values)
        {
            UnloadTextures(set);
        }

        foreach (var set in _runFrames.Values)
        {
            UnloadTextures(set);
        }

        foreach (var set in _hitFrames.Values)
        {
            UnloadTextures(set);
        }

        _idleFrames.Clear();
        _runFrames.Clear();
        _hitFrames.Clear();
        _disposed = true;
    }

    private void LoadSpriteSet(string spriteId)
    {
        var idle = LoadFrames(spriteId, "idle");
        if (idle.Length == 0)
        {
            return;
        }

        _idleFrames[spriteId] = idle;
        _runFrames[spriteId] = LoadFrames(spriteId, "run");
        _hitFrames[spriteId] = LoadFrames(spriteId, "hit");
    }

    private Texture2D[] ResolveFrames(string spriteId, SpriteMotion motion)
    {
        var hit = GetFrameSet(_hitFrames, spriteId);
        var run = GetFrameSet(_runFrames, spriteId);
        var idle = GetFrameSet(_idleFrames, spriteId);

        if (motion == SpriteMotion.Hit && hit.Length > 0)
        {
            return hit;
        }

        if (motion == SpriteMotion.Run && run.Length > 0)
        {
            return run;
        }

        if (idle.Length > 0)
        {
            return idle;
        }

        if (run.Length > 0)
        {
            return run;
        }

        return hit;
    }

    private Texture2D[] GetFrameSet(Dictionary<string, Texture2D[]> source, string spriteId)
    {
        if (source.TryGetValue(spriteId, out var current) && current.Length > 0)
        {
            return current;
        }

        if (source.TryGetValue(FallbackSpriteId, out var fallback) && fallback.Length > 0)
        {
            return fallback;
        }

        return Array.Empty<Texture2D>();
    }

    private Texture2D[] LoadFrames(string spriteId, string animationName)
    {
        var searchPattern = $"{spriteId}_{animationName}_anim_f*.png";
        var files = Directory.GetFiles(_framesDirectory, searchPattern, SearchOption.TopDirectoryOnly);
        if (files.Length == 0)
        {
            return Array.Empty<Texture2D>();
        }

        Array.Sort(files, CompareFramePath);
        var textures = new Texture2D[files.Length];
        for (var i = 0; i < files.Length; i++)
        {
            textures[i] = Raylib.LoadTexture(files[i]);
        }

        return textures;
    }

    private static int CompareFramePath(string left, string right)
    {
        return ExtractFrameIndex(left).CompareTo(ExtractFrameIndex(right));
    }

    private static int ExtractFrameIndex(string path)
    {
        var stem = Path.GetFileNameWithoutExtension(path);
        var marker = stem.LastIndexOf("_f", StringComparison.OrdinalIgnoreCase);
        if (marker < 0 || marker + 2 >= stem.Length)
        {
            return int.MaxValue;
        }

        var numeric = stem[(marker + 2)..];
        return int.TryParse(numeric, out var index) ? index : int.MaxValue;
    }

    private static string ResolveFramesDirectory()
    {
        var envOverride = Environment.GetEnvironmentVariable("DUNGEON_ESCAPE_FRAMES_DIR");
        if (!string.IsNullOrWhiteSpace(envOverride) && Directory.Exists(envOverride))
        {
            return envOverride;
        }

        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "Assets", "Sprites", "frames"),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "Assets", "Sprites", "frames"))
        };

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    private static void DrawTextureAtTile(Texture2D texture, int tileX, int tileY)
    {
        var drawWidth = (int)Math.Round(texture.Width * SpriteScale);
        var drawHeight = (int)Math.Round(texture.Height * SpriteScale);

        var tilePixelX = tileX * GameMap.TileSize;
        var tilePixelY = tileY * GameMap.TileSize;

        var drawX = tilePixelX + (GameMap.TileSize - drawWidth) / 2;
        var drawY = tilePixelY + GameMap.TileSize - drawHeight;

        Raylib.DrawTextureEx(texture, new System.Numerics.Vector2(drawX, drawY), 0f, SpriteScale, Color.White);
    }

    private static void UnloadTextures(Texture2D[] textures)
    {
        foreach (var texture in textures)
        {
            if (texture.Id != 0)
            {
                Raylib.UnloadTexture(texture);
            }
        }
    }
}
