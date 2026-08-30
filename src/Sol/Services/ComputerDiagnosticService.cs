using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Sol.Models;
using Sol.Helpers;

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

    public async Task<ComputerBatterySnapshot> GetBatterySnapshotAsync(string targetHost, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetHost))
        {
            return new ComputerBatterySnapshot
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
            return GetFallbackBatterySnapshot(cleanHost, string.Empty);
        }

        var wmiTask = Task.Run(() => QueryBatteryWmi(cleanHost, cancellationToken), cancellationToken);
        var delayTask = Task.Delay(RealMachineTimeout, cancellationToken);

        var completedTask = await Task.WhenAny(wmiTask, delayTask);
        if (completedTask == delayTask)
        {
            return new ComputerBatterySnapshot
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
                    using (obj)
                    {
                        var rawBoot = obj["LastBootUpTime"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(rawBoot))
                        {
                            lastBootTime = ParseCimDateTime(rawBoot);
                        }
                        break;
                    }
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
                    using (obj)
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
            }

            string serialNumber = string.Empty;
            string biosVersion = string.Empty;
            string biosReleaseDate = string.Empty;

            using (var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT SerialNumber, SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS")))
            using (var results = searcher.Get())
            {
                foreach (ManagementObject obj in results)
                {
                    using (obj)
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
            }

            string osCaption = string.Empty;
            string osVersion = string.Empty;
            string buildNumber = string.Empty;

            using (var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT Caption, Version, BuildNumber FROM Win32_OperatingSystem")))
            using (var results = searcher.Get())
            {
                foreach (ManagementObject obj in results)
                {
                    using (obj)
                    {
                        osCaption = obj["Caption"]?.ToString()?.Trim() ?? string.Empty;
                        osVersion = obj["Version"]?.ToString()?.Trim() ?? string.Empty;
                        buildNumber = obj["BuildNumber"]?.ToString()?.Trim() ?? string.Empty;
                        break;
                    }
                }
            }

            string cpuName = string.Empty;
            using (var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT Name FROM Win32_Processor")))
            using (var results = searcher.Get())
            {
                foreach (ManagementObject obj in results)
                {
                    using (obj)
                    {
                        cpuName = obj["Name"]?.ToString()?.Trim() ?? string.Empty;
                        break;
                    }
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
                    using (disk)
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
                    using (pDisk)
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
                    using (obj)
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
            using (var inParams = regClass.GetMethodParameters("EnumKey"))
            {
                inParams["hDefKey"] = 0x80000002; // HKLM
                inParams["sSubKeyName"] = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing";
                using var outParams = regClass.InvokeMethod("EnumKey", inParams, null);
                if (outParams != null && (uint)outParams["ReturnValue"] == 0)
                {
                    if (outParams["sNames"] is string[] subkeys && subkeys.Contains("RebootPending", StringComparer.OrdinalIgnoreCase))
                    {
                        reasons.Add("Component-Based Servicing (CBS)");
                    }
                }
            }

            // 2. Windows Update Check via WMI EnumKey
            using (var inParams = regClass.GetMethodParameters("EnumKey"))
            {
                inParams["hDefKey"] = 0x80000002;
                inParams["sSubKeyName"] = @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update";
                using var outParams = regClass.InvokeMethod("EnumKey", inParams, null);
                if (outParams != null && (uint)outParams["ReturnValue"] == 0)
                {
                    if (outParams["sNames"] is string[] wuSubkeys && wuSubkeys.Contains("RebootRequired", StringComparer.OrdinalIgnoreCase))
                    {
                        reasons.Add("Windows Update");
                    }
                }
            }

            // 3. PendingFileRenameOperations via WMI GetMultiStringValue
            using (var multiParams = regClass.GetMethodParameters("GetMultiStringValue"))
            {
                multiParams["hDefKey"] = 0x80000002;
                multiParams["sSubKeyName"] = @"SYSTEM\CurrentControlSet\Control\Session Manager";
                multiParams["sValueName"] = "PendingFileRenameOperations";
                using var multiOut = regClass.InvokeMethod("GetMultiStringValue", multiParams, null);
                if (multiOut != null && (uint)multiOut["ReturnValue"] == 0 && multiOut["sValue"] is string[] renameList && renameList.Length > 0 && renameList.Any(s => !string.IsNullOrWhiteSpace(s)))
                {
                    reasons.Add("Pending File Rename Operations");
                }
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

    private static ComputerBatterySnapshot QueryBatteryWmi(string cleanHost, CancellationToken cancellationToken)
    {
        try
        {
            var scope = CreateManagementScope(cleanHost);

            cancellationToken.ThrowIfCancellationRequested();

            bool foundBattery = false;
            string deviceName = string.Empty;
            string chemistry = string.Empty;
            uint designCapacity = 0;
            uint fullChargeCapacity = 0;
            uint chargeRemaining = 0;
            ushort batteryStatus = 0;
            uint estimatedRunTimeMinutes = 0;

            using (var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT DeviceID, Name, Chemistry, DesignCapacity, FullChargeCapacity, EstimatedChargeRemaining, BatteryStatus, EstimatedRunTime FROM Win32_Battery")))
            using (var results = searcher.Get())
            {
                foreach (ManagementObject obj in results)
                {
                    using (obj)
                    {
                        foundBattery = true;
                        deviceName = obj["Name"]?.ToString()?.Trim() ?? obj["DeviceID"]?.ToString()?.Trim() ?? "Battery";
                        
                        var chemCode = obj["Chemistry"]?.ToString();
                        chemistry = chemCode switch
                        {
                            "1" => "Other",
                            "2" => "Unknown",
                            "3" => "Lead Acid",
                            "4" => "Nickel Cadmium (NiCd)",
                            "5" => "Nickel Metal Hydride (NiMH)",
                            "6" => "Lithium-Ion (Li-Ion)",
                            "7" => "Zinc Air",
                            "8" => "Lithium Polymer (Li-Poly)",
                            _ => !string.IsNullOrWhiteSpace(chemCode) ? chemCode : "Lithium-Ion"
                        };

                        if (obj["DesignCapacity"] != null && uint.TryParse(obj["DesignCapacity"].ToString(), out uint dCap))
                        {
                            designCapacity = dCap;
                        }

                        if (obj["FullChargeCapacity"] != null && uint.TryParse(obj["FullChargeCapacity"].ToString(), out uint fCap))
                        {
                            fullChargeCapacity = fCap;
                        }

                        if (obj["EstimatedChargeRemaining"] != null && uint.TryParse(obj["EstimatedChargeRemaining"].ToString(), out uint rem))
                        {
                            chargeRemaining = rem;
                        }

                        if (obj["BatteryStatus"] != null && ushort.TryParse(obj["BatteryStatus"].ToString(), out ushort bStat))
                        {
                            batteryStatus = bStat;
                        }

                        if (obj["EstimatedRunTime"] != null && uint.TryParse(obj["EstimatedRunTime"].ToString(), out uint runTime))
                        {
                            if (runTime > 0 && runTime < 100000)
                            {
                                estimatedRunTimeMinutes = runTime;
                            }
                        }

                        break;
                    }
                }
            }

            if (!foundBattery)
            {
                return new ComputerBatterySnapshot
                {
                    Hostname = cleanHost,
                    HasBattery = false,
                    IsSuccess = true,
                    QueriedAt = DateTime.Now
                };
            }

            int? cycleCount = null;

            // Try to enrich with root\wmi BatteryStaticData / BatteryFullChargedCapacity
            try
            {
                var wmiScope = CreateManagementScope(cleanHost, @"root\wmi");
                using var staticSearcher = new ManagementObjectSearcher(wmiScope, new ObjectQuery("SELECT DesignedCapacity, CycleCount FROM BatteryStaticData"));
                using var staticResults = staticSearcher.Get();
                foreach (ManagementObject sObj in staticResults)
                {
                    using (sObj)
                    {
                        if (designCapacity == 0 && sObj["DesignedCapacity"] != null && uint.TryParse(sObj["DesignedCapacity"].ToString(), out uint dCap))
                        {
                            designCapacity = dCap;
                        }

                        if (sObj["CycleCount"] != null && int.TryParse(sObj["CycleCount"].ToString(), out int cycles) && cycles > 0)
                        {
                            cycleCount = cycles;
                        }
                        break;
                    }
                }

                if (fullChargeCapacity == 0)
                {
                    using var fullSearcher = new ManagementObjectSearcher(wmiScope, new ObjectQuery("SELECT FullChargedCapacity FROM BatteryFullChargedCapacity"));
                    using var fullResults = fullSearcher.Get();
                    foreach (ManagementObject fObj in fullResults)
                    {
                        using (fObj)
                        {
                            if (fObj["FullChargedCapacity"] != null && uint.TryParse(fObj["FullChargedCapacity"].ToString(), out uint fCap))
                            {
                                fullChargeCapacity = fCap;
                            }
                            break;
                        }
                    }
                }
            }
            catch { }

            if (designCapacity > 0 && fullChargeCapacity == 0)
            {
                fullChargeCapacity = designCapacity;
            }

            bool isCharging = batteryStatus == 6 || batteryStatus == 7 || batteryStatus == 8 || batteryStatus == 9;
            bool isAcConnected = isCharging || batteryStatus == 2 || batteryStatus == 3;

            string statusText = batteryStatus switch
            {
                1 => Strings.S.BatteryStatusDischarging,
                2 => Strings.S.BatteryStatusCharging,
                3 => Strings.S.BatteryStatusFull,
                4 or 5 => Strings.S.BatteryStatusDischarging,
                6 or 7 or 8 or 9 => Strings.S.BatteryStatusCharging,
                11 => Strings.S.BatteryStatusDischarging,
                _ => isCharging ? Strings.S.BatteryStatusCharging : (isAcConnected ? Strings.S.BatteryStatusFull : Strings.S.BatteryStatusDischarging)
            };

            TimeSpan? runtime = estimatedRunTimeMinutes > 0 ? TimeSpan.FromMinutes(estimatedRunTimeMinutes) : null;

            return new ComputerBatterySnapshot
            {
                Hostname = cleanHost,
                HasBattery = true,
                DeviceName = deviceName,
                Chemistry = chemistry,
                DesignCapacityMWh = designCapacity,
                FullChargeCapacityMWh = fullChargeCapacity,
                EstimatedChargeRemainingPercent = chargeRemaining,
                BatteryStatusText = statusText,
                IsCharging = isCharging,
                IsAcConnected = isAcConnected,
                CycleCount = cycleCount,
                EstimatedRunTime = runtime,
                IsSuccess = true,
                QueriedAt = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            return new ComputerBatterySnapshot
            {
                Hostname = cleanHost,
                IsSuccess = false,
                ErrorMessage = ex.Message,
                QueriedAt = DateTime.Now
            };
        }
    }

    private static ComputerBatterySnapshot GetFallbackBatterySnapshot(string cleanHost, string originalError)
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
                    FullChargeCapacityMWh = 48060, // 89.0% health (Fehlerfrei)
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
                    FullChargeCapacityMWh = 41040, // 72.0% health (Abnutzung - Warning)
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

            // Other demo fixtures (Desktop/Server/VM)
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

    private static readonly ConcurrentDictionary<string, byte> _disconnectedDemoSessions = new(StringComparer.OrdinalIgnoreCase);

    public async Task<ComputerSessionSnapshot> GetSessionSnapshotAsync(string targetHost, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetHost))
        {
            return new ComputerSessionSnapshot
            {
                Hostname = string.Empty,
                IsSuccess = false,
                ErrorMessage = "Target hostname is empty.",
                QueriedAt = DateTime.Now
            };
        }

        string cleanHost = targetHost.Trim();

        // Fast-path: demo fixtures return mock sessions immediately
        if (IsDemoFixture(cleanHost))
        {
            return GetFallbackSessionSnapshot(cleanHost, "Simulated demo fixture");
        }

        try
        {
            using var timeoutCts = new CancellationTokenSource(RealMachineTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var queryTask = Task.Run(() => QuerySessionsWmi(cleanHost), linkedCts.Token);
            var completedTask = await Task.WhenAny(queryTask, Task.Delay(Timeout.Infinite, linkedCts.Token));

            if (completedTask == queryTask)
            {
                return await queryTask;
            }

            throw new TimeoutException($"Session query timed out after 15 seconds on '{cleanHost}'.");
        }
        catch (Exception ex)
        {
            return GetFallbackSessionSnapshot(cleanHost, ex.Message);
        }
    }

    public async Task DisconnectSessionAsync(string targetHost, uint sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetHost)) return;

        string cleanHost = targetHost.Trim();

        if (IsDemoFixture(cleanHost))
        {
            _disconnectedDemoSessions.TryAdd($"{cleanHost}:{sessionId}", 0);
            await Task.Delay(300, cancellationToken);
            return;
        }

        await Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "logoff.exe",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                psi.ArgumentList.Add(sessionId.ToString());
                psi.ArgumentList.Add($"/server:{cleanHost}");

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    proc.WaitForExit(8000);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Logoff failed for session {sessionId} on {cleanHost}: {ex.Message}", ex);
            }
        }, cancellationToken);
    }

    private static readonly ConcurrentDictionary<string, byte> _terminatedDemoProcesses = new(StringComparer.OrdinalIgnoreCase);

    public async Task<ComputerProcessSnapshot> GetProcessesSnapshotAsync(string targetHost, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetHost))
        {
            return new ComputerProcessSnapshot
            {
                Hostname = string.Empty,
                IsSuccess = false,
                ErrorMessage = "Target hostname is empty."
            };
        }

        string cleanHost = targetHost.Trim();

        // Fast-path: demo fixtures return mock processes immediately
        if (IsDemoFixture(cleanHost))
        {
            return GetFallbackProcessSnapshot(cleanHost, "Simulated demo fixture");
        }

        try
        {
            using var timeoutCts = new CancellationTokenSource(RealMachineTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var queryTask = Task.Run(() => QueryProcessesWmi(cleanHost, linkedCts.Token), linkedCts.Token);
            var completedTask = await Task.WhenAny(queryTask, Task.Delay(Timeout.Infinite, linkedCts.Token));

            if (completedTask == queryTask)
            {
                var result = await queryTask;
                if (!result.IsSuccess && IsDemoFixture(cleanHost))
                {
                    return GetFallbackProcessSnapshot(cleanHost, result.ErrorMessage ?? "WMI query failed.");
                }
                return result;
            }

            throw new TimeoutException($"Process query timed out after 15 seconds on '{cleanHost}'.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            if (IsDemoFixture(cleanHost))
            {
                return GetFallbackProcessSnapshot(cleanHost, "Connection timed out.");
            }
            return new ComputerProcessSnapshot
            {
                Hostname = cleanHost,
                IsSuccess = false,
                ErrorMessage = "WMI process diagnostic query timed out."
            };
        }
        catch (Exception ex)
        {
            if (IsDemoFixture(cleanHost))
            {
                return GetFallbackProcessSnapshot(cleanHost, ex.Message);
            }
            return new ComputerProcessSnapshot
            {
                Hostname = cleanHost,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> TerminateProcessAsync(string targetHost, uint processId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetHost)) return false;
        string cleanHost = targetHost.Trim();

        // Guard against terminating PID 0-4 (System / Idle)
        if (processId <= 4) return false;

        if (IsDemoFixture(cleanHost))
        {
            _terminatedDemoProcesses.TryAdd($"{cleanHost}:{processId}", 0);
            await Task.Delay(200, cancellationToken);
            return true;
        }

        return await Task.Run(() =>
        {
            // 1. Direct native execution for local machine with process-tree termination
            if (IsLocalHost(cleanHost))
            {
                try
                {
                    var proc = Process.GetProcessById((int)processId);
                    if (ComputerProcessInfo.IsCriticalProcess(processId, proc.ProcessName))
                    {
                        return false;
                    }
                    proc.Kill(entireProcessTree: true);
                    proc.WaitForExit(3000);
                    return true;
                }
                catch (ArgumentException)
                {
                    // Process has already terminated or does not exist
                    return true;
                }
                catch (InvalidOperationException)
                {
                    // Process has exited
                    return true;
                }
                catch
                {
                    // Fall through to taskkill fallback
                }
            }

            // 2. Remote WMI Win32_Process.Terminate invocation
            try
            {
                var scope = CreateManagementScope(cleanHost);
                using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery($"SELECT Name, ProcessId FROM Win32_Process WHERE ProcessId = {processId}"));
                using var results = searcher.Get();
                foreach (ManagementObject mo in results)
                {
                    using (mo)
                    {
                        string procName = mo["Name"]?.ToString() ?? string.Empty;
                        if (ComputerProcessInfo.IsCriticalProcess(processId, procName))
                        {
                            return false;
                        }

                        var inParams = mo.GetMethodParameters("Terminate");
                        inParams["Reason"] = (uint)0;
                        var outParams = mo.InvokeMethod("Terminate", inParams, null);
                        if (outParams != null)
                        {
                            uint returnVal = Convert.ToUInt32(outParams["ReturnValue"] ?? 1);
                            if (returnVal == 0) return true;
                        }
                    }
                }
            }
            catch { }

            // 3. Fallback to taskkill.exe with force (/F) and process-tree (/T) termination
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "taskkill.exe",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                if (!IsLocalHost(cleanHost))
                {
                    psi.ArgumentList.Add("/S");
                    psi.ArgumentList.Add(cleanHost);
                }
                psi.ArgumentList.Add("/PID");
                psi.ArgumentList.Add(processId.ToString());
                psi.ArgumentList.Add("/F");
                psi.ArgumentList.Add("/T");
                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    string stdout = proc.StandardOutput.ReadToEnd();
                    string stderr = proc.StandardError.ReadToEnd();
                    proc.WaitForExit(5000);

                    if (proc.ExitCode == 0) return true;

                    // If taskkill reports process not found, it has already exited
                    if (stderr.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                        stderr.Contains("nicht gefunden", StringComparison.OrdinalIgnoreCase) ||
                        stdout.Contains("SUCCESS", StringComparison.OrdinalIgnoreCase) ||
                        stdout.Contains("ERFOLGREICH", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch { }

            // 4. Local verification check
            if (IsLocalHost(cleanHost))
            {
                try
                {
                    _ = Process.GetProcessById((int)processId);
                }
                catch (ArgumentException)
                {
                    // Process verified dead
                    return true;
                }
            }

            return false;
        }, cancellationToken);
    }

    private static ComputerProcessSnapshot QueryProcessesWmi(string cleanHost, CancellationToken cancellationToken)
    {
        try
        {
            var wmiScope = CreateManagementScope(cleanHost, @"root\cimv2");
            cancellationToken.ThrowIfCancellationRequested();

            var perfMetrics = new Dictionary<uint, (double Cpu, double Net)>();
            try
            {
                using var perfSearcher = new ManagementObjectSearcher(wmiScope, new ObjectQuery("SELECT IDProcess, PercentProcessorTime, IODataBytesPersec FROM Win32_PerfFormattedData_PerfProc_Process"));
                using var perfResults = perfSearcher.Get();
                foreach (ManagementObject perf in perfResults)
                {
                    using (perf)
                    {
                        uint id = Convert.ToUInt32(perf["IDProcess"] ?? 0);
                        double cpu = Convert.ToDouble(perf["PercentProcessorTime"] ?? 0);
                        ulong ioBytes = Convert.ToUInt64(perf["IODataBytesPersec"] ?? 0);
                        double mbps = (ioBytes * 8.0) / (1024.0 * 1024.0);
                        perfMetrics[id] = (cpu, mbps);
                    }
                }
            }
            catch { }

            var processes = new List<ComputerProcessInfo>();

            using (var searcher = new ManagementObjectSearcher(wmiScope, new ObjectQuery("SELECT ProcessId, Name, ExecutablePath, WorkingSetSize, CreationDate FROM Win32_Process")))
            using (var results = searcher.Get())
            {
                foreach (ManagementObject obj in results)
                {
                    using (obj)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        uint pid = Convert.ToUInt32(obj["ProcessId"] ?? 0);
                        string name = obj["Name"]?.ToString()?.Trim() ?? string.Empty;
                        string execPath = obj["ExecutablePath"]?.ToString()?.Trim() ?? string.Empty;
                        ulong workingSet = Convert.ToUInt64(obj["WorkingSetSize"] ?? 0);
                        string? rawCreation = obj["CreationDate"]?.ToString();
                        DateTime? creationDate = ParseCimDateTime(rawCreation);

                        double cpuVal = 0.0;
                        double netVal = 0.0;
                        if (perfMetrics.TryGetValue(pid, out var metrics))
                        {
                            cpuVal = metrics.Cpu;
                            netVal = metrics.Net;
                        }

                        string owner = string.Empty;
                        try
                        {
                            var outParams = obj.InvokeMethod("GetOwner", null, null) as ManagementBaseObject;
                            if (outParams != null)
                            {
                                string user = outParams["User"]?.ToString() ?? string.Empty;
                                string domain = outParams["Domain"]?.ToString() ?? string.Empty;
                                if (!string.IsNullOrWhiteSpace(user))
                                {
                                    owner = !string.IsNullOrWhiteSpace(domain) ? $"{domain}\\{user}" : user;
                                }
                            }
                        }
                        catch { }

                        processes.Add(new ComputerProcessInfo
                        {
                            ProcessId = pid,
                            Name = name,
                            ExecutablePath = execPath,
                            WorkingSetBytes = workingSet,
                            CpuUsagePercent = cpuVal,
                            NetworkMbps = netVal,
                            Owner = owner,
                            CreationDate = creationDate
                        });
                    }
                }
            }

            return new ComputerProcessSnapshot
            {
                Hostname = cleanHost,
                Processes = processes.OrderByDescending(p => p.WorkingSetBytes).ToList(),
                IsSuccess = true,
                Timestamp = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            return new ComputerProcessSnapshot
            {
                Hostname = cleanHost,
                IsSuccess = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.Now
            };
        }
    }

    private static ComputerProcessSnapshot GetFallbackProcessSnapshot(string cleanHost, string originalError)
    {
        if (IsDemoFixture(cleanHost))
        {
            var list = new List<ComputerProcessInfo>();

            void AddProc(uint pid, string name, string path, ulong memoryMb, string owner, DateTime creation, double cpu = 0.0, double net = 0.0)
            {
                if (_terminatedDemoProcesses.ContainsKey($"{cleanHost}:{pid}")) return;
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

            // System processes
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

            // Interactive User applications
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

    private static ComputerSessionSnapshot QuerySessionsWmi(string cleanHost)
    {
        try
        {
            var wmiScope = CreateManagementScope(cleanHost, @"root\cimv2");
            var sessions = new List<ComputerSessionInfo>();
            var seenUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Step 1: Query Win32_LogonSession
            var logonSessions = new Dictionary<string, (uint LogonType, DateTime? StartTime, uint SessionId)>();
            try
            {
                using var logonSearcher = new ManagementObjectSearcher(wmiScope, new ObjectQuery("SELECT LogonId, LogonType, StartTime FROM Win32_LogonSession WHERE LogonType = 2 OR LogonType = 10 OR LogonType = 11"));
                using var logonResults = logonSearcher.Get();
                foreach (ManagementObject obj in logonResults)
                {
                    using (obj)
                    {
                        string logonId = obj["LogonId"]?.ToString() ?? string.Empty;
                        uint logonType = obj["LogonType"] != null && uint.TryParse(obj["LogonType"].ToString(), out uint lt) ? lt : 2;
                        DateTime? startTime = null;
                        if (obj["StartTime"] != null)
                        {
                            try
                            {
                                startTime = ManagementDateTimeConverter.ToDateTime(obj["StartTime"].ToString());
                            }
                            catch { }
                        }

                        if (!string.IsNullOrWhiteSpace(logonId))
                        {
                            uint sessId = 1;
                            if (uint.TryParse(logonId, out uint parsedId))
                            {
                                sessId = parsedId;
                            }
                            logonSessions[logonId] = (logonType, startTime, sessId);
                        }
                    }
                }
            }
            catch { }

            // Step 2: Associate LoggedOnUser with LogonSession
            try
            {
                using var loggedSearcher = new ManagementObjectSearcher(wmiScope, new ObjectQuery("SELECT Antecedent, Dependent FROM Win32_LoggedOnUser"));
                using var loggedResults = loggedSearcher.Get();
                foreach (ManagementObject obj in loggedResults)
                {
                    using (obj)
                    {
                        string antecedent = obj["Antecedent"]?.ToString() ?? string.Empty;
                        string dependent = obj["Dependent"]?.ToString() ?? string.Empty;

                        string logonId = string.Empty;
                        var matchLogon = Regex.Match(antecedent, @"LogonId=""?(\d+)""?", RegexOptions.IgnoreCase);
                        if (matchLogon.Success)
                        {
                            logonId = matchLogon.Groups[1].Value;
                        }

                        if (!logonSessions.TryGetValue(logonId, out var sessionMeta))
                        {
                            continue;
                        }

                        string domain = string.Empty;
                        string name = string.Empty;

                        var matchDomain = Regex.Match(dependent, @"Domain=""([^""]+)""", RegexOptions.IgnoreCase);
                        if (matchDomain.Success) domain = matchDomain.Groups[1].Value;

                        var matchName = Regex.Match(dependent, @"Name=""([^""]+)""", RegexOptions.IgnoreCase);
                        if (matchName.Success) name = matchName.Groups[1].Value;

                        if (string.IsNullOrWhiteSpace(name)) continue;

                        if (name.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase) ||
                            name.Equals("LOCAL SERVICE", StringComparison.OrdinalIgnoreCase) ||
                            name.Equals("NETWORK SERVICE", StringComparison.OrdinalIgnoreCase) ||
                            name.StartsWith("DWM-", StringComparison.OrdinalIgnoreCase) ||
                            name.StartsWith("UMFD-", StringComparison.OrdinalIgnoreCase) ||
                            name.Equals("ANONYMOUS LOGON", StringComparison.OrdinalIgnoreCase) ||
                            name.StartsWith("Font Driver Host", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string fullKey = $"{domain}\\{name}";
                        if (seenUsers.Contains(fullKey)) continue;
                        seenUsers.Add(fullKey);

                        var sessType = sessionMeta.LogonType == 10
                            ? ComputerSessionType.RemoteDesktop
                            : ComputerSessionType.Console;

                        sessions.Add(new ComputerSessionInfo
                        {
                            SessionId = sessionMeta.SessionId,
                            Username = name,
                            Domain = domain,
                            SamAccountName = name,
                            DisplayName = name,
                            SessionType = sessType,
                            LogonTime = sessionMeta.StartTime,
                            IsActive = true
                        });
                    }
                }
            }
            catch { }

            // Step 3: Fallback query via explorer.exe process owners
            if (sessions.Count == 0)
            {
                try
                {
                    using var procSearcher = new ManagementObjectSearcher(wmiScope, new ObjectQuery("SELECT ProcessId, Name FROM Win32_Process WHERE Name = 'explorer.exe'"));
                    using var procResults = procSearcher.Get();
                    foreach (ManagementObject proc in procResults)
                    {
                        using (proc)
                        {
                            var outParams = proc.InvokeMethod("GetOwner", null, null);
                            if (outParams != null)
                            {
                                string user = outParams["User"]?.ToString() ?? string.Empty;
                                string domain = outParams["Domain"]?.ToString() ?? string.Empty;

                                if (!string.IsNullOrWhiteSpace(user) && !seenUsers.Contains($"{domain}\\{user}"))
                                {
                                    seenUsers.Add($"{domain}\\{user}");
                                    sessions.Add(new ComputerSessionInfo
                                    {
                                        SessionId = 1,
                                        Username = user,
                                        Domain = domain,
                                        SamAccountName = user,
                                        DisplayName = user,
                                        SessionType = ComputerSessionType.Console,
                                        LogonTime = DateTime.Now,
                                        IsActive = true
                                    });
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            return new ComputerSessionSnapshot
            {
                Hostname = cleanHost,
                Sessions = sessions,
                IsSuccess = true,
                QueriedAt = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            return new ComputerSessionSnapshot
            {
                Hostname = cleanHost,
                IsSuccess = false,
                ErrorMessage = ex.Message,
                QueriedAt = DateTime.Now
            };
        }
    }

    private static ComputerSessionSnapshot GetFallbackSessionSnapshot(string cleanHost, string originalError)
    {
        if (IsDemoFixture(cleanHost))
        {
            var sessions = new List<ComputerSessionInfo>();

            if (cleanHost.Contains("DELL", StringComparison.OrdinalIgnoreCase))
            {
                uint sessId = 1;
                if (!_disconnectedDemoSessions.ContainsKey($"{cleanHost}:{sessId}"))
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
                if (!_disconnectedDemoSessions.ContainsKey($"{cleanHost}:{sessId1}"))
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
                if (!_disconnectedDemoSessions.ContainsKey($"{cleanHost}:{sessId2}"))
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
                if (!_disconnectedDemoSessions.ContainsKey($"{cleanHost}:{sessIdDefault}"))
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
