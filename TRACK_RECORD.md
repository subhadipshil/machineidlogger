# MachineIDLogger — Engineering Track Record & Debugging Ledger

This document serves as the persistent technical record of the **MachineIDLogger** codebase. It documents architectural decisions, runtime invariants, storage contracts, known issues, debugging findings, and verified resolutions. Any engineer or AI agent continuing work on this repository should consult this ledger first.

---

## 1. System Identity & Architecture

| Property | Value |
| :--- | :--- |
| **Product** | MachineIDLogger |
| **Type** | Native Windows System Utility / Diagnostic Tool |
| **Target OS** | Windows 10 (19041+) / Windows 11 (x64) |
| **Framework** | .NET 10 LTS (`net10.0-windows10.0.26100.0`) |
| **UI Stack** | WinUI 3 (Windows App SDK 2.5.1), Unpackaged |
| **Design Pattern** | MVVM via `CommunityToolkit.Mvvm` |
| **Persistence** | SQLite (`Microsoft.Data.Sqlite`), Rolling File Logs, JSON Backups |
| **Security Model** | Principle of Least Privilege (GUI standard user; mutation via `MachineIDLogger.Elevator.exe`) |

---

## 2. Directory & Storage Contract

All runtime logs, audit histories, backups, and user preferences are isolated in `C:\MachineIDLogger\`:

```
C:\MachineIDLogger\
├── Logs\
│   ├── machineid-yyyyMMdd.log      # Daily rolling structured activity log
│   └── crash.log                   # Unhandled fatal crash log (App & AppDomain)
├── History\
│   └── history.db                  # SQLite database (schema_version & machine_guid_history)
├── Backups\
│   ├── guid_backup_*.json          # Atomic Windows MachineGuid backups (SHA-256 integrity hash)
│   └── cursor\
│       └── cursor_backup_*.json    # Cursor IDE telemetry backups
├── Exports\
│   └── *.html / *.json / *.csv     # Generated diagnostic & system inventory exports
└── Config\
    └── settings.json               # Application settings, UI theme, refresh intervals
```

---

## 3. Project & Solution Map

```
MachineIDLogger.slnx
├── src/
│   ├── MachineIDLogger.Core/              # Domain models, Enums, DTOs, RFC 4122 Validators
│   ├── MachineIDLogger.Infrastructure/    # Native Win32 interop, 64-bit Registry, WMI/CIM, SQLite store, File logs
│   ├── MachineIDLogger.Services/          # Discovery engine, Diagnostics runner, MachineGuid workflow, Cursor identity
│   ├── MachineIDLogger.Elevator/          # Dedicated UAC-elevated registry modification worker
│   └── MachineIDLogger/                   # WinUI 3 desktop client (Navigation, 18 views, themes)
├── tests/
│   └── MachineIDLogger.Tests/             # Automated test suite (GUID validation, SQLite CRUD, Exports, Logging)
├── tools/
│   ├── lib/machineid-core.ps1             # Standalone PowerShell engine
│   ├── machineid-current.bat              # Quick inspection tool
│   ├── machineid-change.bat               # Verified mutation tool
│   ├── machineid-restore.bat              # Interactive backup restore tool
│   └── machineid-history.bat              # Audit log inspector
├── installer/
│   ├── MachineIDLogger.iss                # Inno Setup installation script
│   └── build-installer.ps1                # Automated release & packaging pipeline
└── dist/
    ├── MachineIDLogger-Setup-v1.0.0.exe            # Standalone Inno Setup Windows installer
    ├── MachineIDLogger-v1.0.0-Portable-win-x64/    # Unpackaged ready-to-run desktop application
    └── MachineIDLogger-v1.0.0-Portable-win-x64.zip # Portable distribution archive
