using FluentAssertions;
using McpDotnetDiagnostics.Tools;

namespace McpDotnetDiagnostics.Tests.Tools;

public class MemoryStatsToolTests
{
    // --- Error path (unit) ---

    [Fact]
    public async Task GetMemoryStats_InvalidPid_ReturnsErrorMessage()
    {
        var result = await MemoryStatsTool.GetMemoryStats(TestHelpers.InvalidPid);

        result.Should().StartWith("Error:");
        result.Should().Contain(TestHelpers.InvalidPid.ToString());
    }

    [Fact]
    public async Task GetMemoryStats_InvalidPid_DoesNotThrow()
    {
        var act = async () => await MemoryStatsTool.GetMemoryStats(TestHelpers.InvalidPid);

        await act.Should().NotThrowAsync();
    }

    // --- Happy path (integration) ---

    [Fact]
    public async Task GetMemoryStats_ValidPid_ReturnsAllocRate()
    {
        var result = await MemoryStatsTool.GetMemoryStats(TestHelpers.CurrentPid, sampleSeconds: 3);

        result.Should().Contain("Alloc Rate");
        result.Should().NotStartWith("Error:");
    }

    [Fact]
    public async Task GetMemoryStats_ValidPid_ReturnsGenCounts()
    {
        var result = await MemoryStatsTool.GetMemoryStats(TestHelpers.CurrentPid, sampleSeconds: 3);

        result.Should().Contain("Gen0 GC Count");
        result.Should().Contain("Gen1 GC Count");
        result.Should().Contain("Gen2 GC Count");
    }

    [Fact]
    public async Task GetMemoryStats_ValidPid_ReturnsGcPressure()
    {
        var result = await MemoryStatsTool.GetMemoryStats(TestHelpers.CurrentPid, sampleSeconds: 3);

        result.Should().Contain("GC Pressure");
    }

    [Fact]
    public async Task GetMemoryStats_ValidPid_ReturnsSampleWindow()
    {
        var result = await MemoryStatsTool.GetMemoryStats(TestHelpers.CurrentPid, sampleSeconds: 3);

        result.Should().Contain("Sample Window  : 3s");
    }
}
