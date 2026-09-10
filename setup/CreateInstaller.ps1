[CmdletBinding()]
param(
    [string]$ProjectFile = "$PSScriptRoot\..\src\BookTakeout.ConsoleApp\BookTakeout.ConsoleApp.csproj",
    [string]$PackTitle = "Book Takeout",
    [string]$PackId = "DaniilPalii.BookTakeout",
    [string]$ExeName = "BookTakeout.ConsoleApp.exe",
    [string]$Runtime = "win-x64",
    [string]$PublishPath = "$PSScriptRoot\..\artifacts\publish",
    [string]$OutputPath = "$PSScriptRoot\..\artifacts\setup"
)

if (-not (Get-Command vpk -ErrorAction SilentlyContinue))
{
    Write-Host "vpk tool not found. Installing global .NET tool 'vpk'..." -ForegroundColor Yellow
    dotnet tool install -g vpk
}

Write-Host "Publishing single-file binary..." -ForegroundColor Cyan
dotnet publish $ProjectFile `
    --runtime $Runtime `
    --self-contained `
    --output $PublishPath

$exePath = Join-Path $PublishPath $ExeName
$version = (Get-Item $exePath).VersionInfo.ProductVersion

Write-Host "Packaging with Velopack..." -ForegroundColor Cyan
vpk pack `
    --packId $PackId `
    --packTitle $PackTitle `
    --packVersion $version `
    --packAuthors "Daniil Palii" `
    --packDir $PublishPath `
    --mainExe $ExeName `
    --outputDir $OutputPath `
    --instLicense "$PSScriptRoot\..\setup\EndUserLicenseAgreement.txt"

$OutputPath = [System.IO.Path]::GetFullPath($OutputPath)
Write-Host "Successfully packaged version $version to $OutputPath" -ForegroundColor Green