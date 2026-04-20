using Godot;
using System;

public partial class BattleScene
{
    private sealed class BattleCardEffectExecutor : ICardEffectRuntime
    {
        private readonly BattleScene _scene;
        private readonly int _relicAttackBonus;

        public BattleCardEffectExecutor(BattleScene scene, int relicAttackBonus)
        {
            _scene = scene;
            _relicAttackBonus = relicAttackBonus;
        }

        public void ExecuteDamage(CardData card, CardEffectData effect)
        {
            _scene.ResolveDamageCardEffect(card, effect, _relicAttackBonus);
        }

        public void ExecuteGainBlock(CardData card, CardEffectData effect)
        {
            _scene.ApplyGainBlockEffect(card, effect);
        }

        public void ExecuteApplyVulnerable(CardData card, CardEffectData effect)
        {
            _scene.ResolveApplyVulnerableEffect(card, effect);
        }

        public void ExecuteGainStrength(CardData card, CardEffectData effect)
        {
            _scene.ApplyGainStrengthEffect(card, effect);
        }

        public void ExecuteGainEnergy(CardData card, CardEffectData effect)
        {
            _scene.ApplyGainEnergyEffect(card, effect);
        }

        public void ExecuteHeal(CardData card, CardEffectData effect)
        {
            _scene.ApplyHealEffect(card, effect);
        }
    }

    private void ApplyGainBlockEffect(CardData card, CardEffectData effect)
    {
        if (effect.Target != CardEffectTarget.Player || effect.Amount <= 0)
        {
            return;
        }

        _playerBlock += effect.Amount;
        Log(LocalizationService.Format("log.battle.play_gain_block", "Play {0}: gain {1} Block", card.GetLocalizedName(), effect.Amount), "#60a5fa");
        var playerEffectTarget = _playerCardView.EffectTarget();
        SpawnFloatingText(playerEffectTarget, $"+{effect.Amount}", new Color("93c5fd"));
        SpawnShieldEffect(playerEffectTarget, new Color("93c5fd"));
    }

    private void ApplyGainStrengthEffect(CardData card, CardEffectData effect)
    {
        if (effect.Target != CardEffectTarget.Player || effect.Amount <= 0)
        {
            return;
        }

        _playerStrength += effect.Amount;
        var playerEffectTarget = _playerCardView.EffectTarget();
        Log(LocalizationService.Format("log.battle.play_gain_strength", "Play {0}: gain {1} Strength", card.GetLocalizedName(), effect.Amount), "#c084fc");
        SpawnFloatingText(playerEffectTarget, $"STR+{effect.Amount}", new Color("d8b4fe"));
        SpawnRuneEffect(playerEffectTarget, new Color("d8b4fe"));
    }

    private void ApplyGainEnergyEffect(CardData card, CardEffectData effect)
    {
        if (effect.Target != CardEffectTarget.Player || effect.Amount <= 0)
        {
            return;
        }

        _energy += effect.Amount;
        var playerEffectTarget = _playerCardView.EffectTarget();
        Log(LocalizationService.Format("log.battle.play_gain_energy", "Play {0}: gain {1} Energy", card.GetLocalizedName(), effect.Amount), "#fde68a");
        SpawnFloatingText(playerEffectTarget, $"EN+{effect.Amount}", new Color("fde68a"));
    }

    private void ApplyHealEffect(CardData card, CardEffectData effect)
    {
        if (effect.Target != CardEffectTarget.Player || effect.Amount <= 0)
        {
            return;
        }

        var before = _playerHp;
        _playerHp = Math.Min(_playerHp + effect.Amount, _playerMaxHp);
        var gained = _playerHp - before;
        if (gained <= 0)
        {
            return;
        }

        var playerEffectTarget = _playerCardView.EffectTarget();
        Log(LocalizationService.Format("log.battle.play_heal", "Play {0}: heal {1}", card.GetLocalizedName(), gained), "#86efac");
        SpawnFloatingText(playerEffectTarget, $"+{gained} HP", new Color("86efac"));
    }

