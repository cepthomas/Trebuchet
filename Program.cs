using System;
using System.Windows.Forms;

namespace WinStart
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        //static void Main(string[] args)
        //{
        //    // To customize application configuration such as set high DPI settings or default font,
        //    // see https://aka.ms/applicationconfiguration.
        //    ApplicationConfiguration.Initialize();
        //    Application.Run(new MainForm(args));
        //}

        static void Main(string[] args)
        {
            // Handle unexpected esceptions.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, e) => { HandleException(e.Exception, "UI Thread Exception"); };
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => { HandleException((Exception)e.ExceptionObject, "Background Thread Exception"); };

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                var host = new MainForm(args);
                Application.Run(host);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "!!!");
                Environment.Exit(1);
            }
        }

        static void HandleException(Exception ex, string type)
        {
            MessageBox.Show(ex.ToString(), type);
            Environment.Exit(1);
        }

    }
}