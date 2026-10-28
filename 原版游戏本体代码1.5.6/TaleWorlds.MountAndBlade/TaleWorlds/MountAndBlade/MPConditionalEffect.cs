using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class MPConditionalEffect
{
	public class ConditionalEffectContainer : List<MPConditionalEffect>
	{
		private class ConditionState
		{
			public bool IsSatisfied { get; set; }
		}

		private Dictionary<MPConditionalEffect, ConditionalWeakTable<Agent, ConditionState>> _states;

		public ConditionalEffectContainer()
		{
		}

		public ConditionalEffectContainer(IEnumerable<MPConditionalEffect> conditionalEffects)
			: base(conditionalEffects)
		{
		}

		public bool GetState(MPConditionalEffect conditionalEffect, Agent agent)
		{
			if (_states == null || !_states.TryGetValue(conditionalEffect, out var value) || !value.TryGetValue(agent, out var value2))
			{
				return false;
			}
			return value2.IsSatisfied;
		}

		public void SetState(MPConditionalEffect conditionalEffect, Agent agent, bool state)
		{
			ConditionalWeakTable<Agent, ConditionState> value;
			ConditionState value2;
			if (_states == null)
			{
				_states = new Dictionary<MPConditionalEffect, ConditionalWeakTable<Agent, ConditionState>>();
				ConditionalWeakTable<Agent, ConditionState> conditionalWeakTable = new ConditionalWeakTable<Agent, ConditionState>();
				conditionalWeakTable.Add(agent, new ConditionState
				{
					IsSatisfied = state
				});
				_states.Add(conditionalEffect, conditionalWeakTable);
			}
			else if (!_states.TryGetValue(conditionalEffect, out value))
			{
				value = new ConditionalWeakTable<Agent, ConditionState>();
				value.Add(agent, new ConditionState
				{
					IsSatisfied = state
				});
				_states.Add(conditionalEffect, value);
			}
			else if (!value.TryGetValue(agent, out value2))
			{
				value.Add(agent, new ConditionState
				{
					IsSatisfied = state
				});
			}
			else
			{
				value2.IsSatisfied = state;
			}
		}

		public void ResetStates()
		{
			_states = null;
		}
	}

	public MBReadOnlyList<MPPerkCondition> Conditions { get; }

	public MBReadOnlyList<MPPerkEffectBase> Effects { get; }

	public MPPerkCondition.PerkEventFlags EventFlags
	{
		get
		{
			MPPerkCondition.PerkEventFlags perkEventFlags = MPPerkCondition.PerkEventFlags.None;
			foreach (MPPerkCondition condition in Conditions)
			{
				perkEventFlags |= condition.EventFlags;
			}
			return perkEventFlags;
		}
	}

	public bool IsTickRequired
	{
		get
		{
			foreach (MPPerkEffectBase effect in Effects)
			{
				if (effect.IsTickRequired)
				{
					return true;
				}
			}
			return false;
		}
	}

	public MPConditionalEffect(List<string> gameModes, XmlNode node)
	{
		MBList<MPPerkCondition> mBList = new MBList<MPPerkCondition>();
		MBList<MPPerkEffectBase> mBList2 = new MBList<MPPerkEffectBase>();
		foreach (XmlNode childNode in node.ChildNodes)
		{
			if (childNode.Name == "Conditions")
			{
				foreach (XmlNode childNode2 in childNode.ChildNodes)
				{
					if (childNode2.NodeType == XmlNodeType.Element)
					{
						mBList.Add(MPPerkCondition.CreateFrom(gameModes, childNode2));
					}
				}
			}
			else
			{
				if (!(childNode.Name == "Effects"))
				{
					continue;
				}
				foreach (XmlNode childNode3 in childNode.ChildNodes)
				{
					if (childNode3.NodeType == XmlNodeType.Element)
					{
						MPPerkEffect item = MPPerkEffect.CreateFrom(childNode3);
						mBList2.Add(item);
					}
				}
			}
		}
		Conditions = mBList;
		Effects = mBList2;
	}

	public bool Check(MissionPeer peer)
	{
		foreach (MPPerkCondition condition in Conditions)
		{
			if (!condition.Check(peer))
			{
				return false;
			}
		}
		return true;
	}

	public bool Check(Agent agent)
	{
		foreach (MPPerkCondition condition in Conditions)
		{
			if (!condition.Check(agent))
			{
				return false;
			}
		}
		return true;
	}

	public void OnEvent(bool isWarmup, MissionPeer peer, ConditionalEffectContainer container)
	{
		if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() <= 0 && (peer == null || peer.ControlledAgent?.IsActive() != true))
		{
			return;
		}
		bool flag = true;
		foreach (MPPerkCondition condition in Conditions)
		{
			if (condition.IsPeerCondition && !condition.Check(peer))
			{
				flag = false;
				break;
			}
		}
		if (!flag)
		{
			if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() > 0)
			{
				MBReadOnlyList<IFormationUnit> mBReadOnlyList = peer?.ControlledFormation?.Arrangement.GetAllUnits();
				if (mBReadOnlyList == null)
				{
					return;
				}
				{
					foreach (IFormationUnit item in mBReadOnlyList)
					{
						if (item is Agent agent && agent.IsActive())
						{
							UpdateAgentState(isWarmup, container, agent, state: false);
						}
					}
					return;
				}
			}
			UpdateAgentState(isWarmup, container, peer.ControlledAgent, state: false);
			return;
		}
		if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() > 0)
		{
			MBReadOnlyList<IFormationUnit> mBReadOnlyList2 = peer?.ControlledFormation?.Arrangement.GetAllUnits();
			if (mBReadOnlyList2 == null)
			{
				return;
			}
			{
				foreach (IFormationUnit item2 in mBReadOnlyList2)
				{
					if (!(item2 is Agent agent2) || !agent2.IsActive())
					{
						continue;
					}
					bool state = true;
					foreach (MPPerkCondition condition2 in Conditions)
					{
						if (!condition2.IsPeerCondition && !condition2.Check(agent2))
						{
							state = false;
							break;
						}
					}
					UpdateAgentState(isWarmup, container, agent2, state);
				}
				return;
			}
		}
		bool state2 = true;
		foreach (MPPerkCondition condition3 in Conditions)
		{
			if (!condition3.IsPeerCondition && !condition3.Check(peer.ControlledAgent))
			{
				state2 = false;
				break;
			}
		}
		UpdateAgentState(isWarmup, container, peer.ControlledAgent, state2);
	}

	public void OnEvent(bool isWarmup, Agent agent, ConditionalEffectContainer container)
	{
		if (agent == null)
		{
			return;
		}
		bool state = true;
		foreach (MPPerkCondition condition in Conditions)
		{
			if (!condition.Check(agent))
			{
				state = false;
				break;
			}
		}
		UpdateAgentState(isWarmup, container, agent, state);
	}

	public void OnTick(bool isWarmup, MissionPeer peer, int tickCount)
	{
		if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() <= 0 && (peer == null || peer.ControlledAgent?.IsActive() != true))
		{
			return;
		}
		bool flag = true;
		foreach (MPPerkCondition condition in Conditions)
		{
			if (condition.IsPeerCondition && !condition.Check(peer))
			{
				flag = false;
				break;
			}
		}
		if (!flag)
		{
			return;
		}
		if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() > 0)
		{
			MBReadOnlyList<IFormationUnit> mBReadOnlyList = peer?.ControlledFormation?.Arrangement.GetAllUnits();
			if (mBReadOnlyList == null)
			{
				return;
			}
			{
				foreach (IFormationUnit item in mBReadOnlyList)
				{
					if (!(item is Agent agent) || !agent.IsActive())
					{
						continue;
					}
					bool flag2 = true;
					foreach (MPPerkCondition condition2 in Conditions)
					{
						if (!condition2.IsPeerCondition && !condition2.Check(agent))
						{
							flag2 = false;
							break;
						}
					}
					if (!flag2)
					{
						continue;
					}
					foreach (MPPerkEffectBase effect in Effects)
					{
						if ((!isWarmup || !effect.IsDisabledInWarmup) && effect.IsTickRequired)
						{
							effect.OnTick(agent, tickCount);
						}
					}
				}
				return;
			}
		}
		bool flag3 = true;
		foreach (MPPerkCondition condition3 in Conditions)
		{
			if (!condition3.IsPeerCondition && !condition3.Check(peer.ControlledAgent))
			{
				flag3 = false;
				break;
			}
		}
		if (!flag3)
		{
			return;
		}
		foreach (MPPerkEffectBase effect2 in Effects)
		{
			if ((!isWarmup || !effect2.IsDisabledInWarmup) && effect2.IsTickRequired)
			{
				effect2.OnTick(peer.ControlledAgent, tickCount);
			}
		}
	}

	private void UpdateAgentState(bool isWarmup, ConditionalEffectContainer container, Agent agent, bool state)
	{
		if (container.GetState(this, agent) == state)
		{
			return;
		}
		container.SetState(this, agent, state);
		foreach (MPPerkEffectBase effect in Effects)
		{
			if (!isWarmup || !effect.IsDisabledInWarmup)
			{
				effect.OnUpdate(agent, state);
			}
		}
	}
}
