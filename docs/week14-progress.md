# Week 14 Progress — MCP Server: mcp-dotnet-diagnostics

**Month 4, Week 14 | Dates: June 2026**  
**Repo: mcp-dotnet-diagnostics**  
**Tag: v0.2.0 (end of week)**

---

## Goals

- CI pipeline running on every push ✅
- README polished for developer clarity and human tone ✅
- Demo GIF recorded and embedded in README ✅
- NuGet packaging + publish ✅
- Discoverability: GitHub topics, MCP registries ✅
- CONTRIBUTING.md, issue templates, CHANGELOG ✅

---

## The Theme: Working Code → Open-Source Project

Week 13 proved the tools work. Week 14 is about turning that into something a developer
who has never heard of this project can discover, install, and use — without asking any
questions.

The gap between "works on my machine" and "open-source project" is larger than it looks:
CI, packaging, documentation, discoverability, and developer experience are all part of
the product. A technically excellent tool with a bad README gets zero stars.

---

## Day 1 — CI Pipeline ✅

### What Was Built

`.github/workflows/ci.yml` — triggers on push to `main` and all PRs.

Steps: checkout → setup .NET 10 → restore (`McpDotnetDiagnostics.slnx`) → build
(Release) → test (`src/McpDotnetDiagnostics.Tests`).

### Key Decisions

**Self-targeting integration tests work on Linux CI without modification.** Integration
tests use `Environment.ProcessId` — the xUnit runner's own PID. On Ubuntu, `DiagnosticsClient`
finds the socket in `/tmp/` by default, which is exactly where Linux puts it. No `TMPDIR`
config needed. The macOS-specific `TMPDIR` issue does not exist on the CI runner.

**Explicit paths over auto-discovery.** The workflow passes `McpDotnetDiagnostics.slnx`
and the test project path explicitly rather than relying on `dotnet` to scan the directory.
More predictable, more readable.

**Node.js 24 opted in.** Added `FORCE_JAVASCRIPT_ACTIONS_TO_NODE24: true` at the job
level — same pattern as ChefAgent's CI. GitHub is forcing this cutoff on June 16th; better
to opt in now than get a surprise failure.

### Results

```
Test summary: total: 34, failed: 0, succeeded: 34, skipped: 0, duration: 17.0s
Build succeeded. 0 Warning(s). 0 Error(s).
```

34/34 on Linux. CI badge live on README.

### Files Created

```
.github/workflows/ci.yml    — CI pipeline
```

---

## Day 2 — README Polish ✅

### What Changed

The original README had good bones but read like a document rather than something a
person wrote. Rewrote for tone, scannability, and developer trust.

**Key changes:**

- **Opening hook** rewritten — leads with what the developer gets, not what the tool is
- **"What this looks like in practice"** — shows the actual LOH fragmentation diagnosis
  verbatim. The selling moment is Claude's output, not a feature list.
- **Tools table** — added a third column: "Reach for it when..." Tells developers which
  tool to call for which symptom, which is what they actually need to know.
- **TMPDIR warning** promoted to a standalone blockquote after the install steps —
  impossible to miss. This is the #1 setup failure point.
- **"How it works"** section rewritten — explains the tool description chaining insight
  (descriptions guide Claude's investigation sequence implicitly, not through hardcoded
  orchestration). That's genuinely interesting to a .NET developer.
- **ADRs reframed** as "Design decisions" with a one-line rationale each — signals
  engineering depth without being dry.
- ASCII flow diagram removed — prose reads better.

---

## Day 3 — Demo GIF ✅

### What Was Built

Recorded a full health check session in Claude Desktop using QuickTime, converted to
GIF via ezgif.com, embedded in the README.

**The demo shows:**
1. Prompt: *"I have a .NET app running. Can you do a full health check on PID 36226?"*
2. Claude autonomously searches available tools
3. Claude fires all diagnostics in parallel — no instructions on which tools to call
4. Dashboard renders: CPU 0.75%, Working set 45MB, LOH 21MB, Alloc rate 10 MB/s,
   GC fragmentation 52% — flagged red with "Needs attention"

The visual money shot is the "Needs attention" badge and 52% fragmentation in red.
That communicates the value proposition — grounded, data-driven diagnosis — in one frame.

**Technical:**
- Recorded: QuickTime screen capture (macOS built-in, no install needed)
- Converted: ezgif.com, 15 FPS, 640px width
- Final size: 2.6MB — within GitHub's rendering limit
- Location: `docs/assets/demo.gif`

### Files Created

```
docs/assets/demo.gif    — health check demo, embedded in README
```

---

## Day 4 — NuGet Publishing ✅

### What Was Built

`mcp-dotnet-diagnostics` published to NuGet.org as a .NET global tool. Install is now
one command:

```bash
dotnet tool install -g mcp-dotnet-diagnostics
```

### csproj Changes

Added NuGet packaging metadata to `McpDotnetDiagnostics.csproj`:

