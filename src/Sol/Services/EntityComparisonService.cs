using System;
using System.Collections.Generic;
using System.Linq;
using Sol.Models;

namespace Sol.Services;

/// <summary>
/// Deterministic comparison service that computes property differences, group Venn distributions,
/// and smart administrative insights for Users and Computers.
/// </summary>
public class EntityComparisonService : IEntityComparisonService
{
    public UserComparisonResult CompareUsers(AdUser userA, AdUser userB)
    {
        ArgumentNullException.ThrowIfNull(userA);
        ArgumentNullException.ThrowIfNull(userB);

        var propertyDiffs = new List<PropertyDiffItem>();
        var insights = new List<SmartInsight>();

        // 1. Identity Category
        AddDiff(propertyDiffs, "Identity", "Given Name", userA.GivenName, userB.GivenName);
        AddDiff(propertyDiffs, "Identity", "Surname", userA.Surname, userB.Surname);
        AddDiff(propertyDiffs, "Identity", "Display Name", userA.DisplayName, userB.DisplayName);
        AddDiff(propertyDiffs, "Identity", "SAM Account Name", userA.SamAccountName, userB.SamAccountName);
        AddDiff(propertyDiffs, "Identity", "User Principal Name (UPN)", userA.Upn, userB.Upn);
        AddDiff(propertyDiffs, "Identity", "Email", userA.Email, userB.Email);
        AddDiff(propertyDiffs, "Identity", "Employee ID", userA.EmployeeId, userB.EmployeeId);
        AddDiff(propertyDiffs, "Identity", "Organizational Unit (OU)", userA.OuPath, userB.OuPath);
        AddDiff(propertyDiffs, "Identity", "Security Identifier (SID)", userA.Sid, userB.Sid);
        AddDiff(propertyDiffs, "Identity", "Description", userA.Description, userB.Description);
        AddDiff(propertyDiffs, "Identity", "Web Page", userA.WebPage, userB.WebPage);

        // 2. Organization Category
        AddDiff(propertyDiffs, "Organization", "Department", userA.Department, userB.Department);
        AddDiff(propertyDiffs, "Organization", "Title", userA.Title, userB.Title);
        AddDiff(propertyDiffs, "Organization", "Manager", userA.Manager, userB.Manager);
        AddDiff(propertyDiffs, "Organization", "Direct Reports Count", userA.DirectReports.Count.ToString(), userB.DirectReports.Count.ToString());

        // 3. Contact Category
        AddDiff(propertyDiffs, "Contact", "Office", userA.Office, userB.Office);
        AddDiff(propertyDiffs, "Contact", "Office Phone", userA.OfficePhone, userB.OfficePhone);
        AddDiff(propertyDiffs, "Contact", "Mobile Phone", userA.MobilePhone, userB.MobilePhone);
        AddDiff(propertyDiffs, "Contact", "Street Address", userA.StreetAddress, userB.StreetAddress);
        AddDiff(propertyDiffs, "Contact", "City", userA.City, userB.City);
        AddDiff(propertyDiffs, "Contact", "State / Province", userA.State, userB.State);
        AddDiff(propertyDiffs, "Contact", "Postal Code", userA.PostalCode, userB.PostalCode);

        // 4. Account Status & Flags Category
        AddDiff(propertyDiffs, "Account Status & Flags", "Account Status", userA.AccountStatus, userB.AccountStatus);
        AddDiff(propertyDiffs, "Account Status & Flags", "Account Expires Status", userA.AccountExpiresStatus, userB.AccountExpiresStatus);
        AddDiff(propertyDiffs, "Account Status & Flags", "Account Expiration Date", FormatDate(userA.AccountExpires), FormatDate(userB.AccountExpires));
        AddDiff(propertyDiffs, "Account Status & Flags", "Is Locked Out", userA.IsLockedOut.ToString(), userB.IsLockedOut.ToString());
        AddDiff(propertyDiffs, "Account Status & Flags", "Password Never Expires", userA.PasswordNeverExpires.ToString(), userB.PasswordNeverExpires.ToString());

        // 5. Password & Logon Category
        AddDiff(propertyDiffs, "Password & Logon", "Password Expiry Status", userA.PasswordExpiryStatus, userB.PasswordExpiryStatus);
        AddDiff(propertyDiffs, "Password & Logon", "Password Last Set", FormatDate(userA.PasswordLastSet), FormatDate(userB.PasswordLastSet));
        AddDiff(propertyDiffs, "Password & Logon", "Bad Password Count", userA.BadPasswordCount.ToString(), userB.BadPasswordCount.ToString());
        AddDiff(propertyDiffs, "Password & Logon", "Last Logon", FormatDate(userA.LastLogon), FormatDate(userB.LastLogon));
        AddDiff(propertyDiffs, "Password & Logon", "Last Logon Timestamp", FormatDate(userA.LastLogonTimestamp), FormatDate(userB.LastLogonTimestamp));

        // 6. Object Metadata Category
        AddDiff(propertyDiffs, "Metadata", "Created", FormatDate(userA.Created), FormatDate(userB.Created));
        AddDiff(propertyDiffs, "Metadata", "Modified", FormatDate(userA.Modified), FormatDate(userB.Modified));

        // Group Venn Partitioning
        var groupDiff = ComputeGroupDiff(userA.Groups, userB.Groups);

        // Smart Discrepancy Insights
        EvaluateOuInsight(insights, userA.OuPath, userB.OuPath);

        if (!string.Equals(userA.AccountStatus, userB.AccountStatus, StringComparison.OrdinalIgnoreCase) ||
            userA.IsLockedOut != userB.IsLockedOut)
        {
            insights.Add(new SmartInsight(
                "Account Status Discrepancy",
                $"Account operational statuses diverge: Target A is '{userA.AccountStatus}' (Locked: {userA.IsLockedOut}), whereas Target B is '{userB.AccountStatus}' (Locked: {userB.IsLockedOut}).",
                InsightSeverity.Caution,
                "\uE783"
            ));
        }

        if (userA.PasswordNeverExpires != userB.PasswordNeverExpires)
        {
            insights.Add(new SmartInsight(
                "Password Policy Discrepancy",
                $"PasswordNeverExpires flag differs: Target A is {userA.PasswordNeverExpires}, Target B is {userB.PasswordNeverExpires}.",
                InsightSeverity.Info,
                "\uE72E"
            ));
        }

        if (userA.BadPasswordCount >= 3 || userB.BadPasswordCount >= 3)
        {
            insights.Add(new SmartInsight(
                "Elevated Bad Password Count",
                $"Elevated failed password attempts detected: Target A has {userA.BadPasswordCount}, Target B has {userB.BadPasswordCount}.",
                InsightSeverity.Warning,
                "\uE7BA"
            ));
        }

        if (groupDiff.HasDifferences)
        {
            insights.Add(new SmartInsight(
                "Group Membership Asymmetry",
                $"Membership differs across {groupDiff.UniqueToACount + groupDiff.UniqueToBCount} groups ({groupDiff.UniqueToACount} unique to Target A, {groupDiff.UniqueToBCount} unique to Target B, {groupDiff.SharedCount} shared).",
                InsightSeverity.Info,
                "\uE716"
            ));
        }

        int totalDiffCount = propertyDiffs.Count(p => p.IsDifferent) +
                             (groupDiff.HasDifferences ? groupDiff.UniqueToACount + groupDiff.UniqueToBCount : 0);

        return new UserComparisonResult(userA, userB, groupDiff, propertyDiffs, insights, totalDiffCount);
    }

