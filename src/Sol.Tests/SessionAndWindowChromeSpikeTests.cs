using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Sol.Models;
using Xunit;

namespace Sol.Tests;

public class SessionAndWindowChromeSpikeTests
{
    #region Win32 WTS Native Definitions

    [StructLayout(LayoutKind.Sequential)]
    public struct WTS_SESSION_INFO
    {
        public int SessionId;
        public IntPtr pWinStationName;
        public int State;
    }

    public enum WTS_INFO_CLASS
    {
        WTSInitialProgram = 0,
        WTSApplicationName = 1,
        WTSWorkingDirectory = 2,
        WTSOEMId = 3,
        WTSSessionId = 4,
        WTSUserName = 5,
        WTSWinStationName = 6,
        WTSDomainName = 7,
        WTSConnectState = 8,
        WTSClientBuildNumber = 9,
        WTSClientName = 10,
        WTSClientDirectory = 11,
        WTSClientProductId = 12,
        WTSClientHardwareId = 13,
        WTSClientAddress = 14,
        WTSClientDisplay = 15,
        WTSClientCache = 16,
        WTSLogonTime = 17,
        WTSCompression = 18,
        WTSIncomingBytes = 19,
        WTSOutgoingBytes = 20,
        WTSIncomingFrames = 21,
        WTSOutgoingFrames = 22,
        WTSClientInfo = 23,
        WTSUserInfo = 24
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

    #endregion

    #region Win32 Window Styles & Dragging Definitions

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    public const int WM_NCLBUTTONDOWN = 0x00A1;
    public const int HTCAPTION = 2;

    #endregion

    #region Spike Logic Helpers

    public record SpikeWtsSession(int SessionId, string StationName, int State, string UserName, string Domain);

    public static List<SpikeWtsSession> QueryLocalWtsSessions()
    {
        var result = new List<SpikeWtsSession>();
        IntPtr pSessionInfo = IntPtr.Zero;
        int count = 0;

        if (!WTSEnumerateSessions(IntPtr.Zero, 0, 1, out pSessionInfo, out count))
        {
            return result;
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

                result.Add(new SpikeWtsSession(sessionInfo.SessionId, stationName, sessionInfo.State, userName, domain));
            }
        }
        finally
        {
            if (pSessionInfo != IntPtr.Zero)
            {
                WTSFreeMemory(pSessionInfo);
            }
        }

