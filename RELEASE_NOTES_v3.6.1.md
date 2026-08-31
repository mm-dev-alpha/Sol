# ☀️ Sol v3.6.1 Release Notes

> **Sol 3.6.1** is a maintenance and refinement release introducing English-only architectural standardization, high-reliability ProgressRing loading animations, and hardened UI thread marshalling across all workspaces and diagnostic inspectors.

---

## 🌟 What's New in v3.6.1

### 🇬🇧 English-Only Standardization
- **Streamlined Language Architecture**: Completely removed German language support and translation overhead across the entire application to ensure laser focus on core Active Directory workflows, faster release cycles, and reduced support overhead.
- **Simplified Settings Interface**: Removed the App Language selector card from the Settings view. The application now defaults strictly and cleanly to English (`en-US`).
- **Standardized Terminology**: 100% of all user-facing strings are maintained centrally via `Sol.Helpers.Strings` adhering strictly to official Microsoft Active Directory and Windows Server terminology (*Password*, *Account*, *Security Identifier (SID)*, *Workspace*, *Organizational Unit (OU)*).

---

### ⚡ Loading Animation Reliability & Polish
- **Dynamic Composition Bindings**: Resolved an issue where WinUI 3 `ProgressRing` spinners could intermittently freeze or fail to render when parent cards toggled visibility. All loading indicators now bind `IsActive` dynamically to their corresponding ViewModel loading properties.
- **Thread-Safe UI Dispatching**: Hardened `RunOnUIThread` across all diagnostic and inspection operations, ensuring background tasks and asynchronous continuations reliably trigger UI updates and loading state transitions.
- **Smooth Workspace Loading Overlays**: Added animated loading backdrops with centered ProgressRings to both User Workspace and Computer Workspace for visual feedback during search queries.

---

### 🔒 Stability & Quality Assurance
- **Full Test Suite Parity**: All **150 automated unit tests** passing with 100% success rate on .NET 10.
- **Clean Release Build**: 0 compiler warnings and 0 errors under Release configuration.

---

## 📥 Downloads & Assets

Download the self-contained package below for your 64-bit Windows environment. No external .NET runtime installation required.
