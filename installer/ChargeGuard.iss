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
PrivilegesRequired=admin
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
// Helper function to split a string by a delimiter
function SplitString(const S: String; const Delimiter: String): TArrayOfString;
var
  I, Start, Count: Integer;
  DelimLen: Integer;
begin
  DelimLen := Length(Delimiter);
  if DelimLen = 0 then
  begin
    SetArrayLength(Result, 1);
    Result[0] := S;
    Exit;
  end;
  
  Count := 0;
  Start := 1;
  
  // First pass: count the number of parts
  for I := 1 to Length(S) - DelimLen + 1 do
  begin
    if Copy(S, I, DelimLen) = Delimiter then
    begin
      Count := Count + 1;
      Start := I + DelimLen;
    end;
  end;
  
  // Add the last part
  if Start <= Length(S) then
    Count := Count + 1;
  
  // Allocate array
  SetArrayLength(Result, Count);
  
  // Second pass: fill the array
  Count := 0;
  Start := 1;
  for I := 1 to Length(S) - DelimLen + 1 do
  begin
    if Copy(S, I, DelimLen) = Delimiter then
    begin
      Result[Count] := Copy(S, Start, I - Start);
      Count := Count + 1;
      Start := I + DelimLen;
    end;
  end;
  
  // Add the last part
  if Start <= Length(S) then
    Result[Count] := Copy(S, Start, Length(S) - Start + 1);
end;

// Helper function to compare version strings (e.g., "9.0.0" vs "8.0.0")
// Returns: -1 if V1 < V2, 0 if V1 = V2, 1 if V1 > V2
function CompareVersion(V1, V2: String): Integer;
var
  Parts1, Parts2: TArrayOfString;
  I, MaxLen: Integer;
  Num1, Num2: Integer;
begin
  Result := 0;
  
  // Split version strings by dots
  Parts1 := SplitString(V1, '.');
  Parts2 := SplitString(V2, '.');
  
  // Determine maximum length to compare
  MaxLen := GetArrayLength(Parts1);
  if GetArrayLength(Parts2) > MaxLen then
    MaxLen := GetArrayLength(Parts2);
  
  // Compare each part
  for I := 0 to MaxLen - 1 do
  begin
    // Default to 0 if part doesn't exist
    if I < GetArrayLength(Parts1) then
      Num1 := StrToIntDef(Parts1[I], 0)
    else
      Num1 := 0;
      
    if I < GetArrayLength(Parts2) then
      Num2 := StrToIntDef(Parts2[I], 0)
    else
      Num2 := 0;
    
    if Num1 < Num2 then
    begin
      Result := -1;
      Exit;
    end
    else if Num1 > Num2 then
    begin
      Result := 1;
      Exit;
    end;
  end;
  
  // Versions are equal
  Result := 0;
end;

// Check if .NET 9.0 Desktop Runtime is installed
function IsNet90DesktopRuntimeInstalled: Boolean;
var
  Runtimes: TArrayOfString;
  RegistryKey: String;
  I: Integer;
  MinimumVersion: String;
begin
  Result := False;
  MinimumVersion := '9.0.0';
  
  // Check for .NET Desktop Runtime in the registry (x64)
  RegistryKey := 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App';
  
  if RegGetValueNames(HKLM, RegistryKey, Runtimes) then
  begin
    for I := 0 to GetArrayLength(Runtimes) - 1 do
    begin
      // Check if the installed version is >= 9.0.0
      if CompareVersion(Runtimes[I], MinimumVersion) >= 0 then
      begin
        Result := True;
        Exit;
      end;
    end;
  end;
end;

function InitializeSetup(): Boolean;
var
  NetRuntimeInstalled: Boolean;
begin
  // Check if .NET 9.0 Desktop Runtime is installed
  NetRuntimeInstalled := IsNet90DesktopRuntimeInstalled;

  // Show information about .NET 9.0 requirement
  if not NetRuntimeInstalled then
  begin
    if MsgBox('ChargeGuard requires .NET 9.0 Desktop Runtime to run.' + #13#10 +
              'It was not detected on your system.' + #13#10 + #13#10 +
              'You can download it from:' + #13#10 +
              'https://dotnet.microsoft.com/download/dotnet/9.0' + #13#10 + #13#10 +
              'Do you want to continue with the installation anyway?', mbConfirmation, MB_YESNO) = IDNO then
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
