using System.Diagnostics;
using System.Text;

namespace Jellyfin.Plugin.ListenBrainz.IntegrationTests.Infrastructure;

/// <summary>
/// Result of an external command invocation.
/// </summary>
/// <param name="Command">The command line that was executed, for diagnostics.</param>
/// <param name="ExitCode">Process exit code.</param>
/// <param name="StandardOutput">Captured standard output.</param>
/// <param name="StandardError">Captured standard error.</param>
internal sealed record ProcessResult(string Command, int ExitCode, string StandardOutput, string StandardError)
{
    /// <summary>
    /// Throws when the command did not exit successfully.
    /// </summary>
    /// <returns>This result, to allow chaining.</returns>
    /// <exception cref="InvalidOperationException">The command failed.</exception>
    public ProcessResult EnsureSuccess()
    {
        if (ExitCode == 0)
        {
            return this;
        }

        throw new InvalidOperationException(
            $"Command failed with exit code {ExitCode}: {Command}{Environment.NewLine}" +
            $"--- stdout ---{Environment.NewLine}{StandardOutput}{Environment.NewLine}" +
            $"--- stderr ---{Environment.NewLine}{StandardError}");
    }
}

/// <summary>
/// Minimal wrapper for running external commands with captured output.
/// </summary>
internal static class ProcessRunner
{
    private static readonly TimeSpan _drainGracePeriod = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Runs a command to completion.
    /// </summary>
    /// <param name="fileName">Executable to run.</param>
    /// <param name="arguments">Arguments, passed without shell interpretation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The captured process result.</returns>
    public static async Task<ProcessResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = RepositoryLayout.Root,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        var commandLine = $"{fileName} {string.Join(' ', arguments)}";
        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        var exited = new TaskCompletionSource();
        process.Exited += (_, _) => exited.TrySetResult();

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        process.OutputDataReceived += (_, e) => AppendLine(stdout, e.Data);
        process.ErrorDataReceived += (_, e) => AppendLine(stderr, e.Data);

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Could not start '{fileName}'. Is it installed and on PATH?", ex);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (process.HasExited)
        {
            exited.TrySetResult();
        }

        try
        {
            await exited.Task.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        // The process is gone, so this only drains what the readers still have buffered. It is
        // capped because a grandchild that inherited the redirected handles keeps the pipe open
        // past its parent, and waiting for EOF would then never return.
        await Task.WhenAny(
            process.WaitForExitAsync(CancellationToken.None),
            Task.Delay(_drainGracePeriod, CancellationToken.None));

        return new ProcessResult(commandLine, process.ExitCode, stdout.ToString(), stderr.ToString());
    }

    private static void AppendLine(StringBuilder builder, string? line)
    {
        if (line is null)
        {
            return;
        }

        lock (builder)
        {
            builder.AppendLine(line);
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (Exception)
        {
            // The process already exited; nothing to clean up.
        }
    }
}
