using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000431 RID: 1073
	public class PlayerVariablesBehavior : CampaignBehaviorBase
	{
		// Token: 0x06004445 RID: 17477 RVA: 0x0014CE7C File Offset: 0x0014B07C
		public override void RegisterEvents()
		{
			CampaignEvents.PlayerDesertedBattleEvent.AddNonSerializedListener(this, new Action<int>(this.OnPlayerDesertedBattle));
			CampaignEvents.VillageLooted.AddNonSerializedListener(this, new Action<Village>(this.OnVillageLooted));
			CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, new Action<MapEvent>(this.OnPlayerBattleEnd));
		}

		// Token: 0x06004446 RID: 17478 RVA: 0x0014CECE File Offset: 0x0014B0CE
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06004447 RID: 17479 RVA: 0x0014CED0 File Offset: 0x0014B0D0
		private void OnPlayerDesertedBattle(int sacrificedMenCount)
		{
			SkillLevelingManager.OnTacticsUsed(MobileParty.MainParty, (float)(sacrificedMenCount * 50));
			TraitLevelingHelper.OnTroopsSacrificed();
		}

		// Token: 0x06004448 RID: 17480 RVA: 0x0014CEE6 File Offset: 0x0014B0E6
		private void OnVillageLooted(Village village)
		{
			if (PlayerEncounter.Current != null && PlayerEncounter.PlayerIsAttacker && PlayerEncounter.EncounterSettlement != null && PlayerEncounter.EncounterSettlement.Village == village)
			{
				TraitLevelingHelper.OnVillageRaided();
			}
		}

		// Token: 0x06004449 RID: 17481 RVA: 0x0014CF10 File Offset: 0x0014B110
		private void OnPlayerBattleEnd(MapEvent mapEvent)
		{
			float playerPartyContributionRate = (mapEvent.AttackerSide.IsMainPartyAmongParties() ? mapEvent.AttackerSide : mapEvent.DefenderSide).GetPlayerPartyContributionRate();
			TraitLevelingHelper.OnBattleWon(mapEvent, playerPartyContributionRate);
		}
	}
}
