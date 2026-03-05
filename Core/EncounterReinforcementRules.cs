namespace DungeonEscape.Core;

internal readonly record struct EncounterReinforcementMember(
    int X,
    int Y,
    string? EnemyKey);

internal static class EncounterReinforcementRules
{
    public static bool HasOpenEncounterSlot(int encounterSize, int maxEncounterSize)
    {
        return encounterSize < maxEncounterSize;
    }

    public static bool IsCandidateEligible(
        EncounterReinforcementMember candidate,
        IReadOnlyList<EncounterReinforcementMember> aliveEncounterMembers,
        int joinDistanceTiles,
        Func<string?, string?, bool> areEncounterAllies,
        Func<int, int, int, int, bool> hasLineOfSight)
    {
        ArgumentNullException.ThrowIfNull(aliveEncounterMembers);
        ArgumentNullException.ThrowIfNull(areEncounterAllies);
        ArgumentNullException.ThrowIfNull(hasLineOfSight);

        if (string.IsNullOrWhiteSpace(candidate.EnemyKey))
        {
            return false;
        }

        for (var i = 0; i < aliveEncounterMembers.Count; i += 1)
        {
            var ally = aliveEncounterMembers[i];
            if (!areEncounterAllies(ally.EnemyKey, candidate.EnemyKey))
            {
                continue;
            }

            var distance = Math.Abs(candidate.X - ally.X) + Math.Abs(candidate.Y - ally.Y);
            if (distance > joinDistanceTiles)
            {
                continue;
            }

            if (!hasLineOfSight(ally.X, ally.Y, candidate.X, candidate.Y))
            {
                continue;
            }

            return true;
        }

        return false;
    }
}
