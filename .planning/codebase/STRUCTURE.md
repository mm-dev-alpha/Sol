# Codebase Structure

**Analysis Date:** 2026-08-31

## Directory Layout

```text
Sol/
├── .github/
│   ├── ISSUE_TEMPLATE/                 # Issue templates for bug reports & features
│   └── workflows/
│       ├── ci.yml                      # Continuous integration build & test pipeline
│       └── release.yml                 # Automated release packaging & publishing
├── assets/                             # Marketing assets, logos, and showcase GIFs
├── src/
│   ├── Sol/                            # Main WinUI 3 desktop application
│   │   ├── Assets/                     # Application icons, store logos, and splash screen
│   │   ├── Converters/                 # XAML value converters for UI bindings
│   │   ├── Helpers/                    # String tables, credential lockers, time/LDAP helpers
│   │   ├── Models/                     # Data models, diagnostic snapshots, and messages
│   │   ├── Properties/
│   │   │   ├── PublishProfiles/        # Self-contained publish profiles (win-x64/arm64/x86)
│   │   │   └── launchSettings.json     # Debug launch settings
│   │   ├── Services/                   # AD, WMI diagnostics, Jira, Navigation, Settings
│   │   ├── Strings/
│   │   │   └── en-US/                  # Resource files (Resources.resw)
│   │   ├── ViewModels/                 # MVVM ViewModels
│   │   ├── Views/                      # WinUI 3 XAML Pages and standalone Inspector Windows
│   │   ├── App.xaml / App.xaml.cs      # Application entry point & DI configuration
│   │   ├── MainWindow.xaml / .cs       # Main shell window & navigation coordinator
│   │   ├── Sol.csproj                  # MSBuild project file (.NET 10 / Windows App SDK)
│   │   ├── app.manifest                # Win32 application manifest
│   │   └── Package.appxmanifest        # Windows package identity manifest
│   └── Sol.Tests/                      # Automated unit test suite (xUnit)
│       ├── AttributeEditorSafetyTests.cs # Security whitelist and audit logging tests
│       ├── ComputerDiagnosticServiceTests.cs # Parser, warranty, CLI parsing tests
│       ├── ExportServiceTests.cs       # Clipboard export formatting tests
│       ├── UnitTest1.cs                # Basic test harness
│       └── Sol.Tests.csproj            # Test project configuration
├── samples/                            # Embedded WinUI sample reference code
├── GEMINI.md                           # Strict project architectural & styling guidelines
├── README.md                           # Project documentation and showcase
├── SECURITY.md                         # Security policies and reporting
├── LICENSE                             # MIT License
└── clear.ps1                           # Build artifact cleanup helper script
```

## Directory Purposes

**`src/Sol/`:**
- Purpose: Primary application codebase containing all presentation, business logic, and infrastructure code.
- Contains: C# source files, XAML view files, assets, resources, and configuration manifests.
- Key files: `App.xaml.cs`, `MainWindow.xaml.cs`, `Sol.csproj`.

**`src/Sol/Views/`:**
- Purpose: WinUI 3 XAML views, user control definitions, and standalone diagnostic inspector windows.
- Contains: `HomePage.xaml`, `UserWorkspacePage.xaml`, `ComputerWorkspacePage.xaml`, `JiraWorkspacePage.xaml`, `SettingsPage.xaml`, `ProcessManagerWindow.xaml`, `ServicesInspectorWindow.xaml`.

**`src/Sol/ViewModels/`:**
- Purpose: MVVM presentation logic, reactive state management, and command handlers.
- Contains: `HomeViewModel.cs`, `UserWorkspaceViewModel.cs`, `ComputerWorkspaceViewModel.cs`, `JiraWorkspaceViewModel.cs`, `SettingsViewModel.cs`, `ShellViewModel.cs`, `GlobalSearchViewModel.cs`.

**`src/Sol/Services/`:**
- Purpose: Domain service implementations, Active Directory LDAP operations, WMI remote diagnostics, Jira API client, local configuration persistence, and audit logging.
- Contains: `ActiveDirectoryService.cs`, `ComputerDiagnosticService.cs`, `JiraService.cs`, `SettingsService.cs`, `NavigationService.cs`, `SearchService.cs`, `ExportService.cs`, `AdAuditLogger.cs`, `GreetingService.cs`.

**`src/Sol/Models/`:**
- Purpose: POCO data models, diagnostic snapshot wrappers, and messaging DTOs.
- Contains: `AdUser.cs`, `AdComputer.cs`, `ComputerHardwareSnapshot.cs`, `ComputerUptimeSnapshot.cs`, `ComputerDiskDriveInfo.cs`, `ComputerBatterySnapshot.cs`, `ComputerSessionSnapshot.cs`, `ComputerProcessInfo.cs`, `ComputerServiceInfo.cs`, `ComputerBitLockerSnapshot.cs`, `JiraTicket.cs`, `AppNotificationMessage.cs`.

**`src/Sol/Helpers/`:**
- Purpose: Static helpers, centralized localization string tables, credential vaults, and LDAP sanitizers.
- Contains: `Strings.cs`, `JiraCredentialHelper.cs`, `LdapFilterHelper.cs`, `TimeHelper.cs`.

