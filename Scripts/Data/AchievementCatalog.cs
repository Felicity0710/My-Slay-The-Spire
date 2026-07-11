using System.Collections.Generic;

public sealed class AchievementData
{
    public string Id { get; }
    public string NameKey { get; }
    public string DescriptionKey { get; }
    public string Icon { get; }
    public string DefaultName { get; }
    public string DefaultDescription { get; }

    public AchievementData(string id, string icon, string nameKey, string defaultName, string descriptionKey, string defaultDescription)
    {
        Id = id;
        Icon = icon;
        NameKey = nameKey;
        DefaultName = defaultName;
        DescriptionKey = descriptionKey;
        DefaultDescription = defaultDescription;
    }

    public string LocalizedName =>
        LocalizationSettings.CurrentLanguage == GameLanguage.ZhHans && ZhText.TryGetValue(Id, out var zh) ? zh.Name : DefaultName;
    public string LocalizedDescription =>
        LocalizationSettings.CurrentLanguage == GameLanguage.ZhHans && ZhText.TryGetValue(Id, out var zh2) ? zh2.Desc : DefaultDescription;

    private static readonly Dictionary<string, (string Name, string Desc)> ZhText = new()
    {
        ["first_blood"] = ("第一滴血", "击败你的第一个敌人。"),
        ["first_floor"] = ("塔下初探", "抵达地牢第二层。"),
        ["act_one_clear"] = ("第一章征服者", "击败第一章Boss。"),
        ["act_two_clear"] = ("第二章征服者", "击败第二章Boss。"),
        ["the_finisher"] = ("终结者", "单次通关全部三个章节。"),
        ["perfectionist"] = ("完美主义", "无伤完成一场战斗。"),
        ["heavy_hitter"] = ("重击手", "单张卡牌造成50+伤害。"),
        ["untouchable"] = ("坚不可摧", "单回合获得30+格挡。"),
        ["one_turn_kill"] = ("速杀者", "5回合内击败一个Boss。"),
        ["survivor"] = ("幸存者", "以5点或更少生命值赢得一场战斗。"),
        ["deck_master"] = ("卡组大师", "持有30张以上的卡组。"),
        ["relic_hoarder"] = ("遗物收藏家", "单局收集8个不同遗物。"),
        ["potion_brewer"] = ("炼药大师", "同时持有三瓶药水。"),
        ["minimalist"] = ("极简主义", "以12张或更少的卡组通关。"),
        ["elite_hunter"] = ("精英猎人", "单局击败3个精英敌人。"),
        ["merchant_robber"] = ("商人之灾", "成功抢劫一次商店。"),
        ["explorer"] = ("探险家", "同一章节内访问所有类型的节点。"),
        ["pacifist"] = ("和平主义者", "单局完成3次不战斗的事件。"),
        ["iron_might"] = ("铁卫之力", "使用铁卫先锋时，单场战斗达到10+力量。"),
        ["shadow_dance"] = ("暗影之舞", "使用幻影舞者时，单回合打出8+张牌。"),
        ["storm_unleashed"] = ("风暴降临", "使用风暴法师时，单回合造成100+伤害。"),
        ["poverty"] = ("贫困通关", "以0金币通关。"),
        ["full_health_victory"] = ("毫发无伤", "满血击败一个Boss。"),
        ["card_collector"] = ("卡牌收集者", "单局发现某一角色的全部卡牌。"),
    };
}

