using System.Diagnostics;

namespace CsTegraRcmGui.Core.Services;

public sealed class LinuxFolderOpener : IFolderOpener
{
    public void OpenContainingFolder(string filePath)
    {
        var folder = Path.GetDirectoryName(filePath) is { Length: > 0 } dir ? dir : filePath;
        Process.Start(new ProcessStartInfo("xdg-open", [folder]) { UseShellExecute = false });
    }
}
