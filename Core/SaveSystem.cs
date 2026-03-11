using System.Text.Json;
using System.Text.Json.Serialization;

namespace DungeonEscape.Core;

public sealed class StatsSnapshot
{
    public int Strength { get; set; }
    public int Dexterity { get; set; }
    public int Constitution { get; set; }
    public int Intelligence { get; set; }
    public int Wisdom { get; set; }
    public int Charisma { get; set; }
}

public sealed class PlayerSnapshot
{
    public string Name { get; set; } = "Adventurer";
    public string ClassName { get; set; } = string.Empty;
    public Gender Gender { get; set; } = Gender.Male;
    public Race Race { get; set; } = Race.Human;
    public int X { get; set; }
    public int Y { get; set; }
    public StatsSnapshot Stats { get; set; } = new();
    public int Level { get; set; } = 1;
    public int Xp { get; set; }
    public int XpToNextLevel { get; set; } = 300;
    public int StatPoints { get; set; }
    public int FeatPoints { get; set; }
    public int SpellPickPoints { get; set; }
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public bool HasUsedSecondWind { get; set; }
    public List<string> SkillIds { get; set; } = new();
    public List<string> FeatIds { get; set; } = new();
    public List<string> KnownSpellIds { get; set; } = new();
    public int SpellSlotsL1Current { get; set; }
    public int SpellSlotsL2Current { get; set; }
    public int SpellSlotsL3Current { get; set; }
}

public sealed class EnemySnapshot
{
    public string TypeKey { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int? SpawnX { get; set; }
    public int? SpawnY { get; set; }
    public int CurrentHp { get; set; }
    public string LootName { get; set; } = string.Empty;
    public string? LootRarity { get; set; }
    public string? LootItemId { get; set; }
    public int LootItemQuantity { get; set; }
    public int EnemyAttackBonus { get; set; }
    public List<EnemyStatusSnapshot> StatusEffects { get; set; } = new();
}

public sealed class EnemyStatusSnapshot
{
    public string Kind { get; set; } = string.Empty;
    public int Potency { get; set; }
    public int RemainingTurns { get; set; }
    public string SourceSpellId { get; set; } = string.Empty;
    public string SourceLabel { get; set; } = string.Empty;
    public string? RepeatSaveStat { get; set; }
    public int SaveDc { get; set; }
    public bool BreaksOnDamageTaken { get; set; }
}

public sealed class CombatHazardStatusSnapshot
{
    public string Kind { get; set; } = string.Empty;
    public int Potency { get; set; }
    public int DurationTurns { get; set; }
    public int ChancePercent { get; set; } = 100;
    public string? InitialSaveStat { get; set; }
    public string? RepeatSaveStat { get; set; }
    public bool BreaksOnDamageTaken { get; set; }
}

public sealed class ConcentrationSnapshot
{
    public string SpellId { get; set; } = string.Empty;
    public string SpellLabel { get; set; } = string.Empty;
    public int RemainingRounds { get; set; }
}

public sealed class SummonSnapshot
{
    public string SummonTypeId { get; set; } = string.Empty;
    public int CurrentHp { get; set; }
}

public sealed class TransformationSnapshot
{
    public string SourceSpellId { get; set; } = string.Empty;
    public string FormId { get; set; } = string.Empty;
    public int TempHpRemaining { get; set; }
    public bool FirstHitPrimed { get; set; }
}

public sealed class CombatHazardSnapshot
{
    public string InstanceId { get; set; } = string.Empty;
    public string SourceSpellId { get; set; } = string.Empty;
    public string SourceLabel { get; set; } = string.Empty;
    public string Element { get; set; } = string.Empty;
    public int BaseDamage { get; set; }
    public int Variance { get; set; }
    public int ArmorBypass { get; set; }
    public int CenterX { get; set; }
    public int CenterY { get; set; }
    public int RadiusTiles { get; set; }
    public int RemainingRounds { get; set; }
    public bool FollowsPlayer { get; set; }
    public bool RequiresConcentration { get; set; }
    public bool TriggersOnTurnStart { get; set; }
    public bool TriggersOnEntry { get; set; }
    public string? InitialSaveStat { get; set; }
    public string SaveDamageBehavior { get; set; } = nameof(SpellSaveDamageBehavior.None);
    public List<CombatHazardStatusSnapshot> OnTriggerStatuses { get; set; } = new();
}

public sealed class LootDropSnapshot
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Rarity { get; set; } = "Common";
    public int X { get; set; }
    public int Y { get; set; }
    public string? InventoryItemId { get; set; }
    public int InventoryItemQuantity { get; set; }
}

