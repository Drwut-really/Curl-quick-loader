using System.Diagnostics;
using CurlQuickLoader.Models;

namespace CurlQuickLoader.Services;

public class CurlRunner
{
    public record RunResult(int ExitCode, string Output, string Error);

    /// <summary>
    /// Runs the curl command for the given preset and returns the result.
    /// </summary>
    public RunResult Run(CurlPreset preset, Action<string>? outputCallback = null)
    {
        string command = CurlCommandBuilder.Build(preset);
        return RunCommand(command, outputCallback);
    }

    public RunResult RunCommand(string fullCommand, Action<string>? outputCallback = null)
    {
        // Strip leading "curl " to get arguments
        string args = fullCommand.StartsWith("curl ", StringComparison.OrdinalIgnoreCase)
            ? fullCommand[5..]
            : fullCommand;

        var psi = new ProcessStartInfo
        {
            FileName = "curl",
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        using var process = new Process { StartInfo = psi };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
                outputCallback?.Invoke(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
                errorBuilder.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        return new RunResult(process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
    }

    /// <summary>
    /// Checks whether curl is available on PATH.
    /// </summary>
    public static bool IsCurlAvailable()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "curl",
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            p?.WaitForExit(3000);
            return p?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
