using FluentAssertions;
using McpDotnetDiagnostics.Tools;

namespace McpDotnetDiagnostics.Tests.Tools;

public class ProcessInfoToolTests
{
    // --- Error path (unit) ---

    [Fact]
    public void GetProcessInfo_InvalidPid_ReturnsErrorMessage()
    {
        var result = ProcessInfoTool.GetProcessInfo(TestHelpers.InvalidPid);

        result.Should().StartWith("Error:");
        result.Should().Contain(TestHelpers.InvalidPid.ToString());
    }

    [Fact]
    public void GetProcessInfo_InvalidPid_DoesNotThrow()
    {
        var act = () => ProcessInfoTool.GetProcessInfo(TestHelpers.InvalidPid);

        act.Should().NotThrow();
    }

    // --- Happy path (integration) ---

    [Fact]
    public void GetProcessInfo_ValidPid_ReturnsProcessName()
    {
        var result = ProcessInfoTool.GetProcessInfo(TestHelpers.CurrentPid);

        result.Should().Contain("Process Name");
        result.Should().NotStartWith("Error:");
    }

    [Fact]
    public void GetProcessInfo_ValidPid_ReturnsPid()
    {
        var result = ProcessInfoTool.GetProcessInfo(TestHelpers.CurrentPid);

        result.Should().Contain(TestHelpers.CurrentPid.ToString());
    }

    [Fact]
    public void GetProcessInfo_ValidPid_ReturnsDotNetVersion()
    {
        var result = ProcessInfoTool.GetProcessInfo(TestHelpers.CurrentPid);

        result.Should().Contain(".NET Version");
    }

    [Fact]
    public void GetProcessInfo_ValidPid_ReturnsWorkingSet()
    {
        var result = ProcessInfoTool.GetProcessInfo(TestHelpers.CurrentPid);

        result.Should().Contain("Working Set");
    }
}
