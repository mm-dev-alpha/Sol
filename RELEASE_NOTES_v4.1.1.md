# Sol v4.1.1

Maintenance release for the Remote Desktop Services (RDS) workspace, improving Connection Broker hostname resolution, session table sorting, and User Profile Disk maintenance.

## Highlights
- **Broker FQDN Resolution**: Automatically resolves short NetBIOS broker hostnames to fully qualified domain names via DNS.
- **Interactive Session Sorting**: Clickable table column headers with directional sorting across Username, State, Host Server, Session ID, and Logon Time.
- **Session State Badges**: Added color-coded status badges and metric pills for Active, Connected, and Disconnected sessions.
- **PowerShell Error Sanitization**: Decodes escaped hex entities and removes `#< CLIXML` wrappers from background PowerShell error streams.

## Remote Desktop Services
- **Broker Connection**: Short NetBIOS broker names entered in Settings are automatically resolved, displayed, and saved as verified FQDNs.
- **Session Table Alignment**: Harmonized column definitions (`220`, `150`, `*`, `100`, `160`, `90`) and list item styling to eliminate horizontal header-row drift.
- **Disconnect Action**: Replaced the layers icon with the standard Windows disconnect glyph (`PlugDisconnected`).
- **User Profile Disks**: Separated User Account and Session Collection into dedicated cards, added immediate search execution on Enter, and automated collection reloading.
