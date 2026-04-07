using System.Runtime.InteropServices;
using CurlQuickLoader.Forms;
using CurlQuickLoader.Services;

namespace CurlQuickLoader;

static class Program
{
    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);

    private const int ATTACH_PARENT_PROCESS = -1;

    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            // CLI mode: re-attach to the parent console so output is visible
            AttachConsole(ATTACH_PARENT_PROCESS);

            // Print a blank line so output starts on a new line after the command prompt
            Console.WriteLine();

            var repo = new PresetRepository();
            var runner = new CurlRunner();
            var cli = new CliHandler(repo, runner);
            int exitCode = cli.Handle(args);
            Environment.Exit(exitCode);
            return;
        }

        // GUI mode
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}
