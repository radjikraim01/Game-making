namespace DungeonEscape.Core;

public enum EncounterSpellTargetMode
{
    Disabled = 0,
    SelectTarget = 1,
    ConfirmTarget = 2
}

public static class EncounterSpellTargetingRangePolicy
{
    public const int MinimumRangeTiles = 1;
    public const int CantripRangeTiles = 5;
    public const int Level1RangeTiles = 6;
    public const int Level2RangeTiles = 7;
    public const int Level3PlusRangeTiles = 8;

    public static int ResolveSpellRangeTiles(SpellDefinition spell)
    {
        ArgumentNullException.ThrowIfNull(spell);
        return ResolveSpellRangeTiles(spell.SpellLevel);
    }

    public static int ResolveSpellRangeTiles(int spellLevel)
    {
        var range = spellLevel switch
        {
            <= 0 => CantripRangeTiles,
            1 => Level1RangeTiles,
            2 => Level2RangeTiles,
            _ => Level3PlusRangeTiles
        };

        return Math.Max(MinimumRangeTiles, range);
    }
}

public static class EncounterSpellTargetingRules
{
    public static bool IsExplicitMode(EncounterSpellTargetMode mode)
    {
        return mode is EncounterSpellTargetMode.SelectTarget or EncounterSpellTargetMode.ConfirmTarget;
    }

    public static EncounterSpellTargetMode ResolveMode(bool spellMenuOpen, bool hasTargetCandidates, bool requiresConfirmation)
    {
        if (!spellMenuOpen || !hasTargetCandidates)
        {
            return EncounterSpellTargetMode.Disabled;
        }

        return requiresConfirmation
            ? EncounterSpellTargetMode.ConfirmTarget
            : EncounterSpellTargetMode.SelectTarget;
    }

    public static int CycleTargetIndex(int currentIndex, int candidateCount, int direction)
    {
        if (candidateCount <= 0)
        {
            return -1;
        }

        var clampedCurrent = Math.Clamp(currentIndex, 0, candidateCount - 1);
        var step = Math.Sign(direction);
        if (step == 0)
        {
            return clampedCurrent;
        }

        var shifted = clampedCurrent + step;
        if (shifted < 0)
        {
            return candidateCount - 1;
        }

        if (shifted >= candidateCount)
        {
            return 0;
        }

        return shifted;
    }

    public static EncounterTargetValidation ValidateSpellTarget(
        SpellDefinition spell,
        int fromX,
        int fromY,
        int toX,
        int toY,
        bool targetAlive,
        bool requiresLineOfSight,
        Func<int, int, int, int, bool> hasLineOfSight)
    {
        ArgumentNullException.ThrowIfNull(spell);
        return EncounterTargetingRules.Validate(
            fromX,
            fromY,
            toX,
            toY,
            targetAlive,
            EncounterSpellTargetingRangePolicy.ResolveSpellRangeTiles(spell),
            requiresLineOfSight,
            hasLineOfSight);
    }
}
