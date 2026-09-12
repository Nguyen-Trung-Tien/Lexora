; =====================================================================
; Lexora - Inno Setup Packaging Script
; WinUI 3 Unpackaged (Self-Contained x64)
; =====================================================================

#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif

#ifndef MyAppSourceDir
  #define MyAppSourceDir "..\publish\win-x64"
#endif

#ifndef MyOutputDir
  #define MyOutputDir "..\publish\installer"
#endif

#define MyAppName "Lexora"
#define MyAppPublisher "Lexora Team"
#define MyAppURL "https://github.com/Nguyen-Trung-Tien/Lexora"
#define MyAppExeName "Lexora.Presentation.exe"
#define MyAppIcon "..\src\Lexora.Presentation\Assets\AppIcon.ico"

[Setup]
; Unique App ID (DO NOT CHANGE across versions to maintain update compatibility)
AppId={{4E7A7D6C-8451-409B-89E2-7D6195E95E4A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} v{#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir={#MyOutputDir}
OutputBaseFilename=Lexora-Setup-v{#MyAppVersion}
SetupIconFile={#MyAppIcon}
WizardImageFile=wizard-large.bmp
WizardSmallImageFile=wizard-small.bmp
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern

; 64-bit Windows configuration
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Dual-mode installation: allows per-user (no admin) or all-users (admin)
PrivilegesRequiredOverridesAllowed=commandline dialog

; Close running instance of Lexora before upgrade/uninstall
CloseApplications=yes
CloseApplicationsFilter=*.exe

; Add/Remove Programs metadata
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MyAppSourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Custom routine to optionally prompt user to keep their SQLite database & user progress
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    DataDir := ExpandConstant('{localappdata}\Lexora');
    if DirExists(DataDir) then
    begin
      if MsgBox('Do you want to delete your study data, vocabulary, and streak history stored in ' + DataDir + '?', mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
      begin
        DelTree(DataDir, True, True, True);
      end;
    end;
  end;
end;
