using System;
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
    DrawCards,
    GainStrength,
    GainEnergy,
    Heal
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
    private static readonly CardCatalog Catalog = CardCatalog.Load();

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string DescriptionZh { get; }
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
        string descriptionZh,
        CardKind kind,
        int cost,
        IReadOnlyList<CardEffectData> effects)
    {
        Id = id;
        Name = name;
        Description = description;
        DescriptionZh = string.IsNullOrWhiteSpace(descriptionZh) ? description : descriptionZh;
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
        return $"{Name}\n{LocalizationSettings.CostLabel()}: {Cost}\n{GetLocalizedDescription()}";
    }

    public string GetLocalizedDescription()
    {
        return LocalizationSettings.CurrentLanguage == GameLanguage.ZhHans ? DescriptionZh : Description;
    }

    public bool HasEffect(CardEffectType type)
    {
        return Effects.Any(effect => effect.Type == type);
    }

    public static CardData CreateById(string id)
    {
        if (Catalog.CardsById.TryGetValue(id, out var card))
        {
            return CloneCard(card);
        }

        if (Catalog.CardsById.TryGetValue("strike", out var fallback))
        {
            return CloneCard(fallback);
        }

        throw new InvalidOperationException("No fallback card 'strike' configured in cards.json.");
    }

    public static List<string> StarterDeckIds()
    {
        return new List<string>(Catalog.StarterDeck);
    }

    public static List<string> RewardPoolIds()
    {
        return new List<string>(Catalog.RewardPool);
    }

    public static List<CardData> AllCards()
    {
        return Catalog.CardsById.Values
            .Select(CloneCard)
            .OrderBy(card => card.Cost)
            .ThenBy(card => card.Name)
            .ToList();
    }

    private static CardData CloneCard(CardData source)
    {
        var effects = source.Effects
            .Select(effect => new CardEffectData(
                effect.Type,
                effect.Target,
                effect.Amount,
                effect.Repeat,
                effect.UseAttackerStrength,
                effect.UseTargetVulnerable,
                effect.FlatBonus))
            .ToList();

        return new CardData(
            source.Id,
            source.Name,
            source.Description,
            source.DescriptionZh,
            source.Kind,
            source.Cost,
            effects);
    }

    private sealed class CardCatalog
    {
        public Dictionary<string, CardData> CardsById { get; }
        public List<string> StarterDeck { get; }
        public List<string> RewardPool { get; }

        private CardCatalog(Dictionary<string, CardData> cardsById, List<string> starterDeck, List<string> rewardPool)
        {
            CardsById = cardsById;
            StarterDeck = starterDeck;
            RewardPool = rewardPool;
        }

        public static CardCatalog Load()
        {
            var path = CardCatalogPersistence.ResolveCardsJsonPath();
            var dto = CardCatalogPersistence.LoadFromFile(path);

            var cardsById = new Dictionary<string, CardData>();
            foreach (var cardDto in dto.Cards)
            {
                if (string.IsNullOrWhiteSpace(cardDto.Id))
                {
                    throw new InvalidOperationException("Card id is required in cards.json.");
                }

                var effects = new List<CardEffectData>();
                foreach (var effectDto in cardDto.Effects)
                {
                    effects.Add(new CardEffectData(
                        ParseEnum<CardEffectType>(effectDto.Type, "effect type"),
                        ParseEnum<CardEffectTarget>(effectDto.Target, "effect target"),
                        effectDto.Amount,
                        effectDto.Repeat <= 0 ? 1 : effectDto.Repeat,
                        effectDto.UseAttackerStrength,
                        effectDto.UseTargetVulnerable,
                        effectDto.FlatBonus));
                }

                var card = new CardData(
                    cardDto.Id,
                    cardDto.Name ?? cardDto.Id,
                    cardDto.Description ?? string.Empty,
                    cardDto.DescriptionZh ?? cardDto.Description ?? string.Empty,
                    ParseEnum<CardKind>(cardDto.Kind, "card kind"),
                    cardDto.Cost,
                    effects);

                cardsById[card.Id] = card;
            }

            var starterDeck = dto.StarterDeck;
            var rewardPool = dto.RewardPool;
            return new CardCatalog(cardsById, starterDeck, rewardPool);
        }

        private static T ParseEnum<T>(string? raw, string label) where T : struct
        {
            if (!string.IsNullOrWhiteSpace(raw) && Enum.TryParse<T>(raw, ignoreCase: true, out var parsed))
            {
                return parsed;
            }

            throw new InvalidOperationException($"Invalid {label}: '{raw}'.");
        }
    }

}

public enum GameLanguage
{
    En,
    ZhHans
}

public static class LocalizationSettings
{
    public static GameLanguage CurrentLanguage { get; private set; } = GameLanguage.ZhHans;

    public static event Action? LanguageChanged;

    public static void ToggleLanguage()
    {
        SetLanguage(CurrentLanguage == GameLanguage.En ? GameLanguage.ZhHans : GameLanguage.En);
    }

    public static void SetLanguage(GameLanguage language)
    {
        if (CurrentLanguage == language)
        {
            return;
        }

        CurrentLanguage = language;
        LanguageChanged?.Invoke();
    }

    public static string CostLabel()
    {
        return CurrentLanguage == GameLanguage.ZhHans ? "费用" : "Cost";
    }

    public static string LanguageButtonText()
    {
        return CurrentLanguage == GameLanguage.ZhHans ? "语言：中文" : "Language: English";
    }

    public static string HighlightCardDescription(string text)
    {
        if (CurrentLanguage == GameLanguage.ZhHans)
        {
            return text
                .Replace("造成", "[color=#fca5a5]造成[/color]")
                .Replace("获得", "[color=#93c5fd]获得[/color]")
                .Replace("格挡", "[color=#93c5fd]格挡[/color]")
                .Replace("易伤", "[color=#e9d5ff]易伤[/color]")
                .Replace("抽", "[color=#a5f3fc]抽[/color]")
                .Replace("回复", "[color=#86efac]回复[/color]")
                .Replace("伤害", "[color=#fda4af]伤害[/color]");
        }

        return text
            .Replace("Deal", "[color=#fca5a5]Deal[/color]")
            .Replace("Gain", "[color=#93c5fd]Gain[/color]")
            .Replace("Block", "[color=#93c5fd]Block[/color]")
            .Replace("Vulnerable", "[color=#e9d5ff]Vulnerable[/color]")
            .Replace("Draw", "[color=#a5f3fc]Draw[/color]")
            .Replace("Heal", "[color=#86efac]Heal[/color]")
            .Replace("damage", "[color=#fda4af]damage[/color]");
    }
}