public sealed class InventoryItemSnapshot
{
    public string Id { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public bool IsEquipped { get; set; }
    public int? EquippedSlotIndex { get; set; }
}

public sealed class MajorConditionSnapshot
{
    public string Type { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}

public sealed class GameSaveSnapshot
{
    public int SchemaVersion { get; set; } = SaveStore.CurrentSchemaVersion;
    public string SavedAtUtc { get; set; } = DateTime.UtcNow.ToString("O");
    public string SaveKind { get; set; } = "manual";
    public GameState ResumeState { get; set; } = GameState.Playing;
    public PlayerSnapshot Player { get; set; } = new();
    public string PlayerSpriteId { get; set; } = "knight_m";
    public List<EnemySnapshot> Enemies { get; set; } = new();
    public EnemySnapshot? CurrentEnemy { get; set; }
    public int EnemyPoisoned { get; set; }
    public bool WarCryAvailable { get; set; }
    public List<string> CombatLog { get; set; } = new();
    public double RespawnDelaySeconds { get; set; }
    public List<string> ClaimedRewardNodeIds { get; set; } = new();
    public int RunMeleeBonus { get; set; }
    public int RunSpellBonus { get; set; }
    public int RunDefenseBonus { get; set; }
    public int RunCritBonus { get; set; }
    public int RunFleeBonus { get; set; }
    public string RunArchetype { get; set; } = "None";
    public string RunRelic { get; set; } = "None";
    public string Phase3RouteChoice { get; set; } = "None";
    public bool Phase3RiskEventResolved { get; set; }
    public int Phase3XpPercentMod { get; set; }
    public int Phase3EnemyAttackBonus { get; set; }
    public int Phase3EnemiesDefeated { get; set; }
    public bool Phase3PreSanctumRewardGranted { get; set; }
    public bool Phase3RouteWaveSpawned { get; set; }
    public bool Phase3SanctumWaveSpawned { get; set; }
    public int MilestoneChoicesTaken { get; set; }
    public int MilestoneExecutionRank { get; set; }
    public int MilestoneArcRank { get; set; }
    public int MilestoneEscapeRank { get; set; }
    public bool BossDefeated { get; set; }
    public bool FloorCleared { get; set; }
    public int SettingsMasterVolume { get; set; } = 80;
    public bool SettingsVerboseCombatLog { get; set; } = true;
    public string SettingsAccessibilityColorProfile { get; set; } = "Default";
    public bool SettingsAccessibilityHighContrast { get; set; }
    public bool SettingsOptionalConditionsEnabled { get; set; } = true;
    public string CreationOriginCondition { get; set; } = "None";
    public int DungeonConditionEventsTriggered { get; set; }
    public List<MajorConditionSnapshot> MajorConditions { get; set; } = new();
    public List<InventoryItemSnapshot> InventoryItems { get; set; } = new();
    public List<LootDropSnapshot> GroundLoot { get; set; } = new();
    public ConcentrationSnapshot? ActiveConcentration { get; set; }
    public SummonSnapshot? ActiveSummon { get; set; }
    public TransformationSnapshot? ActiveTransformation { get; set; }
    public List<CombatHazardSnapshot> CombatHazards { get; set; } = new();
    public List<PlayerConditionSaveEntry>? PlayerConditions { get; set; }
    // Batch 1 — non-concentration buff state (persistent across turns)
    public bool MageArmorActive { get; set; }
    public int AidMaxHpBonus { get; set; }
    public int PlayerTempHp { get; set; }
    public bool ShieldSpellActive { get; set; }
    public int ShieldSpellTurnsLeft { get; set; }
    // Batch 2 — non-concentration buff state
    public int MirrorImageCharges { get; set; }
    public bool AbsorbElementsCharged { get; set; }
    // Batch 3 — non-concentration buff state
    public bool HellishRebukePrimed { get; set; }
    public int ArmorOfAgathysTempHp { get; set; }
    public bool FireShieldActive { get; set; }
    public bool WrathOfStormPrimed { get; set; }
    public bool DeathWardActive { get; set; }
    public bool HolyRebukePrimed { get; set; }
    public bool CuttingWordsPrimed { get; set; }
    // Batch 4+5 — non-concentration buff state
    public bool CounterspellPrimed { get; set; }
    public bool ElementalWeaponActive { get; set; }
    public string ElementalWeaponElement { get; set; } = string.Empty;
    public bool RevivifyUsed { get; set; }
    public string ProtEnergyElement { get; set; } = string.Empty;
}

public sealed class PlayerConditionSaveEntry
{
    public string Kind { get; set; } = string.Empty;
    public int Potency { get; set; }
    public int RemainingTurns { get; set; }
}

public readonly struct SaveOperationResult
{
    public SaveOperationResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public bool Success { get; }
    public string Message { get; }
}

public sealed class SaveEntrySummary
{
    public bool IsAutosave { get; set; }
    public int ManualSlot { get; set; }
    public bool Exists { get; set; }
    public long SavedAtEpochMs { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}

public static class SaveStore
{
    public const int CurrentSchemaVersion = 23;
    public const int MaxManualSlot = 3;
    public const string SaveDirEnvVar = "DUNGEON_ESCAPE_SAVE_DIR";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static string SaveDir
    {
        get
        {
            var fromEnv = Environment.GetEnvironmentVariable(SaveDirEnvVar);
            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                return fromEnv.Trim();
            }

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DungeonEscapeRaylib",
                "saves");
        }
    }

