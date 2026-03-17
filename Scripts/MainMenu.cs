using Godot;

public partial class MainMenu : Control
{
    private Button _startButton = null!;
    private Button _quitButton = null!;
    private Button _languageButton = null!;
    private Button _cardBrowserButton = null!;

    public override void _Ready()
    {
        _startButton = GetNode<Button>("%StartButton");
        _quitButton = GetNode<Button>("%QuitButton");
        _languageButton = GetNode<Button>("%LanguageButton");
        _cardBrowserButton = GetNode<Button>("%CardBrowserButton");

        _startButton.Pressed += OnStartPressed;
        _quitButton.Pressed += OnQuitPressed;
        _languageButton.Pressed += OnLanguagePressed;
        _cardBrowserButton.Pressed += OnCardBrowserPressed;

        RefreshLanguageButtonText();
    }

    private void OnStartPressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.StartNewRun();
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }

    private void OnCardBrowserPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/CardBrowserScene.tscn");
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }

    private void OnLanguagePressed()
    {
        LocalizationSettings.ToggleLanguage();
        RefreshLanguageButtonText();
    }

    private void RefreshLanguageButtonText()
    {
        _languageButton.Text = LocalizationSettings.LanguageButtonText();
    }
}
