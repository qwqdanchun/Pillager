using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Pillager.Helper
{
    public sealed partial class NtfsReader : IDisposable
    {
        public NtfsReader(DriveInfo driveInfo, RetrieveMode retrieveMode)
        {
            if (driveInfo == null)
                throw new ArgumentNullException("driveInfo");

            _driveInfo = driveInfo;
            _retrieveMode = retrieveMode;

            StringBuilder builder = new StringBuilder(1024);
            GetVolumeNameForVolumeMountPoint(_driveInfo.RootDirectory.Name, builder, builder.Capacity);

            string volume = builder.ToString().TrimEnd(new char[] { '\\' });

            _volumeHandle =
                CreateFile(
                    volume,
                    FileAccess.Read,
                    FileShare.All,
                    IntPtr.Zero,
                    FileMode.Open,
                    0,
                    IntPtr.Zero
                    );

            if (_volumeHandle == null || _volumeHandle.IsInvalid)
                throw new IOException(
                    string.Format(
                        "Unable to open volume {0}. Make sure it exists and that you have Administrator privileges.",
                        driveInfo
                    )
                );

            using (_volumeHandle)
            {
                InitializeDiskInfo();

                _nodes = ProcessMft();
            }

            _nameIndex = null;
            _volumeHandle = null;

            GC.Collect();
        }

        public IDiskInfo DiskInfo
        {
            get { return _diskInfo; }
        }

        public List<INode> GetNodes(string rootPath)
        {
            List<INode> nodes = new List<INode>();

            UInt32 nodeCount = (UInt32)_nodes.Length;
            for (UInt32 i = 0; i < nodeCount; ++i)
                if (_nodes[i].NameIndex != 0 && GetNodeFullNameCore(i).StartsWith(rootPath, StringComparison.InvariantCultureIgnoreCase))
                    nodes.Add(new NodeWrapper(this, i, _nodes[i]));

            return nodes;
        }

        public byte[] GetVolumeBitmap()
        {
            return _bitmapData;
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_volumeHandle != null)
            {
                _volumeHandle.Dispose();
                _volumeHandle = null;
            }
        }

        #endregion
        private string GetNodeFullNameCore(UInt32 nodeIndex)
        {
            UInt32 node = nodeIndex;

            Stack<UInt32> fullPathNodes = new Stack<UInt32>();
            fullPathNodes.Push(node);

            UInt32 lastNode = node;
            while (true)
            {
                UInt32 parent = _nodes[node].ParentNodeIndex;

                if (parent == ROOTDIRECTORY)
                    break;

                if (parent == lastNode)
                    throw new InvalidDataException("Detected a loop in the tree structure.");

                fullPathNodes.Push(parent);

                lastNode = node;
                node = parent;
            }

            StringBuilder fullPath = new StringBuilder();
            fullPath.Append(_driveInfo.Name.TrimEnd(new char[] { '\\' }));

            while (fullPathNodes.Count > 0)
            {
                node = fullPathNodes.Pop();

                fullPath.Append(@"\");
                fullPath.Append(GetNameFromIndex(_nodes[node].NameIndex));
            }

            return fullPath.ToString();
        }
        [DllImport("kernel32", CharSet = CharSet.Auto, BestFitMapping = false)]
        private static extern bool GetVolumeNameForVolumeMountPoint(String volumeName, StringBuilder uniqueVolumeName, int uniqueNameBufferCapacity);

        [DllImport("kernel32", CharSet = CharSet.Auto, BestFitMapping = false)]
        private static extern SafeFileHandle CreateFile(string lpFileName, FileAccess fileAccess, FileShare fileShare, IntPtr lpSecurityAttributes, FileMode fileMode, int dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32", CharSet = CharSet.Auto)]
        private static extern bool ReadFile(SafeFileHandle hFile, IntPtr lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, ref NativeOverlapped lpOverlapped);

        [Serializable]
        private enum FileMode : int
        {
            Append = 6,
            Create = 2,
            CreateNew = 1,
            Open = 3,
            OpenOrCreate = 4,
            Truncate = 5
        }

        [Serializable, Flags]
        private enum FileShare : int
        {
            None = 0,
            Read = 1,
            Write = 2,
            Delete = 4,
            All = Read | Write | Delete
        }

        [Serializable, Flags]
        private enum FileAccess : int
        {
            Read = 1,
            ReadWrite = 3,
            Write = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeOverlapped
        {
            public IntPtr privateLow;
            public IntPtr privateHigh;
            public UInt64 Offset;
            public IntPtr EventHandle;

            public NativeOverlapped(UInt64 offset)
            {
                Offset = offset;
                EventHandle = IntPtr.Zero;
                privateLow = IntPtr.Zero;
                privateHigh = IntPtr.Zero;
            }
        }
        #region Ntfs Structures

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct BootSector
        {
            fixed byte AlignmentOrReserved1[3];
            public UInt64 Signature;
            public UInt16 BytesPerSector;
            public byte SectorsPerCluster;
            fixed byte AlignmentOrReserved2[26];
            public UInt64 TotalSectors;
            public UInt64 MftStartLcn;
            public UInt64 Mft2StartLcn;
            public UInt32 ClustersPerMftRecord;
            public UInt32 ClustersPerIndexRecord;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct VolumeData
        {
            public UInt64 VolumeSerialNumber;
            public UInt64 NumberSectors;
            public UInt64 TotalClusters;
            public UInt64 FreeClusters;
            public UInt64 TotalReserved;
            public UInt32 BytesPerSector;
            public UInt32 BytesPerCluster;
            public UInt32 BytesPerFileRecordSegment;
            public UInt32 ClustersPerFileRecordSegment;
            public UInt64 MftValidDataLength;
            public UInt64 MftStartLcn;
            public UInt64 Mft2StartLcn;
            public UInt64 MftZoneStart;
            public UInt64 MftZoneEnd;
        }

        private enum RecordType : uint
        {
            File = 0x454c4946,  //'FILE' in ASCII
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct RecordHeader
        {
            public RecordType Type;                  /* File type, for example 'FILE' */
            public UInt16 UsaOffset;             /* Offset to the Update Sequence Array */
            public UInt16 UsaCount;              /* Size in words of Update Sequence Array */
            public UInt64 Lsn;                   /* $LogFile Sequence Number (LSN) */
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct INodeReference
        {
            public UInt32 InodeNumberLowPart;
            public UInt16 InodeNumberHighPart;
            public UInt16 SequenceNumber;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct FileRecordHeader
        {
            public RecordHeader RecordHeader;
            public UInt16 SequenceNumber;        /* Sequence number */
            public UInt16 LinkCount;             /* Hard link count */
            public UInt16 AttributeOffset;       /* Offset to the first Attribute */
            public UInt16 Flags;                 /* Flags. bit 1 = in use, bit 2 = directory, bit 4 & 8 = unknown. */
            public UInt32 BytesInUse;             /* Real size of the FILE record */
            public UInt32 BytesAllocated;         /* Allocated size of the FILE record */
            public INodeReference BaseFileRecord;     /* File reference to the base FILE record */
            public UInt16 NextAttributeNumber;   /* Next Attribute Id */
            public UInt16 Padding;               /* Align to 4 UCHAR boundary (XP) */
            public UInt32 MFTRecordNumber;        /* Number of this MFT Record (XP) */
            public UInt16 UpdateSeqNum;          /*  */
        };

        private enum AttributeType : uint
        {
            AttributeInvalid = 0x00,         /* Not defined by Windows */
            AttributeStandardInformation = 0x10,
            AttributeAttributeList = 0x20,
            AttributeFileName = 0x30,
            AttributeObjectId = 0x40,
            AttributeSecurityDescriptor = 0x50,
            AttributeVolumeName = 0x60,
            AttributeVolumeInformation = 0x70,
            AttributeData = 0x80,
            AttributeIndexRoot = 0x90,
            AttributeIndexAllocation = 0xA0,
            AttributeBitmap = 0xB0,
            AttributeReparsePoint = 0xC0,         /* Reparse Point = Symbolic link */
            AttributeEAInformation = 0xD0,
            AttributeEA = 0xE0,
            AttributePropertySet = 0xF0,
            AttributeLoggedUtilityStream = 0x100
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Attribute
        {
            public AttributeType AttributeType;
            public UInt32 Length;
            public byte Nonresident;
            public byte NameLength;
            public UInt16 NameOffset;
            public UInt16 Flags;              /* 0x0001 = Compressed, 0x4000 = Encrypted, 0x8000 = Sparse */
            public UInt16 AttributeNumber;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct AttributeList
        {
            public AttributeType AttributeType;
            public UInt16 Length;
            public byte NameLength;
            public byte NameOffset;
            public UInt64 LowestVcn;
            public INodeReference FileReferenceNumber;
            public UInt16 Instance;
            public fixed UInt16 AlignmentOrReserved[3];
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct AttributeFileName
        {
            public INodeReference ParentDirectory;
            public UInt64 CreationTime;
            public UInt64 ChangeTime;
            public UInt64 LastWriteTime;
            public UInt64 LastAccessTime;
            public UInt64 AllocatedSize;
            public UInt64 DataSize;
            public UInt32 FileAttributes;
            public UInt32 AlignmentOrReserved;
            public byte NameLength;
            public byte NameType;                 /* NTFS=0x01, DOS=0x02 */
            public char Name;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct AttributeStandardInformation
        {
            public UInt64 CreationTime;
            public UInt64 FileChangeTime;
            public UInt64 MftChangeTime;
            public UInt64 LastAccessTime;
            public UInt32 FileAttributes;       /* READ_ONLY=0x01, HIDDEN=0x02, SYSTEM=0x04, VOLUME_ID=0x08, ARCHIVE=0x20, DEVICE=0x40 */
            public UInt32 MaximumVersions;
            public UInt32 VersionNumber;
            public UInt32 ClassId;
            public UInt32 OwnerId;                        // NTFS 3.0 only
            public UInt32 SecurityId;                     // NTFS 3.0 only
            public UInt64 QuotaCharge;                // NTFS 3.0 only
            public UInt64 Usn;                              // NTFS 3.0 only
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ResidentAttribute
        {
            public Attribute Attribute;
            public UInt32 ValueLength;
            public UInt16 ValueOffset;
            public UInt16 Flags;               // 0x0001 = Indexed
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct NonResidentAttribute
        {
            public Attribute Attribute;
            public UInt64 StartingVcn;
            public UInt64 LastVcn;
            public UInt16 RunArrayOffset;
            public byte CompressionUnit;
            public fixed byte AlignmentOrReserved[5];
            public UInt64 AllocatedSize;
            public UInt64 DataSize;
            public UInt64 InitializedSize;
            public UInt64 CompressedSize;    // Only when compressed
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Fragment
        {
            public UInt64 Lcn;                // Logical cluster number, location on disk.
            public UInt64 NextVcn;            // Virtual cluster number of next fragment.

            public Fragment(UInt64 lcn, UInt64 nextVcn)
            {
                Lcn = lcn;
                NextVcn = nextVcn;
            }
        }

        #endregion

        #region Private Classes

        private sealed class Stream
        {
            public UInt64 Clusters;                      // Total number of clusters.
            public UInt64 Size;                          // Total number of bytes.
            public AttributeType Type;
            public int NameIndex;
            public List<Fragment> _fragments;

            public Stream(int nameIndex, AttributeType type, UInt64 size)
            {
                NameIndex = nameIndex;
                Type = type;
                Size = size;
            }

            public List<Fragment> Fragments
            {
                get
                {
                    if (_fragments == null)
                        _fragments = new List<Fragment>(5);

                    return _fragments;
                }
            }
        }

        /// <summary>
        /// Node struct for file and directory entries
        /// </summary>
        /// <remarks>
        /// We keep this as small as possible to reduce footprint for large volume.
        /// </remarks>
        private struct Node
        {
            public Attributes Attributes;
            public UInt32 ParentNodeIndex;
            public UInt64 Size;
            public int NameIndex;
        }

        /// <summary>
        /// Contains extra information not required for basic purposes.
        /// </summary>
        private struct StandardInformation
        {
            public UInt64 CreationTime;
            public UInt64 LastAccessTime;
            public UInt64 LastChangeTime;

            public StandardInformation(
                UInt64 creationTime,
                UInt64 lastAccessTime,
                UInt64 lastChangeTime
                )
            {
                CreationTime = creationTime;
                LastAccessTime = lastAccessTime;
                LastChangeTime = lastChangeTime;
            }
        }

        /// <summary>
        /// Add some functionality to the basic stream
        /// </summary>
        private sealed class FragmentWrapper : IFragment
        {
            StreamWrapper _owner;
            Fragment _fragment;

            public FragmentWrapper(StreamWrapper owner, Fragment fragment)
            {
                _owner = owner;
                _fragment = fragment;
            }

            #region IFragment Members

            public ulong Lcn
            {
                get { return _fragment.Lcn; }
            }

            public ulong NextVcn
            {
                get { return _fragment.NextVcn; }
            }

            #endregion
        }

        /// <summary>
        /// Add some functionality to the basic stream
        /// </summary>
        private sealed class StreamWrapper : IStream
        {
            NtfsReader _reader;
            NodeWrapper _parentNode;
            int _streamIndex;

            public StreamWrapper(NtfsReader reader, NodeWrapper parentNode, int streamIndex)
            {
                _reader = reader;
                _parentNode = parentNode;
                _streamIndex = streamIndex;
            }

            #region IStream Members

            public string Name
            {
                get
                {
                    return _reader.GetNameFromIndex(_reader._streams[_parentNode.NodeIndex][_streamIndex].NameIndex);
                }
            }

            public UInt64 Size
            {
                get
                {
                    return _reader._streams[_parentNode.NodeIndex][_streamIndex].Size;
                }
            }

            public IList<IFragment> Fragments
            {
                get
                {
                    //if ((_reader._retrieveMode & RetrieveMode.Fragments) != RetrieveMode.Fragments)
                    //    throw new NotSupportedException("The fragments haven't been retrieved. Make sure to use the proper RetrieveMode.");

                    IList<Fragment> fragments =
                        _reader._streams[_parentNode.NodeIndex][_streamIndex].Fragments;

                    if (fragments == null || fragments.Count == 0)
                        return null;

                    List<IFragment> newFragments = new List<IFragment>();
                    foreach (Fragment fragment in fragments)
                        newFragments.Add(new FragmentWrapper(this, fragment));

                    return newFragments;
                }
            }

            #endregion
        }

        /// <summary>
        /// Add some functionality to the basic node
        /// </summary>
        private sealed class NodeWrapper : INode
        {
            NtfsReader _reader;
            UInt32 _nodeIndex;
            Node _node;
            string _fullName;

            public NodeWrapper(NtfsReader reader, UInt32 nodeIndex, Node node)
            {
                _reader = reader;
                _nodeIndex = nodeIndex;
                _node = node;
            }

            public UInt32 NodeIndex
            {
                get { return _nodeIndex; }
            }

            public UInt32 ParentNodeIndex
            {
                get { return _node.ParentNodeIndex; }
            }

            public Attributes Attributes
            {
                get { return _node.Attributes; }
            }

            public string Name
            {
                get { return _reader.GetNameFromIndex(_node.NameIndex); }
            }

            public UInt64 Size
            {
                get { return _node.Size; }
            }

            public string FullName
            {
                get
                {
                    if (_fullName == null)
                        _fullName = _reader.GetNodeFullNameCore(_nodeIndex);

                    return _fullName;
                }
            }

            public IList<IStream> Streams
            {
                get
                {
                    if (_reader._streams == null)
                        throw new NotSupportedException("The streams haven't been retrieved. Make sure to use the proper RetrieveMode.");

                    Stream[] streams = _reader._streams[_nodeIndex];
                    if (streams == null)
                        return null;

                    List<IStream> newStreams = new List<IStream>();
                    for (int i = 0; i < streams.Length; ++i)
                        newStreams.Add(new StreamWrapper(_reader, this, i));

                    return newStreams;
                }
            }

            #region INode Members

            public DateTime CreationTime
            {
                get
                {
                    if (_reader._standardInformations == null)
                        throw new NotSupportedException("The StandardInformation haven't been retrieved. Make sure to use the proper RetrieveMode.");

                    return DateTime.FromFileTimeUtc((Int64)_reader._standardInformations[_nodeIndex].CreationTime);
                }
            }

            public DateTime LastChangeTime
            {
                get
                {
                    if (_reader._standardInformations == null)
                        throw new NotSupportedException("The StandardInformation haven't been retrieved. Make sure to use the proper RetrieveMode.");

                    return DateTime.FromFileTimeUtc((Int64)_reader._standardInformations[_nodeIndex].LastChangeTime);
                }
            }

            public DateTime LastAccessTime
            {
                get
                {
                    if (_reader._standardInformations == null)
                        throw new NotSupportedException("The StandardInformation haven't been retrieved. Make sure to use the proper RetrieveMode.");

                    return DateTime.FromFileTimeUtc((Int64)_reader._standardInformations[_nodeIndex].LastAccessTime);
                }
            }

            #endregion
        }

        /// <summary>
        /// Simple structure of available disk informations.
        /// </summary>
        private sealed class DiskInfoWrapper : IDiskInfo
        {
            public UInt16 BytesPerSector;
            public byte SectorsPerCluster;
            public UInt64 TotalSectors;
            public UInt64 MftStartLcn;
            public UInt64 Mft2StartLcn;
            public UInt32 ClustersPerMftRecord;
            public UInt32 ClustersPerIndexRecord;
            public UInt64 BytesPerMftRecord;
            public UInt64 BytesPerCluster;
            public UInt64 TotalClusters;

            #region IDiskInfo Members

            ushort IDiskInfo.BytesPerSector
            {
                get { return BytesPerSector; }
            }

            byte IDiskInfo.SectorsPerCluster
            {
                get { return SectorsPerCluster; }
            }

            ulong IDiskInfo.TotalSectors
            {
                get { return TotalSectors; }
            }

            ulong IDiskInfo.MftStartLcn
            {
                get { return MftStartLcn; }
            }

            ulong IDiskInfo.Mft2StartLcn
            {
                get { return Mft2StartLcn; }
            }

            uint IDiskInfo.ClustersPerMftRecord
            {
                get { return ClustersPerMftRecord; }
            }

            uint IDiskInfo.ClustersPerIndexRecord
            {
                get { return ClustersPerIndexRecord; }
            }

            ulong IDiskInfo.BytesPerMftRecord
            {
                get { return BytesPerMftRecord; }
            }

            ulong IDiskInfo.BytesPerCluster
            {
                get { return BytesPerCluster; }
            }

            ulong IDiskInfo.TotalClusters
            {
                get { return TotalClusters; }
            }

            #endregion
        }

        #endregion

        #region Constants

        private const UInt64 VIRTUALFRAGMENT = 18446744073709551615; // _UI64_MAX - 1 */
        private const UInt32 ROOTDIRECTORY = 5;

        private readonly byte[] BitmapMasks = new byte[] { 1, 2, 4, 8, 16, 32, 64, 128 };

        #endregion

        SafeFileHandle _volumeHandle;
        DiskInfoWrapper _diskInfo;
        Node[] _nodes;
        StandardInformation[] _standardInformations;
        Stream[][] _streams;
        DriveInfo _driveInfo;
        List<string> _names = new List<string>();
        RetrieveMode _retrieveMode;
        byte[] _bitmapData;

        //preallocate a lot of space for the strings to avoid too much dictionary resizing
        //use ordinal comparison to improve performance
        //this will be deallocated once the MFT reading is finished
        Dictionary<string, int> _nameIndex = new Dictionary<string, int>(128 * 1024, StringComparer.Ordinal);

        #region Events

        /// <summary>
        /// Raised once the bitmap data has been read.
        /// </summary>
        public event EventHandler BitmapDataAvailable;

        private void OnBitmapDataAvailable()
        {
            if (BitmapDataAvailable != null)
                BitmapDataAvailable(this, EventArgs.Empty);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Allocate or retrieve an existing index for the particular string.
        /// </summary>
        ///<remarks>
        /// In order to mimize memory usage, we reuse string as much as possible.
        ///</remarks>
        private int GetNameIndex(string name)
        {
            int existingIndex;
            if (_nameIndex.TryGetValue(name, out existingIndex))
                return existingIndex;

            _names.Add(name);
            _nameIndex[name] = _names.Count - 1;

            return _names.Count - 1;
        }

        /// <summary>
        /// Get the string from our stringtable from the given index.
        /// </summary>
        private string GetNameFromIndex(int nameIndex)
        {
            return nameIndex == 0 ? null : _names[nameIndex];
        }

        private Stream SearchStream(List<Stream> streams, AttributeType streamType)
        {
            //since the number of stream is usually small, we can afford O(n)
            foreach (Stream stream in streams)
                if (stream.Type == streamType)
                    return stream;

            return null;
        }

        private Stream SearchStream(List<Stream> streams, AttributeType streamType, int streamNameIndex)
        {
            //since the number of stream is usually small, we can afford O(n)
            foreach (Stream stream in streams)
                if (stream.Type == streamType &&
                    stream.NameIndex == streamNameIndex)
                    return stream;

            return null;
        }

        #endregion

        #region File Reading Wrappers

        private unsafe void ReadFile(byte* buffer, int len, UInt64 absolutePosition)
        {
            ReadFile(buffer, (UInt64)len, absolutePosition);
        }

        private unsafe void ReadFile(byte* buffer, UInt32 len, UInt64 absolutePosition)
        {
            ReadFile(buffer, (UInt64)len, absolutePosition);
        }

        private unsafe void ReadFile(byte* buffer, UInt64 len, UInt64 absolutePosition)
        {
            NativeOverlapped overlapped = new NativeOverlapped(absolutePosition);

            uint read;
            if (!ReadFile(_volumeHandle, (IntPtr)buffer, (uint)len, out read, ref overlapped))
                throw new Exception("Unable to read volume information");

            if (read != (uint)len)
                throw new Exception("Unable to read volume information");
        }

        #endregion

        #region Ntfs Interpretor

        /// <summary>
        /// Read the next contiguous block of information on disk
        /// </summary>
        private unsafe bool ReadNextChunk(
            byte* buffer,
            UInt32 bufferSize,
            UInt32 nodeIndex,
            int fragmentIndex,
            Stream dataStream,
            ref UInt64 BlockStart,
            ref UInt64 BlockEnd,
            ref UInt64 Vcn,
            ref UInt64 RealVcn
            )
        {
            BlockStart = nodeIndex;
            BlockEnd = BlockStart + bufferSize / _diskInfo.BytesPerMftRecord;
            if (BlockEnd > dataStream.Size * 8)
                BlockEnd = dataStream.Size * 8;

            UInt64 u1 = 0;

            int fragmentCount = dataStream.Fragments.Count;
            while (fragmentIndex < fragmentCount)
            {
                Fragment fragment = dataStream.Fragments[fragmentIndex];

                /* Calculate Inode at the end of the fragment. */
                u1 = (RealVcn + fragment.NextVcn - Vcn) * _diskInfo.BytesPerSector * _diskInfo.SectorsPerCluster / _diskInfo.BytesPerMftRecord;

                if (u1 > nodeIndex)
                    break;

                do
                {
                    if (fragment.Lcn != VIRTUALFRAGMENT)
                        RealVcn = RealVcn + fragment.NextVcn - Vcn;

                    Vcn = fragment.NextVcn;

                    if (++fragmentIndex >= fragmentCount)
                        break;

                } while (fragment.Lcn == VIRTUALFRAGMENT);
            }

            if (fragmentIndex >= fragmentCount)
                return false;

            if (BlockEnd >= u1)
                BlockEnd = u1;

            ulong position =
                (dataStream.Fragments[fragmentIndex].Lcn - RealVcn) * _diskInfo.BytesPerSector *
                    _diskInfo.SectorsPerCluster + BlockStart * _diskInfo.BytesPerMftRecord;

            ReadFile(buffer, (BlockEnd - BlockStart) * _diskInfo.BytesPerMftRecord, position);

            return true;
        }

        /// <summary>
        /// Gather basic disk information we need to interpret data
        /// </summary>
        private unsafe void InitializeDiskInfo()
        {
            byte[] volumeData = new byte[512];

            fixed (byte* ptr = volumeData)
            {
                ReadFile(ptr, volumeData.Length, 0);

                BootSector* bootSector = (BootSector*)ptr;

                if (bootSector->Signature != 0x202020205346544E)
                    throw new Exception("This is not an NTFS disk.");

                DiskInfoWrapper diskInfo = new DiskInfoWrapper();
                diskInfo.BytesPerSector = bootSector->BytesPerSector;
                diskInfo.SectorsPerCluster = bootSector->SectorsPerCluster;
                diskInfo.TotalSectors = bootSector->TotalSectors;
                diskInfo.MftStartLcn = bootSector->MftStartLcn;
                diskInfo.Mft2StartLcn = bootSector->Mft2StartLcn;
                diskInfo.ClustersPerMftRecord = bootSector->ClustersPerMftRecord;
                diskInfo.ClustersPerIndexRecord = bootSector->ClustersPerIndexRecord;

                if (bootSector->ClustersPerMftRecord >= 128)
                    diskInfo.BytesPerMftRecord = ((ulong)1 << (byte)(256 - (byte)bootSector->ClustersPerMftRecord));
                else
                    diskInfo.BytesPerMftRecord = diskInfo.ClustersPerMftRecord * diskInfo.BytesPerSector * diskInfo.SectorsPerCluster;

                diskInfo.BytesPerCluster = (UInt64)diskInfo.BytesPerSector * (UInt64)diskInfo.SectorsPerCluster;

                if (diskInfo.SectorsPerCluster > 0)
                    diskInfo.TotalClusters = diskInfo.TotalSectors / diskInfo.SectorsPerCluster;

                _diskInfo = diskInfo;
            }
        }

        /// <summary>
        /// Used to check/adjust data before we begin to interpret it
        /// </summary>
        private unsafe void FixupRawMftdata(byte* buffer, UInt64 len)
        {
            FileRecordHeader* ntfsFileRecordHeader = (FileRecordHeader*)buffer;

            if (ntfsFileRecordHeader->RecordHeader.Type != RecordType.File)
                return;

            UInt16* wordBuffer = (UInt16*)buffer;

            UInt16* UpdateSequenceArray = (UInt16*)(buffer + ntfsFileRecordHeader->RecordHeader.UsaOffset);
            UInt32 increment = (UInt32)_diskInfo.BytesPerSector / sizeof(UInt16);

            UInt32 Index = increment - 1;

            for (int i = 1; i < ntfsFileRecordHeader->RecordHeader.UsaCount; i++)
            {
                /* Check if we are inside the buffer. */
                if (Index * sizeof(UInt16) >= len)
                    throw new Exception("USA data indicates that data is missing, the MFT may be corrupt.");

                // Check if the last 2 bytes of the sector contain the Update Sequence Number.
                if (wordBuffer[Index] != UpdateSequenceArray[0])
                    throw new Exception("USA fixup word is not equal to the Update Sequence Number, the MFT may be corrupt.");

                /* Replace the last 2 bytes in the sector with the value from the Usa array. */
                wordBuffer[Index] = UpdateSequenceArray[i];
                Index = Index + increment;
            }
        }

        /// <summary>
        /// Decode the RunLength value.
        /// </summary>
        private static unsafe Int64 ProcessRunLength(byte* runData, UInt32 runDataLength, Int32 runLengthSize, ref UInt32 index)
        {
            Int64 runLength = 0;
            byte* runLengthBytes = (byte*)&runLength;
            for (int i = 0; i < runLengthSize; i++)
            {
                runLengthBytes[i] = runData[index];
                if (++index >= runDataLength)
                    throw new Exception("Datarun is longer than buffer, the MFT may be corrupt.");
            }
            return runLength;
        }

        /// <summary>
        /// Decode the RunOffset value.
        /// </summary>
        private static unsafe Int64 ProcessRunOffset(byte* runData, UInt32 runDataLength, Int32 runOffsetSize, ref UInt32 index)
        {
            Int64 runOffset = 0;
            byte* runOffsetBytes = (byte*)&runOffset;

            int i;
            for (i = 0; i < runOffsetSize; i++)
            {
                runOffsetBytes[i] = runData[index];
                if (++index >= runDataLength)
                    throw new Exception("Datarun is longer than buffer, the MFT may be corrupt.");
            }

            //process negative values
            if (runOffsetBytes[i - 1] >= 0x80)
                while (i < 8)
                    runOffsetBytes[i++] = 0xFF;

            return runOffset;
        }

        /// <summary>
        /// Read the data that is specified in a RunData list from disk into memory,
        /// skipping the first Offset bytes.
        /// </summary>
        private unsafe byte[] ProcessNonResidentData(
            byte* RunData,
            UInt32 RunDataLength,
            UInt64 Offset,         /* Bytes to skip from begin of data. */
            UInt64 WantedLength    /* Number of bytes to read. */
            )
        {
            /* Sanity check. */
            if (RunData == null || RunDataLength == 0)
                throw new Exception("nothing to read");

            if (WantedLength >= UInt32.MaxValue)
                throw new Exception("too many bytes to read");

            /* We have to round up the WantedLength to the nearest sector. For some
               reason or other Microsoft has decided that raw reading from disk can
               only be done by whole sector, even though ReadFile() accepts it's
               parameters in bytes. */
            if (WantedLength % _diskInfo.BytesPerSector > 0)
                WantedLength += _diskInfo.BytesPerSector - (WantedLength % _diskInfo.BytesPerSector);

            /* Walk through the RunData and read the requested data from disk. */
            UInt32 Index = 0;
            Int64 Lcn = 0;
            Int64 Vcn = 0;

            byte[] buffer = new byte[WantedLength];

            fixed (byte* bufPtr = buffer)
            {
                while (RunData[Index] != 0)
                {
                    /* Decode the RunData and calculate the next Lcn. */
                    Int32 RunLengthSize = (RunData[Index] & 0x0F);
                    Int32 RunOffsetSize = ((RunData[Index] & 0xF0) >> 4);

                    if (++Index >= RunDataLength)
                        throw new Exception("Error: datarun is longer than buffer, the MFT may be corrupt.");

                    Int64 RunLength =
                        ProcessRunLength(RunData, RunDataLength, RunLengthSize, ref Index);

                    Int64 RunOffset =
                        ProcessRunOffset(RunData, RunDataLength, RunOffsetSize, ref Index);

                    // Ignore virtual extents.
                    if (RunOffset == 0 || RunLength == 0)
                        continue;

                    Lcn += RunOffset;
                    Vcn += RunLength;

                    /* Determine how many and which bytes we want to read. If we don't need
                       any bytes from this extent then loop. */
                    UInt64 ExtentVcn = (UInt64)((Vcn - RunLength) * _diskInfo.BytesPerSector * _diskInfo.SectorsPerCluster);
                    UInt64 ExtentLcn = (UInt64)(Lcn * _diskInfo.BytesPerSector * _diskInfo.SectorsPerCluster);
                    UInt64 ExtentLength = (UInt64)(RunLength * _diskInfo.BytesPerSector * _diskInfo.SectorsPerCluster);

                    if (Offset >= ExtentVcn + ExtentLength)
                        continue;

                    if (Offset > ExtentVcn)
                    {
                        ExtentLcn = ExtentLcn + Offset - ExtentVcn;
                        ExtentLength = ExtentLength - (Offset - ExtentVcn);
                        ExtentVcn = Offset;
                    }

                    if (Offset + WantedLength <= ExtentVcn)
                        continue;

                    if (Offset + WantedLength < ExtentVcn + ExtentLength)
                        ExtentLength = Offset + WantedLength - ExtentVcn;

                    if (ExtentLength == 0)
                        continue;

                    ReadFile(bufPtr + ExtentVcn - Offset, ExtentLength, ExtentLcn);
                }
            }

            return buffer;
        }

        /// <summary>
        /// Process each attributes and gather information when necessary
        /// </summary>
        private unsafe void ProcessAttributes(ref Node node, UInt32 nodeIndex, byte* ptr, UInt64 BufLength, UInt16 instance, int depth, List<Stream> streams, bool isMftNode)
        {
            Attribute* attribute = null;
            for (uint AttributeOffset = 0; AttributeOffset < BufLength; AttributeOffset = AttributeOffset + attribute->Length)
            {
                attribute = (Attribute*)(ptr + AttributeOffset);

                // exit the loop if end-marker.
                if ((AttributeOffset + 4 <= BufLength) &&
                    (*(UInt32*)attribute == 0xFFFFFFFF))
                    break;

                //make sure we did read the data correctly
                if ((AttributeOffset + 4 > BufLength) || attribute->Length < 3 ||
                    (AttributeOffset + attribute->Length > BufLength))
                    throw new Exception("Error: attribute in Inode %I64u is bigger than the data, the MFT may be corrupt.");

                //attributes list needs to be processed at the end
                if (attribute->AttributeType == AttributeType.AttributeAttributeList)
                    continue;

                /* If the Instance does not equal the AttributeNumber then ignore the attribute.
                   This is used when an AttributeList is being processed and we only want a specific
                   instance. */
                if ((instance != 65535) && (instance != attribute->AttributeNumber))
                    continue;

                if (attribute->Nonresident == 0)
                {
                    ResidentAttribute* residentAttribute = (ResidentAttribute*)attribute;

                    switch (attribute->AttributeType)
                    {
                        case AttributeType.AttributeFileName:
                            AttributeFileName* attributeFileName = (AttributeFileName*)(ptr + AttributeOffset + residentAttribute->ValueOffset);

                            if (attributeFileName->ParentDirectory.InodeNumberHighPart > 0)
                                throw new NotSupportedException("48 bits inode are not supported to reduce memory footprint.");

                            //node.ParentNodeIndex = ((UInt64)attributeFileName->ParentDirectory.InodeNumberHighPart << 32) + attributeFileName->ParentDirectory.InodeNumberLowPart;
                            node.ParentNodeIndex = attributeFileName->ParentDirectory.InodeNumberLowPart;

                            if (attributeFileName->NameType == 1 || node.NameIndex == 0)
                                node.NameIndex = GetNameIndex(new string(&attributeFileName->Name, 0, attributeFileName->NameLength));

                            break;

                        case AttributeType.AttributeStandardInformation:
                            AttributeStandardInformation* attributeStandardInformation = (AttributeStandardInformation*)(ptr + AttributeOffset + residentAttribute->ValueOffset);

                            node.Attributes |= (Attributes)attributeStandardInformation->FileAttributes;

                            if ((_retrieveMode & RetrieveMode.StandardInformations) == RetrieveMode.StandardInformations)
                                _standardInformations[nodeIndex] =
                                    new StandardInformation(
                                        attributeStandardInformation->CreationTime,
                                        attributeStandardInformation->FileChangeTime,
                                        attributeStandardInformation->LastAccessTime
                                    );

                            break;

                        case AttributeType.AttributeData:
                            node.Size = residentAttribute->ValueLength;
                            break;
                    }
                }
                else
                {
                    NonResidentAttribute* nonResidentAttribute = (NonResidentAttribute*)attribute;

                    //save the length (number of bytes) of the data.
                    if (attribute->AttributeType == AttributeType.AttributeData && node.Size == 0)
                        node.Size = nonResidentAttribute->DataSize;

                    if (streams != null)
                    {
                        //extract the stream name
                        int streamNameIndex = 0;
                        if (attribute->NameLength > 0)
                            streamNameIndex = GetNameIndex(new string((char*)(ptr + AttributeOffset + attribute->NameOffset), 0, (int)attribute->NameLength));

                        //find or create the stream
                        Stream stream =
                            SearchStream(streams, attribute->AttributeType, streamNameIndex);

                        if (stream == null)
                        {
                            stream = new Stream(streamNameIndex, attribute->AttributeType, nonResidentAttribute->DataSize);
                            streams.Add(stream);
                        }
                        else if (stream.Size == 0)
                            stream.Size = nonResidentAttribute->DataSize;

                        //we need the fragment of the MFTNode so retrieve them this time
                        //even if fragments aren't normally read
                        if (isMftNode || (_retrieveMode & RetrieveMode.Fragments) == RetrieveMode.Fragments)
                            ProcessFragments(
                                ref node,
                                stream,
                                ptr + AttributeOffset + nonResidentAttribute->RunArrayOffset,
                                attribute->Length - nonResidentAttribute->RunArrayOffset,
                                nonResidentAttribute->StartingVcn
                            );
                    }
                }
            }

            if (streams != null && streams.Count > 0)
                node.Size = streams[0].Size;
        }

        /// <summary>
        /// Process fragments for streams
        /// </summary>
        private unsafe void ProcessFragments(
            ref Node node,
            Stream stream,
            byte* runData,
            UInt32 runDataLength,
            UInt64 StartingVcn)
        {
            if (runData == null)
                return;

            /* Walk through the RunData and add the extents. */
            uint index = 0;
            Int64 lcn = 0;
            Int64 vcn = (Int64)StartingVcn;
            int runOffsetSize = 0;
            int runLengthSize = 0;

            while (runData[index] != 0)
            {
                /* Decode the RunData and calculate the next Lcn. */
                runLengthSize = (runData[index] & 0x0F);
                runOffsetSize = ((runData[index] & 0xF0) >> 4);

                if (++index >= runDataLength)
                    throw new Exception("Error: datarun is longer than buffer, the MFT may be corrupt.");

                Int64 runLength =
                    ProcessRunLength(runData, runDataLength, runLengthSize, ref index);

                Int64 runOffset =
                    ProcessRunOffset(runData, runDataLength, runOffsetSize, ref index);

                lcn += runOffset;
                vcn += runLength;

                /* Add the size of the fragment to the total number of clusters.
                   There are two kinds of fragments: real and virtual. The latter do not
                   occupy clusters on disk, but are information used by compressed
                   and sparse files. */
                if (runOffset != 0)
                    stream.Clusters += (UInt64)runLength;

                stream.Fragments.Add(
                    new Fragment(
                        runOffset == 0 ? VIRTUALFRAGMENT : (UInt64)lcn,
                        (UInt64)vcn
                    )
                );
            }
        }

        /// <summary>
        /// Process an actual MFT record from the buffer
        /// </summary>
        private unsafe bool ProcessMftRecord(byte* buffer, UInt64 length, UInt32 nodeIndex, out Node node, List<Stream> streams, bool isMftNode)
        {
            node = new Node();

            FileRecordHeader* ntfsFileRecordHeader = (FileRecordHeader*)buffer;

            if (ntfsFileRecordHeader->RecordHeader.Type != RecordType.File)
                return false;

            //the inode is not in use
            if ((ntfsFileRecordHeader->Flags & 1) != 1)
                return false;

            UInt64 baseInode = ((UInt64)ntfsFileRecordHeader->BaseFileRecord.InodeNumberHighPart << 32) + ntfsFileRecordHeader->BaseFileRecord.InodeNumberLowPart;

            //This is an inode extension used in an AttributeAttributeList of another inode, don't parse it
            if (baseInode != 0)
                return false;

            if (ntfsFileRecordHeader->AttributeOffset >= length)
                throw new Exception("Error: attributes in Inode %I64u are outside the FILE record, the MFT may be corrupt.");

            if (ntfsFileRecordHeader->BytesInUse > length)
                throw new Exception("Error: in Inode %I64u the record is bigger than the size of the buffer, the MFT may be corrupt.");

            //make the file appear in the rootdirectory by default
            node.ParentNodeIndex = ROOTDIRECTORY;

            if ((ntfsFileRecordHeader->Flags & 2) == 2)
                node.Attributes |= Attributes.Directory;

            ProcessAttributes(ref node, nodeIndex, buffer + ntfsFileRecordHeader->AttributeOffset, length - ntfsFileRecordHeader->AttributeOffset, 65535, 0, streams, isMftNode);

            return true;
        }

        /// <summary>
        /// Process the bitmap data that contains information on inode usage.
        /// </summary>
        private unsafe byte[] ProcessBitmapData(List<Stream> streams)
        {
            UInt64 Vcn = 0;
            UInt64 MaxMftBitmapBytes = 0;

            Stream bitmapStream = SearchStream(streams, AttributeType.AttributeBitmap);
            if (bitmapStream == null)
                throw new Exception("No Bitmap Data");

            foreach (Fragment fragment in bitmapStream.Fragments)
            {
                if (fragment.Lcn != VIRTUALFRAGMENT)
                    MaxMftBitmapBytes += (fragment.NextVcn - Vcn) * _diskInfo.BytesPerSector * _diskInfo.SectorsPerCluster;

                Vcn = fragment.NextVcn;
            }

            byte[] bitmapData = new byte[MaxMftBitmapBytes];

            fixed (byte* bitmapDataPtr = bitmapData)
            {
                Vcn = 0;
                UInt64 RealVcn = 0;

                foreach (Fragment fragment in bitmapStream.Fragments)
                {
                    if (fragment.Lcn != VIRTUALFRAGMENT)
                    {
                        ReadFile(
                            bitmapDataPtr + RealVcn * _diskInfo.BytesPerSector * _diskInfo.SectorsPerCluster,
                            (fragment.NextVcn - Vcn) * _diskInfo.BytesPerSector * _diskInfo.SectorsPerCluster,
                            fragment.Lcn * _diskInfo.BytesPerSector * _diskInfo.SectorsPerCluster
                            );

                        RealVcn = RealVcn + fragment.NextVcn - Vcn;
                    }

                    Vcn = fragment.NextVcn;
                }
            }

            return bitmapData;
        }

        /// <summary>
        /// Begin the process of interpreting MFT data
        /// </summary>
        private unsafe Node[] ProcessMft()
        {
            //64 KB seems to be optimal for Windows XP, Vista is happier with 256KB...
            uint bufferSize =
                (Environment.OSVersion.Version.Major >= 6 ? 256u : 64u) * 1024;

            byte[] data = new byte[bufferSize];

            fixed (byte* buffer = data)
            {
                //Read the $MFT record from disk into memory, which is always the first record in the MFT. 
                ReadFile(buffer, _diskInfo.BytesPerMftRecord, _diskInfo.MftStartLcn * _diskInfo.BytesPerSector * _diskInfo.SectorsPerCluster);

                //Fixup the raw data from disk. This will also test if it's a valid $MFT record.
                FixupRawMftdata(buffer, _diskInfo.BytesPerMftRecord);

                List<Stream> mftStreams = new List<Stream>();

                if ((_retrieveMode & RetrieveMode.StandardInformations) == RetrieveMode.StandardInformations)
                    _standardInformations = new StandardInformation[1]; //allocate some space for $MFT record

                Node mftNode;
                if (!ProcessMftRecord(buffer, _diskInfo.BytesPerMftRecord, 0, out mftNode, mftStreams, true))
                    throw new Exception("Can't interpret Mft Record");

                //the bitmap data contains all used inodes on the disk
                _bitmapData =
                    ProcessBitmapData(mftStreams);

                OnBitmapDataAvailable();

                Stream dataStream = SearchStream(mftStreams, AttributeType.AttributeData);

                UInt32 maxInode = (UInt32)_bitmapData.Length * 8;
                if (maxInode > (UInt32)(dataStream.Size / _diskInfo.BytesPerMftRecord))
                    maxInode = (UInt32)(dataStream.Size / _diskInfo.BytesPerMftRecord);

                Node[] nodes = new Node[maxInode];
                nodes[0] = mftNode;

                if ((_retrieveMode & RetrieveMode.StandardInformations) == RetrieveMode.StandardInformations)
                {
                    StandardInformation mftRecordInformation = _standardInformations[0];
                    _standardInformations = new StandardInformation[maxInode];
                    _standardInformations[0] = mftRecordInformation;
                }

                if ((_retrieveMode & RetrieveMode.Streams) == RetrieveMode.Streams)
                    _streams = new Stream[maxInode][];

                /* Read and process all the records in the MFT. The records are read into a
                   buffer and then given one by one to the InterpretMftRecord() subroutine. */

                UInt64 BlockStart = 0, BlockEnd = 0;
                UInt64 RealVcn = 0, Vcn = 0;

                ulong totalBytesRead = 0;
                int fragmentIndex = 0;
                int fragmentCount = dataStream.Fragments.Count;
                for (UInt32 nodeIndex = 1; nodeIndex < maxInode; nodeIndex++)
                {
                    // Ignore the Inode if the bitmap says it's not in use.
                    if ((_bitmapData[nodeIndex >> 3] & BitmapMasks[nodeIndex % 8]) == 0)
                        continue;

                    if (nodeIndex >= BlockEnd)
                    {
                        if (!ReadNextChunk(
                                buffer,
                                bufferSize,
                                nodeIndex,
                                fragmentIndex,
                                dataStream,
                                ref BlockStart,
                                ref BlockEnd,
                                ref Vcn,
                                ref RealVcn))
                            break;

                        totalBytesRead += (BlockEnd - BlockStart) * _diskInfo.BytesPerMftRecord;
                    }

                    FixupRawMftdata(
                            buffer + (nodeIndex - BlockStart) * _diskInfo.BytesPerMftRecord,
                            _diskInfo.BytesPerMftRecord
                        );

                    List<Stream> streams = null;
                    if ((_retrieveMode & RetrieveMode.Streams) == RetrieveMode.Streams)
                        streams = new List<Stream>();

                    Node newNode;
                    if (!ProcessMftRecord(
                            buffer + (nodeIndex - BlockStart) * _diskInfo.BytesPerMftRecord,
                            _diskInfo.BytesPerMftRecord,
                            nodeIndex,
                            out newNode,
                            streams,
                            false))
                        continue;

                    nodes[nodeIndex] = newNode;

                    if (streams != null)
                        _streams[nodeIndex] = streams.ToArray();
                }

                return nodes;
            }
        }

        #endregion
        [Flags]
        public enum RetrieveMode
        {
            Minimal = 0,
            StandardInformations = 1,
            Streams = 2,
            Fragments = 4,
            All = StandardInformations | Streams | Fragments,
        }
        public interface IStream
        {
            string Name { get; }
            UInt64 Size { get; }
            IList<IFragment> Fragments { get; }
        }
        public interface IFragment
        {
            UInt64 Lcn { get; }

            UInt64 NextVcn { get; }
        }
        public interface INode
        {
            Attributes Attributes { get; }
            UInt32 NodeIndex { get; }
            UInt32 ParentNodeIndex { get; }
            string Name { get; }
            UInt64 Size { get; }
            string FullName { get; }
            IList<IStream> Streams { get; }

            DateTime CreationTime { get; }
            DateTime LastChangeTime { get; }
            DateTime LastAccessTime { get; }
        }
        public interface IDiskInfo
        {
            UInt16 BytesPerSector { get; }
            byte SectorsPerCluster { get; }
            UInt64 TotalSectors { get; }
            UInt64 MftStartLcn { get; }
            UInt64 Mft2StartLcn { get; }
            UInt32 ClustersPerMftRecord { get; }
            UInt32 ClustersPerIndexRecord { get; }
            UInt64 BytesPerMftRecord { get; }
            UInt64 BytesPerCluster { get; }
            UInt64 TotalClusters { get; }
        }
        [Flags]
        public enum Attributes : uint
        {
            ReadOnly = 1,
            Hidden = 2,
            System = 4,
            Directory = 16,
            Archive = 32,
            Device = 64,
            Normal = 128,
            Temporary = 256,
            SparseFile = 512,
            ReparsePoint = 1024,
            Compressed = 2048,
            Offline = 4096,
            NotContentIndexed = 8192,
            Encrypted = 16384,
        }
        public static class Algorithms
        {
            public static IDictionary<UInt32, List<INode>> AggregateByFragments(IEnumerable<INode> nodes, UInt32 minimumFragments)
            {
                Dictionary<UInt32, List<INode>> fragmentsAggregate = new Dictionary<UInt32, List<INode>>();

                foreach (INode node in nodes)
                {
                    IList<IStream> streams = node.Streams;
                    if (streams == null || streams.Count == 0)
                        continue;

                    IList<IFragment> fragments = streams[0].Fragments;
                    if (fragments == null)
                        continue;

                    UInt32 fragmentCount = (UInt32)fragments.Count;

                    if (fragmentCount < minimumFragments)
                        continue;

                    List<INode> nodeList;
                    fragmentsAggregate.TryGetValue(fragmentCount, out nodeList);

                    if (nodeList == null)
                    {
                        nodeList = new List<INode>();
                        fragmentsAggregate[fragmentCount] = nodeList;
                    }

                    nodeList.Add(node);
                }

                return fragmentsAggregate;
            }
        }
    }
}
