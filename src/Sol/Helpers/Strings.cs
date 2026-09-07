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
    public static bool IsDe => false;

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
    public string Yes => "Yes";
    public string No => "No";
    public string Never => "Never";
    public string SaveBtn => "Save";
    public string HomeBtn => "Home";
    public string LoadingUserData => "Loading data...";
    public string CopiedToClipboard => "Copied to clipboard.";
    public string ClipboardBusy => "Clipboard is currently locked by another application.";

    // MainWindow
    public string NavHome => "Home";
    public string NavUserWorkspace => "User Workspace";
    public string NavComputerWorkspace => "Computer Workspace";
    public string NavSettings => "Settings";
    public string RunningAs => "Running as: ";

    // User & Computer Search
    public string SearchUserPlaceholder => "Search for a user...";
    public string SearchComputerPlaceholder => "Search for a computer...";
    public string MultipleUsersFound => "Multiple Users Found";
    public string SelectUserPrompt => "Please select the correct user:";
    public static string NoUsersFound(string query) => $"No users found matching '{query}'.";
    public static string ErrorLoadingUser(string msg) => $"Error loading user: {msg}";

    // UserWorkspacePage - Hero & Actions
    public string UserDetailsTitle => "User Details";
    public string FirstNameLabel => "First Name";
    public string LastNameLabel => "Last Name";
    public string DisplayNameLabel => "Display Name";
    public string EmailLabel => "Email";
    public string DepartmentLabel => "Department";
    public string TitleLabel => "Title";
    public string ManagerLabel => "Manager";
    public string AccountStatusLabel => "Account Status";
    public string SidLabel => "Security Identifier (SID)";
    public string AddressLabel => "Address";
    public string WebsiteLabel => "Website";
    public string EditBtn => "Edit";
    public string CancelBtn => "Cancel";
    public string CopyBtn => "Copy";
    public string CopyAllBtn => "Copy All";
    public string CloseWorkspaceBtn => "Close Workspace";
    
    public string LockedOut => "Locked Out";
    public string Disabled => "Disabled";
    public string Active => "Active";
    public string EnableAccountBtn => "Enable Account";
    public string DisableAccountBtn => "Disable Account";
    public string EnableComputerBtn => "Enable Computer";
    public string DisableComputerBtn => "Disable Computer";
    public string UnlockAccountBtn => "Unlock Account";
    public string PasswordActionsBtn => "Password Actions";
    public string ResetPasswordBtn => "Reset Password";
    public string ForcePasswordChangeBtn => "User must change password at next logon";
    public string SetNewPasswordTitle => "Set New Password";
    public string NewPasswordLabel => "New Password";
    public string NewPasswordPlaceholder => "Enter new password or generate";
    public string GeneratePasswordBtn => "Generate Password";
    public string MustChangePasswordCheckbox => "User must change password at next logon";
    public string UnlockAccountCheckbox => "Unlock account if locked out";
    public string PasswordResetAuditNotice => "The new password will be copied to your clipboard upon confirmation. This action is logged.";
    public static string ResetPasswordDialogTitle(string name) => $"Reset Password for {name}";

    // UserWorkspacePage - Contact Information Section
    public string ContactInfoSection => "Contact Information";
    public string OfficeLabel => "Office";
    public string OfficePhoneLabel => "Office Phone";
    public string MobilePhoneLabel => "Mobile Phone";
    public string ViewManagerBtn => "View Manager";
    public string DirectReportsLabel => "Direct Reports";
    public string ViewProfileBtn => "View Profile";
    public string SearchManagerPlaceholder => "Search manager...";

    // UserWorkspacePage - Security & Logon Section
    public string SecurityLogonSection => "Security & Logon";
    public string PasswordLastSetLabel => "Password Last Set";
    public string PasswordExpiryLabel => "Password Expiry";
    public string MustChangePasswordLabel => "Must change password at next logon";
    public string BadPasswordCountLabel => "Bad Password Count";
    public string LastLogonLabel => "Last Logon";
    public string PasswordNeverExpires => "Never expires";
    public string PasswordExpired => "Expired";
    public string PasswordExpiresToday => "Expires today";
    public string PasswordExpiresTomorrow => "Expires tomorrow";
    public static string PasswordExpiresInDays(int days) => $"Expires in {days} days";
    public string PasswordStatusUnknown => "Unknown";

    // Group Memberships Component (Standardized across Workspaces)
    public string GroupsTitle => "Group Memberships";
    public string AddGroupTitle => "Add to Group";
    public string AddGroupBtn => "Add";
    public string AddBtn => "Add";
    public string RemoveBtn => "Remove";
    public string AddGroupPlaceholder => "Add to group...";
    public string FilterGroupsPlaceholder => "Filter groups...";
    public string NoGroupsFound => "No groups found.";
    public string NoGroupsMatchFilter => "No groups match the filter.";
    public string AddGroupTooltip => "Add to group";
    public string RemoveGroupTooltip => "Remove from group";
    public string SearchGroupToAddPlaceholder => "Search or enter group name...";

    // Notifications (Toasts / InfoBar)
    public string AccountUnlockedSuccess => "Account unlocked successfully.";
    public string AccountEnabledSuccess => "Account enabled.";
    public string AccountDisabledSuccess => "Account disabled.";
    public string ComputerEnabledSuccess => "Computer enabled.";
    public string ComputerDisabledSuccess => "Computer disabled.";
    public string PasswordResetSuccess => "Password reset successfully. Copied to clipboard.";
    public string ForcePasswordChangeSuccess => "User forced to change password at next logon.";
    public string ProfileUpdatedSuccess => "Profile updated successfully.";
    public static string AddedToGroupSuccess(string group) => $"Added to {group}.";
    public static string RemovedFromGroupSuccess(string group) => $"Removed from {group}.";
    public static string SaveProfileFailed(string error) => $"Save failed: {error}";

    // TitleBar & Shell
    public string AppTitle => "Sol";
    public string TitleBarSearchPlaceholder => "Search for a user...";
    public string SearchUserPlaceholderWithShortcut => "Search for a user...";
    public string ExportBtn => "Export";
    public string FilterPlaceholder => "Filter...";
    public string RefreshBtn => "Refresh";
    public string CloseBtn => "Close";
    public string ConfirmBtn => "Confirm";
    public string DeleteBtn => "Delete";

    // User Workspace Sections
    public string AccountIdentitySection => "Account & Identity";
    public string OrganizationHierarchySection => "Organization & Hierarchy";
    public string LogonNameLabel => "Logon Name";
    public string UpnLabel => "User Principal Name (UPN)";
    public string EmployeeIdLabel => "Employee ID";
    public string OuPathLabel => "OU Path";
    public string AccountExpiresLabel => "Account Expires";
    public string CopyPowerShellBtn => "Copy PowerShell Command";
    public string PowerShellCommandCopied => "PowerShell command copied to clipboard.";
    public string PowerShellCopiedSuccess => "PowerShell command copied to clipboard.";
    public string AllInfoCopiedSuccess => "All profile information copied to clipboard.";
    public string ExportProfileBtn => "Export Profile";

    // Computer Workspace
    public string NoComputerFound => "No matching computer found in Active Directory.";
    public string MultipleComputersFound => "Multiple Computers Found";
    public string SelectComputerPrompt => "Please select the computer account:";
    public string BitLockerKeysTitle => "BitLocker Recovery Keys";
    public string BitLockerKeyCopiedSuccess => "Recovery key copied to clipboard.";
    public string NoBitLockerKeysForComputer => "No BitLocker recovery keys stored for this computer account.";
    public string SystemIdentityTitle => "System & Network Identity";
    public string ComputerNameLabel => "Computer Name";
    public string DnsHostNameLabel => "DNS Hostname";
    public string OperatingSystemLabel => "Operating System";
    public string OsVersionLabel => "OS Version";
    public string DescriptionLabel => "Description";
    public string ManagedByLabel => "Managed By";
    public string LocationLabel => "Location";
    public string AccountSecurityTitle => "Account Status & Security";
    public string ObjectCreatedLabel => "Object Created";
    public string ObjectModifiedLabel => "Object Modified";
    public string QuickDiagnosticTitle => "Diagnostics & Remote Tools";
    public string DiagnosticsQuerying => "Querying diagnostics...";
    public string PingBtn => "Test Connection (Ping)";
    public string RemotePsBtn => "Launch Remote PowerShell";
    public string RdpBtn => "Remote Desktop (RDP)";
    public string DateLabel => "Created:";
    public string InvalidHostNameError => "Invalid hostname or IP address format.";

    // Computer Workspace - Hardware & Asset Diagnostics
    public string HardwareDiagnosticsTitle => "Hardware & Diagnostics";
    public string HardwareModelLabel => "Model & Manufacturer";
    public string SerialNumberLabel => "Serial Number / Service Tag";
    public string BiosVersionLabel => "BIOS Version & Date";
    public string OsBuildLabel => "Windows Build & Version";
    public string CpuLabel => "Processor (CPU)";
    public string TotalMemoryLabel => "Installed Memory (RAM)";
    public string CheckWarrantyTooltip => "Check Vendor Warranty";
    public string OpenUserWorkspaceTooltip => "Open user in workspace";
    public string FetchingHardwareData => "Querying remote hardware specs...";
    public string HardwareDiagnosticFailed => "Hardware diagnostics unreachable (endpoint offline or WMI/RPC blocked)";
    public string RefreshHardwareBtn => "Refresh Hardware Diagnostics";
    public string WarrantyBtn => "Check Warranty";

    // Feature 2: Uptime & Pending Reboot Detection
    public string SystemUptimeLabel => "System Uptime";
    public string LastBootTimeLabel => "Last Boot Time";
    public string PendingRebootLabel => "Pending Reboot";
    public string RebootRequired => "Reboot Required";
    public string NoRebootRequired => "No Reboot Required";
    public string RebootStatusUnknown => "Unknown (Registry unreachable)";
    public static string RebootReasonsTooltip(string reasons) => $"Detected reasons: {reasons}";
    public static string FormatUptimeDays(int days, int hours) => $"{days} days, {hours} hrs";
    public static string FormatUptimeHours(int hours, int minutes) => $"{hours} hrs, {minutes} mins";
    public static string FormatUptimeMinutes(int minutes) => $"{minutes} mins";

    // Feature 3: Disk Space & Drive Health
    public string DrivesAndStorageTitle => "Drives & Storage";
    public string RefreshDrivesBtn => "Refresh Drive Data";
    public string FetchingDrivesData => "Querying remote storage & drives...";
    public string DrivesDiagnosticFailed => "Drive diagnostics unreachable (endpoint offline or WMI/RPC blocked)";
    public string NoDrivesFound => "No local fixed disk drives found.";
    public string FreeOfLabel => "free of";
    public string UsedLabel => "used";
    public string LowDiskSpaceWarning => "Low disk space (< 15%)";
    public string CriticalDiskSpaceWarning => "Critical low disk space (< 5%)";
    public string DriveHealthOk => "Healthy (OK)";
    public string DriveHealthWarning => "Warning";
    public string DriveHealthCritical => "Critical";
    public string HealthyDriveTooltip => "Drive health: Healthy (OK)";
    public static string FormatDriveHealthTooltip(string health, string media) => $"Drive health: {health} ({media})";
    public static string FormatDriveCapacity(string freeFormatted, string totalFormatted, double usedPct) => $"{freeFormatted} free of {totalFormatted} ({usedPct:F0}% used)";

    // Feature 4: Battery Degradation & Health
    public string BatteryAndPowerTitle => "Battery & Power";
    public string RefreshBatteryBtn => "Refresh Battery Data";
    public string FetchingBatteryData => "Querying remote battery & power state...";
    public string BatteryDiagnosticFailed => "Battery diagnostics unreachable (endpoint offline or WMI/RPC blocked)";
    public string NoBatteryDetected => "No battery detected (Desktop / AC-powered system)";
    public string BatteryHealthLabel => "Battery Health";
    public string BatteryWearNotice => "Wear";
    public string BatteryDesignCapacityLabel => "Design Capacity";
    public string BatteryFullChargeCapacityLabel => "Full Charge Capacity";
    public string BatteryChargeRemainingLabel => "Current Charge";
    public string BatteryCycleCountLabel => "Cycle Count";
    public string BatteryRuntimeLabel => "Estimated Runtime";
    public string BatteryStatusCharging => "Charging (AC)";
    public string BatteryStatusDischarging => "Discharging (Battery)";
    public string BatteryStatusFull => "Fully Charged";
    public string BatteryStatusUnknown => "Unknown";
    public string BatteryCyclesUnknown => "Not available";
    public string BatteryRuntimeUnknown => "Calculating...";
    public string BatteryHealthOk => "Healthy";
    public string BatteryHealthWarning => "Degraded";
    public string BatteryHealthCritical => "Service Recommended";
    public string BatteryHealthOkFormat => "Healthy ({0:F0}%)";
    public string BatteryHealthWarningFormat => "Degraded ({0:F0}%)";
    public string BatteryHealthCriticalFormat => "Service Recommended ({0:F0}%)";
    public static string FormatBatteryCycles(int cycles) => $"{cycles:N0} cycles";
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
    public string LoggedOnUsersTitle => "Logged-on Users & Active Sessions";
    public string NoActiveSessionsFound => "No active user sessions logged on.";
    public string SessionTypeConsole => "Console (Local)";
    public string SessionTypeRdp => "Remote Desktop (RDP)";
    public string SessionTypeDisconnected => "Disconnected";
    public string FetchingSessionData => "Fetching active sessions...";
    public string SessionDiagnosticFailed => "Session diagnostics unreachable (endpoint offline or WMI/RPC blocked)";
    public string RefreshSessionsBtn => "Refresh Sessions";
    public string DisconnectSessionBtn => "Disconnect Session";
    public string DisconnectSessionTooltip => "Disconnect or log off this remote user session";
    public string SessionDurationJustNow => "just logged on";
    public static string FormatSessionSince(string logonTime) => $"Logged on since: {logonTime}";
    public static string DisconnectSuccess(string user) => $"Session for '{user}' was disconnected successfully.";
    public static string DisconnectFailed(string user, string err) => $"Failed to disconnect session for '{user}': {err}";

    // Safety Confirmation Dialog Strings
    public string ConfirmDisconnectSessionTitle => "Confirm Session Disconnect";
    public static string ConfirmDisconnectSessionPrompt(string user, string host) => $"Are you sure you want to disconnect / log off the session of '{user}' on '{host}'? Any unsaved user data might be lost.";
    
    public string ConfirmRemoveFromGroupTitle => "Remove from Group";
    public static string ConfirmRemoveUserFromGroupPrompt(string user, string group) => $"Are you sure you want to remove user '{user}' from group '{group}'?";
    public static string ConfirmRemoveComputerFromGroupPrompt(string computer, string group) => $"Are you sure you want to remove computer '{computer}' from group '{group}'?";

    public string ConfirmDisableAccountTitle => "Disable Account";
    public static string ConfirmDisableUserAccountPrompt(string user) => $"Are you sure you want to disable the user account of '{user}'? The user will no longer be able to log in.";
    public static string ConfirmDisableComputerAccountPrompt(string computer) => $"Are you sure you want to disable computer account '{computer}'? The device will lose domain trust.";

    public string ConfirmForcePasswordChangeTitle => "Require Password Change";
    public static string ConfirmForcePasswordChangePrompt(string user) => $"Are you sure you want to require '{user}' to change their password at next logon?";

    public string ConfirmSaveProfileTitle => "Save Profile Changes";
    public static string ConfirmSaveProfilePrompt(string user) => $"Are you sure you want to save the modified properties for user '{user}' to Active Directory?";

    // Feature 6: Remote Process Manager & Task Terminator
    public string ProcessManagerTitle => "Remote Process Manager";
    public string ProcessManagerBtn => "Process Manager";
    public string ProcessManagerTooltip => "Manage running processes on remote computer";
    public string PidHeader => "PID";
    public string ProcessNameHeader => "Process Name";
    public string UserHeader => "User";
    public string CpuHeader => "CPU";
    public string MemoryHeader => "Memory";
    public string NetworkHeader => "Network";
    public string SearchProcessesPlaceholder => "Filter processes (Name, PID, User)...";
    public string SortByMemoryDesc => "Memory (High to Low)";
    public string SortByNameAsc => "Name (A-Z)";
    public string SortByPidAsc => "PID (Low to High)";
    public string TerminateProcessBtn => "End Process";
    public string TerminateProcessTooltip => "Terminate process immediately";
    public string CriticalProcessCannotBeTerminated => "Critical system process cannot be terminated (System Protection)";
    public string ConfirmTerminateProcessTitle => "Confirm Process Termination";
    public static string ConfirmTerminateProcessPrompt(string processName, uint pid, string host) => $"Are you sure you want to terminate process '{processName}' (PID: {pid}) on '{host}'? Unsaved data in this application will be lost.";
    public static string ProcessTerminatedSuccess(string processName) => $"Process '{processName}' terminated successfully.";
    public static string TerminateProcessFailed(string error) => $"Failed to terminate process: {error}";
    public static string TerminateProcessFailedNamed(string processName, uint pid) => $"Failed to terminate process '{processName}' (PID: {pid}).";
    public string FetchingProcessData => "Querying remote process list...";
    public string NoProcessesFound => "No running processes found.";
    public static string TotalProcessesCountBadge(int count) => $"{count} processes";

    // Feature 7: Remote Group Policy Refresh (GPUpdate)
    public string RemoteGpupdateBtn => "GPUpdate";
    public string RemoteGpupdateTooltip => "Trigger immediate Group Policy refresh remotely (/force /nowait)";
    public string ConfirmRemoteGpupdateTitle => "Trigger Remote GPUpdate";
    public static string ConfirmRemoteGpupdatePrompt(string host) => $"Are you sure you want to refresh Group Policy on '{host}'? This will execute 'gpupdate.exe /force /nowait' in the background on the target computer.";
    public static string RemoteGpupdateInitiated(string host) => $"Group Policy update initiated on '{host}'.";
    public static string RemoteGpupdateFailed(string host, string error) => $"Failed to trigger Group Policy update on '{host}': {error}";

    // Feature 8: BitLocker Drive Encryption & Maintenance
    public string BitLockerSectionTitle => "BitLocker & Drive Encryption";
    public string BitLockerProtectionActive => "Protection On (Active)";
    public string BitLockerProtectionSuspended => "Protection Suspended (1 Reboot)";
    public string BitLockerProtectionOff => "Protection Off";
    public string BitLockerProtectionUnknown => "Protection Unknown";
    public string BitLockerMethodLabel => "Encryption Method";
    public string BitLockerStatusLabel => "Volume Status";
    public string SuspendBitLockerBtn => "Suspend (1 Reboot)";
    public string ResumeBitLockerBtn => "Resume Protection";
    public string RefreshBitLockerBtn => "Refresh BitLocker Status";
    public string FetchingBitLockerData => "Querying remote BitLocker status...";
    public string ConfirmSuspendBitLockerTitle => "Suspend BitLocker Protection";
    public static string ConfirmSuspendBitLockerPrompt(string host, string drive) => $"Are you sure you want to suspend BitLocker protection on '{host}' (Drive {drive}) for 1 reboot? This temporarily bypasses BitLocker encryption on the next restart for firmware or BIOS maintenance.";
    public string ConfirmResumeBitLockerTitle => "Resume BitLocker Protection";
    public static string ConfirmResumeBitLockerPrompt(string host, string drive) => $"Are you sure you want to resume BitLocker protection on '{host}' (Drive {drive}) immediately?";
    public static string BitLockerSuspendedSuccess(string host) => $"BitLocker protection suspended for 1 reboot on '{host}'.";
    public static string BitLockerResumedSuccess(string host) => $"BitLocker protection resumed successfully on '{host}'.";
    public static string BitLockerActionFailed(string host, string error) => $"BitLocker operation failed on '{host}': {error}";
    public string BitLockerKeysSubtitle => "Active Directory Recovery Keys";

    // Advanced Attribute Editor (Safe Whitelist & Inspector)
    public string AdvancedEditorBtn => "Attribute Editor";
    public string BackToProfileBtn => "Back to Profile";
    public string AttributeEditorTitle => "Active Directory Attribute Editor";
    public string AttributeEditorDesc => "Inspect and safely modify schema attributes for this user.";
    public string FilterAttributesPlaceholder => "Filter attributes...";
    public string EditAttributeBtn => "Edit Value";
    public string OldValueLabel => "Current Value:";
    public string NewValueLabel => "New Value:";
    public string ConfirmAttributeChangeTitle => "Confirm Attribute Change";
    public static string ConfirmAttributeChangePrompt(string attr, string oldVal, string newVal) => $"Are you sure you want to update attribute '{attr}' from '{oldVal}' to '{newVal}'? This action will be audited.";
    public static string AttributeUpdateSuccess(string attr) => $"Attribute '{attr}' updated successfully.";
    public static string AttributeUpdateFailed(string attr, string err) => $"Failed to update '{attr}': {err}";
    public static string AttributeLabel(string key) => $"Attribute: {key}";
    public string AuditLogNotice => "All modifications to this attribute will be durably written to the security audit log.";
    public string NonEditableAttributeTooltip => "This attribute is read-only (type excluded from safe editing).";

    // Feature 9: JIRA Ticket Integration
    public string NavJiraWorkspace => "JIRA Integration";
    public string JiraWorkspaceTitle => "JIRA Integration";
    public string SearchUserForJiraPlaceholder => "Search for a user to view created JIRA tickets...";
    public string JiraTicketsSectionTitle => "Created JIRA Tickets";
    public string JiraFilterPlaceholder => "Filter by issue key or summary...";
    public string JiraKeyColumn => "Key";
    public string JiraStatusColumn => "Status";
    public string JiraSummaryColumn => "Summary";
    public string JiraPriorityColumn => "Priority";
    public string JiraCreatedColumn => "Created";
    public string LoadMoreBtn => "Load More Tickets";
    public string NoJiraTicketsForUser => "No open JIRA tickets found for this user.";
    public string FetchingJiraTickets => "Fetching JIRA tickets...";
    public string JiraFetchError => "Error fetching JIRA tickets";
    public string ViewJiraTicketsBtn => "View JIRA Tickets";
    public string ViewJiraTicketsTooltip => "View open JIRA tickets created by this user";
    public static string TotalJiraTicketsCountBadge(int count) => count == 1 
        ? "1 open ticket" 
        : $"{count} open tickets";
    public string JiraNotConfiguredPrompt => "JIRA integration is not enabled or configured in Settings.";
    public string RefreshJiraBtn => "Refresh JIRA Tickets";

    // JIRA Settings Configuration
    public string JiraSettings => "JIRA Integration";
    public string JiraIntegrationHeader => "Connect JIRA Ticket System";
    public string JiraIntegrationDesc => "Fetch and inspect open JIRA tickets created by users directly in Sol.";
    public string JiraDeploymentModeLabel => "Deployment Model";
    public string JiraDataCenterOption => "Jira Data Center / Server";
    public string JiraCloudOption => "Jira Cloud (Atlassian)";
    public string JiraBaseUrlLabel => "Base URL";
    public string JiraBaseUrlPlaceholder => "https://jira.company.com";
    public string JiraCloudEmailLabel => "Atlassian Account Email";
    public string JiraCloudEmailPlaceholder => "user@company.com";
    public string JiraPatLabel => "Personal Access Token (PAT)";
    public string JiraPatPlaceholder => "Enter Personal Access Token...";
    public string JiraApiTokenLabel => "Atlassian API Token";
    public string JiraApiTokenPlaceholder => "Enter API token...";
    public string TestJiraConnectionBtn => "Test Connection";
    public string TestingJiraConnection => "Testing JIRA connection...";
    public string JiraConnectionSuccessPrompt => "JIRA connection established successfully.";
    public string JiraConnectionFailedPrompt => "JIRA connection failed. Please verify URL and credentials.";
    public string JiraUrlRequiredPrompt => "Please enter a valid Base URL first (e.g. https://jira.company.com).";
    public string JiraSecretRequiredPrompt => "Please enter a valid access token (PAT or API token).";
    public string JiraEmailRequiredPrompt => "Please enter your Atlassian account email address.";
    public string JiraCredentialsSavedPrompt => "JIRA credentials securely saved in Windows Credential Locker.";
    public string JiraHttpsRequiredPrompt => "JIRA base URL must use secure HTTPS protocol.";
    public string JiraUnauthorizedError => "JIRA authentication failed (HTTP 401 Unauthorized). Please verify your access token or credentials.";
    public string JiraForbiddenError => "JIRA access forbidden (HTTP 403 Forbidden). Insufficient permissions to access this resource.";
    public string JiraHttpErrorFormat => "JIRA request failed with HTTP {0} ({1}).";

    // Feature 10: Remote Windows Services Inspector & Controller
    public string ServicesInspectorTitle => "Remote Services Manager";
    public string ServicesInspectorBtn => "Services";
    public string ServicesInspectorTooltip => "View and manage Windows services on remote computer";
    public string ServiceDisplayNameCol => "Display Name";
    public string ServiceNameCol => "Service Name";
    public string ServiceStatusCol => "Status";
    public string ServiceStatusRunning => "Running";
    public string ServiceStatusStopped => "Stopped";
    public string ServiceStartupTypeCol => "Startup Type";
    public string ServiceLogOnAsCol => "Log On As";
    public string ServiceActionsCol => "Actions";
    public string ServiceFilterAll => "All";
    public string ServiceFilterRunning => "Running";
    public string ServiceFilterStopped => "Stopped";
    public string SearchServicesPlaceholder => "Filter services (Name, Display Name, Account)...";
    public string FetchingServicesData => "Querying remote services list...";
    public string NoServicesFound => "No services found.";
    public string StartServiceBtn => "Start";
    public string StartServiceTooltip => "Start service";
    public string StopServiceBtn => "Stop";
    public string StopServiceTooltip => "Stop service";
    public string RestartServiceBtn => "Restart";
    public string RestartServiceTooltip => "Restart service";
    public string CriticalServiceProtected => "Critical system service (protected from stop/restart)";
    public string ServiceStartModeAuto => "Automatic";
    public string ServiceStartModeManual => "Manual";
    public string ServiceStartModeDisabled => "Disabled";
    public string ConfirmStartServiceTitle => "Start Service";
    public string ConfirmStopServiceTitle => "Stop Service";
    public string ConfirmRestartServiceTitle => "Restart Service";
    public string ConfirmChangeStartupTypeTitle => "Change Startup Type";
    public static string ConfirmStartServicePrompt(string displayName, string host) => $"Are you sure you want to start service '{displayName}' on '{host}'?";
    public static string ConfirmStopServicePrompt(string displayName, string host) => $"Are you sure you want to stop service '{displayName}' on '{host}'? Dependent applications may stop functioning properly.";
    public static string ConfirmRestartServicePrompt(string displayName, string host) => $"Are you sure you want to restart service '{displayName}' on '{host}'?";
    public static string ConfirmChangeStartupTypePrompt(string displayName, string host, string newMode) => $"Are you sure you want to change the startup type for '{displayName}' on '{host}' to '{newMode}'?";
    public static string ServiceStartedSuccess(string displayName) => $"Service '{displayName}' started successfully.";
    public static string ServiceStoppedSuccess(string displayName) => $"Service '{displayName}' stopped successfully.";
    public static string ServiceRestartedSuccess(string displayName) => $"Service '{displayName}' restarted successfully.";
    public static string ServiceStartModeChangedSuccess(string displayName, string mode) => $"Startup type for '{displayName}' changed to '{mode}'.";
    public static string ServiceActionFailed(string action, string error) => $"Service action '{action}' failed: {error}";
    public string ServiceAccessDenied => "Access denied. Administrator privileges required.";
    public string ServiceLocalElevationRequired => "Controlling local services requires running the app as Administrator (Right-click -> 'Run as administrator').";
    public string ServiceDependentServicesRunning => "Service cannot be stopped because dependent services are still running.";
    public string ServiceCannotAcceptControl => "Service cannot accept control at this time (it may be starting or stopping).";
    public string ServiceDisabled => "Service is disabled. Change startup type to Manual or Automatic first.";
    public string ServiceLogonFailed => "Service logon failed. Verify service account credentials.";
    public string ServiceAlreadyRunning => "Service is already running.";
    public string ServiceAlreadyStopped => "Service is already stopped.";
    public string ServiceRequestTimeout => "Service control request timed out.";
    public string ServiceNotSupported => "Service action is not supported by the service.";
    public string ServiceInvalidControl => "Invalid control command for this service.";
    public string ServiceInvalidParameter => "Invalid parameter for service configuration.";
    public static string TotalServicesCountBadge(int count) => $"{count} services";
    public static string RunningServicesCountBadge(int count) => $"{count} running";
    public static string StoppedServicesCountBadge(int count) => $"{count} stopped";

    // Tools Page & Navigation
    public string NavTools => "Tools";
    public string ToolsPageTitle => "Tools";
    public string ToolsPageSubtitle => "Power utilities and system diagnostic tools integrated seamlessly within Sol.";
    public string ToolFileLocksmithTitle => "File Locksmith";
    public string ToolFileLocksmithDesc => "Inspect which processes are locking specific files or directories and terminate them cleanly.";
    public string ToolShortcutGuideTitle => "Shortcut Guide";
    public string ToolShortcutGuideDesc => "Display an interactive cheat sheet of Windows shortcuts, window management, and navigation keys.";
    public string ToolRunTitle => "Sol Run";
    public string ToolRunDesc => "Instant floating launcher to search apps, settings, system commands, and evaluate math expressions.";
    public string ToolStatusActive => "Active";
    public string ToolStatusInactive => "Inactive";
    public string ToolStatusIdle => "Idle";
    public string ToolStatusReady => "Ready";
    public string ToolLaunchBtn => "Launch";
    public string ToolOpenWindowBtn => "Open Window";
    public string ToolConfigureBtn => "Configure";
    public string ToolBrowseFileBtn => "Browse File...";
    public string ToolBrowseFolderBtn => "Browse Folder...";
    public string ToolDropZonePlaceholder => "Drag and drop any file or folder here to check for locks";
    public string ToolHotKeyLabel => "Hotkey: ";

    // File Locksmith Strings
    public string FileLocksmithTitle => "File Locksmith";
    public string FileLocksmithSubtitle => "Inspect processes holding open handles to files or directories.";
    public string FileLocksmithTargetPathLabel => "Target Path";
    public string FileLocksmithTargetPathPlaceholder => "Enter or paste file or folder path...";
    public string FileLocksmithNoLocksFound => "No processes found locking this resource.";
    public static string FileLocksmithLocksFound(int count) => $"{count} locking {(count == 1 ? "process" : "processes")}";
    public string FileLocksmithEndTaskBtn => "End Task";
    public string FileLocksmithEndAllBtn => "End All Processes";
    public string FileLocksmithProcessHeader => "Process Name";
    public string FileLocksmithPidHeader => "PID";
    public string FileLocksmithUserHeader => "User";
    public string FileLocksmithPathHeader => "Application Path";
    public string FileLocksmithInspectBtn => "Inspect Locks";
    public string FileLocksmithInvalidPath => "Please specify an existing file or directory path.";
    public string FileLocksmithConfirmKillAllTitle => "End All Locking Processes";
    public string FileLocksmithConfirmKillAllMessage => "Are you sure you want to terminate all processes locking this resource? Unsaved work in these applications may be lost.";
    public string FileLocksmithTerminatedSuccess => "Process terminated successfully.";
    public static string FileLocksmithTerminateFailed(string error) => $"Failed to terminate process: {error}";
    public string FileLocksmithContextMenuRegistered => "Windows Explorer context menu registered successfully.";
    public string FileLocksmithContextMenuUnregistered => "Windows Explorer context menu removed.";
    public string FileLocksmithContextMenuEntry => "Unlock with Sol File Locksmith";

    // Shortcut Guide Strings
    public string ShortcutGuideTitle => "Shortcut Guide";
    public string ShortcutGuideSubtitle => "Windows 11 & 10 keyboard shortcut reference.";
    public string ShortcutCatSystem => "System & Navigation";
    public string ShortcutCatWindowManagement => "Window Snapping & Layouts";
    public string ShortcutCatVirtualDesktops => "Virtual Desktops & Displays";
    public string ShortcutCatToolsAndCapture => "Screen Capture & Utilities";
    public string ShortcutCatTaskbar => "Taskbar Numbered Apps";
    public string ShortcutCatGeneralEditing => "Common & Editing";
    public string ShortcutCatFileExplorerNav => "File Explorer & Navigation";
    public string ShortcutCatAppNavigation => "Browser & Tab Navigation";
    public string ShortcutGuideSearchPlaceholder => "Filter shortcuts (e.g. clipboard, snap, lock)...";
    public string ShortcutGuideOpenOverlayBtn => "Open Overlay Guide";
    public string ShortcutGuideDismissHint => "Press Esc or Win + Shift + ? to dismiss";

    // Shortcut Descriptions
    public string ShortcutDescStartMenu => "Open or close Start menu";
    public string ShortcutDescQuickSettings => "Open Quick Settings";
    public string ShortcutDescNotificationCenter => "Open Notification Center and Calendar";
    public string ShortcutDescFileExplorer => "Open File Explorer";
    public string ShortcutDescWindowsSettings => "Open Windows Settings";
    public string ShortcutDescLockPc => "Lock your PC or switch accounts";
    public string ShortcutDescDesktop => "Display and hide the desktop";
    public string ShortcutDescClipboardHistory => "Open Clipboard history";
    public string ShortcutDescQuickLink => "Open Quick Link / Power User menu";
    public string ShortcutDescTaskView => "Open Task View";
    public string ShortcutDescSnapLayouts => "Open Snap layouts menu";
    public string ShortcutDescSnapLeft => "Snap active window to the left half";
    public string ShortcutDescSnapRight => "Snap active window to the right half";
    public string ShortcutDescMaximize => "Maximize window or snap to top half";
    public string ShortcutDescMinimize => "Minimize window or restore size";
    public string ShortcutDescMoveLeftMonitor => "Move active window to the monitor on the left";
    public string ShortcutDescMoveRightMonitor => "Move active window to the monitor on the right";
    public string ShortcutDescNewVirtualDesktop => "Create a new virtual desktop";
    public string ShortcutDescSwitchVirtualDesktopLeft => "Switch to the virtual desktop on the left";
    public string ShortcutDescSwitchVirtualDesktopRight => "Switch to the virtual desktop on the right";
    public string ShortcutDescCloseVirtualDesktop => "Close the current active virtual desktop";
    public string ShortcutDescProject => "Choose a presentation display mode (Project)";
    public string ShortcutDescCast => "Open Cast quick action";
    public string ShortcutDescScreenshot => "Take a snip / screenshot of part of your screen";
    public string ShortcutDescXboxGameBar => "Open Xbox Game Bar";
    public string ShortcutDescVoiceTyping => "Launch voice typing / speech recognition";
    public string ShortcutDescWidgets => "Open Windows Widgets board";
    public string ShortcutDescEmoji => "Open Emoji and symbols picker panel";
    public string ShortcutDescTaskbarNumbered => "Open or switch to app at numbered position on taskbar";
    public string ShortcutDescTaskbarNumberedNewInstance => "Open a new instance of app at numbered position";
    public string ShortcutDescCopy => "Copy selected item or text";
    public string ShortcutDescPaste => "Paste content from clipboard";
    public string ShortcutDescCut => "Cut selected item or text";
    public string ShortcutDescUndo => "Undo an action";
    public string ShortcutDescRedo => "Redo an action";
    public string ShortcutDescSelectAll => "Select all items or text in a document or window";
    public string ShortcutDescFind => "Search or find text within document or page";
    public string ShortcutDescSave => "Save current file or document";
    public string ShortcutDescTaskManager => "Open Task Manager directly";
    public string ShortcutDescSwitchApps => "Switch between open apps";
    public string ShortcutDescCloseApp => "Close the active app or window";
    public string ShortcutDescCycleApps => "Cycle through open windows in the order opened";
    public string ShortcutDescRename => "Rename selected item or folder";
    public string ShortcutDescRefresh => "Refresh active window or web page";
    public string ShortcutDescNewFolder => "Create a new folder";
    public string ShortcutDescProperties => "Display properties for selected item";
    public string ShortcutDescBack => "Go back to previous folder or page";
    public string ShortcutDescForward => "Go forward to next folder or page";
    public string ShortcutDescUpFolder => "View the parent folder up one level";
    public string ShortcutDescCloseTab => "Close active tab or window";
    public string ShortcutDescDeletePermanent => "Delete selected item permanently without Recycle Bin";
    public string ShortcutDescNewTab => "Open a new tab";
    public string ShortcutDescNewWindow => "Open a new window";
    public string ShortcutDescReopenTab => "Reopen the last closed tab";
    public string ShortcutDescAddressBar => "Select the address or URL bar";
    public string ShortcutDescNextTab => "Switch to next tab";
    public string ShortcutDescPrevTab => "Switch to previous tab";

    // Sol Run Strings
    public string RunLauncherTitle => "Sol Run";
    public string RunLauncherPlaceholder => "Type a command, app, setting, or math (e.g. calc, display, 125*4)...";
    public string RunCategoryApp => "Application";
    public string RunCategorySetting => "Windows Setting";
    public string RunCategorySystem => "System Command";
    public string RunCategoryCalculator => "Calculation";
    public string RunCategoryShell => "Run Command";
    public string RunCategoryWeb => "Web Address";
    public string RunCategoryPath => "Folder / File";
    public string RunActionLaunch => "Launch application";
    public string RunActionOpen => "Open setting";
    public string RunActionExecute => "Execute command";
    public string RunActionRunShell => "Execute command with Windows Shell";
    public string RunActionOpenWeb => "Open URL in default web browser";
    public string RunActionOpenPath => "Open location in File Explorer";
    public string RunActionCopy => "Copy result to clipboard";
    public string RunNoResults => "No matching commands or applications found.";
    public string RunCopySuccess => "Calculation result copied to clipboard.";
    public string RunShellCmdAction => "Run in Command Prompt (keeps window open)";
    public string RunShellPowershellAction => "Run in Windows PowerShell";
    public string RunShellAdminAction => "Run as Administrator";
    public string RunShellHint => "Type a command to run (e.g. > ipconfig, > ping 8.8.8.8, > dir)";
    public string RunCalculatorHint => "Type an expression to calculate (e.g. = 25 * 4, = sqrt(144))";
    public string RunWebSearchAction => "Search the web";

    // Sol Run System Commands
    public string SysCmdLockTitle => "Lock Workstation";
    public string SysCmdLockDesc => "Lock your computer immediately";
    public string SysCmdRestartTitle => "Restart Computer";
    public string SysCmdRestartDesc => "Reboot Windows immediately";
    public string SysCmdShutdownTitle => "Shut Down Computer";
    public string SysCmdShutdownDesc => "Power off Windows immediately";
    public string SysCmdSleepTitle => "Sleep";
    public string SysCmdSleepDesc => "Put your computer to sleep";
    public string SysCmdSignOutTitle => "Sign Out";
    public string SysCmdSignOutDesc => "Sign out of your current Windows session";
    public string SysCmdEmptyRecycleBinTitle => "Empty Recycle Bin";
    public string SysCmdEmptyRecycleBinDesc => "Permanently delete files in Recycle Bin";

    // Sol Run Windows Settings
    public string SettingDisplayTitle => "Display Settings";
    public string SettingSoundTitle => "Sound Settings";
    public string SettingNetworkTitle => "Network & Internet";
    public string SettingBluetoothTitle => "Bluetooth & Devices";
    public string SettingInstalledAppsTitle => "Installed Apps";
    public string SettingWindowsUpdateTitle => "Windows Update";
    public string SettingTaskbarTitle => "Taskbar Settings";
    public string SettingPowerBatteryTitle => "Power & Battery";
    public string SettingStorageTitle => "Storage & Disks";
    public string SettingDateTimeTitle => "Date & Time";
    public string SettingDefaultAppsTitle => "Default Apps";

    // Sol Run Built-in Applications
    public string AppNotepad => "Notepad";
    public string AppCalculator => "Calculator";
    public string AppTaskManager => "Task Manager";
    public string AppCommandPrompt => "Command Prompt";
    public string AppPowerShell => "PowerShell";
    public string AppRemoteDesktop => "Remote Desktop Connection";
    public string AppRegistryEditor => "Registry Editor";
    public string AppEventViewer => "Event Viewer";
    public string AppDeviceManager => "Device Manager";
    public string AppServices => "Services";
    public string AppSnippingTool => "Snipping Tool";
    public string AppWindowsTerminal => "Windows Terminal";
    public string AppEdge => "Microsoft Edge";

    // SettingsPage
    public string SettingsTitle => "Settings";
    public string AdSettings => "Active Directory";
    public string DomainNameLabel => "Domain Name";
    public string DomainNamePlaceholder => "e.g. contoso.local";
    
    public string SaveSettingsBtn => "Save Settings";
    public string TestAdBtn => "Test AD Connection";
    public string SettingsSavedPrompt => "Settings saved successfully.";
    public string SettingsSaveErrorPrompt => "Error saving settings or credentials.";
    public string SettingsSavedCredentialFailed => "Settings saved, but credential storage failed";
    public string TestingConnection => "Testing connection...";

    public string AboutSettings => "About";
    public string AppName => "Sol";
    public string PipeSeparator => "|";
    public string VersionLabel => "Version";
    public string DeveloperLabel => "Developer";
    public string GitHubProfileLabel => "GitHub (@mm-dev-alpha)";

    // Tools Configuration Settings
    public string ToolsSettingsSection => "Tools Configuration";

    public string ToolsSettingsRunHeader => "Sol Run Launcher";
    public string ToolsSettingsRunDesc => "Quick application launcher, calculator, and system command runner (Alt + Space).";
    public string ToolsSettingsRunEnableLabel => "Enable Sol Run launcher";
    public string ToolsSettingsRunHotkeyCardHeader => "Global Shortcut";
    public string ToolsSettingsRunHotkeyCardDesc => "Alt + Space";

    public string ToolsSettingsShortcutGuideHeader => "Shortcut Guide";
    public string ToolsSettingsShortcutGuideDesc => "On-screen cheat sheet for Windows shortcuts overlay (Win + Shift + ?).";
    public string ToolsSettingsShortcutGuideEnableLabel => "Enable Shortcut Guide global hotkey";
    public string ToolsSettingsShortcutGuideHotkeyCardHeader => "Global Shortcut";
    public string ToolsSettingsShortcutGuideHotkeyCardDesc => "Win + Shift + ?";

    public string ToolsSettingsFileLocksmithHeader => "File Locksmith Shell Integration";
    public string ToolsSettingsFileLocksmithDesc => "Integrate File Locksmith into the Windows Explorer context menu.";
    public string ToolsSettingsFileLocksmithToggleLabel => "Add 'Unlock with Sol File Locksmith' to Explorer context menu";

    public string ToolsSettingsMmcHeader => "MMC Administrative Consoles";
    public string ToolsSettingsMmcDesc => "Instant access to Microsoft management consoles, applets, and admin utilities.";
    public string ToolsSettingsMmcEnableLabel => "Enable MMC Consoles global hotkey";
    public string ToolsSettingsMmcHotkeyCardHeader => "Global Shortcut";
    public string ToolsSettingsMmcHotkeyCardDesc => "Alt + Space";

    public string ToolsSettingsAdminCommandsHeader => "Daily Admin Commands";
    public string ToolsSettingsAdminCommandsDesc => "Fast searchable catalog and 1-click clipboard copying for administrative PowerShell and CMD commands.";
    public string ToolsSettingsAdminCommandsEnableLabel => "Enable Daily Admin Commands global hotkey";
    public string ToolsSettingsAdminCommandsHotkeyCardHeader => "Global Shortcut";
    public string ToolsSettingsAdminCommandsHotkeyCardDesc => "Win + Shift + C";

    // Text Grab / OCR
    public string ToolTextGrabTitle => "Text Grab";
    public string ToolTextGrabDesc => "Extract text, tables, and data from images, documents, and screen regions using on-device OCR.";
    public string OcrLanguageLabel => "OCR Language";
    public string OcrEngineLabel => "OCR Engine";
    public string OcrResultCopied => "OCR result copied to clipboard.";
    public string OcrNoTextFound => "No text was recognized in the selected area.";
    public string OcrProcessing => "Running OCR...";
    public string OcrWinAiUnavailable => "Windows AI recognition is not available on this device.";
    public string OcrEngineWindows => "Windows Runtime OCR";
    public string OcrEngineWindowsAi => "Windows AI (NPU)";
    public static string OcrEngineInitError(string lang) => $"No Windows OCR engine could be initialized for language '{lang}'.";

    // MMC Administrative Consoles (MMC)
    public string ToolMmcTitle => "MMC Administrative Consoles";
    public string ToolMmcDesc => "Instant searchable access to 40+ Microsoft management consoles (.msc), control panel applets (.cpl), and admin tools with pinned favorites.";
    public string ToolMmcHotkey => "Alt + Space";
    public string MmcWindowLauncherTitle => "MMC Administrative Consoles";
    public string MmcSearchPlaceholder => "Search consoles, commands, applets (or type to filter)...";
    public string MmcNoResults => "No matching administrative consoles found.";
    public string MmcLaunchButton => "Launch";
    public string MmcCopyCommandButton => "Copy Command";
    public string MmcCopiedCommand => "Command copied to clipboard.";
    public string MmcHintEnter => "Enter: Launch";
    public string MmcHintCtrlC => "Ctrl+C: Copy";
    public string MmcHintEsc => "Esc: Close";
    public string MmcItemCountFormat => "{0} tools";
    public string MmcCategoryAll => "All";
    public string MmcCategoryActiveDirectory => "Active Directory";
    public string MmcCategoryManagement => "Management";
    public string MmcCategoryDiagnostics => "Diagnostics";
    public string MmcCategorySecurity => "Security";
    public string MmcCategoryNetworking => "Networking";
    public string MmcCategoryStorage => "Storage";
    public string MmcCategorySystem => "System";

    // Daily Admin Commands (ADC)
    public string ToolAdminCommandsTitle => "Daily Admin Commands";
    public string ToolAdminCommandsDesc => "Curated catalog of 50+ essential administrative commands across Active Directory, Networking, Group Policy, Diagnostics, and Security.";
    public string ToolAdminCommandsHotkey => "Win + Shift + C";
    public string AdminCommandsWindowLauncherTitle => "Daily Admin Commands";
    public string AdminCommandsSearchPlaceholder => "Search commands, tasks, utilities (e.g. repadmin, flushdns, sfc)...";
    public string AdminCommandsNoResults => "No matching admin commands found.";
    public string AdminCommandsCopyButton => "Copy";
    public string AdminCommandsCopied => "Copied command to clipboard.";
    public string AdminCommandsHintEnter => "Enter: Copy";
    public string AdminCommandsHintEsc => "Esc: Close";
    public string AdminCommandsItemCountFormat => "{0} commands";
    public string AdminCommandsFilterAllShells => "All Shells";
    public string AdminCommandsFilterPowerShell => "PowerShell";
    public string AdminCommandsFilterCmd => "CMD";
    public string AdminCommandsCategoryAll => "All Categories";
    public string AdminCommandsCategoryActiveDirectory => "Active Directory";
    public string AdminCommandsCategoryNetworking => "Networking";
    public string AdminCommandsCategoryGroupPolicy => "Group Policy";
    public string AdminCommandsCategoryDiagnostics => "Diagnostics";
    public string AdminCommandsCategorySecurity => "Security";
    public string AdminCommandsCategoryRemoteManagement => "Remote";
    public string AdminCommandsCategoryStorage => "Storage";

    // Grab Frame (GF)
    public string ToolGrabFrameTitle => "Grab Frame";
    public string ToolGrabFrameDesc => "Floating viewfinder window to capture, search, and extract text or tables from any desktop area.";
    public string GrabFrameHotkey => "Win + Shift + G";
    public string GrabFrameAutoOcrToggle => "Auto Start OCR";
    public string GrabFrameAutoOcrTooltip => "Automatically recognizes text when moving or resizing the frame.";
    public string GrabFrameAlwaysOnTopToggle => "Keep on Top";
    public string GrabFrameAlwaysOnTopTooltip => "Keeps the Grab Frame viewfinder floating above other windows.";
    public string GrabFrameFreezeToggle => "Freeze View (F)";
    public string GrabFrameFreezeTooltip => "Freezes the image behind the viewfinder.";
    public string GrabFrameTableToggle => "Table Mode (T)";
    public string GrabFrameTableTooltip => "Enables draggable column dividers to extract tab-delimited tables.";
    public string GrabFrameSingleLineToggle => "Single Line (S)";
    public string GrabFrameSingleLineTooltip => "Extract text as a single continuous line.";
    public string GrabFrameRefreshButton => "Re-OCR Frame (Ctrl + R)";
    public string GrabFrameRefreshTooltip => "Re-runs OCR on the current viewfinder area.";
    public string GrabFrameGrabButton => "Grab";
    public string GrabFrameGrabTooltip => "Grab text from frame and copy to clipboard (Ctrl + G / Enter).";
    public string GrabFrameSearchPlaceholder => "Search text in frame...";
    public string GrabFrameMatchesFormat => "{0} matches";
    public string GrabFrameNoMatches => "0 matches";
    public string GrabFrameStatusRecognizing => "Recognizing text in frame...";
    public string GrabFrameStatusCopied => "Copied to clipboard!";
    public string GrabFrameStatusNoText => "No text found in viewfinder.";
    public string GrabFrameCloseTooltip => "Close Grab Frame (Alt + F4)";
    public string GrabFrameAddDividerHint => "Click viewfinder to add column divider. Drag to reposition.";
    public string GrabFrameSettingsHeader => "Grab Frame";
    public string GrabFrameSettingsDescription => "Configure floating viewfinder OCR behaviors, auto-start, and table defaults.";
    public string GrabFrameSettingsAutoOcr => "Auto Start OCR on Move / Resize";
    public string GrabFrameSettingsAutoOcrDesc => "Automatically recognizes text in the viewfinder after moving or resizing.";
    public string GrabFrameSettingsAlwaysOnTop => "Keep Grab Frame On Top";
    public string GrabFrameSettingsAlwaysOnTopDesc => "Keeps the Grab Frame window topmost above other desktop applications.";
    public string GrabFrameSettingsSingleLine => "Default to Single-Line";
    public string GrabFrameSettingsSingleLineDesc => "Strips linebreaks and extracts text on a single line by default.";
    public string GrabFrameSettingsTableMode => "Default to Table Mode";
    public string GrabFrameSettingsTableModeDesc => "Enables table extraction with column dividers by default.";
    public string GrabFrameSettingsAutoPaste => "Auto-Paste into Previous App";
    public string GrabFrameSettingsAutoPasteDesc => "Automatically simulates Ctrl+V paste into the active window after grabbing.";

    // Edit Text Window (ETW)
    public string ToolEditTextTitle => "Edit Text Window";
    public string ToolEditTextDesc => "In-place text manipulation workbench with structured table formatting, calculation pane, and OCR clipboard integration.";
    public string EditTextHotkey => "Win + Shift + E";
    public string EditTextWindowTitle => "Edit Text";
    public string EditTextMenuFile => "File";
    public string EditTextMenuEdit => "Edit";
    public string EditTextMenuTransform => "Transform";
    public string EditTextMenuView => "View";
    public string EditTextMenuNew => "New";
    public string EditTextMenuOpen => "Open File...";
    public string EditTextMenuSave => "Save";
    public string EditTextMenuSaveAs => "Save As...";
    public string EditTextMenuCopyClose => "Copy & Close";
    public string EditTextMenuCloseInsert => "Close & Insert";
    public string EditTextMenuClose => "Close";
    public string EditTextCloseTooltip => "Close";
    public string EditTextMenuUndo => "Undo";
    public string EditTextMenuRedo => "Redo";
    public string EditTextMenuCut => "Cut";
    public string EditTextMenuCopy => "Copy";
    public string EditTextMenuPaste => "Paste";
    public string EditTextMenuOcrPaste => "OCR Paste";
    public string EditTextMenuSelectAll => "Select All";
    public string EditTextMenuClear => "Clear";
    public string EditTextTransformSingleLine => "Make Single Line";
    public string EditTextTransformTrim => "Trim Each Line";
    public string EditTextTransformNumbers => "Try To Make Numbers";
    public string EditTextTransformLetters => "Try To Make Letters";
    public string EditTextTransformFixGuid => "Fix GUID Format";
    public string EditTextTransformToggleCase => "Toggle Case";
    public string EditTextTransformRemoveDuplicates => "Remove Duplicate Lines";
    public string EditTextTransformShuffle => "Shuffle Lines";
    public string EditTextTransformReplaceReserved => "Replace Reserved Characters";
    public string EditTextTransformUnstack => "Unstack Lines";
    public string EditTextTransformSortAsc => "Sort Lines (A-Z)";
    public string EditTextTransformSortDesc => "Sort Lines (Z-A)";
    public string EditTextTransformExtractEmails => "Extract Email Addresses";
    public string EditTextTransformExtractUrls => "Extract URLs";
    public string EditTextTransformExtractNumbers => "Extract Numbers";
    public string EditTextModeText => "Text Mode";
    public string EditTextModeTable => "Table Mode";
    public string EditTextToggleCalcPane => "Calculation Pane (Ctrl + P)";
    public string EditTextToggleWordWrap => "Word Wrap";
    public string EditTextAlwaysOnTop => "Keep on Top";
    public string EditTextTableTranspose => "Transpose Table";
    public string EditTextTableCopyTsv => "Copy as TSV";
    public string EditTextTableCopyCsv => "Copy as CSV";
    public string EditTextTableCopyMarkdown => "Copy as Markdown Table";
    public string EditTextTableAddRow => "Add Row";
    public string EditTextTableAddColumn => "Add Column";
    public string EditTextTableDeleteRow => "Delete Row";
    public string EditTextTableDeleteColumn => "Delete Column";
    public string EditTextStatusStatsFormat => "Lines: {0} | Words: {1} | Chars: {2}";
    public string EditTextStatusCalcSummaryFormat => "Sum: {0} | Avg: {1} | Calculated: {2}";
    public string EditTextStatusCopied => "Copied to clipboard!";
    public string EditTextStatusSaved => "File saved successfully.";
    public string EditTextStatusOpened => "File opened successfully.";
    public string EditTextStatusOcrEmpty => "No text recognized in clipboard image.";
    public string EditTextStatusOcrError => "OCR error while reading clipboard.";
    public string EditTextStatusOcrSuccess => "OCR text inserted.";
    public string EditTextSettingsHeader => "Edit Text Window";
    public string EditTextSettingsDescription => "Configure default text wrapping, always-on-top behavior, and global hotkeys for the text editor.";
    public string EditTextSettingsHotkeyToggle => "Enable Edit Text Window global hotkey";
    public string EditTextSettingsWordWrap => "Word Wrap by Default";
    public string EditTextSettingsWordWrapDesc => "Wraps long lines to fit within the editor view.";
    public string EditTextSettingsAlwaysOnTop => "Keep on Top by Default";
    public string EditTextSettingsAlwaysOnTopDesc => "Keeps the Edit Text Window floating above other windows when opened.";

    // Object Comparison Workspace
    public string NavCompareWorkspace => "Compare";
    public string CompareTitle => "Object Comparison";
    public string CompareSubtitle => "Analyze permissions, attributes, and group policy discrepancies side-by-side.";
    public string CompareModeUsers => "Users";
    public string CompareModeComputers => "Computers";
    public string CompareSearchPlaceholderA => "Search target A (SAM, Name, UPN)...";
    public string CompareSearchPlaceholderB => "Search target B (SAM, Name, UPN)...";
    public string CompareSwapTooltip => "Swap Side A and Side B";
    public string CompareClearBtn => "Clear";
    public string CompareCopyReport => "Copy Report";
    public string CompareReportCopied => "Comparison report copied to clipboard.";
    public string CompareShowDiffsOnly => "Show differences only";
    public string CompareDiffCountBadge => "{0} Differences";
    public string CompareIdenticalBadge => "Identical";
    public string CompareGroupsTitle => "Group Membership Comparison";
    public string CompareGroupFilterAll => "All ({0})";
    public string CompareGroupFilterDiffs => "Differences ({0})";
    public string CompareGroupOnlyA => "{0} Only ({1})";
    public string CompareGroupShared => "Shared ({0})";
    public string CompareGroupOnlyB => "{0} Only ({1})";
    public string CompareGroupSearchPlaceholder => "Filter groups...";
    public string CompareOuDivergenceTitle => "Organizational Unit Divergence";
    public string CompareOuDivergenceDesc => "Objects belong to different OUs. Different Group Policies (GPOs) and access baselines apply.";
    public string CompareAccountStatusDivergenceTitle => "Account Status Discrepancy";
    public string CompareBitLockerDivergenceTitle => "BitLocker Protection Discrepancy";
    public string CompareBitLockerDivergenceDesc => "BitLocker recovery key backup status differs between computer accounts in Active Directory.";
    public string CompareOsDivergenceTitle => "Operating System Disparity";
    public string CompareWithAction => "Compare with...";
    public string CompareEmptyStateTitle => "Select Two Objects to Compare";
    public string CompareEmptyStateSubtitle => "Search and select two users or computers above to inspect discrepancies.";
    public string CompareTargetALabel => "Target A";
    public string CompareTargetBLabel => "Target B";
    public string CompareInsightsTitle => "Smart Discrepancy Insights";
    public string ComparePropertiesTitle => "Attribute Comparison Matrix";
    public string ComparePropertiesSearchPlaceholder => "Filter properties...";
    public string CompareGroupFilterAllItem => "All";
    public string CompareGroupFilterDiffsItem => "Differences Only";
    public string CompareGroupFilterOnlyAItem => "Target A Only";
    public string CompareGroupFilterSharedItem => "Shared";
    public string CompareGroupFilterOnlyBItem => "Target B Only";
    public string CompareSwapTargetsBtn => "Swap Targets";
    public string CompareGroupInBothBadge => "In Both";
    public string CompareGroupOnlyABadge => "{0} Only";
    public string CompareGroupOnlyBBadge => "{0} Only";
    public string CompareGuidanceBothPrompt => "Search and select two accounts above to inspect group memberships and attribute differences side-by-side.";
    public string CompareGuidanceSecondPrompt => "Select a second object above to compare against {0}.";
    public string CompareCategoryIdentity => "Identity & Network";
    public string CompareCategoryOrganization => "Organization";
    public string CompareCategoryAccount => "Account Status & Security";
    public string CompareCategoryActivity => "Activity & Metadata";
    public string CompareCategorySystem => "System & Hardware";
    public string CompareRemoveTargetTooltip => "Remove target";
    public string CompareTargetPlaceholderUserA => "Search first user account (SAM, Name, UPN)...";
    public string CompareTargetPlaceholderUserB => "Search second user account (SAM, Name, UPN)...";
    public string CompareTargetPlaceholderComputerA => "Search first computer account...";
    public string CompareTargetPlaceholderComputerB => "Search second computer account...";
    public string CompareNoInsightsTitle => "No Configuration Warnings";
    public string CompareNoInsightsDesc => "Both objects share compatible organizational units, account statuses, and baseline configurations.";
    public string CompareDifferentBadge => "Divergent";
    public string CompareEqualBadge => "Matching";
    public string CompareCopyValueTooltip => "Copy value";

    public static string[] AllGreetings => new[] 
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
