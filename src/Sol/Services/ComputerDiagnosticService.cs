using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using Sol.Models;

namespace Sol.Services;

public class ComputerDiagnosticService : IComputerDiagnosticService
{
    private static readonly TimeSpan RealMachineTimeout = TimeSpan.FromSeconds(15);

    public async Task<ComputerUptimeSnapshot> GetUptimeSnapshotAsync(string targetHost, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetHost))
        {
            return new ComputerUptimeSnapshot
            {
                Hostname = string.Empty,
                IsSuccess = false,
                ErrorMessage = "Target hostname is empty."
            };
        }

        string cleanHost = targetHost.Trim();

        if (IsDemoFixture(cleanHost))
        {
            await Task.Delay(100, cancellationToken);
            return GetFallbackUptimeSnapshot(cleanHost, string.Empty);
        }

        var wmiTask = Task.Run(() => QueryUptimeWmi(cleanHost, cancellationToken), cancellationToken);
        var delayTask = Task.Delay(RealMachineTimeout, cancellationToken);

        var completedTask = await Task.WhenAny(wmiTask, delayTask);
        if (completedTask == delayTask)
        {
            return new ComputerUptimeSnapshot
            {
                Hostname = cleanHost,
                IsSuccess = false,
                ErrorMessage = "Timeout (15s): Endpoint unreachable",
                QueriedAt = DateTime.Now
            };
        }

        return await wmiTask;
    }

    public async Task<ComputerHardwareSnapshot> GetHardwareSnapshotAsync(string targetHost, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetHost))
        {
            return new ComputerHardwareSnapshot
            {
                Hostname = string.Empty,
                IsSuccess = false,
                ErrorMessage = "Target hostname is empty."
            };
        }

        string cleanHost = targetHost.Trim();

        if (IsDemoFixture(cleanHost))
        {
            await Task.Delay(100, cancellationToken);
            return GetFallbackHardwareSnapshot(cleanHost, string.Empty);
        }

        var wmiTask = Task.Run(() => QueryHardwareWmi(cleanHost, cancellationToken), cancellationToken);
        var delayTask = Task.Delay(RealMachineTimeout, cancellationToken);

        var completedTask = await Task.WhenAny(wmiTask, delayTask);
        if (completedTask == delayTask)
        {
            return new ComputerHardwareSnapshot
            {
                Hostname = cleanHost,
                IsSuccess = false,
                ErrorMessage = "Timeout (15s): Endpoint unreachable",
                QueriedAt = DateTime.Now
            };
        }

        return await wmiTask;
    }

    public async Task<ComputerDiskSnapshot> GetDiskSnapshotAsync(string targetHost, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetHost))
        {
            return new ComputerDiskSnapshot
            {
                Hostname = string.Empty,
                IsSuccess = false,
                ErrorMessage = "Target hostname is empty."
            };
        }

        string cleanHost = targetHost.Trim();

        if (IsDemoFixture(cleanHost))
        {
            await Task.Delay(100, cancellationToken);
            return GetFallbackDiskSnapshot(cleanHost, string.Empty);
        }

        var wmiTask = Task.Run(() => QueryDiskWmi(cleanHost, cancellationToken), cancellationToken);
        var delayTask = Task.Delay(RealMachineTimeout, cancellationToken);

        var completedTask = await Task.WhenAny(wmiTask, delayTask);
        if (completedTask == delayTask)
        {
            return new ComputerDiskSnapshot
            {
                Hostname = cleanHost,
                IsSuccess = false,
                ErrorMessage = "Timeout (15s): Endpoint unreachable",
                QueriedAt = DateTime.Now
            };
        }

        return await wmiTask;
    }

    private static ComputerUptimeSnapshot QueryUptimeWmi(string cleanHost, CancellationToken cancellationToken)
    {
        try
        {
            var scope = CreateManagementScope(cleanHost);

            cancellationToken.ThrowIfCancellationRequested();

            DateTime? lastBootTime = null;

            using (var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT LastBootUpTime FROM Win32_OperatingSystem")))
            using (var results = searcher.Get())
            {
                foreach (ManagementObject obj in results)
                {
                    var rawBoot = obj["LastBootUpTime"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(rawBoot))
                    {
                        lastBootTime = ParseCimDateTime(rawBoot);
                    }
                    break;
                }
            }

            TimeSpan? uptime = lastBootTime.HasValue ? (DateTime.Now - lastBootTime.Value) : null;
            if (uptime.HasValue && uptime.Value < TimeSpan.Zero)
            {
                uptime = TimeSpan.Zero;
            }

            var (isRebootKnown, pendingReasons) = QueryPendingReboot(cleanHost, scope);

            return new ComputerUptimeSnapshot
            {
                Hostname = cleanHost,
                LastBootUpTime = lastBootTime,
                Uptime = uptime,
                IsRebootPending = isRebootKnown && pendingReasons.Count > 0,
                IsRebootStatusKnown = isRebootKnown,
                PendingRebootReasons = pendingReasons,
                IsSuccess = true,
                QueriedAt = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            return new ComputerUptimeSnapshot
            {
                Hostname = cleanHost,
                IsSuccess = false,
                ErrorMessage = ex.Message,
                QueriedAt = DateTime.Now
            };
        }
    }

    private static ComputerHardwareSnapshot QueryHardwareWmi(string cleanHost, CancellationToken cancellationToken)
    {
        try
        {
            var scope = CreateManagementScope(cleanHost);

            cancellationToken.ThrowIfCancellationRequested();

            string manufacturer = string.Empty;
            string model = string.Empty;
            string totalMemory = string.Empty;

            using (var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT Manufacturer, Model, TotalPhysicalMemory FROM Win32_ComputerSystem")))
            using (var results = searcher.Get())
            {
                foreach (ManagementObject obj in results)
                {
                    manufacturer = obj["Manufacturer"]?.ToString()?.Trim() ?? string.Empty;
                    model = obj["Model"]?.ToString()?.Trim() ?? string.Empty;
                    if (obj["TotalPhysicalMemory"] != null && ulong.TryParse(obj["TotalPhysicalMemory"].ToString(), out ulong bytes))
                    {
                        double gb = bytes / (1024.0 * 1024.0 * 1024.0);
                        totalMemory = $"{gb:F1} GB";
                    }
                    break;
                }
            }

            string serialNumber = string.Empty;
            string biosVersion = string.Empty;
            string biosReleaseDate = string.Empty;

            using (var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT SerialNumber, SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS")))
            using (var results = searcher.Get())
            {
                foreach (ManagementObject obj in results)
                {
                    serialNumber = obj["SerialNumber"]?.ToString()?.Trim() ?? string.Empty;
                    biosVersion = obj["SMBIOSBIOSVersion"]?.ToString()?.Trim() ?? string.Empty;
                    
                    var rawDate = obj["ReleaseDate"]?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(rawDate))
                    {
                        biosReleaseDate = FormatCimDateTime(rawDate);
                    }
                    break;
                }
            }

            string osCaption = string.Empty;
            string osVersion = string.Empty;
            string buildNumber = string.Empty;

            using (var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT Caption, Version, BuildNumber FROM Win32_OperatingSystem")))
            using (var results = searcher.Get())
            {
                foreach (ManagementObject obj in results)
                {
                    osCaption = obj["Caption"]?.ToString()?.Trim() ?? string.Empty;
                    osVersion = obj["Version"]?.ToString()?.Trim() ?? string.Empty;
                    buildNumber = obj["BuildNumber"]?.ToString()?.Trim() ?? string.Empty;
                    break;
                }
            }

            string cpuName = string.Empty;
            using (var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT Name FROM Win32_Processor")))
            using (var results = searcher.Get())
            {
                foreach (ManagementObject obj in results)
                {
                    cpuName = obj["Name"]?.ToString()?.Trim() ?? string.Empty;
                    break;
                }
            }

            string displayVersion = GetWindowsDisplayVersionFromBuild(buildNumber);

            return new ComputerHardwareSnapshot
            {
                Hostname = cleanHost,
                Manufacturer = CleanManufacturer(manufacturer),
                Model = model,
                SerialNumber = serialNumber,
                BiosVersion = biosVersion,
                BiosReleaseDate = biosReleaseDate,
                OsCaption = osCaption,
                OsVersion = osVersion,
                BuildNumber = buildNumber,
                DisplayVersion = displayVersion,
                CpuName = cpuName,
                TotalMemoryFormatted = totalMemory,
                IsSuccess = true,
                QueriedAt = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            return new ComputerHardwareSnapshot
            {
                Hostname = cleanHost,
                IsSuccess = false,
                ErrorMessage = ex.Message,
                QueriedAt = DateTime.Now
            };
        }
    }

    private static ComputerDiskSnapshot QueryDiskWmi(string cleanHost, CancellationToken cancellationToken)
    {
        try
        {
            var scope = CreateManagementScope(cleanHost);

            cancellationToken.ThrowIfCancellationRequested();

            var drives = new List<ComputerDiskDriveInfo>();

            // Query physical drive media type and SMART health from Win32_DiskDrive
            var physicalDisks = new List<(string Model, string MediaType, string Status)>();
            try
            {
                using var diskSearcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT Model, MediaType, Status FROM Win32_DiskDrive"));
                using var diskResults = diskSearcher.Get();
                foreach (ManagementObject disk in diskResults)
                {
                    var status = disk["Status"]?.ToString()?.Trim() ?? "OK";
                    var model = disk["Model"]?.ToString()?.Trim() ?? string.Empty;
                    string mType = "SSD";
                    if (model.Contains("NVMe", StringComparison.OrdinalIgnoreCase) || 
                        model.Contains("SSD", StringComparison.OrdinalIgnoreCase) ||
                        model.Contains("T705", StringComparison.OrdinalIgnoreCase) ||
                        model.Contains("980", StringComparison.OrdinalIgnoreCase) ||
                        model.Contains("990", StringComparison.OrdinalIgnoreCase) ||
                        model.Contains("970", StringComparison.OrdinalIgnoreCase))
                    {
                        mType = "NVMe SSD";
                    }
                    else if (model.Contains("HDD", StringComparison.OrdinalIgnoreCase) || model.Contains("Hard Disk", StringComparison.OrdinalIgnoreCase))
                    {
                        mType = "HDD";
                    }
                    physicalDisks.Add((model, mType, status));
                }
            }
            catch { }

            // Optionally enrich from root\Microsoft\Windows\Storage MSFT_PhysicalDisk
            try
            {
                var storageScope = CreateManagementScope(cleanHost, @"root\Microsoft\Windows\Storage");
                using var pSearcher = new ManagementObjectSearcher(storageScope, new ObjectQuery("SELECT FriendlyName, MediaType, HealthStatus, BusType FROM MSFT_PhysicalDisk"));
                using var pResults = pSearcher.Get();
                foreach (ManagementObject pDisk in pResults)
                {
                    var bus = pDisk["BusType"]?.ToString()?.Trim() ?? string.Empty;
                    var hStatus = pDisk["HealthStatus"]?.ToString()?.Trim();
                    string health = (hStatus == "0" || hStatus == "Healthy") ? "OK" : (hStatus == "1" ? "Warning" : (hStatus == "2" ? "Pred Fail" : "OK"));
                    string media = (bus == "17" || bus.Equals("NVMe", StringComparison.OrdinalIgnoreCase)) ? "NVMe SSD" : "SSD";
                    var name = pDisk["FriendlyName"]?.ToString()?.Trim() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        physicalDisks.Add((name, media, health));
                    }
                }
            }
            catch { }

            string defaultMediaType = physicalDisks.Count > 0 ? physicalDisks[0].MediaType : "NVMe SSD";
            string defaultHealth = physicalDisks.Count > 0 ? physicalDisks[0].Status : "OK";

            // Query local fixed logical disks (DriveType = 3)
            using (var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT DeviceID, VolumeName, FileSystem, Size, FreeSpace FROM Win32_LogicalDisk WHERE DriveType = 3")))
            using (var results = searcher.Get())
            {
                foreach (ManagementObject obj in results)
                {
                    string deviceId = obj["DeviceID"]?.ToString()?.Trim() ?? string.Empty;
                    string volumeName = obj["VolumeName"]?.ToString()?.Trim() ?? string.Empty;
                    string fileSystem = obj["FileSystem"]?.ToString()?.Trim() ?? "NTFS";

                    ulong totalBytes = 0;
                    ulong freeBytes = 0;

                    if (obj["Size"] != null && ulong.TryParse(obj["Size"].ToString(), out ulong size))
                    {
                        totalBytes = size;
                    }

                    if (obj["FreeSpace"] != null && ulong.TryParse(obj["FreeSpace"].ToString(), out ulong free))
                    {
                        freeBytes = free;
                    }

                    if (totalBytes > 0)
                    {
                        string driveMedia = defaultMediaType;
                        string driveHealth = defaultHealth;

                        var matchedDisk = physicalDisks.FirstOrDefault(p => 
                            (!string.IsNullOrWhiteSpace(volumeName) && p.Model.Contains(volumeName, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(volumeName) && volumeName.Contains(p.Model, StringComparison.OrdinalIgnoreCase)));

                        if (!string.IsNullOrWhiteSpace(matchedDisk.Model))
                        {
                            driveMedia = matchedDisk.MediaType;
                            driveHealth = matchedDisk.Status;
                        }

                        drives.Add(new ComputerDiskDriveInfo
                        {
                            DeviceId = deviceId,
                            VolumeName = volumeName,
                            FileSystem = fileSystem,
                            TotalBytes = totalBytes,
                            FreeBytes = freeBytes,
                            MediaType = driveMedia,
                            HealthStatus = driveHealth
                        });
                    }
                }
            }

            return new ComputerDiskSnapshot
            {
                Hostname = cleanHost,
                Drives = drives.OrderBy(d => d.DeviceId).ToList(),
                IsSuccess = true,
                QueriedAt = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            return new ComputerDiskSnapshot
            {
                Hostname = cleanHost,
                IsSuccess = false,
                ErrorMessage = ex.Message,
                QueriedAt = DateTime.Now
            };
        }
    }

    public string? GetWarrantyUrl(string? manufacturer, string? serialNumber)
    {
        if (string.IsNullOrWhiteSpace(serialNumber)) return null;

        var cleanSerial = Uri.EscapeDataString(serialNumber.Trim());
        var mfg = manufacturer ?? string.Empty;

        if (mfg.Contains("Dell", StringComparison.OrdinalIgnoreCase))
        {
            return $"https://www.dell.com/support/home/product-support/servicetag/{cleanSerial}/overview";
        }

        if (mfg.Contains("Lenovo", StringComparison.OrdinalIgnoreCase))
        {
            return $"https://pcsupport.lenovo.com/products/search?query={cleanSerial}";
        }

        if (mfg.Contains("HP", StringComparison.OrdinalIgnoreCase) || mfg.Contains("Hewlett", StringComparison.OrdinalIgnoreCase))
        {
            return $"https://support.hp.com/us-en/checkwarranty?serialnumber={cleanSerial}";
        }

        return null;
    }

    public static string GetWindowsDisplayVersionFromBuild(string buildNumber)
    {
        if (string.IsNullOrWhiteSpace(buildNumber) || !int.TryParse(buildNumber, out int build))
        {
            return string.Empty;
        }

        return build switch
        {
            >= 26100 => "24H2",
            >= 22631 => "23H2",
            >= 22621 => "22H2",
            >= 22000 => "21H2",
            19045 => "22H2",
            19044 => "21H2",
            19043 => "21H1",
            19042 => "20H2",
            19041 => "2004",
            18363 => "1909",
            18362 => "1903",
            17763 => "1809",
            17134 => "1803",
            16299 => "1709",
            15063 => "1703",
            14393 => "1607",
            10586 => "1511",
            10240 => "1507",
            _ => string.Empty
        };
    }

    private static string CleanManufacturer(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        if (raw.StartsWith("Dell", StringComparison.OrdinalIgnoreCase)) return "Dell Inc.";
        if (raw.StartsWith("Lenovo", StringComparison.OrdinalIgnoreCase)) return "Lenovo";
        if (raw.StartsWith("HP", StringComparison.OrdinalIgnoreCase) || raw.Contains("Hewlett", StringComparison.OrdinalIgnoreCase)) return "HP";
        if (raw.Contains("Microsoft", StringComparison.OrdinalIgnoreCase)) return "Microsoft";
        if (raw.Contains("VMware", StringComparison.OrdinalIgnoreCase)) return "VMware";

        return raw;
    }

    private static string FormatCimDateTime(string cimDateTime)
    {
        try
        {
            // CIM format: yyyymmddHHMMSS.mmmmmm+UUU (e.g. 20240412000000.000000+000)
            if (cimDateTime.Length >= 8 && DateTime.TryParseExact(cimDateTime.Substring(0, 8), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                return dt.ToString("yyyy-MM-dd");
            }
        }
        catch { }

        return cimDateTime;
    }

    public static DateTime? ParseCimDateTime(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try
        {
            return ManagementDateTimeConverter.ToDateTime(raw);
        }
        catch
        {
            try
            {
                if (raw.Length >= 14 && DateTime.TryParseExact(raw.Substring(0, 14), "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    return dt;
                }
            }
            catch { }
        }
        return null;
    }

    private static (bool isKnown, List<string> reasons) QueryPendingReboot(string host, ManagementScope scope)
    {
        var reasons = new List<string>();
        bool checkedViaRemoteReg = false;

        // Method 1: Try Registry (Local BaseKey for local endpoint, RemoteRegistry winreg for remote hosts)
        try
        {
            using var baseKey = IsLocalHost(host)
                ? Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Default)
                : Microsoft.Win32.RegistryKey.OpenRemoteBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, host);

            // 1. CBS
            using (var cbsKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending"))
            {
                if (cbsKey != null) reasons.Add("Component-Based Servicing (CBS)");
            }

            // 2. Windows Update
            using (var wuKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired"))
            {
                if (wuKey != null) reasons.Add("Windows Update");
            }

            // 3. Pending File Rename Operations
            using (var smKey = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager"))
            {
                var renameVal = smKey?.GetValue("PendingFileRenameOperations");
                if (renameVal is string[] strArr && strArr.Length > 0 && strArr.Any(s => !string.IsNullOrWhiteSpace(s)))
                {
                    reasons.Add("Pending File Rename Operations");
                }
                else if (renameVal is string str && !string.IsNullOrWhiteSpace(str))
                {
                    reasons.Add("Pending File Rename Operations");
                }
            }

            // 4. Computer Name Change Pending
            using (var activeNameKey = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\ComputerName\ActiveComputerName"))
            using (var compNameKey = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\ComputerName\ComputerName"))
            {
                var activeName = activeNameKey?.GetValue("ComputerName")?.ToString();
                var compName = compNameKey?.GetValue("ComputerName")?.ToString();
                if (!string.IsNullOrWhiteSpace(activeName) && !string.IsNullOrWhiteSpace(compName) && !string.Equals(activeName, compName, StringComparison.OrdinalIgnoreCase))
                {
                    reasons.Add("Computer Rename Pending");
                }
            }

            checkedViaRemoteReg = true;
        }
        catch
        {
            // RemoteRegistry service stopped or firewall blocked port 445
        }

        if (checkedViaRemoteReg)
        {
            return (true, reasons.Distinct().ToList());
        }

        // Method 2: Fallback to WMI StdRegProv over established WMI connection
        try
        {
            var defaultScope = new ManagementScope($@"\\{host}\root\default", scope.Options);
            defaultScope.Connect();
            using var regClass = new ManagementClass(defaultScope, new ManagementPath("StdRegProv"), null);

            // 1. CBS Check via WMI EnumKey
            var inParams = regClass.GetMethodParameters("EnumKey");
            inParams["hDefKey"] = 0x80000002; // HKLM
            inParams["sSubKeyName"] = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing";
            var outParams = regClass.InvokeMethod("EnumKey", inParams, null);
            if ((uint)outParams["ReturnValue"] == 0)
            {
                if (outParams["sNames"] is string[] subkeys && subkeys.Contains("RebootPending", StringComparer.OrdinalIgnoreCase))
                {
                    reasons.Add("Component-Based Servicing (CBS)");
                }
            }

            // 2. Windows Update Check via WMI EnumKey
            inParams["sSubKeyName"] = @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update";
            outParams = regClass.InvokeMethod("EnumKey", inParams, null);
            if ((uint)outParams["ReturnValue"] == 0)
            {
                if (outParams["sNames"] is string[] wuSubkeys && wuSubkeys.Contains("RebootRequired", StringComparer.OrdinalIgnoreCase))
                {
                    reasons.Add("Windows Update");
                }
            }

            // 3. PendingFileRenameOperations via WMI GetMultiStringValue
            var multiParams = regClass.GetMethodParameters("GetMultiStringValue");
            multiParams["hDefKey"] = 0x80000002;
            multiParams["sSubKeyName"] = @"SYSTEM\CurrentControlSet\Control\Session Manager";
            multiParams["sValueName"] = "PendingFileRenameOperations";
            var multiOut = regClass.InvokeMethod("GetMultiStringValue", multiParams, null);
            if ((uint)multiOut["ReturnValue"] == 0 && multiOut["sValue"] is string[] renameList && renameList.Length > 0 && renameList.Any(s => !string.IsNullOrWhiteSpace(s)))
            {
                reasons.Add("Pending File Rename Operations");
            }

            return (true, reasons.Distinct().ToList());
        }
        catch
        {
            // Both RemoteRegistry and WMI StdRegProv failed
            return (false, []);
        }
    }

    private static ComputerHardwareSnapshot GetFallbackHardwareSnapshot(string cleanHost, string originalError)
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

    private static ComputerDiskSnapshot GetFallbackDiskSnapshot(string cleanHost, string originalError)
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
                            FreeBytes = 28UL * 1024 * 1024 * 1024, // ~11% free -> triggers low space warning (<15%)
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

    private static ComputerUptimeSnapshot GetFallbackUptimeSnapshot(string cleanHost, string originalError)
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

    public static bool IsLocalHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host)) return true;
        string h = host.Trim();
        return h.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
               h.Equals("127.0.0.1") ||
               h.Equals(".") ||
               h.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
               h.StartsWith(Environment.MachineName + ".", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsDemoFixture(string host)
    {
        if (string.IsNullOrWhiteSpace(host)) return false;
        if (IsLocalHost(host)) return false;

        string h = host.Trim();
        return h.Contains("PC-DELL-LATITUDE", StringComparison.OrdinalIgnoreCase) ||
               h.Contains("PC-LENOVO-THINKPAD", StringComparison.OrdinalIgnoreCase) ||
               h.EndsWith(".company.local", StringComparison.OrdinalIgnoreCase) ||
               h.EndsWith(".contoso.local", StringComparison.OrdinalIgnoreCase) ||
               h.StartsWith("DEMO-", StringComparison.OrdinalIgnoreCase) ||
               h.StartsWith("TEST-", StringComparison.OrdinalIgnoreCase);
    }

    private static ManagementScope CreateManagementScope(string host, string namespaceName = @"root\cimv2")
    {
        var options = new ConnectionOptions
        {
            Impersonation = ImpersonationLevel.Impersonate,
            Authentication = AuthenticationLevel.PacketPrivacy,
            Timeout = TimeSpan.FromSeconds(10),
            EnablePrivileges = true
        };

        string path = IsLocalHost(host) ? $@"\\.\{namespaceName}" : $@"\\{host}\{namespaceName}";
        var scope = new ManagementScope(path, options);
        scope.Connect();
        return scope;
    }
}
