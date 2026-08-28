using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using Sol.Models;
using Sol.Helpers;

namespace Sol.Services;

public class ActiveDirectoryService : IActiveDirectoryService
{
    private readonly ISettingsService _settings;

    public ActiveDirectoryService(ISettingsService settings)
    {
        _settings = settings;
    }

    private string AdDomain => _settings.AdDomain ?? string.Empty;

    public async Task<List<AdUser>> SearchUsersAsync(string query)
    {
        return await Task.Run(() =>
        {
            var results = new List<AdUser>();
            var escapedQuery = LdapFilterHelper.Escape(query);
            
            string ldapPath;
            if (!string.IsNullOrWhiteSpace(AdDomain))
            {
                ldapPath = $"LDAP://{AdDomain}";
            }
            else
            {
                using var rootDse = new DirectoryEntry("LDAP://RootDSE");
                var defaultNamingContext = rootDse.Properties["defaultNamingContext"].Value?.ToString();
                
                if (string.IsNullOrEmpty(defaultNamingContext))
                    throw new Exception("Could not determine default naming context from AD. Consider setting 'AdDomain' in Settings.");

                ldapPath = $"LDAP://{defaultNamingContext}";
            }

            using var entry = new DirectoryEntry(ldapPath);
            using var searcher = new DirectorySearcher(entry);
            
            searcher.Filter = $"(&(objectCategory=person)(objectClass=user)(|(sAMAccountName={escapedQuery})(displayName={escapedQuery})))";
            SetPropertiesToLoad(searcher);
            searcher.SizeLimit = 25;
            
            var matches = searcher.FindAll();
            
            if (matches.Count == 0)
            {
                searcher.Filter = $"(&(objectCategory=person)(objectClass=user)(displayName=*{escapedQuery}*))";
                matches = searcher.FindAll();
            }

            foreach (SearchResult match in matches)
            {
                results.Add(MapToUserModel(match));
            }

            return results;
        });
    }

    private void SetPropertiesToLoad(DirectorySearcher searcher)
    {
        string[] props = {
            "cn", "displayName", "sAMAccountName", "userPrincipalName", "mail",
            "description", "department", "title", "manager", "telephoneNumber",
            "mobile", "userAccountControl", "pwdLastSet", "lastLogon", "lastLogonTimestamp",
            "msDS-UserPasswordExpiryTimeComputed", "memberOf", "distinguishedName",
            "employeeID", "physicalDeliveryOfficeName", "streetAddress", "l", "st",
            "postalCode", "lockoutTime", "badPwdCount", "badPasswordTime", "directReports", "accountExpires",
            "givenName", "sn", "wWWHomePage", "objectSid"
        };
        searcher.PropertiesToLoad.AddRange(props);
            searcher.CacheResults = false;
    }

