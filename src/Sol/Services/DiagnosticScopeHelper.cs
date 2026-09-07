using System;
using System.Management;
using System.Net.Sockets;

namespace Sol.Services;

internal static class DiagnosticScopeHelper
{
    public static readonly TimeSpan RealMachineTimeout = TimeSpan.FromSeconds(15);

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

    public static bool IsRemoteHostReachable(string host, int timeoutMs = 2000)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(host, 135);
            return connectTask.Wait(timeoutMs) && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    public static ManagementScope CreateManagementScope(string host, string namespaceName = @"root\cimv2")
    {
        if (!IsLocalHost(host) && !IsRemoteHostReachable(host, 2000))
        {
            throw new TimeoutException($"Endpoint '{host}' RPC port 135 unreachable (offline or firewalled).");
        }

        var options = new ConnectionOptions
        {
            Impersonation = ImpersonationLevel.Impersonate,
            Authentication = AuthenticationLevel.PacketPrivacy,
            Timeout = TimeSpan.FromSeconds(5),
            EnablePrivileges = true
        };

        string path = IsLocalHost(host) ? $@"\\.\{namespaceName}" : $@"\\{host}\{namespaceName}";
        var scope = new ManagementScope(path, options);
        scope.Connect();
        return scope;
    }
}