    private static string GetManualSlotPath(int slot) => Path.Combine(SaveDir, $"slot{slot}.json");
    private static string GetAutosavePath() => Path.Combine(SaveDir, "autosave.json");

    public static SaveOperationResult SaveManualSlot(int slot, GameSaveSnapshot snapshot)
    {
        if (slot < 1 || slot > MaxManualSlot)
        {
            return new SaveOperationResult(false, "Invalid save slot. Choose slot 1-3.");
        }

        snapshot.SaveKind = "manual";
        snapshot.SchemaVersion = CurrentSchemaVersion;
        snapshot.SavedAtUtc = DateTime.UtcNow.ToString("O");
        return WriteSnapshot(GetManualSlotPath(slot), snapshot, $"Saved to slot {slot}.");
    }

    public static SaveOperationResult SaveAutosave(GameSaveSnapshot snapshot)
    {
        snapshot.SaveKind = "autosave";
        snapshot.SchemaVersion = CurrentSchemaVersion;
        snapshot.SavedAtUtc = DateTime.UtcNow.ToString("O");
        return WriteSnapshot(GetAutosavePath(), snapshot, "Autosave checkpoint updated.");
    }

    public static SaveOperationResult LoadManualSlot(int slot, out GameSaveSnapshot? snapshot)
    {
        snapshot = null;
        if (slot < 1 || slot > MaxManualSlot)
        {
            return new SaveOperationResult(false, "Invalid save slot. Choose slot 1-3.");
        }

        return ReadSnapshot(GetManualSlotPath(slot), out snapshot, $"Loaded slot {slot}.");
    }

    public static SaveOperationResult LoadAutosave(out GameSaveSnapshot? snapshot)
    {
        snapshot = null;
        return ReadSnapshot(GetAutosavePath(), out snapshot, "Loaded autosave.");
    }

    public static IReadOnlyList<SaveEntrySummary> GetManualSlotSummaries()
    {
        var list = new List<SaveEntrySummary>();
        for (var slot = 1; slot <= MaxManualSlot; slot++)
        {
            var path = GetManualSlotPath(slot);
            if (TryReadSnapshot(path, out var snapshot))
            {
                list.Add(BuildSummary(snapshot!, isAutosave: false, slot, exists: true));
                continue;
            }

            list.Add(new SaveEntrySummary
            {
                IsAutosave = false,
                ManualSlot = slot,
                Exists = false,
                Label = $"Slot {slot}",
                Detail = "Empty"
            });
        }

        return list;
    }

