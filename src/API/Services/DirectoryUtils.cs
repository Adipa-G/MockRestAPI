using System.IO.Abstractions;

namespace API.Services
{
    public class DirectoryUtils
    {
        public static IDirectoryInfo? GetBaseDirectory(IFileSystem fileSystem, string folderName)
        {
            var dir = fileSystem.DirectoryInfo.New(AppDomain.CurrentDomain.BaseDirectory);
            do
            {
                if (dir.GetDirectories().Any(d => d.Name == folderName))
                {
                    dir = dir.GetDirectories().FirstOrDefault(d => d.Name == folderName);
                    break;
                }
                dir = dir.Parent;
            } while (dir?.Parent != null);
            return dir;
        }
    }
}
