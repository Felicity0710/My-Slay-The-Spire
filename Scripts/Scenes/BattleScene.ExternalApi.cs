using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class BattleScene
{
    public BattleSnapshot BuildBattleSnapshot()
    {
        var snapshot = new BattleSnapshot
        {
            Turn = _turn,
            Energy = _energy,
            MaxEnergy = MaxEnergy,
            BattleEnded = _battleEnded,
            InputLocked = IsInputLocked(),
            DrawPileCount = _drawPile.Count,
            DiscardPileCount = _discardPile.Count,
            SelectedEnemyIndex = _selectedEnemyIndex,
            Player = new PlayerBattleSnapshot
            {
                Hp = _playerHp,
                MaxHp = _playerMaxHp,
                Block = _playerBlock,
                Strength = _playerStrength,
                Vulnerable = _playerVulnerable
            }
        };

        for (var i = 0; i < _hand.Count; i++)
        {
            var card = _hand[i];
            snapshot.Hand.Add(new CardSnapshot
            {
                HandIndex = i,
                CardId = card.Id,
                Name = card.Name,
                Cost = card.Cost,
                RequiresEnemyTarget = CardRequiresEnemyTarget(card),
                IsPlayable = !_battleEnded && !IsInputLocked() && card.Cost <= _energy,
                Description = card.GetLocalizedDescription()
            });
        }

        for (var i = 0; i < _enemies.Count; i++)
        {
            var enemy = _enemies[i];
            snapshot.Enemies.Add(new EnemyBattleSnapshot
            {
                EnemyIndex = i,
                ArchetypeId = enemy.ArchetypeId,
                Name = enemy.Name,
                Hp = enemy.Hp,
                MaxHp = enemy.MaxHp,
                Block = enemy.Block,
                Strength = enemy.Strength,
                Vulnerable = enemy.Vulnerable,
                IsAlive = enemy.IsAlive,
                IsSelected = i == _selectedEnemyIndex,
                IntentType = enemy.IntentType.ToString(),
                IntentValue = enemy.IntentValue,
                IntentText = _battleEnded || !enemy.IsAlive ? "-" : IntentText(enemy)
            });
        }

        return snapshot;
    }

    public List<LegalActionSnapshot> BuildLegalActions()
    {
        var actions = new List<LegalActionSnapshot>
        {
            new()
            {
                Kind = "start_new_run",
                Label = "Start a new map run"
            },
            new()
            {
                Kind = "start_battle_test_run",
                Label = "Start a battle test run"
            }
        };

        if (_battleEnded || IsInputLocked())
        {
            return actions;
        }

        actions.Add(new LegalActionSnapshot
        {
            Kind = "end_turn",
            Label = "End the current turn"
        });

        for (var i = 0; i < _hand.Count; i++)
        {
            var card = _hand[i];
            if (card.Cost > _energy)
            {
                continue;
            }

            var parameters = new Dictionary<string, object?>
            {
                ["handIndex"] = i,
                ["cardId"] = card.Id
            };
            var label = $"Play {card.Name} from hand index {i}";
            if (CardRequiresEnemyTarget(card))
            {
                var targets = new List<int>();
                for (var enemyIndex = 0; enemyIndex < _enemies.Count; enemyIndex++)
                {
                    if (_enemies[enemyIndex].IsAlive)
                    {
                        targets.Add(enemyIndex);
                    }
                }

                parameters["targetEnemyIndices"] = targets;
                if (targets.Count == 0)
                {
                    continue;
                }
            }

            actions.Add(new LegalActionSnapshot
            {
                Kind = "play_card",
                Label = label,
                Parameters = parameters
            });
        }

        return actions;
    }

    public async Task<string?> TryPlayCardExternallyAsync(int? handIndex, string? cardId, int? targetEnemyIndex)
    {
        if (_battleEnded || IsInputLocked())
        {
            return "Battle input is currently locked.";
        }

        var resolvedHandIndex = ResolveHandIndex(handIndex, cardId);
        if (resolvedHandIndex < 0 || resolvedHandIndex >= _hand.Count)
        {
            return "Requested card is not in hand.";
        }

        var card = _hand[resolvedHandIndex];
        if (CardRequiresEnemyTarget(card))
        {
            if (!targetEnemyIndex.HasValue)
            {
                return "This card requires 'targetEnemyIndex'.";
            }

            if (targetEnemyIndex.Value < 0 || targetEnemyIndex.Value >= _enemies.Count)
            {
                return "targetEnemyIndex is out of range.";
            }

            if (!_enemies[targetEnemyIndex.Value].IsAlive)
            {
                return "The selected enemy is already defeated.";
            }

            _selectedEnemyIndex = targetEnemyIndex.Value;
            SyncEnemyVisualFromSelection();
        }

        PushInputLock();
        try
        {
            var played = await TrySpendAndApplyCard(card);
            if (!played)
            {
                return "Card play failed.";
            }

            return null;
        }
        finally
        {
            PopInputLock();
        }
    }

    public async Task<string?> TryEndTurnExternallyAsync()
    {
        if (_battleEnded || IsInputLocked())
        {
            return "Battle input is currently locked.";
        }

        await EndTurnAsync();
        return null;
    }

    private int ResolveHandIndex(int? handIndex, string? cardId)
    {
        if (handIndex.HasValue && handIndex.Value >= 0 && handIndex.Value < _hand.Count)
        {
            return handIndex.Value;
        }

        if (!string.IsNullOrWhiteSpace(cardId))
        {
            for (var i = 0; i < _hand.Count; i++)
            {
                if (string.Equals(_hand[i].Id, cardId, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }

        return -1;
    }
}
