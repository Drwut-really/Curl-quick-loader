using System.Runtime.InteropServices;
using CurlQuickLoader.Forms;
using CurlQuickLoader.Services;

namespace CurlQuickLoader;

static class Program
{
    // Used in GUI mode to detach from the console so no window appears.
    // With OutputType=Exe the shell attaches a console automatically; freeing
    // it before the message loop prevents an unwanted black window on launch.
    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    [STAThread]
    static void Main(string[] args)
    {
        // Wire up unhandled exception handlers before anything else
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        Application.ThreadException += OnThreadException;
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        Logger.Info("=== CurlQuickLoader starting ===");
        Logger.Info($"Args: [{string.Join(", ", args)}]");

        if (args.Length > 0)
        {
            // CLI mode.
            // OutputType=Exe means the shell (cmd / PowerShell) already holds
            // a console for this process and waits for it to exit before
            // returning the prompt — no AttachConsole dance needed.
            Console.WriteLine();

            Logger.Info("Running in CLI mode");
            try
            {
                var repo = new PresetRepository();
                var runner = new CurlRunner();
                var cli = new CliHandler(repo, runner);
                int exitCode = cli.Handle(args);
                Logger.Info($"CLI exiting with code {exitCode}");
                Environment.Exit(exitCode);
            }
            catch (Exception ex)
            {
                Logger.Error("Unhandled exception in CLI mode", ex);
                Console.Error.WriteLine($"Fatal error: {ex.Message}");
                Environment.Exit(1);
            }
            return;
        }

        // GUI mode — detach from the console immediately so no black window
        // appears when the user double-clicks or launches without switches.
        FreeConsole();

        Logger.Info("Running in GUI mode");
        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            Logger.Info("Creating MainForm...");
            var form = new MainForm();
            Logger.Info("MainForm created, starting message loop");
            Application.Run(form);
            Logger.Info("Message loop exited normally");
        }
        catch (Exception ex)
        {
            Logger.Error("Fatal exception during GUI startup", ex);
            MessageBox.Show(
                $"A fatal error occurred during startup:\n\n{ex.GetType().Name}: {ex.Message}\n\nCheck logs\\ folder next to the exe for details.",
                "Curl Quick Loader — Startup Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
    {
        Logger.Error("Unhandled UI thread exception", e.Exception);
        var result = MessageBox.Show(
            $"An unexpected error occurred:\n\n{e.Exception.GetType().Name}: {e.Exception.Message}\n\nCheck logs\\ folder for details.\n\nContinue running?",
            "Curl Quick Loader — Error",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Error);
        if (result == DialogResult.No)
            Application.Exit();
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        Logger.Error("Unhandled non-UI exception", ex);
    }
}
