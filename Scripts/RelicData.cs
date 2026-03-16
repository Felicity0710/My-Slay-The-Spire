using System.Collections.Generic;

public sealed class RelicData
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }

    public RelicData(string id, string name, string description)
    {
        Id = id;
        Name = name;
        Description = description;
    }

    public string ToRelicText()
    {
        return $"{Name}\\n{Description}";
    }

    public static IReadOnlyList<string> AllRelicIds()
    {
        return new[]
        {
            "lantern",
            "anchor",
            "whetstone",
            "charm",
            "ember_ring",
            "iron_shell",
            "blood_vial"
        };
    }

    public static RelicData CreateById(string id)
    {
        return id switch
        {
            "lantern" => new RelicData("lantern", "Lantern", "Gain +1 Energy on turn 1."),
            "anchor" => new RelicData("anchor", "Anchor", "Gain 8 Block on turn 1."),
            "whetstone" => new RelicData("whetstone", "Whetstone", "Your attacks deal +1 damage."),
            "charm" => new RelicData("charm", "Lucky Charm", "Heal 5 HP after each battle."),
            "ember_ring" => new RelicData("ember_ring", "Ember Ring", "Gain +1 Energy at the start of every turn."),
            "iron_shell" => new RelicData("iron_shell", "Iron Shell", "Gain 3 Block at the start of every turn."),
            "blood_vial" => new RelicData("blood_vial", "Blood Vial", "Heal 2 HP after each battle."),
            _ => new RelicData("lantern", "Lantern", "Gain +1 Energy on turn 1.")
        };
    }
}
