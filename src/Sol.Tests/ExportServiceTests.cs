using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Sol.Services;
using Xunit;

namespace Sol.Tests;

public class ExportServiceTests
{
    private record SampleRecord(string Name, int Count, string Status);

    [Fact]
    public async Task FormatAsCsvStringAsync_FormatsHeaderAndRowsWithQuotes()
    {
        // Arrange
        var service = new ExportService();
        var data = new List<SampleRecord>
        {
            new("Alice \"Admin\"", 42, "Active"),
            new("Bob, Manager", 7, "Disabled")
        };

        // Act
        var csv = await service.FormatAsCsvStringAsync(data);

        // Assert
        Assert.Contains("\"Name\",\"Count\",\"Status\"", csv);
        Assert.Contains("\"Alice \"\"Admin\"\"\",\"42\",\"Active\"", csv);
        Assert.Contains("\"Bob, Manager\",\"7\",\"Disabled\"", csv);
    }

    [Fact]
    public async Task FormatAsJsonStringAsync_FormatsIndentedCamelCase()
    {
        // Arrange
        var service = new ExportService();
        var data = new SampleRecord("Test User", 10, "Enabled");

        // Act
        var json = await service.FormatAsJsonStringAsync(data);

        // Assert
        Assert.Contains("\"name\": \"Test User\"", json);
        Assert.Contains("\"count\": 10", json);
        Assert.Contains("\"status\": \"Enabled\"", json);
    }

    [Fact]
    public void FormatUserProfileReport_ContainsAllSectionsAndFields()
    {
        var service = new ExportService();
        var user = new Sol.Models.AdUser
        {
            DisplayName = "Jane Doe",
            SamAccountName = "jdoe",
            GivenName = "Jane",
            Surname = "Doe",
            Upn = "jdoe@corp.local",
            Email = "jane.doe@example.com",
            EmployeeId = "EMP12345",
            Sid = "S-1-5-21-123456789-500",
            OuPath = "OU=Users,DC=corp,DC=local",
            Title = "Senior Engineer",
            Department = "IT Infrastructure",
            Office = "HQ-3A",
            Manager = "John Boss",
            DirectReports = new List<string> { "Bob Smith", "Alice Cooper" },
            OfficePhone = "+1-555-0100",
            MobilePhone = "+1-555-0199",
            StreetAddress = "123 Main St",
            City = "Metropolis",
            State = "NY",
            PostalCode = "10001",
            AccountStatus = "Enabled",
            Groups = new List<string> { "Domain Users", "IT Admins" }
        };

        var report = service.FormatUserProfileReport(user);

        Assert.Contains("ACTIVE DIRECTORY USER PROFILE: Jane Doe (jdoe)", report);
        Assert.Contains("[ IDENTITY & DIRECTORY ]", report);
        Assert.Contains("Jane Doe", report);
        Assert.Contains("jdoe@corp.local", report);
        Assert.Contains("EMP12345", report);
        Assert.Contains("[ ORGANIZATION ]", report);
        Assert.Contains("Senior Engineer", report);
        Assert.Contains("IT Infrastructure", report);
        Assert.Contains("John Boss", report);
        Assert.Contains("Direct Reports (2)", report);
        Assert.Contains("- Bob Smith", report);
        Assert.Contains("[ CONTACT INFORMATION ]", report);
        Assert.Contains("+1-555-0100", report);
        Assert.Contains("123 Main St", report);
        Assert.Contains("[ ACCOUNT & SECURITY STATUS ]", report);
        Assert.Contains("Account Status:      Enabled", report);
        Assert.Contains("[ GROUP MEMBERSHIPS (2) ]", report);
        Assert.Contains("- IT Admins", report);
    }

