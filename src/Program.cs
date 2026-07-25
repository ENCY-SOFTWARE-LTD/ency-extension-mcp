using EncyExtensionMcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// `ency-extension-mcp login` — interactive one-time store login (no MCP involved).
if (args.Length > 0 && args[0].Equals("login", StringComparison.OrdinalIgnoreCase))
    return await new StoreTokenProvider().LoginInteractive();

// `ency-extension-mcp claim <PackageId> <owner/repo>` — bind a repo so its CI publishes without a secret.
if (args.Length > 0 && args[0].Equals("claim", StringComparison.OrdinalIgnoreCase))
{
    var tokenProvider = new StoreTokenProvider();
    return await ClaimCommand.Run(args, new StoreClient(), tokenProvider.GetAccessToken, Console.WriteLine);
}

// `ency-extension-mcp setup [--no-login]` — register in the editor's MCP config and log in.
if (args.Length > 0 && args[0].Equals("setup", StringComparison.OrdinalIgnoreCase))
{
    var tokenProvider = new StoreTokenProvider();
    return await SetupCommand.Run(SetupCommand.DefaultCursorConfigPath, new ProcessRunner(),
        () => File.Exists(StoreTokenProvider.AuthFilePath), tokenProvider.LoginInteractive,
        args.Contains("--no-login", StringComparer.OrdinalIgnoreCase), Console.WriteLine);
}

var builder = Host.CreateApplicationBuilder(args);

// stdout carries the MCP protocol — all logging must go to stderr.
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();
builder.Services.AddSingleton<IStoreClient, StoreClient>();
builder.Services.AddSingleton<StoreTokenProvider>();
builder.Services.AddSingleton<ExtensionStoreTools>();
builder.Services.AddSingleton<GuideTools>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<ExtensionStoreTools>()
    .WithTools<GuideTools>();

await builder.Build().RunAsync();
return 0;