    private AdUser MapToUserModel(SearchResult result)
    {
        var model = new AdUser
        {
            GivenName = GetStringProperty(result, "givenName"),
            Surname = GetStringProperty(result, "sn"),
            DisplayName = GetStringProperty(result, "displayName"),
            SamAccountName = GetStringProperty(result, "sAMAccountName"),
            Sid = GetSidProperty(result),
            Upn = GetStringProperty(result, "userPrincipalName"),
            Email = GetStringProperty(result, "mail"),
            Department = GetStringProperty(result, "department"),
            Title = GetStringProperty(result, "title"),
            Description = GetStringProperty(result, "description"),
            WebPage = GetStringProperty(result, "wWWHomePage"),
            
            EmployeeId = GetStringProperty(result, "employeeID"),
            OuPath = GetStringProperty(result, "distinguishedName"),
            Office = GetStringProperty(result, "physicalDeliveryOfficeName"),
            OfficePhone = GetStringProperty(result, "telephoneNumber"),
            MobilePhone = GetStringProperty(result, "mobile"),
            StreetAddress = GetStringProperty(result, "streetAddress"),
            City = GetStringProperty(result, "l"),
            State = GetStringProperty(result, "st"),
            PostalCode = GetStringProperty(result, "postalCode")
        };
        
        var managerDn = GetStringProperty(result, "manager");
        var directReportsList = new List<string>();
        var groupsList = new List<string>();

        if (result.Properties.Contains("directReports"))
        {
            foreach (var directReportDn in result.Properties["directReports"])
            {
                try
                {
                    if (directReportDn != null)
                        directReportsList.Add(TimeHelper.ParseManagerName(directReportDn.ToString() ?? ""));
                }
                catch
                {
                    // Ignore malformed DNs
                }
            }
        }

        if (result.Properties.Contains("memberOf"))
        {
            foreach (var groupDn in result.Properties["memberOf"])
            {
                groupsList.Add(TimeHelper.ParseManagerName(groupDn.ToString() ?? ""));
            }
        }

        bool isLockedOut = false;
        bool passwordNeverExpires = false;
        string accountStatus = "Unknown";

        if (result.Properties.Contains("userAccountControl") && result.Properties["userAccountControl"].Count > 0)
        {
            int uac = (int)result.Properties["userAccountControl"][0];
            bool isDisabled = (uac & 0x2) != 0;
            isLockedOut = (uac & 0x10) != 0;
            passwordNeverExpires = (uac & 0x10000) != 0;
            
            if (isLockedOut)
                accountStatus = "Locked out";
            else if (isDisabled)
                accountStatus = "Disabled";
            else
                accountStatus = "Enabled";
        }

        var lockoutTime = GetFileTimeProperty(result, "lockoutTime");
        if (lockoutTime.HasValue && lockoutTime.Value > DateTime.MinValue)
        {
            isLockedOut = true;
            accountStatus = "Locked out";
        }

        var pwdExpiry = GetFileTimeProperty(result, "msDS-UserPasswordExpiryTimeComputed");
        var accExpiry = GetFileTimeProperty(result, "accountExpires");
        
        return model with
        {
            Manager = TimeHelper.ParseManagerName(managerDn),
            DirectReports = directReportsList,
            Groups = groupsList,
            IsLockedOut = isLockedOut,
            PasswordNeverExpires = passwordNeverExpires,
            AccountStatus = accountStatus,
            LastLogon = GetFileTimeProperty(result, "lastLogon"),
            LastLogonTimestamp = GetFileTimeProperty(result, "lastLogonTimestamp"),
            PasswordLastSet = GetFileTimeProperty(result, "pwdLastSet"),
            PasswordExpiry = pwdExpiry,
            AccountExpires = accExpiry,
            AccountExpiresStatus = accExpiry == null ? "Never expires" : $"Expires in {Math.Max(0, (int)((accExpiry.Value - DateTime.Now).TotalDays))} days",
            PasswordExpiryStatus = passwordNeverExpires ? "Never expires" : TimeHelper.GetPasswordExpiryStatus(pwdExpiry),
            BadPasswordCount = GetIntProperty(result, "badPwdCount"),
            BadPasswordTime = GetFileTimeProperty(result, "badPasswordTime")
        };
    }

    private string GetSidProperty(SearchResult result)
    {
        if (result.Properties.Contains("objectSid"))
        {
            var sidBytes = result.Properties["objectSid"][0] as byte[];
            if (sidBytes != null)
            {
                return new System.Security.Principal.SecurityIdentifier(sidBytes, 0).Value;
            }
        }
        return string.Empty;
    }

    private string GetStringProperty(SearchResult result, string propertyName)
    {
        if (result.Properties.Contains(propertyName) && result.Properties[propertyName].Count > 0)
        {
            return result.Properties[propertyName][0].ToString() ?? string.Empty;
        }
        return string.Empty;
    }

    private int GetIntProperty(SearchResult result, string propertyName)
    {
        if (result.Properties.Contains(propertyName) && result.Properties[propertyName].Count > 0)
        {
            if (int.TryParse(result.Properties[propertyName][0].ToString(), out int val))
                return val;
        }
        return 0;
    }

    private DateTime? GetFileTimeProperty(SearchResult result, string propertyName)
    {
        if (result.Properties.Contains(propertyName) && result.Properties[propertyName].Count > 0)
        {
            var value = result.Properties[propertyName][0];
            if (value is long longVal)
            {
                return TimeHelper.ConvertFileTime(longVal);
            }
            
            var highPart = value.GetType().InvokeMember("HighPart", System.Reflection.BindingFlags.GetProperty, null, value, null);
            var lowPart = value.GetType().InvokeMember("LowPart", System.Reflection.BindingFlags.GetProperty, null, value, null);
            
            if (highPart != null && lowPart != null)
            {
                long fileTime = ((long)(int)highPart << 32) + (uint)(int)lowPart;
                return TimeHelper.ConvertFileTime(fileTime);
            }
        }
        return null;
    }

