using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Sol.Helpers;
using Sol.Models;

namespace Sol.Services;

public class FileLocksmithService : IFileLocksmithService
{
    private const int CCH_RM_MAX_APP_NAME = 255;
    private const int CCH_RM_MAX_SVC_NAME = 63;
    private const int ERROR_MORE_DATA = 234;

    [StructLayout(LayoutKind.Sequential)]
    private struct RM_UNIQUE_PROCESS
    {
        public int dwProcessId;
        public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct RM_PROCESS_INFO
    {
        public RM_UNIQUE_PROCESS Process;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)]
        public string strAppName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
        public string strServiceShortName;
        public int ApplicationType;
        public uint AppStatus;
        public uint TSSessionId;
        [MarshalAs(UnmanagedType.Bool)]
        public bool bRestartable;
    }

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmEndSession(uint pSessionHandle);

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmRegisterResources(
        uint pSessionHandle,
        uint nFiles,
        string[] rgsFilenames,
        uint nApplications,
        [In] RM_UNIQUE_PROCESS[]? rgApplications,
        uint nServices,
        string[]? rgsServiceNames);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmGetList(
        uint dwSessionHandle,
        out uint pnProcInfoNeeded,
        ref uint pnProcInfo,
        [In, Out] RM_PROCESS_INFO[]? rgAffectedApps,
        out uint lpdwRebootReasons);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
    private const uint TOKEN_QUERY = 0x0008;

    private const string ShellKeyFile = @"Software\Classes\*\shell\SolFileLocksmith";
    private const string ShellKeyDirectory = @"Software\Classes\Directory\shell\SolFileLocksmith";

    public Task<IReadOnlyList<LockingProcessInfo>> FindLockingProcessesAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Task.FromResult<IReadOnlyList<LockingProcessInfo>>([]);

        return FindLockingProcessesAsync(new[] { path }, cancellationToken);
    }

    public Task<IReadOnlyList<LockingProcessInfo>> FindLockingProcessesAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<LockingProcessInfo>>(() =>
        {
            var validPaths = paths.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
            if (validPaths.Length == 0)
                return [];

            string sessionKey = Guid.NewGuid().ToString();
            int res = RmStartSession(out uint sessionHandle, 0, sessionKey);
            if (res != 0)
                return [];

            try
            {
                res = RmRegisterResources(sessionHandle, (uint)validPaths.Length, validPaths, 0, null, 0, null);
                if (res != 0)
                    return [];

                uint pnProcInfoNeeded = 0;
                uint pnProcInfo = 0;
                uint lpdwRebootReasons = 0;

                res = RmGetList(sessionHandle, out pnProcInfoNeeded, ref pnProcInfo, null, out lpdwRebootReasons);
                if (res == ERROR_MORE_DATA && pnProcInfoNeeded > 0)
                {
                    var processInfo = new RM_PROCESS_INFO[pnProcInfoNeeded];
                    pnProcInfo = pnProcInfoNeeded;

                    res = RmGetList(sessionHandle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, out lpdwRebootReasons);
                    if (res == 0)
                    {
                        var results = new List<LockingProcessInfo>();
                        for (int i = 0; i < pnProcInfo; i++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            int pid = processInfo[i].Process.dwProcessId;
                            string procName = processInfo[i].strAppName;
                            string appPath = string.Empty;
                            string user = string.Empty;

                            try
                            {
                                using var p = Process.GetProcessById(pid);
                                if (string.IsNullOrEmpty(procName))
                                {
                                    procName = p.ProcessName;
                                }

                                try
                                {
                                    appPath = p.MainModule?.FileName ?? string.Empty;
                                }
                                catch { }

                                user = ResolveProcessUser(pid);
                            }
                            catch
                            {
                                if (string.IsNullOrEmpty(procName))
                                {
                                    procName = $"PID: {pid}";
                                }
                            }

                            results.Add(new LockingProcessInfo(
                                pid,
                                procName,
                                appPath,
                                string.IsNullOrEmpty(user) ? "-" : user,
                                validPaths));
                        }

                        return results;
                    }
                }

                return [];
            }
            finally
            {
                RmEndSession(sessionHandle);
            }
        }, cancellationToken);
    }

    private static string ResolveProcessUser(int processId)
    {
        IntPtr hProcess = IntPtr.Zero;
        IntPtr hToken = IntPtr.Zero;

        try
        {
            hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (hProcess == IntPtr.Zero) return string.Empty;

            if (OpenProcessToken(hProcess, TOKEN_QUERY, out hToken) && hToken != IntPtr.Zero)
            {
                using var identity = new WindowsIdentity(hToken);
                return identity.Name;
            }
        }
        catch { }
        finally
        {
            if (hToken != IntPtr.Zero) CloseHandle(hToken);
            if (hProcess != IntPtr.Zero) CloseHandle(hProcess);
        }

        return string.Empty;
    }

    public bool KillProcess(int processId, out string? errorMessage)
    {
        if (processId <= 4)
        {
            errorMessage = Strings.S.CriticalProcessCannotBeTerminated;
            return false;
        }

        try
        {
            using var proc = Process.GetProcessById(processId);
            if (ComputerProcessInfo.IsCriticalProcess((uint)processId, proc.ProcessName))
            {
                errorMessage = Strings.S.CriticalProcessCannotBeTerminated;
                return false;
            }

            proc.Kill(entireProcessTree: true);
            proc.WaitForExit(3000);
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    public bool KillAllProcesses(IEnumerable<int> processIds, out List<string> errors)
    {
        errors = [];
        bool allSucceeded = true;

        foreach (var pid in processIds.Distinct())
        {
            if (!KillProcess(pid, out var err))
            {
                allSucceeded = false;
                if (!string.IsNullOrEmpty(err))
                {
                    errors.Add($"PID {pid}: {err}");
                }
            }
        }

        return allSucceeded;
    }

    public bool IsContextMenuRegistered()
    {
        try
        {
            using var key1 = Registry.CurrentUser.OpenSubKey(ShellKeyFile);
            using var key2 = Registry.CurrentUser.OpenSubKey(ShellKeyDirectory);
            return key1 != null && key2 != null;
        }
        catch
        {
            return false;
        }
    }

    public bool SetContextMenuRegistered(bool enable)
    {
        try
        {
            if (enable)
            {
                string exePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "Sol.exe");
                string command = $"\"{exePath}\" --unlock \"%1\"";
                string title = Strings.S.FileLocksmithContextMenuEntry;

                RegisterKey(ShellKeyFile, title, exePath, command);
                RegisterKey(ShellKeyDirectory, title, exePath, command);
                return true;
            }
            else
            {
                UnregisterKey(ShellKeyFile);
                UnregisterKey(ShellKeyDirectory);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    private static void RegisterKey(string subKey, string title, string iconPath, string command)
    {
        using var key = Registry.CurrentUser.CreateSubKey(subKey);
        if (key != null)
        {
            key.SetValue(string.Empty, title);
            key.SetValue("Icon", iconPath);
            using var cmdKey = key.CreateSubKey("command");
            cmdKey?.SetValue(string.Empty, command);
        }
    }

    private static void UnregisterKey(string subKey)
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(subKey, throwOnMissingSubKey: false);
        }
        catch { }
    }
}
