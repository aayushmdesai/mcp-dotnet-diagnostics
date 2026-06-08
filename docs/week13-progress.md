# Week 13 Progress — MCP Server: mcp-dotnet-diagnostics

**Month 4, Week 13 | Dates: June 2026**  
**Repo: mcp-dotnet-diagnostics**  
**Tag: v0.1.0**

---

## Goals

- Scaffold `mcp-dotnet-diagnostics` — new standalone repo, C#, .NET 10 ✅
- Implement 7 MCP tools covering process, memory, GC, threads, and environment diagnostics ✅
- Wire server into Claude Desktop and verify end-to-end tool calls ✅
- Fix .NET 10 EventPipe payload extraction issue ✅
- Write 34 unit + integration tests, all passing ✅
- Write ADRs 001, 002, 003 ✅

---

## The Shift: Consumer → Provider

ChefAgent was an AI application — it consumed LLMs to help users cook. The MCP server
is AI infrastructure — it extends what LLMs can do, for other developers to use.

The positioning shift: a developer installs `mcp-dotnet-diagnostics`, connects it to
Claude Desktop or Cursor, and their AI assistant gains the ability to inspect a live
.NET process. Without the server, Claude can only reason from general knowledge.
With it, Claude calls real tools and gives grounded, data-driven diagnoses.

This is the difference between application developer and platform engineer — which is
exactly the positioning target for AI Orchestrator / AI Agent Infrastructure roles.

---

## Architecture Decisions

### C# over TypeScript (ADR-001)

The MCP ecosystem leans TypeScript. Decision: C# using the official `ModelContextProtocol`
NuGet package (v1.4.0).

Reasons: the core library (`Microsoft.Diagnostics.NETCore.Client`) is .NET-native.
A TypeScript wrapper would require shelling out to CLI tools rather than using the SDK
directly. C# also differentiates the project — most MCP servers are TypeScript.

### Target Process by PID, Not Self (ADR-002)

The MCP server connects to a separate .NET application by PID using `DiagnosticsClient`,
rather than reporting its own metrics.

Reason: the MCP server itself is uninteresting — 50MB memory, idle GC, no load.
The developer's API or service is where the real data lives. `DiagnosticsClient` uses
the same local Unix socket protocol as `dotnet-counters` and `dotnet-trace` — direct
access to the CLR's internal telemetry stream, not OS-level estimates.

### .NET 10 EventPipe Payload Extraction (ADR-003)

The most significant debugging challenge of the week. All EventCounter values returned
zero despite successful connection. Investigation via a debug `list_counters` tool revealed
two issues:

1. `.NET 10` emits `System.Runtime/EventCounters`, not just `EventCounters` — the filter
   `EventName != "EventCounters"` was rejecting all events.
2. The payload structure changed in .NET 10 — counter data is wrapped one level deeper
   under a `"Payload"` key of type `StructValue`:

```
Earlier .NET: PayloadValue(0) → { "Name" → "gc-heap-size", "Mean" → 19.0 }
.NET 10:      PayloadValue(0) → { "Payload" → { "Name" → "gc-heap-size", "Mean" → 19.0 } }
```

Fix — extract inner `"Payload"` key first, fall back to outer dict for compatibility:

```csharp
var outer = traceEvent.PayloadValue(0) as IDictionary<string, object>;
var payload = outer.TryGetValue("Payload", out var p)
    ? p as IDictionary<string, object>
    : outer;
```

This behavior is undocumented. Discovered through runtime inspection.

---

## Tool Descriptions Are the Contract

The most important insight from building MCP tools: **the tool description is the contract
between you and the LLM.** The LLM reads descriptions to decide when and whether to call
each tool. A vague description means the wrong tool gets called — or no tool at all.

Pattern used across all tools:

- **What it returns** — concrete field names, not vague summaries
- **When to use it** — specific symptoms that would trigger the call
- **What to call first** — chains tools together so Claude investigates in the right order

Example (bad): `"Gets memory information"`

Example (good): `"Returns current .NET memory usage including total allocated bytes, GC
generation collection counts (Gen0/Gen1/Gen2), Large Object Heap size, and memory pressure
level. Use this when investigating memory leaks, unexpectedly high memory usage, or frequent
GC pauses. Call get_process_info first to confirm the process is reachable."`

The chaining hint — "Call get_process_info first" — teaches Claude the correct investigation
sequence without hardcoding it.

---

## Day 1 — Scaffold + ProcessInfoTool ✅

### What Was Done

- New GitHub repo: `mcp-dotnet-diagnostics`
- Solution with two projects: `McpDotnetDiagnostics` (server) and `McpDotnetDiagnostics.Tests`
- NuGet packages: `ModelContextProtocol` 1.4.0, `Microsoft.Diagnostics.NETCore.Client`,
  `Microsoft.Diagnostics.Tracing.TraceEvent`, `Microsoft.Extensions.Hosting`
- `Program.cs` — MCP server entry point using `WithStdioServerTransport()`
- `ProcessInfoTool.cs` — first tool using `System.Diagnostics.Process.GetProcessById(pid)`
- Claude Desktop integration verified — `dotnet-diagnostics` connector visible with blue toggle

