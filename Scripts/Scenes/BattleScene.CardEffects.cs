using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public void ExecuteDiscardCards(CardData card, CardEffectData effect)
        {
            _scene.ResolveDiscardCardsEffect(card, effect);
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
            // Phase 3: player lunges toward the target. Impact frame fires at
            // the apex (the callback) — damage number, slash, hit-stop, flash
            // — then the player recoils back automatically.
            //
            // IMPORTANT: the enemy card the lunge starts on may be replaced by
            // RefreshEnemyRuntimeStatusUi (which destroys+recreates all enemy
            // views) between lunge start and impact. Capture the enemy *index*
            // and re-resolve the fresh target inside the callback so we don't
            // spawn floating text on a freed Control.
            var damageTaken = resolution.Taken;
            var capturedIndex = enemyIndex;
            PlayPlayerAttackAnimation(effectTarget, () =>
            {
                var freshTarget = EnemyEffectTarget(capturedIndex);
                TriggerHitStop(0.045f);
                SpawnFallingDamage(freshTarget, damageTaken, new Color("fda4af"));
                SpawnSlashEffect(freshTarget, new Color("fda4af"));
                TriggerEnemyHit();
                FlashPanel(freshTarget, new Color(1f, 0.55f, 0.55f, 1f));
                PulseImpact(freshTarget, 1.05f);
            });
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

    private void ResolveDiscardCardsEffect(CardData sourceCard, CardEffectData effect)
    {
        var count = effect.Amount;
        if (count <= 0 || _hand.Count == 0)
        {
            return;
        }

        var candidates = new List<CardData>(_hand.Count);
        for (var i = 0; i < _hand.Count; i++)
        {
            if (!ReferenceEquals(_hand[i], sourceCard))
            {
                candidates.Add(_hand[i]);
            }
        }

        if (candidates.Count == 0)
        {
            return;
        }

        // Fisher-Yates partial shuffle to pick `count` distinct random cards from candidates.
        for (var i = 0; i < Math.Min(count, candidates.Count); i++)
        {
            var swap = i + _rng.Next(candidates.Count - i);
            (candidates[i], candidates[swap]) = (candidates[swap], candidates[i]);
        }

        var pickCount = Math.Min(count, candidates.Count);
        for (var i = 0; i < pickCount; i++)
        {
            var discarded = candidates[i];
            _hand.Remove(discarded);

            Log(LocalizationService.Format(
                "log.battle.discarded",
                "{0} is discarded",
                discarded.GetLocalizedName()), "#94a3b8");

            if (discarded.Keywords.Contains(CardKeyword.Curious))
            {
                Log(LocalizationService.Format(
                    "log.battle.curious",
                    "{0} curiously activates",
                    discarded.GetLocalizedName()), "#fcd34d");

                var relicAttackBonus = _state.HasRelic("whetstone") ? 1 : 0;
                var executor = new BattleCardEffectExecutor(this, relicAttackBonus);
                CardEffectPipeline.Execute(discarded, executor);
                // Note: any DrawCards from a Curious auto-play is intentionally ignored here;
                // the outer card's pipeline owns DrawCount aggregation.
            }

            if (discarded.Keywords.Contains(CardKeyword.Exhaust))
            {
                _exhaustPile.Add(discarded);
            }
            else
            {
                _discardPile.Add(discarded);
            }
        }
    }

    private void RefreshEnemyRuntimeStatusUi()
    {
        if (_enemies.Count == 0)
        {
            return;
        }

        UpdateEnemySelectionUi();

        // The legacy single-enemy detail labels (HP/Block/Status) are gone —
        // each EnemyCardView surfaces its own HP bar + status chips via
        // Configure(), refreshed by UpdateEnemySelectionUi().
    }
}
