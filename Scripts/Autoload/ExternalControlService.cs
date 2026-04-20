using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

public partial class ExternalControlService : Node
{
    private const int Port = 47077;

    private readonly ConcurrentQueue<PendingRequest> _pendingRequests = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private CancellationTokenSource? _cancellation;
    private TcpListener? _listener;
    private Task? _listenerTask;
    private bool _isProcessingRequest;
    private string _lastSnapshotFingerprint = string.Empty;
    private long _stateVersion;

    public override void _Ready()
    {
        _cancellation = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Loopback, Port);
        _listener.Start();
        _listenerTask = Task.Run(() => AcceptLoopAsync(_cancellation.Token));
        GD.Print($"[ExternalControl] Listening on 127.0.0.1:{Port}");
    }

    public override void _Process(double delta)
    {
        if (_isProcessingRequest || !_pendingRequests.TryDequeue(out var pending))
        {
            return;
        }

        _isProcessingRequest = true;
        ProcessRequestAsync(pending);
    }

    public override void _ExitTree()
    {
        _cancellation?.Cancel();
        try
        {
            _listener?.Stop();
        }
        catch
        {
            // Ignore shutdown race with accept loop.
        }
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener != null)
        {
            TcpClient? client = null;
            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken);
                _ = HandleClientAsync(client, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                client?.Dispose();
                break;
            }
            catch (ObjectDisposedException)
            {
                client?.Dispose();
                break;
            }
            catch (Exception ex)
            {
                client?.Dispose();
                GD.PrintErr($"[ExternalControl] Accept failed: {ex.Message}");
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using var ownedClient = client;
        try
        {
            using var stream = ownedClient.GetStream();
            using var reader = new StreamReader(stream);
            using var writer = new StreamWriter(stream) { AutoFlush = true };

            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            ExternalCommandRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<ExternalCommandRequest>(line, _jsonOptions);
            }
            catch (Exception ex)
            {
                var invalid = new ExternalCommandResponse
                {
                    Ok = false,
                    Message = $"Invalid JSON: {ex.Message}"
                };
                await writer.WriteLineAsync(JsonSerializer.Serialize(invalid, _jsonOptions));
                return;
            }

            if (request == null)
            {
                var empty = new ExternalCommandResponse
                {
                    Ok = false,
                    Message = "Request body is empty."
                };
                await writer.WriteLineAsync(JsonSerializer.Serialize(empty, _jsonOptions));
                return;
            }

            var pending = new PendingRequest(request);
            _pendingRequests.Enqueue(pending);
            var response = await pending.Completion.Task.WaitAsync(cancellationToken);
            await writer.WriteLineAsync(JsonSerializer.Serialize(response, _jsonOptions));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[ExternalControl] Client handling failed: {ex.Message}");
        }
    }

    private async void ProcessRequestAsync(PendingRequest pending)
    {
        try
        {
            var response = await BuildResponseAsync(pending.Request);
            pending.Completion.TrySetResult(response);
        }
        catch (Exception ex)
        {
            pending.Completion.TrySetResult(new ExternalCommandResponse
            {
                Ok = false,
                Message = ex.Message,
                Snapshot = RefreshVersionedSnapshot()
            });
        }
        finally
        {
            _isProcessingRequest = false;
        }
    }

    private async Task<ExternalCommandResponse> BuildResponseAsync(ExternalCommandRequest request)
    {
        var command = request.Command?.Trim().ToLowerInvariant() ?? string.Empty;
        ApplyFastModeForExternalControl(request.FastMode);
        if (command == "ping")
        {
            var snapshot = await WaitForStableSnapshotAsync();
            return new ExternalCommandResponse
            {
                Ok = true,
                Message = "pong",
                StateVersion = snapshot.StateVersion,
                Snapshot = snapshot
            };
        }

        if (command == "get_snapshot")
        {
            var snapshot = await WaitForStableSnapshotAsync();
            return new ExternalCommandResponse
            {
                Ok = true,
                Message = "snapshot",
                StateVersion = snapshot.StateVersion,
                Snapshot = snapshot
            };
        }

        if (command != "execute_action")
        {
            var unsupported = RefreshVersionedSnapshot();
            return new ExternalCommandResponse
            {
                Ok = false,
                Message = $"Unsupported command '{request.Command}'.",
                StateVersion = unsupported.StateVersion,
                Snapshot = unsupported
            };
        }

        var beforeSnapshot = RefreshVersionedSnapshot();
        if (request.ExpectedStateVersion.HasValue && request.ExpectedStateVersion.Value != beforeSnapshot.StateVersion)
        {
            return new ExternalCommandResponse
            {
                Ok = false,
                Message = $"State version mismatch. Expected {request.ExpectedStateVersion.Value}, current {beforeSnapshot.StateVersion}.",
                StateVersion = beforeSnapshot.StateVersion,
                Snapshot = beforeSnapshot
            };
        }

        if (request.Action == null)
        {
            return new ExternalCommandResponse
            {
                Ok = false,
                Message = "Missing action payload.",
                StateVersion = beforeSnapshot.StateVersion,
                Snapshot = beforeSnapshot
            };
        }

        var error = await ExecuteActionAsync(request.Action);
        var afterSnapshot = string.IsNullOrEmpty(error)
            ? await WaitForPostActionSnapshotAsync(beforeSnapshot)
            : RefreshVersionedSnapshot();
        return new ExternalCommandResponse
        {
            Ok = string.IsNullOrEmpty(error),
            Message = string.IsNullOrEmpty(error) ? "ok" : error,
            StateVersion = afterSnapshot.StateVersion,
            Snapshot = afterSnapshot
        };
    }

    private void ApplyFastModeForExternalControl(bool? fastMode)
    {
        var state = GetNodeOrNull<GameState>("/root/GameState");
        if (state == null)
        {
            return;
        }

        state.SetExternalFastMode(fastMode ?? true);
    }

    private async Task<string?> ExecuteActionAsync(ExternalActionRequest action)
    {
        var kind = action.Kind?.Trim().ToLowerInvariant() ?? string.Empty;
        var tree = GetTree();
        var state = GetNode<GameState>("/root/GameState");
        var currentScene = ResolveActiveSceneNode();
        var uiPhase = state.CurrentUiPhase;

        switch (kind)
        {
            case "start_new_run":
                state.StartNewRun();
                tree.ChangeSceneToFile("res://Scenes/MapScene.tscn");
                return null;
            case "start_battle_test_run":
                state.StartBattleTestRun(action.PresetId, action.EncounterType, action.Floor, action.Seed, action.Randomized);
                tree.ChangeSceneToFile("res://Scenes/BattleScene.tscn");
                return null;
            case "choose_map_node":
                if (currentScene is not MapScene mapScene)
                {
                    return "choose_map_node is only available on the map scene.";
                }
                if (!action.Column.HasValue)
                {
                    return "choose_map_node requires 'column'.";
                }
                return mapScene.TryChooseNodeExternally(action.Column.Value);
            case "play_card":
                if (currentScene is not BattleScene battleScene)
                {
                    return "play_card is only available during battle.";
                }
                if (!action.HandIndex.HasValue && string.IsNullOrWhiteSpace(action.CardId))
                {
                    return "play_card requires 'handIndex' or 'cardId'.";
                }
                return await battleScene.TryPlayCardExternallyAsync(action.HandIndex, action.CardId, action.TargetEnemyIndex);
            case "end_turn":
                if (currentScene is not BattleScene endTurnBattle)
                {
                    return "end_turn is only available during battle.";
                }
                return await endTurnBattle.TryEndTurnExternallyAsync();
            case "choose_reward_type":
                if (currentScene is RewardScene rewardTypeScene)
                {
                    return rewardTypeScene.TryChooseRewardTypeExternally(action.RewardType);
                }
                if (uiPhase == "reward")
                {
                    return TryExecuteRewardTypeDirectly(state, tree, action.RewardType);
                }
                return "choose_reward_type is only available on the reward scene.";
            case "choose_reward_card":
                if (currentScene is RewardScene rewardCardScene)
                {
                    return rewardCardScene.TryChooseRewardCardExternally(action.OptionIndex, action.CardId);
                }
                if (uiPhase == "reward")
                {
                    return TryExecuteRewardCardDirectly(state, tree, action.OptionIndex, action.CardId);
                }
                return "choose_reward_card is only available on the reward scene.";
            case "skip_reward":
                if (currentScene is RewardScene skipRewardScene)
                {
                    return skipRewardScene.TrySkipRewardExternally();
                }
                if (uiPhase == "reward")
                {
                    return ExitRewardToMap(state, tree, clearCardPack: true);
                }
                return "skip_reward is only available on the reward scene.";
            case "choose_event_option":
                if (currentScene is EventScene eventScene)
                {
                    return eventScene.TryChooseEventOptionExternally(action.OptionIndex, action.EventOption);
                }
                if (uiPhase == "event")
                {
                    return TryExecuteEventOptionDirectly(state, tree, action.OptionIndex, action.EventOption);
                }
                return "choose_event_option is only available on the event scene.";
            default:
                return $"Unsupported action kind '{action.Kind}'.";
        }
    }

    private ExternalSnapshot RefreshVersionedSnapshot()
    {
        var snapshot = BuildSnapshotCore();
        var fingerprint = JsonSerializer.Serialize(snapshot, _jsonOptions);
        if (!string.Equals(fingerprint, _lastSnapshotFingerprint, StringComparison.Ordinal))
        {
            _stateVersion += 1;
            _lastSnapshotFingerprint = fingerprint;
        }

        snapshot.StateVersion = _stateVersion;
        return snapshot;
    }

    private async Task<ExternalSnapshot> WaitForStableSnapshotAsync()
    {
        ExternalSnapshot snapshot = RefreshVersionedSnapshot();
        for (var i = 0; i < 8; i++)
        {
            if (!string.Equals(snapshot.SceneType, "unknown", StringComparison.OrdinalIgnoreCase))
            {
                return snapshot;
            }

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            snapshot = RefreshVersionedSnapshot();
        }

        return snapshot;
    }

    private async Task<ExternalSnapshot> WaitForPostActionSnapshotAsync(ExternalSnapshot beforeSnapshot)
    {
        var beforeFingerprint = JsonSerializer.Serialize(beforeSnapshot, _jsonOptions);
        var snapshot = RefreshVersionedSnapshot();
        for (var i = 0; i < 16; i++)
        {
            var fingerprint = JsonSerializer.Serialize(snapshot, _jsonOptions);
            if (!string.Equals(snapshot.SceneType, "unknown", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(fingerprint, beforeFingerprint, StringComparison.Ordinal))
            {
                return snapshot;
            }

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            snapshot = RefreshVersionedSnapshot();
        }

        return snapshot;
    }

    private ExternalSnapshot BuildSnapshotCore()
    {
        var state = GetNode<GameState>("/root/GameState");
        var scene = ResolveActiveSceneNode();
        var snapshot = new ExternalSnapshot
        {
            SceneType = ResolveSceneType(scene, state.CurrentUiPhase),
            Run = BuildRunSnapshot(state)
        };

        switch (snapshot.SceneType)
        {
            case "battle" when scene is BattleScene battleScene:
                snapshot.Battle = battleScene.BuildBattleSnapshot();
                snapshot.LegalActions = battleScene.BuildLegalActions();
                break;
            case "map":
                snapshot.Map = BuildMapSnapshot(state);
                snapshot.LegalActions = BuildMapLegalActions(state);
                break;
            case "reward":
                snapshot.Reward = scene is RewardScene rewardScene
                    ? rewardScene.BuildRewardSnapshot()
                    : BuildRewardSnapshotFromState(state);
                snapshot.LegalActions = BuildRewardLegalActionsFromState(state, snapshot.Reward);
                break;
            case "event":
                snapshot.Event = scene is EventScene eventScene
                    ? eventScene.BuildEventSnapshot()
                    : BuildEventSnapshotFromState(state);
                snapshot.LegalActions = BuildEventLegalActionsFromState(snapshot.Event);
                break;
            default:
                snapshot.LegalActions = BuildGlobalLegalActions();
                break;
        }

        return snapshot;
    }

    private Node? ResolveActiveSceneNode()
    {
        var tree = GetTree();
        if (tree.CurrentScene != null)
        {
            return tree.CurrentScene;
        }

        return FindActiveSceneNode(tree.Root);
    }

    private Node? FindActiveSceneNode(Node node)
    {
        for (var i = node.GetChildCount() - 1; i >= 0; i--)
        {
            var child = node.GetChild(i);
            if (child == this)
            {
                continue;
            }

            var nested = FindActiveSceneNode(child);
            if (nested != null)
            {
                return nested;
            }

            if (!string.IsNullOrWhiteSpace(child.SceneFilePath))
            {
                return child;
            }
        }

        return null;
    }

    private static string ResolveSceneType(Node? scene, string uiPhase)
    {
        if (!string.IsNullOrWhiteSpace(uiPhase) && !string.Equals(uiPhase, "unknown", StringComparison.OrdinalIgnoreCase))
        {
            return uiPhase;
        }

        if (scene == null)
        {
            return "unknown";
        }

        var typeName = scene.GetType().Name;
        var nodeName = scene.Name.ToString();
        var sceneFile = scene.SceneFilePath ?? string.Empty;

        return scene switch
        {
            BattleScene => "battle",
            MapScene => "map",
            RewardScene => "reward",
            EventScene => "event",
            MainMenu => "main_menu",
            _ when ContainsSceneMarker(typeName, nodeName, sceneFile, "Battle") => "battle",
            _ when ContainsSceneMarker(typeName, nodeName, sceneFile, "Map") => "map",
            _ when ContainsSceneMarker(typeName, nodeName, sceneFile, "Reward") => "reward",
            _ when ContainsSceneMarker(typeName, nodeName, sceneFile, "Event") => "event",
            _ when ContainsSceneMarker(typeName, nodeName, sceneFile, "MainMenu") => "main_menu",
            _ => nodeName.ToLowerInvariant()
        };
    }

    private static bool ContainsSceneMarker(string typeName, string nodeName, string sceneFile, string marker)
    {
        return typeName.Contains(marker, StringComparison.OrdinalIgnoreCase)
            || nodeName.Contains(marker, StringComparison.OrdinalIgnoreCase)
            || sceneFile.Contains(marker, StringComparison.OrdinalIgnoreCase);
    }

    private static RunSnapshot BuildRunSnapshot(GameState state)
    {
        return new RunSnapshot
        {
            Floor = state.Floor,
            PlayerHp = state.PlayerHp,
            MaxHp = state.MaxHp,
            BattlesWon = state.BattlesWon,
            PotionCharges = state.PotionCharges,
            SelectedDeckPresetId = state.SelectedDeckPresetId,
            HasCustomDeckOverride = state.HasCustomDeckOverride,
            DeckCardIds = new List<string>(state.DeckCardIds),
            RelicIds = new List<string>(state.RelicIds),
            PotionIds = new List<string>(state.PotionIds),
            PendingEncounterType = state.PendingEncounterType.ToString(),
            PendingEventId = state.PendingEventId
        };
    }

    private static MapSnapshot BuildMapSnapshot(GameState state)
    {
        var map = new MapSnapshot
        {
            CurrentRow = state.CurrentMapRow,
            CurrentColumn = state.CurrentMapColumn
        };

        for (var row = 0; row < state.MapLayout.Count; row++)
        {
            var rowSnapshot = new MapRowSnapshot { RowIndex = row };
            for (var column = 0; column < state.MapLayout[row].Count; column++)
            {
                var node = state.GetMapNodeType(row, column);
                var nextColumns = row < state.MapConnections.Count
                    ? new List<int>(state.MapConnections[row][column])
                    : new List<int>();
                rowSnapshot.Nodes.Add(new MapNodeSnapshot
                {
                    Row = row,
                    Column = column,
                    NodeType = node.ToString(),
                    Label = state.MapNodeLabel(node),
                    CanSelect = state.CanChooseMapNode(row, column),
                    NextColumns = nextColumns
                });
            }

            map.Rows.Add(rowSnapshot);
        }

        return map;
    }

    private static List<LegalActionSnapshot> BuildGlobalLegalActions()
    {
        return new List<LegalActionSnapshot>
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
    }

    private static List<LegalActionSnapshot> BuildMapLegalActions(GameState state)
    {
        var actions = BuildGlobalLegalActions();
        for (var column = 0; column < state.MapLayout[state.CurrentMapRow].Count; column++)
        {
            if (!state.CanChooseMapNode(state.CurrentMapRow, column))
            {
                continue;
            }

            actions.Add(new LegalActionSnapshot
            {
                Kind = "choose_map_node",
                Label = $"Choose row {state.CurrentMapRow}, column {column}",
                Parameters = new Dictionary<string, object?>
                {
                    ["column"] = column
                }
            });
        }

        return actions;
    }

    private static RewardSnapshot BuildRewardSnapshotFromState(GameState state)
    {
        var snapshot = new RewardSnapshot
        {
            Mode = state.PendingRewardOptions.Count > 0 ? "card_pack" : "reward_type",
            RewardTypes = new List<string> { "relic", "card_pack", "potion", "random", "skip" }
        };

        for (var i = 0; i < state.PendingRewardOptions.Count; i++)
        {
            var card = CardData.CreateById(state.PendingRewardOptions[i]);
            snapshot.CardOptions.Add(new RewardOptionSnapshot
            {
                OptionIndex = i,
                Id = card.Id,
                Name = card.Name,
                Description = card.GetLocalizedDescription()
            });
        }

        for (var i = 0; i < state.PendingRelicOptions.Count; i++)
        {
            var relic = RelicData.CreateById(state.PendingRelicOptions[i]);
            snapshot.RelicOptions.Add(new RewardOptionSnapshot
            {
                OptionIndex = i,
                Id = relic.Id,
                Name = relic.Name,
                Description = relic.Description
            });
        }

        return snapshot;
    }

    private static List<LegalActionSnapshot> BuildRewardLegalActionsFromState(GameState state, RewardSnapshot? reward)
    {
        reward ??= BuildRewardSnapshotFromState(state);
        var actions = new List<LegalActionSnapshot>();
        if (string.Equals(reward.Mode, "card_pack", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var option in reward.CardOptions)
            {
                actions.Add(new LegalActionSnapshot
                {
                    Kind = "choose_reward_card",
                    Label = $"Choose reward card option {option.OptionIndex}",
                    Parameters = new Dictionary<string, object?>
                    {
                        ["optionIndex"] = option.OptionIndex,
                        ["cardId"] = option.Id
                    }
                });
            }

            actions.Add(new LegalActionSnapshot
            {
                Kind = "skip_reward",
                Label = "Skip the card reward"
            });
            return actions;
        }

        var rewardTypes = reward.RewardTypes.Count > 0
            ? reward.RewardTypes
            : new List<string> { "relic", "card_pack", "potion", "random", "skip" };

        foreach (var rewardType in rewardTypes)
        {
            if (string.Equals(rewardType, "skip", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            actions.Add(new LegalActionSnapshot
            {
                Kind = "choose_reward_type",
                Label = $"Choose reward type {rewardType}",
                Parameters = new Dictionary<string, object?>
                {
                    ["rewardType"] = rewardType
                }
            });
        }

        actions.Add(new LegalActionSnapshot
        {
            Kind = "skip_reward",
            Label = string.Equals(reward.Mode, "potion_replace", StringComparison.OrdinalIgnoreCase)
                ? "Skip replacing potion"
                : "Skip this reward scene"
        });
        return actions;
    }

    private static EventSnapshot BuildEventSnapshotFromState(GameState state)
    {
        var snapshot = new EventSnapshot
        {
            EventId = state.PendingEventId
        };

        if (state.PendingEventId == "shrine")
        {
            snapshot.Title = "Ancient Shrine";
            snapshot.Description = "A quiet shrine hums with energy.";
            snapshot.Options.Add(new EventOptionSnapshot { OptionIndex = 0, Label = "Pray: +5 Max HP and heal 5" });
            snapshot.Options.Add(new EventOptionSnapshot { OptionIndex = 1, Label = "Take Relic: Lose 8 HP, gain random relic" });
            return snapshot;
        }

        snapshot.Title = "Shady Dealer";
        snapshot.Description = "A dealer offers a risky bargain.";
        snapshot.Options.Add(new EventOptionSnapshot { OptionIndex = 0, Label = "Buy Card: Lose 6 HP, add Quick Slash" });
        snapshot.Options.Add(new EventOptionSnapshot { OptionIndex = 1, Label = "Refuse: Gain nothing" });
        return snapshot;
    }

    private static List<LegalActionSnapshot> BuildEventLegalActionsFromState(EventSnapshot? snapshot)
    {
        var actions = new List<LegalActionSnapshot>();
        if (snapshot == null)
        {
            return actions;
        }

        foreach (var option in snapshot.Options)
        {
            actions.Add(new LegalActionSnapshot
            {
                Kind = "choose_event_option",
                Label = option.Label,
                Parameters = new Dictionary<string, object?>
                {
                    ["optionIndex"] = option.OptionIndex
                }
            });
        }

        return actions;
    }

    private string? TryExecuteRewardTypeDirectly(GameState state, SceneTree tree, string? rewardType)
    {
        var normalized = rewardType?.Trim().ToLowerInvariant() ?? string.Empty;
        switch (normalized)
        {
            case "relic":
                state.RollRelicOptions(3);
                if (state.PendingRelicOptions.Count == 0)
                {
                    state.TryAddRandomPotion(out _);
                    state.PendingRelicOptions.Clear();
                    return ExitRewardToMap(state, tree, clearCardPack: true);
                }

                var relicId = state.PendingRelicOptions[0];
                state.AddRelic(relicId);
                state.PendingRelicOptions.Clear();
                return ExitRewardToMap(state, tree, clearCardPack: true);
            case "card_pack":
                state.RollRewardOptions(3);
                if (state.PendingRewardOptions.Count == 0)
                {
                    return ExitRewardToMap(state, tree, clearCardPack: true);
                }

                return null;
            case "potion":
                state.TryAddRandomPotion(out _);
                return ExitRewardToMap(state, tree, clearCardPack: true);
            case "random":
            {
                var choices = new List<string> { "relic", "card_pack", "potion" };
                if (state.RelicIds.Count >= RelicData.AllRelicIds().Count)
                {
                    choices.Remove("relic");
                }

                if (choices.Count == 0)
                {
                    choices.Add("potion");
                }

                var pick = choices[new Random().Next(choices.Count)];
                return TryExecuteRewardTypeDirectly(state, tree, pick);
            }
            case "skip":
                return ExitRewardToMap(state, tree, clearCardPack: true);
            default:
                return $"Unsupported reward type '{rewardType}'.";
        }
    }

    private string? TryExecuteRewardCardDirectly(GameState state, SceneTree tree, int? optionIndex, string? cardId)
    {
        var resolvedIndex = -1;
        if (optionIndex.HasValue && optionIndex.Value >= 0 && optionIndex.Value < state.PendingRewardOptions.Count)
        {
            resolvedIndex = optionIndex.Value;
        }
        else if (!string.IsNullOrWhiteSpace(cardId))
        {
            resolvedIndex = state.PendingRewardOptions.FindIndex(id =>
                string.Equals(id, cardId, StringComparison.OrdinalIgnoreCase));
        }

        if (resolvedIndex < 0 || resolvedIndex >= state.PendingRewardOptions.Count)
        {
            return "Requested reward card is not available.";
        }

        state.AddCardToDeck(state.PendingRewardOptions[resolvedIndex]);
        return ExitRewardToMap(state, tree, clearCardPack: true);
    }

    private string? ExitRewardToMap(GameState state, SceneTree tree, bool clearCardPack)
    {
        if (clearCardPack)
        {
            state.PendingRewardOptions.Clear();
        }

        state.SetUiPhase("map");
        tree.ChangeSceneToFile("res://Scenes/MapScene.tscn");
        return null;
    }

    private string? TryExecuteEventOptionDirectly(GameState state, SceneTree tree, int? optionIndex, string? eventOption)
    {
        var normalized = eventOption?.Trim().ToLowerInvariant() ?? string.Empty;
        if (state.PendingEventId == "shrine")
        {
            if (optionIndex == 0 || normalized == "pray")
            {
                state.GainMaxHp(5);
                state.ResolveEventFinished();
                state.SetUiPhase("map");
                tree.ChangeSceneToFile("res://Scenes/MapScene.tscn");
                return null;
            }

            if (optionIndex == 1 || normalized == "relic")
            {
                state.PlayerHp = Mathf.Max(1, state.PlayerHp - 8);
                state.RollRelicOptions(1);
                if (state.PendingRelicOptions.Count > 0)
                {
                    state.AddRelic(state.PendingRelicOptions[0]);
                    state.PendingRelicOptions.Clear();
                }

                state.ResolveEventFinished();
                state.SetUiPhase("map");
                tree.ChangeSceneToFile("res://Scenes/MapScene.tscn");
                return null;
            }

            return $"Unsupported shrine option '{eventOption ?? optionIndex?.ToString() ?? string.Empty}'.";
        }

        if (optionIndex == 0 || normalized == "buy")
        {
            state.PlayerHp = Mathf.Max(1, state.PlayerHp - 6);
            state.AddCardToDeck("quick_slash");
            state.ResolveEventFinished();
            state.SetUiPhase("map");
            tree.ChangeSceneToFile("res://Scenes/MapScene.tscn");
            return null;
        }

        if (optionIndex == 1 || normalized == "leave" || normalized == "refuse")
        {
            state.ResolveEventFinished();
            state.SetUiPhase("map");
            tree.ChangeSceneToFile("res://Scenes/MapScene.tscn");
            return null;
        }

        return $"Unsupported event option '{eventOption ?? optionIndex?.ToString() ?? string.Empty}'.";
    }

    private sealed class PendingRequest
    {
        public ExternalCommandRequest Request { get; }
        public TaskCompletionSource<ExternalCommandResponse> Completion { get; } = new();

        public PendingRequest(ExternalCommandRequest request)
        {
            Request = request;
        }
    }
}
