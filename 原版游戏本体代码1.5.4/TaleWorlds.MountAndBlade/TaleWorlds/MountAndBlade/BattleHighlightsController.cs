using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class BattleHighlightsController : MissionLogic
{
	private List<HighlightsController.HighlightType> _highlightTypes = new List<HighlightsController.HighlightType>
	{
		new HighlightsController.HighlightType("hlid_kill_last_enemy_on_battlefield", "Take No Prisoners", "grpid_incidents", -5000, 3000, 0f, float.MaxValue, isVisibilityRequired: false),
		new HighlightsController.HighlightType("hlid_win_battle_as_last_man_standing", "Last Man Standing", "grpid_incidents", -5000, 3000, 0f, float.MaxValue, isVisibilityRequired: false),
		new HighlightsController.HighlightType("hlid_wall_break_kill", "Wall Break Kill", "grpid_incidents", -5000, 3000, 0.25f, 100f, isVisibilityRequired: true)
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
		if (affectorAgent == null || affectedAgent == null || !affectorAgent.IsHuman || !affectedAgent.IsHuman || (agentState != AgentState.Killed && agentState != AgentState.Unconscious))
		{
			return;
		}
		bool flag = affectorAgent?.Team.IsPlayerTeam ?? false;
		bool flag2 = affectorAgent?.IsMainAgent ?? false;
		HighlightsController.Highlight highlight = new HighlightsController.Highlight
		{
			Start = Mission.Current.CurrentTime,
			End = Mission.Current.CurrentTime
		};
		bool flag3 = false;
		if ((flag2 || flag) && (killingBlow.WeaponRecordWeaponFlags.HasAllFlags(WeaponFlags.Burning) || killingBlow.WeaponRecordWeaponFlags.HasAllFlags(WeaponFlags.AffectsArea)))
		{
			highlight.HighlightType = _highlightsController.GetHighlightTypeWithId("hlid_wall_break_kill");
			flag3 = true;
		}
		bool flag4 = (affectedAgent.Team?.TeamAgents)?.Any((Agent agent) => agent.State != AgentState.Killed && agent.State != AgentState.Unconscious) ?? true;
		if (!flag4 && (flag2 || flag))
		{
			highlight.HighlightType = _highlightsController.GetHighlightTypeWithId("hlid_kill_last_enemy_on_battlefield");
			flag3 = true;
		}
		if (flag2)
		{
			MBReadOnlyList<Agent> mBReadOnlyList = affectorAgent.Team?.TeamAgents;
			if (mBReadOnlyList != null && !mBReadOnlyList.Any((Agent agent) => !agent.IsMainAgent && agent.State != AgentState.Killed && agent.State != AgentState.Unconscious) && !flag4)
			{
				highlight.HighlightType = _highlightsController.GetHighlightTypeWithId("hlid_win_battle_as_last_man_standing");
				flag3 = true;
			}
		}
		if (flag3)
		{
			_highlightsController.SaveHighlight(highlight, affectedAgent.Position);
		}
	}
}
