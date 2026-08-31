# Testing Patterns

**Analysis Date:** 2026-08-31

## Test Framework

**Runner:**
- xUnit (Version 2.9.3)
- Visual Studio Test Runner: `xunit.runner.visualstudio` (Version 3.1.4)
- Test SDK: `Microsoft.NET.Test.Sdk` (Version 17.14.1)
- Code Coverage: `coverlet.collector` (Version 6.0.4)
- Config: `src/Sol.Tests/Sol.Tests.csproj`

**Assertion Library:**
- xUnit standard assertions (`Assert.Equal`, `Assert.True`, `Assert.False`, `Assert.NotNull`, `Assert.NotEmpty`, `Assert.Contains`)

**Run Commands:**
```powershell
# Run all unit tests
dotnet test src/Sol.Tests/Sol.Tests.csproj

# Run tests in Release configuration with normal verbosity
dotnet test src/Sol.Tests/Sol.Tests.csproj -c Release --verbosity normal

# Run a specific test class
dotnet test src/Sol.Tests/Sol.Tests.csproj --filter "FullyQualifiedName~AttributeEditorSafetyTests"

# Run tests with code coverage collection
dotnet test src/Sol.Tests/Sol.Tests.csproj --collect:"XPlat Code Coverage"
```

## Test File Organization

**Location:**
- Dedicated test project located at `src/Sol.Tests/` referencing `src/Sol/Sol.csproj`.

**Naming:**
- Test files end in `Tests.cs` (e.g., `AttributeEditorSafetyTests.cs`, `ComputerDiagnosticServiceTests.cs`, `ExportServiceTests.cs`).
- Test methods describe behavior and expectation clearly (e.g., `IsAttributeEditable_StrictlyEnforcesWhitelist`, `AuditLogger_WritesDurableStructuredLogEntry`, `ParseStorageOutput_ExtractsDrivesAndCalculatesPercentages`).

**Structure:**
```text
src/Sol.Tests/
├── AttributeEditorSafetyTests.cs        # Tests attribute editing whitelist, audit logger, and navigation registrations
├── ComputerDiagnosticServiceTests.cs    # Tests diagnostic data parsing, warranty URL generation, CLI fallbacks, and regexes
├── ExportServiceTests.cs                # Tests clipboard formatting and "Copy All" string builders
├── UnitTest1.cs                         # Smoke test and basic assertion harness
└── Sol.Tests.csproj                     # Test project MSBuild configuration
```

## Test Structure & Patterns

**Theory Tests with Inline Data:**
```csharp
[Theory]
[InlineData("title", true)]
[InlineData("department", true)]
[InlineData("objectSid", false)]
[InlineData("pwdLastSet", false)]
public void IsAttributeEditable_StrictlyEnforcesWhitelist(string attributeName, bool expectedAllowed)
{
    // Act
    bool isAllowed = ActiveDirectoryService.IsAttributeEditable(attributeName);

    // Assert
    Assert.Equal(expectedAllowed, isAllowed);
}
```

**Asynchronous Testing:**
```csharp
[Fact]
public async Task AuditLogger_WritesDurableStructuredLogEntry()
{
    // Arrange
    var targetSam = "test.user";
    var attribute = "department";
    var oldVal = "Old Dept";
    var newVal = "New Dept";

    // Act
    await AdAuditLogger.LogAttributeChangeAsync(targetSam, attribute, oldVal, newVal, success: true);

    // Assert
    var logFile = AdAuditLogger.LogFilePath;
    Assert.True(File.Exists(logFile), "Audit log file was not created.");
    var lines = await File.ReadAllLinesAsync(logFile);
    Assert.NotEmpty(lines);
}
```

**Test Isolation & Cleanup:**
```csharp
public class AttributeEditorSafetyTests : IDisposable
{
    private readonly string _testLogDir;

    public AttributeEditorSafetyTests()
    {
        _testLogDir = Path.Combine(Path.GetTempPath(), "Sol_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testLogDir);
        AdAuditLogger.SetCustomLogDirectoryForTesting(_testLogDir);
    }

    public void Dispose()
    {
        AdAuditLogger.SetCustomLogDirectoryForTesting(null);
        if (Directory.Exists(_testLogDir))
        {
            try { Directory.Delete(_testLogDir, true); } catch { }
        }
    }
}
```

## Mocking & Isolation Strategy

**Approach:**
- Diagnostic service unit tests test raw output parsers, regex extractors, status code translators, and warranty URL builders without requiring live WMI endpoints or domain controllers.
- Audit logging tests use isolated temporary directories via `SetCustomLogDirectoryForTesting` to avoid modifying production `%LocalAppData%\Sol\` logs.
- What to Mock/Isolate: External WMI outputs, LDAP responses, file system log paths.
- What NOT to Mock: Business validation rules, attribute whitelists, string resource lookups, data models, and serializer/deserializer contracts.

## Test Types & Coverage

**Unit Tests (150 tests passing):**
- **Security & Whitelist Validation**: Verifies that AD write operations cannot touch critical identity attributes (`objectSid`, `pwdLastSet`, `userAccountControl`, `sAMAccountName`).
- **Audit Log Integrity**: Verifies JSONL format, timestamps, user identifiers, and attribute change records.
- **Diagnostic Output Parsing**: Verifies parsing of WMI and CLI output strings for CPU, RAM, storage disks, uptime, battery status, processes, and services.
- **Warranty URL Resolution**: Validates manufacturer-specific warranty lookup URLs (Dell Service Tag, Lenovo Serial, HP Serial).
- **Export Formatting**: Verifies that "Copy All" key-value text generators produce correct headers, aligned fields, and complete sections without data loss.

**Integration Testing Requirements:**
- Live Active Directory operations (`IActiveDirectoryService`) and live remote WMI/RPC diagnostics require an accessible test domain or staging environment. In unit test runs, parsing and safety logic is isolated and verified independently.

---

*Testing analysis: 2026-08-31*
