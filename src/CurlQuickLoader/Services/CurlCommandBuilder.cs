using System.Text;
using CurlQuickLoader.Models;

namespace CurlQuickLoader.Services;

public static class CurlCommandBuilder
{
    public static string Build(CurlPreset preset)
    {
        var sb = new StringBuilder();
        sb.Append("curl");

        // Method
        sb.Append($" -X {preset.Method}");

        // URL
        sb.Append($" \"{EscapeDoubleQuotes(preset.Url)}\"");

        // Headers
        foreach (var header in preset.Headers)
        {
            if (string.IsNullOrWhiteSpace(header.Key))
                continue;

            string key = StripNewlines(header.Key);
            string value = StripNewlines(header.Value);
            sb.Append($" -H \"{EscapeDoubleQuotes(key)}: {EscapeDoubleQuotes(value)}\"");
        }

        // Body
        if (!string.IsNullOrEmpty(preset.Body))
        {
            string body = EscapeDoubleQuotes(preset.Body);
            sb.Append($" --data-binary \"{body}\"");
        }

        // Extra flags (appended verbatim)
        if (!string.IsNullOrWhiteSpace(preset.ExtraFlags))
        {
            sb.Append($" {preset.ExtraFlags.Trim()}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Validates headers for injection risks. Returns a list of warning messages (empty = OK).
    /// </summary>
    public static List<string> ValidateHeaders(List<Header> headers)
    {
        var warnings = new List<string>();
        foreach (var h in headers)
        {
            if (h.Key.Contains('\n') || h.Key.Contains('\r'))
                warnings.Add($"Header key \"{h.Key}\" contains a newline and was sanitized.");
            if (h.Value.Contains('\n') || h.Value.Contains('\r'))
                warnings.Add($"Header value for \"{h.Key}\" contains a newline and was sanitized.");
        }
        return warnings;
    }

    private static string EscapeDoubleQuotes(string s) => s.Replace("\"", "\\\"");

    private static string StripNewlines(string s) => s.Replace("\r", "").Replace("\n", "");
}