```

---

## 4. Issues Ledger & Root Cause Analyses

### Issue #001: Missing .NET 10 Runtime on Target Systems (`Event ID 1023`)
* **Symptom**: `MachineIDLogger.Elevator.exe` or `MachineIDLogger.exe` fails to start with:
  > *You must install or update .NET to run this application. Framework: 'Microsoft.NETCore.App', version '10.0.0'*
* **Root Cause**: Build was initially configured with `--self-contained false`, relying on system-wide .NET 10 in `C:\Program Files\dotnet\`. Client machines having only .NET 8 could not run the binaries.
* **Fix**:
  1. Updated `installer/build-installer.ps1` to publish with `--self-contained true` for both `MachineIDLogger` and `MachineIDLogger.Elevator`.
  2. Set `<PublishSingleFile>true</PublishSingleFile>` on `MachineIDLogger.Elevator.csproj`.
  3. Bundled the entire .NET 10 runtime directly into the distribution package.

---

### Issue #002: Application Not Opening / Silent Crash on Launch
* **Symptom**: Executable started process, logged initial discovery scan, then immediately exited without displaying a window.
* **Root Cause**:
  1. In `src/MachineIDLogger/MainWindow.xaml.cs`, `AppWindow.SetIcon("Assets/AppIcon.ico")` was called with a relative path. When the application was launched from a different working directory (e.g. Start Menu, desktop shortcut), the relative path failed to resolve.
  2. The `Assets/` directory files were not marked with `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>` in `MachineIDLogger.csproj`, so `Assets/AppIcon.ico` did not exist in the output publish folder.
  3. The unhandled exception in the `MainWindow` constructor aborted `OnLaunched` without creating a log file.
* **Fix**:
  1. Wrapped `SetTitleBar` and `AppWindow.SetIcon` inside defensive `try/catch` blocks.
  2. Changed icon path resolution to `Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico")` and verified `File.Exists()`.
  3. Configured `<Content Include="Assets\**"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></Content>` in `MachineIDLogger.csproj`.
  4. Added global `UnhandledException` and `AppDomain.CurrentDomain.UnhandledException` handlers in `App.xaml.cs` to write directly to `C:\MachineIDLogger\Logs\crash.log`.

---

### Issue #003: Missing Application Icon on Executable and Shortcuts
* **Symptom**: Application executable and Windows installer shortcuts displayed the generic blank application icon.
* **Root Cause**:
  1. `<ApplicationIcon>` property was not declared in `MachineIDLogger.csproj`.
  2. Inno Setup script (`MachineIDLogger.iss`) lacked `SetupIconFile` in `[Setup]` and `IconFilename` in `[Icons]`.
* **Fix**:
  1. Added `<ApplicationIcon>Assets\AppIcon.ico</ApplicationIcon>` to `MachineIDLogger.csproj` to embed the icon resource into the PE header of `MachineIDLogger.exe`.
  2. Added `SetupIconFile=..\src\MachineIDLogger\Assets\AppIcon.ico` to `[Setup]` in `installer/MachineIDLogger.iss`.
  3. Updated `[Icons]` in `MachineIDLogger.iss` to explicitly specify `IconFilename: "{app}\Assets\AppIcon.ico"`.
  4. Added explicit copying of `..\src\MachineIDLogger\Assets\*` into `{app}\Assets` in the installer files section.

---

### Issue #004: Execution Level & Administrator Rights
* **Symptom**: Standard non-elevated user mode restricted access to privileged WMI classes (e.g. detailed physical drive geometry and raw SMBIOS tables) and required secondary elevation for registry writes.
* **Root Cause**: `app.manifest` lacked `<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />`.
* **Fix**:
  1. Updated `src/MachineIDLogger/app.manifest` with `<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />`.
  2. Configured Inno Setup with `PrivilegesRequired=admin` to ensure installed shortcuts launch elevated.
  3. Verified `requireAdministrator` string embedded into compiled binary.

---

### Issue #005: Scannable QR Code and Dedicated Support Interface
### Issue #006: Template Placeholder Icon Replacement
* **Symptom**: Application icon on Windows desktop and installer displayed as a blank gray wireframe box with an "X".
* **Root Cause**: The default template asset was an empty wireframe placeholder image.
* **Fix**: Generated a custom Windows 11 Fluent microchip design with glowing cyan/gold traces and cryptographic key badge. Packed all 6 native Windows resolution frames (16x16, 32x32, 48x48, 64x64, 128x128, 256x256) at 32-bit depth into `AppIcon.ico` and embedded into `MachineIDLogger.exe` PE header and Inno Setup `SetupIconFile`.

---

### Issue #007: Annoying Startup UAC Permission Box
* **Symptom**: Yellow Windows UAC prompt ("Do you want to allow this app to make changes...") appeared every time the application was launched.
* **Root Cause**: `app.manifest` had been set to `level="requireAdministrator"`.
* **Fix**: Restored `level="asInvoker"` in `src/MachineIDLogger/app.manifest`. Standard hardware inspection runs instantly without UAC interruption. Least privilege is preserved: only mutations invoke `MachineIDLogger.Elevator.exe`.

