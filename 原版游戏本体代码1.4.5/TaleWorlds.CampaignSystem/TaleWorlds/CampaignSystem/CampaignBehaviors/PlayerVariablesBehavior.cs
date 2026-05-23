using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class PlayerVariablesBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.PlayerDesertedBattleEvent.AddNonSerializedListener(this, OnPlayerDesertedBattle);
		CampaignEvents.VillageLooted.AddNonSerializedListener(this, OnVillageLooted);
		CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnPlayerBattleEnd);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnPlayerDesertedBattle(int sacrificedMenCount)
	{
		SkillLevelingManager.OnTacticsUsed(MobileParty.MainParty, sacrificedMenCount * 50);
		TraitLevelingHelper.OnTroopsSacrificed();
	}

	private void OnVillageLooted(Village village)
	{
		if (PlayerEncounter.Current != null && PlayerEncounter.PlayerIsAttacker && PlayerEncounter.EncounterSettlement != null && PlayerEncounter.EncounterSettlement.Village == village)
		{
			TraitLevelingHelper.OnVillageRaided();
		}
	}

	private void OnPlayerBattleEnd(MapEvent mapEvent)
	{
		TraitLevelingHelper.OnBattleWon(mapEvent, mapEvent.GetPlayerBattleContributionRate());
	}
}
