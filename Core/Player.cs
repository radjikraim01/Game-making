using Raylib_cs;

namespace DungeonEscape.Core;

public sealed class Player
{
    private readonly HashSet<string> _skillIds = new();
    private readonly HashSet<string> _featIds = new();
    private readonly HashSet<string> _knownSpellIds = new();
    private readonly int[] _spellSlotsMax = new int[4];
    private readonly int[] _spellSlotsCurrent = new int[4];

    public static readonly Dictionary<Gender, Dictionary<StatName, int>> GenderModifiers = new()
    {
        [Gender.Male] = new Dictionary<StatName, int>
        {
            [StatName.Strength] = 1,
            [StatName.Constitution] = 1
        },
        [Gender.Female] = new Dictionary<StatName, int>
        {
            [StatName.Dexterity] = 1,
            [StatName.Wisdom] = 1
        }
    };

    public static readonly Dictionary<Race, Dictionary<StatName, int>> RaceBonuses = new()
    {
        [Race.Human] = new Dictionary<StatName, int>
        {
            [StatName.Strength] = 1,
            [StatName.Dexterity] = 1,
            [StatName.Constitution] = 1,
            [StatName.Intelligence] = 1,
            [StatName.Wisdom] = 1,
            [StatName.Charisma] = 1
        },
        [Race.Elf] = new Dictionary<StatName, int>
        {
            [StatName.Dexterity] = 2,
            [StatName.Intelligence] = 1
        },
        [Race.Dwarf] = new Dictionary<StatName, int>
        {
            [StatName.Constitution] = 2,
            [StatName.Wisdom] = 1
        },
        [Race.HalfOrc] = new Dictionary<StatName, int>
        {
            [StatName.Strength] = 2,
            [StatName.Constitution] = 1
        },
        [Race.Halfling] = new Dictionary<StatName, int>
        {
            [StatName.Dexterity] = 2,
            [StatName.Charisma] = 1
        },
        [Race.Gnome] = new Dictionary<StatName, int>
        {
            [StatName.Intelligence] = 2,
            [StatName.Constitution] = 1
        },
        [Race.Tiefling] = new Dictionary<StatName, int>
        {
            [StatName.Charisma] = 2,
            [StatName.Intelligence] = 1
        }
    };

    // D&D-like early game curve (first 6 levels): 300, 900, 2700, 6500, 14000 cumulative XP.
    private static readonly int[] CumulativeXpByLevel =
    {
        0,      // unused
        0,      // level 1
        300,    // level 2
        900,    // level 3
        2700,   // level 4
        6500,   // level 5
        14000   // level 6
    };

    public Player(int x, int y, CharacterClass characterClass, string name, Gender gender, Race race = Race.Human)
    {
        X = x;
        Y = y;
        CharacterClass = characterClass;
        Name = name;
        Gender = gender;
        Race = race;
        Stats = new Stats
        {
            Strength = 10,
            Dexterity = 10,
            Constitution = 10,
            Intelligence = 10,
            Wisdom = 10,
            Charisma = 10
        };

        foreach (var kv in GenderModifiers[gender])
        {
            Stats.Add(kv.Key, kv.Value);
        }

        if (RaceBonuses.TryGetValue(race, out var raceMods))
        {
            foreach (var kv in raceMods)
            {
                Stats.Add(kv.Key, kv.Value);
            }
        }

        Level = 1;
        Xp = 0;
        XpToNextLevel = GetXpToNextLevel(Level);
        StatPoints = 0;
        FeatPoints = 0;
        SpellPickPoints = SpellProgression.GetSpellPicksForLevel(CharacterClass.Name, Level);
        Skills = new List<Skill>();
        Feats = new List<FeatDefinition>();
        SyncSpellSlots(fullRestore: true);
        EnsureFreeCantripsKnown();

        MaxHp = CalcMaxHp();
        CurrentHp = MaxHp;

        // Auto-grant level 1 class talents
        GrantClassTalentsForLevel(1);
    }

    public int X { get; set; }
    public int Y { get; set; }
    public CharacterClass CharacterClass { get; }
    public string Name { get; }
    public Gender Gender { get; }
    public Race Race { get; }
    public Stats Stats { get; }
    public int Level { get; private set; }
    public int Xp { get; private set; }
    public int XpToNextLevel { get; private set; }
    public int StatPoints { get; private set; }
    public int FeatPoints { get; private set; }
    public int SpellPickPoints { get; private set; }
    public List<Skill> Skills { get; }
    public List<FeatDefinition> Feats { get; }
    public int MaxHp { get; private set; }
    public int CurrentHp { get; set; }
    public bool HasUsedSecondWind { get; set; }

