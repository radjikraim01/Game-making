namespace DungeonEscape.Core;

public static class EncounterSpellAreaRules
{
    public static IReadOnlyCollection<(int X, int Y)> EnumerateAffectedTiles(
        SpellDefinition spell,
        SpellEffectRouteSpec route,
        int casterX,
        int casterY,
        int anchorX,
        int anchorY)
    {
        ArgumentNullException.ThrowIfNull(spell);
        ArgumentNullException.ThrowIfNull(route);

        return route.TargetShape switch
        {
            SpellTargetShape.SingleEnemy => new[] { (anchorX, anchorY) },
            SpellTargetShape.Tile => route.AreaRadiusTiles > 0
                ? EnumerateRadiusTiles(anchorX, anchorY, route.AreaRadiusTiles)
                : new[] { (anchorX, anchorY) },
            SpellTargetShape.Self => EnumerateRadiusTiles(casterX, casterY, Math.Max(0, route.AreaRadiusTiles)),
            SpellTargetShape.Radius => EnumerateRadiusTiles(anchorX, anchorY, Math.Max(0, route.AreaRadiusTiles)),
            SpellTargetShape.Line => TraceLine(casterX, casterY, anchorX, anchorY, Math.Max(1, EncounterSpellTargetingRangePolicy.ResolveSpellRangeTiles(spell))).ToList(),
            SpellTargetShape.Cone => EnumerateConeTiles(casterX, casterY, anchorX, anchorY, Math.Max(1, route.AreaRadiusTiles)),
            _ => Array.Empty<(int X, int Y)>()
        };
    }

    public static IReadOnlyList<Enemy> ResolveAffectedEnemies(
        SpellDefinition spell,
        SpellEffectRouteSpec route,
        int casterX,
        int casterY,
        int anchorX,
        int anchorY,
        IEnumerable<Enemy> candidates,
        Func<int, int, int, int, bool> hasLineOfSight)
    {
        ArgumentNullException.ThrowIfNull(spell);
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(hasLineOfSight);

        var affected = new List<Enemy>();
        foreach (var enemy in candidates)
        {
            if (!enemy.IsAlive)
            {
                continue;
            }

            if (!IsEnemyAffected(spell, route, casterX, casterY, anchorX, anchorY, enemy, hasLineOfSight))
            {
                continue;
            }

            affected.Add(enemy);
        }

        return affected;
    }

    public static IReadOnlyCollection<(int X, int Y)> EnumerateRadiusTiles(int centerX, int centerY, int radiusTiles)
    {
        radiusTiles = Math.Max(0, radiusTiles);
        var tiles = new List<(int X, int Y)>();
        for (var y = centerY - radiusTiles; y <= centerY + radiusTiles; y++)
        {
            for (var x = centerX - radiusTiles; x <= centerX + radiusTiles; x++)
            {
                if (EncounterTargetingRules.GetTileDistance(centerX, centerY, x, y) > radiusTiles)
                {
                    continue;
                }

                tiles.Add((x, y));
            }
        }

        return tiles;
    }

    private static bool IsEnemyAffected(
        SpellDefinition spell,
        SpellEffectRouteSpec route,
        int casterX,
        int casterY,
        int anchorX,
        int anchorY,
        Enemy enemy,
        Func<int, int, int, int, bool> hasLineOfSight)
    {
        var affectedTiles = EnumerateAffectedTiles(spell, route, casterX, casterY, anchorX, anchorY);
        if (route.TargetShape is SpellTargetShape.SingleEnemy or SpellTargetShape.Tile or SpellTargetShape.Line)
        {
            return affectedTiles.Any(tile => tile.X == enemy.X && tile.Y == enemy.Y);
        }

        return route.TargetShape switch
        {
            SpellTargetShape.Self => IsEnemyWithinRadius(casterX, casterY, enemy.X, enemy.Y, route.AreaRadiusTiles, hasLineOfSight),
            SpellTargetShape.Radius => IsEnemyWithinRadius(anchorX, anchorY, enemy.X, enemy.Y, route.AreaRadiusTiles, hasLineOfSight),
            SpellTargetShape.Cone => IsEnemyInCone(casterX, casterY, anchorX, anchorY, enemy.X, enemy.Y, Math.Max(1, route.AreaRadiusTiles), hasLineOfSight),
            _ => false
        };
    }

