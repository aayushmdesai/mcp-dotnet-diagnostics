using FluentAssertions;
using McpDotnetDiagnostics.Tools;

namespace McpDotnetDiagnostics.Tests.Tools;

public class EventCountersToolTests
{
    // --- Error path (unit) ---

    [Fact]
    public async Task GetEventCounters_InvalidPid_ReturnsErrorMessage()
    {
        var result = await EventCountersTool.GetEventCounters(TestHelpers.InvalidPid);

        result.Should().StartWith("Error:");
    }

    [Fact]
    public async Task GetEventCounters_InvalidPid_DoesNotThrow()
    {
        var act = async () => await EventCountersTool.GetEventCounters(TestHelpers.InvalidPid);

        await act.Should().NotThrowAsync();
    }

    // --- Happy path (integration) ---

    [Fact]
    public async Task GetEventCounters_ValidPid_ReturnsMetrics()
    {
        var result = await EventCountersTool.GetEventCounters(
            TestHelpers.CurrentPid,
            sampleSeconds: 3
        );

        result.Should().Contain("Event Counters");
        result.Should().NotStartWith("Error:");
    }

    [Fact]
    public async Task GetEventCounters_ValidPid_ReturnsCpuUsage()
    {
        var result = await EventCountersTool.GetEventCounters(
            TestHelpers.CurrentPid,
            sampleSeconds: 3
        );

        result.Should().Contain("cpu-usage");
    }

    [Fact]
    public async Task GetEventCounters_ValidPid_ReturnsThreadPoolMetrics()
    {
        var result = await EventCountersTool.GetEventCounters(
            TestHelpers.CurrentPid,
            sampleSeconds: 3
        );

        result.Should().Contain("threadpool-thread-count");
    }
}
