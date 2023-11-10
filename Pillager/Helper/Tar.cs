using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using tar_cs;

namespace Pillager.Helper
{
    internal class Tar
    {
        public static void Pack(string savepath,string savezippath)
        {
            using (var outFile = File.Create(savezippath))
            {
                using (var outStream = new GZipStream(outFile, CompressionMode.Compress))
                {
                    using (var writer = new TarWriter(outStream))
                    {
                        writer.WriteDirectory(savepath, savepath, true);
                    }
                }
            }
        }
    }
}
