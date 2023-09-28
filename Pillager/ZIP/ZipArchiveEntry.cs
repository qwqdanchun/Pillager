#region License
/*
    Copyright (c) 2015, Paweł Hofman (CodeTitans)
    All Rights Reserved.

    Licensed under MIT License
    For more information please visit:

    https://github.com/phofman/zip/blob/master/LICENSE
        or
    http://opensource.org/licenses/MIT


    For latest source code, documentation, samples
    and more information please visit:

    https://github.com/phofman/zip
*/
#endregion

namespace System.IO.Compression
{
    /// <summary>
    /// Representation of a single item inside a ZIP archive.
    /// https://msdn.microsoft.com/en-us/library/system.io.compression.ziparchiveentry(v=vs.110).aspx
    /// </summary>
    public sealed class ZipArchiveEntry
    {
        internal ZipArchiveEntry(ZipArchive archive, ShellHelper.FolderItem item, string tempLocalPath, string entryName, long length)
        {
            if (archive == null)
                throw new ArgumentNullException("archive");
            if (string.IsNullOrEmpty(entryName))
                throw new ArgumentNullException("entryName");

            Archive = archive;
            Item = item;
            TempLocalPath = tempLocalPath;
            FullName = entryName;
            Name = Path.GetFileName(entryName);
            Length = length;
        }

        #region Properties

        public ZipArchive Archive
        {
            get;
            private set;
        }

        public long CompressedLength
        {
            get { return Length; }
        }

        /// <summary>
        /// Gets the relative path of the entry in ZIP archive.
        /// </summary>
        public string FullName
        {
            get;
            private set;
        }

        public DateTime LastWriteTime
        {
            get;
            private set;
        }

        public long Length
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        internal ShellHelper.FolderItem Item
        {
            get;
            private set;
        }

        internal string TempLocalPath
        {
            get;
            private set;
        }

        #endregion

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return FullName;
        }

        internal bool Match(ZipArchiveEntry item)
        {
            if (item == null)
                return false;
            return string.CompareOrdinal(item.FullName, FullName) == 0;
        }

        /// <summary>
        /// Opens the entry from the zip archive.
        /// </summary>
        public Stream Open()
        {
            switch (Archive.Mode)
            {
                case ZipArchiveMode.Read:
                    if (string.IsNullOrEmpty(TempLocalPath))
                        throw new IOException(string.Concat("Unable to find requested file matching the (\"", FullName, "\")"));
                    return new FileStream(TempLocalPath, FileMode.Open, FileAccess.Read);
                case ZipArchiveMode.Create: // fall-through
                case ZipArchiveMode.Update:
                    return new FileStream(TempLocalPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                default:
                    throw new IOException("This mode is not supported");
            }
        }

        /// <summary>
        /// Deletes the entry from the zip archive.
        /// </summary>
        public void Delete()
        {
            Archive.Delete(this);
            TempLocalPath = null;
        }

        /// <summary>
        /// Extracts an entry in the zip archive to a file.
        /// </summary>
        public void ExtractToFile(string destinationFileName)
        {
            if (string.IsNullOrEmpty(destinationFileName))
                throw new ArgumentNullException("destinationFileName");

            Archive.ExtractToFile(this, destinationFileName, false);
        }

        /// <summary>
        /// Extracts an entry in the zip archive to a file.
        /// </summary>
        public void ExtractToFile(string destinationFileName, bool overwrite)
        {
            if (string.IsNullOrEmpty(destinationFileName))
                throw new ArgumentNullException("destinationFileName");

            Archive.ExtractToFile(this, destinationFileName, overwrite);
        }
    }
}
