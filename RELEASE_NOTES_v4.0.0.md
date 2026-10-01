# Sol v4.0.0

Major release introducing the Tools Hub workspace (File Locksmith, Shortcut Guide, MMC Consoles, Admin Commands, Grab Frame, and Edit Text), Active Directory object comparison, and security hardening across remote operations.

## Highlights
- **Tools Hub Workspace**: Added six dedicated administration and productivity tools accessible directly or via system-wide hotkeys.
- **Active Directory Compare Workspace**: Side-by-side comparison of domain users and computers with visual attribute and group diffs.
- **Security Hardening**: RFC 4515 LDAP search filter escaping, structured argument execution, and Windows Credential Locker secret storage.
- **Diagnostic Architecture**: Refactored monolithic diagnostic services into modular providers.

## Tools Hub
- **File Locksmith**: Identifies processes locking files or directories with single-click process termination and drag-and-drop support.
- **Shortcut Guide (`Win + Shift + ?`)**: On-screen overlay reference for Windows navigation, snap layouts, and system shortcuts.
- **MMC Administrative Consoles (`Alt + Space`)**: Searchable launcher for 40+ Microsoft Management Consoles and control panel applets with favorites pinning.
- **Daily Admin Commands (`Win + Shift + C`)**: Launcher for 50 common PowerShell, Command Prompt, and Run administrative commands.
- **Grab Frame (`Win + Shift + G`)**: Transparent viewfinder window with live OCR word detection and draggable column dividers for extracting tabular screen content.
- **Edit Text Window (`Win + Shift + E`)**: Text workbench with 40 string transformations, structured table formatting (TSV, CSV, Markdown, XML), and running line math evaluation.

## Active Directory & Diagnostics
- **Object Comparison**: Compare two users or two computers with color-coded diff highlighting across attributes, group memberships, and hardware specs.
- **Security Controls**: Parameterized process executions (`ProcessStartInfo.ArgumentList`) to eliminate command injection risks, and char-buffer zeroing on password resets.
- **Safe Clipboard**: Passwords and BitLocker keys are copied with cloud sync and clipboard history exclusion flags.
