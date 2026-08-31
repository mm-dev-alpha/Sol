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
    public string JiraCloudEmailLabel => "Atlassian Account Email";
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

    // SettingsPage
    public string SettingsTitle => "Settings";
    public string AdSettings => "Active Directory";
    public string DomainNameLabel => "Domain Name";
    
    public string SaveSettingsBtn => "Save Settings";
    public string TestAdBtn => "Test AD Connection";
    public string SettingsSavedPrompt => "Settings saved successfully.";
    public string SettingsSaveErrorPrompt => "Error saving settings or credentials.";
    public string TestingConnection => "Testing connection...";

    public string AboutSettings => "About";
    public string VersionLabel => "Version";
    public string DeveloperLabel => "Developer";
    public string GitHubProfileLabel => "GitHub (@mm-dev-alpha)";

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
