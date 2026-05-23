using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Actions;

public static class RepairShipAction
{
	private static void ApplyInternal(Ship ship, float newHitpoints, Settlement repairPort = null)
	{
		SkillLevelingManager.OnShipRepaired(ship, newHitpoints - ship.HitPoints);
		ship.HitPoints = newHitpoints;
		CampaignEventDispatcher.Instance.OnShipRepaired(ship, repairPort);
	}

	public static void Apply(Ship ship, Settlement repairPort)
	{
		PartyBase owner = ship.Owner;
		if (owner.IsMobile && (owner.MobileParty.IsCaravan || owner.MobileParty.IsLordParty))
		{
			int amount = (int)Campaign.Current.Models.ShipCostModel.GetShipRepairCost(ship, owner);
			GiveGoldAction.ApplyForPartyToSettlement(owner, repairPort, amount);
		}
		ApplyInternal(ship, ship.MaxHitPoints, repairPort);
	}

	public static void ApplyForFree(Ship ship)
	{
		ApplyInternal(ship, ship.MaxHitPoints);
	}

	public static void ApplyForBanditShip(Ship ship)
	{
		if (ship.HitPoints < ship.MaxHitPoints * 0.8f)
		{
			ApplyInternal(ship, ship.MaxHitPoints * 0.8f);
		}
	}
}
