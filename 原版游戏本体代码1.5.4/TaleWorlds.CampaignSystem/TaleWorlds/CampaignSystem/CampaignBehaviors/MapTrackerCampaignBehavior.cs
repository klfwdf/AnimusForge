using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class MapTrackerCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, OnMobilePartyCreated);
		CampaignEvents.OnPartyRemovedEvent.AddNonSerializedListener(this, OnPartyRemoved);
		CampaignEvents.MobilePartyQuestStatusChanged.AddNonSerializedListener(this, OnPartyQuestStatusChanged);
		CampaignEvents.ArmyCreated.AddNonSerializedListener(this, OnArmyCreated);
		CampaignEvents.ArmyDispersed.AddNonSerializedListener(this, OnArmyDispersed);
		CampaignEvents.OnPartyJoinedArmyEvent.AddNonSerializedListener(this, OnPartyJoinedArmy);
		CampaignEvents.PartyRemovedFromArmyEvent.AddNonSerializedListener(this, OnPartyRemovedFromArmy);
		CampaignEvents.OnHeroChangedClanEvent.AddNonSerializedListener(this, OnHeroChangedClan);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		CampaignEvents.OnClanCreatedEvent.AddNonSerializedListener(this, OnCompanionClanCreated);
		CampaignEvents.OnPlayerCharacterChangedEvent.AddNonSerializedListener(this, OnPlayerCharacterChanged);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnMobilePartyCreated(MobileParty mobileParty)
	{
		Campaign.Current.MapTrackerManager.Refresh(mobileParty);
	}

	private void OnPartyRemoved(PartyBase partyBase)
	{
		if (partyBase.IsMobile)
		{
			Campaign.Current.MapTrackerManager.RemoveMapTracker(partyBase.MobileParty);
			Campaign.Current.MapTrackerManager.Refresh(partyBase.MobileParty);
		}
	}

	private void OnPartyQuestStatusChanged(MobileParty mobileParty, bool isUsedByQuest)
	{
		Campaign.Current.MapTrackerManager.Refresh(mobileParty);
	}

	private void OnArmyCreated(Army army)
	{
		Campaign.Current.MapTrackerManager.Refresh(army);
	}

	private void OnArmyDispersed(Army army, Army.ArmyDispersionReason reason, bool isPlayerJoining)
	{
		Campaign.Current.MapTrackerManager.ForceRemoveTracker(army);
	}

	private void OnPartyJoinedArmy(MobileParty mobileParty)
	{
		if (mobileParty == MobileParty.MainParty && mobileParty.Army != null)
		{
			Campaign.Current.MapTrackerManager.Refresh(mobileParty.Army);
		}
	}

	private void OnPartyRemovedFromArmy(MobileParty mobileParty)
	{
		if (mobileParty != MobileParty.MainParty)
		{
			return;
		}
		for (int i = 0; i < Kingdom.All.Count; i++)
		{
			Kingdom kingdom = Kingdom.All[i];
			for (int j = 0; j < kingdom.Armies.Count; j++)
			{
				Campaign.Current.MapTrackerManager.Refresh(kingdom.Armies[j]);
			}
		}
	}

	private void OnHeroChangedClan(Hero hero, Clan oldClan)
	{
		if (hero.PartyBelongedTo != null)
		{
			Campaign.Current.MapTrackerManager.Refresh(hero.PartyBelongedTo);
		}
		for (int i = 0; i < hero.OwnedCaravans.Count; i++)
		{
			Campaign.Current.MapTrackerManager.Refresh(hero.OwnedCaravans[i].MobileParty);
		}
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
	{
		if (clan == Clan.PlayerClan)
		{
			Campaign.Current.MapTrackerManager.ResetTrackers();
		}
	}

	private void OnCompanionClanCreated(Clan clan, bool isCompanion)
	{
		if (isCompanion && clan.Leader.PartyBelongedTo != null)
		{
			Campaign.Current.MapTrackerManager.Refresh(clan.Leader.PartyBelongedTo);
		}
	}

	private void OnPlayerCharacterChanged(Hero oldPlayer, Hero newPlayer, MobileParty newMainParty, bool isMainPartyChanged)
	{
		Campaign.Current.MapTrackerManager.ResetTrackers();
	}
}
