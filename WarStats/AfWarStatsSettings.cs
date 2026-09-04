namespace AFWarStatsTerminal.Settings;

public static class AfWarStatsSettings
{
    private static int _currentSortMode = 0;
    private static int _historySortMode = 0;
    private static int _currentWarsDisplayMode = 1; // 默认单页滚动 (1 = scroll all wars, 0 = paged)
    private static int _historyWarsDisplayMode = 1; // 默认单页滚动 (1 = scroll all wars, 0 = paged)

    internal static int GetCurrentSortMode() => _currentSortMode;
    internal static void SetCurrentSortMode(int selectedIndex) => _currentSortMode = selectedIndex;

    internal static int GetCurrentWarsDisplayMode() => _currentWarsDisplayMode;
    internal static void SetCurrentWarsDisplayMode(int selectedIndex) => _currentWarsDisplayMode = selectedIndex;

    internal static int GetHistorySortMode() => _historySortMode;
    internal static void SetHistorySortMode(int selectedIndex) => _historySortMode = selectedIndex;

    internal static int GetHistoryWarsDisplayMode() => _historyWarsDisplayMode;
    internal static void SetHistoryWarsDisplayMode(int selectedIndex) => _historyWarsDisplayMode = selectedIndex;
}