public static class AchievementCatalog
{
    private static readonly List<AchievementData> Entries = new()
    {
        // === Milestone Achievements ===
        new("first_blood", "🩸", "achievement.first_blood.name", "First Blood",
            "achievement.first_blood.description", "Defeat your first enemy."),
        new("first_floor", "🏛", "achievement.first_floor.name", "Beneath the Tower",
            "achievement.first_floor.description", "Reach the second floor of the dungeon."),
        new("act_one_clear", "🗺", "achievement.act_one_clear.name", "Act I Conqueror",
            "achievement.act_one_clear.description", "Defeat the Act I boss."),
        new("act_two_clear", "⛰", "achievement.act_two_clear.name", "Act II Conqueror",
            "achievement.act_two_clear.description", "Defeat the Act II boss."),
        new("the_finisher", "👑", "achievement.the_finisher.name", "The Finisher",
            "achievement.the_finisher.description", "Clear all three acts in a single run."),

        // === Combat Achievements ===
        new("perfectionist", "✨", "achievement.perfectionist.name", "Perfectionist",
            "achievement.perfectionist.description", "Finish a battle without losing any HP."),
        new("heavy_hitter", "💥", "achievement.heavy_hitter.name", "Heavy Hitter",
            "achievement.heavy_hitter.description", "Deal 50+ damage with a single card."),
        new("untouchable", "🛡", "achievement.untouchable.name", "Untouchable",
            "achievement.untouchable.description", "Gain 30+ Block in a single turn."),
        new("one_turn_kill", "⚡", "achievement.one_turn_kill.name", "Speed Demon",
            "achievement.one_turn_kill.description", "Defeat a boss in 5 turns or fewer."),
        new("survivor", "💪", "achievement.survivor.name", "Survivor",
            "achievement.survivor.description", "Win a battle with 5 or less HP remaining."),

        // === Collection Achievements ===
        new("deck_master", "🎴", "achievement.deck_master.name", "Deck Master",
            "achievement.deck_master.description", "Have a deck of 30+ cards."),
        new("relic_hoarder", "💎", "achievement.relic_hoarder.name", "Relic Hoarder",
            "achievement.relic_hoarder.description", "Collect 8 different relics in a single run."),
        new("potion_brewer", "🧪", "achievement.potion_brewer.name", "Potion Brewer",
            "achievement.potion_brewer.description", "Hold three potions at once."),
        new("minimalist", "🃏", "achievement.minimalist.name", "Minimalist",
            "achievement.minimalist.description", "Win a run with a deck of 12 or fewer cards."),

        // === Playstyle Achievements ===
        new("elite_hunter", "⚔", "achievement.elite_hunter.name", "Elite Hunter",
            "achievement.elite_hunter.description", "Defeat 3 elite enemies in a single run."),
        new("merchant_robber", "🗡", "achievement.merchant_robber.name", "Merchant's Bane",
            "achievement.merchant_robber.description", "Successfully rob a shop."),
        new("explorer", "🧭", "achievement.explorer.name", "Explorer",
            "achievement.explorer.description", "Visit at least one of every map node type in a single act."),
        new("pacifist", "☮", "achievement.pacifist.name", "Pacifist",
            "achievement.pacifist.description", "Complete 3 events without fighting in a single run."),

        // === Character-Specific Achievements ===
        new("iron_might", "⚔️", "achievement.iron_might.name", "Iron Might",
            "achievement.iron_might.description", "As Iron Vanguard, reach 10+ Strength in a single battle."),
        new("shadow_dance", "🌪️", "achievement.shadow_dance.name", "Shadow Dance",
            "achievement.shadow_dance.description", "As Phantom Dancer, play 8+ cards in a single turn."),
        new("storm_unleashed", "⚡", "achievement.storm_unleashed.name", "Storm Unleashed",
            "achievement.storm_unleashed.description", "As Storm Mage, deal 100+ damage in a single turn."),

        // === Challenge Achievements ===
        new("poverty", "🪙", "achievement.poverty.name", "Poverty Run",
            "achievement.poverty.description", "Win a run with 0 gold remaining."),
        new("full_health_victory", "❤", "achievement.full_health_victory.name", "Untouched",
            "achievement.full_health_victory.description", "Defeat a boss while at full HP."),
        new("card_collector", "📚", "achievement.card_collector.name", "Card Collector",
            "achievement.card_collector.description", "Discover every card from one character's pool in a single run."),
    };

    public static IReadOnlyList<AchievementData> All() => Entries;
}
