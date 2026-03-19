namespace SlayHs.Agent;

internal sealed class SimpleBot
{
    private readonly GameBridgeClient _client;

    public SimpleBot(GameBridgeClient client)
    {
        _client = client;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        Console.Error.WriteLine("simple-bot: waiting for game bridge...");

        SnapshotEnvelope snapshot;
        try
        {
            snapshot = (await _client.GetSnapshotAsync(cancellationToken)).Snapshot
                ?? throw new InvalidOperationException("Missing snapshot.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"simple-bot: cannot reach game bridge: {ex.Message}");
            return 1;
        }

        for (var step = 0; step < 500 && !cancellationToken.IsCancellationRequested; step++)
        {
            LogSnapshot(step, snapshot);
            var action = ChooseAction(snapshot);
            if (action == null)
            {
                Console.Error.WriteLine($"simple-bot: no action for scene '{snapshot.SceneType}', stopping.");
                LogLegalActions(snapshot);
                return 0;
            }

            Console.Error.WriteLine($"simple-bot: action -> {FormatAction(action)}");
            var result = await _client.ExecuteAsync(action, snapshot.StateVersion, cancellationToken);
            if (!result.Ok)
            {
                if (result.Message.Contains("State version mismatch", StringComparison.OrdinalIgnoreCase)
                    && result.Snapshot != null)
                {
                    Console.Error.WriteLine("simple-bot: version mismatch, refreshing snapshot and continuing.");
                    snapshot = result.Snapshot;
                    continue;
                }

                Console.Error.WriteLine($"simple-bot: action failed: {result.Message}");
                LogLegalActions(snapshot);
                return 1;
            }

            snapshot = result.Snapshot ?? throw new InvalidOperationException("Missing post-action snapshot.");

            if (snapshot.SceneType == "main_menu" && step > 0)
            {
                Console.Error.WriteLine("simple-bot: back on main menu, stopping.");
                return 0;
            }
        }

        Console.Error.WriteLine("simple-bot: reached step limit.");
        return 0;
    }

    private static void LogSnapshot(int step, SnapshotEnvelope snapshot)
    {
        var run = snapshot.Run;
        Console.Error.WriteLine(
            $"simple-bot: step={step} scene={snapshot.SceneType} version={snapshot.StateVersion} " +
            $"floor={run.Floor} hp={run.PlayerHp}/{run.MaxHp} wins={run.BattlesWon} legal={snapshot.LegalActions.Count}");

        switch (snapshot.SceneType)
        {
            case "battle" when snapshot.Battle != null:
                var battle = snapshot.Battle;
                var alive = battle.Enemies.Count(enemy => enemy.IsAlive);
                Console.Error.WriteLine(
                    $"simple-bot: battle turn={battle.Turn} energy={battle.Energy} hand={battle.Hand.Count} enemiesAlive={alive}");
                break;
            case "reward" when snapshot.Reward != null:
                Console.Error.WriteLine(
                    $"simple-bot: reward mode={snapshot.Reward.Mode} rewardTypes={string.Join(",", snapshot.Reward.RewardTypes)}");
                break;
            case "event" when snapshot.Event != null:
                Console.Error.WriteLine(
                    $"simple-bot: event id={snapshot.Event.EventId} title={snapshot.Event.Title}");
                break;
            case "map" when snapshot.Map != null:
                Console.Error.WriteLine(
                    $"simple-bot: map row={snapshot.Map.CurrentRow}");
                break;
        }
    }

    private static void LogLegalActions(SnapshotEnvelope snapshot)
    {
        if (snapshot.LegalActions.Count == 0)
        {
            Console.Error.WriteLine("simple-bot: legal actions -> none");
            return;
        }

        foreach (var action in snapshot.LegalActions.Take(8))
        {
            Console.Error.WriteLine($"simple-bot: legal -> {action.Kind} ({action.Label})");
        }
    }

    private static string FormatAction(ActionEnvelope action)
    {
        var parts = new List<string> { action.Kind };
        if (action.HandIndex.HasValue)
        {
            parts.Add($"handIndex={action.HandIndex.Value}");
        }
        if (!string.IsNullOrWhiteSpace(action.CardId))
        {
            parts.Add($"cardId={action.CardId}");
        }
        if (action.TargetEnemyIndex.HasValue)
        {
            parts.Add($"targetEnemyIndex={action.TargetEnemyIndex.Value}");
        }
        if (action.Column.HasValue)
        {
            parts.Add($"column={action.Column.Value}");
        }
        if (!string.IsNullOrWhiteSpace(action.RewardType))
        {
            parts.Add($"rewardType={action.RewardType}");
        }
        if (action.OptionIndex.HasValue)
        {
            parts.Add($"optionIndex={action.OptionIndex.Value}");
        }
        if (!string.IsNullOrWhiteSpace(action.EventOption))
        {
            parts.Add($"eventOption={action.EventOption}");
        }

        return string.Join(" ", parts);
    }

    private static ActionEnvelope? ChooseAction(SnapshotEnvelope snapshot)
    {
        var action = snapshot.SceneType switch
        {
            "main_menu" => new ActionEnvelope { Kind = "start_new_run" },
            "map" => ChooseMapAction(snapshot.Map),
            "battle" => ChooseBattleAction(snapshot.Battle),
            "reward" => ChooseRewardAction(snapshot.Reward),
            "event" => ChooseEventAction(snapshot.Event),
            _ => null
        };

        return action ?? ChooseFallbackAction(snapshot);
    }

    private static ActionEnvelope? ChooseMapAction(MapEnvelope? map)
    {
        if (map == null)
        {
            return null;
        }

        var currentRow = map.Rows.FirstOrDefault(row => row.RowIndex == map.CurrentRow);
        if (currentRow == null)
        {
            return null;
        }

        var priority = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["NormalBattle"] = 0,
            ["EliteBattle"] = 1,
            ["Event"] = 2,
            ["Rest"] = 3,
            ["Shop"] = 4
        };

        var node = currentRow.Nodes
            .Where(candidate => candidate.CanSelect)
            .OrderBy(candidate => priority.TryGetValue(candidate.NodeType, out var rank) ? rank : 99)
            .ThenBy(candidate => candidate.Column)
            .FirstOrDefault();

        return node == null
            ? null
            : new ActionEnvelope
            {
                Kind = "choose_map_node",
                Column = node.Column
            };
    }

