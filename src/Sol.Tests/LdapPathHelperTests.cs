using Sol.Helpers;
using Xunit;

namespace Sol.Tests;

public class LdapPathHelperTests
{
    [Theory]
    [InlineData("CN=Max Mustermann,OU=IT,OU=Users,DC=company,DC=local", "OU=IT,OU=Users,DC=company,DC=local")]
    [InlineData("CN=Erika Musterfrau,OU=IT,OU=Users,DC=company,DC=local", "OU=IT,OU=Users,DC=company,DC=local")]
    [InlineData("CN=Mustermann\\, Max,OU=IT,OU=Users,DC=company,DC=local", "OU=IT,OU=Users,DC=company,DC=local")]
    [InlineData("CN=PC-01,CN=Computers,DC=company,DC=local", "CN=Computers,DC=company,DC=local")]
    [InlineData("CN=Administrator,CN=Users,DC=company,DC=local", "CN=Users,DC=company,DC=local")]
    [InlineData("OU=Workstations,OU=Clients,DC=company,DC=local", "OU=Workstations,OU=Clients,DC=company,DC=local")]
    [InlineData("OU=Users,DC=corp", "OU=Users,DC=corp")]
    [InlineData("DC=company,DC=local", "DC=company,DC=local")]
    [InlineData("CN=SpecialObject", "CN=SpecialObject")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void ExtractParentContainer_ReturnsExpectedParent(string? input, string expected)
    {
        string actual = LdapPathHelper.ExtractParentContainer(input);
        Assert.Equal(expected, actual);
    }
}
