using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Actions;

public static class GiveItemAction
{
	private static void ApplyInternal(Hero giver, Hero receiver, PartyBase giverParty, PartyBase receiverParty, in ItemRosterElement itemRosterElement)
	{
		bool flag = false;
		if (giver == null && receiver == null)
		{
			giverParty.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, -itemRosterElement.Amount);
			receiverParty.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, itemRosterElement.Amount);
			flag = true;
		}
		else if (giver.PartyBelongedTo != null && receiver.PartyBelongedTo != null)
		{
			giver.PartyBelongedTo.Party.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, -itemRosterElement.Amount);
			receiver.PartyBelongedTo.Party.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, -itemRosterElement.Amount);
			flag = true;
		}
		if (flag)
		{
			CampaignEventDispatcher.Instance.OnHeroOrPartyGaveItem((giver, giverParty), (receiver, receiverParty), itemRosterElement, showNotification: true);
		}
	}

	public static void ApplyForHeroes(Hero giver, Hero receiver, in ItemRosterElement itemRosterElement)
	{
		ApplyInternal(giver, receiver, null, null, in itemRosterElement);
	}

	public static void ApplyForParties(PartyBase giverParty, PartyBase receiverParty, in ItemRosterElement itemRosterElement)
	{
		ApplyInternal(null, null, giverParty, receiverParty, in itemRosterElement);
	}
}
