using System;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class MyBehavior
{
    // One strict runtime-acceptance owner for Native scene history and the legacy loose facade.
    // Applied is not SyncData/disk durability; failed acceptance may follow partial writes.
	internal static MemoryCommitResult CommitDialogueHistoryWithScene(string memoryId, bool isNonHero, string npcName, string playerText, string aiText, string extraFact, int sceneSessionId)
	{
		return CommitDialogueHistoryWithScene(memoryId, isNonHero, npcName, playerText, aiText, extraFact, sceneSessionId, -1, null);
	}

	internal static MemoryCommitResult CommitDialogueHistoryWithScene(string memoryId, bool isNonHero, string npcName, string playerText, string aiText, string extraFact, int sceneSessionId, int playerTargetAgentIndex, string playerTargetName)
	{
		try
		{
			if (!TWParallel.IsMainThread())
			{
				return new MemoryCommitResult(MemoryCommitStatus.Rejected, "memory_not_main_thread");
			}
			MyBehavior owner = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (owner == null)
			{
				return new MemoryCommitResult(MemoryCommitStatus.Failed, "memory_owner_missing");
			}
			return owner._memoryHistoryCommit.CommitDialogueHistoryWithScene(memoryId,isNonHero,npcName,playerText,aiText,extraFact,sceneSessionId,playerTargetAgentIndex,playerTargetName);
		}
		catch
		{
			return new MemoryCommitResult(MemoryCommitStatus.Failed, "memory_owner_append_failed");
		}
	}
}
