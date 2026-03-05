namespace DungeonEscape.Core;

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
}