```xml
<PackAsTool>true</PackAsTool>
<ToolCommandName>mcp-dotnet-diagnostics</ToolCommandName>
<PackageId>mcp-dotnet-diagnostics</PackageId>
<Version>0.2.0</Version>
<Description>MCP server exposing .NET runtime diagnostics for AI assistants</Description>
<Authors>Aayush Desai</Authors>
<PackageProjectUrl>https://github.com/aayushmdesai14/mcp-dotnet-diagnostics</PackageProjectUrl>
<PackageLicenseExpression>MIT</PackageLicenseExpression>
<PackageTags>mcp;dotnet;diagnostics;ai;claude;model-context-protocol</PackageTags>
<PackageReadmeFile>README.md</PackageReadmeFile>
<RepositoryUrl>https://github.com/aayushmdesai14/mcp-dotnet-diagnostics</RepositoryUrl>
<RepositoryType>git</RepositoryType>
<Copyright>Copyright © 2026 Aayush Desai</Copyright>
```

README bundled into the package via `<None Include="..\..\README.md" Pack="true" PackagePath="\" />` —
renders on nuget.org package page.

### CI Publish Pipeline

Added a `publish` job to `ci.yml` — triggers only on `v*` tag pushes, runs after
`build-and-test` passes:

- Packs with `dotnet pack`
- Pushes with `dotnet nuget push` using `${{ secrets.NUGET_API_KEY }}`
- `--skip-duplicate` flag makes re-runs on existing versions idempotent

### NuGet Trusted Publishing

Attempted Trusted Publishing (OIDC token exchange, no stored secrets). Hit two blockers:
- The "Activate for 7 days" button on nuget.org was unresponsive for a pending private repo policy
- The API keys page redirected to Trusted Publishing

Workaround: used `?forceApiKeys=true` URL parameter to access the API keys page directly.
First publish done manually via `dotnet nuget push`; all subsequent publishes handled by CI.

### Key Decisions

**Global tool over library package.** `PackAsTool=true` makes this an executable installable
via `dotnet tool install -g`. Correct model for an MCP server — it's a process you run,
not a library you import.

**`--skip-duplicate` is mandatory.** Without it, re-running the pipeline on an existing
version exits with code 1 and fails the job. With it, already-published versions are
silently skipped.

**Version in `.csproj` must match git tag intent.** The git tag `v0.2.0` and `<Version>0.2.0</Version>`
must stay in sync — the tag triggers the pipeline, the csproj version determines what gets
pushed to NuGet.

### README Update

Installation section now leads with the one-liner instead of clone+build:
- `command` in `claude_desktop_config.json` simplified to just `mcp-dotnet-diagnostics`
  (global tool on PATH, no binary path needed)
- Clone+build moved to CONTRIBUTING.md (Day 6)

### Files Modified

```
McpDotnetDiagnostics.csproj         — NuGet packaging metadata
.github/workflows/ci.yml            — publish job added
README.md                           — install section updated
```

---

## Day 5 — Discoverability ✅

### What Was Done

**GitHub repo polish:**
- Description: "MCP server for .NET runtime diagnostics — memory, GC, threads, and more"
- Website: `https://www.nuget.org/packages/mcp-dotnet-diagnostics`
- Topics: `mcp`, `dotnet`, `diagnostics`, `ai`, `claude`, `model-context-protocol`, `csharp`, `llm-tools`
- Repo pinned on GitHub profile

**MCP registries:**
- Submitted to `punkpeye/awesome-mcp-servers` via PR — entry in Monitoring section
- Submitted to `mcpservers.org/submit`
- Submitted to Glama (`glama.ai/mcp/servers`) — pending review

**PR entry:**
```
- [aayushmdesai14/mcp-dotnet-diagnostics](https://github.com/aayushmdesai14/mcp-dotnet-diagnostics) 🏠 🍎 🐧 - Live .NET runtime diagnostics for AI assistants. Ask Claude to diagnose memory leaks, GC pressure, LOH fragmentation, and thread starvation in any running .NET process — no code changes required. Install: `dotnet tool install -g mcp-dotnet-diagnostics`
```

Description deliberately names .NET-specific terms (LOH fragmentation, thread starvation)
to signal expertise to the target audience — .NET developers who will recognize these as
real problems they've hit.

### Glama ✅

Submitted to Glama (`glama.ai/mcp/servers`). Required iterating on the Dockerfile
config to get the build passing — three failure modes hit and resolved:

1. **PATH not persisting across Docker RUN layers** — `export PATH` in one step doesn't
   carry to the next. Fixed by using the full path `/root/.dotnet/dotnet` directly.
2. **Missing `libicu` on debian:trixie-slim** — .NET requires ICU globalization libraries
   not included in the slim image. Fixed by adding `apt-get update && apt-get install -y libicu-dev`.
3. **Runtime not found at startup** — the published binary's apphost looks for .NET in
   `/usr/share/dotnet` but the install script puts it in `/root/.dotnet`. Fixed with
   `ln -s /root/.dotnet /usr/share/dotnet`.

