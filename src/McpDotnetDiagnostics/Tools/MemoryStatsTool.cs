using System.ComponentModel;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using ModelContextProtocol.Server;

namespace McpDotnetDiagnostics.Tools;

[McpServerToolType]
public class MemoryStatsTool
{
    [
        McpServerTool,
        Description(
            "Returns current .NET memory usage for a target process including total allocated bytes, "
                + "GC generation collection counts (Gen0/Gen1/Gen2), Large Object Heap size, and memory "
                + "pressure level. Use this when investigating memory leaks, unexpectedly high memory usage, "
                + "frequent GC pauses, or slow application performance caused by garbage collection pressure. "
                + "Call get_process_info first to confirm the process is reachable."
        )
    ]
    public static async Task<string> GetMemoryStats(
        [Description("The process ID (PID) of the target .NET application")] int pid,
        [Description("How long to sample counters in seconds (default: 2)")] int sampleSeconds = 2
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

                // .NET 10 wraps the counter data one level deeper under "Payload"
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
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(sampleSeconds + 1));
            _ = Task.Delay(TimeSpan.FromSeconds(sampleSeconds), cts.Token)
                .ContinueWith(_ => session.Stop(), TaskScheduler.Default);

            source.Process();

            var gcHeapSize =
                counters.TryGetValue("gc-heap-size", out var h) ? h
                : counters.TryGetValue("working-set", out var ws) ? ws
                : 0;
            var gen0 = counters.TryGetValue("gen-0-gc-count", out var g0) ? g0 : 0;
            var gen1 = counters.TryGetValue("gen-1-gc-count", out var g1) ? g1 : 0;
            var gen2 = counters.TryGetValue("gen-2-gc-count", out var g2) ? g2 : 0;
            var loh = counters.TryGetValue("loh-size", out var l) ? l : 0;
            var alloc = counters.TryGetValue("alloc-rate", out var a) ? a : 0;
            var fragmentation = counters.TryGetValue("gc-fragmentation", out var f) ? f : 0;

            var pressure =
                gen2 > 10 ? "HIGH — investigate memory leaks or large object allocation"
                : gen2 > 3 ? "MEDIUM — monitor closely"
                : "NORMAL";

            return $"""
                GC Heap Size   : {gcHeapSize / 1024 / 1024:F1} MB
                LOH Size       : {loh / 1024 / 1024:F1} MB
                Alloc Rate     : {alloc / 1024 / 1024:F1} MB/s
                GC Fragmentation: {fragmentation:F1}%
                Gen0 GC Count  : {gen0}
                Gen1 GC Count  : {gen1}
                Gen2 GC Count  : {gen2}
                GC Pressure    : {pressure}
                Sample Window  : {sampleSeconds}s
                """;
        }
        catch (DiagnosticsClientException ex)
        {
            return $"Error: Could not connect to process {pid}. "
                + $"Ensure it is a .NET application and you have permission to attach. Details: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error reading memory stats: {ex.Message}";
        }
    }
}
