using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Localization;

namespace SandBox;

public class Add1000GoldCheat : GameplayCheatItem
{
	public override void ExecuteCheat()
	{
		GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 1000, disableNotification: true);
	}

	public override TextObject GetName()
	{
		return new TextObject("{=KLbeF6gf}Add 1000 Gold");
	}
}
