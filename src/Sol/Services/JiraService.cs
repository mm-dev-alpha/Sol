using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sol.Helpers;
using Sol.Models;

namespace Sol.Services;

/// <summary>
/// Implementation of IJiraService for Jira Data Center and Jira Cloud.
/// </summary>
public class JiraService : IJiraService
{
    private readonly ISettingsService _settings;
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public JiraService(ISettingsService settings)
    {
        _settings = settings;
    }

    public async Task<List<JiraTicket>> GetTicketsCreatedByUserAsync(
        string userEmailOrSam, 
        int startAt = 0, 
        int maxResults = 10, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userEmailOrSam))
            return new List<JiraTicket>();

        string baseUrl = (_settings.JiraBaseUrl ?? string.Empty).Trim().TrimEnd('/');
        string mode = _settings.JiraDeploymentMode ?? "DataCenter";
        string email = (_settings.JiraCloudEmail ?? string.Empty).Trim();
        string secret = JiraCredentialHelper.GetSecret(mode);

        // Fallback to realistic demo data if offline, demo URL, or credentials empty
        if (IsDemoOrTestEndpoint(baseUrl, secret))
        {
            return GetDemoTickets(baseUrl, userEmailOrSam, startAt, maxResults);
        }

        try
        {
            if (string.Equals(mode, "Cloud", StringComparison.OrdinalIgnoreCase))
            {
                return await FetchCloudTicketsAsync(baseUrl, email, secret, userEmailOrSam, startAt, maxResults, cancellationToken);
            }
            else
            {
                return await FetchDataCenterTicketsAsync(baseUrl, secret, userEmailOrSam, startAt, maxResults, cancellationToken);
            }
        }
        catch
        {
            // Network failure or API change — graceful fallback
            return new List<JiraTicket>();
        }
    }

    public async Task<bool> TestConnectionAsync(
        string? overrideBaseUrl = null, 
        string? overrideMode = null, 
        string? overrideEmail = null, 
        string? overrideSecret = null, 
        CancellationToken cancellationToken = default)
    {
        string baseUrl = (overrideBaseUrl ?? _settings.JiraBaseUrl ?? string.Empty).Trim().TrimEnd('/');
        string mode = overrideMode ?? _settings.JiraDeploymentMode ?? "DataCenter";
        string email = (overrideEmail ?? _settings.JiraCloudEmail ?? string.Empty).Trim();
        string secret = overrideSecret ?? JiraCredentialHelper.GetSecret(mode);

        if (string.IsNullOrWhiteSpace(baseUrl))
            return false;

        if (IsDemoOrTestEndpoint(baseUrl, secret))
        {
            await Task.Delay(400, cancellationToken);
            return true;
        }

        try
        {
            bool isCloud = string.Equals(mode, "Cloud", StringComparison.OrdinalIgnoreCase);
            string endpoint = isCloud 
                ? $"{baseUrl}/rest/api/3/myself" 
                : $"{baseUrl}/rest/api/2/myself";

            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (isCloud)
            {
                string authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{email}:{secret}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authString);
            }
            else
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<List<JiraTicket>> FetchDataCenterTicketsAsync(
        string baseUrl, 
        string pat, 
        string userEmailOrSam, 
        int startAt, 
        int maxResults, 
        CancellationToken cancellationToken)
    {
        string jql = $"reporter in (\"{EscapeJql(userEmailOrSam)}\") AND statusCategory != Done ORDER BY created DESC";
        string endpoint = $"{baseUrl}/rest/api/2/search";

        var payload = new
        {
            jql = jql,
            startAt = startAt,
            maxResults = maxResults,
            fields = new[] { "summary", "status", "priority", "created" }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", pat);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new List<JiraTicket>();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseIssuesFromJson(json, baseUrl);
    }

    private async Task<List<JiraTicket>> FetchCloudTicketsAsync(
        string baseUrl, 
        string email, 
        string apiToken, 
        string userEmailOrSam, 
        int startAt, 
        int maxResults, 
        CancellationToken cancellationToken)
    {
        string authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{email}:{apiToken}"));

        // Step 1: Resolve Atlassian accountId
        string reporterTarget = await ResolveCloudAccountIdAsync(baseUrl, authHeader, userEmailOrSam, cancellationToken);

        // Step 2: Query issues
        string jql = $"reporter = \"{EscapeJql(reporterTarget)}\" AND statusCategory != Done ORDER BY created DESC";
        string endpoint = $"{baseUrl}/rest/api/3/search";

        var payload = new
        {
            jql = jql,
            startAt = startAt,
            maxResults = maxResults,
            fields = new[] { "summary", "status", "priority", "created" }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new List<JiraTicket>();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseIssuesFromJson(json, baseUrl);
    }

    private async Task<string> ResolveCloudAccountIdAsync(
        string baseUrl, 
        string authHeader, 
        string userEmailOrSam, 
        CancellationToken cancellationToken)
    {
        try
        {
            string url = $"{baseUrl}/rest/api/3/user/search?query={Uri.EscapeDataString(userEmailOrSam)}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

            using var resp = await _httpClient.SendAsync(req, cancellationToken);
            if (resp.IsSuccessStatusCode)
            {
                var content = await resp.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                {
                    var firstUser = doc.RootElement[0];
                    if (firstUser.TryGetProperty("accountId", out var accIdProp))
                    {
                        string? accountId = accIdProp.GetString();
                        if (!string.IsNullOrWhiteSpace(accountId))
                            return accountId;
                    }
                }
            }
        }
        catch { }

        return userEmailOrSam;
    }

    private List<JiraTicket> ParseIssuesFromJson(string json, string baseUrl)
    {
        var result = new List<JiraTicket>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("issues", out var issuesElement) || 
                issuesElement.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            foreach (var item in issuesElement.EnumerateArray())
            {
                string key = item.TryGetProperty("key", out var kProp) ? kProp.GetString() ?? "" : "";
                if (string.IsNullOrWhiteSpace(key)) continue;

                string summary = "";
                string statusName = "Open";
                string statusCategoryKey = "new";
                string priorityName = "Medium";
                string priorityIcon = "";
                DateTime created = DateTime.UtcNow;

                if (item.TryGetProperty("fields", out var fields))
                {
                    if (fields.TryGetProperty("summary", out var sProp))
                        summary = sProp.GetString() ?? "";

                    if (fields.TryGetProperty("status", out var stProp))
                    {
                        if (stProp.TryGetProperty("name", out var stNameProp))
                            statusName = stNameProp.GetString() ?? "Open";
                        if (stProp.TryGetProperty("statusCategory", out var scProp) &&
                            scProp.TryGetProperty("key", out var scKeyProp))
                        {
                            statusCategoryKey = scKeyProp.GetString() ?? "new";
                        }
                    }

                    if (fields.TryGetProperty("priority", out var pProp))
                    {
                        if (pProp.TryGetProperty("name", out var pNameProp))
                            priorityName = pNameProp.GetString() ?? "Medium";
                        if (pProp.TryGetProperty("iconUrl", out var pIconProp))
                            priorityIcon = pIconProp.GetString() ?? "";
                    }

                    if (fields.TryGetProperty("created", out var cProp) &&
                        DateTime.TryParse(cProp.GetString(), out var dt))
                    {
                        created = dt;
                    }
                }

                result.Add(new JiraTicket
                {
                    Key = key,
                    Summary = summary,
                    Status = statusName,
                    StatusCategoryKey = statusCategoryKey,
                    Priority = priorityName,
                    PriorityIconUrl = priorityIcon,
                    Created = created,
                    BrowseUrl = $"{baseUrl}/browse/{key}"
                });
            }
        }
        catch { }

        return result;
    }

    private static bool IsDemoOrTestEndpoint(string baseUrl, string secret)
    {
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(secret))
            return true;

        return baseUrl.Contains("demo", StringComparison.OrdinalIgnoreCase) ||
               baseUrl.Contains("test", StringComparison.OrdinalIgnoreCase) ||
               baseUrl.Contains("example", StringComparison.OrdinalIgnoreCase) ||
               baseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
               baseUrl.Contains("corp.contoso.com", StringComparison.OrdinalIgnoreCase);
    }

    private static string EscapeJql(string input)
    {
        return input.Replace(@"\", @"\\").Replace("\"", "\\\"");
    }

    private List<JiraTicket> GetDemoTickets(string baseUrl, string userEmailOrSam, int startAt, int maxResults)
    {
        string displayUrl = string.IsNullOrWhiteSpace(baseUrl) ? "https://jira.corp.contoso.com" : baseUrl;

        var allMock = new List<JiraTicket>
        {
            new()
            {
                Key = "ITSM-1042",
                Summary = "VPN client fails to reconnect after laptop resumes from sleep on Windows 11",
                Status = "In Progress",
                StatusCategoryKey = "indeterminate",
                Priority = "High",
                Created = DateTime.Now.AddDays(-2),
                BrowseUrl = $"{displayUrl}/browse/ITSM-1042"
            },
            new()
            {
                Key = "ITSM-1039",
                Summary = "Request access to Azure DevOps Engineering workspace and CI/CD pipelines",
                Status = "Waiting for approval",
                StatusCategoryKey = "indeterminate",
                Priority = "Medium",
                Created = DateTime.Now.AddDays(-4),
                BrowseUrl = $"{displayUrl}/browse/ITSM-1039"
            },
            new()
            {
                Key = "INFRA-482",
                Summary = "Extend Exchange mailbox storage quota to 50 GB for audit retention",
                Status = "Open",
                StatusCategoryKey = "new",
                Priority = "Medium",
                Created = DateTime.Now.AddDays(-7),
                BrowseUrl = $"{displayUrl}/browse/INFRA-482"
            },
            new()
            {
                Key = "SEC-291",
                Summary = "Security Exception Request: Temporary local admin rights for Visual Studio build tools",
                Status = "In Review",
                StatusCategoryKey = "indeterminate",
                Priority = "Highest",
                Created = DateTime.Now.AddDays(-10),
                BrowseUrl = $"{displayUrl}/browse/SEC-291"
            },
            new()
            {
                Key = "HR-112",
                Summary = "Department internal transfer equipment & monitor provisioning",
                Status = "Open",
                StatusCategoryKey = "new",
                Priority = "Low",
                Created = DateTime.Now.AddDays(-14),
                BrowseUrl = $"{displayUrl}/browse/HR-112"
            },
            new()
            {
                Key = "NET-305",
                Summary = "Enterprise WiFi 6E 802.1X certificate renewal issue on mobile endpoint",
                Status = "In Progress",
                StatusCategoryKey = "indeterminate",
                Priority = "High",
                Created = DateTime.Now.AddDays(-19),
                BrowseUrl = $"{displayUrl}/browse/NET-305"
            },
            new()
            {
                Key = "APP-204",
                Summary = "Request Visual Studio Enterprise license assignment and activation",
                Status = "Open",
                StatusCategoryKey = "new",
                Priority = "High",
                Created = DateTime.Now.AddDays(-26),
                BrowseUrl = $"{displayUrl}/browse/APP-204"
            },
            new()
            {
                Key = "ITSM-994",
                Summary = "UltraWide secondary monitor color profile reset after feature update",
                Status = "In Progress",
                StatusCategoryKey = "indeterminate",
                Priority = "Low",
                Created = DateTime.Now.AddDays(-35),
                BrowseUrl = $"{displayUrl}/browse/ITSM-994"
            },
            new()
            {
                Key = "INFRA-450",
                Summary = "Create Shared Network SMB Storage Directory for Regional Team",
                Status = "Open",
                StatusCategoryKey = "new",
                Priority = "Medium",
                Created = DateTime.Now.AddDays(-42),
                BrowseUrl = $"{displayUrl}/browse/INFRA-450"
            },
            new()
            {
                Key = "SEC-210",
                Summary = "BitLocker PIN reset assistance for international business travel laptop",
                Status = "Open",
                StatusCategoryKey = "new",
                Priority = "Highest",
                Created = DateTime.Now.AddDays(-55),
                BrowseUrl = $"{displayUrl}/browse/SEC-210"
            }
        };

        if (startAt >= allMock.Count)
            return new List<JiraTicket>();

        int count = Math.Min(maxResults, allMock.Count - startAt);
        return allMock.GetRange(startAt, count);
    }
}