### API Discovery

`ModelContextProtocol` v1.4.0 changed the transport method name from `WithStdioTransport()`
(documented in most examples) to `WithStdioServerTransport()`. Required version inspection
to discover.

### Debugging Notes

- Claude Desktop overwrites `claude_desktop_config.json` on restart — must edit the file
  while the app is fully quit (Cmd+Q), then reopen. Writing via terminal `cat >` while
  the app is closed is the reliable approach.
- Logging `ClearProviders()` in `Program.cs` required to stop SDK logs from polluting
  stdout and interfering with the JSON stdio protocol.

### First Tool Call

```
User: "Can you call get_process_info with PID 59228?"

Claude: Process: TestApp | .NET Version: 10.0.8 | Uptime: 1m 23s |
        OS: Unix 14.8.7 | CPU Cores: 4 | Working Set: 16 MB
```

---

## Day 2 — MemoryStatsTool + GcEventsTool ✅

### What Was Done

- `MemoryStatsTool.cs` — EventPipe session subscribing to `System.Runtime` EventSource,
  extracts `gc-heap-size`, `loh-size`, `alloc-rate`, `gen-0/1/2-gc-count`, `gc-fragmentation`
- `GcEventsTool.cs` — subscribes to `Microsoft-Windows-DotNETRuntime` with GC keywords,
  listens to `source.Clr.GCStart` events, returns generation, reason, and timestamp per event
- `ListCountersTool.cs` — debug tool that dumps all raw EventPipe payloads; used to diagnose
  the .NET 10 payload extraction issue (see ADR-003)

### Debugging: The TMPDIR Issue

`DiagnosticsClient` on macOS looks for the .NET diagnostic socket under `/tmp/` by default.
On macOS, the actual socket lives under `$TMPDIR` (e.g. `/var/folders/61/.../T/`).

Fix: add `TMPDIR` to the MCP server's environment config in `claude_desktop_config.json`:

```json
"env": {
  "TMPDIR": "/var/folders/61/8md_0fg94j7_7dgpdqj_fcfw0000gn/T/"
}
```

This is a known macOS + .NET diagnostics combination issue. Noted in ADR-002 consequences.

### testapp

Created a persistent test target at `testapp/` in the repo (gitignored):

```csharp
// Allocates ~1MB every 100ms to generate real GC activity
var timer = new System.Timers.Timer(100);
timer.Elapsed += (s, e) => { var data = new byte[1024 * 1024]; };
timer.Start();
await Task.Delay(Timeout.Infinite);
```

This generates LOH pressure, Gen0/1/2 collections, and meaningful alloc rates —
making diagnostics tools return interesting real data rather than all-zeros.

---

## Day 3 — ThreadStatsTool + EventCountersTool ✅

### What Was Done

- `ThreadStatsTool.cs` — reads `threadpool-thread-count`, `threadpool-queue-length`,
  `threadpool-completed-items-count`, `monitor-lock-contention-count`, `exception-count`
  from `System.Runtime` EventCounters. Returns `HEALTHY / ELEVATED / STARVED` diagnosis.
- `EventCountersTool.cs` — general-purpose tool that returns all 27 available `System.Runtime`
  metrics in a single call. Intended as a broad health overview before targeted investigation.

### Real Diagnosis Example

```
ThreadPool Threads  : 3
Queue Length        : 0
Completed Items/s   : 13
Lock Contention     : 0
Exception Count     : 0
Active Timers       : 2
ThreadPool Status   : HEALTHY
```

---

## Day 4 — EnvironmentInfoTool + Full Health Check Demo ✅

### What Was Done

- `EnvironmentInfoTool.cs` — reads process metadata via `System.Diagnostics.Process` and
  environment variables via `DiagnosticsClient.GetProcessEnvironment()`. Filters to safe
  keys only (`DOTNET_*`, `ASPNETCORE_*`, `PATH`, `TMPDIR`, etc.) — never exposes secrets.

### Full Health Check Demo

Asked Claude Desktop: *"I have a .NET app running. Can you do a full health check on
PID 30251 and tell me if there are any concerns?"*

Claude called tools autonomously in sequence — no instructions given on which tools to use
or in what order:

1. `get_event_counters` — broad overview
2. `get_memory_stats` — memory deep dive
3. `get_gc_events` — GC event timeline
4. `get_environment_info` — runtime config check

**Claude's synthesis (unprompted):**

> "Every GC event — 16 total — was a Gen2 collection triggered by AllocLarge. Something
> in your code is continuously allocating objects above the 85KB LOH threshold at ~10.5 MB/s.
> LOH is never compacted by default, so fragmentation grows. The cascade: LOH allocation →
> Gen2 GC (Stop-the-World) → all managed threads pause → cycle repeats. The fix is
> `ArrayPool<byte>.Shared` — rent a buffer, use it, return it."

This is the value of well-written tool descriptions. Claude chains tools, reads the data,
and gives a diagnosis a senior .NET engineer would recognize as correct.

---

