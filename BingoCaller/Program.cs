using System;
using System.Windows.Forms;

namespace BingoCaller
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // .NET 8 replacement for EnableVisualStyles + SetCompatibleTextRenderingDefault
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}