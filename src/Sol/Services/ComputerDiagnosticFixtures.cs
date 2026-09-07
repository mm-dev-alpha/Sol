using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Sol.Helpers;
using Sol.Models;

namespace Sol.Services;

/// <summary>
/// Provides simulation data and demo fallback fixtures for offline or demo environments.
/// </summary>
public static class ComputerDiagnosticFixtures
{
    public static readonly ConcurrentDictionary<string, byte> DisconnectedDemoSessions = new(StringComparer.OrdinalIgnoreCase);
    public static readonly ConcurrentDictionary<string, byte> TerminatedDemoProcesses = new(StringComparer.OrdinalIgnoreCase);
    public static readonly ConcurrentDictionary<string, bool> DemoBitLockerSuspendedState = new(StringComparer.OrdinalIgnoreCase);
    public static readonly ConcurrentDictionary<string, string> DemoServiceStates = new(StringComparer.OrdinalIgnoreCase);
    public static readonly ConcurrentDictionary<string, string> DemoServiceStartModes = new(StringComparer.OrdinalIgnoreCase);

    public static bool IsDemoFixture(string host)
    {
        if (string.IsNullOrWhiteSpace(host)) return false;
        string h = host.Trim();
        return h.Contains("PC-DELL-LATITUDE", StringComparison.OrdinalIgnoreCase) ||
               h.Contains("PC-LENOVO-THINKPAD", StringComparison.OrdinalIgnoreCase) ||
               h.EndsWith(".company.local", StringComparison.OrdinalIgnoreCase) ||
               h.EndsWith(".contoso.local", StringComparison.OrdinalIgnoreCase) ||
               h.Contains("DEMO", StringComparison.OrdinalIgnoreCase) ||
               h.Contains("TEST", StringComparison.OrdinalIgnoreCase);
    }

    public static ComputerHardwareSnapshot GetFallbackHardwareSnapshot(string cleanHost, string originalError)
    {
        if (IsDemoFixture(cleanHost))
        {
            if (cleanHost.Contains("DELL", StringComparison.OrdinalIgnoreCase))
            {
                return new ComputerHardwareSnapshot
                {
                    Hostname = cleanHost,
                    Manufacturer = "Dell Inc.",
                    Model = "Latitude 5540",
                    SerialNumber = "7G8X9Y2",
                    BiosVersion = "1.12.0",
                    BiosReleaseDate = "2024-03-15",
                    OsCaption = "Microsoft Windows 11 Enterprise",
                    OsVersion = "10.0.26100",
                    BuildNumber = "26100",
                    DisplayVersion = "24H2",
                    CpuName = "13th Gen Intel(R) Core(TM) i7-1365U",
                    TotalMemoryFormatted = "16.0 GB",
                    IsSuccess = true,
                    QueriedAt = DateTime.Now
                };
            }

            if (cleanHost.Contains("LENOVO", StringComparison.OrdinalIgnoreCase) || cleanHost.Contains("THINKPAD", StringComparison.OrdinalIgnoreCase))
            {
                return new ComputerHardwareSnapshot
                {
                    Hostname = cleanHost,
                    Manufacturer = "Lenovo",
                    Model = "ThinkPad T14 Gen 4",
                    SerialNumber = "PF3A4B5C",
                    BiosVersion = "N3MET15W (1.15)",
                    BiosReleaseDate = "2024-01-20",
                    OsCaption = "Microsoft Windows 11 Pro",
                    OsVersion = "10.0.22631",
                    BuildNumber = "22631",
                    DisplayVersion = "23H2",
                    CpuName = "AMD Ryzen 7 PRO 7840U w/ Radeon 780M Graphics",
                    TotalMemoryFormatted = "32.0 GB",
                    IsSuccess = true,
                    QueriedAt = DateTime.Now
                };
            }

            return new ComputerHardwareSnapshot
            {
                Hostname = cleanHost,
                Manufacturer = "Microsoft Corporation",
                Model = "Surface Laptop 5",
                SerialNumber = "012345678953",
                BiosVersion = "15.101.143",
                BiosReleaseDate = "2023-11-10",
                OsCaption = "Microsoft Windows 11 Pro",
                OsVersion = "10.0.22631",
                BuildNumber = "22631",
                DisplayVersion = "23H2",
                CpuName = "12th Gen Intel(R) Core(TM) i7-1265U",
                TotalMemoryFormatted = "16.0 GB",
                IsSuccess = true,
                QueriedAt = DateTime.Now
            };
        }

        return new ComputerHardwareSnapshot
        {
            Hostname = cleanHost,
            IsSuccess = false,
            ErrorMessage = originalError,
            QueriedAt = DateTime.Now
        };
    }

