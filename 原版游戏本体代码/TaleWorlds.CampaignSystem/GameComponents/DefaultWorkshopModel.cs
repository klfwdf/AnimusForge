using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000167 RID: 359
	public class DefaultWorkshopModel : WorkshopModel
	{
		// Token: 0x170006DA RID: 1754
		// (get) Token: 0x06001AE2 RID: 6882 RVA: 0x0008A98B File Offset: 0x00088B8B
		public override int WarehouseCapacity
		{
			get
			{
				return 6000;
			}
		}

		// Token: 0x170006DB RID: 1755
		// (get) Token: 0x06001AE3 RID: 6883 RVA: 0x0008A992 File Offset: 0x00088B92
		public override int DaysForPlayerSaveWorkshopFromBankruptcy
		{
			get
			{
				return 3;
			}
		}

		// Token: 0x170006DC RID: 1756
		// (get) Token: 0x06001AE4 RID: 6884 RVA: 0x0008A995 File Offset: 0x00088B95
		public override int CapitalLowLimit
		{
			get
			{
				return 5000;
			}
		}

		// Token: 0x170006DD RID: 1757
		// (get) Token: 0x06001AE5 RID: 6885 RVA: 0x0008A99C File Offset: 0x00088B9C
		public override int InitialCapital
		{
			get
			{
				return 10000;
			}
		}

		// Token: 0x170006DE RID: 1758
		// (get) Token: 0x06001AE6 RID: 6886 RVA: 0x0008A9A3 File Offset: 0x00088BA3
		public override int DailyExpense
		{
			get
			{
				return 100;
			}
		}

		// Token: 0x170006DF RID: 1759
		// (get) Token: 0x06001AE7 RID: 6887 RVA: 0x0008A9A7 File Offset: 0x00088BA7
		public override int DefaultWorkshopCountInSettlement
		{
			get
			{
				return 4;
			}
		}

		// Token: 0x170006E0 RID: 1760
		// (get) Token: 0x06001AE8 RID: 6888 RVA: 0x0008A9AA File Offset: 0x00088BAA
		public override int MaximumWorkshopsPlayerCanHave
		{
			get
			{
				return this.GetMaxWorkshopCountForClanTier(Campaign.Current.Models.ClanTierModel.MaxClanTier);
			}
		}

		// Token: 0x06001AE9 RID: 6889 RVA: 0x0008A9C8 File Offset: 0x00088BC8
		public override ExplainedNumber GetEffectiveConversionSpeedOfProduction(Workshop workshop, float speed, bool includeDescription)
		{
			ExplainedNumber result = new ExplainedNumber(speed, includeDescription, null);
			Settlement settlement = workshop.Settlement;
			if (settlement.OwnerClan.Kingdom != null)
			{
				if (settlement.OwnerClan.Kingdom.ActivePolicies.Contains(DefaultPolicies.ForgivenessOfDebts))
				{
					result.AddFactor(-0.05f, DefaultPolicies.ForgivenessOfDebts.Name);
				}
				if (settlement.OwnerClan.Kingdom.ActivePolicies.Contains(DefaultPolicies.StateMonopolies))
				{
					result.AddFactor(-0.1f, DefaultPolicies.StateMonopolies.Name);
				}
			}
			if (settlement.IsFortification)
			{
				settlement.Town.AddEffectOfBuildings(BuildingEffectEnum.WorkshopProduction, ref result);
			}
			PerkHelper.AddPerkBonusForTown(DefaultPerks.Trade.MercenaryConnections, settlement.Town, ref result);
			PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Steward.Sweatshops, workshop.Owner.CharacterObject, true, ref result, false);
			return result;
		}

		// Token: 0x06001AEA RID: 6890 RVA: 0x0008AA9A File Offset: 0x00088C9A
		public override int GetMaxWorkshopCountForClanTier(int tier)
		{
			return tier + 1;
		}

		// Token: 0x06001AEB RID: 6891 RVA: 0x0008AA9F File Offset: 0x00088C9F
		public override int GetCostForPlayer(Workshop workshop)
		{
			return workshop.WorkshopType.EquipmentCost + (int)workshop.Settlement.Town.Prosperity * 4 + this.InitialCapital / 5;
		}

		// Token: 0x06001AEC RID: 6892 RVA: 0x0008AAC9 File Offset: 0x00088CC9
		public override int GetCostForNotable(Workshop workshop)
		{
			return (workshop.WorkshopType.EquipmentCost + (int)workshop.Settlement.Town.Prosperity / 2 + workshop.Capital) / 2;
		}

		// Token: 0x06001AED RID: 6893 RVA: 0x0008AAF4 File Offset: 0x00088CF4
		public override Hero GetNotableOwnerForWorkshop(Workshop workshop)
		{
			List<ValueTuple<Hero, float>> list = new List<ValueTuple<Hero, float>>();
			foreach (Hero hero in workshop.Settlement.Notables)
			{
				if (hero.IsAlive && hero != workshop.Owner)
				{
					int count = hero.OwnedWorkshops.Count;
					float item = Math.Max(hero.Power, 0f) / MathF.Pow(10f, (float)count);
					list.Add(new ValueTuple<Hero, float>(hero, item));
				}
			}
			return MBRandom.ChooseWeighted<Hero>(list);
		}

		// Token: 0x06001AEE RID: 6894 RVA: 0x0008AB9C File Offset: 0x00088D9C
		public override int GetConvertProductionCost(WorkshopType workshopType)
		{
			return workshopType.EquipmentCost;
		}

		// Token: 0x06001AEF RID: 6895 RVA: 0x0008ABA4 File Offset: 0x00088DA4
		public override bool CanPlayerSellWorkshop(Workshop workshop, out TextObject explanation)
		{
			Campaign.Current.Models.WorkshopModel.GetCostForNotable(workshop);
			Hero notableOwnerForWorkshop = Campaign.Current.Models.WorkshopModel.GetNotableOwnerForWorkshop(workshop);
			explanation = ((notableOwnerForWorkshop == null) ? new TextObject("{=oqPf2Gdp}There isn't any prospective buyer in the town.", null) : null);
			return notableOwnerForWorkshop != null;
		}

		// Token: 0x06001AF0 RID: 6896 RVA: 0x0008ABF4 File Offset: 0x00088DF4
		public override float GetTradeXpPerWarehouseProduction(EquipmentElement production)
		{
			return (float)production.GetBaseValue() * 0.1f;
		}
	}
}
