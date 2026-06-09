# Contributing to mcp-dotnet-diagnostics

Contributions are welcome — bug fixes, new tools, documentation improvements.

---

## Adding a New Tool

Tools live in `src/McpDotnetDiagnostics/Tools/`. Each tool is a self-contained class.

**1. Create the tool file**

```csharp
[McpServerToolType]
public static class MyNewTool
{
    [McpServerTool(Name = "my_tool_name")]
    [Description("What it returns. What symptoms trigger it. What to call first.")]
    public static async Task<string> MyTool(int pid)
    {
        // use DiagnosticsClient or System.Diagnostics.Process
    }
}
```

**2. Write tests**

Add a test file in `src/McpDotnetDiagnostics.Tests/Tools/`. Two tests minimum:
- Unit: pass `TestHelpers.InvalidPid`, assert clean error message, no exception thrown
- Integration: pass `TestHelpers.CurrentPid`, assert response contains expected fields

**3. Update the README tool table**

Add a row to the Tools table — name, what it returns, when to reach for it.

**4. Open a PR**

CI runs automatically. All 34+ tests must pass.

---

## Building from Source

```bash
git clone https://github.com/aayushmdesai14/mcp-dotnet-diagnostics.git
cd mcp-dotnet-diagnostics
dotnet build McpDotnetDiagnostics.slnx
dotnet test src/McpDotnetDiagnostics.Tests
```

---

## Tool Description Guidelines

The tool description is read by Claude to decide when to call each tool. Write it like
this — what it returns, what symptoms trigger it, what to call before it:

> "Returns GC heap size, LOH size, allocation rate, Gen0/1/2 collection counts,
> fragmentation percentage, and GC pressure level. Call when memory usage is high
> or growing. Call get_process_info first to confirm connectivity."

Vague descriptions produce wrong tool selection. Specific descriptions produce correct
autonomous chaining.

---

## Reporting Bugs

Open an issue with:
- Which tool failed
- Target process (OS, .NET version, PID or app type)
- Expected vs actual output
- macOS users: confirm `TMPDIR` is set in your MCP config