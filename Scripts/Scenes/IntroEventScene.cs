using Godot;

public partial class IntroEventScene : Control
{
    private Label _titleLabel = null!;
    private Label _hpLabel = null!;
    private Label _bodyLabel = null!;
    private Button _continueButton = null!;

    public override void _Ready()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.SetUiPhase("intro");

        // Heal-to-full again here as a safety net in case the intro was entered
        // through a non-standard path. Idempotent — Min keeps HP within MaxHp.
        state.PlayerHp = state.MaxHp;

        _titleLabel = GetNode<Label>("%TitleLabel");
        _hpLabel = GetNode<Label>("%HpLabel");
        _bodyLabel = GetNode<Label>("%BodyLabel");
        _continueButton = GetNode<Button>("%ContinueButton");

        _continueButton.Pressed += OnContinuePressed;
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
        _titleLabel.Text = LocalizationService.Format(
            "ui.intro.title",
            "Act {0} Intro",
            state.Act);
        _hpLabel.Text = LocalizationService.Format(
            "ui.intro.hp",
            "HP {0}/{1}",
            state.PlayerHp,
            state.MaxHp);
        _bodyLabel.Text = LocalizationService.Get(
            "ui.intro.body",
            "A wandering soul finds you on the road and tends to your wounds. "
            + "You feel ready to face whatever this act has in store.");
        _continueButton.Text = LocalizationService.Get("ui.intro.continue", "Continue");
    }

    private void OnContinuePressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        // Intro counts as the act's first resolved node — advance the floor so the
        // regular map shows up next.
        state.ResolveEventFinished();
        state.SetUiPhase("map");
        GetTree().ChangeSceneToFile("res://Scenes/MapScene.tscn");
    }
}
