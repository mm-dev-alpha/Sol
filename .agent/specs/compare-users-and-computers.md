# Specification: Smart User & Computer Comparison Workspace

**Feature Slug:** `compare-users-and-computers`  
**Target:** Sol (WinUI 3 / Windows App SDK Active Directory Admin Suite)  
**Status:** Approved Specification  

---

## 1. Overview & Purpose

System administrators and IT helpdesk engineers frequently troubleshoot access and configuration issues by comparing two objects:
- *"Why can User A access this file share or service while User B cannot?"*
- *"Why does Computer A receive a specific certificate/GPO while Computer B fails?"*

This feature introduces a dedicated, high-performance **Object Comparison Workspace** (`CompareWorkspacePage`) in Sol that performs deterministic, side-by-side Active Directory diff analysis for peer objects (**User vs. User** and **Computer vs. Computer**).

Key capabilities include:
1. **Smart Discrepancy Insights**: Contextual intelligence flagging root causes (OU path divergence affecting GPO inheritance, account lockouts/disabled flags, BitLocker recovery key absence, OS build mismatch).
2. **3-Way Group Venn Analysis**: Segmented set operations computing `Only in Target A`, `Shared / In Both`, and `Only in Target B`, with search filtering.
3. **Attribute Comparison Matrix**: Side-by-side categorized inspection of identity, organization, security, and hardware metadata with an optional **"Show differences only"** view filter.
4. **Seamless Entry & Quick Launch**: Direct navigation from existing `UserWorkspacePage` and `ComputerWorkspacePage` via a "Compare with..." action button that pre-populates Side A.
5. **Zero-Exception Completeness Export**: Structured Markdown / text export copying 100% of compared properties and diff results to the clipboard.

---

## 2. Architecture & Components

```
+-----------------------------------------------------------------------------------+
| MainWindow (RootNavigationView)                                                   |
| - NavItem: "Compare" (Tag: CompareWorkspacePage, Glyph: &#xE8AB;)                 |
+-----------------------------------------------------------------------------------+
       |                                      ^
       | navigates                            | WeakReferenceMessenger:
       v                                      | InitiateComparisonMessage(Mode, Entity)
+------------------------------------+        |
| CompareWorkspacePage               |        |
| - SelectorBar (Users / Computers)  |        +-----------------------------------+
| - Dual AutoSuggestBox (Side A / B) |        | UserWorkspace / ComputerWorkspace |
| - Smart Insights Panel             |        | - Toolbar: "Compare with..." Btn  |
| - Group Venn Matrix (3 columns)    |        +-----------------------------------+
| - Side-by-Side Property Grid       |
+------------------------------------+
       |
       v binds to
+-----------------------------------------------------------------------------------+
| CompareWorkspaceViewModel                                                         |
| - SelectedMode: ComparisonMode (Users, Computers)                                 |
| - TargetA / TargetB: AdUser / AdComputer                                          |
| - UserResult / ComputerResult: UserComparisonResult / ComputerComparisonResult    |
| - SwapTargetsCommand, ClearComparisonCommand, CopyComparisonReportCommand         |
| - ShowDifferencesOnly (bool), GroupFilter (string), SearchQuery (string)          |
+-----------------------------------------------------------------------------------+
       |
       v calls
+-----------------------------------------------------------------------------------+
| IEntityComparisonService (Pure Deterministic Engine)                              |
| - CompareUsers(AdUser a, AdUser b) => UserComparisonResult                        |
| - CompareComputers(AdComputer a, AdComputer b) => ComputerComparisonResult        |
+-----------------------------------------------------------------------------------+
```

---

## 3. Data Contracts (`Sol.Models`)

### 3.1 Comparison Enums & Core Records

```csharp
namespace Sol.Models;

public enum ComparisonMode
{
    Users,
    Computers
}

public enum DiffState
{
    Identical,
    Different,
    OnlyInA,
    OnlyInB
}

public enum InsightSeverity
{
    Info,
    Warning,
    Caution
}

/// <summary>
/// A single compared property pair with difference detection.
/// </summary>
public record PropertyDiffItem(
    string Category,
    string PropertyName,
    string? ValueA,
    string? ValueB,
    bool IsDifferent
);

/// <summary>
/// Mathematical 3-way Venn partition of group memberships.
/// </summary>
public record GroupDiffSummary(
    List<string> OnlyInA,
    List<string> InBoth,
    List<string> OnlyInB
)
{
    public int UniqueToACount => OnlyInA.Count;
    public int SharedCount => InBoth.Count;
    public int UniqueToBCount => OnlyInB.Count;
    public int TotalGroupCount => OnlyInA.Count + InBoth.Count + OnlyInB.Count;
    public bool HasDifferences => UniqueToACount > 0 || UniqueToBCount > 0;
}

/// <summary>
/// Contextual intelligence highlighting potential causes of behavior/access divergence.
/// </summary>
public record SmartInsight(
    string Title,
    string Description,
    InsightSeverity Severity,
    string Glyph
);

/// <summary>
/// Complete evaluation payload for a User vs User comparison.
/// </summary>
public record UserComparisonResult(
    AdUser TargetA,
    AdUser TargetB,
    GroupDiffSummary Groups,
    List<PropertyDiffItem> Properties,
    List<SmartInsight> Insights,
    int DifferenceCount
);

/// <summary>
/// Complete evaluation payload for a Computer vs Computer comparison.
/// </summary>
public record ComputerComparisonResult(
    AdComputer TargetA,
    AdComputer TargetB,
    GroupDiffSummary Groups,
    List<PropertyDiffItem> Properties,
    List<SmartInsight> Insights,
    int DifferenceCount
);

/// <summary>
/// Inter-workspace message to initiate comparison from other views.
/// </summary>
public record InitiateComparisonMessage(
    ComparisonMode Mode,
    object TargetEntity
);
```

