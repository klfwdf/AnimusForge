using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Localization;

namespace SandBox;

public class Add100RenownCheat : GameplayCheatItem
{
	public override void ExecuteCheat()
	{
		GainRenownAction.Apply(Hero.MainHero, 100f, doNotNotify: true);
	}

	public override TextObject GetName()
	{
		return new TextObject("{=zXQwb3lj}Add 100 Renown");
	}
}
