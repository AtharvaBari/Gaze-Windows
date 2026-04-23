; Gaze Windows Installer Script
; Created for Inno Setup

[Setup]
AppId={{D3E8C1F2-BC12-4587-9F33-9DBA3F5DCBF8}}
AppName=Gaze
AppVersion=1.0.0
AppPublisher=Atharva Bari
AppPublisherURL=https://github.com/AtharvaBari/Gaze-Windows
AppSupportURL=https://github.com/AtharvaBari/Gaze-Windows
AppUpdatesURL=https://github.com/AtharvaBari/Gaze-Windows
DefaultDirName={autopf}\Gaze
DefaultGroupName=Gaze
AllowNoIcons=yes
; The installer file name
OutputBaseFilename=Gaze_Windows_v1.0.0
; Visuals
SetupIconFile=Gaze\icon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "Gaze\bin\Release\net8.0-windows\win-x64\publish\Gaze.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "Gaze\icon.png"; DestDir: "{app}"; Flags: ignoreversion
; Add any other files from the publish folder here

[Icons]
Name: "{group}\Gaze"; Filename: "{app}\Gaze.exe"
Name: "{autodesktop}\Gaze"; Filename: "{app}\Gaze.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Gaze.exe"; Description: "{cm:LaunchProgram,Gaze}"; Flags: nowait postinstall skipifsilent
