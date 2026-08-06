; ChargeGuard Installer Script
; Requires Inno Setup 6.x or later

[Setup]
AppName=ChargeGuard
AppVersion=1.0.0
AppPublisher=Rupesh Bhurke
AppPublisherURL=https://github.com/rupeshbhurke/ChargeGuard
AppSupportURL=https://github.com/rupeshbhurke/ChargeGuard/issues
AppUpdatesURL=https://github.com/rupeshbhurke/ChargeGuard/releases
DefaultDirName={localappdata}\ChargeGuard
DefaultGroupName=ChargeGuard
AllowNoIcons=yes
OutputBaseFilename=ChargeGuard-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
; Install for current user only (no elevation required)
AppCopyright=Copyright (C) 2024 Rupesh Bhurke
UninstallDisplayIcon={app}\ChargeGuard.exe

; .NET Runtime check
; Note: This checks if .NET Desktop Runtime is installed
; User should install .NET 9.0 Desktop Runtime if not present

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop icon"; GroupDescription: "Additional icons:"
Name: "startup"; Description: "Start ChargeGuard with Windows"; GroupDescription: "Additional options:"

[Files]
; Main application files
Source: "..\src\ChargeGuard\bin\Release\net9.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\ChargeGuard"; Filename: "{app}\ChargeGuard.exe"; Comment: "Battery Charging Alert Utility"
Name: "{group}\Uninstall ChargeGuard"; Filename: "{uninstallexe}"
Name: "{autodesktop}\ChargeGuard"; Filename: "{app}\ChargeGuard.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\ChargeGuard.exe"; Description: "Launch ChargeGuard"; Flags: nowait postinstall skipifsilent

[Registry]
; Startup registration - only if user selected the task
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "ChargeGuard"; ValueData: """{app}\ChargeGuard.exe"""; Tasks: startup; Flags: uninsdeletevalue

[UninstallDelete]
; Remove user data directory if user chooses
; Note: We don't automatically remove settings to preserve user preferences
; User can manually delete %LocalAppData%\ChargeGuard if desired

[Code]
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
  NetRuntimeInstalled: Boolean;
begin
  // Check if .NET 9.0 Desktop Runtime is installed
  NetRuntimeInstalled := False;

  // Check for .NET 9.0 Desktop Runtime in the registry
  // This checks for the x64 version
  if RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', ResultCode) then
  begin
    // This is a basic check for .NET Framework
    // For .NET 5+, we would need to check different registry keys
    // For now, we'll proceed with a warning
  end;

  // Show information about .NET 9.0 requirement
  if MsgBox('ChargeGuard requires .NET 9.0 Desktop Runtime to run.' + #13#10 +
            'If you are not sure it is installed, you can download it from:' + #13#10 +
            'https://dotnet.microsoft.com/download/dotnet/9.0' + #13#10 + #13#10 +
            'Do you want to continue with the installation?', mbConfirmation, MB_YESNO) = IDNO then
  begin
    Result := False;
    Exit;
  end;

  Result := True;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    // Ask user if they want to remove user data
    if MsgBox('Do you want to remove ChargeGuard settings and logs?' + #13#10 +
              'This will delete all your preferences and log files.', mbConfirmation, MB_YESNO) = IDYES then
    begin
      DelTree(ExpandConstant('{localappdata}\ChargeGuard'), True, True, True);
    end;
  end;
end;
