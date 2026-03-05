namespace DungeonEscape.Core;

public readonly record struct EncounterEnemyMoveDecision(
    int StartX,
    int StartY,
    int TargetX,
    int TargetY,
    int MoveBudgetTiles,
    IReadOnlyList<(int X, int Y)> Steps,
    bool ReachedTargetTile)
{
    public bool ShouldMove => Steps.Count > 0;

    public (int X, int Y) Destination => ShouldMove ? Steps[^1] : (StartX, StartY);
}

public readonly record struct EncounterEnemyAttackDecision(
    bool CanAttack,
    EncounterTargetValidation Validation);

public static class EncounterEnemyTactics
{
    private static readonly (int Dx, int Dy)[] NeighborOffsets =
    {
        (1, 0),
        (-1, 0),
        (0, 1),
        (0, -1)
    };

    public static EncounterEnemyMoveDecision DecideMoveTowardTarget(
        int startX,
        int startY,
        int targetX,
        int targetY,
        int maxMoveTiles,
        Func<int, int, bool> canEnterTile)
    {
        ArgumentNullException.ThrowIfNull(canEnterTile);

        var moveBudget = Math.Max(0, maxMoveTiles);
        var origin = (X: startX, Y: startY);
        if (moveBudget == 0)
        {
            return BuildNoMoveDecision(startX, startY, targetX, targetY, moveBudget);
        }

        var visitedDistance = new Dictionary<(int X, int Y), int>
        {
            [origin] = 0
        };
        var previous = new Dictionary<(int X, int Y), (int X, int Y)>();
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(origin);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var currentDistance = visitedDistance[current];
            if (currentDistance >= moveBudget)
            {
                continue;
            }

            foreach (var (dx, dy) in NeighborOffsets)
            {
                var next = (X: current.X + dx, Y: current.Y + dy);
                if (visitedDistance.ContainsKey(next))
                {
                    continue;
                }

                if (!canEnterTile(next.X, next.Y))
                {
                    continue;
                }

                visitedDistance[next] = currentDistance + 1;
                previous[next] = current;
                queue.Enqueue(next);
            }
        }

        var bestDestination = SelectBestDestination(visitedDistance, origin, targetX, targetY);
        if (!bestDestination.HasValue)
        {
            return BuildNoMoveDecision(startX, startY, targetX, targetY, moveBudget);
        }

        var steps = BuildSteps(origin, bestDestination.Value, previous);
        return new EncounterEnemyMoveDecision(
            StartX: startX,
            StartY: startY,
            TargetX: targetX,
            TargetY: targetY,
            MoveBudgetTiles: moveBudget,
            Steps: steps,
            ReachedTargetTile: bestDestination.Value.X == targetX && bestDestination.Value.Y == targetY);
    }

    public static EncounterEnemyAttackDecision EvaluateAttackFeasibility(
        int attackerX,
        int attackerY,
        int targetX,
        int targetY,
        bool targetAlive,
        int maxRangeTiles,
        bool requiresLineOfSight,
        Func<int, int, int, int, bool> hasLineOfSight)
    {
        var validation = EncounterTargetingRules.Validate(
            fromX: attackerX,
            fromY: attackerY,
            toX: targetX,
            toY: targetY,
            targetAlive: targetAlive,
            maxRangeTiles: maxRangeTiles,
            requiresLineOfSight: requiresLineOfSight,
            hasLineOfSight: hasLineOfSight);
        return new EncounterEnemyAttackDecision(
            CanAttack: validation.IsLegal,
            Validation: validation);
    }

    private static EncounterEnemyMoveDecision BuildNoMoveDecision(
        int startX,
        int startY,
        int targetX,
        int targetY,
        int moveBudget)
    {
        return new EncounterEnemyMoveDecision(
            StartX: startX,
            StartY: startY,
            TargetX: targetX,
            TargetY: targetY,
            MoveBudgetTiles: moveBudget,
            Steps: Array.Empty<(int X, int Y)>(),
            ReachedTargetTile: false);
    }

    private static (int X, int Y)? SelectBestDestination(
        IReadOnlyDictionary<(int X, int Y), int> visitedDistance,
        (int X, int Y) origin,
        int targetX,
        int targetY)
    {
        var startDistance = EncounterTargetingRules.GetTileDistance(origin.X, origin.Y, targetX, targetY);
        (int X, int Y)? best = null;
        var bestDistanceToTarget = int.MaxValue;
        var bestPathLength = int.MaxValue;

        foreach (var entry in visitedDistance)
        {
            var tile = entry.Key;
            var pathLength = entry.Value;
            if (pathLength <= 0)
            {
                continue;
            }

            var distanceToTarget = EncounterTargetingRules.GetTileDistance(tile.X, tile.Y, targetX, targetY);
            if (distanceToTarget >= startDistance)
            {
                continue;
            }

            if (best == null
                || distanceToTarget < bestDistanceToTarget
                || (distanceToTarget == bestDistanceToTarget && pathLength < bestPathLength)
                || (distanceToTarget == bestDistanceToTarget && pathLength == bestPathLength && CompareTiles(tile, best.Value) < 0))
            {
                best = tile;
                bestDistanceToTarget = distanceToTarget;
                bestPathLength = pathLength;
            }
        }

        return best;
    }

    private static List<(int X, int Y)> BuildSteps(
        (int X, int Y) origin,
        (int X, int Y) destination,
        IReadOnlyDictionary<(int X, int Y), (int X, int Y)> previous)
    {
        var reversed = new List<(int X, int Y)>();
        var cursor = destination;
        while (cursor != origin)
        {
            reversed.Add(cursor);
            if (!previous.TryGetValue(cursor, out cursor))
            {
                throw new InvalidOperationException("Encounter enemy movement path reconstruction failed.");
            }
        }

        reversed.Reverse();
        return reversed;
    }

    private static int CompareTiles((int X, int Y) left, (int X, int Y) right)
    {
        var xComparison = left.X.CompareTo(right.X);
        if (xComparison != 0)
        {
            return xComparison;
        }

        return left.Y.CompareTo(right.Y);
    }
}
