using System;
using System.Reflection;
using Sol.Helpers;
using Xunit;

namespace Sol.Tests;

public class LocalizationAuditTests
{
    [Fact]
    public void Strings_AllProperties_ReturnNonEmptyStrings()
    {
        var strings = Strings.S;
        var properties = typeof(Strings).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.NotEmpty(properties);

        foreach (var prop in properties)
        {
            if (prop.PropertyType == typeof(string))
            {
                var value = prop.GetValue(strings) as string;
                Assert.False(string.IsNullOrWhiteSpace(value), $"Property '{prop.Name}' in Strings must not be empty or whitespace.");
                Assert.DoesNotContain("TODO", value, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("UNLOCALIZED", value, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
