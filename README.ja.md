# SonarSlideVB

English version: [README.md](README.md)

<img src="assets/app-icon.png" alt="SonarSlideVB icon" width="128">

VoiceMeeter Banana/Potato の内部パラメータを小さな Windows tray app から制御し、SteelSeries Nova 7 風の ChatMix を再現します。

## 動作

- tray app として動作します。
- VoiceMeeter Remote API を使います。
- 通常は `Strip[n].Gain` または `Bus[n].Gain` である、設定済みの 2 つの VoiceMeeter parameter を調整します。
- 既定 hotkey:
  - 有効/無効の切り替え: `Ctrl+Alt+M`
  - Game 側: `Ctrl+Alt+Left`
  - Chat 側: `Ctrl+Alt+Right`
  - Center: `Ctrl+Alt+Down`

Settings window では Remote API parameter 名ではなく VoiceMeeter 風の名前を表示します。先に layout を選び、その後 dropdown から Game と Chat を選択します。

Settings には次の項目もあります。

- Dial mode:
  - `Nova 7`: 対応する SteelSeries Nova 7 HID ChatMix report を直接読み取ります。
  - `Custom`: diagnostics 用の現在の HID/raw input capture path を有効にしますが、ChatMix の変更はまだ適用しません。
  - `Off`: dial monitoring を停止します。
- Nova 7 retry ms: device discovery/open/read retry 間の delay。
- Nova 7 retry count: 連続 retry の上限。`0` は無期限 retry を意味します。
- Step: dial-scale 単位で表示されます。既定表示値 `1.0` は内部的には `0.1` として保存されます。
- Start with Windows: 現在の user の Windows startup registry key から tray app を起動します。

既定 layout は VoiceMeeter Banana です。

- Game: `Voicemeeter Input`
- Chat: `Voicemeeter AUX Input`

内部的には `Strip[3].Gain` や `Strip[4].Gain` のような VoiceMeeter Remote API parameter として保存されます。

## Nova 7 dial

Nova 7 dial support は `Dial mode: Nova 7` により既定で有効です。アプリは SteelSeries `VID 1038` / `PID 2202` の HID device を探し、ChatMix report で使われる vendor usage page を優先します。summary diagnostics は `%AppData%\SonarSlideVB\app.log` に書き込まれます。

想定する ChatMix report は Linux HID patch や community implementation と互換です。Nova 7 USB dongle 上の marker `0x45` と、その後ろ 2 bytes が Game/Chat balance の `0..100` range を表します。関連 note で見られる alternate marker `0x2D` も受け付けます。この値を `0..100` に正規化し、設定済みの VoiceMeeter Game/Chat target に適用します。

`Dial mode: Custom` は現在 capture-only です。`app.log` に HID/raw input diagnostics を集める用途には使えますが、custom device mapping と ChatMix application はまだ support していません。keyboard raw input value は記録しません。`app.log` が 1 MB を超えると、以前の file は `app.log.1` に rotate されます。

## Build

`scripts/build.ps1` を確認してから PowerShell で実行します。この script は Visual Studio MSBuild を使い、.NET SDK command-line tools は不要です。

```powershell
.\scripts\build.ps1
```

release output は `artifacts\publish` に書き込まれます。

## Install

tagged release は Windows x64 向けに 2 つの asset を提供します。

- `SonarSlideVB-vX.Y.Z-win-x64-installer.exe`: administrator 権限なしで user ごとに `%LocalAppData%\Programs\SonarSlideVB` へ install し、Start Menu shortcut を追加します。
- `SonarSlideVB-vX.Y.Z-win-x64-standalone.zip`: portable build です。展開して `SonarSlideVB.exe` を直接実行します。

どちらも同じ application build を含みます。user configuration と log は、どちらを使っても `%AppData%\SonarSlideVB` に保存されます。

## VoiceMeeter DLL

アプリが `VoicemeeterRemote64.dll` を見つけられない場合は、tray icon から Settings を開き、DLL を手動で選択してください。
