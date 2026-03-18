using Godot;
using System;
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

    private static readonly Dictionary<string, string> EnemyTraitSummaryKey = new()
    {
        ["cultist"] = "combat.enemy_trait.cultist",
        ["cultist_scout"] = "combat.enemy_trait.cultist_scout",
        ["cultist_shaman"] = "combat.enemy_trait.cultist_shaman",
        ["cultist_guard"] = "combat.enemy_trait.cultist_guard",
        ["cultist_brute"] = "combat.enemy_trait.cultist_brute",
        ["elite_sentinel"] = "combat.enemy_trait.elite_sentinel"
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
        if (EnemyTraitSummaryKey.TryGetValue(archetypeId, out var key))
        {
            var localized = LocalizationService.Get(key, string.Empty);
            if (!string.IsNullOrWhiteSpace(localized))
            {
                return localized;
            }

            return key switch
            {
                "combat.enemy_trait.cultist" => "Attack/defense/buff balance.",
                "combat.enemy_trait.cultist_scout" => "Fast damage dealer that strikes often.",
                "combat.enemy_trait.cultist_shaman" => "Support-oriented and buffs allies when possible.",
                "combat.enemy_trait.cultist_guard" => "Defensive first, dragging the duel out.",
                "combat.enemy_trait.cultist_brute" => "Burst damage, gains more power every few turns.",
                "combat.enemy_trait.elite_sentinel" => "Elite cycle: attack, defend and buff in rotation.",
                _ => string.Empty
            };
        }

        return GetEnemyTraitSummary("cultist");
    }

    public static string GetLocalizedEnemyDisplayName(string archetypeId)
    {
        var key = $"combat.enemy_name.{archetypeId}";
        var fallback = GetEnemyVisual(archetypeId).DisplayName;
        return LocalizationService.Get(key, fallback);
    }

    public static string GetLocalizedEnemyName(string archetypeId, string rawName)
    {
        var displayName = GetLocalizedEnemyDisplayName(archetypeId);
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return displayName;
        }

        var parts = rawName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return displayName;
        }

        var suffix = parts[^1];
        if (suffix.Length == 1 && char.IsLetterOrDigit(suffix[0]))
        {
            return $"{displayName} {suffix}";
        }

        return displayName;
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
            "storm_feather" => "res://Assets/Icons/relic_lantern.svg",
            "rune_kite" => "res://Assets/Icons/relic_whetstone.svg",
            "cinder_tea" => "res://Assets/Icons/relic_charm.svg",
            "thorn_mail" => "res://Assets/Icons/relic_anchor.svg",
            "frozen_lens" => "res://Assets/Icons/relic_whetstone.svg",
            "echo_coin" => "res://Assets/Icons/relic_charm.svg",
            "twin_blade_badge" => "res://Assets/Icons/relic_whetstone.svg",
            "warding_bell" => "res://Assets/Icons/relic_anchor.svg",
            "soul_compass" => "res://Assets/Icons/relic_lantern.svg",
            "overclock_core" => "res://Assets/Icons/relic_lantern.svg",
            "glass_meteor" => "res://Assets/Icons/relic_whetstone.svg",
            "dawn_totem" => "res://Assets/Icons/relic_anchor.svg",
            "void_hourglass" => "res://Assets/Icons/relic_charm.svg",
            "jade_cicada" => "res://Assets/Icons/relic_charm.svg",
            "ember_chisel" => "res://Assets/Icons/relic_whetstone.svg",
            _ => "res://Assets/Icons/relic_lantern.svg"
        };
    }
}
