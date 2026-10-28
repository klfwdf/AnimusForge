using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace SandBox;

public class UnlockFogOfWarCheat : GameplayCheatItem
{
	public override void ExecuteCheat()
	{
		foreach (Hero allAliveHero in Hero.AllAliveHeroes)
		{
			allAliveHero.IsKnownToPlayer = true;
		}
	}

	public override TextObject GetName()
	{
		return new TextObject("{=jPtG0Pu1}Unlock Fog of War");
	}
}
