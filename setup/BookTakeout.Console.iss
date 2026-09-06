#define AppId "DaniilPalii.BookTakeout.Console"
#define AppName "Book Takeout"
#define CamelCaseAppName "BookTakeout"
#define Author "Daniil Palii"
#define AppUrl "https://github.com/DaniilPalii/Tools.BookTakeout"
#define AppUpdatesUrl "https://github.com/DaniilPalii/Tools.BookTakeout/releases"

#define ReleaseDirectoryPath "..\artifacts\publish\BookTakeout.Console\release_win-x64"
#define AppExeName "BookTakeout.Console.exe"

#define ExePath ReleaseDirectoryPath + "\" + AppExeName
#define AppVersion GetStringFileInfo(ExePath, "ProductVersion")

[Setup]
AppName = "{#AppName}"
AppVersion = "{#AppVersion}"
AppVerName = "{#AppName} v{#AppVersion}"
OutputBaseFilename = "{#CamelCaseAppName}_v{#AppVersion}_setup"
OutputDir = "..\artifacts\setup"
DefaultDirName = "{autopf}\{#CamelCaseAppName}"
AppId = "{#AppId}"
ArchitecturesAllowed = x64compatible
ArchitecturesInstallIn64BitMode = x64compatible
AppPublisher = "{#Author}"
AppPublisherURL = "{#AppUrl}"
AppSupportURL = "{#AppUrl}"
AppUpdatesURL = "{#AppUpdatesUrl}"
DefaultGroupName = Programs
AllowNoIcons = yes
LicenseFile = "..\setup\EndUserLicenseAgreement.txt"
PrivilegesRequiredOverridesAllowed = dialog
SolidCompression = yes
WizardStyle = classic dynamic

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "ukrainian"; MessagesFile: "compiler:Languages\Ukrainian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Files]
Source: "{#ReleaseDirectoryPath}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram, {#AppName}}"; Flags: nowait postinstall skipifsilent

