using Godot;
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
    Heal,
    DiscardCards
}

public enum CardEffectTarget
{
    Player,
    SelectedEnemy,
    AllEnemies
}

public enum CardKeyword
{
    Retain,
    Exhaust,
    Curious
}

public static class CardUpgradeRules
{
    public static bool CardIdHasUpgrade(string cardId)
    {
        if (string.IsNullOrEmpty(cardId))
        {
            return false;
        }

        if (cardId.EndsWith("+", StringComparison.Ordinal))
        {
            return false;
        }

        return CardData.CreateById(cardId).Upgrade != null;
    }

    public static string MaybeUpgrade(string cardId, double chance, Random rng)
    {
        if (!CardIdHasUpgrade(cardId))
        {
            return cardId;
        }

        if (chance <= 0.0)
        {
            return cardId;
        }

        if (chance >= 1.0 || (rng != null && rng.NextDouble() < chance))
        {
            return cardId + "+";
        }

        return cardId;
    }
}

public sealed class CardUpgradeRecipe
{
    public int CostDelta { get; }
    public int ReplayCountOverride { get; }
    public IReadOnlyList<int> AmountDeltas { get; }
    public IReadOnlyList<CardKeyword> AddKeywords { get; }
    public string DescriptionOverride { get; }
    public string DescriptionZhOverride { get; }

    public CardUpgradeRecipe(
        int costDelta,
        int replayCountOverride,
        IReadOnlyList<int>? amountDeltas,
        IReadOnlyList<CardKeyword>? addKeywords,
        string? descriptionOverride,
        string? descriptionZhOverride)
    {
        CostDelta = costDelta;
        ReplayCountOverride = replayCountOverride < 0 ? 0 : replayCountOverride;
        AmountDeltas = amountDeltas ?? Array.Empty<int>();
        AddKeywords = addKeywords ?? Array.Empty<CardKeyword>();
        DescriptionOverride = descriptionOverride ?? string.Empty;
        DescriptionZhOverride = descriptionZhOverride ?? string.Empty;
    }
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
    private const string DefaultArtPath = "res://icon.svg";
    private const string DefaultArtFolder = "res://Assets/Cards";
    private static readonly CardCatalog Catalog = CardCatalog.Load();
    private readonly string _fallbackName;
    private readonly string _fallbackDescription;

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string DescriptionZh { get; }
    public string ArtPath { get; }
    public string NameKey { get; }
    public string DescriptionKey { get; }
    public CardKind Kind { get; }
    public int Cost { get; }
    public IReadOnlyList<CardEffectData> Effects { get; }
    public IReadOnlyList<CardKeyword> Keywords { get; }
    public int ReplayCount { get; }

