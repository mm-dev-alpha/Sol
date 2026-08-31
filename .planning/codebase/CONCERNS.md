# Codebase Concerns

**Analysis Date:** 2026-08-31

## Tech Debt

**Monolithic Diagnostic Service & ViewModel:**
- Issue: `ComputerDiagnosticService.cs` (~2,516 lines) and `ComputerWorkspaceViewModel.cs` (~1,639 lines) contain logic for 8 different diagnostic domains (Hardware, Uptime, Disks, Battery, Sessions, Processes, Services, BitLocker).
- Files: `src/Sol/Services/ComputerDiagnosticService.cs`, `src/Sol/ViewModels/ComputerWorkspaceViewModel.cs`, `src/Sol/Views/ComputerWorkspacePage.xaml`
- Impact: High cognitive load, merge conflict risk when modifying separate diagnostic features, and difficult modular testing.
- Fix approach: Refactor into modular diagnostic provider classes (e.g., `HardwareDiagnosticProvider`, `StorageDiagnosticProvider`, `BatteryDiagnosticProvider`, `ServicesDiagnosticProvider`) injected via an orchestrating facade or composite diagnostic service. Extract individual diagnostic cards in `ComputerWorkspacePage.xaml` into dedicated WinUI 3 `UserControl`s.

**Unused WinUI 3 Template Artifacts:**
- Issue: `MainPage.xaml` and `MainPage.xaml.cs` are default template artifacts containing placeholder comments (`// TODO: Add your initialization logic here.`) that are not routed or displayed in the application lifecycle.
- Files: `src/Sol/MainPage.xaml`, `src/Sol/MainPage.xaml.cs`
- Impact: Minor clutter and developer confusion about the true landing page.
- Fix approach: Safely remove `MainPage.xaml` and `MainPage.xaml.cs` or document their deprecation since `HomePage.xaml` and `MainWindow.xaml` serve as the active shell and landing views.

## Known Bugs & Edge Cases

**Remote WMI / RPC Stalls on Blocked Firewalls:**
- Symptoms: When querying an offline or firewall-restricted remote computer where TCP port 135 is silently dropped (rather than rejected with RST), WMI queries can take up to the DCOM connection timeout before failing with an RPC error.
- Files: `src/Sol/Services/ComputerDiagnosticService.cs`, `src/Sol/ViewModels/ComputerWorkspaceViewModel.cs`
- Trigger: Entering a computer hostname that is offline or located behind a strict internal network firewall blocking DCOM/RPC.
- Workaround: UI indicates diagnostic loading state with cancellation support (`_diagnosticCts`). Pre-checking ICMP ping or optimizing timeout boundaries helps mitigate perceived latency.

## Security Considerations

**Active Directory Write Access Permissions:**
- Risk: In-place attribute editing, account unlocking, and password resets execute with the permissions of the current logged-in Windows user (`WindowsIdentity.GetCurrent()`). If the user has domain admin or delegated organizational unit rights, changes commit directly to Active Directory.
- Files: `src/Sol/Services/ActiveDirectoryService.cs`, `src/Sol/Services/AdAuditLogger.cs`
- Current mitigation:
  - Whitelist enforcement in `ActiveDirectoryService.IsAttributeEditable` prevents modifying critical security descriptors (`objectSid`, `nTSecurityDescriptor`, `userAccountControl`, `sAMAccountName`).
  - Password resets and account disable actions require explicit user confirmation dialogs before execution.
  - All write operations are recorded in `%LocalAppData%\Sol\Logs\ad_audit.log` via `AdAuditLogger`.
- Recommendations: Maintain regular security reviews of editable attribute whitelists when adding new profile fields.

