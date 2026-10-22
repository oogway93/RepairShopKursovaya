using System;
using System.IO;
using System.Windows.Forms;
using RepairShop.Core;
using RepairShop.Core.Storage;

namespace RepairShop.App
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RepairShop", "data.xml");
            if (args.Length == 2 && args[0] == "--data")
                path = args[1];

            var app = new ShopApp(new XmlDataStore(path));
            DemoData.EnsureSeed(app);

            while (true)
            {
                var login = new LoginForm(app);
                if (login.ShowDialog() != DialogResult.OK)
                    return;
                var main = new MainForm(app, login.LoggedUser);
                Application.Run(main);
                if (!main.LogoutRequested)
                    return;
            }
        }
    }
}
