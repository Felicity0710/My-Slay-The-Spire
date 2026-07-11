using System.Collections.Generic;

public sealed class PotionData
{
    public string Id { get; }
    public string Name { get; }
    public string NameZh { get; }
    public string Description { get; }
    public string DescriptionZh { get; }
    public int HealAmount { get; }
    public int StrengthAmount { get; }
    public int EnergyAmount { get; }
    public int BlockAmount { get; }
    public int MaxHpAmount { get; }

    public string DisplayName =>
        LocalizationSettings.CurrentLanguage == GameLanguage.ZhHans ? NameZh : Name;
    public string DisplayDescription =>
        LocalizationSettings.CurrentLanguage == GameLanguage.ZhHans ? DescriptionZh : Description;

    public PotionData(string id, string name, string nameZh, string description, string descriptionZh,
        int heal = 0, int strength = 0, int energy = 0, int block = 0, int maxHp = 0)
    {
        Id = id;
        Name = name;
        NameZh = nameZh;
        Description = description;
        DescriptionZh = descriptionZh;
        HealAmount = heal;
        StrengthAmount = strength;
        EnergyAmount = energy;
        BlockAmount = block;
        MaxHpAmount = maxHp;
    }

    public static IReadOnlyList<string> AllPotionIds() => new[]
    {
        "healing_potion", "greater_healing_potion",
        "strength_potion", "guard_potion",
        "swift_potion", "fury_potion",
        "block_potion", "max_hp_potion",
        "energy_potion", "vampire_potion"
    };

    public static PotionData CreateById(string id)
    {
        return id switch
        {
            "healing_potion" => new PotionData("healing_potion",
                "Healing Potion", "治疗药水", "Heal 20 HP.", "回复20点生命。", heal: 20),
            "greater_healing_potion" => new PotionData("greater_healing_potion",
                "Greater Healing Potion", "强效治疗药水", "Heal 40 HP.", "回复40点生命。", heal: 40),
            "strength_potion" => new PotionData("strength_potion",
                "Strength Potion", "力量药水", "Gain 2 Strength for this combat.", "本场战斗获得2点力量。", strength: 2),
            "guard_potion" => new PotionData("guard_potion",
                "Guard Potion", "守护药水", "Gain 15 Block.", "获得15点格挡。", block: 15),
            "swift_potion" => new PotionData("swift_potion",
                "Swift Potion", "迅捷药水", "Gain 2 Energy this turn.", "本回合获得2点能量。", energy: 2),
            "fury_potion" => new PotionData("fury_potion",
                "Fury Potion", "狂暴药水", "Gain 2 Strength and 1 Energy.", "获得2点力量和1点能量。", strength: 2, energy: 1),
            "block_potion" => new PotionData("block_potion",
                "Block Tonic", "格挡补剂", "Gain 25 Block.", "获得25点格挡。", block: 25),
            "max_hp_potion" => new PotionData("max_hp_potion",
                "Elixir of Vitality", "活力灵药", "Permanently increase Max HP by 8 and heal 8 HP.", "永久增加8点最大生命并回复8点生命。", heal: 8, maxHp: 8),
            "energy_potion" => new PotionData("energy_potion",
                "Energy Draught", "能量药剂", "Gain 3 Energy this turn.", "本回合获得3点能量。", energy: 3),
            "vampire_potion" => new PotionData("vampire_potion",
                "Vampire Philter", "吸血鬼魔药", "Deal 10 damage to ALL enemies and heal 5 HP.", "对所有敌人造成10点伤害并回复5点生命。", heal: 5),
            _ => CreateById("healing_potion")
        };
    }

    public static IReadOnlyList<string> NormalRewardPool() => new[]
    {
        "healing_potion", "strength_potion", "guard_potion", "swift_potion", "block_potion"
    };

    public static IReadOnlyList<string> EliteRewardPool() => new[]
    {
        "healing_potion", "greater_healing_potion", "strength_potion",
        "guard_potion", "swift_potion", "fury_potion",
        "block_potion", "max_hp_potion", "energy_potion", "vampire_potion"
    };
}
