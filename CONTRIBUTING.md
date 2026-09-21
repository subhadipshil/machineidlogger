# Contributing to MachineIDLogger

Thank you for your interest in contributing to **MachineIDLogger**!

MachineIDLogger is a production-grade, privacy-respecting Windows system inspection and controlled machine identity management utility. To maintain its high standards of technical precision, reliability, and security, please review the following guidelines.

---

## Code of Conduct & Ethical Positioning

* **No HWID Spoofing or Anti-Cheat Bypass**: Contributions intended to disguise hardware serials, bypass bans, evade anti-cheat mechanisms, or facilitate piracy will be rejected immediately.
* **Technical Honesty**: The utility clearly reports the exact provenance of all data (e.g. distinguishing software `MachineGuid` from immutable firmware serials and SMBIOS UUIDs). Any PR that blurs these distinctions or misrepresents system state will not be accepted.
* **Privacy & Telemetry**: MachineIDLogger operates 100% offline. No telemetry, device fingerprints, or usage statistics are transmitted over the network. Network activity is strictly limited to checking for updates when triggered by the user.

---

## Development Environment Setup

### Prerequisites
* Windows 10 (Build 19041+) or Windows 11 (22H2 / 24H2 recommended)
* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (LTS)
* Visual Studio 2022 / 2025 or VS Code with C# Dev Kit
* Windows App SDK dependencies

### Building the Project
Clone the repository and build using the .NET CLI:
```powershell
git clone https://github.com/subhadipshil/machineidlogger.git
cd machineidlogger

# Build Debug configuration
dotnet build MachineIDLogger.slnx

# Run automated tests
dotnet test MachineIDLogger.slnx
```

---

## Architecture Guidelines

MachineIDLogger is built with a decoupled, clean architecture:
- **`MachineIDLogger.Core`**: Domain models, entity definitions, and validation rules. Must have zero UI dependencies.
- **`MachineIDLogger.Infrastructure`**: Win32 native calls (`NativeMethods.cs`), 64-bit registry access (`RegistryHelper.cs`), WMI/CIM queries (`WmiHelper.cs`), SQLite persistence (`SqliteHistoryStore.cs`), and JSON backup handlers (`JsonBackupStore.cs`).
- **`MachineIDLogger.Services`**: Business logic, discovery orchestrator (`DeviceDiscoveryService`), diagnostic test suite, and update checkers.
- **`MachineIDLogger.Elevator`**: Minimalist console executable requiring administrator privileges via UAC to mutate `HKLM\SOFTWARE\Microsoft\Cryptography\MachineGuid`.
- **`MachineIDLogger`**: WinUI 3 desktop client following the MVVM pattern with `CommunityToolkit.Mvvm`.

### Rules for Adding New Hardware Providers
1. **Fault Isolation**: Hardware queries must be wrapped in `Task.Run` or async calls and isolated in `try-catch` blocks. A failure in one provider (e.g. missing GPU or restricted BIOS access) must never crash or block discovery for other subsystems.
2. **No Mock Data in Production**: If a property is not returned by the OS, explicitly report `"Not available"`, `"Not exposed by firmware"`, or `"Unavailable on this system"`. Never populate fabricated hardware strings.
3. **Data Provenance**: Every property model should include the source (e.g. `Win32_Processor`, `GlobalMemoryStatusEx`, `Win32_VideoController`).

---

## Pull Request Guidelines

1. **Branch Naming**: Use feature branches such as `feature/gpu-metrics`, `fix/registry-race-condition`, or `docs/update-guide`.
2. **Commit Messages**: Write clear, descriptive commit messages following the Conventional Commits specification (e.g. `feat: add display HDR detection`, `fix: handle null serial on virtual drives`).
3. **Automated Testing**: Ensure all existing tests pass (`dotnet test`) and add new unit tests for any new validators, converters, or business logic.
4. **Code Quality**: No compiler warnings are permitted. Follow modern C# idioms (nullable reference types, pattern matching, file-scoped namespaces).
