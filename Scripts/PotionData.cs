using System.Collections.Generic;

public sealed class PotionData
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }

    public PotionData(string id, string name, string description)
    {
        Id = id;
        Name = name;
        Description = description;
    }

    public static IReadOnlyList<string> AllPotionIds()
    {
        return new[]
        {
            "healing_potion",
            "strength_potion",
            "swift_potion",
            "guard_potion",
            "fury_potion"
        };
    }

    public static PotionData CreateById(string id)
    {
        return id switch
        {
            "healing_potion" => new PotionData("healing_potion", "Healing Potion", "Restore a chunk of HP when used."),
            "strength_potion" => new PotionData("strength_potion", "Strength Potion", "Gain temporary Strength in combat."),
            "swift_potion" => new PotionData("swift_potion", "Swift Potion", "Gain immediate Energy this turn."),
            "guard_potion" => new PotionData("guard_potion", "Guard Potion", "Gain a burst of Block."),
            "fury_potion" => new PotionData("fury_potion", "Fury Potion", "Boost attack output for a turn."),
            _ => new PotionData("healing_potion", "Healing Potion", "Restore a chunk of HP when used.")
        };
    }
}
