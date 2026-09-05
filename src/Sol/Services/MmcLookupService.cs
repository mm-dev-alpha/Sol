using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Sol.Models;

namespace Sol.Services;

public class MmcLookupService : IMmcLookupService
{
    private readonly HashSet<string> _favoriteIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _favoritesFilePath;
    private readonly object _lock = new();

    public event EventHandler? FavoritesChanged;
    public event EventHandler? OpenRequested;

    private static readonly List<MmcToolItem> _toolCatalog = new()
    {
        // Active Directory
        new() { Id = "dsa", Name = "Active Directory Users and Computers", Command = "dsa.msc", Category = MmcCategory.ActiveDirectory, Description = "Administer users, computers, groups, and organizational units in Active Directory Domain Services." },
        new() { Id = "gpmc", Name = "Group Policy Management Console", Command = "gpmc.msc", Category = MmcCategory.ActiveDirectory, Description = "Manage Group Policy Objects (GPOs), organizational unit inheritance, and domain-wide policy settings." },
        new() { Id = "adsiedit", Name = "ADSI Edit", Command = "adsiedit.msc", Category = MmcCategory.ActiveDirectory, Description = "Low-level Active Directory LDAP directory editor for objects, attributes, schema, and partitions." },
        new() { Id = "domain", Name = "Active Directory Domains and Trusts", Command = "domain.msc", Category = MmcCategory.ActiveDirectory, Description = "Manage domain trust relationships, forest functional levels, and alternate UPN suffixes." },
        new() { Id = "dssite", Name = "Active Directory Sites and Services", Command = "dssite.msc", Category = MmcCategory.ActiveDirectory, Description = "Configure Active Directory replication topology, subnets, sites, and IP inter-site transports." },

        // Management & Computers
        new() { Id = "compmgmt", Name = "Computer Management", Command = "compmgmt.msc", Category = MmcCategory.Management, Description = "Comprehensive console combining Event Viewer, Device Manager, Disk Management, and Services." },
        new() { Id = "services", Name = "Services Console", Command = "services.msc", Category = MmcCategory.Management, Description = "Start, stop, configure, and inspect Windows background services and their startup modes." },
        new() { Id = "devmgmt", Name = "Device Manager", Command = "devmgmt.msc", Category = MmcCategory.Management, Description = "Inspect, update, enable, or disable hardware devices, firmware, and peripheral drivers." },
        new() { Id = "taskschd", Name = "Task Scheduler", Command = "taskschd.msc", Category = MmcCategory.Management, Description = "Create, monitor, and automate scheduled background tasks, triggers, and recurring maintenance jobs." },
        new() { Id = "lusrmgr", Name = "Local Users and Groups", Command = "lusrmgr.msc", Category = MmcCategory.Management, Description = "Create, reset passwords, and manage local user accounts and local security groups." },
        new() { Id = "wmimgmt", Name = "WMI Management", Command = "wmimgmt.msc", Category = MmcCategory.Management, Description = "Configure WMI namespace security, authorization permissions, and backup WMI repository." },
        new() { Id = "printmanagement", Name = "Print Management", Command = "printmanagement.msc", Category = MmcCategory.Management, Description = "Manage local and network print servers, printer drivers, and active spooler queues." },
        new() { Id = "clusmgr", Name = "Failover Cluster Manager", Command = "clusmgr.msc", Category = MmcCategory.Management, Description = "Monitor and configure high-availability failover clusters, roles, storage pools, and cluster nodes." },
        new() { Id = "virtmgmt", Name = "Hyper-V Manager", Command = "virtmgmt.msc", Category = MmcCategory.Management, Description = "Create, configure, inspect, and snapshot Hyper-V virtual machines and virtual switches." },
        new() { Id = "inetmgr", Name = "IIS Manager", Command = "inetmgr.exe", Category = MmcCategory.Management, Description = "Manage Internet Information Services (IIS) web sites, application pools, and SSL bindings." },
        new() { Id = "gpedit", Name = "Local Group Policy Editor", Command = "gpedit.msc", Category = MmcCategory.Management, Description = "Configure computer and user configuration policies on the local machine." },

        // Networking
        new() { Id = "ncpa", Name = "Network Connections", Command = "ncpa.cpl", Category = MmcCategory.Networking, Description = "View and configure physical, virtual, and VPN network adapter IP addresses, DNS, and gateways." },
        new() { Id = "dhcpmgmt", Name = "DHCP Management", Command = "dhcpmgmt.msc", Category = MmcCategory.Networking, Description = "Configure scopes, address pools, reservations, exclusions, and active leases on DHCP servers." },
        new() { Id = "dnsmgmt", Name = "DNS Management", Command = "dnsmgmt.msc", Category = MmcCategory.Networking, Description = "Manage forward/reverse lookup zones, DNS resource records, and root name server hints." },

        // Diagnostics
        new() { Id = "eventvwr", Name = "Event Viewer", Command = "eventvwr.msc", Category = MmcCategory.Diagnostics, Description = "View application, security, system, and setup event logs and diagnostic traces." },
        new() { Id = "resmon", Name = "Resource Monitor", Command = "resmon.exe", Category = MmcCategory.Diagnostics, Description = "Real-time analysis of CPU, memory, disk I/O activity, and network TCP connections." },
        new() { Id = "perfmon", Name = "Performance Monitor", Command = "perfmon.msc", Category = MmcCategory.Diagnostics, Description = "Real-time and logged performance counters, data collector sets, and system reliability reports." },
        new() { Id = "msinfo32", Name = "System Information", Command = "msinfo32.exe", Category = MmcCategory.Diagnostics, Description = "Comprehensive summary of hardware resources, BIOS details, components, and software environment." },
        new() { Id = "taskmgr", Name = "Task Manager", Command = "taskmgr.exe", Category = MmcCategory.Diagnostics, Description = "Monitor running processes, system performance, startup applications, and user session utilization." },
        new() { Id = "dxdiag", Name = "DirectX Diagnostic Tool", Command = "dxdiag.exe", Category = MmcCategory.Diagnostics, Description = "Inspect DirectX audio, video display drivers, feature levels, and hardware acceleration status." },

        // Security
        new() { Id = "wf", Name = "Windows Defender Firewall", Command = "wf.msc", Category = MmcCategory.Security, Description = "Manage inbound, outbound, and connection security rules with advanced packet filtering." },
        new() { Id = "secpol", Name = "Local Security Policy", Command = "secpol.msc", Category = MmcCategory.Security, Description = "Configure password complexity policies, account lockout, audit policies, and user rights." },
        new() { Id = "certlm", Name = "Certificates (Local Computer)", Command = "certlm.msc", Category = MmcCategory.Security, Description = "Manage machine certificates, SSL/TLS web bindings, and local computer trusted root authorities." },
        new() { Id = "certmgr", Name = "Certificates (Current User)", Command = "certmgr.msc", Category = MmcCategory.Security, Description = "Manage user digital certificates, personal encryption keys, and trusted root certificates." },
        new() { Id = "firewallcpl", Name = "Firewall Control Panel", Command = "firewall.cpl", Category = MmcCategory.Security, Description = "Toggle basic network firewall profiles, allow applications, and troubleshoot port blocking." },
        new() { Id = "azman", Name = "Authorization Manager", Command = "azman.msc", Category = MmcCategory.Security, Description = "Configure role-based security authorization policies, operations, and application groups." },

        // Storage
        new() { Id = "diskmgmt", Name = "Disk Management", Command = "diskmgmt.msc", Category = MmcCategory.Storage, Description = "Initialize, partition, format, shrink, extend, and manage local storage drives and volumes." },
        new() { Id = "fsmgmt", Name = "Shared Folders", Command = "fsmgmt.msc", Category = MmcCategory.Storage, Description = "View active SMB file shares, connected client sessions, and open locked file handles." },
        new() { Id = "cleanmgr", Name = "Disk Cleanup", Command = "cleanmgr.exe", Category = MmcCategory.Storage, Description = "Free up disk storage by safely removing temporary files, logs, and previous Windows installations." },

        // System Applets
        new() { Id = "appwiz", Name = "Programs and Features", Command = "appwiz.cpl", Category = MmcCategory.System, Description = "Uninstall or repair installed software, view Windows updates, and turn optional features on or off." },
        new() { Id = "sysdm", Name = "System Properties", Command = "sysdm.cpl", Category = MmcCategory.System, Description = "Configure computer name, domain join, environment variables, page file, and system protection." },
        new() { Id = "powercfg", Name = "Power Options", Command = "powercfg.cpl", Category = MmcCategory.System, Description = "Configure system sleep, display timeouts, power plans, battery actions, and lid behaviors." },
        new() { Id = "timedate", Name = "Date and Time", Command = "timedate.cpl", Category = MmcCategory.System, Description = "Set system date and time, configure time zone, and synchronize with NTP internet time servers." },
        new() { Id = "main", Name = "Mouse Properties", Command = "main.cpl", Category = MmcCategory.System, Description = "Configure mouse pointer speed, buttons, wheel scrolling, and pointer schemes." },
        new() { Id = "hdwwiz", Name = "Hardware Installation Wizard", Command = "hdwwiz.cpl", Category = MmcCategory.System, Description = "Legacy hardware installation wizard for manually installing non-Plug and Play devices." },
        new() { Id = "regedit", Name = "Registry Editor", Command = "regedit.exe", Category = MmcCategory.System, Description = "View, search, and modify Windows system registry keys, values, and hive configurations." },
        new() { Id = "msconfig", Name = "System Configuration", Command = "msconfig.exe", Category = MmcCategory.System, Description = "Configure boot parameters, system service startup, and diagnostic boot configurations." },
        new() { Id = "control", Name = "Control Panel Hub", Command = "control.exe", Category = MmcCategory.System, Description = "Classical administrative Control Panel applet hub." }
    };

