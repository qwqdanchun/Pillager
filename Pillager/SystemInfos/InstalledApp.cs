using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pillager.SystemInfos
{
    internal class InstalledApp
    {
        public static string SystemInfoName = "InstalledApp";

        public static string GetInfo()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall"))
                    foreach (var subkey in key.GetSubKeyNames())
                    {
                        string value = key.OpenSubKey(subkey)?.GetValue("DisplayName", "Error").ToString();
                        if (!string.IsNullOrEmpty(value) && value != "Error" && !value.Contains("Windows"))
                            sb.AppendLine(value);
                    }
            }
            catch
            { }
            return sb.ToString();
        }
        public static void Save(string path)
        {
            try
            {
                string savepath = Path.Combine(path, SystemInfoName);
                string result = GetInfo();
                if (!string.IsNullOrEmpty(result))
                {
                    Directory.CreateDirectory(savepath);
                    File.WriteAllText(Path.Combine(savepath, SystemInfoName + ".txt"), result);
                }
            }
            catch { }
        }
    }
}
