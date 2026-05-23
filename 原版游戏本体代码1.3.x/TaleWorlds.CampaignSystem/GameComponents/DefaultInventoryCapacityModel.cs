using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200011F RID: 287
	public class DefaultInventoryCapacityModel : InventoryCapacityModel
	{
		// Token: 0x06001811 RID: 6161 RVA: 0x000736B5 File Offset: 0x000718B5
		public override int GetItemAverageWeight()
		{
			return 10;
		}

		// Token: 0x06001812 RID: 6162 RVA: 0x000736B9 File Offset: 0x000718B9
		public override float GetItemEffectiveWeight(EquipmentElement equipmentElement, MobileParty mobileParty, bool isCurrentlyAtSea, out TextObject description)
		{
			if (equipmentElement.Item.HasHorseComponent)
			{
				description = DefaultInventoryCapacityModel._textMountsAndPackAnimals;
				return 0f;
			}
			description = DefaultInventoryCapacityModel._textItems;
			return equipmentElement.GetEquipmentElementWeight();
		}

		// Token: 0x06001813 RID: 6163 RVA: 0x000736E8 File Offset: 0x000718E8
		public override ExplainedNumber CalculateInventoryCapacity(MobileParty mobileParty, bool isCurrentlyAtSea, bool includeDescriptions = false, int additionalTroops = 0, int additionalSpareMounts = 0, int additionalPackAnimals = 0, bool includeFollowers = false)
		{
			ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions, null);
			PartyBase party = mobileParty.Party;
			int num = party.NumberOfMounts;
			int num2 = party.NumberOfHealthyMembers;
			int num3 = party.NumberOfPackAnimals;
			if (includeFollowers)
			{
				foreach (MobileParty mobileParty2 in mobileParty.AttachedParties)
				{
					num += mobileParty2.Party.NumberOfMounts;
					num2 += mobileParty2.Party.NumberOfHealthyMembers;
					num3 += mobileParty2.Party.NumberOfPackAnimals;
				}
			}
			if (mobileParty.HasPerk(DefaultPerks.Steward.ArenicosHorses, false) && !isCurrentlyAtSea)
			{
				num2 += MathF.Round((float)num2 * DefaultPerks.Steward.ArenicosHorses.PrimaryBonus);
			}
			if (!mobileParty.IsCurrentlyAtSea && mobileParty.HasPerk(DefaultPerks.Steward.ForcedLabor, false))
			{
				num2 += party.PrisonRoster.TotalHealthyCount;
			}
			result.Add(10f, DefaultInventoryCapacityModel._textBase, null);
			result.Add((float)num2 * 2f * 10f, DefaultInventoryCapacityModel._textTroops, null);
			if (!isCurrentlyAtSea)
			{
				result.Add((float)num * 2f * 10f, DefaultInventoryCapacityModel._textSpareMounts, null);
				ExplainedNumber explainedNumber = new ExplainedNumber((float)num3 * 10f * 10f, false, null);
				if (mobileParty.HasPerk(DefaultPerks.Scouting.BeastWhisperer, true))
				{
					explainedNumber.AddFactor(DefaultPerks.Scouting.BeastWhisperer.SecondaryBonus, DefaultPerks.Scouting.BeastWhisperer.Name);
				}
				if (mobileParty.HasPerk(DefaultPerks.Riding.DeeperSacks, false))
				{
					explainedNumber.AddFactor(DefaultPerks.Riding.DeeperSacks.PrimaryBonus, DefaultPerks.Riding.DeeperSacks.Name);
				}
				if (mobileParty.HasPerk(DefaultPerks.Steward.ArenicosMules, false))
				{
					explainedNumber.AddFactor(DefaultPerks.Steward.ArenicosMules.PrimaryBonus, DefaultPerks.Steward.ArenicosMules.Name);
				}
				result.Add(explainedNumber.ResultNumber, DefaultInventoryCapacityModel._textPackAnimals, null);
				if (mobileParty.HasPerk(DefaultPerks.Trade.CaravanMaster, false))
				{
					result.AddFactor(DefaultPerks.Trade.CaravanMaster.PrimaryBonus, DefaultPerks.Trade.CaravanMaster.Name);
				}
			}
			result.LimitMin(10f);
			return result;
		}

		// Token: 0x06001814 RID: 6164 RVA: 0x0007390C File Offset: 0x00071B0C
		public override ExplainedNumber CalculateTotalWeightCarried(MobileParty mobileParty, bool isCurrentlyAtSea, bool includeDescriptions = false)
		{
			ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions, DefaultInventoryCapacityModel._textItems);
			InventoryCapacityModel inventoryCapacityModel = Campaign.Current.Models.InventoryCapacityModel;
			foreach (ItemRosterElement itemRosterElement in mobileParty.ItemRoster)
			{
				TextObject description;
				float itemEffectiveWeight = inventoryCapacityModel.GetItemEffectiveWeight(itemRosterElement.EquipmentElement, mobileParty, isCurrentlyAtSea, out description);
				result.Add(itemEffectiveWeight * (float)itemRosterElement.Amount, description, null);
			}
			return result;
		}

		// Token: 0x040007D3 RID: 2003
		private const int _itemAverageWeight = 10;

		// Token: 0x040007D4 RID: 2004
		private const float TroopsFactor = 2f;

		// Token: 0x040007D5 RID: 2005
		private const float SpareMountsFactor = 2f;

		// Token: 0x040007D6 RID: 2006
		private const float PackAnimalsFactor = 10f;

		// Token: 0x040007D7 RID: 2007
		private static readonly TextObject _textTroops = new TextObject("{=5k4dxUEJ}Troops", null);

		// Token: 0x040007D8 RID: 2008
		private static readonly TextObject _textBase = new TextObject("{=basevalue}Base", null);

		// Token: 0x040007D9 RID: 2009
		private static readonly TextObject _textSpareMounts = new TextObject("{=rCiKbsyW}Spare Mounts", null);

		// Token: 0x040007DA RID: 2010
		private static readonly TextObject _textPackAnimals = new TextObject("{=dI1AOyqh}Pack Animals", null);

		// Token: 0x040007DB RID: 2011
		private static readonly TextObject _textMountsAndPackAnimals = new TextObject("{=Sb1MKbvP}Mounts and Pack Animals", null);

		// Token: 0x040007DC RID: 2012
		private static readonly TextObject _textLiveStocksAnimals = new TextObject("{=KxUgSAKi}Live Stock Animals", null);

		// Token: 0x040007DD RID: 2013
		private static readonly TextObject _textItems = new TextObject("{=U7er3V9s}Items", null);
	}
}