        return result;
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
                WTSFreeMemory(pBuffer);
            }
        }
        return string.Empty;
    }

    public static bool TryParseLoggedOnUser(
        string antecedent,
        string dependent,
        out string logonId,
        out string domain,
        out string name)
    {
        logonId = string.Empty;
        domain = string.Empty;
        name = string.Empty;

        // In standard WMI Win32_LoggedOnUser:
        // Antecedent is Win32_Account (Domain and Name)
        // Dependent is Win32_LogonSession (LogonId)
        // To guard against any provider variation, search both properties for each token.
        var matchLogon = Regex.Match(dependent, @"LogonId=""?(\d+)""?", RegexOptions.IgnoreCase);
        if (!matchLogon.Success)
        {
            matchLogon = Regex.Match(antecedent, @"LogonId=""?(\d+)""?", RegexOptions.IgnoreCase);
        }

        if (matchLogon.Success)
        {
            logonId = matchLogon.Groups[1].Value;
        }

        var matchDomain = Regex.Match(antecedent, @"Domain=""([^""]+)""", RegexOptions.IgnoreCase);
        if (!matchDomain.Success)
        {
            matchDomain = Regex.Match(dependent, @"Domain=""([^""]+)""", RegexOptions.IgnoreCase);
        }

        if (matchDomain.Success)
        {
            domain = matchDomain.Groups[1].Value;
        }

        var matchName = Regex.Match(antecedent, @"Name=""([^""]+)""", RegexOptions.IgnoreCase);
        if (!matchName.Success)
        {
            matchName = Regex.Match(dependent, @"Name=""([^""]+)""", RegexOptions.IgnoreCase);
        }

        if (matchName.Success)
        {
            name = matchName.Groups[1].Value;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        // Filter system pseudo-accounts
        if (name.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("LOCAL SERVICE", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("NETWORK SERVICE", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("DWM-", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("UMFD-", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("ANONYMOUS LOGON", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("Font Driver Host", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    public static (string Domain, string UserName) ParseComputerSystemUserName(string? rawUserName)
    {
        if (string.IsNullOrWhiteSpace(rawUserName))
        {
            return (string.Empty, string.Empty);
        }

        string trimmed = rawUserName.Trim();
        int slashIndex = trimmed.IndexOf('\\');
        if (slashIndex >= 0)
        {
            string domain = trimmed.Substring(0, slashIndex);
            string user = trimmed.Substring(slashIndex + 1);
            return (domain, user);
        }

        return (string.Empty, trimmed);
    }

    #endregion

    #region Unit & Spike Tests

    [Fact]
    public void WTSEnumerateSessions_OnLocalMachine_SucceedsAndDiscoversConsoleOrActiveUser()
    {
        // Execute on the running machine
        var sessions = QueryLocalWtsSessions();

        // Every Windows machine has at least session 0 (services) or console
        Assert.NotEmpty(sessions);

        // Verify session 0 or console exists
        Assert.Contains(sessions, s => s.SessionId == 0 || s.StationName.Equals("Console", StringComparison.OrdinalIgnoreCase));

        // If an interactive session is present (State == 0 / Active), verify username extraction
        var activeSession = sessions.Find(s => s.State == 0 && !string.IsNullOrWhiteSpace(s.UserName));
        if (activeSession != null)
        {
            Assert.NotEmpty(activeSession.UserName);
        }
    }

    [Fact]
    public void TryParseLoggedOnUser_StandardWmiOrder_ParsesCorrectly()
    {
        // Standard WMI order: Antecedent = Win32_Account, Dependent = Win32_LogonSession
        string antecedent = @"\\TERMINATOR\root\cimv2:Win32_Account.Domain=""TERMINATOR"",Name=""MM""";
        string dependent = @"\\TERMINATOR\root\cimv2:Win32_LogonSession.LogonId=""2128689""";

        bool success = TryParseLoggedOnUser(antecedent, dependent, out string logonId, out string domain, out string name);

        Assert.True(success);
        Assert.Equal("2128689", logonId);
        Assert.Equal("TERMINATOR", domain);
        Assert.Equal("MM", name);
    }

    [Fact]
    public void TryParseLoggedOnUser_SwappedWmiOrder_ParsesCorrectly()
    {
        // Swapped order fallback: Antecedent = Win32_LogonSession, Dependent = Win32_Account
        string antecedent = @"\\SERVER01\root\cimv2:Win32_LogonSession.LogonId=""994411""";
        string dependent = @"\\SERVER01\root\cimv2:Win32_Account.Domain=""CORP"",Name=""jdoe""";

        bool success = TryParseLoggedOnUser(antecedent, dependent, out string logonId, out string domain, out string name);

        Assert.True(success);
        Assert.Equal("994411", logonId);
        Assert.Equal("CORP", domain);
        Assert.Equal("jdoe", name);
    }

    [Theory]
    [InlineData("SYSTEM")]
    [InlineData("LOCAL SERVICE")]
    [InlineData("NETWORK SERVICE")]
    [InlineData("DWM-1")]
    [InlineData("UMFD-0")]
    [InlineData("ANONYMOUS LOGON")]
    [InlineData("Font Driver Host\\UMFD-1")]
    public void TryParseLoggedOnUser_SystemAccounts_FilteredOut(string systemAccountName)
    {
        string antecedent = $@"\\SERVER01\root\cimv2:Win32_Account.Domain=""NT AUTHORITY"",Name=""{systemAccountName}""";
        string dependent = @"\\SERVER01\root\cimv2:Win32_LogonSession.LogonId=""999""";

        bool success = TryParseLoggedOnUser(antecedent, dependent, out _, out _, out _);

        Assert.False(success);
    }

    [Theory]
    [InlineData("CORP\\m.mustermann", "CORP", "m.mustermann")]
    [InlineData("TERMINATOR\\MM", "TERMINATOR", "MM")]
    [InlineData("admin", "", "admin")]
    [InlineData("", "", "")]
    [InlineData(null, "", "")]
    [InlineData("   ", "", "")]
    public void ParseComputerSystemUserName_SplitsProperly(string? input, string expectedDomain, string expectedUser)
    {
        var (domain, user) = ParseComputerSystemUserName(input);
        Assert.Equal(expectedDomain, domain);
        Assert.Equal(expectedUser, user);
    }

    [Fact]
    public void WindowStyleConstants_And_ReleaseCapture_AreConsistent()
    {
        // Verify constants for custom borderless drag
        Assert.Equal(0x00A1, WM_NCLBUTTONDOWN);
        Assert.Equal(2, HTCAPTION);

        // Calling ReleaseCapture when no capture is held returns false/true safely without throwing
        bool _ = ReleaseCapture();
        // Method signature must be callable without EntryPointNotFoundException
        Assert.True(true);
    }

    #endregion
}
