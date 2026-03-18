using Godot;
using System;
using System.Linq;

public partial class RelicCompendiumScene : Control
{
    private readonly string[] _rarities =
    {
        "Starter",
        "Common",
        "Uncommon",
        "Rare",
        "Boss"
    };

    private Button _backButton = null!;
    private Label _titleLabel = null!;
    private VBoxContainer _content = null!;

    public override void _Ready()
    {
        _backButton = GetNode<Button>("%BackButton");
        _titleLabel = GetNode<Label>("Margin/Root/Title");
        _content = GetNode<VBoxContainer>("%RelicContent");

        _backButton.Pressed += () => GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
        LocalizationSettings.LanguageChanged += OnLanguageChanged;

        RefreshUiText();
        BuildCompendium();
    }

    public override void _ExitTree()
    {
        LocalizationSettings.LanguageChanged -= OnLanguageChanged;
    }

    private void RefreshUiText()
    {
        _backButton.Text = LocalizationService.Get("ui.relic_compendium.back", "Back");
        _titleLabel.Text = LocalizationService.Get("ui.relic_compendium.title", "Relic Compendium");
    }

    private void BuildCompendium()
    {
        foreach (Node child in _content.GetChildren())
        {
            child.QueueFree();
        }

        var grouped = RelicData.GroupByRarity();

        foreach (var rarity in _rarities)
        {
            if (!grouped.TryGetValue(rarity, out var relics) || relics.Count == 0)
            {
                continue;
            }

            AddSectionTitle(rarity, relics.Count);
            foreach (var relic in relics
                         .OrderBy(r => r.LocalizedArchetype, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(r => r.LocalizedName, StringComparer.OrdinalIgnoreCase))
            {
                AddRelicRow(relic);
            }
        }
    }

    private void AddSectionTitle(string rarity, int count)
    {
        var key = $"ui.relic_compendium.rarity.{rarity.ToLowerInvariant()}";
        var title = new Label
        {
            Text = LocalizationService.Format(
                "ui.relic_compendium.section_format",
                "[{0}] {1}",
                LocalizationService.Get(key, rarity),
                count)
        };
        title.AddThemeFontSizeOverride("font_size", 30);
        _content.AddChild(title);
        _content.AddChild(new HSeparator());
    }

    private void AddRelicRow(RelicData relic)
    {
        var row = new PanelContainer { CustomMinimumSize = new Vector2(0, 88) };
        var line = new HBoxContainer();
        line.AddThemeConstantOverride("separation", 12);

        var icon = new TextureRect
        {
            CustomMinimumSize = new Vector2(52, 52),
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Texture = GD.Load<Texture2D>(CombatVisualCatalog.GetRelicIconPath(relic.Id))
        };

        var textBox = new VBoxContainer();
        textBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var name = new Label
        {
            Text = LocalizationService.Format(
                "ui.relic_compendium.relic_title",
                "{0} [{1}]",
                relic.LocalizedName,
                relic.LocalizedArchetype)
        };
        name.AddThemeFontSizeOverride("font_size", 22);
        textBox.AddChild(name);

        textBox.AddChild(new Label
        {
            Text = relic.LocalizedDescription,
            AutowrapMode = TextServer.AutowrapMode.Word,
            Modulate = new Color(1f, 1f, 1f, 0.86f)
        });

        line.AddChild(icon);
        line.AddChild(textBox);
        row.AddChild(line);
        _content.AddChild(row);
    }

    private void OnLanguageChanged()
    {
        RefreshUiText();
        BuildCompendium();
    }
}
