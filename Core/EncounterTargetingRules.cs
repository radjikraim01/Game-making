namespace DungeonEscape.Core;

public enum EncounterTargetAction
{
    Melee,
    Ranged,
    Spell
}

public readonly record struct EncounterTargetValidation(
    bool IsLegal,
    int DistanceTiles,
    int MaxRangeTiles,
    bool InRange,
    bool HasLineOfSight,
    bool TargetAlive)
{
    public string BuildBlockedReason()
    {
        if (!TargetAlive)
        {
            return "Target is not alive.";
        }

        if (!InRange)
        {
            return $"Target out of range ({DistanceTiles}/{MaxRangeTiles} tiles).";
        }

        if (!HasLineOfSight)
        {
            return "Line of sight is blocked.";
        }

        return "Target is not legal.";
    }
}

public static class EncounterTargetingRules
{
    public static int GetTileDistance(int fromX, int fromY, int toX, int toY)
    {
        return Math.Abs(toX - fromX) + Math.Abs(toY - fromY);
    }

    public static EncounterTargetValidation Validate(
        int fromX,
        int fromY,
        int toX,
        int toY,
        bool targetAlive,
        int maxRangeTiles,
        bool requiresLineOfSight,
        Func<int, int, int, int, bool> hasLineOfSight)
    {
        ArgumentNullException.ThrowIfNull(hasLineOfSight);
        maxRangeTiles = Math.Max(1, maxRangeTiles);
        var distance = GetTileDistance(fromX, fromY, toX, toY);
        var inRange = distance <= maxRangeTiles;
        var los = !requiresLineOfSight || hasLineOfSight(fromX, fromY, toX, toY);
        var legal = targetAlive && inRange && los;
        return new EncounterTargetValidation(
            IsLegal: legal,
            DistanceTiles: distance,
            MaxRangeTiles: maxRangeTiles,
            InRange: inRange,
            HasLineOfSight: los,
            TargetAlive: targetAlive);
    }
}