    public bool IsAlive => CurrentHp > 0;

    public int Mod(StatName stat)
    {
        return (int)Math.Floor((Stats.Get(stat) - 10) / 2.0);
    }

    public int ProficiencyBonus => Math.Clamp(2 + Math.Max(0, (Level - 1) / 4), 2, 6);

    public bool IsProficientInSave(StatName stat)
        => CharacterClass.SaveProficiencies.Contains(stat)
           || Feats.Any(f => f.GrantsSaveProficiency == stat);

    public int GetSaveBonus(StatName stat)
        => Mod(stat) + (IsProficientInSave(stat) ? ProficiencyBonus : 0);

    public int GetArmorClass(ArmorCategory armorCategory, int shieldBonus = 0)
    {
        var dexMod = Mod(StatName.Dexterity);
        int baseAC, maxDexBonus;
        switch (armorCategory)
        {
            case ArmorCategory.Light:
                baseAC = 11; maxDexBonus = 99; break;
            case ArmorCategory.Medium:
                baseAC = 14; maxDexBonus = 2; break;
            case ArmorCategory.Heavy:
                baseAC = 18; maxDexBonus = 0; break;
            default: // Unarmored
                baseAC = 10; maxDexBonus = 99;
                if (HasFeat("unarmored_defense_feat")) baseAC += 2;
                break;
        }
        var effectiveDex = Math.Min(dexMod, maxDexBonus);
        return baseAC + effectiveDex + DefenseBonus + shieldBonus;
    }

    public bool HasSkill(string id) => _skillIds.Contains(id);
    public bool HasFeat(string id) => _featIds.Contains(id);
    public bool KnowsSpell(string id) => _knownSpellIds.Contains(id);
    public bool IsCasterClass => SpellData.SpellSlotsByClass.ContainsKey(CharacterClass.Name);

    public StatName CastingStat => CharacterClass.Name switch
    {
        "Mage"    => StatName.Intelligence,
        "Cleric"  => StatName.Wisdom,
        "Ranger"  => StatName.Wisdom,
        "Bard"    => StatName.Charisma,
        "Paladin" => StatName.Charisma,
        _         => StatName.Intelligence
    };

    public void AddFeatPoints(int amount)
    {
        if (amount == 0) return;
        FeatPoints = Math.Max(0, FeatPoints + amount);
    }

    private int SumFeatBonus(Func<FeatDefinition, int> selector)
    {
        if (Feats.Count == 0) return 0;
        return Feats.Sum(selector);
    }

    public int SpellDamageBonus => SumFeatBonus(feat => feat.SpellDamageBonus);
    public int SpellArmorBypassBonus => SumFeatBonus(feat => feat.SpellArmorBypassBonus);
    public int InitiativeBonus => SumFeatBonus(feat => feat.InitiativeBonus);
    public int HealingBonus => SumFeatBonus(feat => feat.HealingBonus);

    public int GetSpellSlots(int spellLevel)
    {
        if (spellLevel < 1 || spellLevel > 3) return 0;
        return _spellSlotsCurrent[spellLevel];
    }

    public int GetSpellSlotsMax(int spellLevel)
    {
        if (spellLevel < 1 || spellLevel > 3) return 0;
        return _spellSlotsMax[spellLevel];
    }

    public bool TryConsumeSpellSlot(int spellLevel)
    {
        if (spellLevel < 1 || spellLevel > 3) return false;
        if (_spellSlotsCurrent[spellLevel] <= 0) return false;
        _spellSlotsCurrent[spellLevel] -= 1;
        return true;
    }

    public void RestoreSpellSlot(int spellLevel, int amount = 1)
    {
        if (spellLevel < 1 || spellLevel > 3) return;
        _spellSlotsCurrent[spellLevel] = Math.Min(_spellSlotsMax[spellLevel], _spellSlotsCurrent[spellLevel] + amount);
    }

    public IReadOnlyList<SpellDefinition> GetKnownSpells()
    {
        return _knownSpellIds
            .Where(SpellData.ById.ContainsKey)
            .Select(id => SpellData.ById[id])
            .Where(SpellData.IsPlayerVisible)
            .OrderBy(spell => SpellData.GetCombatFamilySortOrder(spell))
            .ThenBy(spell => spell.SpellLevel)
            .ThenBy(spell => spell.Name)
            .ToList();
    }

