using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class RansomValueCalculationModel : MBGameModel<RansomValueCalculationModel>
{
	public abstract int PrisonerRansomValue(CharacterObject prisoner, Hero sellerHero = null);
}
