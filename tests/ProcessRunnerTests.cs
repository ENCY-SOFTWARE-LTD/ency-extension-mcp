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

    /**
     * npm installs its CLIs on Windows as a .cmd shim plus an extensionless shell script, and the bare
     * name resolves to neither. That is why `setup` reported Claude Code as missing on a machine where
     * `claude` was on PATH (2026-07-25).
     */
    [Fact]
    public async Task FindsAWindowsCmdShimByItsBareName()
    {
        if (!OperatingSystem.IsWindows()) return;

        string dir = Path.Combine(Path.GetTempPath(), "shim-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string name = "fakecli" + Guid.NewGuid().ToString("N")[..6];
        File.WriteAllText(Path.Combine(dir, name + ".cmd"), "@echo shim-ran\r\n");
        string oldPath = Environment.GetEnvironmentVariable("PATH") ?? "";
        Environment.SetEnvironmentVariable("PATH", dir + Path.PathSeparator + oldPath);
        try
        {
            var result = await new ProcessRunner().Run(name, "");

            Assert.True(result.Ok, result.StdErr);
            Assert.Contains("shim-ran", result.StdOut);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", oldPath);
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ARealCommandStillRuns()
    {
        var result = await new ProcessRunner().Run("dotnet", "--version");

        Assert.True(result.Ok, result.StdErr);
        Assert.False(string.IsNullOrWhiteSpace(result.StdOut));
    }
}
