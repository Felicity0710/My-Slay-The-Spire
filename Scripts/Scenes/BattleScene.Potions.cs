using Godot;
using System;

public partial class BattleScene
{
    private Label _potionBarLabel = null!;
    private Button _potionButton1 = null!;
    private Button _potionButton2 = null!;
    private Button _potionButton3 = null!;

    private void SetupPotionUi()
    {
        _potionBarLabel = GetNode<Label>("%PotionBarLabel");
        _potionButton1 = GetNode<Button>("%PotionButton1");
        _potionButton2 = GetNode<Button>("%PotionButton2");
        _potionButton3 = GetNode<Button>("%PotionButton3");

        _potionButton1.Pressed += OnPotionButton1Pressed;
        _potionButton2.Pressed += OnPotionButton2Pressed;
        _potionButton3.Pressed += OnPotionButton3Pressed;

        RefreshPotionUi();
    }

    private void TearDownPotionUi()
    {
        if (IsInstanceValid(_potionButton1))
        {
            _potionButton1.Pressed -= OnPotionButton1Pressed;
        }

        if (IsInstanceValid(_potionButton2))
        {
            _potionButton2.Pressed -= OnPotionButton2Pressed;
        }

        if (IsInstanceValid(_potionButton3))
        {
            _potionButton3.Pressed -= OnPotionButton3Pressed;
        }
    }

    private void RefreshPotionUi()
    {
        if (!IsInstanceValid(_potionBarLabel) || _state == null)
        {
            return;
        }

        _potionBarLabel.Text = LocalizationService.Get("ui.battle.potions_label", "Potions");
        RefreshPotionButton(_potionButton1, 0);
        RefreshPotionButton(_potionButton2, 1);
        RefreshPotionButton(_potionButton3, 2);
    }

    private void RefreshPotionButton(Button button, int slotIndex)
    {
        if (!IsInstanceValid(button))
        {
            return;
        }

        var hasPotion = slotIndex >= 0 && slotIndex < _state.PotionIds.Count;
        if (!hasPotion)
        {
            button.Text = LocalizationService.Format("ui.battle.potion_slot_empty", "{0}. Empty", slotIndex + 1);
            button.TooltipText = LocalizationService.Get("ui.battle.potion_slot_empty_tooltip", "No potion in this slot.");
            button.Disabled = true;
            return;
        }

        var potion = PotionData.CreateById(_state.PotionIds[slotIndex]);
        button.Text = LocalizationService.Format("ui.battle.potion_slot_item", "{0}. {1}", slotIndex + 1, potion.DisplayName);
        button.TooltipText = $"{potion.DisplayName}\n{potion.DisplayDescription}";
        button.Disabled = _battleEnded || IsInputLocked();
    }

    private void OnPotionButton1Pressed()
    {
        OnPotionButtonPressed(0);
    }

    private void OnPotionButton2Pressed()
    {
        OnPotionButtonPressed(1);
    }

    private void OnPotionButton3Pressed()
    {
        OnPotionButtonPressed(2);
    }

    private void OnPotionButtonPressed(int slotIndex)
    {
        if (_battleEnded || IsInputLocked())
        {
            EmitUiSfx("error");
            return;
        }

        if (!_state.TryConsumePotionAt(slotIndex, out var potion))
        {
            EmitUiSfx("error");
            RefreshPotionUi();
            return;
        }

        ApplyPotionEffect(potion);
        EmitUiSfx("card_play");
        RefreshUi();
    }

    // Fired by RunStatusOverlay AFTER the potion has already been removed
    // from inventory — we apply the effect here and refresh battle UI. The
    // overlay itself has handled its own visual refresh.
    private void OnOverlayPotionConsumed(int slotIndex, string potionId)
    {
        var potion = PotionData.CreateById(potionId);
        ApplyPotionEffect(potion);
        EmitUiSfx("card_play");
        RefreshUi();
    }

