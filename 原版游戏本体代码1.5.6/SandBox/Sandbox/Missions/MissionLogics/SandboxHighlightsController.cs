using System.Collections.Generic;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class SandboxHighlightsController : MissionLogic
{
	private List<HighlightsController.HighlightType> _highlightTypes = new List<HighlightsController.HighlightType>
	{
		new HighlightsController.HighlightType("hlid_tournament_last_match_kill", "Champion of the Arena", "grpid_incidents", -5000, 3000, 0f, float.MaxValue, isVisibilityRequired: true)
	};

	private HighlightsController _highlightsController;

	public override void AfterStart()
	{
		_highlightsController = Mission.Current.GetMissionBehavior<HighlightsController>();
		foreach (HighlightsController.HighlightType highlightType in _highlightTypes)
		{
			HighlightsController.AddHighlightType(highlightType);
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		if (affectorAgent == null || !affectorAgent.IsMainAgent || affectedAgent == null || !affectedAgent.IsHuman)
		{
			return;
		}
		TournamentBehavior missionBehavior = Mission.Current.GetMissionBehavior<TournamentBehavior>();
		if (missionBehavior == null || missionBehavior.CurrentMatch == null || missionBehavior.NextRound != null)
		{
			return;
		}
		foreach (TournamentParticipant participant in missionBehavior.CurrentMatch.Participants)
		{
			if (affectorAgent.Character == participant.Character && affectedAgent.Character != participant.Character)
			{
				HighlightsController.Highlight highlight = new HighlightsController.Highlight
				{
					Start = Mission.Current.CurrentTime,
					End = Mission.Current.CurrentTime,
					HighlightType = _highlightsController.GetHighlightTypeWithId("hlid_tournament_last_match_kill")
				};
				_highlightsController.SaveHighlight(highlight, affectedAgent.Position);
				break;
			}
		}
	}
}
