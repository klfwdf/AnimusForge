using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Nameplate
{
	// Token: 0x0200001F RID: 31
	public class SettlementNameplatePartyMarkersVM : ViewModel
	{
		// Token: 0x060002D9 RID: 729 RVA: 0x0000C328 File Offset: 0x0000A528
		public SettlementNameplatePartyMarkersVM(Settlement settlement)
		{
			this._settlement = settlement;
			this.PartiesInSettlement = new MBBindingList<SettlementNameplatePartyMarkerItemVM>();
			this._itemComparer = new SettlementNameplatePartyMarkersVM.PartyMarkerItemComparer();
		}

		// Token: 0x060002DA RID: 730 RVA: 0x0000C350 File Offset: 0x0000A550
		private void PopulatePartyList()
		{
			this.PartiesInSettlement.Clear();
			foreach (MobileParty mobileParty in from p in this._settlement.Parties
				where this.IsMobilePartyValid(p)
				select p)
			{
				this.PartiesInSettlement.Add(new SettlementNameplatePartyMarkerItemVM(mobileParty));
			}
			this.PartiesInSettlement.Sort(this._itemComparer);
		}

		// Token: 0x060002DB RID: 731 RVA: 0x0000C3DC File Offset: 0x0000A5DC
		private bool IsMobilePartyValid(MobileParty party)
		{
			if (party.IsGarrison || party.IsMilitia)
			{
				return false;
			}
			if (party.IsMainParty && (!party.IsMainParty || Campaign.Current.IsMainHeroDisguised))
			{
				return false;
			}
			if (party.Army != null)
			{
				Army army = party.Army;
				return army != null && army.LeaderParty.IsMainParty && !Campaign.Current.IsMainHeroDisguised;
			}
			return true;
		}

		// Token: 0x060002DC RID: 732 RVA: 0x0000C44C File Offset: 0x0000A64C
		private void OnSettlementLeft(MobileParty party, Settlement settlement)
		{
			if (settlement == this._settlement)
			{
				SettlementNameplatePartyMarkerItemVM settlementNameplatePartyMarkerItemVM = this.PartiesInSettlement.SingleOrDefault((SettlementNameplatePartyMarkerItemVM p) => p.Party == party);
				if (settlementNameplatePartyMarkerItemVM != null)
				{
					this.PartiesInSettlement.Remove(settlementNameplatePartyMarkerItemVM);
				}
			}
		}

		// Token: 0x060002DD RID: 733 RVA: 0x0000C498 File Offset: 0x0000A698
		private void OnSettlementEntered(MobileParty partyEnteredSettlement, Settlement settlement, Hero leader)
		{
			if (settlement == this._settlement && partyEnteredSettlement != null && this.PartiesInSettlement.SingleOrDefault((SettlementNameplatePartyMarkerItemVM p) => p.Party == partyEnteredSettlement) == null && this.IsMobilePartyValid(partyEnteredSettlement))
			{
				this.PartiesInSettlement.Add(new SettlementNameplatePartyMarkerItemVM(partyEnteredSettlement));
				this.PartiesInSettlement.Sort(this._itemComparer);
			}
		}

		// Token: 0x060002DE RID: 734 RVA: 0x0000C511 File Offset: 0x0000A711
		private void OnMapEventEnded(MapEvent obj)
		{
			if (obj.MapEventSettlement != null && obj.MapEventSettlement == this._settlement)
			{
				this.PopulatePartyList();
			}
		}

		// Token: 0x060002DF RID: 735 RVA: 0x0000C530 File Offset: 0x0000A730
		public void RegisterEvents()
		{
			if (!this._eventsRegistered)
			{
				this.PopulatePartyList();
				CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
				CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement>(this.OnSettlementLeft));
				CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(this.OnMapEventEnded));
				this._eventsRegistered = true;
			}
		}

		// Token: 0x060002E0 RID: 736 RVA: 0x0000C597 File Offset: 0x0000A797
		public void UnloadEvents()
		{
			if (this._eventsRegistered)
			{
				CampaignEvents.SettlementEntered.ClearListeners(this);
				CampaignEvents.OnSettlementLeftEvent.ClearListeners(this);
				CampaignEvents.MapEventEnded.ClearListeners(this);
				this.PartiesInSettlement.Clear();
				this._eventsRegistered = false;
			}
		}

		// Token: 0x170000EE RID: 238
		// (get) Token: 0x060002E1 RID: 737 RVA: 0x0000C5D4 File Offset: 0x0000A7D4
		// (set) Token: 0x060002E2 RID: 738 RVA: 0x0000C5DC File Offset: 0x0000A7DC
		public MBBindingList<SettlementNameplatePartyMarkerItemVM> PartiesInSettlement
		{
			get
			{
				return this._partiesInSettlement;
			}
			set
			{
				if (value != this._partiesInSettlement)
				{
					this._partiesInSettlement = value;
					base.OnPropertyChangedWithValue<MBBindingList<SettlementNameplatePartyMarkerItemVM>>(value, "PartiesInSettlement");
				}
			}
		}

		// Token: 0x04000163 RID: 355
		private Settlement _settlement;

		// Token: 0x04000164 RID: 356
		private bool _eventsRegistered;

		// Token: 0x04000165 RID: 357
		private SettlementNameplatePartyMarkersVM.PartyMarkerItemComparer _itemComparer;

		// Token: 0x04000166 RID: 358
		private MBBindingList<SettlementNameplatePartyMarkerItemVM> _partiesInSettlement;

		// Token: 0x02000087 RID: 135
		public class PartyMarkerItemComparer : IComparer<SettlementNameplatePartyMarkerItemVM>
		{
			// Token: 0x06000669 RID: 1641 RVA: 0x00016790 File Offset: 0x00014990
			public int Compare(SettlementNameplatePartyMarkerItemVM x, SettlementNameplatePartyMarkerItemVM y)
			{
				return x.SortIndex.CompareTo(y.SortIndex);
			}
		}
	}
}
