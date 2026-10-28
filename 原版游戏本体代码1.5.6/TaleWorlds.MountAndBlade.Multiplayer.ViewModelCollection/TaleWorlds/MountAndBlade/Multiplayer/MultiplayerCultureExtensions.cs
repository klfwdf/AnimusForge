using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.Multiplayer;

public static class MultiplayerCultureExtensions
{
	internal static BasicCultureObject GetCulture(this MissionScoreboardComponent.MissionScoreboardSide side)
	{
		if (side == null)
		{
			return null;
		}
		string text = ((side.Side == BattleSideEnum.Attacker) ? MultiplayerOptions.OptionType.CultureTeam1.GetStrValue() : MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
		if (!string.IsNullOrEmpty(text))
		{
			return MBObjectManager.Instance.GetObject<BasicCultureObject>(text);
		}
		return null;
	}
}
