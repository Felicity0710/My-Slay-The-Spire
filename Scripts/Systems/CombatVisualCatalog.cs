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
        ["cultist"] = new("cultist", "Cultist", "res://Assets/Icons/enemy_cultist.svg", new Color("23384a")),
        ["cultist_scout"] = new("cultist_scout", "Scout", "res://Assets/Icons/enemy_cultist_scout.svg", new Color("1f3f3f")),
        ["cultist_guard"] = new("cultist_guard", "Guard", "res://Assets/Icons/enemy_cultist_guard.svg", new Color("1f2f52")),
        ["cultist_shaman"] = new("cultist_shaman", "Shaman", "res://Assets/Icons/enemy_cultist_shaman.svg", new Color("3a2148")),
        ["cultist_acolyte"] = new("cultist_acolyte", "Acolyte", "res://Assets/Icons/enemy_cultist_acolyte.svg", new Color("4a1f3a")),
        ["cultist_brute"] = new("cultist_brute", "Brute", "res://Assets/Icons/enemy_cultist_brute.svg", new Color("4a2c1f")),
        ["thief_cutpurse"] = new("thief_cutpurse", "Cutpurse", "res://Assets/Icons/enemy_thief_cutpurse.svg", new Color("3a4a2f")),
        ["thief_assassin"] = new("thief_assassin", "Assassin", "res://Assets/Icons/enemy_thief_assassin.svg", new Color("1f1f2f")),
        ["thief_fencer"] = new("thief_fencer", "Fencer", "res://Assets/Icons/enemy_thief_fencer.svg", new Color("2f3a4a")),
        ["slaver_red"] = new("slaver_red", "Red Slaver", "res://Assets/Icons/enemy_slaver_red.svg", new Color("5a1f1f")),
        ["slaver_blue"] = new("slaver_blue", "Blue Slaver", "res://Assets/Icons/enemy_slaver_blue.svg", new Color("1f2f5a")),
        ["plague_rat"] = new("plague_rat", "Plague Rat", "res://Assets/Icons/enemy_plague_rat.svg", new Color("3a3a1f")),
        ["mercenary"] = new("mercenary", "Mercenary", "res://Assets/Icons/enemy_mercenary.svg", new Color("4a3a2f")),
        ["corrupted_guardian"] = new("corrupted_guardian", "Corrupted Guardian", "res://Assets/Icons/enemy_corrupted_guardian.svg", new Color("2f1f3a")),
        ["dark_oracle"] = new("dark_oracle", "Dark Oracle", "res://Assets/Icons/enemy_dark_oracle.svg", new Color("1f1f1f")),
        ["void_beast"] = new("void_beast", "Void Beast", "res://Assets/Icons/enemy_void_beast.svg", new Color("1a0a2a")),
        ["ancient_automaton"] = new("ancient_automaton", "Ancient Automaton", "res://Assets/Icons/enemy_ancient_automaton.svg", new Color("3a3a4a")),
        ["temple_knight"] = new("temple_knight", "Temple Knight", "res://Assets/Icons/enemy_temple_knight.svg", new Color("4a4a2f")),
        ["flame_wraith"] = new("flame_wraith", "Flame Wraith", "res://Assets/Icons/enemy_flame_wraith.svg", new Color("5a2f0a")),
        ["elite_sentinel"] = new("elite_sentinel", "Elite Sentinel", "res://Assets/Icons/enemy_elite_sentinel.svg", new Color("3b1f46")),
        ["elite_inquisitor"] = new("elite_inquisitor", "Inquisitor", "res://Assets/Icons/enemy_elite_inquisitor.svg", new Color("5a1f1f")),
        ["elite_assassin_guild"] = new("elite_assassin_guild", "Assassin Lord", "res://Assets/Icons/enemy_elite_assassin_guild.svg", new Color("1f1f1f")),
        ["elite_slaver_boss"] = new("elite_slaver_boss", "Slaver Chief", "res://Assets/Icons/enemy_elite_slaver_boss.svg", new Color("5a3a1f")),
        ["elite_corrupted_golem"] = new("elite_corrupted_golem", "Corrupted Golem", "res://Assets/Icons/enemy_elite_corrupted_golem.svg", new Color("3a1f3a")),
        ["elite_ancient_dragon"] = new("elite_ancient_dragon", "Ancient Wyrm", "res://Assets/Icons/enemy_elite_ancient_dragon.svg", new Color("4a1f1f")),
        ["merchant"] = new("merchant", "Merchant", "res://Assets/Icons/enemy_merchant.svg", new Color("4a4a2a")),
        ["boss_high_priest"] = new("boss_high_priest", "High Priest Morg", "res://Assets/Icons/enemy_boss_high_priest.svg", new Color("2a1a3a")),
        ["boss_master_thief"] = new("boss_master_thief", "Master Thief Rane", "res://Assets/Icons/enemy_boss_master_thief.svg", new Color("1a1a1a")),
        ["boss_corrupted_heart"] = new("boss_corrupted_heart", "Corrupted Heart", "res://Assets/Icons/enemy_boss_corrupted_heart.svg", new Color("3a0a0a")),
    };

    private static readonly Dictionary<string, string> EnemyTraitSummaryKey = new()
    {
        ["cultist"] = "combat.enemy_trait.cultist",
        ["cultist_scout"] = "combat.enemy_trait.cultist_scout",
        ["cultist_guard"] = "combat.enemy_trait.cultist_guard",
        ["cultist_shaman"] = "combat.enemy_trait.cultist_shaman",
        ["cultist_acolyte"] = "combat.enemy_trait.cultist_acolyte",
        ["cultist_brute"] = "combat.enemy_trait.cultist_brute",
        ["thief_cutpurse"] = "combat.enemy_trait.thief_cutpurse",
        ["thief_assassin"] = "combat.enemy_trait.thief_assassin",
        ["thief_fencer"] = "combat.enemy_trait.thief_fencer",
        ["slaver_red"] = "combat.enemy_trait.slaver_red",
        ["slaver_blue"] = "combat.enemy_trait.slaver_blue",
        ["plague_rat"] = "combat.enemy_trait.plague_rat",
        ["mercenary"] = "combat.enemy_trait.mercenary",
        ["corrupted_guardian"] = "combat.enemy_trait.corrupted_guardian",
        ["dark_oracle"] = "combat.enemy_trait.dark_oracle",
        ["void_beast"] = "combat.enemy_trait.void_beast",
        ["ancient_automaton"] = "combat.enemy_trait.ancient_automaton",
        ["temple_knight"] = "combat.enemy_trait.temple_knight",
        ["flame_wraith"] = "combat.enemy_trait.flame_wraith",
        ["elite_sentinel"] = "combat.enemy_trait.elite_sentinel",
        ["elite_inquisitor"] = "combat.enemy_trait.elite_inquisitor",
        ["elite_assassin_guild"] = "combat.enemy_trait.elite_assassin_guild",
        ["elite_slaver_boss"] = "combat.enemy_trait.elite_slaver_boss",
        ["elite_corrupted_golem"] = "combat.enemy_trait.elite_corrupted_golem",
        ["elite_ancient_dragon"] = "combat.enemy_trait.elite_ancient_dragon",
        ["merchant"] = "combat.enemy_trait.merchant",
        ["boss_high_priest"] = "combat.enemy_trait.boss_high_priest",
        ["boss_master_thief"] = "combat.enemy_trait.boss_master_thief",
        ["boss_corrupted_heart"] = "combat.enemy_trait.boss_corrupted_heart",
    };

    private static readonly Dictionary<string, Color> EnemyTraitAccent = new()
    {
        ["cultist"] = new("7dd3fc"), ["cultist_scout"] = new("5eead4"),
        ["cultist_guard"] = new("93c5fd"), ["cultist_shaman"] = new("d8b4fe"),
        ["cultist_acolyte"] = new("f0abfc"), ["cultist_brute"] = new("fdba74"),
        ["thief_cutpurse"] = new("86efac"), ["thief_assassin"] = new("c084fc"),
        ["thief_fencer"] = new("67e8f9"), ["slaver_red"] = new("fca5a5"),
        ["slaver_blue"] = new("93c5fd"), ["plague_rat"] = new("fde047"),
        ["mercenary"] = new("d6d3d1"), ["corrupted_guardian"] = new("c084fc"),
        ["dark_oracle"] = new("a1a1aa"), ["void_beast"] = new("8b5cf6"),
        ["ancient_automaton"] = new("94a3b8"), ["temple_knight"] = new("fcd34d"),
        ["flame_wraith"] = new("fb923c"),
        ["elite_sentinel"] = new("f9a8d4"), ["elite_inquisitor"] = new("f87171"),
        ["elite_assassin_guild"] = new("a78bfa"), ["elite_slaver_boss"] = new("fb923c"),
        ["elite_corrupted_golem"] = new("c084fc"), ["elite_ancient_dragon"] = new("f87171"),
        ["merchant"] = new("fbbf24"),
        ["boss_high_priest"] = new("c084fc"), ["boss_master_thief"] = new("a3a3a3"),
        ["boss_corrupted_heart"] = new("ef4444"),
    };

    private static readonly Dictionary<string, string> TraitFallbacks = new()
    {
        ["cultist"] = "A basic cultist. Alternates between attacking and performing dark rituals to grow stronger.",
        ["cultist_scout"] = "Fast but fragile. Strikes nearly every turn with quick, light attacks.",
        ["cultist_guard"] = "Defensive frontline. Shields itself heavily before striking back.",
        ["cultist_shaman"] = "Support caster. Buffs all allies with dark magic. Kill first!",
        ["cultist_acolyte"] = "Squishy zealot. Devotes every moment to empowering allies. Eliminate immediately.",
        ["cultist_brute"] = "Heavy hitter. Rages every 3 turns for massive strength, then unleashes devastating blows.",
        ["thief_cutpurse"] = "Aggressive pickpocket. Attacks relentlessly, stealing gold with each hit.",
        ["thief_assassin"] = "Ambush predator. Prepares on turn one, then strikes with lethal precision.",
        ["thief_fencer"] = "Skilled duelist. Balances attack and defense, with a predictable critical riposte every 4th turn.",
        ["slaver_red"] = "Aggressive enforcer. Pummels you while rallying nearby allies.",
        ["slaver_blue"] = "Defensive support. Shields allies and wears you down slowly.",
        ["plague_rat"] = "Diseased vermin. Spreads sickness that weakens you. Explodes on death.",
        ["mercenary"] = "Veteran sellsword. Unpredictable pattern — adapt your strategy each turn.",
        ["corrupted_guardian"] = "Once-holy protector now twisted. Fortifies heavily before unleashing rage.",
        ["dark_oracle"] = "Cursed seer. Places stacking curses on you every other turn.",
        ["void_beast"] = "Creature of the abyss. Attacks relentlessly. Its death empowers surviving allies.",
        ["ancient_automaton"] = "Primeval war machine. Charges for 2 turns, then fires a devastating Hyper Beam.",
        ["temple_knight"] = "Disciplined sentinel of the old faith. Follows a strict 3-turn combat routine.",
        ["flame_wraith"] = "Restless burning spirit. Fans the flames to empower allies. Explodes upon death.",
        ["elite_sentinel"] = "ELITE. Predictable 3-turn cycle: Attack → Defend → Buff. Learn the pattern.",
        ["elite_inquisitor"] = "ELITE. Opens by marking you as a heretic, then attacks with righteous fury.",
        ["elite_assassin_guild"] = "ELITE. Master of the shadows. Efficient multi-action turns with deadly combos.",
        ["elite_slaver_boss"] = "ELITE. Commands slave warriors. Hides behind allies; dangerous when cornered alone.",
        ["elite_corrupted_golem"] = "ELITE. Grows stronger every time you strike it. A battle of attrition you must end quickly.",
        ["elite_ancient_dragon"] = "ELITE. Young dragon with a 3-turn cycle: Fire Breath (AoE) → Claw → Take Flight.",
        ["merchant"] = "A humble merchant... until you try to rob him. Then he fights back with surprising skill.",
        ["boss_high_priest"] = "BOSS — Act I. 4-turn ritual cycle. Enters a desperate second phase below half health.",
        ["boss_master_thief"] = "BOSS — Act II. 5-turn agile cycle. Steals your cards mid-fight! Recovered upon victory.",
        ["boss_corrupted_heart"] = "BOSS — Act III. The source of all corruption. Two distinct phases. The ultimate test.",
    };

    public static EnemyVisualProfile GetEnemyVisual(string id)
    {
        if (EnemyProfiles.TryGetValue(id, out var profile)) return profile;
        return EnemyProfiles["cultist"];
    }

    public static IEnumerable<string> AllEnemyIds() => EnemyProfiles.Keys;

    private static readonly Dictionary<string, string> TraitFallbacksZh = new()
    {
        ["cultist"] = "基础邪教徒。在攻击和黑暗仪式之间切换，逐渐变强。",
        ["cultist_scout"] = "快速但脆弱。几乎每回合都发动快速轻击。",
        ["cultist_guard"] = "防御型前线。先架起厚重护盾，再伺机反击。",
        ["cultist_shaman"] = "辅助施法者。用黑暗魔法强化所有友军。优先击杀！",
        ["cultist_acolyte"] = "脆弱的狂热者。全身心投入强化同伴。立即消灭。",
        ["cultist_brute"] = "重击手。每3回合暴怒一次，叠加巨额力量后发动毁灭性打击。",
        ["thief_cutpurse"] = "攻击型扒手。持续攻击，每次命中偷取金币。",
        ["thief_assassin"] = "伏击猎手。第一回合准备，随后精准致命一击。",
        ["thief_fencer"] = "熟练决斗者。攻防平衡，每4回合发动可预测的暴击。",
        ["slaver_red"] = "攻击型监工。猛击的同时鼓舞附近的友军。",
        ["slaver_blue"] = "防御型支援。为友军提供护盾，慢慢消耗你。",
        ["plague_rat"] = "染病鼠辈。传播削弱你的疾病。死亡时爆炸。",
        ["mercenary"] = "老兵佣兵。行动模式不可预测——每回合都要调整策略。",
        ["corrupted_guardian"] = "曾经神圣的守护者，如今已被腐化。先重装防御，再释放怒火。",
        ["dark_oracle"] = "被诅咒的先知。每隔一回合给你叠加诅咒。",
        ["void_beast"] = "深渊生物。持续猛攻。死亡时强化存活的友军。",
        ["ancient_automaton"] = "远古战争机器。蓄力2回合，然后发射毁灭性的超级光束。",
        ["temple_knight"] = "古老信仰的纪律哨兵。遵循严格的3回合战斗程式。",
        ["flame_wraith"] = "不安的燃烧灵魂。煽动火焰强化友军。死亡时爆炸。",
        ["elite_sentinel"] = "精英。可预测的3回合循环：攻击→防御→强化。掌握规律即可。",
        ["elite_inquisitor"] = "精英。先将你标记为异端，然后用正义之怒发动攻击。",
        ["elite_assassin_guild"] = "精英。暗影大师。高效的多重行动回合，致命连招。",
        ["elite_slaver_boss"] = "精英。指挥奴隶战士。躲在友军身后；单独被逼入绝境时很危险。",
        ["elite_corrupted_golem"] = "精英。每次被击中都会变强。一场必须尽快结束的消耗战。",
        ["elite_ancient_dragon"] = "精英。幼龙，3回合循环：火焰吐息(AoE)→爪击→腾飞。",
        ["merchant"] = "一位谦逊的商人……直到你试图打劫他。那时他会以惊人的技巧反击。",
        ["boss_high_priest"] = "Boss — 第一章。4回合仪式循环。半血以下进入绝望的第二阶段。",
        ["boss_master_thief"] = "Boss — 第二章。5回合敏捷循环。战斗中偷走你的卡牌！胜利后归还。",
        ["boss_corrupted_heart"] = "Boss — 第三章。一切腐化的源头。两个截然不同的阶段。终极考验。",
    };

    public static string GetEnemyTraitSummary(string archetypeId)
    {
        if (LocalizationSettings.CurrentLanguage == GameLanguage.ZhHans
            && TraitFallbacksZh.TryGetValue(archetypeId, out var zhFallback))
            return zhFallback;
        if (EnemyTraitSummaryKey.TryGetValue(archetypeId, out var key))
        {
            var localized = LocalizationService.Get(key, string.Empty);
            if (!string.IsNullOrWhiteSpace(localized)) return localized;
        }
        if (TraitFallbacks.TryGetValue(archetypeId, out var fallback)) return fallback;
        return TraitFallbacks["cultist"];
    }

    private static readonly Dictionary<string, string> EnemyDisplayNameZh = new()
    {
        ["cultist"] = "邪教徒", ["cultist_scout"] = "邪教斥候", ["cultist_guard"] = "邪教守卫",
        ["cultist_shaman"] = "邪教萨满", ["cultist_acolyte"] = "邪教侍僧", ["cultist_brute"] = "邪教蛮兵",
        ["thief_cutpurse"] = "扒手", ["thief_assassin"] = "刺客", ["thief_fencer"] = "剑客",
        ["slaver_red"] = "红奴隶主", ["slaver_blue"] = "蓝奴隶主", ["plague_rat"] = "瘟疫鼠",
        ["mercenary"] = "雇佣兵", ["corrupted_guardian"] = "腐化守卫", ["dark_oracle"] = "暗黑神谕",
        ["void_beast"] = "虚空兽", ["ancient_automaton"] = "古代机械", ["temple_knight"] = "圣殿骑士",
        ["flame_wraith"] = "火焰幽灵", ["elite_sentinel"] = "精英哨兵", ["elite_inquisitor"] = "审判官",
        ["elite_assassin_guild"] = "暗杀公会", ["elite_slaver_boss"] = "奴隶主首领",
        ["elite_corrupted_golem"] = "腐化魔像", ["elite_ancient_dragon"] = "远古幼龙",
        ["merchant"] = "商人", ["boss_high_priest"] = "大祭司莫格",
        ["boss_master_thief"] = "影贼大师雷恩", ["boss_corrupted_heart"] = "腐化之心",
    };

    public static string GetLocalizedEnemyDisplayName(string archetypeId)
    {
        if (LocalizationSettings.CurrentLanguage == GameLanguage.ZhHans
            && EnemyDisplayNameZh.TryGetValue(archetypeId, out var zhName))
            return zhName;
        return GetEnemyVisual(archetypeId).DisplayName;
    }

    public static string GetLocalizedEnemyName(string archetypeId, string rawName)
    {
        var displayName = GetLocalizedEnemyDisplayName(archetypeId);
        if (string.IsNullOrWhiteSpace(rawName)) return displayName;
        var parts = rawName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return displayName;
        var suffix = parts[^1];
        if (suffix.Length == 1 && char.IsLetterOrDigit(suffix[0])) return $"{displayName} {suffix}";
        return displayName;
    }

    public static Color GetEnemyTraitAccent(string archetypeId)
    {
        if (EnemyTraitAccent.TryGetValue(archetypeId, out var color)) return color;
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
        if (relicId.StartsWith("boss_")) return "res://Assets/Icons/relic_lantern.svg";
        return relicId switch
        {
            "lantern" or "ember_ring" or "soul_compass" or "arcane_battery" or "philosopher_stone" or "cursed_key" or "coffee_dripper" => "res://Assets/Icons/relic_lantern.svg",
            "anchor" or "iron_shell" or "thorn_mail" or "warding_bell" or "dawn_totem" or "tungsten_rod" or "orichalcum" or "self_repair_kit" => "res://Assets/Icons/relic_anchor.svg",
            "whetstone" or "twin_blade_badge" or "glass_meteor" or "shuriken" or "necronomicon" or "molten_egg" or "wrist_blade" => "res://Assets/Icons/relic_whetstone.svg",
            "charm" or "blood_vial" or "cinder_tea" or "bird_faced_urn" or "black_blood" or "mango" => "res://Assets/Icons/relic_charm.svg",
            _ => "res://Assets/Icons/relic_lantern.svg"
        };
    }
}
