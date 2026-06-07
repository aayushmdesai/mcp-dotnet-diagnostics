using System.ComponentModel;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using ModelContextProtocol.Server;

namespace McpDotnetDiagnostics.Tools;

[McpServerToolType]
public class ThreadStatsTool
{
    [
        McpServerTool,
        Description(
            "Returns .NET ThreadPool statistics for a target process including worker thread count, "
                + "available threads, queue length, and completed work items. Use this when investigating "
                + "slow response times, request timeouts, or deadlocks — high queue length and low available "
                + "threads indicates thread starvation, which is a common cause of latency spikes in .NET APIs. "
                + "Call get_memory_stats first — GC pressure is a frequent cause of thread starvation."
        )
    ]
    public static async Task<string> GetThreadStats(
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

            var threadCount = counters.TryGetValue("threadpool-thread-count", out var tc) ? tc : 0;
            var queueLength = counters.TryGetValue("threadpool-queue-length", out var ql) ? ql : 0;
            var completed = counters.TryGetValue("threadpool-completed-items-count", out var ci)
                ? ci
                : 0;
            var contention = counters.TryGetValue("monitor-lock-contention-count", out var lc)
                ? lc
                : 0;
            var exceptions = counters.TryGetValue("exception-count", out var ec) ? ec : 0;
            var activeTimers = counters.TryGetValue("active-timer-count", out var at) ? at : 0;

            var diagnosis =
                queueLength > 100
                    ? "STARVED — high queue length indicates threads cannot keep up with work"
                : queueLength > 10 ? "ELEVATED — queue building up, monitor closely"
                : "HEALTHY";

            return $"""
                ThreadPool Threads  : {threadCount}
                Queue Length        : {queueLength}
                Completed Items/s   : {completed}
                Lock Contention     : {contention}
                Exception Count     : {exceptions}
                Active Timers       : {activeTimers}
                ThreadPool Status   : {diagnosis}
                Sample Window       : {sampleSeconds}s
                """;
        }
        catch (DiagnosticsClientException ex)
        {
            return $"Error: Could not connect to process {pid}. "
                + $"Ensure it is a .NET application and you have permission to attach. Details: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error reading thread stats: {ex.Message}";
        }
    }
}
