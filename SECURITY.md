# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |
| < 1.0   | :x:                |

---

## Security Model & Principles

MachineIDLogger is designed with a defense-in-depth security architecture tailored for Windows system administration:

1. **Principle of Least Privilege**:
   - The primary WinUI 3 graphical user interface (`MachineIDLogger.exe`) runs as a standard user process and **does not** require administrator privileges for system discovery, hardware inspection, log viewing, or report generation.
   - Administrative elevation (UAC) is invoked **only** when writing to the protected system registry key `HKLM\SOFTWARE\Microsoft\Cryptography\MachineGuid`.
   - All mutations are handled by an isolated, single-purpose helper process (`MachineIDLogger.Elevator.exe`) that receives the target GUID, validates it, and exits immediately.

2. **Atomic Backups & Cryptographic Integrity**:
   - Before mutating `MachineGuid`, an automatic atomic JSON backup is written to `C:\MachineIDLogger\Backups\`.
   - Backups include a cryptographic SHA-256 hash of the payload to detect accidental tampering or corruption.

3. **Strict Offline Operation & Zero Telemetry**:
   - MachineIDLogger collects and displays sensitive system identifiers (e.g. MAC addresses, MachineGuid, Motherboard/BIOS serials, IP addresses) purely for the local user.
   - None of this information is transmitted over any network interface.
   - Outbound HTTP requests are strictly limited to the user-initiated "Check for Updates" action, which contacts the official GitHub Releases API (`https://api.github.com/repos/subhadipshil/machineidlogger/releases/latest`).

4. **Directory Isolation**:
   - The audit log, SQLite history database, and backup files reside under `C:\MachineIDLogger\`.
   - Only processes with local file write permissions can modify these stores.

---

## Reporting a Vulnerability

If you discover a security vulnerability within MachineIDLogger:

1. **Do NOT open a public GitHub Issue** with exploit details or vulnerability descriptions.
2. Email the maintainer directly at: `subhadipshil@gmail.com` with the subject line `[SECURITY] MachineIDLogger Vulnerability Report`.
3. Include detailed steps to reproduce the issue, your environment details (Windows version, .NET version), and potential impact.
4. You will receive an acknowledgment within 48 hours, followed by updates on the remediation timeline and release patch.
