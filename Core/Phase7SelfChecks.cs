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
            AssertTrue(GameStateRules.IsCombatState(GameState.CombatItemMenu), "CombatItemMenu should be combat-state.");
            AssertFalse(GameStateRules.IsCombatState(GameState.PauseMenu), "PauseMenu should not be combat-state.");
        });

        Run("StateRules_ResumeBehavior", () =>
        {
            AssertEqual(GameState.Combat, GameStateRules.ResolveResumeState(GameState.Combat, hasActiveEnemy: true), "Pause resume should return to combat with active enemy.");
            AssertEqual(GameState.Playing, GameStateRules.ResolveResumeState(GameState.Combat, hasActiveEnemy: false), "Pause resume should fallback to playing without enemy.");
            AssertEqual(GameState.Playing, GameStateRules.ResolveResumeState(GameState.Playing, hasActiveEnemy: true), "Pause resume should return to playing when paused from playing.");
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
