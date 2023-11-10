using System;
using System.IO;
using System.Threading;
using tar_cs;

namespace tar_cs
{
    public class LegacyTarWriter : IDisposable
    {
        private readonly Stream outStream;
        protected byte[] buffer = new byte[1024];
        private bool isClosed;
        public bool ReadOnZero = true;

        /// <summary>
        /// Writes tar (see GNU tar) archive to a stream
        /// </summary>
        /// <param name="writeStream">stream to write archive to</param>
        public LegacyTarWriter(Stream writeStream)
        {
            outStream = writeStream;
        }

        protected virtual Stream OutStream
        {
            get { return outStream; }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Close();
        }

        #endregion


        public void WriteDirectoryEntry(string basepath,string path,int userId, int groupId, int mode)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");
            path = path.Replace(basepath, "").Replace("\\", "/");
            if (path=="") path += '/';
            if (path[path.Length - 1] != '/')
            {
                path += '/';
            }
            DateTime lastWriteTime;
            if (Directory.Exists(path))
            {
                lastWriteTime = Directory.GetLastWriteTime(path);
            }
            else
            {
                lastWriteTime = DateTime.Now;
            }
            WriteHeader(basepath,path, lastWriteTime, 0, userId, groupId, mode, EntryType.Directory);
        }

        public void WriteDirectoryEntry(string basepath, string path)
        {
            WriteDirectoryEntry(basepath,path, 101, 101, 0777);
        }

        public void WriteDirectory(string basepath,string directory, bool doRecursive)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentNullException("directory");

            WriteDirectoryEntry(basepath,directory);

            string[] files = Directory.GetFiles(directory);
            foreach(var fileName in files)
            {
                Write(basepath,fileName);
            }

            string[] directories = Directory.GetDirectories(directory);
            foreach(var dirName in directories)
            {
                WriteDirectoryEntry(basepath,dirName);
                if(doRecursive)
                {
                    WriteDirectory(basepath,dirName, true);
                }
            }
        }


        public void Write(string basepath, string fileName)
        {
            if(string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");
            using (FileStream file = File.OpenRead(fileName))
            {
                Write(basepath,file, file.Length, fileName, 61, 61, 511, File.GetLastWriteTime(file.Name));
            }
        }

        public virtual void Write(string basepath, Stream data, long dataSizeInBytes, string name, int userId, int groupId, int mode,
                                  DateTime lastModificationTime)
        {
            if(isClosed)
                throw new TarException("Can not write to the closed writer");
            WriteHeader(basepath, name, lastModificationTime, dataSizeInBytes, userId, groupId, mode, EntryType.File);
            WriteContent(dataSizeInBytes, data);
            AlignTo512(dataSizeInBytes,false);
        }

        protected void WriteContent(long count, Stream data)
        {
            while (count > 0 && count > buffer.Length)
            {
                int bytesRead = data.Read(buffer, 0, buffer.Length);
                if (bytesRead < 0)
                    throw new IOException("LegacyTarWriter unable to read from provided stream");
                if (bytesRead == 0)
                {
                    if (ReadOnZero)
                        Thread.Sleep(100);
                    else
                        break;
                }
                OutStream.Write(buffer, 0, bytesRead);
                count -= bytesRead;
            }
            if (count > 0)
            {
                int bytesRead = data.Read(buffer, 0, (int) count);
                if (bytesRead < 0)
                    throw new IOException("LegacyTarWriter unable to read from provided stream");
                if (bytesRead == 0)
                {
                    while (count > 0)
                    {
                        OutStream.WriteByte(0);
                        --count;
                    }
                }
                else
                    OutStream.Write(buffer, 0, bytesRead);
            }
        }

        protected virtual void WriteHeader(string basepath, string name, DateTime lastModificationTime, long count, int userId, int groupId, int mode, EntryType entryType)
        {
            var header = new TarHeader
                         {
                             FileName = name.Replace(basepath, "").Replace("\\", "/"),
                             LastModification = lastModificationTime,
                             SizeInBytes = count,
                             UserId = userId,
                             GroupId = groupId,
                             Mode = mode,
                             EntryType = entryType
                         };
            OutStream.Write(header.GetHeaderValue(), 0, header.HeaderSize);
        }


        public void AlignTo512(long size,bool acceptZero)
        {
            size = size%512;
            if (size == 0 && !acceptZero) return;
            while (size < 512)
            {
                OutStream.WriteByte(0);
                size++;
            }
        }

        public virtual void Close()
        {
            if (isClosed) return;
            AlignTo512(0,true);
            AlignTo512(0,true);
            isClosed = true;
        }
    }
}