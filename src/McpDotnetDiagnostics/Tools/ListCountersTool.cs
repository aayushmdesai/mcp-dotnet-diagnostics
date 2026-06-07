using System.ComponentModel;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using ModelContextProtocol.Server;

namespace McpDotnetDiagnostics.Tools;

[McpServerToolType]
public class ListCountersTool
{
    [
        McpServerTool,
        Description(
            "Lists all available EventCounter names and current values from a .NET process. "
                + "Use this to discover what metrics are available before calling get_memory_stats."
        )
    ]
    public static async Task<string> ListCounters(
        [Description("The process ID (PID) of the target .NET application")] int pid,
        [Description("Sample duration in seconds (default: 3)")] int sampleSeconds = 3
    )
    {
        try
        {
            var client = new DiagnosticsClient(pid);
            var providers = new List<EventPipeProvider>
            {
                new EventPipeProvider(
                    "System.Runtime",
                    System.Diagnostics.Tracing.EventLevel.Informational,
                    (long)ClrTraceEventParser.Keywords.None,
                    new Dictionary<string, string> { ["EventCounterIntervalSec"] = "1" }
                ),
            };

            var counters = new Dictionary<string, double>();

            using var session = client.StartEventPipeSession(providers, circularBufferMB: 10);
            using var source = new EventPipeEventSource(session.EventStream);

            source.Dynamic.All += (traceEvent) =>
            {
                if (!traceEvent.EventName.Contains("EventCounters"))
                    return;

                try
                {
                    var payload = traceEvent.PayloadValue(0);
                    if (payload is not IDictionary<string, object> dict)
                        return;

                    foreach (var kvp in dict)
                    {
                        // If the value is itself a struct/dict, go one level deeper
                        if (kvp.Value is IDictionary<string, object> nested)
                        {
                            foreach (var nkvp in nested)
                                counters[$"{kvp.Key}/{nkvp.Key}={nkvp.Value}"] = 1;
                        }
                        else
                        {
                            counters[$"{kvp.Key}={kvp.Value}"] = 1;
                        }
                    }
                }
                catch (Exception ex)
                {
                    counters[$"error={ex.Message}"] = 1;
                }
            };
            _ = Task.Delay(TimeSpan.FromSeconds(sampleSeconds))
                .ContinueWith(_ => session.Stop(), TaskScheduler.Default);

            source.Process();

            if (!counters.Any())
                return "No counters received. The process may not expose EventCounters.";

            var lines = counters.OrderBy(k => k.Key).Select(k => $"{k.Key} = {k.Value}");

            return $"Available counters ({counters.Count}):\n" + string.Join("\n", lines);
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}
