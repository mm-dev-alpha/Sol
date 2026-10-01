# Sol v3.6.0

Introduces remote Windows Services management, progressive computer diagnostic streaming, and Jira Cloud and Data Center integration.

## Highlights
- **Remote Windows Services Inspector**: Dedicated window to query, search, start, stop, and restart services across domain endpoints.
- **Progressive Diagnostic Streaming**: Remote computer diagnostic cards populate asynchronously as queries finish, without waiting for long-running checks.
- **Jira Integration**: Connects to Jira Cloud or Data Center to query user-assigned tickets directly from the User Workspace.
- **Structured Clipboard Export**: One-click export on User and Computer workspaces capturing all loaded attributes and diagnostics as structured text.

## Remote Diagnostics & Services
- **Services Management**: Filter services by status (Running, Stopped, All), search by name, and modify startup type (Automatic, Manual, Disabled).
- **Service Protection**: Guards 24 critical Windows OS services against accidental stoppage.
- **Asynchronous Streaming**: Hardware, uptime, and disk diagnostics render immediately while background WMI and BitLocker queries continue in parallel.

## Integrations & Security
- **Jira Credential Vault**: Stores Personal Access Tokens and API tokens in the Windows Credential Locker (`PasswordVault`).
- **Connection Testing**: In-app connectivity check against Jira REST endpoints (`/myself`) with contextual error reporting.
