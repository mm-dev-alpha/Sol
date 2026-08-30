using System;
using Windows.Security.Credentials;

namespace Sol.Helpers;

/// <summary>
/// Audit-proof credential manager for JIRA Personal Access Tokens and API Tokens
/// using Windows Credential Locker (PasswordVault / DPAPI).
/// </summary>
public static class JiraCredentialHelper
{
    private const string ResourceName = "Sol_Jira_Integration";
    private const string DataCenterPatUser = "DataCenter_PAT";
    private const string CloudTokenUser = "Cloud_ApiToken";

    // In-memory fallback for test harnesses or when PasswordVault is unavailable
    private static string? _testDataCenterPat;
    private static string? _testCloudToken;

    public static string GetSecret(string deploymentMode)
    {
        string username = string.Equals(deploymentMode, "Cloud", StringComparison.OrdinalIgnoreCase) 
            ? CloudTokenUser 
            : DataCenterPatUser;

        try
        {
            var vault = new PasswordVault();
            var cred = vault.Retrieve(ResourceName, username);
            if (cred != null)
            {
                cred.RetrievePassword();
                return cred.Password ?? string.Empty;
            }
        }
        catch
        {
            // Credential not found or vault unavailable
        }

        return string.Equals(deploymentMode, "Cloud", StringComparison.OrdinalIgnoreCase)
            ? (_testCloudToken ?? string.Empty)
            : (_testDataCenterPat ?? string.Empty);
    }

    public static void SaveSecret(string deploymentMode, string secret)
    {
        if (string.IsNullOrWhiteSpace(secret)) return;

        string username = string.Equals(deploymentMode, "Cloud", StringComparison.OrdinalIgnoreCase) 
            ? CloudTokenUser 
            : DataCenterPatUser;

        try
        {
            var vault = new PasswordVault();
            // Remove existing if present
            try
            {
                var existing = vault.Retrieve(ResourceName, username);
                if (existing != null)
                {
                    vault.Remove(existing);
                }
            }
            catch { }

            vault.Add(new PasswordCredential(ResourceName, username, secret));

            // Successfully persisted to Windows Credential Locker; wipe in-memory copy
            if (string.Equals(deploymentMode, "Cloud", StringComparison.OrdinalIgnoreCase))
                _testCloudToken = null;
            else
                _testDataCenterPat = null;
        }
        catch
        {
            // If PasswordVault is unavailable (e.g. unit test harness), preserve in-memory fallback
            if (string.Equals(deploymentMode, "Cloud", StringComparison.OrdinalIgnoreCase))
                _testCloudToken = secret;
            else
                _testDataCenterPat = secret;
        }
    }

    public static void ClearSecret(string deploymentMode)
    {
        string username = string.Equals(deploymentMode, "Cloud", StringComparison.OrdinalIgnoreCase) 
            ? CloudTokenUser 
            : DataCenterPatUser;

        if (string.Equals(deploymentMode, "Cloud", StringComparison.OrdinalIgnoreCase))
            _testCloudToken = null;
        else
            _testDataCenterPat = null;

        try
        {
            var vault = new PasswordVault();
            var existing = vault.Retrieve(ResourceName, username);
            if (existing != null)
            {
                vault.Remove(existing);
            }
        }
        catch { }
    }
}
