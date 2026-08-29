# Linux setup

One-time, per-machine step. Everything else the app needs comes from your
distro's packages, so no bundled binary is required.

## Why this is needed

Linux already has a generic in-kernel driver for any USB device, so the app
can open it as-is — the only thing missing is permission. By default, raw
USB device nodes are only writable by root, so without a rule granting
access, the app would need to run as root to talk to the device.

## Steps

Install the udev rule:

```
sudo cp packaging/linux/udev/99-cstegrarcmgui.rules /etc/udev/rules.d/
sudo udevadm control --reload-rules
sudo udevadm trigger
```

This does two things — see
[`packaging/linux/udev/99-cstegrarcmgui.rules`](udev/99-cstegrarcmgui.rules)
for the rule itself:

- Grants unprivileged read/write access to the device (VID `0955`, PID
  `7321` — a Tegra device in USB recovery mode/RCM) so the app doesn't need
  root.
- Tells ModemManager (if installed/running) to leave the device alone. It
  auto-probes unknown USB devices to check whether they're modems, and even
  briefly touching this device while a payload transfer is in progress is
  enough to stall the bulk endpoints.

That's it — plug the device in (or replug it if it was already connected
before installing the rule) and the app should be able to open it.
