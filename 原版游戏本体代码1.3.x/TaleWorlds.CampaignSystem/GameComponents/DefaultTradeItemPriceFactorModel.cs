using System;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200015D RID: 349
	public class DefaultTradeItemPriceFactorModel : TradeItemPriceFactorModel
	{
		// Token: 0x06001AAF RID: 6831 RVA: 0x0008871C File Offset: 0x0008691C
		public override float GetTradePenalty(ItemObject item, MobileParty clientParty, PartyBase merchant, bool isSelling, float inStore, float supply, float demand)
		{
			Settlement settlement = ((merchant != null) ? merchant.Settlement : null);
			float num = 0.06f;
			bool flag = clientParty != null && clientParty.IsCaravan;
			bool flag2;
			if (merchant == null)
			{
				flag2 = false;
			}
			else
			{
				MobileParty mobileParty = merchant.MobileParty;
				bool? flag3 = ((mobileParty != null) ? new bool?(mobileParty.IsCaravan) : null);
				bool flag4 = true;
				flag2 = (flag3.GetValueOrDefault() == flag4) & (flag3 != null);
			}
			if (clientParty != null && merchant != null && clientParty.MapFaction.IsAtWarWith(merchant.MapFaction))
			{
				num += 0.5f;
			}
			if (!item.IsTradeGood && !item.IsAnimal && !item.HasHorseComponent && !flag && isSelling)
			{
				ExplainedNumber explainedNumber = new ExplainedNumber(1.5f + Math.Max(0f, item.Tierf - 1f) * 0.25f, false, null);
				if (item.IsCraftedWeapon && item.IsCraftedByPlayer && clientParty != null && clientParty.HasPerk(DefaultPerks.Crafting.ArtisanSmith, false))
				{
					explainedNumber.AddFactor(DefaultPerks.Crafting.ArtisanSmith.PrimaryBonus, null);
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
			if (flag2)
			{
				if (item.ItemCategory == DefaultItemCategories.PackAnimal && !isSelling)
				{
					num += 2f;
				}
				num += (isSelling ? 1f : 0.1f);
			}
			bool flag5 = clientParty == null;
			if (flag)
			{
				num *= 0.5f;
			}
			else if (flag5)
			{
				num *= 0.2f;
			}
			float num2 = ((clientParty != null) ? Campaign.Current.Models.PartyTradeModel.GetTradePenaltyFactor(clientParty) : 1f);
			num *= num2;
			ExplainedNumber explainedNumber2 = new ExplainedNumber(num, false, null);
			if (clientParty != null)
			{
				if (settlement != null && clientParty.MapFaction == settlement.MapFaction)
				{
					if (settlement.IsVillage)
					{
						PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.VillageNetwork, clientParty, true, ref explainedNumber2, false);
					}
					else if (settlement.IsTown)
					{
						PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.RumourNetwork, clientParty, true, ref explainedNumber2, false);
					}
				}
				if (item.IsTradeGood)
				{
					if (clientParty.HasPerk(DefaultPerks.Trade.WholeSeller, false) && isSelling)
					{
						PerkHelper.AddPerkBonusForParty(DefaultPerks.Trade.WholeSeller, clientParty, true, ref explainedNumber2, false);
					}
					if (isSelling && item.IsFood && clientParty.LeaderHero != null)
					{
						PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Trade.GranaryAccountant, clientParty.LeaderHero.CharacterObject, true, ref explainedNumber2, false);
					}
				}
				else if (!item.IsTradeGood && (clientParty.HasPerk(DefaultPerks.Trade.Appraiser, false) && isSelling))
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Trade.Appraiser, clientParty, true, ref explainedNumber2, false);
				}
				if (PartyBaseHelper.HasFeat(clientParty.Party, DefaultCulturalFeats.AseraiTraderFeat))
				{
					explainedNumber2.AddFactor(-0.1f, null);
				}
				if (item.WeaponComponent != null && isSelling)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Roguery.ArmsDealer, clientParty, true, ref explainedNumber2, false);
				}
				if (!isSelling && item.IsFood)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Trade.InsurancePlans, clientParty, false, ref explainedNumber2, false);
				}
				if (item.HorseComponent != null && item.HorseComponent.IsPackAnimal && clientParty.HasPerk(DefaultPerks.Steward.ArenicosMules, true))
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Steward.ArenicosMules, clientParty, false, ref explainedNumber2, false);
				}
				if (item.IsMountable)
				{
					if (clientParty.HasPerk(DefaultPerks.Riding.DeeperSacks, true))
					{
						explainedNumber2.AddFactor(DefaultPerks.Riding.DeeperSacks.SecondaryBonus, DefaultPerks.Riding.DeeperSacks.Name);
					}
					if (clientParty.LeaderHero != null && clientParty.LeaderHero.GetPerkValue(DefaultPerks.Steward.ArenicosHorses))
					{
						PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Steward.ArenicosHorses, clientParty.LeaderHero.CharacterObject, false, ref explainedNumber2, false);
					}
				}
				if (clientParty.IsMainParty && Hero.MainHero.GetPerkValue(DefaultPerks.Roguery.SmugglerConnections) && ((merchant != null) ? merchant.MapFaction : null) != null && merchant.MapFaction.MainHeroCrimeRating > 0f)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Roguery.SmugglerConnections, clientParty, false, ref explainedNumber2, false);
				}
				if (!isSelling && merchant != null && merchant.IsSettlement && merchant.Settlement.IsVillage && clientParty.HasPerk(DefaultPerks.Trade.DistributedGoods, true))
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Trade.DistributedGoods, clientParty, false, ref explainedNumber2, false);
				}
				if (isSelling && item.HasHorseComponent && clientParty.HasPerk(DefaultPerks.Trade.LocalConnection, true))
				{
					explainedNumber2.AddFactor(DefaultPerks.Trade.LocalConnection.SecondaryBonus, DefaultPerks.Trade.LocalConnection.Name);
				}
				if (isSelling && (item.ItemCategory == DefaultItemCategories.Pottery || item.ItemCategory == DefaultItemCategories.Tools || item.ItemCategory == DefaultItemCategories.Jewelry || item.ItemCategory == DefaultItemCategories.Cotton) && clientParty.LeaderHero != null)
				{
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Trade.TradeyardForeman, clientParty.LeaderHero.CharacterObject, true, ref explainedNumber2, false);
				}
				if (!isSelling && (item.ItemCategory == DefaultItemCategories.Clay || item.ItemCategory == DefaultItemCategories.Iron || item.ItemCategory == DefaultItemCategories.Silver || item.ItemCategory == DefaultItemCategories.Cotton) && clientParty.HasPerk(DefaultPerks.Trade.RapidDevelopment, false))
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Trade.RapidDevelopment, clientParty, false, ref explainedNumber2, false);
				}
			}
			return explainedNumber2.ResultNumber;
		}

		// Token: 0x06001AB0 RID: 6832 RVA: 0x00088C40 File Offset: 0x00086E40
		private float GetPriceFactor(ItemObject item, MobileParty tradingParty, PartyBase merchant, float inStoreValue, float supply, float demand, bool isSelling)
		{
			float basePriceFactor = this.GetBasePriceFactor(item.GetItemCategory(), inStoreValue, supply, demand, isSelling, item.Value);
			float tradePenalty = this.GetTradePenalty(item, tradingParty, merchant, isSelling, inStoreValue, supply, demand);
			if (!isSelling)
			{
				return basePriceFactor * (1f + tradePenalty);
			}
			return basePriceFactor * 1f / (1f + tradePenalty);
		}

		// Token: 0x06001AB1 RID: 6833 RVA: 0x00088C98 File Offset: 0x00086E98
		public override float GetBasePriceFactor(ItemCategory itemCategory, float inStoreValue, float supply, float demand, bool isSelling, int transferValue)
		{
			if (isSelling)
			{
				inStoreValue += (float)transferValue;
			}
			float value = MathF.Pow(demand / (0.1f * supply + inStoreValue * 0.04f + 2f), itemCategory.IsAnimal ? 0.3f : 0.6f);
			if (itemCategory.IsTradeGood)
			{
				return MathF.Clamp(value, 0.1f, 10f);
			}
			return MathF.Clamp(value, 0.8f, 1.3f);
		}

		// Token: 0x06001AB2 RID: 6834 RVA: 0x00088D0C File Offset: 0x00086F0C
		public override int GetPrice(EquipmentElement itemRosterElement, MobileParty clientParty, PartyBase merchant, bool isSelling, float inStoreValue, float supply, float demand)
		{
			float priceFactor = this.GetPriceFactor(itemRosterElement.Item, clientParty, merchant, inStoreValue, supply, demand, isSelling);
			float f = (float)itemRosterElement.ItemValue * priceFactor;
			int num = (isSelling ? MathF.Floor(f) : MathF.Ceiling(f));
			if (!isSelling && ((merchant != null) ? merchant.MobileParty : null) != null && merchant.MobileParty.IsCaravan && clientParty.HasPerk(DefaultPerks.Trade.SilverTongue, true))
			{
				num = MathF.Ceiling((float)num * (1f - DefaultPerks.Trade.SilverTongue.SecondaryBonus));
			}
			return MathF.Max(num, 1);
		}

		// Token: 0x06001AB3 RID: 6835 RVA: 0x00088D9C File Offset: 0x00086F9C
		public override int GetTheoreticalMaxItemMarketValue(ItemObject item)
		{
			if (item.IsTradeGood || item.IsAnimal)
			{
				return MathF.Round((float)item.Value * 10f);
			}
			return MathF.Round((float)item.Value * 1.3f);
		}

		// Token: 0x040008EA RID: 2282
		private const float MinPriceFactor = 0.1f;

		// Token: 0x040008EB RID: 2283
		private const float MaxPriceFactor = 10f;

		// Token: 0x040008EC RID: 2284
		private const float MinPriceFactorNonTrade = 0.8f;

		// Token: 0x040008ED RID: 2285
		private const float MaxPriceFactorNonTrade = 1.3f;

		// Token: 0x040008EE RID: 2286
		private const float HighTradePenaltyBaseValue = 1.5f;

		// Token: 0x040008EF RID: 2287
		private const float PackAnimalTradePenalty = 0.8f;

		// Token: 0x040008F0 RID: 2288
		private const float MountTradePenalty = 0.8f;
	}
}
