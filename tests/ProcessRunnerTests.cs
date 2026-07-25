using EncyExtensionMcp;
using Xunit;

public class ProcessRunnerTests
{
    /**
     * Every tool here shells out to something optional (claude, gh) and probes it first. Process.Start
     * throws when the executable is missing, which killed the whole server instead of failing one call
     * — seen live when `setup` ran on a machine without Claude Code.
     */
    [Fact]
    public async Task AMissingExecutableIsAFailedResultNotAnException()
    {
        var result = await new ProcessRunner().Run("definitely-not-installed-" + Guid.NewGuid().ToString("N"), "--version");

        Assert.False(result.Ok);
        Assert.Contains("not installed or not on PATH", result.StdErr);
    }

    [Fact]
    public async Task ARealCommandStillRuns()
    {
        var result = await new ProcessRunner().Run("dotnet", "--version");

        Assert.True(result.Ok, result.StdErr);
        Assert.False(string.IsNullOrWhiteSpace(result.StdOut));
    }
}
