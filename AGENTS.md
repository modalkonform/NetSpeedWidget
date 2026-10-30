# AGENTS.md

Guidance for coding agents working in this repository.

NetSpeedWidget is a small Windows 11 app (C# / .NET 10, Windows Forms) that draws the
current download and upload speed on the taskbar, just left of the tray. It is
Windows-only: the app and the tests do not build or run elsewhere.

## Commands

```sh
dotnet build NetSpeedWidget.slnx
dotnet test NetSpeedWidget.slnx
dotnet test NetSpeedWidget.slnx --filter "FullyQualifiedName~SpeedFormatterTests"   # one class
dotnet test NetSpeedWidget.slnx --filter "FullyQualifiedName~SmallerFont_StopsAt10" # one test
dotnet publish src/NetSpeedWidget -c Release -r win-x64 --self-contained false \
  -p:PublishSingleFile=true -o publish
```

- `publish` fails while `publish\NetSpeedWidget.exe` is running (file lock). Stop it
  first: `Stop-Process -Name NetSpeedWidget`.
- No linter or formatter is configured.
- The development machine uses a German locale, so local `dotnet` output is German
  ("Bestanden!" = passed, "Fehler" = failed). CI output is English.

## Architecture

The code is split on purpose into two kinds:

- **Pure logic, unit-tested** in `tests/NetSpeedWidget.Tests`: `SpeedFormatter` (unit
  scaling, culture-aware), `TrafficRate` (rate between two counter samples; zero on
  adapter switch, counter reset or missing sample), `WidgetPlacement`, `Settings`
  (font-size steps and limits) with `SettingsStore` (JSON), and `StartupRegistration`
  (HKCU Run key).
- **Windows glue, verified by running the app**: `TaskbarWidget` (the window; a 1 s
  timer samples, computes the rate and redraws), `Taskbar` (taskbar/tray bounds,
  light or dark theme), `ActiveAdapterMonitor`, `NativeMethods` (P/Invoke).

Types used by tests are `public` because xUnit test methods are public and take them
as parameters; there is no `InternalsVisibleTo`.

How the window works (all in `TaskbarWidget`):

- Windows 11 has no taskbar deskbands, so the widget is a borderless
  `WS_EX_LAYERED | TOOLWINDOW | TOPMOST | NOACTIVATE` window placed over the taskbar.
- Content is drawn into a 32-bpp ARGB bitmap and pushed with `UpdateLayeredWindow`
  (per-pixel alpha); WinForms painting is not used. The background has alpha 1, not 0,
  so the whole widget still receives right-clicks.
- Clicking the taskbar raises it above the widget. An `EVENT_SYSTEM_FOREGROUND`
  WinEvent hook and every timer tick re-assert `HWND_TOPMOST`.
- `Shell_TrayWnd` → child `TrayNotifyWnd` gives the tray rectangle. Placement is
  recomputed every tick, so the widget follows tray width changes.
- The widget hides while `SHQueryUserNotificationState` reports a fullscreen app or
  presentation.
- Sizes are pixels at 100 % scaling, multiplied by `DeviceDpi / 96` (PerMonitorV2).
  The width is fixed per unit, measured from the widest possible value, so the widget
  does not jump as numbers change.

The active adapter is found with `GetBestInterface` for 8.8.8.8 (a route lookup only;
nothing is sent), matched to a `NetworkInterface` by IPv4 index. IPv6-only networks
therefore show 0.

## Gotchas

- An older third-party tool named "NetSpeedMonitor" uses `%APPDATA%\NetSpeedMonitor`.
  This app uses `NetSpeedWidget` for its settings folder, Run-key value and
  single-instance mutex. Never write to or delete the other tool's folder.
- Settings live in `%APPDATA%\NetSpeedWidget\settings.json` (`Unit`, `FontSize`). New
  fields need defaults so older files still load; a test covers this.
- Font size is limited to 10–18 px; 18 px is the largest size at which two lines fit
  the standard 48 px taskbar.
- `StartupRegistrationTests` write to the real registry under
  `HKCU\Software\NetSpeedWidget.Tests` and delete that key afterwards.
- Agent safety checks block scripts that write the real
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` key. Ask the user to toggle
  "Start with Windows" in the widget's right-click menu instead.

## Verifying UI changes

Unit tests do not cover the window. Run the built exe, wait a few seconds for it to
start, then screenshot the right end of the taskbar. Capture with `BitBlt` and the
`CAPTUREBLT` flag (for example from PowerShell with `System.Drawing`) so the layered
window is included.

## CI

`.github/workflows/tests.yml` runs `dotnet test --logger GitHubActions` on
`windows-latest` for pull requests and on manual dispatch; results appear in the job's
step summary. There is deliberately no `actions/setup-dotnet`: the runner image already
includes the .NET 10 SDK, and downloading the newest patch cost about 40 s per run.
Re-add it only if the image lacks a needed SDK.

## Conventions

- 2-space indentation in every file (not the C# default of 4); lines of at most 100
  characters.
- XML doc comments (`/// <summary>`) on types and public methods; other comments only
  for non-obvious logic.
- Test-first for the pure-logic classes: write the failing test, then the change.
- Conventional Commits, with no AI-assistant attribution in commit messages. Changes
  reach `main` through pull requests.
