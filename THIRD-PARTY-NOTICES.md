# Third-party notices

## libusb-1.0.dll (Windows builds)

`src/CsTegraRcmGui/runtimes/win-x64/native/libusb-1.0.dll` is an unmodified
binary from the official [libusb](https://github.com/libusb/libusb) project
(v1.0.30, VS2022/MS64 build), bundled here because Windows has no
system-installed libusb equivalent to the one Linux distributions provide as
a package. It is licensed under the [GNU Lesser General Public License,
version 2.1](https://github.com/libusb/libusb/blob/master/COPYING). Source
is available at https://github.com/libusb/libusb. Being dynamically loaded
(P/Invoke via [LibUsbDotNet](https://github.com/LibUsbDotNet/LibUsbDotNet)),
this binary can be replaced with a different LGPL-2.1-compliant build of the
same library by overwriting the file next to the built executable.
