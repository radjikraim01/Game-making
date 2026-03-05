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
    CombatItemMenu,
    CharacterMenu,
    LevelUp,
    FeatSelection,
    SpellSelection,
    SkillSelection,
    RewardChoice,
    PauseMenu,
    VictoryScreen,
    DeathScreen
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
    Dwarf
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
    public required Stats BaseStats { get; init; }
}

public static class CharacterClasses
{
    public static readonly IReadOnlyList<CharacterClass> All = new List<CharacterClass>
    {
        new()
        {
            Name = "Warrior",
            Description = "A master of arms, strong and resilient.",
            BaseStats = new Stats
            {
                Strength = 16,
                Dexterity = 12,
                Constitution = 14,
                Intelligence = 8,
                Wisdom = 10,
                Charisma = 10
            }
        },
        new()
        {
            Name = "Rogue",
            Description = "A nimble skirmisher, quick and perceptive.",
            BaseStats = new Stats
            {
                Strength = 10,
                Dexterity = 16,
                Constitution = 12,
                Intelligence = 14,
                Wisdom = 8,
                Charisma = 10
            }
        },
        new()
        {
            Name = "Mage",
            Description = "A scholarly spellcaster who commands arcane energies.",
            BaseStats = new Stats
            {
                Strength = 8,
                Dexterity = 12,
                Constitution = 10,
                Intelligence = 16,
                Wisdom = 14,
                Charisma = 10
            }
        },
        new()
        {
            Name = "Paladin",
            Description = "A holy warrior armored in faith, equally at home with sword and prayer.",
            BaseStats = new Stats
            {
                Strength = 15,
                Dexterity = 10,
                Constitution = 13,
                Intelligence = 8,
                Wisdom = 12,
                Charisma = 14
            }
        },
        new()
        {
            Name = "Ranger",
            Description = "A hunter of the wilds, precise and patient, deadly at any range.",
            BaseStats = new Stats
            {
                Strength = 12,
                Dexterity = 15,
                Constitution = 12,
                Intelligence = 10,
                Wisdom = 14,
                Charisma = 9
            }
        },
        new()
        {
            Name = "Cleric",
            Description = "A divine conduit who heals allies and smites evil with holy power.",
            BaseStats = new Stats
            {
                Strength = 12,
                Dexterity = 8,
                Constitution = 14,
                Intelligence = 10,
                Wisdom = 16,
                Charisma = 12
            }
        },
        new()
        {
            Name = "Barbarian",
            Description = "A primal warrior who flies into a rage, shrugging off pain to deal brutal damage.",
            BaseStats = new Stats
            {
                Strength = 17,
                Dexterity = 13,
                Constitution = 16,
                Intelligence = 7,
                Wisdom = 9,
                Charisma = 8
            }
        },
        new()
        {
            Name = "Bard",
            Description = "A silver-tongued performer whose music bends reality and beguiles foes.",
            BaseStats = new Stats
            {
                Strength = 9,
                Dexterity = 14,
                Constitution = 11,
                Intelligence = 12,
                Wisdom = 10,
                Charisma = 16
            }
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
        new() { Id = "meditation", Name = "Meditation", Description = "Stillness refills your inner reservoir.", Effect = "+5 Max Mana (scales with WIS)", ScalingStat = StatName.Wisdom },
        new() { Id = "brute_force", Name = "Brute Force", Description = "Raw power behind every swing.", Effect = "+2 Melee Damage (scales with STR)", ScalingStat = StatName.Strength },
        new() { Id = "iron_skin", Name = "Iron Skin", Description = "Blows glance off your hardened hide.", Effect = "+1 Defense (scales with CON)", ScalingStat = StatName.Constitution },
        new() { Id = "war_cry", Name = "War Cry", Description = "Your battle shout rattles enemies before the fight begins.", Effect = "+3 First-Strike Damage (scales with STR)", ScalingStat = StatName.Strength },
        new() { Id = "second_wind", Name = "Second Wind", Description = "Once per combat, rally and restore HP.", Effect = "Heal 10 HP in combat (scales with CON)", ScalingStat = StatName.Constitution },
        new() { Id = "eagle_eye", Name = "Eagle Eye", Description = "You find every gap in an enemy's guard.", Effect = "+2% Crit Chance (scales with DEX)", ScalingStat = StatName.Dexterity },
        new() { Id = "shadowstep", Name = "Shadowstep", Description = "You vanish into shadow when things go wrong.", Effect = "+10% Flee Chance (scales with DEX)", ScalingStat = StatName.Dexterity },
        new() { Id = "swift_strikes", Name = "Swift Strikes", Description = "Speed lets you squeeze in an extra blow.", Effect = "Bonus attack roll each turn (scales with DEX)", ScalingStat = StatName.Dexterity },
        new() { Id = "poison_blade", Name = "Poison Blade", Description = "Your weapon drips with slow-acting venom.", Effect = "+1 Poison damage per turn (scales with DEX)", ScalingStat = StatName.Dexterity },
        new() { Id = "arcane_surge", Name = "Arcane Surge", Description = "Channel raw magic through your strikes.", Effect = "+3 Magic Damage (scales with INT)", ScalingStat = StatName.Intelligence },
        new() { Id = "mana_shield", Name = "Mana Shield", Description = "Burn mana to absorb incoming damage.", Effect = "Spend 3 MP -> absorb 5 damage (scales with INT)", ScalingStat = StatName.Intelligence },
        new() { Id = "inspire", Name = "Inspire", Description = "Your presence sharpens every strike.", Effect = "+2 All Damage (scales with CHA)", ScalingStat = StatName.Charisma }
    };
}

public sealed class FeatDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Effect { get; init; }
    public int MeleeDamageBonus { get; init; }
    public int SpellDamageBonus { get; init; }
    public int DefenseBonus { get; init; }
    public int CritChanceBonus { get; init; }
    public int FleeChanceBonus { get; init; }
    public int MaxHpPerLevelBonus { get; init; }
    public int MaxHpFlatBonus { get; init; }
    public int MaxManaBonus { get; init; }
    public int SpellArmorBypassBonus { get; init; }
    public int MinLevel { get; init; } = 1;
    public bool RequiresCasterClass { get; init; }
    public IReadOnlyList<string> RequiredFeatIds { get; init; } = Array.Empty<string>();
    public string? PrerequisiteText { get; init; }
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
        // Armor training family.
        new() { Id = "light_armor_training_feat", Name = "Light Armor Training", Description = "You learn to move and fight in light armor.", Effect = "Allows light armor; +1 defense and +2% flee while wearing light armor" },
        new() { Id = "medium_armor_training_feat", Name = "Medium Armor Training", Description = "You can bear medium armor without losing combat rhythm.", Effect = "Allows medium armor; +2 defense while wearing medium armor", PrerequisiteText = "Requires light armor training (class or feat)." },
        new() { Id = "heavy_armor_training_feat", Name = "Heavy Armor Training", Description = "You train to endure and exploit heavy plate.", Effect = "Allows heavy armor; +3 defense while wearing heavy armor", PrerequisiteText = "Requires medium armor training (class or feat)." },
        new() { Id = "unarmored_defense_feat", Name = "Unarmored Defense", Description = "You are hardest to pin down without armor.", Effect = "When unarmored: +2 defense and +8% flee chance" },