    public IReadOnlyList<SpellDefinition> GetClassSpells()
    {
        if (!SpellData.ClassSpellUnlocks.TryGetValue(CharacterClass.Name, out var unlocks))
        {
            return Array.Empty<SpellDefinition>();
        }

        return unlocks
            .Select(unlock => unlock.SpellId)
            .Distinct()
            .Where(SpellData.ById.ContainsKey)
            .Select(id => SpellData.ById[id])
            .Where(SpellData.IsPlayerVisible)
            .OrderBy(spell => SpellData.GetCombatFamilySortOrder(spell))
            .ThenBy(spell => spell.SpellLevel)
            .ThenBy(spell => spell.Name)
            .ToList();
    }

    public int GetSpellUnlockLevel(string spellId)
    {
        if (!SpellData.ClassSpellUnlocks.TryGetValue(CharacterClass.Name, out var unlocks))
        {
            return int.MaxValue;
        }

        var level = unlocks
            .Where(unlock => string.Equals(unlock.SpellId, spellId, StringComparison.Ordinal))
            .Select(unlock => unlock.MinLevel)
            .DefaultIfEmpty(int.MaxValue)
            .Min();

        return level;
    }

    public bool CanLearnSpell(SpellDefinition spell, out string reason)
    {
        reason = string.Empty;

        if (!string.Equals(spell.ClassName, CharacterClass.Name, StringComparison.Ordinal))
        {
            reason = "Not a spell for your class.";
            return false;
        }

        if (!SpellData.IsPlayerVisible(spell))
        {
            reason = "Archived spell.";
            return false;
        }

        if (_knownSpellIds.Contains(spell.Id))
        {
            reason = "Already learned.";
            return false;
        }

        var unlockLevel = GetSpellUnlockLevel(spell.Id);
        if (unlockLevel == int.MaxValue)
        {
            reason = "Unavailable for this class progression.";
            return false;
        }

        if (Level < unlockLevel)
        {
            reason = $"Unlocks at level {unlockLevel}.";
            return false;
        }

        if (SpellPickPoints <= 0)
        {
            reason = "No spell picks remaining.";
            return false;
        }

        return true;
    }

    public IReadOnlyList<SpellDefinition> GetLearnableSpells()
    {
        return GetClassSpells()
            .Where(spell => CanLearnSpell(spell, out _))
            .ToList();
    }

    public bool LearnSpell(SpellDefinition spell)
    {
        if (!CanLearnSpell(spell, out _))
        {
            return false;
        }

        _knownSpellIds.Add(spell.Id);
        SpellPickPoints -= 1;
        return true;
    }

    public int MeleeDamageBonus
    {
        get
        {
            var bonus = 0;
            if (HasSkill("brute_force")) bonus += 2 + Math.Max(0, (int)Math.Floor(Mod(StatName.Strength) * 0.5));
            if (HasSkill("arcane_surge")) bonus += 3 + Math.Max(0, Mod(CastingStat));
            if (HasSkill("inspire")) bonus += 2 + Math.Max(0, (int)Math.Floor(Mod(StatName.Charisma) * 0.5));
            bonus += SumFeatBonus(feat => feat.MeleeDamageBonus);
            return bonus;
        }
    }

    public int WarCryBonus => HasSkill("war_cry") ? 3 + Math.Max(0, (int)Math.Floor(Mod(StatName.Strength) * 0.5)) : 0;

    public int ExpandedCritRange
    {
        get
        {
            var bonus = 0;
            if (HasSkill("eagle_eye")) bonus += 1;
            bonus += SumFeatBonus(feat => feat.CritRangeBonus);
            return bonus;
        }
    }

    public int CritThreshold => Math.Max(2, 20 - ExpandedCritRange);

    public int DefenseBonus
    {
        get
        {
            var bonus = 0;
            if (HasSkill("iron_skin")) bonus += 1 + Math.Max(0, (int)Math.Floor(Mod(StatName.Constitution) * 0.5));
            bonus += SumFeatBonus(feat => feat.DefenseBonus);
            // Aura of Protection: Paladin feat — CHA modifier added to defense permanently
            if (HasFeat("paladin_aura_protection_feat")) bonus += Math.Max(0, Mod(StatName.Charisma));
            return bonus;
        }
    }

