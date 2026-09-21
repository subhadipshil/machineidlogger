# MachineIDLogger Core Shared PowerShell Script
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("current", "change", "restore", "history")]
    [string]$Action,

    [string]$TargetGuid = ""
)

$DataDir = "C:\MachineIDLogger"
$BackupDir = "$DataDir\Backups"
$LogDir = "$DataDir\Logs"
$HistoryDir = "$DataDir\History"
$HistoryDb = "$HistoryDir\history.db"

function Ensure-Directories {
    foreach ($dir in @($DataDir, $BackupDir, $LogDir, $HistoryDir)) {
        if (-not (Test-Path $dir)) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
        }
    }
}

function Log-Message([string]$Severity, [string]$Category, [string]$Message) {
    Ensure-Directories
    $timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fff")
    $logFile = "$LogDir\machineid-$((Get-Date).ToString('yyyyMMdd')).log"
    $line = "[$timestamp] [$($Severity.ToUpper().PadRight(5))] [$($Category.PadRight(15))] $Message"
    Add-Content -Path $logFile -Value $line -Encoding UTF8
}

function Test-IsAdmin {
    $currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentIdentity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-RegistryMachineGuid {
    try {
        $regKey = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine, [Microsoft.Win32.RegistryView]::Registry64)
        $cryptoKey = $regKey.OpenSubKey("SOFTWARE\Microsoft\Cryptography", $false)
        if ($cryptoKey -ne $null) {
            $val = $cryptoKey.GetValue("MachineGuid")
            $cryptoKey.Close()
            $regKey.Close()
            return $val
        }
    } catch {
        return $null
    }
    return $null
}

function Set-RegistryMachineGuid([string]$NewGuid) {
    $regKey = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine, [Microsoft.Win32.RegistryView]::Registry64)
    $cryptoKey = $regKey.OpenSubKey("SOFTWARE\Microsoft\Cryptography", $true)
    if ($cryptoKey -eq $null) {
        throw "Failed to open HKLM\SOFTWARE\Microsoft\Cryptography with write permissions."
    }
    $cryptoKey.SetValue("MachineGuid", $NewGuid, [Microsoft.Win32.RegistryValueKind]::String)
    $cryptoKey.Flush()
    $cryptoKey.Close()
    $regKey.Close()
}

function Create-Backup([string]$CurrentGuid, [string]$Note) {
    Ensure-Directories
    $timestamp = (Get-Date).ToUniversalTime().ToString("yyyyMMdd_HHmmss")
    $safeGuid = $CurrentGuid.Replace("{","").Replace("}","").Replace("-","")
    $backupFile = "$BackupDir\guid_backup_${timestamp}_${safeGuid}.json"

    $meta = @{
        IdentifierType = "WindowsMachineGuid"
        PreviousValue = $CurrentGuid
        CreatedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
        MachineName = $env:COMPUTERNAME
        OsVersion = [System.Environment]::OSVersion.ToString()
        Notes = $Note
    }

    $json = $meta | ConvertTo-Json -Depth 3
    Set-Content -Path $backupFile -Value $json -Encoding UTF8
    return $backupFile
}

function Test-ValidGuid([string]$GuidStr) {
    $guidOutput = [System.Guid]::Empty
    return [System.Guid]::TryParse($GuidStr, [ref]$guidOutput) -and ($guidOutput -ne [System.Guid]::Empty)
}

function Record-History([string]$PrevGuid, [string]$NewGuid, [string]$ActionName, [string]$Status, [string]$BackupPath, [string]$Notes) {
    Ensure-Directories
    # Fallback to local text-based history log alongside SQLite
    $historyLog = "$HistoryDir\history.log"
    $time = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    $entry = "[$time] Action: $ActionName | Status: $Status | Previous: $PrevGuid | New: $NewGuid | Backup: $BackupPath | $Notes"
    Add-Content -Path $historyLog -Value $entry -Encoding UTF8
}

# ----------------- ACTIONS -----------------

