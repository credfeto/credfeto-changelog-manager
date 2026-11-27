using System.IO;

namespace Credfeto.ChangeLog.Helpers;

public static class StartFolder
{
    public static string Read()
    {
        return TryRead() ?? Throws.StartFolderMissing();
    }

    public static string? TryRead()
    {
        try
        {
            return Directory.GetCurrentDirectory();
        }
        // The OS reports a current folder that has since been deleted as not found
        catch (IOException exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
    }
}
