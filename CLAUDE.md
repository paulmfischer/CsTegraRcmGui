# Working in this repo

## Project shape

- `src/CsTegraRcmGui` — Avalonia GUI app (MVVM, CommunityToolkit.Mvvm).
- `src/CsTegraRcmGui.Core` — platform-agnostic services/models (settings,
  logging, RCM device access).
- `tools/usbwatch` — standalone USB diagnostics helper.
- `packaging/` — platform-specific setup docs/rules (udev on Linux, driver
  binding notes on Windows).

## Build/run

```
dotnet build
dotnet run --project src/CsTegraRcmGui
```

Target framework is net10.0.

## Git commits

- Do not add `Claude-Session: ...` trailers to  commit messages.
- Only commit when explicitly asked. Let the user review the diff first.
- Prefer new commits over amending, unless asked to amend.
- Prefix commit subjects with a Conventional Commits type (`feat:`, `fix:`,
  `chore:`, `docs:`, `refactor:`, `test:`, `ci:`, etc.) — this repo's
  release notes are auto-generated from commit/PR history, and direct
  commits (not through a PR) show up there by subject line alone.