---

## 4. Service Engine (`Sol.Services.IEntityComparisonService`)

### 4.1 Interface Contract

```csharp
namespace Sol.Services;

public interface IEntityComparisonService
{
    UserComparisonResult CompareUsers(AdUser userA, AdUser userB);
    ComputerComparisonResult CompareComputers(AdComputer computerA, AdComputer computerB);
}
```

### 4.2 Discrepancy & Insights Rules
1. **OU Path Divergence (Severity: Warning, Glyph: `&#xE7BA;`)**:
   - Compares `TargetA.OuPath` against `TargetB.OuPath` (case-insensitive).
   - If divergent: Generates alert indicating different Organizational Units, signaling that divergent Group Policy Objects (GPOs), administrative delegation, and baseline scripts apply.
2. **Account Status & Flags Divergence (Severity: Caution, Glyph: `&#xE783;`)**:
   - User: `AccountStatus` (Enabled vs. Disabled), `IsLockedOut`, `PasswordNeverExpires`, `AccountExpiresStatus`.
   - Computer: `IsEnabled`, `AccountStatus`.
   - Generates immediate caution alert if one object is active while the other is disabled or locked.
3. **Group Membership Asymmetry (Severity: Info/Warning, Glyph: `&#xE716;`)**:
   - Case-insensitive set operations using `StringComparer.OrdinalIgnoreCase`.
   - `OnlyInA = A.Groups.Except(B.Groups, StringComparer.OrdinalIgnoreCase).OrderBy(g => g).ToList()`
   - `InBoth = A.Groups.Intersect(B.Groups, StringComparer.OrdinalIgnoreCase).OrderBy(g => g).ToList()`
   - `OnlyInB = B.Groups.Except(A.Groups, StringComparer.OrdinalIgnoreCase).OrderBy(g => g).ToList()`
   - If `HasDifferences` is true: Generates summary insight of asymmetrical memberships.
4. **Computer System & Security Discrepancies (Computers Only)**:
   - **OS Mismatch (Severity: Info, Glyph: `&#xE7F8;`)**: Divergence in `OperatingSystem` or `OperatingSystemVersion`.
   - **BitLocker Protection Discrepancy (Severity: Caution, Glyph: `&#xE72E;`)**: Target A has recovery keys backed up in AD while Target B has none, or key counts differ.
5. **Logon & Password Recency**:
   - High bad password attempt counts (`BadPasswordCount >= 3`).
   - Expired password status disparity.

---

## 5. UI/UX & Design System Compliance

### 5.1 Navigation Integration
- Add `NavigationViewItem` to `MainWindow.xaml`:
  ```xml
  <NavigationViewItem Content="{x:Bind S.NavCompareWorkspace}" Tag="CompareWorkspacePage">
      <NavigationViewItem.Icon>
          <FontIcon Glyph="&#xE8AB;" />
      </NavigationViewItem.Icon>
  </NavigationViewItem>
  ```
- Register navigation target in `NavigationService.cs`.

### 5.2 Header Toolbar in User & Computer Workspaces
- Add "Compare with..." button in `UserWorkspacePage.xaml` and `ComputerWorkspacePage.xaml` header action bars:
  ```xml
  <Button Style="{ThemeResource SubtleIconButtonStyle}"
          Command="{x:Bind ViewModel.OpenCompareWithCommand}"
          ToolTipService.ToolTip="{x:Bind S.CompareWithAction}">
      <StackPanel Orientation="Horizontal" Spacing="6">
          <FontIcon Glyph="&#xE8AB;" FontSize="14" />
          <TextBlock Text="{x:Bind S.CompareWithAction}" Style="{ThemeResource CaptionTextBlockStyle}" />
      </StackPanel>
  </Button>
  ```

### 5.3 Compare Workspace Page Layout
- **Mode Bar**: `SelectorBar` toggling `Users` and `Computers`.
- **Target Selection Deck**:
  - Side A card with `AutoSuggestBox` (debounced search via `IActiveDirectoryService`), selection chip with Avatar, DisplayName, SAM name, and OU.
  - Center swap button (`&#xE8AB;`) and difference count pill.
  - Side B card with identical auto-suggest and selection chip.