        // Core baseline feats.
        new() { Id = "tough_feat", Name = "Tough", Description = "Your hit point maximum increases dramatically.", Effect = "+2 max HP per level", MaxHpPerLevelBonus = 2 },
        new() { Id = "martial_adept_feat", Name = "Martial Adept", Description = "You are exceptionally trained in weapon offense.", Effect = "+2 melee damage", MeleeDamageBonus = 2 },
        new() { Id = "savage_attacker_feat", Name = "Savage Attacker", Description = "Your strikes hit with brutal force.", Effect = "+3 melee damage", MeleeDamageBonus = 3, RequiredFeatIds = new[] { "martial_adept_feat" }, PrerequisiteText = "Requires Martial Adept." },
        new() { Id = "defensive_duelist_feat", Name = "Defensive Duelist", Description = "You parry and deflect incoming blows.", Effect = "+2 defense", DefenseBonus = 2 },
        new() { Id = "battle_hardened_feat", Name = "Battle Hardened", Description = "Hard combat has forged your resilience.", Effect = "+1 defense, +1 max HP per level", DefenseBonus = 1, MaxHpPerLevelBonus = 1, RequiredFeatIds = new[] { "tough_feat" }, PrerequisiteText = "Requires Tough." },
        new() { Id = "mobile_feat", Name = "Mobile", Description = "You move quickly and evade danger.", Effect = "+15% flee chance", FleeChanceBonus = 15 },
        new() { Id = "alert_feat", Name = "Alert", Description = "You act with heightened awareness.", Effect = "+5% crit chance", CritChanceBonus = 5 },
        new() { Id = "lucky_feat", Name = "Lucky", Description = "Fortune favors your attempts.", Effect = "+3% crit chance, +5% flee chance", CritChanceBonus = 3, FleeChanceBonus = 5, RequiredFeatIds = new[] { "alert_feat" }, PrerequisiteText = "Requires Alert." },
        new() { Id = "war_caster_feat", Name = "War Caster", Description = "Your spellcasting is battle-ready.", Effect = "+2 spell damage", SpellDamageBonus = 2, RequiresCasterClass = true, PrerequisiteText = "Requires a spellcasting class." },
        new() { Id = "arcane_mind_feat", Name = "Arcane Mind", Description = "Your magical training deepens your focus.", Effect = "+1 spell damage, +4 max mana", SpellDamageBonus = 1, MaxManaBonus = 4, RequiresCasterClass = true, PrerequisiteText = "Requires a spellcasting class." },
        new() { Id = "elemental_adept_feat", Name = "Elemental Adept", Description = "Your elemental spells punch through defenses.", Effect = "Spells ignore +1 armor", SpellArmorBypassBonus = 1, RequiresCasterClass = true, RequiredFeatIds = new[] { "arcane_mind_feat" }, PrerequisiteText = "Requires Arcane Mind and a spellcasting class." },
        new() { Id = "resilient_feat", Name = "Resilient", Description = "You gain broad physical and mental fortitude.", Effect = "+1 Constitution, +1 Wisdom" },

        // Weapon offense and pressure.
        new() { Id = "weapon_focus_feat", Name = "Weapon Focus", Description = "You refine your form with your favored weapon.", Effect = "+1 melee damage", MeleeDamageBonus = 1 },
        new() { Id = "power_attack_feat", Name = "Power Attack", Description = "You put raw force behind each committed strike.", Effect = "+2 melee damage", MeleeDamageBonus = 2, RequiredFeatIds = new[] { "weapon_focus_feat" }, PrerequisiteText = "Requires Weapon Focus." },
        new() { Id = "relentless_strikes_feat", Name = "Relentless Strikes", Description = "You chain attacks with predatory timing.", Effect = "+1 melee damage, +2% crit chance", MeleeDamageBonus = 1, CritChanceBonus = 2, RequiredFeatIds = new[] { "power_attack_feat" }, PrerequisiteText = "Requires Power Attack." },
        new() { Id = "brutal_finish_feat", Name = "Brutal Finish", Description = "You capitalize on weakened enemies with ruthless precision.", Effect = "+2 melee damage, +2% crit chance", MeleeDamageBonus = 2, CritChanceBonus = 2, RequiredFeatIds = new[] { "power_attack_feat" }, PrerequisiteText = "Requires Power Attack." },
        new() { Id = "twin_cut_feat", Name = "Twin Cut", Description = "Quick follow-through turns openings into deep wounds.", Effect = "+1 melee damage, +3% crit chance", MeleeDamageBonus = 1, CritChanceBonus = 3 },
        new() { Id = "bloodthirst_feat", Name = "Bloodthirst", Description = "Aggression feeds your momentum in battle.", Effect = "+2 melee damage, +3% flee chance", MeleeDamageBonus = 2, FleeChanceBonus = 3 },
        new() { Id = "crushing_blow_feat", Name = "Crushing Blow", Description = "Heavy hits break stance and confidence alike.", Effect = "+2 melee damage, +1 defense", MeleeDamageBonus = 2, DefenseBonus = 1 },
        new() { Id = "duelist_precision_feat", Name = "Duelist Precision", Description = "Measured angles turn defense into offense.", Effect = "+1 melee damage, +2% crit chance, +1 defense", MeleeDamageBonus = 1, CritChanceBonus = 2, DefenseBonus = 1, RequiredFeatIds = new[] { "defensive_duelist_feat" }, PrerequisiteText = "Requires Defensive Duelist." },
        new() { Id = "vanguard_assault_feat", Name = "Vanguard Assault", Description = "You press forward with offensive discipline.", Effect = "+2 melee damage, +1 defense", MeleeDamageBonus = 2, DefenseBonus = 1 },
        new() { Id = "feral_momentum_feat", Name = "Feral Momentum", Description = "Wild tempo lets you hit hard then disengage fast.", Effect = "+2 melee damage, +5% flee chance", MeleeDamageBonus = 2, FleeChanceBonus = 5 },
        new() { Id = "exposed_opening_feat", Name = "Exposed Opening", Description = "You punish every mistake with ruthless accuracy.", Effect = "+1 melee damage, +2% crit chance", MeleeDamageBonus = 1, CritChanceBonus = 2 },
        new() { Id = "lethal_tempo_feat", Name = "Lethal Tempo", Description = "You flow between attack and repositioning effortlessly.", Effect = "+2 melee damage, +3% flee chance", MeleeDamageBonus = 2, FleeChanceBonus = 3, RequiredFeatIds = new[] { "mobile_feat" }, PrerequisiteText = "Requires Mobile." },
        new() { Id = "riposte_master_feat", Name = "Riposte Master", Description = "Countering your opponent becomes second nature.", Effect = "+2 melee damage, +1 defense", MeleeDamageBonus = 2, DefenseBonus = 1, RequiredFeatIds = new[] { "defensive_duelist_feat" }, PrerequisiteText = "Requires Defensive Duelist." },

