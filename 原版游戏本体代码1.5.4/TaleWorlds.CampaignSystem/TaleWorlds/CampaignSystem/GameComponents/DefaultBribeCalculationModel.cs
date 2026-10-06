using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultBribeCalculationModel : BribeCalculationModel
{
	public override bool IsBribeNotNeededToEnterKeep(Settlement settlement)
	{
		Campaign.Current.Models.SettlementAccessModel.CanMainHeroEnterLordsHall(settlement, out var accessDetails);
		if (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.LimitedAccess)
		{
			if (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.LimitedAccess)
			{
				return accessDetails.LimitedAccessSolution != SettlementAccessModel.LimitedAccessSolution.Bribe;
			}
			return false;
		}
		return true;
	}

	public override bool IsBribeNotNeededToEnterDungeon(Settlement settlement)
	{
		Campaign.Current.Models.SettlementAccessModel.CanMainHeroEnterDungeon(settlement, out var accessDetails);
		if (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.LimitedAccess)
		{
			if (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.LimitedAccess)
			{
				return accessDetails.LimitedAccessSolution != SettlementAccessModel.LimitedAccessSolution.Bribe;
			}
			return false;
		}
		return true;
	}

	private float GetSkillFactor()
	{
		return (1f - (float)Hero.MainHero.GetSkillValue(DefaultSkills.Roguery) / 300f) * 0.65f + 0.35f;
	}

	private int GetBribeForCriminalRating(IFaction faction)
	{
		return MathF.Round(Campaign.Current.Models.CrimeModel.GetCost(faction, CrimeModel.PaymentMethod.Gold, 0f)) / 5;
	}

	private int GetBaseBribeValue(IFaction faction)
	{
		if (faction.IsAtWarWith(Clan.PlayerClan))
		{
			return 5000;
		}
		if (faction.IsAtWarWith(Hero.MainHero.MapFaction))
		{
			return 3000;
		}
		if (FactionManager.IsNeutralWithFaction(faction, Clan.PlayerClan))
		{
			return 100;
		}
		if (Hero.MainHero.Clan == faction)
		{
			return 0;
		}
		if (Hero.MainHero.MapFaction == faction)
		{
			return 0;
		}
		if (faction is Clan)
		{
			_ = Hero.MainHero.MapFaction;
			_ = (faction as Clan).Kingdom;
			return 0;
		}
		return 0;
	}

	public override int GetBribeToEnterLordsHall(Settlement settlement)
	{
		if (IsBribeNotNeededToEnterKeep(settlement))
		{
			return 0;
		}
		return GetBribeInternal(settlement);
	}

	public override int GetBribeToEnterDungeon(Settlement settlement)
	{
		return GetBribeToEnterLordsHall(settlement);
	}

	private int GetBribeInternal(Settlement settlement)
	{
		int baseBribeValue = GetBaseBribeValue(settlement.MapFaction);
		baseBribeValue += GetBribeForCriminalRating(settlement.MapFaction);
		if (Clan.PlayerClan.Renown < 500f)
		{
			baseBribeValue += (500 - (int)Clan.PlayerClan.Renown) * 15 / 10;
			baseBribeValue = MathF.Max(baseBribeValue, 50);
		}
		baseBribeValue = (int)((float)baseBribeValue * GetSkillFactor() / 25f) * 25;
		return MathF.Max(baseBribeValue - settlement.BribePaid, 0);
	}
}
