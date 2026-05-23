using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003ED RID: 1005
	public class FactionDiscontinuationCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06003EAC RID: 16044 RVA: 0x00118FD4 File Offset: 0x001171D4
		public override void RegisterEvents()
		{
			CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChanged));
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
			CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, new Action<Clan>(this.DailyTickClan));
			CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, new Action(this.OnGameLoadFinished));
		}

		// Token: 0x06003EAD RID: 16045 RVA: 0x00119040 File Offset: 0x00117240
		public void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			if (this._independentClans.ContainsKey(newOwner.Clan))
			{
				this._independentClans.Remove(newOwner.Clan);
			}
			if (this.CanClanBeDiscontinued(oldOwner.Clan))
			{
				this.AddIndependentClan(oldOwner.Clan);
			}
			Kingdom kingdom = oldOwner.Clan.Kingdom;
			if (kingdom != null && this.CanKingdomBeDiscontinued(kingdom))
			{
				this.DiscontinueKingdom(kingdom);
			}
		}

		// Token: 0x06003EAE RID: 16046 RVA: 0x001190B0 File Offset: 0x001172B0
		public void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (newKingdom == null)
			{
				if (this.CanClanBeDiscontinued(clan))
				{
					this.AddIndependentClan(clan);
				}
			}
			else if (this._independentClans.ContainsKey(clan))
			{
				this._independentClans.Remove(clan);
			}
			if (clan == Clan.PlayerClan && oldKingdom != null && this.CanKingdomBeDiscontinued(oldKingdom))
			{
				this.DiscontinueKingdom(oldKingdom);
			}
		}

		// Token: 0x06003EAF RID: 16047 RVA: 0x00119108 File Offset: 0x00117308
		private void DailyTickClan(Clan clan)
		{
			if (this._independentClans.ContainsKey(clan) && this._independentClans[clan].IsPast)
			{
				this.DiscontinueClan(clan);
			}
		}

		// Token: 0x06003EB0 RID: 16048 RVA: 0x00119140 File Offset: 0x00117340
		private bool CanKingdomBeDiscontinued(Kingdom kingdom)
		{
			bool flag = !kingdom.IsEliminated && kingdom != Clan.PlayerClan.Kingdom && kingdom.Settlements.IsEmpty<Settlement>();
			if (flag)
			{
				CampaignEventDispatcher.Instance.CanKingdomBeDiscontinued(kingdom, ref flag);
			}
			return flag;
		}

		// Token: 0x06003EB1 RID: 16049 RVA: 0x00119184 File Offset: 0x00117384
		private void DiscontinueKingdom(Kingdom kingdom)
		{
			foreach (Clan clan in new List<Clan>(kingdom.Clans))
			{
				this.FinalizeMapEvents(clan);
				ChangeKingdomAction.ApplyByLeaveByKingdomDestruction(clan, true);
			}
			kingdom.RulingClan = null;
			DestroyKingdomAction.Apply(kingdom);
		}

		// Token: 0x06003EB2 RID: 16050 RVA: 0x001191F0 File Offset: 0x001173F0
		private void FinalizeMapEvents(Clan clan)
		{
			foreach (WarPartyComponent warPartyComponent in clan.WarPartyComponents.ToList<WarPartyComponent>())
			{
				if ((warPartyComponent != null) & warPartyComponent.Party.IsActive)
				{
					if (warPartyComponent.MobileParty.MapEvent != null)
					{
						warPartyComponent.MobileParty.MapEvent.FinalizeEvent();
					}
					if (warPartyComponent.MobileParty.SiegeEvent != null)
					{
						warPartyComponent.MobileParty.SiegeEvent.FinalizeSiegeEvent();
					}
				}
			}
			foreach (Settlement settlement in clan.Settlements)
			{
				if (settlement.Party.MapEvent != null)
				{
					settlement.Party.MapEvent.FinalizeEvent();
				}
				if (settlement.Party.SiegeEvent != null)
				{
					settlement.Party.SiegeEvent.FinalizeSiegeEvent();
				}
			}
		}

		// Token: 0x06003EB3 RID: 16051 RVA: 0x00119304 File Offset: 0x00117504
		private bool CanClanBeDiscontinued(Clan clan)
		{
			return clan.Kingdom == null && !clan.IsRebelClan && !clan.IsBanditFaction && !clan.IsMinorFaction && clan != Clan.PlayerClan && clan.Settlements.IsEmpty<Settlement>();
		}

		// Token: 0x06003EB4 RID: 16052 RVA: 0x0011933B File Offset: 0x0011753B
		private void DiscontinueClan(Clan clan)
		{
			DestroyClanAction.Apply(clan);
			this._independentClans.Remove(clan);
		}

		// Token: 0x06003EB5 RID: 16053 RVA: 0x00119350 File Offset: 0x00117550
		private void AddIndependentClan(Clan clan)
		{
			if (!this._independentClans.ContainsKey(clan))
			{
				this._independentClans.Add(clan, CampaignTime.DaysFromNow(28f));
			}
		}

		// Token: 0x06003EB6 RID: 16054 RVA: 0x00119376 File Offset: 0x00117576
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<Dictionary<Clan, CampaignTime>>("_independentClans", ref this._independentClans);
		}

		// Token: 0x06003EB7 RID: 16055 RVA: 0x0011938C File Offset: 0x0011758C
		private void OnGameLoadFinished()
		{
			if (MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.2", 0))
			{
				foreach (Kingdom kingdom in Kingdom.All)
				{
					if (!kingdom.IsEliminated && this.CanKingdomBeDiscontinued(kingdom))
					{
						this.DiscontinueKingdom(kingdom);
					}
				}
			}
		}

		// Token: 0x040012B1 RID: 4785
		private const float SurvivalDurationForIndependentClanInDays = 28f;

		// Token: 0x040012B2 RID: 4786
		private Dictionary<Clan, CampaignTime> _independentClans = new Dictionary<Clan, CampaignTime>();
	}
}
