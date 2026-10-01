# Sol v3.6.1

Maintenance release standardizing on English (en-US) localization, stabilizing WinUI 3 progress indicator animations, and hardening UI thread dispatching.

## Highlights
- **Localization Consolidation**: Standardized the application on English (`en-US`), simplifying the settings interface.
- **Loading Indicators**: Resolved an issue where WinUI 3 `ProgressRing` spinners could freeze when parent cards toggled visibility.
- **UI Thread Safety**: Hardened background task dispatching across remote diagnostic queries to ensure reliable state updates.

## User Interface & Diagnostics
- **Progress Indicators**: Bound `ProgressRing.IsActive` dynamically to ViewModel loading properties across User and Computer workspaces.
- **Async Dispatching**: Consolidated UI marshalling through `RunOnUIThread` for all WMI, WTS, and Active Directory query continuations.
