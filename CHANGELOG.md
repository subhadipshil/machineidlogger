# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.0] - 2026-09-21

### Added
- **Core Architecture & Engine**:
  - Full .NET 10 LTS native Windows implementation with C# 13 and WinUI 3 (Windows App SDK).
  - Standalone unpackaged desktop architecture with 0 external runtime installation requirements.
  - Multi-threaded asynchronous discovery engine (`DeviceDiscoveryService`) with isolated fault-tolerant providers.
- **Hardware & Device Discovery**:
  - **System Overview**: Detection of OS edition, build number (UBR), install date, uptime, platform SKU, and domain/workgroup.
  - **Processor (CPU)**: Physical core count, logical thread count, base/current/max clock frequencies, L2/L3 cache sizes, virtualization capabilities (VT-x / AMD-V), and live dynamic CPU utilization.
  - **Memory (RAM)**: Physical RAM statistics (total, available, used, load %), pagefile metrics, and an expandable DIMM/SODIMM physical module inventory table (slot, manufacturer, speed, part number, serial).
  - **Graphics (GPU)**: Multi-adapter enumeration (integrated, discrete, and virtual adapters) with dedicated VRAM, driver versions, driver dates, video modes, and PNP device IDs.
  - **Storage**: Separation of physical disk controllers (model, manufacturer, firmware revision, interface, serial) and logical partitions (drive letters, volume labels, filesystems, capacity, free space, and usage).
  - **Motherboard & BIOS/UEFI**: Mainboard model, board manufacturer, PCB version, serial number, BIOS vendor, BIOS version, release date, and SMBIOS tables.
  - **Displays & Monitors**: Multi-monitor detection with resolution, refresh frequency (Hz), connection interface, and primary display flag.
  - **Network Inventory**: Physical and virtual NIC enumeration, MAC addresses, IPv4/IPv6 addresses, DNS servers, DHCP lease status, and link speed.
  - **Device Inventory**: Technical PnP device catalog categorized across 12 hardware classes with live search and hardware ID copying.
- **Controlled Windows MachineGuid Management**:
  - Presentation of active Windows `MachineGuid` with monospace typography.
  - 7-step verified elevation workflow (validation, pre-read, atomic backup, UAC elevation via `MachineIDLogger.Elevator.exe`, immediate verification, SQLite history record, and structured logging).
  - One-click restoration from historical backups.
- **Featured: Cursor Local Identity**:
  - Version-aware detection and telemetry inspection for `%APPDATA%\Cursor\User\globalStorage\storage.json`.
  - Independent management for `machineId`, `macMachineId`, `devDeviceId`, and `sqmId`.
  - Automated pre-change atomic backup generation.
- **Diagnostics & Multi-Format Reporting**:
  - 12-point health diagnostic test runner.
  - System report export engine generating clean, printable **HTML**, **JSON**, **CSV**, and **TXT** files.
- **Batch Utilities (`tools/`)**:
  - Command-line batch scripts (`machineid-current.bat`, `machineid-change.bat`, `machineid-restore.bat`, `machineid-history.bat`) backed by PowerShell engine `tools/lib/machineid-core.ps1`.
- **Packaging & Distribution**:
  - Inno Setup installer script (`installer/MachineIDLogger.iss`) and automated build pipeline (`installer/build-installer.ps1`).
  - Standalone portable ZIP distribution (`dist/MachineIDLogger-v1.0.0-win-x64.zip`).
- **Support the Developer**:
  - "Buy Me a Coffee" / UPI donation section in About page with copy button and QR code for UPI ID `subhadipshil.pnb@ybl`.
- **Updates & Release Notes**:
  - Integrated "What's New" release notes page and automated "Check for Updates" via GitHub Releases API.
