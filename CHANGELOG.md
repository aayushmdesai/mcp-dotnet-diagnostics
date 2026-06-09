# Changelog

All notable changes to this project will be documented here.

---

## [0.2.0] — 2026-06-09

### Added
- CI pipeline — 34/34 tests on every push to `main` and PRs (`.github/workflows/ci.yml`)
- Automated NuGet publish on `v*` tag push via GitHub Actions + API key secret
- NuGet global tool packaging — `dotnet tool install -g mcp-dotnet-diagnostics`
- Demo GIF in README showing autonomous health check and LOH fragmentation diagnosis
- Issue templates — bug report, feature request, new tool proposal
- CONTRIBUTING.md — how to add a new tool, build from source, tool description guidelines

### Changed
- README rewritten for developer clarity and human tone
- Installation section now leads with `dotnet tool install -g` instead of clone+build
- Tools table adds "Reach for it when..." column
- TMPDIR warning promoted to standalone blockquote — impossible to miss

### Fixed
- `.csproj` repository URLs corrected (`aayushmdesai` → `aayushmdesai14`)

---

## [0.1.0] — 2026-06-08

### Added
- 7 MCP tools: `get_process_info`, `get_memory_stats`, `get_gc_events`, `get_thread_stats`,
  `get_event_counters`, `get_environment_info`, `list_counters`
- Claude Desktop integration via stdio transport
- 34 tests — unit (invalid PID) + integration (xUnit runner self-targeting)
- ADR-001: C# over TypeScript
- ADR-002: Target process by PID, not self
- ADR-003: .NET 10 EventPipe payload extraction
- testapp — GC-active target for manual testing and demo recording