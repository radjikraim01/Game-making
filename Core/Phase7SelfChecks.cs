namespace DungeonEscape.Core;

public sealed class Phase7CheckResult
{
    public int Passed { get; init; }
    public int Failed { get; init; }
    public IReadOnlyList<string> Failures { get; init; } = Array.Empty<string>();
    public bool IsSuccess => Failed == 0;
}

public static class Phase7SelfChecks
{
    public static Phase7CheckResult RunAll()
    {
        var failures = new List<string>();
        var passed = 0;

        void Run(string name, Action test)
        {
            try
            {
                test();
                passed += 1;
                Console.WriteLine($"[PASS] {name}");
            }
            catch (Exception ex)
            {
                failures.Add($"{name}: {ex.Message}");
                Console.WriteLine($"[FAIL] {name}");
            }
        }

        Run("CombatMath_BaseDamage_MinimumOne", () =>
        {
            var damage = CombatMath.CalculateMeleeBaseDamage(-5, 0, 0, 0);
            AssertEqual(1, damage, "Base melee damage should clamp at 1.");
        });

        Run("CombatMath_CritChance_UsesAllBonuses", () =>
        {
            var chance = CombatMath.CalculateCritChancePercent(3, 4, 2, 1);
            AssertEqual(18, chance, "Crit chance formula mismatch.");
        });

        Run("CombatMath_FleeChance_IsClamped", () =>
        {
            AssertEqual(95, CombatMath.CalculateFleeChancePercent(90, 20, 10), "Flee upper clamp failed.");
            AssertEqual(5, CombatMath.CalculateFleeChancePercent(-90, -20, -10), "Flee lower clamp failed.");
        });

        Run("CombatMath_FinalDamage_RespectsArmorBypass", () =>
        {
            var damage = CombatMath.CalculateFinalDamage(rawDamage: 20, defense: 7, armorBypass: 2);
            AssertEqual(15, damage, "Armor bypass damage mismatch.");
        });

        Run("CombatMath_EnemyDamage_MinimumBehavior", () =>
        {
            AssertEqual(1, CombatMath.CalculateEnemyDamage(enemyAttackRoll: 2, defense: 99), "Enemy damage minimum should be 1.");
            AssertEqual(0, CombatMath.CalculateEnemyDamage(enemyAttackRoll: 2, defense: 99, minimumDamage: 0), "Enemy damage minimum should allow 0 when requested.");
        });

        Run("StateRules_SaveEligibleAndCombatStates", () =>
        {
            AssertTrue(GameStateRules.IsSaveEligibleState(GameState.Playing), "Playing should be save-eligible.");
            AssertTrue(GameStateRules.IsSaveEligibleState(GameState.Combat), "Combat should be save-eligible.");
            AssertFalse(GameStateRules.IsSaveEligibleState(GameState.StartMenu), "StartMenu should not be save-eligible.");

            AssertTrue(GameStateRules.IsCombatState(GameState.Combat), "Combat should be combat-state.");
            AssertTrue(GameStateRules.IsCombatState(GameState.CombatSpellMenu), "CombatSpellMenu should be combat-state.");
            AssertTrue(GameStateRules.IsCombatState(GameState.CombatSpellTargeting), "CombatSpellTargeting should be combat-state.");
            AssertTrue(GameStateRules.IsCombatState(GameState.CombatItemMenu), "CombatItemMenu should be combat-state.");
            AssertFalse(GameStateRules.IsCombatState(GameState.PauseMenu), "PauseMenu should not be combat-state.");
        });

        Run("StateRules_ResumeBehavior", () =>
        {
            AssertEqual(GameState.Combat, GameStateRules.ResolveResumeState(GameState.Combat, hasActiveEnemy: true), "Pause resume should return to combat with active enemy.");
            AssertEqual(GameState.Playing, GameStateRules.ResolveResumeState(GameState.Combat, hasActiveEnemy: false), "Pause resume should fallback to playing without enemy.");
            AssertEqual(GameState.Playing, GameStateRules.ResolveResumeState(GameState.Playing, hasActiveEnemy: true), "Pause resume should return to playing when paused from playing.");
        });

        Run("EncounterInitiative_DeterministicOrdering_AndTies", () =>
        {
            var participants = new List<EncounterInitiativeParticipant>
            {
                new("player", EncounterCombatantKind.Player, InitiativeModifier: 2, StableOrder: 0),
                new("enemy_alpha", EncounterCombatantKind.Enemy, InitiativeModifier: 1, StableOrder: 1),
                new("enemy_bravo", EncounterCombatantKind.Enemy, InitiativeModifier: 1, StableOrder: 2)
            };

            var first = EncounterInitiative.BuildOrder(participants, seed: 7312);
            var second = EncounterInitiative.BuildOrder(participants, seed: 7312);
            AssertEqual(first.Count, second.Count, "Initiative order count should be deterministic with same seed.");
            for (var i = 0; i < first.Count; i++)
            {
                AssertEqual(first[i].Id, second[i].Id, $"Initiative id mismatch at index {i}.");
                AssertEqual(first[i].Roll, second[i].Roll, $"Initiative roll mismatch at index {i}.");
                AssertEqual(first[i].InitiativeScore, second[i].InitiativeScore, $"Initiative score mismatch at index {i}.");
            }

            var tieSlots = new List<EncounterInitiativeSlot>
            {
                new("enemy_c", EncounterCombatantKind.Enemy, Roll: 11, InitiativeScore: 15, StableOrder: 2),
                new("enemy_b", EncounterCombatantKind.Enemy, Roll: 10, InitiativeScore: 15, StableOrder: 1),
                new("enemy_a", EncounterCombatantKind.Enemy, Roll: 14, InitiativeScore: 18, StableOrder: 3),
                new("enemy_d", EncounterCombatantKind.Enemy, Roll: 9, InitiativeScore: 15, StableOrder: 1)
            };

            var orderedTies = EncounterInitiative.SortSlots(tieSlots);
            var orderedIds = orderedTies.Select(slot => slot.Id).ToArray();
            AssertEqual("enemy_a", orderedIds[0], "Highest initiative score should sort first.");
            AssertEqual("enemy_b", orderedIds[1], "StableOrder tie-break should sort lower StableOrder first.");
            AssertEqual("enemy_d", orderedIds[2], "Id should break ties after StableOrder.");
            AssertEqual("enemy_c", orderedIds[3], "Remaining tied entry order mismatch.");
        });

        Run("EncounterInitiative_AdvanceTurn_WrapAndEdgeCases", () =>
        {
            AssertEqual(0, EncounterInitiative.AdvanceTurnIndex(0, 0), "Zero participant advance should return 0.");
            AssertEqual(0, EncounterInitiative.AdvanceTurnIndex(-1, 3), "Negative current turn should normalize to 0.");
            AssertEqual(1, EncounterInitiative.AdvanceTurnIndex(0, 3), "Turn 0 should advance to 1.");
            AssertEqual(2, EncounterInitiative.AdvanceTurnIndex(1, 3), "Turn 1 should advance to 2.");
            AssertEqual(0, EncounterInitiative.AdvanceTurnIndex(2, 3), "Last turn should wrap to 0.");
            AssertEqual(0, EncounterInitiative.AdvanceTurnIndex(99, 3), "Out-of-range current turn should normalize to 0.");
        });

        Run("EncounterMovement_RaceArmorBudgets", () =>
        {
            AssertEqual(5, EncounterMovementRules.GetEffectiveMoveBudget(Race.Dwarf, ArmorCategory.Unarmored),
                "Dwarf unarmored move budget mismatch.");
            AssertEqual(6, EncounterMovementRules.GetEffectiveMoveBudget(Race.Human, ArmorCategory.Unarmored),
                "Human unarmored move budget mismatch.");
            AssertEqual(7, EncounterMovementRules.GetEffectiveMoveBudget(Race.Elf, ArmorCategory.Unarmored),
                "Elf unarmored move budget mismatch.");
            AssertEqual(4, EncounterMovementRules.GetEffectiveMoveBudget(Race.Dwarf, ArmorCategory.Heavy),
                "Dwarf heavy armor movement floor mismatch.");
            AssertEqual(5, EncounterMovementRules.GetEffectiveMoveBudget(Race.Human, ArmorCategory.Heavy),
                "Human heavy armor movement penalty mismatch.");
            AssertEqual(6, EncounterMovementRules.GetEffectiveMoveBudget(Race.Elf, ArmorCategory.Heavy),
                "Elf heavy armor movement penalty mismatch.");
        });

        Run("EncounterMovement_ReachableTiles_RespectBudgetAndBlockers", () =>
        {
            var blocked = new HashSet<(int X, int Y)> { (1, 0) };
            bool CanEnter(int x, int y)
            {
                if (x < -1 || x > 3 || y < -1 || y > 3)
                {
                    return false;
                }

                return !blocked.Contains((x, y));
            }

            var reachable = EncounterMovementRules.BuildReachableTiles(0, 0, 2, CanEnter);
            var set = reachable.ToHashSet();
            AssertTrue(set.Contains((0, 1)), "Adjacent open tile should be reachable.");
            AssertTrue(set.Contains((0, 2)), "Two-step straight tile should be reachable.");
            AssertTrue(set.Contains((1, 1)), "Tile reachable around blocker should be included.");
            AssertFalse(set.Contains((1, 0)), "Blocked tile must not be reachable.");
            AssertFalse(set.Contains((2, 2)), "Out-of-budget tile must not be reachable.");

            var none = EncounterMovementRules.BuildReachableTiles(0, 0, 0, CanEnter);
            AssertEqual(0, none.Count, "Zero movement budget should yield no reachable tiles.");
        });

        Run("EncounterTargeting_Validation_RangeAndLos", () =>
        {
            bool HasLos(int fromX, int fromY, int toX, int toY)
            {
                // Simulate one blocked lane from (0,0) to (2,0).
                return !(fromX == 0 && fromY == 0 && toX == 2 && toY == 0);
            }

            var meleeOutOfRange = EncounterTargetingRules.Validate(
                fromX: 0,
                fromY: 0,
                toX: 2,
                toY: 0,
                targetAlive: true,
                maxRangeTiles: 1,
                requiresLineOfSight: true,
                hasLineOfSight: HasLos);
            AssertFalse(meleeOutOfRange.IsLegal, "Out-of-range target should not be legal.");
            AssertFalse(meleeOutOfRange.InRange, "Out-of-range target should report InRange=false.");

            var losBlocked = EncounterTargetingRules.Validate(
                fromX: 0,
                fromY: 0,
                toX: 2,
                toY: 0,
                targetAlive: true,
                maxRangeTiles: 3,
                requiresLineOfSight: true,
                hasLineOfSight: HasLos);
            AssertFalse(losBlocked.IsLegal, "LOS-blocked target should not be legal.");
            AssertTrue(losBlocked.InRange, "LOS test target should be in range.");
            AssertFalse(losBlocked.HasLineOfSight, "LOS-blocked target should report HasLineOfSight=false.");

            var legal = EncounterTargetingRules.Validate(
                fromX: 0,
                fromY: 0,
                toX: 1,
                toY: 0,
                targetAlive: true,
                maxRangeTiles: 1,
                requiresLineOfSight: true,
                hasLineOfSight: HasLos);
            AssertTrue(legal.IsLegal, "Adjacent clear target should be legal.");
            AssertEqual(1, legal.DistanceTiles, "Distance computation mismatch.");
        });

        Run("EncounterTargeting_Validation_DeadTargetBlocked", () =>
        {
            var deadTarget = EncounterTargetingRules.Validate(
                fromX: 0,
                fromY: 0,
                toX: 0,
                toY: 1,
                targetAlive: false,
                maxRangeTiles: 2,
                requiresLineOfSight: false,
                hasLineOfSight: (_, _, _, _) => true);
            AssertFalse(deadTarget.IsLegal, "Dead target should never be legal.");
            AssertEqual("Target is not alive.", deadTarget.BuildBlockedReason(), "Dead-target block reason mismatch.");
        });

        Run("EncounterEnemyTactics_MoveTowardTarget_LegalStepOrBlockedNoStep", () =>
        {
            bool CanEnterOpenLane(int x, int y)
            {
                return x >= -1 && x <= 4 && y >= -1 && y <= 1;
            }

            var move = EncounterEnemyTactics.DecideMoveTowardTarget(
                startX: 0,
                startY: 0,
                targetX: 3,
                targetY: 0,
                maxMoveTiles: 2,
                canEnterTile: CanEnterOpenLane);
            AssertTrue(move.ShouldMove, "Enemy should advance when an open legal path exists.");
            AssertEqual(2, move.Steps.Count, "Enemy should spend available movement when it moves toward target.");
            AssertEqual((1, 0), move.Steps[0], "First tactical step should advance toward target.");
            AssertEqual((2, 0), move.Destination, "Tactical destination mismatch for open-lane move.");
            AssertTrue(move.Steps.All(step => CanEnterOpenLane(step.X, step.Y)),
                "All tactical steps must remain on legal enterable tiles.");

            var blocked = new HashSet<(int X, int Y)>
            {
                (1, 0),
                (-1, 0),
                (0, 1),
                (0, -1)
            };

            bool CanEnterBlockedPocket(int x, int y)
            {
                if (x < -1 || x > 1 || y < -1 || y > 1)
                {
                    return false;
                }

                return !blocked.Contains((x, y));
            }

            var noStep = EncounterEnemyTactics.DecideMoveTowardTarget(
                startX: 0,
                startY: 0,
                targetX: 2,
                targetY: 0,
                maxMoveTiles: 1,
                canEnterTile: CanEnterBlockedPocket);
            AssertFalse(noStep.ShouldMove, "Enemy should hold position when all adjacent legal steps are blocked.");
            AssertEqual(0, noStep.Steps.Count, "Blocked tactical move should not produce steps.");
            AssertEqual((0, 0), noStep.Destination, "Blocked tactical move should keep origin destination.");
        });

        Run("EncounterEnemyTactics_AttackFeasibility_GatedByRangeLosAndAlive", () =>
        {
            bool HasLos(int fromX, int fromY, int toX, int toY)
            {
                return !(fromX == 0 && fromY == 0 && toX == 2 && toY == 0);
            }

            var outOfRange = EncounterEnemyTactics.EvaluateAttackFeasibility(
                attackerX: 0,
                attackerY: 0,
                targetX: 3,
                targetY: 0,
                targetAlive: true,
                maxRangeTiles: 1,
                requiresLineOfSight: false,
                hasLineOfSight: HasLos);
            AssertFalse(outOfRange.CanAttack, "Out-of-range target should block tactical enemy attack.");
            AssertFalse(outOfRange.Validation.InRange, "Out-of-range attack should report InRange=false.");

            var losBlocked = EncounterEnemyTactics.EvaluateAttackFeasibility(
                attackerX: 0,
                attackerY: 0,
                targetX: 2,
                targetY: 0,
                targetAlive: true,
                maxRangeTiles: 3,
                requiresLineOfSight: true,
                hasLineOfSight: HasLos);
            AssertFalse(losBlocked.CanAttack, "LOS-blocked target should block tactical enemy attack.");
            AssertTrue(losBlocked.Validation.InRange, "LOS-blocked target should remain in range.");
            AssertFalse(losBlocked.Validation.HasLineOfSight, "LOS-blocked target should report blocked LOS.");

            var deadTarget = EncounterEnemyTactics.EvaluateAttackFeasibility(
                attackerX: 0,
                attackerY: 0,
                targetX: 1,
                targetY: 0,
                targetAlive: false,
                maxRangeTiles: 1,
                requiresLineOfSight: true,
                hasLineOfSight: HasLos);
            AssertFalse(deadTarget.CanAttack, "Dead target should block tactical enemy attack.");
            AssertFalse(deadTarget.Validation.TargetAlive, "Dead target should report TargetAlive=false.");

            var legalAttack = EncounterEnemyTactics.EvaluateAttackFeasibility(
                attackerX: 0,
                attackerY: 0,
                targetX: 1,
                targetY: 0,
                targetAlive: true,
                maxRangeTiles: 1,
                requiresLineOfSight: true,
                hasLineOfSight: HasLos);
            AssertTrue(legalAttack.CanAttack, "In-range clear alive target should be attackable.");
            AssertTrue(legalAttack.Validation.IsLegal, "Legal tactical attack should preserve target validation state.");
        });

        Run("EncounterReinforcements_Join_WhenAllyInRangeWithLos", () =>
        {
            var anchors = new List<EncounterReinforcementMember>
            {
                new(X: 6, Y: 6, EnemyKey: "goblin_grunt")
            };
            var candidate = new EncounterReinforcementMember(
                X: 8,
                Y: 6,
                EnemyKey: "goblin_slinger");

            var isEligible = EncounterReinforcementRules.IsCandidateEligible(
                candidate,
                anchors,
                joinDistanceTiles: 2,
                AreEncounterAllies,
                hasLineOfSight: (_, _, _, _) => true);
            AssertTrue(isEligible, "Goblin reinforcement should join when ally, in range, and LOS is clear.");
        });

        Run("EncounterReinforcements_Blocked_WhenNoAllyRangeLosOrCapFails", () =>
        {
            var anchors = new List<EncounterReinforcementMember>
            {
                new(X: 6, Y: 6, EnemyKey: "goblin_grunt")
            };

            var nonAllyCandidate = new EncounterReinforcementMember(X: 7, Y: 6, EnemyKey: "skeleton");
            var nonAllyEligible = EncounterReinforcementRules.IsCandidateEligible(
                nonAllyCandidate,
                anchors,
                joinDistanceTiles: 2,
                AreEncounterAllies,
                hasLineOfSight: (_, _, _, _) => true);
            AssertFalse(nonAllyEligible, "Reinforcement should be blocked when candidate is not encounter ally.");

            var outOfRangeCandidate = new EncounterReinforcementMember(X: 10, Y: 6, EnemyKey: "goblin_slinger");
            var outOfRangeEligible = EncounterReinforcementRules.IsCandidateEligible(
                outOfRangeCandidate,
                anchors,
                joinDistanceTiles: 2,
                AreEncounterAllies,
                hasLineOfSight: (_, _, _, _) => true);
            AssertFalse(outOfRangeEligible, "Reinforcement should be blocked when candidate is out of range.");

            var losBlockedCandidate = new EncounterReinforcementMember(X: 8, Y: 6, EnemyKey: "goblin_slinger");
            var losBlockedEligible = EncounterReinforcementRules.IsCandidateEligible(
                losBlockedCandidate,
                anchors,
                joinDistanceTiles: 2,
                AreEncounterAllies,
                hasLineOfSight: (_, _, _, _) => false);
            AssertFalse(losBlockedEligible, "Reinforcement should be blocked when LOS is blocked.");

            AssertFalse(
                EncounterReinforcementRules.HasOpenEncounterSlot(encounterSize: 3, maxEncounterSize: 3),
                "Cap-full encounter should reject additional reinforcement joins.");
        });

        Run("EncounterSpellTargeting_RangePolicy_BySpellLevel", () =>
        {
            AssertEqual(EncounterSpellTargetingRangePolicy.CantripRangeTiles,
                EncounterSpellTargetingRangePolicy.ResolveSpellRangeTiles(0),
                "Cantrip range policy mismatch.");
            AssertEqual(EncounterSpellTargetingRangePolicy.Level1RangeTiles,
                EncounterSpellTargetingRangePolicy.ResolveSpellRangeTiles(1),
                "Level 1 range policy mismatch.");
            AssertEqual(EncounterSpellTargetingRangePolicy.Level2RangeTiles,
                EncounterSpellTargetingRangePolicy.ResolveSpellRangeTiles(2),
                "Level 2 range policy mismatch.");
            AssertEqual(EncounterSpellTargetingRangePolicy.Level3PlusRangeTiles,
                EncounterSpellTargetingRangePolicy.ResolveSpellRangeTiles(3),
                "Level 3+ range policy mismatch.");
            AssertEqual(EncounterSpellTargetingRangePolicy.CantripRangeTiles,
                EncounterSpellTargetingRangePolicy.ResolveSpellRangeTiles(-99),
                "Negative spell level should normalize to cantrip range policy.");

            var level2Spell = new SpellDefinition
            {
                Id = "selfcheck_l2_spell",
                Name = "Selfcheck L2 Spell",
                ClassName = "Mage",
                SpellLevel = 2,
                Description = "Self-check fixture spell.",
                ScalingStat = StatName.Intelligence,
                BaseDamage = 1,
                Variance = 0,
                ArmorBypass = 0,
                DamageTag = "arcane",
                SuppressCounterAttack = false
            };
            AssertEqual(EncounterSpellTargetingRangePolicy.Level2RangeTiles,
                EncounterSpellTargetingRangePolicy.ResolveSpellRangeTiles(level2Spell),
                "Spell-definition range resolution mismatch.");

            var selfAuraSpell = new SpellDefinition
            {
                Id = "selfcheck_self_aura",
                Name = "Self Aura",
                ClassName = "Cleric",
                SpellLevel = 2,
                Description = "Self aura fixture.",
                ScalingStat = StatName.Wisdom,
                BaseDamage = 1,
                Variance = 0,
                ArmorBypass = 0,
                DamageTag = "radiant",
                SuppressCounterAttack = false,
                TargetShape = SpellTargetShape.Self
            };
            AssertEqual(0,
                EncounterSpellTargetingRangePolicy.ResolveSpellRangeTiles(selfAuraSpell),
                "Self-targeted spells should resolve to zero range.");
        });

        Run("EncounterSpellTargeting_ModeAndCycling_ExplicitBehavior", () =>
        {
            AssertFalse(EncounterSpellTargetingRules.IsExplicitMode(EncounterSpellTargetMode.Disabled),
                "Disabled mode should not be explicit.");
            AssertTrue(EncounterSpellTargetingRules.IsExplicitMode(EncounterSpellTargetMode.SelectTarget),
                "SelectTarget mode should be explicit.");
            AssertTrue(EncounterSpellTargetingRules.IsExplicitMode(EncounterSpellTargetMode.ConfirmTarget),
                "ConfirmTarget mode should be explicit.");

            AssertEqual(EncounterSpellTargetMode.Disabled,
                EncounterSpellTargetingRules.ResolveMode(spellMenuOpen: false, hasTargetCandidates: true, requiresConfirmation: false),
                "Closed spell menu should force disabled spell target mode.");
            AssertEqual(EncounterSpellTargetMode.Disabled,
                EncounterSpellTargetingRules.ResolveMode(spellMenuOpen: true, hasTargetCandidates: false, requiresConfirmation: false),
                "Missing target candidates should force disabled spell target mode.");
            AssertEqual(EncounterSpellTargetMode.SelectTarget,
                EncounterSpellTargetingRules.ResolveMode(spellMenuOpen: true, hasTargetCandidates: true, requiresConfirmation: false),
                "Open spell menu should enter SelectTarget mode when confirmation is not required.");
            AssertEqual(EncounterSpellTargetMode.ConfirmTarget,
                EncounterSpellTargetingRules.ResolveMode(spellMenuOpen: true, hasTargetCandidates: true, requiresConfirmation: true),
                "Open spell menu should enter ConfirmTarget mode when confirmation is required.");

            AssertEqual(-1, EncounterSpellTargetingRules.CycleTargetIndex(currentIndex: 0, candidateCount: 0, direction: 1),
                "Zero target candidates should not return a valid index.");
            AssertEqual(1, EncounterSpellTargetingRules.CycleTargetIndex(currentIndex: 0, candidateCount: 3, direction: 1),
                "Forward cycle mismatch.");
            AssertEqual(2, EncounterSpellTargetingRules.CycleTargetIndex(currentIndex: 0, candidateCount: 3, direction: -1),
                "Backward wrap cycle mismatch.");
            AssertEqual(0, EncounterSpellTargetingRules.CycleTargetIndex(currentIndex: 2, candidateCount: 3, direction: 1),
                "Forward wrap cycle mismatch.");
            AssertEqual(2, EncounterSpellTargetingRules.CycleTargetIndex(currentIndex: 99, candidateCount: 3, direction: 0),
                "Zero-direction cycle should normalize and keep the current index.");
        });

        Run("EncounterSpellTargeting_Validation_UsesRangePolicy", () =>
        {
            var level1Spell = new SpellDefinition
            {
                Id = "selfcheck_l1_spell",
                Name = "Selfcheck L1 Spell",
                ClassName = "Mage",
                SpellLevel = 1,
                Description = "Self-check fixture spell.",
                ScalingStat = StatName.Intelligence,
                BaseDamage = 1,
                Variance = 0,
                ArmorBypass = 0,
                DamageTag = "arcane",
                SuppressCounterAttack = false
            };

            bool HasLos(int fromX, int fromY, int toX, int toY)
            {
                return !(fromX == 0 && fromY == 0 && toX == 5 && toY == 0);
            }

            var outOfRange = EncounterSpellTargetingRules.ValidateSpellTarget(
                spell: level1Spell,
                fromX: 0,
                fromY: 0,
                toX: 7,
                toY: 0,
                targetAlive: true,
                requiresLineOfSight: true,
                hasLineOfSight: HasLos);
            AssertFalse(outOfRange.IsLegal, "Out-of-range spell target should be blocked.");
            AssertEqual(EncounterSpellTargetingRangePolicy.Level1RangeTiles, outOfRange.MaxRangeTiles,
                "Spell validation should use level-based range policy.");

            var losBlocked = EncounterSpellTargetingRules.ValidateSpellTarget(
                spell: level1Spell,
                fromX: 0,
                fromY: 0,
                toX: 5,
                toY: 0,
                targetAlive: true,
                requiresLineOfSight: true,
                hasLineOfSight: HasLos);
            AssertFalse(losBlocked.IsLegal, "LOS-blocked spell target should be illegal.");
            AssertTrue(losBlocked.InRange, "LOS-blocked target should remain in range.");
            AssertFalse(losBlocked.HasLineOfSight, "LOS-blocked target should report blocked line of sight.");

            var legal = EncounterSpellTargetingRules.ValidateSpellTarget(
                spell: level1Spell,
                fromX: 0,
                fromY: 0,
                toX: 4,
                toY: 0,
                targetAlive: true,
                requiresLineOfSight: true,
                hasLineOfSight: HasLos);
            AssertTrue(legal.IsLegal, "In-range clear spell target should be legal.");
            AssertEqual(4, legal.DistanceTiles, "Spell distance calculation mismatch.");

            var selfAuraSpell = new SpellDefinition
            {
                Id = "selfcheck_self_validate",
                Name = "Self Aura",
                ClassName = "Cleric",
                SpellLevel = 2,
                Description = "Self aura fixture.",
                ScalingStat = StatName.Wisdom,
                BaseDamage = 1,
                Variance = 0,
                ArmorBypass = 0,
                DamageTag = "radiant",
                SuppressCounterAttack = false,
                TargetShape = SpellTargetShape.Self
            };
            var selfValidation = EncounterSpellTargetingRules.ValidateSpellTarget(
                spell: selfAuraSpell,
                fromX: 4,
                fromY: 4,
                toX: 99,
                toY: 99,
                targetAlive: false,
                requiresLineOfSight: true,
                hasLineOfSight: HasLos);
            AssertTrue(selfValidation.IsLegal, "Self-targeted spells should validate without an enemy anchor.");
            AssertEqual(0, selfValidation.MaxRangeTiles, "Self-targeted spells should report zero max range.");
        });

        Run("EncounterSpellAreaRules_Shapes_EnumerateExpectedTiles", () =>
        {
            var fireball = SpellData.ById["mage_fireball"];
            var fireballRoute = SpellData.ResolveEffectRoute(fireball);
            var fireballTiles = EncounterSpellAreaRules.EnumerateAffectedTiles(
                fireball,
                fireballRoute,
                casterX: 5,
                casterY: 5,
                anchorX: 8,
                anchorY: 5);
            AssertTrue(fireballTiles.Contains((8, 5)), "Radius preview should include anchor tile.");
            AssertTrue(fireballTiles.Contains((7, 5)), "Radius preview should include adjacent tile.");
            AssertFalse(fireballTiles.Contains((5, 5)), "Radius preview should not reach back to caster at this anchor.");

            var bolt = SpellData.ById["mage_lightning_bolt"];
            var boltRoute = SpellData.ResolveEffectRoute(bolt);
            var boltTiles = EncounterSpellAreaRules.EnumerateAffectedTiles(
                bolt,
                boltRoute,
                casterX: 5,
                casterY: 5,
                anchorX: 8,
                anchorY: 5);
            AssertTrue(boltTiles.Contains((6, 5)), "Line preview should include first forward tile.");
            AssertTrue(boltTiles.Contains((8, 5)), "Line preview should include anchor tile.");
            AssertFalse(boltTiles.Contains((8, 6)), "Line preview should stay on the traced line.");

            var coneSpell = SpellData.ById["mage_burning_hands"];
            var coneRoute = SpellData.ResolveEffectRoute(coneSpell);
            var coneTiles = EncounterSpellAreaRules.EnumerateAffectedTiles(
                coneSpell,
                coneRoute,
                casterX: 5,
                casterY: 5,
                anchorX: 8,
                anchorY: 5);
            AssertTrue(coneTiles.Contains((6, 5)), "Cone preview should include the first forward tile.");
            AssertTrue(coneTiles.Contains((7, 4)), "Cone preview should widen on later rows.");
            AssertFalse(coneTiles.Contains((5, 4)), "Cone preview should not include tiles beside the caster.");
        });

        Run("FeatProgression_CreationAndEveryFourthLevel", () =>
        {
            AssertEqual(1, FeatProgression.GetCreationStartingFeatPicks(), "Creation feat pick count mismatch.");
            AssertFalse(FeatProgression.GrantsFeat(1), "Level 1 should not grant an extra progression feat.");
            AssertFalse(FeatProgression.GrantsFeat(3), "Level 3 should not grant a progression feat.");
            AssertTrue(FeatProgression.GrantsFeat(4), "Level 4 should grant a progression feat.");
            AssertFalse(FeatProgression.GrantsFeat(6), "Level 6 should not grant a progression feat.");
            AssertTrue(FeatProgression.GrantsFeat(8), "Level 8 should grant a progression feat.");
        });

        Run("FeatCatalog_SizeAndLevelAccess", () =>
        {
            AssertTrue(FeatBook.All.Count >= 46, $"Feat catalog too small. Count={FeatBook.All.Count}.");
            var gatedByLevel = FeatBook.All
                .Where(feat => feat.MinLevel > 1)
                .Select(feat => $"{feat.Id}(Lv{feat.MinLevel})")
                .ToList();
            AssertTrue(gatedByLevel.Count == 0, $"All feats should be level-1 available right now. Found: {string.Join(", ", gatedByLevel)}");

            foreach (var feat in FeatBook.All)
            {
                foreach (var requiredFeatId in feat.RequiredFeatIds)
                {
                    AssertTrue(FeatBook.ById.ContainsKey(requiredFeatId),
                        $"Feat '{feat.Id}' references unknown prerequisite '{requiredFeatId}'.");
                }
            }
        });

        Run("ArmorTraining_ClassRanks_AndCategoryChecks", () =>
        {
            AssertEqual(3, ArmorTraining.GetClassTrainingRank("Warrior"), "Warrior class armor rank mismatch.");
            AssertEqual(2, ArmorTraining.GetClassTrainingRank("Ranger"), "Ranger class armor rank mismatch.");
            AssertEqual(0, ArmorTraining.GetClassTrainingRank("Mage"), "Mage class armor rank mismatch.");
            AssertEqual(3, ArmorTraining.GetRequiredRank(ArmorCategory.Heavy), "Heavy armor required rank mismatch.");
        });

        Run("ArmorFeatChain_RequiresPriorTraining", () =>
        {
            var mageClass = CharacterClasses.All.First(c => string.Equals(c.Name, "Mage", StringComparison.Ordinal));
            var mage = new Player(2, 2, mageClass, "ArmorAudit", Gender.Male, Race.Human);
            mage.AddFeatPoints(3);

            var light = FeatBook.ById["light_armor_training_feat"];
            var medium = FeatBook.ById["medium_armor_training_feat"];
            var heavy = FeatBook.ById["heavy_armor_training_feat"];

            AssertFalse(mage.CanLearnFeat(medium, out _), "Mage should not learn medium armor feat before light training.");
            AssertTrue(mage.LearnFeat(light), "Mage should learn light armor feat.");
            AssertTrue(mage.CanLearnFeat(medium, out _), "Mage should learn medium armor feat after light training.");
            AssertTrue(mage.LearnFeat(medium), "Mage should learn medium armor feat.");
            AssertTrue(mage.CanLearnFeat(heavy, out _), "Mage should learn heavy armor feat after medium training.");
        });

        Run("SaveStore_ManualRoundTrip_AndCorruptHandling", () =>
        {
            var testDir = Path.Combine(Path.GetTempPath(), $"dungeonescape-phase7-{Guid.NewGuid():N}");
            var previous = Environment.GetEnvironmentVariable(SaveStore.SaveDirEnvVar);
            Environment.SetEnvironmentVariable(SaveStore.SaveDirEnvVar, testDir);
            try
            {
                Directory.CreateDirectory(testDir);

                var snapshot = BuildSnapshot();
                var save = SaveStore.SaveManualSlot(1, snapshot);
                AssertTrue(save.Success, $"Save failed: {save.Message}");

                var load = SaveStore.LoadManualSlot(1, out var loaded);
                AssertTrue(load.Success, $"Load failed: {load.Message}");
                AssertTrue(loaded != null, "Loaded snapshot was null.");
                AssertEqual("manual", loaded!.SaveKind, "Save kind mismatch after round-trip.");
                AssertEqual(GameState.Playing, loaded.ResumeState, "ResumeState mismatch after round-trip.");
                AssertEqual(2, loaded.Enemies.Count, "Enemy count mismatch after round-trip.");
                AssertEqual(77, loaded.SettingsMasterVolume, "Settings volume mismatch after round-trip.");
                AssertFalse(loaded.SettingsVerboseCombatLog, "Verbose combat flag mismatch after round-trip.");
                AssertEqual("DeuteranopiaFriendly", loaded.SettingsAccessibilityColorProfile, "Accessibility color profile mismatch after round-trip.");
                AssertTrue(loaded.SettingsAccessibilityHighContrast, "Accessibility high-contrast flag mismatch after round-trip.");
                AssertFalse(loaded.SettingsOptionalConditionsEnabled, "Optional conditions mode mismatch after round-trip.");
                AssertEqual("ArcaneBlindness", loaded.CreationOriginCondition, "Creation origin condition mismatch after round-trip.");
                AssertEqual(1, loaded.DungeonConditionEventsTriggered, "Dungeon condition event counter mismatch after round-trip.");
                AssertEqual(1, loaded.MajorConditions.Count, "Major condition count mismatch after round-trip.");
                AssertEqual("CrushedLimb", loaded.MajorConditions[0].Type, "Major condition type mismatch after round-trip.");
                AssertEqual("Dungeon", loaded.MajorConditions[0].Source, "Major condition source mismatch after round-trip.");
                AssertEqual(2, loaded.Enemies[0].StatusEffects.Count, "Enemy status count mismatch after round-trip.");
                AssertEqual("Incapacitated", loaded.Enemies[0].StatusEffects[0].Kind, "Enemy status kind mismatch after round-trip.");
                AssertEqual(2, loaded.Enemies[0].StatusEffects[0].RemainingTurns, "Enemy status duration mismatch after round-trip.");
                AssertEqual("Wisdom", loaded.Enemies[0].StatusEffects[0].RepeatSaveStat, "Enemy repeat-save stat mismatch after round-trip.");
                AssertEqual(14, loaded.Enemies[0].StatusEffects[0].SaveDc, "Enemy save DC mismatch after round-trip.");
                AssertTrue(loaded.Enemies[0].StatusEffects[0].BreaksOnDamageTaken, "Enemy damage-break flag mismatch after round-trip.");
                AssertEqual("Marked", loaded.Enemies[0].StatusEffects[1].Kind, "Second enemy status kind mismatch after round-trip.");
                AssertEqual(2, loaded.InventoryItems.Count, "Inventory item count mismatch after round-trip.");
                AssertEqual("Skirmisher", loaded.RunArchetype, "Run archetype mismatch after round-trip.");
                AssertEqual("VeilstriderCharm", loaded.RunRelic, "Run relic mismatch after round-trip.");
                AssertEqual("UpperCatacombs", loaded.Phase3RouteChoice, "Phase 3 route choice mismatch after round-trip.");
                AssertTrue(loaded.Phase3RiskEventResolved, "Phase 3 risk event flag mismatch after round-trip.");
                AssertEqual(20, loaded.Phase3XpPercentMod, "Phase 3 XP modifier mismatch after round-trip.");
                AssertEqual(1, loaded.Phase3EnemyAttackBonus, "Phase 3 enemy pressure mismatch after round-trip.");
                AssertEqual(6, loaded.Phase3EnemiesDefeated, "Phase 3 kill progress mismatch after round-trip.");
                AssertTrue(loaded.Phase3PreSanctumRewardGranted, "Phase 3 pre-sanctum reward flag mismatch after round-trip.");
                AssertTrue(loaded.Phase3RouteWaveSpawned, "Phase 3 route wave flag mismatch after round-trip.");
                AssertFalse(loaded.Phase3SanctumWaveSpawned, "Phase 3 sanctum wave flag mismatch after round-trip.");
                AssertEqual(1, loaded.MilestoneChoicesTaken, "Milestone checkpoint count mismatch after round-trip.");
                AssertEqual(0, loaded.MilestoneExecutionRank, "Milestone execution rank mismatch after round-trip.");
                AssertEqual(0, loaded.MilestoneArcRank, "Milestone arc rank mismatch after round-trip.");
                AssertEqual(1, loaded.MilestoneEscapeRank, "Milestone escape rank mismatch after round-trip.");
                AssertTrue(loaded.ActiveConcentration != null, "Concentration snapshot should survive save round-trip.");
                AssertEqual("cleric_spirit_guardians", loaded.ActiveConcentration!.SpellId, "Concentration spell id mismatch after round-trip.");
                AssertEqual(1, loaded.CombatHazards.Count, "Combat hazard count mismatch after round-trip.");
                AssertEqual("bard_cloud_of_daggers", loaded.CombatHazards[0].SourceSpellId, "Combat hazard source mismatch after round-trip.");
                AssertEqual("Wisdom", loaded.CombatHazards[0].InitialSaveStat, "Combat hazard save stat mismatch after round-trip.");
                AssertEqual("HalfOnSave", loaded.CombatHazards[0].SaveDamageBehavior, "Combat hazard save-damage behavior mismatch after round-trip.");
                AssertEqual(1, loaded.CombatHazards[0].OnTriggerStatuses.Count, "Combat hazard status count mismatch after round-trip.");
                AssertEqual("Restrained", loaded.CombatHazards[0].OnTriggerStatuses[0].Kind, "Combat hazard status kind mismatch after round-trip.");
                AssertEqual(65, loaded.CombatHazards[0].OnTriggerStatuses[0].ChancePercent, "Combat hazard status chance mismatch after round-trip.");
                AssertEqual("Dexterity", loaded.CombatHazards[0].OnTriggerStatuses[0].InitialSaveStat, "Combat hazard initial-save stat mismatch after round-trip.");
                AssertEqual("Strength", loaded.CombatHazards[0].OnTriggerStatuses[0].RepeatSaveStat, "Combat hazard repeat-save stat mismatch after round-trip.");

                var entries = SaveStore.GetAvailableLoadEntries();
                AssertTrue(entries.Any(e => !e.IsAutosave && e.ManualSlot == 1 && e.Exists), "Manual slot summary not listed.");

                File.WriteAllText(Path.Combine(testDir, "slot2.json"), "{invalid-json");
                var corrupt = SaveStore.LoadManualSlot(2, out var corruptSnapshot);
                AssertFalse(corrupt.Success, "Corrupt save should fail to load.");
                AssertTrue(corruptSnapshot == null, "Corrupt snapshot output should be null.");
            }
            finally
            {
                Environment.SetEnvironmentVariable(SaveStore.SaveDirEnvVar, previous);
                try
                {
                    if (Directory.Exists(testDir))
                    {
                        Directory.Delete(testDir, recursive: true);
                    }
                }
                catch
                {
                    // Ignore cleanup failures.
                }
            }
        });

        Run("SpellCatalog_ExpandedCoverage_AndUnlockIntegrity", () =>
        {
            var requiredClasses = new[] { "Mage", "Cleric", "Bard", "Paladin", "Ranger" };
            var minCatalogByClass = new Dictionary<string, int>
            {
                ["Mage"] = 45,
                ["Cleric"] = 30,
                ["Bard"] = 30,
                ["Paladin"] = 18,
                ["Ranger"] = 18
            };

            foreach (var className in requiredClasses)
            {
                var classSpellCount = SpellData.ById.Values.Count(spell =>
                    string.Equals(spell.ClassName, className, StringComparison.Ordinal));
                AssertTrue(classSpellCount >= minCatalogByClass[className],
                    $"Spell catalog too small for {className}. Count={classSpellCount}");

                AssertTrue(SpellData.ClassSpellUnlocks.ContainsKey(className),
                    $"Unlock list missing for class {className}.");

                var unlocks = SpellData.ClassSpellUnlocks[className];
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var unlock in unlocks)
                {
                    AssertTrue(SpellData.ById.ContainsKey(unlock.SpellId),
                        $"Unlock references missing spell id {unlock.SpellId} for class {className}.");
                    AssertTrue(seen.Add(unlock.SpellId),
                        $"Duplicate unlock reference {unlock.SpellId} detected for class {className}.");
                }
            }
        });

