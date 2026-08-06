; ChargeGuard Installer Script
; Requires Inno Setup 6.x or later

[Setup]
AppName=ChargeGuard
AppVersion=1.0.0
AppPublisher=ChargeGuard
AppPublisherURL=https://github.com/yourusername/ChargeGuard
AppSupportURL=https://github.com/yourusername/ChargeGuard/issues
AppUpdatesURL=https://github.com/yourusername/ChargeGuard/releases
DefaultDirName={localappdata}\ChargeGuard
DefaultGroupName=ChargeGuard
AllowNoIcons=yes
OutputBaseFilename=ChargeGuard-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
; Install for current user only (no elevation required)

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
  ErrorCode: Integer;
  NetRuntimeInstalled: Boolean;
  ResultCode: Integer;
begin
  // Check if .NET 9.0 Desktop Runtime is installed
  // This is a basic check - in production, you might want to use a more robust method
  NetRuntimeInstalled := False;

  // Try to detect .NET 9.0 Desktop Runtime
  // This is a simplified check - adjust as needed for your specific requirements
  if RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', ResultCode) then
  begin
    // This checks for .NET Framework, not .NET Core/.NET 5+
    // For .NET 9.0, you would need to check for the specific runtime
    // For now, we'll just warn the user
  end;

  // Show a message if .NET runtime might not be installed
  // In production, implement proper .NET 9.0 detection
  if not NetRuntimeInstalled then
  begin
    if MsgBox('ChargeGuard requires .NET 9.0 Desktop Runtime to run.' + #13#10 +
              'If you are not sure it is installed, you can download it from:' + #13#10 +
              'https://dotnet.microsoft.com/download/dotnet/9.0' + #13#10 + #13#10 +
              'Do you want to continue with the installation?', mbConfirmation, MB_YESNO) = IDNO then
    begin
      Result := False;
      Exit;
    end;
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
