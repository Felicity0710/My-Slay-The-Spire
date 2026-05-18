using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

// Single-slot save/load system. Persists the run state when the player picks a
// map node, and is consumed by MainMenu's Continue button. Save file lives in
// Godot's user data dir (resolved via OS.GetUserDataDir()) so it survives
// reinstalls within the same user account.
public static class SaveSystem
{
    private const string SaveFileName = "savegame.json";
    private const int SaveFormatVersion = 1;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public sealed class SaveData
    {
        public int Version { get; set; } = SaveFormatVersion;
        public string SavedAtUtc { get; set; } = string.Empty;
        public string SceneFilePath { get; set; } = "res://Scenes/MapScene.tscn";

        public int PlayerHp { get; set; }
        public int MaxHp { get; set; }
        public int Gold { get; set; }
        public int Floor { get; set; }
        public int Act { get; set; }
        public bool RunCompleted { get; set; }
        public bool MerchantFled { get; set; }
        public int BattlesWon { get; set; }
        public int PotionCharges { get; set; }
        public string RngState { get; set; } = "1";

        public List<string> DeckCardIds { get; set; } = new();
        public List<string> RelicIds { get; set; } = new();
        public List<string> PotionIds { get; set; } = new();
        public string SelectedDeckPresetId { get; set; } = string.Empty;
        public bool HasCustomDeckOverride { get; set; }
        public List<string> CustomDeckOverrideIds { get; set; } = new();

        public int CurrentMapRow { get; set; }
        public int CurrentMapColumn { get; set; } = -1;
        public int PendingMapColumn { get; set; } = -1;
        public int PendingEncounterType { get; set; }
        public string PendingEventId { get; set; } = string.Empty;

        public List<string> PendingRewardOptions { get; set; } = new();
        public List<string> PendingPotionRewardOptions { get; set; } = new();
        public List<string> PendingRelicOptions { get; set; } = new();

        public List<ShopEntryDto> ShopSnapshot { get; set; } = new();
        public bool ShopSnapshotHasData { get; set; }
        public bool ShopSnapshotRemoveServiceUsed { get; set; }
        public bool PendingMerchantFightVictory { get; set; }

        // Map graph — list of rows, each row is the node types and outgoing column ids.
        public List<List<int>> MapLayout { get; set; } = new();
        public List<List<List<int>>> MapConnections { get; set; } = new();
    }

    public sealed class ShopEntryDto
    {
        public string Kind { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public int Price { get; set; }
        public bool Sold { get; set; }
    }

    public static string GetSaveFilePath()
    {
        var dir = OS.GetUserDataDir();
        return Path.Combine(dir, SaveFileName);
    }

    public static bool HasSave()
    {
        try
        {
            return File.Exists(GetSaveFilePath());
        }
        catch (Exception ex)
        {
            GD.PushWarning($"SaveSystem.HasSave failed: {ex.Message}");
            return false;
        }
    }

    public static void Write(SaveData data)
    {
        try
        {
            data.Version = SaveFormatVersion;
            data.SavedAtUtc = DateTime.UtcNow.ToString("o");
            var json = JsonSerializer.Serialize(data, SerializerOptions);
            var path = GetSaveFilePath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            File.Move(tmp, path);
        }
        catch (Exception ex)
        {
            GD.PushWarning($"SaveSystem.Write failed: {ex.Message}");
        }
    }

    public static SaveData? Read()
    {
        try
        {
            var path = GetSaveFilePath();
            if (!File.Exists(path))
            {
                return null;
            }

            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<SaveData>(json, SerializerOptions);
            return data;
        }
        catch (Exception ex)
        {
            GD.PushWarning($"SaveSystem.Read failed: {ex.Message}");
            return null;
        }
    }

    public static void Delete()
    {
        try
        {
            var path = GetSaveFilePath();
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            GD.PushWarning($"SaveSystem.Delete failed: {ex.Message}");
        }
    }
}
