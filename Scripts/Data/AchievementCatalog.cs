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

    public string LocalizedName => LocalizationService.Get(NameKey, DefaultName);
    public string LocalizedDescription => LocalizationService.Get(DescriptionKey, DefaultDescription);
}

// Pure catalog of achievements players can earn. Unlock progress lives
// elsewhere (when implemented); the compendium screen reads only the catalog.
public static class AchievementCatalog
{
    private static readonly List<AchievementData> Entries = new()
    {
        new AchievementData(
            id: "first_blood",
            icon: "🩸",
            nameKey: "achievement.first_blood.name",
            defaultName: "First Blood",
            descriptionKey: "achievement.first_blood.description",
            defaultDescription: "Defeat your first enemy."),

        new AchievementData(
            id: "first_floor",
            icon: "🏛",
            nameKey: "achievement.first_floor.name",
            defaultName: "Beneath the Tower",
            descriptionKey: "achievement.first_floor.description",
            defaultDescription: "Reach the second floor of the dungeon."),

        new AchievementData(
            id: "act_one_clear",
            icon: "🗺",
            nameKey: "achievement.act_one_clear.name",
            defaultName: "Act I Conqueror",
            descriptionKey: "achievement.act_one_clear.description",
            defaultDescription: "Clear all encounters in the first act."),

        new AchievementData(
            id: "act_two_clear",
            icon: "⛰",
            nameKey: "achievement.act_two_clear.name",
            defaultName: "Act II Conqueror",
            descriptionKey: "achievement.act_two_clear.description",
            defaultDescription: "Clear all encounters in the second act."),

        new AchievementData(
            id: "the_finisher",
            icon: "👑",
            nameKey: "achievement.the_finisher.name",
            defaultName: "The Finisher",
            descriptionKey: "achievement.the_finisher.description",
            defaultDescription: "Clear all three acts in a single run."),

        new AchievementData(
            id: "perfectionist",
            icon: "✨",
            nameKey: "achievement.perfectionist.name",
            defaultName: "Perfectionist",
            descriptionKey: "achievement.perfectionist.description",
            defaultDescription: "Finish a battle without losing any HP."),

        new AchievementData(
            id: "merchant_robber",
            icon: "🗡",
            nameKey: "achievement.merchant_robber.name",
            defaultName: "Merchant's Bane",
            descriptionKey: "achievement.merchant_robber.description",
            defaultDescription: "Successfully rob a shop."),

        new AchievementData(
            id: "deck_master",
            icon: "🎴",
            nameKey: "achievement.deck_master.name",
            defaultName: "Deck Master",
            descriptionKey: "achievement.deck_master.description",
            defaultDescription: "Carry a deck of 25 cards into a fight."),

        new AchievementData(
            id: "relic_hoarder",
            icon: "💎",
            nameKey: "achievement.relic_hoarder.name",
            defaultName: "Relic Hoarder",
            descriptionKey: "achievement.relic_hoarder.description",
            defaultDescription: "Collect 5 different relics in a single run."),

        new AchievementData(
            id: "elite_hunter",
            icon: "⚔",
            nameKey: "achievement.elite_hunter.name",
            defaultName: "Elite Hunter",
            descriptionKey: "achievement.elite_hunter.description",
            defaultDescription: "Defeat 3 elite enemies in a single run."),

        new AchievementData(
            id: "potion_brewer",
            icon: "🧪",
            nameKey: "achievement.potion_brewer.name",
            defaultName: "Potion Brewer",
            descriptionKey: "achievement.potion_brewer.description",
            defaultDescription: "Hold three potions at once."),

        new AchievementData(
            id: "explorer",
            icon: "🧭",
            nameKey: "achievement.explorer.name",
            defaultName: "Explorer",
            descriptionKey: "achievement.explorer.description",
            defaultDescription: "Visit at least one of every map node type.")
    };

    public static IReadOnlyList<AchievementData> All() => Entries;
}
