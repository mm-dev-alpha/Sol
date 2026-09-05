using System;
using System.Collections.Generic;
using Sol.Models;
using Sol.Services;
using Xunit;

namespace Sol.Tests;

public class EntityComparisonServiceTests
{
    private readonly IEntityComparisonService _service = new EntityComparisonService();

    [Fact]
    public void CompareUsers_IdenticalUsers_ZeroDifferences()
    {
        var userA = new AdUser
        {
            SamAccountName = "user1",
            GivenName = "Alice",
            Surname = "Smith",
            DisplayName = "Alice Smith",
            Email = "alice@corp.local",
            OuPath = "OU=Users,DC=corp",
            AccountStatus = "Active",
            Groups = ["Domain Users", "All-Staff"]
        };

        var userB = new AdUser
        {
            SamAccountName = "user1",
            GivenName = "Alice",
            Surname = "Smith",
            DisplayName = "Alice Smith",
            Email = "alice@corp.local",
            OuPath = "OU=Users,DC=corp",
            AccountStatus = "Active",
            Groups = ["Domain Users", "All-Staff"]
        };

        var result = _service.CompareUsers(userA, userB);

        Assert.Equal(0, result.DifferenceCount);
        Assert.Empty(result.Groups.OnlyInA);
        Assert.Empty(result.Groups.OnlyInB);
        Assert.Equal(2, result.Groups.SharedCount);
        Assert.Empty(result.Insights);
    }

    [Fact]
    public void CompareUsers_OuDivergence_GeneratesWarningInsight()
    {
        var userA = new AdUser { SamAccountName = "u1", OuPath = "OU=Sales,DC=corp", Groups = [] };
        var userB = new AdUser { SamAccountName = "u2", OuPath = "OU=IT,DC=corp", Groups = [] };

        var result = _service.CompareUsers(userA, userB);

        Assert.Contains(result.Insights, i => i.Severity == InsightSeverity.Warning && i.Title.Contains("Organizational Unit"));
    }

    [Fact]
    public void CompareUsers_GroupVenn_PartitionsCaseInsensitively()
    {
        var userA = new AdUser { SamAccountName = "u1", Groups = ["VPN-Users", "Domain Users", "finance-team"] };
        var userB = new AdUser { SamAccountName = "u2", Groups = ["domain users", "Cloud-Admins", "FINANCE-TEAM"] };

        var result = _service.CompareUsers(userA, userB);

        Assert.Single(result.Groups.OnlyInA);
        Assert.Equal("VPN-Users", result.Groups.OnlyInA[0]);
        Assert.Single(result.Groups.OnlyInB);
        Assert.Equal("Cloud-Admins", result.Groups.OnlyInB[0]);
        Assert.Equal(2, result.Groups.SharedCount);
    }

    [Fact]
    public void CompareUsers_AccountStatusMismatch_FlagsCaution()
    {
        var userA = new AdUser { SamAccountName = "u1", AccountStatus = "Active", IsLockedOut = false, Groups = [] };
        var userB = new AdUser { SamAccountName = "u2", AccountStatus = "Disabled", IsLockedOut = true, Groups = [] };

        var result = _service.CompareUsers(userA, userB);

        Assert.Contains(result.Insights, i => i.Severity == InsightSeverity.Caution && i.Title.Contains("Account Status"));
    }

    [Fact]
    public void CompareComputers_BitLockerDiscrepancy_GeneratesCautionInsight()
    {
        var compA = new AdComputer { Name = "PC-01", BitLockerKeys = [new BitLockerKeyInfo { KeyId = "k1" }] };
        var compB = new AdComputer { Name = "PC-02", BitLockerKeys = [] };

        var result = _service.CompareComputers(compA, compB);

        Assert.Contains(result.Insights, i => i.Severity == InsightSeverity.Caution && i.Title.Contains("BitLocker"));
    }

    [Fact]
    public void CompareComputers_OsMismatch_GeneratesInfoInsight()
    {
        var compA = new AdComputer { Name = "PC-01", OperatingSystem = "Windows 10 Enterprise", OperatingSystemVersion = "10.0.19045" };
        var compB = new AdComputer { Name = "PC-02", OperatingSystem = "Windows 11 Enterprise", OperatingSystemVersion = "10.0.22631" };

        var result = _service.CompareComputers(compA, compB);

        Assert.Contains(result.Insights, i => i.Severity == InsightSeverity.Info && i.Title.Contains("Operating System"));
    }
}
