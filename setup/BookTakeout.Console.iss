#define AppId "DaniilPalii.BookTakeout.Console"
#define AppName "Book Takeout"
#define CamelCaseAppName "BookTakeout"

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

[Files]
Source: "{#ReleaseDirectoryPath}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs