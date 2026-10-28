using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace NavalDLC.GameComponents;

public class NavalDLCShipLimitModel : PartyShipLimitModel
{
	private const int LordPartyShipBaseLimit = 3;

	private const int ConvoyPartyShipBaseLimit = 3;

	private const int BanditPartyShipBaseLimit = 3;

	private const float MustSellPriorityValue = float.MaxValue;

	private const float MustDiscardPriorityValue = float.MinValue;

	public override int GetIdealShipNumber(MobileParty mobileParty)
	{
		if (mobileParty.IsCaravan)
		{
			return 3;
		}
		if (mobileParty.IsLordParty)
		{
			return 3;
		}
		if (mobileParty.IsBandit)
		{
			return 3;
		}
		Debug.FailedAssert("Unhandled case", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\GameComponents\\NavalDLCShipLimitModel.cs", "GetIdealShipNumber", 34);
		return base.BaseModel.GetIdealShipNumber(mobileParty);
	}

	public override int GetIdealShipNumber(Clan clan)
	{
		return clan.WarPartyComponents.Count * 3;
	}

	public override float GetShipPriority(MobileParty mobileParty, Ship ship, bool isSelling)
	{
		if (mobileParty.IsBandit)
		{
			return MBMath.ClampFloat(ship.HitPoints / ship.MaxHitPoints, 0f, 1f);
		}
		if (mobileParty.IsCaravan)
		{
			if (ship.ShipHull.Type == ShipHull.ShipType.Heavy || (ship.ShipHull.Type == ShipHull.ShipType.Medium && !mobileParty.CaravanPartyComponent.IsElite))
			{
				if (!isSelling)
				{
					return float.MinValue;
				}
				return float.MaxValue;
			}
			float baseSpeed = ship.ShipHull.BaseSpeed;
			float inventoryCapacity = ship.InventoryCapacity;
			float num = ship.SeaWorthiness;
			float maxHitPoints = ship.MaxHitPoints;
			return inventoryCapacity * 2f + baseSpeed * 10f + num + maxHitPoints * 0.1f;
		}
		return 1f;
	}
}
