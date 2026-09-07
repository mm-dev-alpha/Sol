using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sol.Helpers;
using Sol.Models;

namespace Sol.Services;

public class ExportService : IExportService
{
    public async Task ExportToCsvAsync<T>(IEnumerable<T> records, Stream outputStream, CancellationToken cancellationToken = default)
    {
        using var writer = new StreamWriter(outputStream, Encoding.UTF8, leaveOpen: true);
        var csvContent = await FormatAsCsvStringAsync(records, cancellationToken);
        await writer.WriteAsync(csvContent.AsMemory(), cancellationToken);
        await writer.FlushAsync(cancellationToken);
    }

    public async Task ExportToJsonAsync<T>(T data, Stream outputStream, CancellationToken cancellationToken = default)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        await JsonSerializer.SerializeAsync(outputStream, data, options, cancellationToken);
    }

    public Task<string> FormatAsCsvStringAsync<T>(IEnumerable<T> records, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var sb = new StringBuilder();
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .Where(p => p.CanRead && !p.PropertyType.IsGenericType)
                                 .ToArray();

            // Header line
            sb.AppendLine(string.Join(",", props.Select(p => $"\"{EscapeCsv(p.Name)}\"")));

            // Rows
            foreach (var record in records)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var values = props.Select(p =>
                {
                    var val = p.GetValue(record)?.ToString() ?? string.Empty;
                    return $"\"{EscapeCsv(val)}\"";
                });
                sb.AppendLine(string.Join(",", values));
            }

            return sb.ToString();
        }, cancellationToken);
    }

    public Task<string> FormatAsJsonStringAsync<T>(T data, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Serialize(data, options);
        }, cancellationToken);
    }

    private static string EscapeCsv(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        string sanitized = input;
        if (sanitized.Length > 0 && (sanitized[0] == '=' || sanitized[0] == '+' || sanitized[0] == '-' || sanitized[0] == '@' || sanitized[0] == '\t' || sanitized[0] == '\r'))
        {
            sanitized = "'" + sanitized;
        }

        return sanitized.Replace("\"", "\"\"");
    }

    public string FormatUserProfileReport(AdUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var sb = new StringBuilder();
        sb.AppendLine("================================================================================");
        sb.AppendLine($"ACTIVE DIRECTORY USER PROFILE: {user.DisplayName} ({user.SamAccountName})");
        sb.AppendLine("================================================================================");
        sb.AppendLine();

        // 1. Identity & Directory
        sb.AppendLine("[ IDENTITY & DIRECTORY ]");
        sb.AppendLine($"  Display Name:        {user.DisplayName}");
        if (!string.IsNullOrWhiteSpace(user.GivenName))
            sb.AppendLine($"  First Name:          {user.GivenName}");
        if (!string.IsNullOrWhiteSpace(user.Surname))
            sb.AppendLine($"  Last Name:           {user.Surname}");
        sb.AppendLine($"  SAM Account Name:    {user.SamAccountName}");
        if (!string.IsNullOrWhiteSpace(user.Upn))
            sb.AppendLine($"  User Principal Name: {user.Upn}");
        if (!string.IsNullOrWhiteSpace(user.Email))
            sb.AppendLine($"  Email Address:       {user.Email}");
        if (!string.IsNullOrWhiteSpace(user.EmployeeId))
            sb.AppendLine($"  Employee ID:         {user.EmployeeId}");
        if (!string.IsNullOrWhiteSpace(user.Sid))
            sb.AppendLine($"  Security ID (SID):   {user.Sid}");
        if (!string.IsNullOrWhiteSpace(user.OuPath))
            sb.AppendLine($"  OU Path:             {user.OuPath}");
        if (!string.IsNullOrWhiteSpace(user.Description))
            sb.AppendLine($"  Description:         {user.Description}");
        if (!string.IsNullOrWhiteSpace(user.WebPage))
            sb.AppendLine($"  Web Page:            {user.WebPage}");
        sb.AppendLine();

        // 2. Organization
        sb.AppendLine("[ ORGANIZATION ]");
        sb.AppendLine($"  Job Title:           {(!string.IsNullOrWhiteSpace(user.Title) ? user.Title : "—")}");
        sb.AppendLine($"  Department:          {(!string.IsNullOrWhiteSpace(user.Department) ? user.Department : "—")}");
        sb.AppendLine($"  Office:              {(!string.IsNullOrWhiteSpace(user.Office) ? user.Office : "—")}");
        sb.AppendLine($"  Manager:             {(!string.IsNullOrWhiteSpace(user.Manager) ? user.Manager : "—")}");
        if (user.DirectReports != null && user.DirectReports.Count > 0)
        {
            sb.AppendLine($"  Direct Reports ({user.DirectReports.Count}):");
            foreach (var report in user.DirectReports)
            {
                sb.AppendLine($"    - {report}");
            }
        }
        else
        {
            sb.AppendLine("  Direct Reports:      None");
        }
        sb.AppendLine();

        // 3. Contact Details
        sb.AppendLine("[ CONTACT INFORMATION ]");
        sb.AppendLine($"  Office Phone:        {(!string.IsNullOrWhiteSpace(user.OfficePhone) ? user.OfficePhone : "—")}");
        sb.AppendLine($"  Mobile Phone:        {(!string.IsNullOrWhiteSpace(user.MobilePhone) ? user.MobilePhone : "—")}");
        if (!string.IsNullOrWhiteSpace(user.StreetAddress) || !string.IsNullOrWhiteSpace(user.City) || !string.IsNullOrWhiteSpace(user.PostalCode) || !string.IsNullOrWhiteSpace(user.State))
        {
            sb.AppendLine($"  Street Address:      {(!string.IsNullOrWhiteSpace(user.StreetAddress) ? user.StreetAddress : "—")}");
            sb.AppendLine($"  City:                {(!string.IsNullOrWhiteSpace(user.City) ? user.City : "—")}");
            sb.AppendLine($"  Postal Code:         {(!string.IsNullOrWhiteSpace(user.PostalCode) ? user.PostalCode : "—")}");
            sb.AppendLine($"  State / Province:    {(!string.IsNullOrWhiteSpace(user.State) ? user.State : "—")}");
        }
        sb.AppendLine();

        // 4. Account & Security Status
        sb.AppendLine("[ ACCOUNT & SECURITY STATUS ]");
        sb.AppendLine($"  Account Status:      {user.AccountStatus}");
        sb.AppendLine($"  Locked Out:          {(user.IsLockedOut ? Strings.S.Yes : Strings.S.No)}");
        sb.AppendLine($"  Account Expires:     {(user.AccountExpires.HasValue ? user.AccountExpires.Value.ToString("g") : (!string.IsNullOrWhiteSpace(user.AccountExpiresStatus) ? user.AccountExpiresStatus : Strings.S.Never))}");
        sb.AppendLine($"  Password Last Set:   {user.PasswordLastSet?.ToString("d") ?? "N/A"}");
        sb.AppendLine($"  Password Expiry:     {(!string.IsNullOrWhiteSpace(user.PasswordExpiryStatus) ? user.PasswordExpiryStatus : (user.PasswordExpiry.HasValue ? user.PasswordExpiry.Value.ToString("g") : Strings.S.Never))}");
        sb.AppendLine($"  Password Never Exp.: {(user.PasswordNeverExpires ? Strings.S.Yes : Strings.S.No)}");
        sb.AppendLine($"  Bad Password Count:  {user.BadPasswordCount}");
        if (user.BadPasswordTime.HasValue && user.BadPasswordTime.Value != DateTime.MinValue)
            sb.AppendLine($"  Last Bad Password:   {user.BadPasswordTime.Value:g}");
        sb.AppendLine();

        // 5. Activity & Object Metadata
        sb.AppendLine("[ ACTIVITY & OBJECT METADATA ]");
        sb.AppendLine($"  Last Logon:          {user.LastLogon?.ToString("g") ?? "N/A"}");
        if (user.LastLogonTimestamp.HasValue)
            sb.AppendLine($"  Last Logon Timestamp:{user.LastLogonTimestamp.Value:g}");
        sb.AppendLine($"  Created:             {user.Created?.ToString("g") ?? "N/A"}");
        sb.AppendLine($"  Modified:            {user.Modified?.ToString("g") ?? "N/A"}");
        sb.AppendLine();

        // 6. Security Groups
        sb.AppendLine($"[ GROUP MEMBERSHIPS ({user.Groups.Count}) ]");
        if (user.Groups.Count > 0)
        {
            foreach (var group in user.Groups)
            {
                sb.AppendLine($"  - {group}");
            }
        }
        else
        {
            sb.AppendLine("  (No groups assigned)");
        }

        return sb.ToString();
    }

    public string FormatComputerProfileReport(
        AdComputer computer,
        ComputerHardwareSnapshot? hardware = null,
        ComputerUptimeSnapshot? uptime = null,
        ComputerDiskSnapshot? disk = null,
        ComputerBatterySnapshot? battery = null,
        ComputerSessionSnapshot? sessions = null,
        ComputerBitLockerSnapshot? bitlocker = null,
        string? warrantyUrl = null)
    {
        ArgumentNullException.ThrowIfNull(computer);

        var sb = new StringBuilder();
        sb.AppendLine("================================================================================");
        sb.AppendLine($"COMPUTER PROFILE & DIAGNOSTICS: {computer.Name} ({computer.DnsHostName})");
        sb.AppendLine("================================================================================");
        sb.AppendLine();

        // 1. Identity & Active Directory
        sb.AppendLine("[ ACTIVE DIRECTORY & NETWORK IDENTITY ]");
        sb.AppendLine($"  Computer Name:       {computer.Name}");
        sb.AppendLine($"  SAM Account Name:    {computer.SamAccountName}");
        sb.AppendLine($"  DNS Host Name:       {computer.DnsHostName}");
        if (!string.IsNullOrWhiteSpace(computer.IPv4Address))
            sb.AppendLine($"  IPv4 Address:        {computer.IPv4Address}");
        sb.AppendLine($"  Operating System:    {computer.OperatingSystem} {computer.OperatingSystemVersion}".Trim());
        sb.AppendLine($"  Account Status:      {computer.AccountStatus} (Enabled: {computer.IsEnabled})");
        if (!string.IsNullOrWhiteSpace(computer.Sid))
            sb.AppendLine($"  Security ID (SID):   {computer.Sid}");
        sb.AppendLine($"  OU Path:             {computer.OuPath}");
        if (!string.IsNullOrWhiteSpace(computer.Description))
            sb.AppendLine($"  Description:         {computer.Description}");
        if (!string.IsNullOrWhiteSpace(computer.ManagedBy))
            sb.AppendLine($"  Managed By:          {computer.ManagedBy}");
        if (!string.IsNullOrWhiteSpace(computer.Location))
            sb.AppendLine($"  Location:            {computer.Location}");
        sb.AppendLine($"  Password Last Set:   {computer.PasswordLastSet?.ToString("g") ?? "N/A"}");
        sb.AppendLine($"  Last Logon:          {computer.LastLogon?.ToString("g") ?? "N/A"}");
        sb.AppendLine($"  Created:             {computer.Created?.ToString("g") ?? "N/A"}");
        sb.AppendLine($"  Modified:            {computer.Modified?.ToString("g") ?? "N/A"}");
        sb.AppendLine();

        // 2. Hardware & BIOS Diagnostics
        sb.AppendLine("[ HARDWARE & BIOS DIAGNOSTICS ]");
        if (hardware != null && hardware.IsSuccess)
        {
            sb.AppendLine($"  Manufacturer & Model:{hardware.Manufacturer} {hardware.Model}".Trim());
            sb.AppendLine($"  Serial / Service Tag:{hardware.SerialNumber}");
            sb.AppendLine($"  BIOS Version & Date: {hardware.BiosVersion} ({hardware.BiosReleaseDate})");
            sb.AppendLine($"  OS Build:            {hardware.FormattedBuild}");
            sb.AppendLine($"  Processor (CPU):     {hardware.CpuName}");
            sb.AppendLine($"  Total Memory (RAM):  {hardware.TotalMemoryFormatted}");
            if (!string.IsNullOrWhiteSpace(warrantyUrl))
                sb.AppendLine($"  Warranty Check Link: {warrantyUrl}");
        }
        else if (hardware != null && !hardware.IsSuccess)
        {
            sb.AppendLine($"  Diagnostics Status:  Error ({hardware.ErrorMessage})");
        }
        else
        {
            sb.AppendLine("  Diagnostics Status:  Not queried or unreachable");
        }
        sb.AppendLine();

        // 3. System Uptime & Reboot State
        sb.AppendLine("[ SYSTEM UPTIME & REBOOT STATUS ]");
        if (uptime != null && uptime.IsSuccess)
        {
            sb.AppendLine($"  System Uptime:       {uptime.FormattedUptime}");
            sb.AppendLine($"  Last Boot Time:      {uptime.FormattedLastBoot}");
            sb.AppendLine($"  Reboot Required:     {uptime.RebootStatusText}");
            if (uptime.PendingRebootReasons.Count > 0)
                sb.AppendLine($"  Reboot Reasons:      {uptime.FormattedRebootReasons}");
        }
        else if (uptime != null && !uptime.IsSuccess)
        {
            sb.AppendLine($"  Uptime Status:       Error ({uptime.ErrorMessage})");
        }
        else
        {
            sb.AppendLine("  Uptime Status:       Not queried or unreachable");
        }
        sb.AppendLine();

        // 4. Local Storage & Logical Drives
        sb.AppendLine("[ STORAGE & LOGICAL DISK DRIVES ]");
        if (disk != null && disk.IsSuccess && disk.Drives.Count > 0)
        {
            foreach (var drive in disk.Drives)
            {
                sb.AppendLine($"  - {drive.CopyDetailsText}");
            }
        }
        else if (disk != null && !disk.IsSuccess)
        {
            sb.AppendLine($"  Storage Status:      Error ({disk.ErrorMessage})");
        }
        else
        {
            sb.AppendLine("  Storage Status:      Not queried or no local fixed drives reported");
        }
        sb.AppendLine();

        // 5. Battery & Power Diagnostics (Laptops)
        if (battery != null && battery.IsSuccess && battery.HasBattery)
        {
            sb.AppendLine("[ BATTERY & POWER DIAGNOSTICS ]");
            sb.AppendLine($"  Battery Health:      {battery.HealthStatusDisplay} ({Strings.S.BatteryWearNotice}: {battery.WearPercentage:F1}%)");
            sb.AppendLine($"  Charge Remaining:    {battery.EstimatedChargeRemainingPercent}% ({battery.BatteryStatusText})");
            sb.AppendLine($"  Full / Design Cap.:  {battery.FormattedFullChargeCapacity} / {battery.FormattedDesignCapacity}");
            sb.AppendLine($"  Cycle Count:         {battery.FormattedCycleCount}");
            sb.AppendLine($"  Estimated Runtime:   {battery.FormattedEstimatedRunTime}");
            if (!string.IsNullOrWhiteSpace(battery.Chemistry))
                sb.AppendLine($"  Chemistry:           {battery.Chemistry}");
            sb.AppendLine();
        }

        // 6. Active Logon Sessions
        sb.AppendLine("[ ACTIVE & DISCONNECTED LOGON SESSIONS ]");
        if (sessions != null && sessions.IsSuccess && sessions.Sessions.Count > 0)
        {
            foreach (var session in sessions.Sessions)
            {
                sb.AppendLine($"  - Session ID {session.SessionId}: {session.CopyDetailsText}");
            }
        }
        else if (sessions != null && sessions.IsSuccess && sessions.Sessions.Count == 0)
        {
            sb.AppendLine("  (No active logon sessions currently active)");
        }
        else if (sessions != null && !sessions.IsSuccess)
        {
            sb.AppendLine($"  Sessions Status:     Error ({sessions.ErrorMessage})");
        }
        else
        {
            sb.AppendLine("  Sessions Status:     Not queried or unreachable");
        }
        sb.AppendLine();

        // 7. BitLocker Drive Encryption & Recovery Keys
        sb.AppendLine("[ BITLOCKER ENCRYPTION & RECOVERY KEYS ]");
        if (bitlocker != null && bitlocker.IsSuccess)
        {
            sb.AppendLine($"  Drive Letter:        {bitlocker.DriveLetter}");
            sb.AppendLine($"  Protection Status:   {(bitlocker.IsProtectionActive ? "Active / Protected" : (bitlocker.IsProtectionSuspended ? "Suspended" : "Disabled"))}");
            sb.AppendLine($"  Conversion Status:   {bitlocker.FormattedConversionStatus}");
            sb.AppendLine($"  Encryption Method:   {bitlocker.FormattedEncryptionMethod}");
        }
        if (computer.BitLockerKeys.Count > 0)
        {
            sb.AppendLine($"  AD Recovery Keys ({computer.BitLockerKeys.Count}):");
            foreach (var key in computer.BitLockerKeys)
            {
                sb.AppendLine($"    - ID: {key.KeyId} | Password: {key.RecoveryPassword} | Created: {key.FormattedCreated}");
            }
        }
        else if (bitlocker == null || !bitlocker.IsSuccess)
        {
            sb.AppendLine("  (No BitLocker recovery keys stored in Active Directory)");
        }
        sb.AppendLine();

        // 8. Security Groups
        sb.AppendLine($"[ GROUP MEMBERSHIPS ({computer.Groups.Count}) ]");
        if (computer.Groups.Count > 0)
        {
            foreach (var group in computer.Groups)
            {
                sb.AppendLine($"  - {group}");
            }
        }
        else
        {
            sb.AppendLine("  (No groups assigned)");
        }

        return sb.ToString();
    }
}
