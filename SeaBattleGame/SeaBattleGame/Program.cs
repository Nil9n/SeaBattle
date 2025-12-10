using System;
using System.Windows.Forms;

namespace SeaBattleGame
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // Запуск главной формы
            Application.Run(new MainForm());
        }
    }
}