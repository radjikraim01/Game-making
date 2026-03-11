using Raylib_cs;

namespace DungeonEscape.Core;

public sealed class Game : IDisposable
{
    private static Font _uiFont;
    private static bool _uiFontInitialized;
    private static bool _uiFontLoadedFromFile;
    private const float UiFontSpacing = 1f;
    private const string PrimaryUiFontPath = @"C:\Windows\Fonts\segoeui.ttf";
    private const string SecondaryUiFontPath = @"C:\Windows\Fonts\verdana.ttf";
    private const string TertiaryUiFontPath = @"C:\Windows\Fonts\trebuc.ttf";

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

    private static readonly (EquipmentSlot Slot, int SlotIndex, string Label)[] PauseEquipmentDisplaySlots =
    {
        (EquipmentSlot.MainHand, 0, "Main Hand"),
        (EquipmentSlot.OffHand, 0, "Off Hand"),
        (EquipmentSlot.Armor, 0, "Armor"),
        (EquipmentSlot.Head, 0, "Head"),
        (EquipmentSlot.Neck, 0, "Neck"),
        (EquipmentSlot.Cloak, 0, "Cloak"),
        (EquipmentSlot.Belt, 0, "Belt"),
        (EquipmentSlot.Ring, 0, "Ring 1"),
        (EquipmentSlot.Ring, 1, "Ring 2")
    };

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
    private int _combatSkillMenuOffset;
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
    private int _pauseInventoryOffset;
    private int _pauseSaveOffset;
    private int _pauseLoadOffset;
    private int _selectedRewardOptionIndex;

    private string _startMenuMessage = string.Empty;
    private string _pendingName = string.Empty;
    private string _creationMessage = string.Empty;
    private string _spellSelectionTitle = string.Empty;
    private string _selectionMessage = string.Empty;
    private string _pauseMessage = string.Empty;
    private string _rewardMessage = string.Empty;
    private string _selectedPlayerSpriteId = "knight_m";
    private bool _creationClassConfirmed;
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
    private readonly int[] _levelUpAllocatedStats = new int[6];
    private readonly List<int> _levelUpStatAllocationOrder = new();
    private bool _levelUpSessionActive;
    private readonly List<SaveEntrySummary> _pauseSaveEntries = new();
    private readonly List<SaveEntrySummary> _pauseLoadEntries = new();
    private readonly List<string> _combatLog = new();
    private Enemy? _currentEnemy;
    private bool _warCryAvailable;
    private bool _arcaneWardUsedThisCombat;
    private bool _channelDivinityUsedThisCombat;
    private bool _cuttingWordsUsedThisCombat;
    private bool _layOnHandsUsedThisCombat;
    private bool _channelDivinityPrimed;
    // Phase B active feat state
    private bool _battleCryUsedThisCombat;
    private bool _battleCryPrimed;
    private bool _vanishUsedThisCombat;
    private bool _vanishPrimed;
    private bool _divineSmiteUsedThisCombat;
    private bool _divineSmitePrimed;
    private bool _empowerSpellUsedThisCombat;
    private bool _empowerSpellPrimed;
    private bool _divineFavorUsedThisCombat;
    private bool _divineFavorActive;
    private bool _magicWeaponActive;
    private bool _flameArrowsActive;
    private bool _zephyrStrikeActive;
    private bool _zephyrStrikeHitPrimed;
    private bool _crusadersMantleActive;
    // Batch 1 buff state
    private bool _shieldOfFaithActive;
    private bool _blessActive;
    private bool _heroismActive;
    private bool _mageArmorActive;
    private bool _shieldSpellActive;
    private int  _shieldSpellTurnsLeft;
    private bool _barkskinActive;
    private bool _blurActive;
    private bool _hasteActive;
    private int  _aidMaxHpBonus;
    private int  _playerTempHp;
    // Batch 2 — tactical combat spells
    private int  _mirrorImageCharges;
    private bool _absorbElementsCharged;
    private bool _expeditiousRetreatActive;
    private bool _longstriderActive;
    private bool _hexActive;
    private bool _protFromEvilActive;
    private bool _sanctuaryActive;
    private bool _compelledDuelActive;
    private bool _enhanceAbilityActive;
    // Batch 3 — reactive & retaliation spells
    private bool _hellishRebukePrimed;
    private int  _armorOfAgathysTempHp;
    private bool _fireShieldActive;
    private bool _wrathOfStormPrimed;
    private bool _spiritShroudActive;
    private bool _deathWardActive;
    private bool _holyRebukePrimed;
    private bool _thornsActive;
    private bool _stoneskinActive;
    private bool _cuttingWordsPrimed;
    private bool _greaterInvisibilityActive;
    // Batch 4 — expanded arsenal
    private bool _counterspellPrimed;
    private bool _invisibilityActive;
    private bool _elementalWeaponActive;
    private string _elementalWeaponElement = string.Empty;
    private bool _revivifyUsed;
    // Batch 5 — signature powers
    private bool _blinkActive;
    private bool _protEnergyActive;
    private string _protEnergyElement = string.Empty;
    private bool _beaconOfHopeActive;
    private bool _majorImageActive;
    private bool _auraOfCourageActive;
    private SummonInstance? _activeSummon;
    private TransformationInstance? _activeTransformation;
    private string _pendingFormSpellId = string.Empty;
    private string[] _pendingFormOptions = Array.Empty<string>();
    private int _formSelectionIndex;
    private bool _wordOfRenewalUsedThisCombat;
    // D&D feat active state
    private bool _defensiveDuelistAvailable;
    private bool _luckyUsedThisCombat;
    private bool _luckyPrimed;
    private bool _sentinelAvailable;
    private bool _indomitableAvailable;
    private bool _uncannyDodgeAvailable;
    private bool _enemyHasActedThisCombat;
    private bool _metamagicUsedThisCombat;
    private bool _metamagicPrimed;
    private bool _sharpshooterUsedThisCombat;
    private bool _sharpshooterPrimed;
    private bool _countercharmAvailable;
    private bool _riposteAvailable;
    private bool _shieldExpertAvailable;
    private bool _recklessAttackUsedThisCombat;
    private bool _overchannelUsedThisCombat;
    private bool _overchannelPrimed;
    private bool _spiritualWeaponUsedThisCombat;
    private bool _spiritualWeaponPrimed;
    private bool _bardicInspirationUsedThisCombat;
    private bool _bardicInspirationPrimed;
    private bool _bardicInspirationForAttack;
    private bool _enemyNextAttackDisadvantage;
    private bool _enemyNextAttackAdvantage;
    private string _equippedWeaponId = "unarmed";
    private bool _playerAttackAdvantage;
    private bool _playerAttackDisadvantage;
    private bool _playerSaveAdvantage;
    private bool _playerSaveDisadvantage;
    private int _packEnemiesRemainingAfterCurrent;
    private bool _encounterActive;
    private int _encounterRound = 1;
    private readonly List<Enemy> _encounterEnemies = new();
    private readonly List<EncounterInitiativeSlot> _encounterTurnOrder = new();
    private readonly List<CombatHazardState> _activeCombatHazards = new();
    private int _encounterTurnIndex;
    private string _encounterCurrentCombatantId = string.Empty;
    private int _selectedEncounterTargetIndex = -1;
    private string _pendingCombatSpellId = string.Empty;
    private int _pendingCombatSpellVariantIndex;
    private int _combatSpellTargetCursorX = -1;
    private int _combatSpellTargetCursorY = -1;
    private string _activeConcentrationSpellId = string.Empty;
    private string _activeConcentrationLabel = string.Empty;
    private int _activeConcentrationRemainingRounds;
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
    private readonly List<PlayerConditionState> _playerConditions = new();
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
    // Only races with sprites are shown in character creation. Others remain defined for future use.
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

    public Game()
    {
        EnsureUiFontLoaded();
    }
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
            "No normal color sense. Magic sense grants limited perception; severe penalties without it."
        ),
        (
            CreationConditionPreset.CrushedLimb,
            "Crushed Limb",
            "Severe mobility/combat strain. High-tier restoration can remove it."
        )
    };
    private static readonly string[] CreationSections = { "Identity", "Class", "Stats", "Spells", "Feats", "Review" };
    private const int SkillVisibleCount = 4;
    private const int SpellMenuVisibleCount = 4;
    private const int CombatItemVisibleCount = 4;
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
        [Race.Human] = "Balanced and adaptable across all classes. (+1 all stats)",
        [Race.Elf] = "Agile and perceptive, fitting scouts and casters. (+2 DEX, +1 INT)",
        [Race.Dwarf] = "Hardy and resilient, great frontline survivability. (+2 CON, +1 WIS)",
        [Race.HalfOrc] = "Powerful and tough, built for the front of any fight. (+2 STR, +1 CON)",
        [Race.Halfling] = "Quick and surprisingly charming under pressure. (+2 DEX, +1 CHA)",
        [Race.Gnome] = "Sharp-minded and surprisingly durable for their size. (+2 INT, +1 CON)",
        [Race.Tiefling] = "Infernal heritage sharpens both will and wit. (+2 CHA, +1 INT)"
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
    private static readonly string[] ChromaticOrbVariants = { "acid", "cold", "fire", "lightning", "poison", "thunder" };
    private static readonly string[] CommandVariants = { "halt", "flee", "grovel" };
    private static readonly string[] ElementalWeaponVariants = { "fire", "cold", "lightning", "thunder" };
    private static readonly string[] ProtFromEnergyVariants = { "fire", "cold", "lightning", "acid" };

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
            MajorConditionType.ArcaneBlindness => "No normal color vision. Magic sense allows limited perception; severe spell penalties without it.",
            MajorConditionType.CrushedLimb => "Melee, defense, and flee performance reduced until restored.",
            _ => "Condition effects unknown."
        };
    }

    private string GetConditionPurgeCostLabel()
    {
        return $"{ConditionPurgeHealthPotionCost} Health Potion, {ConditionPurgeHealingDraughtCost} Healing Draught, {ConditionPurgeSharpeningOilCost} Sharpening Oil";
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
        return IsBlindMageModeActive() && _player != null;
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

    private int GetAndConsumeChannelDivinityBonus()
    {
        if (!_channelDivinityPrimed || _player == null) return 0;
        _channelDivinityPrimed = false;
        return _player.ChannelDivinityBonus;
    }

    private int GetAndConsumeEmpowerSpellBonus()
    {
        _empowerSpellPrimed = false;
        return 0;
    }

    private bool ConsumeEmpowerSpellPrime()
    {
        if (!_empowerSpellPrimed) return false;
        _empowerSpellPrimed = false;
        return true;
    }

    private int GetProficiencyBonus()
    {
        if (_player == null) return 2;
        return Math.Clamp(2 + Math.Max(0, (_player.Level - 1) / 4), 2, 6);
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
        // Creation-origin conditions are temporarily disabled in the UI hotfix pass.
        // Keep origin state deterministic to avoid hidden side effects from restored data.
        _creationOriginCondition = CreationConditionPreset.None;
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
        var healingDraught = GetInventoryItem("healing_draught");
        var sharpeningOil = GetInventoryItem("sharpening_oil");

        var hpQty = Math.Max(0, healthPotion?.Quantity ?? 0);
        var hdQty = Math.Max(0, healingDraught?.Quantity ?? 0);
        var oilQty = Math.Max(0, sharpeningOil?.Quantity ?? 0);

        if (hpQty < ConditionPurgeHealthPotionCost ||
            hdQty < ConditionPurgeHealingDraughtCost ||
            oilQty < ConditionPurgeSharpeningOilCost)
        {
            reason = $"Need {GetConditionPurgeCostLabel()}.";
            return false;
        }

        healthPotion!.Quantity -= ConditionPurgeHealthPotionCost;
        healingDraught!.Quantity -= ConditionPurgeHealingDraughtCost;
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
                Id = "healing_draught",
                Name = "Healing Draught",
                Description = "Restore 35% HP.",
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
                Description = "Light armor: +1 defense, +2% flee while equipped. Requires Light armor training.",
                Kind = InventoryItemKind.Equipment,
                Slot = EquipmentSlot.Armor,
                Quantity = 1
            },
            new InventoryItem
            {
                Id = "brigandine_coat",
                Name = "Brigandine Coat",
                Description = "Medium armor: +2 defense while equipped. Requires Medium armor training.",
                Kind = InventoryItemKind.Equipment,
                Slot = EquipmentSlot.Armor,
                Quantity = 0
            },
            new InventoryItem
            {
                Id = "plate_harness",
                Name = "Plate Harness",
                Description = "Heavy armor: +3 defense while equipped. Requires Heavy armor training.",
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
            },
            new InventoryItem
            {
                Id = "iron_greaves",
                Name = "Iron Greaves",
                Description = "Boots slot: +1 defense while equipped.",
                Kind = InventoryItemKind.Equipment,
                Slot = EquipmentSlot.Boots,
                Quantity = 0
            },
            new InventoryItem
            {
                Id = "thieves_gloves",
                Name = "Thieves' Gloves",
                Description = "Gloves slot: +1 melee damage while equipped.",
                Kind = InventoryItemKind.Equipment,
                Slot = EquipmentSlot.Gloves,
                Quantity = 0
            },
            new InventoryItem
            {
                Id = "serpent_bracers",
                Name = "Serpent Bracers",
                Description = "Bracers slot: +1 spell damage while equipped.",
                Kind = InventoryItemKind.Equipment,
                Slot = EquipmentSlot.Bracers,
                Quantity = 0
            },
            new InventoryItem
            {
                Id = "antidote_vial",
                Name = "Antidote Vial",
                Description = "Consumable: Cures the Poisoned condition.",
                Kind = InventoryItemKind.Consumable,
                Quantity = 1
            },
            new InventoryItem
            {
                Id = "smoke_bomb",
                Name = "Smoke Bomb",
                Description = "Consumable buff: +15% flee chance for this run.",
                Kind = InventoryItemKind.Consumable,
                Quantity = 0
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
            ArmorCategory.Unarmored when player.HasFeat("unarmored_defense_feat") => 2,
            ArmorCategory.Light when ArmorTraining.HasTrainingForCategory(player.CharacterClass.Name, player.HasFeat, ArmorCategory.Light) => 1,
            ArmorCategory.Medium when ArmorTraining.HasTrainingForCategory(player.CharacterClass.Name, player.HasFeat, ArmorCategory.Medium) => 2,
            ArmorCategory.Heavy when ArmorTraining.HasTrainingForCategory(player.CharacterClass.Name, player.HasFeat, ArmorCategory.Heavy) => 3,
            _ => 0
        };
    }

    private int GetArmorStateFleeBonus(Player player)
    {
        var current = GetCurrentArmorCategory();
        return current switch
        {
            ArmorCategory.Unarmored when player.HasFeat("unarmored_defense_feat") => 8,
            ArmorCategory.Light when ArmorTraining.HasTrainingForCategory(player.CharacterClass.Name, player.HasFeat, ArmorCategory.Light) => 2,
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
    private static readonly bool EnableRunMetaLayer = false;
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
    private const int ConditionPurgeHealingDraughtCost = 2;
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
            Name = "Healing Draught",
            Rarity = LootRarity.Common,
            InventoryItemId = "healing_draught",
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
        },
        new()
        {
            Name = "Plate Harness",
            Rarity = LootRarity.Rare,
            InventoryItemId = "plate_harness",
            MinItemQuantity = 1,
            MaxItemQuantity = 1
        },
        new()
        {
            Name = "Dented Iron Greaves",
            Rarity = LootRarity.Uncommon,
            InventoryItemId = "iron_greaves",
            MinItemQuantity = 1,
            MaxItemQuantity = 1
        },
        new()
        {
            Name = "Thieves' Gloves",
            Rarity = LootRarity.Uncommon,
            InventoryItemId = "thieves_gloves",
            MinItemQuantity = 1,
            MaxItemQuantity = 1
        },
        new()
        {
            Name = "Antidote Vial",
            Rarity = LootRarity.Uncommon,
            InventoryItemId = "antidote_vial",
            MinItemQuantity = 1,
            MaxItemQuantity = 2
        },
        new()
        {
            Name = "Smoke Bomb",
            Rarity = LootRarity.Uncommon,
            InventoryItemId = "smoke_bomb",
            MinItemQuantity = 1,
            MaxItemQuantity = 1
        },
        new()
        {
            Name = "Serpent Bracers",
            Rarity = LootRarity.Rare,
            InventoryItemId = "serpent_bracers",
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
    private static Color ColCyan = new(80, 220, 230, 255);

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

        public static int HudHeight
        {
            get
            {
                var y1 = UiScale(7);
                var y2 = y1 + UiLineH(18);
                var y3 = y2 + UiLineH(20);
                var y4 = y3 + UiLineH(16);
                return y4 + UiLineH(14) + UiScale(6);
            }
        }
        public const int HudPadding = 10;
        public static int RewardBannerY => HudHeight + 2;
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
            case GameState.CombatFormSelection:
                DrawWorld();
                DrawCombatUi();
                DrawFormSelectionUi();
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
            : $"Player={_player.Name} Lv{_player.Level} HP={_player.CurrentHp}/{_player.MaxHp}";
        var enemyInfo = _currentEnemy == null
            ? "CurrentEnemy=<null>"
            : $"CurrentEnemy={_currentEnemy.Type.Name} HP={_currentEnemy.CurrentHp}/{_currentEnemy.Type.MaxHp}";
        var runLayerSummary = EnableRunMetaLayer
            ? $" Archetype={GetRunArchetypeLabel(_runArchetype)} Relic={GetRunRelicLabel(_runRelic)} Route={GetPhase3RouteLabel(_phase3RouteChoice)} P3Kills={_phase3EnemiesDefeated}/{Phase3SanctumUnlockRequiredKills} P3Rewards={GetClaimedPrimaryRewardCount()}/{Phase3SanctumUnlockRequiredRewardNodes} Milestones={GetMilestoneRanksLabel()}"
            : string.Empty;
        return $"State={_gameState} PausedFrom={_pausedFromState}{runLayerSummary} Zone={GetFloorZoneLabel(_currentFloorZone)} CondMode={(_settingsOptionalConditionsEnabled ? "On" : "Off")} Cond={GetActiveMajorConditionSummary()} EnemiesAlive={_enemies.Count(e => e.IsAlive)} EncActive={_encounterActive} EncSize={_encounterEnemies.Count} EncRound={_encounterRound} EncRem={_packEnemiesRemainingAfterCurrent} EncTurn={_encounterTurnIndex}/{Math.Max(0, _encounterTurnOrder.Count - 1)} EncCurrent={_encounterCurrentCombatantId} MoveMode={_combatMoveModeActive} MovePts={_combatMovePointsRemaining}/{_combatMovePointsMax} SpritesReady={_spriteLibrary.IsReady} PlayerSprite={_selectedPlayerSpriteId} {playerInfo} {enemyInfo}";
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
            FloorMacroZone.BranchingDepths => "Central Warrens",
            FloorMacroZone.SanctumRing => "Boss Den",
            _ => "Outer Warrens"
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
        if (!EnableRunMetaLayer)
        {
            return false;
        }

        return _activeRewardNode != null &&
               string.Equals(_activeRewardNode.Id, Phase3RouteForkNodeId, StringComparison.Ordinal);
    }

    private bool IsPhase3RiskEventActive()
    {
        if (!EnableRunMetaLayer)
        {
            return false;
        }

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
        if (!EnableRunMetaLayer)
        {
            if (_floorCleared)
            {
                return "Objective: floor cleared.";
            }

            if (!_phase3SanctumWaveSpawned)
            {
                return "Objective: cut through the goblin warrens and push into the boss den.";
            }

            if (!_bossDefeated)
            {
                return "Objective: defeat the Goblin General.";
            }

            return "Objective: clear the last surviving goblins.";
        }

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
        if (!EnableRunMetaLayer)
        {
            return false;
        }

        return IsStandardRewardNodeActive() &&
               _runArchetype == RunArchetype.None &&
               GetClaimedPrimaryRewardCount() == 0;
    }

    private bool IsRelicCheckpointActive()
    {
        if (!EnableRunMetaLayer)
        {
            return false;
        }

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
        if (!EnableRunMetaLayer)
        {
            return false;
        }

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
            RunArchetype.Arcanist => "Arcanist edge: +1 spell damage and +1 HP on kill.",
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
                var beforeHp = _player.CurrentHp;
                _player.CurrentHp = Math.Min(_player.MaxHp, _player.CurrentHp + hpGain);
                _runCritBonus = Math.Max(-20, _runCritBonus - 2);
                _runFleeBonus = Math.Max(-25, _runFleeBonus - 4);
                resultMessage =
                    $"Cache stabilized: {firstBundle}, {secondBundle}, HP +{_player.CurrentHp - beforeHp}, but crit -2% and flee -4%.";
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
                AddInventoryItemQuantity("healing_draught", 1);
                resultMessage = "Archetype chosen: Arcanist. +2 spell damage, +1 Healing Draught.";
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

        if (_runArchetype == RunArchetype.Arcanist)
        {
            var arcHeal = Math.Max(1, executionRank);
            var beforeArcHp = _player.CurrentHp;
            _player.CurrentHp = Math.Min(_player.MaxHp, _player.CurrentHp + arcHeal);
            var arcHpGained = _player.CurrentHp - beforeArcHp;
            if (arcHpGained > 0)
            {
                PushCombatLog($"Execution Doctrine also restores {arcHpGained} HP.");
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
                    _equippedWeaponId = _player.CharacterClass.StartingWeaponId;
                    _creationPointsRemaining = 25;
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
            case GameState.CombatFormSelection:
                HandleFormSelectionInput();
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
        _creationMessage = "Complete each section in order. A/D changes section and ESC goes back.";
        _startMenuMessage = string.Empty;
        _selectionMessage = string.Empty;

        _pendingName = string.Empty;
        _selectedGenderIndex = 0;
        _selectedRaceIndex = 0;
        _selectedClassIndex = 0;
        _creationClassConfirmed = false;
        SetPlayerAppearanceBySpriteId(ResolveDefaultSpriteForRaceAndGender(Races[_selectedRaceIndex], Genders[_selectedGenderIndex]));
        Array.Clear(_creationAllocatedStats, 0, _creationAllocatedStats.Length);
        _creationStatAllocationOrder.Clear();
        _creationChosenSpellIds.Clear();
        _creationChosenSpellOrder.Clear();
        _creationChosenFeatIds.Clear();
        _creationChosenFeatOrder.Clear();
        _creationPointsRemaining = 25;
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
            // Order list encodes: entry 0..5 = raise stat[entry], entry 6..11 = lower stat[entry-6].
            _creationStatAllocationOrder.RemoveAll(entry => entry < 0 || entry >= StatOrder.Length * 2);

            // Reconstruct deltas from the order list.
            var normalizedDeltas = new int[StatOrder.Length];
            foreach (var entry in _creationStatAllocationOrder)
            {
                if (entry < StatOrder.Length)
                    normalizedDeltas[entry]++;
                else
                    normalizedDeltas[entry - StatOrder.Length]--;
            }

            // Clamp to valid creation range [-3, 10].
            for (var i = 0; i < StatOrder.Length; i++)
                normalizedDeltas[i] = Math.Clamp(normalizedDeltas[i], -3, 10);

            if (!_creationAllocatedStats.SequenceEqual(normalizedDeltas))
                Array.Copy(normalizedDeltas, _creationAllocatedStats, StatOrder.Length);
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
        _equippedWeaponId = _player.CharacterClass.StartingWeaponId;
        if (!IsValidPlayerSpriteId(_selectedPlayerSpriteId))
        {
            SetPlayerAppearanceBySpriteId(ResolveDefaultSpriteForRaceAndGender(chosenRace, chosenGender));
        }

        var spentPoints = 0;
        for (var i = 0; i < StatOrder.Length; i++)
        {
            var delta = _creationAllocatedStats[i];
            if (delta > 0)
            {
                for (var p = 0; p < delta; p++)
                    _player.AllocateCreationStatPoint(StatOrder[i]);
            }
            else if (delta < 0)
            {
                for (var p = 0; p < -delta; p++)
                    _player.DeallocateCreationStatPoint(StatOrder[i]);
            }
            spentPoints += PointBuyCost(10 + delta);
        }

        _creationPointsRemaining = 25 - spentPoints;

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
            var filtered = _player
                .GetClassSpells()
                .Where(spell => !(spell.IsCantrip && _player.KnowsSpell(spell.Id)))
                .OrderBy(spell => spell.SpellLevel)
                .ThenBy(spell => spell.Name, StringComparer.Ordinal)
                .ToList();
            _creationLearnableSpells.AddRange(filtered);
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

    private bool IsCreationClassReady()
    {
        return _creationClassConfirmed;
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
        return IsCreationNameReady() && IsCreationClassReady() && IsCreationStatsReady() && IsCreationSpellsReady() && IsCreationFeatsReady();
    }

    private bool IsCreationSectionReady(int sectionIndex)
    {
        return sectionIndex switch
        {
            0 => IsCreationNameReady(),
            1 => IsCreationClassReady(),
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
            0 => "Identity: type Name, set Gender/Race with LEFT/RIGHT, ENTER steps forward, ESC goes back.",
            1 => "Class: UP/DOWN browse classes. ENTER confirms this class, ESC goes back.",
            2 => "Stats: UP/DOWN choose row. RIGHT adds, LEFT removes. ENTER advances once all points are spent.",
            3 => "Spells: ENTER learns/removes spells. When picks are done, creation auto-advances.",
            4 => "Feats: choose your starting feat. ENTER selects; ESC moves back if you want to change earlier sections.",
            5 => "Review: all checks must be green before starting. ENTER starts the run.",
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

    private void MoveCreationToPreviousSection()
    {
        if (_creationSectionIndex <= 0)
        {
            ReturnToMainMenu();
            return;
        }

        _creationSectionIndex -= 1;
        _creationMessage = $"Back to {CreationSections[_creationSectionIndex]}.";
    }

    private void AdvanceCreationToNextSection(string message)
    {
        var nextSection = Math.Min(_creationSectionIndex + 1, CreationSections.Length - 1);
        while (nextSection < CreationSections.Length - 1 && IsCreationSectionReady(nextSection))
        {
            nextSection += 1;
        }

        _creationSectionIndex = nextSection;
        _creationMessage = message;
    }

    private void HandleCharacterCreationHubInput()
    {
        if (Pressed(KeyEscape))
        {
            MoveCreationToPreviousSection();
            return;
        }

        var isTypingNameField = _creationSectionIndex == 0 && _selectedCreationIdentityIndex == 0;
        if (!isTypingNameField && PressedOrRepeat(KeyA))
        {
            _creationSectionIndex = (_creationSectionIndex - 1 + CreationSections.Length) % CreationSections.Length;
            return;
        }

        if (!isTypingNameField && PressedOrRepeat(KeyD))
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
        const int identityFieldCount = 3;
        _selectedCreationIdentityIndex = Math.Clamp(_selectedCreationIdentityIndex, 0, identityFieldCount - 1);

        if (PressedOrRepeat(KeyUp))
        {
            _selectedCreationIdentityIndex = (_selectedCreationIdentityIndex - 1 + identityFieldCount) % identityFieldCount;
            return;
        }

        if (PressedOrRepeat(KeyDown))
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
                if (IsCreationNameReady())
                {
                    _selectedCreationIdentityIndex = 1;
                    _creationMessage = "Name set. Choose Gender next.";
                }
                else
                {
                    _creationMessage = "Enter a name before moving on.";
                }
            }

            return;
        }

        if (_selectedCreationIdentityIndex == 1)
        {
            if (PressedOrRepeat(KeyLeft))
            {
                _selectedGenderIndex = (_selectedGenderIndex - 1 + Genders.Length) % Genders.Length;
                SetPlayerAppearanceBySpriteId(ResolveDefaultSpriteForRaceAndGender(Races[_selectedRaceIndex], Genders[_selectedGenderIndex]));
                RebuildCreationPlayer(keepStats: true, keepSpells: true, keepFeats: true);
                return;
            }

            if (PressedOrRepeat(KeyRight))
            {
                _selectedGenderIndex = (_selectedGenderIndex + 1) % Genders.Length;
                SetPlayerAppearanceBySpriteId(ResolveDefaultSpriteForRaceAndGender(Races[_selectedRaceIndex], Genders[_selectedGenderIndex]));
                RebuildCreationPlayer(keepStats: true, keepSpells: true, keepFeats: true);
                return;
            }

            if (Pressed(KeyEnter))
            {
                _selectedCreationIdentityIndex = 2;
                _creationMessage = "Gender set. Choose Race next.";
            }

            return;
        }

        if (_selectedCreationIdentityIndex == 2)
        {
            if (PressedOrRepeat(KeyLeft))
            {
                _selectedRaceIndex = (_selectedRaceIndex - 1 + Races.Length) % Races.Length;
                SetPlayerAppearanceBySpriteId(ResolveDefaultSpriteForRaceAndGender(Races[_selectedRaceIndex], Genders[_selectedGenderIndex]));
                RebuildCreationPlayer(keepStats: true, keepSpells: true, keepFeats: true);
                return;
            }

            if (PressedOrRepeat(KeyRight))
            {
                _selectedRaceIndex = (_selectedRaceIndex + 1) % Races.Length;
                SetPlayerAppearanceBySpriteId(ResolveDefaultSpriteForRaceAndGender(Races[_selectedRaceIndex], Genders[_selectedGenderIndex]));
                RebuildCreationPlayer(keepStats: true, keepSpells: true, keepFeats: true);
                return;
            }

            if (Pressed(KeyEnter))
            {
                AdvanceCreationToNextSection("Identity locked. Choose a class.");
            }

            return;
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
        if (PressedOrRepeat(KeyUp))
        {
            ChangeCreationClass(-1);
            return;
        }

        if (PressedOrRepeat(KeyDown))
        {
            ChangeCreationClass(1);
            return;
        }

        if (Pressed(KeyEnter))
        {
            _creationClassConfirmed = true;
            AdvanceCreationToNextSection("Class confirmed. Allocate your 6 stat points.");
        }
    }

    private void ChangeCreationClass(int delta)
    {
        var next = (_selectedClassIndex + delta + CharacterClasses.All.Count) % CharacterClasses.All.Count;
        if (next == _selectedClassIndex) return;

        _selectedClassIndex = next;
        _creationClassConfirmed = false;
        RebuildCreationPlayer(keepStats: false, keepSpells: false, keepFeats: true);
        _creationSelectionIndex = 0;
        _selectedSpellLearnIndex = 0;
        _creationMessage = "Class changed: stat/spell setup reset and starting feat revalidated. Press ENTER to confirm this class.";
    }

    private void HandleCreationStatsInput()
    {
        var undoIndex = StatOrder.Length;
        var menuCount = StatOrder.Length + 2;
        if (PressedOrRepeat(KeyUp))
        {
            _creationSelectionIndex = (_creationSelectionIndex - 1 + menuCount) % menuCount;
            return;
        }

        if (PressedOrRepeat(KeyDown))
        {
            _creationSelectionIndex = (_creationSelectionIndex + 1) % menuCount;
            return;
        }

        if (_creationSelectionIndex < StatOrder.Length)
        {
            var stat = StatOrder[_creationSelectionIndex];
            var curDelta = _creationAllocatedStats[_creationSelectionIndex];
            var boughtScore = 10 + curDelta;

            if (PressedOrRepeat(KeyRight))
            {
                if (_player == null) return;
                if (boughtScore >= 20)
                {
                    _creationMessage = $"{stat} is already at the creation maximum (20).";
                    return;
                }
                var costToRaise = PointBuyCostToRaise(boughtScore);
                if (costToRaise > _creationPointsRemaining)
                {
                    _creationMessage = $"Not enough points to raise {stat} (costs {costToRaise}, have {_creationPointsRemaining}).";
                    return;
                }
                _player.AllocateCreationStatPoint(stat);
                _creationAllocatedStats[_creationSelectionIndex]++;
                _creationStatAllocationOrder.Add(_creationSelectionIndex);
                _creationPointsRemaining -= costToRaise;
                var newValAfterRaise = _player.Stats.Get(stat);
                _creationMessage = _creationPointsRemaining == 0
                    ? $"{stat} raised to {newValAfterRaise}. All 25 points spent."
                    : $"{stat} raised to {newValAfterRaise}. {_creationPointsRemaining} points left.";
                return;
            }

            if (PressedOrRepeat(KeyLeft))
            {
                if (_player == null) return;
                if (boughtScore <= 7)
                {
                    _creationMessage = $"{stat} is already at the minimum (7).";
                    return;
                }
                var refund = PointBuyCostToLower(boughtScore);
                _player.DeallocateCreationStatPoint(stat);
                _creationAllocatedStats[_creationSelectionIndex]--;
                _creationStatAllocationOrder.Add(_creationSelectionIndex + StatOrder.Length);
                _creationPointsRemaining += refund;
                var newValAfterLower = _player.Stats.Get(stat);
                _creationMessage = $"{stat} lowered to {newValAfterLower}. {_creationPointsRemaining} points left.";
                return;
            }

            if (Pressed(KeyEnter))
            {
                if (_creationPointsRemaining == 0)
                    AdvanceCreationToNextSection("Stats complete. Move on to spells.");
                else
                    _creationMessage = $"Use RIGHT to raise and LEFT to lower. {_creationPointsRemaining} points remaining.";
                return;
            }

            return;
        }

        if (!Pressed(KeyEnter)) return;

        if (_creationSelectionIndex == undoIndex)
        {
            if (_creationStatAllocationOrder.Count == 0)
            {
                _creationMessage = "No stat allocations to undo.";
                return;
            }

            var lastEntry = _creationStatAllocationOrder[^1];
            _creationStatAllocationOrder.RemoveAt(_creationStatAllocationOrder.Count - 1);

            int undoStatIdx;
            bool undoWasRaise;
            if (lastEntry >= 0 && lastEntry < StatOrder.Length)
            {
                undoStatIdx = lastEntry;
                undoWasRaise = true;
            }
            else if (lastEntry >= StatOrder.Length && lastEntry < StatOrder.Length * 2)
            {
                undoStatIdx = lastEntry - StatOrder.Length;
                undoWasRaise = false;
            }
            else
            {
                RebuildCreationPlayer(keepStats: true, keepSpells: true, keepFeats: true);
                _creationMessage = "Last allocation could not be resolved; rebuilt.";
                return;
            }

            if (undoWasRaise)
                _creationAllocatedStats[undoStatIdx]--;
            else
                _creationAllocatedStats[undoStatIdx]++;

            RebuildCreationPlayer(keepStats: true, keepSpells: true, keepFeats: true);
            _creationSelectionIndex = undoStatIdx;
            _creationMessage = $"{StatOrder[undoStatIdx]} undo applied. {_creationPointsRemaining} points left.";
            return;
        }

        if (_creationAllocatedStats.All(delta => delta == 0))
        {
            _creationMessage = "Stats are already at baseline (all 10).";
            return;
        }

        Array.Clear(_creationAllocatedStats, 0, _creationAllocatedStats.Length);
        _creationStatAllocationOrder.Clear();
        RebuildCreationPlayer(keepStats: false, keepSpells: true, keepFeats: true);
        _creationSelectionIndex = 0;
        _creationMessage = $"Stats reset to baseline. {_creationPointsRemaining} points available.";
    }

    private void HandleCreationSpellsInput()
    {
        if (_player == null) return;

        var undoIndex = GetCreationSpellUndoRowIndex();
        var resetIndex = GetCreationSpellResetRowIndex();
        var menuCount = GetCreationSpellMenuCount();

        if (PressedOrRepeat(KeyUp))
        {
            if (menuCount > 0)
            {
                _selectedSpellLearnIndex = (_selectedSpellLearnIndex - 1 + menuCount) % menuCount;
                EnsureSpellLearnSelectionVisible(menuCount);
            }
            return;
        }

        if (PressedOrRepeat(KeyDown))
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
            if (IsCreationSpellsReady())
            {
                AdvanceCreationToNextSection("No spell picks remain. Move on to feats.");
            }
            else
            {
                _creationMessage = _player.IsCasterClass
                    ? "No class spells available for this level band."
                    : "This class has no spell list in the current 1-6 scope.";
            }
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
            if (_player.SpellPickPoints == 0)
            {
                AdvanceCreationToNextSection($"{spell.Name} learned. Spell picks complete.");
            }
            else
            {
                _creationMessage = $"{spell.Name} learned. {_player.SpellPickPoints} picks left.";
            }
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
        if (PressedOrRepeat(KeyUp))
        {
            if (menuCount > 0)
            {
                _selectedCreationFeatIndex = (_selectedCreationFeatIndex - 1 + menuCount) % menuCount;
                EnsureCreationFeatSelectionVisible(menuCount);
            }
            return;
        }

        if (PressedOrRepeat(KeyDown))
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
        if (_player.FeatPoints == 0)
        {
            AdvanceCreationToNextSection($"{feat.Name} selected as your starting feat.");
        }
        else
        {
            _creationMessage = $"{feat.Name} selected. {_player.FeatPoints} feat picks left.";
        }
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
            _creationMessage = "Spend all 25 build points before starting.";
            return;
        }

        if (!IsCreationClassReady())
        {
            _creationMessage = "Confirm your class before starting.";
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
            var delta = _creationAllocatedStats[_creationSelectionIndex];
            var boughtScore = 10 + delta;
            if (boughtScore >= 20 || _creationPointsRemaining <= 0) return;
            var cost = PointBuyCostToRaise(boughtScore);
            if (cost > _creationPointsRemaining) return;
            _player.AllocateCreationStatPoint(StatOrder[_creationSelectionIndex]);
            _creationAllocatedStats[_creationSelectionIndex]++;
            _creationStatAllocationOrder.Add(_creationSelectionIndex);
            _creationPointsRemaining -= cost;
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
        _creationClassConfirmed = false;
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
        _combatSkillMenuOffset = 0;
        _skillMenuOffset = 0;
        _spellMenuOffset = 0;
        _combatItemMenuOffset = 0;
        _spellLearnMenuOffset = 0;
        _creationFeatMenuOffset = 0;
        _featMenuOffset = 0;
        _characterSheetScroll = 0;
        _creationSectionIndex = 0;
        _creationSelectionIndex = 0;
        _creationPointsRemaining = 25;
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
        _mageArmorActive = false;
        _aidMaxHpBonus = 0;
        _playerTempHp = 0;
        _shieldSpellActive = false;
        _shieldSpellTurnsLeft = 0;
        // Batch 2 new game reset
        _mirrorImageCharges = 0;
        _absorbElementsCharged = false;
        _expeditiousRetreatActive = false;
        _longstriderActive = false;
        _hexActive = false;
        _protFromEvilActive = false;
        _sanctuaryActive = false;
        _compelledDuelActive = false;
        _enhanceAbilityActive = false;
        // Batch 3 new game reset
        _hellishRebukePrimed = false;
        _armorOfAgathysTempHp = 0;
        _fireShieldActive = false;
        _wrathOfStormPrimed = false;
        _spiritShroudActive = false;
        _deathWardActive = false;
        _holyRebukePrimed = false;
        _thornsActive = false;
        _stoneskinActive = false;
        _cuttingWordsPrimed = false;
        _greaterInvisibilityActive = false;
        // Batch 4+5 new game reset
        _counterspellPrimed = false;
        _invisibilityActive = false;
        _elementalWeaponActive = false;
        _elementalWeaponElement = string.Empty;
        _revivifyUsed = false;
        _blinkActive = false;
        _protEnergyActive = false;
        _protEnergyElement = string.Empty;
        _beaconOfHopeActive = false;
        _majorImageActive = false;
        _auraOfCourageActive = false;
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
        if (!EnableRunMetaLayer)
        {
            SpawnEnemyPack(Phase3UpperRouteEnemyPack);
            SpawnEnemyPack(Phase3LowerRouteEnemyPack);
            _phase3RouteWaveSpawned = true;
            _phase3SanctumWaveSpawned = false;
            return;
        }

        _phase3RouteWaveSpawned = false;
        _phase3SanctumWaveSpawned = false;
    }

    private void DisableRunMetaLayerStateIfNeeded()
    {
        if (EnableRunMetaLayer)
        {
            return;
        }

        _runArchetype = RunArchetype.None;
        _runRelic = RunRelic.None;
        _phase3RouteChoice = Phase3RouteChoice.None;
        _phase3RiskEventResolved = false;
        _phase3XpPercentMod = 0;
        _phase3EnemyAttackBonus = 0;
        _phase3EnemiesDefeated = 0;
        _phase3PreSanctumRewardGranted = false;
        _phase3RouteWaveSpawned = true;
        _milestoneChoicesTaken = 0;
        _milestoneExecutionRank = 0;
        _milestoneArcRank = 0;
        _milestoneEscapeRank = 0;
        ResetRelicCombatTriggers();
        ResetMilestoneCombatTriggers();
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
        if (!EnableRunMetaLayer) return;
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
        AddInventoryItemQuantity("healing_draught", 1);
        return "Upper route boon: +1 melee, +1 spell, +1 Healing Draught.";
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
        if (!EnableRunMetaLayer)
        {
            if (_gameState != GameState.Playing) return;
            if (_phase3SanctumWaveSpawned) return;
            if (_currentFloorZone != FloorMacroZone.SanctumRing) return;

            if (_enemies.Any(enemy =>
                    enemy.IsAlive &&
                    string.Equals(ResolveEnemyTypeKey(enemy.Type), "goblin_general", StringComparison.Ordinal)))
            {
                _phase3SanctumWaveSpawned = true;
                return;
            }

            var bossDenSpawned = SpawnEnemyPack(Phase3SanctumEnemyPack);
            if (bossDenSpawned <= 0)
            {
                return;
            }

            _phase3SanctumWaveSpawned = true;
            ShowRewardMessage($"Boss den reached. {bossDenSpawned} defenders rally around the Goblin General.", requireAcknowledge: false, visibleSeconds: 10);
            return;
        }

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
            var beforeHp = _player.CurrentHp;
            _player.CurrentHp = Math.Min(_player.MaxHp, _player.CurrentHp + hpGain);
            routeOutcome = $"Lower route cache delivers {firstBundle}, {secondBundle}, HP +{_player.CurrentHp - beforeHp}.";
        }

        var summary = string.IsNullOrWhiteSpace(routeOutcome)
            ? $"Sanctum defenders mobilize ({sanctumSpawned} hostiles)."
            : $"Sanctum defenders mobilize ({sanctumSpawned} hostiles). {routeOutcome}";
        ShowRewardMessage(summary, requireAcknowledge: false, visibleSeconds: 12);
    }

    private bool TryOpenPhase3RouteForkChoice()
    {
        if (!EnableRunMetaLayer) return false;
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
        if (!EnableRunMetaLayer) return false;
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
                    var beforeHp = _player.CurrentHp;
                    _player.CurrentHp = Math.Min(_player.MaxHp, _player.CurrentHp + hpGain);
                    resultMessage = $"Recovered supplies: HP +{_player.CurrentHp - beforeHp}.";
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
                        resultMessage = "Combat edge forged (Arcanist): +1 spell damage.";
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
            BeginLevelUpFlow();
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
                ? "healing_draught"
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
        _combatSkillMenuOffset = 0;
        _combatItemMenuOffset = 0;
        _selectedEncounterTargetIndex = -1;
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
            PushCombatLog($"HP {_player.CurrentHp}/{_player.MaxHp}");
            _player.HasUsedSecondWind = false;
            _warCryAvailable = _player.HasSkill("war_cry");
            _arcaneWardUsedThisCombat = false;
            _channelDivinityUsedThisCombat = false;
            _channelDivinityPrimed = false;
            _cuttingWordsUsedThisCombat = false;
            _layOnHandsUsedThisCombat = false;
            _battleCryUsedThisCombat = false;
            _battleCryPrimed = false;
            _vanishUsedThisCombat = false;
            _vanishPrimed = false;
            _divineSmiteUsedThisCombat = false;
            _divineSmitePrimed = false;
            _empowerSpellUsedThisCombat = false;
            _empowerSpellPrimed = false;
            _divineFavorUsedThisCombat = false;
            _divineFavorActive = false;
            _magicWeaponActive = false;
            _flameArrowsActive = false;
            _zephyrStrikeActive = false;
            _zephyrStrikeHitPrimed = false;
            _crusadersMantleActive = false;
            _shieldOfFaithActive = false;
            _blessActive = false;
            _heroismActive = false;
            _shieldSpellActive = false;
            _shieldSpellTurnsLeft = 0;
            _barkskinActive = false;
            _blurActive = false;
            _hasteActive = false;
            _playerTempHp = 0;
            // Batch 2 combat reset
            _mirrorImageCharges = 0;
            _absorbElementsCharged = false;
            _expeditiousRetreatActive = false;
            _longstriderActive = false;
            _hexActive = false;
            _protFromEvilActive = false;
            _sanctuaryActive = false;
            _compelledDuelActive = false;
            _enhanceAbilityActive = false;
            // Batch 3 combat reset
            _hellishRebukePrimed = false;
            _armorOfAgathysTempHp = 0;
            _fireShieldActive = false;
            _wrathOfStormPrimed = false;
            _spiritShroudActive = false;
            _deathWardActive = false;
            _holyRebukePrimed = false;
            _thornsActive = false;
            _stoneskinActive = false;
            _cuttingWordsPrimed = false;
            _greaterInvisibilityActive = false;
            // Batch 4+5 combat reset
            _counterspellPrimed = false;
            _invisibilityActive = false;
            _elementalWeaponActive = false;
            _elementalWeaponElement = string.Empty;
            _revivifyUsed = false;
            _blinkActive = false;
            _protEnergyActive = false;
            _protEnergyElement = string.Empty;
            _beaconOfHopeActive = false;
            _majorImageActive = false;
            _auraOfCourageActive = false;
            _activeSummon = null;
            _activeTransformation = null;
            _wordOfRenewalUsedThisCombat = false;
            // D&D feat reset
            _defensiveDuelistAvailable = _player.HasFeat("defensive_duelist_feat");
            _luckyUsedThisCombat = false;
            _luckyPrimed = false;
            _sentinelAvailable = _player.HasFeat("sentinel_feat");
            _indomitableAvailable = _player.HasFeat("warrior_indomitable_feat");
            _uncannyDodgeAvailable = _player.HasFeat("rogue_uncanny_dodge_feat");
            _enemyHasActedThisCombat = false;
            _metamagicUsedThisCombat = false;
            _metamagicPrimed = false;
            _sharpshooterUsedThisCombat = false;
            _sharpshooterPrimed = false;
            _countercharmAvailable = _player.HasFeat("bard_countercharm_feat");
            _riposteAvailable = _player.HasFeat("riposte_feat");
            _shieldExpertAvailable = _player.HasFeat("shield_expert_feat");
            _recklessAttackUsedThisCombat = false;
            _overchannelUsedThisCombat = false;
            _overchannelPrimed = false;
            _spiritualWeaponUsedThisCombat = false;
            _spiritualWeaponPrimed = false;
            _bardicInspirationUsedThisCombat = false;
            _bardicInspirationPrimed = false;
            _bardicInspirationForAttack = false;
            _enemyNextAttackDisadvantage = false;
            _enemyNextAttackAdvantage = false;
            _playerAttackAdvantage = false;
            _playerAttackDisadvantage = false;
            _playerSaveAdvantage = false;
            _playerSaveDisadvantage = false;
            _playerConditions.Clear();
            // Meditation: restore 1 L1 spell slot at the start of each fight
            if (_player.HasSkill("meditation")) _player.RestoreSpellSlot(1);
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
            if (_activeTransformation != null)
                actions.Add("Dismiss Form");
            else if (_player.GetKnownSpells().Count > 0)
                actions.Add("Spells");
            if (GetCombatConsumables().Count > 0) actions.Add("Items");
            actions.Add("Wait");
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

        if (PressedOrRepeat(KeyLeft))
        {
            CycleEncounterTarget(-1);
            return;
        }

        if (PressedOrRepeat(KeyRight))
        {
            CycleEncounterTarget(1);
            return;
        }

        var actions = GetCombatActions();

        if (PressedOrRepeat(KeyUp))
        {
            _selectedActionIndex = (_selectedActionIndex - 1 + actions.Count) % actions.Count;
            return;
        }

        if (PressedOrRepeat(KeyDown))
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
            case "Dismiss Form":
                if (_activeTransformation != null)
                {
                    RevertTransformation("You dismiss your form, returning to normal.");
                    EndActiveConcentration("Transformation dismissed.");
                }
                break;
            case "Items":
                OpenCombatItemMenu();
                break;
            case "Wait":
                PushCombatLog("You hold position and give up the initiative.");
                DoEnemyAttack();
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
        if (CombatStatusRules.LimitsEnemyAttackRangeToMelee(enemy.StatusEffects))
        {
            return CombatMeleeRangeTiles;
        }

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
        var baseBudget = enemyKey switch
        {
            "goblin_skirmisher" => EnemySkirmisherMoveBudgetTiles,
            "warg" => EnemySkirmisherMoveBudgetTiles,
            _ => EnemyDefaultMoveBudgetTiles
        };

        if (CombatStatusRules.PreventsEnemyMovement(enemy.StatusEffects))
        {
            return 0;
        }

        var penalty = CombatStatusRules.GetMovePenalty(enemy.StatusEffects);
        return Math.Max(0, baseBudget - penalty);
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

        foreach (var step in moveDecision.Steps)
        {
            enemy.X = step.X;
            enemy.Y = step.Y;
            ResolveEnemyEntryHazards(enemy);
            if (!enemy.IsAlive)
            {
                movedTiles = moveDecision.Steps.Count;
                return true;
            }
        }

        movedTiles = moveDecision.Steps.Count;
        PushCombatLog($"{enemy.Type.Name} repositions {movedTiles} tile{(movedTiles == 1 ? string.Empty : "s")}.");
        return true;
    }

    private bool TryExecuteEnemyRetreatMovement(Enemy enemy)
    {
        if (_player == null || !enemy.IsAlive)
        {
            return false;
        }

        var moveBudget = GetEnemyCombatMoveBudget(enemy);
        if (moveBudget <= 0)
        {
            return false;
        }

        var reachable = EncounterMovementRules.BuildReachableTiles(
            enemy.X,
            enemy.Y,
            moveBudget,
            (x, y) => CanEnemyTraverseCombatTile(enemy, x, y));
        if (reachable.Count == 0)
        {
            return false;
        }

        var currentDistance = Math.Abs(enemy.X - _player.X) + Math.Abs(enemy.Y - _player.Y);
        var retreatTile = reachable
            .OrderByDescending(tile => Math.Abs(tile.X - _player.X) + Math.Abs(tile.Y - _player.Y))
            .ThenBy(tile => Math.Abs(tile.X - enemy.X) + Math.Abs(tile.Y - enemy.Y))
            .FirstOrDefault();

        var retreatDistance = Math.Abs(retreatTile.X - _player.X) + Math.Abs(retreatTile.Y - _player.Y);
        if (retreatDistance <= currentDistance)
        {
            return false;
        }

        enemy.X = retreatTile.X;
        enemy.Y = retreatTile.Y;
        ResolveEnemyEntryHazards(enemy);
        PushCombatLog($"{enemy.Type.Name} recoils from fear and falls back.");
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

        return EncounterTargetingRules.ValidateMelee(
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

    private static bool UsesSelfCenteredSpellTargeting(SpellDefinition spell)
    {
        return SpellData.ResolveEffectRoute(spell).TargetShape == SpellTargetShape.Self;
    }

    private static bool UsesFreeTileSpellTargeting(SpellDefinition spell)
    {
        return SpellData.ResolveEffectRoute(spell).TargetShape is
            SpellTargetShape.Tile or
            SpellTargetShape.Radius or
            SpellTargetShape.Line or
            SpellTargetShape.Cone;
    }

    private static IReadOnlyList<string> GetSpellVariantOptions(SpellDefinition spell)
    {
        return spell.Id switch
        {
            "mage_chromatic_orb" => ChromaticOrbVariants,
            "cleric_command" => CommandVariants,
            "paladin_elemental_weapon" => ElementalWeaponVariants,
            "mage_protection_from_energy" => ProtFromEnergyVariants,
            _ => Array.Empty<string>()
        };
    }

    private static bool SpellSupportsVariantSelection(SpellDefinition spell)
    {
        return GetSpellVariantOptions(spell).Count > 0;
    }

    private void ResetPendingCombatSpellVariant(SpellDefinition spell)
    {
        var options = GetSpellVariantOptions(spell);
        _pendingCombatSpellVariantIndex = options.Count == 0
            ? 0
            : Math.Clamp(_pendingCombatSpellVariantIndex, 0, options.Count - 1);
    }

    private bool CyclePendingCombatSpellVariant(SpellDefinition spell, int delta)
    {
        var options = GetSpellVariantOptions(spell);
        if (options.Count == 0)
        {
            _pendingCombatSpellVariantIndex = 0;
            return false;
        }

        var nextIndex = (_pendingCombatSpellVariantIndex + delta) % options.Count;
        if (nextIndex < 0)
        {
            nextIndex += options.Count;
        }

        if (nextIndex == _pendingCombatSpellVariantIndex)
        {
            return false;
        }

        _pendingCombatSpellVariantIndex = nextIndex;
        return true;
    }

    private string GetSelectedPendingSpellVariantId(SpellDefinition spell)
    {
        var options = GetSpellVariantOptions(spell);
        if (options.Count == 0)
        {
            return string.Empty;
        }

        _pendingCombatSpellVariantIndex = Math.Clamp(_pendingCombatSpellVariantIndex, 0, options.Count - 1);
        return options[_pendingCombatSpellVariantIndex];
    }

    private string GetSelectedPendingSpellVariantLabel(SpellDefinition spell)
    {
        var variantId = GetSelectedPendingSpellVariantId(spell);
        if (string.IsNullOrWhiteSpace(variantId))
        {
            return string.Empty;
        }

        return spell.Id switch
        {
            "mage_chromatic_orb" => $"Element: {char.ToUpperInvariant(variantId[0])}{variantId[1..]}",
            "paladin_elemental_weapon" => $"Element: {char.ToUpperInvariant(variantId[0])}{variantId[1..]}",
            "mage_protection_from_energy" => $"Resist: {char.ToUpperInvariant(variantId[0])}{variantId[1..]}",
            "cleric_command" => variantId switch
            {
                "halt" => "Command: Halt",
                "flee" => "Command: Flee",
                "grovel" => "Command: Grovel",
                _ => $"Command: {variantId}"
            },
            _ => variantId
        };
    }

    private void PrimeCombatSpellTargeting(SpellDefinition spell)
    {
        _nextMoveAt = -1;
        if (_player == null)
        {
            _combatSpellTargetCursorX = -1;
            _combatSpellTargetCursorY = -1;
            return;
        }

        if (!UsesFreeTileSpellTargeting(spell))
        {
            _combatSpellTargetCursorX = -1;
            _combatSpellTargetCursorY = -1;
            return;
        }

        var anchor = _currentEnemy != null
            ? (_currentEnemy.X, _currentEnemy.Y)
            : (_player.X, _player.Y);
        _combatSpellTargetCursorX = anchor.Item1;
        _combatSpellTargetCursorY = anchor.Item2;
    }

    private bool TryMoveCombatSpellTargetCursor(int dx, int dy)
    {
        if (_player == null || !TryGetPendingCombatSpell(out var pendingSpell) || !UsesFreeTileSpellTargeting(pendingSpell))
        {
            return false;
        }

        var targetX = _combatSpellTargetCursorX < 0 ? _player.X : _combatSpellTargetCursorX;
        var targetY = _combatSpellTargetCursorY < 0 ? _player.Y : _combatSpellTargetCursorY;
        targetX = Math.Clamp(targetX + dx, 1, GameMap.MapWidthTiles - 2);
        targetY = Math.Clamp(targetY + dy, 1, GameMap.MapHeightTiles - 2);
        if (IsWallOrSealed(targetX, targetY))
        {
            return false;
        }

        _combatSpellTargetCursorX = targetX;
        _combatSpellTargetCursorY = targetY;
        return true;
    }

    private (bool IsLegal, int DistanceTiles, int MaxRangeTiles, bool HasLineOfSight, string BlockedReason) ValidateCombatSpellAim(SpellDefinition spell)
    {
        if (_player == null)
        {
            return (false, 0, GetSpellTargetRangeTiles(spell), false, "No active player.");
        }

        if (UsesSelfCenteredSpellTargeting(spell))
        {
            return (true, 0, 0, true, string.Empty);
        }

        if (UsesFreeTileSpellTargeting(spell))
        {
            var (anchorX, anchorY) = ResolveSpellAnchorTile(spell);
            var maxRange = GetSpellTargetRangeTiles(spell);
            if (anchorX < 0 || anchorY < 0 || IsWallOrSealed(anchorX, anchorY))
            {
                return (false, 0, maxRange, false, "Anchor tile is blocked.");
            }

            var distance = EncounterTargetingRules.GetTileDistance(_player.X, _player.Y, anchorX, anchorY);
            var inRange = distance <= maxRange;
            var hasLineOfSight = HasLineOfSight(_player.X, _player.Y, anchorX, anchorY);
            var blockedReason = !inRange
                ? $"Anchor out of range ({distance}/{maxRange} tiles)."
                : !hasLineOfSight
                    ? "Line of sight to anchor is blocked."
                    : string.Empty;
            return (inRange && hasLineOfSight, distance, maxRange, hasLineOfSight, blockedReason);
        }

        var validation = ValidateCurrentEnemyTargetForSpell(spell);
        var route = SpellData.ResolveEffectRoute(spell);
        var legalityReason = string.Empty;
        if (validation.IsLegal && _currentEnemy != null && TryGetSpellCreatureTypeBlockReason(spell, _currentEnemy, route, out var typeReason))
        {
            legalityReason = typeReason;
        }

        return (
            validation.IsLegal && string.IsNullOrWhiteSpace(legalityReason),
            validation.DistanceTiles,
            validation.MaxRangeTiles,
            validation.HasLineOfSight,
            string.IsNullOrWhiteSpace(legalityReason)
                ? validation.IsLegal ? string.Empty : validation.BuildBlockedReason()
                : legalityReason);
    }

    private string GetSpellTargetDescriptor(SpellDefinition spell)
    {
        if (UsesSelfCenteredSpellTargeting(spell))
        {
            return "Self-centered";
        }

        if (UsesFreeTileSpellTargeting(spell))
        {
            var (anchorX, anchorY) = ResolveSpellAnchorTile(spell);
            return $"Anchor {anchorX},{anchorY}";
        }

        return _currentEnemy?.Type.Name ?? "None";
    }

    private (int X, int Y) ResolveSpellAnchorTile(SpellDefinition spell)
    {
        if (_player == null)
        {
            return (0, 0);
        }

        var route = SpellData.ResolveEffectRoute(spell);
        return route.TargetShape switch
        {
            SpellTargetShape.Self => (_player.X, _player.Y),
            _ when UsesFreeTileSpellTargeting(spell) && _combatSpellTargetCursorX >= 0 && _combatSpellTargetCursorY >= 0 =>
                (_combatSpellTargetCursorX, _combatSpellTargetCursorY),
            _ when _currentEnemy != null => (_currentEnemy.X, _currentEnemy.Y),
            _ => (_player.X, _player.Y)
        };
    }

    private IReadOnlyList<Enemy> ResolveSpellAffectedEnemies(SpellDefinition spell)
    {
        if (_player == null)
        {
            return Array.Empty<Enemy>();
        }

        if (string.Equals(spell.Id, "mage_acid_splash", StringComparison.Ordinal))
        {
            return ResolveAcidSplashTargets();
        }

        var route = SpellData.ResolveEffectRoute(spell);
        var (anchorX, anchorY) = ResolveSpellAnchorTile(spell);
        return EncounterSpellAreaRules.ResolveAffectedEnemies(
            spell,
            route,
            _player.X,
            _player.Y,
            anchorX,
            anchorY,
            GetAliveEncounterEnemies(),
            HasLineOfSight)
            .Where(enemy => SpellAffectsCreatureType(route, enemy))
            .ToList();
    }

    private IReadOnlyList<Enemy> ResolveAcidSplashTargets()
    {
        if (_currentEnemy == null)
        {
            return Array.Empty<Enemy>();
        }

        var targets = new List<Enemy> { _currentEnemy };
        var splashTarget = GetAliveEncounterEnemies()
            .Where(enemy => !ReferenceEquals(enemy, _currentEnemy))
            .Where(enemy => EncounterTargetingRules.GetTileDistance(_currentEnemy.X, _currentEnemy.Y, enemy.X, enemy.Y) <= 1)
            .OrderBy(enemy => EncounterTargetingRules.GetTileDistance(_currentEnemy.X, _currentEnemy.Y, enemy.X, enemy.Y))
            .ThenBy(enemy => enemy.CurrentHp)
            .FirstOrDefault();

        if (splashTarget != null)
        {
            targets.Add(splashTarget);
        }

        return targets;
    }

    private string BuildSpellAffectedTargetSummary(SpellDefinition spell)
    {
        var affectedEnemies = ResolveSpellAffectedEnemies(spell);
        if (affectedEnemies.Count == 0)
        {
            return UsesSelfCenteredSpellTargeting(spell)
                ? "Affects: no enemies currently in area."
                : "Affects: no legal enemies beyond the anchor.";
        }

        var names = string.Join(", ", affectedEnemies
            .Take(3)
            .Select(enemy => enemy.Type.Name));
        if (affectedEnemies.Count > 3)
        {
            names = $"{names}, +{affectedEnemies.Count - 3} more";
        }

        return $"Affects {affectedEnemies.Count}: {names}";
    }

    private static bool IsEmpoweredMeleeSpell(SpellDefinition spell)
    {
        return SpellData.ResolveEffectRoute(spell).CombatFamily == SpellCombatFamily.SmiteStrike ||
               string.Equals(spell.Id, "ranger_ensnaring_strike", StringComparison.Ordinal);
    }

    private IReadOnlyList<CombatStatusApplySpec> ResolveSpellOnHitStatuses(SpellDefinition spell, SpellEffectRouteSpec route)
    {
        if (string.Equals(spell.Id, "mage_chromatic_orb", StringComparison.Ordinal))
        {
            return GetSelectedPendingSpellVariantId(spell) switch
            {
                "acid" => new[] { new CombatStatusApplySpec { Kind = CombatStatusKind.Corroded, Potency = 1, DurationTurns = 2 } },
                "cold" => new[] { new CombatStatusApplySpec { Kind = CombatStatusKind.Chilled, Potency = 1, DurationTurns = 2 } },
                "fire" => new[] { new CombatStatusApplySpec { Kind = CombatStatusKind.Burning, Potency = 2, DurationTurns = 2 } },
                "lightning" => new[] { new CombatStatusApplySpec { Kind = CombatStatusKind.Shocked, Potency = 1, DurationTurns = 1 } },
                "poison" => new[] { new CombatStatusApplySpec { Kind = CombatStatusKind.Poison, Potency = 2, DurationTurns = 2 } },
                _ => Array.Empty<CombatStatusApplySpec>()
            };
        }

        if (string.Equals(spell.Id, "cleric_command", StringComparison.Ordinal))
        {
            return GetSelectedPendingSpellVariantId(spell) switch
            {
                "flee" => new[] { new CombatStatusApplySpec { Kind = CombatStatusKind.Feared, Potency = 1, DurationTurns = 2 } },
                "grovel" => new CombatStatusApplySpec[]
                {
                    new() { Kind = CombatStatusKind.Incapacitated, Potency = 1, DurationTurns = 1 },
                    new() { Kind = CombatStatusKind.Prone, Potency = 1, DurationTurns = 2 }
                },
                _ => new[] { new CombatStatusApplySpec { Kind = CombatStatusKind.Incapacitated, Potency = 1, DurationTurns = 1 } }
            };
        }

        return route.OnHitStatuses;
    }

    private SpellElement ResolveSpellElementForCast(SpellDefinition spell, SpellEffectRouteSpec route)
    {
        if (string.Equals(spell.Id, "mage_chromatic_orb", StringComparison.Ordinal) ||
            string.Equals(spell.Id, "paladin_elemental_weapon", StringComparison.Ordinal) ||
            string.Equals(spell.Id, "mage_protection_from_energy", StringComparison.Ordinal))
        {
            return GetSelectedPendingSpellVariantId(spell) switch
            {
                "acid" => SpellElement.Acid,
                "cold" => SpellElement.Cold,
                "fire" => SpellElement.Fire,
                "lightning" => SpellElement.Lightning,
                "poison" => SpellElement.Poison,
                "thunder" => SpellElement.Thunder,
                _ => route.Element
            };
        }

        return route.Element;
    }

    private string ResolveSpellDamageTagForCast(SpellDefinition spell, SpellEffectRouteSpec route)
    {
        var element = ResolveSpellElementForCast(spell, route);
        return element switch
        {
            SpellElement.Acid => "acid",
            SpellElement.Cold => "cold",
            SpellElement.Fire => "fire",
            SpellElement.Lightning => "lightning",
            SpellElement.Poison => "poison",
            SpellElement.Thunder => "thunder",
            _ => spell.DamageTag
        };
    }

    private (int BaseDamage, int Variance, int ArmorBypass) ResolveSpellDamageProfile(SpellDefinition spell, Enemy target)
    {
        var baseDamage = spell.BaseDamage;
        var variance = spell.Variance;
        var armorBypass = spell.ArmorBypass + (_player?.SpellArmorBypassBonus ?? 0);

        if (string.Equals(spell.Id, "cleric_toll_the_dead", StringComparison.Ordinal) &&
            target.CurrentHp < target.Type.MaxHp)
        {
            baseDamage += 4;
            variance += 2;
        }

        return (baseDamage, variance, armorBypass);
    }

    private int ResolveSpellHitCount(SpellDefinition spell)
    {
        return string.Equals(spell.Id, "mage_scorching_ray", StringComparison.Ordinal) ? 2 : 1;
    }

    private bool TryPushEnemyAwayFromPoint(Enemy enemy, int originX, int originY, int maxTiles, out int pushedTiles)
    {
        pushedTiles = 0;
        var stepX = Math.Sign(enemy.X - originX);
        var stepY = Math.Sign(enemy.Y - originY);
        if (stepX == 0 && stepY == 0)
        {
            stepY = 1;
        }

        for (var step = 0; step < maxTiles; step++)
        {
            var nextX = enemy.X + stepX;
            var nextY = enemy.Y + stepY;
            if (IsWallOrSealed(nextX, nextY))
            {
                break;
            }

            if (_enemies.Any(other => !ReferenceEquals(other, enemy) && other.IsAlive && other.X == nextX && other.Y == nextY))
            {
                break;
            }

            if (_player != null && _player.X == nextX && _player.Y == nextY)
            {
                break;
            }

            enemy.X = nextX;
            enemy.Y = nextY;
            pushedTiles += 1;
        }

        return pushedTiles > 0;
    }

    private bool ResolveEmpoweredMeleeSpellStrike(SpellDefinition spell, SpellEffectRouteSpec route, string tierLabel)
    {
        if (_player == null || _currentEnemy == null)
        {
            return false;
        }

        var meleeValidation = ValidateCurrentEnemyTargetForMelee();
        if (!meleeValidation.IsLegal)
        {
            PushCombatLog($"{spell.Name} requires a melee target: {meleeValidation.BuildBlockedReason()}");
            return false;
        }

        var warCryDamage = 0;
        if (_warCryAvailable && _player.HasSkill("war_cry"))
        {
            warCryDamage = _player.WarCryBonus;
            _warCryAvailable = false;
            PushCombatLog($"War Cry adds {warCryDamage} first-strike damage.");
        }

        var markBonus = GetEnemyIncomingDamageBonus(_currentEnemy);
        var (weaponDamage, crit, rawDamage, armorMitigation, smiteCritThreshold) = CalcPlayerDamage();
        var (bonusBaseDamage, bonusVariance, bonusArmorBypass) = ResolveSpellDamageProfile(spell, _currentEnemy);
        var weaponRiderEmpowerReroll = ConsumeEmpowerSpellPrime();
        var weaponRiderChannelBonus = GetAndConsumeChannelDivinityBonus();
        var spellBasePlusBonus = bonusBaseDamage + _player.SpellDamageBonus + GetClassSpellDamageBonus(_player) + _runSpellBonus + GetConditionSpellModifier() + weaponRiderChannelBonus;
        var (spellDamage, spellRawDamage, spellArmorMitigation, spellStatPower) = CalcSpellDamageAgainstEnemy(
            _currentEnemy, spell.ScalingStat, spellBasePlusBonus, bonusVariance, bonusArmorBypass, spell.SpellLevel);
        if (weaponRiderEmpowerReroll)
        {
            var (spellDamage2, spellRawDamage2, spellArmorMitigation2, spellStatPower2) = CalcSpellDamageAgainstEnemy(
                _currentEnemy, spell.ScalingStat, spellBasePlusBonus, bonusVariance, bonusArmorBypass, spell.SpellLevel);
            if (spellDamage2 > spellDamage)
            {
                (spellDamage, spellRawDamage, spellArmorMitigation, spellStatPower) = (spellDamage2, spellRawDamage2, spellArmorMitigation2, spellStatPower2);
                PushCombatLog("Empowered Spell — the second roll was stronger!");
            }
        }
        var total = weaponDamage + spellDamage + warCryDamage;
        _currentEnemy.CurrentHp = Math.Max(0, _currentEnemy.CurrentHp - total);
        if (total > 0)
        {
            TryBreakDamageSensitiveStatuses(_currentEnemy, spell.Name);
        }

        PushCombatLog($"{spell.Name} ({tierLabel}) empowers your strike for {total} total damage.");
        PushCombatLog($"Weapon raw {rawDamage} - armor {armorMitigation} | Crit {smiteCritThreshold}+{(crit ? " | CRIT x2" : string.Empty)}.");
        PushCombatLog($"Spell rider raw {spellRawDamage} (stat +{spellStatPower}) - armor {spellArmorMitigation}.");
        if (markBonus > 0)
        {
            PushCombatLog($"Marked target suffers +{markBonus} bonus damage.");
        }

        foreach (var statusMessage in ApplySpellOnHitStatuses(spell, route, _currentEnemy))
        {
            PushCombatLog(statusMessage);
        }

        if (_player.PoisonDamage > 0 && _currentEnemy.IsAlive)
        {
            TryApplyOrRefreshEnemyStatus(
                _currentEnemy,
                new CombatStatusApplySpec
                {
                    Kind = CombatStatusKind.Poison,
                    Potency = _player.PoisonDamage,
                    DurationTurns = 2
                },
                "poison_blade",
                "Poison Blade",
                null,
                out var poisonMessage);
            if (!string.IsNullOrWhiteSpace(poisonMessage))
            {
                PushCombatLog(poisonMessage);
            }
        }

        if (_currentEnemy.IsAlive &&
            string.Equals(spell.Id, "paladin_thunderous_smite", StringComparison.Ordinal) &&
            TryPushEnemyAwayFromPoint(_currentEnemy, _player.X, _player.Y, 2, out var pushedTiles))
        {
            PushCombatLog($"{_currentEnemy.Type.Name} is blasted back {pushedTiles} tile(s).");
        }

        PushCombatLog($"{_currentEnemy.Type.Name} HP {_currentEnemy.CurrentHp}/{_currentEnemy.Type.MaxHp}.");
        ApplyRelicMeleeTrigger();
        return true;
    }

    private string GetActiveConcentrationSummary()
    {
        if (string.IsNullOrWhiteSpace(_activeConcentrationSpellId))
        {
            return "Concentration: none";
        }

        return $"Concentration: {_activeConcentrationLabel} ({Math.Max(0, _activeConcentrationRemainingRounds)}r)";
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
        SyncFollowingCombatHazardsToPlayer();
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

        if (_player.HasSkill("mana_shield") && !_arcaneWardUsedThisCombat)
        {
            skills.Add("mana_shield");
        }

        if (_player.HasSkill("channel_divinity") && !_channelDivinityUsedThisCombat)
        {
            skills.Add("channel_divinity");
        }

        if (_player.HasSkill("cutting_words") && !_cuttingWordsUsedThisCombat)
        {
            skills.Add("cutting_words");
        }

        if (_player.HasSkill("lay_on_hands") && !_layOnHandsUsedThisCombat)
        {
            skills.Add("lay_on_hands");
        }

        // Phase B active feat actions
        if (_player.HasFeat("warrior_battle_cry_feat") && !_battleCryUsedThisCombat)
            skills.Add("warrior_battle_cry_feat");
        if (_player.HasFeat("rogue_vanish_feat") && !_vanishUsedThisCombat)
            skills.Add("rogue_vanish_feat");
        if (_player.HasFeat("paladin_divine_smite_feat") && !_divineSmiteUsedThisCombat && _player.GetSpellSlots(1) > 0)
            skills.Add("paladin_divine_smite_feat");
        if (_player.HasFeat("paladin_divine_favor_feat") && !_divineFavorUsedThisCombat)
            skills.Add("paladin_divine_favor_feat");
        if (_player.HasFeat("mage_empower_spell_feat") && !_empowerSpellUsedThisCombat)
            skills.Add("mage_empower_spell_feat");
        if (_player.HasFeat("cleric_word_of_renewal_feat") && !_wordOfRenewalUsedThisCombat && _player.GetSpellSlots(1) < _player.GetSpellSlotsMax(1))
            skills.Add("cleric_word_of_renewal_feat");

        // D&D active feats (prime-based once/combat)
        if (_player.HasFeat("lucky_feat") && !_luckyUsedThisCombat)
            skills.Add("lucky_feat");
        if (_player.HasFeat("mage_metamagic_feat") && !_metamagicUsedThisCombat)
            skills.Add("mage_metamagic_feat");
        if (_player.HasFeat("ranger_sharpshooter_feat") && !_sharpshooterUsedThisCombat)
            skills.Add("ranger_sharpshooter_feat");

        // Phase C active feats (prime-based once/combat)
        if (_player.HasFeat("barbarian_reckless_attack_feat") && !_recklessAttackUsedThisCombat)
            skills.Add("barbarian_reckless_attack_feat");
        if (_player.HasFeat("mage_overchannel_feat") && !_overchannelUsedThisCombat)
            skills.Add("mage_overchannel_feat");
        if (_player.HasFeat("cleric_spiritual_weapon_feat") && !_spiritualWeaponUsedThisCombat)
            skills.Add("cleric_spiritual_weapon_feat");
        if (_player.HasFeat("bard_bardic_inspiration_feat") && !_bardicInspirationUsedThisCombat)
            skills.Add("bard_bardic_inspiration_feat");

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
        _combatSkillMenuOffset = 0;
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

        if (PressedOrRepeat(KeyUp))
        {
            _selectedCombatSkillIndex = (_selectedCombatSkillIndex - 1 + skills.Count) % skills.Count;
            EnsureCombatSkillSelectionVisible(skills.Count);
            return;
        }

        if (PressedOrRepeat(KeyDown))
        {
            _selectedCombatSkillIndex = (_selectedCombatSkillIndex + 1) % skills.Count;
            EnsureCombatSkillSelectionVisible(skills.Count);
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
                DoArcaneWard();
                return;
            case "channel_divinity":
                DoChannelDivinity();
                return;
            case "cutting_words":
                DoCuttingWords();
                return;
            case "lay_on_hands":
                DoLayOnHands();
                return;
            case "warrior_battle_cry_feat":
                DoBattleCry();
                return;
            case "rogue_vanish_feat":
                DoVanish();
                return;
            case "paladin_divine_smite_feat":
                DoDivineSmite();
                return;
            case "paladin_divine_favor_feat":
                DoDivineFavor();
                return;
            case "mage_empower_spell_feat":
                DoEmpowerSpell();
                return;
            case "cleric_word_of_renewal_feat":
                DoWordOfRenewal();
                return;
            case "lucky_feat":
                DoLucky();
                return;
            case "mage_metamagic_feat":
                DoMetamagic();
                return;
            case "ranger_sharpshooter_feat":
                DoSharpshooter();
                return;
            case "barbarian_reckless_attack_feat":
                DoRecklessAttack();
                return;
            case "mage_overchannel_feat":
                DoOverchannel();
                return;
            case "cleric_spiritual_weapon_feat":
                DoSpiritualWeapon();
                return;
            case "bard_bardic_inspiration_feat":
                DoBardicInspiration();
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
        if (_activeTransformation != null)
        {
            RevertTransformation("You dismiss your form, returning to normal.");
            EndActiveConcentration("Transformation dismissed.");
            return;
        }
        var knownSpells = _player.GetKnownSpells();
        if (knownSpells.Count == 0) return;
        ClearPendingCombatSpell();
        _selectedSpellIndex = Math.Clamp(_selectedSpellIndex, 0, knownSpells.Count - 1);
        EnsureSpellSelectionVisible(knownSpells.Count);
        _gameState = GameState.CombatSpellMenu;
    }

    private void OpenCombatItemMenu()
    {
        if (_player == null) return;
        var items = GetCombatConsumables();
        if (items.Count == 0) return;
        ClearPendingCombatSpell();
        _selectedCombatItemIndex = Math.Clamp(_selectedCombatItemIndex, 0, items.Count - 1);
        EnsureCombatItemSelectionVisible(items.Count);
        _gameState = GameState.CombatItemMenu;
    }

    private void ClearPendingCombatSpell()
    {
        _pendingCombatSpellId = string.Empty;
        _pendingCombatSpellVariantIndex = 0;
        _combatSpellTargetCursorX = -1;
        _combatSpellTargetCursorY = -1;
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

        if (PressedOrRepeat(KeyUp))
        {
            _selectedSpellIndex = (_selectedSpellIndex - 1 + spells.Count) % spells.Count;
            EnsureSpellSelectionVisible(spells.Count);
            return;
        }

        if (PressedOrRepeat(KeyDown))
        {
            _selectedSpellIndex = (_selectedSpellIndex + 1) % spells.Count;
            EnsureSpellSelectionVisible(spells.Count);
            return;
        }

        if (!Pressed(KeyEnter)) return;

        var chosenSpell = spells[Math.Min(_selectedSpellIndex, spells.Count - 1)];
        _pendingCombatSpellId = chosenSpell.Id;
        _pendingCombatSpellVariantIndex = 0;
        ResetPendingCombatSpellVariant(chosenSpell);
        PrimeCombatSpellTargeting(chosenSpell);
        _gameState = GameState.CombatSpellTargeting;
    }

    private void HandleFormSelectionInput()
    {
        if (Pressed(KeyUp)) _formSelectionIndex = Math.Max(0, _formSelectionIndex - 1);
        if (Pressed(KeyDown)) _formSelectionIndex = Math.Min(_pendingFormOptions.Length - 1, _formSelectionIndex + 1);

        if (Pressed(KeyEnter))
        {
            var formId = _pendingFormOptions[_formSelectionIndex];
            var spell = SpellData.ById[_pendingFormSpellId];
            var route = SpellData.ResolveEffectRoute(spell);
            ActivateTransformation(spell, route, formId);
        }

        if (Pressed(KeyEscape))
        {
            _gameState = GameState.CombatSpellMenu;
        }
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

        if (SpellSupportsVariantSelection(pendingSpell))
        {
            if (PressedOrRepeat(KeyUp))
            {
                CyclePendingCombatSpellVariant(pendingSpell, -1);
                return;
            }

            if (PressedOrRepeat(KeyDown))
            {
                CyclePendingCombatSpellVariant(pendingSpell, 1);
                return;
            }
        }

        if (UsesFreeTileSpellTargeting(pendingSpell))
        {
            if (TryGetMoveDelta(out var moveX, out var moveY))
            {
                TryMoveCombatSpellTargetCursor(moveX, moveY);
                return;
            }
        }
        else if (PressedOrRepeat(KeyLeft))
        {
            CycleEncounterTarget(-1);
            return;
        }

        if (!UsesFreeTileSpellTargeting(pendingSpell) && PressedOrRepeat(KeyRight))
        {
            CycleEncounterTarget(1);
            return;
        }

        if (!Pressed(KeyEnter)) return;

        var spellValidation = ValidateCombatSpellAim(pendingSpell);
        if (!spellValidation.IsLegal)
        {
            PushCombatLog($"{pendingSpell.Name} blocked: {spellValidation.BlockedReason}");
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

        if (PressedOrRepeat(KeyUp))
        {
            _selectedCombatItemIndex = (_selectedCombatItemIndex - 1 + items.Count) % items.Count;
            EnsureCombatItemSelectionVisible(items.Count);
            return;
        }

        if (PressedOrRepeat(KeyDown))
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
            case "healing_draught":
            {
                if (_player.CurrentHp >= _player.MaxHp)
                {
                    resultMessage = "HP is already full.";
                    return false;
                }

                var restoreAmount = Math.Max(4, (int)Math.Ceiling(_player.MaxHp * 0.35));
                var before = _player.CurrentHp;
                _player.CurrentHp = Math.Min(_player.MaxHp, _player.CurrentHp + restoreAmount);
                var gained = _player.CurrentHp - before;
                if (gained <= 0)
                {
                    resultMessage = "Healing Draught had no effect.";
                    return false;
                }

                item.Quantity -= 1;
                resultMessage = $"Used {item.Name}: HP +{gained} ({item.Quantity} left).";
                turnConsumed = true;
                return true;
            }
            case "sharpening_oil":
                item.Quantity -= 1;
                _runMeleeBonus += 1;
                resultMessage = "Sharpening Oil applied: +1 run melee damage.";
                turnConsumed = true;
                return true;
            case "antidote_vial":
            {
                var poisoned = _playerConditions.FirstOrDefault(c => c.Kind == PlayerConditionKind.Poisoned);
                if (poisoned == null)
                {
                    resultMessage = "You are not poisoned.";
                    return false;
                }
                _playerConditions.Remove(poisoned);
                item.Quantity -= 1;
                resultMessage = $"Used {item.Name}: Poison cured! ({item.Quantity} left).";
                turnConsumed = true;
                return true;
            }
            case "smoke_bomb":
                item.Quantity -= 1;
                _runFleeBonus += 15;
                resultMessage = $"Used {item.Name}: +15% flee chance for this run. ({item.Quantity} left).";
                turnConsumed = true;
                return true;
            default:
                resultMessage = $"{item.Name} has no combat effect configured.";
                return false;
        }
    }

    private void StartSpellConcentration(SpellDefinition spell, SpellEffectRouteSpec route)
    {
        if (!route.RequiresConcentration)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(_activeConcentrationSpellId))
        {
            EndActiveConcentration($"Concentration on {_activeConcentrationLabel} ends.");
        }

        var initialRounds = route.HazardSpec?.DurationRounds ?? Math.Max(2, spell.SpellLevel + 1);
        _activeConcentrationSpellId = spell.Id;
        _activeConcentrationLabel = spell.Name;
        _activeConcentrationRemainingRounds = initialRounds;
        PushCombatLog($"You begin concentrating on {spell.Name} ({initialRounds} round(s)).");
    }

    private void EndActiveConcentration(string reason, bool logMessage = true)
    {
        if (string.IsNullOrWhiteSpace(_activeConcentrationSpellId))
        {
            return;
        }

        var spellId = _activeConcentrationSpellId;
        var spellLabel = _activeConcentrationLabel;
        _activeConcentrationSpellId = string.Empty;
        _activeConcentrationLabel = string.Empty;
        _activeConcentrationRemainingRounds = 0;

        // Clear weapon rider / self-buff flags tied to this concentration
        switch (spellId)
        {
            case "paladin_magic_weapon":
                _magicWeaponActive = false;
                _runMeleeBonus = Math.Max(0, _runMeleeBonus - 1);
                break;
            case "ranger_flame_arrows":
                _flameArrowsActive = false;
                break;
            case "paladin_crusaders_mantle":
                _crusadersMantleActive = false;
                _runDefenseBonus = Math.Max(0, _runDefenseBonus - 1);
                break;
            case "ranger_zephyr_strike":
                _zephyrStrikeActive = false;
                _zephyrStrikeHitPrimed = false;
                _runFleeBonus = Math.Max(0, _runFleeBonus - 10);
                break;
            case "paladin_divine_favor":
                _divineFavorActive = false;
                break;
            case "cleric_shield_of_faith":
            case "paladin_shield_of_faith":
                _shieldOfFaithActive = false;
                _runDefenseBonus = Math.Max(0, _runDefenseBonus - 2);
                break;
            case "cleric_bless":
                _blessActive = false;
                break;
            case "paladin_heroism":
            case "bard_heroism":
                _heroismActive = false;
                _playerTempHp = 0;
                break;
            case "ranger_barkskin":
                _barkskinActive = false;
                break;
            case "mage_blur":
                _blurActive = false;
                break;
            case "mage_haste":
                _hasteActive = false;
                _runDefenseBonus = Math.Max(0, _runDefenseBonus - 2);
                _combatMovePointsMax = Math.Max(1, _combatMovePointsMax - 2);
                break;
            // Batch 2 — Tactical combat spells
            case "mage_expeditious_retreat":
                _expeditiousRetreatActive = false;
                _runFleeBonus = Math.Max(0, _runFleeBonus - 15);
                break;
            case "ranger_longstrider":
                _longstriderActive = false;
                _combatMovePointsMax = Math.Max(1, _combatMovePointsMax - 2);
                break;
            case "bard_hex":
                _hexActive = false;
                break;
            case "cleric_protection_evg":
            case "paladin_protection_evg":
                _protFromEvilActive = false;
                _runDefenseBonus = Math.Max(0, _runDefenseBonus - 1);
                break;
            case "cleric_sanctuary":
                _sanctuaryActive = false;
                break;
            case "paladin_compelled_duel":
                _compelledDuelActive = false;
                _runMeleeBonus = Math.Max(0, _runMeleeBonus - 2);
                break;
            case "bard_enhance_ability":
            case "cleric_enhance_ability":
            case "mage_enhance_ability":
                _enhanceAbilityActive = false;
                _runDefenseBonus = Math.Max(0, _runDefenseBonus - 2);
                _runFleeBonus = Math.Max(0, _runFleeBonus - 3);
                break;
            // Batch 3 concentration cleanup
            case "cleric_spirit_shroud":
                _spiritShroudActive = false;
                break;
            case "ranger_thorns":
                _thornsActive = false;
                break;
            case "ranger_stoneskin":
            case "mage_stoneskin":
                _stoneskinActive = false;
                break;
            case "bard_greater_invisibility":
                _greaterInvisibilityActive = false;
                break;
            // Batch 4+5 concentration cleanup
            case "bard_invisibility":
                _invisibilityActive = false;
                break;
            case "paladin_elemental_weapon":
                _elementalWeaponActive = false;
                _elementalWeaponElement = string.Empty;
                break;
            case "mage_blink":
                _blinkActive = false;
                break;
            case "mage_protection_from_energy":
                _protEnergyActive = false;
                _protEnergyElement = string.Empty;
                break;
            case "cleric_beacon_of_hope":
                _beaconOfHopeActive = false;
                break;
            case "bard_major_image":
                _majorImageActive = false;
                break;
            case "paladin_aura_of_courage":
                _auraOfCourageActive = false;
                break;
        }

        // Clean up any summon tied to this concentration
        if (_activeSummon != null && _activeSummon.Type.RequiresConcentration
            && _activeSummon.Type.SourceSpellId == spellId)
        {
            if (_activeSummon.Type.Behavior == SummonBehaviorKind.BuffMount)
            {
                _runDefenseBonus = Math.Max(0, _runDefenseBonus - 2);
                _runFleeBonus = Math.Max(0, _runFleeBonus - 15);
            }
            PushCombatLog($"{_activeSummon.Type.Name} vanishes.");
            _activeSummon = null;
        }

        // Clean up transformation tied to this concentration
        if (_activeTransformation != null && _activeTransformation.SourceSpellId == spellId)
        {
            RevertTransformation($"{_activeTransformation.Form.Name} reverts.");
        }

        ClearEffectsFromSpellSource(spellId);
        if (logMessage)
        {
            PushCombatLog(string.IsNullOrWhiteSpace(reason)
                ? $"{spellLabel} concentration ends."
                : reason);
        }
    }

    private void ClearEffectsFromSpellSource(string spellId)
    {
        if (string.IsNullOrWhiteSpace(spellId))
        {
            return;
        }

        _activeCombatHazards.RemoveAll(hazard =>
            string.Equals(hazard.SourceSpellId, spellId, StringComparison.Ordinal));

        foreach (var enemy in _enemies)
        {
            enemy.StatusEffects.RemoveAll(status =>
                string.Equals(status.SourceSpellId, spellId, StringComparison.Ordinal));
        }
    }

    private void ActivateTransformation(SpellDefinition spell, SpellEffectRouteSpec route, string formId, bool milestoneSlotWaive = false)
    {
        if (!SpellData.Forms.TryGetValue(formId, out var form))
        {
            PushCombatLog($"{spell.Name}: form not found.");
            if (!spell.SuppressCounterAttack) DoEnemyAttack();
            return;
        }

        // End existing transformation if any
        if (_activeTransformation != null)
            RevertTransformation("Your previous form dissolves.");

        _activeTransformation = new TransformationInstance
        {
            Form = form,
            SourceSpellId = spell.Id,
            TempHpRemaining = form.TempHp,
            FirstHitPrimed = form.Special == FormSpecialKind.FirstHitBonus
        };

        // Apply passive bonuses
        if (form.Special == FormSpecialKind.DefenseBonus)
            _runDefenseBonus += form.SpecialValue;
        if (form.Special == FormSpecialKind.FleeBonus)
            _runFleeBonus += form.SpecialValue;
        if (form.Special == FormSpecialKind.Evasion)
            _runFleeBonus += form.SpecialValue;

        StartSpellConcentration(spell, route);
        PushCombatLog($"{spell.Name}: You transform into a {form.Name}! ({form.TempHp} temp HP)");
        PushCombatLog($"  AC {form.FormAC} | ATK +{form.AttackBonus} | {form.DamageCount}d{form.DamageDice}+{form.DamageBonus} {form.DamageType}");
        if (!form.CanAttack) PushCombatLog("  You cannot attack in this form.");

        if (spell.RequiresSlot && !milestoneSlotWaive)
            PushCombatLog($"L{spell.SpellLevel} slots {_player!.GetSpellSlots(spell.SpellLevel)}/{_player.GetSpellSlotsMax(spell.SpellLevel)}.");

        _gameState = GameState.Combat;
        if (!spell.SuppressCounterAttack) DoEnemyAttack();
    }

    private void RevertTransformation(string reason)
    {
        if (_activeTransformation == null) return;

        var form = _activeTransformation.Form;

        // Revert passive bonuses
        if (form.Special == FormSpecialKind.DefenseBonus)
            _runDefenseBonus = Math.Max(0, _runDefenseBonus - form.SpecialValue);
        if (form.Special == FormSpecialKind.FleeBonus)
            _runFleeBonus = Math.Max(0, _runFleeBonus - form.SpecialValue);
        if (form.Special == FormSpecialKind.Evasion)
            _runFleeBonus = Math.Max(0, _runFleeBonus - form.SpecialValue);

        PushCombatLog(reason);
        _activeTransformation = null;
    }

    private void ApplyPlayerCondition(PlayerConditionKind kind, int potency, int duration)
    {
        // Aura of Courage: reduce incoming condition duration by 1 (min 1)
        if (_auraOfCourageActive)
            duration = Math.Max(1, duration - 1);

        var existing = _playerConditions.FirstOrDefault(c => c.Kind == kind);
        if (existing != null)
        {
            existing.Potency = Math.Max(existing.Potency, potency);
            existing.RemainingTurns = Math.Max(existing.RemainingTurns, duration);
        }
        else
        {
            _playerConditions.Add(new PlayerConditionState { Kind = kind, Potency = potency, RemainingTurns = duration });
        }
    }

    private void AdvancePlayerConditions()
    {
        for (var i = _playerConditions.Count - 1; i >= 0; i--)
        {
            var cond = _playerConditions[i];
            if (cond.Kind == PlayerConditionKind.Poisoned && _player != null)
            {
                _player.CurrentHp = Math.Max(0, _player.CurrentHp - cond.Potency);
                PushCombatLog($"Poison deals {cond.Potency} damage. HP {_player.CurrentHp}/{_player.MaxHp}.");
                TryResolveConcentrationAfterDamage(cond.Potency, "Poison");
            }
            else if (cond.Kind == PlayerConditionKind.Weakened)
            {
                if (cond.RemainingTurns == cond.RemainingTurns) // always log on first call — fires each tick
                {
                    PushCombatLog("You feel weakened. (−2 to attack rolls)");
                }
            }
            cond.RemainingTurns--;
            if (cond.RemainingTurns <= 0)
            {
                PushCombatLog($"{cond.Kind} fades.");
                _playerConditions.RemoveAt(i);
            }
        }
    }

    private int GetPlayerConditionAttackPenalty()
    {
        return -2 * _playerConditions.Count(c => c.Kind == PlayerConditionKind.Weakened);
    }

    private void AdvanceConcentrationRound()
    {
        if (string.IsNullOrWhiteSpace(_activeConcentrationSpellId))
        {
            return;
        }

        _activeConcentrationRemainingRounds = Math.Max(0, _activeConcentrationRemainingRounds - 1);
        if (_activeConcentrationRemainingRounds > 0)
        {
            return;
        }

        EndActiveConcentration($"{_activeConcentrationLabel} fades as the fight drags on.");
    }

    private void TryResolveConcentrationAfterDamage(int damage, string source)
    {
        if (_player == null || damage <= 0 || string.IsNullOrWhiteSpace(_activeConcentrationSpellId))
        {
            return;
        }

        var dc = Math.Max(10, damage / 2);
        var conMod = _player.Mod(StatName.Constitution);
        var blessSaveBonus = _blessActive ? _rng.Next(1, 5) : 0;
        var rawRoll1 = _rng.Next(1, 21);
        int roll;
        if (_player.HasFeat("war_caster_feat"))
        {
            var rawRoll2 = _rng.Next(1, 21);
            var betterRaw = Math.Max(rawRoll1, rawRoll2);
            roll = betterRaw + conMod + blessSaveBonus;
            PushCombatLog($"War Caster — concentration rolls {rawRoll1 + conMod + blessSaveBonus} and {rawRoll2 + conMod + blessSaveBonus}, keeps higher ({roll}).");
        }
        else
        {
            roll = rawRoll1 + conMod + blessSaveBonus;
        }
        if (blessSaveBonus > 0)
            PushCombatLog($"Bless — +{blessSaveBonus} to concentration save.");
        if (roll >= dc)
        {
            PushCombatLog($"Concentration check {roll} vs DC {dc}: {source} does not break {_activeConcentrationLabel}.");
            return;
        }

        EndActiveConcentration($"Concentration broken by {source} ({roll} vs DC {dc}).");
    }

    private CombatHazardState CreateCombatHazardState(SpellDefinition spell, SpellEffectRouteSpec route, int centerX, int centerY)
    {
        var hazardSpec = route.HazardSpec!;
        var state = new CombatHazardState
        {
            InstanceId = $"hazard_{Guid.NewGuid():N}",
            SourceSpellId = spell.Id,
            SourceLabel = spell.Name,
            Element = route.Element,
            BaseDamage = spell.BaseDamage,
            Variance = spell.Variance,
            ArmorBypass = spell.ArmorBypass + (_player?.SpellArmorBypassBonus ?? 0),
            CenterX = centerX,
            CenterY = centerY,
            RadiusTiles = Math.Max(0, hazardSpec.RadiusTiles),
            RemainingRounds = Math.Max(1, hazardSpec.DurationRounds),
            FollowsPlayer = hazardSpec.FollowsPlayer,
            RequiresConcentration = route.RequiresConcentration || hazardSpec.RequiresConcentration,
            TriggersOnTurnStart = hazardSpec.TriggersOnTurnStart,
            TriggersOnEntry = hazardSpec.TriggersOnEntry,
            InitialSaveStat = hazardSpec.InitialSaveStat,
            SaveDamageBehavior = hazardSpec.SaveDamageBehavior
        };

        foreach (var status in hazardSpec.OnTriggerStatuses)
        {
            state.OnTriggerStatuses.Add(new CombatStatusApplySpec
            {
                Kind = status.Kind,
                Potency = status.Potency,
                DurationTurns = status.DurationTurns,
                ChancePercent = status.ChancePercent,
                InitialSaveStat = status.InitialSaveStat,
                RepeatSaveStat = status.RepeatSaveStat,
                BreaksOnDamageTaken = status.BreaksOnDamageTaken
            });
        }

        return state;
    }

    private void PlaceCombatHazard(SpellDefinition spell, SpellEffectRouteSpec route)
    {
        if (_player == null || route.HazardSpec == null)
        {
            return;
        }

        var (centerX, centerY) = ResolveSpellAnchorTile(spell);
        var hazard = CreateCombatHazardState(spell, route, centerX, centerY);
        _activeCombatHazards.Add(hazard);
        PushCombatLog($"{spell.Name} creates a hazard at {centerX},{centerY} for {hazard.RemainingRounds} round(s).");
    }

    private void SyncFollowingCombatHazardsToPlayer()
    {
        if (_player == null)
        {
            return;
        }

        foreach (var hazard in _activeCombatHazards.Where(hazard => hazard.FollowsPlayer))
        {
            hazard.CenterX = _player.X;
            hazard.CenterY = _player.Y;
        }
    }

    private void AdvanceCombatHazardDurations()
    {
        for (var i = _activeCombatHazards.Count - 1; i >= 0; i--)
        {
            var hazard = _activeCombatHazards[i];
            hazard.RemainingRounds -= 1;
            if (hazard.RemainingRounds > 0)
            {
                continue;
            }

            PushCombatLog($"{hazard.SourceLabel} dissipates.");
            _activeCombatHazards.RemoveAt(i);
        }
    }

    private bool TryResolveCombatHazardDamageSaveOutcome(
        CombatHazardState hazard,
        Enemy enemy,
        StatName scalingStat,
        out string message,
        out int damageNumerator,
        out int damageDenominator,
        out bool skipStatuses)
    {
        message = string.Empty;
        damageNumerator = 1;
        damageDenominator = 1;
        skipStatuses = false;
        if (!hazard.InitialSaveStat.HasValue || hazard.SaveDamageBehavior == SpellSaveDamageBehavior.None)
        {
            return false;
        }

        var saveStat = hazard.InitialSaveStat.Value;
        var saved = TryRollEnemySave(enemy, saveStat, scalingStat, out var rollTotal, out _, out var dc);
        var saveLabel = GetStatShortLabel(saveStat);
        if (!saved)
        {
            message = $"{enemy.Type.Name} fails the {saveLabel} save against {hazard.SourceLabel} ({rollTotal} vs DC {dc}).";
            return true;
        }

        skipStatuses = true;
        switch (hazard.SaveDamageBehavior)
        {
            case SpellSaveDamageBehavior.NegateOnSave:
                damageNumerator = 0;
                message = $"{enemy.Type.Name} resists {hazard.SourceLabel} ({saveLabel} save {rollTotal} vs DC {dc}) and takes no damage.";
                break;
            case SpellSaveDamageBehavior.HalfOnSave:
                damageDenominator = 2;
                message = $"{enemy.Type.Name} partially resists {hazard.SourceLabel} ({saveLabel} save {rollTotal} vs DC {dc}) and takes half damage.";
                break;
            default:
                message = $"{enemy.Type.Name} resists {hazard.SourceLabel} ({saveLabel} save {rollTotal} vs DC {dc}).";
                break;
        }

        return true;
    }

    private int ApplyCombatHazardDamageToEnemy(CombatHazardState hazard, Enemy enemy, out bool skipStatuses, out string saveMessage)
    {
        skipStatuses = false;
        saveMessage = string.Empty;
        if (_player == null)
        {
            return 0;
        }

        var scalingStat = SpellData.ById.TryGetValue(hazard.SourceSpellId, out var sourceSpell)
            ? sourceSpell.ScalingStat
            : StatName.Wisdom;
        var damageNumerator = 1;
        var damageDenominator = 1;
        if (TryResolveCombatHazardDamageSaveOutcome(hazard, enemy, scalingStat, out saveMessage, out damageNumerator, out damageDenominator, out skipStatuses))
        {
        }
        var (damage, _, _, _) = CalcSpellDamageAgainstEnemy(
            enemy,
            scalingStat,
            hazard.BaseDamage + _player.SpellDamageBonus + GetClassSpellDamageBonus(_player) + _runSpellBonus + GetConditionSpellModifier(),
            hazard.Variance,
            hazard.ArmorBypass,
            spellLevel: 1);
        damage += GetEnemyIncomingDamageBonus(enemy);
        if (damageNumerator == 0)
        {
            damage = 0;
        }
        else if (damageDenominator > 1)
        {
            damage = damage * damageNumerator / damageDenominator;
        }
        enemy.CurrentHp = Math.Max(0, enemy.CurrentHp - damage);
        if (damage > 0)
        {
            TryBreakDamageSensitiveStatuses(enemy, hazard.SourceLabel);
        }
        return damage;
    }

    private bool IsEnemyInsideHazard(CombatHazardState hazard, Enemy enemy)
    {
        return EncounterTargetingRules.GetTileDistance(hazard.CenterX, hazard.CenterY, enemy.X, enemy.Y) <= hazard.RadiusTiles;
    }

    private void ApplyCombatHazardStatuses(CombatHazardState hazard, Enemy enemy)
    {
        foreach (var status in hazard.OnTriggerStatuses)
        {
            var scalingStat = SpellData.ById.TryGetValue(hazard.SourceSpellId, out var sourceSpell)
                ? sourceSpell.ScalingStat
                : (StatName?)null;
            TryApplyOrRefreshEnemyStatus(enemy, status, hazard.SourceSpellId, hazard.SourceLabel, scalingStat, out var message);
            if (!string.IsNullOrWhiteSpace(message))
            {
                PushCombatLog(message);
            }
        }
    }

    private bool ResolveEnemyStartOfTurnHazards(Enemy enemy)
    {
        foreach (var hazard in _activeCombatHazards.Where(hazard => hazard.TriggersOnTurnStart).ToList())
        {
            if (!IsEnemyInsideHazard(hazard, enemy))
            {
                continue;
            }

            var damage = ApplyCombatHazardDamageToEnemy(hazard, enemy, out var skipStatuses, out var saveMessage);
            if (!string.IsNullOrWhiteSpace(saveMessage))
            {
                PushCombatLog(saveMessage);
            }
            if (damage > 0)
            {
                PushCombatLog($"{enemy.Type.Name} suffers {damage} from {hazard.SourceLabel}.");
                PushCombatLog($"{enemy.Type.Name} HP {enemy.CurrentHp}/{enemy.Type.MaxHp}.");
            }
            if (!skipStatuses)
            {
                ApplyCombatHazardStatuses(hazard, enemy);
            }
            if (!enemy.IsAlive)
            {
                return false;
            }
        }

        return enemy.IsAlive;
    }

    private void ResolveEnemyEntryHazards(Enemy enemy)
    {
        foreach (var hazard in _activeCombatHazards.Where(hazard => hazard.TriggersOnEntry).ToList())
        {
            if (!IsEnemyInsideHazard(hazard, enemy))
            {
                continue;
            }

            var damage = ApplyCombatHazardDamageToEnemy(hazard, enemy, out var skipStatuses, out var saveMessage);
            if (!string.IsNullOrWhiteSpace(saveMessage))
            {
                PushCombatLog(saveMessage);
            }
            if (damage > 0)
            {
                PushCombatLog($"{enemy.Type.Name} crosses {hazard.SourceLabel} for {damage} damage.");
                PushCombatLog($"{enemy.Type.Name} HP {enemy.CurrentHp}/{enemy.Type.MaxHp}.");
            }
            if (!skipStatuses)
            {
                ApplyCombatHazardStatuses(hazard, enemy);
            }
            if (!enemy.IsAlive)
            {
                return;
            }
        }
    }

    private bool ResolveEncounterEnemyDeathsImmediate()
    {
        if (_player == null)
        {
            return false;
        }

        var defeatedEnemies = _encounterEnemies
            .Where(enemy => !enemy.IsAlive)
            .Distinct()
            .ToList();
        if (defeatedEnemies.Count == 0)
        {
            return false;
        }

        var levelUpTriggered = false;
        foreach (var defeatedEnemy in defeatedEnemies)
        {
            PushCombatLog($"{defeatedEnemy.Type.Name} collapses immediately.");
            _phase3EnemiesDefeated += 1;
            var baseXp = defeatedEnemy.Type.XpReward;
            var xp = Math.Max(1, (int)Math.Round(baseXp * (100 + _phase3XpPercentMod) / 100.0, MidpointRounding.AwayFromZero));
            PushCombatLog($"You gain {xp} XP.");
            levelUpTriggered |= _player.GainXp(xp);
            ApplyMilestoneExecutionRewardOnEnemyDefeat();
            if (string.Equals(ResolveEnemyTypeKey(defeatedEnemy.Type), "goblin_general", StringComparison.Ordinal))
            {
                _bossDefeated = true;
                PushCombatLog("The Goblin General is down. The sanctum trembles.");
            }

            SpawnGuaranteedLootDrop(defeatedEnemy);
            _enemies = _enemies.Where(enemy => !ReferenceEquals(enemy, defeatedEnemy)).ToList();
            _enemyAi.Remove(defeatedEnemy);
            _enemyLootKits.Remove(defeatedEnemy);
            _encounterEnemies.RemoveAll(enemy => ReferenceEquals(enemy, defeatedEnemy));
            if (ReferenceEquals(_currentEnemy, defeatedEnemy))
            {
                _currentEnemy = null;
            }
        }

        PruneEncounterTurnOrder();
        SyncEncounterTargetSelection(preferCurrentEnemy: true);
        _packEnemiesRemainingAfterCurrent = Math.Max(0, _encounterEnemies.Count(enemy =>
            enemy.IsAlive &&
            !ReferenceEquals(enemy, _currentEnemy)));

        if (levelUpTriggered)
        {
            ResetEncounterContext();
            _selectionMessage = string.Empty;
            BeginLevelUpFlow();
            return true;
        }

        if (_encounterEnemies.All(enemy => !enemy.IsAlive))
        {
            if (_bossDefeated)
            {
                _floorCleared = true;
                PushCombatLog("All hostiles eliminated. Floor 1 cleared.");
                ResetEncounterContext();
                _gameState = GameState.VictoryScreen;
                TryAutosaveCheckpoint("floor1_cleared");
            }
            else
            {
                ResetEncounterContext();
                EnterPlayingState("combat_victory");
            }

            return true;
        }

        return false;
    }

    private void CastCombatSpell(SpellDefinition spell)
    {
        if (_player == null || _currentEnemy == null || _gameState == GameState.DeathScreen) return;
        var route = SpellData.ResolveEffectRoute(spell);
        if (route.IsFutureGated)
        {
            var requirement = string.IsNullOrWhiteSpace(route.FutureRequirement)
                ? "a later spell-engine pass"
                : route.FutureRequirement;
            PushCombatLog($"{spell.Name} is archived until {requirement} is implemented.");
            return;
        }

        var aimValidation = ValidateCombatSpellAim(spell);
        if (!aimValidation.IsLegal)
        {
            PushCombatLog($"{spell.Name} blocked: {aimValidation.BlockedReason}");
            return;
        }

        if (route.TargetShape != SpellTargetShape.SingleEnemy && route.HazardSpec == null)
        {
            var previewTargets = ResolveSpellAffectedEnemies(spell);
            if (previewTargets.Count == 0)
            {
                PushCombatLog($"{spell.Name} blocked: no enemies are in the affected area.");
                return;
            }
        }

        if (IsEmpoweredMeleeSpell(spell))
        {
            var meleeValidation = ValidateCurrentEnemyTargetForMelee();
            if (!meleeValidation.IsLegal)
            {
                PushCombatLog($"{spell.Name} requires a melee target: {meleeValidation.BuildBlockedReason()}");
                return;
            }
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

        var tierLabel = spell.IsCantrip ? "Cantrip" : $"L{spell.SpellLevel}";

        // Healing spells bypass the enemy-damage pipeline entirely.
        if (spell.IsHealSpell)
        {
            if (_player.CurrentHp >= _player.MaxHp)
            {
                PushCombatLog($"{spell.Name}: you are already at full health. Slot wasted.");
                if (spell.RequiresSlot && !milestoneSlotWaive)
                    PushCombatLog($"L{spell.SpellLevel} slots {_player.GetSpellSlots(spell.SpellLevel)}/{_player.GetSpellSlotsMax(spell.SpellLevel)}.");
                if (!spell.SuppressCounterAttack) DoEnemyAttack();
                return;
            }

            var statMod = _player.Mod(spell.ScalingStat);
            var blessedHealerBonus = _player.HasSkill("blessed_healer") ? Math.Max(0, _player.Mod(StatName.Wisdom)) : 0;
            var healBase = spell.BaseDamage + _rng.Next(spell.Variance + 1) + statMod + _player.HealingBonus + blessedHealerBonus;
            var healAmount = Math.Max(1, healBase);
            // Beacon of Hope: double the next heal and consume the bonus
            if (_beaconOfHopeActive)
            {
                healAmount *= 2;
                _beaconOfHopeActive = false;
                PushCombatLog("Beacon of Hope amplifies the healing!");
            }
            var before = _player.CurrentHp;
            _player.CurrentHp = Math.Min(_player.MaxHp, _player.CurrentHp + healAmount);
            var restored = _player.CurrentHp - before;
            PushCombatLog($"{spell.Name} ({tierLabel}) restores {restored} HP.");
            PushCombatLog($"HP {before} -> {_player.CurrentHp}/{_player.MaxHp}.");
            // Mass Healing Word: also cleanse one condition
            if (string.Equals(spell.Id, "cleric_mass_healing_word", StringComparison.Ordinal) && _playerConditions.Count > 0)
            {
                var worst = _playerConditions.OrderByDescending(c => c.Potency).First();
                _playerConditions.Remove(worst);
                PushCombatLog($"Mass Healing Word also cleanses: {worst.Kind} removed.");
            }
            if (spell.RequiresSlot && !milestoneSlotWaive)
                PushCombatLog($"L{spell.SpellLevel} slots {_player.GetSpellSlots(spell.SpellLevel)}/{_player.GetSpellSlotsMax(spell.SpellLevel)}.");
            if (!spell.SuppressCounterAttack) DoEnemyAttack();
            return;
        }

        // Concentration aura spells: start concentration, no immediate damage.
        if (route.RouteKind == SpellEffectRouteKind.ConcentrationAura)
        {
            StartSpellConcentration(spell, route);
            string auraMsg;
            switch (spell.Id)
            {
                case "paladin_heroism":
                case "bard_heroism":
                    _heroismActive = true;
                    var heroismPulse = Math.Max(1, _player.Mod(StatName.Charisma));
                    _playerTempHp += heroismPulse;
                    auraMsg = $"bravery fills you (+{heroismPulse} temp HP).";
                    break;
                default:
                    auraMsg = "a healing aura surrounds you.";
                    break;
            }
            PushCombatLog($"{spell.Name} ({tierLabel}): {auraMsg}");
            if (spell.RequiresSlot && !milestoneSlotWaive)
                PushCombatLog($"L{spell.SpellLevel} slots {_player.GetSpellSlots(spell.SpellLevel)}/{_player.GetSpellSlotsMax(spell.SpellLevel)}.");
            if (!spell.SuppressCounterAttack) DoEnemyAttack();
            return;
        }

        // Weapon rider spells: concentration-based per-hit damage buff.
        if (route.RouteKind == SpellEffectRouteKind.WeaponRider)
        {
            StartSpellConcentration(spell, route);

            switch (spell.Id)
            {
                case "paladin_magic_weapon":
                    _magicWeaponActive = true;
                    _runMeleeBonus += 1;
                    break;
                case "ranger_flame_arrows":
                    _flameArrowsActive = true;
                    break;
                case "paladin_crusaders_mantle":
                    _crusadersMantleActive = true;
                    _runDefenseBonus += 1;
                    break;
                case "paladin_divine_favor":
                    _divineFavorActive = true;
                    break;
                case "bard_hex":
                    _hexActive = true;
                    break;
                case "paladin_elemental_weapon":
                    _elementalWeaponActive = true;
                    _elementalWeaponElement = GetSelectedPendingSpellVariantId(spell);
                    if (string.IsNullOrWhiteSpace(_elementalWeaponElement)) _elementalWeaponElement = "fire";
                    break;
            }

            PushCombatLog($"{spell.Name} ({tierLabel}): your weapon glows with power.");
            if (spell.RequiresSlot && !milestoneSlotWaive)
                PushCombatLog($"L{spell.SpellLevel} slots {_player.GetSpellSlots(spell.SpellLevel)}/{_player.GetSpellSlotsMax(spell.SpellLevel)}.");
            if (!spell.SuppressCounterAttack) DoEnemyAttack();
            return;
        }

        // Self-buff spells: stat/movement buff (some concentration, some persistent).
        if (route.RouteKind == SpellEffectRouteKind.SelfBuff)
        {
            string selfBuffMsg;
            switch (spell.Id)
            {
                case "ranger_zephyr_strike":
                    StartSpellConcentration(spell, route);
                    _zephyrStrikeActive = true;
                    _zephyrStrikeHitPrimed = true;
                    _runFleeBonus += 10;
                    selfBuffMsg = "wind surges around you.";
                    break;
                case "cleric_shield_of_faith":
                case "paladin_shield_of_faith":
                    StartSpellConcentration(spell, route);
                    _shieldOfFaithActive = true;
                    _runDefenseBonus += 2;
                    selfBuffMsg = "a shimmering field of divine energy grants +2 AC.";
                    break;
                case "cleric_bless":
                    StartSpellConcentration(spell, route);
                    _blessActive = true;
                    selfBuffMsg = "divine favor guides your strikes and saves.";
                    break;
                case "mage_mage_armor":
                {
                    var armorCat = GetCurrentArmorCategory();
                    if (armorCat != ArmorCategory.Unarmored)
                    {
                        PushCombatLog("Mage Armor has no effect while wearing armor.");
                        if (spell.RequiresSlot) _player.RestoreSpellSlot(spell.SpellLevel);
                        if (!spell.SuppressCounterAttack) DoEnemyAttack();
                        return;
                    }
                    _mageArmorActive = true;
                    _runDefenseBonus += 3;
                    selfBuffMsg = "an invisible barrier of force surrounds you (+3 AC).";
                    break;
                }
                case "mage_shield":
                    _shieldSpellActive = true;
                    _shieldSpellTurnsLeft = 1;
                    _runDefenseBonus += 5;
                    selfBuffMsg = "a shimmering shield of force appears (+5 AC, 1 turn).";
                    break;
                case "mage_false_life":
                {
                    var tempHpGain = _rng.Next(1, 5) + 4;
                    _playerTempHp += tempHpGain;
                    selfBuffMsg = $"necromantic energy bolsters you with {tempHpGain} temp HP.";
                    break;
                }
                case "ranger_barkskin":
                    StartSpellConcentration(spell, route);
                    _barkskinActive = true;
                    selfBuffMsg = "your skin hardens like bark (AC floor 16).";
                    break;
                case "cleric_aid":
                case "paladin_aid":
                    _aidMaxHpBonus += 5;
                    _player.AdjustMaxHp(5);
                    selfBuffMsg = $"bolstering energy grants +5 max HP ({_player.CurrentHp}/{_player.MaxHp}).";
                    break;
                case "mage_blur":
                    StartSpellConcentration(spell, route);
                    _blurActive = true;
                    selfBuffMsg = "your form shimmers — enemies attack with disadvantage.";
                    break;
                case "mage_haste":
                    StartSpellConcentration(spell, route);
                    _hasteActive = true;
                    _runDefenseBonus += 2;
                    _combatMovePointsMax += 2;
                    _combatMovePointsRemaining += 2;
                    selfBuffMsg = "you surge with supernatural speed (+2 AC, +2 move).";
                    break;
                // Batch 2 — Tactical combat spells
                case "mage_misty_step":
                    _combatMovePointsRemaining += 3;
                    selfBuffMsg = "you blink through the Weave — +3 move points this turn.";
                    break;
                case "mage_mirror_image":
                    _mirrorImageCharges = 3;
                    selfBuffMsg = "three illusory duplicates shimmer into being.";
                    break;
                case "mage_expeditious_retreat":
                    StartSpellConcentration(spell, route);
                    _expeditiousRetreatActive = true;
                    _runFleeBonus += 15;
                    selfBuffMsg = "arcane speed surges through you (+15% flee, concentration).";
                    break;
                case "ranger_absorb_elements":
                    _absorbElementsCharged = true;
                    selfBuffMsg = "elemental energy coalesces around your weapon (+1d6 on next hit).";
                    break;
                case "ranger_longstrider":
                    StartSpellConcentration(spell, route);
                    _longstriderActive = true;
                    _combatMovePointsMax += 2;
                    _combatMovePointsRemaining += 2;
                    selfBuffMsg = "your stride lengthens (+2 move/turn, concentration).";
                    break;
                case "cleric_protection_evg":
                case "paladin_protection_evg":
                    StartSpellConcentration(spell, route);
                    _protFromEvilActive = true;
                    _runDefenseBonus += 1;
                    selfBuffMsg = "a divine ward shields you (+1 AC, concentration).";
                    break;
                case "cleric_sanctuary":
                    StartSpellConcentration(spell, route);
                    _sanctuaryActive = true;
                    selfBuffMsg = "a divine shield wards you — enemies must pass a Wisdom save to attack you (concentration).";
                    break;
                case "paladin_compelled_duel":
                    StartSpellConcentration(spell, route);
                    _compelledDuelActive = true;
                    _runMeleeBonus += 2;
                    selfBuffMsg = "divine challenge marks your foe (+2 melee, concentration).";
                    break;
                case "bard_enhance_ability":
                case "cleric_enhance_ability":
                case "mage_enhance_ability":
                    StartSpellConcentration(spell, route);
                    _enhanceAbilityActive = true;
                    _runDefenseBonus += 2;
                    _runFleeBonus += 3;
                    selfBuffMsg = "cat-like grace enhances your reflexes (+2 AC, +3% flee, concentration).";
                    break;
                // Batch 3 — Reactive & retaliation spells
                case "mage_hellish_rebuke":
                    _hellishRebukePrimed = true;
                    selfBuffMsg = "hellfire crackles around you — the next attacker will burn (2d6 fire).";
                    break;
                case "mage_armor_of_agathys":
                    _armorOfAgathysTempHp = 8;
                    selfBuffMsg = "frost armor encases you (+8 frost temp HP; attackers take 1d8 cold).";
                    break;
                case "mage_fire_shield":
                    _fireShieldActive = true;
                    selfBuffMsg = "flames wreathe your body — attackers will burn (2d8 fire).";
                    break;
                case "cleric_wrath_of_storm":
                    _wrathOfStormPrimed = true;
                    selfBuffMsg = "lightning crackles around you — the next attacker will be struck (2d8 lightning).";
                    break;
                case "cleric_spirit_shroud":
                    StartSpellConcentration(spell, route);
                    _spiritShroudActive = true;
                    selfBuffMsg = "vengeful spirits swirl around you (+1d8 radiant melee, attackers take 1d6 radiant, concentration).";
                    break;
                case "cleric_death_ward":
                case "paladin_death_ward":
                    _deathWardActive = true;
                    selfBuffMsg = "a golden ward shimmers — you will cheat death once.";
                    break;
                case "paladin_holy_rebuke":
                    _holyRebukePrimed = true;
                    selfBuffMsg = "divine wrath gathers — the next attacker will face judgment (2d6 radiant + heal 1d4).";
                    break;
                case "ranger_thorns":
                    StartSpellConcentration(spell, route);
                    _thornsActive = true;
                    selfBuffMsg = "thorny vines wrap around you — attackers take 1d6 piercing (concentration).";
                    break;
                case "ranger_stoneskin":
                case "mage_stoneskin":
                    StartSpellConcentration(spell, route);
                    _stoneskinActive = true;
                    selfBuffMsg = "your skin hardens to stone — incoming damage reduced by 3 (concentration).";
                    break;
                case "bard_cutting_words":
                    _cuttingWordsPrimed = true;
                    selfBuffMsg = "a cutting remark ready on your lips — next enemy hit reduced by 1d8.";
                    break;
                case "bard_greater_invisibility":
                    StartSpellConcentration(spell, route);
                    _greaterInvisibilityActive = true;
                    selfBuffMsg = "you vanish from sight — advantage on attacks, enemies have disadvantage (concentration).";
                    break;
                // Batch 4 — expanded arsenal
                case "mage_counterspell":
                    _counterspellPrimed = true;
                    selfBuffMsg = "a magical ward coils around you — the next hit against you is halved.";
                    break;
                case "bard_invisibility":
                    StartSpellConcentration(spell, route);
                    _invisibilityActive = true;
                    selfBuffMsg = "you slip from sight — enemies have -15% hit chance (concentration, breaks on attack).";
                    break;
                case "paladin_revivify":
                    if (_revivifyUsed)
                    {
                        selfBuffMsg = "Revivify already used this combat — slot consumed.";
                    }
                    else
                    {
                        var hpThreshold = _player.MaxHp / 4;
                        if (_player.CurrentHp <= hpThreshold)
                        {
                            _revivifyUsed = true;
                            var chaMod = Math.Max(0, _player.Mod(StatName.Charisma));
                            var reviveRoll = _rng.Next(1, 7) + _rng.Next(1, 7) + chaMod;
                            var reviveHeal = Math.Max(1, reviveRoll);
                            var reviveBefore = _player.CurrentHp;
                            _player.CurrentHp = Math.Min(_player.MaxHp, _player.CurrentHp + reviveHeal);
                            selfBuffMsg = $"divine energy surges — you recover {_player.CurrentHp - reviveBefore} HP!";
                        }
                        else
                        {
                            selfBuffMsg = "you're not wounded enough — Revivify fades unused. Slot consumed.";
                        }
                    }
                    break;
                // Batch 5 — signature powers
                case "mage_blink":
                    StartSpellConcentration(spell, route);
                    _blinkActive = true;
                    selfBuffMsg = "you phase in and out of the Ethereal Plane — 30% chance each hit misses (concentration).";
                    break;
                case "mage_protection_from_energy":
                {
                    StartSpellConcentration(spell, route);
                    _protEnergyActive = true;
                    _protEnergyElement = GetSelectedPendingSpellVariantId(spell);
                    if (string.IsNullOrWhiteSpace(_protEnergyElement)) _protEnergyElement = "fire";
                    selfBuffMsg = $"elemental wards reinforce you — half damage from {_protEnergyElement} (concentration).";
                    break;
                }
                case "cleric_beacon_of_hope":
                    StartSpellConcentration(spell, route);
                    _beaconOfHopeActive = true;
                    selfBuffMsg = "a beacon of divine light surrounds you — your next heal is doubled (concentration).";
                    break;
                case "bard_major_image":
                    StartSpellConcentration(spell, route);
                    _majorImageActive = true;
                    selfBuffMsg = "a vivid illusion appears beside you — 25% chance attacks target the decoy (concentration).";
                    break;
                case "paladin_aura_of_courage":
                    StartSpellConcentration(spell, route);
                    _auraOfCourageActive = true;
                    selfBuffMsg = "divine courage fills you — all incoming conditions last 1 fewer turn (concentration).";
                    break;
                default:
                    StartSpellConcentration(spell, route);
                    selfBuffMsg = "you feel empowered.";
                    break;
            }

            PushCombatLog($"{spell.Name} ({tierLabel}): {selfBuffMsg}");
            if (spell.RequiresSlot && !milestoneSlotWaive)
                PushCombatLog($"L{spell.SpellLevel} slots {_player.GetSpellSlots(spell.SpellLevel)}/{_player.GetSpellSlotsMax(spell.SpellLevel)}.");
            if (!spell.SuppressCounterAttack) DoEnemyAttack();
            return;
        }

        // Summon spells: persistent summoned entity that auto-attacks each turn.
        if (route.RouteKind == SpellEffectRouteKind.Summon)
        {
            if (!SpellData.SummonTypes.TryGetValue(spell.Id, out var summonType))
            {
                PushCombatLog($"{spell.Name}: summon definition not found.");
                if (!spell.SuppressCounterAttack) DoEnemyAttack();
                return;
            }

            // Replace existing summon if active
            if (_activeSummon != null)
            {
                if (_activeSummon.Type.Behavior == SummonBehaviorKind.BuffMount)
                {
                    _runDefenseBonus = Math.Max(0, _runDefenseBonus - 2);
                    _runFleeBonus = Math.Max(0, _runFleeBonus - 15);
                }
                PushCombatLog($"{_activeSummon.Type.Name} fades as you summon a new companion.");
                _activeSummon = null;
            }

            _activeSummon = new SummonInstance
            {
                Type = summonType,
                CurrentHp = summonType.MaxHp
            };

            // Apply BuffMount stat bonuses
            if (summonType.Behavior == SummonBehaviorKind.BuffMount)
            {
                _runDefenseBonus += 2;
                _runFleeBonus += 15;
            }

            StartSpellConcentration(spell, route);
            PushCombatLog($"{spell.Name} ({tierLabel}): {summonType.Description}");
            if (spell.RequiresSlot && !milestoneSlotWaive)
                PushCombatLog($"L{spell.SpellLevel} slots {_player.GetSpellSlots(spell.SpellLevel)}/{_player.GetSpellSlotsMax(spell.SpellLevel)}.");
            if (!spell.SuppressCounterAttack) DoEnemyAttack();
            return;
        }

        // Transformation spells: polymorph/shapeshift into a form.
        if (route.RouteKind == SpellEffectRouteKind.Transformation)
        {
            if (!SpellData.TransformationForms.TryGetValue(spell.Id, out var formIds) || formIds.Length == 0)
            {
                PushCombatLog($"{spell.Name}: no forms defined.");
                if (!spell.SuppressCounterAttack) DoEnemyAttack();
                return;
            }

            if (formIds.Length == 1)
            {
                ActivateTransformation(spell, route, formIds[0], milestoneSlotWaive);
                return;
            }

            // Multi-form spell — open form selection UI
            _pendingFormSpellId = spell.Id;
            _pendingFormOptions = formIds;
            _formSelectionIndex = 0;
            _gameState = GameState.CombatFormSelection;
            return;
        }

        // Cleanse spells: remove one active player condition.
        if (route.RouteKind == SpellEffectRouteKind.Cleanse)
        {
            if (_playerConditions.Count == 0)
            {
                PushCombatLog($"{spell.Name}: no active conditions to remove. Slot wasted.");
            }
            else
            {
                var worst = _playerConditions.OrderByDescending(c => c.Potency).First();
                _playerConditions.Remove(worst);
                PushCombatLog($"{spell.Name} ({tierLabel}): {worst.Kind} removed.");
            }
            if (spell.RequiresSlot && !milestoneSlotWaive)
                PushCombatLog($"L{spell.SpellLevel} slots {_player.GetSpellSlots(spell.SpellLevel)}/{_player.GetSpellSlotsMax(spell.SpellLevel)}.");
            if (!spell.SuppressCounterAttack) DoEnemyAttack();
            return;
        }

        if (IsEmpoweredMeleeSpell(spell))
        {
            if (!ResolveEmpoweredMeleeSpellStrike(spell, route, tierLabel))
            {
                return;
            }

            if (spell.RequiresSlot && !milestoneSlotWaive)
            {
                PushCombatLog($"L{spell.SpellLevel} slots {_player.GetSpellSlots(spell.SpellLevel)}/{_player.GetSpellSlotsMax(spell.SpellLevel)}.");
            }

            if (CheckEnemyDeath())
            {
                return;
            }

            DoEnemyAttack();
            return;
        }

        StartSpellConcentration(spell, route);

        var shouldCounterAttack = !spell.SuppressCounterAttack;
        var relicBurst = ConsumeRelicSpellBurstDamage();
        var milestoneArcBonus = milestoneSlotWaive ? GetArcDoctrineWaiveBonusDamage() : 0;
        var suppressedCombatantId = _currentEnemy != null
            ? BuildEncounterEnemyCombatantId(_currentEnemy)
            : null;
        var affectedEnemies = ResolveSpellAffectedEnemies(spell);
        var anchorEnemy = _currentEnemy;
        var damageTag = ResolveSpellDamageTagForCast(spell, route);
        var vampiricTouchAnchorHpBefore = anchorEnemy?.CurrentHp ?? 0;

        if (affectedEnemies.Count == 0 && route.HazardSpec == null)
        {
            PushCombatLog($"{spell.Name} ({tierLabel}) finds no enemy in its area.");
        }

        foreach (var enemy in affectedEnemies)
        {
            var appliedAnyDamage = false;
            var skipStatusesFromRouteSave = false;
            var damageNumerator = 1;
            var damageDenominator = 1;
            if (TryResolveSpellDamageSaveOutcome(spell, route, enemy, out var routeSaveMessage, out damageNumerator, out damageDenominator, out skipStatusesFromRouteSave) &&
                !string.IsNullOrWhiteSpace(routeSaveMessage))
            {
                PushCombatLog(routeSaveMessage);
            }

            if (route.DealsDirectDamage)
            {
                var (baseDamage, variance, armorBypass) = ResolveSpellDamageProfile(spell, enemy);
                var hitCount = ResolveSpellHitCount(spell);
                var channelDivinityBonus = GetAndConsumeChannelDivinityBonus();
                var directEmpowerReroll = ConsumeEmpowerSpellPrime();
                var spellBaseCalc = baseDamage + _player.SpellDamageBonus + GetClassSpellDamageBonus(_player) + _runSpellBonus + GetConditionSpellModifier() + channelDivinityBonus;
                for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
                {
                    var (damage, rawDamage, armorMitigation, statPower) = CalcSpellDamageAgainstEnemy(
                        enemy, spell.ScalingStat, spellBaseCalc, variance, armorBypass, spell.SpellLevel);
                    if (directEmpowerReroll && hitIndex == 0)
                    {
                        var (damage2, rawDamage2, armorMitigation2, statPower2) = CalcSpellDamageAgainstEnemy(
                            enemy, spell.ScalingStat, spellBaseCalc, variance, armorBypass, spell.SpellLevel);
                        if (damage2 > damage)
                        {
                            (damage, rawDamage, armorMitigation, statPower) = (damage2, rawDamage2, armorMitigation2, statPower2);
                            PushCombatLog("Empowered Spell — kept the better roll!");
                        }
                    }
                    var markedBonus = GetEnemyIncomingDamageBonus(enemy);
                    var anchorBonus = ReferenceEquals(enemy, anchorEnemy) && hitIndex == 0 ? relicBurst + milestoneArcBonus : 0;
                    var totalDamage = damage + anchorBonus + markedBonus;
                    if (damageNumerator == 0)
                    {
                        totalDamage = 0;
                    }
                    else if (damageDenominator > 1)
                    {
                        totalDamage = totalDamage * damageNumerator / damageDenominator;
                    }

                    enemy.CurrentHp = Math.Max(0, enemy.CurrentHp - totalDamage);
                    appliedAnyDamage |= totalDamage > 0;
                    if (totalDamage > 0)
                    {
                        TryBreakDamageSensitiveStatuses(enemy, spell.Name);
                    }

                    var hitLabel = hitCount > 1 ? $" hit {hitIndex + 1}" : string.Empty;
                    PushCombatLog($"{spell.Name} ({tierLabel}){hitLabel} hits {enemy.Type.Name} for {totalDamage} {damageTag}.");
                    PushCombatLog($"Raw {rawDamage} (stat +{statPower}) - armor {armorMitigation}.");
                    if (markedBonus > 0)
                    {
                        PushCombatLog($"{enemy.Type.Name} suffers +{markedBonus} marked damage.");
                    }
                }

                if (!skipStatusesFromRouteSave &&
                    enemy.IsAlive &&
                    string.Equals(spell.Id, "bard_thunderwave", StringComparison.Ordinal) &&
                    TryPushEnemyAwayFromPoint(enemy, _player.X, _player.Y, 2, out var pushedTiles))
                {
                    PushCombatLog($"{enemy.Type.Name} is hurled back {pushedTiles} tile(s).");
                }
            }
            else
            {
                PushCombatLog($"{spell.Name} ({tierLabel}) targets {enemy.Type.Name}.");
            }

            foreach (var statusMessage in skipStatusesFromRouteSave
                ? Array.Empty<string>()
                : ApplySpellOnHitStatuses(spell, route, enemy))
            {
                PushCombatLog(statusMessage);
            }

            if (appliedAnyDamage)
            {
                PushCombatLog($"{enemy.Type.Name} HP {enemy.CurrentHp}/{enemy.Type.MaxHp}.");
            }
        }

        if (relicBurst > 0)
        {
            PushCombatLog($"Astral Conduit amplifies the cast (+{relicBurst}).");
        }
        if (milestoneSlotWaive)
        {
            PushCombatLog($"Arc Doctrine preserves this slot (+{milestoneArcBonus} damage). Charges left: {_milestoneArcChargesThisCombat}.");
        }
        if (spell.RequiresSlot && !milestoneSlotWaive)
        {
            PushCombatLog($"L{spell.SpellLevel} slots {_player.GetSpellSlots(spell.SpellLevel)}/{_player.GetSpellSlotsMax(spell.SpellLevel)}.");
        }

        // Vampiric Touch: heal player for 50% of damage dealt (tracked via anchor enemy HP delta)
        if (string.Equals(spell.Id, "mage_vampiric_touch", StringComparison.Ordinal) && _player != null && anchorEnemy != null)
        {
            var vtDamageDealt = Math.Max(0, vampiricTouchAnchorHpBefore - anchorEnemy.CurrentHp);
            if (vtDamageDealt > 0)
            {
                var vtHeal = Math.Max(1, vtDamageDealt / 2);
                var vtBefore = _player.CurrentHp;
                _player.CurrentHp = Math.Min(_player.MaxHp, _player.CurrentHp + vtHeal);
                if (_player.CurrentHp > vtBefore)
                    PushCombatLog($"Vampiric Touch drains life — you recover {_player.CurrentHp - vtBefore} HP.");
            }
        }

        if (route.HazardSpec != null)
        {
            PlaceCombatHazard(spell, route);
        }

        if (spell.SuppressCounterAttack)
        {
            PushCombatLog($"{anchorEnemy?.Type.Name ?? "The target"} is disrupted and cannot counter this turn.");
        }

        var requiresImmediateDeathResolution = route.TargetShape != SpellTargetShape.SingleEnemy || route.HazardSpec != null;
        if (requiresImmediateDeathResolution)
        {
            if (ResolveEncounterEnemyDeathsImmediate()) return;
        }
        else if (CheckEnemyDeath())
        {
            return;
        }

        // Spiritual Weapon: once per combat — bonus melee attack for 1d8 + WIS mod after spell
        if (_spiritualWeaponPrimed && _player != null && _currentEnemy != null && _currentEnemy.IsAlive)
        {
            _spiritualWeaponPrimed = false;
            var wisMod = _player.Mod(StatName.Wisdom);
            var swAtkBonus = wisMod + GetProficiencyBonus();
            var swAtkBonusStr = swAtkBonus >= 0 ? $"+{swAtkBonus}" : $"{swAtkBonus}";
            var (swHitResult, swD20, swTotal) = CombatMath.RollAttack(swAtkBonus, _currentEnemy.Type.ArmorClass, _rng);
            if (swHitResult != AttackRollResult.Miss)
            {
                var swDice = _rng.Next(1, 9);
                var swDmg = Math.Max(1, swDice + Math.Max(0, wisMod));
                _currentEnemy.CurrentHp = Math.Max(0, _currentEnemy.CurrentHp - swDmg);
                PushCombatLog($"Spiritual Weapon! d20{swAtkBonusStr}={swTotal} hits for {swDmg} ({swDice}+{wisMod} WIS).");
                PushCombatLog($"{_currentEnemy.Type.Name} HP {_currentEnemy.CurrentHp}/{_currentEnemy.Type.MaxHp}.");
                if (CheckEnemyDeath()) return;
            }
            else
            {
                PushCombatLog($"Spiritual Weapon misses! d20{swAtkBonusStr}={swTotal} vs AC {_currentEnemy.Type.ArmorClass}.");
            }
        }

        if (shouldCounterAttack)
        {
            DoEnemyAttack();
        }
        else
        {
            DoEnemyAttack(suppressedEnemyCombatantId: suppressedCombatantId);
        }
    }

    private (int damage, int rawDamage, int armorMitigation, int statPower) CalcSpellDamage(StatName scaleStat, int baseDamage, int variance, int armorBypass, int spellLevel = 1, int cantripDiceSides = 0)
    {
        return CalcSpellDamageAgainstEnemy(_currentEnemy, scaleStat, baseDamage, variance, armorBypass, spellLevel, cantripDiceSides);
    }

    private (int damage, int rawDamage, int armorMitigation, int statPower) CalcSpellDamageAgainstEnemy(Enemy? target, StatName scaleStat, int baseDamage, int variance, int armorBypass, int spellLevel = 1, int cantripDiceSides = 0)
    {
        if (_player == null || target == null) return (0, 0, 0, 0);

        // Cantrip level scaling (D&D 5e: 1 die at L1, 2 at L5, 3 at L11, 4 at L17)
        if (spellLevel == 0 && cantripDiceSides > 0)
        {
            int diceCount = _player.Level switch { >= 17 => 4, >= 11 => 3, >= 5 => 2, _ => 1 };
            int diceTotal = 0;
            for (int i = 0; i < diceCount; i++)
                diceTotal += _rng.Next(1, cantripDiceSides + 1);
            // Add spell damage bonuses (feats, etc.) but NOT spellcasting stat (D&D cantrip rule)
            var spellBonus = _player.SpellDamageBonus + GetClassSpellDamageBonus(_player);
            var rawDamage_c = diceTotal + spellBonus;
            // Cantrips still check save/AC for final damage
            var effectiveArmor_c = Math.Max(0, target.Type.Defense - armorBypass);
            var finalDmg_c = CombatMath.CalculateFinalDamage(rawDamage_c, target.Type.Defense, armorBypass);
            return (finalDmg_c, rawDamage_c, effectiveArmor_c, 0);
        }

        var mod = _player.Mod(scaleStat);
        var statPower = spellLevel switch
        {
            0 => 0,                             // cantrips: no stat scaling
            1 => Math.Max(0, mod),              // L1: single mod
            _ => Math.Max(0, mod * 2)           // L2+: doubled mod (unchanged)
        };
        // Overchannel: maximize all damage dice of this spell
        int varianceRoll;
        if (_overchannelPrimed && spellLevel > 0)
        {
            _overchannelPrimed = false;
            varianceRoll = variance; // max roll
            PushCombatLog("Overchannel — spell damage maximized!");
            // Recoil: 1d6 damage to player
            var recoil = _rng.Next(1, 7);
            if (_player != null)
            {
                _player.CurrentHp = Math.Max(0, _player.CurrentHp - recoil);
                PushCombatLog($"Overchannel recoil: {recoil} damage to you! HP {_player.CurrentHp}/{_player.MaxHp}.");
            }
        }
        else
        {
            varianceRoll = variance > 0 ? _rng.Next(variance + 1) : 0;
        }
        var raw = CombatMath.CalculateSpellRawDamage(baseDamage, statPower, varianceRoll);
        var effectiveArmor = Math.Max(0, target.Type.Defense - armorBypass);
        var finalDamage = CombatMath.CalculateFinalDamage(raw, target.Type.Defense, armorBypass);
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

    private void EnsureCombatSkillSelectionVisible(int skillCount)
    {
        if (skillCount <= SkillVisibleCount)
        {
            _combatSkillMenuOffset = 0;
            return;
        }

        if (_selectedCombatSkillIndex < _combatSkillMenuOffset)
        {
            _combatSkillMenuOffset = _selectedCombatSkillIndex;
        }
        else if (_selectedCombatSkillIndex >= _combatSkillMenuOffset + SkillVisibleCount)
        {
            _combatSkillMenuOffset = _selectedCombatSkillIndex - SkillVisibleCount + 1;
        }

        var maxOffset = Math.Max(0, skillCount - SkillVisibleCount);
        _combatSkillMenuOffset = Math.Clamp(_combatSkillMenuOffset, 0, maxOffset);
    }

    private static int ClampMenuOffsetToVisibleCount(int selectedIndex, int totalCount, int currentOffset, int visibleCount)
    {
        if (visibleCount <= 0 || totalCount <= visibleCount)
        {
            return 0;
        }

        if (selectedIndex < currentOffset)
        {
            currentOffset = selectedIndex;
        }
        else if (selectedIndex >= currentOffset + visibleCount)
        {
            currentOffset = selectedIndex - visibleCount + 1;
        }

        return Math.Clamp(currentOffset, 0, Math.Max(0, totalCount - visibleCount));
    }

    private bool HasAnyLearnableSpells()
    {
        return _player != null && _spellLearnChoices.Any(spell => _player.CanLearnSpell(spell, out _));
    }

    private static string GetFeatPrerequisiteLabel(FeatDefinition feat)
    {
        if (!string.IsNullOrWhiteSpace(feat.PrerequisiteText))
        {
            return feat.PrerequisiteText!;
        }

        var requirementParts = new List<string>();

        if (feat.RequiredFeatIds.Count > 0)
        {
            var requiredNames = feat.RequiredFeatIds
                .Select(id => FeatBook.ById.TryGetValue(id, out var requiredFeat) ? requiredFeat.Name : id);
            requirementParts.Add(string.Join(", ", requiredNames));
        }

        if (feat.RequiresCasterClass)
        {
            requirementParts.Add("a spellcasting class");
        }

        if (requirementParts.Count == 0)
        {
            return "No prerequisite.";
        }

        return $"Requires {string.Join(" and ", requirementParts)}.";
    }

    private string GetFeatEffectLabel(FeatDefinition feat)
    {
        if (_player == null)
        {
            return feat.Effect;
        }

        return _player.GetFeatEffectText(feat);
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
        var bonus = player.CharacterClass.Name switch
        {
            "Mage" => 1,
            "Cleric" => 1,
            "Bard" => 1,
            "Paladin" => 1,
            "Ranger" => 1,
            _ => 0
        };
        // Blessed Strikes: Cleric feat — Potent Spellcasting (WIS mod added to spell damage)
        if (player.HasFeat("cleric_blessed_strikes_feat"))
            bonus += Math.Max(0, player.Mod(StatName.Wisdom));
        return bonus;
    }

    private int GetClassCritRangeBonus(Player player)
    {
        return player.CharacterClass.Name switch
        {
            "Rogue" => 1,
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

    private WeaponDefinition GetEquippedWeaponDef()
    {
        return WeaponBook.ById.TryGetValue(_equippedWeaponId, out var weapon)
            ? weapon
            : WeaponBook.Unarmed;
    }

    private int GetPlayerArmorClass()
    {
        if (_player == null) return 10;
        var armorCategory = GetCurrentArmorCategory();
        return _player.GetArmorClass(armorCategory)
            + GetClassDefenseBonus(_player)
            + _runDefenseBonus
            + GetConditionDefenseModifier();
    }

    private (int damage, bool crit, int rawDamage, int armorMitigation, int critChance) CalcPlayerDamage(bool forceCrit = false, bool bypassArmor = false)
    {
        if (_player == null || _currentEnemy == null) return (0, false, 0, 0, 0);

        var weapon = GetEquippedWeaponDef();

        // Resolve attack stat: finesse uses max(STR, DEX), ranged uses DEX, else weapon's stat
        StatName atkStat;
        if (weapon.IsFinesse)
            atkStat = _player.Mod(StatName.Strength) >= _player.Mod(StatName.Dexterity)
                ? StatName.Strength : StatName.Dexterity;
        else if (weapon.IsRanged)
            atkStat = StatName.Dexterity;
        else
            atkStat = weapon.AttackStat;

        var atkMod = _player.Mod(atkStat);
        var classMeleeBonus = GetClassMeleeDamageBonus(_player);

        // Roll weapon dice — double on crit (D&D: double dice only, not flat bonuses)
        // Brutal Critical: on crit, roll one additional weapon damage die
        var brutalCritExtra = (forceCrit && _player.HasFeat("barbarian_brutal_critical_feat")) ? 1 : 0;
        var diceCount = (forceCrit ? weapon.DiceCount * 2 : weapon.DiceCount) + brutalCritExtra;
        var diceRolls = new int[diceCount];
        for (int i = 0; i < diceCount; i++)
            diceRolls[i] = _rng.Next(1, weapon.DamageDice + 1);

        // Piercer: reroll one weapon damage die and keep the higher result (once per turn)
        if (_player.HasFeat("piercer_feat") && diceRolls.Length > 0)
        {
            int lowestIdx = 0;
            for (int i = 1; i < diceRolls.Length; i++)
                if (diceRolls[i] < diceRolls[lowestIdx]) lowestIdx = i;
            var reroll = _rng.Next(1, weapon.DamageDice + 1);
            if (reroll > diceRolls[lowestIdx])
                diceRolls[lowestIdx] = reroll;
        }

        var diceDamage = 0;
        foreach (var r in diceRolls) diceDamage += r;

        // Flat bonuses (NOT doubled on crit)
        var flatBonus = atkMod + _player.MeleeDamageBonus + classMeleeBonus
            + _runMeleeBonus + GetConditionMeleeModifier();

        var rawDamage = diceDamage + Math.Max(0, flatBonus);
        var finalDamage = Math.Max(1, rawDamage);
        var markedBonus = GetEnemyIncomingDamageBonus(_currentEnemy);
        finalDamage += markedBonus;

        var displayThreshold = Math.Max(2, _player.CritThreshold - GetClassCritRangeBonus(_player) - _runCritBonus);
        return (finalDamage, forceCrit, rawDamage, 0, displayThreshold);
    }

    private void DoTransformedPlayerAttack()
    {
        if (_player == null || _currentEnemy == null || _activeTransformation == null) return;
        var form = _activeTransformation.Form;

        if (!form.CanAttack)
        {
            PushCombatLog("You cannot attack in this form!");
            return;
        }

        // Apply PackTactics: +2 if enemy already damaged
        var atkBonus = form.AttackBonus;
        if (form.Special == FormSpecialKind.PackTactics && _currentEnemy.CurrentHp < _currentEnemy.Type.MaxHp)
            atkBonus += form.SpecialValue;

        // Apply ArmorBypass: reduce effective AC
        var effectiveAC = _currentEnemy.Type.ArmorClass;
        if (form.Special == FormSpecialKind.ArmorBypass)
            effectiveAC = Math.Max(0, effectiveAC - form.SpecialValue);

        var (hitResult, d20, rollTotal) = CombatMath.RollAttack(atkBonus, effectiveAC, _rng);
        var isCrit = hitResult == AttackRollResult.CriticalHit;
        var atkBonusStr = atkBonus >= 0 ? $"+{atkBonus}" : $"{atkBonus}";

        if (hitResult != AttackRollResult.Miss)
        {
            // Roll form damage
            var dice = 0;
            for (var i = 0; i < form.DamageCount; i++)
                dice += _rng.Next(1, form.DamageDice + 1);
            var dmgBonus = form.DamageBonus;

            // UseCasterStatMod: add caster's spell stat mod
            if (form.UseCasterStatMod)
            {
                var scaleStat = SpellData.ById.TryGetValue(_activeTransformation.SourceSpellId, out var spellDef)
                    ? spellDef.ScalingStat : StatName.Wisdom;
                dmgBonus += Math.Max(0, _player.Mod(scaleStat));
            }

            var dmg = Math.Max(1, dice + dmgBonus);

            // Crit: double dice
            if (isCrit) dmg += dice;

            // BonusCritDamage special
            if (isCrit && form.Special == FormSpecialKind.BonusCritDamage)
                dmg += _rng.Next(1, form.SpecialValue + 1);

            // FirstHitBonus special (consumed on first hit)
            if (_activeTransformation.FirstHitPrimed && form.Special == FormSpecialKind.FirstHitBonus)
            {
                dmg += _rng.Next(1, form.SpecialValue + 1);
                _activeTransformation.FirstHitPrimed = false;
                PushCombatLog($"Ambush strike! Bonus 1d{form.SpecialValue} damage.");
            }

            _currentEnemy.CurrentHp = Math.Max(0, _currentEnemy.CurrentHp - dmg);
            var critTag = isCrit ? " CRIT!" : "";
            PushCombatLog($"{form.Name} attacks! d20{atkBonusStr}={rollTotal}{critTag} for {dmg} {form.DamageType}.");
            PushCombatLog($"{_currentEnemy.Type.Name} HP {_currentEnemy.CurrentHp}/{_currentEnemy.Type.MaxHp}.");

            // PoisonOnHit: apply Poison status to enemy
            if (form.Special == FormSpecialKind.PoisonOnHit && _currentEnemy.IsAlive)
            {
                var existing = _currentEnemy.StatusEffects.FirstOrDefault(s => s.Kind == CombatStatusKind.Poison);
                if (existing != null)
                {
                    existing.Potency = Math.Max(existing.Potency, form.SpecialValue);
                    existing.RemainingTurns = Math.Max(existing.RemainingTurns, 2);
                }
                else
                {
                    _currentEnemy.StatusEffects.Add(new CombatStatusState
                    {
                        Kind = CombatStatusKind.Poison,
                        Potency = form.SpecialValue,
                        RemainingTurns = 2,
                        SourceSpellId = _activeTransformation.SourceSpellId,
                        SourceLabel = form.Name
                    });
                }
                PushCombatLog($"Venom! {_currentEnemy.Type.Name} poisoned ({form.SpecialValue} dmg/turn, 2 turns).");
            }

            // BurnOnHit: apply Burning status to enemy
            if (form.Special == FormSpecialKind.BurnOnHit && _currentEnemy.IsAlive)
            {
                var existing = _currentEnemy.StatusEffects.FirstOrDefault(s => s.Kind == CombatStatusKind.Burning);
                if (existing != null)
                {
                    existing.Potency = Math.Max(existing.Potency, form.SpecialValue);
                    existing.RemainingTurns = Math.Max(existing.RemainingTurns, 2);
                }
                else
                {
                    _currentEnemy.StatusEffects.Add(new CombatStatusState
                    {
                        Kind = CombatStatusKind.Burning,
                        Potency = form.SpecialValue,
                        RemainingTurns = 2,
                        SourceSpellId = _activeTransformation.SourceSpellId,
                        SourceLabel = form.Name
                    });
                }
                PushCombatLog($"Ignites! {_currentEnemy.Type.Name} burning ({form.SpecialValue} fire/turn, 2 turns).");
            }

            // DebuffOnHit: apply Weakened status to enemy (persistent -2 attack for 2 turns)
            if (form.Special == FormSpecialKind.DebuffOnHit && _currentEnemy.IsAlive)
            {
                var existing = _currentEnemy.StatusEffects.FirstOrDefault(s => s.Kind == CombatStatusKind.Weakened);
                if (existing != null)
                {
                    existing.Potency = Math.Max(existing.Potency, form.SpecialValue);
                    existing.RemainingTurns = Math.Max(existing.RemainingTurns, 2);
                }
                else
                {
                    _currentEnemy.StatusEffects.Add(new CombatStatusState
                    {
                        Kind = CombatStatusKind.Weakened,
                        Potency = form.SpecialValue,
                        RemainingTurns = 2,
                        SourceSpellId = _activeTransformation.SourceSpellId,
                        SourceLabel = form.Name
                    });
                }
                PushCombatLog($"Debilitating blow! {_currentEnemy.Type.Name} weakened (-{form.SpecialValue} attack, 2 turns).");
            }

            // HealOnKill
            if (_currentEnemy.CurrentHp <= 0 && form.Special == FormSpecialKind.HealOnKill)
            {
                var healAmt = _rng.Next(1, form.SpecialValue + 1);
                var before = _player.CurrentHp;
                _player.CurrentHp = Math.Min(_player.MaxHp, _player.CurrentHp + healAmt);
                PushCombatLog($"Devour! Heal {_player.CurrentHp - before} HP.");
            }

            if (!_currentEnemy.IsAlive) { ResolveEncounterEnemyDeathsImmediate(); return; }
        }
        else
        {
            PushCombatLog($"{form.Name} misses! d20{atkBonusStr}={rollTotal} vs AC {effectiveAC}.");
        }

        // Counter-attack (unless NoCounterAttack special)
        if (form.Special != FormSpecialKind.NoCounterAttack)
            DoEnemyAttack();
    }

    private void DoPlayerAttack()
    {
        if (_activeTransformation != null)
        {
            DoTransformedPlayerAttack();
            return;
        }
        if (_player == null || _currentEnemy == null) return;

        // Sanctuary breaks when you attack
        if (_sanctuaryActive)
        {
            _sanctuaryActive = false;
            EndActiveConcentration("You broke Sanctuary by attacking.");
        }

        // Invisibility breaks when you attack
        if (_invisibilityActive)
        {
            _invisibilityActive = false;
            EndActiveConcentration("Invisibility breaks as you strike.");
        }
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

        // Vanish: advantage on this attack
        if (_vanishPrimed)
        {
            _vanishPrimed = false;
            _playerAttackAdvantage = true;
            PushCombatLog("Vanish! You slip into shadow — advantage on this attack.");
        }

        // Assassin: advantage + auto-crit on hit if enemy hasn't taken their first turn yet
        var assassinForceCrit = _player.HasFeat("rogue_assassin_feat") && !_enemyHasActedThisCombat;
        if (assassinForceCrit)
        {
            _playerAttackAdvantage = true;
            PushCombatLog("Assassin — striking before the enemy reacts! Advantage + auto-crit on hit.");
        }

        // Lucky: advantage on this attack
        if (_luckyPrimed)
        {
            _luckyPrimed = false;
            _playerAttackAdvantage = true;
            PushCombatLog("Lucky — fortune favors the bold! Advantage on this attack.");
        }

        var markBonus = GetEnemyIncomingDamageBonus(_currentEnemy);
        // Sharpshooter: capture prime state before consuming (so both CalcPlayerDamage calls use it)
        var sharpshooterActive = _sharpshooterPrimed;

        // --- D&D Attack Roll ---
        bool attackHit = false;
        bool attackIsCrit = false;
        var weapon = GetEquippedWeaponDef();
        StatName atkStat;
        if (weapon.IsFinesse)
            atkStat = _player.Mod(StatName.Strength) >= _player.Mod(StatName.Dexterity)
                ? StatName.Strength : StatName.Dexterity;
        else if (weapon.IsRanged)
            atkStat = StatName.Dexterity;
        else
            atkStat = weapon.AttackStat;

        var totalAttackBonus = _player.Mod(atkStat) + GetProficiencyBonus() + GetPlayerConditionAttackPenalty();
        // Bardic Inspiration: add 1d6 to attack roll
        if (_bardicInspirationPrimed && _bardicInspirationForAttack)
        {
            _bardicInspirationPrimed = false;
            var inspRoll = _rng.Next(1, 7);
            totalAttackBonus += inspRoll;
            PushCombatLog($"Bardic Inspiration — +{inspRoll} to your attack roll!");
        }
        // Greater Invisibility: advantage on player attacks
        if (_greaterInvisibilityActive)
            _playerAttackAdvantage = true;
        var critThreshold = Math.Max(2, _player.CritThreshold - GetClassCritRangeBonus(_player) - _runCritBonus);
        var (hitResult, d20Raw, attackTotal) = CombatMath.RollAttack(
            totalAttackBonus, _currentEnemy.Type.ArmorClass, _rng,
            advantage: _playerAttackAdvantage, disadvantage: _playerAttackDisadvantage,
            critThreshold: critThreshold);
        _playerAttackAdvantage = false;
        _playerAttackDisadvantage = false;

        attackHit = hitResult != AttackRollResult.Miss;
        attackIsCrit = hitResult == AttackRollResult.CriticalHit;

        var acTarget = _currentEnemy.Type.ArmorClass;
        if (!attackHit)
        {
            PushCombatLog($"You miss! d20:{d20Raw}+{totalAttackBonus}={attackTotal} vs AC {acTarget}.");
            DoEnemyAttack();
            return;
        }
        var critLabel = attackIsCrit ? " CRITICAL HIT!" : string.Empty;
        PushCombatLog($"You hit! d20:{d20Raw}+{totalAttackBonus}={attackTotal} vs AC {acTarget}.{critLabel}");

        // Override forceCrit with actual crit determination (Assassin: auto-crit on any hit before enemy acts)
        var resolvedCrit = attackIsCrit || (assassinForceCrit && attackHit);
        if (assassinForceCrit && attackHit && !attackIsCrit)
            PushCombatLog("Assassin — auto-critical!");

        // Crusher: on a critical hit, impose Disadvantage on the enemy's next attack
        if (resolvedCrit && _player.HasFeat("crusher_feat") && _currentEnemy.IsAlive)
        {
            _enemyNextAttackDisadvantage = true;
            PushCombatLog("Crusher — the impact staggers the enemy! Disadvantage on their next attack.");
        }

        var (damage, crit, rawDamage, armorMitigation, critThresholdDisplay) = CalcPlayerDamage(forceCrit: resolvedCrit, bypassArmor: sharpshooterActive);

        // Savage Attacker: roll twice, keep higher result (D&D 2024: once per turn)
        if (_player.HasFeat("savage_attacker_feat"))
        {
            var (damage2, crit2, rawDamage2, armorMitigation2, _) = CalcPlayerDamage(forceCrit: resolvedCrit, bypassArmor: sharpshooterActive);
            if (damage2 > damage)
            {
                (damage, crit, rawDamage, armorMitigation) = (damage2, crit2, rawDamage2, armorMitigation2);
                PushCombatLog("Savage Attacker — kept the better roll!");
            }
        }

        // Battle Cry: flat armor-bypassing bonus damage
        var battleCryBonus = 0;
        if (_battleCryPrimed)
        {
            battleCryBonus = Math.Max(2, 3 + _player.Mod(StatName.Strength));
            _battleCryPrimed = false;
            PushCombatLog($"Battle Cry unleashed: +{battleCryBonus} bonus damage.");
        }

        // Divine Smite: consume L1 slot for +5 radiant bonus
        var divineSmiteBonus = 0;
        if (_divineSmitePrimed)
        {
            _divineSmitePrimed = false;
            if (_player.TryConsumeSpellSlot(1))
            {
                divineSmiteBonus = 5;
                PushCombatLog($"Divine Smite! Holy power surges for +{divineSmiteBonus} radiant damage.");
            }
            else
            {
                PushCombatLog("Divine Smite fizzled — no L1 spell slot available.");
            }
        }

        // Great Weapon Master: passive — add proficiency bonus to every melee hit (D&D 2024)
        var gwmPassive = _player.HasFeat("great_weapon_master_feat") ? GetProficiencyBonus() : 0;

        // Sharpshooter: once/combat — precision physical attack ignores all armor, +5 damage
        var sharpshooterFlatBonus = 0;
        if (_sharpshooterPrimed)
        {
            _sharpshooterPrimed = false;
            sharpshooterFlatBonus = 5;
            PushCombatLog("Sharpshooter — precision shot ignores armor!");
        }

        var divineFavorBonus = _divineFavorActive ? _rng.Next(1, 5) : 0;
        if (_divineFavorActive && divineFavorBonus > 0)
            PushCombatLog($"Divine Favor — +{divineFavorBonus} radiant!");

        // Bless: +1d4 bonus damage per hit
        var blessBonus = 0;
        if (_blessActive)
        {
            blessBonus = _rng.Next(1, 5);
            PushCombatLog($"Bless — +{blessBonus} divine guidance!");
        }

        // Magic Weapon: +1d6 force per hit
        var magicWeaponBonus = 0;
        if (_magicWeaponActive)
        {
            magicWeaponBonus = _rng.Next(1, 7);
            PushCombatLog($"Magic Weapon — +{magicWeaponBonus} force!");
        }

        // Flame Arrows: +1d8 fire per hit
        var flameArrowsBonus = 0;
        if (_flameArrowsActive)
        {
            flameArrowsBonus = _rng.Next(1, 9);
            PushCombatLog($"Flame Arrows — +{flameArrowsBonus} fire!");
        }

        // Elemental Weapon: +2d4 of chosen element per hit
        var elementalWeaponBonus = 0;
        if (_elementalWeaponActive)
        {
            elementalWeaponBonus = _rng.Next(1, 5) + _rng.Next(1, 5);
            PushCombatLog($"Elemental Weapon — +{elementalWeaponBonus} {_elementalWeaponElement}!");
        }

        // Crusader's Mantle: +1d6 radiant per hit
        var crusadersMantleBonus = 0;
        if (_crusadersMantleActive)
        {
            crusadersMantleBonus = _rng.Next(1, 7);
            PushCombatLog($"Crusader's Mantle — +{crusadersMantleBonus} radiant!");
        }

        // Zephyr Strike: +1d8 force NEXT HIT ONLY (consumed)
        var zephyrStrikeBonus = 0;
        if (_zephyrStrikeActive && _zephyrStrikeHitPrimed)
        {
            _zephyrStrikeHitPrimed = false;
            zephyrStrikeBonus = _rng.Next(1, 9);
            PushCombatLog($"Zephyr Strike — +{zephyrStrikeBonus} force surge!");
        }

        // Colossus Slayer: +1d8 bonus damage when enemy is below max HP
        var colossusSlayerBonus = 0;
        if (_player.HasFeat("ranger_colossus_slayer_feat") && _currentEnemy.CurrentHp < _currentEnemy.Type.MaxHp)
        {
            colossusSlayerBonus = _rng.Next(1, 9);
            PushCombatLog($"Colossus Slayer — +{colossusSlayerBonus} bonus damage to wounded prey!");
        }

        // Absorb Elements: +1d6 on next melee hit (consumed)
        var absorbElementsBonus = 0;
        if (_absorbElementsCharged)
        {
            _absorbElementsCharged = false;
            absorbElementsBonus = _rng.Next(1, 7);
            PushCombatLog($"Absorb Elements — +{absorbElementsBonus} elemental surge!");
        }

        // Hex: +1d4 necrotic per hit
        var hexBonus = 0;
        if (_hexActive)
        {
            hexBonus = _rng.Next(1, 5);
            PushCombatLog($"Hex — +{hexBonus} necrotic!");
        }

        // Spirit Shroud: +1d8 radiant per melee hit
        var spiritShroudBonus = 0;
        if (_spiritShroudActive)
        {
            spiritShroudBonus = _rng.Next(1, 9);
            PushCombatLog($"Spirit Shroud — +{spiritShroudBonus} radiant!");
        }

        var total = damage + warCryDamage + battleCryBonus + divineSmiteBonus + gwmPassive + sharpshooterFlatBonus + divineFavorBonus + magicWeaponBonus + flameArrowsBonus + crusadersMantleBonus + zephyrStrikeBonus + colossusSlayerBonus + blessBonus + absorbElementsBonus + hexBonus + spiritShroudBonus + elementalWeaponBonus;
        _currentEnemy.CurrentHp = Math.Max(0, _currentEnemy.CurrentHp - total);
        if (total > 0)
        {
            TryBreakDamageSensitiveStatuses(_currentEnemy, "your attack");
        }
        var critTag = crit ? " CRIT x2!" : string.Empty;
        PushCombatLog($"You hit for {total}.{critTag}");
        PushCombatLog($"Raw {rawDamage} - armor {armorMitigation} | Crit on {critThresholdDisplay}+.");
        if (markBonus > 0)
        {
            PushCombatLog($"Marked target suffers +{markBonus} bonus damage.");
        }
        PushCombatLog($"{_currentEnemy.Type.Name} HP {_currentEnemy.CurrentHp}/{_currentEnemy.Type.MaxHp}.");
        ApplyRelicMeleeTrigger();

        if (_player.HasBonusAttack && _currentEnemy.IsAlive)
        {
            var (bonus, bonusCrit, bonusRaw, bonusArmor, _) = CalcPlayerDamage();
            _currentEnemy.CurrentHp = Math.Max(0, _currentEnemy.CurrentHp - bonus);
            if (bonus > 0)
            {
                TryBreakDamageSensitiveStatuses(_currentEnemy, "Swift Strikes");
            }
            var bonusCritTag = bonusCrit ? " CRIT x2!" : string.Empty;
            PushCombatLog($"Swift Strikes: +{bonus}.{bonusCritTag}");
            PushCombatLog($"{_currentEnemy.Type.Name} HP {_currentEnemy.CurrentHp}/{_currentEnemy.Type.MaxHp}.");
            ApplyRelicMeleeTrigger();
        }

        if (_player.PoisonDamage > 0 && _currentEnemy.IsAlive)
        {
            var poisonApplied = TryApplyOrRefreshEnemyStatus(
                _currentEnemy,
                new CombatStatusApplySpec
                {
                    Kind = CombatStatusKind.Poison,
                    Potency = _player.PoisonDamage,
                    DurationTurns = 2
                },
                "poison_blade",
                "Poison Blade",
                null,
                out var poisonMessage);
            if (poisonApplied && !string.IsNullOrWhiteSpace(poisonMessage))
            {
                PushCombatLog(poisonMessage);
            }
        }

        if (CheckEnemyDeath()) return;

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

    private void DoArcaneWard()
    {
        if (_player == null || _currentEnemy == null) return;
        if (_gameState == GameState.DeathScreen || !_player.IsAlive) return;

        _arcaneWardUsedThisCombat = true;
        var absorb = _player.ArcaneWardAbsorb;
        PushCombatLog($"Arcane Ward primed ({absorb} absorb).");
        DoEnemyAttack(firstEnemyDamageAbsorb: absorb);
    }

    private void DoChannelDivinity()
    {
        if (_player == null || _currentEnemy == null) return;
        _channelDivinityUsedThisCombat = true;
        _channelDivinityPrimed = true;
        var bonus = _player.ChannelDivinityBonus;
        PushCombatLog($"Channel Divinity primed — next divine spell deals +{bonus} damage.");
        _gameState = GameState.Combat;
    }

    private void DoCuttingWords()
    {
        if (_player == null || _currentEnemy == null) return;
        _cuttingWordsUsedThisCombat = true;
        var reduction = _rng.Next(1, 5); // 1d4
        PushCombatLog($"Cutting Words — enemy's next attack roll reduced by {reduction}.");
        DoEnemyAttack(attackRollPenalty: reduction);
    }

    private void DoLayOnHands()
    {
        if (_player == null) return;
        _layOnHandsUsedThisCombat = true;
        var heal = _player.LayOnHandsHeal;
        var before = _player.CurrentHp;
        _player.CurrentHp = Math.Min(_player.CurrentHp + heal, _player.MaxHp);
        var recovered = _player.CurrentHp - before;
        PushCombatLog($"Lay on Hands restores {recovered} HP.");
        PushCombatLog($"HP {before} -> {_player.CurrentHp}/{_player.MaxHp}.");
        DoEnemyAttack();
    }

    private void DoBattleCry()
    {
        if (_player == null) return;
        _battleCryUsedThisCombat = true;
        _battleCryPrimed = true;
        var bonus = Math.Max(2, 3 + _player.Mod(StatName.Strength));
        PushCombatLog($"Battle Cry! Next attack deals +{bonus} bonus damage.");
        _gameState = GameState.Combat;
    }

    private void DoVanish()
    {
        if (_player == null) return;
        _vanishUsedThisCombat = true;
        _vanishPrimed = true;
        PushCombatLog("Vanish! You slip into shadow — next attack is a guaranteed critical hit.");
        _gameState = GameState.Combat;
    }

    private void DoDivineSmite()
    {
        if (_player == null) return;
        _divineSmiteUsedThisCombat = true;
        _divineSmitePrimed = true;
        PushCombatLog("Divine Smite primed — next melee hit channels +5 radiant damage.");
        _gameState = GameState.Combat;
    }

    private void DoDivineFavor()
    {
        if (_player == null) return;
        _divineFavorUsedThisCombat = true;
        _divineFavorActive = true;
        PushCombatLog("Divine Favor — sacred radiance infuses your weapon. Each hit deals +1d4 radiant damage.");
        _gameState = GameState.Combat;
    }

    private void DoEmpowerSpell()
    {
        if (_player == null) return;
        _empowerSpellUsedThisCombat = true;
        _empowerSpellPrimed = true;
        PushCombatLog("Empowered Spell primed — next spell rolls damage twice, keeps the higher result.");
        _gameState = GameState.Combat;
    }

    private void DoWordOfRenewal()
    {
        if (_player == null) return;
        _wordOfRenewalUsedThisCombat = true;
        var before = _player.GetSpellSlots(1);
        _player.RestoreSpellSlot(1);
        var after = _player.GetSpellSlots(1);
        PushCombatLog($"Word of Renewal — L1 spell slots restored ({before} -> {after}/{_player.GetSpellSlotsMax(1)}).");
        _gameState = GameState.Combat;
    }

    private void DoLucky()
    {
        if (_player == null) return;
        _luckyUsedThisCombat = true;
        _luckyPrimed = true;
        PushCombatLog("Lucky — fate bends! Next attack has Advantage (roll twice, take higher).");
        _gameState = GameState.Combat;
    }

    private void DoMetamagic()
    {
        if (_player == null) return;
        _metamagicUsedThisCombat = true;
        _metamagicPrimed = true;
        PushCombatLog("Metamagic Adept (Heightened Spell) — next spell's save is made with Disadvantage!");
        _gameState = GameState.Combat;
    }

    private void DoSharpshooter()
    {
        if (_player == null) return;
        _sharpshooterUsedThisCombat = true;
        _sharpshooterPrimed = true;
        PushCombatLog("Sharpshooter — precision aim! Next attack ignores all armor and deals +5 damage.");
        _gameState = GameState.Combat;
    }

    private void DoRecklessAttack()
    {
        if (_player == null) return;
        _recklessAttackUsedThisCombat = true;
        _playerAttackAdvantage = true;
        _enemyNextAttackAdvantage = true;
        PushCombatLog("Reckless Attack — all in! Advantage on your next attack, but the enemy gains Advantage on theirs.");
        _gameState = GameState.Combat;
    }

    private void DoOverchannel()
    {
        if (_player == null) return;
        _overchannelUsedThisCombat = true;
        _overchannelPrimed = true;
        PushCombatLog("Overchannel — raw power surges through you! Next spell maximizes all damage dice (but costs 1d6 HP).");
        _gameState = GameState.Combat;
    }

    private void DoSpiritualWeapon()
    {
        if (_player == null) return;
        _spiritualWeaponUsedThisCombat = true;
        _spiritualWeaponPrimed = true;
        PushCombatLog("Spiritual Weapon — a spectral weapon materializes! After your next spell, it strikes.");
        _gameState = GameState.Combat;
    }

    private void ResolveSummonAutoAttack()
    {
        if (_activeSummon == null || _player == null || !_activeSummon.IsAlive)
            return;
        if (_activeSummon.Type.Behavior != SummonBehaviorKind.AutoAttack)
            return;
        if (_currentEnemy == null || !_currentEnemy.IsAlive)
            return;

        var scaleStat = SpellData.ById.TryGetValue(_activeSummon.Type.SourceSpellId, out var spellDef)
            ? spellDef.ScalingStat
            : StatName.Wisdom;
        var statMod = _activeSummon.Type.UseCasterStatMod ? _player.Mod(scaleStat) : 0;
        var atkBonus = statMod + _activeSummon.Type.AttackBonus + GetProficiencyBonus();
        var atkBonusStr = atkBonus >= 0 ? $"+{atkBonus}" : $"{atkBonus}";

        var (hitResult, d20, rollTotal) = CombatMath.RollAttack(atkBonus, _currentEnemy.Type.ArmorClass, _rng);

        if (hitResult != AttackRollResult.Miss)
        {
            var dice = 0;
            for (var di = 0; di < _activeSummon.Type.DamageCount; di++)
                dice += _rng.Next(1, _activeSummon.Type.DamageDice + 1);
            var dmg = Math.Max(1, dice + Math.Max(0, statMod) + _activeSummon.Type.DamageBonus);
            _currentEnemy.CurrentHp = Math.Max(0, _currentEnemy.CurrentHp - dmg);
            PushCombatLog($"{_activeSummon.Type.Name} strikes! d20{atkBonusStr}={rollTotal} hits for {dmg} {_activeSummon.Type.DamageType}.");
            PushCombatLog($"{_currentEnemy.Type.Name} HP {_currentEnemy.CurrentHp}/{_currentEnemy.Type.MaxHp}.");
            CheckEnemyDeath();
        }
        else
        {
            PushCombatLog($"{_activeSummon.Type.Name} misses! d20{atkBonusStr}={rollTotal} vs AC {_currentEnemy.Type.ArmorClass}.");
        }
    }

    private void DoBardicInspiration()
    {
        if (_player == null) return;
        _bardicInspirationUsedThisCombat = true;
        _bardicInspirationPrimed = true;
        _bardicInspirationForAttack = true;  // default: apply to next attack
        PushCombatLog("Bardic Inspiration — your own melody steadies you! +1d6 to your next attack roll.");
        _gameState = GameState.Combat;
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
            case "healing_draught":
            {
                // Enemies do not use healing draughts in combat; kept as droppable loot only.
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

    private void DoEnemyAttack(int firstEnemyDamageAbsorb = 0, bool skipFirstEnemyTurn = false, string? suppressedEnemyCombatantId = null, int attackRollPenalty = 0)
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
        ResolveEnemyTurnsUntilPlayerTurn(firstEnemyDamageAbsorb, skipFirstEnemyTurn, suppressedEnemyCombatantId, attackRollPenalty);
    }

    private void ResolveEnemyTurnsUntilPlayerTurn(
        int firstEnemyDamageAbsorb = 0,
        bool skipFirstEnemyTurn = false,
        string? suppressedEnemyCombatantId = null,
        int attackRollPenalty = 0)
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
        var pendingAttackRollPenalty = Math.Max(0, attackRollPenalty);
        var skipNextEnemyTurn = skipFirstEnemyTurn;
        var pendingSuppressedCombatantId = suppressedEnemyCombatantId;
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

            var skipSpecificEnemyTurn = !string.IsNullOrWhiteSpace(pendingSuppressedCombatantId) &&
                string.Equals(slot.Id, pendingSuppressedCombatantId, StringComparison.Ordinal);
            if (skipNextEnemyTurn || skipSpecificEnemyTurn)
            {
                skipNextEnemyTurn = false;
                pendingSuppressedCombatantId = skipSpecificEnemyTurn ? null : pendingSuppressedCombatantId;
                if (!ResolveEnemyStartOfTurnHazards(actingEnemy))
                {
                    ResolveEncounterEnemyDeathsImmediate();
                    return;
                }

                if (!ResolveEnemyStartOfTurnStatuses(actingEnemy))
                {
                    ResolveEncounterEnemyDeathsImmediate();
                    return;
                }

                PushCombatLog($"{actingEnemy.Type.Name} loses the turn.");
                AdvanceEnemyStatusDurations(actingEnemy);
            }
            else
            {
                ExecuteEnemyTurn(actingEnemy, pendingDamageAbsorb, pendingAttackRollPenalty);
                pendingDamageAbsorb = 0;
                pendingAttackRollPenalty = 0;

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

    private void ExecuteEnemyTurn(Enemy enemy, int damageAbsorb, int attackRollPenalty = 0)
    {
        if (_player == null || !enemy.IsAlive)
        {
            return;
        }

        if (!ResolveEnemyStartOfTurnHazards(enemy))
        {
            ResolveEncounterEnemyDeathsImmediate();
            return;
        }

        if (!ResolveEnemyStartOfTurnStatuses(enemy))
        {
            ResolveEncounterEnemyDeathsImmediate();
            return;
        }

        if (CombatStatusRules.PreventsEnemyAction(enemy.StatusEffects))
        {
            PushCombatLog($"{enemy.Type.Name} is stunned and loses the turn.");
            AdvanceEnemyStatusDurations(enemy);
            return;
        }

        if (CombatStatusRules.ForcesEnemyRetreat(enemy.StatusEffects) && TryExecuteEnemyRetreatMovement(enemy))
        {
            AdvanceEnemyStatusDurations(enemy);
            return;
        }

        if (TryEnemyUseCombatLoot(enemy))
        {
            AdvanceEnemyStatusDurations(enemy);
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
            if (!enemy.IsAlive)
            {
                ResolveEncounterEnemyDeathsImmediate();
                return;
            }
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
            if (!moved && CombatStatusRules.PreventsEnemyMovement(enemy.StatusEffects))
            {
                PushCombatLog($"{enemy.Type.Name} is rooted and cannot reposition.");
                AdvanceEnemyStatusDurations(enemy);
                return;
            }

            var reason = attackDecision.Validation.BuildBlockedReason();
            var prefix = moved
                ? $"{enemy.Type.Name} still cannot get a clear attack"
                : $"{enemy.Type.Name} cannot get a clear attack";
            PushCombatLog($"{prefix} ({reason}).");
            AdvanceEnemyStatusDurations(enemy);
            return;
        }

        // --- D&D Attack Roll ---
        var enemyLootAttackBonus = _enemyLootKits.TryGetValue(enemy, out var enemyLoot_) ? enemyLoot_.AttackBonus : 0;
        var statusAttackPenalty = CombatStatusRules.GetAttackPenalty(enemy.StatusEffects);
        var totalEnemyAttackBonus = enemy.Type.AttackBonus + enemyLootAttackBonus + _phase3EnemyAttackBonus
            - statusAttackPenalty - attackRollPenalty;

        // Countercharm: bard reduces enemy attack bonus by 1d4
        if (_countercharmAvailable && _player != null && _player.HasFeat("bard_countercharm_feat"))
        {
            _countercharmAvailable = false;
            var disruptRoll = _rng.Next(1, 5);
            totalEnemyAttackBonus -= disruptRoll;
            PushCombatLog($"Countercharm — your performance disrupts their attack roll by {disruptRoll}!");
        }

        // Hex: enemy attack penalty
        if (_hexActive)
            totalEnemyAttackBonus -= 2;

        var playerAC = _activeTransformation != null
            ? _activeTransformation.Form.FormAC
            : GetPlayerArmorClass();

        // Barkskin: AC floor 16
        if (_barkskinActive && _activeTransformation == null)
            playerAC = Math.Max(playerAC, 16);

        // Defensive Duelist: add DEX mod to AC before this attack resolves (reaction)
        if (_defensiveDuelistAvailable && _player != null && _player.HasFeat("defensive_duelist_feat"))
        {
            _defensiveDuelistAvailable = false;
            var dexBonus = Math.Max(0, _player.Mod(StatName.Dexterity));
            playerAC += dexBonus;
            PushCombatLog($"Defensive Duelist — +{dexBonus} DEX raises your AC to {playerAC}!");
        }

        // Consume Crusher/Reckless Attack enemy advantage/disadvantage flags
        var enemyHasAdv = _enemyNextAttackAdvantage;
        var enemyHasDisadv = _enemyNextAttackDisadvantage;
        _enemyNextAttackAdvantage = false;
        _enemyNextAttackDisadvantage = false;

        // Blur: enemy attacks always have disadvantage while active
        if (_blurActive && _activeTransformation == null)
            enemyHasDisadv = true;
        // Greater Invisibility: enemy attacks with disadvantage
        if (_greaterInvisibilityActive)
            enemyHasDisadv = true;
        // Fog Cloud: enemies in the hazard zone take -3 to attack rolls
        foreach (var fogHazard in _activeCombatHazards)
        {
            if (string.Equals(fogHazard.SourceSpellId, "ranger_fog_cloud", StringComparison.Ordinal) && IsEnemyInsideHazard(fogHazard, enemy))
            {
                totalEnemyAttackBonus -= 3;
                PushCombatLog($"Fog Cloud obscures {enemy.Type.Name}'s aim (−3 attack).");
                break;
            }
        }
        if (enemyHasAdv) PushCombatLog($"{enemy.Type.Name} has Advantage on this attack!");
        if (enemyHasDisadv) PushCombatLog($"{enemy.Type.Name} is staggered — Disadvantage on this attack!");

        // Invisibility: 15% flat miss chance before the attack resolves
        if (_invisibilityActive && _rng.Next(100) < 15)
        {
            PushCombatLog($"Invisibility — {enemy.Type.Name} can't pin down your position and misses!");
            AdvanceEnemyStatusDurations(enemy);
            return;
        }

        // Sanctuary: enemy must pass WIS save or skip attack entirely
        if (_sanctuaryActive && _player != null)
        {
            var sanctuaryDC = 8 + Math.Max(0, _player.Mod(StatName.Wisdom)) + _player.ProficiencyBonus;
            var enemySave = _rng.Next(1, 21) + enemy.Type.AttackBonus / 2;
            if (enemySave < sanctuaryDC)
            {
                PushCombatLog($"Sanctuary — {enemy.Type.Name} fails WIS save ({enemySave} vs DC {sanctuaryDC}) and cannot attack!");
                AdvanceEnemyStatusDurations(enemy);
                return;
            }
            PushCombatLog($"Sanctuary — {enemy.Type.Name} breaks through (save {enemySave} vs DC {sanctuaryDC}).");
        }

        var (enemyHitResult, enemyD20, enemyRollTotal) = CombatMath.RollAttack(totalEnemyAttackBonus, playerAC, _rng,
            advantage: enemyHasAdv, disadvantage: enemyHasDisadv);

        // Mark that at least one enemy has acted — used by Assassin feat
        _enemyHasActedThisCombat = true;

        if (enemyHitResult == AttackRollResult.Miss)
        {
            PushCombatLog($"{enemy.Type.Name} misses! d20:{enemyD20}+{totalEnemyAttackBonus}={enemyRollTotal} vs AC {playerAC}.");

            // Sentinel: counter-attack when enemy misses (opportunity attack on failed attack)
            if (_sentinelAvailable && _player != null && _player.HasFeat("sentinel_feat") && _player.IsAlive)
            {
                _sentinelAvailable = false;
                var (counterDamage, counterCrit, _, _, _) = CalcPlayerDamage();
                enemy.CurrentHp = Math.Max(0, enemy.CurrentHp - counterDamage);
                var counterCritTag = counterCrit ? " CRIT!" : string.Empty;
                PushCombatLog($"Sentinel — you exploit the opening for {counterDamage}!{counterCritTag}");
                PushCombatLog($"{enemy.Type.Name} HP {enemy.CurrentHp}/{enemy.Type.MaxHp}.");
                if (!enemy.IsAlive) { ResolveEncounterEnemyDeathsImmediate(); return; }
            }

            AdvanceEnemyStatusDurations(enemy);
            return;
        }

        // Mirror Image: probabilistic hit absorption
        if (_mirrorImageCharges > 0)
        {
            var mirrorChance = _mirrorImageCharges * 25;
            if (_rng.Next(100) < mirrorChance)
            {
                _mirrorImageCharges--;
                PushCombatLog($"Mirror Image — a duplicate shatters! ({_mirrorImageCharges} remaining)");
                if (_mirrorImageCharges == 0)
                    PushCombatLog("All mirror images have been destroyed.");
                AdvanceEnemyStatusDurations(enemy);
                return;
            }
        }

        // Blink: 30% chance attack misses while phased out
        if (_blinkActive && _rng.Next(100) < 30)
        {
            PushCombatLog($"Blink — you phase out! {enemy.Type.Name}'s attack passes through harmlessly.");
            AdvanceEnemyStatusDurations(enemy);
            return;
        }

        // Major Image: 25% chance attack targets the decoy instead
        if (_majorImageActive && _rng.Next(100) < 25)
        {
            PushCombatLog($"Major Image — the illusion absorbs {enemy.Type.Name}'s attack!");
            AdvanceEnemyStatusDurations(enemy);
            return;
        }


        // Shield Expert: once per combat — downgrade one enemy crit to a normal hit
        if (enemyHitResult == AttackRollResult.CriticalHit && _shieldExpertAvailable && _player != null && _player.HasFeat("shield_expert_feat"))
        {
            _shieldExpertAvailable = false;
            enemyHitResult = AttackRollResult.Hit;
            PushCombatLog("Shield Expert — you absorb the critical blow! Downgraded to a normal hit.");
        }

        // Hit or critical — roll damage
        var enemyIsCrit = enemyHitResult == AttackRollResult.CriticalHit;
        var enemyDiceCount = enemy.Type.AttackDiceCount * (enemyIsCrit ? 2 : 1);
        int enemyDiceTotal = 0;
        for (int i = 0; i < enemyDiceCount; i++)
            enemyDiceTotal += _rng.Next(1, Math.Max(2, enemy.Type.DamageDice + 1));
        var critLabel = enemyIsCrit ? " CRITICAL HIT!" : string.Empty;
        PushCombatLog($"{enemy.Type.Name} hits!{critLabel} d20:{enemyD20}+{totalEnemyAttackBonus}={enemyRollTotal} vs AC {playerAC}.");
        PushCombatLog($"Damage: {enemyDiceCount}d{enemy.Type.DamageDice} ({enemyDiceTotal}) + {enemy.Type.DamageBonus} bonus.");

        var clampedAbsorb = Math.Max(0, damageAbsorb);
        var damage = Math.Max(1, enemyDiceTotal + enemy.Type.DamageBonus);
        if (clampedAbsorb > 0)
            damage = Math.Max(0, damage - clampedAbsorb);

        // Uncanny Dodge: once per combat, halve one incoming hit (D&D: reaction to halve damage)
        if (_uncannyDodgeAvailable && _player != null && _player.HasFeat("rogue_uncanny_dodge_feat") && damage > 0)
        {
            _uncannyDodgeAvailable = false;
            damage = Math.Max(1, damage / 2);
            PushCombatLog($"Uncanny Dodge — you deflect half the blow! Damage halved to {damage}.");
        }

        // Indomitable: once per combat, reduce hit by player level (D&D: reroll failed save adding class level)
        if (_indomitableAvailable && _player != null && _player.HasFeat("warrior_indomitable_feat") && damage > 0)
        {
            _indomitableAvailable = false;
            var levelReduction = _player.Level;
            damage = Math.Max(0, damage - levelReduction);
            PushCombatLog($"Indomitable — warrior's will reduces {levelReduction} damage!");
        }

        if (_player == null) return;

        // Capture pre-reduction damage for reactive trigger
        var originalEnemyDamage = damage;

        // Counterspell: one-use damage halving (any source)
        if (_counterspellPrimed && damage > 0)
        {
            _counterspellPrimed = false;
            damage = Math.Max(0, damage / 2);
            PushCombatLog($"Counterspell — the magical ward halves the blow! ({damage} damage taken).");
        }

        // Protection from Energy: half damage if matching element (approximated via damage tag keyword)
        if (_protEnergyActive && !string.IsNullOrWhiteSpace(_protEnergyElement) && damage > 0)
        {
            // Compare by checking if enemy damage tag contains the element keyword
            var enemyDmgTag = enemy.Type.DamageType ?? string.Empty;
            var matchesElement = enemyDmgTag.Contains(_protEnergyElement, StringComparison.OrdinalIgnoreCase);
            if (matchesElement)
            {
                damage = Math.Max(0, damage / 2);
                PushCombatLog($"Protection from Energy absorbs half the {_protEnergyElement} damage.");
            }
        }

        // Stoneskin: flat −3 damage reduction
        if (_stoneskinActive && damage > 0)
        {
            var reduced = Math.Min(damage, 3);
            damage -= reduced;
            PushCombatLog($"Stoneskin absorbs {reduced} damage.");
        }

        // Cutting Words: one-use 1d8 damage reduction (consumed)
        if (_cuttingWordsPrimed && damage > 0)
        {
            _cuttingWordsPrimed = false;
            var cutReduction = _rng.Next(1, 9);
            var actualCut = Math.Min(damage, cutReduction);
            damage -= actualCut;
            PushCombatLog($"Cutting Words — you mock the enemy, reducing damage by {actualCut}!");
        }

        // Transformation temp HP absorbs damage before real HP
        if (_activeTransformation != null)
        {
            var dmgAfterResist = damage;
            if (_activeTransformation.Form.Special == FormSpecialKind.DamageResist)
                dmgAfterResist = Math.Max(0, damage - _activeTransformation.Form.SpecialValue);
            if (_activeTransformation.Form.Id == "form_mist")
                dmgAfterResist = Math.Max(0, dmgAfterResist - 3);

            var tempLost = Math.Min(_activeTransformation.TempHpRemaining, dmgAfterResist);
            _activeTransformation.TempHpRemaining -= tempLost;
            PushCombatLog($"Form absorbs {tempLost} damage ({_activeTransformation.TempHpRemaining} temp HP left).");

            if (_activeTransformation.TempHpRemaining <= 0)
            {
                RevertTransformation("Your form shatters!");
                EndActiveConcentration("Transformation broken by damage.");
            }

            if (clampedAbsorb > 0)
                PushCombatLog($"Mana Shield absorbs {clampedAbsorb}.");
            PushCombatLog($"Your HP {_player.CurrentHp}/{_player.MaxHp}.");
            TryRollDungeonConditionFromEnemyHit(damage);
        }
        else
        {
            // Armor of Agathys: separate frost temp HP pool (absorbs first)
            if (_armorOfAgathysTempHp > 0 && damage > 0)
            {
                var agathysAbsorbed = Math.Min(_armorOfAgathysTempHp, damage);
                _armorOfAgathysTempHp -= agathysAbsorbed;
                damage -= agathysAbsorbed;
                PushCombatLog($"Armor of Agathys absorbs {agathysAbsorbed} damage ({_armorOfAgathysTempHp} frost HP remaining).");
            }

            // Temp HP absorption (False Life, Heroism) before real HP
            if (_playerTempHp > 0 && damage > 0)
            {
                var absorbed = Math.Min(_playerTempHp, damage);
                _playerTempHp -= absorbed;
                damage -= absorbed;
                if (absorbed > 0)
                    PushCombatLog($"Temp HP absorbs {absorbed} damage ({_playerTempHp} remaining).");
            }

            _player.CurrentHp = Math.Max(0, _player.CurrentHp - damage);

            // Death Ward: prevent lethal damage — set HP to 1 instead
            if (_deathWardActive && _player.CurrentHp <= 0)
            {
                _player.CurrentHp = 1;
                _deathWardActive = false;
                PushCombatLog("Death Ward triggers — you cling to life at 1 HP!");
            }

            if (clampedAbsorb > 0)
            {
                PushCombatLog($"Mana Shield absorbs {clampedAbsorb}; you take {damage}.");
            }

            PushCombatLog($"Your HP {_player.CurrentHp}/{_player.MaxHp}.");
            TryResolveConcentrationAfterDamage(damage, enemy.Type.Name);
            TryRollDungeonConditionFromEnemyHit(damage);
        }

        // Goblin Slinger: 30% chance to poison on a hit
        if (damage > 0 && string.Equals(enemy.Type.Name, "Goblin Slinger", StringComparison.Ordinal) && _rng.Next(100) < 30)
        {
            ApplyPlayerCondition(PlayerConditionKind.Poisoned, potency: 2, duration: 3);
            PushCombatLog("The dart was coated with poison! Poisoned for 3 turns (−2 HP/turn).");
        }

        // Riposte: once per combat — counter-attack for half weapon damage when hit
        if (_riposteAvailable && _player.HasFeat("riposte_feat") && _player.IsAlive && damage > 0 && enemy.IsAlive)
        {
            _riposteAvailable = false;
            var (riposteDmg, _, _, _, _) = CalcPlayerDamage();
            var halfRiposte = Math.Max(1, riposteDmg / 2);
            enemy.CurrentHp = Math.Max(0, enemy.CurrentHp - halfRiposte);
            PushCombatLog($"Riposte! You strike back for {halfRiposte} damage!");
            PushCombatLog($"{enemy.Type.Name} HP {enemy.CurrentHp}/{enemy.Type.MaxHp}.");
            if (!enemy.IsAlive) { ResolveEncounterEnemyDeathsImmediate(); return; }
        }

        // ── Reactive Damage: spells that punish attackers ──
        if (enemy.IsAlive && originalEnemyDamage > 0)
        {
            var reactiveDamage = 0;

            // Hellish Rebuke (primed, consumed)
            if (_hellishRebukePrimed)
            {
                _hellishRebukePrimed = false;
                var rebukeRoll = _rng.Next(1, 7) + _rng.Next(1, 7);
                reactiveDamage += rebukeRoll;
                PushCombatLog($"Hellish Rebuke — flames engulf the attacker for {rebukeRoll} fire damage!");
            }

            // Wrath of the Storm (primed, consumed)
            if (_wrathOfStormPrimed)
            {
                _wrathOfStormPrimed = false;
                var wrathRoll = _rng.Next(1, 9) + _rng.Next(1, 9);
                reactiveDamage += wrathRoll;
                PushCombatLog($"Wrath of the Storm — lightning strikes back for {wrathRoll} damage!");
            }

            // Holy Rebuke (primed, consumed — also heals player 1d4)
            if (_holyRebukePrimed && _player != null)
            {
                _holyRebukePrimed = false;
                var holyRoll = _rng.Next(1, 7) + _rng.Next(1, 7);
                reactiveDamage += holyRoll;
                var healRoll = _rng.Next(1, 5);
                _player.CurrentHp = Math.Min(_player.MaxHp, _player.CurrentHp + healRoll);
                PushCombatLog($"Holy Rebuke — radiant wrath for {holyRoll} + you heal {healRoll} HP!");
            }

            // Armor of Agathys (persistent while frost temp HP > 0)
            if (_armorOfAgathysTempHp > 0)
            {
                var coldRoll = _rng.Next(1, 9);
                reactiveDamage += coldRoll;
                PushCombatLog($"Armor of Agathys — frost bites the attacker for {coldRoll} cold damage!");
            }

            // Fire Shield (persistent, no concentration)
            if (_fireShieldActive)
            {
                var fireRoll = _rng.Next(1, 9) + _rng.Next(1, 9);
                reactiveDamage += fireRoll;
                PushCombatLog($"Fire Shield — flames sear the attacker for {fireRoll} fire damage!");
            }

            // Thorns (persistent, concentration)
            if (_thornsActive)
            {
                var thornRoll = _rng.Next(1, 7);
                reactiveDamage += thornRoll;
                PushCombatLog($"Thorns — vines lash back for {thornRoll} piercing damage!");
            }

            // Spirit Shroud (persistent, concentration)
            if (_spiritShroudActive)
            {
                var shroudRoll = _rng.Next(1, 7);
                reactiveDamage += shroudRoll;
                PushCombatLog($"Spirit Shroud — vengeful spirits strike for {shroudRoll} radiant damage!");
            }

            // Apply total reactive damage
            if (reactiveDamage > 0)
            {
                enemy.CurrentHp = Math.Max(0, enemy.CurrentHp - reactiveDamage);
                PushCombatLog($"{enemy.Type.Name} HP {enemy.CurrentHp}/{enemy.Type.MaxHp} after reactive damage.");
                if (!enemy.IsAlive) { ResolveEncounterEnemyDeathsImmediate(); return; }
            }
        }

        AdvanceEnemyStatusDurations(enemy);
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
            BeginLevelUpFlow();
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
        _warCryAvailable = false;
        ResetEncounterContext();
        PushCombatLog("You have been defeated...");
        _gameState = GameState.DeathScreen;
    }

    private void HandleLevelUpInput()
    {
        if (_player == null) return;

        var levelUpRowCount = StatOrder.Length + 3;
        if (PressedOrRepeat(KeyUp))
        {
            _selectedStatIndex = (_selectedStatIndex - 1 + levelUpRowCount) % levelUpRowCount;
            return;
        }

        if (PressedOrRepeat(KeyDown))
        {
            _selectedStatIndex = (_selectedStatIndex + 1) % levelUpRowCount;
            return;
        }

        if (Pressed(KeyEscape))
        {
            _selectionMessage = TryUndoLastLevelUpStatPoint()
                ? "Last stat point refunded."
                : "No level-up point to undo.";
            return;
        }

        if (Pressed(KeyLeft))
        {
            if (_selectedStatIndex < StatOrder.Length)
            {
                var chosenStat = StatOrder[_selectedStatIndex];
                _selectionMessage = TryDecreaseSelectedLevelUpStat()
                    ? $"{chosenStat} refunded."
                    : "That stat has no level-up point assigned.";
            }
            return;
        }

        if (Pressed(KeyRight))
        {
            if (_selectedStatIndex < StatOrder.Length)
            {
                var chosenStat = StatOrder[_selectedStatIndex];
                _selectionMessage = TryIncreaseSelectedLevelUpStat()
                    ? $"{chosenStat} increased."
                    : "No stat points left.";
            }
            return;
        }

        if (!Pressed(KeyEnter)) return;

        if (_selectedStatIndex < StatOrder.Length)
        {
            var chosenStat = StatOrder[_selectedStatIndex];
            _selectionMessage = TryIncreaseSelectedLevelUpStat()
                ? $"{chosenStat} increased."
                : "No stat points left.";
            return;
        }

        if (_selectedStatIndex == StatOrder.Length)
        {
            _selectionMessage = TryUndoLastLevelUpStatPoint()
                ? "Last stat point refunded."
                : "No level-up point to undo.";
            return;
        }

        if (_selectedStatIndex == StatOrder.Length + 1)
        {
            _selectionMessage = ResetLevelUpStatAllocations()
                ? "Level-up stat allocation reset."
                : "Nothing to reset.";
            return;
        }

        if (_player.StatPoints > 0)
        {
            _selectionMessage = $"Spend all stat points first ({_player.StatPoints} remaining).";
            return;
        }

        _selectionMessage = "Stat allocation locked. Moving to feat/spell/skill picks.";
        _levelUpSessionActive = false;
        PreparePostLevelUpChoices();
    }

    private void BeginLevelUpFlow()
    {
        Array.Clear(_levelUpAllocatedStats, 0, _levelUpAllocatedStats.Length);
        _levelUpStatAllocationOrder.Clear();
        _selectedStatIndex = 0;
        _selectionMessage = "Allocate stats, then continue when ready.";
        _levelUpSessionActive = true;
        _gameState = GameState.LevelUp;
    }

    private bool TryIncreaseSelectedLevelUpStat()
    {
        if (_player == null || _selectedStatIndex < 0 || _selectedStatIndex >= StatOrder.Length)
        {
            return false;
        }

        var stat = StatOrder[_selectedStatIndex];
        if (!_player.IncreaseStat(stat))
        {
            return false;
        }

        _levelUpAllocatedStats[_selectedStatIndex] += 1;
        _levelUpStatAllocationOrder.Add(_selectedStatIndex);
        return true;
    }

    private bool TryDecreaseSelectedLevelUpStat()
    {
        if (_player == null || _selectedStatIndex < 0 || _selectedStatIndex >= StatOrder.Length)
        {
            return false;
        }

        if (_levelUpAllocatedStats[_selectedStatIndex] <= 0)
        {
            return false;
        }

        var stat = StatOrder[_selectedStatIndex];
        if (!_player.RefundAllocatedStatPoint(stat))
        {
            return false;
        }

        _levelUpAllocatedStats[_selectedStatIndex] -= 1;
        for (var i = _levelUpStatAllocationOrder.Count - 1; i >= 0; i--)
        {
            if (_levelUpStatAllocationOrder[i] != _selectedStatIndex)
            {
                continue;
            }

            _levelUpStatAllocationOrder.RemoveAt(i);
            break;
        }

        return true;
    }

    private bool TryUndoLastLevelUpStatPoint()
    {
        if (_levelUpStatAllocationOrder.Count == 0)
        {
            return false;
        }

        _selectedStatIndex = _levelUpStatAllocationOrder[^1];
        return TryDecreaseSelectedLevelUpStat();
    }

    private bool ResetLevelUpStatAllocations()
    {
        if (_levelUpStatAllocationOrder.Count == 0)
        {
            return false;
        }

        while (_levelUpStatAllocationOrder.Count > 0)
        {
            if (!TryUndoLastLevelUpStatPoint())
            {
                break;
            }
        }

        return true;
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

        if (PressedOrRepeat(KeyUp))
        {
            _selectedSpellLearnIndex = (_selectedSpellLearnIndex - 1 + _spellLearnChoices.Count) % _spellLearnChoices.Count;
            EnsureSpellLearnSelectionVisible(_spellLearnChoices.Count);
            return;
        }

        if (PressedOrRepeat(KeyDown))
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

        // Auto-grant class talent(s) for current level
        var beforeCount = _player.Skills.Count;
        _player.GrantClassTalentsForLevel(_player.Level);
        var newTalents = _player.Skills.Skip(beforeCount).ToList();

        if (newTalents.Count == 0)
        {
            EnterPlayingState("levelup_complete");
            return;
        }

        // Show notification screen listing what was granted
        _skillChoices.Clear();
        _skillChoices.AddRange(newTalents);
        _selectionMessage = string.Empty;
        _gameState = GameState.SkillSelection;
    }

    private void HandleSkillSelectionInput()
    {
        if (_player == null || !Pressed(KeyEnter)) return;
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
                EnsurePauseSaveSelectionVisible();
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
                    EnsurePauseLoadSelectionVisible();
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
        _pauseMenuIndex = Math.Clamp(_pauseMenuIndex, 0, Math.Max(0, optionCount - 1));
        EnsurePauseInventorySelectionVisible();

        if (Pressed(KeyEscape))
        {
            BackToPauseRoot(1);
            return;
        }

        if (PressedOrRepeat(KeyUp))
        {
            _pauseMenuIndex = (_pauseMenuIndex - 1 + optionCount) % optionCount;
            EnsurePauseInventorySelectionVisible();
            return;
        }

        if (PressedOrRepeat(KeyDown))
        {
            _pauseMenuIndex = (_pauseMenuIndex + 1) % optionCount;
            EnsurePauseInventorySelectionVisible();
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
        _pauseMenuIndex = Math.Clamp(_pauseMenuIndex, 0, Math.Max(0, optionCount - 1));
        EnsurePauseSaveSelectionVisible();

        if (Pressed(KeyEscape))
        {
            BackToPauseRoot(2);
            return;
        }

        if (PressedOrRepeat(KeyUp))
        {
            _pauseMenuIndex = (_pauseMenuIndex - 1 + optionCount) % optionCount;
            EnsurePauseSaveSelectionVisible();
            return;
        }

        if (PressedOrRepeat(KeyDown))
        {
            _pauseMenuIndex = (_pauseMenuIndex + 1) % optionCount;
            EnsurePauseSaveSelectionVisible();
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
        EnsurePauseSaveSelectionVisible();
    }

    private void HandlePauseLoadInput()
    {
        var optionCount = _pauseLoadEntries.Count + 1; // + Back
        _pauseMenuIndex = Math.Clamp(_pauseMenuIndex, 0, Math.Max(0, optionCount - 1));
        EnsurePauseLoadSelectionVisible();

        if (Pressed(KeyEscape))
        {
            BackToPauseRoot(3);
            return;
        }

        if (PressedOrRepeat(KeyUp))
        {
            _pauseMenuIndex = (_pauseMenuIndex - 1 + optionCount) % optionCount;
            EnsurePauseLoadSelectionVisible();
            return;
        }

        if (PressedOrRepeat(KeyDown))
        {
            _pauseMenuIndex = (_pauseMenuIndex + 1) % optionCount;
            EnsurePauseLoadSelectionVisible();
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
                case "healing_draught":
                {
                    if (_player.CurrentHp >= _player.MaxHp)
                    {
                        _pauseMessage = "HP is already full.";
                        return;
                    }

                    var restoreAmount = Math.Max(4, (int)Math.Ceiling(_player.MaxHp * 0.35));
                    var before = _player.CurrentHp;
                    _player.CurrentHp = Math.Min(_player.MaxHp, _player.CurrentHp + restoreAmount);
                    var gained = _player.CurrentHp - before;
                    if (gained <= 0)
                    {
                        _pauseMessage = "Healing Draught had no effect.";
                        return;
                    }

                    item.Quantity -= 1;
                    _pauseMessage = $"Used {item.Name}: HP +{gained} ({item.Quantity} left).";
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
            // leather_jerkin / brigandine_coat / plate_harness: defense handled via GetArmorStateDefenseBonus — no _runDefenseBonus delta
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
            case "iron_greaves":
                _runDefenseBonus = Math.Max(0, _runDefenseBonus + direction);
                return;
            case "thieves_gloves":
                _runMeleeBonus = Math.Max(0, _runMeleeBonus + direction);
                return;
            case "serpent_bracers":
                _runSpellBonus = Math.Max(0, _runSpellBonus + direction);
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

    private int GetPauseListBackY()
    {
        var panelY = UiLayout.PausePanelInsetY;
        var panelH = Raylib.GetScreenHeight() - UiLayout.PausePanelInsetY * 2;
        var reserveBottom = string.IsNullOrWhiteSpace(_pauseMessage) ? 86 : 122;
        return Math.Max(panelY + 120, panelY + panelH - reserveBottom);
    }

    private static int GetPauseVisibleRowCount(int listY, int backY, int rowStep, int rowHeight)
    {
        var count = 0;
        for (var rowY = listY; rowY + rowHeight <= backY - 8; rowY += rowStep)
        {
            count += 1;
        }

        return Math.Max(1, count);
    }

    private int GetPauseInventoryVisibleCount()
    {
        var listY = UiLayout.PausePanelInsetY + 74;
        return GetPauseVisibleRowCount(listY, GetPauseListBackY(), 58, 54);
    }

    private int GetPauseSaveVisibleCount()
    {
        var listY = UiLayout.PausePanelInsetY + 74;
        return GetPauseVisibleRowCount(listY, GetPauseListBackY(), 62, 56);
    }

    private int GetPauseLoadVisibleCount()
    {
        var listY = UiLayout.PausePanelInsetY + 74;
        return GetPauseVisibleRowCount(listY, GetPauseListBackY(), 62, 56);
    }

    private void EnsurePauseInventorySelectionVisible()
    {
        var itemCount = _inventoryItems.Count;
        var visibleCount = GetPauseInventoryVisibleCount();
        if (itemCount <= visibleCount)
        {
            _pauseInventoryOffset = 0;
            return;
        }

        var backIndex = itemCount;
        _pauseMenuIndex = Math.Clamp(_pauseMenuIndex, 0, backIndex);
        if (_pauseMenuIndex == backIndex)
        {
            _pauseInventoryOffset = Math.Max(0, itemCount - visibleCount);
            return;
        }

        if (_pauseMenuIndex < _pauseInventoryOffset)
        {
            _pauseInventoryOffset = _pauseMenuIndex;
        }
        else if (_pauseMenuIndex >= _pauseInventoryOffset + visibleCount)
        {
            _pauseInventoryOffset = _pauseMenuIndex - visibleCount + 1;
        }

        _pauseInventoryOffset = Math.Clamp(_pauseInventoryOffset, 0, Math.Max(0, itemCount - visibleCount));
    }

    private void EnsurePauseSaveSelectionVisible()
    {
        var itemCount = _pauseSaveEntries.Count;
        var visibleCount = GetPauseSaveVisibleCount();
        if (itemCount <= visibleCount)
        {
            _pauseSaveOffset = 0;
            return;
        }

        var backIndex = itemCount;
        _pauseMenuIndex = Math.Clamp(_pauseMenuIndex, 0, backIndex);
        if (_pauseMenuIndex == backIndex)
        {
            _pauseSaveOffset = Math.Max(0, itemCount - visibleCount);
            return;
        }

        if (_pauseMenuIndex < _pauseSaveOffset)
        {
            _pauseSaveOffset = _pauseMenuIndex;
        }
        else if (_pauseMenuIndex >= _pauseSaveOffset + visibleCount)
        {
            _pauseSaveOffset = _pauseMenuIndex - visibleCount + 1;
        }

        _pauseSaveOffset = Math.Clamp(_pauseSaveOffset, 0, Math.Max(0, itemCount - visibleCount));
    }

    private void EnsurePauseLoadSelectionVisible()
    {
        var itemCount = _pauseLoadEntries.Count;
        var visibleCount = GetPauseLoadVisibleCount();
        if (itemCount <= visibleCount)
        {
            _pauseLoadOffset = 0;
            return;
        }

        var backIndex = itemCount;
        _pauseMenuIndex = Math.Clamp(_pauseMenuIndex, 0, backIndex);
        if (_pauseMenuIndex == backIndex)
        {
            _pauseLoadOffset = Math.Max(0, itemCount - visibleCount);
            return;
        }

        if (_pauseMenuIndex < _pauseLoadOffset)
        {
            _pauseLoadOffset = _pauseMenuIndex;
        }
        else if (_pauseMenuIndex >= _pauseLoadOffset + visibleCount)
        {
            _pauseLoadOffset = _pauseMenuIndex - visibleCount + 1;
        }

        _pauseLoadOffset = Math.Clamp(_pauseLoadOffset, 0, Math.Max(0, itemCount - visibleCount));
    }

    private void OpenPauseInventoryMenu()
    {
        _pauseMenuView = PauseMenuView.Inventory;
        _pauseMenuIndex = 0;
        _pauseInventoryOffset = 0;
        _pauseMessage = "Left panel shows equipped slots. Select backpack gear on the right and press ENTER to equip or unequip it.";
        EnsurePauseInventorySelectionVisible();
        ResetPauseConfirm();
    }

    private void OpenPauseSaveMenu()
    {
        RefreshPauseSaveEntries();
        _pauseMenuView = PauseMenuView.Save;
        _pauseMenuIndex = 0;
        _pauseSaveOffset = 0;
        _pauseMessage = "Choose a slot to save or overwrite.";
        EnsurePauseSaveSelectionVisible();
        ResetPauseConfirm();
    }

    private void OpenPauseLoadMenu()
    {
        RefreshPauseLoadEntries();
        _pauseMenuView = PauseMenuView.Load;
        _pauseMenuIndex = 0;
        _pauseLoadOffset = 0;
        _pauseMessage = _pauseLoadEntries.Count == 0
            ? "No save files found."
            : "Select a save file to load.";
        EnsurePauseLoadSelectionVisible();
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
        _pauseSaveOffset = Math.Clamp(_pauseSaveOffset, 0, Math.Max(0, _pauseSaveEntries.Count - 1));
    }

    private void RefreshPauseLoadEntries()
    {
        _pauseLoadEntries.Clear();
        _pauseLoadEntries.AddRange(SaveStore.GetAvailableLoadEntries());
        _pauseLoadOffset = Math.Clamp(_pauseLoadOffset, 0, Math.Max(0, _pauseLoadEntries.Count - 1));
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
                EnemyAttackBonus = Math.Max(0, enemyLoot?.AttackBonus ?? 0),
                StatusEffects = BuildEnemyStatusSnapshots(enemy)
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
                EnemyAttackBonus = Math.Max(0, currentEnemyLoot?.AttackBonus ?? 0),
                StatusEffects = BuildEnemyStatusSnapshots(_currentEnemy)
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
            EnemyPoisoned = Math.Max(0, _currentEnemy?.StatusEffects
                .Where(status => status.Kind == CombatStatusKind.Poison)
                .Select(status => status.Potency)
                .DefaultIfEmpty(0)
                .Max() ?? 0),
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
            }).ToList(),
            ActiveConcentration = string.IsNullOrWhiteSpace(_activeConcentrationSpellId)
                ? null
                : new ConcentrationSnapshot
                {
                    SpellId = _activeConcentrationSpellId,
                    SpellLabel = _activeConcentrationLabel,
                    RemainingRounds = Math.Max(0, _activeConcentrationRemainingRounds)
                },
            ActiveSummon = _activeSummon != null
                ? new SummonSnapshot
                {
                    SummonTypeId = _activeSummon.Type.Id,
                    CurrentHp = _activeSummon.CurrentHp
                }
                : null,
            ActiveTransformation = _activeTransformation != null
                ? new TransformationSnapshot
                {
                    SourceSpellId = _activeTransformation.SourceSpellId,
                    FormId = _activeTransformation.Form.Id,
                    TempHpRemaining = _activeTransformation.TempHpRemaining,
                    FirstHitPrimed = _activeTransformation.FirstHitPrimed
                }
                : null,
            CombatHazards = BuildCombatHazardSnapshots(),
            PlayerConditions = _playerConditions.Count > 0
                ? _playerConditions.Select(c => new PlayerConditionSaveEntry
                    { Kind = c.Kind.ToString(), Potency = c.Potency, RemainingTurns = c.RemainingTurns }).ToList()
                : null,
            // Batch 1 persistent buff state
            MageArmorActive = _mageArmorActive,
            AidMaxHpBonus = _aidMaxHpBonus,
            PlayerTempHp = _playerTempHp,
            ShieldSpellActive = _shieldSpellActive,
            ShieldSpellTurnsLeft = _shieldSpellTurnsLeft,
            // Batch 2 persistent buff state
            MirrorImageCharges = _mirrorImageCharges,
            AbsorbElementsCharged = _absorbElementsCharged,
            // Batch 3 persistent buff state
            HellishRebukePrimed = _hellishRebukePrimed,
            ArmorOfAgathysTempHp = _armorOfAgathysTempHp,
            FireShieldActive = _fireShieldActive,
            WrathOfStormPrimed = _wrathOfStormPrimed,
            DeathWardActive = _deathWardActive,
            HolyRebukePrimed = _holyRebukePrimed,
            CuttingWordsPrimed = _cuttingWordsPrimed,
            // Batch 4+5 persistent buff state
            CounterspellPrimed = _counterspellPrimed,
            ElementalWeaponActive = _elementalWeaponActive,
            ElementalWeaponElement = _elementalWeaponElement,
            RevivifyUsed = _revivifyUsed,
            ProtEnergyElement = _protEnergyElement
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

        if (!Player.TryFromSnapshot(
                snapshot.Player,
                out var restoredPlayer,
                out var playerError,
                out var removedArchivedSpellCount,
                out var refundedArchivedSpellPicks) || restoredPlayer == null)
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
            RestoreEnemyStatuses(enemy, enemySnapshot);
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
                RestoreEnemyStatuses(restoredCurrentEnemy, snapshot.CurrentEnemy);
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
        if (_currentEnemy != null &&
            snapshot.EnemyPoisoned > 0 &&
            !_currentEnemy.StatusEffects.Any(status => status.Kind == CombatStatusKind.Poison))
        {
            _currentEnemy.StatusEffects.Add(new CombatStatusState
            {
                Kind = CombatStatusKind.Poison,
                Potency = Math.Max(1, snapshot.EnemyPoisoned),
                RemainingTurns = 1,
                SourceSpellId = "legacy_poison",
                SourceLabel = "Legacy Poison"
            });
        }
        _warCryAvailable = snapshot.WarCryAvailable;
        ResetEncounterContext();
        if (_currentEnemy != null)
        {
            BeginEncounterFromSeed(_currentEnemy);
            BeginPlayerCombatTurn();
        }
        if (resumeState == GameState.Combat && snapshot.ActiveConcentration != null)
        {
            _activeConcentrationSpellId = snapshot.ActiveConcentration.SpellId ?? string.Empty;
            _activeConcentrationLabel = string.IsNullOrWhiteSpace(snapshot.ActiveConcentration.SpellLabel)
                ? _activeConcentrationSpellId
                : snapshot.ActiveConcentration.SpellLabel;
            _activeConcentrationRemainingRounds = Math.Max(0, snapshot.ActiveConcentration.RemainingRounds);

            // Reconstruct weapon rider / self-buff state from concentration
            switch (snapshot.ActiveConcentration.SpellId)
            {
                case "paladin_magic_weapon":
                    _magicWeaponActive = true;
                    break;
                case "ranger_flame_arrows":
                    _flameArrowsActive = true;
                    break;
                case "paladin_crusaders_mantle":
                    _crusadersMantleActive = true;
                    break;
                case "ranger_zephyr_strike":
                    _zephyrStrikeActive = true;
                    _zephyrStrikeHitPrimed = true;
                    break;
                case "paladin_divine_favor":
                    _divineFavorActive = true;
                    break;
                case "cleric_shield_of_faith":
                case "paladin_shield_of_faith":
                    _shieldOfFaithActive = true;
                    break;
                case "cleric_bless":
                    _blessActive = true;
                    break;
                case "paladin_heroism":
                case "bard_heroism":
                    _heroismActive = true;
                    break;
                case "ranger_barkskin":
                    _barkskinActive = true;
                    break;
                case "mage_blur":
                    _blurActive = true;
                    break;
                case "mage_haste":
                    _hasteActive = true;
                    break;
                // Batch 2 concentration reconstruction
                case "mage_expeditious_retreat":
                    _expeditiousRetreatActive = true;
                    break;
                case "ranger_longstrider":
                    _longstriderActive = true;
                    break;
                case "bard_hex":
                    _hexActive = true;
                    break;
                case "cleric_protection_evg":
                case "paladin_protection_evg":
                    _protFromEvilActive = true;
                    break;
                case "cleric_sanctuary":
                    _sanctuaryActive = true;
                    break;
                case "paladin_compelled_duel":
                    _compelledDuelActive = true;
                    break;
                case "bard_enhance_ability":
                case "cleric_enhance_ability":
                case "mage_enhance_ability":
                    _enhanceAbilityActive = true;
                    break;
                // Batch 3 concentration reconstruction
                case "cleric_spirit_shroud":
                    _spiritShroudActive = true;
                    break;
                case "ranger_thorns":
                    _thornsActive = true;
                    break;
                case "ranger_stoneskin":
                case "mage_stoneskin":
                    _stoneskinActive = true;
                    break;
                case "bard_greater_invisibility":
                    _greaterInvisibilityActive = true;
                    break;
                // Batch 4+5 concentration reconstruction
                case "bard_invisibility":
                    _invisibilityActive = true;
                    break;
                case "paladin_elemental_weapon":
                    _elementalWeaponActive = true;
                    _elementalWeaponElement = snapshot.ElementalWeaponElement;
                    break;
                case "mage_blink":
                    _blinkActive = true;
                    break;
                case "mage_protection_from_energy":
                    _protEnergyActive = true;
                    _protEnergyElement = !string.IsNullOrWhiteSpace(snapshot.ProtEnergyElement)
                        ? snapshot.ProtEnergyElement
                        : "fire";
                    break;
                case "cleric_beacon_of_hope":
                    _beaconOfHopeActive = true;
                    break;
                case "bard_major_image":
                    _majorImageActive = true;
                    break;
                case "paladin_aura_of_courage":
                    _auraOfCourageActive = true;
                    break;
            }
        }

        // Restore Batch 1 persistent buff state from snapshot
        // Note: _runDefenseBonus is restored from snapshot.RunDefenseBonus which already includes all buff bonuses
        _mageArmorActive = snapshot.MageArmorActive;
        _aidMaxHpBonus = Math.Max(0, snapshot.AidMaxHpBonus);
        _playerTempHp = Math.Max(0, snapshot.PlayerTempHp);
        _shieldSpellActive = snapshot.ShieldSpellActive;
        _shieldSpellTurnsLeft = Math.Max(0, snapshot.ShieldSpellTurnsLeft);
        // Batch 2 persistent buff state
        _mirrorImageCharges = Math.Max(0, snapshot.MirrorImageCharges);
        _absorbElementsCharged = snapshot.AbsorbElementsCharged;
        // Batch 3 persistent buff state
        _hellishRebukePrimed = snapshot.HellishRebukePrimed;
        _armorOfAgathysTempHp = Math.Max(0, snapshot.ArmorOfAgathysTempHp);
        _fireShieldActive = snapshot.FireShieldActive;
        _wrathOfStormPrimed = snapshot.WrathOfStormPrimed;
        _deathWardActive = snapshot.DeathWardActive;
        _holyRebukePrimed = snapshot.HolyRebukePrimed;
        _cuttingWordsPrimed = snapshot.CuttingWordsPrimed;
        // Batch 4+5 persistent buff state
        _counterspellPrimed = snapshot.CounterspellPrimed;
        _elementalWeaponActive = snapshot.ElementalWeaponActive;
        _elementalWeaponElement = snapshot.ElementalWeaponElement ?? string.Empty;
        _revivifyUsed = snapshot.RevivifyUsed;
        // ProtEnergyElement is also loaded via concentration reconstruction (above), but
        // reading it here too ensures it's available even if concentration state is absent.
        if (!string.IsNullOrWhiteSpace(snapshot.ProtEnergyElement))
            _protEnergyElement = snapshot.ProtEnergyElement;

        // Restore active summon from snapshot
        _activeSummon = null;
        if (snapshot.ActiveSummon != null
            && SpellData.SummonTypes.TryGetValue(snapshot.ActiveSummon.SummonTypeId, out var loadedSummonType))
        {
            _activeSummon = new SummonInstance
            {
                Type = loadedSummonType,
                CurrentHp = loadedSummonType.MaxHp > 0
                    ? Math.Clamp(snapshot.ActiveSummon.CurrentHp, 1, loadedSummonType.MaxHp)
                    : 0
            };
            if (loadedSummonType.Behavior == SummonBehaviorKind.BuffMount)
            {
                _runDefenseBonus += 2;
                _runFleeBonus += 15;
            }
        }

        // Restore active transformation from snapshot
        _activeTransformation = null;
        if (snapshot.ActiveTransformation != null
            && SpellData.Forms.TryGetValue(snapshot.ActiveTransformation.FormId, out var loadedForm))
        {
            _activeTransformation = new TransformationInstance
            {
                Form = loadedForm,
                SourceSpellId = snapshot.ActiveTransformation.SourceSpellId,
                TempHpRemaining = Math.Clamp(snapshot.ActiveTransformation.TempHpRemaining, 1, loadedForm.TempHp),
                FirstHitPrimed = snapshot.ActiveTransformation.FirstHitPrimed
            };

            // Reconstruct passive bonuses
            if (loadedForm.Special == FormSpecialKind.DefenseBonus)
                _runDefenseBonus += loadedForm.SpecialValue;
            if (loadedForm.Special == FormSpecialKind.FleeBonus)
                _runFleeBonus += loadedForm.SpecialValue;
            if (loadedForm.Special == FormSpecialKind.Evasion)
                _runFleeBonus += loadedForm.SpecialValue;
        }

        if (resumeState == GameState.Combat)
        {
            RestoreCombatHazards(snapshot);
        }

        _playerConditions.Clear();
        if (snapshot.PlayerConditions != null)
        {
            foreach (var entry in snapshot.PlayerConditions)
            {
                if (Enum.TryParse<PlayerConditionKind>(entry.Kind, ignoreCase: true, out var kind) && entry.RemainingTurns > 0)
                {
                    _playerConditions.Add(new PlayerConditionState
                        { Kind = kind, Potency = Math.Max(0, entry.Potency), RemainingTurns = entry.RemainingTurns });
                }
            }
        }

        SyncFollowingCombatHazardsToPlayer();
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
        DisableRunMetaLayerStateIfNeeded();
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

        if (removedArchivedSpellCount > 0)
        {
            var migrationMessage = refundedArchivedSpellPicks > 0
                ? $"Spell cleanup migrated {removedArchivedSpellCount} archived spell(s); refunded {refundedArchivedSpellPicks} spell pick(s)."
                : $"Spell cleanup migrated {removedArchivedSpellCount} archived spell(s).";

            if (_gameState == GameState.Combat)
            {
                _combatLog.Add(migrationMessage);
            }
            else if (_gameState == GameState.Playing)
            {
                ShowRewardMessage(migrationMessage, requireAcknowledge: false, visibleSeconds: 12);
            }
        }

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
        _combatSkillMenuOffset = 0;
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
        if (!string.IsNullOrWhiteSpace(_activeConcentrationSpellId))
        {
            EndActiveConcentration(string.Empty, logMessage: false);
        }
        _encounterActive = false;
        _encounterRound = 1;
        _encounterEnemies.Clear();
        _encounterTurnOrder.Clear();
        _activeCombatHazards.Clear();
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
                InitiativeModifier: _player.Mod(StatName.Dexterity) + _player.InitiativeBonus,
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

    private int GetPlayerSpellSaveDc(StatName spellcastingStat)
    {
        if (_player == null)
        {
            return 10;
        }

        var proficiencyBonus = Math.Clamp(2 + Math.Max(0, (_player.Level - 1) / 4), 2, 6);
        return 8 + proficiencyBonus + _player.Mod(spellcastingStat);
    }

    private bool TryRollEnemySave(Enemy enemy, StatName saveStat, StatName spellcastingStat, out int rollTotal, out int modifier, out int dc)
    {
        modifier = enemy.Type.GetSaveModifier(saveStat);
        dc = GetPlayerSpellSaveDc(spellcastingStat);
        var roll1 = _rng.Next(1, 21);
        // Metamagic Heightened Spell: enemy rolls twice, takes lower (disadvantage on save)
        if (_metamagicPrimed)
        {
            _metamagicPrimed = false;
            var roll2 = _rng.Next(1, 21);
            var lower = Math.Min(roll1, roll2);
            PushCombatLog($"Heightened Spell — enemy rolls {roll1} and {roll2}, takes lower ({lower})!");
            rollTotal = lower + modifier;
        }
        else
        {
            rollTotal = roll1 + modifier;
        }
        return rollTotal >= dc;
    }

    private bool TryResolveSharedSpellInitialSave(SpellDefinition spell, SpellEffectRouteSpec route, Enemy enemy, out string message)
    {
        message = string.Empty;
        if (!route.InitialSaveStat.HasValue || route.SaveDamageBehavior != SpellSaveDamageBehavior.None)
        {
            return false;
        }

        var saveStat = route.InitialSaveStat.Value;
        var saved = TryRollEnemySave(enemy, saveStat, spell.ScalingStat, out var rollTotal, out _, out var dc);
        var saveLabel = GetStatShortLabel(saveStat);
        if (saved)
        {
            message = $"{enemy.Type.Name} resists {spell.Name} ({saveLabel} save {rollTotal} vs DC {dc}).";
            return true;
        }

        message = $"{enemy.Type.Name} fails the {saveLabel} save against {spell.Name} ({rollTotal} vs DC {dc}).";
        return false;
    }

    private bool TryResolveSpellDamageSaveOutcome(
        SpellDefinition spell,
        SpellEffectRouteSpec route,
        Enemy enemy,
        out string message,
        out int damageNumerator,
        out int damageDenominator,
        out bool skipStatuses)
    {
        message = string.Empty;
        damageNumerator = 1;
        damageDenominator = 1;
        skipStatuses = false;
        if (!route.InitialSaveStat.HasValue || route.SaveDamageBehavior == SpellSaveDamageBehavior.None)
        {
            return false;
        }

        var saveStat = route.InitialSaveStat.Value;
        var saved = TryRollEnemySave(enemy, saveStat, spell.ScalingStat, out var rollTotal, out _, out var dc);
        var saveLabel = GetStatShortLabel(saveStat);
        if (!saved)
        {
            message = $"{enemy.Type.Name} fails the {saveLabel} save against {spell.Name} ({rollTotal} vs DC {dc}).";
            return true;
        }

        skipStatuses = true;
        switch (route.SaveDamageBehavior)
        {
            case SpellSaveDamageBehavior.NegateOnSave:
                damageNumerator = 0;
                message = $"{enemy.Type.Name} resists {spell.Name} ({saveLabel} save {rollTotal} vs DC {dc}) and takes no damage.";
                break;
            case SpellSaveDamageBehavior.HalfOnSave:
                damageDenominator = 2;
                message = $"{enemy.Type.Name} partially resists {spell.Name} ({saveLabel} save {rollTotal} vs DC {dc}) and takes half damage.";
                break;
            default:
                message = $"{enemy.Type.Name} resists {spell.Name} ({saveLabel} save {rollTotal} vs DC {dc}).";
                break;
        }

        return true;
    }

    private static bool SpellAffectsCreatureType(SpellEffectRouteSpec route, Enemy enemy)
    {
        return route.AllowedCreatureTypes == CreatureTypeTag.Any ||
            (enemy.Type.CreatureTypes & route.AllowedCreatureTypes) != 0;
    }

    private static string GetCreatureTypeLabel(CreatureTypeTag creatureTypes)
    {
        if (creatureTypes == CreatureTypeTag.Any)
        {
            return "any creature";
        }

        var labels = new List<string>();
        if (creatureTypes.HasFlag(CreatureTypeTag.Humanoid)) labels.Add("humanoid");
        if (creatureTypes.HasFlag(CreatureTypeTag.Beast)) labels.Add("beast");
        if (creatureTypes.HasFlag(CreatureTypeTag.Undead)) labels.Add("undead");
        if (creatureTypes.HasFlag(CreatureTypeTag.Giant)) labels.Add("giant");
        if (creatureTypes.HasFlag(CreatureTypeTag.Monstrosity)) labels.Add("monstrosity");
        if (creatureTypes.HasFlag(CreatureTypeTag.Construct)) labels.Add("construct");

        return labels.Count == 0
            ? "creature"
            : string.Join("/", labels);
    }

    private static string GetStatShortLabel(StatName stat)
    {
        return stat switch
        {
            StatName.Strength => "STR",
            StatName.Dexterity => "DEX",
            StatName.Constitution => "CON",
            StatName.Intelligence => "INT",
            StatName.Wisdom => "WIS",
            StatName.Charisma => "CHA",
            _ => stat.ToString().ToUpperInvariant()
        };
    }

    private static string BuildSaveSummary(StatName saveStat, SpellSaveDamageBehavior saveDamageBehavior)
    {
        return saveDamageBehavior switch
        {
            SpellSaveDamageBehavior.NegateOnSave => $"{GetStatShortLabel(saveStat)} save negates dmg",
            SpellSaveDamageBehavior.HalfOnSave => $"{GetStatShortLabel(saveStat)} save halves dmg",
            _ => $"{GetStatShortLabel(saveStat)} save"
        };
    }

    private bool TryGetSpellCreatureTypeBlockReason(SpellDefinition spell, Enemy enemy, SpellEffectRouteSpec route, out string reason)
    {
        if (SpellAffectsCreatureType(route, enemy))
        {
            reason = string.Empty;
            return false;
        }

        reason = $"{spell.Name} only affects {GetCreatureTypeLabel(route.AllowedCreatureTypes)} targets.";
        return true;
    }

    private IReadOnlyList<string> ApplySpellOnHitStatuses(SpellDefinition spell, SpellEffectRouteSpec route, Enemy target)
    {
        var statusSpecs = ResolveSpellOnHitStatuses(spell, route);
        if (statusSpecs.Count == 0)
        {
            return Array.Empty<string>();
        }

        var messages = new List<string>();
        if (TryResolveSharedSpellInitialSave(spell, route, target, out var sharedSaveMessage))
        {
            if (!string.IsNullOrWhiteSpace(sharedSaveMessage))
            {
                messages.Add(sharedSaveMessage);
            }

            return messages;
        }

        if (!string.IsNullOrWhiteSpace(sharedSaveMessage))
        {
            messages.Add(sharedSaveMessage);
        }

        foreach (var statusSpec in statusSpecs)
        {
            TryApplyOrRefreshEnemyStatus(target, statusSpec, spell.Id, spell.Name, spell.ScalingStat, out var message);
            if (!string.IsNullOrWhiteSpace(message))
            {
                messages.Add(message);
            }
        }

        return messages;
    }

    private bool TryApplyOrRefreshEnemyStatus(
        Enemy enemy,
        CombatStatusApplySpec spec,
        string sourceSpellId,
        string sourceLabel,
        StatName? spellcastingStat,
        out string message)
    {
        message = string.Empty;
        if (spec.InitialSaveStat.HasValue && spellcastingStat.HasValue)
        {
            var initialSaveStat = spec.InitialSaveStat.Value;
            if (TryRollEnemySave(enemy, initialSaveStat, spellcastingStat.Value, out var rollTotal, out _, out var dc))
            {
                message = $"{enemy.Type.Name} resists {GetCombatStatusLabel(spec.Kind).ToLowerInvariant()} ({GetStatShortLabel(initialSaveStat)} save {rollTotal} vs DC {dc}).";
                return false;
            }
        }

        if (spec.ChancePercent < 100 && _rng.Next(100) + 1 > spec.ChancePercent)
        {
            message = $"{enemy.Type.Name} resists {GetCombatStatusLabel(spec.Kind).ToLowerInvariant()}.";
            return false;
        }

        var existing = enemy.StatusEffects.FirstOrDefault(status => status.Kind == spec.Kind);
        if (existing != null)
        {
            existing.Potency = Math.Max(existing.Potency, spec.Potency);
            existing.RemainingTurns = Math.Max(existing.RemainingTurns, spec.DurationTurns);
            existing.RepeatSaveStat ??= spec.RepeatSaveStat;
            existing.SaveDc = Math.Max(existing.SaveDc, spec.RepeatSaveStat.HasValue && spellcastingStat.HasValue ? GetPlayerSpellSaveDc(spellcastingStat.Value) : 0);
            existing.BreaksOnDamageTaken |= spec.BreaksOnDamageTaken;
            message = $"{enemy.Type.Name} remains {GetCombatStatusAdjective(spec.Kind)} ({existing.RemainingTurns} turn(s)).";
            return true;
        }

        enemy.StatusEffects.Add(new CombatStatusState
        {
            Kind = spec.Kind,
            Potency = spec.Potency,
            RemainingTurns = spec.DurationTurns,
            SourceSpellId = sourceSpellId,
            SourceLabel = sourceLabel,
            RepeatSaveStat = spec.RepeatSaveStat,
            SaveDc = spec.RepeatSaveStat.HasValue && spellcastingStat.HasValue ? GetPlayerSpellSaveDc(spellcastingStat.Value) : 0,
            BreaksOnDamageTaken = spec.BreaksOnDamageTaken
        });
        message = $"{enemy.Type.Name} is {GetCombatStatusAdjective(spec.Kind)} ({spec.DurationTurns} turn(s)).";
        return true;
    }

    private static string GetCombatStatusLabel(CombatStatusKind kind)
    {
        return kind switch
        {
            CombatStatusKind.Poison => "Poison",
            CombatStatusKind.Burning => "Burning",
            CombatStatusKind.Corroded => "Corroded",
            CombatStatusKind.Chilled => "Chilled",
            CombatStatusKind.Shocked => "Shocked",
            CombatStatusKind.Blinded => "Blinded",
            CombatStatusKind.Slowed => "Slowed",
            CombatStatusKind.Feared => "Feared",
            CombatStatusKind.Rooted => "Rooted",
            CombatStatusKind.Restrained => "Restrained",
            CombatStatusKind.Paralyzed => "Paralyzed",
            CombatStatusKind.Incapacitated => "Incapacitated",
            CombatStatusKind.Prone => "Prone",
            CombatStatusKind.Marked => "Marked",
            CombatStatusKind.Stunned => "Stunned",
            CombatStatusKind.Weakened => "Weakened",
            CombatStatusKind.Cursed => "Cursed",
            _ => kind.ToString()
        };
    }

    private static string GetCombatStatusAdjective(CombatStatusKind kind)
    {
        return kind switch
        {
            CombatStatusKind.Poison => "poisoned",
            CombatStatusKind.Burning => "burning",
            CombatStatusKind.Corroded => "corroded",
            CombatStatusKind.Chilled => "chilled",
            CombatStatusKind.Shocked => "shocked",
            CombatStatusKind.Blinded => "blinded",
            CombatStatusKind.Slowed => "slowed",
            CombatStatusKind.Feared => "feared",
            CombatStatusKind.Rooted => "rooted",
            CombatStatusKind.Restrained => "restrained",
            CombatStatusKind.Paralyzed => "paralyzed",
            CombatStatusKind.Incapacitated => "incapacitated",
            CombatStatusKind.Prone => "prone",
            CombatStatusKind.Marked => "marked",
            CombatStatusKind.Stunned => "stunned",
            CombatStatusKind.Weakened => "weakened",
            CombatStatusKind.Cursed => "cursed",
            _ => kind.ToString().ToLowerInvariant()
        };
    }

    private void TryBreakDamageSensitiveStatuses(Enemy enemy, string sourceLabel)
    {
        var breakableStatuses = enemy.StatusEffects
            .Where(status => status.BreaksOnDamageTaken)
            .ToList();
        foreach (var status in breakableStatuses)
        {
            enemy.StatusEffects.Remove(status);
            PushCombatLog($"{enemy.Type.Name} is no longer {GetCombatStatusAdjective(status.Kind)} after taking damage from {sourceLabel}.");
        }
    }

    private void TryResolveEnemyEndOfTurnSaves(Enemy enemy)
    {
        foreach (var status in enemy.StatusEffects.ToList())
        {
            if (!status.RepeatSaveStat.HasValue || status.SaveDc <= 0)
            {
                continue;
            }

            var saveStat = status.RepeatSaveStat.Value;
            var modifier = enemy.Type.GetSaveModifier(saveStat);
            var roll = _rng.Next(1, 21) + modifier;
            if (roll < status.SaveDc)
            {
                continue;
            }

            enemy.StatusEffects.Remove(status);
            PushCombatLog($"{enemy.Type.Name} shakes off {GetCombatStatusLabel(status.Kind).ToLowerInvariant()} ({GetStatShortLabel(saveStat)} save {roll} vs DC {status.SaveDc}).");
        }
    }

    private string GetEnemyStatusSummary(Enemy enemy)
    {
        if (enemy.StatusEffects.Count == 0)
        {
            return "No active effects";
        }

        return string.Join(", ", enemy.StatusEffects
            .OrderBy(status => status.Kind.ToString(), StringComparer.Ordinal)
            .Select(status => status.Potency > 1
                ? $"{GetCombatStatusLabel(status.Kind)} {status.Potency}/{status.RemainingTurns}t"
                : $"{GetCombatStatusLabel(status.Kind)} {status.RemainingTurns}t")
            .Take(3));
    }

    private string BuildSpellEffectSummary(SpellDefinition spell)
    {
        var route = SpellData.ResolveEffectRoute(spell);
        var familyLabel = SpellData.GetCombatFamilyLabel(route.CombatFamily);
        if (route.IsFutureGated)
        {
            return string.IsNullOrWhiteSpace(route.FutureRequirement)
                ? $"{familyLabel}: archived until later subsystem support."
                : $"{familyLabel}: archived until {route.FutureRequirement}.";
        }

        var segments = new List<string> { familyLabel };
        var targetShapeLabel = route.TargetShape switch
        {
            SpellTargetShape.SingleEnemy => "single target",
            SpellTargetShape.Self => route.AreaRadiusTiles > 0 ? $"self aura r{route.AreaRadiusTiles}" : "self",
            SpellTargetShape.Tile => route.HazardSpec != null ? "placed tile" : "anchored tile",
            SpellTargetShape.Radius => $"radius {Math.Max(1, route.AreaRadiusTiles)}",
            SpellTargetShape.Line => "line",
            SpellTargetShape.Cone => $"cone {Math.Max(1, route.AreaRadiusTiles)}",
            _ => "spell"
        };
        segments.Add(targetShapeLabel);
        if (!string.IsNullOrWhiteSpace(route.RuntimeBehaviorNote))
        {
            segments.Add(route.RuntimeBehaviorNote);
        }

        if (route.AllowedCreatureTypes != CreatureTypeTag.Any)
        {
            segments.Add($"{GetCreatureTypeLabel(route.AllowedCreatureTypes)} only");
        }

        if (route.InitialSaveStat.HasValue)
        {
            segments.Add(BuildSaveSummary(route.InitialSaveStat.Value, route.SaveDamageBehavior));
        }
        else if (route.HazardSpec?.InitialSaveStat.HasValue == true)
        {
            segments.Add(BuildSaveSummary(route.HazardSpec.InitialSaveStat.Value, route.HazardSpec.SaveDamageBehavior));
        }

        if (!route.DealsDirectDamage)
        {
            segments.Add("no direct damage");
        }
        else if (route.Element != SpellElement.Unknown)
        {
            segments.Add($"{route.Element.ToString().ToLowerInvariant()} damage");
        }

        if (route.OnHitStatuses.Count > 0)
        {
            var statusSummary = string.Join(", ", route.OnHitStatuses.Select(status =>
            {
                var label = status.Potency > 1
                    ? $"{GetCombatStatusLabel(status.Kind)} {status.Potency}/{status.DurationTurns}t"
                    : $"{GetCombatStatusLabel(status.Kind)} {status.DurationTurns}t";
                if (status.RepeatSaveStat.HasValue)
                {
                    label += $" repeat {GetStatShortLabel(status.RepeatSaveStat.Value)}";
                }
                if (status.BreaksOnDamageTaken)
                {
                    label += " break on dmg";
                }
                return status.ChancePercent < 100
                    ? $"{label} @{status.ChancePercent}%"
                    : label;
            }));
            segments.Add(statusSummary);
        }

        if (route.RequiresConcentration)
        {
            segments.Add("concentration");
        }

        if (route.HazardSpec != null)
        {
            var triggerSummary = route.HazardSpec.TriggersOnTurnStart && route.HazardSpec.TriggersOnEntry
                ? "start+entry"
                : route.HazardSpec.TriggersOnEntry
                    ? "entry"
                    : "turn start";
            segments.Add($"hazard {route.HazardSpec.DurationRounds}r {triggerSummary}");
        }

        if (spell.SuppressCounterAttack)
        {
            segments.Add("retaliation break");
        }

        return string.Join(" | ", segments);
    }

    private int GetEnemyIncomingDamageBonus(Enemy enemy)
    {
        var bonus = CombatStatusRules.GetIncomingDamageBonus(enemy.StatusEffects);
        // Hunter's Instinct: +2 damage to enemies with the Marked status
        if (_player != null &&
            _player.HuntersInstinctBonus > 0 &&
            enemy.StatusEffects.Any(s => s.Kind == CombatStatusKind.Marked))
        {
            bonus += _player.HuntersInstinctBonus;
        }
        return bonus;
    }

    private bool ResolveEnemyStartOfTurnStatuses(Enemy enemy)
    {
        foreach (var status in enemy.StatusEffects.ToList())
        {
            var tickDamage = status.Kind switch
            {
                CombatStatusKind.Poison => status.Potency,
                CombatStatusKind.Burning => status.Potency,
                CombatStatusKind.Corroded => status.Potency,
                _ => 0
            };

            if (tickDamage <= 0)
            {
                continue;
            }

            enemy.CurrentHp = Math.Max(0, enemy.CurrentHp - tickDamage);
            if (tickDamage > 0)
            {
                TryBreakDamageSensitiveStatuses(enemy, GetCombatStatusLabel(status.Kind));
            }
            var statusLabel = GetCombatStatusLabel(status.Kind);
            PushCombatLog($"{enemy.Type.Name} suffers {tickDamage} damage from {statusLabel.ToLowerInvariant()}.");
            PushCombatLog($"{enemy.Type.Name} HP {enemy.CurrentHp}/{enemy.Type.MaxHp}.");
        }

        return enemy.IsAlive;
    }

    private void AdvanceEnemyStatusDurations(Enemy enemy)
    {
        TryResolveEnemyEndOfTurnSaves(enemy);
        for (var i = enemy.StatusEffects.Count - 1; i >= 0; i--)
        {
            var status = enemy.StatusEffects[i];
            status.RemainingTurns -= 1;
            if (status.RemainingTurns > 0)
            {
                continue;
            }

            enemy.StatusEffects.RemoveAt(i);
            PushCombatLog($"{enemy.Type.Name} is no longer {GetCombatStatusAdjective(status.Kind)}.");
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

        SyncFollowingCombatHazardsToPlayer();
        AdvanceCombatHazardDurations();
        AdvanceConcentrationRound();

        // Aura of Vitality: tick heal at start of player turn while concentration holds
        if (_activeConcentrationSpellId == "paladin_aura_of_vitality"
            && _player != null && _player.CurrentHp < _player.MaxHp)
        {
            var pulse = _rng.Next(1, 7) + _rng.Next(1, 7); // 2d6
            var before = _player.CurrentHp;
            _player.CurrentHp = Math.Min(_player.MaxHp, _player.CurrentHp + pulse);
            PushCombatLog($"Aura of Vitality: +{_player.CurrentHp - before} HP ({_player.CurrentHp}/{_player.MaxHp}).");
        }

        // Heroism: grant caster stat mod temp HP at start of each turn
        if (_heroismActive && _player != null
            && (_activeConcentrationSpellId == "paladin_heroism" || _activeConcentrationSpellId == "bard_heroism"))
        {
            var castStat = _activeConcentrationSpellId == "paladin_heroism" ? StatName.Charisma : StatName.Charisma;
            var pulse = Math.Max(1, _player.Mod(castStat));
            _playerTempHp += pulse;
            PushCombatLog($"Heroism: +{pulse} temp HP ({_playerTempHp} total).");
        }

        // Shield spell: auto-expire after 1 turn
        if (_shieldSpellActive)
        {
            _shieldSpellTurnsLeft--;
            if (_shieldSpellTurnsLeft <= 0)
            {
                _shieldSpellActive = false;
                _runDefenseBonus = Math.Max(0, _runDefenseBonus - 5);
                PushCombatLog("Shield spell fades.");
            }
        }

        // Summon auto-attack at start of player turn
        ResolveSummonAutoAttack();

        // Transformation regeneration
        if (_activeTransformation != null && _activeTransformation.Form.Special == FormSpecialKind.Regeneration)
        {
            var regen = _rng.Next(1, _activeTransformation.Form.SpecialValue + 1);
            _activeTransformation.TempHpRemaining = Math.Min(
                _activeTransformation.Form.TempHp,
                _activeTransformation.TempHpRemaining + regen);
            PushCombatLog($"Regeneration: +{regen} temp HP ({_activeTransformation.TempHpRemaining}/{_activeTransformation.Form.TempHp}).");
        }

        AdvancePlayerConditions();
        TryJoinEncounterReinforcements();
        PruneEncounterTurnOrder();
        SetEncounterTurnToPlayer();
        SyncEncounterTargetSelection(preferCurrentEnemy: false);
        _combatMoveModeActive = false;
        _nextMoveAt = -1;
        _combatMovePointsMax = GetPlayerCombatMoveBudget(_player);
        if (_longstriderActive) _combatMovePointsMax += 2;
        if (_hasteActive) _combatMovePointsMax += 2;
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

        _packEnemiesRemainingAfterCurrent = Math.Max(0, aliveEnemies.Count(enemy =>
            !ReferenceEquals(enemy, _currentEnemy)));
        SyncEncounterTargetSelection(preferCurrentEnemy: true);
        DoEnemyAttack();
        return true;
    }

    private static List<EnemyStatusSnapshot> BuildEnemyStatusSnapshots(Enemy enemy)
    {
        return enemy.StatusEffects
            .Select(status => new EnemyStatusSnapshot
            {
                Kind = status.Kind.ToString(),
                Potency = Math.Max(0, status.Potency),
                RemainingTurns = Math.Max(0, status.RemainingTurns),
                SourceSpellId = status.SourceSpellId,
                SourceLabel = status.SourceLabel,
                RepeatSaveStat = status.RepeatSaveStat?.ToString(),
                SaveDc = Math.Max(0, status.SaveDc),
                BreaksOnDamageTaken = status.BreaksOnDamageTaken
            })
            .Where(status => status.Potency > 0 && status.RemainingTurns > 0)
            .ToList();
    }

    private List<CombatHazardSnapshot> BuildCombatHazardSnapshots()
    {
        return _activeCombatHazards
            .Select(hazard => new CombatHazardSnapshot
            {
                InstanceId = hazard.InstanceId,
                SourceSpellId = hazard.SourceSpellId,
                SourceLabel = hazard.SourceLabel,
                Element = hazard.Element.ToString(),
                BaseDamage = hazard.BaseDamage,
                Variance = hazard.Variance,
                ArmorBypass = hazard.ArmorBypass,
                CenterX = hazard.CenterX,
                CenterY = hazard.CenterY,
                RadiusTiles = hazard.RadiusTiles,
                RemainingRounds = Math.Max(0, hazard.RemainingRounds),
                FollowsPlayer = hazard.FollowsPlayer,
                RequiresConcentration = hazard.RequiresConcentration,
                TriggersOnTurnStart = hazard.TriggersOnTurnStart,
                TriggersOnEntry = hazard.TriggersOnEntry,
                InitialSaveStat = hazard.InitialSaveStat?.ToString(),
                SaveDamageBehavior = hazard.SaveDamageBehavior.ToString(),
                OnTriggerStatuses = hazard.OnTriggerStatuses
                    .Select(status => new CombatHazardStatusSnapshot
                    {
                        Kind = status.Kind.ToString(),
                        Potency = Math.Max(0, status.Potency),
                        DurationTurns = Math.Max(0, status.DurationTurns),
                        ChancePercent = Math.Clamp(status.ChancePercent, 1, 100),
                        InitialSaveStat = status.InitialSaveStat?.ToString(),
                        RepeatSaveStat = status.RepeatSaveStat?.ToString(),
                        BreaksOnDamageTaken = status.BreaksOnDamageTaken
                    })
                    .Where(status => status.Potency > 0 && status.DurationTurns > 0)
                    .ToList()
            })
            .Where(hazard => hazard.RemainingRounds > 0)
            .ToList();
    }

    private static void RestoreEnemyStatuses(Enemy enemy, EnemySnapshot snapshot)
    {
        enemy.StatusEffects.Clear();
        foreach (var statusSnapshot in snapshot.StatusEffects ?? new List<EnemyStatusSnapshot>())
        {
            if (!Enum.TryParse<CombatStatusKind>(statusSnapshot.Kind, ignoreCase: true, out var parsedKind))
            {
                continue;
            }

            var potency = Math.Max(0, statusSnapshot.Potency);
            var remainingTurns = Math.Max(0, statusSnapshot.RemainingTurns);
            if (potency <= 0 || remainingTurns <= 0)
            {
                continue;
            }

            enemy.StatusEffects.Add(new CombatStatusState
            {
                Kind = parsedKind,
                Potency = potency,
                RemainingTurns = remainingTurns,
                SourceSpellId = statusSnapshot.SourceSpellId ?? string.Empty,
                SourceLabel = statusSnapshot.SourceLabel ?? string.Empty,
                RepeatSaveStat = Enum.TryParse<StatName>(statusSnapshot.RepeatSaveStat, ignoreCase: true, out var repeatSaveStat)
                    ? repeatSaveStat
                    : null,
                SaveDc = Math.Max(0, statusSnapshot.SaveDc),
                BreaksOnDamageTaken = statusSnapshot.BreaksOnDamageTaken
            });
        }
    }

    private void RestoreCombatHazards(GameSaveSnapshot snapshot)
    {
        _activeCombatHazards.Clear();
        foreach (var hazardSnapshot in snapshot.CombatHazards ?? new List<CombatHazardSnapshot>())
        {
            if (!Enum.TryParse<SpellElement>(hazardSnapshot.Element, ignoreCase: true, out var parsedElement))
            {
                parsedElement = SpellElement.Unknown;
            }

            var hazard = new CombatHazardState
            {
                InstanceId = string.IsNullOrWhiteSpace(hazardSnapshot.InstanceId) ? $"hazard_{Guid.NewGuid():N}" : hazardSnapshot.InstanceId,
                SourceSpellId = hazardSnapshot.SourceSpellId ?? string.Empty,
                SourceLabel = hazardSnapshot.SourceLabel ?? string.Empty,
                Element = parsedElement,
                BaseDamage = Math.Max(0, hazardSnapshot.BaseDamage),
                Variance = Math.Max(0, hazardSnapshot.Variance),
                ArmorBypass = Math.Max(0, hazardSnapshot.ArmorBypass),
                CenterX = hazardSnapshot.CenterX,
                CenterY = hazardSnapshot.CenterY,
                RadiusTiles = Math.Max(0, hazardSnapshot.RadiusTiles),
                RemainingRounds = Math.Max(0, hazardSnapshot.RemainingRounds),
                FollowsPlayer = hazardSnapshot.FollowsPlayer,
                RequiresConcentration = hazardSnapshot.RequiresConcentration,
                TriggersOnTurnStart = hazardSnapshot.TriggersOnTurnStart,
                TriggersOnEntry = hazardSnapshot.TriggersOnEntry,
                InitialSaveStat = Enum.TryParse<StatName>(hazardSnapshot.InitialSaveStat, ignoreCase: true, out var hazardInitialSaveStat)
                    ? hazardInitialSaveStat
                    : null,
                SaveDamageBehavior = Enum.TryParse<SpellSaveDamageBehavior>(hazardSnapshot.SaveDamageBehavior, ignoreCase: true, out var hazardSaveDamageBehavior)
                    ? hazardSaveDamageBehavior
                    : SpellSaveDamageBehavior.None
            };

            foreach (var statusSnapshot in hazardSnapshot.OnTriggerStatuses ?? new List<CombatHazardStatusSnapshot>())
            {
                if (!Enum.TryParse<CombatStatusKind>(statusSnapshot.Kind, ignoreCase: true, out var parsedKind))
                {
                    continue;
                }

                if (statusSnapshot.Potency <= 0 || statusSnapshot.DurationTurns <= 0)
                {
                    continue;
                }

                hazard.OnTriggerStatuses.Add(new CombatStatusApplySpec
                {
                    Kind = parsedKind,
                    Potency = statusSnapshot.Potency,
                    DurationTurns = statusSnapshot.DurationTurns,
                    ChancePercent = Math.Clamp(statusSnapshot.ChancePercent, 1, 100),
                    InitialSaveStat = Enum.TryParse<StatName>(statusSnapshot.InitialSaveStat, ignoreCase: true, out var initialSaveStat)
                        ? initialSaveStat
                        : null,
                    RepeatSaveStat = Enum.TryParse<StatName>(statusSnapshot.RepeatSaveStat, ignoreCase: true, out var repeatSaveStat)
                        ? repeatSaveStat
                        : null,
                    BreaksOnDamageTaken = statusSnapshot.BreaksOnDamageTaken
                });
            }

            if (hazard.RemainingRounds > 0)
            {
                _activeCombatHazards.Add(hazard);
            }
        }
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
        var backY = GetPauseListBackY();
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
            EnsurePauseInventorySelectionVisible();
            var equipmentPanelW = Math.Min(360, Math.Max(280, listW / 3));
            var backpackGap = 18;
            var equipmentX = listX;
            var equipmentY = listY;
            var equipmentH = backY - listY - 12;
            var backpackX = equipmentX + equipmentPanelW + backpackGap;
            var backpackW = Math.Max(260, listW - equipmentPanelW - backpackGap);
            var backpackHeaderY = listY;
            var backpackListY = backpackHeaderY + 34;
            var visibleCount = GetPauseVisibleRowCount(backpackListY, backY, 58, 54);
            var start = _pauseInventoryOffset;
            var end = Math.Min(_inventoryItems.Count, start + visibleCount);

            DrawPanel(equipmentX, equipmentY, equipmentPanelW, equipmentH, ColPanelAlt, ColBorder);
            DrawTextLine("Equipped", equipmentX + 12, equipmentY + 8, 24, ColSkyBlue);
            var slotY = equipmentY + 44;
            for (var i = 0; i < PauseEquipmentDisplaySlots.Length; i++)
            {
                var displaySlot = PauseEquipmentDisplaySlots[i];
                var rowY = slotY + i * 34;
                var equippedItem = GetEquippedItemInSlot(displaySlot.Slot, displaySlot.SlotIndex);
                var equippedLabel = equippedItem == null ? "Empty" : equippedItem.Name;
                var valueColor = equippedItem == null ? ColGray : ColLightGray;
                DrawMenuRow(equipmentX + 8, rowY - 3, equipmentPanelW - 16, 28, false);
                DrawTextLine(displaySlot.Label, equipmentX + 14, rowY, 16, ColYellow);
                DrawTextClamped(equippedLabel, equipmentX + 142, rowY + 1, 15, equipmentPanelW - 154, valueColor);
            }

            DrawPanel(backpackX, equipmentY, backpackW, equipmentH, ColPanelAlt, ColBorder);
            DrawTextLine("Backpack", backpackX + 12, backpackHeaderY + 8, 24, ColSkyBlue);
            for (var i = start; i < end; i++)
            {
                var rowY = backpackListY + (i - start) * 58;
                var selected = i == _pauseMenuIndex;
                var item = _inventoryItems[i];
                DrawMenuRow(backpackX + 8, rowY, backpackW - 16, 54, selected);
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
                var statusX = backpackX + backpackW - 186;
                var statusMaxW = backpackX + backpackW - 24 - statusX;
                var itemLabelMaxW = Math.Max(80, statusX - (backpackX + 20) - 10);
                DrawTextClamped($"{item.Name} [{typeLabel}]", backpackX + 20, rowY + 7, 22, itemLabelMaxW, selected ? ColYellow : ColWhite);
                DrawTextClamped(statusLabel, statusX, rowY + 8, 18, statusMaxW, selected ? ColYellow : ColSkyBlue);
                DrawTextClamped(item.Description, backpackX + 20, rowY + 33, 16, backpackW - 40, ColLightGray);
            }

            if (start > 0)
            {
                DrawCenteredText("...more above...", backpackX + backpackW / 2, backpackListY - 16, 13, ColGray);
            }
            if (end < _inventoryItems.Count)
            {
                DrawCenteredText("...more below...", backpackX + backpackW / 2, backY - 18, 13, ColGray);
            }

            var backIndex = _inventoryItems.Count;
            var backSelected = _pauseMenuIndex == backIndex;
            DrawMenuRow(backpackX + 8, backY, backpackW - 16, 36, backSelected);
            DrawCenteredText("Back", backpackX + backpackW / 2, backY + 7, 20, backSelected ? ColYellow : ColWhite);
        }
        else if (_pauseMenuView == PauseMenuView.Save)
        {
            footerHint = "UP/DOWN select | ENTER save | ESC back";
            EnsurePauseSaveSelectionVisible();
            var visibleCount = GetPauseVisibleRowCount(listY, backY, 62, 56);
            var start = _pauseSaveOffset;
            var end = Math.Min(_pauseSaveEntries.Count, start + visibleCount);
            for (var i = start; i < end; i++)
            {
                var rowY = listY + (i - start) * 62;
                var selected = i == _pauseMenuIndex;
                var entry = _pauseSaveEntries[i];
                DrawMenuRow(listX, rowY, listW, 56, selected);
                DrawTextClamped(entry.Label, listX + 12, rowY + 8, 22, listW - 24, selected ? ColYellow : ColWhite);
                DrawTextClamped(entry.Detail, listX + 12, rowY + 33, 14, listW - 24, ColLightGray);
            }

            if (start > 0)
            {
                DrawCenteredText("...more above...", w / 2, listY - 16, 13, ColGray);
            }
            if (end < _pauseSaveEntries.Count)
            {
                DrawCenteredText("...more below...", w / 2, backY - 18, 13, ColGray);
            }

            var backIndex = _pauseSaveEntries.Count;
            var backSelected = _pauseMenuIndex == backIndex;
            DrawMenuRow(listX, backY, listW, 36, backSelected);
            DrawCenteredText("Back", w / 2, backY + 7, 20, backSelected ? ColYellow : ColWhite);
        }
        else if (_pauseMenuView == PauseMenuView.Load)
        {
            footerHint = "UP/DOWN select | ENTER load | ESC back";
            EnsurePauseLoadSelectionVisible();

            if (_pauseLoadEntries.Count == 0)
            {
                DrawWrappedText("No save files available yet. Create one from Save Game.", listX + 10, listY + 8, listW - 20, 18, ColLightGray);
            }
            else
            {
                var visibleCount = GetPauseVisibleRowCount(listY, backY, 62, 56);
                var start = _pauseLoadOffset;
                var end = Math.Min(_pauseLoadEntries.Count, start + visibleCount);
                for (var i = start; i < end; i++)
                {
                    var rowY = listY + (i - start) * 62;
                    var selected = i == _pauseMenuIndex;
                    var entry = _pauseLoadEntries[i];
                    DrawMenuRow(listX, rowY, listW, 56, selected);
                    DrawTextClamped(entry.Label, listX + 12, rowY + 8, 22, listW - 24, selected ? ColYellow : ColWhite);
                    DrawTextClamped(entry.Detail, listX + 12, rowY + 33, 14, listW - 24, ColLightGray);
                }

                if (start > 0)
                {
                    DrawCenteredText("...more above...", w / 2, listY - 16, 13, ColGray);
                }
                if (end < _pauseLoadEntries.Count)
                {
                    DrawCenteredText("...more below...", w / 2, backY - 18, 13, ColGray);
                }
            }

            var backIndex = _pauseLoadEntries.Count;
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
                DrawTextClamped(PauseSettingsOptions[i], listX + 12, rowY + 12, 20, Math.Max(80, listW - 200), selected ? ColYellow : ColWhite);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    DrawTextClamped(value, listX + listW - 170, rowY + 12, 19, 158, selected ? ColYellow : ColSkyBlue);
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
                DrawTextClamped(PauseAccessibilityOptions[i], listX + 12, rowY + 12, 20, Math.Max(80, listW - 290), selected ? ColYellow : ColWhite);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    DrawTextClamped(value, listX + listW - 260, rowY + 12, 19, 248, selected ? ColYellow : ColSkyBlue);
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
            DrawCenteredTextClamped(_pauseMessage, w / 2, panelY + panelH - 67, 16, panelW - 56, ColLightGray);
        }

        DrawFooterBar(panelX + 12, panelY + panelH - 36, panelW - 24, 24);
        DrawCenteredTextClamped(footerHint, w / 2, panelY + panelH - 31, 15, panelW - 40, ColLightGray);
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
            DrawCenteredTextClamped(_startMenuMessage, centerX, panelY + panelH - 67, 14, panelW - 36, ColLightGray);
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
        const int topPad = 40;
        const int navW = 185;
        const int gap = 12;
        const int summaryW = 280;
        var panelH = h - topPad - outerPad;
        var mainW = w - (outerPad * 2 + navW + summaryW + gap * 2);

        var navX = outerPad;
        var mainX = navX + navW + gap;
        var summaryX = mainX + mainW + gap;
        var panelY = topPad;

        // Title in header band above panels
        DrawCenteredText("Character Creator", w / 2, (topPad - GetEffectiveUiTextSize(22)) / 2, 22, ColYellow);

        DrawPanel(navX, panelY, navW, panelH, ColPanelSoft, ColBorder);
        DrawPanel(mainX, panelY, mainW, panelH, ColPanel, ColBorder);
        DrawPanel(summaryX, panelY, summaryW, panelH, ColPanelSoft, ColBorder);

        // Nav tabs with accent bars
        var tabH = UiLineH(16) + UiLineH(12) + UiScale(12);
        var tabPad = UiScale(8);
        for (var i = 0; i < CreationSections.Length; i++)
        {
            var tabY = panelY + tabPad + i * (tabH + UiScale(4));
            var selected = i == _creationSectionIndex;
            var ready = IsCreationSectionReady(i);
            var accentColor = selected ? ColYellow : (ready ? ColGreen : new Color(160, 60, 60, 255));
            DrawMenuRow(navX + 4, tabY, navW - 8, tabH, selected);
            Raylib.DrawRectangle(navX + 4, tabY, 3, tabH, accentColor);
            var indicator = ready ? "+" : "o";
            DrawTextLine(indicator, navX + 12, tabY + UiScale(6), 12, ready ? ColGreen : new Color(200, 100, 100, 255));
            DrawTextLine(CreationSections[i], navX + 28, tabY + UiScale(5), 16, selected ? ColYellow : ColLightGray);
            DrawTextLine(ready ? "Ready" : "Pending", navX + 28, tabY + UiScale(5) + UiLineH(16), 12, ready ? ColGreen : new Color(200, 100, 100, 255));
        }

        // Nav hints — bottom-anchored
        var navHintsY = panelY + panelH - UiScale(6) - UiLineH(12) * 4;
        DrawCenteredTextClamped("Auto-advance on ready", navX + navW / 2, navHintsY, 11, navW - 16, ColGray);
        DrawCenteredTextClamped("A/D jump sections", navX + navW / 2, navHintsY + UiLineH(12), 11, navW - 16, ColGray);
        DrawCenteredTextClamped("< > edit fields", navX + navW / 2, navHintsY + UiLineH(12) * 2, 11, navW - 16, ColGray);
        DrawCenteredTextClamped("ESC back", navX + navW / 2, navHintsY + UiLineH(12) * 3, 11, navW - 16, ColGray);

        if (_player == null)
        {
            DrawCenteredText("Creation data unavailable.", mainX + mainW / 2, panelY + 120, 24, ColRed);
            return;
        }

        var contentX = mainX + 14;
        var contentW = mainW - 28;

        // Content header band
        var headerH = UiScale(6) + UiLineH(20) + UiLineH(13) + UiScale(6);
        Raylib.DrawRectangle(mainX + 1, panelY + 1, mainW - 2, headerH, new Color(25, 25, 38, 255));
        DrawTextLine(CreationSections[_creationSectionIndex], contentX, panelY + UiScale(6), 20, ColSkyBlue);
        var sectionSubtitles = new[]
        {
            "Set your character's name, gender, and race.",
            "Choose a class. Press ENTER to confirm your choice.",
            "Spend 25 build points. Race bonuses shown in blue. RIGHT/LEFT to adjust.",
            "Pick spells for your class. Press ENTER to select or remove.",
            "Choose a starting feat to define your build.",
            "Review all picks before beginning the adventure."
        };
        DrawTextLine(sectionSubtitles[_creationSectionIndex], contentX, panelY + UiScale(6) + UiLineH(20), 13, ColGray);
        Raylib.DrawLine(mainX + 1, panelY + headerH, mainX + mainW - 1, panelY + headerH, ColBorder);
        var yCursor = panelY + headerH + UiScale(10);

        switch (_creationSectionIndex)
        {
            case 0:
            {
                var nameSelected = _selectedCreationIdentityIndex == 0;
                var genderSelected = _selectedCreationIdentityIndex == 1;
                var raceSelected = _selectedCreationIdentityIndex == 2;
                var rowH = UiLineH(12) + UiLineH(20) + UiScale(12);
                var rowStep = rowH + UiScale(6);

                // Name
                DrawMenuRow(contentX, yCursor, contentW, rowH, nameSelected);
                Raylib.DrawRectangle(contentX, yCursor, 3, rowH, nameSelected ? ColYellow : ColGray);
                DrawTextLine("NAME", contentX + 10, yCursor + UiScale(6), 12, ColGray);
                var nameValue = string.IsNullOrWhiteSpace(_pendingName) ? string.Empty : _pendingName;
                var nameSuffix = nameSelected && Raylib.GetTime() % 1 < 0.5 ? "_" : string.Empty;
                DrawTextClamped($"{nameValue}{nameSuffix}", contentX + 10, yCursor + UiScale(6) + UiLineH(12), 20, contentW - 20, nameSelected ? ColWhite : ColLightGray);
                yCursor += rowStep;

                // Gender
                DrawMenuRow(contentX, yCursor, contentW, rowH, genderSelected);
                Raylib.DrawRectangle(contentX, yCursor, 3, rowH, genderSelected ? ColYellow : ColGray);
                DrawTextLine("GENDER", contentX + 10, yCursor + UiScale(6), 12, ColGray);
                DrawTextLine(Genders[_selectedGenderIndex].ToString(), contentX + 10, yCursor + UiScale(6) + UiLineH(12), 20, genderSelected ? ColWhite : ColLightGray);
                if (genderSelected)
                {
                    const string arrowStr = "< >";
                    var arrowW = Raylib.MeasureText(arrowStr, GetEffectiveUiTextSize(16));
                    DrawTextLine(arrowStr, contentX + contentW - arrowW - 10, yCursor + UiScale(6) + UiLineH(12), 16, ColYellow);
                }
                yCursor += rowStep;

                // Race
                DrawMenuRow(contentX, yCursor, contentW, rowH, raceSelected);
                Raylib.DrawRectangle(contentX, yCursor, 3, rowH, raceSelected ? ColYellow : ColGray);
                DrawTextLine("RACE", contentX + 10, yCursor + UiScale(6), 12, ColGray);
                DrawTextLine(Races[_selectedRaceIndex].ToString(), contentX + 10, yCursor + UiScale(6) + UiLineH(12), 20, raceSelected ? ColWhite : ColLightGray);
                if (raceSelected)
                {
                    const string arrowStr = "< >";
                    var arrowW = Raylib.MeasureText(arrowStr, GetEffectiveUiTextSize(16));
                    DrawTextLine(arrowStr, contentX + contentW - arrowW - 10, yCursor + UiScale(6) + UiLineH(12), 16, ColYellow);
                }
                yCursor += rowStep;

                var appearanceLabel = ResolveDefaultSpriteForRaceAndGender(Races[_selectedRaceIndex], Genders[_selectedGenderIndex]);
                DrawWrappedText($"Portrait locked to Race + Gender in this build ({appearanceLabel}). {RaceDescriptions[Races[_selectedRaceIndex]]}", contentX + 4, yCursor, contentW - 8, 14, ColGray);
                break;
            }
            case 1:
            {
                var noteH = DrawWrappedText("Changing class resets stats, spells, and feat legality. Highlighted class is not locked until you press ENTER.", contentX, yCursor, contentW, 15, ColGray);
                yCursor += noteH + UiScale(8);
                var rowH = UiLineH(20) + UiScale(8);
                for (var i = 0; i < CharacterClasses.All.Count; i++)
                {
                    var selected = i == _selectedClassIndex;
                    var rowY = yCursor + i * (rowH + 4);
                    DrawMenuRow(contentX, rowY, contentW, rowH, selected);
                    DrawTextLine(CharacterClasses.All[i].Name, contentX + 10, rowY + UiScale(4), 20, selected ? ColYellow : ColLightGray);
                }
                var classInfoY = yCursor + CharacterClasses.All.Count * (rowH + 4) + UiScale(10);
                var chosenClass = CharacterClasses.All[_selectedClassIndex];
                DrawTextLine(chosenClass.Name, contentX, classInfoY, 22, ColYellow);
                var descH = DrawWrappedText(chosenClass.Description, contentX, classInfoY + UiLineH(22) + 4, contentW, 16, ColLightGray);
                DrawTextClamped(
                    $"Status: {(IsCreationClassReady() ? "Confirmed" : "Pending — press ENTER to confirm")}",
                    contentX, classInfoY + UiLineH(22) + 4 + descH + 4, 16, contentW,
                    IsCreationClassReady() ? ColGreen : ColYellow);
                break;
            }
            case 2:
            {
                var pointsColor = _creationPointsRemaining > 0 ? ColYellow : ColGreen;
                DrawTextLine($"Points remaining: {_creationPointsRemaining} / 25", contentX, yCursor, 18, pointsColor);
                yCursor += UiLineH(18) + UiScale(8);

                Player.RaceBonuses.TryGetValue(Races[_selectedRaceIndex], out var currentRaceBonuses);

                var statRowH = UiLineH(20) + UiScale(8);
                var statRowStep = statRowH + UiScale(4);
                for (var i = 0; i < StatOrder.Length; i++)
                {
                    var selected = i == _creationSelectionIndex;
                    var rowY = yCursor + i * statRowStep;
                    DrawMenuRow(contentX, rowY, contentW, statRowH, selected);
                    var stat = StatOrder[i];
                    var allocated = _creationAllocatedStats[i];
                    var bought = 10 + allocated;
                    var val = _player.Stats.Get(stat);
                    var mod = _player.Mod(stat);
                    var modStr = mod >= 0 ? $"+{mod}" : $"{mod}";

                    // Stat name + inline race badge if this stat gets a bonus
                    var raceBonus = currentRaceBonuses != null && currentRaceBonuses.TryGetValue(stat, out var rb) ? rb : 0;
                    DrawTextLine(stat.ToString(), contentX + 10, rowY + UiScale(4), 18, selected ? ColYellow : ColLightGray);
                    if (raceBonus != 0)
                    {
                        var raceBadge = $"+{raceBonus} race";
                        var nameW = Raylib.MeasureText(stat.ToString(), GetEffectiveUiTextSize(18));
                        DrawTextLine(raceBadge, contentX + 10 + nameW + UiScale(6), rowY + UiScale(4), 12, ColSkyBlue);
                    }

                    DrawTextLine($"<  {val} ({modStr})  >", contentX + contentW / 2 - UiScale(30), rowY + UiScale(4), 18, selected ? ColWhite : ColLightGray);

                    // Cost-to-raise badge on right
                    string costInfo;
                    Color costColor;
                    if (bought >= 20)
                    {
                        costInfo = "MAX";
                        costColor = ColGray;
                    }
                    else if (bought <= 7)
                    {
                        costInfo = "MIN";
                        costColor = ColGray;
                    }
                    else
                    {
                        var nextCost = PointBuyCostToRaise(bought);
                        costInfo = $"+{nextCost}pt";
                        costColor = nextCost <= _creationPointsRemaining ? ColGreen : ColRed;
                    }
                    var costInfoW = Raylib.MeasureText(costInfo, GetEffectiveUiTextSize(13));
                    DrawTextLine(costInfo, contentX + contentW - costInfoW - UiScale(10), rowY + UiScale(4), 13, costColor);
                }

                var actionY = yCursor + StatOrder.Length * statRowStep + UiScale(6);
                Raylib.DrawLine(contentX, actionY, contentX + contentW, actionY, ColBorder);
                actionY += UiScale(8);
                var btnW = (contentW - UiScale(12)) / 2;
                var undoSel = _creationSelectionIndex == StatOrder.Length;
                var resetSel = _creationSelectionIndex == StatOrder.Length + 1;
                var btnH = UiLineH(16) + UiScale(8);
                DrawMenuRow(contentX, actionY, btnW, btnH, undoSel);
                DrawCenteredText("Undo", contentX + btnW / 2, actionY + UiScale(4), 15, undoSel ? ColYellow : ColLightGray);
                DrawMenuRow(contentX + btnW + UiScale(12), actionY, btnW, btnH, resetSel);
                DrawCenteredText("Reset All", contentX + btnW + UiScale(12) + btnW / 2, actionY + UiScale(4), 15, resetSel ? ColYellow : ColLightGray);
                DrawWrappedText("RIGHT adds · LEFT removes · spend all 25 pts to unlock Review", contentX, actionY + btnH + UiScale(8), contentW, 13, ColGray);
                break;
            }
            case 3:
            {
                var spellPickLeft = _player.SpellPickPoints;
                DrawTextLine($"Spell picks remaining: {spellPickLeft}", contentX, yCursor, 18, spellPickLeft > 0 ? ColSkyBlue : ColGreen);
                yCursor += UiLineH(18) + UiScale(6);

                var autoCantrips = _player
                    .GetKnownSpells()
                    .Where(spell => spell.IsCantrip)
                    .OrderBy(spell => spell.Name, StringComparer.Ordinal)
                    .ToList();
                if (autoCantrips.Count > 0)
                {
                    var autoLabel = $"Auto cantrips: {string.Join(", ", autoCantrips.Select(spell => spell.Name))}";
                    var autoHeight = DrawWrappedText(autoLabel, contentX, yCursor, contentW, 13, ColGray);
                    yCursor += autoHeight + UiScale(6);
                }

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
                        DrawTextLine("Undo Last Spell Pick", contentX + 10, rowY + 16, 18, selected ? ColYellow : ColLightGray);
                        DrawTextClamped("Reverts the most recent spell selection", contentX + 10, rowY + 34, 13, contentW - 20, ColGray);
                        continue;
                    }

                    if (i == resetRowIndex)
                    {
                        DrawTextLine("Reset Spell Picks", contentX + 10, rowY + 16, 18, selected ? ColYellow : ColLightGray);
                        DrawTextClamped("Undo all selected spells and rebuild picks", contentX + 10, rowY + 34, 13, contentW - 20, ColGray);
                        continue;
                    }

                    var spell = _creationLearnableSpells[i];
                    var tier = spell.IsCantrip ? "Cantrip" : $"L{spell.SpellLevel}";
                    var familyLabel = SpellData.GetCombatFamilyLabel(spell);
                    var canLearn = _player.CanLearnSpell(spell, out var blockReason);
                    var spellAlreadyKnown = _player.KnowsSpell(spell.Id);
                    var manuallySelected = _creationChosenSpellIds.Contains(spell.Id);
                    var status = canLearn
                        ? "Learnable (ENTER to add)"
                        : spellAlreadyKnown
                            ? manuallySelected ? "Selected (ENTER to remove)" : "Known by default"
                            : $"Locked: {blockReason}";
                    var nameColor = canLearn ? (selected ? ColYellow : ColWhite) : spellAlreadyKnown ? ColSkyBlue : ColGray;
                    var statusColor = canLearn ? ColGreen : spellAlreadyKnown ? (manuallySelected ? ColYellow : ColSkyBlue) : ColRed;

                    DrawTextClamped($"{spell.Name} ({tier}) [{familyLabel}]", contentX + 10, rowY + 6, 17, contentW - 20, nameColor);
                    DrawTextClamped(BuildSpellEffectSummary(spell), contentX + 10, rowY + 24, 13, contentW - 20, ColLightGray);
                    DrawTextClamped(status, contentX + 10, rowY + 40, 13, contentW - 20, statusColor);
                }

                if (menuCount == 0)
                {
                    if (!_player.IsCasterClass)
                        DrawWrappedText("This class has no spell progression in the current 1-6 scope.", contentX, yCursor + 6, contentW, 16, ColGray);
                    else if (_player.SpellPickPoints <= 0)
                        DrawWrappedText("No spell picks available at this level. Level up to unlock additional picks.", contentX, yCursor + 6, contentW, 16, ColGray);
                    else
                    {
                        DrawWrappedText("No learnable spells right now. Review locked spells below.", contentX, yCursor + 6, contentW, 16, ColGray);
                        var previewY = yCursor + 40;
                        foreach (var spell in _player.GetClassSpells().Take(3))
                        {
                            var canLearn = _player.CanLearnSpell(spell, out var reason);
                            var tier = spell.IsCantrip ? "Cantrip" : $"L{spell.SpellLevel}";
                            var familyLabel = SpellData.GetCombatFamilyLabel(spell);
                            DrawTextLine(
                                $"{spell.Name} ({tier}, {familyLabel}): {(canLearn ? "Learnable" : reason)}",
                                contentX + 4, previewY, 14, canLearn ? ColGreen : ColGray);
                            previewY += UiLineH(14) + 2;
                        }
                    }
                }
                else
                {
                    DrawWrappedText("Families describe current combat behavior. Locked rows show exact reasons.", contentX, yCursor + SpellLearnVisibleCount * 60 + 8, contentW, 13, ColGray);
                }
                break;
            }
            case 4:
            {
                var featPicksLeft = _player.FeatPoints;
                DrawTextLine($"Feat picks remaining: {featPicksLeft}", contentX, yCursor, 18, featPicksLeft > 0 ? ColYellow : ColGreen);
                yCursor += UiLineH(18) + UiScale(6);

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
                        ? $"Learnable (ENTER to select). Req: {GetFeatPrerequisiteLabel(feat)}"
                        : chosen
                            ? "Selected (ENTER to remove)"
                            : $"Locked: {blockReason}";
                    var nameColor = canLearn ? (selected ? ColYellow : ColWhite) : chosen ? ColSkyBlue : ColGray;
                    var statusColor = canLearn ? ColGreen : chosen ? ColSkyBlue : ColRed;
                    DrawTextClamped(feat.Name, contentX + 10, rowY + 6, 18, contentW - 20, nameColor);
                    DrawTextClamped($"Effect: {GetFeatEffectLabel(feat)}", contentX + 10, rowY + 28, 13, contentW - 20, ColLightGray);
                    DrawTextClamped(status, contentX + 10, rowY + 46, 13, contentW - 20, statusColor);
                }

                if (menuCount == 0)
                {
                    DrawWrappedText("No feats available in the current catalog.", contentX, yCursor + 8, contentW, 16, ColGray);
                }
                else
                {
                    var highlightedFeat = _creationFeatChoices[Math.Clamp(_selectedCreationFeatIndex, 0, _creationFeatChoices.Count - 1)];
                    DrawWrappedText(
                        $"Selected: {highlightedFeat.Description}  Requirements: {GetFeatPrerequisiteLabel(highlightedFeat)}",
                        contentX, yCursor + CreationFeatVisibleCount * 70 + 8, contentW, 13, ColGray);
                }
                break;
            }
            case 5:
            {
                var readyName = IsCreationNameReady();
                var readyClass = IsCreationClassReady();
                var readyStats = IsCreationStatsReady();
                var readySpells = IsCreationSpellsReady();
                var readyFeats = IsCreationFeatsReady();
                (string label, bool ok)[] reviewRows =
                {
                    ("Name set", readyName),
                    ("Class confirmed", readyClass),
                    ("Stats allocated", readyStats),
                    ("Spell picks complete", readySpells),
                    ("Starting feat selected", readyFeats)
                };

                for (var i = 0; i < reviewRows.Length; i++)
                {
                    var (label, ok) = reviewRows[i];
                    DrawPanel(contentX, yCursor + i * 44, contentW, 38, ColPanelAlt, ColBorder);
                    DrawTextLine($"{(ok ? "+" : "o")} {label}: {(ok ? "Done" : "Pending")}", contentX + 12, yCursor + 10 + i * 44, 18, ok ? ColGreen : ColRed);
                }

                var startY = yCursor + reviewRows.Length * 44 + 24;
                var readyToStart = readyName && readyStats && readySpells && readyFeats;
                DrawMenuRow(contentX, startY, contentW, 54, readyToStart);
                DrawCenteredText("Start Adventure (ENTER)", contentX + contentW / 2, startY + 15, 22, readyToStart ? ColYellow : ColGray);
                DrawWrappedText("You can still go back to any section before confirming.", contentX, startY + 60, contentW, 14, ColLightGray);
                break;
            }
        }

        // Summary panel — character card + structured sections
        var summaryTextW = summaryW - 24;
        var sy = panelY + 10;
        Raylib.BeginScissorMode(summaryX + 1, panelY + 1, summaryW - 2, panelH - 2);

        // Character card
        var buildReady = IsCreationReady();
        var cardBg = buildReady ? new Color(18, 45, 18, 255) : new Color(38, 28, 16, 255);
        var cardH = UiScale(6) + UiLineH(20) + UiLineH(14) + UiLineH(13) + UiScale(6);
        Raylib.DrawRectangle(summaryX + 4, sy, summaryW - 8, cardH, cardBg);
        Raylib.DrawRectangleLines(summaryX + 4, sy, summaryW - 8, cardH, buildReady ? ColGreen : new Color(180, 140, 60, 255));

        var shownName = string.IsNullOrWhiteSpace(_pendingName) ? "Unnamed" : _pendingName.Trim();
        DrawTextClamped(shownName, summaryX + 10, sy + UiScale(6), 20, summaryTextW - 8, ColWhite);

        var cardClassName = CharacterClasses.All[_selectedClassIndex].Name;
        var classConfirmed = IsCreationClassReady();
        DrawTextClamped(
            classConfirmed ? cardClassName : $"{cardClassName} (pending)",
            summaryX + 10, sy + UiScale(6) + UiLineH(20), 14, summaryTextW - 8,
            classConfirmed ? ColYellow : ColGray);
        DrawTextClamped(
            $"{Races[_selectedRaceIndex]} · {Genders[_selectedGenderIndex]}",
            summaryX + 10, sy + UiScale(6) + UiLineH(20) + UiLineH(14), 13, summaryTextW - 8, ColLightGray);

        var badgeText = buildReady ? "READY" : "PENDING";
        var summaryBadgeW = Raylib.MeasureText(badgeText, 11);
        DrawTextLine(badgeText, summaryX + summaryW - summaryBadgeW - 12, sy + UiScale(6), 11, buildReady ? ColGreen : new Color(200, 160, 60, 255));

        sy += cardH + UiScale(6);

        // HP + picks
        Raylib.DrawLine(summaryX + 8, sy, summaryX + summaryW - 8, sy, ColBorder);
        sy += UiScale(6);
        DrawTextClamped($"HP {_player.CurrentHp}/{_player.MaxHp}", summaryX + 10, sy, 14, summaryTextW, ColLightGray);
        sy += UiLineH(14) + UiScale(2);
        DrawTextClamped($"Pts {_creationPointsRemaining}/25  Spells +{_player.SpellPickPoints}  Feats +{_player.FeatPoints}", summaryX + 10, sy, 13, summaryTextW, ColLightGray);
        sy += UiLineH(13) + UiScale(8);

        // Combat Stats
        Raylib.DrawLine(summaryX + 8, sy, summaryX + summaryW - 8, sy, ColBorder);
        sy += UiScale(6);
        DrawTextLine("Combat Stats", summaryX + 10, sy, 14, ColSkyBlue);
        sy += UiLineH(14) + UiScale(2);
        var csArmorState = GetCurrentArmorCategory();
        var csArmorLabel = GetArmorStateLabel(csArmorState);
        var csAC = GetPlayerArmorClass();
        var csWeapon = GetEquippedWeaponDef();
        var csAtkStat = csWeapon.IsFinesse
            ? (_player.Mod(StatName.Strength) >= _player.Mod(StatName.Dexterity) ? StatName.Strength : StatName.Dexterity)
            : (csWeapon.IsRanged ? StatName.Dexterity : csWeapon.AttackStat);
        DrawTextClamped($"AC: {csAC}  ({csArmorLabel})", summaryX + 10, sy, 13, summaryTextW, ColLightGray);
        sy += UiLineH(13) + 2;
        DrawTextClamped($"Weapon: {csWeapon.Name} ({csWeapon.DiceCount}d{csWeapon.DamageDice}+{GetStatShortLabel(csAtkStat)})", summaryX + 10, sy, 13, summaryTextW, ColLightGray);
        sy += UiLineH(13) + 2;
        var csSaves = string.Join(", ", _player.CharacterClass.SaveProficiencies.Select(s => GetStatShortLabel(s)));
        DrawTextClamped($"Saves: {csSaves}", summaryX + 10, sy, 13, summaryTextW, ColLightGray);
        sy += UiLineH(13) + 2;
        if (_player.IsCasterClass)
        {
            var csDC = 8 + GetProficiencyBonus() + Math.Max(0, _player.Mod(_player.CastingStat));
            DrawTextClamped($"Spell DC: {csDC}", summaryX + 10, sy, 13, summaryTextW, ColLightGray);
            sy += UiLineH(13) + 2;
        }
        sy += UiScale(8);

        // Checks — 2-column grid
        Raylib.DrawLine(summaryX + 8, sy, summaryX + summaryW - 8, sy, ColBorder);
        sy += UiScale(6);
        DrawTextLine("Checks", summaryX + 10, sy, 14, ColLightGray);
        sy += UiLineH(14) + UiScale(2);

        var col1X = summaryX + 10;
        var col2X = summaryX + summaryW / 2 + 4;
        (string name, bool ok)[] checks =
        {
            ("Name", IsCreationNameReady()),
            ("Class", IsCreationClassReady()),
            ("Stats", IsCreationStatsReady()),
            ("Spells", IsCreationSpellsReady()),
            ("Feat", IsCreationFeatsReady()),
        };
        for (var i = 0; i < checks.Length; i += 2)
        {
            var (l1, ok1) = checks[i];
            DrawTextClamped($"{(ok1 ? "+" : "o")} {l1}", col1X, sy, 13, summaryW / 2 - 14, ok1 ? ColGreen : ColRed);
            if (i + 1 < checks.Length)
            {
                var (l2, ok2) = checks[i + 1];
                DrawTextClamped($"{(ok2 ? "+" : "o")} {l2}", col2X, sy, 13, summaryW / 2 - 14, ok2 ? ColGreen : ColRed);
            }
            sy += UiLineH(13) + 2;
        }
        sy += UiScale(6);

        // Core stats — 2-column with modifiers
        Raylib.DrawLine(summaryX + 8, sy, summaryX + summaryW - 8, sy, ColBorder);
        sy += UiScale(6);
        DrawTextLine("Stats", summaryX + 10, sy, 14, ColLightGray);
        sy += UiLineH(14) + UiScale(2);

        (StatName s1, StatName s2)[] statPairs =
        {
            (StatName.Strength, StatName.Dexterity),
            (StatName.Constitution, StatName.Intelligence),
            (StatName.Wisdom, StatName.Charisma),
        };
        foreach (var (s1, s2) in statPairs)
        {
            var v1 = _player.Stats.Get(s1); var m1 = _player.Mod(s1);
            var v2 = _player.Stats.Get(s2); var m2 = _player.Mod(s2);
            DrawTextClamped($"{s1.ToString()[..3].ToUpper()} {v1} ({(m1 >= 0 ? "+" : "")}{m1})", col1X, sy, 13, summaryW / 2 - 14, ColLightGray);
            DrawTextClamped($"{s2.ToString()[..3].ToUpper()} {v2} ({(m2 >= 0 ? "+" : "")}{m2})", col2X, sy, 13, summaryW / 2 - 14, ColLightGray);
            sy += UiLineH(13) + 2;
        }
        sy += UiScale(6);

        // Feat
        Raylib.DrawLine(summaryX + 8, sy, summaryX + summaryW - 8, sy, ColBorder);
        sy += UiScale(6);
        DrawTextLine("Feat", summaryX + 10, sy, 14, ColYellow);
        sy += UiLineH(14) + UiScale(2);
        var selectedFeat = _player.Feats.FirstOrDefault();
        DrawTextClamped(selectedFeat?.Name ?? "None", summaryX + 10, sy, 13, summaryTextW, selectedFeat != null ? ColLightGray : ColGray);
        sy += UiLineH(13) + UiScale(8);

        // Spells
        Raylib.DrawLine(summaryX + 8, sy, summaryX + summaryW - 8, sy, ColBorder);
        sy += UiScale(6);
        DrawTextLine("Spells", summaryX + 10, sy, 14, ColSkyBlue);
        sy += UiLineH(14) + UiScale(2);
        var knownSpells = _player.GetKnownSpells();
        if (knownSpells.Count == 0)
        {
            DrawTextClamped("None", summaryX + 10, sy, 13, summaryTextW, ColGray);
        }
        else
        {
            foreach (var spell in knownSpells.Take(5))
            {
                var tier = spell.IsCantrip ? "C" : $"L{spell.SpellLevel}";
                DrawTextClamped($"{tier} {spell.Name}", summaryX + 10, sy, 13, summaryTextW, ColLightGray);
                sy += UiLineH(13) + 2;
            }
        }
        Raylib.EndScissorMode();

        DrawFooterBar(outerPad, h - 38, w - outerPad * 2, 26);
        var footer = string.IsNullOrWhiteSpace(_creationMessage) ? GetCreationSectionHint() : _creationMessage;
        DrawTextClamped(footer, outerPad + 8, h - 33, 16, w - outerPad * 2 - 16, ColLightGray);
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
        int worldH,
        int hudH = 0)
    {
        var halfViewW = screenW / 2f;
        var halfViewH = (screenH - hudH) / 2f;
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
        var hudH = UiLayout.HudHeight;

        // Center the viewport on the playable area below the HUD so world
        // geometry is never obscured by the top panel.
        var viewCenterY = hudH + (screenH - hudH) / 2f;

        var camera = new Camera2D
        {
            Offset = new System.Numerics.Vector2(screenW / 2f, viewCenterY),
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
        if (IsCombatState(_gameState) && _currentEnemy != null && _currentEnemy.IsAlive)
        {
            var enemyWorldX = _currentEnemy.X * GameMap.TileSize + GameMap.TileSize / 2f;
            var enemyWorldY = _currentEnemy.Y * GameMap.TileSize + GameMap.TileSize / 2f;
            desiredTarget = new System.Numerics.Vector2(
                (playerWorldX + enemyWorldX) * 0.5f,
                (playerWorldY + enemyWorldY) * 0.5f);
        }

        desiredTarget = ClampCameraTargetToWorld(desiredTarget, screenW, screenH, worldW, worldH, hudH);

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

            shiftedTarget = ClampCameraTargetToWorld(shiftedTarget, screenW, screenH, worldW, worldH, hudH);
            var dt = Math.Clamp(Raylib.GetFrameTime(), 0f, 0.25f);
            _cameraTarget.X = Damp(_cameraTarget.X, shiftedTarget.X, GameTuning.CameraSmoothness, dt);
            _cameraTarget.Y = Damp(_cameraTarget.Y, shiftedTarget.Y, GameTuning.CameraSmoothness, dt);
        }

        _cameraTarget = ClampCameraTargetToWorld(_cameraTarget, screenW, screenH, worldW, worldH, hudH);
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

    private void DrawCombatHazards()
    {
        if (!IsCombatState(_gameState) || _activeCombatHazards.Count == 0)
        {
            return;
        }

        foreach (var hazard in _activeCombatHazards)
        {
            var fill = hazard.Element switch
            {
                SpellElement.Fire => new Color(220, 96, 52, 72),
                SpellElement.Radiant => new Color(232, 220, 118, 72),
                SpellElement.Arcane => new Color(118, 148, 232, 72),
                SpellElement.Nature => new Color(112, 182, 102, 72),
                SpellElement.Force => new Color(178, 178, 228, 72),
                _ => new Color(128, 142, 176, 72)
            };
            var border = hazard.Element switch
            {
                SpellElement.Fire => new Color(255, 156, 98, 180),
                SpellElement.Radiant => new Color(255, 240, 150, 180),
                SpellElement.Arcane => new Color(164, 196, 255, 180),
                SpellElement.Nature => new Color(154, 224, 138, 180),
                SpellElement.Force => new Color(210, 210, 255, 180),
                _ => new Color(182, 194, 220, 180)
            };

            foreach (var tile in EncounterSpellAreaRules.EnumerateRadiusTiles(hazard.CenterX, hazard.CenterY, hazard.RadiusTiles))
            {
                if (IsWallOrSealed(tile.X, tile.Y))
                {
                    continue;
                }

                var px = tile.X * GameMap.TileSize;
                var py = tile.Y * GameMap.TileSize;
                Raylib.DrawRectangle(px + 5, py + 5, GameMap.TileSize - 10, GameMap.TileSize - 10, fill);
                Raylib.DrawRectangleLines(px + 4, py + 4, GameMap.TileSize - 8, GameMap.TileSize - 8, border);
            }
        }
    }

    private void DrawPendingCombatSpellPreview()
    {
        if (_gameState != GameState.CombatSpellTargeting || _player == null)
        {
            return;
        }

        if (!TryGetPendingCombatSpell(out var pendingSpell))
        {
            return;
        }

        var route = SpellData.ResolveEffectRoute(pendingSpell);
        var (anchorX, anchorY) = ResolveSpellAnchorTile(pendingSpell);
        var aimValidation = ValidateCombatSpellAim(pendingSpell);
        var tiles = EncounterSpellAreaRules.EnumerateAffectedTiles(
            pendingSpell,
            route,
            _player.X,
            _player.Y,
            anchorX,
            anchorY);
        var fill = aimValidation.IsLegal
            ? new Color(112, 186, 238, 76)
            : new Color(224, 92, 92, 76);
        var border = aimValidation.IsLegal
            ? new Color(176, 228, 255, 188)
            : new Color(255, 154, 154, 188);
        foreach (var tile in tiles)
        {
            if (tile.X < 0 || tile.X >= GameMap.MapWidthTiles || tile.Y < 0 || tile.Y >= GameMap.MapHeightTiles)
            {
                continue;
            }

            if (IsWallOrSealed(tile.X, tile.Y))
            {
                continue;
            }

            var px = tile.X * GameMap.TileSize;
            var py = tile.Y * GameMap.TileSize;
            Raylib.DrawRectangle(px + 6, py + 6, GameMap.TileSize - 12, GameMap.TileSize - 12, fill);
            Raylib.DrawRectangleLines(px + 5, py + 5, GameMap.TileSize - 10, GameMap.TileSize - 10, border);
        }

        if (!UsesSelfCenteredSpellTargeting(pendingSpell))
        {
            var anchorPx = anchorX * GameMap.TileSize;
            var anchorPy = anchorY * GameMap.TileSize;
            var anchorBorder = aimValidation.IsLegal ? ColYellow : ColAccentRose;
            Raylib.DrawRectangleLines(anchorPx + 2, anchorPy + 2, GameMap.TileSize - 4, GameMap.TileSize - 4, anchorBorder);
            Raylib.DrawRectangleLines(anchorPx + 1, anchorPy + 1, GameMap.TileSize - 2, GameMap.TileSize - 2, anchorBorder);
        }

        foreach (var enemy in ResolveSpellAffectedEnemies(pendingSpell))
        {
            var px = enemy.X * GameMap.TileSize;
            var py = enemy.Y * GameMap.TileSize;
            Raylib.DrawRectangleLines(px + 1, py + 1, GameMap.TileSize - 2, GameMap.TileSize - 2, ColSkyBlue);
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
        DrawCombatHazards();
        DrawPendingCombatSpellPreview();

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
        var hasBuildIdentity = EnableRunMetaLayer && (
            _runArchetype != RunArchetype.None ||
            _runRelic != RunRelic.None ||
            GetEffectiveExecutionRank() > 0 ||
            GetEffectiveArcRank() > 0 ||
            GetEffectiveEscapeRank() > 0);
        var topRightText = hasBuildIdentity ? $"Build: {GetRunIdentityLabel()}" : controlsText;
        var topRightFont = hasBuildIdentity ? 15 : 16;
        var topRightColor = hasBuildIdentity ? ColSkyBlue : ColLightGray;
        var topRightX = hudW - pad - MeasureUiText(topRightText, topRightFont);
        var levelX = pad;
        var enemiesMinX = levelX + MeasureUiText(levelText, 18) + 26;
        var enemiesX = Math.Max(enemiesMinX, topRightX - MeasureUiText(enemiesText, 16) - 18);

        var hudR1 = UiScale(7);
        var hudR2 = hudR1 + UiLineH(18);
        var hudR3 = hudR2 + UiLineH(20);
        var hudR4 = hudR3 + UiLineH(16);

        DrawTextLine(levelText, levelX, hudR1, 18, ColWhite);
        DrawTextLine(enemiesText, enemiesX, hudR1, 16, ColLightGray);
        if (topRightX > enemiesX + 120)
        {
            DrawTextLine(topRightText, topRightX, hudR1, topRightFont, topRightColor);
        }

        var hpText = $"HP {_player.CurrentHp}/{_player.MaxHp}";
        var packText = $"Pack: {GetInventoryQuantityTotal()} items";
        var hpX = 8;
        var packX = hpX + MeasureUiText(hpText, 20) + 24;
        DrawTextLine(hpText, hpX, hudR2, 20, ColGreen);
        DrawTextLine(packText, packX, hudR2, 16, ColYellow);
        var objectiveText = GetPhase3ObjectiveLabel();
        DrawTextClamped(objectiveText, hpX, hudR3, 16, Math.Max(220, hudW - hpX - pad), ColLightGray);
        var conditionsHudText = _settingsOptionalConditionsEnabled
            ? $"Conditions: {GetActiveMajorConditionSummary()} | Cure: Settings -> Accessibility -> Purge ({GetConditionPurgeCostLabel()})"
            : "Conditions: disabled in settings.";
        DrawTextClamped(
            conditionsHudText,
            hpX,
            hudR4,
            14,
            Math.Max(220, hudW - hpX - pad),
            _settingsOptionalConditionsEnabled && _activeMajorConditions.Count > 0 ? ColAccentRose : ColGray);

        if (_player.IsCasterClass)
        {
            var slotsText = $"Slots L1 {_player.GetSpellSlots(1)}/{_player.GetSpellSlotsMax(1)}  L2 {_player.GetSpellSlots(2)}/{_player.GetSpellSlotsMax(2)}  L3 {_player.GetSpellSlots(3)}/{_player.GetSpellSlotsMax(3)}";
            var slotsX = hudW - pad - MeasureUiText(slotsText, 16);
            if (slotsX > packX + 120)
            {
                DrawTextLine(slotsText, slotsX, hudR2, 16, ColLightGray);
            }
            else
            {
                DrawTextClamped(slotsText, packX, hudR2, 14, Math.Max(140, hudW - packX - pad), ColLightGray);
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
            DrawPanel(8, debugY, Math.Min(hudW - 16, 980), 30, new Color(9, 12, 20, 210), ColBorder);
            DrawTextClamped(debugText, 14, debugY + 7, 14, Math.Min(hudW - 28, 964), ColLightGray);
        }
    }

    private void DrawCombatUi()
    {
        if (_player == null || _currentEnemy == null) return;

        var w = Raylib.GetScreenWidth();
        var h = Raylib.GetScreenHeight();
        var submenuOpen =
            _gameState == GameState.CombatSkillMenu ||
            _gameState == GameState.CombatSpellMenu ||
            _gameState == GameState.CombatSpellTargeting ||
            _gameState == GameState.CombatItemMenu;
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
        var statusLabel = GetEnemyStatusSummary(_currentEnemy);
        var concentrationLabel = GetActiveConcentrationSummary();
        var targetRoster = string.Join("  |  ", aliveTargets.Take(3).Select(enemy =>
        {
            var marker = ReferenceEquals(enemy, _currentEnemy) ? ">" : " ";
            var distance = _player == null ? 0 : EncounterTargetingRules.GetTileDistance(_player.X, _player.Y, enemy.X, enemy.Y);
            return $"{marker}{enemy.Type.Name} {enemy.CurrentHp}hp {distance}t";
        }));
        if (aliveTargets.Count > 3)
        {
            targetRoster = $"{targetRoster}  |  +{aliveTargets.Count - 3} more";
        }

        var contentX = 12;
        var contentH = submenuOpen
            ? Math.Clamp(h / 5, 148, 164)
            : Math.Clamp(h / 5, 156, 172);
        var summaryPad = UiScale(6);
        var hasActiveBuffs = _divineFavorActive || _magicWeaponActive || _flameArrowsActive || _crusadersMantleActive || _zephyrStrikeActive
            || _shieldOfFaithActive || _blessActive || _heroismActive || _mageArmorActive || _shieldSpellActive
            || _barkskinActive || _blurActive || _hasteActive || _aidMaxHpBonus > 0
            || _mirrorImageCharges > 0 || _absorbElementsCharged || _expeditiousRetreatActive || _longstriderActive
            || _hexActive || _protFromEvilActive || _sanctuaryActive || _compelledDuelActive || _enhanceAbilityActive
            || _hellishRebukePrimed || _armorOfAgathysTempHp > 0 || _fireShieldActive || _wrathOfStormPrimed
            || _spiritShroudActive || _deathWardActive || _holyRebukePrimed || _thornsActive
            || _stoneskinActive || _cuttingWordsPrimed || _greaterInvisibilityActive
            || _counterspellPrimed || _invisibilityActive || _elementalWeaponActive || _revivifyUsed
            || _blinkActive || _protEnergyActive || _beaconOfHopeActive || _majorImageActive || _auraOfCourageActive;
        var hasActiveSummon = _activeSummon != null;
        var hasActiveTransformation = _activeTransformation != null;
        var summaryH = summaryPad + UiLineH(14) + UiLineH(13) + (hasActiveBuffs ? UiLineH(13) : 0) + (hasActiveSummon ? UiLineH(13) : 0) + (hasActiveTransformation ? UiLineH(13) : 0) + summaryPad;
        var bottomReserve = Math.Max(78, summaryH + 42);
        var contentY = h - contentH - bottomReserve;
        var colGap = 12;
        var actionW = Math.Clamp(w / 4, 250, 300);
        var contentW = w - contentX * 2;
        var logW = Math.Max(220, contentW - actionW - colGap);
        var summaryY = contentY + contentH + 8;

        // Log panel
        var logX = contentX;
        var logY = contentY;
        var logH = contentH;
        DrawPanel(logX, logY, logW, logH, ColPanelSoft, ColBorder);
        DrawTextLine("Combat Log", logX + 12, logY + 8, 18, ColSkyBlue);

        var lineStartY = logY + 34;
        var lineStep = _settingsVerboseCombatLog ? 22 : 24;
        var maxLogLines = Math.Max(1, (logH - 42) / lineStep);
        var targetLogLines = Math.Min(GetCombatLogVisibleLines(), maxLogLines);
        var lineY = lineStartY;
        foreach (var line in _combatLog.TakeLast(targetLogLines))
        {
            DrawTextClamped(line, logX + 14, lineY, 16, logW - 28, ColLightGray);
            lineY += lineStep;
        }

        // Actions panel
        var actionX = logX + logW + colGap;
        var actionH = submenuOpen
            ? Math.Clamp(h / 3, 204, 216)
            : Math.Clamp(h / 3, 216, 232);
        var actionY = h - actionH - 78;
        DrawPanel(actionX, actionY, actionW, actionH, ColPanelSoft, ColBorder);
        DrawTextLine("Actions", actionX + 12, actionY + 8, 18, ColSkyBlue);
        DrawTextClamped($"Target {activeTargetIndex + 1}/{Math.Max(1, aliveTargets.Count)}  {_currentEnemy.Type.Name}", actionX + 12, actionY + 34, 16, actionW - 24, ColAccentRose);
        DrawTextClamped($"HP {enemyHp}/{_currentEnemy.Type.MaxHp}  Dist {meleeValidation.DistanceTiles}/{meleeValidation.MaxRangeTiles}  {losLabel}", actionX + 12, actionY + 54, 14, actionW - 24, ColWhite);
        DrawTextClamped(attackReadinessLabel, actionX + 12, actionY + 72, 14, actionW - 24, meleeValidation.IsLegal ? ColGreen : ColYellow);

        var actions = GetCombatActions();
        var actionRowsTop = actionY + 92;
        var actionRowsBottom = actionY + actionH - 10;
        var actionRowStep = 24;
        var visibleActionRows = Math.Max(1, (actionRowsBottom - actionRowsTop) / actionRowStep);
        _selectedActionIndex = Math.Clamp(_selectedActionIndex, 0, Math.Max(0, actions.Count - 1));
        var actionOffset = 0;
        if (_selectedActionIndex >= visibleActionRows)
        {
            actionOffset = _selectedActionIndex - visibleActionRows + 1;
        }

        for (var slot = 0; slot < visibleActionRows; slot++)
        {
            var i = actionOffset + slot;
            if (i >= actions.Count) break;
            var selected = i == _selectedActionIndex;
            var rowY = actionRowsTop + slot * actionRowStep;
            DrawMenuRow(actionX + 10, rowY, actionW - 20, 22, selected);
            var marker = selected ? ">" : " ";
            DrawTextClamped($"{marker} {actions[i]}", actionX + 14, rowY + 2, 16, actionW - 28, selected ? ColYellow : ColWhite);
        }

        if (actionOffset > 0)
        {
            DrawCenteredText("...more above...", actionX + actionW / 2, actionRowsTop - 16, 11, ColGray);
        }
        if (actionOffset + visibleActionRows < actions.Count)
        {
            DrawCenteredText("...more below...", actionX + actionW / 2, actionRowsBottom - 2, 11, ColGray);
        }

        var armorStyleDefense = GetArmorStateDefenseBonus(_player);
        var armorStyleFlee = GetArmorStateFleeBonus(_player);
        var totalDefense = _player.DefenseBonus + GetClassDefenseBonus(_player) + _runDefenseBonus + armorStyleDefense + GetConditionDefenseModifier();
        var fleeChance = Math.Clamp(50 + _player.FleeBonus + GetClassFleeBonus(_player) + _runFleeBonus + armorStyleFlee + GetConditionFleeModifier(), 5, 95);
        var hpText = _playerTempHp > 0
            ? $"HP {_player.CurrentHp}/{_player.MaxHp} | Temp {_playerTempHp}"
            : $"HP {_player.CurrentHp}/{_player.MaxHp}";
        var moveText = $"Move {_combatMovePointsRemaining}/{_combatMovePointsMax}";
        var condSummary = GetActiveMajorConditionSummary();
        var playerCondText = _playerConditions.Count > 0
            ? "  |  " + string.Join(", ", _playerConditions.Select(c => $"{c.Kind}({c.RemainingTurns}t)"))
            : string.Empty;
        var sRow1Y = summaryY + summaryPad;
        var sRow2Y = sRow1Y + UiLineH(14);
        DrawPanel(12, summaryY, w - 24, summaryH, new Color(14, 18, 28, 220), ColBorder);
        DrawTextClamped($"{hpText}  |  {moveText}  |  DEF {totalDefense}  |  Flee {fleeChance}%{playerCondText}", 20, sRow1Y, 14, w - 40, _playerConditions.Count > 0 ? ColAccentRose : ColLightGray);
        DrawTextClamped($"Targets: {targetRoster}  |  Effects: {statusLabel}  |  Conc: {concentrationLabel}  |  Cond: {condSummary}", 20, sRow2Y, 13, w - 40, _settingsOptionalConditionsEnabled ? ColAccentRose : ColGray);
        if (hasActiveBuffs)
        {
            var buffParts = new List<string>();
            if (_divineFavorActive) buffParts.Add("Divine Favor +1d4");
            if (_magicWeaponActive) buffParts.Add("Magic Weapon +1d6");
            if (_flameArrowsActive) buffParts.Add("Flame Arrows +1d8");
            if (_crusadersMantleActive) buffParts.Add("Crusader's Mantle +1d6");
            if (_zephyrStrikeActive) buffParts.Add(_zephyrStrikeHitPrimed ? "Zephyr Strike +1d8 (primed)" : "Zephyr Strike (spent)");
            if (_shieldOfFaithActive) buffParts.Add("Shield of Faith +2 AC");
            if (_blessActive) buffParts.Add("Bless +1d4 atk/saves");
            if (_heroismActive) buffParts.Add("Heroism (temp HP/turn)");
            if (_mageArmorActive) buffParts.Add("Mage Armor +3 AC");
            if (_shieldSpellActive) buffParts.Add("Shield +5 AC (1 turn)");
            if (_barkskinActive) buffParts.Add("Barkskin AC\u226516");
            if (_blurActive) buffParts.Add("Blur (enemy disadv.)");
            if (_hasteActive) buffParts.Add("Haste +2 AC/move");
            if (_aidMaxHpBonus > 0) buffParts.Add($"Aid +{_aidMaxHpBonus} max HP");
            // Batch 2
            if (_mirrorImageCharges > 0) buffParts.Add($"Mirror Image ({_mirrorImageCharges})");
            if (_absorbElementsCharged) buffParts.Add("Absorb Elements (charged)");
            if (_expeditiousRetreatActive) buffParts.Add("Exp. Retreat +15% flee");
            if (_longstriderActive) buffParts.Add("Longstrider +2 move");
            if (_hexActive) buffParts.Add("Hex +1d4, enemy -2 atk");
            if (_protFromEvilActive) buffParts.Add("Protection +1 AC");
            if (_sanctuaryActive) buffParts.Add("Sanctuary");
            if (_compelledDuelActive) buffParts.Add("Compelled Duel +2 melee");
            if (_enhanceAbilityActive) buffParts.Add("Enhance Ability +2 AC/+3 flee");
            // Batch 3
            if (_hellishRebukePrimed) buffParts.Add("Hellish Rebuke (primed)");
            if (_armorOfAgathysTempHp > 0) buffParts.Add($"Armor of Agathys ({_armorOfAgathysTempHp} frost HP)");
            if (_fireShieldActive) buffParts.Add("Fire Shield (2d8 fire)");
            if (_wrathOfStormPrimed) buffParts.Add("Wrath of Storm (primed)");
            if (_spiritShroudActive) buffParts.Add("Spirit Shroud +1d8 melee/1d6 reactive");
            if (_deathWardActive) buffParts.Add("Death Ward (active)");
            if (_holyRebukePrimed) buffParts.Add("Holy Rebuke (primed)");
            if (_thornsActive) buffParts.Add("Thorns (1d6 pierce)");
            if (_stoneskinActive) buffParts.Add("Stoneskin (-3 dmg)");
            if (_cuttingWordsPrimed) buffParts.Add("Cutting Words (primed)");
            if (_greaterInvisibilityActive) buffParts.Add("Greater Invisibility (adv/disadv)");
            // Batch 4+5
            if (_counterspellPrimed) buffParts.Add("Counterspell (primed)");
            if (_invisibilityActive) buffParts.Add("Invisibility (-15% hit)");
            if (_elementalWeaponActive) buffParts.Add($"Elemental Weapon ({_elementalWeaponElement})");
            if (_revivifyUsed) buffParts.Add("Revivify (used)");
            if (_blinkActive) buffParts.Add("Blink (30% dodge)");
            if (_protEnergyActive) buffParts.Add($"Prot. Energy ({_protEnergyElement})");
            if (_beaconOfHopeActive) buffParts.Add("Beacon of Hope (next heal x2)");
            if (_majorImageActive) buffParts.Add("Major Image (25% dodge)");
            if (_auraOfCourageActive) buffParts.Add("Aura of Courage (-1 turn conds)");
            var sRow3Y = sRow2Y + UiLineH(13);
            DrawTextClamped($"Buffs: {string.Join("  |  ", buffParts)}", 20, sRow3Y, 13, w - 40, ColYellow);
        }
        if (hasActiveSummon)
        {
            var summonHp = _activeSummon!.Type.MaxHp == 0 ? "invulnerable" : $"{_activeSummon.CurrentHp}/{_activeSummon.Type.MaxHp} hp";
            var summonRowY = (hasActiveBuffs ? sRow2Y + UiLineH(13) + UiLineH(13) : sRow2Y + UiLineH(13));
            DrawTextClamped($"Summon: {_activeSummon.Type.Name} ({summonHp})", 20, summonRowY, 13, w - 40, ColGreen);
        }
        if (hasActiveTransformation)
        {
            var extraRows = (hasActiveBuffs ? 1 : 0) + (hasActiveSummon ? 1 : 0);
            var transformRowY = sRow2Y + UiLineH(13) * (1 + extraRows);
            DrawTextClamped($"Form: {_activeTransformation!.Form.Name} ({_activeTransformation.TempHpRemaining}/{_activeTransformation.Form.TempHp} temp HP)", 20, transformRowY, 13, w - 40, ColCyan);
        }

        DrawFooterBar(
            12,
            h - 34,
            w - 24,
            24);
        if (_combatMoveModeActive)
        {
            DrawCenteredTextClamped($"Move Mode: ARROWS/WASD step  |  ENTER/ESC end move  |  Remaining {_combatMovePointsRemaining}", w / 2, h - 30, 14, w - 40, ColLightGray);
        }
        else
        {
            var combatFooter = EnableRunMetaLayer
                ? $"UP/DOWN action  |  LEFT/RIGHT target  |  ENTER act  |  Wait passes turn  |  Arc {_milestoneArcChargesThisCombat}"
                : "UP/DOWN action  |  LEFT/RIGHT target  |  ENTER act  |  Wait passes turn";
            DrawCenteredTextClamped(combatFooter, w / 2, h - 30, 14, w - 40, ColLightGray);
        }
    }

    private static void GetCombatSubmenuPanelBounds(int screenW, int screenH, out int panelX, out int panelY, out int panelW, out int panelH)
    {
        panelW = Math.Min(Math.Clamp(screenW / 3 + 36, 360, 430), screenW - 32);
        panelH = Math.Min(Math.Clamp(screenH / 3 + 8, 204, 236), screenH - UiLayout.HudHeight - 110);
        panelY = screenH - panelH - 118;
        panelX = screenW - panelW - 16;
    }

    // Expanded centered panel for the spell menu — much more room with large spell rosters.
    private static void GetCombatSpellPanelBounds(int screenW, int screenH, out int panelX, out int panelY, out int panelW, out int panelH)
    {
        panelW = Math.Min(Math.Max(600, screenW * 2 / 3), screenW - 40);
        panelH = Math.Min(Math.Max(420, screenH - 160), screenH - 40);
        panelX = (screenW - panelW) / 2;
        panelY = (screenH - panelH) / 2;
    }

    private void DrawCombatSkillMenu()
    {
        if (_player == null) return;

        var skillIds = GetCombatSkills();
        if (skillIds.Count == 0) return;

        var w = Raylib.GetScreenWidth();
        var h = Raylib.GetScreenHeight();
        GetCombatSubmenuPanelBounds(w, h, out var panelX, out var panelY, out var panelW, out var panelH);
        var panelCenterX = panelX + panelW / 2;
        EnsureCombatSkillSelectionVisible(skillIds.Count);
        var visibleRows = Math.Max(1, Math.Min(SkillVisibleCount, (panelH - 118) / 28));
        _combatSkillMenuOffset = ClampMenuOffsetToVisibleCount(_selectedCombatSkillIndex, skillIds.Count, _combatSkillMenuOffset, visibleRows);
        var start = _combatSkillMenuOffset;
        var end = Math.Min(skillIds.Count, start + visibleRows);

        DrawPanel(panelX, panelY, panelW, panelH, ColPanelAlt, ColBorder);
        DrawCenteredText("Combat Skills", panelCenterX, panelY + 12, 28, ColSkyBlue);

        for (var i = start; i < end; i++)
        {
            var id = skillIds[i];
            var selected = i == _selectedCombatSkillIndex;
            var marker = selected ? "> " : "  ";
            var label = id switch
            {
                "second_wind"               => "Second Wind (1/combat)",
                "mana_shield"               => "Arcane Ward (1/combat)",
                "channel_divinity"          => "Channel Divinity (1/combat)",
                "cutting_words"             => "Cutting Words (1/combat)",
                "lay_on_hands"              => "Lay on Hands (1/combat)",
                "warrior_battle_cry_feat"   => "Battle Cry (1/combat)",
                "rogue_vanish_feat"         => "Vanish (1/combat)",
                "paladin_divine_smite_feat" => "Divine Smite (1/combat)",
                "mage_empower_spell_feat"     => "Empower Spell (1/combat)",
                "cleric_word_of_renewal_feat" => "Word of Renewal (1/combat)",
                "lucky_feat"               => "Lucky (1/combat)",
                "mage_metamagic_feat"      => "Heightened Spell (1/combat)",
                "ranger_sharpshooter_feat"   => "Sharpshooter (1/combat)",
                "paladin_divine_favor_feat"  => "Divine Favor (1/combat)",
                _                            => id
            };
            var affordable = true;
            var color = !affordable ? ColGray : selected ? ColYellow : ColWhite;
            var rowY = panelY + 48 + (i - start) * 28;
            DrawMenuRow(panelX + 18, rowY - 3, panelW - 36, 24, selected && affordable);
            DrawCenteredTextClamped($"{marker}{label}", panelCenterX, rowY + 2, 18, panelW - 48, color);
        }

        if (start > 0)
        {
            DrawCenteredText("...more above...", panelCenterX, panelY + 34, 14, ColGray);
        }
        if (end < skillIds.Count)
        {
            DrawCenteredText("...more below...", panelCenterX, panelY + panelH - 74, 14, ColGray);
        }

        var chosenId = skillIds[Math.Min(_selectedCombatSkillIndex, skillIds.Count - 1)];
        var desc = chosenId switch
        {
            "second_wind"      => "Recover HP based on Constitution, then enemy acts.",
            "mana_shield"      => "Channel arcane energy to absorb the next enemy attack (once per combat).",
            "channel_divinity" => $"Prime your next spell with divine power (+{_player?.ChannelDivinityBonus ?? 0} damage, 1/combat).",
            "cutting_words"    => "Undermine the enemy's attack with a biting insult — reduces their next roll by 1d4.",
            "lay_on_hands"            => $"Restore {_player?.LayOnHandsHeal ?? 0} HP through sacred touch, then enemy acts.",
            "lucky_feat"               => "Prime Lucky — next attack is a guaranteed critical hit.",
            "mage_metamagic_feat"      => "Heightened Spell — next spell forces the enemy to roll their save twice, taking the lower result.",
            "ranger_sharpshooter_feat" => "Precision shot — next physical attack ignores all armor and deals +5 bonus damage.",
            _                          => "Class combat feature."
        };
        DrawCenteredTextClamped(desc, panelCenterX, panelY + panelH - 52, 15, panelW - 24, ColLightGray);
        DrawFooterBar(panelX + 10, panelY + panelH - 22, panelW - 20, 16);
        DrawCenteredTextClamped("ENTER cast  |  ESC back", panelCenterX, panelY + panelH - 21, 13, panelW - 24, ColLightGray);
    }

    private void DrawFormSelectionUi()
    {
        var w = Raylib.GetScreenWidth();
        var h = Raylib.GetScreenHeight();
        GetCombatSubmenuPanelBounds(w, h, out var panelX, out var panelY, out var panelW, out var panelH);
        DrawPanel(panelX, panelY, panelW, panelH, ColPanel, ColBorder);

        var titleY = panelY + 8;
        DrawCenteredTextClamped("Choose a Form:", panelX + panelW / 2, titleY, 16, panelW - 16, ColYellow);

        var rowStartY = titleY + 26;
        var rowH = 20;
        var maxVisible = Math.Max(1, (panelH - 64) / rowH);
        var offset = Math.Max(0, _formSelectionIndex - maxVisible + 1);

        for (var i = 0; i < maxVisible && offset + i < _pendingFormOptions.Length; i++)
        {
            var idx = offset + i;
            var formId = _pendingFormOptions[idx];
            if (!SpellData.Forms.TryGetValue(formId, out var form)) continue;

            var selected = idx == _formSelectionIndex;
            var rowY = rowStartY + i * rowH;
            if (selected)
                DrawMenuRow(panelX + 6, rowY - 1, panelW - 12, rowH, true);

            var marker = selected ? ">" : " ";
            var stats = $"AC{form.FormAC} ATK+{form.AttackBonus} {form.DamageCount}d{form.DamageDice}+{form.DamageBonus} HP{form.TempHp}";
            DrawTextClamped($"{marker} {form.Name} — {stats}", panelX + 10, rowY + 2, 13, panelW - 20, selected ? ColYellow : ColWhite);
        }

        // Description of selected form
        if (_formSelectionIndex >= 0 && _formSelectionIndex < _pendingFormOptions.Length
            && SpellData.Forms.TryGetValue(_pendingFormOptions[_formSelectionIndex], out var selForm))
        {
            var descY = panelY + panelH - 38;
            DrawTextClamped(selForm.Description, panelX + 10, descY, 12, panelW - 20, ColCyan);
        }

        DrawCenteredTextClamped("ENTER select  |  ESC back", panelX + panelW / 2, panelY + panelH - 21, 13, panelW - 24, ColLightGray);
    }

    private void DrawCombatSpellMenu()
    {
        if (_player == null) return;

        var spells = _player.GetKnownSpells();
        if (spells.Count == 0) return;

        var w = Raylib.GetScreenWidth();
        var h = Raylib.GetScreenHeight();
        GetCombatSpellPanelBounds(w, h, out var panelX, out var panelY, out var panelW, out var panelH);
        var panelCenterX = panelX + panelW / 2;

        // Reserve bottom area for target/effect summary (3 rows + footer = ~90px)
        const int headerH = 52;
        const int footerH = 96;
        const int rowH = 44; // two-line rows: name line + description line
        var listTop = panelY + headerH;
        var listBottom = panelY + panelH - footerH;
        var visibleRows = Math.Max(1, (listBottom - listTop) / rowH);

        DrawPanel(panelX, panelY, panelW, panelH, ColPanelAlt, ColBorder);
        DrawCenteredText("— Spells —", panelCenterX, panelY + 10, 28, ColSkyBlue);

        _spellMenuOffset = ClampMenuOffsetToVisibleCount(_selectedSpellIndex, spells.Count, _spellMenuOffset, visibleRows);
        var start = _spellMenuOffset;
        var end = Math.Min(spells.Count, start + visibleRows);

        if (start > 0)
            DrawCenteredText("▲ more above", panelCenterX, listTop - 14, 13, ColGray);

        for (var i = start; i < end; i++)
        {
            var spell = spells[i];
            var selected = i == _selectedSpellIndex;
            var slots = spell.RequiresSlot ? _player.GetSpellSlots(spell.SpellLevel) : 0;
            var slotsMax = spell.RequiresSlot ? _player.GetSpellSlotsMax(spell.SpellLevel) : 0;
            var usable = !spell.RequiresSlot || slots > 0;
            var nameColor = !usable ? ColGray : selected ? ColYellow : ColWhite;
            var descColor = !usable ? new Color(80, 80, 80, 255) : selected ? new Color(220, 210, 130, 255) : ColLightGray;
            var tierLabel = spell.IsCantrip ? "Cantrip" : $"Lv {spell.SpellLevel}";
            var familyLabel = SpellData.GetCombatFamilyLabel(spell);
            var costLabel = spell.IsCantrip ? "free" : $"{slots}/{slotsMax} slots";
            var marker = selected ? "▶ " : "   ";

            var rowY = listTop + (i - start) * rowH;
            DrawMenuRow(panelX + 12, rowY, panelW - 24, rowH - 4, selected && usable);

            // Name line
            DrawCenteredTextClamped(
                $"{marker}{spell.Name}   [{tierLabel} · {familyLabel}]   {costLabel}",
                panelCenterX,
                rowY + 6,
                20,
                panelW - 48,
                nameColor);

            // Description line (trimmed to single sentence for brevity)
            var desc = spell.Description ?? string.Empty;
            var dotIdx = desc.IndexOf('.');
            var shortDesc = dotIdx > 0 ? desc[..(dotIdx + 1)] : (desc.Length > 80 ? desc[..80] + "…" : desc);
            DrawCenteredTextClamped(
                shortDesc,
                panelCenterX,
                rowY + 26,
                14,
                panelW - 64,
                descColor);
        }

        if (end < spells.Count)
            DrawCenteredText("▼ more below", panelCenterX, listTop + visibleRows * rowH + 2, 13, ColGray);

        // Bottom info panel
        var infoY = panelY + panelH - footerH + 4;
        var selectedSpell = spells[Math.Min(_selectedSpellIndex, spells.Count - 1)];
        var spellValidation = ValidateCurrentEnemyTargetForSpell(selectedSpell);
        var spellLosLabel = spellValidation.HasLineOfSight ? "LOS ✓" : "LOS blocked";
        var spellTargetColor = spellValidation.IsLegal ? ColGreen : ColYellow;
        var selectedSpellTargetSummary = BuildSpellAffectedTargetSummary(selectedSpell);
        var selectedSpellEffectSummary = BuildSpellEffectSummary(selectedSpell);

        // Horizontal divider
        Raylib.DrawLine(panelX + 16, infoY - 4, panelX + panelW - 16, infoY - 4, ColBorder);

        DrawCenteredTextClamped(
            UsesSelfCenteredSpellTargeting(selectedSpell)
                ? $"Self-centered  ·  Dist {spellValidation.DistanceTiles}/{spellValidation.MaxRangeTiles}  ·  {spellLosLabel}"
                : $"Target: {_currentEnemy?.Type.Name ?? "None"}  ·  Dist {spellValidation.DistanceTiles}/{spellValidation.MaxRangeTiles}  ·  {spellLosLabel}",
            panelCenterX, infoY + 2, 16, panelW - 32, spellTargetColor);
        DrawCenteredTextClamped(selectedSpellTargetSummary, panelCenterX, infoY + 22, 15, panelW - 32, ColLightGray);
        DrawCenteredTextClamped(selectedSpellEffectSummary, panelCenterX, infoY + 42, 15, panelW - 32, ColSkyBlue);

        DrawFooterBar(panelX + 10, panelY + panelH - 22, panelW - 20, 16);
        DrawCenteredTextClamped("↑↓ navigate  |  ENTER select  |  ESC back", panelCenterX, panelY + panelH - 21, 13, panelW - 24, ColLightGray);
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
        var h = Raylib.GetScreenHeight();
        GetCombatSubmenuPanelBounds(w, h, out var panelX, out var panelY, out var panelW, out var panelH);
        var panelCenterX = panelX + panelW / 2;
        var tierLabel = pendingSpell.IsCantrip ? "Cantrip" : $"L{pendingSpell.SpellLevel}";
        var validation = ValidateCombatSpellAim(pendingSpell);
        var targetName = GetSpellTargetDescriptor(pendingSpell);
        var variantLabel = GetSelectedPendingSpellVariantLabel(pendingSpell);
        var losLabel = validation.HasLineOfSight ? "LOS clear" : "LOS blocked";
        var legalityLabel = validation.IsLegal
            ? "Cast is legal."
            : $"Blocked: {validation.BlockedReason}";
        var effectSummary = BuildSpellEffectSummary(pendingSpell);
        var affectedSummary = BuildSpellAffectedTargetSummary(pendingSpell);
        var aliveTargets = GetAliveEncounterEnemies();
        var activeTargetIndex = aliveTargets.FindIndex(enemy => ReferenceEquals(enemy, _currentEnemy));
        if (activeTargetIndex < 0)
        {
            activeTargetIndex = 0;
        }

        DrawPanel(panelX, panelY, panelW, panelH, ColPanelAlt, ColBorder);
        DrawCenteredText("Spell Targeting", panelCenterX, panelY + 10, 30, ColSkyBlue);
        DrawCenteredTextClamped($"{pendingSpell.Name} ({tierLabel})", panelCenterX, panelY + 52, 23, panelW - 24, ColYellow);
        if (!string.IsNullOrWhiteSpace(variantLabel))
        {
            DrawCenteredTextClamped(variantLabel, panelCenterX, panelY + 71, 15, panelW - 24, ColSkyBlue);
        }
        DrawCenteredTextClamped(
            UsesFreeTileSpellTargeting(pendingSpell)
                ? $"{targetName}"
                : $"Target {targetName}  {activeTargetIndex + 1}/{Math.Max(1, aliveTargets.Count)}",
            panelCenterX,
            panelY + 86,
            19,
            panelW - 24,
            ColWhite);
        DrawCenteredTextClamped(
            $"Range {validation.DistanceTiles}/{validation.MaxRangeTiles}  {losLabel}",
            panelCenterX,
            panelY + 114,
            16,
            panelW - 24,
            validation.IsLegal ? ColGreen : ColYellow);
        DrawCenteredTextClamped(legalityLabel, panelCenterX, panelY + 141, 15, panelW - 24, validation.IsLegal ? ColGreen : ColYellow);
        DrawCenteredTextClamped(affectedSummary, panelCenterX, panelY + panelH - 56, 15, panelW - 24, ColLightGray);
        DrawCenteredTextClamped(effectSummary, panelCenterX, panelY + panelH - 38, 14, panelW - 24, ColSkyBlue);
        DrawFooterBar(panelX + 10, panelY + panelH - 22, panelW - 20, 16);
        DrawCenteredTextClamped(
            UsesSelfCenteredSpellTargeting(pendingSpell)
                ? "ENTER confirm cast  |  ESC cancel"
                : UsesFreeTileSpellTargeting(pendingSpell)
                    ? "ARROWS/WASD move anchor  |  ENTER confirm cast  |  ESC cancel"
                    : "LEFT/RIGHT cycle target  |  ENTER confirm cast  |  ESC cancel",
            panelCenterX,
            panelY + panelH - 21,
            13,
            panelW - 24,
            ColLightGray);
        if (SpellSupportsVariantSelection(pendingSpell))
        {
            DrawCenteredTextClamped("UP/DOWN change mode", panelCenterX, panelY + panelH - 68, 14, panelW - 24, ColGray);
        }
    }

    private void DrawCombatItemMenu()
    {
        if (_player == null) return;

        var items = GetCombatConsumables();
        if (items.Count == 0) return;

        var w = Raylib.GetScreenWidth();
        var h = Raylib.GetScreenHeight();
        GetCombatSubmenuPanelBounds(w, h, out var panelX, out var panelY, out var panelW, out var panelH);
        var panelCenterX = panelX + panelW / 2;
        var listTop = panelY + 48;
        var listBottom = panelY + panelH - 76;
        var visibleRows = Math.Max(1, (listBottom - listTop) / 28);

        DrawPanel(panelX, panelY, panelW, panelH, ColPanelAlt, ColBorder);
        DrawCenteredText("Consumables", panelCenterX, panelY + 10, 30, ColSkyBlue);

        _combatItemMenuOffset = ClampMenuOffsetToVisibleCount(_selectedCombatItemIndex, items.Count, _combatItemMenuOffset, visibleRows);
        var start = _combatItemMenuOffset;
        var end = Math.Min(items.Count, start + visibleRows);
        for (var i = start; i < end; i++)
        {
            var item = items[i];
            var selected = i == _selectedCombatItemIndex;
            var marker = selected ? "> " : "  ";
            var rowY = panelY + 48 + (i - start) * 28;
            DrawMenuRow(panelX + 20, rowY - 3, panelW - 40, 24, selected);
            DrawCenteredText(
                $"{marker}{item.Name}  x{item.Quantity}",
                panelCenterX,
                panelY + 52 + (i - start) * 28,
                18,
                selected ? ColYellow : ColWhite);
        }

        if (start > 0)
        {
            DrawCenteredText("...more above...", panelCenterX, panelY + 36, 14, ColGray);
        }
        if (end < items.Count)
        {
            DrawCenteredText("...more below...", panelCenterX, panelY + panelH - 76, 14, ColGray);
        }

        var selectedItem = items[Math.Min(_selectedCombatItemIndex, items.Count - 1)];
        var description = selectedItem.Id switch
        {
            "health_potion" => "Restore 35% HP. Consumes your turn.",
            "healing_draught" => "Restore 35% HP. Consumes your turn.",
            "sharpening_oil" => "Gain +1 melee damage for this run. Consumes your turn.",
            _ => selectedItem.Description
        };
        DrawCenteredTextClamped(description, panelCenterX, panelY + panelH - 44, 15, panelW - 24, ColLightGray);
        DrawFooterBar(panelX + 10, panelY + panelH - 22, panelW - 20, 16);
        DrawCenteredTextClamped("ENTER use  |  ESC back", panelCenterX, panelY + panelH - 21, 13, panelW - 24, ColLightGray);
    }


    private int GetCharacterSheetContentHeight()
    {
        if (_player == null) return 0;

        var screenW = Raylib.GetScreenWidth();
        var rightX = 290;
        var rightW = screenW - rightX - 46;
        var height = 0;
        height += 28 + 20 + 20 + 24; // progression
        height += 28 + 20 + 20 + 20 + 20 + 20; // combat profile
        if (_player.IsCasterClass) height += 40; // casting stat line + spell damage bonus line
        height += 8 + 28 + 20 + 20 + 20; // armor profile
        height += 8;
        height += 28 + 20 + 20 + 20 + 20 + 20 + 20; // run identity fixed lines
        height += MeasureWrappedTextHeight(GetPhase3ObjectiveLabel(), Math.Max(200, rightW - 24), 14) + 8;
        height += 20 + 24; // major conditions header and purge line
        if (_activeMajorConditions.Count == 0)
        {
            height += 20;
        }
        else
        {
            height += _activeMajorConditions.Count * (18 + 34);
            foreach (var condition in _activeMajorConditions)
            {
                height += MeasureWrappedTextHeight(GetMajorConditionEffectSummary(condition.Type), Math.Max(180, rightW - 40), 13) - 34;
            }
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
        DrawCenteredTextClamped($"{_player.Name} - {_player.Race} {_player.Gender} {_player.CharacterClass.Name}", w / 2, 74, 20, w - 120, ColLightGray);

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
        Raylib.DrawText($"XP {_player.Xp}/{_player.XpToNextLevel}", leftX + 10, ly, 18, ColLightGray);

        ClampCharacterSheetScroll();

        var classMeleeBonus = GetClassMeleeDamageBonus(_player);
        var classSpellBonus = GetClassSpellDamageBonus(_player);
        var classCritRangeBonus = GetClassCritRangeBonus(_player);
        var classDefenseBonus = GetClassDefenseBonus(_player);
        var classFleeBonus = GetClassFleeBonus(_player);
        var armorStyleDefense = GetArmorStateDefenseBonus(_player);
        var armorStyleFlee = GetArmorStateFleeBonus(_player);
        var equippedArmorItem = GetEquippedArmorItem();
        var armorState = GetCurrentArmorCategory();
        var armorStateLabel = GetArmorStateLabel(armorState);
        var armorTrainingSummary = GetArmorTrainingSummary(_player);
        var totalFleeChance = Math.Clamp(50 + _player.FleeBonus + classFleeBonus + _runFleeBonus + armorStyleFlee + GetConditionFleeModifier(), 5, 95);
        var hudCritThreshold = Math.Max(2, _player.CritThreshold - classCritRangeBonus - _runCritBonus);
        var hudWeapon = GetEquippedWeaponDef();
        var hudAtkStat = hudWeapon.IsFinesse
            ? (_player.Mod(StatName.Strength) >= _player.Mod(StatName.Dexterity) ? StatName.Strength : StatName.Dexterity)
            : (hudWeapon.IsRanged ? StatName.Dexterity : hudWeapon.AttackStat);
        var hudAtkBonus = _player.Mod(hudAtkStat) + GetProficiencyBonus();
        var hudAtkBonusStr = hudAtkBonus >= 0 ? $"+{hudAtkBonus}" : $"{hudAtkBonus}";
        var rightTextW = Math.Max(220, rightW - 24);

        // Right panel content supports scrolling for long builds.
        Raylib.BeginScissorMode(rightX + 1, rightY + 1, rightW - 2, rightH - 2);
        var ry = rightY + 12 - _characterSheetScroll * 22;

        Raylib.DrawText("Progression", rightX + 10, ry, 22, ColSkyBlue);
        ry += 28;
        DrawTextClamped($"Level {_player.Level}   Class {_player.CharacterClass.Name}", rightX + 10, ry, 16, rightTextW, ColLightGray);
        ry += 20;
        DrawTextClamped($"XP: {_player.Xp}/{_player.XpToNextLevel}", rightX + 10, ry, 16, rightTextW, ColLightGray);
        ry += 20;
        DrawTextClamped($"Unspent points -> Stat {_player.StatPoints} / Feat {_player.FeatPoints} / Spell {_player.SpellPickPoints}", rightX + 10, ry, 15, rightTextW, ColLightGray);
        ry += 24;

        Raylib.DrawText("Combat Profile", rightX + 10, ry, 22, ColYellow);
        ry += 28;
        DrawTextClamped($"Role: {GetClassCombatTag(_player.CharacterClass.Name)}", rightX + 10, ry, 16, rightTextW, ColLightGray);
        ry += 20;
        DrawTextClamped($"Attack: d20{hudAtkBonusStr} ({hudWeapon.Name} {hudWeapon.DiceCount}d{hudWeapon.DamageDice})", rightX + 10, ry, 16, rightTextW, ColLightGray);
        ry += 20;
        DrawTextClamped($"Crit range: {hudCritThreshold}+ on d20", rightX + 10, ry, 16, rightTextW, ColLightGray);
        ry += 20;
        DrawTextClamped($"AC: {GetPlayerArmorClass()} ({armorStateLabel})", rightX + 10, ry, 16, rightTextW, ColLightGray);
        ry += 20;
        DrawTextClamped($"Flee chance: {totalFleeChance}%", rightX + 10, ry, 16, rightTextW, ColLightGray);
        ry += 20;
        if (_player.IsCasterClass)
        {
            var castingStatLabel = GetStatShortLabel(_player.CastingStat);
            var castingMod = _player.Mod(_player.CastingStat);
            var castingModStr = castingMod >= 0 ? $"+{castingMod}" : $"{castingMod}";
            DrawTextClamped($"Casting stat: {castingStatLabel} ({castingModStr})  Save DC: {8 + (2 + Math.Max(0, (_player.Level - 1) / 4)) + Math.Max(0, castingMod)}", rightX + 10, ry, 16, rightTextW, ColLightGray);
            ry += 20;
            DrawTextClamped($"Spell damage bonus: {_player.SpellDamageBonus + classSpellBonus + _runSpellBonus + GetConditionSpellModifier()}", rightX + 10, ry, 16, rightTextW, ColLightGray);
            ry += 20;
        }

        var armorItemLabel = equippedArmorItem == null ? "None" : equippedArmorItem.Name;
        var armorStyleFleeLabel = armorStyleFlee >= 0 ? $"+{armorStyleFlee}%" : $"{armorStyleFlee}%";
        ry += 8;
        Raylib.DrawText("Armor Profile", rightX + 10, ry, 22, ColSkyBlue);
        ry += 28;
        DrawTextClamped($"Equipped: {armorItemLabel} ({armorStateLabel})", rightX + 10, ry, 16, rightTextW, ColLightGray);
        ry += 20;
        DrawTextClamped($"Training: {armorTrainingSummary}", rightX + 10, ry, 16, rightTextW, ColLightGray);
        ry += 20;
        DrawTextClamped($"Armor flee modifier: {armorStyleFleeLabel}", rightX + 10, ry, 16, rightTextW, ColLightGray);
        ry += 20;

        ry += 8;
        Raylib.DrawText(EnableRunMetaLayer ? "Run Identity" : "Dungeon Progress", rightX + 10, ry, 22, ColSkyBlue);
        ry += 28;
        if (EnableRunMetaLayer)
        {
            DrawTextClamped($"Archetype: {GetRunArchetypeLabel(_runArchetype)}", rightX + 10, ry, 16, rightTextW, ColLightGray);
            ry += 20;
            DrawTextClamped($"Relic: {GetRunRelicLabel(_runRelic)}", rightX + 10, ry, 16, rightTextW, ColLightGray);
            ry += 20;
            DrawTextClamped($"Doctrines: {GetMilestoneRanksLabel()}", rightX + 10, ry, 16, rightTextW, ColLightGray);
            ry += 20;
            DrawTextClamped($"Combat charges: Arc {_milestoneArcChargesThisCombat}  Escape {_milestoneEscapeChargesThisCombat}", rightX + 10, ry, 16, rightTextW, ColLightGray);
            ry += 20;
            var xpRouteLabel = _phase3XpPercentMod > 0
                ? $"+{_phase3XpPercentMod}%"
                : $"{_phase3XpPercentMod}%";
            DrawTextClamped($"Route pressure: XP {xpRouteLabel}  Enemy atk +{_phase3EnemyAttackBonus}", rightX + 10, ry, 16, rightTextW, ColLightGray);
            ry += 20;
        }
        DrawTextClamped($"Current area: {GetFloorZoneLabel(_currentFloorZone)}", rightX + 10, ry, 16, rightTextW, ColLightGray);
        ry += 20;
        ry += DrawWrappedText(GetPhase3ObjectiveLabel(), rightX + 10, ry, Math.Max(200, rightW - 24), 14, ColLightGray) + 8;
        Raylib.DrawText("Major Conditions", rightX + 10, ry, 20, ColAccentRose);
        ry += 24;
        if (_activeMajorConditions.Count == 0)
        {
            DrawTextClamped(_settingsOptionalConditionsEnabled ? "None active." : "Conditions disabled.", rightX + 10, ry, 15, rightTextW, ColLightGray);
            ry += 20;
        }
        else
        {
            foreach (var condition in _activeMajorConditions)
            {
                DrawTextClamped($"{GetMajorConditionLabel(condition.Type)} ({condition.Source})", rightX + 10, ry, 15, rightTextW, ColWhite);
                ry += 18;
                ry += DrawWrappedText(GetMajorConditionEffectSummary(condition.Type), rightX + 20, ry, Math.Max(180, rightW - 40), 13, ColLightGray) + 6;
            }
        }
        DrawTextClamped($"High-tier purge: {GetConditionPurgeCostLabel()}", rightX + 10, ry, 14, rightTextW, ColSkyBlue);
        ry += 22;
        ry += 8;

        Raylib.DrawText("Class Features", rightX + 10, ry, 22, ColWhite);
        ry += 28;
        if (_player.Skills.Count == 0)
        {
            Raylib.DrawText("No class features yet.", rightX + 10, ry, 18, ColGray);
            ry += 24;
        }
        else
        {
            foreach (var skill in _player.Skills)
            {
                DrawTextClamped(skill.Name, rightX + 10, ry, 16, rightTextW, ColWhite);
                ry += 18;
                DrawTextClamped(_player.GetSkillEffectText(skill), rightX + 18, ry, 14, rightTextW - 8, ColLightGray);
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
                DrawTextClamped(feat.Name, rightX + 10, ry, 16, rightTextW, ColWhite);
                ry += 18;
                DrawTextClamped(_player.GetFeatEffectText(feat), rightX + 18, ry, 14, rightTextW - 8, ColLightGray);
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
                DrawTextClamped($"{tierLabel} {spell.Name}", rightX + 10, ry, 15, rightTextW, ColLightGray);
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
        var panelTop = UiLayout.LevelPanelInsetY;
        var headerY = panelTop + UiScale(14);
        DrawCenteredText("LEVEL UP!", w / 2, headerY, 34, ColYellow);

        var subY = headerY + UiLineH(34);
        DrawCenteredText($"Level {_player.Level - 1}  →  Level {_player.Level}", w / 2, subY, 16, ColLightGray);

        var pendingY = subY + UiLineH(16);
        var pendingParts = new System.Text.StringBuilder();
        pendingParts.Append($"◆ {_player.StatPoints} stat point{(_player.StatPoints == 1 ? "" : "s")}");
        if (_player.FeatPoints > 0)  pendingParts.Append($"  ·  {_player.FeatPoints} feat{(_player.FeatPoints == 1 ? "" : "s")}");
        if (_player.SpellPickPoints > 0) pendingParts.Append($"  ·  {_player.SpellPickPoints} spell{(_player.SpellPickPoints == 1 ? "" : "s")}");
        DrawCenteredText(pendingParts.ToString(), w / 2, pendingY, 14, ColSkyBlue);

        var dividerY = pendingY + UiLineH(14) + UiScale(6);
        Raylib.DrawLine(w / 2 - 200, dividerY, w / 2 + 200, dividerY, ColBorder);

        var rowHeight = UiLineH(20) + 8;
        var totalRows = StatOrder.Length + 3;
        var listWidth = Math.Min(560, w - 220);
        var listX = w / 2 - listWidth / 2;
        var listAreaStart = dividerY + UiScale(8);
        var footerTop = h - UiScale(72);
        var listAreaH = footerTop - listAreaStart - UiScale(8);
        var levelListTop = listAreaStart + Math.Max(0, (listAreaH - totalRows * rowHeight) / 2);
        var rowH = rowHeight - 2; // inner row panel height (rowHeight includes the gap)
        for (var i = 0; i < totalRows; i++)
        {
            // Draw a thin divider before the action rows (Undo/Reset/Continue).
            if (i == StatOrder.Length)
            {
                var divY = levelListTop + i * rowHeight - UiScale(5);
                Raylib.DrawLine(listX, divY, listX + listWidth, divY, ColBorder);
            }

            var selected = i == _selectedStatIndex;
            var rowY = levelListTop + i * rowHeight;
            DrawMenuRow(listX, rowY - 4, listWidth, rowH, selected);
            if (i < StatOrder.Length)
            {
                var stat = StatOrder[i];
                var allocated = _levelUpAllocatedStats[i];
                var allocLabel = allocated > 0 ? $"  (+{allocated})" : string.Empty;
                DrawTextClamped($"{stat}: {_player.Stats.Get(stat)}{allocLabel}", listX + 12, rowY, 20, listWidth - 24, selected ? ColYellow : ColWhite);
            }
            else
            {
                var label = i switch
                {
                    var undoRow when undoRow == StatOrder.Length => "↩ Undo Last",
                    var resetRow when resetRow == StatOrder.Length + 1 => "⟳ Reset All",
                    _ => _player.StatPoints > 0
                        ? $"► Continue  ({_player.StatPoints} point{(_player.StatPoints == 1 ? string.Empty : "s")} remaining)"
                        : "► Continue to feat / spell / skill picks"
                };
                var color = i == StatOrder.Length + 2 && _player.StatPoints > 0
                    ? ColGray
                    : selected ? ColYellow : ColLightGray;
                DrawTextClamped(label, listX + 12, rowY, 18, listWidth - 24, color);
            }
        }

        var hintY = footerTop - UiLineH(16) - UiScale(4);
        if (_selectedStatIndex < StatOrder.Length)
        {
            var selectedStat = StatOrder[Math.Clamp(_selectedStatIndex, 0, StatOrder.Length - 1)];
            var selectedMod = _player.Mod(selectedStat);
            DrawCenteredText($"Selected: {selectedStat} (modifier {selectedMod:+#;-#;0})  |  RIGHT/ENTER add  |  LEFT remove", w / 2, hintY, 15, ColLightGray);
        }
        else
        {
            DrawCenteredText(_levelUpSessionActive
                ? "ENTER confirm row  |  ESC undo last allocation"
                : "Review level-up choices before continuing",
                w / 2, hintY, 15, ColLightGray);
        }

        if (!string.IsNullOrWhiteSpace(_selectionMessage))
        {
            var msgBarY = footerTop - UiScale(4);
            DrawFooterBar(UiLayout.LevelFooterX, msgBarY, w - UiLayout.LevelFooterInset, UiLineH(13) + UiScale(4));
            DrawCenteredText(_selectionMessage, w / 2, msgBarY + UiScale(3), 13, ColLightGray);
        }

        DrawFooterBar(UiLayout.LevelFooterX, footerTop, w - UiLayout.LevelFooterInset, UiLineH(14) + UiScale(6));
        DrawCenteredText("UP/DOWN browse  |  RIGHT/ENTER add  |  LEFT remove  |  ESC undo last  |  Continue when ready", w / 2, footerTop + UiScale(4), 14, ColLightGray);
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
        var featFooterTop = string.IsNullOrWhiteSpace(_selectionMessage) ? h - 58 : h - 82;
        var featListTop = 142 + Math.Max(0, (featFooterTop - 30 - 142 - (end - start) * 68) / 2);
        var selectionRowTextW = w - UiLayout.SelectionRowInset - 24;
        for (var i = start; i < end; i++)
        {
            var feat = _featChoices[i];
            var selected = i == _selectedFeatIndex;
            var y = featListTop + (i - start) * 68;
            var canLearn = _player.CanLearnFeat(feat, out var blockReason);
            var nameColor = canLearn ? (selected ? ColYellow : ColWhite) : ColGray;
            var statusColor = canLearn ? ColGreen : ColRed;

            DrawMenuRow(UiLayout.SelectionRowX, y - 8, w - UiLayout.SelectionRowInset, 62, selected);
            DrawCenteredTextClamped(feat.Name, w / 2, y, 25, selectionRowTextW, nameColor);
            DrawCenteredTextClamped($"Effect: {GetFeatEffectLabel(feat)}", w / 2, y + 22, 15, selectionRowTextW, selected ? ColLightGray : ColGray);

            var statusText = canLearn
                ? $"Ready. Req: {GetFeatPrerequisiteLabel(feat)}"
                : $"Locked: {blockReason}";
            DrawCenteredTextClamped(statusText, w / 2, y + 40, 13, selectionRowTextW, statusColor);
        }

        if (start > 0)
        {
            DrawCenteredText("...more above...", w / 2, featListTop - 20, 14, ColGray);
        }
        if (end < _featChoices.Count)
        {
            DrawCenteredText("...more below...", w / 2, featListTop + (end - start) * 68 + 4, 14, ColGray);
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

        DrawCenteredText("New Class Feature!", w / 2, 62, 34, ColYellow);
        DrawCenteredText($"{_player.CharacterClass.Name} — Level {_player.Level}", w / 2, 102, 20, ColSkyBlue);

        var listTop = 160;
        var rowH = 80;
        var textW = w - UiLayout.SelectionRowInset - 24;
        for (var i = 0; i < _skillChoices.Count; i++)
        {
            var skill = _skillChoices[i];
            var y = listTop + i * rowH;
            DrawMenuRow(UiLayout.SelectionRowX, y - 8, w - UiLayout.SelectionRowInset, 72, selected: true);
            DrawCenteredTextClamped(skill.Name, w / 2, y, 26, textW, ColYellow);
            DrawCenteredTextClamped(skill.Description, w / 2, y + 28, 15, textW, ColLightGray);
            DrawCenteredTextClamped($"Effect: {_player.GetSkillEffectText(skill)}", w / 2, y + 50, 13, textW, ColGreen);
        }

        DrawFooterBar(UiLayout.SelectionFooterX, h - 58, w - UiLayout.SelectionFooterInset, 18);
        DrawCenteredText("ENTER to continue", w / 2, h - 57, 13, ColLightGray);
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

        var spellFooterTop = string.IsNullOrWhiteSpace(_selectionMessage) ? h - 58 : h - 82;
        var spellVisibleCount = SpellLearnVisibleCount;
        _spellLearnMenuOffset = ClampMenuOffsetToVisibleCount(_selectedSpellLearnIndex, _spellLearnChoices.Count, _spellLearnMenuOffset, spellVisibleCount);
        var start = _spellLearnMenuOffset;
        var end = Math.Min(_spellLearnChoices.Count, start + spellVisibleCount);
        var spellListTop = 134 + Math.Max(0, (spellFooterTop - 30 - 134 - (end - start) * 68) / 2);
        var selectionRowTextW = w - UiLayout.SelectionRowInset - 24;
        for (var i = start; i < end; i++)
        {
            var spell = _spellLearnChoices[i];
            var selected = i == _selectedSpellLearnIndex;
            var y = spellListTop + (i - start) * 68;
            var tier = spell.IsCantrip ? "Cantrip" : $"Level {spell.SpellLevel}";
            var familyLabel = SpellData.GetCombatFamilyLabel(spell);
            var canLearn = _player.CanLearnSpell(spell, out var blockReason);
            var known = _player.KnowsSpell(spell.Id);
            var nameColor = canLearn ? (selected ? ColYellow : ColWhite) : ColGray;
            var statusColor = canLearn ? ColGreen : ColRed;

            DrawMenuRow(UiLayout.SelectionRowX, y - 8, w - UiLayout.SelectionRowInset, 58, selected);
            DrawCenteredTextClamped($"{spell.Name} ({tier}) [{familyLabel}]", w / 2, y, 24, selectionRowTextW, nameColor);
            DrawCenteredTextClamped(BuildSpellEffectSummary(spell), w / 2, y + 22, 15, selectionRowTextW, selected ? ColLightGray : ColGray);

            var statusText = canLearn
                ? "Learnable now"
                : known
                    ? "Locked: Already learned."
                    : $"Locked: {blockReason}";
            DrawCenteredTextClamped(statusText, w / 2, y + 42, 13, selectionRowTextW, statusColor);
        }

        if (start > 0)
        {
            DrawCenteredText("...more above...", w / 2, spellListTop - 20, 14, ColGray);
        }
        if (end < _spellLearnChoices.Count)
        {
            DrawCenteredText("...more below...", w / 2, spellListTop + (end - start) * 68 + 4, 14, ColGray);
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
        var panelX = UiLayout.RewardPanelInsetX;
        var panelY = UiLayout.RewardPanelInsetY;
        var panelW = w - UiLayout.RewardPanelInsetX * 2;
        var panelH = h - UiLayout.RewardPanelInsetY * 2;
        Raylib.DrawRectangle(0, 0, w, h, new Color(0, 0, 0, 215));
        DrawPanel(
            panelX,
            panelY,
            panelW,
            panelH,
            ColPanel,
            ColBorder);

        DrawCenteredTextClamped(_activeRewardNode.Name, w / 2, 74, 36, panelW - 24, ColYellow);
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
        DrawCenteredTextClamped(headerDescription, w / 2, 116, 18, panelW - 30, ColLightGray);
        DrawCenteredTextClamped($"Build: {GetRunIdentityLabel()}", w / 2, 138, 15, panelW - 30, ColSkyBlue);

        var optionNames = GetActiveRewardOptionNames();
        var optionDescriptions = GetActiveRewardOptionDescriptions();
        var optionW = w - UiLayout.RewardOptionInset;
        var optionTextW = Math.Max(220, optionW - 24);

        for (var i = 0; i < optionNames.Length; i++)
        {
            var selected = i == _selectedRewardOptionIndex;
            var rowY = 174 + i * 86;
            DrawMenuRow(UiLayout.RewardOptionX, rowY - 8, w - UiLayout.RewardOptionInset, 74, selected);
            DrawCenteredTextClamped(optionNames[i], w / 2, rowY + 2, 26, optionTextW, selected ? ColYellow : ColWhite);
            DrawCenteredTextClamped(optionDescriptions[i], w / 2, rowY + 34, 16, optionTextW, selected ? ColLightGray : ColGray);
        }

        var detailY = 174 + optionNames.Length * 86;
        var detailH = h - detailY - 132;
        if (detailH >= 68)
        {
            DrawPanel(UiLayout.RewardOptionX, detailY, optionW, detailH, ColPanelAlt, ColBorder);
            DrawTextClamped("Selected option", UiLayout.RewardOptionX + 12, detailY + 10, 17, optionW - 24, ColSkyBlue);
            DrawWrappedText(
                optionDescriptions[Math.Clamp(_selectedRewardOptionIndex, 0, optionDescriptions.Length - 1)],
                UiLayout.RewardOptionX + 12,
                detailY + 34,
                optionW - 24,
                15,
                ColLightGray);
        }

        DrawFooterBar(UiLayout.RewardFooterX, h - 88, w - UiLayout.RewardFooterInset, 22);
        DrawCenteredTextClamped(_rewardMessage, w / 2, h - 86, 14, w - UiLayout.RewardFooterInset - 24, ColLightGray);
        DrawFooterBar(UiLayout.RewardFooterX, h - 58, w - UiLayout.RewardFooterInset, 22);
        DrawCenteredTextClamped("UP/DOWN choose reward  |  ENTER claim  |  ESC defer", w / 2, h - 56, 14, w - UiLayout.RewardFooterInset - 24, ColLightGray);
    }

    private void DrawVictoryScreen()
    {
        var w = Raylib.GetScreenWidth();
        var h = Raylib.GetScreenHeight();
        var panelW = w - UiLayout.VictoryPanelInsetX * 2;
        Raylib.DrawRectangle(0, 0, w, h, new Color(0, 0, 0, 220));
        DrawPanel(
            UiLayout.VictoryPanelInsetX,
            UiLayout.VictoryPanelInsetY,
            panelW,
            h - UiLayout.VictoryPanelInsetY * 2,
            ColPanel,
            ColBorder);

        DrawCenteredTextClamped("FLOOR 1 CLEARED", w / 2, 148, 52, panelW - 24, ColYellow);
        DrawCenteredTextClamped("The goblin den has fallen and the floor is secure.", w / 2, 222, 20, panelW - 24, ColLightGray);

        var summaryY = 270;
        DrawCenteredTextClamped($"Level {_player?.Level ?? 1}   Pack Items {GetInventoryQuantityTotal()}", w / 2, summaryY, 22, panelW - 24, ColSkyBlue);
        if (EnableRunMetaLayer)
        {
            DrawCenteredTextClamped($"Archetype: {GetRunArchetypeLabel(_runArchetype)}", w / 2, summaryY + 34, 18, panelW - 24, ColLightGray);
            DrawCenteredTextClamped($"Relic: {GetRunRelicLabel(_runRelic)}", w / 2, summaryY + 58, 17, panelW - 24, ColLightGray);
            DrawCenteredTextClamped($"Route: {GetPhase3RouteLabel(_phase3RouteChoice)}  XP {_phase3XpPercentMod:+#;-#;0}%  Enemy atk +{_phase3EnemyAttackBonus}", w / 2, summaryY + 80, 16, panelW - 24, ColLightGray);
            DrawCenteredTextClamped($"Doctrine ranks: {GetMilestoneRanksLabel()}", w / 2, summaryY + 102, 16, panelW - 24, ColLightGray);
            DrawCenteredTextClamped($"Run bonuses: Melee +{_runMeleeBonus}  Spell +{_runSpellBonus}  Defense +{_runDefenseBonus}  Crit +{_runCritBonus}%  Flee +{_runFleeBonus}%", w / 2, summaryY + 126, 17, panelW - 24, ColLightGray);
        }
        else
        {
            DrawCenteredTextClamped($"Zones cleared on one floor: Outer Warrens -> Central Warrens -> Boss Den", w / 2, summaryY + 46, 18, panelW - 24, ColLightGray);
            DrawCenteredTextClamped($"Run bonuses: Melee +{_runMeleeBonus}  Spell +{_runSpellBonus}  Defense +{_runDefenseBonus}  Crit +{_runCritBonus}%  Flee +{_runFleeBonus}%", w / 2, summaryY + 88, 17, panelW - 24, ColLightGray);
        }
        DrawCenteredTextClamped("Press ENTER to return to title", w / 2, h - 146, 20, panelW - 24, ColWhite);
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
        if (_uiFontInitialized && _uiFontLoadedFromFile)
        {
            Raylib.UnloadFont(_uiFont);
            _uiFontLoadedFromFile = false;
        }

        _spriteLibrary.Dispose();
        _map.Dispose();
    }

    private static bool Pressed(int key)
    {
        return Raylib.IsKeyPressed((KeyboardKey)key);
    }

    private static bool PressedOrRepeat(int key)
    {
        var keyboardKey = (KeyboardKey)key;
        return Raylib.IsKeyPressed(keyboardKey) || Raylib.IsKeyPressedRepeat(keyboardKey);
    }

    private static float GetUiTextScale()
    {
        var screenW = Math.Max(1280, Raylib.GetScreenWidth());
        var screenH = Math.Max(720, Raylib.GetScreenHeight());
        var widthScale = screenW / 1366f;
        var heightScale = screenH / 768f;
        var baseScale = MathF.Min(widthScale, heightScale) * 1.12f;
        return Math.Clamp(baseScale, 1.28f, 1.65f);
    }

    private static int GetEffectiveUiTextSize(int size)
    {
        if (size <= 0)
        {
            return 0;
        }

        return Math.Max(size + 4, (int)MathF.Ceiling(size * GetUiTextScale()));
    }

    // Line height for a text row: effective font size + 4px breathing room.
    private static int UiLineH(int size) => GetEffectiveUiTextSize(size) + 4;

    // Pathfinder point buy: total cost of a score relative to baseline 10.
    // Creation floor 7 (-4 pts), creation ceiling 20 (27 pts).
    private static int PointBuyCost(int score) => score switch
    {
        <= 7  => -4,
        8     => -2,
        9     => -1,
        10    => 0,
        11    => 1,
        12    => 2,
        13    => 3,
        14    => 5,
        15    => 7,
        16    => 10,
        17    => 13,
        18    => 17,
        19    => 22,
        >= 20 => 27
    };

    private static int PointBuyCostToRaise(int currentScore) =>
        PointBuyCost(currentScore + 1) - PointBuyCost(currentScore);

    private static int PointBuyCostToLower(int currentScore) =>
        PointBuyCost(currentScore) - PointBuyCost(currentScore - 1);

    // Scale a pixel constant (margin, padding, gap) by the current UI scale.
    private static int UiScale(int pixels) => (int)MathF.Ceiling(pixels * GetUiTextScale());

    private static void EnsureUiFontLoaded()
    {
        if (_uiFontInitialized)
        {
            return;
        }

        _uiFont = Raylib.GetFontDefault();
        foreach (var fontPath in new[] { PrimaryUiFontPath, SecondaryUiFontPath, TertiaryUiFontPath })
        {
            if (!System.IO.File.Exists(fontPath))
            {
                continue;
            }

            try
            {
                var loadedFont = Raylib.LoadFontEx(fontPath, 128, null, 0);
                if (loadedFont.Texture.Id == 0)
                {
                    continue;
                }

                _uiFont = loadedFont;
                _uiFontLoadedFromFile = true;
                Raylib.SetTextureFilter(_uiFont.Texture, TextureFilter.Point);
                break;
            }
            catch
            {
                // Fall back to the default bitmap font if system font loading fails.
            }
        }

        _uiFontInitialized = true;
    }

    private static int MeasureUiText(string text, int size)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        EnsureUiFontLoaded();
        var effectiveSize = GetEffectiveUiTextSize(size);
        return (int)MathF.Ceiling(Raylib.MeasureTextEx(_uiFont, text, effectiveSize, UiFontSpacing).X);
    }

    private static void DrawTextLine(string text, int x, int y, int size, Color color)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        EnsureUiFontLoaded();
        var effectiveSize = GetEffectiveUiTextSize(size);
        Raylib.DrawTextEx(_uiFont, text, new System.Numerics.Vector2(x, y), effectiveSize, UiFontSpacing, color);
    }

    private static void DrawCenteredText(string text, int centerX, int y, int size, Color color)
    {
        var width = MeasureUiText(text, size);
        DrawTextLine(text, centerX - width / 2, y, size, color);
    }

    private static void DrawCenteredTextClamped(string text, int centerX, int y, int size, int maxWidth, Color color)
    {
        var safeText = ClampTextToWidth(text, maxWidth, size);
        DrawCenteredText(safeText, centerX, y, size, color);
    }

    private static void DrawTextClamped(string text, int x, int y, int size, int maxWidth, Color color)
    {
        var safeText = ClampTextToWidth(text, maxWidth, size);
        DrawTextLine(safeText, x, y, size, color);
    }

    private static string ClampTextToWidth(string text, int maxWidth, int size)
    {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0)
        {
            return string.Empty;
        }

        if (MeasureUiText(text, size) <= maxWidth)
        {
            return text;
        }

        const string ellipsis = "...";
        if (MeasureUiText(ellipsis, size) > maxWidth)
        {
            return string.Empty;
        }

        var length = text.Length;
        while (length > 0)
        {
            var candidate = $"{text[..length]}{ellipsis}";
            if (MeasureUiText(candidate, size) <= maxWidth)
            {
                return candidate;
            }

            length -= 1;
        }

        return ellipsis;
    }

    private static int MeasureWrappedTextHeight(string text, int maxWidth, int size)
    {
        if (string.IsNullOrWhiteSpace(text) || maxWidth <= 0)
        {
            return 0;
        }

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return 0;
        }

        var lineCount = 1;
        var line = string.Empty;
        foreach (var word in words)
        {
            var candidate = string.IsNullOrEmpty(line) ? word : $"{line} {word}";
            if (MeasureUiText(candidate, size) > maxWidth && !string.IsNullOrEmpty(line))
            {
                lineCount += 1;
                line = word;
            }
            else
            {
                line = candidate;
            }
        }

        var effectiveSize = GetEffectiveUiTextSize(size);
        return lineCount * effectiveSize + Math.Max(0, lineCount - 1) * 4;
    }

    private static int DrawWrappedText(string text, int x, int y, int maxWidth, int size, Color color)
    {
        if (string.IsNullOrWhiteSpace(text) || maxWidth <= 0)
        {
            return 0;
        }

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return 0;
        }

        var line = string.Empty;
        var drawY = y;
        var lineCount = 0;
        var effectiveSize = GetEffectiveUiTextSize(size);

        foreach (var word in words)
        {
            var candidate = string.IsNullOrEmpty(line) ? word : $"{line} {word}";
            if (MeasureUiText(candidate, size) > maxWidth && !string.IsNullOrEmpty(line))
            {
                DrawTextLine(line, x, drawY, size, color);
                drawY += effectiveSize + 4;
                lineCount += 1;
                line = word;
            }
            else
            {
                line = candidate;
            }
        }

        if (!string.IsNullOrEmpty(line))
        {
            DrawTextLine(line, x, drawY, size, color);
            lineCount += 1;
        }

        return lineCount * effectiveSize + Math.Max(0, lineCount - 1) * 4;
    }
}





