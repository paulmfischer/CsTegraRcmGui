using System.Runtime.InteropServices;

namespace CsTegraRcmGui.Core.Services;

/// <summary>
/// Linux-only fallback for the RCM stack-smashing GET_STATUS control
/// transfer: submits it directly as a raw usbfs URB via ioctl, bypassing
/// libusb's control-transfer path (which rejects a transfer this size
/// locally on Linux, before it ever reaches the device). Mirrors
/// JTegraNX's Linux native helper (Native/Linux/native.cpp).
/// </summary>
internal static class LinuxRcmTrigger
{
    private const int ORdWr = 2;
    private const long IoctlSubmitUrb = 0x8038550a;
    private const long IoctlDiscardUrb = 0x550b;
    private const long IoctlReapUrbNoDelay = 0x4008550d;
    private const byte UrbTypeControl = 2;
    private const byte EndpointDirectionIn = 0x80;
    private const int MaxOpenAttempts = 5;
    private const uint OpenRetryDelayMicroseconds = 50_000;

    /// <summary>
    /// Returns true only when the URB was submitted and never completed
    /// normally — the signal that the overflow actually landed and the
    /// device jumped away. Any other outcome (couldn't open the device
    /// node, SUBMITURB failed, or the transfer completed normally) means
    /// the trigger did not land: the device is left with a fully-written
    /// payload but no working trigger, and will silently time out and
    /// disconnect on its own if the caller reports this as success.
    /// </summary>
    public static bool Trigger(byte busNumber, byte deviceAddress, int triggerLength, IFileLogger log)
    {
        var devicePath = $"/dev/bus/usb/{busNumber:D3}/{deviceAddress:D3}";

        // The device node can briefly exist but not yet be readable/writable
        // right after enumeration: udev applies the permission-granting rule
        // asynchronously, so opening it immediately after the device is
        // first seen as connected can race that rule and fail with EACCES.
        // Retrying briefly absorbs that race instead of failing outright.
        var fd = -1;
        for (var attempt = 1; attempt <= MaxOpenAttempts; attempt++)
        {
            fd = Open(devicePath, ORdWr);
            if (fd >= 0)
                break;

            var errno = Marshal.GetLastWin32Error();
            log.Log($"Trigger (Linux raw URB): failed to open {devicePath} (errno {errno}, attempt {attempt}/{MaxOpenAttempts})");
            if (attempt < MaxOpenAttempts)
                Usleep(OpenRetryDelayMicroseconds);
        }

        if (fd < 0)
            return false;

        var requestSize = 8 + triggerLength; // 8-byte USB setup packet + response data
        var requestBuffer = Marshal.AllocHGlobal(requestSize);
        var urbBuffer = Marshal.AllocHGlobal(56); // sizeof(struct usbdevfs_urb) on x86_64
        var reapedUrbSlot = Marshal.AllocHGlobal(IntPtr.Size);
        try
        {
            Marshal.WriteByte(requestBuffer, 0, 0x82);       // bRequestType: Device-to-host | Standard | Recipient=Endpoint
            Marshal.WriteByte(requestBuffer, 1, 0x00);       // bRequest: GET_STATUS
            Marshal.WriteInt16(requestBuffer, 2, 0);         // wValue
            Marshal.WriteInt16(requestBuffer, 4, 0);         // wIndex
            Marshal.WriteInt16(requestBuffer, 6, (short)triggerLength); // wLength

            // struct usbdevfs_urb (x86_64 layout): type@0, endpoint@1, status@4,
            // flags@8, buffer@16, buffer_length@24, actual_length@28,
            // start_frame@32, number_of_packets@36, error_count@40, signr@44,
            // usercontext@48 — 56 bytes total.
            for (var i = 0; i < 56; i++)
                Marshal.WriteByte(urbBuffer, i, 0);
            Marshal.WriteByte(urbBuffer, 0, UrbTypeControl);
            Marshal.WriteByte(urbBuffer, 1, EndpointDirectionIn);
            Marshal.WriteIntPtr(urbBuffer, 16, requestBuffer);
            Marshal.WriteInt32(urbBuffer, 24, requestSize);

            log.Log($"Trigger (Linux raw URB): submitting, length={triggerLength}");
            if (Ioctl(fd, IoctlSubmitUrb, urbBuffer) != 0)
            {
                log.Log($"Trigger (Linux raw URB): SUBMITURB failed (errno {Marshal.GetLastWin32Error()})");
                return false;
            }

            Usleep(250_000);

            if (Ioctl(fd, IoctlReapUrbNoDelay, reapedUrbSlot) == 0)
            {
                // The transfer completed normally, which means the overflow
                // did not land: the device is still alive and responding
                // instead of having jumped into the payload.
                log.Log("Trigger (Linux raw URB): device responded before jumping away (trigger did not land)");
                return false;
            }

            // Expected: once the trigger lands, the device jumps into
            // the payload and never completes this URB.
            log.Log("Trigger (Linux raw URB): no response yet (expected once the trigger lands) — discarding");
            Ioctl(fd, IoctlDiscardUrb, IntPtr.Zero);
            Usleep(40_000);
            Ioctl(fd, IoctlReapUrbNoDelay, reapedUrbSlot);
            return true;
        }
        finally
        {
            Marshal.FreeHGlobal(requestBuffer);
            Marshal.FreeHGlobal(urbBuffer);
            Marshal.FreeHGlobal(reapedUrbSlot);
            Close(fd);
        }
    }

    [DllImport("libc", EntryPoint = "open", SetLastError = true)]
    private static extern int Open(string pathname, int flags);

    [DllImport("libc", EntryPoint = "close", SetLastError = true)]
    private static extern int Close(int fd);

    [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
    private static extern int Ioctl(int fd, long request, IntPtr arg);

    [DllImport("libc", EntryPoint = "usleep")]
    private static extern void Usleep(uint microseconds);
}