    public static ComputerDiskSnapshot GetFallbackDiskSnapshot(string cleanHost, string originalError)
    {
        if (IsDemoFixture(cleanHost))
        {
            if (cleanHost.Contains("DELL", StringComparison.OrdinalIgnoreCase))
            {
                return new ComputerDiskSnapshot
                {
                    Hostname = cleanHost,
                    Drives = new List<ComputerDiskDriveInfo>
                    {
                        new ComputerDiskDriveInfo
                        {
                            DeviceId = "C:",
                            VolumeName = "OSDisk",
                            FileSystem = "NTFS",
                            TotalBytes = 512UL * 1024 * 1024 * 1024,
                            FreeBytes = 168UL * 1024 * 1024 * 1024,
                            MediaType = "NVMe SSD",
                            HealthStatus = "OK"
                        },
                        new ComputerDiskDriveInfo
                        {
                            DeviceId = "D:",
                            VolumeName = "Data",
                            FileSystem = "NTFS",
                            TotalBytes = 1024UL * 1024 * 1024 * 1024,
                            FreeBytes = 780UL * 1024 * 1024 * 1024,
                            MediaType = "SSD",
                            HealthStatus = "OK"
                        }
                    },
                    IsSuccess = true,
                    QueriedAt = DateTime.Now
                };
            }

            if (cleanHost.Contains("LENOVO", StringComparison.OrdinalIgnoreCase) || cleanHost.Contains("THINKPAD", StringComparison.OrdinalIgnoreCase))
            {
                return new ComputerDiskSnapshot
                {
                    Hostname = cleanHost,
                    Drives = new List<ComputerDiskDriveInfo>
                    {
                        new ComputerDiskDriveInfo
                        {
                            DeviceId = "C:",
                            VolumeName = "Windows",
                            FileSystem = "NTFS",
                            TotalBytes = 256UL * 1024 * 1024 * 1024,
                            FreeBytes = 28UL * 1024 * 1024 * 1024,
                            MediaType = "NVMe SSD",
                            HealthStatus = "OK"
                        }
                    },
                    IsSuccess = true,
                    QueriedAt = DateTime.Now
                };
            }

            return new ComputerDiskSnapshot
            {
                Hostname = cleanHost,
                Drives = new List<ComputerDiskDriveInfo>
                {
                    new ComputerDiskDriveInfo
                    {
                        DeviceId = "C:",
                        VolumeName = "System",
                        FileSystem = "NTFS",
                        TotalBytes = 512UL * 1024 * 1024 * 1024,
                        FreeBytes = 240UL * 1024 * 1024 * 1024,
                        MediaType = "NVMe SSD",
                        HealthStatus = "OK"
                    }
                },
                IsSuccess = true,
                QueriedAt = DateTime.Now
            };
        }

        return new ComputerDiskSnapshot
        {
            Hostname = cleanHost,
            IsSuccess = false,
            ErrorMessage = originalError,
            QueriedAt = DateTime.Now
        };
    }

