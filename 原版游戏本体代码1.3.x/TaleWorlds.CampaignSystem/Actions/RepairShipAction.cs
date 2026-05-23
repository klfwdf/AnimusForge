using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004C1 RID: 1217
	public static class RepairShipAction
	{
		// Token: 0x06004A0D RID: 18957 RVA: 0x00174C33 File Offset: 0x00172E33
		private static void ApplyInternal(Ship ship, float newHitpoints, Settlement repairPort = null)
		{
			SkillLevelingManager.OnShipRepaired(ship, newHitpoints - ship.HitPoints);
			ship.HitPoints = newHitpoints;
			CampaignEventDispatcher.Instance.OnShipRepaired(ship, repairPort);
		}

		// Token: 0x06004A0E RID: 18958 RVA: 0x00174C58 File Offset: 0x00172E58
		public static void Apply(Ship ship, Settlement repairPort)
		{
			PartyBase owner = ship.Owner;
			if (owner.IsMobile && (owner.MobileParty.IsCaravan || owner.MobileParty.IsLordParty))
			{
				int amount = (int)Campaign.Current.Models.ShipCostModel.GetShipRepairCost(ship, owner);
				GiveGoldAction.ApplyForPartyToSettlement(owner, repairPort, amount, false);
			}
			RepairShipAction.ApplyInternal(ship, ship.MaxHitPoints, repairPort);
		}

		// Token: 0x06004A0F RID: 18959 RVA: 0x00174CBC File Offset: 0x00172EBC
		public static void ApplyForFree(Ship ship)
		{
			RepairShipAction.ApplyInternal(ship, ship.MaxHitPoints, null);
		}

		// Token: 0x06004A10 RID: 18960 RVA: 0x00174CCB File Offset: 0x00172ECB
		public static void ApplyForBanditShip(Ship ship)
		{
			if (ship.HitPoints < ship.MaxHitPoints * 0.8f)
			{
				RepairShipAction.ApplyInternal(ship, ship.MaxHitPoints * 0.8f, null);
			}
		}
	}
}
