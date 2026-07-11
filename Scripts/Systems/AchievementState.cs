using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

/// <summary>
/// Tracks which achievements have been unlocked. Unlocks are persisted to
/// a JSON file in the user data directory so they survive between runs.
/// </summary>
public static class AchievementState
{
    private const string FileName = "achievements.json";

    private static readonly HashSet<string> UnlockedIds = new();
    private static readonly HashSet<string> NewThisRun = new();

    private static bool _loaded;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private sealed class AchievementSaveDto
    {
        public List<string> UnlockedIds { get; set; } = new();
    }

    public static string GetFilePath()
    {
        var dir = OS.GetUserDataDir();
        return Path.Combine(dir, FileName);
    }

    public static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        try
        {
            var path = GetFilePath();
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var dto = JsonSerializer.Deserialize<AchievementSaveDto>(json, SerializerOptions);
                if (dto?.UnlockedIds != null)
                {
                    foreach (var id in dto.UnlockedIds) UnlockedIds.Add(id);
                }
            }
        }
        catch (Exception ex)
        {
            GD.PushWarning($"AchievementState load failed: {ex.Message}");
        }
    }

    private static void Save()
    {
        try
        {
            var dto = new AchievementSaveDto();
            foreach (var id in UnlockedIds) dto.UnlockedIds.Add(id);
            var json = JsonSerializer.Serialize(dto, SerializerOptions);
            var path = GetFilePath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }
        catch (Exception ex)
        {
            GD.PushWarning($"AchievementState save failed: {ex.Message}");
        }
    }

    /// <summary>Try to unlock an achievement. Returns true if newly unlocked.</summary>
    public static bool TryUnlock(string id)
    {
        EnsureLoaded();
        if (UnlockedIds.Add(id))
        {
            NewThisRun.Add(id);
            Save();
            return true;
        }
        return false;
    }

    /// <summary>Check if an achievement is already unlocked.</summary>
    public static bool IsUnlocked(string id)
    {
        EnsureLoaded();
        return UnlockedIds.Contains(id);
    }

    /// <summary>Get achievements newly unlocked this run (for end-of-run popups).</summary>
    public static IReadOnlyCollection<string> GetNewThisRun() => NewThisRun;

    /// <summary>Clear the "new this run" set at the start of a new run.</summary>
    public static void ResetNewThisRun()
    {
        NewThisRun.Clear();
    }

    /// <summary>
    /// Evaluate all achievements against current GameState and unlock any that qualify.
    /// Call this at the end of a run (victory or defeat).
    /// </summary>
    public static void EvaluateRunEnd(GameState state)
    {
        EnsureLoaded();

        // Milestone
        if (state.BattlesWon >= 1) TryUnlock("first_blood");
        if (state.Floor >= 2) TryUnlock("first_floor");
        if (state.Act >= 2) TryUnlock("act_one_clear");
        if (state.Act >= 3) TryUnlock("act_two_clear");
        if (state.RunCompleted) TryUnlock("the_finisher");

        // Collection
        if (state.DeckCardIds.Count >= 30) TryUnlock("deck_master");
        if (state.DeckCardIds.Count <= 12 && state.RunCompleted) TryUnlock("minimalist");
        if (state.RelicIds.Count >= 8) TryUnlock("relic_hoarder");
        if (state.PotionIds.Count >= 3) TryUnlock("potion_brewer");

        // Elite hunting
        if (state.EliteKills >= 3) TryUnlock("elite_hunter");

        // Events
        if (state.EventsResolved >= 3) TryUnlock("pacifist");

        // Gold
        if (state.Gold == 0 && state.RunCompleted) TryUnlock("poverty");

        // Merchant
        if (state.MerchantRobbed) TryUnlock("merchant_robber");
    }
}