    public static ComputerBatterySnapshot GetFallbackBatterySnapshot(string cleanHost, string originalError)
    {
        if (IsDemoFixture(cleanHost))
        {
            if (cleanHost.Contains("DELL", StringComparison.OrdinalIgnoreCase))
            {
                return new ComputerBatterySnapshot
                {
                    Hostname = cleanHost,
                    HasBattery = true,
                    DeviceName = "Dell Primary Battery (ExpressCharge)",
                    Chemistry = "Lithium-Ion (Li-Ion)",
                    DesignCapacityMWh = 54000,
                    FullChargeCapacityMWh = 48060,
                    EstimatedChargeRemainingPercent = 92,
                    BatteryStatusText = Strings.S.BatteryStatusDischarging,
                    IsCharging = false,
                    IsAcConnected = false,
                    CycleCount = 184,
                    EstimatedRunTime = TimeSpan.FromHours(4).Add(TimeSpan.FromMinutes(15)),
                    IsSuccess = true,
                    QueriedAt = DateTime.Now
                };
            }

            if (cleanHost.Contains("LENOVO", StringComparison.OrdinalIgnoreCase) || cleanHost.Contains("THINKPAD", StringComparison.OrdinalIgnoreCase))
            {
                return new ComputerBatterySnapshot
                {
                    Hostname = cleanHost,
                    HasBattery = true,
                    DeviceName = "Lenovo Li-Polymer 57Wh Battery",
                    Chemistry = "Lithium-Polymer (Li-Poly)",
                    DesignCapacityMWh = 57000,
                    FullChargeCapacityMWh = 41040,
                    EstimatedChargeRemainingPercent = 45,
                    BatteryStatusText = Strings.S.BatteryStatusCharging,
                    IsCharging = true,
                    IsAcConnected = true,
                    CycleCount = 412,
                    EstimatedRunTime = TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(40)),
                    IsSuccess = true,
                    QueriedAt = DateTime.Now
                };
            }

            return new ComputerBatterySnapshot
            {
                Hostname = cleanHost,
                HasBattery = false,
                IsSuccess = true,
                QueriedAt = DateTime.Now
            };
        }

