using Godot;

public partial class MainMenu : Control
{
    private Button _startButton = null!;
    private Button _battleTestButton = null!;
    private Button _quitButton = null!;
    private Button _languageButton = null!;
    private Button _cardEditorButton = null!;
    private OptionButton _deckPresetOption = null!;
    private Label _deckPresetLabel = null!;

    public override void _Ready()
    {
        _startButton = GetNode<Button>("%StartButton");
        _battleTestButton = GetNode<Button>("%BattleTestButton");
        _quitButton = GetNode<Button>("%QuitButton");
        _languageButton = GetNode<Button>("%LanguageButton");
        _cardEditorButton = GetNode<Button>("%CardEditorButton");
        _deckPresetOption = GetNode<OptionButton>("%DeckPresetOption");
        _deckPresetLabel = GetNode<Label>("%DeckPresetLabel");

        _startButton.Pressed += OnStartPressed;
        _battleTestButton.Pressed += OnBattleTestPressed;
        _quitButton.Pressed += OnQuitPressed;
        _languageButton.Pressed += OnLanguagePressed;
        _cardEditorButton.Pressed += OnCardEditorPressed;
        _deckPresetOption.ItemSelected += OnDeckPresetSelected;

        RefreshUiText();
        PopulateDeckPresets();
    }

    private void OnStartPressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.StartNewRun();
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }

    private void OnBattleTestPressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.StartBattleTestRun();
        GetTree().ChangeSceneToFile("res://Scenes/BattleScene.tscn");
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }

    private void OnCardEditorPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/CardEditorScene.tscn");
    }

    private void OnLanguagePressed()
    {
        LocalizationSettings.ToggleLanguage();
        RefreshUiText();
        PopulateDeckPresets();
    }

    private void OnDeckPresetSelected(long index)
    {
        var state = GetNode<GameState>("/root/GameState");
        var presets = state.DeckPresets();
        if (index < 0 || index >= presets.Count)
        {
            return;
        }

        state.SetDeckPreset(presets[(int)index].Id);
    }

    private void RefreshUiText()
    {
        _languageButton.Text = LocalizationSettings.LanguageButtonText();
        _deckPresetLabel.Text = LocalizationSettings.CurrentLanguage == GameLanguage.ZhHans
            ? "测试卡组预设"
            : "Deck Preset";
        _startButton.Text = LocalizationSettings.CurrentLanguage == GameLanguage.ZhHans
            ? "开始爬塔（地图）"
            : "Start Run (Map)";
        _battleTestButton.Text = LocalizationSettings.CurrentLanguage == GameLanguage.ZhHans
            ? "直接战斗测试"
            : "Battle Test";
    }

    private void PopulateDeckPresets()
    {
        var state = GetNode<GameState>("/root/GameState");
        var presets = state.DeckPresets();
        _deckPresetOption.Clear();

        var selectedIndex = 0;
        for (var i = 0; i < presets.Count; i++)
        {
            var preset = presets[i];
            _deckPresetOption.AddItem($"{preset.LocalizedName} · {preset.LocalizedDescription}");
            if (preset.Id == state.SelectedDeckPresetId)
            {
                selectedIndex = i;
            }
        }

        _deckPresetOption.Select(selectedIndex);
    }
}