    public int FleeBonus
    {
        get
        {
            var bonus = 0;
            if (HasSkill("shadowstep")) bonus += 10 + Math.Max(0, Mod(StatName.Dexterity));
            bonus += SumFeatBonus(feat => feat.FleeChanceBonus);
            return bonus;
        }
    }

    public int PoisonDamage => HasSkill("poison_blade") ? 1 + Math.Max(0, (int)Math.Floor(Mod(StatName.Dexterity) * 0.5)) : 0;
    public int SecondWindHeal => 10 + Math.Max(0, Mod(StatName.Constitution));
    public int ArcaneWardAbsorb => 5 + Math.Max(0, Mod(CastingStat));
    public bool HasBonusAttack => HasSkill("swift_strikes");
    public int ChannelDivinityBonus => Math.Max(1, Mod(StatName.Wisdom));
    public int BlessedHealerBonus => Math.Max(0, Mod(StatName.Wisdom));
    public int LayOnHandsHeal => Level * 3;
    public int HuntersInstinctBonus => HasSkill("hunters_instinct") ? 2 : 0;

    public double XpMultiplier
    {
        get
        {
            if (!HasSkill("fast_learner")) return 1.0;
            var intBonus = Math.Max(0, Mod(StatName.Intelligence)) * 0.02;
            return 1.1 + intBonus;
        }
    }

    public bool GainXp(int amount)
    {
        var final = (int)Math.Floor(amount * XpMultiplier);
        Xp += final;

        var leveled = false;
        while (Xp >= XpToNextLevel)
        {
            LevelUp();
            leveled = true;
        }

        return leveled;
    }

    public bool IncreaseStat(StatName stat)
    {
        if (StatPoints <= 0) return false;

        Stats.Add(stat, 1);
        StatPoints -= 1;
        RecalcMaxStats();
        return true;
    }

    public bool RefundAllocatedStatPoint(StatName stat)
    {
        if (Stats.Get(stat) <= 1)
        {
            return false;
        }

        Stats.Add(stat, -1);
        StatPoints += 1;
        RecalcMaxStats();
        return true;
    }

    public void AllocateCreationStatPoint(StatName stat)
    {
        Stats.Add(stat, 1);
        RecalcMaxStats();
        CurrentHp = MaxHp;
    }

    public void DeallocateCreationStatPoint(StatName stat)
    {
        Stats.Add(stat, -1);
        RecalcMaxStats();
        CurrentHp = MaxHp;
    }

    public void LearnSkill(Skill skill)
    {
        if (_skillIds.Contains(skill.Id)) return;
        _skillIds.Add(skill.Id);
        Skills.Add(skill);
        RecalcMaxStats();
    }

    public void GrantClassTalentsForLevel(int level)
    {
        if (!SkillBook.ClassTalentProgression.TryGetValue(CharacterClass.Name, out var progression)) return;
        foreach (var (minLevel, skillId) in progression)
        {
            if (minLevel != level) continue;
            var skill = SkillBook.All.FirstOrDefault(s => s.Id == skillId);
            if (skill != null) LearnSkill(skill);
        }
    }

    public IReadOnlyList<(int Level, string SkillId, string Name)> GetClassTalentRoadmap()
    {
        if (!SkillBook.ClassTalentProgression.TryGetValue(CharacterClass.Name, out var progression))
            return Array.Empty<(int, string, string)>();
        return progression
            .Select(p => (p.Level, p.SkillId, SkillBook.All.FirstOrDefault(s => s.Id == p.SkillId)?.Name ?? p.SkillId))
            .ToList();
    }

    public bool LearnFeat(FeatDefinition feat)
    {
        if (!CanLearnFeat(feat, out _)) return false;

        _featIds.Add(feat.Id);
        Feats.Add(feat);
        FeatPoints -= 1;

        // Apply direct stat bonuses (e.g. Resilient: +1 CON +1 WIS, Iron Will: +1 CON).
        if (feat.StatBonusStr != 0) Stats.Add(StatName.Strength,     feat.StatBonusStr);
        if (feat.StatBonusDex != 0) Stats.Add(StatName.Dexterity,    feat.StatBonusDex);
        if (feat.StatBonusCon != 0) Stats.Add(StatName.Constitution, feat.StatBonusCon);
        if (feat.StatBonusInt != 0) Stats.Add(StatName.Intelligence, feat.StatBonusInt);
        if (feat.StatBonusWis != 0) Stats.Add(StatName.Wisdom,       feat.StatBonusWis);
        if (feat.StatBonusCha != 0) Stats.Add(StatName.Charisma,     feat.StatBonusCha);

        // Sync spell slots in case this feat grants bonus slots.
        SyncSpellSlots(fullRestore: false);
        RecalcMaxStats();
        return true;
    }

