# Security Policy
## Reporting a Vulnerability

We take the security and integrity of Sol seriously. Since Sol interacts directly with enterprise Active Directory environments and system diagnostics, responsible disclosure of security vulnerabilities is deeply appreciated.

If you discover a security vulnerability in Sol:

1. **Do not create a public GitHub issue.**
2. Please report the issue privately via [GitHub Private Vulnerability Reporting](https://github.com/mm-dev-alpha/Sol/security/advisories/new) or by contacting the maintainer directly.
3. Include detailed steps to reproduce the vulnerability, sample payloads or scenarios, and any relevant logs.

We will acknowledge receipt of your report within 48 hours and provide a timeline for triage and remediation.

---

## Security Architecture & Design Principles

- **Zero Plaintext Credential Storage**: Jira Personal Access Tokens and API Tokens are persisted securely using Windows Credential Locker (`Windows.Security.Credentials.PasswordVault` backed by DPAPI). Plaintext tokens are never stored in settings files or committed to disk.
- **Strict LDAP Sanitization**: All directory search queries and identity lookups are escaped per RFC 4515 via `LdapFilterHelper.Escape` to prevent LDAP filter injection.
- **Safe Command Execution**: External process invocations (`taskkill.exe`, `sc.exe`, `logoff.exe`) utilize structured `ProcessStartInfo.ArgumentList` parameterization instead of concatenated shell commands, preventing argument injection.
- **Critical OS Process & Service Protection**: Critical Windows operating system processes (PID 0–4, system binaries) and essential services (RPC, LSASS, EventLog, DHCP, etc.) are protected against accidental termination or stoppage.
- **Zero Telemetry**: Sol does not collect, transmit, or phone home any telemetry, metrics, or personal information.
