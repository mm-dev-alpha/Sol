using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sol.Helpers;

public class Strings
{
    public static Strings S { get; } = new();

    public static string CurrentLanguage { get; set; } = "en";
    private static string Lang => CurrentLanguage;
    public static bool IsDe => Lang == "de";

    private static readonly Dictionary<string, PropertyInfo> _propertyCache =
        typeof(Strings).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

    public static string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        if (_propertyCache.TryGetValue(key, out var prop))
        {
            return prop.GetValue(S) as string ?? string.Empty;
        }
        return $"[{key}]";
    }

    // General & Common
    public string Yes => IsDe ? "Ja" : "Yes";
    public string No => IsDe ? "Nein" : "No";
    public string Never => IsDe ? "Nie" : "Never";
    public string SaveBtn => IsDe ? "Speichern" : "Save";
    public string HomeBtn => IsDe ? "Startseite" : "Home";
    public string LoadingUserData => IsDe ? "Daten werden geladen..." : "Loading data...";
    public string CopiedToClipboard => IsDe ? "In die Zwischenablage kopiert." : "Copied to clipboard.";

    // MainWindow
    public string NavHome => IsDe ? "Startseite" : "Home";
    public string NavUserWorkspace => IsDe ? "Benutzer-Arbeitsbereich" : "User Workspace";
    public string NavComputerWorkspace => IsDe ? "Computer-Arbeitsbereich" : "Computer Workspace";
    public string NavSettings => IsDe ? "Einstellungen" : "Settings";
    public string RunningAs => IsDe ? "Ausgeführt als: " : "Running as: ";

    // User & Computer Search
    public string SearchUserPlaceholder => IsDe ? "Nach einem Benutzer suchen..." : "Search for a user...";
    public string SearchComputerPlaceholder => IsDe ? "Nach einem Computer suchen..." : "Search for a computer...";
    public string MultipleUsersFound => IsDe ? "Mehrere Benutzer gefunden" : "Multiple Users Found";
    public string SelectUserPrompt => IsDe ? "Bitte wählen Sie den gewünschten Benutzer aus:" : "Please select the correct user:";
    public static string NoUsersFound(string query) => IsDe ? $"Keine Benutzer gefunden, die '{query}' entsprechen." : $"No users found matching '{query}'.";
    public static string ErrorLoadingUser(string msg) => IsDe ? $"Fehler beim Laden des Benutzers: {msg}" : $"Error loading user: {msg}";

    // UserWorkspacePage - Hero & Actions
    public string UserDetailsTitle => IsDe ? "Benutzerdetails" : "User Details";
    public string FirstNameLabel => IsDe ? "Vorname" : "First Name";
    public string LastNameLabel => IsDe ? "Nachname" : "Last Name";
    public string DisplayNameLabel => IsDe ? "Anzeigename" : "Display Name";
    public string EmailLabel => IsDe ? "E-Mail" : "Email";
    public string DepartmentLabel => IsDe ? "Abteilung" : "Department";
    public string TitleLabel => IsDe ? "Position" : "Title";
    public string ManagerLabel => IsDe ? "Vorgesetzter" : "Manager";
    public string AccountStatusLabel => IsDe ? "Kontostatus" : "Account Status";
    public string SidLabel => IsDe ? "Sicherheitskennung (SID)" : "Security Identifier (SID)";
    public string AddressLabel => IsDe ? "Adresse" : "Address";
    public string WebsiteLabel => IsDe ? "Webseite" : "Website";
    public string EditBtn => IsDe ? "Bearbeiten" : "Edit";
    public string CancelBtn => IsDe ? "Abbrechen" : "Cancel";
    public string CopyBtn => IsDe ? "Kopieren" : "Copy";
    public string CopyAllBtn => IsDe ? "Alle kopieren" : "Copy All";
    public string CloseWorkspaceBtn => IsDe ? "Arbeitsbereich schließen" : "Close Workspace";
    
    public string LockedOut => IsDe ? "Gesperrt" : "Locked Out";
    public string Disabled => IsDe ? "Deaktiviert" : "Disabled";
    public string Active => IsDe ? "Aktiviert" : "Active";
    public string EnableAccountBtn => IsDe ? "Konto aktivieren" : "Enable Account";
    public string DisableAccountBtn => IsDe ? "Konto deaktivieren" : "Disable Account";
    public string EnableComputerBtn => IsDe ? "Computer aktivieren" : "Enable Computer";
    public string DisableComputerBtn => IsDe ? "Computer deaktivieren" : "Disable Computer";
    public string UnlockAccountBtn => IsDe ? "Konto entsperren" : "Unlock Account";
    public string PasswordActionsBtn => IsDe ? "Kennwortaktionen" : "Password Actions";
    public string ResetPasswordBtn => IsDe ? "Kennwort zurücksetzen" : "Reset Password";
    public string ForcePasswordChangeBtn => IsDe ? "Kennwortänderung bei nächster Anmeldung" : "User must change password at next logon";
    public string SetNewPasswordTitle => IsDe ? "Neues Kennwort festlegen" : "Set New Password";
    public string NewPasswordLabel => IsDe ? "Neues Kennwort" : "New Password";
    public string NewPasswordPlaceholder => IsDe ? "Neues Kennwort eingeben oder generieren" : "Enter new password or generate";
    public string GeneratePasswordBtn => IsDe ? "Kennwort generieren" : "Generate Password";
    public string MustChangePasswordCheckbox => IsDe ? "Kennwort bei nächster Anmeldung ändern" : "User must change password at next logon";
    public string UnlockAccountCheckbox => IsDe ? "Konto entsperren, falls gesperrt" : "Unlock account if locked out";
    public string PasswordResetAuditNotice => IsDe ? "Das neue Kennwort wird bei Bestätigung in die Zwischenablage kopiert. Diese Aktion wird protokolliert." : "The new password will be copied to your clipboard upon confirmation. This action is logged.";
    public static string ResetPasswordDialogTitle(string name) => IsDe ? $"Kennwort für {name} zurücksetzen" : $"Reset Password for {name}";

    // UserWorkspacePage - Contact Information Section
    public string ContactInfoSection => IsDe ? "Kontaktinformationen" : "Contact Information";
    public string OfficeLabel => IsDe ? "Büro" : "Office";
    public string OfficePhoneLabel => IsDe ? "Rufnummer geschäftlich" : "Office Phone";
    public string MobilePhoneLabel => IsDe ? "Mobiltelefon" : "Mobile Phone";
    public string ViewManagerBtn => IsDe ? "Vorgesetzten anzeigen" : "View Manager";
    public string DirectReportsLabel => IsDe ? "Direkte Mitarbeiter" : "Direct Reports";
    public string ViewProfileBtn => IsDe ? "Profil anzeigen" : "View Profile";
    public string SearchManagerPlaceholder => IsDe ? "Vorgesetzten suchen..." : "Search manager...";

    // UserWorkspacePage - Security & Logon Section
    public string SecurityLogonSection => IsDe ? "Sicherheit & Anmeldung" : "Security & Logon";
    public string PasswordLastSetLabel => IsDe ? "Kennwort zuletzt festgelegt" : "Password Last Set";
    public string PasswordExpiryLabel => IsDe ? "Kennwortablauf" : "Password Expiry";
    public string MustChangePasswordLabel => IsDe ? "Kennwort bei nächster Anmeldung ändern" : "Must change password at next logon";
    public string BadPasswordCountLabel => IsDe ? "Fehlerhafte Kennworteingaben" : "Bad Password Count";
    public string LastLogonLabel => IsDe ? "Letzte Anmeldung" : "Last Logon";
    public string PasswordNeverExpires => IsDe ? "Läuft nie ab" : "Never expires";
    public string PasswordExpired => IsDe ? "Abgelaufen" : "Expired";
    public string PasswordExpiresToday => IsDe ? "Läuft heute ab" : "Expires today";
    public string PasswordExpiresTomorrow => IsDe ? "Läuft morgen ab" : "Expires tomorrow";
    public static string PasswordExpiresInDays(int days) => IsDe ? $"Läuft in {days} Tagen ab" : $"Expires in {days} days";
    public string PasswordStatusUnknown => IsDe ? "Unbekannt" : "Unknown";

    // Group Memberships Component (Standardized across Workspaces)
    public string GroupsTitle => IsDe ? "Gruppenmitgliedschaften" : "Group Memberships";
    public string AddGroupTitle => IsDe ? "Zu Gruppe hinzufügen" : "Add to Group";
    public string AddGroupBtn => IsDe ? "Hinzufügen" : "Add";
    public string AddBtn => IsDe ? "Hinzufügen" : "Add";
    public string RemoveBtn => IsDe ? "Entfernen" : "Remove";
    public string AddGroupPlaceholder => IsDe ? "Zur Gruppe hinzufügen..." : "Add to group...";
    public string FilterGroupsPlaceholder => IsDe ? "Gruppen filtern..." : "Filter groups...";
    public string NoGroupsFound => IsDe ? "Keine Gruppen gefunden." : "No groups found.";
    public string NoGroupsMatchFilter => IsDe ? "Keine Gruppen entsprechen dem Filter." : "No groups match the filter.";
    public string AddGroupTooltip => IsDe ? "Gruppe hinzufügen" : "Add to group";
    public string RemoveGroupTooltip => IsDe ? "Aus Gruppe entfernen" : "Remove from group";
    public string SearchGroupToAddPlaceholder => IsDe ? "Gruppenname suchen oder eingeben..." : "Search or enter group name...";

    // Notifications (Toasts / InfoBar)
    public string AccountUnlockedSuccess => IsDe ? "Konto erfolgreich entsperrt." : "Account unlocked successfully.";
    public string AccountEnabledSuccess => IsDe ? "Konto aktiviert." : "Account enabled.";
    public string AccountDisabledSuccess => IsDe ? "Konto deaktiviert." : "Account disabled.";
    public string ComputerEnabledSuccess => IsDe ? "Computer aktiviert." : "Computer enabled.";
    public string ComputerDisabledSuccess => IsDe ? "Computer deaktiviert." : "Computer disabled.";
    public string PasswordResetSuccess => IsDe ? "Kennwort erfolgreich zurückgesetzt und in die Zwischenablage kopiert." : "Password reset successfully. Copied to clipboard.";
    public string ForcePasswordChangeSuccess => IsDe ? "Benutzer muss das Kennwort bei der nächsten Anmeldung ändern." : "User forced to change password at next logon.";
    public string ProfileUpdatedSuccess => IsDe ? "Profil erfolgreich aktualisiert." : "Profile updated successfully.";
    public static string AddedToGroupSuccess(string group) => IsDe ? $"Zu '{group}' hinzugefügt." : $"Added to {group}.";
    public static string RemovedFromGroupSuccess(string group) => IsDe ? $"Aus '{group}' entfernt." : $"Removed from {group}.";
    public static string SaveProfileFailed(string error) => IsDe ? $"Speichern fehlgeschlagen: {error}" : $"Save failed: {error}";

    // TitleBar & Shell
    public string AppTitle => "Sol";
    public string TitleBarSearchPlaceholder => IsDe ? "Nach einem Benutzer suchen..." : "Search for a user...";
    public string SearchUserPlaceholderWithShortcut => IsDe ? "Nach einem Benutzer suchen..." : "Search for a user...";
    public string ExportBtn => IsDe ? "Exportieren" : "Export";
    public string FilterPlaceholder => IsDe ? "Filtern..." : "Filter...";
    public string RefreshBtn => IsDe ? "Aktualisieren" : "Refresh";
    public string CloseBtn => IsDe ? "Schließen" : "Close";
    public string ConfirmBtn => IsDe ? "Bestätigen" : "Confirm";
    public string DeleteBtn => IsDe ? "Löschen" : "Delete";

    // User Workspace Sections
    public string AccountIdentitySection => IsDe ? "Konto & Identität" : "Account & Identity";
    public string OrganizationHierarchySection => IsDe ? "Organisation & Hierarchie" : "Organization & Hierarchy";
    public string LogonNameLabel => IsDe ? "Anmeldename" : "Logon Name";
    public string UpnLabel => IsDe ? "Benutzerprinzipalname (UPN)" : "User Principal Name (UPN)";
    public string EmployeeIdLabel => IsDe ? "Mitarbeiter-ID" : "Employee ID";
    public string OuPathLabel => IsDe ? "Organisationseinheit (OU)" : "OU Path";
    public string AccountExpiresLabel => IsDe ? "Kontoablauf" : "Account Expires";
    public string CopyPowerShellBtn => IsDe ? "PowerShell-Befehl kopieren" : "Copy PowerShell Command";
    public string PowerShellCommandCopied => IsDe ? "PowerShell-Befehl in die Zwischenablage kopiert." : "PowerShell command copied to clipboard.";
    public string PowerShellCopiedSuccess => IsDe ? "PowerShell-Befehl in die Zwischenablage kopiert." : "PowerShell command copied to clipboard.";
    public string AllInfoCopiedSuccess => IsDe ? "Alle Profilinformationen in die Zwischenablage kopiert." : "All profile information copied to clipboard.";
    public string ExportProfileBtn => IsDe ? "Profil exportieren" : "Export Profile";

    // Computer Workspace
    public string NoComputerFound => IsDe ? "Kein passender Computer im Active Directory gefunden." : "No matching computer found in Active Directory.";
    public string MultipleComputersFound => IsDe ? "Mehrere Computer gefunden" : "Multiple Computers Found";
    public string SelectComputerPrompt => IsDe ? "Bitte wählen Sie das gewünschte Computerkonto aus:" : "Please select the computer account:";
    public string BitLockerKeysTitle => IsDe ? "BitLocker-Wiederherstellungsschlüssel" : "BitLocker Recovery Keys";
    public string BitLockerKeyCopiedSuccess => IsDe ? "Wiederherstellungsschlüssel in die Zwischenablage kopiert." : "Recovery key copied to clipboard.";
    public string NoBitLockerKeysForComputer => IsDe ? "Keine BitLocker-Wiederherstellungsschlüssel für dieses Computerkonto hinterlegt." : "No BitLocker recovery keys stored for this computer account.";
    public string SystemIdentityTitle => IsDe ? "System & Netzwerkidentität" : "System & Network Identity";
    public string ComputerNameLabel => IsDe ? "Computername" : "Computer Name";
    public string DnsHostNameLabel => IsDe ? "DNS-Hostname" : "DNS Hostname";
    public string OperatingSystemLabel => IsDe ? "Betriebssystem" : "Operating System";
    public string OsVersionLabel => IsDe ? "Betriebssystemversion" : "OS Version";
    public string DescriptionLabel => IsDe ? "Beschreibung" : "Description";
    public string ManagedByLabel => IsDe ? "Verwaltet von" : "Managed By";
    public string LocationLabel => IsDe ? "Standort" : "Location";
    public string AccountSecurityTitle => IsDe ? "Kontostatus & Sicherheit" : "Account Status & Security";
    public string ObjectCreatedLabel => IsDe ? "Objekt erstellt am" : "Object Created";
    public string ObjectModifiedLabel => IsDe ? "Objekt geändert am" : "Object Modified";
    public string QuickDiagnosticTitle => IsDe ? "Diagnose & Fernwartung" : "Diagnostics & Remote Tools";
    public string PingBtn => IsDe ? "Erreichbarkeit prüfen (Ping)" : "Test Connection (Ping)";
    public string RemotePsBtn => IsDe ? "Remote PowerShell starten" : "Launch Remote PowerShell";
    public string RdpBtn => IsDe ? "Remotedesktop (RDP)" : "Remote Desktop (RDP)";
    public string DateLabel => IsDe ? "Erstellt am:" : "Created:";

    // Computer Workspace - Hardware & Asset Diagnostics
    public string HardwareDiagnosticsTitle => IsDe ? "Hardware & Gerätediagnose" : "Hardware & Diagnostics";
    public string HardwareModelLabel => IsDe ? "Modell & Hersteller" : "Model & Manufacturer";
    public string SerialNumberLabel => IsDe ? "Seriennummer / Service-Tag" : "Serial Number / Service Tag";
    public string BiosVersionLabel => IsDe ? "BIOS-Version & Datum" : "BIOS Version & Date";
    public string OsBuildLabel => IsDe ? "Windows Build & Version" : "Windows Build & Version";
    public string CpuLabel => IsDe ? "Prozessor (CPU)" : "Processor (CPU)";
    public string TotalMemoryLabel => IsDe ? "Arbeitsspeicher (RAM)" : "Installed Memory (RAM)";
    public string CheckWarrantyTooltip => IsDe ? "Hersteller-Garantie aufrufen" : "Check Vendor Warranty";
    public string OpenUserWorkspaceTooltip => IsDe ? "Benutzer im Arbeitsbereich öffnen" : "Open user in workspace";
    public string FetchingHardwareData => IsDe ? "Hardware-Spezifikationen werden remote abgefragt..." : "Querying remote hardware specs...";
    public string HardwareDiagnosticFailed => IsDe ? "Hardwarediagnose nicht erreichbar (Computer offline oder WMI/RPC blockiert)" : "Hardware diagnostics unreachable (endpoint offline or WMI/RPC blocked)";
    public string RefreshHardwareBtn => IsDe ? "Hardware-Diagnose aktualisieren" : "Refresh Hardware Diagnostics";
    public string WarrantyBtn => IsDe ? "Garantie prüfen" : "Check Warranty";

    // Feature 2: Uptime & Pending Reboot Detection
    public string SystemUptimeLabel => IsDe ? "System-Betriebszeit" : "System Uptime";
    public string LastBootTimeLabel => IsDe ? "Letzter Systemstart" : "Last Boot Time";
    public string PendingRebootLabel => IsDe ? "Ausstehender Neustart" : "Pending Reboot";
    public string RebootRequired => IsDe ? "Neustart erforderlich" : "Reboot Required";
    public string NoRebootRequired => IsDe ? "Kein Neustart erforderlich" : "No Reboot Required";
    public string RebootStatusUnknown => IsDe ? "Unbekannt (Registry nicht erreichbar)" : "Unknown (Registry unreachable)";
    public static string RebootReasonsTooltip(string reasons) => IsDe ? $"Erkannte Ursachen: {reasons}" : $"Detected reasons: {reasons}";
    public static string FormatUptimeDays(int days, int hours) => IsDe ? $"{days} Tage, {hours} Std." : $"{days} days, {hours} hrs";
    public static string FormatUptimeHours(int hours, int minutes) => IsDe ? $"{hours} Std., {minutes} Min." : $"{hours} hrs, {minutes} mins";
    public static string FormatUptimeMinutes(int minutes) => IsDe ? $"{minutes} Min." : $"{minutes} mins";

    // Feature 3: Disk Space & Drive Health
    public string DrivesAndStorageTitle => IsDe ? "Laufwerke & Speicherplatz" : "Drives & Storage";
    public string RefreshDrivesBtn => IsDe ? "Laufwerksdaten aktualisieren" : "Refresh Drive Data";
    public string FetchingDrivesData => IsDe ? "Laufwerksdaten werden über WMI abgerufen..." : "Querying remote storage & drives...";
    public string DrivesDiagnosticFailed => IsDe ? "Laufwerksdiagnose nicht erreichbar (Computer offline oder WMI/RPC blockiert)" : "Drive diagnostics unreachable (endpoint offline or WMI/RPC blocked)";
    public string NoDrivesFound => IsDe ? "Keine lokalen Festplattenlaufwerke gefunden." : "No local fixed disk drives found.";
    public string FreeOfLabel => IsDe ? "frei von" : "free of";
    public string UsedLabel => IsDe ? "belegt" : "used";
    public string LowDiskSpaceWarning => IsDe ? "Geringer Speicherplatz (< 15%)" : "Low disk space (< 15%)";
    public string CriticalDiskSpaceWarning => IsDe ? "Kritischer Speicherplatz (< 5%)" : "Critical low disk space (< 5%)";
    public string DriveHealthOk => IsDe ? "Fehlerfrei (OK)" : "Healthy (OK)";
    public string DriveHealthWarning => IsDe ? "Warnung" : "Warning";
    public string DriveHealthCritical => IsDe ? "Kritisch" : "Critical";
    public string HealthyDriveTooltip => IsDe ? "Laufwerkszustand: Einwandfrei (OK)" : "Drive health: Healthy (OK)";
    public static string FormatDriveHealthTooltip(string health, string media) => IsDe
        ? $"Laufwerkszustand: {health} ({media})"
        : $"Drive health: {health} ({media})";
    public static string FormatDriveCapacity(string freeFormatted, string totalFormatted, double usedPct) => IsDe
        ? $"{freeFormatted} frei von {totalFormatted} ({usedPct:F0}% belegt)"
        : $"{freeFormatted} free of {totalFormatted} ({usedPct:F0}% used)";

    // Feature 4: Battery Degradation & Health
    public string BatteryAndPowerTitle => IsDe ? "Akku & Energie" : "Battery & Power";
    public string RefreshBatteryBtn => IsDe ? "Akkudaten aktualisieren" : "Refresh Battery Data";
    public string FetchingBatteryData => IsDe ? "Akkudaten werden remote abgefragt..." : "Querying remote battery & power state...";
    public string BatteryDiagnosticFailed => IsDe ? "Akkudiagnose nicht erreichbar (Computer offline oder WMI/RPC blockiert)" : "Battery diagnostics unreachable (endpoint offline or WMI/RPC blocked)";
    public string NoBatteryDetected => IsDe ? "Kein Akku erkannt (Desktop / stationäres System)" : "No battery detected (Desktop / AC-powered system)";
    public string BatteryHealthLabel => IsDe ? "Akkuzustand (Gesundheit)" : "Battery Health";
    public string BatteryWearNotice => IsDe ? "Verschleiß" : "Wear";
    public string BatteryDesignCapacityLabel => IsDe ? "Design-Kapazität" : "Design Capacity";
    public string BatteryFullChargeCapacityLabel => IsDe ? "Volle Ladekapazität" : "Full Charge Capacity";
    public string BatteryChargeRemainingLabel => IsDe ? "Aktueller Ladestand" : "Current Charge";
    public string BatteryCycleCountLabel => IsDe ? "Ladezyklen" : "Cycle Count";
    public string BatteryRuntimeLabel => IsDe ? "Geschätzte Restlaufzeit" : "Estimated Runtime";
    public string BatteryStatusCharging => IsDe ? "Wird geladen (Netzbetrieb)" : "Charging (AC)";
    public string BatteryStatusDischarging => IsDe ? "Entlädt (Akkubetrieb)" : "Discharging (Battery)";
    public string BatteryStatusFull => IsDe ? "Vollständig geladen" : "Fully Charged";
    public string BatteryStatusUnknown => IsDe ? "Unbekannt" : "Unknown";
    public string BatteryCyclesUnknown => IsDe ? "Nicht verfügbar" : "Not available";
    public string BatteryRuntimeUnknown => IsDe ? "Wird berechnet..." : "Calculating...";
    public string BatteryHealthOk => IsDe ? "Fehlerfrei" : "Healthy";
    public string BatteryHealthWarning => IsDe ? "Abnutzung" : "Degraded";
    public string BatteryHealthCritical => IsDe ? "Austausch empfohlen" : "Service Recommended";
    public string BatteryHealthOkFormat => IsDe ? "Fehlerfrei ({0:F0}%)" : "Healthy ({0:F0}%)";
    public string BatteryHealthWarningFormat => IsDe ? "Abnutzung ({0:F0}%)" : "Degraded ({0:F0}%)";
    public string BatteryHealthCriticalFormat => IsDe ? "Austausch empfohlen ({0:F0}%)" : "Service Recommended ({0:F0}%)";
    public static string FormatBatteryCycles(int cycles) => IsDe ? $"{cycles:N0} Zyklen" : $"{cycles:N0} cycles";
    public static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalDays >= 1)
        {
            return FormatUptimeDays((int)ts.TotalDays, ts.Hours);
        }
        if (ts.TotalHours >= 1)
        {
            return FormatUptimeHours(ts.Hours, ts.Minutes);
        }
        return FormatUptimeMinutes(Math.Max(1, ts.Minutes));
    }

    // Feature 5: Live Logged-On Users & Active Session Inspector
    public string LoggedOnUsersTitle => IsDe ? "Aktive Benutzer & Sitzungen" : "Logged-on Users & Active Sessions";
    public string NoActiveSessionsFound => IsDe ? "Keine aktiven Benutzersitzungen angemeldet." : "No active user sessions logged on.";
    public string SessionTypeConsole => IsDe ? "Konsole (Lokal)" : "Console (Local)";
    public string SessionTypeRdp => IsDe ? "Remotedesktop (RDP)" : "Remote Desktop (RDP)";
    public string SessionTypeDisconnected => IsDe ? "Getrennt" : "Disconnected";
    public string FetchingSessionData => IsDe ? "Sitzungen werden abgefragt..." : "Fetching active sessions...";
    public string SessionDiagnosticFailed => IsDe ? "Sitzungsdiagnose nicht erreichbar (Computer offline oder WMI/RPC blockiert)" : "Session diagnostics unreachable (endpoint offline or WMI/RPC blocked)";
    public string RefreshSessionsBtn => IsDe ? "Sitzungen aktualisieren" : "Refresh Sessions";
    public string DisconnectSessionBtn => IsDe ? "Sitzung trennen" : "Disconnect Session";
    public string DisconnectSessionTooltip => IsDe ? "Diese Benutzersitzung remote abmelden oder trennen" : "Disconnect or log off this remote user session";
    public string SessionDurationJustNow => IsDe ? "gerade angemeldet" : "just logged on";
    public static string FormatSessionSince(string logonTime) => IsDe ? $"Angemeldet seit: {logonTime}" : $"Logged on since: {logonTime}";
    public static string DisconnectSuccess(string user) => IsDe ? $"Sitzung von '{user}' wurde erfolgreich getrennt." : $"Session for '{user}' was disconnected successfully.";
    public static string DisconnectFailed(string user, string err) => IsDe ? $"Trennen der Sitzung von '{user}' fehlgeschlagen: {err}" : $"Failed to disconnect session for '{user}': {err}";

    // Safety Confirmation Dialog Strings
    public string ConfirmDisconnectSessionTitle => IsDe ? "Sitzungsabmeldung bestätigen" : "Confirm Session Disconnect";
    public static string ConfirmDisconnectSessionPrompt(string user, string host) => IsDe 
        ? $"Möchten Sie die Sitzung von '{user}' auf '{host}' wirklich trennen / abmelden? Nicht gespeicherte Daten des Benutzers könnten verloren gehen."
        : $"Are you sure you want to disconnect / log off the session of '{user}' on '{host}'? Any unsaved user data might be lost.";
    
    public string ConfirmRemoveFromGroupTitle => IsDe ? "Aus Gruppe entfernen" : "Remove from Group";
    public static string ConfirmRemoveUserFromGroupPrompt(string user, string group) => IsDe
        ? $"Möchten Sie den Benutzer '{user}' wirklich aus der Gruppe '{group}' entfernen?"
        : $"Are you sure you want to remove user '{user}' from group '{group}'?";
    public static string ConfirmRemoveComputerFromGroupPrompt(string computer, string group) => IsDe
        ? $"Möchten Sie das Computerkonto '{computer}' wirklich aus der Gruppe '{group}' entfernen?"
        : $"Are you sure you want to remove computer '{computer}' from group '{group}'?";

    public string ConfirmDisableAccountTitle => IsDe ? "Konto deaktivieren" : "Disable Account";
    public static string ConfirmDisableUserAccountPrompt(string user) => IsDe
        ? $"Möchten Sie das Benutzerkonto von '{user}' wirklich deaktivieren? Der Benutzer kann sich danach nicht mehr anmelden."
        : $"Are you sure you want to disable the user account of '{user}'? The user will no longer be able to log in.";
    public static string ConfirmDisableComputerAccountPrompt(string computer) => IsDe
        ? $"Möchten Sie das Computerkonto '{computer}' wirklich deaktivieren? Das Gerät verliert die Domänenauthentifizierung."
        : $"Are you sure you want to disable computer account '{computer}'? The device will lose domain trust.";

    public string ConfirmForcePasswordChangeTitle => IsDe ? "Kennwortänderung erzwingen" : "Require Password Change";
    public static string ConfirmForcePasswordChangePrompt(string user) => IsDe
        ? $"Möchten Sie für '{user}' erzwingen, dass das Kennwort bei der nächsten Anmeldung geändert werden muss?"
        : $"Are you sure you want to require '{user}' to change their password at next logon?";

    public string ConfirmSaveProfileTitle => IsDe ? "Profiländerungen speichern" : "Save Profile Changes";
    public static string ConfirmSaveProfilePrompt(string user) => IsDe
        ? $"Möchten Sie die vorgenommenen Änderungen am Benutzerkonto '{user}' in Active Directory speichern?"
        : $"Are you sure you want to save the modified properties for user '{user}' to Active Directory?";

    // Feature 6: Remote Process Manager & Task Terminator
    public string ProcessManagerTitle => IsDe ? "Remote-Task-Manager" : "Remote Process Manager";
    public string ProcessManagerBtn => IsDe ? "Task-Manager" : "Process Manager";
    public string ProcessManagerTooltip => IsDe ? "Laufende Prozesse auf dem Remote-Computer verwalten" : "Manage running processes on remote computer";
    public string PidHeader => "PID";
    public string ProcessNameHeader => IsDe ? "Name" : "Process Name";
    public string UserHeader => IsDe ? "Benutzer" : "User";
    public string CpuHeader => "CPU";
    public string MemoryHeader => IsDe ? "Arbeitsspeicher" : "Memory";
    public string NetworkHeader => IsDe ? "Netzwerk" : "Network";
    public string SearchProcessesPlaceholder => IsDe ? "Prozesse filtern (Name, PID, Benutzer)..." : "Filter processes (Name, PID, User)...";
    public string SortByMemoryDesc => IsDe ? "Speicher (absteigend)" : "Memory (High to Low)";
    public string SortByNameAsc => IsDe ? "Name (A-Z)" : "Name (A-Z)";
    public string SortByPidAsc => IsDe ? "PID (aufsteigend)" : "PID (Low to High)";
    public string TerminateProcessBtn => IsDe ? "Prozess beenden" : "End Process";
    public string TerminateProcessTooltip => IsDe ? "Prozess sofort beenden" : "Terminate process immediately";
    public string CriticalProcessCannotBeTerminated => IsDe ? "Kritischer Systemprozess kann nicht beendet werden (Systemschutz)" : "Critical system process cannot be terminated (System Protection)";
    public string ConfirmTerminateProcessTitle => IsDe ? "Prozess beenden bestätigen" : "Confirm Process Termination";
    public static string ConfirmTerminateProcessPrompt(string processName, uint pid, string host) => IsDe
        ? $"Möchten Sie den Prozess '{processName}' (PID: {pid}) auf '{host}' wirklich beenden? Nicht gespeicherte Daten in dieser Anwendung gehen verloren."
        : $"Are you sure you want to terminate process '{processName}' (PID: {pid}) on '{host}'? Unsaved data in this application will be lost.";
    public static string ProcessTerminatedSuccess(string processName) => IsDe ? $"Prozess '{processName}' erfolgreich beendet." : $"Process '{processName}' terminated successfully.";
    public static string TerminateProcessFailed(string error) => IsDe ? $"Beenden des Prozesses fehlgeschlagen: {error}" : $"Failed to terminate process: {error}";
    public static string TerminateProcessFailedNamed(string processName, uint pid) => IsDe
        ? $"Prozess '{processName}' (PID: {pid}) konnte nicht beendet werden."
        : $"Failed to terminate process '{processName}' (PID: {pid}).";
    public string FetchingProcessData => IsDe ? "Prozessliste wird remote abgefragt..." : "Querying remote process list...";
    public string NoProcessesFound => IsDe ? "Keine laufenden Prozesse gefunden." : "No running processes found.";
    public static string TotalProcessesCountBadge(int count) => IsDe ? $"{count} Prozesse" : $"{count} processes";

    // Feature 7: Remote Group Policy Refresh (GPUpdate)
    public string RemoteGpupdateBtn => "GPUpdate";
    public string RemoteGpupdateTooltip => IsDe ? "Gruppenrichtlinien auf dem Remote-Computer sofort aktualisieren (/force /nowait)" : "Trigger immediate Group Policy refresh remotely (/force /nowait)";
    public string ConfirmRemoteGpupdateTitle => IsDe ? "Remote-GPUpdate auslösen" : "Trigger Remote GPUpdate";
    public static string ConfirmRemoteGpupdatePrompt(string host) => IsDe
        ? $"Möchten Sie die Gruppenrichtlinien auf '{host}' wirklich aktualisieren? Dies führt 'gpupdate.exe /force /nowait' im Hintergrund auf dem Zielcomputer aus."
        : $"Are you sure you want to refresh Group Policy on '{host}'? This will execute 'gpupdate.exe /force /nowait' in the background on the target computer.";
    public static string RemoteGpupdateInitiated(string host) => IsDe
        ? $"Gruppenrichtlinien-Aktualisierung auf '{host}' eingeleitet."
        : $"Group Policy update initiated on '{host}'.";
    public static string RemoteGpupdateFailed(string host, string error) => IsDe
        ? $"Fehler beim Auslösen der Gruppenrichtlinien-Aktualisierung auf '{host}': {error}"
        : $"Failed to trigger Group Policy update on '{host}': {error}";

    // Feature 8: BitLocker Drive Encryption & Maintenance
    public string BitLockerSectionTitle => IsDe ? "BitLocker & Laufwerksverschlüsselung" : "BitLocker & Drive Encryption";
    public string BitLockerProtectionActive => IsDe ? "Schutz aktiv (Verschlüsselt)" : "Protection On (Active)";
    public string BitLockerProtectionSuspended => IsDe ? "Schutz ausgesetzt (1 Neustart)" : "Protection Suspended (1 Reboot)";
    public string BitLockerProtectionOff => IsDe ? "Schutz deaktiviert" : "Protection Off";
    public string BitLockerProtectionUnknown => IsDe ? "Schutzstatus unbekannt" : "Protection Unknown";
    public string BitLockerMethodLabel => IsDe ? "Verschlüsselungsmethode" : "Encryption Method";
    public string BitLockerStatusLabel => IsDe ? "Laufwerksstatus" : "Volume Status";
    public string SuspendBitLockerBtn => IsDe ? "Schutz aussetzen (1 Neustart)" : "Suspend (1 Reboot)";
    public string ResumeBitLockerBtn => IsDe ? "Schutz fortsetzen" : "Resume Protection";
    public string RefreshBitLockerBtn => IsDe ? "BitLocker-Status aktualisieren" : "Refresh BitLocker Status";
    public string FetchingBitLockerData => IsDe ? "BitLocker-Status wird remote abgefragt..." : "Querying remote BitLocker status...";
    public string ConfirmSuspendBitLockerTitle => IsDe ? "BitLocker-Schutz aussetzen" : "Suspend BitLocker Protection";
    public static string ConfirmSuspendBitLockerPrompt(string host, string drive) => IsDe
        ? $"Möchten Sie den BitLocker-Schutz auf '{host}' (Laufwerk {drive}) für 1 Neustart wirklich aussetzen? Dadurch wird die BitLocker-Verschlüsselung beim nächsten Neustart für Firmware- oder BIOS-Wartungen temporär umgangen."
        : $"Are you sure you want to suspend BitLocker protection on '{host}' (Drive {drive}) for 1 reboot? This temporarily bypasses BitLocker encryption on the next restart for firmware or BIOS maintenance.";
    public string ConfirmResumeBitLockerTitle => IsDe ? "BitLocker-Schutz fortsetzen" : "Resume BitLocker Protection";
    public static string ConfirmResumeBitLockerPrompt(string host, string drive) => IsDe
        ? $"Möchten Sie den BitLocker-Schutz auf '{host}' (Laufwerk {drive}) sofort wieder aktivieren?"
        : $"Are you sure you want to resume BitLocker protection on '{host}' (Drive {drive}) immediately?";
    public static string BitLockerSuspendedSuccess(string host) => IsDe
        ? $"BitLocker-Schutz für 1 Neustart auf '{host}' ausgesetzt."
        : $"BitLocker protection suspended for 1 reboot on '{host}'.";
    public static string BitLockerResumedSuccess(string host) => IsDe
        ? $"BitLocker-Schutz auf '{host}' erfolgreich fortgesetzt."
        : $"BitLocker protection resumed successfully on '{host}'.";
    public static string BitLockerActionFailed(string host, string error) => IsDe
        ? $"BitLocker-Vorgang auf '{host}' fehlgeschlagen: {error}"
        : $"BitLocker operation failed on '{host}': {error}";
    public string BitLockerKeysSubtitle => IsDe ? "Active Directory-Wiederherstellungsschlüssel" : "Active Directory Recovery Keys";

    // Advanced Attribute Editor (Safe Whitelist & Inspector)
    public string AdvancedEditorBtn => IsDe ? "Attribut-Editor" : "Attribute Editor";
    public string BackToProfileBtn => IsDe ? "Zurück zum Profil" : "Back to Profile";
    public string AttributeEditorTitle => IsDe ? "Active Directory-Attribut-Editor" : "Active Directory Attribute Editor";
    public string AttributeEditorDesc => IsDe ? "Schemaattribute für diesen Benutzer prüfen und sicher bearbeiten." : "Inspect and safely modify schema attributes for this user.";
    public string FilterAttributesPlaceholder => IsDe ? "Attribute filtern..." : "Filter attributes...";
    public string EditAttributeBtn => IsDe ? "Wert bearbeiten" : "Edit Value";
    public string OldValueLabel => IsDe ? "Bisheriger Wert:" : "Current Value:";
    public string NewValueLabel => IsDe ? "Neuer Wert:" : "New Value:";
    public string ConfirmAttributeChangeTitle => IsDe ? "Attributänderung bestätigen" : "Confirm Attribute Change";
    public static string ConfirmAttributeChangePrompt(string attr, string oldVal, string newVal) => IsDe 
        ? $"Möchten Sie das Attribut '{attr}' wirklich von '{oldVal}' zu '{newVal}' ändern? Diese Aktion wird protokolliert."
        : $"Are you sure you want to update attribute '{attr}' from '{oldVal}' to '{newVal}'? This action will be audited.";
    public static string AttributeUpdateSuccess(string attr) => IsDe ? $"Attribut '{attr}' erfolgreich aktualisiert." : $"Attribute '{attr}' updated successfully.";
    public static string AttributeUpdateFailed(string attr, string err) => IsDe ? $"Aktualisierung von '{attr}' fehlgeschlagen: {err}" : $"Failed to update '{attr}': {err}";
    public static string AttributeLabel(string key) => IsDe ? $"Attribut: {key}" : $"Attribute: {key}";
    public string AuditLogNotice => IsDe ? "Alle Änderungen an diesem Attribut werden dauerhaft im Sicherheitsprotokoll erfasst." : "All modifications to this attribute will be durably written to the security audit log.";
    public string NonEditableAttributeTooltip => IsDe ? "Dieses Attribut ist schreibgeschützt (Typ nicht zur Bearbeitung freigegeben)." : "This attribute is read-only (type excluded from safe editing).";

    // Feature 9: JIRA Ticket Integration
    public string NavJiraWorkspace => IsDe ? "JIRA-Integration" : "JIRA Integration";
    public string JiraWorkspaceTitle => IsDe ? "JIRA-Integration" : "JIRA Integration";
    public string SearchUserForJiraPlaceholder => IsDe ? "Benutzer suchen, um erstellte JIRA-Tickets anzuzeigen..." : "Search for a user to view created JIRA tickets...";
    public string JiraTicketsSectionTitle => IsDe ? "Erstellte JIRA-Tickets" : "Created JIRA Tickets";
    public string JiraFilterPlaceholder => IsDe ? "Nach Vorgangsschlüssel oder Zusammenfassung filtern..." : "Filter by issue key or summary...";
    public string JiraKeyColumn => IsDe ? "Schlüssel" : "Key";
    public string JiraStatusColumn => IsDe ? "Status" : "Status";
    public string JiraSummaryColumn => IsDe ? "Zusammenfassung" : "Summary";
    public string JiraPriorityColumn => IsDe ? "Priorität" : "Priority";
    public string JiraCreatedColumn => IsDe ? "Erstellt" : "Created";
    public string LoadMoreBtn => IsDe ? "Weitere Tickets laden" : "Load More Tickets";
    public string NoJiraTicketsForUser => IsDe ? "Keine offenen JIRA-Tickets für diesen Benutzer gefunden." : "No open JIRA tickets found for this user.";
    public string FetchingJiraTickets => IsDe ? "JIRA-Tickets werden abgerufen..." : "Fetching JIRA tickets...";
    public string JiraFetchError => IsDe ? "Fehler beim Abrufen der JIRA-Tickets" : "Error fetching JIRA tickets";
    public string ViewJiraTicketsBtn => IsDe ? "JIRA-Tickets anzeigen" : "View JIRA Tickets";
    public string ViewJiraTicketsTooltip => IsDe ? "Offene JIRA-Tickets dieses Benutzers anzeigen" : "View open JIRA tickets created by this user";
    public static string TotalJiraTicketsCountBadge(int count) => count == 1 
        ? (IsDe ? "1 offenes Ticket" : "1 open ticket") 
        : (IsDe ? $"{count} offene Tickets" : $"{count} open tickets");
    public string JiraNotConfiguredPrompt => IsDe ? "JIRA-Integration ist in den Einstellungen nicht aktiviert oder konfiguriert." : "JIRA integration is not enabled or configured in Settings.";
    public string RefreshJiraBtn => IsDe ? "JIRA-Tickets aktualisieren" : "Refresh JIRA Tickets";

    // JIRA Settings Configuration
    public string JiraSettings => IsDe ? "JIRA-Integration" : "JIRA Integration";
    public string JiraIntegrationHeader => IsDe ? "JIRA-Ticketsystem anbinden" : "Connect JIRA Ticket System";
    public string JiraIntegrationDesc => IsDe ? "Offene JIRA-Tickets von Benutzern direkt in Sol abrufen und anzeigen." : "Fetch and inspect open JIRA tickets created by users directly in Sol.";
    public string JiraDeploymentModeLabel => IsDe ? "Bereitstellungsmodell" : "Deployment Model";
    public string JiraDataCenterOption => "Jira Data Center / Server";
    public string JiraCloudOption => "Jira Cloud (Atlassian)";
    public string JiraBaseUrlLabel => IsDe ? "Basis-URL" : "Base URL";
    public string JiraCloudEmailLabel => IsDe ? "Atlassian-Konto-E-Mail" : "Atlassian Account Email";
    public string JiraPatLabel => "Personal Access Token (PAT)";
    public string JiraPatPlaceholder => IsDe ? "Persönliches Zugriffstoken eingeben..." : "Enter Personal Access Token...";
    public string JiraApiTokenLabel => IsDe ? "Atlassian API-Token" : "Atlassian API Token";
    public string JiraApiTokenPlaceholder => IsDe ? "API-Token eingeben..." : "Enter API token...";
    public string TestJiraConnectionBtn => IsDe ? "Verbindung testen" : "Test Connection";
    public string TestingJiraConnection => IsDe ? "JIRA-Verbindung wird getestet..." : "Testing JIRA connection...";
    public string JiraConnectionSuccessPrompt => IsDe ? "JIRA-Verbindung erfolgreich hergestellt." : "JIRA connection established successfully.";
    public string JiraConnectionFailedPrompt => IsDe ? "JIRA-Verbindung fehlgeschlagen. Bitte URL und Zugangsdaten prüfen." : "JIRA connection failed. Please verify URL and credentials.";
    public string JiraCredentialsSavedPrompt => IsDe ? "JIRA-Anmeldeinformationen sicher im Windows-Tresor gespeichert." : "JIRA credentials securely saved in Windows Credential Locker.";

    // Feature 10: Remote Windows Services Inspector & Controller
    public string ServicesInspectorTitle => IsDe ? "Remotedienste-Manager" : "Remote Services Manager";
    public string ServicesInspectorBtn => IsDe ? "Dienste" : "Services";
    public string ServicesInspectorTooltip => IsDe ? "Windows-Dienste auf dem Remote-Computer anzeigen und verwalten" : "View and manage Windows services on remote computer";
    public string ServiceDisplayNameCol => IsDe ? "Anzeigename" : "Display Name";
    public string ServiceNameCol => IsDe ? "Dienstname" : "Service Name";
    public string ServiceStatusCol => "Status";
    public string ServiceStatusRunning => IsDe ? "Wird ausgeführt" : "Running";
    public string ServiceStatusStopped => IsDe ? "Beendet" : "Stopped";
    public string ServiceStartupTypeCol => IsDe ? "Starttyp" : "Startup Type";
    public string ServiceLogOnAsCol => IsDe ? "Anmelden als" : "Log On As";
    public string ServiceActionsCol => IsDe ? "Aktionen" : "Actions";
    public string ServiceFilterAll => IsDe ? "Alle" : "All";
    public string ServiceFilterRunning => IsDe ? "Wird ausgeführt" : "Running";
    public string ServiceFilterStopped => IsDe ? "Beendet" : "Stopped";
    public string SearchServicesPlaceholder => IsDe ? "Dienste filtern (Name, Anzeigename, Anmeldekonto)..." : "Filter services (Name, Display Name, Account)...";
    public string FetchingServicesData => IsDe ? "Dienstliste wird remote abgefragt..." : "Querying remote services list...";
    public string NoServicesFound => IsDe ? "Keine Dienste gefunden." : "No services found.";
    public string StartServiceBtn => IsDe ? "Starten" : "Start";
    public string StartServiceTooltip => IsDe ? "Dienst starten" : "Start service";
    public string StopServiceBtn => IsDe ? "Beenden" : "Stop";
    public string StopServiceTooltip => IsDe ? "Dienst beenden" : "Stop service";
    public string RestartServiceBtn => IsDe ? "Neu starten" : "Restart";
    public string RestartServiceTooltip => IsDe ? "Dienst neu starten" : "Restart service";
    public string CriticalServiceProtected => IsDe ? "Kritischer Systemdienst (geschützt vor Beenden/Neustart)" : "Critical system service (protected from stop/restart)";
    public string ServiceStartModeAuto => IsDe ? "Automatisch" : "Automatic";
    public string ServiceStartModeManual => IsDe ? "Manuell" : "Manual";
    public string ServiceStartModeDisabled => IsDe ? "Deaktiviert" : "Disabled";
    public string ConfirmStartServiceTitle => IsDe ? "Dienst starten" : "Start Service";
    public string ConfirmStopServiceTitle => IsDe ? "Dienst beenden" : "Stop Service";
    public string ConfirmRestartServiceTitle => IsDe ? "Dienst neu starten" : "Restart Service";
    public string ConfirmChangeStartupTypeTitle => IsDe ? "Starttyp ändern" : "Change Startup Type";
    public static string ConfirmStartServicePrompt(string displayName, string host) => IsDe
        ? $"Möchten Sie den Dienst '{displayName}' auf '{host}' wirklich starten?"
        : $"Are you sure you want to start service '{displayName}' on '{host}'?";
    public static string ConfirmStopServicePrompt(string displayName, string host) => IsDe
        ? $"Möchten Sie den Dienst '{displayName}' auf '{host}' wirklich beenden? Abhängige Anwendungen funktionieren möglicherweise nicht mehr ordnungsgemäß."
        : $"Are you sure you want to stop service '{displayName}' on '{host}'? Dependent applications may stop functioning properly.";
    public static string ConfirmRestartServicePrompt(string displayName, string host) => IsDe
        ? $"Möchten Sie den Dienst '{displayName}' auf '{host}' wirklich neu starten?"
        : $"Are you sure you want to restart service '{displayName}' on '{host}'?";
    public static string ConfirmChangeStartupTypePrompt(string displayName, string host, string newMode) => IsDe
        ? $"Möchten Sie den Starttyp für '{displayName}' auf '{host}' wirklich auf '{newMode}' ändern?"
        : $"Are you sure you want to change the startup type for '{displayName}' on '{host}' to '{newMode}'?";
    public static string ServiceStartedSuccess(string displayName) => IsDe ? $"Dienst '{displayName}' erfolgreich gestartet." : $"Service '{displayName}' started successfully.";
    public static string ServiceStoppedSuccess(string displayName) => IsDe ? $"Dienst '{displayName}' erfolgreich beendet." : $"Service '{displayName}' stopped successfully.";
    public static string ServiceRestartedSuccess(string displayName) => IsDe ? $"Dienst '{displayName}' erfolgreich neu gestartet." : $"Service '{displayName}' restarted successfully.";
    public static string ServiceStartModeChangedSuccess(string displayName, string mode) => IsDe ? $"Starttyp für '{displayName}' auf '{mode}' geändert." : $"Startup type for '{displayName}' changed to '{mode}'.";
    public static string ServiceActionFailed(string action, string error) => IsDe ? $"Dienstaktion '{action}' fehlgeschlagen: {error}" : $"Service action '{action}' failed: {error}";
    public string ServiceAccessDenied => IsDe ? "Zugriff verweigert. Administratorrechte erforderlich." : "Access denied. Administrator privileges required.";
    public string ServiceLocalElevationRequired => IsDe ? "Lokale Dienststeuerung erfordert das Ausführen der App als Administrator (Rechtsklick -> 'Als Administrator ausführen')." : "Controlling local services requires running the app as Administrator (Right-click -> 'Run as administrator').";
    public string ServiceDependentServicesRunning => IsDe ? "Dienst kann nicht beendet werden, da abhängige Dienste noch ausgeführt werden." : "Service cannot be stopped because dependent services are still running.";
    public string ServiceCannotAcceptControl => IsDe ? "Dienst kann die Steuerung derzeit nicht annehmen (möglicherweise wird er bereits gestartet oder beendet)." : "Service cannot accept control at this time (it may be starting or stopping).";
    public string ServiceDisabled => IsDe ? "Dienst ist deaktiviert. Ändern Sie zuerst den Starttyp auf Manuell oder Automatisch." : "Service is disabled. Change startup type to Manual or Automatic first.";
    public string ServiceLogonFailed => IsDe ? "Dienstanmeldung fehlgeschlagen. Dienstkonto-Anmeldeinformationen prüfen." : "Service logon failed. Verify service account credentials.";
    public string ServiceAlreadyRunning => IsDe ? "Dienst wird bereits ausgeführt." : "Service is already running.";
    public string ServiceAlreadyStopped => IsDe ? "Dienst ist bereits beendet." : "Service is already stopped.";
    public string ServiceRequestTimeout => IsDe ? "Zeitüberschreitung bei der Dienstanforderung." : "Service control request timed out.";
    public string ServiceNotSupported => IsDe ? "Dienstaktion wird vom Dienst nicht unterstützt." : "Service action is not supported by the service.";
    public string ServiceInvalidControl => IsDe ? "Ungültiger Steuerungsbefehl für diesen Dienst." : "Invalid control command for this service.";
    public string ServiceInvalidParameter => IsDe ? "Ungültiger Parameter für die Dienstkonfiguration." : "Invalid parameter for service configuration.";
    public static string TotalServicesCountBadge(int count) => IsDe ? $"{count} Dienste" : $"{count} services";
    public static string RunningServicesCountBadge(int count) => IsDe ? $"{count} aktiv" : $"{count} running";
    public static string StoppedServicesCountBadge(int count) => IsDe ? $"{count} beendet" : $"{count} stopped";

    // SettingsPage
    public string SettingsTitle => IsDe ? "Einstellungen" : "Settings";
    public string GeneralSettings => IsDe ? "Allgemein" : "General";
    public string AppLanguageLabel => IsDe ? "App-Sprache" : "App Language";
    public string RestartRequiredDesc => IsDe ? "Neustart erforderlich, um Sprachänderungen zu übernehmen" : "Restart required to apply changes";
    public string AdSettings => IsDe ? "Active Directory" : "Active Directory";
    public string DomainNameLabel => IsDe ? "Domänenname" : "Domain Name";
    
    public string SaveSettingsBtn => IsDe ? "Einstellungen speichern" : "Save Settings";
    public string TestAdBtn => IsDe ? "AD-Verbindung testen" : "Test AD Connection";
    public string SettingsSavedPrompt => IsDe ? "Einstellungen gespeichert. Bitte App neu starten, um Sprachänderungen zu übernehmen." : "Settings saved. Restart app to apply language.";
    public string SettingsSaveErrorPrompt => IsDe ? "Fehler beim Speichern der Einstellungen oder Anmeldeinformationen." : "Error saving settings or credentials.";
    public string TestingConnection => IsDe ? "Verbindung wird getestet..." : "Testing connection...";

    public string AboutSettings => IsDe ? "Über" : "About";
    public string VersionLabel => IsDe ? "Version" : "Version";
    public string DeveloperLabel => IsDe ? "Entwickler" : "Developer";
    public string GitHubProfileLabel => "GitHub (@mm-dev-alpha)";

    public static string[] AllGreetings => IsDe 
        ? new[] 
        {
            "\"Gestern ging es aber noch!!\" ⏳",
            "\"Da kam so eine Fehlermeldung, aber die habe ich einfach weggeklickt.\" 🖱️",
            "\"Mein Computer ist heute so komisch langsam, das liegt sicher an eurem letzten Update!\" 🐌",
            "\"Ich kann nicht drucken.\" 🖨️",
            "\"Ja, natürlich habe ich den PC neu gestartet!\" 🔌",
            "\"Mein Passwort ist zu 100 % richtig, das System spinnt einfach!\" 🔑",
            "\"Das ist extrem dringend, ich kann seit drei Wochen nicht arbeiten!!\" 🚨",
            "\"Ein Ticket erstellen? Geht das nicht einfach so auf dem kurzen Dienstweg?\" 🎫"
        }
        : new[] 
        {
            "\"It was working fine yesterday!\" ⏳",
            "\"An error message popped up, but I just clicked it away.\" 🖱️",
            "\"My computer is unusually slow today; it must be because of your latest update!\" 🐌",
            "\"I'm unable to print.\" 🖨️",
            "\"Yes, of course I rebooted the computer!\" 🔌",
            "\"My password is 100% correct, the system is just acting up!\" 🔑",
            "\"This is extremely urgent, I haven't been able to work for three weeks!!\" 🚨",
            "\"Submit a ticket? Can't we just sort this out quickly off the record?\" 🎫"
        };
}