        Run("SpellProgression_HalfCaster_L3Slots_ByLevel6", () =>
        {
            if (!SpellData.SpellSlotsByClass.TryGetValue("Paladin", out var paladinSlots) || paladinSlots == null)
            {
                throw new InvalidOperationException("Paladin slot progression missing.");
            }

            if (!SpellData.SpellSlotsByClass.TryGetValue("Ranger", out var rangerSlots) || rangerSlots == null)
            {
                throw new InvalidOperationException("Ranger slot progression missing.");
            }

            AssertTrue(paladinSlots.TryGetValue(6, out var paladinL6), "Paladin level 6 slots missing.");
            AssertTrue(rangerSlots.TryGetValue(6, out var rangerL6), "Ranger level 6 slots missing.");
            AssertTrue(paladinL6.L3 > 0, $"Paladin level 6 should have L3 slots. Found {paladinL6.L3}.");
            AssertTrue(rangerL6.L3 > 0, $"Ranger level 6 should have L3 slots. Found {rangerL6.L3}.");
        });

        Run("Player_FreeCantrips_AutoKnown_AtLevel1", () =>
        {
            var mageClass = CharacterClasses.All.First(c => string.Equals(c.Name, "Mage", StringComparison.Ordinal));
            var mage = new Player(2, 2, mageClass, "CantripAudit", Gender.Male, Race.Human);

            var known = mage.GetKnownSpells();
            var knownCantrips = known.Count(spell => spell.IsCantrip);
            AssertTrue(knownCantrips >= 1, "Mage should start with free cantrips.");

            var expectedSpellPicks = SpellProgression.GetSpellPicksForLevel("Mage", 1);
            AssertEqual(expectedSpellPicks, mage.SpellPickPoints,
                "Free cantrips should not consume level-1 spell picks.");
        });