    public ComputerComparisonResult CompareComputers(AdComputer computerA, AdComputer computerB)
    {
        ArgumentNullException.ThrowIfNull(computerA);
        ArgumentNullException.ThrowIfNull(computerB);

        var propertyDiffs = new List<PropertyDiffItem>();
        var insights = new List<SmartInsight>();

        // 1. Identity & Network
        AddDiff(propertyDiffs, "Identity & Network", "Name", computerA.Name, computerB.Name);
        AddDiff(propertyDiffs, "Identity & Network", "SAM Account Name", computerA.SamAccountName, computerB.SamAccountName);
        AddDiff(propertyDiffs, "Identity & Network", "DNS Host Name", computerA.DnsHostName, computerB.DnsHostName);
        AddDiff(propertyDiffs, "Identity & Network", "Operating System", computerA.OperatingSystem, computerB.OperatingSystem);
        AddDiff(propertyDiffs, "Identity & Network", "Operating System Version", computerA.OperatingSystemVersion, computerB.OperatingSystemVersion);
        AddDiff(propertyDiffs, "Identity & Network", "Organizational Unit (OU)", computerA.OuPath, computerB.OuPath);
        AddDiff(propertyDiffs, "Identity & Network", "IPv4 Address", computerA.IPv4Address, computerB.IPv4Address);
        AddDiff(propertyDiffs, "Identity & Network", "Location", computerA.Location, computerB.Location);
        AddDiff(propertyDiffs, "Identity & Network", "Managed By", computerA.ManagedBy, computerB.ManagedBy);
        AddDiff(propertyDiffs, "Identity & Network", "Security Identifier (SID)", computerA.Sid, computerB.Sid);
        AddDiff(propertyDiffs, "Identity & Network", "Description", computerA.Description, computerB.Description);

        // 2. Status & Activity
        AddDiff(propertyDiffs, "Status & Activity", "Account Status", computerA.AccountStatus, computerB.AccountStatus);
        AddDiff(propertyDiffs, "Status & Activity", "Is Enabled", computerA.IsEnabled.ToString(), computerB.IsEnabled.ToString());
        AddDiff(propertyDiffs, "Status & Activity", "Last Logon", FormatDate(computerA.LastLogon), FormatDate(computerB.LastLogon));
        AddDiff(propertyDiffs, "Status & Activity", "Last Logon Timestamp", FormatDate(computerA.LastLogonTimestamp), FormatDate(computerB.LastLogonTimestamp));
        AddDiff(propertyDiffs, "Status & Activity", "Password Last Set", FormatDate(computerA.PasswordLastSet), FormatDate(computerB.PasswordLastSet));

        // 3. Security & BitLocker
        AddDiff(propertyDiffs, "Security & BitLocker", "BitLocker Keys Count", computerA.BitLockerKeys.Count.ToString(), computerB.BitLockerKeys.Count.ToString());

        // 4. Metadata
        AddDiff(propertyDiffs, "Metadata", "Created", FormatDate(computerA.Created), FormatDate(computerB.Created));
        AddDiff(propertyDiffs, "Metadata", "Modified", FormatDate(computerA.Modified), FormatDate(computerB.Modified));

        // Group Venn Partitioning
        var groupDiff = ComputeGroupDiff(computerA.Groups, computerB.Groups);

        // Smart Discrepancy Insights
        EvaluateOuInsight(insights, computerA.OuPath, computerB.OuPath);

        if (!string.Equals(computerA.OperatingSystem, computerB.OperatingSystem, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(computerA.OperatingSystemVersion, computerB.OperatingSystemVersion, StringComparison.OrdinalIgnoreCase))
        {
            insights.Add(new SmartInsight(
                "Operating System Disparity",
                $"OS mismatch detected: Target A runs '{computerA.OperatingSystem} {computerA.OperatingSystemVersion}', while Target B runs '{computerB.OperatingSystem} {computerB.OperatingSystemVersion}'.",
                InsightSeverity.Info,
                "\uE7F8"
            ));
        }

        if (computerA.BitLockerKeys.Count != computerB.BitLockerKeys.Count)
        {
            insights.Add(new SmartInsight(
                "BitLocker Protection Discrepancy",
                $"BitLocker key recovery backup disparity: Target A has {computerA.BitLockerKeys.Count} recovery keys in AD, whereas Target B has {computerB.BitLockerKeys.Count} keys.",
                InsightSeverity.Caution,
                "\uE72E"
            ));
        }

        if (computerA.IsEnabled != computerB.IsEnabled ||
            !string.Equals(computerA.AccountStatus, computerB.AccountStatus, StringComparison.OrdinalIgnoreCase))
        {
            insights.Add(new SmartInsight(
                "Account Status Discrepancy",
                $"Computer account status differs: Target A is '{computerA.AccountStatus}' (Enabled: {computerA.IsEnabled}), Target B is '{computerB.AccountStatus}' (Enabled: {computerB.IsEnabled}).",
                InsightSeverity.Caution,
                "\uE783"
            ));
        }

        if (groupDiff.HasDifferences)
        {
            insights.Add(new SmartInsight(
                "Group Membership Asymmetry",
                $"Computer group memberships diverge: Target A has {groupDiff.UniqueToACount} unique groups, Target B has {groupDiff.UniqueToBCount} unique groups ({groupDiff.SharedCount} shared).",
                InsightSeverity.Info,
                "\uE716"
            ));
        }

        int totalDiffCount = propertyDiffs.Count(p => p.IsDifferent) +
                             (groupDiff.HasDifferences ? groupDiff.UniqueToACount + groupDiff.UniqueToBCount : 0);

        return new ComputerComparisonResult(computerA, computerB, groupDiff, propertyDiffs, insights, totalDiffCount);
    }

    private static void EvaluateOuInsight(List<SmartInsight> insights, string ouA, string ouB)
    {
        if (!string.IsNullOrWhiteSpace(ouA) &&
            !string.IsNullOrWhiteSpace(ouB) &&
            !string.Equals(ouA, ouB, StringComparison.OrdinalIgnoreCase))
        {
            insights.Add(new SmartInsight(
                "Organizational Unit Divergence",
                $"Objects reside in different OUs ('{ouA}' vs '{ouB}'). Divergent Group Policy Objects (GPOs), administrative delegation, and baseline permissions apply.",
                InsightSeverity.Warning,
                "\uE7BA"
            ));
        }
    }

    private static GroupDiffSummary ComputeGroupDiff(IEnumerable<string>? listA, IEnumerable<string>? listB)
    {
        var setA = new HashSet<string>(listA ?? [], StringComparer.OrdinalIgnoreCase);
        var setB = new HashSet<string>(listB ?? [], StringComparer.OrdinalIgnoreCase);

        var onlyInA = setA.Except(setB, StringComparer.OrdinalIgnoreCase).OrderBy(g => g, StringComparer.OrdinalIgnoreCase).ToList();
        var inBoth = setA.Intersect(setB, StringComparer.OrdinalIgnoreCase).OrderBy(g => g, StringComparer.OrdinalIgnoreCase).ToList();
        var onlyInB = setB.Except(setA, StringComparer.OrdinalIgnoreCase).OrderBy(g => g, StringComparer.OrdinalIgnoreCase).ToList();

        return new GroupDiffSummary(onlyInA, inBoth, onlyInB);
    }

    private static void AddDiff(List<PropertyDiffItem> list, string category, string propertyName, string? valueA, string? valueB)
    {
        string normA = (valueA ?? string.Empty).Trim();
        string normB = (valueB ?? string.Empty).Trim();
        bool isDiff = !string.Equals(normA, normB, StringComparison.OrdinalIgnoreCase);

        list.Add(new PropertyDiffItem(category, propertyName, normA, normB, isDiff));
    }

    private static string FormatDate(DateTime? dt)
    {
        return dt.HasValue ? dt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Never / Unset";
    }
}