    private void ResolveDamageCardEffect(CardData card, CardEffectData effect, int relicAttackBonus)
    {
        if (effect.Amount <= 0)
        {
            return;
        }

        if (effect.Target == CardEffectTarget.AllEnemies)
        {
            for (var enemyIndex = 0; enemyIndex < _enemies.Count; enemyIndex++)
            {
                if (_enemies[enemyIndex].IsAlive)
                {
                    ApplyDamageToEnemy(enemyIndex, card, effect, relicAttackBonus);
                }
            }
            return;
        }

        ApplyDamageToEnemy(_selectedEnemyIndex, card, effect, relicAttackBonus);
    }

    private void ResolveApplyVulnerableEffect(CardData card, CardEffectData effect)
    {
        if (effect.Amount <= 0)
        {
            return;
        }

        if (effect.Target == CardEffectTarget.AllEnemies)
        {
            for (var enemyIndex = 0; enemyIndex < _enemies.Count; enemyIndex++)
            {
                if (_enemies[enemyIndex].IsAlive)
                {
                    ApplyVulnerableToEnemy(enemyIndex, card.Name, effect.Amount);
                }
            }
            return;
        }

        ApplyVulnerableToEnemy(_selectedEnemyIndex, card.Name, effect.Amount);
    }

    private void ApplyDamageToEnemy(int enemyIndex, CardData card, CardEffectData effect, int relicAttackBonus)
    {
        var targetEnemy = _enemies[enemyIndex];
        if (!targetEnemy.IsAlive)
        {
            return;
        }

        var strength = effect.UseAttackerStrength ? _playerStrength : 0;
        var vulnerable = effect.UseTargetVulnerable ? targetEnemy.Vulnerable : 0;
        var flatBonus = effect.FlatBonus + relicAttackBonus;

        var resolution = CombatResolver.ResolveHit(
            effect.Amount,
            strength,
            vulnerable,
            targetEnemy.Block,
            targetEnemy.Hp,
            flatBonus);

        targetEnemy.Block = resolution.RemainingBlock;
        targetEnemy.Hp = resolution.RemainingHp;

        var effectTarget = EnemyEffectTarget(enemyIndex);
        Log(LocalizationService.Format("log.battle.play_damage", "Play {0}: damage {1} ({2} HP)", card.GetLocalizedName(), resolution.FinalDamage, resolution.Taken), "#f87171");
        if (resolution.Taken > 0)
        {
            TriggerHitStop(0.045f);
            SpawnFloatingText(effectTarget, $"-{resolution.Taken}", new Color("fda4af"));
            SpawnSlashEffect(effectTarget, new Color("fda4af"));
            TriggerEnemyHit();
            FlashPanel(effectTarget, new Color(1f, 0.55f, 0.55f, 1f));
            PunchPanel(effectTarget, 8f);
            PulseImpact(effectTarget, 1.05f);
        }

        RefreshEnemyRuntimeStatusUi();
    }

    private void ApplyVulnerableToEnemy(int enemyIndex, string cardName, int amount)
    {
        var enemy = _enemies[enemyIndex];
        if (!enemy.IsAlive)
        {
            return;
        }

        enemy.Vulnerable += amount;
        var effectTarget = EnemyEffectTarget(enemyIndex);

        Log(LocalizationService.Format("log.battle.play_vulnerable", "Play {0}: apply {1} Vulnerable", cardName, amount), "#c084fc");
        SpawnFloatingText(effectTarget, $"VUL+{amount}", new Color("d8b4fe"));
        SpawnRuneEffect(effectTarget, new Color("d8b4fe"));

        RefreshEnemyRuntimeStatusUi();
    }

    private void RefreshEnemyRuntimeStatusUi()
    {
        if (_enemies.Count == 0)
        {
            return;
        }

        UpdateEnemySelectionUi();

        if (_selectedEnemyIndex < 0 || _selectedEnemyIndex >= _enemies.Count)
        {
            return;
        }

        var enemy = _enemies[_selectedEnemyIndex];
        _enemyHpLabel.Text = LocalizationService.Format("ui.battle.enemy_hp", "Enemy HP: {0}", enemy.Hp);
        _enemyBlockLabel.Text = LocalizationService.Format("ui.battle.enemy_block", "Enemy Block: {0}", enemy.Block);
        _enemyStatusLabel.Text = LocalizationService.Format("ui.battle.enemy_status", "Enemy Status: STR {0}, VUL {1}", enemy.Strength, enemy.Vulnerable);
    }
}
