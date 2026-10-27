using System;

namespace AnimusForge;

// Compatibility recognition of the exact CN/EN public-document fact envelopes.
// This is a display-query boundary, never a persistence or model-memory filter.
internal static class PlayerConversationHistoryScope
{
    private static readonly string[] PublicDocumentPrefixes =
    {
        MemoryTextResources.Raw("AF_MEM_Compat_PublicDiplomacyIssuedCn"),
        MemoryTextResources.Raw("AF_MEM_Compat_PublicDiplomacyReceivedCn"),
        MemoryTextResources.Raw("AF_MEM_Compat_PublicDiplomacyResultCn"),
        MemoryTextResources.Raw("AF_MEM_Compat_PublicDiplomacyIssuedEn"),
        MemoryTextResources.Raw("AF_MEM_Compat_PublicDiplomacyReceivedEn"),
        MemoryTextResources.Raw("AF_MEM_Compat_PublicDiplomacyResultEn")
    };

    internal static bool IsNpcPublicDocumentFact(string line)
    {
        // Player/NPC utterances (including quotations of these envelopes) and
        // player-related diplomacy arrangements are deliberately retained.
        if (!DialogueHistoryLedger.TryReadAfefPrefix(line, out bool npc, out int length) || !npc)
            return false;
        string body = line.Substring(length).TrimStart();
        foreach (string prefix in PublicDocumentPrefixes)
            if (body.StartsWith(prefix, StringComparison.Ordinal)) return true;
        return false;
    }
}
