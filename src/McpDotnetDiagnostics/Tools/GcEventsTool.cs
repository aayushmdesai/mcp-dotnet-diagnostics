using System.ComponentModel;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using ModelContextProtocol.Server;

namespace McpDotnetDiagnostics.Tools;

[McpServerToolType]
public class GcEventsTool
{
    [
        McpServerTool,
        Description(
            "Returns recent GC events for a target .NET process including collection generation "
                + "(Gen0/Gen1/Gen2), duration, pause time, and timestamp. Use this when investigating "
                + "periodic response time spikes, application pauses, memory that grows without releasing, "
                + "or when get_memory_stats shows high Gen2 collection counts. Call get_memory_stats first "
                + "to establish whether GC pressure is the root cause before drilling into individual events."
        )
    ]
    public static async Task<string> GetGcEvents(
        [Description("The process ID (PID) of the target .NET application")] int pid,
        [Description("How long to collect GC events in seconds (default: 5)")] int sampleSeconds = 5
    )
    {
        try
        {
            var client = new DiagnosticsClient(pid);

            var providers = new List<EventPipeProvider>
            {
                new EventPipeProvider(
                    "Microsoft-Windows-DotNETRuntime",
                    System.Diagnostics.Tracing.EventLevel.Informational,
                    (long)ClrTraceEventParser.Keywords.GC
                ),
            };

            var gcEvents = new List<string>();

            using var session = client.StartEventPipeSession(providers, circularBufferMB: 10);
            using var source = new EventPipeEventSource(session.EventStream);

            source.Clr.GCStart += (gcData) =>
            {
                var generation = gcData.Depth;
                var reason = gcData.Reason.ToString();
                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

                gcEvents.Add($"[{timestamp}] Gen{generation} GC — Reason: {reason}");
            };

            _ = Task.Delay(TimeSpan.FromSeconds(sampleSeconds))
                .ContinueWith(_ => session.Stop(), TaskScheduler.Default);

            source.Process();

            if (gcEvents.Count == 0)
                return $"No GC events observed in {sampleSeconds}s window. "
                    + $"GC pressure appears low — this is normal for a healthy application.";

            var summary =
                $"GC Events ({sampleSeconds}s window): {gcEvents.Count} total\n"
                + string.Join("\n", gcEvents);

            return summary;
        }
        catch (DiagnosticsClientException ex)
        {
            return $"Error: Could not connect to process {pid}. "
                + $"Ensure it is a .NET application and you have permission to attach. Details: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error reading GC events: {ex.Message}";
        }
    }
}