    private static ActionEnvelope? ChooseBattleAction(BattleEnvelope? battle)
    {
        if (battle == null || battle.BattleEnded)
        {
            return null;
        }

        var enemies = battle.Enemies.Where(enemy => enemy.IsAlive).ToList();
        var cards = battle.Hand.Where(card => card.IsPlayable).ToList();

        foreach (var enemy in enemies.OrderBy(enemy => enemy.Hp + enemy.Block))
        {
            foreach (var card in cards.OrderByDescending(EstimateDirectDamage))
            {
                var damage = EstimateDirectDamage(card);
                if (damage <= 0 || !card.RequiresEnemyTarget)
                {
                    continue;
                }

                if (damage >= enemy.Hp + enemy.Block)
                {
                    return new ActionEnvelope
                    {
                        Kind = "play_card",
                        HandIndex = card.HandIndex,
                        TargetEnemyIndex = enemy.EnemyIndex
                    };
                }
            }
        }

        var vulnerable = cards
            .Where(card => card.Description.Contains("Vulnerable", StringComparison.OrdinalIgnoreCase) ||
                           card.Description.Contains("易伤", StringComparison.OrdinalIgnoreCase))
            .OrderBy(card => card.Cost)
            .FirstOrDefault();
        if (vulnerable != null)
        {
            var target = enemies.OrderByDescending(enemy => enemy.Hp + enemy.Block).FirstOrDefault();
            if (target != null)
            {
                return new ActionEnvelope
                {
                    Kind = "play_card",
                    HandIndex = vulnerable.HandIndex,
                    TargetEnemyIndex = target.EnemyIndex
                };
            }
        }

        var bestAttack = cards
            .Where(card => EstimateDirectDamage(card) > 0)
            .OrderByDescending(EstimateDirectDamage)
            .ThenBy(card => card.Cost)
            .FirstOrDefault();
        if (bestAttack != null)
        {
            var target = enemies.OrderBy(enemy => enemy.Hp + enemy.Block).FirstOrDefault();
            if (bestAttack.RequiresEnemyTarget && target != null)
            {
                return new ActionEnvelope
                {
                    Kind = "play_card",
                    HandIndex = bestAttack.HandIndex,
                    TargetEnemyIndex = target.EnemyIndex
                };
            }

            return new ActionEnvelope
            {
                Kind = "play_card",
                HandIndex = bestAttack.HandIndex
            };
        }

        var defense = cards
            .Where(card => card.Description.Contains("Block", StringComparison.OrdinalIgnoreCase) ||
                           card.Description.Contains("格挡", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(card => card.Cost)
            .FirstOrDefault();
        if (defense != null)
        {
            return new ActionEnvelope
            {
                Kind = "play_card",
                HandIndex = defense.HandIndex
            };
        }

        return new ActionEnvelope { Kind = "end_turn" };
    }

    private static int EstimateDirectDamage(CardEnvelope card)
    {
        var description = card.Description;
        var digitBuffer = string.Empty;
        var best = 0;

        foreach (var ch in description)
        {
            if (char.IsDigit(ch))
            {
                digitBuffer += ch;
                continue;
            }

            if (digitBuffer.Length > 0)
            {
                if (int.TryParse(digitBuffer, out var value))
                {
                    best = Math.Max(best, value);
                }

                digitBuffer = string.Empty;
            }
        }

        if (digitBuffer.Length > 0 && int.TryParse(digitBuffer, out var trailing))
        {
            best = Math.Max(best, trailing);
        }

        return best;
    }

    private static ActionEnvelope? ChooseRewardAction(RewardEnvelope? reward)
    {
        if (reward == null)
        {
            return null;
        }

        if (string.Equals(reward.Mode, "card_pack", StringComparison.OrdinalIgnoreCase))
        {
            var bestCard = reward.CardOptions
                .OrderByDescending(option => ScoreRewardCard(option))
                .ThenBy(option => option.OptionIndex)
                .FirstOrDefault();

            return bestCard == null
                ? new ActionEnvelope { Kind = "skip_reward" }
                : new ActionEnvelope
                {
                    Kind = "choose_reward_card",
                    OptionIndex = bestCard.OptionIndex
                };
        }

        var preference = reward.RewardTypes.Contains("relic", StringComparer.OrdinalIgnoreCase)
            ? "relic"
            : reward.RewardTypes.Contains("card_pack", StringComparer.OrdinalIgnoreCase)
                ? "card_pack"
                : reward.RewardTypes.Contains("potion", StringComparer.OrdinalIgnoreCase)
                    ? "potion"
                    : "random";

        return new ActionEnvelope
        {
            Kind = "choose_reward_type",
            RewardType = preference
        };
    }

    private static int ScoreRewardCard(RewardOptionEnvelope option)
    {
        var text = $"{option.Name} {option.Description}";
        var score = 0;
        if (text.Contains("Strength", StringComparison.OrdinalIgnoreCase) || text.Contains("力量", StringComparison.OrdinalIgnoreCase))
        {
            score += 40;
        }
        if (text.Contains("Energy", StringComparison.OrdinalIgnoreCase) || text.Contains("能量", StringComparison.OrdinalIgnoreCase))
        {
            score += 35;
        }
        if (text.Contains("Vulnerable", StringComparison.OrdinalIgnoreCase) || text.Contains("易伤", StringComparison.OrdinalIgnoreCase))
        {
            score += 20;
        }
        score += text.Count(char.IsDigit);
        return score;
    }

    private static ActionEnvelope? ChooseEventAction(EventEnvelope? eventState)
    {
        if (eventState == null)
        {
            return null;
        }

        return new ActionEnvelope
        {
            Kind = "choose_event_option",
            OptionIndex = 0
        };
    }

    private static ActionEnvelope? ChooseFallbackAction(SnapshotEnvelope snapshot)
    {
        var legalAction = snapshot.LegalActions.FirstOrDefault();
        if (legalAction == null)
        {
            return null;
        }

        var parameters = legalAction.Parameters;
        var action = new ActionEnvelope
        {
            Kind = legalAction.Kind
        };

        if (parameters.TryGetPropertyValue("column", out var columnNode) && columnNode != null)
        {
            action.Column = columnNode.GetValue<int>();
        }
        if (parameters.TryGetPropertyValue("handIndex", out var handIndexNode) && handIndexNode != null)
        {
            action.HandIndex = handIndexNode.GetValue<int>();
        }
        if (parameters.TryGetPropertyValue("cardId", out var cardIdNode) && cardIdNode != null)
        {
            action.CardId = cardIdNode.GetValue<string>();
        }
        if (parameters.TryGetPropertyValue("rewardType", out var rewardTypeNode) && rewardTypeNode != null)
        {
            action.RewardType = rewardTypeNode.GetValue<string>();
        }
        if (parameters.TryGetPropertyValue("optionIndex", out var optionIndexNode) && optionIndexNode != null)
        {
            action.OptionIndex = optionIndexNode.GetValue<int>();
        }
        if (parameters.TryGetPropertyValue("targetEnemyIndices", out var targetEnemyIndicesNode)
            && targetEnemyIndicesNode is System.Text.Json.Nodes.JsonArray targets
            && targets.Count > 0
            && targets[0] != null)
        {
            action.TargetEnemyIndex = targets[0]!.GetValue<int>();
        }

        return action;
    }
}
