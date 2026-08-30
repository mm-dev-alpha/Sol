<div align="center">

<img src="assets/sol-logo.png" alt="Sol Logo" width="128">

# Sol ☀️

**A Modern Active Directory & Systems Management Suite for Windows**

[![GitHub Release](https://img.shields.io/github/v/release/mm-dev-alpha/Sol?color=0078D4&logo=github)](https://github.com/mm-dev-alpha/Sol/releases)
[![CI Build Status](https://img.shields.io/github/actions/workflow/status/mm-dev-alpha/Sol/ci.yml?branch=main&logo=github)](https://github.com/mm-dev-alpha/Sol/actions/workflows/ci.yml)
[![GitHub Downloads](https://img.shields.io/github/downloads/mm-dev-alpha/Sol/total?color=2ea44f)](https://github.com/mm-dev-alpha/Sol/releases)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Windows App SDK](https://img.shields.io/badge/Windows%20App%20SDK-1.7%20%7C%20WinUI%203-0078D4?logo=windows&logoColor=white)](https://learn.microsoft.com/windows/apps/windows-app-sdk/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows%2011%20%2F%20Server-0078D6?logo=windows11&logoColor=white)](https://www.microsoft.com/windows)
[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20A%20Coffee-Support-yellow.svg?logo=buy-me-a-coffee&logoColor=black)](https://www.buymeacoffee.com/mmdevalpha)

*Fast, elegant, and secure IT administration with native Fluent 2 design.*

</div>

---

## 🌟 Overview

**Sol** is a high-performance native Windows desktop application built for IT systems administrators, helpdesk engineers, and support teams. Built with **.NET 10** and **WinUI 3 (Windows App SDK 1.7)**, Sol combines directory services administration, remote system diagnostics, BitLocker key recovery, process management, and service control into a distraction-free, fluid interface with native Mica backdrop material.

---

## 🎬 Showcase

<!-- HERO DEMO PLACEHOLDER: Replace with assets/demo.gif or video embed -->
<div align="center">
  <img src="assets/demo-placeholder.png" alt="Sol Hero Showcase" width="90%">
  <p><em>Instant Active Directory search, remote diagnostics, and BitLocker recovery key discovery.</em></p>
</div>

---

## ✨ Features

### 🔍 Central Landing Hub
- **Unified Fast Search**: Search for Active Directory **Users** and **Computers** with instant autocomplete and navigation.
- **Fluent 2 Design**: Native Mica backdrop with automatic Light and Dark theme adaptation.

### 👤 Active Directory User Workspace
- **Complete Profile & Identity**: Display Name, SamAccountName, UPN, Employee ID, OU Path, and Security Identifier (**SID**) with one-click copy buttons.
- **Organization & Reporting**: Manager navigation, direct reports tree, and security group memberships.
- **Contact Information**: Phone numbers, office location, street address, and email.
- **Safe In-Place Editing**: Update user attributes directly in Active Directory.
- **Account & Security Controls**:
  - Reset passwords with auto-generated secure 16-character complex passwords.
  - Unlock locked accounts, enable/disable accounts, and set password expiry flags.
- **Raw Attribute Inspector**: Inspect all Active Directory attributes in a raw key-value view.

### 💻 Computer Workspace & Remote Diagnostics
- **Directory & Network Identity**: DNS Hostname, SAM Account Name, IPv4 Address, OU Path, Operating System version, and Owner (`ManagedBy`).
- **Hardware & BIOS Diagnostics**: Manufacturer, Model, Serial / Service Tag, BIOS version & release date, CPU, RAM, and one-click manufacturer warranty lookup link.
- **System Uptime & Reboot Status**: Precise uptime duration, last boot timestamp, and pending reboot detection (Component-Based Servicing, Windows Update, PendingFileRenameOperations).
- **Storage & Disk Health**: Logical drive partitions, capacity bars, free/total space, file system (NTFS/ReFS), and drive health status (SSD/NVMe).
- **Battery & Power Diagnostics**: Battery health percentage, wear level, full charge vs. design capacity, cycle count, estimated runtime, and charging status for mobile endpoints.
- **Active Logon Sessions**: Inspect active and disconnected console and RDP sessions with logon duration.
- **BitLocker Drive Encryption**: System drive encryption state (XTS-AES 128/256-Bit), protection status, and instant discovery of Active Directory BitLocker recovery passwords (`msFVE-RecoveryInformation`).
- **Quick Actions**: One-click Ping test, Remote Desktop (RDP), and remote PowerShell console launching.

### ⚡ Remote Process Manager
- Standalone inspection window displaying live remote processes with PID, Name, User, CPU%, Memory (MB), and Network state.
- Instant search and multi-column sorting (PID, Name, User, CPU, Memory).
- Safe process termination with built-in protection guarding critical Windows OS processes (PID 0–4 and system binaries).

### ⚙️ Remote Windows Services Inspector & Controller
- Inspect all installed Windows services with Display Name, Service Name, Status (Running / Stopped / Pending), Startup Type (Auto / Manual / Disabled), and Service Account (`StartName`).
- Filter by status (**All**, **Running**, **Stopped**) and live search.
- **Remote Service Control**: Start, stop, and restart services with confirmation dialogs.
- **Startup Type Configuration**: Change startup modes directly from a dropdown.
- **Built-in Safety**: 24 critical Windows OS services (RPC, LSASS, DHCP, EventLog, etc.) are protected against accidental stoppage.
- **Action Diagnostics**: Informative error translation for WMI return codes and local Administrator elevation guidance.

### 🎫 JIRA Integration
- Support for **Jira Data Center** (Personal Access Tokens) and **Jira Cloud** (Email + API Token).
- Secure token storage using **Windows Credential Locker (PasswordVault / DPAPI)** — zero plaintext secrets on disk.
- Query and view open tickets created by or associated with the active user directly in the workspace.

### 📋 100% Comprehensive "Copy All" Export
- One-click copy on both User and Computer workspaces exports 100% of all loaded Active Directory properties, metadata, and diagnostic modules into clean, human-readable sections with aligned key-value pairs.

### 🌍 Dual-Language Localization
- Full bilingual support (**English** and **German**) with zero hardcoded strings.

---

## 🛠️ Architecture & Tech Stack

| Component | Technology |
|---|---|
| **Runtime & Language** | [.NET 10 (C# 14)](https://dotnet.microsoft.com/) |
| **UI Framework** | [WinUI 3 / Windows App SDK 1.7](https://learn.microsoft.com/windows/apps/winui/winui3/) |
| **Architecture** | MVVM via [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) |
| **Controls & Styling** | [CommunityToolkit.WinUI](https://learn.microsoft.com/windows/communitytoolkit/) (SettingsCard, Segmented, Mica) |
| **Directory Services** | LDAP via `System.DirectoryServices.AccountManagement` & `System.DirectoryServices` |
| **Remote Diagnostics** | WMI / CIM (`System.Management`) with CLI fallback |
| **Credential Security** | Windows Credential Locker (`Windows.Security.Credentials.PasswordVault`) |
| **Deployment Model** | Self-contained, single-file unpackaged binary (no MSIX or Store required) |

---

## 🚀 Getting Started

### System Requirements
- **Windows 11** (recommended) or **Windows 10** (Version 1809+, Build 17763+)
- **Windows Server 2025 / 2022 / 2019**
- Domain-joined machine or RSAT installed for Active Directory operations

### Installation

Download the latest standalone release from the [**Releases**](https://github.com/mm-dev-alpha/Sol/releases) page:

1. Download `Sol-v1.0.0-win-x64.zip`.
2. Extract the archive to any folder.
3. Run `Sol.exe`.

> *No installation, admin rights, or .NET runtime installation required (self-contained).*

---

### Building from Source

```bash
# Clone the repository
git clone https://github.com/mm-dev-alpha/Sol.git
cd Sol

# Restore dependencies
dotnet restore src/Sol/Sol.csproj

# Run all unit tests
dotnet test src/Sol.Tests/Sol.Tests.csproj

# Run Sol in Debug mode
dotnet run --project src/Sol/Sol.csproj
```

### Creating a Self-Contained Release Build

```powershell
dotnet publish src/Sol/Sol.csproj -c Release -r win-x64 --self-contained -o ./publish
```

---

## 🔒 Security & Privacy

- **Zero Telemetry**: Sol collects, stores, and transmits zero telemetry, usage statistics, or analytics.
- **Audit-Proof Credential Storage**: All JIRA API tokens and PATs are encrypted via Windows Credential Locker (DPAPI).
- **RFC 4515 LDAP Escaping**: All directory search queries are sanitized to prevent LDAP filter injection.
- **Structured Shell Execution**: External command invocations (`sc.exe`, `taskkill.exe`, `logoff.exe`) use `ProcessStartInfo.ArgumentList` to eliminate command-line argument injection risks.
- **Destructive Action Confirmation**: Password resets, account disables, process terminations, and service stoppages require explicit confirmation.
- **Local Audit Logging**: Attribute modifications are logged locally to `%LocalAppData%\Sol\Logs\ad_audit.log` with automatic log rolling.

---

## 💬 Community & Discussions

Have questions, feature ideas, or feedback?
- Join the conversation on [**GitHub Discussions**](https://github.com/mm-dev-alpha/Sol/discussions).
- Report bugs via [**GitHub Issues**](https://github.com/mm-dev-alpha/Sol/issues).

---

## ☕ Support the Project

If Sol simplifies your daily IT workflow, consider supporting its development:

[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20A%20Coffee-Support-yellow.svg?style=for-the-badge&logo=buy-me-a-coffee&logoColor=black)](https://www.buymeacoffee.com/mmdevalpha)

---

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

---

<div align="center">
  <sub>Built with ❤️ by <a href="https://github.com/mm-dev-alpha">@mm-dev-alpha</a> for IT Professionals</sub>
</div>
