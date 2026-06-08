using FluentAssertions;
using McpDotnetDiagnostics.Tools;

namespace McpDotnetDiagnostics.Tests.Tools;

public class GcEventsToolTests
{
    // --- Error path (unit) ---

    [Fact]
    public async Task GetGcEvents_InvalidPid_ReturnsErrorMessage()
    {
        var result = await GcEventsTool.GetGcEvents(TestHelpers.InvalidPid);

        result.Should().StartWith("Error:");
        result.Should().Contain(TestHelpers.InvalidPid.ToString());
    }

    [Fact]
    public async Task GetGcEvents_InvalidPid_DoesNotThrow()
    {
        var act = async () => await GcEventsTool.GetGcEvents(TestHelpers.InvalidPid);

        await act.Should().NotThrowAsync();
    }

    // --- Happy path (integration) ---

    [Fact]
    public async Task GetGcEvents_ValidPid_ReturnsResult()
    {
        var result = await GcEventsTool.GetGcEvents(TestHelpers.CurrentPid, sampleSeconds: 3);

        result.Should().NotStartWith("Error:");
    }

    [Fact]
    public async Task GetGcEvents_ValidPid_ReturnsEitherEventsOrNoEventsMessage()
    {
        var result = await GcEventsTool.GetGcEvents(TestHelpers.CurrentPid, sampleSeconds: 3);

        result.Should().ContainAny("GC Events", "No GC events observed");
    }
}
