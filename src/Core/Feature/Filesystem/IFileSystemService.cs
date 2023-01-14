using System.IO;

namespace SoftThorn.Monstercat.Browser.Core
{
    public interface IFileSystemService
    {
        string[] DirectoryGetFiles(string directoryPath, string searchPattern);
        Stream FileOpen(string filePath);
    }
}