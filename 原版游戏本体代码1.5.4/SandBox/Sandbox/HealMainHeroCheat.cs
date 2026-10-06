using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox;

public class HealMainHeroCheat : GameplayCheatItem
{
	public override void ExecuteCheat()
	{
		if (Agent.Main != null)
		{
			Agent.Main.Health = Agent.Main.HealthLimit;
		}
	}

	public override TextObject GetName()
	{
		return new TextObject("{=PsmnVIcb}Heal Main Hero");
	}
}