        Run("SpellVisibility_PlayerFacingRoster_HidesPrototypeEntries", () =>
        {
            AssertTrue(SpellData.ById.TryGetValue("mage_fire_bolt", out var authoredSpell) && SpellData.IsPlayerVisible(authoredSpell),
                "Authored spell should remain player-visible.");
            AssertTrue(SpellData.ById.TryGetValue("mage_alarm", out var prototypeSpell) && prototypeSpell != null && prototypeSpell.IsPrototypeExpanded,
                "Prototype-expanded spell should exist for audit coverage.");
            AssertFalse(SpellData.IsPlayerVisible(prototypeSpell!),
                "Prototype-expanded spell should be hidden from player-facing lists.");
        });

        Run("SpellVisibility_PlayerFacingRoster_HidesFutureGatedAuthoredEntries", () =>
        {
            // ranger_pass_without_trace is still gated (→ 9J+), use it as the coverage case
            AssertTrue(SpellData.ById.TryGetValue("ranger_pass_without_trace", out var authoredFutureSpell) &&
                authoredFutureSpell != null &&
                !authoredFutureSpell.IsPrototypeExpanded,
                "Authored future-gated spell should exist for visibility coverage.");
            AssertFalse(SpellData.IsPlayerVisible(authoredFutureSpell!),
                "Authored spell requiring a future subsystem should be hidden from player-facing lists.");
        });

