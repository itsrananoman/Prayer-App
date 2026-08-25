; Inno Setup Script for Prayer Application (Self-Contained Single-File Package)
#define MyAppName "Prayer"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "DevCrafters"
#define MyAppCopyright "Developed by Rana Noman"
#define MyAppExeName "Prayer.exe"
#define MyAppIcon "..\Resources\Icons\Prayer.ico"

[Setup]
AppId={{C6D2D44E-29BE-45E5-9118-8A33D0DE46C8}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppCopyright={#MyAppCopyright}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\bin\Installer
OutputBaseFilename=PrayerSetup_v1.1.0
SetupIconFile={#MyAppIcon}
UninstallDisplayIcon={app}\{#MyAppExeName},0
WizardImageFile=WizardImage.bmp
WizardSmallImageFile=WizardSmallImage.bmp
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "Launch Prayer automatically on Windows startup (minimized to system tray)"; GroupDescription: "Startup Options:"

[Files]
Source: "..\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Resources\Icons\Prayer.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Resources\Icons\app_icon.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Resources\Icons\Prayer.ico"; DestDir: "{app}\Resources\Icons"; Flags: ignoreversion
Source: "..\Resources\Icons\app_icon.ico"; DestDir: "{app}\Resources\Icons"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; IconIndex: 0; AppUserModelID: "DevCrafters.Prayer.FocusLock.1.0"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"; IconFilename: "{app}\{#MyAppExeName}"; IconIndex: 0
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; IconIndex: 0; AppUserModelID: "DevCrafters.Prayer.FocusLock.1.0"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--minimized"; IconFilename: "{app}\{#MyAppExeName}"; IconIndex: 0; AppUserModelID: "DevCrafters.Prayer.FocusLock.1.0"; Tasks: autostart

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Clean up user data and autostart registry on uninstall if requested
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  AppDataPath: String;
begin
  if CurUninstallStep = usUninstall then
  begin
    // Remove autostart registry key
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'PrayerFocusLock');

    // Prompt user about keeping settings & database
    if MsgBox('Would you like to keep your saved settings and prayer database on this computer?', mbConfirmation, MB_YESNO or MB_DEFBUTTON1) = IDNO then
    begin
      AppDataPath := ExpandConstant('{userappdata}\PrayerApp');
      if DirExists(AppDataPath) then
      begin
        DelTree(AppDataPath, True, True, True);
      end;
    end;
  end;
end;
