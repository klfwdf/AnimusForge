using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class TownSecurityCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, MapEventEnded);
		CampaignEvents.OnSiegeEventEndedEvent.AddNonSerializedListener(this, SiegeEventEnded);
		CampaignEvents.OnHideoutDeactivatedEvent.AddNonSerializedListener(this, OnHideoutDeactivated);
	}

	private void OnHideoutDeactivated(Settlement hideout)
	{
		SettlementSecurityModel model = Campaign.Current.Models.SettlementSecurityModel;
		foreach (Settlement item in Settlement.All.Where((Settlement t) => t.IsTown && t.Position.DistanceSquared(hideout.Position) < model.HideoutClearedSecurityEffectRadius * model.HideoutClearedSecurityEffectRadius).ToList())
		{
			item.Town.Security += model.HideoutClearedSecurityGain;
		}
	}

	private void MapEventEnded(MapEvent mapEvent)
	{
		if (!mapEvent.IsFieldBattle || !mapEvent.HasWinner)
		{
			return;
		}
		SettlementSecurityModel model = Campaign.Current.Models.SettlementSecurityModel;
		foreach (Settlement town in Settlement.All.Where((Settlement t) => t.IsTown && t.Position.DistanceSquared(mapEvent.Position) < model.MapEventSecurityEffectRadius * model.MapEventSecurityEffectRadius).ToList())
		{
			if (mapEvent.Winner.Parties.Any((MapEventParty party) => party.Party.IsMobile && party.Party.MobileParty.IsBandit) && mapEvent.InvolvedParties.Any((PartyBase party) => ValidCivilianPartyCondition(party, mapEvent, town.MapFaction)))
			{
				float sumOfAttackedPartyStrengths = mapEvent.StrengthOfSide[(int)mapEvent.DefeatedSide];
				town.Town.Security += model.GetLootedNearbyPartySecurityEffect(town.Town, sumOfAttackedPartyStrengths);
			}
			else if (mapEvent.InvolvedParties.Any((PartyBase party) => ValidBanditPartyCondition(party, mapEvent)))
			{
				float sumOfAttackedPartyStrengths2 = mapEvent.StrengthOfSide[(int)mapEvent.DefeatedSide];
				town.Town.Security += model.GetNearbyBanditPartyDefeatedSecurityEffect(town.Town, sumOfAttackedPartyStrengths2);
			}
		}
	}

	private bool ValidCivilianPartyCondition(PartyBase party, MapEvent mapEvent, IFaction mapFaction)
	{
		if (party.IsMobile)
		{
			if (party.Side == mapEvent.WinningSide || !party.MobileParty.IsVillager || !DiplomacyHelper.IsSameFactionAndNotEliminated(party.MapFaction, mapFaction))
			{
				if (party.MobileParty.IsCaravan)
				{
					return !party.MapFaction.IsAtWarWith(mapFaction);
				}
				return false;
			}
			return true;
		}
		return false;
	}

	private bool ValidBanditPartyCondition(PartyBase party, MapEvent mapEvent)
	{
		if (party.Side != mapEvent.WinningSide)
		{
			return party.MobileParty?.IsBandit ?? false;
		}
		return false;
	}

	private void SiegeEventEnded(SiegeEvent siegeEvent)
	{
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
