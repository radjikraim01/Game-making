namespace DungeonEscape.Core;

public enum AttackRollResult { Miss, Hit, CriticalHit }

public static class CombatMath
{
    public static int CalculateMeleeBaseDamage(
        int strengthModifier,
        int playerMeleeBonus,
        int classMeleeBonus,
        int runMeleeBonus)
    {
        return Math.Max(1, strengthModifier + 1 + playerMeleeBonus + classMeleeBonus + runMeleeBonus);
    }

    public static int CalculateCritChancePercent(
        int dexterityModifier,
        int playerCritBonus,
        int classCritBonus,
        int runCritBonus)
    {
        return Math.Max(5, 5 + dexterityModifier * 2 + playerCritBonus + classCritBonus + runCritBonus);
    }

    public static int CalculateFleeChancePercent(
        int playerFleeBonus,
        int classFleeBonus,
        int runFleeBonus)
    {
        return Math.Clamp(50 + playerFleeBonus + classFleeBonus + runFleeBonus, 5, 95);
    }

    public static int CalculateSpellRawDamage(int baseDamage, int statPower, int varianceRoll)
    {
        return Math.Max(0, baseDamage + statPower + varianceRoll);
    }

    public static int CalculateFinalDamage(int rawDamage, int defense, int armorBypass, int minimumDamage = 1)
    {
        var effectiveArmor = Math.Max(0, defense - armorBypass);
        return Math.Max(minimumDamage, rawDamage - effectiveArmor);
    }

    public static int CalculateEnemyDamage(int enemyAttackRoll, int defense, int minimumDamage = 1)
    {
        return Math.Max(minimumDamage, enemyAttackRoll - defense);
    }

    /// <summary>Roll d20 attack vs target AC. Handles advantage/disadvantage internally.</summary>
    public static (AttackRollResult result, int d20Raw, int total) RollAttack(
        int attackBonus, int targetAC, Random rng,
        bool advantage = false, bool disadvantage = false,
        int critThreshold = 20)
    {
        // Advantage and disadvantage cancel each other
        bool hasAdv = advantage && !disadvantage;
        bool hasDisadv = disadvantage && !advantage;

        int roll1 = rng.Next(1, 21);
        int roll2 = (hasAdv || hasDisadv) ? rng.Next(1, 21) : roll1;

        int d20Raw;
        if (hasAdv)
            d20Raw = Math.Max(roll1, roll2);
        else if (hasDisadv)
            d20Raw = Math.Min(roll1, roll2);
        else
            d20Raw = roll1;

        // Natural 1 = always miss, at-or-above critThreshold = always crit
        if (d20Raw == 1) return (AttackRollResult.Miss, d20Raw, d20Raw + attackBonus);
        if (d20Raw >= critThreshold) return (AttackRollResult.CriticalHit, d20Raw, d20Raw + attackBonus);

        int total = d20Raw + attackBonus;
        return total >= targetAC
            ? (AttackRollResult.Hit, d20Raw, total)
            : (AttackRollResult.Miss, d20Raw, total);
    }

    /// <summary>Roll d20 save vs DC. Returns true if save succeeded.</summary>
    public static (bool success, int d20Raw, int total) RollSave(
        int dc, int saveBonus, Random rng,
        bool advantage = false, bool disadvantage = false)
    {
        bool hasAdv = advantage && !disadvantage;
        bool hasDisadv = disadvantage && !advantage;

        int roll1 = rng.Next(1, 21);
        int roll2 = (hasAdv || hasDisadv) ? rng.Next(1, 21) : roll1;

        int d20Raw;
        if (hasAdv)
            d20Raw = Math.Max(roll1, roll2);
        else if (hasDisadv)
            d20Raw = Math.Min(roll1, roll2);
        else
            d20Raw = roll1;

        int total = d20Raw + saveBonus;
        return (total >= dc, d20Raw, total);
    }
}

