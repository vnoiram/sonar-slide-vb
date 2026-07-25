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

The default targets are `Strip[3].Gain` for Game and `Strip[4].Gain` for Chat. Change these in Settings to match your VoiceMeeter layout.

## Build

Review `scripts/build.ps1`, then run it in PowerShell. The script uses Visual Studio MSBuild and does not require the .NET SDK command-line tools.

```powershell
.\scripts\build.ps1
```

The release output is written to `artifacts\publish`.

## VoiceMeeter DLL

If the app cannot find `VoicemeeterRemote64.dll`, open Settings from the tray icon and select the DLL manually.
