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
        button.Text = LocalizationService.Format("ui.battle.potion_slot_item", "{0}. {1}", slotIndex + 1, potion.Name);
        button.TooltipText = $"{potion.Name}\n{potion.Description}";
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

    private void ApplyPotionEffect(PotionData potion)
    {
        const int healingPotionHeal = 15;
        const int strengthPotionGain = 2;
        const int swiftPotionEnergy = 1;
        const int guardPotionBlock = 12;
        const int furyPotionStrength = 2;
        const int furyPotionEnergy = 1;

        var playerTarget = _playerCardView.EffectTarget();

        switch (potion.Id)
        {
            case "healing_potion":
            {
                var heal = Math.Min(healingPotionHeal, Math.Max(_playerMaxHp - _playerHp, 0));
                _playerHp += heal;
                SpawnFloatingText(playerTarget, $"+{heal} HP", new Color("86efac"));
                SpawnRuneEffect(playerTarget, new Color("86efac"));
                Log(LocalizationService.Format("log.battle.potion_healing", "Used {0}: heal {1} HP", potion.Name, heal), "#86efac");
                break;
            }
            case "strength_potion":
            {
                _playerStrength += strengthPotionGain;
                SpawnFloatingText(playerTarget, $"+{strengthPotionGain} STR", new Color("d8b4fe"));
                SpawnRuneEffect(playerTarget, new Color("d8b4fe"));
                Log(LocalizationService.Format("log.battle.potion_strength", "Used {0}: gain {1} Strength", potion.Name, strengthPotionGain), "#d8b4fe");
                break;
            }
            case "swift_potion":
            {
                _energy += swiftPotionEnergy;
                SpawnFloatingText(playerTarget, $"+{swiftPotionEnergy} EN", new Color("fde68a"));
                SpawnRuneEffect(playerTarget, new Color("fde68a"));
                Log(LocalizationService.Format("log.battle.potion_swift", "Used {0}: gain {1} Energy", potion.Name, swiftPotionEnergy), "#fde68a");
                break;
            }
            case "guard_potion":
            {
                _playerBlock += guardPotionBlock;
                SpawnFloatingText(playerTarget, $"+{guardPotionBlock} Block", new Color("93c5fd"));
                SpawnShieldEffect(playerTarget, new Color("93c5fd"));
                Log(LocalizationService.Format("log.battle.potion_guard", "Used {0}: gain {1} Block", potion.Name, guardPotionBlock), "#93c5fd");
                break;
            }
            case "fury_potion":
            {
                _playerStrength += furyPotionStrength;
                _energy += furyPotionEnergy;
                SpawnFloatingText(playerTarget, $"+{furyPotionStrength} STR +{furyPotionEnergy} EN", new Color("fca5a5"));
                SpawnRuneEffect(playerTarget, new Color("fca5a5"));
                Log(LocalizationService.Format("log.battle.potion_fury", "Used {0}: gain {1} Strength and {2} Energy", potion.Name, furyPotionStrength, furyPotionEnergy), "#fca5a5");
                break;
            }
            default:
            {
                var fallbackHeal = Math.Min(healingPotionHeal, Math.Max(_playerMaxHp - _playerHp, 0));
                _playerHp += fallbackHeal;
                SpawnFloatingText(playerTarget, $"+{fallbackHeal} HP", new Color("86efac"));
                SpawnRuneEffect(playerTarget, new Color("86efac"));
                Log(LocalizationService.Format("log.battle.potion_unknown", "Used {0}: fallback heal {1} HP", potion.Name, fallbackHeal), "#86efac");
                break;
            }
        }
    }
}
