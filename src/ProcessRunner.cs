using System.Diagnostics;
using System.Text;

namespace EncyExtensionMcp;

public record ProcResult(int ExitCode, string StdOut, string StdErr)
{
    public bool Ok => ExitCode == 0;
    /** stdout, or stderr when stdout is empty — for error surfaces. */
    public string Output => string.IsNullOrWhiteSpace(StdOut) ? StdErr : StdOut;
}

public interface IProcessRunner
{
    Task<ProcResult> Run(string fileName, string arguments, string? workingDir = null,
        IDictionary<string, string>? env = null, int timeoutSeconds = 120);
}

public class ProcessRunner : IProcessRunner
{
    public async Task<ProcResult> Run(string fileName, string arguments, string? workingDir = null,
        IDictionary<string, string>? env = null, int timeoutSeconds = 120)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDir ?? Environment.CurrentDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        if (env != null)
            foreach (var (k, v) in env) psi.Environment[k] = v;

        using var p = new Process { StartInfo = psi };
        p.Start();
        p.StandardInput.Close();
        var stdout = p.StandardOutput.ReadToEndAsync();
        var stderr = p.StandardError.ReadToEndAsync();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        try
        {
            await p.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { p.Kill(entireProcessTree: true); } catch { /* already gone */ }
            return new ProcResult(-1, await stdout, $"timed out after {timeoutSeconds}s: {fileName} {arguments}");
        }
        return new ProcResult(p.ExitCode, await stdout, await stderr);
    }
}
