using System;
using System.Collections.Generic;
using System.Linq;

public sealed class DeckPresetData
{
    public string Id { get; }
    public string Name { get; }
    public string NameZh { get; }
    public string Description { get; }
    public string DescriptionZh { get; }
    public IReadOnlyList<string> CardIds { get; }

    public DeckPresetData(
        string id,
        string name,
        string nameZh,
        string description,
        string descriptionZh,
        IReadOnlyList<string> cardIds)
    {
        Id = id;
        Name = name;
        NameZh = nameZh;
        Description = description;
        DescriptionZh = descriptionZh;
        CardIds = cardIds;
    }

    public string LocalizedName => LocalizationSettings.CurrentLanguage == GameLanguage.ZhHans ? NameZh : Name;

    public string LocalizedDescription => LocalizationSettings.CurrentLanguage == GameLanguage.ZhHans
        ? DescriptionZh
        : Description;
}

public static class DeckPresetCatalog
{
    private static readonly List<DeckPresetData> Presets = new()
    {
        new DeckPresetData(
            id: "starter",
            name: "Balanced Starter",
            nameZh: "均衡起手",
            description: "Stable attack + block with simple scaling.",
            descriptionZh: "攻防均衡，带基础成长。",
            cardIds: CardData.StarterDeckIds()),

        new DeckPresetData(
            id: "infinite_cycle",
            name: "Infinite Cycle",
            nameZh: "无限循环",
            description: "Zero-cost draw/energy loop with explosive turn chains.",
            descriptionZh: "0费抽牌回能循环，追求回合连锁爆发。",
            cardIds: new List<string>
            {
                "spark_loop", "spark_loop", "spark_loop",
                "arcane_recycle", "arcane_recycle",
                "hand_overflow", "hand_overflow",
                "mana_turbine", "overclock",
                "arcane_barrage", "quick_slash"
            }),

        new DeckPresetData(
            id: "infinite_fireball",
            name: "Infinite Fireball",
            nameZh: "无限火球",
            description: "Multi-hit spell core, stacking energy then burst to finish.",
            descriptionZh: "多段法术为核心，先攒能量再一回合爆发斩杀。",
            cardIds: new List<string>
            {
                "infinite_fireball", "infinite_fireball",
                "ember_wheel", "ember_wheel",
                "arcane_barrage", "arcane_barrage",
                "spark_loop", "arcane_recycle",
                "mana_turbine", "battle_focus", "war_cry"
            }),

        new DeckPresetData(
            id: "death_legion",
            name: "Death Legion",
            nameZh: "亡灵军势",
            description: "Vulnerable spreading + sustain, overwhelms over long fights.",
            descriptionZh: "群体易伤扩散 + 吸血续航，长线压制。",
            cardIds: new List<string>
            {
                "grave_whisper", "grave_whisper",
                "bone_shrapnel", "bone_shrapnel",
                "death_chorus", "soul_siphon",
                "rending_wave", "meteor_shower",
                "phoenix_cycle", "fortify", "reaper_touch"
            })
    };

    public static IReadOnlyList<DeckPresetData> All()
    {
        return Presets;
    }

    public static DeckPresetData Resolve(string? id)
    {
        var found = Presets.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
        return found ?? Presets[0];
    }
}
