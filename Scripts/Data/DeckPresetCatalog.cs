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
    // Character-select visuals — large emoji portrait + accent color used for
    // the avatar tile border and the portrait backdrop.
    public string Glyph { get; }
    public Color Accent { get; }

    public DeckPresetData(
        string id,
        string name,
        string description,
        string nameKey,
        string descriptionKey,
        IReadOnlyList<string> cardIds,
        string glyph,
        Color accent)
    {
        Id = id;
        Name = name;
        Description = description;
        NameKey = nameKey;
        DescriptionKey = descriptionKey;
        CardIds = cardIds;
        Glyph = glyph;
        Accent = accent;
    }

    public string LocalizedName => LocalizationService.Get(NameKey, Name);

    public string LocalizedDescription => LocalizationService.Get(DescriptionKey, Description);
}

public static class DeckPresetCatalog
{
    private static readonly List<DeckPresetData> Presets = new()
    {
        new DeckPresetData(
            id: "starter",
            name: "Balanced Starter",
            description: "Stable attack + block with simple scaling.",
            nameKey: "deck_preset.starter.name",
            descriptionKey: "deck_preset.starter.description",
            cardIds: CardData.StarterDeckIds(),
            glyph: "🧙",
            accent: new Color(0.54f, 0.80f, 1f)),

        new DeckPresetData(
            id: "infinite_cycle",
            name: "Infinite Cycle",
            description: "Zero-cost draw/energy loop with explosive turn chains.",
            nameKey: "deck_preset.infinite_cycle.name",
            descriptionKey: "deck_preset.infinite_cycle.description",
            cardIds: new List<string>
            {
                "spark_loop", "spark_loop", "spark_loop",
                "arcane_recycle", "arcane_recycle",
                "hand_overflow", "hand_overflow",
                "mana_turbine", "overclock",
                "arcane_barrage", "quick_slash"
            },
            glyph: "♾",
            accent: new Color(0.72f, 0.63f, 0.95f)),

        new DeckPresetData(
            id: "infinite_fireball",
            name: "Infinite Fireball",
            description: "Multi-hit spell core, stacking energy then burst to finish.",
            nameKey: "deck_preset.infinite_fireball.name",
            descriptionKey: "deck_preset.infinite_fireball.description",
            cardIds: new List<string>
            {
                "infinite_fireball", "infinite_fireball",
                "ember_wheel", "ember_wheel",
                "arcane_barrage", "arcane_barrage",
                "spark_loop", "arcane_recycle",
                "mana_turbine", "battle_focus", "war_cry"
            },
            glyph: "🔥",
            accent: new Color(1f, 0.55f, 0.30f)),

        new DeckPresetData(
            id: "death_legion",
            name: "Death Legion",
            description: "Vulnerable spreading + sustain, overwhelms over long fights.",
            nameKey: "deck_preset.death_legion.name",
            descriptionKey: "deck_preset.death_legion.description",
            cardIds: new List<string>
            {
                "grave_whisper", "grave_whisper",
                "bone_shrapnel", "bone_shrapnel",
                "death_chorus", "soul_siphon",
                "rending_wave", "meteor_shower",
                "phoenix_cycle", "fortify", "reaper_touch"
            },
            glyph: "💀",
            accent: new Color(0.78f, 0.55f, 0.85f)),

        new DeckPresetData(
            id: "berserker_slam",
            name: "Berserker Slam",
            description: "Strength stacking and heavy blows to end fights in a few turns.",
            nameKey: "deck_preset.berserker_slam.name",
            descriptionKey: "deck_preset.berserker_slam.description",
            cardIds: new List<string>
            {
                "war_cry", "war_cry",
                "berserker_form", "berserker_form",
                "adrenaline_rush", "adrenaline_rush",
                "heavy_slash", "crushing_blow",
                "twin_strike", "bash", "reaper_touch"
            },
            glyph: "⚔",
            accent: new Color(1f, 0.40f, 0.40f)),

        new DeckPresetData(
            id: "fortress_control",
            name: "Fortress Control",
            description: "High defense core with block conversion and delayed finishing damage.",
            nameKey: "deck_preset.fortress_control.name",
            descriptionKey: "deck_preset.fortress_control.description",
            cardIds: new List<string>
            {
                "fortress_stance", "fortress_stance",
                "iron_wall", "iron_wall",
                "fortify", "fortify",
                "shield_bash", "shield_bash",
                "second_wind", "shrug", "meteor_shower"
            },
            glyph: "🛡",
            accent: new Color(0.55f, 0.78f, 0.90f)),

        new DeckPresetData(
            id: "storm_engine",
            name: "Storm Engine",
            description: "Draw + energy machine that loops Chain Lightning and Arcane Barrage.",
            nameKey: "deck_preset.storm_engine.name",
            descriptionKey: "deck_preset.storm_engine.description",
            cardIds: new List<string>
            {
                "chain_lightning", "chain_lightning",
                "arcane_barrage", "arcane_barrage",
                "spark_loop", "spark_loop",
                "arcane_recycle", "hand_overflow",
                "mana_turbine", "meditate", "overclock"
            },
            glyph: "⚡",
            accent: new Color(0.95f, 0.95f, 0.45f))
    };

    public static IReadOnlyList<DeckPresetData> All()
    {
        return Presets;
    }

    public static DeckPresetData Resolve(string? id)
    {
        var found = Presets.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
        return found ?? Presets[0];
    }
}
