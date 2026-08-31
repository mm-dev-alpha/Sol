<!-- refreshed: 2026-08-31 -->
# Architecture

**Analysis Date:** 2026-08-31

## System Overview

```text
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                                   Presentation Layer                                   │
├──────────────────────────┬───────────────────────────┬─────────────────────────────────┤
│       Main Window        │      Workspace Pages      │       Inspector Windows         │
│  `src/Sol/MainWindow.*`  │   `src/Sol/Views/*Page.*` │  `src/Sol/Views/*Window.*`      │
│  (Mica, TitleBar, Nav)   │   (Home, User, Computer,  │  (ProcessManagerWindow,         │
│                          │    Jira, Settings)        │   ServicesInspectorWindow)      │
└─────────────┬────────────┴─────────────┬─────────────┴────────────────┬────────────────┘
              │                          │                              │
              ▼                          ▼                              ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                                    ViewModel Layer                                     │
│                             `src/Sol/ViewModels/`                                     │
│  (ShellViewModel, HomeViewModel, UserWorkspaceViewModel, ComputerWorkspaceViewModel,   │
│   JiraWorkspaceViewModel, SettingsViewModel, GlobalSearchViewModel)                    │
│  - Observable Properties & Commands via CommunityToolkit.Mvvm                         │
│  - Decoupled Notification & Navigation Bus via WeakReferenceMessenger                  │
└────────────────────────────────────────┬───────────────────────────────────────────────┘
                                         │
                                         ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                                     Service Layer                                      │
│                               `src/Sol/Services/`                                      │
│  ├─ ActiveDirectoryService (LDAP Queries, User/Computer Management, BitLocker Keys)    │
│  ├─ ComputerDiagnosticService (WMI/CIM Snapshots, Remote Process/Service Control, CLI) │
│  ├─ JiraService (REST Client, Cloud & Data Center Ticket Queries)                      │
│  ├─ SettingsService (Local JSON Persistence, Domain & UI Config)                      │
│  ├─ NavigationService (Shell Navigation & Page Lifecycle Coordinator)                  │
│  ├─ SearchService (Unified Fast Search for Users and Computers)                        │
│  ├─ ExportService (Clipboard Data Formatting & Complete Copy All Export)               │
│  └─ AdAuditLogger (Local JSONL Audit Trail for Directory Modifications)                │
└──────────────┬─────────────────────────┬──────────────────────────────┬────────────────┘
               │                         │                              │
               ▼                         ▼                              ▼
┌──────────────────────────┬───────────────────────────┬─────────────────────────────────┐
│     Active Directory     │     Remote Endpoints      │       Atlassian Jira REST       │
│  (LDAP / Kerberos / AD)  │   (WMI / CIM / CLI RPC)   │   (HTTPS Cloud & Data Center)   │
└──────────────────────────┴───────────────────────────┴─────────────────────────────────┘
```

## Component Responsibilities

| Component | Responsibility | File |
|---|---|---|
| `App` | Application bootstrap, DI container lifecycle (`IHost`), global crash handler, and language configuration (`en-US`). | `src/Sol/App.xaml.cs` |
| `MainWindow` | App shell, Mica backdrop, custom TitleBar with Windows Identity context, Shell Navigation, and global InfoBar toast bus. | `src/Sol/MainWindow.xaml.cs` |
| `ShellViewModel` | Coordinates shell-level navigation and identity status bindings. | `src/Sol/ViewModels/ShellViewModel.cs` |
| `HomeViewModel` / `HomePage` | Central search hub, instant user/computer query dispatch, and quick action navigation. | `src/Sol/ViewModels/HomeViewModel.cs`, `src/Sol/Views/HomePage.xaml` |
| `UserWorkspaceViewModel` / `UserWorkspacePage` | User profile visualization, organizational tree, in-place attribute editing, password resets, account lockout management, Jira ticket overview, and complete Copy All export. | `src/Sol/ViewModels/UserWorkspaceViewModel.cs`, `src/Sol/Views/UserWorkspacePage.xaml` |
| `ComputerWorkspaceViewModel` / `ComputerWorkspacePage` | Computer AD properties, hardware/BIOS diagnostics, uptime/pending reboot detection, storage health, battery wear, active sessions, BitLocker keys, and action launchers. | `src/Sol/ViewModels/ComputerWorkspaceViewModel.cs`, `src/Sol/Views/ComputerWorkspacePage.xaml` |
| `ProcessManagerWindow` | Standalone inspection window displaying live remote processes with multi-column sorting and safe process termination guardrails. | `src/Sol/Views/ProcessManagerWindow.xaml.cs` |
| `ServicesInspectorWindow` | Standalone inspection window for remote Windows services with status filtering, service startup configuration, and safe start/stop/restart controls. | `src/Sol/Views/ServicesInspectorWindow.xaml.cs` |
| `JiraWorkspaceViewModel` / `JiraWorkspacePage` | Dedicated Jira workspace for browsing and searching user-associated issues. | `src/Sol/ViewModels/JiraWorkspaceViewModel.cs`, `src/Sol/Views/JiraWorkspacePage.xaml` |
| `SettingsViewModel` / `SettingsPage` | Configuration of target AD domain, language overrides, Jira connection test and credential storage. | `src/Sol/ViewModels/SettingsViewModel.cs`, `src/Sol/Views/SettingsPage.xaml` |
| `ActiveDirectoryService` | High-level and low-level LDAP querying, attribute updating, account state changes, and BitLocker recovery key discovery. | `src/Sol/Services/ActiveDirectoryService.cs` |
| `ComputerDiagnosticService` | Remote WMI/CIM querying, CLI fallbacks (`sc`, `taskkill`, `logoff`), warranty URL generation, and diagnostic snapshot generation. | `src/Sol/Services/ComputerDiagnosticService.cs` |
| `JiraService` | Jira REST API client for Cloud and Data Center environments. | `src/Sol/Services/JiraService.cs` |
| `SettingsService` | Local JSON configuration management in `%LocalAppData%\Sol\settings.json`. | `src/Sol/Services/SettingsService.cs` |
| `NavigationService` | Page-to-page navigation coordinator decoupled from View code-behind. | `src/Sol/Services/NavigationService.cs` |
| `SearchService` | Parallel background search execution for Users and Computers. | `src/Sol/Services/SearchService.cs` |
| `ExportService` | Complete Copy All export formatting and clipboard management. | `src/Sol/Services/ExportService.cs` |
| `AdAuditLogger` | Append-only local JSONL audit logger for directory modifications. | `src/Sol/Services/AdAuditLogger.cs` |
| `Strings` | Static centralized string provider (`Strings.S.*`) guaranteeing zero hardcoded UI strings. | `src/Sol/Helpers/Strings.cs` |
| `JiraCredentialHelper` | Hardware-backed credential storage via Windows Credential Locker with DPAPI fallback. | `src/Sol/Helpers/JiraCredentialHelper.cs` |

## Pattern Overview

**Overall Architecture:**
- Modern MVVM (Model-View-ViewModel) pattern implemented using `CommunityToolkit.Mvvm`.
- Dependency Injection container (`Microsoft.Extensions.Hosting.IHost`) configured at application startup.
- Reactive UI event aggregation via `CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger`.
- Compile-time `{x:Bind}` data binding in WinUI 3 XAML views with explicit data typing (`x:DataType`).

**Key Characteristics:**
- **Zero Hardcoded Strings**: All user-facing text is strictly defined in `src/Sol/Helpers/Strings.cs` (`Strings.S.*`).
- **Zero Telemetry & Full Local Privacy**: No telemetry, analytics, or third-party tracking; logs and credentials remain strictly local.
- **Defensive Security Guardrails**: Sanitized LDAP filter queries, structured process invocation via `ArgumentList`, and built-in protection against terminating critical OS processes or stopping essential Windows services.

## Layers

**Presentation Layer (`src/Sol/Views/`, `src/Sol/MainWindow.xaml`):**
- Purpose: WinUI 3 XAML visual presentation, layout containers, controls, animations, and window management.
- Contains: XAML page markup, code-behind for window styling/interop and visual transitions.
- Depends on: ViewModel Layer, Helper/String resources.

**ViewModel Layer (`src/Sol/ViewModels/`):**
- Purpose: State management, presentation logic, relay commands, asynchronous data loading, and event subscriptions.
- Contains: Observable properties, commands (`[RelayCommand]`), view state tracking (`IsLoading`, error banners).
- Depends on: Service Layer, Model Layer, Helper resources.

**Service Layer (`src/Sol/Services/`):**
- Purpose: Core domain operations, LDAP directory communications, remote WMI execution, HTTP REST calls, settings serialization, and audit logging.
- Contains: Interface contracts (`IActiveDirectoryService`, `IComputerDiagnosticService`, etc.) and implementations.
- Depends on: Model Layer, Helper libraries, .NET Runtime BCL.

**Model Layer (`src/Sol/Models/`):**
- Purpose: POCO data models, diagnostic snapshot records, and notification message wrappers.
- Contains: `AdUser`, `AdComputer`, `ComputerHardwareSnapshot`, `ComputerUptimeSnapshot`, `ComputerDiskDriveInfo`, `ComputerBatterySnapshot`, `ComputerSessionSnapshot`, `ComputerProcessInfo`, `ComputerServiceInfo`, `ComputerBitLockerSnapshot`, `JiraTicket`, `AppNotificationMessage`.
- Depends on: BCL only.

## Data Flow

### 1. User Search & Workspace Loading Flow

1. User enters query in `HomePage` or `MainWindow` search box.
2. `GlobalSearchViewModel.ExecuteSearchAsync` dispatches query to `ISearchService.SearchUsersAsync`.
3. `ActiveDirectoryService` executes sanitized LDAP query (`LdapFilterHelper.EscapeLdapFilter`) using `System.DirectoryServices.DirectorySearcher`.
4. If single user is found, `WeakReferenceMessenger` publishes `UserSearchSelectedMessage`.
5. `UserWorkspaceViewModel` receives message, navigates shell to `UserWorkspacePage`, and invokes `LoadUserAsync(samAccountName)`.
6. Full user profile, direct reports, manager hierarchy, and Jira tickets (if enabled) are asynchronously loaded in parallel and bound to the view.

### 2. Computer Remote Diagnostics Retrieval Flow

1. User searches for computer or opens `ComputerWorkspacePage`.
2. `ComputerWorkspaceViewModel.LoadComputerAsync` triggers parallel diagnostic tasks via `IComputerDiagnosticService`:
   - Hardware & BIOS snapshot (`GetHardwareSnapshotAsync`)
   - Uptime & pending reboot check (`GetUptimeSnapshotAsync`)
   - Logical & physical storage drive status (`GetDiskSnapshotAsync`)
   - Battery health & degradation snapshot (`GetBatterySnapshotAsync`)
   - Active interactive and RDP sessions (`GetSessionSnapshotAsync`)
   - BitLocker volume encryption & Active Directory recovery passwords (`GetBitLockerStatusAsync`)
3. `ComputerDiagnosticService` queries remote target using WMI / CIM over RPC (`System.Management`).
4. Snapshots are returned, ViewModel properties are updated, and UI cards render live health badges and diagnostic details.

### 3. Safe In-Place Attribute Modification & Audit Flow

1. User edits an allowed attribute (e.g. `Department`, `Title`, `Office`) on `UserWorkspacePage` and clicks Save.
2. `UserWorkspaceViewModel.SaveUserAsync` validates attribute against `ActiveDirectoryService.IsAttributeEditable` whitelist.
3. `ActiveDirectoryService.UpdateUserProfileAsync` modifies directory object via `DirectoryEntry.CommitChanges()`.
4. `AdAuditLogger.LogAttributeChangeAsync` writes structured JSONL entry to `%LocalAppData%\Sol\Logs\ad_audit.log`.
5. Success toast is broadcast via `WeakReferenceMessenger.Send(new AppNotificationMessage(...))`, and `MainWindow` displays the InfoBar notification.

## Key Abstractions

**Diagnostic Snapshot Pattern:**
- Purpose: Encapsulates remote endpoint query results, connection status, error messages, and raw data models in an immutable snapshot object.
- Examples: `ComputerHardwareSnapshot`, `ComputerUptimeSnapshot`, `ComputerDiskSnapshot`, `ComputerBatterySnapshot`, `ComputerSessionSnapshot`, `ComputerProcessSnapshot`, `ComputerBitLockerSnapshot`, `ComputerServicesSnapshot` in `src/Sol/Models/`.
- Pattern: Immutable snapshot with `IsSuccess`, `ErrorMessage`, and timestamp metadata.

**Notification Messaging Bus:**
- Purpose: Loosely coupled in-app toast notification delivery across isolated workspaces and standalone windows.
- Examples: `AppNotificationMessage` dispatched via `WeakReferenceMessenger.Default`.
- Pattern: Mediator / Event Aggregator.

## Entry Points

**Main Entry Point (`src/Sol/App.xaml.cs`):**
- Location: `Sol.App` (WinUI 3 application lifecycle)
- Triggers: Executable launch (`Sol.exe`)
- Responsibilities: Initializes Host DI container, overrides culture to `en-US`, sets up global unhandled exception handler, instantiates and activates `MainWindow`.

**Main Window Shell (`src/Sol/MainWindow.xaml.cs`):**
- Location: `Sol.MainWindow`
- Triggers: `App.OnLaunched`
- Responsibilities: Extends content into title bar, hooks Mica system backdrop, sets up custom AppTitleBar, registers navigation frame with `INavigationService`, binds identity context, and listens for global InfoBar notifications.

## Architectural Constraints

- **WinUI 3 UI Thread Affinity:** All UI bindings, ObservableCollection updates, and InfoBar presentations must execute on the `DispatcherQueue` UI thread.
- **WMC9999 XAML Compiler Crash Prevention:** In `<DataTemplate>`, `{x:Bind}` must strictly evaluate against the declared `x:DataType` or use fully qualified static namespaces (e.g., `{x:Bind local:Strings.S.PropertyName}`) rather than unqualified page properties.
- **XAML Compiler UI Language:** `Sol.csproj` must maintain `<XamlCompilerUILanguage>en-US</XamlCompilerUILanguage>` at all times to prevent WinUI 3 XAML compiler crash on non-English / German Windows host systems.
- **100% Comprehensive Export Rule:** Any new workspace property or diagnostic module must be immediately added to `UserWorkspaceViewModel.CopyAll` and `ComputerWorkspaceViewModel.CopyAllDetails`.
- **Protected System Processes & Services:** Hardcoded safety lists in `ComputerDiagnosticService.cs` prevent stopping critical OS services (24 protected services) and terminating core system processes (PID 0-4 and critical system binaries).

---

*Architecture analysis: 2026-08-31*
