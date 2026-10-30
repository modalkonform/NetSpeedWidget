# NetSpeed Widget

Shows the current download (↓) and upload (↑) speed on the Windows 11 taskbar,
just left of the tray icons.

- Follows the network adapter Windows currently uses for the internet
  (switches automatically between Wi-Fi, Ethernet, …).
- Updates once per second and uses your system's number format.
- Hides while a fullscreen app (video, game, presentation) is in front.
- Matches a light or dark taskbar.

## Usage

Run `NetSpeedWidget.exe`. Right-click the numbers for:

- **Bits (Kbps, Mbps)** / **Bytes (KB/s, MB/s)**
- **Font size +** / **Font size −** (10–18 px, 1 px per click; the menu stays
  open so you can click repeatedly) and **Reset font size** (back to 12 px)
- **Start with Windows** (current user only, no admin rights needed)
- **Exit**

Unit and font size are saved in `%APPDATA%\NetSpeedWidget\settings.json`.

## Build

Requires the .NET 10 SDK.

```sh
dotnet test
dotnet publish src/NetSpeedWidget -c Release -r win-x64 --self-contained false \
  -p:PublishSingleFile=true -o publish
```

The result is a single `publish\NetSpeedWidget.exe`. It needs the .NET 10
Desktop Runtime on the machine it runs on.
