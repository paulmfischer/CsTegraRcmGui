# Windows setup

One-time, per-machine step — the Windows equivalent of the udev rule in
[`packaging/linux/udev`](../linux/udev). Everything else (the `libusb-1.0.dll`
`LibUsbRcmDeviceService` needs) is bundled with the app and needs no separate
install.

## Why this is needed

Linux already has a generic in-kernel driver for any USB device; the udev
rule just grants the app permission to open it. Windows has no equivalent —
a device needs a specific driver bound to it before any app can open it at
all, and a device sitting in Tegra USB recovery mode (RCM) has none by
default. This binds one.

## Steps

1. Put the device into RCM mode and plug it in. It'll show up unrecognized —
   e.g. as "APX" in Device Manager, possibly under "Other devices".
2. Download and run [Zadig](https://zadig.akeo.ie/).
3. Options → **List All Devices** (the device won't appear otherwise).
4. Select **APX** from the device dropdown (VID `0955`, PID `7321`).
5. Set the target driver to **libusbK**, then click **Install Driver** (or
   **Replace Driver** if something else is already bound).

That's it — this also installs `libusbK.dll` system-wide, which the app's
Windows-specific RCM trigger path (`WindowsRcmTrigger.cs`) needs. libusb's
Windows backend caps control transfers at 4096 bytes regardless of which of
WinUSB/libusbK/libusb0 is bound, which is too small for the trigger transfer
this exploit requires — `WindowsRcmTrigger` bypasses that cap by talking to
libusbK's driver directly, which is why libusbK specifically (not WinUSB) is
the driver to pick above.

This only needs to be redone if the driver binding is later changed (e.g.
back to WinUSB, or removed) — it isn't tied to the app installation itself.
