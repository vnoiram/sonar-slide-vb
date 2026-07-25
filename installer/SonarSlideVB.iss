#ifndef AppVersion
  #error "AppVersion must be defined, e.g. ISCC.exe SonarSlideVB.iss /DAppVersion=0.1.0"
#endif

#define AppName "SonarSlideVB"
#define AppPublisher "SonarSlideVB"
#define AppExeName "SonarSlideVB.exe"

[Setup]
AppId={{B7C6E0B0-6C6C-4E9C-9A7E-3B7E7E8B6E6E}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\SonarSlideVB
DefaultGroupName=SonarSlideVB
DisableProgramGroupPage=yes
DisableDirPage=yes
DisableWelcomePage=no
PrivilegesRequired=lowest
OutputDir=..\artifacts
OutputBaseFilename=SonarSlideVB-v{#AppVersion}-win-x64-installer
SetupIconFile=..\assets\app-icon.ico
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Files]
Source: "..\artifacts\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#AppExeName}"

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch SonarSlideVB"; Flags: nowait postinstall skipifsilent
