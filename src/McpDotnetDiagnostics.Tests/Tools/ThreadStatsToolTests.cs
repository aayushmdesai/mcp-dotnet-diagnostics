using FluentAssertions;
using McpDotnetDiagnostics.Tools;

namespace McpDotnetDiagnostics.Tests.Tools;

public class ThreadStatsToolTests
{
    // --- Error path (unit) ---

    [Fact]
    public async Task GetThreadStats_InvalidPid_ReturnsErrorMessage()
    {
        var result = await ThreadStatsTool.GetThreadStats(TestHelpers.InvalidPid);

        result.Should().StartWith("Error:");
        result.Should().Contain(TestHelpers.InvalidPid.ToString());
    }

    [Fact]
    public async Task GetThreadStats_InvalidPid_DoesNotThrow()
    {
        var act = async () => await ThreadStatsTool.GetThreadStats(TestHelpers.InvalidPid);

        await act.Should().NotThrowAsync();
    }

    // --- Happy path (integration) ---

    [Fact]
    public async Task GetThreadStats_ValidPid_ReturnsThreadCount()
    {
        var result = await ThreadStatsTool.GetThreadStats(TestHelpers.CurrentPid, sampleSeconds: 3);

        result.Should().Contain("ThreadPool Threads");
        result.Should().NotStartWith("Error:");
    }

    [Fact]
    public async Task GetThreadStats_ValidPid_ReturnsQueueLength()
    {
        var result = await ThreadStatsTool.GetThreadStats(TestHelpers.CurrentPid, sampleSeconds: 3);

        result.Should().Contain("Queue Length");
    }

    [Fact]
    public async Task GetThreadStats_ValidPid_ReturnsThreadPoolStatus()
    {
        var result = await ThreadStatsTool.GetThreadStats(TestHelpers.CurrentPid, sampleSeconds: 3);

        result.Should().ContainAny("HEALTHY", "ELEVATED", "STARVED");
    }
}
