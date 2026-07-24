using EncyExtensionMcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// stdout carries the MCP protocol — all logging must go to stderr.
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();
builder.Services.AddSingleton<IStoreClient, StoreClient>();
builder.Services.AddSingleton<ExtensionStoreTools>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<ExtensionStoreTools>();

await builder.Build().RunAsync();
