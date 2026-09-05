using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Sol.Tests;

public class XamlResourceAuditTests
{
    [Fact]
    public void AllXamlFiles_DoNotUseStaticResource_ForAccentButtonStyle()
    {
        // AccentButtonStyle is defined within ThemeDictionaries in WinUI 3 and fails at runtime with XamlParseException if referenced via StaticResource.
        string projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Sol"));
        if (!Directory.Exists(projectDir))
        {
            // Fallback if directory structure differs in test runner
            projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Sol"));
        }

        Assert.True(Directory.Exists(projectDir), $"Sol project directory not found at: {projectDir}");

        var xamlFiles = Directory.GetFiles(projectDir, "*.xaml", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) &&
                        !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .ToList();

        Assert.NotEmpty(xamlFiles);

        var violatingFiles = new System.Collections.Generic.List<string>();

        foreach (var file in xamlFiles)
        {
            string content = File.ReadAllText(file);
            if (content.Contains("{StaticResource AccentButtonStyle}", StringComparison.OrdinalIgnoreCase))
            {
                violatingFiles.Add(Path.GetFileName(file));
            }
        }

        Assert.True(violatingFiles.Count == 0,
            $"The following XAML files incorrectly reference AccentButtonStyle as a StaticResource instead of ThemeResource: {string.Join(", ", violatingFiles)}");
    }
}
