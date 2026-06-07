using McpDotnetDiagnostics.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();

builder
    .Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<ProcessInfoTool>()
    .WithTools<MemoryStatsTool>()
    .WithTools<GcEventsTool>()
    .WithTools<ListCountersTool>()
    .WithTools<ThreadStatsTool>()
    .WithTools<EventCountersTool>();

await builder.Build().RunAsync();
