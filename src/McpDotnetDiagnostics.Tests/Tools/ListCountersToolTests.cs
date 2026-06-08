using FluentAssertions;
using McpDotnetDiagnostics.Tools;

namespace McpDotnetDiagnostics.Tests.Tools;

public class ListCountersToolTests
{
    // --- Error path (unit) ---

    [Fact]
    public async Task ListCounters_InvalidPid_ReturnsErrorMessage()
    {
        var result = await ListCountersTool.ListCounters(TestHelpers.InvalidPid);

        result.Should().StartWith("Error:");
    }

    [Fact]
    public async Task ListCounters_InvalidPid_DoesNotThrow()
    {
        var act = async () => await ListCountersTool.ListCounters(TestHelpers.InvalidPid);

        await act.Should().NotThrowAsync();
    }

    // --- Happy path (integration) ---

    [Fact]
    public async Task ListCounters_ValidPid_ReturnsCounters()
    {
        var result = await ListCountersTool.ListCounters(TestHelpers.CurrentPid, sampleSeconds: 3);

        result.Should().NotStartWith("Error:");
    }
}
