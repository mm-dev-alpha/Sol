namespace Sol.Helpers;

/// <summary>
/// Provides utilities for parsing and manipulating Active Directory LDAP paths and distinguished names.
/// </summary>
public static class LdapPathHelper
{
    /// <summary>
    /// Extracts the parent container or Organizational Unit (OU) path from a Distinguished Name (DN),
    /// stripping the leaf Relative Distinguished Name (RDN) component (e.g. "CN=Max Mustermann").
    /// Handles escaped characters per RFC 4514 (e.g. "CN=Mustermann\, Max,OU=IT,...").
    /// If the path already represents a container or OU (starts with "OU=" or "DC="),
    /// or if no unescaped delimiter is found, it is returned intact.
    /// </summary>
    /// <param name="distinguishedName">The full Distinguished Name or container path.</param>
    /// <returns>The parent container / OU path, or string.Empty if input is null or whitespace.</returns>
    public static string ExtractParentContainer(string? distinguishedName)
    {
        if (string.IsNullOrWhiteSpace(distinguishedName))
            return string.Empty;

        string trimmed = distinguishedName.Trim();

        // If the path already begins with an Organizational Unit or Domain Component, it is already a container path.
        if (trimmed.StartsWith("OU=", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("DC=", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        // Find the first unescaped comma that separates the leaf RDN (e.g. "CN=...") from its parent container.
        int i = 0;
        int delimiterIndex = -1;

        while (i < trimmed.Length)
        {
            char c = trimmed[i];
            if (c == '\\' && i + 1 < trimmed.Length)
            {
                // Escaped character sequence; skip escape prefix and following character.
                i += 2;
            }
            else if (c == ',')
            {
                delimiterIndex = i;
                break;
            }
            else
            {
                i++;
            }
        }

        if (delimiterIndex >= 0 && delimiterIndex + 1 < trimmed.Length)
        {
            return trimmed[(delimiterIndex + 1)..].Trim();
        }

        return trimmed;
    }
}
