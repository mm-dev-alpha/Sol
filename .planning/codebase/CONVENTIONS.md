# Coding Conventions

**Analysis Date:** 2026-08-31

## Naming Patterns

**Files & Types:**
- C# Classes, Interfaces, Enums, Structs: PascalCase (e.g., `ActiveDirectoryService.cs`, `IComputerDiagnosticService.cs`, `ComputerHardwareSnapshot.cs`)
- Views & Windows: PascalCase ending in `Page.xaml` or `Window.xaml` (e.g., `UserWorkspacePage.xaml`, `ProcessManagerWindow.xaml`)
- ViewModels: PascalCase ending in `ViewModel.cs` (e.g., `ComputerWorkspaceViewModel.cs`)
- Tests: PascalCase ending in `Tests.cs` (e.g., `AttributeEditorSafetyTests.cs`)

**Methods & Properties:**
- Methods: PascalCase (e.g., `GetHardwareSnapshotAsync`, `SearchUsersAsync`, `CopyAllDetails`)
- Asynchronous Methods: Must end with the `Async` suffix (e.g., `LoadUserAsync`, `ResetPasswordAsync`)
- Properties: PascalCase (e.g., `CurrentComputer`, `IsLoading`, `HardwareSnapshot`)
- Relay Commands: Defined with `[RelayCommand]` on methods or `IAsyncRelayCommand` properties (e.g., `CopyAllCommand`, `RefreshDiagnosticsCommand`)

**Variables & Fields:**
- Private Instance Fields: camelCase with leading underscore (e.g., `_adService`, `_diagnosticService`, `_navigationService`)
- Method Parameters & Local Variables: camelCase (e.g., `samAccountName`, `cancellationToken`, `targetHost`)
- Constants & Static Readonly: PascalCase (e.g., `S`, `CurrentLanguage`)

## Code Style

**C# Language Features (.NET 10 / C# 14):**
- Nullable Reference Types enabled (`<Nullable>enable</Nullable>`) across all projects. Use `?` for nullable references and perform null checking or pattern matching.
- Implicit Usings enabled (`<ImplicitUsings>enable</ImplicitUsings>`).
- File-scoped namespaces used consistently (`namespace Sol.Services;`).
- Pattern matching (`if (service is not null)`, `is T service`, `is NavigationViewItem item`).
- Expression-bodied members for concise properties and methods.

**WinUI 3 & XAML Compilation Guidelines (Prevent WMC9999):**
- **Inside `<DataTemplate>`**:
  - Always use the fully qualified static namespace for resource strings and static helpers (e.g. `{x:Bind local:Strings.S.PropertyName}`) instead of `{x:Bind S.PropertyName}` or `{x:Bind PageRoot.S.PropertyName}`.
  - `{x:Bind}` within a `DataTemplate` evaluates strictly against the declared `x:DataType`. Attempting to resolve unqualified page-level or helper properties causes an internal XAML compiler crash (`WMC9999`).
- **Control Properties & API Compatibility**:
  - Verify WinUI 3 / Windows App SDK control specifications before binding properties (e.g., `SelectorBar` uses `SelectedItem` and `SelectionChanged`, not `SelectedIndex`).
- **Compiler UI Language**:
  - Always keep `<XamlCompilerUILanguage>en-US</XamlCompilerUILanguage>` in `Sol.csproj` to prevent XAML compilation failures on non-English / German Windows developer machines.

## Localization & String Architecture

**Zero Hardcoded Strings Rule:**
- **Strict Centralized Strings**: ALL user-facing text (labels, headers, placeholders, tooltips, buttons, dialogs, toasts, InfoBars, exception messages displayed to the user) MUST be maintained centrally in `src/Sol/Helpers/Strings.cs` (`Strings.S.*`).
- **No Raw String Literals**: Never place raw string literals in XAML view markup (`Content="..."`, `Header="..."`, `PlaceholderText="..."`, `ToolTipService.ToolTip="..."`) or C# ViewModels / Services.
- **Language Standard**: Sol is English-only (`en-US`). All string definitions in `Strings.cs` must be clear, concise, and professional English using standard Microsoft Windows Server & Active Directory terminology (*Password*, *Account*, *Security Identifier (SID)*, *Workspace*, *Organizational Unit (OU)*).

## UI/UX Design System & Uniformity Standards

**Card & Component Uniformity:**
- Section headers, card containers, badge pills, loading states, and error cards across all pages/workspaces follow identical structural and visual patterns:
  - **Header**: Section title (`SubtitleTextBlockStyle`, `TextFillColorSecondaryBrush`), optional status/count pill (`Padding="8,2"` or `Padding="6,2"`, optical vertical centering), and right-aligned subtle action/refresh button (`SubtleIconButtonStyle`, `Glyph="&#xE72C;"`).
  - **Loading State**: `SettingsCard` with `ProgressRing` (`Width="16" Height="16"`), `CaptionTextBlockStyle`, `TextFillColorSecondaryBrush`.
  - **Error State**: `SettingsCard` with `HeaderIcon` containing `FontIcon Glyph="&#xE783;"` (`SystemFillColorCautionBrush`) and header text localized via `Strings.S.*`.
  - **State Integrity**: Status badge pills (Health, Status, Counts) in section headers strictly require `IsSuccess` and valid data before rendering to prevent false-positive indicators on error states.

## Workspace Data Export & "Copy All" Completeness

**Zero-Exception Completeness:**
- The "Copy All" action on both the User Workspace (`UserWorkspaceViewModel.CopyAll`) and Computer Workspace (`ComputerWorkspaceViewModel.CopyAllDetails`) must comprehensively export 100% of all loaded data without exception.
- Output must be formatted into clean, structured, human-readable sections with aligned key-value pairs.
- **Mandatory Maintenance Rule**: Whenever any new property, information card, diagnostic module, or external integration data (e.g. BitLocker, Battery, Disk health, JIRA tickets) is added to a workspace, the corresponding `CopyAll` / `CopyAllDetails` method in `UserWorkspaceViewModel.cs` or `ComputerWorkspaceViewModel.cs` MUST be expanded concurrently to include the new information.

## Security & Defensive Programming

**Input Sanitization & Injection Prevention:**
- All LDAP search queries MUST be sanitized via `LdapFilterHelper.EscapeLdapFilter` to prevent LDAP filter injection attacks.
- External command invocations (`sc.exe`, `taskkill.exe`, `logoff.exe`, `gpupdate.exe`) MUST use `ProcessStartInfo.ArgumentList` instead of raw concatenated argument strings.
- Active Directory write operations strictly enforce whitelisted editable attributes in `ActiveDirectoryService.IsAttributeEditable`.

**Audit Logging & Error Handling:**
- All Active Directory attribute edits and state modifications are logged locally to `%LocalAppData%\Sol\Logs\ad_audit.log` via `AdAuditLogger.LogAttributeChangeAsync`.
- User notifications use the global toast InfoBar via `WeakReferenceMessenger.Default.Send(new AppNotificationMessage(message, severity))`.
- Critical exceptions are caught gracefully and logged to `%LocalAppData%\Sol\crash.log` without crashing the application shell.

---

*Convention analysis: 2026-08-31*
