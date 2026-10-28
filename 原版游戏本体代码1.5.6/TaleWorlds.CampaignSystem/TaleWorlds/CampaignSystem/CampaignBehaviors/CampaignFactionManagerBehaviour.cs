using TaleWorlds.CampaignSystem.Actions;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class CampaignFactionManagerBehaviour : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		CampaignEvents.KingdomCreatedEvent.AddNonSerializedListener(this, OnKingdomCreated);
		CampaignEvents.OnClanCreatedEvent.AddNonSerializedListener(this, OnClanCreated);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdomEvent);
	}

	private void OnClanChangedKingdomEvent(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool arg5)
	{
		RefreshFactionsAtWarWith();
	}

	private void OnNewGameCreated(CampaignGameStarter obj)
	{
		RefreshFactionsAtWarWith();
	}

	private void OnGameLoaded(CampaignGameStarter obj)
	{
		RefreshFactionsAtWarWith();
	}

	private void OnClanCreated(Clan obj, bool isCompanion)
	{
		RefreshFactionsAtWarWith();
	}

	private void OnKingdomCreated(Kingdom obj)
	{
		RefreshFactionsAtWarWith();
	}

	private static void RefreshFactionsAtWarWith()
	{
		foreach (Kingdom item in Kingdom.All)
		{
			item.UpdateFactionsAtWarWith();
		}
		foreach (Clan item2 in Clan.All)
		{
			item2.UpdateFactionsAtWarWith();
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
