using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class ClanTierModel : MBGameModel<ClanTierModel>
{
	public abstract int MinClanTier { get; }

	public abstract int MaxClanTier { get; }

	public abstract int MercenaryEligibleTier { get; }

	public abstract int VassalEligibleTier { get; }

	public abstract int BannerEligibleTier { get; }

	public abstract int RebelClanStartingTier { get; }

	public abstract int CompanionToLordClanStartingTier { get; }

	public abstract int CalculateInitialRenown(Clan clan);

	public abstract int CalculateInitialInfluence(Clan clan);

	public abstract int CalculateTier(Clan clan);

	public abstract (ExplainedNumber, bool) HasUpcomingTier(Clan clan, out TextObject extraExplanation, bool includeDescriptions = false);

	public abstract int GetRequiredRenownForTier(int tier);

	public abstract int GetPartyLimitForTier(Clan clan, int clanTierToCheck);

	public abstract int GetCompanionLimit(Clan clan);
}
