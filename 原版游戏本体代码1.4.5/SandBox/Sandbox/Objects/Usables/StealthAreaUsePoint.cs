using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects.Usables;

public class StealthAreaUsePoint : UsableMissionObject
{
	private const string HighlightEntityName = "highlight_pointer_glow_ground";

	private bool _isEnabled = true;

	private bool _isAlreadyUsed;

	private WeakGameEntity _highlightGameEntity;

	public string ActionStringId;

	public string DescriptionStringId;

	protected override void OnInit()
	{
		base.OnInit();
		_isAlreadyUsed = false;
		ActionMessage = GameTexts.FindText(string.IsNullOrEmpty(ActionStringId) ? "str_call_troops" : ActionStringId);
		ActionMessage.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		DescriptionMessage = (string.IsNullOrEmpty(DescriptionStringId) ? TextObject.GetEmpty() : GameTexts.FindText(DescriptionStringId));
		foreach (WeakGameEntity child in base.GameEntity.GetChildren())
		{
			foreach (WeakGameEntity child2 in child.GetChildren())
			{
				if (child2.Name.Equals("highlight_pointer_glow_ground"))
				{
					_highlightGameEntity = child2;
					break;
				}
			}
			if (_highlightGameEntity != null)
			{
				break;
			}
		}
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		return DescriptionMessage;
	}

	public override void OnUse(Agent userAgent, sbyte agentBoneIndex)
	{
		base.OnUse(userAgent, agentBoneIndex);
		if (!IsInCombat())
		{
			if (userAgent.IsMainAgent)
			{
				SoundManager.StartOneShotEvent("event:/mission/combat/pickup_arrows", userAgent.Position);
				_isAlreadyUsed = true;
				_highlightGameEntity.SetVisibilityExcludeParents(visible: false);
				userAgent.StopUsingGameObject();
			}
			DisableAgentAIs();
		}
	}

	public override void OnUseStopped(Agent userAgent, bool isSuccessful, int preferenceIndex)
	{
		base.OnUseStopped(userAgent, isSuccessful, preferenceIndex);
		if (LockUserFrames || LockUserPositions)
		{
			userAgent.ClearTargetFrame();
		}
	}

	public void DisableAgentAIs()
	{
		foreach (Agent agent in Mission.Current.Agents)
		{
			if (agent.IsActive() && agent.IsAIControlled)
			{
				agent.SetIsAIPaused(isPaused: true);
				WorldPosition position = new WorldPosition(Mission.Current.Scene, agent.Position);
				agent.SetScriptedPosition(ref position, addHumanLikeDelay: false);
			}
		}
	}

	public override bool IsDisabledForAgent(Agent agent)
	{
		if (!agent.IsMainAgent)
		{
			if (!_isAlreadyUsed)
			{
				return !_isEnabled;
			}
			return true;
		}
		return false;
	}

	public override bool IsUsableByAgent(Agent userAgent)
	{
		if (userAgent.IsMainAgent && !_isAlreadyUsed && _isEnabled)
		{
			return !IsInCombat();
		}
		return false;
	}

	private bool IsInCombat()
	{
		bool result = false;
		foreach (Agent allAgent in Mission.Current.AllAgents)
		{
			if (allAgent.IsActive())
			{
				Agent.AIStateFlag aIStateFlag = Agent.AIStateFlag.Alarmed;
				if ((allAgent.AIStateFlags & aIStateFlag) == aIStateFlag)
				{
					result = true;
					break;
				}
			}
		}
		return result;
	}

	public void EnableStealthAreaUsePoint()
	{
		if (!_isEnabled)
		{
			SoundManager.StartOneShotEvent("event:/ui/notification/quest_update", base.GameEntity.GlobalPosition);
		}
		_highlightGameEntity.SetVisibilityExcludeParents(visible: true);
		_isEnabled = true;
	}

	public void DisableStealthAreaUsePoint()
	{
		_isEnabled = false;
		_highlightGameEntity.SetVisibilityExcludeParents(visible: false);
	}
}
