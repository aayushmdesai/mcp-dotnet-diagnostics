using FluentAssertions;
using McpDotnetDiagnostics.Tools;

namespace McpDotnetDiagnostics.Tests.Tools;

public class EnvironmentInfoToolTests
{
    // --- Error path (unit) ---

    [Fact]
    public void GetEnvironmentInfo_InvalidPid_ReturnsErrorMessage()
    {
        var result = EnvironmentInfoTool.GetEnvironmentInfo(TestHelpers.InvalidPid);

        result.Should().StartWith("Error:");
    }

    [Fact]
    public void GetEnvironmentInfo_InvalidPid_DoesNotThrow()
    {
        var act = () => EnvironmentInfoTool.GetEnvironmentInfo(TestHelpers.InvalidPid);

        act.Should().NotThrow();
    }

    // --- Happy path (integration) ---

    [Fact]
    public void GetEnvironmentInfo_ValidPid_ReturnsProcessName()
    {
        var result = EnvironmentInfoTool.GetEnvironmentInfo(TestHelpers.CurrentPid);

        result.Should().Contain("Process Name");
        result.Should().NotStartWith("Error:");
    }

    [Fact]
    public void GetEnvironmentInfo_ValidPid_ReturnsOsInfo()
    {
        var result = EnvironmentInfoTool.GetEnvironmentInfo(TestHelpers.CurrentPid);

        result.Should().Contain("OS");
        result.Should().Contain("CPU Cores");
    }

    [Fact]
    public void GetEnvironmentInfo_ValidPid_ReturnsPid()
    {
        var result = EnvironmentInfoTool.GetEnvironmentInfo(TestHelpers.CurrentPid);

        result.Should().Contain(TestHelpers.CurrentPid.ToString());
    }
}
