using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Objects.Usables;

namespace TaleWorlds.MountAndBlade;

public class ClimbingMachineDetachment : IDetachment
{
	private const int Capacity = int.MaxValue;

	private readonly List<Agent> _agents;

	private readonly MBList<Formation> _userFormations;

	private readonly MBReadOnlyList<ClimbingMachine> _climbingMachines;

	private readonly MBList<Agent> _climberAgents;

	public MBReadOnlyList<Formation> UserFormations => _userFormations;

	public bool IsLoose => true;

	public bool IsActive { get; private set; }

	public ClimbingMachineDetachment(in MBList<ClimbingMachine> climbingMachines)
	{
		_agents = new List<Agent>();
		_userFormations = new MBList<Formation>();
		_climbingMachines = climbingMachines;
		_climberAgents = new MBList<Agent>();
		IsActive = true;
	}

	public void Deactivate()
	{
		IsActive = false;
	}

	public void AddAgent(Agent agent, int slotIndex, Agent.AIScriptedFrameFlags customFlags = Agent.AIScriptedFrameFlags.None)
	{
		_agents.Add(agent);
	}

	public void AddAgentAtSlotIndex(Agent agent, int slotIndex)
	{
		AddAgent(agent, slotIndex);
		agent.Formation?.DetachUnit(agent, isLoose: true);
		agent.Detachment = this;
		agent.SetDetachmentWeight(1f);
	}

	void IDetachment.FormationStartUsing(Formation formation)
	{
		_userFormations.Add(formation);
	}

	void IDetachment.FormationStopUsing(Formation formation)
	{
		_userFormations.Remove(formation);
	}

	public bool IsUsedByFormation(Formation formation)
	{
		return _userFormations.Contains(formation);
	}

	Agent IDetachment.GetMovingAgentAtSlotIndex(int slotIndex)
	{
		if (slotIndex >= _agents.Count)
		{
			return null;
		}
		return _agents[slotIndex];
	}

	void IDetachment.GetSlotIndexWeightTuples(List<(int, float)> slotIndexWeightTuples)
	{
	}

	bool IDetachment.IsSlotAtIndexAvailableForAgent(int slotIndex, Agent agent)
	{
		return false;
	}

	bool IDetachment.IsAgentEligible(Agent agent)
	{
		return agent.Detachment == this;
	}

	void IDetachment.UnmarkDetachment()
	{
	}

	bool IDetachment.IsDetachmentRecentlyEvaluated()
	{
		return true;
	}

	void IDetachment.MarkSlotAtIndex(int slotIndex)
	{
	}

	bool IDetachment.IsAgentUsingOrInterested(Agent agent)
	{
		return _agents.Contains(agent);
	}

	void IDetachment.OnFormationLeave(Formation formation)
	{
		for (int num = _agents.Count - 1; num >= 0; num--)
		{
			Agent agent = _agents[num];
			if (agent.Formation == formation && !agent.IsPlayerControlled)
			{
				((IDetachment)this).RemoveAgent(agent);
				formation.AttachUnit(agent);
			}
		}
	}

	public bool IsStandingPointAvailableForAgent(Agent agent)
	{
		return false;
	}

	public List<float> GetTemplateCostsOfAgent(Agent candidate, List<float> oldValue)
	{
		return oldValue;
	}

	float IDetachment.GetExactCostOfAgentAtSlot(Agent candidate, int slotIndex)
	{
		return float.MaxValue;
	}

	public float GetTemplateWeightOfAgent(Agent candidate)
	{
		return float.MaxValue;
	}

	public float? GetWeightOfAgentAtNextSlot(List<Agent> newAgents, out Agent match)
	{
		match = null;
		return null;
	}

	public float? GetWeightOfAgentAtNextSlot(List<(Agent, float)> agentTemplateScores, out Agent match)
	{
		match = null;
		return null;
	}

	public float? GetWeightOfAgentAtOccupiedSlot(Agent detachedAgent, List<Agent> newAgents, out Agent match)
	{
		match = null;
		return float.MaxValue;
	}

	public void RemoveAgent(Agent agent)
	{
		_agents.Remove(agent);
		agent.DisableScriptedMovement();
		agent.DisableScriptedCombatMovement();
		_climberAgents.Remove(agent);
	}

	public int GetNumberOfUsableSlots()
	{
		return int.MaxValue;
	}

	public WorldFrame? GetAgentFrame(Agent agent)
	{
		return null;
	}

	public float? GetWeightOfNextSlot(BattleSideEnum side)
	{
		return null;
	}

	public float GetWeightOfOccupiedSlot(Agent agent)
	{
		return float.MinValue;
	}

	float IDetachment.GetDetachmentWeight(BattleSideEnum side)
	{
		return float.MinValue;
	}

	void IDetachment.ResetEvaluation()
	{
	}

	bool IDetachment.IsEvaluated()
	{
		return true;
	}

	void IDetachment.SetAsEvaluated()
	{
	}

	float IDetachment.GetDetachmentWeightFromCache()
	{
		return float.MinValue;
	}

