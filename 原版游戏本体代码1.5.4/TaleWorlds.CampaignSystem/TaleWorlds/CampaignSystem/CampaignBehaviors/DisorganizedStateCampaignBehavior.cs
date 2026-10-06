using System.Linq;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

internal class DisorganizedStateCampaignBehavior : CampaignBehaviorBase
{
	private bool _checkForEvent;

	public override void RegisterEvents()
	{
		CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnd);
		CampaignEvents.PartyRemovedFromArmyEvent.AddNonSerializedListener(this, OnPartyRemovedFromArmy);
		CampaignEvents.GameMenuOptionSelectedEvent.AddNonSerializedListener(this, OnGameMenuOptionSelected);
	}

	private void OnGameMenuOptionSelected(GameMenu gameMenu, GameMenuOption gameMenuOption)
	{
		if (!_checkForEvent || (!(gameMenuOption.IdString == "str_order_attack") && !(gameMenuOption.IdString == "attack")))
		{
			return;
		}
		foreach (MapEventParty party in MobileParty.MainParty.MapEvent.DefenderSide.Parties)
		{
			if (Campaign.Current.Models.PartyImpairmentModel.CanGetDisorganized(party.Party))
			{
				party.Party.MobileParty.SetDisorganized(isDisorganized: true);
			}
		}
	}

	private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
	{
		if (!mapEvent.IsSallyOut)
		{
			return;
		}
		if (!mapEvent.AttackerSide.IsMainPartyAmongParties())
		{
			foreach (MapEventParty party in mapEvent.DefenderSide.Parties)
			{
				if (Campaign.Current.Models.PartyImpairmentModel.CanGetDisorganized(party.Party))
				{
					party.Party.MobileParty.SetDisorganized(isDisorganized: true);
				}
			}
			return;
		}
		_checkForEvent = true;
	}

	private void OnPartyRemovedFromArmy(MobileParty mobileParty)
	{
		if (Campaign.Current.Models.PartyImpairmentModel.CanGetDisorganized(mobileParty.Party))
		{
			mobileParty.SetDisorganized(isDisorganized: true);
		}
	}

	private void OnMapEventEnd(MapEvent mapEvent)
	{
		if ((mapEvent.AttackerSide.Parties.Sum((MapEventParty x) => x.HealthyManCountAtStart) != mapEvent.AttackerSide.Parties.Sum((MapEventParty x) => x.Party.NumberOfHealthyMembers) || mapEvent.DefenderSide.Parties.Sum((MapEventParty x) => x.HealthyManCountAtStart) != mapEvent.DefenderSide.Parties.Sum((MapEventParty x) => x.Party.NumberOfHealthyMembers)) && !mapEvent.IsHideoutBattle)
		{
			foreach (PartyBase involvedParty in mapEvent.InvolvedParties)
			{
				if (involvedParty.IsActive)
				{
					MobileParty mobileParty = involvedParty.MobileParty;
					if ((mobileParty == null || !mobileParty.IsMainParty || !mapEvent.DiplomaticallyFinished || !mapEvent.AttackerSide.MapFaction.IsAtWarWith(mapEvent.DefenderSide.MapFaction)) && (!mapEvent.IsSallyOut || involvedParty.MapEventSide.MissionSide == BattleSideEnum.Defender) && Campaign.Current.Models.PartyImpairmentModel.CanGetDisorganized(involvedParty) && (mapEvent.RetreatingSide == BattleSideEnum.None || mapEvent.RetreatingSide != involvedParty.Side))
					{
						involvedParty.MobileParty.SetDisorganized(isDisorganized: true);
					}
				}
			}
		}
		_checkForEvent = false;
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_checkForEvent", ref _checkForEvent);
	}
}
