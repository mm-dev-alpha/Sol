namespace Sol.Helpers;

/// <summary>
/// Provides RFC 4515 LDAP filter value escaping.
/// </summary>
public static class LdapFilterHelper
{
    /// <summary>
    /// Escapes special characters in an LDAP filter value per RFC 4515.
    /// </summary>
    public static string Escape(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        return input
            .Replace("\\", "\\5c")
            .Replace("*", "\\2a")
            .Replace("(", "\\28")
            .Replace(")", "\\29")
            .Replace("\0", "\\00");
    }
}
