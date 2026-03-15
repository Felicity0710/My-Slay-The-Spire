using System.Collections.Generic;

public enum CardKind
{
    Attack,
    Skill
}

public sealed class CardData
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public CardKind Kind { get; }
    public int Cost { get; }
    public int Damage { get; }
    public int Block { get; }
    public int ApplyVulnerable { get; }
    public int DrawCount { get; }

    public CardData(
        string id,
        string name,
        string description,
        CardKind kind,
        int cost,
        int damage,
        int block,
        int applyVulnerable,
        int drawCount)
    {
        Id = id;
        Name = name;
        Description = description;
        Kind = kind;
        Cost = cost;
        Damage = damage;
        Block = block;
        ApplyVulnerable = applyVulnerable;
        DrawCount = drawCount;
    }

    public string ToCardText()
    {
        return $"{Name}\\nCost: {Cost}\\n{Description}";
    }

    public static CardData CreateById(string id)
    {
        return id switch
        {
            "strike" => new CardData("strike", "Strike", "Deal 6 damage.", CardKind.Attack, 1, 6, 0, 0, 0),
            "defend" => new CardData("defend", "Defend", "Gain 5 Block.", CardKind.Skill, 1, 0, 5, 0, 0),
            "heavy_slash" => new CardData("heavy_slash", "Heavy Slash", "Deal 12 damage.", CardKind.Attack, 2, 12, 0, 0, 0),
            "bash" => new CardData("bash", "Bash", "Deal 8 damage. Apply 2 Vulnerable.", CardKind.Attack, 2, 8, 0, 2, 0),
            "shrug" => new CardData("shrug", "Shrug It Off", "Gain 8 Block. Draw 1 card.", CardKind.Skill, 1, 0, 8, 0, 1),
            "quick_slash" => new CardData("quick_slash", "Quick Slash", "Deal 7 damage. Draw 1 card.", CardKind.Attack, 1, 7, 0, 0, 1),
            _ => new CardData("strike", "Strike", "Deal 6 damage.", CardKind.Attack, 1, 6, 0, 0, 0)
        };
    }

    public static List<string> StarterDeckIds()
    {
        var ids = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            ids.Add("strike");
            ids.Add("defend");
        }

        ids.Add("bash");
        return ids;
    }

    public static List<string> RewardPoolIds()
    {
        return new List<string>
        {
            "heavy_slash",
            "shrug",
            "quick_slash",
            "bash",
            "strike",
            "defend"
        };
    }
}
