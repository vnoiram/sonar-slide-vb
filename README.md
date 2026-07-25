# SonarSlideVB

VoiceMeeter Banana/Potato inside parameters are controlled from a small Windows tray app to mimic SteelSeries Nova 7 style ChatMix.

## Behavior

- Runs as a tray app.
- Uses VoiceMeeter Remote API.
- Adjusts two configured VoiceMeeter parameters, usually `Strip[n].Gain` or `Bus[n].Gain`.
- Default hotkeys:
  - Toggle enabled: `Ctrl+Alt+M`
  - Game side: `Ctrl+Alt+Left`
  - Chat side: `Ctrl+Alt+Right`
  - Center: `Ctrl+Alt+Down`

The Settings window shows VoiceMeeter-style names instead of Remote API parameter names. Select the layout first, then choose Game and Chat from the dropdowns.

Default layout is VoiceMeeter Banana:

- Game: `Voicemeeter Input`
- Chat: `Voicemeeter AUX Input`

Internally these are saved as VoiceMeeter Remote API parameters such as `Strip[3].Gain` and `Strip[4].Gain`.

## Nova 7 dial

Nova 7 dial support is enabled by default. The app looks for SteelSeries `VID 1038` / `PID 2202` HID devices and prioritizes the vendor usage page used by the ChatMix report. Reports are written to `%AppData%\SonarSlideVB\app.log`.

The expected ChatMix report is compatible with the Linux reference implementations: marker `0x45` on Nova 7 USB dongles, with the following two bytes representing the Game/Chat balance. The app also accepts `0x2D` as an alternate marker seen in related notes. It normalizes that value to `0..100` and applies it to the configured VoiceMeeter Game/Chat targets.

## Build

Review `scripts/build.ps1`, then run it in PowerShell. The script uses Visual Studio MSBuild and does not require the .NET SDK command-line tools.

```powershell
.\scripts\build.ps1
```

The release output is written to `artifacts\publish`.

## VoiceMeeter DLL

If the app cannot find `VoicemeeterRemote64.dll`, open Settings from the tray icon and select the DLL manually.
