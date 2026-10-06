using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond.MultiplayerBadges;
using TaleWorlds.MountAndBlade.MissionRepresentatives;

namespace TaleWorlds.MountAndBlade.Multiplayer;

public class SkirmishScoreboardData : IScoreboardData
{
	public MissionScoreboardComponent.ScoreboardHeader[] GetScoreboardHeaders()
	{
		return new MissionScoreboardComponent.ScoreboardHeader[9]
		{
			new MissionScoreboardComponent.ScoreboardHeader("ping", (MissionPeer missionPeer) => MathF.Round(missionPeer.GetNetworkPeer().AveragePingInMilliseconds).ToString(), (BotData bot) => ""),
			new MissionScoreboardComponent.ScoreboardHeader("avatar", (MissionPeer missionPeer) => "", (BotData bot) => ""),
			new MissionScoreboardComponent.ScoreboardHeader("badge", (MissionPeer missionPeer) => BadgeManager.GetByIndex(missionPeer.GetPeer().ChosenBadgeIndex)?.StringId, (BotData bot) => ""),
			new MissionScoreboardComponent.ScoreboardHeader("name", (MissionPeer missionPeer) => missionPeer.GetComponent<MissionPeer>().DisplayedName, (BotData bot) => new TextObject("{=hvQSOi79}Bot").ToString()),
			new MissionScoreboardComponent.ScoreboardHeader("kill", (MissionPeer missionPeer) => missionPeer.KillCount.ToString(), (BotData bot) => bot.KillCount.ToString()),
			new MissionScoreboardComponent.ScoreboardHeader("death", (MissionPeer missionPeer) => missionPeer.DeathCount.ToString(), (BotData bot) => bot.DeathCount.ToString()),
			new MissionScoreboardComponent.ScoreboardHeader("assist", (MissionPeer missionPeer) => missionPeer.AssistCount.ToString(), (BotData bot) => bot.AssistCount.ToString()),
			new MissionScoreboardComponent.ScoreboardHeader("gold", (MissionPeer missionPeer) => missionPeer.GetComponent<FlagDominationMissionRepresentative>().GetGoldAmountForVisual().ToString(), (BotData bot) => ""),
			new MissionScoreboardComponent.ScoreboardHeader("score", (MissionPeer missionPeer) => missionPeer.Score.ToString(), (BotData bot) => "".ToString())
		};
	}
}
