using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public sealed class DeckPresetData
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string NameKey { get; }
    public string DescriptionKey { get; }
    public IReadOnlyList<string> CardIds { get; }
    public string Glyph { get; }
    public Color Accent { get; }
    public int MaxHp { get; }
    public string StarterRelicId { get; }

    public DeckPresetData(
        string id,
        string name,
        string description,
        string nameKey,
        string descriptionKey,
        IReadOnlyList<string> cardIds,
        string glyph,
        Color accent,
        int maxHp = 80,
        string starterRelicId = "lantern")
    {
        Id = id;
        Name = name;
        Description = description;
        NameKey = nameKey;
        DescriptionKey = descriptionKey;
        CardIds = cardIds;
        Glyph = glyph;
        Accent = accent;
        MaxHp = maxHp;
        StarterRelicId = starterRelicId;
    }

    public string LocalizedName => LocalizationService.Get(NameKey, Name);
    public string LocalizedDescription => LocalizationService.Get(DescriptionKey, Description);
}

public static class DeckPresetCatalog
{
    private static readonly List<DeckPresetData> Presets = new()
    {
        new DeckPresetData(
            id: "iron_vanguard",
            name: "Iron Vanguard",
            description: "Strength-based warrior. Stack power and crush foes with heavy blows. Wields self-sacrifice for immense strength.",
            nameKey: "deck_preset.iron_vanguard.name",
            descriptionKey: "deck_preset.iron_vanguard.description",
            cardIds: new List<string>
            {
                "strike", "strike", "strike", "strike", "strike",
                "defend", "defend", "defend", "defend",
                "bash", "blood_pact", "heavy_slash"
            },
            glyph: "⚔️",
            accent: new Color(0.80f, 0.20f, 0.20f),
            maxHp: 80,
            starterRelicId: "battle_standard"),

        new DeckPresetData(
            id: "phantom_dancer",
            name: "Phantom Dancer",
            description: "Agile assassin. Multi-hit combos, Vulnerable stacking, and draw/discard cycling. Strikes from the shadows.",
            nameKey: "deck_preset.phantom_dancer.name",
            descriptionKey: "deck_preset.phantom_dancer.description",
            cardIds: new List<string>
            {
                "strike", "strike", "strike", "strike",
                "defend", "defend", "defend", "defend",
                "quick_slash", "tactical_step", "triage", "rend_armor"
            },
            glyph: "🌪️",
            accent: new Color(0.53f, 0.33f, 0.80f),
            maxHp: 70,
            starterRelicId: "shadow_band"),

        new DeckPresetData(
            id: "storm_mage",
            name: "Storm Mage",
            description: "Arcane spellcaster. Manipulates energy, casts devastating multi-hit spells, and chains infinite combos.",
            nameKey: "deck_preset.storm_mage.name",
            descriptionKey: "deck_preset.storm_mage.description",
            cardIds: new List<string>
            {
                "strike", "strike", "strike",
                "defend", "defend", "defend", "defend", "defend",
                "spark_loop", "arcane_recycle", "meditate", "chain_lightning"
            },
            glyph: "⚡",
            accent: new Color(0.27f, 0.53f, 0.87f),
            maxHp: 65,
            starterRelicId: "ember_ring")
    };

    public static IReadOnlyList<DeckPresetData> All() => Presets;

    public static DeckPresetData Resolve(string? id)
    {
        var found = Presets.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
        return found ?? Presets[0];
    }
}
