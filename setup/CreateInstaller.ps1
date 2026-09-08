[CmdletBinding()]
param(
    [string]$ProjectFile = "BookTakeout.Console.csproj",
    [string]$PackTitle = "Book Takeout",
    [string]$PackId = "DaniilPalii.BookTakeout",
    [string]$ExeName = "BookTakeout.Console.exe",
    [string]$Runtime = "win-x64",
    [string]$PublishPath = "./artifacts/publish",
    [string]$OutputPath = "./artifacts/setup"
)

if (-not (Get-Command vpk -ErrorAction SilentlyContinue))
{
    Write-Host "vpk tool not found. Installing global .NET tool 'vpk'..." -ForegroundColor Yellow
    dotnet tool install -g vpk
}

Write-Host "Publishing single-file binary..." -ForegroundColor Cyan
dotnet publish $ProjectFile `
    -r $Runtime `
    --self-contained `
    -o $PublishPath

$version = (Get-Item $exePath).VersionInfo.ProductVersion

Write-Host "Packaging with Velopack..." -ForegroundColor Cyan
vpk pack `
    --packId $PackId `
    --packTitle $PackTitle `
    --packVersion $version `
    --packAuthor "Daniil Palii" `
    --packDir $PublishPath `
    --mainExe $ExeName `
    --outputDir $OutputPath `
    --instLicense .\setup\EndUserLicenseAgreement.txt

Write-Host "Successfully packaged version $version to $OutputPath" -ForegroundColor Green