**Remote Process Termination & Service Stoppage Guardrails:**
- Risk: Terminating critical Windows OS processes or stopping core services can cause blue screens or system instability on remote endpoints.
- Files: `src/Sol/Services/ComputerDiagnosticService.cs`, `src/Sol/Views/ProcessManagerWindow.xaml.cs`, `src/Sol/Views/ServicesInspectorWindow.xaml.cs`
- Current mitigation: Built-in safety lists protect PID 0-4 and critical system binaries (`smss.exe`, `csrss.exe`, `lsass.exe`, `services.exe`, `svchost.exe`) as well as 24 critical Windows services (`RpcSs`, `LSM`, `EventLog`, `SamSs`, `PlugPlay`, `DcomLaunch`, etc.).

## Performance Bottlenecks

**Sequential vs Parallel Remote Diagnostic Snapshots:**
- Problem: Loading a computer workspace initiates multiple remote queries (Hardware, Uptime, Disks, Battery, Sessions, BitLocker).
- Files: `src/Sol/ViewModels/ComputerWorkspaceViewModel.cs`, `src/Sol/Services/ComputerDiagnosticService.cs`
- Cause: If WMI queries run sequentially over high-latency networks, overall page load time increases.
- Current approach: Individual diagnostic tasks run in parallel via `Task.WhenAll` / concurrent async tasks, ensuring the UI populates cards as each snapshot completes without blocking the rest.

## Fragile Areas

**WinUI 3 `<DataTemplate>` Static Binding Crash (`WMC9999`):**
- Files: `src/Sol/Views/UserWorkspacePage.xaml`, `src/Sol/Views/ComputerWorkspacePage.xaml`, `src/Sol/Views/ServicesInspectorWindow.xaml`, `src/Sol/Views/ProcessManagerWindow.xaml`
- Why fragile: WinUI 3 XAML compiler (`XamlCompiler`) throws fatal internal error `WMC9999` if `{x:Bind}` inside a `DataTemplate` tries to resolve unqualified page properties or helpers rather than fully qualified static paths (e.g. `{x:Bind local:Strings.S.PropertyName}`).
- Safe modification: Always declare explicit `x:DataType` on every `DataTemplate` and qualify static helper/resource bindings with `local:Strings.S.*`.

**"Copy All" Completeness Requirement:**
- Files: `src/Sol/ViewModels/UserWorkspaceViewModel.cs` (`CopyAll`), `src/Sol/ViewModels/ComputerWorkspaceViewModel.cs` (`CopyAllDetails`)
- Why fragile: Adding new diagnostic modules or profile attributes to the UI without updating the string builders will cause exported diagnostic reports to be incomplete.
- Safe modification: Always update `CopyAll` and `CopyAllDetails` alongside any UI or model additions and verify output with `ExportServiceTests.cs`.

## Scaling Limits

**Large Active Directory Queries & Result Sets:**
- Current capacity: Fast search limits results with `SizeLimit` and fuzzy filter matches.
- Limit: Searching very large Active Directory forests (>100,000 objects) without specific filters could consume significant memory if unpaged.
- Scaling path: Continue utilizing targeted LDAP filters with `LdapFilterHelper.EscapeLdapFilter` and bounded search result limits.

## Dependencies at Risk

- `Microsoft.WindowsAppSDK` (Version 2.4.0) & `Microsoft.Extensions.*` (Version 10.0.11): Currently pinned to latest .NET 10 compatible preview/release builds. Ensure compatibility when upgrading minor versions of Windows App SDK.

## Missing Critical Features

- Automated UI/E2E Testing Pipeline: Unit tests cover 100% of parsers, safety logic, and export builders (150 tests), but WinUI 3 visual UI automation testing in headless CI is currently not configured.

## Test Coverage Gaps

**Live Active Directory Integration Tests:**
- What's not tested in CI: Direct connection to live Active Directory Domain Controllers (mocked/unit-tested at service boundary with whitelists and regexes).
- Files: `src/Sol/Services/ActiveDirectoryService.cs`
- Risk: Schema quirks or unexpected directory object configurations in non-standard Active Directory environments.
- Priority: Medium (requires dedicated staging domain or local LDAP mock container for automated integration testing).

---

*Concerns audit: 2026-08-31*