---

### Issue #008: In-Place Upgrade / Update Detection in Setup Installer
* **Symptom**: Running the installer on a machine where MachineIDLogger was already installed treated it as a fresh install rather than offering an update.
* **Root Cause**: Inno Setup lacked an `InitializeSetup` routine detecting existing uninstall registry entries.
* **Fix**:
  1. Bumped product version to `1.0.1`.
  2. Implemented `DetectExistingInstallation` in Inno Setup Pascal code to check 64-bit and 32-bit registry keys.
  3. Prompts the user: *"MachineIDLogger (v1.0.0) is already installed. Do you want to UPDATE MachineIDLogger to version 1.0.1?"*
  4. Automatically switches wizard text to *"MachineIDLogger Update"* and upgrades in-place while keeping `C:\MachineIDLogger\` history intact.

---

### Issue #009: Beautified UPI Payment Presentation
* **Symptom**: QR code was displayed without branding or visual context.
* **Fix**: Generated `Assets/BeautifiedQr.png` incorporating a branded header, scannable QR code, verified recipient badge (`subhadipshil.pnb@ybl`), and zero-fee indicators. Integrated into both `CoffeePage.xaml` and `AboutPage.xaml`.

---

### Issue #010: Professional Non-AI Vector Icon (FaceSoter Standard)
* **Symptom**: User noted the previous 3D icon looked AI-generated and desktop shortcut icon displayed as a generic wireframe box with an "X".
* **Root Cause**:
  1. Asset used an isometric 3D render with complex gradients rather than clean procedural vector art.
  2. Inno Setup specified `IconFilename: "{app}\Assets\AppIcon.ico"` for shortcuts. Windows Explorer caches stale shortcuts or fails to bind multi-frame icons when pointing to external loose files.
  3. `[InstallDelete]` did not clean up stale desktop `.lnk` shortcuts.
* **Fix**:
  1. Created `tools/generate_icon.py` modeled after `FaceSoter/assets/generate_icon.py`. Generates crisp geometric PIL vectors (Fluent Blue `#0078D4` squircle, Navy core, golden contact pins, scanning brackets, and white cryptographic ID shield/keyhole emblem).
  2. Packed all 7 Windows standard resolutions (16x16, 24x24, 32x32, 48x48, 64x64, 128x128, 256x256) at 32-bit depth into `AppIcon.ico` and saved all high-DPI PNGs.
  3. Embedded `AppIcon.ico` directly into `MachineIDLogger.exe` PE header via `<ApplicationIcon>`.
  4. Updated Inno Setup shortcuts to reference `{app}\{#MyAppExeName}`, added `[InstallDelete]` for stale desktop shortcuts, and added `ChangesAssociations=yes` to flush shell icon caches.

---

### Issue #011: Blank UI Fields and Non-Responsive Buttons in WinUI 3 Views
* **Symptom**: On the About page, "Target Framework", "Architecture Target", "Developer", and "Repository" text blocks were blank, and clicking "Check for Updates" and "GitHub" buttons performed no action.
* **Root Cause**: In `AboutPage.xaml.cs` (and other views), `InitializeComponent()` was invoked before `ViewModel = App.Services.GetRequiredService<AboutViewModel>()`. In WinUI 3, compiled bindings (`{x:Bind}`) evaluate against `null` if the property is not populated before component initialization, leaving one-time bindings blank and command bindings disconnected.
* **Fix**:
  1. Moved `ViewModel = App.Services.GetRequiredService<...>()` to execute before `InitializeComponent()` across all 19 view classes.
  2. Added explicit `Click` event handler fallbacks (`CheckForUpdates_Click`, `OpenGitHub_Click`, `CopyUpiId_Click`, `ToggleQr_Click`) in both XAML and code-behind to guarantee responsive button behavior.
  3. Replaced `.ico` references in XAML image controls with `.png` (`ms-appx:///Assets/AppIcon.png`) for reliable rendering.

---

