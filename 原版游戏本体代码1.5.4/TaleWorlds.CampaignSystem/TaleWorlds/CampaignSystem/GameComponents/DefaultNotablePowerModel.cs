using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultNotablePowerModel : NotablePowerModel
{
	private struct NotablePowerRank
	{
		public readonly TextObject Name;

		public readonly int MinPowerValue;

		public readonly float InfluenceBonus;

		public NotablePowerRank(TextObject name, int minPowerValue, float influenceBonus)
		{
			Name = name;
			MinPowerValue = minPowerValue;
			InfluenceBonus = influenceBonus;
		}
	}

	private NotablePowerRank[] NotablePowerRanks = new NotablePowerRank[3]
	{
		new NotablePowerRank(new TextObject("{=aTeuX4L0}Regular"), 0, 0.05f),
		new NotablePowerRank(new TextObject("{=nTETQEmy}Influential"), 100, 0.1f),
		new NotablePowerRank(new TextObject("{=UCpyo9hw}Powerful"), 200, 0.15f)
	};

	private TextObject _currentRankEffect = new TextObject("{=7j9uHxLM}Current Rank Effect");

	private TextObject _militiaEffect = new TextObject("{=R1MaIgOb}Militia Effect");

	private TextObject _rulerClanEffect = new TextObject("{=JE3RTqx5}Ruler Clan Effect");

	private TextObject _propertyEffect = new TextObject("{=yDomN9L2}Property Effect");

	public override int NotableDisappearPowerLimit => 100;

	public override int RegularNotableMaxPowerLevel => NotablePowerRanks[1].MinPowerValue;

	public override ExplainedNumber CalculateDailyPowerChangeForHero(Hero hero, bool includeDescriptions = false)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(0f, includeDescriptions);
		if (!hero.IsActive)
		{
			return explainedNumber;
		}
		if (hero.Power > (float)RegularNotableMaxPowerLevel)
		{
			CalculateDailyPowerChangeForInfluentialNotables(hero, ref explainedNumber);
		}
		CalculateDailyPowerChangePerPropertyOwned(hero, ref explainedNumber);
		if (hero.Issue != null)
		{
			CalculatePowerChangeFromIssues(hero, ref explainedNumber);
		}
		if (hero.IsArtisan)
		{
			explainedNumber.Add(-0.1f, _propertyEffect);
		}
		if (hero.IsGangLeader)
		{
			explainedNumber.Add(-0.4f, _propertyEffect);
		}
		if (hero.IsRuralNotable)
		{
			explainedNumber.Add(0.1f, _propertyEffect);
		}
		if (hero.IsHeadman)
		{
			explainedNumber.Add(0.1f, _propertyEffect);
		}
		if (hero.IsMerchant)
		{
			explainedNumber.Add(0.2f, _propertyEffect);
		}
		if (hero.CurrentSettlement != null)
		{
			if (hero.CurrentSettlement.IsVillage && hero.CurrentSettlement.Village.Bound.IsCastle)
			{
				explainedNumber.Add(0.1f, _propertyEffect);
			}
			if (hero.SupporterOf == hero.CurrentSettlement.OwnerClan)
			{
				CalculateDailyPowerChangeForAffiliationWithRulerClan(ref explainedNumber);
			}
		}
		return explainedNumber;
	}

	private void CalculateDailyPowerChangePerPropertyOwned(Hero hero, ref ExplainedNumber explainedNumber)
	{
		int count = hero.OwnedAlleys.Count;
		explainedNumber.Add(0.1f * (float)count, _propertyEffect);
	}

	private void CalculateDailyPowerChangeForAffiliationWithRulerClan(ref ExplainedNumber explainedNumber)
	{
		explainedNumber.Add(0.2f, _rulerClanEffect);
	}

	private void CalculateDailyPowerChangeForInfluentialNotables(Hero hero, ref ExplainedNumber explainedNumber)
	{
		float value = -1f * ((hero.Power - (float)RegularNotableMaxPowerLevel) / 500f);
		explainedNumber.Add(value, _currentRankEffect);
	}

	private void CalculatePowerChangeFromIssues(Hero hero, ref ExplainedNumber explainedNumber)
	{
		Campaign.Current.Models.IssueModel.GetIssueEffectOfHero(DefaultIssueEffects.IssueOwnerPower, hero, ref explainedNumber);
	}

	public override TextObject GetPowerRankName(Hero hero)
	{
		return GetPowerRank(hero).Name;
	}

	public override float GetInfluenceBonusToClan(Hero hero)
	{
		return GetPowerRank(hero).InfluenceBonus;
	}

	private NotablePowerRank GetPowerRank(Hero hero)
	{
		int num = 0;
		for (int i = 0; i < NotablePowerRanks.Length; i++)
		{
			if (hero.Power > (float)NotablePowerRanks[i].MinPowerValue)
			{
				num = i;
			}
		}
		return NotablePowerRanks[num];
	}

	public override int GetInitialPower(Hero hero)
	{
		int num = 0;
		float randomFloat = MBRandom.RandomFloat;
		num += ((randomFloat < 0.2f) ? MBRandom.RandomInt((int)((float)(NotablePowerRanks[0].MinPowerValue + NotablePowerRanks[1].MinPowerValue) * 0.5f), NotablePowerRanks[1].MinPowerValue) : ((randomFloat < 0.8f) ? MBRandom.RandomInt(NotablePowerRanks[1].MinPowerValue, NotablePowerRanks[2].MinPowerValue) : MBRandom.RandomInt(NotablePowerRanks[2].MinPowerValue, (int)((float)NotablePowerRanks[2].MinPowerValue * 2f))));
		if ((hero.Occupation == Occupation.GangLeader || hero.Occupation == Occupation.Artisan || hero.Occupation == Occupation.RuralNotable || hero.Occupation == Occupation.Merchant || hero.Occupation == Occupation.Headman) && hero.HomeSettlement.IsVillage && hero.HomeSettlement.Village.Bound != null && hero.HomeSettlement.Village.Bound.IsCastle)
		{
			num += (int)(MBRandom.RandomFloat * 20f);
		}
		return num;
	}

	public override int GetInitialNotableSupporterCost(Hero hero)
	{
		return 20000 + 10000 * Clan.PlayerClan.SupporterNotables.Count;
	}
}
