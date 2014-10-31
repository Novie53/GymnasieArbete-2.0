using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Logging_Program
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Exception ex = (Exception)e.ExceptionObject;
                //TODO IMPLIMENT
                //MainLib.VarClass.writeToLog("Fallskärm", "", e.ExceptionObject.ToString(), "", "", ex.Message, "", ex.StackTrace, "", ex.ToString());
                //Todo, TEMP, ta reda på vad som kan vara bra att spara och vad som kan lämnas
            }
            finally
            {
                Application.Exit();
            }
        }
    }
}
