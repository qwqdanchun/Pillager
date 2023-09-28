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

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.IO.Compression
{
    /// <summary>
    /// Class representing ZIP archive.
    /// https://msdn.microsoft.com/en-us/library/system.io.compression.ziparchive(v=vs.110).aspx
    /// </summary>
    public sealed class ZipArchive : IDisposable
    {
        private readonly string _zipFileName;
        private readonly string _tempFolder;
        private readonly ZipArchiveMode _mode;

        private readonly IList<ZipArchiveEntry> _existing;
        private readonly List<ZipArchiveEntry> _toAdd;

        /// <summary>
        /// Opens specified archive for reading.
        /// </summary>
        public ZipArchive(FileStream zipStream)
            : this(zipStream, ZipArchiveMode.Read)
        {
        }

        /// <summary>
        /// Opens specified archive in given mode.
        /// </summary>
        public ZipArchive(FileStream zipFileStream, ZipArchiveMode mode)
        {
            if (zipFileStream == null)
                throw new ArgumentNullException("zipFileStream");

            _mode = mode;
            _zipFileName = zipFileStream.Name;
            _tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            // HINT: immediatelly close the file, as we will use Shell API to manipulate the file
            // and we need to let it modify its content (that's why other constructors are not
            // supported, that let to leave this stream open)
            zipFileStream.Close();
            CreateFolders();

            // initialize empty ZIP file if needed:
            var fileInfo = new FileInfo(_zipFileName);
            if (mode == ZipArchiveMode.Create || (mode == ZipArchiveMode.Update && fileInfo.Length == 0) || (mode == ZipArchiveMode.Read && fileInfo.Length == 0))
            {
                CreateEmptyZipFile();
                _existing = new List<ZipArchiveEntry>();
            }
            else
            {
                _existing = ScanForEntries(_zipFileName);
            }

            Entries = new ReadOnlyCollection<ZipArchiveEntry>(_existing);
            _toAdd = new List<ZipArchiveEntry>();
        }

        ~ZipArchive()
        {
            Dispose(false);
        }

        #region IDisposable Implementation

        private void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    switch (_mode)
                    {
                        case ZipArchiveMode.Create: // fall-though
                        case ZipArchiveMode.Update:
                            ZipContent();
                            break;
                        case ZipArchiveMode.Read:
                            // do nothing...
                            break;
                        default:
                            throw new IOException("Unsupported mode to update the archive on disposing");
                    }
                }
            }
            catch
            {
                // don't throw an exception, when called from finalizer's thread
                if (disposing)
                    throw;
            }
            finally
            {
                DeleteTemp();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value that describes the type of action the archive can perform on entries.
        /// </summary>
        public ZipArchiveMode Mode
        {
            get { return _mode; }
        }

        /// <summary>
        /// Gets the collection of entries that are currently in the archive.
        /// </summary>
        public ReadOnlyCollection<ZipArchiveEntry> Entries
        {
            get;
            private set;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Makes sure all parent folders exist for specified path to file or directory.
        /// </summary>
        private static string CreateParentFolder(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            path = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        private void CreateFolders()
        {
            CreateParentFolder(_zipFileName);

            try
            {
                Directory.CreateDirectory(_tempFolder);
            }
            catch
            {
            }
        }

        private void DeleteTemp()
        {
            try
            {
                Directory.Delete(_tempFolder, true);
            }
            catch
            {
            }
        }

        private void CreateEmptyZipFile()
        {
            byte[] headerBits = new byte[] { 80, 75, 5, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            File.WriteAllBytes(_zipFileName, headerBits);
        }

        private void ZipContent()
        {
            var destFile = ShellHelper.GetShell32Folder(_zipFileName);
            var srcFolder = ShellHelper.GetShell32Folder(_tempFolder);
            var items = srcFolder.Items();

            // copy folder into a ZIP file using Windows Shell API
            destFile.Copy(items);
            ShellHelper.WaitForCompletion(destFile.Path);
        }

        private void UnzipContent(string targetFolder)
        {
            if (!Directory.Exists(targetFolder))
                Directory.CreateDirectory(targetFolder);

            var srcFile = ShellHelper.GetShell32Folder(_zipFileName);
            var destFolder = ShellHelper.GetShell32Folder(targetFolder);

            destFolder.Copy(srcFile.Items());
            ShellHelper.WaitForCompletion(srcFile.Path);
        }

        private void CopyAddedFiles(string targetFolder)
        {
            foreach (var item in _toAdd)
            {
                var targetFile = Path.Combine(targetFolder, item.FullName);
                if (!string.IsNullOrEmpty(item.TempLocalPath))
                {
                    CreateParentFolder(targetFile);
                    File.Copy(item.TempLocalPath, targetFile, true);
                }
            }
        }

        private static string NormalizeEntryName(string entryName)
        {
            if (string.IsNullOrEmpty(entryName))
                throw new ArgumentNullException("entryName");

            // remove leading directory separators:
            int i = 0;
            while (i < entryName.Length && (entryName[i] == Path.AltDirectorySeparatorChar || entryName[i] == Path.DirectorySeparatorChar))
                i++;

            if (i > 0)
            {
                entryName = entryName.Substring(i);
            }

            // and make sure the same separator is used across the whole entry name's path:
            return entryName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        private static int Find(IEnumerable<ZipArchiveEntry> list, ZipArchiveEntry item)
        {
            int i = 0;

            foreach (var x in list)
            {
                if (x.Match(item))
                    return i;
                i++;
            }

            return -1;
        }

        private ZipArchiveEntry Add(ZipArchiveEntry item)
        {
            var existingIndex = Find(_toAdd, item);
            if (existingIndex >= 0)
            {
                _toAdd.RemoveAt(existingIndex);
            }

            _toAdd.Add(item);

            existingIndex = Find(_existing, item);
            if (existingIndex > 0)
            {
                _existing.RemoveAt(existingIndex);
            }

            _existing.Add(item);
            return item;
        }

        private IList<ZipArchiveEntry> ScanForEntries(string zipFileName)
        {
            var result = new List<ZipArchiveEntry>();

            if (File.Exists(zipFileName))
            {
                var srcFolder = ShellHelper.GetShell32Folder(zipFileName);

                ScanAdd(result, zipFileName, srcFolder.Items());
            }

            return result;
        }

        private void ScanAdd(List<ZipArchiveEntry> result, string initialPath, ShellHelper.FolderItems items)
        {
            if (items != null)
            {
                int count = items.Count;
                for (int i = 0; i < count; i++)
                {
                    var item = items[i];
                    var folder = item.AsFolder;
                    if (folder != null)
                    {
                        ScanAdd(result, initialPath, folder.Items());
                    }
                    else
                    {
                        var path = item.Path;
                        if (path != null && path.StartsWith(initialPath, StringComparison.Ordinal))
                            path = path.Substring(initialPath.Length + 1);
                        result.Add(new ZipArchiveEntry(this, item, null, path, item.Size));
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Archives a file by compressing it and adding it to the ZIP.
        /// </summary>
        public ZipArchiveEntry CreateEntryFromFile(string sourceFileName, string entryName, CompressionLevel compressionLevel)
        {
            if (string.IsNullOrEmpty(sourceFileName))
                throw new ArgumentNullException("sourceFileName");
            if (_mode == ZipArchiveMode.Read)
                throw new NotSupportedException("Current mode doesn't support items creation");
            if (string.IsNullOrEmpty(entryName))
                throw new ArgumentNullException("entryName");

            entryName = NormalizeEntryName(entryName);

            // copy file into temp folder and register for future compression:
            var path = Path.GetFullPath(sourceFileName);
            var destPath = Path.Combine(_tempFolder, entryName);

            CreateParentFolder(destPath);
            File.Copy(path, destPath, true);
            return Add(new ZipArchiveEntry(this, null, destPath, entryName, new FileInfo(destPath).Length));
        }

        /// <summary>
        /// Archives a file by compressing it and adding it to the ZIP.
        /// </summary>
        public ZipArchiveEntry CreateEntryFromFile(string sourceFileName, string entryName)
        {
            return CreateEntryFromFile(sourceFileName, entryName, CompressionLevel.Optimal);
        }

        /// <summary>
        /// Extracts all the files in the ZIP archive to specified directory.
        /// </summary>
        public void ExtractToDirectory(string destinationDirectoryName)
        {
            if (string.IsNullOrEmpty(destinationDirectoryName))
                throw new ArgumentNullException("destinationDirectoryName");

            var targetFolder = Path.GetFullPath(destinationDirectoryName);

            UnzipContent(targetFolder);
            CopyAddedFiles(targetFolder);
        }

        /// <summary>
        /// Creates an empty entry that has the specified path and entry name in the archive.
        /// </summary>
        public ZipArchiveEntry CreateEntry(string entryName)
        {
            return CreateEntry(entryName, CompressionLevel.Optimal);
        }

        /// <summary>
        /// Creates an empty entry that has the specified path and entry name in the archive.
        /// </summary>
        public ZipArchiveEntry CreateEntry(string entryName, CompressionLevel compressionLevel)
        {
            if (_mode == ZipArchiveMode.Read)
                throw new NotSupportedException("Current mode doesn't support items creation");
            if (string.IsNullOrEmpty(entryName))
                throw new ArgumentNullException("entryName");

            entryName = NormalizeEntryName(entryName);

            // create an empty file
            var destPath = Path.Combine(_tempFolder, entryName);

            CreateParentFolder(destPath);
            File.WriteAllBytes(destPath, new byte[0]);

            return Add(new ZipArchiveEntry(this, null, destPath, entryName, 0));
        }

        internal void Delete(ZipArchiveEntry item)
        {
            if (item != null)
            {
                if (!string.IsNullOrEmpty(item.TempLocalPath))
                    File.Delete(item.TempLocalPath);

                // if this is the last file withing directory, remove the directory to avoid runtime UI with errors:
                var parentFolder = Path.GetDirectoryName(item.TempLocalPath);
                if (!string.IsNullOrEmpty(parentFolder))
                {
                    var files = Directory.GetFiles(parentFolder, "*", SearchOption.AllDirectories);
                    if (files == null || files.Length == 0)
                    {
                        Directory.Delete(parentFolder, true);
                    }
                }

                bool removedFromAdd = _toAdd.Remove(item);
                bool removedFromExisting = _existing.Remove(item);

                if (removedFromAdd || removedFromExisting)
                    return;

                // it's not supported to delete files from existing ZIP:
                throw new IOException("Unable to delete file from ZIP archive");
            }
        }

        internal void ExtractToFile(ZipArchiveEntry item, string destinationFileName, bool overwrite)
        {
            if (item != null && !string.IsNullOrEmpty(destinationFileName))
            {
                var destPath = Path.GetFullPath(destinationFileName);
                var destFolder = CreateParentFolder(destPath);

                // is it an item from the local temp folder (item to add)?
                if (!string.IsNullOrEmpty(item.TempLocalPath))
                {
                    File.Copy(item.TempLocalPath, destPath, overwrite);
                }
                else
                {
                    // is it an item from the existing ZIP?
                    if (item.Item == null)
                        throw new IOException("Invalid entry to extract");

                    var destination = ShellHelper.GetShell32Folder(destFolder);
                    var itemFolder = item.Item.AsFolder;
                    if (itemFolder != null)
                    {
                        // TODO: this should potentially work, however waiting for completion method is required and ZipAchiveEntry refer to a file rather than to folder...
                        //destination.Copy(itemFolder.Items(), false, null);
                        throw new IOException("Extraction of folder is not supported via ZipArchiveEntry item");
                    }

                    // TODO: this could potentially overwrite existing file
                    destination.Copy(item.Item);
                    var path = Path.Combine(destFolder, item.Name);
                    ShellHelper.WaitForCompletion(path);

                    // update the name to required one:
                    if (path != destinationFileName)
                    {
                        if (File.Exists(destinationFileName))
                            File.Delete(destinationFileName);
                        File.Move(path, destinationFileName);
                    }
                }
            }
        }
    }
}
