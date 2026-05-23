using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CharacterDevelopment
{
	// Token: 0x020003AC RID: 940
	public static class TraitLevelingHelper
	{
		// Token: 0x06003655 RID: 13909 RVA: 0x000E1274 File Offset: 0x000DF474
		public static void UpdateTraitXPAccordingToTraitLevels()
		{
			foreach (TraitObject traitObject in TraitObject.All)
			{
				int traitLevel = Hero.MainHero.GetTraitLevel(traitObject);
				if (traitLevel != 0)
				{
					int traitXpRequiredForTraitLevel = Campaign.Current.Models.CharacterDevelopmentModel.GetTraitXpRequiredForTraitLevel(traitObject, traitLevel);
					Campaign.Current.PlayerTraitDeveloper.SetPropertyValue(traitObject, traitXpRequiredForTraitLevel);
				}
			}
		}

		// Token: 0x06003656 RID: 13910 RVA: 0x000E12F8 File Offset: 0x000DF4F8
		public static void OnBattleWon(MapEvent mapEvent, float contribution)
		{
			float strengthRatio = mapEvent.GetMapEventSide(PlayerEncounter.Current.PlayerSide).StrengthRatio;
			if (strengthRatio > 9f)
			{
				int xpValue = (int)(MBMath.Map(strengthRatio, 9f, 10f, 5f, 20f) * contribution);
				TraitLevelingHelper.AddPlayerTraitXPAndLogEntry(DefaultTraits.Valor, xpValue, ActionNotes.BattleValor, null);
			}
		}

		// Token: 0x06003657 RID: 13911 RVA: 0x000E134F File Offset: 0x000DF54F
		public static void OnTroopsSacrificed()
		{
			TraitLevelingHelper.AddPlayerTraitXPAndLogEntry(DefaultTraits.Valor, -30, ActionNotes.SacrificedTroops, null);
		}

		// Token: 0x06003658 RID: 13912 RVA: 0x000E1360 File Offset: 0x000DF560
		public static void OnLordExecuted()
		{
			TraitLevelingHelper.AddPlayerTraitXPAndLogEntry(DefaultTraits.Honor, -1000, ActionNotes.SacrificedTroops, null);
		}

		// Token: 0x06003659 RID: 13913 RVA: 0x000E1374 File Offset: 0x000DF574
		public static void OnVillageRaided()
		{
			TraitLevelingHelper.AddPlayerTraitXPAndLogEntry(DefaultTraits.Mercy, -30, ActionNotes.VillageRaid, null);
		}

		// Token: 0x0600365A RID: 13914 RVA: 0x000E1385 File Offset: 0x000DF585
		public static void OnHostileAction(int amount)
		{
			TraitLevelingHelper.AddPlayerTraitXPAndLogEntry(DefaultTraits.Honor, amount, ActionNotes.HostileAction, null);
			TraitLevelingHelper.AddPlayerTraitXPAndLogEntry(DefaultTraits.Mercy, amount, ActionNotes.HostileAction, null);
		}

		// Token: 0x0600365B RID: 13915 RVA: 0x000E13A3 File Offset: 0x000DF5A3
		public static void OnPartyTreatedWell()
		{
			TraitLevelingHelper.AddPlayerTraitXPAndLogEntry(DefaultTraits.Generosity, 20, ActionNotes.PartyTakenCareOf, null);
		}

		// Token: 0x0600365C RID: 13916 RVA: 0x000E13B4 File Offset: 0x000DF5B4
		public static void OnPartyStarved()
		{
			TraitLevelingHelper.AddPlayerTraitXPAndLogEntry(DefaultTraits.Generosity, -20, ActionNotes.PartyHungry, null);
		}

		// Token: 0x0600365D RID: 13917 RVA: 0x000E13C8 File Offset: 0x000DF5C8
		public static void OnIssueFailed(Hero targetHero, Tuple<TraitObject, int>[] effectedTraits)
		{
			foreach (Tuple<TraitObject, int> tuple in effectedTraits)
			{
				TraitLevelingHelper.AddPlayerTraitXPAndLogEntry(tuple.Item1, tuple.Item2, ActionNotes.QuestFailed, targetHero);
			}
		}

		// Token: 0x0600365E RID: 13918 RVA: 0x000E1400 File Offset: 0x000DF600
		public static void OnIssueSolvedThroughQuest(Hero targetHero, Tuple<TraitObject, int>[] effectedTraits)
		{
			foreach (Tuple<TraitObject, int> tuple in effectedTraits)
			{
				TraitLevelingHelper.AddPlayerTraitXPAndLogEntry(tuple.Item1, tuple.Item2, ActionNotes.QuestSuccess, targetHero);
			}
		}

		// Token: 0x0600365F RID: 13919 RVA: 0x000E1435 File Offset: 0x000DF635
		public static void OnIssueSolvedThroughQuest(Hero targetHero, TraitObject trait, int xp)
		{
			TraitLevelingHelper.AddPlayerTraitXPAndLogEntry(trait, xp, ActionNotes.QuestSuccess, targetHero);
		}

		// Token: 0x06003660 RID: 13920 RVA: 0x000E1444 File Offset: 0x000DF644
		public static void OnIssueSolvedThroughAlternativeSolution(Hero targetHero, Tuple<TraitObject, int>[] effectedTraits)
		{
			foreach (Tuple<TraitObject, int> tuple in effectedTraits)
			{
				TraitLevelingHelper.AddPlayerTraitXPAndLogEntry(tuple.Item1, tuple.Item2, ActionNotes.QuestSuccess, targetHero);
			}
		}

		// Token: 0x06003661 RID: 13921 RVA: 0x000E147C File Offset: 0x000DF67C
		public static void OnIssueSolvedThroughBetrayal(Hero targetHero, Tuple<TraitObject, int>[] effectedTraits)
		{
			foreach (Tuple<TraitObject, int> tuple in effectedTraits)
			{
				TraitLevelingHelper.AddPlayerTraitXPAndLogEntry(tuple.Item1, tuple.Item2, ActionNotes.QuestBetrayal, targetHero);
			}
		}

		// Token: 0x06003662 RID: 13922 RVA: 0x000E14B1 File Offset: 0x000DF6B1
		public static void OnLordFreed(Hero targetHero)
		{
			TraitLevelingHelper.AddPlayerTraitXPAndLogEntry(DefaultTraits.Calculating, 20, ActionNotes.NPCFreed, targetHero);
		}

		// Token: 0x06003663 RID: 13923 RVA: 0x000E14C2 File Offset: 0x000DF6C2
		public static void OnPersuasionDefection(Hero targetHero)
		{
			TraitLevelingHelper.AddPlayerTraitXPAndLogEntry(DefaultTraits.Calculating, 20, ActionNotes.PersuadedToDefect, targetHero);
		}

		// Token: 0x06003664 RID: 13924 RVA: 0x000E14D4 File Offset: 0x000DF6D4
		public static void OnSiegeAftermathApplied(Settlement settlement, SiegeAftermathAction.SiegeAftermath aftermathType, TraitObject[] effectedTraits)
		{
			foreach (TraitObject trait in effectedTraits)
			{
				TraitLevelingHelper.AddPlayerTraitXPAndLogEntry(trait, Campaign.Current.Models.SiegeAftermathModel.GetSiegeAftermathTraitXpChangeForPlayer(trait, settlement, aftermathType), ActionNotes.SiegeAftermath, null);
			}
		}

		// Token: 0x06003665 RID: 13925 RVA: 0x000E1515 File Offset: 0x000DF715
		public static void OnIncidentResolved(TraitObject trait, int xpValue)
		{
			TraitLevelingHelper.AddPlayerTraitXPAndLogEntry(trait, xpValue, ActionNotes.DefaultNote, Hero.MainHero);
		}

		// Token: 0x06003666 RID: 13926 RVA: 0x000E1524 File Offset: 0x000DF724
		private static void AddPlayerTraitXPAndLogEntry(TraitObject trait, int xpValue, ActionNotes context, Hero referenceHero)
		{
			int traitLevel = Hero.MainHero.GetTraitLevel(trait);
			TraitLevelingHelper.AddTraitXp(trait, xpValue);
			if (traitLevel != Hero.MainHero.GetTraitLevel(trait))
			{
				CampaignEventDispatcher.Instance.OnPlayerTraitChanged(trait, traitLevel);
			}
			if (MathF.Abs(xpValue) >= 10)
			{
				LogEntry.AddLogEntry(new PlayerReputationChangesLogEntry(trait, referenceHero, context));
			}
		}

		// Token: 0x06003667 RID: 13927 RVA: 0x000E1578 File Offset: 0x000DF778
		private static void AddTraitXp(TraitObject trait, int xpAmount)
		{
			xpAmount += Campaign.Current.PlayerTraitDeveloper.GetPropertyValue(trait);
			int num;
			int value;
			Campaign.Current.Models.CharacterDevelopmentModel.GetTraitLevelForTraitXp(Hero.MainHero, trait, xpAmount, out num, out value);
			Campaign.Current.PlayerTraitDeveloper.SetPropertyValue(trait, value);
			if (num != Hero.MainHero.GetTraitLevel(trait))
			{
				Hero.MainHero.SetTraitLevel(trait, num);
			}
		}

		// Token: 0x040010DE RID: 4318
		private const int LordExecutedHonorPenalty = -1000;

		// Token: 0x040010DF RID: 4319
		private const int TroopsSacrificedValorPenalty = -30;

		// Token: 0x040010E0 RID: 4320
		private const int VillageRaidedMercyPenalty = -30;

		// Token: 0x040010E1 RID: 4321
		private const int PartyStarvingGenerosityPenalty = -20;

		// Token: 0x040010E2 RID: 4322
		private const int PartyTreatedWellGenerosityBonus = 20;

		// Token: 0x040010E3 RID: 4323
		private const int LordFreedCalculatingBonus = 20;

		// Token: 0x040010E4 RID: 4324
		private const int PersuasionDefectionCalculatingBonus = 20;
	}
}
