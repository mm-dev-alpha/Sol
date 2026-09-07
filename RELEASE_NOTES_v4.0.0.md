# ☀️ Sol v4.0.0 Release Notes

> **Sol 4.0.0** is a major release introducing the new Tools page (File Locksmith, Shortcut Guide, MMC Administrative Consoles, Daily Admin Commands, Grab Frame, and Edit Text Window), side-by-side Active Directory Object Comparison, architectural decomposition, and enterprise security fixes.

---

## 🌟 What's New in v4.0.0

### 🧰 Integrated Tools Page
Sol introduces a dedicated **Tools** page with built-in administrative utilities and productivity tools:

- **File Locksmith**:
  - Inspect open file and directory handles to see which processes are locking resources.
  - Terminate locking processes cleanly with single-click actions.
  - Drag-and-drop file target zone, file browsing, and optional Windows Explorer context menu integration.
- **Shortcut Guide (`Win + Shift + ?`)**:
  - On-screen overlay reference displaying keyboard shortcuts for Windows 11/10 navigation, window snapping, virtual desktops, and common operations.
- **MMC Administrative Consoles (`Alt + Space`)**:
  - Fast, searchable launcher for 40+ Microsoft Management Consoles (`.msc`) and control panel applets (`.cpl`), including *Active Directory Users & Computers*, *Group Policy Management*, *Event Viewer*, *Services*, *Disk Management*, and *Device Manager*.
  - Category filtering and persistent favorites pinning.
- **Daily Admin Commands (`Win + Shift + C`)**:
  - Curated catalog of 50+ essential administrative commands (*FlushDNS*, *GPUpdate*, *SFC*, *DISM*, *WinRM*, *Network Diagnostics*, *RSAT Management*).
  - One-click copy or elevated execution across PowerShell, Command Prompt, and Run dialogues with persistent favorites pinning.
- **Grab Frame (`Win + Shift + G`)**:
  - Floating viewfinder window with a native transparent backdrop to capture and extract text or tables from any desktop area.
  - Real-time word detection, draggable table column dividers for tabular data extraction, and auto-OCR debouncing.
- **Edit Text Window (`Win + Shift + E`)**:
  - Dedicated text workbench featuring 40+ string transformation routines (casing, trimming, line sorting, duplicate removal, regex replacements).
  - Structured tabular data editor (TSV, CSV, Markdown tables, XML, column/row transposition).
  - Live line-by-line math expression evaluator with running totals and averages.
  - Direct text insertion into target applications via `SendInput`.

---

### ⚖️ Active Directory Compare Workspace
- Side-by-side comparative inspection of Active Directory Users and Computers.
- Visual diff highlighting across group memberships, directory attributes, organizational units, and hardware specifications.

---

### 🔒 Enterprise Security Improvements

- **Input & Command Hardening**:
  - RFC 4515 LDAP search filter escaping across all directory queries to prevent LDAP injection.
  - External process execution (`sc.exe`, `taskkill.exe`, `logoff.exe`, PowerShell) parameterized via `ProcessStartInfo.ArgumentList`.
- **Credential & Secret Protection**:
  - Jira Cloud tokens and Jira Data Center PATs stored exclusively in the Windows Credential Locker (DPAPI / `PasswordVault`) with secure deletion on clear.
  - Active Directory password resets use char-buffer zeroing to prevent plaintext credentials from lingering in memory.
- **Safe Clipboard Operations**:
  - Passwords and BitLocker recovery keys use Windows `SetContentWithOptions` flags to exclude sensitive strings from clipboard history and cloud synchronization.
- **Critical Process Shield**:
  - Safeguards prevent accidental termination of critical Windows OS processes (PID 0–4 and core system binaries).

---

### ⚡ Architecture & Performance Polish

- **Decomposed Diagnostics**:
  - Refactored diagnostics into dedicated single-responsibility services (`HardwareDiagnosticService`, `BitLockerManagementService`, `ProcessManagementService`, `DiagnosticScopeHelper`).
- **UI Virtualization & Responsiveness**:
  - Enforced `MaxHeight` virtualization constraints on list views to ensure smooth scrolling and avoid UI freezes on large Active Directory organizational units.
  - Dynamic `ProgressRing` binding ensures reliable spinner animations during background queries.

---

## 📋 System Requirements
- **Windows 11** or **Windows 10** (Version 1809+, Build 17763+)
- **Windows Server 2025 / 2022 / 2019**
- Domain-joined machine or RSAT installed for Active Directory operations
