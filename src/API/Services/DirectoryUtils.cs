using System.IO.Abstractions;
using System.Runtime;

using API.Options;

namespace API.Services
{
    public class DirectoryUtils
    {
        public static IDirectoryInfo? GetBaseDirectory<T>(ILogger<T> logger,IFileSystem fileSystem, string folderName)
        {
            var dir = fileSystem.DirectoryInfo.New(AppDomain.CurrentDomain.BaseDirectory);
            do
            {
                if (dir.GetDirectories().Any(d => d.Name == folderName))
                {
                    dir = dir.GetDirectories().FirstOrDefault(d => d.Name == folderName);
                    return dir;
                }
                dir = dir.Parent;
            } while (dir?.Parent != null);
            logger.LogError("Unable to locate the folder {folder} in either app folder or in a parent folder", folderName);
            return null;
        }
    }
}