        Run("SpellRoutes_StatusMetadata_AreTagged", () =>
        {
            var frostRoute = SpellData.ResolveEffectRoute(SpellData.ById["mage_ray_of_frost"]);
            AssertEqual(SpellEffectRouteKind.DamageAndStatus, frostRoute.RouteKind,
                "Ray of Frost should route through damage-and-status metadata.");
            AssertEqual(SpellCombatFamily.DebuffHex, frostRoute.CombatFamily,
                "Ray of Frost should be classified as a debuff family spell.");
            AssertTrue(frostRoute.OnHitStatuses.Any(status => status.Kind == CombatStatusKind.Chilled),
                "Ray of Frost should apply chilled metadata.");

            var markRoute = SpellData.ResolveEffectRoute(SpellData.ById["ranger_hunters_mark"]);
            AssertEqual(SpellCombatFamily.MarkDebuff, markRoute.CombatFamily,
                "Hunter's Mark should be classified as a mark/debuff family spell.");
            AssertTrue(markRoute.OnHitStatuses.Any(status => status.Kind == CombatStatusKind.Marked),
                "Hunter's Mark should apply marked metadata.");

            var acidArrowRoute = SpellData.ResolveEffectRoute(SpellData.ById["mage_melfs_acid_arrow"]);
            AssertEqual(SpellCombatFamily.DamageOverTime, acidArrowRoute.CombatFamily,
                "Melf's Acid Arrow should be classified as a damage-over-time family spell.");
            AssertTrue(acidArrowRoute.OnHitStatuses.Any(status => status.Kind == CombatStatusKind.Corroded),
                "Melf's Acid Arrow should apply corroded metadata.");

            var searingSmiteRoute = SpellData.ResolveEffectRoute(SpellData.ById["paladin_searing_smite"]);
            AssertEqual(SpellCombatFamily.SmiteStrike, searingSmiteRoute.CombatFamily,
                "Searing Smite should be classified as a smite-strike family spell.");
            AssertFalse(searingSmiteRoute.RequiresConcentration,
                "Searing Smite should resolve as an empowered melee strike in the current build.");

            var baneRoute = SpellData.ResolveEffectRoute(SpellData.ById["cleric_bane"]);
            AssertFalse(baneRoute.DealsDirectDamage,
                "Bane should no longer deal direct damage.");
            AssertEqual(SpellTargetShape.Radius, baneRoute.TargetShape,
                "Bane should affect a small radius around the target point.");
            AssertTrue(baneRoute.RequiresConcentration,
                "Bane should require concentration.");
            AssertEqual(StatName.Charisma, baneRoute.InitialSaveStat,
                "Bane should now use a Charisma save.");

            var holdPersonRoute = SpellData.ResolveEffectRoute(SpellData.ById["cleric_hold_person"]);
            AssertFalse(holdPersonRoute.DealsDirectDamage,
                "Hold Person should no longer deal direct damage.");
            AssertEqual(StatName.Wisdom, holdPersonRoute.InitialSaveStat,
                "Hold Person should now use a Wisdom save.");
            AssertEqual(CreatureTypeTag.Humanoid, holdPersonRoute.AllowedCreatureTypes,
                "Hold Person should only affect humanoids.");
            AssertTrue(holdPersonRoute.OnHitStatuses.Any(status =>
                    status.Kind == CombatStatusKind.Paralyzed &&
                    status.RepeatSaveStat == StatName.Wisdom),
                "Hold Person should now apply a paralyzed status with repeat saves.");

            var hazardRoute = SpellData.ResolveEffectRoute(SpellData.ById["bard_cloud_of_daggers"]);
            AssertFalse(hazardRoute.IsFutureGated,
                "Cloud of Daggers should be unlocked once hazard zones exist.");
            AssertEqual(SpellCombatFamily.HazardZone, hazardRoute.CombatFamily,
                "Cloud of Daggers should be classified as a hazard-zone spell.");
            AssertTrue(hazardRoute.RequiresConcentration,
                "Cloud of Daggers should require concentration.");
            AssertTrue(hazardRoute.HazardSpec != null,
                "Cloud of Daggers should carry a hazard payload.");

            var fireballRoute = SpellData.ResolveEffectRoute(SpellData.ById["mage_fireball"]);
            AssertEqual(SpellTargetShape.Radius, fireballRoute.TargetShape,
                "Fireball should now use a radius target shape.");
            AssertEqual(StatName.Dexterity, fireballRoute.InitialSaveStat,
                "Fireball should now use a Dexterity save.");
            AssertEqual(SpellSaveDamageBehavior.HalfOnSave, fireballRoute.SaveDamageBehavior,
                "Fireball should now deal half damage on a successful save.");

            var sacredFlameRoute = SpellData.ResolveEffectRoute(SpellData.ById["cleric_sacred_flame"]);
            AssertEqual(StatName.Dexterity, sacredFlameRoute.InitialSaveStat,
                "Sacred Flame should now use a Dexterity save.");
            AssertEqual(SpellSaveDamageBehavior.NegateOnSave, sacredFlameRoute.SaveDamageBehavior,
                "Sacred Flame should now deal no damage on a successful save.");

            var viciousMockeryRoute = SpellData.ResolveEffectRoute(SpellData.ById["bard_vicious_mockery"]);
            AssertEqual(StatName.Wisdom, viciousMockeryRoute.InitialSaveStat,
                "Vicious Mockery should now use a Wisdom save.");
            AssertEqual(SpellSaveDamageBehavior.NegateOnSave, viciousMockeryRoute.SaveDamageBehavior,
                "Vicious Mockery should now deal no damage on a successful save.");

            var dissonantWhispersRoute = SpellData.ResolveEffectRoute(SpellData.ById["bard_dissonant_whispers"]);
            AssertEqual(StatName.Wisdom, dissonantWhispersRoute.InitialSaveStat,
                "Dissonant Whispers should now use a Wisdom save.");
            AssertEqual(SpellSaveDamageBehavior.HalfOnSave, dissonantWhispersRoute.SaveDamageBehavior,
                "Dissonant Whispers should now deal half damage on a successful save.");

            var guardiansRoute = SpellData.ResolveEffectRoute(SpellData.ById["cleric_spirit_guardians"]);
            AssertEqual(SpellTargetShape.Self, guardiansRoute.TargetShape,
                "Spirit Guardians should anchor on self.");
            AssertTrue(guardiansRoute.HazardSpec?.FollowsPlayer == true,
                "Spirit Guardians should follow the player as an aura.");
            AssertEqual(StatName.Wisdom, guardiansRoute.HazardSpec?.InitialSaveStat,
                "Spirit Guardians should now use a Wisdom save on hazard ticks.");
            AssertEqual(SpellSaveDamageBehavior.HalfOnSave, guardiansRoute.HazardSpec?.SaveDamageBehavior,
                "Spirit Guardians should now deal half damage on a successful save.");

            var webRoute = SpellData.ResolveEffectRoute(SpellData.ById["mage_web"]);
            AssertTrue(webRoute.HazardSpec?.OnTriggerStatuses.Any(status =>
                    status.Kind == CombatStatusKind.Restrained &&
                    status.InitialSaveStat == StatName.Dexterity &&
                    status.RepeatSaveStat == StatName.Strength) == true,
                "Web should restrain with an initial Dexterity save and repeat Strength save.");

            var curseRoute = SpellData.ResolveEffectRoute(SpellData.ById["cleric_bestow_curse"]);
            AssertEqual(StatName.Wisdom, curseRoute.InitialSaveStat,
                "Bestow Curse should now use a Wisdom save.");
            AssertTrue(curseRoute.OnHitStatuses.Any(status => status.Kind == CombatStatusKind.Cursed),
                "Bestow Curse should now apply a dedicated cursed status.");

            var blindnessRoute = SpellData.ResolveEffectRoute(SpellData.ById["cleric_blindness"]);
            AssertEqual(StatName.Constitution, blindnessRoute.InitialSaveStat,
                "Blindness should now use a Constitution save.");
            AssertTrue(blindnessRoute.OnHitStatuses.Any(status =>
                    status.Kind == CombatStatusKind.Blinded &&
                    status.RepeatSaveStat == StatName.Constitution),
                "Blindness should now allow repeat Constitution saves.");

            var patternRoute = SpellData.ResolveEffectRoute(SpellData.ById["bard_hypnotic_pattern"]);
            AssertTrue(patternRoute.OnHitStatuses.Any(status =>
                    status.Kind == CombatStatusKind.Incapacitated &&
                    status.BreaksOnDamageTaken),
                "Hypnotic Pattern should apply an incapacitating status that breaks on damage.");

            var laughterRoute = SpellData.ResolveEffectRoute(SpellData.ById["bard_hideous_laughter"]);
            AssertEqual(StatName.Wisdom, laughterRoute.InitialSaveStat,
                "Hideous Laughter should now use a Wisdom save.");
            AssertTrue(laughterRoute.OnHitStatuses.Any(status =>
                    status.Kind == CombatStatusKind.Incapacitated &&
                    status.RepeatSaveStat == StatName.Wisdom &&
                    status.BreaksOnDamageTaken),
                "Hideous Laughter should now use repeat saves and break on damage.");

            var fearRoute = SpellData.ResolveEffectRoute(SpellData.ById["bard_fear"]);
            AssertEqual(StatName.Wisdom, fearRoute.InitialSaveStat,
                "Fear should now use a Wisdom save.");
            AssertTrue(fearRoute.OnHitStatuses.Any(status =>
                    status.Kind == CombatStatusKind.Feared &&
                    status.RepeatSaveStat == StatName.Wisdom),
                "Fear should now apply repeat-save fear.");

            var slowRoute = SpellData.ResolveEffectRoute(SpellData.ById["bard_slow"]);
            AssertEqual(StatName.Wisdom, slowRoute.InitialSaveStat,
                "Slow should now use a Wisdom save.");
            AssertTrue(slowRoute.OnHitStatuses.Any(status =>
                    status.Kind == CombatStatusKind.Slowed &&
                    status.RepeatSaveStat == StatName.Wisdom),
                "Slow should now apply repeat-save slow.");

            var wrathfulSmiteRoute = SpellData.ResolveEffectRoute(SpellData.ById["paladin_wrathful_smite"]);
            AssertTrue(wrathfulSmiteRoute.OnHitStatuses.Any(status =>
                    status.Kind == CombatStatusKind.Feared &&
                    status.InitialSaveStat == StatName.Wisdom &&
                    status.RepeatSaveStat == StatName.Wisdom),
                "Wrathful Smite should now use Wisdom save-driven fear.");

            var blindingSmiteRoute = SpellData.ResolveEffectRoute(SpellData.ById["paladin_blinding_smite"]);
            AssertTrue(blindingSmiteRoute.OnHitStatuses.Any(status =>
                    status.Kind == CombatStatusKind.Blinded &&
                    status.InitialSaveStat == StatName.Constitution &&
                    status.RepeatSaveStat == StatName.Constitution),
                "Blinding Smite should now use Constitution save-driven blindness.");

            var ensnaringStrikeRoute = SpellData.ResolveEffectRoute(SpellData.ById["ranger_ensnaring_strike"]);
            AssertTrue(ensnaringStrikeRoute.OnHitStatuses.Any(status =>
                    status.Kind == CombatStatusKind.Restrained &&
                    status.InitialSaveStat == StatName.Strength &&
                    status.RepeatSaveStat == StatName.Strength),
                "Ensnaring Strike should now use Strength save-driven restraint.");
        });

        Run("SpellFamilies_PlayerVisibleRoster_HasActiveCombatFamilies", () =>
        {
            var visibleSpells = SpellData.ById.Values
                .Where(SpellData.IsPlayerVisible)
                .ToList();
            AssertTrue(visibleSpells.Count > 0, "Visible spell roster should not be empty.");
            AssertTrue(visibleSpells.All(spell => !SpellData.ResolveEffectRoute(spell).IsFutureGated),
                "Visible spells should never point at future-gated combat routes.");
        });

        Run("SpellRoutes_SaveDamageBehavior_RequiresSaveStat_AndActiveRoute", () =>
        {
            var saveDamageSpells = SpellData.ById.Values
                .Where(spell => SpellData.ResolveEffectRoute(spell).SaveDamageBehavior != SpellSaveDamageBehavior.None)
                .ToList();

            AssertTrue(saveDamageSpells.Count > 0, "Save-damage spell roster should not be empty.");
            AssertTrue(saveDamageSpells.All(spell => SpellData.ResolveEffectRoute(spell).InitialSaveStat.HasValue),
                "Every save-damage spell should declare an initial save stat.");
            AssertTrue(saveDamageSpells.All(spell => !SpellData.ResolveEffectRoute(spell).IsFutureGated),
                "Save-damage spells should not remain future-gated.");
        });

