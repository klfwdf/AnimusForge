using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Nameplate;

public class SettlementNameplatePartyMarkersVM : ViewModel
{
	public class PartyMarkerItemComparer : IComparer<SettlementNameplatePartyMarkerItemVM>
	{
		public int Compare(SettlementNameplatePartyMarkerItemVM x, SettlementNameplatePartyMarkerItemVM y)
		{
			return x.SortIndex.CompareTo(y.SortIndex);
		}
	}

	private Settlement _settlement;

	private bool _eventsRegistered;

	private PartyMarkerItemComparer _itemComparer;

	private MBBindingList<SettlementNameplatePartyMarkerItemVM> _partiesInSettlement;

	public MBBindingList<SettlementNameplatePartyMarkerItemVM> PartiesInSettlement
	{
		get
		{
			return _partiesInSettlement;
		}
		set
		{
			if (value != _partiesInSettlement)
			{
				_partiesInSettlement = value;
				OnPropertyChangedWithValue(value, "PartiesInSettlement");
			}
		}
	}

	public SettlementNameplatePartyMarkersVM(Settlement settlement)
	{
		_settlement = settlement;
		PartiesInSettlement = new MBBindingList<SettlementNameplatePartyMarkerItemVM>();
		_itemComparer = new PartyMarkerItemComparer();
	}

	private void PopulatePartyList()
	{
		PartiesInSettlement.Clear();
		foreach (MobileParty item in _settlement.Parties.Where((MobileParty p) => IsMobilePartyValid(p)))
		{
			PartiesInSettlement.Add(new SettlementNameplatePartyMarkerItemVM(item));
		}
		PartiesInSettlement.Sort(_itemComparer);
	}

	private bool IsMobilePartyValid(MobileParty party)
	{
		if (!party.IsGarrison && !party.IsMilitia)
		{
			if (!party.IsMainParty || (party.IsMainParty && !Campaign.Current.IsMainHeroDisguised))
			{
				if (party.Army != null)
				{
					Army army = party.Army;
					if (army != null && army.LeaderParty.IsMainParty)
					{
						return !Campaign.Current.IsMainHeroDisguised;
					}
					return false;
				}
				return true;
			}
			return false;
		}
		return false;
	}

	private void OnSettlementLeft(MobileParty party, Settlement settlement)
	{
		if (settlement == _settlement)
		{
			SettlementNameplatePartyMarkerItemVM settlementNameplatePartyMarkerItemVM = PartiesInSettlement.SingleOrDefault((SettlementNameplatePartyMarkerItemVM p) => p.Party == party);
			if (settlementNameplatePartyMarkerItemVM != null)
			{
				PartiesInSettlement.Remove(settlementNameplatePartyMarkerItemVM);
			}
		}
	}

	private void OnSettlementEntered(MobileParty partyEnteredSettlement, Settlement settlement, Hero leader)
	{
		if (settlement == _settlement && partyEnteredSettlement != null && PartiesInSettlement.SingleOrDefault((SettlementNameplatePartyMarkerItemVM p) => p.Party == partyEnteredSettlement) == null && IsMobilePartyValid(partyEnteredSettlement))
		{
			PartiesInSettlement.Add(new SettlementNameplatePartyMarkerItemVM(partyEnteredSettlement));
			PartiesInSettlement.Sort(_itemComparer);
		}
	}

	private void OnMapEventEnded(MapEvent obj)
	{
		if (obj.MapEventSettlement != null && obj.MapEventSettlement == _settlement)
		{
			PopulatePartyList();
		}
	}

	public void RegisterEvents()
	{
		if (!_eventsRegistered)
		{
			PopulatePartyList();
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
			_eventsRegistered = true;
		}
	}

	public void UnloadEvents()
	{
		if (_eventsRegistered)
		{
			CampaignEvents.SettlementEntered.ClearListeners(this);
			CampaignEvents.OnSettlementLeftEvent.ClearListeners(this);
			CampaignEvents.MapEventEnded.ClearListeners(this);
			PartiesInSettlement.Clear();
			_eventsRegistered = false;
		}
	}
}
