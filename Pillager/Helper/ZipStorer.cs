// ZipStorer, by Jaime Olivares
// Website: http://github.com/jaime-olivares/zipstorer
// Version: 3.5.0 (May 20, 2019)

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Pillager.Helper
{
    /// <summary>
    /// Unique class for compression/decompression file. Represents a Zip file.
    /// </summary>
    public class ZipStorer : IDisposable
    {
        /// <summary>
        /// Compression method enumeration
        /// </summary>
        public enum Compression : ushort
        {
            /// <summary>Uncompressed storage</summary> 
            Store = 0,
            /// <summary>Deflate compression method</summary>
            Deflate = 8
        }

        /// <summary>
        /// Represents an entry in Zip file directory
        /// </summary>
        public class ZipFileEntry
        {
            /// <summary>Compression method</summary>
            public Compression Method;
            /// <summary>Full path and filename as stored in Zip</summary>
            public string FilenameInZip;
            /// <summary>Original file size</summary>
            public uint FileSize;
            /// <summary>Compressed file size</summary>
            public uint CompressedSize;
            /// <summary>Offset of header information inside Zip storage</summary>
            public uint HeaderOffset;
            /// <summary>Offset of file inside Zip storage</summary>
            public uint FileOffset;
            /// <summary>Size of header information</summary>
            public uint HeaderSize;
            /// <summary>32-bit checksum of entire file</summary>
            public uint Crc32;
            /// <summary>Last modification time of file</summary>
            public DateTime ModifyTime;
            /// <summary>Creation time of file</summary>
            public DateTime CreationTime;
            /// <summary>Last access time of file</summary>
            public DateTime AccessTime;
            /// <summary>User comment for file</summary>
            public string Comment;
            /// <summary>True if UTF8 encoding for filename and comments, false if default (CP 437)</summary>
            public bool EncodeUTF8;

            /// <summary>Overriden method</summary>
            /// <returns>Filename in Zip</returns>
            public override string ToString()
            {
                return FilenameInZip;
            }
        }

        #region Public fields
        /// <summary>True if UTF8 encoding for filename and comments, false if default (CP 437)</summary>
        public bool EncodeUTF8 = true;
        /// <summary>Force deflate algotithm even if it inflates the stored file. Off by default.</summary>
        public bool ForceDeflating = false;
        #endregion

        #region Private fields
        // List of files to store
        private List<ZipFileEntry> Files = new List<ZipFileEntry>();
        // Filename of storage file
        private string FileName;
        // Stream object of storage file
        private Stream ZipFileStream;
        // General comment
        private string Comment = string.Empty;
        // Central dir image
        private byte[] CentralDirImage;
        // Existing files in zip
        private ushort ExistingFiles;
        // File access for Open method
        private FileAccess Access;
        // leave the stream open after the ZipStorer object is disposed
        private bool leaveOpen;
        // Static CRC32 Table
        private static uint[] CrcTable;
        // Default filename encoder
        private static Encoding DefaultEncoding = Encoding.GetEncoding(437);
        #endregion

        #region Public methods
        // Static constructor. Just invoked once in order to create the CRC32 lookup table.
        static ZipStorer()
        {
            // Generate CRC32 table
            CrcTable = new uint[256];
            for (int i = 0; i < CrcTable.Length; i++)
            {
                uint c = (uint)i;
                for (int j = 0; j < 8; j++)
                {
                    if ((c & 1) != 0)
                        c = 3988292384 ^ (c >> 1);
                    else
                        c >>= 1;
                }
                CrcTable[i] = c;
            }
        }
        /// <summary>
        /// Method to create a new storage file
        /// </summary>
        /// <param name="_filename">Full path of Zip file to create</param>
        /// <param name="_comment">General comment for Zip file</param>
        /// <returns>A valid ZipStorer object</returns>
        public static ZipStorer Create(string _filename, string _comment = null)
        {
            Stream stream = new FileStream(_filename, FileMode.Create, FileAccess.ReadWrite);

            ZipStorer zip = Create(stream, _comment);
            zip.Comment = _comment ?? string.Empty;
            zip.FileName = _filename;

            return zip;
        }
        /// <summary>
        /// Method to create a new zip storage in a stream
        /// </summary>
        /// <param name="_stream"></param>
        /// <param name="_comment"></param>
        /// <param name="_leaveOpen">true to leave the stream open after the ZipStorer object is disposed; otherwise, false (default).</param>
        /// <returns>A valid ZipStorer object</returns>
        public static ZipStorer Create(Stream _stream, string _comment = null, bool _leaveOpen = false)
        {
            ZipStorer zip = new ZipStorer();
            zip.Comment = _comment ?? string.Empty;
            zip.ZipFileStream = _stream;
            zip.Access = FileAccess.Write;
            zip.leaveOpen = _leaveOpen;
            return zip;
        }
        /// <summary>
        /// Add full contents of a file into the Zip storage
        /// </summary>
        /// <param name="_method">Compression method</param>
        /// <param name="_pathname">Full path of file to add to Zip storage</param>
        /// <param name="_filenameInZip">Filename and path as desired in Zip directory</param>
        /// <param name="_comment">Comment for stored file</param>        
        public ZipFileEntry AddFile(Compression _method, string _pathname, string _filenameInZip, string _comment = null)
        {
            if (Access == FileAccess.Read)
                throw new InvalidOperationException("Writing is not alowed");

            using (var stream = new FileStream(_pathname, FileMode.Open, FileAccess.Read))
            {
                return AddStream(_method, _filenameInZip, stream, File.GetLastWriteTime(_pathname), _comment);
            }
        }
        /// <summary>
        /// Add full contents of a stream into the Zip storage
        /// </summary>
        /// <remarks>Same parameters and return value as AddStreamAsync()</remarks>
        public ZipFileEntry AddStream(Compression _method, string _filenameInZip, Stream _source, DateTime _modTime, string _comment = null)
        {
            return AddStreamAsync(_method, _filenameInZip, _source, _modTime, _comment);
        }
        /// <summary>
        /// Add full contents of a stream into the Zip storage
        /// </summary>
        /// <param name="_method">Compression method</param>
        /// <param name="_filenameInZip">Filename and path as desired in Zip directory</param>
        /// <param name="_source">Stream object containing the data to store in Zip</param>
        /// <param name="_modTime">Modification time of the data to store</param>
        /// <param name="_comment">Comment for stored file</param>
        public ZipFileEntry AddStreamAsync(Compression _method, string _filenameInZip, Stream _source, DateTime _modTime, string _comment = null)
        {
            if (Access == FileAccess.Read)
                throw new InvalidOperationException("Writing is not alowed");

            // Prepare the fileinfo
            ZipFileEntry zfe = new ZipFileEntry
            {
                Method = _method,
                EncodeUTF8 = EncodeUTF8,
                FilenameInZip = NormalizedFilename(_filenameInZip),
                Comment = _comment ?? string.Empty,
                // Even though we write the header now, it will have to be rewritten, since we don't know compressed size or crc.
                Crc32 = 0, // to be updated later
                HeaderOffset = (uint)ZipFileStream.Position, // offset within file of the start of this local record
                CreationTime = _modTime,
                ModifyTime = _modTime,
                AccessTime = _modTime
            };

            // Write local header
            WriteLocalHeader(zfe);
            zfe.FileOffset = (uint)ZipFileStream.Position;

            // Write file to zip (store)
            Store(zfe, _source);

            _source.Close();

            UpdateCrcAndSizes(zfe);

            Files.Add(zfe);
            return zfe;
        }
        /// <summary>
        /// Add full contents of a directory into the Zip storage
        /// </summary>
        /// <param name="_method">Compression method</param>
        /// <param name="_pathname">Full path of directory to add to Zip storage</param>
        /// <param name="_pathnameInZip">Path name as desired in Zip directory</param>
        /// <param name="_comment">Comment for stored directory</param>
        public void AddDirectory(Compression _method, string _pathname, string _pathnameInZip, string _comment = null)
        {
            if (Access == FileAccess.Read)
                throw new InvalidOperationException("Writing is not allowed");

            string foldername;
            int pos = _pathname.LastIndexOf(Path.DirectorySeparatorChar);
            string separator = Path.DirectorySeparatorChar.ToString();
            foldername = pos >= 0 ? _pathname.Remove(0, pos + 1) : _pathname;

            if (_pathnameInZip != null && _pathnameInZip != "")
                foldername = _pathnameInZip + foldername;

            if (!foldername.EndsWith(separator, StringComparison.CurrentCulture))
                foldername = foldername + separator;

            //AddStream(_method, foldername, null/* TODO Change to default(_) if this is not a reference type */, File.GetLastWriteTime(_pathname), _comment);

            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(_pathname);
            foreach (string fileName in fileEntries)
                AddFile(_method, fileName, foldername + Path.GetFileName(fileName), "");

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(_pathname);
            foreach (string subdirectory in subdirectoryEntries)
                AddDirectory(_method, subdirectory, foldername, "");
        }
        /// <summary>
        /// Updates central directory (if pertinent) and close the Zip storage
        /// </summary>
        /// <remarks>This is a required step, unless automatic dispose is used</remarks>
        public void Close()
        {
            if (Access != FileAccess.Read)
            {
                uint centralOffset = (uint)ZipFileStream.Position;
                uint centralSize = 0;

                if (CentralDirImage != null)
                    ZipFileStream.Write(CentralDirImage, 0, CentralDirImage.Length);

                foreach (var t in Files)
                {
                    long pos = ZipFileStream.Position;
                    WriteCentralDirRecord(t);
                    centralSize += (uint)(ZipFileStream.Position - pos);
                }

                if (CentralDirImage != null)
                    WriteEndRecord(centralSize + (uint)CentralDirImage.Length, centralOffset);
                else
                    WriteEndRecord(centralSize, centralOffset);
            }

            if (ZipFileStream != null && !leaveOpen)
            {
                ZipFileStream.Flush();
                ZipFileStream.Dispose();
                ZipFileStream = null;
            }
        }
        #endregion

        #region Private methods
        /* Local file header:
            local file header signature     4 bytes  (0x04034b50)
            version needed to extract       2 bytes
            general purpose bit flag        2 bytes
            compression method              2 bytes
            last mod file time              2 bytes
            last mod file date              2 bytes
            crc-32                          4 bytes
            compressed size                 4 bytes
            uncompressed size               4 bytes
            filename length                 2 bytes
            extra field length              2 bytes

            filename (variable size)
            extra field (variable size)
        */
        private void WriteLocalHeader(ZipFileEntry _zfe)
        {
            long pos = ZipFileStream.Position;
            Encoding encoder = _zfe.EncodeUTF8 ? Encoding.UTF8 : DefaultEncoding;
            byte[] encodedFilename = encoder.GetBytes(_zfe.FilenameInZip);
            byte[] extraInfo = CreateExtraInfo(_zfe);

            ZipFileStream.Write(new byte[] { 80, 75, 3, 4, 20, 0 }, 0, 6); // No extra header
            ZipFileStream.Write(BitConverter.GetBytes((ushort)(_zfe.EncodeUTF8 ? 0x0800 : 0)), 0, 2); // filename and comment encoding 
            ZipFileStream.Write(BitConverter.GetBytes((ushort)_zfe.Method), 0, 2);  // zipping method
            ZipFileStream.Write(BitConverter.GetBytes(DateTimeToDosTime(_zfe.ModifyTime)), 0, 4); // zipping date and time
            ZipFileStream.Write(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, 0, 12); // unused CRC, un/compressed size, updated later
            ZipFileStream.Write(BitConverter.GetBytes((ushort)encodedFilename.Length), 0, 2); // filename length
            ZipFileStream.Write(BitConverter.GetBytes((ushort)extraInfo.Length), 0, 2); // extra length

            ZipFileStream.Write(encodedFilename, 0, encodedFilename.Length);
            ZipFileStream.Write(extraInfo, 0, extraInfo.Length);
            _zfe.HeaderSize = (uint)(ZipFileStream.Position - pos);
        }
        /* Central directory's File header:
            central file header signature   4 bytes  (0x02014b50)
            version made by                 2 bytes
            version needed to extract       2 bytes
            general purpose bit flag        2 bytes
            compression method              2 bytes
            last mod file time              2 bytes
            last mod file date              2 bytes
            crc-32                          4 bytes
            compressed size                 4 bytes
            uncompressed size               4 bytes
            filename length                 2 bytes
            extra field length              2 bytes
            file comment length             2 bytes
            disk number start               2 bytes
            internal file attributes        2 bytes
            external file attributes        4 bytes
            relative offset of local header 4 bytes

            filename (variable size)
            extra field (variable size)
            file comment (variable size)
        */
        private void WriteCentralDirRecord(ZipFileEntry _zfe)
        {
            Encoding encoder = _zfe.EncodeUTF8 ? Encoding.UTF8 : DefaultEncoding;
            byte[] encodedFilename = encoder.GetBytes(_zfe.FilenameInZip);
            byte[] encodedComment = encoder.GetBytes(_zfe.Comment);
            byte[] extraInfo = CreateExtraInfo(_zfe);

            ZipFileStream.Write(new byte[] { 80, 75, 1, 2, 23, 0xB, 20, 0 }, 0, 8);
            ZipFileStream.Write(BitConverter.GetBytes((ushort)(_zfe.EncodeUTF8 ? 0x0800 : 0)), 0, 2); // filename and comment encoding 
            ZipFileStream.Write(BitConverter.GetBytes((ushort)_zfe.Method), 0, 2);  // zipping method
            ZipFileStream.Write(BitConverter.GetBytes(DateTimeToDosTime(_zfe.ModifyTime)), 0, 4);  // zipping date and time
            ZipFileStream.Write(BitConverter.GetBytes(_zfe.Crc32), 0, 4); // file CRC
            ZipFileStream.Write(BitConverter.GetBytes(_zfe.CompressedSize), 0, 4); // compressed file size
            ZipFileStream.Write(BitConverter.GetBytes(_zfe.FileSize), 0, 4); // uncompressed file size
            ZipFileStream.Write(BitConverter.GetBytes((ushort)encodedFilename.Length), 0, 2); // Filename in zip
            ZipFileStream.Write(BitConverter.GetBytes((ushort)extraInfo.Length), 0, 2); // extra length
            ZipFileStream.Write(BitConverter.GetBytes((ushort)encodedComment.Length), 0, 2);

            ZipFileStream.Write(BitConverter.GetBytes((ushort)0), 0, 2); // disk=0
            ZipFileStream.Write(BitConverter.GetBytes((ushort)0), 0, 2); // file type: binary
            ZipFileStream.Write(BitConverter.GetBytes((ushort)0), 0, 2); // Internal file attributes
            ZipFileStream.Write(BitConverter.GetBytes((ushort)0x8100), 0, 2); // External file attributes (normal/readable)
            ZipFileStream.Write(BitConverter.GetBytes(_zfe.HeaderOffset), 0, 4);  // Offset of header

            ZipFileStream.Write(encodedFilename, 0, encodedFilename.Length);
            ZipFileStream.Write(extraInfo, 0, extraInfo.Length);
            ZipFileStream.Write(encodedComment, 0, encodedComment.Length);
        }
        /* End of central dir record:
            end of central dir signature    4 bytes  (0x06054b50)
            number of this disk             2 bytes
            number of the disk with the
            start of the central directory  2 bytes
            total number of entries in
            the central dir on this disk    2 bytes
            total number of entries in
            the central dir                 2 bytes
            size of the central directory   4 bytes
            offset of start of central
            directory with respect to
            the starting disk number        4 bytes
            zipfile comment length          2 bytes
            zipfile comment (variable size)
        */
        private void WriteEndRecord(uint _size, uint _offset)
        {
            Encoding encoder = EncodeUTF8 ? Encoding.UTF8 : DefaultEncoding;
            byte[] encodedComment = encoder.GetBytes(Comment);

            ZipFileStream.Write(new byte[] { 80, 75, 5, 6, 0, 0, 0, 0 }, 0, 8);
            ZipFileStream.Write(BitConverter.GetBytes((ushort)Files.Count + ExistingFiles), 0, 2);
            ZipFileStream.Write(BitConverter.GetBytes((ushort)Files.Count + ExistingFiles), 0, 2);
            ZipFileStream.Write(BitConverter.GetBytes(_size), 0, 4);
            ZipFileStream.Write(BitConverter.GetBytes(_offset), 0, 4);
            ZipFileStream.Write(BitConverter.GetBytes((ushort)encodedComment.Length), 0, 2);
            ZipFileStream.Write(encodedComment, 0, encodedComment.Length);
        }
        // Copies all source file into storage file
        private Compression Store(ZipFileEntry _zfe, Stream _source)
        {
            byte[] buffer = new byte[16384];
            int bytesRead;
            uint totalRead = 0;
            Stream outStream;

            long posStart = ZipFileStream.Position;
            long sourceStart = _source.CanSeek ? _source.Position : 0;

            outStream = _zfe.Method == Compression.Store ? ZipFileStream : new DeflateStream(ZipFileStream, CompressionMode.Compress, true);

            _zfe.Crc32 = 0 ^ 0xffffffff;

            do
            {
                bytesRead = _source.Read(buffer, 0, buffer.Length);

                totalRead += (uint)bytesRead;
                if (bytesRead > 0)
                {
                    outStream.Write(buffer, 0, bytesRead);

                    for (uint i = 0; i < bytesRead; i++)
                    {
                        _zfe.Crc32 = CrcTable[(_zfe.Crc32 ^ buffer[i]) & 0xFF] ^ (_zfe.Crc32 >> 8);
                    }
                }
            } while (bytesRead > 0);
            outStream.Flush();

            if (_zfe.Method == Compression.Deflate)
                outStream.Dispose();

            _zfe.Crc32 ^= 0xffffffff;
            _zfe.FileSize = totalRead;
            _zfe.CompressedSize = (uint)(ZipFileStream.Position - posStart);

            // Verify for real compression
            if (_zfe.Method == Compression.Deflate && !ForceDeflating && _source.CanSeek && _zfe.CompressedSize > _zfe.FileSize)
            {
                // Start operation again with Store algorithm
                _zfe.Method = Compression.Store;
                ZipFileStream.Position = posStart;
                ZipFileStream.SetLength(posStart);
                _source.Position = sourceStart;

                return Store(_zfe, _source);
            }

            return _zfe.Method;
        }
        /* DOS Date and time:
            MS-DOS date. The date is a packed value with the following format. Bits Description 
                0-4 Day of the month (131) 
                5-8 Month (1 = January, 2 = February, and so on) 
                9-15 Year offset from 1980 (add 1980 to get actual year) 
            MS-DOS time. The time is a packed value with the following format. Bits Description 
                0-4 Second divided by 2 
                5-10 Minute (059) 
                11-15 Hour (023 on a 24-hour clock) 
        */
        private uint DateTimeToDosTime(DateTime _dt)
        {
            return (uint)(
                (_dt.Second / 2) | (_dt.Minute << 5) | (_dt.Hour << 11) |
                (_dt.Day << 16) | (_dt.Month << 21) | ((_dt.Year - 1980) << 25));
        }
        private byte[] CreateExtraInfo(ZipFileEntry _zfe)
        {
            byte[] buffer = new byte[36];
            BitConverter.GetBytes((ushort)0x000A).CopyTo(buffer, 0); // NTFS FileTime
            BitConverter.GetBytes((ushort)32).CopyTo(buffer, 2); // Length
            BitConverter.GetBytes((ushort)1).CopyTo(buffer, 8); // Tag 1
            BitConverter.GetBytes((ushort)24).CopyTo(buffer, 10); // Size 1
            BitConverter.GetBytes(_zfe.ModifyTime.ToFileTime()).CopyTo(buffer, 12); // MTime
            BitConverter.GetBytes(_zfe.AccessTime.ToFileTime()).CopyTo(buffer, 20); // ATime
            BitConverter.GetBytes(_zfe.CreationTime.ToFileTime()).CopyTo(buffer, 28); // CTime

            return buffer;
        }

        /* CRC32 algorithm
          The 'magic number' for the CRC is 0xdebb20e3.  
          The proper CRC pre and post conditioning is used, meaning that the CRC register is
          pre-conditioned with all ones (a starting value of 0xffffffff) and the value is post-conditioned by
          taking the one's complement of the CRC residual.
          If bit 3 of the general purpose flag is set, this field is set to zero in the local header and the correct
          value is put in the data descriptor and in the central directory.
        */
        private void UpdateCrcAndSizes(ZipFileEntry _zfe)
        {
            long lastPos = ZipFileStream.Position;  // remember position

            ZipFileStream.Position = _zfe.HeaderOffset + 8;
            ZipFileStream.Write(BitConverter.GetBytes((ushort)_zfe.Method), 0, 2);  // zipping method

            ZipFileStream.Position = _zfe.HeaderOffset + 14;
            ZipFileStream.Write(BitConverter.GetBytes(_zfe.Crc32), 0, 4);  // Update CRC
            ZipFileStream.Write(BitConverter.GetBytes(_zfe.CompressedSize), 0, 4);  // Compressed size
            ZipFileStream.Write(BitConverter.GetBytes(_zfe.FileSize), 0, 4);  // Uncompressed size

            ZipFileStream.Position = lastPos;  // restore position
        }
        // Replaces backslashes with slashes to store in zip header
        private string NormalizedFilename(string _filename)
        {
            string filename = _filename.Replace('\\', '/');

            int pos = filename.IndexOf(':');
            if (pos >= 0)
                filename = filename.Remove(0, pos + 1);

            return filename.Trim('/');
        }
        #endregion

        #region IDisposable Members
        /// <summary>
        /// Closes the Zip file stream
        /// </summary>
        public void Dispose()
        {
            Close();
        }
        #endregion
    }
}