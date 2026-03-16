using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

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
            var path = ResolveCardsJsonPath();
            var json = File.ReadAllText(path);

            var dto = JsonSerializer.Deserialize<CardCatalogDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (dto == null || dto.Cards == null || dto.Cards.Count == 0)
            {
                throw new InvalidOperationException($"Invalid card catalog JSON: {path}");
            }

            var cardsById = new Dictionary<string, CardData>();
            foreach (var cardDto in dto.Cards)
            {
                if (string.IsNullOrWhiteSpace(cardDto.Id))
                {
                    throw new InvalidOperationException("Card id is required in cards.json.");
                }

                var effects = new List<CardEffectData>();
                foreach (var effectDto in cardDto.Effects ?? new List<CardEffectDto>())
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
                    ParseEnum<CardKind>(cardDto.Kind, "card kind"),
                    cardDto.Cost,
                    effects);

                cardsById[card.Id] = card;
            }

            var starterDeck = dto.StarterDeck ?? new List<string>();
            var rewardPool = dto.RewardPool ?? new List<string>();
            return new CardCatalog(cardsById, starterDeck, rewardPool);
        }

        private static string ResolveCardsJsonPath()
        {
            var candidates = new List<string>();

            var envPath = Environment.GetEnvironmentVariable("SLAY_THE_HS_CARDS_JSON");
            if (!string.IsNullOrWhiteSpace(envPath))
            {
                candidates.Add(envPath);
            }

            candidates.AddRange(EnumerateCardsJsonCandidates(AppContext.BaseDirectory));
            candidates.AddRange(EnumerateCardsJsonCandidates(Directory.GetCurrentDirectory()));

            foreach (var path in candidates.Distinct())
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            throw new FileNotFoundException("Cannot locate Data/cards.json for card catalog loading.");
        }

        private static IEnumerable<string> EnumerateCardsJsonCandidates(string startDir)
        {
            var current = new DirectoryInfo(startDir);
            for (var i = 0; i < 8 && current != null; i++)
            {
                yield return Path.Combine(current.FullName, "Data", "cards.json");
                current = current.Parent;
            }
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

    private sealed class CardCatalogDto
    {
        public List<CardDto>? Cards { get; set; }
        public List<string>? StarterDeck { get; set; }
        public List<string>? RewardPool { get; set; }
    }

    private sealed class CardDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public int Cost { get; set; }
        public List<CardEffectDto>? Effects { get; set; }
    }

    private sealed class CardEffectDto
    {
        public string Type { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public int Amount { get; set; }
        public int Repeat { get; set; } = 1;
        public bool UseAttackerStrength { get; set; } = true;
        public bool UseTargetVulnerable { get; set; } = true;
        public int FlatBonus { get; set; }
    }
}
