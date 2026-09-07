using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Sol.Helpers;

public static class CommandLineHelper
{
    public const string UnlockFlag = "--unlock";

    /// <summary>
    /// Splits a command-line string into arguments respecting quotes.
    /// </summary>
    public static string[] SplitCommandLine(string? commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
            return [];

        var args = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < commandLine.Length; i++)
        {
            char c = commandLine[i];

            if (c == '\"')
            {
                inQuotes = !inQuotes;
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    args.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            args.Add(current.ToString());
        }

        return [.. args];
    }

    /// <summary>
    /// Extracts the target path if the '--unlock' argument is specified in the arguments list.
    /// </summary>
    public static string? TryGetUnlockPath(string[]? args)
    {
        if (args == null || args.Length == 0)
            return null;

        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], UnlockFlag, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                string target = args[i + 1].Trim('\"').Trim();
                if (!string.IsNullOrWhiteSpace(target) && !IsOptionFlag(target))
                {
                    return target;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the target path if the '--unlock' argument is specified in the raw command line string.
    /// </summary>
    public static string? TryGetUnlockPath(string? commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
            return null;

        var args = SplitCommandLine(commandLine);
        return TryGetUnlockPath(args);
    }

    private static bool IsOptionFlag(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        // Long option flags like --silent or --help
        if (token.StartsWith("--", StringComparison.Ordinal))
            return true;

        // Short option flags like -s or -f, but not filenames like -private.txt or paths like .\-file.txt
        if (token.StartsWith('-') && token.Length >= 2 && char.IsLetter(token[1]))
        {
            return !token.Contains('.') && !token.Contains('\\') && !token.Contains('/');
        }

        return false;
    }

    private static readonly Regex HostNameRegex = new(
        @"^[a-zA-Z0-9]([a-zA-Z0-9\-\.]*[a-zA-Z0-9])?$",
        RegexOptions.Compiled);

    /// <summary>
    /// Validates whether a given string is a safe host name, FQDN, or IPv4 address
    /// without shell metacharacters or argument injection vectors.
    /// </summary>
    public static bool IsValidHostNameOrAddress(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return false;

        string trimmed = host.Trim();
        if (trimmed.Length > 255 || trimmed.StartsWith('-'))
            return false;

        return HostNameRegex.IsMatch(trimmed);
    }
}
