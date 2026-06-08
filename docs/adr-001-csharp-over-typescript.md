# ADR-001: C# over TypeScript for MCP Server Implementation

**Date:** June 2026  
**Status:** Accepted

## Context

The MCP ecosystem's reference SDK and most published servers are written in TypeScript.
When scaffolding `mcp-dotnet-diagnostics`, a choice was needed between the TypeScript SDK
and the official `ModelContextProtocol` NuGet package for .NET.

## Decision

Build in C# using the `ModelContextProtocol` NuGet package (v1.4.0).

## Reasons

- **Domain fit:** The server attaches to live .NET processes via `Microsoft.Diagnostics.NETCore.Client`.
  This library is .NET-native. A TypeScript wrapper would require shelling out to CLI tools
  (`dotnet-counters`, `dotnet-trace`) rather than using the SDK directly — an inferior approach.

- **Differentiation:** The overwhelming majority of MCP servers are TypeScript. A C# implementation
  is immediately distinctive in a portfolio and demonstrates depth in the .NET ecosystem.

- **Existing strength:** Background is in .NET and Azure. Using C# means the implementation
  can go deeper — understanding EventPipe, EventCounters, and the CLR diagnostics protocol —
  rather than fighting an unfamiliar language while also learning MCP.

## Consequences

- The server targets .NET developers specifically, which is appropriate given the tool's purpose.
- Contributors need .NET SDK installed, unlike TypeScript servers which only need Node.
- The `ModelContextProtocol` NuGet package is newer and less documented than the TypeScript SDK —
  some API discovery was required (e.g. `WithStdioServerTransport()` naming in v1.4.0).