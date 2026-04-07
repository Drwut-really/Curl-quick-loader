using CurlQuickLoader.Models;

namespace CurlQuickLoader.Services;

public class CliHandler
{
    private readonly PresetRepository _repo;
    private readonly CurlRunner _runner;

    public CliHandler(PresetRepository repo, CurlRunner runner)
    {
        _repo = repo;
        _runner = runner;
    }

    /// <summary>
    /// Handles CLI arguments. Returns an exit code (0 = success, non-zero = failure).
    /// </summary>
    public int Handle(string[] args)
    {
        if (args.Length == 0)
            return PrintUsage();

        return args[0].ToLowerInvariant() switch
        {
            "--list" => HandleList(),
            "--run" => args.Length >= 2 ? HandleRun(args[1]) : PrintError("--run requires a preset name."),
            "--export" => args.Length >= 2 ? HandleExport(args[1]) : PrintError("--export requires a preset name."),
            "--help" or "-h" or "/?" => PrintUsage(),
            _ => PrintError($"Unknown argument: {args[0]}")
        };
    }

    private int HandleList()
    {
        var presets = _repo.Load();
        if (presets.Count == 0)
        {
            Console.WriteLine("No presets found.");
            return 0;
        }

        foreach (var p in presets)
            Console.WriteLine($"  {p.Name}  ({p.Method} {p.Url})");

        return 0;
    }

    private int HandleRun(string name)
    {
        var preset = _repo.FindByName(name);
        if (preset is null)
        {
            Console.Error.WriteLine($"Preset \"{name}\" not found.");
            return 1;
        }

        string command = CurlCommandBuilder.Build(preset);
        Console.WriteLine($"> {command}");
        Console.WriteLine();

        var result = _runner.Run(preset);

        if (!string.IsNullOrWhiteSpace(result.Output))
            Console.Write(result.Output);

        if (!string.IsNullOrWhiteSpace(result.Error))
            Console.Error.Write(result.Error);

        return result.ExitCode;
    }

    private int HandleExport(string name)
    {
        var preset = _repo.FindByName(name);
        if (preset is null)
        {
            Console.Error.WriteLine($"Preset \"{name}\" not found.");
            return 1;
        }

        Console.WriteLine(CurlCommandBuilder.Build(preset));
        return 0;
    }

    private static int PrintUsage()
    {
        Console.WriteLine("Curl Quick Loader — CLI Mode");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  CurlQuickLoader.exe                  Launch GUI");
        Console.WriteLine("  CurlQuickLoader.exe --list            List all preset names");
        Console.WriteLine("  CurlQuickLoader.exe --run <name>      Run a preset");
        Console.WriteLine("  CurlQuickLoader.exe --export <name>   Print the curl command");
        Console.WriteLine("  CurlQuickLoader.exe --help            Show this help");
        return 0;
    }

    private static int PrintError(string message)
    {
        Console.Error.WriteLine($"Error: {message}");
        Console.Error.WriteLine("Use --help for usage information.");
        return 1;
    }
}
