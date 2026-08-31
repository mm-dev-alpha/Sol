# ☀️ Sol v3.6.0 Release Notes

> **Sol 3.6.0** is a major enterprise release introducing remote Windows Services management, progressive diagnostic streaming, real-time JIRA integration, design system uniformity, and comprehensive performance and safety hardening.

---

## 🌟 What's New in v3.6.0

### 🛠️ Remote Windows Services Inspector & Controller
- **Live Remote Service Management**: Query, inspect, and manage Windows services across domain endpoints via WMI and fallback RPC tooling in a dedicated, high-performance inspector window.
- **Full Lifecycle Operations**: Start, stop, and restart services with a single click; update startup types (**Automatic**, **Manual**, **Disabled**) on the fly.
- **Critical Service Shield**: 24 core Windows OS and security services (such as RPC, EventLog, WinDefend, LSASS) are automatically protected against accidental stoppage or restart, with a clear security shield indicator.
- **Fast Interactive Filtering & Sorting**: Instant multi-column sorting by Display Name, Service Name, Status, Startup Type, and Log On As account.

---

### ⚡ Progressive Remote Diagnostics & Live Loading Indicators
- **Instantaneous Card Streaming**: Remote computer diagnostics (Hardware, Uptime, Drives, Sessions, Battery, BitLocker) now stream progressively in real time. Fast queries (e.g. Hardware) render in under a second without waiting for long-running BitLocker or WMI queries.
- **Active Loading Indicators**: Every diagnostic card clearly shows an active loading state during remote endpoint communication and timeout periods.
- **Live Hero Status Pill**: Subtle header badge indicates when background diagnostic queries are active.
- **Task & Services Manager Loading Overlays**: Smooth progress overlay appears immediately while remote process and service lists are retrieved.

---

### 🔗 Real-Time JIRA Integration (Cloud & Data Center)
- **Zero-Restart Enablement**: Enabling or disabling JIRA integration now applies instantly across the entire application without requiring an app restart.
- **Isolated Per-Mode Secret Vault**: Personal Access Tokens (Data Center) and API Tokens (Cloud) are isolated and encrypted via the Windows Credential Locker (DPAPI).
- **Authentic Connection Testing**: Directly verifies endpoint reachability and credentials against the Jira REST API (`/rest/api/2/myself` and `/rest/api/3/myself`) with clear contextual feedback.

---

### 🎨 WinUI 3 UX Polish & Localization Enhancements
- **German UI Alignment**: Widened the Services status column to prevent text clipping on German status badges (*"Wird ausgeführt"*).
- **ComboBox Realization Fixes**: Eliminated empty gray selection headers in Settings and Inspector windows by migrating to pure `ItemsSource` data bindings.
- **Dual-Language Parity**: 100% strict localization across English and German with official Active Directory and Windows Server terminology.
- **Refreshed Startup Greetings**: Friendly, localized welcome greetings with dynamic emoji pairings.

---

### 📋 Complete Workspace Data Export
- **Comprehensive 'Copy All'**: User and Computer workspace export actions now capture 100% of all loaded Active Directory attributes, security metadata, hardware specs, BitLocker keys, disk partitions, and battery health in a clean, human-readable format.

---

### 🔒 Enterprise Safety & Pre-Release Hardening
- Rigorous security audit remediations protecting against ContentDialog crashes, COM memory leaks, unobserved async exceptions, and LDAP injection vectors.
- Automated CI/CD build matrix and **150 automated unit tests** passing with 0 warnings and 0 errors on .NET 10 & Windows App SDK.

---

## 📥 Downloads & Assets

Download the self-contained package below for your 64-bit Windows environment. No external .NET runtime installation required.
