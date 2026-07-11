using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public sealed class RelicData
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string Rarity { get; }
    public string Archetype { get; }

    public RelicData(string id, string name, string description, string rarity = "Common", string archetype = "General")
    {
        Id = id;
        Name = name;
        Description = description;
        Rarity = rarity;
        Archetype = archetype;
    }

    public string ToRelicText()
    {
        return $"{LocalizedName}\n{LocalizedDescription}";
    }

    public string LocalizedName
    {
        get
        {
            if (LocalizationSettings.CurrentLanguage == GameLanguage.ZhHans
                && ZhText.TryGetValue(Id, out var zh))
                return zh.Name;
            return Name;
        }
    }

    public string LocalizedDescription
    {
        get
        {
            if (LocalizationSettings.CurrentLanguage == GameLanguage.ZhHans
                && ZhText.TryGetValue(Id, out var zh))
                return zh.Desc;
            return Description;
        }
    }

    public string LocalizedArchetype => LocalizationService.Get(
        $"relic.archetype.{Archetype.ToLowerInvariant().Replace(' ', '_').Replace('-', '_')}",
        Archetype);

    public static IReadOnlyList<string> AllRelicIds() => Catalog.AllIds;

    public static RelicData CreateById(string id)
    {
        if (Catalog.ById.TryGetValue(id, out var relic)) return relic;
        return Catalog.Fallback;
    }

    public static IReadOnlyDictionary<string, List<RelicData>> GroupByRarity()
    {
        var grouped = new Dictionary<string, List<RelicData>>(StringComparer.OrdinalIgnoreCase);
        foreach (var relicId in Catalog.AllIds)
        {
            var relic = Catalog.ById[relicId];
            if (!grouped.TryGetValue(relic.Rarity, out var bucket))
            {
                bucket = new List<RelicData>();
                grouped[relic.Rarity] = bucket;
            }
            bucket.Add(relic);
        }
        return grouped;
    }

    private static readonly Dictionary<string, (string Name, string Desc)> ZhText = new()
    {
        {"lantern", ("提灯", "第1回合获得1点能量。")},
        {"anchor", ("船锚", "第1回合获得8点格挡。")},
        {"dawn_totem", ("黎明图腾", "第1回合额外抽1张牌并获得4点格挡。")},
        {"whetstone", ("磨刀石", "你的攻击额外造成1点伤害。")},
        {"charm", ("幸运符", "每场战斗后恢复5点生命。")},
        {"cinder_tea", ("灰烬茶", "战斗后若本场受过伤，恢复3点生命。")},
        {"echo_coin", ("回响硬币", "战斗中每打出5张牌，抽1张牌。")},
        {"warding_bell", ("守护铃", "若回合结束时没有格挡，获得6点格挡。")},
        {"blood_vial", ("血瓶", "每场战斗后恢复2点生命。")},
        {"storm_feather", ("风暴羽", "每回合打出第3张牌后，下回合获得1点能量。")},
        {"rune_kite", ("符文风筝", "单回合弃掉2张以上牌时，获得4点格挡。")},
        {"thorn_mail", ("荆棘甲", "受到攻击时，对攻击者反弹2点伤害。")},
        {"overclock_core", ("超频核心", "每场战斗第4张技能牌费用变为0。")},
        {"ember_chisel", ("余烬凿", "每当你消耗一张牌，本回合获得1点临时力量。")},
        {"ember_ring", ("余烬之环", "每回合开始时获得1点能量。")},
        {"iron_shell", ("铁甲壳", "每回合开始时获得3点格挡。")},
        {"frozen_lens", ("冰镜", "每回合打出的第一张减益牌额外施加1层。")},
        {"twin_blade_badge", ("双刃徽章", "每回合第一张攻击会以60%伤害额外命中一次。")},
        {"glass_meteor", ("玻璃流星", "攻击额外造成3点伤害，但敌方攻击对你多造成1点伤害。")},
        {"void_hourglass", ("虚空沙漏", "若你的回合没有打出攻击牌，则回合结束时获得1点力量。")},
        {"soul_compass", ("灵魂罗盘", "精英奖励会多提供一个遗物选项。")},
        {"jade_cicada", ("玉蝉", "生命低于50%时，战斗开始获得1层神器。")},
        {"lucky_coin", ("幸运金币", "每场战斗后额外获得5金币。")},
        {"preserved_meat", ("腌肉", "篝火休息回复量+20%。")},
        {"bottled_water", ("瓶装水", "每场战斗开始时获得3点临时格挡。")},
        {"dusty_scroll", ("古旧卷轴", "每场战斗第一张技能牌费用为0。")},
        {"cracked_orb", ("裂痕宝珠", "每回合打出3张攻击牌时，获得2点格挡。")},
        {"twisted_funnel", ("扭曲漏斗", "战斗开始时获得1点临时力量（持续3回合）。")},
        {"hourglass", ("沙漏", "有易伤的敌人受到的所有攻击+1伤害。")},
        {"dream_catcher", ("捕梦网", "在篝火处可以额外升级一张牌。")},
        {"toolbox", ("工具箱", "每场战斗开始时，从3张随机普通牌中选择1张加入手牌。")},
        {"prayer_beads", ("念珠", "每场战斗首次受到伤害减少5点。")},
        {"blue_candle", ("蓝蜡烛", "你可以从弃牌堆打出奇巧牌。")},
        {"shuriken", ("手里剑", "每回合打出3张攻击牌时，获得1点力量。")},
        {"orichalcum", ("奥利哈钢", "若回合结束时没有格挡，获得4点格挡。")},
        {"necronomicon", ("亡灵之书", "每回合第一张2费攻击牌打出两次。")},
        {"tungsten_rod", ("钨棒", "每次受到伤害减少1点。")},
        {"dead_branch", ("枯枝", "每当你消耗一张牌，随机获得一张牌加入手牌。")},
        {"bird_faced_urn", ("鸟面瓮", "每当你打出一张技能牌，回复1点生命。")},
        {"ice_cream", ("冰淇淋", "未使用的能量保留到下一回合。")},
        {"pen_nib", ("笔尖", "每打出的第10张攻击牌造成双倍伤害。")},
        {"mango", ("芒果", "拾取时，最大生命+15。")},
        {"membership_card", ("会员卡", "所有商店价格降低20%。")},
        {"warlord_crown", ("霸王之冠", "每回合开始时获得1点力量。")},
        {"shadow_step", ("影步", "每回合打出的第4张牌费用为0。")},
        {"arcane_battery", ("奥术电池", "每场战斗第一回合获得5点能量。")},
        {"philosopher_stone", ("贤者之石", "每回合+1能量。所有敌人力量+1。")},
        {"cursed_key", ("诅咒钥匙", "每回合+1能量。你不能再打开非Boss宝箱。")},
        {"coffee_dripper", ("咖啡滴滤器", "每回合+1能量。你不能再在篝火休息。")},
        {"black_blood", ("黑血", "每场战斗后回复10点生命（替代6点）。")},
        {"astrolabe", ("星盘", "拾取时，选择并转化3张牌。每回合多抽1张牌。")},
        {"battle_standard", ("战斗旗帜", "每场战斗后回复6点生命。")},
        {"shadow_band", ("暗影指环", "每场战斗开始时多抽1张牌。")},
        {"blood_chalice", ("血杯", "每当你因自己的卡牌受伤时，获得1点力量。")},
        {"self_repair_kit", ("自修工具", "每场战斗首次生命低于50%时，回复15点生命。")},
        {"poison_vial", ("毒液瓶", "对敌人施加易伤时额外+1层。")},
        {"wrist_blade", ("腕刃", "0费攻击牌伤害+2。")},
        {"bandolier", ("弹药带", "每回合开始时多抽1张牌。")},
        {"cracked_core", ("碎晶核心", "每打出第5张牌时，对所有敌人造成4点伤害。")},
        {"spellbook", ("法术书", "每回合第一张技能牌费用-1。")},
        {"molten_egg", ("熔岩蛋", "所有加入牌库的攻击牌自动升级。")},
        {"toxic_egg", ("剧毒蛋", "所有加入牌库的技能牌自动升级。")},
        {"pocket_watch", ("怀表", "每回合打出不超过3张牌时，下回合多抽3张牌。")},
    };

    private static class Catalog
    {
        public static readonly RelicData Fallback = new("lantern", "Lantern", "Gain +1 Energy on turn 1.", "Starter", "Tempo");
        public static readonly Dictionary<string, RelicData> ById = LoadRelics();
        public static readonly IReadOnlyList<string> AllIds = ById.Keys.OrderBy(id => id, StringComparer.Ordinal).ToList();

        private static Dictionary<string, RelicData> LoadRelics()
        {
            var parsed = TryLoadConfiguredRelics();
            return parsed.Count > 0 ? parsed : BuildFallbackRelics();
        }

        private static Dictionary<string, RelicData> TryLoadConfiguredRelics()
        {
            try
            {
                var (json, _) = ReadRelicsJson();
                var dto = JsonSerializer.Deserialize<RelicCatalogDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var result = new Dictionary<string, RelicData>();
                foreach (var entry in dto?.Relics ?? new List<RelicEntryDto>())
                {
                    if (string.IsNullOrWhiteSpace(entry.Id) || string.IsNullOrWhiteSpace(entry.Name)) continue;
                    result[entry.Id] = new RelicData(entry.Id, entry.Name,
                        string.IsNullOrWhiteSpace(entry.Description) ? "No description." : entry.Description,
                        string.IsNullOrWhiteSpace(entry.Rarity) ? "Common" : entry.Rarity,
                        string.IsNullOrWhiteSpace(entry.Archetype) ? "General" : entry.Archetype);
                }
                return result;
            }
            catch { return new Dictionary<string, RelicData>(); }
        }

        private static Dictionary<string, RelicData> BuildFallbackRelics()
        {
            var relics = new[]
            {
                new RelicData("lantern", "Lantern", "Gain +1 Energy on turn 1.", "Starter", "Tempo"),
                new RelicData("anchor", "Anchor", "Gain 8 Block on turn 1.", "Starter", "Block"),
                new RelicData("whetstone", "Whetstone", "Your attacks deal +1 damage.", "Common", "Strike"),
                new RelicData("charm", "Lucky Charm", "Heal 5 HP after each battle.", "Common", "Sustain"),
                new RelicData("ember_ring", "Ember Ring", "Gain +1 Energy at the start of every turn.", "Rare", "Combo"),
                new RelicData("iron_shell", "Iron Shell", "Gain 3 Block at the start of every turn.", "Rare", "Block"),
                new RelicData("blood_vial", "Blood Vial", "Heal 2 HP after each battle.", "Uncommon", "Sustain")
            };
            return relics.ToDictionary(r => r.Id, r => r, StringComparer.Ordinal);
        }

        private static (string Json, string Source) ReadRelicsJson()
        {
            var candidates = new List<string>();
            var envPath = Environment.GetEnvironmentVariable("SLAY_THE_HS_RELICS_JSON");
            if (!string.IsNullOrWhiteSpace(envPath)) candidates.Add(envPath);
            candidates.AddRange(EnumerateCandidates(AppContext.BaseDirectory));
            candidates.AddRange(EnumerateCandidates(Directory.GetCurrentDirectory()));
            candidates.AddRange(EnumerateTestCandidates(AppContext.BaseDirectory));
            candidates.AddRange(EnumerateTestCandidates(Directory.GetCurrentDirectory()));
            if (GameDataAccess.TryReadText(candidates, new[] { "res://Data/relics.json" }, out var json, out var source))
                return (json, source);
            throw new FileNotFoundException("Cannot locate Data/relics.json for relic catalog loading.");
        }

        private static IEnumerable<string> EnumerateCandidates(string startDir)
        {
            var current = new DirectoryInfo(startDir);
            for (var i = 0; i < 8 && current != null; i++)
            {
                yield return Path.Combine(current.FullName, "Data", "relics.json");
                current = current.Parent;
            }
        }

        private static IEnumerable<string> EnumerateTestCandidates(string startDir)
        {
            var current = new DirectoryInfo(startDir);
            for (var i = 0; i < 8 && current != null; i++)
            {
                yield return Path.Combine(current.FullName, "Tests", "CombatLogicTests", "Data", "relics.json");
                current = current.Parent;
            }
        }
    }

    private sealed class RelicCatalogDto { public List<RelicEntryDto> Relics { get; set; } = new(); }
    private sealed class RelicEntryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Rarity { get; set; } = string.Empty;
        public string Archetype { get; set; } = string.Empty;
    }
}
