# Sol v4.1.0

Introduces the Remote Desktop Services (RDS) Workspace for farm-wide session management and stepped User Profile Disk (`.vhdx`) expansion.

## Highlights
- **Remote Desktop Services Workspace**: New navigation workspace to monitor and manage user sessions across RDS Connection Brokers.
- **User Profile Disk Resizing**: Automated stepped expansion of virtual profile disks (`.vhdx`) and underlying NTFS volume partitions.
- **Administrator Elevation Relaunch**: In-app banner detecting unelevated status with a one-click prompt to relaunch Sol with administrative privileges.

## Remote Desktop Services
- **Connection Broker Integration**: Executes RemoteDesktop PowerShell cmdlets in an out-of-process 64-bit PowerShell host to avoid COM/CLR threading conflicts.
- **Session Management**: Lists active and disconnected user sessions with support for filtering, single or batch session disconnects, and session logoff.
- **User Profile Disk Maintenance**:
  - Active Directory user search with automatic discovery of user profile disks across network shares.
  - Active session conflict detection to verify locks are released before resizing.
  - Dynamic drive letter mapping to allow `diskpart.exe` execution against UNC profile shares.
  - Native partition expansion using Windows Storage cmdlets with fallback dismount handling and `.resize.lock` coordination.

## General
- Added elevation check utility (`IsProcessElevated`) and command-line preserving relaunch via the Win32 `runas` verb.