Final build step:
```
apt-get update && apt-get install -y libicu-dev && curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 10.0 && ln -s /root/.dotnet /usr/share/dotnet && /root/.dotnet/dotnet publish src/McpDotnetDiagnostics -c Release -o ./publish
```

**Result:** Server started, all 7 tools registered, MCP introspection passed.
Glama release 1.0.0 created. Badge added to awesome-mcp-servers PR.

```
serverInfo: { name: 'McpDotnetDiagnostics', version: '0.2.0.0' }
tools: get_process_info, get_memory_stats, get_gc_events, get_thread_stats,
       get_event_counters, get_environment_info, list_counters
```

**Also added:**
- `LICENSE` file (MIT) — was missing, caused Glama license check to fail
- `glama.json` — profile metadata for Glama search visibility

### Pending
- awesome-mcp-servers PR — waiting for maintainer merge
- Anthropic Discord `#mcp` channel post — Week 15

---

## Days 6-7 — CONTRIBUTING, Issue Templates, CHANGELOG ✅

### What Was Built

**CONTRIBUTING.md** — how to add a new tool end-to-end:
- Tool class pattern with `[McpServerToolType]` attribute
- Test requirements (unit + integration, minimum 2 per tool)
- README table update requirement
- Build from source instructions
- Tool description guidelines — explains why good descriptions matter for autonomous chaining

**Issue templates** (`.github/ISSUE_TEMPLATE/`):
- `bug_report.md` — tool name, OS, .NET version, expected vs actual, macOS TMPDIR checklist
- `feature_request.md` — what diagnostic data, what problem, EventSource involved
- `new_tool.md` — tool name, counters, EventSource, who benefits

**CHANGELOG.md:**
- v0.2.0 — CI, NuGet packaging, README rewrite, issue templates, CONTRIBUTING
- v0.1.0 — 7 tools, Claude Desktop integration, 34 tests, ADRs

### Files Created

```
CONTRIBUTING.md                              — how to add a new tool
CHANGELOG.md                                 — v0.1.0 and v0.2.0 entries
.github/ISSUE_TEMPLATE/bug_report.md         — bug report template
.github/ISSUE_TEMPLATE/feature_request.md    — feature request template
.github/ISSUE_TEMPLATE/new_tool.md           — new tool proposal template
```

---

## Key Learnings

**Open-source is a product, not just code.** The README, install experience, demo GIF,
and discoverability are all part of what a developer evaluates. The tools were done in
Week 13. Week 14 is about everything that makes someone actually use them.

**The demo GIF is worth more than 1000 words of documentation.** The 52% fragmentation
badge in red, the "Needs attention" callout, Claude firing tools autonomously — that
communicates the value in one glance. No developer reads documentation before deciding
whether to star a repo.

**Self-targeting integration tests travel well.** The exact same test suite that runs
locally on macOS (targeting `Environment.ProcessId`) runs on Linux CI without
modification. The `TMPDIR` workaround is macOS-specific; Linux just works.

**Discoverability is part of the product.** GitHub topics, a one-line repo description,
and a PR to the right directory are all searchable signals. A developer searching for
"dotnet mcp" on GitHub or mcpservers.org needs to find the repo — the code alone doesn't
do that.

**Name the specific problems in your description.** "LOH fragmentation" and "thread
starvation" are .NET-specific terms. Using them signals expertise and filters for exactly
the audience who will find the tool useful. Generic descriptions ("monitoring tool") get
ignored.

**CONTRIBUTING.md is infrastructure for other developers.** The tool description
guidelines section is the most important part — it explains why descriptions matter for
autonomous tool chaining, which is non-obvious to a first-time contributor.

**NuGet Trusted Publishing has a chicken-and-egg problem.** The policy can't be activated
until the package exists, but you need to publish to create the package. First publish
always requires an API key; Trusted Publishing takes over after that. The `?forceApiKeys=true`
URL parameter bypasses the redirect when nuget.org tries to push you to Trusted Publishing.

**`--skip-duplicate` makes CI pipelines idempotent.** Without it, re-running a publish
job on an already-published version fails with a 409. One flag fixes it permanently.

**Glama Dockerfile for .NET has three non-obvious gotchas.** PATH doesn't persist
across Docker RUN layers (use full binary path), `debian:trixie-slim` is missing `libicu`
(.NET globalization dependency), and the published apphost looks for the runtime in
`/usr/share/dotnet` not `/root/.dotnet` (fix with a symlink). None of these are documented
anywhere — discovered through iteration.

**Node.js action deprecations are a recurring CI maintenance tax.** Same issue as
ChefAgent Week 8. Pattern: opt in to the new version early (`FORCE_JAVASCRIPT_ACTIONS_TO_NODE24`)
rather than waiting for a forced cutover. Takes one line.

---

## Deferred

- Remote process diagnostics (cross-machine via TCP) — v2.0
- Process discovery by name (remove need to find PID manually) — v2.0
- `get_slow_queries` EF Core diagnostics — v2.0
- macOS CI runner (would catch TMPDIR issues automatically) — v2.0