if ($Action -eq "current") {
    $current = Get-RegistryMachineGuid
    $smbios = (Get-CimInstance Win32_ComputerSystemProduct -ErrorAction SilentlyContinue).UUID
    $compName = $env:COMPUTERNAME
    $os = (Get-CimInstance Win32_OperatingSystem -ErrorAction SilentlyContinue).Caption

    Write-Host ""
    Write-Host "==================================================================" -ForegroundColor Cyan
    Write-Host " MachineIDLogger - Current System Identity" -ForegroundColor Cyan
    Write-Host "==================================================================" -ForegroundColor Cyan
    Write-Host " Host Name        : " -NoNewline; Write-Host $compName -ForegroundColor White
    Write-Host " Operating System : " -NoNewline; Write-Host $os -ForegroundColor White
    Write-Host " Windows MachineID: " -NoNewline; Write-Host $current -ForegroundColor Yellow
    Write-Host " SMBIOS UUID      : " -NoNewline; Write-Host $smbios -ForegroundColor Gray
    Write-Host " Registry Source  : " -NoNewline; Write-Host "HKLM\SOFTWARE\Microsoft\Cryptography\MachineGuid (64-bit)" -ForegroundColor DarkGray
    Write-Host "==================================================================" -ForegroundColor Cyan
    Write-Host ""
}
elseif ($Action -eq "change") {
    Write-Host ""
    Write-Host "==================================================================" -ForegroundColor Cyan
    Write-Host " MachineIDLogger - Change Windows MachineGuid" -ForegroundColor Cyan
    Write-Host "==================================================================" -ForegroundColor Cyan

    if (-not (Test-IsAdmin)) {
        Write-Host " [ERROR] Administrator privileges are required to modify MachineGuid." -ForegroundColor Red
        Write-Host " Please re-run this tool from an elevated command prompt (Run as Administrator)." -ForegroundColor Yellow
        exit 1
    }

    $currentGuid = Get-RegistryMachineGuid
    Write-Host " Current MachineGuid : " -NoNewline; Write-Host $currentGuid -ForegroundColor Yellow

    if ([string]::IsNullOrWhiteSpace($TargetGuid)) {
        $newGuid = [System.Guid]::NewGuid().ToString("D").ToLowerInvariant()
        Write-Host " Generated New UUID  : " -NoNewline; Write-Host $newGuid -ForegroundColor Green
    } else {
        if (-not (Test-ValidGuid $TargetGuid)) {
            Write-Host " [ERROR] Provided target GUID '$TargetGuid' is not a valid RFC 4122 GUID format." -ForegroundColor Red
            exit 1
        }
        $newGuid = ([System.Guid]::Parse($TargetGuid)).ToString("D").ToLowerInvariant()
        Write-Host " Custom New UUID     : " -NoNewline; Write-Host $newGuid -ForegroundColor Green
    }

    Write-Host ""
    Write-Host " [NOTICE] Changing MachineGuid modifies the Windows software installation identity." -ForegroundColor Gray
    Write-Host " It does NOT modify hardware serials or BIOS firmware UUIDs." -ForegroundColor Gray
    Write-Host ""
    $confirm = Read-Host " Are you sure you want to backup and apply this change? (Y/N)"
    if ($confirm -notmatch "^[Yy]$") {
        Write-Host " Operation cancelled by user." -ForegroundColor Yellow
        exit 0
    }

    # Backup
    Write-Host " Creating pre-modification backup..." -ForegroundColor Cyan
    $backupPath = Create-Backup -CurrentGuid $currentGuid -Note "CLI change tool pre-modification backup"
    Write-Host " Backup saved to: $backupPath" -ForegroundColor DarkGray

    # Apply
    Write-Host " Writing new MachineGuid to 64-bit registry..." -ForegroundColor Cyan
    Set-RegistryMachineGuid -NewGuid $newGuid

    # Verify
    Write-Host " Verifying written value..." -ForegroundColor Cyan
    $verified = Get-RegistryMachineGuid

    if ($verified -eq $newGuid) {
        Write-Host ""
        Write-Host " [SUCCESS] MachineGuid was successfully changed and verified!" -ForegroundColor Green
        Write-Host " Previous GUID : $currentGuid" -ForegroundColor Gray
        Write-Host " Active GUID   : $verified" -ForegroundColor Yellow
        Write-Host " Backup File   : $backupPath" -ForegroundColor DarkGray
        Log-Message -Severity "INFO" -Category "MachineGuid" -Message "CLI: Successfully changed MachineGuid from '$currentGuid' to '$verified'."
        Record-History -PrevGuid $currentGuid -NewGuid $verified -ActionName "Changed" -Status "Success" -BackupPath $backupPath -Notes "CLI change"
    } else {
        Write-Host ""
        Write-Host " [CRITICAL FAILURE] Verification failed! Registry value is '$verified', expected '$newGuid'." -ForegroundColor Red
        Log-Message -Severity "ERROR" -Category "MachineGuid" -Message "CLI: Verification mismatch. Expected '$newGuid', got '$verified'."
        Record-History -PrevGuid $currentGuid -NewGuid $newGuid -ActionName "Changed" -Status "FailedVerification" -BackupPath $backupPath -Notes "Verification mismatch"
        exit 2
    }
}
elseif ($Action -eq "restore") {
    Write-Host ""
    Write-Host "==================================================================" -ForegroundColor Cyan
    Write-Host " MachineIDLogger - Restore Windows MachineGuid" -ForegroundColor Cyan
    Write-Host "==================================================================" -ForegroundColor Cyan

    if (-not (Test-IsAdmin)) {
        Write-Host " [ERROR] Administrator privileges are required to restore MachineGuid." -ForegroundColor Red
        Write-Host " Please re-run this tool from an elevated command prompt." -ForegroundColor Yellow
        exit 1
    }

    if (-not (Test-Path $BackupDir)) {
        Write-Host " [ERROR] No backup directory found at $BackupDir." -ForegroundColor Red
        exit 1
    }

    $backups = Get-ChildItem -Path $BackupDir -Filter "guid_backup_*.json" | Sort-Object CreationTime -Descending
    if ($backups.Count -eq 0) {
        Write-Host " [ERROR] No backup files found in $BackupDir." -ForegroundColor Red
        exit 1
    }

    Write-Host " Available backups:" -ForegroundColor Cyan
    for ($i = 0; $i -lt [Math]::Min(10, $backups.Count); $i++) {
        $b = $backups[$i]
        $content = Get-Content $b.FullName -Raw | ConvertFrom-Json
        Write-Host " [$($i + 1)] $($b.CreationTime.ToString('yyyy-MM-dd HH:mm:ss')) | GUID: $($content.PreviousValue) | File: $($b.Name)" -ForegroundColor White
    }

    Write-Host ""
    $selection = Read-Host " Select backup number to restore (1-$([Math]::Min(10, $backups.Count))) or 'Q' to quit"
    if ($selection -match "^[Qq]$") { exit 0 }

    $index = [int]$selection - 1
    if ($index -lt 0 -or $index -ge $backups.Count) {
        Write-Host " Invalid selection." -ForegroundColor Red
        exit 1
    }

    $chosenBackup = $backups[$index]
    $jsonContent = Get-Content $chosenBackup.FullName -Raw | ConvertFrom-Json
    $restoreGuid = $jsonContent.PreviousValue

    if (-not (Test-ValidGuid $restoreGuid)) {
        Write-Host " [ERROR] Backup file contains invalid GUID: $restoreGuid" -ForegroundColor Red
        exit 1
    }

    $currentGuid = Get-RegistryMachineGuid
    Write-Host " Current Value : $currentGuid" -ForegroundColor Yellow
    Write-Host " Restore Target: $restoreGuid" -ForegroundColor Green

    $confirm = Read-Host " Confirm restore? (Y/N)"
    if ($confirm -notmatch "^[Yy]$") { exit 0 }

    $backupPath = Create-Backup -CurrentGuid $currentGuid -Note "Pre-restore automatic backup"
    Set-RegistryMachineGuid -NewGuid $restoreGuid
    $verified = Get-RegistryMachineGuid

    if ($verified -eq $restoreGuid) {
        Write-Host " [SUCCESS] MachineGuid was successfully restored to: $verified" -ForegroundColor Green
        Log-Message -Severity "INFO" -Category "MachineGuid" -Message "CLI: Successfully restored MachineGuid to '$verified'."
        Record-History -PrevGuid $currentGuid -NewGuid $verified -ActionName "Restored" -Status "Success" -BackupPath $backupPath -Notes "CLI restore"
    } else {
        Write-Host " [CRITICAL FAILURE] Verification failed during restore." -ForegroundColor Red
        exit 2
    }
}
elseif ($Action -eq "history") {
    Write-Host ""
    Write-Host "==================================================================" -ForegroundColor Cyan
    Write-Host " MachineIDLogger - Identity Change History" -ForegroundColor Cyan
    Write-Host "==================================================================" -ForegroundColor Cyan

    $historyLog = "$HistoryDir\history.log"
    if (Test-Path $historyLog) {
        Get-Content $historyLog | Select-Object -Last 20 | ForEach-Object {
            Write-Host " $_" -ForegroundColor White
        }
    } else {
        Write-Host " No history log entries recorded yet." -ForegroundColor Gray
    }

    $backups = Get-ChildItem -Path $BackupDir -Filter "*.json" -ErrorAction SilentlyContinue
    Write-Host ""
    Write-Host " Total Backups on Disk: $($backups.Count)" -ForegroundColor Gray
    Write-Host " Directory: $BackupDir" -ForegroundColor DarkGray
    Write-Host "==================================================================" -ForegroundColor Cyan
    Write-Host ""
}