    [Fact]
    public void FormatComputerProfileReport_ContainsAllSectionsAndDiagnostics()
    {
        var service = new ExportService();
        var computer = new Sol.Models.AdComputer
        {
            Name = "SRV-PROD-01",
            SamAccountName = "SRV-PROD-01$",
            DnsHostName = "srv-prod-01.corp.local",
            IPv4Address = "10.0.0.50",
            OperatingSystem = "Windows Server 2025 Datacenter",
            OperatingSystemVersion = "10.0.26100",
            AccountStatus = "Enabled",
            IsEnabled = true,
            OuPath = "OU=Servers,DC=corp,DC=local",
            Groups = new List<string> { "Domain Computers", "File Servers" },
            BitLockerKeys = new List<Sol.Models.BitLockerKeyInfo>
            {
                new() { KeyId = "K1", RecoveryPassword = "123456-789012-345678", Created = new System.DateTime(2026, 1, 1) }
            }
        };

        var hardware = new Sol.Models.ComputerHardwareSnapshot
        {
            IsSuccess = true,
            Manufacturer = "Dell Inc.",
            Model = "PowerEdge R750",
            SerialNumber = "TAG1234",
            BiosVersion = "2.1.4",
            BiosReleaseDate = "2025-11-01",
            CpuName = "Intel Xeon Gold 6330",
            TotalMemoryFormatted = "128 GB",
            BuildNumber = "26100",
            DisplayVersion = "24H2"
        };

        var uptime = new Sol.Models.ComputerUptimeSnapshot
        {
            IsSuccess = true,
            Uptime = System.TimeSpan.FromDays(45) + System.TimeSpan.FromHours(3),
            LastBootUpTime = new System.DateTime(2026, 7, 20, 12, 0, 0),
            IsRebootPending = false,
            IsRebootStatusKnown = true
        };

        var disk = new Sol.Models.ComputerDiskSnapshot
        {
            IsSuccess = true,
            Drives = new List<Sol.Models.ComputerDiskDriveInfo>
            {
                new()
                {
                    DeviceId = "C:",
                    VolumeName = "OS",
                    FileSystem = "NTFS",
                    TotalBytes = 1000UL * 1024 * 1024 * 1024,
                    FreeBytes = 500UL * 1024 * 1024 * 1024
                }
            }
        };

        var sessions = new Sol.Models.ComputerSessionSnapshot
        {
            IsSuccess = true,
            Sessions = new List<Sol.Models.ComputerSessionInfo>
            {
                new()
                {
                    SessionId = 2,
                    Username = "admin",
                    Domain = "CORP",
                    SessionType = Sol.Models.ComputerSessionType.Console
                }
            }
        };

        var bitlocker = new Sol.Models.ComputerBitLockerSnapshot
        {
            IsSuccess = true,
            DriveLetter = "C:",
            ProtectionStatus = 1,
            ConversionStatus = 1,
            EncryptionMethod = 7
        };

        var report = service.FormatComputerProfileReport(
            computer,
            hardware,
            uptime,
            disk,
            null,
            sessions,
            bitlocker,
            "https://dell.com/support");

        Assert.Contains("COMPUTER PROFILE & DIAGNOSTICS: SRV-PROD-01", report);
        Assert.Contains("[ ACTIVE DIRECTORY & NETWORK IDENTITY ]", report);
        Assert.Contains("10.0.0.50", report);
        Assert.Contains("[ HARDWARE & BIOS DIAGNOSTICS ]", report);
        Assert.Contains("PowerEdge R750", report);
        Assert.Contains("TAG1234", report);
        Assert.Contains("https://dell.com/support", report);
        Assert.Contains("[ SYSTEM UPTIME & REBOOT STATUS ]", report);
        Assert.Contains("45 days", report);
        Assert.Contains("[ STORAGE & LOGICAL DISK DRIVES ]", report);
        Assert.Contains("C: (OS)", report);
        Assert.Contains("NTFS", report);
        Assert.Contains("[ ACTIVE & DISCONNECTED LOGON SESSIONS ]", report);
        Assert.Contains("admin", report);
        Assert.Contains("[ BITLOCKER ENCRYPTION & RECOVERY KEYS ]", report);
        Assert.Contains("123456-789012-345678", report);
        Assert.Contains("[ GROUP MEMBERSHIPS (2) ]", report);
    }

    [Theory]
    [InlineData("=cmd|' /C calc'!A0", "'=cmd|' /C calc'!A0")]
    [InlineData("+1234", "'+1234")]
    [InlineData("-formula", "'-formula")]
    [InlineData("@SUM(A1:A10)", "'@SUM(A1:A10)")]
    [InlineData("\ttabprefix", "'\ttabprefix")]
    [InlineData("\rreturnprefix", "'\rreturnprefix")]
    [InlineData("SafeNormalText", "SafeNormalText")]
    public async Task FormatAsCsvStringAsync_FormulaInjectionPrefix_PrependsApostrophe(string input, string expected)
    {
        var service = new ExportService();
        var record = new SampleRecord(input, 1, "Active");
        var csv = await service.FormatAsCsvStringAsync([record]);

        Assert.Contains($"\"{expected}\"", csv);
    }
}
