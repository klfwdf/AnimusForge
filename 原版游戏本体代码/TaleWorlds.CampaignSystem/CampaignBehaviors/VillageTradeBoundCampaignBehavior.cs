using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000450 RID: 1104
	public class VillageTradeBoundCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x060046CC RID: 18124 RVA: 0x00161914 File Offset: 0x0015FB14
		public override void RegisterEvents()
		{
			CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreated));
			CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChanged));
			CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnGameLoaded));
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.WarDeclared));
			CampaignEvents.MakePeace.AddNonSerializedListener(this, new Action<IFaction, IFaction, MakePeaceAction.MakePeaceDetail>(this.OnMakePeace));
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.ClanChangedKingdom));
			CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener(this, new Action<Clan>(this.OnClanDestroyed));
		}

		// Token: 0x060046CD RID: 18125 RVA: 0x001619C2 File Offset: 0x0015FBC2
		private void OnClanDestroyed(Clan obj)
		{
			this.UpdateTradeBounds();
		}

		// Token: 0x060046CE RID: 18126 RVA: 0x001619CA File Offset: 0x0015FBCA
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x060046CF RID: 18127 RVA: 0x001619CC File Offset: 0x0015FBCC
		private void ClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			this.UpdateTradeBounds();
		}

		// Token: 0x060046D0 RID: 18128 RVA: 0x001619D4 File Offset: 0x0015FBD4
		private void OnGameLoaded(CampaignGameStarter obj)
		{
			this.UpdateTradeBounds();
		}

		// Token: 0x060046D1 RID: 18129 RVA: 0x001619DC File Offset: 0x0015FBDC
		private void OnMakePeace(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
		{
			this.UpdateTradeBounds();
		}

		// Token: 0x060046D2 RID: 18130 RVA: 0x001619E4 File Offset: 0x0015FBE4
		private void WarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail declareWarDetail)
		{
			this.UpdateTradeBounds();
		}

		// Token: 0x060046D3 RID: 18131 RVA: 0x001619EC File Offset: 0x0015FBEC
		private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			this.UpdateTradeBounds();
		}

		// Token: 0x060046D4 RID: 18132 RVA: 0x001619F4 File Offset: 0x0015FBF4
		public void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
		{
			this.UpdateTradeBounds();
		}

		// Token: 0x060046D5 RID: 18133 RVA: 0x001619FC File Offset: 0x0015FBFC
		private void UpdateTradeBounds()
		{
			foreach (Town town in Campaign.Current.AllCastles)
			{
				foreach (Village village in town.Villages)
				{
					village.TradeBound = Campaign.Current.Models.VillageTradeModel.GetTradeBoundToAssignForVillage(village);
				}
			}
		}
	}
}
