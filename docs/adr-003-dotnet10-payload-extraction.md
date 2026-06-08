# ADR-003: .NET 10 EventPipe Payload Extraction

**Date:** June 2026  
**Status:** Accepted

## Context

After implementing `get_memory_stats` using the standard EventPipe approach, all counter
values returned zero despite the tool connecting successfully to the target process.
`dotnet-counters` could read the same process correctly, confirming the diagnostic socket
was accessible.

## Investigation

Added a debug tool (`list_counters`) that dumped raw event payloads. This revealed:

1. The event name filter `traceEvent.EventName != "EventCounters"` was too strict —
   .NET 10 emits the event as `System.Runtime/EventCounters`, not just `EventCounters`.
   Fixed with `Contains("EventCounters")`.

2. Even after fixing the filter, values were still zero. Further payload inspection showed:

PayloadType = Microsoft.Diagnostics.Tracing.Parsers.DynamicTraceEventData+StructValue
dict/Payload = StructValue

The payload structure in .NET 10 wraps counter data one level deeper than earlier versions:

PayloadValue(0) → IDictionary { "Name" → "gc-heap-size", "Mean" → 19.0 }

**Earlier .NET versions:**

**.NET 10:**

## Decision

Extract the inner `"Payload"` key first, falling back to the outer dictionary for
compatibility with older runtimes:

```csharp
var outer = traceEvent.PayloadValue(0) as IDictionary<string, object>;
var payload = outer.TryGetValue("Payload", out var p)
    ? p as IDictionary<string, object>
    : outer;
```

## Consequences

- The fix is backward compatible — older .NET versions don't have the `"Payload"` key,
  so the fallback to `outer` handles them correctly.
- This behavior is undocumented in `Microsoft.Diagnostics.Tracing.TraceEvent` — discovered
  through runtime inspection, not official docs.
- All EventPipe-based tools (`get_memory_stats`, `get_thread_stats`, `get_event_counters`,
  `list_counters`) apply this same extraction pattern.

