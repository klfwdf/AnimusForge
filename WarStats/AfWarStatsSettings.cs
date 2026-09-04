using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using MCM.Common;
using TaleWorlds.Localization;

namespace AFWarStatsTerminal.Settings;

public sealed class AfWarStatsSettings : AttributeGlobalSettings<AfWarStatsSettings>
{
    private const string CurrentGroup = "{=AFWST_MCMCurrentGroup}Current Wars";

    private const string HistoryGroup = "{=AFWST_MCMHistoryGroup}War Archive";

    private static readonly string[] SortChoices =
    {
        "{=AFWST_SortLatest}Latest Wars First",
        "{=AFWST_SortEarliest}Earliest Wars First",
        "{=AFWST_SortDuration}Longest Duration",
        "{=AFWST_SortCasualties}Most Casualties",
        "{=AFWST_SortWeariness}Highest War Weariness",
        "{=AFWST_SortPlayer}Player-Related First"
    };

    private static readonly string[] CurrentDisplayModeChoices =
    {
        "{=AFWST_DisplayPaged}Multiple Pages (6 per page)",
        "{=AFWST_DisplayScrollable}Single Page (scroll all wars)"
    };

    private static readonly string[] HistoryDisplayModeChoices =
    {
        "{=AFWST_DisplayPagedHistory}Multiple Pages (8 per page)",
        "{=AFWST_DisplayScrollable}Single Page (scroll all wars)"
    };

    private static readonly AfWarStatsSettings Fallback = new();

    internal static AfWarStatsSettings Current =>
        GlobalSettings<AfWarStatsSettings>.Instance ?? Fallback;

    public override string Id => "AFWarStatsTerminal";

    public override string DisplayName =>
        new TextObject("{=AFWST_SettingsName}War Statistics").ToString();

    public override string FolderName => "AFWarStatsTerminal";

    public override string FormatType => "json2";

    [SettingPropertyDropdown(
        "{=AFWST_MCMCurrentSortMode}Current War Sort",
        Order = 0,
        RequireRestart = false,
        HintText = "{=AFWST_MCMCurrentSortModeHint}Choose the order used by the Current Wars page.")]
    [SettingPropertyGroup(CurrentGroup, GroupOrder = 0)]
    public Dropdown<string> CurrentSortMode { get; set; } = CreateSortDropdown();

    [SettingPropertyDropdown(
        "{=AFWST_MCMCurrentDisplayMode}Current War List Display",
        Order = 1,
        RequireRestart = false,
        HintText = "{=AFWST_MCMCurrentDisplayModeHint}Choose between six-row pages and one scrollable list.")]
    [SettingPropertyGroup(CurrentGroup, GroupOrder = 0)]
    public Dropdown<string> CurrentWarsDisplayMode { get; set; } = CreateCurrentDisplayModeDropdown();

    [SettingPropertyDropdown(
        "{=AFWST_MCMHistorySortMode}Historical War Sort",
        Order = 0,
        RequireRestart = false,
        HintText = "{=AFWST_MCMHistorySortModeHint}Choose the order used by the Historical Wars page.")]
    [SettingPropertyGroup(HistoryGroup, GroupOrder = 0)]
    public Dropdown<string> HistorySortMode { get; set; } = CreateHistorySortDropdown();

    [SettingPropertyDropdown(
        "{=AFWST_MCMHistoryDisplayMode}Historical War List Display",
        Order = 1,
        RequireRestart = false,
        HintText = "{=AFWST_MCMHistoryDisplayModeHint}Choose between eight-row pages and one scrollable historical-war list.")]
    [SettingPropertyGroup(HistoryGroup, GroupOrder = 0)]
    public Dropdown<string> HistoryWarsDisplayMode { get; set; } = CreateHistoryDisplayModeDropdown();

    internal static int GetCurrentSortMode()
    {
        int selectedIndex = Current.CurrentSortMode?.SelectedIndex ?? 0;
        return NormalizeSortMode(selectedIndex);
    }

    internal static void SetCurrentSortMode(int selectedIndex)
    {
        Dropdown<string> dropdown = Current.CurrentSortMode;
        if (dropdown != null)
        {
            dropdown.SelectedIndex = NormalizeSortMode(selectedIndex);
        }
    }

    internal static int GetCurrentWarsDisplayMode()
    {
        int selectedIndex = Current.CurrentWarsDisplayMode?.SelectedIndex ?? 0;
        return NormalizeDisplayMode(selectedIndex);
    }

    internal static void SetCurrentWarsDisplayMode(int selectedIndex)
    {
        Dropdown<string> dropdown = Current.CurrentWarsDisplayMode;
        if (dropdown != null)
        {
            dropdown.SelectedIndex = NormalizeDisplayMode(selectedIndex);
        }
    }

    internal static int GetHistorySortMode()
    {
        int selectedIndex = Current.HistorySortMode?.SelectedIndex ?? 0;
        return NormalizeSortMode(selectedIndex);
    }

    internal static void SetHistorySortMode(int selectedIndex)
    {
        Dropdown<string> dropdown = Current.HistorySortMode;
        if (dropdown != null)
        {
            dropdown.SelectedIndex = NormalizeSortMode(selectedIndex);
        }
    }

    internal static int GetHistoryWarsDisplayMode()
    {
        int selectedIndex = Current.HistoryWarsDisplayMode?.SelectedIndex ?? 0;
        return NormalizeDisplayMode(selectedIndex);
    }

    internal static void SetHistoryWarsDisplayMode(int selectedIndex)
    {
        Dropdown<string> dropdown = Current.HistoryWarsDisplayMode;
        if (dropdown != null)
        {
            dropdown.SelectedIndex = NormalizeDisplayMode(selectedIndex);
        }
    }

    private static Dropdown<string> CreateHistorySortDropdown()
    {
        return CreateSortDropdown();
    }

    private static Dropdown<string> CreateSortDropdown()
    {
        return new Dropdown<string>(SortChoices, 0);
    }

    private static Dropdown<string> CreateCurrentDisplayModeDropdown()
    {
        return new Dropdown<string>(CurrentDisplayModeChoices, 0);
    }

    private static Dropdown<string> CreateHistoryDisplayModeDropdown()
    {
        return new Dropdown<string>(HistoryDisplayModeChoices, 0);
    }

    private static int NormalizeSortMode(int selectedIndex)
    {
        return selectedIndex < 0 || selectedIndex >= SortChoices.Length ? 0 : selectedIndex;
    }

    private static int NormalizeDisplayMode(int selectedIndex)
    {
        return selectedIndex < 0 || selectedIndex >= CurrentDisplayModeChoices.Length ? 0 : selectedIndex;
    }
}
