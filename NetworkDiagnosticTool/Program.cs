using System;
using System.Threading;
using System.Windows.Forms;
using NetworkDiagnosticTool.Forms;

namespace NetworkDiagnosticTool
{
    static class Program
    {
        private static Mutex _mutex;

        [STAThread]
        static void Main()
        {
            // Ensure single instance
            bool createdNew;
            _mutex = new Mutex(true, "NetworkDiagnosticTool_SingleInstance", out createdNew);

            if (!createdNew)
            {
                MessageBox.Show(
                    "Network Diagnostic Tool is already running.\n\nCheck the system tray for the application icon.",
                    "Already Running",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Set up unhandled exception handlers
                Application.ThreadException += Application_ThreadException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                Application.Run(new MainForm());
            }
            finally
            {
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
            }
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            HandleException(e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException(e.ExceptionObject as Exception);
        }

        private static void HandleException(Exception ex)
        {
            if (ex == null) return;

            var message = $"An unexpected error occurred:\n\n{ex.Message}\n\nWould you like to continue running the application?";

            var result = MessageBox.Show(
                message,
                "Error",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Error);

            if (result == DialogResult.No)
            {
                Application.Exit();
            }
        }
    }
}
