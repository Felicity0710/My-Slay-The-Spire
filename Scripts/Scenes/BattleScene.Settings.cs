using Godot;

public partial class BattleScene
{
    // Settings live entirely in NodeSettingsOverlay now (resolution / fps / vsync /
    // fps counter / master volume / music volume). These battle-scene shims used
    // to wire them up locally — they remain as no-ops so existing call sites
    // continue to compile while the in-scene SettingsModal is deprecated.

    private void SetupSettingsUi() { }

    private void RefreshBattleStaticText()
    {
        _endTurnButton.Text = LocalizationService.Get("ui.battle.end_turn", "End Turn");
        _backButton.Text = LocalizationService.Get("ui.battle.back_to_map", "Back To Map");
        _testVictoryButton.Text = LocalizationService.Get("ui.battle.test_victory_button", "Test Victory");
        _turnBannerLabel.Text = LocalizationService.Get("ui.battle.turn_player", "Player Turn");
    }

    private void OnLanguageChanged()
    {
        RefreshBattleStaticText();
        RefreshUi();
    }
}
