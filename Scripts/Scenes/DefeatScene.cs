using Godot;

public partial class DefeatScene : Control
{
    private Label _titleLabel = null!;
    private Label _subtitleLabel = null!;
    private Label _summaryLabel = null!;
    private Button _menuButton = null!;
    private Button _retryButton = null!;

    // Snapshot the run summary BEFORE we wipe the save so the player can see
    // what they accomplished. GameState is autoload — its fields survive the
    // scene change from BattleScene, but we delete the save file on entry
    // to ensure Try Again starts a fresh run.
    public override void _Ready()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.SetUiPhase("defeat");
        SaveSystem.Delete();

        _titleLabel = GetNode<Label>("%TitleLabel");
        _subtitleLabel = GetNode<Label>("%SubtitleLabel");
        _summaryLabel = GetNode<Label>("%SummaryLabel");
        _menuButton = GetNode<Button>("%MenuButton");
        _retryButton = GetNode<Button>("%RetryButton");

        _menuButton.Pressed += OnMenuPressed;
        _retryButton.Pressed += OnRetryPressed;

        LocalizationSettings.LanguageChanged += RefreshText;
        RefreshText();
    }

    public override void _ExitTree()
    {
        LocalizationSettings.LanguageChanged -= RefreshText;
    }

    private void RefreshText()
    {
        var state = GetNode<GameState>("/root/GameState");
        _titleLabel.Text = "💀 " + LocalizationService.Get("ui.defeat.title", "Defeat");
        _subtitleLabel.Text = LocalizationService.Get(
            "ui.defeat.subtitle",
            "The dungeon has claimed another adventurer.");
        _summaryLabel.Text = LocalizationService.Format(
            "ui.defeat.summary",
            "Floors climbed: {0}    Battles won: {1}    Gold: {2}",
            state.Floor,
            state.BattlesWon,
            state.Gold);
        _menuButton.Text = "← " + LocalizationService.Get("ui.defeat.menu_button", "Back to Main Menu");
        _retryButton.Text = "↻ " + LocalizationService.Get("ui.defeat.retry_button", "Try Again");
    }

    private void OnMenuPressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.SetUiPhase("main_menu");
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }

    private void OnRetryPressed()
    {
        // Start a brand-new run from the character-select screen so the player
        // can pick a fresh class. If the project doesn't have one set up yet,
        // the scene will fall back to MapScene the normal way.
        var state = GetNode<GameState>("/root/GameState");
        state.StartNewRun();
        state.SetUiPhase("character_select");
        GetTree().ChangeSceneToFile("res://Scenes/CharacterSelectScene.tscn");
    }
}
