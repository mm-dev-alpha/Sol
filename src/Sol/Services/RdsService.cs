using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Sol.Helpers;
using Sol.Models;

namespace Sol.Services;

/// <summary>
/// Production implementation of <see cref="IRdsService"/> executing Remote Desktop cmdlets
/// and disk expansion operations via Windows PowerShell and native Windows storage utilities.
/// </summary>
public class RdsService : IRdsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ISettingsService? _settings;

    public RdsService(ISettingsService? settings = null)
    {
        _settings = settings;
    }

    /// <summary>
    /// Decodes PowerShell CLIXML error streams and extracts clean, human-readable error messages.
    /// </summary>
    public static string SanitizePowerShellError(string rawStderr)
    {
        if (string.IsNullOrWhiteSpace(rawStderr))
            return string.Empty;

        string trimmed = rawStderr.Trim();
        if (!trimmed.Contains("#< CLIXML") && !trimmed.Contains("<Objs"))
            return trimmed;

        try
        {
            var errorMatches = System.Text.RegularExpressions.Regex.Matches(trimmed, @"<S S=""Error"">([\s\S]*?)</S>");
            if (errorMatches.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (System.Text.RegularExpressions.Match m in errorMatches)
                {
                    string decoded = System.Text.RegularExpressions.Regex.Replace(m.Groups[1].Value, @"_x([0-9a-fA-F]{4})_", match =>
                    {
                        int charCode = Convert.ToInt32(match.Groups[1].Value, 16);
                        return ((char)charCode).ToString();
                    });
                    sb.Append(decoded);
                }

                string combined = sb.ToString();
                var lines = combined.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                var meaningfulLines = lines
                    .Select(l => l.Trim())
                    .Where(l =>
                        l.Length > 0 &&
                        !l.StartsWith(":") &&
                        !l.StartsWith("+") &&
                        !l.StartsWith("In Zeile:", StringComparison.OrdinalIgnoreCase) &&
                        !l.StartsWith("At line:", StringComparison.OrdinalIgnoreCase) &&
                        !l.StartsWith("CategoryInfo", StringComparison.OrdinalIgnoreCase) &&
                        !l.StartsWith("FullyQualifiedErrorId", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (meaningfulLines.Count > 0)
                {
                    return string.Join(Environment.NewLine, meaningfulLines).Trim();
                }

                var fallback = lines.FirstOrDefault(l => !l.Trim().StartsWith("+") && !l.Trim().StartsWith("CategoryInfo"))?.Trim() ?? string.Empty;
                return fallback.TrimStart(':', ' ');
            }
        }
        catch
        {
            return System.Text.RegularExpressions.Regex.Replace(trimmed, @"<[^>]+>", " ").Trim();
        }

        return System.Text.RegularExpressions.Regex.Replace(trimmed, @"<[^>]+>", " ").Trim();
    }

    /// <inheritdoc />
    public async Task<string> ResolveBrokerFqdnAsync(string broker, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(broker))
            return string.Empty;

        string trimmed = broker.Trim();
        if (trimmed.Contains('.'))
            return trimmed;

        // 1. Try DNS resolution
        try
        {
            var hostEntry = await System.Net.Dns.GetHostEntryAsync(trimmed, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(hostEntry.HostName) && hostEntry.HostName.Contains('.'))
            {
                return hostEntry.HostName;
            }
        }
        catch { }

        // 2. Fallback: Configured Active Directory domain
        string? adDomain = _settings?.AdDomain;
        if (!string.IsNullOrWhiteSpace(adDomain))
        {
            string cleanDomain = adDomain.Trim().TrimStart('.').TrimEnd('.');
            if (!string.IsNullOrWhiteSpace(cleanDomain))
            {
                return $"{trimmed}.{cleanDomain}";
            }
        }

        // 3. Fallback: Local machine DNS domain
        try
        {
            string sysDomain = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
            if (!string.IsNullOrWhiteSpace(sysDomain))
            {
                return $"{trimmed}.{sysDomain.Trim().TrimStart('.').TrimEnd('.')}";
            }
        }
        catch { }

        return trimmed;
    }

    /// <inheritdoc />
    public bool IsElevated()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch (Exception ex)
        {
            AppLog.Write($"RdsService.IsElevated check failed: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public bool RestartAsAdministrator()
    {
        try
        {
            string? exePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                exePath = Process.GetCurrentProcess().MainModule?.FileName;
            }

            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                AppLog.Write("RdsService.RestartAsAdministrator: Process executable path not found.");
                return false;
            }

            var args = Environment.GetCommandLineArgs().Skip(1);
            string arguments = string.Join(" ", args.Select(a => $"\"{a.Replace("\"", "\\\"")}\""));

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = arguments,
                UseShellExecute = true,
                Verb = "runas"
            };

            AppLog.Write($"RdsService.RestartAsAdministrator: Spawning elevated process {exePath} {arguments}");
            Process.Start(psi);

            // Exit current un-elevated process
            Environment.Exit(0);
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Write($"RdsService.RestartAsAdministrator exception: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> TestBrokerConnectionAsync(string broker, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(broker))
            return false;

        string resolvedBroker = await ResolveBrokerFqdnAsync(broker, cancellationToken).ConfigureAwait(false);
        if (!resolvedBroker.Contains('.'))
            return false;

        string script = $@"
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
try {{
    Import-Module RemoteDesktop -ErrorAction Stop
    $count = 0
    try {{
        $collections = @(Get-RDSessionCollection -ConnectionBroker '{EscapePsString(resolvedBroker)}' -ErrorAction Stop)
        $count = $collections.Count
    }} catch {{
        $sessions = @(Get-RDUserSession -ConnectionBroker '{EscapePsString(resolvedBroker)}' -ErrorAction Stop)
        $count = $sessions.Count
    }}
    [PSCustomObject]@{{ Success = $true; Count = $count }} | ConvertTo-Json -Compress
}} catch {{
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}}
";
        try
        {
            var (exitCode, stdout, stderr) = await RunPowerShellScriptAsync(script, 15000, cancellationToken).ConfigureAwait(false);
            if (exitCode != 0 || string.IsNullOrWhiteSpace(stdout))
            {
                AppLog.Write($"RdsService.TestBrokerConnectionAsync failed with exitCode={exitCode}, stderr={stderr}");
                return false;
            }

            using var doc = JsonDocument.Parse(stdout.Trim());
            if (doc.RootElement.TryGetProperty("Success", out var successProp))
            {
                return successProp.GetBoolean();
            }

            return false;
        }
        catch (Exception ex)
        {
            AppLog.Write($"RdsService.TestBrokerConnectionAsync exception: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RdsSessionItem>> GetSessionsAsync(string broker, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(broker))
            return Array.Empty<RdsSessionItem>();

        string resolvedBroker = await ResolveBrokerFqdnAsync(broker, cancellationToken).ConfigureAwait(false);
        if (!resolvedBroker.Contains('.'))
            throw new InvalidOperationException(string.Format(Strings.S.RdsBrokerFqdnRequiredError, broker.Trim()));

        string script = $@"
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
try {{
    Import-Module RemoteDesktop -ErrorAction Stop
    $sessions = @(Get-RDUserSession -ConnectionBroker '{EscapePsString(resolvedBroker)}' -ErrorAction Stop)
    $list = @()
    foreach ($s in $sessions) {{
        $logonRaw = $null
        foreach ($p in 'LogonTime','ConnectTime','StartTime','CreateTime') {{
            if ($s.PSObject.Properties[$p] -and $s.$p) {{ $logonRaw = $s.$p; break }}
        }}
        $logonStr = $null
        if ($logonRaw -is [datetime]) {{
            $logonStr = $logonRaw.ToString('yyyy-MM-ddTHH:mm:ss')
        }} elseif ($logonRaw) {{
            $logonStr = [string]$logonRaw
        }}

        $list += [PSCustomObject]@{{
            Username = [string]$s.UserName
            Domain = [string]$s.DomainName
            SessionId = [int]$s.SessionId
            State = [string]$s.SessionState
            HostServer = [string]$s.HostServer
            UnifiedSessionId = [int]$s.UnifiedSessionId
            LogonTime = $logonStr
        }}
    }}
    $list | ConvertTo-Json -Compress
}} catch {{
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}}
";
        var (exitCode, stdout, stderr) = await RunPowerShellScriptAsync(script, 30000, cancellationToken).ConfigureAwait(false);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Failed to query RDS sessions from broker '{resolvedBroker}': {stderr}");
        }

        string trimmed = stdout.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed == "null")
            return Array.Empty<RdsSessionItem>();

        try
        {
            List<RdsSessionItem> results = new();
            if (trimmed.StartsWith("["))
            {
                var parsed = JsonSerializer.Deserialize<List<RdsSessionRawDto>>(trimmed, JsonOptions);
                if (parsed != null)
                {
                    results.AddRange(parsed.Select(MapSessionRawToModel));
                }
            }
            else if (trimmed.StartsWith("{"))
            {
                var single = JsonSerializer.Deserialize<RdsSessionRawDto>(trimmed, JsonOptions);
                if (single != null)
                {
                    results.Add(MapSessionRawToModel(single));
                }
            }

            return results
                .OrderByDescending(s => s.IsActive)
                .ThenBy(s => s.Username)
                .ToList();
        }
        catch (Exception ex)
        {
            AppLog.Write($"RdsService.GetSessionsAsync JSON parse error: {ex.Message}");
            throw new InvalidOperationException($"Failed to parse RDS session data: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> LogoffSessionAsync(
        string broker,
        string hostServer,
        int unifiedSessionId,
        bool force = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(broker))
            throw new ArgumentException("Connection broker cannot be null or empty.", nameof(broker));
        if (string.IsNullOrWhiteSpace(hostServer))
            throw new ArgumentException("Host server cannot be null or empty.", nameof(hostServer));

        string resolvedBroker = await ResolveBrokerFqdnAsync(broker, cancellationToken).ConfigureAwait(false);
        if (!resolvedBroker.Contains('.'))
            throw new InvalidOperationException(string.Format(Strings.S.RdsBrokerFqdnRequiredError, broker.Trim()));

        string script = $@"
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
try {{
    Import-Module RemoteDesktop -ErrorAction Stop
    $baseParams = @{{ HostServer = '{EscapePsString(hostServer)}'; UnifiedSessionId = {unifiedSessionId}; Force = [bool]${force.ToString().ToLowerInvariant()} }}
    try {{
        Invoke-RDUserLogoff -ConnectionBroker '{EscapePsString(resolvedBroker)}' @baseParams -ErrorAction Stop
    }} catch {{
        Invoke-RDUserLogoff @baseParams -ErrorAction Stop
    }}
    [PSCustomObject]@{{ Success = $true }} | ConvertTo-Json -Compress
}} catch {{
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}}
";
        string operatorIdentity = GetCurrentOperatorIdentity();
        try
        {
            var (exitCode, stdout, stderr) = await RunPowerShellScriptAsync(script, 20000, cancellationToken).ConfigureAwait(false);
            bool success = exitCode == 0;

            if (success)
            {
                Log.Information("RDS Session Logoff: Operator={Operator}, Broker={Broker}, HostServer={HostServer}, UnifiedSessionId={UnifiedSessionId}, Force={Force}, Result=Success",
                    operatorIdentity, resolvedBroker, hostServer, unifiedSessionId, force);
                return true;
            }
            else
            {
                Log.Warning("RDS Session Logoff: Operator={Operator}, Broker={Broker}, HostServer={HostServer}, UnifiedSessionId={UnifiedSessionId}, Force={Force}, Result=Failure, Error={Error}",
                    operatorIdentity, resolvedBroker, hostServer, unifiedSessionId, force, stderr);
                throw new InvalidOperationException(stderr);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "RDS Session Logoff Exception: Operator={Operator}, Broker={Broker}, HostServer={HostServer}, UnifiedSessionId={UnifiedSessionId}",
                operatorIdentity, resolvedBroker, hostServer, unifiedSessionId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectSessionAsync(
        string broker,
        string hostServer,
        int unifiedSessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(broker))
            throw new ArgumentException("Connection broker cannot be null or empty.", nameof(broker));
        if (string.IsNullOrWhiteSpace(hostServer))
            throw new ArgumentException("Host server cannot be null or empty.", nameof(hostServer));

        string resolvedBroker = await ResolveBrokerFqdnAsync(broker, cancellationToken).ConfigureAwait(false);
        if (!resolvedBroker.Contains('.'))
            throw new InvalidOperationException(string.Format(Strings.S.RdsBrokerFqdnRequiredError, broker.Trim()));

        string script = $@"
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
try {{
    Import-Module RemoteDesktop -ErrorAction Stop
    try {{
        Disconnect-RDUser -HostServer '{EscapePsString(hostServer)}' -UnifiedSessionId {unifiedSessionId} -ErrorAction Stop
    }} catch {{
        # Fallback to tsdiscon if Disconnect-RDUser cmdlet not available
        $sessions = @(Get-RDUserSession -ConnectionBroker '{EscapePsString(resolvedBroker)}' -ErrorAction SilentlyContinue)
        $match = $sessions | Where-Object {{ $_.UnifiedSessionId -eq {unifiedSessionId} }} | Select-Object -First 1
        if ($match) {{
            & tsdiscon.exe $match.SessionId /server:'{EscapePsString(hostServer)}'
        }} else {{
            throw $_.Exception.Message
        }}
    }}
    [PSCustomObject]@{{ Success = $true }} | ConvertTo-Json -Compress
}} catch {{
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}}
";
        string operatorIdentity = GetCurrentOperatorIdentity();
        try
        {
            var (exitCode, stdout, stderr) = await RunPowerShellScriptAsync(script, 20000, cancellationToken).ConfigureAwait(false);
            bool success = exitCode == 0;

            if (success)
            {
                Log.Information("RDS Session Disconnect: Operator={Operator}, Broker={Broker}, HostServer={HostServer}, UnifiedSessionId={UnifiedSessionId}, Result=Success",
                    operatorIdentity, resolvedBroker, hostServer, unifiedSessionId);
                return true;
            }
            else
            {
                Log.Warning("RDS Session Disconnect: Operator={Operator}, Broker={Broker}, HostServer={HostServer}, UnifiedSessionId={UnifiedSessionId}, Result=Failure, Error={Error}",
                    operatorIdentity, resolvedBroker, hostServer, unifiedSessionId, stderr);
                throw new InvalidOperationException(stderr);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "RDS Session Disconnect Exception: Operator={Operator}, Broker={Broker}, HostServer={HostServer}, UnifiedSessionId={UnifiedSessionId}",
                operatorIdentity, resolvedBroker, hostServer, unifiedSessionId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RdsCollectionInfo>> GetCollectionsAsync(string broker, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(broker))
            return Array.Empty<RdsCollectionInfo>();

        string resolvedBroker = await ResolveBrokerFqdnAsync(broker, cancellationToken).ConfigureAwait(false);
        if (!resolvedBroker.Contains('.'))
            throw new InvalidOperationException(string.Format(Strings.S.RdsBrokerFqdnRequiredError, broker.Trim()));

        string shortBroker = broker.Split('.')[0];
        string script = $@"
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
try {{
    Import-Module RemoteDesktop -ErrorAction Stop
    $broker = '{EscapePsString(resolvedBroker)}'
    $shortBroker = '{EscapePsString(shortBroker)}'

    $collections = @()
    try {{
        $collections = @(Get-RDSessionCollection -ConnectionBroker $broker -ErrorAction Stop)
    }} catch {{
        try {{
            $collections = @(Get-RDSessionCollection -ConnectionBroker $shortBroker -ErrorAction Stop)
        }} catch {{
            try {{
                $collections = @(Get-RDSessionCollection -ErrorAction Stop)
            }} catch {{
                try {{
                    $sessions = @(Get-RDUserSession -ConnectionBroker $broker -ErrorAction SilentlyContinue)
                    $uniqueColls = $sessions | Where-Object {{ $_.PSObject.Properties['CollectionName'] -and $_.CollectionName }} | Select-Object -ExpandProperty CollectionName -Unique
                    if ($uniqueColls) {{
                        $collections = @($uniqueColls)
                    }}
                }} catch {{ }}
            }}
        }}
    }}

    if ($collections.Count -eq 0) {{
        try {{
            $sessions = @(Get-RDUserSession -ConnectionBroker $broker -ErrorAction SilentlyContinue)
            if (-not $sessions -or $sessions.Count -eq 0) {{
                $sessions = @(Get-RDUserSession -ConnectionBroker $shortBroker -ErrorAction SilentlyContinue)
            }}
            $uniqueColls = $sessions | Where-Object {{ $_.PSObject.Properties['CollectionName'] -and $_.CollectionName }} | Select-Object -ExpandProperty CollectionName -Unique
            if ($uniqueColls) {{
                $collections = @($uniqueColls)
            }}
        }} catch {{ }}
    }}

    $list = @()
    foreach ($c in $collections) {{
        $cName = if ($c.PSObject -and $c.PSObject.Properties['CollectionName']) {{ [string]$c.CollectionName }} else {{ [string]$c }}
        $cName = $cName.Trim()
        if ([string]::IsNullOrWhiteSpace($cName)) {{ continue }}

        $updEnabled = $false
        $updDiskPath = ''
        try {{
            $cfg = Get-RDSessionCollectionConfiguration -ConnectionBroker $broker -CollectionName $cName -UserProfileDisk -ErrorAction Stop
            if ($cfg -and $cfg.EnableUserProfileDisk) {{
                $updEnabled = [bool]$cfg.EnableUserProfileDisk
                $updDiskPath = if ($cfg.DiskPath) {{ [string]$cfg.DiskPath }} else {{ '' }}
            }}
        }} catch {{
            try {{
                $cfg = Get-RDSessionCollectionConfiguration -ConnectionBroker $shortBroker -CollectionName $cName -UserProfileDisk -ErrorAction Stop
                if ($cfg -and $cfg.EnableUserProfileDisk) {{
                    $updEnabled = [bool]$cfg.EnableUserProfileDisk
                    $updDiskPath = if ($cfg.DiskPath) {{ [string]$cfg.DiskPath }} else {{ '' }}
                }}
            }} catch {{
                try {{
                    $cfg = Get-RDSessionCollectionConfiguration -CollectionName $cName -UserProfileDisk -ErrorAction SilentlyContinue
                    if ($cfg -and $cfg.EnableUserProfileDisk) {{
                        $updEnabled = [bool]$cfg.EnableUserProfileDisk
                        $updDiskPath = if ($cfg.DiskPath) {{ [string]$cfg.DiskPath }} else {{ '' }}
                    }}
                }} catch {{ }}
            }}
        }}

        $list += [PSCustomObject]@{{
            CollectionName = $cName
            UpdEnabled = $updEnabled
            UpdDiskPath = $updDiskPath
        }}
    }}
    ConvertTo-Json -InputObject @($list) -Compress
}} catch {{
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}}
";
        var (exitCode, stdout, stderr) = await RunPowerShellScriptAsync(script, 30000, cancellationToken).ConfigureAwait(false);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Failed to query session collections from broker '{resolvedBroker}': {stderr}");
        }

        string trimmed = stdout.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed == "null")
            return Array.Empty<RdsCollectionInfo>();

        try
        {
            List<RdsCollectionInfo> results = new();
            if (trimmed.StartsWith("["))
            {
                var parsed = JsonSerializer.Deserialize<List<RdsCollectionInfo>>(trimmed, JsonOptions);
                if (parsed != null) results.AddRange(parsed);
            }
            else if (trimmed.StartsWith("{"))
            {
                var single = JsonSerializer.Deserialize<RdsCollectionInfo>(trimmed, JsonOptions);
                if (single != null) results.Add(single);
            }
            return results;
        }
        catch (Exception ex)
        {
            AppLog.Write($"RdsService.GetCollectionsAsync JSON parse error: {ex.Message}");
            throw new InvalidOperationException($"Failed to parse collections data: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<RdsDiskLayoutInfo> InspectUpdDiskAsync(string vhdxPath, CancellationToken cancellationToken = default)
    {
        var info = new RdsDiskLayoutInfo
        {
            VhdxPath = vhdxPath,
            Exists = File.Exists(vhdxPath)
        };

        if (!info.Exists)
        {
            info.ErrorMessage = $"File does not exist: {vhdxPath}";
            return info;
        }

        // Test file lock state via FileStream
        try
        {
            using var stream = new FileStream(vhdxPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            info.IsFileLocked = false;
        }
        catch (IOException)
        {
            info.IsFileLocked = true;
        }
        catch (UnauthorizedAccessException)
        {
            info.IsFileLocked = true;
        }

        // Query disk capacity and partition geometry via PowerShell
        string script = $@"
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
try {{
    $mounted = Mount-DiskImage -ImagePath '{EscapePsString(vhdxPath)}' -Access ReadOnly -NoDriveLetter -PassThru -ErrorAction Stop
    $disk = $mounted | Get-Disk
    $part = $disk | Get-Partition | Where-Object {{ $_.Type -ne 'Reserved' }} | Sort-Object Size -Descending | Select-Object -First 1
    
    $result = [PSCustomObject]@{{
        CapacityBytes = [uint64]$disk.Size
        PartitionStyle = [string]$disk.PartitionStyle
        PartitionNumber = if ($part) {{ [int]$part.PartitionNumber }} else {{ 1 }}
        PartitionSizeBytes = if ($part) {{ [uint64]$part.Size }} else {{ 0 }}
    }}
    $null = Dismount-DiskImage -ImagePath '{EscapePsString(vhdxPath)}' -ErrorAction SilentlyContinue
    $result | ConvertTo-Json -Compress
}} catch {{
    $null = Dismount-DiskImage -ImagePath '{EscapePsString(vhdxPath)}' -ErrorAction SilentlyContinue
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}}
";
        try
        {
            var (exitCode, stdout, stderr) = await RunPowerShellScriptAsync(script, 25000, cancellationToken).ConfigureAwait(false);
            if (exitCode == 0 && !string.IsNullOrWhiteSpace(stdout))
            {
                using var doc = JsonDocument.Parse(stdout.Trim());
                var root = doc.RootElement;
                if (root.TryGetProperty("CapacityBytes", out var capProp))
                    info.CapacityBytes = capProp.GetUInt64();
                if (root.TryGetProperty("PartitionStyle", out var styleProp))
                    info.PartitionStyle = styleProp.GetString() ?? "GPT";
                if (root.TryGetProperty("PartitionNumber", out var numProp))
                    info.PartitionNumber = numProp.GetInt32();
                if (root.TryGetProperty("PartitionSizeBytes", out var partProp))
                    info.PartitionSizeBytes = partProp.GetUInt64();
            }
            else
            {
                info.ErrorMessage = string.IsNullOrWhiteSpace(stderr) ? "Failed to read VHDX layout." : stderr;
            }
        }
        catch (Exception ex)
        {
            info.ErrorMessage = ex.Message;
        }

        return info;
    }

    /// <inheritdoc />
    public async Task<RdsDiskExpansionResult> ExpandUpdDiskAsync(
        string broker,
        string collectionName,
        string vhdxPath,
        string userSid,
        string samAccountName,
        ulong additionalGigabytes,
        IProgress<RdsExpansionStep>? progress = null,
        CancellationToken cancellationToken = default)
    {
        string operatorIdentity = GetCurrentOperatorIdentity();
        var result = new RdsDiskExpansionResult();

        if (additionalGigabytes == 0)
        {
            result.ErrorMessage = "Additional size must be at least 1 GiB.";
            return result;
        }

        // Phase 0: Prerequisites Check
        progress?.Report(RdsExpansionStep.CheckingPrerequisites);
        if (!IsElevated())
        {
            result.ErrorMessage = "Administrator elevation is required to expand User Profile Disks.";
            return result;
        }

        if (!File.Exists(vhdxPath))
        {
            result.ErrorMessage = $"Profile disk file does not exist: {vhdxPath}";
            return result;
        }

        // Read initial layout
        var initialLayout = await InspectUpdDiskAsync(vhdxPath, cancellationToken).ConfigureAwait(false);
        if (initialLayout.CapacityBytes == 0)
        {
            result.ErrorMessage = $"Unable to read initial disk layout: {initialLayout.ErrorMessage}";
            return result;
        }

        result.InitialCapacityBytes = initialLayout.CapacityBytes;
        ulong currentMB = (ulong)Math.Ceiling(initialLayout.CapacityBytes / (1024.0 * 1024.0));
        ulong targetMB = currentMB + (additionalGigabytes * 1024);
        ulong targetBytes = targetMB * 1024 * 1024;
        result.TargetCapacityBytes = targetBytes;

        // Size boundary check
        if (string.Equals(initialLayout.PartitionStyle, "MBR", StringComparison.OrdinalIgnoreCase) && targetBytes > (2047UL * 1024 * 1024 * 1024))
        {
            result.ErrorMessage = "Requested capacity exceeds the 2047 GiB limit for MBR partitioned profile disks.";
            return result;
        }

        if (targetBytes > (64UL * 1024 * 1024 * 1024 * 1024))
        {
            result.ErrorMessage = "Requested capacity exceeds the 64 TiB supported limit for VHDX.";
            return result;
        }

        // Phase 1: Verifying Sessions & File Lock
        progress?.Report(RdsExpansionStep.VerifyingSessionsAndLock);

        // Check for active sessions of this user
        var sessions = await GetSessionsAsync(broker, cancellationToken).ConfigureAwait(false);
        var userSessions = sessions.Where(s =>
            string.Equals(s.Username, samAccountName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(s.UserPrincipalDisplay, samAccountName, StringComparison.OrdinalIgnoreCase)).ToList();

        if (userSessions.Count > 0)
        {
            result.ErrorMessage = $"User '{samAccountName}' still has {userSessions.Count} active or disconnected session(s). Please log off the user before expanding.";
            return result;
        }

        // Coordinate lock sidecar
        string lockFilePath = $"{vhdxPath}.resize.lock";
        FileStream? lockHandle = null;
        bool partitionResizeAttempted = false;
        try
        {
            try
            {
                lockHandle = new FileStream(lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Could not obtain exclusive lock ({lockFilePath}): another resize operation may be in progress. ({ex.Message})";
                return result;
            }

            // Verify target VHDX file accessibility with retries
            bool fileAccessible = false;
            for (int i = 0; i < 15; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    using (var testStream = new FileStream(vhdxPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        fileAccessible = true;
                        break;
                    }
                }
                catch
                {
                    await Task.Delay(2000, cancellationToken).ConfigureAwait(false);
                }
            }

            if (!fileAccessible)
            {
                result.ErrorMessage = "The VHDX profile disk is currently held open or locked by a remote session/host server.";
                return result;
            }

            // Phase 2: Expanding Virtual Disk (diskpart)
            progress?.Report(RdsExpansionStep.ExpandingVirtualDisk);

            string? mappedDriveLetter = null;
            string diskpartVhdxPath = vhdxPath;

            if (vhdxPath.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase))
            {
                char? letter = GetAvailableDriveLetter();
                string dirPath = Path.GetDirectoryName(vhdxPath) ?? string.Empty;
                string fileName = Path.GetFileName(vhdxPath);

                if (letter != null && !string.IsNullOrEmpty(dirPath) && !string.IsNullOrEmpty(fileName))
                {
                    try
                    {
                        var mapPsi = new ProcessStartInfo
                        {
                            FileName = "net.exe",
                            Arguments = $"use {letter}: \"{dirPath}\" /persistent:no",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };
                        using var mapProc = Process.Start(mapPsi);
                        if (mapProc != null)
                        {
                            await mapProc.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                            if (mapProc.ExitCode == 0)
                            {
                                mappedDriveLetter = $"{letter}:";
                                diskpartVhdxPath = Path.Combine(mappedDriveLetter, fileName);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLog.Write($"RdsService: Temporary net use mapping failed: {ex.Message}");
                    }
                }
            }

            string diskpartScript = $@"select vdisk file=""{diskpartVhdxPath}""{Environment.NewLine}expand vdisk maximum={targetMB}{Environment.NewLine}";
            string tempScriptPath = Path.Combine(Path.GetTempPath(), $"sol_diskpart_{Guid.NewGuid():N}.txt");
            await File.WriteAllTextAsync(tempScriptPath, diskpartScript, cancellationToken).ConfigureAwait(false);

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "diskpart.exe",
                    Arguments = $"/s \"{tempScriptPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var proc = Process.Start(psi);
                if (proc == null)
                {
                    result.ErrorMessage = "Failed to launch diskpart.exe.";
                    return result;
                }

                using var dpCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var stdoutTask = proc.StandardOutput.ReadToEndAsync(dpCts.Token);
                var stderrTask = proc.StandardError.ReadToEndAsync(dpCts.Token);
                var exitTask = proc.WaitForExitAsync(dpCts.Token);

                var completed = await Task.WhenAny(exitTask, Task.Delay(60000, dpCts.Token)).ConfigureAwait(false);
                if (completed != exitTask)
                {
                    try { proc.Kill(entireProcessTree: true); } catch { }
                    result.ErrorMessage = "diskpart.exe execution timed out after 60 seconds.";
                    return result;
                }

                string dpOut = await stdoutTask.ConfigureAwait(false);
                string dpErr = await stderrTask.ConfigureAwait(false);

                if (proc.ExitCode != 0 || !dpOut.Contains("successfully expanded", StringComparison.OrdinalIgnoreCase))
                {
                    result.ErrorMessage = $"diskpart failed to expand VHDX (ExitCode {proc.ExitCode}): {dpOut} {dpErr}";
                    return result;
                }
            }
            finally
            {
                try { File.Delete(tempScriptPath); } catch { }

                if (mappedDriveLetter != null)
                {
                    try
                    {
                        var unmapPsi = new ProcessStartInfo
                        {
                            FileName = "net.exe",
                            Arguments = $"use {mappedDriveLetter} /delete /y",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };
                        using var unmapProc = Process.Start(unmapPsi);
                        if (unmapProc != null)
                        {
                            await unmapProc.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
                        }
                    }
                    catch { }
                }
            }

            // Phase 3: Resizing NTFS Partition
            progress?.Report(RdsExpansionStep.ResizingNtfsPartition);
            partitionResizeAttempted = true;

            string resizePsScript = $@"
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$mounted = $false
try {{
    $img = Mount-DiskImage -ImagePath '{EscapePsString(vhdxPath)}' -Access ReadWrite -NoDriveLetter -PassThru -ErrorAction Stop
    $mounted = $true
    $disk = $img | Get-Disk
    $part = $disk | Get-Partition | Where-Object {{ $_.Type -ne 'Reserved' }} | Sort-Object Size -Descending | Select-Object -First 1
    if (-not $part) {{ throw 'NTFS partition not found on mounted disk.' }}

    $supported = Get-PartitionSupportedSize -DiskNumber $disk.Number -PartitionNumber $part.PartitionNumber
    Resize-Partition -DiskNumber $disk.Number -PartitionNumber $part.PartitionNumber -Size $supported.SizeMax -ErrorAction Stop

    $finalDisk = Get-Disk -Number $disk.Number
    $finalPart = Get-Partition -DiskNumber $disk.Number -PartitionNumber $part.PartitionNumber

    $null = Dismount-DiskImage -ImagePath '{EscapePsString(vhdxPath)}' -ErrorAction Stop
    $mounted = $false

    [PSCustomObject]@{{
        FinalCapacityBytes = [uint64]$finalDisk.Size
        FinalPartitionBytes = [uint64]$finalPart.Size
    }} | ConvertTo-Json -Compress
}} catch {{
    if ($mounted) {{
        $null = Dismount-DiskImage -ImagePath '{EscapePsString(vhdxPath)}' -ErrorAction SilentlyContinue
    }}
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}}
";
            var (resExitCode, resStdout, resStderr) = await RunPowerShellScriptAsync(resizePsScript, 60000, cancellationToken).ConfigureAwait(false);
            if (resExitCode != 0 || string.IsNullOrWhiteSpace(resStdout))
            {
                result.ErrorMessage = $"Failed to extend partition inside VHDX: {resStderr}";
                return result;
            }

            using var doc = JsonDocument.Parse(resStdout.Trim());
            var root = doc.RootElement;
            result.FinalCapacityBytes = root.GetProperty("FinalCapacityBytes").GetUInt64();
            result.FinalPartitionBytes = root.GetProperty("FinalPartitionBytes").GetUInt64();

            // Phase 4: Verifying and Completing
            progress?.Report(RdsExpansionStep.VerifyingAndCompleting);

            result.IsSuccess = true;
            Log.Information("RDS UPD Expand Succeeded: Operator={Operator}, Broker={Broker}, Collection={Collection}, UserSid={UserSid}, VhdxPath={VhdxPath}, InitialSizeGiB={InitialGiB:N2}, FinalSizeGiB={FinalGiB:N2}",
                operatorIdentity, broker, collectionName, userSid, vhdxPath,
                result.InitialCapacityBytes / (1024.0 * 1024.0 * 1024.0),
                result.FinalCapacityBytes / (1024.0 * 1024.0 * 1024.0));

            return result;
        }
        catch (Exception ex)
        {
            if (partitionResizeAttempted)
            {
                try
                {
                    await RunPowerShellScriptAsync($"Dismount-DiskImage -ImagePath '{EscapePsString(vhdxPath)}' -ErrorAction SilentlyContinue", 10000, CancellationToken.None).ConfigureAwait(false);
                }
                catch { }
            }

            result.ErrorMessage = ex.Message;
            Log.Error(ex, "RDS UPD Expand Failed: Operator={Operator}, Broker={Broker}, VhdxPath={VhdxPath}",
                operatorIdentity, broker, vhdxPath);
            return result;
        }
        finally
        {
            if (lockHandle != null)
            {
                try
                {
                    lockHandle.Dispose();
                }
                catch { }

                try
                {
                    if (File.Exists(lockFilePath))
                    {
                        File.Delete(lockFilePath);
                    }
                }
                catch { }
            }
        }
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunPowerShellScriptAsync(
        string script,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        // Encode script as base64 Unicode string for -EncodedCommand to prevent shell escaping errors
        byte[] bytes = Encoding.Unicode.GetBytes(script);
        string encoded = Convert.ToBase64String(bytes);

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand {encoded}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var proc = Process.Start(psi);
        if (proc == null)
        {
            return (-1, string.Empty, "Failed to start powershell.exe");
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var stdoutTask = proc.StandardOutput.ReadToEndAsync(cts.Token);
        var stderrTask = proc.StandardError.ReadToEndAsync(cts.Token);
        var exitTask = proc.WaitForExitAsync(cts.Token);

        var completed = await Task.WhenAny(exitTask, Task.Delay(timeoutMs, cts.Token)).ConfigureAwait(false);
        if (completed != exitTask)
        {
            try { proc.Kill(entireProcessTree: true); } catch { }
            return (-1, string.Empty, $"Execution timed out after {timeoutMs}ms");
        }

        string stdout = await stdoutTask.ConfigureAwait(false);
        string rawStderr = await stderrTask.ConfigureAwait(false);
        string stderr = SanitizePowerShellError(rawStderr);

        return (proc.ExitCode, stdout, stderr);
    }

    private static char? GetAvailableDriveLetter()
    {
        try
        {
            var taken = DriveInfo.GetDrives()
                .Select(d => char.ToUpperInvariant(d.Name[0]))
                .ToHashSet();

            for (char c = 'Z'; c >= 'D'; c--)
            {
                if (!taken.Contains(c))
                {
                    return c;
                }
            }
        }
        catch { }

        return null;
    }

    private static string EscapePsString(string input)
    {
        return (input ?? string.Empty).Replace("'", "''");
    }

    private static string GetCurrentOperatorIdentity()
    {
        try
        {
            return WindowsIdentity.GetCurrent().Name;
        }
        catch
        {
            return Environment.UserName;
        }
    }

    private static RdsSessionItem MapSessionRawToModel(RdsSessionRawDto dto)
    {
        DateTime? dt = null;
        if (!string.IsNullOrWhiteSpace(dto.LogonTime))
        {
            if (DateTime.TryParse(dto.LogonTime, out var parsed))
            {
                dt = parsed;
            }
        }

        return new RdsSessionItem
        {
            Username = dto.Username ?? string.Empty,
            Domain = dto.Domain ?? string.Empty,
            SessionId = dto.SessionId,
            State = dto.State ?? string.Empty,
            HostServer = dto.HostServer ?? string.Empty,
            UnifiedSessionId = dto.UnifiedSessionId,
            LogonTime = dt,
            FormattedLogonTime = dt?.ToString("yyyy-MM-dd HH:mm") ?? (dto.LogonTime ?? string.Empty)
        };
    }

    private sealed class RdsSessionRawDto
    {
        public string? Username { get; set; }
        public string? Domain { get; set; }
        public int SessionId { get; set; }
        public string? State { get; set; }
        public string? HostServer { get; set; }
        public int UnifiedSessionId { get; set; }
        public string? LogonTime { get; set; }
    }
}
