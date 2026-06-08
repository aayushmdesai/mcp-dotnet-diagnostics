# mcp-dotnet-diagnostics

Give your AI assistant eyes into your .NET application's runtime health.

`mcp-dotnet-diagnostics` is a [Model Context Protocol](https://modelcontextprotocol.io) server
that exposes live .NET runtime diagnostics as tools. Connect it to Claude Desktop or any MCP
client and ask your AI assistant to diagnose memory leaks, GC pressure, thread starvation,
and more — against any running .NET process on your machine.

---

## What It Does

Without this server, your AI assistant can only guess:

> *"You might want to check your memory usage..."*

With it, Claude reads real runtime data and gives grounded diagnoses:

> *"Every GC event in the last 5 seconds was a Gen2 collection triggered by `AllocLarge`.
> Something is continuously allocating objects above the 85KB LOH threshold at ~10.5 MB/s.
> LOH is never compacted by default — fragmentation is at 55% and growing. The fix is
> `ArrayPool<byte>.Shared`. Rent a buffer, use it, return it. This eliminates the LOH
> pressure entirely."*

Claude decides which tools to call and in what order. You just ask the question.

---

## Tools

| Tool | Description |
|------|-------------|
| `get_process_info` | Process name, PID, uptime, .NET version, OS. First step in any investigation. |
| `get_memory_stats` | GC heap size, LOH size, allocation rate, Gen0/1/2 counts, fragmentation, GC pressure level. |
| `get_gc_events` | Per-event GC timeline — generation, reason, and timestamp for each collection. |
| `get_thread_stats` | ThreadPool thread count, queue length, completed items, lock contention. Diagnoses thread starvation. |
| `get_event_counters` | All 27 `System.Runtime` metrics in one call — CPU, memory, GC, JIT, exceptions, timers. |
| `get_environment_info` | Process metadata and filtered environment variables (safe keys only — no secrets). |
| `list_counters` | Discover all available EventCounter names and values from a process. |

---

## Requirements

- .NET 8 SDK or later
- Claude Desktop (or any MCP client)
- A running .NET application to diagnose

> **macOS note:** The .NET diagnostics protocol uses a Unix socket under `$TMPDIR`.
> You must pass `TMPDIR` explicitly in your MCP config (see installation step 3 below).

---

## Installation

**Step 1 — Clone and build**

```bash
git clone https://github.com/aayushmdesai/mcp-dotnet-diagnostics.git
cd mcp-dotnet-diagnostics
dotnet publish src/McpDotnetDiagnostics -c Release -o ./publish
```

**Step 2 — Find the binary path**

```bash
echo $(pwd)/publish/McpDotnetDiagnostics
```

Copy this path — you'll need it in the next step.

**Step 3 — Add to Claude Desktop config**

Open `~/Library/Application Support/Claude/claude_desktop_config.json` while Claude
Desktop is **fully quit**, then add:

```json
{
  "mcpServers": {
    "dotnet-diagnostics": {
      "command": "/your/path/to/publish/McpDotnetDiagnostics",
      "env": {
        "TMPDIR": "/var/folders/xx/your-tmpdir/T/"
      }
    }
  }
}
```

To find your `TMPDIR`:

```bash
echo $TMPDIR
```

**Step 4 — Reopen Claude Desktop**

The `dotnet-diagnostics` connector will appear in the tools menu.

---

## Usage

Find the PID of your running .NET application:

```bash
dotnet-counters ps
```

Then ask Claude:

```
"Can you do a full health check on PID 12345?"
"Why does my API have high memory usage? PID is 12345."
"Are there any thread starvation issues in PID 12345?"
```

Claude will call the appropriate tools, combine the results, and give you a diagnosis.

---

## How It Works

```
You ask Claude a question
        │
        ▼
Claude reads tool descriptions and decides which to call
        │
        ▼
MCP server connects to your .NET app via DiagnosticsClient
(same protocol as dotnet-counters and dotnet-trace)
        │
        ▼
Runtime telemetry flows back over EventPipe
        │
        ▼
Claude synthesizes data into a diagnosis
```

The server uses `Microsoft.Diagnostics.NETCore.Client` to connect to any running .NET
process by PID. This is the same library that powers `dotnet-counters`, `dotnet-trace`,
and `dotnet-dump` — direct access to the CLR's internal telemetry stream.

---

## Running Tests

```bash
dotnet test src/McpDotnetDiagnostics.Tests
```

34 tests covering all 7 tools — unit tests for error paths, integration tests against
the live test runner process.

---

## Architecture Decisions

- [ADR-001: C# over TypeScript](docs/adr-001-csharp-over-typescript.md)
- [ADR-002: Target process by PID, not self](docs/adr-002-target-process-diagnostics.md)
- [ADR-003: .NET 10 EventPipe payload extraction](docs/adr-003-dotnet10-payload-extraction.md)

---

## Built With

- [ModelContextProtocol](https://www.nuget.org/packages/ModelContextProtocol) — official .NET MCP SDK
- [Microsoft.Diagnostics.NETCore.Client](https://www.nuget.org/packages/Microsoft.Diagnostics.NETCore.Client) — .NET runtime diagnostics
- [Microsoft.Diagnostics.Tracing.TraceEvent](https://www.nuget.org/packages/Microsoft.Diagnostics.Tracing.TraceEvent) — EventPipe event parsing

---

## License

MIT