    private void ApplyPotionEffect(PotionData potion)
    {
        var playerTarget = _playerCardView.EffectTarget();

        // Apply numeric effects from PotionData
        if (potion.HealAmount > 0)
        {
            var heal = Math.Min(potion.HealAmount, Math.Max(_playerMaxHp - _playerHp, 0));
            _playerHp += heal;
            SpawnFloatingText(playerTarget, $"+{heal} HP", new Color("86efac"));
            SpawnRuneEffect(playerTarget, new Color("86efac"));
            Log(LocalizationService.Format("log.battle.potion_healing", "Used {0}: heal {1} HP", potion.DisplayName, heal), "#86efac");
        }

        if (potion.StrengthAmount > 0)
        {
            _playerStrength += potion.StrengthAmount;
            SpawnFloatingText(playerTarget, $"+{potion.StrengthAmount} STR", new Color("d8b4fe"));
            SpawnRuneEffect(playerTarget, new Color("d8b4fe"));
            Log(LocalizationService.Format("log.battle.potion_strength", "Used {0}: gain {1} Strength", potion.DisplayName, potion.StrengthAmount), "#d8b4fe");
        }

        if (potion.EnergyAmount > 0)
        {
            _energy += potion.EnergyAmount;
            SpawnFloatingText(playerTarget, $"+{potion.EnergyAmount} EN", new Color("fde68a"));
            SpawnRuneEffect(playerTarget, new Color("fde68a"));
            Log(LocalizationService.Format("log.battle.potion_swift", "Used {0}: gain {1} Energy", potion.DisplayName, potion.EnergyAmount), "#fde68a");
        }

        if (potion.BlockAmount > 0)
        {
            _playerBlock += potion.BlockAmount;
            SpawnFloatingText(playerTarget, $"+{potion.BlockAmount} Block", new Color("93c5fd"));
            SpawnShieldEffect(playerTarget, new Color("93c5fd"));
            Log(LocalizationService.Format("log.battle.potion_guard", "Used {0}: gain {1} Block", potion.DisplayName, potion.BlockAmount), "#93c5fd");
        }

        if (potion.MaxHpAmount > 0)
        {
            _playerMaxHp += potion.MaxHpAmount;
            SpawnFloatingText(playerTarget, $"+{potion.MaxHpAmount} Max HP", new Color("fde68a"));
            SpawnRuneEffect(playerTarget, new Color("fde68a"));
            Log(LocalizationService.Format("log.battle.potion_maxhp", "Used {0}: +{1} Max HP", potion.DisplayName, potion.MaxHpAmount), "#fde68a");
        }

        // Special: Vampire Philter — extra heal + strength
        if (potion.Id == "vampire_potion")
        {
            _playerStrength += 2;
            SpawnFloatingText(playerTarget, "+2 STR", new Color("f87171"));
            SpawnRuneEffect(playerTarget, new Color("f87171"));
            Log(LocalizationService.Format("log.battle.potion_vampire", "Used {0}: gain 2 Strength and heal {1} HP", potion.DisplayName, potion.HealAmount), "#f87171");
        }

        // Fallback: if no effect was applied, do a small heal
        if (potion.HealAmount == 0 && potion.StrengthAmount == 0 && potion.EnergyAmount == 0
            && potion.BlockAmount == 0 && potion.MaxHpAmount == 0 && potion.Id != "vampire_potion")
        {
            var fallbackHeal = Math.Min(10, Math.Max(_playerMaxHp - _playerHp, 0));
            _playerHp += fallbackHeal;
            SpawnFloatingText(playerTarget, $"+{fallbackHeal} HP", new Color("86efac"));
            SpawnRuneEffect(playerTarget, new Color("86efac"));
            Log(LocalizationService.Format("log.battle.potion_unknown", "Used {0}: fallback heal {1} HP", potion.DisplayName, fallbackHeal), "#86efac");
        }
    }
}
