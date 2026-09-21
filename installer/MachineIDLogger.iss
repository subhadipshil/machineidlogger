; Inno Setup Script for MachineIDLogger
; Production Windows System Utility Installer with Update Detection

#define MyAppName "MachineIDLogger"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Subhadip Shil"
#define MyAppURL "https://github.com/subhadipshil/machineidlogger"
#define MyAppExeName "MachineIDLogger.exe"

[Setup]
AppId={{D93D6535-5C43-4887-98C5-5D681B9F1E35}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
UsePreviousAppDir=yes
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir=..\dist
OutputBaseFilename=MachineIDLogger-Setup-v{#MyAppVersion}
SetupIconFile=..\src\MachineIDLogger\Assets\AppIcon.ico
PrivilegesRequired=admin
ChangesAssociations=yes
CloseApplications=yes
RestartApplications=no
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=Windows System Inspection and Identity Utility
VersionInfoCopyright=Copyright (C) 2026 Subhadip Shil

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[InstallDelete]
Type: files; Name: "{autodesktop}\{#MyAppName}.lnk"
Type: files; Name: "{commondesktop}\{#MyAppName}.lnk"
Type: files; Name: "{userdesktop}\{#MyAppName}.lnk"
Type: files; Name: "{autoprograms}\{#MyAppName}.lnk"

[Files]
Source: "..\src\MachineIDLogger\bin\Release\net10.0-windows10.0.26100.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\src\MachineIDLogger\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\tools\*"; DestDir: "{app}\tools"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; IconFilename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent runascurrentuser

[Code]
var
  IsUpgradeMode: Boolean;
  ExistingVersionStr: String;
  ExistingInstallPath: String;

// Helper to detect if previous version exists in 64-bit or 32-bit registry
function DetectExistingInstallation(): Boolean;
var
  RegKey: String;
  FoundPath: String;
  FoundVer: String;
begin
  Result := False;
  RegKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{D93D6535-5C43-4887-98C5-5D681B9F1E35}_is1';
  FoundPath := '';
  FoundVer := '';

  // 1. Check 64-bit HKLM
  if RegQueryStringValue(HKLM64, RegKey, 'InstallLocation', FoundPath) then
  begin
    RegQueryStringValue(HKLM64, RegKey, 'DisplayVersion', FoundVer);
  end
  // 2. Check 32-bit HKLM
  else if RegQueryStringValue(HKLM, RegKey, 'InstallLocation', FoundPath) then
  begin
    RegQueryStringValue(HKLM, RegKey, 'DisplayVersion', FoundVer);
  end
  // 3. Check HKCU
  else if RegQueryStringValue(HKCU, RegKey, 'InstallLocation', FoundPath) then
  begin
    RegQueryStringValue(HKCU, RegKey, 'DisplayVersion', FoundVer);
  end;

  if FoundPath <> '' then
  begin
    ExistingInstallPath := FoundPath;
    ExistingVersionStr := FoundVer;
    Result := True;
  end;
end;

function InitializeSetup(): Boolean;
var
  MsgPrompt: String;
  PromptChoice: Integer;
begin
  Result := True;
  IsUpgradeMode := False;

  if DetectExistingInstallation() then
  begin
    IsUpgradeMode := True;
    if ExistingVersionStr = '' then ExistingVersionStr := '1.0.0';

    if WizardSilent() then
    begin
      Result := True;
      Exit;
    end;

    if ExistingVersionStr = '{#MyAppVersion}' then
      MsgPrompt := 'MachineIDLogger (v' + ExistingVersionStr + ') is already installed at:' + #13#10#13#10 +
                   '  ' + ExistingInstallPath + #13#10#13#10 +
                   'Do you want to reinstall / update your installation?' + #13#10#13#10 +
                   'All your historical audit logs, SQLite history, and backups in C:\MachineIDLogger will be fully preserved.'
    else
      MsgPrompt := 'An existing installation of MachineIDLogger (v' + ExistingVersionStr + ') was detected at:' + #13#10#13#10 +
                   '  ' + ExistingInstallPath + #13#10#13#10 +
                   'Do you want to UPDATE MachineIDLogger to version {#MyAppVersion}?' + #13#10#13#10 +
                   'All your historical audit logs, SQLite history, and backups in C:\MachineIDLogger will be fully preserved.';

    PromptChoice := MsgBox(MsgPrompt, mbConfirmation, MB_YESNO);
    if PromptChoice = IDNO then
    begin
      Result := False;
      Exit;
    end;
  end;
end;

procedure InitializeWizard();
begin
  if IsUpgradeMode then
  begin
    WizardForm.Caption := 'Update - MachineIDLogger v{#MyAppVersion}';
    WizardForm.WelcomeLabel1.Caption := 'MachineIDLogger Update';
    WizardForm.WelcomeLabel2.Caption := 'Setup will update your existing MachineIDLogger installation to version {#MyAppVersion}.' + #13#10#13#10 +
      'Installation Directory: ' + ExistingInstallPath + #13#10#13#10 +
      'All local history, audit records, and backups in C:\MachineIDLogger will remain intact.' + #13#10#13#10 +
      'Click Next to proceed with the update.';
  end;
end;

// Custom uninstall behavior: Ask user whether to retain audit history and backups
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: String;
  KeepHistory: Integer;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    DataDir := 'C:\MachineIDLogger';
    if DirExists(DataDir) then
    begin
      KeepHistory := MsgBox(
        'MachineIDLogger audit history, backups, and logs are stored in C:\MachineIDLogger.' + #13#10 + #13#10 +
        'Do you want to RETAIN your machine identity history and backups?' + #13#10 + #13#10 +
        'Click Yes to preserve your history for future reinstalls.' + #13#10 +
        'Click No to completely delete all local history and backups.',
        mbConfirmation, MB_YESNO
      );
      
      if KeepHistory = IDNO then
      begin
        DelTree(DataDir, True, True, True);
      end;
    end;
  end;
end;
