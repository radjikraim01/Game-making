namespace DungeonEscape.Core;

public enum GameState
{
    StartMenu,
    HelpMenu,
    CharacterCreationHub,
    CharacterName,
    CharacterGender,
    CharacterClass,
    CharacterStatAllocation,
    Playing,
    Combat,
    CombatSkillMenu,
    CombatSpellMenu,
    CombatSpellTargeting,
    CombatItemMenu,
    CharacterMenu,
    LevelUp,
    FeatSelection,
    SpellSelection,
    SkillSelection,
    RewardChoice,
    PauseMenu,
    VictoryScreen,
    DeathScreen,
    CombatFormSelection
}

public enum Gender
{
    Male,
    Female
}

public enum Race
{
    Human,
    Elf,
    Dwarf,
    HalfOrc,
    Halfling,
    Gnome,
    Tiefling
}

public enum StatName
{
    Strength,
    Dexterity,
    Constitution,
    Intelligence,
    Wisdom,
    Charisma
}

public enum ArmorCategory
{
    Unarmored,
    Light,
    Medium,
    Heavy
}

public enum WeaponCategory { Simple, Martial }

public sealed class Stats
{
    public int Strength { get; set; }
    public int Dexterity { get; set; }
    public int Constitution { get; set; }
    public int Intelligence { get; set; }
    public int Wisdom { get; set; }
    public int Charisma { get; set; }

    public Stats Clone()
    {
        return new Stats
        {
            Strength = Strength,
            Dexterity = Dexterity,
            Constitution = Constitution,
            Intelligence = Intelligence,
            Wisdom = Wisdom,
            Charisma = Charisma
        };
    }

    public int Get(StatName stat)
    {
        return stat switch
        {
            StatName.Strength => Strength,
            StatName.Dexterity => Dexterity,
            StatName.Constitution => Constitution,
            StatName.Intelligence => Intelligence,
            StatName.Wisdom => Wisdom,
            StatName.Charisma => Charisma,
            _ => 0
        };
    }

    public void Add(StatName stat, int delta)
    {
        switch (stat)
        {
            case StatName.Strength:
                Strength += delta;
                break;
            case StatName.Dexterity:
                Dexterity += delta;
                break;
            case StatName.Constitution:
                Constitution += delta;
                break;
            case StatName.Intelligence:
                Intelligence += delta;
                break;
            case StatName.Wisdom:
                Wisdom += delta;
                break;
            case StatName.Charisma:
                Charisma += delta;
                break;
        }
    }
}

public sealed class CharacterClass
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required int HitDie { get; init; }
    public StatName[] SaveProficiencies { get; init; } = Array.Empty<StatName>();
    public WeaponCategory[] WeaponProficiencies { get; init; } = Array.Empty<WeaponCategory>();
    public string StartingWeaponId { get; init; } = "unarmed";
}

public static class CharacterClasses
{
    public static readonly IReadOnlyList<CharacterClass> All = new List<CharacterClass>
    {
        new()
        {
            Name = "Warrior",
            Description = "A master of arms, strong and resilient.",
            HitDie = 10,
            SaveProficiencies = new[] { StatName.Strength, StatName.Constitution },
            WeaponProficiencies = new[] { WeaponCategory.Simple, WeaponCategory.Martial },
            StartingWeaponId = "longsword"
        },
        new()
        {
            Name = "Rogue",
            Description = "A nimble skirmisher, quick and perceptive.",
            HitDie = 8,
            SaveProficiencies = new[] { StatName.Dexterity, StatName.Intelligence },
            WeaponProficiencies = new[] { WeaponCategory.Simple, WeaponCategory.Martial },
            StartingWeaponId = "rapier"
        },
        new()
        {
            Name = "Mage",
            Description = "A scholarly spellcaster who commands arcane energies.",
            HitDie = 6,
            SaveProficiencies = new[] { StatName.Intelligence, StatName.Wisdom },
            WeaponProficiencies = new[] { WeaponCategory.Simple },
            StartingWeaponId = "quarterstaff"
        },
        new()
        {
            Name = "Paladin",
            Description = "A holy warrior armored in faith, equally at home with sword and prayer.",
            HitDie = 10,
            SaveProficiencies = new[] { StatName.Wisdom, StatName.Charisma },
            WeaponProficiencies = new[] { WeaponCategory.Simple, WeaponCategory.Martial },
            StartingWeaponId = "longsword"
        },
        new()
        {
            Name = "Ranger",
            Description = "A hunter of the wilds, precise and patient, deadly at any range.",
            HitDie = 10,
            SaveProficiencies = new[] { StatName.Strength, StatName.Dexterity },
            WeaponProficiencies = new[] { WeaponCategory.Simple, WeaponCategory.Martial },
            StartingWeaponId = "longbow"
        },
        new()
        {
            Name = "Cleric",
            Description = "A divine conduit who heals allies and smites evil with holy power.",
            HitDie = 8,
            SaveProficiencies = new[] { StatName.Wisdom, StatName.Charisma },
            WeaponProficiencies = new[] { WeaponCategory.Simple },
            StartingWeaponId = "mace"
        },
        new()
        {
            Name = "Barbarian",
            Description = "A primal warrior who flies into a rage, shrugging off pain to deal brutal damage.",
            HitDie = 12,
            SaveProficiencies = new[] { StatName.Strength, StatName.Constitution },
            WeaponProficiencies = new[] { WeaponCategory.Simple, WeaponCategory.Martial },
            StartingWeaponId = "greataxe"
        },
        new()
        {
            Name = "Bard",
            Description = "A silver-tongued performer whose music bends reality and beguiles foes.",
            HitDie = 8,
            SaveProficiencies = new[] { StatName.Dexterity, StatName.Charisma },
            WeaponProficiencies = new[] { WeaponCategory.Simple, WeaponCategory.Martial },
            StartingWeaponId = "rapier"
        }
    };
}

public sealed class Skill
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Effect { get; init; }
    public required StatName ScalingStat { get; init; }
}

public static class SkillBook
{
    public static readonly IReadOnlyList<Skill> All = new List<Skill>
    {
        new() { Id = "toughness", Name = "Toughness", Description = "Your body hardens against punishment.", Effect = "+5 Max HP (scales with CON)", ScalingStat = StatName.Constitution },
        new() { Id = "fast_learner", Name = "Fast Learner", Description = "You absorb lessons from every skirmish.", Effect = "+10% XP Gain (scales with INT)", ScalingStat = StatName.Intelligence },
        new() { Id = "meditation", Name = "Inner Reserve", Description = "Disciplined breathing lets you recover arcane energy between fights.", Effect = "Recover 1 L1 spell slot at each rest point.", ScalingStat = StatName.Wisdom },
        new() { Id = "brute_force", Name = "Brute Force", Description = "Raw power behind every swing.", Effect = "+2 Melee Damage (scales with STR)", ScalingStat = StatName.Strength },
        new() { Id = "iron_skin", Name = "Iron Skin", Description = "Blows glance off your hardened hide.", Effect = "+1 Defense (scales with CON)", ScalingStat = StatName.Constitution },
        new() { Id = "war_cry", Name = "War Cry", Description = "Your battle shout rattles enemies before the fight begins.", Effect = "+3 First-Strike Damage (scales with STR)", ScalingStat = StatName.Strength },
        new() { Id = "second_wind", Name = "Second Wind", Description = "Once per combat, rally and restore HP.", Effect = "Heal 10 HP in combat (scales with CON)", ScalingStat = StatName.Constitution },
        new() { Id = "eagle_eye", Name = "Eagle Eye", Description = "You find every gap in an enemy's guard.", Effect = "+2% Crit Chance (scales with DEX)", ScalingStat = StatName.Dexterity },
        new() { Id = "shadowstep", Name = "Shadowstep", Description = "You vanish into shadow when things go wrong.", Effect = "+10% Flee Chance (scales with DEX)", ScalingStat = StatName.Dexterity },
        new() { Id = "swift_strikes", Name = "Swift Strikes", Description = "Speed lets you squeeze in an extra blow.", Effect = "Bonus attack roll each turn (scales with DEX)", ScalingStat = StatName.Dexterity },
        new() { Id = "poison_blade", Name = "Poison Blade", Description = "Your weapon drips with slow-acting venom.", Effect = "+1 Poison damage per turn (scales with DEX)", ScalingStat = StatName.Dexterity },
        new() { Id = "arcane_surge", Name = "Arcane Surge", Description = "Channel raw magic through your strikes.", Effect = "+3 Magic Damage (scales with casting stat)", ScalingStat = StatName.Intelligence },
        new() { Id = "mana_shield", Name = "Arcane Ward", Description = "Channel arcane energy into a protective barrier.", Effect = "Once per combat: absorb damage (scales with casting stat).", ScalingStat = StatName.Intelligence },
        new() { Id = "inspire", Name = "Inspire", Description = "Your presence sharpens every strike.", Effect = "+2 All Damage (scales with CHA)", ScalingStat = StatName.Charisma },
        new() { Id = "channel_divinity", Name = "Channel Divinity", Description = "You channel raw divine power through your next spell.", Effect = "Once per combat: next divine spell deals bonus damage (scales with WIS).", ScalingStat = StatName.Wisdom },
        new() { Id = "blessed_healer", Name = "Blessed Healer", Description = "The divine energy you channel heals more than it should.", Effect = "Your healing spells restore extra HP equal to your WIS modifier.", ScalingStat = StatName.Wisdom },
        new() { Id = "cutting_words", Name = "Cutting Words", Description = "A perfectly timed insult undermines an enemy's strike.", Effect = "Once per combat: reduce one enemy attack roll by 1d4.", ScalingStat = StatName.Charisma },
        new() { Id = "lay_on_hands", Name = "Lay on Hands", Description = "A divine touch mends wounds through sheer faith.", Effect = "Once per combat: restore HP from a sacred healing pool (scales with level).", ScalingStat = StatName.Charisma },
        new() { Id = "hunters_instinct", Name = "Hunter's Instinct", Description = "You read your prey's patterns and exploit every opening.", Effect = "Marked targets take +2 damage from all sources.", ScalingStat = StatName.Wisdom }
    };

    // Class talent progression: class name -> list of (minLevel, skillId)
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<(int Level, string SkillId)>> ClassTalentProgression =
        new Dictionary<string, IReadOnlyList<(int, string)>>
        {
            ["Warrior"] = new List<(int, string)>
            {
                (1, "second_wind"),
                (3, "war_cry"),
                (5, "swift_strikes"),
                (7, "iron_skin"),
                (9, "brute_force")
            },
            ["Rogue"] = new List<(int, string)>
            {
                (1, "eagle_eye"),
                (3, "shadowstep"),
                (5, "poison_blade")
            },
            ["Mage"] = new List<(int, string)>
            {
                (1, "mana_shield"),
                (3, "meditation"),
                (5, "arcane_surge")
            },
            ["Cleric"] = new List<(int, string)>
            {
                (1, "channel_divinity"),
                (3, "blessed_healer")
            },
            ["Bard"] = new List<(int, string)>
            {
                (1, "inspire"),
                (3, "cutting_words")
            },
            ["Paladin"] = new List<(int, string)>
            {
                (1, "lay_on_hands"),
                (3, "brute_force"),
                (5, "swift_strikes"),
                (7, "iron_skin")
            },
            ["Ranger"] = new List<(int, string)>
            {
                (1, "eagle_eye"),
                (3, "hunters_instinct"),
                (5, "swift_strikes"),
                (7, "shadowstep")
            }
        };
}

public sealed class FeatDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Effect { get; init; }
    // Combat stat bonuses
    public int MeleeDamageBonus { get; init; }
    public int SpellDamageBonus { get; init; }
    public int DefenseBonus { get; init; }
    public int CritChanceBonus { get; init; }
    public int CritRangeBonus { get; init; }
    public int FleeChanceBonus { get; init; }
    public int MaxHpPerLevelBonus { get; init; }
    public int MaxHpFlatBonus { get; init; }
    public int SpellArmorBypassBonus { get; init; }
    // New stat dimensions (Phase A)
    public int InitiativeBonus { get; init; }
    public int BonusSpellSlotL1 { get; init; }
    public int HealingBonus { get; init; }
    public int StatBonusStr { get; init; }
    public int StatBonusDex { get; init; }
    public int StatBonusCon { get; init; }
    public int StatBonusInt { get; init; }
    public int StatBonusWis { get; init; }
    public int StatBonusCha { get; init; }
    // Prerequisite gating
    public int MinLevel { get; init; } = 1;
    public bool RequiresCasterClass { get; init; }
    public string? RequiredClassName { get; init; }
    public IReadOnlyList<string> RequiredFeatIds { get; init; } = Array.Empty<string>();
    public string? PrerequisiteText { get; init; }
    // D&D saving throw proficiency
    public StatName? GrantsSaveProficiency { get; init; }
}

public static class FeatProgression
{
    public static int GetCreationStartingFeatPicks()
    {
        return 1;
    }

    public static bool GrantsFeat(int level)
    {
        return level >= 4 && level % 4 == 0;
    }
}

public static class ArmorTraining
{
    public static int GetRequiredRank(ArmorCategory category)
    {
        return category switch
        {
            ArmorCategory.Light => 1,
            ArmorCategory.Medium => 2,
            ArmorCategory.Heavy => 3,
            _ => 0
        };
    }

    public static string GetCategoryLabel(ArmorCategory category)
    {
        return category switch
        {
            ArmorCategory.Unarmored => "Unarmored",
            ArmorCategory.Light => "Light",
            ArmorCategory.Medium => "Medium",
            ArmorCategory.Heavy => "Heavy",
            _ => "Unknown"
        };
    }

    public static string GetRankLabel(int rank)
    {
        return Math.Clamp(rank, 0, 3) switch
        {
            3 => "Heavy",
            2 => "Medium",
            1 => "Light",
            _ => "None"
        };
    }

    public static int GetClassTrainingRank(string className)
    {
        return className switch
        {
            "Warrior" => 3,
            "Paladin" => 3,
            "Ranger" => 2,
            "Barbarian" => 2,
            "Cleric" => 2,
            "Rogue" => 1,
            "Bard" => 1,
            _ => 0
        };
    }

    public static int GetFeatTrainingRank(Func<string, bool> hasFeat)
    {
        if (hasFeat("heavy_armor_training_feat")) return 3;
        if (hasFeat("medium_armor_training_feat")) return 2;
        if (hasFeat("light_armor_training_feat")) return 1;
        return 0;
    }

    public static int GetEffectiveTrainingRank(string className, Func<string, bool> hasFeat)
    {
        var classRank = GetClassTrainingRank(className);
        var featRank = GetFeatTrainingRank(hasFeat);
        return Math.Max(classRank, featRank);
    }

    public static bool HasTrainingForCategory(string className, Func<string, bool> hasFeat, ArmorCategory category)
    {
        var needed = GetRequiredRank(category);
        if (needed <= 0)
        {
            return true;
        }

        return GetEffectiveTrainingRank(className, hasFeat) >= needed;
    }
}

public static class FeatBook
{
    public static readonly IReadOnlyList<FeatDefinition> All = new List<FeatDefinition>
    {
        // ── Armor training (gates equipment access) ──────────────────────────
        new() { Id = "light_armor_training_feat",  Name = "Light Armor Training",  Description = "You learn to move and fight in light armor.",              Effect = "Allows light armor; +1 defense and +2% flee while wearing it" },
        new() { Id = "medium_armor_training_feat", Name = "Medium Armor Training", Description = "You bear medium armor without losing combat rhythm.",        Effect = "Allows medium armor; +2 defense while wearing it",           PrerequisiteText = "Requires light armor training (class or feat)." },
        new() { Id = "heavy_armor_training_feat",  Name = "Heavy Armor Training",  Description = "You train to endure and exploit heavy plate.",              Effect = "Allows heavy armor; +3 defense while wearing it",           PrerequisiteText = "Requires medium armor training (class or feat)." },
        new() { Id = "unarmored_defense_feat",     Name = "Unarmored Defense",     Description = "Without armor you are at your most elusive.",               Effect = "When unarmored: +2 defense, +8% flee chance" },

        // ── Universal survival ────────────────────────────────────────────────
        new() { Id = "tough_feat",          Name = "Tough",           Description = "Your hit point maximum increases substantially.",          Effect = "+2 max HP per level",                        MaxHpPerLevelBonus = 2 },
        new() { Id = "resilient_feat",      Name = "Resilient (Constitution)", Description = "Training under duress hardens your body against punishment.",    Effect = "+1 Constitution. Grants proficiency in Constitution saving throws.", StatBonusCon = 1, GrantsSaveProficiency = StatName.Constitution },
        new() { Id = "battle_hardened_feat",Name = "Durable",         Description = "Hardy and tough, you recover more effectively from injuries.",   Effect = "+1 Constitution. Regain an additional 5 HP when healing.", StatBonusCon = 1, HealingBonus = 5, RequiredFeatIds = new[] { "tough_feat" }, PrerequisiteText = "Requires Tough." },
        new() { Id = "iron_resolve_feat",   Name = "Endurance",       Description = "Relentless training forges your body into a resilient instrument of war.",  Effect = "+1 Constitution, +3 defense.", StatBonusCon = 1, DefenseBonus = 3, RequiredFeatIds = new[] { "battle_hardened_feat" }, PrerequisiteText = "Requires Durable." },

        // ── Universal melee ───────────────────────────────────────────────────
        new() { Id = "great_weapon_master_feat", Name = "Great Weapon Master", Description = "You trade precision for devastating power on each swing.",       Effect = "+1 Strength. Passive: add your Proficiency Bonus to every melee hit.", StatBonusStr = 1 },
        new() { Id = "savage_attacker_feat",     Name = "Savage Attacker",     Description = "You roll your weapon's damage dice twice and take the higher.",  Effect = "Once per combat: when you hit with melee, roll damage twice and use the higher result." },
        new() { Id = "defensive_duelist_feat",   Name = "Defensive Duelist",   Description = "With a precise parry you absorb the worst of an incoming blow.", Effect = "+1 Dexterity. Once per combat: add DEX modifier to AC against one attack (Reaction).", StatBonusDex = 1 },
        new() { Id = "lucky_feat",               Name = "Lucky",               Description = "Fortune bends in your favour at the worst possible moment.",     Effect = "Once per combat: your next attack has Advantage (roll d20 twice, take higher)." },

        // ── Universal mobility / utility ──────────────────────────────────────
        new() { Id = "mobile_feat",       Name = "Mobile",       Description = "You move quickly and disengage before the retaliation lands.",           Effect = "+15% flee chance",               FleeChanceBonus = 15 },
        new() { Id = "alert_feat",        Name = "Alert",        Description = "Heightened awareness lets you act before the threat closes.",            Effect = "+5 initiative, +1 AC", InitiativeBonus = 5, DefenseBonus = 1 },
        new() { Id = "sentinel_feat",     Name = "Sentinel",     Description = "You punish every opening — when an enemy overextends attacking you and misses, you strike.",  Effect = "+1 Strength. Once per combat: when you evade an enemy attack, make a free counter-attack.", StatBonusStr = 1 },
        new() { Id = "fleet_footed_feat", Name = "Athlete",      Description = "Dedicated physical conditioning makes you swift, nimble, and hard to pin down.",  Effect = "+1 Dexterity, +10% flee chance.", StatBonusDex = 1, FleeChanceBonus = 10, RequiredFeatIds = new[] { "mobile_feat" }, PrerequisiteText = "Requires Mobile." },

        // ── Universal magic (caster classes only) ─────────────────────────────
        new() { Id = "war_caster_feat",     Name = "War Caster",     Description = "You have mastered casting in the chaos of battle, maintaining concentration through brutal punishment.",  Effect = "+1 Intelligence. Advantage on Constitution saves to maintain concentration: roll twice, take higher.", StatBonusInt = 1, RequiresCasterClass = true, PrerequisiteText = "Requires a spellcasting class." },
        new() { Id = "arcane_mind_feat",    Name = "Spell Sniper",   Description = "Your spells find the gaps in enemy resistance, bypassing their natural defenses.",  Effect = "+1 Intelligence. Spells bypass +1 enemy armor.", StatBonusInt = 1, SpellArmorBypassBonus = 1, RequiresCasterClass = true, PrerequisiteText = "Requires a spellcasting class." },
        new() { Id = "spell_focus_feat",    Name = "Arcane Precision", Description = "Focused magical training amplifies the force behind every spell you cast.",      Effect = "+2 spell damage",               SpellDamageBonus = 2, RequiresCasterClass = true, PrerequisiteText = "Requires a spellcasting class." },
        new() { Id = "elemental_adept_feat",Name = "Elemental Adept",Description = "Your elemental spells punch through hardened defenses and treat the lowest rolls as average.", Effect = "+1 Intelligence. Spells ignore +2 armor, +1 spell damage minimum.", StatBonusInt = 1, SpellArmorBypassBonus = 2, SpellDamageBonus = 1, RequiresCasterClass = true, RequiredFeatIds = new[] { "arcane_mind_feat" }, PrerequisiteText = "Requires Spell Sniper and a spellcasting class." },
        new() { Id = "piercing_magic_feat", Name = "Spell Penetration", Description = "Refined targeting drives your spells through magical resistance and physical barriers.",  Effect = "+1 spell damage, spells ignore +1 armor", SpellDamageBonus = 1, SpellArmorBypassBonus = 1, RequiresCasterClass = true, RequiredFeatIds = new[] { "spell_focus_feat" }, PrerequisiteText = "Requires Arcane Precision and a spellcasting class." },

        // ── Warrior class feats ───────────────────────────────────────────────
        new() { Id = "warrior_indomitable_feat", Name = "Indomitable",  Description = "When the enemy lands a blow you refuse to accept, your warrior training carries you through.", Effect = "Once per combat: reduce one enemy hit's damage by your Fighter level.",   RequiredClassName = "Warrior", PrerequisiteText = "Warrior only." },
        new() { Id = "warrior_battle_focus_feat",Name = "Fighting Initiate", Description = "Dedicated training in a combat style deepens your defensive form and endurance.", Effect = "+1 Strength, +1 defense, +2 max HP per level", StatBonusStr = 1, DefenseBonus = 1, MaxHpPerLevelBonus = 2, RequiredClassName = "Warrior", PrerequisiteText = "Warrior only." },
        new() { Id = "warrior_iron_will_feat",   Name = "Indomitable Body", Description = "Years of warfare have turned your body into an instrument of unstoppable endurance.",  Effect = "+4 defense, +1 Constitution",     DefenseBonus = 4, StatBonusCon = 1, RequiredClassName = "Warrior", RequiredFeatIds = new[] { "warrior_battle_focus_feat" }, PrerequisiteText = "Requires Fighting Initiate. Warrior only." },

        // ── Rogue class feats ─────────────────────────────────────────────────
        new() { Id = "rogue_uncanny_dodge_feat", Name = "Uncanny Dodge", Description = "Pure instinct kicks in — you blur aside as the blow lands.",        Effect = "Once per combat: automatically halve one incoming hit.",                  RequiredClassName = "Rogue", PrerequisiteText = "Rogue only." },
        new() { Id = "rogue_assassin_feat",      Name = "Assassin",      Description = "Strike before your target knows you're there for a killing blow.",   Effect = "If you attack before the enemy acts: Advantage + any hit is an automatic critical.", RequiredClassName = "Rogue", RequiredFeatIds = new[] { "rogue_uncanny_dodge_feat" }, PrerequisiteText = "Requires Uncanny Dodge. Rogue only." },
        new() { Id = "rogue_blade_dancer_feat",  Name = "Skulker",       Description = "You exploit shadow, motion, and misdirection to close the distance and strike.",  Effect = "+1 Dexterity, +2 melee damage, crit on 19-20, +5% flee chance", StatBonusDex = 1, MeleeDamageBonus = 2, CritRangeBonus = 1, FleeChanceBonus = 5, RequiredClassName = "Rogue", RequiredFeatIds = new[] { "rogue_assassin_feat" }, PrerequisiteText = "Requires Assassin. Rogue only." },

        // ── Mage class feats ──────────────────────────────────────────────────
        new() { Id = "mage_arcane_surge_feat", Name = "Arcane Surge",    Description = "You tap a deeper reservoir of prepared spell energy.",              Effect = "+1 bonus L1 spell slot, +1 spell damage",  BonusSpellSlotL1 = 1, SpellDamageBonus = 1, RequiredClassName = "Mage", PrerequisiteText = "Mage only." },
        new() { Id = "mage_metamagic_feat",    Name = "Metamagic Adept", Description = "You warp spell energy mid-cast — the target finds their resistance crumbling.", Effect = "+1 Intelligence. Once per combat: Heightened Spell — target rolls save twice and takes the lower result (Disadvantage).", StatBonusInt = 1, RequiredClassName = "Mage", RequiredFeatIds = new[] { "mage_arcane_surge_feat" }, PrerequisiteText = "Requires Arcane Surge. Mage only." },

        // ── Cleric class feats ────────────────────────────────────────────────
        new() { Id = "cleric_divine_ward_feat",     Name = "Sacred Fortitude", Description = "Your faith manifests as divine protection, hardening body and soul against all harm.",  Effect = "+2 defense, +4 max HP",                      DefenseBonus = 2, MaxHpFlatBonus = 4, RequiredClassName = "Cleric", PrerequisiteText = "Cleric only." },
        new() { Id = "cleric_blessed_strikes_feat", Name = "Blessed Strikes",  Description = "Divine energy flows through every spell you cast, adding sacred radiance always.", Effect = "Passive (Potent Spellcasting): your Wisdom modifier is permanently added to all Cleric spell damage.", RequiredClassName = "Cleric", PrerequisiteText = "Cleric only." },

        // ── Ranger class feats ────────────────────────────────────────────────
        new() { Id = "ranger_naturalist_feat",   Name = "Observant",    Description = "Keen senses honed in the wild let you read intent before an enemy commits to action.",  Effect = "+1 Wisdom, +5 initiative, +5% flee chance", StatBonusWis = 1, InitiativeBonus = 5, FleeChanceBonus = 5, RequiredClassName = "Ranger", PrerequisiteText = "Ranger only." },
        new() { Id = "ranger_sharpshooter_feat", Name = "Sharpshooter", Description = "You exploit every gap in the enemy's defenses with a perfectly placed shot.", Effect = "+1 Dexterity. Once per combat: next physical attack ignores all armor and deals +5 bonus damage.", StatBonusDex = 1, RequiredClassName = "Ranger", PrerequisiteText = "Ranger only." },

        // ── Bard class feats ──────────────────────────────────────────────────
        new() { Id = "bard_jack_of_trades_feat", Name = "Jack of All Trades", Description = "Your breadth of training makes you useful in any situation.", Effect = "+2 initiative, +1 melee damage, +1 spell damage", InitiativeBonus = 2, MeleeDamageBonus = 1, SpellDamageBonus = 1, RequiredClassName = "Bard", PrerequisiteText = "Bard only." },
        new() { Id = "bard_countercharm_feat",   Name = "Countercharm",       Description = "A perfectly timed performance shatters the enemy's focus just as they attack.", Effect = "Once per combat: impose a 1d4 penalty on the next enemy attack roll (Disadvantage).", RequiredClassName = "Bard", RequiredFeatIds = new[] { "bard_jack_of_trades_feat" }, PrerequisiteText = "Requires Jack of All Trades. Bard only." },

        // ── Paladin class feats ───────────────────────────────────────────────
        new() { Id = "paladin_divine_favor_feat",   Name = "Divine Favor",       Description = "You call down divine power, infusing your weapon with sacred radiance.",  Effect = "Once per combat: activate Divine Favor — every weapon hit deals +1d4 radiant bonus damage.", RequiredClassName = "Paladin", PrerequisiteText = "Paladin only." },
        new() { Id = "paladin_aura_protection_feat",Name = "Aura of Protection", Description = "Your conviction radiates outward, hardening your defenses with divine force.", Effect = "Your Charisma modifier is added to your defense permanently.",            RequiredClassName = "Paladin", RequiredFeatIds = new[] { "paladin_divine_favor_feat" }, PrerequisiteText = "Requires Divine Favor. Paladin only." },

        // ── Phase B: Active mechanic feats (once per combat actions) ─────────
        new() { Id = "warrior_battle_cry_feat",   Name = "Battle Cry",      Description = "A roar of fury primes your next blow for devastating extra force.",     Effect = "Once per combat: next melee hit deals STR-scaling bonus damage",        RequiredClassName = "Warrior", PrerequisiteText = "Warrior only." },
        new() { Id = "rogue_vanish_feat",         Name = "Vanish",          Description = "You slip into a blind spot and reposition for a killing blow.",         Effect = "Once per combat: next attack has Advantage (roll d20 twice, take higher).",       RequiredClassName = "Rogue",   PrerequisiteText = "Rogue only." },
        new() { Id = "paladin_divine_smite_feat", Name = "Divine Smite",    Description = "You channel holy power through your weapon, searing the target.",       Effect = "Once per combat: burn a L1 spell slot for +5 bonus radiant damage",    RequiredClassName = "Paladin", PrerequisiteText = "Paladin only." },
        new() { Id = "mage_empower_spell_feat",   Name = "Empowered Spell", Description = "You pour extra magical force into your next cast, rolling the damage twice and keeping the better result.",  Effect = "Once per combat: next spell rolls damage twice, takes higher result.", RequiredClassName = "Mage",    PrerequisiteText = "Mage only." },
        new() { Id = "cleric_word_of_renewal_feat",Name = "Word of Renewal", Description = "A sacred utterance refills your reserves before the fight is over.",   Effect = "Once per combat: restore 1 L1 spell slot instantly",                   RequiredClassName = "Cleric",  PrerequisiteText = "Cleric only." },

        // ── Phase C: Universal combat feats ───────────────────────────────────
        new() { Id = "riposte_feat",     Name = "Riposte",      Description = "A perfectly timed counter-attack punishes the enemy's overconfidence.",          Effect = "Once per combat: when an enemy hits you, deal a counter-attack for half weapon damage." },
        new() { Id = "crusher_feat",     Name = "Crusher",      Description = "Your heavy strikes leave enemies disoriented and off-balance.",                   Effect = "+1 Strength. On a critical hit, the target has Disadvantage on its next attack.", StatBonusStr = 1 },
        new() { Id = "piercer_feat",     Name = "Piercer",      Description = "Precise and deadly, your thrusts find the softest gaps in any defense.",          Effect = "+1 Dexterity. Once per turn: reroll one weapon damage die and use the higher result.", StatBonusDex = 1 },

        // ── Phase C: Defensive feats ───────────────────────────────────────────
        new() { Id = "shield_expert_feat", Name = "Shield Expert", Description = "Your mastery of defensive techniques lets you absorb blows that would fell lesser warriors.", Effect = "+2 AC. Once per combat: downgrade one enemy critical hit to a normal hit.", DefenseBonus = 2 },
        new() { Id = "iron_will_feat",     Name = "Iron Will",     Description = "Mental fortitude and spiritual training harden you against psychic and divine influence.",   Effect = "+1 Wisdom. Gain proficiency in Wisdom saving throws.", StatBonusWis = 1, GrantsSaveProficiency = StatName.Wisdom },

        // ── Phase C: Barbarian class feats ────────────────────────────────────
        new() { Id = "barbarian_reckless_attack_feat", Name = "Reckless Attack",  Description = "You throw all caution aside, striking with overwhelming fury at the cost of your own defenses.", Effect = "Once per combat: Advantage on your next attack, but the enemy gains Advantage on their next attack.", RequiredClassName = "Barbarian", PrerequisiteText = "Barbarian only." },
        new() { Id = "barbarian_brutal_critical_feat", Name = "Brutal Critical",  Description = "When you land a devastating blow, you roll another die of carnage.",                           Effect = "+1 Strength. On a critical hit, roll one additional weapon damage die.", StatBonusStr = 1, RequiredClassName = "Barbarian", RequiredFeatIds = new[] { "barbarian_reckless_attack_feat" }, PrerequisiteText = "Requires Reckless Attack. Barbarian only." },

        // ── Phase C: Ranger class feats ───────────────────────────────────────
        new() { Id = "ranger_colossus_slayer_feat", Name = "Colossus Slayer", Description = "You exploit the gaps in already-wounded prey, driving your strikes into unhealed wounds.", Effect = "Once per turn: when you hit a creature below its max HP, deal +1d8 bonus damage.", RequiredClassName = "Ranger", RequiredFeatIds = new[] { "ranger_sharpshooter_feat" }, PrerequisiteText = "Requires Sharpshooter. Ranger only." },

        // ── Phase C: Mage class feats ─────────────────────────────────────────
        new() { Id = "mage_overchannel_feat", Name = "Overchannel", Description = "You pour raw unstable power into your next spell, maximizing every iota of destructive force.", Effect = "Once per combat: maximize all damage dice of one spell. You take 1d6 recoil damage.", RequiredClassName = "Mage", RequiredFeatIds = new[] { "mage_metamagic_feat" }, PrerequisiteText = "Requires Metamagic Adept. Mage only." },

        // ── Phase C: Cleric class feats ───────────────────────────────────────
        new() { Id = "cleric_spiritual_weapon_feat", Name = "Spiritual Weapon", Description = "A spectral weapon of divine force materializes to strike at your command.", Effect = "Once per combat: after casting a spell, make a bonus melee attack for 1d8 + WIS modifier.", RequiredClassName = "Cleric", RequiredFeatIds = new[] { "cleric_blessed_strikes_feat" }, PrerequisiteText = "Requires Blessed Strikes. Cleric only." },

        // ── Phase C: Bard class feats ─────────────────────────────────────────
        new() { Id = "bard_bardic_inspiration_feat", Name = "Bardic Inspiration", Description = "Your encouraging words inspire allies and steady your own hand at the critical moment.", Effect = "+1 Charisma. Once per combat: add 1d6 to your next attack roll or saving throw.", StatBonusCha = 1, RequiredClassName = "Bard", RequiredFeatIds = new[] { "bard_countercharm_feat" }, PrerequisiteText = "Requires Countercharm. Bard only." },
    };

