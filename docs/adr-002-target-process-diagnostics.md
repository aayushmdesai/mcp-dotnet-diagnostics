# ADR-002: Diagnose Target Process by PID, Not the MCP Server Itself

**Date:** June 2026  
**Status:** Accepted

## Context

When designing the diagnostic tools, a choice was needed: should the MCP server
report diagnostics about itself, or connect to a separate target .NET process by PID?

## Options Considered

**Option A — Self-diagnostics:** The MCP server reads its own memory, GC, and thread stats
using in-process APIs (`GC.GetTotalMemory()`, `ThreadPool.GetAvailableThreads()`, etc.).

**Option B — Target process by PID:** The MCP server accepts a PID parameter and connects
to a separate running .NET application using `Microsoft.Diagnostics.NETCore.Client`.

## Decision

Option B — target process by PID.

## Reasons

- **The MCP server itself is uninteresting.** It's a lightweight background process doing
  almost nothing. Its memory is ~50MB, GC is clean, threads are idle. Diagnosing it
  would always return "everything looks fine."

- **The value is in the developer's app.** The developer's API, worker service, or web app
  is the process that's slow, leaking memory, or stalling threads. That's where useful
  data lives.

- **`DiagnosticsClient` is the right tool.** `Microsoft.Diagnostics.NETCore.Client` connects
  to any running .NET process via a local Unix socket/named pipe — the same protocol used
  by `dotnet-counters` and `dotnet-trace`. It gives direct access to the CLR's internal
  telemetry stream, not just OS-level metrics.

## Consequences

- Both the MCP server and the target process must run on the same machine (the diagnostics
  protocol uses local sockets, not network connections).
- Users must provide a PID. A future v2 could add process discovery by name.
- On macOS, `TMPDIR` must be explicitly set in the MCP server's environment so
  `DiagnosticsClient` finds the correct socket path (see ADR-003).