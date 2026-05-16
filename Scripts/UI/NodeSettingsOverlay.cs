using Godot;

// Small floating "Settings" gear that lives on top of any node scene
// (Shop / Event / Rest / Intro / etc). Opens a modal with a single feature
// for now: Re-enter current node, which restores the snapshot the scene
// saved at _Ready and reloads the scene from disk. Because GameState's RNG
// state is part of the snapshot, every random roll inside the node is
// reproduced identically.
public partial class NodeSettingsOverlay : CanvasLayer
{
    private Button _gearButton = null!;
    private Control _modal = null!;
    private Label _titleLabel = null!;
    private Label _hintLabel = null!;
    private Button _reenterButton = null!;
    private Button _closeButton = null!;

    public override void _Ready()
    {
        _gearButton = GetNode<Button>("%GearButton");
        _modal = GetNode<Control>("%Modal");
        _titleLabel = GetNode<Label>("%TitleLabel");
        _hintLabel = GetNode<Label>("%HintLabel");
        _reenterButton = GetNode<Button>("%ReenterButton");
        _closeButton = GetNode<Button>("%CloseButton");

        _modal.Visible = false;

        _gearButton.Pressed += OnGearPressed;
        _reenterButton.Pressed += OnReenterPressed;
        _closeButton.Pressed += OnClosePressed;

        LocalizationSettings.LanguageChanged += RefreshText;
        RefreshText();
    }

    public override void _ExitTree()
    {
        LocalizationSettings.LanguageChanged -= RefreshText;
    }

    private void RefreshText()
    {
        _gearButton.Text = LocalizationService.Get("ui.node_settings.gear", "Settings");
        _titleLabel.Text = LocalizationService.Get("ui.node_settings.title", "Settings");
        _hintLabel.Text = LocalizationService.Get(
            "ui.node_settings.hint",
            "Rewind this node back to the state it was in when you stepped onto it.");
        _reenterButton.Text = LocalizationService.Get(
            "ui.node_settings.reenter",
            "Re-enter current node");
        _closeButton.Text = LocalizationService.Get("ui.node_settings.close", "Close");
    }

    private void OnGearPressed()
    {
        _modal.Visible = true;
        var state = GetNode<GameState>("/root/GameState");
        _reenterButton.Disabled = !state.HasNodeEntrySnapshot;
    }

    private void OnClosePressed()
    {
        _modal.Visible = false;
    }

    private void OnReenterPressed()
    {
        var state = GetNode<GameState>("/root/GameState");
        if (!state.HasNodeEntrySnapshot)
        {
            _modal.Visible = false;
            return;
        }

        var scenePath = state.GetNodeEntrySceneFilePath();
        state.RestoreNodeEntrySnapshot();
        _modal.Visible = false;

        if (string.IsNullOrWhiteSpace(scenePath))
        {
            GetTree().ReloadCurrentScene();
        }
        else
        {
            GetTree().ChangeSceneToFile(scenePath);
        }
    }
}