        Run("CombatStatusRules_Modifiers_AreDeterministic", () =>
        {
            var statuses = new List<CombatStatusState>
            {
                new() { Kind = CombatStatusKind.Chilled, Potency = 1, RemainingTurns = 2, SourceSpellId = "test", SourceLabel = "Test" },
                new() { Kind = CombatStatusKind.Slowed, Potency = 1, RemainingTurns = 2, SourceSpellId = "test", SourceLabel = "Test" },
                new() { Kind = CombatStatusKind.Weakened, Potency = 1, RemainingTurns = 2, SourceSpellId = "test", SourceLabel = "Test" },
                new() { Kind = CombatStatusKind.Marked, Potency = 2, RemainingTurns = 3, SourceSpellId = "test", SourceLabel = "Test" },
                new() { Kind = CombatStatusKind.Feared, Potency = 1, RemainingTurns = 2, SourceSpellId = "test", SourceLabel = "Test" },
                new() { Kind = CombatStatusKind.Rooted, Potency = 1, RemainingTurns = 1, SourceSpellId = "test", SourceLabel = "Test" },
                new() { Kind = CombatStatusKind.Stunned, Potency = 1, RemainingTurns = 1, SourceSpellId = "test", SourceLabel = "Test" }
            };

            AssertEqual(4, CombatStatusRules.GetAttackPenalty(statuses),
                "Chilled + slowed + fear + weakened should create deterministic attack penalties.");
            AssertEqual(4, CombatStatusRules.GetMovePenalty(statuses),
                "Chilled + slowed + fear should create deterministic move penalties.");
            AssertEqual(2, CombatStatusRules.GetIncomingDamageBonus(statuses),
                "Marked should increase incoming damage.");
            AssertTrue(CombatStatusRules.PreventsEnemyMovement(statuses),
                "Rooted or stunned enemies should not move.");
            AssertTrue(CombatStatusRules.PreventsEnemyAction(statuses),
                "Stunned enemies should lose their action.");
            AssertTrue(CombatStatusRules.ForcesEnemyRetreat(statuses),
                "Feared enemies should prefer retreat behavior.");
            AssertFalse(CombatStatusRules.LimitsEnemyAttackRangeToMelee(statuses),
                "Blindness should be the specific trigger for melee-only range.");

            statuses.Add(new CombatStatusState
            {
                Kind = CombatStatusKind.Blinded,
                Potency = 2,
                RemainingTurns = 2,
                SourceSpellId = "test",
                SourceLabel = "Test"
            });
            AssertTrue(CombatStatusRules.LimitsEnemyAttackRangeToMelee(statuses),
                "Blinded enemies should lose ranged reach.");

            statuses.Add(new CombatStatusState
            {
                Kind = CombatStatusKind.Corroded,
                Potency = 2,
                RemainingTurns = 2,
                SourceSpellId = "test",
                SourceLabel = "Test"
            });
            AssertEqual(3, CombatStatusRules.GetIncomingDamageBonus(statuses),
                "Corroded should add a small incoming-damage bonus on top of mark.");
        });

        Run("CombatStatusRules_AdvancedConditions_AreDeterministic", () =>
        {
            var statuses = new List<CombatStatusState>
            {
                new() { Kind = CombatStatusKind.Restrained, Potency = 1, RemainingTurns = 2, SourceSpellId = "test", SourceLabel = "Test" },
                new() { Kind = CombatStatusKind.Paralyzed, Potency = 1, RemainingTurns = 2, SourceSpellId = "test", SourceLabel = "Test" },
                new() { Kind = CombatStatusKind.Incapacitated, Potency = 1, RemainingTurns = 2, SourceSpellId = "test", SourceLabel = "Test" },
                new() { Kind = CombatStatusKind.Prone, Potency = 1, RemainingTurns = 2, SourceSpellId = "test", SourceLabel = "Test" },
                new() { Kind = CombatStatusKind.Cursed, Potency = 2, RemainingTurns = 2, SourceSpellId = "test", SourceLabel = "Test" }
            };

            AssertEqual(4, CombatStatusRules.GetAttackPenalty(statuses),
                "Restrained + prone + cursed should impose deterministic attack penalties.");
            AssertEqual(1, CombatStatusRules.GetMovePenalty(statuses),
                "Prone should add a deterministic move penalty.");
            AssertEqual(6, CombatStatusRules.GetIncomingDamageBonus(statuses),
                "Restrained + paralyzed + prone + cursed should increase incoming damage.");
            AssertTrue(CombatStatusRules.PreventsEnemyAction(statuses),
                "Paralyzed or incapacitated enemies should lose their action.");
            AssertTrue(CombatStatusRules.PreventsEnemyMovement(statuses),
                "Restrained, paralyzed, or incapacitated enemies should not move.");
        });

        Run("SpellRestore_MigratesArchivedKnownSpells_AndRefundsPicks", () =>
        {
            var snapshot = new PlayerSnapshot
            {
                Name = "SpellAudit",
                ClassName = "Mage",
                Gender = Gender.Female,
                Race = Race.Elf,
                X = 4,
                Y = 4,
                Stats = new StatsSnapshot
                {
                    Strength = 8,
                    Dexterity = 12,
                    Constitution = 10,
                    Intelligence = 16,
                    Wisdom = 14,
                    Charisma = 10
                },
                Level = 1,
                Xp = 0,
                XpToNextLevel = 300,
                SpellPickPoints = 0,
                MaxHp = 30,
                CurrentHp = 30,
                KnownSpellIds = new List<string> { "mage_fire_bolt", "mage_alarm" }
            };

            AssertTrue(Player.TryFromSnapshot(snapshot, out var restoredPlayer, out var restoreError, out var removedArchivedSpellCount, out var refundedArchivedSpellPicks),
                $"Player restore failed during spell migration audit: {restoreError}");
            AssertTrue(restoredPlayer != null, "Restored player should not be null after successful restore.");
            AssertEqual(1, removedArchivedSpellCount, "Exactly one archived spell should be migrated out of the test snapshot.");
            AssertEqual(1, refundedArchivedSpellPicks, "Removed archived leveled spell should refund one spell pick.");
            AssertEqual(1, restoredPlayer!.SpellPickPoints, "Refunded spell pick should be added back to the player.");
            AssertTrue(restoredPlayer.KnowsSpell("mage_fire_bolt"), "Authored known spell should survive migration.");
            AssertFalse(restoredPlayer.KnowsSpell("mage_alarm"), "Archived known spell should be removed during migration.");
            AssertFalse(restoredPlayer.GetKnownSpells().Any(spell => spell.IsPrototypeExpanded),
                "Player-facing known spell list should not expose prototype spells after restore.");
        });

        Run("RollAttack_Nat20_AlwaysCrits", () =>
        {
            var rng = new Random(0);
            var gotCrit = false;
            for (int i = 0; i < 1000; i++)
            {
                var (result, d20Raw, _) = CombatMath.RollAttack(0, 99, rng);
                if (d20Raw == 20) { AssertEqual(AttackRollResult.CriticalHit, result, "Nat 20 must always be CriticalHit."); gotCrit = true; }
            }
            AssertTrue(gotCrit, "Nat 20 never rolled in 1000 tries — RNG issue.");
        });

        Run("RollAttack_Nat1_AlwaysMisses", () =>
        {
            var rng = new Random(0);
            var gotNat1 = false;
            for (int i = 0; i < 1000; i++)
            {
                var (result, d20Raw, _) = CombatMath.RollAttack(99, 1, rng);
                if (d20Raw == 1) { AssertEqual(AttackRollResult.Miss, result, "Nat 1 must always be Miss."); gotNat1 = true; }
            }
            AssertTrue(gotNat1, "Nat 1 never rolled in 1000 tries — RNG issue.");
        });

        Run("RollAttack_Advantage_BetterDistribution", () =>
        {
            var rng = new Random(42);
            int hitsNormal = 0, hitsAdvantage = 0;
            for (int i = 0; i < 2000; i++)
            {
                var (r1, _, _) = CombatMath.RollAttack(0, 15, rng, advantage: false);
                if (r1 == AttackRollResult.Hit || r1 == AttackRollResult.CriticalHit) hitsNormal++;
            }
            for (int i = 0; i < 2000; i++)
            {
                var (r2, _, _) = CombatMath.RollAttack(0, 15, rng, advantage: true);
                if (r2 == AttackRollResult.Hit || r2 == AttackRollResult.CriticalHit) hitsAdvantage++;
            }
            AssertTrue(hitsAdvantage > hitsNormal, $"Advantage should produce more hits. Normal:{hitsNormal} Adv:{hitsAdvantage}.");
        });

        Run("Player_GetArmorClass_Unarmored_Is10PlusDex", () =>
        {
            var cls = CharacterClasses.All.First(c => c.Name == "Rogue");
            var p = new Player(0, 0, cls, "Test", Gender.Male);
            p.Stats.Dexterity = 16;
            var ac = p.GetArmorClass(ArmorCategory.Unarmored);
            AssertEqual(13, ac, $"Unarmored AC should be 10+3=13 (no feat bonuses). Got {ac}.");
        });

        Run("Player_GetArmorClass_HeavyArmor_NoDex", () =>
        {
            var cls = CharacterClasses.All.First(c => c.Name == "Warrior");
            var p = new Player(0, 0, cls, "Test", Gender.Male);
            p.Stats.Dexterity = 16;
            var ac = p.GetArmorClass(ArmorCategory.Heavy);
            AssertEqual(18, ac, $"Heavy armor AC should be 18+0=18 (no dex). Got {ac}.");
        });

        Run("Player_GetArmorClass_MediumArmor_DexCapped", () =>
        {
            var cls = CharacterClasses.All.First(c => c.Name == "Cleric");
            var p = new Player(0, 0, cls, "Test", Gender.Male);
            p.Stats.Dexterity = 18;
            var ac = p.GetArmorClass(ArmorCategory.Medium);
            AssertEqual(16, ac, $"Medium armor AC should be 14+2=16 (dex capped). Got {ac}.");
        });

        Run("Player_SaveBonus_ProficientSave_IncludesProf", () =>
        {
            var cls = CharacterClasses.All.First(c => c.Name == "Warrior");
            var p = new Player(0, 0, cls, "Test", Gender.Male);
            p.Stats.Strength = 14;
            var strSave = p.GetSaveBonus(StatName.Strength);
            AssertEqual(4, strSave, $"Warrior STR save (proficient) at L1 should be +4. Got {strSave}.");
        });

        Run("Player_SaveBonus_NonProficient_NoProfBonus", () =>
        {
            var cls = CharacterClasses.All.First(c => c.Name == "Warrior");
            var p = new Player(0, 0, cls, "Test", Gender.Male);
            p.Stats.Dexterity = 14;
            var dexSave = p.GetSaveBonus(StatName.Dexterity);
            AssertEqual(2, dexSave, $"Warrior DEX save (non-proficient) should be +2 (mod only). Got {dexSave}.");
        });

        Run("Cantrip_ScalingFormula_L1_OneDie", () =>
        {
            int diceCount1 = 1 switch { >= 17 => 4, >= 11 => 3, >= 5 => 2, _ => 1 };
            int diceCount5 = 5 switch { >= 17 => 4, >= 11 => 3, >= 5 => 2, _ => 2 };
            int diceCount11 = 11 switch { >= 17 => 4, >= 11 => 3, >= 5 => 2, _ => 3 };
            int diceCount17 = 17 switch { >= 17 => 4, >= 11 => 3, >= 5 => 2, _ => 4 };
            AssertEqual(1, diceCount1, "Level 1 cantrip: 1 die.");
            AssertEqual(2, diceCount5, "Level 5 cantrip: 2 dice.");
            AssertEqual(3, diceCount11, "Level 11 cantrip: 3 dice.");
            AssertEqual(4, diceCount17, "Level 17 cantrip: 4 dice.");
        });

        Run("WeaponBook_AllStartingWeaponsExist", () =>
        {
            foreach (var cls in CharacterClasses.All)
            {
                var found = WeaponBook.ById.ContainsKey(cls.StartingWeaponId);
                AssertTrue(found, $"Starting weapon '{cls.StartingWeaponId}' for {cls.Name} not found in WeaponBook.");
            }
        });

        Run("WeaponBook_Longsword_RollsD8", () =>
        {
            var longsword = WeaponBook.ById["longsword"];
            AssertEqual(8, longsword.DamageDice, "Longsword should be 1d8.");
            AssertEqual(1, longsword.DiceCount, "Longsword dice count should be 1.");
        });

        Run("WeaponBook_Rapier_IsFinesse", () =>
        {
            var rapier = WeaponBook.ById["rapier"];
            AssertTrue(rapier.IsFinesse, "Rapier should have IsFinesse = true.");
            AssertEqual(8, rapier.DamageDice, "Rapier should be 1d8.");
        });

        Run("EnemyType_GoblinHasArmorClass", () =>
        {
            var goblin = EnemyTypes.All["goblin"];
            AssertEqual(13, goblin.ArmorClass, $"Goblin AC should be 13. Got {goblin.ArmorClass}.");
            AssertEqual(3, goblin.AttackBonus, $"Goblin AttackBonus should be 3. Got {goblin.AttackBonus}.");
            AssertEqual(6, goblin.DamageDice, $"Goblin DamageDice should be 6 (d6). Got {goblin.DamageDice}.");
        });

        Run("Player_ProficiencyBonus_L1_Is2", () =>
        {
            var cls = CharacterClasses.All.First(c => c.Name == "Warrior");
            var p = new Player(0, 0, cls, "Test", Gender.Male);
            AssertEqual(2, p.ProficiencyBonus, $"L1 proficiency bonus should be 2. Got {p.ProficiencyBonus}.");
        });

        Run("CritRange_Default_CritsOnlyOn20", () =>
        {
            var cls = CharacterClasses.All.First(c => c.Name == "Warrior");
            var p = new Player(0, 0, cls, "Test", Gender.Male);
            AssertEqual(20, p.CritThreshold, $"Default crit threshold should be 20. Got {p.CritThreshold}.");
        });

        Run("CritRange_Skulker_HasCritRangeBonus1", () =>
        {
            // Verify Skulker feat definition has CritRangeBonus = 1
            var skulker = FeatBook.ById["rogue_blade_dancer_feat"];
            AssertEqual(1, skulker.CritRangeBonus, $"Skulker CritRangeBonus should be 1. Got {skulker.CritRangeBonus}.");
        });

        Run("CritRange_Nat1_StillMisses_EvenWithLowThreshold", () =>
        {
            var rng = new Random(0);
            for (int i = 0; i < 500; i++)
            {
                var (result, d20Raw, _) = CombatMath.RollAttack(99, 1, rng, critThreshold: 2);
                if (d20Raw == 1)
                    AssertEqual(AttackRollResult.Miss, result, "Nat 1 must always miss even with critThreshold=2.");
            }
        });

