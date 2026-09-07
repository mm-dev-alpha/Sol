using Sol.Helpers;
using Xunit;

namespace Sol.Tests;

public class TimeHelperTests
{
    [Theory]
    [InlineData("CN=Doe\\, John,OU=Users,DC=contoso,DC=com", "Doe, John")]
    [InlineData("CN=Smith\\, Jane A.,OU=Engineering,DC=corp,DC=local", "Smith, Jane A.")]
    [InlineData("CN=Standard User,OU=Staff,DC=domain,DC=com", "Standard User")]
    [InlineData("CN=SingleName", "SingleName")]
    [InlineData("CN=O\\'Connor\\, Pat,OU=Users,DC=domain,DC=com", "O'Connor, Pat")]
    [InlineData("OU=JustAnOU,DC=domain,DC=com", "OU=JustAnOU,DC=domain,DC=com")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void ParseManagerName_HandlesEscapedCommasAndStandardDns(string? dn, string expected)
    {
        var result = TimeHelper.ParseManagerName(dn!);
        Assert.Equal(expected, result);
    }
}
