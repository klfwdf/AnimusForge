using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using psai.net;

namespace TaleWorlds.MountAndBlade.View.MissionViews.Sound;

public class MusicStealthMissionView : MissionView, IMusicHandler
{
	private enum WarningSoundState
	{
		None,
		Neutral,
		Cautious,
		PatrollingCautious,
		Alarmed
	}

	private const string StealthNotificationSoundEventId = "event:/ui/stealth/stealth_notification_b";

	private const string AgentStateParameterName = "agent_state";

	private static object _lockObject = new object();

	private List<Agent> _cautiousAgents;

	private List<Agent> _patrollingCautiousAgents;

	private List<Agent> _detectedAgents;

	private List<Agent> _combatAgents;

	private Dictionary<Agent, SoundEvent> _stealthNotificationSoundEvents;

	private WarningSoundState _warningSoundState;

	bool IMusicHandler.IsPausable => false;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_cautiousAgents = new List<Agent>();
		_patrollingCautiousAgents = new List<Agent>();
		_detectedAgents = new List<Agent>();
		_combatAgents = new List<Agent>();
		_stealthNotificationSoundEvents = new Dictionary<Agent, SoundEvent>();
		MBMusicManager.Current.DeactivateCurrentMode();
		MBMusicManager.Current.ActivateBattleMode();
		MBMusicManager.Current.OnBattleMusicHandlerInit(this);
		_warningSoundState = WarningSoundState.None;
	}

	public override void OnMissionScreenFinalize()
	{
		MBMusicManager.Current.DeactivateBattleMode();
		MBMusicManager.Current.OnBattleMusicHandlerFinalize();
	}

	void IMusicHandler.OnUpdated(float dt)
	{
	}

	public override void AfterStart()
	{
		base.AfterStart();
		MBMusicManager.Current.StartTheme(MusicTheme.StealthA, 0.25f);
		PsaiCore.Instance.HoldCurrentIntensity(hold: true);
	}

	public override void OnAgentAlarmedStateChanged(Agent agent, Agent.AIStateFlag flag)
	{
		lock (_lockObject)
		{
			_cautiousAgents.Remove(agent);
			_patrollingCautiousAgents.Remove(agent);
			_detectedAgents.Remove(agent);
			switch (flag)
			{
			case Agent.AIStateFlag.Cautious:
				_cautiousAgents.Add(agent);
				break;
			case Agent.AIStateFlag.PatrollingCautious:
				_patrollingCautiousAgents.Add(agent);
				break;
			case Agent.AIStateFlag.Alarmed:
				_detectedAgents.Add(agent);
				break;
			}
			CheckIntensityChange();
			CheckWarningSoundStateChange(agent);
		}
	}

	public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
	{
		base.OnAgentHit(affectedAgent, affectorAgent, in affectorWeapon, in blow, in attackCollisionData);
		if (affectedAgent.IsMainAgent && affectorAgent != null && affectorAgent != affectedAgent && affectorAgent.IsEnemyOf(Agent.Main) && !_combatAgents.Contains(affectorAgent))
		{
			_combatAgents.Add(affectorAgent);
			CheckIntensityChange();
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
		_cautiousAgents.Remove(affectedAgent);
		_patrollingCautiousAgents.Remove(affectedAgent);
		_detectedAgents.Remove(affectedAgent);
		_combatAgents.Remove(affectedAgent);
		CheckIntensityChange();
		CheckWarningSoundStateChange(affectedAgent);
		if (_stealthNotificationSoundEvents.ContainsKey(affectedAgent) && !affectedAgent.IsActive())
		{
			_stealthNotificationSoundEvents.Remove(affectedAgent);
		}
	}

	private void CheckIntensityChange()
	{
		float num = ((_combatAgents.Count > 0) ? 1f : ((_detectedAgents.Count > 0) ? 0.75f : ((_cautiousAgents.Count <= 0 && _patrollingCautiousAgents.Count <= 0) ? 0.25f : 0.5f)));
		float num2 = num - PsaiCore.Instance.GetCurrentIntensity();
		if (Math.Abs(num2) > 1E-05f)
		{
			PsaiCore.Instance.HoldCurrentIntensity(hold: false);
			PsaiCore.Instance.AddToCurrentIntensity(num2);
			PsaiCore.Instance.HoldCurrentIntensity(hold: true);
		}
	}

	private void CheckWarningSoundStateChange(Agent relatedAgent)
	{
		WarningSoundState intendedWarningSoundState = GetIntendedWarningSoundState();
		if (intendedWarningSoundState != _warningSoundState)
		{
			ChangeWarningSoundState(intendedWarningSoundState, relatedAgent);
		}
	}

	private WarningSoundState GetIntendedWarningSoundState()
	{
		if (_detectedAgents.Count > 0)
		{
			return WarningSoundState.Alarmed;
		}
		if (_patrollingCautiousAgents.Count > 0)
		{
			return WarningSoundState.PatrollingCautious;
		}
		if (_cautiousAgents.Count > 0)
		{
			return WarningSoundState.Cautious;
		}
		return WarningSoundState.Neutral;
	}

	private void ChangeWarningSoundState(WarningSoundState newState, Agent relatedAgent)
	{
		if (!_stealthNotificationSoundEvents.TryGetValue(relatedAgent, out var value))
		{
			value = SoundEvent.CreateEventFromString("event:/ui/stealth/stealth_notification_b", base.Mission.Scene);
			_stealthNotificationSoundEvents.Add(relatedAgent, value);
		}
		_warningSoundState = newState;
		value.SetPosition(relatedAgent.Position);
		float value2 = (float)_warningSoundState;
		value.SetParameter("agent_state", value2);
		if (!value.IsPlaying())
		{
			value.Play();
		}
	}
}
