using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond.MultiplayerBadges;
using TaleWorlds.MountAndBlade.MissionRepresentatives;

namespace TaleWorlds.MountAndBlade.Multiplayer;

public class DuelScoreboardData : IScoreboardData
{
	public MissionScoreboardComponent.ScoreboardHeader[] GetScoreboardHeaders()
	{
		GameNetwork.MyPeer.GetComponent<MissionRepresentativeBase>();
		return new MissionScoreboardComponent.ScoreboardHeader[7]
		{
			new MissionScoreboardComponent.ScoreboardHeader("ping", (MissionPeer missionPeer) => MathF.Round(missionPeer.GetNetworkPeer().AveragePingInMilliseconds).ToString(), (BotData bot) => ""),
			new MissionScoreboardComponent.ScoreboardHeader("avatar", (MissionPeer missionPeer) => "", (BotData bot) => ""),
			new MissionScoreboardComponent.ScoreboardHeader("badge", (MissionPeer missionPeer) => BadgeManager.GetByIndex(missionPeer.GetPeer().ChosenBadgeIndex)?.StringId, (BotData bot) => ""),
			new MissionScoreboardComponent.ScoreboardHeader("name", (MissionPeer missionPeer) => missionPeer.GetComponent<MissionPeer>().DisplayedName, (BotData bot) => new TextObject("{=hvQSOi79}Bot").ToString()),
			new MissionScoreboardComponent.ScoreboardHeader("winstreak", (MissionPeer missionPeer) => missionPeer.GetComponent<DuelMissionRepresentative>().NumberOfWins.ToString(), (BotData bot) => ""),
			new MissionScoreboardComponent.ScoreboardHeader("bounty", (MissionPeer missionPeer) => missionPeer.GetComponent<DuelMissionRepresentative>().Bounty.ToString(), (BotData bot) => ""),
			new MissionScoreboardComponent.ScoreboardHeader("score", (MissionPeer missionPeer) => missionPeer.GetComponent<DuelMissionRepresentative>().Score.ToString(), (BotData bot) => "")
		};
	}
}
