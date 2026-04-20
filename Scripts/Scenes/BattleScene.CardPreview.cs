using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;

public partial class BattleScene
{
    private static readonly Regex NumberTokenRegex = new(@"\d+", RegexOptions.Compiled);

    private sealed class PreviewEnemyState
    {
        public int Hp;
        public int Block;
        public int Vulnerable;

        public bool IsAlive => Hp > 0;

        public PreviewEnemyState(EnemyUnit enemy)
        {
            Hp = enemy.Hp;
            Block = enemy.Block;
            Vulnerable = enemy.Vulnerable;
        }
    }

    private void RefreshDraggingCardPreview(CardView card, Vector2 mouseGlobal)
    {
        if (!IsInstanceValid(card))
        {
            return;
        }

        if (_battleEnded || IsInputLocked())
        {
            card.ClearPreviewDescription();
            return;
        }

        var previewTargetEnemyIndex = ResolvePreviewTargetEnemyIndex(card.Card, mouseGlobal);
        var previewDescription = BuildDynamicPreviewDescription(card.Card, previewTargetEnemyIndex);
        card.SetPreviewDescription(previewDescription);
    }

    private void ClearDraggingCardPreview(CardView card)
    {
        if (IsInstanceValid(card))
        {
            card.ClearPreviewDescription();
        }
    }

