using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultClanTierModel : ClanTierModel
{
	private static readonly int[] TierLowerRenownLimits = new int[7] { 0, 50, 150, 350, 900, 2350, 6150 };

	private readonly TextObject _partyLimitBonusText = GameTexts.FindText("str_clan_tier_party_limit_bonus");

	private readonly TextObject _companionLimitBonusText = GameTexts.FindText("str_clan_tier_companion_limit_bonus");

	private readonly TextObject _mercenaryEligibleText = GameTexts.FindText("str_clan_tier_mercenary_eligible");

	private readonly TextObject _vassalEligibleText = GameTexts.FindText("str_clan_tier_vassal_eligible");

	private readonly TextObject _additionalCurrentPartySizeBonus = GameTexts.FindText("str_clan_tier_party_size_bonus");

	private readonly TextObject _additionalWorkshopCountBonus = GameTexts.FindText("str_clan_tier_workshop_count_bonus");

	private readonly TextObject _kingdomEligibleText = GameTexts.FindText("str_clan_tier_kingdom_eligible");

	public override int MinClanTier => 0;

	public override int MaxClanTier => 6;

	public override int MercenaryEligibleTier => 1;

	public override int VassalEligibleTier => 2;

	public override int BannerEligibleTier => 0;

	public override int RebelClanStartingTier => 3;

	public override int CompanionToLordClanStartingTier => 2;

	private int KingdomEligibleTier => Campaign.Current.Models.KingdomCreationModel.MinimumClanTierToCreateKingdom;

	public override int CalculateInitialRenown(Clan clan)
	{
		int num = TierLowerRenownLimits[clan.Tier];
		int num2 = ((clan.Tier >= MaxClanTier) ? (TierLowerRenownLimits[MaxClanTier] + 1500) : TierLowerRenownLimits[clan.Tier + 1]);
		int maxValue = (int)((float)num2 - (float)(num2 - num) * 0.4f);
		return MBRandom.RandomInt(num, maxValue);
	}

	public override int CalculateInitialInfluence(Clan clan)
	{
		return (int)(150f + (float)MBRandom.RandomInt((int)((float)CalculateInitialRenown(clan) / 15f)) + (float)MBRandom.RandomInt(MBRandom.RandomInt(MBRandom.RandomInt(400))));
	}

	public override int CalculateTier(Clan clan)
	{
		int result = MinClanTier;
		for (int i = MinClanTier + 1; i <= MaxClanTier; i++)
		{
			if (clan.Renown >= (float)TierLowerRenownLimits[i])
			{
				result = i;
			}
		}
		return result;
	}

	public override (ExplainedNumber, bool) HasUpcomingTier(Clan clan, out TextObject extraExplanation, bool includeDescriptions = false)
	{
		bool flag = clan.Tier < MaxClanTier;
		ExplainedNumber item = new ExplainedNumber(0f, includeDescriptions);
		extraExplanation = null;
		if (flag)
		{
			int num = GetPartyLimitForTier(clan, clan.Tier + 1) - GetPartyLimitForTier(clan, clan.Tier);
			if (num != 0)
			{
				item.Add(num, _partyLimitBonusText);
			}
			int num2 = GetCompanionLimitFromTier(clan.Tier + 1) - GetCompanionLimitFromTier(clan.Tier);
			if (num2 != 0)
			{
				item.Add(num2, _companionLimitBonusText);
			}
			int nextClanTierPartySizeEffectChangeForHero = Campaign.Current.Models.PartySizeLimitModel.GetNextClanTierPartySizeEffectChangeForHero(clan.Leader);
			if (nextClanTierPartySizeEffectChangeForHero > 0)
			{
				item.Add(nextClanTierPartySizeEffectChangeForHero, _additionalCurrentPartySizeBonus);
			}
			int num3 = Campaign.Current.Models.WorkshopModel.GetMaxWorkshopCountForClanTier(clan.Tier + 1) - Campaign.Current.Models.WorkshopModel.GetMaxWorkshopCountForClanTier(clan.Tier);
			if (num3 > 0)
			{
				item.Add(num3, _additionalWorkshopCountBonus);
			}
			if (clan.Tier + 1 == MercenaryEligibleTier)
			{
				extraExplanation = _mercenaryEligibleText;
			}
			else if (clan.Tier + 1 == VassalEligibleTier)
			{
				extraExplanation = _vassalEligibleText;
			}
			else if (clan.Tier + 1 == KingdomEligibleTier)
			{
				extraExplanation = _kingdomEligibleText;
			}
		}
		return (item, flag);
	}

	public override int GetRequiredRenownForTier(int tier)
	{
		return TierLowerRenownLimits[tier];
	}

	public override int GetPartyLimitForTier(Clan clan, int clanTierToCheck)
	{
		ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions: false, null);
		if (!clan.IsMinorFaction)
		{
			if (clanTierToCheck < 3)
			{
				result.Add(1f);
			}
			else if (clanTierToCheck < 5)
			{
				result.Add(2f);
			}
			else
			{
				result.Add(3f);
			}
		}
		else
		{
			result.Add(MathF.Clamp(clanTierToCheck, 1f, 4f));
		}
		AddPartyLimitPerkEffects(clan, ref result);
		return MathF.Round(result.ResultNumber);
	}

	private void AddPartyLimitPerkEffects(Clan clan, ref ExplainedNumber result)
	{
		if (clan.Leader != null && clan.Leader.GetPerkValue(DefaultPerks.Leadership.TalentMagnet))
		{
			result.Add(DefaultPerks.Leadership.TalentMagnet.SecondaryBonus, DefaultPerks.Leadership.TalentMagnet.Name);
		}
	}

	public override int GetCompanionLimit(Clan clan)
	{
		int num = GetCompanionLimitFromTier(clan.Tier);
		if (clan.Leader.GetPerkValue(DefaultPerks.Leadership.WePledgeOurSwords))
		{
			num += (int)DefaultPerks.Leadership.WePledgeOurSwords.PrimaryBonus;
		}
		if (clan.Leader.GetPerkValue(DefaultPerks.Charm.Camaraderie))
		{
			num += (int)DefaultPerks.Charm.Camaraderie.SecondaryBonus;
		}
		return num;
	}

	private int GetCompanionLimitFromTier(int clanTier)
	{
		return clanTier + 3;
	}
}