### Issue #012: Version 1.0.0 Release Lockdown & Dist Organization
* **Symptom**: Unreleased version `1.0.1` artifacts polluted the `dist/` directory alongside `1.0.0`.
* **Fix**:
  1. Strictly locked version to `v1.0.0` in `MachineIDLogger.csproj`, `IdentityAndUtilityViewModels.cs`, `MachineIDLogger.iss`, and `build-installer.ps1`.
  2. Purged all `*1.0.1*` artifacts from `dist/`.
  3. Automated `build-installer.ps1` to cleanly structure `dist/` containing solely the clean `v1.0.0` release files:
     - `dist/MachineIDLogger-Setup-v1.0.0.exe`
     - `dist/MachineIDLogger-v1.0.0-win-x64/`
     - `dist/MachineIDLogger-v1.0.0-win-x64.zip`

### Issue #013: Universal Clipboard Failure & DataTemplate Button Null Context Across App
* **Symptom**: Copy buttons across Overview, Machine GUID, Hardware Inventory, History, Logs, Cursor Identity, and About pages failed to copy text to clipboard. Clicking buttons inside list rows silently did nothing. Also, CPU usage and device counts rendered as "12 B" / "50 B".
* **Root Causes**:
  1. **WinUI 3 DataTemplate Scoping**: In compiled templates (`<DataTemplate x:DataType="models:Foo">`), `{x:Bind}` does *not* populate `FrameworkElement.DataContext`. Item-level event handlers attempting `(sender as Button).DataContext as Foo` always received `null`, causing list item copy and restore buttons to abort silently.
  2. **Unpackaged WinRT Clipboard**: Calling `Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage)` in unpackaged Win32 desktop apps failed to commit or was lost when garbage collected without `Clipboard.Flush()`, or threw `CLIPBRD_E_CANT_OPEN` COM exceptions when clipboard history or external utilities held the clipboard mutex.
  3. **Converter Discrepancies**: `ByteFormatConverter` was applied to percentage and integer metrics, causing CPU usage to display as "12 B" and hardware inventory item counts to display as "45 B".
* **Fix**:
  1. Implemented native Win32 clipboard fallback in `NativeMethods.cs` (`OpenClipboard`, `EmptyClipboard`, `SetClipboardData(CF_UNICODETEXT)`, `GlobalAlloc`, `GlobalLock`, `CloseClipboard`) with a 6-attempt exponential backoff retry loop.
  2. Created unified `ClipboardHelper.Copy(text, source)` in `Services/ClipboardHelper.cs` combining native Win32 clipboard write with WinRT `DataPackage` and `Clipboard.Flush()` for 100% reliable system-wide copy.
  3. Added `Tag="{x:Bind}"` across all DataTemplates in `MachineGuidPage.xaml`, `DeviceInventoryPage.xaml`, `HistoryPage.xaml`, `LogsPage.xaml`, and `CursorIdentityPage.xaml`, and updated handlers to read `(sender as FrameworkElement)?.Tag ?? (sender as FrameworkElement)?.DataContext`.
  4. Created `PercentageFormatConverter` and `DeviceCountConverter` in `Converters.cs` and fixed bindings across `OverviewPage.xaml`, `ProcessorPage.xaml`, `MemoryPage.xaml`, and `DeviceInventoryPage.xaml`.
  5. Audited every single button and interactive element across all 17 XAML pages. Added explicit `Click` handlers for `ExportReport_Click`, `RunDiagnostics_Click`, `Save_Click`, and `OpenDataFolder_Click`, and enhanced HTML report export to automatically launch in the user's default browser.

### Issue #014: Diagnostics Results Rendering, Elevated Process Manifest, Centered UI Window, and Dist Portable Packaging
* **Symptoms**:
  1. Diagnostics page displayed card summary counts (11 Pass, 1 Warning) but "DIAGNOSTIC TEST RESULTS" list remained empty below the header, and clicking "Run Diagnostics" did not render rows. Under "ERRORS", the zero count was blank.
  2. The application UI window opened stuck at (0, 0) in the top-left corner instead of centered on the user's display.
  3. User requested the application always open with administrator privileges directly (`requireAdministrator`).
  4. Portable package in `dist/` did not have "Portable" in its directory and zip name.
* **Root Causes**:
  1. In WinUI 3, placing a virtualizing `ListView` inside an unconstrained `StackPanel` inside an outer `ScrollViewer` causes the `ListView` measure pass to receive infinite available height, resulting in layout collapse to 0 height. Furthermore, having both `Command` and `Click` on the button caused duplicate re-entrant calls on the UI thread without background offloading.
  2. `MainWindow` in WinUI 3 lacks automatic screen centering and defaults to Windows default cascading/top-left placement.
  3. `app.manifest` was previously configured as `asInvoker`.
  4. `build-installer.ps1` previously named the folder `MachineIDLogger-v1.0.0-win-x64` without the "Portable" designator.
