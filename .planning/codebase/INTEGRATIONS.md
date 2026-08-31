# External Integrations

**Analysis Date:** 2026-08-31

## APIs & External Services

**Directory Services (Active Directory):**
- Microsoft Active Directory / Windows Domain Services
  - Protocol: LDAP / Kerberos / NTLM over TCP 389, 636, 3268, 3269
  - Client SDK: `System.DirectoryServices` and `System.DirectoryServices.AccountManagement`
  - Implementation: `src/Sol/Services/ActiveDirectoryService.cs`
  - Authentication: Windows Integrated Authentication via current thread/user token (`WindowsIdentity.GetCurrent()`)
  - Features:
    - User and computer object discovery and fuzzy LDAP querying (`LdapFilterHelper.EscapeLdapFilter` in `src/Sol/Helpers/LdapFilterHelper.cs`)
    - In-place attribute modification (`title`, `department`, `physicalDeliveryOfficeName`, `telephoneNumber`, `mobile`, `streetAddress`, `l`, `st`, `postalCode`, `description`, `wWWHomePage`, `manager`)
    - Account lifecycle control (unlock account, enable/disable account, reset password with auto-generated 16-char complex password, force password change flag)
    - Direct reports tree and security group memberships (`memberOf`)
    - BitLocker recovery key discovery via child object lookup (`msFVE-RecoveryInformation`)

**Remote Systems Diagnostics & Management (WMI / CIM):**
- Windows Management Instrumentation (WMI / CIM)
  - Protocol: DCOM / RPC (Port 135 + dynamic high RPC port range)
  - Client SDK: `System.Management` (`ManagementScope`, `ManagementObjectSearcher`, `ManagementClass`)
  - Implementation: `src/Sol/Services/ComputerDiagnosticService.cs`
  - Namespaces & Classes:
    - `root\cimv2`: `Win32_OperatingSystem`, `Win32_ComputerSystem`, `Win32_Bios`, `Win32_Processor`, `Win32_PhysicalMemory`, `Win32_LogicalDisk`, `Win32_DiskDrive`, `Win32_Battery`, `Win32_Process`, `Win32_Service`, `Win32_LoggedOnUser`
    - `root\cimv2\Security\MicrosoftVolumeEncryption`: `Win32_EncryptableVolume` (BitLocker volume encryption state, protection status, encryption algorithm, suspend/resume protection)
    - `root\default`: `StdRegProv` (Pending reboot registry flags: Component-Based Servicing, Windows Update `RebootRequired`, `PendingFileRenameOperations`)
  - CLI Fallback Utilities (via structured `ProcessStartInfo.ArgumentList`):
    - `sc.exe`: Service querying, starting, stopping, restarting, and startup configuration fallback
    - `tasklist.exe` / `taskkill.exe`: Remote process listing and termination fallback
    - `qwinsta.exe` / `logoff.exe`: Remote interactive/RDP session listing and session termination
    - `gpupdate.exe`: Remote Group Policy refresh trigger (`gpupdate.exe /force /nowait`)
    - `ping.exe`, `mstsc.exe`, `powershell.exe`: Quick diagnostic action launches

**Issue Tracking & Service Desk (Atlassian Jira):**
- Jira Cloud & Jira Data Center / Server REST API
  - Protocol: HTTPS REST API (JSON)
  - Client: `System.Net.Http.HttpClient`
  - Implementation: `src/Sol/Services/JiraService.cs`
  - Endpoints:
    - Server info verification: `/rest/api/2/serverInfo` or `/rest/api/2/myself`
    - JQL search: `/rest/api/2/search?jql={escapedJql}&startAt={start}&maxResults={max}`
  - Supported Auth Modes:
    - Jira Data Center / Server: Personal Access Token (Bearer Token Authorization header)
    - Jira Cloud: Basic Authentication (`email:api_token` encoded in Base64)

## Data Storage

**Databases:**
- No external SQL/NoSQL database required. Direct enterprise source-of-truth is Active Directory and live remote WMI endpoints.

**File Storage & Local Persistence:**
- Local Filesystem only:
  - Settings: `%LocalAppData%\Sol\settings.json` (managed via `src/Sol/Services/SettingsService.cs`)
  - Audit Logs: `%LocalAppData%\Sol\Logs\ad_audit.log` (JSON Lines format managed via `src/Sol/Services/AdAuditLogger.cs`)
  - Crash Dumps: `%LocalAppData%\Sol\crash.log` (unhandled exception log via `src/Sol/App.xaml.cs`)

**Caching:**
- `Microsoft.Extensions.Caching.Memory.IMemoryCache`: In-memory caching for repetitive directory and diagnostic queries during active user sessions.

## Authentication & Identity

**Active Directory / Windows Domain:**
- Windows Integrated Identity / Kerberos / NTLM ticket passing. Uses ambient security context of the running Windows user (`WindowsIdentity.GetCurrent()`).

**Jira Token Storage:**
- Implementation: `src/Sol/Helpers/JiraCredentialHelper.cs`
- Primary: Windows Credential Locker (`Windows.Security.Credentials.PasswordVault`) using resource identifier `"Sol_Jira_Token"`
- Fallback: Windows DPAPI (`System.Security.Cryptography.ProtectedData.Protect` with `DataProtectionScope.CurrentUser`) stored under `%LocalAppData%\Sol\jira.dat`
- Zero plaintext credentials stored on disk.

## Monitoring & Observability

**Error Tracking:**
- Local crash handler in `src/Sol/App.xaml.cs` trapping `Application.UnhandledException` and persisting stack trace to `%LocalAppData%\Sol\crash.log`.
- Zero third-party telemetry, cloud analytics, or external phone-home trackers.

**Logs:**
- Local audit logging for all Active Directory attribute and state modifications in `src/Sol/Services/AdAuditLogger.cs`. Each operation writes a timestamped JSONL record (`TimestampUtc`, `TargetUserSam`, `AttributeName`, `OldValue`, `NewValue`, `OperatorIdentity`, `Success`, `ErrorMessage`).
- Rolling file support with log rotation to prevent unbounded disk growth.

## CI/CD & Deployment

**Hosting & Distribution:**
- GitHub Releases (`https://github.com/mm-dev-alpha/Sol/releases`)
- Packaged as standalone, self-contained zip archives (`Sol-vX.Y.Z-win-x64.zip`) with SHA256 checksum verification (`checksums.sha256`).

**CI Pipeline:**
- Continuous Integration: `.github/workflows/ci.yml` (triggers on push/PR to `main`, executes `dotnet restore`, `dotnet test`, `dotnet build -c Release`).
- Release Automation: `.github/workflows/release.yml` (triggers on git tag `v*.*.*`, executes unit tests, builds self-contained `win-x64` publish payload, generates zip archive with SHA256 checksums, and publishes GitHub Release with release notes).

## Environment Configuration

**Required Environment & Network Connectivity:**
- Domain network connection or VPN with reachable Domain Controller (TCP 389/636/88/445)
- RPC / WMI network accessibility (TCP 135 and dynamic RPC ports) for remote diagnostics
- Remote Registry service enabled on target computers for pending reboot detection
- Windows Remote Administration firewall rules enabled on endpoints for WMI/RPC

**Secrets Location:**
- Zero hardcoded secrets in source code or repository.
- Jira credentials secured exclusively in Windows Credential Locker / DPAPI.

## Webhooks & Callbacks

**Incoming:**
- None (client-only Windows desktop application).

**Outgoing:**
- None (all communications are direct request/response via LDAP, WMI, or Jira REST).

---

*Integration audit: 2026-08-31*
