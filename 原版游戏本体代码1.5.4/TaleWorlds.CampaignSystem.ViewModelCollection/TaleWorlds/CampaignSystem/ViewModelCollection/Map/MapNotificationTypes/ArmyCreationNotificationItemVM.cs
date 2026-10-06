using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class ArmyCreationNotificationItemVM : MapNotificationItemBaseVM
{
	public Army Army { get; }

	public ArmyCreationNotificationItemVM(ArmyCreationMapNotification data)
		: base(data)
	{
		Army = data.CreatedArmy;
		base.NotificationIdentifier = "armycreation";
		_onInspect = delegate
		{
			GoToMapPosition(Army?.LeaderParty?.Position ?? MobileParty.MainParty.Position);
		};
		CampaignEvents.OnPartyJoinedArmyEvent.AddNonSerializedListener(this, OnPartyJoinedArmy);
		CampaignEvents.ArmyDispersed.AddNonSerializedListener(this, OnArmyDispersed);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
	{
		if (clan == MobileParty.MainParty.ActualClan && oldKingdom != newKingdom)
		{
			ExecuteRemove();
		}
	}

	private void OnArmyDispersed(Army arg1, Army.ArmyDispersionReason arg2, bool isPlayersArmy)
	{
		if (arg1 == Army)
		{
			ExecuteRemove();
		}
	}

	private void OnPartyJoinedArmy(MobileParty party)
	{
		if (party == MobileParty.MainParty && party.Army == Army)
		{
			ExecuteRemove();
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CampaignEvents.OnPartyJoinedArmyEvent.ClearListeners(this);
		CampaignEvents.ArmyDispersed.ClearListeners(this);
		CampaignEvents.OnClanChangedKingdomEvent.ClearListeners(this);
	}
}
