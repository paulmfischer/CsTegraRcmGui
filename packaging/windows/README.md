# Windows setup

One-time, per-machine step. Everything else the app needs is bundled with it
and needs no separate install.

## Why this is needed

A device needs a specific driver bound to it before any app can open it at
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

That's it — this also installs `libusbK.dll` system-wide, which the app
needs. libusbK is required specifically (not WinUSB or libusb0): Windows
caps regular USB transfers at a size too small for the transfer this exploit
requires, and the app works around that cap in a way that only libusbK
supports.

This only needs to be redone if the driver binding is later changed (e.g.
back to WinUSB, or removed) — it isn't tied to the app installation itself.

## Running the app

Run it as Administrator (right-click the exe → **Run as administrator**).
Without elevation, opening the device fails with access denied even with the
libusbK driver bound correctly.
