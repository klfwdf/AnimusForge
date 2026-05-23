using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class FactionDiscontinuationCampaignBehavior : CampaignBehaviorBase
{
	private const float SurvivalDurationForIndependentClanInDays = 28f;

	private Dictionary<Clan, CampaignTime> _independentClans = new Dictionary<Clan, CampaignTime>();

	public override void RegisterEvents()
	{
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, DailyTickClan);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
	}

	public void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		if (_independentClans.ContainsKey(newOwner.Clan))
		{
			_independentClans.Remove(newOwner.Clan);
		}
		if (CanClanBeDiscontinued(oldOwner.Clan))
		{
			AddIndependentClan(oldOwner.Clan);
		}
		Kingdom kingdom = oldOwner.Clan.Kingdom;
		if (kingdom != null && CanKingdomBeDiscontinued(kingdom))
		{
			DiscontinueKingdom(kingdom);
		}
	}

	public void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
	{
		if (newKingdom == null)
		{
			if (CanClanBeDiscontinued(clan))
			{
				AddIndependentClan(clan);
			}
		}
		else if (_independentClans.ContainsKey(clan))
		{
			_independentClans.Remove(clan);
		}
		if (clan == Clan.PlayerClan && oldKingdom != null && CanKingdomBeDiscontinued(oldKingdom))
		{
			DiscontinueKingdom(oldKingdom);
		}
	}

	private void DailyTickClan(Clan clan)
	{
		if (_independentClans.ContainsKey(clan) && _independentClans[clan].IsPast)
		{
			DiscontinueClan(clan);
		}
	}

	private bool CanKingdomBeDiscontinued(Kingdom kingdom)
	{
		bool result = !kingdom.IsEliminated && kingdom != Clan.PlayerClan.Kingdom && kingdom.Settlements.IsEmpty();
		if (result)
		{
			CampaignEventDispatcher.Instance.CanKingdomBeDiscontinued(kingdom, ref result);
		}
		return result;
	}

	private void DiscontinueKingdom(Kingdom kingdom)
	{
		foreach (Clan item in new List<Clan>(kingdom.Clans))
		{
			FinalizeMapEvents(item);
			ChangeKingdomAction.ApplyByLeaveByKingdomDestruction(item);
		}
		kingdom.RulingClan = null;
		DestroyKingdomAction.Apply(kingdom);
	}

	private void FinalizeMapEvents(Clan clan)
	{
		foreach (WarPartyComponent item in clan.WarPartyComponents.ToList())
		{
			if (item?.Party.IsActive ?? false)
			{
				if (item.MobileParty.MapEvent != null)
				{
					item.MobileParty.MapEvent.FinalizeEvent();
				}
				if (item.MobileParty.SiegeEvent != null)
				{
					item.MobileParty.SiegeEvent.FinalizeSiegeEvent();
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

	private bool CanClanBeDiscontinued(Clan clan)
	{
		if (clan.Kingdom == null && !clan.IsRebelClan && !clan.IsBanditFaction && !clan.IsMinorFaction && clan != Clan.PlayerClan)
		{
			return clan.Settlements.IsEmpty();
		}
		return false;
	}

	private void DiscontinueClan(Clan clan)
	{
		DestroyClanAction.Apply(clan);
		_independentClans.Remove(clan);
	}

	private void AddIndependentClan(Clan clan)
	{
		if (!_independentClans.ContainsKey(clan))
		{
			_independentClans.Add(clan, CampaignTime.DaysFromNow(28f));
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_independentClans", ref _independentClans);
	}

	private void OnGameLoadFinished()
	{
		if (!(MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.2")))
		{
			return;
		}
		foreach (Kingdom item in Kingdom.All)
		{
			if (!item.IsEliminated && CanKingdomBeDiscontinued(item))
			{
				DiscontinueKingdom(item);
			}
		}
	}
}