    public static IReadOnlyList<SaveEntrySummary> GetAvailableLoadEntries()
    {
        var list = new List<SaveEntrySummary>();

        var autosavePath = GetAutosavePath();
        if (TryReadSnapshot(autosavePath, out var autosaveSnapshot))
        {
            list.Add(BuildSummary(autosaveSnapshot!, isAutosave: true, slot: 0, exists: true));
        }

        for (var slot = 1; slot <= MaxManualSlot; slot++)
        {
            var path = GetManualSlotPath(slot);
            if (!TryReadSnapshot(path, out var snapshot)) continue;
            list.Add(BuildSummary(snapshot!, isAutosave: false, slot, exists: true));
        }

        return list
            .OrderByDescending(e => e.SavedAtEpochMs)
            .ThenBy(e => e.IsAutosave ? 0 : e.ManualSlot)
            .ToList();
    }

    private static SaveOperationResult WriteSnapshot(string path, GameSaveSnapshot snapshot, string successMessage)
    {
        try
        {
            Directory.CreateDirectory(SaveDir);
            var json = JsonSerializer.Serialize(snapshot, JsonOptions);
            File.WriteAllText(path, json);
            return new SaveOperationResult(true, successMessage);
        }
        catch (Exception ex)
        {
            return new SaveOperationResult(false, $"Save failed: {ex.Message}");
        }
    }

    private static SaveOperationResult ReadSnapshot(string path, out GameSaveSnapshot? snapshot, string successMessage)
    {
        snapshot = null;
        try
        {
            if (!File.Exists(path))
            {
                return new SaveOperationResult(false, "Save file not found.");
            }

            var json = File.ReadAllText(path);
            snapshot = JsonSerializer.Deserialize<GameSaveSnapshot>(json, JsonOptions);
            if (snapshot == null)
            {
                return new SaveOperationResult(false, "Save file is empty or invalid.");
            }

            return new SaveOperationResult(true, successMessage);
        }
        catch (Exception ex)
        {
            return new SaveOperationResult(false, $"Load failed: {ex.Message}");
        }
    }

    private static bool TryReadSnapshot(string path, out GameSaveSnapshot? snapshot)
    {
        snapshot = null;
        try
        {
            if (!File.Exists(path)) return false;
            var json = File.ReadAllText(path);
            snapshot = JsonSerializer.Deserialize<GameSaveSnapshot>(json, JsonOptions);
            return snapshot != null;
        }
        catch
        {
            snapshot = null;
            return false;
        }
    }

    private static SaveEntrySummary BuildSummary(GameSaveSnapshot snapshot, bool isAutosave, int slot, bool exists)
    {
        var savedLocal = "Unknown time";
        long epochMs = 0;
        if (DateTimeOffset.TryParse(snapshot.SavedAtUtc, out var dto))
        {
            epochMs = dto.ToUnixTimeMilliseconds();
            savedLocal = dto.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }

        var playerName = string.IsNullOrWhiteSpace(snapshot.Player?.Name) ? "Adventurer" : snapshot.Player.Name;
        var className = string.IsNullOrWhiteSpace(snapshot.Player?.ClassName) ? "Unknown Class" : snapshot.Player.ClassName;
        var level = snapshot.Player?.Level ?? 1;
        var hpNow = snapshot.Player?.CurrentHp ?? 0;
        var hpMax = snapshot.Player?.MaxHp ?? 0;
        var state = snapshot.ResumeState.ToString();

        return new SaveEntrySummary
        {
            IsAutosave = isAutosave,
            ManualSlot = slot,
            Exists = exists,
            SavedAtEpochMs = epochMs,
            Label = isAutosave ? "Autosave" : $"Slot {slot}",
            Detail = $"{playerName} L{level} {className} | HP {hpNow}/{hpMax} | {state} | {savedLocal}"
        };
    }
}
