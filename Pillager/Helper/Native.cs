using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Pillager.Helper
{
    public static class Native
    {
        [DllImport("shell32.dll")]
        public static extern int SHGetFolderPath(IntPtr hwndOwner, int nFolder, IntPtr hToken, uint dwFlags, [Out] StringBuilder pszPath);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetProcessDPIAware();
        [DllImport("ntdll", SetLastError = true)]
        public static extern uint NtSuspendProcess([In] IntPtr Handle);
        [DllImport("ntdll.dll", SetLastError = false)]
        public static extern uint NtResumeProcess(IntPtr ProcessHandle);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadFile(IntPtr hFile, [Out] byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped);
        [DllImport("kernel32.dll", EntryPoint = "SetFilePointer")]
        public static extern int SetFilePointer(IntPtr hFile, int lDistanceToMove, int lpDistanceToMoveHigh, int dwMoveMethod);


        [DllImport("Wlanapi.dll")]
        public static extern int WlanOpenHandle(int dwClientVersion, IntPtr pReserved, [Out] out IntPtr pdwNegotiatedVersion, ref IntPtr ClientHandle);

        [DllImport("Wlanapi", EntryPoint = "WlanCloseHandle")]
        public static extern uint WlanCloseHandle([In] IntPtr hClientHandle, IntPtr pReserved);


        [DllImport("Wlanapi", EntryPoint = "WlanEnumInterfaces")]
        public static extern uint WlanEnumInterfaces([In] IntPtr hClientHandle, IntPtr pReserved, ref IntPtr ppInterfaceList);


        [DllImport("wlanapi.dll", SetLastError = true)]
        public static extern uint WlanGetProfile([In] IntPtr clientHandle, [In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid, [In, MarshalAs(UnmanagedType.LPWStr)] string profileName, [In] IntPtr pReserved, [Out, MarshalAs(UnmanagedType.LPWStr)] out string profileXml, [In, Out, Optional] ref int flags, [Out, Optional] out IntPtr pdwGrantedAccess);

        [DllImport("wlanapi.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public static extern uint WlanGetProfileList([In] IntPtr clientHandle, [In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid, [In] IntPtr pReserved, ref IntPtr profileList);

        [StructLayout(LayoutKind.Sequential)]
        public struct WLAN_INTERFACE_INFO_LIST
        {

            public int dwNumberofItems;
            public int dwIndex;
            public WLAN_INTERFACE_INFO[] InterfaceInfo;


            public WLAN_INTERFACE_INFO_LIST(IntPtr pList)
            {
                dwNumberofItems = (int)Marshal.ReadInt64(pList, 0);
                dwIndex = (int)Marshal.ReadInt64(pList, 4);
                InterfaceInfo = new WLAN_INTERFACE_INFO[dwNumberofItems];
                for (int i = 0; i < dwNumberofItems; i++)
                {
                    IntPtr pItemList = new IntPtr(pList.ToInt64() + (i * 532) + 8);
                    WLAN_INTERFACE_INFO wii = new WLAN_INTERFACE_INFO();
                    wii = (WLAN_INTERFACE_INFO)Marshal.PtrToStructure(pItemList, typeof(WLAN_INTERFACE_INFO));
                    InterfaceInfo[i] = wii;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WLAN_INTERFACE_INFO
        {
            public Guid InterfaceGuid;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strInterfaceDescription;

        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WLAN_PROFILE_INFO
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strProfileName;
            public WlanProfileFlags ProfileFLags;
        }

        [Flags]
        public enum WlanProfileFlags
        {
            AllUser = 0,
            GroupPolicy = 1,
            User = 2
        }

        public struct WLAN_PROFILE_INFO_LIST
        {
            public int dwNumberOfItems;
            public int dwIndex;
            public WLAN_PROFILE_INFO[] ProfileInfo;

            public WLAN_PROFILE_INFO_LIST(IntPtr ppProfileList)
            {
                dwNumberOfItems = (int)Marshal.ReadInt64(ppProfileList);
                dwIndex = (int)Marshal.ReadInt64(ppProfileList, 4);
                ProfileInfo = new WLAN_PROFILE_INFO[dwNumberOfItems];
                IntPtr ppProfileListTemp = new IntPtr(ppProfileList.ToInt64() + 8);

                for (int i = 0; i < dwNumberOfItems; i++)
                {
                    ppProfileList = new IntPtr(ppProfileListTemp.ToInt64() + i * Marshal.SizeOf(typeof(WLAN_PROFILE_INFO)));
                    ProfileInfo[i] = (WLAN_PROFILE_INFO)Marshal.PtrToStructure(ppProfileList, typeof(WLAN_PROFILE_INFO));
                }
            }
        }

        internal const int PROCESS_TERMINATE = 0x0001;
        internal const int PROCESS_CREATE_THREAD = 0x0002;
        internal const int PROCESS_DUP_HANDLE = 0x0040;
        internal const int PROCESS_CREATE_PROCESS = 0x0080;
        internal const int PROCESS_SET_QUOTA = 0x0100;
        internal const int PROCESS_SET_INFORMATION = 0x0200;
        internal const int PROCESS_SUSPEND_RESUME = 0x0800;
        internal const int PROCESS_QUERY_INFORMATION = 0x400;
        internal const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        internal const int PROCESS_VM_OPERATION = 0x08;
        internal const int PROCESS_VM_READ = 0x10;
        internal const int PROCESS_VM_WRITE = 0x20;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SYSTEM_HANDLE_INFORMATION
        { // Information Class 16
            public ushort ProcessID;
            public ushort CreatorBackTrackIndex;
            public byte ObjectType;
            public byte HandleAttribute;
            public ushort Handle;
            public IntPtr Object_Pointer;
            public IntPtr AccessMask;
        }

        public enum OBJECT_INFORMATION_CLASS : int
        {
            ObjectBasicInformation = 0,
            ObjectNameInformation = 1,
            ObjectTypeInformation = 2,
            ObjectAllTypesInformation = 3,
            ObjectHandleInformation = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FileNameInformation
        {
            public int NameLength;
            public char Name;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UNICODE_STRING
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;

            public override string ToString()
            {
                return Buffer != IntPtr.Zero ? Marshal.PtrToStringUni(Buffer, Length / 2) : null;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct GENERIC_MAPPING
        {
            public int GenericRead;
            public int GenericWrite;
            public int GenericExecute;
            public int GenericAll;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OBJECT_TYPE_INFORMATION
        {
            public UNICODE_STRING Name;
            int TotalNumberOfObjects;
            int TotalNumberOfHandles;
            int TotalPagedPoolUsage;
            int TotalNonPagedPoolUsage;
            int TotalNamePoolUsage;
            int TotalHandleTableUsage;
            int HighWaterNumberOfObjects;
            int HighWaterNumberOfHandles;
            int HighWaterPagedPoolUsage;
            int HighWaterNonPagedPoolUsage;
            int HighWaterNamePoolUsage;
            int HighWaterHandleTableUsage;
            int InvalidAttributes;
            GENERIC_MAPPING GenericMapping;
            int ValidAccess;
            bool SecurityRequired;
            bool MaintainHandleCount;
            ushort MaintainTypeList;
            POOL_TYPE PoolType;
            int PagedPoolUsage;
            int NonPagedPoolUsage;
        }

        public enum POOL_TYPE
        {
            NonPagedPool,
            PagedPool,
            NonPagedPoolMustSucceed,
            DontUseThisType,
            NonPagedPoolCacheAligned,
            PagedPoolCacheAligned,
            NonPagedPoolCacheAlignedMustS
        }

        public const int CNST_SYSTEM_HANDLE_INFORMATION = 0x10;
        public const int DUPLICATE_SAME_ACCESS = 0x2;

        [DllImport("ntdll.dll")]
        public static extern int NtQueryObject(IntPtr ObjectHandle, int ObjectInformationClass, IntPtr ObjectInformation, int ObjectInformationLength, ref int returnLength);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        public static extern bool GetHandleInformation(IntPtr hObject, out uint lpdwFlags);

        [DllImport("ntdll.dll")]
        public static extern uint NtQuerySystemInformation(int SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength, ref int returnLength);


        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(PROCESS_ACCESS_FLAGS dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        public static extern IntPtr CreateFile(
            string lpFileName,
            int dwDesiredAccess,
            FileShare dwShareMode,
            IntPtr lpSecurityAttributes,
            FileMode dwCreationDisposition,
            int dwFlagsAndAttributes,
            IntPtr hTemplateFile);
        [DllImport("ntdll.dll")]
        public static extern uint NtQueryInformationFile(IntPtr fileHandle, ref IO_STATUS_BLOCK IoStatusBlock,
            IntPtr pInfoBlock, uint length, FILE_INFORMATION_CLASS fileInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, IntPtr hSourceHandle, IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle, uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwOptions);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        public const uint STATUS_SUCCESS = 0;
        public const uint STATUS_INFO_LENGTH_MISMATCH = 0xC0000004;

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct IO_STATUS_BLOCK
        {
            public uint Status;
            public IntPtr Information;
        }

        [Flags]
        public enum PROCESS_ACCESS_FLAGS : uint
        {
            PROCESS_ALL_ACCESS = 0x001F0FFF,
            PROCESS_CREATE_PROCESS = 0x0080,
            PROCESS_CREATE_THREAD = 0x0002,
            PROCESS_DUP_HANDLE = 0x0040,
            PROCESS_QUERY_INFORMATION = 0x0400,
            PROCESS_QUERY_LIMITED_INFORMATION = 0x1000,
            PROCESS_SET_INFORMATION = 0x0200,
            PROCESS_SET_QUOTA = 0x0100,
            PROCESS_SUSPEND_RESUME = 0x0800,
            PROCESS_TERMINATE = 0x0001,
            PROCESS_VM_OPERATION = 0x0008,
            PROCESS_VM_READ = 0x0010,
            PROCESS_VM_WRITE = 0x0020,
            SYNCHRONIZE = 0x00100000
        }

        public enum FILE_INFORMATION_CLASS
        {
            FileDirectoryInformation = 1,
            FileFullDirectoryInformation = 2,
            FileBothDirectoryInformation = 3,
            FileBasicInformation = 4,
            FileStandardInformation = 5,
            FileInternalInformation = 6,
            FileEaInformation = 7,
            FileAccessInformation = 8,
            FileNameInformation = 9,
            FileRenameInformation = 10,
            FileLinkInformation = 11,
            FileNamesInformation = 12,
            FileDispositionInformation = 13,
            FilePositionInformation = 14,
            FileFullEaInformation = 15,
            FileModeInformation = 16,
            FileAlignmentInformation = 17,
            FileAllInformation = 18,
            FileAllocationInformation = 19,
            FileEndOfFileInformation = 20,
            FileAlternateNameInformation = 21,
            FileStreamInformation = 22,
            FilePipeInformation = 23,
            FilePipeLocalInformation = 24,
            FilePipeRemoteInformation = 25,
            FileMailslotQueryInformation = 26,
            FileMailslotSetInformation = 27,
            FileCompressionInformation = 28,
            FileObjectIdInformation = 29,
            FileCompletionInformation = 30,
            FileMoveClusterInformation = 31,
            FileQuotaInformation = 32,
            FileReparsePointInformation = 33,
            FileNetworkOpenInformation = 34,
            FileAttributeTagInformation = 35,
            FileTrackingInformation = 36,
            FileIdBothDirectoryInformation = 37,
            FileIdFullDirectoryInformation = 38,
            FileValidDataLengthInformation = 39,
            FileShortNameInformation = 40,
            FileIoCompletionNotificationInformation = 41,
            FileIoStatusBlockRangeInformation = 42,
            FileIoPriorityHintInformation = 43,
            FileSfioReserveInformation = 44,
            FileSfioVolumeInformation = 45,
            FileHardLinkInformation = 46,
            FileProcessIdsUsingFileInformation = 47,
            FileNormalizedNameInformation = 48,
            FileNetworkPhysicalNameInformation = 49,
            FileMaximumInformation = 50
        }
    }
}