        // Defense and survival.
        new() { Id = "stone_skin_feat", Name = "Stone Skin", Description = "Your guard becomes harder to break.", Effect = "+1 defense", DefenseBonus = 1 },
        new() { Id = "guardian_instinct_feat", Name = "Guardian Instinct", Description = "Protective instincts sharpen your reactions.", Effect = "+1 defense, +2% crit chance", DefenseBonus = 1, CritChanceBonus = 2 },
        new() { Id = "iron_resolve_feat", Name = "Iron Resolve", Description = "You hold firm when others would collapse.", Effect = "+1 defense, +1 max HP per level", DefenseBonus = 1, MaxHpPerLevelBonus = 1, RequiredFeatIds = new[] { "stone_skin_feat" }, PrerequisiteText = "Requires Stone Skin." },
        new() { Id = "endurance_feat", Name = "Endurance", Description = "Your stamina carries you through long fights.", Effect = "+1 max HP per level", MaxHpPerLevelBonus = 1 },
        new() { Id = "thick_hide_feat", Name = "Thick Hide", Description = "Repeated punishment hardens your body.", Effect = "+1 defense, +1 max HP per level", DefenseBonus = 1, MaxHpPerLevelBonus = 1, RequiredFeatIds = new[] { "endurance_feat" }, PrerequisiteText = "Requires Endurance." },
        new() { Id = "veteran_toughness_feat", Name = "Veteran Toughness", Description = "Campaign scars leave you harder to kill.", Effect = "+2 max HP per level", MaxHpPerLevelBonus = 2, RequiredFeatIds = new[] { "tough_feat" }, PrerequisiteText = "Requires Tough." },
        new() { Id = "battle_medic_feat", Name = "Battle Medic", Description = "Pragmatic triage keeps you in the fight.", Effect = "+8 max HP", MaxHpFlatBonus = 8 },
        new() { Id = "warded_soul_feat", Name = "Warded Soul", Description = "Your spirit reinforces body and mind alike.", Effect = "+1 defense, +4 max mana", DefenseBonus = 1, MaxManaBonus = 4 },
        new() { Id = "last_stand_feat", Name = "Last Stand", Description = "You become stronger the longer you endure.", Effect = "+2 defense, +1 max HP per level", DefenseBonus = 2, MaxHpPerLevelBonus = 1, RequiredFeatIds = new[] { "iron_resolve_feat" }, PrerequisiteText = "Requires Iron Resolve." },

        // Crit and precision specialization.
        new() { Id = "keen_edge_feat", Name = "Keen Edge", Description = "You seek decisive strikes at every exchange.", Effect = "+2% crit chance", CritChanceBonus = 2 },
        new() { Id = "precision_master_feat", Name = "Precision Master", Description = "Your placement turns solid hits into lethal ones.", Effect = "+3% crit chance", CritChanceBonus = 3, RequiredFeatIds = new[] { "keen_edge_feat" }, PrerequisiteText = "Requires Keen Edge." },
        new() { Id = "opportunist_feat", Name = "Opportunist", Description = "You exploit fleeting opportunities and disengage safely.", Effect = "+2% crit chance, +5% flee chance", CritChanceBonus = 2, FleeChanceBonus = 5 },
        new() { Id = "executioner_eye_feat", Name = "Executioner Eye", Description = "You read fatal lines in every defense.", Effect = "+4% crit chance", CritChanceBonus = 4, RequiredFeatIds = new[] { "precision_master_feat" }, PrerequisiteText = "Requires Precision Master." },
        new() { Id = "deadeye_feat", Name = "Deadeye", Description = "Focused targeting boosts both precision and impact.", Effect = "+1 melee damage, +3% crit chance", MeleeDamageBonus = 1, CritChanceBonus = 3 },
        new() { Id = "opening_strike_feat", Name = "Opening Strike", Description = "You start exchanges with calculated pressure.", Effect = "+1 defense, +2% crit chance", DefenseBonus = 1, CritChanceBonus = 2 },
        new() { Id = "momentum_killer_feat", Name = "Momentum Killer", Description = "You break enemy rhythm with punishing counters.", Effect = "+2 melee damage, +2% crit chance", MeleeDamageBonus = 2, CritChanceBonus = 2, RequiredFeatIds = new[] { "deadeye_feat" }, PrerequisiteText = "Requires Deadeye." },

        // Mobility and disengage control.
        new() { Id = "fleet_footed_feat", Name = "Fleet-Footed", Description = "You reposition quickly when pressure spikes.", Effect = "+8% flee chance", FleeChanceBonus = 8 },
        new() { Id = "escape_artist_feat", Name = "Escape Artist", Description = "You slip free even when enemies think you trapped.", Effect = "+10% flee chance", FleeChanceBonus = 10, RequiredFeatIds = new[] { "fleet_footed_feat" }, PrerequisiteText = "Requires Fleet-Footed." },
        new() { Id = "shadow_runner_feat", Name = "Shadow Runner", Description = "You blend evasive movement with strike timing.", Effect = "+6% flee chance, +1% crit chance", FleeChanceBonus = 6, CritChanceBonus = 1 },
        new() { Id = "evasive_footwork_feat", Name = "Evasive Footwork", Description = "Your step patterns deflect hostile pressure.", Effect = "+5% flee chance, +1 defense", FleeChanceBonus = 5, DefenseBonus = 1 },
        new() { Id = "hit_and_run_feat", Name = "Hit and Run", Description = "You strike and vanish before retaliation lands.", Effect = "+1 melee damage, +6% flee chance", MeleeDamageBonus = 1, FleeChanceBonus = 6, RequiredFeatIds = new[] { "mobile_feat" }, PrerequisiteText = "Requires Mobile." },
        new() { Id = "ghost_step_feat", Name = "Ghost Step", Description = "You move through danger with uncanny calm.", Effect = "+7% flee chance, +1 defense", FleeChanceBonus = 7, DefenseBonus = 1, RequiredFeatIds = new[] { "fleet_footed_feat" }, PrerequisiteText = "Requires Fleet-Footed." },
        new() { Id = "pathfinder_feat", Name = "Pathfinder", Description = "You always find a route through chaos.", Effect = "+4% flee chance, +1% crit chance", FleeChanceBonus = 4, CritChanceBonus = 1 },
        new() { Id = "slippery_target_feat", Name = "Slippery Target", Description = "Enemies struggle to lock you down.", Effect = "+6% flee chance, +1 defense", FleeChanceBonus = 6, DefenseBonus = 1, RequiredFeatIds = new[] { "evasive_footwork_feat" }, PrerequisiteText = "Requires Evasive Footwork." },