    private int ResolvePreviewTargetEnemyIndex(CardData card, Vector2 mouseGlobal)
    {
        if (CardRequiresEnemyTarget(card))
        {
            if (TryGetEnemyIndexAt(mouseGlobal, out var hoveredEnemyIndex) && IsValidAliveEnemyIndex(hoveredEnemyIndex))
            {
                return hoveredEnemyIndex;
            }

            if (IsValidAliveEnemyIndex(_hoverEnemyIndex))
            {
                return _hoverEnemyIndex;
            }
        }

        if (IsValidAliveEnemyIndex(_selectedEnemyIndex))
        {
            return _selectedEnemyIndex;
        }

        for (var i = 0; i < _enemies.Count; i++)
        {
            if (_enemies[i].IsAlive)
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsValidAliveEnemyIndex(int index)
    {
        return index >= 0 && index < _enemies.Count && _enemies[index].IsAlive;
    }

    private string BuildDynamicPreviewDescription(CardData card, int previewTargetEnemyIndex)
    {
        var baseDescription = card.GetLocalizedDescription();
        if (string.IsNullOrWhiteSpace(baseDescription))
        {
            return baseDescription;
        }

        var enemies = BuildPreviewEnemyStates();
        var previewNumbers = BuildPreviewNumberReplacements(card, enemies, previewTargetEnemyIndex);
        if (previewNumbers.Count == 0)
        {
            return baseDescription;
        }

        return ReplaceDescriptionNumbers(baseDescription, previewNumbers);
    }

    private List<PreviewEnemyState> BuildPreviewEnemyStates()
    {
        var copied = new List<PreviewEnemyState>(_enemies.Count);
        for (var i = 0; i < _enemies.Count; i++)
        {
            copied.Add(new PreviewEnemyState(_enemies[i]));
        }

        return copied;
    }

    private List<int> BuildPreviewNumberReplacements(CardData card, List<PreviewEnemyState> enemies, int previewTargetEnemyIndex)
    {
        var previewNumbers = new List<int>(card.Effects.Count);
        var previewPlayerHp = _playerHp;
        var previewPlayerBlock = _playerBlock;
        var previewPlayerStrength = _playerStrength;
        var previewEnergy = _energy;
        var relicAttackBonus = _state != null && _state.HasRelic("whetstone") ? 1 : 0;

        foreach (var effect in card.Effects)
        {
            switch (effect.Type)
            {
                case CardEffectType.Damage:
                    AppendDamagePreviewNumber(previewNumbers, enemies, effect, previewTargetEnemyIndex, previewPlayerStrength, relicAttackBonus);
                    break;
                case CardEffectType.GainBlock:
                    AppendGainBlockPreviewNumber(previewNumbers, effect, ref previewPlayerBlock);
                    break;
                case CardEffectType.ApplyVulnerable:
                    AppendApplyVulnerablePreviewNumber(previewNumbers, enemies, effect, previewTargetEnemyIndex);
                    break;
                case CardEffectType.GainStrength:
                    AppendGainStrengthPreviewNumber(previewNumbers, effect, ref previewPlayerStrength);
                    break;
                case CardEffectType.GainEnergy:
                    AppendGainEnergyPreviewNumber(previewNumbers, effect, ref previewEnergy);
                    break;
                case CardEffectType.Heal:
                    AppendHealPreviewNumber(previewNumbers, effect, ref previewPlayerHp);
                    break;
                case CardEffectType.DrawCards:
                    AppendDrawPreviewNumber(previewNumbers, effect);
                    break;
            }
        }

        return previewNumbers;
    }

    private void AppendDamagePreviewNumber(
        List<int> previewNumbers,
        List<PreviewEnemyState> enemies,
        CardEffectData effect,
        int previewTargetEnemyIndex,
        int previewPlayerStrength,
        int relicAttackBonus)
    {
        var previewDamage = Math.Max(effect.Amount, 0);
        if (effect.Amount <= 0)
        {
            previewNumbers.Add(previewDamage);
            return;
        }

        if (effect.Target == CardEffectTarget.SelectedEnemy)
        {
            if (IsValidPreviewEnemyIndex(enemies, previewTargetEnemyIndex))
            {
                var enemy = enemies[previewTargetEnemyIndex];
                previewDamage = ResolvePreviewDamage(effect, previewPlayerStrength, relicAttackBonus, enemy).FinalDamage;

                for (var repeat = 0; repeat < effect.Repeat; repeat++)
                {
                    if (!enemy.IsAlive)
                    {
                        break;
                    }

                    var resolution = ResolvePreviewDamage(effect, previewPlayerStrength, relicAttackBonus, enemy);
                    enemy.Block = resolution.RemainingBlock;
                    enemy.Hp = resolution.RemainingHp;
                }
            }

            previewNumbers.Add(previewDamage);
            return;
        }

        if (effect.Target == CardEffectTarget.AllEnemies)
        {
            var referenceEnemyIndex = ResolvePreviewReferenceEnemyIndex(enemies, previewTargetEnemyIndex);
            if (referenceEnemyIndex >= 0)
            {
                previewDamage = ResolvePreviewDamage(effect, previewPlayerStrength, relicAttackBonus, enemies[referenceEnemyIndex]).FinalDamage;
            }

            for (var repeat = 0; repeat < effect.Repeat; repeat++)
            {
                for (var enemyIndex = 0; enemyIndex < enemies.Count; enemyIndex++)
                {
                    var enemy = enemies[enemyIndex];
                    if (!enemy.IsAlive)
                    {
                        continue;
                    }

                    var resolution = ResolvePreviewDamage(effect, previewPlayerStrength, relicAttackBonus, enemy);
                    enemy.Block = resolution.RemainingBlock;
                    enemy.Hp = resolution.RemainingHp;
                }
            }

            previewNumbers.Add(previewDamage);
            return;
        }

        previewNumbers.Add(previewDamage);
    }

    private int ResolvePreviewReferenceEnemyIndex(IReadOnlyList<PreviewEnemyState> enemies, int preferredEnemyIndex)
    {
        if (IsValidPreviewEnemyIndex(enemies, preferredEnemyIndex))
        {
            return preferredEnemyIndex;
        }

        for (var i = 0; i < enemies.Count; i++)
        {
            if (enemies[i].IsAlive)
            {
                return i;
            }
        }

        return -1;
    }

    private DamageResolution ResolvePreviewDamage(CardEffectData effect, int previewPlayerStrength, int relicAttackBonus, PreviewEnemyState enemy)
    {
        var strength = effect.UseAttackerStrength ? previewPlayerStrength : 0;
        var vulnerable = effect.UseTargetVulnerable ? enemy.Vulnerable : 0;
        var flatBonus = effect.FlatBonus + relicAttackBonus;

        return CombatResolver.ResolveHit(
            effect.Amount,
            strength,
            vulnerable,
            enemy.Block,
            enemy.Hp,
            flatBonus);
    }

    private bool IsValidPreviewEnemyIndex(IReadOnlyList<PreviewEnemyState> enemies, int enemyIndex)
    {
        return enemyIndex >= 0 && enemyIndex < enemies.Count && enemies[enemyIndex].IsAlive;
    }

    private void AppendGainBlockPreviewNumber(List<int> previewNumbers, CardEffectData effect, ref int previewPlayerBlock)
    {
        var totalGain = Math.Max(effect.Amount, 0) * effect.Repeat;
        previewNumbers.Add(totalGain);

        if (effect.Target == CardEffectTarget.Player)
        {
            previewPlayerBlock += totalGain;
        }
    }

    private void AppendApplyVulnerablePreviewNumber(
        List<int> previewNumbers,
        List<PreviewEnemyState> enemies,
        CardEffectData effect,
        int previewTargetEnemyIndex)
    {
        var totalAmount = Math.Max(effect.Amount, 0) * effect.Repeat;
        previewNumbers.Add(totalAmount);

        if (totalAmount <= 0)
        {
            return;
        }

        if (effect.Target == CardEffectTarget.AllEnemies)
        {
            for (var i = 0; i < enemies.Count; i++)
            {
                if (!enemies[i].IsAlive)
                {
                    continue;
                }

                enemies[i].Vulnerable += totalAmount;
            }

            return;
        }

        if (effect.Target == CardEffectTarget.SelectedEnemy && IsValidPreviewEnemyIndex(enemies, previewTargetEnemyIndex))
        {
            enemies[previewTargetEnemyIndex].Vulnerable += totalAmount;
        }
    }

    private void AppendGainStrengthPreviewNumber(List<int> previewNumbers, CardEffectData effect, ref int previewPlayerStrength)
    {
        var totalGain = Math.Max(effect.Amount, 0) * effect.Repeat;
        previewNumbers.Add(totalGain);

        if (effect.Target == CardEffectTarget.Player)
        {
            previewPlayerStrength += totalGain;
        }
    }

    private void AppendGainEnergyPreviewNumber(List<int> previewNumbers, CardEffectData effect, ref int previewEnergy)
    {
        var totalGain = Math.Max(effect.Amount, 0) * effect.Repeat;
        previewNumbers.Add(totalGain);

        if (effect.Target == CardEffectTarget.Player)
        {
            previewEnergy += totalGain;
        }
    }

    private void AppendHealPreviewNumber(List<int> previewNumbers, CardEffectData effect, ref int previewPlayerHp)
    {
        if (effect.Target != CardEffectTarget.Player || effect.Amount <= 0)
        {
            previewNumbers.Add(Math.Max(effect.Amount, 0) * effect.Repeat);
            return;
        }

        var totalHeal = 0;
        for (var repeat = 0; repeat < effect.Repeat; repeat++)
        {
            var before = previewPlayerHp;
            previewPlayerHp = Math.Min(previewPlayerHp + effect.Amount, _playerMaxHp);
            totalHeal += previewPlayerHp - before;
        }

        previewNumbers.Add(totalHeal);
    }

    private void AppendDrawPreviewNumber(List<int> previewNumbers, CardEffectData effect)
    {
        var totalDraw = Math.Max(effect.Amount, 0) * effect.Repeat;
        previewNumbers.Add(totalDraw);
    }

    private string ReplaceDescriptionNumbers(string baseDescription, IReadOnlyList<int> previewNumbers)
    {
        if (previewNumbers.Count == 0)
        {
            return baseDescription;
        }

        var replaceIndex = 0;
        return NumberTokenRegex.Replace(baseDescription, match =>
        {
            if (replaceIndex >= previewNumbers.Count)
            {
                return match.Value;
            }

            var replacement = previewNumbers[replaceIndex++];
            return replacement.ToString();
        });
    }
}