    private PrincipalContext GetPrincipalContext()
    {
        if (!string.IsNullOrWhiteSpace(AdDomain))
        {
            return new PrincipalContext(ContextType.Domain, AdDomain);
        }
        return new PrincipalContext(ContextType.Domain);
    }

    public async Task<List<string>> SearchGroupsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        return await Task.Run(() =>
        {
            var results = new List<string>();
            var escapedQuery = LdapFilterHelper.Escape(query);

            string ldapPath = string.IsNullOrEmpty(AdDomain) ? "" : $"LDAP://{AdDomain}";
            using var entry = new DirectoryEntry(ldapPath);
            using var searcher = new DirectorySearcher(entry);

            searcher.Filter = $"(&(objectCategory=group)(|(sAMAccountName=*{escapedQuery}*)(name=*{escapedQuery}*)))";
            searcher.PropertiesToLoad.Add("sAMAccountName");
            searcher.SizeLimit = 15;

            var matches = searcher.FindAll();
            foreach (SearchResult match in matches)
            {
                if (match.Properties.Contains("sAMAccountName") && match.Properties["sAMAccountName"].Count > 0)
                {
                    results.Add(match.Properties["sAMAccountName"][0]?.ToString() ?? "");
                }
            }

            return results.OrderBy(g => g).ToList();
        });
    }

    public async Task UnlockAccountAsync(string samAccountName)
    {
        await Task.Run(() =>
        {
            using var context = GetPrincipalContext();
            using var user = UserPrincipal.FindByIdentity(context, samAccountName);
            if (user != null)
            {
                user.UnlockAccount();
                user.Save();
            }
            else throw new Exception("User not found.");
        });
    }

    public async Task EnableAccountAsync(string samAccountName, bool enable)
    {
        await Task.Run(() =>
        {
            using var context = GetPrincipalContext();
            using var user = UserPrincipal.FindByIdentity(context, samAccountName);
            if (user != null)
            {
                user.Enabled = enable;
                user.Save();
            }
            else throw new Exception("User not found.");
        });
    }

    public async Task ResetPasswordAsync(string samAccountName, string newPassword, bool requireChangeAtNextLogon)
    {
        await Task.Run(() =>
        {
            using var context = GetPrincipalContext();
            using var user = UserPrincipal.FindByIdentity(context, samAccountName);
            if (user != null)
            {
                user.SetPassword(newPassword);
                if (requireChangeAtNextLogon)
                {
                    user.ExpirePasswordNow();
                }
                user.Save();
            }
            else throw new Exception("User not found.");
        });
    }

    public async Task ForcePasswordChangeAsync(string samAccountName)
    {
        await Task.Run(() =>
        {
            using var context = GetPrincipalContext();
            using var user = UserPrincipal.FindByIdentity(context, samAccountName);
            if (user != null)
            {
                user.ExpirePasswordNow();
                user.Save();
            }
            else throw new Exception("User not found.");
        });
    }

    public async Task AddUserToGroupAsync(string samAccountName, string groupName)
    {
        await Task.Run(() =>
        {
            using var context = GetPrincipalContext();
            using var user = UserPrincipal.FindByIdentity(context, samAccountName);
            using var group = GroupPrincipal.FindByIdentity(context, groupName);
            if (user != null && group != null)
            {
                if (!group.Members.Contains(user))
                {
                    group.Members.Add(user);
                    group.Save();
                }
            }
            else throw new Exception("User or Group not found.");
        });
    }

    public async Task RemoveUserFromGroupAsync(string samAccountName, string groupName)
    {
        await Task.Run(() =>
        {
            using var context = GetPrincipalContext();
            using var user = UserPrincipal.FindByIdentity(context, samAccountName);
            using var group = GroupPrincipal.FindByIdentity(context, groupName);
            if (user != null && group != null)
            {
                if (group.Members.Contains(user))
                {
                    group.Members.Remove(user);
                    group.Save();
                }
            }
            else throw new Exception("User or Group not found.");
        });
    }

    public async Task UpdateUserProfileAsync(string samAccountName, Dictionary<string, string> updates, string? newManager)
    {
        await Task.Run(() =>
        {
            string ldapPath = "";
            if (!string.IsNullOrWhiteSpace(AdDomain))
            {
                ldapPath = $"LDAP://{AdDomain}";
            }
            else
            {
                using var rootDse = new DirectoryEntry("LDAP://RootDSE");
                ldapPath = $"LDAP://{rootDse.Properties["defaultNamingContext"].Value}";
            }

            using var searchRoot = new DirectoryEntry(ldapPath);
            using var searcher = new DirectorySearcher(searchRoot)
            {
                Filter = $"(&(objectCategory=person)(objectClass=user)(sAMAccountName={samAccountName}))"
            };
            
            var result = searcher.FindOne();
            if (result == null) throw new Exception("User not found.");

            using var entry = result.GetDirectoryEntry();

            foreach (var kvp in updates)
            {
                if (string.IsNullOrWhiteSpace(kvp.Value))
                {
                    if (entry.Properties.Contains(kvp.Key))
                        entry.Properties[kvp.Key].Clear();
                }
                else
                {
                    entry.Properties[kvp.Key].Value = kvp.Value;
                }
            }

            if (!string.IsNullOrWhiteSpace(newManager))
            {
                string targetSam = newManager;
                if (newManager.Contains('(') && newManager.Contains(')'))
                {
                    int start = newManager.LastIndexOf('(') + 1;
                    int end = newManager.LastIndexOf(')');
                    targetSam = newManager.Substring(start, end - start);
                }

                using var mgrSearcher = new DirectorySearcher(searchRoot)
                {
                    Filter = $"(&(objectCategory=person)(objectClass=user)(sAMAccountName={targetSam}))"
                };
                mgrSearcher.PropertiesToLoad.Add("distinguishedName");
                var mgrResult = mgrSearcher.FindOne();

                if (mgrResult != null)
                {
                    entry.Properties["manager"].Value = mgrResult.Properties["distinguishedName"][0];
                }
                else
                {
                    throw new Exception($"Manager '{newManager}' not found in Active Directory.");
                }
            }
            else
            {
                if (entry.Properties.Contains("manager"))
                    entry.Properties["manager"].Clear();
            }

            entry.CommitChanges();
        });
    }

    public async Task<List<KeyValuePair<string, string>>> GetAllUserAttributesAsync(string samAccountName)
    {
        return await Task.Run(() =>
        {
            var results = new List<KeyValuePair<string, string>>();
            using var ctx = GetPrincipalContext();
            using var user = UserPrincipal.FindByIdentity(ctx, samAccountName);
            if (user == null) throw new Exception("User not found.");

            var entry = (DirectoryEntry)user.GetUnderlyingObject();
            entry.RefreshCache();

            foreach (string propertyName in entry.Properties.PropertyNames)
            {
                var valCollection = entry.Properties[propertyName];
                if (valCollection != null && valCollection.Count > 0)
                {
                    var values = new List<string>();
                    foreach (var val in valCollection)
                    {
                        if (val is byte[] bytes)
                        {
                            values.Add(BitConverter.ToString(bytes).Replace("-", " "));
                        }
                        else if (val != null)
                        {
                            values.Add(val.ToString() ?? "");
                        }
                    }
                    results.Add(new KeyValuePair<string, string>(propertyName, string.Join(", ", values)));
                }
            }

            return results.OrderBy(k => k.Key).ToList();
        });
    }

    public static readonly HashSet<string> SafeEditableAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "title", "department", "physicalDeliveryOfficeName", "telephoneNumber", 
        "mobile", "streetAddress", "l", "st", "postalCode", "description", "wWWHomePage"
    };

    public static bool IsAttributeEditable(string attributeName) => SafeEditableAttributes.Contains(attributeName);

    public async Task UpdateRawAttributeAsync(string samAccountName, string attributeName, string newValue)
    {
        if (!IsAttributeEditable(attributeName))
        {
            throw new InvalidOperationException($"Attribute '{attributeName}' is not permitted for modification via the safe attribute editor.");
        }

        string oldValue = string.Empty;
        try
        {
            await Task.Run(() =>
            {
                using var ctx = GetPrincipalContext();
                using var user = UserPrincipal.FindByIdentity(ctx, samAccountName);
                if (user == null) throw new Exception("User not found.");

                var entry = (DirectoryEntry)user.GetUnderlyingObject();

                if (entry.Properties.Contains(attributeName) && entry.Properties[attributeName].Value != null)
                {
                    oldValue = entry.Properties[attributeName].Value?.ToString() ?? string.Empty;
                }

                if (string.IsNullOrWhiteSpace(newValue))
                {
                    if (entry.Properties.Contains(attributeName))
                        entry.Properties[attributeName].Clear();
                }
                else
                {
                    entry.Properties[attributeName].Value = newValue;
                }

                entry.CommitChanges();
            });

            await AdAuditLogger.LogAttributeChangeAsync(samAccountName, attributeName, oldValue, newValue, success: true);
        }
        catch (Exception ex)
        {
            await AdAuditLogger.LogAttributeChangeAsync(samAccountName, attributeName, oldValue, newValue, success: false, errorMessage: ex.Message);
            throw;
        }
    }

    // ==========================================
    // COMPUTER OPERATIONS
    // ==========================================

    public async Task<List<AdComputer>> SearchComputersAsync(string query)
    {
        return await Task.Run(() =>
        {
            var results = new List<AdComputer>();
            if (string.IsNullOrWhiteSpace(query)) return results;

            var escapedQuery = LdapFilterHelper.Escape(query.Trim());
            string rootPath = string.IsNullOrWhiteSpace(AdDomain) ? "" : $"LDAP://{AdDomain}";
            using var rootEntry = string.IsNullOrEmpty(rootPath) ? new DirectoryEntry() : new DirectoryEntry(rootPath);
            using var searcher = new DirectorySearcher(rootEntry);

            searcher.Filter = $"(&(objectCategory=computer)(|(name={escapedQuery}*)(sAMAccountName={escapedQuery}*)(dNSHostName={escapedQuery}*)))";
            SetComputerPropertiesToLoad(searcher);
            searcher.SizeLimit = 25;

            var matches = searcher.FindAll();
            if (matches.Count == 0 && escapedQuery.Length >= 2)
            {
                searcher.Filter = $"(&(objectCategory=computer)(|(name=*{escapedQuery}*)(dNSHostName=*{escapedQuery}*)(description=*{escapedQuery}*)))";
                matches = searcher.FindAll();
            }

            foreach (SearchResult match in matches)
            {
                results.Add(MapToComputerModel(match));
            }

            return results;
        });
    }

    private void SetComputerPropertiesToLoad(DirectorySearcher searcher)
    {
        searcher.PropertiesToLoad.Clear();
        searcher.PropertiesToLoad.AddRange(new[]
        {
            "name", "sAMAccountName", "dNSHostName", "operatingSystem", "operatingSystemVersion",
            "distinguishedName", "description", "objectSid", "managedBy", "location",
            "userAccountControl", "lastLogon", "lastLogonTimestamp", "pwdLastSet", "whenCreated", "memberOf"
        });
    }

    private AdComputer MapToComputerModel(SearchResult result)
    {
        string name = GetStringProperty(result, "name");
        string sam = GetStringProperty(result, "sAMAccountName");
        string dns = GetStringProperty(result, "dNSHostName");
        string os = GetStringProperty(result, "operatingSystem");
        string osVer = GetStringProperty(result, "operatingSystemVersion");
        string dn = GetStringProperty(result, "distinguishedName");
        string desc = GetStringProperty(result, "description");
        string managedBy = GetStringProperty(result, "managedBy");
        string location = GetStringProperty(result, "location");

        // Parse Groups
        var groupsList = new List<string>();
        if (result.Properties.Contains("memberOf"))
        {
            foreach (var groupDn in result.Properties["memberOf"])
            {
                groupsList.Add(TimeHelper.ParseManagerName(groupDn.ToString() ?? ""));
            }
        }

        // Account Status
        bool isEnabled = true;
        string accountStatus = "Enabled";
        if (result.Properties.Contains("userAccountControl") && result.Properties["userAccountControl"].Count > 0)
        {
            int uac = (int)result.Properties["userAccountControl"][0];
            bool isDisabled = (uac & 0x2) != 0;
            isEnabled = !isDisabled;
            accountStatus = isDisabled ? "Disabled" : "Enabled";
        }

        // BitLocker Recovery Keys discovery under this computer object
        var bitLockerKeys = new List<BitLockerKeyInfo>();
        try
        {
            if (!string.IsNullOrWhiteSpace(dn))
            {
                using var compEntry = new DirectoryEntry($"LDAP://{dn}");
                using var fveSearcher = new DirectorySearcher(compEntry);
                fveSearcher.Filter = "(objectClass=msFVE-RecoveryInformation)";
                fveSearcher.SearchScope = SearchScope.OneLevel;
                fveSearcher.PropertiesToLoad.Add("msFVE-RecoveryPassword");
                fveSearcher.PropertiesToLoad.Add("whenCreated");
                fveSearcher.PropertiesToLoad.Add("name");

                var fveResults = fveSearcher.FindAll();
                foreach (SearchResult fve in fveResults)
                {
                    string rPwd = fve.Properties.Contains("msFVE-RecoveryPassword") ? (string)fve.Properties["msFVE-RecoveryPassword"][0] : "";
                    string idName = fve.Properties.Contains("name") ? (string)fve.Properties["name"][0] : "";
                    DateTime created = fve.Properties.Contains("whenCreated") ? (DateTime)fve.Properties["whenCreated"][0] : DateTime.MinValue;

                    bitLockerKeys.Add(new BitLockerKeyInfo
                    {
                        KeyId = idName,
                        RecoveryPassword = rPwd,
                        Created = created
                    });
                }
            }
        }
        catch
        {
            // BitLocker retrieval failure (e.g. permission restriction) handled gracefully
        }

        return new AdComputer
        {
            Name = name,
            SamAccountName = sam,
            DnsHostName = dns,
            OperatingSystem = os,
            OperatingSystemVersion = osVer,
            OuPath = dn,
            Description = desc,
            Sid = GetSidProperty(result),
            ManagedBy = TimeHelper.ParseManagerName(managedBy),
            Location = location,
            AccountStatus = accountStatus,
            IsEnabled = isEnabled,
            LastLogon = GetFileTimeProperty(result, "lastLogon"),
            LastLogonTimestamp = GetFileTimeProperty(result, "lastLogonTimestamp"),
            PasswordLastSet = GetFileTimeProperty(result, "pwdLastSet"),
            Created = result.Properties.Contains("whenCreated") ? (DateTime)result.Properties["whenCreated"][0] : null,
            BitLockerKeys = bitLockerKeys.OrderByDescending(k => k.Created).ToList(),
            Groups = groupsList
        };
    }

    public async Task EnableComputerAccountAsync(string samAccountName, bool enable)
    {
        await Task.Run(() =>
        {
            using var context = GetPrincipalContext();
            using var computer = ComputerPrincipal.FindByIdentity(context, IdentityType.SamAccountName, samAccountName);
            if (computer == null)
                throw new InvalidOperationException($"Computer account '{samAccountName}' not found.");

            computer.Enabled = enable;
            computer.Save();
        });
    }

    public async Task AddComputerToGroupAsync(string samAccountName, string groupName)
    {
        await Task.Run(() =>
        {
            using var context = GetPrincipalContext();
            using var group = GroupPrincipal.FindByIdentity(context, groupName);
            if (group == null)
                throw new InvalidOperationException($"Group '{groupName}' not found.");

            using var computer = ComputerPrincipal.FindByIdentity(context, IdentityType.SamAccountName, samAccountName);
            if (computer == null)
                throw new InvalidOperationException($"Computer account '{samAccountName}' not found.");

            group.Members.Add(computer);
            group.Save();
        });
    }

    public async Task RemoveComputerFromGroupAsync(string samAccountName, string groupName)
    {
        await Task.Run(() =>
        {
            using var context = GetPrincipalContext();
            using var group = GroupPrincipal.FindByIdentity(context, groupName);
            if (group == null)
                throw new InvalidOperationException($"Group '{groupName}' not found.");

            using var computer = ComputerPrincipal.FindByIdentity(context, IdentityType.SamAccountName, samAccountName);
            if (computer == null)
                throw new InvalidOperationException($"Computer account '{samAccountName}' not found.");

            group.Members.Remove(computer);
            group.Save();
        });
    }
}
