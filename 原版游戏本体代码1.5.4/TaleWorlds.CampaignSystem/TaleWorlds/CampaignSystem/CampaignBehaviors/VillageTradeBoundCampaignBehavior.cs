using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class VillageTradeBoundCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		CampaignEvents.WarDeclared.AddNonSerializedListener(this, WarDeclared);
		CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeace);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, ClanChangedKingdom);
		CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener(this, OnClanDestroyed);
	}

	private void OnClanDestroyed(Clan obj)
	{
		UpdateTradeBounds();
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void ClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
	{
		UpdateTradeBounds();
	}

	private void OnGameLoaded(CampaignGameStarter obj)
	{
		UpdateTradeBounds();
	}

	private void OnMakePeace(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
	{
		UpdateTradeBounds();
	}

	private void WarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail declareWarDetail)
	{
		UpdateTradeBounds();
	}

	private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		UpdateTradeBounds();
	}

	public void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
	{
		UpdateTradeBounds();
	}

	private void UpdateTradeBounds()
	{
		foreach (Town allCastle in Campaign.Current.AllCastles)
		{
			foreach (Village village in allCastle.Villages)
			{
				village.TradeBound = Campaign.Current.Models.VillageTradeModel.GetTradeBoundToAssignForVillage(village);
			}
		}
	}
}