    public CardUpgradeRecipe? Upgrade { get; }
    public bool IsUpgraded { get; }
    public string BaseId { get; }

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
        string? nameKey,
        string? descriptionKey,
        string artPath,
        CardKind kind,
        int cost,
        IReadOnlyList<CardEffectData> effects,
        IReadOnlyList<CardKeyword>? keywords = null,
        int replayCount = 1,
        CardUpgradeRecipe? upgrade = null,
        bool isUpgraded = false,
        string? baseId = null)
    {
        Id = id;
        _fallbackName = string.IsNullOrWhiteSpace(name) ? id : name;
        _fallbackDescription = string.IsNullOrWhiteSpace(description) ? string.Empty : description;
        NameKey = string.IsNullOrWhiteSpace(nameKey) ? $"card.{id}.name" : nameKey.Trim();
        DescriptionKey = string.IsNullOrWhiteSpace(descriptionKey) ? $"card.{id}.description" : descriptionKey.Trim();
        Name = _fallbackName;
        Description = _fallbackDescription;
        DescriptionZh = descriptionZh;
        ArtPath = ResolveArtPath(id, artPath);
        Kind = kind;
        Cost = cost;
        Effects = effects;
        Keywords = keywords ?? Array.Empty<CardKeyword>();
        ReplayCount = replayCount < 1 ? 1 : replayCount;
        Upgrade = upgrade;
        IsUpgraded = isUpgraded;
        BaseId = string.IsNullOrWhiteSpace(baseId) ? id : baseId;

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
        return $"{GetLocalizedName()}\n{LocalizationSettings.CostLabel()}: {Cost}\n{GetLocalizedDescription()}";
    }

    public string GetLocalizedDescription()
    {
        return ResolveLocalizedText(_fallbackDescription, DescriptionKey, DescriptionZh, _fallbackDescription);
    }

    public string GetLocalizedName()
    {
        var localizedBase = ResolveLocalizedText(_fallbackName, NameKey, _fallbackName, _fallbackName);
        return IsUpgraded ? localizedBase + "+" : localizedBase;
    }

    private static string ResolveLocalizedText(
        string fallback,
        string key,
        string localizedFallback,
        string englishFallback)
    {
        if (LocalizationSettings.CurrentLanguage == GameLanguage.En)
        {
            return fallback;
        }

        if (!string.IsNullOrWhiteSpace(key))
        {
            var localized = LocalizationService.Get(key, string.Empty);
            if (!string.IsNullOrWhiteSpace(localized))
            {
                return localized;
            }
        }

        return !string.IsNullOrWhiteSpace(localizedFallback) ? localizedFallback : englishFallback;
    }

    public bool HasEffect(CardEffectType type)
    {
        return Effects.Any(effect => effect.Type == type);
    }

    public static CardData CreateById(string id)
    {
        if (!string.IsNullOrEmpty(id) && id.EndsWith("+", StringComparison.Ordinal))
        {
            var baseId = id.Substring(0, id.Length - 1);
            if (Catalog.CardsById.TryGetValue(baseId, out var baseCard))
            {
                return BuildUpgradedClone(baseCard);
            }
        }

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

    private static CardData BuildUpgradedClone(CardData baseCard)
    {
        var recipe = baseCard.Upgrade;
        var effects = new List<CardEffectData>(baseCard.Effects.Count);
        for (var i = 0; i < baseCard.Effects.Count; i++)
        {
            var source = baseCard.Effects[i];
            var amount = source.Amount;
            if (recipe != null && i < recipe.AmountDeltas.Count)
            {
                amount = Math.Max(0, amount + recipe.AmountDeltas[i]);
            }

            effects.Add(new CardEffectData(
                source.Type,
                source.Target,
                amount,
                source.Repeat,
                source.UseAttackerStrength,
                source.UseTargetVulnerable,
                source.FlatBonus));
        }

        var costDelta = recipe?.CostDelta ?? 0;
        var newCost = Math.Max(0, baseCard.Cost + costDelta);

        var replayCount = baseCard.ReplayCount;
        if (recipe != null && recipe.ReplayCountOverride > 0)
        {
            replayCount = recipe.ReplayCountOverride;
        }

        var keywords = new List<CardKeyword>(baseCard.Keywords);
        if (recipe != null)
        {
            foreach (var keyword in recipe.AddKeywords)
            {
                if (!keywords.Contains(keyword))
                {
                    keywords.Add(keyword);
                }
            }
        }

        var description = !string.IsNullOrWhiteSpace(recipe?.DescriptionOverride)
            ? recipe!.DescriptionOverride
            : baseCard.Description;
        var descriptionZh = !string.IsNullOrWhiteSpace(recipe?.DescriptionZhOverride)
            ? recipe!.DescriptionZhOverride
            : baseCard.DescriptionZh;

        return new CardData(
            baseCard.Id + "+",
            baseCard.Name,
            description,
            descriptionZh,
            baseCard.NameKey,
            string.Empty,
            baseCard.ArtPath,
            baseCard.Kind,
            newCost,
            effects,
            keywords,
            replayCount,
            upgrade: null,
            isUpgraded: true,
            baseId: baseCard.Id);
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

        var keywords = source.Keywords.Count == 0
            ? (IReadOnlyList<CardKeyword>)Array.Empty<CardKeyword>()
            : new List<CardKeyword>(source.Keywords);

        return new CardData(
            source.Id,
            source.Name,
            source.Description,
            source.DescriptionZh,
            source.NameKey,
            source.DescriptionKey,
            source.ArtPath,
            source.Kind,
            source.Cost,
            effects,
            keywords,
            source.ReplayCount,
            source.Upgrade,
            source.IsUpgraded,
            source.BaseId);
    }

    private static string ResolveArtPath(string id, string? path)
    {
        var normalized = string.IsNullOrWhiteSpace(path) ? $"{DefaultArtFolder}/{id}.png" : path.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return DefaultArtPath;
        }

        if (!normalized.StartsWith("res://", StringComparison.OrdinalIgnoreCase)
            && !normalized.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = $"res://{normalized.TrimStart('/', '\\')}";
        }

        if (TryResourceExists(normalized))
        {
            return normalized;
        }

        var fallbackById = $"{DefaultArtFolder}/{id}.png";
        if (TryResourceExists(fallbackById))
        {
            return fallbackById;
        }

        var placeholder = "res://Assets/Cards/placeholder.svg";
        if (TryResourceExists(placeholder))
        {
            return placeholder;
        }

        return DefaultArtPath;
    }

    private static bool TryResourceExists(string path)
    {
        if (string.Equals(
            System.Environment.GetEnvironmentVariable("SLAY_HS_SKIP_GODOT_RESOURCE_CHECKS"),
            "1",
            StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            return ResourceLoader.Exists(path);
        }
        catch
        {
            // Console tests can construct CardData before Godot is initialized.
            return false;
        }
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

                var keywords = new List<CardKeyword>();
                foreach (var raw in cardDto.Keywords ?? new List<string>())
                {
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        continue;
                    }

                    keywords.Add(ParseEnum<CardKeyword>(raw, "card keyword"));
                }

                var replayCount = cardDto.ReplayCount <= 0 ? 1 : cardDto.ReplayCount;

                CardUpgradeRecipe? upgrade = null;
                if (cardDto.Upgrade != null)
                {
                    var addKeywords = new List<CardKeyword>();
                    foreach (var raw in cardDto.Upgrade.AddKeywords ?? new List<string>())
                    {
                        if (string.IsNullOrWhiteSpace(raw))
                        {
                            continue;
                        }

                        addKeywords.Add(ParseEnum<CardKeyword>(raw, "upgrade keyword"));
                    }

                    upgrade = new CardUpgradeRecipe(
                        cardDto.Upgrade.CostDelta,
                        cardDto.Upgrade.ReplayCount,
                        cardDto.Upgrade.AmountDeltas,
                        addKeywords,
                        cardDto.Upgrade.Description,
                        cardDto.Upgrade.DescriptionZh);
                }

                var card = new CardData(
                    cardDto.Id,
                    cardDto.Name ?? cardDto.Id,
                    cardDto.Description ?? string.Empty,
                    cardDto.DescriptionZh ?? cardDto.Description ?? string.Empty,
                    cardDto.NameKey,
                    cardDto.DescriptionKey,
                    cardDto.ArtPath,
                    ParseEnum<CardKind>(cardDto.Kind, "card kind"),
                    cardDto.Cost,
                    effects,
                    keywords,
                    replayCount,
                    upgrade);

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
        return LocalizationService.Get("ui.cost", "Cost");
    }

    public static string LanguageButtonText()
    {
        return CurrentLanguage == GameLanguage.ZhHans
            ? LocalizationService.Get("ui.language_button", "\u8bed\u8a00: \u4e2d\u6587")
            : LocalizationService.Get("ui.language_button", "Language: English");
    }

    public static string HighlightCardDescription(string text)
    {
        if (CurrentLanguage == GameLanguage.ZhHans)
        {
            return text
                .Replace("\u53d1\u52a8", "[color=#fca5a5]\u53d1\u52a8[/color]")
                .Replace("\u683c\u6321", "[color=#93c5fd]\u683c\u6321[/color]")
                .Replace("\u4fbf\u5bb9", "[color=#e9d5ff]\u4fbf\u5bb9[/color]")
                .Replace("\u7075\u9b42", "[color=#a5f3fc]\u7075\u9b42[/color]")
                .Replace("\u4fee\u590d", "[color=#86efac]\u4fee\u590d[/color]");
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
