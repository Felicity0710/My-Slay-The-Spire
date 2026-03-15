using Godot;

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

    public CardData(string id, string name, string description, CardKind kind, int cost, int damage, int block)
    {
        Id = id;
        Name = name;
        Description = description;
        Kind = kind;
        Cost = cost;
        Damage = damage;
        Block = block;
    }

    public CardData Clone()
    {
        return new CardData(Id, Name, Description, Kind, Cost, Damage, Block);
    }

    public string ToCardText()
    {
        return $"{Name}\nCost: {Cost}\n{Description}";
    }

    public static CardData Strike()
    {
        return new CardData("strike", "Strike", "Deal 6 damage.", CardKind.Attack, 1, 6, 0);
    }

    public static CardData Defend()
    {
        return new CardData("defend", "Defend", "Gain 5 Block.", CardKind.Skill, 1, 0, 5);
    }

    public static CardData HeavySlash()
    {
        return new CardData("heavy_slash", "Heavy Slash", "Deal 12 damage.", CardKind.Attack, 2, 12, 0);
    }
}