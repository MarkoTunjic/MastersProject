namespace GrpcGenerator.copier;

public static class Copier
{
    public static void CopyDirectory(string sourcePath, string destinationPath)
    {
        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));
        }

        foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        {
            File.Copy(newPath, newPath.Replace(sourcePath, destinationPath), true);
        }
    }
}