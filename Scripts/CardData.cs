using System.Collections.Generic;
using System.Linq;

public enum CardKind
{
    Attack,
    Skill
}

public enum CardEffectType
{
    Damage,
    GainBlock,
    ApplyVulnerable,
    DrawCards
}

public enum CardEffectTarget
{
    Player,
    SelectedEnemy,
    AllEnemies
}

public sealed class CardEffectData
{
    public CardEffectType Type { get; }
    public CardEffectTarget Target { get; }
    public int Amount { get; }
    public int Repeat { get; }
    public bool UseAttackerStrength { get; }
    public bool UseTargetVulnerable { get; }
    public int FlatBonus { get; }

    public CardEffectData(
        CardEffectType type,
        CardEffectTarget target,
        int amount,
        int repeat = 1,
        bool useAttackerStrength = true,
        bool useTargetVulnerable = true,
        int flatBonus = 0)
    {
        Type = type;
        Target = target;
        Amount = amount;
        Repeat = repeat < 1 ? 1 : repeat;
        UseAttackerStrength = useAttackerStrength;
        UseTargetVulnerable = useTargetVulnerable;
        FlatBonus = flatBonus;
    }
}

public sealed class CardData
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public CardKind Kind { get; }
    public int Cost { get; }
    public IReadOnlyList<CardEffectData> Effects { get; }

    // Legacy aggregates kept for compatibility with existing UI and logic helpers.
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
        IReadOnlyList<CardEffectData> effects)
    {
        Id = id;
        Name = name;
        Description = description;
        Kind = kind;
        Cost = cost;
        Effects = effects;

        Damage = Effects
            .Where(e => e.Type == CardEffectType.Damage && e.Target == CardEffectTarget.SelectedEnemy)
            .Sum(e => e.Amount * e.Repeat);
        Block = Effects
            .Where(e => e.Type == CardEffectType.GainBlock && e.Target == CardEffectTarget.Player)
            .Sum(e => e.Amount * e.Repeat);
        ApplyVulnerable = Effects
            .Where(e => e.Type == CardEffectType.ApplyVulnerable && e.Target == CardEffectTarget.SelectedEnemy)
            .Sum(e => e.Amount * e.Repeat);
        DrawCount = Effects
            .Where(e => e.Type == CardEffectType.DrawCards)
            .Sum(e => e.Amount * e.Repeat);
    }

    public string ToCardText()
    {
        return $"{Name}\\nCost: {Cost}\\n{Description}";
    }

    public bool HasEffect(CardEffectType type)
    {
        return Effects.Any(effect => effect.Type == type);
    }

    public static CardData CreateById(string id)
    {
        return id switch
        {
            "strike" => new CardData(
                "strike",
                "Strike",
                "Deal 6 damage.",
                CardKind.Attack,
                1,
                new List<CardEffectData>
                {
                    new(CardEffectType.Damage, CardEffectTarget.SelectedEnemy, 6)
                }),
            "defend" => new CardData(
                "defend",
                "Defend",
                "Gain 5 Block.",
                CardKind.Skill,
                1,
                new List<CardEffectData>
                {
                    new(CardEffectType.GainBlock, CardEffectTarget.Player, 5, useAttackerStrength: false, useTargetVulnerable: false)
                }),
            "heavy_slash" => new CardData(
                "heavy_slash",
                "Heavy Slash",
                "Deal 12 damage.",
                CardKind.Attack,
                2,
                new List<CardEffectData>
                {
                    new(CardEffectType.Damage, CardEffectTarget.SelectedEnemy, 12)
                }),
            "bash" => new CardData(
                "bash",
                "Bash",
                "Deal 8 damage. Apply 2 Vulnerable.",
                CardKind.Attack,
                2,
                new List<CardEffectData>
                {
                    new(CardEffectType.Damage, CardEffectTarget.SelectedEnemy, 8),
                    new(CardEffectType.ApplyVulnerable, CardEffectTarget.SelectedEnemy, 2, useAttackerStrength: false)
                }),
            "shrug" => new CardData(
                "shrug",
                "Shrug It Off",
                "Gain 8 Block. Draw 1 card.",
                CardKind.Skill,
                1,
                new List<CardEffectData>
                {
                    new(CardEffectType.GainBlock, CardEffectTarget.Player, 8, useAttackerStrength: false, useTargetVulnerable: false),
                    new(CardEffectType.DrawCards, CardEffectTarget.Player, 1, useAttackerStrength: false, useTargetVulnerable: false)
                }),
            "quick_slash" => new CardData(
                "quick_slash",
                "Quick Slash",
                "Deal 7 damage. Draw 1 card.",
                CardKind.Attack,
                1,
                new List<CardEffectData>
                {
                    new(CardEffectType.Damage, CardEffectTarget.SelectedEnemy, 7),
                    new(CardEffectType.DrawCards, CardEffectTarget.Player, 1, useAttackerStrength: false, useTargetVulnerable: false)
                }),
            "whirlwind" => new CardData(
                "whirlwind",
                "Whirlwind",
                "Deal 4 damage to all enemies twice.",
                CardKind.Attack,
                2,
                new List<CardEffectData>
                {
                    new(CardEffectType.Damage, CardEffectTarget.AllEnemies, 4, repeat: 2)
                }),
            _ => CreateById("strike")
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
            "whirlwind",
            "bash",
            "strike",
            "defend"
        };
    }
}
