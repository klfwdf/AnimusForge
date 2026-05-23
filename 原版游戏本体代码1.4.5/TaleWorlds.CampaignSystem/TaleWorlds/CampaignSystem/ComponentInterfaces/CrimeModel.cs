using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class CrimeModel : MBGameModel<CrimeModel>
{
	[Flags]
	public enum PaymentMethod : uint
	{
		ExMachina = 0x1000u,
		Gold = 1u,
		Influence = 2u,
		Punishment = 4u,
		Execution = 8u
	}

	public abstract float DeclareWarCrimeRatingThreshold { get; }

	public abstract float GetMaxCrimeRating();

	public abstract float GetMinAcceptableCrimeRating(IFaction faction);

	public abstract float GetCrimeRatingAfterPunishment();

	public abstract bool DoesPlayerHaveAnyCrimeRating(IFaction faction);

	public abstract bool IsPlayerCrimeRatingSevere(IFaction faction);

	public abstract bool IsPlayerCrimeRatingModerate(IFaction faction);

	public abstract bool IsPlayerCrimeRatingMild(IFaction faction);

	public abstract float GetCost(IFaction faction, PaymentMethod paymentMethod, float minimumCrimeRating);

	public abstract ExplainedNumber GetDailyCrimeRatingChange(IFaction faction, bool includeDescriptions = false);
}
