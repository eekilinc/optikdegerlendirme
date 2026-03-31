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
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir=.
OutputBaseFilename=OptikDegerlendirme-v{#MyAppVersion}-Setup
SetupIconFile=..\Assets\app_icon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "installer\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "installer\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
