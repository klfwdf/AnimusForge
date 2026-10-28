using TaleWorlds.CampaignSystem.Actions;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class PartyConfigurationCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnHeroChangedClanEvent.AddNonSerializedListener(this, OnHeroChangedClan);
		CampaignEvents.OnBeforePlayerCharacterChangedEvent.AddNonSerializedListener(this, OnBeforePlayerCharacterChanged);
		CampaignEvents.CompanionRemoved.AddNonSerializedListener(this, OnCompanionRemoved);
	}

	private void OnCompanionRemoved(Hero hero, RemoveCompanionAction.RemoveCompanionDetail detail)
	{
		hero?.ResetPartyConfiguration();
	}

	private void OnBeforePlayerCharacterChanged(Hero _, Hero hero)
	{
		hero?.ResetPartyConfiguration();
	}

	private void OnHeroChangedClan(Hero hero, Clan clan)
	{
		if (clan == Clan.PlayerClan)
		{
			hero.ResetPartyConfiguration();
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
