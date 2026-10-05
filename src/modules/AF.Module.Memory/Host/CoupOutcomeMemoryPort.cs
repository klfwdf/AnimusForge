using System;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge;

// Same-DLL team port, not a public sub-mod API. The memory owner alone constructs
// the recovery payload; consumers never depend on its constructor parameter count.
internal static class CoupOutcomeMemoryPort
{
    internal static InteractionMemoryCommit CreateCommit(string key, string heroId,
        string npcText, int gameDay, string settlementId)
    {
        // Preserve the existing coup payload/default date resolution and stable key.
        // A new required constructor argument now fails compilation here, not in-game.
        return new InteractionMemoryCommit(key, InteractionChannel.Domain, key, heroId,
            string.Empty, string.Empty, new[] { new FactRecord("coup_outcome", heroId, npcText) },
            0L, 0L, key, gameDay, 0, settlementId, -1, -1, string.Empty);
    }

    internal static bool Prepare(InteractionMemoryCommit commit, string npcName,
        out string recoveryId, out string payloadHash, out string errorCode)
        => MyBehavior.TryPrepareExternalDialogueHistoryRecoveryIdentity(commit, false, npcName,
            out recoveryId, out payloadHash, out errorCode);

    internal static MemoryCommitResult Commit(InteractionMemoryCommit commit, string npcName)
        => MyBehavior.CommitExternalDialogueHistoryRecoverable(commit, false, npcName);

    internal static string GetStatus(string recoveryId, string subjectId, string payloadHash)
        => MyBehavior.GetExternalDialogueHistoryRecoveryStatus(recoveryId, subjectId, payloadHash).ToString();
}
