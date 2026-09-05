using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Sol.Models;

namespace Sol.Services;

public class AdminCommandService : IAdminCommandService
{
    private readonly HashSet<string> _favoriteIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _favoritesFilePath;
    private readonly object _lock = new();

    public event EventHandler? FavoritesChanged;
    public event EventHandler? OpenRequested;

    private static readonly List<AdminCommandItem> _commandCatalog = new()
    {
        // Active Directory
        new() { Id = "ad-repl-status", Title = "Check AD Replication Status", Command = "repadmin /showrepl", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.ActiveDirectory, Description = "Check Active Directory inbound and outbound replication status across all directory partitions." },
        new() { Id = "ad-repl-syncall", Title = "Force AD Replication Sync", Command = "repadmin /syncall /AdeP", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.ActiveDirectory, Description = "Force full Active Directory synchronization across all partitions and domain controllers." },
        new() { Id = "ad-users-lastlogon", Title = "List Users by Last Logon", Command = "Get-ADUser -Filter * -Properties LastLogonDate | Select-Object Name, LastLogonDate", ShellType = AdminShellType.PowerShell, Category = AdminCommandCategory.ActiveDirectory, Description = "Query all domain users and their last authenticated logon timestamp." },
        new() { Id = "ad-passwords-expired", Title = "Find Active Users Password Age", Command = "Get-ADUser -Filter {Enabled -eq $true} -Properties PasswordLastSet | Select-Object Name, PasswordLastSet", ShellType = AdminShellType.PowerShell, Category = AdminCommandCategory.ActiveDirectory, Description = "Inspect when active user passwords were last changed to identify aging credentials." },
        new() { Id = "ad-locked-accounts", Title = "Find Locked AD Accounts", Command = "Search-ADAccount -LockedOut | Select-Object Name, SamAccountName", ShellType = AdminShellType.PowerShell, Category = AdminCommandCategory.ActiveDirectory, Description = "Quickly locate all user accounts currently locked out by account lockout policies." },
        new() { Id = "ad-unlock-account", Title = "Unlock User Account", Command = "Unlock-ADAccount -Identity \"<username>\"", ShellType = AdminShellType.PowerShell, Category = AdminCommandCategory.ActiveDirectory, Description = "Clear account lockout status for an Active Directory user." },
        new() { Id = "ad-list-dcs", Title = "List All Domain Controllers", Command = "Get-ADDomainController -Filter * | Select-Object Name, IPv4Address, OperatingSystem", ShellType = AdminShellType.PowerShell, Category = AdminCommandCategory.ActiveDirectory, Description = "Enumerate all domain controllers in the current domain with IP addresses and OS versions." },
        new() { Id = "ad-fsmo-roles", Title = "Query FSMO Role Holders", Command = "netdom query fsmo", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.ActiveDirectory, Description = "Identify servers holding the Schema Master, Domain Naming Master, PDC, RID, and Infrastructure roles." },
        new() { Id = "ad-disabled-users", Title = "Find Disabled Domain Users", Command = "dsquery user -disabled", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.ActiveDirectory, Description = "Query all user accounts in the directory that are marked as disabled." },
        new() { Id = "ad-find-dc", Title = "Discover Domain Controller", Command = "nltest /dsgetdc:", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.ActiveDirectory, Description = "Discover the currently assigned logon domain controller and site location." },
        new() { Id = "ad-verify-trust", Title = "Verify Domain Secure Channel", Command = "nltest /sc_query:", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.ActiveDirectory, Description = "Check the security channel trust relationship between the workstation and domain controller." },

        // Networking
        new() { Id = "net-flush-dns", Title = "Flush DNS Resolver Cache", Command = "ipconfig /flushdns", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.Networking, Description = "Flush and reset the contents of the DNS client resolver cache." },
        new() { Id = "net-ipconfig-all", Title = "Display Detailed IP Configuration", Command = "ipconfig /all", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.Networking, Description = "Display full TCP/IP configuration for all network adapters and DHCP servers." },
        new() { Id = "net-dhcp-renew", Title = "Release and Renew DHCP Lease", Command = "ipconfig /release && ipconfig /renew", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.Networking, Description = "Release existing IP address assignments and request new leases from the DHCP server." },
        new() { Id = "net-test-port", Title = "Test Remote TCP Port Connectivity", Command = "Test-NetConnection -ComputerName \"<hostname>\" -Port 443", ShellType = AdminShellType.PowerShell, Category = AdminCommandCategory.Networking, Description = "Test TCP socket connectivity to a specified hostname or IP and port." },
        new() { Id = "net-resolve-dns", Title = "Resolve DNS Record", Command = "Resolve-DnsName -Name \"<hostname>\" -Type A", ShellType = AdminShellType.PowerShell, Category = AdminCommandCategory.Networking, Description = "Perform detailed DNS resolution queries directly via PowerShell." },
        new() { Id = "net-listening-ports", Title = "List Listening Ports and PIDs", Command = "netstat -ano | findstr LISTENING", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.Networking, Description = "Display all active listening TCP ports with owning process identifiers (PIDs)." },
        new() { Id = "net-tcp-connections", Title = "Query Active Listening TCP Endpoints", Command = "Get-NetTCPConnection -State Listen | Select-Object LocalAddress, LocalPort, OwningProcess", ShellType = AdminShellType.PowerShell, Category = AdminCommandCategory.Networking, Description = "Inspect listening TCP sockets, bind addresses, and process owners." },
        new() { Id = "net-routing-table", Title = "Print IP Routing Table", Command = "route print", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.Networking, Description = "Display network interface lists and active IPv4/IPv6 routing tables." },
        new() { Id = "net-arp-table", Title = "Display ARP Resolution Table", Command = "arp -a", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.Networking, Description = "Display current Address Resolution Protocol (ARP) host cache entries." },
        new() { Id = "net-ping-stats", Title = "Ping Remote Host with Statistics", Command = "Test-Connection -TargetName \"<host>\" -Count 4", ShellType = AdminShellType.PowerShell, Category = AdminCommandCategory.Networking, Description = "Ping remote target and return structured ICMP latency round-trip measurements." },
        new() { Id = "net-adapter-info", Title = "Inspect Network Adapters and Link Speed", Command = "Get-NetAdapter | Select-Object Name, Status, LinkSpeed, MacAddress", ShellType = AdminShellType.PowerShell, Category = AdminCommandCategory.Networking, Description = "Inspect all physical and virtual network adapters, interface speed, and MAC addresses." },

        // Group Policy
        new() { Id = "gp-force-update", Title = "Force Group Policy Update", Command = "gpupdate /force", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.GroupPolicy, Description = "Force immediate reapplication of all computer and user Group Policy settings." },
        new() { Id = "gp-result-summary", Title = "Display Applied Group Policy Summary", Command = "gpresult /r", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.GroupPolicy, Description = "Display Resultant Set of Policy (RSoP) summary and applied security group memberships." },
        new() { Id = "gp-result-html", Title = "Generate Complete HTML GPO Report", Command = "gpresult /h C:\\temp\\gpreport.html", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.GroupPolicy, Description = "Export full graphical HTML diagnostic report detailing every applied policy setting." },
        new() { Id = "gp-all-reports", Title = "Export All Domain GPOs to HTML", Command = "Get-GPOReport -All -ReportType Html -Path C:\\temp\\DomainGPOs.html", ShellType = AdminShellType.PowerShell, Category = AdminCommandCategory.GroupPolicy, Description = "Export comprehensive XML/HTML audit report of all Group Policy Objects in the domain." },
        new() { Id = "gp-rsop-report", Title = "Export RSOP Resultant Set of Policy", Command = "Get-GPResultantSetOfPolicy -ReportType Html -Path C:\\temp\\RSOP.html", ShellType = AdminShellType.PowerShell, Category = AdminCommandCategory.GroupPolicy, Description = "Generate HTML policy audit report for the current user and computer session." },

        // Diagnostics
        new() { Id = "sys-sfc-scan", Title = "System File Checker Integrity Scan", Command = "sfc /scannow", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.Diagnostics, Description = "Scan the integrity of all protected system files and repair corrupted versions." },
        new() { Id = "sys-dism-repair", Title = "Repair Windows Image Component Store", Command = "DISM /Online /Cleanup-Image /RestoreHealth", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.Diagnostics, Description = "Scan and repair the Windows Component Store against Windows Update or install media." },
        new() { Id = "sys-recent-events", Title = "Query Recent System Event Log Errors", Command = "Get-WinEvent -FilterHashtable @{LogName='System'; Level=1,2} -MaxEvents 30 | Select-Object TimeCreated, Id, Message", ShellType = AdminShellType.PowerShell, Category = AdminCommandCategory.Diagnostics, Description = "Filter recent critical and error events from the Windows System event log." },
        new() { Id = "sys-stopped-auto-services", Title = "Identify Failed Automatic Services", Command = "Get-Service | Where-Object {$_.Status -eq 'Stopped' -and $_.StartType -eq 'Automatic'} | Select-Object Name, DisplayName", ShellType = AdminShellType.PowerShell, Category = AdminCommandCategory.Diagnostics, Description = "Find background services configured for automatic start that are currently not running." },
        new() { Id = "sys-restart-service", Title = "Force Restart Windows Service", Command = "Restart-Service -Name \"<service_name>\" -Force", ShellType = AdminShellType.PowerShell, Category = AdminCommandCategory.Diagnostics, Description = "Stop and immediately restart a Windows service, including dependent services." },
        new() { Id = "sys-top-cpu", Title = "Top 10 CPU Consuming Processes", Command = "Get-Process | Sort-Object CPU -Descending | Select-Object -First 10 Id, ProcessName, CPU, WorkingSet", ShellType = AdminShellType.PowerShell, Category = AdminCommandCategory.Diagnostics, Description = "List processes consuming the most total processor time." },
        new() { Id = "sys-top-memory", Title = "Top 10 Memory Consuming Processes", Command = "Get-Process | Sort-Object WorkingSet -Descending | Select-Object -First 10 Id, ProcessName, WorkingSet", ShellType = AdminShellType.PowerShell, Category = AdminCommandCategory.Diagnostics, Description = "List processes consuming the largest physical memory working sets." },
        new() { Id = "sys-systeminfo", Title = "Display Full System Configuration", Command = "systeminfo", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.Diagnostics, Description = "Display detailed configuration information about the computer, OS, hotfixes, and BIOS." },
        new() { Id = "sys-tasklist-users", Title = "List Processes with User Accounts", Command = "tasklist /v", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.Diagnostics, Description = "List active running processes with user context, session ID, and CPU execution time." },
        new() { Id = "sys-driver-query", Title = "List Installed Device Drivers", Command = "driverquery /v", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.Diagnostics, Description = "List installed hardware device drivers, driver types, and memory addresses." },
        new() { Id = "sys-battery-report", Title = "Generate Battery Health Report", Command = "powercfg /batteryreport", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.Diagnostics, Description = "Create a detailed HTML report analyzing battery discharge history and wear capacity." },

        // Security
        new() { Id = "sec-domain-user-info", Title = "Query Domain User Account Properties", Command = "net user <username> /domain", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.Security, Description = "View password expiry, logon hours, workstation limits, and domain group memberships." },
        new() { Id = "sec-local-admins", Title = "List Local Administrators Group", Command = "net localgroup administrators", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.Security, Description = "Display accounts with administrative rights on the local computer." },
        new() { Id = "sec-get-local-admins", Title = "Query Local Admin Members (PowerShell)", Command = "Get-LocalGroupMember -Group \"Administrators\" | Select-Object Name, PrincipalSource, ObjectClass", ShellType = AdminShellType.PowerShell, Category = AdminCommandCategory.Security, Description = "Enumerate local administrators with principal type (Local, Active Directory, Azure AD)." },
        new() { Id = "sec-whoami-all", Title = "Display User SID, Groups, and Privileges", Command = "whoami /all", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.Security, Description = "Display user name, security identifier (SID), assigned security groups, and token privileges." },
        new() { Id = "sec-klist-purge", Title = "Purge Kerberos Ticket Cache", Command = "klist purge", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.Security, Description = "Purge all cached Kerberos ticket-granting tickets (TGT) and service tickets." },
        new() { Id = "sec-cert-verify", Title = "Verify Certificate Chain and CRL Revocation", Command = "certutil -verify -urlfetch \"<certificate.cer>\"", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.Security, Description = "Verify X.509 certificate chain, AIA certificate discovery, and CRL revocation status." },
        new() { Id = "sec-tls-ciphers", Title = "List Enabled TLS Cipher Suites", Command = "Get-TlsCipherSuite | Select-Object Name", ShellType = AdminShellType.PowerShell, Category = AdminCommandCategory.Security, Description = "List all enabled SSL/TLS cryptographic cipher suites configured in the Schannel provider." },
        new() { Id = "sec-bitlocker-status", Title = "Check BitLocker Volume Encryption Status", Command = "manage-bde -status", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.Security, Description = "Check BitLocker drive encryption state, protection status, and encryption methods." },
        new() { Id = "sec-bitlocker-key", Title = "Retrieve BitLocker Volume Recovery Key", Command = "manage-bde -protectors -get C:", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.Security, Description = "Display numerical password recovery identifiers and secrets for the specified volume." },

        // Remote Management
        new() { Id = "rem-enter-session", Title = "Start Interactive Remote PowerShell Session", Command = "Enter-PSSession -ComputerName \"<hostname>\"", ShellType = AdminShellType.PowerShell, Category = AdminCommandCategory.RemoteManagement, Description = "Establish an interactive remote management shell via WinRM PowerShell remoting." },
        new() { Id = "rem-invoke-command", Title = "Run Remote Command Block via WinRM", Command = "Invoke-Command -ComputerName \"<hostname>\" -ScriptBlock { Get-Service }", ShellType = AdminShellType.PowerShell, Category = AdminCommandCategory.RemoteManagement, Description = "Execute script blocks on one or more remote systems and stream objects back." },
        new() { Id = "rem-mstsc-admin", Title = "Connect RDP Session to Remote Console", Command = "mstsc /v:<hostname> /admin", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.RemoteManagement, Description = "Launch Remote Desktop Connection connecting directly to the console/admin session." },
        new() { Id = "rem-query-sessions", Title = "Query Remote User Sessions", Command = "qwinsta /server:<hostname>", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.RemoteManagement, Description = "Query active and disconnected user Terminal Services sessions on a remote host." },
        new() { Id = "rem-logoff-session", Title = "Logoff Remote User Session", Command = "logoff <sessionId> /server:<hostname>", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.RemoteManagement, Description = "Terminate and log off a specific user session ID on a remote server." },
        new() { Id = "rem-reboot-computer", Title = "Initiate Remote Scheduled Reboot", Command = "shutdown /r /m \\\\<hostname> /t 60 /c \"Rebooting for maintenance\"", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.RemoteManagement, Description = "Remotely trigger a system reboot with a timed notification grace period." },
        new() { Id = "rem-winrm-config", Title = "Enable and Configure WinRM Service", Command = "winrm quickconfig", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.RemoteManagement, Description = "Start WinRM service and configure firewall exceptions for remote PowerShell." },

        // Storage
        new() { Id = "stor-volume-space", Title = "Inspect Volume Free Space and Labels", Command = "Get-Volume | Select-Object DriveLetter, FileSystemLabel, SizeRemaining, Size", ShellType = AdminShellType.PowerShell, Category = AdminCommandCategory.Storage, Description = "Query drive letters, file systems, volume labels, free space, and total storage capacities." },
        new() { Id = "stor-chkdsk", Title = "Run Online File System Check", Command = "chkdsk C: /scan", ShellType = AdminShellType.Cmd, Category = AdminCommandCategory.Storage, Description = "Run online diagnostic check for NTFS file system defects without dismounting drive." },
        new() { Id = "stor-smb-shares", Title = "List Active SMB File Shares", Command = "Get-SmbShare | Select-Object Name, Path, Description", ShellType = AdminShellType.PowerShell, Category = AdminCommandCategory.Storage, Description = "List all SMB shared network folders, local paths, and access descriptions." },
        new() { Id = "stor-smb-openfiles", Title = "Query Locked Files Open via SMB", Command = "Get-SmbOpenFile | Select-Object FileId, Path, ClientUserName, Locks", ShellType = AdminShellType.PowerShell, Category = AdminCommandCategory.Storage, Description = "Identify files open and locked by network users across active SMB shares." },
        new() { Id = "stor-smb-sessions", Title = "View Active SMB Client Connections", Command = "Get-SmbSession | Select-Object SessionId, ClientComputerName, ClientUserName, NumOpens", ShellType = AdminShellType.PowerShell, Category = AdminCommandCategory.Storage, Description = "Inspect connected client computers, usernames, and open file handle counts." }
    };

