using System.ComponentModel;
using System.Diagnostics;
using ModelContextProtocol.Server;

namespace McpDotnetDiagnostics.Tools;

[McpServerToolType]
public class ProcessInfoTool
{
    [
        McpServerTool,
        Description(
            "Returns basic information about a running .NET process by PID. "
                + "Includes process name, uptime, .NET runtime version, OS platform, "
                + "and CPU core count. Use this as the first step when investigating "
                + "any .NET application — it confirms the process is reachable and "
                + "provides baseline context before diving into memory or thread diagnostics."
        )
    ]
    public static string GetProcessInfo(
        [Description("The process ID (PID) of the target .NET application")] int pid
    )
    {
        try
        {
            var process = Process.GetProcessById(pid);
            var uptime = DateTime.Now - process.StartTime;

            return $"""
                Process Name : {process.ProcessName}
                PID          : {process.Id}
                Uptime       : {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s
                .NET Version : {Environment.Version}
                OS           : {Environment.OSVersion}
                CPU Cores    : {Environment.ProcessorCount}
                Working Set  : {process.WorkingSet64 / 1024 / 1024} MB
                """;
        }
        catch (ArgumentException)
        {
            return $"Error: No process found with PID {pid}. "
                + $"Ensure the application is running and the PID is correct.";
        }
        catch (Exception ex)
        {
            return $"Error reading process info: {ex.Message}";
        }
    }
}
