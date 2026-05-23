using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class CompanionHiringPriceCalculationModel : MBGameModel<CompanionHiringPriceCalculationModel>
{
	public abstract int GetCompanionHiringPrice(Hero companion);
}
