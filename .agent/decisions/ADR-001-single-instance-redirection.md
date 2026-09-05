# ADR-001: Single-Instancing and Command Redirection via AppInstance
- Date: 2026-09-04
- Status: Accepted
- Context: When invoking File Locksmith from the Windows Explorer context menu (`Sol.exe --unlock "%1"`), Windows launches a new process. This caused a second instance of Sol to launch, showing duplicate main windows instead of forwarding the command to the running instance.
- Decision: Utilize Microsoft Windows App SDK `Microsoft.Windows.AppLifecycle.AppInstance` with custom `Program.Main` (`DISABLE_XAML_GENERATED_MAIN`) to detect existing instances, grant foreground rights via `AllowSetForegroundWindow`, and redirect activation arguments asynchronously via `RedirectActivationToAsync`.
- Trade-offs: Takes control of startup bootstrapping before XAML runtime initialization. Async redirection from STA `Main` must be executed safely via non-blocking synchronization to avoid STA thread deadlocks.
