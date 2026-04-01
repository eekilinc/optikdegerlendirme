; Optik Değerlendirme Installer Script
#define MyAppName "Optik Değerlendirme"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "AĞLASUN MYO"
#define MyAppURL "https://github.com/eekilinc/optikdegerlendirme"
#define MyAppExeName "OptikFormApp.exe"

[Setup]
AppId={{E3F5A7B2-C4D8-4E1F-9A2B-3C6D8E5F9A1B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=LICENSE
OutputDir=.
OutputBaseFilename=OptikDegerlendirme-v{#MyAppVersion}-Setup
; SetupIconFile=..\Assets\app_icon_new.png
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequiredOverridesAllowed=dialog
; WizardImageFile=..\Assets\about_banner.png
; WizardSmallImageFile=..\Assets\app_icon_new.png

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "Hızlı Başlatma Alanı Oluştur"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "associate"; Description: ".opt dosyalarını ilişkilendir"; GroupDescription: "Dosya İlişkilendirmeleri"; Flags: unchecked

[Files]
Source: "publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; IconIndex: 0
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; IconFilename: "{app}\{#MyAppExeName}"; IconIndex: 0
Name: "{autoprograms}\{#MyAppName}\Uninstall"; Filename: "{uninstallexe}"
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKCR; Subkey: ".opt"; ValueType: string; ValueName: ""; ValueData: "OptikDegerlendirme"; Flags: uninsdeletevalue; Tasks: associate
Root: HKCR; Subkey: "OptikDegerlendirme"; ValueType: string; ValueName: ""; ValueData: "Optik Değerlendirme Dosyası"; Flags: uninsdeletekey; Tasks: associate
Root: HKCR; Subkey: "OptikDegerlendirme\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"; Tasks: associate
Root: HKCR; Subkey: "OptikDegerlendirme\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: associate

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
end;

function GetCustomSetupExitCode(ExitCode: Integer): Integer;
begin
  Result := ExitCode;
end;
