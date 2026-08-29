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

Grab the latest zip for your OS from the
[Releases page](../../releases/latest). Each zip is self-contained (bundles
the .NET runtime — no separate SDK/runtime install needed) and includes the
OS-specific setup README plus licensing docs:

- Windows: `CsTegraRcmGui-<version>-win-x64.zip`
- Linux: `CsTegraRcmGui-<version>-linux-x64.zip`

Extract it, complete the one-time OS setup step from the bundled README, then
run the executable inside.

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

Releases are versioned with [SemVer](https://semver.org/) (major = breaking,
minor = feature, patch = fix) and built automatically. To cut one:

```
git tag v1.2.0
git push origin v1.2.0
```

Pushing a `v*` tag triggers [`.github/workflows/release.yml`](.github/workflows/release.yml),
which publishes self-contained Windows and Linux zips and attaches them to a
GitHub Release with auto-generated notes. macOS isn't built yet — it needs
local testing first (see the note at the top of that workflow).

## Credits

The RCM payload layout and the platform-specific trigger workarounds are
ported from [JTegraNX](https://github.com/dylwedma11748/JTegraNX)
(`src/main/java/rcm/RCM.java`), licensed GPL-2.0. This project's license
follows from that.

## License

GPL-2.0 — see [`LICENSE`](LICENSE).

Third-party components bundled with this app are documented in
[`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md).
