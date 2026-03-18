using System.Text.Json;
using System.Text.Json.Nodes;

namespace SlayHs.Agent;

internal sealed class McpServer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly GameBridgeClient _client;

    public McpServer(GameBridgeClient client)
    {
        _client = client;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await Console.In.ReadLineAsync(cancellationToken);
            if (line == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            JsonObject? request;
            try
            {
                request = JsonNode.Parse(line)?.AsObject();
            }
            catch (Exception ex)
            {
                await WriteErrorAsync(null, -32700, $"Parse error: {ex.Message}");
                continue;
            }

            if (request == null)
            {
                await WriteErrorAsync(null, -32600, "Invalid JSON-RPC request.");
                continue;
            }

            var idNode = request["id"];
            var method = request["method"]?.GetValue<string>() ?? string.Empty;
            try
            {
                switch (method)
                {
                    case "initialize":
                        await WriteResultAsync(idNode, BuildInitializeResult());
                        break;
                    case "notifications/initialized":
                        break;
                    case "ping":
                        await WriteResultAsync(idNode, new JsonObject());
                        break;
                    case "tools/list":
                        await WriteResultAsync(idNode, BuildToolsList());
                        break;
                    case "tools/call":
                        await HandleToolCallAsync(idNode, request["params"]?.AsObject(), cancellationToken);
                        break;
                    default:
                        if (idNode != null)
                        {
                            await WriteErrorAsync(idNode, -32601, $"Method '{method}' not found.");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                await WriteErrorAsync(idNode, -32000, ex.Message);
            }
        }
    }

    private async Task HandleToolCallAsync(JsonNode? idNode, JsonObject? parameters, CancellationToken cancellationToken)
    {
        var name = parameters?["name"]?.GetValue<string>() ?? string.Empty;
        var arguments = parameters?["arguments"]?.AsObject() ?? new JsonObject();

        switch (name)
        {
            case "get_game_snapshot":
            {
                var result = await _client.GetSnapshotAsync(cancellationToken);
                await WriteResultAsync(idNode, ToToolResponse(result));
                return;
            }
            case "execute_game_action":
            {
                var action = new ActionEnvelope
                {
                    Kind = arguments["kind"]?.GetValue<string>() ?? string.Empty,
                    HandIndex = arguments["handIndex"]?.GetValue<int?>(),
                    CardId = arguments["cardId"]?.GetValue<string?>(),
                    TargetEnemyIndex = arguments["targetEnemyIndex"]?.GetValue<int?>(),
                    Column = arguments["column"]?.GetValue<int?>(),
                    RewardType = arguments["rewardType"]?.GetValue<string?>(),
                    OptionIndex = arguments["optionIndex"]?.GetValue<int?>(),
                    EventOption = arguments["eventOption"]?.GetValue<string?>(),
                    PresetId = arguments["presetId"]?.GetValue<string?>()
                };
                var expectedStateVersion = arguments["expectedStateVersion"]?.GetValue<long?>();
                var result = await _client.ExecuteAsync(action, expectedStateVersion, cancellationToken);
                await WriteResultAsync(idNode, ToToolResponse(result));
                return;
            }
            default:
                await WriteErrorAsync(idNode, -32602, $"Unknown tool '{name}'.");
                return;
        }
    }

    private static JsonObject BuildInitializeResult()
    {
        return new JsonObject
        {
            ["protocolVersion"] = "2024-11-05",
            ["capabilities"] = new JsonObject
            {
                ["tools"] = new JsonObject()
            },
            ["serverInfo"] = new JsonObject
            {
                ["name"] = "slay-the-hs-agent",
                ["version"] = "0.1.0"
            }
        };
    }

    private static JsonObject BuildToolsList()
    {
        return new JsonObject
        {
            ["tools"] = new JsonArray
            {
                new JsonObject
                {
                    ["name"] = "get_game_snapshot",
                    ["description"] = "Read the latest Slay the HS game state snapshot, including legal actions.",
                    ["inputSchema"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject(),
                        ["additionalProperties"] = false
                    }
                },
                new JsonObject
                {
                    ["name"] = "execute_game_action",
                    ["description"] = "Execute one legal game action against the running Slay the HS instance.",
                    ["inputSchema"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["kind"] = new JsonObject
                            {
                                ["type"] = "string",
                                ["description"] = "Action kind such as play_card, end_turn, choose_map_node, choose_reward_type, choose_reward_card, skip_reward, choose_event_option, start_new_run, or start_battle_test_run."
                            },
                            ["expectedStateVersion"] = new JsonObject
                            {
                                ["type"] = "integer",
                                ["description"] = "Optional optimistic concurrency guard."
                            },
                            ["handIndex"] = new JsonObject { ["type"] = "integer" },
                            ["cardId"] = new JsonObject { ["type"] = "string" },
                            ["targetEnemyIndex"] = new JsonObject { ["type"] = "integer" },
                            ["column"] = new JsonObject { ["type"] = "integer" },
                            ["rewardType"] = new JsonObject { ["type"] = "string" },
                            ["optionIndex"] = new JsonObject { ["type"] = "integer" },
                            ["eventOption"] = new JsonObject { ["type"] = "string" },
                            ["presetId"] = new JsonObject { ["type"] = "string" }
                        },
                        ["required"] = new JsonArray { "kind" },
                        ["additionalProperties"] = false
                    }
                }
            }
        };
    }

    private static JsonObject ToToolResponse(CommandResult result)
    {
        var text = result.Ok
            ? $"ok: {result.Message}"
            : $"error: {result.Message}";

        return new JsonObject
        {
            ["content"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "text",
                    ["text"] = text
                }
            },
            ["structuredContent"] = JsonSerializer.SerializeToNode(result, JsonOptions)
        };
    }

    private static async Task WriteResultAsync(JsonNode? idNode, JsonObject result)
    {
        var envelope = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = idNode?.DeepClone(),
            ["result"] = result
        };
        await Console.Out.WriteLineAsync(envelope.ToJsonString(JsonOptions));
        await Console.Out.FlushAsync();
    }

    private static async Task WriteErrorAsync(JsonNode? idNode, int code, string message)
    {
        var envelope = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = idNode?.DeepClone(),
            ["error"] = new JsonObject
            {
                ["code"] = code,
                ["message"] = message
            }
        };
        await Console.Out.WriteLineAsync(envelope.ToJsonString(JsonOptions));
        await Console.Out.FlushAsync();
    }
}
