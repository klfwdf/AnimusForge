using System;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultTradeItemPriceFactorModel : TradeItemPriceFactorModel
{
	private const float MinPriceFactor = 0.1f;

	private const float MaxPriceFactor = 10f;

	private const float MinPriceFactorNonTrade = 0.8f;

	private const float MaxPriceFactorNonTrade = 1.3f;

	private const float HighTradePenaltyBaseValue = 1.5f;

	private const float PackAnimalTradePenalty = 0.8f;

	private const float MountTradePenalty = 0.8f;

	public override float GetTradePenalty(ItemObject item, MobileParty clientParty, PartyBase merchant, bool isSelling, float inStore, float supply, float demand)
	{
		Settlement settlement = merchant?.Settlement;
		float num = 0.06f;
		bool flag = clientParty?.IsCaravan ?? false;
		bool num2 = merchant != null && merchant.MobileParty?.IsCaravan == true;
		if (clientParty != null && merchant != null && clientParty.MapFaction.IsAtWarWith(merchant.MapFaction))
		{
			num += 0.5f;
		}
		if (!item.IsTradeGood && !item.IsAnimal && !item.HasHorseComponent && !flag && isSelling)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(1.5f + Math.Max(0f, item.Tierf - 1f) * 0.25f);
			if (item.IsCraftedWeapon && item.IsCraftedByPlayer && clientParty != null && clientParty.HasPerk(DefaultPerks.Crafting.ArtisanSmith))
			{
				explainedNumber.AddFactor(DefaultPerks.Crafting.ArtisanSmith.PrimaryBonus);
			}
			num += explainedNumber.ResultNumber;
		}
		if (item.HasHorseComponent && item.HorseComponent.IsPackAnimal && !flag && isSelling)
		{
			num += 0.8f;
		}
		if (item.HasHorseComponent && item.HorseComponent.IsMount && !flag && isSelling)
		{
			num += 0.8f;
		}
		if (settlement != null && settlement.IsVillage)
		{
			num += (isSelling ? 1f : 0.1f);
		}
		if (num2)
		{
			if (item.ItemCategory == DefaultItemCategories.PackAnimal && !isSelling)
			{
				num += 2f;
			}
			num += (isSelling ? 1f : 0.1f);
		}
		bool flag2 = clientParty == null;
		if (flag)
		{
			num *= 0.5f;
		}
		else if (flag2)
		{
			num *= 0.2f;
		}
		float num3 = ((clientParty != null) ? Campaign.Current.Models.PartyTradeModel.GetTradePenaltyFactor(clientParty) : 1f);
		num *= num3;
		ExplainedNumber stat = new ExplainedNumber(num);
		if (clientParty != null)
		{
			if (settlement != null && clientParty.MapFaction == settlement.MapFaction)
			{
				if (settlement.IsVillage)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.VillageNetwork, clientParty, isPrimaryBonus: true, ref stat);
				}
				else if (settlement.IsTown)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.RumourNetwork, clientParty, isPrimaryBonus: true, ref stat);
				}
			}
			if (item.IsTradeGood)
			{
				if (clientParty.HasPerk(DefaultPerks.Trade.WholeSeller) && isSelling)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Trade.WholeSeller, clientParty, isPrimaryBonus: true, ref stat);
				}
				if (isSelling && item.IsFood && clientParty.LeaderHero != null)
				{
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Trade.GranaryAccountant, clientParty.LeaderHero.CharacterObject, isPrimaryBonus: true, ref stat);
				}
			}
			else if (!item.IsTradeGood && clientParty.HasPerk(DefaultPerks.Trade.Appraiser) && isSelling)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Trade.Appraiser, clientParty, isPrimaryBonus: true, ref stat);
			}
			if (PartyBaseHelper.HasFeat(clientParty.Party, DefaultCulturalFeats.AseraiTraderFeat))
			{
				stat.AddFactor(-0.1f);
			}
			if (item.WeaponComponent != null && isSelling)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Roguery.ArmsDealer, clientParty, isPrimaryBonus: true, ref stat);
			}
			if (!isSelling && item.IsFood)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Trade.InsurancePlans, clientParty, isPrimaryBonus: false, ref stat);
			}
			if (item.HorseComponent != null && item.HorseComponent.IsPackAnimal && clientParty.HasPerk(DefaultPerks.Steward.ArenicosMules, checkSecondaryRole: true))
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Steward.ArenicosMules, clientParty, isPrimaryBonus: false, ref stat);
			}
			if (item.IsMountable)
			{
				if (clientParty.HasPerk(DefaultPerks.Riding.DeeperSacks, checkSecondaryRole: true))
				{
					stat.AddFactor(DefaultPerks.Riding.DeeperSacks.SecondaryBonus, DefaultPerks.Riding.DeeperSacks.Name);
				}
				if (clientParty.LeaderHero != null && clientParty.LeaderHero.GetPerkValue(DefaultPerks.Steward.ArenicosHorses))
				{
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Steward.ArenicosHorses, clientParty.LeaderHero.CharacterObject, isPrimaryBonus: false, ref stat);
				}
			}
			if (clientParty.IsMainParty && Hero.MainHero.GetPerkValue(DefaultPerks.Roguery.SmugglerConnections) && merchant?.MapFaction != null && merchant.MapFaction.MainHeroCrimeRating > 0f)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Roguery.SmugglerConnections, clientParty, isPrimaryBonus: false, ref stat);
			}
			if (!isSelling && merchant != null && merchant.IsSettlement && merchant.Settlement.IsVillage && clientParty.HasPerk(DefaultPerks.Trade.DistributedGoods, checkSecondaryRole: true))
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Trade.DistributedGoods, clientParty, isPrimaryBonus: false, ref stat);
			}
			if (isSelling && item.HasHorseComponent && clientParty.HasPerk(DefaultPerks.Trade.LocalConnection, checkSecondaryRole: true))
			{
				stat.AddFactor(DefaultPerks.Trade.LocalConnection.SecondaryBonus, DefaultPerks.Trade.LocalConnection.Name);
			}
			if (isSelling && (item.ItemCategory == DefaultItemCategories.Pottery || item.ItemCategory == DefaultItemCategories.Tools || item.ItemCategory == DefaultItemCategories.Jewelry || item.ItemCategory == DefaultItemCategories.Cotton) && clientParty.LeaderHero != null)
			{
				PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Trade.TradeyardForeman, clientParty.LeaderHero.CharacterObject, isPrimaryBonus: true, ref stat);
			}
			if (!isSelling && (item.ItemCategory == DefaultItemCategories.Clay || item.ItemCategory == DefaultItemCategories.Iron || item.ItemCategory == DefaultItemCategories.Silver || item.ItemCategory == DefaultItemCategories.Cotton) && clientParty.HasPerk(DefaultPerks.Trade.RapidDevelopment))
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Trade.RapidDevelopment, clientParty, isPrimaryBonus: false, ref stat);
			}
		}
		return stat.ResultNumber;
	}

	private float GetPriceFactor(ItemObject item, MobileParty tradingParty, PartyBase merchant, float inStoreValue, float supply, float demand, bool isSelling)
	{
		float basePriceFactor = GetBasePriceFactor(item.GetItemCategory(), inStoreValue, supply, demand, isSelling, item.Value);
		float tradePenalty = GetTradePenalty(item, tradingParty, merchant, isSelling, inStoreValue, supply, demand);
		if (!isSelling)
		{
			return basePriceFactor * (1f + tradePenalty);
		}
		return basePriceFactor * 1f / (1f + tradePenalty);
	}

	public override float GetBasePriceFactor(ItemCategory itemCategory, float inStoreValue, float supply, float demand, bool isSelling, int transferValue)
	{
		if (isSelling)
		{
			inStoreValue += (float)transferValue;
		}
		float value = TaleWorlds.Library.MathF.Pow(demand / (0.1f * supply + inStoreValue * 0.04f + 2f), itemCategory.IsAnimal ? 0.3f : 0.6f);
		if (itemCategory.IsTradeGood)
		{
			return TaleWorlds.Library.MathF.Clamp(value, 0.1f, 10f);
		}
		return TaleWorlds.Library.MathF.Clamp(value, 0.8f, 1.3f);
	}

	public override int GetPrice(EquipmentElement itemRosterElement, MobileParty clientParty, PartyBase merchant, bool isSelling, float inStoreValue, float supply, float demand)
	{
		float priceFactor = GetPriceFactor(itemRosterElement.Item, clientParty, merchant, inStoreValue, supply, demand, isSelling);
		float f = (float)itemRosterElement.ItemValue * priceFactor;
		int num = (isSelling ? TaleWorlds.Library.MathF.Floor(f) : TaleWorlds.Library.MathF.Ceiling(f));
		if (!isSelling && merchant?.MobileParty != null && merchant.MobileParty.IsCaravan && clientParty.HasPerk(DefaultPerks.Trade.SilverTongue, checkSecondaryRole: true))
		{
			num = TaleWorlds.Library.MathF.Ceiling((float)num * (1f - DefaultPerks.Trade.SilverTongue.SecondaryBonus));
		}
		return TaleWorlds.Library.MathF.Max(num, 1);
	}

	public override int GetTheoreticalMaxItemMarketValue(ItemObject item)
	{
		if (item.IsTradeGood || item.IsAnimal)
		{
			return TaleWorlds.Library.MathF.Round((float)item.Value * 10f);
		}
		return TaleWorlds.Library.MathF.Round((float)item.Value * 1.3f);
	}
}
