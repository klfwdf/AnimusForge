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
			string normalizedMemoryId = NormalizeMemoryHeroId(memoryId);
			if (string.IsNullOrEmpty(normalizedMemoryId) || isNonHero != IsNonHeroMemoryId(normalizedMemoryId))
			{
				return new MemoryCommitResult(MemoryCommitStatus.Rejected, "memory_identity_invalid");
			}
			if (string.IsNullOrWhiteSpace(playerText) && string.IsNullOrWhiteSpace(aiText) && string.IsNullOrWhiteSpace(extraFact))
			{
				return new MemoryCommitResult(MemoryCommitStatus.Rejected, "memory_empty_commit");
			}
			Hero hero = isNonHero ? null : (Hero.Find(memoryId.Trim()) ?? FindHeroById(normalizedMemoryId));
			if (!isNonHero && !IsHeroNpcEligibleForCompressedMemory(hero))
			{
				return new MemoryCommitResult(MemoryCommitStatus.Rejected, "memory_target_ineligible");
			}
			bool accepted = isNonHero
				? owner.AppendDialogueHistoryById(normalizedMemoryId, npcName, playerText, aiText, extraFact, sceneSessionId)
				: owner.AppendDialogueHistory(hero, playerText, aiText, extraFact, sceneSessionId);
			return accepted
				? new MemoryCommitResult(MemoryCommitStatus.Applied)
				: new MemoryCommitResult(MemoryCommitStatus.Failed, "memory_owner_write_unconfirmed");
		}
		catch
		{
			return new MemoryCommitResult(MemoryCommitStatus.Failed, "memory_owner_append_failed");
		}
	}
}