    public bool CanLearnFeat(FeatDefinition feat, out string reason)
    {
        reason = string.Empty;

        if (FeatPoints <= 0)
        {
            reason = "No feat picks remaining.";
            return false;
        }

        if (_featIds.Contains(feat.Id))
        {
            reason = "Already learned.";
            return false;
        }

        if (Level < feat.MinLevel)
        {
            reason = $"Requires level {feat.MinLevel}.";
            return false;
        }

        if (feat.RequiresCasterClass && !IsCasterClass)
        {
            reason = feat.PrerequisiteText ?? "Requires a spellcasting class.";
            return false;
        }

        if (!string.IsNullOrEmpty(feat.RequiredClassName) &&
            !string.Equals(feat.RequiredClassName, CharacterClass.Name, StringComparison.OrdinalIgnoreCase))
        {
            reason = feat.PrerequisiteText ?? $"Requires {feat.RequiredClassName} class.";
            return false;
        }

        if (string.Equals(feat.Id, "light_armor_training_feat", StringComparison.Ordinal))
        {
            if (ArmorTraining.GetEffectiveTrainingRank(CharacterClass.Name, HasFeat) >= ArmorTraining.GetRequiredRank(ArmorCategory.Light))
            {
                reason = "You already have light armor training.";
                return false;
            }
        }
        else if (string.Equals(feat.Id, "medium_armor_training_feat", StringComparison.Ordinal))
        {
            if (!ArmorTraining.HasTrainingForCategory(CharacterClass.Name, HasFeat, ArmorCategory.Light))
            {
                reason = feat.PrerequisiteText ?? "Requires light armor training (class or feat).";
                return false;
            }

            if (ArmorTraining.GetEffectiveTrainingRank(CharacterClass.Name, HasFeat) >= ArmorTraining.GetRequiredRank(ArmorCategory.Medium))
            {
                reason = "You already have medium armor training.";
                return false;
            }
        }
        else if (string.Equals(feat.Id, "heavy_armor_training_feat", StringComparison.Ordinal))
        {
            if (!ArmorTraining.HasTrainingForCategory(CharacterClass.Name, HasFeat, ArmorCategory.Medium))
            {
                reason = feat.PrerequisiteText ?? "Requires medium armor training (class or feat).";
                return false;
            }

            if (ArmorTraining.GetEffectiveTrainingRank(CharacterClass.Name, HasFeat) >= ArmorTraining.GetRequiredRank(ArmorCategory.Heavy))
            {
                reason = "You already have heavy armor training.";
                return false;
            }
        }

        if (feat.RequiredFeatIds.Count > 0)
        {
            var missingIds = feat.RequiredFeatIds
                .Where(id => !_featIds.Contains(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (missingIds.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(feat.PrerequisiteText))
                {
                    reason = feat.PrerequisiteText;
                    return false;
                }

                var missingNames = missingIds
                    .Select(id => FeatBook.ById.TryGetValue(id, out var requiredFeat) ? requiredFeat.Name : id);
                reason = $"Requires: {string.Join(", ", missingNames)}.";
                return false;
            }
        }

        return true;
    }

    public string GetSkillEffectText(Skill skill)
    {
        return skill.Id switch
        {
            "toughness" => $"+{5 + Math.Max(0, Mod(StatName.Constitution))} Max HP",
            "fast_learner" => $"+{(int)Math.Round(XpMultiplier * 100 - 100)}% XP",
            "meditation" => "Recover 1 L1 slot at rest",
            "brute_force" => $"+{2 + Math.Max(0, (int)Math.Floor(Mod(StatName.Strength) * 0.5))} Melee Dmg",
            "war_cry" => $"+{WarCryBonus} First-Strike",
            "iron_skin" => $"+{DefenseBonus} Defense",
            "second_wind" => $"Heal {SecondWindHeal} HP",
            "eagle_eye" => ExpandedCritRange > 0 ? $"Crit on {CritThreshold}-20" : "Crit on 19-20",
            "shadowstep" => $"+{FleeBonus}% Flee",
            "swift_strikes" => "Bonus attack roll",
            "poison_blade" => $"+{PoisonDamage} Poison/turn",
            "arcane_surge" => $"+{3 + Math.Max(0, Mod(CastingStat))} Magic Dmg",
            "mana_shield" => $"Absorb {ArcaneWardAbsorb} dmg (once per combat)",
            "inspire" => $"+{2 + Math.Max(0, (int)Math.Floor(Mod(StatName.Charisma) * 0.5))} All Dmg",
            "channel_divinity" => $"+{ChannelDivinityBonus} divine spell dmg (once per combat)",
            "blessed_healer" => $"+{BlessedHealerBonus} HP per heal",
            "cutting_words" => "Reduce enemy attack by 1d4 (once per combat)",
            "lay_on_hands" => $"Heal {LayOnHandsHeal} HP (once per combat)",
            "hunters_instinct" => "+2 dmg to marked targets",
            _ => skill.Effect
        };
    }

    public string GetFeatEffectText(FeatDefinition feat)
    {
        var parts = new List<string>();
        if (feat.MeleeDamageBonus != 0)    parts.Add($"+{feat.MeleeDamageBonus} Melee Damage");
        if (feat.SpellDamageBonus != 0)    parts.Add($"+{feat.SpellDamageBonus} Spell Damage");
        if (feat.DefenseBonus != 0)        parts.Add($"+{feat.DefenseBonus} Defense");
        if (feat.CritChanceBonus != 0)     parts.Add($"+{feat.CritChanceBonus}% Crit Chance");
        if (feat.FleeChanceBonus != 0)     parts.Add($"+{feat.FleeChanceBonus}% Flee Chance");
        if (feat.SpellArmorBypassBonus != 0) parts.Add($"Spells ignore +{feat.SpellArmorBypassBonus} armor");
        if (feat.MaxHpFlatBonus != 0)      parts.Add($"+{feat.MaxHpFlatBonus} Max HP");
        if (feat.MaxHpPerLevelBonus != 0)  parts.Add($"+{feat.MaxHpPerLevelBonus * Level} Max HP ({feat.MaxHpPerLevelBonus}/level)");
        if (feat.InitiativeBonus != 0)     parts.Add($"+{feat.InitiativeBonus} Initiative");
        if (feat.BonusSpellSlotL1 != 0)   parts.Add($"+{feat.BonusSpellSlotL1} L1 Spell Slot");
        if (feat.HealingBonus != 0)        parts.Add($"+{feat.HealingBonus} Healing");
        if (feat.StatBonusStr != 0)        parts.Add($"+{feat.StatBonusStr} Strength");
        if (feat.StatBonusDex != 0)        parts.Add($"+{feat.StatBonusDex} Dexterity");
        if (feat.StatBonusCon != 0)        parts.Add($"+{feat.StatBonusCon} Constitution");
        if (feat.StatBonusInt != 0)        parts.Add($"+{feat.StatBonusInt} Intelligence");
        if (feat.StatBonusWis != 0)        parts.Add($"+{feat.StatBonusWis} Wisdom");
        if (feat.StatBonusCha != 0)        parts.Add($"+{feat.StatBonusCha} Charisma");
        if (parts.Count > 0)
        {
            return string.Join(", ", parts);
        }

        return feat.Effect;
    }

    public PlayerSnapshot CreateSnapshot()
    {
        return new PlayerSnapshot
        {
            Name = Name,
            ClassName = CharacterClass.Name,
            Gender = Gender,
            Race = Race,
            X = X,
            Y = Y,
            Stats = new StatsSnapshot
            {
                Strength = Stats.Strength,
                Dexterity = Stats.Dexterity,
                Constitution = Stats.Constitution,
                Intelligence = Stats.Intelligence,
                Wisdom = Stats.Wisdom,
                Charisma = Stats.Charisma
            },
            Level = Level,
            Xp = Xp,
            XpToNextLevel = XpToNextLevel,
            StatPoints = StatPoints,
            FeatPoints = FeatPoints,
            SpellPickPoints = SpellPickPoints,
            MaxHp = MaxHp,
            CurrentHp = CurrentHp,
            HasUsedSecondWind = HasUsedSecondWind,
            SkillIds = _skillIds.OrderBy(id => id).ToList(),
            FeatIds = _featIds.OrderBy(id => id).ToList(),
            KnownSpellIds = _knownSpellIds
                .Where(SpellData.IsPlayerVisible)
                .OrderBy(id => id)
                .ToList(),
            SpellSlotsL1Current = _spellSlotsCurrent[1],
            SpellSlotsL2Current = _spellSlotsCurrent[2],
            SpellSlotsL3Current = _spellSlotsCurrent[3]
        };
    }

    public static bool TryFromSnapshot(
        PlayerSnapshot snapshot,
        out Player? player,
        out string errorMessage,
        out int removedArchivedSpellCount,
        out int refundedArchivedSpellPicks)
    {
        player = null;
        errorMessage = string.Empty;
        removedArchivedSpellCount = 0;
        refundedArchivedSpellPicks = 0;

        if (snapshot == null)
        {
            errorMessage = "Player data is missing.";
            return false;
        }

        if (snapshot.Stats == null)
        {
            errorMessage = "Player stats are missing.";
            return false;
        }

        var classDef = CharacterClasses.All.FirstOrDefault(c =>
            string.Equals(c.Name, snapshot.ClassName, StringComparison.OrdinalIgnoreCase));
        if (classDef == null)
        {
            errorMessage = $"Unknown class '{snapshot.ClassName}'.";
            return false;
        }

        var displayName = string.IsNullOrWhiteSpace(snapshot.Name) ? "Adventurer" : snapshot.Name.Trim();
        var restored = new Player(snapshot.X, snapshot.Y, classDef, displayName, snapshot.Gender, snapshot.Race);

        restored.Stats.Strength = snapshot.Stats.Strength;
        restored.Stats.Dexterity = snapshot.Stats.Dexterity;
        restored.Stats.Constitution = snapshot.Stats.Constitution;
        restored.Stats.Intelligence = snapshot.Stats.Intelligence;
        restored.Stats.Wisdom = snapshot.Stats.Wisdom;
        restored.Stats.Charisma = snapshot.Stats.Charisma;

        restored.Level = Math.Max(1, snapshot.Level);
        restored.Xp = Math.Max(0, snapshot.Xp);
        restored.XpToNextLevel = Math.Max(1, snapshot.XpToNextLevel);
        restored.StatPoints = Math.Max(0, snapshot.StatPoints);
        restored.FeatPoints = Math.Max(0, snapshot.FeatPoints);
        restored.SpellPickPoints = Math.Max(0, snapshot.SpellPickPoints);
        restored.HasUsedSecondWind = snapshot.HasUsedSecondWind;

        restored._skillIds.Clear();
        restored.Skills.Clear();
        foreach (var id in (snapshot.SkillIds ?? new List<string>()).Distinct())
        {
            var skill = SkillBook.All.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.Ordinal));
            if (skill == null) continue;
            restored._skillIds.Add(skill.Id);
            restored.Skills.Add(skill);
        }

