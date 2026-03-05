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
            AssertTrue(FeatBook.All.Count >= 60, $"Feat catalog too small. Count={FeatBook.All.Count}.");
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
                MaxMana = 15,
                CurrentMana = 11,
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
                new() { TypeKey = "goblin", X = 8, Y = 9, CurrentHp = 12 },
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
            }
        };
    }
}
