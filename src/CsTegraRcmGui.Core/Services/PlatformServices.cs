namespace CsTegraRcmGui.Core.Services;

/// <summary>
/// Single place that picks the concrete implementation of each
/// platform-specific service for the current OS, so composition roots don't
/// each carry their own <c>OperatingSystem.IsX()</c> checks.
/// </summary>
public static class PlatformServices
{
    public static IFolderOpener CreateFolderOpener() =>
        OperatingSystem.IsWindows() ? new WindowsFolderOpener() : new LinuxFolderOpener();

    public static IRcmTrigger CreateRcmTrigger() =>
        OperatingSystem.IsLinux() ? new LinuxRcmTrigger()
        : OperatingSystem.IsWindows() ? new WindowsRcmTrigger()
        : new DefaultRcmTrigger();
}
