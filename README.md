<div align="center">

<img src="src/MachineIDLogger/Assets/AppIcon.png" alt="MachineIDLogger Icon" width="108" height="108" />

# MachineIDLogger

**Production-grade Windows system hardware inspection & controlled machine identity utility.**

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%20Windows%2011%20(x64)-0078D6.svg?logo=windows)](https://microsoft.com/windows)
[![.NET](https://img.shields.io/badge/.NET-10.0%20LTS-512BD4.svg?logo=dotnet)](https://dotnet.microsoft.com/)
[![UI](https://img.shields.io/badge/UI-WinUI%203%20%2F%20Windows%20App%20SDK%202.5.1-0078D4.svg)](https://learn.microsoft.com/windows/apps/winui/winui3/)
[![Version](https://img.shields.io/badge/Version-v1.0.0-success.svg)](https://github.com/subhadipshil/machineidlogger/releases)
[![Tests](https://img.shields.io/badge/Tests-17%20Passed-brightgreen.svg)]()
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

[Download Installer (v1.0.0)](https://github.com/subhadipshil/machineidlogger/releases) • [Portable ZIP](https://github.com/subhadipshil/machineidlogger/releases) • [Track Record](TRACK_RECORD.md) • [Changelog](CHANGELOG.md)

</div>

---

## Overview

**MachineIDLogger** is a native desktop utility engineered for Windows technicians, system administrators, software developers, and power users. Built with **C# 13**, **.NET 10 LTS**, and **WinUI 3**, it delivers deep hardware discovery, live telemetry, explicit identifier provenance analysis, and an atomic, verified workflow for managing the Windows `MachineGuid`.

---

## Key Features

### 🖥️ Real Hardware & System Discovery
* **Zero Mock Data**: All metrics are queried live from the Windows kernel, CIM/WMI, DXGI, NetworkInterface, and SetupAPI.
* **Processor (CPU)**: Physical cores, logical threads, base/boost clock speeds, cache tiers (L2/L3), virtualization flags (VT-x / AMD-V), and live load monitoring.
* **Memory (RAM)**: Real-time physical allocation (total, available, used, load percentage) alongside detailed physical DIMM/SODIMM module inventory (bank, capacity, speed, part number, serial number).
* **Graphics (GPU)**: Multi-adapter enumeration (integrated, discrete, virtual), dedicated VRAM, driver versions, video modes, and PNP device IDs.
* **Storage Drives & Volumes**: Physical disks (model, firmware, interface, serial number) separated from logical filesystem volumes (drive letter, filesystem, capacity, free space).
* **Motherboard & BIOS/UEFI**: Mainboard model, manufacturer, serial number, BIOS vendor, version, and SMBIOS tables.
* **Displays & Monitors**: Multi-monitor resolution, refresh frequency (Hz), interface, and primary display identification.
* **Network Adapters**: Active adapters, physical MAC addresses, IPv4/IPv6 addresses, gateway DNS, and link status.
* **PnP Device Inventory**: Searchable hardware catalog across 12 categories with one-click hardware ID copying.

### 🔑 Controlled Windows MachineGuid Manager
* **Monospace UI**: High-visibility presentation of the active Windows `MachineGuid`.
* **7-Step Verified Workflow**:
  1. **RFC 4122 Validation**: Strict format checking (rejects invalid or NIL GUIDs).
  2. **64-bit Registry Pre-Read**: Direct access to `HKLM\SOFTWARE\Microsoft\Cryptography\MachineGuid`.
  3. **Atomic Backup**: SHA-256 verified JSON backup stored in `C:\MachineIDLogger\Backups\`.
  4. **Elevation Isolation**: Main GUI runs with standard permissions; elevation is requested via `MachineIDLogger.Elevator.exe` only when mutating system keys.
  5. **Post-Write Verification**: Re-reads registry handle to verify successful write before confirming.
  6. **SQLite History Audit**: Records audit entries in `C:\MachineIDLogger\History\history.db`.
  7. **Structured Logging**: Timestamps, before/after values, and status logged in `C:\MachineIDLogger\Logs\`.
* **One-Click Rollback**: Restore any previous historical GUID with verified integrity checking.

### 🔍 System Diagnostics & Multi-Format Exports
* **16-Point Hardware & API Diagnostics**:
  * Audio & Speaker hardware controllers
  * Display subsystem, resolution & refresh frequency
  * Network gateway & external DNS reachability (`dns.google`)
  * Power & battery subsystem telemetry
  * Process privilege & UAC elevation state
  * 64-bit Registry read/write capabilities
  * WMI service availability & query responsiveness
  * SMBIOS firmware UUID exposure
  * Persistent storage, SQLite database & backup folder integrity
* **Report Exporter**: Export comprehensive diagnostic and hardware reports to standalone **HTML** (with printable CSS), **JSON**, **CSV**, and **TXT**.

### 💻 Featured: Cursor Local Identity
* **Version-Aware Inspection**: Detects Cursor's `%APPDATA%\Cursor\User\globalStorage\storage.json`.
* **Telemetry Identifiers**: Inspects `machineId`, `macMachineId`, `devDeviceId`, and `sqmId`.
* **Safety First**: Independent of Windows `MachineGuid`, with automatic pre-mutation JSON backups.

### ⚡ Standalone CLI & Batch Utilities (`tools/`)
Operate directly from scripts or the Windows command prompt:
* `machineid-current.bat`: Outputs active MachineGuid, SMBIOS UUID, and OS edition.
* `machineid-change.bat`: Interactive, elevated mutation workflow with automatic backup.
* `machineid-restore.bat`: Interactive selection to restore from historical backups.
* `machineid-history.bat`: Displays audit history table from SQLite database.

---

## Technical Positioning & Ethical Boundaries

> [!IMPORTANT]
> **Ethical Utility Notice**: MachineIDLogger is strictly an auditing, diagnostic, and administrative tool.
> - It is **NOT** a "HWID spoofer", anti-ban utility, or anti-cheat evasion software.
> - The software enforces strict technical transparency: changing the Windows `MachineGuid` modifies the software installation GUID in the Windows Registry (`HKLM\SOFTWARE\Microsoft\Cryptography\MachineGuid`).
> - It **does not** modify or spoof physical hardware serial numbers, SMBIOS firmware UUIDs, motherboard serials, MAC addresses, or disk drive serials.

---

## Directory & Data Contract

All persistent logs, audit history, backups, and configurations are maintained in `C:\MachineIDLogger\`:

```
C:\MachineIDLogger\
├── Logs\
│   ├── machineid-yyyyMMdd.log      # Rolling structured activity log
│   └── crash.log                   # Unhandled fatal crash log
├── History\
│   └── history.db                  # SQLite database (audit trail)
├── Backups\
│   ├── guid_backup_*.json          # Atomic Windows MachineGuid backups (SHA-256)
│   └── cursor\
│       └── cursor_backup_*.json    # Cursor telemetry backups
├── Exports\
│   └── *.html / *.json / *.csv     # Generated diagnostic reports
└── Config\
    └── settings.json               # Application settings & UI theme preferences
```

---

## Downloads & Installation

### Option 1: Standalone Windows Installer (Recommended)
Download **`MachineIDLogger-Setup-v1.0.0.exe`** from the [Latest Release](https://github.com/subhadipshil/machineidlogger/releases):
* Includes Start Menu and Desktop shortcuts with high-resolution vector icons.
* Self-contained: runs out of the box with zero runtime dependencies.
* Supports clean installation, in-place upgrades, and full uninstallation.

### Option 2: Portable Archive
Download **`MachineIDLogger-v1.0.0-Portable-win-x64.zip`**:
* Extract anywhere and run `MachineIDLogger.exe`.
* No registry installation required; ideal for USB technician drives.

---

## Building from Source

### Prerequisites
* Windows 10 (Build 19041+) or Windows 11 (x64)
* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* PowerShell 7+ (or Windows PowerShell 5.1)
* [Inno Setup 6 / 7](https://jrsoftware.org/isinfo.php) (optional, for compiling the installer)

### Build Commands
```powershell
# 1. Clone repository
git clone https://github.com/subhadipshil/machineidlogger.git
cd machineidlogger

# 2. Build Release configuration
dotnet build MachineIDLogger.slnx -c Release --nologo

# 3. Run automated tests (17 tests)
dotnet test MachineIDLogger.slnx -c Release --nologo

# 4. Package Release (Portable folder, ZIP archive, and Inno Setup installer)
powershell -ExecutionPolicy Bypass -File installer/build-installer.ps1
```

Generated packages will be output to the `dist/` directory:
* `dist/MachineIDLogger-Setup-v1.0.0.exe`
* `dist/MachineIDLogger-v1.0.0-Portable-win-x64/`
* `dist/MachineIDLogger-v1.0.0-Portable-win-x64.zip`

---

## Support the Developer ☕

MachineIDLogger is 100% free and open-source software built for the Windows developer and system administration community. If this utility has been useful to you, voluntary contributions help keep active maintenance and development going!

<div align="center">

<img src="src/MachineIDLogger/Assets/BeautifiedQr.png" alt="UPI Payment QR" width="220" />

**UPI ID**: `subhadipshil.pnb@ybl`

[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20A%20Coffee-Donate-orange.svg?style=for-the-badge&logo=buy-me-a-coffee)](https://buymeacoffee.com/subhadipshil)

</div>

---

## License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.