- **Smart Insights Deck**: Card or `InfoBar` list rendering detected `SmartInsight` items.
- **Group Venn Discrepancy Section**:
  - Filter segmented bar: `All (total)`, `Differences Only (diffs)`, `Target A Only (count)`, `Shared (count)`, `Target B Only (count)`.
  - Instant text filter box.
  - Visual 3-column container displaying group items tagged with color-coded pills.
- **Attribute Diff Matrix**:
  - Categorized SettingsCards (*Identity*, *Organization*, *Account Status & Logon*, *Security & Flags*).
  - Highlighting rows where `IsDifferent == true` with `SystemFillColorCautionBackgroundBrush`.
  - ToggleSwitch: "Show differences only".
- **Export Action**:
  - "Copy Report" button generating structured Markdown report covering 100% of diff findings.

---

## 6. Zero Hardcoded Strings (`Strings.cs`)

All user-facing strings added to `Sol.Helpers.Strings.S`:
- `NavCompareWorkspace` = `"Compare"`
- `CompareTitle` = `"Object Comparison"`
- `CompareSubtitle` = `"Analyze permissions, attributes, and group policy discrepancies side-by-side."`
- `CompareModeUsers` = `"Users"`
- `CompareModeComputers` = `"Computers"`
- `CompareSearchPlaceholderUserA` = `"Search first user account..."`
- `CompareSearchPlaceholderUserB` = `"Search second user account..."`
- `CompareSearchPlaceholderComputerA` = `"Search first computer..."`
- `CompareSearchPlaceholderComputerB` = `"Search second computer..."`
- `CompareSwapTooltip` = `"Swap Side A and Side B"`
- `CompareClearBtn` = `"Clear"`
- `CompareCopyReport` = `"Copy Report"`
- `CompareReportCopied` = `"Comparison report copied to clipboard."`
- `CompareShowDiffsOnly` = `"Show differences only"`
- `CompareDiffCountBadge` = `"{0} Differences"`
- `CompareIdenticalBadge` = `"Identical"`
- `CompareGroupsTitle` = `"Group Membership Comparison"`
- `CompareGroupFilterAll` = `"All ({0})"`
- `CompareGroupFilterDiffs` = `"Differences ({0})"`
- `CompareGroupOnlyA` = `"{0} Only ({1})"`
- `CompareGroupShared` = `"Shared ({0})"`
- `CompareGroupOnlyB` = `"{0} Only ({1})"`
- `CompareGroupSearchPlaceholder` = `"Filter groups..."`
- `CompareOuDivergenceTitle` = `"Organizational Unit Divergence"`
- `CompareOuDivergenceDesc` = `"Objects belong to different OUs. Different Group Policies (GPOs) and access baselines apply."`
- `CompareAccountStatusDivergenceTitle` = `"Account Status Discrepancy"`
- `CompareBitLockerDivergenceTitle` = `"BitLocker Protection Discrepancy"`
- `CompareBitLockerDivergenceDesc` = `"BitLocker recovery key backup status differs between computer accounts in Active Directory."`
- `CompareOsDivergenceTitle` = `"Operating System Disparity"`
- `CompareWithAction` = `"Compare with..."`
- `CompareEmptyStateTitle` = `"Select Two Objects to Compare"`
- `CompareEmptyStateSubtitle` = `"Search and select two users or computers above to inspect discrepancies."`

---

## 7. Verification & Quality Plan

### 7.1 Automated xUnit Tests (`src/Sol.Tests/EntityComparisonServiceTests.cs`)
1. `CompareUsers_IdenticalUsers_ZeroDifferencesAndEmptyDiffSets`: Verifies identical accounts return 0 differences and `HasDifferences == false`.
2. `CompareUsers_DivergentOu_ProducesOuInsightWarning`: Verifies OU differences trigger the OU divergence insight.
3. `CompareUsers_GroupMembershipVenn_CorrectPartition`: Verifies `OnlyInA`, `InBoth`, and `OnlyInB` are strictly partitioned with case-insensitive equality.
4. `CompareUsers_AccountStatusMismatch_FlagsCaution`: Verifies enabled vs disabled / locked accounts produce caution insights.
5. `CompareComputers_BitLockerAndOsDiff_FlagsCorrectDiscrepancies`: Verifies BitLocker key and OS version discrepancies.
6. `CompareWorkspaceViewModel_SwapTargets_InvertsTargetsAndRecalculates`: Verifies swap logic.
7. `CompareWorkspaceViewModel_CopyReport_ExportsCompleteData`: Verifies clipboard string contains all categories, groups, and insights.

### 7.2 UI & Manual Verification
- Launch application, navigate to **Compare**.
- Toggle between Users and Computers modes.
- Search and pick two users -> verify diff cards, Venn diagram, and insights render cleanly.
- Click "Show differences only" -> verify identical rows collapse.
- Click "Copy Report" -> paste into Notepad/Markdown reader and verify 100% data fidelity.
- From `UserWorkspacePage`, click "Compare with..." on a loaded user -> verify navigation switches to Compare with User A pre-filled.
