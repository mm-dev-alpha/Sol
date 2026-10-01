# Sol v4.1.2

Maintenance and reliability release for Remote Desktop Services (RDS), introducing two-phase collection discovery, locale-agnostic disk expansion, and explicit disk inspection feedback.

## Highlights
- **Two-Phase Collection Discovery**: Decouples session collection enumeration from UPD share queries, resolving timeout issues on multi-collection broker farms.
- **Locale-Agnostic Diskpart Handling**: Evaluates volume expansion exit codes and localized output strings reliably on non-English Windows installations.
- **Explicit Inspection Feedback**: Displays dedicated loading indicators and progress states during VHDX discovery and volume partition analysis.
- **Automatic Post-Expansion Refresh**: Automatically reloads and displays the updated disk layout immediately following successful profile expansion.

## Remote Desktop Services
- **Collection Query Optimization**: Prevents duplicate parallel queries and uses active user sessions as a fast fallback to resolve the target session collection.
- **Container Sizing Guards**: Correctly identifies when a virtual disk container already meets or exceeds requested capacity without triggering false errors.
- **Visual Feedback**: Added a dedicated disk inspection loading card with animated progress ring and centralized status messaging.