    public static readonly IReadOnlyDictionary<string, FeatDefinition> ById = All
        .ToDictionary(feat => feat.Id, feat => feat, StringComparer.Ordinal);
}

public sealed class WeaponDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required int DamageDice { get; init; }   // die sides: 4, 6, 8, 10, 12
    public required int DiceCount { get; init; }     // usually 1; greatsword=2
    public required StatName AttackStat { get; init; } // STR or DEX
    public bool IsFinesse { get; init; }             // uses max(STR, DEX)
    public bool IsRanged { get; init; }
    public bool IsTwoHanded { get; init; }
    public int VersatileDice { get; init; }          // 0 = not versatile
    public WeaponCategory Category { get; init; }
    public required string Description { get; init; }
}

public static class WeaponBook
{
    public static readonly WeaponDefinition Unarmed = new()
    {
        Id = "unarmed", Name = "Unarmed Strike", DamageDice = 1, DiceCount = 1,
        AttackStat = StatName.Strength, Category = WeaponCategory.Simple,
        Description = "A bare fist. Not ideal."
    };

    public static readonly IReadOnlyDictionary<string, WeaponDefinition> ById = new Dictionary<string, WeaponDefinition>
    {
        ["unarmed"] = Unarmed,
        ["longsword"] = new() { Id = "longsword", Name = "Longsword", DamageDice = 8, DiceCount = 1,
            AttackStat = StatName.Strength, VersatileDice = 10, Category = WeaponCategory.Martial,
            Description = "A versatile sword, deadly in one or two hands." },
        ["rapier"] = new() { Id = "rapier", Name = "Rapier", DamageDice = 8, DiceCount = 1,
            AttackStat = StatName.Dexterity, IsFinesse = true, Category = WeaponCategory.Martial,
            Description = "A precise thrusting blade." },
        ["quarterstaff"] = new() { Id = "quarterstaff", Name = "Quarterstaff", DamageDice = 6, DiceCount = 1,
            AttackStat = StatName.Strength, VersatileDice = 8, Category = WeaponCategory.Simple,
            Description = "A sturdy walking stick, also good for braining enemies." },
        ["mace"] = new() { Id = "mace", Name = "Mace", DamageDice = 6, DiceCount = 1,
            AttackStat = StatName.Strength, Category = WeaponCategory.Simple,
            Description = "A heavy headed club." },
        ["longbow"] = new() { Id = "longbow", Name = "Longbow", DamageDice = 8, DiceCount = 1,
            AttackStat = StatName.Dexterity, IsRanged = true, IsTwoHanded = true, Category = WeaponCategory.Martial,
            Description = "A powerful bow, accurate at long range." },
        ["greataxe"] = new() { Id = "greataxe", Name = "Greataxe", DamageDice = 12, DiceCount = 1,
            AttackStat = StatName.Strength, IsTwoHanded = true, Category = WeaponCategory.Martial,
            Description = "A brutal two-handed axe." },
    };
}

public sealed class SpellDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string ClassName { get; init; }
    public required int SpellLevel { get; init; } // 0 = cantrip
    public required string Description { get; init; }
    public required StatName ScalingStat { get; init; }
    public required int BaseDamage { get; init; }
    public required int Variance { get; init; }
    public required int ArmorBypass { get; init; }
    public required string DamageTag { get; init; }
    public SpellOrigin Origin { get; init; } = SpellOrigin.Authored;
    public bool SuppressCounterAttack { get; init; }
    public SpellTargetShape TargetShape { get; set; } = SpellTargetShape.SingleEnemy;
    public SpellEffectRouteSpec? EffectRoute { get; set; }
    public int CantripDiceSides { get; init; }  // 0 = use old BaseDamage path

    public bool IsHealSpell { get; init; }
    public bool IsCleanseSpell { get; init; }
    public bool IsCantrip => SpellLevel == 0;
    public bool RequiresSlot => SpellLevel > 0;
    public bool IsPrototypeExpanded => Origin == SpellOrigin.PrototypeExpanded;
}

public enum SpellOrigin
{
    Authored,
    PrototypeExpanded
}

public enum SpellTargetShape
{
    SingleEnemy,
    Self,
    Tile,
    Radius,
    Line,
    Cone
}

public enum SpellEffectRouteKind
{
    DirectDamage,
    DamageAndStatus,
    FutureGated,
    ConcentrationAura,
    Cleanse,
    WeaponRider,
    SelfBuff,
    Summon,
    Transformation
}

public enum SummonBehaviorKind
{
    AutoAttack,    // Attacks current target each player turn (Spiritual Weapon, Flaming Sphere)
    PassiveAura,   // No attacks, passive effect (future: Spirit Guardians-like)
    BuffMount,     // Stat buff without combat actor (future: Find Steed)
    Utility        // Non-combat (future: Unseen Servant)
}

public sealed class SummonType
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string SourceSpellId { get; init; }
    public int MaxHp { get; init; }                  // 0 = invulnerable/spectral
    public int DamageCount { get; init; } = 1;       // number of dice (2 = 2d6)
    public int DamageDice { get; init; } = 8;        // die sides (d8 = 8)
    public int DamageBonus { get; init; }             // flat bonus after dice
    public int AttackBonus { get; init; }             // added to caster's spell attack
    public bool UseCasterStatMod { get; init; } = true;
    public string DamageType { get; init; } = "force";
    public SummonBehaviorKind Behavior { get; init; } = SummonBehaviorKind.AutoAttack;
    public bool RequiresConcentration { get; init; } = true;
    public string Description { get; init; } = string.Empty;
}

public sealed class SummonInstance
{
    public SummonType Type { get; init; } = null!;
    public int CurrentHp { get; set; }
    public bool IsAlive => Type.MaxHp == 0 || CurrentHp > 0;
}

public enum FormSpecialKind
{
    None,
    PackTactics,
    Evasion,
    PoisonOnHit,
    BonusCritDamage,
    NoCounterAttack,
    FirstHitBonus,
    DebuffOnHit,
    Regeneration,
    HealOnKill,
    ArmorBypass,
    DefenseBonus,
    BurnOnHit,
    FleeBonus,
    DamageResist
}

public sealed class FormDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int FormAC { get; init; }
    public int AttackBonus { get; init; }
    public int DamageCount { get; init; } = 1;
    public int DamageDice { get; init; } = 8;
    public int DamageBonus { get; init; }
    public string DamageType { get; init; } = "slashing";
    public int TempHp { get; init; }
    public bool CanAttack { get; init; } = true;
    public bool UseCasterStatMod { get; init; }
    public FormSpecialKind Special { get; init; } = FormSpecialKind.None;
    public int SpecialValue { get; init; }
    public string Description { get; init; } = string.Empty;
}

public sealed class TransformationInstance
{
    public FormDefinition Form { get; init; } = null!;
    public string SourceSpellId { get; init; } = string.Empty;
    public int TempHpRemaining { get; set; }
    public bool FirstHitPrimed { get; set; }
}

public enum SpellSupportState
{
    Active,
    FutureGated
}

public enum PlayerConditionKind { Poisoned, Weakened }

public sealed class PlayerConditionState
{
    public PlayerConditionKind Kind { get; set; }
    public int Potency { get; set; }
    public int RemainingTurns { get; set; }
}

public enum SpellSaveDamageBehavior
{
    None,
    NegateOnSave,
    HalfOnSave
}

public enum SpellCombatFamily
{
    DirectDamage,
    BurstDamage,
    DamageOverTime,
    MarkDebuff,
    ControlSpell,
    DebuffHex,
    SmiteStrike,
    HealSupport,
    WeaponRider,
    SelfBuff,
    SummonConjuration,
    HazardZone,
    Utility,
    TransformationPolymorph
}

public enum SpellElement
{
    Unknown,
    Fire,
    Cold,
    Lightning,
    Acid,
    Thunder,
    Radiant,
    Necrotic,
    Psychic,
    Force,
    Nature,
    Piercing,
    Water,
    Arcane,
    Poison,
    Elemental
}

[Flags]
public enum CreatureTypeTag
{
    None = 0,
    Humanoid = 1 << 0,
    Beast = 1 << 1,
    Undead = 1 << 2,
    Giant = 1 << 3,
    Monstrosity = 1 << 4,
    Construct = 1 << 5,
    Any = Humanoid | Beast | Undead | Giant | Monstrosity | Construct
}

public enum CombatStatusKind
{
    Poison,
    Burning,
    Corroded,
    Chilled,
    Shocked,
    Blinded,
    Slowed,
    Feared,
    Rooted,
    Restrained,
    Paralyzed,
    Incapacitated,
    Prone,
    Marked,
    Stunned,
    Weakened,
    Cursed
}

public sealed class CombatStatusApplySpec
{
    public required CombatStatusKind Kind { get; init; }
    public int Potency { get; init; } = 1;
    public int DurationTurns { get; init; } = 1;
    public int ChancePercent { get; init; } = 100;
    public StatName? InitialSaveStat { get; init; }
    public StatName? RepeatSaveStat { get; init; }
    public bool BreaksOnDamageTaken { get; init; }
}

public sealed class SpellEffectRouteSpec
{
    public SpellEffectRouteKind RouteKind { get; init; } = SpellEffectRouteKind.DirectDamage;
    public SpellTargetShape TargetShape { get; init; } = SpellTargetShape.SingleEnemy;
    public SpellElement Element { get; init; } = SpellElement.Unknown;
    public SpellCombatFamily CombatFamily { get; init; } = SpellCombatFamily.DirectDamage;
    public SpellSupportState SupportState { get; init; } = SpellSupportState.Active;
    public bool DealsDirectDamage { get; init; } = true;
    public string FutureRequirement { get; init; } = string.Empty;
    public string RuntimeBehaviorNote { get; init; } = string.Empty;
    public int AreaRadiusTiles { get; init; }
    public bool RequiresConcentration { get; init; }
    public StatName? InitialSaveStat { get; init; }
    public SpellSaveDamageBehavior SaveDamageBehavior { get; init; }
    public CreatureTypeTag AllowedCreatureTypes { get; init; } = CreatureTypeTag.Any;
    public CombatHazardSpec? HazardSpec { get; init; }
    public IReadOnlyList<CombatStatusApplySpec> OnHitStatuses { get; init; } = Array.Empty<CombatStatusApplySpec>();

    public bool IsFutureGated => SupportState != SpellSupportState.Active;
}

public sealed class CombatHazardSpec
{
    public int RadiusTiles { get; init; }
    public int DurationRounds { get; init; } = 3;
    public bool FollowsPlayer { get; init; }
    public bool RequiresConcentration { get; init; }
    public bool TriggersOnTurnStart { get; init; } = true;
    public bool TriggersOnEntry { get; init; }
    public StatName? InitialSaveStat { get; init; }
    public SpellSaveDamageBehavior SaveDamageBehavior { get; init; }
    public IReadOnlyList<CombatStatusApplySpec> OnTriggerStatuses { get; init; } = Array.Empty<CombatStatusApplySpec>();
}

public sealed class CombatStatusState
{
    public required CombatStatusKind Kind { get; init; }
    public required int Potency { get; set; }
    public required int RemainingTurns { get; set; }
    public string SourceSpellId { get; init; } = string.Empty;
    public string SourceLabel { get; init; } = string.Empty;
    public StatName? RepeatSaveStat { get; set; }
    public int SaveDc { get; set; }
    public bool BreaksOnDamageTaken { get; set; }
}

public sealed class CombatHazardState
{
    public string InstanceId { get; init; } = string.Empty;
    public string SourceSpellId { get; init; } = string.Empty;
    public string SourceLabel { get; init; } = string.Empty;
    public SpellElement Element { get; init; } = SpellElement.Unknown;
    public int BaseDamage { get; init; }
    public int Variance { get; init; }
    public int ArmorBypass { get; init; }
    public int CenterX { get; set; }
    public int CenterY { get; set; }
    public int RadiusTiles { get; init; }
    public int RemainingRounds { get; set; }
    public bool FollowsPlayer { get; init; }
    public bool RequiresConcentration { get; init; }
    public bool TriggersOnTurnStart { get; init; }
    public bool TriggersOnEntry { get; init; }
    public StatName? InitialSaveStat { get; init; }
    public SpellSaveDamageBehavior SaveDamageBehavior { get; init; }
    public List<CombatStatusApplySpec> OnTriggerStatuses { get; } = new();
}

public static class SpellData
{
    // House rule to match requested pacing for level 1-6:
    // half-casters (Paladin/Ranger) can reach 3rd-level slots by level 6.
    // Set to false for strict SRD pacing.
    public static bool UseAcceleratedHalfCasterProgression = true;

