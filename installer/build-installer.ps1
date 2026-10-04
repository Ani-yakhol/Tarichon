param(
    [string]$Iscc = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    # Also build Tarichon-Setup-<ver>-Full.exe (bundles the .NET 8 Desktop Runtime) and Tarichon-Portable.zip
    [switch]$All
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$src = Join-Path $root 'src\HebrewTaskbarWidget.SettingsRecovery'
$output = Join-Path $PSScriptRoot 'Output'
$staging = Join-Path $output 'staging'
$iss = Join-Path $PSScriptRoot 'Tarichon-Setup.iss'

[xml]$csproj = Get-Content (Join-Path $src 'HebrewTaskbarWidget.csproj')
$version = ($csproj.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version

if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }

dotnet publish (Join-Path $src 'HebrewTaskbarWidget.csproj') -c Release -r win-x64 --self-contained false -o (Join-Path $staging 'app')
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed (main app)' }

dotnet publish (Join-Path $src 'HebrewTaskbarWidget.SettingsRecovery.csproj') -c Release -r win-x64 --self-contained false -o (Join-Path $staging 'settings')
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed (settings app)' }

$defines = @("/DMyAppVersion=$version", "/DMainFilesDir=$staging\app", "/DSettingsFilesDir=$staging\settings")

& $Iscc @defines $iss
if ($LASTEXITCODE -ne 0) { throw 'ISCC failed' }

if ($All) {
    $runtime = Join-Path $PSScriptRoot 'Redist\windowsdesktop-runtime-8.0-win-x64.exe'
    if (-not (Test-Path $runtime)) {
        Invoke-WebRequest 'https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe' -OutFile $runtime -UseBasicParsing
    }
    & $Iscc @defines '/DIncludeDotNetRuntime=true' $iss
    if ($LASTEXITCODE -ne 0) { throw 'ISCC failed (Full)' }

    $portable = Join-Path $staging 'portable'
    New-Item -ItemType Directory $portable | Out-Null
    Copy-Item (Join-Path $staging 'app\*') $portable -Recurse
    Copy-Item (Join-Path $staging 'settings\HebrewTaskbarWidgetSettings.*') $portable
    Get-ChildItem $portable -Filter *.pdb -Recurse | Remove-Item
    $zip = Join-Path $output 'Tarichon-Portable.zip'
    if (Test-Path $zip) { Remove-Item $zip }
    # .NET ZipFile keeps Hebrew file names intact (Compress-Archive in PS 5.1 can garble them)
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($portable, $zip, 'Optimal', $false, [System.Text.Encoding]::UTF8)
}

Remove-Item $staging -Recurse -Force
Get-ChildItem $output -File
