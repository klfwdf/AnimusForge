using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class GenericXpModel : MBGameModel<GenericXpModel>
{
	public abstract float GetXpMultiplier(Hero hero);
}
