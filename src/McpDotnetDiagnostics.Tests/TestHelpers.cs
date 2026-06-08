namespace McpDotnetDiagnostics.Tests;

public static class TestHelpers
{
    /// <summary>
    /// The PID of the current test runner process.
    /// Used for integration tests — xUnit itself is a .NET process
    /// with a live diagnostic socket we can attach to.
    /// </summary>
    public static int CurrentPid => Environment.ProcessId;

    /// <summary>
    /// A PID that is guaranteed not to exist.
    /// Used for error path unit tests.
    /// </summary>
    public const int InvalidPid = 999999;
}