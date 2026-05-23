using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003F0 RID: 1008
	public class GarrisonRecruitmentCampaignBehavior : CampaignBehaviorBase, IGarrisonRecruitmentBehavior
	{
		// Token: 0x06003EC8 RID: 16072 RVA: 0x00119F91 File Offset: 0x00118191
		public override void RegisterEvents()
		{
			CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement>(this.OnDailySettlementTick));
		}

		// Token: 0x06003EC9 RID: 16073 RVA: 0x00119FAA File Offset: 0x001181AA
		private static CharacterObject GetBasicTroopForTown(Town town)
		{
			return town.MapFaction.BasicTroop;
		}

		// Token: 0x06003ECA RID: 16074 RVA: 0x00119FB8 File Offset: 0x001181B8
		private void OnDailySettlementTick(Settlement settlement)
		{
			if (settlement.IsFortification)
			{
				Town town = settlement.Town;
				if (this.SettlementCheckGarrisonChangeCommonCondition(settlement))
				{
					this.TickGarrisonChangeForTown(town);
				}
				if (this.CanSettlementAutoRecruit(settlement))
				{
					this.TickAutoRecruitmentGarrisonChange(town);
				}
				if (town.GarrisonParty != null)
				{
					this.HandleGarrisonXpChange(town);
				}
			}
		}

		// Token: 0x06003ECB RID: 16075 RVA: 0x0011A004 File Offset: 0x00118204
		private void TickAutoRecruitmentGarrisonChange(Town town)
		{
			float resultNumber = this.GetAutoRecruitmentGarrisonChangeExplainedNumber(town).ResultNumber;
			if (resultNumber > 0f)
			{
				if (town.GarrisonParty == null)
				{
					town.Owner.Settlement.AddGarrisonParty();
				}
				int num = 0;
				while ((float)num < resultNumber)
				{
					GarrisonRecruitmentCampaignBehavior.VolunteerTroop volunteerTroop = this._volunteerListCache.ElementAt(num);
					Hero ownerNotable = volunteerTroop.OwnerNotable;
					int notableVolunteerArrayIndex = volunteerTroop.NotableVolunteerArrayIndex;
					town.GarrisonParty.MemberRoster.AddToCounts(ownerNotable.VolunteerTypes[notableVolunteerArrayIndex], 1, false, 0, 0, true, -1);
					town.Settlement.OwnerClan.AutoRecruitmentExpenses += Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(ownerNotable.VolunteerTypes[notableVolunteerArrayIndex], town.Settlement.OwnerClan.Leader, false).RoundedResultNumber;
					ownerNotable.VolunteerTypes[notableVolunteerArrayIndex] = null;
					num++;
				}
			}
		}

		// Token: 0x06003ECC RID: 16076 RVA: 0x0011A0E8 File Offset: 0x001182E8
		private void TickGarrisonChangeForTown(Town town)
		{
			int num = (int)this.GetBaseGarrisonChangeExplainedNumber(town, false).ResultNumber;
			if (num > 0)
			{
				if (town.GarrisonParty == null)
				{
					town.Owner.Settlement.AddGarrisonParty();
				}
				town.GarrisonParty.MemberRoster.AddToCounts(GarrisonRecruitmentCampaignBehavior.GetBasicTroopForTown(town), num, false, 0, 0, true, -1);
			}
		}

		// Token: 0x06003ECD RID: 16077 RVA: 0x0011A140 File Offset: 0x00118340
		private void HandleGarrisonXpChange(Town town)
		{
			int num = Campaign.Current.Models.DailyTroopXpBonusModel.CalculateDailyTroopXpBonus(town);
			float num2 = Campaign.Current.Models.DailyTroopXpBonusModel.CalculateGarrisonXpBonusMultiplier(town);
			if (num > 0)
			{
				foreach (TroopRosterElement troopRosterElement in town.GarrisonParty.MemberRoster.GetTroopRoster())
				{
					town.GarrisonParty.MemberRoster.AddXpToTroop(troopRosterElement.Character, MathF.Round((float)num * num2 * (float)troopRosterElement.Number));
				}
			}
		}

		// Token: 0x06003ECE RID: 16078 RVA: 0x0011A1F0 File Offset: 0x001183F0
		private void RepopulateVolunteerListCache(Town town)
		{
			this._volunteerListCache.Clear();
			foreach (Hero hero in town.Settlement.Notables)
			{
				if (hero.IsAlive)
				{
					int num = Campaign.Current.Models.VolunteerModel.MaximumIndexGarrisonCanRecruitFromHero(town.Settlement, hero);
					for (int i = 0; i < num; i++)
					{
						if (hero.VolunteerTypes[i] != null)
						{
							GarrisonRecruitmentCampaignBehavior.VolunteerTroop item = new GarrisonRecruitmentCampaignBehavior.VolunteerTroop(hero, i);
							this._volunteerListCache.Add(item);
						}
					}
				}
			}
			foreach (Village village in town.Settlement.BoundVillages)
			{
				if (village.VillageState == Village.VillageStates.Normal)
				{
					foreach (Hero hero2 in village.Settlement.Notables)
					{
						if (hero2.IsAlive)
						{
							int num2 = Campaign.Current.Models.VolunteerModel.MaximumIndexGarrisonCanRecruitFromHero(town.Settlement, hero2);
							for (int j = 0; j < num2; j++)
							{
								if (hero2.VolunteerTypes[j] != null)
								{
									GarrisonRecruitmentCampaignBehavior.VolunteerTroop item2 = new GarrisonRecruitmentCampaignBehavior.VolunteerTroop(hero2, j);
									this._volunteerListCache.Add(item2);
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x06003ECF RID: 16079 RVA: 0x0011A390 File Offset: 0x00118590
		private ExplainedNumber GetAutoRecruitmentGarrisonChangeExplainedNumber(Town town)
		{
			ExplainedNumber result = new ExplainedNumber(0f, true, null);
			this.RepopulateVolunteerListCache(town);
			MobileParty garrisonParty = town.GarrisonParty;
			int num = ((garrisonParty != null) ? garrisonParty.GetAvailableWageBudget() : town.Settlement.GarrisonWagePaymentLimit);
			if (num > 0)
			{
				int num2 = 0;
				int num3 = 0;
				int count = this._volunteerListCache.Count;
				result.Add((float)count, new TextObject("{=Uzsnek6O}Auto Recruitment", null), null);
				foreach (GarrisonRecruitmentCampaignBehavior.VolunteerTroop volunteerTroop in this._volunteerListCache)
				{
					num2 += volunteerTroop.Wage;
					if (num2 >= num)
					{
						break;
					}
					num3++;
				}
				if ((float)num3 < result.LimitMaxValue)
				{
					result.LimitMax((float)num3, new TextObject("{=7GJOWuUO}Wage Limit", null));
				}
				int num4 = ((town.GarrisonParty == null) ? ((int)Campaign.Current.Models.PartySizeLimitModel.CalculateGarrisonPartySizeLimit(town.Settlement, false).ResultNumber) : (town.GarrisonParty.Party.PartySizeLimit - town.GarrisonParty.Party.NumberOfAllMembers));
				if ((float)num4 < result.LimitMaxValue)
				{
					result.LimitMax((float)num4, new TextObject("{=mp68RYnD}Party Size Limit", null));
				}
				int maximumDailyAutoRecruitmentCount = Campaign.Current.Models.SettlementGarrisonModel.GetMaximumDailyAutoRecruitmentCount(town);
				if ((float)maximumDailyAutoRecruitmentCount < result.LimitMaxValue)
				{
					result.LimitMax((float)maximumDailyAutoRecruitmentCount, new TextObject("{=91fnSU2A}Maximum Auto Recruitment", null));
				}
			}
			return result;
		}

		// Token: 0x06003ED0 RID: 16080 RVA: 0x0011A520 File Offset: 0x00118720
		private ExplainedNumber GetBaseGarrisonChangeExplainedNumber(Town town, bool includeDescriptions = false)
		{
			ExplainedNumber result = Campaign.Current.Models.SettlementGarrisonModel.CalculateBaseGarrisonChange(town.Settlement, includeDescriptions);
			int num = ((town.GarrisonParty == null) ? ((int)Campaign.Current.Models.PartySizeLimitModel.CalculateGarrisonPartySizeLimit(town.Settlement, false).ResultNumber) : (town.GarrisonParty.Party.PartySizeLimit - town.GarrisonParty.Party.NumberOfAllMembers));
			if (result.LimitMaxValue > (float)num)
			{
				result.LimitMax((float)num, new TextObject("{=mp68RYnD}Party Size Limit", null));
			}
			int characterWage = Campaign.Current.Models.PartyWageModel.GetCharacterWage(GarrisonRecruitmentCampaignBehavior.GetBasicTroopForTown(town));
			MobileParty garrisonParty = town.GarrisonParty;
			int num2 = ((garrisonParty != null) ? garrisonParty.GetAvailableWageBudget() : town.Settlement.GarrisonWagePaymentLimit) / characterWage;
			if (result.LimitMaxValue > (float)num2)
			{
				result.LimitMax((float)num2, new TextObject("{=7GJOWuUO}Wage Limit", null));
			}
			return result;
		}

		// Token: 0x06003ED1 RID: 16081 RVA: 0x0011A614 File Offset: 0x00118814
		public ExplainedNumber GetGarrisonChangeExplainedNumber(Town town)
		{
			ExplainedNumber result = new ExplainedNumber(0f, true, null);
			ExplainedNumber baseGarrisonChangeExplainedNumber = this.GetBaseGarrisonChangeExplainedNumber(town, true);
			result.AddFromExplainedNumber(baseGarrisonChangeExplainedNumber, new TextObject("{=basevalue}Base", null));
			if (this.CanSettlementAutoRecruit(town.Settlement))
			{
				ExplainedNumber autoRecruitmentGarrisonChangeExplainedNumber = this.GetAutoRecruitmentGarrisonChangeExplainedNumber(town);
				result.AddFromExplainedNumber(autoRecruitmentGarrisonChangeExplainedNumber, new TextObject("{=Uzsnek6O}Auto Recruitment", null));
			}
			return result;
		}

		// Token: 0x06003ED2 RID: 16082 RVA: 0x0011A675 File Offset: 0x00118875
		private bool CanSettlementAutoRecruit(Settlement settlement)
		{
			return settlement.Town.GarrisonAutoRecruitmentIsEnabled && settlement.Town.FoodChange > 0f && this.SettlementCheckGarrisonChangeCommonCondition(settlement);
		}

		// Token: 0x06003ED3 RID: 16083 RVA: 0x0011A69F File Offset: 0x0011889F
		private bool SettlementCheckGarrisonChangeCommonCondition(Settlement settlement)
		{
			return settlement.Party.MapEvent == null && settlement.Party.SiegeEvent == null;
		}

		// Token: 0x06003ED4 RID: 16084 RVA: 0x0011A6BE File Offset: 0x001188BE
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x040012B4 RID: 4788
		private SortedSet<GarrisonRecruitmentCampaignBehavior.VolunteerTroop> _volunteerListCache = new SortedSet<GarrisonRecruitmentCampaignBehavior.VolunteerTroop>();

		// Token: 0x020007F5 RID: 2037
		public struct VolunteerTroop : IComparable
		{
			// Token: 0x06006366 RID: 25446 RVA: 0x001BD3E5 File Offset: 0x001BB5E5
			public VolunteerTroop(Hero ownerNotable, int notableVolunteerArrayIndex)
			{
				this.OwnerNotable = ownerNotable;
				this.NotableVolunteerArrayIndex = notableVolunteerArrayIndex;
				this.Wage = Campaign.Current.Models.PartyWageModel.GetCharacterWage(ownerNotable.VolunteerTypes[notableVolunteerArrayIndex]);
			}

			// Token: 0x06006367 RID: 25447 RVA: 0x001BD418 File Offset: 0x001BB618
			public int CompareTo(object obj)
			{
				GarrisonRecruitmentCampaignBehavior.VolunteerTroop volunteerTroop = (GarrisonRecruitmentCampaignBehavior.VolunteerTroop)obj;
				int num = this.Wage.CompareTo(volunteerTroop.Wage);
				if (num == 0)
				{
					num = volunteerTroop.NotableVolunteerArrayIndex.CompareTo(this.NotableVolunteerArrayIndex);
				}
				if (num == 0)
				{
					num = volunteerTroop.OwnerNotable.Id.CompareTo(this.OwnerNotable.Id);
				}
				return num;
			}

			// Token: 0x04001FCA RID: 8138
			public Hero OwnerNotable;

			// Token: 0x04001FCB RID: 8139
			public int NotableVolunteerArrayIndex;

			// Token: 0x04001FCC RID: 8140
			public int Wage;
		}
	}
}
