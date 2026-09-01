namespace CsTegraRcmGui.Core.Services;

/// <summary>
/// Opens a file's containing folder in the platform's file manager.
/// </summary>
public interface IFolderOpener
{
    void OpenContainingFolder(string filePath);
}
