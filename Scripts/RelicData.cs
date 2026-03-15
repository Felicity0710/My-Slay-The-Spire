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

    public static RelicData CreateById(string id)
    {
        return id switch
        {
            "lantern" => new RelicData("lantern", "Lantern", "Gain +1 Energy on turn 1."),
            "anchor" => new RelicData("anchor", "Anchor", "Gain 8 Block on turn 1."),
            "whetstone" => new RelicData("whetstone", "Whetstone", "Your attacks deal +1 damage."),
            "charm" => new RelicData("charm", "Lucky Charm", "Heal 5 HP after each battle."),
            _ => new RelicData("lantern", "Lantern", "Gain +1 Energy on turn 1.")
        };
    }
}
