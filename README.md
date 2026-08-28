# CsTegraRcmGui

A cross-platform GUI for putting a Tegra device into USB recovery mode (RCM)
payload injection, built with [Avalonia](https://avaloniaui.net/) and .NET.

Tested on Windows and Linux. It's built on Avalonia, which is cross-platform,
so macOS should work too, but it hasn't been tested there — if you try it,
feedback is welcome.

## Prerequisites

- The device's RCM exploit entry point (e.g. a jig or the "short" trick) to
  get it into RCM mode — this app injects a payload once the device is
  already in that mode, it doesn't put it there.

## Download executable

TODO

## Running from source

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Platform-specific setup

The device needs to be visible to the app as a raw USB device before a
payload can be injected. Both platforms need a one-time, per-machine setup
step — see the OS-specific README for yours:

- [`packaging/linux/README.md`](packaging/linux/README.md)
- [`packaging/windows/README.md`](packaging/windows/README.md)
- macOS: not tested. No setup steps are documented yet.

### Run

```
dotnet run --project src/CsTegraRcmGui
```

## Usage

1. Complete the platform setup above.
2. Put the device into RCM mode and plug it in.
3. Launch the app and browse to a `payload.bin`.
4. Optionally save it as a favorite for quick reuse.
5. Click **Inject payload**.

## Building a release

TODO

## License notices

Third-party components bundled with this app are documented in
[`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md).