        restored._featIds.Clear();
        restored.Feats.Clear();
        foreach (var id in (snapshot.FeatIds ?? new List<string>()).Distinct())
        {
            var feat = FeatBook.All.FirstOrDefault(f => string.Equals(f.Id, id, StringComparison.Ordinal));
            if (feat == null) continue;
            restored._featIds.Add(feat.Id);
            restored.Feats.Add(feat);
        }

        restored._knownSpellIds.Clear();
        foreach (var id in (snapshot.KnownSpellIds ?? new List<string>()).Distinct())
        {
            if (!SpellData.ById.TryGetValue(id, out var spell)) continue;
            if (!string.Equals(spell.ClassName, restored.CharacterClass.Name, StringComparison.Ordinal)) continue;
            if (!SpellData.IsPlayerVisible(spell))
            {
                removedArchivedSpellCount += 1;
                if (!spell.IsCantrip)
                {
                    refundedArchivedSpellPicks += 1;
                }

                continue;
            }

            restored._knownSpellIds.Add(spell.Id);
        }

        if (refundedArchivedSpellPicks > 0)
        {
            restored.SpellPickPoints += refundedArchivedSpellPicks;
        }

        restored.EnsureFreeCantripsKnown();

        restored.MaxHp = restored.CalcMaxHp();
        restored.CurrentHp = Math.Clamp(snapshot.CurrentHp, 0, restored.MaxHp);

