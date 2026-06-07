using System.ComponentModel;
using Microsoft.Diagnostics.NETCore.Client;
using ModelContextProtocol.Server;

namespace McpDotnetDiagnostics.Tools;

[McpServerToolType]
public class EnvironmentInfoTool
{
    [
        McpServerTool,
        Description(
            "Returns runtime environment information for a target .NET process including "
                + ".NET runtime version, OS details, processor count, memory, and process configuration. "
                + "Use this when investigating version mismatches, environment-specific bugs, or unexpected "
                + "runtime behavior. Call get_process_info first to confirm the process is reachable."
        )
    ]
    public static string GetEnvironmentInfo(
        [Description("The process ID (PID) of the target .NET application")] int pid
    )
    {
        try
        {
            var process = System.Diagnostics.Process.GetProcessById(pid);
            var uptime = DateTime.Now - process.StartTime;

            // Get environment variables from the target process via DiagnosticsClient
            var client = new DiagnosticsClient(pid);
            var envVars = client.GetProcessEnvironment();

            // Filter out sensitive values
            var safeKeys = new[]
            {
                "DOTNET_",
                "ASPNETCORE_",
                "PATH",
                "OS",
                "COMPUTERNAME",
                "USERNAME",
                "TMPDIR",
                "HOME",
                "TERM",
            };
            var filteredEnv = envVars
                .Where(k =>
                    safeKeys.Any(prefix =>
                        k.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                    )
                )
                .OrderBy(k => k.Key)
                .Select(k => $"  {k.Key} = {k.Value}")
                .ToList();

            var lines = new List<string>
            {
                $"Process Name     : {process.ProcessName}",
                $"PID              : {pid}",
                $"Uptime           : {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s",
                $"Working Set      : {process.WorkingSet64 / 1024 / 1024} MB",
                $"OS               : {Environment.OSVersion}",
                $"CPU Cores        : {Environment.ProcessorCount}",
                $"64-bit Process   : {Environment.Is64BitProcess}",
                $"",
                $"Relevant Environment Variables ({filteredEnv.Count}):",
            };

            lines.AddRange(filteredEnv.Any() ? filteredEnv : new[] { "  (none found)" });

            return string.Join("\n", lines);
        }
        catch (DiagnosticsClientException ex)
        {
            return $"Error: Could not connect to process {pid}. Details: {ex.Message}";
        }
        catch (ArgumentException)
        {
            return $"Error: No process found with PID {pid}.";
        }
        catch (Exception ex)
        {
            return $"Error reading environment info: {ex.Message}";
        }
    }
}
