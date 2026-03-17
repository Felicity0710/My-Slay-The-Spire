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
        ["cultist_scout"] = new EnemyVisualProfile(
            "cultist_scout",
            "Scout",
            "res://Assets/Icons/enemy_cultist.svg",
            new Color("1f3f3f")),
        ["cultist_shaman"] = new EnemyVisualProfile(
            "cultist_shaman",
            "Shaman",
            "res://Assets/Icons/enemy_cultist.svg",
            new Color("3a2148")),
        ["cultist_guard"] = new EnemyVisualProfile(
            "cultist_guard",
            "Guard",
            "res://Assets/Icons/enemy_cultist.svg",
            new Color("1f2f52")),
        ["cultist_brute"] = new EnemyVisualProfile(
            "cultist_brute",
            "Brute",
            "res://Assets/Icons/enemy_cultist.svg",
            new Color("4a2c1f")),
        ["elite_sentinel"] = new EnemyVisualProfile(
            "elite_sentinel",
            "Elite Sentinel",
            "res://Assets/Icons/enemy_elite.svg",
            new Color("3b1f46"))
    };

    private static readonly Dictionary<string, string> EnemyTraitSummary = new()
    {
        ["cultist"] = "标准型：攻击/防御/增益均衡。",
        ["cultist_scout"] = "游击型：伤害偏低但出手频繁。",
        ["cultist_shaman"] = "辅助型：优先给队友加力量，能抬高全队输出。",
        ["cultist_guard"] = "前排型：开局优先叠格挡，拖长战斗节奏。",
        ["cultist_brute"] = "爆发型：高伤攻击，且每3回合会强化自己。",
        ["elite_sentinel"] = "精英循环：攻击→防御→增益三段式轮转。"
    };

    private static readonly Dictionary<string, Color> EnemyTraitAccent = new()
    {
        ["cultist"] = new Color("7dd3fc"),
        ["cultist_scout"] = new Color("5eead4"),
        ["cultist_shaman"] = new Color("d8b4fe"),
        ["cultist_guard"] = new Color("93c5fd"),
        ["cultist_brute"] = new Color("fdba74"),
        ["elite_sentinel"] = new Color("f9a8d4")
    };

    public static EnemyVisualProfile GetEnemyVisual(string id)
    {
        if (EnemyProfiles.TryGetValue(id, out var profile))
        {
            return profile;
        }

        return EnemyProfiles["cultist"];
    }

    public static string GetEnemyTraitSummary(string archetypeId)
    {
        if (EnemyTraitSummary.TryGetValue(archetypeId, out var text))
        {
            return text;
        }

        return EnemyTraitSummary["cultist"];
    }

    public static Color GetEnemyTraitAccent(string archetypeId)
    {
        if (EnemyTraitAccent.TryGetValue(archetypeId, out var color))
        {
            return color;
        }

        return EnemyTraitAccent["cultist"];
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