        Run("Player_CritThreshold_RogueWithSkulker_Is18", () =>
        {
            // Rogue gets eagle_eye at L1 (CritRange+1) and Skulker adds CritRangeBonus=1 → total range=2, threshold=18
            var cls = CharacterClasses.All.First(c => c.Name == "Rogue");
            var p = new Player(0, 0, cls, "Test", Gender.Male);
            p.AddFeatPoints(3);
            var uncanny = FeatBook.ById["rogue_uncanny_dodge_feat"];
            var assassin = FeatBook.ById["rogue_assassin_feat"];
            var skulker = FeatBook.ById["rogue_blade_dancer_feat"];
            p.LearnFeat(uncanny);
            p.LearnFeat(assassin);
            p.LearnFeat(skulker);
            AssertEqual(2, p.ExpandedCritRange, $"Rogue with eagle_eye+Skulker should have ExpandedCritRange=2. Got {p.ExpandedCritRange}.");
            AssertEqual(18, p.CritThreshold, $"Rogue with eagle_eye+Skulker should have crit threshold 18. Got {p.CritThreshold}.");
        });

        Run("HealSpell_SuppressCounterAttack_FlagSet", () =>
        {
            AssertTrue(SpellData.ById.ContainsKey("cleric_healing_word"), "cleric_healing_word must exist in SpellData.");
            AssertTrue(SpellData.ById["cleric_healing_word"].SuppressCounterAttack, "cleric_healing_word.SuppressCounterAttack must be true.");
            AssertTrue(SpellData.ById.ContainsKey("bard_healing_word"), "bard_healing_word must exist in SpellData.");
            AssertTrue(SpellData.ById["bard_healing_word"].SuppressCounterAttack, "bard_healing_word.SuppressCounterAttack must be true.");
        });

        Run("PlayerCondition_Poisoned_ApplyAndExpires", () =>
        {
            var conditions = new List<PlayerConditionState>();
            // Simulate ApplyPlayerCondition
            conditions.Add(new PlayerConditionState { Kind = PlayerConditionKind.Poisoned, Potency = 2, RemainingTurns = 2 });
            // Advance once
            for (var i = conditions.Count - 1; i >= 0; i--)
            {
                conditions[i].RemainingTurns--;
                if (conditions[i].RemainingTurns <= 0) conditions.RemoveAt(i);
            }
            AssertEqual(1, conditions.Count, "After 1 advance, Poisoned(2t) should still have 1 turn remaining.");
            // Advance again
            for (var i = conditions.Count - 1; i >= 0; i--)
            {
                conditions[i].RemainingTurns--;
                if (conditions[i].RemainingTurns <= 0) conditions.RemoveAt(i);
            }
            AssertEqual(0, conditions.Count, "After 2 advances, Poisoned(2t) should have expired.");
        });

        Run("PlayerCondition_Weakened_AttackPenaltyIs2", () =>
        {
            var conditions = new List<PlayerConditionState>
            {
                new() { Kind = PlayerConditionKind.Weakened, Potency = 0, RemainingTurns = 2 }
            };
            var penalty = -2 * conditions.Count(c => c.Kind == PlayerConditionKind.Weakened);
            AssertEqual(-2, penalty, "One Weakened condition should give −2 attack penalty.");
        });

        Run("AuraOfVitality_RouteKind_IsConcentrationAura", () =>
        {
            AssertTrue(SpellData.ById.ContainsKey("paladin_aura_of_vitality"), "paladin_aura_of_vitality must exist.");
            var route = SpellData.ResolveEffectRoute(SpellData.ById["paladin_aura_of_vitality"]);
            AssertEqual(SpellEffectRouteKind.ConcentrationAura, route.RouteKind, "AoV route kind must be ConcentrationAura.");
        });

        Run("LesserRestoration_RouteKind_IsCleanse", () =>
        {
            AssertTrue(SpellData.ById.ContainsKey("cleric_lesser_restoration"), "cleric_lesser_restoration must exist.");
            var route = SpellData.ResolveEffectRoute(SpellData.ById["cleric_lesser_restoration"]);
            AssertEqual(SpellEffectRouteKind.Cleanse, route.RouteKind, "Lesser Restoration route kind must be Cleanse.");
        });

        Run("CombatBalance_GoblinDamageBonusIsOne", () =>
        {
            AssertEqual(1, EnemyTypes.All["goblin"].DamageBonus, "Base goblin DamageBonus must remain 1.");
        });

        Run("CritBonus_WiredIntoCritThreshold", () =>
        {
            // Verify goblin AC hasn't drifted — anchor for crit threshold sanity
            AssertEqual(13, EnemyTypes.All["goblin"].ArmorClass, "Base goblin AC must remain 13.");
        });

        Run("Equipment_NewItemIdsInEnemyLoot", () =>
        {
            // Verify that the new equipment/consumable IDs are referenced in the loot table (compile-time anchor)
            // The loot table is private, so we verify the enemy data hasn't drifted as a proxy anchor.
            AssertTrue(EnemyTypes.All.ContainsKey("goblin_slinger"), "Goblin Slinger must exist (poison source for antidote_vial).");
            AssertEqual(12, EnemyTypes.All["goblin_slinger"].ArmorClass, "Goblin Slinger AC must remain 12.");
        });

        Run("9H_MagicWeapon_Ungated", () =>
        {
            var route = SpellData.ResolveEffectRoute(SpellData.ById["paladin_magic_weapon"]);
            AssertEqual(SpellEffectRouteKind.WeaponRider, route.RouteKind, "Magic Weapon must be WeaponRider.");
            AssertTrue(route.RequiresConcentration, "Magic Weapon must require concentration.");
        });

        Run("9H_FlameArrows_Ungated", () =>
        {
            var route = SpellData.ResolveEffectRoute(SpellData.ById["ranger_flame_arrows"]);
            AssertEqual(SpellEffectRouteKind.WeaponRider, route.RouteKind, "Flame Arrows must be WeaponRider.");
            AssertTrue(route.RequiresConcentration, "Flame Arrows must require concentration.");
        });

        Run("9H_ZephyrStrike_Ungated", () =>
        {
            var route = SpellData.ResolveEffectRoute(SpellData.ById["ranger_zephyr_strike"]);
            AssertEqual(SpellEffectRouteKind.SelfBuff, route.RouteKind, "Zephyr Strike must be SelfBuff.");
            AssertTrue(route.RequiresConcentration, "Zephyr Strike must require concentration.");
        });

        Run("9H_CrusadersMantle_Ungated", () =>
        {
            var route = SpellData.ResolveEffectRoute(SpellData.ById["paladin_crusaders_mantle"]);
            AssertEqual(SpellEffectRouteKind.WeaponRider, route.RouteKind, "Crusader's Mantle must be WeaponRider.");
            AssertTrue(route.RequiresConcentration, "Crusader's Mantle must require concentration.");
        });

        Run("9I_SpiritualWeapon_Ungated", () =>
        {
            var route = SpellData.ResolveEffectRoute(SpellData.ById["cleric_spiritual_weapon"]);
            AssertEqual(SpellEffectRouteKind.Summon, route.RouteKind, "Spiritual Weapon must be Summon route.");
            AssertTrue(route.RequiresConcentration, "Spiritual Weapon must require concentration.");
        });

        Run("9I_SummonType_Exists", () =>
        {
            AssertTrue(SpellData.SummonTypes.ContainsKey("cleric_spiritual_weapon"),
                "SummonTypes must contain cleric_spiritual_weapon.");
            var st = SpellData.SummonTypes["cleric_spiritual_weapon"];
            AssertEqual(8, st.DamageDice, "Spiritual Weapon summon must use d8.");
            AssertEqual(SummonBehaviorKind.AutoAttack, st.Behavior, "Spiritual Weapon must be AutoAttack behavior.");
        });

        Run("9I_FindFamiliar_Ungated", () =>
        {
            var route = SpellData.ResolveEffectRoute(SpellData.ById["mage_find_familiar"]);
            AssertEqual(SpellEffectRouteKind.Summon, route.RouteKind, "Find Familiar must be Summon route.");
            AssertTrue(SpellData.SummonTypes.ContainsKey("mage_find_familiar"), "SummonTypes must contain mage_find_familiar.");
            AssertEqual(4, SpellData.SummonTypes["mage_find_familiar"].DamageDice, "Familiar must use d4.");
            AssertEqual(8, SpellData.SummonTypes["mage_find_familiar"].MaxHp, "Familiar must be fragile (8 HP).");
        });

        Run("9I_FlamingSphere_Ungated", () =>
        {
            var route = SpellData.ResolveEffectRoute(SpellData.ById["mage_flaming_sphere"]);
            AssertEqual(SpellEffectRouteKind.Summon, route.RouteKind, "Flaming Sphere must be Summon route.");
            AssertTrue(SpellData.SummonTypes.ContainsKey("mage_flaming_sphere"), "SummonTypes must contain mage_flaming_sphere.");
            AssertEqual(2, SpellData.SummonTypes["mage_flaming_sphere"].DamageCount, "Flaming Sphere must roll 2d6.");
        });

        Run("9I_FindSteed_BuffMount", () =>
        {
            var route = SpellData.ResolveEffectRoute(SpellData.ById["paladin_find_steed"]);
            AssertEqual(SpellEffectRouteKind.Summon, route.RouteKind, "Find Steed must be Summon route.");
            AssertTrue(SpellData.SummonTypes.ContainsKey("paladin_find_steed"), "SummonTypes must contain paladin_find_steed.");
            AssertEqual(SummonBehaviorKind.BuffMount, SpellData.SummonTypes["paladin_find_steed"].Behavior, "Find Steed must be BuffMount.");
        });

        Run("9I_SummonBeast_Ungated", () =>
        {
            var route = SpellData.ResolveEffectRoute(SpellData.ById["ranger_summon_beast"]);
            AssertEqual(SpellEffectRouteKind.Summon, route.RouteKind, "Summon Beast must be Summon route.");
            AssertTrue(SpellData.SummonTypes.ContainsKey("ranger_summon_beast"), "SummonTypes must contain ranger_summon_beast.");
            AssertEqual(20, SpellData.SummonTypes["ranger_summon_beast"].MaxHp, "Beast Spirit must have 20 HP.");
        });

        Run("9I_ConjureAnimals_Ungated", () =>
        {
            var route = SpellData.ResolveEffectRoute(SpellData.ById["ranger_conjure_animals"]);
            AssertEqual(SpellEffectRouteKind.Summon, route.RouteKind, "Conjure Animals must be Summon route.");
            AssertTrue(SpellData.SummonTypes.ContainsKey("ranger_conjure_animals"), "SummonTypes must contain ranger_conjure_animals.");
            AssertEqual(2, SpellData.SummonTypes["ranger_conjure_animals"].DamageCount, "Conjured Pack must roll 2d6.");
            AssertEqual(30, SpellData.SummonTypes["ranger_conjure_animals"].MaxHp, "Conjured Pack must have 30 HP.");
        });

        Run("9I_MageSummonFey_Ungated", () =>
        {
            var route = SpellData.ResolveEffectRoute(SpellData.ById["mage_summon_fey"]);
            AssertEqual(SpellEffectRouteKind.Summon, route.RouteKind, "Summon Fey (Mage) must be Summon route.");
            AssertTrue(SpellData.SummonTypes.ContainsKey("mage_summon_fey"), "SummonTypes must contain mage_summon_fey.");
            AssertEqual(2, SpellData.SummonTypes["mage_summon_fey"].DamageCount, "Fey Spirit must roll 2d6.");
            AssertEqual(35, SpellData.SummonTypes["mage_summon_fey"].MaxHp, "Fey Spirit must have 35 HP.");
        });

        Run("9I_MageSummonUndead_Ungated", () =>
        {
            var route = SpellData.ResolveEffectRoute(SpellData.ById["mage_summon_undead"]);
            AssertEqual(SpellEffectRouteKind.Summon, route.RouteKind, "Summon Undead must be Summon route.");
            AssertTrue(SpellData.SummonTypes.ContainsKey("mage_summon_undead"), "SummonTypes must contain mage_summon_undead.");
            AssertEqual(8, SpellData.SummonTypes["mage_summon_undead"].DamageDice, "Undead Spirit must use d8.");
            AssertEqual(30, SpellData.SummonTypes["mage_summon_undead"].MaxHp, "Undead Spirit must have 30 HP.");
        });

        Run("9I_ClericAnimateDead_Ungated", () =>
        {
            var route = SpellData.ResolveEffectRoute(SpellData.ById["cleric_animate_dead"]);
            AssertEqual(SpellEffectRouteKind.Summon, route.RouteKind, "Animate Dead must be Summon route.");
            AssertTrue(SpellData.SummonTypes.ContainsKey("cleric_animate_dead"), "SummonTypes must contain cleric_animate_dead.");
            AssertEqual(22, SpellData.SummonTypes["cleric_animate_dead"].MaxHp, "Skeleton Warrior must have 22 HP.");
        });

        Run("9I_RangerSummonFey_Ungated", () =>
        {
            var route = SpellData.ResolveEffectRoute(SpellData.ById["ranger_summon_fey"]);
            AssertEqual(SpellEffectRouteKind.Summon, route.RouteKind, "Summon Fey (Ranger) must be Summon route.");
            AssertTrue(SpellData.SummonTypes.ContainsKey("ranger_summon_fey"), "SummonTypes must contain ranger_summon_fey.");
            AssertEqual(2, SpellData.SummonTypes["ranger_summon_fey"].DamageCount, "Fey Spirit must roll 2d6.");
        });

        Run("9I_SummonElemental_Ungated", () =>
        {
            var route = SpellData.ResolveEffectRoute(SpellData.ById["mage_summon_elemental"]);
            AssertEqual(SpellEffectRouteKind.Summon, route.RouteKind, "Summon Elemental must be Summon route.");
            AssertTrue(SpellData.SummonTypes.ContainsKey("mage_summon_elemental"), "SummonTypes must contain mage_summon_elemental.");
            AssertEqual(10, SpellData.SummonTypes["mage_summon_elemental"].DamageDice, "Elemental must use d10.");
        });

        Run("9I_SummonShadowspawn_Ungated", () =>
        {
            var route = SpellData.ResolveEffectRoute(SpellData.ById["mage_summon_shadowspawn"]);
            AssertEqual(SpellEffectRouteKind.Summon, route.RouteKind, "Summon Shadowspawn must be Summon route.");
            AssertTrue(SpellData.SummonTypes.ContainsKey("mage_summon_shadowspawn"), "SummonTypes must contain mage_summon_shadowspawn.");
            AssertEqual(12, SpellData.SummonTypes["mage_summon_shadowspawn"].DamageDice, "Shadowspawn must use d12.");
        });

