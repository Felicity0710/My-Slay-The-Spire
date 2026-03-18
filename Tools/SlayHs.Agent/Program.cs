using SlayHs.Agent;

var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, args) =>
{
    args.Cancel = true;
    cancellation.Cancel();
};

var mode = args.Length > 0 ? args[0].Trim().ToLowerInvariant() : "help";
var client = new GameBridgeClient();

switch (mode)
{
    case "mcp":
        await new McpServer(client).RunAsync(cancellation.Token);
        return;
    case "bot":
        Environment.ExitCode = await new SimpleBot(client).RunAsync(cancellation.Token);
        return;
    default:
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  dotnet run --project Tools/SlayHs.Agent -- mcp");
        Console.Error.WriteLine("  dotnet run --project Tools/SlayHs.Agent -- bot");
        return;
}