**`src/Sol/Converters/`:**
- Purpose: XAML value converters for UI bindings.
- Contains: `AccountStatusToColorConverter.cs`, `BoolToThicknessConverter.cs`, `InvertedBoolConverter.cs`, `Converters.cs`.

**`src/Sol.Tests/`:**
- Purpose: Automated test project verifying diagnostic parsers, safety whitelists, audit logging, and export logic.
- Contains: `AttributeEditorSafetyTests.cs`, `ComputerDiagnosticServiceTests.cs`, `ExportServiceTests.cs`, `Sol.Tests.csproj`.

## Key File Locations

**Entry Points:**
- `src/Sol/App.xaml.cs`: Application lifecycle bootstrap, DI registration, global exception handler.
- `src/Sol/MainWindow.xaml.cs`: Shell frame setup, custom title bar, NavigationView binding.

**Configuration:**
- `src/Sol/Sol.csproj`: Main project configuration, NuGet packages, compiler language flag (`<XamlCompilerUILanguage>en-US</XamlCompilerUILanguage>`).
- `src/Sol/Properties/launchSettings.json`: Debug launch profiles.
- `src/Sol/Properties/PublishProfiles/win-x64.pubxml`: Single-file unpackaged self-contained publish profile.

**Core Logic:**
- `src/Sol/Services/ActiveDirectoryService.cs`: LDAP integration, user/computer CRUD, password reset, unlock.
- `src/Sol/Services/ComputerDiagnosticService.cs`: Remote WMI diagnostics, service and process management.
- `src/Sol/Services/JiraService.cs`: Atlassian Jira Cloud & Data Center REST client.
- `src/Sol/Helpers/Strings.cs`: Centralized static localization repository.

**Testing:**
- `src/Sol.Tests/Sol.Tests.csproj`: xUnit test project configuration.
- `src/Sol.Tests/ComputerDiagnosticServiceTests.cs`: Remote diagnostic parsing tests.
- `src/Sol.Tests/AttributeEditorSafetyTests.cs`: Safety whitelists and audit logging tests.

## Naming Conventions

**Files:**
- C# Classes: PascalCase matching class name (e.g., `ActiveDirectoryService.cs`, `UserWorkspaceViewModel.cs`)
- Views & Windows: PascalCase ending in `Page.xaml` or `Window.xaml` (e.g., `ComputerWorkspacePage.xaml`, `ProcessManagerWindow.xaml`)
- Tests: PascalCase ending in `Tests.cs` (e.g., `ComputerDiagnosticServiceTests.cs`)
- Models & Snapshots: PascalCase ending in `Snapshot.cs` or `Info.cs` (e.g., `ComputerHardwareSnapshot.cs`)

**Directories:**
- Plural PascalCase (e.g., `Views`, `ViewModels`, `Services`, `Models`, `Helpers`, `Converters`)

## Where to Add New Code

**Adding a New Workspace / Page:**
1. Create View: `src/Sol/Views/{Feature}Page.xaml` and `.xaml.cs`
2. Create ViewModel: `src/Sol/ViewModels/{Feature}ViewModel.cs` inheriting from `ObservableObject`
3. Register in DI: Add ViewModel and Service singletons to `src/Sol/App.xaml.cs` (`ConfigureServices`)
4. Register Navigation: Add page registration in `src/Sol/Services/NavigationService.cs` and add `NavigationViewItem` to `src/Sol/MainWindow.xaml`
5. Add Strings: Add all labels, headers, and tooltips to `src/Sol/Helpers/Strings.cs`
6. Add "Copy All" support: Ensure all loaded workspace data is formatted and included in the export method.

**Adding a New Remote Diagnostic Module:**
1. Create Model: `src/Sol/Models/Computer{Module}Snapshot.cs` with `IsSuccess`, `ErrorMessage`, and timestamp
2. Add Service Contract: Add `Get{Module}SnapshotAsync` method to `src/Sol/Services/IComputerDiagnosticService.cs`
3. Implement Service Method: Implement WMI/CLI querying logic in `src/Sol/Services/ComputerDiagnosticService.cs`
4. Bind in ViewModel: Add observable property in `src/Sol/ViewModels/ComputerWorkspaceViewModel.cs` and trigger during `LoadComputerAsync`
5. Create UI Card: Add `SettingsCard` or container in `src/Sol/Views/ComputerWorkspacePage.xaml` following standard design system template
6. Update "Copy All": Append new diagnostic section to `ComputerWorkspaceViewModel.CopyAllDetails`
7. Add Unit Tests: Add parsing and error handling unit tests in `src/Sol.Tests/ComputerDiagnosticServiceTests.cs`.

**Adding Helper / Utility Functions:**
- General helpers: `src/Sol/Helpers/`
- Value converters for XAML bindings: `src/Sol/Converters/`

## Special Directories

**`.github/workflows/`:**
- CI/CD automated build and release workflows (`ci.yml`, `release.yml`). Committed to Git.

**`publish/`, `releases/`:**
- Output directory for self-contained release builds and packaged zip archives. Ignored by Git.

**`samples/WinUI-Gallery/`:**
- Local reference samples from WinUI Gallery. Used for reference during UI development.

---

*Structure analysis: 2026-08-31*
