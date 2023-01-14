using System.IO;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed class FileSystemService : IFileSystemService
    {
        public Stream FileOpen(string filePath)
        {
            return File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
        }

        public string[] DirectoryGetFiles(string directoryPath, string searchPattern)
        {
            return Directory.GetFiles(directoryPath, searchPattern);
        }
    }
}
