using System.IO.Compression;

namespace GrpcGenerator.zipper;

public static class Zipper
{
    public static void ZipDirectory(string startPath, string zipPath)
    {
        ZipFile.CreateFromDirectory(startPath, zipPath);
    }
}