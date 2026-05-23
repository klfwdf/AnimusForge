using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Localization;

namespace SandBox;

public class Add100InfluenceCheat : GameplayCheatItem
{
	public override void ExecuteCheat()
	{
		ChangeClanInfluenceAction.Apply(Clan.PlayerClan, 100f);
	}

	public override TextObject GetName()
	{
		return new TextObject("{=6TgRwB2Q}Add 100 Influence");
	}
}
