using System.Collections.Generic;
using Entry = AnimusForge.RewardSystemBehavior.DebtExportEntry;
namespace AnimusForge;
// Merge an exported snapshot only; the existing ImportDebtEntries performs the domain commit.
internal static class DebtImportMergePolicy
{
    internal static Dictionary<string,Entry> ApplyImportedDebtEntries(Dictionary<string,Entry> existing,
        IEnumerable<KeyValuePair<string,Entry>> imported, bool overwriteExisting)
    {
        var result = existing ?? new Dictionary<string,Entry>();
        if (imported == null) return result;
        foreach (var item in imported)
        {
            if (string.IsNullOrEmpty(item.Key) || item.Value == null) continue;
            if (overwriteExisting)
            {
                result.Remove(item.Key);
                result[item.Key] = item.Value;
            }
            else if (!result.ContainsKey(item.Key)) result[item.Key] = item.Value;
        }
        return result;
    }
}