    private static bool IsEnemyWithinRadius(
        int centerX,
        int centerY,
        int enemyX,
        int enemyY,
        int radiusTiles,
        Func<int, int, int, int, bool> hasLineOfSight)
    {
        radiusTiles = Math.Max(0, radiusTiles);
        var distance = EncounterTargetingRules.GetTileDistance(centerX, centerY, enemyX, enemyY);
        if (distance > radiusTiles)
        {
            return false;
        }

        return hasLineOfSight(centerX, centerY, enemyX, enemyY);
    }

    private static bool IsEnemyInCone(
        int casterX,
        int casterY,
        int anchorX,
        int anchorY,
        int enemyX,
        int enemyY,
        int lengthTiles,
        Func<int, int, int, int, bool> hasLineOfSight)
    {
        var dx = anchorX - casterX;
        var dy = anchorY - casterY;
        if (dx == 0 && dy == 0)
        {
            return false;
        }

        int forward;
        int lateral;
        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            var sign = Math.Sign(dx);
            forward = (enemyX - casterX) * sign;
            lateral = Math.Abs(enemyY - casterY);
        }
        else
        {
            var sign = Math.Sign(dy);
            forward = (enemyY - casterY) * sign;
            lateral = Math.Abs(enemyX - casterX);
        }

        if (forward < 1 || forward > lengthTiles)
        {
            return false;
        }

        if (lateral > Math.Max(0, forward - 1))
        {
            return false;
        }

        return hasLineOfSight(casterX, casterY, enemyX, enemyY);
    }

    private static IReadOnlyCollection<(int X, int Y)> EnumerateConeTiles(
        int casterX,
        int casterY,
        int anchorX,
        int anchorY,
        int lengthTiles)
    {
        lengthTiles = Math.Max(1, lengthTiles);
        var tiles = new List<(int X, int Y)>();
        for (var y = casterY - lengthTiles; y <= casterY + lengthTiles; y++)
        {
            for (var x = casterX - lengthTiles; x <= casterX + lengthTiles; x++)
            {
                if (x == casterX && y == casterY)
                {
                    continue;
                }

                if (!IsTileInCone(casterX, casterY, anchorX, anchorY, x, y, lengthTiles))
                {
                    continue;
                }

                tiles.Add((x, y));
            }
        }

        return tiles;
    }

    private static bool IsTileInCone(
        int casterX,
        int casterY,
        int anchorX,
        int anchorY,
        int tileX,
        int tileY,
        int lengthTiles)
    {
        var dx = anchorX - casterX;
        var dy = anchorY - casterY;
        if (dx == 0 && dy == 0)
        {
            return false;
        }

        int forward;
        int lateral;
        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            var sign = Math.Sign(dx);
            forward = (tileX - casterX) * sign;
            lateral = Math.Abs(tileY - casterY);
        }
        else
        {
            var sign = Math.Sign(dy);
            forward = (tileY - casterY) * sign;
            lateral = Math.Abs(tileX - casterX);
        }

        if (forward < 1 || forward > lengthTiles)
        {
            return false;
        }

        return lateral <= Math.Max(0, forward - 1);
    }

    private static IEnumerable<(int X, int Y)> TraceLine(int startX, int startY, int endX, int endY, int maxTiles)
    {
        maxTiles = Math.Max(1, maxTiles);
        var tiles = new List<(int X, int Y)>();

        var x0 = startX;
        var y0 = startY;
        var x1 = endX;
        var y1 = endY;
        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;
        var steps = 0;

        while (steps < maxTiles)
        {
            if (!(x0 == startX && y0 == startY))
            {
                tiles.Add((x0, y0));
                steps += 1;
                if (steps >= maxTiles)
                {
                    break;
                }
            }

            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            var err2 = err * 2;
            if (err2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (err2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }

        return tiles;
    }
}
