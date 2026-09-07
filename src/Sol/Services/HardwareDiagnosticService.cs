using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using Sol.Helpers;
using Sol.Models;

namespace Sol.Services;

/// <summary>
/// Service implementation for querying live hardware, BIOS, uptime, storage, and power diagnostics from computer endpoints.
/// </summary>
public class HardwareDiagnosticService : IHardwareDiagnosticService
{
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

        if (ComputerDiagnosticFixtures.IsDemoFixture(cleanHost))
        {
            await Task.Delay(100, cancellationToken);
            return ComputerDiagnosticFixtures.GetFallbackUptimeSnapshot(cleanHost, string.Empty);
        }

        var wmiTask = Task.Run(() => QueryUptimeWmi(cleanHost, cancellationToken), cancellationToken);
        var delayTask = Task.Delay(DiagnosticScopeHelper.RealMachineTimeout, cancellationToken);

        var completedTask = await Task.WhenAny(wmiTask, delayTask);
        if (completedTask == delayTask)
        {
            _ = wmiTask.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
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

        if (ComputerDiagnosticFixtures.IsDemoFixture(cleanHost))
        {
            await Task.Delay(100, cancellationToken);
            return ComputerDiagnosticFixtures.GetFallbackHardwareSnapshot(cleanHost, string.Empty);
        }

        var wmiTask = Task.Run(() => QueryHardwareWmi(cleanHost, cancellationToken), cancellationToken);
        var delayTask = Task.Delay(DiagnosticScopeHelper.RealMachineTimeout, cancellationToken);

        var completedTask = await Task.WhenAny(wmiTask, delayTask);
        if (completedTask == delayTask)
        {
            _ = wmiTask.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
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

        if (ComputerDiagnosticFixtures.IsDemoFixture(cleanHost))
        {
            await Task.Delay(100, cancellationToken);
            return ComputerDiagnosticFixtures.GetFallbackDiskSnapshot(cleanHost, string.Empty);
        }

        var wmiTask = Task.Run(() => QueryDiskWmi(cleanHost, cancellationToken), cancellationToken);
        var delayTask = Task.Delay(DiagnosticScopeHelper.RealMachineTimeout, cancellationToken);

        var completedTask = await Task.WhenAny(wmiTask, delayTask);
        if (completedTask == delayTask)
        {
            _ = wmiTask.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
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

        if (ComputerDiagnosticFixtures.IsDemoFixture(cleanHost))
        {
            await Task.Delay(100, cancellationToken);
            return ComputerDiagnosticFixtures.GetFallbackBatterySnapshot(cleanHost, string.Empty);
        }

        var wmiTask = Task.Run(() => QueryBatteryWmi(cleanHost, cancellationToken), cancellationToken);
        var delayTask = Task.Delay(DiagnosticScopeHelper.RealMachineTimeout, cancellationToken);

        var completedTask = await Task.WhenAny(wmiTask, delayTask);
        if (completedTask == delayTask)
        {
            _ = wmiTask.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
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
            var scope = DiagnosticScopeHelper.CreateManagementScope(cleanHost);

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
        catch (OperationCanceledException)
        {
            throw;
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
            var scope = DiagnosticScopeHelper.CreateManagementScope(cleanHost);

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
        catch (OperationCanceledException)
        {
            throw;
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
            var scope = DiagnosticScopeHelper.CreateManagementScope(cleanHost);

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
                var storageScope = DiagnosticScopeHelper.CreateManagementScope(cleanHost, @"root\Microsoft\Windows\Storage");
                using var pSearcher = new ManagementObjectSearcher(storageScope, new ObjectQuery("SELECT FriendlyName, MediaType, HealthStatus, BusType FROM MSFT_PhysicalDisk"));
                using var pResults = pSearcher.Get();
                foreach (ManagementObject pDisk in pResults)
                {
                    using (pDisk)
                    {
                        var bus = pDisk["BusType"]?.ToString()?.Trim() ?? string.Empty;
                        var mediaTypeRaw = pDisk["MediaType"]?.ToString()?.Trim();
                        var hStatus = pDisk["HealthStatus"]?.ToString()?.Trim();
                        string health = (hStatus == "0" || hStatus == "Healthy") ? "OK" : (hStatus == "1" ? "Warning" : (hStatus == "2" ? "Pred Fail" : "OK"));

                        bool isNvme = bus == "17" || bus.Equals("NVMe", StringComparison.OrdinalIgnoreCase);
                        bool isSata = bus == "11" || bus.Equals("SATA", StringComparison.OrdinalIgnoreCase) || bus.Equals("ATA", StringComparison.OrdinalIgnoreCase);
                        bool isSas = bus == "10" || bus.Equals("SAS", StringComparison.OrdinalIgnoreCase);

                        string media;
                        if (mediaTypeRaw == "3" || mediaTypeRaw?.Equals("HDD", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            media = isSata ? "SATA HDD" : (isSas ? "SAS HDD" : "HDD");
                        }
                        else if (mediaTypeRaw == "4" || mediaTypeRaw?.Equals("SSD", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            media = isNvme ? "NVMe SSD" : (isSata ? "SATA SSD" : "SSD");
                        }
                        else if (mediaTypeRaw == "5" || mediaTypeRaw?.Equals("SCM", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            media = "SCM";
                        }
                        else
                        {
                            media = isNvme ? "NVMe SSD" : (isSata ? "SATA SSD" : "Fixed Disk");
                        }

                        var name = pDisk["FriendlyName"]?.ToString()?.Trim() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            physicalDisks.Add((name, media, health));
                        }
                    }
                }
            }
            catch { }

            string defaultMediaType = physicalDisks.Count > 0 ? physicalDisks[0].MediaType : "Fixed Disk";
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
        catch (OperationCanceledException)
        {
            throw;
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

    public static string FormatCimDateTime(string cimDateTime)
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
            using var baseKey = DiagnosticScopeHelper.IsLocalHost(host)
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

    private static ComputerBatterySnapshot QueryBatteryWmi(string cleanHost, CancellationToken cancellationToken)
    {
        try
        {
            var scope = DiagnosticScopeHelper.CreateManagementScope(cleanHost);

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
                var wmiScope = DiagnosticScopeHelper.CreateManagementScope(cleanHost, @"root\wmi");
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
        catch (OperationCanceledException)
        {
            throw;
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
}
