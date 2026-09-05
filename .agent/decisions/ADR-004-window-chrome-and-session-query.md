# ADR-004: Window Chrome Modification and Computer Session Enumeration Strategy
- Date: 2026-09-05
- Status: Accepted
- Context:
  1. `EditTextWindow` custom toolbar controls on the top right are obscured by system caption buttons (minimize, maximize, close) rendered by WinUI 3 `AppWindow.TitleBar`.
  2. The Computer Workspace logged-on user diagnostic regularly reports no users due to an inverted WMI `Win32_LoggedOnUser` parsing order (Antecedent = Win32_Account, Dependent = Win32_LogonSession) and lack of native fallback for local sessions.
- Decision:
  1. For `EditTextWindow`, invoke `_presenter.SetBorderAndTitleBar(hasBorder: true, hasTitleBar: false)`. Handle window dragging via `ReleaseCapture()` and `SendMessage(hwnd, WM_NCLBUTTONDOWN, HTCAPTION, 0)` on the top bar grid, and render a custom styled close button directly in XAML.
  2. For `ComputerDiagnosticService`, correct the WMI regex parsing to accept either Antecedent/Dependent ordering, add `Win32_ComputerSystem.UserName` console user query, and add native `wtsapi32.dll` (`WTSEnumerateSessionsW` / `WTSQuerySessionInformationW`) interop for 100% reliable local session detection when WMI is restricted or slow.
- Trade-offs:
  - Disabling the system title bar requires custom client-area drag message pumping (`HTCAPTION`).
  - Native `WTSEnumerateSessionsW` operates only on local machine (or requires explicit RPC server handles for remote), while WMI handles remote CIM connections. Using WTS as a local fallback and WMI for remote gives the best reliability.
