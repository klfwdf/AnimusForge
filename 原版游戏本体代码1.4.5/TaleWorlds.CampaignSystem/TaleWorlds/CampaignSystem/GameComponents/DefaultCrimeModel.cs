using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultCrimeModel : CrimeModel
{
	private const float ModerateCrimeRatingThreshold = 30f;

	private const float SevereCrimeRatingThreshold = 65f;

	public override float DeclareWarCrimeRatingThreshold => 60f;

	public override bool DoesPlayerHaveAnyCrimeRating(IFaction faction)
	{
		return faction.MainHeroCrimeRating > 0f;
	}

	public override bool IsPlayerCrimeRatingSevere(IFaction faction)
	{
		return faction.MainHeroCrimeRating >= 65f;
	}

	public override bool IsPlayerCrimeRatingModerate(IFaction faction)
	{
		if (faction.MainHeroCrimeRating > 30f)
		{
			return faction.MainHeroCrimeRating <= 65f;
		}
		return false;
	}

	public override bool IsPlayerCrimeRatingMild(IFaction faction)
	{
		if (faction.MainHeroCrimeRating > 0f)
		{
			return faction.MainHeroCrimeRating <= 30f;
		}
		return false;
	}

	public override float GetCost(IFaction faction, PaymentMethod paymentMethod, float minimumCrimeRating)
	{
		float x = MathF.Max(0f, faction.MainHeroCrimeRating - minimumCrimeRating);
		return paymentMethod switch
		{
			PaymentMethod.Gold => (int)(MathF.Pow(x, 1.2f) * 100f), 
			PaymentMethod.Influence => MathF.Pow(x, 1.2f), 
			_ => 0f, 
		};
	}

	public override ExplainedNumber GetDailyCrimeRatingChange(IFaction faction, bool includeDescriptions = false)
	{
		ExplainedNumber bonuses = new ExplainedNumber(0f, includeDescriptions);
		int num = faction.Settlements.Count((Settlement x) => x.IsTown && x.Alleys.Any((Alley y) => y.Owner == Hero.MainHero));
		bonuses.Add((float)num * Campaign.Current.Models.AlleyModel.GetDailyCrimeRatingOfAlley, includeDescriptions ? new TextObject("{=t87T82jq}Owned alleys") : null);
		if (faction.MainHeroCrimeRating.ApproximatelyEqualsTo(0f))
		{
			return bonuses;
		}
		Clan clan = faction as Clan;
		if (Hero.MainHero.Clan == faction)
		{
			bonuses.Add(-5f, includeDescriptions ? new TextObject("{=eNtRt6F5}Your own Clan") : null);
		}
		else if (faction.IsKingdomFaction && faction.Leader == Hero.MainHero)
		{
			bonuses.Add(-5f, includeDescriptions ? new TextObject("{=xer2bta5}Your own Kingdom") : null);
		}
		else if (Hero.MainHero.MapFaction == faction)
		{
			bonuses.Add(-1.5f, includeDescriptions ? new TextObject("{=QRwaQIbm}Is in Kingdom") : null);
		}
		else if (clan != null && Hero.MainHero.MapFaction == clan.Kingdom)
		{
			bonuses.Add(-1.25f, includeDescriptions ? new TextObject("{=hXGByLG9}Sharing the same Kingdom") : null);
		}
		else if (Hero.MainHero.Clan.IsAtWarWith(faction))
		{
			bonuses.Add(-0.25f, includeDescriptions ? new TextObject("{=BYTrUJyj}In War") : null);
		}
		else
		{
			bonuses.Add(-1f, includeDescriptions ? new TextObject("{=basevalue}Base") : null);
		}
		PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Roguery.WhiteLies, Hero.MainHero.CharacterObject, isPrimaryBonus: true, ref bonuses);
		return bonuses;
	}

	public override float GetMaxCrimeRating()
	{
		return 100f;
	}

	public override float GetMinAcceptableCrimeRating(IFaction faction)
	{
		if (faction != Hero.MainHero.MapFaction)
		{
			return 30f;
		}
		return 20f;
	}

	public override float GetCrimeRatingAfterPunishment()
	{
		return 25f;
	}
}