* **Fix**:
  1. Replaced `ListView` in `DiagnosticsPage.xaml` with `ItemsControl` (which renders items naturally within the outer `ScrollViewer` without virtualization collapse), removed duplicate `Command` bindings on the buttons, added `FallbackValue='0'` for `ErrorCount`, and wrapped `_diagService.RunDiagnosticsAsync()` in `Task.Run` with `if (IsRunning) return;` re-entrancy protection.
  2. Implemented `CenterOnScreen()` in `MainWindow.xaml.cs` using Windows App SDK `AppWindow.MoveAndResize`, computing display work area dimensions to center the 1240x800 window.
  3. Updated `app.manifest` to `<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />`.
  4. Updated `installer/build-installer.ps1` to produce `MachineIDLogger-v1.0.0-Portable-win-x64` and `MachineIDLogger-v1.0.0-Portable-win-x64.zip` with robust retry-protected `.NET` `ZipFile` compression.

### Issue #015: Inno Setup 740 Elevation Error on Auto-Launch, Page Navigation State Wipe, Buy Me a Coffee Web/UPI Fallback, Diagnostics Progress Bar & Expanded Hardware Checks
* **Symptoms**:
  1. **Installer Auto-Launch Error 740**: Upon completing setup with the Inno Setup installer, checking "Launch MachineIDLogger" and clicking Finish displayed an error: `Unable to execute file: ...\MachineIDLogger.exe. CreateProcess failed; code 740. The requested operation requires elevation.`
  2. **Page Navigation State Wipe**: When first opening the application, Overview showed hardware specs, machine identifier, and system platform details. However, navigating to another page (such as Diagnostics or Hardware) and returning to Overview left the fields blank.
  3. **Buy Me a Coffee Non-Functional**: Clicking "Open UPI Link" silently failed on standard Windows machines due to the absence of a registered `upi://` protocol handler.
  4. **Diagnostics UI Missing Progress & Missing Diagnostics**: Running diagnostics lacked an indeterminate progress bar or loading ring, giving no visual feedback while background checks were executing. In addition, speaker, display, network, and power hardware were not covered.
* **Root Causes**:
  1. `MachineIDLogger.exe` embedded manifest specifies `requireAdministrator`. In Inno Setup, the `[Run]` entry was missing the `runascurrentuser` flag, causing Inno Setup to attempt standard non-elevated `CreateProcess` which Windows rejects with error code 740.
  2. In `App.xaml.cs`, all 18 ViewModels were originally registered as `AddTransient`. Navigating between tabs destroyed and re-instantiated ViewModels. Furthermore, nested model properties in `SystemOverview` were not re-evaluated by `x:Bind` unless `Bindings.Update()` was explicitly triggered and `Overview` property changes were observed.
  3. Windows has no native handler for `upi://` URLs unless an emulator or specific mobile bridge is installed.
  4. Diagnostics had no `IsRunning` indicator bound to an active progress bar, and only covered 12 initial system checks.
* **Fix**:
  1. Updated `installer/MachineIDLogger.iss` line 61 to include `runascurrentuser` in the `[Run]` section (`Flags: nowait postinstall skipifsilent runascurrentuser`), enabling seamless elevated launch from the installer without error 740.
  2. Converted all 18 ViewModels in `App.xaml.cs` to `services.AddSingleton<...>()` so navigation preserves in-memory discovery data and state across the entire session.
  3. Added `NavigationCacheMode = NavigationCacheMode.Required` to pages, added `RefreshOverview()` on `OnNavigatedTo`, hooked `_discoveryService.ScanStateChanged` in `OverviewViewModel` to trigger automatic UI updates when scans complete, and called `Bindings.Update()` on navigation.
  4. Added `OpenBuyMeCoffeeWebCommand` and web fallback button to launch the developer's Buy Me a Coffee page in the default web browser, added clipboard copying for UPI ID `subhadipshil.pnb@ybl` when launching UPI links, and ensured all buttons have dual Command and Click handlers.
  5. Added an indeterminate `ProgressBar` and `ProgressRing` inside `DiagnosticsPage.xaml` linked to `ViewModel.IsRunning`, disabled buttons while running, replaced `Results` with a fresh `ObservableCollection` on completion, and added 4 new hardware diagnostic checks in `DiagnosticsService.cs` (Audio & Speaker Hardware, Display Output & Monitors, Network Gateway & Connectivity, and Power & Battery Subsystem), bringing the suite to 16 comprehensive checks.

