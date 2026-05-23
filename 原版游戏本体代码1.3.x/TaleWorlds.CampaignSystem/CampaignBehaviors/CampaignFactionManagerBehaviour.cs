using System;
using TaleWorlds.CampaignSystem.Actions;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003D6 RID: 982
	public class CampaignFactionManagerBehaviour : CampaignBehaviorBase
	{
		// Token: 0x06003A70 RID: 14960 RVA: 0x000F1B74 File Offset: 0x000EFD74
		public override void RegisterEvents()
		{
			CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreated));
			CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnGameLoaded));
			CampaignEvents.KingdomCreatedEvent.AddNonSerializedListener(this, new Action<Kingdom>(this.OnKingdomCreated));
			CampaignEvents.OnClanCreatedEvent.AddNonSerializedListener(this, new Action<Clan, bool>(this.OnClanCreated));
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdomEvent));
		}

		// Token: 0x06003A71 RID: 14961 RVA: 0x000F1BF4 File Offset: 0x000EFDF4
		private void OnClanChangedKingdomEvent(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool arg5)
		{
			CampaignFactionManagerBehaviour.RefreshFactionsAtWarWith();
		}

		// Token: 0x06003A72 RID: 14962 RVA: 0x000F1BFB File Offset: 0x000EFDFB
		private void OnNewGameCreated(CampaignGameStarter obj)
		{
			CampaignFactionManagerBehaviour.RefreshFactionsAtWarWith();
		}

		// Token: 0x06003A73 RID: 14963 RVA: 0x000F1C02 File Offset: 0x000EFE02
		private void OnGameLoaded(CampaignGameStarter obj)
		{
			CampaignFactionManagerBehaviour.RefreshFactionsAtWarWith();
		}

		// Token: 0x06003A74 RID: 14964 RVA: 0x000F1C09 File Offset: 0x000EFE09
		private void OnClanCreated(Clan obj, bool isCompanion)
		{
			CampaignFactionManagerBehaviour.RefreshFactionsAtWarWith();
		}

		// Token: 0x06003A75 RID: 14965 RVA: 0x000F1C10 File Offset: 0x000EFE10
		private void OnKingdomCreated(Kingdom obj)
		{
			CampaignFactionManagerBehaviour.RefreshFactionsAtWarWith();
		}

		// Token: 0x06003A76 RID: 14966 RVA: 0x000F1C18 File Offset: 0x000EFE18
		private static void RefreshFactionsAtWarWith()
		{
			foreach (Kingdom kingdom in Kingdom.All)
			{
				kingdom.UpdateFactionsAtWarWith();
			}
			foreach (Clan clan in Clan.All)
			{
				clan.UpdateFactionsAtWarWith();
			}
		}

		// Token: 0x06003A77 RID: 14967 RVA: 0x000F1CA8 File Offset: 0x000EFEA8
		public override void SyncData(IDataStore dataStore)
		{
		}
	}
}