    public AdminCommandService(string? customFavoritesPath = null)
    {
        if (!string.IsNullOrWhiteSpace(customFavoritesPath))
        {
            _favoritesFilePath = customFavoritesPath;
        }
        else
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dir = Path.Combine(appData, "Sol");
            Directory.CreateDirectory(dir);
            _favoritesFilePath = Path.Combine(dir, "admin_command_favorites.json");
        }

        LoadFavorites();
    }

    public IReadOnlyList<AdminCommandItem> GetAllCommands()
    {
        lock (_lock)
        {
            return _commandCatalog
                .Select(CloneWithFavoriteStatus)
                .OrderByDescending(c => c.IsFavorite)
                .ThenBy(c => c.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public IReadOnlyList<AdminCommandItem> SearchCommands(string? query = null, AdminCommandCategory? category = null, AdminShellType? shellType = null)
    {
        lock (_lock)
        {
            var items = _commandCatalog.AsEnumerable();

            if (category.HasValue && category.Value != AdminCommandCategory.All)
            {
                items = items.Where(c => c.Category == category.Value);
            }

            if (shellType.HasValue && shellType.Value != AdminShellType.All)
            {
                items = items.Where(c => c.ShellType == shellType.Value);
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                string q = query.Trim();
                items = items.Where(c =>
                    c.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    c.Command.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    c.Description.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    c.CategoryDisplayName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    c.ShellBadgeText.Contains(q, StringComparison.OrdinalIgnoreCase));
            }

            return items
                .Select(CloneWithFavoriteStatus)
                .OrderByDescending(c => c.IsFavorite)
                .ThenByDescending(c => !string.IsNullOrWhiteSpace(query) && c.Title.StartsWith(query.Trim(), StringComparison.OrdinalIgnoreCase))
                .ThenBy(c => c.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public void ToggleFavorite(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return;

        lock (_lock)
        {
            if (_favoriteIds.Contains(id))
            {
                _favoriteIds.Remove(id);
            }
            else
            {
                _favoriteIds.Add(id);
            }
            SaveFavorites();
        }

        FavoritesChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool IsFavorite(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        lock (_lock)
        {
            return _favoriteIds.Contains(id);
        }
    }

    public string CopyCommand(AdminCommandItem item)
    {
        if (item == null) return string.Empty;
        return item.Command;
    }

    public void RequestOpen()
    {
        OpenRequested?.Invoke(this, EventArgs.Empty);
    }

    private AdminCommandItem CloneWithFavoriteStatus(AdminCommandItem source)
    {
        return new AdminCommandItem
        {
            Id = source.Id,
            Title = source.Title,
            Command = source.Command,
            ShellType = source.ShellType,
            Category = source.Category,
            Description = source.Description,
            RequiresElevation = source.RequiresElevation,
            IsFavorite = _favoriteIds.Contains(source.Id)
        };
    }

    private void LoadFavorites()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_favoritesFilePath))
                {
                    string json = File.ReadAllText(_favoritesFilePath);
                    var ids = JsonSerializer.Deserialize<List<string>>(json);
                    if (ids != null)
                    {
                        _favoriteIds.Clear();
                        foreach (var id in ids)
                        {
                            _favoriteIds.Add(id);
                        }
                    }
                }
            }
            catch { }
        }
    }

    private void SaveFavorites()
    {
        try
        {
            string json = JsonSerializer.Serialize(_favoriteIds.ToList(), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_favoritesFilePath, json);
        }
        catch { }
    }

    private const int HOTKEY_ID = 0x4143;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;
    private const uint VK_C = 0x43;

    private IntPtr _registeredHwnd = IntPtr.Zero;

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public bool RegisterGlobalHotkey(IntPtr hWnd)
    {
        UnregisterGlobalHotkey(_registeredHwnd);
        _registeredHwnd = hWnd;

        // 1. Primary: Win + Shift + C
        bool registered = RegisterHotKey(hWnd, HOTKEY_ID, MOD_WIN | MOD_SHIFT | MOD_NOREPEAT, VK_C);
        if (!registered)
        {
            registered = RegisterHotKey(hWnd, HOTKEY_ID, MOD_WIN | MOD_SHIFT, VK_C);
        }

        // 2. Secondary fallback: Alt + Shift + C
        if (!registered)
        {
            registered = RegisterHotKey(hWnd, HOTKEY_ID, MOD_ALT | MOD_SHIFT | MOD_NOREPEAT, VK_C);
        }
        if (!registered)
        {
            registered = RegisterHotKey(hWnd, HOTKEY_ID, MOD_ALT | MOD_SHIFT, VK_C);
        }

        return registered;
    }

    public void UnregisterGlobalHotkey(IntPtr hWnd)
    {
        if (_registeredHwnd != IntPtr.Zero)
        {
            UnregisterHotKey(_registeredHwnd, HOTKEY_ID);
            _registeredHwnd = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        UnregisterGlobalHotkey(_registeredHwnd);
        GC.SuppressFinalize(this);
    }
}
