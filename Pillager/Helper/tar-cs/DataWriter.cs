using System.IO;

namespace tar_cs
{
    internal class DataWriter : IArchiveDataWriter
    {
        private readonly long size;
        private long remainingBytes;
        private bool canWrite = true;
        private readonly Stream stream;

        public DataWriter(Stream data, long dataSizeInBytes)
        {
            size = dataSizeInBytes;
            remainingBytes = size;
            stream = data;
        }

        public int Write(byte[] buffer, int count)
        {
            if(remainingBytes == 0)
            {
                canWrite = false;
                return -1;
            }
            int bytesToWrite;
            if(remainingBytes - count < 0)
            {
                bytesToWrite = (int)remainingBytes;
            }
            else
            {
                bytesToWrite = count;
            }
            stream.Write(buffer,0,bytesToWrite);
            remainingBytes -= bytesToWrite;
            return bytesToWrite;
        }

        public bool CanWrite
        {
            get
            {
                return canWrite;
            }
        }
    }
}