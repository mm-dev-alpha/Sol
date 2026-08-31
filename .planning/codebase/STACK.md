# Technology Stack

**Analysis Date:** 2026-08-31

## Languages

**Primary:**
- C# 14 (.NET 10.0) - Core application logic, ViewModels, Services, Models, and Helpers (`src/Sol/`)
- XAML (WinUI 3 / Windows App SDK) - UI declarative layouts, controls, data templates, and animations (`src/Sol/Views/`, `src/Sol/MainWindow.xaml`, `src/Sol/App.xaml`)

**Secondary:**
- PowerShell - Build scripts, directory clean-up, and release automation (`clear.ps1`, `.github/workflows/`)

## Runtime

**Environment:**
- .NET 10.0 Desktop CLR (`net10.0-windows10.0.26100.0`)
- Windows App SDK 1.7 / 2.4.0 (WinUI 3)
- Windows Desktop Runtime Execution Model: Unpackaged / Self-Contained (`<WindowsPackageType>None</WindowsPackageType>`, `<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>`)
- Target Platform: `windows10.0.26100.0` (Minimum: `10.0.17763.0` - Windows 10 Version 1809 / Windows Server 2019)
- Target Architectures: `x64`, `ARM64`, `x86`

**Package Manager:**
- NuGet (via MSBuild PackageReference in `src/Sol/Sol.csproj` and `src/Sol.Tests/Sol.Tests.csproj`)
- Lockfile: Managed via project SDK references

## Frameworks

**Core:**
- Microsoft.WindowsAppSDK (Version 2.4.0) - WinUI 3 modern desktop presentation framework, Mica backdrop, Windows App Runtime
- Microsoft.Windows.SDK.BuildTools (Version 10.0.28000.2705) - Windows SDK build targets and header tools
- Microsoft.Windows.SDK.BuildTools.WinApp (Version 0.6.1) - Windows App SDK build targets

**MVVM & UI Toolkits:**
- CommunityToolkit.Mvvm (Version 8.4.2) - Source-generated MVVM framework (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`, `WeakReferenceMessenger`)
- CommunityToolkit.Diagnostics (Version 8.4.2) - High-performance guard clauses and validation utilities
- CommunityToolkit.WinUI.Controls.SettingsControls (Version 8.2.251219) - Fluent SettingsCard and SettingsExpander components
- CommunityToolkit.WinUI.Animations (Version 8.2.251219) - Implicit and explicit XAML visual animations
- CommunityToolkit.WinUI.Media (Version 8.2.251219) - Acrylic/Mica brushes, visual effects, and media helpers

**Dependency Injection & Infrastructure:**
- Microsoft.Extensions.Hosting (Version 10.0.11) - Generic host application lifecycle, DI container (`IHost`, `IServiceProvider`)
- Microsoft.Extensions.Caching.Memory (Version 10.0.11) - In-memory query caching
- Serilog.Extensions.Hosting (Version 10.0.0) - Application logging integration
- Serilog.Sinks.File (Version 7.0.0) - Rolling file sink for local audit and error logging

**Testing:**
- xUnit (Version 2.9.3) - Unit test runner and test assertion framework (`src/Sol.Tests/`)
- xunit.runner.visualstudio (Version 3.1.4) - Visual Studio & `dotnet test` integration runner
- Microsoft.NET.Test.Sdk (Version 17.14.1) - MSBuild test target infrastructure
- coverlet.collector (Version 6.0.4) - Code coverage data collector

## Key Dependencies

**Critical:**
- System.DirectoryServices (Version 9.0.5) - Low-level LDAP client (`DirectoryEntry`, `DirectorySearcher`) for Active Directory user/computer queries, group memberships, and BitLocker recovery key retrieval (`msFVE-RecoveryInformation`)
- System.DirectoryServices.AccountManagement (Version 9.0.5) - High-level Active Directory account management (`PrincipalContext`, `UserPrincipal`, `ComputerPrincipal`, `GroupPrincipal`)
- System.Management (Version 10.0.11) - WMI / CIM infrastructure (`ManagementScope`, `ManagementObjectSearcher`, `ManagementClass`) for remote hardware, uptime, disk, battery, session, process, and service diagnostics
- System.Security.Cryptography.ProtectedData (Version 9.0.5) - Windows DPAPI encryption wrapper for fallback secure credential storage
- Windows.Security.Credentials.PasswordVault - Windows Credential Locker for secure hardware-backed storage of Atlassian Jira API tokens and PATs

## Configuration

**Environment:**
- Unpackaged WinUI 3 application manifest: `src/Sol/app.manifest`
- Application packaging metadata: `src/Sol/Package.appxmanifest`
- Visual Studio launch profiles: `src/Sol/Properties/launchSettings.json`
- Application Settings Persistence: `%LocalAppData%\Sol\settings.json` (managed via `SettingsService.cs`)
- Audit Logging: `%LocalAppData%\Sol\Logs\ad_audit.log` (managed via `AdAuditLogger.cs`)
- Unhandled Crash Logging: `%LocalAppData%\Sol\crash.log` (managed via `App.xaml.cs`)

**Build Configuration:**
- `src/Sol/Sol.csproj`:
  - `<XamlCompilerUILanguage>en-US</XamlCompilerUILanguage>` (enforces strict English XAML compiler error reporting and prevents `MissingManifestResourceException` on localized host OS)
  - `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>`
  - `<WindowsPackageType>None</WindowsPackageType>` and `<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>`
  - Publish profiles: `src/Sol/Properties/PublishProfiles/win-x64.pubxml`, `win-arm64.pubxml`, `win-x86.pubxml`

## Platform Requirements

**Development:**
- Windows 11 (22H2+ recommended) or Windows 10 (Build 17763+)
- .NET 10.0 SDK
- Visual Studio 2022 / 2026 or VS Code with C# Dev Kit and Windows App SDK build tools
- Optional: Domain-joined workstation or RSAT installed for local Active Directory testing

**Production:**
- Supported OS: Windows 11, Windows 10 (Version 1809+, Build 17763+), Windows Server 2025 / 2022 / 2019
- Prerequisites: None (self-contained .NET 10 and Windows App SDK runtime packaged into single standalone deployment)
- Network connectivity: TCP Port 389/636 (LDAP/LDAPS), Port 88 (Kerberos), Port 445 (SMB), Port 135 & dynamic RPC range (WMI/DCOM), HTTPS (Jira REST API)

---

*Stack analysis: 2026-08-31*
