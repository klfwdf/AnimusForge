using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace NavalDLC.CampaignBehaviors;

public class ClanFleetManagementCampaignBehavior : CampaignBehaviorBase, IFleetManagementCampaignBehavior
{
	private class SentTroopsData
	{
		[SaveableProperty(0)]
		public TroopRoster SentTroops { get; private set; }

		[SaveableProperty(1)]
		public CampaignTime TroopReturnTime { get; private set; }

		[SaveableProperty(2)]
		public TextObject ShipName { get; private set; }

		[SaveableProperty(3)]
		public TextObject PartyName { get; private set; }

		public static SentTroopsData GetSentTroops(TroopRoster troops, CampaignTime returnTime, TextObject shipName, TextObject partyName)
		{
			SentTroopsData sentTroopsData = new SentTroopsData();
			sentTroopsData.SentTroops = TroopRoster.CreateDummyTroopRoster();
			sentTroopsData.SentTroops.Add(troops);
			sentTroopsData.TroopReturnTime = returnTime;
			sentTroopsData.ShipName = shipName;
			sentTroopsData.PartyName = partyName;
			return sentTroopsData;
		}
	}

	public class ClanFleetManagementCampaignBehaviorTypeDefiner : SaveableTypeDefiner
	{
		public ClanFleetManagementCampaignBehaviorTypeDefiner()
			: base(612504)
		{
		}

		protected override void DefineContainerDefinitions()
		{
			ConstructContainerDefinition(typeof(List<SentTroopsData>));
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(SentTroopsData), 2);
		}
	}

	private List<SentTroopsData> _sentTroops = new List<SentTroopsData>();

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
	}

	private void HourlyTick()
	{
		if (!Campaign.Current.Models.FleetManagementModel.CanTroopsReturn())
		{
			return;
		}
		for (int num = _sentTroops.Count - 1; num >= 0; num--)
		{
			if (_sentTroops[num].TroopReturnTime.IsPast)
			{
				MakeTroopsReturn(_sentTroops[num]);
				_sentTroops.RemoveAt(num);
			}
		}
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		AddDialogs(starter);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_sentTroops", ref _sentTroops);
	}

	private void AddDialogs(CampaignGameStarter starter)
	{
		starter.AddPlayerLine("clan_party_manage_fleet", "hero_main_options", "clan_party_manage_fleet_screen", "{=7DdiFD9W}Let me inspect your ships.", conversation_clan_member_manage_fleet_on_condition, null, 90, conversation_clan_member_manage_fleet_clickable_condition);
		starter.AddDialogLine("clan_party_manage_fleet_screen", "clan_party_manage_fleet_screen", "lord_pretalk", "{=!}fleet screen goes here.", null, conversation_clan_member_manage_fleet_on_consequence);
	}

	private void MakeTroopsReturn(SentTroopsData sentTroops)
	{
		MobileParty.MainParty.MemberRoster.Add(sentTroops.SentTroops);
		TextObject textObject = new TextObject("{=CC5Aa5VH}Your troops have returned from delivering {SHIP_NAME} to {PARTY_NAME}.");
		textObject.SetTextVariable("SHIP_NAME", sentTroops.ShipName);
		textObject.SetTextVariable("PARTY_NAME", sentTroops.PartyName);
		InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0f, 1f, 0f)));
	}

	private bool conversation_clan_member_manage_fleet_on_condition()
	{
		Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
		if (MobileParty.MainParty.MapEvent == null && oneToOneConversationHero != null && oneToOneConversationHero.Clan == Clan.PlayerClan && oneToOneConversationHero.PartyBelongedTo != null && oneToOneConversationHero.PartyBelongedTo.LeaderHero == oneToOneConversationHero && oneToOneConversationHero.PartyBelongedTo.MapEvent == null && !oneToOneConversationHero.PartyBelongedTo.IsCaravan && !oneToOneConversationHero.PartyBelongedTo.IsMilitia && !oneToOneConversationHero.PartyBelongedTo.IsVillager && !oneToOneConversationHero.PartyBelongedTo.IsPatrolParty)
		{
			if (oneToOneConversationHero.PartyBelongedTo.Ships.Count <= 0)
			{
				return MobileParty.MainParty.Ships.Count > 0;
			}
			return true;
		}
		return false;
	}

	private bool conversation_clan_member_manage_fleet_clickable_condition(out TextObject explanation)
	{
		Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
		if (oneToOneConversationHero != null && oneToOneConversationHero.PartyBelongedTo != null && !oneToOneConversationHero.CanHaveFleet && oneToOneConversationHero.PartyBelongedTo.Ships.Count == 0)
		{
			explanation = new TextObject("{=U4WWJ0bi}{PARTY_NAME} is not allowed to have ships.");
			explanation.SetTextVariable("PARTY_NAME", oneToOneConversationHero.PartyBelongedTo.Name);
			return false;
		}
		explanation = TextObject.GetEmpty();
		return true;
	}

	private void conversation_clan_member_manage_fleet_on_consequence()
	{
		PortStateHelper.OpenAsManageOtherFleet(Hero.OneToOneConversationHero.PartyBelongedTo.Party, OnManageOtherFleetDone);
	}

	private void OnManageOtherFleetDone()
	{
		Campaign.Current.ConversationManager.ContinueConversation();
	}

	public void SendShipToParty(Ship ship, MobileParty mobileParty)
	{
		TroopRoster troops = MobileParty.MainParty.MemberRoster.RemoveNumberOfNonHeroTroopsRandomly(Campaign.Current.Models.FleetManagementModel.MinimumTroopCountRequiredToSendShips);
		_sentTroops.Add(SentTroopsData.GetSentTroops(troops, Campaign.Current.Models.FleetManagementModel.GetReturnTimeForTroops(ship), ship.Name, mobileParty.Name));
		ChangeShipOwnerAction.ApplyByTransferring(mobileParty.Party, ship);
	}

	public void SendShipToClan(Ship ship, Clan clan)
	{
		bool doesPartyNeedShips;
		MobileParty clanPartyToGetAvailableShip = ShipHelper.GetClanPartyToGetAvailableShip(ship, clan, out doesPartyNeedShips);
		if (clanPartyToGetAvailableShip != null)
		{
			SendShipToParty(ship, clanPartyToGetAvailableShip);
		}
	}
}