	float IDetachment.ComputeAndCacheDetachmentWeight(BattleSideEnum side)
	{
		return float.MinValue;
	}

	public void TickClimbingMachines()
	{
		if (!IsActive || _userFormations.Count <= 0)
		{
			return;
		}
		for (int num = _userFormations[0].UnitsWithoutLooseDetachedOnes.Count - 1; num >= 0; num--)
		{
			if (_userFormations[0].UnitsWithoutLooseDetachedOnes[num] is Agent { IsPlayerControlled: false, IsDetachableFromFormation: not false } agent && agent.IsInWater() && agent.Formation != null && !_climberAgents.Contains(agent))
			{
				if (agent.AIMoveToGameObjectIsEnabled() || agent.CurrentlyUsedGameObject != null)
				{
					agent.StopUsingGameObject(isSuccessful: true, Agent.StopUsingGameObjectFlags.None);
				}
				_climberAgents.Add(agent);
				AddAgentAtSlotIndex(agent, 0);
				if (agent.GetPrimaryWieldedItemIndex() != EquipmentIndex.None)
				{
					agent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);
				}
				if (agent.GetOffhandWieldedItemIndex() != EquipmentIndex.None)
				{
					agent.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.Instant);
				}
				if (agent.GetAgentFlags().HasAnyFlag(AgentFlag.CanAttack | AgentFlag.CanDefend))
				{
					agent.SetAgentFlags((AgentFlag)((uint)agent.GetAgentFlags() & 0xFFFFFFE7u));
				}
			}
		}
		for (int num2 = _userFormations[0].DetachedUnits.Count - 1; num2 >= 0; num2--)
		{
			Agent agent2 = _userFormations[0].DetachedUnits[num2];
			if (!agent2.IsPlayerControlled && agent2.IsDetachableFromFormation && agent2.Detachment != this && agent2.IsInWater())
			{
				agent2.Detachment.RemoveAgent(agent2);
				if (agent2.Formation != null && !_climberAgents.Contains(agent2))
				{
					agent2.Formation.AttachUnit(agent2);
					_climberAgents.Add(agent2);
					AddAgentAtSlotIndex(agent2, 0);
					if (agent2.GetPrimaryWieldedItemIndex() != EquipmentIndex.None)
					{
						agent2.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);
					}
					if (agent2.GetOffhandWieldedItemIndex() != EquipmentIndex.None)
					{
						agent2.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.Instant);
					}
					if (agent2.GetAgentFlags().HasAnyFlag(AgentFlag.CanAttack | AgentFlag.CanDefend))
					{
						agent2.SetAgentFlags((AgentFlag)((uint)agent2.GetAgentFlags() & 0xFFFFFFE7u));
					}
				}
			}
		}
		for (int num3 = _climberAgents.Count - 1; num3 >= 0; num3--)
		{
			Agent agent3 = _climberAgents[num3];
			if (agent3.IsActive())
			{
				if (agent3.IsInWater())
				{
					float num4 = float.MaxValue;
					ClimbingMachine climbingMachine = null;
					foreach (ClimbingMachine climbingMachine2 in _climbingMachines)
					{
						float num5 = climbingMachine2.GameEntity.GlobalPosition.DistanceSquared(agent3.Position) * (Vec2.DotProduct((agent3.Position.AsVec2 - climbingMachine2.GameEntity.GlobalPosition.AsVec2).Normalized(), climbingMachine2.PilotStandingPoint.GameEntity.GetGlobalFrame().rotation.f.AsVec2.Normalized()) + 2f);
						if (num5 < num4)
						{
							num4 = num5;
							climbingMachine = climbingMachine2;
						}
					}
					if (climbingMachine != null)
					{
						StandingPoint standingPoint = null;
						foreach (StandingPoint standingPoint2 in climbingMachine.StandingPoints)
						{
							if (!standingPoint2.HasUser && !standingPoint2.IsDeactivated)
							{
								standingPoint = standingPoint2;
								break;
							}
						}
						if (standingPoint != null && agent3.CanReachAndUseObject(standingPoint, standingPoint.GetUserFrameForAgent(agent3).Origin.AsVec2.DistanceSquared(agent3.Position.AsVec2)) && MathF.Abs(standingPoint.GameEntity.GlobalPosition.z - agent3.Position.z) < 1.5f)
						{
							agent3.DisableScriptedMovement();
							agent3.UseGameObject(standingPoint);
						}
						else
						{
							WorldPosition position = new WorldPosition(climbingMachine.GameEntity.Scene, climbingMachine.GameEntity.GlobalPosition);
							agent3.SetScriptedPosition(ref position, addHumanLikeDelay: false);
						}
					}
				}
				else if (agent3.CurrentlyUsedGameObject == null)
				{
					_climberAgents.RemoveAt(num3);
					RemoveAgent(agent3);
					agent3.Formation?.AttachUnit(agent3);
				}
			}
			else
			{
				_climberAgents.RemoveAt(num3);
				RemoveAgent(agent3);
				agent3.Formation?.AttachUnit(agent3);
			}
		}
	}
}
