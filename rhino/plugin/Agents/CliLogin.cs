using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Rhino.AI;

// What the CLI itself says about its login, and how to start a new one.
//
// Both CLIs ship a non-interactive status command (`claude auth status --json`, `codex login
// status`), so the plugin never has to guess a sign-out from error text: it asks. Signing IN,
// by contrast, cannot happen in the agent's own process (spawned CreateNoWindow with all three
// streams redirected, while the flow wants a console and a browser), so it gets its own terminal
// window. RhinoApp-free so it Compile Include's into the tests.
internal static class CliLogin
{
    // Deliberately tri-state: only a CLI that SAYS it is signed out gets a sign-in window. A probe
    // that timed out, crashed, or answered in a shape we don't know is Unknown, and Unknown leaves
    // the caller on its ordinary error path rather than sending the user off to sign in for nothing.
    public enum State
    {
        Unknown,
        SignedIn,
        SignedOut,
    }

    // Long enough for a cold CLI start on a busy machine, short enough that a hung probe cannot
    // hold up the prompt it runs in front of.
    private static TimeSpan ProbeTimeout { get; } = TimeSpan.FromSeconds(10);

    // Run the status command hidden and hand its output to the CLI's own reader. Every failure mode
    // (missing binary, timeout, non-zero exit with nothing to read) lands on Unknown by design.
    public static async Task<State> ProbeAsync(string cliPath, IReadOnlyList<string> statusArguments, Func<string, int, State> read)
    {
        if (cliPath.Length == 0 || statusArguments.Count == 0)
            return State.Unknown;

        ProcessStartInfo psi = new()
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        CliProcess.ConfigureEncoding(psi);
        CliProcess.ConfigureFileName(psi, cliPath);
        foreach (string argument in statusArguments)
            psi.ArgumentList.Add(argument);

        try
        {
            using Process proc = Process.Start(psi) ?? throw new InvalidOperationException("no process");
            using CancellationTokenSource timeout = new(ProbeTimeout);

            // Read both pipes before waiting: a status command that filled one of them would
            // otherwise block on a full buffer and be killed as a false timeout.
            Task<string> stdout = proc.StandardOutput.ReadToEndAsync();
            Task<string> stderr = proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync(timeout.Token).ConfigureAwait(false);

            return read(await stdout.ConfigureAwait(false) + await stderr.ConfigureAwait(false), proc.ExitCode);
        }
        catch (Exception)
        {
            return State.Unknown;
        }
    }

    // Start the CLI's sign-in in a terminal of its own. Worked-or-not with a reason, because the
    // caller's fallback (naming the command for the user to run) is a perfectly good outcome.
    public static bool TryStart(string cliPath, IReadOnlyList<string> loginArguments, out string error)
    {
        error = string.Empty;
        if (cliPath.Length == 0)
        {
            error = "the CLI path is not known yet";
            return false;
        }
        if (loginArguments.Count == 0)
        {
            error = "this CLI has no sign-in command";
            return false;
        }

        try
        {
            Process.Start(StartInfo(cliPath, string.Join(' ', loginArguments)));
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static ProcessStartInfo StartInfo(string cliPath, string arguments)
    {
        // macOS has no console to allocate, so the window has to come from Terminal.app itself.
        if (OperatingSystem.IsMacOS())
        {
            string command = $"{Quote(cliPath)} {arguments}";
            ProcessStartInfo psi = new("/usr/bin/osascript");
            psi.ArgumentList.Add("-e");
            psi.ArgumentList.Add($"tell application \"Terminal\" to do script \"{Escape(command)}\"");
            psi.ArgumentList.Add("-e");
            psi.ArgumentList.Add("tell application \"Terminal\" to activate");
            return psi;
        }

        // ShellExecute (not a cmd.exe wrapper) is what gives a console exe its own window, and it
        // takes the binary as a path rather than a command line, so a spacey install dir and a .cmd
        // shim both launch without any quoting of our own.
        return new ProcessStartInfo(cliPath, arguments)
        {
            UseShellExecute = true,
            CreateNoWindow = false,
        };
    }

    // POSIX single-quoting for the command that goes inside AppleScript's `do script`...
    private static string Quote(string path) => $"'{path.Replace("'", "'\\''")}'";

    // ...and then that whole command becomes an AppleScript string literal.
    private static string Escape(string command) => command.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