### Issue #016: InvalidCastException on InfoBar.IsOpen Aborting XAML Bindings on About, Coffee & Overview Pages
* **Symptoms**:
  1. Navigating to "About & Support" or "Buy Me a Coffee ☕" rendered empty text for Target Framework, Architecture Target, Developer, Repository, and UPI PAYMENT ID. Clicking buttons failed to execute commands.
  2. Overview page nested bindings sporadically failed to evaluate on navigation.
* **Root Cause**:
  In `AboutPage.xaml`, `CoffeePage.xaml`, `OverviewPage.xaml`, `DiagnosticsPage.xaml`, `HistoryPage.xaml`, and `SettingsPage.xaml`, `InfoBar.IsOpen` (which is of type `bool`) was bound to string properties (`CopyFeedback` / `StatusMessage`) using `Converter={StaticResource BoolToVisConverter}`.
  WinUI 3's compiled binding generator generated code casting the converter's return value to `bool`: `(bool)LookupConverter("BoolToVisConverter").Convert(...)`.
  Because `BoolToVisConverter` returned `Visibility.Collapsed` (an enum), C# threw `System.InvalidCastException: Specified cast is not valid.` during page initialization. This exception silently aborted the page's binding pass, preventing all subsequent properties (AppName, TargetFramework, Architecture, Developer, GitHubRepoUrl, UpiId, buttons) from ever being evaluated or bound.
* **Fix**:
  1. Updated `BooleanToVisibilityConverter` to inspect `targetType`: if `targetType == typeof(bool) || targetType == typeof(bool?)`, it returns `bool` instead of `Visibility`, preventing any `InvalidCastException`. Added string null/whitespace checking.
  2. Created and registered `StringToBoolConverter` in `Converters.cs` and `App.xaml`, and updated all `InfoBar.IsOpen` bindings across `AboutPage.xaml`, `CoffeePage.xaml`, `OverviewPage.xaml`, `DiagnosticsPage.xaml`, `HistoryPage.xaml`, and `SettingsPage.xaml`.
  3. Added full action suite to `AboutPage.xaml` (Copy UPI ID, Open UPI Link, Buy Me a Coffee (Web), Show/Hide QR) matching `CoffeePage.xaml`.
  4. Added bulletproof browser launcher with `cmd.exe /c start "" "<url>"` fallback in `AboutViewModel.LaunchBrowser` to ensure URLs open reliably even under elevated process contexts.

---

## 5. Verification & Testing Playbook

Run these commands from PowerShell in the workspace root:

```powershell
# 1. Run automated unit test suite (17 tests)
& "$env:USERPROFILE\.dotnet\dotnet.exe" test MachineIDLogger.slnx -c Release --nologo

# 2. Build Release configuration
& "$env:USERPROFILE\.dotnet\dotnet.exe" build MachineIDLogger.slnx -c Release --nologo

# 3. Compile full self-contained release and Inno Setup installer
powershell -ExecutionPolicy Bypass -File installer/build-installer.ps1

# 4. Launch the local unpackaged portable executable
& "dist\MachineIDLogger-v1.0.0-Portable-win-x64\MachineIDLogger.exe"
```

---

## 6. Quick Checklist for Future Conversations

When troubleshooting or adding new features:
1. **Check Logs First**: Always inspect `C:\MachineIDLogger\Logs\crash.log` and `C:\MachineIDLogger\Logs\machineid-yyyyMMdd.log`.
2. **Never Fabricate Data**: If an API or WMI query returns null/empty, assign `"Not available"` or `"Not exposed by firmware"`. Do not use mock values.
3. **Preserve Least Privilege**: Only the standalone worker `MachineIDLogger.Elevator.exe` should request administrator elevation via UAC.
4. **Maintain Offline Privacy**: Never add telemetry, tracking, or network calls other than the user-initiated GitHub Releases update checker.
5. **Always Update This File**: When fixing bugs or adding major architectural components, append the details to Section 4.
