using System.Collections.Generic;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace NavalDLC.GameComponents;

public class NavalDLCCampaignShipParametersModel : CampaignShipParametersModel
{
	public override float GetDefaultCombatFactor(ShipHull shipHull)
	{
		switch (shipHull.Type)
		{
		case ShipHull.ShipType.Light:
			return 1f;
		case ShipHull.ShipType.Medium:
			return 1.2f;
		case ShipHull.ShipType.Heavy:
			return 1.4f;
		default:
			Debug.FailedAssert("Unhandled ship type", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\GameComponents\\NavalDLCCampaignShipParametersModel.cs", "GetDefaultCombatFactor", 25);
			return 1f;
		}
	}

	public override float GetShipSizeWeatherFactor(ShipHull shipHull)
	{
		switch (shipHull.Type)
		{
		case ShipHull.ShipType.Light:
			return 35f;
		case ShipHull.ShipType.Medium:
			return 70f;
		case ShipHull.ShipType.Heavy:
			return 105f;
		default:
			Debug.FailedAssert("Unhandled ship type", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\GameComponents\\NavalDLCCampaignShipParametersModel.cs", "GetShipSizeWeatherFactor", 41);
			return 0.1f;
		}
	}

	public override float GetCampaignSpeedBonusFactor(Ship ship)
	{
		float num = 0f;
		foreach (KeyValuePair<string, ShipSlot> availableSlot in ship.ShipHull.AvailableSlots)
		{
			ShipUpgradePiece pieceAtSlot = ship.GetPieceAtSlot(availableSlot.Key);
			if (pieceAtSlot != null && pieceAtSlot.CampaignSpeedBonusMultiplier > 0f)
			{
				num += pieceAtSlot.CampaignSpeedBonusMultiplier;
			}
		}
		if (ship.Figurehead != null && ship.Figurehead == DefaultFigureheads.Horse)
		{
			num += ship.Figurehead.EffectAmount;
		}
		return num;
	}

	public override float GetCrewCapacityBonusFactor(Ship ship)
	{
		float num = 0f;
		foreach (KeyValuePair<string, ShipSlot> availableSlot in ship.ShipHull.AvailableSlots)
		{
			ShipUpgradePiece pieceAtSlot = ship.GetPieceAtSlot(availableSlot.Key);
			if (pieceAtSlot != null)
			{
				num += pieceAtSlot.CrewCapacityBonusMultiplier;
			}
		}
		return num;
	}

	public override float GetShipWeightFactor(Ship ship)
	{
		float num = 0f;
		foreach (KeyValuePair<string, ShipSlot> availableSlot in ship.ShipHull.AvailableSlots)
		{
			ShipUpgradePiece pieceAtSlot = ship.GetPieceAtSlot(availableSlot.Key);
			if (pieceAtSlot != null)
			{
				num += pieceAtSlot.ShipWeightBonusMultiplier;
			}
		}
		return num;
	}

	public override float GetForwardDragFactor(Ship ship)
	{
		float num = 0f;
		foreach (KeyValuePair<string, ShipSlot> availableSlot in ship.ShipHull.AvailableSlots)
		{
			ShipUpgradePiece pieceAtSlot = ship.GetPieceAtSlot(availableSlot.Key);
			if (pieceAtSlot != null)
			{
				num += pieceAtSlot.DecreaseForwardDragMultiplier;
			}
		}
		return 0f - num;
	}

	public override float GetCrewShieldHitPointsFactor(Ship ship)
	{
		float num = 0f;
		foreach (KeyValuePair<string, ShipSlot> availableSlot in ship.ShipHull.AvailableSlots)
		{
			ShipUpgradePiece pieceAtSlot = ship.GetPieceAtSlot(availableSlot.Key);
			if (pieceAtSlot != null)
			{
				num += pieceAtSlot.CrewShieldHitPointsBonusMultiplier;
			}
		}
		if (ship.Figurehead == DefaultFigureheads.Turtle)
		{
			num += ship.Figurehead.EffectAmount;
		}
		return num;
	}

	public override int GetAdditionalAmmoBonus(Ship ship)
	{
		int num = 0;
		foreach (KeyValuePair<string, ShipSlot> availableSlot in ship.ShipHull.AvailableSlots)
		{
			ShipUpgradePiece pieceAtSlot = ship.GetPieceAtSlot(availableSlot.Key);
			if (pieceAtSlot != null)
			{
				num += pieceAtSlot.AdditionalAmmoBonus;
			}
		}
		return num;
	}

	public override float GetMaxOarPowerFactor(Ship ship)
	{
		float num = 0f;
		foreach (KeyValuePair<string, ShipSlot> availableSlot in ship.ShipHull.AvailableSlots)
		{
			ShipUpgradePiece pieceAtSlot = ship.GetPieceAtSlot(availableSlot.Key);
			if (pieceAtSlot != null)
			{
				num += pieceAtSlot.MaxOarPowerBonusMultiplier;
			}
		}
		return num;
	}

	public override float GetMaxOarForceFactor(Ship ship)
	{
		float num = 0f;
		foreach (KeyValuePair<string, ShipSlot> availableSlot in ship.ShipHull.AvailableSlots)
		{
			ShipUpgradePiece pieceAtSlot = ship.GetPieceAtSlot(availableSlot.Key);
			if (pieceAtSlot != null)
			{
				num += pieceAtSlot.MaxOarForceBonusMultiplier;
			}
		}
		if (ship.Figurehead == DefaultFigureheads.Deer)
		{
			num += ship.Figurehead.EffectAmount;
		}
		return num;
	}

	public override float GetSailForceFactor(Ship ship)
	{
		float num = 0f;
		foreach (KeyValuePair<string, ShipSlot> availableSlot in ship.ShipHull.AvailableSlots)
		{
			ShipUpgradePiece pieceAtSlot = ship.GetPieceAtSlot(availableSlot.Key);
			if (pieceAtSlot != null)
			{
				num += pieceAtSlot.SailForceBonusMultiplier;
			}
		}
		if (ship.Figurehead == DefaultFigureheads.Swan)
		{
			num += ship.Figurehead.EffectAmount;
		}
		return num;
	}

	public override float GetCrewMeleeDamageFactor(Ship ship)
	{
		float num = 0f;
		foreach (KeyValuePair<string, ShipSlot> availableSlot in ship.ShipHull.AvailableSlots)
		{
			ShipUpgradePiece pieceAtSlot = ship.GetPieceAtSlot(availableSlot.Key);
			if (pieceAtSlot != null)
			{
				num += pieceAtSlot.CrewMeleeDamageBonusMultiplier;
			}
		}
		return num;
	}

	public override int GetAdditionalArcherQuivers(Ship ship)
	{
		int num = 0;
		foreach (KeyValuePair<string, ShipSlot> availableSlot in ship.ShipHull.AvailableSlots)
		{
			ShipUpgradePiece pieceAtSlot = ship.GetPieceAtSlot(availableSlot.Key);
			if (pieceAtSlot != null)
			{
				num += pieceAtSlot.ArcherQuiverBonus;
			}
		}
		return num;
	}

	public override int GetAdditionalThrowingWeaponStack(Ship ship)
	{
		int num = 0;
		foreach (KeyValuePair<string, ShipSlot> availableSlot in ship.ShipHull.AvailableSlots)
		{
			ShipUpgradePiece pieceAtSlot = ship.GetPieceAtSlot(availableSlot.Key);
			if (pieceAtSlot != null)
			{
				num += pieceAtSlot.ThrowingWeaponStackBonus;
			}
		}
		return num;
	}

	public override float GetSailRotationSpeedFactor(Ship ship)
	{
		float num = 0f;
		foreach (KeyValuePair<string, ShipSlot> availableSlot in ship.ShipHull.AvailableSlots)
		{
			ShipUpgradePiece pieceAtSlot = ship.GetPieceAtSlot(availableSlot.Key);
			if (pieceAtSlot != null)
			{
				num += pieceAtSlot.SailRotationSpeedBonusMultiplier;
			}
		}
		return num;
	}

	public override float GetFurlUnfurlSpeedFactor(Ship ship)
	{
		float num = 0f;
		foreach (KeyValuePair<string, ShipSlot> availableSlot in ship.ShipHull.AvailableSlots)
		{
			ShipUpgradePiece pieceAtSlot = ship.GetPieceAtSlot(availableSlot.Key);
			if (pieceAtSlot != null)
			{
				num += pieceAtSlot.FurlUnfurlSpeedBonusMultiplier;
			}
		}
		return num;
	}
}
