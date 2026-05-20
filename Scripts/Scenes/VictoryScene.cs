using Godot;

public partial class VictoryScene : Control
{
    private Label _titleLabel = null!;
    private Label _subtitleLabel = null!;
    private Label _summaryLabel = null!;
    private Button _menuButton = null!;
    private Button _newRunButton = null!;

    public override void _Ready()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.SetUiPhase("victory");
        SaveSystem.Delete();

        _titleLabel = GetNode<Label>("%TitleLabel");
        _subtitleLabel = GetNode<Label>("%SubtitleLabel");
        _summaryLabel = GetNode<Label>("%SummaryLabel");
        _menuButton = GetNode<Button>("%MenuButton");
        _newRunButton = GetNode<Button>("%NewRunButton");

        _menuButton.Pressed += OnMenuPressed;
        _newRunButton.Pressed += OnNewRunPressed;
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
        _titleLabel.Text = "👑 " + LocalizationService.Get("ui.victory.title", "Victory");
        _subtitleLabel.Text = LocalizationService.Format(
            "ui.victory.subtitle",
            "You have cleared all {0} acts.",
            MapProgressionRules.MaxActs);
        _summaryLabel.Text = LocalizationService.Format(
            "ui.victory.summary",
            "Floors climbed: {0}    Battles won: {1}    Final HP: {2}/{3}    Gold: {4}",
            state.Floor,
            state.BattlesWon,
            state.PlayerHp,
            state.MaxHp,
            state.Gold);
        _menuButton.Text = "← " + LocalizationService.Get("ui.victory.menu_button", "Back to Main Menu");
        _newRunButton.Text = "↻ " + LocalizationService.Get("ui.victory.new_run_button", "New Run");
    }

    private void OnMenuPressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.SetUiPhase("main_menu");
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }

    private void OnNewRunPressed()
    {
        // Same flow as DefeatScene's Try Again: start a fresh run from
        // character select so the player can pick a different archetype.
        var state = GetNode<GameState>("/root/GameState");
        state.StartNewRun();
        state.SetUiPhase("character_select");
        GetTree().ChangeSceneToFile("res://Scenes/CharacterSelectScene.tscn");
    }
}