## Day 5 — Tests ✅

### Approach

Two test layers per tool:

- **Unit tests** — pass `InvalidPid = 999999`, assert error message is clean and structured,
  assert no exception thrown
- **Integration tests** — pass `Environment.ProcessId` (the xUnit runner's own PID), assert
  response contains expected fields and does not start with "Error:"

The xUnit test runner is itself a live .NET process with a diagnostic socket — no separate
process needed for integration tests.

### Results

```
Test summary: total: 34, failed: 0, succeeded: 34, skipped: 0, duration: 20.9s
```

34 tests covering all 7 tools. Integration tests take ~20s because each EventPipe tool
samples for 3 seconds.

---

## Files Created

```
src/McpDotnetDiagnostics/Program.cs                          — MCP server entry point
src/McpDotnetDiagnostics/Tools/ProcessInfoTool.cs            — get_process_info
src/McpDotnetDiagnostics/Tools/MemoryStatsTool.cs            — get_memory_stats
src/McpDotnetDiagnostics/Tools/GcEventsTool.cs               — get_gc_events
src/McpDotnetDiagnostics/Tools/ThreadStatsTool.cs            — get_thread_stats
src/McpDotnetDiagnostics/Tools/EventCountersTool.cs          — get_event_counters
src/McpDotnetDiagnostics/Tools/EnvironmentInfoTool.cs        — get_environment_info
src/McpDotnetDiagnostics/Tools/ListCountersTool.cs           — list_counters (debug)
src/McpDotnetDiagnostics.Tests/TestHelpers.cs                — CurrentPid, InvalidPid
src/McpDotnetDiagnostics.Tests/Tools/ProcessInfoToolTests.cs — 6 tests
src/McpDotnetDiagnostics.Tests/Tools/MemoryStatsToolTests.cs — 6 tests
src/McpDotnetDiagnostics.Tests/Tools/GcEventsToolTests.cs    — 4 tests
src/McpDotnetDiagnostics.Tests/Tools/ThreadStatsToolTests.cs — 5 tests
src/McpDotnetDiagnostics.Tests/Tools/EventCountersToolTests.cs — 5 tests
src/McpDotnetDiagnostics.Tests/Tools/EnvironmentInfoToolTests.cs — 5 tests
src/McpDotnetDiagnostics.Tests/Tools/ListCountersToolTests.cs — 3 tests
docs/adr-001-csharp-over-typescript.md                       — NEW
docs/adr-002-target-process-diagnostics.md                   — NEW
docs/adr-003-dotnet10-payload-extraction.md                  — NEW
testapp/Program.cs                                           — GC-active test target
testapp/testapp.csproj                                       — gitignored
week13-progress.md                                           — this file
```

---

## Tool Summary

| Tool | Description | Key Counters |
|------|-------------|--------------|
| `get_process_info` | Basic process metadata | Name, PID, uptime, .NET version, OS |
| `get_memory_stats` | GC heap and allocation | gc-heap-size, loh-size, alloc-rate, gen counts, fragmentation |
| `get_gc_events` | Per-event GC timeline | Generation, reason, timestamp |
| `get_thread_stats` | ThreadPool health | Thread count, queue length, contention, diagnosis |
| `get_event_counters` | All 27 runtime metrics | Full System.Runtime EventCounters snapshot |
| `get_environment_info` | Runtime environment | Process metadata, filtered env vars |
| `list_counters` | Raw counter discovery | All available EventPipe counter names and values |

---

## Key Learnings

**Tool descriptions are infrastructure.** The LLM reads them to decide when to call each
tool. Vague descriptions = wrong tool called. Good descriptions include what the tool
returns, what symptoms trigger it, and what to call first.

**The xUnit runner is a valid integration test target.** `Environment.ProcessId` points
to a live .NET process with a diagnostic socket — no separate app needed.

**Claude Desktop overwrites its config.** Edit `claude_desktop_config.json` only while
the app is fully quit. Use terminal `cat >` for reliability.

**`TMPDIR` must be passed explicitly on macOS.** `DiagnosticsClient` looks in `/tmp/`
by default; macOS puts sockets in `$TMPDIR`. Pass `TMPDIR` in the MCP server's `env`
config in `claude_desktop_config.json`.

**`.NET 10` changed EventPipe payload structure.** Counter data is wrapped one level
deeper under a `"Payload"` key of type `StructValue`. Undocumented. Discovered through
runtime inspection using `list_counters`.

**Building infrastructure is fundamentally different from building applications.**
With ChefAgent, the consumer of tools was a human. With MCP, the consumer is an LLM.
Structured output, clear error messages, and precise descriptions matter more than UI —
the LLM can't ask clarifying questions.

---

## Deferred

- CI pipeline (`.github/workflows/ci.yml`) — Week 14
- README with install instructions and demo GIF — Week 14
- Open-source release (GitHub topics, NuGet publish consideration) — Week 14
- Remote process diagnostics (cross-machine via TCP) — v2.0
- Process discovery by name (remove need to find PID manually) — v2.0
- `get_slow_queries` EF Core diagnostics — v2.0