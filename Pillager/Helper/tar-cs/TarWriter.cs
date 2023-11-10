using System;
using System.IO;

namespace tar_cs
{
    public class TarWriter : LegacyTarWriter
    {

        public TarWriter(Stream writeStream) : base(writeStream)
        {
        }

        protected override void WriteHeader(string basepath, string name, DateTime lastModificationTime, long count, int userId, int groupId, int mode, EntryType entryType)
        {
            var tarHeader = new UsTarHeader()
            {
                FileName = name.Replace(basepath, "").Replace("\\", "/"),
                LastModification = lastModificationTime,
                SizeInBytes = count,
                UserId = userId,
                UserName = Convert.ToString(userId,8),
                GroupId = groupId,
                GroupName = Convert.ToString(groupId,8),
                Mode = mode,
                EntryType = entryType
            };
            OutStream.Write(tarHeader.GetHeaderValue(), 0, tarHeader.HeaderSize);
        }

        protected virtual void WriteHeader(string basepath, string name, DateTime lastModificationTime, long count, string userName, string groupName, int mode)
        {
            var tarHeader = new UsTarHeader()
            {
                FileName = name.Replace(basepath, "").Replace("\\", "/"),
                LastModification = lastModificationTime,
                SizeInBytes = count,
                UserId = userName.GetHashCode(),
                UserName = userName,
                GroupId = groupName.GetHashCode(),
                GroupName = groupName,
                Mode = mode
            };
            OutStream.Write(tarHeader.GetHeaderValue(), 0, tarHeader.HeaderSize);
        }
    }
}