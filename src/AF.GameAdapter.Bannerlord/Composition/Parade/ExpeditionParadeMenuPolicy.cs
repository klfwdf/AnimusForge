using System.Collections.Generic;

namespace AnimusForge;

internal static class ExpeditionParadeMenuPolicy
{
    internal const string MenuId = "af_parade_menu";
    internal const string EntryId = "af_parade_entry";

    // Resolve once after all regular menu registrations; never depend on translated labels.
    internal static int InsertionIndex(IReadOnlyList<string> optionIds, int firstLeaveIndex)
    {
        for (int i = 0; i < optionIds.Count; i++)
            if (optionIds[i] == "rex_stage_public_execution" || optionIds[i] == "rex_stage_public_execution_custom") return i;
        return firstLeaveIndex >= 0 && firstLeaveIndex <= optionIds.Count ? firstLeaveIndex : optionIds.Count;
    }
}