    public static readonly Dictionary<string, SpellDefinition> ById = new()
    {
        // Mage
        ["mage_fire_bolt"] = new() { Id = "mage_fire_bolt", Name = "Fire Bolt", ClassName = "Mage", SpellLevel = 0, Description = "A bolt of fire scorches one foe and can leave it burning.", ScalingStat = StatName.Intelligence, BaseDamage = 4, Variance = 3, ArmorBypass = 1, DamageTag = "fire", SuppressCounterAttack = false, CantripDiceSides = 10 },
        ["mage_ray_of_frost"] = new() { Id = "mage_ray_of_frost", Name = "Ray of Frost", ClassName = "Mage", SpellLevel = 0, Description = "A freezing beam chills one foe and slows its advance.", ScalingStat = StatName.Intelligence, BaseDamage = 3, Variance = 3, ArmorBypass = 1, DamageTag = "cold", SuppressCounterAttack = false, CantripDiceSides = 8 },
        ["mage_chill_touch"] = new() { Id = "mage_chill_touch", Name = "Chill Touch", ClassName = "Mage", SpellLevel = 0, Description = "Necrotic force saps one foe and leaves it weakened.", ScalingStat = StatName.Intelligence, BaseDamage = 3, Variance = 4, ArmorBypass = 1, DamageTag = "necrotic", SuppressCounterAttack = false, CantripDiceSides = 8 },
        ["mage_shocking_grasp"] = new() { Id = "mage_shocking_grasp", Name = "Shocking Grasp", ClassName = "Mage", SpellLevel = 0, Description = "Crackling lightning shocks one foe and breaks retaliation.", ScalingStat = StatName.Intelligence, BaseDamage = 4, Variance = 2, ArmorBypass = 2, DamageTag = "lightning", SuppressCounterAttack = true, CantripDiceSides = 8 },
        ["mage_acid_splash"] = new() { Id = "mage_acid_splash", Name = "Acid Splash", ClassName = "Mage", SpellLevel = 0, Description = "Corrosive acid splashes a foe and can catch one nearby enemy.", ScalingStat = StatName.Intelligence, BaseDamage = 3, Variance = 3, ArmorBypass = 1, DamageTag = "acid", SuppressCounterAttack = false, CantripDiceSides = 6 },
        ["mage_magic_missile"] = new() { Id = "mage_magic_missile", Name = "Magic Missile", ClassName = "Mage", SpellLevel = 1, Description = "Reliable force darts hammer a single foe.", ScalingStat = StatName.Intelligence, BaseDamage = 10, Variance = 4, ArmorBypass = 1, DamageTag = "force", SuppressCounterAttack = false },
        ["mage_burning_hands"] = new() { Id = "mage_burning_hands", Name = "Burning Hands", ClassName = "Mage", SpellLevel = 1, Description = "A short cone of flame scorches nearby foes and can burn them.", ScalingStat = StatName.Intelligence, BaseDamage = 11, Variance = 5, ArmorBypass = 1, DamageTag = "fire", SuppressCounterAttack = false },
        ["mage_chromatic_orb"] = new() { Id = "mage_chromatic_orb", Name = "Chromatic Orb", ClassName = "Mage", SpellLevel = 1, Description = "Choose an element and hurl a volatile orb into one foe.", ScalingStat = StatName.Intelligence, BaseDamage = 12, Variance = 6, ArmorBypass = 1, DamageTag = "elemental", SuppressCounterAttack = false },
        ["mage_ice_knife"] = new() { Id = "mage_ice_knife", Name = "Ice Knife", ClassName = "Mage", SpellLevel = 1, Description = "A frozen shard bursts on impact and chills nearby foes.", ScalingStat = StatName.Intelligence, BaseDamage = 11, Variance = 5, ArmorBypass = 1, DamageTag = "cold", SuppressCounterAttack = false },
        ["mage_find_familiar"] = new() { Id = "mage_find_familiar", Name = "Find Familiar", ClassName = "Mage", SpellLevel = 1, Description = "Summon a tiny familiar that attacks your foe each turn. 1d4 + INT force. Fragile. Concentration.", ScalingStat = StatName.Intelligence, BaseDamage = 8, Variance = 3, ArmorBypass = 0, DamageTag = "force", SuppressCounterAttack = true },
        ["mage_scorching_ray"] = new() { Id = "mage_scorching_ray", Name = "Scorching Ray", ClassName = "Mage", SpellLevel = 2, Description = "Twin rays of fire hammer a foe in rapid succession.", ScalingStat = StatName.Intelligence, BaseDamage = 14, Variance = 6, ArmorBypass = 1, DamageTag = "fire", SuppressCounterAttack = false },
        ["mage_shatter"] = new() { Id = "mage_shatter", Name = "Shatter", ClassName = "Mage", SpellLevel = 2, Description = "A thunder burst cracks a small area around the impact point.", ScalingStat = StatName.Intelligence, BaseDamage = 14, Variance = 5, ArmorBypass = 2, DamageTag = "thunder", SuppressCounterAttack = false },
        ["mage_web"] = new() { Id = "mage_web", Name = "Web", ClassName = "Mage", SpellLevel = 2, Description = "Spin a sticky web patch that can root enemies who enter or linger.", ScalingStat = StatName.Intelligence, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "arcane", SuppressCounterAttack = true },
        ["mage_melfs_acid_arrow"] = new() { Id = "mage_melfs_acid_arrow", Name = "Melf's Acid Arrow", ClassName = "Mage", SpellLevel = 2, Description = "A streaking acid arrow hits hard and keeps corroding the foe.", ScalingStat = StatName.Intelligence, BaseDamage = 15, Variance = 6, ArmorBypass = 2, DamageTag = "acid", SuppressCounterAttack = false },
        ["mage_flaming_sphere"] = new() { Id = "mage_flaming_sphere", Name = "Flaming Sphere", ClassName = "Mage", SpellLevel = 2, Description = "Conjure a roiling ball of fire that rams your enemies each turn. 2d6 fire. Concentration.", ScalingStat = StatName.Intelligence, BaseDamage = 14, Variance = 5, ArmorBypass = 1, DamageTag = "fire", SuppressCounterAttack = true },
        ["mage_summon_elemental"] = new() { Id = "mage_summon_elemental", Name = "Summon Elemental", ClassName = "Mage", SpellLevel = 2, Description = "Call forth an elemental spirit of fire and stone. 1d10 fire per hit. Concentration.", ScalingStat = StatName.Intelligence, BaseDamage = 14, Variance = 5, ArmorBypass = 1, DamageTag = "fire", SuppressCounterAttack = true },
        ["mage_fireball"] = new() { Id = "mage_fireball", Name = "Fireball", ClassName = "Mage", SpellLevel = 3, Description = "Detonate a fiery blast over an area and leave foes burning.", ScalingStat = StatName.Intelligence, BaseDamage = 19, Variance = 8, ArmorBypass = 2, DamageTag = "fire", SuppressCounterAttack = false },
        ["mage_lightning_bolt"] = new() { Id = "mage_lightning_bolt", Name = "Lightning Bolt", ClassName = "Mage", SpellLevel = 3, Description = "A line of lightning rips through foes and can shock them.", ScalingStat = StatName.Intelligence, BaseDamage = 18, Variance = 8, ArmorBypass = 3, DamageTag = "lightning", SuppressCounterAttack = false },
        ["mage_tidal_wave"] = new() { Id = "mage_tidal_wave", Name = "Tidal Wave", ClassName = "Mage", SpellLevel = 3, Description = "A crashing line of water slams foes and slows them.", ScalingStat = StatName.Intelligence, BaseDamage = 17, Variance = 7, ArmorBypass = 2, DamageTag = "water", SuppressCounterAttack = false },
        ["mage_summon_fey"] = new() { Id = "mage_summon_fey", Name = "Summon Fey", ClassName = "Mage", SpellLevel = 3, Description = "Call forth a fey spirit that teleports and strikes each turn. 2d6 + INT force. Concentration.", ScalingStat = StatName.Intelligence, BaseDamage = 16, Variance = 6, ArmorBypass = 2, DamageTag = "force", SuppressCounterAttack = true },
        ["mage_summon_undead"] = new() { Id = "mage_summon_undead", Name = "Summon Undead", ClassName = "Mage", SpellLevel = 3, Description = "Raise a spectral undead that claws at your foes each turn. 1d8+2 + INT necrotic. Concentration.", ScalingStat = StatName.Intelligence, BaseDamage = 15, Variance = 6, ArmorBypass = 2, DamageTag = "necrotic", SuppressCounterAttack = true },
        ["mage_summon_shadowspawn"] = new() { Id = "mage_summon_shadowspawn", Name = "Summon Shadowspawn", ClassName = "Mage", SpellLevel = 3, Description = "Call a dread shadow entity that terrorizes your foes each turn. 1d12 + INT psychic. Concentration.", ScalingStat = StatName.Intelligence, BaseDamage = 16, Variance = 7, ArmorBypass = 2, DamageTag = "psychic", SuppressCounterAttack = true },
        ["mage_phantom_steed"] = new() { Id = "mage_phantom_steed", Name = "Phantom Steed", ClassName = "Mage", SpellLevel = 3, Description = "Conjure a spectral horse. +2 defense, +15 flee chance while mounted. Concentration.", ScalingStat = StatName.Intelligence, BaseDamage = 10, Variance = 3, ArmorBypass = 0, DamageTag = "force", SuppressCounterAttack = true },
        // Batch 1 — Mage buffs
        ["mage_mage_armor"] = new() { Id = "mage_mage_armor", Name = "Mage Armor", ClassName = "Mage", SpellLevel = 1, Description = "An invisible barrier of force surrounds you. +3 AC while unarmored.", ScalingStat = StatName.Intelligence, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "force", SuppressCounterAttack = true },
        ["mage_shield"] = new() { Id = "mage_shield", Name = "Shield", ClassName = "Mage", SpellLevel = 1, Description = "A shimmering barrier of force grants +5 AC until your next turn.", ScalingStat = StatName.Intelligence, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "force", SuppressCounterAttack = true },
        ["mage_false_life"] = new() { Id = "mage_false_life", Name = "False Life", ClassName = "Mage", SpellLevel = 1, Description = "Bolster yourself with necromantic energy, gaining 1d4+4 temporary hit points.", ScalingStat = StatName.Intelligence, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "necrotic", SuppressCounterAttack = true },
        ["mage_blur"] = new() { Id = "mage_blur", Name = "Blur", ClassName = "Mage", SpellLevel = 2, Description = "Your body shimmers, causing enemy attacks to roll with disadvantage. Concentration.", ScalingStat = StatName.Intelligence, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "arcane", SuppressCounterAttack = true },
        ["mage_haste"] = new() { Id = "mage_haste", Name = "Haste", ClassName = "Mage", SpellLevel = 3, Description = "Supernatural speed grants +2 AC and +2 combat movement. Concentration.", ScalingStat = StatName.Intelligence, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "arcane", SuppressCounterAttack = true },
        // Batch 2 — Mage tactical spells
        ["mage_misty_step"] = new() { Id = "mage_misty_step", Name = "Misty Step", ClassName = "Mage", SpellLevel = 2, Description = "Briefly surrounded by silvery mist, you teleport up to 30 feet. +3 move points this turn.", ScalingStat = StatName.Intelligence, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "arcane", SuppressCounterAttack = true },
        ["mage_mirror_image"] = new() { Id = "mage_mirror_image", Name = "Mirror Image", ClassName = "Mage", SpellLevel = 2, Description = "Three illusory duplicates absorb incoming attacks. No concentration.", ScalingStat = StatName.Intelligence, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "arcane", SuppressCounterAttack = true },
        ["mage_expeditious_retreat"] = new() { Id = "mage_expeditious_retreat", Name = "Expeditious Retreat", ClassName = "Mage", SpellLevel = 1, Description = "Arcane speed surges through you. +15% flee chance. Concentration.", ScalingStat = StatName.Intelligence, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "arcane", SuppressCounterAttack = true },
        ["mage_enhance_ability"] = new() { Id = "mage_enhance_ability", Name = "Enhance Ability", ClassName = "Mage", SpellLevel = 2, Description = "Cat's Grace enhances your reflexes. +2 AC, +3% flee. Concentration.", ScalingStat = StatName.Intelligence, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "arcane", SuppressCounterAttack = true },
        // Batch 3 — Mage reactive & retaliation spells
        ["mage_hellish_rebuke"] = new() { Id = "mage_hellish_rebuke", Name = "Hellish Rebuke", ClassName = "Mage", SpellLevel = 1, Description = "Hellfire crackles around you. The next attacker takes 2d6 fire damage (consumed).", ScalingStat = StatName.Intelligence, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "fire", SuppressCounterAttack = true },
        ["mage_armor_of_agathys"] = new() { Id = "mage_armor_of_agathys", Name = "Armor of Agathys", ClassName = "Mage", SpellLevel = 1, Description = "Frost armor encases you. +8 frost temp HP; attackers take 1d8 cold while active.", ScalingStat = StatName.Intelligence, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "cold", SuppressCounterAttack = true },
        ["mage_fire_shield"] = new() { Id = "mage_fire_shield", Name = "Fire Shield", ClassName = "Mage", SpellLevel = 2, Description = "Flames wreathe your body. Attackers take 2d8 fire damage. No concentration.", ScalingStat = StatName.Intelligence, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "fire", SuppressCounterAttack = true },
        ["mage_stoneskin"] = new() { Id = "mage_stoneskin", Name = "Stoneskin", ClassName = "Mage", SpellLevel = 2, Description = "Your skin hardens to stone. Incoming damage reduced by 3. Concentration.", ScalingStat = StatName.Intelligence, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "arcane", SuppressCounterAttack = true },

        // Cleric
        ["cleric_healing_word"] = new() { Id = "cleric_healing_word", Name = "Healing Word", ClassName = "Cleric", SpellLevel = 1, Description = "A whispered holy word mends wounds quickly — swift as a bonus action.", ScalingStat = StatName.Wisdom, BaseDamage = 1, Variance = 3, ArmorBypass = 0, DamageTag = "healing", SuppressCounterAttack = true, IsHealSpell = true },
        ["cleric_lesser_restoration"] = new() { Id = "cleric_lesser_restoration", Name = "Lesser Restoration", ClassName = "Cleric", SpellLevel = 2, Description = "Sacred energy purges one affliction from your body.", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "healing", SuppressCounterAttack = true, IsCleanseSpell = true },
        ["cleric_cure_wounds"] = new() { Id = "cleric_cure_wounds", Name = "Cure Wounds", ClassName = "Cleric", SpellLevel = 1, Description = "A touch of sacred energy restores your wounds.", ScalingStat = StatName.Wisdom, BaseDamage = 8, Variance = 7, ArmorBypass = 0, DamageTag = "healing", SuppressCounterAttack = true, IsHealSpell = true },
        ["cleric_prayer_of_healing"] = new() { Id = "cleric_prayer_of_healing", Name = "Prayer of Healing", ClassName = "Cleric", SpellLevel = 2, Description = "A sustained holy prayer mends deep wounds.", ScalingStat = StatName.Wisdom, BaseDamage = 14, Variance = 7, ArmorBypass = 0, DamageTag = "healing", SuppressCounterAttack = true, IsHealSpell = true },
        ["cleric_sacred_flame"] = new() { Id = "cleric_sacred_flame", Name = "Sacred Flame", ClassName = "Cleric", SpellLevel = 0, Description = "Radiant fire lashes one foe from above.", ScalingStat = StatName.Wisdom, BaseDamage = 4, Variance = 3, ArmorBypass = 1, DamageTag = "radiant", SuppressCounterAttack = false, CantripDiceSides = 8 },
        ["cleric_toll_the_dead"] = new() { Id = "cleric_toll_the_dead", Name = "Toll the Dead", ClassName = "Cleric", SpellLevel = 0, Description = "A grave knell batters one foe and strikes harder if it is wounded.", ScalingStat = StatName.Wisdom, BaseDamage = 4, Variance = 4, ArmorBypass = 1, DamageTag = "necrotic", SuppressCounterAttack = false, CantripDiceSides = 8 },
        ["cleric_word_of_radiance"] = new() { Id = "cleric_word_of_radiance", Name = "Word of Radiance", ClassName = "Cleric", SpellLevel = 0, Description = "A burst of sacred light sears foes around you.", ScalingStat = StatName.Wisdom, BaseDamage = 3, Variance = 3, ArmorBypass = 1, DamageTag = "radiant", SuppressCounterAttack = false, CantripDiceSides = 6 },
        ["cleric_guiding_bolt"] = new() { Id = "cleric_guiding_bolt", Name = "Guiding Bolt", ClassName = "Cleric", SpellLevel = 1, Description = "A radiant bolt hits hard and marks the foe for extra damage.", ScalingStat = StatName.Wisdom, BaseDamage = 11, Variance = 5, ArmorBypass = 1, DamageTag = "radiant", SuppressCounterAttack = false },
        ["cleric_inflict_wounds"] = new() { Id = "cleric_inflict_wounds", Name = "Inflict Wounds", ClassName = "Cleric", SpellLevel = 1, Description = "A necrotic touch withers flesh and spirit.", ScalingStat = StatName.Wisdom, BaseDamage = 12, Variance = 6, ArmorBypass = 1, DamageTag = "necrotic", SuppressCounterAttack = false },
        ["cleric_command"] = new() { Id = "cleric_command", Name = "Command", ClassName = "Cleric", SpellLevel = 1, Description = "Speak a divine command such as Halt, Flee, or Grovel to one foe.", ScalingStat = StatName.Wisdom, BaseDamage = 8, Variance = 3, ArmorBypass = 0, DamageTag = "psychic", SuppressCounterAttack = true },
        ["cleric_bane"] = new() { Id = "cleric_bane", Name = "Bane", ClassName = "Cleric", SpellLevel = 1, Description = "Lay a weakening curse over a small cluster of enemies.", ScalingStat = StatName.Wisdom, BaseDamage = 9, Variance = 3, ArmorBypass = 0, DamageTag = "psychic", SuppressCounterAttack = true },
        ["cleric_spiritual_weapon"] = new() { Id = "cleric_spiritual_weapon", Name = "Spiritual Weapon", ClassName = "Cleric", SpellLevel = 2, Description = "Summon a spectral weapon that strikes your foe each turn. 1d8 + WIS force per hit. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 15, Variance = 6, ArmorBypass = 1, DamageTag = "force", SuppressCounterAttack = true },
        ["cleric_hold_person"] = new() { Id = "cleric_hold_person", Name = "Hold Person", ClassName = "Cleric", SpellLevel = 2, Description = "A binding prayer can briefly paralyze one foe while you focus.", ScalingStat = StatName.Wisdom, BaseDamage = 10, Variance = 3, ArmorBypass = 0, DamageTag = "radiant", SuppressCounterAttack = true },
        ["cleric_blindness"] = new() { Id = "cleric_blindness", Name = "Blindness", ClassName = "Cleric", SpellLevel = 2, Description = "A curse of darkness robs one foe of sight for a short time.", ScalingStat = StatName.Wisdom, BaseDamage = 11, Variance = 4, ArmorBypass = 1, DamageTag = "necrotic", SuppressCounterAttack = true },
        ["cleric_spirit_guardians"] = new() { Id = "cleric_spirit_guardians", Name = "Spirit Guardians", ClassName = "Cleric", SpellLevel = 3, Description = "Radiant spirits orbit you and scour enemies who stay near.", ScalingStat = StatName.Wisdom, BaseDamage = 18, Variance = 8, ArmorBypass = 2, DamageTag = "radiant", SuppressCounterAttack = false },
        ["cleric_bestow_curse"] = new() { Id = "cleric_bestow_curse", Name = "Bestow Curse", ClassName = "Cleric", SpellLevel = 3, Description = "Lay a heavy curse that weakens and exposes one foe.", ScalingStat = StatName.Wisdom, BaseDamage = 16, Variance = 6, ArmorBypass = 2, DamageTag = "necrotic", SuppressCounterAttack = true },
        ["cleric_flame_strike"] = new() { Id = "cleric_flame_strike", Name = "Flame Strike", ClassName = "Cleric", SpellLevel = 3, Description = "Call down holy fire on a small area and leave foes burning.", ScalingStat = StatName.Wisdom, BaseDamage = 19, Variance = 8, ArmorBypass = 2, DamageTag = "fire", SuppressCounterAttack = false },
        ["cleric_animate_dead"] = new() { Id = "cleric_animate_dead", Name = "Animate Dead", ClassName = "Cleric", SpellLevel = 3, Description = "Raise a skeleton warrior that fights by your side each turn. 1d6+1 + WIS necrotic. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 14, Variance = 5, ArmorBypass = 1, DamageTag = "necrotic", SuppressCounterAttack = true },
        ["cleric_summon_celestial"] = new() { Id = "cleric_summon_celestial", Name = "Summon Celestial", ClassName = "Cleric", SpellLevel = 3, Description = "Call a radiant guardian angel that smites your foes each turn. 1d10 + WIS radiant. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 17, Variance = 7, ArmorBypass = 2, DamageTag = "radiant", SuppressCounterAttack = true },
        // Batch 1 — Cleric buffs
        ["cleric_shield_of_faith"] = new() { Id = "cleric_shield_of_faith", Name = "Shield of Faith", ClassName = "Cleric", SpellLevel = 1, Description = "A shimmering field of divine energy grants +2 AC. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "radiant", SuppressCounterAttack = true },
        // Batch 2 — Cleric tactical spells
        ["cleric_protection_evg"] = new() { Id = "cleric_protection_evg", Name = "Protection from Evil", ClassName = "Cleric", SpellLevel = 1, Description = "A divine ward shields you from evil. +1 AC. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "radiant", SuppressCounterAttack = true },
        ["cleric_sanctuary"] = new() { Id = "cleric_sanctuary", Name = "Sanctuary", ClassName = "Cleric", SpellLevel = 1, Description = "A divine shield wards you — enemies must pass a Wisdom save to attack. Breaks if you attack. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "radiant", SuppressCounterAttack = true },
        ["cleric_enhance_ability"] = new() { Id = "cleric_enhance_ability", Name = "Enhance Ability", ClassName = "Cleric", SpellLevel = 2, Description = "Cat's Grace enhances your reflexes. +2 AC, +3% flee. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "radiant", SuppressCounterAttack = true },
        ["cleric_bless"] = new() { Id = "cleric_bless", Name = "Bless", ClassName = "Cleric", SpellLevel = 1, Description = "Divine favor guides your strikes and fortifies your will. +1d4 damage and saves. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "radiant", SuppressCounterAttack = true },
        ["cleric_aid"] = new() { Id = "cleric_aid", Name = "Aid", ClassName = "Cleric", SpellLevel = 2, Description = "Bolster yourself with toughness. +5 max HP and current HP.", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "healing", SuppressCounterAttack = true },
        // Batch 3 — Cleric reactive & retaliation spells
        ["cleric_wrath_of_storm"] = new() { Id = "cleric_wrath_of_storm", Name = "Wrath of the Storm", ClassName = "Cleric", SpellLevel = 1, Description = "Lightning crackles around you. The next attacker takes 2d8 lightning (consumed).", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "lightning", SuppressCounterAttack = true },
        ["cleric_spirit_shroud"] = new() { Id = "cleric_spirit_shroud", Name = "Spirit Shroud", ClassName = "Cleric", SpellLevel = 3, Description = "Vengeful spirits swirl around you. +1d8 radiant melee damage; attackers take 1d6 radiant. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "radiant", SuppressCounterAttack = true },
        ["cleric_death_ward"] = new() { Id = "cleric_death_ward", Name = "Death Ward", ClassName = "Cleric", SpellLevel = 2, Description = "A golden ward shimmers — if you would drop to 0 HP, you survive at 1 HP instead (consumed).", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "radiant", SuppressCounterAttack = true },

        // Bard
        ["bard_vicious_mockery"] = new() { Id = "bard_vicious_mockery", Name = "Vicious Mockery", ClassName = "Bard", SpellLevel = 0, Description = "A cutting insult wounds the mind and weakens the foe.", ScalingStat = StatName.Charisma, BaseDamage = 3, Variance = 3, ArmorBypass = 0, DamageTag = "psychic", SuppressCounterAttack = true, CantripDiceSides = 4 },
        ["bard_thunderclap"] = new() { Id = "bard_thunderclap", Name = "Thunderclap", ClassName = "Bard", SpellLevel = 0, Description = "A thunderclap bursts around you and strikes nearby foes.", ScalingStat = StatName.Charisma, BaseDamage = 4, Variance = 3, ArmorBypass = 1, DamageTag = "thunder", SuppressCounterAttack = false, CantripDiceSides = 6 },
        ["bard_mind_sliver"] = new() { Id = "bard_mind_sliver", Name = "Mind Sliver", ClassName = "Bard", SpellLevel = 0, Description = "A psychic spike blunts one foe's next offense.", ScalingStat = StatName.Charisma, BaseDamage = 3, Variance = 3, ArmorBypass = 1, DamageTag = "psychic", SuppressCounterAttack = true, CantripDiceSides = 6 },
        ["bard_healing_word"] = new() { Id = "bard_healing_word", Name = "Healing Word", ClassName = "Bard", SpellLevel = 1, Description = "An inspiring word of magic mends wounds quickly — swift as a bonus action.", ScalingStat = StatName.Charisma, BaseDamage = 1, Variance = 3, ArmorBypass = 0, DamageTag = "healing", SuppressCounterAttack = true, IsHealSpell = true },
        ["bard_dissonant_whispers"] = new() { Id = "bard_dissonant_whispers", Name = "Dissonant Whispers", ClassName = "Bard", SpellLevel = 1, Description = "Malignant whispers send one foe reeling in fear.", ScalingStat = StatName.Charisma, BaseDamage = 10, Variance = 5, ArmorBypass = 0, DamageTag = "psychic", SuppressCounterAttack = false },
        ["bard_thunderwave"] = new() { Id = "bard_thunderwave", Name = "Thunderwave", ClassName = "Bard", SpellLevel = 1, Description = "A burst of thunder batters nearby foes and hurls them back.", ScalingStat = StatName.Charisma, BaseDamage = 10, Variance = 5, ArmorBypass = 1, DamageTag = "thunder", SuppressCounterAttack = true },
        ["bard_hideous_laughter"] = new() { Id = "bard_hideous_laughter", Name = "Hideous Laughter", ClassName = "Bard", SpellLevel = 1, Description = "Crippling laughter can leave one foe helpless while you focus.", ScalingStat = StatName.Charisma, BaseDamage = 9, Variance = 4, ArmorBypass = 0, DamageTag = "psychic", SuppressCounterAttack = true },
        ["bard_shatter"] = new() { Id = "bard_shatter", Name = "Shatter", ClassName = "Bard", SpellLevel = 2, Description = "A shattering note blasts a small area with thunder.", ScalingStat = StatName.Charisma, BaseDamage = 14, Variance = 6, ArmorBypass = 1, DamageTag = "thunder", SuppressCounterAttack = false },
        ["bard_heat_metal"] = new() { Id = "bard_heat_metal", Name = "Heat Metal", ClassName = "Bard", SpellLevel = 2, Description = "Searing heat burns one foe over time while you hold focus.", ScalingStat = StatName.Charisma, BaseDamage = 13, Variance = 5, ArmorBypass = 2, DamageTag = "fire", SuppressCounterAttack = false },
        ["bard_cloud_of_daggers"] = new() { Id = "bard_cloud_of_daggers", Name = "Cloud of Daggers", ClassName = "Bard", SpellLevel = 2, Description = "Whirling blades fill one tile and cut enemies standing in it.", ScalingStat = StatName.Charisma, BaseDamage = 13, Variance = 5, ArmorBypass = 2, DamageTag = "force", SuppressCounterAttack = false },
        ["bard_hypnotic_pattern"] = new() { Id = "bard_hypnotic_pattern", Name = "Hypnotic Pattern", ClassName = "Bard", SpellLevel = 3, Description = "A mesmerizing cone can leave enemies spellbound while you focus.", ScalingStat = StatName.Charisma, BaseDamage = 14, Variance = 5, ArmorBypass = 2, DamageTag = "psychic", SuppressCounterAttack = true },
        ["bard_fear"] = new() { Id = "bard_fear", Name = "Fear", ClassName = "Bard", SpellLevel = 3, Description = "Project a cone of terror that can drive enemies back in fear.", ScalingStat = StatName.Charisma, BaseDamage = 15, Variance = 6, ArmorBypass = 2, DamageTag = "psychic", SuppressCounterAttack = true },
        ["bard_slow"] = new() { Id = "bard_slow", Name = "Slow", ClassName = "Bard", SpellLevel = 3, Description = "Warp time in a small area to mire foes in sluggish motion.", ScalingStat = StatName.Charisma, BaseDamage = 14, Variance = 5, ArmorBypass = 1, DamageTag = "arcane", SuppressCounterAttack = true },
        ["bard_summon_fey"] = new() { Id = "bard_summon_fey", Name = "Summon Fey", ClassName = "Bard", SpellLevel = 3, Description = "Call forth a fey trickster that bewilders and strikes each turn. 1d8+1 + CHA psychic. Concentration.", ScalingStat = StatName.Charisma, BaseDamage = 15, Variance = 5, ArmorBypass = 1, DamageTag = "psychic", SuppressCounterAttack = true },
        // Batch 1 — Bard buffs
        ["bard_heroism"] = new() { Id = "bard_heroism", Name = "Heroism", ClassName = "Bard", SpellLevel = 1, Description = "An inspiring touch grants bravery. Temp HP each turn, immune to fear. Concentration.", ScalingStat = StatName.Charisma, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "psychic", SuppressCounterAttack = true },
        // Batch 2 — Bard tactical spells
        ["bard_hex"] = new() { Id = "bard_hex", Name = "Hex", ClassName = "Bard", SpellLevel = 1, Description = "Curse a foe. +1d4 necrotic per hit, enemy −2 attack. Concentration.", ScalingStat = StatName.Charisma, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "psychic", SuppressCounterAttack = true },
        ["bard_enhance_ability"] = new() { Id = "bard_enhance_ability", Name = "Enhance Ability", ClassName = "Bard", SpellLevel = 2, Description = "Cat's Grace enhances your reflexes. +2 AC, +3% flee. Concentration.", ScalingStat = StatName.Charisma, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "arcane", SuppressCounterAttack = true },
        // Batch 3 — Bard reactive & retaliation spells
        ["bard_cutting_words"] = new() { Id = "bard_cutting_words", Name = "Cutting Words", ClassName = "Bard", SpellLevel = 1, Description = "A cutting remark ready on your lips — next enemy hit reduced by 1d8 (consumed).", ScalingStat = StatName.Charisma, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "psychic", SuppressCounterAttack = true },
        ["bard_greater_invisibility"] = new() { Id = "bard_greater_invisibility", Name = "Greater Invisibility", ClassName = "Bard", SpellLevel = 2, Description = "You vanish from sight. Advantage on attacks, enemies have disadvantage. Concentration.", ScalingStat = StatName.Charisma, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "arcane", SuppressCounterAttack = true },

        // Paladin
        ["paladin_cure_wounds"] = new() { Id = "paladin_cure_wounds", Name = "Cure Wounds", ClassName = "Paladin", SpellLevel = 1, Description = "A touch of holy light mends your wounds.", ScalingStat = StatName.Charisma, BaseDamage = 8, Variance = 7, ArmorBypass = 0, DamageTag = "healing", SuppressCounterAttack = true, IsHealSpell = true },
        ["paladin_searing_smite"] = new() { Id = "paladin_searing_smite", Name = "Searing Smite", ClassName = "Paladin", SpellLevel = 1, Description = "A searing smite strike burns one foe and can leave it aflame.", ScalingStat = StatName.Charisma, BaseDamage = 12, Variance = 5, ArmorBypass = 1, DamageTag = "fire", SuppressCounterAttack = false },
        ["paladin_thunderous_smite"] = new() { Id = "paladin_thunderous_smite", Name = "Thunderous Smite", ClassName = "Paladin", SpellLevel = 1, Description = "A thunderous smite strike batters one foe and blasts it backward.", ScalingStat = StatName.Charisma, BaseDamage = 12, Variance = 5, ArmorBypass = 2, DamageTag = "thunder", SuppressCounterAttack = false },
        ["paladin_wrathful_smite"] = new() { Id = "paladin_wrathful_smite", Name = "Wrathful Smite", ClassName = "Paladin", SpellLevel = 1, Description = "A wrathful smite strike leaves one foe shaken with fear.", ScalingStat = StatName.Charisma, BaseDamage = 11, Variance = 5, ArmorBypass = 1, DamageTag = "psychic", SuppressCounterAttack = false },
        ["paladin_divine_favor"] = new() { Id = "paladin_divine_favor", Name = "Divine Favor", ClassName = "Paladin", SpellLevel = 1, Description = "Sacred might empowers your weapon with radiant force. +1d4 radiant per hit. Concentration.", ScalingStat = StatName.Charisma, BaseDamage = 11, Variance = 4, ArmorBypass = 1, DamageTag = "radiant", SuppressCounterAttack = true },
        ["paladin_branding_smite"] = new() { Id = "paladin_branding_smite", Name = "Branding Smite", ClassName = "Paladin", SpellLevel = 2, Description = "A radiant smite strike brands one foe for extra incoming damage.", ScalingStat = StatName.Charisma, BaseDamage = 15, Variance = 6, ArmorBypass = 2, DamageTag = "radiant", SuppressCounterAttack = false },
        ["paladin_magic_weapon"] = new() { Id = "paladin_magic_weapon", Name = "Magic Weapon", ClassName = "Paladin", SpellLevel = 2, Description = "Imbue your weapon with arcane force. +1 attack, +1d6 force per hit while concentrating.", ScalingStat = StatName.Charisma, BaseDamage = 14, Variance = 5, ArmorBypass = 2, DamageTag = "force", SuppressCounterAttack = true },
        ["paladin_find_steed"] = new() { Id = "paladin_find_steed", Name = "Find Steed", ClassName = "Paladin", SpellLevel = 2, Description = "Summon a celestial warhorse. +2 defense, +15 flee chance while mounted. Concentration.", ScalingStat = StatName.Charisma, BaseDamage = 10, Variance = 3, ArmorBypass = 0, DamageTag = "radiant", SuppressCounterAttack = true },
        ["paladin_aura_of_vitality"] = new() { Id = "paladin_aura_of_vitality", Name = "Aura of Vitality", ClassName = "Paladin", SpellLevel = 3, Description = "A radiant healing aura surrounds you. At the start of each turn, you restore 2d6 HP.", ScalingStat = StatName.Charisma, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "healing", SuppressCounterAttack = true },
        ["paladin_blinding_smite"] = new() { Id = "paladin_blinding_smite", Name = "Blinding Smite", ClassName = "Paladin", SpellLevel = 3, Description = "A blinding smite strike floods one foe with searing light.", ScalingStat = StatName.Charisma, BaseDamage = 18, Variance = 7, ArmorBypass = 2, DamageTag = "radiant", SuppressCounterAttack = false },
        ["paladin_crusaders_mantle"] = new() { Id = "paladin_crusaders_mantle", Name = "Crusader's Mantle", ClassName = "Paladin", SpellLevel = 3, Description = "A radiant aura empowers your strikes. +1d6 radiant per hit, +1 defense while concentrating.", ScalingStat = StatName.Charisma, BaseDamage = 17, Variance = 6, ArmorBypass = 2, DamageTag = "radiant", SuppressCounterAttack = true },
        ["paladin_summon_celestial"] = new() { Id = "paladin_summon_celestial", Name = "Summon Celestial", ClassName = "Paladin", SpellLevel = 3, Description = "Call a celestial avenger that smites your foes each turn. 1d10+1 + CHA radiant. Concentration.", ScalingStat = StatName.Charisma, BaseDamage = 17, Variance = 7, ArmorBypass = 2, DamageTag = "radiant", SuppressCounterAttack = true },
        // Batch 1 — Paladin buffs
        ["paladin_shield_of_faith"] = new() { Id = "paladin_shield_of_faith", Name = "Shield of Faith", ClassName = "Paladin", SpellLevel = 1, Description = "A shimmering field of divine energy grants +2 AC. Concentration.", ScalingStat = StatName.Charisma, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "radiant", SuppressCounterAttack = true },
        // Batch 2 — Paladin tactical spells
        ["paladin_protection_evg"] = new() { Id = "paladin_protection_evg", Name = "Protection from Evil", ClassName = "Paladin", SpellLevel = 1, Description = "A divine ward shields you from evil. +1 AC. Concentration.", ScalingStat = StatName.Charisma, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "radiant", SuppressCounterAttack = true },
        ["paladin_compelled_duel"] = new() { Id = "paladin_compelled_duel", Name = "Compelled Duel", ClassName = "Paladin", SpellLevel = 1, Description = "Divine challenge marks your foe. +2 melee bonus. Concentration.", ScalingStat = StatName.Charisma, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "radiant", SuppressCounterAttack = true },
        ["paladin_heroism"] = new() { Id = "paladin_heroism", Name = "Heroism", ClassName = "Paladin", SpellLevel = 1, Description = "A touch of valor fills you with bravery. Temp HP each turn, immune to fear. Concentration.", ScalingStat = StatName.Charisma, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "radiant", SuppressCounterAttack = true },
        ["paladin_aid"] = new() { Id = "paladin_aid", Name = "Aid", ClassName = "Paladin", SpellLevel = 2, Description = "Bolster yourself with toughness. +5 max HP and current HP.", ScalingStat = StatName.Charisma, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "healing", SuppressCounterAttack = true },
        // Batch 3 — Paladin reactive & retaliation spells
        ["paladin_death_ward"] = new() { Id = "paladin_death_ward", Name = "Death Ward", ClassName = "Paladin", SpellLevel = 2, Description = "A golden ward shimmers — if you would drop to 0 HP, you survive at 1 HP instead (consumed).", ScalingStat = StatName.Charisma, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "radiant", SuppressCounterAttack = true },
        ["paladin_holy_rebuke"] = new() { Id = "paladin_holy_rebuke", Name = "Holy Rebuke", ClassName = "Paladin", SpellLevel = 1, Description = "Divine wrath gathers — the next attacker takes 2d6 radiant and you heal 1d4 (consumed).", ScalingStat = StatName.Charisma, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "radiant", SuppressCounterAttack = true },

        // Ranger
        ["ranger_hunters_mark"] = new() { Id = "ranger_hunters_mark", Name = "Hunters Mark", ClassName = "Ranger", SpellLevel = 1, Description = "Mark a foe as prey and deal extra damage to it for several turns.", ScalingStat = StatName.Wisdom, BaseDamage = 11, Variance = 5, ArmorBypass = 1, DamageTag = "piercing", SuppressCounterAttack = false },
        ["ranger_hail_of_thorns"] = new() { Id = "ranger_hail_of_thorns", Name = "Hail of Thorns", ClassName = "Ranger", SpellLevel = 1, Description = "A thorn burst erupts around your target and tears nearby foes.", ScalingStat = StatName.Wisdom, BaseDamage = 11, Variance = 5, ArmorBypass = 1, DamageTag = "piercing", SuppressCounterAttack = false },
        ["ranger_ensnaring_strike"] = new() { Id = "ranger_ensnaring_strike", Name = "Ensnaring Strike", ClassName = "Ranger", SpellLevel = 1, Description = "Empower a melee strike with binding vines that can root the foe.", ScalingStat = StatName.Wisdom, BaseDamage = 10, Variance = 4, ArmorBypass = 0, DamageTag = "nature", SuppressCounterAttack = false },
        ["ranger_cordon_of_arrows"] = new() { Id = "ranger_cordon_of_arrows", Name = "Cordon of Arrows", ClassName = "Ranger", SpellLevel = 1, Description = "Lay a spectral arrow ward that fires on enemies entering its patch.", ScalingStat = StatName.Wisdom, BaseDamage = 10, Variance = 4, ArmorBypass = 1, DamageTag = "piercing", SuppressCounterAttack = false },
        ["ranger_spike_growth"] = new() { Id = "ranger_spike_growth", Name = "Spike Growth", ClassName = "Ranger", SpellLevel = 2, Description = "Raise a hidden spike patch that tears enemies moving through it.", ScalingStat = StatName.Wisdom, BaseDamage = 14, Variance = 6, ArmorBypass = 1, DamageTag = "piercing", SuppressCounterAttack = false },
        ["ranger_zephyr_strike"] = new() { Id = "ranger_zephyr_strike", Name = "Zephyr Strike", ClassName = "Ranger", SpellLevel = 2, Description = "Wind surges around you. +1d8 force on your next hit, +10 flee chance while concentrating.", ScalingStat = StatName.Wisdom, BaseDamage = 13, Variance = 5, ArmorBypass = 2, DamageTag = "force", SuppressCounterAttack = true },
        ["ranger_pass_without_trace"] = new() { Id = "ranger_pass_without_trace", Name = "Pass Without Trace", ClassName = "Ranger", SpellLevel = 2, Description = "Shrouding shadows empower a lethal opening strike.", ScalingStat = StatName.Wisdom, BaseDamage = 12, Variance = 4, ArmorBypass = 1, DamageTag = "nature", SuppressCounterAttack = true },
        ["ranger_summon_beast"] = new() { Id = "ranger_summon_beast", Name = "Summon Beast", ClassName = "Ranger", SpellLevel = 2, Description = "Call a fey beast spirit to fight by your side. 1d8 + WIS piercing per hit. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 13, Variance = 5, ArmorBypass = 1, DamageTag = "piercing", SuppressCounterAttack = true },
        ["ranger_summon_plant"] = new() { Id = "ranger_summon_plant", Name = "Summon Plant", ClassName = "Ranger", SpellLevel = 2, Description = "Call a thorny plant creature that lashes your foes each turn. 1d6+2 + WIS piercing. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 12, Variance = 4, ArmorBypass = 1, DamageTag = "piercing", SuppressCounterAttack = true },
        ["ranger_lightning_arrow"] = new() { Id = "ranger_lightning_arrow", Name = "Lightning Arrow", ClassName = "Ranger", SpellLevel = 3, Description = "A charged shot detonates on impact and shocks nearby foes.", ScalingStat = StatName.Wisdom, BaseDamage = 18, Variance = 7, ArmorBypass = 2, DamageTag = "lightning", SuppressCounterAttack = false },
        ["ranger_conjure_barrage"] = new() { Id = "ranger_conjure_barrage", Name = "Conjure Barrage", ClassName = "Ranger", SpellLevel = 3, Description = "Loose a wide cone of conjured missiles across the field.", ScalingStat = StatName.Wisdom, BaseDamage = 17, Variance = 7, ArmorBypass = 2, DamageTag = "piercing", SuppressCounterAttack = false },
        ["ranger_flame_arrows"] = new() { Id = "ranger_flame_arrows", Name = "Flame Arrows", ClassName = "Ranger", SpellLevel = 3, Description = "Wreathe your arrows in fire. +1d8 fire per hit while concentrating.", ScalingStat = StatName.Wisdom, BaseDamage = 16, Variance = 6, ArmorBypass = 2, DamageTag = "fire", SuppressCounterAttack = true },
        ["ranger_conjure_animals"] = new() { Id = "ranger_conjure_animals", Name = "Conjure Animals", ClassName = "Ranger", SpellLevel = 3, Description = "Summon a pack of beasts that maul your enemies each turn. 2d6 + WIS piercing. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 17, Variance = 7, ArmorBypass = 2, DamageTag = "piercing", SuppressCounterAttack = true },
        ["ranger_summon_fey"] = new() { Id = "ranger_summon_fey", Name = "Summon Fey", ClassName = "Ranger", SpellLevel = 3, Description = "Call forth a fey spirit of the wild that strikes each turn. 2d6 + WIS force. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 16, Variance = 6, ArmorBypass = 2, DamageTag = "force", SuppressCounterAttack = true },
        // Batch 2 — Ranger tactical spells
        ["ranger_absorb_elements"] = new() { Id = "ranger_absorb_elements", Name = "Absorb Elements", ClassName = "Ranger", SpellLevel = 1, Description = "Absorb elemental energy and prime your next melee for +1d6 bonus damage.", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "nature", SuppressCounterAttack = true },
        ["ranger_longstrider"] = new() { Id = "ranger_longstrider", Name = "Longstrider", ClassName = "Ranger", SpellLevel = 1, Description = "Your stride lengthens. +2 move points per turn. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "nature", SuppressCounterAttack = true },
        // Batch 3 — Ranger reactive & retaliation spells
        ["ranger_thorns"] = new() { Id = "ranger_thorns", Name = "Thorns", ClassName = "Ranger", SpellLevel = 1, Description = "Thorny vines wrap around you. Attackers take 1d6 piercing. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "nature", SuppressCounterAttack = true },
        ["ranger_stoneskin"] = new() { Id = "ranger_stoneskin", Name = "Stoneskin", ClassName = "Ranger", SpellLevel = 2, Description = "Your skin hardens to stone. Incoming damage reduced by 3. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "nature", SuppressCounterAttack = true },
        // Batch 1 — Ranger buffs
        ["ranger_barkskin"] = new() { Id = "ranger_barkskin", Name = "Barkskin", ClassName = "Ranger", SpellLevel = 2, Description = "Your skin becomes bark-like. Your AC cannot be lower than 16. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "nature", SuppressCounterAttack = true },

        // === Pass 9K — Transformation spells ===
        ["mage_polymorph"] = new() { Id = "mage_polymorph", Name = "Polymorph", ClassName = "Mage", SpellLevel = 2, Description = "Transform into a beast form with new combat stats. Choose wolf, bear, eagle, spider, or warg. Concentration.", ScalingStat = StatName.Intelligence, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "arcane", SuppressCounterAttack = true },
        ["mage_shadow_form"] = new() { Id = "mage_shadow_form", Name = "Shadow Form", ClassName = "Mage", SpellLevel = 2, Description = "Become a spectral shade. Reduces incoming damage by 3, necrotic strikes. Concentration.", ScalingStat = StatName.Intelligence, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "necrotic", SuppressCounterAttack = true },
        ["mage_elemental_form"] = new() { Id = "mage_elemental_form", Name = "Elemental Form", ClassName = "Mage", SpellLevel = 3, Description = "Transform into a fire, water, earth, or air elemental. Concentration.", ScalingStat = StatName.Intelligence, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "arcane", SuppressCounterAttack = true },
        ["mage_monstrous_form"] = new() { Id = "mage_monstrous_form", Name = "Monstrous Form", ClassName = "Mage", SpellLevel = 3, Description = "Take the form of an ogre or troll. Massive damage and temp HP. Concentration.", ScalingStat = StatName.Intelligence, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "arcane", SuppressCounterAttack = true },
        ["ranger_wild_shape"] = new() { Id = "ranger_wild_shape", Name = "Wild Shape", ClassName = "Ranger", SpellLevel = 1, Description = "Shift into a wolf, cat, or snake. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "nature", SuppressCounterAttack = true },
        ["ranger_animal_form"] = new() { Id = "ranger_animal_form", Name = "Animal Form", ClassName = "Ranger", SpellLevel = 2, Description = "Shift into a bear, dire wolf, giant eagle, or warg. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "nature", SuppressCounterAttack = true },
        ["ranger_insect_form"] = new() { Id = "ranger_insect_form", Name = "Insect Form", ClassName = "Ranger", SpellLevel = 2, Description = "Take the form of a scorpion, mantis, or phase spider. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "nature", SuppressCounterAttack = true },
        ["ranger_plant_form"] = new() { Id = "ranger_plant_form", Name = "Plant Form", ClassName = "Ranger", SpellLevel = 3, Description = "Become a treant, giant flytrap, or shambling mound. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "nature", SuppressCounterAttack = true },
        ["ranger_elemental_form"] = new() { Id = "ranger_elemental_form", Name = "Elemental Form", ClassName = "Ranger", SpellLevel = 3, Description = "Transform into a fire, water, earth, or air elemental. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "nature", SuppressCounterAttack = true },
        ["ranger_primal_form"] = new() { Id = "ranger_primal_form", Name = "Primal Form", ClassName = "Ranger", SpellLevel = 3, Description = "Take the shape of a prehistoric beast — tyrannosaurus, triceratops, or raptor. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "nature", SuppressCounterAttack = true },
        ["cleric_divine_vessel"] = new() { Id = "cleric_divine_vessel", Name = "Divine Vessel", ClassName = "Cleric", SpellLevel = 2, Description = "Channel divine power into an angelic form. Radiant strikes, +2 defense. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "radiant", SuppressCounterAttack = true },
        ["cleric_stone_guardian"] = new() { Id = "cleric_stone_guardian", Name = "Stone Guardian", ClassName = "Cleric", SpellLevel = 3, Description = "Become a divine stone fortress. Highest AC and temp HP. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "radiant", SuppressCounterAttack = true },
        ["paladin_holy_transformation"] = new() { Id = "paladin_holy_transformation", Name = "Holy Transformation", ClassName = "Paladin", SpellLevel = 2, Description = "Transform into a celestial warrior. Radiant strikes, +2 defense. Concentration.", ScalingStat = StatName.Charisma, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "radiant", SuppressCounterAttack = true },
        ["paladin_avatar_of_wrath"] = new() { Id = "paladin_avatar_of_wrath", Name = "Avatar of Wrath", ClassName = "Paladin", SpellLevel = 3, Description = "Become an avenging angel. Devastating radiant strikes. Concentration.", ScalingStat = StatName.Charisma, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "radiant", SuppressCounterAttack = true },
        ["bard_polymorph"] = new() { Id = "bard_polymorph", Name = "Polymorph", ClassName = "Bard", SpellLevel = 2, Description = "Transform into a beast form. Choose wolf, bear, spider, or eagle. Concentration.", ScalingStat = StatName.Charisma, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "arcane", SuppressCounterAttack = true },
        ["bard_gaseous_form"] = new() { Id = "bard_gaseous_form", Name = "Gaseous Form", ClassName = "Bard", SpellLevel = 2, Description = "Become incorporeal mist. Cannot attack, but +30 flee and damage resist. Concentration.", ScalingStat = StatName.Charisma, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "arcane", SuppressCounterAttack = true },

        // === Batch 4 — Expanded Arsenal ===
        ["mage_sleep"] = new() { Id = "mage_sleep", Name = "Sleep", ClassName = "Mage", SpellLevel = 1, Description = "A wave of drowsiness sweeps over one foe. WIS save or Incapacitated 2 turns.", ScalingStat = StatName.Intelligence, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "arcane", SuppressCounterAttack = true },
        ["mage_counterspell"] = new() { Id = "mage_counterspell", Name = "Counterspell", ClassName = "Mage", SpellLevel = 3, Description = "Prime a magical ward — the next hit against you is halved (concentration).", ScalingStat = StatName.Intelligence, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "arcane", SuppressCounterAttack = true },
        ["mage_vampiric_touch"] = new() { Id = "mage_vampiric_touch", Name = "Vampiric Touch", ClassName = "Mage", SpellLevel = 3, Description = "Necrotic energy tears at one foe — you absorb 50% of the damage as healing.", ScalingStat = StatName.Intelligence, BaseDamage = 20, Variance = 8, ArmorBypass = 1, DamageTag = "necrotic", SuppressCounterAttack = false },
        ["cleric_mass_healing_word"] = new() { Id = "cleric_mass_healing_word", Name = "Mass Healing Word", ClassName = "Cleric", SpellLevel = 3, Description = "A burst of holy energy heals 2d8 + Wisdom and cleanses one condition.", ScalingStat = StatName.Wisdom, BaseDamage = 14, Variance = 7, ArmorBypass = 0, DamageTag = "healing", SuppressCounterAttack = true, IsHealSpell = true },
        ["cleric_daylight"] = new() { Id = "cleric_daylight", Name = "Daylight", ClassName = "Cleric", SpellLevel = 3, Description = "Blinding radiant light bursts in a wide area. DEX save or half damage; Blinded on fail.", ScalingStat = StatName.Wisdom, BaseDamage = 16, Variance = 7, ArmorBypass = 1, DamageTag = "radiant", SuppressCounterAttack = false },
        ["cleric_beacon_of_hope"] = new() { Id = "cleric_beacon_of_hope", Name = "Beacon of Hope", ClassName = "Cleric", SpellLevel = 3, Description = "Divine radiance fills you. Your next heal is doubled. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "radiant", SuppressCounterAttack = true },
        ["bard_faerie_fire"] = new() { Id = "bard_faerie_fire", Name = "Faerie Fire", ClassName = "Bard", SpellLevel = 1, Description = "Glittering light coats enemies in the area, marking them as targets. Concentration.", ScalingStat = StatName.Charisma, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "arcane", SuppressCounterAttack = true },
        ["bard_charm_person"] = new() { Id = "bard_charm_person", Name = "Charm Person", ClassName = "Bard", SpellLevel = 1, Description = "A humanoid enemy is briefly charmed. WIS save or Incapacitated 1 turn.", ScalingStat = StatName.Charisma, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "psychic", SuppressCounterAttack = true },
        ["bard_invisibility"] = new() { Id = "bard_invisibility", Name = "Invisibility", ClassName = "Bard", SpellLevel = 2, Description = "You vanish from sight. Enemies have -15% hit chance. Breaks on attack. Concentration.", ScalingStat = StatName.Charisma, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "arcane", SuppressCounterAttack = true },
        ["paladin_elemental_weapon"] = new() { Id = "paladin_elemental_weapon", Name = "Elemental Weapon", ClassName = "Paladin", SpellLevel = 3, Description = "Imbue your weapon with elemental power. Choose element. +2d4 elemental per hit. Concentration.", ScalingStat = StatName.Charisma, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "radiant", SuppressCounterAttack = true },
        ["paladin_revivify"] = new() { Id = "paladin_revivify", Name = "Revivify", ClassName = "Paladin", SpellLevel = 3, Description = "Call on divine power when near death. If HP <= 25% max, heal 2d6 + CHA. Once per combat.", ScalingStat = StatName.Charisma, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "healing", SuppressCounterAttack = true },
        ["paladin_daylight"] = new() { Id = "paladin_daylight", Name = "Daylight", ClassName = "Paladin", SpellLevel = 3, Description = "Blinding radiant light bursts in a wide area. DEX save or half damage; Blinded on fail.", ScalingStat = StatName.Charisma, BaseDamage = 16, Variance = 7, ArmorBypass = 1, DamageTag = "radiant", SuppressCounterAttack = false },
        ["ranger_fog_cloud"] = new() { Id = "ranger_fog_cloud", Name = "Fog Cloud", ClassName = "Ranger", SpellLevel = 1, Description = "A thick fog fills the area. Enemies within take -3 to attack rolls. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "nature", SuppressCounterAttack = true },
        ["ranger_cure_wounds"] = new() { Id = "ranger_cure_wounds", Name = "Cure Wounds", ClassName = "Ranger", SpellLevel = 1, Description = "Natural healing energy mends your wounds. Heals 1d8 + Wisdom.", ScalingStat = StatName.Wisdom, BaseDamage = 8, Variance = 7, ArmorBypass = 0, DamageTag = "healing", SuppressCounterAttack = true, IsHealSpell = true },

        // === Batch 5 — Signature Powers ===
        ["mage_blink"] = new() { Id = "mage_blink", Name = "Blink", ClassName = "Mage", SpellLevel = 3, Description = "You phase in and out of the Ethereal Plane. 30% chance each hit misses you. Concentration.", ScalingStat = StatName.Intelligence, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "arcane", SuppressCounterAttack = true },
        ["mage_protection_from_energy"] = new() { Id = "mage_protection_from_energy", Name = "Protection from Energy", ClassName = "Mage", SpellLevel = 3, Description = "Choose an element. You resist half damage from that type. Concentration.", ScalingStat = StatName.Intelligence, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "arcane", SuppressCounterAttack = true },
        ["bard_major_image"] = new() { Id = "bard_major_image", Name = "Major Image", ClassName = "Bard", SpellLevel = 3, Description = "A vivid illusion distracts your foes. 25% chance attacks target the decoy. Concentration.", ScalingStat = StatName.Charisma, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "arcane", SuppressCounterAttack = true },
        ["paladin_aura_of_courage"] = new() { Id = "paladin_aura_of_courage", Name = "Aura of Courage", ClassName = "Paladin", SpellLevel = 2, Description = "Divine courage surrounds you. Feared cannot be applied; conditions reduced by 1 turn. Concentration.", ScalingStat = StatName.Charisma, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "radiant", SuppressCounterAttack = true },
        ["ranger_plant_growth"] = new() { Id = "ranger_plant_growth", Name = "Plant Growth", ClassName = "Ranger", SpellLevel = 3, Description = "Thick vegetation bursts up in a wide zone. Enemies inside are Slowed each turn. Concentration.", ScalingStat = StatName.Wisdom, BaseDamage = 0, Variance = 0, ArmorBypass = 0, DamageTag = "nature", SuppressCounterAttack = true }
    };

    public static readonly Dictionary<string, SummonType> SummonTypes = new()
    {
        ["cleric_spiritual_weapon"] = new()
        {
            Id = "cleric_spiritual_weapon",
            Name = "Spiritual Weapon",
            SourceSpellId = "cleric_spiritual_weapon",
            MaxHp = 0,
            DamageDice = 8,
            DamageBonus = 0,
            AttackBonus = 0,
            UseCasterStatMod = true,
            DamageType = "force",
            Behavior = SummonBehaviorKind.AutoAttack,
            RequiresConcentration = true,
            Description = "A spectral weapon strikes your foes each turn."
        },
        ["mage_find_familiar"] = new()
        {
            Id = "mage_find_familiar",
            Name = "Familiar",
            SourceSpellId = "mage_find_familiar",
            MaxHp = 8,
            DamageDice = 4,
            DamageBonus = 0,
            AttackBonus = 0,
            UseCasterStatMod = true,
            DamageType = "force",
            Behavior = SummonBehaviorKind.AutoAttack,
            RequiresConcentration = true,
            Description = "A tiny familiar darts at your enemies each turn."
        },
        ["mage_flaming_sphere"] = new()
        {
            Id = "mage_flaming_sphere",
            Name = "Flaming Sphere",
            SourceSpellId = "mage_flaming_sphere",
            MaxHp = 0,
            DamageCount = 2,
            DamageDice = 6,
            DamageBonus = 0,
            AttackBonus = 0,
            UseCasterStatMod = false,
            DamageType = "fire",
            Behavior = SummonBehaviorKind.AutoAttack,
            RequiresConcentration = true,
            Description = "A roiling ball of fire rams into your enemies each turn."
        },
        ["paladin_find_steed"] = new()
        {
            Id = "paladin_find_steed",
            Name = "Celestial Steed",
            SourceSpellId = "paladin_find_steed",
            MaxHp = 25,
            DamageDice = 0,
            DamageBonus = 0,
            AttackBonus = 0,
            UseCasterStatMod = false,
            DamageType = "radiant",
            Behavior = SummonBehaviorKind.BuffMount,
            RequiresConcentration = true,
            Description = "A celestial warhorse grants +2 defense and +15 flee chance."
        },
        ["ranger_summon_beast"] = new()
        {
            Id = "ranger_summon_beast",
            Name = "Beast Spirit",
            SourceSpellId = "ranger_summon_beast",
            MaxHp = 20,
            DamageDice = 8,
            DamageBonus = 1,
            AttackBonus = 0,
            UseCasterStatMod = true,
            DamageType = "piercing",
            Behavior = SummonBehaviorKind.AutoAttack,
            RequiresConcentration = true,
            Description = "A fey beast spirit fights by your side each turn."
        },
        ["mage_summon_fey"] = new()
        {
            Id = "mage_summon_fey",
            Name = "Fey Spirit",
            SourceSpellId = "mage_summon_fey",
            MaxHp = 35,
            DamageCount = 2,
            DamageDice = 6,
            DamageBonus = 0,
            AttackBonus = 0,
            UseCasterStatMod = true,
            DamageType = "force",
            Behavior = SummonBehaviorKind.AutoAttack,
            RequiresConcentration = true,
            Description = "A fey spirit teleports and strikes your foes each turn."
        },
        ["mage_summon_undead"] = new()
        {
            Id = "mage_summon_undead",
            Name = "Undead Spirit",
            SourceSpellId = "mage_summon_undead",
            MaxHp = 30,
            DamageDice = 8,
            DamageBonus = 2,
            AttackBonus = 0,
            UseCasterStatMod = true,
            DamageType = "necrotic",
            Behavior = SummonBehaviorKind.AutoAttack,
            RequiresConcentration = true,
            Description = "A spectral undead claws at your foes each turn."
        },
        ["cleric_animate_dead"] = new()
        {
            Id = "cleric_animate_dead",
            Name = "Skeleton Warrior",
            SourceSpellId = "cleric_animate_dead",
            MaxHp = 22,
            DamageDice = 6,
            DamageBonus = 1,
            AttackBonus = 0,
            UseCasterStatMod = true,
            DamageType = "necrotic",
            Behavior = SummonBehaviorKind.AutoAttack,
            RequiresConcentration = true,
            Description = "A raised skeleton fights by your side each turn."
        },
        ["ranger_summon_fey"] = new()
        {
            Id = "ranger_summon_fey",
            Name = "Fey Spirit",
            SourceSpellId = "ranger_summon_fey",
            MaxHp = 35,
            DamageCount = 2,
            DamageDice = 6,
            DamageBonus = 0,
            AttackBonus = 0,
            UseCasterStatMod = true,
            DamageType = "force",
            Behavior = SummonBehaviorKind.AutoAttack,
            RequiresConcentration = true,
            Description = "A fey spirit of the wild strikes your foes each turn."
        },
        ["ranger_conjure_animals"] = new()
        {
            Id = "ranger_conjure_animals",
            Name = "Conjured Pack",
            SourceSpellId = "ranger_conjure_animals",
            MaxHp = 30,
            DamageCount = 2,
            DamageDice = 6,
            DamageBonus = 0,
            AttackBonus = 1,
            UseCasterStatMod = true,
            DamageType = "piercing",
            Behavior = SummonBehaviorKind.AutoAttack,
            RequiresConcentration = true,
            Description = "A pack of conjured beasts mauls your enemies each turn."
        },
        ["mage_summon_elemental"] = new()
        {
            Id = "mage_summon_elemental",
            Name = "Fire Elemental",
            SourceSpellId = "mage_summon_elemental",
            MaxHp = 0,
            DamageDice = 10,
            DamageBonus = 0,
            AttackBonus = 0,
            UseCasterStatMod = false,
            DamageType = "fire",
            Behavior = SummonBehaviorKind.AutoAttack,
            RequiresConcentration = true,
            Description = "An elemental spirit of fire and stone burns your foes each turn."
        },
        ["mage_summon_shadowspawn"] = new()
        {
            Id = "mage_summon_shadowspawn",
            Name = "Shadowspawn",
            SourceSpellId = "mage_summon_shadowspawn",
            MaxHp = 30,
            DamageDice = 12,
            DamageBonus = 0,
            AttackBonus = 0,
            UseCasterStatMod = true,
            DamageType = "psychic",
            Behavior = SummonBehaviorKind.AutoAttack,
            RequiresConcentration = true,
            Description = "A dread shadow entity terrorizes and claws at your foes each turn."
        },
        ["mage_phantom_steed"] = new()
        {
            Id = "mage_phantom_steed",
            Name = "Phantom Steed",
            SourceSpellId = "mage_phantom_steed",
            MaxHp = 0,
            DamageDice = 0,
            DamageBonus = 0,
            AttackBonus = 0,
            UseCasterStatMod = false,
            DamageType = "force",
            Behavior = SummonBehaviorKind.BuffMount,
            RequiresConcentration = true,
            Description = "A spectral horse grants +2 defense and +15 flee chance."
        },
        ["cleric_summon_celestial"] = new()
        {
            Id = "cleric_summon_celestial",
            Name = "Celestial Guardian",
            SourceSpellId = "cleric_summon_celestial",
            MaxHp = 35,
            DamageDice = 10,
            DamageBonus = 0,
            AttackBonus = 0,
            UseCasterStatMod = true,
            DamageType = "radiant",
            Behavior = SummonBehaviorKind.AutoAttack,
            RequiresConcentration = true,
            Description = "A radiant guardian angel smites your foes each turn."
        },
        ["bard_summon_fey"] = new()
        {
            Id = "bard_summon_fey",
            Name = "Fey Trickster",
            SourceSpellId = "bard_summon_fey",
            MaxHp = 25,
            DamageDice = 8,
            DamageBonus = 1,
            AttackBonus = 0,
            UseCasterStatMod = true,
            DamageType = "psychic",
            Behavior = SummonBehaviorKind.AutoAttack,
            RequiresConcentration = true,
            Description = "A fey trickster bewilders and strikes your foes each turn."
        },
        ["paladin_summon_celestial"] = new()
        {
            Id = "paladin_summon_celestial",
            Name = "Celestial Avenger",
            SourceSpellId = "paladin_summon_celestial",
            MaxHp = 30,
            DamageDice = 10,
            DamageBonus = 1,
            AttackBonus = 0,
            UseCasterStatMod = true,
            DamageType = "radiant",
            Behavior = SummonBehaviorKind.AutoAttack,
            RequiresConcentration = true,
            Description = "A celestial avenger smites your foes each turn."
        },
        ["ranger_summon_plant"] = new()
        {
            Id = "ranger_summon_plant",
            Name = "Thorn Guardian",
            SourceSpellId = "ranger_summon_plant",
            MaxHp = 18,
            DamageDice = 6,
            DamageBonus = 2,
            AttackBonus = 0,
            UseCasterStatMod = true,
            DamageType = "piercing",
            Behavior = SummonBehaviorKind.AutoAttack,
            RequiresConcentration = true,
            Description = "A thorny plant creature lashes at your foes each turn."
        }
    };

    // === Pass 9K — Form definitions for transformation spells ===
    public static readonly Dictionary<string, FormDefinition> Forms = new()
    {
        // Tier 1 (L1 forms)
        ["form_wolf"] = new() { Id = "form_wolf", Name = "Wolf", FormAC = 14, AttackBonus = 6, DamageCount = 1, DamageDice = 8, DamageBonus = 3, DamageType = "piercing", TempHp = 8, Special = FormSpecialKind.PackTactics, SpecialValue = 2, Description = "Pack hunter. +2 attack vs damaged foes." },
        ["form_cat"] = new() { Id = "form_cat", Name = "Cat", FormAC = 15, AttackBonus = 7, DamageCount = 1, DamageDice = 6, DamageBonus = 3, DamageType = "slashing", TempHp = 6, Special = FormSpecialKind.Evasion, SpecialValue = 15, Description = "Nimble predator. +15 flee chance." },
        ["form_snake"] = new() { Id = "form_snake", Name = "Snake", FormAC = 13, AttackBonus = 6, DamageCount = 1, DamageDice = 6, DamageBonus = 2, DamageType = "piercing", TempHp = 7, Special = FormSpecialKind.PoisonOnHit, SpecialValue = 4, Description = "Venomous fangs. Poisons foe on hit." },

        // Tier 2 (L2 forms)
        ["form_bear"] = new() { Id = "form_bear", Name = "Bear", FormAC = 13, AttackBonus = 8, DamageCount = 2, DamageDice = 6, DamageBonus = 4, DamageType = "slashing", TempHp = 15, Special = FormSpecialKind.BonusCritDamage, SpecialValue = 6, Description = "Powerful mauler. +1d6 on critical hits." },
        ["form_dire_wolf"] = new() { Id = "form_dire_wolf", Name = "Dire Wolf", FormAC = 14, AttackBonus = 8, DamageCount = 2, DamageDice = 6, DamageBonus = 3, DamageType = "piercing", TempHp = 12, Special = FormSpecialKind.PackTactics, SpecialValue = 2, Description = "Alpha predator. +2 attack vs damaged foes." },
        ["form_giant_eagle"] = new() { Id = "form_giant_eagle", Name = "Giant Eagle", FormAC = 15, AttackBonus = 7, DamageCount = 1, DamageDice = 8, DamageBonus = 4, DamageType = "slashing", TempHp = 10, Special = FormSpecialKind.NoCounterAttack, Description = "Aerial striker. Attacks don't provoke counter-attacks." },
        ["form_giant_spider"] = new() { Id = "form_giant_spider", Name = "Giant Spider", FormAC = 14, AttackBonus = 7, DamageCount = 1, DamageDice = 8, DamageBonus = 3, DamageType = "piercing", TempHp = 10, Special = FormSpecialKind.DebuffOnHit, SpecialValue = 2, Description = "Web spinner. -2 enemy attack on hit for 2 turns." },
        ["form_warg"] = new() { Id = "form_warg", Name = "Warg", FormAC = 14, AttackBonus = 7, DamageCount = 2, DamageDice = 6, DamageBonus = 2, DamageType = "piercing", TempHp = 12, Special = FormSpecialKind.PackTactics, SpecialValue = 2, Description = "Dungeon beast. +2 attack vs damaged foes." },
        ["form_scorpion"] = new() { Id = "form_scorpion", Name = "Giant Scorpion", FormAC = 16, AttackBonus = 7, DamageCount = 1, DamageDice = 10, DamageBonus = 3, DamageType = "piercing", TempHp = 12, Special = FormSpecialKind.PoisonOnHit, SpecialValue = 6, Description = "Armored stinger. Poisons foe on hit." },
        ["form_mantis"] = new() { Id = "form_mantis", Name = "Giant Mantis", FormAC = 14, AttackBonus = 8, DamageCount = 2, DamageDice = 6, DamageBonus = 3, DamageType = "slashing", TempHp = 10, Special = FormSpecialKind.FirstHitBonus, SpecialValue = 8, Description = "Ambush predator. +1d8 on first attack." },
        ["form_insect_spider"] = new() { Id = "form_insect_spider", Name = "Phase Spider", FormAC = 15, AttackBonus = 7, DamageCount = 1, DamageDice = 8, DamageBonus = 3, DamageType = "piercing", TempHp = 10, Special = FormSpecialKind.PoisonOnHit, SpecialValue = 6, Description = "Phasing arachnid. Venomous bite poisons foe." },
        ["form_shadow"] = new() { Id = "form_shadow", Name = "Shadow", FormAC = 16, AttackBonus = 7, DamageCount = 1, DamageDice = 8, DamageBonus = 4, DamageType = "necrotic", TempHp = 12, Special = FormSpecialKind.DamageResist, SpecialValue = 3, Description = "Spectral shade. Reduces incoming damage by 3." },
        ["form_angelic"] = new() { Id = "form_angelic", Name = "Angelic Form", FormAC = 15, AttackBonus = 8, DamageCount = 1, DamageDice = 10, DamageBonus = 0, DamageType = "radiant", TempHp = 12, UseCasterStatMod = true, Special = FormSpecialKind.DefenseBonus, SpecialValue = 2, Description = "Divine radiance. +2 defense, radiant strikes." },
        ["form_celestial_warrior"] = new() { Id = "form_celestial_warrior", Name = "Celestial Warrior", FormAC = 16, AttackBonus = 7, DamageCount = 1, DamageDice = 10, DamageBonus = 0, DamageType = "radiant", TempHp = 15, UseCasterStatMod = true, Special = FormSpecialKind.DefenseBonus, SpecialValue = 2, Description = "Holy champion. +2 defense, radiant strikes." },
        ["form_mist"] = new() { Id = "form_mist", Name = "Mist", FormAC = 20, AttackBonus = 0, DamageCount = 0, DamageDice = 0, DamageBonus = 0, DamageType = "none", TempHp = 8, CanAttack = false, Special = FormSpecialKind.FleeBonus, SpecialValue = 30, Description = "Incorporeal mist. Cannot attack. +30 flee, incoming damage -3." },

        // Tier 3 (L3 forms)
        ["form_fire_elemental"] = new() { Id = "form_fire_elemental", Name = "Fire Elemental", FormAC = 15, AttackBonus = 9, DamageCount = 2, DamageDice = 8, DamageBonus = 4, DamageType = "fire", TempHp = 15, Special = FormSpecialKind.BurnOnHit, SpecialValue = 6, Description = "Living flame. Burns foe for 1d6 fire/turn on hit." },
        ["form_water_elemental"] = new() { Id = "form_water_elemental", Name = "Water Elemental", FormAC = 16, AttackBonus = 8, DamageCount = 2, DamageDice = 8, DamageBonus = 3, DamageType = "bludgeoning", TempHp = 18, Special = FormSpecialKind.DefenseBonus, SpecialValue = 3, Description = "Tidal force. +3 defense." },
        ["form_earth_elemental"] = new() { Id = "form_earth_elemental", Name = "Earth Elemental", FormAC = 18, AttackBonus = 7, DamageCount = 2, DamageDice = 10, DamageBonus = 3, DamageType = "bludgeoning", TempHp = 20, Special = FormSpecialKind.DamageResist, SpecialValue = 3, Description = "Living stone. Reduces incoming damage by 3." },
        ["form_air_elemental"] = new() { Id = "form_air_elemental", Name = "Air Elemental", FormAC = 14, AttackBonus = 10, DamageCount = 2, DamageDice = 6, DamageBonus = 5, DamageType = "bludgeoning", TempHp = 12, Special = FormSpecialKind.NoCounterAttack, Description = "Whirlwind strikes. Attacks don't provoke counters." },
        ["form_ogre"] = new() { Id = "form_ogre", Name = "Ogre", FormAC = 12, AttackBonus = 8, DamageCount = 2, DamageDice = 8, DamageBonus = 4, DamageType = "bludgeoning", TempHp = 20, Description = "Brute force. Massive damage, low AC." },
        ["form_troll"] = new() { Id = "form_troll", Name = "Troll", FormAC = 15, AttackBonus = 9, DamageCount = 2, DamageDice = 6, DamageBonus = 3, DamageType = "slashing", TempHp = 18, Special = FormSpecialKind.Regeneration, SpecialValue = 4, Description = "Regenerating horror. Regains 1d4 temp HP each turn." },
        ["form_treant"] = new() { Id = "form_treant", Name = "Treant", FormAC = 17, AttackBonus = 8, DamageCount = 2, DamageDice = 10, DamageBonus = 4, DamageType = "bludgeoning", TempHp = 22, Special = FormSpecialKind.DefenseBonus, SpecialValue = 2, Description = "Ancient guardian. +2 defense, massive reach." },
        ["form_flytrap"] = new() { Id = "form_flytrap", Name = "Giant Flytrap", FormAC = 14, AttackBonus = 9, DamageCount = 2, DamageDice = 8, DamageBonus = 3, DamageType = "acid", TempHp = 16, Special = FormSpecialKind.ArmorBypass, SpecialValue = 3, Description = "Dissolving maw. Ignores 3 AC on attacks." },
        ["form_shambler"] = new() { Id = "form_shambler", Name = "Shambling Mound", FormAC = 15, AttackBonus = 8, DamageCount = 2, DamageDice = 8, DamageBonus = 3, DamageType = "bludgeoning", TempHp = 18, Special = FormSpecialKind.DamageResist, SpecialValue = 3, Description = "Overgrown mass. Reduces incoming damage by 3." },
        ["form_stone_guardian"] = new() { Id = "form_stone_guardian", Name = "Stone Guardian", FormAC = 19, AttackBonus = 6, DamageCount = 2, DamageDice = 10, DamageBonus = 0, DamageType = "bludgeoning", TempHp = 25, UseCasterStatMod = true, Special = FormSpecialKind.DefenseBonus, SpecialValue = 3, Description = "Divine fortress. Highest AC/temp HP, +3 defense." },
        ["form_avenging_angel"] = new() { Id = "form_avenging_angel", Name = "Avenging Angel", FormAC = 16, AttackBonus = 10, DamageCount = 2, DamageDice = 10, DamageBonus = 0, DamageType = "radiant", TempHp = 18, UseCasterStatMod = true, Special = FormSpecialKind.FirstHitBonus, SpecialValue = 8, Description = "Heaven's wrath. +1d8 radiant on first strike." },
        ["form_trex"] = new() { Id = "form_trex", Name = "Tyrannosaurus", FormAC = 14, AttackBonus = 10, DamageCount = 3, DamageDice = 8, DamageBonus = 5, DamageType = "piercing", TempHp = 20, Special = FormSpecialKind.HealOnKill, SpecialValue = 6, Description = "Apex predator. Heal 1d6 HP on kill." },
        ["form_triceratops"] = new() { Id = "form_triceratops", Name = "Triceratops", FormAC = 16, AttackBonus = 9, DamageCount = 2, DamageDice = 10, DamageBonus = 4, DamageType = "bludgeoning", TempHp = 22, Special = FormSpecialKind.DebuffOnHit, SpecialValue = 2, Description = "Charging horn. -2 enemy attack on hit." },
        ["form_raptor"] = new() { Id = "form_raptor", Name = "Raptor", FormAC = 15, AttackBonus = 10, DamageCount = 2, DamageDice = 6, DamageBonus = 5, DamageType = "slashing", TempHp = 14, Special = FormSpecialKind.FirstHitBonus, SpecialValue = 8, Description = "Swift pounce. +1d8 on first attack." },
    };

    public static readonly Dictionary<string, string[]> TransformationForms = new()
    {
        // Mage
        ["mage_polymorph"] = new[] { "form_wolf", "form_bear", "form_giant_eagle", "form_giant_spider", "form_warg" },
        ["mage_shadow_form"] = new[] { "form_shadow" },
        ["mage_elemental_form"] = new[] { "form_fire_elemental", "form_water_elemental", "form_earth_elemental", "form_air_elemental" },
        ["mage_monstrous_form"] = new[] { "form_ogre", "form_troll" },
        // Ranger
        ["ranger_wild_shape"] = new[] { "form_wolf", "form_cat", "form_snake" },
        ["ranger_animal_form"] = new[] { "form_bear", "form_dire_wolf", "form_giant_eagle", "form_warg" },
        ["ranger_insect_form"] = new[] { "form_scorpion", "form_mantis", "form_insect_spider" },
        ["ranger_plant_form"] = new[] { "form_treant", "form_flytrap", "form_shambler" },
        ["ranger_elemental_form"] = new[] { "form_fire_elemental", "form_water_elemental", "form_earth_elemental", "form_air_elemental" },
        ["ranger_primal_form"] = new[] { "form_trex", "form_triceratops", "form_raptor" },
        // Cleric
        ["cleric_divine_vessel"] = new[] { "form_angelic" },
        ["cleric_stone_guardian"] = new[] { "form_stone_guardian" },
        // Paladin
        ["paladin_holy_transformation"] = new[] { "form_celestial_warrior" },
        ["paladin_avatar_of_wrath"] = new[] { "form_avenging_angel" },
        // Bard
        ["bard_polymorph"] = new[] { "form_wolf", "form_bear", "form_giant_spider", "form_giant_eagle" },
        ["bard_gaseous_form"] = new[] { "form_mist" },
    };

    public static bool IsPlayerVisible(SpellDefinition spell)
    {
        return spell.Origin == SpellOrigin.Authored && ResolveEffectRoute(spell).SupportState == SpellSupportState.Active;
    }

    public static bool IsPlayerVisible(string spellId)
    {
        return ById.TryGetValue(spellId, out var spell) && IsPlayerVisible(spell);
    }

    public static readonly Dictionary<string, List<(int MinLevel, string SpellId)>> ClassSpellUnlocks = new()
    {
        ["Mage"] = new()
        {
            (1, "mage_fire_bolt"),
            (1, "mage_ray_of_frost"),
            (1, "mage_chill_touch"),
            (1, "mage_shocking_grasp"),
            (1, "mage_acid_splash"),
            (1, "mage_magic_missile"),
            (1, "mage_burning_hands"),
            (1, "mage_chromatic_orb"),
            (1, "mage_ice_knife"),
            (3, "mage_scorching_ray"),
            (3, "mage_shatter"),
            (3, "mage_web"),
            (3, "mage_melfs_acid_arrow"),
            (1, "mage_mage_armor"),
            (1, "mage_shield"),
            (1, "mage_false_life"),
            (3, "mage_blur"),
            (3, "mage_misty_step"),
            (3, "mage_mirror_image"),
            (1, "mage_expeditious_retreat"),
            (3, "mage_enhance_ability"),
            (5, "mage_fireball"),
            (5, "mage_lightning_bolt"),
            (5, "mage_tidal_wave"),
            (5, "mage_haste"),
            // Batch 3
            (1, "mage_hellish_rebuke"),
            (1, "mage_armor_of_agathys"),
            (3, "mage_fire_shield"),
            (3, "mage_stoneskin"),
            // Batch 4+5
            (1, "mage_sleep"),
            (5, "mage_counterspell"),
            (5, "mage_vampiric_touch"),
            (5, "mage_blink"),
            (5, "mage_protection_from_energy")
        },
        ["Cleric"] = new()
        {
            (1, "cleric_sacred_flame"),
            (1, "cleric_toll_the_dead"),
            (1, "cleric_word_of_radiance"),
            (1, "cleric_cure_wounds"),
            (1, "cleric_healing_word"),
            (1, "cleric_guiding_bolt"),
            (1, "cleric_inflict_wounds"),
            (1, "cleric_command"),
            (1, "cleric_bane"),
            (3, "cleric_spiritual_weapon"),
            (3, "cleric_hold_person"),
            (3, "cleric_blindness"),
            (3, "cleric_prayer_of_healing"),
            (3, "cleric_lesser_restoration"),
            (1, "cleric_shield_of_faith"),
            (1, "cleric_bless"),
            (3, "cleric_aid"),
            (1, "cleric_protection_evg"),
            (1, "cleric_sanctuary"),
            (3, "cleric_enhance_ability"),
            (5, "cleric_spirit_guardians"),
            (5, "cleric_bestow_curse"),
            (5, "cleric_flame_strike"),
            // Batch 3
            (1, "cleric_wrath_of_storm"),
            (5, "cleric_spirit_shroud"),
            (3, "cleric_death_ward"),
            // Batch 4+5
            (5, "cleric_mass_healing_word"),
            (5, "cleric_daylight"),
            (5, "cleric_beacon_of_hope"),
            (5, "cleric_animate_dead")
        },
        ["Bard"] = new()
        {
            (1, "bard_vicious_mockery"),
            (1, "bard_thunderclap"),
            (1, "bard_mind_sliver"),
            (1, "bard_healing_word"),
            (1, "bard_dissonant_whispers"),
            (1, "bard_thunderwave"),
            (1, "bard_hideous_laughter"),
            (3, "bard_shatter"),
            (3, "bard_heat_metal"),
            (3, "bard_cloud_of_daggers"),
            (1, "bard_heroism"),
            (1, "bard_hex"),
            (3, "bard_enhance_ability"),
            (5, "bard_hypnotic_pattern"),
            (5, "bard_fear"),
            (5, "bard_slow"),
            // Batch 3
            (1, "bard_cutting_words"),
            (3, "bard_greater_invisibility"),
            // Batch 4+5
            (1, "bard_faerie_fire"),
            (1, "bard_charm_person"),
            (3, "bard_invisibility"),
            (5, "bard_major_image")
        },
        ["Paladin"] = new()
        {
            (2, "paladin_cure_wounds"),
            (2, "paladin_searing_smite"),
            (2, "paladin_thunderous_smite"),
            (2, "paladin_wrathful_smite"),
            (2, "paladin_divine_favor"),
            (2, "paladin_shield_of_faith"),
            (2, "paladin_heroism"),
            (2, "paladin_protection_evg"),
            (2, "paladin_compelled_duel"),
            (5, "paladin_aid"),
            (5, "paladin_branding_smite"),
            (5, "paladin_magic_weapon"),
            (6, "paladin_aura_of_vitality"),
            (6, "paladin_blinding_smite"),
            (6, "paladin_crusaders_mantle"),
            // Batch 3
            (3, "paladin_death_ward"),
            (2, "paladin_holy_rebuke"),
            // Batch 4+5
            (5, "paladin_elemental_weapon"),
            (5, "paladin_revivify"),
            (5, "paladin_daylight"),
            (3, "paladin_aura_of_courage")
        },
        ["Ranger"] = new()
        {
            (2, "ranger_hunters_mark"),
            (2, "ranger_hail_of_thorns"),
            (2, "ranger_ensnaring_strike"),
            (2, "ranger_cordon_of_arrows"),
            (2, "ranger_absorb_elements"),
            (2, "ranger_longstrider"),
            (5, "ranger_spike_growth"),
            (5, "ranger_barkskin"),
            (5, "ranger_zephyr_strike"),
            (5, "ranger_pass_without_trace"),
            (6, "ranger_lightning_arrow"),
            (6, "ranger_conjure_barrage"),
            (6, "ranger_flame_arrows"),
            // Batch 3
            (2, "ranger_thorns"),
            (3, "ranger_stoneskin"),
            // Batch 4+5
            (2, "ranger_fog_cloud"),
            (2, "ranger_cure_wounds"),
            (5, "ranger_plant_growth")
        }
    };

    public static readonly Dictionary<string, Dictionary<int, (int L1, int L2, int L3)>> SpellSlotsByClass = BuildSpellSlotsByClass();

    private static Dictionary<string, Dictionary<int, (int L1, int L2, int L3)>> BuildSpellSlotsByClass()
    {
        var byClass = new Dictionary<string, Dictionary<int, (int L1, int L2, int L3)>>()
        {
            ["Mage"] = new()
            {
                [1] = (2, 0, 0),
                [2] = (3, 0, 0),
                [3] = (4, 2, 0),
                [4] = (4, 3, 0),
                [5] = (4, 3, 2),
                [6] = (4, 3, 3)
            },
            ["Cleric"] = new()
            {
                [1] = (2, 0, 0),
                [2] = (3, 0, 0),
                [3] = (4, 2, 0),
                [4] = (4, 3, 0),
                [5] = (4, 3, 2),
                [6] = (4, 3, 3)
            },
            ["Bard"] = new()
            {
                [1] = (2, 0, 0),
                [2] = (3, 0, 0),
                [3] = (4, 2, 0),
                [4] = (4, 3, 0),
                [5] = (4, 3, 2),
                [6] = (4, 3, 3)
            }
        };

        if (UseAcceleratedHalfCasterProgression)
        {
            byClass["Paladin"] = new()
            {
                [1] = (0, 0, 0),
                [2] = (2, 0, 0),
                [3] = (3, 0, 0),
                [4] = (3, 2, 0),
                [5] = (4, 2, 0),
                [6] = (4, 3, 2)
            };
            byClass["Ranger"] = new()
            {
                [1] = (0, 0, 0),
                [2] = (2, 0, 0),
                [3] = (3, 0, 0),
                [4] = (3, 2, 0),
                [5] = (4, 2, 0),
                [6] = (4, 3, 2)
            };
        }
        else
        {
            byClass["Paladin"] = new()
            {
                [1] = (0, 0, 0),
                [2] = (2, 0, 0),
                [3] = (3, 0, 0),
                [4] = (3, 0, 0),
                [5] = (4, 2, 0),
                [6] = (4, 2, 0)
            };
            byClass["Ranger"] = new()
            {
                [1] = (0, 0, 0),
                [2] = (2, 0, 0),
                [3] = (3, 0, 0),
                [4] = (3, 0, 0),
                [5] = (4, 2, 0),
                [6] = (4, 2, 0)
            };
        }

        return byClass;
    }

    static SpellData()
    {
        ExpandPrototypeSpellCatalog();
    }

    private static void ExpandPrototypeSpellCatalog()
    {
        // Mage / Wizard-like list (prototype combat translation).
        AddPrototypeClassSpellRange("Mage", StatName.Intelligence, "arcane", 0,
            "Blade Ward", "Dancing Lights", "Friends", "Light", "Mage Hand", "Mending",
            "Message", "Minor Illusion", "Poison Spray", "Prestidigitation", "True Strike");
        AddPrototypeClassSpellRange("Mage", StatName.Intelligence, "arcane", 1,
            "Alarm", "Charm Person", "Color Spray", "Comprehend Languages", "Detect Magic",
            "Disguise Self", "Expeditious Retreat", "False Life", "Feather Fall", "Find Familiar",
            "Fog Cloud", "Grease", "Identify", "Illusory Script", "Jump", "Longstrider",
            "Mage Armor", "Protection from Evil and Good", "Shield", "Silent Image", "Sleep",
            "Thunderwave", "Unseen Servant");
        AddPrototypeClassSpellRange("Mage", StatName.Intelligence, "arcane", 2,
            "Alter Self", "Arcane Lock", "Blindness Deafness", "Blur", "Cloud of Daggers",
            "Continual Flame", "Darkness", "Darkvision", "Detect Thoughts", "Enhance Ability",
            "Enlarge Reduce", "Flaming Sphere", "Gentle Repose", "Gust of Wind", "Hold Person",
            "Invisibility", "Knock", "Levitate", "Locate Object", "Magic Mouth", "Magic Weapon",
            "Mirror Image", "Misty Step", "Nystuls Magic Aura", "Ray of Enfeeblement", "Rope Trick",
            "See Invisibility", "Spider Climb", "Suggestion");
        AddPrototypeClassSpellRange("Mage", StatName.Intelligence, "arcane", 3,
            "Animate Dead", "Bestow Curse", "Blink", "Clairvoyance", "Counterspell", "Daylight",
            "Dispel Magic", "Fear", "Feign Death", "Fly", "Gaseous Form", "Glyph of Warding",
            "Haste", "Hypnotic Pattern", "Leomunds Tiny Hut", "Magic Circle", "Major Image",
            "Nondetection", "Phantom Steed", "Protection from Energy", "Remove Curse", "Sending",
            "Sleet Storm", "Stinking Cloud", "Tongues", "Vampiric Touch", "Water Breathing");

        // Cleric list.
        AddPrototypeClassSpellRange("Cleric", StatName.Wisdom, "radiant", 0,
            "Guidance", "Light", "Mending", "Resistance", "Spare the Dying", "Thaumaturgy");
        AddPrototypeClassSpellRange("Cleric", StatName.Wisdom, "radiant", 1,
            "Bless", "Create or Destroy Water", "Cure Wounds", "Detect Evil and Good", "Detect Magic",
            "Detect Poison and Disease", "Healing Word", "Protection from Evil and Good",
            "Purify Food and Drink", "Sanctuary", "Shield of Faith");
        AddPrototypeClassSpellRange("Cleric", StatName.Wisdom, "radiant", 2,
            "Aid", "Augury", "Calm Emotions", "Continual Flame", "Enhance Ability", "Find Traps",
            "Gentle Repose", "Lesser Restoration", "Locate Object", "Prayer of Healing",
            "Protection from Poison", "Silence", "Warding Bond", "Zone of Truth");
        AddPrototypeClassSpellRange("Cleric", StatName.Wisdom, "radiant", 3,
            "Animate Dead", "Beacon of Hope", "Clairvoyance", "Create Food and Water", "Daylight",
            "Dispel Magic", "Feign Death", "Glyph of Warding", "Magic Circle", "Mass Healing Word",
            "Meld into Stone", "Protection from Energy", "Remove Curse", "Revivify", "Sending",
            "Speak with Dead", "Tongues", "Water Walk");

        // Bard list.
        AddPrototypeClassSpellRange("Bard", StatName.Charisma, "psychic", 0,
            "Blade Ward", "Dancing Lights", "Friends", "Light", "Mage Hand", "Mending",
            "Message", "Minor Illusion", "Prestidigitation", "True Strike");
        AddPrototypeClassSpellRange("Bard", StatName.Charisma, "psychic", 1,
            "Bane", "Charm Person", "Comprehend Languages", "Cure Wounds", "Detect Magic",
            "Disguise Self", "Faerie Fire", "Feather Fall", "Healing Word", "Heroism", "Identify",
            "Illusory Script", "Longstrider", "Silent Image", "Sleep", "Speak with Animals",
            "Unseen Servant");
        AddPrototypeClassSpellRange("Bard", StatName.Charisma, "psychic", 2,
            "Animal Messenger", "Blindness Deafness", "Calm Emotions", "Crown of Madness",
            "Detect Thoughts", "Enhance Ability", "Enthrall", "Hold Person", "Invisibility",
            "Knock", "Lesser Restoration", "Locate Animals or Plants", "Locate Object",
            "Magic Mouth", "Phantasmal Force", "See Invisibility", "Silence", "Suggestion",
            "Zone of Truth");
        AddPrototypeClassSpellRange("Bard", StatName.Charisma, "psychic", 3,
            "Bestow Curse", "Clairvoyance", "Dispel Magic", "Feign Death", "Glyph of Warding",
            "Leomunds Tiny Hut", "Major Image", "Nondetection", "Plant Growth", "Sending",
            "Speak with Dead", "Speak with Plants", "Stinking Cloud", "Tongues");

        // Paladin list.
        AddPrototypeClassSpellRange("Paladin", StatName.Charisma, "radiant", 1,
            "Bless", "Command", "Compelled Duel", "Cure Wounds", "Detect Evil and Good",
            "Detect Magic", "Detect Poison and Disease", "Heroism", "Protection from Evil and Good",
            "Purify Food and Drink", "Shield of Faith");
        AddPrototypeClassSpellRange("Paladin", StatName.Charisma, "radiant", 2,
            "Aid", "Find Steed", "Lesser Restoration", "Locate Object", "Protection from Poison",
            "Zone of Truth");
        AddPrototypeClassSpellRange("Paladin", StatName.Charisma, "radiant", 3,
            "Create Food and Water", "Daylight", "Dispel Magic", "Elemental Weapon",
            "Magic Circle", "Remove Curse", "Revivify");

        // Ranger list.
        AddPrototypeClassSpellRange("Ranger", StatName.Wisdom, "piercing", 1,
            "Alarm", "Animal Friendship", "Cure Wounds", "Detect Magic", "Detect Poison and Disease",
            "Fog Cloud", "Goodberry", "Jump", "Longstrider", "Speak with Animals");
        AddPrototypeClassSpellRange("Ranger", StatName.Wisdom, "piercing", 2,
            "Aid", "Animal Messenger", "Barkskin", "Beast Sense", "Darkvision", "Find Traps",
            "Lesser Restoration", "Locate Animals or Plants", "Locate Object", "Protection from Poison",
            "Silence");
        AddPrototypeClassSpellRange("Ranger", StatName.Wisdom, "piercing", 3,
            "Conjure Animals", "Daylight", "Nondetection", "Plant Growth", "Protection from Energy",
            "Speak with Plants", "Water Breathing", "Water Walk", "Wind Wall");

        // Keep progression sorted and deterministic for menus.
        foreach (var className in ClassSpellUnlocks.Keys.ToList())
        {
            ClassSpellUnlocks[className] = ClassSpellUnlocks[className]
                .OrderBy(entry => entry.MinLevel)
                .ThenBy(entry => entry.SpellId, StringComparer.Ordinal)
                .ToList();
        }

        ApplyRuntimeMetadata();
    }

    public static SpellEffectRouteSpec ResolveEffectRoute(SpellDefinition spell)
    {
        return spell.EffectRoute ?? BuildLegacyEffectRoute(spell);
    }

    public static string GetCombatFamilyLabel(SpellDefinition spell)
    {
        return GetCombatFamilyLabel(ResolveEffectRoute(spell).CombatFamily);
    }

    public static string GetCombatFamilyLabel(SpellCombatFamily family)
    {
        return family switch
        {
            SpellCombatFamily.DirectDamage => "Direct",
            SpellCombatFamily.BurstDamage => "Burst",
            SpellCombatFamily.DamageOverTime => "DoT",
            SpellCombatFamily.MarkDebuff => "Mark",
            SpellCombatFamily.ControlSpell => "Control",
            SpellCombatFamily.DebuffHex => "Hex",
            SpellCombatFamily.SmiteStrike => "Smite",
            SpellCombatFamily.HealSupport => "Support",
            SpellCombatFamily.WeaponRider => "Rider",
            SpellCombatFamily.SelfBuff => "Buff",
            SpellCombatFamily.SummonConjuration => "Summon",
            SpellCombatFamily.HazardZone => "Hazard",
            SpellCombatFamily.Utility => "Utility",
            _ => "Spell"
        };
    }

    public static int GetCombatFamilySortOrder(SpellDefinition spell)
    {
        return ResolveEffectRoute(spell).CombatFamily switch
        {
            SpellCombatFamily.DirectDamage => 0,
            SpellCombatFamily.BurstDamage => 1,
            SpellCombatFamily.DamageOverTime => 2,
            SpellCombatFamily.MarkDebuff => 3,
            SpellCombatFamily.ControlSpell => 4,
            SpellCombatFamily.DebuffHex => 5,
            SpellCombatFamily.SmiteStrike => 6,
            SpellCombatFamily.HealSupport => 7,
            SpellCombatFamily.WeaponRider => 8,
            SpellCombatFamily.SelfBuff => 9,
            SpellCombatFamily.SummonConjuration => 10,
            SpellCombatFamily.HazardZone => 11,
            SpellCombatFamily.Utility => 12,
            _ => 99
        };
    }

    private static SpellEffectRouteSpec BuildLegacyEffectRoute(SpellDefinition spell)
    {
        return new SpellEffectRouteSpec
        {
            RouteKind = SpellEffectRouteKind.DirectDamage,
            TargetShape = spell.TargetShape,
            Element = ResolveElementFromDamageTag(spell.DamageTag),
            CombatFamily = SpellCombatFamily.DirectDamage,
            DealsDirectDamage = true
        };
    }

    private static SpellElement ResolveElementFromDamageTag(string damageTag)
    {
        return damageTag.ToLowerInvariant() switch
        {
            "fire" => SpellElement.Fire,
            "cold" => SpellElement.Cold,
            "lightning" => SpellElement.Lightning,
            "acid" => SpellElement.Acid,
            "thunder" => SpellElement.Thunder,
            "radiant" => SpellElement.Radiant,
            "necrotic" => SpellElement.Necrotic,
            "psychic" => SpellElement.Psychic,
            "force" => SpellElement.Force,
            "nature" => SpellElement.Nature,
            "piercing" => SpellElement.Piercing,
            "water" => SpellElement.Water,
            "arcane" => SpellElement.Arcane,
            "poison" => SpellElement.Poison,
            "elemental" => SpellElement.Elemental,
            _ => SpellElement.Unknown
        };
    }

    private static CombatStatusApplySpec Status(
        CombatStatusKind kind,
        int potency,
        int durationTurns,
        int chancePercent = 100,
        StatName? initialSaveStat = null,
        StatName? repeatSaveStat = null,
        bool breaksOnDamageTaken = false)
    {
        return new CombatStatusApplySpec
        {
            Kind = kind,
            Potency = potency,
            DurationTurns = durationTurns,
            ChancePercent = Math.Clamp(chancePercent, 1, 100),
            InitialSaveStat = initialSaveStat,
            RepeatSaveStat = repeatSaveStat,
            BreaksOnDamageTaken = breaksOnDamageTaken
        };
    }

    private static CombatHazardSpec Hazard(
        int radiusTiles,
        int durationRounds,
        bool followsPlayer = false,
        bool requiresConcentration = false,
        bool triggersOnTurnStart = true,
        bool triggersOnEntry = false,
        StatName? initialSaveStat = null,
        SpellSaveDamageBehavior saveDamageBehavior = SpellSaveDamageBehavior.None,
        params CombatStatusApplySpec[] statuses)
    {
        return new CombatHazardSpec
        {
            RadiusTiles = Math.Max(0, radiusTiles),
            DurationRounds = Math.Max(1, durationRounds),
            FollowsPlayer = followsPlayer,
            RequiresConcentration = requiresConcentration,
            TriggersOnTurnStart = triggersOnTurnStart,
            TriggersOnEntry = triggersOnEntry,
            InitialSaveStat = initialSaveStat,
            SaveDamageBehavior = saveDamageBehavior,
            OnTriggerStatuses = statuses
        };
    }

    private static SpellEffectRouteSpec FutureGate(
        string requirement,
        SpellCombatFamily family,
        SpellTargetShape targetShape = SpellTargetShape.SingleEnemy)
    {
        return new SpellEffectRouteSpec
        {
            RouteKind = SpellEffectRouteKind.FutureGated,
            TargetShape = targetShape,
            CombatFamily = family,
            SupportState = SpellSupportState.FutureGated,
            DealsDirectDamage = false,
            FutureRequirement = requirement
        };
    }

    private static void ApplyRuntimeMetadata()
    {
        foreach (var spell in ById.Values)
        {
            spell.EffectRoute = BuildLegacyEffectRoute(spell);
        }

        // Mage
        SetRoute("mage_fire_bolt", SpellCombatFamily.DamageOverTime, SpellElement.Fire, statuses: new[] { Status(CombatStatusKind.Burning, 2, 2) });
        SetRoute("mage_ray_of_frost", SpellCombatFamily.DebuffHex, SpellElement.Cold, statuses: new[] { Status(CombatStatusKind.Chilled, 1, 2) });
        SetRoute("mage_chill_touch", SpellCombatFamily.DebuffHex, SpellElement.Necrotic, statuses: new[] { Status(CombatStatusKind.Weakened, 1, 2) });
        SetRoute("mage_shocking_grasp", SpellCombatFamily.ControlSpell, SpellElement.Lightning, statuses: new[] { Status(CombatStatusKind.Shocked, 2, 1) });
        SetRoute("mage_acid_splash", SpellCombatFamily.DirectDamage, SpellElement.Acid, "Single-target splash in current build.", initialSaveStat: StatName.Dexterity, saveDamageBehavior: SpellSaveDamageBehavior.NegateOnSave);
        SetRoute("mage_magic_missile", SpellCombatFamily.DirectDamage, SpellElement.Force);
        SetRoute("mage_burning_hands", SpellCombatFamily.BurstDamage, SpellElement.Fire, targetShape: SpellTargetShape.Cone, areaRadiusTiles: 3, initialSaveStat: StatName.Dexterity, saveDamageBehavior: SpellSaveDamageBehavior.HalfOnSave, statuses: new[] { Status(CombatStatusKind.Burning, 2, 2) });
        SetRoute("mage_chromatic_orb", SpellCombatFamily.BurstDamage, SpellElement.Elemental, "Choose acid, cold, fire, lightning, poison, or thunder at cast.");
        SetRoute("mage_ice_knife", SpellCombatFamily.BurstDamage, SpellElement.Cold, targetShape: SpellTargetShape.Radius, areaRadiusTiles: 1, initialSaveStat: StatName.Dexterity, saveDamageBehavior: SpellSaveDamageBehavior.HalfOnSave, statuses: new[] { Status(CombatStatusKind.Chilled, 1, 1) });
        SetRoute("mage_scorching_ray", SpellCombatFamily.BurstDamage, SpellElement.Fire, "Two ray hits on the anchored target.", statuses: new[] { Status(CombatStatusKind.Burning, 1, 2) });
        SetRoute("mage_shatter", SpellCombatFamily.BurstDamage, SpellElement.Thunder, targetShape: SpellTargetShape.Radius, areaRadiusTiles: 1, initialSaveStat: StatName.Constitution, saveDamageBehavior: SpellSaveDamageBehavior.HalfOnSave);
        SetRoute(
            "mage_web",
            SpellCombatFamily.HazardZone,
            SpellElement.Arcane,
            "Placed web zone that restrains creatures that fail to resist it.",
            targetShape: SpellTargetShape.Tile,
            requiresConcentration: true,
            dealsDirectDamage: false,
            hazardSpec: Hazard(radiusTiles: 1, durationRounds: 3, requiresConcentration: true, triggersOnTurnStart: true, triggersOnEntry: true, statuses: new[] { Status(CombatStatusKind.Restrained, 1, 1, initialSaveStat: StatName.Dexterity, repeatSaveStat: StatName.Strength) }));
        SetRoute("mage_melfs_acid_arrow", SpellCombatFamily.DamageOverTime, SpellElement.Acid, statuses: new[] { Status(CombatStatusKind.Corroded, 2, 2) });
        SetRoute("mage_fireball", SpellCombatFamily.BurstDamage, SpellElement.Fire, targetShape: SpellTargetShape.Radius, areaRadiusTiles: 2, initialSaveStat: StatName.Dexterity, saveDamageBehavior: SpellSaveDamageBehavior.HalfOnSave, statuses: new[] { Status(CombatStatusKind.Burning, 2, 2) });
        SetRoute("mage_lightning_bolt", SpellCombatFamily.BurstDamage, SpellElement.Lightning, targetShape: SpellTargetShape.Line, initialSaveStat: StatName.Dexterity, saveDamageBehavior: SpellSaveDamageBehavior.HalfOnSave, statuses: new[] { Status(CombatStatusKind.Shocked, 1, 1) });
        SetRoute("mage_tidal_wave", SpellCombatFamily.BurstDamage, SpellElement.Water, targetShape: SpellTargetShape.Line, initialSaveStat: StatName.Dexterity, saveDamageBehavior: SpellSaveDamageBehavior.HalfOnSave, statuses: new[] { Status(CombatStatusKind.Slowed, 1, 1) });

        // Cleric
        SetRoute("cleric_cure_wounds",         SpellCombatFamily.HealSupport, SpellElement.Radiant,  "Heals you for 1d8 + Wisdom modifier.", targetShape: SpellTargetShape.Self, dealsDirectDamage: false);
        SetRoute("cleric_healing_word",        SpellCombatFamily.HealSupport, SpellElement.Radiant,  "Heals you for 1d4+3 + Wisdom modifier (bonus action — no counter).", targetShape: SpellTargetShape.Self, dealsDirectDamage: false);
        SetRoute("cleric_prayer_of_healing",   SpellCombatFamily.HealSupport, SpellElement.Radiant,  "Heals you for 2d8 + Wisdom modifier.", targetShape: SpellTargetShape.Self, dealsDirectDamage: false);
        SetRoute("cleric_lesser_restoration",  SpellCombatFamily.HealSupport, SpellElement.Radiant,  "Removes one active condition (Poisoned, Weakened).", targetShape: SpellTargetShape.Self, dealsDirectDamage: false, routeKind: SpellEffectRouteKind.Cleanse);
        SetRoute("cleric_sacred_flame", SpellCombatFamily.DirectDamage, SpellElement.Radiant, initialSaveStat: StatName.Dexterity, saveDamageBehavior: SpellSaveDamageBehavior.NegateOnSave);
        SetRoute("cleric_toll_the_dead", SpellCombatFamily.DirectDamage, SpellElement.Necrotic, "Hits harder against wounded foes.", initialSaveStat: StatName.Wisdom, saveDamageBehavior: SpellSaveDamageBehavior.NegateOnSave);
        SetRoute("cleric_word_of_radiance", SpellCombatFamily.BurstDamage, SpellElement.Radiant, targetShape: SpellTargetShape.Self, areaRadiusTiles: 1, initialSaveStat: StatName.Constitution, saveDamageBehavior: SpellSaveDamageBehavior.NegateOnSave);
        SetRoute("cleric_guiding_bolt", SpellCombatFamily.MarkDebuff, SpellElement.Radiant, statuses: new[] { Status(CombatStatusKind.Marked, 2, 2) });
        SetRoute("cleric_inflict_wounds", SpellCombatFamily.DirectDamage, SpellElement.Necrotic);
        SetRoute("cleric_command", SpellCombatFamily.ControlSpell, SpellElement.Psychic, "Choose Halt, Flee, or Grovel at cast.", dealsDirectDamage: false, initialSaveStat: StatName.Wisdom);
        SetRoute("cleric_bane", SpellCombatFamily.DebuffHex, SpellElement.Psychic, "Curses enemies in a small area around the target point.", targetShape: SpellTargetShape.Radius, areaRadiusTiles: 1, requiresConcentration: true, dealsDirectDamage: false, initialSaveStat: StatName.Charisma, statuses: new[] { Status(CombatStatusKind.Weakened, 1, 2) });
        SetRoute("cleric_hold_person", SpellCombatFamily.ControlSpell, SpellElement.Radiant, "Paralyzing hold that only works on humanoids.", requiresConcentration: true, dealsDirectDamage: false, initialSaveStat: StatName.Wisdom, allowedCreatureTypes: CreatureTypeTag.Humanoid, statuses: new[] { Status(CombatStatusKind.Paralyzed, 1, 2, repeatSaveStat: StatName.Wisdom) });
        SetRoute("cleric_blindness", SpellCombatFamily.ControlSpell, SpellElement.Necrotic, "Blind a foe unless it shakes off the curse.", dealsDirectDamage: false, initialSaveStat: StatName.Constitution, statuses: new[] { Status(CombatStatusKind.Blinded, 2, 2, repeatSaveStat: StatName.Constitution) });
        SetRoute("cleric_bestow_curse", SpellCombatFamily.DebuffHex, SpellElement.Necrotic, "Lay a heavy curse that weakens and exposes one foe.", requiresConcentration: true, dealsDirectDamage: false, initialSaveStat: StatName.Wisdom, statuses: new[] { Status(CombatStatusKind.Cursed, 2, 3) });
        SetRoute(
            "cleric_spirit_guardians",
            SpellCombatFamily.HazardZone,
            SpellElement.Radiant,
            targetShape: SpellTargetShape.Self,
            areaRadiusTiles: 2,
            requiresConcentration: true,
            hazardSpec: Hazard(radiusTiles: 2, durationRounds: 4, followsPlayer: true, requiresConcentration: true, triggersOnTurnStart: true, initialSaveStat: StatName.Wisdom, saveDamageBehavior: SpellSaveDamageBehavior.HalfOnSave));
        SetRoute("cleric_flame_strike", SpellCombatFamily.BurstDamage, SpellElement.Fire, targetShape: SpellTargetShape.Radius, areaRadiusTiles: 1, initialSaveStat: StatName.Dexterity, saveDamageBehavior: SpellSaveDamageBehavior.HalfOnSave, statuses: new[] { Status(CombatStatusKind.Burning, 2, 2) });

        // Bard
        SetRoute("bard_healing_word",    SpellCombatFamily.HealSupport, SpellElement.Radiant,  "Heals you for 1d4+3 + Charisma modifier (bonus action — no counter).", targetShape: SpellTargetShape.Self, dealsDirectDamage: false);
        SetRoute("bard_vicious_mockery", SpellCombatFamily.DebuffHex, SpellElement.Psychic, initialSaveStat: StatName.Wisdom, saveDamageBehavior: SpellSaveDamageBehavior.NegateOnSave, statuses: new[] { Status(CombatStatusKind.Weakened, 1, 2) });
        SetRoute("bard_thunderclap", SpellCombatFamily.BurstDamage, SpellElement.Thunder, targetShape: SpellTargetShape.Self, areaRadiusTiles: 1, initialSaveStat: StatName.Constitution, saveDamageBehavior: SpellSaveDamageBehavior.NegateOnSave);
        SetRoute("bard_mind_sliver", SpellCombatFamily.DebuffHex, SpellElement.Psychic, initialSaveStat: StatName.Intelligence, saveDamageBehavior: SpellSaveDamageBehavior.NegateOnSave, statuses: new[] { Status(CombatStatusKind.Weakened, 1, 1) });
        SetRoute("bard_dissonant_whispers", SpellCombatFamily.ControlSpell, SpellElement.Psychic, initialSaveStat: StatName.Wisdom, saveDamageBehavior: SpellSaveDamageBehavior.HalfOnSave, statuses: new[] { Status(CombatStatusKind.Feared, 1, 2) });
        SetRoute("bard_thunderwave", SpellCombatFamily.BurstDamage, SpellElement.Thunder, "Blasts enemies away from you.", targetShape: SpellTargetShape.Self, areaRadiusTiles: 2, initialSaveStat: StatName.Constitution, saveDamageBehavior: SpellSaveDamageBehavior.HalfOnSave);
        SetRoute("bard_hideous_laughter", SpellCombatFamily.ControlSpell, SpellElement.Psychic, "Crippling laughter can leave one foe incapacitated until it recovers.", requiresConcentration: true, dealsDirectDamage: false, initialSaveStat: StatName.Wisdom, statuses: new[] { Status(CombatStatusKind.Incapacitated, 1, 2, repeatSaveStat: StatName.Wisdom, breaksOnDamageTaken: true) });
        SetRoute("bard_shatter", SpellCombatFamily.BurstDamage, SpellElement.Thunder, targetShape: SpellTargetShape.Radius, areaRadiusTiles: 1, initialSaveStat: StatName.Constitution, saveDamageBehavior: SpellSaveDamageBehavior.HalfOnSave);
        SetRoute("bard_heat_metal", SpellCombatFamily.DamageOverTime, SpellElement.Fire, "Sustained burning pressure on one foe.", statuses: new[] { Status(CombatStatusKind.Burning, 2, 2) }, requiresConcentration: true);
        SetRoute("bard_cloud_of_daggers", SpellCombatFamily.HazardZone, SpellElement.Force, targetShape: SpellTargetShape.Tile, requiresConcentration: true, hazardSpec: Hazard(radiusTiles: 0, durationRounds: 3, requiresConcentration: true, triggersOnTurnStart: true));
        SetRoute("bard_hypnotic_pattern", SpellCombatFamily.ControlSpell, SpellElement.Psychic, "Mesmerizing cone that incapacitates enemies who fail to resist it.", targetShape: SpellTargetShape.Cone, areaRadiusTiles: 4, requiresConcentration: true, dealsDirectDamage: false, initialSaveStat: StatName.Wisdom, statuses: new[] { Status(CombatStatusKind.Incapacitated, 1, 2, breaksOnDamageTaken: true) });
        SetRoute("bard_fear", SpellCombatFamily.ControlSpell, SpellElement.Psychic, "Terror wave that drives enemies back if they fail to resist it.", targetShape: SpellTargetShape.Cone, areaRadiusTiles: 4, requiresConcentration: true, dealsDirectDamage: false, initialSaveStat: StatName.Wisdom, statuses: new[] { Status(CombatStatusKind.Feared, 1, 2, repeatSaveStat: StatName.Wisdom) });
        SetRoute("bard_slow", SpellCombatFamily.DebuffHex, SpellElement.Arcane, "Warped time clings to enemies who fail to resist it.", targetShape: SpellTargetShape.Radius, areaRadiusTiles: 1, requiresConcentration: true, dealsDirectDamage: false, initialSaveStat: StatName.Wisdom, statuses: new[] { Status(CombatStatusKind.Slowed, 1, 2, repeatSaveStat: StatName.Wisdom) });

        // Paladin
        SetRoute("paladin_cure_wounds",        SpellCombatFamily.HealSupport, SpellElement.Radiant,  "Heals you for 1d8 + Charisma modifier.", targetShape: SpellTargetShape.Self, dealsDirectDamage: false);
        SetRoute("paladin_searing_smite", SpellCombatFamily.SmiteStrike, SpellElement.Fire, "Empowers a melee strike instead of firing as a ranged spell.", statuses: new[] { Status(CombatStatusKind.Burning, 2, 2) });
        SetRoute("paladin_thunderous_smite", SpellCombatFamily.SmiteStrike, SpellElement.Thunder, "Empowers a melee strike and blasts the foe backward.", statuses: Array.Empty<CombatStatusApplySpec>());
        SetRoute("paladin_wrathful_smite", SpellCombatFamily.SmiteStrike, SpellElement.Psychic, "Empowers a melee strike with fear.", statuses: new[] { Status(CombatStatusKind.Feared, 1, 2, initialSaveStat: StatName.Wisdom, repeatSaveStat: StatName.Wisdom) });
        SetRoute("paladin_branding_smite", SpellCombatFamily.SmiteStrike, SpellElement.Radiant, "Empowers a melee strike and brands the foe.", statuses: new[] { Status(CombatStatusKind.Marked, 1, 3) });
        SetRoute("paladin_blinding_smite", SpellCombatFamily.SmiteStrike, SpellElement.Radiant, "Empowers a melee strike with searing light.", statuses: new[] { Status(CombatStatusKind.Blinded, 2, 2, initialSaveStat: StatName.Constitution, repeatSaveStat: StatName.Constitution) });

        // Ranger
        SetRoute("ranger_hunters_mark", SpellCombatFamily.MarkDebuff, SpellElement.Piercing, statuses: new[] { Status(CombatStatusKind.Marked, 2, 3) }, requiresConcentration: true);
        SetRoute("ranger_hail_of_thorns", SpellCombatFamily.BurstDamage, SpellElement.Piercing, targetShape: SpellTargetShape.Radius, areaRadiusTiles: 1, initialSaveStat: StatName.Dexterity, saveDamageBehavior: SpellSaveDamageBehavior.HalfOnSave);
        SetRoute("ranger_ensnaring_strike", SpellCombatFamily.ControlSpell, SpellElement.Nature, "Empowers a melee strike with binding vines.", statuses: new[] { Status(CombatStatusKind.Restrained, 1, 2, initialSaveStat: StatName.Strength, repeatSaveStat: StatName.Strength) });
        SetRoute("ranger_cordon_of_arrows", SpellCombatFamily.HazardZone, SpellElement.Piercing, targetShape: SpellTargetShape.Tile, hazardSpec: Hazard(radiusTiles: 1, durationRounds: 3, triggersOnTurnStart: true, triggersOnEntry: true));
        SetRoute("ranger_spike_growth", SpellCombatFamily.HazardZone, SpellElement.Piercing, targetShape: SpellTargetShape.Tile, requiresConcentration: true, hazardSpec: Hazard(radiusTiles: 2, durationRounds: 3, requiresConcentration: true, triggersOnTurnStart: false, triggersOnEntry: true));
        SetRoute("ranger_lightning_arrow", SpellCombatFamily.BurstDamage, SpellElement.Lightning, targetShape: SpellTargetShape.Radius, areaRadiusTiles: 1, initialSaveStat: StatName.Dexterity, saveDamageBehavior: SpellSaveDamageBehavior.HalfOnSave, statuses: new[] { Status(CombatStatusKind.Shocked, 1, 1) });
        SetRoute("ranger_conjure_barrage", SpellCombatFamily.BurstDamage, SpellElement.Piercing, targetShape: SpellTargetShape.Cone, areaRadiusTiles: 4, initialSaveStat: StatName.Dexterity, saveDamageBehavior: SpellSaveDamageBehavior.HalfOnSave);

        SetRoute("cleric_spiritual_weapon", SpellCombatFamily.SummonConjuration, SpellElement.Force,
            "Summons a spectral weapon that auto-attacks each player turn.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Summon,
            requiresConcentration: true);
        SetRoute("mage_find_familiar", SpellCombatFamily.SummonConjuration, SpellElement.Force,
            "Summons a tiny familiar that auto-attacks each player turn.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Summon,
            requiresConcentration: true);
        SetRoute("mage_flaming_sphere", SpellCombatFamily.SummonConjuration, SpellElement.Fire,
            "Conjures a flaming sphere that rams enemies each player turn.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Summon,
            requiresConcentration: true);
        SetRoute("paladin_find_steed", SpellCombatFamily.SummonConjuration, SpellElement.Radiant,
            "Summons a celestial steed granting defense and mobility.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Summon,
            requiresConcentration: true);
        SetRoute("ranger_summon_beast", SpellCombatFamily.SummonConjuration, SpellElement.Piercing,
            "Calls a fey beast spirit that auto-attacks each player turn.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Summon,
            requiresConcentration: true);
        SetRoute("ranger_conjure_animals", SpellCombatFamily.SummonConjuration, SpellElement.Piercing,
            "Conjures a pack of beasts that auto-attack each player turn.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Summon,
            requiresConcentration: true);
        SetRoute("mage_summon_fey", SpellCombatFamily.SummonConjuration, SpellElement.Force,
            "Calls a fey spirit that teleports and auto-attacks each player turn.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Summon,
            requiresConcentration: true);
        SetRoute("mage_summon_undead", SpellCombatFamily.SummonConjuration, SpellElement.Necrotic,
            "Raises a spectral undead that auto-attacks each player turn.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Summon,
            requiresConcentration: true);
        SetRoute("cleric_animate_dead", SpellCombatFamily.SummonConjuration, SpellElement.Necrotic,
            "Raises a skeleton warrior that auto-attacks each player turn.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Summon,
            requiresConcentration: true);
        SetRoute("ranger_summon_fey", SpellCombatFamily.SummonConjuration, SpellElement.Force,
            "Calls a fey spirit of the wild that auto-attacks each player turn.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Summon,
            requiresConcentration: true);
        SetRoute("mage_summon_elemental", SpellCombatFamily.SummonConjuration, SpellElement.Fire,
            "Calls a fire elemental spirit that auto-attacks each player turn.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Summon,
            requiresConcentration: true);
        SetRoute("mage_summon_shadowspawn", SpellCombatFamily.SummonConjuration, SpellElement.Psychic,
            "Calls a dread shadow entity that auto-attacks each player turn.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Summon,
            requiresConcentration: true);
        SetRoute("mage_phantom_steed", SpellCombatFamily.SummonConjuration, SpellElement.Force,
            "Conjures a spectral horse granting defense and mobility.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Summon,
            requiresConcentration: true);
        SetRoute("cleric_summon_celestial", SpellCombatFamily.SummonConjuration, SpellElement.Radiant,
            "Calls a radiant guardian angel that auto-attacks each player turn.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Summon,
            requiresConcentration: true);
        SetRoute("bard_summon_fey", SpellCombatFamily.SummonConjuration, SpellElement.Psychic,
            "Calls a fey trickster that auto-attacks each player turn.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Summon,
            requiresConcentration: true);
        SetRoute("paladin_summon_celestial", SpellCombatFamily.SummonConjuration, SpellElement.Radiant,
            "Calls a celestial avenger that auto-attacks each player turn.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Summon,
            requiresConcentration: true);
        SetRoute("ranger_summon_plant", SpellCombatFamily.SummonConjuration, SpellElement.Nature,
            "Calls a thorny plant creature that auto-attacks each player turn.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Summon,
            requiresConcentration: true);
        SetRoute("paladin_divine_favor", SpellCombatFamily.WeaponRider, SpellElement.Radiant,
            "+1d4 radiant per hit while concentrating.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.WeaponRider,
            requiresConcentration: true);
        SetRoute("paladin_magic_weapon", SpellCombatFamily.WeaponRider, SpellElement.Force,
            "+1 attack, +1d6 force per hit while concentrating.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.WeaponRider,
            requiresConcentration: true);
        SetRoute("paladin_aura_of_vitality", SpellCombatFamily.HealSupport, SpellElement.Radiant,
            "Concentration, up to 1 minute. Start of each turn: heals 2d6 HP.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.ConcentrationAura,
            requiresConcentration: true,
            hazardSpec: Hazard(radiusTiles: 0, durationRounds: 10, requiresConcentration: true, triggersOnTurnStart: false));
        SetRoute("paladin_crusaders_mantle", SpellCombatFamily.WeaponRider, SpellElement.Radiant,
            "+1d6 radiant per hit, +1 defense while concentrating.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.WeaponRider,
            requiresConcentration: true);
        SetRoute("ranger_zephyr_strike", SpellCombatFamily.SelfBuff, SpellElement.Force,
            "+1d8 force next hit, +10 flee while concentrating.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        GateFutureSpell("ranger_pass_without_trace", SpellCombatFamily.Utility, "Exploration utility runtime");

        // === Batch 1 — Core Buffs & Defenses routes ===
        SetRoute("cleric_shield_of_faith", SpellCombatFamily.SelfBuff, SpellElement.Radiant,
            "+2 AC while concentrating.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        SetRoute("paladin_shield_of_faith", SpellCombatFamily.SelfBuff, SpellElement.Radiant,
            "+2 AC while concentrating.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        SetRoute("cleric_bless", SpellCombatFamily.SelfBuff, SpellElement.Radiant,
            "+1d4 damage and saves while concentrating.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        SetRoute("paladin_heroism", SpellCombatFamily.HealSupport, SpellElement.Radiant,
            "Concentration. Start of each turn: gain CHA mod temp HP.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.ConcentrationAura,
            requiresConcentration: true,
            hazardSpec: Hazard(radiusTiles: 0, durationRounds: 10, requiresConcentration: true, triggersOnTurnStart: false));
        SetRoute("bard_heroism", SpellCombatFamily.HealSupport, SpellElement.Psychic,
            "Concentration. Start of each turn: gain CHA mod temp HP.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.ConcentrationAura,
            requiresConcentration: true,
            hazardSpec: Hazard(radiusTiles: 0, durationRounds: 10, requiresConcentration: true, triggersOnTurnStart: false));
        SetRoute("mage_mage_armor", SpellCombatFamily.SelfBuff, SpellElement.Force,
            "+3 AC while unarmored. No concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: false);
        SetRoute("mage_shield", SpellCombatFamily.SelfBuff, SpellElement.Force,
            "+5 AC until your next turn. No concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: false);
        SetRoute("mage_false_life", SpellCombatFamily.SelfBuff, SpellElement.Necrotic,
            "Gain 1d4+4 temporary HP. No concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: false);
        SetRoute("ranger_barkskin", SpellCombatFamily.SelfBuff, SpellElement.Nature,
            "AC cannot be lower than 16. Concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        SetRoute("cleric_aid", SpellCombatFamily.SelfBuff, SpellElement.Radiant,
            "+5 max HP and current HP. No concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: false);
        SetRoute("paladin_aid", SpellCombatFamily.SelfBuff, SpellElement.Radiant,
            "+5 max HP and current HP. No concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: false);
        SetRoute("mage_blur", SpellCombatFamily.SelfBuff, SpellElement.Arcane,
            "Enemy attacks roll with disadvantage. Concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        SetRoute("mage_haste", SpellCombatFamily.SelfBuff, SpellElement.Arcane,
            "+2 AC, +2 combat movement. Concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        // Batch 2 — Tactical combat spell routes
        SetRoute("mage_misty_step", SpellCombatFamily.SelfBuff, SpellElement.Arcane,
            "+3 move points this turn. No concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: false);
        SetRoute("mage_mirror_image", SpellCombatFamily.SelfBuff, SpellElement.Arcane,
            "3 illusory duplicates absorb hits. No concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: false);
        SetRoute("mage_expeditious_retreat", SpellCombatFamily.SelfBuff, SpellElement.Arcane,
            "+15% flee chance. Concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        SetRoute("ranger_absorb_elements", SpellCombatFamily.SelfBuff, SpellElement.Nature,
            "+1d6 on next melee hit. No concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: false);
        SetRoute("ranger_longstrider", SpellCombatFamily.SelfBuff, SpellElement.Nature,
            "+2 move points per turn. Concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        SetRoute("bard_hex", SpellCombatFamily.WeaponRider, SpellElement.Necrotic,
            "+1d4 necrotic per hit, enemy -2 attack. Concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.WeaponRider,
            requiresConcentration: true);
        SetRoute("cleric_protection_evg", SpellCombatFamily.SelfBuff, SpellElement.Radiant,
            "+1 AC divine ward. Concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        SetRoute("paladin_protection_evg", SpellCombatFamily.SelfBuff, SpellElement.Radiant,
            "+1 AC divine ward. Concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        SetRoute("cleric_sanctuary", SpellCombatFamily.SelfBuff, SpellElement.Radiant,
            "Enemy saves or skips attack. Breaks on player attack. Concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        SetRoute("paladin_compelled_duel", SpellCombatFamily.SelfBuff, SpellElement.Radiant,
            "+2 melee bonus. Concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        SetRoute("bard_enhance_ability", SpellCombatFamily.SelfBuff, SpellElement.Arcane,
            "+2 AC, +3% flee (Cat's Grace). Concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        SetRoute("cleric_enhance_ability", SpellCombatFamily.SelfBuff, SpellElement.Radiant,
            "+2 AC, +3% flee (Cat's Grace). Concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        SetRoute("mage_enhance_ability", SpellCombatFamily.SelfBuff, SpellElement.Arcane,
            "+2 AC, +3% flee (Cat's Grace). Concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        // Batch 3 — Reactive & retaliation spell routes
        SetRoute("mage_hellish_rebuke", SpellCombatFamily.SelfBuff, SpellElement.Fire,
            "Primed: 2d6 fire to next attacker (consumed).",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: false);
        SetRoute("mage_armor_of_agathys", SpellCombatFamily.SelfBuff, SpellElement.Cold,
            "+8 frost temp HP; 1d8 cold to attackers while active.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: false);
        SetRoute("mage_fire_shield", SpellCombatFamily.SelfBuff, SpellElement.Fire,
            "Persistent 2d8 fire to attackers. No concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: false);
        SetRoute("mage_stoneskin", SpellCombatFamily.SelfBuff, SpellElement.Arcane,
            "−3 flat damage reduction. Concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        SetRoute("cleric_wrath_of_storm", SpellCombatFamily.SelfBuff, SpellElement.Lightning,
            "Primed: 2d8 lightning to next attacker (consumed).",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: false);
        SetRoute("cleric_spirit_shroud", SpellCombatFamily.SelfBuff, SpellElement.Radiant,
            "+1d8 radiant melee; 1d6 radiant reactive. Concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        SetRoute("cleric_death_ward", SpellCombatFamily.SelfBuff, SpellElement.Radiant,
            "Prevent death: HP→0 becomes 1 HP (consumed).",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: false);
        SetRoute("paladin_death_ward", SpellCombatFamily.SelfBuff, SpellElement.Radiant,
            "Prevent death: HP→0 becomes 1 HP (consumed).",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: false);
        SetRoute("paladin_holy_rebuke", SpellCombatFamily.SelfBuff, SpellElement.Radiant,
            "Primed: 2d6 radiant + heal 1d4 on next hit (consumed).",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: false);
        SetRoute("ranger_thorns", SpellCombatFamily.SelfBuff, SpellElement.Nature,
            "1d6 piercing to attackers. Concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        SetRoute("ranger_stoneskin", SpellCombatFamily.SelfBuff, SpellElement.Nature,
            "−3 flat damage reduction. Concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        SetRoute("bard_cutting_words", SpellCombatFamily.SelfBuff, SpellElement.Psychic,
            "Primed: reduce next enemy damage by 1d8 (consumed).",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: false);
        SetRoute("bard_greater_invisibility", SpellCombatFamily.SelfBuff, SpellElement.Arcane,
            "Advantage on attacks, enemy disadvantage. Concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);

        // === Batch 4 — Expanded Arsenal ===
        SetRoute("mage_sleep", SpellCombatFamily.ControlSpell, SpellElement.Arcane,
            "WIS save or Incapacitated 2 turns.",
            targetShape: SpellTargetShape.SingleEnemy, dealsDirectDamage: false,
            initialSaveStat: StatName.Wisdom, saveDamageBehavior: SpellSaveDamageBehavior.NegateOnSave,
            statuses: new[] { Status(CombatStatusKind.Incapacitated, 1, 2, repeatSaveStat: StatName.Wisdom) });
        SetRoute("mage_counterspell", SpellCombatFamily.SelfBuff, SpellElement.Arcane,
            "Primed reaction — next hit against you is halved.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff);
        SetRoute("mage_vampiric_touch", SpellCombatFamily.DirectDamage, SpellElement.Necrotic,
            "Necrotic hit; heals player for 50% of damage dealt.");
        SetRoute("cleric_mass_healing_word", SpellCombatFamily.HealSupport, SpellElement.Radiant,
            "Heals 2d8 + WIS and cleanses 1 condition.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false);
        SetRoute("cleric_daylight", SpellCombatFamily.BurstDamage, SpellElement.Radiant,
            "Radiant sunburst. DEX save or half damage; Undead/Fiend Blinded 2 turns.",
            targetShape: SpellTargetShape.Radius, areaRadiusTiles: 2,
            initialSaveStat: StatName.Dexterity, saveDamageBehavior: SpellSaveDamageBehavior.HalfOnSave,
            statuses: new[] { Status(CombatStatusKind.Blinded, 2, 2) });
        SetRoute("paladin_daylight", SpellCombatFamily.BurstDamage, SpellElement.Radiant,
            "Radiant sunburst. DEX save or half damage; Undead/Fiend Blinded 2 turns.",
            targetShape: SpellTargetShape.Radius, areaRadiusTiles: 2,
            initialSaveStat: StatName.Dexterity, saveDamageBehavior: SpellSaveDamageBehavior.HalfOnSave,
            statuses: new[] { Status(CombatStatusKind.Blinded, 2, 2) });
        SetRoute("bard_faerie_fire", SpellCombatFamily.DebuffHex, SpellElement.Arcane,
            "Concentration, radius 2. Marked (potency 2, 3 turns) on all enemies in area.",
            targetShape: SpellTargetShape.Radius, areaRadiusTiles: 2,
            requiresConcentration: true, dealsDirectDamage: false,
            statuses: new[] { Status(CombatStatusKind.Marked, 2, 3) });
        SetRoute("bard_charm_person", SpellCombatFamily.ControlSpell, SpellElement.Psychic,
            "Humanoid only. WIS save or Incapacitated 1 turn.",
            targetShape: SpellTargetShape.SingleEnemy, dealsDirectDamage: false,
            initialSaveStat: StatName.Wisdom, saveDamageBehavior: SpellSaveDamageBehavior.NegateOnSave,
            allowedCreatureTypes: CreatureTypeTag.Humanoid,
            statuses: new[] { Status(CombatStatusKind.Incapacitated, 1, 1) });
        SetRoute("bard_invisibility", SpellCombatFamily.SelfBuff, SpellElement.Arcane,
            "Enemy hit chance -15%. Breaks on attack. Concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        SetRoute("paladin_elemental_weapon", SpellCombatFamily.WeaponRider, SpellElement.Elemental,
            "Choose element at cast. +2d4 elemental per hit. Concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.WeaponRider,
            requiresConcentration: true);
        SetRoute("paladin_revivify", SpellCombatFamily.SelfBuff, SpellElement.Radiant,
            "If HP <= 25% max, heal 2d6 + CHA. Once per combat.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff);
        SetRoute("ranger_fog_cloud", SpellCombatFamily.HazardZone, SpellElement.Nature,
            "Concentration, radius 2. Enemies inside take -3 to attack rolls.",
            targetShape: SpellTargetShape.Tile, dealsDirectDamage: false,
            requiresConcentration: true,
            hazardSpec: Hazard(radiusTiles: 2, durationRounds: 4, requiresConcentration: true, triggersOnTurnStart: false));
        SetRoute("ranger_cure_wounds", SpellCombatFamily.HealSupport, SpellElement.Nature,
            "Heals 1d8 + Wisdom.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false);

        // === Batch 5 — Signature Powers ===
        SetRoute("mage_blink", SpellCombatFamily.SelfBuff, SpellElement.Arcane,
            "30% chance each enemy hit misses. Concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        SetRoute("mage_protection_from_energy", SpellCombatFamily.SelfBuff, SpellElement.Arcane,
            "Choose element at cast. Half damage from that type. Concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        SetRoute("cleric_beacon_of_hope", SpellCombatFamily.SelfBuff, SpellElement.Radiant,
            "Next heal doubled; Feared immunity. Concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        SetRoute("bard_major_image", SpellCombatFamily.SelfBuff, SpellElement.Arcane,
            "25% chance attacks target the decoy instead. Concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        SetRoute("paladin_aura_of_courage", SpellCombatFamily.SelfBuff, SpellElement.Radiant,
            "Feared cannot be applied; conditions reduced by 1 turn. Concentration.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.SelfBuff,
            requiresConcentration: true);
        SetRoute("ranger_plant_growth", SpellCombatFamily.HazardZone, SpellElement.Nature,
            "Concentration, radius 3. Enemies inside are Slowed each turn.",
            targetShape: SpellTargetShape.Tile, dealsDirectDamage: false,
            requiresConcentration: true,
            hazardSpec: Hazard(radiusTiles: 3, durationRounds: 4, requiresConcentration: true, triggersOnTurnStart: true,
                statuses: new[] { Status(CombatStatusKind.Slowed, 2, 2) }));

        SetRoute("ranger_flame_arrows", SpellCombatFamily.WeaponRider, SpellElement.Fire,
            "+1d8 fire per hit while concentrating.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.WeaponRider,
            requiresConcentration: true);

        // === Pass 9K — Transformation spell routes ===
        SetRoute("mage_polymorph", SpellCombatFamily.TransformationPolymorph, SpellElement.Arcane,
            "Transform into a beast form with new combat stats.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Transformation,
            requiresConcentration: true);
        SetRoute("mage_shadow_form", SpellCombatFamily.TransformationPolymorph, SpellElement.Necrotic,
            "Become a spectral shade. Damage resist, necrotic strikes.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Transformation,
            requiresConcentration: true);
        SetRoute("mage_elemental_form", SpellCombatFamily.TransformationPolymorph, SpellElement.Arcane,
            "Transform into an elemental.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Transformation,
            requiresConcentration: true);
        SetRoute("mage_monstrous_form", SpellCombatFamily.TransformationPolymorph, SpellElement.Arcane,
            "Take the form of a dungeon monster.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Transformation,
            requiresConcentration: true);
        SetRoute("ranger_wild_shape", SpellCombatFamily.TransformationPolymorph, SpellElement.Nature,
            "Shift into a small beast form.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Transformation,
            requiresConcentration: true);
        SetRoute("ranger_animal_form", SpellCombatFamily.TransformationPolymorph, SpellElement.Nature,
            "Shift into a larger beast form.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Transformation,
            requiresConcentration: true);
        SetRoute("ranger_insect_form", SpellCombatFamily.TransformationPolymorph, SpellElement.Nature,
            "Take insect/arachnid form.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Transformation,
            requiresConcentration: true);
        SetRoute("ranger_plant_form", SpellCombatFamily.TransformationPolymorph, SpellElement.Nature,
            "Become a plant creature.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Transformation,
            requiresConcentration: true);
        SetRoute("ranger_elemental_form", SpellCombatFamily.TransformationPolymorph, SpellElement.Nature,
            "Transform into an elemental.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Transformation,
            requiresConcentration: true);
        SetRoute("ranger_primal_form", SpellCombatFamily.TransformationPolymorph, SpellElement.Nature,
            "Take prehistoric beast form.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Transformation,
            requiresConcentration: true);
        SetRoute("cleric_divine_vessel", SpellCombatFamily.TransformationPolymorph, SpellElement.Radiant,
            "Channel divine power into an angelic form.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Transformation,
            requiresConcentration: true);
        SetRoute("cleric_stone_guardian", SpellCombatFamily.TransformationPolymorph, SpellElement.Radiant,
            "Become a divine stone fortress.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Transformation,
            requiresConcentration: true);
        SetRoute("paladin_holy_transformation", SpellCombatFamily.TransformationPolymorph, SpellElement.Radiant,
            "Transform into a celestial warrior.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Transformation,
            requiresConcentration: true);
        SetRoute("paladin_avatar_of_wrath", SpellCombatFamily.TransformationPolymorph, SpellElement.Radiant,
            "Become an avenging angel.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Transformation,
            requiresConcentration: true);
        SetRoute("bard_polymorph", SpellCombatFamily.TransformationPolymorph, SpellElement.Arcane,
            "Transform into a beast form.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Transformation,
            requiresConcentration: true);
        SetRoute("bard_gaseous_form", SpellCombatFamily.TransformationPolymorph, SpellElement.Arcane,
            "Become incorporeal mist. Cannot attack, massive flee.",
            targetShape: SpellTargetShape.Self, dealsDirectDamage: false,
            routeKind: SpellEffectRouteKind.Transformation,
            requiresConcentration: true);
    }

    private static void SetRoute(
        string spellId,
        SpellCombatFamily family,
        SpellElement element,
        string runtimeBehaviorNote = "",
        SpellTargetShape? targetShape = null,
        int areaRadiusTiles = 0,
        bool requiresConcentration = false,
        bool dealsDirectDamage = true,
        StatName? initialSaveStat = null,
        SpellSaveDamageBehavior saveDamageBehavior = SpellSaveDamageBehavior.None,
        CreatureTypeTag allowedCreatureTypes = CreatureTypeTag.Any,
        CombatHazardSpec? hazardSpec = null,
        SpellEffectRouteKind? routeKind = null,
        params CombatStatusApplySpec[] statuses)
    {
        if (!ById.TryGetValue(spellId, out var spell))
        {
            return;
        }

        if (targetShape.HasValue)
        {
            spell.TargetShape = targetShape.Value;
        }

        spell.EffectRoute = new SpellEffectRouteSpec
        {
            RouteKind = routeKind ?? (statuses.Length > 0 ? SpellEffectRouteKind.DamageAndStatus : SpellEffectRouteKind.DirectDamage),
            TargetShape = spell.TargetShape,
            Element = element,
            CombatFamily = family,
            DealsDirectDamage = dealsDirectDamage,
            RuntimeBehaviorNote = runtimeBehaviorNote,
            AreaRadiusTiles = Math.Max(0, areaRadiusTiles),
            RequiresConcentration = requiresConcentration,
            InitialSaveStat = initialSaveStat,
            SaveDamageBehavior = saveDamageBehavior,
            AllowedCreatureTypes = allowedCreatureTypes,
            HazardSpec = hazardSpec,
            OnHitStatuses = statuses
        };
    }

    private static void GateFutureSpell(string spellId, SpellCombatFamily family, string requirement)
    {
        if (!ById.TryGetValue(spellId, out var spell))
        {
            return;
        }

        spell.EffectRoute = FutureGate(requirement, family, spell.TargetShape);
    }

    private static void AddPrototypeClassSpellRange(
        string className,
        StatName scalingStat,
        string defaultDamageTag,
        int spellLevel,
        params string[] spellNames)
    {
        foreach (var spellName in spellNames)
        {
            AddPrototypeSpell(className, scalingStat, defaultDamageTag, spellLevel, spellName);
        }
    }

    private static void AddPrototypeSpell(
        string className,
        StatName scalingStat,
        string defaultDamageTag,
        int spellLevel,
        string spellName)
    {
        var classPrefix = className.ToLowerInvariant();
        var spellId = $"{classPrefix}_{Slugify(spellName)}";
        var unlockLevel = ResolvePrototypeUnlockLevel(className, spellLevel);

        if (!ById.ContainsKey(spellId))
        {
            var (baseDamage, variance, armorBypass) = ResolvePrototypeDamageProfile(spellLevel);
            ById[spellId] = new SpellDefinition
            {
                Id = spellId,
                Name = spellName,
                ClassName = className,
                SpellLevel = spellLevel,
                Description = $"Prototype adaptation of {spellName}.",
                ScalingStat = scalingStat,
                BaseDamage = baseDamage,
                Variance = variance,
                ArmorBypass = armorBypass,
                DamageTag = InferPrototypeDamageTag(defaultDamageTag, spellName),
                Origin = SpellOrigin.PrototypeExpanded,
                SuppressCounterAttack = ShouldSuppressCounterAttack(spellName)
            };
        }

        if (!ClassSpellUnlocks.TryGetValue(className, out var unlocks))
        {
            unlocks = new List<(int MinLevel, string SpellId)>();
            ClassSpellUnlocks[className] = unlocks;
        }

        var index = unlocks.FindIndex(entry => string.Equals(entry.SpellId, spellId, StringComparison.Ordinal));
        if (index >= 0)
        {
            var existing = unlocks[index];
            if (unlockLevel < existing.MinLevel)
            {
                unlocks[index] = (unlockLevel, spellId);
            }
        }
        else
        {
            unlocks.Add((unlockLevel, spellId));
        }
    }

    private static (int BaseDamage, int Variance, int ArmorBypass) ResolvePrototypeDamageProfile(int spellLevel)
    {
        return spellLevel switch
        {
            <= 0 => (6, 3, 1),
            1 => (10, 4, 1),
            2 => (14, 5, 2),
            _ => (18, 6, 2)
        };
    }

    private static int ResolvePrototypeUnlockLevel(string className, int spellLevel)
    {
        var isHalfCaster = string.Equals(className, "Paladin", StringComparison.Ordinal) ||
                           string.Equals(className, "Ranger", StringComparison.Ordinal);

        if (spellLevel <= 0)
        {
            return 1;
        }

        if (!isHalfCaster)
        {
            return spellLevel switch
            {
                1 => 1,
                2 => 3,
                _ => 5
            };
        }

        return spellLevel switch
        {
            1 => 2,
            2 => 5,
            _ => 6
        };
    }

    private static bool ShouldSuppressCounterAttack(string spellName)
    {
        var lower = spellName.ToLowerInvariant();
        var controlKeywords = new[]
        {
            "hold", "fear", "slow", "charm", "sleep", "blind", "silence", "web", "command",
            "suggestion", "laughter", "ensnaring", "hypnotic", "stinking", "fog", "darkness",
            "invisibility", "hideous", "confusion", "restrain", "paralyze", "stun", "banish"
        };

        return controlKeywords.Any(keyword => lower.Contains(keyword, StringComparison.Ordinal));
    }

    private static string InferPrototypeDamageTag(string defaultTag, string spellName)
    {
        var lower = spellName.ToLowerInvariant();
        if (lower.Contains("fire", StringComparison.Ordinal) ||
            lower.Contains("flame", StringComparison.Ordinal) ||
            lower.Contains("burn", StringComparison.Ordinal) ||
            lower.Contains("searing", StringComparison.Ordinal) ||
            lower.Contains("scorch", StringComparison.Ordinal))
        {
            return "fire";
        }

        if (lower.Contains("frost", StringComparison.Ordinal) ||
            lower.Contains("ice", StringComparison.Ordinal) ||
            lower.Contains("cold", StringComparison.Ordinal))
        {
            return "cold";
        }

        if (lower.Contains("lightning", StringComparison.Ordinal))
        {
            return "lightning";
        }

        if (lower.Contains("thunder", StringComparison.Ordinal))
        {
            return "thunder";
        }

        if (lower.Contains("acid", StringComparison.Ordinal))
        {
            return "acid";
        }

        if (lower.Contains("radiant", StringComparison.Ordinal) ||
            lower.Contains("sacred", StringComparison.Ordinal) ||
            lower.Contains("guiding", StringComparison.Ordinal) ||
            lower.Contains("spirit", StringComparison.Ordinal) ||
            lower.Contains("divine", StringComparison.Ordinal) ||
            lower.Contains("holy", StringComparison.Ordinal))
        {
            return "radiant";
        }

        if (lower.Contains("necro", StringComparison.Ordinal) ||
            lower.Contains("curse", StringComparison.Ordinal) ||
            lower.Contains("dead", StringComparison.Ordinal))
        {
            return "necrotic";
        }

        if (lower.Contains("psych", StringComparison.Ordinal) ||
            lower.Contains("mind", StringComparison.Ordinal) ||
            lower.Contains("fear", StringComparison.Ordinal) ||
            lower.Contains("mockery", StringComparison.Ordinal) ||
            lower.Contains("whisper", StringComparison.Ordinal) ||
            lower.Contains("laughter", StringComparison.Ordinal) ||
            lower.Contains("charm", StringComparison.Ordinal))
        {
            return "psychic";
        }

        if (lower.Contains("poison", StringComparison.Ordinal))
        {
            return "poison";
        }

        if (lower.Contains("dagger", StringComparison.Ordinal) ||
            lower.Contains("arrow", StringComparison.Ordinal) ||
            lower.Contains("barrage", StringComparison.Ordinal) ||
            lower.Contains("thorn", StringComparison.Ordinal))
        {
            return "piercing";
        }

        return defaultTag;
    }

    private static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "spell";
        }

        var builder = new System.Text.StringBuilder(value.Length);
        var previousUnderscore = false;
        foreach (var ch in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                previousUnderscore = false;
            }
            else if (!previousUnderscore)
            {
                builder.Append('_');
                previousUnderscore = true;
            }
        }

        var slug = builder.ToString().Trim('_');
        return string.IsNullOrWhiteSpace(slug) ? "spell" : slug;
    }
}

public static class SpellProgression
{
    // Spell pick points granted when reaching each level.
    // Full casters get starting picks at level 1; half casters begin at level 2.
    private static readonly Dictionary<string, Dictionary<int, int>> PicksByClassAndLevel = new()
    {
        ["Mage"] = new()
        {
            [1] = 4,
            [2] = 1,
            [3] = 1,
            [4] = 1,
            [5] = 1,
            [6] = 1
        },
        ["Cleric"] = new()
        {
            [1] = 4,
            [2] = 1,
            [3] = 1,
            [4] = 1,
            [5] = 1,
            [6] = 1
        },
        ["Bard"] = new()
        {
            [1] = 4,
            [2] = 1,
            [3] = 1,
            [4] = 1,
            [5] = 1,
            [6] = 1
        },
        ["Paladin"] = new()
        {
            [2] = 2,
            [3] = 1,
            [4] = 1,
            [5] = 1,
            [6] = 1
        },
        ["Ranger"] = new()
        {
            [2] = 2,
            [3] = 1,
            [4] = 1,
            [5] = 1,
            [6] = 1
        }
    };

    public static int GetSpellPicksForLevel(string className, int level)
    {
        if (!PicksByClassAndLevel.TryGetValue(className, out var byLevel))
        {
            return 0;
        }

        return byLevel.TryGetValue(level, out var picks) ? picks : 0;
    }
}

public sealed class EnemyType
{
    public required string Name { get; init; }
    public required int MaxHp { get; init; }
    public required int Attack { get; init; }
    public required int Defense { get; init; }
    public required int XpReward { get; init; }
    public required Raylib_cs.Color Color { get; init; }
    public CreatureTypeTag CreatureTypes { get; init; } = CreatureTypeTag.Humanoid;
    public Stats SaveStats { get; init; } = new()
    {
        Strength = 10,
        Dexterity = 10,
        Constitution = 10,
        Intelligence = 10,
        Wisdom = 10,
        Charisma = 10
    };
    // D&D 5e fields
    public int ArmorClass { get; init; }   // d20 target number
    public int AttackBonus { get; init; }  // enemy's d20 attack modifier
    public int DamageDice { get; init; }   // damage die sides (4=d4, 6=d6, 8=d8, 12=d12)
    public int DamageBonus { get; init; }  // flat bonus added to damage roll
    public int AttackDiceCount { get; init; } = 1;  // default 1 die
    public string DamageType { get; init; } = "physical"; // elemental damage type (fire/cold/lightning/acid)

    public int GetSaveModifier(StatName stat)
    {
        return (int)Math.Floor((SaveStats.Get(stat) - 10) / 2.0);
    }
}

public static class EnemyTypes
{
    public static readonly Dictionary<string, EnemyType> All = new()
    {
        ["goblin"] = new EnemyType
        {
            Name = "Goblin",
            MaxHp = 8,
            Attack = 3,
            Defense = 1,
            XpReward = 35,
            Color = new Raylib_cs.Color(78, 150, 78, 255),
            SaveStats = new Stats { Strength = 8, Dexterity = 14, Constitution = 10, Intelligence = 10, Wisdom = 8, Charisma = 8 },
            ArmorClass = 13, AttackBonus = 3, DamageDice = 6, DamageBonus = 1
        },
        ["goblin_grunt"] = new EnemyType
        {
            Name = "Goblin Grunt",
            MaxHp = 8,
            Attack = 3,
            Defense = 1,
            XpReward = 34,
            Color = new Raylib_cs.Color(78, 150, 78, 255),
            SaveStats = new Stats { Strength = 8, Dexterity = 14, Constitution = 10, Intelligence = 10, Wisdom = 8, Charisma = 8 },
            ArmorClass = 13, AttackBonus = 3, DamageDice = 6, DamageBonus = 1
        },
        ["goblin_skirmisher"] = new EnemyType
        {
            Name = "Goblin Skirmisher",
            MaxHp = 10,
            Attack = 4,
            Defense = 1,
            XpReward = 44,
            Color = new Raylib_cs.Color(92, 164, 102, 255),
            SaveStats = new Stats { Strength = 10, Dexterity = 15, Constitution = 10, Intelligence = 10, Wisdom = 9, Charisma = 8 },
            ArmorClass = 14, AttackBonus = 4, DamageDice = 6, DamageBonus = 1
        },
        ["goblin_slinger"] = new EnemyType
        {
            Name = "Goblin Slinger",
            MaxHp = 7,
            Attack = 4,
            Defense = 1,
            XpReward = 48,
            Color = new Raylib_cs.Color(104, 168, 118, 255),
            SaveStats = new Stats { Strength = 8, Dexterity = 15, Constitution = 10, Intelligence = 10, Wisdom = 10, Charisma = 8 },
            ArmorClass = 12, AttackBonus = 4, DamageDice = 4, DamageBonus = 1
        },
        ["goblin_supervisor"] = new EnemyType
        {
            Name = "Goblin Supervisor",
            MaxHp = 30,
            Attack = 9,
            Defense = 2,
            XpReward = 96,
            Color = new Raylib_cs.Color(130, 180, 92, 255),
            SaveStats = new Stats { Strength = 11, Dexterity = 13, Constitution = 12, Intelligence = 11, Wisdom = 10, Charisma = 10 },
            ArmorClass = 15, AttackBonus = 5, DamageDice = 8, DamageBonus = 2
        },
        ["goblin_general"] = new EnemyType
        {
            Name = "Goblin General",
            MaxHp = 84,
            Attack = 16,
            Defense = 4,
            XpReward = 410,
            Color = new Raylib_cs.Color(190, 92, 70, 255),
            SaveStats = new Stats { Strength = 16, Dexterity = 12, Constitution = 14, Intelligence = 11, Wisdom = 12, Charisma = 12 },
            ArmorClass = 18, AttackBonus = 8, DamageDice = 8, DamageBonus = 3, AttackDiceCount = 2
        },
        ["warg"] = new EnemyType
        {
            Name = "Warg",
            MaxHp = 18,
            Attack = 7,
            Defense = 2,
            XpReward = 55,
            Color = new Raylib_cs.Color(102, 122, 92, 255),
            CreatureTypes = CreatureTypeTag.Beast,
            SaveStats = new Stats { Strength = 14, Dexterity = 13, Constitution = 13, Intelligence = 6, Wisdom = 12, Charisma = 8 },
            ArmorClass = 13, AttackBonus = 4, DamageDice = 6, DamageBonus = 1, AttackDiceCount = 2
        },
        ["skeleton"] = new EnemyType
        {
            Name = "Skeleton",
            MaxHp = 24,
            Attack = 8,
            Defense = 2,
            XpReward = 65,
            Color = new Raylib_cs.Color(185, 185, 185, 255),
            CreatureTypes = CreatureTypeTag.Undead,
            SaveStats = new Stats { Strength = 10, Dexterity = 14, Constitution = 14, Intelligence = 6, Wisdom = 8, Charisma = 5 },
            ArmorClass = 13, AttackBonus = 4, DamageDice = 6, DamageBonus = 1
        },
        ["cultist"] = new EnemyType
        {
            Name = "Cultist",
            MaxHp = 26,
            Attack = 9,
            Defense = 3,
            XpReward = 80,
            Color = new Raylib_cs.Color(148, 92, 152, 255),
            SaveStats = new Stats { Strength = 10, Dexterity = 11, Constitution = 11, Intelligence = 10, Wisdom = 11, Charisma = 11 },
            ArmorClass = 14, AttackBonus = 5, DamageDice = 8, DamageBonus = 2
        },
        ["shadow_mage"] = new EnemyType
        {
            Name = "Shadow Mage",
            MaxHp = 32,
            Attack = 10,
            Defense = 3,
            XpReward = 95,
            Color = new Raylib_cs.Color(92, 104, 172, 255),
            SaveStats = new Stats { Strength = 8, Dexterity = 12, Constitution = 11, Intelligence = 15, Wisdom = 12, Charisma = 11 },
            ArmorClass = 15, AttackBonus = 5, DamageDice = 8, DamageBonus = 2
        },
        ["ogre"] = new EnemyType
        {
            Name = "Ogre",
            MaxHp = 42,
            Attack = 12,
            Defense = 4,
            XpReward = 130,
            Color = new Raylib_cs.Color(126, 98, 72, 255),
            CreatureTypes = CreatureTypeTag.Giant,
            SaveStats = new Stats { Strength = 19, Dexterity = 8, Constitution = 16, Intelligence = 5, Wisdom = 7, Charisma = 7 },
            ArmorClass = 11, AttackBonus = 5, DamageDice = 8, DamageBonus = 2, AttackDiceCount = 2
        },
        ["troll"] = new EnemyType
        {
            Name = "Troll",
            MaxHp = 54,
            Attack = 14,
            Defense = 5,
            XpReward = 180,
            Color = new Raylib_cs.Color(116, 82, 134, 255),
            CreatureTypes = CreatureTypeTag.Giant,
            SaveStats = new Stats { Strength = 18, Dexterity = 13, Constitution = 18, Intelligence = 7, Wisdom = 9, Charisma = 7 },
            ArmorClass = 15, AttackBonus = 7, DamageDice = 6, DamageBonus = 2, AttackDiceCount = 2
        },
        ["dread_knight"] = new EnemyType
        {
            Name = "Dread Knight",
            MaxHp = 86,
            Attack = 17,
            Defense = 4,
            XpReward = 420,
            Color = new Raylib_cs.Color(188, 72, 80, 255),
            CreatureTypes = CreatureTypeTag.Undead,
            SaveStats = new Stats { Strength = 18, Dexterity = 10, Constitution = 16, Intelligence = 11, Wisdom = 12, Charisma = 14 },
            ArmorClass = 18, AttackBonus = 8, DamageDice = 8, DamageBonus = 3, AttackDiceCount = 2
        }
    };
}

public sealed class Enemy
{
    public Enemy(int x, int y, EnemyType type, int? spawnX = null, int? spawnY = null)
    {
        X = x;
        Y = y;
        SpawnX = spawnX ?? x;
        SpawnY = spawnY ?? y;
        Type = type;
        CurrentHp = type.MaxHp;
    }

    public int X { get; set; }
    public int Y { get; set; }
    public int SpawnX { get; }
    public int SpawnY { get; }
    public EnemyType Type { get; }
    public int CurrentHp { get; set; }
    public List<CombatStatusState> StatusEffects { get; } = new();

    public bool IsAlive => CurrentHp > 0;
}

public static class CombatStatusRules
{
    public static int GetAttackPenalty(IEnumerable<CombatStatusState> statuses)
    {
        var penalty = 0;
        foreach (var status in statuses)
        {
            penalty += status.Kind switch
            {
                CombatStatusKind.Weakened => status.Potency,
                CombatStatusKind.Cursed => status.Potency,
                CombatStatusKind.Chilled => status.Potency,
                CombatStatusKind.Shocked => status.Potency,
                CombatStatusKind.Blinded => status.Potency,
                CombatStatusKind.Slowed => status.Potency,
                CombatStatusKind.Feared => status.Potency,
                CombatStatusKind.Restrained => status.Potency,
                CombatStatusKind.Prone => status.Potency,
                _ => 0
            };
        }

        return Math.Max(0, penalty);
    }

    public static int GetMovePenalty(IEnumerable<CombatStatusState> statuses)
    {
        var penalty = 0;
        foreach (var status in statuses)
        {
            penalty += status.Kind switch
            {
                CombatStatusKind.Chilled => status.Potency,
                CombatStatusKind.Slowed => 1 + status.Potency,
                CombatStatusKind.Feared => status.Potency,
                CombatStatusKind.Prone => status.Potency,
                _ => 0
            };
        }

        return Math.Max(0, penalty);
    }

    public static int GetIncomingDamageBonus(IEnumerable<CombatStatusState> statuses)
    {
        return Math.Max(0, statuses
            .Where(status =>
                status.Kind == CombatStatusKind.Marked ||
                status.Kind == CombatStatusKind.Corroded ||
                status.Kind == CombatStatusKind.Restrained ||
                status.Kind == CombatStatusKind.Paralyzed ||
                status.Kind == CombatStatusKind.Prone ||
                status.Kind == CombatStatusKind.Cursed)
            .Sum(status => status.Kind switch
            {
                CombatStatusKind.Corroded => Math.Max(1, status.Potency / 2),
                CombatStatusKind.Paralyzed => Math.Max(1, status.Potency + 1),
                _ => status.Potency
            }));
    }

    public static bool PreventsEnemyAction(IEnumerable<CombatStatusState> statuses)
    {
        return statuses.Any(status =>
            status.Kind == CombatStatusKind.Stunned ||
            status.Kind == CombatStatusKind.Paralyzed ||
            status.Kind == CombatStatusKind.Incapacitated);
    }

    public static bool PreventsEnemyMovement(IEnumerable<CombatStatusState> statuses)
    {
        return statuses.Any(status =>
            status.Kind == CombatStatusKind.Rooted ||
            status.Kind == CombatStatusKind.Restrained ||
            status.Kind == CombatStatusKind.Stunned ||
            status.Kind == CombatStatusKind.Paralyzed ||
            status.Kind == CombatStatusKind.Incapacitated);
    }

    public static bool ForcesEnemyRetreat(IEnumerable<CombatStatusState> statuses)
    {
        return statuses.Any(status => status.Kind == CombatStatusKind.Feared);
    }

    public static bool LimitsEnemyAttackRangeToMelee(IEnumerable<CombatStatusState> statuses)
    {
        return statuses.Any(status => status.Kind == CombatStatusKind.Blinded);
    }
}