        // Caster specialization.
        new() { Id = "spell_focus_feat", Name = "Spell Focus", Description = "You sharpen control over your spellcraft.", Effect = "+1 spell damage", SpellDamageBonus = 1, RequiresCasterClass = true, PrerequisiteText = "Requires a spellcasting class." },
        new() { Id = "greater_spell_focus_feat", Name = "Greater Spell Focus", Description = "Your spell precision grows deadlier.", Effect = "+2 spell damage", SpellDamageBonus = 2, RequiresCasterClass = true, RequiredFeatIds = new[] { "spell_focus_feat" }, PrerequisiteText = "Requires Spell Focus and a spellcasting class." },
        new() { Id = "arcane_reservoir_feat", Name = "Arcane Reservoir", Description = "You expand your internal mana reserve.", Effect = "+6 max mana", MaxManaBonus = 6, RequiresCasterClass = true, PrerequisiteText = "Requires a spellcasting class." },
        new() { Id = "deep_reservoir_feat", Name = "Deep Reservoir", Description = "You draw from a deeper well of power.", Effect = "+8 max mana", MaxManaBonus = 8, RequiresCasterClass = true, RequiredFeatIds = new[] { "arcane_reservoir_feat" }, PrerequisiteText = "Requires Arcane Reservoir and a spellcasting class." },
        new() { Id = "piercing_magic_feat", Name = "Piercing Magic", Description = "Your spells push through hardened defenses.", Effect = "Spells ignore +1 armor", SpellArmorBypassBonus = 1, RequiresCasterClass = true, RequiredFeatIds = new[] { "spell_focus_feat" }, PrerequisiteText = "Requires Spell Focus and a spellcasting class." },
        new() { Id = "devastating_magic_feat", Name = "Devastating Magic", Description = "Your high-commitment casts hit with extreme force.", Effect = "+2 spell damage, spells ignore +1 armor", SpellDamageBonus = 2, SpellArmorBypassBonus = 1, RequiresCasterClass = true, RequiredFeatIds = new[] { "greater_spell_focus_feat" }, PrerequisiteText = "Requires Greater Spell Focus and a spellcasting class." },
        new() { Id = "warded_caster_feat", Name = "Warded Caster", Description = "You maintain output while holding a guarded stance.", Effect = "+1 spell damage, +1 defense", SpellDamageBonus = 1, DefenseBonus = 1, RequiresCasterClass = true, PrerequisiteText = "Requires a spellcasting class." },
        new() { Id = "swift_channeling_feat", Name = "Swift Channeling", Description = "Fast channeling keeps your escape windows open.", Effect = "+1 spell damage, +3% flee chance", SpellDamageBonus = 1, FleeChanceBonus = 3, RequiresCasterClass = true, PrerequisiteText = "Requires a spellcasting class." },
        new() { Id = "mind_over_matter_feat", Name = "Mind Over Matter", Description = "Mental control reinforces your body under pressure.", Effect = "+1 spell damage, +1 defense, +4 max mana", SpellDamageBonus = 1, DefenseBonus = 1, MaxManaBonus = 4, RequiresCasterClass = true, RequiredFeatIds = new[] { "arcane_mind_feat" }, PrerequisiteText = "Requires Arcane Mind and a spellcasting class." },
        new() { Id = "stable_weave_feat", Name = "Stable Weave", Description = "Refined spell forms reduce misplays and improve finish windows.", Effect = "+1 spell damage, +2% crit chance", SpellDamageBonus = 1, CritChanceBonus = 2, RequiresCasterClass = true, PrerequisiteText = "Requires a spellcasting class." },
        new() { Id = "ritual_mastery_feat", Name = "Ritual Mastery", Description = "Disciplined preparation enhances sustained casting.", Effect = "+1 spell damage, +4 max mana", SpellDamageBonus = 1, MaxManaBonus = 4, RequiresCasterClass = true, PrerequisiteText = "Requires a spellcasting class." },
        new() { Id = "elemental_savant_feat", Name = "Elemental Savant", Description = "You shape elemental forces with practiced certainty.", Effect = "+1 spell damage, spells ignore +1 armor", SpellDamageBonus = 1, SpellArmorBypassBonus = 1, RequiresCasterClass = true, RequiredFeatIds = new[] { "elemental_adept_feat" }, PrerequisiteText = "Requires Elemental Adept and a spellcasting class." },
        new() { Id = "battle_conduit_feat", Name = "Battle Conduit", Description = "You convert melee pressure into casting rhythm.", Effect = "+1 melee damage, +1 spell damage", MeleeDamageBonus = 1, SpellDamageBonus = 1, RequiresCasterClass = true, PrerequisiteText = "Requires a spellcasting class." },
        new() { Id = "mana_ward_feat", Name = "Mana Ward", Description = "Arcane shielding reinforces your battlefield posture.", Effect = "+6 max mana, +1 defense", MaxManaBonus = 6, DefenseBonus = 1, RequiresCasterClass = true, PrerequisiteText = "Requires a spellcasting class." }
    };

    public static readonly IReadOnlyDictionary<string, FeatDefinition> ById = All
        .ToDictionary(feat => feat.Id, feat => feat, StringComparer.Ordinal);
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
    public bool SuppressCounterAttack { get; init; }

    public bool IsCantrip => SpellLevel == 0;
    public bool RequiresSlot => SpellLevel > 0;
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
        ["mage_fire_bolt"] = new() { Id = "mage_fire_bolt", Name = "Fire Bolt", ClassName = "Mage", SpellLevel = 0, Description = "A bolt of fire streaks toward a foe.", ScalingStat = StatName.Intelligence, BaseDamage = 7, Variance = 3, ArmorBypass = 1, DamageTag = "fire", SuppressCounterAttack = false },
        ["mage_ray_of_frost"] = new() { Id = "mage_ray_of_frost", Name = "Ray of Frost", ClassName = "Mage", SpellLevel = 0, Description = "A freezing beam slows and stings the target.", ScalingStat = StatName.Intelligence, BaseDamage = 6, Variance = 3, ArmorBypass = 1, DamageTag = "cold", SuppressCounterAttack = false },
        ["mage_chill_touch"] = new() { Id = "mage_chill_touch", Name = "Chill Touch", ClassName = "Mage", SpellLevel = 0, Description = "Necrotic grasp weakens life force.", ScalingStat = StatName.Intelligence, BaseDamage = 6, Variance = 4, ArmorBypass = 1, DamageTag = "necrotic", SuppressCounterAttack = false },
        ["mage_shocking_grasp"] = new() { Id = "mage_shocking_grasp", Name = "Shocking Grasp", ClassName = "Mage", SpellLevel = 0, Description = "A crackling touch jolts your enemy.", ScalingStat = StatName.Intelligence, BaseDamage = 7, Variance = 2, ArmorBypass = 2, DamageTag = "lightning", SuppressCounterAttack = true },
        ["mage_acid_splash"] = new() { Id = "mage_acid_splash", Name = "Acid Splash", ClassName = "Mage", SpellLevel = 0, Description = "Corrosive droplets burn through weak armor seams.", ScalingStat = StatName.Intelligence, BaseDamage = 6, Variance = 3, ArmorBypass = 1, DamageTag = "acid", SuppressCounterAttack = false },
        ["mage_magic_missile"] = new() { Id = "mage_magic_missile", Name = "Magic Missile", ClassName = "Mage", SpellLevel = 1, Description = "Reliable force darts strike true.", ScalingStat = StatName.Intelligence, BaseDamage = 10, Variance = 4, ArmorBypass = 1, DamageTag = "force", SuppressCounterAttack = false },
        ["mage_burning_hands"] = new() { Id = "mage_burning_hands", Name = "Burning Hands", ClassName = "Mage", SpellLevel = 1, Description = "A fan of flame scorches foes.", ScalingStat = StatName.Intelligence, BaseDamage = 11, Variance = 5, ArmorBypass = 1, DamageTag = "fire", SuppressCounterAttack = false },
        ["mage_chromatic_orb"] = new() { Id = "mage_chromatic_orb", Name = "Chromatic Orb", ClassName = "Mage", SpellLevel = 1, Description = "An orb of elemental power slams into the target.", ScalingStat = StatName.Intelligence, BaseDamage = 12, Variance = 6, ArmorBypass = 1, DamageTag = "elemental", SuppressCounterAttack = false },
        ["mage_ice_knife"] = new() { Id = "mage_ice_knife", Name = "Ice Knife", ClassName = "Mage", SpellLevel = 1, Description = "A frozen shard shatters into piercing frost.", ScalingStat = StatName.Intelligence, BaseDamage = 11, Variance = 5, ArmorBypass = 1, DamageTag = "cold", SuppressCounterAttack = false },
        ["mage_scorching_ray"] = new() { Id = "mage_scorching_ray", Name = "Scorching Ray", ClassName = "Mage", SpellLevel = 2, Description = "Focused rays of fire strike repeatedly.", ScalingStat = StatName.Intelligence, BaseDamage = 14, Variance = 6, ArmorBypass = 1, DamageTag = "fire", SuppressCounterAttack = false },
        ["mage_shatter"] = new() { Id = "mage_shatter", Name = "Shatter", ClassName = "Mage", SpellLevel = 2, Description = "A concussive burst fractures defenses.", ScalingStat = StatName.Intelligence, BaseDamage = 14, Variance = 5, ArmorBypass = 2, DamageTag = "thunder", SuppressCounterAttack = false },
        ["mage_web"] = new() { Id = "mage_web", Name = "Web", ClassName = "Mage", SpellLevel = 2, Description = "Sticky strands restrain and interrupt.", ScalingStat = StatName.Intelligence, BaseDamage = 10, Variance = 3, ArmorBypass = 0, DamageTag = "arcane", SuppressCounterAttack = true },
        ["mage_melfs_acid_arrow"] = new() { Id = "mage_melfs_acid_arrow", Name = "Melf's Acid Arrow", ClassName = "Mage", SpellLevel = 2, Description = "A streaking arrow of acid chews through defenses.", ScalingStat = StatName.Intelligence, BaseDamage = 15, Variance = 6, ArmorBypass = 2, DamageTag = "acid", SuppressCounterAttack = false },
        ["mage_fireball"] = new() { Id = "mage_fireball", Name = "Fireball", ClassName = "Mage", SpellLevel = 3, Description = "A roaring explosion engulfs your foe.", ScalingStat = StatName.Intelligence, BaseDamage = 19, Variance = 8, ArmorBypass = 2, DamageTag = "fire", SuppressCounterAttack = false },
        ["mage_lightning_bolt"] = new() { Id = "mage_lightning_bolt", Name = "Lightning Bolt", ClassName = "Mage", SpellLevel = 3, Description = "A line of lightning tears through defenses.", ScalingStat = StatName.Intelligence, BaseDamage = 18, Variance = 8, ArmorBypass = 3, DamageTag = "lightning", SuppressCounterAttack = false },
        ["mage_tidal_wave"] = new() { Id = "mage_tidal_wave", Name = "Tidal Wave", ClassName = "Mage", SpellLevel = 3, Description = "A crashing surge slams enemies off balance.", ScalingStat = StatName.Intelligence, BaseDamage = 17, Variance = 7, ArmorBypass = 2, DamageTag = "water", SuppressCounterAttack = false },

        // Cleric
        ["cleric_sacred_flame"] = new() { Id = "cleric_sacred_flame", Name = "Sacred Flame", ClassName = "Cleric", SpellLevel = 0, Description = "Radiant fire descends from above.", ScalingStat = StatName.Wisdom, BaseDamage = 7, Variance = 3, ArmorBypass = 1, DamageTag = "radiant", SuppressCounterAttack = false },
        ["cleric_toll_the_dead"] = new() { Id = "cleric_toll_the_dead", Name = "Toll the Dead", ClassName = "Cleric", SpellLevel = 0, Description = "A dreadful bell drains spirit and flesh.", ScalingStat = StatName.Wisdom, BaseDamage = 7, Variance = 4, ArmorBypass = 1, DamageTag = "necrotic", SuppressCounterAttack = false },
        ["cleric_word_of_radiance"] = new() { Id = "cleric_word_of_radiance", Name = "Word of Radiance", ClassName = "Cleric", SpellLevel = 0, Description = "A burst of sacred light sears nearby evil.", ScalingStat = StatName.Wisdom, BaseDamage = 6, Variance = 3, ArmorBypass = 1, DamageTag = "radiant", SuppressCounterAttack = false },
        ["cleric_guiding_bolt"] = new() { Id = "cleric_guiding_bolt", Name = "Guiding Bolt", ClassName = "Cleric", SpellLevel = 1, Description = "A beam of divine radiance strikes true.", ScalingStat = StatName.Wisdom, BaseDamage = 11, Variance = 5, ArmorBypass = 1, DamageTag = "radiant", SuppressCounterAttack = false },
        ["cleric_inflict_wounds"] = new() { Id = "cleric_inflict_wounds", Name = "Inflict Wounds", ClassName = "Cleric", SpellLevel = 1, Description = "Necrotic energy rots the enemy body.", ScalingStat = StatName.Wisdom, BaseDamage = 12, Variance = 6, ArmorBypass = 1, DamageTag = "necrotic", SuppressCounterAttack = false },
        ["cleric_command"] = new() { Id = "cleric_command", Name = "Command", ClassName = "Cleric", SpellLevel = 1, Description = "A divine word stuns the target will.", ScalingStat = StatName.Wisdom, BaseDamage = 8, Variance = 3, ArmorBypass = 0, DamageTag = "psychic", SuppressCounterAttack = true },
        ["cleric_bane"] = new() { Id = "cleric_bane", Name = "Bane", ClassName = "Cleric", SpellLevel = 1, Description = "A baleful prayer weakens the enemy's resolve.", ScalingStat = StatName.Wisdom, BaseDamage = 9, Variance = 3, ArmorBypass = 0, DamageTag = "psychic", SuppressCounterAttack = true },
        ["cleric_spiritual_weapon"] = new() { Id = "cleric_spiritual_weapon", Name = "Spiritual Weapon", ClassName = "Cleric", SpellLevel = 2, Description = "A spectral weapon slams into your foe.", ScalingStat = StatName.Wisdom, BaseDamage = 15, Variance = 6, ArmorBypass = 1, DamageTag = "force", SuppressCounterAttack = false },
        ["cleric_hold_person"] = new() { Id = "cleric_hold_person", Name = "Hold Person", ClassName = "Cleric", SpellLevel = 2, Description = "Divine force attempts to paralyze the foe.", ScalingStat = StatName.Wisdom, BaseDamage = 10, Variance = 3, ArmorBypass = 0, DamageTag = "radiant", SuppressCounterAttack = true },
        ["cleric_blindness"] = new() { Id = "cleric_blindness", Name = "Blindness", ClassName = "Cleric", SpellLevel = 2, Description = "A curse of darkness robs the target of sight.", ScalingStat = StatName.Wisdom, BaseDamage = 11, Variance = 4, ArmorBypass = 1, DamageTag = "necrotic", SuppressCounterAttack = true },
        ["cleric_spirit_guardians"] = new() { Id = "cleric_spirit_guardians", Name = "Spirit Guardians", ClassName = "Cleric", SpellLevel = 3, Description = "Holy spirits lash enemies around you.", ScalingStat = StatName.Wisdom, BaseDamage = 18, Variance = 8, ArmorBypass = 2, DamageTag = "radiant", SuppressCounterAttack = false },
        ["cleric_bestow_curse"] = new() { Id = "cleric_bestow_curse", Name = "Bestow Curse", ClassName = "Cleric", SpellLevel = 3, Description = "A potent curse weakens body and mind.", ScalingStat = StatName.Wisdom, BaseDamage = 16, Variance = 6, ArmorBypass = 2, DamageTag = "necrotic", SuppressCounterAttack = true },
        ["cleric_flame_strike"] = new() { Id = "cleric_flame_strike", Name = "Flame Strike", ClassName = "Cleric", SpellLevel = 3, Description = "Divine fire descends in a searing column.", ScalingStat = StatName.Wisdom, BaseDamage = 19, Variance = 8, ArmorBypass = 2, DamageTag = "fire", SuppressCounterAttack = false },

        // Bard
        ["bard_vicious_mockery"] = new() { Id = "bard_vicious_mockery", Name = "Vicious Mockery", ClassName = "Bard", SpellLevel = 0, Description = "Insulting magic tears at an enemy psyche.", ScalingStat = StatName.Charisma, BaseDamage = 6, Variance = 3, ArmorBypass = 0, DamageTag = "psychic", SuppressCounterAttack = true },
        ["bard_thunderclap"] = new() { Id = "bard_thunderclap", Name = "Thunderclap", ClassName = "Bard", SpellLevel = 0, Description = "A sharp clap of thunder rattles foes.", ScalingStat = StatName.Charisma, BaseDamage = 7, Variance = 3, ArmorBypass = 1, DamageTag = "thunder", SuppressCounterAttack = false },
        ["bard_mind_sliver"] = new() { Id = "bard_mind_sliver", Name = "Mind Sliver", ClassName = "Bard", SpellLevel = 0, Description = "A psychic spike punctures enemy concentration.", ScalingStat = StatName.Charisma, BaseDamage = 6, Variance = 3, ArmorBypass = 1, DamageTag = "psychic", SuppressCounterAttack = true },
        ["bard_dissonant_whispers"] = new() { Id = "bard_dissonant_whispers", Name = "Dissonant Whispers", ClassName = "Bard", SpellLevel = 1, Description = "Psychic whispers unmake enemy focus.", ScalingStat = StatName.Charisma, BaseDamage = 10, Variance = 5, ArmorBypass = 0, DamageTag = "psychic", SuppressCounterAttack = false },
        ["bard_thunderwave"] = new() { Id = "bard_thunderwave", Name = "Thunderwave", ClassName = "Bard", SpellLevel = 1, Description = "A wave of thunder knocks composure loose.", ScalingStat = StatName.Charisma, BaseDamage = 10, Variance = 5, ArmorBypass = 1, DamageTag = "thunder", SuppressCounterAttack = true },
        ["bard_hideous_laughter"] = new() { Id = "bard_hideous_laughter", Name = "Hideous Laughter", ClassName = "Bard", SpellLevel = 1, Description = "Uncontrollable laughter leaves the foe exposed.", ScalingStat = StatName.Charisma, BaseDamage = 9, Variance = 4, ArmorBypass = 0, DamageTag = "psychic", SuppressCounterAttack = true },
        ["bard_shatter"] = new() { Id = "bard_shatter", Name = "Shatter", ClassName = "Bard", SpellLevel = 2, Description = "A shattering note cracks armor and bone.", ScalingStat = StatName.Charisma, BaseDamage = 14, Variance = 6, ArmorBypass = 1, DamageTag = "thunder", SuppressCounterAttack = false },
        ["bard_heat_metal"] = new() { Id = "bard_heat_metal", Name = "Heat Metal", ClassName = "Bard", SpellLevel = 2, Description = "You superheat gear and sear your foe.", ScalingStat = StatName.Charisma, BaseDamage = 13, Variance = 5, ArmorBypass = 2, DamageTag = "fire", SuppressCounterAttack = false },
        ["bard_cloud_of_daggers"] = new() { Id = "bard_cloud_of_daggers", Name = "Cloud of Daggers", ClassName = "Bard", SpellLevel = 2, Description = "Whirling blades shred the target repeatedly.", ScalingStat = StatName.Charisma, BaseDamage = 13, Variance = 5, ArmorBypass = 2, DamageTag = "force", SuppressCounterAttack = false },
        ["bard_hypnotic_pattern"] = new() { Id = "bard_hypnotic_pattern", Name = "Hypnotic Pattern", ClassName = "Bard", SpellLevel = 3, Description = "A mesmerizing pattern overwhelms the target.", ScalingStat = StatName.Charisma, BaseDamage = 14, Variance = 5, ArmorBypass = 2, DamageTag = "psychic", SuppressCounterAttack = true },
        ["bard_fear"] = new() { Id = "bard_fear", Name = "Fear", ClassName = "Bard", SpellLevel = 3, Description = "Primal dread crushes enemy resolve.", ScalingStat = StatName.Charisma, BaseDamage = 15, Variance = 6, ArmorBypass = 2, DamageTag = "psychic", SuppressCounterAttack = true },
        ["bard_slow"] = new() { Id = "bard_slow", Name = "Slow", ClassName = "Bard", SpellLevel = 3, Description = "Temporal drag crushes enemy momentum.", ScalingStat = StatName.Charisma, BaseDamage = 14, Variance = 5, ArmorBypass = 1, DamageTag = "arcane", SuppressCounterAttack = true },

        // Paladin
        ["paladin_searing_smite"] = new() { Id = "paladin_searing_smite", Name = "Searing Smite", ClassName = "Paladin", SpellLevel = 1, Description = "Your weapon ignites with holy flame.", ScalingStat = StatName.Charisma, BaseDamage = 12, Variance = 5, ArmorBypass = 1, DamageTag = "fire", SuppressCounterAttack = false },
        ["paladin_thunderous_smite"] = new() { Id = "paladin_thunderous_smite", Name = "Thunderous Smite", ClassName = "Paladin", SpellLevel = 1, Description = "A thunderous strike shakes your foe.", ScalingStat = StatName.Charisma, BaseDamage = 12, Variance = 5, ArmorBypass = 2, DamageTag = "thunder", SuppressCounterAttack = true },
        ["paladin_wrathful_smite"] = new() { Id = "paladin_wrathful_smite", Name = "Wrathful Smite", ClassName = "Paladin", SpellLevel = 1, Description = "A wrathful blow rends body and spirit.", ScalingStat = StatName.Charisma, BaseDamage = 11, Variance = 5, ArmorBypass = 1, DamageTag = "psychic", SuppressCounterAttack = false },
        ["paladin_divine_favor"] = new() { Id = "paladin_divine_favor", Name = "Divine Favor", ClassName = "Paladin", SpellLevel = 1, Description = "Sacred might empowers your attack with radiant force.", ScalingStat = StatName.Charisma, BaseDamage = 11, Variance = 4, ArmorBypass = 1, DamageTag = "radiant", SuppressCounterAttack = false },
        ["paladin_branding_smite"] = new() { Id = "paladin_branding_smite", Name = "Branding Smite", ClassName = "Paladin", SpellLevel = 2, Description = "Radiant force brands your foe.", ScalingStat = StatName.Charisma, BaseDamage = 15, Variance = 6, ArmorBypass = 2, DamageTag = "radiant", SuppressCounterAttack = false },
        ["paladin_magic_weapon"] = new() { Id = "paladin_magic_weapon", Name = "Magic Weapon", ClassName = "Paladin", SpellLevel = 2, Description = "Your strike channels sharpened magic.", ScalingStat = StatName.Charisma, BaseDamage = 14, Variance = 5, ArmorBypass = 2, DamageTag = "force", SuppressCounterAttack = false },
        ["paladin_aura_of_vitality"] = new() { Id = "paladin_aura_of_vitality", Name = "Aura of Vitality", ClassName = "Paladin", SpellLevel = 2, Description = "A pulse of holy vitality batters hostile spirits.", ScalingStat = StatName.Charisma, BaseDamage = 14, Variance = 5, ArmorBypass = 1, DamageTag = "radiant", SuppressCounterAttack = false },
        ["paladin_blinding_smite"] = new() { Id = "paladin_blinding_smite", Name = "Blinding Smite", ClassName = "Paladin", SpellLevel = 3, Description = "A sacred burst can blind and overwhelm.", ScalingStat = StatName.Charisma, BaseDamage = 18, Variance = 7, ArmorBypass = 2, DamageTag = "radiant", SuppressCounterAttack = true },
        ["paladin_crusaders_mantle"] = new() { Id = "paladin_crusaders_mantle", Name = "Crusader's Mantle", ClassName = "Paladin", SpellLevel = 3, Description = "Radiant aura crashes down with righteous force.", ScalingStat = StatName.Charisma, BaseDamage = 17, Variance = 6, ArmorBypass = 2, DamageTag = "radiant", SuppressCounterAttack = false },

        // Ranger
        ["ranger_hunters_mark"] = new() { Id = "ranger_hunters_mark", Name = "Hunters Mark", ClassName = "Ranger", SpellLevel = 1, Description = "Mark prey and strike with deadly precision.", ScalingStat = StatName.Wisdom, BaseDamage = 11, Variance = 5, ArmorBypass = 1, DamageTag = "piercing", SuppressCounterAttack = false },
        ["ranger_hail_of_thorns"] = new() { Id = "ranger_hail_of_thorns", Name = "Hail of Thorns", ClassName = "Ranger", SpellLevel = 1, Description = "Thorny fragments burst around your target.", ScalingStat = StatName.Wisdom, BaseDamage = 11, Variance = 5, ArmorBypass = 1, DamageTag = "piercing", SuppressCounterAttack = false },
        ["ranger_ensnaring_strike"] = new() { Id = "ranger_ensnaring_strike", Name = "Ensnaring Strike", ClassName = "Ranger", SpellLevel = 1, Description = "Vines lash out and bind your enemy.", ScalingStat = StatName.Wisdom, BaseDamage = 10, Variance = 4, ArmorBypass = 0, DamageTag = "nature", SuppressCounterAttack = true },
        ["ranger_cordon_of_arrows"] = new() { Id = "ranger_cordon_of_arrows", Name = "Cordon of Arrows", ClassName = "Ranger", SpellLevel = 1, Description = "A ring of spectral arrows erupts around the target.", ScalingStat = StatName.Wisdom, BaseDamage = 10, Variance = 4, ArmorBypass = 1, DamageTag = "piercing", SuppressCounterAttack = false },
        ["ranger_spike_growth"] = new() { Id = "ranger_spike_growth", Name = "Spike Growth", ClassName = "Ranger", SpellLevel = 2, Description = "Razor sharp growth tears at the target.", ScalingStat = StatName.Wisdom, BaseDamage = 14, Variance = 6, ArmorBypass = 1, DamageTag = "piercing", SuppressCounterAttack = false },
        ["ranger_zephyr_strike"] = new() { Id = "ranger_zephyr_strike", Name = "Zephyr Strike", ClassName = "Ranger", SpellLevel = 2, Description = "Wind laced movement creates a punishing strike.", ScalingStat = StatName.Wisdom, BaseDamage = 13, Variance = 5, ArmorBypass = 2, DamageTag = "force", SuppressCounterAttack = false },
        ["ranger_pass_without_trace"] = new() { Id = "ranger_pass_without_trace", Name = "Pass Without Trace", ClassName = "Ranger", SpellLevel = 2, Description = "Shrouding shadows empower a lethal opening strike.", ScalingStat = StatName.Wisdom, BaseDamage = 12, Variance = 4, ArmorBypass = 1, DamageTag = "nature", SuppressCounterAttack = true },
        ["ranger_lightning_arrow"] = new() { Id = "ranger_lightning_arrow", Name = "Lightning Arrow", ClassName = "Ranger", SpellLevel = 3, Description = "A charged shot detonates on impact.", ScalingStat = StatName.Wisdom, BaseDamage = 18, Variance = 7, ArmorBypass = 2, DamageTag = "lightning", SuppressCounterAttack = false },
        ["ranger_conjure_barrage"] = new() { Id = "ranger_conjure_barrage", Name = "Conjure Barrage", ClassName = "Ranger", SpellLevel = 3, Description = "A fan of conjured projectiles shreds the enemy line.", ScalingStat = StatName.Wisdom, BaseDamage = 17, Variance = 7, ArmorBypass = 2, DamageTag = "piercing", SuppressCounterAttack = false },
        ["ranger_flame_arrows"] = new() { Id = "ranger_flame_arrows", Name = "Flame Arrows", ClassName = "Ranger", SpellLevel = 3, Description = "Ignited volleys strike with searing precision.", ScalingStat = StatName.Wisdom, BaseDamage = 16, Variance = 6, ArmorBypass = 2, DamageTag = "fire", SuppressCounterAttack = false }
    };

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
            (5, "mage_fireball"),
            (5, "mage_lightning_bolt"),
            (5, "mage_tidal_wave")
        },
        ["Cleric"] = new()
        {
            (1, "cleric_sacred_flame"),
            (1, "cleric_toll_the_dead"),
            (1, "cleric_word_of_radiance"),
            (1, "cleric_guiding_bolt"),
            (1, "cleric_inflict_wounds"),
            (1, "cleric_command"),
            (1, "cleric_bane"),
            (3, "cleric_spiritual_weapon"),
            (3, "cleric_hold_person"),
            (3, "cleric_blindness"),
            (5, "cleric_spirit_guardians"),
            (5, "cleric_bestow_curse"),
            (5, "cleric_flame_strike")
        },
        ["Bard"] = new()
        {
            (1, "bard_vicious_mockery"),
            (1, "bard_thunderclap"),
            (1, "bard_mind_sliver"),
            (1, "bard_dissonant_whispers"),
            (1, "bard_thunderwave"),
            (1, "bard_hideous_laughter"),
            (3, "bard_shatter"),
            (3, "bard_heat_metal"),
            (3, "bard_cloud_of_daggers"),
            (5, "bard_hypnotic_pattern"),
            (5, "bard_fear"),
            (5, "bard_slow")
        },
        ["Paladin"] = new()
        {
            (2, "paladin_searing_smite"),
            (2, "paladin_thunderous_smite"),
            (2, "paladin_wrathful_smite"),
            (2, "paladin_divine_favor"),
            (5, "paladin_branding_smite"),
            (5, "paladin_magic_weapon"),
            (5, "paladin_aura_of_vitality"),
            (6, "paladin_blinding_smite"),
            (6, "paladin_crusaders_mantle")
        },
        ["Ranger"] = new()
        {
            (2, "ranger_hunters_mark"),
            (2, "ranger_hail_of_thorns"),
            (2, "ranger_ensnaring_strike"),
            (2, "ranger_cordon_of_arrows"),
            (5, "ranger_spike_growth"),
            (5, "ranger_zephyr_strike"),
            (5, "ranger_pass_without_trace"),
            (6, "ranger_lightning_arrow"),
            (6, "ranger_conjure_barrage"),
            (6, "ranger_flame_arrows")
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
}

