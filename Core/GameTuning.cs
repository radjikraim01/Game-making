namespace DungeonEscape.Core;

public static class GameTuning
{
    public static float CameraSmoothness { get; set; } = 10f;
    public static float CameraDeadZoneHalfWidthTiles { get; set; } = 2.5f;
    public static float CameraDeadZoneHalfHeightTiles { get; set; } = 1.75f;

    public static float EnemyVisionRangeTiles { get; set; } = 8f;
    public static float EnemyFovDegrees { get; set; } = 95f;
    public static float EnemyProximityDetectTiles { get; set; } = 2f;

    public static float EnemyPatrolStepSeconds { get; set; } = 0.60f;
    public static float EnemyInvestigateStepSeconds { get; set; } = 0.34f;
    public static float EnemyChaseStepSeconds { get; set; } = 0.24f;
    public static float EnemySearchStepSeconds { get; set; } = 0.36f;
    public static float EnemyReturnStepSeconds { get; set; } = 0.28f;

    public static float EnemySearchDurationSeconds { get; set; } = 2.8f;
    public static float EnemyChaseTimeoutSeconds { get; set; } = 7.5f;
    public static int EnemyLeashDistanceTiles { get; set; } = 14;
}
