using Godot;
using System.Collections.Generic;

public sealed class EnemyVisualProfile
{
    public string Id { get; }
    public string DisplayName { get; }
    public string PortraitPath { get; }
    public Color StageTint { get; }

    public EnemyVisualProfile(string id, string displayName, string portraitPath, Color stageTint)
    {
        Id = id;
        DisplayName = displayName;
        PortraitPath = portraitPath;
        StageTint = stageTint;
    }
}

public static class CombatVisualCatalog
{
    private static readonly Dictionary<string, EnemyVisualProfile> EnemyProfiles = new()
    {
        ["cultist"] = new EnemyVisualProfile(
            "cultist",
            "Cultist",
            "res://Assets/Icons/enemy_cultist.svg",
            new Color("23384a")),
        ["elite_sentinel"] = new EnemyVisualProfile(
            "elite_sentinel",
            "Elite Sentinel",
            "res://Assets/Icons/enemy_elite.svg",
            new Color("3b1f46"))
    };

    public static EnemyVisualProfile GetEnemyVisual(string id)
    {
        if (EnemyProfiles.TryGetValue(id, out var profile))
        {
            return profile;
        }

        return EnemyProfiles["cultist"];
    }

    public static string GetIntentIconPath(EnemyIntentType intentType)
    {
        return intentType switch
        {
            EnemyIntentType.Attack => "res://Assets/Icons/intent_attack.svg",
            EnemyIntentType.Defend => "res://Assets/Icons/intent_defend.svg",
            EnemyIntentType.Buff => "res://Assets/Icons/intent_buff.svg",
            _ => "res://Assets/Icons/intent_attack.svg"
        };
    }

    public static string GetRelicIconPath(string relicId)
    {
        return relicId switch
        {
            "lantern" => "res://Assets/Icons/relic_lantern.svg",
            "anchor" => "res://Assets/Icons/relic_anchor.svg",
            "whetstone" => "res://Assets/Icons/relic_whetstone.svg",
            "charm" => "res://Assets/Icons/relic_charm.svg",
            "ember_ring" => "res://Assets/Icons/relic_lantern.svg",
            "iron_shell" => "res://Assets/Icons/relic_anchor.svg",
            "blood_vial" => "res://Assets/Icons/relic_charm.svg",
            _ => "res://Assets/Icons/relic_lantern.svg"
        };
    }
}
