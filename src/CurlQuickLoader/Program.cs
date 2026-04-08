using System.Runtime.InteropServices;
using CurlQuickLoader.Forms;
using CurlQuickLoader.Services;

namespace CurlQuickLoader;

static class Program
{
    [DllImport("kernel32.dll")] private static extern bool FreeConsole();
    [DllImport("kernel32.dll")] private static extern IntPtr GetConsoleWindow();
    [DllImport("kernel32.dll")] private static extern uint GetConsoleProcessList(uint[] pids, uint count);
    [DllImport("user32.dll")]   private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_HIDE = 0;

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
            // CLI mode — Exe subsystem means the shell already owns a console
            // for this process and waits for it to exit before showing the prompt.
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

        // GUI mode — detach from the console so no black window appears.
        //
        // Two launch scenarios:
        //   a) Double-click / Explorer: Windows creates a brand-new console for
        //      this process. GetConsoleProcessList returns 1 (only us). We must
        //      hide the window BEFORE freeing it, otherwise it flashes visibly.
        //   b) Launched from cmd/PowerShell with no args: the shell's console is
        //      inherited (process list has ≥ 2 entries). We just detach — hiding
        //      the window would close the user's terminal, which is wrong.
        uint[] pids = new uint[2];
        bool isOwnedConsole = GetConsoleProcessList(pids, 2) == 1;
        if (isOwnedConsole)
        {
            IntPtr hwnd = GetConsoleWindow();
            if (hwnd != IntPtr.Zero)
                ShowWindow(hwnd, SW_HIDE); // hide before free to prevent flash
        }
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