    public MmcLookupService(string? customFavoritesPath = null)
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
            _favoritesFilePath = Path.Combine(dir, "mmc_favorites.json");
        }

        LoadFavorites();
    }

    public IReadOnlyList<MmcToolItem> GetAllTools()
    {
        lock (_lock)
        {
            return _toolCatalog
                .Select(CloneWithFavoriteStatus)
                .OrderByDescending(t => t.IsFavorite)
                .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public IReadOnlyList<MmcToolItem> SearchTools(string? query = null, MmcCategory? category = null)
    {
        lock (_lock)
        {
            var items = _toolCatalog.AsEnumerable();

            if (category.HasValue && category.Value != MmcCategory.All)
            {
                items = items.Where(t => t.Category == category.Value);
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                string q = query.Trim();
                items = items.Where(t =>
                    t.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    t.Command.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (t.Arguments != null && t.Arguments.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    t.Description.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    t.CategoryDisplayName.Contains(q, StringComparison.OrdinalIgnoreCase));
            }

            return items
                .Select(CloneWithFavoriteStatus)
                .OrderByDescending(t => t.IsFavorite)
                .ThenByDescending(t => !string.IsNullOrWhiteSpace(query) && t.Name.StartsWith(query.Trim(), StringComparison.OrdinalIgnoreCase))
                .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
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

    public bool LaunchTool(MmcToolItem tool, out string? errorMessage)
    {
        errorMessage = null;
        if (tool == null || string.IsNullOrWhiteSpace(tool.Command))
        {
            errorMessage = "Invalid tool command.";
            return false;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = tool.Command,
                Arguments = tool.Arguments ?? string.Empty,
                UseShellExecute = true
            };

            Process.Start(psi);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    public string CopyRunCommand(MmcToolItem tool)
    {
        if (tool == null) return string.Empty;
        return tool.FullRunCommand;
    }

    public void RequestOpen()
    {
        OpenRequested?.Invoke(this, EventArgs.Empty);
    }

    private MmcToolItem CloneWithFavoriteStatus(MmcToolItem source)
    {
        return new MmcToolItem
        {
            Id = source.Id,
            Name = source.Name,
            Command = source.Command,
            Arguments = source.Arguments,
            Description = source.Description,
            Category = source.Category,
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

    private const int HOTKEY_ID = 0x4D4D;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;
    private const uint VK_SPACE = 0x20;
    private const uint VK_M = 0x4D;

    private IntPtr _registeredHwnd = IntPtr.Zero;

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public bool RegisterGlobalHotkey(IntPtr hWnd)
    {
        UnregisterGlobalHotkey(_registeredHwnd);
        _registeredHwnd = hWnd;

        // 1. Primary: Alt + Space
        bool registered = RegisterHotKey(hWnd, HOTKEY_ID, MOD_ALT | MOD_NOREPEAT, VK_SPACE);
        if (!registered)
        {
            registered = RegisterHotKey(hWnd, HOTKEY_ID, MOD_ALT, VK_SPACE);
        }

        // 2. Secondary fallback: Win + Shift + M
        if (!registered)
        {
            registered = RegisterHotKey(hWnd, HOTKEY_ID, MOD_WIN | MOD_SHIFT | MOD_NOREPEAT, VK_M);
        }
        if (!registered)
        {
            registered = RegisterHotKey(hWnd, HOTKEY_ID, MOD_WIN | MOD_SHIFT, VK_M);
        }

        // 3. Tertiary fallback: Alt + Shift + M
        if (!registered)
        {
            registered = RegisterHotKey(hWnd, HOTKEY_ID, MOD_ALT | MOD_SHIFT | MOD_NOREPEAT, VK_M);
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