        restored.SyncSpellSlots(fullRestore: true);
        restored._spellSlotsCurrent[1] = Math.Clamp(snapshot.SpellSlotsL1Current, 0, restored._spellSlotsMax[1]);
        restored._spellSlotsCurrent[2] = Math.Clamp(snapshot.SpellSlotsL2Current, 0, restored._spellSlotsMax[2]);
        restored._spellSlotsCurrent[3] = Math.Clamp(snapshot.SpellSlotsL3Current, 0, restored._spellSlotsMax[3]);

        player = restored;
        return true;
    }

    public void Draw()
    {
        Raylib.DrawRectangle(
            X * GameMap.TileSize,
            Y * GameMap.TileSize,
            GameMap.TileSize,
            GameMap.TileSize,
            new Color(210, 45, 45, 255));
    }

    private int CalcMaxHp()
    {
        var conMod = Mod(StatName.Constitution);
        // Level 1: max hit die + CON mod. Each subsequent level: average hit die + CON mod (min 1).
        var levelOneHp = CharacterClass.HitDie + conMod;
        var perLevel = Math.Max(1, CharacterClass.HitDie / 2 + 1 + conMod);
        var baseHp = Math.Max(1, levelOneHp) + perLevel * (Level - 1);
        var skillBonus = HasSkill("toughness") ? 5 + Math.Max(0, conMod) : 0;
        var featBonus = SumFeatBonus(feat => feat.MaxHpPerLevelBonus * Level + feat.MaxHpFlatBonus);
        return baseHp + skillBonus + featBonus;
    }

    private void LevelUp()
    {
        Level += 1;
        Xp -= XpToNextLevel;
        XpToNextLevel = GetXpToNextLevel(Level);

        StatPoints += 1;
        if (FeatProgression.GrantsFeat(Level))
        {
            FeatPoints += 1;
        }
        SpellPickPoints += SpellProgression.GetSpellPicksForLevel(CharacterClass.Name, Level);
        EnsureFreeCantripsKnown();

        SyncSpellSlots(fullRestore: true);
        RecalcMaxStats();
        CurrentHp = MaxHp;
    }

    private void RecalcMaxStats()
    {
        var newMaxHp = CalcMaxHp();
        var hpGain = Math.Max(0, newMaxHp - MaxHp);
        MaxHp = newMaxHp;
        CurrentHp = Math.Min(CurrentHp + hpGain, MaxHp);
    }

    /// <summary>Adjust MaxHp by a flat amount (e.g. Aid +5). Caller manages revert.</summary>
    public void AdjustMaxHp(int delta)
    {
        MaxHp = Math.Max(1, MaxHp + delta);
        if (delta > 0)
            CurrentHp = Math.Min(CurrentHp + delta, MaxHp);
        else
            CurrentHp = Math.Min(CurrentHp, MaxHp);
    }

    private int GetXpToNextLevel(int currentLevel)
    {
        // Exact D&D-like curve for levels 1-6, then a gentler custom ramp.
        if (currentLevel < 6)
        {
            var nextLevel = currentLevel + 1;
            return CumulativeXpByLevel[nextLevel] - CumulativeXpByLevel[currentLevel];
        }

        return Math.Max(9000, (int)Math.Floor(XpToNextLevel * 1.25));
    }

    private void SyncSpellSlots(bool fullRestore)
    {
        Array.Clear(_spellSlotsMax, 0, _spellSlotsMax.Length);
        if (!SpellData.SpellSlotsByClass.TryGetValue(CharacterClass.Name, out var progression))
        {
            Array.Clear(_spellSlotsCurrent, 0, _spellSlotsCurrent.Length);
            return;
        }

        var effectiveLevel = Math.Min(6, Math.Max(1, Level));
        var slots = progression[effectiveLevel];
        _spellSlotsMax[1] = slots.L1 + SumFeatBonus(feat => feat.BonusSpellSlotL1);
        _spellSlotsMax[2] = slots.L2;
        _spellSlotsMax[3] = slots.L3;

        for (var spellLevel = 1; spellLevel <= 3; spellLevel++)
        {
            if (fullRestore)
            {
                _spellSlotsCurrent[spellLevel] = _spellSlotsMax[spellLevel];
            }
            else if (_spellSlotsCurrent[spellLevel] > _spellSlotsMax[spellLevel])
            {
                _spellSlotsCurrent[spellLevel] = _spellSlotsMax[spellLevel];
            }
        }
    }

    private void EnsureFreeCantripsKnown()
    {
        if (!SpellData.ClassSpellUnlocks.TryGetValue(CharacterClass.Name, out var unlocks))
        {
            return;
        }

        foreach (var unlock in unlocks)
        {
            if (unlock.MinLevel > Level) continue;
            if (!SpellData.ById.TryGetValue(unlock.SpellId, out var spell)) continue;
            if (!spell.IsCantrip) continue;
            if (!SpellData.IsPlayerVisible(spell)) continue;
            _knownSpellIds.Add(spell.Id);
        }
    }
}
