using System.IO.Compression;

namespace GrpcGenerator.Utils;

public static class Zipper
{
    public static void ZipDirectory(string startPath, string zipPath)
    {
        ZipFile.CreateFromDirectory(startPath, zipPath);
    }
}