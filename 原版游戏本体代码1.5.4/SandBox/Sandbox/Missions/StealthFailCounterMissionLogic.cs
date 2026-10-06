using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions;

public class StealthFailCounterMissionLogic : MissionLogic
{
	private readonly List<Agent> _alarmedAgents = new List<Agent>();

	private Timer _failCounter;

	public float FailCounterSeconds = 5f;

	private TextObject _popupTitle;

	private TextObject _popupDescription;

	public bool IsActive { get; private set; } = true;

	public float FailCounterElapsedTime
	{
		get
		{
			if (!IsActive || _failCounter == null)
			{
				return -1f;
			}
			return _failCounter.ElapsedTime();
		}
	}

	public override void OnAgentAlarmedStateChanged(Agent agent, Agent.AIStateFlag flag)
	{
		base.OnAgentAlarmedStateChanged(agent, flag);
		if (agent.Team != null && !agent.Team.IsPlayerAlly)
		{
			if (agent.IsAlarmed() && !_alarmedAgents.Contains(agent))
			{
				_alarmedAgents.Add(agent);
			}
			else if (!agent.IsAlarmed() && _alarmedAgents.Contains(agent))
			{
				_alarmedAgents.Remove(agent);
			}
			if (_alarmedAgents.Count == 0)
			{
				IsActive = false;
			}
			else
			{
				IsActive = true;
			}
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
		_alarmedAgents.Remove(affectedAgent);
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (!IsActive)
		{
			return;
		}
		if (base.Mission.Mode == MissionMode.Stealth)
		{
			if (_failCounter == null && !_alarmedAgents.IsEmpty())
			{
				_failCounter = new Timer(base.Mission.CurrentTime, FailCounterSeconds);
			}
			if (_failCounter != null)
			{
				if (_alarmedAgents.IsEmpty())
				{
					_failCounter = null;
				}
				else if (_failCounter.Check(base.Mission.CurrentTime))
				{
					ShowMissionFailedPopup();
				}
			}
		}
		else
		{
			IsActive = false;
		}
	}

	public void SetFailTexts(TextObject title, TextObject description)
	{
		_popupTitle = title;
		_popupDescription = description;
	}

	private void ShowMissionFailedPopup()
	{
		IsActive = false;
		TextObject obj = (TextObject.IsNullOrEmpty(_popupTitle) ? new TextObject("{=wQbfWNZO}Mission Failed!") : _popupTitle);
		TextObject textObject = (TextObject.IsNullOrEmpty(_popupDescription) ? new TextObject("{=5R0TauYV}You have been compromised.") : _popupDescription);
		InformationManager.ShowInquiry(new InquiryData(affirmativeText: new TextObject("{=DM6luo3c}Continue").ToString(), titleText: obj.ToString(), text: textObject.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, negativeText: null, affirmativeAction: delegate
		{
			Game.Current.EventManager.TriggerEvent(new OnStealthMissionCounterFailedEvent());
			Mission.Current.EndMission();
		}, negativeAction: null), Campaign.Current.GameMode == CampaignGameMode.Campaign);
	}
}
