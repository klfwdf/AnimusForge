using System.Collections.Generic;
using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace NavalDLC.GameComponents;

public class NavalDLCShipCostModel : ShipCostModel
{
	private const float BuyPenalty = 1.5f;

	private const float RepairPenalty = 0.25f;

	private const float SellPenalty = 0.3f;

	private const float UpgradePiecePenalty = 0.3f;

	private const float AIClansShipValueDiscountRatio = 0.01f;

	private const float RoyalNavyPrerogativeMultiplier = 0.9f;

	public override float GetShipTradeValue(Ship ship, PartyBase seller, PartyBase buyer)
	{
		bool applyAiDiscount = buyer != null && buyer.IsMobile && buyer.MobileParty.ActualClan != Clan.PlayerClan && seller.IsSettlement;
		float num = GetShipBaseValue(ship, applyAiDiscount, seller, buyer) * 1.5f;
		if (buyer != null)
		{
			Clan clan = null;
			Kingdom kingdom = null;
			if (buyer.IsMobile)
			{
				clan = buyer.MobileParty.ActualClan;
				kingdom = clan?.Kingdom;
			}
			else if (buyer.IsSettlement)
			{
				clan = buyer.Settlement.OwnerClan;
				kingdom = clan?.Kingdom;
			}
			if (kingdom != null)
			{
				if (kingdom.HasPolicy(NavalPolicies.RoyalNavyPrerogative) && kingdom.RulingClan == clan)
				{
					num *= 0.9f;
				}
				if (seller.IsSettlement && seller.Settlement.OwnerClan.Kingdom != null && seller.Settlement.OwnerClan.Kingdom == kingdom && kingdom.HasPolicy(NavalPolicies.ArsenalDepositoryAct))
				{
					num *= 0.85f;
				}
			}
			if (seller.IsMobile && buyer.IsSettlement)
			{
				num = num * 0.3f - Campaign.Current.Models.ShipCostModel.GetShipRepairCost(ship, seller);
			}
		}
		return num;
	}

	private static float GetShipBaseValue(Ship ship, bool applyAiDiscount, PartyBase owner, PartyBase buyer)
	{
		float num = ship.ShipHull.Value;
		if (applyAiDiscount)
		{
			num *= 0.01f;
		}
		int num2 = 0;
		foreach (KeyValuePair<string, ShipSlot> availableSlot in ship.ShipHull.AvailableSlots)
		{
			int num3 = 0;
			ShipUpgradePiece pieceAtSlot = ship.GetPieceAtSlot(availableSlot.Key);
			if (pieceAtSlot != null)
			{
				num3 = GetShipUpgradePieceValueInternal(ship, pieceAtSlot, owner, buyer);
			}
			if (ship.UnlockedUpgradePieces != null)
			{
				for (int i = 0; i < ship.UnlockedUpgradePieces.Count; i++)
				{
					ShipUpgradePiece shipUpgradePiece = ship.UnlockedUpgradePieces[i];
					if (shipUpgradePiece.DoesPieceMatchSlot(availableSlot.Value))
					{
						int shipUpgradePieceValueInternal = GetShipUpgradePieceValueInternal(ship, shipUpgradePiece, owner, buyer);
						if (shipUpgradePieceValueInternal > num3)
						{
							num3 = shipUpgradePieceValueInternal;
						}
					}
				}
			}
			num2 += num3;
		}
		return num + (float)num2 * 0.3f;
	}

	public override float GetShipRepairCost(Ship ship, PartyBase owner)
	{
		float num = (ship.MaxHitPoints - ship.HitPoints) / ship.MaxHitPoints;
		bool applyAiDiscount = owner?.MobileParty?.ActualClan != Clan.PlayerClan;
		ExplainedNumber stat = new ExplainedNumber(GetShipBaseValue(ship, applyAiDiscount, owner, owner) * num * 0.25f);
		if (owner != null && owner.MobileParty != null)
		{
			PerkHelper.AddPerkBonusForParty(NavalPerks.Boatswain.MerchantPrince, owner.MobileParty, isPrimaryBonus: true, ref stat);
		}
		return stat.ResultNumber;
	}

	public override int GetShipUpgradePieceCost(Ship ship, ShipUpgradePiece piece, PartyBase owner)
	{
		MBReadOnlyList<ShipUpgradePiece> unlockedUpgradePieces = ship.UnlockedUpgradePieces;
		if (unlockedUpgradePieces != null && unlockedUpgradePieces.Contains(piece))
		{
			return 0;
		}
		foreach (KeyValuePair<string, ShipSlot> availableSlot in ship.ShipHull.AvailableSlots)
		{
			if (ship.GetPieceAtSlot(availableSlot.Key) == piece)
			{
				return 0;
			}
		}
		return GetShipUpgradePieceValueInternal(ship, piece, owner, owner);
	}

	private static int GetShipUpgradePieceValueInternal(Ship ship, ShipUpgradePiece piece, PartyBase owner, PartyBase buyer)
	{
		float num = piece.LightValue;
		if (ship.ShipHull.Type == ShipHull.ShipType.Medium)
		{
			num = piece.MediumValue;
		}
		else if (ship.ShipHull.Type == ShipHull.ShipType.Heavy)
		{
			num = piece.HeavyValue;
		}
		if (owner != null)
		{
			if (owner.IsMobile)
			{
				ExplainedNumber stat = new ExplainedNumber(num);
				PerkHelper.AddPerkBonusForParty(NavalPerks.Boatswain.MasterShipwright, owner.MobileParty, isPrimaryBonus: true, ref stat);
				num = stat.ResultNumber;
			}
			Clan clan = owner.MobileParty?.ActualClan;
			Kingdom kingdom = clan?.Kingdom;
			if (kingdom != null && kingdom.RulingClan == clan && kingdom.HasPolicy(NavalPolicies.RoyalNavyPrerogative))
			{
				num *= 0.9f;
			}
		}
		if (owner?.MobileParty?.ActualClan != Clan.PlayerClan && buyer?.MobileParty?.ActualClan != Clan.PlayerClan)
		{
			num *= 0.01f;
		}
		return MathF.Round(num);
	}

	public override float GetShipSellingPenalty()
	{
		return 0.3f;
	}
}
