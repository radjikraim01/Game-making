namespace DungeonEscape.Core;

public enum EncounterCombatantKind
{
    Player,
    Enemy
}

public sealed record EncounterInitiativeParticipant(
    string Id,
    EncounterCombatantKind Kind,
    int InitiativeModifier,
    int StableOrder);

public sealed record EncounterInitiativeSlot(
    string Id,
    EncounterCombatantKind Kind,
    int Roll,
    int InitiativeScore,
    int StableOrder);

public static class EncounterInitiative
{
    public static IReadOnlyList<EncounterInitiativeSlot> BuildOrder(
        IEnumerable<EncounterInitiativeParticipant> participants,
        int seed)
    {
        ArgumentNullException.ThrowIfNull(participants);

        var rng = new Random(seed);
        var slots = participants
            .Select(participant =>
            {
                var roll = rng.Next(1, 21);
                var score = roll + participant.InitiativeModifier;
                return new EncounterInitiativeSlot(
                    Id: participant.Id,
                    Kind: participant.Kind,
                    Roll: roll,
                    InitiativeScore: score,
                    StableOrder: participant.StableOrder);
            })
            .ToList();

        slots.Sort(CompareSlots);
        return slots;
    }

    public static IReadOnlyList<EncounterInitiativeSlot> SortSlots(
        IEnumerable<EncounterInitiativeSlot> slots)
    {
        ArgumentNullException.ThrowIfNull(slots);

        var ordered = slots.ToList();
        ordered.Sort(CompareSlots);
        return ordered;
    }

    public static int AdvanceTurnIndex(int currentIndex, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        if (currentIndex < 0 || currentIndex >= count)
        {
            return 0;
        }

        return (currentIndex + 1) % count;
    }

    private static int CompareSlots(EncounterInitiativeSlot left, EncounterInitiativeSlot right)
    {
        var byScore = right.InitiativeScore.CompareTo(left.InitiativeScore);
        if (byScore != 0)
        {
            return byScore;
        }

        var byStableOrder = left.StableOrder.CompareTo(right.StableOrder);
        if (byStableOrder != 0)
        {
            return byStableOrder;
        }

        return string.CompareOrdinal(left.Id, right.Id);
    }
}