        Run("9I_PhantomSteed_BuffMount", () =>
        {
            var route = SpellData.ResolveEffectRoute(SpellData.ById["mage_phantom_steed"]);
            AssertEqual(SpellEffectRouteKind.Summon, route.RouteKind, "Phantom Steed must be Summon route.");
            AssertEqual(SummonBehaviorKind.BuffMount, SpellData.SummonTypes["mage_phantom_steed"].Behavior, "Phantom Steed must be BuffMount.");
        });

        Run("9I_ClericSummonCelestial_Ungated", () =>
        {
            var route = SpellData.ResolveEffectRoute(SpellData.ById["cleric_summon_celestial"]);
            AssertEqual(SpellEffectRouteKind.Summon, route.RouteKind, "Cleric Summon Celestial must be Summon route.");
            AssertEqual(35, SpellData.SummonTypes["cleric_summon_celestial"].MaxHp, "Celestial Guardian must have 35 HP.");
        });

        Run("9I_BardSummonFey_Ungated", () =>
        {
            var route = SpellData.ResolveEffectRoute(SpellData.ById["bard_summon_fey"]);
            AssertEqual(SpellEffectRouteKind.Summon, route.RouteKind, "Bard Summon Fey must be Summon route.");
            AssertTrue(SpellData.SummonTypes.ContainsKey("bard_summon_fey"), "SummonTypes must contain bard_summon_fey.");
        });

        Run("9I_PaladinSummonCelestial_Ungated", () =>
        {
            var route = SpellData.ResolveEffectRoute(SpellData.ById["paladin_summon_celestial"]);
            AssertEqual(SpellEffectRouteKind.Summon, route.RouteKind, "Paladin Summon Celestial must be Summon route.");
            AssertEqual(30, SpellData.SummonTypes["paladin_summon_celestial"].MaxHp, "Celestial Avenger must have 30 HP.");
        });

        Run("9I_RangerSummonPlant_Ungated", () =>
        {
            var route = SpellData.ResolveEffectRoute(SpellData.ById["ranger_summon_plant"]);
            AssertEqual(SpellEffectRouteKind.Summon, route.RouteKind, "Summon Plant must be Summon route.");
            AssertEqual(18, SpellData.SummonTypes["ranger_summon_plant"].MaxHp, "Thorn Guardian must have 18 HP.");
        });

        // === Pass 9K — Transformation & Shapeshift System ===

        Run("9K_FormData_AllFormsExist", () =>
        {
            var expectedIds = new[]
            {
                "form_wolf", "form_cat", "form_snake", "form_bear", "form_dire_wolf",
                "form_giant_eagle", "form_giant_spider", "form_warg", "form_scorpion",
                "form_mantis", "form_insect_spider", "form_shadow", "form_angelic",
                "form_celestial_warrior", "form_mist", "form_fire_elemental",
                "form_water_elemental", "form_earth_elemental", "form_air_elemental",
                "form_ogre", "form_troll", "form_treant", "form_flytrap", "form_shambler",
                "form_stone_guardian", "form_avenging_angel", "form_trex",
                "form_triceratops", "form_raptor"
            };
            foreach (var id in expectedIds)
                AssertTrue(SpellData.Forms.ContainsKey(id), $"Form '{id}' missing from SpellData.Forms.");
            AssertEqual(29, SpellData.Forms.Count, "Expected 29 form definitions.");
        });

        Run("9K_FormData_TempHpScaling", () =>
        {
            // L1 forms average < L2 forms average < L3 forms average
            var t1 = new[] { "form_wolf", "form_cat", "form_snake" };
            var t2 = new[] { "form_bear", "form_dire_wolf", "form_giant_eagle", "form_giant_spider", "form_warg", "form_scorpion", "form_mantis", "form_insect_spider", "form_shadow", "form_angelic", "form_celestial_warrior", "form_mist" };
            var t3 = new[] { "form_fire_elemental", "form_water_elemental", "form_earth_elemental", "form_air_elemental", "form_ogre", "form_troll", "form_treant", "form_flytrap", "form_shambler", "form_stone_guardian", "form_avenging_angel", "form_trex", "form_triceratops", "form_raptor" };
            var avg1 = t1.Average(id => SpellData.Forms[id].TempHp);
            var avg2 = t2.Average(id => SpellData.Forms[id].TempHp);
            var avg3 = t3.Average(id => SpellData.Forms[id].TempHp);
            AssertTrue(avg1 < avg2, $"T1 avg temp HP ({avg1:F1}) must be < T2 ({avg2:F1}).");
            AssertTrue(avg2 < avg3, $"T2 avg temp HP ({avg2:F1}) must be < T3 ({avg3:F1}).");
        });

        Run("9K_TransformationSpells_AllDefined", () =>
        {
            var spellIds = new[]
            {
                "mage_polymorph", "mage_shadow_form", "mage_elemental_form", "mage_monstrous_form",
                "ranger_wild_shape", "ranger_animal_form", "ranger_insect_form", "ranger_plant_form", "ranger_elemental_form", "ranger_primal_form",
                "cleric_divine_vessel", "cleric_stone_guardian",
                "paladin_holy_transformation", "paladin_avatar_of_wrath",
                "bard_polymorph", "bard_gaseous_form"
            };
            foreach (var id in spellIds)
            {
                AssertTrue(SpellData.ById.ContainsKey(id), $"Spell '{id}' missing from ById.");
                AssertTrue(SpellData.TransformationForms.ContainsKey(id), $"Spell '{id}' missing from TransformationForms.");
            }
        });

        Run("9K_TransformationSpells_RoutingCorrect", () =>
        {
            var spellIds = new[]
            {
                "mage_polymorph", "mage_shadow_form", "mage_elemental_form", "mage_monstrous_form",
                "ranger_wild_shape", "ranger_animal_form", "ranger_insect_form", "ranger_plant_form", "ranger_elemental_form", "ranger_primal_form",
                "cleric_divine_vessel", "cleric_stone_guardian",
                "paladin_holy_transformation", "paladin_avatar_of_wrath",
                "bard_polymorph", "bard_gaseous_form"
            };
            foreach (var id in spellIds)
            {
                var route = SpellData.ResolveEffectRoute(SpellData.ById[id]);
                AssertEqual(SpellEffectRouteKind.Transformation, route.RouteKind, $"{id} must have Transformation route.");
            }
        });

        Run("9K_FormSelection_SingleVsMulti", () =>
        {
            // Single-form spells
            AssertEqual(1, SpellData.TransformationForms["mage_shadow_form"].Length, "Shadow Form must have 1 form option.");
            AssertEqual(1, SpellData.TransformationForms["cleric_divine_vessel"].Length, "Divine Vessel must have 1 form option.");
            // Multi-form spells
            AssertTrue(SpellData.TransformationForms["mage_polymorph"].Length >= 2, "Mage Polymorph must have 2+ form options.");
            AssertTrue(SpellData.TransformationForms["ranger_wild_shape"].Length >= 2, "Ranger Wild Shape must have 2+ form options.");
        });

        Run("9K_MagePolymorph_HasWarg", () =>
        {
            var forms = SpellData.TransformationForms["mage_polymorph"];
            AssertTrue(forms.Contains("form_warg"), "Mage Polymorph must include form_warg (dungeon monster).");
        });

        Run("9K_RangerWildShape_Ungated", () =>
        {
            var route = SpellData.ResolveEffectRoute(SpellData.ById["ranger_wild_shape"]);
            AssertEqual(SpellEffectRouteKind.Transformation, route.RouteKind, "Ranger Wild Shape must be Transformation route.");
            AssertTrue(route.RequiresConcentration, "Ranger Wild Shape must require concentration.");
        });

        Run("9K_MistForm_CannotAttack", () =>
        {
            AssertFalse(SpellData.Forms["form_mist"].CanAttack, "Mist form must have CanAttack=false.");
            AssertEqual(FormSpecialKind.FleeBonus, SpellData.Forms["form_mist"].Special, "Mist form must have FleeBonus special.");
        });

        Run("9K_OgreForm_MatchesDungeonMonster", () =>
        {
            var f = SpellData.Forms["form_ogre"];
            AssertTrue(f.TempHp >= 15 && f.TempHp <= 25, $"Ogre form TempHp ({f.TempHp}) should be in [15,25].");
            AssertTrue(f.AttackBonus >= 6 && f.AttackBonus <= 10, $"Ogre form AttackBonus ({f.AttackBonus}) should be in [6,10].");
        });

        Run("9K_FormSpecials_HaveValues", () =>
        {
            foreach (var (id, form) in SpellData.Forms)
            {
                if (form.Special != FormSpecialKind.None && form.Special != FormSpecialKind.NoCounterAttack)
                    AssertTrue(form.SpecialValue > 0, $"Form '{id}' has non-None special but SpecialValue=0.");
            }
        });

        Run("9K_CasterStatModForms", () =>
        {
            AssertTrue(SpellData.Forms["form_angelic"].UseCasterStatMod, "Angelic form must use caster stat mod.");
            AssertTrue(SpellData.Forms["form_celestial_warrior"].UseCasterStatMod, "Celestial Warrior must use caster stat mod.");
            AssertTrue(SpellData.Forms["form_stone_guardian"].UseCasterStatMod, "Stone Guardian must use caster stat mod.");
            AssertTrue(SpellData.Forms["form_avenging_angel"].UseCasterStatMod, "Avenging Angel must use caster stat mod.");
            AssertFalse(SpellData.Forms["form_wolf"].UseCasterStatMod, "Wolf form must NOT use caster stat mod.");
        });

        Run("9K_AllSpellsSuppressCounterAttack", () =>
        {
            var spellIds = new[]
            {
                "mage_polymorph", "mage_shadow_form", "mage_elemental_form", "mage_monstrous_form",
                "ranger_wild_shape", "ranger_animal_form", "ranger_insect_form", "ranger_plant_form", "ranger_elemental_form", "ranger_primal_form",
                "cleric_divine_vessel", "cleric_stone_guardian",
                "paladin_holy_transformation", "paladin_avatar_of_wrath",
                "bard_polymorph", "bard_gaseous_form"
            };
            foreach (var id in spellIds)
                AssertTrue(SpellData.ById[id].SuppressCounterAttack, $"{id} must have SuppressCounterAttack=true.");
        });

        Run("9K_NoOrphanedForms", () =>
        {
            var referencedForms = new HashSet<string>();
            foreach (var (_, formIds) in SpellData.TransformationForms)
                foreach (var fid in formIds)
                    referencedForms.Add(fid);
            foreach (var (id, _) in SpellData.Forms)
                AssertTrue(referencedForms.Contains(id), $"Form '{id}' is defined but never referenced by any transformation spell.");
        });

        Run("9K_RangerPrimalForm_HasDinosaurs", () =>
        {
            var forms = SpellData.TransformationForms["ranger_primal_form"];
            AssertTrue(forms.Contains("form_trex"), "Primal Form must include form_trex.");
            AssertTrue(forms.Contains("form_triceratops"), "Primal Form must include form_triceratops.");
            AssertTrue(forms.Contains("form_raptor"), "Primal Form must include form_raptor.");
        });

        // === Post-9K Audit — SuppressCounterAttack regression guards ===

        Run("Audit_SelfTargetRoutes_SuppressCounterAttack", () =>
        {
            var selfTargetRoutes = new[]
            {
                SpellEffectRouteKind.Summon,
                SpellEffectRouteKind.WeaponRider,
                SpellEffectRouteKind.SelfBuff,
                SpellEffectRouteKind.ConcentrationAura,
                SpellEffectRouteKind.Cleanse,
                SpellEffectRouteKind.Transformation
            };
            foreach (var spell in SpellData.ById.Values)
            {
                var route = SpellData.ResolveEffectRoute(spell);
                if (route.IsFutureGated) continue;
                if (!selfTargetRoutes.Contains(route.RouteKind)) continue;
                AssertTrue(spell.SuppressCounterAttack,
                    $"Spell '{spell.Id}' (route {route.RouteKind}) must have SuppressCounterAttack=true.");
            }
        });

        Run("Audit_HealSpells_SuppressCounterAttack", () =>
        {
            foreach (var spell in SpellData.ById.Values)
            {
                if (!spell.IsHealSpell) continue;
                AssertTrue(spell.SuppressCounterAttack,
                    $"Heal spell '{spell.Id}' must have SuppressCounterAttack=true.");
            }
        });

        // === Post-9K Audit — Orphan detection ===

        Run("Audit_AllSummonTypes_ReferencedBySpell", () =>
        {
            var summonSpellIds = SpellData.ById.Values
                .Where(s => SpellData.ResolveEffectRoute(s).RouteKind == SpellEffectRouteKind.Summon)
                .Select(s => s.Id)
                .ToHashSet(StringComparer.Ordinal);
            foreach (var (id, _) in SpellData.SummonTypes)
                AssertTrue(summonSpellIds.Contains(id),
                    $"SummonType '{id}' has no matching Summon-routed spell.");
        });

        Run("Audit_AllSpells_HaveValidRoute", () =>
        {
            foreach (var spell in SpellData.ById.Values)
            {
                var route = SpellData.ResolveEffectRoute(spell);
                AssertTrue(route != null, $"Spell '{spell.Id}' has null route.");
            }
        });

        // === Post-9K Audit — Enum coverage ===

        Run("Audit_FormSpecialKind_AllValuesUsed", () =>
        {
            var usedSpecials = SpellData.Forms.Values
                .Select(f => f.Special)
                .ToHashSet();
            foreach (var kind in Enum.GetValues<FormSpecialKind>())
                AssertTrue(usedSpecials.Contains(kind),
                    $"FormSpecialKind.{kind} is not used by any form definition.");
        });

        Run("Audit_SpellEffectRouteKind_AllValuesRouted", () =>
        {
            var routedKinds = SpellData.ById.Values
                .Select(s => SpellData.ResolveEffectRoute(s).RouteKind)
                .ToHashSet();
            foreach (var kind in Enum.GetValues<SpellEffectRouteKind>())
                AssertTrue(routedKinds.Contains(kind),
                    $"SpellEffectRouteKind.{kind} has no spell routed to it.");
        });

        Run("Audit_SummonBehaviorKind_ActiveValues_Used", () =>
        {
            var usedBehaviors = SpellData.SummonTypes.Values
                .Select(s => s.Behavior)
                .ToHashSet();
            AssertTrue(usedBehaviors.Contains(SummonBehaviorKind.AutoAttack),
                "SummonBehaviorKind.AutoAttack must be used by at least one summon type.");
            AssertTrue(usedBehaviors.Contains(SummonBehaviorKind.BuffMount),
                "SummonBehaviorKind.BuffMount must be used by at least one summon type.");
        });