        return new ComputerBatterySnapshot
        {
            Hostname = cleanHost,
            IsSuccess = false,
            ErrorMessage = originalError,
            QueriedAt = DateTime.Now
        };
    }

    public static ComputerUptimeSnapshot GetFallbackUptimeSnapshot(string cleanHost, string originalError)
    {
        if (IsDemoFixture(cleanHost))
        {
            if (cleanHost.Contains("DELL", StringComparison.OrdinalIgnoreCase))
            {
                var boot = DateTime.Now.AddDays(-12).AddHours(-4);
                return new ComputerUptimeSnapshot
                {
                    Hostname = cleanHost,
                    LastBootUpTime = boot,
                    Uptime = DateTime.Now - boot,
                    IsRebootPending = true,
                    IsRebootStatusKnown = true,
                    PendingRebootReasons = new List<string> { "Windows Update", "Component-Based Servicing (CBS)" },
                    IsSuccess = true,
                    QueriedAt = DateTime.Now
                };
            }

            if (cleanHost.Contains("LENOVO", StringComparison.OrdinalIgnoreCase) || cleanHost.Contains("THINKPAD", StringComparison.OrdinalIgnoreCase))
            {
                var boot = DateTime.Now.AddDays(-3).AddHours(-8);
                return new ComputerUptimeSnapshot
                {
                    Hostname = cleanHost,
                    LastBootUpTime = boot,
                    Uptime = DateTime.Now - boot,
                    IsRebootPending = false,
                    IsRebootStatusKnown = true,
                    PendingRebootReasons = [],
                    IsSuccess = true,
                    QueriedAt = DateTime.Now
                };
            }

            var defaultBoot = DateTime.Now.AddDays(-5).AddHours(-11);
            return new ComputerUptimeSnapshot
            {
                Hostname = cleanHost,
                LastBootUpTime = defaultBoot,
                Uptime = DateTime.Now - defaultBoot,
                IsRebootPending = false,
                IsRebootStatusKnown = true,
                PendingRebootReasons = [],
                IsSuccess = true,
                QueriedAt = DateTime.Now
            };
        }

        return new ComputerUptimeSnapshot
        {
            Hostname = cleanHost,
            IsSuccess = false,
            ErrorMessage = originalError,
            QueriedAt = DateTime.Now
        };
    }

    public static ComputerSessionSnapshot GetFallbackSessionSnapshot(string cleanHost, string originalError)
    {
        if (IsDemoFixture(cleanHost))
        {
            var sessions = new List<ComputerSessionInfo>();

            if (cleanHost.Contains("DELL", StringComparison.OrdinalIgnoreCase))
            {
                uint sessId = 1;
                if (!DisconnectedDemoSessions.ContainsKey($"{cleanHost}:{sessId}"))
                {
                    sessions.Add(new ComputerSessionInfo
                    {
                        SessionId = sessId,
                        Username = "m.mustermann",
                        Domain = "CORP",
                        SamAccountName = "m.mustermann",
                        DisplayName = "Max Mustermann",
                        SessionType = ComputerSessionType.Console,
                        LogonTime = DateTime.Today.AddHours(7).AddMinutes(45),
                        IsActive = true
                    });
                }
            }
            else if (cleanHost.Contains("LENOVO", StringComparison.OrdinalIgnoreCase) || cleanHost.Contains("THINKPAD", StringComparison.OrdinalIgnoreCase))
            {
                uint sessId1 = 2;
                if (!DisconnectedDemoSessions.ContainsKey($"{cleanHost}:{sessId1}"))
                {
                    sessions.Add(new ComputerSessionInfo
                    {
                        SessionId = sessId1,
                        Username = "e.schmidt",
                        Domain = "CORP",
                        SamAccountName = "e.schmidt",
                        DisplayName = "Erika Schmidt",
                        SessionType = ComputerSessionType.RemoteDesktop,
                        LogonTime = DateTime.Today.AddHours(8).AddMinutes(30),
                        IsActive = true
                    });
                }

                uint sessId2 = 3;
                if (!DisconnectedDemoSessions.ContainsKey($"{cleanHost}:{sessId2}"))
                {
                    sessions.Add(new ComputerSessionInfo
                    {
                        SessionId = sessId2,
                        Username = "a.becker",
                        Domain = "CORP",
                        SamAccountName = "a.becker",
                        DisplayName = "Alexander Becker",
                        SessionType = ComputerSessionType.Disconnected,
                        LogonTime = DateTime.Today.AddDays(-1).AddHours(17).AddMinutes(15),
                        IsActive = false
                    });
                }
            }
            else
            {
                uint sessIdDefault = 1;
                if (!DisconnectedDemoSessions.ContainsKey($"{cleanHost}:{sessIdDefault}"))
                {
                    sessions.Add(new ComputerSessionInfo
                    {
                        SessionId = sessIdDefault,
                        Username = "admin",
                        Domain = "CORP",
                        SamAccountName = "admin",
                        DisplayName = "Administrator",
                        SessionType = ComputerSessionType.Console,
                        LogonTime = DateTime.Today.AddHours(9).AddMinutes(0),
                        IsActive = true
                    });
                }
            }

            return new ComputerSessionSnapshot
            {
                Hostname = cleanHost,
                Sessions = sessions,
                IsSuccess = true,
                QueriedAt = DateTime.Now
            };
        }

        return new ComputerSessionSnapshot
        {
            Hostname = cleanHost,
            IsSuccess = false,
            ErrorMessage = originalError,
            QueriedAt = DateTime.Now
        };
    }

    public static ComputerProcessSnapshot GetFallbackProcessSnapshot(string cleanHost, string originalError)
    {
        if (IsDemoFixture(cleanHost))
        {
            var list = new List<ComputerProcessInfo>();

            void AddProc(uint pid, string name, string path, ulong memoryMb, string owner, DateTime creation, double cpu = 0.0, double net = 0.0)
            {
                if (TerminatedDemoProcesses.ContainsKey($"{cleanHost}:{pid}")) return;
                list.Add(new ComputerProcessInfo
                {
                    ProcessId = pid,
                    Name = name,
                    ExecutablePath = path,
                    WorkingSetBytes = memoryMb * 1024UL * 1024UL,
                    CpuUsagePercent = cpu,
                    NetworkMbps = net,
                    Owner = owner,
                    CreationDate = creation
                });
            }

            var now = DateTime.Now;

            AddProc(0, "System Idle Process", "", 0, "NT AUTHORITY\\SYSTEM", now.AddDays(-2), cpu: 82.5, net: 0.0);
            AddProc(4, "System", "", 120, "NT AUTHORITY\\SYSTEM", now.AddDays(-2), cpu: 0.8, net: 0.1);
            AddProc(412, "smss.exe", @"C:\Windows\System32\smss.exe", 8, "NT AUTHORITY\\SYSTEM", now.AddDays(-2), cpu: 0.0, net: 0.0);
            AddProc(620, "csrss.exe", @"C:\Windows\System32\csrss.exe", 18, "NT AUTHORITY\\SYSTEM", now.AddDays(-2), cpu: 0.2, net: 0.0);
            AddProc(704, "wininit.exe", @"C:\Windows\System32\wininit.exe", 12, "NT AUTHORITY\\SYSTEM", now.AddDays(-2), cpu: 0.0, net: 0.0);
            AddProc(812, "services.exe", @"C:\Windows\System32\services.exe", 32, "NT AUTHORITY\\SYSTEM", now.AddDays(-2), cpu: 0.3, net: 0.0);
            AddProc(844, "lsass.exe", @"C:\Windows\System32\lsass.exe", 46, "NT AUTHORITY\\SYSTEM", now.AddDays(-2), cpu: 0.5, net: 0.1);
            AddProc(980, "dwm.exe", @"C:\Windows\System32\dwm.exe", 145, "Window Manager\\DWM-1", now.AddHours(-8), cpu: 1.8, net: 0.0);
            AddProc(1120, "svchost.exe", @"C:\Windows\System32\svchost.exe", 85, "NT AUTHORITY\\SYSTEM", now.AddDays(-2), cpu: 0.6, net: 0.2);
            AddProc(1450, "svchost.exe", @"C:\Windows\System32\svchost.exe", 62, "NT AUTHORITY\\LOCAL SERVICE", now.AddDays(-2), cpu: 0.1, net: 0.0);

            string userOwner = cleanHost.Contains("LENOVO", StringComparison.OrdinalIgnoreCase) ? "CORP\\e.schmidt" : "CORP\\m.mustermann";

            AddProc(4812, "explorer.exe", @"C:\Windows\explorer.exe", 260, userOwner, now.AddHours(-6), cpu: 0.9, net: 0.0);
            AddProc(6120, "chrome.exe", @"C:\Program Files\Google\Chrome\Application\chrome.exe", 1420, userOwner, now.AddHours(-4), cpu: 4.8, net: 2.3);
            AddProc(7240, "msedge.exe", @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe", 890, userOwner, now.AddHours(-3), cpu: 2.4, net: 0.8);
            AddProc(8912, "OUTLOOK.EXE", @"C:\Program Files\Microsoft Office\root\Office16\OUTLOOK.EXE", 740, userOwner, now.AddHours(-5), cpu: 0.5, net: 0.2);
            AddProc(9420, "ms-teams.exe", @"C:\Program Files\WindowsApps\MSTeams\ms-teams.exe", 680, userOwner, now.AddHours(-5), cpu: 2.1, net: 0.6);
            AddProc(10240, "Code.exe", @"C:\Users\AppData\Local\Programs\Microsoft VS Code\Code.exe", 540, userOwner, now.AddHours(-2), cpu: 1.6, net: 0.0);
            AddProc(11350, "OneDrive.exe", @"C:\Program Files\Microsoft OneDrive\OneDrive.exe", 110, userOwner, now.AddHours(-6), cpu: 0.1, net: 0.4);
            AddProc(12480, "RuntimeBroker.exe", @"C:\Windows\System32\RuntimeBroker.exe", 45, userOwner, now.AddHours(-4), cpu: 0.0, net: 0.0);
            AddProc(13890, "powershell.exe", @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe", 130, userOwner, now.AddMinutes(-45), cpu: 0.3, net: 0.0);

            return new ComputerProcessSnapshot
            {
                Hostname = cleanHost,
                Processes = list.OrderByDescending(p => p.WorkingSetBytes).ToList(),
                IsSuccess = true,
                Timestamp = now
            };
        }

        return new ComputerProcessSnapshot
        {
            Hostname = cleanHost,
            IsSuccess = false,
            ErrorMessage = originalError,
            Timestamp = DateTime.Now
        };
    }

    public static ComputerServicesSnapshot GetFallbackServicesSnapshot(string host, string? reason = null)
    {
        var baseServices = new List<ComputerServiceInfo>
        {
            new() { Name = "Spooler", DisplayName = "Print Spooler", State = "Running", StartMode = "Auto", StartName = "LocalSystem", Description = "Manages all print jobs and handles load-balancing of multi-printer setups." },
            new() { Name = "wuauserv", DisplayName = "Windows Update", State = "Running", StartMode = "Manual", StartName = "NT AUTHORITY\\NetworkService", Description = "Enables the detection, download, and installation of updates for Windows and other programs." },
            new() { Name = "WinDefend", DisplayName = "Microsoft Defender Antivirus Service", State = "Running", StartMode = "Auto", StartName = "NT AUTHORITY\\LocalService", Description = "Helps protect users from malware and other potentially unwanted software." },
            new() { Name = "W32Time", DisplayName = "Windows Time", State = "Running", StartMode = "Auto", StartName = "NT AUTHORITY\\LocalService", Description = "Maintains date and time synchronization on all clients and servers in the network." },
            new() { Name = "LanmanServer", DisplayName = "Server", State = "Running", StartMode = "Auto", StartName = "LocalSystem", Description = "Supports file, print, and named-pipe sharing over the network for this computer." },
            new() { Name = "LanmanWorkstation", DisplayName = "Workstation", State = "Running", StartMode = "Auto", StartName = "NT AUTHORITY\\NetworkService", Description = "Creates and maintains client network connections to remote servers." },
            new() { Name = "Netlogon", DisplayName = "Netlogon", State = "Running", StartMode = "Auto", StartName = "LocalSystem", Description = "Maintains a secure channel between this computer and the domain controller for authenticating users and services." },
            new() { Name = "RpcSs", DisplayName = "Remote Procedure Call (RPC)", State = "Running", StartMode = "Auto", StartName = "NT AUTHORITY\\NetworkService", Description = "The RPCSS service is the Service Control Manager for COM and DCOM servers." },
            new() { Name = "EventLog", DisplayName = "Windows Event Log", State = "Running", StartMode = "Auto", StartName = "NT AUTHORITY\\LocalService", Description = "This service manages events and event logs." },
            new() { Name = "PlugPlay", DisplayName = "Plug and Play", State = "Running", StartMode = "Auto", StartName = "LocalSystem", Description = "Enables a computer to recognize and adapt to hardware changes with little or no user input." },
            new() { Name = "Winmgmt", DisplayName = "Windows Management Instrumentation", State = "Running", StartMode = "Auto", StartName = "LocalSystem", Description = "Provides a common interface and object model to access management information about operating system, devices, applications and services." },
            new() { Name = "CryptSvc", DisplayName = "Cryptographic Services", State = "Running", StartMode = "Auto", StartName = "NT AUTHORITY\\NetworkService", Description = "Provides four management services: Catalog Database Service, Protected Root Service, Automatic Root Certificate Update Service, and Key Service." },
            new() { Name = "Dhcp", DisplayName = "DHCP Client", State = "Running", StartMode = "Auto", StartName = "NT AUTHORITY\\LocalService", Description = "Registers and updates IP addresses and DNS records for this computer." },
            new() { Name = "Dnscache", DisplayName = "DNS Client", State = "Running", StartMode = "Auto", StartName = "NT AUTHORITY\\NetworkService", Description = "The DNS Client service (dnscache) caches Domain Name System (DNS) names and registers the full computer name for this computer." },
            new() { Name = "BITS", DisplayName = "Background Intelligent Transfer Service", State = "Running", StartMode = "Auto", StartName = "LocalSystem", Description = "Transfers files in the background using idle network bandwidth." },
            new() { Name = "BrokerInfrastructure", DisplayName = "Background Tasks Infrastructure Service", State = "Running", StartMode = "Auto", StartName = "LocalSystem", Description = "Windows infrastructure service that controls which background tasks can run on the system." },
            new() { Name = "DcomLaunch", DisplayName = "DCOM Server Process Launcher", State = "Running", StartMode = "Auto", StartName = "LocalSystem", Description = "The DCOMLAUNCH service launches COM and DCOM servers in response to object activation requests." },
            new() { Name = "gpsvc", DisplayName = "Group Policy Client", State = "Running", StartMode = "Auto", StartName = "LocalSystem", Description = "The service is responsible for applying settings configured by administrators for the computer and users through the Group Policy component." },
            new() { Name = "SysMain", DisplayName = "SysMain", State = "Running", StartMode = "Auto", StartName = "LocalSystem", Description = "Maintains and improves system performance over time." },
            new() { Name = "Themes", DisplayName = "Themes", State = "Running", StartMode = "Auto", StartName = "LocalSystem", Description = "Provides user experience theme management." },
            new() { Name = "AudioSrv", DisplayName = "Windows Audio", State = "Running", StartMode = "Auto", StartName = "NT AUTHORITY\\LocalService", Description = "Manages audio for Windows-based programs." },
            new() { Name = "AudioEndpointBuilder", DisplayName = "Windows Audio Endpoint Builder", State = "Running", StartMode = "Auto", StartName = "NT AUTHORITY\\LocalService", Description = "Manages audio devices for the Windows Audio service." },
            new() { Name = "WSearch", DisplayName = "Windows Search", State = "Running", StartMode = "Auto", StartName = "NT AUTHORITY\\SYSTEM", Description = "Provides content indexing, property caching, and search results for files, e-mail, and other content." },
            new() { Name = "DiagTrack", DisplayName = "Connected User Experiences and Telemetry", State = "Running", StartMode = "Auto", StartName = "NT AUTHORITY\\SYSTEM", Description = "The Connected User Experiences and Telemetry service enables features that support in-application and connected user experiences." },
            new() { Name = "WerSvc", DisplayName = "Windows Error Reporting Service", State = "Stopped", StartMode = "Manual", StartName = "LocalSystem", Description = "Allows errors to be reported when programs stop working or responding." },
            new() { Name = "Schedule", DisplayName = "Task Scheduler", State = "Running", StartMode = "Auto", StartName = "LocalSystem", Description = "Enables a user to configure and schedule automated tasks on this computer." },
            new() { Name = "RemoteRegistry", DisplayName = "Remote Registry", State = "Stopped", StartMode = "Disabled", StartName = "NT AUTHORITY\\LocalService", Description = "Enables remote users to modify registry settings on this computer." },
            new() { Name = "AppIDSvc", DisplayName = "Application Identity", State = "Stopped", StartMode = "Manual", StartName = "NT AUTHORITY\\LocalService", Description = "Determines and verifies the identity of an application." },
            new() { Name = "SNMP", DisplayName = "SNMP Service", State = "Stopped", StartMode = "Disabled", StartName = "LocalSystem", Description = "Includes SNMP agents that monitor the activity in network devices and report to the network console workstation." },
            new() { Name = "vmicguestinterface", DisplayName = "Hyper-V Guest Service Interface", State = "Stopped", StartMode = "Manual", StartName = "LocalSystem", Description = "Provides an interface for the Hyper-V host to interact with specific services running inside the virtual machine." },
            new() { Name = "vmicheartbeat", DisplayName = "Hyper-V Heartbeat Service", State = "Stopped", StartMode = "Manual", StartName = "LocalSystem", Description = "Monitors the state of this virtual machine by reporting a heartbeat at regular intervals." },
            new() { Name = "Fax", DisplayName = "Fax", State = "Stopped", StartMode = "Disabled", StartName = "NT AUTHORITY\\NetworkService", Description = "Enables you to send and receive faxes, utilizing fax resources available on this computer or on the network." },
            new() { Name = "RemoteAccess", DisplayName = "Routing and Remote Access", State = "Stopped", StartMode = "Disabled", StartName = "LocalSystem", Description = "Offers routing services to businesses in local area and wide area network environments." },
            new() { Name = "TapiSrv", DisplayName = "Telephony", State = "Stopped", StartMode = "Manual", StartName = "NT AUTHORITY\\NetworkService", Description = "Provides Telephony API (TAPI) support for programs that control telephony devices on the local computer." },
            new() { Name = "WbioSrvc", DisplayName = "Windows Biometric Service", State = "Running", StartMode = "Auto", StartName = "LocalSystem", Description = "The Windows biometric service gives client applications the ability to capture, compare, manipulate, and store biometric data." }
        };

        var finalServices = new List<ComputerServiceInfo>();
        foreach (var svc in baseServices)
        {
            string key = $"{host}:{svc.Name}";
            string state = svc.State;
            string startMode = svc.StartMode;

            if (DemoServiceStates.TryGetValue(key, out var overrideState))
            {
                state = overrideState;
            }
            if (DemoServiceStartModes.TryGetValue(key, out var overrideMode))
            {
                startMode = overrideMode;
            }

            finalServices.Add(svc with { State = state, StartMode = startMode });
        }

        var sorted = finalServices.OrderBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
        return new ComputerServicesSnapshot
        {
            Hostname = host,
            Services = sorted,
            IsSuccess = true,
            Timestamp = DateTime.Now
        };
    }
}
