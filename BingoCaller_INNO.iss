; BingoCaller -- Inno Setup 6 installer script
;
; Single-file, framework-dependent .NET 8 publish. Easiest: run Build-Release.ps1 (publishes, compiles this
; script and writes the SHA256). By hand:
;   dotnet publish BingoCaller\BingoCaller.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish_BingoCaller
;   ISCC.exe BingoCaller_INNO.iss          (installer lands in .\dist)
;
; Keep MyAppVersion in step with <Version> in BingoCaller\BingoCaller.csproj and CHANGELOG.md
; (Build-Release.ps1 refuses to build if they differ).
;
; DESIGN NOTE -- per-user install on purpose:
;   The program saves BingoCaller.settings.json NEXT TO the exe (portable, no registry, no AppData).
;   A per-user install (PrivilegesRequired=lowest) puts the folder where the user can write, with no
;   elevation. Installing to Program Files and then granting "Users: modify" on the folder would let any
;   local user replace the exe/DLLs (privilege escalation), so this script deliberately does neither.

#define MyAppName      "BingoCaller"
#define MyAppVersion   "1.0.0"
#define MyAppPublisher "Jochen Thieren"
; Assumed repository address -- change if the repository is named differently.
#define MyAppURL       "https://github.com/JochenThieren/BingoCaller"
#define MyAppExeName   "BingoCaller.exe"
#define BinDir         SourcePath + "publish_BingoCaller"

[Setup]
; AppId uniquely identifies this application -- never reuse it in another installer.
AppId={{01EFA820-54BA-4895-934C-29C582F06BAC}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}/releases
VersionInfoVersion={#MyAppVersion}
; Per-user install: no administrator rights, folder is user-writable (see design note above).
PrivilegesRequired=lowest
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
DisableDirPage=no
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
MinVersion=10.0
OutputDir={#SourcePath}dist
OutputBaseFilename=setup_{#MyAppName}_{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
LicenseFile={#SourcePath}LICENSE
UninstallDisplayIcon={app}\{#MyAppExeName}
; Wizard/uninstaller branding; the exe embeds the same icon (ApplicationIcon in the csproj).
SetupIconFile={#SourcePath}BingoCaller\BingoCaller.ico
; Not code-signed (no certificate): integrity is via the SHA256 file published with each release.

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "dutch";   MessagesFile: "compiler:Languages\Dutch.isl"
Name: "french";  MessagesFile: "compiler:Languages\French.isl"
Name: "german";  MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Single-file .NET 8 executable
Source: "{#BinDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
; Governance / legal (small text files; the SBOM is shipped so it can be obtained from an installed copy)
Source: "{#SourcePath}LICENSE";      DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}README.md";    DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}SECURITY.md";  DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}CHANGELOG.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}sbom.json";    DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}";                       Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:ProgramOnTheWeb,{#MyAppName}}";  Filename: "{#MyAppURL}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}";                 Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; The only file the program creates itself (its settings). Removing it is the "secure removal" of user data.
Type: files; Name: "{app}\BingoCaller.settings.json"

[Code]
// .NET 8 Desktop Runtime (x64) check. The registry key is written by the Microsoft runtime installer and is
// readable without administrator rights; `dotnet --list-runtimes` is a fallback for unusual installs.
function IsDotNet8DesktopInstalledRegistry: Boolean;
var
  Names: TArrayOfString;
  I: Integer;
begin
  Result := False;
  if RegGetValueNames(HKLM64, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App', Names) then
    for I := 0 to GetArrayLength(Names) - 1 do
      if Pos('8.', Names[I]) = 1 then
      begin
        Result := True;
        Exit;
      end;
end;

function IsDotNet8DesktopInstalledCli: Boolean;
var
  ResultCode: Integer;
  Output: AnsiString;
  TmpFile: String;
begin
  Result := False;
  TmpFile := ExpandConstant('{tmp}\dotnet_check.txt');
  if Exec(ExpandConstant('{cmd}'), '/c dotnet --list-runtimes > "' + TmpFile + '" 2>&1', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    if LoadStringFromFile(TmpFile, Output) then
      Result := Pos('Microsoft.WindowsDesktop.App 8.', String(Output)) > 0;
    DeleteFile(TmpFile);
  end;
end;

function InitializeSetup: Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;
  if WizardSilent then Exit;   // unattended installs: never block on a dialog

  if not (IsDotNet8DesktopInstalledRegistry or IsDotNet8DesktopInstalledCli) then
  begin
    if MsgBox(
      'BingoCaller needs the .NET 8 Desktop Runtime (x64), which is not installed on this PC.' + #13#10 + #13#10 +
      'Click YES to open the Microsoft download page (install the runtime, then run this setup again).' + #13#10 +
      'Click NO to continue anyway (the program will not start until the runtime is installed).',
      mbError, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
      Result := False;
    end;
  end;
end;
