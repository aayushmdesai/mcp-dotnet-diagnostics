using System.ComponentModel;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using ModelContextProtocol.Server;

namespace McpDotnetDiagnostics.Tools;

[McpServerToolType]
public class EventCountersTool
{
    [
        McpServerTool,
        Description(
            "Returns all available EventCounter metrics from a .NET process including CPU usage, "
                + "memory, GC, threading, exceptions, and JIT stats in a single call. Use this for a "
                + "broad health overview when you don't know which specific area to investigate. "
                + "For deeper analysis, follow up with get_memory_stats or get_thread_stats."
        )
    ]
    public static async Task<string> GetEventCounters(
        [Description("The process ID (PID) of the target .NET application")] int pid,
        [Description("How long to sample in seconds (default: 3)")] int sampleSeconds = 3
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

                var outer = traceEvent.PayloadValue(0) as IDictionary<string, object>;
                if (outer == null)
                    return;

                var payload = outer.TryGetValue("Payload", out var p)
                    ? p as IDictionary<string, object>
                    : outer;
                if (payload == null)
                    return;

                var name = payload.TryGetValue("Name", out var n) ? n?.ToString() : null;
                var mean =
                    payload.TryGetValue("Mean", out var m) ? Convert.ToDouble(m)
                    : payload.TryGetValue("Increment", out var i) ? Convert.ToDouble(i)
                    : 0;

                if (name != null)
                    counters[name] = mean;
            };

            _ = Task.Delay(TimeSpan.FromSeconds(sampleSeconds))
                .ContinueWith(_ => session.Stop(), TaskScheduler.Default);

            source.Process();

            if (!counters.Any())
                return "No counters received. Try increasing sampleSeconds.";

            var lines = counters.OrderBy(k => k.Key).Select(k => $"{k.Key, -35} : {k.Value:F2}");

            return $"Event Counters ({sampleSeconds}s sample, {counters.Count} metrics):\n"
                + string.Join("\n", lines);
        }
        catch (DiagnosticsClientException ex)
        {
            return $"Error: Could not connect to process {pid}. Details: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error reading event counters: {ex.Message}";
        }
    }
}
