# MachineIDLogger Release Build and Installer Packaging Automation
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RootDir = Split-Path -Parent $ScriptDir
$DistDir = Join-Path $RootDir "dist"
$Dotnet = "C:\Users\subha\.dotnet\dotnet.exe"

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host " Building MachineIDLogger ($Configuration) for win-x64..." -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan

if (-not (Test-Path $DistDir)) {
    New-Item -ItemType Directory -Path $DistDir -Force | Out-Null
}

# 1. Publish MachineIDLogger Desktop Utility
Write-Host "Publishing MachineIDLogger WinUI 3 Desktop Application (Self-Contained)..." -ForegroundColor Yellow
& $Dotnet publish "$RootDir\src\MachineIDLogger\MachineIDLogger.csproj" `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -o "$RootDir\src\MachineIDLogger\bin\$Configuration\net10.0-windows10.0.26100.0\win-x64\publish"

# 2. Publish Elevator Worker
Write-Host "Publishing MachineIDLogger.Elevator Worker (Self-Contained Single-File)..." -ForegroundColor Yellow
& $Dotnet publish "$RootDir\src\MachineIDLogger.Elevator\MachineIDLogger.Elevator.csproj" `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -o "$RootDir\src\MachineIDLogger.Elevator\bin\$Configuration\net10.0\win-x64\publish"

# Copy Elevator binaries and config into Main App publish folder
$elevatorPublishDir = "$RootDir\src\MachineIDLogger.Elevator\bin\$Configuration\net10.0\win-x64\publish"
$appPublishDir = "$RootDir\src\MachineIDLogger\bin\$Configuration\net10.0-windows10.0.26100.0\win-x64\publish"
if (Test-Path $elevatorPublishDir) {
    Copy-Item "$elevatorPublishDir\*" -Destination $appPublishDir -Recurse -Force
    Write-Host "Copied MachineIDLogger.Elevator binaries to main distribution directory." -ForegroundColor Green
}

# 3. Clean and Prepare Unpackaged Portable Distribution
$Version = "1.0.0"

# Clean up any leftover legacy or non-portable artifacts from dist
Get-ChildItem -Path $DistDir -Filter "*1.0.1*" | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Get-ChildItem -Path $DistDir -Filter "*win-x64" | Where-Object { $_.Name -notmatch "Portable" } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Get-ChildItem -Path $DistDir -Filter "*win-x64.zip" | Where-Object { $_.Name -notmatch "Portable" } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

$portableFolder = Join-Path $DistDir "MachineIDLogger-v$Version-Portable-win-x64"

Write-Host "Syncing ready-to-run portable directory: $portableFolder..." -ForegroundColor Yellow
if (Test-Path $portableFolder) { Remove-Item $portableFolder -Recurse -Force }
New-Item -ItemType Directory -Path $portableFolder -Force | Out-Null
Copy-Item "$appPublishDir\*" -Destination $portableFolder -Recurse -Force

Write-Host "Waiting briefly for background file handles to clear..." -ForegroundColor Gray
Start-Sleep -Seconds 2

$zipPath = Join-Path $DistDir "MachineIDLogger-v$Version-Portable-win-x64.zip"
Write-Host "Creating portable archive: $zipPath..." -ForegroundColor Yellow
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

Add-Type -AssemblyName "System.IO.Compression.FileSystem"
$zipCreated = $false
for ($attempt = 1; $attempt -le 5; $attempt++) {
    try {
        if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
        [System.IO.Compression.ZipFile]::CreateFromDirectory($portableFolder, $zipPath, [System.IO.Compression.CompressionLevel]::Optimal, $false)
        $zipCreated = $true
        break
    } catch {
        Write-Host "Archive attempt $attempt failed ($($_.Exception.Message)). Retrying in 2 seconds..." -ForegroundColor Yellow
        Start-Sleep -Seconds 2
    }
}

if (-not $zipCreated) {
    Compress-Archive -Path "$portableFolder\*" -DestinationPath $zipPath -Force
}
Write-Host "Portable package created: $zipPath" -ForegroundColor Green

# 4. Check for Inno Setup Compiler (ISCC)
$isccPaths = @(
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 7\ISCC.exe",
    "C:\Users\subha\AppData\Local\Programs\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 7\ISCC.exe",
    "C:\Program Files\Inno Setup 7\ISCC.exe"
)

$iscc = $isccPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

if ($iscc) {
    Write-Host "Compiling Windows Installer using Inno Setup ($iscc)..." -ForegroundColor Yellow
    & $iscc "$ScriptDir\MachineIDLogger.iss"
    Write-Host "Windows Installer generated successfully in: $DistDir" -ForegroundColor Green
} else {
    Write-Host "Inno Setup compiler not detected. The portable release archive is available in: $DistDir" -ForegroundColor Gray
    Write-Host "To compile the full Windows setup executable, install Inno Setup 6/7 via 'winget install JRSoftware.InnoSetup'." -ForegroundColor DarkGray
}

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host " Build & Packaging Completed Successfully!" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Cyan
