using System.Collections.Generic;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace NavalDLC.GameComponents;

public class NavalDLCShipStatModel : ShipStatModel
{
	public override float GetShipFlagshipScore(Ship ship)
	{
		return GetShipTierf(ship) * MathF.Max(0.1f, ship.HitPoints / ship.MaxHitPoints);
	}

	private float GetShipTierf(Ship ship)
	{
		int num = ship.ShipHull.Value;
		foreach (KeyValuePair<string, ShipSlot> availableSlot in ship.ShipHull.AvailableSlots)
		{
			ShipUpgradePiece pieceAtSlot = ship.GetPieceAtSlot(availableSlot.Key);
			if (pieceAtSlot != null)
			{
				num = ((ship.ShipHull.Type != ShipHull.ShipType.Light) ? ((ship.ShipHull.Type != ShipHull.ShipType.Medium) ? (num + pieceAtSlot.HeavyValue) : (num + pieceAtSlot.MediumValue)) : (num + pieceAtSlot.LightValue));
			}
		}
		if (ship.Figurehead != null)
		{
			num += 15000;
		}
		return num;
	}
}