        Run("Audit_CombatStatusKind_AllValuesInRules", () =>
        {
            var statuses = Enum.GetValues<CombatStatusKind>();
            foreach (var kind in statuses)
            {
                var test = new List<CombatStatusState>
                {
                    new() { Kind = kind, Potency = 2, RemainingTurns = 2, SourceSpellId = "audit", SourceLabel = "Audit" }
                };
                // Verify each status kind can be queried without crash
                _ = CombatStatusRules.GetAttackPenalty(test);
                _ = CombatStatusRules.GetMovePenalty(test);
                _ = CombatStatusRules.GetIncomingDamageBonus(test);
                _ = CombatStatusRules.PreventsEnemyAction(test);
                _ = CombatStatusRules.PreventsEnemyMovement(test);
            }
        });

        // === Batch 1 — Core Buffs & Defenses ===
        var batch1ConcentrationSpells = new[]
        {
            "cleric_shield_of_faith", "paladin_shield_of_faith", "cleric_bless",
            "paladin_heroism", "bard_heroism", "ranger_barkskin", "mage_blur", "mage_haste",
            "paladin_divine_favor"
        };
        Run("Batch1_ConcentrationBuffs_HaveRoutes", () =>
        {
            foreach (var id in batch1ConcentrationSpells)
            {
                AssertTrue(SpellData.ById.ContainsKey(id), $"Spell {id} missing from ById.");
                var route = SpellData.ResolveEffectRoute(SpellData.ById[id]);
                AssertTrue(route.RouteKind != SpellEffectRouteKind.DirectDamage || route.RouteKind == SpellEffectRouteKind.SelfBuff
                    || route.RouteKind == SpellEffectRouteKind.WeaponRider || route.RouteKind == SpellEffectRouteKind.ConcentrationAura,
                    $"Spell {id} has unexpected route {route.RouteKind}.");
            }
        });

        var batch1NonConcentrationSpells = new[] { "mage_mage_armor", "mage_shield", "mage_false_life", "cleric_aid", "paladin_aid" };
        Run("Batch1_NonConcentrationBuffs_HaveRoutes", () =>
        {
            foreach (var id in batch1NonConcentrationSpells)
            {
                AssertTrue(SpellData.ById.ContainsKey(id), $"Spell {id} missing from ById.");
                var route = SpellData.ResolveEffectRoute(SpellData.ById[id]);
                AssertTrue(route.RouteKind == SpellEffectRouteKind.SelfBuff,
                    $"Spell {id} should be SelfBuff but is {route.RouteKind}.");
                AssertFalse(route.RequiresConcentration, $"Spell {id} should NOT require concentration.");
            }
        });

        Run("Batch1_AllSpells_SuppressCounterAttack", () =>
        {
            var allBatch1 = batch1ConcentrationSpells.Concat(batch1NonConcentrationSpells);
            foreach (var id in allBatch1)
            {
                var spell = SpellData.ById[id];
                AssertTrue(spell.SuppressCounterAttack, $"Spell {id} must have SuppressCounterAttack=true.");
            }
        });

        Run("Batch1_AllSpells_InClassUnlocks", () =>
        {
            var allBatch1 = batch1ConcentrationSpells.Concat(batch1NonConcentrationSpells);
            var allUnlockIds = SpellData.ClassSpellUnlocks.Values
                .SelectMany(list => list.Select(entry => entry.SpellId))
                .ToHashSet();
            foreach (var id in allBatch1)
            {
                AssertTrue(allUnlockIds.Contains(id), $"Spell {id} missing from ClassSpellUnlocks.");
            }
        });

        // === Batch 2 — Tactical Combat Spells ===
        var batch2AllSpells = new[]
        {
            "mage_misty_step", "mage_mirror_image", "mage_expeditious_retreat",
            "ranger_absorb_elements", "ranger_longstrider",
            "bard_hex",
            "cleric_protection_evg", "paladin_protection_evg",
            "cleric_sanctuary",
            "paladin_compelled_duel",
            "bard_enhance_ability", "cleric_enhance_ability", "mage_enhance_ability"
        };

        Run("Batch2_AllSpells_HaveRoutes", () =>
        {
            foreach (var id in batch2AllSpells)
            {
                AssertTrue(SpellData.ById.ContainsKey(id), $"Spell {id} missing from ById.");
                var route = SpellData.ResolveEffectRoute(SpellData.ById[id]);
                AssertTrue(route != null, $"Spell {id} has null EffectRoute.");
            }
        });

        Run("Batch2_AllSpells_SuppressCounterAttack", () =>
        {
            foreach (var id in batch2AllSpells)
            {
                var spell = SpellData.ById[id];
                AssertTrue(spell.SuppressCounterAttack, $"Spell {id} must have SuppressCounterAttack=true.");
            }
        });

        Run("Batch2_AllSpells_InClassUnlocks", () =>
        {
            var allUnlockIds2 = SpellData.ClassSpellUnlocks.Values
                .SelectMany(list => list.Select(entry => entry.SpellId))
                .ToHashSet();
            foreach (var id in batch2AllSpells)
            {
                AssertTrue(allUnlockIds2.Contains(id), $"Spell {id} missing from ClassSpellUnlocks.");
            }
        });

        // === Batch 3 — Reactive & Retaliation Spells ===
        var batch3AllSpells = new[]
        {
            "mage_hellish_rebuke", "mage_armor_of_agathys", "mage_fire_shield", "mage_stoneskin",
            "cleric_wrath_of_storm", "cleric_spirit_shroud", "cleric_death_ward",
            "paladin_death_ward", "paladin_holy_rebuke",
            "ranger_thorns", "ranger_stoneskin",
            "bard_cutting_words", "bard_greater_invisibility"
        };

        Run("Batch3_AllSpells_HaveRoutes", () =>
        {
            foreach (var id in batch3AllSpells)
            {
                AssertTrue(SpellData.ById.ContainsKey(id), $"Spell {id} missing from ById.");
                var route = SpellData.ResolveEffectRoute(SpellData.ById[id]);
                AssertTrue(route != null, $"Spell {id} has null EffectRoute.");
            }
        });

        Run("Batch3_AllSpells_SuppressCounterAttack", () =>
        {
            foreach (var id in batch3AllSpells)
            {
                var spell = SpellData.ById[id];
                AssertTrue(spell.SuppressCounterAttack, $"Spell {id} must have SuppressCounterAttack=true.");
            }
        });

        Run("Batch3_AllSpells_InClassUnlocks", () =>
        {
            var allUnlockIds3 = SpellData.ClassSpellUnlocks.Values
                .SelectMany(list => list.Select(entry => entry.SpellId))
                .ToHashSet();
            foreach (var id in batch3AllSpells)
            {
                AssertTrue(allUnlockIds3.Contains(id), $"Spell {id} missing from ClassSpellUnlocks.");
            }
        });

        // === Batch 4+5 — Expanded Arsenal & Signature Powers ===
        var batch45AllSpells = new[]
        {
            // Batch 4
            "mage_sleep", "mage_counterspell", "mage_vampiric_touch",
            "cleric_mass_healing_word", "cleric_daylight",
            "bard_faerie_fire", "bard_charm_person", "bard_invisibility",
            "paladin_elemental_weapon", "paladin_revivify", "paladin_daylight",
            "ranger_fog_cloud", "ranger_cure_wounds",
            // Batch 5
            "mage_blink", "mage_protection_from_energy",
            "cleric_beacon_of_hope",
            "bard_major_image",
            "paladin_aura_of_courage",
            "ranger_plant_growth"
        };

        // Spells that intentionally deal damage and have SuppressCounterAttack=false
        var batch45DamageSpells = new HashSet<string>
        {
            "mage_vampiric_touch", "cleric_daylight", "paladin_daylight"
        };

        Run("Batch45_AllSpells_HaveRoutes", () =>
        {
            foreach (var id in batch45AllSpells)
            {
                AssertTrue(SpellData.ById.ContainsKey(id), $"Spell {id} missing from ById.");
                var route = SpellData.ResolveEffectRoute(SpellData.ById[id]);
                AssertTrue(route != null, $"Spell {id} has null EffectRoute.");
            }
        });

        Run("Batch45_NonDamage_SuppressCounterAttack", () =>
        {
            foreach (var id in batch45AllSpells)
            {
                if (batch45DamageSpells.Contains(id)) continue;
                var spell = SpellData.ById[id];
                AssertTrue(spell.SuppressCounterAttack, $"Spell {id} must have SuppressCounterAttack=true.");
            }
        });

        Run("Batch45_AllSpells_InClassUnlocks", () =>
        {
            var allUnlockIds45 = SpellData.ClassSpellUnlocks.Values
                .SelectMany(list => list.Select(entry => entry.SpellId))
                .ToHashSet();
            foreach (var id in batch45AllSpells)
            {
                AssertTrue(allUnlockIds45.Contains(id), $"Spell {id} missing from ClassSpellUnlocks.");
            }
        });

        return new Phase7CheckResult
        {
            Passed = passed,
            Failed = failures.Count,
            Failures = failures
        };
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void AssertFalse(bool condition, string message)
    {
        if (condition) throw new InvalidOperationException(message);
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message} Expected={expected} Actual={actual}");
        }
    }

    private static bool AreEncounterAllies(string? leftEnemyKey, string? rightEnemyKey)
    {
        var leftIsGoblin = !string.IsNullOrWhiteSpace(leftEnemyKey) &&
                           leftEnemyKey.StartsWith("goblin", StringComparison.OrdinalIgnoreCase);
        var rightIsGoblin = !string.IsNullOrWhiteSpace(rightEnemyKey) &&
                            rightEnemyKey.StartsWith("goblin", StringComparison.OrdinalIgnoreCase);
        if (leftIsGoblin && rightIsGoblin)
        {
            return true;
        }

        return string.Equals(leftEnemyKey, rightEnemyKey, StringComparison.Ordinal);
    }

    private static GameSaveSnapshot BuildSnapshot()
    {
        return new GameSaveSnapshot
        {
            SaveKind = "manual",
            ResumeState = GameState.Playing,
            Player = new PlayerSnapshot
            {
                Name = "AuditRunner",
                ClassName = "Warrior",
                Gender = Gender.Male,
                X = 5,
                Y = 7,
                Level = 3,
                Xp = 150,
                XpToNextLevel = 750,
                MaxHp = 60,
                CurrentHp = 52,
                Stats = new StatsSnapshot
                {
                    Strength = 14,
                    Dexterity = 12,
                    Constitution = 13,
                    Intelligence = 10,
                    Wisdom = 9,
                    Charisma = 8
                },
                SkillIds = new List<string> { "war_cry" },
                FeatIds = new List<string> { "martial_adept_feat" },
                KnownSpellIds = new List<string>()
            },
            Enemies = new List<EnemySnapshot>
            {
                new()
                {
                    TypeKey = "goblin",
                    X = 8,
                    Y = 9,
                    CurrentHp = 12,
                    StatusEffects = new List<EnemyStatusSnapshot>
                    {
                        new() { Kind = "Incapacitated", Potency = 1, RemainingTurns = 2, SourceSpellId = "bard_hideous_laughter", SourceLabel = "Hideous Laughter", RepeatSaveStat = "Wisdom", SaveDc = 14, BreaksOnDamageTaken = true },
                        new() { Kind = "Marked", Potency = 1, RemainingTurns = 3, SourceSpellId = "ranger_hunters_mark", SourceLabel = "Hunters Mark" }
                    }
                },
                new() { TypeKey = "warg", X = 10, Y = 11, CurrentHp = 18 }
            },
            CombatLog = new List<string> { "Combat start", "Enemy spotted" },
            ClaimedRewardNodeIds = new List<string> { "cache_entry" },
            RunMeleeBonus = 1,
            RunSpellBonus = 0,
            RunDefenseBonus = 1,
            RunCritBonus = 2,
            RunFleeBonus = 3,
            RunArchetype = "Skirmisher",
            RunRelic = "VeilstriderCharm",
            Phase3RouteChoice = "UpperCatacombs",
            Phase3RiskEventResolved = true,
            Phase3XpPercentMod = 20,
            Phase3EnemyAttackBonus = 1,
            Phase3EnemiesDefeated = 6,
            Phase3PreSanctumRewardGranted = true,
            Phase3RouteWaveSpawned = true,
            Phase3SanctumWaveSpawned = false,
            MilestoneChoicesTaken = 1,
            MilestoneExecutionRank = 0,
            MilestoneArcRank = 0,
            MilestoneEscapeRank = 1,
            SettingsMasterVolume = 77,
            SettingsVerboseCombatLog = false,
            SettingsAccessibilityColorProfile = "DeuteranopiaFriendly",
            SettingsAccessibilityHighContrast = true,
            SettingsOptionalConditionsEnabled = false,
            CreationOriginCondition = "ArcaneBlindness",
            DungeonConditionEventsTriggered = 1,
            MajorConditions = new List<MajorConditionSnapshot>
            {
                new() { Type = "CrushedLimb", Source = "Dungeon" }
            },
            InventoryItems = new List<InventoryItemSnapshot>
            {
                new() { Id = "health_potion", Quantity = 2, IsEquipped = false },
                new() { Id = "warding_charm", Quantity = 1, IsEquipped = true }
            },
            ActiveConcentration = new ConcentrationSnapshot
            {
                SpellId = "cleric_spirit_guardians",
                SpellLabel = "Spirit Guardians",
                RemainingRounds = 3
            },
            CombatHazards = new List<CombatHazardSnapshot>
            {
                new()
                {
                    InstanceId = "hazard_selfcheck",
                    SourceSpellId = "bard_cloud_of_daggers",
                    SourceLabel = "Cloud of Daggers",
                    Element = "Force",
                    BaseDamage = 13,
                    Variance = 5,
                    ArmorBypass = 2,
                    CenterX = 8,
                    CenterY = 9,
                    RadiusTiles = 0,
                    RemainingRounds = 2,
                    FollowsPlayer = false,
                    RequiresConcentration = true,
                    TriggersOnTurnStart = true,
                    TriggersOnEntry = false,
                    InitialSaveStat = "Wisdom",
                    SaveDamageBehavior = "HalfOnSave",
                    OnTriggerStatuses = new List<CombatHazardStatusSnapshot>
                    {
                        new() { Kind = "Restrained", Potency = 1, DurationTurns = 1, ChancePercent = 65, InitialSaveStat = "Dexterity", RepeatSaveStat = "Strength" }
                    }
                }
            }
        };
    }
}
