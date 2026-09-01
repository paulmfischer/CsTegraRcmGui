using System.Diagnostics;

namespace CsTegraRcmGui.Core.Services;

public sealed class WindowsFolderOpener : IFolderOpener
{
    public void OpenContainingFolder(string filePath)
    {
        Process.Start(new ProcessStartInfo("explorer.exe", [$"/select,\"{filePath}\""]) { UseShellExecute = true });
    }
}