public static class EnemyTypes
{
    public static readonly Dictionary<string, EnemyType> All = new()
    {
        ["goblin"] = new EnemyType
        {
            Name = "Goblin",
            MaxHp = 16,
            Attack = 5,
            Defense = 1,
            XpReward = 35,
            Color = new Raylib_cs.Color(78, 150, 78, 255)
        },
        ["warg"] = new EnemyType
        {
            Name = "Warg",
            MaxHp = 22,
            Attack = 7,
            Defense = 2,
            XpReward = 55,
            Color = new Raylib_cs.Color(102, 122, 92, 255)
        },
        ["skeleton"] = new EnemyType
        {
            Name = "Skeleton",
            MaxHp = 24,
            Attack = 8,
            Defense = 2,
            XpReward = 65,
            Color = new Raylib_cs.Color(185, 185, 185, 255)
        },
        ["cultist"] = new EnemyType
        {
            Name = "Cultist",
            MaxHp = 26,
            Attack = 9,
            Defense = 3,
            XpReward = 80,
            Color = new Raylib_cs.Color(148, 92, 152, 255)
        },
        ["shadow_mage"] = new EnemyType
        {
            Name = "Shadow Mage",
            MaxHp = 32,
            Attack = 10,
            Defense = 3,
            XpReward = 95,
            Color = new Raylib_cs.Color(92, 104, 172, 255)
        },
        ["ogre"] = new EnemyType
        {
            Name = "Ogre",
            MaxHp = 42,
            Attack = 12,
            Defense = 4,
            XpReward = 130,
            Color = new Raylib_cs.Color(126, 98, 72, 255)
        },
        ["troll"] = new EnemyType
        {
            Name = "Troll",
            MaxHp = 54,
            Attack = 14,
            Defense = 5,
            XpReward = 180,
            Color = new Raylib_cs.Color(116, 82, 134, 255)
        },
        ["dread_knight"] = new EnemyType
        {
            Name = "Dread Knight",
            MaxHp = 86,
            Attack = 17,
            Defense = 4,
            XpReward = 420,
            Color = new Raylib_cs.Color(188, 72, 80, 255)
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

    public bool IsAlive => CurrentHp > 0;
}
