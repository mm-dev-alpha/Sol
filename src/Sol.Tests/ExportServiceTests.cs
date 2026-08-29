using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Sol.Services;
using Xunit;

namespace Sol.Tests;

public class ExportServiceTests
{
    private record SampleRecord(string Name, int Count, string Status);

    [Fact]
    public async Task FormatAsCsvStringAsync_FormatsHeaderAndRowsWithQuotes()
    {
        // Arrange
        var service = new ExportService();
        var data = new List<SampleRecord>
        {
            new("Alice \"Admin\"", 42, "Active"),
            new("Bob, Manager", 7, "Disabled")
        };

        // Act
        var csv = await service.FormatAsCsvStringAsync(data);

        // Assert
        Assert.Contains("\"Name\",\"Count\",\"Status\"", csv);
        Assert.Contains("\"Alice \"\"Admin\"\"\",\"42\",\"Active\"", csv);
        Assert.Contains("\"Bob, Manager\",\"7\",\"Disabled\"", csv);
    }

    [Fact]
    public async Task FormatAsJsonStringAsync_FormatsIndentedCamelCase()
    {
        // Arrange
        var service = new ExportService();
        var data = new SampleRecord("Test User", 10, "Enabled");

        // Act
        var json = await service.FormatAsJsonStringAsync(data);

        // Assert
        Assert.Contains("\"name\": \"Test User\"", json);
        Assert.Contains("\"count\": 10", json);
        Assert.Contains("\"status\": \"Enabled\"", json);
    }
}
