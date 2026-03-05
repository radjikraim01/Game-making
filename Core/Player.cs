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
        Stats = characterClass.BaseStats.Clone();

        foreach (var kv in GenderModifiers[gender])
        {
            Stats.Add(kv.Key, kv.Value);
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
        MaxMana = CalcMaxMana();
        CurrentMana = MaxMana;
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
    public int MaxMana { get; private set; }
    public int CurrentMana { get; set; }
    public bool HasUsedSecondWind { get; set; }

    public bool IsAlive => CurrentHp > 0;

    public int Mod(StatName stat)
    {
        return (int)Math.Floor((Stats.Get(stat) - 10) / 2.0);
    }

    public bool HasSkill(string id) => _skillIds.Contains(id);
    public bool HasFeat(string id) => _featIds.Contains(id);
    public bool KnowsSpell(string id) => _knownSpellIds.Contains(id);
    public bool IsCasterClass => SpellData.SpellSlotsByClass.ContainsKey(CharacterClass.Name);

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

    public int SpellDamageBonus
    {
        get
        {
            return SumFeatBonus(feat => feat.SpellDamageBonus);
        }
    }

    public int SpellArmorBypassBonus => SumFeatBonus(feat => feat.SpellArmorBypassBonus);

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

    public IReadOnlyList<SpellDefinition> GetKnownSpells()
    {
        return _knownSpellIds
            .Where(SpellData.ById.ContainsKey)
            .Select(id => SpellData.ById[id])
            .OrderBy(spell => spell.SpellLevel)
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
            .OrderBy(spell => spell.SpellLevel)
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
            if (HasSkill("arcane_surge")) bonus += 3 + Math.Max(0, Mod(StatName.Intelligence));
            if (HasSkill("inspire")) bonus += 2 + Math.Max(0, (int)Math.Floor(Mod(StatName.Charisma) * 0.5));
            bonus += SumFeatBonus(feat => feat.MeleeDamageBonus);
            return bonus;
        }
    }

    public int WarCryBonus => HasSkill("war_cry") ? 3 + Math.Max(0, (int)Math.Floor(Mod(StatName.Strength) * 0.5)) : 0;

    public int CritBonus
    {
        get
        {
            var bonus = 0;
            if (HasSkill("eagle_eye")) bonus += 2 + Math.Max(0, Mod(StatName.Dexterity));
            bonus += SumFeatBonus(feat => feat.CritChanceBonus);
            return bonus;
        }
    }

    public int DefenseBonus
    {
        get
        {
            var bonus = 0;
            if (HasSkill("iron_skin")) bonus += 1 + Math.Max(0, (int)Math.Floor(Mod(StatName.Constitution) * 0.5));
            bonus += SumFeatBonus(feat => feat.DefenseBonus);
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
    public int ManaShieldAbsorb => 5 + Math.Max(0, Mod(StatName.Intelligence));
    public bool HasBonusAttack => HasSkill("swift_strikes");

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

    public void AllocateCreationStatPoint(StatName stat)
    {
        Stats.Add(stat, 1);
        RecalcMaxStats();
        CurrentHp = MaxHp;
        CurrentMana = MaxMana;
    }

    public void LearnSkill(Skill skill)
    {
        if (_skillIds.Contains(skill.Id)) return;
        _skillIds.Add(skill.Id);
        Skills.Add(skill);
        RecalcMaxStats();
    }

    public bool LearnFeat(FeatDefinition feat)
    {
        if (!CanLearnFeat(feat, out _)) return false;

        _featIds.Add(feat.Id);
        Feats.Add(feat);
        FeatPoints -= 1;

        if (feat.Id == "resilient_feat")
        {
            Stats.Add(StatName.Constitution, 1);
            Stats.Add(StatName.Wisdom, 1);
        }

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
            "meditation" => $"+{5 + Math.Max(0, Mod(StatName.Wisdom))} Max MP",
            "brute_force" => $"+{2 + Math.Max(0, (int)Math.Floor(Mod(StatName.Strength) * 0.5))} Melee Dmg",
            "war_cry" => $"+{WarCryBonus} First-Strike",
            "iron_skin" => $"+{DefenseBonus} Defense",
            "second_wind" => $"Heal {SecondWindHeal} HP",
            "eagle_eye" => $"+{CritBonus}% Crit",
            "shadowstep" => $"+{FleeBonus}% Flee",
            "swift_strikes" => "Bonus attack roll",
            "poison_blade" => $"+{PoisonDamage} Poison/turn",
            "arcane_surge" => $"+{3 + Math.Max(0, Mod(StatName.Intelligence))} Magic Dmg",
            "mana_shield" => $"Absorb {ManaShieldAbsorb} dmg for 3 MP",
            "inspire" => $"+{2 + Math.Max(0, (int)Math.Floor(Mod(StatName.Charisma) * 0.5))} All Dmg",
            _ => skill.Effect
        };
    }

    public string GetFeatEffectText(FeatDefinition feat)
    {
        if (string.Equals(feat.Id, "resilient_feat", StringComparison.Ordinal))
        {
            return "+1 Constitution and +1 Wisdom";
        }

        var parts = new List<string>();
        if (feat.MeleeDamageBonus != 0) parts.Add($"+{feat.MeleeDamageBonus} Melee Damage");
        if (feat.SpellDamageBonus != 0) parts.Add($"+{feat.SpellDamageBonus} Spell Damage");
        if (feat.DefenseBonus != 0) parts.Add($"+{feat.DefenseBonus} Defense");
        if (feat.CritChanceBonus != 0) parts.Add($"+{feat.CritChanceBonus}% Crit Chance");
        if (feat.FleeChanceBonus != 0) parts.Add($"+{feat.FleeChanceBonus}% Flee Chance");
        if (feat.SpellArmorBypassBonus != 0) parts.Add($"Spells ignore +{feat.SpellArmorBypassBonus} armor");
        if (feat.MaxHpFlatBonus != 0) parts.Add($"+{feat.MaxHpFlatBonus} Max HP");
        if (feat.MaxHpPerLevelBonus != 0) parts.Add($"+{feat.MaxHpPerLevelBonus * Level} Max HP");
        if (feat.MaxManaBonus != 0) parts.Add($"+{feat.MaxManaBonus} Max Mana");

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
            MaxMana = MaxMana,
            CurrentMana = CurrentMana,
            HasUsedSecondWind = HasUsedSecondWind,
            SkillIds = _skillIds.OrderBy(id => id).ToList(),
            FeatIds = _featIds.OrderBy(id => id).ToList(),
            KnownSpellIds = _knownSpellIds.OrderBy(id => id).ToList(),
            SpellSlotsL1Current = _spellSlotsCurrent[1],
            SpellSlotsL2Current = _spellSlotsCurrent[2],
            SpellSlotsL3Current = _spellSlotsCurrent[3]
        };
    }

    public static bool TryFromSnapshot(PlayerSnapshot snapshot, out Player? player, out string errorMessage)
    {
        player = null;
        errorMessage = string.Empty;

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
            restored._knownSpellIds.Add(spell.Id);
        }

        restored.EnsureFreeCantripsKnown();

        restored.MaxHp = restored.CalcMaxHp();
        restored.MaxMana = restored.CalcMaxMana();
        restored.CurrentHp = Math.Clamp(snapshot.CurrentHp, 0, restored.MaxHp);
        restored.CurrentMana = Math.Clamp(snapshot.CurrentMana, 0, restored.MaxMana);

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
        var baseHp = 10 + Stats.Constitution * 2;
        var skillBonus = HasSkill("toughness") ? 5 + Math.Max(0, Mod(StatName.Constitution)) : 0;
        var featBonus = SumFeatBonus(feat => feat.MaxHpPerLevelBonus * Level + feat.MaxHpFlatBonus);

        return baseHp + skillBonus + featBonus;
    }

    private int CalcMaxMana()
    {
        var baseMana = 5 + Stats.Wisdom;
        var skillBonus = HasSkill("meditation") ? 5 + Math.Max(0, Mod(StatName.Wisdom)) : 0;
        var featBonus = SumFeatBonus(feat => feat.MaxManaBonus);
        return baseMana + skillBonus + featBonus;
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
        CurrentMana = MaxMana;
    }

    private void RecalcMaxStats()
    {
        var newMaxHp = CalcMaxHp();
        var hpGain = Math.Max(0, newMaxHp - MaxHp);
        MaxHp = newMaxHp;
        CurrentHp = Math.Min(CurrentHp + hpGain, MaxHp);

        var newMaxMana = CalcMaxMana();
        var mpGain = Math.Max(0, newMaxMana - MaxMana);
        MaxMana = newMaxMana;
        CurrentMana = Math.Min(CurrentMana + mpGain, MaxMana);
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
        _spellSlotsMax[1] = slots.L1;
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
            _knownSpellIds.Add(spell.Id);
        }
    }
}
