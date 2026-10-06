using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class KingdomCreationModel : MBGameModel<KingdomCreationModel>
{
	public abstract int MinimumClanTierToCreateKingdom { get; }

	public abstract int MinimumNumberOfSettlementsOwnedToCreateKingdom { get; }

	public abstract int MinimumTroopCountToCreateKingdom { get; }

	public abstract int MaximumNumberOfInitialPolicies { get; }

	public abstract bool IsPlayerKingdomCreationPossible(out List<TextObject> explanations);

	public abstract bool IsPlayerKingdomAbdicationPossible(out List<TextObject> explanations);

	public abstract IEnumerable<CultureObject> GetAvailablePlayerKingdomCultures();
}
