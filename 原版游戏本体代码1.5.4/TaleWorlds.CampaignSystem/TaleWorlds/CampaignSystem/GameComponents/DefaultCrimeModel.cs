using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
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

	public override ExplainedNumber GetEffectiveCrimeChange(IFaction faction, float deltaCrimeRating)
	{
		ExplainedNumber result = new ExplainedNumber(deltaCrimeRating);
		if (deltaCrimeRating > 0f)
		{
			TraitEffectHelper.ApplyTraitEffect(Hero.MainHero, DefaultPersonalityTraitEffects.HonorCrimeIncreaseSlowEffect, ref result);
		}
		result.Add(faction.MainHeroCrimeRating);
		result.LimitMin(0f);
		result.LimitMax(Campaign.Current.Models.CrimeModel.GetMaxCrimeRating());
		return result;
	}

	public override ExplainedNumber GetDailyCrimeRatingChange(IFaction faction, bool includeDescriptions = false)
	{
		ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions);
		int num = faction.Settlements.Count((Settlement x) => x.IsTown && x.Alleys.Any((Alley y) => y.Owner == Hero.MainHero));
		result.Add((float)num * Campaign.Current.Models.AlleyModel.GetDailyCrimeRatingOfAlley, includeDescriptions ? new TextObject("{=t87T82jq}Owned alleys") : null);
		if (faction.MainHeroCrimeRating.ApproximatelyEqualsTo(0f))
		{
			return result;
		}
		if (Hero.MainHero.Clan == faction)
		{
			result.Add(-5f, includeDescriptions ? new TextObject("{=eNtRt6F5}Your own Clan") : null);
		}
		else if (faction.IsKingdomFaction && faction.Leader == Hero.MainHero)
		{
			result.Add(-5f, includeDescriptions ? new TextObject("{=xer2bta5}Your own Kingdom") : null);
		}
		else if (Hero.MainHero.MapFaction == faction)
		{
			result.Add(-1.5f, includeDescriptions ? new TextObject("{=QRwaQIbm}Is in Kingdom") : null);
		}
		else if (faction is Clan clan && Hero.MainHero.MapFaction == clan.Kingdom)
		{
			result.Add(-1.25f, includeDescriptions ? new TextObject("{=hXGByLG9}Sharing the same Kingdom") : null);
		}
		else if (Hero.MainHero.Clan.IsAtWarWith(faction))
		{
			result.Add(-0.25f, includeDescriptions ? new TextObject("{=BYTrUJyj}In War") : null);
		}
		else
		{
			result.Add(-1f, includeDescriptions ? new TextObject("{=basevalue}Base") : null);
		}
		TraitEffectHelper.ApplyTraitEffect(Hero.MainHero, DefaultPersonalityTraitEffects.HonorCrimeDecaySlowEffect, ref result);
		PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Roguery.WhiteLies, BattleEnvironment.Any, Hero.MainHero.CharacterObject, isPrimaryBonus: true, ref result);
		return result;
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
