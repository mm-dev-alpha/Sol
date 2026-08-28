namespace Sol.Helpers;

/// <summary>
/// Utilities for converting Active Directory file-time values and formatting password/logon status.
/// </summary>
public static class TimeHelper
{
    /// <summary>
    /// Converts an AD file-time (Int64) to a nullable DateTime (UTC → Local).
    /// Returns null for sentinel values (≤ 0 or Int64.MaxValue).
    /// </summary>
    public static DateTime? ConvertFileTime(long fileTime)
    {
        try
        {
            if (fileTime <= 0 || fileTime == long.MaxValue)
                return null;

            return DateTime.FromFileTimeUtc(fileTime).ToLocalTime();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Returns a human-readable password expiry status string.
    /// </summary>
    public static string GetPasswordExpiryStatus(DateTime? expiryDate)
    {
        if (expiryDate is null)
            return Strings.S.PasswordStatusUnknown;

        var now = DateTime.Now;
        if (expiryDate < now)
            return Strings.S.PasswordExpired;

        var daysLeft = (expiryDate.Value - now).Days;
        return daysLeft switch
        {
            0 => Strings.S.PasswordExpiresToday,
            1 => Strings.S.PasswordExpiresTomorrow,
            _ => Strings.PasswordExpiresInDays(daysLeft)
        };
    }

    /// <summary>
    /// Extracts the CN (Common Name) portion from an Active Directory Distinguished Name.
    /// e.g. "CN=John Doe,OU=Users,DC=contoso,DC=com" → "John Doe"
    /// </summary>
    public static string ParseManagerName(string distinguishedName)
    {
        if (string.IsNullOrEmpty(distinguishedName))
            return string.Empty;

        if (distinguishedName.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
        {
            var commaIndex = distinguishedName.IndexOf(',');
            return commaIndex > 3
                ? distinguishedName[3..commaIndex]
                : distinguishedName[3..];
        }

        return distinguishedName;
    }
}
