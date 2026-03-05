using Raylib_cs;

namespace DungeonEscape.Core;

public static class VisualTheme
{
    // Step 1: art direction lock
    public const string StyleName = "Dark Fantasy - Ember Sanctum";
    public const string StyleIntent = "Cold stone, ember glow, heavy shadows, haunted atmosphere.";

    public static readonly Color ScreenBaseColor = new(7, 10, 18, 255);
    public static readonly Color WorldFogTint = new(12, 16, 28, 255);
    public static readonly Color VignetteEdgeColor = new(4, 5, 9, 190);
    public static readonly Color ScreenHazeColor = new(10, 14, 22, 36);

    public static readonly Color RewardNodeFill = new(208, 167, 76, 255);
    public static readonly Color RewardNodeEdge = new(246, 206, 114, 255);

    public static readonly Color PlayerLightInner = new(255, 207, 152, 62);
    public static readonly Color PlayerLightOuter = new(255, 207, 152, 0);
    public static readonly Color RewardLightInner = new(226, 183, 95, 44);
    public static readonly Color RewardLightOuter = new(226, 183, 95, 0);
    public static readonly Color BossLightInner = new(196, 72, 82, 36);
    public static readonly Color BossLightOuter = new(196, 72, 82, 0);

    public const float PlayerLightRadius = 214f;
    public const float RewardLightRadius = 98f;
    public const float BossLightRadius = 132f;
    public const byte FogMinAlpha = 78;
    public const byte FogMaxAlpha = 102;
    public const float FogPulseSpeed = 0.42f;

    public const int EmberParticleCount = 54;
    public static readonly Color EmberCoreColor = new(237, 165, 96, 185);
    public static readonly Color EmberTrailColor = new(198, 104, 58, 115);
    public const float EmberDriftMin = 7f;
    public const float EmberDriftMax = 21f;

    public static Color GetFloorColor(float noise)
    {
        var n = Math.Clamp(noise, 0f, 1f);
        var baseR = 36 + (int)(n * 13f);
        var baseG = 34 + (int)(n * 11f);
        var baseB = 44 + (int)(n * 12f);
        return new Color((byte)baseR, (byte)baseG, (byte)baseB, (byte)255);
    }

    public static Color GetWallColor(float noise)
    {
        var n = Math.Clamp(noise, 0f, 1f);
        var baseR = 24 + (int)(n * 11f);
        var baseG = 24 + (int)(n * 10f);
        var baseB = 33 + (int)(n * 14f);
        return new Color((byte)baseR, (byte)baseG, (byte)baseB, (byte)255);
    }
}
