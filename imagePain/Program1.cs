using imagePain;
using System;
using System.Windows.Forms;

namespace imagePain.Forms // Замените на ваше пространство имен
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm()); // Замените MainForm на имя вашей основной формы
        }
    }
}

