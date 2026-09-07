using System;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using Sol.Models;

namespace Sol.Services;

/// <summary>
/// Implements BitLocker encryption status querying, suspension, and resumption for computer endpoints.
/// </summary>
public class BitLockerManagementService : IBitLockerManagementService
{
    public async Task<ComputerBitLockerSnapshot> GetBitLockerStatusAsync(string targetHost, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetHost))
        {
            return new ComputerBitLockerSnapshot
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
            bool isSusp = ComputerDiagnosticFixtures.DemoBitLockerSuspendedState.TryGetValue(cleanHost, out bool s) && s;
            return new ComputerBitLockerSnapshot
            {
                Hostname = cleanHost,
                DriveLetter = "C:",
                ProtectionStatus = (uint)(isSusp ? 0 : 1),
                ConversionStatus = 1,
                EncryptionMethod = 7, // XTS-AES 256
                IsSuspended = isSusp,
                IsSuccess = true,
                Timestamp = DateTime.Now
            };
        }

        var wmiTask = Task.Run(() => QueryBitLockerWmi(cleanHost), cancellationToken);
        var delayTask = Task.Delay(DiagnosticScopeHelper.RealMachineTimeout, cancellationToken);

        var completedTask = await Task.WhenAny(wmiTask, delayTask);
        if (completedTask == delayTask)
        {
            _ = wmiTask.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
            return new ComputerBitLockerSnapshot
            {
                Hostname = cleanHost,
                IsSuccess = false,
                ErrorMessage = "Timeout (15s): Endpoint unreachable",
                Timestamp = DateTime.Now
            };
        }

        return await wmiTask;
    }

    private static ComputerBitLockerSnapshot QueryBitLockerWmi(string cleanHost)
    {
        try
        {
            var scope = DiagnosticScopeHelper.CreateManagementScope(cleanHost, @"root\cimv2\Security\MicrosoftVolumeEncryption");
            using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT DriveLetter, ProtectionStatus, ConversionStatus, EncryptionMethod FROM Win32_EncryptableVolume WHERE DriveLetter = 'C:'"));
            using var collection = searcher.Get();

            foreach (ManagementObject mo in collection)
            {
                using (mo)
                {
                    string driveLetter = mo["DriveLetter"]?.ToString() ?? "C:";
                    uint protectionStatus = mo["ProtectionStatus"] != null ? Convert.ToUInt32(mo["ProtectionStatus"]) : 1;
                    uint conversionStatus = mo["ConversionStatus"] != null ? Convert.ToUInt32(mo["ConversionStatus"]) : 1;
                    uint encryptionMethod = mo["EncryptionMethod"] != null ? Convert.ToUInt32(mo["EncryptionMethod"]) : 7;

                    return new ComputerBitLockerSnapshot
                    {
                        Hostname = cleanHost,
                        DriveLetter = driveLetter,
                        ProtectionStatus = protectionStatus,
                        ConversionStatus = conversionStatus,
                        EncryptionMethod = encryptionMethod,
                        IsSuspended = protectionStatus == 0,
                        IsSuccess = true,
                        Timestamp = DateTime.Now
                    };
                }
            }

            return new ComputerBitLockerSnapshot
            {
                Hostname = cleanHost,
                DriveLetter = "C:",
                ProtectionStatus = 0,
                ConversionStatus = 0,
                EncryptionMethod = 0,
                IsSuccess = true,
                Timestamp = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            return new ComputerBitLockerSnapshot
            {
                Hostname = cleanHost,
                IsSuccess = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.Now
            };
        }
    }

    public async Task<bool> SuspendBitLockerProtectionAsync(string targetHost, uint rebootCount = 1, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetHost)) return false;
        string cleanHost = targetHost.Trim();

        if (ComputerDiagnosticFixtures.IsDemoFixture(cleanHost))
        {
            ComputerDiagnosticFixtures.DemoBitLockerSuspendedState[cleanHost] = true;
            await Task.Delay(300, cancellationToken);
            return true;
        }

        return await Task.Run(() =>
        {
            try
            {
                var scope = DiagnosticScopeHelper.CreateManagementScope(cleanHost, @"root\cimv2\Security\MicrosoftVolumeEncryption");
                using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT DeviceID, DriveLetter FROM Win32_EncryptableVolume WHERE DriveLetter = 'C:'"));
                using var collection = searcher.Get();

                foreach (ManagementObject mo in collection)
                {
                    using (mo)
                    {
                        using var inParams = mo.GetMethodParameters("DisableKeyProtectors");
                        inParams["DisableCount"] = rebootCount;
                        using var outParams = mo.InvokeMethod("DisableKeyProtectors", inParams, null);
                        if (outParams != null)
                        {
                            uint returnVal = Convert.ToUInt32(outParams["ReturnValue"] ?? 1);
                            return returnVal == 0;
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to suspend BitLocker on {cleanHost}: {ex.Message}", ex);
            }
        }, cancellationToken);
    }

    public async Task<bool> ResumeBitLockerProtectionAsync(string targetHost, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetHost)) return false;
        string cleanHost = targetHost.Trim();

        if (ComputerDiagnosticFixtures.IsDemoFixture(cleanHost))
        {
            ComputerDiagnosticFixtures.DemoBitLockerSuspendedState[cleanHost] = false;
            await Task.Delay(300, cancellationToken);
            return true;
        }

        return await Task.Run(() =>
        {
            try
            {
                var scope = DiagnosticScopeHelper.CreateManagementScope(cleanHost, @"root\cimv2\Security\MicrosoftVolumeEncryption");
                using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT DeviceID, DriveLetter FROM Win32_EncryptableVolume WHERE DriveLetter = 'C:'"));
                using var collection = searcher.Get();

                foreach (ManagementObject mo in collection)
                {
                    using (mo)
                    {
                        using var outParams = mo.InvokeMethod("EnableKeyProtectors", null, null);
                        if (outParams != null)
                        {
                            uint returnVal = Convert.ToUInt32(outParams["ReturnValue"] ?? 1);
                            return returnVal == 0;
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to resume BitLocker on {cleanHost}: {ex.Message}", ex);
            }
        }, cancellationToken);
    }
}
