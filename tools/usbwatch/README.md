# usbwatch

Standalone diagnostic tool: polls `LibUsbRcmDeviceService.GetState()` twice a
second and prints a line whenever the state changes. Useful for checking that
a device is being detected correctly (permissions, udev rules, cabling)
without going through the full GUI.

Run:

```bash
dotnet run --project tools/usbwatch
```
