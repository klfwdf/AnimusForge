using System;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;

namespace SandBox.ViewModelCollection.Nameplate.NameplateNotifications.SettlementNotificationTypes;

public class ItemSoldNotificationItemVM : SettlementNotificationItemBaseVM
{
	private int _number;

	private PartyBase _heroParty;

	public ItemRosterElement Item { get; }

	public PartyBase ReceiverParty { get; }

	public PartyBase PayerParty { get; }

	public ItemSoldNotificationItemVM(Action<SettlementNotificationItemBaseVM> onRemove, PartyBase receiverParty, PartyBase payerParty, ItemRosterElement item, int number, int createdTick)
		: base(onRemove, createdTick)
	{
		Item = item;
		ReceiverParty = receiverParty;
		PayerParty = payerParty;
		_number = number;
		_heroParty = (receiverParty.IsSettlement ? payerParty : receiverParty);
		base.Text = SandBoxUIHelper.GetItemSoldNotificationText(Item, _number, _number < 0);
		base.CharacterName = ((_heroParty.LeaderHero != null) ? _heroParty.LeaderHero.Name.ToString() : _heroParty.Name.ToString());
		CharacterObject visualPartyLeader = PartyBaseHelper.GetVisualPartyLeader(_heroParty);
		base.CharacterVisual = new CharacterImageIdentifierVM(SandBoxUIHelper.GetCharacterCode(visualPartyLeader));
		base.RelationType = 0;
		base.CreatedTick = createdTick;
		if (_heroParty.LeaderHero != null)
		{
			base.RelationType = ((!_heroParty.LeaderHero.Clan.IsAtWarWith(Hero.MainHero.Clan)) ? 1 : (-1));
		}
	}

	public void AddNewTransaction(int amount)
	{
		_number += amount;
		if (_number == 0)
		{
			ExecuteRemove();
		}
		else
		{
			base.Text = SandBoxUIHelper.GetItemSoldNotificationText(Item, _number, _number < 0);
		}
	}
}
