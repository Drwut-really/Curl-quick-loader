using System.Diagnostics;
using CurlQuickLoader.Models;

namespace CurlQuickLoader.Services;

public class CurlRunner
{
    public record RunResult(int ExitCode, string Output, string Error);

    /// <summary>
    /// Runs the curl command for the given preset and returns the result.
    /// Uses ProcessStartInfo.ArgumentList so that bodies with embedded quotes
    /// or newlines are passed to curl without any double-escaping issues.
    /// </summary>
    public RunResult Run(CurlPreset preset, Action<string>? outputCallback = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "curl",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        psi.ArgumentList.Add("-X");
        psi.ArgumentList.Add(preset.Method);
        psi.ArgumentList.Add(preset.Url);

        foreach (var header in preset.Headers)
        {
            if (string.IsNullOrWhiteSpace(header.Key)) continue;
            string key = header.Key.Replace("\r", "").Replace("\n", "");
            string value = header.Value.Replace("\r", "").Replace("\n", "");
            psi.ArgumentList.Add("-H");
            psi.ArgumentList.Add($"{key}: {value}");
        }

        foreach (var entry in preset.FormData)
        {
            if (string.IsNullOrWhiteSpace(entry.Key)) continue;
            string key = entry.Key.Replace("\r", "").Replace("\n", "");
            string value = entry.Value.Replace("\r", "").Replace("\n", "");
            psi.ArgumentList.Add("--form-string");
            psi.ArgumentList.Add($"{key}={value}");
        }

        if (!string.IsNullOrEmpty(preset.Body))
        {
            // Mirror the Content-Type auto-injection from CurlCommandBuilder so that
            // execution matches the command preview shown in the UI.
            bool hasContentType = preset.Headers.Any(h =>
                string.Equals(h.Key.Trim(), "Content-Type", StringComparison.OrdinalIgnoreCase));
            if (!hasContentType)
            {
                psi.ArgumentList.Add("-H");
                psi.ArgumentList.Add("Content-Type: application/json");
            }

            psi.ArgumentList.Add("--data-binary");
            psi.ArgumentList.Add(preset.Body);
        }

        if (!string.IsNullOrWhiteSpace(preset.ExtraFlags))
        {
            foreach (var arg in SplitArgs(preset.ExtraFlags.Trim()))
                psi.ArgumentList.Add(arg);
        }

        return RunProcess(psi, outputCallback);
    }

    /// <summary>
    /// Runs a raw curl command string (e.g. from clipboard or export).
    /// </summary>
    public RunResult RunCommand(string fullCommand, Action<string>? outputCallback = null)
    {
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

        return RunProcess(psi, outputCallback);
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

    private static RunResult RunProcess(ProcessStartInfo psi, Action<string>? outputCallback)
    {
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
    /// Splits a flags string (e.g. "--insecure -v --max-time 30") into individual arguments,
    /// respecting double-quoted groups.
    /// </summary>
    private static IEnumerable<string> SplitArgs(string input)
    {
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;

        foreach (char c in input)
        {
            if (c == '"')
                inQuotes = !inQuotes;
            else if ((c == ' ' || c == '\t') && !inQuotes)
            {
                if (current.Length > 0)
                {
                    yield return current.ToString();
                    current.Clear();
                }
            }
            else
                current.Append(c);
        }

        if (current.Length > 0)
            yield return current.ToString();
    }
}
