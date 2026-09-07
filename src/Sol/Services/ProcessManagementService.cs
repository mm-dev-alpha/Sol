using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Sol.Helpers;
using Sol.Models;

namespace Sol.Services;

/// <summary>
/// Service implementation for managing processes, sessions, services, and Group Policy updates on computer endpoints.
/// </summary>
public class ProcessManagementService : IProcessManagementService
{
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
        if (ComputerDiagnosticFixtures.IsDemoFixture(cleanHost))
        {
            return ComputerDiagnosticFixtures.GetFallbackSessionSnapshot(cleanHost, "Simulated demo fixture");
        }

        try
        {
            using var timeoutCts = new CancellationTokenSource(DiagnosticScopeHelper.RealMachineTimeout);
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
            if (DiagnosticScopeHelper.IsLocalHost(cleanHost))
            {
                try
                {
                    var wtsSessions = QueryLocalSessionsWts();
                    if (wtsSessions.Count > 0)
                    {
                        return new ComputerSessionSnapshot
                        {
                            Hostname = cleanHost,
                            Sessions = wtsSessions,
                            IsSuccess = true,
                            QueriedAt = DateTime.Now
                        };
                    }
                }
                catch { }
            }

            return ComputerDiagnosticFixtures.GetFallbackSessionSnapshot(cleanHost, ex.Message);
        }
    }

    public async Task DisconnectSessionAsync(string targetHost, uint sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetHost)) return;

        string cleanHost = targetHost.Trim();

        if (ComputerDiagnosticFixtures.IsDemoFixture(cleanHost))
        {
            ComputerDiagnosticFixtures.DisconnectedDemoSessions.TryAdd($"{cleanHost}:{sessionId}", 0);
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
        if (ComputerDiagnosticFixtures.IsDemoFixture(cleanHost))
        {
            return ComputerDiagnosticFixtures.GetFallbackProcessSnapshot(cleanHost, "Simulated demo fixture");
        }

        try
        {
            using var timeoutCts = new CancellationTokenSource(DiagnosticScopeHelper.RealMachineTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var queryTask = Task.Run(() => QueryProcessesWmi(cleanHost, linkedCts.Token), linkedCts.Token);
            var completedTask = await Task.WhenAny(queryTask, Task.Delay(Timeout.Infinite, linkedCts.Token));

            if (completedTask == queryTask)
            {
                var result = await queryTask;
                if (!result.IsSuccess && ComputerDiagnosticFixtures.IsDemoFixture(cleanHost))
                {
                    return ComputerDiagnosticFixtures.GetFallbackProcessSnapshot(cleanHost, result.ErrorMessage ?? "WMI query failed.");
                }
                return result;
            }

            throw new TimeoutException($"Process query timed out after 15 seconds on '{cleanHost}'.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            if (ComputerDiagnosticFixtures.IsDemoFixture(cleanHost))
            {
                return ComputerDiagnosticFixtures.GetFallbackProcessSnapshot(cleanHost, "Connection timed out.");
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
            if (ComputerDiagnosticFixtures.IsDemoFixture(cleanHost))
            {
                return ComputerDiagnosticFixtures.GetFallbackProcessSnapshot(cleanHost, ex.Message);
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

        if (ComputerDiagnosticFixtures.IsDemoFixture(cleanHost))
        {
            ComputerDiagnosticFixtures.TerminatedDemoProcesses.TryAdd($"{cleanHost}:{processId}", 0);
            await Task.Delay(200, cancellationToken);
            return true;
        }

        return await Task.Run(async () =>
        {
            // 1. Direct native execution for local machine with process-tree termination
            if (DiagnosticScopeHelper.IsLocalHost(cleanHost))
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
                var scope = DiagnosticScopeHelper.CreateManagementScope(cleanHost);
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

                        using var inParams = mo.GetMethodParameters("Terminate");
                        inParams["Reason"] = (uint)0;
                        using var outParams = mo.InvokeMethod("Terminate", inParams, null);
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
                if (!DiagnosticScopeHelper.IsLocalHost(cleanHost))
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
                    using var killCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    var stdoutTask = proc.StandardOutput.ReadToEndAsync(killCts.Token);
                    var stderrTask = proc.StandardError.ReadToEndAsync(killCts.Token);
                    var exitTask = proc.WaitForExitAsync(killCts.Token);

                    var completed = await Task.WhenAny(exitTask, Task.Delay(5000)).ConfigureAwait(false);
                    if (completed != exitTask)
                    {
                        try { proc.Kill(entireProcessTree: true); } catch { }
                        killCts.Cancel();
                    }
                    string stdout = string.Empty;
                    string stderr = string.Empty;
                    try
                    {
                        await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
                        stdout = await stdoutTask.ConfigureAwait(false);
                        stderr = await stderrTask.ConfigureAwait(false);
                    }
                    catch { }

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
            if (DiagnosticScopeHelper.IsLocalHost(cleanHost))
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

    public async Task<bool> TriggerGroupPolicyUpdateAsync(string targetHost, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetHost)) return false;
        string cleanHost = targetHost.Trim();

        if (ComputerDiagnosticFixtures.IsDemoFixture(cleanHost))
        {
            await Task.Delay(800, cancellationToken);
            return true;
        }

        return await Task.Run(() =>
        {
            try
            {
                if (DiagnosticScopeHelper.IsLocalHost(cleanHost))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "gpupdate.exe",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    psi.ArgumentList.Add("/force");
                    psi.ArgumentList.Add("/nowait");

                    using var proc = Process.Start(psi);
                    return proc != null;
                }
                else
                {
                    var scope = DiagnosticScopeHelper.CreateManagementScope(cleanHost, @"root\cimv2");
                    using var processClass = new ManagementClass(scope, new ManagementPath("Win32_Process"), null);
                    using var inParams = processClass.GetMethodParameters("Create");
                    inParams["CommandLine"] = "gpupdate.exe /force /nowait";
                    using var outParams = processClass.InvokeMethod("Create", inParams, null);
                    if (outParams != null)
                    {
                        uint returnVal = Convert.ToUInt32(outParams["ReturnValue"] ?? 1);
                        return returnVal == 0;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Remote GPUpdate failed on {cleanHost}: {ex.Message}", ex);
            }
        }, cancellationToken);
    }

    public async Task<ComputerServicesSnapshot> GetServicesSnapshotAsync(string targetHost, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetHost))
        {
            return new ComputerServicesSnapshot
            {
                Hostname = string.Empty,
                IsSuccess = false,
                ErrorMessage = "Target hostname is empty."
            };
        }

        string cleanHost = targetHost.Trim();

        // Fast-path: demo fixtures return mock services immediately
        if (ComputerDiagnosticFixtures.IsDemoFixture(cleanHost))
        {
            return ComputerDiagnosticFixtures.GetFallbackServicesSnapshot(cleanHost, "Simulated demo fixture");
        }

        try
        {
            using var timeoutCts = new CancellationTokenSource(DiagnosticScopeHelper.RealMachineTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var queryTask = Task.Run(() => QueryServicesWmi(cleanHost, linkedCts.Token), linkedCts.Token);
            var completedTask = await Task.WhenAny(queryTask, Task.Delay(Timeout.Infinite, linkedCts.Token));

            if (completedTask == queryTask)
            {
                var result = await queryTask;
                if (!result.IsSuccess && ComputerDiagnosticFixtures.IsDemoFixture(cleanHost))
                {
                    return ComputerDiagnosticFixtures.GetFallbackServicesSnapshot(cleanHost, result.ErrorMessage ?? "WMI service query failed.");
                }
                return result;
            }

            throw new TimeoutException($"Services query timed out after 15 seconds on '{cleanHost}'.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            if (ComputerDiagnosticFixtures.IsDemoFixture(cleanHost))
            {
                return ComputerDiagnosticFixtures.GetFallbackServicesSnapshot(cleanHost, "Connection timed out.");
            }
            return new ComputerServicesSnapshot
            {
                Hostname = cleanHost,
                IsSuccess = false,
                ErrorMessage = "WMI service diagnostic query timed out."
            };
        }
        catch (Exception ex)
        {
            if (ComputerDiagnosticFixtures.IsDemoFixture(cleanHost))
            {
                return ComputerDiagnosticFixtures.GetFallbackServicesSnapshot(cleanHost, ex.Message);
            }
            return new ComputerServicesSnapshot
            {
                Hostname = cleanHost,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> StartServiceAsync(string targetHost, string serviceName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetHost) || string.IsNullOrWhiteSpace(serviceName)) return false;
        if (!IsValidServiceName(serviceName)) return false;
        string cleanHost = targetHost.Trim();
        string cleanServiceName = serviceName.Trim();
        string safeWqlServiceName = EscapeWql(cleanServiceName);

        if (ComputerDiagnosticFixtures.IsDemoFixture(cleanHost))
        {
            ComputerDiagnosticFixtures.DemoServiceStates[$"{cleanHost}:{cleanServiceName}"] = "Running";
            await Task.Delay(300, cancellationToken);
            return true;
        }

        if (DiagnosticScopeHelper.IsLocalHost(cleanHost) && !IsRunningAsAdministrator())
        {
            throw new InvalidOperationException(Strings.S.ServiceLocalElevationRequired);
        }

        return await Task.Run(async () =>
        {
            uint? wmiReturnVal = null;
            string? wmiExceptionMsg = null;

            // 1. On localhost, try WMI method invocation Win32_Service.StartService() (fast ALPC)
            if (DiagnosticScopeHelper.IsLocalHost(cleanHost))
            {
                try
                {
                    var scope = DiagnosticScopeHelper.CreateManagementScope(cleanHost);
                    var enumOptions = new System.Management.EnumerationOptions { ReturnImmediately = true, Timeout = TimeSpan.FromSeconds(5) };
                    using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery($"SELECT Name, State FROM Win32_Service WHERE Name = '{safeWqlServiceName}'"), enumOptions);
                    using var results = searcher.Get();
                    var invokeOptions = new InvokeMethodOptions(null, TimeSpan.FromSeconds(5));
                    foreach (ManagementObject mo in results)
                    {
                        using (mo)
                        {
                            using var outParams = mo.InvokeMethod("StartService", null, invokeOptions);
                            if (outParams != null)
                            {
                                uint returnVal = Convert.ToUInt32(outParams["ReturnValue"] ?? 1);
                                wmiReturnVal = returnVal;
                                // 0 = Success, 10 = Service Already Running
                                if (returnVal == 0 || returnVal == 10) return true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    wmiExceptionMsg = ex.Message;
                }
            }

            // 2. Fallback via sc.exe
            try
            {
                var (exitCode, stdout, stderr) = await RunScCommandAsync(cleanHost, ["start", cleanServiceName], 8000, cancellationToken).ConfigureAwait(false);
                if (exitCode == 0) return true;

                if (stderr.Contains("FAILED 5") || stdout.Contains("FAILED 5") || stderr.Contains("Access is denied") || stdout.Contains("Access is denied"))
                {
                    throw new InvalidOperationException(Strings.S.ServiceAccessDenied);
                }
                if (stderr.Contains("FAILED 1056") || stdout.Contains("FAILED 1056"))
                {
                    return true; // Already running
                }
                if (stderr.Contains("FAILED 1058") || stdout.Contains("FAILED 1058"))
                {
                    throw new InvalidOperationException(Strings.S.ServiceDisabled);
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch { }

            if (wmiReturnVal.HasValue)
            {
                throw new InvalidOperationException(GetWmiServiceErrorMessage(wmiReturnVal.Value));
            }

            if (!string.IsNullOrWhiteSpace(wmiExceptionMsg))
            {
                throw new InvalidOperationException(wmiExceptionMsg);
            }

            return false;
        }, cancellationToken);
    }

    public async Task<bool> StopServiceAsync(string targetHost, string serviceName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetHost) || string.IsNullOrWhiteSpace(serviceName)) return false;
        if (ComputerServiceInfo.IsCritical(serviceName)) return false;
        if (!IsValidServiceName(serviceName)) return false;
        string cleanHost = targetHost.Trim();
        string cleanServiceName = serviceName.Trim();
        string safeWqlServiceName = EscapeWql(cleanServiceName);

        if (ComputerDiagnosticFixtures.IsDemoFixture(cleanHost))
        {
            ComputerDiagnosticFixtures.DemoServiceStates[$"{cleanHost}:{cleanServiceName}"] = "Stopped";
            await Task.Delay(300, cancellationToken);
            return true;
        }

        if (DiagnosticScopeHelper.IsLocalHost(cleanHost) && !IsRunningAsAdministrator())
        {
            throw new InvalidOperationException(Strings.S.ServiceLocalElevationRequired);
        }

        return await Task.Run(async () =>
        {
            uint? wmiReturnVal = null;
            string? wmiExceptionMsg = null;

            // 1. On localhost, try WMI method invocation Win32_Service.StopService() (fast ALPC)
            if (DiagnosticScopeHelper.IsLocalHost(cleanHost))
            {
                try
                {
                    var scope = DiagnosticScopeHelper.CreateManagementScope(cleanHost);
                    var enumOptions = new System.Management.EnumerationOptions { ReturnImmediately = true, Timeout = TimeSpan.FromSeconds(5) };
                    using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery($"SELECT Name, State FROM Win32_Service WHERE Name = '{safeWqlServiceName}'"), enumOptions);
                    using var results = searcher.Get();
                    var invokeOptions = new InvokeMethodOptions(null, TimeSpan.FromSeconds(5));
                    foreach (ManagementObject mo in results)
                    {
                        using (mo)
                        {
                            using var outParams = mo.InvokeMethod("StopService", null, invokeOptions);
                            if (outParams != null)
                            {
                                uint returnVal = Convert.ToUInt32(outParams["ReturnValue"] ?? 1);
                                wmiReturnVal = returnVal;
                                // 0 = Success, 6 = Service Not Active (Already Stopped)
                                if (returnVal == 0 || returnVal == 6) return true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    wmiExceptionMsg = ex.Message;
                }
            }

            // 2. Fallback via sc.exe
            try
            {
                var (exitCode, stdout, stderr) = await RunScCommandAsync(cleanHost, ["stop", cleanServiceName], 8000, cancellationToken).ConfigureAwait(false);
                if (exitCode == 0) return true;

                if (stderr.Contains("FAILED 5") || stdout.Contains("FAILED 5") || stderr.Contains("Access is denied") || stdout.Contains("Access is denied"))
                {
                    throw new InvalidOperationException(Strings.S.ServiceAccessDenied);
                }
                if (stderr.Contains("FAILED 1062") || stdout.Contains("FAILED 1062"))
                {
                    return true; // Already stopped
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch { }

            if (wmiReturnVal.HasValue)
            {
                throw new InvalidOperationException(GetWmiServiceErrorMessage(wmiReturnVal.Value));
            }

            if (!string.IsNullOrWhiteSpace(wmiExceptionMsg))
            {
                throw new InvalidOperationException(wmiExceptionMsg);
            }

            return false;
        }, cancellationToken);
    }

    public async Task<bool> RestartServiceAsync(string targetHost, string serviceName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetHost) || string.IsNullOrWhiteSpace(serviceName)) return false;
        if (ComputerServiceInfo.IsCritical(serviceName)) return false;
        if (!IsValidServiceName(serviceName)) return false;
        string cleanHost = targetHost.Trim();
        string cleanServiceName = serviceName.Trim();

        if (ComputerDiagnosticFixtures.IsDemoFixture(cleanHost))
        {
            ComputerDiagnosticFixtures.DemoServiceStates[$"{cleanHost}:{cleanServiceName}"] = "Stopped";
            await Task.Delay(250, cancellationToken);
            ComputerDiagnosticFixtures.DemoServiceStates[$"{cleanHost}:{cleanServiceName}"] = "Running";
            await Task.Delay(250, cancellationToken);
            return true;
        }

        // Stop first
        await StopServiceAsync(cleanHost, cleanServiceName, cancellationToken);

        // Short delay for clean shutdown
        await Task.Delay(1000, cancellationToken);

        // Start service
        return await StartServiceAsync(cleanHost, cleanServiceName, cancellationToken);
    }

    public async Task<bool> SetServiceStartModeAsync(string targetHost, string serviceName, string startMode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetHost) || string.IsNullOrWhiteSpace(serviceName) || string.IsNullOrWhiteSpace(startMode)) return false;
        if (ComputerServiceInfo.IsCritical(serviceName)) return false;
        if (!IsValidServiceName(serviceName)) return false;
        string cleanHost = targetHost.Trim();
        string cleanServiceName = serviceName.Trim();
        string safeWqlServiceName = EscapeWql(cleanServiceName);

        // Normalize start mode for WMI: "Automatic" -> "Auto", "Manual" -> "Manual", "Disabled" -> "Disabled"
        string normalizedMode = string.Equals(startMode, "Auto", StringComparison.OrdinalIgnoreCase) || string.Equals(startMode, "Automatic", StringComparison.OrdinalIgnoreCase)
            ? "Automatic"
            : (string.Equals(startMode, "Disabled", StringComparison.OrdinalIgnoreCase) ? "Disabled" : "Manual");

        string wmiMode = normalizedMode == "Automatic" ? "Auto" : (normalizedMode == "Disabled" ? "Disabled" : "Manual");

        if (ComputerDiagnosticFixtures.IsDemoFixture(cleanHost))
        {
            ComputerDiagnosticFixtures.DemoServiceStartModes[$"{cleanHost}:{cleanServiceName}"] = wmiMode;
            await Task.Delay(300, cancellationToken);
            return true;
        }

        if (DiagnosticScopeHelper.IsLocalHost(cleanHost) && !IsRunningAsAdministrator())
        {
            throw new InvalidOperationException(Strings.S.ServiceLocalElevationRequired);
        }

        return await Task.Run(async () =>
        {
            uint? wmiReturnVal = null;
            string? wmiExceptionMsg = null;

            // 1. On localhost, try WMI method invocation Win32_Service.ChangeStartMode(StartMode) (fast ALPC)
            if (DiagnosticScopeHelper.IsLocalHost(cleanHost))
            {
                try
                {
                    var scope = DiagnosticScopeHelper.CreateManagementScope(cleanHost);
                    var enumOptions = new System.Management.EnumerationOptions { ReturnImmediately = true, Timeout = TimeSpan.FromSeconds(5) };
                    using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery($"SELECT Name, State FROM Win32_Service WHERE Name = '{safeWqlServiceName}'"), enumOptions);
                    using var results = searcher.Get();
                    var invokeOptions = new InvokeMethodOptions(null, TimeSpan.FromSeconds(5));
                    foreach (ManagementObject mo in results)
                    {
                        using (mo)
                        {
                            using var inParams = mo.GetMethodParameters("ChangeStartMode");
                            inParams["StartMode"] = normalizedMode;
                            using var outParams = mo.InvokeMethod("ChangeStartMode", inParams, invokeOptions);
                            if (outParams != null)
                            {
                                uint returnVal = Convert.ToUInt32(outParams["ReturnValue"] ?? 1);
                                wmiReturnVal = returnVal;
                                if (returnVal == 0) return true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    wmiExceptionMsg = ex.Message;
                }
            }

            // 2. Fallback via sc.exe config <service> start= <boot|system|auto|demand|disabled|delayed-auto>
            try
            {
                string scStartType = wmiMode switch
                {
                    "Auto" => "auto",
                    "Disabled" => "disabled",
                    _ => "demand"
                };

                var (exitCode, stdout, stderr) = await RunScCommandAsync(cleanHost, ["config", cleanServiceName, "start=", scStartType], 8000, cancellationToken).ConfigureAwait(false);
                if (exitCode == 0) return true;

                if (stderr.Contains("FAILED 5") || stdout.Contains("FAILED 5") || stderr.Contains("Access is denied") || stdout.Contains("Access is denied"))
                {
                    throw new InvalidOperationException(Strings.S.ServiceAccessDenied);
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch { }

            if (wmiReturnVal.HasValue)
            {
                throw new InvalidOperationException(GetWmiServiceErrorMessage(wmiReturnVal.Value));
            }

            if (!string.IsNullOrWhiteSpace(wmiExceptionMsg))
            {
                throw new InvalidOperationException(wmiExceptionMsg);
            }

            return false;
        }, cancellationToken);
    }

    public static async Task<(int ExitCode, string StdOut, string StdErr)> RunScCommandAsync(
        string cleanHost,
        IReadOnlyList<string> commandArgs,
        int timeoutMs = 8000,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sc.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            if (!DiagnosticScopeHelper.IsLocalHost(cleanHost))
            {
                psi.ArgumentList.Add($"\\\\{cleanHost}");
            }

            foreach (var arg in commandArgs)
            {
                psi.ArgumentList.Add(arg);
            }

            using var proc = Process.Start(psi);
            if (proc == null)
            {
                return (-1, string.Empty, "Failed to start sc.exe process.");
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var stdoutTask = proc.StandardOutput.ReadToEndAsync(cts.Token);
            var stderrTask = proc.StandardError.ReadToEndAsync(cts.Token);
            var exitTask = proc.WaitForExitAsync(cts.Token);

            var timeoutTask = Task.Delay(timeoutMs, cts.Token);
            var completedTask = await Task.WhenAny(exitTask, timeoutTask).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
            {
                try { proc.Kill(entireProcessTree: true); } catch { }
                cts.Cancel();
                try { await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false); } catch { }
                return (-1, string.Empty, "Operation cancelled.");
            }

            if (completedTask == exitTask && !exitTask.IsCanceled)
            {
                await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
                return (proc.ExitCode, await stdoutTask.ConfigureAwait(false), await stderrTask.ConfigureAwait(false));
            }

            // Timed out or cancelled
            try
            {
                proc.Kill(entireProcessTree: true);
            }
            catch { }

            cts.Cancel();
            try
            {
                await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
            }
            catch { }

            return (-1, string.Empty, cancellationToken.IsCancellationRequested ? "Operation cancelled." : "Execution timed out.");
        }
        catch (OperationCanceledException)
        {
            return (-1, string.Empty, "Operation cancelled.");
        }
        catch (Exception ex)
        {
            return (-1, string.Empty, ex.Message);
        }
    }

    private static readonly Regex ServiceNameRegex = new(
        @"^[a-zA-Z0-9_\-.$ ]{1,256}$",
        RegexOptions.Compiled);

    public static bool IsValidServiceName(string? serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName)) return false;
        return ServiceNameRegex.IsMatch(serviceName.Trim());
    }

    private static string EscapeWql(string input)
    {
        return input.Replace(@"\", @"\\").Replace("'", @"\'");
    }

    private static bool IsRunningAsAdministrator()
    {
        try
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static string GetWmiServiceErrorMessage(uint returnVal)
    {
        return returnVal switch
        {
            0 => string.Empty,
            1 => Strings.S.ServiceNotSupported,
            2 => Strings.S.ServiceAccessDenied,
            3 => Strings.S.ServiceDependentServicesRunning,
            4 => Strings.S.ServiceInvalidControl,
            5 => Strings.S.ServiceCannotAcceptControl,
            6 => Strings.S.ServiceAlreadyStopped,
            7 => Strings.S.ServiceRequestTimeout,
            10 => Strings.S.ServiceAlreadyRunning,
            14 => Strings.S.ServiceDisabled,
            15 => Strings.S.ServiceLogonFailed,
            21 => Strings.S.ServiceInvalidParameter,
            _ => $"WMI Error ({returnVal})"
        };
    }

    private static ComputerProcessSnapshot QueryProcessesWmi(string cleanHost, CancellationToken cancellationToken)
    {
        try
        {
            var wmiScope = DiagnosticScopeHelper.CreateManagementScope(cleanHost, @"root\cimv2");
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
                        DateTime? creationDate = HardwareDiagnosticService.ParseCimDateTime(rawCreation);

                        double cpuVal = 0.0;
                        double netVal = 0.0;
                        if (perfMetrics.TryGetValue(pid, out var metrics))
                        {
                            cpuVal = metrics.Cpu;
                            netVal = metrics.Net;
                        }

                        string owner = string.Empty;
                        bool isLocal = DiagnosticScopeHelper.IsLocalHost(cleanHost);
                        if (pid <= 4 || ComputerProcessInfo.IsCriticalProcess(pid, name))
                        {
                            owner = "NT AUTHORITY\\SYSTEM";
                        }
                        else if (isLocal || cpuVal > 0.1 || workingSet > 25 * 1024 * 1024 || string.Equals(name, "explorer.exe", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                using var outParams = obj.InvokeMethod("GetOwner", null, null) as ManagementBaseObject;
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
                        }

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

    private static ComputerSessionSnapshot QuerySessionsWmi(string cleanHost)
    {
        try
        {
            var wmiScope = DiagnosticScopeHelper.CreateManagementScope(cleanHost, @"root\cimv2");
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
                        var matchLogon = Regex.Match(dependent, @"LogonId=""?(\d+)""?", RegexOptions.IgnoreCase);
                        if (!matchLogon.Success)
                        {
                            matchLogon = Regex.Match(antecedent, @"LogonId=""?(\d+)""?", RegexOptions.IgnoreCase);
                        }

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

                        var matchDomain = Regex.Match(antecedent, @"Domain=""([^""]+)""", RegexOptions.IgnoreCase);
                        if (!matchDomain.Success)
                        {
                            matchDomain = Regex.Match(dependent, @"Domain=""([^""]+)""", RegexOptions.IgnoreCase);
                        }
                        if (matchDomain.Success) domain = matchDomain.Groups[1].Value;

                        var matchName = Regex.Match(antecedent, @"Name=""([^""]+)""", RegexOptions.IgnoreCase);
                        if (!matchName.Success)
                        {
                            matchName = Regex.Match(dependent, @"Name=""([^""]+)""", RegexOptions.IgnoreCase);
                        }
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

            // Step 2b: Fallback to Win32_ComputerSystem.UserName
            if (sessions.Count == 0)
            {
                try
                {
                    using var csSearcher = new ManagementObjectSearcher(wmiScope, new ObjectQuery("SELECT UserName FROM Win32_ComputerSystem"));
                    using var csResults = csSearcher.Get();
                    foreach (ManagementObject csObj in csResults)
                    {
                        using (csObj)
                        {
                            string rawUser = csObj["UserName"]?.ToString() ?? string.Empty;
                            if (!string.IsNullOrWhiteSpace(rawUser))
                            {
                                string dom = string.Empty;
                                string usr = rawUser.Trim();
                                int slashIdx = usr.IndexOf('\\');
                                if (slashIdx >= 0)
                                {
                                    dom = usr.Substring(0, slashIdx);
                                    usr = usr.Substring(slashIdx + 1);
                                }

                                string key = $"{dom}\\{usr}";
                                if (!string.IsNullOrWhiteSpace(usr) && !seenUsers.Contains(key))
                                {
                                    seenUsers.Add(key);
                                    sessions.Add(new ComputerSessionInfo
                                    {
                                        SessionId = 1,
                                        Username = usr,
                                        Domain = dom,
                                        SamAccountName = usr,
                                        DisplayName = usr,
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
                            using var outParams = proc.InvokeMethod("GetOwner", null, null);
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

            // Step 4: For local host, complement with native WTS enumeration
            if (DiagnosticScopeHelper.IsLocalHost(cleanHost) && sessions.Count == 0)
            {
                var wtsSessions = QueryLocalSessionsWts();
                foreach (var ws in wtsSessions)
                {
                    string key = $"{ws.Domain}\\{ws.Username}";
                    if (!string.IsNullOrWhiteSpace(ws.Username) && !seenUsers.Contains(key))
                    {
                        seenUsers.Add(key);
                        sessions.Add(ws);
                    }
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

    private static ComputerServicesSnapshot QueryServicesWmi(string cleanHost, CancellationToken cancellationToken)
    {
        var services = new List<ComputerServiceInfo>();
        try
        {
            var scope = DiagnosticScopeHelper.CreateManagementScope(cleanHost);
            using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT Name, DisplayName, State, StartMode, StartName, AcceptStop, AcceptPause, PathName, Description, ProcessId FROM Win32_Service"));
            using var results = searcher.Get();

            foreach (ManagementObject mo in results)
            {
                if (cancellationToken.IsCancellationRequested) break;
                using (mo)
                {
                    string name = mo["Name"]?.ToString() ?? string.Empty;
                    string displayName = mo["DisplayName"]?.ToString() ?? name;
                    string state = mo["State"]?.ToString() ?? "Stopped";
                    string startMode = mo["StartMode"]?.ToString() ?? "Manual";
                    string startName = mo["StartName"]?.ToString() ?? string.Empty;
                    bool acceptStop = Convert.ToBoolean(mo["AcceptStop"] ?? false);
                    bool acceptPause = Convert.ToBoolean(mo["AcceptPause"] ?? false);
                    string pathName = mo["PathName"]?.ToString() ?? string.Empty;
                    string description = mo["Description"]?.ToString() ?? string.Empty;
                    uint processId = 0;
                    if (mo["ProcessId"] != null)
                    {
                        try { processId = Convert.ToUInt32(mo["ProcessId"]); } catch { }
                    }

                    services.Add(new ComputerServiceInfo
                    {
                        Name = name,
                        DisplayName = string.IsNullOrWhiteSpace(displayName) ? name : displayName,
                        State = state,
                        StartMode = startMode,
                        StartName = startName,
                        AcceptStop = acceptStop,
                        AcceptPause = acceptPause,
                        PathName = pathName,
                        Description = description,
                        ProcessId = processId
                    });
                }
            }

            var sorted = services.OrderBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
            return new ComputerServicesSnapshot
            {
                Hostname = cleanHost,
                Services = sorted,
                IsSuccess = true,
                Timestamp = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            return new ComputerServicesSnapshot
            {
                Hostname = cleanHost,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    #region Win32 WTS Session Enumeration

    [StructLayout(LayoutKind.Sequential)]
    private struct WTS_SESSION_INFO
    {
        public int SessionId;
        public IntPtr pWinStationName;
        public int State;
    }

    private enum WTS_INFO_CLASS
    {
        WTSUserName = 5,
        WTSDomainName = 7
    }

    [DllImport("wtsapi32.dll", EntryPoint = "WTSEnumerateSessionsW", SetLastError = true)]
    private static extern bool WTSEnumerateSessions(
        IntPtr hServer,
        int reserved,
        int version,
        out IntPtr ppSessionInfo,
        out int pCount);

    [DllImport("wtsapi32.dll", EntryPoint = "WTSQuerySessionInformationW", SetLastError = true)]
    private static extern bool WTSQuerySessionInformation(
        IntPtr hServer,
        int sessionId,
        WTS_INFO_CLASS wtsInfoClass,
        out IntPtr ppBuffer,
        out int pBytesReturned);

    [DllImport("wtsapi32.dll")]
    private static extern void WTSFreeMemory(IntPtr pMemory);

    public static List<ComputerSessionInfo> QueryLocalSessionsWts()
    {
        var list = new List<ComputerSessionInfo>();
        IntPtr pSessionInfo = IntPtr.Zero;
        int count = 0;

        if (!WTSEnumerateSessions(IntPtr.Zero, 0, 1, out pSessionInfo, out count))
        {
            return list;
        }

        try
        {
            int structSize = Marshal.SizeOf<WTS_SESSION_INFO>();
            for (int i = 0; i < count; i++)
            {
                IntPtr current = IntPtr.Add(pSessionInfo, i * structSize);
                var sessionInfo = Marshal.PtrToStructure<WTS_SESSION_INFO>(current);

                string stationName = sessionInfo.pWinStationName != IntPtr.Zero
                    ? Marshal.PtrToStringUni(sessionInfo.pWinStationName) ?? string.Empty
                    : string.Empty;

                string userName = QueryWtsString(sessionInfo.SessionId, WTS_INFO_CLASS.WTSUserName);
                string domain = QueryWtsString(sessionInfo.SessionId, WTS_INFO_CLASS.WTSDomainName);

                if (string.IsNullOrWhiteSpace(userName) ||
                    userName.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase) ||
                    userName.Equals("LOCAL SERVICE", StringComparison.OrdinalIgnoreCase) ||
                    userName.Equals("NETWORK SERVICE", StringComparison.OrdinalIgnoreCase) ||
                    userName.StartsWith("DWM-", StringComparison.OrdinalIgnoreCase) ||
                    userName.StartsWith("UMFD-", StringComparison.OrdinalIgnoreCase) ||
                    userName.Equals("ANONYMOUS LOGON", StringComparison.OrdinalIgnoreCase) ||
                    userName.StartsWith("Font Driver Host", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var sessType = sessionInfo.State == 4
                    ? ComputerSessionType.Disconnected
                    : (stationName.Equals("Console", StringComparison.OrdinalIgnoreCase)
                        ? ComputerSessionType.Console
                        : ComputerSessionType.RemoteDesktop);

                list.Add(new ComputerSessionInfo
                {
                    SessionId = (uint)sessionInfo.SessionId,
                    Username = userName,
                    Domain = domain,
                    SamAccountName = userName,
                    DisplayName = userName,
                    SessionType = sessType,
                    LogonTime = DateTime.Now,
                    IsActive = sessionInfo.State == 0
                });
            }
        }
        catch { }
        finally
        {
            if (pSessionInfo != IntPtr.Zero)
            {
                WTSFreeMemory(pSessionInfo);
            }
        }

        return list;
    }

    private static string QueryWtsString(int sessionId, WTS_INFO_CLASS infoClass)
    {
        if (WTSQuerySessionInformation(IntPtr.Zero, sessionId, infoClass, out IntPtr pBuffer, out int bytesReturned))
        {
            try
            {
                if (pBuffer != IntPtr.Zero && bytesReturned > 0)
                {
                    return Marshal.PtrToStringUni(pBuffer) ?? string.Empty;
                }
            }
            finally
            {
                if (pBuffer != IntPtr.Zero)
                {
                    WTSFreeMemory(pBuffer);
                }
            }
        }
        return string.Empty;
    }

    #endregion
}
