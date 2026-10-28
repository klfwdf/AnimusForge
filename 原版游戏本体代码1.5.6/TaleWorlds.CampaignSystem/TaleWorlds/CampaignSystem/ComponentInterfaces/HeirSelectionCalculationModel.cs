using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class HeirSelectionCalculationModel : MBGameModel<HeirSelectionCalculationModel>
{
	public abstract int HighestSkillPoint { get; }

	public abstract int CalculateHeirSelectionPoint(Hero candidateHeir, Hero deadHero, ref Hero maxSkillHero);
}
