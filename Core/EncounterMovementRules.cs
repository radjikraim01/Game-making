namespace DungeonEscape.Core;

public static class EncounterMovementRules
{
    public const int HeavyArmorPenaltyTiles = 1;
    public const int MinimumEffectiveMoveTiles = 4;

    public static int GetBaseMoveBudget(Race race)
    {
        return race switch
        {
            Race.Dwarf => 5,
            Race.Elf => 7,
            _ => 6
        };
    }

    public static int GetEffectiveMoveBudget(Race race, ArmorCategory armorCategory)
    {
        var baseMove = GetBaseMoveBudget(race);
        if (armorCategory == ArmorCategory.Heavy)
        {
            return Math.Max(MinimumEffectiveMoveTiles, baseMove - HeavyArmorPenaltyTiles);
        }

        return Math.Max(MinimumEffectiveMoveTiles, baseMove);
    }

    public static IReadOnlyCollection<(int X, int Y)> BuildReachableTiles(
        int startX,
        int startY,
        int moveBudget,
        Func<int, int, bool> canEnterTile)
    {
        ArgumentNullException.ThrowIfNull(canEnterTile);
        if (moveBudget <= 0)
        {
            return Array.Empty<(int X, int Y)>();
        }

        var visited = new Dictionary<(int X, int Y), int>
        {
            [(startX, startY)] = 0
        };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue((startX, startY));

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var currentDistance = visited[current];
            if (currentDistance >= moveBudget)
            {
                continue;
            }

            TryVisit(current.X + 1, current.Y, currentDistance + 1);
            TryVisit(current.X - 1, current.Y, currentDistance + 1);
            TryVisit(current.X, current.Y + 1, currentDistance + 1);
            TryVisit(current.X, current.Y - 1, currentDistance + 1);
        }

        var reachable = visited
            .Where(kv => kv.Value > 0 && kv.Value <= moveBudget)
            .Select(kv => kv.Key)
            .ToList();
        return reachable;

        void TryVisit(int x, int y, int distance)
        {
            if (distance > moveBudget)
            {
                return;
            }

            if (!canEnterTile(x, y))
            {
                return;
            }

            var tile = (x, y);
            if (visited.TryGetValue(tile, out var knownDistance) && knownDistance <= distance)
            {
                return;
            }

            visited[tile] = distance;
            queue.Enqueue(tile);
        }
    }
}
