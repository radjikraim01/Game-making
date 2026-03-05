using Raylib_cs;

namespace DungeonEscape.Core;

public sealed class Game : IDisposable
{
    private enum InventoryItemKind
    {
        Consumable,
        Equipment
    }

    private enum EquipmentSlot
    {
        MainHand,
        OffHand,
        Armor,
        Head,
        Goggles,
        Neck,
        Cloak,
        Shirt,
        Bracers,
        Gloves,
        Belt,
        KneePads,
        Boots,
        Ring
    }

    private enum PauseConfirmAction
    {
        None,
        OverwriteSave,
        LoadRun,
        QuitToTitle
    }

    private sealed class InventoryItem
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public required string Description { get; init; }
        public required InventoryItemKind Kind { get; init; }
        public EquipmentSlot? Slot { get; init; }
        public int Quantity { get; set; }
        public bool IsEquipped { get; set; }
        public int? EquippedSlotIndex { get; set; }
    }

    private sealed class RewardNode
    {
        public required string Id { get; init; }
        public required int X { get; init; }
        public required int Y { get; init; }
        public required string Name { get; init; }
        public required string Description { get; init; }
    }

    private sealed class AtmosParticle
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float DriftX { get; set; }
        public float RiseSpeed { get; set; }
        public float Size { get; set; }
        public float Alpha { get; set; }
        public float Phase { get; set; }
    }

    private enum PauseMenuView
    {
        Root,
        Inventory,
        Save,
        Load,
        Settings,
        Accessibility
    }

    private enum AccessibilityColorProfile
    {
        Default,
        DeuteranopiaFriendly
    }

    private enum CreationConditionPreset
    {
        None,
        ArcaneBlindness,
        CrushedLimb
    }

    private enum MajorConditionType
    {
        ArcaneBlindness,
        CrushedLimb
    }

    private sealed class MajorConditionState
    {
        public required MajorConditionType Type { get; init; }
        public required string Source { get; init; }
    }

    private enum EnemyAiState
    {
        Patrol,
        Investigate,
        Chase,
        Search,
        Return
    }

    private enum RunArchetype
    {
        None,
        Vanguard,
        Arcanist,
        Skirmisher
    }

    private enum RunRelic
    {
        None,
        BloodwakeEmblem,
        AstralConduit,
        VeilstriderCharm
    }

    private enum Phase3RouteChoice
    {
        None,
        UpperCatacombs,
        LowerShrine
    }

    private enum FloorMacroZone
    {
        EntryFrontier,
        BranchingDepths,
        SanctumRing
    }

    private sealed class EnemyAiRuntime
    {
        public EnemyAiState State { get; set; } = EnemyAiState.Patrol;
        public int LastKnownPlayerX { get; set; }
        public int LastKnownPlayerY { get; set; }
        public double LastSeenPlayerAt { get; set; } = -1;
        public double SearchEndsAt { get; set; } = -1;
        public double NextMoveAt { get; set; } = -1;
        public int FacingX { get; set; } = 1;
        public int FacingY { get; set; }
        public int PatrolDirectionIndex { get; set; }
    }

    private enum LootRarity
    {
        Common,
        Uncommon,
        Rare
    }

    private sealed class GroundLoot
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public required LootRarity Rarity { get; init; }
        public required int X { get; init; }
        public required int Y { get; init; }
        public string? InventoryItemId { get; set; }
        public int InventoryItemQuantity { get; set; }
    }

    private sealed class LootTemplate
    {
        public required string Name { get; init; }
        public required LootRarity Rarity { get; init; }
        public string? InventoryItemId { get; init; }
        public int MinItemQuantity { get; init; }
        public int MaxItemQuantity { get; init; }
    }

    private sealed class EnemyLootKit
    {
        public required string Name { get; init; }
        public required LootRarity Rarity { get; init; }
        public string? ItemId { get; set; }
        public int ItemQuantity { get; set; }
        public int AttackBonus { get; set; }
        public bool UsedConsumableThisFight { get; set; }
    }

    private readonly Random _rng = new();
    private readonly GameMap _map = new();
    private readonly List<RewardNode> _rewardNodes = new()
    {
        new() { Id = "cache_entry", X = 12, Y = 9, Name = "Scout Cache", Description = "Supplies left by a prior expedition." },
        new() { Id = "altar_shrine", X = 31, Y = 21, Name = "Broken Altar", Description = "A fractured relic still humming with power." },
        new() { Id = "vault_sanctum", X = 50, Y = 27, Name = "War Vault", Description = "A sealed chest beside the boss wing." }
    };
    private readonly HashSet<string> _claimedRewardNodeIds = new();
    private readonly List<InventoryItem> _inventoryItems = new();
    private readonly List<AtmosParticle> _emberParticles = new();
    private readonly Dictionary<Enemy, EnemyAiRuntime> _enemyAi = new();
    private readonly Dictionary<Enemy, EnemyLootKit> _enemyLootKits = new();
    private readonly List<GroundLoot> _groundLoot = new();
    private Player? _player;
    private List<Enemy> _enemies = new();

    private GameState _gameState = GameState.StartMenu;

    private int _selectedClassIndex;
    private int _selectedGenderIndex;
    private int _selectedRaceIndex;
    private int _selectedStatIndex;
    private int _selectedSkillIndex;
    private int _selectedFeatIndex;
    private int _selectedSpellLearnIndex;
    private int _selectedCreationFeatIndex;
    private int _selectedCreationIdentityIndex;
    private int _selectedAppearanceIndex;
    private int _selectedCreationConditionIndex;
    private int _selectedActionIndex;
    private int _selectedCombatSkillIndex;
    private int _selectedSpellIndex;
    private int _selectedCombatItemIndex;
    private int _skillMenuOffset;
    private int _spellMenuOffset;
    private int _combatItemMenuOffset;
    private int _spellLearnMenuOffset;
    private int _creationFeatMenuOffset;
    private int _featMenuOffset;
    private int _characterSheetScroll;
    private int _creationSectionIndex;
    private int _startMenuIndex;
    private int _creationSelectionIndex;
    private int _creationPointsRemaining;
    private int _pauseMenuIndex;
    private int _selectedRewardOptionIndex;

    private string _startMenuMessage = string.Empty;
    private string _pendingName = string.Empty;
    private string _creationMessage = string.Empty;
    private string _spellSelectionTitle = string.Empty;
    private string _selectionMessage = string.Empty;
    private string _pauseMessage = string.Empty;
    private string _rewardMessage = string.Empty;
    private string _selectedPlayerSpriteId = "knight_m";
    private bool _spellSelectionStartsAdventure;
    private bool _rewardMessageRequiresAcknowledge;
    private bool _bossDefeated;
    private bool _floorCleared;
    private RunArchetype _runArchetype = RunArchetype.None;
    private RunRelic _runRelic = RunRelic.None;
    private Phase3RouteChoice _phase3RouteChoice = Phase3RouteChoice.None;
    private bool _phase3RiskEventResolved;
    private int _phase3XpPercentMod;
    private int _phase3EnemyAttackBonus;
    private int _phase3EnemiesDefeated;
    private bool _phase3PreSanctumRewardGranted;
    private bool _phase3RouteWaveSpawned;
    private bool _phase3SanctumWaveSpawned;
    private bool _phase3SanctumLockNoticeShown;
    private FloorMacroZone _currentFloorZone = FloorMacroZone.EntryFrontier;
    private bool _relicMeleeTriggerUsedThisCombat;
    private bool _relicSpellTriggerUsedThisCombat;
    private bool _relicFleeTriggerUsedThisCombat;
    private int _milestoneChoicesTaken;
    private int _milestoneExecutionRank;
    private int _milestoneArcRank;
    private int _milestoneEscapeRank;
    private int _milestoneArcChargesThisCombat;
    private int _milestoneEscapeChargesThisCombat;
    private int _runMeleeBonus;
    private int _runSpellBonus;
    private int _runDefenseBonus;
    private int _runCritBonus;
    private int _runFleeBonus;
    private RewardNode? _activeRewardNode;
    private GameState _spellSelectionNextState = GameState.Playing;
    private GameState _pausedFromState = GameState.Playing;
    private readonly List<Skill> _skillChoices = new();
    private readonly List<FeatDefinition> _featChoices = new();
    private readonly List<SpellDefinition> _spellLearnChoices = new();
    private readonly List<SpellDefinition> _creationLearnableSpells = new();
    private readonly List<FeatDefinition> _creationFeatChoices = new();
    private readonly HashSet<string> _creationChosenSpellIds = new();
    private readonly List<string> _creationChosenSpellOrder = new();
    private readonly HashSet<string> _creationChosenFeatIds = new();
    private readonly List<string> _creationChosenFeatOrder = new();
    private readonly int[] _creationAllocatedStats = new int[6];
    private readonly List<int> _creationStatAllocationOrder = new();
    private readonly List<SaveEntrySummary> _pauseSaveEntries = new();
    private readonly List<SaveEntrySummary> _pauseLoadEntries = new();
    private readonly List<string> _combatLog = new();
    private Enemy? _currentEnemy;
    private int _enemyPoisoned;
    private bool _warCryAvailable;
    private int _packEnemiesRemainingAfterCurrent;
    private bool _encounterActive;
    private int _encounterRound = 1;
    private readonly List<Enemy> _encounterEnemies = new();
    private readonly List<EncounterInitiativeSlot> _encounterTurnOrder = new();
    private int _encounterTurnIndex;
    private string _encounterCurrentCombatantId = string.Empty;
    private int _selectedEncounterTargetIndex = -1;
    private string _pendingCombatSpellId = string.Empty;
    private bool _combatMoveModeActive;
    private int _combatMovePointsMax;
    private int _combatMovePointsRemaining;
    private bool _resolvingEnemyDeath;
    private int _settingsMasterVolume = 80;
    private bool _settingsVerboseCombatLog = true;
    private AccessibilityColorProfile _settingsAccessibilityColorProfile = AccessibilityColorProfile.Default;
    private bool _settingsAccessibilityHighContrast;
    private bool _settingsOptionalConditionsEnabled = true;
    private CreationConditionPreset _creationOriginCondition = CreationConditionPreset.None;
    private readonly List<MajorConditionState> _activeMajorConditions = new();
    private int _dungeonConditionEventsTriggered;
    private PauseConfirmAction _pauseConfirmAction = PauseConfirmAction.None;
    private int _pauseConfirmTarget = -1;

    private double _enemyResolveAt = -1;
    private Enemy? _defeatedEnemyPending;
    private double _respawnEnemiesAt = -1;
    private PauseMenuView _pauseMenuView = PauseMenuView.Root;
    private int _atmosphereScreenW;
    private int _atmosphereScreenH;
    private readonly SpriteLibrary _spriteLibrary = new();
    private double _playerRunAnimUntil = -1;
    private double _nextMoveAt = -1;
    private double _rewardMessageExpiresAt = -1;
    private string? _activePickupLootId;
    private double _activePickupProgressSeconds;
    private string _activePickupStatus = string.Empty;
    private double _activePickupStatusUntil = -1;
    private bool _debugOverlayEnabled;
    private bool _cameraTargetInitialized;
    private System.Numerics.Vector2 _cameraTarget;

    private static readonly Gender[] Genders = { Gender.Male, Gender.Female };
    private static readonly Race[] Races = { Race.Human, Race.Elf, Race.Dwarf };
    private static readonly string[] PauseRootOptions =
    {
        "Resume",
        "Inventory",
        "Save Game",
        "Load Game",
        "Settings",
        "Quit To Title"
    };
    private static readonly string[] PauseSettingsOptions =
    {
        "Toggle Fullscreen",
        "Master Volume",
        "Combat Log Detail",
        "Accessibility",
        "Back"
    };
    private static readonly string[] PauseAccessibilityOptions =
    {
        "Color Profile",
        "High Contrast UI",
        "Optional Conditions",
        "Purge Major Condition",
        "Back"
    };
    private static readonly (CreationConditionPreset Id, string Label, string Description)[] CreationConditionOptions =
    {
        (
            CreationConditionPreset.None,
            "None",
            "No starting condition."
        ),
        (
            CreationConditionPreset.ArcaneBlindness,
            "Arcane Blindness",
            "No normal color sense. Magic sense grants perception while mana is available."
        ),
        (
            CreationConditionPreset.CrushedLimb,
            "Crushed Limb",
            "Severe mobility/combat strain. High-tier restoration can remove it."
        )
    };
    private static readonly string[] CreationSections = { "Identity", "Class", "Stats", "Spells", "Feats", "Review" };
    private const int SkillVisibleCount = 4;
    private const int SpellMenuVisibleCount = 6;
    private const int CombatItemVisibleCount = 6;
    private const int SpellLearnVisibleCount = 5;
    private const int CreationFeatVisibleCount = 5;
    private const int FeatVisibleCount = 4;
    private static readonly StatName[] StatOrder =
    {
        StatName.Strength,
        StatName.Dexterity,
        StatName.Constitution,
        StatName.Intelligence,
        StatName.Wisdom,
        StatName.Charisma
    };

    private static readonly Dictionary<Gender, string> GenderDescriptions = new()
    {
        [Gender.Male] = "+1 Strength, +1 Constitution",
        [Gender.Female] = "+1 Dexterity, +1 Wisdom"
    };

    private static readonly Dictionary<Race, string> RaceDescriptions = new()
    {
        [Race.Human] = "Balanced and adaptable across all classes.",
        [Race.Elf] = "Agile and perceptive, fitting scouts and casters.",
        [Race.Dwarf] = "Hardy and resilient, great frontline survivability."
    };

    private static readonly (string Id, string Label)[] PlayerAppearanceOptions =
    {
        ("knight_m", "Knight (Male)"),
        ("knight_f", "Knight (Female)"),
        ("elf_m", "Elf (Male)"),
        ("elf_f", "Elf (Female)"),
        ("wizzard_m", "Wizard (Male)"),
        ("wizzard_f", "Wizard (Female)"),
        ("dwarf_m", "Dwarf (Male)"),
        ("dwarf_f", "Dwarf (Female)"),
        ("doc", "Cleric")
    };

    private List<string> GetStartMenuOptions()
    {
        var options = new List<string>();
        if (SaveStore.GetAvailableLoadEntries().Count > 0)
        {
            options.Add("Continue");
        }

        options.Add("New Game");
        options.Add("How To Play");
        options.Add("Quit");
        return options;
    }

    private CreationConditionPreset GetSelectedCreationConditionPreset()
    {
        if (_selectedCreationConditionIndex < 0 || _selectedCreationConditionIndex >= CreationConditionOptions.Length)
        {
            return CreationConditionPreset.None;
        }

        return CreationConditionOptions[_selectedCreationConditionIndex].Id;
    }

    private static MajorConditionType? TryMapCreationConditionToMajor(CreationConditionPreset preset)
    {
        return preset switch
        {
            CreationConditionPreset.ArcaneBlindness => MajorConditionType.ArcaneBlindness,
            CreationConditionPreset.CrushedLimb => MajorConditionType.CrushedLimb,
            _ => null
        };
    }

    private static string GetCreationConditionLabel(CreationConditionPreset preset)
    {
        return preset switch
        {
            CreationConditionPreset.ArcaneBlindness => "Arcane Blindness",
            CreationConditionPreset.CrushedLimb => "Crushed Limb",
            _ => "None"
        };
    }

    private static string GetMajorConditionLabel(MajorConditionType type)
    {
        return type switch
        {
            MajorConditionType.ArcaneBlindness => "Arcane Blindness",
            MajorConditionType.CrushedLimb => "Crushed Limb",
            _ => "Unknown Condition"
        };
    }

    private static string GetMajorConditionEffectSummary(MajorConditionType type)
    {
        return type switch
        {
            MajorConditionType.ArcaneBlindness => "No normal color vision; magic sense while mana remains, severe penalties at 0 mana.",
            MajorConditionType.CrushedLimb => "Melee, defense, and flee performance reduced until restored.",
            _ => "Condition effects unknown."
        };
    }

    private string GetConditionPurgeCostLabel()
    {
        return $"{ConditionPurgeHealthPotionCost} Health Potion, {ConditionPurgeManaDraughtCost} Mana Draught, {ConditionPurgeSharpeningOilCost} Sharpening Oil";
    }

    private bool IsMajorConditionActive(MajorConditionType type)
    {
        if (!_settingsOptionalConditionsEnabled)
        {
            return false;
        }

        return _activeMajorConditions.Any(condition => condition.Type == type);
    }

    private bool IsBlindMageModeActive()
    {
        return IsMajorConditionActive(MajorConditionType.ArcaneBlindness);
    }

    private bool IsBlindMageMagicSenseActive()
    {
        return IsBlindMageModeActive() && _player != null && _player.CurrentMana > 0;
    }

    private int GetConditionMeleeModifier()
    {
        if (!_settingsOptionalConditionsEnabled)
        {
            return 0;
        }

        var modifier = 0;
        if (_activeMajorConditions.Any(condition => condition.Type == MajorConditionType.CrushedLimb))
        {
            modifier -= 2;
        }

        if (IsMajorConditionActive(MajorConditionType.ArcaneBlindness) && !IsBlindMageMagicSenseActive())
        {
            modifier -= 1;
        }

        return modifier;
    }

    private int GetConditionSpellModifier()
    {
        if (!_settingsOptionalConditionsEnabled)
        {
            return 0;
        }

        if (!IsMajorConditionActive(MajorConditionType.ArcaneBlindness))
        {
            return 0;
        }

        return IsBlindMageMagicSenseActive() ? 1 : -2;
    }

    private int GetConditionDefenseModifier()
    {
        if (!_settingsOptionalConditionsEnabled)
        {
            return 0;
        }

        return _activeMajorConditions.Any(condition => condition.Type == MajorConditionType.CrushedLimb) ? -1 : 0;
    }

    private int GetConditionFleeModifier()
    {
        if (!_settingsOptionalConditionsEnabled)
        {
            return 0;
        }

        var modifier = 0;
        if (_activeMajorConditions.Any(condition => condition.Type == MajorConditionType.CrushedLimb))
        {
            modifier -= 10;
        }

        if (IsMajorConditionActive(MajorConditionType.ArcaneBlindness) && !IsBlindMageMagicSenseActive())
        {
            modifier -= 15;
        }

        return modifier;
    }

    private string GetActiveMajorConditionSummary()
    {
        if (!_settingsOptionalConditionsEnabled)
        {
            return "Disabled";
        }

        if (_activeMajorConditions.Count == 0)
        {
            return "None";
        }

        var labels = _activeMajorConditions
            .Select(condition => GetMajorConditionLabel(condition.Type))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        return string.Join(", ", labels);
    }

    private bool TryApplyMajorCondition(MajorConditionType type, string source, out string message)
    {
        if (_activeMajorConditions.Any(condition => condition.Type == type))
        {
            message = $"{GetMajorConditionLabel(type)} is already active.";
            return false;
        }

        _activeMajorConditions.Add(new MajorConditionState
        {
            Type = type,
            Source = source
        });

        message = $"Major condition gained: {GetMajorConditionLabel(type)} ({source}). {GetMajorConditionEffectSummary(type)}";
        return true;
    }

    private void TryApplyCreationOriginConditionIfNeeded()
    {
        _creationOriginCondition = GetSelectedCreationConditionPreset();
        if (!_settingsOptionalConditionsEnabled)
        {
            return;
        }

        var mapped = TryMapCreationConditionToMajor(_creationOriginCondition);
        if (!mapped.HasValue)
        {
            return;
        }

        if (!TryApplyMajorCondition(mapped.Value, "Origin", out var message))
        {
            return;
        }

        PushCombatLog(message);
        ShowRewardMessage(message, requireAcknowledge: true, visibleSeconds: 10);
    }

    private void TryRollDungeonConditionFromEnemyHit(int damage)
    {
        if (!_settingsOptionalConditionsEnabled || _player == null || !_player.IsAlive)
        {
            return;
        }

        if (_dungeonConditionEventsTriggered >= 1)
        {
            return;
        }

        if (_activeMajorConditions.Any(condition => condition.Type == MajorConditionType.CrushedLimb))
        {
            return;
        }

        var heavyHitThreshold = Math.Max(6, (int)Math.Ceiling(_player.MaxHp * 0.18));
        var hpRatio = _player.CurrentHp / (double)Math.Max(1, _player.MaxHp);
        var qualifies = damage >= heavyHitThreshold || hpRatio <= 0.35;
        if (!qualifies)
        {
            return;
        }

        if (_rng.NextDouble() > 0.16)
        {
            return;
        }

        if (!TryApplyMajorCondition(MajorConditionType.CrushedLimb, "Dungeon", out var message))
        {
            return;
        }

        _dungeonConditionEventsTriggered += 1;
        PushCombatLog(message);
        ShowRewardMessage(message, requireAcknowledge: true, visibleSeconds: 10);
    }

    private bool TrySpendConditionPurgeCost(out string reason)
    {
        reason = string.Empty;
        var healthPotion = GetInventoryItem("health_potion");
        var manaDraught = GetInventoryItem("mana_draught");
        var sharpeningOil = GetInventoryItem("sharpening_oil");

        var hpQty = Math.Max(0, healthPotion?.Quantity ?? 0);
        var mpQty = Math.Max(0, manaDraught?.Quantity ?? 0);
        var oilQty = Math.Max(0, sharpeningOil?.Quantity ?? 0);

        if (hpQty < ConditionPurgeHealthPotionCost ||
            mpQty < ConditionPurgeManaDraughtCost ||
            oilQty < ConditionPurgeSharpeningOilCost)
        {
            reason = $"Need {GetConditionPurgeCostLabel()}.";
            return false;
        }

        healthPotion!.Quantity -= ConditionPurgeHealthPotionCost;
        manaDraught!.Quantity -= ConditionPurgeManaDraughtCost;
        sharpeningOil!.Quantity -= ConditionPurgeSharpeningOilCost;
        return true;
    }

    private void TryPurgeMajorCondition()
    {
        if (_activeMajorConditions.Count == 0)
        {
            _pauseMessage = "No active major condition to purge.";
            return;
        }

        if (!TrySpendConditionPurgeCost(out var reason))
        {
            _pauseMessage = $"High-tier purge failed: {reason}";
            return;
        }

        var cured = _activeMajorConditions[0];
        _activeMajorConditions.RemoveAt(0);
        _pauseMessage = $"High-tier purge completed. {GetMajorConditionLabel(cured.Type)} removed.";
        PushCombatLog(_pauseMessage);
    }

    private void InitializeRunInventory()
    {
        _inventoryItems.Clear();
        _inventoryItems.AddRange(new[]
        {
            new InventoryItem
            {
                Id = "health_potion",
                Name = "Health Potion",
                Description = "Restore 35% HP.",
                Kind = InventoryItemKind.Consumable,
                Slot = null,
                Quantity = 3
            },
            new InventoryItem
            {
                Id = "mana_draught",
                Name = "Mana Draught",
                Description = "Restore 35% MP.",
                Kind = InventoryItemKind.Consumable,
                Slot = null,
                Quantity = 2
            },
            new InventoryItem
            {
                Id = "sharpening_oil",
                Name = "Sharpening Oil",
                Description = "Consumable buff: +1 melee damage for this run.",
                Kind = InventoryItemKind.Consumable,
                Slot = null,
                Quantity = 1
            },
            new InventoryItem
            {
                Id = "leather_jerkin",
                Name = "Leather Jerkin",
                Description = "Armor slot (Light): +1 defense while equipped.",
                Kind = InventoryItemKind.Equipment,
                Slot = EquipmentSlot.Armor,
                Quantity = 1
            },
            new InventoryItem
            {
                Id = "brigandine_coat",
                Name = "Brigandine Coat",
                Description = "Armor slot (Medium): +2 defense while equipped.",
                Kind = InventoryItemKind.Equipment,
                Slot = EquipmentSlot.Armor,
                Quantity = 0
            },
            new InventoryItem
            {
                Id = "plate_harness",
                Name = "Plate Harness",
                Description = "Armor slot (Heavy): +3 defense while equipped.",
                Kind = InventoryItemKind.Equipment,
                Slot = EquipmentSlot.Armor,
                Quantity = 0
            },
            new InventoryItem
            {
                Id = "warding_charm",
                Name = "Warding Charm",
                Description = "Neck slot: +1 defense while equipped.",
                Kind = InventoryItemKind.Equipment,
                Slot = EquipmentSlot.Neck,
                Quantity = 1
            },
            new InventoryItem
            {
                Id = "hunter_cloak",
                Name = "Hunter Cloak",
                Description = "Cloak slot: +3% flee and +1% crit while equipped.",
                Kind = InventoryItemKind.Equipment,
                Slot = EquipmentSlot.Cloak,
                Quantity = 1
            },
            new InventoryItem
            {
                Id = "iron_helm",
                Name = "Iron Helm",
                Description = "Head slot: +1 defense while equipped.",
                Kind = InventoryItemKind.Equipment,
                Slot = EquipmentSlot.Head,
                Quantity = 1
            },
            new InventoryItem
            {
                Id = "focus_belt",
                Name = "Focus Belt",
                Description = "Belt slot: +1 spell damage while equipped.",
                Kind = InventoryItemKind.Equipment,
                Slot = EquipmentSlot.Belt,
                Quantity = 1
            },
            new InventoryItem
            {
                Id = "luck_ring",
                Name = "Luck Ring",
                Description = "Ring slot: +1% crit while equipped.",
                Kind = InventoryItemKind.Equipment,
                Slot = EquipmentSlot.Ring,
                Quantity = 1
            },
            new InventoryItem
            {
                Id = "guard_ring",
                Name = "Guard Ring",
                Description = "Ring slot: +1 defense while equipped.",
                Kind = InventoryItemKind.Equipment,
                Slot = EquipmentSlot.Ring,
                Quantity = 1
            }
        });
    }

    private InventoryItem? GetInventoryItem(string id)
    {
        return _inventoryItems.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal));
    }

    private static int GetEquipmentSlotCapacity(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Ring => 2,
            _ => 1
        };
    }

    private static int FindFirstFreeSlotIndex(HashSet<int> occupied, int capacity)
    {
        for (var i = 0; i < capacity; i++)
        {
            if (!occupied.Contains(i))
            {
                return i;
            }
        }

        return -1;
    }

    private static string GetEquipmentSlotLabel(EquipmentSlot slot, int? slotIndex = null)
    {
        return slot switch
        {
            EquipmentSlot.MainHand => "Main Hand",
            EquipmentSlot.OffHand => "Off Hand",
            EquipmentSlot.Armor => "Armor",
            EquipmentSlot.Head => "Head",
            EquipmentSlot.Goggles => "Goggles",
            EquipmentSlot.Neck => "Neck",
            EquipmentSlot.Cloak => "Cloak",
            EquipmentSlot.Shirt => "Shirt",
            EquipmentSlot.Bracers => "Bracers",
            EquipmentSlot.Gloves => "Gloves",
            EquipmentSlot.Belt => "Belt",
            EquipmentSlot.KneePads => "Knee Pads",
            EquipmentSlot.Boots => "Boots",
            EquipmentSlot.Ring => slotIndex.HasValue ? $"Ring {slotIndex.Value + 1}" : "Ring",
            _ => "Gear"
        };
    }

    private static bool TryGetArmorCategory(InventoryItem item, out ArmorCategory category)
    {
        category = ArmorCategory.Unarmored;
        if (item.Kind != InventoryItemKind.Equipment || item.Slot != EquipmentSlot.Armor)
        {
            return false;
        }

        return ArmorItemCategories.TryGetValue(item.Id, out category);
    }

    private static string GetArmorStateLabel(ArmorCategory category)
    {
        return ArmorTraining.GetCategoryLabel(category);
    }

    private InventoryItem? GetEquippedArmorItem()
    {
        return _inventoryItems.FirstOrDefault(item =>
            item.Kind == InventoryItemKind.Equipment &&
            item.IsEquipped &&
            item.Slot == EquipmentSlot.Armor &&
            item.Quantity > 0);
    }

    private ArmorCategory GetCurrentArmorCategory()
    {
        var armorItem = GetEquippedArmorItem();
        if (armorItem == null)
        {
            return ArmorCategory.Unarmored;
        }

        return TryGetArmorCategory(armorItem, out var armorCategory)
            ? armorCategory
            : ArmorCategory.Light;
    }

    private static int GetClassArmorTrainingRank(Player player)
    {
        return ArmorTraining.GetClassTrainingRank(player.CharacterClass.Name);
    }

    private static int GetFeatArmorTrainingRank(Player player)
    {
        return ArmorTraining.GetFeatTrainingRank(player.HasFeat);
    }

    private static int GetEffectiveArmorTrainingRank(Player player)
    {
        return ArmorTraining.GetEffectiveTrainingRank(player.CharacterClass.Name, player.HasFeat);
    }

    private bool CanEquipArmorCategory(Player player, ArmorCategory category, out string reason)
    {
        reason = string.Empty;
        if (ArmorTraining.HasTrainingForCategory(player.CharacterClass.Name, player.HasFeat, category))
        {
            return true;
        }

        var requiredCategory = ArmorTraining.GetCategoryLabel(category);
        reason = $"Requires {requiredCategory} armor training.";
        return false;
    }

    private int GetArmorStateDefenseBonus(Player player)
    {
        var current = GetCurrentArmorCategory();
        return current switch
        {
            ArmorCategory.Light when player.HasFeat("light_armor_training_feat") => 1,
            ArmorCategory.Medium when player.HasFeat("medium_armor_training_feat") => 2,
            ArmorCategory.Heavy when player.HasFeat("heavy_armor_training_feat") => 3,
            ArmorCategory.Unarmored when player.HasFeat("unarmored_defense_feat") => 2,
            _ => 0
        };
    }

    private int GetArmorStateFleeBonus(Player player)
    {
        var current = GetCurrentArmorCategory();
        return current switch
        {
            ArmorCategory.Light when player.HasFeat("light_armor_training_feat") => 2,
            ArmorCategory.Unarmored when player.HasFeat("unarmored_defense_feat") => 8,
            _ => 0
        };
    }

    private int GetPlayerCombatMoveBudget(Player player)
    {
        var armorCategory = GetCurrentArmorCategory();
        return EncounterMovementRules.GetEffectiveMoveBudget(player.Race, armorCategory);
    }

    private string GetArmorTrainingSummary(Player player)
    {
        var classRank = GetClassArmorTrainingRank(player);
        var featRank = GetFeatArmorTrainingRank(player);
        var effectiveRank = GetEffectiveArmorTrainingRank(player);
        return $"Class {ArmorTraining.GetRankLabel(classRank)} / Feat {ArmorTraining.GetRankLabel(featRank)} / Effective {ArmorTraining.GetRankLabel(effectiveRank)}";
    }

    private InventoryItem? GetEquippedItemInSlot(EquipmentSlot slot, int slotIndex)
    {
        return _inventoryItems.FirstOrDefault(item =>
            item.Kind == InventoryItemKind.Equipment &&
            item.IsEquipped &&
            item.Slot == slot &&
            item.EquippedSlotIndex.GetValueOrDefault() == slotIndex &&
            item.Quantity > 0);
    }

    private int? GetNextAvailableSlotIndex(EquipmentSlot slot)
    {
        var capacity = GetEquipmentSlotCapacity(slot);
        var occupied = _inventoryItems
            .Where(item => item.Kind == InventoryItemKind.Equipment &&
                           item.IsEquipped &&
                           item.Quantity > 0 &&
                           item.Slot == slot)
            .Select(item => item.EquippedSlotIndex.GetValueOrDefault())
            .ToHashSet();
        var freeIndex = FindFirstFreeSlotIndex(occupied, capacity);
        return freeIndex >= 0 ? freeIndex : null;
    }

    private void NormalizeEquippedEquipmentState(bool adjustRunBonuses)
    {
        var occupiedSlotIndices = new Dictionary<EquipmentSlot, HashSet<int>>();
        foreach (var item in _inventoryItems.Where(i => i.Kind == InventoryItemKind.Equipment))
        {
            if (!item.IsEquipped)
            {
                item.EquippedSlotIndex = null;
                continue;
            }

            var slot = item.Slot;
            var invalidate = item.Quantity <= 0 || !slot.HasValue;
            if (!invalidate)
            {
                var slotValue = slot.GetValueOrDefault();
                var capacity = GetEquipmentSlotCapacity(slotValue);
                if (!occupiedSlotIndices.TryGetValue(slotValue, out var occupiedForSlot))
                {
                    occupiedForSlot = new HashSet<int>();
                    occupiedSlotIndices[slotValue] = occupiedForSlot;
                }

                if (item.EquippedSlotIndex.HasValue)
                {
                    var desiredIndex = item.EquippedSlotIndex.Value;
                    invalidate = desiredIndex < 0 ||
                                 desiredIndex >= capacity ||
                                 !occupiedForSlot.Add(desiredIndex);
                }
                else
                {
                    var resolvedIndex = FindFirstFreeSlotIndex(occupiedForSlot, capacity);
                    if (resolvedIndex < 0)
                    {
                        invalidate = true;
                    }
                    else
                    {
                        occupiedForSlot.Add(resolvedIndex);
                        item.EquippedSlotIndex = resolvedIndex;
                    }
                }

                if (!invalidate &&
                    slotValue == EquipmentSlot.Armor &&
                    _player != null &&
                    TryGetArmorCategory(item, out var armorCategory) &&
                    !CanEquipArmorCategory(_player, armorCategory, out _))
                {
                    invalidate = true;
                }
            }

            if (!invalidate)
            {
                continue;
            }

            item.IsEquipped = false;
            item.EquippedSlotIndex = null;
            if (adjustRunBonuses)
            {
                ApplyInventoryEquipmentBonus(item, equip: false);
            }
        }
    }

    private int GetInventoryQuantityTotal()
    {
        return _inventoryItems.Sum(item => Math.Max(0, item.Quantity));
    }

    private void ResetPauseConfirm()
    {
        _pauseConfirmAction = PauseConfirmAction.None;
        _pauseConfirmTarget = -1;
    }

    private int GetCombatLogBufferSize()
    {
        return _settingsVerboseCombatLog ? 12 : 6;
    }

    private int GetCombatLogVisibleLines()
    {
        return _settingsVerboseCombatLog ? 8 : 5;
    }

    private const int KeyEnter = 257;
    private const int KeyEscape = 256;
    private const int KeyBackspace = 259;
    private const int KeyUp = 265;
    private const int KeyDown = 264;
    private const int KeyLeft = 263;
    private const int KeyRight = 262;
    private const int KeyW = 87;
    private const int KeyA = 65;
    private const int KeyS = 83;
    private const int KeyD = 68;
    private const int KeyE = 69;
    private const int KeyC = 67;
    private const int KeySpace = 32;
    private const int KeyF1 = 290;
    private const int Key1 = 49;
    private const int Key2 = 50;
    private const int Key3 = 51;
    private const int Key4 = 52;
    private const int Key5 = 53;
    private const int Key6 = 54;
    private const double MovementInitialRepeatDelaySeconds = 0.16;
    private const double MovementRepeatIntervalSeconds = 0.09;
    private const double LootPickupHoldSeconds = 0.75;
    private const double PickupStatusVisibleSeconds = 2.4;
    private const int MilestoneRewardInterval = 2;
    private const int MaxEffectiveDoctrineRank = 3;
    private const int BloodwakeBaseHeal = 3;
    private const int BloodwakeVanguardBonusHeal = 3;
    private const int AstralConduitBaseBurstDamage = 4;
    private const int AstralConduitArcanistBurstBonus = 3;
    private const int VeilstriderBaseFleeBonus = 12;
    private const int VeilstriderSkirmisherFleeBonus = 8;
    private const int ArcDoctrineBaseBonusDamage = 1;
    private const int ArcDoctrineArcanistBonusDamage = 1;
    private const int ExecutionDoctrineBaseHeal = 2;
    private const int ExecutionDoctrineHealPerRank = 2;
    private const int ExecutionDoctrineVanguardHealBonus = 2;
    private const int ExecutionDoctrineArcanistManaPerRank = 1;
    private const int GoblinPackJoinDistanceTiles = 2;
    private const int GoblinPackMaxEncounterSize = 3;
    private const int EncounterReinforcementJoinDistanceTiles = 2;
    private const int EncounterEnemyTurnCapPerPlayerAction = 24;
    private const int CombatMeleeRangeTiles = 1;
    private const int CombatRangedEnemyRangeTiles = 4;
    private const int EnemyDefaultMoveBudgetTiles = 1;
    private const int EnemySkirmisherMoveBudgetTiles = 2;
    private const int Phase3SanctumUnlockRequiredKills = 8;
    private const int Phase3SanctumUnlockRequiredRewardNodes = 2;
    private const string Phase3RouteForkNodeId = "phase3_route_fork";
    private const string Phase3RiskEventNodeId = "phase3_risk_event";
    private const int ConditionPurgeHealthPotionCost = 2;
    private const int ConditionPurgeManaDraughtCost = 2;
    private const int ConditionPurgeSharpeningOilCost = 1;

    private static readonly string[] RewardOptionNames =
    {
        "Recover Supplies",
        "Take Battle Trophy",
        "Forge Combat Edge"
    };

    private static readonly string[] ArchetypeOptionNames =
    {
        "Path of the Vanguard",
        "Path of the Arcanist",
        "Path of the Skirmisher"
    };

    private static readonly string[] RelicOptionNames =
    {
        "Bloodwake Emblem",
        "Astral Conduit",
        "Veilstrider Charm"
    };

    private static readonly string[] MilestoneOptionNames =
    {
        "Execution Doctrine",
        "Arc Doctrine",
        "Escape Doctrine"
    };

    private static readonly string[] Phase3RouteOptionNames =
    {
        "Upper Catacombs Route",
        "Lower Shrine Route"
    };

    private static readonly string[] Phase3RiskEventOptionNames =
    {
        "Take Blood Oath",
        "Stabilize The Cache"
    };

    private static readonly (int X, int Y)[] CardinalDirections =
    {
        (0, -1),
        (1, 0),
        (0, 1),
        (-1, 0)
    };

    private static readonly (int X, int Y)[] LootPlacementOffsets =
    {
        (0, 0),
        (1, 0),
        (-1, 0),
        (0, 1),
        (0, -1),
        (1, 1),
        (-1, 1),
        (1, -1),
        (-1, -1)
    };

    private static readonly (int X, int Y, string Key)[] Phase3EntryEnemyPack =
    {
        (6, 5, "goblin_grunt"),
        (7, 5, "goblin_grunt"),
        (12, 8, "goblin_grunt"),
        (13, 8, "goblin_skirmisher"),
        (12, 9, "goblin_grunt"),
        (20, 6, "goblin_skirmisher"),
        (21, 6, "goblin_grunt"),
        (24, 6, "goblin_slinger")
    };

    private static readonly (int X, int Y, string Key)[] Phase3UpperRouteEnemyPack =
    {
        (29, 9, "goblin_grunt"),
        (30, 9, "goblin_skirmisher"),
        (29, 10, "goblin_grunt"),
        (38, 6, "goblin_slinger"),
        (39, 6, "goblin_grunt"),
        (43, 8, "goblin_skirmisher"),
        (44, 8, "goblin_grunt"),
        (50, 10, "goblin_supervisor")
    };

    private static readonly (int X, int Y, string Key)[] Phase3LowerRouteEnemyPack =
    {
        (10, 20, "goblin_grunt"),
        (11, 20, "goblin_skirmisher"),
        (10, 21, "goblin_grunt"),
        (16, 24, "goblin_slinger"),
        (17, 24, "goblin_grunt"),
        (27, 20, "goblin_skirmisher"),
        (28, 20, "goblin_grunt"),
        (32, 24, "goblin_supervisor"),
        (33, 24, "goblin_grunt"),
        (36, 18, "goblin_slinger")
    };

    private static readonly (int X, int Y, string Key)[] Phase3SanctumEnemyPack =
    {
        (47, 22, "goblin_supervisor"),
        (48, 22, "goblin_skirmisher"),
        (47, 23, "goblin_grunt"),
        (53, 24, "goblin_slinger"),
        (54, 24, "goblin_supervisor"),
        (55, 30, "goblin_general")
    };

    private static readonly (int X, int Y, string Key)[] Phase3UpperSanctumReinforcementPack =
    {
        (49, 26, "goblin_supervisor"),
        (50, 26, "goblin_grunt"),
        (53, 28, "goblin_slinger")
    };

    private static readonly LootTemplate[] LowLevelLootTable =
    {
        new()
        {
            Name = "Stolen Bandage Roll",
            Rarity = LootRarity.Common,
            InventoryItemId = "health_potion",
            MinItemQuantity = 1,
            MaxItemQuantity = 1
        },
        new()
        {
            Name = "Goblin Field Rations",
            Rarity = LootRarity.Common,
            InventoryItemId = "health_potion",
            MinItemQuantity = 1,
            MaxItemQuantity = 2
        },
        new()
        {
            Name = "Dusty Mana Vial",
            Rarity = LootRarity.Common,
            InventoryItemId = "mana_draught",
            MinItemQuantity = 1,
            MaxItemQuantity = 1
        },
        new()
        {
            Name = "Pitch Flask",
            Rarity = LootRarity.Common,
            InventoryItemId = "sharpening_oil",
            MinItemQuantity = 1,
            MaxItemQuantity = 1
        },
        new()
        {
            Name = "Pilfered Iron Helm",
            Rarity = LootRarity.Uncommon,
            InventoryItemId = "iron_helm",
            MinItemQuantity = 1,
            MaxItemQuantity = 1
        },
        new()
        {
            Name = "Scuffed Leather Jerkin",
            Rarity = LootRarity.Uncommon,
            InventoryItemId = "leather_jerkin",
            MinItemQuantity = 1,
            MaxItemQuantity = 1
        },
        new()
        {
            Name = "Stolen Focus Belt",
            Rarity = LootRarity.Uncommon,
            InventoryItemId = "focus_belt",
            MinItemQuantity = 1,
            MaxItemQuantity = 1
        },
        new()
        {
            Name = "Brigandine Coat",
            Rarity = LootRarity.Rare,
            InventoryItemId = "brigandine_coat",
            MinItemQuantity = 1,
            MaxItemQuantity = 1
        },
        new()
        {
            Name = "Warding Charm",
            Rarity = LootRarity.Rare,
            InventoryItemId = "warding_charm",
            MinItemQuantity = 1,
            MaxItemQuantity = 1
        },
        new()
        {
            Name = "Hunter Cloak",
            Rarity = LootRarity.Rare,
            InventoryItemId = "hunter_cloak",
            MinItemQuantity = 1,
            MaxItemQuantity = 1
        },
        new()
        {
            Name = "Luck Ring",
            Rarity = LootRarity.Rare,
            InventoryItemId = "luck_ring",
            MinItemQuantity = 1,
            MaxItemQuantity = 1
        },
        new()
        {
            Name = "Guard Ring",
            Rarity = LootRarity.Rare,
            InventoryItemId = "guard_ring",
            MinItemQuantity = 1,
            MaxItemQuantity = 1
        }
    };

    private static readonly IReadOnlyDictionary<string, ArmorCategory> ArmorItemCategories =
        new Dictionary<string, ArmorCategory>(StringComparer.Ordinal)
        {
            ["leather_jerkin"] = ArmorCategory.Light,
            ["brigandine_coat"] = ArmorCategory.Medium,
            ["plate_harness"] = ArmorCategory.Heavy
        };

    private static Color ColWhite = new(255, 255, 255, 255);
    private static Color ColGray = new(154, 162, 176, 255);
    private static Color ColLightGray = new(220, 226, 236, 255);
    private static Color ColYellow = new(246, 206, 114, 255);
    private static Color ColGreen = new(110, 220, 110, 255);
    private static Color ColSkyBlue = new(132, 199, 255, 255);
    private static Color ColDarkGreen = new(40, 130, 40, 255);
    private static Color ColRed = new(220, 80, 80, 255);
    private static Color ColInk = new(9, 12, 20, 255);
    private static Color ColPanel = new(20, 26, 38, 245);
    private static Color ColPanelAlt = new(26, 34, 48, 245);
    private static Color ColPanelSoft = new(18, 22, 32, 220);
    private static Color ColBorder = new(88, 112, 148, 255);
    private static Color ColAccentRose = new(232, 104, 124, 255);
    private static Color ColSelectBg = new(74, 102, 146, 210);
    private static Color ColSelectBgSoft = new(56, 80, 118, 190);
    private static Color ColFooter = new(12, 16, 26, 230);

    private void ApplyAccessibilityPalette()
    {
        if (_settingsAccessibilityColorProfile == AccessibilityColorProfile.DeuteranopiaFriendly)
        {
            ApplyDeuteranopiaFriendlyPalette();
        }
        else
        {
            ApplyDefaultPalette();
        }

        if (_settingsAccessibilityHighContrast)
        {
            ApplyHighContrastOverrides();
        }
    }

    private static void ApplyDefaultPalette()
    {
        ColWhite = new Color(255, 255, 255, 255);
        ColGray = new Color(154, 162, 176, 255);
        ColLightGray = new Color(220, 226, 236, 255);
        ColYellow = new Color(246, 206, 114, 255);
        ColGreen = new Color(110, 220, 110, 255);
        ColSkyBlue = new Color(132, 199, 255, 255);
        ColDarkGreen = new Color(40, 130, 40, 255);
        ColRed = new Color(220, 80, 80, 255);
        ColInk = new Color(9, 12, 20, 255);
        ColPanel = new Color(20, 26, 38, 245);
        ColPanelAlt = new Color(26, 34, 48, 245);
        ColPanelSoft = new Color(18, 22, 32, 220);
        ColBorder = new Color(88, 112, 148, 255);
        ColAccentRose = new Color(232, 104, 124, 255);
        ColSelectBg = new Color(74, 102, 146, 210);
        ColSelectBgSoft = new Color(56, 80, 118, 190);
        ColFooter = new Color(12, 16, 26, 230);
    }

    private static void ApplyDeuteranopiaFriendlyPalette()
    {
        ColWhite = new Color(255, 255, 255, 255);
        ColGray = new Color(164, 172, 186, 255);
        ColLightGray = new Color(232, 236, 244, 255);
        ColYellow = new Color(255, 214, 132, 255);
        ColGreen = new Color(88, 194, 236, 255);
        ColSkyBlue = new Color(146, 218, 255, 255);
        ColDarkGreen = new Color(38, 134, 164, 255);
        ColRed = new Color(238, 156, 98, 255);
        ColInk = new Color(8, 12, 20, 255);
        ColPanel = new Color(22, 30, 44, 245);
        ColPanelAlt = new Color(30, 40, 56, 245);
        ColPanelSoft = new Color(19, 25, 36, 220);
        ColBorder = new Color(102, 138, 176, 255);
        ColAccentRose = new Color(248, 170, 108, 255);
        ColSelectBg = new Color(64, 110, 158, 220);
        ColSelectBgSoft = new Color(52, 90, 130, 200);
        ColFooter = new Color(12, 18, 28, 235);
    }

    private static void ApplyHighContrastOverrides()
    {
        ColWhite = new Color(248, 248, 248, 255);
        ColGray = new Color(188, 196, 212, 255);
        ColLightGray = new Color(238, 242, 250, 255);
        ColYellow = new Color(255, 236, 140, 255);
        ColGreen = new Color(168, 255, 176, 255);
        ColSkyBlue = new Color(156, 226, 255, 255);
        ColDarkGreen = new Color(70, 180, 92, 255);
        ColRed = new Color(255, 144, 144, 255);
        ColInk = new Color(0, 0, 0, 255);
        ColPanel = new Color(8, 10, 14, 252);
        ColPanelAlt = new Color(13, 16, 22, 252);
        ColPanelSoft = new Color(10, 12, 18, 238);
        ColBorder = new Color(238, 242, 250, 255);
        ColAccentRose = new Color(255, 170, 186, 255);
        ColSelectBg = new Color(96, 142, 208, 236);
        ColSelectBgSoft = new Color(72, 108, 160, 218);
        ColFooter = new Color(4, 6, 10, 240);
    }

    private static class UiLayout
    {
        public const int MinTopMargin = 20;

        public const int PausePanelInsetX = 118;
        public const int PausePanelInsetY = 34;
        public const int StartPanelWidth = 444;
        public const int StartPanelHeight = 320;
        public const int HelpPanelWidth = 508;
        public const int HelpPanelHeight = 388;

        public const int HudHeight = 72;
        public const int HudPadding = 10;
        public const int RewardBannerY = 74;
        public const int RewardBannerHeight = 36;

        public const int CombatOverlayInset = 44;
        public const int CombatHeaderInsetX = 64;
        public const int CombatHeaderY = 62;
        public const int CombatHeaderHeight = 64;
        public const int CombatContentY = 138;
        public const int CombatContentHeight = 260;
        public const int CombatColumnsGap = 16;
        public const int CombatFooterInset = 62;

        public const int LevelPanelInsetX = 92;
        public const int LevelPanelInsetY = 56;
        public const int LevelFooterX = 102;
        public const int LevelFooterInset = 204;
        public const int SelectionPanelInsetX = 70;
        public const int SelectionPanelInsetY = 44;
        public const int SpellSelectionPanelInsetY = 42;
        public const int SelectionRowX = 96;
        public const int SelectionRowInset = 192;
        public const int SelectionFooterX = 84;
        public const int SelectionFooterInset = 168;
        public const int RewardPanelInsetX = 86;
        public const int RewardPanelInsetY = 54;
        public const int RewardOptionX = 120;
        public const int RewardOptionInset = 240;
        public const int RewardFooterX = 110;
        public const int RewardFooterInset = 220;
        public const int VictoryPanelInsetX = 108;
        public const int VictoryPanelInsetY = 96;
        public const int DeathPanelInsetX = 108;
        public const int DeathPanelInsetY = 116;
        public const int DeathFooterX = 128;
        public const int DeathFooterInset = 256;
    }

    public void Update()
    {
        ProcessTimers();
        UpdateRewardMessageTimer();
        EnsurePlayerStateConsistency();
        HandleInput();
        UpdateWorldSimulation();
        UpdateRewardMessageTimer();
        EnsurePlayerStateConsistency();
    }

    public void Draw()
    {
        Raylib.ClearBackground(VisualTheme.ScreenBaseColor);

        switch (_gameState)
        {
            case GameState.StartMenu:
                DrawStartMenu();
                break;
            case GameState.HelpMenu:
                DrawHelpMenu();
                break;
            case GameState.CharacterCreationHub:
                DrawCharacterCreationHub();
                break;
            case GameState.CharacterName:
                DrawNameInput();
                break;
            case GameState.CharacterGender:
                DrawGenderSelection();
                break;
            case GameState.CharacterClass:
                DrawClassSelection();
                break;
            case GameState.CharacterStatAllocation:
                DrawCharacterStatAllocation();
                break;
            case GameState.Playing:
                DrawWorld();
                break;
            case GameState.Combat:
                DrawWorld();
                DrawCombatUi();
                break;
            case GameState.CombatSkillMenu:
                DrawWorld();
                DrawCombatUi();
                DrawCombatSkillMenu();
                break;
            case GameState.CombatSpellMenu:
                DrawWorld();
                DrawCombatUi();
                DrawCombatSpellMenu();
                break;
            case GameState.CombatSpellTargeting:
                DrawWorld();
                DrawCombatUi();
                DrawCombatSpellTargeting();
                break;
            case GameState.CombatItemMenu:
                DrawWorld();
                DrawCombatUi();
                DrawCombatItemMenu();
                break;
            case GameState.CharacterMenu:
                DrawWorld();
                DrawCharacterSheet();
                break;
            case GameState.LevelUp:
                DrawWorld();
                DrawLevelUpMenu();
                break;
            case GameState.FeatSelection:
                DrawWorld();
                DrawFeatSelection();
                break;
            case GameState.SpellSelection:
                DrawWorld();
                DrawSpellSelection();
                break;
            case GameState.SkillSelection:
                DrawWorld();
                DrawSkillSelection();
                break;
            case GameState.RewardChoice:
                DrawWorld();
                DrawRewardChoice();
                break;
            case GameState.PauseMenu:
                DrawPausedScene();
                DrawPauseMenu();
                break;
            case GameState.VictoryScreen:
                DrawVictoryScreen();
                break;
            case GameState.DeathScreen:
                DrawDeathScreen();
                break;
        }
    }

    public string GetDiagnosticsSummary()
    {
        var playerInfo = _player == null
            ? "Player=<null>"
            : $"Player={_player.Name} Lv{_player.Level} HP={_player.CurrentHp}/{_player.MaxHp} MP={_player.CurrentMana}/{_player.MaxMana}";
        var enemyInfo = _currentEnemy == null
            ? "CurrentEnemy=<null>"
            : $"CurrentEnemy={_currentEnemy.Type.Name} HP={_currentEnemy.CurrentHp}/{_currentEnemy.Type.MaxHp}";
        return $"State={_gameState} PausedFrom={_pausedFromState} Archetype={GetRunArchetypeLabel(_runArchetype)} Relic={GetRunRelicLabel(_runRelic)} Route={GetPhase3RouteLabel(_phase3RouteChoice)} Zone={GetFloorZoneLabel(_currentFloorZone)} P3Kills={_phase3EnemiesDefeated}/{Phase3SanctumUnlockRequiredKills} P3Rewards={GetClaimedPrimaryRewardCount()}/{Phase3SanctumUnlockRequiredRewardNodes} Milestones={GetMilestoneRanksLabel()} CondMode={(_settingsOptionalConditionsEnabled ? "On" : "Off")} Cond={GetActiveMajorConditionSummary()} EnemiesAlive={_enemies.Count(e => e.IsAlive)} EncActive={_encounterActive} EncSize={_encounterEnemies.Count} EncRound={_encounterRound} EncRem={_packEnemiesRemainingAfterCurrent} EncTurn={_encounterTurnIndex}/{Math.Max(0, _encounterTurnOrder.Count - 1)} EncCurrent={_encounterCurrentCombatantId} MoveMode={_combatMoveModeActive} MovePts={_combatMovePointsRemaining}/{_combatMovePointsMax} SpritesReady={_spriteLibrary.IsReady} PlayerSprite={_selectedPlayerSpriteId} {playerInfo} {enemyInfo}";
    }

    private static string GetRunArchetypeLabel(RunArchetype archetype)
    {
        return archetype switch
        {
            RunArchetype.Vanguard => "Vanguard",
            RunArchetype.Arcanist => "Arcanist",
            RunArchetype.Skirmisher => "Skirmisher",
            _ => "Unchosen"
        };
    }

    private static string GetRunRelicLabel(RunRelic relic)
    {
        return relic switch
        {
            RunRelic.BloodwakeEmblem => "Bloodwake Emblem",
            RunRelic.AstralConduit => "Astral Conduit",
            RunRelic.VeilstriderCharm => "Veilstrider Charm",
            _ => "Unchosen"
        };
    }

    private static string GetPhase3RouteLabel(Phase3RouteChoice routeChoice)
    {
        return routeChoice switch
        {
            Phase3RouteChoice.UpperCatacombs => "Upper Catacombs",
            Phase3RouteChoice.LowerShrine => "Lower Shrine",
            _ => "Unchosen"
        };
    }

    private static string GetFloorZoneLabel(FloorMacroZone zone)
    {
        return zone switch
        {
            FloorMacroZone.BranchingDepths => "Branching Depths",
            FloorMacroZone.SanctumRing => "Sanctum Ring",
            _ => "Entry Frontier"
        };
    }

    private static FloorMacroZone ResolveFloorMacroZone(int tileX, int tileY)
    {
        if (tileX >= 42 && tileX < 58 && tileY >= 18 && tileY < 33)
        {
            return FloorMacroZone.SanctumRing;
        }

        if (tileX >= 24 || tileY >= 15)
        {
            return FloorMacroZone.BranchingDepths;
        }

        return FloorMacroZone.EntryFrontier;
    }

    private bool IsStandardRewardNodeActive()
    {
        return _activeRewardNode != null &&
               _rewardNodes.Any(node => string.Equals(node.Id, _activeRewardNode.Id, StringComparison.Ordinal));
    }

    private bool IsPhase3RouteChoiceActive()
    {
        return _activeRewardNode != null &&
               string.Equals(_activeRewardNode.Id, Phase3RouteForkNodeId, StringComparison.Ordinal);
    }

    private bool IsPhase3RiskEventActive()
    {
        return _activeRewardNode != null &&
               string.Equals(_activeRewardNode.Id, Phase3RiskEventNodeId, StringComparison.Ordinal);
    }

    private int GetClaimedPrimaryRewardCount()
    {
        return _rewardNodes.Count(node => _claimedRewardNodeIds.Contains(node.Id));
    }

    private int GetPhase3SanctumKillsRemaining()
    {
        return Math.Max(0, Phase3SanctumUnlockRequiredKills - _phase3EnemiesDefeated);
    }

    private int GetPhase3SanctumRewardsRemaining()
    {
        return Math.Max(0, Phase3SanctumUnlockRequiredRewardNodes - GetClaimedPrimaryRewardCount());
    }

    private bool IsPhase3SanctumUnlockReady()
    {
        if (_phase3RouteChoice == Phase3RouteChoice.None || !_phase3RiskEventResolved)
        {
            return false;
        }

        return GetPhase3SanctumKillsRemaining() == 0 &&
               GetPhase3SanctumRewardsRemaining() == 0;
    }

    private string GetPhase3ObjectiveLabel()
    {
        if (_phase3RouteChoice == Phase3RouteChoice.None)
        {
            return "Objective: reach the central fork (x30-x31, y12-y16) and choose your route.";
        }

        if (!_phase3RiskEventResolved)
        {
            return "Objective: resolve the Oath Crucible in your selected route.";
        }

        if (!IsPhase3SanctumUnlockReady())
        {
            var killsDone = Math.Min(Phase3SanctumUnlockRequiredKills, _phase3EnemiesDefeated);
            var rewardsDone = Math.Min(Phase3SanctumUnlockRequiredRewardNodes, GetClaimedPrimaryRewardCount());
            return $"Objective: prepare sanctum gate - kills {killsDone}/{Phase3SanctumUnlockRequiredKills}, reward nodes {rewardsDone}/{Phase3SanctumUnlockRequiredRewardNodes}.";
        }

        if (!_phase3PreSanctumRewardGranted)
        {
            return "Objective: route boon ready. Move to claim it, then enter Sanctum Ring.";
        }

        if (!_phase3SanctumWaveSpawned)
        {
            return "Objective: enter Sanctum Ring to trigger the final defense.";
        }

        if (!_bossDefeated)
        {
            return "Objective: defeat the Goblin General.";
        }

        return "Objective: clear remaining hostiles.";
    }

    private bool IsRouteTargetEventZoneReached()
    {
        if (_player == null || _phase3RouteChoice == Phase3RouteChoice.None)
        {
            return false;
        }

        return _phase3RouteChoice switch
        {
            Phase3RouteChoice.UpperCatacombs => _player.X >= 34 && _player.X < 56 && _player.Y >= 2 && _player.Y < 14,
            Phase3RouteChoice.LowerShrine => _player.X >= 24 && _player.X < 40 && _player.Y >= 16 && _player.Y < 28,
            _ => false
        };
    }

    private static bool IsInsideRect(int x, int y, int rectX, int rectY, int rectW, int rectH)
    {
        return x >= rectX && x < rectX + rectW &&
               y >= rectY && y < rectY + rectH;
    }

    private bool IsPhase3RouteForkTile(int x, int y)
    {
        return IsInsideRect(x, y, rectX: 30, rectY: 12, rectW: 2, rectH: 5);
    }

    private bool IsPhase3SealedCorridorTile(int x, int y)
    {
        return _phase3RouteChoice switch
        {
            Phase3RouteChoice.UpperCatacombs =>
                IsInsideRect(x, y, rectX: 20, rectY: 20, rectW: 4, rectH: 2) ||
                IsInsideRect(x, y, rectX: 39, rectY: 21, rectW: 4, rectH: 2),
            Phase3RouteChoice.LowerShrine =>
                IsInsideRect(x, y, rectX: 31, rectY: 7, rectW: 4, rectH: 2) ||
                IsInsideRect(x, y, rectX: 46, rectY: 13, rectW: 2, rectH: 6),
            _ => false
        };
    }

    private bool IsWallOrSealed(int x, int y)
    {
        if (_map.IsWall(x, y))
        {
            return true;
        }

        if (_phase3RouteChoice == Phase3RouteChoice.None)
        {
            return false;
        }

        return IsPhase3SealedCorridorTile(x, y);
    }

    private string GetMilestoneRanksLabel()
    {
        return $"Exec {GetEffectiveExecutionRank()}/{MaxEffectiveDoctrineRank}  Arc {GetEffectiveArcRank()}/{MaxEffectiveDoctrineRank}  Esc {GetEffectiveEscapeRank()}/{MaxEffectiveDoctrineRank}";
    }

    private string GetRunIdentityLabel()
    {
        return $"{GetRunArchetypeLabel(_runArchetype)} | {GetRunRelicLabel(_runRelic)} | Route {GetPhase3RouteLabel(_phase3RouteChoice)} | {GetMilestoneRanksLabel()}";
    }

    private int GetEffectiveExecutionRank()
    {
        return Math.Clamp(_milestoneExecutionRank, 0, MaxEffectiveDoctrineRank);
    }

    private int GetEffectiveArcRank()
    {
        return Math.Clamp(_milestoneArcRank, 0, MaxEffectiveDoctrineRank);
    }

    private int GetEffectiveEscapeRank()
    {
        return Math.Clamp(_milestoneEscapeRank, 0, MaxEffectiveDoctrineRank);
    }

    private bool IsArchetypeChoiceActive()
    {
        return IsStandardRewardNodeActive() &&
               _runArchetype == RunArchetype.None &&
               GetClaimedPrimaryRewardCount() == 0;
    }

    private bool IsRelicCheckpointActive()
    {
        return IsStandardRewardNodeActive() &&
               _runArchetype != RunArchetype.None &&
               _runRelic == RunRelic.None &&
               GetClaimedPrimaryRewardCount() >= 1;
    }

    private int GetMilestoneCheckpointsEarned()
    {
        return GetClaimedPrimaryRewardCount() / MilestoneRewardInterval;
    }

    private bool IsMilestoneCheckpointActive()
    {
        return IsStandardRewardNodeActive() &&
               _runArchetype != RunArchetype.None &&
               _runRelic != RunRelic.None &&
               GetMilestoneCheckpointsEarned() > _milestoneChoicesTaken;
    }

    private string[] GetActiveRewardOptionNames()
    {
        if (IsPhase3RouteChoiceActive())
        {
            return Phase3RouteOptionNames;
        }

        if (IsPhase3RiskEventActive())
        {
            return Phase3RiskEventOptionNames;
        }

        if (IsArchetypeChoiceActive())
        {
            return ArchetypeOptionNames;
        }

        if (IsRelicCheckpointActive())
        {
            return RelicOptionNames;
        }

        if (IsMilestoneCheckpointActive())
        {
            return MilestoneOptionNames;
        }

        return RewardOptionNames;
    }

    private string[] GetActiveRewardOptionDescriptions()
    {
        if (_player == null)
        {
            return new[]
            {
                "Recover 35% HP and 35% MP.",
                "Gain XP and find usable supplies.",
                "Gain run bonuses."
            };
        }

        if (IsPhase3RouteChoiceActive())
        {
            return new[]
            {
                "Upper route: +20% XP from kills this run, but enemies strike harder (+1 attack).",
                "Lower route: fortify now (+1 defense, +6% flee, +1 Health Potion), but kill XP is reduced by 10%."
            };
        }

        if (IsPhase3RiskEventActive())
        {
            return new[]
            {
                "Blood Oath: lose 25% current HP (cannot kill you), gain +2 melee, +2 spell, +4% crit.",
                "Stabilize cache: gain two supply bundles and recover HP/MP, but lose 2% crit and 4% flee."
            };
        }

        if (IsArchetypeChoiceActive())
        {
            return new[]
            {
                "Frontline path: +1 melee and +2 defense.",
                "Spell path: +2 spell damage and +1 Mana Draught.",
                "Mobility path: +3% crit, +5% flee, +1 Sharpening Oil."
            };
        }

        if (IsRelicCheckpointActive())
        {
            return new[]
            {
                "Milestone relic: first melee hit each combat restores HP (extra with Vanguard).",
                "Milestone relic: first spell each combat gains burst damage (extra with Arcanist).",
                "Milestone relic: first flee attempt each combat gains major chance boost (extra with Skirmisher)."
            };
        }

        if (IsMilestoneCheckpointActive())
        {
            var executionRank = GetEffectiveExecutionRank();
            var arcRank = GetEffectiveArcRank();
            var escapeRank = GetEffectiveEscapeRank();
            var executionAtCap = executionRank >= MaxEffectiveDoctrineRank;
            var arcAtCap = arcRank >= MaxEffectiveDoctrineRank;
            var escapeAtCap = escapeRank >= MaxEffectiveDoctrineRank;
            var executionNextRank = executionAtCap ? executionRank : executionRank + 1;
            var arcNextRank = arcAtCap ? arcRank : arcRank + 1;
            var escapeNextRank = escapeAtCap ? escapeRank : escapeRank + 1;
            var executionPreview = ExecutionDoctrineBaseHeal + executionNextRank * ExecutionDoctrineHealPerRank + (_runArchetype == RunArchetype.Vanguard ? ExecutionDoctrineVanguardHealBonus : 0);
            var arcChargesPreview = arcNextRank;
            var arcDamagePreview = ArcDoctrineBaseBonusDamage + arcNextRank + (_runArchetype == RunArchetype.Arcanist ? ArcDoctrineArcanistBonusDamage : 0);
            var escapeChargesPreview = escapeNextRank;
            return new[]
            {
                executionAtCap
                    ? $"Execution doctrine mastered ({executionRank}/{MaxEffectiveDoctrineRank}): further picks convert to supplies."
                    : $"Execution doctrine rank {executionNextRank}/{MaxEffectiveDoctrineRank}: enemy kill recovers about {executionPreview} HP.",
                arcAtCap
                    ? $"Arc doctrine mastered ({arcRank}/{MaxEffectiveDoctrineRank}): further picks convert to supplies."
                    : $"Arc doctrine rank {arcNextRank}/{MaxEffectiveDoctrineRank}: up to {arcChargesPreview} slot waives/combat and about +{arcDamagePreview} damage on waived casts.",
                escapeAtCap
                    ? $"Escape doctrine mastered ({escapeRank}/{MaxEffectiveDoctrineRank}): further picks convert to supplies."
                    : $"Escape doctrine rank {escapeNextRank}/{MaxEffectiveDoctrineRank}: up to {escapeChargesPreview} failed-flee retaliation cancels each combat."
            };
        }

        var combatEdgeDescription = _runArchetype switch
        {
            RunArchetype.Vanguard => "Vanguard edge: +1 melee and +1 defense.",
            RunArchetype.Arcanist => "Arcanist edge: +1 spell damage and +1 mana refill burst.",
            RunArchetype.Skirmisher => "Skirmisher edge: +1% crit, +3% flee, +1 melee.",
            _ => "Alternate buff: (+1 melee/+1 spell) or (+1 defense/+2% crit/+3% flee)."
        };
        return new[]
        {
            "Recover 35% HP and 35% MP.",
            $"Gain {120 + _player.Level * 20} XP and find usable supplies.",
            combatEdgeDescription
        };
    }

    private void ApplyPhase3RouteChoice(int optionIndex, out string resultMessage)
    {
        switch (optionIndex)
        {
            case 0:
                _phase3RouteChoice = Phase3RouteChoice.UpperCatacombs;
                _phase3XpPercentMod += 20;
                _phase3EnemyAttackBonus += 1;
                {
                    var spawned = TrySpawnPhase3RouteWaveForChoice();
                    resultMessage = $"Route locked: Upper Catacombs. Enemy pressure rises (+1 attack), kill XP is boosted by 20%, and {spawned} route defenders mobilize.";
                }
                return;
            case 1:
                _phase3RouteChoice = Phase3RouteChoice.LowerShrine;
                _phase3XpPercentMod -= 10;
                _runDefenseBonus += 1;
                _runFleeBonus += 6;
                AddInventoryItemQuantity("health_potion", 1);
                {
                    var spawned = TrySpawnPhase3RouteWaveForChoice();
                    resultMessage = $"Route locked: Lower Shrine. You fortify (+1 defense, +6% flee, +1 Health Potion), kill XP drops by 10%, and {spawned} route defenders mobilize.";
                }
                return;
            default:
                _phase3RouteChoice = Phase3RouteChoice.UpperCatacombs;
                _phase3XpPercentMod += 20;
                _phase3EnemyAttackBonus += 1;
                {
                    var spawned = TrySpawnPhase3RouteWaveForChoice();
                    resultMessage = $"Route defaulted to Upper Catacombs with {spawned} route defenders mobilized.";
                }
                return;
        }
    }

    private void ApplyPhase3RiskEventChoice(int optionIndex, out string resultMessage)
    {
        if (_player == null)
        {
            _phase3RiskEventResolved = true;
            resultMessage = "The event has faded.";
            return;
        }

        _phase3RiskEventResolved = true;
        switch (optionIndex)
        {
            case 0:
            {
                var hpCost = Math.Max(4, (int)Math.Ceiling(_player.CurrentHp * 0.25));
                hpCost = Math.Min(hpCost, Math.Max(0, _player.CurrentHp - 1));
                _player.CurrentHp = Math.Max(1, _player.CurrentHp - hpCost);
                _runMeleeBonus += 2;
                _runSpellBonus += 2;
                _runCritBonus += 4;
                resultMessage = $"Blood Oath accepted: HP -{hpCost}, melee +2, spell +2, crit +4%.";
                return;
            }
            case 1:
            {
                var firstBundle = GrantRewardSupplyLoot();
                var secondBundle = GrantRewardSupplyLoot();
                var hpGain = Math.Max(6, (int)Math.Ceiling(_player.MaxHp * 0.20));
                var mpGain = Math.Max(4, (int)Math.Ceiling(_player.MaxMana * 0.20));
                var beforeHp = _player.CurrentHp;
                var beforeMp = _player.CurrentMana;
                _player.CurrentHp = Math.Min(_player.MaxHp, _player.CurrentHp + hpGain);
                _player.CurrentMana = Math.Min(_player.MaxMana, _player.CurrentMana + mpGain);
                _runCritBonus = Math.Max(-20, _runCritBonus - 2);
                _runFleeBonus = Math.Max(-25, _runFleeBonus - 4);
                resultMessage =
                    $"Cache stabilized: {firstBundle}, {secondBundle}, HP +{_player.CurrentHp - beforeHp}, MP +{_player.CurrentMana - beforeMp}, but crit -2% and flee -4%.";
                return;
            }
            default:
                resultMessage = "The event passes without effect.";
                return;
        }
    }

    private void ApplyMilestoneChoice(int optionIndex, out string resultMessage)
    {
        _milestoneChoicesTaken += 1;
        switch (optionIndex)
        {
            case 0:
                if (TryAdvanceDoctrineRank(ref _milestoneExecutionRank))
                {
                    resultMessage = $"Checkpoint secured: Execution Doctrine rank {GetEffectiveExecutionRank()}/{MaxEffectiveDoctrineRank}.";
                }
                else
                {
                    resultMessage = $"Execution Doctrine is mastered. {GrantMilestoneOverflowSupply()}";
                }
                return;
            case 1:
                if (TryAdvanceDoctrineRank(ref _milestoneArcRank))
                {
                    resultMessage = $"Checkpoint secured: Arc Doctrine rank {GetEffectiveArcRank()}/{MaxEffectiveDoctrineRank}.";
                }
                else
                {
                    resultMessage = $"Arc Doctrine is mastered. {GrantMilestoneOverflowSupply()}";
                }
                return;
            case 2:
                if (TryAdvanceDoctrineRank(ref _milestoneEscapeRank))
                {
                    resultMessage = $"Checkpoint secured: Escape Doctrine rank {GetEffectiveEscapeRank()}/{MaxEffectiveDoctrineRank}.";
                }
                else
                {
                    resultMessage = $"Escape Doctrine is mastered. {GrantMilestoneOverflowSupply()}";
                }
                return;
            default:
                if (TryAdvanceDoctrineRank(ref _milestoneExecutionRank))
                {
                    resultMessage = $"Checkpoint defaulted to Execution Doctrine rank {GetEffectiveExecutionRank()}/{MaxEffectiveDoctrineRank}.";
                }
                else
                {
                    resultMessage = $"Execution Doctrine is already mastered. {GrantMilestoneOverflowSupply()}";
                }
                return;
        }
    }

    private static bool TryAdvanceDoctrineRank(ref int rank)
    {
        if (rank >= MaxEffectiveDoctrineRank)
        {
            rank = MaxEffectiveDoctrineRank;
            return false;
        }

        rank += 1;
        return true;
    }

    private string GrantMilestoneOverflowSupply()
    {
        if (_player == null)
        {
            return "Overflow stockpile granted.";
        }

        var lootGain = GrantRewardSupplyLoot();
        var hpRestore = Math.Max(6, (int)Math.Ceiling(_player.MaxHp * 0.20));
        var beforeHp = _player.CurrentHp;
        _player.CurrentHp = Math.Min(_player.MaxHp, _player.CurrentHp + hpRestore);
        var gainedHp = _player.CurrentHp - beforeHp;
        return gainedHp > 0
            ? $"Overflow cache recovered ({lootGain}, HP +{gainedHp})."
            : $"Overflow cache recovered ({lootGain}).";
    }

    private void ApplyRelicChoice(int optionIndex, out string resultMessage)
    {
        switch (optionIndex)
        {
            case 0:
                _runRelic = RunRelic.BloodwakeEmblem;
                resultMessage = "Milestone relic chosen: Bloodwake Emblem.";
                return;
            case 1:
                _runRelic = RunRelic.AstralConduit;
                resultMessage = "Milestone relic chosen: Astral Conduit.";
                return;
            case 2:
                _runRelic = RunRelic.VeilstriderCharm;
                resultMessage = "Milestone relic chosen: Veilstrider Charm.";
                return;
            default:
                _runRelic = RunRelic.BloodwakeEmblem;
                resultMessage = "Milestone relic defaulted to Bloodwake Emblem.";
                return;
        }
    }

    private void ApplyArchetypeChoice(int optionIndex, out string resultMessage)
    {
        switch (optionIndex)
        {
            case 0:
                _runArchetype = RunArchetype.Vanguard;
                _runMeleeBonus += 1;
                _runDefenseBonus += 2;
                resultMessage = "Archetype chosen: Vanguard. +1 melee damage, +2 defense.";
                return;
            case 1:
                _runArchetype = RunArchetype.Arcanist;
                _runSpellBonus += 2;
                AddInventoryItemQuantity("mana_draught", 1);
                resultMessage = "Archetype chosen: Arcanist. +2 spell damage, +1 Mana Draught.";
                return;
            case 2:
                _runArchetype = RunArchetype.Skirmisher;
                _runCritBonus += 3;
                _runFleeBonus += 5;
                AddInventoryItemQuantity("sharpening_oil", 1);
                resultMessage = "Archetype chosen: Skirmisher. +3% crit, +5% flee, +1 Sharpening Oil.";
                return;
            default:
                _runArchetype = RunArchetype.Vanguard;
                _runMeleeBonus += 1;
                _runDefenseBonus += 2;
                resultMessage = "Archetype defaulted to Vanguard.";
                return;
        }
    }

    private void ResetRelicCombatTriggers()
    {
        _relicMeleeTriggerUsedThisCombat = false;
        _relicSpellTriggerUsedThisCombat = false;
        _relicFleeTriggerUsedThisCombat = false;
    }

    private void ApplyRelicMeleeTrigger()
    {
        if (_player == null) return;
        if (_runRelic != RunRelic.BloodwakeEmblem || _relicMeleeTriggerUsedThisCombat) return;

        _relicMeleeTriggerUsedThisCombat = true;
        var heal = BloodwakeBaseHeal + (_runArchetype == RunArchetype.Vanguard ? BloodwakeVanguardBonusHeal : 0);
        var before = _player.CurrentHp;
        _player.CurrentHp = Math.Min(_player.MaxHp, _player.CurrentHp + heal);
        var gained = _player.CurrentHp - before;
        if (gained > 0)
        {
            PushCombatLog($"Bloodwake Emblem restores {gained} HP.");
            PushCombatLog($"Your HP {_player.CurrentHp}/{_player.MaxHp}.");
        }
    }

    private int ConsumeRelicSpellBurstDamage()
    {
        if (_runRelic != RunRelic.AstralConduit || _relicSpellTriggerUsedThisCombat)
        {
            return 0;
        }

        _relicSpellTriggerUsedThisCombat = true;
        return AstralConduitBaseBurstDamage + (_runArchetype == RunArchetype.Arcanist ? AstralConduitArcanistBurstBonus : 0);
    }

    private int ConsumeRelicFleeBonus()
    {
        if (_runRelic != RunRelic.VeilstriderCharm || _relicFleeTriggerUsedThisCombat)
        {
            return 0;
        }

        _relicFleeTriggerUsedThisCombat = true;
        return VeilstriderBaseFleeBonus + (_runArchetype == RunArchetype.Skirmisher ? VeilstriderSkirmisherFleeBonus : 0);
    }

    private void ResetMilestoneCombatTriggers()
    {
        _milestoneArcChargesThisCombat = GetEffectiveArcRank();
        _milestoneEscapeChargesThisCombat = GetEffectiveEscapeRank();
    }

    private void ApplyMilestoneExecutionRewardOnEnemyDefeat()
    {
        if (_player == null) return;
        var executionRank = GetEffectiveExecutionRank();
        if (executionRank <= 0) return;

        var heal = ExecutionDoctrineBaseHeal + executionRank * ExecutionDoctrineHealPerRank;
        if (_runArchetype == RunArchetype.Vanguard)
        {
            heal += ExecutionDoctrineVanguardHealBonus;
        }

        var beforeHp = _player.CurrentHp;
        _player.CurrentHp = Math.Min(_player.MaxHp, _player.CurrentHp + heal);
        var hpGained = _player.CurrentHp - beforeHp;
        if (hpGained > 0)
        {
            PushCombatLog($"Execution Doctrine restores {hpGained} HP.");
            PushCombatLog($"Your HP {_player.CurrentHp}/{_player.MaxHp}.");
        }

        if (_runArchetype == RunArchetype.Arcanist && _player.MaxMana > 0)
        {
            var manaGain = Math.Max(1, executionRank * ExecutionDoctrineArcanistManaPerRank);
            var beforeMp = _player.CurrentMana;
            _player.CurrentMana = Math.Min(_player.MaxMana, _player.CurrentMana + manaGain);
            var mpGained = _player.CurrentMana - beforeMp;
            if (mpGained > 0)
            {
                PushCombatLog($"Execution Doctrine also restores {mpGained} MP.");
            }
        }
    }

    private bool TryConsumeMilestoneArcSlotWaive()
    {
        if (_milestoneArcChargesThisCombat <= 0)
        {
            return false;
        }

        _milestoneArcChargesThisCombat -= 1;
        return true;
    }

    private bool TryConsumeMilestoneEscapeBlock()
    {
        if (_milestoneEscapeChargesThisCombat <= 0)
        {
            return false;
        }

        _milestoneEscapeChargesThisCombat -= 1;
        return true;
    }

    private int GetArcDoctrineWaiveBonusDamage()
    {
        var arcRank = GetEffectiveArcRank();
        if (arcRank <= 0)
        {
            return 0;
        }

        var bonus = ArcDoctrineBaseBonusDamage + arcRank;
        if (_runArchetype == RunArchetype.Arcanist)
        {
            bonus += ArcDoctrineArcanistBonusDamage;
        }

        return bonus;
    }

    private void ShowRewardMessage(string message, bool requireAcknowledge, double visibleSeconds)
    {
        _rewardMessage = message;
        _rewardMessageRequiresAcknowledge = requireAcknowledge;
        _rewardMessageExpiresAt = requireAcknowledge
            ? -1
            : Raylib.GetTime() + Math.Max(1.0, visibleSeconds);
    }

    private void ClearRewardMessage()
    {
        _rewardMessage = string.Empty;
        _rewardMessageRequiresAcknowledge = false;
        _rewardMessageExpiresAt = -1;
    }

    private void ResetLootPickupState()
    {
        _activePickupLootId = null;
        _activePickupProgressSeconds = 0;
        _activePickupStatus = string.Empty;
        _activePickupStatusUntil = -1;
    }

    private void UpdateRewardMessageTimer()
    {
        if (_rewardMessageRequiresAcknowledge) return;
        if (_rewardMessageExpiresAt <= 0) return;
        if (_gameState == GameState.PauseMenu) return;

        if (Raylib.GetTime() >= _rewardMessageExpiresAt)
        {
            ClearRewardMessage();
        }
    }

    private void ProcessTimers()
    {
        if (_gameState == GameState.PauseMenu || _gameState == GameState.RewardChoice || _gameState == GameState.VictoryScreen)
        {
            return;
        }

        if (_gameState == GameState.DeathScreen || (_player != null && !_player.IsAlive))
        {
            _enemyResolveAt = -1;
            _defeatedEnemyPending = null;
            _respawnEnemiesAt = -1;
            _resolvingEnemyDeath = false;
            return;
        }

        var now = Raylib.GetTime();

        if (_enemyResolveAt > 0 && now >= _enemyResolveAt && _defeatedEnemyPending != null)
        {
            ResolveEnemyDefeat(_defeatedEnemyPending);
            _defeatedEnemyPending = null;
            _enemyResolveAt = -1;
        }

        if (_respawnEnemiesAt > 0 && now >= _respawnEnemiesAt)
        {
            SpawnEnemies();
            _respawnEnemiesAt = -1;
        }
    }

    private void EnsurePlayerStateConsistency()
    {
        if (_player == null || _player.IsAlive) return;
        if (_gameState == GameState.DeathScreen) return;

        HandlePlayerDeath();
    }

    private void HandleInput()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.F11))
        {
            Raylib.ToggleFullscreen();
        }

        if (Pressed(KeyF1))
        {
            _debugOverlayEnabled = !_debugOverlayEnabled;
        }

        if (_player != null && !_player.IsAlive && _gameState != GameState.DeathScreen)
        {
            HandlePlayerDeath();
            return;
        }

        switch (_gameState)
        {
            case GameState.StartMenu:
                var startOptions = GetStartMenuOptions();
                if (startOptions.Count == 0) return;
                _startMenuIndex = Math.Clamp(_startMenuIndex, 0, startOptions.Count - 1);
                if (Pressed(KeyUp))
                {
                    _startMenuIndex = (_startMenuIndex - 1 + startOptions.Count) % startOptions.Count;
                }
                else if (Pressed(KeyDown))
                {
                    _startMenuIndex = (_startMenuIndex + 1) % startOptions.Count;
                }
                else if (Pressed(KeyEnter))
                {
                    var selected = startOptions[_startMenuIndex];
                    if (string.Equals(selected, "Continue", StringComparison.Ordinal))
                    {
                        ContinueLatestSave();
                    }
                    else if (string.Equals(selected, "New Game", StringComparison.Ordinal))
                    {
                        PrepareNewGame();
                        BeginCharacterCreationHub();
                    }
                    else if (string.Equals(selected, "How To Play", StringComparison.Ordinal))
                    {
                        _gameState = GameState.HelpMenu;
                    }
                    else if (string.Equals(selected, "Quit", StringComparison.Ordinal))
                    {
                        Raylib.CloseWindow();
                    }
                }
                break;

            case GameState.HelpMenu:
                if (Pressed(KeyEscape) || Pressed(KeyEnter))
                {
                    _gameState = GameState.StartMenu;
                }
                break;

            case GameState.CharacterCreationHub:
                HandleCharacterCreationHubInput();
                break;

            case GameState.CharacterName:
                HandleNameInput();
                break;

            case GameState.CharacterGender:
                if (Pressed(KeyUp))
                {
                    _selectedGenderIndex = (_selectedGenderIndex - 1 + Genders.Length) % Genders.Length;
                }
                else if (Pressed(KeyDown))
                {
                    _selectedGenderIndex = (_selectedGenderIndex + 1) % Genders.Length;
                }
                else if (Pressed(KeyEnter))
                {
                    _gameState = GameState.CharacterClass;
                }
                break;

            case GameState.CharacterClass:
                if (Pressed(KeyUp))
                {
                    _selectedClassIndex = (_selectedClassIndex - 1 + CharacterClasses.All.Count) % CharacterClasses.All.Count;
                }
                else if (Pressed(KeyDown))
                {
                    _selectedClassIndex = (_selectedClassIndex + 1) % CharacterClasses.All.Count;
                }
                else if (Pressed(KeyEnter))
                {
                    var chosenClass = CharacterClasses.All[_selectedClassIndex];
                    var chosenGender = Genders[_selectedGenderIndex];
                    var chosenRace = Races[_selectedRaceIndex];
                    _player = new Player(2, 2, chosenClass, _pendingName.Trim(), chosenGender, chosenRace);
                    _creationPointsRemaining = 6;
                    _creationSelectionIndex = 0;
                    _gameState = GameState.CharacterStatAllocation;
                }
                break;

            case GameState.CharacterStatAllocation:
                HandleCharacterStatAllocationInput();
                break;

            case GameState.Playing:
                HandlePlayingInput();
                break;

            case GameState.Combat:
                HandleCombatInput();
                break;

            case GameState.CombatSkillMenu:
                HandleCombatSkillInput();
                break;

            case GameState.CombatSpellMenu:
                HandleCombatSpellInput();
                break;
            case GameState.CombatSpellTargeting:
                HandleCombatSpellTargetingInput();
                break;
            case GameState.CombatItemMenu:
                HandleCombatItemInput();
                break;

            case GameState.CharacterMenu:
                if (Pressed(KeyUp))
                {
                    _characterSheetScroll = Math.Max(0, _characterSheetScroll - 1);
                }
                else if (Pressed(KeyDown))
                {
                    _characterSheetScroll += 1;
                    ClampCharacterSheetScroll();
                }
                else if (Pressed(KeyC) || Pressed(KeyEscape))
                {
                    _gameState = GameState.Playing;
                }
                break;

            case GameState.LevelUp:
                HandleLevelUpInput();
                break;

            case GameState.FeatSelection:
                HandleFeatSelectionInput();
                break;

            case GameState.SpellSelection:
                HandleSpellSelectionInput();
                break;

            case GameState.SkillSelection:
                HandleSkillSelectionInput();
                break;

            case GameState.RewardChoice:
                HandleRewardChoiceInput();
                break;

            case GameState.PauseMenu:
                HandlePauseMenuInput();
                break;

            case GameState.VictoryScreen:
                if (Pressed(KeyEnter) || Pressed(KeyEscape))
                {
                    ReturnToMainMenu();
                }
                break;

            case GameState.DeathScreen:
                if (Pressed(KeyEnter) || Pressed(KeyEscape))
                {
                    ReturnToMainMenu();
                }
                break;
        }
    }

    private void BeginCharacterCreationHub()
    {
        _creationSectionIndex = 0;
        _selectedCreationIdentityIndex = 0;
        _selectedAppearanceIndex = 0;
        _selectedCreationConditionIndex = 0;
        _creationSelectionIndex = 0;
        _selectedSpellLearnIndex = 0;
        _selectedCreationFeatIndex = 0;
        _spellLearnMenuOffset = 0;
        _creationFeatMenuOffset = 0;
        _creationMessage = "Use 1-6 or LEFT/RIGHT to move sections. A/D works outside Name field.";
        _startMenuMessage = string.Empty;
        _selectionMessage = string.Empty;

        _pendingName = string.Empty;
        _selectedGenderIndex = 0;
        _selectedRaceIndex = 0;
        _selectedClassIndex = 0;
        SetPlayerAppearanceBySpriteId(ResolveDefaultSpriteForRaceAndGender(Races[_selectedRaceIndex], Genders[_selectedGenderIndex]));
        Array.Clear(_creationAllocatedStats, 0, _creationAllocatedStats.Length);
        _creationStatAllocationOrder.Clear();
        _creationChosenSpellIds.Clear();
        _creationChosenSpellOrder.Clear();
        _creationChosenFeatIds.Clear();
        _creationChosenFeatOrder.Clear();
        _creationPointsRemaining = 6;
        _creationOriginCondition = CreationConditionPreset.None;
        _activeMajorConditions.Clear();
        _dungeonConditionEventsTriggered = 0;

        RebuildCreationPlayer(keepStats: false, keepSpells: false, keepFeats: false);
        _gameState = GameState.CharacterCreationHub;
    }

    private void RebuildCreationPlayer(bool keepStats, bool keepSpells, bool keepFeats)
    {
        if (!keepStats)
        {
            Array.Clear(_creationAllocatedStats, 0, _creationAllocatedStats.Length);
            _creationStatAllocationOrder.Clear();
        }
        else
        {
            _creationStatAllocationOrder.RemoveAll(index => index < 0 || index >= StatOrder.Length);
            if (_creationStatAllocationOrder.Count == 0 && _creationAllocatedStats.Any(points => points > 0))
            {
                for (var i = 0; i < StatOrder.Length; i++)
                {
                    var points = Math.Max(0, _creationAllocatedStats[i]);
                    for (var p = 0; p < points; p++)
                    {
                        _creationStatAllocationOrder.Add(i);
                    }
                }
            }

            var normalizedStats = new int[StatOrder.Length];
            foreach (var index in _creationStatAllocationOrder)
            {
                normalizedStats[index] += 1;
            }

            if (!_creationAllocatedStats.SequenceEqual(normalizedStats))
            {
                Array.Copy(normalizedStats, _creationAllocatedStats, StatOrder.Length);
            }
        }

        if (!keepSpells)
        {
            _creationChosenSpellIds.Clear();
            _creationChosenSpellOrder.Clear();
        }
        else
        {
            if (_creationChosenSpellOrder.Count == 0 && _creationChosenSpellIds.Count > 0)
            {
                _creationChosenSpellOrder.AddRange(_creationChosenSpellIds.OrderBy(id => id, StringComparer.Ordinal));
            }

            var uniqueOrder = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var spellId in _creationChosenSpellOrder)
            {
                if (string.IsNullOrWhiteSpace(spellId)) continue;
                if (seen.Add(spellId))
                {
                    uniqueOrder.Add(spellId);
                }
            }

            foreach (var spellId in _creationChosenSpellIds)
            {
                if (string.IsNullOrWhiteSpace(spellId)) continue;
                if (seen.Add(spellId))
                {
                    uniqueOrder.Add(spellId);
                }
            }

            _creationChosenSpellOrder.Clear();
            _creationChosenSpellOrder.AddRange(uniqueOrder);
            _creationChosenSpellIds.Clear();
            foreach (var spellId in _creationChosenSpellOrder)
            {
                _creationChosenSpellIds.Add(spellId);
            }
        }

        if (!keepFeats)
        {
            _creationChosenFeatIds.Clear();
            _creationChosenFeatOrder.Clear();
        }
        else
        {
            if (_creationChosenFeatOrder.Count == 0 && _creationChosenFeatIds.Count > 0)
            {
                _creationChosenFeatOrder.AddRange(_creationChosenFeatIds.OrderBy(id => id, StringComparer.Ordinal));
            }

            var uniqueFeatOrder = new List<string>();
            var seenFeats = new HashSet<string>(StringComparer.Ordinal);
            foreach (var featId in _creationChosenFeatOrder)
            {
                if (string.IsNullOrWhiteSpace(featId)) continue;
                if (seenFeats.Add(featId))
                {
                    uniqueFeatOrder.Add(featId);
                }
            }

            foreach (var featId in _creationChosenFeatIds)
            {
                if (string.IsNullOrWhiteSpace(featId)) continue;
                if (seenFeats.Add(featId))
                {
                    uniqueFeatOrder.Add(featId);
                }
            }

            var maxStartingPicks = FeatProgression.GetCreationStartingFeatPicks();
            if (maxStartingPicks >= 0 && uniqueFeatOrder.Count > maxStartingPicks)
            {
                uniqueFeatOrder = uniqueFeatOrder.Take(maxStartingPicks).ToList();
            }

            _creationChosenFeatOrder.Clear();
            _creationChosenFeatOrder.AddRange(uniqueFeatOrder);
            _creationChosenFeatIds.Clear();
            foreach (var featId in _creationChosenFeatOrder)
            {
                _creationChosenFeatIds.Add(featId);
            }
        }

        var chosenClass = CharacterClasses.All[_selectedClassIndex];
        var chosenGender = Genders[_selectedGenderIndex];
        var chosenRace = Races[_selectedRaceIndex];
        var displayName = string.IsNullOrWhiteSpace(_pendingName) ? "Adventurer" : _pendingName.Trim();
        _player = new Player(2, 2, chosenClass, displayName, chosenGender, chosenRace);
        if (!IsValidPlayerSpriteId(_selectedPlayerSpriteId))
        {
            SetPlayerAppearanceBySpriteId(ResolveDefaultSpriteForRaceAndGender(chosenRace, chosenGender));
        }

        var allocatedTotal = 0;
        for (var i = 0; i < StatOrder.Length; i++)
        {
            var points = Math.Max(0, _creationAllocatedStats[i]);
            allocatedTotal += points;
            for (var p = 0; p < points; p++)
            {
                _player.AllocateCreationStatPoint(StatOrder[i]);
            }
        }

        _creationPointsRemaining = Math.Max(0, 6 - allocatedTotal);

        if (keepSpells && _creationChosenSpellOrder.Count > 0)
        {
            var applied = new List<string>();
            foreach (var spellId in _creationChosenSpellOrder)
            {
                if (!SpellData.ById.TryGetValue(spellId, out var spell)) continue;
                if (_player.LearnSpell(spell))
                {
                    _creationChosenSpellIds.Add(spellId);
                    applied.Add(spellId);
                }
            }

            _creationChosenSpellIds.Clear();
            _creationChosenSpellOrder.Clear();
            foreach (var id in applied)
            {
                _creationChosenSpellIds.Add(id);
                _creationChosenSpellOrder.Add(id);
            }
        }
        else
        {
            _creationChosenSpellIds.Clear();
            _creationChosenSpellOrder.Clear();
        }

        var startingFeatPicks = FeatProgression.GetCreationStartingFeatPicks();
        if (startingFeatPicks > 0)
        {
            _player.AddFeatPoints(startingFeatPicks);
        }

        if (keepFeats && _creationChosenFeatOrder.Count > 0)
        {
            var appliedFeats = new List<string>();
            foreach (var featId in _creationChosenFeatOrder)
            {
                if (!FeatBook.ById.TryGetValue(featId, out var feat)) continue;
                if (_player.LearnFeat(feat))
                {
                    _creationChosenFeatIds.Add(featId);
                    appliedFeats.Add(featId);
                }
            }

            _creationChosenFeatIds.Clear();
            _creationChosenFeatOrder.Clear();
            foreach (var id in appliedFeats)
            {
                _creationChosenFeatIds.Add(id);
                _creationChosenFeatOrder.Add(id);
            }
        }
        else
        {
            _creationChosenFeatIds.Clear();
            _creationChosenFeatOrder.Clear();
        }

        RefreshCreationLearnableSpells();
        RefreshCreationFeatChoices();
    }

    private int GetCreationSpellMenuCount()
    {
        var hasSpellPicks = _creationChosenSpellOrder.Count > 0;
        return _creationLearnableSpells.Count + (hasSpellPicks ? 2 : 0);
    }

    private int GetCreationSpellUndoRowIndex()
    {
        return _creationChosenSpellOrder.Count > 0 ? _creationLearnableSpells.Count : -1;
    }

    private int GetCreationSpellResetRowIndex()
    {
        return _creationChosenSpellOrder.Count > 0 ? _creationLearnableSpells.Count + 1 : -1;
    }

    private void RefreshCreationLearnableSpells()
    {
        _creationLearnableSpells.Clear();
        if (_player != null)
        {
            _creationLearnableSpells.AddRange(_player.GetClassSpells());
        }

        var menuCount = GetCreationSpellMenuCount();
        if (menuCount <= 0)
        {
            _selectedSpellLearnIndex = 0;
            _spellLearnMenuOffset = 0;
            return;
        }

        _selectedSpellLearnIndex = Math.Clamp(_selectedSpellLearnIndex, 0, menuCount - 1);
        EnsureSpellLearnSelectionVisible(menuCount);
    }

    private void RefreshCreationFeatChoices()
    {
        _creationFeatChoices.Clear();
        _creationFeatChoices.AddRange(FeatBook.All
            .OrderBy(feat => feat.MinLevel)
            .ThenBy(feat => feat.Name, StringComparer.Ordinal));

        if (_creationFeatChoices.Count == 0)
        {
            _selectedCreationFeatIndex = 0;
            _creationFeatMenuOffset = 0;
            return;
        }

        _selectedCreationFeatIndex = Math.Clamp(_selectedCreationFeatIndex, 0, _creationFeatChoices.Count - 1);
        EnsureCreationFeatSelectionVisible(_creationFeatChoices.Count);
    }

    private bool IsCreationNameReady()
    {
        return !string.IsNullOrWhiteSpace(_pendingName);
    }

    private bool IsCreationStatsReady()
    {
        return _creationPointsRemaining == 0;
    }

    private bool IsCreationSpellsReady()
    {
        return _player != null && _player.SpellPickPoints == 0;
    }

    private bool IsCreationFeatsReady()
    {
        return _player != null && _player.FeatPoints == 0;
    }

    private bool IsCreationReady()
    {
        return IsCreationNameReady() && IsCreationStatsReady() && IsCreationSpellsReady() && IsCreationFeatsReady();
    }

    private bool IsCreationSectionReady(int sectionIndex)
    {
        return sectionIndex switch
        {
            0 => IsCreationNameReady(),
            1 => true,
            2 => IsCreationStatsReady(),
            3 => IsCreationSpellsReady(),
            4 => IsCreationFeatsReady(),
            5 => IsCreationReady(),
            _ => false
        };
    }

    private string GetCreationSectionHint()
    {
        return _creationSectionIndex switch
        {
            0 => "Identity: set Name, Gender, Race, Appearance, and optional origin condition.",
            1 => "Class: UP/DOWN browse classes. ENTER confirms and moves to Stats.",
            2 => "Stats: spend all 6 points. ENTER adds to selected stat; use Undo/Reset rows as needed.",
            3 => "Spells: browse class spell list. ENTER learns spells; use Undo/Reset rows if you change your mind.",
            4 => "Feats: choose your starting feat. Locked feats show exact prerequisites.",
            5 => "Review: all checks must be green before starting.",
            _ => "Character creation"
        };
    }

    private static int GetCreationSectionHotkey()
    {
        if (Pressed(Key1)) return 0;
        if (Pressed(Key2)) return 1;
        if (Pressed(Key3)) return 2;
        if (Pressed(Key4)) return 3;
        if (Pressed(Key5)) return 4;
        if (Pressed(Key6)) return 5;
        return -1;
    }

    private void HandleCharacterCreationHubInput()
    {
        if (Pressed(KeyEscape))
        {
            ReturnToMainMenu();
            return;
        }

        var hotkeySection = GetCreationSectionHotkey();
        if (hotkeySection >= 0)
        {
            _creationSectionIndex = hotkeySection;
            _creationMessage = $"Moved to {CreationSections[_creationSectionIndex]} (hotkey {hotkeySection + 1}).";
            return;
        }

        var isTypingNameField = _creationSectionIndex == 0 && _selectedCreationIdentityIndex == 0;

        if (Pressed(KeyLeft) || (!isTypingNameField && Pressed(KeyA)))
        {
            _creationSectionIndex = (_creationSectionIndex - 1 + CreationSections.Length) % CreationSections.Length;
            return;
        }

        if (Pressed(KeyRight) || (!isTypingNameField && Pressed(KeyD)))
        {
            _creationSectionIndex = (_creationSectionIndex + 1) % CreationSections.Length;
            return;
        }

        switch (_creationSectionIndex)
        {
            case 0:
                HandleCreationIdentityInput();
                break;
            case 1:
                HandleCreationClassInput();
                break;
            case 2:
                HandleCreationStatsInput();
                break;
            case 3:
                HandleCreationSpellsInput();
                break;
            case 4:
                HandleCreationFeatsInput();
                break;
            case 5:
                HandleCreationReviewInput();
                break;
        }
    }

    private void HandleCreationIdentityInput()
    {
        const int identityFieldCount = 5;
        if (Pressed(KeyUp))
        {
            _selectedCreationIdentityIndex = (_selectedCreationIdentityIndex - 1 + identityFieldCount) % identityFieldCount;
            return;
        }

        if (Pressed(KeyDown))
        {
            _selectedCreationIdentityIndex = (_selectedCreationIdentityIndex + 1) % identityFieldCount;
            return;
        }

        if (_selectedCreationIdentityIndex == 0)
        {
            if (Pressed(KeyBackspace) && _pendingName.Length > 0)
            {
                _pendingName = _pendingName[..^1];
            }

            while (true)
            {
                var ch = Raylib.GetCharPressed();
                if (ch == 0) break;
                if (_pendingName.Length >= 18) break;
                var c = (char)ch;
                if (char.IsLetterOrDigit(c) || c == ' ')
                {
                    _pendingName += c;
                }
            }

            if (Pressed(KeyEnter))
            {
                _selectedCreationIdentityIndex = 1;
            }

            return;
        }

        if (_selectedCreationIdentityIndex == 1)
        {
            if (Pressed(KeyLeft))
            {
                _selectedGenderIndex = (_selectedGenderIndex - 1 + Genders.Length) % Genders.Length;
                SetPlayerAppearanceBySpriteId(ResolveDefaultSpriteForRaceAndGender(Races[_selectedRaceIndex], Genders[_selectedGenderIndex]));
                RebuildCreationPlayer(keepStats: true, keepSpells: true, keepFeats: true);
                return;
            }

            if (Pressed(KeyRight) || Pressed(KeyEnter))
            {
                _selectedGenderIndex = (_selectedGenderIndex + 1) % Genders.Length;
                SetPlayerAppearanceBySpriteId(ResolveDefaultSpriteForRaceAndGender(Races[_selectedRaceIndex], Genders[_selectedGenderIndex]));
                RebuildCreationPlayer(keepStats: true, keepSpells: true, keepFeats: true);
            }

            return;
        }

        if (_selectedCreationIdentityIndex == 2)
        {
            if (Pressed(KeyLeft))
            {
                _selectedRaceIndex = (_selectedRaceIndex - 1 + Races.Length) % Races.Length;
                SetPlayerAppearanceBySpriteId(ResolveDefaultSpriteForRaceAndGender(Races[_selectedRaceIndex], Genders[_selectedGenderIndex]));
                RebuildCreationPlayer(keepStats: true, keepSpells: true, keepFeats: true);
                return;
            }

            if (Pressed(KeyRight) || Pressed(KeyEnter))
            {
                _selectedRaceIndex = (_selectedRaceIndex + 1) % Races.Length;
                SetPlayerAppearanceBySpriteId(ResolveDefaultSpriteForRaceAndGender(Races[_selectedRaceIndex], Genders[_selectedGenderIndex]));
                RebuildCreationPlayer(keepStats: true, keepSpells: true, keepFeats: true);
            }

            return;
        }

        if (_selectedCreationIdentityIndex == 3)
        {
            if (Pressed(KeyLeft))
            {
                CyclePlayerAppearance(-1);
                return;
            }

            if (Pressed(KeyRight) || Pressed(KeyEnter))
            {
                CyclePlayerAppearance(1);
            }

            return;
        }

        if (Pressed(KeyLeft))
        {
            _selectedCreationConditionIndex =
                (_selectedCreationConditionIndex - 1 + CreationConditionOptions.Length) % CreationConditionOptions.Length;
            _creationMessage = $"Origin condition: {CreationConditionOptions[_selectedCreationConditionIndex].Label}.";
            return;
        }

        if (Pressed(KeyRight) || Pressed(KeyEnter))
        {
            _selectedCreationConditionIndex = (_selectedCreationConditionIndex + 1) % CreationConditionOptions.Length;
            _creationMessage = $"Origin condition: {CreationConditionOptions[_selectedCreationConditionIndex].Label}.";
        }
    }

    private void CyclePlayerAppearance(int delta)
    {
        var count = PlayerAppearanceOptions.Length;
        _selectedAppearanceIndex = (_selectedAppearanceIndex + delta + count) % count;
        _selectedPlayerSpriteId = PlayerAppearanceOptions[_selectedAppearanceIndex].Id;
        _creationMessage = $"Appearance set: {PlayerAppearanceOptions[_selectedAppearanceIndex].Label}.";
    }

    private void SetPlayerAppearanceBySpriteId(string spriteId)
    {
        if (string.IsNullOrWhiteSpace(spriteId))
        {
            spriteId = "knight_m";
        }

        for (var i = 0; i < PlayerAppearanceOptions.Length; i++)
        {
            if (!string.Equals(PlayerAppearanceOptions[i].Id, spriteId, StringComparison.OrdinalIgnoreCase)) continue;
            _selectedAppearanceIndex = i;
            _selectedPlayerSpriteId = PlayerAppearanceOptions[i].Id;
            return;
        }

        _selectedAppearanceIndex = 0;
        _selectedPlayerSpriteId = PlayerAppearanceOptions[0].Id;
    }

    private static bool IsValidPlayerSpriteId(string spriteId)
    {
        return PlayerAppearanceOptions.Any(option =>
            string.Equals(option.Id, spriteId, StringComparison.OrdinalIgnoreCase));
    }

    private string GetSelectedAppearanceLabel()
    {
        if (_selectedAppearanceIndex >= 0 && _selectedAppearanceIndex < PlayerAppearanceOptions.Length)
        {
            return PlayerAppearanceOptions[_selectedAppearanceIndex].Label;
        }

        return "Custom";
    }

    private static string ResolveDefaultSpriteForRaceAndGender(Race race, Gender gender)
    {
        var female = gender == Gender.Female;
        return race switch
        {
            Race.Elf => female ? "elf_f" : "elf_m",
            Race.Dwarf => female ? "dwarf_f" : "dwarf_m",
            _ => female ? "knight_f" : "knight_m"
        };
    }

    private void HandleCreationClassInput()
    {
        if (Pressed(KeyUp))
        {
            ChangeCreationClass(-1);
            return;
        }

        if (Pressed(KeyDown))
        {
            ChangeCreationClass(1);
            return;
        }

        if (Pressed(KeyEnter))
        {
            _creationSectionIndex = 2;
            _creationMessage = "Class confirmed. Allocate your 6 stat points.";
        }
    }

    private void ChangeCreationClass(int delta)
    {
        var next = (_selectedClassIndex + delta + CharacterClasses.All.Count) % CharacterClasses.All.Count;
        if (next == _selectedClassIndex) return;

        _selectedClassIndex = next;
        RebuildCreationPlayer(keepStats: false, keepSpells: false, keepFeats: true);
        _creationSelectionIndex = 0;
        _selectedSpellLearnIndex = 0;
        _creationMessage = "Class changed: stat/spell setup reset and starting feat revalidated.";
    }

    private void HandleCreationStatsInput()
    {
        var undoIndex = StatOrder.Length;
        var menuCount = StatOrder.Length + 2;
        if (Pressed(KeyUp))
        {
            _creationSelectionIndex = (_creationSelectionIndex - 1 + menuCount) % menuCount;
            return;
        }

        if (Pressed(KeyDown))
        {
            _creationSelectionIndex = (_creationSelectionIndex + 1) % menuCount;
            return;
        }

        if (!Pressed(KeyEnter)) return;

        if (_creationSelectionIndex < StatOrder.Length)
        {
            if (_player == null || _creationPointsRemaining <= 0)
            {
                _creationMessage = "No stat points left. Move to Spells/Feats/Review or reset stats.";
                return;
            }

            var stat = StatOrder[_creationSelectionIndex];
            _player.AllocateCreationStatPoint(stat);
            _creationAllocatedStats[_creationSelectionIndex] += 1;
            _creationStatAllocationOrder.Add(_creationSelectionIndex);
            _creationPointsRemaining -= 1;
            _creationMessage = _creationPointsRemaining == 0
                ? "All stat points allocated. Move to Spells, Feats, or Review."
                : $"{stat} increased. {_creationPointsRemaining} points left.";
            return;
        }

        if (_creationSelectionIndex == undoIndex)
        {
            if (_creationStatAllocationOrder.Count == 0)
            {
                _creationMessage = "No stat points allocated yet to undo.";
                return;
            }

            var lastIndex = _creationStatAllocationOrder[^1];
            _creationStatAllocationOrder.RemoveAt(_creationStatAllocationOrder.Count - 1);
            if (lastIndex < 0 || lastIndex >= StatOrder.Length || _creationAllocatedStats[lastIndex] <= 0)
            {
                RebuildCreationPlayer(keepStats: true, keepSpells: true, keepFeats: true);
                _creationMessage = "Last stat pick could not be resolved; allocation rebuilt.";
                return;
            }

            _creationAllocatedStats[lastIndex] -= 1;
            RebuildCreationPlayer(keepStats: true, keepSpells: true, keepFeats: true);
            _creationSelectionIndex = lastIndex;
            _creationMessage = $"{StatOrder[lastIndex]} undo applied. {_creationPointsRemaining} points left.";
            return;
        }

        if (_creationAllocatedStats.All(points => points == 0))
        {
            _creationMessage = "No allocated stats to reset.";
            return;
        }

        Array.Clear(_creationAllocatedStats, 0, _creationAllocatedStats.Length);
        _creationStatAllocationOrder.Clear();
        RebuildCreationPlayer(keepStats: false, keepSpells: true, keepFeats: true);
        _creationSelectionIndex = 0;
        _creationMessage = "Stat allocation reset.";
    }

    private void HandleCreationSpellsInput()
    {
        if (_player == null) return;

        var undoIndex = GetCreationSpellUndoRowIndex();
        var resetIndex = GetCreationSpellResetRowIndex();
        var menuCount = GetCreationSpellMenuCount();

        if (Pressed(KeyUp))
        {
            if (menuCount > 0)
            {
                _selectedSpellLearnIndex = (_selectedSpellLearnIndex - 1 + menuCount) % menuCount;
                EnsureSpellLearnSelectionVisible(menuCount);
            }
            return;
        }

        if (Pressed(KeyDown))
        {
            if (menuCount > 0)
            {
                _selectedSpellLearnIndex = (_selectedSpellLearnIndex + 1) % menuCount;
                EnsureSpellLearnSelectionVisible(menuCount);
            }
            return;
        }

        if (!Pressed(KeyEnter)) return;
        if (menuCount == 0)
        {
            _creationMessage = _player.IsCasterClass
                ? "No class spells available for this level band."
                : "This class has no spell list in the current prototype scope.";
            return;
        }

        if (_selectedSpellLearnIndex == undoIndex)
        {
            if (_creationChosenSpellOrder.Count == 0)
            {
                _creationMessage = "No selected spells to undo.";
                return;
            }

            var lastSpellId = _creationChosenSpellOrder[^1];
            _creationChosenSpellOrder.RemoveAt(_creationChosenSpellOrder.Count - 1);
            _creationChosenSpellIds.Remove(lastSpellId);
            RebuildCreationPlayer(keepStats: true, keepSpells: true, keepFeats: true);
            var newMenuCount = GetCreationSpellMenuCount();
            if (newMenuCount <= 0)
            {
                _selectedSpellLearnIndex = 0;
                _spellLearnMenuOffset = 0;
            }
            else
            {
                _selectedSpellLearnIndex = Math.Clamp(_selectedSpellLearnIndex, 0, newMenuCount - 1);
                EnsureSpellLearnSelectionVisible(newMenuCount);
            }

            var spellName = SpellData.ById.TryGetValue(lastSpellId, out var removedSpell) ? removedSpell.Name : lastSpellId;
            _creationMessage = $"{spellName} removed from selected spells.";
            return;
        }

        if (_selectedSpellLearnIndex == resetIndex)
        {
            _creationChosenSpellIds.Clear();
            _creationChosenSpellOrder.Clear();
            RebuildCreationPlayer(keepStats: true, keepSpells: false, keepFeats: true);
            _selectedSpellLearnIndex = 0;
            _spellLearnMenuOffset = 0;
            _creationMessage = "Spell picks reset.";
            return;
        }

        if (_selectedSpellLearnIndex < 0 || _selectedSpellLearnIndex >= _creationLearnableSpells.Count)
        {
            _creationMessage = "Select a spell row to learn a spell.";
            return;
        }

        var spell = _creationLearnableSpells[_selectedSpellLearnIndex];
        if (_player.KnowsSpell(spell.Id))
        {
            if (_creationChosenSpellIds.Contains(spell.Id) && _creationChosenSpellIds.Remove(spell.Id))
            {
                _creationChosenSpellOrder.RemoveAll(id => string.Equals(id, spell.Id, StringComparison.Ordinal));
                RebuildCreationPlayer(keepStats: true, keepSpells: true, keepFeats: true);
                var newMenuCount = GetCreationSpellMenuCount();
                if (newMenuCount <= 0)
                {
                    _selectedSpellLearnIndex = 0;
                    _spellLearnMenuOffset = 0;
                }
                else
                {
                    _selectedSpellLearnIndex = Math.Clamp(_selectedSpellLearnIndex, 0, newMenuCount - 1);
                    EnsureSpellLearnSelectionVisible(newMenuCount);
                }

                _creationMessage = $"{spell.Name} removed from selected spells.";
            }
            else
            {
                _creationMessage = spell.IsCantrip
                    ? $"{spell.Name} is a free class cantrip and is always known."
                    : $"{spell.Name} cannot be removed from this screen.";
            }

            return;
        }

        if (!_player.CanLearnSpell(spell, out var blockReason))
        {
            _creationMessage = $"{spell.Name}: {blockReason}";
            return;
        }

        if (_player.LearnSpell(spell))
        {
            if (_creationChosenSpellIds.Add(spell.Id))
            {
                _creationChosenSpellOrder.Add(spell.Id);
            }

            RefreshCreationLearnableSpells();
            _creationMessage = _player.SpellPickPoints == 0
                ? $"{spell.Name} learned. Spell picks complete."
                : $"{spell.Name} learned. {_player.SpellPickPoints} picks left.";
        }
        else
        {
            _creationMessage = $"Could not learn {spell.Name} right now.";
        }
    }

    private void HandleCreationFeatsInput()
    {
        if (_player == null) return;

        var menuCount = _creationFeatChoices.Count;
        if (Pressed(KeyUp))
        {
            if (menuCount > 0)
            {
                _selectedCreationFeatIndex = (_selectedCreationFeatIndex - 1 + menuCount) % menuCount;
                EnsureCreationFeatSelectionVisible(menuCount);
            }
            return;
        }

        if (Pressed(KeyDown))
        {
            if (menuCount > 0)
            {
                _selectedCreationFeatIndex = (_selectedCreationFeatIndex + 1) % menuCount;
                EnsureCreationFeatSelectionVisible(menuCount);
            }
            return;
        }

        if (!Pressed(KeyEnter)) return;
        if (menuCount == 0)
        {
            _creationMessage = "No feats available in the current catalog.";
            return;
        }

        if (_selectedCreationFeatIndex < 0 || _selectedCreationFeatIndex >= menuCount)
        {
            _creationMessage = "Select a feat row to choose your starting feat.";
            return;
        }

        var feat = _creationFeatChoices[_selectedCreationFeatIndex];
        if (_player.HasFeat(feat.Id) && _creationChosenFeatIds.Contains(feat.Id))
        {
            _creationChosenFeatIds.Clear();
            _creationChosenFeatOrder.Clear();
            RebuildCreationPlayer(keepStats: true, keepSpells: true, keepFeats: false);
            _creationMessage = $"{feat.Name} removed from starting feat selection.";
            return;
        }

        if (!_player.CanLearnFeat(feat, out var blockReason))
        {
            _creationMessage = $"{feat.Name}: {blockReason}";
            return;
        }

        _creationChosenFeatIds.Clear();
        _creationChosenFeatOrder.Clear();
        _creationChosenFeatIds.Add(feat.Id);
        _creationChosenFeatOrder.Add(feat.Id);
        RebuildCreationPlayer(keepStats: true, keepSpells: true, keepFeats: true);
        _creationMessage = _player.FeatPoints == 0
            ? $"{feat.Name} selected as your starting feat."
            : $"{feat.Name} selected. {_player.FeatPoints} feat picks left.";
    }

    private void HandleCreationReviewInput()
    {
        if (!Pressed(KeyEnter)) return;

        if (!IsCreationNameReady())
        {
            _creationMessage = "Name is required before starting.";
            return;
        }

        if (!IsCreationStatsReady())
        {
            _creationMessage = "Spend all 6 stat points before starting.";
            return;
        }

        if (_player == null)
        {
            _creationMessage = "Character data missing. Reopen class section.";
            return;
        }

        if (!IsCreationSpellsReady())
        {
            _creationMessage = "You still have spell picks remaining.";
            return;
        }

        if (!IsCreationFeatsReady())
        {
            _creationMessage = "Choose your starting feat before starting.";
            return;
        }

        RebuildCreationPlayer(keepStats: true, keepSpells: true, keepFeats: true);
        TryApplyCreationOriginConditionIfNeeded();
        SpawnEnemies();
        EnterPlayingState("adventure_start");
    }

    private void HandleNameInput()
    {
        if (Pressed(KeyEnter) && _pendingName.Trim().Length > 0)
        {
            _gameState = GameState.CharacterGender;
            return;
        }

        if (Pressed(KeyBackspace) && _pendingName.Length > 0)
        {
            _pendingName = _pendingName[..^1];
        }

        while (true)
        {
            var ch = Raylib.GetCharPressed();
            if (ch == 0) break;

            if (_pendingName.Length >= 18) break;

            var c = (char)ch;
            if (char.IsLetterOrDigit(c) || c == ' ')
            {
                _pendingName += c;
            }
        }
    }

    private void HandleCharacterStatAllocationInput()
    {
        if (_player == null) return;

        var menuSize = StatOrder.Length + 1;
        if (Pressed(KeyUp))
        {
            _creationSelectionIndex = (_creationSelectionIndex - 1 + menuSize) % menuSize;
            return;
        }

        if (Pressed(KeyDown))
        {
            _creationSelectionIndex = (_creationSelectionIndex + 1) % menuSize;
            return;
        }

        if (Pressed(KeyEscape))
        {
            _gameState = GameState.CharacterClass;
            return;
        }

        if (!Pressed(KeyEnter)) return;

        if (_creationSelectionIndex < StatOrder.Length)
        {
            if (_creationPointsRemaining <= 0) return;

            _player.AllocateCreationStatPoint(StatOrder[_creationSelectionIndex]);
            _creationPointsRemaining -= 1;
            return;
        }

        if (_creationPointsRemaining > 0) return;

        if (TryOpenSpellSelection(
                title: "Choose Starting Spells",
                nextState: GameState.Playing,
                startsAdventure: true))
        {
            return;
        }

        TryApplyCreationOriginConditionIfNeeded();
        SpawnEnemies();
        EnterPlayingState("adventure_start");
    }

    private void PrepareNewGame()
    {
        _pendingName = string.Empty;
        _selectedGenderIndex = 0;
        _selectedRaceIndex = 0;
        _selectedClassIndex = 0;
        _selectedAppearanceIndex = 0;
        _selectedCreationConditionIndex = 0;
        _selectedPlayerSpriteId = "knight_m";
        _selectedStatIndex = 0;
        _selectedSkillIndex = 0;
        _selectedFeatIndex = 0;
        _selectedSpellLearnIndex = 0;
        _selectedCreationFeatIndex = 0;
        _selectedCreationIdentityIndex = 0;
        _selectedActionIndex = 0;
        _selectedCombatSkillIndex = 0;
        _selectedSpellIndex = 0;
        _selectedCombatItemIndex = 0;
        _skillMenuOffset = 0;
        _spellMenuOffset = 0;
        _combatItemMenuOffset = 0;
        _spellLearnMenuOffset = 0;
        _creationFeatMenuOffset = 0;
        _featMenuOffset = 0;
        _characterSheetScroll = 0;
        _creationSectionIndex = 0;
        _creationSelectionIndex = 0;
        _creationPointsRemaining = 6;
        _pauseMenuIndex = 0;
        _pauseMenuView = PauseMenuView.Root;
        _startMenuMessage = string.Empty;

        _creationMessage = string.Empty;
        _selectionMessage = string.Empty;
        _pauseMessage = string.Empty;
        ClearRewardMessage();
        _player = null;
        _enemies.Clear();
        _currentEnemy = null;
        _activeRewardNode = null;
        _spellSelectionTitle = string.Empty;
        _spellSelectionStartsAdventure = false;
        _spellSelectionNextState = GameState.Playing;
        _skillChoices.Clear();
        _featChoices.Clear();
        _spellLearnChoices.Clear();
        _creationLearnableSpells.Clear();
        _creationFeatChoices.Clear();
        _creationChosenSpellIds.Clear();
        _creationChosenSpellOrder.Clear();
        _creationChosenFeatIds.Clear();
        _creationChosenFeatOrder.Clear();
        _creationStatAllocationOrder.Clear();
        _pauseSaveEntries.Clear();
        _pauseLoadEntries.Clear();
        _claimedRewardNodeIds.Clear();
        _inventoryItems.Clear();
        _enemyAi.Clear();
        _enemyLootKits.Clear();
        _groundLoot.Clear();
        Array.Clear(_creationAllocatedStats, 0, _creationAllocatedStats.Length);
        _combatLog.Clear();
        _enemyPoisoned = 0;
        _warCryAvailable = false;
        ResetEncounterContext();
        _resolvingEnemyDeath = false;
        _selectedRewardOptionIndex = 0;
        _bossDefeated = false;
        _floorCleared = false;
        _runArchetype = RunArchetype.None;
        _runRelic = RunRelic.None;
        _phase3RouteChoice = Phase3RouteChoice.None;
        _phase3RiskEventResolved = false;
        _phase3XpPercentMod = 0;
        _phase3EnemyAttackBonus = 0;
        _phase3EnemiesDefeated = 0;
        _phase3PreSanctumRewardGranted = false;
        _phase3RouteWaveSpawned = false;
        _phase3SanctumWaveSpawned = false;
        _phase3SanctumLockNoticeShown = false;
        _creationOriginCondition = CreationConditionPreset.None;
        _activeMajorConditions.Clear();
        _dungeonConditionEventsTriggered = 0;
        _currentFloorZone = FloorMacroZone.EntryFrontier;
        _milestoneChoicesTaken = 0;
        _milestoneExecutionRank = 0;
        _milestoneArcRank = 0;
        _milestoneEscapeRank = 0;
        ResetRelicCombatTriggers();
        ResetMilestoneCombatTriggers();
        _runMeleeBonus = 0;
        _runSpellBonus = 0;
        _runDefenseBonus = 0;
        _runCritBonus = 0;
        _runFleeBonus = 0;
        _enemyResolveAt = -1;
        _defeatedEnemyPending = null;
        _respawnEnemiesAt = -1;
        _nextMoveAt = -1;
        ResetLootPickupState();
        ResetCameraTracking();
        _pausedFromState = GameState.Playing;
        ResetPauseConfirm();
        InitializeRunInventory();
        SetPlayerAppearanceBySpriteId(ResolveDefaultSpriteForRaceAndGender(Races[_selectedRaceIndex], Genders[_selectedGenderIndex]));
    }

    private void ReturnToMainMenu()
    {
        PrepareNewGame();
        _startMenuIndex = 0;
        _gameState = GameState.StartMenu;
    }

    private void ContinueLatestSave()
    {
        var entries = SaveStore.GetAvailableLoadEntries();
        if (entries.Count == 0)
        {
            _startMenuMessage = "No save files found.";
            return;
        }

        var latest = entries[0];
        SaveOperationResult load;
        GameSaveSnapshot? snapshot;
        if (latest.IsAutosave)
        {
            load = SaveStore.LoadAutosave(out snapshot);
        }
        else
        {
            load = SaveStore.LoadManualSlot(latest.ManualSlot, out snapshot);
        }

        if (!load.Success || snapshot == null)
        {
            _startMenuMessage = load.Message;
            return;
        }

        if (!TryRestoreFromSnapshot(snapshot, out var restoreError))
        {
            _startMenuMessage = restoreError;
            return;
        }

        _startMenuMessage = string.Empty;
        PushCombatLog(load.Message);
    }

    private void SpawnEnemies()
    {
        _enemies.Clear();
        _enemyAi.Clear();
        _enemyLootKits.Clear();
        SpawnEnemyPack(Phase3EntryEnemyPack);
        _phase3RouteWaveSpawned = false;
        _phase3SanctumWaveSpawned = false;
    }

    private int SpawnEnemyPack((int X, int Y, string Key)[] pack)
    {
        var spawned = 0;
        for (var i = 0; i < pack.Length; i++)
        {
            var point = pack[i];
            if (!EnemyTypes.All.TryGetValue(point.Key, out var type))
            {
                continue;
            }

            if (IsWallOrSealed(point.X, point.Y))
            {
                continue;
            }

            if (_player != null && _player.X == point.X && _player.Y == point.Y)
            {
                continue;
            }

            if (_enemies.Any(enemy => enemy.IsAlive && enemy.X == point.X && enemy.Y == point.Y))
            {
                continue;
            }

            var enemy = new Enemy(point.X, point.Y, type);
            _enemies.Add(enemy);
            _enemyAi[enemy] = CreateEnemyAi(enemy);
            _enemyLootKits[enemy] = CreateEnemyLootKit(enemy);
            spawned += 1;
        }

        return spawned;
    }

    private Enemy? GetEnemyAt(int x, int y)
    {
        return _enemies.FirstOrDefault(e => e.IsAlive && e.X == x && e.Y == y);
    }

    private RewardNode? GetUnclaimedRewardNodeAt(int x, int y)
    {
        return _rewardNodes.FirstOrDefault(node => node.X == x && node.Y == y && !_claimedRewardNodeIds.Contains(node.Id));
    }

    private void UpdateCurrentFloorZone()
    {
        if (_player == null)
        {
            _currentFloorZone = FloorMacroZone.EntryFrontier;
            return;
        }

        var resolved = ResolveFloorMacroZone(_player.X, _player.Y);
        if (resolved == _currentFloorZone)
        {
            return;
        }

        if (resolved != FloorMacroZone.SanctumRing)
        {
            _phase3SanctumLockNoticeShown = false;
        }

        _currentFloorZone = resolved;
        ShowRewardMessage($"Zone entered: {GetFloorZoneLabel(_currentFloorZone)}.", requireAcknowledge: false, visibleSeconds: 7);
    }

    private int TrySpawnPhase3RouteWaveForChoice()
    {
        if (_phase3RouteWaveSpawned)
        {
            return 0;
        }

        var spawned = _phase3RouteChoice switch
        {
            Phase3RouteChoice.UpperCatacombs => SpawnEnemyPack(Phase3UpperRouteEnemyPack),
            Phase3RouteChoice.LowerShrine => SpawnEnemyPack(Phase3LowerRouteEnemyPack),
            _ => 0
        };
        _phase3RouteWaveSpawned = true;
        return spawned;
    }

    private void TryGrantPhase3PreSanctumRouteReward()
    {
        if (_gameState != GameState.Playing) return;
        if (_phase3PreSanctumRewardGranted) return;
        if (!IsPhase3SanctumUnlockReady()) return;
        if (_player == null) return;

        _phase3PreSanctumRewardGranted = true;
        var rewardText = _phase3RouteChoice switch
        {
            Phase3RouteChoice.UpperCatacombs => GrantUpperRoutePreSanctumReward(),
            Phase3RouteChoice.LowerShrine => GrantLowerRoutePreSanctumReward(),
            _ => "No route boon was granted."
        };

        ShowRewardMessage($"Route objective complete. {rewardText}", requireAcknowledge: false, visibleSeconds: 12);
    }

    private string GrantUpperRoutePreSanctumReward()
    {
        _runMeleeBonus += 1;
        _runSpellBonus += 1;
        AddInventoryItemQuantity("mana_draught", 1);
        return "Upper route boon: +1 melee, +1 spell, +1 Mana Draught.";
    }

    private string GrantLowerRoutePreSanctumReward()
    {
        _runDefenseBonus += 1;
        _runFleeBonus += 3;
        AddInventoryItemQuantity("health_potion", 1);
        return "Lower route boon: +1 defense, +3% flee, +1 Health Potion.";
    }

    private void TrySpawnPhase3SanctumWaveIfNeeded()
    {
        if (_gameState != GameState.Playing) return;
        if (_phase3SanctumWaveSpawned) return;
        if (_currentFloorZone != FloorMacroZone.SanctumRing) return;
        if (!IsPhase3SanctumUnlockReady())
        {
            if (!_phase3SanctumLockNoticeShown)
            {
                var killsDone = Math.Min(Phase3SanctumUnlockRequiredKills, _phase3EnemiesDefeated);
                var rewardsDone = Math.Min(Phase3SanctumUnlockRequiredRewardNodes, GetClaimedPrimaryRewardCount());
                ShowRewardMessage(
                    $"Sanctum gate sealed. Required: kills {killsDone}/{Phase3SanctumUnlockRequiredKills}, reward nodes {rewardsDone}/{Phase3SanctumUnlockRequiredRewardNodes}, and route event resolved.",
                    requireAcknowledge: false,
                    visibleSeconds: 10);
                _phase3SanctumLockNoticeShown = true;
            }

            return;
        }

        if (_enemies.Any(enemy =>
                enemy.IsAlive &&
                string.Equals(ResolveEnemyTypeKey(enemy.Type), "goblin_general", StringComparison.Ordinal)))
        {
            _phase3SanctumWaveSpawned = true;
            return;
        }

        var sanctumSpawned = SpawnEnemyPack(Phase3SanctumEnemyPack);
        if (sanctumSpawned <= 0)
        {
            return;
        }

        _phase3SanctumWaveSpawned = true;
        if (_player == null)
        {
            return;
        }

        var routeOutcome = string.Empty;
        if (_phase3RouteChoice == Phase3RouteChoice.UpperCatacombs)
        {
            var reinforcements = SpawnEnemyPack(Phase3UpperSanctumReinforcementPack);
            routeOutcome = reinforcements > 0
                ? $"Upper route spillover adds {reinforcements} extra defenders."
                : "Upper route spillover keeps defender pressure high.";
        }
        else if (_phase3RouteChoice == Phase3RouteChoice.LowerShrine)
        {
            var firstBundle = GrantRewardSupplyLoot();
            var secondBundle = GrantRewardSupplyLoot();
            var hpGain = Math.Max(8, (int)Math.Ceiling(_player.MaxHp * 0.22));
            var mpGain = Math.Max(4, (int)Math.Ceiling(_player.MaxMana * 0.22));
            var beforeHp = _player.CurrentHp;
            var beforeMp = _player.CurrentMana;
            _player.CurrentHp = Math.Min(_player.MaxHp, _player.CurrentHp + hpGain);
            _player.CurrentMana = Math.Min(_player.MaxMana, _player.CurrentMana + mpGain);
            routeOutcome = $"Lower route cache delivers {firstBundle}, {secondBundle}, HP +{_player.CurrentHp - beforeHp}, MP +{_player.CurrentMana - beforeMp}.";
        }

        var summary = string.IsNullOrWhiteSpace(routeOutcome)
            ? $"Sanctum defenders mobilize ({sanctumSpawned} hostiles)."
            : $"Sanctum defenders mobilize ({sanctumSpawned} hostiles). {routeOutcome}";
        ShowRewardMessage(summary, requireAcknowledge: false, visibleSeconds: 12);
    }

    private bool TryOpenPhase3RouteForkChoice()
    {
        if (_player == null) return false;
        if (_gameState != GameState.Playing) return false;
        if (_activeRewardNode != null || _currentEnemy != null) return false;
        if (_rewardMessageRequiresAcknowledge) return false;
        if (_runRelic == RunRelic.None) return false;
        if (_phase3RouteChoice != Phase3RouteChoice.None) return false;
        if (_currentFloorZone != FloorMacroZone.BranchingDepths) return false;
        if (!IsPhase3RouteForkTile(_player.X, _player.Y)) return false;

        _activeRewardNode = new RewardNode
        {
            Id = Phase3RouteForkNodeId,
            X = _player.X,
            Y = _player.Y,
            Name = "Forked Descent",
            Description = "Choose one route into the deeper wings."
        };
        _selectedRewardOptionIndex = 0;
        ShowRewardMessage("You reach a major fork. Commit to one route.", requireAcknowledge: false, visibleSeconds: 10);
        _gameState = GameState.RewardChoice;
        return true;
    }

    private bool TryOpenPhase3RiskEvent()
    {
        if (_player == null) return false;
        if (_gameState != GameState.Playing) return false;
        if (_activeRewardNode != null || _currentEnemy != null) return false;
        if (_rewardMessageRequiresAcknowledge) return false;
        if (_phase3RouteChoice == Phase3RouteChoice.None || _phase3RiskEventResolved) return false;
        if (!IsRouteTargetEventZoneReached()) return false;

        _activeRewardNode = new RewardNode
        {
            Id = Phase3RiskEventNodeId,
            X = _player.X,
            Y = _player.Y,
            Name = "Oath Crucible",
            Description = "A volatile rite offers power at a cost."
        };
        _selectedRewardOptionIndex = 0;
        ShowRewardMessage("A volatile ritual chamber responds to your route choice.", requireAcknowledge: false, visibleSeconds: 10);
        _gameState = GameState.RewardChoice;
        return true;
    }

    private void TryOpenRewardNodeAtPlayer()
    {
        if (_player == null) return;
        var node = GetUnclaimedRewardNodeAt(_player.X, _player.Y);
        if (node == null) return;

        _activeRewardNode = node;
        _selectedRewardOptionIndex = 0;
        var discoveryMessage = IsArchetypeChoiceActive()
            ? $"{node.Name} found. Choose your archetype path."
            : IsRelicCheckpointActive()
                ? $"{node.Name} resonates. Milestone checkpoint: choose one relic."
            : IsMilestoneCheckpointActive()
                ? $"{node.Name} resonates again. Choose your milestone doctrine."
            : $"You discovered: {node.Name}.";
        ShowRewardMessage(discoveryMessage, requireAcknowledge: false, visibleSeconds: 10);
        _gameState = GameState.RewardChoice;
    }

    private void ApplyRewardChoice(int optionIndex)
    {
        if (_player == null || _activeRewardNode == null) return;
        var resultMessage = string.Empty;
        var leveledFromReward = false;
        var shouldMarkNodeClaimed = IsStandardRewardNodeActive();

        if (IsPhase3RouteChoiceActive())
        {
            ApplyPhase3RouteChoice(optionIndex, out resultMessage);
            shouldMarkNodeClaimed = false;
        }
        else if (IsPhase3RiskEventActive())
        {
            ApplyPhase3RiskEventChoice(optionIndex, out resultMessage);
            shouldMarkNodeClaimed = false;
        }
        else if (IsArchetypeChoiceActive())
        {
            ApplyArchetypeChoice(optionIndex, out resultMessage);
        }
        else if (IsRelicCheckpointActive())
        {
            ApplyRelicChoice(optionIndex, out resultMessage);
        }
        else if (IsMilestoneCheckpointActive())
        {
            ApplyMilestoneChoice(optionIndex, out resultMessage);
        }
        else
        {
            switch (optionIndex)
            {
                case 0:
                {
                    var hpGain = Math.Max(8, (int)Math.Ceiling(_player.MaxHp * 0.35));
                    var mpGain = Math.Max(4, (int)Math.Ceiling(_player.MaxMana * 0.35));
                    var beforeHp = _player.CurrentHp;
                    var beforeMp = _player.CurrentMana;
                    _player.CurrentHp = Math.Min(_player.MaxHp, _player.CurrentHp + hpGain);
                    _player.CurrentMana = Math.Min(_player.MaxMana, _player.CurrentMana + mpGain);
                    resultMessage = $"Recovered supplies: HP +{_player.CurrentHp - beforeHp}, MP +{_player.CurrentMana - beforeMp}.";
                    break;
                }
                case 1:
                {
                    var bonusXp = 120 + _player.Level * 20;
                    var lootGain = GrantRewardSupplyLoot();
                    leveledFromReward = _player.GainXp(bonusXp);
                    resultMessage = leveledFromReward
                        ? $"Battle trophy claimed: +{bonusXp} XP, {lootGain}. Level up ready."
                        : $"Battle trophy claimed: +{bonusXp} XP, {lootGain}.";
                    break;
                }
                case 2:
                {
                    if (_runArchetype == RunArchetype.Vanguard)
                    {
                        _runMeleeBonus += 1;
                        _runDefenseBonus += 1;
                        resultMessage = "Combat edge forged (Vanguard): +1 melee and +1 defense.";
                    }
                    else if (_runArchetype == RunArchetype.Arcanist)
                    {
                        _runSpellBonus += 1;
                        if (_player.MaxMana > 0)
                        {
                            var manaBurst = Math.Max(4, (int)Math.Ceiling(_player.MaxMana * 0.20));
                            var beforeMana = _player.CurrentMana;
                            _player.CurrentMana = Math.Min(_player.MaxMana, _player.CurrentMana + manaBurst);
                            resultMessage = $"Combat edge forged (Arcanist): +1 spell damage, MP +{_player.CurrentMana - beforeMana}.";
                        }
                        else
                        {
                            resultMessage = "Combat edge forged (Arcanist): +1 spell damage.";
                        }
                    }
                    else if (_runArchetype == RunArchetype.Skirmisher)
                    {
                        _runCritBonus += 1;
                        _runFleeBonus += 3;
                        _runMeleeBonus += 1;
                        resultMessage = "Combat edge forged (Skirmisher): +1 melee, +1% crit, +3% flee.";
                    }
                    else if ((GetClaimedPrimaryRewardCount() % 2) == 0)
                    {
                        _runMeleeBonus += 1;
                        _runSpellBonus += 1;
                        resultMessage = "Combat edge forged: +1 melee and +1 spell damage.";
                    }
                    else
                    {
                        _runDefenseBonus += 1;
                        _runCritBonus += 2;
                        _runFleeBonus += 3;
                        resultMessage = "Combat edge forged: +1 defense, +2% crit, +3% flee.";
                    }

                    break;
                }
                default:
                    resultMessage = "No reward selected.";
                    break;
            }
        }

        if (shouldMarkNodeClaimed)
        {
            _claimedRewardNodeIds.Add(_activeRewardNode.Id);
        }

        TryAutosaveCheckpoint("reward_node");
        _activeRewardNode = null;
        if (leveledFromReward)
        {
            ShowRewardMessage(resultMessage, requireAcknowledge: false, visibleSeconds: 12);
            _selectedStatIndex = 0;
            _gameState = GameState.LevelUp;
            return;
        }

        ShowRewardMessage(resultMessage, requireAcknowledge: true, visibleSeconds: 12);
        _gameState = GameState.Playing;
    }

    private void HandlePlayingInput()
    {
        if (_player == null) return;
        if (!_player.IsAlive)
        {
            HandlePlayerDeath();
            return;
        }

        if (_floorCleared)
        {
            _gameState = GameState.VictoryScreen;
            return;
        }

        if (Pressed(KeyEscape))
        {
            OpenPauseMenu(GameState.Playing);
            return;
        }

        if (Pressed(KeyC))
        {
            _characterSheetScroll = 0;
            _gameState = GameState.CharacterMenu;
            return;
        }

        if (_rewardMessageRequiresAcknowledge)
        {
            if (Pressed(KeyEnter) || Pressed(KeySpace))
            {
                ClearRewardMessage();
            }
            return;
        }

        if (TryHandleLootPickupInput())
        {
            return;
        }

        if (!TryGetMoveDelta(out var moveX, out var moveY))
        {
            return;
        }

        var newX = _player.X + moveX;
        var newY = _player.Y + moveY;

        if (IsWallOrSealed(newX, newY)) return;

        var enemy = GetEnemyAt(newX, newY);
        if (enemy != null)
        {
            StartCombat(enemy);
        }
        else
        {
            _player.X = newX;
            _player.Y = newY;
            _playerRunAnimUntil = Raylib.GetTime() + 0.18;
            UpdateCurrentFloorZone();
            TryGrantPhase3PreSanctumRouteReward();
            TrySpawnPhase3SanctumWaveIfNeeded();
            if (TryOpenPhase3RouteForkChoice())
            {
                return;
            }

            if (TryOpenPhase3RiskEvent())
            {
                return;
            }

            TryOpenRewardNodeAtPlayer();
        }
    }

    private bool TryHandleLootPickupInput()
    {
        if (_player == null) return false;

        if (_activePickupStatusUntil > 0 && Raylib.GetTime() >= _activePickupStatusUntil)
        {
            _activePickupStatus = string.Empty;
            _activePickupStatusUntil = -1;
        }

        var loot = GetGroundLootAt(_player.X, _player.Y);
        if (loot == null)
        {
            _activePickupLootId = null;
            _activePickupProgressSeconds = 0;
            return false;
        }

        if (!Raylib.IsKeyDown((KeyboardKey)KeyE))
        {
            _activePickupLootId = null;
            _activePickupProgressSeconds = 0;
            return false;
        }

        if (!string.Equals(_activePickupLootId, loot.Id, StringComparison.Ordinal))
        {
            _activePickupLootId = loot.Id;
            _activePickupProgressSeconds = 0;
        }

        if (IsLootPickupContested(_player.X, _player.Y))
        {
            _activePickupProgressSeconds = 0;
            if (Raylib.GetTime() >= _activePickupStatusUntil)
            {
                _activePickupStatus = "Pickup interrupted by nearby threat.";
                _activePickupStatusUntil = Raylib.GetTime() + PickupStatusVisibleSeconds;
            }

            return true;
        }

        _activePickupProgressSeconds = Math.Min(LootPickupHoldSeconds, _activePickupProgressSeconds + Raylib.GetFrameTime());
        if (_activePickupProgressSeconds < LootPickupHoldSeconds)
        {
            return true;
        }

        CollectGroundLoot(loot);
        _activePickupLootId = null;
        _activePickupProgressSeconds = 0;
        return true;
    }

    private bool IsLootPickupContested(int playerX, int playerY)
    {
        foreach (var enemy in _enemies.Where(e => e.IsAlive))
        {
            var manhattan = Math.Abs(enemy.X - playerX) + Math.Abs(enemy.Y - playerY);
            if (manhattan <= 1)
            {
                return true;
            }

            if (!_enemyAi.TryGetValue(enemy, out var ai))
            {
                continue;
            }

            if (ai.State == EnemyAiState.Chase && manhattan <= 3)
            {
                return true;
            }
        }

        return false;
    }

    private GroundLoot? GetGroundLootAt(int x, int y)
    {
        return _groundLoot.FirstOrDefault(loot => loot.X == x && loot.Y == y);
    }

    private void CollectGroundLoot(GroundLoot loot)
    {
        var gains = new List<string>();
        if (!string.IsNullOrWhiteSpace(loot.InventoryItemId) && loot.InventoryItemQuantity > 0)
        {
            AddInventoryItemQuantity(loot.InventoryItemId, loot.InventoryItemQuantity);
            var inventoryItem = GetInventoryItem(loot.InventoryItemId);
            var itemName = inventoryItem?.Name ?? loot.InventoryItemId;
            gains.Add($"+{loot.InventoryItemQuantity} {itemName}");
        }

        _groundLoot.RemoveAll(existing => string.Equals(existing.Id, loot.Id, StringComparison.Ordinal));
        _activePickupStatus = gains.Count > 0
            ? $"Loot secured: {loot.Name} ({string.Join(", ", gains)})."
            : $"Loot secured: {loot.Name}.";
        _activePickupStatusUntil = Raylib.GetTime() + PickupStatusVisibleSeconds;
        ShowRewardMessage(_activePickupStatus, requireAcknowledge: false, visibleSeconds: 8);
    }

    private void AddInventoryItemQuantity(string itemId, int amount)
    {
        if (amount <= 0) return;
        var item = GetInventoryItem(itemId);
        if (item == null) return;
        item.Quantity += amount;
    }

    private string GrantRewardSupplyLoot()
    {
        var rewardItemId = _rng.NextDouble() < 0.58
            ? "health_potion"
            : _rng.NextDouble() < 0.80
                ? "mana_draught"
                : "sharpening_oil";
        var quantity = rewardItemId == "health_potion" && _rng.NextDouble() < 0.28 ? 2 : 1;
        AddInventoryItemQuantity(rewardItemId, quantity);
        var itemName = GetInventoryItem(rewardItemId)?.Name ?? rewardItemId;
        return $"+{quantity} {itemName}";
    }

    private void UpdateWorldSimulation()
    {
        if (_gameState != GameState.Playing) return;
        if (_player == null || !_player.IsAlive) return;
        if (_rewardMessageRequiresAcknowledge) return;
        UpdateCurrentFloorZone();
        TryGrantPhase3PreSanctumRouteReward();
        TrySpawnPhase3SanctumWaveIfNeeded();

        UpdateEnemyAi();
    }

    private void UpdateEnemyAi()
    {
        if (_enemies.Count == 0) return;
        if (_enemyAi.Count == 0)
        {
            RebuildEnemyAi();
        }

        var now = Raylib.GetTime();
        foreach (var enemy in _enemies.Where(e => e.IsAlive))
        {
            if (!_enemyAi.TryGetValue(enemy, out var ai))
            {
                ai = CreateEnemyAi(enemy);
                _enemyAi[enemy] = ai;
            }

            if (ai.NextMoveAt > now)
            {
                continue;
            }

            UpdateEnemyAwareness(enemy, ai, now);
            if (_gameState != GameState.Playing || _player == null)
            {
                return;
            }

            switch (ai.State)
            {
                case EnemyAiState.Patrol:
                    TryPatrolStep(enemy, ai);
                    break;
                case EnemyAiState.Investigate:
                case EnemyAiState.Search:
                    TryMoveEnemyToward(enemy, ai, ai.LastKnownPlayerX, ai.LastKnownPlayerY);
                    break;
                case EnemyAiState.Chase:
                    TryMoveEnemyToward(enemy, ai, _player.X, _player.Y);
                    break;
                case EnemyAiState.Return:
                    TryMoveEnemyToward(enemy, ai, enemy.SpawnX, enemy.SpawnY);
                    break;
            }

            ai.NextMoveAt = now + GetEnemyStepDelay(ai.State);
            if (_gameState != GameState.Playing)
            {
                return;
            }
        }
    }

    private void UpdateEnemyAwareness(Enemy enemy, EnemyAiRuntime ai, double now)
    {
        if (_player == null) return;

        var seesPlayer = CanEnemySeePlayer(enemy, ai);
        if (seesPlayer)
        {
            ai.LastKnownPlayerX = _player.X;
            ai.LastKnownPlayerY = _player.Y;
            ai.LastSeenPlayerAt = now;
            ai.State = EnemyAiState.Chase;
            return;
        }

        switch (ai.State)
        {
            case EnemyAiState.Patrol:
                if (ai.LastSeenPlayerAt > 0 && now - ai.LastSeenPlayerAt <= 1.2)
                {
                    ai.State = EnemyAiState.Investigate;
                }
                break;
            case EnemyAiState.Investigate:
                if (enemy.X == ai.LastKnownPlayerX && enemy.Y == ai.LastKnownPlayerY)
                {
                    ai.State = EnemyAiState.Search;
                    ai.SearchEndsAt = now + GameTuning.EnemySearchDurationSeconds;
                }
                break;
            case EnemyAiState.Chase:
            {
                var distFromSpawn = Math.Abs(enemy.X - enemy.SpawnX) + Math.Abs(enemy.Y - enemy.SpawnY);
                var timedOut = ai.LastSeenPlayerAt > 0 && (now - ai.LastSeenPlayerAt) >= GameTuning.EnemyChaseTimeoutSeconds;
                var leashExceeded = distFromSpawn > GameTuning.EnemyLeashDistanceTiles;
                if (timedOut || leashExceeded)
                {
                    ai.State = EnemyAiState.Search;
                    ai.SearchEndsAt = now + GameTuning.EnemySearchDurationSeconds;
                }

                break;
            }
            case EnemyAiState.Search:
                if (ai.SearchEndsAt <= 0)
                {
                    ai.SearchEndsAt = now + GameTuning.EnemySearchDurationSeconds;
                }
                else if (now >= ai.SearchEndsAt)
                {
                    ai.State = EnemyAiState.Return;
                }
                break;
            case EnemyAiState.Return:
                if (enemy.X == enemy.SpawnX && enemy.Y == enemy.SpawnY)
                {
                    ai.State = EnemyAiState.Patrol;
                    ai.LastSeenPlayerAt = -1;
                }
                break;
        }
    }

    private EnemyAiRuntime CreateEnemyAi(Enemy enemy)
    {
        return new EnemyAiRuntime
        {
            State = EnemyAiState.Patrol,
            LastKnownPlayerX = enemy.X,
            LastKnownPlayerY = enemy.Y,
            PatrolDirectionIndex = Math.Abs(enemy.X + enemy.Y) % CardinalDirections.Length,
            FacingX = 1,
            FacingY = 0
        };
    }

    private void RebuildEnemyAi()
    {
        _enemyAi.Clear();
        foreach (var enemy in _enemies)
        {
            _enemyAi[enemy] = CreateEnemyAi(enemy);
        }
    }

    private EnemyLootKit CreateEnemyLootKit(Enemy enemy)
    {
        var template = RollLowLevelLootTemplate(enemy);
        var itemQty = 0;
        if (!string.IsNullOrWhiteSpace(template.InventoryItemId) && template.MaxItemQuantity > 0)
        {
            var minQty = Math.Max(1, template.MinItemQuantity);
            var maxQty = Math.Max(minQty, template.MaxItemQuantity);
            itemQty = _rng.Next(minQty, maxQty + 1);
        }

        return new EnemyLootKit
        {
            Name = template.Name,
            Rarity = template.Rarity,
            ItemId = template.InventoryItemId,
            ItemQuantity = itemQty,
            AttackBonus = 0,
            UsedConsumableThisFight = false
        };
    }

    private void RebuildEnemyLootKits()
    {
        _enemyLootKits.Clear();
        foreach (var enemy in _enemies)
        {
            _enemyLootKits[enemy] = CreateEnemyLootKit(enemy);
        }
    }

    private double GetEnemyStepDelay(EnemyAiState state)
    {
        return state switch
        {
            EnemyAiState.Chase => GameTuning.EnemyChaseStepSeconds,
            EnemyAiState.Investigate => GameTuning.EnemyInvestigateStepSeconds,
            EnemyAiState.Search => GameTuning.EnemySearchStepSeconds,
            EnemyAiState.Return => GameTuning.EnemyReturnStepSeconds,
            _ => GameTuning.EnemyPatrolStepSeconds
        };
    }

    private bool TryMoveEnemyToward(Enemy enemy, EnemyAiRuntime ai, int targetX, int targetY)
    {
        var bestDx = 0;
        var bestDy = 0;
        var bestScore = int.MaxValue;
        var found = false;

        foreach (var dir in CardinalDirections)
        {
            var nx = enemy.X + dir.X;
            var ny = enemy.Y + dir.Y;
            if (!CanEnemyStepInto(enemy, nx, ny))
            {
                continue;
            }

            var score = Math.Abs(targetX - nx) + Math.Abs(targetY - ny);
            score = score * 10 + _rng.Next(0, 4);
            if (score >= bestScore)
            {
                continue;
            }

            bestScore = score;
            bestDx = dir.X;
            bestDy = dir.Y;
            found = true;
        }

        if (!found)
        {
            return false;
        }

        MoveEnemy(enemy, ai, bestDx, bestDy);
        return true;
    }

    private bool TryPatrolStep(Enemy enemy, EnemyAiRuntime ai)
    {
        var start = ai.PatrolDirectionIndex;
        for (var i = 0; i < CardinalDirections.Length; i++)
        {
            var index = (start + i) % CardinalDirections.Length;
            var dir = CardinalDirections[index];
            var nx = enemy.X + dir.X;
            var ny = enemy.Y + dir.Y;

            if (Math.Abs(nx - enemy.SpawnX) > 4 || Math.Abs(ny - enemy.SpawnY) > 4)
            {
                continue;
            }

            if (!CanEnemyStepInto(enemy, nx, ny))
            {
                continue;
            }

            ai.PatrolDirectionIndex = (index + 1) % CardinalDirections.Length;
            MoveEnemy(enemy, ai, dir.X, dir.Y);
            return true;
        }

        ai.PatrolDirectionIndex = (ai.PatrolDirectionIndex + 1) % CardinalDirections.Length;
        return false;
    }

    private bool CanEnemyStepInto(Enemy enemy, int x, int y)
    {
        if (IsWallOrSealed(x, y))
        {
            return false;
        }

        if (_enemies.Any(other => !ReferenceEquals(other, enemy) && other.IsAlive && other.X == x && other.Y == y))
        {
            return false;
        }

        if (_player != null && _player.X == x && _player.Y == y)
        {
            StartCombat(enemy);
            return false;
        }

        return true;
    }

    private void MoveEnemy(Enemy enemy, EnemyAiRuntime ai, int dx, int dy)
    {
        enemy.X += dx;
        enemy.Y += dy;
        ai.FacingX = dx;
        ai.FacingY = dy;
    }

    private bool CanEnemySeePlayer(Enemy enemy, EnemyAiRuntime ai)
    {
        if (_player == null) return false;

        var dx = _player.X - enemy.X;
        var dy = _player.Y - enemy.Y;
        var sqrDistance = dx * dx + dy * dy;
        var visionRange = GameTuning.EnemyVisionRangeTiles;
        if (sqrDistance > visionRange * visionRange)
        {
            return false;
        }

        if (!HasLineOfSight(enemy.X, enemy.Y, _player.X, _player.Y))
        {
            return false;
        }

        var proximityRange = GameTuning.EnemyProximityDetectTiles;
        if (sqrDistance <= proximityRange * proximityRange)
        {
            return true;
        }

        var facingLen = MathF.Sqrt(ai.FacingX * ai.FacingX + ai.FacingY * ai.FacingY);
        var fx = facingLen > 0.001f ? ai.FacingX / facingLen : 1f;
        var fy = facingLen > 0.001f ? ai.FacingY / facingLen : 0f;

        var distance = MathF.Sqrt(sqrDistance);
        var tx = dx / distance;
        var ty = dy / distance;
        var dot = fx * tx + fy * ty;
        var minDot = MathF.Cos(MathF.PI * GameTuning.EnemyFovDegrees / 360f);
        return dot >= minDot;
    }

    private bool HasLineOfSight(int fromX, int fromY, int toX, int toY)
    {
        var x = fromX;
        var y = fromY;
        var dx = Math.Abs(toX - fromX);
        var dy = Math.Abs(toY - fromY);
        var sx = fromX < toX ? 1 : -1;
        var sy = fromY < toY ? 1 : -1;
        var err = dx - dy;

        while (x != toX || y != toY)
        {
            var e2 = err * 2;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }

            if (x == toX && y == toY)
            {
                break;
            }

            if (IsWallOrSealed(x, y))
            {
                return false;
            }
        }

        return true;
    }

    private bool TryGetMoveDelta(out int dx, out int dy)
    {
        dx = 0;
        dy = 0;
        var now = Raylib.GetTime();

        if (TryGetDirectionalPressed(out dx, out dy))
        {
            _nextMoveAt = now + MovementInitialRepeatDelaySeconds;
            return true;
        }

        if (!TryGetDirectionalHeld(out dx, out dy))
        {
            _nextMoveAt = -1;
            return false;
        }

        if (_nextMoveAt < 0)
        {
            _nextMoveAt = now + MovementInitialRepeatDelaySeconds;
            return false;
        }

        if (now < _nextMoveAt)
        {
            return false;
        }

        _nextMoveAt = now + MovementRepeatIntervalSeconds;
        return true;
    }

    private bool TryGetDirectionalPressed(out int dx, out int dy)
    {
        dx = 0;
        dy = 0;

        if (Pressed(KeyUp) || Pressed(KeyW))
        {
            dy = -1;
            return true;
        }

        if (Pressed(KeyDown) || Pressed(KeyS))
        {
            dy = 1;
            return true;
        }

        if (Pressed(KeyLeft) || Pressed(KeyA))
        {
            dx = -1;
            return true;
        }

        if (Pressed(KeyRight) || Pressed(KeyD))
        {
            dx = 1;
            return true;
        }

        return false;
    }

    private static bool TryGetDirectionalHeld(out int dx, out int dy)
    {
        dx = 0;
        dy = 0;

        if (Raylib.IsKeyDown((KeyboardKey)KeyUp) || Raylib.IsKeyDown((KeyboardKey)KeyW))
        {
            dy = -1;
            return true;
        }

        if (Raylib.IsKeyDown((KeyboardKey)KeyDown) || Raylib.IsKeyDown((KeyboardKey)KeyS))
        {
            dy = 1;
            return true;
        }

        if (Raylib.IsKeyDown((KeyboardKey)KeyLeft) || Raylib.IsKeyDown((KeyboardKey)KeyA))
        {
            dx = -1;
            return true;
        }

        if (Raylib.IsKeyDown((KeyboardKey)KeyRight) || Raylib.IsKeyDown((KeyboardKey)KeyD))
        {
            dx = 1;
            return true;
        }

        return false;
    }

    private void StartCombat(Enemy enemy)
    {
        if (_player == null || !_player.IsAlive) return;
        if (!enemy.IsAlive) return;

        _nextMoveAt = -1;
        _activePickupLootId = null;
        _activePickupProgressSeconds = 0;
        _currentEnemy = enemy;
        if (!_enemyLootKits.TryGetValue(enemy, out var enemyLoot))
        {
            enemyLoot = CreateEnemyLootKit(enemy);
            _enemyLootKits[enemy] = enemyLoot;
        }

        enemyLoot.UsedConsumableThisFight = false;
        _selectedActionIndex = 0;
        _selectedCombatSkillIndex = 0;
        _selectedSpellIndex = 0;
        _selectedCombatItemIndex = 0;
        _combatItemMenuOffset = 0;
        _selectedEncounterTargetIndex = -1;
        _enemyPoisoned = 0;
        ResetEncounterContext();
        BeginEncounterFromSeed(enemy);
        SyncEncounterTargetSelection(preferCurrentEnemy: true);
        ResetRelicCombatTriggers();
        ResetMilestoneCombatTriggers();
        _resolvingEnemyDeath = false;
        _combatLog.Clear();
        PushCombatLog($"A {enemy.Type.Name} blocks your path!");
        if (_encounterEnemies.Count > 1)
        {
            PushCombatLog($"Encounter formed: {_encounterEnemies.Count} hostiles.");
        }

        if (_encounterTurnOrder.Count > 0)
        {
            var opener = _encounterTurnOrder[_encounterTurnIndex];
            PushCombatLog($"Initiative opens with: {opener.Id}.");
        }

        if (_player != null)
        {
            PushCombatLog($"{_player.CharacterClass.Name}: {GetClassCombatTag(_player.CharacterClass.Name)}");
            PushCombatLog($"HP {_player.CurrentHp}/{_player.MaxHp}  MP {_player.CurrentMana}/{_player.MaxMana}");
            _player.HasUsedSecondWind = false;
            _warCryAvailable = _player.HasSkill("war_cry");
        }
        else
        {
            _warCryAvailable = false;
            _combatMoveModeActive = false;
            _combatMovePointsMax = 0;
            _combatMovePointsRemaining = 0;
        }

        _gameState = GameState.Combat;
        if (_player != null && _encounterTurnOrder.Count > 0)
        {
            var openingSlot = _encounterTurnOrder[_encounterTurnIndex];
            if (openingSlot.Kind == EncounterCombatantKind.Player)
            {
                SetEncounterTurnToPlayer();
                BeginPlayerCombatTurn();
            }
            else
            {
                ResolveEnemyTurnsUntilPlayerTurn();
            }
        }

        TryAutosaveCheckpoint("combat_start");
    }

    private List<string> GetCombatActions()
    {
        var actions = new List<string>();
        if (_player != null)
        {
            if (_combatMovePointsRemaining > 0)
            {
                actions.Add("Move");
            }

            actions.Add("Attack");
            if (GetCombatSkills().Count > 0) actions.Add("Skills");
            if (_player.GetKnownSpells().Count > 0) actions.Add("Spells");
            if (GetCombatConsumables().Count > 0) actions.Add("Items");
        }
        else
        {
            actions.Add("Attack");
        }

        actions.Add("Flee");
        return actions;
    }

    private void HandleCombatInput()
    {
        if (_player != null && !_player.IsAlive)
        {
            HandlePlayerDeath();
            return;
        }

        SyncEncounterTargetSelection(preferCurrentEnemy: true);
        if (_player == null || _currentEnemy == null || _resolvingEnemyDeath) return;
        if (!_currentEnemy.IsAlive)
        {
            CheckEnemyDeath();
            return;
        }

        if (_combatMoveModeActive)
        {
            HandleCombatMoveInput();
            return;
        }

        SetEncounterTurnToPlayer();

        if (Pressed(KeyEscape))
        {
            OpenPauseMenu(GameState.Combat);
            return;
        }

        if (Pressed(KeyLeft))
        {
            CycleEncounterTarget(-1);
            return;
        }

        if (Pressed(KeyRight))
        {
            CycleEncounterTarget(1);
            return;
        }

        var actions = GetCombatActions();

        if (Pressed(KeyUp))
        {
            _selectedActionIndex = (_selectedActionIndex - 1 + actions.Count) % actions.Count;
            return;
        }

        if (Pressed(KeyDown))
        {
            _selectedActionIndex = (_selectedActionIndex + 1) % actions.Count;
            return;
        }

        if (!Pressed(KeyEnter)) return;

        _selectedActionIndex = Math.Min(_selectedActionIndex, actions.Count - 1);
        var action = actions[_selectedActionIndex];
        switch (action)
        {
            case "Move":
                BeginCombatMoveMode();
                break;
            case "Attack":
                DoPlayerAttack();
                break;
            case "Skills":
                OpenCombatSkillMenu();
                break;
            case "Spells":
                OpenCombatSpellMenu();
                break;
            case "Items":
                OpenCombatItemMenu();
                break;
            case "Flee":
                DoFlee();
                break;
        }
    }

    private List<Enemy> GetAliveEncounterEnemies()
    {
        return _encounterEnemies
            .Where(enemy => enemy.IsAlive)
            .ToList();
    }

    private void SyncEncounterTargetSelection(bool preferCurrentEnemy = false)
    {
        var aliveEnemies = GetAliveEncounterEnemies();
        if (aliveEnemies.Count == 0)
        {
            _selectedEncounterTargetIndex = -1;
            _currentEnemy = null;
            return;
        }

        if (preferCurrentEnemy && _currentEnemy != null)
        {
            var preferredIndex = aliveEnemies.FindIndex(enemy => ReferenceEquals(enemy, _currentEnemy));
            if (preferredIndex >= 0)
            {
                _selectedEncounterTargetIndex = preferredIndex;
                _currentEnemy = aliveEnemies[_selectedEncounterTargetIndex];
                return;
            }
        }

        if (_selectedEncounterTargetIndex >= 0 && _selectedEncounterTargetIndex < aliveEnemies.Count)
        {
            _currentEnemy = aliveEnemies[_selectedEncounterTargetIndex];
            return;
        }

        if (_player != null)
        {
            var nearest = aliveEnemies
                .Select((enemy, index) => new
                {
                    Enemy = enemy,
                    Index = index,
                    Distance = EncounterTargetingRules.GetTileDistance(_player.X, _player.Y, enemy.X, enemy.Y)
                })
                .OrderBy(candidate => candidate.Distance)
                .ThenBy(candidate => candidate.Enemy.CurrentHp)
                .First();
            _selectedEncounterTargetIndex = nearest.Index;
        }
        else
        {
            _selectedEncounterTargetIndex = 0;
        }

        _currentEnemy = aliveEnemies[_selectedEncounterTargetIndex];
    }

    private void CycleEncounterTarget(int direction)
    {
        var aliveEnemies = GetAliveEncounterEnemies();
        if (aliveEnemies.Count <= 1)
        {
            SyncEncounterTargetSelection(preferCurrentEnemy: true);
            return;
        }

        if (_selectedEncounterTargetIndex < 0 || _selectedEncounterTargetIndex >= aliveEnemies.Count)
        {
            SyncEncounterTargetSelection(preferCurrentEnemy: true);
        }

        if (_selectedEncounterTargetIndex < 0 || _selectedEncounterTargetIndex >= aliveEnemies.Count)
        {
            return;
        }

        _selectedEncounterTargetIndex = EncounterSpellTargetingRules.CycleTargetIndex(
            _selectedEncounterTargetIndex,
            aliveEnemies.Count,
            direction);
        if (_selectedEncounterTargetIndex < 0 || _selectedEncounterTargetIndex >= aliveEnemies.Count)
        {
            return;
        }

        _currentEnemy = aliveEnemies[_selectedEncounterTargetIndex];
    }

    private int GetSpellTargetRangeTiles(SpellDefinition spell)
    {
        return EncounterSpellTargetingRangePolicy.ResolveSpellRangeTiles(spell);
    }

    private int GetEnemyAttackRangeTiles(Enemy enemy)
    {
        var enemyKey = ResolveEnemyTypeKey(enemy.Type);
        if (string.Equals(enemyKey, "goblin_slinger", StringComparison.Ordinal))
        {
            return CombatRangedEnemyRangeTiles;
        }

        return CombatMeleeRangeTiles;
    }

    private int GetEnemyCombatMoveBudget(Enemy enemy)
    {
        var enemyKey = ResolveEnemyTypeKey(enemy.Type);
        return enemyKey switch
        {
            "goblin_skirmisher" => EnemySkirmisherMoveBudgetTiles,
            "warg" => EnemySkirmisherMoveBudgetTiles,
            _ => EnemyDefaultMoveBudgetTiles
        };
    }

    private bool CanEnemyTraverseCombatTile(Enemy movingEnemy, int x, int y)
    {
        if (_player == null)
        {
            return false;
        }

        if (IsWallOrSealed(x, y))
        {
            return false;
        }

        // Enemy should not enter player tile during tactical movement.
        if (_player.X == x && _player.Y == y)
        {
            return false;
        }

        if (_enemies.Any(enemy =>
                enemy.IsAlive &&
                !ReferenceEquals(enemy, movingEnemy) &&
                enemy.X == x &&
                enemy.Y == y))
        {
            return false;
        }

        return true;
    }

    private bool TryExecuteEnemyTacticalMovement(Enemy enemy, out int movedTiles)
    {
        movedTiles = 0;
        if (_player == null || !enemy.IsAlive)
        {
            return false;
        }

        var moveBudget = GetEnemyCombatMoveBudget(enemy);
        if (moveBudget <= 0)
        {
            return false;
        }

        var moveDecision = EncounterEnemyTactics.DecideMoveTowardTarget(
            enemy.X,
            enemy.Y,
            _player.X,
            _player.Y,
            moveBudget,
            (x, y) => CanEnemyTraverseCombatTile(enemy, x, y));
        if (!moveDecision.ShouldMove)
        {
            return false;
        }

        var destination = moveDecision.Destination;
        if (destination.X == enemy.X && destination.Y == enemy.Y)
        {
            return false;
        }

        enemy.X = destination.X;
        enemy.Y = destination.Y;
        movedTiles = moveDecision.Steps.Count;
        PushCombatLog($"{enemy.Type.Name} repositions {movedTiles} tile{(movedTiles == 1 ? string.Empty : "s")}.");
        return true;
    }

    private EncounterTargetValidation ValidateCurrentEnemyTargetForMelee()
    {
        if (_player == null || _currentEnemy == null)
        {
            return new EncounterTargetValidation(
                IsLegal: false,
                DistanceTiles: 0,
                MaxRangeTiles: CombatMeleeRangeTiles,
                InRange: false,
                HasLineOfSight: false,
                TargetAlive: false);
        }

        return EncounterTargetingRules.Validate(
            _player.X,
            _player.Y,
            _currentEnemy.X,
            _currentEnemy.Y,
            _currentEnemy.IsAlive,
            CombatMeleeRangeTiles,
            requiresLineOfSight: true,
            HasLineOfSight);
    }

    private EncounterTargetValidation ValidateCurrentEnemyTargetForSpell(SpellDefinition spell)
    {
        if (_player == null || _currentEnemy == null)
        {
            return new EncounterTargetValidation(
                IsLegal: false,
                DistanceTiles: 0,
                MaxRangeTiles: GetSpellTargetRangeTiles(spell),
                InRange: false,
                HasLineOfSight: false,
                TargetAlive: false);
        }

        return EncounterSpellTargetingRules.ValidateSpellTarget(
            spell,
            _player.X,
            _player.Y,
            _currentEnemy.X,
            _currentEnemy.Y,
            _currentEnemy.IsAlive,
            requiresLineOfSight: true,
            HasLineOfSight);
    }

    private EncounterTargetValidation ValidateEnemyAttackTarget(Enemy enemy)
    {
        if (_player == null || !enemy.IsAlive)
        {
            return new EncounterTargetValidation(
                IsLegal: false,
                DistanceTiles: 0,
                MaxRangeTiles: GetEnemyAttackRangeTiles(enemy),
                InRange: false,
                HasLineOfSight: false,
                TargetAlive: false);
        }

        return EncounterTargetingRules.Validate(
            enemy.X,
            enemy.Y,
            _player.X,
            _player.Y,
            _player.IsAlive,
            GetEnemyAttackRangeTiles(enemy),
            requiresLineOfSight: true,
            HasLineOfSight);
    }

    private void BeginCombatMoveMode()
    {
        if (_player == null)
        {
            return;
        }

        if (_combatMovePointsRemaining <= 0)
        {
            PushCombatLog("No movement points remaining this turn.");
            return;
        }

        _combatMoveModeActive = true;
        _nextMoveAt = -1;
        PushCombatLog($"Movement mode: {_combatMovePointsRemaining} tiles available.");
    }

    private bool CanTraverseCombatTile(int x, int y)
    {
        if (_player == null)
        {
            return false;
        }

        if (IsWallOrSealed(x, y))
        {
            return false;
        }

        if (_player.X == x && _player.Y == y)
        {
            return true;
        }

        if (_enemies.Any(enemy => enemy.IsAlive && enemy.X == x && enemy.Y == y))
        {
            return false;
        }

        return true;
    }

    private IReadOnlyCollection<(int X, int Y)> BuildPlayerReachableCombatTiles()
    {
        if (_player == null || _combatMovePointsRemaining <= 0)
        {
            return Array.Empty<(int X, int Y)>();
        }

        return EncounterMovementRules.BuildReachableTiles(
            _player.X,
            _player.Y,
            _combatMovePointsRemaining,
            CanTraverseCombatTile);
    }

    private void HandleCombatMoveInput()
    {
        if (_player == null)
        {
            _combatMoveModeActive = false;
            return;
        }

        if (Pressed(KeyEscape) || Pressed(KeyEnter))
        {
            _combatMoveModeActive = false;
            _nextMoveAt = -1;
            return;
        }

        if (_combatMovePointsRemaining <= 0)
        {
            _combatMoveModeActive = false;
            _nextMoveAt = -1;
            return;
        }

        if (!TryGetMoveDelta(out var moveX, out var moveY))
        {
            return;
        }

        var targetX = _player.X + moveX;
        var targetY = _player.Y + moveY;
        if (!CanTraverseCombatTile(targetX, targetY))
        {
            return;
        }

        _player.X = targetX;
        _player.Y = targetY;
        _playerRunAnimUntil = Raylib.GetTime() + 0.14;
        _combatMovePointsRemaining = Math.Max(0, _combatMovePointsRemaining - 1);
        SyncEncounterTargetSelection(preferCurrentEnemy: true);

        if (_combatMovePointsRemaining <= 0)
        {
            _combatMoveModeActive = false;
            _nextMoveAt = -1;
            PushCombatLog("Movement exhausted for this turn.");
        }
    }

    private List<string> GetCombatSkills()
    {
        var skills = new List<string>();
        if (_player == null) return skills;

        if (_player.HasSkill("second_wind") && !_player.HasUsedSecondWind)
        {
            skills.Add("second_wind");
        }

        if (_player.HasSkill("mana_shield") && _player.CurrentMana >= 3)
        {
            skills.Add("mana_shield");
        }

        return skills;
    }

    private List<InventoryItem> GetCombatConsumables()
    {
        return _inventoryItems
            .Where(item => item.Kind == InventoryItemKind.Consumable && item.Quantity > 0)
            .ToList();
    }

    private void OpenCombatSkillMenu()
    {
        if (_player == null) return;
        if (GetCombatSkills().Count == 0) return;
        ClearPendingCombatSpell();
        _selectedCombatSkillIndex = 0;
        _gameState = GameState.CombatSkillMenu;
    }

    private void HandleCombatSkillInput()
    {
        if (_player != null && !_player.IsAlive)
        {
            HandlePlayerDeath();
            return;
        }

        SyncEncounterTargetSelection(preferCurrentEnemy: true);
        if (_player == null || _currentEnemy == null)
        {
            _gameState = GameState.Combat;
            return;
        }

        var skills = GetCombatSkills();
        if (skills.Count == 0)
        {
            _gameState = GameState.Combat;
            return;
        }

        if (Pressed(KeyEscape))
        {
            _gameState = GameState.Combat;
            return;
        }

        if (Pressed(KeyUp))
        {
            _selectedCombatSkillIndex = (_selectedCombatSkillIndex - 1 + skills.Count) % skills.Count;
            return;
        }

        if (Pressed(KeyDown))
        {
            _selectedCombatSkillIndex = (_selectedCombatSkillIndex + 1) % skills.Count;
            return;
        }

        if (!Pressed(KeyEnter)) return;

        var chosenSkillId = skills[Math.Min(_selectedCombatSkillIndex, skills.Count - 1)];
        _gameState = GameState.Combat;
        CastCombatSkill(chosenSkillId);
    }

    private void CastCombatSkill(string skillId)
    {
        if (_player == null || _currentEnemy == null || _gameState == GameState.DeathScreen)
        {
            _gameState = GameState.Combat;
            return;
        }

        switch (skillId)
        {
            case "second_wind":
                DoSecondWind();
                return;
            case "mana_shield":
                DoManaShield();
                return;
            default:
                PushCombatLog("That skill is not available.");
                _gameState = GameState.Combat;
                return;
        }
    }

    private void OpenCombatSpellMenu()
    {
        if (_player == null) return;
        if (_player.GetKnownSpells().Count == 0) return;
        ClearPendingCombatSpell();
        _selectedSpellIndex = 0;
        _spellMenuOffset = 0;
        _gameState = GameState.CombatSpellMenu;
    }

    private void OpenCombatItemMenu()
    {
        if (_player == null) return;
        if (GetCombatConsumables().Count == 0) return;
        ClearPendingCombatSpell();
        _selectedCombatItemIndex = 0;
        _combatItemMenuOffset = 0;
        _gameState = GameState.CombatItemMenu;
    }

    private void ClearPendingCombatSpell()
    {
        _pendingCombatSpellId = string.Empty;
    }

    private bool TryGetPendingCombatSpell(out SpellDefinition spell)
    {
        spell = null!;
        if (_player == null || string.IsNullOrWhiteSpace(_pendingCombatSpellId))
        {
            return false;
        }

        var pendingSpell = _player
            .GetKnownSpells()
            .FirstOrDefault(candidate =>
                string.Equals(candidate.Id, _pendingCombatSpellId, StringComparison.Ordinal));
        if (pendingSpell == null)
        {
            ClearPendingCombatSpell();
            return false;
        }

        spell = pendingSpell;
        return true;
    }

    private void HandleCombatSpellInput()
    {
        if (_player != null && !_player.IsAlive)
        {
            HandlePlayerDeath();
            return;
        }

        SyncEncounterTargetSelection(preferCurrentEnemy: true);
        if (_player == null || _currentEnemy == null)
        {
            ClearPendingCombatSpell();
            _gameState = GameState.Combat;
            return;
        }

        var spells = _player.GetKnownSpells();
        if (spells.Count == 0)
        {
            ClearPendingCombatSpell();
            _gameState = GameState.Combat;
            return;
        }

        if (Pressed(KeyEscape))
        {
            ClearPendingCombatSpell();
            _gameState = GameState.Combat;
            return;
        }

        if (Pressed(KeyUp))
        {
            _selectedSpellIndex = (_selectedSpellIndex - 1 + spells.Count) % spells.Count;
            EnsureSpellSelectionVisible(spells.Count);
            return;
        }

        if (Pressed(KeyDown))
        {
            _selectedSpellIndex = (_selectedSpellIndex + 1) % spells.Count;
            EnsureSpellSelectionVisible(spells.Count);
            return;
        }

        if (!Pressed(KeyEnter)) return;

        var chosenSpell = spells[Math.Min(_selectedSpellIndex, spells.Count - 1)];
        _pendingCombatSpellId = chosenSpell.Id;
        _gameState = GameState.CombatSpellTargeting;
    }

    private void HandleCombatSpellTargetingInput()
    {
        if (_player != null && !_player.IsAlive)
        {
            HandlePlayerDeath();
            return;
        }

        SyncEncounterTargetSelection(preferCurrentEnemy: true);
        if (_player == null || _currentEnemy == null)
        {
            ClearPendingCombatSpell();
            _gameState = GameState.Combat;
            return;
        }

        if (!TryGetPendingCombatSpell(out var pendingSpell))
        {
            _gameState = GameState.CombatSpellMenu;
            return;
        }

        if (Pressed(KeyEscape))
        {
            ClearPendingCombatSpell();
            _gameState = GameState.CombatSpellMenu;
            return;
        }

        if (Pressed(KeyLeft))
        {
            CycleEncounterTarget(-1);
            return;
        }

        if (Pressed(KeyRight))
        {
            CycleEncounterTarget(1);
            return;
        }

        if (!Pressed(KeyEnter)) return;

        var spellValidation = ValidateCurrentEnemyTargetForSpell(pendingSpell);
        if (!spellValidation.IsLegal)
        {
            PushCombatLog($"{pendingSpell.Name} blocked: {spellValidation.BuildBlockedReason()}");
            return;
        }

        ClearPendingCombatSpell();
        _gameState = GameState.Combat;
        CastCombatSpell(pendingSpell);
    }

    private void HandleCombatItemInput()
    {
        if (_player != null && !_player.IsAlive)
        {
            HandlePlayerDeath();
            return;
        }

        SyncEncounterTargetSelection(preferCurrentEnemy: true);
        if (_player == null || _currentEnemy == null)
        {
            _gameState = GameState.Combat;
            return;
        }

        var items = GetCombatConsumables();
        if (items.Count == 0)
        {
            PushCombatLog("No consumables left for combat use.");
            _gameState = GameState.Combat;
            return;
        }

        if (Pressed(KeyEscape))
        {
            _gameState = GameState.Combat;
            return;
        }

        if (Pressed(KeyUp))
        {
            _selectedCombatItemIndex = (_selectedCombatItemIndex - 1 + items.Count) % items.Count;
            EnsureCombatItemSelectionVisible(items.Count);
            return;
        }

        if (Pressed(KeyDown))
        {
            _selectedCombatItemIndex = (_selectedCombatItemIndex + 1) % items.Count;
            EnsureCombatItemSelectionVisible(items.Count);
            return;
        }

        if (!Pressed(KeyEnter)) return;

        var chosenItem = items[Math.Min(_selectedCombatItemIndex, items.Count - 1)];
        _gameState = GameState.Combat;
        if (!TryUseCombatConsumable(chosenItem, out var resultMessage, out var turnConsumed))
        {
            PushCombatLog(resultMessage);
            return;
        }

        PushCombatLog(resultMessage);
        if (CheckEnemyDeath()) return;
        if (turnConsumed) DoEnemyAttack();
    }

    private bool TryUseCombatConsumable(InventoryItem item, out string resultMessage, out bool turnConsumed)
    {
        resultMessage = "Item could not be used.";
        turnConsumed = false;

        if (_player == null)
        {
            resultMessage = "No active player found.";
            return false;
        }

        if (item.Kind != InventoryItemKind.Consumable)
        {
            resultMessage = $"{item.Name} cannot be used during combat.";
            return false;
        }

        if (item.Quantity <= 0)
        {
            resultMessage = $"{item.Name} is depleted.";
            return false;
        }

        switch (item.Id)
        {
            case "health_potion":
            {
                if (_player.CurrentHp >= _player.MaxHp)
                {
                    resultMessage = "HP is already full.";
                    return false;
                }

                var healAmount = Math.Max(8, (int)Math.Ceiling(_player.MaxHp * 0.35));
                var before = _player.CurrentHp;
                _player.CurrentHp = Math.Min(_player.MaxHp, _player.CurrentHp + healAmount);
                var gained = _player.CurrentHp - before;
                if (gained <= 0)
                {
                    resultMessage = "Health Potion had no effect.";
                    return false;
                }

                item.Quantity -= 1;
                resultMessage = $"Used {item.Name}: HP +{gained} ({item.Quantity} left).";
                turnConsumed = true;
                return true;
            }
            case "mana_draught":
            {
                if (_player.MaxMana <= 0)
                {
                    resultMessage = $"{_player.CharacterClass.Name} has no mana pool.";
                    return false;
                }

                if (_player.CurrentMana >= _player.MaxMana)
                {
                    resultMessage = "MP is already full.";
                    return false;
                }

                var restoreAmount = Math.Max(4, (int)Math.Ceiling(_player.MaxMana * 0.35));
                var before = _player.CurrentMana;
                _player.CurrentMana = Math.Min(_player.MaxMana, _player.CurrentMana + restoreAmount);
                var gained = _player.CurrentMana - before;
                if (gained <= 0)
                {
                    resultMessage = "Mana Draught had no effect.";
                    return false;
                }

                item.Quantity -= 1;
                resultMessage = $"Used {item.Name}: MP +{gained} ({item.Quantity} left).";
                turnConsumed = true;
                return true;
            }
            case "sharpening_oil":
                item.Quantity -= 1;
                _runMeleeBonus += 1;
                resultMessage = "Sharpening Oil applied: +1 run melee damage.";
                turnConsumed = true;
                return true;
            default:
                resultMessage = $"{item.Name} has no combat effect configured.";
                return false;
        }
    }

    private void CastCombatSpell(SpellDefinition spell)
    {
        if (_player == null || _currentEnemy == null || _gameState == GameState.DeathScreen) return;
        var targetValidation = ValidateCurrentEnemyTargetForSpell(spell);
        if (!targetValidation.IsLegal)
        {
            PushCombatLog($"{spell.Name} blocked: {targetValidation.BuildBlockedReason()}");
            return;
        }

        var milestoneSlotWaive = false;
        if (spell.RequiresSlot)
        {
            if (TryConsumeMilestoneArcSlotWaive())
            {
                milestoneSlotWaive = true;
            }
            else if (!_player.TryConsumeSpellSlot(spell.SpellLevel))
            {
                PushCombatLog($"{spell.Name}: no L{spell.SpellLevel} slots left.");
                return;
            }
        }

        var shouldCounterAttack = !spell.SuppressCounterAttack;
        var (damage, rawDamage, armorMitigation, statPower) = CalcSpellDamage(
            spell.ScalingStat,
            spell.BaseDamage + _player.SpellDamageBonus + GetClassSpellDamageBonus(_player) + _runSpellBonus + GetConditionSpellModifier(),
            spell.Variance,
            spell.ArmorBypass + _player.SpellArmorBypassBonus);
        var relicBurst = ConsumeRelicSpellBurstDamage();
        var milestoneArcBonus = milestoneSlotWaive ? GetArcDoctrineWaiveBonusDamage() : 0;

        var totalDamage = damage + relicBurst + milestoneArcBonus;
        _currentEnemy.CurrentHp = Math.Max(0, _currentEnemy.CurrentHp - totalDamage);
        var tierLabel = spell.IsCantrip ? "Cantrip" : $"L{spell.SpellLevel}";
        PushCombatLog($"{spell.Name} ({tierLabel}) hits for {totalDamage} {spell.DamageTag}.");
        PushCombatLog($"Raw {rawDamage} (stat +{statPower}) - armor {armorMitigation}.");
        if (relicBurst > 0)
        {
            PushCombatLog($"Astral Conduit amplifies the cast (+{relicBurst}).");
        }
        if (milestoneSlotWaive)
        {
            PushCombatLog($"Arc Doctrine preserves this slot (+{milestoneArcBonus} damage). Charges left: {_milestoneArcChargesThisCombat}.");
        }
        PushCombatLog($"{_currentEnemy.Type.Name} HP {_currentEnemy.CurrentHp}/{_currentEnemy.Type.MaxHp}.");
        if (spell.RequiresSlot && !milestoneSlotWaive)
        {
            PushCombatLog($"L{spell.SpellLevel} slots {_player.GetSpellSlots(spell.SpellLevel)}/{_player.GetSpellSlotsMax(spell.SpellLevel)}.");
        }

        if (spell.SuppressCounterAttack)
        {
            PushCombatLog($"{_currentEnemy.Type.Name} is disrupted and cannot counter this turn.");
        }

        if (CheckEnemyDeath()) return;
        if (shouldCounterAttack)
        {
            DoEnemyAttack();
        }
        else
        {
            DoEnemyAttack(skipFirstEnemyTurn: true);
        }
    }

    private (int damage, int rawDamage, int armorMitigation, int statPower) CalcSpellDamage(StatName scaleStat, int baseDamage, int variance, int armorBypass)
    {
        if (_player == null || _currentEnemy == null) return (0, 0, 0, 0);
        var statPower = Math.Max(0, _player.Mod(scaleStat) * 2);
        var varianceRoll = variance > 0 ? _rng.Next(variance + 1) : 0;
        var raw = CombatMath.CalculateSpellRawDamage(baseDamage, statPower, varianceRoll);
        var effectiveArmor = Math.Max(0, _currentEnemy.Type.Defense - armorBypass);
        var finalDamage = CombatMath.CalculateFinalDamage(raw, _currentEnemy.Type.Defense, armorBypass);
        return (finalDamage, raw, effectiveArmor, statPower);
    }

    private void EnsureSpellSelectionVisible(int spellCount)
    {
        if (spellCount <= SpellMenuVisibleCount)
        {
            _spellMenuOffset = 0;
            return;
        }

        if (_selectedSpellIndex < _spellMenuOffset)
        {
            _spellMenuOffset = _selectedSpellIndex;
        }
        else if (_selectedSpellIndex >= _spellMenuOffset + SpellMenuVisibleCount)
        {
            _spellMenuOffset = _selectedSpellIndex - SpellMenuVisibleCount + 1;
        }

        var maxOffset = Math.Max(0, spellCount - SpellMenuVisibleCount);
        _spellMenuOffset = Math.Clamp(_spellMenuOffset, 0, maxOffset);
    }

    private void EnsureCombatItemSelectionVisible(int itemCount)
    {
        if (itemCount <= CombatItemVisibleCount)
        {
            _combatItemMenuOffset = 0;
            return;
        }

        if (_selectedCombatItemIndex < _combatItemMenuOffset)
        {
            _combatItemMenuOffset = _selectedCombatItemIndex;
        }
        else if (_selectedCombatItemIndex >= _combatItemMenuOffset + CombatItemVisibleCount)
        {
            _combatItemMenuOffset = _selectedCombatItemIndex - CombatItemVisibleCount + 1;
        }

        var maxOffset = Math.Max(0, itemCount - CombatItemVisibleCount);
        _combatItemMenuOffset = Math.Clamp(_combatItemMenuOffset, 0, maxOffset);
    }

    private void EnsureSpellLearnSelectionVisible(int spellCount)
    {
        if (spellCount <= SpellLearnVisibleCount)
        {
            _spellLearnMenuOffset = 0;
            return;
        }

        if (_selectedSpellLearnIndex < _spellLearnMenuOffset)
        {
            _spellLearnMenuOffset = _selectedSpellLearnIndex;
        }
        else if (_selectedSpellLearnIndex >= _spellLearnMenuOffset + SpellLearnVisibleCount)
        {
            _spellLearnMenuOffset = _selectedSpellLearnIndex - SpellLearnVisibleCount + 1;
        }

        var maxOffset = Math.Max(0, spellCount - SpellLearnVisibleCount);
        _spellLearnMenuOffset = Math.Clamp(_spellLearnMenuOffset, 0, maxOffset);
    }

    private void EnsureCreationFeatSelectionVisible(int featCount)
    {
        if (featCount <= CreationFeatVisibleCount)
        {
            _creationFeatMenuOffset = 0;
            return;
        }

        if (_selectedCreationFeatIndex < _creationFeatMenuOffset)
        {
            _creationFeatMenuOffset = _selectedCreationFeatIndex;
        }
        else if (_selectedCreationFeatIndex >= _creationFeatMenuOffset + CreationFeatVisibleCount)
        {
            _creationFeatMenuOffset = _selectedCreationFeatIndex - CreationFeatVisibleCount + 1;
        }

        var maxOffset = Math.Max(0, featCount - CreationFeatVisibleCount);
        _creationFeatMenuOffset = Math.Clamp(_creationFeatMenuOffset, 0, maxOffset);
    }

    private void EnsureFeatSelectionVisible(int featCount)
    {
        if (featCount <= FeatVisibleCount)
        {
            _featMenuOffset = 0;
            return;
        }

        if (_selectedFeatIndex < _featMenuOffset)
        {
            _featMenuOffset = _selectedFeatIndex;
        }
        else if (_selectedFeatIndex >= _featMenuOffset + FeatVisibleCount)
        {
            _featMenuOffset = _selectedFeatIndex - FeatVisibleCount + 1;
        }

        var maxOffset = Math.Max(0, featCount - FeatVisibleCount);
        _featMenuOffset = Math.Clamp(_featMenuOffset, 0, maxOffset);
    }

    private void EnsureSkillSelectionVisible(int skillCount)
    {
        if (skillCount <= SkillVisibleCount)
        {
            _skillMenuOffset = 0;
            return;
        }

        if (_selectedSkillIndex < _skillMenuOffset)
        {
            _skillMenuOffset = _selectedSkillIndex;
        }
        else if (_selectedSkillIndex >= _skillMenuOffset + SkillVisibleCount)
        {
            _skillMenuOffset = _selectedSkillIndex - SkillVisibleCount + 1;
        }

        var maxOffset = Math.Max(0, skillCount - SkillVisibleCount);
        _skillMenuOffset = Math.Clamp(_skillMenuOffset, 0, maxOffset);
    }

    private bool HasAnyLearnableSpells()
    {
        return _player != null && _spellLearnChoices.Any(spell => _player.CanLearnSpell(spell, out _));
    }

    private int GetClassMeleeDamageBonus(Player player)
    {
        return player.CharacterClass.Name switch
        {
            "Warrior" => 2,
            "Barbarian" => 3,
            "Paladin" => 1,
            "Ranger" => 1,
            _ => 0
        };
    }

    private int GetClassSpellDamageBonus(Player player)
    {
        return player.CharacterClass.Name switch
        {
            "Mage" => 2,
            "Cleric" => 1,
            "Bard" => 1,
            "Paladin" => 1,
            "Ranger" => 1,
            _ => 0
        };
    }

    private int GetClassCritBonus(Player player)
    {
        return player.CharacterClass.Name switch
        {
            "Rogue" => 8,
            "Ranger" => 4,
            "Bard" => 3,
            _ => 0
        };
    }

    private int GetClassDefenseBonus(Player player)
    {
        return player.CharacterClass.Name switch
        {
            "Warrior" => 1,
            "Paladin" => 1,
            "Barbarian" => 1,
            "Cleric" => 1,
            _ => 0
        };
    }

    private int GetClassEvasionChance(Player player)
    {
        return player.CharacterClass.Name switch
        {
            "Rogue" => 12,
            "Ranger" => 6,
            "Bard" => 4,
            _ => 0
        };
    }

    private int GetClassFleeBonus(Player player)
    {
        return player.CharacterClass.Name switch
        {
            "Rogue" => 10,
            "Ranger" => 5,
            "Bard" => 5,
            _ => 0
        };
    }

    private string GetClassCombatTag(string className)
    {
        return className switch
        {
            "Warrior" => "Frontline bruiser",
            "Rogue" => "Skirmisher / crit",
            "Mage" => "Arcane cannon",
            "Paladin" => "Holy vanguard",
            "Ranger" => "Mobile striker",
            "Cleric" => "Battle chaplain",
            "Barbarian" => "Rage brawler",
            "Bard" => "Control support",
            _ => "Adventurer"
        };
    }

    private (int damage, bool crit, int rawDamage, int armorMitigation, int critChance) CalcPlayerDamage()
    {
        if (_player == null || _currentEnemy == null) return (0, false, 0, 0, 0);

        var strMod = _player.Mod(StatName.Strength);
        var classMeleeBonus = GetClassMeleeDamageBonus(_player);
        var baseDamage = CombatMath.CalculateMeleeBaseDamage(
            strMod,
            _player.MeleeDamageBonus,
            classMeleeBonus,
            _runMeleeBonus + GetConditionMeleeModifier());
        var dexMod = _player.Mod(StatName.Dexterity);
        var critChance = CombatMath.CalculateCritChancePercent(
            dexMod,
            _player.CritBonus,
            GetClassCritBonus(_player),
            _runCritBonus);
        var crit = _rng.NextDouble() * 100 < critChance;

        var rawDamage = baseDamage + _rng.Next(baseDamage);
        var armorMitigation = _currentEnemy.Type.Defense;
        var afterDef = Math.Max(1, rawDamage - armorMitigation);
        var finalDamage = crit ? afterDef * 2 : afterDef;
        return (finalDamage, crit, rawDamage, armorMitigation, critChance);
    }

    private void DoPlayerAttack()
    {
        if (_player == null || _currentEnemy == null) return;
        var validation = ValidateCurrentEnemyTargetForMelee();
        if (!validation.IsLegal)
        {
            PushCombatLog($"Attack blocked: {validation.BuildBlockedReason()}");
            return;
        }

        var warCryDamage = 0;
        if (_warCryAvailable && _player.HasSkill("war_cry"))
        {
            warCryDamage = _player.WarCryBonus;
            _warCryAvailable = false;
            PushCombatLog($"War Cry adds {warCryDamage} first-strike damage.");
        }

        var (damage, crit, rawDamage, armorMitigation, critChance) = CalcPlayerDamage();
        var total = damage + warCryDamage;
        _currentEnemy.CurrentHp = Math.Max(0, _currentEnemy.CurrentHp - total);
        var critTag = crit ? " CRIT x2!" : string.Empty;
        PushCombatLog($"You hit for {total}.{critTag}");
        PushCombatLog($"Raw {rawDamage} - armor {armorMitigation} | Crit chance {critChance}%.");
        PushCombatLog($"{_currentEnemy.Type.Name} HP {_currentEnemy.CurrentHp}/{_currentEnemy.Type.MaxHp}.");
        ApplyRelicMeleeTrigger();

        if (_player.HasBonusAttack && _currentEnemy.IsAlive)
        {
            var (bonus, bonusCrit, bonusRaw, bonusArmor, bonusCritChance) = CalcPlayerDamage();
            _currentEnemy.CurrentHp = Math.Max(0, _currentEnemy.CurrentHp - bonus);
            var bonusCritTag = bonusCrit ? " CRIT x2!" : string.Empty;
            PushCombatLog($"Swift Strikes: +{bonus}.{bonusCritTag}");
            PushCombatLog($"Raw {bonusRaw} - armor {bonusArmor} | Crit {bonusCritChance}%.");
            PushCombatLog($"{_currentEnemy.Type.Name} HP {_currentEnemy.CurrentHp}/{_currentEnemy.Type.MaxHp}.");
            ApplyRelicMeleeTrigger();
        }

        if (_player.PoisonDamage > 0 && _currentEnemy.IsAlive)
        {
            _enemyPoisoned = _player.PoisonDamage;
            PushCombatLog($"Poison Blade primed ({_enemyPoisoned}/turn).");
        }

        if (CheckEnemyDeath()) return;

        if (_enemyPoisoned > 0 && _currentEnemy != null)
        {
            _currentEnemy.CurrentHp = Math.Max(0, _currentEnemy.CurrentHp - _enemyPoisoned);
            PushCombatLog($"Poison deals {_enemyPoisoned} damage.");
            PushCombatLog($"{_currentEnemy.Type.Name} HP {_currentEnemy.CurrentHp}/{_currentEnemy.Type.MaxHp}.");
            if (CheckEnemyDeath()) return;
        }

        DoEnemyAttack();
    }

    private void DoSecondWind()
    {
        if (_player == null) return;

        _player.HasUsedSecondWind = true;
        var heal = _player.SecondWindHeal;
        var before = _player.CurrentHp;
        _player.CurrentHp = Math.Min(_player.CurrentHp + heal, _player.MaxHp);
        var recovered = _player.CurrentHp - before;
        PushCombatLog($"Second Wind restores {recovered} HP.");
        PushCombatLog($"HP {before} -> {_player.CurrentHp}/{_player.MaxHp}.");
        DoEnemyAttack();
    }

    private void DoManaShield()
    {
        if (_player == null || _currentEnemy == null) return;
        if (_gameState == GameState.DeathScreen || !_player.IsAlive) return;

        _player.CurrentMana -= 3;
        var absorb = _player.ManaShieldAbsorb;
        PushCombatLog($"Mana Shield primed ({absorb} absorb).");
        PushCombatLog($"MP {_player.CurrentMana}/{_player.MaxMana}.");
        DoEnemyAttack(firstEnemyDamageAbsorb: absorb);
    }

    private bool TryEnemyUseCombatLoot(Enemy enemy)
    {
        if (_player == null) return false;
        if (!_enemyLootKits.TryGetValue(enemy, out var loot)) return false;
        if (loot.UsedConsumableThisFight) return false;
        if (string.IsNullOrWhiteSpace(loot.ItemId) || loot.ItemQuantity <= 0) return false;

        switch (loot.ItemId)
        {
            case "health_potion":
            {
                if (enemy.CurrentHp >= enemy.Type.MaxHp) return false;
                var hpRatio = enemy.CurrentHp / (double)Math.Max(1, enemy.Type.MaxHp);
                if (hpRatio > 0.4) return false;

                var heal = Math.Max(7, (int)Math.Ceiling(enemy.Type.MaxHp * 0.35));
                var before = enemy.CurrentHp;
                enemy.CurrentHp = Math.Min(enemy.Type.MaxHp, enemy.CurrentHp + heal);
                var gained = enemy.CurrentHp - before;
                if (gained <= 0) return false;

                loot.ItemQuantity -= 1;
                loot.UsedConsumableThisFight = true;
                PushCombatLog($"{enemy.Type.Name} uses {loot.Name} and recovers {gained} HP.");
                PushCombatLog($"{enemy.Type.Name} HP {enemy.CurrentHp}/{enemy.Type.MaxHp}.");
                return true;
            }
            case "sharpening_oil":
            {
                if (loot.AttackBonus > 0) return false;
                if (_rng.NextDouble() > 0.55) return false;

                loot.ItemQuantity -= 1;
                loot.AttackBonus += 2;
                loot.UsedConsumableThisFight = true;
                PushCombatLog($"{enemy.Type.Name} uses {loot.Name}. Their attacks grow sharper.");
                return true;
            }
            case "mana_draught":
            {
                // Enemies currently do not use mana abilities; keep as droppable loot.
                return false;
            }
            default:
                return false;
        }
    }

    private Enemy? FindEncounterEnemyByCombatantId(string combatantId)
    {
        foreach (var enemy in _encounterEnemies)
        {
            if (!enemy.IsAlive)
            {
                continue;
            }

            if (string.Equals(BuildEncounterEnemyCombatantId(enemy), combatantId, StringComparison.Ordinal))
            {
                return enemy;
            }
        }

        return null;
    }

    private void DoEnemyAttack(int firstEnemyDamageAbsorb = 0, bool skipFirstEnemyTurn = false)
    {
        if (_player == null)
        {
            return;
        }

        if (_gameState == GameState.DeathScreen || !_player.IsAlive)
        {
            HandlePlayerDeath();
            return;
        }

        PruneEncounterTurnOrder();
        if (_encounterTurnOrder.Count == 0)
        {
            RebuildEncounterTurnOrder(preferredCombatantId: "player");
        }

        if (_encounterTurnOrder.Count == 0)
        {
            SetEncounterTurnToPlayer();
            BeginPlayerCombatTurn();
            return;
        }

        SetEncounterTurnToPlayer();
        AdvanceEncounterTurn();
        ResolveEnemyTurnsUntilPlayerTurn(firstEnemyDamageAbsorb, skipFirstEnemyTurn);
    }

    private void ResolveEnemyTurnsUntilPlayerTurn(int firstEnemyDamageAbsorb = 0, bool skipFirstEnemyTurn = false)
    {
        if (_player == null)
        {
            return;
        }

        if (_gameState == GameState.DeathScreen || !_player.IsAlive)
        {
            HandlePlayerDeath();
            return;
        }

        var pendingDamageAbsorb = Math.Max(0, firstEnemyDamageAbsorb);
        var skipNextEnemyTurn = skipFirstEnemyTurn;
        var processedEnemyTurns = 0;

        while (processedEnemyTurns < EncounterEnemyTurnCapPerPlayerAction)
        {
            if (_player == null || !_player.IsAlive || _gameState == GameState.DeathScreen)
            {
                HandlePlayerDeath();
                return;
            }

            if (_gameState != GameState.Combat)
            {
                return;
            }

            if (_resolvingEnemyDeath || _defeatedEnemyPending != null || _enemyResolveAt > 0)
            {
                return;
            }

            TryJoinEncounterReinforcements();
            PruneEncounterTurnOrder();
            if (_encounterTurnOrder.Count == 0)
            {
                return;
            }

            SyncEncounterCurrentCombatantId();
            var slot = _encounterTurnOrder[_encounterTurnIndex];
            if (slot.Kind == EncounterCombatantKind.Player)
            {
                SetEncounterTurnToPlayer();
                BeginPlayerCombatTurn();
                return;
            }

            var actingEnemy = FindEncounterEnemyByCombatantId(slot.Id);
            if (actingEnemy == null)
            {
                AdvanceEncounterTurn();
                processedEnemyTurns += 1;
                continue;
            }

            _currentEnemy = actingEnemy;
            SetEncounterTurnToEnemy(actingEnemy);
            _packEnemiesRemainingAfterCurrent = Math.Max(0, _encounterEnemies.Count(enemy =>
                enemy.IsAlive &&
                !ReferenceEquals(enemy, _currentEnemy)));

            if (skipNextEnemyTurn)
            {
                skipNextEnemyTurn = false;
            }
            else
            {
                ExecuteEnemyTurn(actingEnemy, pendingDamageAbsorb);
                pendingDamageAbsorb = 0;

                if (_player == null || !_player.IsAlive || _gameState == GameState.DeathScreen)
                {
                    HandlePlayerDeath();
                    return;
                }

                if (_resolvingEnemyDeath || _defeatedEnemyPending != null || _enemyResolveAt > 0)
                {
                    return;
                }
            }

            AdvanceEncounterTurn();
            processedEnemyTurns += 1;
        }

        PushCombatLog($"Enemy phase capped at {EncounterEnemyTurnCapPerPlayerAction} turns.");
        SetEncounterTurnToPlayer();
        BeginPlayerCombatTurn();
    }

    private void ExecuteEnemyTurn(Enemy enemy, int damageAbsorb)
    {
        if (_player == null || !enemy.IsAlive)
        {
            return;
        }

        if (TryEnemyUseCombatLoot(enemy))
        {
            return;
        }

        var attackDecision = EncounterEnemyTactics.EvaluateAttackFeasibility(
            attackerX: enemy.X,
            attackerY: enemy.Y,
            targetX: _player.X,
            targetY: _player.Y,
            targetAlive: _player.IsAlive,
            maxRangeTiles: GetEnemyAttackRangeTiles(enemy),
            requiresLineOfSight: true,
            hasLineOfSight: HasLineOfSight);

        var moved = false;
        if (!attackDecision.CanAttack)
        {
            moved = TryExecuteEnemyTacticalMovement(enemy, out _);
            if (moved)
            {
                attackDecision = EncounterEnemyTactics.EvaluateAttackFeasibility(
                    attackerX: enemy.X,
                    attackerY: enemy.Y,
                    targetX: _player.X,
                    targetY: _player.Y,
                    targetAlive: _player.IsAlive,
                    maxRangeTiles: GetEnemyAttackRangeTiles(enemy),
                    requiresLineOfSight: true,
                    hasLineOfSight: HasLineOfSight);
            }
        }

        if (!attackDecision.CanAttack)
        {
            var reason = attackDecision.Validation.BuildBlockedReason();
            var prefix = moved
                ? $"{enemy.Type.Name} still cannot get a clear attack"
                : $"{enemy.Type.Name} cannot get a clear attack";
            PushCombatLog($"{prefix} ({reason}).");
            return;
        }

        var evadeChance = GetClassEvasionChance(_player);
        if (evadeChance > 0 && _rng.Next(100) < evadeChance)
        {
            PushCombatLog($"{enemy.Type.Name} attacks, but you evade!");
            return;
        }

        var armorStyleDefense = GetArmorStateDefenseBonus(_player);
        var totalDefense = _player.DefenseBonus + GetClassDefenseBonus(_player) + _runDefenseBonus + armorStyleDefense + GetConditionDefenseModifier();
        var enemyAttackBonus = _enemyLootKits.TryGetValue(enemy, out var enemyLoot)
            ? enemyLoot.AttackBonus
            : 0;
        var rawEnemyRoll = enemy.Type.Attack + enemyAttackBonus + _phase3EnemyAttackBonus + _rng.Next(4) - 1;
        var clampedAbsorb = Math.Max(0, damageAbsorb);
        var adjustedRaw = Math.Max(0, rawEnemyRoll - clampedAbsorb);
        var damage = clampedAbsorb > 0
            ? Math.Max(0, CombatMath.CalculateEnemyDamage(adjustedRaw, totalDefense, minimumDamage: 0))
            : CombatMath.CalculateEnemyDamage(rawEnemyRoll, totalDefense);

        _player.CurrentHp = Math.Max(0, _player.CurrentHp - damage);
        if (clampedAbsorb > 0)
        {
            PushCombatLog($"Mana Shield absorbs {clampedAbsorb}; you take {damage}.");
            PushCombatLog($"Enemy roll {rawEnemyRoll} - absorb {clampedAbsorb} - defense {totalDefense}.");
        }
        else
        {
            PushCombatLog($"{enemy.Type.Name} hits for {damage}.");
            PushCombatLog($"Enemy roll {rawEnemyRoll} - defense {totalDefense}.");
        }

        PushCombatLog($"Your HP {_player.CurrentHp}/{_player.MaxHp}.");
        TryRollDungeonConditionFromEnemyHit(damage);
    }

    private void DoFlee()
    {
        if (_player == null) return;
        if (!_player.IsAlive)
        {
            HandlePlayerDeath();
            return;
        }

        var armorStyleFlee = GetArmorStateFleeBonus(_player);
        var chance = CombatMath.CalculateFleeChancePercent(
            _player.FleeBonus,
            GetClassFleeBonus(_player),
            _runFleeBonus + armorStyleFlee + GetConditionFleeModifier());
        var relicFleeBonus = ConsumeRelicFleeBonus();
        if (relicFleeBonus > 0)
        {
            chance = Math.Clamp(chance + relicFleeBonus, 5, 95);
            PushCombatLog($"Veilstrider Charm boosts this flee attempt (+{relicFleeBonus}%).");
        }
        var roll = _rng.Next(1, 101);
        PushCombatLog($"Flee check: roll {roll} vs {chance}.");
        if (roll <= chance)
        {
            PushCombatLog("You fled from battle!");
            _currentEnemy = null;
            ResetEncounterContext();
            EnterPlayingState("flee_success");
        }
        else
        {
            PushCombatLog("You failed to flee!");
            if (TryConsumeMilestoneEscapeBlock())
            {
                PushCombatLog($"Escape Doctrine prevents retaliation. Charges left: {_milestoneEscapeChargesThisCombat}.");
                if (_runArchetype == RunArchetype.Skirmisher)
                {
                    PushCombatLog("Skirmisher rhythm keeps your momentum.");
                }

                DoEnemyAttack(skipFirstEnemyTurn: true);
                return;
            }

            DoEnemyAttack();
        }
    }

    private bool CheckEnemyDeath()
    {
        if (_player == null || _currentEnemy == null) return false;
        if (_gameState == GameState.DeathScreen) return true;
        if (_currentEnemy.IsAlive) return false;
        if (_resolvingEnemyDeath) return true;

        _resolvingEnemyDeath = true;
        _currentEnemy.CurrentHp = 0;
        _defeatedEnemyPending = _currentEnemy;
        _enemyResolveAt = Raylib.GetTime() + 0.5;
        PushCombatLog($"{_currentEnemy.Type.Name} collapses...");
        return true;
    }

    private void SpawnGuaranteedLootDrop(Enemy defeatedEnemy)
    {
        if (!_enemyLootKits.TryGetValue(defeatedEnemy, out var kit))
        {
            kit = CreateEnemyLootKit(defeatedEnemy);
            _enemyLootKits[defeatedEnemy] = kit;
        }

        if (!string.IsNullOrWhiteSpace(kit.ItemId) && kit.ItemQuantity > 0)
        {
            var (dropX, dropY) = ResolveLootDropPosition(defeatedEnemy.X, defeatedEnemy.Y);
            _groundLoot.Add(new GroundLoot
            {
                Id = $"loot_{Guid.NewGuid():N}",
                Name = kit.Name,
                Rarity = kit.Rarity,
                X = dropX,
                Y = dropY,
                InventoryItemId = kit.ItemId,
                InventoryItemQuantity = kit.ItemQuantity
            });

            PushCombatLog($"Loot dropped near {defeatedEnemy.Type.Name}.");
        }
        else
        {
            PushCombatLog($"{defeatedEnemy.Type.Name} had no usable supplies left.");
        }
    }

    private LootTemplate RollLowLevelLootTemplate(Enemy enemy)
    {
        var rare = LowLevelLootTable.Where(template => template.Rarity == LootRarity.Rare).ToArray();
        var uncommon = LowLevelLootTable.Where(template => template.Rarity == LootRarity.Uncommon).ToArray();
        var common = LowLevelLootTable.Where(template => template.Rarity == LootRarity.Common).ToArray();

        LootTemplate PickFrom(LootTemplate[] pool, LootTemplate[] fallback)
        {
            if (pool.Length > 0)
            {
                return pool[_rng.Next(pool.Length)];
            }

            if (fallback.Length > 0)
            {
                return fallback[_rng.Next(fallback.Length)];
            }

            return LowLevelLootTable[0];
        }

        var enemyKey = ResolveEnemyTypeKey(enemy.Type);
        if (string.Equals(enemyKey, "goblin_general", StringComparison.Ordinal))
        {
            return PickFrom(rare, common);
        }

        if (string.Equals(enemyKey, "goblin_supervisor", StringComparison.Ordinal))
        {
            if (_rng.NextDouble() < 0.45)
            {
                return PickFrom(rare, uncommon);
            }

            return PickFrom(uncommon, common);
        }

        if (string.Equals(enemyKey, "goblin_slinger", StringComparison.Ordinal) ||
            string.Equals(enemyKey, "goblin_skirmisher", StringComparison.Ordinal))
        {
            if (_rng.NextDouble() < 0.40)
            {
                return PickFrom(uncommon, common);
            }

            return PickFrom(common, LowLevelLootTable);
        }

        if (enemy.Type.XpReward >= 170 && _rng.NextDouble() < 0.35)
        {
            return PickFrom(rare, common);
        }

        if (enemy.Type.XpReward >= 110 && _rng.NextDouble() < 0.60)
        {
            return PickFrom(uncommon, common);
        }

        return PickFrom(common, LowLevelLootTable);
    }

    private (int X, int Y) ResolveLootDropPosition(int x, int y)
    {
        foreach (var offset in LootPlacementOffsets)
        {
            var candidateX = x + offset.X;
            var candidateY = y + offset.Y;
            if (IsWallOrSealed(candidateX, candidateY))
            {
                continue;
            }

            if (_groundLoot.Any(loot => loot.X == candidateX && loot.Y == candidateY))
            {
                continue;
            }

            if (_enemies.Any(enemy => enemy.IsAlive && enemy.X == candidateX && enemy.Y == candidateY))
            {
                continue;
            }

            return (candidateX, candidateY);
        }

        return (x, y);
    }

    private void ResolveEnemyDefeat(Enemy defeatedEnemy)
    {
        if (_player == null)
        {
            _resolvingEnemyDeath = false;
            return;
        }

        if (!_player.IsAlive || _gameState == GameState.DeathScreen)
        {
            _resolvingEnemyDeath = false;
            _defeatedEnemyPending = null;
            _enemyResolveAt = -1;
            return;
        }

        PushCombatLog($"{defeatedEnemy.Type.Name} defeated!");
        _phase3EnemiesDefeated += 1;
        var baseXp = defeatedEnemy.Type.XpReward;
        var xp = Math.Max(1, (int)Math.Round(baseXp * (100 + _phase3XpPercentMod) / 100.0, MidpointRounding.AwayFromZero));
        PushCombatLog($"You gain {xp} XP.");
        if (_phase3XpPercentMod != 0)
        {
            var modifierText = _phase3XpPercentMod > 0 ? $"+{_phase3XpPercentMod}%" : $"{_phase3XpPercentMod}%";
            PushCombatLog($"Route modifier applied to XP ({modifierText}).");
        }
        var didLevelUp = _player.GainXp(xp);
        ApplyMilestoneExecutionRewardOnEnemyDefeat();
        if (string.Equals(ResolveEnemyTypeKey(defeatedEnemy.Type), "goblin_general", StringComparison.Ordinal))
        {
            _bossDefeated = true;
            PushCombatLog("The Goblin General is down. The sanctum trembles.");
        }

        // Counterplay rule: every defeated enemy drops loot so the player can choose loot-first vs fight-first.
        SpawnGuaranteedLootDrop(defeatedEnemy);

        _enemies = _enemies.Where(e => !ReferenceEquals(e, defeatedEnemy)).ToList();
        _enemyAi.Remove(defeatedEnemy);
        _enemyLootKits.Remove(defeatedEnemy);
        if (ReferenceEquals(_currentEnemy, defeatedEnemy)) _currentEnemy = null;
        _resolvingEnemyDeath = false;

        if (!didLevelUp && TryPromoteNextGoblinPackEnemy(defeatedEnemy))
        {
            return;
        }

        if (_enemies.Count == 0)
        {
            if (_bossDefeated)
            {
                _floorCleared = true;
                PushCombatLog("All hostiles eliminated. Floor 1 cleared.");
                ResetEncounterContext();
                _gameState = GameState.VictoryScreen;
                TryAutosaveCheckpoint("floor1_cleared");
                return;
            }

            PushCombatLog("Area secured. Continue deeper for the boss.");
        }

        if (_gameState == GameState.DeathScreen || !_player.IsAlive)
        {
            return;
        }

        var victorySummary = didLevelUp
            ? $"{defeatedEnemy.Type.Name} defeated: +{xp} XP. Level up available."
            : $"{defeatedEnemy.Type.Name} defeated: +{xp} XP.";
        ShowRewardMessage(victorySummary, requireAcknowledge: !didLevelUp, visibleSeconds: 12);

        if (didLevelUp)
        {
            ResetEncounterContext();
            _selectionMessage = string.Empty;
            _gameState = GameState.LevelUp;
        }
        else
        {
            ResetEncounterContext();
            EnterPlayingState("combat_victory");
        }
    }

    private void HandlePlayerDeath()
    {
        if (_gameState == GameState.DeathScreen) return;
        _resolvingEnemyDeath = false;
        _enemyResolveAt = -1;
        _defeatedEnemyPending = null;
        _respawnEnemiesAt = -1;
        _currentEnemy = null;
        _enemyPoisoned = 0;
        _warCryAvailable = false;
        ResetEncounterContext();
        PushCombatLog("You have been defeated...");
        _gameState = GameState.DeathScreen;
    }

    private void HandleLevelUpInput()
    {
        if (_player == null) return;

        if (Pressed(KeyUp))
        {
            _selectedStatIndex = (_selectedStatIndex - 1 + StatOrder.Length) % StatOrder.Length;
            return;
        }

        if (Pressed(KeyDown))
        {
            _selectedStatIndex = (_selectedStatIndex + 1) % StatOrder.Length;
            return;
        }

        if (!Pressed(KeyEnter)) return;

        if (_player.StatPoints <= 0)
        {
            _selectionMessage = "No stat points left. Moving to feat/spell/skill picks.";
            PreparePostLevelUpChoices();
            return;
        }

        var chosenStat = StatOrder[_selectedStatIndex];
        if (_player.IncreaseStat(chosenStat))
        {
            _selectionMessage = $"{chosenStat} increased.";
        }

        if (_player.StatPoints == 0)
        {
            PreparePostLevelUpChoices();
        }
    }

    private void PreparePostLevelUpChoices()
    {
        if (_player == null) return;

        if (_player.FeatPoints > 0)
        {
            PrepareFeatSelection();
            return;
        }

        PrepareSpellOrSkillSelectionAfterLevelUp();
    }

    private void PrepareSpellOrSkillSelectionAfterLevelUp()
    {
        if (_player == null) return;

        if (TryOpenSpellSelection(
                title: "Choose New Spell",
                nextState: GameState.SkillSelection,
                startsAdventure: false))
        {
            return;
        }

        PrepareSkillSelection();
    }

    private void PrepareFeatSelection()
    {
        if (_player == null) return;

        _featChoices.Clear();
        var unlearned = FeatBook.All
            .Where(feat => !_player.HasFeat(feat.Id))
            .OrderBy(feat => feat.Name);
        _featChoices.AddRange(unlearned);
        _selectedFeatIndex = 0;
        _featMenuOffset = 0;
        _selectionMessage = string.Empty;

        if (_featChoices.Count == 0)
        {
            PrepareSpellOrSkillSelectionAfterLevelUp();
            return;
        }

        _gameState = GameState.FeatSelection;
    }

    private void HandleFeatSelectionInput()
    {
        if (_player == null) return;

        if (_featChoices.Count == 0)
        {
            PrepareSkillSelection();
            return;
        }

        if (Pressed(KeyUp))
        {
            _selectedFeatIndex = (_selectedFeatIndex - 1 + _featChoices.Count) % _featChoices.Count;
            EnsureFeatSelectionVisible(_featChoices.Count);
            return;
        }

        if (Pressed(KeyDown))
        {
            _selectedFeatIndex = (_selectedFeatIndex + 1) % _featChoices.Count;
            EnsureFeatSelectionVisible(_featChoices.Count);
            return;
        }

        if (!Pressed(KeyEnter)) return;

        var chosen = _featChoices[_selectedFeatIndex];
        if (!_player.CanLearnFeat(chosen, out var blockReason))
        {
            _selectionMessage = blockReason;
            return;
        }

        if (_player.LearnFeat(chosen))
        {
            _selectionMessage = $"Learned feat: {chosen.Name}.";
            if (_player.FeatPoints > 0)
            {
                PrepareFeatSelection();
                return;
            }
        }

        PrepareSpellOrSkillSelectionAfterLevelUp();
    }

    private bool TryOpenSpellSelection(string title, GameState nextState, bool startsAdventure)
    {
        if (_player == null) return false;
        if (_player.SpellPickPoints <= 0) return false;

        _spellLearnChoices.Clear();
        _spellLearnChoices.AddRange(_player.GetClassSpells());
        if (_spellLearnChoices.Count == 0) return false;
        if (!HasAnyLearnableSpells()) return false;

        _spellSelectionTitle = title;
        _spellSelectionNextState = nextState;
        _spellSelectionStartsAdventure = startsAdventure;
        _selectedSpellLearnIndex = 0;
        _spellLearnMenuOffset = 0;
        _selectionMessage = string.Empty;
        _gameState = GameState.SpellSelection;
        return true;
    }

    private void HandleSpellSelectionInput()
    {
        if (_player == null)
        {
            _gameState = _spellSelectionNextState;
            return;
        }

        if (_spellLearnChoices.Count == 0)
        {
            CompleteSpellSelection();
            return;
        }

        if (Pressed(KeyUp))
        {
            _selectedSpellLearnIndex = (_selectedSpellLearnIndex - 1 + _spellLearnChoices.Count) % _spellLearnChoices.Count;
            EnsureSpellLearnSelectionVisible(_spellLearnChoices.Count);
            return;
        }

        if (Pressed(KeyDown))
        {
            _selectedSpellLearnIndex = (_selectedSpellLearnIndex + 1) % _spellLearnChoices.Count;
            EnsureSpellLearnSelectionVisible(_spellLearnChoices.Count);
            return;
        }

        if (!Pressed(KeyEnter)) return;

        var chosen = _spellLearnChoices[_selectedSpellLearnIndex];
        if (!_player.CanLearnSpell(chosen, out var blockReason))
        {
            _selectionMessage = blockReason;
            return;
        }

        if (!_player.LearnSpell(chosen))
        {
            _selectionMessage = "Unable to learn that spell right now.";
            return;
        }

        _selectionMessage = $"Learned spell: {chosen.Name}.";

        if (_player.SpellPickPoints > 0)
        {
            if (HasAnyLearnableSpells())
            {
                EnsureSpellLearnSelectionVisible(_spellLearnChoices.Count);
                return;
            }
        }

        CompleteSpellSelection();
    }

    private void CompleteSpellSelection()
    {
        _selectionMessage = string.Empty;

        if (_spellSelectionStartsAdventure)
        {
            TryApplyCreationOriginConditionIfNeeded();
            SpawnEnemies();
            EnterPlayingState("adventure_start");
            return;
        }

        if (_spellSelectionNextState == GameState.SkillSelection)
        {
            PrepareSkillSelection();
            return;
        }

        _gameState = _spellSelectionNextState;
    }

    private void PrepareSkillSelection()
    {
        if (_player == null) return;

        _skillChoices.Clear();
        var unlearned = SkillBook.All
            .Where(skill => !_player.HasSkill(skill.Id))
            .OrderBy(skill => skill.Name);
        _skillChoices.AddRange(unlearned);
        _selectedSkillIndex = 0;
        _skillMenuOffset = 0;
        _selectionMessage = string.Empty;

        if (_skillChoices.Count == 0)
        {
            EnterPlayingState("levelup_complete");
            return;
        }

        _gameState = GameState.SkillSelection;
    }

    private void HandleSkillSelectionInput()
    {
        if (_player == null)
            return;

        if (_skillChoices.Count == 0)
        {
            EnterPlayingState("levelup_complete");
            return;
        }

        if (Pressed(KeyUp))
        {
            _selectedSkillIndex = (_selectedSkillIndex - 1 + _skillChoices.Count) % _skillChoices.Count;
            EnsureSkillSelectionVisible(_skillChoices.Count);
            return;
        }

        if (Pressed(KeyDown))
        {
            _selectedSkillIndex = (_selectedSkillIndex + 1) % _skillChoices.Count;
            EnsureSkillSelectionVisible(_skillChoices.Count);
            return;
        }

        if (!Pressed(KeyEnter)) return;

        var chosen = _skillChoices[_selectedSkillIndex];
        if (_player.HasSkill(chosen.Id))
        {
            _selectionMessage = "That skill is already learned.";
            return;
        }

        _player.LearnSkill(chosen);
        _selectionMessage = $"Learned skill: {chosen.Name}.";
        EnterPlayingState("levelup_complete");
    }

    private void HandleRewardChoiceInput()
    {
        if (_player == null || _activeRewardNode == null)
        {
            _gameState = GameState.Playing;
            return;
        }

        var optionCount = GetActiveRewardOptionNames().Length;
        if (Pressed(KeyEscape))
        {
            ShowRewardMessage("Reward deferred. You can return to this node later.", requireAcknowledge: false, visibleSeconds: 10);
            _activeRewardNode = null;
            _gameState = GameState.Playing;
            return;
        }

        if (Pressed(KeyUp))
        {
            _selectedRewardOptionIndex = (_selectedRewardOptionIndex - 1 + optionCount) % optionCount;
            return;
        }

        if (Pressed(KeyDown))
        {
            _selectedRewardOptionIndex = (_selectedRewardOptionIndex + 1) % optionCount;
            return;
        }

        if (!Pressed(KeyEnter)) return;
        ApplyRewardChoice(_selectedRewardOptionIndex);
    }

    private void EnterPlayingState(string autosaveReason)
    {
        if (_floorCleared)
        {
            _gameState = GameState.VictoryScreen;
            return;
        }

        ResetEncounterContext();
        _gameState = GameState.Playing;
        if (!string.IsNullOrWhiteSpace(autosaveReason))
        {
            TryAutosaveCheckpoint(autosaveReason);
        }
    }

    private void OpenPauseMenu(GameState returnState)
    {
        if (!IsSaveEligibleState(returnState)) return;

        _nextMoveAt = -1;
        _pausedFromState = returnState;
        _pauseMenuView = PauseMenuView.Root;
        _pauseMenuIndex = 0;
        _pauseMessage = string.Empty;
        ResetPauseConfirm();
        RefreshPauseSaveEntries();
        RefreshPauseLoadEntries();
        _gameState = GameState.PauseMenu;
    }

    private void ResumeFromPause()
    {
        _gameState = GameStateRules.ResolveResumeState(_pausedFromState, _currentEnemy != null);
    }

    private void HandlePauseMenuInput()
    {
        if (TryHandlePauseConfirmationInput())
        {
            return;
        }

        switch (_pauseMenuView)
        {
            case PauseMenuView.Root:
                HandlePauseRootInput();
                return;
            case PauseMenuView.Inventory:
                HandlePauseInventoryInput();
                return;
            case PauseMenuView.Save:
                HandlePauseSaveInput();
                return;
            case PauseMenuView.Load:
                HandlePauseLoadInput();
                return;
            case PauseMenuView.Settings:
                HandlePauseSettingsInput();
                return;
            case PauseMenuView.Accessibility:
                HandlePauseAccessibilityInput();
                return;
        }
    }

    private bool TryHandlePauseConfirmationInput()
    {
        if (_pauseConfirmAction == PauseConfirmAction.None)
        {
            return false;
        }

        if (Pressed(KeyEscape))
        {
            _pauseMessage = "Action canceled.";
            ResetPauseConfirm();
            return true;
        }

        if (!Pressed(KeyEnter))
        {
            return true;
        }

        var action = _pauseConfirmAction;
        var target = _pauseConfirmTarget;
        ResetPauseConfirm();

        switch (action)
        {
            case PauseConfirmAction.OverwriteSave:
                if (target < 1 || target > SaveStore.MaxManualSlot)
                {
                    _pauseMessage = "Selected save slot is invalid.";
                    return true;
                }

                TryManualSaveSlot(target);
                RefreshPauseSaveEntries();
                RefreshPauseLoadEntries();
                return true;

            case PauseConfirmAction.LoadRun:
                if (target < 0 || target >= _pauseLoadEntries.Count)
                {
                    _pauseMessage = "Selected load entry is no longer available.";
                    return true;
                }

                TryLoadEntry(_pauseLoadEntries[target]);
                if (_gameState == GameState.PauseMenu)
                {
                    RefreshPauseLoadEntries();
                }

                return true;

            case PauseConfirmAction.QuitToTitle:
                ReturnToMainMenu();
                return true;

            default:
                return true;
        }
    }

    private void HandlePauseRootInput()
    {
        if (Pressed(KeyEscape))
        {
            ResumeFromPause();
            return;
        }

        if (Pressed(KeyUp))
        {
            _pauseMenuIndex = (_pauseMenuIndex - 1 + PauseRootOptions.Length) % PauseRootOptions.Length;
            return;
        }

        if (Pressed(KeyDown))
        {
            _pauseMenuIndex = (_pauseMenuIndex + 1) % PauseRootOptions.Length;
            return;
        }

        if (!Pressed(KeyEnter)) return;

        switch (_pauseMenuIndex)
        {
            case 0:
                ResumeFromPause();
                return;
            case 1:
                OpenPauseInventoryMenu();
                return;
            case 2:
                OpenPauseSaveMenu();
                return;
            case 3:
                OpenPauseLoadMenu();
                return;
            case 4:
                OpenPauseSettingsMenu();
                return;
            case 5:
                _pauseConfirmAction = PauseConfirmAction.QuitToTitle;
                _pauseConfirmTarget = -1;
                _pauseMessage = "Quit to title? Unsaved progress will be lost. ENTER confirm, ESC cancel.";
                return;
        }
    }

    private void HandlePauseInventoryInput()
    {
        var optionCount = _inventoryItems.Count + 1; // + Back
        if (Pressed(KeyEscape))
        {
            BackToPauseRoot(1);
            return;
        }

        if (Pressed(KeyUp))
        {
            _pauseMenuIndex = (_pauseMenuIndex - 1 + optionCount) % optionCount;
            return;
        }

        if (Pressed(KeyDown))
        {
            _pauseMenuIndex = (_pauseMenuIndex + 1) % optionCount;
            return;
        }

        if (!Pressed(KeyEnter)) return;

        if (_pauseMenuIndex == _inventoryItems.Count)
        {
            BackToPauseRoot(1);
            return;
        }

        if (_pauseMenuIndex < 0 || _pauseMenuIndex >= _inventoryItems.Count)
        {
            _pauseMessage = "Selected item is invalid.";
            return;
        }

        TryUseOrToggleInventoryItem(_inventoryItems[_pauseMenuIndex]);
    }

    private void HandlePauseSaveInput()
    {
        var optionCount = _pauseSaveEntries.Count + 1; // + Back
        if (Pressed(KeyEscape))
        {
            BackToPauseRoot(2);
            return;
        }

        if (Pressed(KeyUp))
        {
            _pauseMenuIndex = (_pauseMenuIndex - 1 + optionCount) % optionCount;
            return;
        }

        if (Pressed(KeyDown))
        {
            _pauseMenuIndex = (_pauseMenuIndex + 1) % optionCount;
            return;
        }

        if (!Pressed(KeyEnter)) return;

        if (_pauseMenuIndex == _pauseSaveEntries.Count)
        {
            BackToPauseRoot(2);
            return;
        }

        var selected = _pauseSaveEntries[_pauseMenuIndex];
        if (selected.Exists)
        {
            _pauseConfirmAction = PauseConfirmAction.OverwriteSave;
            _pauseConfirmTarget = selected.ManualSlot;
            _pauseMessage = $"Overwrite slot {selected.ManualSlot}? ENTER confirm, ESC cancel.";
            return;
        }

        TryManualSaveSlot(selected.ManualSlot);
        RefreshPauseSaveEntries();
        RefreshPauseLoadEntries();
    }

    private void HandlePauseLoadInput()
    {
        var optionCount = _pauseLoadEntries.Count + 1; // + Back
        if (Pressed(KeyEscape))
        {
            BackToPauseRoot(3);
            return;
        }

        if (Pressed(KeyUp))
        {
            _pauseMenuIndex = (_pauseMenuIndex - 1 + optionCount) % optionCount;
            return;
        }

        if (Pressed(KeyDown))
        {
            _pauseMenuIndex = (_pauseMenuIndex + 1) % optionCount;
            return;
        }

        if (!Pressed(KeyEnter)) return;

        if (_pauseMenuIndex == _pauseLoadEntries.Count)
        {
            BackToPauseRoot(3);
            return;
        }

        if (_pauseMenuIndex < 0 || _pauseMenuIndex >= _pauseLoadEntries.Count)
        {
            _pauseMessage = "Selected load entry is invalid.";
            return;
        }

        var selected = _pauseLoadEntries[_pauseMenuIndex];
        _pauseConfirmAction = PauseConfirmAction.LoadRun;
        _pauseConfirmTarget = _pauseMenuIndex;
        _pauseMessage = $"Load {selected.Label}? Unsaved progress is lost. ENTER confirm, ESC cancel.";
    }

    private void HandlePauseSettingsInput()
    {
        var optionCount = PauseSettingsOptions.Length;
        if (Pressed(KeyEscape))
        {
            BackToPauseRoot(4);
            return;
        }

        if (Pressed(KeyUp))
        {
            _pauseMenuIndex = (_pauseMenuIndex - 1 + optionCount) % optionCount;
            return;
        }

        if (Pressed(KeyDown))
        {
            _pauseMenuIndex = (_pauseMenuIndex + 1) % optionCount;
            return;
        }

        if (_pauseMenuIndex == 1 && Pressed(KeyLeft))
        {
            AdjustMasterVolume(-5);
            return;
        }

        if (_pauseMenuIndex == 1 && Pressed(KeyRight))
        {
            AdjustMasterVolume(5);
            return;
        }

        if (!Pressed(KeyEnter)) return;

        switch (_pauseMenuIndex)
        {
            case 0:
                Raylib.ToggleFullscreen();
                _pauseMessage = Raylib.IsWindowFullscreen()
                    ? "Fullscreen enabled."
                    : "Windowed mode enabled.";
                return;
            case 1:
                AdjustMasterVolume(5);
                return;
            case 2:
                _settingsVerboseCombatLog = !_settingsVerboseCombatLog;
                _pauseMessage = _settingsVerboseCombatLog
                    ? "Combat log detail: Verbose."
                    : "Combat log detail: Compact.";
                return;
            case 3:
                OpenPauseAccessibilityMenu();
                return;
            case 4:
                BackToPauseRoot(4);
                return;
        }
    }

    private void HandlePauseAccessibilityInput()
    {
        var optionCount = PauseAccessibilityOptions.Length;
        if (Pressed(KeyEscape))
        {
            BackToPauseSettings(3);
            return;
        }

        if (Pressed(KeyUp))
        {
            _pauseMenuIndex = (_pauseMenuIndex - 1 + optionCount) % optionCount;
            return;
        }

        if (Pressed(KeyDown))
        {
            _pauseMenuIndex = (_pauseMenuIndex + 1) % optionCount;
            return;
        }

        if (_pauseMenuIndex == 0 && Pressed(KeyLeft))
        {
            CycleAccessibilityColorProfile(-1);
            return;
        }

        if (_pauseMenuIndex == 0 && Pressed(KeyRight))
        {
            CycleAccessibilityColorProfile(1);
            return;
        }

        if (_pauseMenuIndex == 1 && (Pressed(KeyLeft) || Pressed(KeyRight)))
        {
            ToggleHighContrastUi();
            return;
        }

        if (_pauseMenuIndex == 2 && (Pressed(KeyLeft) || Pressed(KeyRight)))
        {
            ToggleOptionalConditionsMode();
            return;
        }

        if (!Pressed(KeyEnter)) return;

        switch (_pauseMenuIndex)
        {
            case 0:
                CycleAccessibilityColorProfile(1);
                return;
            case 1:
                ToggleHighContrastUi();
                return;
            case 2:
                ToggleOptionalConditionsMode();
                return;
            case 3:
                TryPurgeMajorCondition();
                return;
            case 4:
                BackToPauseSettings(3);
                return;
        }
    }

    private void TryUseOrToggleInventoryItem(InventoryItem item)
    {
        if (_player == null)
        {
            _pauseMessage = "No active run loaded.";
            return;
        }

        if (item.Kind == InventoryItemKind.Consumable)
        {
            if (item.Quantity <= 0)
            {
                _pauseMessage = $"{item.Name} is depleted.";
                return;
            }

            switch (item.Id)
            {
                case "health_potion":
                {
                    if (_player.CurrentHp >= _player.MaxHp)
                    {
                        _pauseMessage = "HP is already full.";
                        return;
                    }

                    var healAmount = Math.Max(8, (int)Math.Ceiling(_player.MaxHp * 0.35));
                    var before = _player.CurrentHp;
                    _player.CurrentHp = Math.Min(_player.MaxHp, _player.CurrentHp + healAmount);
                    var gained = _player.CurrentHp - before;
                    if (gained <= 0)
                    {
                        _pauseMessage = "Health Potion had no effect.";
                        return;
                    }

                    item.Quantity -= 1;
                    _pauseMessage = $"Used {item.Name}: HP +{gained} ({item.Quantity} left).";
                    return;
                }
                case "mana_draught":
                {
                    if (_player.MaxMana <= 0)
                    {
                        _pauseMessage = $"{_player.CharacterClass.Name} has no mana pool.";
                        return;
                    }

                    if (_player.CurrentMana >= _player.MaxMana)
                    {
                        _pauseMessage = "MP is already full.";
                        return;
                    }

                    var restoreAmount = Math.Max(4, (int)Math.Ceiling(_player.MaxMana * 0.35));
                    var before = _player.CurrentMana;
                    _player.CurrentMana = Math.Min(_player.MaxMana, _player.CurrentMana + restoreAmount);
                    var gained = _player.CurrentMana - before;
                    if (gained <= 0)
                    {
                        _pauseMessage = "Mana Draught had no effect.";
                        return;
                    }

                    item.Quantity -= 1;
                    _pauseMessage = $"Used {item.Name}: MP +{gained} ({item.Quantity} left).";
                    return;
                }
                case "sharpening_oil":
                    item.Quantity -= 1;
                    _runMeleeBonus += 1;
                    _pauseMessage = "Sharpening Oil applied: +1 run melee damage.";
                    return;
                default:
                    _pauseMessage = $"{item.Name} has no configured effect yet.";
                    return;
            }
        }

        if (item.Quantity <= 0)
        {
            _pauseMessage = $"{item.Name} is unavailable.";
            return;
        }

        if (!item.Slot.HasValue)
        {
            _pauseMessage = $"{item.Name} has no valid equipment slot.";
            return;
        }

        var slot = item.Slot.Value;
        if (slot == EquipmentSlot.Armor && TryGetArmorCategory(item, out var armorCategory))
        {
            if (!CanEquipArmorCategory(_player, armorCategory, out var reason))
            {
                _pauseMessage = $"{item.Name} ({GetArmorStateLabel(armorCategory)}) cannot be equipped. {reason}";
                return;
            }
        }

        if (item.IsEquipped)
        {
            var currentSlotLabel = GetEquipmentSlotLabel(slot, item.EquippedSlotIndex);
            item.IsEquipped = false;
            item.EquippedSlotIndex = null;
            ApplyInventoryEquipmentBonus(item, equip: false);
            _pauseMessage = $"{item.Name} unequipped from {currentSlotLabel} slot.";
            return;
        }

        var targetSlotIndex = GetNextAvailableSlotIndex(slot);
        InventoryItem? replaced = null;
        if (!targetSlotIndex.HasValue)
        {
            var capacity = GetEquipmentSlotCapacity(slot);
            for (var i = 0; i < capacity; i++)
            {
                var occupied = GetEquippedItemInSlot(slot, i);
                if (occupied == null || ReferenceEquals(occupied, item))
                {
                    continue;
                }

                replaced = occupied;
                targetSlotIndex = i;
                break;
            }

            if (!targetSlotIndex.HasValue)
            {
                targetSlotIndex = 0;
            }
        }

        if (replaced != null)
        {
            replaced.IsEquipped = false;
            replaced.EquippedSlotIndex = null;
            ApplyInventoryEquipmentBonus(replaced, equip: false);
        }

        item.IsEquipped = true;
        item.EquippedSlotIndex = targetSlotIndex;
        ApplyInventoryEquipmentBonus(item, equip: true);
        var equippedSlotLabel = GetEquipmentSlotLabel(slot, item.EquippedSlotIndex);
        _pauseMessage = replaced != null
            ? $"{item.Name} equipped in {equippedSlotLabel} slot. {replaced.Name} was unequipped."
            : $"{item.Name} equipped in {equippedSlotLabel} slot.";
    }

    private void ApplyInventoryEquipmentBonus(InventoryItem item, bool equip)
    {
        var direction = equip ? 1 : -1;
        switch (item.Id)
        {
            case "warding_charm":
                _runDefenseBonus = Math.Max(0, _runDefenseBonus + direction);
                return;
            case "leather_jerkin":
                _runDefenseBonus = Math.Max(0, _runDefenseBonus + direction);
                return;
            case "brigandine_coat":
                _runDefenseBonus = Math.Max(0, _runDefenseBonus + direction * 2);
                return;
            case "plate_harness":
                _runDefenseBonus = Math.Max(0, _runDefenseBonus + direction * 3);
                return;
            case "hunter_cloak":
                _runCritBonus = Math.Max(0, _runCritBonus + direction);
                _runFleeBonus = Math.Max(0, _runFleeBonus + direction * 3);
                return;
            case "iron_helm":
                _runDefenseBonus = Math.Max(0, _runDefenseBonus + direction);
                return;
            case "focus_belt":
                _runSpellBonus = Math.Max(0, _runSpellBonus + direction);
                return;
            case "luck_ring":
                _runCritBonus = Math.Max(0, _runCritBonus + direction);
                return;
            case "guard_ring":
                _runDefenseBonus = Math.Max(0, _runDefenseBonus + direction);
                return;
        }
    }

    private void AdjustMasterVolume(int delta)
    {
        var previous = _settingsMasterVolume;
        _settingsMasterVolume = Math.Clamp(_settingsMasterVolume + delta, 0, 100);
        if (_settingsMasterVolume == previous)
        {
            _pauseMessage = $"Master volume remains {_settingsMasterVolume}%";
            return;
        }

        _pauseMessage = $"Master volume set to {_settingsMasterVolume}%";
    }

    private void CycleAccessibilityColorProfile(int direction)
    {
        var profiles = (AccessibilityColorProfile[])Enum.GetValues(typeof(AccessibilityColorProfile));
        var currentIndex = Array.IndexOf(profiles, _settingsAccessibilityColorProfile);
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        var step = direction < 0 ? -1 : 1;
        var nextIndex = (currentIndex + step + profiles.Length) % profiles.Length;
        _settingsAccessibilityColorProfile = profiles[nextIndex];
        ApplyAccessibilityPalette();
        _pauseMessage = $"Color profile set to {GetAccessibilityColorProfileLabel(_settingsAccessibilityColorProfile)}.";
    }

    private void ToggleHighContrastUi()
    {
        _settingsAccessibilityHighContrast = !_settingsAccessibilityHighContrast;
        ApplyAccessibilityPalette();
        _pauseMessage = _settingsAccessibilityHighContrast
            ? "High contrast UI enabled."
            : "High contrast UI disabled.";
    }

    private void ToggleOptionalConditionsMode()
    {
        _settingsOptionalConditionsEnabled = !_settingsOptionalConditionsEnabled;
        _pauseMessage = _settingsOptionalConditionsEnabled
            ? "Optional conditions enabled."
            : "Optional conditions disabled (effects suppressed).";
    }

    private static string GetAccessibilityColorProfileLabel(AccessibilityColorProfile profile)
    {
        return profile switch
        {
            AccessibilityColorProfile.DeuteranopiaFriendly => "Deuteranopia-Friendly",
            _ => "Default"
        };
    }

    private void OpenPauseInventoryMenu()
    {
        _pauseMenuView = PauseMenuView.Inventory;
        _pauseMenuIndex = 0;
        _pauseMessage = "ENTER uses consumables and equips gear by slot (rings use Ring 1/Ring 2; armor needs training).";
        ResetPauseConfirm();
    }

    private void OpenPauseSaveMenu()
    {
        RefreshPauseSaveEntries();
        _pauseMenuView = PauseMenuView.Save;
        _pauseMenuIndex = 0;
        _pauseMessage = "Choose a slot to save or overwrite.";
        ResetPauseConfirm();
    }

    private void OpenPauseLoadMenu()
    {
        RefreshPauseLoadEntries();
        _pauseMenuView = PauseMenuView.Load;
        _pauseMenuIndex = 0;
        _pauseMessage = _pauseLoadEntries.Count == 0
            ? "No save files found."
            : "Select a save file to load.";
        ResetPauseConfirm();
    }

    private void OpenPauseSettingsMenu()
    {
        _pauseMenuView = PauseMenuView.Settings;
        _pauseMenuIndex = 0;
        _pauseMessage = "Change fullscreen, volume, combat log detail, and accessibility.";
        ResetPauseConfirm();
    }

    private void OpenPauseAccessibilityMenu()
    {
        _pauseMenuView = PauseMenuView.Accessibility;
        _pauseMenuIndex = 0;
        _pauseMessage = $"Adjust accessibility and optional conditions. Purge cost: {GetConditionPurgeCostLabel()}.";
        ResetPauseConfirm();
    }

    private void BackToPauseRoot(int focusIndex)
    {
        _pauseMenuView = PauseMenuView.Root;
        _pauseMenuIndex = Math.Clamp(focusIndex, 0, PauseRootOptions.Length - 1);
        ResetPauseConfirm();
    }

    private void BackToPauseSettings(int focusIndex)
    {
        _pauseMenuView = PauseMenuView.Settings;
        _pauseMenuIndex = Math.Clamp(focusIndex, 0, PauseSettingsOptions.Length - 1);
        ResetPauseConfirm();
    }

    private void RefreshPauseSaveEntries()
    {
        _pauseSaveEntries.Clear();
        _pauseSaveEntries.AddRange(SaveStore.GetManualSlotSummaries());
    }

    private void RefreshPauseLoadEntries()
    {
        _pauseLoadEntries.Clear();
        _pauseLoadEntries.AddRange(SaveStore.GetAvailableLoadEntries());
    }

    private void TryManualSaveSlot(int slot)
    {
        if (!TryBuildSaveSnapshot("manual", out var snapshot, out var errorMessage))
        {
            _pauseMessage = errorMessage;
            return;
        }

        var result = SaveStore.SaveManualSlot(slot, snapshot);
        _pauseMessage = result.Message;
    }

    private void TryLoadEntry(SaveEntrySummary entry)
    {
        SaveOperationResult load;
        GameSaveSnapshot? snapshot;
        if (entry.IsAutosave)
        {
            load = SaveStore.LoadAutosave(out snapshot);
        }
        else
        {
            load = SaveStore.LoadManualSlot(entry.ManualSlot, out snapshot);
        }

        if (!load.Success || snapshot == null)
        {
            _pauseMessage = load.Message;
            return;
        }

        if (!TryRestoreFromSnapshot(snapshot, out var restoreError))
        {
            _pauseMessage = restoreError;
            return;
        }

        PushCombatLog(load.Message);
    }

    private void TryAutosaveCheckpoint(string reason)
    {
        if (!TryBuildSaveSnapshot("autosave", out var snapshot, out _)) return;
        var result = SaveStore.SaveAutosave(snapshot);
        if (!result.Success)
        {
            PushCombatLog($"Autosave failed ({reason}).");
        }
    }

    private bool TryBuildSaveSnapshot(string saveKind, out GameSaveSnapshot snapshot, out string errorMessage)
    {
        snapshot = new GameSaveSnapshot();
        errorMessage = string.Empty;

        if (_player == null)
        {
            errorMessage = "No active run to save.";
            return false;
        }

        var resumeState = _gameState == GameState.PauseMenu ? _pausedFromState : _gameState;
        if (!IsSaveEligibleState(resumeState))
        {
            errorMessage = "Save is only available during active gameplay.";
            return false;
        }

        if (!_player.IsAlive || resumeState == GameState.DeathScreen)
        {
            errorMessage = "Cannot save after character death.";
            return false;
        }

        if (_resolvingEnemyDeath || _defeatedEnemyPending != null || _enemyResolveAt > 0)
        {
            errorMessage = "Wait for combat resolution to finish before saving.";
            return false;
        }

        var enemies = new List<EnemySnapshot>();
        foreach (var enemy in _enemies)
        {
            var typeKey = ResolveEnemyTypeKey(enemy.Type);
            if (typeKey == null) continue;
            _enemyLootKits.TryGetValue(enemy, out var enemyLoot);
            enemies.Add(new EnemySnapshot
            {
                TypeKey = typeKey,
                X = enemy.X,
                Y = enemy.Y,
                SpawnX = enemy.SpawnX,
                SpawnY = enemy.SpawnY,
                CurrentHp = Math.Clamp(enemy.CurrentHp, 0, enemy.Type.MaxHp),
                LootName = enemyLoot?.Name ?? string.Empty,
                LootRarity = enemyLoot?.Rarity.ToString(),
                LootItemId = enemyLoot?.ItemId,
                LootItemQuantity = Math.Max(0, enemyLoot?.ItemQuantity ?? 0),
                EnemyAttackBonus = Math.Max(0, enemyLoot?.AttackBonus ?? 0)
            });
        }

        EnemySnapshot? currentEnemy = null;
        if (_currentEnemy != null)
        {
            var currentEnemyKey = ResolveEnemyTypeKey(_currentEnemy.Type);
            if (currentEnemyKey == null)
            {
                errorMessage = "Current combat enemy type is unknown and cannot be saved.";
                return false;
            }

            _enemyLootKits.TryGetValue(_currentEnemy, out var currentEnemyLoot);

            currentEnemy = new EnemySnapshot
            {
                TypeKey = currentEnemyKey,
                X = _currentEnemy.X,
                Y = _currentEnemy.Y,
                SpawnX = _currentEnemy.SpawnX,
                SpawnY = _currentEnemy.SpawnY,
                CurrentHp = Math.Clamp(_currentEnemy.CurrentHp, 0, _currentEnemy.Type.MaxHp),
                LootName = currentEnemyLoot?.Name ?? string.Empty,
                LootRarity = currentEnemyLoot?.Rarity.ToString(),
                LootItemId = currentEnemyLoot?.ItemId,
                LootItemQuantity = Math.Max(0, currentEnemyLoot?.ItemQuantity ?? 0),
                EnemyAttackBonus = Math.Max(0, currentEnemyLoot?.AttackBonus ?? 0)
            };
        }

        if (resumeState == GameState.Combat && (currentEnemy == null || currentEnemy.CurrentHp <= 0))
        {
            errorMessage = "Cannot save combat state without a valid active enemy.";
            return false;
        }

        var respawnDelay = 0.0;
        if (_respawnEnemiesAt > 0)
        {
            respawnDelay = Math.Max(0.0, _respawnEnemiesAt - Raylib.GetTime());
        }

        snapshot = new GameSaveSnapshot
        {
            SaveKind = saveKind,
            ResumeState = resumeState,
            Player = _player.CreateSnapshot(),
            PlayerSpriteId = _selectedPlayerSpriteId,
            Enemies = enemies,
            CurrentEnemy = resumeState == GameState.Combat ? currentEnemy : null,
            EnemyPoisoned = Math.Max(0, _enemyPoisoned),
            WarCryAvailable = _warCryAvailable,
            CombatLog = _combatLog.TakeLast(GetCombatLogBufferSize()).ToList(),
            RespawnDelaySeconds = respawnDelay,
            ClaimedRewardNodeIds = _claimedRewardNodeIds.OrderBy(id => id).ToList(),
            RunMeleeBonus = _runMeleeBonus,
            RunSpellBonus = _runSpellBonus,
            RunDefenseBonus = _runDefenseBonus,
            RunCritBonus = _runCritBonus,
            RunFleeBonus = _runFleeBonus,
            RunArchetype = _runArchetype.ToString(),
            RunRelic = _runRelic.ToString(),
            Phase3RouteChoice = _phase3RouteChoice.ToString(),
            Phase3RiskEventResolved = _phase3RiskEventResolved,
            Phase3XpPercentMod = _phase3XpPercentMod,
            Phase3EnemyAttackBonus = _phase3EnemyAttackBonus,
            Phase3EnemiesDefeated = _phase3EnemiesDefeated,
            Phase3PreSanctumRewardGranted = _phase3PreSanctumRewardGranted,
            Phase3RouteWaveSpawned = _phase3RouteWaveSpawned,
            Phase3SanctumWaveSpawned = _phase3SanctumWaveSpawned,
            MilestoneChoicesTaken = _milestoneChoicesTaken,
            MilestoneExecutionRank = _milestoneExecutionRank,
            MilestoneArcRank = _milestoneArcRank,
            MilestoneEscapeRank = _milestoneEscapeRank,
            BossDefeated = _bossDefeated,
            FloorCleared = _floorCleared,
            SettingsMasterVolume = _settingsMasterVolume,
            SettingsVerboseCombatLog = _settingsVerboseCombatLog,
            SettingsAccessibilityColorProfile = _settingsAccessibilityColorProfile.ToString(),
            SettingsAccessibilityHighContrast = _settingsAccessibilityHighContrast,
            SettingsOptionalConditionsEnabled = _settingsOptionalConditionsEnabled,
            CreationOriginCondition = _creationOriginCondition.ToString(),
            DungeonConditionEventsTriggered = _dungeonConditionEventsTriggered,
            MajorConditions = _activeMajorConditions.Select(condition => new MajorConditionSnapshot
            {
                Type = condition.Type.ToString(),
                Source = condition.Source
            }).ToList(),
            InventoryItems = _inventoryItems.Select(item => new InventoryItemSnapshot
            {
                Id = item.Id,
                Quantity = Math.Max(0, item.Quantity),
                IsEquipped = item.Kind == InventoryItemKind.Equipment && item.Quantity > 0 && item.IsEquipped,
                EquippedSlotIndex = item.Kind == InventoryItemKind.Equipment && item.Quantity > 0 && item.IsEquipped
                    ? item.EquippedSlotIndex
                    : null
            }).ToList(),
            GroundLoot = _groundLoot.Select(loot => new LootDropSnapshot
            {
                Id = loot.Id,
                Name = loot.Name,
                Rarity = loot.Rarity.ToString(),
                X = loot.X,
                Y = loot.Y,
                InventoryItemId = loot.InventoryItemId,
                InventoryItemQuantity = Math.Max(0, loot.InventoryItemQuantity)
            }).ToList()
        };
        return true;
    }

    private bool TryRestoreFromSnapshot(GameSaveSnapshot snapshot, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (snapshot.Player == null)
        {
            errorMessage = "Save file is missing player data.";
            return false;
        }

        if (snapshot.SchemaVersion > SaveStore.CurrentSchemaVersion)
        {
            errorMessage = "Save file was created by a newer build and cannot be loaded.";
            return false;
        }

        if (!Player.TryFromSnapshot(snapshot.Player, out var restoredPlayer, out var playerError) || restoredPlayer == null)
        {
            errorMessage = $"Player restore failed: {playerError}";
            return false;
        }

        var restoredEnemies = new List<Enemy>();
        var restoredEnemyLootKits = new Dictionary<Enemy, EnemyLootKit>();
        foreach (var enemySnapshot in snapshot.Enemies ?? new List<EnemySnapshot>())
        {
            if (!EnemyTypes.All.TryGetValue(enemySnapshot.TypeKey, out var type)) continue;

            var spawnX = enemySnapshot.SpawnX ?? enemySnapshot.X;
            var spawnY = enemySnapshot.SpawnY ?? enemySnapshot.Y;
            var enemy = new Enemy(enemySnapshot.X, enemySnapshot.Y, type, spawnX, spawnY)
            {
                CurrentHp = Math.Clamp(enemySnapshot.CurrentHp, 0, type.MaxHp)
            };
            restoredEnemies.Add(enemy);

            if (TryBuildEnemyLootKitFromSnapshot(enemySnapshot, out var restoredLoot))
            {
                restoredEnemyLootKits[enemy] = restoredLoot;
            }
            else
            {
                restoredEnemyLootKits[enemy] = CreateEnemyLootKit(enemy);
            }
        }

        var resumeState = IsCombatState(snapshot.ResumeState) ? GameState.Combat : GameState.Playing;
        Enemy? restoredCurrentEnemy = null;
        if (resumeState == GameState.Combat && snapshot.CurrentEnemy != null)
        {
            restoredCurrentEnemy = restoredEnemies.FirstOrDefault(e =>
                e.X == snapshot.CurrentEnemy.X &&
                e.Y == snapshot.CurrentEnemy.Y &&
                string.Equals(ResolveEnemyTypeKey(e.Type), snapshot.CurrentEnemy.TypeKey, StringComparison.Ordinal));

            if (restoredCurrentEnemy == null &&
                EnemyTypes.All.TryGetValue(snapshot.CurrentEnemy.TypeKey, out var currentEnemyType))
            {
                var spawnX = snapshot.CurrentEnemy.SpawnX ?? snapshot.CurrentEnemy.X;
                var spawnY = snapshot.CurrentEnemy.SpawnY ?? snapshot.CurrentEnemy.Y;
                restoredCurrentEnemy = new Enemy(snapshot.CurrentEnemy.X, snapshot.CurrentEnemy.Y, currentEnemyType, spawnX, spawnY)
                {
                    CurrentHp = Math.Clamp(snapshot.CurrentEnemy.CurrentHp, 0, currentEnemyType.MaxHp)
                };
                restoredEnemies.Add(restoredCurrentEnemy);
                if (TryBuildEnemyLootKitFromSnapshot(snapshot.CurrentEnemy, out var restoredLoot))
                {
                    restoredEnemyLootKits[restoredCurrentEnemy] = restoredLoot;
                }
                else
                {
                    restoredEnemyLootKits[restoredCurrentEnemy] = CreateEnemyLootKit(restoredCurrentEnemy);
                }
            }
        }

        if (resumeState == GameState.Combat && (restoredCurrentEnemy == null || restoredCurrentEnemy.CurrentHp <= 0))
        {
            errorMessage = "Saved combat state is invalid (missing active enemy).";
            return false;
        }

        _player = restoredPlayer;
        _selectedGenderIndex = Array.FindIndex(Genders, gender => gender == restoredPlayer.Gender);
        if (_selectedGenderIndex < 0) _selectedGenderIndex = 0;
        _selectedRaceIndex = Array.FindIndex(Races, race => race == restoredPlayer.Race);
        if (_selectedRaceIndex < 0) _selectedRaceIndex = 0;
        var restoredSprite = string.IsNullOrWhiteSpace(snapshot.PlayerSpriteId)
            ? ResolveDefaultSpriteForRaceAndGender(restoredPlayer.Race, restoredPlayer.Gender)
            : snapshot.PlayerSpriteId;
        SetPlayerAppearanceBySpriteId(restoredSprite);
        _enemies = restoredEnemies;
        RebuildEnemyAi();
        _enemyLootKits.Clear();
        foreach (var enemy in _enemies)
        {
            if (restoredEnemyLootKits.TryGetValue(enemy, out var kit))
            {
                _enemyLootKits[enemy] = kit;
            }
            else
            {
                _enemyLootKits[enemy] = CreateEnemyLootKit(enemy);
            }
        }

        _currentEnemy = resumeState == GameState.Combat ? restoredCurrentEnemy : null;
        _enemyPoisoned = Math.Max(0, snapshot.EnemyPoisoned);
        _warCryAvailable = snapshot.WarCryAvailable;
        ResetEncounterContext();
        if (_currentEnemy != null)
        {
            BeginEncounterFromSeed(_currentEnemy);
            BeginPlayerCombatTurn();
        }
        _resolvingEnemyDeath = false;
        _enemyResolveAt = -1;
        _defeatedEnemyPending = null;
        _respawnEnemiesAt = snapshot.RespawnDelaySeconds > 0 && _enemies.Count == 0
            ? Raylib.GetTime() + snapshot.RespawnDelaySeconds
            : -1;
        _claimedRewardNodeIds.Clear();
        foreach (var nodeId in snapshot.ClaimedRewardNodeIds ?? new List<string>())
        {
            _claimedRewardNodeIds.Add(nodeId);
        }

        _runMeleeBonus = snapshot.RunMeleeBonus;
        _runSpellBonus = snapshot.RunSpellBonus;
        _runDefenseBonus = snapshot.RunDefenseBonus;
        _runCritBonus = snapshot.RunCritBonus;
        _runFleeBonus = snapshot.RunFleeBonus;
        _runArchetype = Enum.TryParse<RunArchetype>(snapshot.RunArchetype, ignoreCase: true, out var parsedRunArchetype)
            ? parsedRunArchetype
            : RunArchetype.None;
        _runRelic = Enum.TryParse<RunRelic>(snapshot.RunRelic, ignoreCase: true, out var parsedRunRelic)
            ? parsedRunRelic
            : RunRelic.None;
        _phase3RouteChoice = Enum.TryParse<Phase3RouteChoice>(snapshot.Phase3RouteChoice, ignoreCase: true, out var parsedRouteChoice)
            ? parsedRouteChoice
            : Phase3RouteChoice.None;
        _phase3RiskEventResolved = snapshot.Phase3RiskEventResolved;
        _phase3XpPercentMod = Math.Clamp(snapshot.Phase3XpPercentMod, -90, 200);
        _phase3EnemyAttackBonus = Math.Clamp(snapshot.Phase3EnemyAttackBonus, 0, 10);
        _phase3EnemiesDefeated = Math.Max(0, snapshot.Phase3EnemiesDefeated);
        _phase3PreSanctumRewardGranted = snapshot.Phase3PreSanctumRewardGranted;
        _phase3RouteWaveSpawned = snapshot.Phase3RouteWaveSpawned || _phase3RouteChoice != Phase3RouteChoice.None;
        _phase3SanctumWaveSpawned = snapshot.Phase3SanctumWaveSpawned;
        _phase3SanctumLockNoticeShown = false;
        if (_enemies.Any(enemy =>
                enemy.IsAlive &&
                string.Equals(ResolveEnemyTypeKey(enemy.Type), "goblin_general", StringComparison.Ordinal)))
        {
            _phase3SanctumWaveSpawned = true;
        }
        _currentFloorZone = ResolveFloorMacroZone(restoredPlayer.X, restoredPlayer.Y);
        _milestoneChoicesTaken = Math.Max(0, snapshot.MilestoneChoicesTaken);
        _milestoneExecutionRank = Math.Clamp(snapshot.MilestoneExecutionRank, 0, MaxEffectiveDoctrineRank);
        _milestoneArcRank = Math.Clamp(snapshot.MilestoneArcRank, 0, MaxEffectiveDoctrineRank);
        _milestoneEscapeRank = Math.Clamp(snapshot.MilestoneEscapeRank, 0, MaxEffectiveDoctrineRank);
        ResetRelicCombatTriggers();
        ResetMilestoneCombatTriggers();
        _bossDefeated = snapshot.BossDefeated;
        _floorCleared = snapshot.FloorCleared;
        _settingsMasterVolume = Math.Clamp(snapshot.SettingsMasterVolume, 0, 100);
        _settingsVerboseCombatLog = snapshot.SettingsVerboseCombatLog;
        _settingsAccessibilityColorProfile = Enum.TryParse<AccessibilityColorProfile>(
            snapshot.SettingsAccessibilityColorProfile,
            ignoreCase: true,
            out var parsedAccessibilityProfile)
            ? parsedAccessibilityProfile
            : AccessibilityColorProfile.Default;
        _settingsAccessibilityHighContrast = snapshot.SettingsAccessibilityHighContrast;
        _settingsOptionalConditionsEnabled = snapshot.SettingsOptionalConditionsEnabled;
        _creationOriginCondition = Enum.TryParse<CreationConditionPreset>(
            snapshot.CreationOriginCondition,
            ignoreCase: true,
            out var parsedCreationCondition)
            ? parsedCreationCondition
            : CreationConditionPreset.None;
        _selectedCreationConditionIndex = Array.FindIndex(
            CreationConditionOptions,
            option => option.Id == _creationOriginCondition);
        if (_selectedCreationConditionIndex < 0)
        {
            _selectedCreationConditionIndex = 0;
        }
        _dungeonConditionEventsTriggered = Math.Max(0, snapshot.DungeonConditionEventsTriggered);
        _activeMajorConditions.Clear();
        foreach (var condition in snapshot.MajorConditions ?? new List<MajorConditionSnapshot>())
        {
            if (!Enum.TryParse<MajorConditionType>(condition.Type, ignoreCase: true, out var parsedConditionType))
            {
                continue;
            }

            if (_activeMajorConditions.Any(existing => existing.Type == parsedConditionType))
            {
                continue;
            }

            _activeMajorConditions.Add(new MajorConditionState
            {
                Type = parsedConditionType,
                Source = string.IsNullOrWhiteSpace(condition.Source) ? "Unknown" : condition.Source
            });
        }
        ApplyAccessibilityPalette();
        _groundLoot.Clear();
        foreach (var loot in snapshot.GroundLoot ?? new List<LootDropSnapshot>())
        {
            var rarity = Enum.TryParse<LootRarity>(loot.Rarity, ignoreCase: true, out var parsedRarity)
                ? parsedRarity
                : LootRarity.Common;
            if (IsWallOrSealed(loot.X, loot.Y))
            {
                continue;
            }

            var itemId = string.IsNullOrWhiteSpace(loot.InventoryItemId) ? null : loot.InventoryItemId;
            var itemQty = Math.Max(0, loot.InventoryItemQuantity);
            if (itemId == null || itemQty <= 0)
            {
                // Compatibility: older saves may contain abstract loot values without item payload.
                itemId = "health_potion";
                itemQty = 1;
            }

            _groundLoot.Add(new GroundLoot
            {
                Id = string.IsNullOrWhiteSpace(loot.Id) ? $"loot_{Guid.NewGuid():N}" : loot.Id,
                Name = string.IsNullOrWhiteSpace(loot.Name) ? "Recovered Supplies" : loot.Name,
                Rarity = rarity,
                X = loot.X,
                Y = loot.Y,
                InventoryItemId = itemId,
                InventoryItemQuantity = itemQty
            });
        }

        _activeRewardNode = null;
        _selectedRewardOptionIndex = 0;
        ResetLootPickupState();
        InitializeRunInventory();
        if (snapshot.InventoryItems != null && snapshot.InventoryItems.Count > 0)
        {
            foreach (var savedItem in snapshot.InventoryItems)
            {
                if (string.IsNullOrWhiteSpace(savedItem.Id)) continue;
                var item = GetInventoryItem(savedItem.Id);
                if (item == null) continue;
                item.Quantity = Math.Max(0, savedItem.Quantity);
                item.IsEquipped = item.Kind == InventoryItemKind.Equipment &&
                                  item.Quantity > 0 &&
                                  savedItem.IsEquipped;
                item.EquippedSlotIndex = item.IsEquipped ? savedItem.EquippedSlotIndex : null;
            }
        }
        NormalizeEquippedEquipmentState(adjustRunBonuses: true);

        _combatLog.Clear();
        foreach (var line in (snapshot.CombatLog ?? new List<string>())
                     .Where(line => !string.IsNullOrWhiteSpace(line))
                     .TakeLast(GetCombatLogBufferSize()))
        {
            _combatLog.Add(line);
        }

        if (_combatLog.Count == 0 && _currentEnemy != null)
        {
            _combatLog.Add($"A {_currentEnemy.Type.Name} blocks your path!");
        }

        ResetRunSelectionsAfterLoad();
        _pausedFromState = resumeState;
        _pauseMenuView = PauseMenuView.Root;
        _pauseMenuIndex = 0;
        _pauseMessage = string.Empty;
        ResetPauseConfirm();
        ResetCameraTracking();
        _gameState = _floorCleared ? GameState.VictoryScreen : resumeState;

        if (_player != null && !_player.IsAlive)
        {
            HandlePlayerDeath();
        }

        return true;
    }

    private void ResetRunSelectionsAfterLoad()
    {
        _selectedActionIndex = 0;
        _selectedCombatSkillIndex = 0;
        _selectedSpellIndex = 0;
        _selectedRewardOptionIndex = 0;
        _skillMenuOffset = 0;
        _spellMenuOffset = 0;
        _characterSheetScroll = 0;
        _nextMoveAt = -1;
        ClearPendingCombatSpell();
        ResetLootPickupState();
        ClearRewardMessage();
    }

    private static bool IsSaveEligibleState(GameState state)
    {
        return GameStateRules.IsSaveEligibleState(state);
    }

    private static bool IsCombatState(GameState state)
    {
        return state == GameState.CombatSpellTargeting || GameStateRules.IsCombatState(state);
    }

    private static string? ResolveEnemyTypeKey(EnemyType type)
    {
        foreach (var kv in EnemyTypes.All)
        {
            if (ReferenceEquals(kv.Value, type) ||
                string.Equals(kv.Value.Name, type.Name, StringComparison.OrdinalIgnoreCase))
            {
                return kv.Key;
            }
        }

        return null;
    }

    private static bool IsGoblinEnemyKey(string? enemyKey)
    {
        return !string.IsNullOrWhiteSpace(enemyKey) &&
               enemyKey.StartsWith("goblin", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildEncounterEnemyCombatantId(Enemy enemy)
    {
        var enemyKey = ResolveEnemyTypeKey(enemy.Type);
        var normalizedKey = string.IsNullOrWhiteSpace(enemyKey)
            ? enemy.Type.Name.Replace(' ', '_').ToLowerInvariant()
            : enemyKey;
        return $"{normalizedKey}:{enemy.SpawnX},{enemy.SpawnY}";
    }

    private static bool AreEncounterAllies(string? leftEnemyKey, string? rightEnemyKey)
    {
        if (IsGoblinEnemyKey(leftEnemyKey) && IsGoblinEnemyKey(rightEnemyKey))
        {
            return true;
        }

        return string.Equals(leftEnemyKey, rightEnemyKey, StringComparison.Ordinal);
    }

    private void ResetEncounterContext()
    {
        ClearPendingCombatSpell();
        _encounterActive = false;
        _encounterRound = 1;
        _encounterEnemies.Clear();
        _encounterTurnOrder.Clear();
        _encounterTurnIndex = 0;
        _encounterCurrentCombatantId = string.Empty;
        _selectedEncounterTargetIndex = -1;
        _packEnemiesRemainingAfterCurrent = 0;
        _combatMoveModeActive = false;
        _combatMovePointsMax = 0;
        _combatMovePointsRemaining = 0;
    }

    private void SyncEncounterCurrentCombatantId()
    {
        if (_encounterTurnOrder.Count == 0)
        {
            _encounterTurnIndex = 0;
            _encounterCurrentCombatantId = string.Empty;
            return;
        }

        _encounterTurnIndex = Math.Clamp(_encounterTurnIndex, 0, _encounterTurnOrder.Count - 1);
        _encounterCurrentCombatantId = _encounterTurnOrder[_encounterTurnIndex].Id;
    }

    private void RebuildEncounterTurnOrder(string? preferredCombatantId = null)
    {
        var preservedCombatantId = string.IsNullOrWhiteSpace(preferredCombatantId)
            ? _encounterCurrentCombatantId
            : preferredCombatantId;
        _encounterTurnOrder.Clear();
        if (!_encounterActive || _player == null)
        {
            _encounterTurnIndex = 0;
            _encounterCurrentCombatantId = string.Empty;
            return;
        }

        var candidates = new List<EncounterInitiativeParticipant>
        {
            new(
                Id: "player",
                Kind: EncounterCombatantKind.Player,
                InitiativeModifier: _player.Mod(StatName.Dexterity),
                StableOrder: 0)
        };

        var stableOrder = 1;
        foreach (var enemy in _encounterEnemies.Where(enemy => enemy.IsAlive))
        {
            candidates.Add(new EncounterInitiativeParticipant(
                Id: BuildEncounterEnemyCombatantId(enemy),
                Kind: EncounterCombatantKind.Enemy,
                InitiativeModifier: Math.Max(0, enemy.Type.Attack / 4),
                StableOrder: stableOrder));
            stableOrder += 1;
        }

        var initiativeSeed = HashCode.Combine(
            _encounterEnemies.Count,
            _encounterRound,
            _phase3EnemiesDefeated,
            _player.Level,
            _player.Stats.Dexterity);

        _encounterTurnOrder.AddRange(EncounterInitiative.BuildOrder(candidates, initiativeSeed));
        if (_encounterTurnOrder.Count == 0)
        {
            _encounterTurnIndex = 0;
            _encounterCurrentCombatantId = string.Empty;
            return;
        }

        var preservedIndex = string.IsNullOrWhiteSpace(preservedCombatantId)
            ? -1
            : _encounterTurnOrder.FindIndex(slot =>
                string.Equals(slot.Id, preservedCombatantId, StringComparison.Ordinal));
        if (preservedIndex >= 0)
        {
            _encounterTurnIndex = preservedIndex;
        }
        else
        {
            _encounterTurnIndex = 0;
        }

        SyncEncounterCurrentCombatantId();
    }

    private void PruneEncounterTurnOrder()
    {
        if (_encounterTurnOrder.Count == 0)
        {
            _encounterCurrentCombatantId = string.Empty;
            _encounterTurnIndex = 0;
            return;
        }

        var preservedCombatantId = _encounterCurrentCombatantId;
        var aliveEnemyIds = _encounterEnemies
            .Where(enemy => enemy.IsAlive)
            .Select(BuildEncounterEnemyCombatantId)
            .ToHashSet(StringComparer.Ordinal);

        _encounterTurnOrder.RemoveAll(slot =>
            slot.Kind switch
            {
                EncounterCombatantKind.Player => _player == null || !_player.IsAlive,
                EncounterCombatantKind.Enemy => !aliveEnemyIds.Contains(slot.Id),
                _ => true
            });

        if (!string.IsNullOrWhiteSpace(preservedCombatantId))
        {
            var preservedIndex = _encounterTurnOrder.FindIndex(slot =>
                string.Equals(slot.Id, preservedCombatantId, StringComparison.Ordinal));
            if (preservedIndex >= 0)
            {
                _encounterTurnIndex = preservedIndex;
            }
        }

        SyncEncounterCurrentCombatantId();
    }

    private void SetEncounterTurnToPlayer()
    {
        if (_encounterTurnOrder.Count == 0)
        {
            return;
        }

        var playerIndex = _encounterTurnOrder.FindIndex(slot => slot.Kind == EncounterCombatantKind.Player);
        if (playerIndex >= 0)
        {
            _encounterTurnIndex = playerIndex;
            SyncEncounterCurrentCombatantId();
        }
    }

    private void BeginPlayerCombatTurn()
    {
        if (_player == null)
        {
            _selectedEncounterTargetIndex = -1;
            _currentEnemy = null;
            _combatMoveModeActive = false;
            _combatMovePointsMax = 0;
            _combatMovePointsRemaining = 0;
            return;
        }

        TryJoinEncounterReinforcements();
        PruneEncounterTurnOrder();
        SetEncounterTurnToPlayer();
        SyncEncounterTargetSelection(preferCurrentEnemy: true);
        _combatMoveModeActive = false;
        _nextMoveAt = -1;
        _combatMovePointsMax = GetPlayerCombatMoveBudget(_player);
        _combatMovePointsRemaining = _combatMovePointsMax;
        _packEnemiesRemainingAfterCurrent = Math.Max(0, _encounterEnemies.Count(enemy =>
            enemy.IsAlive &&
            !ReferenceEquals(enemy, _currentEnemy)));
    }

    private void SetEncounterTurnToEnemy(Enemy enemy)
    {
        if (_encounterTurnOrder.Count == 0)
        {
            return;
        }

        var targetId = BuildEncounterEnemyCombatantId(enemy);
        var enemyIndex = _encounterTurnOrder.FindIndex(slot =>
            slot.Kind == EncounterCombatantKind.Enemy &&
            string.Equals(slot.Id, targetId, StringComparison.Ordinal));

        if (enemyIndex >= 0)
        {
            _encounterTurnIndex = enemyIndex;
            SyncEncounterCurrentCombatantId();
        }

        var aliveEnemies = GetAliveEncounterEnemies();
        var selectedIndex = aliveEnemies.FindIndex(candidate => ReferenceEquals(candidate, enemy));
        if (selectedIndex >= 0)
        {
            _selectedEncounterTargetIndex = selectedIndex;
            _currentEnemy = aliveEnemies[selectedIndex];
        }
    }

    private void AdvanceEncounterTurn()
    {
        if (_encounterTurnOrder.Count == 0)
        {
            _encounterCurrentCombatantId = string.Empty;
            _encounterTurnIndex = 0;
            return;
        }

        var previousIndex = _encounterTurnIndex;
        _encounterTurnIndex = EncounterInitiative.AdvanceTurnIndex(_encounterTurnIndex, _encounterTurnOrder.Count);
        if (_encounterTurnIndex <= previousIndex)
        {
            _encounterRound += 1;
        }

        SyncEncounterCurrentCombatantId();
    }

    private void BeginEncounterFromSeed(Enemy seedEnemy)
    {
        _encounterActive = true;
        _encounterRound = 1;
        _encounterEnemies.Clear();
        _encounterEnemies.Add(seedEnemy);

        var seedEnemyKey = ResolveEnemyTypeKey(seedEnemy.Type);
        foreach (var enemy in _enemies)
        {
            if (ReferenceEquals(enemy, seedEnemy) || !enemy.IsAlive)
            {
                continue;
            }

            var enemyKey = ResolveEnemyTypeKey(enemy.Type);
            if (!AreEncounterAllies(seedEnemyKey, enemyKey))
            {
                continue;
            }

            var distance = Math.Abs(enemy.X - seedEnemy.X) + Math.Abs(enemy.Y - seedEnemy.Y);
            if (distance > GoblinPackJoinDistanceTiles)
            {
                continue;
            }

            if (!HasLineOfSight(seedEnemy.X, seedEnemy.Y, enemy.X, enemy.Y))
            {
                continue;
            }

            _encounterEnemies.Add(enemy);
            if (_encounterEnemies.Count >= GoblinPackMaxEncounterSize)
            {
                break;
            }
        }

        _packEnemiesRemainingAfterCurrent = Math.Max(0, _encounterEnemies.Count - 1);
        RebuildEncounterTurnOrder();
    }

    private int TryJoinEncounterReinforcements()
    {
        if (!_encounterActive || _player == null || _gameState == GameState.DeathScreen)
        {
            return 0;
        }

        if (!EncounterReinforcementRules.HasOpenEncounterSlot(_encounterEnemies.Count, GoblinPackMaxEncounterSize))
        {
            return 0;
        }

        var aliveEncounterMembers = _encounterEnemies
            .Where(enemy => enemy.IsAlive)
            .Select(enemy => new EncounterReinforcementMember(
                X: enemy.X,
                Y: enemy.Y,
                EnemyKey: ResolveEnemyTypeKey(enemy.Type)))
            .ToList();
        if (aliveEncounterMembers.Count == 0)
        {
            return 0;
        }

        var joiners = new List<Enemy>();
        foreach (var candidate in _enemies)
        {
            if (!candidate.IsAlive)
            {
                continue;
            }

            if (_encounterEnemies.Any(existing => ReferenceEquals(existing, candidate)))
            {
                continue;
            }

            if (joiners.Any(existing => ReferenceEquals(existing, candidate)))
            {
                continue;
            }

            var candidateMember = new EncounterReinforcementMember(
                X: candidate.X,
                Y: candidate.Y,
                EnemyKey: ResolveEnemyTypeKey(candidate.Type));
            if (!EncounterReinforcementRules.IsCandidateEligible(
                    candidateMember,
                    aliveEncounterMembers,
                    EncounterReinforcementJoinDistanceTiles,
                    AreEncounterAllies,
                    HasLineOfSight))
            {
                continue;
            }

            joiners.Add(candidate);
            if (!EncounterReinforcementRules.HasOpenEncounterSlot(
                    _encounterEnemies.Count + joiners.Count,
                    GoblinPackMaxEncounterSize))
            {
                break;
            }
        }

        if (joiners.Count == 0)
        {
            return 0;
        }

        var preservedCombatantId = _encounterCurrentCombatantId;
        var mergedSlots = _encounterTurnOrder.ToList();
        var nextStableOrder = mergedSlots.Count > 0
            ? mergedSlots.Max(slot => slot.StableOrder) + 1
            : 1;

        foreach (var joiner in joiners)
        {
            _encounterEnemies.Add(joiner);
            if (!_enemyLootKits.ContainsKey(joiner))
            {
                _enemyLootKits[joiner] = CreateEnemyLootKit(joiner);
            }

            var initiativeMod = Math.Max(0, joiner.Type.Attack / 4);
            var rollSeed = HashCode.Combine(
                joiner.SpawnX,
                joiner.SpawnY,
                _encounterRound,
                nextStableOrder,
                _phase3EnemiesDefeated);
            var roll = new Random(rollSeed).Next(1, 21);
            var score = roll + initiativeMod;
            mergedSlots.Add(new EncounterInitiativeSlot(
                Id: BuildEncounterEnemyCombatantId(joiner),
                Kind: EncounterCombatantKind.Enemy,
                Roll: roll,
                InitiativeScore: score,
                StableOrder: nextStableOrder));
            nextStableOrder += 1;

            PushCombatLog($"{joiner.Type.Name} joins the encounter!");
        }

        if (_encounterTurnOrder.Count == 0)
        {
            RebuildEncounterTurnOrder(preferredCombatantId: preservedCombatantId);
        }
        else
        {
            _encounterTurnOrder.Clear();
            _encounterTurnOrder.AddRange(EncounterInitiative.SortSlots(mergedSlots));
            if (!string.IsNullOrWhiteSpace(preservedCombatantId))
            {
                var preservedIndex = _encounterTurnOrder.FindIndex(slot =>
                    string.Equals(slot.Id, preservedCombatantId, StringComparison.Ordinal));
                if (preservedIndex >= 0)
                {
                    _encounterTurnIndex = preservedIndex;
                }
            }

            SyncEncounterCurrentCombatantId();
        }

        _packEnemiesRemainingAfterCurrent = Math.Max(0, _encounterEnemies.Count(enemy =>
            enemy.IsAlive &&
            !ReferenceEquals(enemy, _currentEnemy)));
        SyncEncounterTargetSelection(preferCurrentEnemy: true);
        return joiners.Count;
    }

    private void RebuildEncounterEnemiesFromCurrent(Enemy currentEnemy)
    {
        _encounterEnemies.RemoveAll(enemy => !enemy.IsAlive);
        if (_encounterEnemies.Count > 0)
        {
            PruneEncounterTurnOrder();
            return;
        }

        BeginEncounterFromSeed(currentEnemy);
    }

    private bool TryPromoteNextGoblinPackEnemy(Enemy defeatedEnemy)
    {
        if (!_encounterActive || _player == null || !_player.IsAlive || _gameState != GameState.Combat)
        {
            return false;
        }

        _encounterEnemies.RemoveAll(enemy => !enemy.IsAlive || ReferenceEquals(enemy, defeatedEnemy));
        TryJoinEncounterReinforcements();
        PruneEncounterTurnOrder();
        if (_encounterTurnOrder.Count == 0)
        {
            RebuildEncounterTurnOrder(preferredCombatantId: "player");
            SetEncounterTurnToPlayer();
        }

        var aliveEnemies = GetAliveEncounterEnemies();
        if (aliveEnemies.Count == 0)
        {
            ResetEncounterContext();
            return false;
        }

        if (_currentEnemy == null || !_currentEnemy.IsAlive || ReferenceEquals(_currentEnemy, defeatedEnemy))
        {
            _currentEnemy = aliveEnemies[0];
        }

        if (!_enemyLootKits.ContainsKey(_currentEnemy))
        {
            _enemyLootKits[_currentEnemy] = CreateEnemyLootKit(_currentEnemy);
        }

        _enemyPoisoned = 0;
        _packEnemiesRemainingAfterCurrent = Math.Max(0, aliveEnemies.Count(enemy =>
            !ReferenceEquals(enemy, _currentEnemy)));
        SyncEncounterTargetSelection(preferCurrentEnemy: true);
        DoEnemyAttack();
        return true;
    }

    private static bool TryBuildEnemyLootKitFromSnapshot(EnemySnapshot snapshot, out EnemyLootKit kit)
    {
        kit = null!;
        if (string.IsNullOrWhiteSpace(snapshot.LootName))
        {
            return false;
        }

        var rarity = Enum.TryParse<LootRarity>(snapshot.LootRarity, ignoreCase: true, out var parsedRarity)
            ? parsedRarity
            : LootRarity.Common;

        kit = new EnemyLootKit
        {
            Name = snapshot.LootName,
            Rarity = rarity,
            ItemId = string.IsNullOrWhiteSpace(snapshot.LootItemId) ? null : snapshot.LootItemId,
            ItemQuantity = Math.Max(0, snapshot.LootItemQuantity),
            AttackBonus = Math.Max(0, snapshot.EnemyAttackBonus),
            UsedConsumableThisFight = false
        };
        return true;
    }

    private void PushCombatLog(string line)
    {
        _combatLog.Add(line);
        var bufferSize = GetCombatLogBufferSize();
        if (_combatLog.Count > bufferSize)
        {
            _combatLog.RemoveRange(0, _combatLog.Count - bufferSize);
        }
    }

    private void DrawPausedScene()
    {
        if (_pausedFromState == GameState.Combat && _currentEnemy != null)
        {
            DrawWorld();
            DrawCombatUi();
            return;
        }

        DrawWorld();
    }

    private void DrawPauseMenu()
    {
        var w = Raylib.GetScreenWidth();
        var h = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(0, 0, w, h, new Color(0, 0, 0, 178));

        var panelX = UiLayout.PausePanelInsetX;
        var panelY = UiLayout.PausePanelInsetY;
        var panelW = w - UiLayout.PausePanelInsetX * 2;
        var panelH = h - UiLayout.PausePanelInsetY * 2;
        DrawPanel(panelX, panelY, panelW, panelH, ColPanel, ColBorder);

        var title = _pauseMenuView switch
        {
            PauseMenuView.Inventory => "Inventory",
            PauseMenuView.Save => "Save Game",
            PauseMenuView.Load => "Load Game",
            PauseMenuView.Settings => "Settings",
            PauseMenuView.Accessibility => "Accessibility",
            _ => "Paused"
        };
        DrawCenteredText(title, w / 2, panelY + 18, 42, ColYellow);

        var listX = panelX + 24;
        var listW = panelW - 48;
        var listY = panelY + 74;
        var footerHint = "UP/DOWN select | ENTER confirm | ESC resume";

        if (_pauseMenuView == PauseMenuView.Root)
        {
            for (var i = 0; i < PauseRootOptions.Length; i++)
            {
                var rowY = listY + i * 34;
                var selected = i == _pauseMenuIndex;
                DrawMenuRow(listX + 4, rowY, listW - 8, 30, selected);
                DrawCenteredText(PauseRootOptions[i], w / 2, rowY + 4, 21, selected ? ColYellow : ColWhite);
            }
        }
        else if (_pauseMenuView == PauseMenuView.Inventory)
        {
            footerHint = "UP/DOWN select | ENTER use/equip | ESC back";

            for (var i = 0; i < _inventoryItems.Count; i++)
            {
                var rowY = listY + i * 58;
                var selected = i == _pauseMenuIndex;
                var item = _inventoryItems[i];
                DrawMenuRow(listX, rowY, listW, 54, selected);
                var armorTypeLabel = item.Slot == EquipmentSlot.Armor && TryGetArmorCategory(item, out var armorCategory)
                    ? $"{GetEquipmentSlotLabel(item.Slot.Value)} ({GetArmorStateLabel(armorCategory)})"
                    : null;
                var typeLabel = item.Kind == InventoryItemKind.Consumable
                    ? "Consumable"
                    : item.Slot.HasValue
                        ? armorTypeLabel ?? $"{GetEquipmentSlotLabel(item.Slot.Value)} Gear"
                        : "Equipment";
                var statusLabel = item.Kind == InventoryItemKind.Consumable
                    ? $"x{item.Quantity}"
                    : item.Quantity <= 0
                        ? "Unavailable"
                        : item.IsEquipped
                            ? item.Slot.HasValue
                                ? $"Equipped ({GetEquipmentSlotLabel(item.Slot.Value, item.EquippedSlotIndex)})"
                                : "Equipped"
                            : "Ready";
                if (item.Kind == InventoryItemKind.Equipment &&
                    item.Quantity > 0 &&
                    !item.IsEquipped &&
                    item.Slot == EquipmentSlot.Armor &&
                    TryGetArmorCategory(item, out var blockedArmorCategory) &&
                    _player != null &&
                    !CanEquipArmorCategory(_player, blockedArmorCategory, out _))
                {
                    statusLabel = $"Blocked ({GetArmorStateLabel(blockedArmorCategory)} training)";
                }
                Raylib.DrawText($"{item.Name} [{typeLabel}]", listX + 12, rowY + 7, 20, selected ? ColYellow : ColWhite);
                Raylib.DrawText(statusLabel, listX + listW - 190, rowY + 8, 18, selected ? ColYellow : ColSkyBlue);
                Raylib.DrawText(item.Description, listX + 12, rowY + 31, 14, ColLightGray);
            }

            var backIndex = _inventoryItems.Count;
            var backY = listY + Math.Max(1, _inventoryItems.Count) * 58 + 6;
            var backSelected = _pauseMenuIndex == backIndex;
            DrawMenuRow(listX, backY, listW, 36, backSelected);
            DrawCenteredText("Back", w / 2, backY + 7, 20, backSelected ? ColYellow : ColWhite);
        }
        else if (_pauseMenuView == PauseMenuView.Save)
        {
            footerHint = "UP/DOWN select | ENTER save | ESC back";

            for (var i = 0; i < _pauseSaveEntries.Count; i++)
            {
                var rowY = listY + i * 62;
                var selected = i == _pauseMenuIndex;
                var entry = _pauseSaveEntries[i];
                DrawMenuRow(listX, rowY, listW, 56, selected);
                Raylib.DrawText(entry.Label, listX + 12, rowY + 8, 22, selected ? ColYellow : ColWhite);
                Raylib.DrawText(entry.Detail, listX + 12, rowY + 33, 14, ColLightGray);
            }

            var backIndex = _pauseSaveEntries.Count;
            var backY = listY + _pauseSaveEntries.Count * 62 + 8;
            var backSelected = _pauseMenuIndex == backIndex;
            DrawMenuRow(listX, backY, listW, 36, backSelected);
            DrawCenteredText("Back", w / 2, backY + 7, 20, backSelected ? ColYellow : ColWhite);
        }
        else if (_pauseMenuView == PauseMenuView.Load)
        {
            footerHint = "UP/DOWN select | ENTER load | ESC back";

            if (_pauseLoadEntries.Count == 0)
            {
                DrawWrappedText("No save files available yet. Create one from Save Game.", listX + 10, listY + 8, listW - 20, 18, ColLightGray);
            }
            else
            {
                for (var i = 0; i < _pauseLoadEntries.Count; i++)
                {
                    var rowY = listY + i * 62;
                    var selected = i == _pauseMenuIndex;
                    var entry = _pauseLoadEntries[i];
                    DrawMenuRow(listX, rowY, listW, 56, selected);
                    Raylib.DrawText(entry.Label, listX + 12, rowY + 8, 22, selected ? ColYellow : ColWhite);
                    Raylib.DrawText(entry.Detail, listX + 12, rowY + 33, 14, ColLightGray);
                }
            }

            var backIndex = _pauseLoadEntries.Count;
            var backY = listY + Math.Max(1, _pauseLoadEntries.Count) * 62 + 8;
            var backSelected = _pauseMenuIndex == backIndex;
            DrawMenuRow(listX, backY, listW, 36, backSelected);
            DrawCenteredText("Back", w / 2, backY + 7, 20, backSelected ? ColYellow : ColWhite);
        }
        else if (_pauseMenuView == PauseMenuView.Settings)
        {
            footerHint = "UP/DOWN select | LEFT/RIGHT adjust | ENTER confirm | ESC back";

            for (var i = 0; i < PauseSettingsOptions.Length; i++)
            {
                var rowY = listY + i * 56;
                var selected = i == _pauseMenuIndex;
                DrawMenuRow(listX, rowY, listW, 48, selected);
                var value = i switch
                {
                    0 => Raylib.IsWindowFullscreen() ? "Fullscreen" : "Windowed",
                    1 => $"{_settingsMasterVolume}%",
                    2 => _settingsVerboseCombatLog ? "Verbose" : "Compact",
                    3 => "Open",
                    _ => string.Empty
                };
                Raylib.DrawText(PauseSettingsOptions[i], listX + 12, rowY + 12, 20, selected ? ColYellow : ColWhite);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    Raylib.DrawText(value, listX + listW - 170, rowY + 12, 19, selected ? ColYellow : ColSkyBlue);
                }
            }
        }
        else if (_pauseMenuView == PauseMenuView.Accessibility)
        {
            footerHint = "UP/DOWN select | LEFT/RIGHT adjust | ENTER confirm | ESC back";

            for (var i = 0; i < PauseAccessibilityOptions.Length; i++)
            {
                var rowY = listY + i * 56;
                var selected = i == _pauseMenuIndex;
                DrawMenuRow(listX, rowY, listW, 48, selected);
                var value = i switch
                {
                    0 => GetAccessibilityColorProfileLabel(_settingsAccessibilityColorProfile),
                    1 => _settingsAccessibilityHighContrast ? "On" : "Off",
                    2 => _settingsOptionalConditionsEnabled ? "On" : "Off",
                    3 => _activeMajorConditions.Count > 0 ? "Ready" : "No Conditions",
                    _ => string.Empty
                };
                Raylib.DrawText(PauseAccessibilityOptions[i], listX + 12, rowY + 12, 20, selected ? ColYellow : ColWhite);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    Raylib.DrawText(value, listX + listW - 260, rowY + 12, 19, selected ? ColYellow : ColSkyBlue);
                }
            }
        }

        if (_pauseConfirmAction != PauseConfirmAction.None)
        {
            footerHint = "ENTER confirm | ESC cancel";
        }

        if (!string.IsNullOrWhiteSpace(_pauseMessage))
        {
            DrawFooterBar(panelX + 20, panelY + panelH - 72, panelW - 40, 26);
            DrawCenteredText(_pauseMessage, w / 2, panelY + panelH - 67, 16, ColLightGray);
        }

        DrawFooterBar(panelX + 12, panelY + panelH - 36, panelW - 24, 24);
        DrawCenteredText(footerHint, w / 2, panelY + panelH - 31, 15, ColLightGray);
    }

    private void DrawStartMenu()
    {
        var screenW = Raylib.GetScreenWidth();
        var screenH = Raylib.GetScreenHeight();
        var centerX = screenW / 2;
        var panelW = UiLayout.StartPanelWidth;
        var panelH = UiLayout.StartPanelHeight;
        var panelX = GetCenteredPanelX(screenW, panelW);
        var panelY = GetCenteredPanelY(screenH, panelH);
        DrawPanel(panelX, panelY, panelW, panelH, ColPanel, ColBorder);
        var startOptions = GetStartMenuOptions();
        if (startOptions.Count == 0) startOptions.Add("New Game");
        _startMenuIndex = Math.Clamp(_startMenuIndex, 0, startOptions.Count - 1);

        const int titleSize = 52;
        const int subtitleSize = 20;
        var titleY = panelY + 18;
        var subtitleY = titleY + titleSize + 14;
        DrawCenteredText("Dungeon Escape", centerX, titleY, titleSize, ColYellow);
        DrawCenteredText("A dark fantasy dungeon crawler", centerX, subtitleY, subtitleSize, ColLightGray);

        var rowSpacing = startOptions.Count > 3 ? 38 : 48;
        var menuStartY = subtitleY + (startOptions.Count > 3 ? 42 : 56);
        for (var i = 0; i < startOptions.Count; i++)
        {
            var selected = i == _startMenuIndex;
            var rowY = menuStartY + i * rowSpacing;
            DrawMenuRow(centerX - 130, rowY - 8, 260, 38, selected);
            DrawCenteredText(startOptions[i], centerX, rowY, 24, selected ? ColYellow : ColWhite);
        }

        if (!string.IsNullOrWhiteSpace(_startMenuMessage))
        {
            DrawFooterBar(panelX + 12, panelY + panelH - 72, panelW - 24, 24);
            DrawCenteredText(_startMenuMessage, centerX, panelY + panelH - 67, 14, ColLightGray);
        }

        DrawFooterBar(panelX + 8, panelY + panelH - 42, panelW - 16, 30);
        DrawCenteredText("Use UP/DOWN and ENTER", centerX, panelY + panelH - 35, 16, ColLightGray);
    }

    private void DrawHelpMenu()
    {
        var screenW = Raylib.GetScreenWidth();
        var screenH = Raylib.GetScreenHeight();
        var centerX = screenW / 2;
        var panelW = UiLayout.HelpPanelWidth;
        var panelH = UiLayout.HelpPanelHeight;
        var panelX = GetCenteredPanelX(screenW, panelW);
        var panelY = GetCenteredPanelY(screenH, panelH);
        DrawPanel(panelX, panelY, panelW, panelH, ColPanel, ColBorder);
        DrawCenteredText("How To Play", centerX, panelY + 16, 38, ColYellow);

        var tips = new[]
        {
            "Move: WASD or Arrow Keys",
            "Open Character Sheet: C",
            "Pause Menu: ESC (save/load and quit)",
            "Combat: choose action and press ENTER",
            "Skills and Spells are available in combat",
            "Level-up grants stats, feats, spells, and one skill"
        };

        for (var i = 0; i < tips.Length; i++)
        {
            var y = panelY + 80 + i * 42;
            DrawPanel(panelX + 30, y - 6, panelW - 60, 34, ColPanelAlt, ColBorder);
            Raylib.DrawText(tips[i], panelX + 42, y + 2, 20, ColLightGray);
        }

        DrawFooterBar(panelX + 18, panelY + panelH - 48, panelW - 36, 30);
        DrawCenteredText("Press ENTER or ESC to return", centerX, panelY + panelH - 42, 18, ColLightGray);
    }

    private void DrawCharacterCreationHub()
    {
        var w = Raylib.GetScreenWidth();
        var h = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(0, 0, w, h, ColInk);

        const int outerPad = 14;
        const int topPad = 18;
        const int navW = 120;
        const int gap = 10;
        const int summaryW = 154;
        var panelH = h - topPad * 2;
        var mainW = w - (outerPad * 2 + navW + summaryW + gap * 2);

        var navX = outerPad;
        var mainX = navX + navW + gap;
        var summaryX = mainX + mainW + gap;
        var panelY = topPad;

        DrawPanel(navX, panelY, navW, panelH, ColPanelSoft, ColBorder);
        DrawPanel(mainX, panelY, mainW, panelH, ColPanel, ColBorder);
        DrawPanel(summaryX, panelY, summaryW, panelH, ColPanelSoft, ColBorder);

        DrawCenteredText("Character Creator", w / 2, 10, 26, ColYellow);

        for (var i = 0; i < CreationSections.Length; i++)
        {
            var y = panelY + 54 + i * 60;
            var selected = i == _creationSectionIndex;
            var ready = IsCreationSectionReady(i);
            DrawMenuRow(navX + 8, y - 8, navW - 16, 46, selected);
            Raylib.DrawText($"{i + 1}. {CreationSections[i]}", navX + 14, y - 1, 19, selected ? ColYellow : ColLightGray);
            Raylib.DrawText(ready ? "Ready" : "Pending", navX + 16, y + 20, 12, ready ? ColGreen : ColRed);
        }

        DrawCenteredText("1-6 jump sections", navX + navW / 2, panelY + panelH - 78, 13, ColGray);
        DrawCenteredText("LEFT/RIGHT switch", navX + navW / 2, panelY + panelH - 60, 13, ColGray);
        DrawCenteredText("A/D outside Name", navX + navW / 2, panelY + panelH - 42, 13, ColGray);
        DrawCenteredText("ESC back to menu", navX + navW / 2, panelY + panelH - 24, 13, ColGray);

        if (_player == null)
        {
            DrawCenteredText("Creation data unavailable.", mainX + mainW / 2, panelY + 120, 24, ColRed);
            return;
        }

        var contentX = mainX + 14;
        var contentW = mainW - 28;
        var yCursor = panelY + 20;
        Raylib.DrawText(CreationSections[_creationSectionIndex], contentX, yCursor, 30, ColSkyBlue);
        yCursor += 42;

        switch (_creationSectionIndex)
        {
            case 0:
            {
                var nameSelected = _selectedCreationIdentityIndex == 0;
                var genderSelected = _selectedCreationIdentityIndex == 1;
                var raceSelected = _selectedCreationIdentityIndex == 2;
                var appearanceSelected = _selectedCreationIdentityIndex == 3;
                var conditionSelected = _selectedCreationIdentityIndex == 4;
                var selectedCondition = CreationConditionOptions[_selectedCreationConditionIndex];

                DrawMenuRow(contentX, yCursor, contentW, 44, nameSelected);
                var nameValue = string.IsNullOrWhiteSpace(_pendingName) ? "(empty)" : _pendingName;
                var nameSuffix = nameSelected && Raylib.GetTime() % 1 < 0.5 ? "_" : string.Empty;
                Raylib.DrawText($"Name: {nameValue}{nameSuffix}", contentX + 12, yCursor + 12, 20, ColWhite);
                yCursor += 56;

                DrawMenuRow(contentX, yCursor, contentW, 44, genderSelected);
                Raylib.DrawText($"Gender: {Genders[_selectedGenderIndex]}", contentX + 12, yCursor + 12, 20, ColWhite);
                yCursor += 62;

                DrawMenuRow(contentX, yCursor, contentW, 44, raceSelected);
                Raylib.DrawText($"Race: {Races[_selectedRaceIndex]}", contentX + 12, yCursor + 12, 20, ColWhite);
                yCursor += 62;

                DrawMenuRow(contentX, yCursor, contentW, 44, appearanceSelected);
                Raylib.DrawText($"Appearance: {GetSelectedAppearanceLabel()}", contentX + 12, yCursor + 12, 20, ColWhite);
                yCursor += 62;

                DrawMenuRow(contentX, yCursor, contentW, 44, conditionSelected);
                Raylib.DrawText($"Origin Condition: {selectedCondition.Label}", contentX + 12, yCursor + 12, 20, ColWhite);
                yCursor += 52;
                DrawWrappedText(selectedCondition.Description, contentX + 10, yCursor, contentW - 20, 14, ColLightGray);
                yCursor += 46;

                DrawWrappedText(
                    $"UP/DOWN switch fields. Type in Name. LEFT/RIGHT adjusts Gender/Race/Appearance/Condition. {RaceDescriptions[Races[_selectedRaceIndex]]}",
                    contentX,
                    yCursor,
                    contentW,
                    16,
                    ColLightGray);
                break;
            }
            case 1:
            {
                DrawWrappedText("Changing class resets stats/spells and revalidates your selected starting feat.", contentX, yCursor, contentW, 16, ColGray);
                yCursor += 30;
                for (var i = 0; i < CharacterClasses.All.Count; i++)
                {
                    var selected = i == _selectedClassIndex;
                    var rowY = yCursor + i * 34;
                    DrawMenuRow(contentX, rowY, contentW, 30, selected);
                    Raylib.DrawText(CharacterClasses.All[i].Name, contentX + 10, rowY + 6, 20, selected ? ColYellow : ColLightGray);
                }

                var classInfoY = yCursor + CharacterClasses.All.Count * 34 + 10;
                var chosenClass = CharacterClasses.All[_selectedClassIndex];
                Raylib.DrawText(chosenClass.Name, contentX, classInfoY, 24, ColYellow);
                DrawWrappedText(chosenClass.Description, contentX, classInfoY + 30, contentW, 16, ColLightGray);
                break;
            }
            case 2:
            {
                DrawWrappedText($"Points remaining: {_creationPointsRemaining}/6", contentX, yCursor, contentW, 20, ColWhite);
                yCursor += 32;
                var menuCount = StatOrder.Length + 2;
                for (var i = 0; i < menuCount; i++)
                {
                    var selected = i == _creationSelectionIndex;
                    var rowY = yCursor + i * 34;
                    DrawMenuRow(contentX, rowY, contentW, 30, selected);
                    if (i < StatOrder.Length)
                    {
                        var stat = StatOrder[i];
                        var allocated = _creationAllocatedStats[i];
                        Raylib.DrawText(
                            $"{stat,-12}: {_player.Stats.Get(stat),2}   (+{allocated})",
                            contentX + 10,
                            rowY + 6,
                            20,
                            selected ? ColYellow : ColLightGray);
                    }
                    else if (i == StatOrder.Length)
                    {
                        Raylib.DrawText("Undo Last Stat Point", contentX + 10, rowY + 6, 20, selected ? ColYellow : ColLightGray);
                    }
                    else
                    {
                        Raylib.DrawText("Reset Stat Allocation", contentX + 10, rowY + 6, 20, selected ? ColYellow : ColLightGray);
                    }
                }

                DrawWrappedText("Tip: finish all points to unlock a green Review check. Use Undo for quick correction.", contentX, yCursor + menuCount * 34 + 8, contentW, 14, ColGray);
                break;
            }
            case 3:
            {
                var spellPickLeft = _player.SpellPickPoints;
                Raylib.DrawText($"Spell picks remaining: {spellPickLeft}", contentX, yCursor, 20, ColSkyBlue);
                yCursor += 28;

                var undoRowIndex = GetCreationSpellUndoRowIndex();
                var resetRowIndex = GetCreationSpellResetRowIndex();
                var menuCount = GetCreationSpellMenuCount();
                EnsureSpellLearnSelectionVisible(menuCount);

                var start = _spellLearnMenuOffset;
                var end = Math.Min(menuCount, start + SpellLearnVisibleCount);
                for (var i = start; i < end; i++)
                {
                    var selected = i == _selectedSpellLearnIndex;
                    var rowY = yCursor + (i - start) * 60;
                    DrawMenuRow(contentX, rowY, contentW, 56, selected);

                    if (i == undoRowIndex)
                    {
                        Raylib.DrawText("Undo Last Spell Pick", contentX + 10, rowY + 16, 20, selected ? ColYellow : ColLightGray);
                        Raylib.DrawText("Reverts the most recent spell selection", contentX + 10, rowY + 36, 13, ColGray);
                        continue;
                    }

                    if (i == resetRowIndex)
                    {
                        Raylib.DrawText("Reset Spell Picks", contentX + 10, rowY + 16, 20, selected ? ColYellow : ColLightGray);
                        Raylib.DrawText("Undo selected spells and rebuild picks", contentX + 10, rowY + 36, 13, ColGray);
                        continue;
                    }

                    var spell = _creationLearnableSpells[i];
                    var tier = spell.IsCantrip ? "Cantrip" : $"L{spell.SpellLevel}";
                    var canLearn = _player.CanLearnSpell(spell, out var blockReason);
                    var spellAlreadyKnown = _player.KnowsSpell(spell.Id);
                    var manuallySelected = _creationChosenSpellIds.Contains(spell.Id);
                    var status = canLearn
                        ? "Learnable now (ENTER to add)"
                        : spellAlreadyKnown
                            ? manuallySelected ? "Selected (ENTER to remove)" : "Known by default (free cantrip)"
                            : $"Locked: {blockReason}";
                    var nameColor = canLearn ? (selected ? ColYellow : ColWhite) : spellAlreadyKnown ? ColSkyBlue : ColGray;
                    var statusColor = canLearn ? ColGreen : spellAlreadyKnown ? (manuallySelected ? ColYellow : ColSkyBlue) : ColRed;

                    Raylib.DrawText($"{spell.Name} ({tier})", contentX + 10, rowY + 6, 18, nameColor);
                    Raylib.DrawText(spell.Description, contentX + 10, rowY + 24, 14, ColLightGray);
                    Raylib.DrawText(status, contentX + 10, rowY + 40, 13, statusColor);
                }

                if (menuCount == 0)
                {
                    if (!_player.IsCasterClass)
                    {
                        DrawWrappedText("This class has no spell progression in the current 1-6 scope.", contentX, yCursor + 6, contentW, 18, ColGray);
                    }
                    else if (_player.SpellPickPoints <= 0)
                    {
                        DrawWrappedText("No spell picks available at this level. Level up to unlock additional picks.", contentX, yCursor + 6, contentW, 18, ColGray);
                    }
                    else
                    {
                        DrawWrappedText("No learnable spells right now. Review locked spells below.", contentX, yCursor + 6, contentW, 18, ColGray);
                        var previewY = yCursor + 42;
                        foreach (var spell in _player.GetClassSpells().Take(3))
                        {
                            var canLearn = _player.CanLearnSpell(spell, out var reason);
                            var tier = spell.IsCantrip ? "Cantrip" : $"L{spell.SpellLevel}";
                            Raylib.DrawText(
                                $"{spell.Name} ({tier}): {(canLearn ? "Learnable" : reason)}",
                                contentX + 4,
                                previewY,
                                15,
                                canLearn ? ColGreen : ColGray);
                            previewY += 22;
                        }
                    }
                }
                else
                {
                    DrawWrappedText("Tip: inspect locked spells for exact reasons. Use Undo/Reset if you want to revise picks.", contentX, yCursor + SpellLearnVisibleCount * 60 + 8, contentW, 14, ColGray);
                }

                break;
            }
            case 4:
            {
                var featPicksLeft = _player.FeatPoints;
                Raylib.DrawText($"Starting feat picks remaining: {featPicksLeft}", contentX, yCursor, 20, ColYellow);
                yCursor += 28;

                var menuCount = _creationFeatChoices.Count;
                EnsureCreationFeatSelectionVisible(menuCount);
                var start = _creationFeatMenuOffset;
                var end = Math.Min(menuCount, start + CreationFeatVisibleCount);
                for (var i = start; i < end; i++)
                {
                    var selected = i == _selectedCreationFeatIndex;
                    var rowY = yCursor + (i - start) * 70;
                    DrawMenuRow(contentX, rowY, contentW, 66, selected);

                    var feat = _creationFeatChoices[i];
                    var canLearn = _player.CanLearnFeat(feat, out var blockReason);
                    var chosen = _player.HasFeat(feat.Id) && _creationChosenFeatIds.Contains(feat.Id);
                    var status = canLearn
                        ? "Learnable now (ENTER to select)"
                        : chosen
                            ? "Selected (ENTER to remove)"
                            : $"Locked: {blockReason}";
                    var nameColor = canLearn ? (selected ? ColYellow : ColWhite) : chosen ? ColSkyBlue : ColGray;
                    var statusColor = canLearn ? ColGreen : chosen ? ColSkyBlue : ColRed;

                    Raylib.DrawText(feat.Name, contentX + 10, rowY + 6, 19, nameColor);
                    Raylib.DrawText($"Lv {feat.MinLevel}+ | {feat.Effect}", contentX + 10, rowY + 28, 14, ColLightGray);
                    Raylib.DrawText(status, contentX + 10, rowY + 46, 13, statusColor);
                }

                if (menuCount == 0)
                {
                    DrawWrappedText("No feats available in the current catalog.", contentX, yCursor + 8, contentW, 18, ColGray);
                }
                else
                {
                    DrawWrappedText("Tip: choose one starting feat. Locked rows show exact prerequisite blockers.", contentX, yCursor + CreationFeatVisibleCount * 70 + 8, contentW, 14, ColGray);
                }
                break;
            }
            case 5:
            {
                var readyName = IsCreationNameReady();
                var readyStats = IsCreationStatsReady();
                var readySpells = IsCreationSpellsReady();
                var readyFeats = IsCreationFeatsReady();
                var rows = new[]
                {
                    $"Name set: {(readyName ? "Yes" : "No")}",
                    $"Stats allocated: {(readyStats ? "Yes" : "No")}",
                    $"Spell picks complete: {(readySpells ? "Yes" : "No")}",
                    $"Starting feat selected: {(readyFeats ? "Yes" : "No")}"
                };

                for (var i = 0; i < rows.Length; i++)
                {
                    var ok = i switch
                    {
                        0 => readyName,
                        1 => readyStats,
                        2 => readySpells,
                        _ => readyFeats
                    };
                    DrawPanel(contentX, yCursor + i * 44, contentW, 38, ColPanelAlt, ColBorder);
                    Raylib.DrawText(rows[i], contentX + 12, yCursor + 10 + i * 44, 20, ok ? ColGreen : ColRed);
                }

                var startY = yCursor + rows.Length * 44 + 24;
                var readyToStart = readyName && readyStats && readySpells && readyFeats;
                DrawMenuRow(contentX, startY, contentW, 54, readyToStart);
                DrawCenteredText("Start Adventure (ENTER)", contentX + contentW / 2, startY + 15, 24, readyToStart ? ColYellow : ColGray);
                DrawWrappedText("You can still move back to Identity/Class/Stats/Spells/Feats before confirming.", contentX, startY + 64, contentW, 15, ColLightGray);
                break;
            }
        }

        // Summary panel
        var sy = panelY + 16;
        Raylib.DrawText("Summary", summaryX + 10, sy, 24, ColYellow);
        sy += 34;
        Raylib.DrawText(IsCreationReady() ? "Build Ready" : "Build Incomplete", summaryX + 10, sy, 14, IsCreationReady() ? ColGreen : ColRed);
        sy += 22;
        var shownName = string.IsNullOrWhiteSpace(_pendingName) ? "(unset)" : _pendingName.Trim();
        Raylib.DrawText($"Name: {shownName}", summaryX + 10, sy, 16, ColWhite);
        sy += 22;
        Raylib.DrawText($"Gender: {Genders[_selectedGenderIndex]}", summaryX + 10, sy, 16, ColWhite);
        sy += 22;
        Raylib.DrawText($"Race: {Races[_selectedRaceIndex]}", summaryX + 10, sy, 16, ColWhite);
        sy += 22;
        Raylib.DrawText($"Appearance: {GetSelectedAppearanceLabel()}", summaryX + 10, sy, 16, ColWhite);
        sy += 22;
        Raylib.DrawText($"Class: {CharacterClasses.All[_selectedClassIndex].Name}", summaryX + 10, sy, 16, ColWhite);
        sy += 22;
        Raylib.DrawText($"Origin Cond: {CreationConditionOptions[_selectedCreationConditionIndex].Label}", summaryX + 10, sy, 15, ColLightGray);
        sy += 24;

        Raylib.DrawText($"HP: {_player.CurrentHp}/{_player.MaxHp}", summaryX + 10, sy, 16, ColGreen);
        sy += 20;
        Raylib.DrawText($"MP: {_player.CurrentMana}/{_player.MaxMana}", summaryX + 10, sy, 16, ColSkyBlue);
        sy += 22;
        Raylib.DrawText($"Stat points left: {_creationPointsRemaining}", summaryX + 10, sy, 16, ColWhite);
        sy += 20;
        Raylib.DrawText($"Spell picks left: {_player.SpellPickPoints}", summaryX + 10, sy, 16, ColWhite);
        sy += 20;
        Raylib.DrawText($"Feat picks left: {_player.FeatPoints}", summaryX + 10, sy, 16, ColWhite);
        sy += 26;

        Raylib.DrawText("Checks", summaryX + 10, sy, 18, ColLightGray);
        sy += 22;
        Raylib.DrawText(IsCreationNameReady() ? "Name: OK" : "Name: Missing", summaryX + 10, sy, 14, IsCreationNameReady() ? ColGreen : ColRed);
        sy += 18;
        Raylib.DrawText(IsCreationStatsReady() ? "Stats: OK" : "Stats: Incomplete", summaryX + 10, sy, 14, IsCreationStatsReady() ? ColGreen : ColRed);
        sy += 18;
        Raylib.DrawText(IsCreationSpellsReady() ? "Spells: OK" : "Spells: Incomplete", summaryX + 10, sy, 14, IsCreationSpellsReady() ? ColGreen : ColRed);
        sy += 18;
        Raylib.DrawText(IsCreationFeatsReady() ? "Feats: OK" : "Feats: Incomplete", summaryX + 10, sy, 14, IsCreationFeatsReady() ? ColGreen : ColRed);
        sy += 24;

        Raylib.DrawText("Core Stats", summaryX + 10, sy, 18, ColLightGray);
        sy += 24;
        foreach (var stat in StatOrder)
        {
            Raylib.DrawText($"{stat}: {_player.Stats.Get(stat)}", summaryX + 10, sy, 15, ColLightGray);
            sy += 18;
        }

        sy += 8;
        Raylib.DrawText("Picked Feat", summaryX + 10, sy, 18, ColYellow);
        sy += 22;
        var selectedFeat = _player.Feats.FirstOrDefault();
        if (selectedFeat == null)
        {
            Raylib.DrawText("None", summaryX + 10, sy, 15, ColGray);
            sy += 18;
        }
        else
        {
            Raylib.DrawText(selectedFeat.Name, summaryX + 10, sy, 14, ColLightGray);
            sy += 16;
        }

        sy += 8;
        Raylib.DrawText("Picked Spells", summaryX + 10, sy, 18, ColSkyBlue);
        sy += 22;
        var knownSpells = _player.GetKnownSpells();
        if (knownSpells.Count == 0)
        {
            Raylib.DrawText("None", summaryX + 10, sy, 15, ColGray);
            sy += 18;
        }
        else
        {
            foreach (var spell in knownSpells.Take(6))
            {
                var tier = spell.IsCantrip ? "C" : $"L{spell.SpellLevel}";
                Raylib.DrawText($"{tier} {spell.Name}", summaryX + 10, sy, 14, ColLightGray);
                sy += 16;
            }
        }

        DrawFooterBar(outerPad, h - 34, w - outerPad * 2, 22);
        var footer = string.IsNullOrWhiteSpace(_creationMessage) ? GetCreationSectionHint() : _creationMessage;
        Raylib.DrawText(footer, outerPad + 8, h - 30, 14, ColLightGray);
    }

    private void DrawNameInput()
    {
        var centerX = Raylib.GetScreenWidth() / 2;
        var centerY = Raylib.GetScreenHeight() / 2;
        DrawCenteredText("Enter Your Name", centerX, centerY - 64, 28, new Color(245, 200, 66, 255));

        var nameField = _pendingName + (Raylib.GetTime() % 1 < 0.5 ? "_" : string.Empty);
        DrawCenteredText(nameField, centerX, centerY - 10, 24, ColWhite);
        DrawCenteredText("Type letters/numbers/space. ENTER to continue.", centerX, centerY + 40, 18, ColGray);
    }

    private void DrawGenderSelection()
    {
        var centerX = Raylib.GetScreenWidth() / 2;
        DrawCenteredText("Choose Your Origin", centerX, 90, 28, new Color(245, 200, 66, 255));

        for (var i = 0; i < Genders.Length; i++)
        {
            var selected = i == _selectedGenderIndex;
            var y = 170 + i * 70;
            var color = selected ? ColYellow : ColLightGray;
            var marker = selected ? "> " : "  ";
            DrawCenteredText($"{marker}{Genders[i]}", centerX, y, 24, color);
            DrawCenteredText(GenderDescriptions[Genders[i]], centerX, y + 25, 18, selected ? ColGreen : ColGray);
        }
    }

    private void DrawClassSelection()
    {
        var w = Raylib.GetScreenWidth();
        var centerX = w / 2;
        var listX = Math.Max(20, w / 8);
        var listW = Math.Clamp(w / 3, 220, 320);
        var detailsX = listX + listW + 24;
        var detailsW = Math.Max(260, w - detailsX - 24);
        DrawCenteredText("Choose Class", centerX, 30, 28, new Color(245, 200, 66, 255));

        for (var i = 0; i < CharacterClasses.All.Count; i++)
        {
            var c = CharacterClasses.All[i];
            var selected = i == _selectedClassIndex;
            var y = 70 + i * 26;
            var marker = selected ? "> " : "  ";
            Raylib.DrawText($"{marker}{c.Name}", listX, y, 22, selected ? ColYellow : ColLightGray);
        }

        var chosen = CharacterClasses.All[_selectedClassIndex];
        Raylib.DrawText(chosen.Name, detailsX, 90, 30, new Color(245, 200, 66, 255));
        DrawWrappedText(chosen.Description, detailsX, 130, detailsW, 20, ColLightGray);
    }

    private void DrawCharacterStatAllocation()
    {
        if (_player == null) return;

        var w = Raylib.GetScreenWidth();
        var h = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(0, 0, w, h, new Color(0, 0, 0, 180));
        DrawCenteredText("Allocate Starting Stats", w / 2, 40, 34, ColYellow);
        DrawCenteredText($"Points remaining: {_creationPointsRemaining}", w / 2, 82, 24, ColWhite);

        for (var i = 0; i < StatOrder.Length; i++)
        {
            var selected = _creationSelectionIndex == i;
            var marker = selected ? "> " : "  ";
            var stat = StatOrder[i];
            DrawCenteredText(
                $"{marker}{stat}: {_player.Stats.Get(stat)}",
                w / 2,
                138 + i * 36,
                24,
                selected ? ColYellow : ColWhite);
        }

        var confirmSelected = _creationSelectionIndex == StatOrder.Length;
        var confirmColor = _creationPointsRemaining == 0 ? (confirmSelected ? ColGreen : ColWhite) : ColGray;
        DrawCenteredText($"{(confirmSelected ? "> " : "  ")}Start Adventure", w / 2, 138 + StatOrder.Length * 36 + 12, 26, confirmColor);
        DrawCenteredText("Spend all points, then start. ESC to go back.", w / 2, h - 42, 18, ColGray);
    }

    private static int GetWorldPixelWidth()
    {
        return GameMap.MapWidthTiles * GameMap.TileSize;
    }

    private static int GetWorldPixelHeight()
    {
        return GameMap.MapHeightTiles * GameMap.TileSize;
    }

    private void ResetCameraTracking()
    {
        _cameraTargetInitialized = false;
        _cameraTarget = default;
    }

    private static float Damp(float from, float to, float speed, float dt)
    {
        if (dt <= 0f) return to;
        var alpha = 1f - MathF.Exp(-MathF.Max(0f, speed) * dt);
        return from + (to - from) * alpha;
    }

    private static System.Numerics.Vector2 ClampCameraTargetToWorld(
        System.Numerics.Vector2 target,
        int screenW,
        int screenH,
        int worldW,
        int worldH)
    {
        var halfViewW = screenW / 2f;
        var halfViewH = screenH / 2f;
        var clampedX = Math.Clamp(target.X, halfViewW, Math.Max(halfViewW, worldW - halfViewW));
        var clampedY = Math.Clamp(target.Y, halfViewH, Math.Max(halfViewH, worldH - halfViewH));
        return new System.Numerics.Vector2(clampedX, clampedY);
    }

    private Camera2D BuildWorldCamera()
    {
        var screenW = Raylib.GetScreenWidth();
        var screenH = Raylib.GetScreenHeight();
        var worldW = GetWorldPixelWidth();
        var worldH = GetWorldPixelHeight();

        var camera = new Camera2D
        {
            Offset = new System.Numerics.Vector2(screenW / 2f, screenH / 2f),
            Rotation = 0f,
            Zoom = 1f
        };

        if (_player == null)
        {
            _cameraTargetInitialized = false;
            camera.Target = new System.Numerics.Vector2(worldW / 2f, worldH / 2f);
            return camera;
        }

        var playerWorldX = _player.X * GameMap.TileSize + GameMap.TileSize / 2f;
        var playerWorldY = _player.Y * GameMap.TileSize + GameMap.TileSize / 2f;

        var desiredTarget = new System.Numerics.Vector2(playerWorldX, playerWorldY);
        desiredTarget = ClampCameraTargetToWorld(desiredTarget, screenW, screenH, worldW, worldH);

        if (!_cameraTargetInitialized)
        {
            _cameraTarget = desiredTarget;
            _cameraTargetInitialized = true;
        }
        else
        {
            var deadZoneHalfX = Math.Max(4f, GameTuning.CameraDeadZoneHalfWidthTiles * GameMap.TileSize);
            var deadZoneHalfY = Math.Max(4f, GameTuning.CameraDeadZoneHalfHeightTiles * GameMap.TileSize);
            var shiftedTarget = _cameraTarget;

            if (playerWorldX > shiftedTarget.X + deadZoneHalfX)
            {
                shiftedTarget.X = playerWorldX - deadZoneHalfX;
            }
            else if (playerWorldX < shiftedTarget.X - deadZoneHalfX)
            {
                shiftedTarget.X = playerWorldX + deadZoneHalfX;
            }

            if (playerWorldY > shiftedTarget.Y + deadZoneHalfY)
            {
                shiftedTarget.Y = playerWorldY - deadZoneHalfY;
            }
            else if (playerWorldY < shiftedTarget.Y - deadZoneHalfY)
            {
                shiftedTarget.Y = playerWorldY + deadZoneHalfY;
            }

            shiftedTarget = ClampCameraTargetToWorld(shiftedTarget, screenW, screenH, worldW, worldH);
            var dt = Math.Clamp(Raylib.GetFrameTime(), 0f, 0.25f);
            _cameraTarget.X = Damp(_cameraTarget.X, shiftedTarget.X, GameTuning.CameraSmoothness, dt);
            _cameraTarget.Y = Damp(_cameraTarget.Y, shiftedTarget.Y, GameTuning.CameraSmoothness, dt);
        }

        _cameraTarget = ClampCameraTargetToWorld(_cameraTarget, screenW, screenH, worldW, worldH);
        camera.Target = _cameraTarget;
        return camera;
    }

    private void DrawRewardNodes()
    {
        foreach (var node in _rewardNodes)
        {
            var claimed = _claimedRewardNodeIds.Contains(node.Id);
            if (claimed) continue;

            var px = node.X * GameMap.TileSize;
            var py = node.Y * GameMap.TileSize;
            Raylib.DrawRectangle(px + 6, py + 6, GameMap.TileSize - 12, GameMap.TileSize - 12, VisualTheme.RewardNodeFill);
            Raylib.DrawRectangleLines(px + 5, py + 5, GameMap.TileSize - 10, GameMap.TileSize - 10, VisualTheme.RewardNodeEdge);
        }
    }

    private void DrawGroundLoot()
    {
        foreach (var loot in _groundLoot)
        {
            var px = loot.X * GameMap.TileSize;
            var py = loot.Y * GameMap.TileSize;
            var fill = loot.Rarity switch
            {
                LootRarity.Rare => new Color(188, 130, 248, 220),
                LootRarity.Uncommon => new Color(84, 176, 124, 220),
                _ => new Color(220, 192, 120, 220)
            };
            var edge = loot.Rarity switch
            {
                LootRarity.Rare => new Color(232, 188, 255, 255),
                LootRarity.Uncommon => new Color(156, 226, 180, 255),
                _ => new Color(244, 224, 152, 255)
            };

            Raylib.DrawRectangle(px + 9, py + 9, GameMap.TileSize - 18, GameMap.TileSize - 18, fill);
            Raylib.DrawRectangleLines(px + 8, py + 8, GameMap.TileSize - 16, GameMap.TileSize - 16, edge);
        }
    }

    private void DrawCombatReachableTiles()
    {
        if (_player == null || !_combatMoveModeActive || _combatMovePointsRemaining <= 0)
        {
            return;
        }

        if (!IsCombatState(_gameState))
        {
            return;
        }

        var reachable = BuildPlayerReachableCombatTiles();
        foreach (var tile in reachable)
        {
            var px = tile.X * GameMap.TileSize;
            var py = tile.Y * GameMap.TileSize;
            Raylib.DrawRectangle(px + 4, py + 4, GameMap.TileSize - 8, GameMap.TileSize - 8, new Color(82, 154, 204, 64));
            Raylib.DrawRectangleLines(px + 3, py + 3, GameMap.TileSize - 6, GameMap.TileSize - 6, new Color(126, 198, 255, 170));
        }
    }

    private void DrawPhase3SealedCorridorLocks()
    {
        if (_phase3RouteChoice == Phase3RouteChoice.None)
        {
            return;
        }

        if (_phase3RouteChoice == Phase3RouteChoice.UpperCatacombs)
        {
            DrawSealedCorridorRect(20, 20, 4, 2);
            DrawSealedCorridorRect(39, 21, 4, 2);
            return;
        }

        if (_phase3RouteChoice == Phase3RouteChoice.LowerShrine)
        {
            DrawSealedCorridorRect(31, 7, 4, 2);
            DrawSealedCorridorRect(46, 13, 2, 6);
        }
    }

    private static void DrawSealedCorridorRect(int tileX, int tileY, int tileW, int tileH)
    {
        var px = tileX * GameMap.TileSize;
        var py = tileY * GameMap.TileSize;
        var pw = tileW * GameMap.TileSize;
        var ph = tileH * GameMap.TileSize;
        Raylib.DrawRectangle(px, py, pw, ph, new Color(58, 18, 22, 208));
        Raylib.DrawRectangleLines(px, py, pw, ph, new Color(220, 84, 94, 245));
    }

    private void EnsureAtmosphereParticles()
    {
        var screenW = Raylib.GetScreenWidth();
        var screenH = Raylib.GetScreenHeight();
        if (_atmosphereScreenW == screenW &&
            _atmosphereScreenH == screenH &&
            _emberParticles.Count == VisualTheme.EmberParticleCount)
        {
            return;
        }

        _atmosphereScreenW = screenW;
        _atmosphereScreenH = screenH;
        _emberParticles.Clear();
        for (var i = 0; i < VisualTheme.EmberParticleCount; i++)
        {
            _emberParticles.Add(CreateEmberParticle(screenW, screenH, spawnInView: true));
        }
    }

    private AtmosParticle CreateEmberParticle(int screenW, int screenH, bool spawnInView)
    {
        return new AtmosParticle
        {
            X = spawnInView
                ? (float)_rng.NextDouble() * screenW
                : (float)_rng.NextDouble() * (screenW + 80f) - 40f,
            Y = spawnInView
                ? (float)_rng.NextDouble() * screenH
                : screenH + 8f + (float)_rng.NextDouble() * 40f,
            DriftX = (float)(_rng.NextDouble() * 18.0 - 9.0),
            RiseSpeed = (float)(VisualTheme.EmberDriftMin + _rng.NextDouble() * (VisualTheme.EmberDriftMax - VisualTheme.EmberDriftMin)),
            Size = 1.1f + (float)_rng.NextDouble() * 2.2f,
            Alpha = 0.32f + (float)_rng.NextDouble() * 0.68f,
            Phase = (float)(_rng.NextDouble() * Math.PI * 2.0)
        };
    }

    private void RespawnEmberParticle(AtmosParticle particle, int screenW, int screenH)
    {
        particle.X = (float)_rng.NextDouble() * (screenW + 80f) - 40f;
        particle.Y = screenH + 8f + (float)_rng.NextDouble() * 40f;
        particle.DriftX = (float)(_rng.NextDouble() * 18.0 - 9.0);
        particle.RiseSpeed = (float)(VisualTheme.EmberDriftMin + _rng.NextDouble() * (VisualTheme.EmberDriftMax - VisualTheme.EmberDriftMin));
        particle.Size = 1.1f + (float)_rng.NextDouble() * 2.2f;
        particle.Alpha = 0.32f + (float)_rng.NextDouble() * 0.68f;
        particle.Phase = (float)(_rng.NextDouble() * Math.PI * 2.0);
    }

    private void UpdateAtmosphereParticles()
    {
        if (_emberParticles.Count == 0) return;

        var dt = Math.Clamp(Raylib.GetFrameTime(), 0.001f, 0.05f);
        var screenW = Raylib.GetScreenWidth();
        var screenH = Raylib.GetScreenHeight();
        for (var i = 0; i < _emberParticles.Count; i++)
        {
            var particle = _emberParticles[i];
            particle.Phase += dt * (0.9f + particle.Size * 0.25f);
            particle.X += (particle.DriftX + MathF.Sin(particle.Phase) * 4.0f) * dt;
            particle.Y -= particle.RiseSpeed * dt;
            if (particle.Y < -24f || particle.X < -32f || particle.X > screenW + 32f)
            {
                RespawnEmberParticle(particle, screenW, screenH);
            }
        }
    }

    private void DrawAtmosphereParticles()
    {
        for (var i = 0; i < _emberParticles.Count; i++)
        {
            var particle = _emberParticles[i];
            var pulse = 0.72f + 0.28f * MathF.Sin(particle.Phase * 1.75f);
            var coreAlpha = Math.Clamp((int)(VisualTheme.EmberCoreColor.A * particle.Alpha * pulse), 0, 255);
            var core = new Color(
                VisualTheme.EmberCoreColor.R,
                VisualTheme.EmberCoreColor.G,
                VisualTheme.EmberCoreColor.B,
                (byte)coreAlpha);
            Raylib.DrawCircle((int)particle.X, (int)particle.Y, particle.Size, core);

            if (particle.Size >= 1.7f)
            {
                var trailAlpha = Math.Clamp((int)(VisualTheme.EmberTrailColor.A * particle.Alpha * 0.75f), 0, 255);
                var trail = new Color(
                    VisualTheme.EmberTrailColor.R,
                    VisualTheme.EmberTrailColor.G,
                    VisualTheme.EmberTrailColor.B,
                    (byte)trailAlpha);
                Raylib.DrawLine(
                    (int)particle.X,
                    (int)(particle.Y + particle.Size * 2f),
                    (int)(particle.X - particle.DriftX * 0.12f),
                    (int)(particle.Y + particle.Size * 5f),
                    trail);
            }
        }
    }

    private void DrawWorldLightingOverlay()
    {
        var worldW = GetWorldPixelWidth();
        var worldH = GetWorldPixelHeight();

        var pulse = 0.5f + 0.5f * MathF.Sin((float)Raylib.GetTime() * VisualTheme.FogPulseSpeed);
        var fogAlpha = (byte)(VisualTheme.FogMinAlpha + pulse * (VisualTheme.FogMaxAlpha - VisualTheme.FogMinAlpha));
        var fogColor = new Color(VisualTheme.WorldFogTint.R, VisualTheme.WorldFogTint.G, VisualTheme.WorldFogTint.B, fogAlpha);
        Raylib.DrawRectangle(0, 0, worldW, worldH, fogColor);

        if (_player != null)
        {
            var playerX = _player.X * GameMap.TileSize + GameMap.TileSize / 2;
            var playerY = _player.Y * GameMap.TileSize + GameMap.TileSize / 2;
            var playerLightRadius = VisualTheme.PlayerLightRadius + 8f * MathF.Sin((float)Raylib.GetTime() * 1.1f);
            Raylib.DrawCircleGradient(playerX, playerY, playerLightRadius, VisualTheme.PlayerLightInner, VisualTheme.PlayerLightOuter);
        }

        foreach (var node in _rewardNodes)
        {
            if (_claimedRewardNodeIds.Contains(node.Id)) continue;
            var nodeX = node.X * GameMap.TileSize + GameMap.TileSize / 2;
            var nodeY = node.Y * GameMap.TileSize + GameMap.TileSize / 2;
            var localPulse = 0.5f + 0.5f * MathF.Sin((float)Raylib.GetTime() * 1.45f + node.X * 0.37f + node.Y * 0.21f);
            var localRadius = VisualTheme.RewardLightRadius + 7f * localPulse;
            Raylib.DrawCircleGradient(nodeX, nodeY, localRadius, VisualTheme.RewardLightInner, VisualTheme.RewardLightOuter);
        }

        if (!_bossDefeated)
        {
            var bossX = 52 * GameMap.TileSize + GameMap.TileSize / 2;
            var bossY = 27 * GameMap.TileSize + GameMap.TileSize / 2;
            var bossRadius = VisualTheme.BossLightRadius + 10f * MathF.Sin((float)Raylib.GetTime() * 0.93f);
            Raylib.DrawCircleGradient(bossX, bossY, bossRadius, VisualTheme.BossLightInner, VisualTheme.BossLightOuter);
        }
    }

    private void DrawBlindMagePerceptionOverlay()
    {
        if (_player == null || !IsBlindMageModeActive())
        {
            return;
        }

        var cx = _player.X * GameMap.TileSize + GameMap.TileSize / 2;
        var cy = _player.Y * GameMap.TileSize + GameMap.TileSize / 2;
        var t = (float)Raylib.GetTime();
        if (IsBlindMageMagicSenseActive())
        {
            var pulse = 0.5f + 0.5f * MathF.Sin(t * 2.2f);
            var senseRadius = GameMap.TileSize * (5.4f + pulse * 1.1f);
            Raylib.DrawCircleLines(cx, cy, senseRadius, new Color(124, 218, 255, 145));
            Raylib.DrawCircleLines(cx, cy, senseRadius * 0.68f, new Color(108, 186, 242, 118));

            foreach (var enemy in _enemies.Where(enemy => enemy.IsAlive))
            {
                var ex = enemy.X * GameMap.TileSize + GameMap.TileSize / 2;
                var ey = enemy.Y * GameMap.TileSize + GameMap.TileSize / 2;
                var distTiles = Math.Abs(enemy.X - _player.X) + Math.Abs(enemy.Y - _player.Y);
                if (distTiles > 9)
                {
                    continue;
                }

                Raylib.DrawCircleLines(ex, ey, GameMap.TileSize * 0.55f, new Color(170, 232, 255, 210));
                Raylib.DrawLine(cx, cy, ex, ey, new Color(92, 176, 236, 85));
            }

            foreach (var node in _rewardNodes)
            {
                if (_claimedRewardNodeIds.Contains(node.Id))
                {
                    continue;
                }

                var distTiles = Math.Abs(node.X - _player.X) + Math.Abs(node.Y - _player.Y);
                if (distTiles > 10)
                {
                    continue;
                }

                var nx = node.X * GameMap.TileSize + GameMap.TileSize / 2;
                var ny = node.Y * GameMap.TileSize + GameMap.TileSize / 2;
                Raylib.DrawCircleLines(nx, ny, GameMap.TileSize * 0.45f, new Color(224, 246, 255, 175));
            }
        }
        else
        {
            var pulse = 0.5f + 0.5f * MathF.Sin(t * 2.8f);
            var resonanceRadius = GameMap.TileSize * (2.4f + pulse * 0.9f);
            Raylib.DrawCircleLines(cx, cy, resonanceRadius, new Color(214, 214, 214, 170));
            Raylib.DrawCircleLines(cx, cy, resonanceRadius * 0.56f, new Color(196, 196, 196, 145));
        }
    }

    private void DrawBlindMageScreenFilter()
    {
        if (!IsBlindMageModeActive())
        {
            return;
        }

        var w = Raylib.GetScreenWidth();
        var h = Raylib.GetScreenHeight();
        var hasMagicSense = IsBlindMageMagicSenseActive();
        var wash = hasMagicSense
            ? new Color(24, 24, 24, 84)
            : new Color(36, 36, 36, 148);
        Raylib.DrawRectangle(0, 0, w, h, wash);

        if (!hasMagicSense)
        {
            var edge = new Color(0, 0, 0, 198);
            var clear = new Color(0, 0, 0, 0);
            var thickness = Math.Max(96, Math.Min(w, h) / 5);
            Raylib.DrawRectangleGradientV(0, 0, w, thickness, edge, clear);
            Raylib.DrawRectangleGradientV(0, h - thickness, w, thickness, clear, edge);
            Raylib.DrawRectangleGradientH(0, 0, thickness, h, edge, clear);
            Raylib.DrawRectangleGradientH(w - thickness, 0, thickness, h, clear, edge);
        }
    }

    private void DrawScreenAtmosphereOverlay()
    {
        var w = Raylib.GetScreenWidth();
        var h = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(0, 0, w, h, VisualTheme.ScreenHazeColor);

        var edgeColor = VisualTheme.VignetteEdgeColor;
        var edgeTransparent = new Color(edgeColor.R, edgeColor.G, edgeColor.B, (byte)0);
        var thickness = Math.Max(84, Math.Min(w, h) / 6);
        Raylib.DrawRectangleGradientV(0, 0, w, thickness, edgeColor, edgeTransparent);
        Raylib.DrawRectangleGradientV(0, h - thickness, w, thickness, edgeTransparent, edgeColor);
        Raylib.DrawRectangleGradientH(0, 0, thickness, h, edgeColor, edgeTransparent);
        Raylib.DrawRectangleGradientH(w - thickness, 0, thickness, h, edgeTransparent, edgeColor);
    }

    private void DrawWorld()
    {
        EnsureAtmosphereParticles();
        UpdateAtmosphereParticles();

        var camera = BuildWorldCamera();
        Raylib.BeginMode2D(camera);
        _map.Draw();
        DrawPhase3SealedCorridorLocks();
        DrawRewardNodes();
        DrawGroundLoot();
        DrawCombatReachableTiles();

        foreach (var enemy in _enemies.Where(e => e.IsAlive))
        {
            var px = enemy.X * GameMap.TileSize;
            var py = enemy.Y * GameMap.TileSize;
            var enemySprite = ResolveEnemySpriteId(enemy);
            if (!_spriteLibrary.TryDraw(enemySprite, enemy.X, enemy.Y, SpriteMotion.Idle))
            {
                Raylib.DrawRectangle(px, py, GameMap.TileSize, GameMap.TileSize, enemy.Type.Color);
            }

            var hpW = GameMap.TileSize - 4;
            var hpRatio = Math.Max(0, enemy.CurrentHp) / (float)enemy.Type.MaxHp;
            Raylib.DrawRectangle(px + 2, py - 8, hpW, 4, new Color(80, 0, 0, 255));
            Raylib.DrawRectangle(px + 2, py - 8, (int)(hpW * hpRatio), 4, ColGreen);
        }

        DrawPlayerWorldSprite();
        DrawWorldLightingOverlay();
        DrawBlindMagePerceptionOverlay();
        DrawEnemyDebugOverlay();
        Raylib.EndMode2D();

        DrawBlindMageScreenFilter();
        DrawScreenAtmosphereOverlay();
        DrawAtmosphereParticles();
        DrawHud();
    }


    private void DrawPlayerWorldSprite()
    {
        if (_player == null) return;

        var motion = Raylib.GetTime() < _playerRunAnimUntil ? SpriteMotion.Run : SpriteMotion.Idle;
        var playerSprite = ResolvePlayerSpriteId(_player);
        if (!_spriteLibrary.TryDraw(playerSprite, _player.X, _player.Y, motion))
        {
            _player.Draw();
        }
    }

    private void DrawEnemyDebugOverlay()
    {
        if (!_debugOverlayEnabled) return;
        if (_player == null) return;

        var visionPx = GameTuning.EnemyVisionRangeTiles * GameMap.TileSize;
        var fovHalfRadians = GameTuning.EnemyFovDegrees * (MathF.PI / 180f) * 0.5f;

        foreach (var enemy in _enemies.Where(e => e.IsAlive))
        {
            if (!_enemyAi.TryGetValue(enemy, out var ai))
            {
                continue;
            }

            var cx = enemy.X * GameMap.TileSize + GameMap.TileSize / 2f;
            var cy = enemy.Y * GameMap.TileSize + GameMap.TileSize / 2f;
            Raylib.DrawCircleLines((int)cx, (int)cy, visionPx, new Color(144, 176, 220, 70));

            var facingLen = MathF.Sqrt(ai.FacingX * ai.FacingX + ai.FacingY * ai.FacingY);
            var fx = facingLen > 0.001f ? ai.FacingX / facingLen : 1f;
            var fy = facingLen > 0.001f ? ai.FacingY / facingLen : 0f;
            var leftX = fx * MathF.Cos(-fovHalfRadians) - fy * MathF.Sin(-fovHalfRadians);
            var leftY = fx * MathF.Sin(-fovHalfRadians) + fy * MathF.Cos(-fovHalfRadians);
            var rightX = fx * MathF.Cos(fovHalfRadians) - fy * MathF.Sin(fovHalfRadians);
            var rightY = fx * MathF.Sin(fovHalfRadians) + fy * MathF.Cos(fovHalfRadians);
            Raylib.DrawLine((int)cx, (int)cy, (int)(cx + leftX * visionPx), (int)(cy + leftY * visionPx), new Color(120, 220, 180, 95));
            Raylib.DrawLine((int)cx, (int)cy, (int)(cx + rightX * visionPx), (int)(cy + rightY * visionPx), new Color(120, 220, 180, 95));

            var stateColor = ai.State switch
            {
                EnemyAiState.Chase => ColRed,
                EnemyAiState.Search => ColYellow,
                EnemyAiState.Return => ColSkyBlue,
                EnemyAiState.Investigate => new Color(255, 184, 108, 255),
                _ => ColLightGray
            };

            var stateLabel = $"{ai.State}";
            Raylib.DrawText(stateLabel, enemy.X * GameMap.TileSize - 4, enemy.Y * GameMap.TileSize - 21, 12, stateColor);

            var leashRadius = GameTuning.EnemyLeashDistanceTiles * GameMap.TileSize;
            var spawnCx = enemy.SpawnX * GameMap.TileSize + GameMap.TileSize / 2;
            var spawnCy = enemy.SpawnY * GameMap.TileSize + GameMap.TileSize / 2;
            Raylib.DrawCircleLines(spawnCx, spawnCy, leashRadius, new Color(120, 140, 180, 35));
        }
    }

    private string ResolvePlayerSpriteId(Player player)
    {
        if (IsValidPlayerSpriteId(_selectedPlayerSpriteId))
        {
            return _selectedPlayerSpriteId;
        }

        return ResolveDefaultSpriteForRaceAndGender(player.Race, player.Gender);
    }

    private static string ResolveEnemySpriteId(Enemy enemy)
    {
        return enemy.Type.Name switch
        {
            "Goblin" => "goblin",
            "Goblin Grunt" => "goblin",
            "Goblin Skirmisher" => "goblin",
            "Goblin Slinger" => "goblin",
            "Goblin Supervisor" => "goblin",
            "Goblin General" => "goblin",
            "Warg" => "wogol",
            "Skeleton" => "skelet",
            "Cultist" => "masked_orc",
            "Shadow Mage" => "orc_shaman",
            "Ogre" => "ogre",
            "Troll" => "big_zombie",
            "Dread Knight" => "big_demon",
            _ => "goblin"
        };
    }
    private void DrawHud()
    {
        if (_player == null) return;

        var hudW = Raylib.GetScreenWidth();
        DrawPanel(0, 0, hudW, UiLayout.HudHeight, new Color(8, 12, 20, 210), ColBorder);
        var pad = UiLayout.HudPadding;

        var levelText = $"Lv.{_player.Level} XP: {_player.Xp}/{_player.XpToNextLevel}";
        var enemiesText = $"Enemies: {_enemies.Count(e => e.IsAlive)}  Zone: {GetFloorZoneLabel(_currentFloorZone)}";
        var controlsText = "[C] Sheet  [WASD/Arrows] Move  [Hold E] Loot  [F1] Debug  [F11] Fullscreen";
        var hasBuildIdentity =
            _runArchetype != RunArchetype.None ||
            _runRelic != RunRelic.None ||
            GetEffectiveExecutionRank() > 0 ||
            GetEffectiveArcRank() > 0 ||
            GetEffectiveEscapeRank() > 0;
        var topRightText = hasBuildIdentity ? $"Build: {GetRunIdentityLabel()}" : controlsText;
        var topRightFont = hasBuildIdentity ? 14 : 15;
        var topRightColor = hasBuildIdentity ? ColSkyBlue : ColLightGray;
        var topRightX = hudW - pad - Raylib.MeasureText(topRightText, topRightFont);
        var levelX = pad;
        var enemiesMinX = levelX + Raylib.MeasureText(levelText, 17) + 24;
        var enemiesX = Math.Max(enemiesMinX, topRightX - Raylib.MeasureText(enemiesText, 15) - 18);

        Raylib.DrawText(levelText, levelX, 9, 17, ColWhite);
        Raylib.DrawText(enemiesText, enemiesX, 9, 15, ColLightGray);
        if (topRightX > enemiesX + 120)
        {
            Raylib.DrawText(topRightText, topRightX, 9, topRightFont, topRightColor);
        }

        var hpText = $"HP {_player.CurrentHp}/{_player.MaxHp}";
        var mpText = $"MP {_player.CurrentMana}/{_player.MaxMana}";
        var packText = $"Pack: {GetInventoryQuantityTotal()} items";
        var hpX = 8;
        var mpX = hpX + Raylib.MeasureText(hpText, 18) + 22;
        var packX = mpX + Raylib.MeasureText(mpText, 18) + 22;
        Raylib.DrawText(hpText, hpX, 30, 18, ColGreen);
        Raylib.DrawText(mpText, mpX, 30, 18, ColSkyBlue);
        Raylib.DrawText(packText, packX, 30, 15, ColYellow);
        var objectiveText = GetPhase3ObjectiveLabel();
        DrawWrappedText(objectiveText, hpX, 48, Math.Max(220, hudW - hpX - pad), 13, ColLightGray);
        var conditionsHudText = _settingsOptionalConditionsEnabled
            ? $"Conditions: {GetActiveMajorConditionSummary()} | Cure: Settings -> Accessibility -> Purge ({GetConditionPurgeCostLabel()})"
            : "Conditions: disabled in settings.";
        DrawWrappedText(
            conditionsHudText,
            hpX,
            63,
            Math.Max(220, hudW - hpX - pad),
            12,
            _settingsOptionalConditionsEnabled && _activeMajorConditions.Count > 0 ? ColAccentRose : ColGray);

        if (_player.IsCasterClass)
        {
            var slotsText = $"Slots L1 {_player.GetSpellSlots(1)}/{_player.GetSpellSlotsMax(1)}  L2 {_player.GetSpellSlots(2)}/{_player.GetSpellSlotsMax(2)}  L3 {_player.GetSpellSlots(3)}/{_player.GetSpellSlotsMax(3)}";
            var slotsX = hudW - pad - Raylib.MeasureText(slotsText, 16);
            if (slotsX > packX + 120)
            {
                Raylib.DrawText(slotsText, slotsX, 30, 16, ColLightGray);
            }
            else
            {
                DrawWrappedText(slotsText, packX, 30, Math.Max(140, hudW - packX - pad), 14, ColLightGray);
            }
        }

        if (!string.IsNullOrWhiteSpace(_rewardMessage) && _gameState == GameState.Playing)
        {
            DrawFooterBar(12, UiLayout.RewardBannerY, Math.Max(220, hudW - 24), UiLayout.RewardBannerHeight);
            var notice = _rewardMessageRequiresAcknowledge
                ? $"{_rewardMessage}  [ENTER] Continue"
                : _rewardMessage;
            DrawWrappedText(notice, 18, UiLayout.RewardBannerY + 5, Math.Max(220, hudW - 36), 14, ColLightGray);
        }

        if (_gameState == GameState.Playing)
        {
            var tileLoot = GetGroundLootAt(_player.X, _player.Y);
            if (tileLoot != null)
            {
                var contested = IsLootPickupContested(_player.X, _player.Y);
                var progress = _activePickupLootId == tileLoot.Id
                    ? Math.Clamp(_activePickupProgressSeconds / LootPickupHoldSeconds, 0.0, 1.0)
                    : 0.0;
                var progressPercent = (int)Math.Round(progress * 100);
                var prompt = contested
                    ? $"Threat nearby. Clear space to secure {tileLoot.Name}."
                    : $"Hold [E] to secure {tileLoot.Name} - {progressPercent}%";
                var promptY = UiLayout.RewardBannerY + UiLayout.RewardBannerHeight + 6;
                DrawPanel(12, promptY, Math.Max(260, hudW - 24), 30, new Color(14, 18, 28, 225), ColBorder);
                DrawWrappedText(prompt, 18, promptY + 7, Math.Max(250, hudW - 36), 14, contested ? ColRed : ColLightGray);
            }
            else if (!string.IsNullOrWhiteSpace(_activePickupStatus) && _activePickupStatusUntil > Raylib.GetTime())
            {
                var statusY = UiLayout.RewardBannerY + UiLayout.RewardBannerHeight + 6;
                DrawPanel(12, statusY, Math.Max(260, hudW - 24), 30, new Color(14, 18, 28, 225), ColBorder);
                DrawWrappedText(_activePickupStatus, 18, statusY + 7, Math.Max(250, hudW - 36), 14, ColSkyBlue);
            }
        }

        if (_debugOverlayEnabled)
        {
            var debugText =
                $"DEBUG  CamSmooth {GameTuning.CameraSmoothness:0.0}  DeadZone {GameTuning.CameraDeadZoneHalfWidthTiles:0.0}x{GameTuning.CameraDeadZoneHalfHeightTiles:0.0}  " +
                $"Vision {GameTuning.EnemyVisionRangeTiles:0.0}  FOV {GameTuning.EnemyFovDegrees:0}  Leash {GameTuning.EnemyLeashDistanceTiles}";
            var debugY = UiLayout.HudHeight + 8;
            DrawPanel(8, debugY, Math.Min(hudW - 16, 980), 26, new Color(9, 12, 20, 210), ColBorder);
            Raylib.DrawText(debugText, 14, debugY + 6, 14, ColLightGray);
        }
    }

    private void DrawCombatUi()
    {
        if (_player == null || _currentEnemy == null) return;

        var w = Raylib.GetScreenWidth();
        var h = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(0, 0, w, h, new Color(0, 0, 0, 210));
        DrawPanel(
            UiLayout.CombatOverlayInset,
            UiLayout.CombatOverlayInset,
            w - UiLayout.CombatOverlayInset * 2,
            h - UiLayout.CombatOverlayInset * 2,
            ColPanel,
            ColAccentRose);

        // Enemy header
        DrawPanel(
            UiLayout.CombatHeaderInsetX,
            UiLayout.CombatHeaderY,
            w - UiLayout.CombatHeaderInsetX * 2,
            UiLayout.CombatHeaderHeight,
            ColPanelAlt,
            ColBorder);
        var aliveTargets = GetAliveEncounterEnemies();
        var activeTargetIndex = aliveTargets.FindIndex(enemy => ReferenceEquals(enemy, _currentEnemy));
        if (activeTargetIndex < 0)
        {
            activeTargetIndex = 0;
        }

        var meleeValidation = ValidateCurrentEnemyTargetForMelee();
        var enemyHp = Math.Max(0, _currentEnemy.CurrentHp);
        var attackReadinessLabel = meleeValidation.IsLegal
            ? "Melee ready"
            : $"Melee blocked ({meleeValidation.BuildBlockedReason()})";
        var losLabel = meleeValidation.HasLineOfSight ? "LOS clear" : "LOS blocked";
        DrawCenteredText($"{_currentEnemy.Type.Name}  Target {activeTargetIndex + 1}/{Math.Max(1, aliveTargets.Count)}", w / 2, 74, 28, ColAccentRose);
        DrawCenteredText($"Enemy HP {enemyHp}/{_currentEnemy.Type.MaxHp}  Dist {meleeValidation.DistanceTiles}/{meleeValidation.MaxRangeTiles}", w / 2, 100, 17, ColWhite);
        DrawCenteredText($"{attackReadinessLabel}  |  {losLabel}", w / 2, 118, 14, meleeValidation.IsLegal ? ColGreen : ColYellow);

        // Shared content area for log + action panels.
        var contentX = UiLayout.CombatHeaderInsetX;
        var contentY = UiLayout.CombatContentY;
        var contentW = w - UiLayout.CombatHeaderInsetX * 2;
        var contentH = UiLayout.CombatContentHeight;
        var colGap = UiLayout.CombatColumnsGap;
        var actionW = Math.Clamp(contentW / 4, 132, 220);
        var logW = contentW - actionW - colGap;

        // Log panel
        var logX = contentX;
        var logY = contentY;
        var logH = contentH;
        DrawPanel(logX, logY, logW, logH, ColPanelSoft, ColBorder);
        Raylib.DrawText("Combat Log", logX + 10, logY + 8, 20, ColSkyBlue);

        var lineY = logY + 40;
        foreach (var line in _combatLog.TakeLast(GetCombatLogVisibleLines()))
        {
            Raylib.DrawText(line, logX + 12, lineY, 17, ColLightGray);
            lineY += 26;
        }

        // Actions panel
        var actionX = logX + logW + colGap;
        var actionY = contentY;
        var actionH = contentH;
        DrawPanel(actionX, actionY, actionW, actionH, ColPanelSoft, ColBorder);
        Raylib.DrawText("Actions", actionX + 22, actionY + 8, 20, ColSkyBlue);

        var actions = GetCombatActions();
        for (var i = 0; i < actions.Count; i++)
        {
            var selected = i == Math.Min(_selectedActionIndex, actions.Count - 1);
            var rowY = actionY + 48 + i * 44;
            DrawMenuRow(actionX + 10, rowY, actionW - 20, 36, selected);
            DrawCenteredText(actions[i], actionX + actionW / 2, rowY + 8, 22, selected ? ColYellow : ColWhite);
        }

        var armorStyleDefense = GetArmorStateDefenseBonus(_player);
        var armorStyleFlee = GetArmorStateFleeBonus(_player);
        var totalDefense = _player.DefenseBonus + GetClassDefenseBonus(_player) + _runDefenseBonus + armorStyleDefense + GetConditionDefenseModifier();
        var fleeChance = Math.Clamp(50 + _player.FleeBonus + GetClassFleeBonus(_player) + _runFleeBonus + armorStyleFlee + GetConditionFleeModifier(), 5, 95);
        Raylib.DrawText($"HP {_player.CurrentHp}/{_player.MaxHp}", actionX + 10, actionY + actionH - 96, 13, ColGreen);
        Raylib.DrawText($"MP {_player.CurrentMana}/{_player.MaxMana}", actionX + 10, actionY + actionH - 80, 13, ColSkyBlue);
        Raylib.DrawText($"Move {_combatMovePointsRemaining}/{_combatMovePointsMax}", actionX + 10, actionY + actionH - 64, 13, ColYellow);
        Raylib.DrawText($"DEF {totalDefense}  Flee {fleeChance}%", actionX + 10, actionY + actionH - 48, 13, ColLightGray);
        var condSummary = GetActiveMajorConditionSummary();
        Raylib.DrawText($"Cond: {condSummary}", actionX + 10, actionY + actionH - 32, 12, _settingsOptionalConditionsEnabled ? ColAccentRose : ColGray);
        Raylib.DrawText(_player.CharacterClass.Name, actionX + 10, actionY + actionH - 20, 13, ColWhite);
        Raylib.DrawText(GetClassCombatTag(_player.CharacterClass.Name), actionX + 10, actionY + actionH - 6, 12, ColSkyBlue);

        DrawFooterBar(
            UiLayout.CombatFooterInset,
            h - UiLayout.CombatFooterInset,
            w - UiLayout.CombatFooterInset * 2,
            24);
        if (_combatMoveModeActive)
        {
            DrawCenteredText($"Move Mode: ARROWS/WASD step  |  ENTER/ESC end move  |  Remaining {_combatMovePointsRemaining}", w / 2, h - 58, 15, ColLightGray);
        }
        else
        {
            DrawCenteredText($"UP/DOWN action  |  LEFT/RIGHT target  |  ENTER act  |  Arc {_milestoneArcChargesThisCombat}  Escape {_milestoneEscapeChargesThisCombat}", w / 2, h - 58, 15, ColLightGray);
        }
    }

    private void DrawCombatSkillMenu()
    {
        if (_player == null) return;

        var skillIds = GetCombatSkills();
        if (skillIds.Count == 0) return;

        var w = Raylib.GetScreenWidth();
        var panelW = w - 120;
        var panelX = 60;
        var panelY = 118;
        var panelH = 200;

        DrawPanel(panelX, panelY, panelW, panelH, ColPanelAlt, ColBorder);
        DrawCenteredText("Combat Skills", w / 2, panelY + 12, 28, ColSkyBlue);

        for (var i = 0; i < skillIds.Count; i++)
        {
            var id = skillIds[i];
            var selected = i == _selectedCombatSkillIndex;
            var marker = selected ? "> " : "  ";
            var label = id switch
            {
                "second_wind" => "Second Wind (1/combat)",
                "mana_shield" => $"Mana Shield (MP 3)",
                _ => id
            };
            var affordable = id != "mana_shield" || _player.CurrentMana >= 3;
            var color = !affordable ? ColGray : selected ? ColYellow : ColWhite;
            var rowY = panelY + 50 + i * 34;
            DrawMenuRow(panelX + 22, rowY - 4, panelW - 44, 30, selected && affordable);
            DrawCenteredText($"{marker}{label}", w / 2, rowY + 2, 21, color);
        }

        var chosenId = skillIds[Math.Min(_selectedCombatSkillIndex, skillIds.Count - 1)];
        var desc = chosenId switch
        {
            "second_wind" => "Recover HP based on Constitution, then enemy acts.",
            "mana_shield" => "Spend mana to absorb damage on the next enemy attack.",
            _ => "Class combat skill."
        };
        DrawCenteredText(desc, w / 2, panelY + panelH - 56, 17, ColLightGray);
        DrawCenteredText($"Current MP: {_player.CurrentMana}/{_player.MaxMana}", w / 2, panelY + panelH - 34, 16, ColSkyBlue);
        DrawFooterBar(panelX + 10, panelY + panelH - 22, panelW - 20, 16);
        DrawCenteredText("ENTER cast  |  ESC back", w / 2, panelY + panelH - 21, 13, ColLightGray);
    }

    private void DrawCombatSpellMenu()
    {
        if (_player == null) return;

        var spells = _player.GetKnownSpells();
        if (spells.Count == 0) return;

        var w = Raylib.GetScreenWidth();
        var panelW = w - 120;
        var panelX = 60;
        var panelY = 108;
        var panelH = 230;

        DrawPanel(panelX, panelY, panelW, panelH, ColPanelAlt, ColBorder);
        DrawCenteredText("Spells", w / 2, panelY + 10, 30, ColSkyBlue);

        EnsureSpellSelectionVisible(spells.Count);
        var start = _spellMenuOffset;
        var end = Math.Min(spells.Count, start + SpellMenuVisibleCount);
        for (var i = start; i < end; i++)
        {
            var spell = spells[i];
            var selected = i == _selectedSpellIndex;
            var marker = selected ? "> " : "  ";
            var slots = spell.RequiresSlot ? _player.GetSpellSlots(spell.SpellLevel) : 0;
            var slotsMax = spell.RequiresSlot ? _player.GetSpellSlotsMax(spell.SpellLevel) : 0;
            var usable = !spell.RequiresSlot || slots > 0;
            var color = !usable ? ColGray : selected ? ColYellow : ColWhite;
            var tierLabel = spell.IsCantrip ? "Cantrip" : $"L{spell.SpellLevel}";
            var costLabel = spell.IsCantrip ? "No slot cost" : $"Slots {slots}/{slotsMax}";
            var rowY = panelY + 48 + (i - start) * 28;
            DrawMenuRow(panelX + 20, rowY - 3, panelW - 40, 24, selected && usable);
            DrawCenteredText(
                $"{marker}{spell.Name} ({tierLabel})  {costLabel}",
                w / 2,
                panelY + 52 + (i - start) * 28,
                18,
                color);
        }

        if (start > 0)
        {
            DrawCenteredText("...more above...", w / 2, panelY + 36, 14, ColGray);
        }
        if (end < spells.Count)
        {
            DrawCenteredText("...more below...", w / 2, panelY + panelH - 60, 14, ColGray);
        }

        var selectedSpell = spells[Math.Min(_selectedSpellIndex, spells.Count - 1)];
        var spellValidation = ValidateCurrentEnemyTargetForSpell(selectedSpell);
        var spellLosLabel = spellValidation.HasLineOfSight ? "LOS clear" : "LOS blocked";
        var spellTargetColor = spellValidation.IsLegal ? ColGreen : ColYellow;
        DrawCenteredText(
            $"Target {_currentEnemy?.Type.Name ?? "None"}  Dist {spellValidation.DistanceTiles}/{spellValidation.MaxRangeTiles}  {spellLosLabel}",
            w / 2,
            panelY + panelH - 62,
            14,
            spellTargetColor);
        DrawCenteredText(selectedSpell.Description, w / 2, panelY + panelH - 44, 17, ColLightGray);
        DrawFooterBar(panelX + 10, panelY + panelH - 22, panelW - 20, 16);
        DrawCenteredText("ENTER target  |  ESC back", w / 2, panelY + panelH - 21, 13, ColLightGray);
    }

    private void DrawCombatSpellTargeting()
    {
        if (_player == null) return;
        if (!TryGetPendingCombatSpell(out var pendingSpell))
        {
            DrawCombatSpellMenu();
            return;
        }

        var w = Raylib.GetScreenWidth();
        var panelW = w - 120;
        var panelX = 60;
        var panelY = 108;
        var panelH = 230;
        var tierLabel = pendingSpell.IsCantrip ? "Cantrip" : $"L{pendingSpell.SpellLevel}";
        var validation = ValidateCurrentEnemyTargetForSpell(pendingSpell);
        var targetName = _currentEnemy?.Type.Name ?? "None";
        var losLabel = validation.HasLineOfSight ? "LOS clear" : "LOS blocked";
        var aliveLabel = validation.TargetAlive ? "Alive" : "Down";
        var legalityLabel = validation.IsLegal
            ? "Cast is legal."
            : $"Blocked: {validation.BuildBlockedReason()}";
        var aliveTargets = GetAliveEncounterEnemies();
        var activeTargetIndex = aliveTargets.FindIndex(enemy => ReferenceEquals(enemy, _currentEnemy));
        if (activeTargetIndex < 0)
        {
            activeTargetIndex = 0;
        }

        DrawPanel(panelX, panelY, panelW, panelH, ColPanelAlt, ColBorder);
        DrawCenteredText("Spell Targeting", w / 2, panelY + 10, 30, ColSkyBlue);
        DrawCenteredText($"{pendingSpell.Name} ({tierLabel})", w / 2, panelY + 52, 23, ColYellow);
        DrawCenteredText(
            $"Target {targetName}  {activeTargetIndex + 1}/{Math.Max(1, aliveTargets.Count)}",
            w / 2,
            panelY + 86,
            19,
            ColWhite);
        DrawCenteredText(
            $"Range {validation.DistanceTiles}/{validation.MaxRangeTiles}  {losLabel}  {aliveLabel}",
            w / 2,
            panelY + 114,
            16,
            validation.IsLegal ? ColGreen : ColYellow);
        DrawCenteredText(legalityLabel, w / 2, panelY + 141, 15, validation.IsLegal ? ColGreen : ColYellow);
        DrawCenteredText(pendingSpell.Description, w / 2, panelY + panelH - 44, 17, ColLightGray);
        DrawFooterBar(panelX + 10, panelY + panelH - 22, panelW - 20, 16);
        DrawCenteredText("LEFT/RIGHT cycle target  |  ENTER confirm cast  |  ESC cancel", w / 2, panelY + panelH - 21, 13, ColLightGray);
    }

    private void DrawCombatItemMenu()
    {
        if (_player == null) return;

        var items = GetCombatConsumables();
        if (items.Count == 0) return;

        var w = Raylib.GetScreenWidth();
        var panelW = w - 120;
        var panelX = 60;
        var panelY = 108;
        var panelH = 230;

        DrawPanel(panelX, panelY, panelW, panelH, ColPanelAlt, ColBorder);
        DrawCenteredText("Consumables", w / 2, panelY + 10, 30, ColSkyBlue);

        EnsureCombatItemSelectionVisible(items.Count);
        var start = _combatItemMenuOffset;
        var end = Math.Min(items.Count, start + CombatItemVisibleCount);
        for (var i = start; i < end; i++)
        {
            var item = items[i];
            var selected = i == _selectedCombatItemIndex;
            var marker = selected ? "> " : "  ";
            var rowY = panelY + 48 + (i - start) * 28;
            DrawMenuRow(panelX + 20, rowY - 3, panelW - 40, 24, selected);
            DrawCenteredText(
                $"{marker}{item.Name}  x{item.Quantity}",
                w / 2,
                panelY + 52 + (i - start) * 28,
                18,
                selected ? ColYellow : ColWhite);
        }

        if (start > 0)
        {
            DrawCenteredText("...more above...", w / 2, panelY + 36, 14, ColGray);
        }
        if (end < items.Count)
        {
            DrawCenteredText("...more below...", w / 2, panelY + panelH - 60, 14, ColGray);
        }

        var selectedItem = items[Math.Min(_selectedCombatItemIndex, items.Count - 1)];
        var description = selectedItem.Id switch
        {
            "health_potion" => "Restore 35% HP. Consumes your turn.",
            "mana_draught" => "Restore 35% MP. Consumes your turn.",
            "sharpening_oil" => "Gain +1 melee damage for this run. Consumes your turn.",
            _ => selectedItem.Description
        };
        DrawCenteredText(description, w / 2, panelY + panelH - 44, 17, ColLightGray);
        DrawFooterBar(panelX + 10, panelY + panelH - 22, panelW - 20, 16);
        DrawCenteredText("ENTER use  |  ESC back", w / 2, panelY + panelH - 21, 13, ColLightGray);
    }


    private int GetCharacterSheetContentHeight()
    {
        if (_player == null) return 0;

        var height = 0;
        height += 28 + 20 + 20 + 24; // progression
        height += 28 + 20 + 20 + 20 + 20 + 20; // combat profile
        if (_player.IsCasterClass) height += 20;
        height += 8 + 28 + 20 + 20 + 20; // armor profile
        height += 8;
        height += 28 + 20 + 20 + 20 + 20 + 20 + 20 + 32 + 8; // run identity
        height += 20 + 24; // major conditions header and purge line
        if (_activeMajorConditions.Count == 0)
        {
            height += 20;
        }
        else
        {
            height += _activeMajorConditions.Count * (18 + 34);
        }
        height += 8;

        height += 28; // skills header
        if (_player.Skills.Count == 0)
        {
            height += 24;
        }
        else
        {
            height += _player.Skills.Count * (18 + 20);
        }

        height += 10 + 28; // feats header
        if (_player.Feats.Count == 0)
        {
            height += 24;
        }
        else
        {
            height += _player.Feats.Count * (18 + 20);
        }

        height += 10;
        if (_player.IsCasterClass)
        {
            height += 28 + 20 + 20 + 20 + 24 + 24; // spellcasting headers/slot lines
            height += _player.GetKnownSpells().Count * 18;
        }

        return height;
    }

    private void ClampCharacterSheetScroll()
    {
        if (_player == null)
        {
            _characterSheetScroll = 0;
            return;
        }

        var viewportHeight = (Raylib.GetScreenHeight()) - 152;
        var overflowPixels = Math.Max(0, GetCharacterSheetContentHeight() - viewportHeight);
        var maxScrollSteps = (int)Math.Ceiling(overflowPixels / 22.0);
        _characterSheetScroll = Math.Clamp(_characterSheetScroll, 0, maxScrollSteps);
    }

    private void DrawCharacterSheet()
    {
        if (_player == null) return;

        var w = Raylib.GetScreenWidth();
        var h = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(0, 0, w, h, new Color(0, 0, 0, 215));
        Raylib.DrawRectangle(28, 24, w - 56, h - 48, new Color(24, 28, 38, 255));
        Raylib.DrawRectangleLines(28, 24, w - 56, h - 48, new Color(96, 118, 148, 255));

        DrawCenteredText("Character Sheet", w / 2, 40, 32, ColWhite);
        DrawCenteredText($"{_player.Name} - {_player.Race} {_player.Gender} {_player.CharacterClass.Name}", w / 2, 74, 20, ColLightGray);

        var leftX = 46;
        var leftY = 112;
        var leftW = 228;
        var leftH = h - 150;
        var rightX = 290;
        var rightY = 112;
        var rightW = w - rightX - 46;
        var rightH = h - 150;

        Raylib.DrawRectangle(leftX, leftY, leftW, leftH, new Color(18, 22, 32, 220));
        Raylib.DrawRectangle(rightX, rightY, rightW, rightH, new Color(18, 22, 32, 220));
        Raylib.DrawRectangleLines(leftX, leftY, leftW, leftH, new Color(70, 88, 118, 255));
        Raylib.DrawRectangleLines(rightX, rightY, rightW, rightH, new Color(70, 88, 118, 255));

        var ly = leftY + 14;
        Raylib.DrawText("Core Stats", leftX + 10, ly, 24, ColYellow);
        ly += 34;
        foreach (var stat in StatOrder)
        {
            Raylib.DrawText($"{stat}: {_player.Stats.Get(stat)}", leftX + 10, ly, 20, ColWhite);
            ly += 26;
        }
        ly += 8;
        Raylib.DrawText($"HP {_player.CurrentHp}/{_player.MaxHp}", leftX + 10, ly, 18, ColGreen);
        ly += 22;
        Raylib.DrawText($"MP {_player.CurrentMana}/{_player.MaxMana}", leftX + 10, ly, 18, ColSkyBlue);
        ly += 22;
        Raylib.DrawText($"XP {_player.Xp}/{_player.XpToNextLevel}", leftX + 10, ly, 18, ColLightGray);

        ClampCharacterSheetScroll();

        var classMeleeBonus = GetClassMeleeDamageBonus(_player);
        var classSpellBonus = GetClassSpellDamageBonus(_player);
        var classCritBonus = GetClassCritBonus(_player);
        var classDefenseBonus = GetClassDefenseBonus(_player);
        var classEvasion = GetClassEvasionChance(_player);
        var classFleeBonus = GetClassFleeBonus(_player);
        var critChance = Math.Max(5, 5 + _player.Mod(StatName.Dexterity) * 2 + _player.CritBonus + classCritBonus + _runCritBonus);
        var armorStyleDefense = GetArmorStateDefenseBonus(_player);
        var armorStyleFlee = GetArmorStateFleeBonus(_player);
        var equippedArmorItem = GetEquippedArmorItem();
        var armorState = GetCurrentArmorCategory();
        var armorStateLabel = GetArmorStateLabel(armorState);
        var armorTrainingSummary = GetArmorTrainingSummary(_player);
        var totalDefense = _player.DefenseBonus + classDefenseBonus + _runDefenseBonus + armorStyleDefense + GetConditionDefenseModifier();
        var totalFleeChance = Math.Clamp(50 + _player.FleeBonus + classFleeBonus + _runFleeBonus + armorStyleFlee + GetConditionFleeModifier(), 5, 95);

        // Right panel content supports scrolling for long builds.
        Raylib.BeginScissorMode(rightX + 1, rightY + 1, rightW - 2, rightH - 2);
        var ry = rightY + 12 - _characterSheetScroll * 22;

        Raylib.DrawText("Progression", rightX + 10, ry, 22, ColSkyBlue);
        ry += 28;
        Raylib.DrawText($"Level {_player.Level}   Class {_player.CharacterClass.Name}", rightX + 10, ry, 16, ColLightGray);
        ry += 20;
        Raylib.DrawText($"XP: {_player.Xp}/{_player.XpToNextLevel}", rightX + 10, ry, 16, ColLightGray);
        ry += 20;
        Raylib.DrawText($"Unspent points -> Stat {_player.StatPoints} / Feat {_player.FeatPoints} / Spell {_player.SpellPickPoints}", rightX + 10, ry, 15, ColLightGray);
        ry += 24;

        Raylib.DrawText("Combat Profile", rightX + 10, ry, 22, ColYellow);
        ry += 28;
        Raylib.DrawText($"Role: {GetClassCombatTag(_player.CharacterClass.Name)}", rightX + 10, ry, 16, ColLightGray);
        ry += 20;
        Raylib.DrawText($"Melee bonus: {_player.MeleeDamageBonus + classMeleeBonus + _runMeleeBonus + GetConditionMeleeModifier()}", rightX + 10, ry, 16, ColLightGray);
        ry += 20;
        Raylib.DrawText($"Crit chance: {critChance}%", rightX + 10, ry, 16, ColLightGray);
        ry += 20;
        Raylib.DrawText($"Defense: {totalDefense}", rightX + 10, ry, 16, ColLightGray);
        ry += 20;
        Raylib.DrawText($"Flee chance: {totalFleeChance}%  Evasion: {classEvasion}%", rightX + 10, ry, 16, ColLightGray);
        ry += 20;
        if (_player.IsCasterClass)
        {
            Raylib.DrawText($"Spell damage bonus: {_player.SpellDamageBonus + classSpellBonus + _runSpellBonus + GetConditionSpellModifier()}", rightX + 10, ry, 16, ColLightGray);
            ry += 20;
        }

        var armorItemLabel = equippedArmorItem == null ? "None" : equippedArmorItem.Name;
        var armorStyleFleeLabel = armorStyleFlee >= 0 ? $"+{armorStyleFlee}%" : $"{armorStyleFlee}%";
        ry += 8;
        Raylib.DrawText("Armor Profile", rightX + 10, ry, 22, ColSkyBlue);
        ry += 28;
        Raylib.DrawText($"Equipped: {armorItemLabel} ({armorStateLabel})", rightX + 10, ry, 16, ColLightGray);
        ry += 20;
        Raylib.DrawText($"Training: {armorTrainingSummary}", rightX + 10, ry, 16, ColLightGray);
        ry += 20;
        Raylib.DrawText($"Armor style bonus: Defense +{armorStyleDefense}  Flee {armorStyleFleeLabel}", rightX + 10, ry, 16, ColLightGray);
        ry += 20;

        ry += 8;
        Raylib.DrawText("Run Identity", rightX + 10, ry, 22, ColSkyBlue);
        ry += 28;
        Raylib.DrawText($"Archetype: {GetRunArchetypeLabel(_runArchetype)}", rightX + 10, ry, 16, ColLightGray);
        ry += 20;
        Raylib.DrawText($"Relic: {GetRunRelicLabel(_runRelic)}", rightX + 10, ry, 16, ColLightGray);
        ry += 20;
        Raylib.DrawText($"Doctrines: {GetMilestoneRanksLabel()}", rightX + 10, ry, 16, ColLightGray);
        ry += 20;
        Raylib.DrawText($"Combat charges: Arc {_milestoneArcChargesThisCombat}  Escape {_milestoneEscapeChargesThisCombat}", rightX + 10, ry, 16, ColLightGray);
        ry += 20;
        var xpRouteLabel = _phase3XpPercentMod > 0
            ? $"+{_phase3XpPercentMod}%"
            : $"{_phase3XpPercentMod}%";
        Raylib.DrawText($"Route pressure: XP {xpRouteLabel}  Enemy atk +{_phase3EnemyAttackBonus}", rightX + 10, ry, 16, ColLightGray);
        ry += 20;
        Raylib.DrawText($"Current macro zone: {GetFloorZoneLabel(_currentFloorZone)}", rightX + 10, ry, 16, ColLightGray);
        ry += 20;
        DrawWrappedText(GetPhase3ObjectiveLabel(), rightX + 10, ry, Math.Max(200, rightW - 24), 14, ColLightGray);
        ry += 32;
        Raylib.DrawText("Major Conditions", rightX + 10, ry, 20, ColAccentRose);
        ry += 24;
        if (_activeMajorConditions.Count == 0)
        {
            Raylib.DrawText(_settingsOptionalConditionsEnabled ? "None active." : "Conditions disabled.", rightX + 10, ry, 15, ColLightGray);
            ry += 20;
        }
        else
        {
            foreach (var condition in _activeMajorConditions)
            {
                Raylib.DrawText($"{GetMajorConditionLabel(condition.Type)} ({condition.Source})", rightX + 10, ry, 15, ColWhite);
                ry += 18;
                DrawWrappedText(GetMajorConditionEffectSummary(condition.Type), rightX + 20, ry, Math.Max(180, rightW - 40), 13, ColLightGray);
                ry += 34;
            }
        }
        Raylib.DrawText($"High-tier purge: {GetConditionPurgeCostLabel()}", rightX + 10, ry, 14, ColSkyBlue);
        ry += 22;
        ry += 8;

        Raylib.DrawText("Skills", rightX + 10, ry, 22, ColWhite);
        ry += 28;
        if (_player.Skills.Count == 0)
        {
            Raylib.DrawText("No skills learned yet.", rightX + 10, ry, 18, ColGray);
            ry += 24;
        }
        else
        {
            foreach (var skill in _player.Skills)
            {
                Raylib.DrawText(skill.Name, rightX + 10, ry, 16, ColWhite);
                ry += 18;
                Raylib.DrawText(_player.GetSkillEffectText(skill), rightX + 18, ry, 14, ColLightGray);
                ry += 20;
            }
        }

        ry += 10;
        Raylib.DrawText("Feats", rightX + 10, ry, 22, ColYellow);
        ry += 28;
        if (_player.Feats.Count == 0)
        {
            Raylib.DrawText("No feats chosen yet.", rightX + 10, ry, 18, ColGray);
            ry += 24;
        }
        else
        {
            foreach (var feat in _player.Feats)
            {
                Raylib.DrawText(feat.Name, rightX + 10, ry, 16, ColWhite);
                ry += 18;
                Raylib.DrawText(_player.GetFeatEffectText(feat), rightX + 18, ry, 14, ColLightGray);
                ry += 20;
            }
        }

        ry += 10;
        if (_player.IsCasterClass)
        {
            Raylib.DrawText("Spellcasting", rightX + 10, ry, 22, ColSkyBlue);
            ry += 28;
            Raylib.DrawText($"Pending spell picks: {_player.SpellPickPoints}", rightX + 10, ry, 16, ColSkyBlue);
            ry += 20;
            Raylib.DrawText($"L1 slots: {_player.GetSpellSlots(1)}/{_player.GetSpellSlotsMax(1)}", rightX + 10, ry, 16, ColLightGray);
            ry += 20;
            Raylib.DrawText($"L2 slots: {_player.GetSpellSlots(2)}/{_player.GetSpellSlotsMax(2)}", rightX + 10, ry, 16, ColLightGray);
            ry += 20;
            Raylib.DrawText($"L3 slots: {_player.GetSpellSlots(3)}/{_player.GetSpellSlotsMax(3)}", rightX + 10, ry, 16, ColLightGray);
            ry += 24;
            Raylib.DrawText("Known Spells", rightX + 10, ry, 20, ColSkyBlue);
            ry += 24;
            foreach (var spell in _player.GetKnownSpells())
            {
                var tierLabel = spell.IsCantrip ? "Cantrip" : $"L{spell.SpellLevel}";
                Raylib.DrawText($"{tierLabel} {spell.Name}", rightX + 10, ry, 15, ColLightGray);
                ry += 18;
            }
        }
        Raylib.EndScissorMode();

        DrawCenteredText("UP/DOWN scroll  |  C or ESC close", w / 2, h - 26, 16, ColGray);
    }

    private void DrawLevelUpMenu()
    {
        if (_player == null) return;

        var w = Raylib.GetScreenWidth();
        var h = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(0, 0, w, h, new Color(0, 0, 0, 210));
        DrawPanel(
            UiLayout.LevelPanelInsetX,
            UiLayout.LevelPanelInsetY,
            w - UiLayout.LevelPanelInsetX * 2,
            h - UiLayout.LevelPanelInsetY * 2,
            ColPanel,
            ColBorder);
        DrawCenteredText("LEVEL UP!", w / 2, 78, 40, ColYellow);
        DrawCenteredText($"Points to spend: {_player.StatPoints}", w / 2, 124, 24, ColWhite);
        DrawCenteredText($"Feat picks pending: {_player.FeatPoints}", w / 2, 150, 20, ColSkyBlue);
        DrawCenteredText($"Spell picks pending: {_player.SpellPickPoints}", w / 2, 172, 20, ColSkyBlue);

        for (var i = 0; i < StatOrder.Length; i++)
        {
            var stat = StatOrder[i];
            var selected = i == _selectedStatIndex;
            var rowY = 198 + i * 34;
            DrawMenuRow(w / 2 - 184, rowY - 6, 368, 30, selected);
            DrawCenteredText($"{stat}: {_player.Stats.Get(stat)}", w / 2, rowY, 23, selected ? ColYellow : ColWhite);
        }

        var selectedStat = StatOrder[Math.Clamp(_selectedStatIndex, 0, StatOrder.Length - 1)];
        var selectedMod = _player.Mod(selectedStat);
        DrawCenteredText($"Selected: {selectedStat} (modifier {selectedMod:+#;-#;0})", w / 2, h - 98, 16, ColLightGray);

        if (!string.IsNullOrWhiteSpace(_selectionMessage))
        {
            DrawFooterBar(UiLayout.LevelFooterX, h - 96, w - UiLayout.LevelFooterInset, 18);
            DrawCenteredText(_selectionMessage, w / 2, h - 95, 13, ColLightGray);
        }

        DrawFooterBar(UiLayout.LevelFooterX, h - 74, w - UiLayout.LevelFooterInset, 22);
        DrawCenteredText("UP/DOWN choose stat  |  ENTER spend point", w / 2, h - 70, 15, ColLightGray);
    }

    private void DrawFeatSelection()
    {
        if (_player == null) return;

        var w = Raylib.GetScreenWidth();
        var h = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(0, 0, w, h, new Color(0, 0, 0, 220));
        DrawPanel(
            UiLayout.SelectionPanelInsetX,
            UiLayout.SelectionPanelInsetY,
            w - UiLayout.SelectionPanelInsetX * 2,
            h - UiLayout.SelectionPanelInsetY * 2,
            ColPanel,
            ColBorder);
        DrawCenteredText("Choose a Feat", w / 2, 60, 34, ColYellow);
        DrawCenteredText($"Feat picks remaining: {_player.FeatPoints}", w / 2, 96, 22, ColSkyBlue);

        if (_featChoices.Count == 0)
        {
            DrawCenteredText("No available feats.", w / 2, h / 2, 24, ColGray);
            return;
        }

        EnsureFeatSelectionVisible(_featChoices.Count);
        var start = _featMenuOffset;
        var end = Math.Min(_featChoices.Count, start + FeatVisibleCount);
        for (var i = start; i < end; i++)
        {
            var feat = _featChoices[i];
            var selected = i == _selectedFeatIndex;
            var y = 148 + (i - start) * 68;
            var canLearn = _player.CanLearnFeat(feat, out var blockReason);
            var nameColor = canLearn ? (selected ? ColYellow : ColWhite) : ColGray;
            var statusColor = canLearn ? ColGreen : ColRed;

            DrawMenuRow(UiLayout.SelectionRowX, y - 8, w - UiLayout.SelectionRowInset, 62, selected);
            DrawCenteredText(feat.Name, w / 2, y, 26, nameColor);
            DrawCenteredText(feat.Description, w / 2, y + 22, 16, selected ? ColLightGray : ColGray);

            var statusText = canLearn
                ? $"Effect: {_player.GetFeatEffectText(feat)}"
                : $"Locked: {blockReason}";
            DrawCenteredText(statusText, w / 2, y + 40, 14, statusColor);
        }

        if (start > 0)
        {
            DrawCenteredText("...more above...", w / 2, 128, 14, ColGray);
        }
        if (end < _featChoices.Count)
        {
            DrawCenteredText("...more below...", w / 2, h - 82, 14, ColGray);
        }

        if (!string.IsNullOrWhiteSpace(_selectionMessage))
        {
            DrawFooterBar(UiLayout.SelectionFooterX, h - 82, w - UiLayout.SelectionFooterInset, 18);
            DrawCenteredText(_selectionMessage, w / 2, h - 81, 13, ColLightGray);
        }

        DrawFooterBar(UiLayout.SelectionFooterX, h - 58, w - UiLayout.SelectionFooterInset, 18);
        DrawCenteredText("UP/DOWN choose feat  |  ENTER learn legal feat", w / 2, h - 57, 13, ColLightGray);
    }

    private void DrawSkillSelection()
    {
        if (_player == null) return;

        var w = Raylib.GetScreenWidth();
        var h = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(0, 0, w, h, new Color(0, 0, 0, 220));
        DrawPanel(
            UiLayout.SelectionPanelInsetX,
            UiLayout.SelectionPanelInsetY,
            w - UiLayout.SelectionPanelInsetX * 2,
            h - UiLayout.SelectionPanelInsetY * 2,
            ColPanel,
            ColBorder);
        DrawCenteredText("Choose a New Skill", w / 2, 62, 34, ColWhite);
        DrawCenteredText($"Available skills: {_skillChoices.Count}", w / 2, 98, 18, ColSkyBlue);

        if (_skillChoices.Count == 0)
        {
            DrawCenteredText("No skills available.", w / 2, h / 2, 22, ColGray);
            return;
        }

        EnsureSkillSelectionVisible(_skillChoices.Count);
        var start = _skillMenuOffset;
        var end = Math.Min(_skillChoices.Count, start + SkillVisibleCount);
        for (var i = start; i < end; i++)
        {
            var skill = _skillChoices[i];
            var selected = i == _selectedSkillIndex;
            var y = 136 + (i - start) * 78;

            DrawMenuRow(UiLayout.SelectionRowX, y - 8, w - UiLayout.SelectionRowInset, 70, selected);
            DrawCenteredText(skill.Name, w / 2, y, 24, selected ? ColYellow : ColWhite);
            DrawCenteredText(skill.Description, w / 2, y + 24, 16, selected ? ColLightGray : ColGray);
            DrawCenteredText($"Current effect: {_player.GetSkillEffectText(skill)}", w / 2, y + 46, 14, selected ? ColGreen : ColDarkGreen);
        }

        if (start > 0)
        {
            DrawCenteredText("...more above...", w / 2, 122, 14, ColGray);
        }
        if (end < _skillChoices.Count)
        {
            DrawCenteredText("...more below...", w / 2, h - 82, 14, ColGray);
        }

        if (!string.IsNullOrWhiteSpace(_selectionMessage))
        {
            DrawFooterBar(UiLayout.SelectionFooterX, h - 82, w - UiLayout.SelectionFooterInset, 18);
            DrawCenteredText(_selectionMessage, w / 2, h - 81, 13, ColLightGray);
        }

        DrawFooterBar(UiLayout.SelectionFooterX, h - 58, w - UiLayout.SelectionFooterInset, 18);
        DrawCenteredText("UP/DOWN browse skills  |  ENTER learn", w / 2, h - 57, 13, ColLightGray);
    }

    private void DrawSpellSelection()
    {
        if (_player == null) return;

        var w = Raylib.GetScreenWidth();
        var h = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(0, 0, w, h, new Color(0, 0, 0, 220));
        DrawPanel(
            UiLayout.SelectionPanelInsetX,
            UiLayout.SpellSelectionPanelInsetY,
            w - UiLayout.SelectionPanelInsetX * 2,
            h - UiLayout.SpellSelectionPanelInsetY * 2,
            ColPanel,
            ColBorder);
        DrawCenteredText(_spellSelectionTitle, w / 2, 52, 34, ColSkyBlue);
        DrawCenteredText($"Spell picks remaining: {_player.SpellPickPoints}", w / 2, 92, 22, ColWhite);

        if (_spellLearnChoices.Count == 0)
        {
            DrawCenteredText("No learnable spells available right now.", w / 2, h / 2, 24, ColGray);
            return;
        }

        EnsureSpellLearnSelectionVisible(_spellLearnChoices.Count);
        var start = _spellLearnMenuOffset;
        var end = Math.Min(_spellLearnChoices.Count, start + SpellLearnVisibleCount);
        for (var i = start; i < end; i++)
        {
            var spell = _spellLearnChoices[i];
            var selected = i == _selectedSpellLearnIndex;
            var y = 134 + (i - start) * 68;
            var tier = spell.IsCantrip ? "Cantrip" : $"Level {spell.SpellLevel}";
            var canLearn = _player.CanLearnSpell(spell, out var blockReason);
            var known = _player.KnowsSpell(spell.Id);
            var nameColor = canLearn ? (selected ? ColYellow : ColWhite) : ColGray;
            var statusColor = canLearn ? ColGreen : ColRed;

            DrawMenuRow(UiLayout.SelectionRowX, y - 8, w - UiLayout.SelectionRowInset, 58, selected);
            DrawCenteredText($"{spell.Name} ({tier})", w / 2, y, 24, nameColor);
            DrawCenteredText(spell.Description, w / 2, y + 22, 16, selected ? ColLightGray : ColGray);

            var statusText = canLearn
                ? "Learnable now"
                : known
                    ? "Locked: Already learned."
                    : $"Locked: {blockReason}";
            DrawCenteredText(statusText, w / 2, y + 42, 14, statusColor);
        }

        if (start > 0)
        {
            DrawCenteredText("...more above...", w / 2, 122, 14, ColGray);
        }
        if (end < _spellLearnChoices.Count)
        {
            DrawCenteredText("...more below...", w / 2, h - 38, 14, ColGray);
        }

        if (!string.IsNullOrWhiteSpace(_selectionMessage))
        {
            DrawFooterBar(UiLayout.SelectionFooterX, h - 82, w - UiLayout.SelectionFooterInset, 18);
            DrawCenteredText(_selectionMessage, w / 2, h - 81, 13, ColLightGray);
        }

        DrawFooterBar(UiLayout.SelectionFooterX, h - 58, w - UiLayout.SelectionFooterInset, 18);
        DrawCenteredText("UP/DOWN browse spells  |  ENTER learn legal spell", w / 2, h - 57, 13, ColLightGray);
    }

    private void DrawRewardChoice()
    {
        if (_player == null || _activeRewardNode == null) return;

        var w = Raylib.GetScreenWidth();
        var h = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(0, 0, w, h, new Color(0, 0, 0, 215));
        DrawPanel(
            UiLayout.RewardPanelInsetX,
            UiLayout.RewardPanelInsetY,
            w - UiLayout.RewardPanelInsetX * 2,
            h - UiLayout.RewardPanelInsetY * 2,
            ColPanel,
            ColBorder);

        DrawCenteredText(_activeRewardNode.Name, w / 2, 74, 36, ColYellow);
        var headerDescription = IsPhase3RouteChoiceActive()
            ? "Phase 3 branch decision: lock one route and accept its pressure profile."
            : IsPhase3RiskEventActive()
                ? "Risk/Reward event: each option grants value and imposes a real cost."
            : IsArchetypeChoiceActive()
                ? "Choose a path now. Future combat-edge rewards will scale with this archetype."
            : IsRelicCheckpointActive()
                ? "Milestone checkpoint. Choose one relic to define your run pattern."
            : IsMilestoneCheckpointActive()
                ? "Checkpoint interval reached. Choose one doctrine upgrade."
            : _activeRewardNode.Description;
        DrawCenteredText(headerDescription, w / 2, 116, 18, ColLightGray);
        DrawCenteredText($"Build: {GetRunIdentityLabel()}", w / 2, 138, 15, ColSkyBlue);

        var optionNames = GetActiveRewardOptionNames();
        var optionDescriptions = GetActiveRewardOptionDescriptions();

        for (var i = 0; i < optionNames.Length; i++)
        {
            var selected = i == _selectedRewardOptionIndex;
            var rowY = 174 + i * 86;
            DrawMenuRow(UiLayout.RewardOptionX, rowY - 8, w - UiLayout.RewardOptionInset, 74, selected);
            DrawCenteredText(optionNames[i], w / 2, rowY + 2, 26, selected ? ColYellow : ColWhite);
            DrawCenteredText(optionDescriptions[i], w / 2, rowY + 34, 16, selected ? ColLightGray : ColGray);
        }

        DrawFooterBar(UiLayout.RewardFooterX, h - 88, w - UiLayout.RewardFooterInset, 22);
        DrawCenteredText(_rewardMessage, w / 2, h - 86, 14, ColLightGray);
        DrawFooterBar(UiLayout.RewardFooterX, h - 58, w - UiLayout.RewardFooterInset, 22);
        DrawCenteredText("UP/DOWN choose reward  |  ENTER claim  |  ESC defer", w / 2, h - 56, 14, ColLightGray);
    }

    private void DrawVictoryScreen()
    {
        var w = Raylib.GetScreenWidth();
        var h = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(0, 0, w, h, new Color(0, 0, 0, 220));
        DrawPanel(
            UiLayout.VictoryPanelInsetX,
            UiLayout.VictoryPanelInsetY,
            w - UiLayout.VictoryPanelInsetX * 2,
            h - UiLayout.VictoryPanelInsetY * 2,
            ColPanel,
            ColBorder);

        DrawCenteredText("FLOOR 1 CLEARED", w / 2, 148, 52, ColYellow);
        DrawCenteredText("The sanctum has fallen and the route is secured.", w / 2, 222, 20, ColLightGray);

        var summaryY = 270;
        DrawCenteredText($"Level {_player?.Level ?? 1}   Pack Items {GetInventoryQuantityTotal()}", w / 2, summaryY, 22, ColSkyBlue);
        DrawCenteredText($"Archetype: {GetRunArchetypeLabel(_runArchetype)}", w / 2, summaryY + 34, 18, ColLightGray);
        DrawCenteredText($"Relic: {GetRunRelicLabel(_runRelic)}", w / 2, summaryY + 58, 17, ColLightGray);
        DrawCenteredText($"Route: {GetPhase3RouteLabel(_phase3RouteChoice)}  XP {_phase3XpPercentMod:+#;-#;0}%  Enemy atk +{_phase3EnemyAttackBonus}", w / 2, summaryY + 80, 16, ColLightGray);
        DrawCenteredText($"Doctrine ranks: {GetMilestoneRanksLabel()}", w / 2, summaryY + 102, 16, ColLightGray);
        DrawCenteredText($"Run bonuses: Melee +{_runMeleeBonus}  Spell +{_runSpellBonus}  Defense +{_runDefenseBonus}  Crit +{_runCritBonus}%  Flee +{_runFleeBonus}%", w / 2, summaryY + 126, 17, ColLightGray);
        DrawCenteredText("Press ENTER to return to title", w / 2, h - 146, 20, ColWhite);
    }

    private void DrawDeathScreen()
    {
        var w = Raylib.GetScreenWidth();
        var h = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(0, 0, w, h, new Color(0, 0, 0, 228));
        DrawPanel(
            UiLayout.DeathPanelInsetX,
            UiLayout.DeathPanelInsetY,
            w - UiLayout.DeathPanelInsetX * 2,
            h - UiLayout.DeathPanelInsetY * 2,
            ColPanel,
            ColAccentRose);
        DrawCenteredText("YOU DIED", w / 2, h / 2 - 52, 56, ColAccentRose);
        DrawCenteredText("Your HP has fallen to zero.", w / 2, h / 2 + 6, 22, ColLightGray);
        DrawFooterBar(UiLayout.DeathFooterX, h / 2 + 48, w - UiLayout.DeathFooterInset, 24);
        DrawCenteredText("Press ENTER to return to main menu", w / 2, h / 2 + 52, 16, ColLightGray);
    }

    private static void DrawPanel(int x, int y, int width, int height, Color fill, Color border)
    {
        Raylib.DrawRectangle(x, y, width, height, fill);
        Raylib.DrawRectangleLines(x, y, width, height, border);
    }

    private static void DrawMenuRow(int x, int y, int width, int height, bool selected)
    {
        Raylib.DrawRectangle(x, y, width, height, selected ? ColSelectBg : ColSelectBgSoft);
    }

    private static void DrawFooterBar(int x, int y, int width, int height)
    {
        Raylib.DrawRectangle(x, y, width, height, ColFooter);
    }

    private static int GetCenteredPanelX(int screenW, int panelW)
    {
        return (screenW - panelW) / 2;
    }

    private static int GetCenteredPanelY(int screenH, int panelH)
    {
        return Math.Max(UiLayout.MinTopMargin, (screenH - panelH) / 2);
    }


    public void Dispose()
    {
        _spriteLibrary.Dispose();
        _map.Dispose();
    }

    private static bool Pressed(int key)
    {
        return Raylib.IsKeyPressed((KeyboardKey)key);
    }

    private static void DrawCenteredText(string text, int centerX, int y, int size, Color color)
    {
        var width = Raylib.MeasureText(text, size);
        Raylib.DrawText(text, centerX - width / 2, y, size, color);
    }

    private static void DrawWrappedText(string text, int x, int y, int maxWidth, int size, Color color)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var line = string.Empty;
        var drawY = y;

        foreach (var word in words)
        {
            var candidate = string.IsNullOrEmpty(line) ? word : $"{line} {word}";
            if (Raylib.MeasureText(candidate, size) > maxWidth && !string.IsNullOrEmpty(line))
            {
                Raylib.DrawText(line, x, drawY, size, color);
                drawY += size + 4;
                line = word;
            }
            else
            {
                line = candidate;
            }
        }

        if (!string.IsNullOrEmpty(line))
        {
            Raylib.DrawText(line, x, drawY, size, color);
        }
    }
}





