using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace AnimusForge;

public partial class SiegeAiInterventionBehavior
{
	private static void AwardGoldToPlayer(int amount, string source)
	{
		if (amount <= 0 || Hero.MainHero == null)
		{
			return;
		}
		try
		{
			int before = Hero.MainHero.Gold;
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, amount, disableNotification: true);
			if (Hero.MainHero.Gold < before + amount)
			{
				Hero.MainHero.ChangeHeroGold(before + amount - Hero.MainHero.Gold);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "AwardGoldToPlayer failed (" + source + "): " + ex.Message);
			try
			{
				Hero.MainHero.ChangeHeroGold(amount);
			}
			catch
			{
			}
		}
	}

	private static void TransferHeroGoldToPlayer(Hero source, int amount)
	{
		GiveGoldAction.ApplyBetweenCharacters(source, Hero.MainHero, amount, disableNotification: true);
	}

	private static void RestoreItemStackToPlayerParty(ItemRoster playerRoster, ItemObject item, int amount)
	{
		playerRoster.AddToCounts(item, amount);
	}

	private static void MoveItemStackToPendingLoot(
		ItemRoster sourceRoster,
		ItemRoster pendingLootRoster,
		EquipmentElement equipmentElement,
		int amount)
	{
		sourceRoster.AddToCounts(equipmentElement, -amount);
		pendingLootRoster.AddToCounts(equipmentElement, amount);
	}
}
