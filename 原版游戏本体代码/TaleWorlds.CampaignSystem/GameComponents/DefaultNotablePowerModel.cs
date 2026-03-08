using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200012D RID: 301
	public class DefaultNotablePowerModel : NotablePowerModel
	{
		// Token: 0x17000685 RID: 1669
		// (get) Token: 0x060018C7 RID: 6343 RVA: 0x0007977A File Offset: 0x0007797A
		public override int NotableDisappearPowerLimit
		{
			get
			{
				return 100;
			}
		}

		// Token: 0x060018C8 RID: 6344 RVA: 0x00079780 File Offset: 0x00077980
		public override ExplainedNumber CalculateDailyPowerChangeForHero(Hero hero, bool includeDescriptions = false)
		{
			ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions, null);
			if (!hero.IsActive)
			{
				return result;
			}
			if (hero.Power > (float)this.RegularNotableMaxPowerLevel)
			{
				this.CalculateDailyPowerChangeForInfluentialNotables(hero, ref result);
			}
			this.CalculateDailyPowerChangePerPropertyOwned(hero, ref result);
			if (hero.Issue != null)
			{
				this.CalculatePowerChangeFromIssues(hero, ref result);
			}
			if (hero.IsArtisan)
			{
				result.Add(-0.1f, this._propertyEffect, null);
			}
			if (hero.IsGangLeader)
			{
				result.Add(-0.4f, this._propertyEffect, null);
			}
			if (hero.IsRuralNotable)
			{
				result.Add(0.1f, this._propertyEffect, null);
			}
			if (hero.IsHeadman)
			{
				result.Add(0.1f, this._propertyEffect, null);
			}
			if (hero.IsMerchant)
			{
				result.Add(0.2f, this._propertyEffect, null);
			}
			if (hero.CurrentSettlement != null)
			{
				if (hero.CurrentSettlement.IsVillage && hero.CurrentSettlement.Village.Bound.IsCastle)
				{
					result.Add(0.1f, this._propertyEffect, null);
				}
				if (hero.SupporterOf == hero.CurrentSettlement.OwnerClan)
				{
					this.CalculateDailyPowerChangeForAffiliationWithRulerClan(ref result);
				}
			}
			return result;
		}

		// Token: 0x17000686 RID: 1670
		// (get) Token: 0x060018C9 RID: 6345 RVA: 0x000798B9 File Offset: 0x00077AB9
		public override int RegularNotableMaxPowerLevel
		{
			get
			{
				return this.NotablePowerRanks[1].MinPowerValue;
			}
		}

		// Token: 0x060018CA RID: 6346 RVA: 0x000798CC File Offset: 0x00077ACC
		private void CalculateDailyPowerChangePerPropertyOwned(Hero hero, ref ExplainedNumber explainedNumber)
		{
			int count = hero.OwnedAlleys.Count;
			explainedNumber.Add(0.1f * (float)count, this._propertyEffect, null);
		}

		// Token: 0x060018CB RID: 6347 RVA: 0x000798FA File Offset: 0x00077AFA
		private void CalculateDailyPowerChangeForAffiliationWithRulerClan(ref ExplainedNumber explainedNumber)
		{
			explainedNumber.Add(0.2f, this._rulerClanEffect, null);
		}

		// Token: 0x060018CC RID: 6348 RVA: 0x00079910 File Offset: 0x00077B10
		private void CalculateDailyPowerChangeForInfluentialNotables(Hero hero, ref ExplainedNumber explainedNumber)
		{
			float value = -1f * ((hero.Power - (float)this.RegularNotableMaxPowerLevel) / 500f);
			explainedNumber.Add(value, this._currentRankEffect, null);
		}

		// Token: 0x060018CD RID: 6349 RVA: 0x00079946 File Offset: 0x00077B46
		private void CalculatePowerChangeFromIssues(Hero hero, ref ExplainedNumber explainedNumber)
		{
			Campaign.Current.Models.IssueModel.GetIssueEffectOfHero(DefaultIssueEffects.IssueOwnerPower, hero, ref explainedNumber);
		}

		// Token: 0x060018CE RID: 6350 RVA: 0x00079963 File Offset: 0x00077B63
		public override TextObject GetPowerRankName(Hero hero)
		{
			return this.GetPowerRank(hero).Name;
		}

		// Token: 0x060018CF RID: 6351 RVA: 0x00079971 File Offset: 0x00077B71
		public override float GetInfluenceBonusToClan(Hero hero)
		{
			return this.GetPowerRank(hero).InfluenceBonus;
		}

		// Token: 0x060018D0 RID: 6352 RVA: 0x00079980 File Offset: 0x00077B80
		private DefaultNotablePowerModel.NotablePowerRank GetPowerRank(Hero hero)
		{
			int num = 0;
			for (int i = 0; i < this.NotablePowerRanks.Length; i++)
			{
				if (hero.Power > (float)this.NotablePowerRanks[i].MinPowerValue)
				{
					num = i;
				}
			}
			return this.NotablePowerRanks[num];
		}

		// Token: 0x060018D1 RID: 6353 RVA: 0x000799CC File Offset: 0x00077BCC
		public override int GetInitialPower(Hero hero)
		{
			int num = 0;
			float randomFloat = MBRandom.RandomFloat;
			num += ((randomFloat < 0.2f) ? MBRandom.RandomInt((int)((float)(this.NotablePowerRanks[0].MinPowerValue + this.NotablePowerRanks[1].MinPowerValue) * 0.5f), this.NotablePowerRanks[1].MinPowerValue) : ((randomFloat < 0.8f) ? MBRandom.RandomInt(this.NotablePowerRanks[1].MinPowerValue, this.NotablePowerRanks[2].MinPowerValue) : MBRandom.RandomInt(this.NotablePowerRanks[2].MinPowerValue, (int)((float)this.NotablePowerRanks[2].MinPowerValue * 2f))));
			if ((hero.Occupation == Occupation.GangLeader || hero.Occupation == Occupation.Artisan || hero.Occupation == Occupation.RuralNotable || hero.Occupation == Occupation.Merchant || hero.Occupation == Occupation.Headman) && hero.HomeSettlement.IsVillage && hero.HomeSettlement.Village.Bound != null && hero.HomeSettlement.Village.Bound.IsCastle)
			{
				num += (int)(MBRandom.RandomFloat * 20f);
			}
			return num;
		}

		// Token: 0x060018D2 RID: 6354 RVA: 0x00079B07 File Offset: 0x00077D07
		public override int GetInitialNotableSupporterCost(Hero hero)
		{
			return 20000 + 10000 * Clan.PlayerClan.SupporterNotables.Count;
		}

		// Token: 0x04000800 RID: 2048
		private DefaultNotablePowerModel.NotablePowerRank[] NotablePowerRanks = new DefaultNotablePowerModel.NotablePowerRank[]
		{
			new DefaultNotablePowerModel.NotablePowerRank(new TextObject("{=aTeuX4L0}Regular", null), 0, 0.05f),
			new DefaultNotablePowerModel.NotablePowerRank(new TextObject("{=nTETQEmy}Influential", null), 100, 0.1f),
			new DefaultNotablePowerModel.NotablePowerRank(new TextObject("{=UCpyo9hw}Powerful", null), 200, 0.15f)
		};

		// Token: 0x04000801 RID: 2049
		private TextObject _currentRankEffect = new TextObject("{=7j9uHxLM}Current Rank Effect", null);

		// Token: 0x04000802 RID: 2050
		private TextObject _militiaEffect = new TextObject("{=R1MaIgOb}Militia Effect", null);

		// Token: 0x04000803 RID: 2051
		private TextObject _rulerClanEffect = new TextObject("{=JE3RTqx5}Ruler Clan Effect", null);

		// Token: 0x04000804 RID: 2052
		private TextObject _propertyEffect = new TextObject("{=yDomN9L2}Property Effect", null);

		// Token: 0x0200058E RID: 1422
		private struct NotablePowerRank
		{
			// Token: 0x06004D75 RID: 19829 RVA: 0x0017B571 File Offset: 0x00179771
			public NotablePowerRank(TextObject name, int minPowerValue, float influenceBonus)
			{
				this.Name = name;
				this.MinPowerValue = minPowerValue;
				this.InfluenceBonus = influenceBonus;
			}

			// Token: 0x0400177F RID: 6015
			public readonly TextObject Name;

			// Token: 0x04001780 RID: 6016
			public readonly int MinPowerValue;

			// Token: 0x04001781 RID: 6017
			public readonly float InfluenceBonus;
		}
	}
}
