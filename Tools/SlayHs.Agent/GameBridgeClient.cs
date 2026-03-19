using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace SlayHs.Agent;

internal sealed class GameBridgeClient
{
    private readonly string _host;
    private readonly int _port;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public GameBridgeClient(string host = "127.0.0.1", int port = 47077)
    {
        _host = host;
        _port = port;
    }

    public Task<CommandResult> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        return SendAsync(new CommandEnvelope
        {
            Command = "get_snapshot"
        }, cancellationToken);
    }

    public Task<CommandResult> ExecuteAsync(ActionEnvelope action, long? expectedStateVersion, CancellationToken cancellationToken)
    {
        return SendAsync(new CommandEnvelope
        {
            Command = "execute_action",
            ExpectedStateVersion = expectedStateVersion,
            Action = action
        }, cancellationToken);
    }

    private async Task<CommandResult> SendAsync(CommandEnvelope envelope, CancellationToken cancellationToken)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(_host, _port, cancellationToken);

        using var stream = client.GetStream();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

        var json = JsonSerializer.Serialize(envelope, _jsonOptions);
        await writer.WriteLineAsync(json);

        var responseLine = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(responseLine))
        {
            throw new InvalidOperationException("Game bridge returned an empty response.");
        }

        var response = JsonSerializer.Deserialize<CommandResult>(responseLine, _jsonOptions);
        if (response == null)
        {
            throw new InvalidOperationException("Game bridge returned an unreadable response.");
        }

        return response;
    }
}
