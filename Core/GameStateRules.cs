namespace DungeonEscape.Core;

public static class GameStateRules
{
    public static bool IsSaveEligibleState(GameState state)
    {
        return state == GameState.Playing || state == GameState.Combat;
    }

    public static bool IsCombatState(GameState state)
    {
        return state == GameState.Combat ||
               state == GameState.CombatSkillMenu ||
               state == GameState.CombatSpellMenu ||
               state == GameState.CombatItemMenu;
    }

    public static GameState ResolveResumeState(GameState pausedFromState, bool hasActiveEnemy)
    {
        if (pausedFromState == GameState.Combat && hasActiveEnemy)
        {
            return GameState.Combat;
        }

        return GameState.Playing;
    }
}
